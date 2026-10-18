using Exedron.Invoicing.Core;
using Exedron.Invoicing.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace Exedron.Invoicing.Adapters
{
    // Direct Ilyda API; does not call the Arbitrans singleton or initiate POS payments.
    public sealed partial class IlydaInvoiceProvider : IInvoiceProvider, IInvoiceProviderRecovery,
        IInvoiceProviderPaymentSigner, IInvoiceProviderPaymentLifecycle, IInvoiceDocumentUpload
    {
        private readonly IlydaProviderOptions options;
        private readonly HttpClient client;
        public IlydaInvoiceProvider(IlydaProviderOptions options, HttpMessageHandler handler = null)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            if (options.XmlSerializer == null) options.XmlSerializer = new InvoiceXmlSerializer();
            client = LoggedHttpClient.Create(options.Logger, InvoiceProviderCatalog.DirectIlydaKey, options.TestMode ? "Test" : "Production", handler);
            client.Timeout = options.Timeout > TimeSpan.Zero ? options.Timeout : TimeSpan.FromSeconds(30);
        }
        public InvoiceProviderDefinition Descriptor => InvoiceProviderCatalog.Get(InvoiceProviderCatalog.DirectIlydaKey);
        private string BaseUrl => options.TestMode ? "https://test.vs.gr" : "https://vs.gr";

        public InvoiceResponse SendInvoice(InvoiceModel invoice, Guid invoiceId, ProviderPaymentContext payment)
        {
            try
            {
                ValidateConfiguration();
                if (invoice?.Issuer?.VATNumber != options.IssuerVat)
                    return Error("Ilyda: Το ΑΦΜ εκδότη δεν συμφωνεί με τις ρυθμίσεις.");
                if ((invoice.PaymentMethods ?? new PaymentMethod[0]).Any(p => (int)p.Type == 8))
                    return Error("Ilyda: Το εγχειρίδιο A1155 POS δεν τεκμηριώνει πληρωμή IRIS.");
                if (payment == null && (invoice.PaymentMethods ?? new PaymentMethod[0]).Any(p => (int)p.Type == 7))
                    return Error("Ilyda: Η πληρωμή POS πρέπει να ολοκληρωθεί μέσω GBridge πριν από τη διαβίβαση.");
                var pendingSignature = options.PosStore?.Load(invoiceId);
                if (payment == null && pendingSignature != null &&
                    pendingSignature.State != "Cancelled" && pendingSignature.State != "Rejected")
                    return Error("Ilyda: Υπάρχει προηγούμενη υπογραφή POS. Ελέγξτε την πληρωμή και ακυρώστε τη μη χρησιμοποιημένη υπογραφή πριν αλλάξετε τρόπο πληρωμής.");
                var payload = IlydaInvoiceMapper.Build(invoice, options.XmlSerializer.SerializeForProvider());
                if (payment != null) ApplyPosPayment(payload, invoice, invoiceId, payment);
                if (payment != null && IsDeferredPayment(invoiceId)) return SendExistingInvoicePayment(invoice, invoiceId, payload);
                var json = payload.ToString(Formatting.None);
                using (options.Store.Acquire(invoiceId))
                {
                    var previous = options.Store.Load(invoiceId);
                    if (previous != null)
                    {
                        CheckScope(previous);
                        if (IsConfirmedCounterpartRejection(previous))
                        {
                            // Older versions treated the accompanying I0003 wrapper as
                            // an uncertain result, hiding AADE's explicit validation rejection.
                            previous.State = "Rejected";
                            options.Store.Save(previous);
                        }
                        if ((string)JObject.Parse(previous.RequestJson)["invoiceNumber"] != (string)payload["invoiceNumber"] &&
                            !CanCorrectRejectedDeliveryType(previous, JObject.Parse(json)))
                            return Error("Ilyda: Η φορολογική ταυτότητα άλλαξε μετά την προηγούμενη αποστολή. Απαιτείται έλεγχος.");
                        if (previous.State != "Rejected")
                        {
                            if (!JToken.DeepEquals(JObject.Parse(previous.RequestJson), JObject.Parse(json)))
                                return Error("Ilyda: Τα στοιχεία άλλαξαν μετά την αποστολή. Ανακτήστε την προηγούμενη έκδοση πριν από οποιαδήποτε διόρθωση.");
                            return Recover(previous);
                        }
                    }
                    var attempt = new IlydaSubmission { AttemptId = Guid.NewGuid(), InvoiceId = invoiceId,
                        TestMode = options.TestMode, IssuerVat = options.IssuerVat, RequestJson = json, State = "Sending" };
                    options.Store.Save(attempt); // A crash from here onward is an unknown outcome, never a rejection.
                    try
                    {
                        using (var response = Request(HttpMethod.Post, "/api/invoice", json))
                        {
                            var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                            attempt.ResponseJson = body;
                            JObject data;
                            try { data = JObject.Parse(body); }
                            catch (JsonException) { attempt.State = "Unknown"; options.Store.Save(attempt); return Error("Ilyda: Μη έγκυρη απάντηση. Η έκδοση δεν επιβεβαιώθηκε."); }
                            attempt.ResponseJson = body;
                            attempt.InvoiceUid = (string)(data["invoiceMarking"] as JObject)?["invoiceIdentifier"];
                            var result = MapResponse(data);
                            if (Errors(data).Any(e => (string)e["code"] == "ER-30" && (bool?)e["fatal"] == true))
                            {
                                var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("GTB Standard Time"));
                                result.Errors = result.Errors.Concat(new[] {
                                    "Ilyda: invoiceIssueDate=" + (string)payload["invoiceIssueDate"] +
                                    "; invoiceNumber=" + (string)payload["invoiceNumber"] +
                                    "; ώρα Ελλάδας=" + IlydaInvoiceMapper.FormatIssueDate(now) +
                                    ". Δεν άλλαξε η ημερομηνία του παραστατικού." }).ToArray();
                            }
                            bool queued = Errors(data).Any(e => (string)e["code"] == "MQ001" || (string)e["code"] == "MQ002");
                            // Only a structured validation rejection with no fiscal marking proves rejection.
                            bool validationStatus = response.IsSuccessStatusCode || (int)response.StatusCode == 400 || (int)response.StatusCode == 422;
                            bool rejected = validationStatus && (IsCounterpartValidation(data) || (data["invoiceMarking"]?.Type == JTokenType.Null &&
                                Errors(data).Any(e => (bool?)e["fatal"] == true) &&
                                !Errors(data).Any(e => ((string)e["code"] ?? "").StartsWith("I", StringComparison.Ordinal))));
                            attempt.State = queued ? "Queued" : !result.HasErrors && response.IsSuccessStatusCode ? "Accepted" : rejected ? "Rejected" : "Unknown";
                            options.Store.Save(attempt);
                            if (!response.IsSuccessStatusCode) return Error("Ilyda HTTP " + (int)response.StatusCode + ": " + string.Join("; ", result.Errors));
                            return result;
                        }
                    }
                    catch (Exception ex) when (ex is HttpRequestException || ex is System.Threading.Tasks.TaskCanceledException)
                    {
                        attempt.State = "Unknown"; options.Store.Save(attempt);
                        return Error("Ilyda: Η σύνδεση διακόπηκε χωρίς επιβεβαίωση. Δεν έγινε αυτόματη επανυποβολή. Ελέγξτε την προηγούμενη αποστολή στον πάροχο.");
                    }
                }
            }
            catch (Exception ex) { return Error(Redact(ex.Message)); }
        }

        public InvoiceResponse RecoverInvoice(Guid invoiceId)
        {
            try
            {
                ValidateConfiguration();
                using (options.Store.Acquire(invoiceId))
                {
                    var attempt = options.Store.Load(invoiceId);
                    if (attempt == null) return Error("Ilyda: Δεν υπάρχει αποθηκευμένη προσπάθεια για το παραστατικό.");
                    CheckScope(attempt);
                    return Recover(attempt);
                }
            }
            catch (Exception ex) { return Error(Redact(ex.Message)); }
        }

        private InvoiceResponse Recover(IlydaSubmission attempt)
        {
            if (attempt.State == "Accepted") return MapResponse(JObject.Parse(attempt.ResponseJson));
            if ((attempt.State != "Queued" && attempt.State != "QueueFailed") || string.IsNullOrWhiteSpace(attempt.InvoiceUid))
                return Error("Ilyda: Δεν έχει επιβεβαιωθεί η έκδοση. Το διαθέσιμο API ανάκτησης αφορά μόνο την ουρά offline. Απαιτείται έλεγχος στον πάροχο πριν από νέα αποστολή.");
            using (var response = Request(HttpMethod.Get, "/api/invoice/pending/by-uid/" + Uri.EscapeDataString(attempt.InvoiceUid), null))
            {
                // A missing queue entry says nothing about normally issued invoices. Never authorize a resend.
                if (!response.IsSuccessStatusCode) return Error("Ilyda: Απέτυχε ο έλεγχος ουράς (HTTP " + (int)response.StatusCode + "). Δεν έγινε νέα αποστολή.");
                var entry = JObject.Parse(response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                var original = JObject.Parse(attempt.RequestJson);
                if (!string.Equals((string)entry["uid"], attempt.InvoiceUid, StringComparison.OrdinalIgnoreCase) ||
                    (string)entry["sellerVatIdentifier"] != attempt.IssuerVat || (string)entry["invoiceNumber"] != (string)original["invoiceNumber"])
                    return Error("Ilyda: Η απάντηση ουράς αφορά διαφορετικό παραστατικό. Απαιτείται έλεγχος.");
                attempt.QueueState = (string)entry["invoiceState"];
                attempt.QueueErrors = (string)entry["errorsJson"];
                attempt.QueueResponseJson = entry.ToString(Formatting.None);
                if (attempt.QueueState != "SUBMITTED")
                {
                    bool terminal = attempt.QueueState == "SUBMISSION_ERRORS" || attempt.QueueState == "MAX_RETRIES_REACHED";
                    attempt.State = terminal ? "QueueFailed" : "Queued";
                    options.Store.Save(attempt);
                    return Error(terminal
                        ? "Ilyda: Απαιτείται παρέμβαση στον πάροχο (" + attempt.QueueState + "). " + attempt.QueueErrors + " Δεν επιτρέπεται νέα έκδοση."
                        : "Ilyda: Το παραστατικό παραμένει στην ουρά διαβίβασης. Δεν έγινε νέα αποστολή.");
                }
                var saved = JObject.Parse(attempt.ResponseJson);
                var marking = (JObject)saved["invoiceMarking"];
                marking["mark"] = entry["mark"];
                marking["verificationHash"] = entry["verificationHash"];
                saved["errors"] = new JArray();
                var result = MapResponse(saved);
                if (!result.HasErrors)
                {
                    attempt.ResponseJson = saved.ToString(Formatting.None); attempt.State = "Accepted"; options.Store.Save(attempt);
                }
                else options.Store.Save(attempt);
                return result;
            }
        }

        private void CheckScope(IlydaSubmission saved)
        {
            if (saved.TestMode != options.TestMode || saved.IssuerVat != options.IssuerVat)
                throw new InvalidOperationException("Ilyda: Διατηρήστε το αρχικό περιβάλλον και ΑΦΜ για την ανάκτηση / επανυποβολή.");
        }
        public void UploadInvoiceDocument(Guid invoiceId, byte[] pdf)
        {
            ValidateConfiguration();
            using (options.Store.Acquire(invoiceId))
            {
                var saved = options.Store.Load(invoiceId);
                if (saved == null || saved.State != "Accepted") throw new InvalidOperationException("Ilyda: Το PDF ανεβαίνει μόνο μετά από επιβεβαιωμένη έκδοση.");
                CheckScope(saved);
                if (saved.PdfUploaded) return;
                var providerId = (string)JObject.Parse(saved.ResponseJson)["invoiceMarking"]?["invoiceId"];
                if (string.IsNullOrWhiteSpace(providerId)) throw new InvalidOperationException("Ilyda: Δεν επιστράφηκε εσωτερικό invoiceId για την αναφόρτωση PDF. Η έκδοση έχει επιβεβαιωθεί· απαιτείται έλεγχος του PDF στον πάροχο.");
                if (pdf == null || pdf.Length < 5 || Encoding.ASCII.GetString(pdf, 0, 5) != "%PDF-") throw new InvalidOperationException("Ilyda: Το έγγραφο πρέπει να είναι PDF.");
                using (var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl + "/api/invoice/upload/" + Uri.EscapeDataString(providerId)))
                {
                    Authenticate(request);
                    var content = new MultipartFormDataContent();
                    var file = new ByteArrayContent(pdf); file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
                    content.Add(file, "FileUpload", invoiceId.ToString("N") + ".pdf"); request.Content = content;
                    using (var response = client.SendAsync(request).GetAwaiter().GetResult())
                    {
                        if (!response.IsSuccessStatusCode) throw new InvalidOperationException("Ilyda: Αποτυχία αναφόρτωσης PDF (HTTP " + (int)response.StatusCode + ").");
                        var reply = JObject.Parse(response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                        if (Errors(reply).Any(e => (bool?)e["fatal"] == true)) throw new InvalidOperationException("Ilyda PDF: " + Redact(string.Join("; ", Errors(reply).Select(Message))));
                        saved.PdfUploaded = true; options.Store.Save(saved);
                    }
                }
            }
        }
        public static bool IsConfirmedCounterpartRejection(IlydaSubmission attempt)
        {
            if (attempt == null || (attempt.State != "Unknown" && attempt.State != "Rejected") ||
                !string.IsNullOrWhiteSpace(attempt.InvoiceUid) || string.IsNullOrWhiteSpace(attempt.ResponseJson)) return false;
            try { return IsCounterpartValidation(JObject.Parse(attempt.ResponseJson)); }
            catch (JsonException) { return false; }
        }

        private static bool IsCounterpartValidation(JObject response)
        {
            var errors = Errors(response);
            return response["invoiceMarking"]?.Type == JTokenType.Null &&
                errors.Any(e => (string)e["code"] == "204" && (bool?)e["fatal"] == true &&
                    (string)e["statusCode"] == "ValidationError" &&
                    ((string)e["aadeMessage"] ?? "").Trim() == "Counterpart is mandatory for this invoice type") &&
                !errors.Any(e => ((string)e["code"] ?? "").StartsWith("MQ", StringComparison.Ordinal)) &&
                errors.Where(e => (bool?)e["fatal"] == true).All(e =>
                    (string)e["code"] == "I0003" || ((string)e["code"] == "204" && (string)e["statusCode"] == "ValidationError"));
        }

        private static bool CanCorrectRejectedDeliveryType(IlydaSubmission previous, JObject current)
        {
            // Permit only the diagnosed 9.3 -> 9.1 correction after a definitive
            // MDP-0090 rejection. Never change the identity of an uncertain/issued invoice.
            if (previous.State != "Rejected" || string.IsNullOrWhiteSpace(previous.ResponseJson)) return false;
            try
            {
                var response = JObject.Parse(previous.ResponseJson);
                if (response["invoiceMarking"]?.Type != JTokenType.Null ||
                    !Errors(response).Any(e => (string)e["code"] == "MDP-0090" && (bool?)e["fatal"] == true)) return false;
                var original = JObject.Parse(previous.RequestJson);
                var identity = ((string)original["invoiceNumber"] ?? "").Split('|');
                var replacement = ((string)current["invoiceNumber"] ?? "").Split('|');
                if (identity.Length != 6 || replacement.Length != 6 || identity[3] != "9.3" || replacement[3] != "9.1") return false;
                identity[3] = "9.1";
                if (string.Join("|", identity) != (string)current["invoiceNumber"] ||
                    (decimal?)original["docTotal"]?["invoiceTotalAmountWithVat"] != 0m ||
                    (original["paymentMethods"] as JArray)?.Count != 0 ||
                    ((original["precedingInvoices"] as JArray)?.Count ?? 0) == 0) return false;
                var attachment = (original["additionalSupportDocs"] as JArray)?.SingleOrDefault(d => (string)d["description"] == "##InvoicesDoc##");
                if (attachment == null) return false;
                var xml = System.Xml.Linq.XDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String((string)attachment["reference"])));
                System.Xml.Linq.XNamespace ns = "http://www.aade.gr/myDATA/invoice/v1.0";
                var header = xml.Root?.Element(ns + "invoice")?.Element(ns + "invoiceHeader");
                if (header?.Element(ns + "invoiceType")?.Value != "9.3" || !header.Elements(ns + "correlatedInvoices").Any()) return false;
                header.Element(ns + "invoiceType").Value = "9.1";
                attachment["reference"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(xml.ToString(System.Xml.Linq.SaveOptions.DisableFormatting)));
                original["invoiceNumber"] = current["invoiceNumber"].DeepClone();
                // This also protects the parent MARK, series, number, date, parties,
                // quantities, prices and all other fields from incidental edits.
                return JToken.DeepEquals(original, current);
            }
            catch (Exception ex) when (ex is JsonException || ex is FormatException || ex is System.Xml.XmlException || ex is InvalidOperationException)
            {
                return false;
            }
        }

        private void ValidateConfiguration()
        {
            if (string.IsNullOrWhiteSpace(options.ApiKey) && (string.IsNullOrWhiteSpace(options.UserName) || string.IsNullOrWhiteSpace(options.Password)))
                throw new InvalidOperationException("Ilyda: Συμπληρώστε API key ή όνομα χρήστη και κωδικό για το επιλεγμένο περιβάλλον.");
            if (options.Store == null || options.XmlSerializer.SerializeForProvider() == null || string.IsNullOrWhiteSpace(options.IssuerVat))
                throw new InvalidOperationException("Ilyda: Δεν ολοκληρώθηκε η ρύθμιση του παρόχου.");
        }
        private HttpResponseMessage Request(HttpMethod method, string path, string json)
        {
            using (var request = new HttpRequestMessage(method, path.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ? path : BaseUrl + path))
            {
                Authenticate(request);
                if (json != null) request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                return client.SendAsync(request).GetAwaiter().GetResult();
            }
        }
        private void Authenticate(HttpRequestMessage request)
        {
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            if (!string.IsNullOrWhiteSpace(options.ApiKey)) request.Headers.Add("X-Auth-Key", options.ApiKey);
            else request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(options.UserName + ":" + options.Password)));
        }
        private string Redact(string message)
        {
            foreach (var secret in new[] { options.ApiKey, options.Password })
                if (!string.IsNullOrEmpty(secret)) message = message.Replace(secret, "[redacted]");
            return message;
        }
        internal static JArray Errors(JObject data) => data["errors"] as JArray ?? new JArray();
        public static InvoiceResponse MapResponse(JObject data)
        {
            var marking = data["invoiceMarking"] as JObject;
            var errors = Errors(data);
            var fatal = errors.Where(e => (bool?)e["fatal"] == true).Select(Message).ToArray();
            bool queued = errors.Any(e => (string)e["code"] == "MQ001" || (string)e["code"] == "MQ002");
            long mark;
            bool accepted = !queued && fatal.Length == 0 && marking != null &&
                long.TryParse((string)marking["mark"], out mark) && mark > 0 &&
                !string.IsNullOrWhiteSpace((string)marking["invoiceIdentifier"]) && !string.IsNullOrWhiteSpace((string)marking["verificationHash"]);
            string qr = (string)marking?["qrCode"];
            return new InvoiceResponse { Mark = (string)marking?["mark"], Uid = (string)marking?["invoiceIdentifier"],
                VerificationHash = (string)marking?["verificationHash"], InvoiceSignature = (string)marking?["verificationHash"],
                QrCodeUrl = qr, InvoiceUrl = qr, Url = qr, ProviderUrl = (string)marking?["providerUrl"],
                ProviderInvoiceId = (string)marking?["invoiceId"],
                TransmissionFailure2 = queued,
                Errors = accepted ? new string[0] : fatal.Length > 0 ? fatal : new[] { queued
                    ? "Ilyda: Το παραστατικό βρίσκεται στην ουρά offline (MQ001/MQ002). Αναμένεται MARK· μην το επανυποβάλετε."
                    : "Ilyda: Δεν επιστράφηκε έγκυρο MARK, UID και κωδικός αυθεντικοποίησης." },
                Warnings = errors.Where(e => (bool?)e["fatal"] != true).Select(Message).ToArray() };
        }
        private static string Message(JToken e) => (string)e["code"] + ": " +
            (!string.IsNullOrWhiteSpace((string)e["defaultMessage"]) ? (string)e["defaultMessage"] :
             !string.IsNullOrWhiteSpace((string)e["aadeMessage"]) ? (string)e["aadeMessage"] : "Σφάλμα παρόχου");
        private static InvoiceResponse Error(string message) => new InvoiceResponse { Errors = new[] { message }, Warnings = new string[0] };
        public void Dispose() { client.Dispose(); }

        public static string PrepareRequestJson(InvoiceModel invoice, Func<InvoiceModel, string> xmlFactory)
        {
            return IlydaInvoiceMapper.Build(invoice, xmlFactory).ToString(Formatting.None);
        }
    }
}
