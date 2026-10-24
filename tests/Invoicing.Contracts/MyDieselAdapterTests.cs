using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Exedron.Invoicing.Adapters;
using Exedron.Invoicing.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static partial class Program
{
    private static void MyDieselAdapterTests()
    {
        Test("myDiesel QR parsing is strict and does not retain raw JSON", () => {
            var qr = MyDieselQrParser.Parse("{\"code\":\"αυνυ 1022 8625 7956 7356\",\"afm\":\"123456789\",\"validUntil\":1792666800,\"issuedLitres\":150.25}");
            Equal("AYNY-1022-8625-7956-7356", qr.Identity.CouponCode);
            Equal("123456789", qr.Identity.BeneficiaryVat);
            Equal(150.25m, qr.IssuedLitres);
            Equal(new DateTime(2026, 10, 22, 11, 0, 0, DateTimeKind.Utc), qr.ValidUntilUtc.UtcDateTime);
            Throws<ArgumentException>(() => MyDieselQrParser.Parse("{\"code\":\"AYNY-1022-8625-7956-7356\",\"code\":\"AYNY-1022-8625-7956-7356\",\"afm\":\"123456789\",\"validUntil\":1792666800,\"issuedLitres\":150}"));
            Throws<ArgumentException>(() => MyDieselQrParser.Parse("{\"code\":\"AYNY-1022-8625-7956-7356\",\"afm\":\"123456789\",\"validUntil\":1792666800,\"issuedLitres\":150,\"raw\":\"unexpected\"}"));
            Throws<ArgumentOutOfRangeException>(() => MyDieselQrParser.Parse("{\"code\":\"AYNY-1022-8625-7956-7356\",\"afm\":\"123456789\",\"validUntil\":1792666800,\"issuedLitres\":150.123}"));
        });
        Test("myDiesel UAT sends private API key and company VAT with exact coupon JSON", () => {
            var logger = new MyDieselTestLogger();
            var handler = new MyDieselTestHandler(request => {
                Equal("https://acc-mydiesel.aade.gr/prv/secured/erp/coupon/query", request.RequestUri.AbsoluteUri);
                Equal(HttpMethod.Post, request.Method);
                Equal("uat-secret", request.Headers.GetValues("X-myDiesel-PRV-API-Key").Single());
                Equal("998877665", request.Headers.GetValues("company-vat").Single());
                Check(!request.Headers.Contains("aade-user-id"), "UAT leaked production user header.");
                var body = JObject.Parse(request.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                Equal("AYNY-1022-8625-7956-7356", (string)body["couponCode"]);
                Equal("998877665", (string)body["companyVat"]);
                Equal(1, (int)body["branchNum"]);
                return Json(HttpStatusCode.OK, CouponEnvelope("RESERVED", true, omitOptional: true));
            });
            using (var client = UatClient(logger, handler))
            {
                var result = client.Query(new MyDieselLookupRequest("ayny1022862579567356", "998877665", 1));
                Equal(MyDieselOperationState.Succeeded, result.State);
                Equal(MyDieselCouponStatus.Reserved, result.Data.KnownStatus);
                Check(result.Data.FuelCode == null && result.Data.LowBalanceMessage == null, "Omitted optional values were not accepted.");
            }
            Equal(1, logger.Requests.Count);
            Equal(1, logger.Responses.Count);
            string evidence = JsonConvert.SerializeObject(logger.Requests);
            Check(!evidence.Contains("uat-secret"), "UAT API key leaked into request evidence.");
            Check(!logger.Requests[0].Endpoint.Contains("?"), "Arbitrary query parameters were logged.");
        });
        Test("myDiesel production uses myDATA credentials and never sends company-vat", () => {
            var logger = new MyDieselTestLogger();
            var handler = new MyDieselTestHandler(request => {
                Equal("https://mydatapi.aade.gr/myDiesel/secured/erp/company?vat=998877665", request.RequestUri.AbsoluteUri);
                Equal("prod-user", request.Headers.GetValues("aade-user-id").Single());
                Equal("prod-secret", request.Headers.GetValues("ocp-apim-subscription-key").Single());
                Check(!request.Headers.Contains("company-vat"), "Production sent gateway-owned company-vat.");
                Check(!request.Headers.Contains("X-myDiesel-PRV-API-Key"), "Production sent private UAT header.");
                return Json(HttpStatusCode.OK, "{\"success\":true,\"data\":{\"vatNumber\":\"998877665\",\"companyName\":\"TEST FUEL\",\"active\":true,\"gasStations\":[{\"name\":\"Station\",\"branchNum\":1,\"amdika\":\"A1\"}]}}");
            });
            var options = new MyDieselClientOptions { Scope = new MyDieselScope(FiscalEnvironment.Production, "998877665", 1, "prd"),
                AadeUserId = "prod-user", ProductionSubscriptionKey = "prod-secret", Logger = logger };
            using (var client = new MyDieselClient(options, handler))
            {
                var result = client.GetCompany("998877665");
                Equal(MyDieselOperationState.Succeeded, result.State);
                Equal(1, result.Data.GasStations.Single().BranchNum);
            }
            string evidence = JsonConvert.SerializeObject(logger.Requests);
            Check(!evidence.Contains("prod-secret") && !evidence.Contains("prod-user"), "Production credential leaked into evidence.");
        });
        Test("myDiesel implements fuel, redeem, release and branch endpoints", () => {
            var calls = new Queue<string>(new[] { "fuel-types", "coupon/redeem", "coupon/release", "coupon/branch" });
            var handler = new MyDieselTestHandler(request => {
                string expected = calls.Dequeue();
                Check(request.RequestUri.AbsolutePath.EndsWith(expected, StringComparison.Ordinal), "Wrong endpoint: " + request.RequestUri);
                if (expected == "fuel-types") return Json(HttpStatusCode.OK, "{\"success\":true,\"data\":[{\"key\":\"DIESEL\",\"code\":20},{\"key\":\"DIESEL_PR\",\"code\":21}]}");
                if (expected == "coupon/redeem")
                {
                    var body = JObject.Parse(request.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                    Equal("INVOICE", (string)body["invoiceType"]); Equal("400001234567890", (string)body["mark"]);
                    return Json(HttpStatusCode.OK, CouponEnvelope("REDEEMED", false, explicitNulls: true));
                }
                return Json(HttpStatusCode.OK, CouponEnvelope(expected.EndsWith("release") ? "ACTIVE" : "REDEEMED", expected.EndsWith("release")));
            });
            using (var client = UatClient(new MyDieselTestLogger(), handler))
            {
                Equal(2, client.GetFuelTypes().Data.Length);
                var lookup = new MyDieselLookupRequest("AYNY-1022-8625-7956-7356", "998877665", 1);
                Equal(MyDieselCouponStatus.Redeemed, client.Redeem(new MyDieselRedeemRequest(lookup.CouponCode, lookup.CompanyVat, 1, "400001234567890")).Data.KnownStatus);
                Equal(MyDieselCouponStatus.Active, client.Release(lookup).Data.KnownStatus);
                Equal(MyDieselCouponStatus.Redeemed, client.ChangeBranch(lookup).Data.KnownStatus);
            }
            Equal(0, calls.Count);
        });
        Test("myDiesel separates definitive errors from uncertain outcomes", () => {
            var replies = new Queue<Func<HttpRequestMessage, HttpResponseMessage>>(new Func<HttpRequestMessage, HttpResponseMessage>[] {
                _ => Json(HttpStatusCode.Conflict,"{\"code\":\"COUPON_RESERVED_BY_OTHER_BRANCH\",\"path\":\"/secured/erp/coupon/query\",\"timestamp\":\"2026-10-22T08:40:12.330Z\",\"error\":\"reserved\"}"),
                _ => Json(HttpStatusCode.GatewayTimeout,"{\"success\":false,\"data\":{\"status\":504,\"path\":\"/secured/erp/coupon/redeem\",\"timestamp\":\"2026-10-22T07:25:41.902Z\"},\"error\":\"timeout\"}"),
                _ => Json(HttpStatusCode.OK,"{not-json}"),
                _ => throw new HttpRequestException("network secret must not persist")
            });
            var handler = new MyDieselTestHandler(request => replies.Dequeue()(request));
            using (var client = UatClient(new MyDieselTestLogger(), handler))
            {
                var lookup = new MyDieselLookupRequest("AYNY-1022-8625-7956-7356", "998877665", 1);
                var conflict = client.Query(lookup);
                Equal(MyDieselOperationState.Rejected, conflict.State); Equal("COUPON_RESERVED_BY_OTHER_BRANCH", conflict.Error.Code);
                var timeout = client.Redeem(new MyDieselRedeemRequest(lookup.CouponCode, lookup.CompanyVat, 1, "400001234567890"));
                Equal(MyDieselOperationState.Unknown, timeout.State); Equal(504, timeout.HttpStatus.Value);
                var malformed = client.Query(lookup);
                Equal(MyDieselOperationState.Unknown, malformed.State); Equal("MALFORMED_SUCCESS_RESPONSE", malformed.Error.Code);
                var transport = client.Query(lookup);
                Equal(MyDieselOperationState.Unknown, transport.State); Equal(typeof(HttpRequestException).FullName, transport.CommunicationError);
                Check(transport.ResponseBody == null, "Transport failure invented a response.");
            }
        });
        Test("myDiesel rejects a request whose company differs from its frozen scope", () => {
            var handler = new MyDieselTestHandler(_ => throw new Exception("Transport must not run."));
            using (var client = UatClient(new MyDieselTestLogger(), handler))
                Throws<ArgumentException>(() => client.Query(new MyDieselLookupRequest("AYNY-1022-8625-7956-7356", "997766554", 1)));
            Equal(0, handler.Calls);
        });
        Test("myDiesel correlates evidence and classifies logger failure before dispatch", () => {
            var caseId = Guid.NewGuid(); var invoiceId = Guid.NewGuid();
            var logger = new MyDieselTestLogger();
            var handler = new MyDieselTestHandler(_ => Json(HttpStatusCode.OK, CouponEnvelope("RESERVED", true)));
            var options = new MyDieselClientOptions { Scope = new MyDieselScope(FiscalEnvironment.Test, "998877665", 1, "uat"),
                UatApiKey = "uat-secret", Logger = logger, CouponCaseId = caseId, InvoiceId = invoiceId };
            using (var client = new MyDieselClient(options, handler))
                Equal(MyDieselOperationState.Succeeded, client.Query(new MyDieselLookupRequest("AYNY-1022-8625-7956-7356", "998877665", 1)).State);
            Equal(FiscalDocumentKind.MyDieselCoupon, logger.Requests.Single().DocumentKind.Value);
            Equal(caseId, logger.Requests.Single().DocumentId.Value); Equal(invoiceId, logger.Requests.Single().InvoiceId.Value);

            var failing = new MyDieselTestLogger { FailRequest = true };
            var blocked = new MyDieselTestHandler(_ => throw new Exception("Transport must not run."));
            using (var client = UatClient(failing, blocked))
            {
                var result = client.Query(new MyDieselLookupRequest("AYNY-1022-8625-7956-7356", "998877665", 1));
                Equal(MyDieselOperationState.FailedBeforeDispatch, result.State);
                Equal(typeof(InvalidOperationException).FullName, result.CommunicationError);
            }
            Equal(0, blocked.Calls);
        });
    }

    private static MyDieselClient UatClient(MyDieselTestLogger logger, HttpMessageHandler handler)
    {
        return new MyDieselClient(new MyDieselClientOptions { Scope = new MyDieselScope(FiscalEnvironment.Test, "998877665", 1, "uat"), UatApiKey = "uat-secret", Logger = logger }, handler);
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string json)
    {
        return new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }

    private static string CouponEnvelope(string status, bool redeemable, bool omitOptional = false, bool explicitNulls = false)
    {
        var data = new JObject {
            ["code"] = "AYNY-1022-8625-7956-7356", ["couponStatus"] = status, ["redeemable"] = redeemable,
            ["createdAt"] = 1792648800, ["validUntil"] = 1792666800, ["issuedLitres"] = 150.00m, ["lowBalance"] = false
        };
        if (!omitOptional)
        {
            data["fuelType"] = explicitNulls ? JValue.CreateNull() : (JToken)"DIESEL";
            data["fuelCode"] = explicitNulls ? JValue.CreateNull() : (JToken)20;
            data["lowBalanceMessage"] = explicitNulls ? JValue.CreateNull() : (JToken)new JObject { ["en"] = "warning", ["el"] = "προειδοποίηση" };
        }
        return new JObject { ["success"] = true, ["data"] = data }.ToString(Formatting.None);
    }

    private sealed class MyDieselTestHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> send;
        internal int Calls { get; private set; }
        internal MyDieselTestHandler(Func<HttpRequestMessage, HttpResponseMessage> send) { this.send = send; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Calls++;
            try { return Task.FromResult(send(request)); }
            catch (Exception ex) { return Task.FromException<HttpResponseMessage>(ex); }
        }
    }

    private sealed class MyDieselTestLogger : IRequestResponseLogger
    {
        internal readonly List<InvoiceRequestLog> Requests = new List<InvoiceRequestLog>();
        internal readonly List<InvoiceResponseLog> Responses = new List<InvoiceResponseLog>();
        internal bool FailRequest;
        public void RequestStarted(InvoiceRequestLog request) { if (FailRequest) throw new InvalidOperationException("fixture"); Requests.Add(request); }
        public void ResponseReceived(InvoiceResponseLog response) { Responses.Add(response); }
    }
}
