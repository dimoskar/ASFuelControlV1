using System;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using ASFuelControl.Data;
using ASFuelControl.Invoicing;
using ASFuelControl.Invoicing.Mapping;
using ASFuelControl.Invoicing.Persistence;
using Exedron.Invoicing.Core;
using Newtonsoft.Json.Linq;

internal static class Program
{
    static string connection;
    static SqlFiscalDatabase database;
    static int passed, failed;
    static int Main(string[] args)
    {
        if (args.Length != 1 || !Regex.IsMatch(args[0], "^ASFC_InvoicingStep5_[0-9a-f]{32}$")) throw new ArgumentException("Private test instance required.");
        string name = "ASFC_Step5_" + Guid.NewGuid().ToString("N");
        var cs = new SqlConnectionStringBuilder { DataSource = "(localdb)\\" + args[0], InitialCatalog = "master", IntegratedSecurity = true, Pooling = false };
        string master = cs.ConnectionString; cs.InitialCatalog = name; connection = cs.ConnectionString;
        bool created = false;
        try
        {
            Execute(master, "CREATE DATABASE [" + name + "];"); created = true;
            Execute(master, "ALTER DATABASE [" + name + "] SET COMPATIBILITY_LEVEL = 100;");
            database = new SqlFiscalDatabase(connection); database.InstallSchema();
            Tests(); Console.WriteLine("RESULT: " + passed + " passed, " + failed + " failed."); return failed == 0 ? 0 : 1;
        }
        finally
        {
            SqlConnection.ClearAllPools();
            if (created) Execute(master, "ALTER DATABASE [" + name + "] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [" + name + "];");
        }
    }
    static void Tests()
    {
        Test("Ilyda username and password authenticate without an API key", () => {
            var source = new Source(); var http = new Http(source.Id,InvoiceProviderCatalog.DirectIlydaKey);
            var credentials = new Credentials { Value = new ProviderCredentials { Password = "SECRET-password" } };
            var route = new ProviderRoute(InvoiceProviderCatalog.DirectIlydaKey,true,"000000000",0,"password-profile","synthetic-user");
            var result = Service(http,credentials).Send(source.Id,route,source);
            Check(result.Accepted,Errors(result)); Equal("Basic",http.AuthorizationScheme);
            Equal(Convert.ToBase64String(Encoding.UTF8.GetBytes("synthetic-user:SECRET-password")),http.AuthorizationParameter);
            Check(!http.HasApiKey,"Password authentication still sent an API key");
            Check(!Latest(source.Id).SnapshotJson.Contains("SECRET-password"),"Password leaked into the journal");
        });
        foreach (var key in new[] { InvoiceProviderCatalog.MyDataKey, InvoiceProviderCatalog.ITimologiaKey, InvoiceProviderCatalog.DirectIlydaKey })
        {
            Test(key + " persists exact payload before dispatch and recovers accepted history", () => {
                var s = new Source(); var h = new Http(s.Id, key); var secrets = new Credentials(); var service = Service(h,secrets);
                var result = service.Send(s.Id, Route(key), s); Check(result.Accepted, Errors(result)); Equal(1,h.Posts);
                // Both source and current route can now be unavailable/changed: saved route wins.
                var again = service.Send(s.Id, Route(InvoiceProviderCatalog.MyDataKey, false), null);
                Check(again.Accepted, Errors(again)); Equal(1,h.Posts); Equal(1,s.Loads); Equal(key,secrets.Last.ProviderKey); Equal(true,secrets.Last.TestMode);
                var e = service.LoadParentEvidence(s.Id); Equal(900010L,e.Transmission.Mark.Value); Equal(FiscalEnvironment.Test,e.Environment);
                Equal(1,History(s.Id).Length); Equal(2,Revisions(s.Id).Length);
                var json = Latest(s.Id).SnapshotJson; Check(json.Contains("credential-v1"), "Missing historical route"); Check(!json.Contains("SECRET"), "Secret leaked to fiscal journal");
            });
        }
        foreach (var key in new[] { InvoiceProviderCatalog.MyDataKey, InvoiceProviderCatalog.DirectIlydaKey })
            Test(key + " timeout blocks resubmission despite route/entity edits", () => {
                var s = new Source(); var h = new Http(s.Id,key) { InvoicePost = body => { throw new HttpRequestException("synthetic timeout"); } }; var service = Service(h);
                Equal("Unknown",service.Send(s.Id,Route(key),s).State);
                s.Invoice.TotalAmount = 999; Equal("Unknown",service.Send(s.Id,Route(InvoiceProviderCatalog.ITimologiaKey),s).State);
                Equal(1,h.Posts); Equal(1,s.Loads); Throws(() => service.LoadParentEvidence(s.Id));
            });
        foreach (var mode in new[] { "matched", "missing", "different-total", "different-customer", "cancelled", "duplicate", "bad-token" })
            Test("myDATA paginated reconciliation " + mode + " never resubmits", () => {
                var s = new Source(); var h = new Http(s.Id,"myDATA") { InvoicePost = b => { throw new HttpRequestException("response lost"); } };
                XNamespace ns = "http://www.aade.gr/myDATA/invoice/v1.0";
                h.Lookup = () => {
                    var invoice = new XElement(XDocument.Parse(h.LastPayload).Root.Element(ns + "invoice"));
                    invoice.AddFirst(new XElement(ns + "uid","UID"),new XElement(ns + "mark","900010"));
                    if (mode == "different-total") invoice.Element(ns + "invoiceSummary").Element(ns + "totalNetValue").Value = "101";
                    if (mode == "different-customer") invoice.Element(ns + "counterpart").Element(ns + "vatNumber").Value = "222222222";
                    var root = new XElement(ns + "RequestedDoc");
                    if (h.Gets == 1 || mode == "bad-token") root.Add(new XElement(ns + "continuationToken",new XElement(ns + "nextPartitionKey","part"),new XElement(ns + "nextRowKey","row")));
                    if (h.Gets > 1 && mode != "missing") root.Add(new XElement(ns + "invoicesDoc",invoice,mode == "duplicate" ? new XElement(invoice) : null));
                    if (mode == "cancelled") root.Add(new XElement(ns + "cancelledInvoicesDoc",new XElement(ns + "cancelledInvoice",new XElement(ns + "invoiceMark","900010"))));
                    return Reply(root.ToString());
                };
                var service = Service(h); Equal("Unknown",service.Send(s.Id,Route(h.Key),s).State);
                var result = service.Recover(s.Id); Check(result.State == (mode == "matched" ? "Accepted" : "Unknown"),Errors(result));
                Equal(1,h.Posts); Equal(2,h.Gets);
                Equal(2,((JArray)JObject.Parse(Latest(s.Id).SnapshotJson)["RecoveryResponsesXml"]).Count);
            });
        Test("iTimologia timeout lookup acceptance is durably saved", () => {
            var s = new Source(); var h = new Http(s.Id,InvoiceProviderCatalog.ITimologiaKey) { InvoicePost = b => { throw new HttpRequestException("timeout"); } }; var service = Service(h);
            var result = service.Send(s.Id,Route(h.Key),s); Check(result.Accepted,Errors(result)); Equal(1,h.Posts); Equal(1,h.Gets);
            Equal(3,Revisions(s.Id).Length); Equal("Unknown",Revisions(s.Id)[1].State);
            Check(service.Recover(s.Id).Accepted,"cached recovery failed"); Equal(1,h.Gets);
        });
        foreach (string mode in new[] { "not-found", "wrong-id", "missing-id", "zero-mark", "cancelled" })
            Test("iTimologia lookup " + mode + " never permits another POST", () => {
                var s = new Source(); var h = new Http(s.Id,InvoiceProviderCatalog.ITimologiaKey) { InvoicePost = b => { throw new HttpRequestException("timeout"); } };
                h.Lookup = () => {
                    if (mode == "not-found") return Reply("{}",404);
                    var data = JObject.Parse(TimReply(s.Id));
                    if (mode == "wrong-id") data["data"]["ext_system_id"] = Guid.NewGuid().ToString("N");
                    if (mode == "missing-id") ((JObject)data["data"]).Remove("ext_system_id");
                    if (mode == "zero-mark") data["data"]["mark"] = "0";
                    if (mode == "cancelled") data["data"]["cancelled_by_mark"] = "999";
                    return Reply(data.ToString());
                };
                var service = Service(h); Equal("Unknown",service.Send(s.Id,Route(h.Key),s).State);
                Equal("Unknown",service.Send(s.Id,Route(h.Key),s).State); Equal(1,h.Posts); Equal(1,s.Loads);
            });
        Test("iTimologia 401 retries only after durable rejection", () => {
            var s = new Source(); var h = new Http(s.Id,InvoiceProviderCatalog.ITimologiaKey);
            h.InvoicePost = b => h.Posts == 1 ? Reply("",401) : Reply(TimReply(s.Id));
            var result = Service(h).Send(s.Id,Route(h.Key),s); Check(result.Accepted,Errors(result)); Equal(2,h.Posts);
            var attempts = History(s.Id); Equal(2,attempts.Length); Equal("Rejected",attempts[0].State); Equal("Accepted",attempts[1].State);
        });
        foreach (int code in new[] { 422, 452 })
            Test("iTimologia " + code + " allows only explicit rejected retry", () => {
                var s = new Source(); var h = new Http(s.Id,"iTimologia") { InvoicePost = b => Reply("{\"error\":\"Synthetic rejection\"}",code) };
                var service = Service(h);
                Equal("Rejected",service.Send(s.Id,Route(h.Key),s).State);
                Equal("Rejected",service.Send(s.Id,Route(h.Key),s).State); Equal(1,h.Posts); Equal(0,h.Gets);
                h.InvoicePost = b => Reply(TimReply(s.Id)); Check(service.Send(s.Id,Route(h.Key),s,true).Accepted,"Explicit retry failed");
                Equal(2,h.Posts); Equal(2,History(s.Id).Length);
            });
        Test("accepted iTimologia missing metadata is enriched by GET only", () => {
            var s = new Source(); var h = new Http(s.Id,"iTimologia");
            h.InvoicePost = b => Reply(new JObject { ["data"] = new JObject { ["ext_system_id"] = s.Id.ToString("N"), ["uid"] = "UID", ["mark"] = "900010" } }.ToString());
            var service = Service(h); Check(service.Send(s.Id,Route(h.Key),s).Accepted,"Missing print metadata revoked acceptance");
            var result = service.Recover(s.Id); Check(result.Accepted,Errors(result)); Equal("https://example.invalid/qr",result.Response.QrCodeUrl);
            Equal(1,h.Posts); Equal(1,h.Gets); Equal(3,Revisions(s.Id).Length);
            Check((string)JObject.Parse(Latest(s.Id).SnapshotJson)["PrintMetadataResponseJson"] != null,"Missing recovery evidence");
            service.Recover(s.Id); Equal(1,h.Gets);
        });
        foreach (var terminalState in new[] { "SUBMISSION_ERRORS", "MAX_RETRIES_REACHED" })
            Test("Ilyda queue " + terminalState + " persists intervention without resending", () => {
                var s = new Source(); var h = new Http(s.Id,"Ilyda.Direct");
                h.InvoicePost = b => Reply("{\"invoiceMarking\":{\"invoiceIdentifier\":\"UID\"},\"errors\":[{\"code\":\"MQ002\",\"fatal\":false}]}");
                h.Lookup = () => Reply(new JObject { ["uid"] = "UID", ["sellerVatIdentifier"] = "000000000", ["invoiceNumber"] = JObject.Parse(h.LastPayload)["invoiceNumber"], ["invoiceState"] = terminalState, ["errorsJson"] = "Synthetic queue error" }.ToString());
                var service = Service(h); Equal("Queued",service.Send(s.Id,Route(h.Key),s).State);
                Equal("QueueFailed",service.Recover(s.Id).State); Equal(terminalState,(string)JObject.Parse(Latest(s.Id).SnapshotJson)["QueueState"]);
                Equal("QueueFailed",service.Send(s.Id,Route(h.Key),s,true).State); Equal(1,h.Posts);
            });
        Test("definitive myDATA rejection requires explicit corrected retry", () => {
            var s = new Source(); var h = new Http(s.Id,InvoiceProviderCatalog.MyDataKey) { InvoicePost = b => Reply("<ResponseDoc><response><statusCode>ValidationError</statusCode><errors><error><code>1</code><message>synthetic</message></error></errors></response></ResponseDoc>") }; var service = Service(h);
            Equal("Rejected",service.Send(s.Id,Route(h.Key),s).State); Equal("Rejected",service.Send(s.Id,Route(h.Key),s).State); Equal(1,h.Posts);
            h.InvoicePost = b => Reply(MyDataReply); Check(service.Send(s.Id,Route(h.Key),s,true).Accepted,"retry failed"); Equal(2,h.Posts); Equal(2,History(s.Id).Length);
        });
        Test("rejected retry cannot change fiscal identity or route", () => {
            var s = new Source(); var h = new Http(s.Id,InvoiceProviderCatalog.MyDataKey) { InvoicePost = b => Reply("<ResponseDoc><response><statusCode>XMLSyntaxError</statusCode></response></ResponseDoc>") }; var service = Service(h);
            service.Send(s.Id,Route(h.Key),s); s.Invoice.Number++;
            Throws(() => service.Send(s.Id,Route(h.Key),s,true)); s.Invoice.Number--;
            Throws(() => service.Send(s.Id,Route(h.Key,false),s,true)); Equal(1,h.Posts);
        });
        Test("Ilyda malformed response is retained as unknown", () => {
            var s = new Source(); var h = new Http(s.Id,InvoiceProviderCatalog.DirectIlydaKey) { InvoicePost = b => Reply("broken-response") };
            Equal("Unknown",Service(h).Send(s.Id,Route(h.Key),s).State); Equal("broken-response",(string)JObject.Parse(Latest(s.Id).SnapshotJson)["ResponseJson"]);
        });
        Test("Ilyda queue recovery verifies identity and preserves original response", () => {
            var s = new Source(); var h = new Http(s.Id,InvoiceProviderCatalog.DirectIlydaKey);
            h.InvoicePost = b => Reply("{\"invoiceMarking\":{\"invoiceIdentifier\":\"UID\",\"mark\":null},\"errors\":[{\"code\":\"MQ001\",\"fatal\":false}]}");
            h.Lookup = () => Reply(new JObject { ["uid"] = "UID", ["sellerVatIdentifier"] = "000000000", ["invoiceNumber"] = JObject.Parse(h.LastPayload)["invoiceNumber"], ["invoiceState"] = "SUBMITTED", ["mark"] = "900010", ["verificationHash"] = "HASH" }.ToString());
            var service = Service(h); Equal("Queued",service.Send(s.Id,Route(h.Key),s).State);
            Check(service.Recover(s.Id).Accepted,"queue recovery failed"); Equal(1,h.Posts); Equal(1,h.Gets);
            Check(Revisions(s.Id)[1].SnapshotJson.Contains("MQ001"),"lost queue evidence");
        });
        Test("Ilyda queue not-found is still queued", () => {
            var s = new Source(); var h = new Http(s.Id,InvoiceProviderCatalog.DirectIlydaKey) { InvoicePost = b => Reply("{\"invoiceMarking\":{\"invoiceIdentifier\":\"UID\"},\"errors\":[{\"code\":\"MQ002\",\"fatal\":false}]}"), Lookup = () => Reply("{}",404) };
            var service = Service(h); service.Send(s.Id,Route(h.Key),s); Equal("Queued",service.Recover(s.Id).State); Equal(1,h.Posts);
        });
        Test("Ilyda PDF failure leaves issuance accepted and upload success is cached", () => {
            var s = new Source(); var h = new Http(s.Id,InvoiceProviderCatalog.DirectIlydaKey); var service = Service(h);
            service.Send(s.Id,Route(h.Key),s); h.UploadStatus = 500; Throws(() => service.UploadIlydaPdf(s.Id,Encoding.ASCII.GetBytes("%PDF-test")));
            Equal("Accepted",Latest(s.Id).State); h.UploadStatus = 200;
            service.UploadIlydaPdf(s.Id,Encoding.ASCII.GetBytes("%PDF-test")); service.UploadIlydaPdf(s.Id,Encoding.ASCII.GetBytes("%PDF-test"));
            Equal(2,h.Uploads); Equal(1,h.Posts); Equal(true,(bool)JObject.Parse(Latest(s.Id).SnapshotJson)["PdfUploaded"]);
        });
        Test("failed result persistence is not reported as acceptance", () => {
            var s = new Source(); var h = new Http(s.Id,InvoiceProviderCatalog.ITimologiaKey); var service = Service(h);
            Execute(connection,"CREATE TRIGGER dbo.TestFailOutcome ON dbo.FiscalAttemptRevision AFTER INSERT AS IF EXISTS (SELECT 1 FROM inserted WHERE State='Accepted') BEGIN RAISERROR('synthetic persistence failure',16,1); ROLLBACK TRANSACTION; END;");
            try { var result = service.Send(s.Id,Route(h.Key),s); Check(!result.Accepted,"Reported an undurable acceptance"); Equal("Sending",Latest(s.Id).State); }
            finally { Execute(connection,"DROP TRIGGER dbo.TestFailOutcome;"); }
            Check(service.Recover(s.Id).Accepted,"Read-only recovery failed"); Equal(1,h.Posts);
        });
        Test("initial journal failure prevents invoice transport", () => {
            var s = new Source(); var h = new Http(s.Id,InvoiceProviderCatalog.MyDataKey);
            Execute(connection,"CREATE TRIGGER dbo.TestFailBegin ON dbo.FiscalAttempt AFTER INSERT AS BEGIN RAISERROR('synthetic begin failure',16,1); ROLLBACK TRANSACTION; END;");
            try { Throws(() => Service(h).Send(s.Id,Route(h.Key),s)); Equal(0,h.Posts); }
            finally { Execute(connection,"DROP TRIGGER dbo.TestFailBegin;"); }
        });
        foreach (string legacy in new[] { "summary", "printed", "signature", "qr" })
            Test("legacy " + legacy + " blocks new issuance", () => {
                var s = new Source(); var h = new Http(s.Id,InvoiceProviderCatalog.MyDataKey);
                if (legacy == "summary") s.Legacy = true;
                if (legacy == "printed") s.Invoice.IsPrinted = true;
                if (legacy == "signature") s.Invoice.InvoiceSignature = "-";
                if (legacy == "qr") s.Invoice.QRCodeData = "old";
                Throws(() => Service(h).Send(s.Id,Route(h.Key),s)); Equal(0,h.Calls); Equal(0,History(s.Id).Length);
            });
        foreach (int payment in new[] { 2,4 }) Test("payment " + payment + " cannot bypass future POS workflow", () => {
            var s = new Source(); s.Invoice.PaymentType = payment; var h = new Http(s.Id,InvoiceProviderCatalog.MyDataKey);
            Throws(() => Service(h).Send(s.Id,Route(h.Key),s)); Equal(0,h.Calls);
        });
        Test("uncertain signing history blocks provider changes", () => {
            var s = new Source(); var h = new Http(s.Id,InvoiceProviderCatalog.MyDataKey);
            var store = new SqlFiscalSigningStore(database); var doc = new FiscalDocumentReference(FiscalDocumentKind.Invoice,s.Id);
            using (var lease = store.Acquire(doc)) store.Begin(lease,new FiscalSigningRequest(Guid.NewGuid(),doc,new FiscalSigningScope("test","device",FiscalEnvironment.Test,"000000000",0),"bytes",new byte[] {1}),DateTime.UtcNow);
            Throws(() => Service(h).Send(s.Id,Route(h.Key),s)); Equal(0,h.Calls); Equal(0,s.Loads);
        });
        Test("missing historical routing metadata fails closed", () => {
            var s = new Source(); var h = new Http(s.Id,InvoiceProviderCatalog.MyDataKey); var store = new SqlMyDataSubmissionStore(database);
            using (store.Acquire("old-scope",s.Id)) store.Save(new MyDataSubmission { AttemptId = Guid.NewGuid(), InvoiceId = s.Id, Scope = "old-scope", RequestXml = "<invoice/>", State = "Sending", StartedUtc = DateTimeOffset.UtcNow });
            Throws(() => Service(h).Send(s.Id,Route(h.Key),s)); Equal(0,h.Calls);
        });
        Test("routing metadata cannot be changed on a persisted attempt", () => {
            var s = new Source(); var h = new Http(s.Id,InvoiceProviderCatalog.MyDataKey) { InvoicePost = b => { throw new HttpRequestException(); } }; Service(h).Send(s.Id,Route(h.Key),s);
            var store = new SqlMyDataSubmissionStore(database); string scope = MyDataOptions.TestUrl + "|account";
            using (store.Acquire(scope,s.Id)) { var a = store.Load(scope,s.Id); a.RoutingContext = "changed"; Throws(() => store.Save(a)); }
        });
        Test("parent with an unresolved cancellation cannot be correlated", () => {
            var s = new Source(); var h = new Http(s.Id,InvoiceProviderCatalog.MyDataKey); var service = Service(h); var result = service.Send(s.Id,Route(h.Key),s);
            using (var lease = database.EnterInvoice(s.Id)) new SqlFiscalOperationStore(database).BeginCancellation(lease,result.AttemptId.Value,"text",Encoding.UTF8.GetBytes("cancel 900010"));
            Throws(() => service.LoadParentEvidence(s.Id));
        });
        Test("concurrent workers cannot both prepare or send", () => {
            var s = new Source(); var h = new Http(s.Id,InvoiceProviderCatalog.MyDataKey);
            using (var entered = new ManualResetEventSlim()) using (var release = new ManualResetEventSlim())
            {
                h.InvoicePost = b => { entered.Set(); if (!release.Wait(10000)) throw new Exception("test timeout"); return Reply(MyDataReply); };
                var running = Task.Run(() => Service(h).Send(s.Id,Route(h.Key),s));
                try { Check(entered.Wait(10000),"sender never entered"); Throws(() => Service(h).Send(s.Id,Route(h.Key),s)); }
                finally { release.Set(); }
                Check(running.GetAwaiter().GetResult().Accepted,"first send failed"); Equal(1,h.Posts); Equal(1,s.Loads);
            }
        });
        Test("HTTP journal omits authentication secrets and ties exchanges to invoice", () => {
            var s = new Source(); var h = new Http(s.Id,InvoiceProviderCatalog.ITimologiaKey); Service(h).Send(s.Id,Route(h.Key),s);
            using (var cn = new SqlConnection(connection)) using (var cmd = cn.CreateCommand())
            {
                cn.Open(); cmd.CommandText = "SELECT RequestJson,ResponseJson FROM dbo.FiscalHttpExchange WHERE InvoiceId=@id"; cmd.Parameters.AddWithValue("@id",s.Id);
                using (var reader = cmd.ExecuteReader()) { int count = 0; while(reader.Read()) { count++; Check(!reader.GetString(0).Contains("SECRET"),"request secret leak"); Check(!reader.GetString(1).Contains("SECRET"),"response secret leak"); } Equal(2,count); }
            }
        });
    }

