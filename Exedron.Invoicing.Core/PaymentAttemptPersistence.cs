using System;

namespace Exedron.Invoicing.Core
{
    public enum ProviderPaymentOperation
    {
        Sale = 0,
        Refund = 1
    }

    public static class ProviderPaymentAttemptStates
    {
        public const string Prepared = "Prepared";
        public const string SignatureReady = "SignatureReady";
        public const string SignaturePending = "SignaturePending";
        public const string Pending = "Pending";
        public const string Unknown = "Unknown";
        public const string Declined = "Declined";
        public const string Approved = "Approved";
        public const string Invoiced = "Invoiced";
        public const string Mismatch = "Mismatch";
        public const string VoidPending = "VoidPending";
        public const string Voided = "Voided";
        public const string VoidFailed = "VoidFailed";
        public const string Failed = "Failed";
    }

    public sealed class ProviderPaymentAttemptRequest
    {
        public Guid InvoiceId { get; set; }
        public string Gateway { get; set; }
        public string InvoiceProvider { get; set; }
        public int ProviderId { get; set; }
        public ProviderPaymentOperation Operation { get; set; }
        public int PaymentType { get; set; }
        public string TerminalId { get; set; }
        public decimal Amount { get; set; }
        public int AmountMinorUnits { get; set; }
        public int CurrencyCode { get; set; } = 978;
        // Versioned host snapshot, containing routing and invoice data, never credentials.
        public string WorkflowContext { get; set; }
        public Guid? OriginalInvoiceId { get; set; }
        public Guid? OriginalPaymentAttemptId { get; set; }
    }

    public sealed class ProviderPaymentAttempt
    {
        public Guid Id { get; set; }
        public Guid InvoiceId { get; set; }
        public Guid? OriginalPaymentAttemptId { get; set; }
        public int AttemptNumber { get; set; }
        public int ProviderId { get; set; }
        public string State { get; set; }
        public string TransactionUid { get; set; }
        public string VoidTransactionUid { get; set; }
        public string TerminalId { get; set; }
        public decimal Amount { get; set; }
        public int AmountMinorUnits { get; set; }
        public ProviderPaymentOperation Operation { get; set; }
        public int PaymentType { get; set; }
        public string ProviderUid { get; set; }
        public string ProviderInput { get; set; }
        public string ProviderSignature { get; set; }
        public string TransactionId { get; set; }
        public string ResponseTerminalId { get; set; }
        public string Rrn { get; set; }
        public string AuthorizationCode { get; set; }
        public int? ResponseAmountMinorUnits { get; set; }
        public int? ResponseTransactionType { get; set; }
        public int? ResponsePaymentType { get; set; }
        public bool RequiresSignatureRenewal { get; set; }
        public Guid? OperatorRetryOfAttemptId { get; set; }
        // Retained only for reading attempts created before operator-controlled retries.
        public Guid? AutomaticRetryOfAttemptId { get; set; }
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
    }

    public sealed class PaymentSignatureRejectedException : InvalidOperationException
    {
        public PaymentSignatureRejectedException() : base("Ο πάροχος απέρριψε την αίτηση υπογραφής POS. Διορθώστε τις ρυθμίσεις και επιλέξτε Συνέχιση έκδοσης.") { }
    }

    public sealed class PaymentRecoveryEvidence
    {
        public PaymentAuthorizationResult Approval { get; set; }
        public string IncompleteMessage { get; set; }
    }

    public interface IProviderPaymentAttemptStore
    {
        ProviderPaymentAttempt GetOrCreate(ProviderPaymentAttemptRequest request);
        void SaveSignature(Guid attemptId, ProviderPaymentSignature signature);
        void MarkPending(Guid attemptId);
        void MarkApproved(Guid attemptId, PaymentAuthorizationResult result);
        void MarkFailure(Guid attemptId, string state, string errorCode, string errorMessage);
        void MarkInvoiceSubmitted(Guid attemptId);
        void MarkVoidPending(Guid attemptId, string voidTransactionUid);
        void MarkVoided(Guid attemptId);
        void MarkVoidFailed(Guid attemptId, string errorMessage);
    }

    public interface IProviderPaymentAttemptControl : IProviderPaymentAttemptStore
    {
        ProviderPaymentAttempt LoadLatest(Guid invoiceId);
    }

    public interface IProviderPaymentWorkflowStore : IProviderPaymentAttemptControl
    {
        void MarkSignaturePending(Guid attemptId);
    }

    public sealed class PaymentGatewayEvent
    {
        public string TransactionUid { get; set; }
        public string InputType { get; set; }
        public DateTimeOffset StartedUtc { get; set; }
        public DateTimeOffset CompletedUtc { get; set; }
        public int? HttpStatus { get; set; }
        public string RequestJson { get; set; }
        public string ResponseJson { get; set; }
        public string Status { get; set; }
        public string ReplyCode { get; set; }
        public string ReplyDescription { get; set; }
        public string EftposReplyCode { get; set; }
        public string EftposReplyMessage { get; set; }
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
    }

    public interface IPaymentGatewayEventSink
    {
        void Record(PaymentGatewayEvent paymentEvent);
    }
}
