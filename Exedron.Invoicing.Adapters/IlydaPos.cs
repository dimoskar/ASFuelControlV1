using Exedron.Invoicing.Core;
using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Xml.Linq;
using Exedron.Invoicing.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Exedron.Invoicing.Adapters
{
    public sealed partial class IlydaInvoiceProvider
    {
        // v1.3.2: DEFAULT is the EDPS format. Preserve the original provider value for invoice submission.
        public static readonly string[] PosProtocols = { "DEFAULT", "EDPS", "MELLON", "EPAY", "NEXI", "CARDLINK", "VIVA", "NEOSOFT", "ATTICA", "WORLDLINE" };

        public IDisposable AcquirePayment(Guid invoiceId)
        {
            if (options.PosStore == null) throw new InvalidOperationException("Ilyda: Δεν έχει ρυθμιστεί η αποθήκευση συναλλαγών POS.");
            return options.PosStore.Acquire(invoiceId);
        }

        public ProviderPaymentSignature CreatePaymentSignature(InvoiceModel invoice)
        {
            throw new InvalidOperationException("Ilyda: Χρησιμοποιήστε τη ροή POS με αποθήκευση της υπογραφής ανά παραστατικό.");
        }

        private string SigningEndpoint()
        {
            var value = string.IsNullOrWhiteSpace(options.SignatureUrl) ? BaseUrl + "/api/invoice/sign" : options.SignatureUrl.Trim();
            Uri uri;
            if (!Uri.TryCreate(value, UriKind.Absolute, out uri) || uri.Scheme != "https" ||
                !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Fragment) || !string.IsNullOrEmpty(uri.Query) ||
                uri.AbsolutePath.TrimEnd('/') == "/api/invoice")
                throw new InvalidOperationException("Ilyda: Απαιτείται HTTPS URL υπογραφής /api/invoice/sign, όχι το URL έκδοσης παραστατικών.");
            if ((options.TestMode && uri.Host.Equals("vs.gr", StringComparison.OrdinalIgnoreCase)) ||
                (!options.TestMode && uri.Host.Equals("test.vs.gr", StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Ilyda: Το URL υπογραφής αφορά διαφορετικό περιβάλλον.");
            return uri.AbsoluteUri.TrimEnd('/');
        }

        private JObject SignatureRequest(InvoiceModel invoice, Guid invoiceId)
        {
            ValidateConfiguration();
            // Complete local invoice validation BEFORE creating an externally tracked signature.
            IlydaInvoiceMapper.Build(invoice, options.XmlSerializer.SerializeForProvider());
            if (!string.Equals(invoice.InvoiceHeader.Currency, "EUR", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Ilyda/GBridge: Υποστηρίζεται πληρωμή POS μόνο σε EUR.");
            if (invoice.Issuer.VATNumber != options.IssuerVat) throw new InvalidOperationException("Ilyda: Διαφορετικό ΑΦΜ εκδότη.");
            if (string.IsNullOrWhiteSpace(options.PosTerminalId)) throw new InvalidOperationException("Ilyda: Απαιτείται TID του POS.");
            string protocol = string.IsNullOrWhiteSpace(options.NspProtocol) ? "DEFAULT" : options.NspProtocol.Trim().ToUpperInvariant();
            if (!PosProtocols.Contains(protocol)) throw new InvalidOperationException("Ilyda: Μη έγκυρο πρωτόκολλο POS.");
            if (protocol == "EDPS") protocol = "DEFAULT";
            var payments = (invoice.PaymentMethods ?? new PaymentMethod[0]).Where(p => (int)p.Type == 7 || (int)p.Type == 8).ToArray();
            if (payments.Length != 1 || (int)payments[0].Type != 7)
                throw new InvalidOperationException("Ilyda: Υποστηρίζεται μία πληρωμή κάρτας POS ανά συναλλαγή. Δεν τεκμηριώνεται IRIS.");
            var totals = invoice.InvoiceSummary;
            if (new[] { totals.TotalNetValue, totals.TotalVATAmount, totals.TotalGrossValue }.Any(v => v != decimal.Round(v, 2)))
                throw new InvalidOperationException("Ilyda: Τα ποσά υπογραφής απαιτούν έως δύο δεκαδικά ψηφία.");
            decimal amount = payments[0].Amount;
            if (amount <= 0 || amount > totals.TotalGrossValue || amount != decimal.Round(amount, 2))
                throw new InvalidOperationException("Ilyda: Μη έγκυρο ποσό πληρωμής POS.");
            decimal vatRate = 0;
            if (protocol == "VIVA" || protocol == "WORLDLINE")
            {
                var rates = invoice.InvoiceDetails.Select(l => (int)l.VATCategory).Distinct().ToArray();
                if (rates.Length != 1) throw new InvalidOperationException("Ilyda: Απαιτείται ένας συντελεστής ΦΠΑ για το επιλεγμένο πρωτόκολλο POS.");
                vatRate = IlydaInvoiceMapper.VatRate(rates[0]);
            }
            var existing = IsDeferredPayment(invoiceId) ? ExistingInvoice(invoice, invoiceId) : null;
            return new JObject {
                ["amount"] = amount, ["netAmount"] = totals.TotalNetValue, ["vatAmount"] = totals.TotalVATAmount,
                ["grossAmount"] = totals.TotalGrossValue, ["vatRate"] = vatRate, ["sellerVat"] = options.IssuerVat,
                ["series"] = invoice.InvoiceHeader.Series, ["invoiceTypeCode"] = invoice.InvoiceHeader.InvoiceType,
                ["sellerBranch"] = invoice.Issuer.Branch, ["terminalId"] = options.PosTerminalId.Trim(),
                ["nspProtocol"] = protocol, ["mark"] = existing != null
                    ? (string)JObject.Parse(existing.ResponseJson)["invoiceMarking"]["mark"] : null
            };
        }

        public ProviderPaymentSignature PreparePaymentSignature(InvoiceModel invoice, Guid invoiceId, bool authorizationMayStart)
        {
            var request = SignatureRequest(invoice, invoiceId);
            var endpoint = SigningEndpoint();
            var saved = options.PosStore.Load(invoiceId);
            if (saved != null)
            {
                CheckPosScope(saved);
                if (saved.State == "CancelPending" && authorizationMayStart)
                {
                    CancelUnusedPaymentSignature(invoiceId, saved.CancelReason);
                    saved = options.PosStore.Load(invoiceId);
                }
                if (saved.State != "Cancelled" && saved.State != "Rejected" && saved.State != "Expired" && !SameJson(JObject.Parse(saved.RequestJson), request))
                    throw new InvalidOperationException("Ilyda: Τα ποσά, το POS ή η ταυτότητα παραστατικού άλλαξαν. Απαιτείται έλεγχος/ακύρωση της προηγούμενης υπογραφής πριν από νέα πληρωμή.");
                if (saved.State == "Requesting" || saved.State == "Unknown" || saved.State == "CancelPending")
                    throw new InvalidOperationException("Ilyda: Η προηγούμενη αίτηση/ακύρωση υπογραφής δεν επιβεβαιώθηκε. Απαιτείται έλεγχος στον πάροχο πριν από νέα αίτηση.");
                if (saved.State == "Ready")
                {
                    var data = SignatureData(saved);
                    if (!authorizationMayStart || SignatureExpiry(data) > DateTimeOffset.UtcNow.AddSeconds(30))
                        return PaymentSignature(invoice, data);
                    // Expired, never-dispatched signatures must not authorize a fresh payment.
                    saved.State = "Expired"; options.PosStore.Save(saved);
                }
                if (!authorizationMayStart) throw new InvalidOperationException("Ilyda: Δεν υπάρχει διαθέσιμη αποθηκευμένη υπογραφή για ανάκτηση πληρωμής.");
            }
            if (!authorizationMayStart) throw new InvalidOperationException("Ilyda: Η αποθηκευμένη υπογραφή της πληρωμής λείπει.");
            // An existing/unknown invoice submission must never trigger another payment.
            var submission = options.Store.Load(invoiceId);
            if (!IsDeferredPayment(invoiceId) && submission != null && submission.State != "Rejected")
                throw new InvalidOperationException("Ilyda: Υπάρχει προηγούμενη αποστολή παραστατικού. Ανακτήστε/ελέγξτε την έκδοση πριν από νέα πληρωμή.");
            if (IsDeferredPayment(invoiceId)) ExistingInvoice(invoice, invoiceId);
            saved = new IlydaPosSession { Id = Guid.NewGuid(), InvoiceId = invoiceId, TestMode = options.TestMode,
                IssuerVat = options.IssuerVat, Endpoint = endpoint, RequestJson = request.ToString(Formatting.None), State = "Requesting" };
            options.PosStore.Save(saved);
            try
            {
                using (var response = Request(HttpMethod.Post, endpoint, saved.RequestJson))
                {
                    saved.ResponseJson = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    options.PosStore.Save(saved);
                    var body = JObject.Parse(saved.ResponseJson);
                    var signatures = body["invoiceSignatures"] as JArray;
                    if (signatures == null || signatures.Count != 1 || !response.IsSuccessStatusCode || Errors(body).Count != 0)
                    {
                        // Only an explicit validation failure with no issued signature permits a new request.
                        bool rejected = (response.IsSuccessStatusCode || (int)response.StatusCode == 400 || (int)response.StatusCode == 422) &&
                            (signatures == null || signatures.Count == 0) && Errors(body).Any(e => e is JObject && (bool?)e["fatal"] == true);
                        saved.State = rejected ? "Rejected" : "Unknown"; options.PosStore.Save(saved);
                        if (rejected) throw new PaymentSignatureRejectedException();
                        throw new InvalidOperationException("Ilyda: Δεν επιβεβαιώθηκε η υπογραφή POS. " + Redact(Errors(body).ToString(Formatting.None)));
                    }
                    var signature = SignatureData(saved);
                    ValidateSignature(signature, request);
                    saved.State = "Ready"; options.PosStore.Save(saved);
                    return PaymentSignature(invoice, signature);
                }
            }
            catch
            {
                if (saved.State == "Requesting") { saved.State = "Unknown"; options.PosStore.Save(saved); }
                throw;
            }
        }

        private void CheckPosScope(IlydaPosSession saved)
        {
            if (saved.TestMode != options.TestMode || saved.IssuerVat != options.IssuerVat || saved.Endpoint != SigningEndpoint())
                throw new InvalidOperationException("Ilyda: Διατηρήστε το αρχικό περιβάλλον, ΑΦΜ και URL της υπογραφής.");
        }
        private static JObject SignatureData(IlydaPosSession saved)
        {
            var items = JObject.Parse(saved.ResponseJson)["invoiceSignatures"] as JArray;
            if (items == null || items.Count != 1 || !(items[0] is JObject)) throw new InvalidOperationException("Ilyda: Μη έγκυρη απάντηση υπογραφής.");
            return (JObject)items[0];
        }
        private static DateTimeOffset SignatureExpiry(JObject signature)
        {
            var value = signature["signatureExpirationDate"];
            if (value?.Type == JTokenType.Integer) return DateTimeOffset.FromUnixTimeMilliseconds((long)value);
            DateTimeOffset expiry;
            if (DateTimeOffset.TryParse((string)value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out expiry)) return expiry;
            throw new InvalidOperationException("Ilyda: Δεν επιστράφηκε έγκυρη λήξη υπογραφής.");
        }
        private static void ValidateSignature(JObject data, JObject request)
        {
            foreach (string field in new[] { "signature", "signedContent", "uid", "terminalId" })
                if (string.IsNullOrWhiteSpace((string)data[field])) throw new InvalidOperationException("Ilyda: Λείπει το πεδίο υπογραφής " + field);
            if ((string)data["signingAuthor"] != InvoiceProviderCatalog.Get(InvoiceProviderCatalog.DirectIlydaKey).AadeProviderCode || (string)data["terminalId"] != (string)request["terminalId"] ||
                (bool?)data["cancelled"] == true || SignatureExpiry(data) <= DateTimeOffset.UtcNow.AddSeconds(30))
                throw new InvalidOperationException("Ilyda: Η υπογραφή έληξε, ακυρώθηκε ή αφορά διαφορετικό POS/πάροχο.");
            foreach (string field in new[] { "amount", "netAmount", "vatAmount", "grossAmount" })
                if ((decimal?)data[field] != (decimal?)request[field]) throw new InvalidOperationException("Ilyda: Διαφορετικό ποσό στην υπογραφή: " + field);
            foreach (string field in new[] { "sellerVat", "series", "invoiceTypeCode", "sellerBranch", "nspProtocol" })
                if (data[field] != null && data[field].Type != JTokenType.Null && (string)data[field] != (string)request[field])
                    throw new InvalidOperationException("Ilyda: Διαφορετικά στοιχεία στην υπογραφή: " + field);
        }
        private static ProviderPaymentSignature PaymentSignature(InvoiceModel invoice, JObject data)
        {
            return new ProviderPaymentSignature { SourcePayment = invoice.PaymentMethods.Single(p => (int)p.Type == 7),
                ProviderUid = (string)data["uid"], ProviderInput = (string)data["signedContent"], ProviderSignature = (string)data["signature"] };
        }

        public string GBridgeSignature(Guid invoiceId, string signature)
        {
            var saved = options.PosStore.Load(invoiceId);
            if (saved == null || (string)SignatureData(saved)["signature"] != signature)
                throw new InvalidOperationException("Ilyda: Η υπογραφή POS δεν αντιστοιχεί στην αποθηκευμένη απάντηση.");
            if (saved.State != "Ready" || SignatureExpiry(SignatureData(saved)) <= DateTimeOffset.UtcNow.AddSeconds(30))
                throw new InvalidOperationException("The saved Ilyda signature is unavailable or expired; no authorization may start.");
            string protocol = (string)JObject.Parse(saved.RequestJson)["nspProtocol"];
            if (string.IsNullOrWhiteSpace(signature) || !PosProtocols.Contains(protocol))
                throw new InvalidOperationException("Ilyda: Μη έγκυρη υπογραφή ή πρωτόκολλο POS για GBridge.");
            // Ilyda A1155 v1.3.2 section 7.7 vs GBridge Complete_Send_Template:
            // GBridge always takes HexString, including protocols Ilyda returns as Base64.
            if (protocol == "DEFAULT" || protocol == "EDPS" || protocol == "NEOSOFT" || protocol == "CARDLINK" || protocol == "VIVA")
            {
                try { return BitConverter.ToString(Convert.FromBase64String(signature)).Replace("-", "").ToLowerInvariant(); }
                catch (FormatException ex) { throw new InvalidOperationException("Ilyda: Μη έγκυρη υπογραφή Base64 για GBridge.", ex); }
            }
            if (string.IsNullOrWhiteSpace(signature) || signature.Length % 2 != 0 || !signature.All(Uri.IsHexDigit))
                throw new InvalidOperationException("Ilyda: Μη έγκυρη δεκαεξαδική υπογραφή για GBridge.");
            return signature;
        }

        public void CancelUnusedPaymentSignature(Guid invoiceId, string reason)
        {
            var saved = options.PosStore.Load(invoiceId);
            if (saved == null || saved.State == "Cancelled" || saved.State == "Rejected") return;
            CheckPosScope(saved);
            if (saved.State == "Expired" && saved.PaymentState == null) return;
            if (saved.State != "Ready" && saved.State != "CancelPending") throw new InvalidOperationException("Ilyda: Απαιτείται έλεγχος της προηγούμενης υπογραφής.");
            if (saved.PaymentState != null) throw new InvalidOperationException("Ilyda: Η υπογραφή έχει συσχετιστεί με πληρωμή.");
            var issued = options.Store.Load(invoiceId);
            if (issued != null && issued.State != "Rejected" && !IsDeferredPayment(invoiceId))
                throw new InvalidOperationException("Ilyda: Ελέγξτε την έκδοση πριν από ακύρωση υπογραφής.");
            var signature = SignatureData(saved);
            if (SignatureExpiry(signature) <= DateTimeOffset.UtcNow)
            {
                saved.State = "Expired"; options.PosStore.Save(saved);
                return;
            }
            saved.CancelReason = reason; saved.State = "CancelPending"; options.PosStore.Save(saved);
            var payload = new JObject { ["signature"] = signature["signature"], ["signatureCancelReason"] = reason };
            using (var response = Request(HttpMethod.Post, saved.Endpoint + "/cancel", payload.ToString(Formatting.None)))
            {
                var body = JObject.Parse(response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                var cancelled = body["invoiceSignatures"] as JArray;
                if (!response.IsSuccessStatusCode || Errors(body).Count != 0 || cancelled == null ||
                    !cancelled.Any(s => (string)s["signature"] == (string)signature["signature"] && (bool?)s["cancelled"] == true))
                    throw new InvalidOperationException("Ilyda: Δεν επιβεβαιώθηκε η ακύρωση υπογραφής. Απαιτείται έλεγχος πριν από νέα πληρωμή.");
                saved.State = "Cancelled"; options.PosStore.Save(saved);
            }
        }

        private void ApplyPosPayment(JObject payload, InvoiceModel invoice, Guid invoiceId, ProviderPaymentContext payment)
        {
            var saved = options.PosStore?.Load(invoiceId);
            if (saved == null || saved.State != "Ready") throw new InvalidOperationException("Ilyda: Δεν βρέθηκε επιβεβαιωμένη υπογραφή POS.");
            CheckPosScope(saved);
            var signature = SignatureData(saved);
            var request = SignatureRequest(invoice, invoiceId);
            if (!SameJson(request, JObject.Parse(saved.RequestJson)) ||
                payment.ProviderSignature != (string)signature["signature"] || payment.ProviderUid != (string)signature["uid"] ||
                payment.TerminalId != (string)signature["terminalId"] || string.IsNullOrWhiteSpace(payment.TransactionId))
                throw new InvalidOperationException("Ilyda: Η πληρωμή δεν συμφωνεί με την αποθηκευμένη υπογραφή.");
            var pos = ((JArray)payload["paymentMethods"]).Single(p => (int)p["type"] == 7);
            pos["terminalId"] = payment.TerminalId; pos["transactionId"] = payment.TransactionId;
            pos["signature"] = payment.ProviderSignature; pos["signingAuthor"] = InvoiceProviderCatalog.Get(InvoiceProviderCatalog.DirectIlydaKey).AadeProviderCode;

        }

        private bool IsDeferredPayment(Guid invoiceId)
        {
            var issued = options.Store.Load(invoiceId);
            return issued != null && issued.State == "Accepted" &&
                ((JArray)JObject.Parse(issued.RequestJson)["paymentMethods"]).Any(p => (int)p["type"] == 5) &&
                !((JArray)JObject.Parse(issued.RequestJson)["paymentMethods"]).Any(p => (int)p["type"] == 7 || (int)p["type"] == 8);
        }
        private IlydaSubmission ExistingInvoice(InvoiceModel invoice, Guid invoiceId)
        {
            var saved = options.Store.Load(invoiceId);
            if (saved == null || saved.State != "Accepted") throw new InvalidOperationException("Ilyda: Απαιτείται η αποθηκευμένη επιβεβαιωμένη έκδοση για ετεροχρονισμένη πληρωμή.");
            CheckScope(saved);
            var marking = JObject.Parse(saved.ResponseJson)["invoiceMarking"];
            long mark;
            var original = JObject.Parse(saved.RequestJson);
            var current = IlydaInvoiceMapper.Build(invoice, options.XmlSerializer.SerializeForProvider());
            if (!long.TryParse((string)marking?["mark"], out mark) || mark <= 0 || string.IsNullOrWhiteSpace((string)marking?["invoiceId"]) ||
                (string)original["invoiceNumber"] != (string)current["invoiceNumber"] ||
                !SameJson(original["docTotal"], current["docTotal"]) ||
                !SameJson(original["buyer"], current["buyer"]))
                throw new InvalidOperationException("Ilyda: Το MARK δεν συμφωνεί με το αποθηκευμένο παραστατικό.");
            return saved;
        }
        private static bool SameJson(JToken left, JToken right)
        {
            // Normalize CLR numeric values through the same JSON representation as persisted payloads.
            return JToken.DeepEquals(left == null ? null : JToken.Parse(left.ToString(Formatting.None)),
                right == null ? null : JToken.Parse(right.ToString(Formatting.None)));
        }
        private InvoiceResponse SendExistingInvoicePayment(InvoiceModel invoice, Guid invoiceId, JObject payload)
        {
            var issued = ExistingInvoice(invoice, invoiceId);
            var saved = options.PosStore.Load(invoiceId);
            var request = new JObject { ["invoiceId"] = JObject.Parse(issued.ResponseJson)["invoiceMarking"]["invoiceId"],
                ["paymentMethods"] = new JArray(((JArray)payload["paymentMethods"]).Where(p => (int)p["type"] == 7)) };
            string json = request.ToString(Formatting.None);
            if (saved.PaymentRequestJson != null && saved.PaymentRequestJson != json)
                return Error("Ilyda: Τα δεδομένα της προηγούμενης διαβίβασης πληρωμής άλλαξαν.");
            if (saved.PaymentState == "Accepted") return MapResponse(JObject.Parse(issued.ResponseJson));
            if (saved.PaymentState == "Sending" || saved.PaymentState == "Unknown")
                return Error("Ilyda: Δεν επιβεβαιώθηκε η προηγούμενη διαβίβαση πληρωμής. Απαιτείται έλεγχος στον πάροχο, χωρίς νέα χρέωση.");
            saved.PaymentRequestJson = json; saved.PaymentState = "Sending"; options.PosStore.Save(saved);
            try
            {
                using (var response = Request(HttpMethod.Post, "/api/invoice/sendPaymentMethods", json))
                {
                    saved.PaymentResponseJson = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    var body = JObject.Parse(saved.PaymentResponseJson);
                    var results = (body["submittedPaymentMethods"] as JArray ?? new JArray()).Concat(body["previouslySubmittedPaymentMethods"] as JArray ?? new JArray());
                    var expected = request["paymentMethods"][0];
                    bool found = results.Any(p => (string)p["transactionId"] == (string)expected["transactionId"] &&
                        (string)p["signature"] == (string)expected["signature"] && (string)p["terminalId"] == (string)expected["terminalId"] &&
                        (decimal?)p["amount"] == (decimal?)expected["amount"] && long.TryParse((string)p["paymentMethodMark"], out var mark) && mark > 0);
                    saved.PaymentState = response.IsSuccessStatusCode && Errors(body).Count == 0 && found ? "Accepted" : "Unknown";
                    options.PosStore.Save(saved);
                    return saved.PaymentState == "Accepted" ? MapResponse(JObject.Parse(issued.ResponseJson)) : Error("Ilyda: Δεν επιβεβαιώθηκε η διαβίβαση πληρωμής. Ελέγξτε τον πάροχο πριν από νέα ενέργεια.");
                }
            }
            catch { saved.PaymentState = "Unknown"; options.PosStore.Save(saved); throw; }
        }
    }
}
