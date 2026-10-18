using Exedron.Invoicing.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Exedron.Invoicing.Adapters
{
    public sealed class GBridgeClient : IPaymentGateway
    {
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.None
        };

        private readonly GBridgeOptions options;
        private readonly HttpClient httpClient;
        private readonly bool ownsHttpClient;

        public GBridgeClient(GBridgeOptions options)
            : this(options, null)
        {
        }

        public GBridgeClient(GBridgeOptions options, HttpMessageHandler handler)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            httpClient = LoggedHttpClient.Create(options.Logger, "GBridge", "Terminal", handler);
            ownsHttpClient = true;
            httpClient.Timeout = options.Timeout > TimeSpan.Zero
                ? options.Timeout
                : TimeSpan.FromSeconds(220);
        }

        public string TerminalId
        {
            get { return options.TerminalId == null ? null : options.TerminalId.Trim(); }
        }

        public string ValidateConfiguration()
        {
            if (string.IsNullOrWhiteSpace(options.TerminalId))
            {
                return "The GBridge POS Terminal ID is not configured.";
            }

            string endpoint;
            string error;
            return TryNormalizeEndpoint(options.Endpoint, out endpoint, out error) ? null : error;
        }

        public PaymentAuthorizationResult Authorize(PaymentAuthorizationRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            GBridgePaymentResult result = Pay(
                request.TransactionUid,
                request.Operation,
                request.PaymentType,
                request.ProviderId,
                request.ProviderInput,
                request.ProviderSignature,
                request.ExpectedTerminalId,
                request.ExpectedAmountMinorUnits,
                request.RecoverOnly);
            return new PaymentAuthorizationResult
            {
                TransactionId = result.TransactionId,
                TerminalId = result.TerminalId,
                Rrn = result.Rrn,
                AuthorizationCode = result.AuthorizationCode,
                AmountMinorUnits = result.AmountMinorUnits,
                ResponseTransactionUid = result.ResponseTransactionUid,
                TransactionType = result.TransactionType,
                PaymentType = result.PaymentType,
                Stan = result.Stan,
                BatchNumber = result.BatchNumber,
                Acquirer = result.Acquirer,
                MerchantId = result.MerchantId,
                MaskedPan = result.MaskedPan,
                CardType = result.CardType,
                TransactionTimestamp = result.TransactionTimestamp,
                VoidTimestamp = result.VoidTimestamp,
                MerchantReceipt = result.MerchantReceipt,
                CustomerReceipt = result.CustomerReceipt
            };
        }

        public GBridgePaymentResult Pay(
            string transactionUid,
            ProviderPaymentOperation operation,
            int paymentType,
            int providerId,
            string providerInput,
            string providerSignature,
            string expectedTerminalId,
            int expectedAmountMinorUnits,
            bool recoverOnly = false)
        {
            ValidateCommon(transactionUid);
            if (paymentType != 0 && paymentType != 1)
            {
                throw new ArgumentOutOfRangeException(nameof(paymentType), "GBridge payment type must be card (0) or IRIS (1).");
            }
            if (providerId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(providerId), "A valid GBridge provider ID is required.");
            }
            if (expectedAmountMinorUnits <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(expectedAmountMinorUnits), "The GBridge payment amount must be greater than zero.");
            }
            if (string.IsNullOrWhiteSpace(providerInput))
            {
                throw new ArgumentException("The provider input returned by iTimologia is empty.", nameof(providerInput));
            }
            if (string.IsNullOrWhiteSpace(providerSignature))
            {
                throw new ArgumentException("The provider signature returned by iTimologia is empty.", nameof(providerSignature));
            }

            var request = new GBridgeRequest
            {
                InputType = "1",
                Tid = options.TerminalId.Trim(),
                TransUid = transactionUid,
                TransactionData = new GBridgeTransactionRequest
                {
                    TxnType = (int)operation,
                    IsTaxFree = false,
                    PaymentType = paymentType,
                    Tip = 0,
                    CashBack = 0,
                    Instalments = 0,
                    ProviderId = providerId,
                    ProviderInput = providerInput,
                    ProviderSignature = providerSignature
                }
            };

            GBridgeResponse response;
            if (recoverOnly)
            {
                response = RecoverPayment(
                    transactionUid,
                    new GBridgeException("A previous GBridge payment request has an unknown outcome."),
                    FirstNonEmpty(expectedTerminalId, options.TerminalId), expectedAmountMinorUnits, (int)operation, paymentType);
            }
            else
            {
                try
                {
                    response = Post(request);
                }
                catch (Exception ex) when (
                    ex is TaskCanceledException ||
                    ex is HttpRequestException ||
                    (ex is GBridgeException && ((GBridgeException)ex).FailureKind != GBridgeFailureKind.SignatureRejected &&
                     ((GBridgeException)ex).FailureKind != GBridgeFailureKind.CredentialsRejected))
                {
                    response = RecoverPayment(transactionUid, ex,
                        FirstNonEmpty(expectedTerminalId, options.TerminalId), expectedAmountMinorUnits, (int)operation, paymentType);
                }
                // A returned F4 is retained as Unknown. Only an explicit operator retry
                // may replace it; ordinary processing and recovery never reauthorize it.
            }

            GBridgeEftReplyData reply = RequireApprovedTransaction(
                response,
                false,
                transactionUid,
                FirstNonEmpty(expectedTerminalId, options.TerminalId),
                expectedAmountMinorUnits,
                (int)operation,
                paymentType);
            return new GBridgePaymentResult
            {
                TransactionId = reply.EftposTranId,
                TerminalId = FirstNonEmpty(reply.TransactionData.Tid, options.TerminalId),
                Rrn = reply.TransactionData.Rrn,
                AuthorizationCode = reply.TransactionData.AuthCode,
                AmountMinorUnits = reply.TransactionData.Amount,
                ResponseTransactionUid = response.TransUid,
                TransactionType = reply.TransactionData.TxnType,
                PaymentType = reply.TransactionData.PaymentType,
                Stan = reply.TransactionData.Stan,
                BatchNumber = reply.TransactionData.BatchNumber,
                Acquirer = reply.TransactionData.Acquirer,
                MerchantId = reply.TransactionData.Mid,
                MaskedPan = reply.TransactionData.CardPan,
                CardType = reply.TransactionData.CardType,
                TransactionTimestamp = reply.TransactionData.Timestamp,
                VoidTimestamp = reply.TransactionData.VoidTimestamp,
                MerchantReceipt = reply.TransactionData.MerchantReceipt,
                CustomerReceipt = reply.TransactionData.CustomerReceipt
            };
        }

        public void Void(string transactionUid, string originalTransactionId)
        {
            ValidateCommon(transactionUid);
            if (string.IsNullOrWhiteSpace(originalTransactionId))
            {
                throw new ArgumentException("The original GBridge transaction ID is required for a void.", nameof(originalTransactionId));
            }

            var request = new GBridgeRequest
            {
                InputType = "2",
                Tid = options.TerminalId.Trim(),
                TransUid = transactionUid,
                CommandParams = originalTransactionId
            };

            try
            {
                GBridgeResponse response = Post(request);
                RequireApprovedTransaction(
                    response,
                    true,
                    transactionUid,
                    options.TerminalId,
                    null,
                    null,
                    null);
                return;
            }
            catch (Exception ex) when (
                ex is TaskCanceledException ||
                ex is HttpRequestException ||
                ex is GBridgeException)
            {
                GBridgeResponse recoveredResponse = Query("4", transactionUid, ex);
                RequireApprovedTransaction(
                    recoveredResponse,
                    true,
                    transactionUid,
                    options.TerminalId,
                    null,
                    null,
                    null);
            }
        }

        // Strict host entry point: retries query only and verify the original approved
        // transaction as well as the amount, terminal and payment/operation types.
        public void Void(PaymentVoidRequest request)
        {
            if (request == null || request.TerminalId != TerminalId || request.AmountMinorUnits <= 0 ||
                string.IsNullOrWhiteSpace(request.OriginalTransactionId)) throw new ArgumentException("Original payment evidence is required.");
            ValidateCommon(request.TransactionUid);
            GBridgeResponse response;
            if (request.RecoverOnly) response = Query("4", request.TransactionUid, new InvalidOperationException("Recover saved void only."));
            else
            {
                try { response = Post(new GBridgeRequest { InputType = "2", Tid = TerminalId,
                    TransUid = request.TransactionUid, CommandParams = request.OriginalTransactionId }); }
                catch (Exception ex) { response = Query("4", request.TransactionUid, ex); }
            }
            var result = RequireApprovedTransaction(response, true, request.TransactionUid, request.TerminalId,
                request.AmountMinorUnits, request.TransactionType, request.PaymentType);
            if (result.EftposTranId != request.OriginalTransactionId)
                throw new GBridgeException("Void evidence does not identify the original transaction.", GBridgeFailureKind.Mismatch);
        }

        public void Dispose()
        {
            if (ownsHttpClient)
            {
                httpClient.Dispose();
            }
        }

        // Only the explicit, correlated validation response proves this authorization was rejected.
        // FE alone also covers ambiguous transport failures and must never enable a second charge.
        public static bool IsProviderSignatureRejection(string transactionUid, string responseJson, int? httpStatus)
        {
            if (string.IsNullOrWhiteSpace(transactionUid) || string.IsNullOrWhiteSpace(responseJson) ||
                (httpStatus != 400 && httpStatus != 200)) return false;
            try
            {
                var response = JObject.Parse(responseJson);
                var transactions = response["EFTReplyData"] as JArray;
                return (string)response["InputType"] == "1" &&
                    (string)response["TransUid"] == transactionUid &&
                    (string)response["Status"] == "400" && (string)response["ReplyCode"] == "FE" &&
                    (string)response["EftposReplyCode"] == "400" &&
                    string.Equals(((string)response["ReplyDesc"] ?? "").Trim(),
                        "Eftpos Communication Error: 400 BAD_REQUEST & Invalid provider signature",
                        StringComparison.OrdinalIgnoreCase) && transactions != null && transactions.Count == 0;
            }
            catch (JsonException) { return false; }
        }

        public static bool IsIlydaSignatureEncodingRejection(string transactionUid, string requestJson, string responseJson, int? httpStatus)
        {
            if (httpStatus != 400 || string.IsNullOrWhiteSpace(transactionUid) ||
                string.IsNullOrWhiteSpace(requestJson) || string.IsNullOrWhiteSpace(responseJson)) return false;
            try
            {
                var request = JObject.Parse(requestJson);
                var response = JObject.Parse(responseJson);
                var transaction = request["TransactionData"] as JObject;
                string signature = (string)transaction?["ProviderSignature"];
                if ((string)request["InputType"] != "1" || (string)request["TransUid"] != transactionUid ||
                    (int?)transaction?["ProviderId"] != InvoiceProviderCatalog.Get(InvoiceProviderCatalog.DirectIlydaKey).AadeProviderId ||
                    string.IsNullOrWhiteSpace(signature) || signature.All(Uri.IsHexDigit) ||
                    Convert.FromBase64String(signature).Length == 0) return false;
                var transactions = response["EFTReplyData"] as JArray;
                return (string)response["InputType"] == "1" && (string)response["TransUid"] == transactionUid &&
                    (string)response["Status"] == "400" && (string)response["ReplyCode"] == "FE" &&
                    (string)response["EftposReplyCode"] == "500" &&
                    (string)response["ReplyDesc"] == "Eftpos Communication Error: 500 INTERNAL_SERVER_ERROR & providerData.signature: Signature must not be null or empty" &&
                    transactions != null && transactions.Count == 0;
            }
            catch (JsonException) { return false; }
            catch (FormatException) { return false; }
        }

        public static bool IsNspCredentialsRejection(string transactionUid, string responseJson, int? httpStatus)
        {
            if (string.IsNullOrWhiteSpace(transactionUid) || string.IsNullOrWhiteSpace(responseJson) ||
                (httpStatus != 401 && httpStatus != 200 && httpStatus != 400)) return false;
            try
            {
                var response = JObject.Parse(responseJson);
                var transactions = response["EFTReplyData"] as JArray;
                // The vendor lists F7; the captured 401 response uses F0. F0 alone is a generic server error.
                return (string)response["InputType"] == "1" && (string)response["TransUid"] == transactionUid &&
                    (string)response["Status"] == "401" && (string)response["EftposReplyCode"] == "401" &&
                    ((string)response["ReplyCode"] == "F7" || (string)response["ReplyCode"] == "F0") &&
                    string.Equals(((string)response["ReplyDesc"] ?? "").Trim(),
                        "NSP Credentials Issue. Call Support Department", StringComparison.OrdinalIgnoreCase) &&
                    transactions != null && transactions.Count == 0;
            }
            catch (JsonException) { return false; }
        }

        public static bool HasOnlySignatureRejectedAuthorizations(string transactionUid, IEnumerable<PaymentGatewayEvent> events, int providerId = 0)
        {
            return HasOnlyRejectedAuthorizations(transactionUid, events, providerId, false);
        }

        public static bool HasOnlyRejectedAuthorizations(string transactionUid, IEnumerable<PaymentGatewayEvent> events, int providerId = 0)
        {
            return HasOnlyRejectedAuthorizations(transactionUid, events, providerId, true);
        }

        private static bool HasOnlyRejectedAuthorizations(string transactionUid, IEnumerable<PaymentGatewayEvent> events, int providerId, bool includeCredentials)
        {
            bool rejected = false;
            foreach (var entry in events)
            {
                if (entry.TransactionUid != transactionUid) return false;
                // Provider signing is not a POS authorization and cannot itself charge a card.
                if (entry.InputType == "SIGNATURE") continue;
                if (entry.InputType == "1")
                {
                    if (!(includeCredentials && IsNspCredentialsRejection(transactionUid, entry.ResponseJson, entry.HttpStatus)) &&
                        !IsProviderSignatureRejection(transactionUid, entry.ResponseJson, entry.HttpStatus) &&
                        !(providerId == InvoiceProviderCatalog.Get(InvoiceProviderCatalog.DirectIlydaKey).AadeProviderId &&
                          IsIlydaSignatureEncodingRejection(transactionUid, entry.RequestJson, entry.ResponseJson, entry.HttpStatus))) return false;
                    rejected = true;
                }
                else
                {
                    if (entry.InputType != "3" && entry.InputType != "4") return false;
                    if (string.IsNullOrWhiteSpace(entry.ResponseJson)) continue;
                    try
                    {
                        var response = JObject.Parse(entry.ResponseJson);
                        var data = response["EFTReplyData"];
                        // Never override a recovered transaction (approved, declined or otherwise).
                        if (data != null && data.Type != JTokenType.Null &&
                            (!(data is JArray) || ((JArray)data).Count != 0)) return false;
                        if ((string)response["ReplyCode"] != "0E" && (string)response["ReplyCode"] != "F2") return false;
                    }
                    catch (JsonException) { return false; }
                }
            }
            return rejected;
        }

        // Explicit installation policy: these exact responses permit an operator-requested replacement.
        // The original outcome and all response evidence are retained; this is not a reported decline.
        public static bool CanRetryAfterF4(string transactionUid, string terminalId, IList<PaymentGatewayEvent> events,
            string providerInput = null, int? transactionType = null, int? paymentType = null, int? providerId = null)
        {
            if (IsSingleF4(transactionUid, terminalId, events, providerInput, transactionType, paymentType, providerId)) return true;
            if (events == null || events.Count < 3 || string.IsNullOrWhiteSpace(transactionUid) || string.IsNullOrWhiteSpace(terminalId)) return false;
            bool sawAuthorization = false;
            for (int i = 0; i < events.Count; i++)
            {
                var item = events[i];
                if (item == null) return false;
                if (item.InputType == "SIGNATURE") continue;
                try
                {
                    var request = JObject.Parse(item.RequestJson ?? "");
                    var response = JObject.Parse(item.ResponseJson ?? "");
                    var data = response["EFTReplyData"] as JArray;
                    if ((string)request["InputType"] != item.InputType || (string)response["InputType"] != item.InputType ||
                        (string)request["TransUid"] != transactionUid || (string)response["TransUid"] != transactionUid ||
                        item.TransactionUid != transactionUid || (string)request["Tid"] != terminalId || data == null || data.Count != 0)
                        return false;
                    if (item.InputType == "1")
                    {
                        var transaction = request["TransactionData"];
                        // GBridge receives the amount in the provider's signed input, not an Amount request field.
                        if (providerInput != null && (string)transaction?["ProviderInput"] != providerInput ||
                            transactionType.HasValue && (int?)transaction?["TxnType"] != transactionType ||
                            paymentType.HasValue && (int?)transaction?["PaymentType"] != paymentType ||
                            providerId.HasValue && (int?)transaction?["ProviderId"] != providerId) return false;
                        if (sawAuthorization || i >= events.Count - 2 || item.HttpStatus != 200 ||
                            (string)response["Status"] != "200" || (string)response["ReplyCode"] != "F4" ||
                            (string)response["EftposReplyCode"] != "200") return false;
                        sawAuthorization = true;
                    }
                    else if (item.InputType == "4")
                    {
                        if (!sawAuthorization || (item.HttpStatus != 200 && item.HttpStatus != 400) ||
                            (string)response["Status"] != "400" || (string)response["ReplyCode"] != "F2" ||
                            !string.IsNullOrEmpty((string)response["EftposReplyCode"])) return false;
                    }
                    else if (item.InputType == "3")
                    {
                        if (!sawAuthorization || item.HttpStatus != 200 || (string)response["Status"] != "200" ||
                            (string)response["ReplyCode"] != "0E" || (string)response["EftposReplyCode"] != "200") return false;
                    }
                    else return false;
                }
                catch (Exception ex) when (ex is JsonException || ex is ArgumentException || ex is InvalidCastException) { return false; }
            }
            return sawAuthorization && events[events.Count - 2].InputType == "4" && events[events.Count - 1].InputType == "3";
        }

        public static bool IsSingleF4(string uid, string tid, IList<PaymentGatewayEvent> events,
            string providerInput = null, int? transactionType = null, int? paymentType = null, int? providerId = null)
        {
            if (events == null || string.IsNullOrWhiteSpace(uid) || string.IsNullOrWhiteSpace(tid)) return false;
            var gatewayEvents = events.Where(e => e == null || e.InputType != "SIGNATURE").ToArray();
            if (gatewayEvents.Length != 1 || gatewayEvents[0] == null) return false;
            var item = gatewayEvents[0];
            if (item.InputType != "1" || item.TransactionUid != uid || item.HttpStatus != 200) return false;
            try
            {
                var request = JObject.Parse(item.RequestJson ?? ""); var response = JObject.Parse(item.ResponseJson ?? "");
                var transaction = request["TransactionData"]; var data = response["EFTReplyData"];
                return (string)request["InputType"] == "1" && (string)response["InputType"] == "1" &&
                    (string)request["TransUid"] == uid && (string)response["TransUid"] == uid && (string)request["Tid"] == tid &&
                    (string)response["Status"] == "200" && (string)response["ReplyCode"] == "F4" && (string)response["EftposReplyCode"] == "200" &&
                    (data == null || data.Type == JTokenType.Null || data is JArray && ((JArray)data).Count == 0) &&
                    (providerInput == null || (string)transaction?["ProviderInput"] == providerInput) &&
                    (!transactionType.HasValue || (int?)transaction?["TxnType"] == transactionType) &&
                    (!paymentType.HasValue || (int?)transaction?["PaymentType"] == paymentType) &&
                    (!providerId.HasValue || (int?)transaction?["ProviderId"] == providerId);
            }
            catch (Exception ex) when (ex is JsonException || ex is ArgumentException || ex is InvalidCastException) { return false; }
        }

        // Replay only the recorded transaction, using the same validator as live recovery.
        // A later void, contradictory result or second authorization prevents restoration.
        public static PaymentRecoveryEvidence ReadRecordedOutcome(ProviderPaymentAttempt attempt, IList<PaymentGatewayEvent> events)
        {
            if (attempt == null || events == null) return null;
            var paymentEvents = events.Where(e => e == null || e.InputType != "SIGNATURE").ToArray();
            if (paymentEvents.Any(e => e == null || e.TransactionUid != attempt.TransactionUid ||
                e.InputType != "1" && e.InputType != "3" && e.InputType != "4") || paymentEvents.Count(e => e.InputType == "1") > 1) return null;
            PaymentRecoveryEvidence found = null;
            foreach (var entry in paymentEvents.Reverse())
            {
                try
                {
                    if (entry.HttpStatus != 200 || string.IsNullOrWhiteSpace(entry.ResponseJson) || string.IsNullOrWhiteSpace(entry.RequestJson)) continue;
                    var request = JsonConvert.DeserializeObject<GBridgeRequest>(entry.RequestJson, JsonSettings);
                    var response = JsonConvert.DeserializeObject<GBridgeResponse>(entry.ResponseJson, JsonSettings);
                    if (request == null || request.InputType != entry.InputType ||
                        request.TransUid != attempt.TransactionUid || request.Tid != attempt.TerminalId) return null;
                    if (response == null) continue;
                    if (response.InputType != entry.InputType)
                    {
                        if (response.EftReplyData == null || response.EftReplyData.Count == 0) continue;
                        return null;
                    }
                    if (response.EftReplyData != null && response.EftReplyData.Any(r => r?.TransactionData?.Voided == true)) return null;
                    if (HasSuccessfulEnvelope(response) && response.EftReplyData != null && response.EftReplyData.Any(r =>
                        r?.TransactionData?.Approved == true && !MatchesTransactionUid(r.TransactionData, attempt.TransactionUid) &&
                        !MatchesDatabaseShortReference(response, r.TransactionData, attempt.TransactionUid))) return null;
                    var reply = RequireApprovedTransaction(response, false, attempt.TransactionUid, attempt.TerminalId,
                        attempt.AmountMinorUnits, (int)attempt.Operation, attempt.PaymentType);
                    var data = reply.TransactionData;
                    if (found?.Approval != null && found.Approval.TransactionId != reply.EftposTranId) return null;
                    found = new PaymentRecoveryEvidence { Approval = new PaymentAuthorizationResult {
                        TransactionId = reply.EftposTranId, TerminalId = data.Tid, ResponseTransactionUid = response.TransUid,
                        AmountMinorUnits = data.Amount, TransactionType = data.TxnType, PaymentType = data.PaymentType,
                        Rrn = data.Rrn, AuthorizationCode = data.AuthCode, Stan = data.Stan, BatchNumber = data.BatchNumber,
                        Acquirer = data.Acquirer, MerchantId = data.Mid, MaskedPan = data.CardPan, CardType = data.CardType,
                        TransactionTimestamp = data.Timestamp, MerchantReceipt = data.MerchantReceipt, CustomerReceipt = data.CustomerReceipt } };
                }
                catch (GBridgeException ex)
                {
                    if (ex.FailureKind == GBridgeFailureKind.Mismatch || ex.FailureKind == GBridgeFailureKind.Declined) return null;
                    if (ex.FailureKind == GBridgeFailureKind.IncompleteResponse && found == null)
                        found = new PaymentRecoveryEvidence { IncompleteMessage = ex.Message };
                }
                catch (JsonException) { continue; }
            }
            return found;
        }

        private GBridgeResponse RecoverPayment(string transactionUid, Exception originalError,
            string terminalId, int amountMinorUnits, int transactionType, int paymentType)
        {
            string databaseDetails;
            GBridgeResponse response = TryQuery("4", transactionUid, out databaseDetails);
            if (HasResolvedTransaction(response, transactionUid, terminalId, amountMinorUnits, transactionType, paymentType))
            {
                return response;
            }
            var incomplete = HasIncompleteTransaction(response, transactionUid, terminalId, amountMinorUnits, transactionType, paymentType) ? response : null;

            string terminalDetails;
            response = TryQuery("3", transactionUid, out terminalDetails);
            if (HasResolvedTransaction(response, transactionUid, terminalId, amountMinorUnits, transactionType, paymentType))
            {
                return response;
            }

            if (HasIncompleteTransaction(response, transactionUid, terminalId, amountMinorUnits, transactionType, paymentType)) return response;
            if (incomplete != null) return incomplete;

            throw new GBridgeException(
                "GBridge payment outcome remains unconfirmed. No new payment was sent during recovery. " +
                "TransUid=" + transactionUid + ", TID=" + options.TerminalId + ".\r\n" +
                "Database lookup (4): " + databaseDetails + "\r\n" +
                "Terminal lookup (3): " + terminalDetails + "\r\n" +
                "Original error: " + originalError.Message,
                GBridgeFailureKind.Unknown,
                originalError);
        }

        private GBridgeResponse Query(string inputType, string transactionUid, Exception originalError)
        {
            string details;
            GBridgeResponse response = TryQuery(inputType, transactionUid, out details);
            if (response != null)
            {
                return response;
            }

            throw new GBridgeException(
                "GBridge transaction outcome could not be recovered. Lookup (" + inputType + "): " +
                details + " Original error: " + originalError.Message,
                GBridgeFailureKind.Unknown,
                originalError);
        }

        private GBridgeResponse TryQuery(string inputType, string transactionUid, out string details)
        {
            try
            {
                var response = Post(new GBridgeRequest
                {
                    InputType = inputType,
                    Tid = options.TerminalId.Trim(),
                    TransUid = transactionUid
                });
                details = "Status=" + response.Status + ", ReplyCode=" + response.ReplyCode +
                    ", EftposReplyCode=" + response.EftposReplyCode + ", Message=" +
                    FirstNonEmpty(response.EftposReplyMessage, response.ReplyDesc, "No details returned.") +
                    ", Transactions=" + (response.EftReplyData == null ? 0 : response.EftReplyData.Count());
                return response;
            }
            catch (Exception ex)
            {
                details = ex.GetBaseException().Message;
                return null;
            }
        }

        private GBridgeResponse Post(GBridgeRequest body)
        {
            string endpoint;
            string endpointError;
            if (!TryNormalizeEndpoint(options.Endpoint, out endpoint, out endpointError))
            {
                throw new InvalidOperationException(endpointError);
            }

            string json = JsonConvert.SerializeObject(body, JsonSettings);
            string responseBody = null;
            int? httpStatus = null;
            GBridgeResponse parsedResponse = null;
            Exception failure = null;
            DateTimeOffset startedUtc = DateTimeOffset.UtcNow;
            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Post, endpoint))
                {
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                    using (HttpResponseMessage response = httpClient.SendAsync(request).GetAwaiter().GetResult())
                    {
                        httpStatus = (int)response.StatusCode;
                        responseBody = response.Content == null
                            ? string.Empty
                            : response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                        if (!string.IsNullOrWhiteSpace(responseBody))
                        {
                            try
                            {
                                parsedResponse = JsonConvert.DeserializeObject<GBridgeResponse>(responseBody, JsonSettings);
                            }
                            catch (JsonException)
                            {
                                // The actionable invalid-JSON error is produced below for successful HTTP replies.
                            }
                        }

                        if (body.InputType == "1" && IsNspCredentialsRejection(body.TransUid, responseBody, httpStatus))
                            throw new GBridgeException(ProviderPaymentCoordinator.FailureMessage(GBridgeFailureKind.CredentialsRejected),
                                GBridgeFailureKind.CredentialsRejected);

                        if (body.InputType == "1" && (IsProviderSignatureRejection(body.TransUid, responseBody, httpStatus) ||
                            IsIlydaSignatureEncodingRejection(body.TransUid, JsonConvert.SerializeObject(body, JsonSettings), responseBody, httpStatus)))
                            throw new GBridgeException(
                                "The POS rejected the provider signature. Correct the POS signature settings and click Resend to request a new signature and payment attempt.",
                                GBridgeFailureKind.SignatureRejected);

                        if (!response.IsSuccessStatusCode)
                        {
                            throw new GBridgeException(
                                "GBridge request failed (HTTP " + (int)response.StatusCode + "): " +
                                FirstNonEmpty(
                                    parsedResponse == null ? null : parsedResponse.ReplyDesc,
                                    responseBody,
                                    response.ReasonPhrase,
                                    "No error details were returned."));
                        }

                        if (parsedResponse == null)
                        {
                            throw new GBridgeException("GBridge returned an empty or invalid JSON response.");
                        }

                        return parsedResponse;
                    }
                }
            }
            catch (Exception ex)
            {
                failure = ex;
                throw;
            }
            finally
            {
                RecordEvent(body, startedUtc, httpStatus, json, responseBody, parsedResponse, failure);
            }
        }

        private void RecordEvent(
            GBridgeRequest request,
            DateTimeOffset startedUtc,
            int? httpStatus,
            string requestJson,
            string responseJson,
            GBridgeResponse response,
            Exception failure)
        {
            if (options.EventSink == null)
            {
                return;
            }

            try
            {
                options.EventSink.Record(new PaymentGatewayEvent
                {
                    TransactionUid = request.TransUid,
                    InputType = request.InputType,
                    StartedUtc = startedUtc,
                    CompletedUtc = DateTimeOffset.UtcNow,
                    HttpStatus = httpStatus,
                    RequestJson = requestJson,
                    ResponseJson = responseJson,
                    Status = response == null ? null : response.Status,
                    ReplyCode = response == null ? null : response.ReplyCode,
                    ReplyDescription = response == null ? null : response.ReplyDesc,
                    EftposReplyCode = response == null ? null : response.EftposReplyCode,
                    EftposReplyMessage = response == null ? null : response.EftposReplyMessage,
                    ErrorCode = failure is GBridgeException
                        ? ((GBridgeException)failure).FailureKind.ToString()
                        : failure == null ? null : failure.GetType().Name,
                    ErrorMessage = failure == null ? null : failure.Message
                });
            }
            catch
            {
                // Payment processing must not fail because the diagnostic journal is unavailable.
            }
        }

        private void ValidateCommon(string transactionUid)
        {
            if (string.IsNullOrWhiteSpace(options.TerminalId))
            {
                throw new InvalidOperationException("The GBridge POS Terminal ID is not configured.");
            }
            if (string.IsNullOrWhiteSpace(transactionUid))
            {
                throw new ArgumentException("A stable GBridge transaction UID is required.", nameof(transactionUid));
            }
        }

        public static bool TryNormalizeEndpoint(string value, out string endpoint, out string error)
        {
            string candidate = string.IsNullOrWhiteSpace(value)
                ? GBridgeOptions.DefaultEndpoint
                : value.Trim();
            Uri uri;
            if (!Uri.TryCreate(candidate, UriKind.Absolute, out uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
                string.IsNullOrWhiteSpace(uri.Host))
            {
                endpoint = null;
                error = "The GBridge endpoint must be an absolute HTTP or HTTPS URL, including its port when required (for example http://127.0.0.1:7902/).";
                return false;
            }

            if (!string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment) || !string.IsNullOrEmpty(uri.UserInfo))
            {
                endpoint = null;
                error = "The GBridge endpoint cannot contain a query string or fragment.";
                return false;
            }

            endpoint = uri.AbsoluteUri.EndsWith("/", StringComparison.Ordinal)
                ? uri.AbsoluteUri
                : uri.AbsoluteUri + "/";
            error = null;
            return true;
        }

        private static GBridgeEftReplyData RequireApprovedTransaction(
            GBridgeResponse response,
            bool requireVoided,
            string expectedTransactionUid,
            string expectedTerminalId,
            int? expectedAmountMinorUnits,
            int? expectedTransactionType,
            int? expectedPaymentType)
        {
            if (!HasSuccessfulEnvelope(response) || response.EftReplyData == null)
            {
                throw new GBridgeException(DescribeFailure(response, requireVoided), GBridgeFailureKind.Unknown);
            }

            List<GBridgeEftReplyData> correlated = response.EftReplyData.Where(value =>
                value != null &&
                value.TransactionData != null &&
                value.TransactionData.Approved &&
                (MatchesTransactionUid(value.TransactionData, expectedTransactionUid) ||
                 (!requireVoided && MatchesDatabaseShortReference(response, value.TransactionData, expectedTransactionUid)))).ToList();

            if (!requireVoided && correlated.Any(value =>
                value.TransactionData.Voided &&
                MatchesExpectedTransaction(
                    value.TransactionData,
                    expectedTerminalId,
                    expectedAmountMinorUnits,
                    expectedTransactionType,
                    expectedPaymentType)))
            {
                throw new GBridgeException(
                    "The recovered GBridge payment has already been voided.",
                    GBridgeFailureKind.Failed);
            }

            GBridgeEftReplyData result = correlated.FirstOrDefault(value =>
                value.TransactionData.Voided == requireVoided &&
                (requireVoided || !string.IsNullOrWhiteSpace(value.EftposTranId)) &&
                MatchesExpectedTransaction(
                    value.TransactionData,
                    expectedTerminalId,
                    expectedAmountMinorUnits,
                    expectedTransactionType,
                    expectedPaymentType));
            if (result == null)
            {
                result = correlated.FirstOrDefault(value =>
                    value.TransactionData.Voided == requireVoided &&
                    (requireVoided || !string.IsNullOrWhiteSpace(value.EftposTranId)));
            }
            if (result == null) result = correlated.FirstOrDefault(value => value.TransactionData.Voided == requireVoided);
            if (result == null)
            {
                var declined = !requireVoided ? response.EftReplyData.FirstOrDefault(value => value?.TransactionData != null &&
                    value.TransactionData.ApprovedWasReported && !value.TransactionData.Approved && !value.TransactionData.Voided &&
                    MatchesTransactionUid(value.TransactionData, expectedTransactionUid)) : null;
                if (declined != null)
                {
                    ValidateTransactionFields(response, declined, expectedTransactionUid, expectedTerminalId,
                        expectedAmountMinorUnits, expectedTransactionType, expectedPaymentType, false);
                    throw new GBridgeException(DescribeFailure(response, false), GBridgeFailureKind.Declined);
                }
                var unreported = response.EftReplyData.FirstOrDefault(value => value?.TransactionData != null && !value.TransactionData.ApprovedWasReported &&
                    MatchesTransactionUid(value.TransactionData, expectedTransactionUid));
                if (unreported != null)
                {
                    ValidateTransactionFields(response, unreported, expectedTransactionUid, expectedTerminalId,
                        expectedAmountMinorUnits, expectedTransactionType, expectedPaymentType, false);
                    throw new GBridgeException("Λείπουν πεδία: Approved.", GBridgeFailureKind.IncompleteResponse);
                }
                throw new GBridgeException(DescribeFailure(response, requireVoided), GBridgeFailureKind.Unknown);
            }

            ValidateTransactionFields(response, result, expectedTransactionUid, expectedTerminalId,
                expectedAmountMinorUnits, expectedTransactionType, expectedPaymentType, !requireVoided);
            return result;
        }

        private static void ValidateTransactionFields(GBridgeResponse response, GBridgeEftReplyData result,
            string expectedTransactionUid, string expectedTerminalId, int? expectedAmountMinorUnits,
            int? expectedTransactionType, int? expectedPaymentType, bool requireTransactionId)
        {
            var mismatches = new List<string>();
            var missing = new List<string>();
            if (requireTransactionId && string.IsNullOrWhiteSpace(result.EftposTranId)) missing.Add("EftposTranId");
            if (string.IsNullOrWhiteSpace(response.TransUid)) missing.Add("TransUid");
            else if (!string.Equals(response.TransUid, expectedTransactionUid, StringComparison.OrdinalIgnoreCase))
            {
                mismatches.Add("TransUid");
            }
            if (string.IsNullOrWhiteSpace(result.TransactionData.Tid)) missing.Add("TID");
            else if (!string.Equals(result.TransactionData.Tid, expectedTerminalId, StringComparison.OrdinalIgnoreCase))
            {
                mismatches.Add("TID");
            }
            if (expectedAmountMinorUnits.HasValue && !result.TransactionData.AmountWasReported) missing.Add("Amount");
            else if (expectedAmountMinorUnits.HasValue &&
                result.TransactionData.Amount != expectedAmountMinorUnits.Value)
            {
                mismatches.Add("amount");
            }
            if (expectedTransactionType.HasValue && !result.TransactionData.TransactionTypeWasReported) missing.Add("TxnType");
            else if (expectedTransactionType.HasValue && result.TransactionData.TxnType != expectedTransactionType.Value)
            {
                mismatches.Add("transaction type");
            }
            if (expectedPaymentType.HasValue && !result.TransactionData.PaymentTypeWasReported) missing.Add("PaymentType");
            else if (expectedPaymentType.HasValue && result.TransactionData.PaymentType != expectedPaymentType.Value)
            {
                mismatches.Add("payment type");
            }

            if (mismatches.Count > 0)
            {
                throw new GBridgeException(
                    "GBridge returned an approved transaction that does not match the request: " +
                    string.Join(", ", mismatches) + ".",
                    GBridgeFailureKind.Mismatch);
            }

            if (missing.Count > 0)
                throw new GBridgeException("Λείπουν πεδία: " + string.Join(", ", missing) + ".", GBridgeFailureKind.IncompleteResponse);
        }

        private static bool HasResolvedTransaction(GBridgeResponse response, string uid, string tid, int amount, int operation, int paymentType)
        {
            try { RequireApprovedTransaction(response, false, uid, tid, amount, operation, paymentType); return true; }
            catch (GBridgeException ex) { return ex.FailureKind == GBridgeFailureKind.Declined || ex.FailureKind == GBridgeFailureKind.Mismatch; }
        }

        private static bool HasIncompleteTransaction(GBridgeResponse response, string uid, string tid, int amount, int operation, int paymentType)
        {
            try { RequireApprovedTransaction(response, false, uid, tid, amount, operation, paymentType); return false; }
            catch (GBridgeException ex) { return ex.FailureKind == GBridgeFailureKind.IncompleteResponse; }
        }

        private static bool HasSuccessfulEnvelope(GBridgeResponse response)
        {
            if (response == null ||
                !string.Equals(response.Status, "200", StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(response.ReplyCode, "00", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            bool isDatabaseQuery = string.Equals(response.InputType, "4", StringComparison.OrdinalIgnoreCase);
            return string.Equals(response.EftposReplyCode, "200", StringComparison.OrdinalIgnoreCase) ||
                (isDatabaseQuery && string.IsNullOrWhiteSpace(response.EftposReplyCode));
        }

        private static bool MatchesDatabaseShortReference(GBridgeResponse response, GBridgeTransactionData data, string uid)
        {
            Guid parsed;
            return response.InputType == "4" && response.EftReplyData.Count == 1 &&
                string.Equals(response.TransUid, uid, StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrEmpty(uid) && uid.Length >= 32 && Guid.TryParseExact(uid.Substring(0, 32), "N", out parsed) &&
                data.CustomerReference != null && data.CustomerReference.Length == 8 && string.IsNullOrWhiteSpace(data.OriginalCustomerReference) &&
                uid.StartsWith(data.CustomerReference, StringComparison.OrdinalIgnoreCase);
        }

        private static bool MatchesTransactionUid(
            GBridgeTransactionData transaction,
            string expectedTransactionUid)
        {
            bool hasReference = !string.IsNullOrWhiteSpace(transaction.CustomerReference) ||
                !string.IsNullOrWhiteSpace(transaction.OriginalCustomerReference);
            return !hasReference ||
                string.Equals(
                    transaction.CustomerReference,
                    expectedTransactionUid,
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    transaction.OriginalCustomerReference,
                    expectedTransactionUid,
                    StringComparison.OrdinalIgnoreCase);
        }

        private static bool MatchesExpectedTransaction(
            GBridgeTransactionData transaction,
            string expectedTerminalId,
            int? expectedAmountMinorUnits,
            int? expectedTransactionType,
            int? expectedPaymentType)
        {
            return string.Equals(transaction.Tid, expectedTerminalId, StringComparison.OrdinalIgnoreCase) &&
                (!expectedAmountMinorUnits.HasValue || transaction.Amount == expectedAmountMinorUnits.Value) &&
                (!expectedTransactionType.HasValue || transaction.TransactionTypeWasReported && transaction.TxnType == expectedTransactionType.Value) &&
                (!expectedPaymentType.HasValue || transaction.PaymentTypeWasReported && transaction.PaymentType == expectedPaymentType.Value);
        }

        private static string DescribeFailure(GBridgeResponse response, bool requireVoided)
        {
            if (response == null)
            {
                return "GBridge returned no response.";
            }

            string operation = requireVoided ? "void" : "payment";
            return "GBridge " + operation + " failed. Status=" + FirstNonEmpty(response.Status, "(empty)") +
                ", ReplyCode=" + FirstNonEmpty(response.ReplyCode, "(empty)") +
                ", EftposReplyCode=" + FirstNonEmpty(response.EftposReplyCode, "(empty)") +
                ", Message=" + FirstNonEmpty(response.EftposReplyMessage, response.ReplyDesc, "No details returned.");
        }

        private static string FirstNonEmpty(params string[] values)
        {
            return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
        }

        private sealed class GBridgeRequest
        {
            [JsonProperty("InputType")]
            public string InputType { get; set; }

            [JsonProperty("Tid")]
            public string Tid { get; set; }

            [JsonProperty("TransUid")]
            public string TransUid { get; set; }

            [JsonProperty("TransactionData")]
            public GBridgeTransactionRequest TransactionData { get; set; }

            [JsonProperty("CommandParams")]
            public string CommandParams { get; set; }
        }

        private sealed class GBridgeTransactionRequest
        {
            public int TxnType { get; set; }
            public bool IsTaxFree { get; set; }
            public int PaymentType { get; set; }
            public int Tip { get; set; }
            public int CashBack { get; set; }
            public int Instalments { get; set; }
            public int ProviderId { get; set; }
            public string ProviderInput { get; set; }
            public string ProviderSignature { get; set; }
        }

        private sealed class GBridgeResponse
        {
            public string InputType { get; set; }
            public string TransUid { get; set; }
            public string Status { get; set; }
            public string ReplyCode { get; set; }
            public string ReplyDesc { get; set; }
            public string EftposReplyCode { get; set; }
            public string EftposReplyMessage { get; set; }

            [JsonProperty("EFTReplyData")]
            [JsonConverter(typeof(SingleOrArrayConverter<GBridgeEftReplyData>))]
            public List<GBridgeEftReplyData> EftReplyData { get; set; }
        }

        private sealed class GBridgeEftReplyData
        {
            public string EftposTranId { get; set; }
            public GBridgeTransactionData TransactionData { get; set; }
        }

        private sealed class GBridgeTransactionData
        {
            [JsonProperty("TID")]
            public string Tid { get; set; }

            private bool approved;
            [JsonIgnore]
            public bool ApprovedWasReported { get; private set; }
            public bool Approved { get { return approved; } set { approved = value; ApprovedWasReported = true; } }
            public bool Voided { get; set; }
            private int amount;
            [JsonIgnore] public bool AmountWasReported { get; private set; }
            public int Amount { get { return amount; } set { amount = value; AmountWasReported = true; } }
            private int transactionType, paymentType;
            [JsonIgnore] public bool TransactionTypeWasReported { get; private set; }
            [JsonIgnore] public bool PaymentTypeWasReported { get; private set; }
            public int TxnType { get { return transactionType; } set { transactionType = value; TransactionTypeWasReported = true; } }
            public int PaymentType { get { return paymentType; } set { paymentType = value; PaymentTypeWasReported = true; } }
            public string CustomerReference { get; set; }
            public string OriginalCustomerReference { get; set; }
            [JsonProperty("STAN")]
            public int? Stan { get; set; }
            public int? BatchNumber { get; set; }
            public string Acquirer { get; set; }

            [JsonProperty("MID")]
            public string Mid { get; set; }

            [JsonProperty("CardPAN")]
            public string CardPan { get; set; }

            public string CardType { get; set; }
            public DateTimeOffset? Timestamp { get; set; }
            public DateTimeOffset? VoidTimestamp { get; set; }
            public string MerchantReceipt { get; set; }
            public string CustomerReceipt { get; set; }

            [JsonProperty("RRN")]
            public string Rrn { get; set; }

            public string AuthCode { get; set; }
        }

        private sealed class SingleOrArrayConverter<T> : JsonConverter
        {
            public override bool CanWrite { get { return false; } }
            public override bool CanConvert(Type objectType) { return objectType == typeof(List<T>); }

            public override object ReadJson(
                JsonReader reader,
                Type objectType,
                object existingValue,
                JsonSerializer serializer)
            {
                JToken token = JToken.Load(reader);
                if (token.Type == JTokenType.Null)
                {
                    return new List<T>();
                }
                if (token.Type == JTokenType.Array)
                {
                    return token.ToObject<List<T>>(serializer) ?? new List<T>();
                }
                if (token.Type == JTokenType.Object)
                {
                    return new List<T> { token.ToObject<T>(serializer) };
                }

                throw new JsonSerializationException("Expected a JSON object or array.");
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                throw new NotSupportedException();
            }
        }
    }

}
