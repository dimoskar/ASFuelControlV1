using Exedron.Invoicing.Core.Models;
using System;

namespace Exedron.Invoicing.Core
{
    public interface IInvoiceProvider : IDisposable
    {
        InvoiceProviderDefinition Descriptor { get; }

        InvoiceResponse SendInvoice(
            InvoiceModel invoice,
            Guid invoiceId,
            ProviderPaymentContext providerPayment);
    }

    /// <summary>
    /// Optional capability implemented only by providers that participate in the
    /// common provider-signature/GBridge payment flow.
    /// </summary>
    public interface IInvoiceProviderPaymentSigner
    {
        ProviderPaymentSignature CreatePaymentSignature(InvoiceModel invoice);
    }

    // Providers with durable signature lifecycles own reuse, expiry and cancellation.
    public interface IInvoiceProviderPaymentLifecycle
    {
        IDisposable AcquirePayment(Guid invoiceId);
        ProviderPaymentSignature PreparePaymentSignature(InvoiceModel invoice, Guid invoiceId, bool authorizationMayStart);
        void CancelUnusedPaymentSignature(Guid invoiceId, string reason);
    }

    public interface IInvoiceProviderRecovery
    {
        // Retrieve the existing fiscal result; never submit an invoice or initiate a payment.
        InvoiceResponse RecoverInvoice(Guid invoiceId);
    }

    public interface IInvoiceProviderLookup
    {
        InvoiceLookupResult LookupInvoice(Guid invoiceId);
    }

    public sealed class InvoiceLookupResult
    {
        public bool NotFound { get; set; }
        public InvoiceResponse Response { get; set; }
    }

    /// <summary>
    /// Completes invoice issuance with POS payment. Implementations own their payment ordering
    /// and recovery; they must reject incompatible invoice providers before any side effect.
    /// Dispose releases POS resources, not a separately supplied invoice provider.
    /// </summary>
    public interface IPOSSupport : IDisposable
    {
        InvoiceResponse SendInvoiceWithPOS(IInvoiceProvider provider, InvoiceModel invoice, Guid invoiceId);
    }

    // Lower-level authorization transport used by GBridgePOSSupport.
    public interface IPaymentGateway : IDisposable
    {
        string TerminalId { get; }
        string ValidateConfiguration();
        PaymentAuthorizationResult Authorize(PaymentAuthorizationRequest request);
        void Void(string transactionUid, string originalTransactionId);
    }

    public sealed class ProviderPaymentSignature
    {
        public PaymentMethod SourcePayment { get; set; }
        public string ProviderUid { get; set; }
        public string ProviderInput { get; set; }
        public string ProviderSignature { get; set; }
    }

    public sealed class ProviderPaymentContext
    {
        public PaymentMethod SourcePayment { get; set; }
        public string ProviderUid { get; set; }
        public string ProviderSignature { get; set; }
        public string TransactionId { get; set; }
        public string TerminalId { get; set; }
    }

    public sealed class PaymentAuthorizationRequest
    {
        public string TransactionUid { get; set; }
        public ProviderPaymentOperation Operation { get; set; }
        public int PaymentType { get; set; }
        public int ProviderId { get; set; }
        public string ProviderInput { get; set; }
        public string ProviderSignature { get; set; }
        public string ExpectedTerminalId { get; set; }
        public int ExpectedAmountMinorUnits { get; set; }
        public bool RecoverOnly { get; set; }
    }

    public sealed class PaymentAuthorizationResult
    {
        public string TransactionId { get; set; }
        public string TerminalId { get; set; }
        public string Rrn { get; set; }
        public string AuthorizationCode { get; set; }
        public int AmountMinorUnits { get; set; }
        public string ResponseTransactionUid { get; set; }
        public int TransactionType { get; set; }
        public int PaymentType { get; set; }
        public int? Stan { get; set; }
        public int? BatchNumber { get; set; }
        public string Acquirer { get; set; }
        public string MerchantId { get; set; }
        public string MaskedPan { get; set; }
        public string CardType { get; set; }
        public DateTimeOffset? TransactionTimestamp { get; set; }
        public DateTimeOffset? VoidTimestamp { get; set; }
        public string MerchantReceipt { get; set; }
        public string CustomerReceipt { get; set; }
    }
}
