using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ASFuelControl.Invoicing.Persistence;
using Exedron.Invoicing.Core;
using Exedron.Invoicing.Core.Models;
using Exedron.Invoicing.Adapters;
using Exedron.Invoicing.Adapters.Samtec;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ASFuelControl.Invoicing
{
    public sealed class InvoiceContinuationBlockedException :
        InvalidOperationException
    {
        public InvoiceContinuationBlockedException(string message) :
            base(message)
        {
        }
    }

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
        // Optional brand/company QR payload frozen with the printable document.
        public string CompanyQrContent { get; set; }
        public Guid? MyDieselCouponCaseId { get; set; }
        public MyDieselScope MyDieselScope { get; set; }
        public decimal? MyDieselExciseDiscountPerLitre { get; set; }
        // Audit trail for an issued identity whose fiscal payload was regenerated
        // before any external operation completed. Previous attempts remain in the
        // fiscal journal; this records why the current frozen payload differs.
        public List<InvoicePaymentMethodChange> PaymentMethodChanges { get; set; }
        // A stale, unprinted direct-pump delivery can be resumed on a later day.
        // Its issued identity and host transaction date stay unchanged; only the
        // planned dispatch timestamp in the frozen electronic payload is moved.
        public List<InvoiceDispatchDateChange> DispatchDateChanges { get; set; }
        // A provider may reject an otherwise unissued document because its issue
        // date is no longer the current date.  Reissuing it is a new fiscal
        // identity: the abandoned number/date and the replacement identity are
        // retained here while the original rejected provider attempt remains in
        // the fiscal journal.
        public List<InvoiceIssueDateReissue> IssueDateReissues { get; set; }
        // A user-requested resend may rebuild the current counterpart country or
        // invoice type. The previous attempt remains immutable in the fiscal
        // journal while this records the regenerated document identity.
        public List<InvoiceForeignPartyCorrection> ForeignPartyCorrections { get; set; }
        // A user-requested resend uses the currently configured receiver. The
        // previous attempt remains immutable in the fiscal journal.
        public List<InvoiceSubmissionReceiverChange> SubmissionReceiverChanges { get; set; }
        // Explicit, audited adoption of a legacy invoice that was incorrectly
        // signed locally even though its current policy assigns signing to an
        // invoice provider. The original evidence remains immutable here and is
        // replaced in the host projection only after provider acceptance.
        public LegacyFiscalEvidenceReplacement LegacyFiscalReplacement { get; set; }
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
    public sealed class InvoicePaymentMethodChange
    {
        public int PreviousPaymentType { get; set; }
        public int PaymentType { get; set; }
        public string ChangedBy { get; set; }
        public DateTime ChangedUtc { get; set; }
    }
    public sealed class InvoiceDispatchDateChange
    {
        public DateTime PreviousDispatchDate { get; set; }
        public DateTime DispatchDate { get; set; }
        public string ChangedBy { get; set; }
        public DateTime ChangedUtc { get; set; }
    }
    public sealed class InvoiceIssueDateReissue
    {
        public int PreviousNumber { get; set; }
        public string PreviousSeries { get; set; }
        public DateTime PreviousIssueDate { get; set; }
        public int Number { get; set; }
        public string Series { get; set; }
        public DateTime IssueDate { get; set; }
        public string InvoiceType { get; set; }
        public string ChangedBy { get; set; }
        public DateTime ChangedUtc { get; set; }
        public string Reason { get; set; }
    }
    public sealed class InvoiceForeignPartyCorrection
    {
        public string VatNumber { get; set; }
        public string PreviousCountry { get; set; }
        public string Country { get; set; }
        public string PreviousInvoiceType { get; set; }
        public string InvoiceType { get; set; }
        public DateTime ChangedUtc { get; set; }
        public string Reason { get; set; }
    }
    public sealed class InvoiceSubmissionReceiverChange
    {
        public Guid PreviousAttemptId { get; set; }
        public string PreviousProviderKey { get; set; }
        public string ProviderKey { get; set; }
        public string PreviousAccount { get; set; }
        public string Account { get; set; }
        public bool PreviousTestMode { get; set; }
        public bool TestMode { get; set; }
        public bool ReusedApprovedPayment { get; set; }
        public DateTime ChangedUtc { get; set; }
        public string Reason { get; set; }
    }
    public sealed class LegacyFiscalEvidenceReplacement
    {
        public string PreviousInvoiceSignature { get; set; }
        public string PreviousQrCode { get; set; }
        public string AuthorizedBy { get; set; }
        public DateTime AuthorizedUtc { get; set; }
        public string Reason { get; set; }
    }
    public sealed class InvoiceWorkflowResult
    {
        public string FiscalState { get; internal set; }
        public Guid? AcceptedAttemptId { get; internal set; }
        public InvoiceResponse Response { get; internal set; }
        public string FiscalQrCodeUrl { get; internal set; }
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

        private const string ReprintRequestPrefix = "ASFC-REPRINT-V1:";

        public InvoiceWorkflowResult Process(Guid invoiceId, IInvoiceWorkflowSource source, bool print = true, string reprintReason = null, bool retryRejected = false,
            bool retryDeclinedPayment = false)
        {
            var reprintDestination = ReadReprintDestination(ref reprintReason);
            return Process(invoiceId,source,print,reprintReason,retryRejected,retryDeclinedPayment,reprintDestination);
        }

        // Keep the temporarily published seven-argument entry point so either
        // executable generation can run while an installation is being updated.
        public InvoiceWorkflowResult Process(Guid invoiceId, IInvoiceWorkflowSource source, bool print, string reprintReason, bool retryRejected,
            bool retryDeclinedPayment, string reprintDestination)
        {
            return ProcessCore(invoiceId,source,print,reprintReason,retryRejected,
                retryDeclinedPayment,reprintDestination,false);
        }

        // PrintAgent-only repair path. A definitive Samtec amount rejection may
        // be retried automatically only when the current mapper produces different
        // request bytes. Once those corrected bytes have been rejected, later polls
        // see no payload change and cannot dispatch again.
        public InvoiceWorkflowResult ProcessAutomaticSamtecAmountCorrection(Guid invoiceId,
            IInvoiceWorkflowSource source, bool print = true, string reprintReason = null,
            string reprintDestination = null)
        {
            return ProcessCore(invoiceId,source,print,reprintReason,false,false,
                reprintDestination,true);
        }

        private InvoiceWorkflowResult ProcessCore(Guid invoiceId, IInvoiceWorkflowSource source,
            bool print, string reprintReason, bool retryRejected, bool retryDeclinedPayment,
            string reprintDestination, bool automaticSamtecAmountCorrection)
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
                        if (!snapshot.LocalOnly && snapshot.Provider != null)
                        {
                            var submission = history.Load(lease,FiscalJournalFamily.Submission)
                                .Single(a => a.AttemptId == previousPrint.OriginalAttemptId);
                            result.Response = ProviderSubmissionService.ReadAcceptedResponse(submission);
                        }
                        if (!snapshot.LocalOnly && snapshot.Signing != null)
                        {
                            var signed = history.Load(lease,FiscalJournalFamily.Signing).Last(a => a.State == "Signed");
                            var signingEvidence = Newtonsoft.Json.Linq.JObject.Parse(signed.SnapshotJson);
                            result.FiscalQrCodeUrl = (string)signingEvidence["QrCode"];
                            if (result.Response == null) result.Response = new InvoiceResponse();
                            result.Response.InvoiceSignature = (string)signingEvidence["Signature"];
                            if (string.IsNullOrWhiteSpace(result.Response.QrCodeUrl))
                                result.Response.QrCodeUrl = result.FiscalQrCodeUrl;
                        }
                    }
                    result.Print = previousPrint;
                    project(invoiceId,result);
                }
                else
                {
                    var submissionHistory = history.Load(lease,
                        FiscalJournalFamily.Submission);
                    var previousSubmission = submissionHistory.LastOrDefault();
                    bool regenerateRejectedSubmission = retryRejected && source != null &&
                        previousSubmission != null &&
                        (previousSubmission.State == "Rejected" ||
                            previousSubmission.State == "RetryAuthorized");
                    Guid? releasedSubmissionAttemptId =
                        regenerateRejectedSubmission
                            ? previousSubmission.AttemptId
                            : (Guid?)null;
                    if (regenerateRejectedSubmission)
                    {
                        InvoiceWorkflowSnapshot regeneratedSubmission = source.Prepare(lease);
                        if (regeneratedSubmission == null)
                        {
                            throw new InvalidOperationException(
                                "The corrected provider workflow could not be regenerated.");
                        }

                        var signingHistory = history.Load(lease,
                            FiscalJournalFamily.Signing);
                        FiscalAttemptEvidence previousSigning = signingHistory.LastOrDefault();
                        bool hasSignedFiscalRequest = signingHistory.Any(item =>
                            item.State == FiscalSigningState.Signed.ToString());
                        var paymentHistory = history.Load(lease,
                            FiscalJournalFamily.Payment);
                        FiscalAttemptEvidence previousPayment =
                            paymentHistory.LastOrDefault();
                        bool hasApprovedPayment = previousPayment != null &&
                            (previousPayment.State == "Approved" ||
                                previousPayment.State == "Invoiced");
                        try
                        {
                            snapshot = MergeRegeneratedSubmission(snapshot,
                                regeneratedSubmission, invoiceId,
                                previousSubmission, hasSignedFiscalRequest,
                                hasApprovedPayment);
                        }
                        catch (InvoiceContinuationBlockedException exception)
                        {
                            if (previousSubmission.State == "RetryAuthorized")
                            {
                                var resendStore =
                                    new SqlProviderSubmissionResendStore(database);
                                resendStore.Revoke(lease,
                                    previousSubmission.AttemptId,
                                    exception.Message);
                            }
                            throw;
                        }
                        string replacementJson = JsonConvert.SerializeObject(snapshot);
                        byte[] replacement = Encoding.UTF8.GetBytes(replacementJson);
                        bytes = snapshots.ReplaceAfterRejectedSubmission(lease, bytes,
                            replacement);
                    }

                    if (snapshot.LocalOnly) result.FiscalState = "LocalOnly";
                    if (snapshot.Signing != null)
                    {
                        var previousSigning = history.Load(lease,FiscalJournalFamily.Signing).LastOrDefault();
                        bool retrySigning = retryRejected;
                        InvoiceWorkflowSnapshot regenerated = null;
                        if (!retrySigning && automaticSamtecAmountCorrection && source != null &&
                            IsAutomaticSamtecAmountCorrection(lease,snapshot,previousSigning))
                        {
                            regenerated = source.Prepare(lease) ?? throw new InvalidOperationException(
                                "The corrected invoice workflow could not be regenerated.");
                            Validate(regenerated,invoiceId);
                            retrySigning = !snapshot.Signing.Payload.SequenceEqual(
                                regenerated.Signing?.Payload ?? new byte[0]);
                        }
                        if (retrySigning && previousSigning?.State == FiscalSigningState.Rejected.ToString() &&
                            snapshot.Signing.Scope.SignerKey == SamtecFiscalSigner.Key)
                        {
                            if (source != null)
                            {
                                if (regenerated == null)
                                    regenerated = source.Prepare(lease) ?? throw new InvalidOperationException(
                                        "The corrected invoice workflow could not be regenerated.");
                                snapshot = MergeRegeneratedSigning(snapshot,regenerated,invoiceId);
                                var replacement = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(snapshot));
                                bytes = snapshots.ReplaceAfterRejectedSigning(lease,bytes,replacement);
                            }
                            else
                                snapshot.Signing = SamtecFiscalSigner.CorrectLineAmountsForRetry(snapshot.Signing);
                        }
                        if (snapshot.Signing != null)
                        {
                            var signed = signing.Sign(snapshot.Signing.Document,
                                snapshot.Signing.Scope, snapshot,
                                retryDefinitiveFailure: retrySigning,
                                releasedSubmissionAttemptId:
                                    releasedSubmissionAttemptId);
                            // A corrected retry has a new immutable request/attempt ID.
                            // Keep the in-memory workflow aligned for direct-myDATA
                            // submission and for recovery after a process restart.
                            snapshot.Signing = signed.Request;
                            result.FiscalState = signed.State.ToString();
                            if (signed.State != FiscalSigningState.Signed)
                            {
                                string diagnostic = signed.Result?.Diagnostic ??
                                    signed.Diagnostic;
                                if (!string.IsNullOrWhiteSpace(diagnostic))
                                {
                                    result.Response = new InvoiceResponse
                                    {
                                        Errors = new[] { diagnostic }
                                    };
                                }
                                return result;
                            }
                            result.AcceptedAttemptId = signed.Request.AttemptId;
                            result.FiscalQrCodeUrl = signed.Result.QrCode;
                            result.Response = new InvoiceResponse
                            {
                                InvoiceSignature = signed.Result.Signature,
                                QrCodeUrl = signed.Result.QrCode
                            };
                        }
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
                        else if (snapshot.Signing != null) submitted = providers.SendAfterSigning(invoiceId,
                            snapshot.Provider,snapshot,result.AcceptedAttemptId.Value,retryRejected);
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
                        if (snapshot.LegacyFiscalReplacement != null &&
                            result.ReadyToPrint)
                        {
                            ValidateLegacyFiscalReplacementResponse(
                                result.Response);
                        }
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
                    // A UI-selected printer may replace the destination of an
                    // unfinished first handoff without turning it into a reprint.
                    var printDestination = !string.IsNullOrWhiteSpace(reprintDestination)
                        ? reprintDestination
                        : snapshot.PrintDestination;
                    result.Print = printing.Print(lease, result.OfflineAttemptId ?? result.AcceptedAttemptId, printDestination,
                        snapshot.PrintFormat, () => render(snapshot, result.Response), reprintReason, result.OfflineAttemptId.HasValue);
                    project(invoiceId, result);
                }
                return result;
            }
        }

        private bool IsAutomaticSamtecAmountCorrection(IFiscalDocumentLease lease,
            InvoiceWorkflowSnapshot snapshot, FiscalAttemptEvidence previousSigning)
        {
            if (previousSigning == null || snapshot?.Signing == null ||
                previousSigning.State != FiscalSigningState.Rejected.ToString() ||
                snapshot.Signing.Scope.SignerKey != SamtecFiscalSigner.Key ||
                !IsSamtecAmountMismatch(previousSigning))
                return false;

            // One automatic regenerated attempt is the absolute limit. This also
            // prevents a changing print rendering or another non-fiscal payload
            // detail from causing an endless retry cycle after the corrected
            // request itself has been rejected by the device.
            if (history.Load(lease,FiscalJournalFamily.Signing)
                .Count(evidence => evidence.State == FiscalSigningState.Rejected.ToString() &&
                    IsSamtecAmountMismatch(evidence)) != 1)
                return false;

            foreach (var family in new[] { FiscalJournalFamily.Submission,
                FiscalJournalFamily.ProviderSession, FiscalJournalFamily.Payment,
                FiscalJournalFamily.Cancellation, FiscalJournalFamily.Print,
                FiscalJournalFamily.MyDiesel })
                if (history.Load(lease,family).Any()) return false;
            try
            {
                // Integrated EFTPOS may have external payment consequences. Those
                // cases remain operator-controlled even when the device reports 89.
                var request = Newtonsoft.Json.Linq.JObject.Parse(
                    Encoding.UTF8.GetString(previousSigning.RequestPayload));
                var payments = request["Doc"]?["Payments"] as Newtonsoft.Json.Linq.JArray;
                return payments == null || !payments.Any(payment => (int?)payment["Type"] == 1);
            }
            catch
            {
                return false;
            }
        }

        private static bool IsSamtecAmountMismatch(FiscalAttemptEvidence evidence)
        {
            try
            {
                var outcome = Newtonsoft.Json.Linq.JObject.Parse(evidence?.SnapshotJson ?? "");
                string response = (string)outcome["Response"] ?? "";
                return response.IndexOf("89/D0/02",StringComparison.OrdinalIgnoreCase) >= 0 ||
                    response.IndexOf("Amounts of JSON transaction line does not match",
                        StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch
            {
                return false;
            }
        }
        private static string ReadReprintDestination(ref string reason)
        {
            if (string.IsNullOrWhiteSpace(reason) || !reason.StartsWith(ReprintRequestPrefix,StringComparison.Ordinal))
                return null;
            try
            {
                var request = Newtonsoft.Json.Linq.JObject.Parse(reason.Substring(ReprintRequestPrefix.Length));
                var destination = (string)request["PrintDestination"];
                var auditReason = (string)request["Reason"];
                if (string.IsNullOrWhiteSpace(destination) || string.IsNullOrWhiteSpace(auditReason))
                    throw new InvalidOperationException("Incomplete reprint routing request.");
                reason = auditReason;
                return destination;
            }
            catch (Newtonsoft.Json.JsonException ex)
            {
                throw new InvalidOperationException("Invalid reprint routing request.",ex);
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
            if (value.LegacyFiscalReplacement != null)
            {
                bool supportedProvider = value.Provider != null &&
                    (value.Provider.ProviderKey ==
                        InvoiceProviderCatalog.ITimologiaKey ||
                     value.Provider.ProviderKey ==
                        InvoiceProviderCatalog.DirectIlydaKey);
                bool hasPreviousEvidence = !string.IsNullOrWhiteSpace(
                        value.LegacyFiscalReplacement.PreviousInvoiceSignature) ||
                    !string.IsNullOrWhiteSpace(
                        value.LegacyFiscalReplacement.PreviousQrCode);
                if (!supportedProvider || value.LocalOnly || value.Signing != null ||
                    !hasPreviousEvidence || string.IsNullOrWhiteSpace(
                        value.LegacyFiscalReplacement.AuthorizedBy) ||
                    value.LegacyFiscalReplacement.AuthorizedUtc == default(DateTime) ||
                    string.IsNullOrWhiteSpace(
                        value.LegacyFiscalReplacement.Reason))
                {
                    throw new InvalidOperationException(
                        "Invalid legacy fiscal-evidence replacement audit.");
                }
            }
            if (value.Signing != null && (value.Signing.Document.Kind != FiscalDocumentKind.Invoice || value.Signing.Document.DocumentId != id)) throw new InvalidOperationException("Signing document mismatch.");
            if (value.Payment != null && (value.Signing != null || JsonConvert.SerializeObject(value.Payment.Provider) != JsonConvert.SerializeObject(value.Provider))) throw new InvalidOperationException("Payment route mismatch or unsupported device/POS combination.");
            if (value.Provider != null && validateInvoice)
            {
                if (value.Invoice?.Invoice?.InvoiceId != id || !value.Invoice.RequireIssuedIdentity) throw new InvalidOperationException("Issued invoice identity required.");
                var model = new InvoiceBuilder(new InvoiceTypeResolver()).Build(value.Invoice);
                if (model.Issuer.VATNumber != value.Provider.IssuerVat || model.Issuer.Branch != value.Provider.IssuerBranch) throw new InvalidOperationException("Issuer differs from workflow route.");
                new InvoiceAdapterFactory(new InvoiceTypeResolver()).CreatePayloadMapper(value.Provider.ProviderKey).Serialize(model, id);
                bool pos = (model.PaymentMethods ?? new PaymentMethod[0]).Any(p => (int)p.Type == 7 || (int)p.Type == 8);
                bool integratedSamtecPos = pos && value.Payment == null && value.Signing != null &&
                    value.Provider.ProviderKey == InvoiceProviderCatalog.MyDataKey && value.Signing.Scope != null &&
                    value.Signing.Scope.SignerKey == SamtecFiscalSigner.Key;
                InvoiceSubmissionReceiverChange receiverChange =
                    value.SubmissionReceiverChanges?.LastOrDefault();
                bool reusedApprovedPayment = pos && value.Payment == null &&
                    receiverChange?.ReusedApprovedPayment == true;
                if (pos != (value.Payment != null) && !integratedSamtecPos &&
                    !reusedApprovedPayment)
                    throw new InvalidOperationException("Explicit POS route required exactly for card/IRIS invoices unless Samtec performs the integrated POS transaction.");
                if (value.Signing != null && (value.Provider.ProviderKey != InvoiceProviderCatalog.MyDataKey ||
                    value.Signing.Scope.IssuerVat != value.Provider.IssuerVat || value.Signing.Scope.IssuerBranch != value.Provider.IssuerBranch ||
                    (value.Signing.Scope.Environment == FiscalEnvironment.Test) != value.Provider.TestMode)) throw new InvalidOperationException("Only matching Samtec/direct-myDATA composition is supported.");
            }
        }

        private static void ValidateLegacyFiscalReplacementResponse(
            InvoiceResponse response)
        {
            if (response == null ||
                string.IsNullOrWhiteSpace(response.InvoiceSignature) ||
                string.IsNullOrWhiteSpace(response.QrCodeUrl))
            {
                throw new InvalidOperationException(
                    "Ο πάροχος αποδέχθηκε το παραστατικό χωρίς πλήρη υπογραφή και QR. " +
                    "Η παλιά σήμανση δεν αντικαταστάθηκε· απαιτείται ανάκτηση στοιχείων από τον πάροχο.");
            }
        }

        private static InvoiceWorkflowSnapshot MergeRegeneratedSigning(InvoiceWorkflowSnapshot saved,
            InvoiceWorkflowSnapshot regenerated, Guid invoiceId)
        {
            Validate(regenerated,invoiceId);
            if (saved == null)
            {
                throw new InvalidOperationException(
                    "The rejected signing workflow is unavailable.");
            }

            var recoveredDispatch = saved.Invoice?.Delivery?.Departure;
            var dispatchChanges = saved.DispatchDateChanges;
            var paymentMethodChanges = saved.PaymentMethodChanges;
            var issueDateReissues = saved.IssueDateReissues;
            var foreignPartyCorrections = saved.ForeignPartyCorrections;
            var receiverChanges = saved.SubmissionReceiverChanges;
            if (dispatchChanges != null && dispatchChanges.Count > 0 &&
                recoveredDispatch.HasValue && regenerated.Invoice?.Delivery != null)
            {
                regenerated.Invoice.Delivery.Departure = recoveredDispatch.Value;
            }

            regenerated.PaymentMethodChanges = paymentMethodChanges;
            regenerated.DispatchDateChanges = dispatchChanges;
            regenerated.IssueDateReissues = issueDateReissues;
            regenerated.ForeignPartyCorrections = foreignPartyCorrections;
            regenerated.SubmissionReceiverChanges = receiverChanges;
            Validate(regenerated,invoiceId);
            return regenerated;
        }

        private static InvoiceWorkflowSnapshot MergeRegeneratedSubmission(
            InvoiceWorkflowSnapshot saved, InvoiceWorkflowSnapshot regenerated,
            Guid invoiceId, FiscalAttemptEvidence rejectedSubmission,
            bool hasSignedFiscalRequest, bool hasApprovedPayment)
        {
            Validate(regenerated, invoiceId, false);
            bool receiverChanged = saved?.Provider != null &&
                regenerated?.Provider != null &&
                !SameProviderIdentity(saved.Provider, regenerated.Provider);
            if (saved == null || saved.Provider == null || regenerated.Provider == null)
            {
                throw new InvalidOperationException(
                    "A provider resend requires a currently configured electronic receiver.");
            }

            if (regenerated.LocalOnly)
            {
                throw new InvalidOperationException(
                    "A provider resend requires an electronic submission route.");
            }

            var invoiceTypeResolver = new InvoiceTypeResolver();
            var invoiceBuilder = new InvoiceBuilder(invoiceTypeResolver);
            InvoiceModel savedModel = invoiceBuilder.Build(saved.Invoice);
            InvoiceBuildRequest effectiveInvoice = hasSignedFiscalRequest
                ? MergeSignedInvoiceForSubmission(saved.Invoice,
                    regenerated.Invoice)
                : regenerated.Invoice;
            InvoiceModel regeneratedModel = invoiceBuilder.Build(effectiveInvoice);
            ValidateAuthorizedContinuation(saved, savedModel,
                regeneratedModel, invoiceId, rejectedSubmission);
            List<InvoiceForeignPartyCorrection> foreignPartyCorrections =
                saved.ForeignPartyCorrections;
            string previousCountry = savedModel.CounterPart?.Country;
            string country = regeneratedModel.CounterPart?.Country;
            string previousInvoiceType = savedModel.InvoiceHeader.InvoiceType;
            string invoiceType = regeneratedModel.InvoiceHeader.InvoiceType;
            bool changedForeignIdentity = !string.Equals(previousCountry, country,
                    StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(previousInvoiceType, invoiceType,
                    StringComparison.Ordinal);
            if (changedForeignIdentity)
            {
                foreignPartyCorrections = foreignPartyCorrections ??
                    new List<InvoiceForeignPartyCorrection>();
                foreignPartyCorrections.Add(new InvoiceForeignPartyCorrection
                {
                    VatNumber = regeneratedModel.CounterPart.VATNumber,
                    PreviousCountry = previousCountry,
                    Country = country,
                    PreviousInvoiceType = previousInvoiceType,
                    InvoiceType = invoiceType,
                    ChangedUtc = DateTime.UtcNow,
                    Reason = "User-requested resend regenerated the current invoice data."
                });
            }

            var recoveredDispatch = saved.Invoice?.Delivery?.Departure;
            var dispatchChanges = saved.DispatchDateChanges;
            var receiverChanges = saved.SubmissionReceiverChanges;
            saved.Version = Math.Max(saved.Version, regenerated.Version);
            if (receiverChanged || hasApprovedPayment)
            {
                receiverChanges = receiverChanges ??
                    new List<InvoiceSubmissionReceiverChange>();
                receiverChanges.Add(new InvoiceSubmissionReceiverChange
                {
                    PreviousAttemptId = rejectedSubmission.AttemptId,
                    PreviousProviderKey = saved.Provider.ProviderKey,
                    ProviderKey = regenerated.Provider.ProviderKey,
                    PreviousAccount = saved.Provider.Account,
                    Account = regenerated.Provider.Account,
                    PreviousTestMode = saved.Provider.TestMode,
                    TestMode = regenerated.Provider.TestMode,
                    ReusedApprovedPayment = hasApprovedPayment,
                    ChangedUtc = DateTime.UtcNow,
                    Reason = "User-requested resend through the currently configured receiver."
                });
            }

            saved.Provider = regenerated.Provider;
            saved.Invoice = effectiveInvoice;
            if (!hasSignedFiscalRequest)
            {
                saved.LocalOnly = regenerated.LocalOnly;
                saved.Payment = hasApprovedPayment ? null : regenerated.Payment;
                saved.Signing = hasApprovedPayment ? null : regenerated.Signing;
                saved.PrintDestination = regenerated.PrintDestination;
                saved.PrintContent = regenerated.PrintContent;
                saved.PrintFormat = regenerated.PrintFormat;
                saved.CompanyQrContent = regenerated.CompanyQrContent;
                saved.MyDieselCouponCaseId = regenerated.MyDieselCouponCaseId;
                saved.MyDieselScope = regenerated.MyDieselScope;
                saved.MyDieselExciseDiscountPerLitre =
                    regenerated.MyDieselExciseDiscountPerLitre;
            }

            if (dispatchChanges != null && dispatchChanges.Count > 0 &&
                recoveredDispatch.HasValue && saved.Invoice?.Delivery != null)
            {
                saved.Invoice.Delivery.Departure = recoveredDispatch.Value;
            }

            saved.DispatchDateChanges = dispatchChanges;
            saved.ForeignPartyCorrections = foreignPartyCorrections;
            saved.SubmissionReceiverChanges = receiverChanges;
            Validate(saved, invoiceId);
            return saved;
        }

        private static InvoiceBuildRequest MergeSignedInvoiceForSubmission(
            InvoiceBuildRequest saved, InvoiceBuildRequest regenerated)
        {
            if (saved?.Invoice == null || regenerated?.Invoice == null)
            {
                throw new InvalidOperationException(
                    "The signed invoice snapshot is incomplete.");
            }

            InvoiceInput savedInvoice = saved.Invoice;
            InvoiceInput regeneratedInvoice = regenerated.Invoice;
            bool traderChanged = savedInvoice.TraderId != regeneratedInvoice.TraderId;
            bool vehicleChanged = savedInvoice.VehicleId != regeneratedInvoice.VehicleId;
            regenerated.Company = saved.Company;
            regenerated.RequireIssuedIdentity = saved.RequireIssuedIdentity;
            regeneratedInvoice.InvoiceId = savedInvoice.InvoiceId;
            regeneratedInvoice.TraderId = savedInvoice.TraderId;
            regeneratedInvoice.VehicleId = savedInvoice.VehicleId;
            if (traderChanged)
            {
                regeneratedInvoice.Trader = savedInvoice.Trader;
                regenerated.Country = saved.Country;
            }
            if (vehicleChanged)
            {
                if (saved.Delivery != null && regenerated.Delivery != null)
                {
                    regenerated.Delivery.Vehicle = saved.Delivery.Vehicle;
                }
                if (saved.Movement != null && regenerated.Movement != null)
                {
                    regenerated.Movement.Vehicle = saved.Movement.Vehicle;
                }
            }
            regeneratedInvoice.InvoiceSeries = savedInvoice.InvoiceSeries;
            regeneratedInvoice.Number = savedInvoice.Number;
            regeneratedInvoice.PrintedDateTime = savedInvoice.PrintedDateTime;
            regeneratedInvoice.Series = savedInvoice.Series;
            regeneratedInvoice.VATPaymentSuspension =
                savedInvoice.VATPaymentSuspension;
            regeneratedInvoice.InvoiceLines = savedInvoice.InvoiceLines;
            regeneratedInvoice.NettoAmount = savedInvoice.NettoAmount;
            regeneratedInvoice.DiscountAmount = savedInvoice.DiscountAmount;
            regeneratedInvoice.VATAmount = savedInvoice.VATAmount;
            regeneratedInvoice.TotalAmount = savedInvoice.TotalAmount;
            regeneratedInvoice.PaymentType = savedInvoice.PaymentType;
            return regenerated;
        }

        public static InvoiceContinuationDecision AssessContinuation(
            ProviderRoute route, InvoiceModel previousModel,
            InvoiceModel currentModel, Guid invoiceId, string previousState,
            bool userConsented, out InvoicePayloadComparison comparison)
        {
            if (route == null)
            {
                throw new ArgumentNullException(nameof(route));
            }
            if (previousModel == null)
            {
                throw new ArgumentNullException(nameof(previousModel));
            }
            if (currentModel == null)
            {
                throw new ArgumentNullException(nameof(currentModel));
            }

            var resolver = new InvoiceTypeResolver();
            var factory = new InvoiceAdapterFactory(resolver);
            IInvoicePayloadMapper mapper = factory.CreatePayloadMapper(
                route.ProviderKey);
            string previousPayload = mapper.Serialize(previousModel,
                invoiceId);
            string currentPayload = mapper.Serialize(currentModel,
                invoiceId);
            string payloadFormat = route.ProviderKey ==
                InvoiceProviderCatalog.MyDataKey
                    ? "application/xml"
                    : "application/json";
            var comparer = new SemanticInvoicePayloadComparer();
            comparison = comparer.Compare(
                previousPayload, currentPayload, payloadFormat);
            InvoiceProviderDefinition provider = InvoiceProviderCatalog.Get(
                route.ProviderKey);
            var request = new InvoiceContinuationRequest
            {
                Provider = provider,
                PreviousState = previousState,
                Existence = InvoiceExistenceState.NotChecked,
                Payload = comparison,
                UserConsented = userConsented
            };
            var guard = new InvoiceContinuationGuard();
            return guard.Evaluate(request);
        }

        private static void ValidateAuthorizedContinuation(
            InvoiceWorkflowSnapshot saved, InvoiceModel savedModel,
            InvoiceModel regeneratedModel, Guid invoiceId,
            FiscalAttemptEvidence previousSubmission)
        {
            if (previousSubmission == null ||
                previousSubmission.State != "RetryAuthorized")
            {
                return;
            }

            JObject evidence = JObject.Parse(
                previousSubmission.SnapshotJson);
            string previousState = (string)evidence[
                "OperatorResendAuthorization"]?["PreviousState"];
            if (string.IsNullOrWhiteSpace(previousState))
            {
                previousState = previousSubmission.State;
            }
            InvoicePayloadComparison comparison;
            InvoiceContinuationDecision decision = AssessContinuation(
                saved.Provider, savedModel, regeneratedModel, invoiceId,
                previousState, true, out comparison);
            if (decision.Action == InvoiceContinuationAction.Resend ||
                decision.Action ==
                    InvoiceContinuationAction.ContinueExisting)
            {
                return;
            }

            string changedPaths = comparison.ChangedPaths == null ||
                comparison.ChangedPaths.Length == 0
                    ? string.Empty
                    : " Changed values: " + string.Join(", ",
                        comparison.ChangedPaths) + ".";
            throw new InvoiceContinuationBlockedException(decision.Reason +
                changedPaths +
                " If recovery confirms the original invoice, cancel its MARK " +
                "for direct myDATA or select a cancellation invoice type for " +
                "the invoice provider, then issue the replacement.");
        }

        private static bool SameProviderIdentity(ProviderRoute left, ProviderRoute right)
        {
            if (left == null || right == null) return left == null && right == null;
            return left.ProviderKey == right.ProviderKey && left.TestMode == right.TestMode &&
                left.IssuerVat == right.IssuerVat && left.IssuerBranch == right.IssuerBranch;
        }
    }
}
