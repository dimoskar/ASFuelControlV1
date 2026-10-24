using System;
using System.Linq;
using System.Text;
using ASFuelControl.Invoicing.Persistence;
using Exedron.Invoicing.Core;
using Exedron.Invoicing.Core.Models;
using Exedron.Invoicing.Adapters;
using Newtonsoft.Json;

namespace ASFuelControl.Invoicing
{
    // Non-secret, versioned snapshot. No ORM entities survive preparation.
    public sealed class InvoiceWorkflowSnapshot : IInvoiceSubmissionSource, IFiscalSigningSource
    {
        public int Version { get; set; } = 1;
        public Guid InvoiceId { get; set; }
        public ProviderRoute Provider { get; set; }
        public PaymentRoute Payment { get; set; }
        public InvoiceBuildRequest Invoice { get; set; }
        public FiscalSigningRequest Signing { get; set; }
        public bool LocalOnly { get; set; }
        public string PrintDestination { get; set; }
        public string PrintFormat { get; set; }
        public byte[] PrintContent { get; set; }
        public Guid? MyDieselCouponCaseId { get; set; }
        public MyDieselScope MyDieselScope { get; set; }
        public decimal? MyDieselExciseDiscountPerLitre { get; set; }
        InvoiceBuildRequest IInvoiceSubmissionSource.Prepare(Guid id, ProviderRoute route)
        {
            if (id != InvoiceId || JsonConvert.SerializeObject(route) != JsonConvert.SerializeObject(Provider)) throw new InvalidOperationException("Workflow route mismatch.");
            return Invoice;
        }
        FiscalSigningRequest IFiscalSigningSource.Prepare(FiscalDocumentReference document, FiscalSigningScope scope)
        {
            if (document.DocumentId != InvoiceId || Signing == null || !FiscalSigningCoordinator.SameScope(scope, Signing.Scope)) throw new InvalidOperationException("Workflow signing scope mismatch.");
            return Signing;
        }
    }
    public sealed class InvoiceWorkflowResult
    {
        public string FiscalState { get; internal set; }
        public Guid? AcceptedAttemptId { get; internal set; }
        public InvoiceResponse Response { get; internal set; }
        public string PaymentState { get; internal set; }
        public FiscalOperationRecord Print { get; internal set; }
        public bool Accepted => AcceptedAttemptId.HasValue;
        public bool LocalOnly { get; internal set; }
        public Guid? OfflineAttemptId { get; internal set; }
        public Guid? MyDieselAttemptId { get; internal set; }
        public string MyDieselState { get; internal set; }
        public bool ReadyToPrint => Accepted || LocalOnly || OfflineAttemptId.HasValue;
    }
    public interface IMyDieselRedemptionCoordinator
    {
        MyDieselServiceResult RedeemOrRecover(Guid couponCaseId, Guid invoiceId, string mark);
    }
    public interface IInvoiceWorkflowSource
    {
        // Called once under the document lease. The host may allocate numbers using
        // SqlInvoiceWorkflowStore, then opens a fresh ORM context to freeze input.
        InvoiceWorkflowSnapshot Prepare(IFiscalDocumentLease lease);
    }
    public sealed class InvoiceWorkflowService
    {
        private readonly SqlFiscalDatabase database;
        private readonly SqlInvoiceWorkflowStore snapshots;
        private readonly SqlFiscalHistoryStore history;
        private readonly ProviderSubmissionService providers;
        private readonly ProviderPaymentService payments;
        private readonly FiscalSigningService signing;
        private readonly FiscalPrintService printing;
        private readonly Func<InvoiceWorkflowSnapshot, InvoiceResponse, byte[]> render;
        private readonly Action<Guid, InvoiceWorkflowResult> project;
        private readonly Func<ProviderRoute, ProviderRoute> currentConnection;
        private readonly IMyDieselRedemptionCoordinator myDiesel;

