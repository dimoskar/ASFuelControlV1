using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Exedron.Invoicing.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Exedron.Invoicing.Adapters.Samtec
{
    // DeviceId identifies an immutable, versioned configuration profile. The host must
    // retain old profiles for historical requests, rather than resolve through current settings.
    public sealed class SamtecDeviceProfile
    {
        public SamtecDeviceProfile(FiscalSigningScope scope, Uri endpoint)
        {
            if (scope == null || scope.SignerKey != SamtecFiscalSigner.Key) throw new ArgumentException("A Samtec scope is required.");
            if (endpoint == null || !endpoint.IsAbsoluteUri || (endpoint.Scheme != "http" && endpoint.Scheme != "https") ||
                endpoint.UserInfo.Length != 0 || endpoint.Query.Length != 0 || endpoint.Fragment.Length != 0)
                throw new ArgumentException("An explicit HTTP(S) device endpoint without credentials, query or fragment is required.");
            Scope = scope; Endpoint = endpoint;
        }
        public FiscalSigningScope Scope { get; }
        public Uri Endpoint { get; }
    }

    // No recovery interface: the repository has no verified protocol for retrieving
    // a signature bound to an uncertain request. Unknown requires reconciliation.
    public sealed class SamtecFiscalSigner : IFiscalSigner, IFiscalSigningRequestValidator, IDisposable
    {
        public const string Key = "Samtec";
        public const string Format = "samtec-sign-json/v1";
        private readonly SamtecDeviceProfile profile;
        private readonly HttpClient client;
        public SamtecFiscalSigner(SamtecDeviceProfile profile, HttpMessageHandler transport = null)
        {
            this.profile = profile ?? throw new ArgumentNullException(nameof(profile));
            client = transport == null
                ? new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
                : new HttpClient(transport, false);
            client.Timeout = TimeSpan.FromSeconds(130);
        }
        public FiscalSignerCapabilities Capabilities { get; } = new FiscalSignerCapabilities(Key,
            new[] { FiscalDocumentKind.Invoice, FiscalDocumentKind.Balance, FiscalDocumentKind.TankFilling });

        public static FiscalSigningRequest Prepare(FiscalDocumentReference document, FiscalSigningScope scope, Request wire)
        {
            if (wire == null) throw new ArgumentNullException(nameof(wire));
            var request = new FiscalSigningRequest(Guid.NewGuid(), document, scope, Format,
                new UTF8Encoding(false).GetBytes(JsonConvert.SerializeObject(wire)));
            ValidatePayload(request);
            return request;
        }
        public void Validate(FiscalSigningRequest request)
        {
            if (request == null || !FiscalSigningCoordinator.SameScope(request.Scope, profile.Scope))
                throw new InvalidOperationException("The configured device does not match the historical signing scope.");
            ValidatePayload(request);
        }
        private static void ValidatePayload(FiscalSigningRequest request)
        {
            if (request.PayloadFormat != Format || request.Scope.SignerKey != Key)
                throw new InvalidOperationException("Unsupported Samtec payload format or signer.");
            // Reject unmodelled fields (including hidden device commands) before transport.
            var wire = JsonConvert.DeserializeObject<Request>(new UTF8Encoding(false, true).GetString(request.Payload),
                new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Error });
            var details = wire?.Doc?.InvoiceDetails;
            Guid uid;
            if (details == null || wire.JobType != "Sign" || wire.JobParams != null ||
                !Guid.TryParse(details.InvoiceUID, out uid) || uid != request.Document.DocumentId ||
                details.PrintDevice != 0 || details.ReqForToken != 0 || string.IsNullOrWhiteSpace(wire.AtxtContent))
                throw new InvalidOperationException("A rendered, matching sign-only document is required.");
            if (details.InvoiceType == 215 || details.CancelInvNo != null || details.CancelInvSeries != null ||
                (details.CancelInvType ?? 0) != 0 || (details.CancelDevDailyNum ?? 0) != 0)
                throw new InvalidOperationException("Cancellation requires verified parent device evidence and the cancellation workflow.");
            if (wire.Doc.Payments != null && wire.Doc.Payments.Any(p => p == null ||
                (p.Type != 0 && p.Type != 5) || p.EftposTID != null || p.EftposTransType != 0))
                throw new InvalidOperationException("POS must be coordinated through the payment workflow.");
            switch (request.Document.Kind)
            {
                case FiscalDocumentKind.Invoice:
                    if (details.InvoiceType <= 0 || string.IsNullOrWhiteSpace(details.InvoiceNo) ||
                        wire.InputType != SamtecRequestBuilder.InputType(details.InvoiceType) ||
                        (wire.InputType == 1 && (wire.Doc.TransactionLines == null || wire.Doc.TransactionLines.Length == 0 ||
                            wire.Doc.Payments == null || wire.Doc.Payments.Length != 1)))
                        throw new InvalidOperationException("An issued invoice with matching lines and payment is required.");
                    break;
                case FiscalDocumentKind.Balance:
                case FiscalDocumentKind.TankFilling:
                    if (wire.InputType != 2 || details.InvoiceTotal != 0 || wire.Doc.Payments != null || wire.Doc.TransactionLines != null ||
                        (request.Document.Kind == FiscalDocumentKind.Balance ? details.InvoiceType != 501 : details.InvoiceType != 40 && details.InvoiceType != 181))
                        throw new InvalidOperationException("Invalid non-invoice signing document.");
                    break;
                default: throw new NotSupportedException("This document kind has no implemented Samtec signing operation.");
            }
        }
        public FiscalSigningResult Sign(FiscalSigningRequest request)
        {
            Validate(request);
            string raw = null;
            try
            {
                using (var message = new HttpRequestMessage(HttpMethod.Post, profile.Endpoint))
                {
                    message.Content = new ByteArrayContent(request.Payload);
                    message.Content.Headers.ContentType = new MediaTypeHeaderValue("text/json");
                    message.Headers.TryAddWithoutValidation("charset", "UTF-8");
                    message.Headers.TryAddWithoutValidation("Accept", "application/json, text/javascript, */*");
                    using (var response = client.SendAsync(message).GetAwaiter().GetResult())
                    {
                        raw = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                        if (!response.IsSuccessStatusCode)
                            return FiscalSigningResult.Unknown(request.AttemptId, raw, "Device HTTP status " + (int)response.StatusCode + "; signing outcome is unconfirmed.");
                        return Parse(request, raw);
                    }
                }
            }
            catch (Exception ex)
            {
                return FiscalSigningResult.Unknown(request.AttemptId, raw, "Unconfirmed device response: " + ex.GetType().Name);
            }
        }
        private static FiscalSigningResult Parse(FiscalSigningRequest request, string raw)
        {
            var response = JObject.Parse(raw);
            string uid = (string)response["InvoiceUID"], job = (string)response["JobType"], code = (string)response["ResultCode"];
            Guid parsed;
            if ((uid != null && (!Guid.TryParse(uid, out parsed) || parsed != request.Document.DocumentId)) ||
                (job != null && job != "Sign"))
                return FiscalSigningResult.Unknown(request.AttemptId, raw, "Device response belongs to a different document or operation.");
            var parts = (code ?? "").Split(',');
            if (parts.Length < 2 || parts[0].Trim() != "ΔΦΣΣ" || string.IsNullOrWhiteSpace(parts[1]) ||
                parts[1].Trim() == "-" || parts[1].Trim() == "Sign Device A" || code.IndexOf("Error", StringComparison.OrdinalIgnoreCase) >= 0)
                return FiscalSigningResult.Unknown(request.AttemptId, raw, "Device reply contains no confirmed fiscal signature.");
            string qr = null; Uri uri;
            if (parts.Length > 2 && Uri.TryCreate(parts[2].Trim(), UriKind.Absolute, out uri) && uri.Scheme == "https") qr = parts[2].Trim();
            return FiscalSigningResult.Signed(request.AttemptId, parts[1].Trim(), qr, uid, raw);
        }
        public void Dispose() { client.Dispose(); }
    }

    public static class SamtecRequestBuilder
    {
        public static int InputType(int code) => new[] { 40, 63, 65, 158, 159, 160, 177, 181, 219, 238, 298, 299, 303, 304, 316, 317, 500, 501, 502 }.Contains(code) ? 2 : 1;
        public static Request Create(FiscalDocumentReference document, string renderedText, int officialCode)
        {
            return new Request { JobType = "Sign", InputType = document.Kind == FiscalDocumentKind.Invoice ? InputType(officialCode) : 2,
                AtxtContent = renderedText, Doc = new Doc { InvoiceDetails = new Invoicedetails {
                    InvoiceUID = document.DocumentId.ToString(), InvoiceType = officialCode,
                    CancelInvType = document.Kind == FiscalDocumentKind.Invoice ? (int?)null : 0,
                    CancelDevDailyNum = document.Kind == FiscalDocumentKind.Invoice ? (int?)null : 0 } } };
        }
    }
}
