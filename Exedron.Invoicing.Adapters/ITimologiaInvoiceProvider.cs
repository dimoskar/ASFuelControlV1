using Exedron.Invoicing.Core;
using Exedron.Invoicing.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Exedron.Invoicing.Adapters
{
    /// <summary>
    /// iTimologia API v1 client based on the vendor's C# reference client version 2.0.1.
    /// </summary>
    public sealed class ITimologiaInvoiceProvider : IInvoiceProvider, IInvoiceProviderPaymentSigner, IInvoiceProviderRecovery, IInvoiceProviderLookup
    {
        private const string TokenCachePrefix = "EXEDRON_INVOICING_ITIMOLOGIA_TOKEN_";
        private const string TokenMutexPrefix = @"Local\EXEDRON_ITIMOLOGIA_TOKEN_LOCK_";
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.None
        };

        private readonly ITimologiaProviderOptions options;
        private readonly HttpClient httpClient;
        private readonly bool ownsHttpClient;
        private static readonly object TokenSync = new object();
        private string communicationToken;
        private string memoryTokenCache;
        internal int PosSignatureFormat { get { return options.PosSignatureFormat; } }
        // Settings describe the desired ordering, not the provider's wire-level selector.
        // Captured endpoint responses (2026-10-14): 0 => payment/net/VAT/gross,
        // 1 => net/VAT/gross/payment, opposite to the supplied DLL guide.
        internal int PosAcquirerId { get { return options.PosSignatureFormat == 1 ? 0 : 1; } }

        public ITimologiaInvoiceProvider(ITimologiaProviderOptions options)
            : this(options, null)
        {
        }

        public ITimologiaInvoiceProvider(ITimologiaProviderOptions options, HttpMessageHandler handler)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            if (options.SubmissionStore == null) throw new ArgumentException("A host-owned iTimologia submission store is required.", nameof(options));
            httpClient = LoggedHttpClient.Create(options.Logger, InvoiceProviderCatalog.ITimologiaKey, options.TestMode ? "Test" : "Production", handler);
            ownsHttpClient = true;
            httpClient.Timeout = options.Timeout > TimeSpan.Zero
                ? options.Timeout
                : TimeSpan.FromSeconds(15);
        }

        public InvoiceProviderDefinition Descriptor
        {
            get { return InvoiceProviderCatalog.Get(InvoiceProviderCatalog.ITimologiaKey); }
        }

        public InvoiceResponse SendInvoice(
            InvoiceModel invoice,
            Guid invoiceId,
            ProviderPaymentContext providerPayment)
        {
            using (options.SubmissionStore.Acquire(invoiceId))
            {
                var previous = options.SubmissionStore.LoadLatest(invoiceId, options.TestMode, NormalizeVat(options.IssuerVat));
                if (previous != null && previous.State != "Rejected")
                {
                    string validation = Validate(invoice);
                    if (!string.IsNullOrEmpty(validation)) return ErrorResponse(validation);
                    var currentJson = JsonConvert.SerializeObject(MapInvoice(invoice, invoiceId.ToString("N"), providerPayment), JsonSettings);
                    if (string.IsNullOrWhiteSpace(previous.RequestJson) ||
                        !JToken.DeepEquals(JToken.Parse(previous.RequestJson), JToken.Parse(currentJson)))
                        return ErrorResponse("The invoice changed after its previous iTimologia submission. Reconcile the original invoice before making corrections.");
                    // Reconcile first. A missing lookup result after a lost response is not a rejection.
                    var lookup = LookupInvoice(invoiceId);
                    return lookup.NotFound ? ErrorResponse("The previous iTimologia submission is unconfirmed. No new invoice was sent; reconcile its saved history first.") : lookup.Response;
                }
                return SendInvoiceCore(invoice, invoiceId, providerPayment);
            }
        }

        private InvoiceResponse SendInvoiceCore(
            InvoiceModel invoice,
            Guid invoiceId,
            ProviderPaymentContext providerPayment)
        {
            string validationError = Validate(invoice);
            if (!string.IsNullOrEmpty(validationError))
            {
                return ErrorResponse(validationError);
            }
            if (invoice.PaymentMethods != null &&
                invoice.PaymentMethods.Any(IsPosPayment) &&
                providerPayment == null)
            {
                return ErrorResponse(
                    "iTimologia POS/IRIS invoices must be issued through the common payment coordinator.");
            }

            string externalSystemId = invoiceId.ToString("N", CultureInfo.InvariantCulture);
            try
            {
                var request = MapInvoice(invoice, externalSystemId, providerPayment);
                string token = GetCommunicationToken(false);
                ApiResult result = SendJson(
                    HttpMethod.Post,
                    BuildEndpoint("invoices"),
                    token,
                    request, invoiceId);

                if (result.StatusCode == HttpStatusCode.Unauthorized)
                {
                    token = GetCommunicationToken(true);
                    result = SendJson(
                        HttpMethod.Post,
                        BuildEndpoint("invoices"),
                        token,
                        request, invoiceId);
                }

                InvoiceResponse invoiceResponse;
                if (result.StatusCode == HttpStatusCode.Conflict)
                {
                    invoiceResponse = RecoverInvoice(externalSystemId, token, result);
                }
                else if (!result.IsSuccess)
                {
                    invoiceResponse = ErrorResponse(DescribeFailure(result));
                }
                else
                {
                    invoiceResponse = MapResponse(result.Body, externalSystemId);
                }

                return invoiceResponse;
            }
            catch (TaskCanceledException ex)
            {
                return TryRecoverAfterUnknownOutcome(externalSystemId, ex.Message);
            }
            catch (HttpRequestException ex)
            {
                return TryRecoverAfterUnknownOutcome(externalSystemId, ex.Message);
            }
            catch (Exception ex)
            {
                return ErrorResponse(Redact(ex.Message));
            }
        }

        public InvoiceResponse SendInvoice(InvoiceModel invoice, Guid invoiceId)
        {
            return SendInvoice(invoice, invoiceId, null);
        }

        public InvoiceResponse RecoverInvoice(Guid invoiceId)
        {
            return LookupInvoice(invoiceId).Response;
        }

        public InvoiceLookupResult LookupInvoice(Guid invoiceId)
        {
            InvoiceResponse accepted = null;
            try
            {
                using (options.SubmissionStore.Acquire(invoiceId))
                {
                    var saved = options.SubmissionStore.LoadLatest(invoiceId, options.TestMode, NormalizeVat(options.IssuerVat));
                    if (saved != null && saved.State == "Accepted")
                    {
                        accepted = MapResponse(saved.PrintMetadataResponseJson ?? saved.ResponseJson, invoiceId.ToString("N"));
                        if (ProviderPrintMetadata.Missing(InvoiceProviderCatalog.ITimologiaKey, accepted).Length == 0)
                            return new InvoiceLookupResult { Response = accepted };
                    }
                    var token = GetCommunicationToken(false);
                    var endpoint = BuildEndpoint("invoices/" + invoiceId.ToString("N", CultureInfo.InvariantCulture));
                    var result = SendJson(HttpMethod.Get, endpoint, token, null);
                    if (result.StatusCode == HttpStatusCode.Unauthorized)
                        result = SendJson(HttpMethod.Get, endpoint, GetCommunicationToken(true), null);
                    var response = result.IsSuccess ? MapResponse(result.Body, invoiceId.ToString("N", CultureInfo.InvariantCulture)) : ErrorResponse(DescribeFailure(result));
                    if (!response.HasErrors)
                    {
                        var data = JsonConvert.DeserializeObject<ITimologiaInvoiceReply>(result.Body, JsonSettings)?.Data;
                        if (string.IsNullOrWhiteSpace(data?.ExternalSystemId))
                            response = ErrorResponse("iTimologia lookup omitted the external invoice identity. Reconciliation remains unconfirmed.");
                    }
                    if (saved != null && (saved.State == "Sending" || saved.State == "Unknown"))
                    {
                        saved.StatusCode = (int)result.StatusCode;
                        saved.ResponseJson = result.Body;
                        saved.CompletedUtc = DateTime.UtcNow;
                        saved.State = response.HasErrors ? "Unknown" : "Accepted";
                        options.SubmissionStore.Complete(saved);
                    }
                    if (accepted != null)
                    {
                        if (!response.HasErrors && response.Mark == accepted.Mark && response.Uid == accepted.Uid &&
                            ProviderPrintMetadata.Missing(InvoiceProviderCatalog.ITimologiaKey, response).Length == 0)
                        {
                            // Retain the original acceptance body; append independently
                            // verified metadata without changing MARK, UID or fiscal state.
                            saved.PrintMetadataResponseJson = result.Body;
                            options.SubmissionStore.Complete(saved);
                            return new InvoiceLookupResult { Response = response };
                        }
                        accepted.Warnings = new[] { "Invoice remains accepted; print metadata retrieval is incomplete or inconsistent. No new invoice was sent." };
                        return new InvoiceLookupResult { Response = accepted };
                    }
                    return new InvoiceLookupResult { NotFound = result.StatusCode == HttpStatusCode.NotFound, Response = response };
                }
            }
            catch (Exception ex)
            {
                if (accepted != null)
                {
                    accepted.Warnings = new[] { "Invoice remains accepted; print metadata retrieval failed: " + Redact(ex.Message) };
                    return new InvoiceLookupResult { Response = accepted };
                }
                return new InvoiceLookupResult { Response = ErrorResponse("iTimologia invoice retrieval failed: " + Redact(ex.Message)) };
            }
        }

        public void Dispose()
        {
            if (ownsHttpClient)
            {
                httpClient.Dispose();
            }
        }

        private static bool IsPosPayment(PaymentMethod paymentMethod)
        {
            if (paymentMethod == null)
            {
                return false;
            }

            int type = Convert.ToInt32(paymentMethod.Type, CultureInfo.InvariantCulture);
            return type == 7 || type == 8;
        }

        public ProviderPaymentSignature CreatePaymentSignature(InvoiceModel invoice)
        {
            return CreatePaymentSignature(invoice, null);
        }

        public ProviderPaymentSignature CreatePaymentSignature(InvoiceModel invoice, Action<PaymentGatewayEvent> recordExchange)
        {
            if (options.PosSignatureFormat != 0 && options.PosSignatureFormat != 1)
                throw new InvalidOperationException("Select a valid iTimologia POS signature format: Mellon/ePay/Cardlink/Nexi or EDPS/Viva.");
            string validationError = Validate(invoice);
            if (!string.IsNullOrEmpty(validationError))
            {
                throw new InvalidOperationException(validationError);
            }

            PaymentMethod[] posPayments = invoice.PaymentMethods.Where(IsPosPayment).ToArray();
            if (posPayments.Length != 1)
            {
                throw new InvalidOperationException(
                    "Exactly one POS or IRIS payment method is supported per GBridge invoice.");
            }

            PaymentMethod payment = posPayments[0];
            var signatureRequest = new ProviderSignatureRequest
            {
                VatNumber = NormalizeVat(invoice.Issuer.VATNumber),
                Series = invoice.InvoiceHeader.Series ?? string.Empty,
                Number = invoice.InvoiceHeader.AA,
                IssueDate = invoice.InvoiceHeader.IssueDate.ToString(
                    "yyyy-MM-ddTHH:mm:ss",
                    CultureInfo.InvariantCulture),
                InvoiceType = invoice.InvoiceHeader.InvoiceType,
                Branch = invoice.Issuer.Branch,
                Mark = invoice.Mark > 0
                    ? invoice.Mark.ToString(CultureInfo.InvariantCulture)
                    : null,
                Amount = DecimalText(payment.Amount),
                TotalNetValue = DecimalText(invoice.InvoiceSummary.TotalNetValue),
                TotalVatAmount = DecimalText(invoice.InvoiceSummary.TotalVATAmount),
                TotalGrossValue = DecimalText(invoice.InvoiceSummary.TotalGrossValue),
                Tid = options.PosTerminalId == null ? null : options.PosTerminalId.Trim(),
                // The signature API requires the catering flag even for ordinary retail/fuel sales.
                IsCatering = false,

                // Request the desired ordering; never reorder the signed response locally.
                AcquirerId = PosAcquirerId
            };

            DateTimeOffset signatureStarted = DateTimeOffset.UtcNow;
            string communicationToken = GetCommunicationToken(false);
            ApiResult signatureResult = SendJson(
                HttpMethod.Post,
                BuildEndpoint("signature"),
                communicationToken,
                signatureRequest);
            if (signatureResult.StatusCode == HttpStatusCode.Unauthorized)
            {
                communicationToken = GetCommunicationToken(true);
                signatureResult = SendJson(
                    HttpMethod.Post,
                    BuildEndpoint("signature"),
                    communicationToken,
                    signatureRequest);
            }
            // Record before parsing/validation: otherwise the response needed to diagnose a
            // format mismatch disappears. Authentication headers/tokens are never recorded.
            if (recordExchange != null)
                recordExchange(new PaymentGatewayEvent {
                    InputType = "SIGNATURE", StartedUtc = signatureStarted, CompletedUtc = DateTimeOffset.UtcNow,
                    HttpStatus = (int)signatureResult.StatusCode,
                    RequestJson = JsonConvert.SerializeObject(signatureRequest, JsonSettings),
                    ResponseJson = signatureResult.Body,
                    ReplyDescription = "iTimologia /signature (acq_id=" + signatureRequest.AcquirerId + ")"
                });
            if (!signatureResult.IsSuccess)
            {
                if (IsSignatureRequestRejected((int)signatureResult.StatusCode, signatureResult.Body)) throw new PaymentSignatureRejectedException();
                throw new InvalidOperationException(DescribeFailure(signatureResult));
            }

            ProviderSignatureReply signature;
            try
            {
                signature = JsonConvert.DeserializeObject<ProviderSignatureReply>(
                    signatureResult.Body,
                    JsonSettings);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException(
                    "iTimologia returned an invalid provider-signature response.",
                    ex);
            }

            if (signature == null ||
                string.IsNullOrWhiteSpace(signature.Signature) ||
                string.IsNullOrWhiteSpace(signature.InputFormattedValues))
            {
                throw new InvalidOperationException(
                    FirstNonEmpty(
                        signature == null ? null : signature.Error,
                        signature == null ? null : signature.Message,
                        "iTimologia did not return the provider signature and formatted input values."));
            }

            ValidateSignatureInputFormat(signature.InputFormattedValues, signatureRequest, options.PosSignatureFormat);
            return new ProviderPaymentSignature
            {
                SourcePayment = payment,
                ProviderSignature = signature.Signature,
                ProviderUid = signature.Uid,
                ProviderInput = signature.InputFormattedValues
            };
        }

        public static bool IsSignatureRequestRejected(int? status, string body)
        {
            if (status != 400 && status != 401 && status != 422) return false;
            try
            {
                var reply = JObject.Parse(body ?? "");
                return string.IsNullOrWhiteSpace((string)reply["signature"]) && string.IsNullOrWhiteSpace((string)reply["input_formatted_values"]) &&
                    (reply["message"]?.Type == JTokenType.String || reply["error"] != null || reply["errors"] != null);
            }
            catch (JsonException) { return false; }
        }

        private static void ValidateSignatureInputFormat(string input, ProviderSignatureRequest request, int signatureFormat)
        {
            var parts = input.Split(';');
            var expected = signatureFormat == 1
                ? new[] { request.Amount, request.TotalNetValue, request.TotalVatAmount, request.TotalGrossValue }
                : new[] { request.TotalNetValue, request.TotalVatAmount, request.TotalGrossValue, request.Amount };
            bool valid = parts.Length == 8 && parts[7] == request.Tid;
            for (int i = 0; valid && i < expected.Length; i++)
            {
                decimal actual;
                decimal amount = decimal.Parse(expected[i], CultureInfo.InvariantCulture);
                valid = decimal.TryParse(parts[i + 3], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out actual) &&
                    actual == decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero);
            }
            if (!valid)
                throw new InvalidOperationException("iTimologia returned signed input that does not match the requested POS format (acq_id=" +
                    request.AcquirerId + ", " + (signatureFormat == 1 ? "EDPS / Viva" : "Mellon / ePay / Cardlink / Nexi") +
                    "). No payment was sent. Expected amounts in cents: " + string.Join(";", expected.Select(value =>
                        decimal.Round(decimal.Parse(value, CultureInfo.InvariantCulture) * 100m, 0, MidpointRounding.AwayFromZero).ToString(CultureInfo.InvariantCulture))) +
                    ". Received: " + (parts.Length >= 7 ? string.Join(";", parts.Skip(3).Take(4)) : "invalid field count") +
                    "; fields=" + parts.Length + "; terminal=" + (parts.Length == 8 ? parts[7] : "invalid") +
                    ". Open payment history and select SIGNATURE for the actual request and response.");
        }

        private InvoiceResponse TryRecoverAfterUnknownOutcome(string externalSystemId, string originalError)
        {
            // Read-only recovery; a missing result never permits another invoice POST.
            return LookupInvoice(Guid.Parse(externalSystemId)).Response;
        }

        private InvoiceResponse RecoverInvoice(string externalSystemId, string token, ApiResult conflict)
        {
            return LookupInvoice(Guid.Parse(externalSystemId)).Response;
        }

        private string GetCommunicationToken(bool forceRefresh)
        {
            lock (TokenSync)
            {
                var variable = TokenCacheVariableName;
                if (options.TokenCache == null)
                    return GetCommunicationTokenUnderLock(variable, forceRefresh);

                // The host cache is shared with other processes. A process-local lock
                // alone cannot prevent both applications from renewing the same token.
                using (var mutex = new Mutex(false,
                    TokenMutexPrefix + variable.Substring(TokenCachePrefix.Length)))
                {
                    bool acquired = false;
                    try
                    {
                        try { acquired = mutex.WaitOne(TimeSpan.FromSeconds(60)); }
                        catch (AbandonedMutexException) { acquired = true; }
                        if (!acquired)
                            throw new TimeoutException("Timed out waiting for the shared iTimologia token cache.");

                        // Another process may have renewed the token while this process
                        // waited, so the shared cache must only be read after acquisition.
                        return GetCommunicationTokenUnderLock(variable, forceRefresh);
                    }
                    finally
                    {
                        if (acquired) mutex.ReleaseMutex();
                    }
                }
            }
        }

        private string GetCommunicationTokenUnderLock(string variable, bool forceRefresh)
        {
                TokenCache cached = null;
                var json = options.TokenCache == null ? memoryTokenCache : options.TokenCache.Load(variable);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    try { cached = JsonConvert.DeserializeObject<TokenCache>(json); }
                    catch (JsonException) { /* An invalid cache is replaced only after successful authentication. */ }
                }
                if (cached != null && !string.IsNullOrWhiteSpace(cached.ConnectionToken) &&
                    cached.ExpiresAt > DateTimeOffset.UtcNow.AddMinutes(1) &&
                    (!forceRefresh || cached.ConnectionToken != communicationToken))
                {
                    communicationToken = cached.ConnectionToken;
                    return communicationToken;
                }

                var previousToken = cached == null ? communicationToken : cached.ConnectionToken;
                var renew = !string.IsNullOrWhiteSpace(previousToken);
                object request = renew
                    ? (object)new TokenRenewRequest { Token = previousToken }
                    : new TokenCreateRequest { SubscriptionId = options.SubscriptionId.Trim() };
                ApiResult result = SendJson(
                    HttpMethod.Post,
                    BuildEndpoint(renew ? "tokens/renew" : "tokens/create"),
                    options.MasterToken.Trim(),
                    request);
                if (!result.IsSuccess)
                {
                    throw new InvalidOperationException(DescribeFailure(result));
                }

                TokenReply reply;
                try
                {
                    reply = JsonConvert.DeserializeObject<TokenReply>(result.Body, JsonSettings);
                }
                catch (JsonException ex)
                {
                    throw new InvalidOperationException("iTimologia returned an invalid token response.", ex);
                }

                if (reply == null || string.IsNullOrWhiteSpace(reply.Token))
                {
                    string error = reply == null ? null : FirstNonEmpty(reply.Error, reply.Message);
                    throw new InvalidOperationException(
                        FirstNonEmpty(error, "iTimologia did not return a communication token."));
                }

                if (!string.IsNullOrWhiteSpace(reply.Error) || !reply.ExpiresAt.HasValue ||
                    reply.ExpiresAt.Value <= DateTimeOffset.UtcNow)
                    throw new InvalidOperationException(FirstNonEmpty(reply.Error,
                        "iTimologia did not return a valid communication-token expiration time."));

                var cachedJson = JsonConvert.SerializeObject(new TokenCache
                {
                    ConnectionToken = reply.Token,
                    ExpiresAt = reply.ExpiresAt.Value.ToUniversalTime()
                }, JsonSettings);
                if (options.TokenCache != null) options.TokenCache.Save(variable, cachedJson);
                memoryTokenCache = cachedJson;
                communicationToken = reply.Token;
                return communicationToken;
        }

        public string TokenCacheVariableName
        {
            get
            {
                // Never put credentials in the variable name; changing credentials isolates the cache too.
                var scope = JsonConvert.SerializeObject(new[] { BuildEndpoint(""),
                    (options.SubscriptionId ?? "").Trim(), (options.MasterToken ?? "").Trim() });
                using (var hash = SHA256.Create())
                    return TokenCachePrefix +
                        BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(scope))).Replace("-", "");
            }
        }

        private ApiResult SendJson(HttpMethod method, string endpoint, string bearerToken, object body, Guid? invoiceId = null)
        {
            using (var request = new HttpRequestMessage(method, endpoint))
            {
                if (invoiceId.HasValue) request.Properties[LoggedHttpClient.InvoiceIdKey] = invoiceId.Value;
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                if (!string.IsNullOrWhiteSpace(bearerToken))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
                }

                string json = null;
                if (body != null)
                {
                    json = JsonConvert.SerializeObject(body, JsonSettings);
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                }

                // Capture only invoice POSTs, never authentication or signature endpoints/headers.
                // Record the same serialized bytes used by the request before starting transport.
                var trace = invoiceId.HasValue && options.SubmissionStore != null
                    ? new ITimologiaSubmissionAttempt { AttemptId = Guid.NewGuid(), InvoiceId = invoiceId.Value,
                        TestMode = options.TestMode, IssuerVat = NormalizeVat(options.IssuerVat), Endpoint = endpoint,
                        RequestJson = json, CreatedUtc = DateTime.UtcNow, State = "Sending" }
                    : null;
                // If the initial save fails, no invoice request has been sent.
                if (trace != null) options.SubmissionStore.Begin(trace);
                try
                {
                    using (HttpResponseMessage response = httpClient.SendAsync(request).GetAwaiter().GetResult())
                    {
                        if (trace != null) trace.StatusCode = (int)response.StatusCode;
                        string responseBody = response.Content == null
                            ? string.Empty
                            : response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                        if (trace != null)
                        {
                            trace.ResponseJson = responseBody;
                            trace.State = "Unknown";
                            if (response.IsSuccessStatusCode && !MapResponse(responseBody, invoiceId.Value.ToString("N")).HasErrors)
                                trace.State = "Accepted";
                            // The vendor explicitly rejects 422 input and 452 AADE submissions.
                            // Only 401 is automatically retried; other rejections require an
                            // explicit corrected retry using the original identity/payment.
                            if (IsDefinitiveRejection(response.StatusCode, responseBody)) trace.State = "Rejected";
                        }
                        return new ApiResult(response.StatusCode, response.ReasonPhrase, responseBody);
                    }
                }
                catch (Exception ex)
                {
                    if (trace != null) { trace.TransportError = ex.GetType().FullName; trace.State = "Unknown"; }
                    throw;
                }
                finally
                {
                    if (trace != null)
                    {
                        trace.CompletedUtc = DateTime.UtcNow;
                        // Durable fiscal outcome is required before reporting success or retrying a 401.
                        // On failure, Sending remains a blocker and the HTTP journal retains the exchange.
                        options.SubmissionStore.Complete(trace);
                    }
                }
            }
        }

        private string BuildEndpoint(string relativePath)
        {
            string vat = NormalizeVat(options.IssuerVat);
            string environment = options.TestMode ? ".sandbox.itimologia.gr/api/v1/" : ".itimologia.gr/api/v1/";
            return "https://el" + vat + environment + relativePath.TrimStart('/');
        }

        private static bool IsDefinitiveRejection(HttpStatusCode status, string body)
        {
            if (status != HttpStatusCode.Unauthorized && (int)status != 422 && (int)status != 452) return false;
            // Contradictory fiscal markings are uncertainty, even with a rejection HTTP code.
            try
            {
                var data = string.IsNullOrWhiteSpace(body) ? null : JObject.Parse(body)["data"];
                long mark;
                if (long.TryParse((string)data?["mark"], out mark) && mark > 0) return false;
            }
            catch (JsonException) { }
            return true;
        }

        private string Validate(InvoiceModel invoice)
        {
            try { InvoiceValidation.Validate(invoice); }
            catch (ProviderInvoiceValidationException ex) { return ex.Message; }
            if (invoice == null)
            {
                return "The iTimologia invoice cannot be empty.";
            }

            string vat;
            try
            {
                vat = NormalizeVat(options.IssuerVat);
            }
            catch (ArgumentException ex)
            {
                return ex.Message;
            }

            if (string.IsNullOrWhiteSpace(options.MasterToken))
            {
                return "The iTimologia master token is not configured.";
            }

            if (string.IsNullOrWhiteSpace(options.SubscriptionId))
            {
                return "The iTimologia subscription ID is not configured.";
            }

            if (invoice.Issuer == null || string.IsNullOrWhiteSpace(invoice.Issuer.VATNumber))
            {
                return "The invoice issuer VAT number is missing.";
            }

            if (!string.Equals(NormalizeVat(invoice.Issuer.VATNumber), vat, StringComparison.Ordinal))
            {
                return "The invoice issuer VAT number does not match the configured company VAT number.";
            }

            if (invoice.InvoiceHeader == null || string.IsNullOrWhiteSpace(invoice.InvoiceHeader.InvoiceType))
            {
                return "The invoice header or myDATA invoice type is missing.";
            }

            if (invoice.InvoiceDetails == null || invoice.InvoiceDetails.Length == 0)
            {
                return "The invoice must contain at least one line.";
            }

            if (invoice.InvoiceSummary == null)
            {
                return "The invoice summary is missing.";
            }

            if (invoice.PaymentMethods != null && invoice.PaymentMethods.Any(IsPosPayment))
            {
                if (string.IsNullOrWhiteSpace(options.PosTerminalId))
                {
                    return "The GBridge POS Terminal ID is not configured.";
                }
            }

            return null;
        }

        private static string NormalizeVat(string value)
        {
            string vat = (value ?? string.Empty).Trim().Replace(" ", string.Empty);
            if (vat.StartsWith("EL", StringComparison.OrdinalIgnoreCase))
            {
                vat = vat.Substring(2);
            }

            if (vat.Length != 9 || vat.Any(character => !char.IsDigit(character)))
            {
                throw new ArgumentException("The iTimologia issuer VAT number must contain exactly nine digits.");
            }

            return vat;
        }

        public static string PrepareRequestJson(InvoiceModel invoice, Guid invoiceId)
        {
            return JsonConvert.SerializeObject(MapInvoice(invoice, invoiceId.ToString("N", CultureInfo.InvariantCulture), null), JsonSettings);
        }

        private static ITimologiaInvoiceRequest MapInvoice(
            InvoiceModel source,
            string externalSystemId,
            ProviderPaymentContext posPayment)
        {
            // Validate the shared AADE representation even for JSON transport.
            WorkflowInvoiceXml.Build(source);
            bool isRetail = source.InvoiceHeader.InvoiceType.StartsWith("11.", StringComparison.Ordinal);
            // Keep the recipient for delivery receipts and for fuel supply numbers,
            // even when the fuel receipt is not also a delivery note.
            bool isRetailDelivery = source.InvoiceHeader.InvoiceType == "11.1" && source.InvoiceHeader.DeliveryNote;
            string supplyAccount = MyDataPartyFields.SupplyAccountNumber(source);
            if (isRetailDelivery && source.CounterPart == null)
                throw new InvalidOperationException("A retail delivery receipt requires recipient details. No invoice was submitted.");
            if (source.InvoiceHeader.DeliveryNote || IsStandaloneDeliveryNote(source.InvoiceHeader.InvoiceType))
                foreach (var detail in source.InvoiceDetails ?? new InvoiceDetail[0])
                    if (detail == null || string.IsNullOrWhiteSpace(detail.ItemDescription))
                        throw new InvalidOperationException("itemDescr is required for every delivery invoice line. No invoice was submitted.");
            List<InvoiceDetailRequest> details = (source.InvoiceDetails ?? new InvoiceDetail[0])
                .Select(value => MapDetail(value, source.InvoiceHeader.DeliveryNote || IsStandaloneDeliveryNote(source.InvoiceHeader.InvoiceType),
                    MyDataLineFields.IncludeQuantityAndUnit(source.InvoiceHeader.InvoiceType)))
                .ToList();
            var counterpart = isRetail && !isRetailDelivery && supplyAccount == null ? null : MapTrader<CounterpartRequest>(source.CounterPart);
            if (counterpart != null) counterpart.SupplyAccountNumber = supplyAccount;
            return new ITimologiaInvoiceRequest
            {
                ExternalSystemId = externalSystemId,
                Notes = EmptyToNull(source.PaymentTerms),
                Issuer = MapTrader<IssuerRequest>(source.Issuer),
                Counterpart = counterpart,
                Header = MapHeader(source.InvoiceHeader, MyDataHeaderFields.SpecialInvoiceCategory(source)),
                PaymentMethods = (source.PaymentMethods ?? new PaymentMethod[0])
                    .Select(value => MapPaymentMethod(value, posPayment))
                    .ToList(),
                Details = details,
                TaxesTotals = (source.TaxesTotals ?? new TaxesTotal[0])
                    .Select(MapTaxesTotal)
                    .ToList(),
                Summary = MapSummary(source.InvoiceSummary, details)
            };
        }

        private static T MapTrader<T>(Trader source) where T : TraderRequest, new()
        {
            if (source == null)
            {
                return null;
            }

            return new T
            {
                VatNumber = EmptyToNull(source.VATNumber),
                Name = EmptyToNull(source.Name),
                Country = EmptyToNull(source.Country),
                Branch = source.Branch >= 0 ? (int?)source.Branch : null,
                Address = MapAddress(source.Address)
            };
        }

        private static AddressRequest MapAddress(Address source)
        {
            if (source == null)
            {
                return null;
            }

            var mapped = new AddressRequest
            {
                Street = EmptyToNull(source.Street),
                Number = EmptyToNull(source.Number),
                PostalCode = EmptyToNull(source.PostalCode),
                City = EmptyToNull(source.City)
            };
            return mapped.Street == null && mapped.Number == null && mapped.PostalCode == null && mapped.City == null
                ? null
                : mapped;
        }

        private static bool IsStandaloneDeliveryNote(string invoiceType)
        {
            // Explicit supported types; do not assume future 9.x codes have the same contract.
            return invoiceType == "9.1" || invoiceType == "9.2" || invoiceType == "9.3" || invoiceType == "10.1" || invoiceType == "10.2";
        }

        private static InvoiceHeaderRequest MapHeader(InvoiceHeader source, int? specialCategory)
        {
            // 10.1 receipt headers must not inherit transport fields from legacy callers.
            bool isReceipt = source.InvoiceType == "10.1";
            return new InvoiceHeaderRequest
            {
                Series = source.Series ?? string.Empty,
                Number = source.AA,
                IssueDate = source.IssueDate.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
                InvoiceType = source.InvoiceType,
                VatPaymentSuspension = source.VATPaymentSuspension,
                Currency = EmptyToNull(source.Currency),
                ExchangeRate = source.ExchangeRate == 0m ? null : DecimalText(source.ExchangeRate),
                CorrelatedInvoices = source.CorrelatedInvoices == null
                    ? null
                    : source.CorrelatedInvoices.ToList(),
                SelfPricing = source.SelfPricing ? (bool?)true : null,
                DispatchDate = isReceipt || source.DispatchDateTime == default(DateTime)
                    ? null
                    : source.DispatchDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                DispatchTime = isReceipt || source.DispatchDateTime == default(DateTime)
                    ? null
                    : source.DispatchDateTime.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
                VehicleNumber = isReceipt ? null : EmptyToNull(source.VehicleNumber),
                MovePurpose = isReceipt || Convert.ToInt32(source.MovePurpose, CultureInfo.InvariantCulture) == 0
                    ? null
                    : (int?)Convert.ToInt32(source.MovePurpose, CultureInfo.InvariantCulture),
                FuelInvoice = source.FuelInvoice ? (bool?)true : null,
                SpecialInvoiceCategory = specialCategory?.ToString(CultureInfo.InvariantCulture),
                IsDeliveryNote = source.DeliveryNote && !IsStandaloneDeliveryNote(source.InvoiceType) ? (bool?)true : null,
                OtherDeliveryNoteHeader = isReceipt ? null : MapOtherDeliveryNoteHeader(source.OtherDeliveryNoteHeader)
            };
        }

        private static OtherDeliveryNoteHeaderRequest MapOtherDeliveryNoteHeader(OtherDeliveryNoteHeader source)
        {
            if (source == null)
            {
                return null;
            }

            return new OtherDeliveryNoteHeaderRequest
            {
                StartShippingBranch = source.StartShippingBranch,
                CompleteShippingBranch = source.CompleteShippingBranch,
                LoadingAddress = MapAddress(source.LoadingAddress),
                DeliveryAddress = MapAddress(source.DeliveryAddress)
            };
        }

        private static PaymentMethodRequest MapPaymentMethod(
            PaymentMethod source,
            ProviderPaymentContext posPayment)
        {
            bool isAuthorizedPosPayment = posPayment != null &&
                object.ReferenceEquals(posPayment.SourcePayment, source);
            return new PaymentMethodRequest
            {
                Type = Convert.ToInt32(source.Type, CultureInfo.InvariantCulture),
                Amount = DecimalText(source.Amount),
                Information = EmptyToNull(source.PaymentMethodInfo),
                TransactionId = isAuthorizedPosPayment ? posPayment.TransactionId : null,
                Tid = isAuthorizedPosPayment ? posPayment.TerminalId : null,
                ProviderSignature = isAuthorizedPosPayment ? posPayment.ProviderSignature : null
            };
        }

        private static InvoiceDetailRequest MapDetail(InvoiceDetail source, bool includeDeliveryItemFields, bool includeQuantity)
        {
            var classifications = new List<IncomeClassificationRequest>();
            if (source.IncomeClassification != null)
            {
                classifications.Add(MapIncomeClassification(source.IncomeClassification));
            }
            if (source.IncomeClassifications != null)
            {
                classifications.AddRange(source.IncomeClassifications
                    .Where(value => value != null)
                    .Select(MapIncomeClassification));
            }

            int parsedDetailType;
            return new InvoiceDetailRequest
            {
                LineNumber = source.LineNumber > 0 ? (int?)source.LineNumber : null,
                // AADE accepts itemDescr and itemCode only for delivery/movement
                // documents (or the tax-free special category, which is not represented
                // by the legacy common model). Arbitrans follows the same delivery-note
                // switch while retaining these values separately for provider printing.
                ItemDescription = includeDeliveryItemFields ? EmptyToNull(source.ItemDescription) : null,
                ItemCode = includeDeliveryItemFields ? EmptyToNull(source.ItemCode) : null,
                FuelCode = EmptyToNull(source.FuelCode),
                Quantity = !includeQuantity || source.Quantity == 0m ? null : (decimal?)source.Quantity,
                Quantity15 = includeQuantity ? MovementQuantities.Get(source) : null,
                MeasurementUnit = !includeQuantity || Convert.ToInt32(source.MeasurementUnit, CultureInfo.InvariantCulture) == 0
                    ? null
                    : (int?)Convert.ToInt32(source.MeasurementUnit, CultureInfo.InvariantCulture),
                InvoiceDetailType = int.TryParse(source.InvoiceDetailType, out parsedDetailType)
                    ? (int?)parsedDetailType
                    : null,
                NetValue = DecimalText(source.NetValue),
                VatCategory = Convert.ToInt32(source.VATCategory, CultureInfo.InvariantCulture),
                VatAmount = DecimalText(source.VATAmount),
                VatExemptionCategory = NullableInt(source.VATExemptionCategory),
                DiscountOption = source.DiscountOption ? (bool?)true : null,
                WithheldAmount = DecimalTextOrNull(source.WithheldAmount),
                WithheldPercentCategory = NullableInt(source.WithheldPercentCategory),
                StampDutyAmount = DecimalTextOrNull(source.StampDutyAmount),
                StampDutyPercentCategory = NullableInt(source.StampDutyPercentCategory),
                FeesAmount = DecimalTextOrNull(source.FeesAmount),
                FeesPercentCategory = NullableInt(source.FeesPercentCategory),
                OtherTaxesAmount = DecimalTextOrNull(source.OtherTaxesAmount),
                OtherTaxesPercentCategory = NullableInt(source.OtherTaxesCategory),
                DeductionsAmount = DecimalTextOrNull(source.DeductionsAmount),
                LineComments = EmptyToNull(source.LineComments),
                IncomeClassifications = classifications.Count == 0 ? null : classifications
            };
        }

        private static IncomeClassificationRequest MapIncomeClassification(IncomeClassification source)
        {
            return new IncomeClassificationRequest
            {
                ClassificationType = EmptyToNull(source.ClassificationType),
                ClassificationCategory = EmptyToNull(source.ClassificationCategory),
                Amount = DecimalText(source.Amount)
            };
        }

        private static TaxesTotalRequest MapTaxesTotal(TaxesTotal source)
        {
            return new TaxesTotalRequest
            {
                TaxType = EmptyToNull(source.TaxType),
                TaxCategory = source.TaxCategory == 0 ? null : (int?)source.TaxCategory,
                UnderlyingValue = DecimalText(source.UnderlyingValue),
                TaxAmount = DecimalText(source.TaxAmount)
            };
        }

        private static InvoiceSummaryRequest MapSummary(
            InvoiceSummary source,
            IEnumerable<InvoiceDetailRequest> details)
        {
            if (source == null)
            {
                return null;
            }

            List<IncomeClassificationRequest> summaryClassifications = details
                .Where(detail => detail.IncomeClassifications != null)
                .SelectMany(detail => detail.IncomeClassifications)
                .GroupBy(classification => new
                {
                    classification.ClassificationType,
                    classification.ClassificationCategory
                })
                .Select(group => new IncomeClassificationRequest
                {
                    ClassificationType = group.Key.ClassificationType,
                    ClassificationCategory = group.Key.ClassificationCategory,
                    Amount = DecimalText(group.Sum(classification =>
                        decimal.Parse(classification.Amount, CultureInfo.InvariantCulture)))
                })
                .ToList();

            return new InvoiceSummaryRequest
            {
                TotalNetValue = DecimalText(source.TotalNetValue),
                TotalVatAmount = DecimalText(source.TotalVATAmount),
                TotalWithheldAmount = DecimalTextOrNull(source.TotalWithheldAmount),
                TotalFeesAmount = DecimalTextOrNull(source.TotalFeesAmount),
                TotalStampDutyAmount = DecimalTextOrNull(source.TotalStampDutyAmount),
                TotalOtherTaxesAmount = DecimalTextOrNull(source.TotalOtherTaxesAmount),
                TotalDeductionsAmount = DecimalTextOrNull(source.TotalDeductionsAmount),
                TotalGrossValue = DecimalText(source.TotalGrossValue),
                // iTimologia requires the summary to exactly repeat the totals of the
                // classifications serialized on the rows. Deriving it here prevents a
                // stale or absent common-model summary from disagreeing with the rows.
                IncomeClassifications = summaryClassifications.Count == 0
                    ? null
                    : summaryClassifications
            };
        }

        private static InvoiceResponse MapResponse(string responseBody, string externalSystemId)
        {
            ITimologiaInvoiceReply response;
            try
            {
                response = JsonConvert.DeserializeObject<ITimologiaInvoiceReply>(responseBody, JsonSettings);
            }
            catch (JsonException ex)
            {
                return ErrorResponse("iTimologia returned an invalid invoice response: " + ex.Message);
            }

            ITimologiaInvoiceData data = response == null ? null : response.Data;
            if (data != null && !string.IsNullOrWhiteSpace(data.CancelledByMark) && data.CancelledByMark != "0")
                return ErrorResponse("iTimologia returned a cancelled invoice. Manual reconciliation is required.");
            if (data != null && !string.IsNullOrWhiteSpace(data.ExternalSystemId) &&
                !string.Equals(data.ExternalSystemId.Replace("-", ""), externalSystemId, StringComparison.OrdinalIgnoreCase))
                return ErrorResponse("iTimologia returned a different external invoice id. Manual reconciliation is required.");
            string[] messages = data == null || data.TransmissionMessages == null
                ? new string[0]
                : data.TransmissionMessages
                    .Select(value => string.IsNullOrWhiteSpace(value.Code)
                        ? value.Message
                        : value.Code + ": " + value.Message)
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .ToArray();
            long mark;
            bool hasErrors = data == null || string.IsNullOrWhiteSpace(data.Uid) ||
                !long.TryParse(data.Mark, NumberStyles.None, CultureInfo.InvariantCulture, out mark) || mark <= 0;
            if (hasErrors && messages.Length == 0)
            {
                messages = new[] { "iTimologia did not return the invoice UID and MARK." };
            }

            return new InvoiceResponse
            {
                Mark = data == null ? null : data.Mark,
                Uid = data == null ? null : data.Uid,
                Url = data == null ? null : data.WebView,
                InvoiceUrl = data == null ? null : data.WebView,
                QrCodeUrl = data == null ? null : data.WebView,
                ProviderUrl = data == null ? null : data.WebView,
                VerificationHash = data == null ? null : data.AuthenticationCode,
                InvoiceSignature = data == null ? null : data.SignatureCode,
                Errors = hasErrors ? messages : new string[0],
                Warnings = hasErrors ? new string[0] : messages
            };
        }

        private static InvoiceResponse ErrorResponse(string message)
        {
            return new InvoiceResponse
            {
                Errors = new[] { FirstNonEmpty(message, "Unknown iTimologia error.") },
                Warnings = new string[0]
            };
        }

        private string Redact(string message)
        {
            foreach (var secret in new[] { options.MasterToken, communicationToken })
                if (!string.IsNullOrEmpty(secret)) message = message?.Replace(secret,"[redacted]");
            return message;
        }

        private string DescribeFailure(ApiResult result)
        {
            string message = ExtractApiError(result.Body);
            if (string.IsNullOrWhiteSpace(message))
            {
                message = result.ReasonPhrase;
            }
            return "iTimologia request failed (HTTP " + (int)result.StatusCode + "): " +
                Redact(FirstNonEmpty(message, "No error details were returned."));
        }

        private static string ExtractApiError(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return null;
            }

            try
            {
                // Only expose error fields, never a raw authentication response
                // that could contain a token alongside the failure details.
                return string.Join("; ", ErrorMessages(JToken.Parse(body)));
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static IEnumerable<string> ErrorMessages(JToken token)
        {
            if (token is JValue && token.Type == JTokenType.String)
            {
                if (!string.IsNullOrWhiteSpace((string)token)) yield return (string)token;
            }
            else if (token is JArray)
            {
                foreach (var child in token.Children()) foreach (var message in ErrorMessages(child)) yield return message;
            }
            else if (token is JObject)
            {
                foreach (string key in new[] { "error", "message", "description", "errors" })
                    if (token[key] != null) foreach (var message in ErrorMessages(token[key])) yield return message;
            }
        }

        private static string DecimalText(decimal value)
        {
            return value.ToString("0.00############", CultureInfo.InvariantCulture);
        }

        private static string DecimalTextOrNull(decimal value)
        {
            return value == 0m ? null : DecimalText(value);
        }

        private static int? NullableInt(object value)
        {
            if (value == null)
            {
                return null;
            }

            int converted = Convert.ToInt32(value, CultureInfo.InvariantCulture);
            return converted == 0 ? null : (int?)converted;
        }

        private static string EmptyToNull(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static string FirstNonEmpty(params string[] values)
        {
            return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
        }

        private sealed class ApiResult
        {
            public ApiResult(HttpStatusCode statusCode, string reasonPhrase, string body)
            {
                StatusCode = statusCode;
                ReasonPhrase = reasonPhrase;
                Body = body;
            }

            public HttpStatusCode StatusCode { get; private set; }
            public string ReasonPhrase { get; private set; }
            public string Body { get; private set; }
            public bool IsSuccess { get { return (int)StatusCode >= 200 && (int)StatusCode <= 299; } }
        }

        private sealed class ProviderSignatureRequest
        {
            [JsonProperty("vat_number")]
            public string VatNumber { get; set; }

            [JsonProperty("series")]
            public string Series { get; set; }

            [JsonProperty("aa")]
            public string Number { get; set; }

            [JsonProperty("issue_date")]
            public string IssueDate { get; set; }

            [JsonProperty("invoice_type")]
            public string InvoiceType { get; set; }

            [JsonProperty("branch")]
            public int Branch { get; set; }

            [JsonProperty("mark")]
            public string Mark { get; set; }

            [JsonProperty("amount")]
            public string Amount { get; set; }

            [JsonProperty("total_net_value")]
            public string TotalNetValue { get; set; }

            [JsonProperty("total_vat_amount")]
            public string TotalVatAmount { get; set; }

            [JsonProperty("total_gross_value")]
            public string TotalGrossValue { get; set; }

            [JsonProperty("tid")]
            public string Tid { get; set; }

            [JsonProperty("acq_id")]
            public int? AcquirerId { get; set; }

            [JsonProperty("is_estiasi", DefaultValueHandling = DefaultValueHandling.Include)]
            public bool IsCatering { get; set; }
        }

        private sealed class ProviderSignatureReply
        {
            [JsonProperty("uid")]
            public string Uid { get; set; }

            [JsonProperty("signature")]
            public string Signature { get; set; }

            [JsonProperty("input_formatted_values")]
            public string InputFormattedValues { get; set; }

            [JsonProperty("signature_b64")]
            public string SignatureBase64 { get; set; }

            [JsonProperty("error")]
            public string Error { get; set; }

            [JsonProperty("message")]
            public string Message { get; set; }
        }

        private sealed class TokenCreateRequest
        {
            [JsonProperty("subscription_id")]
            public string SubscriptionId { get; set; }
        }

        private sealed class TokenRenewRequest
        {
            [JsonProperty("token")]
            public string Token { get; set; }
        }

        private sealed class TokenCache
        {
            [JsonProperty("connectionToken")]
            public string ConnectionToken { get; set; }
            [JsonProperty("expiresAt")]
            public DateTimeOffset ExpiresAt { get; set; }
        }

        private sealed class TokenReply
        {
            [JsonProperty("expires_at")]
            public DateTimeOffset? ExpiresAt { get; set; }

            [JsonProperty("token")]
            public string Token { get; set; }

            [JsonProperty("error")]
            public string Error { get; set; }

            [JsonProperty("message")]
            public string Message { get; set; }
        }

        private sealed class ITimologiaInvoiceRequest
        {
            [JsonProperty("notes")]
            public string Notes { get; set; }

            [JsonProperty("ext_system_id")]
            public string ExternalSystemId { get; set; }

            [JsonProperty("issuer")]
            public IssuerRequest Issuer { get; set; }

            [JsonProperty("counterpart")]
            public CounterpartRequest Counterpart { get; set; }

            [JsonProperty("invoice_header")]
            public InvoiceHeaderRequest Header { get; set; }

            [JsonProperty("payment_methods")]
            public List<PaymentMethodRequest> PaymentMethods { get; set; }

            [JsonProperty("invoice_details")]
            public List<InvoiceDetailRequest> Details { get; set; }

            [JsonProperty("taxes_totals")]
            public List<TaxesTotalRequest> TaxesTotals { get; set; }

            [JsonProperty("invoice_summary")]
            public InvoiceSummaryRequest Summary { get; set; }
        }

        private abstract class TraderRequest
        {
            [JsonProperty("vat_number")]
            public string VatNumber { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("country")]
            public string Country { get; set; }

            [JsonProperty("branch")]
            public int? Branch { get; set; }

            [JsonProperty("supply_account_no")]
            public string SupplyAccountNumber { get; set; }

            [JsonProperty("address")]
            public AddressRequest Address { get; set; }
        }

        private sealed class IssuerRequest : TraderRequest { }
        private sealed class CounterpartRequest : TraderRequest { }

        private sealed class AddressRequest
        {
            [JsonProperty("street")]
            public string Street { get; set; }

            [JsonProperty("number")]
            public string Number { get; set; }

            [JsonProperty("postal_code")]
            public string PostalCode { get; set; }

            [JsonProperty("city")]
            public string City { get; set; }
        }

        private sealed class InvoiceHeaderRequest
        {
            [JsonProperty("series")]
            public string Series { get; set; }

            [JsonProperty("aa")]
            public string Number { get; set; }

            [JsonProperty("issue_date")]
            public string IssueDate { get; set; }

            [JsonProperty("invoice_type")]
            public string InvoiceType { get; set; }

            [JsonProperty("vat_payment_suspension")]
            public bool? VatPaymentSuspension { get; set; }

            [JsonProperty("currency")]
            public string Currency { get; set; }

            [JsonProperty("exchange_rate")]
            public string ExchangeRate { get; set; }

            [JsonProperty("correlated_invoices")]
            public List<long> CorrelatedInvoices { get; set; }

            [JsonProperty("self_pricing")]
            public bool? SelfPricing { get; set; }

            [JsonProperty("dispatch_date")]
            public string DispatchDate { get; set; }

            [JsonProperty("dispatch_time")]
            public string DispatchTime { get; set; }

            [JsonProperty("vehicle_number")]
            public string VehicleNumber { get; set; }

            [JsonProperty("move_purpose")]
            public int? MovePurpose { get; set; }

            [JsonProperty("fuel_invoice")]
            public bool? FuelInvoice { get; set; }

            // The vendor's iTimologiaClass.cs declares this JSON field as string.
            [JsonProperty("special_invoice_category")]
            public string SpecialInvoiceCategory { get; set; }

            [JsonProperty("is_delivery_note")]
            public bool? IsDeliveryNote { get; set; }

            [JsonProperty("other_delivery_note_header")]
            public OtherDeliveryNoteHeaderRequest OtherDeliveryNoteHeader { get; set; }
        }

        private sealed class OtherDeliveryNoteHeaderRequest
        {
            [JsonProperty("start_shipping_branch")]
            public int StartShippingBranch { get; set; }

            [JsonProperty("complete_shipping_branch")]
            public int CompleteShippingBranch { get; set; }

            [JsonProperty("loading_address")]
            public AddressRequest LoadingAddress { get; set; }

            [JsonProperty("delivery_address")]
            public AddressRequest DeliveryAddress { get; set; }
        }

        private sealed class PaymentMethodRequest
        {
            [JsonProperty("type")]
            public int Type { get; set; }

            [JsonProperty("amount")]
            public string Amount { get; set; }

            [JsonProperty("paymentmethodinfo")]
            public string Information { get; set; }

            [JsonProperty("transaction_id")]
            public string TransactionId { get; set; }

            [JsonProperty("tid")]
            public string Tid { get; set; }

            [JsonProperty("providers_signature")]
            public string ProviderSignature { get; set; }
        }

        private sealed class InvoiceDetailRequest
        {
            [JsonProperty("quantity15")]
            public decimal? Quantity15 { get; set; }
            [JsonProperty("line_number")]
            public int? LineNumber { get; set; }

            [JsonProperty("item_descr")]
            public string ItemDescription { get; set; }

            [JsonProperty("item_code")]
            public string ItemCode { get; set; }

            [JsonProperty("fuel_code")]
            public string FuelCode { get; set; }

            [JsonProperty("quantity")]
            public decimal? Quantity { get; set; }

            [JsonProperty("measurement_unit")]
            public int? MeasurementUnit { get; set; }

            [JsonProperty("invoice_detail_type")]
            public int? InvoiceDetailType { get; set; }

            [JsonProperty("net_value")]
            public string NetValue { get; set; }

            [JsonProperty("vat_category")]
            public int VatCategory { get; set; }

            [JsonProperty("vat_amount")]
            public string VatAmount { get; set; }

            [JsonProperty("vat_exemption_category")]
            public int? VatExemptionCategory { get; set; }

            [JsonProperty("discount_option")]
            public bool? DiscountOption { get; set; }

            [JsonProperty("withheld_amount")]
            public string WithheldAmount { get; set; }

            [JsonProperty("withheld_percent_category")]
            public int? WithheldPercentCategory { get; set; }

            [JsonProperty("stamp_duty_amount")]
            public string StampDutyAmount { get; set; }

            [JsonProperty("stamp_duty_percent_category")]
            public int? StampDutyPercentCategory { get; set; }

            [JsonProperty("fees_amount")]
            public string FeesAmount { get; set; }

            [JsonProperty("fees_percent_category")]
            public int? FeesPercentCategory { get; set; }

            [JsonProperty("other_taxes_amount")]
            public string OtherTaxesAmount { get; set; }

            [JsonProperty("other_taxes_percent_category")]
            public int? OtherTaxesPercentCategory { get; set; }

            [JsonProperty("deductions_amount")]
            public string DeductionsAmount { get; set; }

            [JsonProperty("line_comments")]
            public string LineComments { get; set; }

            [JsonProperty("income_classification")]
            public List<IncomeClassificationRequest> IncomeClassifications { get; set; }
        }

        private sealed class IncomeClassificationRequest
        {
            [JsonProperty("classification_type")]
            public string ClassificationType { get; set; }

            [JsonProperty("classification_category")]
            public string ClassificationCategory { get; set; }

            [JsonProperty("amount")]
            public string Amount { get; set; }
        }

        private sealed class TaxesTotalRequest
        {
            [JsonProperty("tax_type")]
            public string TaxType { get; set; }

            [JsonProperty("tax_category")]
            public int? TaxCategory { get; set; }

            [JsonProperty("underlying_value")]
            public string UnderlyingValue { get; set; }

            [JsonProperty("tax_amount")]
            public string TaxAmount { get; set; }
        }

        private sealed class InvoiceSummaryRequest
        {
            [JsonProperty("total_net_value")]
            public string TotalNetValue { get; set; }

            [JsonProperty("total_vat_amount")]
            public string TotalVatAmount { get; set; }

            [JsonProperty("total_withheld_amount")]
            public string TotalWithheldAmount { get; set; }

            [JsonProperty("total_fees_amount")]
            public string TotalFeesAmount { get; set; }

            [JsonProperty("total_stamp_duty_amount")]
            public string TotalStampDutyAmount { get; set; }

            [JsonProperty("total_other_taxes_amount")]
            public string TotalOtherTaxesAmount { get; set; }

            [JsonProperty("total_deductions_amount")]
            public string TotalDeductionsAmount { get; set; }

            [JsonProperty("total_gross_value")]
            public string TotalGrossValue { get; set; }

            [JsonProperty("income_classification")]
            public List<IncomeClassificationRequest> IncomeClassifications { get; set; }
        }

        private sealed class ITimologiaInvoiceReply
        {
            [JsonProperty("data")]
            public ITimologiaInvoiceData Data { get; set; }
        }

        private sealed class ITimologiaInvoiceData
        {
            [JsonProperty("ext_system_id")]
            public string ExternalSystemId { get; set; }

            [JsonProperty("cancelled_by_mark")]
            public string CancelledByMark { get; set; }

            [JsonProperty("uid")]
            public string Uid { get; set; }

            [JsonProperty("mark")]
            public string Mark { get; set; }

            [JsonProperty("authentication_code")]
            public string AuthenticationCode { get; set; }

            [JsonProperty("signature_code")]
            public string SignatureCode { get; set; }

            [JsonProperty("web_view")]
            public string WebView { get; set; }

            [JsonProperty("transmission_messages")]
            public List<TransmissionMessage> TransmissionMessages { get; set; }
        }

        private sealed class TransmissionMessage
        {
            [JsonProperty("code")]
            public string Code { get; set; }

            [JsonProperty("message")]
            public string Message { get; set; }
        }
    }
}
