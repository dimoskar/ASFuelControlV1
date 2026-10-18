using System;
using Exedron.Invoicing.Core.Models;
namespace Exedron.Invoicing.Core
{
    public sealed class IlydaProviderOptions
    {
        public bool TestMode { get; set; }
        public string IssuerVat { get; set; }
        // Optional override; defaults to the documented environment-specific /api/invoice/sign.
        public string SignatureUrl { get; set; }
        public string PosTerminalId { get; set; }
        public string NspProtocol { get; set; } = "DEFAULT";
        public IIlydaPosStore PosStore { get; set; }
        public string ApiKey { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
        public IInvoiceXmlSerializer XmlSerializer { get; set; }
        public IRequestResponseLogger Logger { get; set; }
        public IIlydaSubmissionStore Store { get; set; }
    }

    // Persisted before transmission. Retain every attempt, including its original payload.
    public sealed class IlydaSubmission : IRoutedSubmission
    {
        public string RoutingContext { get; set; }
        public Guid AttemptId { get; set; }
        public Guid InvoiceId { get; set; }
        public bool TestMode { get; set; }
        public string IssuerVat { get; set; }
        public string RequestJson { get; set; }
        public string ResponseJson { get; set; }
        public string State { get; set; }
        public string InvoiceUid { get; set; }
        public string QueueState { get; set; }
        public string QueueErrors { get; set; }
        public string QueueResponseJson { get; set; }
        public bool PdfUploaded { get; set; }
    }

    public interface IIlydaSubmissionStore
    {
        IDisposable Acquire(Guid invoiceId);
        IlydaSubmission Load(Guid invoiceId);
        void Save(IlydaSubmission submission);
    }

}
