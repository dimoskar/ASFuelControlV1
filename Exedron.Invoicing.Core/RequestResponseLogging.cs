using System;

namespace Exedron.Invoicing.Core
{
    /// <summary>Implemented by the host; no storage or logging backend is included.</summary>
    public interface IRequestResponseLogger
    {
        // Called BEFORE transport. Throwing prevents this request from being sent.
        void RequestStarted(InvoiceRequestLog request);
        // A failure here must never convert a successful payment/submission into a retry.
        void ResponseReceived(InvoiceResponseLog response);
    }
    public sealed class InvoiceRequestLog
    {
        public Guid ExchangeId { get; set; }
        public Guid? InvoiceId { get; set; }
        public string Integration { get; set; }
        public string Environment { get; set; }
        public string Method { get; set; }
        public string Endpoint { get; set; }
        public string ContentType { get; set; }
        public string Body { get; set; }
        public DateTimeOffset StartedUtc { get; set; }
    }
    public sealed class InvoiceResponseLog
    {
        public Guid ExchangeId { get; set; }
        public DateTimeOffset CompletedUtc { get; set; }
        public int? HttpStatus { get; set; }
        public string Body { get; set; }
        public string CommunicationError { get; set; }
    }
}
