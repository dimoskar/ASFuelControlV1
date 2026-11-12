using System;

namespace Exedron.Invoicing.Core
{
    public enum InvoiceCancellationStrategy
    {
        None = 0,
        CancelMark = 1,
        IssueCancellationDocument = 2
    }

    public enum InvoicePayloadComparisonKind
    {
        Unknown = 0,
        Equivalent = 1,
        Changed = 2,
        IdentityChanged = 3
    }

    public enum InvoiceExistenceState
    {
        NotChecked = 0,
        NotFound = 1,
        Active = 2,
        Cancelled = 3
    }

    public enum InvoiceContinuationAction
    {
        Block = 0,
        Recover = 1,
        ContinueExisting = 2,
        Resend = 3,
        CancelAndReissue = 4
    }

    public sealed class InvoicePayloadComparison
    {
        public InvoicePayloadComparisonKind Kind { get; set; }
        public string[] ChangedPaths { get; set; }

        public static InvoicePayloadComparison Equivalent()
        {
            return new InvoicePayloadComparison
            {
                Kind = InvoicePayloadComparisonKind.Equivalent,
                ChangedPaths = new string[0]
            };
        }
    }

    public interface IInvoicePayloadComparer
    {
        InvoicePayloadComparison Compare(string previousPayload,
            string currentPayload, string payloadFormat);
    }

    public sealed class InvoiceContinuationRequest
    {
        public InvoiceProviderDefinition Provider { get; set; }
        public string PreviousState { get; set; }
        public InvoiceExistenceState Existence { get; set; }
        public InvoicePayloadComparison Payload { get; set; }
        public bool UserConsented { get; set; }
        public bool IdentityChangeAuthorized { get; set; }
        public Guid? CancellationInvoiceTypeId { get; set; }
    }

    public sealed class InvoiceContinuationDecision
    {
        public InvoiceContinuationAction Action { get; internal set; }
        public bool RequiresUserConsent { get; internal set; }
        public bool RequiresCancellationInvoiceType { get; internal set; }
        public InvoiceCancellationStrategy CancellationStrategy { get; internal set; }
        public string Reason { get; internal set; }
    }

    public interface IInvoiceContinuationGuard
    {
        InvoiceContinuationDecision Evaluate(InvoiceContinuationRequest request);
    }

    public sealed class InvoiceCancellationRequest
    {
        public Guid InvoiceId { get; set; }
        public Guid AcceptedAttemptId { get; set; }
        public long? Mark { get; set; }
        public Guid? CancellationInvoiceTypeId { get; set; }
        public string AuthorizedBy { get; set; }
        public string Reason { get; set; }
    }

    public sealed class InvoiceCancellationResult
    {
        public bool Accepted { get; set; }
        public bool Unknown { get; set; }
        public long? CancellationMark { get; set; }
        public Guid? CancellationInvoiceId { get; set; }
        public string Evidence { get; set; }
    }

    public interface IInvoiceCancellationHandler
    {
        InvoiceCancellationStrategy Strategy { get; }
        InvoiceCancellationResult Cancel(InvoiceCancellationRequest request);
    }

    public sealed class InvoiceContinuationGuard : IInvoiceContinuationGuard
    {
        public InvoiceContinuationDecision Evaluate(
            InvoiceContinuationRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Provider == null)
            {
                throw new ArgumentException("The invoice provider is required.",
                    nameof(request));
            }

            string state = (request.PreviousState ?? string.Empty).Trim();
            InvoicePayloadComparisonKind payloadKind = request.Payload == null
                ? InvoicePayloadComparisonKind.Unknown
                : request.Payload.Kind;

            if (state.Length == 0 || state == "FailedBeforeDispatch" ||
                request.Existence == InvoiceExistenceState.NotFound)
            {
                return Decision(InvoiceContinuationAction.Resend,
                    request.Provider.CancellationStrategy, false, false,
                    "Δεν επιβεβαιώθηκε ενεργό προηγούμενο παραστατικό.");
            }

            if (request.Existence == InvoiceExistenceState.Cancelled)
            {
                return Decision(InvoiceContinuationAction.Resend,
                    request.Provider.CancellationStrategy, false, false,
                    "Το προηγούμενο παραστατικό έχει ήδη ακυρωθεί.");
            }

            if (state == "Rejected")
            {
                if (payloadKind ==
                        InvoicePayloadComparisonKind.IdentityChanged &&
                    !request.IdentityChangeAuthorized)
                {
                    return Decision(InvoiceContinuationAction.Block,
                        request.Provider.CancellationStrategy, false, false,
                        "Η επανάληψη απορριφθείσας διαβίβασης πρέπει να διατηρεί τη φορολογική ταυτότητα του παραστατικού, εκτός αν έχει καταχωριστεί ελεγχόμενη διόρθωση.");
                }
                if (payloadKind == InvoicePayloadComparisonKind.Unknown)
                {
                    return Decision(InvoiceContinuationAction.Block,
                        request.Provider.CancellationStrategy, false, false,
                        "Δεν ήταν δυνατή η ασφαλής σύγκριση της ταυτότητας της απορριφθείσας διαβίβασης.");
                }

                return Decision(InvoiceContinuationAction.Resend,
                    request.Provider.CancellationStrategy, false, false,
                    "Η προηγούμενη διαβίβαση απορρίφθηκε οριστικά και η επανάληψη διατηρεί τη φορολογική της ταυτότητα.");
            }