        public InvoiceWorkflowService(SqlFiscalDatabase database, ProviderSubmissionService providers,
            ProviderPaymentService payments, FiscalSigningService signing, FiscalPrintService printing,
            Func<InvoiceWorkflowSnapshot, InvoiceResponse, byte[]> render, Action<Guid, InvoiceWorkflowResult> project,
            Func<ProviderRoute, ProviderRoute> currentConnection = null, IMyDieselRedemptionCoordinator myDiesel = null)
        {
            this.database = database; this.providers = providers; this.payments = payments;
            this.signing = signing; this.printing = printing;
            this.render = render ?? throw new ArgumentNullException(nameof(render));
            this.project = project ?? throw new ArgumentNullException(nameof(project));
            this.currentConnection = currentConnection;
            this.myDiesel = myDiesel;
            snapshots = new SqlInvoiceWorkflowStore(database); history = new SqlFiscalHistoryStore(database);
        }

        public InvoiceWorkflowResult Process(Guid invoiceId, IInvoiceWorkflowSource source, bool print = true, string reprintReason = null, bool retryRejected = false,
            bool retryDeclinedPayment = false)
        {
            using (var lease = database.EnterInvoice(invoiceId))
            {
                var bytes = snapshots.Load(lease);
                if (bytes == null)
                {
                    if (reprintReason != null) throw new InvalidOperationException("A reprint requires an existing workflow and saved print job.");
                    foreach (FiscalJournalFamily family in Enum.GetValues(typeof(FiscalJournalFamily)))
                        if (history.Load(lease, family).Any()) throw new InvalidOperationException("Existing fiscal history needs migration/reconciliation before PrintAgent takes ownership.");
                    var prepared = source?.Prepare(lease) ?? throw new InvalidOperationException("A source is required for first issuance.");
                    Validate(prepared, invoiceId);
                    bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(prepared));
                    snapshots.Save(lease, bytes);
                }
                var snapshot = JsonConvert.DeserializeObject<InvoiceWorkflowSnapshot>(Encoding.UTF8.GetString(bytes));
                // The fingerprinted input was fully validated before first persistence.
                // Reprinting must remain independent of today's mapper/serializer rules.
                Validate(snapshot, invoiceId, false);
                if (snapshot.Provider != null && currentConnection != null &&
                    !Enum.GetValues(typeof(FiscalJournalFamily)).Cast<FiscalJournalFamily>().Any(f => history.Load(lease,f).Any()))
                {
                    var configured = currentConnection(snapshot.Provider) ?? throw new InvalidOperationException("The configured provider connection is unavailable.");
                    bytes = snapshots.RefreshProviderConnection(lease,bytes,JsonConvert.SerializeObject(configured));
                    snapshot = JsonConvert.DeserializeObject<InvoiceWorkflowSnapshot>(Encoding.UTF8.GetString(bytes));
                    Validate(snapshot,invoiceId,false);
                }
                if (history.Load(lease, FiscalJournalFamily.Cancellation).Any()) throw new InvalidOperationException("Cancellation history requires reconciliation before printing or issuance.");
                var previousPrint = new SqlFiscalOperationStore(database).Load(lease, true).LastOrDefault();
                if (reprintReason != null && previousPrint == null) throw new InvalidOperationException("There is no saved print job to reprint.");

                var result = new InvoiceWorkflowResult { LocalOnly = snapshot.LocalOnly };
                if (previousPrint != null)
                {
                    // Once printing has started, acceptance is already proven by its
                    // source FK. A print retry makes NO provider, device or POS call.
                    if (previousPrint.OfflineReceipt)
                    {
                        result.OfflineAttemptId = previousPrint.OriginalAttemptId;
                        var submission = history.Load(lease,FiscalJournalFamily.Submission).Single(a => a.AttemptId == previousPrint.OriginalAttemptId);
                        result.FiscalState = submission.State;
                        if (submission.State == "Accepted") result.AcceptedAttemptId = submission.AttemptId;
                        result.Response = IlydaInvoiceProvider.MapResponse(Newtonsoft.Json.Linq.JObject.Parse((string)Newtonsoft.Json.Linq.JObject.Parse(submission.SnapshotJson)["ResponseJson"]));
                    }
                    else
                    {
                        result.AcceptedAttemptId = previousPrint.OriginalAttemptId;
                        result.FiscalState = snapshot.LocalOnly ? "LocalOnly" : "Accepted";
                    }
                    result.Print = previousPrint;
                    project(invoiceId,result);
                }
                else
                {
                    if (snapshot.LocalOnly) result.FiscalState = "LocalOnly";
                    if (snapshot.Signing != null)
                    {
                        var signed = signing.Sign(snapshot.Signing.Document, snapshot.Signing.Scope, snapshot);
                        result.FiscalState = signed.State.ToString();
                        if (signed.State != FiscalSigningState.Signed) return result;
                        result.AcceptedAttemptId = signed.Request.AttemptId;
                        result.Response = new InvoiceResponse { InvoiceSignature = signed.Result.Signature, QrCodeUrl = signed.Result.QrCode };
                    }
                    if (snapshot.Provider != null)
                    {
                        ProviderSubmissionResult submitted;
                        if (snapshot.Payment != null)
                        {
                            var payment = payments.Send(invoiceId, snapshot.Payment, snapshot, retryDeclined: retryDeclinedPayment, retryRejectedInvoice: retryRejected);
                            result.PaymentState = payment.Payment?.State;
                            submitted = payment.Submission;
                            if (submitted == null && !string.IsNullOrWhiteSpace(payment.Payment?.ErrorMessage))
                                result.Response = new InvoiceResponse { Errors = new[] { payment.Payment.ErrorMessage } };
                        }
                        else if (snapshot.Signing != null) submitted = providers.SendAfterSigning(invoiceId, snapshot.Provider, snapshot, result.AcceptedAttemptId.Value);
                        else submitted = providers.Send(invoiceId, snapshot.Provider, snapshot, retryRejected);
                        result.FiscalState = submitted?.State ?? ("Payment: " + result.PaymentState);
                        result.AcceptedAttemptId = submitted?.Accepted == true ? submitted.AttemptId : null;
                        if (!result.Accepted)
                        {
                            // Authentication/pre-dispatch errors have no submission
                            // attempt yet, but their explanation must reach the caller.
                            result.Response = submitted?.Response ?? result.Response;
                            if (submitted?.State == "Queued" && snapshot.Provider.ProviderKey == InvoiceProviderCatalog.DirectIlydaKey &&
                                submitted.Response?.TransmissionFailure2 == true && ProviderPrintMetadata.Missing(snapshot.Provider.ProviderKey,submitted.Response).Length == 0)
                                result.OfflineAttemptId = submitted.AttemptId;
                            else return result;
                        }
                        if (snapshot.Signing != null)
                        {
                            submitted.Response.InvoiceSignature = result.Response.InvoiceSignature;
                            if (string.IsNullOrWhiteSpace(submitted.Response.QrCodeUrl)) submitted.Response.QrCodeUrl = result.Response.QrCodeUrl;
                        }
                        result.Response = submitted.Response;
                    }
                    // Project acceptance independently of physical printing. If this
                    // write fails, the next call recovers durable fiscal acceptance.
                    project(invoiceId, result);
                    if (snapshot.MyDieselCouponCaseId.HasValue)
                    {
                        if (myDiesel == null) throw new InvalidOperationException("The saved myDiesel redemption coordinator is unavailable.");
                        if (!result.Accepted || result.Response == null || string.IsNullOrWhiteSpace(result.Response.Mark))
                            throw new InvalidOperationException("myDiesel requires an accepted invoice with a 15-digit MARK before redemption.");
                        var redemption = myDiesel.RedeemOrRecover(snapshot.MyDieselCouponCaseId.Value,invoiceId,result.Response.Mark);
                        result.MyDieselAttemptId = redemption.AttemptId;
                        result.MyDieselState = redemption.CouponCase == null ? redemption.OperationState.ToString() : redemption.CouponCase.LocalStage.ToString();
                        if (redemption.CouponCase == null || (redemption.CouponCase.LocalStage != MyDieselCaseStage.Redeemed &&
                            redemption.CouponCase.LocalStage != MyDieselCaseStage.VerificationPending && redemption.CouponCase.LocalStage != MyDieselCaseStage.Final))
                            return result;
                    }
                }
                if (print)
                {
                    result.Print = printing.Print(lease, result.OfflineAttemptId ?? result.AcceptedAttemptId, snapshot.PrintDestination,
                        snapshot.PrintFormat, () => render(snapshot, result.Response), reprintReason, result.OfflineAttemptId.HasValue);
                    project(invoiceId, result);
                }
                return result;
            }
        }
        private static void Validate(InvoiceWorkflowSnapshot value, Guid id, bool validateInvoice = true)
        {
            if (value == null || value.Version != 1 && value.Version != 2 || value.InvoiceId != id ||
                !value.LocalOnly && value.Provider == null && value.Signing == null || value.PrintContent == null || value.PrintContent.Length == 0 ||
                string.IsNullOrWhiteSpace(value.PrintDestination) || string.IsNullOrWhiteSpace(value.PrintFormat)) throw new InvalidOperationException("Incomplete workflow snapshot.");
            if (value.LocalOnly && (value.Provider != null || value.Signing != null || value.Payment != null))
                throw new InvalidOperationException("Local-only processing cannot include external fiscal or payment operations.");
            if (value.MyDieselCouponCaseId.HasValue && (value.Version != 2 || value.MyDieselScope == null || value.LocalOnly || value.Provider == null))
                throw new InvalidOperationException("myDiesel requires a version-2 electronic invoice workflow and a frozen non-secret coupon scope.");
            if (!value.MyDieselCouponCaseId.HasValue && value.MyDieselScope != null)
                throw new InvalidOperationException("A myDiesel scope requires a coupon case.");
            if (value.MyDieselCouponCaseId.HasValue && value.MyDieselExciseDiscountPerLitre.HasValue)
                MyDieselTaxRules.NormalizeExciseDiscountPerLitre(value.MyDieselExciseDiscountPerLitre.Value);
            if (!value.MyDieselCouponCaseId.HasValue && value.MyDieselExciseDiscountPerLitre.HasValue)
                throw new InvalidOperationException("A myDiesel excise discount requires a coupon case.");
            // Applies to both first issuance and restored workflows, before any
            // device call. Provider issuance obtains its signature from the provider.
            if (value.Provider != null && value.Provider.ProviderKey != InvoiceProviderCatalog.MyDataKey && value.Signing != null)
                throw new InvalidOperationException("A provider invoice cannot also use a fiscal signing device.");
            if (value.Signing != null && (value.Signing.Document.Kind != FiscalDocumentKind.Invoice || value.Signing.Document.DocumentId != id)) throw new InvalidOperationException("Signing document mismatch.");
            if (value.Payment != null && (value.Signing != null || JsonConvert.SerializeObject(value.Payment.Provider) != JsonConvert.SerializeObject(value.Provider))) throw new InvalidOperationException("Payment route mismatch or unsupported device/POS combination.");
            if (value.Provider != null && validateInvoice)
            {
                if (value.Invoice?.Invoice?.InvoiceId != id || !value.Invoice.RequireIssuedIdentity) throw new InvalidOperationException("Issued invoice identity required.");
                var model = new InvoiceBuilder(new InvoiceTypeResolver()).Build(value.Invoice);
                if (model.Issuer.VATNumber != value.Provider.IssuerVat || model.Issuer.Branch != value.Provider.IssuerBranch) throw new InvalidOperationException("Issuer differs from workflow route.");
                new InvoiceAdapterFactory(new InvoiceTypeResolver()).CreatePayloadMapper(value.Provider.ProviderKey).Serialize(model, id);
                bool pos = (model.PaymentMethods ?? new PaymentMethod[0]).Any(p => (int)p.Type == 7 || (int)p.Type == 8);
                if (pos != (value.Payment != null)) throw new InvalidOperationException("Explicit POS route required exactly for card/IRIS invoices.");
                if (value.Signing != null && (value.Provider.ProviderKey != InvoiceProviderCatalog.MyDataKey ||
                    value.Signing.Scope.IssuerVat != value.Provider.IssuerVat || value.Signing.Scope.IssuerBranch != value.Provider.IssuerBranch ||
                    (value.Signing.Scope.Environment == FiscalEnvironment.Test) != value.Provider.TestMode)) throw new InvalidOperationException("Only matching Samtec/direct-myDATA composition is supported.");
            }
        }
    }
}
