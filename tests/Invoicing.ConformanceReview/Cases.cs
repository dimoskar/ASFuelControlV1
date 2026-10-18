using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ASFuelControl.Data;
using ASFuelControl.Invoicing.Mapping;
using Exedron.Invoicing.Adapters;
using Exedron.Invoicing.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Offline review probes. Reuses the synthetic host fixtures; never runs their Main
// or application startup, and has no database or network collaborator.
internal static class ConformanceReview
{
    private static readonly BindingFlags Flags = BindingFlags.Static | BindingFlags.NonPublic;
    private static MappingContext Context() => (MappingContext)typeof(Program).GetMethod("Context",Flags).Invoke(null,null);
    private static Invoice Invoice(Fixture fixture,MappingContext context) => (Invoice)typeof(Program).GetMethod("Invoice",Flags).Invoke(null,new object[] {fixture,context});
    private sealed class MemoryStore : IITimologiaSubmissionStore
    {
        public ITimologiaSubmissionAttempt Latest;
        public IDisposable Acquire(Guid id) => new Lease();
        public ITimologiaSubmissionAttempt LoadLatest(Guid id,bool test,string vat) => Latest;
        public void Begin(ITimologiaSubmissionAttempt attempt) { Latest = attempt; }
        public void Complete(ITimologiaSubmissionAttempt attempt) { Latest = attempt; }
        private sealed class Lease : IDisposable { public void Dispose() {} }
    }
    private sealed class OfflineLogger : IRequestResponseLogger
    {
        public void RequestStarted(InvoiceRequestLog request) {}
        public void ResponseReceived(InvoiceResponseLog response) {}
    }
    private sealed class FakeHttp : HttpMessageHandler
    {
        public int Status, Posts, Gets;
        public Guid Id;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellation)
        {
            string body; int status;
            if (request.RequestUri.AbsolutePath.Contains("tokens/"))
            { body = "{\"token\":\"synthetic-token\",\"expires_at\":\"2060-01-01T00:00:00Z\"}"; status = 200; }
            else if (request.Method == HttpMethod.Get)
            { Gets++; body = "{}"; status = 404; }
            else
            {
                Posts++; status = Status;
                body = Status == 200 ? new JObject { ["data"] = new JObject {
                    ["ext_system_id"] = Id.ToString("N"),["uid"] = "UID",["mark"] = "900010" } }.ToString()
                    : "{\"error\":\"Synthetic definitive input rejection\"}";
            }
            return Task.FromResult(new HttpResponseMessage((HttpStatusCode)status) { Content = new StringContent(body) });
        }
    }
    private static void ResponseCases()
    {
        var results = new JArray();
        foreach (var status in new[] { 422,452,200 })
        {
            var context = Context(); var invoice = Invoice(new Fixture(),context);
            var model = new HostInvoiceMapper().Build(invoice,context);
            var store = new MemoryStore(); var http = new FakeHttp { Status = status,Id = invoice.InvoiceId };
            using (var provider = new ITimologiaInvoiceProvider(new ITimologiaProviderOptions {
                IssuerVat = model.Issuer.VATNumber,MasterToken = "synthetic-master",SubscriptionId = "1",TestMode = true,SubmissionStore = store,Logger = new OfflineLogger() },http))
            {
                var first = provider.SendInvoice(model,invoice.InvoiceId,null);
                string state = store.Latest?.State;
                if (state != (status == 200 ? "Accepted" : "Rejected")) throw new Exception("Incorrect HTTP outcome for " + status);
                if (status != 200) provider.SendInvoice(model,invoice.InvoiceId,null);
                results.Add(new JObject { ["provider"] = "iTimologia",["http_status"] = status,["state"] = state,
                    ["accepted"] = !first.HasErrors,["qr"] = first.QrCodeUrl,["signature"] = first.InvoiceSignature,
                    ["invoice_posts_after_repeat"] = http.Posts,["lookup_gets"] = http.Gets });
            }
        }
        var ilyda = IlydaInvoiceProvider.MapResponse(JObject.Parse("{\"invoiceMarking\":{\"mark\":\"900010\",\"invoiceIdentifier\":\"UID\",\"verificationHash\":\"HASH\"},\"errors\":[]}"));
        results.Add(new JObject { ["provider"] = "Ilyda",["accepted_without_qr_or_provider_url"] = !ilyda.HasErrors });
        File.WriteAllText("response-results.json",results.ToString());
        Console.WriteLine(results.ToString());
    }
    private static void Case(string name,Fixture fixture,Action<Invoice,MappingContext> configure = null)
    {
        var context = Context(); var invoice = Invoice(fixture,context); configure?.Invoke(invoice,context);
        try
        {
            var model = new HostInvoiceMapper().Build(invoice,context);
            File.WriteAllText(name+".model.json",JsonConvert.SerializeObject(model,Formatting.Indented));
            foreach(var provider in new[] { InvoiceProviderCatalog.MyDataKey,InvoiceProviderCatalog.ITimologiaKey,InvoiceProviderCatalog.DirectIlydaKey })
            {
                try
                {
                    var payload = new InvoiceAdapterFactory(new InvoiceTypeResolver()).CreatePayloadMapper(provider).Serialize(model,invoice.InvoiceId);
                    File.WriteAllText(name+"."+provider+(provider == InvoiceProviderCatalog.MyDataKey ? ".xml" : ".json"),payload);
                    Console.WriteLine("GENERATED "+name+" "+provider);
                }
                catch(Exception ex) { Console.WriteLine("BLOCKED "+name+" "+provider+": "+ex.Message); }
            }
        }
        catch(Exception ex) { Console.WriteLine("BLOCKED "+name+" host: "+ex.Message); }
    }
    static int Main()
    {
        Case("domestic-fuel",new Fixture());
        Case("retail-fuel",new Fixture { OfficialCode = 200 });
        Case("eu-zero-vat",new Fixture { Country = "DE",Gross = 100,Vat = 0,Rate = 0 });
        Case("non-eu-zero-vat",new Fixture { Country = "US",Gross = 100,Vat = 0,Rate = 0 });
        Case("domestic-exempt",new Fixture { Gross = 100,Vat = 0,Rate = 0,VatExemption = true });
        Case("services",new Fixture(),(i,c) => c.Items[i.InvoiceLines[0].FuelTypeId] = new ItemMapping { SalesItemType = 2,MeasurementUnit = 0 });
        Case("credit-correlated",new Fixture { Credit = true,ParentMark = 900001 });
        Case("delivery",new Fixture { OfficialCode = 159 },(i,c) => c.Delivery = (DeliveryContext)typeof(Program).GetMethod("Delivery",Flags).Invoke(null,null));
        Case("mixed-fuel-merchandise",new Fixture { Gross = 248,Vat = 48,TwoLines = true },(i,c) => {
            var id = Guid.NewGuid(); i.InvoiceLines[1].FuelType = new FuelType { FuelTypeId = id,Name = "Merchandise",Code = "AB" };
            c.Items[id] = new ItemMapping { SalesItemType = 0,MeasurementUnit = 1 };
        });
        Case("invalid-fuel-code",new Fixture(),(i,c) => i.InvoiceLines[0].FuelType.EnumeratorValue = 98);
        foreach (int code in new[] { 99,101,9999 })
            Case("fuel-normalized-" + code,new Fixture { Gross = 248,Vat = 48,TwoLines = true },(i,c) => i.InvoiceLines[1].FuelType.EnumeratorValue = code);
        Case("fuel-only-999",new Fixture(),(i,c) => i.InvoiceLines[0].FuelType.EnumeratorValue = 999);
        Case("fuel-two-999-lines",new Fixture { Gross = 372,Vat = 72,TwoLines = true },(i,c) => {
            var extra = Invoice(new Fixture(),c).InvoiceLines[0];
            extra.InvoiceId = i.InvoiceId; extra.InvoiceLineId = Guid.NewGuid();
            i.InvoiceLines[0].FuelType.EnumeratorValue = 999;
            i.InvoiceLines[1].FuelType.EnumeratorValue = 999;
            i.InvoiceLines[0].TotalPrice = i.InvoiceLines[1].TotalPrice = 124;
            i.InvoiceLines[0].VatAmount = i.InvoiceLines[1].VatAmount = 24;
            i.InvoiceLines.Add(extra);
        });
        Case("fuel-999-exceeds-real-fuel",new Fixture { Gross = 372,Vat = 72,TwoLines = true },(i,c) => {
            i.InvoiceLines[0].FuelType.EnumeratorValue = 999;
            i.InvoiceLines[0].TotalPrice = 248; i.InvoiceLines[0].VatAmount = 48;
            i.InvoiceLines[1].TotalPrice = 124; i.InvoiceLines[1].VatAmount = 24;
        });
        Case("wrong-vat-arithmetic",new Fixture { Gross = 124,Vat = 23,Rate = 24 });
        Case("excessive-series",new Fixture(),(i,c) => i.Series = new string('S',51));
        Case("provider-fuel-normalized",new Fixture(),(i,c) => i.InvoiceLines[0].VolumeNormalized = 48.75m);
        ResponseCases();
        Console.WriteLine("OBSERVATIONS ONLY: generated payloads require XSD and business-rule checks; generation is not acceptance.");
        return 0;
    }
}
