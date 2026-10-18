using System;
using Exedron.Invoicing.Core.Models;
namespace Exedron.Invoicing.Core
{
    public sealed class PaymentVoidRequest
    {
        public string TransactionUid { get; set; }
        public string OriginalTransactionId { get; set; }
        public string TerminalId { get; set; }
        public int AmountMinorUnits { get; set; }
        public int PaymentType { get; set; }
        public int TransactionType { get; set; }
        public bool RecoverOnly { get; set; }
    }
    public sealed class GBridgeOptions
    {
        public const string PosTypeKey = "GBridge.GBridge";
        public const string DefaultEndpoint = "http://127.0.0.1:7902/";

        public string Endpoint { get; set; } = DefaultEndpoint;
        public string TerminalId { get; set; }
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(220);
        public IRequestResponseLogger Logger { get; set; }
        public IPaymentGatewayEventSink EventSink { get; set; }
    }

    public sealed class GBridgePaymentResult
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
