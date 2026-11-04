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
        [JsonConstructor]
        public SamtecDeviceProfile(FiscalSigningScope scope, Uri endpoint, string eftPosTID = null)
        {
            if (scope == null || scope.SignerKey != SamtecFiscalSigner.Key) throw new ArgumentException("A Samtec scope is required.");
            if (endpoint == null || !endpoint.IsAbsoluteUri || (endpoint.Scheme != "http" && endpoint.Scheme != "https") ||
                endpoint.UserInfo.Length != 0 || endpoint.Query.Length != 0 || endpoint.Fragment.Length != 0)
                throw new ArgumentException("An explicit HTTP(S) device endpoint without credentials, query or fragment is required.");
            Scope = scope; Endpoint = endpoint;
            EftPosTID = string.IsNullOrWhiteSpace(eftPosTID) ? null : eftPosTID.Trim();
        }
        public FiscalSigningScope Scope { get; }
        public Uri Endpoint { get; }
        // Samtec performs the card/IRIS transaction as part of the same Sign
        // request. Keep the terminal with the immutable device profile so a
        // prepared request never switches terminal after configuration changes.
        public string EftPosTID { get; }
    }

    // No recovery interface: the repository has no verified protocol for retrieving
    // a signature bound to an uncertain request. Unknown requires reconciliation.
    public sealed class SamtecFiscalSigner : IFiscalSigner, IFiscalSigningRequestValidator,
        IFiscalSignerRecordedResponseRecovery, IFiscalSignerAutomaticRetryPolicy, IDisposable
    {
        public const string Key = "Samtec";
        public const string Format = "samtec-sign-json/v1";
        private const string RetryableDiagnostic = "SAMTEC_RETRYABLE: ";
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
        public int MaximumAutomaticDispatches => 3;
        public bool CanRetry(FiscalSigningRequest originalRequest, FiscalSigningResult result)
        {
            return originalRequest != null && result != null && result.AttemptId == originalRequest.AttemptId &&
                result.State == FiscalSigningState.Unknown &&
                ((result.Diagnostic ?? "").StartsWith(RetryableDiagnostic,StringComparison.Ordinal) ||
                 LegacyRetryableDiagnostic(result.Diagnostic));
        }
        private static bool LegacyRetryableDiagnostic(string diagnostic)
        {
            string value = diagnostic ?? "";
            if (value.StartsWith("Signing was interrupted",StringComparison.Ordinal) ||
                value.StartsWith("The signing call did not return a confirmed outcome: HttpRequestException",StringComparison.Ordinal) ||
                value.StartsWith("The signing call did not return a confirmed outcome: TaskCanceledException",StringComparison.Ordinal) ||
                value.StartsWith("Unconfirmed device response: HttpRequestException",StringComparison.Ordinal) ||
                value.StartsWith("Unconfirmed device response: TaskCanceledException",StringComparison.Ordinal) ||
                value.StartsWith("Unconfirmed device response: OperationCanceledException",StringComparison.Ordinal))
                return true;
            const string statusPrefix = "Device HTTP status ";
            if (!value.StartsWith(statusPrefix,StringComparison.Ordinal)) return false;
            int end = value.IndexOf(';',statusPrefix.Length), status;
            return end > statusPrefix.Length && int.TryParse(value.Substring(statusPrefix.Length,
                end - statusPrefix.Length),out status) && (status == 408 || status == 429 || status >= 500);
        }

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
            var wire = Deserialize(request);
            var pos = wire.Doc.Payments == null ? null : wire.Doc.Payments.SingleOrDefault(p => p.Type == 1);
            if (pos != null && !string.Equals(pos.EftposTID as string, profile.EftPosTID, StringComparison.Ordinal))
                throw new InvalidOperationException("The Samtec EFTPOS terminal differs from the saved device profile.");
        }
        private static Request Deserialize(FiscalSigningRequest request) =>
            JsonConvert.DeserializeObject<Request>(new UTF8Encoding(false, true).GetString(request.Payload),
                new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Error });
        private static void ValidatePayload(FiscalSigningRequest request)
        {
            if (request.PayloadFormat != Format || request.Scope.SignerKey != Key)
                throw new InvalidOperationException("Unsupported Samtec payload format or signer.");
            // Reject unmodelled fields (including hidden device commands) before transport.
            var wire = Deserialize(request);
            var details = wire?.Doc?.InvoiceDetails;
            Guid uid;
            if (details == null || wire.JobType != "Sign" || wire.JobParams != null ||
                !Guid.TryParse(details.InvoiceUID, out uid) || uid != request.Document.DocumentId ||
                details.PrintDevice != 0 || details.ReqForToken != 0 || string.IsNullOrWhiteSpace(wire.AtxtContent))
                throw new InvalidOperationException("A rendered, matching sign-only document is required.");
            if (details.InvoiceType == 215 || details.CancelInvNo != null || details.CancelInvSeries != null ||
                (details.CancelInvType ?? 0) != 0 || (details.CancelDevDailyNum ?? 0) != 0)
                throw new InvalidOperationException("Cancellation requires verified parent device evidence and the cancellation workflow.");
            if (wire.Doc.Payments != null && wire.Doc.Payments.Any(p => p == null || p.EftposTransType != 0 ||
                (p.Type == 1 ? !(p.EftposTID is string) || string.IsNullOrWhiteSpace((string)p.EftposTID) :
                (p.Type != 0 && p.Type != 5) || p.EftposTID != null)))
                throw new InvalidOperationException("Invalid Samtec payment or EFTPOS fields.");
            switch (request.Document.Kind)
            {
                case FiscalDocumentKind.Invoice:
                    if (details.InvoiceType <= 0 || string.IsNullOrWhiteSpace(details.InvoiceNo) ||
                        wire.InputType != SamtecRequestBuilder.InputType(details.InvoiceType) ||
                        (wire.InputType == 1 && (wire.Doc.TransactionLines == null || wire.Doc.TransactionLines.Length == 0 ||
                            wire.Doc.Payments == null || wire.Doc.Payments.Length != 1)))
                        throw new InvalidOperationException("An issued invoice with matching lines and payment is required.");
                    foreach (var line in wire.Doc.TransactionLines ?? new Transactionline[0])
                    {
                        decimal gross = Math.Abs(line.GrossAmount);
                        decimal discount = Math.Abs(line.DMValue);
                        if (line.ItemAmount <= 0 || line.SaleQty <= 0 || line.NetAmount < 0 ||
                            line.VatAmount < 0 || gross <= 0 ||
                            decimal.Round(line.NetAmount + line.VatAmount,2) != gross ||
                            decimal.Round(line.ItemAmount * line.SaleQty,2) !=
                                decimal.Round(gross + discount,2) ||
                            (discount == 0 ? line.DMType != 0 : line.DMType != 1))
                            throw new InvalidOperationException(
                                "A Samtec transaction line must use VAT-inclusive ItemAmount and DMValue consistent with its gross total.");
                    }
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
                        {
                            int status = (int)response.StatusCode;
                            string diagnostic = "Device HTTP status " + status + "; signing outcome is unconfirmed.";
                            if (status == 408 || status == 429 || status >= 500)
                                diagnostic = RetryableDiagnostic + diagnostic;
                            return FiscalSigningResult.Unknown(request.AttemptId, raw, diagnostic);
                        }
                        return Parse(request, raw);
                    }
                }
            }
            catch (Exception ex)
            {
                string diagnostic = "Unconfirmed device response: " + ex.GetType().Name;
                // Once response text was received, a parser/contract failure is
                // not a transport retry. Preserve it for operator reconciliation.
                if (raw == null) diagnostic = RetryableDiagnostic + diagnostic;
                return FiscalSigningResult.Unknown(request.AttemptId, raw,diagnostic);
            }
        }
        public FiscalSigningResult RecoverRecorded(FiscalSigningRequest request, string recordedResponse,
            string recordedDiagnostic)
        {
            Validate(request);
            if (string.IsNullOrWhiteSpace(recordedResponse))
                return FiscalSigningResult.Unknown(request.AttemptId, recordedResponse,
                    "The recorded device response is empty.");
            if ((recordedDiagnostic ?? "").StartsWith(RetryableDiagnostic,StringComparison.Ordinal) ||
                (recordedDiagnostic ?? "").StartsWith("Automatic signing retries were exhausted",StringComparison.Ordinal))
                return FiscalSigningResult.Unknown(request.AttemptId,recordedResponse,recordedDiagnostic);
            try { return Parse(request, recordedResponse); }
            catch (Exception ex)
            {
                return FiscalSigningResult.Unknown(request.AttemptId, recordedResponse,
                    "The recorded device response remains unconfirmed: " + ex.GetType().Name);
            }
        }

        // A definitive rejection permits a new attempt. Recalculate the Samtec
        // discount from the immutable wire values so older malformed requests can
        // be retried without reading mutable invoice data.
        public static FiscalSigningRequest CorrectLineAmountsForRetry(FiscalSigningRequest rejected)
        {
            if (rejected == null || rejected.PayloadFormat != Format || rejected.Scope.SignerKey != Key)
                throw new ArgumentException("A saved Samtec signing request is required.");
            var wire = Deserialize(rejected);
            foreach (var line in wire.Doc?.TransactionLines ?? new Transactionline[0])
            {
                decimal gross = Math.Abs(line.GrossAmount);
                decimal preDiscountGross = decimal.Round(line.ItemAmount * line.SaleQty, 2);
                decimal discount = preDiscountGross - gross;
                if (discount < 0)
                    throw new InvalidOperationException("A Samtec line gross total exceeds its rounded quantity and retail unit price.");
                int sign = line.GrossAmount < 0 ? -1 : 1;
                line.DMType = discount > 0 ? 1 : 0;
                line.DMValue = discount > 0 ? sign * discount : 0;
            }
            return Prepare(rejected.Document, rejected.Scope, wire);
        }
        private static FiscalSigningResult Parse(FiscalSigningRequest request, string raw)
        {
            var response = JObject.Parse(raw);
            string uid = (string)response["InvoiceUID"], job = (string)response["JobType"], code = (string)response["ResultCode"];
            Guid parsed;
            if ((uid != null && (!Guid.TryParse(uid, out parsed) || parsed != request.Document.DocumentId)) ||
                (job != null && job != "Sign"))
                return FiscalSigningResult.Unknown(request.AttemptId, raw, "Device response belongs to a different document or operation.");
            bool attributed = job == "Sign" && Guid.TryParse(uid, out parsed) && parsed == request.Document.DocumentId;
            if (attributed && IsDuplicateInvoiceUid(code))
            {
                string signature = FirstText(response,"Signature","FiscalSignature","Sign");
                if (ValidSignature(signature))
                    return FiscalSigningResult.Signed(request.AttemptId,signature,
                        FirstText(response,"QrCode","QRCode","QrCodeUrl"),uid,raw);
                return FiscalSigningResult.Unknown(request.AttemptId,raw,
                    "The device reports a duplicate InvoiceUID, which is evidence that the original request was signed, " +
                    "but the original signature was not returned. Automatic retries stopped; operator recovery is required.");
            }
            if (attributed && IsTransientRejection(code))
                return FiscalSigningResult.Unknown(request.AttemptId,raw,
                    RetryableDiagnostic + "The device temporarily rejected the matching request: " +
                    (code ?? "unknown response"));
            if (attributed && !string.IsNullOrWhiteSpace(code) &&
                (code.StartsWith("Error", StringComparison.OrdinalIgnoreCase) ||
                 code.IndexOf("ReplyCodeError", StringComparison.OrdinalIgnoreCase) >= 0))
                return FiscalSigningResult.Rejected(request.AttemptId, raw,
                    "The device explicitly rejected the matching signing request: " + code.Split(',')[0].Trim());
            var parts = (code ?? "").Split(',');
            if (parts.Length < 2 || parts[0].Trim() != "ΔΦΣΣ" || string.IsNullOrWhiteSpace(parts[1]) ||
                parts[1].Trim() == "-" || parts[1].Trim() == "Sign Device A" || code.IndexOf("Error", StringComparison.OrdinalIgnoreCase) >= 0)
                return FiscalSigningResult.Unknown(request.AttemptId, raw, "Device reply contains no confirmed fiscal signature.");
            string qr = null; Uri uri;
            if (parts.Length > 2 && Uri.TryCreate(parts[2].Trim(), UriKind.Absolute, out uri) && uri.Scheme == "https") qr = parts[2].Trim();
            return FiscalSigningResult.Signed(request.AttemptId, parts[1].Trim(), qr, uid, raw);
        }
        private static bool IsTransientRejection(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return false;
            string value = code.ToLowerInvariant();
            return value.Contains("17/f0/02") || value.Contains("χρήση μενού") ||
                value.Contains("χρηση μενου") || value.Contains("device busy") ||
                value.Contains("temporar") || value.Contains("try again") ||
                value.Contains("προσωριν") || value.Contains("απασχολ");
        }
        private static bool IsDuplicateInvoiceUid(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return false;
            string value = code.ToLowerInvariant();
            bool duplicate = value.Contains("duplicate") || value.Contains("already exists") ||
                value.Contains("already signed") || value.Contains("already been signed") ||
                value.Contains("ήδη υπάρχει") || value.Contains("ηδη υπαρχει") ||
                value.Contains("ήδη υπογραφ") || value.Contains("ηδη υπογραφ");
            return duplicate && (value.Contains("invoiceuid") || value.Contains("invoice uid") ||
                value.Contains("invoice id") || value.Contains("παραστατικ"));
        }
        private static string FirstText(JObject response, params string[] names)
        {
            foreach (string name in names)
            {
                var property = response.Properties().FirstOrDefault(item =>
                    string.Equals(item.Name,name,StringComparison.OrdinalIgnoreCase));
                string value = property == null ? null : (string)property.Value;
                if (!string.IsNullOrWhiteSpace(value)) return value.Trim();
            }
            return null;
        }
        private static bool ValidSignature(string value) => !string.IsNullOrWhiteSpace(value) &&
            value.Trim() != "-" && value.Trim() != "Sign Device A";
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
