using System;

namespace Exedron.Invoicing.Core
{
    // The host holds one durable invoice lease for this entire synchronous operation.
    // Authorize and invoice submission are independent; no automatic void on issue errors.
    public sealed class ProviderPaymentCoordinator
    {
        private readonly IProviderPaymentWorkflowStore store;
        public ProviderPaymentCoordinator(IProviderPaymentWorkflowStore store)
        { this.store = store ?? throw new ArgumentNullException(nameof(store)); }

        public ProviderPaymentAttempt Authorize(ProviderPaymentAttemptRequest request, IPaymentGateway gateway,
            Func<ProviderPaymentSignature> prepareSignature, Func<ProviderPaymentSignature, string> gatewaySignature = null,
            bool retryRejected = false, bool recoveryOnly = false, Action<ProviderPaymentAttempt> beforeSignature = null)
        {
            var attempt = store.LoadLatest(request.InvoiceId);
            if (attempt != null && (attempt.State == "Declined" || attempt.State == "Failed" || attempt.State == "Voided") && !retryRejected)
                return attempt;
            if (attempt == null && recoveryOnly) throw new InvalidOperationException("No payment exists to recover.");
            if (attempt != null && attempt.State == "Voided") throw new InvalidOperationException("A voided payment cannot be charged again by invoice retry.");
            attempt = store.GetOrCreate(request); // Immutable scope, amount, terminal and host snapshot.
            if (attempt.State == "Approved" || attempt.State == "Invoiced" || attempt.State == "Mismatch" ||
                attempt.State == "VoidPending" || attempt.State == "VoidFailed" || attempt.State == "SignaturePending") return attempt;
            if (gateway == null || gateway.TerminalId != request.TerminalId || !string.IsNullOrWhiteSpace(gateway.ValidateConfiguration()))
                throw new InvalidOperationException("The historical payment gateway configuration is unavailable or mismatched.");
            if (attempt.State == "Prepared" || attempt.State == "SignatureReady" && !recoveryOnly)
            {
                if (recoveryOnly) return attempt;
                // Cancellation has its own durable provider state. Keep Prepared until it completes.
                if (attempt.State == "Prepared") beforeSignature?.Invoke(attempt);
                // Interrupted signature creation cannot silently create another provider session.
                store.MarkSignaturePending(attempt.Id);
                ProviderPaymentSignature signature;
                try { signature = prepareSignature(); }
                catch (PaymentSignatureRejectedException ex)
                {
                    store.MarkFailure(attempt.Id, "Failed", "SignatureRequestRejected", ex.Message);
                    return store.LoadLatest(request.InvoiceId);
                }
                store.SaveSignature(attempt.Id, signature);
                attempt = store.LoadLatest(request.InvoiceId);
            }
            bool recovering = attempt.State == "Pending" || attempt.State == "Unknown";
            if (!recovering && (attempt.State != "SignatureReady" || recoveryOnly)) return attempt;
            var savedSignature = new ProviderPaymentSignature { ProviderUid = attempt.ProviderUid,
                ProviderInput = attempt.ProviderInput, ProviderSignature = attempt.ProviderSignature };
            // Adapter-specific encoding/expiry checks happen BEFORE Pending, never on lookup.
            string signatureText = recovering || gatewaySignature == null ? savedSignature.ProviderSignature : gatewaySignature(savedSignature);
            store.MarkPending(attempt.Id);
            PaymentAuthorizationResult result;
            try
            {
                result = gateway.Authorize(new PaymentAuthorizationRequest { TransactionUid = attempt.TransactionUid,
                    Operation = attempt.Operation, PaymentType = attempt.PaymentType, ProviderId = attempt.ProviderId,
                    ProviderInput = attempt.ProviderInput, ProviderSignature = signatureText, ExpectedTerminalId = attempt.TerminalId,
                    ExpectedAmountMinorUnits = attempt.AmountMinorUnits, RecoverOnly = recovering });
            }
            catch (GBridgeException ex)
            {
                var state = ex.FailureKind == GBridgeFailureKind.Mismatch ? "Mismatch" :
                    ex.FailureKind == GBridgeFailureKind.Declined || ex.FailureKind == GBridgeFailureKind.SignatureRejected ||
                    ex.FailureKind == GBridgeFailureKind.CredentialsRejected ? "Declined" : "Unknown";
                store.MarkFailure(attempt.Id, state, ex.FailureKind.ToString(), ex.FailureKind == GBridgeFailureKind.IncompleteResponse
                    ? FailureMessage(ex.FailureKind) + " " + ex.Message : FailureMessage(ex.FailureKind));
                return store.LoadLatest(request.InvoiceId);
            }
            catch (Exception ex)
            {
                store.MarkFailure(attempt.Id, "Unknown", ex.GetType().Name, "Payment outcome could not be confirmed.");
                return store.LoadLatest(request.InvoiceId);
            }
            // A save failure must propagate. Pending then permits lookup only, never another charge.
            store.MarkApproved(attempt.Id, result);
            return store.LoadLatest(request.InvoiceId);
        }

        // Only controlled messages reach the UI; raw gateway bodies stay in the evidence journal.
        public static string FailureMessage(GBridgeFailureKind kind)
        {
            switch (kind)
            {
                case GBridgeFailureKind.CredentialsRejected:
                    return "Το GBridge απέρριψε τα διαπιστευτήρια NSP (401). Διορθώστε τα με την υποστήριξη POS και επιλέξτε Συνέχιση έκδοσης για νέα πληρωμή.";
                case GBridgeFailureKind.SignatureRejected:
                    return "Το POS απέρριψε την υπογραφή παρόχου. Ελέγξτε τις ρυθμίσεις POS / υπογραφής και επιλέξτε Συνέχιση έκδοσης για νέα προσπάθεια.";
                case GBridgeFailureKind.Declined:
                    return "Το POS επιβεβαίωσε απόρριψη της πληρωμής. Η Συνέχιση έκδοσης ξεκινά νέα προσπάθεια πληρωμής.";
                case GBridgeFailureKind.Mismatch:
                    return "Η απάντηση POS δεν συμφωνεί με την αρχική συναλλαγή. Απαιτείται έλεγχος στο POS πριν από συνέχεια.";
                case GBridgeFailureKind.IncompleteResponse:
                    return "Ελλιπής απάντηση POS. Επιλέξτε Ανάκτηση κατάστασης για έλεγχο της ίδιας συναλλαγής, χωρίς νέα χρέωση.";
                default:
                    return "Η έκβαση της πληρωμής δεν επιβεβαιώθηκε. Επιλέξτε Ανάκτηση κατάστασης για έλεγχο της ίδιας συναλλαγής.";
            }
        }
    }
}
