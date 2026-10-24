using System;
using System.Net.Http;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Exedron.Invoicing.Core;

namespace Exedron.Invoicing.Adapters
{
    internal sealed class RequestEvidenceException : Exception
    {
        internal RequestEvidenceException(Exception inner)
            : base("Request evidence could not be persisted before transport.", inner) { }
    }

    internal static class LoggedHttpClient
    {
        internal const string InvoiceIdKey = "Exedron.Invoicing.InvoiceId";
        internal const string DocumentKindKey = "Exedron.Invoicing.DocumentKind";
        internal const string DocumentIdKey = "Exedron.Invoicing.DocumentId";
        internal static readonly AsyncLocal<Guid?> CurrentInvoice = new AsyncLocal<Guid?>();
        internal static HttpClient Create(IRequestResponseLogger logger, string integration, string environment, HttpMessageHandler handler)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger), "The host must supply a request/response logger.");
            return new HttpClient(new LoggingHandler(logger, integration, environment)
            {
                InnerHandler = handler == null ? (HttpMessageHandler)new HttpClientHandler { AllowAutoRedirect = false } : new BorrowedHandler(handler)
            });
        }
        private sealed class BorrowedHandler : HttpMessageHandler
        {
            private readonly HttpMessageInvoker invoker;
            internal BorrowedHandler(HttpMessageHandler handler) { invoker = new HttpMessageInvoker(handler, false); }
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) { return invoker.SendAsync(request, token); }
            protected override void Dispose(bool disposing) { if (disposing) invoker.Dispose(); base.Dispose(disposing); }
        }
        private sealed class LoggingHandler : DelegatingHandler
        {
            private readonly IRequestResponseLogger logger;
            private readonly string integration;
            private readonly string environment;
            internal LoggingHandler(IRequestResponseLogger logger, string integration, string environment)
            { this.logger = logger; this.integration = integration; this.environment = environment; }
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
            {
                bool authentication = request.RequestUri.AbsolutePath.IndexOf("/tokens/", StringComparison.OrdinalIgnoreCase) >= 0;
                string media = request.Content?.Headers.ContentType?.MediaType;
                bool binary = media != null && (media.StartsWith("multipart/", StringComparison.OrdinalIgnoreCase) || media == "application/pdf");
                var entry = new InvoiceRequestLog
                {
                    ExchangeId = Guid.NewGuid(), Integration = integration, Environment = environment,
                    InvoiceId = request.Properties.ContainsKey(InvoiceIdKey) ? (Guid?)request.Properties[InvoiceIdKey] : CurrentInvoice.Value,
                    DocumentKind = request.Properties.ContainsKey(DocumentKindKey) ? (FiscalDocumentKind?)request.Properties[DocumentKindKey] : null,
                    DocumentId = request.Properties.ContainsKey(DocumentIdKey) ? (Guid?)request.Properties[DocumentIdKey] : null,
                    Method = request.Method.Method,
                    // No authorization headers, user-info or potentially secret query parameters.
                    Endpoint = SafeEndpoint(request.RequestUri), ContentType = media,
                    StartedUtc = DateTimeOffset.UtcNow,
                    Body = authentication ? "[Authentication payload omitted]" : binary ? "[Binary document payload omitted]" :
                        request.Content == null ? null : await request.Content.ReadAsStringAsync().ConfigureAwait(false)
                };
                try { logger.RequestStarted(entry); }
                catch (Exception ex) { throw new RequestEvidenceException(ex); }
                var completed = new InvoiceResponseLog { ExchangeId = entry.ExchangeId };
                HttpResponseMessage response = null;
                try
                {
                    response = await base.SendAsync(request, token).ConfigureAwait(false);
                    completed.HttpStatus = (int)response.StatusCode;
                    completed.Body = authentication ? "[Authentication payload omitted]" : response.Content == null ? null :
                        await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return response;
                }
                catch (Exception ex)
                {
                    // Exception messages can contain URLs/credentials from external handlers.
                    completed.CommunicationError = ex.GetType().FullName;
                    response?.Dispose();
                    throw;
                }
                finally
                {
                    completed.CompletedUtc = DateTimeOffset.UtcNow;
                    try { logger.ResponseReceived(completed); }
                    catch { /* Host logging must not discard a received fiscal/payment response. */ }
                }
            }
            private static string SafeEndpoint(Uri uri)
            {
                // Preserve documented myDATA request parameters, not arbitrary authentication parameters.
                var allowed = new[] { "mark", "entityVatNumber", "nextPartitionKey", "nextRowKey" };
                var query = uri.Query.TrimStart('?').Split('&').Where(pair =>
                    allowed.Contains(Uri.UnescapeDataString(pair.Split('=')[0]), StringComparer.OrdinalIgnoreCase));
                var suffix = string.Join("&", query);
                return new UriBuilder(uri) { UserName = "", Password = "", Query = "", Fragment = "" }.Uri.GetLeftPart(UriPartial.Path) +
                    (suffix.Length == 0 ? "" : "?" + suffix);
            }
        }
    }
}
