using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using Exedron.Invoicing.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Exedron.Invoicing.Adapters
{
    /// <summary>
    /// Public test fixtures distributed with the official myDiesel ERP examples v0.1.
    /// They are valid only against the private UAT endpoint and must never be used for production.
    /// </summary>
    public static class MyDieselUatCatalog
    {
        public const string ContractVersion = "0.1";
        public const string PrimaryErpApiKey = "082aea42ecea588c2b4171f9125cfe05d629b486c5cf1394";
        public const string SecondaryErpApiKey = "f9674de8b57bd23437350819898b6706de677675245f952d";

        private static readonly string[] companyVats = {
            "444444003", "444444015", "444444027",
            "444444039", "444444040", "444444052",
            "444444064", "444444076", "444444088",
            "444444090", "444444107", "444444119",
            "444444120", "444444132", "444444144",
            "444444156", "444444168", "444444170",
            "444444181", "444444193", "444444200",
            "444444212", "444444224", "444444236",
            "444444248", "444444250", "444444261",
            "444444273", "444444285", "444444297",
            "444444304", "444444316", "444444328",
            "444444330", "444444341", "444444353",
            "444444365", "444444377", "444444389",
            "444444390", "444444501", "444444513",
            "444444525", "444444537", "444444549"
        };

        private static readonly string[] beneficiaryVats = { "111111102", "111111114" };

        public static string[] CompanyVats => (string[])companyVats.Clone();
        public static string[] BeneficiaryVats => (string[])beneficiaryVats.Clone();
        public static bool IsCompanyVat(string value) => companyVats.Contains((value ?? "").Trim(), StringComparer.Ordinal);
    }

    public sealed class MyDieselClientOptions
    {
        public MyDieselScope Scope { get; set; }
        public string AadeUserId { get; set; }
        public string ProductionSubscriptionKey { get; set; }
        public string UatApiKey { get; set; }
        public IRequestResponseLogger Logger { get; set; }
        public TimeSpan Timeout { get; set; }
        public Guid? CouponCaseId { get; set; }
        public Guid? InvoiceId { get; set; }
    }

    public static class MyDieselQrParser
    {
        private static readonly string[] Expected = { "code", "afm", "validUntil", "issuedLitres" };

        public static MyDieselQrCoupon Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("myDiesel QR payload is required.", nameof(json));
            JObject value;
            try
            {
                using (var reader = new JsonTextReader(new StringReader(json)) { DateParseHandling = DateParseHandling.None })
                    value = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
            }
            catch (JsonException ex) { throw new ArgumentException("myDiesel QR payload must be one JSON object without duplicate properties.", nameof(json), ex); }
            var names = value.Properties().Select(p => p.Name).ToArray();
            if (names.Length != Expected.Length || names.Except(Expected, StringComparer.Ordinal).Any() || Expected.Except(names, StringComparer.Ordinal).Any())
                throw new ArgumentException("myDiesel QR payload must contain only code, afm, validUntil and issuedLitres.", nameof(json));
            if (value.Properties().Any(p => p.Value == null || p.Value.Type == JTokenType.Null))
                throw new ArgumentException("myDiesel QR payload fields cannot be null.", nameof(json));
            long validUntil;
            decimal issuedLitres;
            if (value["validUntil"].Type != JTokenType.Integer || !long.TryParse(value["validUntil"].ToString(), NumberStyles.None, CultureInfo.InvariantCulture, out validUntil))
                throw new ArgumentException("myDiesel QR validUntil must be Unix seconds.", nameof(json));
            if ((value["issuedLitres"].Type != JTokenType.Integer && value["issuedLitres"].Type != JTokenType.Float) ||
                !decimal.TryParse(value["issuedLitres"].ToString(Formatting.None), NumberStyles.Number, CultureInfo.InvariantCulture, out issuedLitres))
                throw new ArgumentException("myDiesel QR issuedLitres must be a JSON number.", nameof(json));
            return new MyDieselQrCoupon(
                new MyDieselCouponIdentity((string)value["code"], (string)value["afm"]), validUntil, issuedLitres);
        }
    }

    public sealed class MyDieselClient : IMyDieselClient
    {
        public const string UatBaseUrl = "https://acc-mydiesel.aade.gr/prv/";
        public const string ProductionBaseUrl = "https://mydatapi.aade.gr/myDiesel/";
        private readonly MyDieselClientOptions options;
        private readonly HttpClient client;
        private readonly string baseUrl;

        public MyDieselClient(MyDieselClientOptions options, HttpMessageHandler handler = null)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            if (options.Scope == null) throw new ArgumentException("A myDiesel scope is required.", nameof(options));
            if (options.Logger == null) throw new ArgumentException("A host-owned request/response logger is required.", nameof(options));
            if (options.Scope.Environment == FiscalEnvironment.Production)
            {
                Require(options.AadeUserId, "AADE user ID");
                Require(options.ProductionSubscriptionKey, "Production subscription key");
            }
            else Require(options.UatApiKey, "UAT API key");
            baseUrl = options.Scope.Environment == FiscalEnvironment.Production ? ProductionBaseUrl : UatBaseUrl;
            client = LoggedHttpClient.Create(options.Logger, "myDiesel", options.Scope.Environment.ToString(), handler);
            client.Timeout = options.Timeout > TimeSpan.Zero ? options.Timeout : TimeSpan.FromSeconds(40);
        }

        public MyDieselCallResult<MyDieselCompany> GetCompany(string companyVat)
        {
            string vat = MyDieselCouponIdentity.NormalizeVat(companyVat, "Company VAT");
            RequireCompany(vat);
            return Send(MyDieselOperation.CompanyLookup, HttpMethod.Get, "secured/erp/company?vat=" + Uri.EscapeDataString(vat), null, ValidateCompany);
        }

        public MyDieselCallResult<MyDieselFuelType[]> GetFuelTypes()
        {
            return Send(MyDieselOperation.FuelTypes, HttpMethod.Get, "secured/erp/fuel-types", null, ValidateFuelTypes);
        }

        public MyDieselCallResult<MyDieselCouponInfo> Query(MyDieselLookupRequest request)
        {
            return Coupon(MyDieselOperation.Query, HttpMethod.Post, "secured/erp/coupon/query", request);
        }

        public MyDieselCallResult<MyDieselCouponInfo> Redeem(MyDieselRedeemRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            RequireCompany(request.CompanyVat);
            var body = new JObject {
                ["couponCode"] = request.CouponCode, ["companyVat"] = request.CompanyVat, ["branchNum"] = request.BranchNum,
                ["invoiceType"] = request.InvoiceType, ["mark"] = request.Mark
            };
            return Send(MyDieselOperation.Redeem, HttpMethod.Put, "secured/erp/coupon/redeem", body, ValidateCoupon);
        }

        public MyDieselCallResult<MyDieselCouponInfo> Release(MyDieselLookupRequest request)
        {
            return Coupon(MyDieselOperation.Release, HttpMethod.Put, "secured/erp/coupon/release", request);
        }

        public MyDieselCallResult<MyDieselCouponInfo> ChangeBranch(MyDieselLookupRequest request)
        {
            return Coupon(MyDieselOperation.ChangeBranch, HttpMethod.Put, "secured/erp/coupon/branch", request);
        }

        private MyDieselCallResult<MyDieselCouponInfo> Coupon(MyDieselOperation operation, HttpMethod method, string path, MyDieselLookupRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            RequireCompany(request.CompanyVat);
            var body = new JObject { ["couponCode"] = request.CouponCode, ["companyVat"] = request.CompanyVat, ["branchNum"] = request.BranchNum };
            return Send(operation, method, path, body, ValidateCoupon);
        }

        private MyDieselCallResult<T> Send<T>(MyDieselOperation operation, HttpMethod method, string path, JObject body, Func<JToken, T> parse)
        {
            using (var request = new HttpRequestMessage(method, baseUrl + path))
            {
                ApplyHeaders(request);
                if (options.CouponCaseId.HasValue)
                {
                    request.Properties[LoggedHttpClient.DocumentKindKey] = FiscalDocumentKind.MyDieselCoupon;
                    request.Properties[LoggedHttpClient.DocumentIdKey] = options.CouponCaseId.Value;
                }
                if (options.InvoiceId.HasValue) request.Properties[LoggedHttpClient.InvoiceIdKey] = options.InvoiceId.Value;
                if (body != null) request.Content = new StringContent(body.ToString(Formatting.None), Encoding.UTF8, "application/json");
                try
                {
                    using (var response = client.SendAsync(request).GetAwaiter().GetResult())
                    {
                        string responseBody = response.Content == null ? "" : response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                        int status = (int)response.StatusCode;
                        if (status >= 200 && status < 300)
                        {
                            try
                            {
                                var envelope = JObject.Parse(responseBody);
                                if ((bool?)envelope["success"] != true || envelope["data"] == null || envelope["data"].Type == JTokenType.Null)
                                    throw new JsonException("Missing successful myDiesel envelope.");
                                return Result(operation, MyDieselOperationState.Succeeded, status, parse(envelope["data"]), null, responseBody, null);
                            }
                            catch (Exception ex) when (ex is JsonException || ex is InvalidOperationException || ex is FormatException || ex is ArgumentException)
                            {
                                return Result<T>(operation, MyDieselOperationState.Unknown, status, default(T),
                                    new MyDieselError { HttpStatus = status, Code = "MALFORMED_SUCCESS_RESPONSE", Diagnostic = ex.Message }, responseBody, null);
                            }
                        }
                        var error = ParseError(status, responseBody);
                        var state = status >= 500 ? MyDieselOperationState.Unknown : MyDieselOperationState.Rejected;
                        return Result<T>(operation, state, status, default(T), error, responseBody, null);
                    }
                }
                catch (RequestEvidenceException ex)
                {
                    return Result<T>(operation, MyDieselOperationState.FailedBeforeDispatch, null, default(T), null, null,
                        ex.InnerException == null ? ex.GetType().FullName : ex.InnerException.GetType().FullName);
                }
                catch (Exception ex)
                {
                    return Result<T>(operation, MyDieselOperationState.Unknown, null, default(T), null, null, ex.GetType().FullName);
                }
            }
        }

        private void ApplyHeaders(HttpRequestMessage request)
        {
            if (options.Scope.Environment == FiscalEnvironment.Production)
            {
                request.Headers.Add("aade-user-id", options.AadeUserId.Trim());
                request.Headers.Add("ocp-apim-subscription-key", options.ProductionSubscriptionKey.Trim());
            }
            else
            {
                request.Headers.Add("X-myDiesel-PRV-API-Key", options.UatApiKey.Trim());
                request.Headers.Add("company-vat", options.Scope.CompanyVat);
            }
        }

        private void RequireCompany(string companyVat)
        {
            if (!string.Equals(options.Scope.CompanyVat, companyVat, StringComparison.Ordinal))
                throw new ArgumentException("The request company VAT must match the frozen myDiesel scope.", nameof(companyVat));
        }

        private static MyDieselCompany ValidateCompany(JToken token)
        {
            var value = token.ToObject<MyDieselCompany>();
            if (value == null || string.IsNullOrWhiteSpace(value.CompanyName)) throw new JsonException("Invalid myDiesel company response.");
            value.VatNumber = MyDieselCouponIdentity.NormalizeVat(value.VatNumber, "Company VAT");
            value.GasStations = value.GasStations ?? new MyDieselGasStation[0];
            if (value.GasStations.Any(s => s == null || s.BranchNum < 0 || string.IsNullOrWhiteSpace(s.Name) || string.IsNullOrWhiteSpace(s.Amdika)))
                throw new JsonException("Invalid myDiesel gas station response.");
            return value;
        }

        private static MyDieselFuelType[] ValidateFuelTypes(JToken token)
        {
            var values = token.ToObject<MyDieselFuelType[]>() ?? throw new JsonException("Invalid myDiesel fuel-types response.");
            if (values.Length == 0 || values.Any(v => v == null || string.IsNullOrWhiteSpace(v.Key) || v.Code <= 0) || values.Select(v => v.Code).Distinct().Count() != values.Length)
                throw new JsonException("Invalid myDiesel fuel-types response.");
            return values;
        }

        private static MyDieselCouponInfo ValidateCoupon(JToken token)
        {
            var value = token.ToObject<MyDieselCouponInfo>();
            if (value == null || string.IsNullOrWhiteSpace(value.CouponStatus) || value.IssuedLitres <= 0)
                throw new JsonException("Invalid myDiesel coupon response.");
            value.Code = MyDieselCouponIdentity.NormalizeCouponCode(value.Code);
            var created = value.CreatedUtc;
            var valid = value.ValidUntilUtc;
            if (valid <= created) throw new JsonException("myDiesel coupon validity precedes creation.");
            if (value.LowBalance && (value.LowBalanceMessage == null || string.IsNullOrWhiteSpace(value.LowBalanceMessage.El)))
                throw new JsonException("myDiesel low-balance warning is missing.");
            return value;
        }

        private static MyDieselError ParseError(int status, string body)
        {
            try
            {
                var json = JObject.Parse(body);
                var timestamp = (DateTimeOffset?)json["timestamp"] ?? (DateTimeOffset?)json.SelectToken("data.timestamp");
                return new MyDieselError {
                    HttpStatus = status,
                    Code = (string)json["code"] ?? "HTTP_" + status.ToString(CultureInfo.InvariantCulture),
                    Path = (string)json["path"] ?? (string)json.SelectToken("data.path"),
                    Timestamp = timestamp,
                    Diagnostic = (string)json["error"]
                };
            }
            catch (JsonException)
            {
                return new MyDieselError { HttpStatus = status, Code = "HTTP_" + status.ToString(CultureInfo.InvariantCulture), Diagnostic = "Unrecognized myDiesel error response." };
            }
        }

        private static MyDieselCallResult<T> Result<T>(MyDieselOperation operation, MyDieselOperationState state, int? status,
            T data, MyDieselError error, string body, string communicationError)
        {
            return new MyDieselCallResult<T> { Operation = operation, State = state, HttpStatus = status, Data = data,
                Error = error, ResponseBody = body, CommunicationError = communicationError };
        }

        private static string Require(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException(name + " is required.");
            return value.Trim();
        }

        public void Dispose() { client.Dispose(); }
    }
}
