using System;
using System.Linq;
using System.Net.Http;
using ASFuelControl.Invoicing.Persistence;
using Exedron.Invoicing.Core;
using Exedron.Invoicing.Core.Models;
using Exedron.Invoicing.Adapters;
using Newtonsoft.Json;

namespace ASFuelControl.Invoicing
{
    public sealed class ProviderPaymentResult
    {
        public ProviderPaymentAttempt Payment { get; internal set; }
        public ProviderSubmissionResult Submission { get; internal set; }
    }
    public sealed partial class ProviderPaymentService
    {
        private readonly SqlFiscalDatabase database;
        private readonly SqlFiscalHistoryStore history;
        private readonly ProviderSubmissionService providers;
        private readonly Func<PaymentRoute, HttpMessageHandler> transport;
        private readonly Func<PaymentRoute, PaymentRoute> currentPaymentRoute;
        public ProviderPaymentService(SqlFiscalDatabase database, IProviderCredentialsResolver credentials,
            Func<ProviderRoute, HttpMessageHandler> providerTransport = null, Func<PaymentRoute, HttpMessageHandler> gatewayTransport = null,
            Func<PaymentRoute, PaymentRoute> currentPaymentRoute = null)
        {
            this.database = database ?? throw new ArgumentNullException(nameof(database));
            history = new SqlFiscalHistoryStore(database);
            providers = new ProviderSubmissionService(database, credentials, providerTransport);
            transport = gatewayTransport;
            this.currentPaymentRoute = currentPaymentRoute;
        }
        public ProviderPaymentResult Send(Guid invoiceId, PaymentRoute route, IInvoiceSubmissionSource source,
            bool retryDeclined = false, bool retryRejectedInvoice = false)
        { return Run(invoiceId, route, source, retryDeclined, retryRejectedInvoice, false); }
        public ProviderPaymentResult Recover(Guid invoiceId) { return Run(invoiceId, null, null, false, false, true); }
        // The UI supplies the attempt it displayed. A stale/double click cannot retry a newer attempt.
        public ProviderPaymentResult RetryPayment(Guid invoiceId, Guid expectedAttemptId)
        { return Run(invoiceId, null, null, false, false, false, retryPaymentAttemptId: expectedAttemptId); }
        public Guid? GetRetryablePaymentAttemptId(Guid invoiceId)
        {
            using (var lease = database.EnterInvoice(invoiceId))
            {
                var saved = history.Load(lease, FiscalJournalFamily.Payment).LastOrDefault();
                if (saved?.Operation != "Payment" || saved.PayloadFormat != "provider-payment.v2.json") return null;
                var request = JsonConvert.DeserializeObject<ProviderPaymentAttemptRequest>(System.Text.Encoding.UTF8.GetString(saved.RequestPayload));
                return new SqlProviderPaymentStore(database, saved.Scope).GetOperatorRetryCandidate(request, QualifiesForOperatorRetry)?.Id;
            }
        }
        private static bool QualifiesForOperatorRetry(ProviderPaymentAttempt attempt, PaymentGatewayEvent[] events)
        { return GBridgeClient.CanRetryAfterF4(attempt.TransactionUid, attempt.TerminalId, events,
            attempt.ProviderInput, (int)attempt.Operation, attempt.PaymentType, attempt.ProviderId); }
        private static ProviderPaymentResult AwaitOperatorRetry(ProviderPaymentAttempt payment)
        {
            payment.ErrorMessage = "Η πληρωμή δεν ολοκληρώθηκε. Ελέγξτε το POS και επιλέξτε Επανάληψη πληρωμής για νέα προσπάθεια.";
            return new ProviderPaymentResult { Payment = payment };
        }