    sealed class Source : IInvoiceSubmissionSource
    {
        public Guid Id = Guid.NewGuid(); public Invoice Invoice; public int Loads; public bool Legacy;
        public Source()
        {
            Invoice = new Invoice { InvoiceId = Id, Number = 42, Series = "TEST", TransactionDate = new DateTime(2026,10,15,10,30,0), PaymentType = 1,
                TotalAmount = 124, VatAmount = 24, NettoAmount = 100,
                InvoiceType = new InvoiceType { OfficialEnumerator = 100, SendToMyData = true },
                Trader = new Trader { TraderId = Guid.NewGuid(), TaxRegistrationNumber = "111111111", Country = "GR", Name = "Synthetic", Address = "Test Road 2", City = "Athens", ZipCode = "10000" } };
            Invoice.InvoiceLines.Add(new InvoiceLine { InvoiceLineId = Guid.NewGuid(), InvoiceId = Id, Volume = 50, TotalPrice = 124, VatAmount = 24, VatPercentage = 24,
                FuelType = new FuelType { FuelTypeId = Guid.NewGuid(), EnumeratorValue = 30, Name = "Heating diesel" } });
        }
        public InvoiceBuildRequest Prepare(Guid id, ProviderRoute route)
        {
            Loads++; DatabaseInvoiceSource.EnsureNoLegacyHistory(Invoice,Legacy);
            return new HostInvoiceMapper().Map(Invoice,new MappingContext { Environment = route.TestMode ? FiscalEnvironment.Test : FiscalEnvironment.Production,
                Company = new CompanySettings { CompanyTaxId = "000000000", CompanyBranch = "0", CompanyName = "Issuer", CompanyAddress = "Test Road 1", CompanyCity = "Athens", CompanyZipCode = "10000" },
                WholesaleCodes = new System.Collections.Generic.HashSet<int>(new[] {100}), Countries = new[] { new CountryInput { CountryCode = "GR", IsEu = true } } });
        }
    }
    sealed class Credentials : IProviderCredentialsResolver
    {
        public ProviderRoute Last;
        public ProviderCredentials Value;
        public ProviderCredentials Resolve(ProviderRoute route) { Last = route; return Value ?? new ProviderCredentials { SubscriptionKey = "SECRET-key", MasterToken = "SECRET-token", ApiKey = "SECRET-api" }; }
    }
    sealed class Http : HttpMessageHandler
    {
        public readonly Guid Id; public readonly string Key; public int Calls, Posts, Gets, Uploads; public int UploadStatus = 200; public string LastPayload;
        public Func<string,HttpResponseMessage> InvoicePost; public Func<HttpResponseMessage> Lookup;
        public string AuthorizationScheme, AuthorizationParameter; public bool HasApiKey;
        public Http(Guid id,string key) { Id = id; Key = key; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            Calls++; var path = request.RequestUri.AbsolutePath;
            AuthorizationScheme = request.Headers.Authorization?.Scheme; AuthorizationParameter = request.Headers.Authorization?.Parameter;
            HasApiKey = request.Headers.Contains("X-Auth-Key");
            if (path.Contains("/tokens/")) return Task.FromResult(Reply("{\"token\":\"SECRET-communication\",\"expires_at\":\"2099-01-01T00:00:00Z\"}"));
            if (path.Contains("/upload/")) { Uploads++; return Task.FromResult(Reply("{}",UploadStatus)); }
            if (request.Method == HttpMethod.Get) { Gets++; return Task.FromResult(Lookup == null ? Reply(TimReply(Id)) : Lookup()); }
            Posts++; LastPayload = request.Content.ReadAsStringAsync().GetAwaiter().GetResult(); CheckDispatched(Id,LastPayload);
            return Task.FromResult(InvoicePost != null ? InvoicePost(LastPayload) : Reply(Key == InvoiceProviderCatalog.MyDataKey ? MyDataReply : Key == InvoiceProviderCatalog.ITimologiaKey ? TimReply(Id) : IlydaReply));
        }
    }
    static ProviderSubmissionService Service(Http h, Credentials c = null) => new ProviderSubmissionService(database,c ?? new Credentials(),route => h);
    static ProviderRoute Route(string key,bool test = true) => new ProviderRoute(key,test,"000000000",0,"credential-v1","account");
    const string MyDataReply = "<ResponseDoc><response><statusCode>Success</statusCode><invoiceMark>900010</invoiceMark><invoiceUid>UID</invoiceUid></response></ResponseDoc>";
    const string IlydaReply = "{\"invoiceMarking\":{\"invoiceIdentifier\":\"UID\",\"invoiceId\":\"provider-id\",\"mark\":\"900010\",\"verificationHash\":\"HASH\",\"qrCode\":\"https://example.invalid/qr\"},\"errors\":[]}";
    static string TimReply(Guid id) => "{\"data\":{\"ext_system_id\":\"" + id.ToString("N") + "\",\"uid\":\"UID\",\"mark\":\"900010\",\"authentication_code\":\"HASH\",\"web_view\":\"https://example.invalid/qr\"}}";
    static HttpResponseMessage Reply(string text,int status = 200) => new HttpResponseMessage((HttpStatusCode)status) { Content = new StringContent(text) };
    static FiscalAttemptEvidence[] History(Guid id) { using(var l = database.EnterInvoice(id)) return new SqlFiscalHistoryStore(database).Load(l,FiscalJournalFamily.Submission).ToArray(); }
    static FiscalAttemptEvidence Latest(Guid id) => History(id).Last();
    static FiscalAttemptEvidence[] Revisions(Guid id) { using(var l = database.EnterInvoice(id)) return new SqlFiscalHistoryStore(database).LoadHistory(l,FiscalJournalFamily.Submission,Latest(id).AttemptId).ToArray(); }
    static void CheckDispatched(Guid id,string body)
    {
        using (var cn = new SqlConnection(connection)) using (var cmd = cn.CreateCommand())
        {
            cn.Open(); cmd.CommandText = "SELECT TOP (1) a.RequestPayload,r.State FROM dbo.FiscalAttempt a JOIN dbo.FiscalAttemptRevision r ON r.AttemptId=a.AttemptId AND r.Revision=a.CurrentRevision WHERE a.DocumentId=@id AND a.Family='Submission' ORDER BY a.AttemptSequence DESC"; cmd.Parameters.AddWithValue("@id",id);
            using(var r = cmd.ExecuteReader()) { Check(r.Read(),"Transport began before journal"); Equal(body,Encoding.UTF8.GetString((byte[])r[0])); Equal("Sending",r.GetString(1)); }
        }
    }
    static void Execute(string cs,string sql) { using(var c = new SqlConnection(cs)) using(var cmd = c.CreateCommand()) { c.Open(); cmd.CommandText = sql; cmd.ExecuteNonQuery(); } }
    static string Errors(ProviderSubmissionResult r) => string.Join(";",r.Response?.Errors ?? new string[0]);
    static void Throws(Action action) { try { action(); } catch(InvalidOperationException) { return; } catch(SqlException) { return; } throw new Exception("Expected rejection"); }
    static void Equal<T>(T a,T b) { if (!object.Equals(a,b)) throw new Exception("Expected " + a + ", got " + b); }
    static void Check(bool ok,string why) { if (!ok) throw new Exception(why); }
    static void Test(string name,Action action) { try { action(); passed++; Console.WriteLine("PASS " + name); } catch(Exception e) { failed++; Console.WriteLine("FAIL " + name + ": " + e); } }
}
