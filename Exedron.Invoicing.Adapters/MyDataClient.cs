using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Xml.Linq;
using Exedron.Invoicing.Core;
using Exedron.Invoicing.Core.Models;

namespace Exedron.Invoicing.Adapters
{
    public sealed partial class MyDataClient : IMyDataClient, IInvoiceProviderRecovery
    {
        private readonly MyDataOptions options;
        private readonly IInvoiceXmlSerializer serializer;
        private readonly HttpClient client;
        private readonly string baseUrl;
        private readonly string scope;

        public MyDataClient(MyDataOptions options, IInvoiceXmlSerializer serializer, HttpMessageHandler handler = null)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            if (options.Store == null) throw new ArgumentException("A host-owned myDATA submission store is required.", nameof(options));
            if (string.IsNullOrWhiteSpace(options.UserId) || string.IsNullOrWhiteSpace(options.SubscriptionKey))
                throw new ArgumentException("myDATA user ID and subscription key are required.", nameof(options));
            baseUrl = (string.IsNullOrWhiteSpace(options.BaseUrl) ? options.TestMode ? MyDataOptions.TestUrl : MyDataOptions.ProductionUrl : options.BaseUrl).TrimEnd('/') + "/";
            Uri endpoint;
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out endpoint) || endpoint.Scheme != "https" ||
                !string.IsNullOrEmpty(endpoint.UserInfo) || !string.IsNullOrEmpty(endpoint.Query) || !string.IsNullOrEmpty(endpoint.Fragment))
                throw new ArgumentException("myDATA requires an HTTPS base URL without credentials or query parameters.", nameof(options));
            scope = baseUrl + "|" + options.UserId.Trim();
            client = LoggedHttpClient.Create(options.Logger, InvoiceProviderCatalog.MyDataKey, options.TestMode ? "Test" : "Production", handler);
            client.Timeout = options.Timeout > TimeSpan.Zero ? options.Timeout : TimeSpan.FromSeconds(30);
        }
        public InvoiceProviderDefinition Descriptor => InvoiceProviderCatalog.Get(InvoiceProviderCatalog.MyDataKey);

        public InvoiceResponse SendInvoice(InvoiceModel invoice, Guid invoiceId, ProviderPaymentContext payment)
        {
            if (payment != null) return Error("A provider POS signature cannot be submitted through the direct myDATA channel.");
            var xml = serializer.Serialize(invoice, true);
            using (options.Store.Acquire(scope, invoiceId))
            {
                var previous = options.Store.Load(scope, invoiceId);
                if (previous != null && previous.State != "Rejected")
                {
                    if (previous.State == "Accepted" && previous.RequestXml == xml)
                        return ParseInvoiceResponse(previous.ResponseXml);
                    return Error("The previous myDATA submission must be reconciled before sending again. No new invoice was sent.");
                }
                var attempt = new MyDataSubmission
                {
                    AttemptId = Guid.NewGuid(), InvoiceId = invoiceId, Scope = scope,
                    RequestXml = xml, State = "Sending", StartedUtc = DateTimeOffset.UtcNow
                };
                options.Store.Save(attempt);
                InvoiceResponse result;
                try
                {
                    var reply = Send(HttpMethod.Post, "SendInvoices", xml, invoiceId);
                    attempt.HttpStatus = reply.HttpStatus;
                    attempt.ResponseXml = reply.Xml;
                    result = ParseInvoiceResponse(reply.Xml);
                    var rows = XDocument.Parse(reply.Xml).Descendants().Where(e => e.Name.LocalName == "response").ToArray();
                    var row = rows.Length == 1 ? rows[0] : null;
                    string status = Value(row, "statusCode");
                    bool successHttp = reply.HttpStatus >= 200 && reply.HttpStatus < 300;
                    bool rejected = successHttp && row != null && string.IsNullOrEmpty(Value(row, "invoiceMark")) &&
                        string.IsNullOrEmpty(Value(row, "invoiceUid")) && (status == "ValidationError" || status == "XMLSyntaxError");
                    attempt.State = successHttp && !result.HasErrors ? "Accepted" : rejected ? "Rejected" : "Unknown";
                    if (!successHttp) result = Error("myDATA HTTP " + reply.HttpStatus.ToString(CultureInfo.InvariantCulture) + ": " + string.Join("; ", result.Errors ?? new string[0]));
                }
                catch (Exception ex)
                {
                    attempt.State = "Unknown";
                    attempt.CommunicationError = ex.GetType().FullName;
                    result = Error("The myDATA submission outcome is unconfirmed. Reconcile the saved attempt before retrying.");
                }
                attempt.CompletedUtc = DateTimeOffset.UtcNow;
                // If saving the result fails the persisted Sending record still blocks duplicate sends.
                options.Store.Save(attempt);
                return result;
            }
        }
        public InvoiceResponse RecoverInvoice(Guid invoiceId)
        {
            using (options.Store.Acquire(scope, invoiceId))
            {
                var saved = options.Store.Load(scope, invoiceId);
                if (saved?.State == "Accepted") return ParseInvoiceResponse(saved.ResponseXml);
                if (saved == null || saved.State == "Rejected") return Error("There is no uncertain myDATA submission to reconcile.");
                return Reconcile(saved, invoiceId);
            }
        }
        public MyDataApiResponse CancelInvoice(long mark, string entityVatNumber = null)
        {
            if (mark <= 0) throw new ArgumentOutOfRangeException(nameof(mark));
            string path = "CancelInvoice?mark=" + mark.ToString(CultureInfo.InvariantCulture);
            if (!string.IsNullOrWhiteSpace(entityVatNumber)) path += "&entityVatNumber=" + Uri.EscapeDataString(entityVatNumber.Trim());
            return Send(HttpMethod.Post, path, null, null);
        }
        public MyDataApiResponse RequestTransmittedDocs(long fromMark, string nextPartitionKey = null, string nextRowKey = null)
        {
            if (fromMark < 0) throw new ArgumentOutOfRangeException(nameof(fromMark));
            string path = "RequestTransmittedDocs?mark=" + fromMark.ToString(CultureInfo.InvariantCulture);
            if (!string.IsNullOrWhiteSpace(nextPartitionKey)) path += "&nextPartitionKey=" + Uri.EscapeDataString(nextPartitionKey);
            if (!string.IsNullOrWhiteSpace(nextRowKey)) path += "&nextRowKey=" + Uri.EscapeDataString(nextRowKey);
            return Send(HttpMethod.Get, path, null, null);
        }
        private MyDataApiResponse Send(HttpMethod method, string path, string xml, Guid? invoiceId)
        {
            using (var request = new HttpRequestMessage(method, baseUrl + path))
            {
                request.Headers.Add("aade-user-id", options.UserId);
                request.Headers.Add("ocp-apim-subscription-key", options.SubscriptionKey);
                if (invoiceId.HasValue) request.Properties[LoggedHttpClient.InvoiceIdKey] = invoiceId.Value;
                if (xml != null) request.Content = new StringContent(xml, Encoding.UTF8, "application/xml");
                using (var reply = client.SendAsync(request).GetAwaiter().GetResult())
                    return new MyDataApiResponse { HttpStatus = (int)reply.StatusCode, Xml = reply.Content == null ? "" : reply.Content.ReadAsStringAsync().GetAwaiter().GetResult() };
            }
        }
        private static InvoiceResponse ParseInvoiceResponse(string xml)
        {
            var rows = XDocument.Parse(xml).Descendants().Where(e => e.Name.LocalName == "response").ToArray();
            if (rows.Length != 1) return Error("myDATA returned an unexpected response count for one invoice.");
            var row = rows[0];
            long mark;
            if (Value(row, "statusCode") == "Success" && long.TryParse(Value(row, "invoiceMark"), out mark) && mark > 0 && !string.IsNullOrWhiteSpace(Value(row, "invoiceUid")))
                return new InvoiceResponse { Mark = Value(row, "invoiceMark"), Uid = Value(row, "invoiceUid"), QrCodeUrl = Value(row, "qrUrl"), Errors = new string[0], Warnings = new string[0] };
            var errors = row.Descendants().Where(e => e.Name.LocalName == "error").Select(e => Value(e, "code") + " - " + Value(e, "message")).ToArray();
            return new InvoiceResponse { Errors = errors.Length > 0 ? errors : new[] { "myDATA did not confirm invoice issuance: " + Value(row, "statusCode") }, Warnings = new string[0] };
        }
        private static string Value(XElement parent, string name) { return (string)parent?.Elements().FirstOrDefault(e => e.Name.LocalName == name); }
        private static InvoiceResponse Error(string message) { return new InvoiceResponse { Errors = new[] { message }, Warnings = new string[0] }; }
        public void Dispose() { client.Dispose(); }
    }
}