        private ProviderPaymentResult Run(Guid invoiceId, PaymentRoute route, IInvoiceSubmissionSource source,
            bool retryDeclined, bool retryRejectedInvoice, bool recoverOnly, Guid? originalInvoiceId = null, Guid? originalAttemptId = null,
            Guid? retryPaymentAttemptId = null)
        {
            using (var lease = database.EnterInvoice(invoiceId))
            {
                var records = history.Load(lease, FiscalJournalFamily.Payment);
                if (records.Any(a => a.Operation != "Payment")) throw new InvalidOperationException("A related payment operation requires reconciliation before invoice processing.");
                var saved = records.LastOrDefault();
                if (retryPaymentAttemptId.HasValue && saved?.AttemptId != retryPaymentAttemptId.Value)
                    throw new InvalidOperationException("Η προσπάθεια πληρωμής έχει αλλάξει. Ανανεώστε την κατάσταση πριν από επανάληψη.");
                ProviderPaymentAttemptRequest request;
                PaymentSnapshot snapshot;
                if (saved != null)
                {
                    if (saved.PayloadFormat != "provider-payment.v2.json") throw new InvalidOperationException("Legacy payment history requires reconciliation.");
                    request = JsonConvert.DeserializeObject<ProviderPaymentAttemptRequest>(System.Text.Encoding.UTF8.GetString(saved.RequestPayload));
                    if (string.IsNullOrWhiteSpace(request.WorkflowContext)) throw new InvalidOperationException("Historical payment snapshot is missing.");
                    snapshot = JsonConvert.DeserializeObject<PaymentSnapshot>(request.WorkflowContext);
                    if (snapshot?.Version != 1 || snapshot.InvoiceId != invoiceId || snapshot.Route == null || snapshot.Invoice == null)
                        throw new InvalidOperationException("Invalid historical payment snapshot.");
                    route = snapshot.Route; // Preserve historical routing unless an eligible explicit continuation is reconfigured below.
                    if (request.OriginalInvoiceId != originalInvoiceId || request.OriginalPaymentAttemptId != originalAttemptId)
                        throw new InvalidOperationException("Use the original refund workflow and payment reference to continue this transaction.");
                    if (saved.Scope != JsonConvert.SerializeObject(route)) throw new InvalidOperationException("Payment scope differs from the saved snapshot.");
                }
                else
                {
                    if (recoverOnly) throw new InvalidOperationException("No payment exists to recover.");
                    if (route == null || source == null) throw new ArgumentException("A route and saved invoice source are required.");
                    foreach (var family in new[] { FiscalJournalFamily.Signing, FiscalJournalFamily.Submission, FiscalJournalFamily.ProviderSession,
                        FiscalJournalFamily.Cancellation, FiscalJournalFamily.Print })
                        if (history.Load(lease, family).Any()) throw new InvalidOperationException("Existing " + family + " history requires reconciliation.");
                    var input = source.Prepare(invoiceId, route.Provider);
                    if (input?.Invoice?.InvoiceId != invoiceId || !input.RequireIssuedIdentity) throw new InvalidOperationException("A saved issued invoice is required.");
                    var invoice = new InvoiceBuilder(new InvoiceTypeResolver()).Build(input);
                    Validate(invoice, route, originalInvoiceId.HasValue);
                    new InvoiceAdapterFactory(new InvoiceTypeResolver()).CreatePayloadMapper(route.Provider.ProviderKey).Serialize(invoice, invoiceId);
                    snapshot = new PaymentSnapshot { Version = 1, InvoiceId = invoiceId, Route = route, Invoice = invoice };
                    var payment = invoice.PaymentMethods.Single();
                    request = new ProviderPaymentAttemptRequest { InvoiceId = invoiceId, Gateway = "GBridge", InvoiceProvider = route.Provider.ProviderKey,
                        ProviderId = InvoiceProviderCatalog.Get(route.Provider.ProviderKey).AadeProviderId.Value,
                        PaymentType = (int)payment.Type == 8 ? 1 : 0, Operation = originalInvoiceId.HasValue ? ProviderPaymentOperation.Refund : ProviderPaymentOperation.Sale,
                        OriginalInvoiceId = originalInvoiceId, OriginalPaymentAttemptId = originalAttemptId,
                        TerminalId = route.TerminalId, Amount = payment.Amount, AmountMinorUnits = checked((int)(payment.Amount * 100)),
                        WorkflowContext = JsonConvert.SerializeObject(snapshot) };
                }
                foreach (var family in new[] { FiscalJournalFamily.Signing, FiscalJournalFamily.Cancellation, FiscalJournalFamily.Print })
                    if (history.Load(lease, family).Any()) throw new InvalidOperationException("Existing " + family + " history prevents payment workflow continuation.");
                var store = new SqlProviderPaymentStore(database, JsonConvert.SerializeObject(route));
                store.ReconcileRecordedOutcome(request, GBridgeClient.ReadRecordedOutcome);
                // Read under the already-held invoice lease, outside the store's SQL transaction.
                var signatureSession = route.Provider.ProviderKey == InvoiceProviderCatalog.DirectIlydaKey
                    ? new SqlIlydaPosStore(database).Load(invoiceId) : null;
                store.ReconcileSignatureRejection(request, events => {
                    if (route.Provider.ProviderKey == InvoiceProviderCatalog.DirectIlydaKey) {
                        return signatureSession?.State == "Rejected" && signatureSession.TestMode == route.Provider.TestMode && signatureSession.IssuerVat == route.Provider.IssuerVat;
                    }
                    var last = events.LastOrDefault();
                    return last != null && ITimologiaInvoiceProvider.IsSignatureRequestRejected(last.HttpStatus, last.ResponseJson);
                });
                var existingPayment = store.ReconcileRejectedAuthorization(request, (attempt, events) => {
                    if (GBridgeClient.HasOnlySignatureRejectedAuthorizations(attempt.TransactionUid, events, attempt.ProviderId))
                        return GBridgeFailureKind.SignatureRejected;
                    return GBridgeClient.HasOnlyRejectedAuthorizations(attempt.TransactionUid, events, attempt.ProviderId)
                        ? (GBridgeFailureKind?)GBridgeFailureKind.CredentialsRejected : null;
                });
                var submission = history.Load(lease, FiscalJournalFamily.Submission).LastOrDefault();
                if (submission != null && existingPayment?.State != "Approved" && existingPayment?.State != "Invoiced")
                    throw new InvalidOperationException("Invoice history exists without a confirmed payment; no authorization is permitted.");
                if (retryPaymentAttemptId.HasValue)
                {
                    var prior = existingPayment;
                    existingPayment = store.TryCreateOperatorRetry(request, retryPaymentAttemptId.Value, QualifiesForOperatorRetry,
                        events => GBridgeClient.IsSingleF4(prior.TransactionUid, prior.TerminalId, events)
                            ? "GBridge.F4.v1" : "GBridge.F4-F2-0E.v1");
                    if (existingPayment == null)
                        throw new InvalidOperationException("Δεν επιτρέπεται επανάληψη αυτής της πληρωμής. Ανακτήστε την κατάσταση και ελέγξτε το POS.");
                }
                else if (!recoverOnly && existingPayment != null)
                {
                    // PrintAgent/manual issuance must not turn a qualifying F4 into another charge.
                    // Unsent replacements from interrupted or older builds also await an explicit click.
                    if (store.GetOperatorRetryCandidate(request, QualifiesForOperatorRetry) != null ||
                        (existingPayment.OperatorRetryOfAttemptId.HasValue || existingPayment.AutomaticRetryOfAttemptId.HasValue) &&
                        (existingPayment.State == "Prepared" || existingPayment.State == "SignatureReady"))
                        return AwaitOperatorRetry(existingPayment);
                }
                // Only an unsent attempt or an explicitly retried definitive failure may
                // use corrected signing settings. Pending/unknown/approved routes stay frozen.
                if (!recoverOnly && !retryPaymentAttemptId.HasValue && submission == null && currentPaymentRoute != null && existingPayment != null &&
                    (existingPayment.State == "Prepared" || existingPayment.State == "SignatureReady" ||
                     retryDeclined && (existingPayment.State == "Declined" || existingPayment.State == "Failed")))
                {
                    var configured = currentPaymentRoute(route);
                    if (configured == null || configured.Provider.ProviderKey != route.Provider.ProviderKey ||
                        configured.Provider.TestMode != route.Provider.TestMode || configured.Provider.IssuerVat != route.Provider.IssuerVat ||
                        configured.Provider.IssuerBranch != route.Provider.IssuerBranch || configured.TerminalId != route.TerminalId || configured.Endpoint != route.Endpoint)
                        throw new InvalidOperationException("Η διόρθωση ρυθμίσεων POS πρέπει να διατηρεί πάροχο, περιβάλλον, εκδότη και τερματικό.");
                    Validate(snapshot.Invoice, configured, originalInvoiceId.HasValue);
                    if (JsonConvert.SerializeObject(configured) != JsonConvert.SerializeObject(route))
                    {
                        using (var previous = providers.Open(route.Provider, ProviderSubmissionService.Context(route.Provider, snapshot.Invoice), invoiceId, route))
                            (previous as IInvoiceProviderPaymentLifecycle)?.CancelUnusedPaymentSignature(invoiceId, "TRANSACTION_CANCELLED");
                        if (existingPayment.State == "Prepared" || existingPayment.State == "SignatureReady")
                            store.MarkFailure(existingPayment.Id, "Failed", "SigningSettingsChanged", "Η μη απεσταλμένη προσπάθεια αντικαταστάθηκε με τις διορθωμένες ρυθμίσεις POS.");
                        route = configured; snapshot.Route = route;
                        request.WorkflowContext = JsonConvert.SerializeObject(snapshot);
                        store = new SqlProviderPaymentStore(database, JsonConvert.SerializeObject(route));
                        existingPayment = store.GetOrCreate(request);
                    }
                }
                using (var provider = providers.Open(route.Provider, ProviderSubmissionService.Context(route.Provider, snapshot.Invoice), invoiceId, route))
                using (var gateway = new GBridgeClient(new GBridgeOptions { Endpoint = route.Endpoint, TerminalId = route.TerminalId,
                    Logger = new ProviderSubmissionService.InvoiceLogger(new SqlEvidenceLogger(database), invoiceId),
                    EventSink = new SqlEvidenceLogger(database) }, transport?.Invoke(route)))
                {
                    var lifecycle = provider as IInvoiceProviderPaymentLifecycle;
                    if (!recoverOnly && existingPayment?.State == "Declined") lifecycle?.CancelUnusedPaymentSignature(invoiceId, "TRANSACTION_FAILED");
                    Func<bool, ProviderPaymentAttempt> authorize = onlyRecover => new ProviderPaymentCoordinator(store).Authorize(request, gateway,
                        () => lifecycle != null ? lifecycle.PreparePaymentSignature(snapshot.Invoice, invoiceId, true) :
                            ((ITimologiaInvoiceProvider)provider).CreatePaymentSignature(snapshot.Invoice, e => {
                                e.TransactionUid = store.LoadLatest(invoiceId).TransactionUid; new SqlEvidenceLogger(database).Record(e); }),
                        signature => provider is IlydaInvoiceProvider ? ((IlydaInvoiceProvider)provider).GBridgeSignature(invoiceId, signature.ProviderSignature) : signature.ProviderSignature,
                        retryDeclined, onlyRecover, attempt => {
                            if (attempt.RequiresSignatureRenewal) lifecycle?.CancelUnusedPaymentSignature(invoiceId, "TRANSACTION_CANCELLED");
                        });
                    var payment = authorize(recoverOnly);
                    if (!recoverOnly && payment.State == "Declined") lifecycle?.CancelUnusedPaymentSignature(invoiceId, "TRANSACTION_FAILED");
                    if (store.GetOperatorRetryCandidate(request, QualifiesForOperatorRetry) != null) return AwaitOperatorRetry(payment);
                    // The dedicated button performs payment only; issuance can continue using its durable approval.
                    if (retryPaymentAttemptId.HasValue) return new ProviderPaymentResult { Payment = payment };
                    if (payment.State != "Approved" && payment.State != "Invoiced") return new ProviderPaymentResult { Payment = payment };
                    InvoiceResponse response = null;
                    bool retryableSubmission = submission != null &&
                        (submission.State == "Rejected" || submission.State == "RetryAuthorized");
                    if (submission != null && (!retryableSubmission || !retryRejectedInvoice || recoverOnly))
                        response = ((IInvoiceProviderRecovery)provider).RecoverInvoice(invoiceId);
                    else if (!recoverOnly)
                        response = provider.SendInvoice(snapshot.Invoice, invoiceId, new ProviderPaymentContext {
                            SourcePayment = snapshot.Invoice.PaymentMethods.Single(), ProviderUid = payment.ProviderUid,
                            ProviderSignature = payment.ProviderSignature, TransactionId = payment.TransactionId, TerminalId = payment.ResponseTerminalId });
                    submission = history.Load(lease, FiscalJournalFamily.Submission).LastOrDefault();
                    var result = ProviderSubmissionService.Result(submission, response);
                    if (result.Accepted && payment.State != "Invoiced") { store.MarkInvoiceSubmitted(payment.Id); payment = store.LoadLatest(invoiceId); }
                    return new ProviderPaymentResult { Payment = payment, Submission = result };
                }
            }
        }
        private static void Validate(InvoiceModel invoice, PaymentRoute route, bool refund = false)
        {
            if (invoice.Issuer.VATNumber != route.Provider.IssuerVat || invoice.Issuer.Branch != route.Provider.IssuerBranch)
                throw new InvalidOperationException("Issuer/branch does not match payment routing.");
            var payments = invoice.PaymentMethods ?? new PaymentMethod[0];
            if (invoice.InvoiceHeader.Currency != "EUR" || payments.Length != 1 || ((int)payments[0].Type != 7 && (int)payments[0].Type != 8) ||
                payments[0].Amount <= 0 || payments[0].Amount != decimal.Round(payments[0].Amount, 2) || payments[0].Amount != invoice.InvoiceSummary.TotalGrossValue)
                throw new InvalidOperationException("One full, positive EUR card/IRIS payment with exact cents is required.");
            if (route.Provider.ProviderKey == InvoiceProviderCatalog.DirectIlydaKey && (int)payments[0].Type == 8)
                throw new InvalidOperationException("The supplied Ilyda contract does not support IRIS.");
            if ((int)payments[0].Type == 8 && (route.PosMode == null || !PosModeCatalog.Get(route.Provider.ProviderKey,route.PosMode).SupportsIris(route.Provider.ProviderKey)))
                throw new InvalidOperationException("IRIS μέσω GBridge υποστηρίζεται μόνο με iTimologia και τρόπο POS Mellon. Επιλέξτε επαληθευμένο τρόπο POS πριν από πληρωμή.");
            if (new[] { "5.1", "5.2", "11.4" }.Contains(invoice.InvoiceHeader.InvoiceType) != refund)
                throw new InvalidOperationException("A refund requires an explicit original accepted payment reference; invoice retry cannot infer it.");
        }
        private sealed class PaymentSnapshot
        {
            public int Version; public Guid InvoiceId; public PaymentRoute Route; public InvoiceModel Invoice;
        }
    }
}