            if (state == "ReconciliationRequired" ||
                payloadKind == InvoicePayloadComparisonKind.Unknown &&
                (state == "Unknown" || state == "Sending"))
            {
                return Decision(InvoiceContinuationAction.Recover,
                    request.Provider.CancellationStrategy, false, false,
                    "Ανακτήστε πρώτα την προηγούμενη προσπάθεια πριν αποφασίσετε αν θα επαναληφθεί ή θα ακυρωθεί.");
            }
            if (payloadKind == InvoicePayloadComparisonKind.Unknown)
            {
                return Decision(InvoiceContinuationAction.Recover,
                    request.Provider.CancellationStrategy, false, false,
                    "Δεν ήταν δυνατή η ασφαλής σύγκριση της προηγούμενης και της τρέχουσας διαβίβασης. Ανακτήστε την προηγούμενη προσπάθεια πριν συνεχίσετε.");
            }

            bool payloadChanged = payloadKind ==
                InvoicePayloadComparisonKind.Changed ||
                payloadKind == InvoicePayloadComparisonKind.IdentityChanged;
            bool activeInvoice = state == "Accepted" ||
                request.Existence == InvoiceExistenceState.Active;
            if (payloadChanged && !activeInvoice)
            {
                return Decision(InvoiceContinuationAction.Recover,
                    request.Provider.CancellationStrategy, false, false,
                    "Τα δεδομένα του παραστατικού άλλαξαν, αλλά το αποτέλεσμα της προηγούμενης διαβίβασης δεν έχει επιβεβαιωθεί. Εκτελέστε ανάκτηση πριν από ακύρωση ή επανέκδοση.");
            }

            if (payloadChanged)
            {
                if (request.Provider.CancellationStrategy ==
                    InvoiceCancellationStrategy.None)
                {
                    return Decision(InvoiceContinuationAction.Block,
                        request.Provider.CancellationStrategy, false, false,
                        "Το ενεργό παραστατικό έχει διαφορετικά δεδομένα και δεν έχει ρυθμιστεί τρόπος ακύρωσης για τον συγκεκριμένο πάροχο.");
                }
                bool needsType = request.Provider.CancellationStrategy ==
                    InvoiceCancellationStrategy.IssueCancellationDocument;
                if (!request.UserConsented)
                {
                    return Decision(InvoiceContinuationAction.Block,
                        request.Provider.CancellationStrategy, true, needsType,
                        "Το ενεργό παραστατικό έχει διαφορετικά δεδομένα. Απαιτείται επιβεβαίωση του χρήστη για την ακύρωση και την έκδοση αντικατάστασης.");
                }
                if (needsType &&
                    !request.CancellationInvoiceTypeId.HasValue)
                {
                    return Decision(InvoiceContinuationAction.Block,
                        request.Provider.CancellationStrategy, false, true,
                        "Επιλέξτε τύπο ακυρωτικού παραστατικού πριν από την ακύρωση του παραστατικού στον πάροχο.");
                }

                return Decision(InvoiceContinuationAction.CancelAndReissue,
                    request.Provider.CancellationStrategy, false, false,
                    "Ακυρώστε το ενεργό παραστατικό πριν από την έκδοση του παραστατικού με τα νέα δεδομένα.");
            }

            if (state == "Accepted" ||
                request.Existence == InvoiceExistenceState.Active)
            {
                return Decision(InvoiceContinuationAction.ContinueExisting,
                    request.Provider.CancellationStrategy, false, false,
                    "Το ενεργό παραστατικό περιέχει ήδη τα ίδια δεδομένα.");
            }

            if (state == "RetryAuthorized")
            {
                return Decision(InvoiceContinuationAction.Resend,
                    request.Provider.CancellationStrategy, false, false,
                    "Η επανάληψη έχει ήδη εγκριθεί για τα ίδια δεδομένα παραστατικού.");
            }

            if (state == "Unknown" || state == "Sending")
            {
                if (!request.UserConsented)
                {
                    return Decision(InvoiceContinuationAction.Block,
                        request.Provider.CancellationStrategy, true, false,
                        "Το αποτέλεσμα της προηγούμενης διαβίβασης παραμένει άγνωστο. Απαιτείται επιβεβαίωση του χρήστη πριν από την επανάληψη με τα ίδια δεδομένα.");
                }

                return Decision(InvoiceContinuationAction.Resend,
                    request.Provider.CancellationStrategy, false, false,
                    "Επαναλάβετε τη διαβίβαση των ίδιων δεδομένων, διατηρώντας τη σταθερή ταυτότητα του παραστατικού στον πάροχο.");
            }

            return Decision(InvoiceContinuationAction.Block,
                request.Provider.CancellationStrategy, false, false,
                "Η προηγούμενη κατάσταση δεν επιτρέπει περαιτέρω επεξεργασία.");
        }

        private static InvoiceContinuationDecision Decision(
            InvoiceContinuationAction action,
            InvoiceCancellationStrategy cancellationStrategy,
            bool requiresUserConsent,
            bool requiresCancellationInvoiceType,
            string reason)
        {
            return new InvoiceContinuationDecision
            {
                Action = action,
                CancellationStrategy = cancellationStrategy,
                RequiresUserConsent = requiresUserConsent,
                RequiresCancellationInvoiceType =
                    requiresCancellationInvoiceType,
                Reason = reason
            };
        }
    }
}
