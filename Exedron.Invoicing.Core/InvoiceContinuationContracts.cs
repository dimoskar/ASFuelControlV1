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
                    "No active previous invoice was confirmed.");
            }

            if (request.Existence == InvoiceExistenceState.Cancelled)
            {
                return Decision(InvoiceContinuationAction.Resend,
                    request.Provider.CancellationStrategy, false, false,
                    "The previous invoice is already cancelled.");
            }

            if (state == "Rejected")
            {
                if (payloadKind ==
                        InvoicePayloadComparisonKind.IdentityChanged &&
                    !request.IdentityChangeAuthorized)
                {
                    return Decision(InvoiceContinuationAction.Block,
                        request.Provider.CancellationStrategy, false, false,
                        "A rejected retry must retain its issued fiscal identity unless an audited correction explicitly replaces it.");
                }
                if (payloadKind == InvoicePayloadComparisonKind.Unknown)
                {
                    return Decision(InvoiceContinuationAction.Block,
                        request.Provider.CancellationStrategy, false, false,
                        "The rejected request identity could not be compared safely.");
                }

                return Decision(InvoiceContinuationAction.Resend,
                    request.Provider.CancellationStrategy, false, false,
                    "The previous request was definitively rejected and the retry retains its fiscal identity.");
            }

            if (state == "ReconciliationRequired" ||
                payloadKind == InvoicePayloadComparisonKind.Unknown &&
                (state == "Unknown" || state == "Sending"))
            {
                return Decision(InvoiceContinuationAction.Recover,
                    request.Provider.CancellationStrategy, false, false,
                    "Recover the previous attempt before deciding whether to resend or cancel it.");
            }
            if (payloadKind == InvoicePayloadComparisonKind.Unknown)
            {
                return Decision(InvoiceContinuationAction.Recover,
                    request.Provider.CancellationStrategy, false, false,
                    "The previous and current payloads could not be compared safely. Recover the prior attempt before continuing.");
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
                    "The payload changed, but the previous outcome is not confirmed. Recover it before cancellation or reissue.");
            }

            if (payloadChanged)
            {
                if (request.Provider.CancellationStrategy ==
                    InvoiceCancellationStrategy.None)
                {
                    return Decision(InvoiceContinuationAction.Block,
                        request.Provider.CancellationStrategy, false, false,
                        "The active invoice differs and this provider has no configured cancellation strategy.");
                }
                bool needsType = request.Provider.CancellationStrategy ==
                    InvoiceCancellationStrategy.IssueCancellationDocument;
                if (!request.UserConsented)
                {
                    return Decision(InvoiceContinuationAction.Block,
                        request.Provider.CancellationStrategy, true, needsType,
                        "The active invoice has different values. Explicit consent is required to cancel it and issue a replacement.");
                }
                if (needsType &&
                    !request.CancellationInvoiceTypeId.HasValue)
                {
                    return Decision(InvoiceContinuationAction.Block,
                        request.Provider.CancellationStrategy, false, true,
                        "Select the cancellation invoice type before cancelling the provider invoice.");
                }

                return Decision(InvoiceContinuationAction.CancelAndReissue,
                    request.Provider.CancellationStrategy, false, false,
                    "Cancel the active invoice before issuing the changed payload.");
            }

            if (state == "Accepted" ||
                request.Existence == InvoiceExistenceState.Active)
            {
                return Decision(InvoiceContinuationAction.ContinueExisting,
                    request.Provider.CancellationStrategy, false, false,
                    "The active invoice already represents the same values.");
            }

            if (state == "RetryAuthorized")
            {
                return Decision(InvoiceContinuationAction.Resend,
                    request.Provider.CancellationStrategy, false, false,
                    "The retry has already been authorized for the same semantic payload.");
            }

            if (state == "Unknown" || state == "Sending")
            {
                if (!request.UserConsented)
                {
                    return Decision(InvoiceContinuationAction.Block,
                        request.Provider.CancellationStrategy, true, false,
                        "The previous outcome is unresolved. Explicit consent is required before resending the same values.");
                }

                return Decision(InvoiceContinuationAction.Resend,
                    request.Provider.CancellationStrategy, false, false,
                    "Resend the same semantic payload using the provider's stable invoice identity.");
            }

            return Decision(InvoiceContinuationAction.Block,
                request.Provider.CancellationStrategy, false, false,
                "The previous state does not permit further processing.");
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
