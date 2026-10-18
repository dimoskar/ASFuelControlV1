using System;

namespace Exedron.Invoicing.Core
{
    public sealed class MyDataOptions
    {
        public const string ProductionUrl = "https://mydatapi.aade.gr/myDATA/";
        public const string TestUrl = "https://mydataapidev.aade.gr/";
        public bool TestMode { get; set; }
        public string BaseUrl { get; set; }
        public string UserId { get; set; }
        public string SubscriptionKey { get; set; }
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
        public IRequestResponseLogger Logger { get; set; }
        public IMyDataSubmissionStore Store { get; set; }
    }
    public interface IMyDataClient : IInvoiceProvider
    {
        MyDataApiResponse CancelInvoice(long mark, string entityVatNumber = null);
        MyDataApiResponse RequestTransmittedDocs(long fromMark, string nextPartitionKey = null, string nextRowKey = null);
    }
    public sealed class MyDataApiResponse
    {
        public int HttpStatus { get; set; }
        public string Xml { get; set; }
    }
    public sealed class MyDataSubmission : IRoutedSubmission
    {
        public string RoutingContext { get; set; }
        public Guid AttemptId { get; set; }
        public Guid InvoiceId { get; set; }
        public string Scope { get; set; }
        public string RequestXml { get; set; }
        public string ResponseXml { get; set; }
        public string[] RecoveryResponsesXml { get; set; }
        public int? HttpStatus { get; set; }
        public string State { get; set; }
        public string CommunicationError { get; set; }
        public DateTimeOffset StartedUtc { get; set; }
        public DateTimeOffset? CompletedUtc { get; set; }
    }
    public interface IMyDataSubmissionStore
    {
        // Scope isolates provider environment and issuer credentials; lock spans load/send/save.
        IDisposable Acquire(string scope, Guid invoiceId);
        MyDataSubmission Load(string scope, Guid invoiceId);
        // Append new AttemptId; update that attempt only. Never overwrite earlier evidence.
        void Save(MyDataSubmission attempt);
    }
}
