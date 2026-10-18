using System;

namespace Exedron.Invoicing.Core
{
    // HTTP evidence only: fiscal recovery and retry authorization remain in the existing workflow.
    public sealed class ITimologiaSubmissionAttempt : IRoutedSubmission
    {
        public string RoutingContext { get; set; }
        public Guid AttemptId { get; set; }
        public Guid InvoiceId { get; set; }
        public bool TestMode { get; set; }
        public string IssuerVat { get; set; }
        public string Endpoint { get; set; }
        public string RequestJson { get; set; }
        public int? StatusCode { get; set; }
        public string ResponseJson { get; set; }
        public string PrintMetadataResponseJson { get; set; }
        public string TransportError { get; set; }
        public string State { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime? CompletedUtc { get; set; }
    }

    public interface IITimologiaSubmissionStore
    {
        IDisposable Acquire(Guid invoiceId);
        // Isolate the lookup by environment and issuer; an unknown result never authorizes a resend.
        ITimologiaSubmissionAttempt LoadLatest(Guid invoiceId, bool testMode, string issuerVat);
        void Begin(ITimologiaSubmissionAttempt attempt);
        void Complete(ITimologiaSubmissionAttempt attempt);
    }
}
