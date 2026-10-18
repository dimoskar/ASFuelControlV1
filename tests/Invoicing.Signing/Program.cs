using System;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ASFuelControl.Data;
using ASFuelControl.Invoicing;
using ASFuelControl.Invoicing.Persistence;
using Exedron.Invoicing.Core;
using Exedron.Invoicing.Adapters.Samtec;
using Newtonsoft.Json.Linq;

internal static class Program
{
    static string connection;
    static SqlFiscalDatabase database;
    static int passed, failed;
    static int Main(string[] args)
    {
        if (args.Length != 1 || !Regex.IsMatch(args[0], "^ASFC_InvoicingStep6_[0-9a-f]{32}$")) throw new ArgumentException("Private test instance required.");
        string name = "ASFC_Step6_" + Guid.NewGuid().ToString("N");
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
    static FiscalSigningScope Scope(string device = "samtec-test-v1") => new FiscalSigningScope(SamtecFiscalSigner.Key, device, FiscalEnvironment.Test, "000000000", 0);
    static FiscalSigningService Service(Http transport) => new FiscalSigningService(database, scope => new SamtecFiscalSigner(new SamtecDeviceProfile(scope, new Uri("http://device.invalid:8080/")), transport));
    static void Tests()
    {
        foreach (var kind in new[] { FiscalDocumentKind.Invoice, FiscalDocumentKind.Balance, FiscalDocumentKind.TankFilling })
            Test(kind + " saves original bytes before HTTP and retains signature/QR without print mutations", () => {
                var s = new Source(kind); var h = new Http(s.Document); var service = Service(h);
                var result = service.Sign(s.Document, Scope(), s); Equal(FiscalSigningState.Signed, result.State);
                Equal("SIGNATURE", result.Result.Signature); Equal("https://example.invalid/qr", result.Result.QrCode);
                Equal(h.Raw, result.Result.Response); Equal(1, h.Calls); Equal(3, History(s.Document).Length);
                Equal(FiscalSigningState.Prepared, History(s.Document)[0].State); Equal(FiscalSigningState.Signing, History(s.Document)[1].State);
                Equal(FiscalSigningState.Signed, service.Sign(s.Document, Scope("changed"), null).State); Equal(1,h.Calls); Equal(1,s.Loads);
                Check(s.Invoice.IsPrinted != true && s.Invoice.InvoiceSignature == null && s.Invoice.QRCodeData == null, "Invoice was modified");
                Check(s.Balance.PrintDate == null && s.Balance.DocumentSign == null && s.Filling.SignSignature == null, "Non-invoice was modified");
            });
        foreach (var culture in new[] { "el-GR", "en-US" })
            foreach (int code in new[] { 100, 169, 175, 316, 158, 501 })
                Test("legacy wire parity " + culture + " code " + code, () => {
                    var old = Thread.CurrentThread.CurrentCulture;
                    try {
                        Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                        var s = new Source(); s.Invoice.InvoiceType.OfficialEnumerator = code;
                        s.Invoice.InvoiceLines[0].VolumeNormalized = 50.1239m;
                        var legacy = new ASFuelControl.Invoicing.Tests.LegacySamtec().Build(s.Invoice, "Rendered\r\nΚείμενο");
                        var actual = s.Prepare(s.Document, Scope());
                        Check(JToken.DeepEquals(JToken.Parse(legacy), JToken.Parse(Encoding.UTF8.GetString(actual.Payload))), "Wire contract differs from PrintAgent");
                    } finally { Thread.CurrentThread.CurrentCulture = old; }
                });
        Test("code 316 supports sub-litre normalized volume", () => {
            var s = new Source(); s.Invoice.InvoiceType.OfficialEnumerator = 316; s.Invoice.InvoiceLines[0].VolumeNormalized = .1239m;
            Equal("?30!0.123", (string)JObject.Parse(Encoding.UTF8.GetString(s.Prepare(s.Document,Scope()).Payload))["Doc"]["InvoiceDetails"]["PrintLine"]);
        });
        Test("zero VAT exemption, credit and five-decimal price preserve legacy wire", () => {
            var s = new Source(); s.Invoice.PaymentType = 0; s.Invoice.Trader.VatExemption = true;
            var line = s.Invoice.InvoiceLines[0]; line.VatPercentage = 0; line.VatAmount = 0; line.UnitPriceWhole = 1.23456m;
            var legacy = new ASFuelControl.Invoicing.Tests.LegacySamtec().Build(s.Invoice,"Rendered\r\nΚείμενο");
            Check(JToken.DeepEquals(JToken.Parse(legacy),JToken.Parse(Encoding.UTF8.GetString(s.Prepare(s.Document,Scope()).Payload))),"exemption wire changed");
        });
        foreach (string raw in new[] { "broken", "{}", "{\"ResultCode\":\"Error,declined\"}", "{\"ResultCode\":\"ΔΦΣΣ\"}",
            "{\"ResultCode\":\"ΔΦΣΣ,-\"}", "{\"ResultCode\":\"ΔΦΣΣ,Sign Device A\"}", "{\"ResultCode\":\"prefix ΔΦΣΣ,SIGN\"}",
            "{\"JobType\":\"Other\",\"ResultCode\":\"ΔΦΣΣ,SIGN\"}", "{\"InvoiceUID\":\"wrong\",\"ResultCode\":\"ΔΦΣΣ,SIGN\"}" })
            Test("uncertain reply is retained and never re-signed: " + raw, () => {
                var s = new Source(); var h = new Http(s.Document) { Raw = raw }; var service = Service(h);
                var result = service.Sign(s.Document, Scope(), s); Equal(FiscalSigningState.Unknown, result.State); Equal(raw,result.Result.Response);
                Equal(FiscalSigningState.Unknown, service.Sign(s.Document,Scope("new-device"),null,true).State); Equal(1,h.Calls); Equal(1,s.Loads);
            });
        Test("a signature without a QR remains signed", () => {
            var s = new Source(); var h = new Http(s.Document) { Raw = "{\"ResultCode\":\"ΔΦΣΣ,SIGN\"}" };
            var result = Service(h).Sign(s.Document,Scope(),s); Equal(FiscalSigningState.Signed,result.State); Equal<string>(null,result.Result.QrCode);
        });
        Test("HTTP error with apparent signature is unknown", () => {
            var s = new Source(); var h = new Http(s.Document) { Status = 500 };
            var result = Service(h).Sign(s.Document,Scope(),s); Equal(FiscalSigningState.Unknown,result.State); Equal(h.Raw,result.Result.Response);
        });
        Test("timeout remains unknown across service restart and setting changes", () => {
            var s = new Source(); var h = new Http(s.Document) { Respond = () => { throw new HttpRequestException("synthetic timeout"); } };
            Equal(FiscalSigningState.Unknown,Service(h).Sign(s.Document,Scope(),s).State);
            Equal(FiscalSigningState.Unknown,Service(h).Sign(s.Document,Scope("changed"),null,true).State); Equal(1,h.Calls);
        });
        Test("interrupted Signing becomes Unknown without external lookup or Sign", () => {
            var s = new Source(); var store = new SqlFiscalSigningStore(database);
            using (var lease = store.Acquire(s.Document)) { var a = store.Begin(lease,s.Prepare(s.Document,Scope()),DateTime.UtcNow);
                store.Save(lease,0,new FiscalSigningAttempt(a.Request,FiscalSigningState.Signing,1,a.CreatedUtc,a.CreatedUtc)); }
            var h = new Http(s.Document); Equal(FiscalSigningState.Unknown,Service(h).Recover(s.Document).State); Equal(0,h.Calls);
            Equal(FiscalSigningState.Unknown,Service(h).Sign(s.Document,Scope(),null,true).State); Equal(0,h.Calls);
        });
        Test("Prepared requires explicit resume and sends original rendered bytes", () => {
            var s = new Source(); var store = new SqlFiscalSigningStore(database); var original = s.Prepare(s.Document,Scope());
            using (var lease = store.Acquire(s.Document)) store.Begin(lease,original,DateTime.UtcNow);
            s.Invoice.Number = 999; var h = new Http(s.Document); var service = Service(h);
            Equal(FiscalSigningState.Prepared,service.Sign(s.Document,Scope("changed"),s).State); Equal(0,h.Calls);
            Equal(FiscalSigningState.Signed,service.ResumePrepared(s.Document).State);
            Check(original.Payload.SequenceEqual(h.Bytes),"Prepared bytes were rebuilt"); Equal(1,s.Loads);
            Throws(() => service.ResumePrepared(s.Document)); Equal(1,h.Calls);
        });
        Test("local device mismatch is durably failed before dispatch; explicit same-scope retry works", () => {
            var s = new Source(); var h = new Http(s.Document);
            var wrong = new FiscalSigningService(database, scope => new SamtecFiscalSigner(new SamtecDeviceProfile(Scope("wrong"),new Uri("http://device.invalid/")),h));
            Equal(FiscalSigningState.FailedBeforeDispatch,wrong.Sign(s.Document,Scope(),s).State); Equal(0,h.Calls);
            Equal(FiscalSigningState.FailedBeforeDispatch,Service(h).Sign(s.Document,Scope(),s).State);
            Throws(() => Service(h).Sign(s.Document,Scope("changed"),s,true)); Equal(0,h.Calls);
            Equal(FiscalSigningState.Signed,Service(h).Sign(s.Document,Scope(),s,true).State); Equal(1,h.Calls);
        });
        foreach (string stage in new[] { "Prepared", "Signing", "Signed" })
            Test("SQL failure saving " + stage + " never reports success or permits duplicate signing", () => {
                var s = new Source(); var h = new Http(s.Document);
                Execute(connection,"CREATE TRIGGER dbo.TestSigningFailure ON dbo.FiscalAttemptRevision AFTER INSERT AS IF EXISTS (SELECT 1 FROM inserted WHERE State='" + stage + "') BEGIN RAISERROR('synthetic persistence failure',16,1); ROLLBACK TRANSACTION; END;");
                try { Throws(() => Service(h).Sign(s.Document,Scope(),s)); }
                finally { Execute(connection,"DROP TRIGGER dbo.TestSigningFailure;"); }
                Equal(stage == "Signed" ? 1 : 0,h.Calls);
                if (stage == "Signed") { Equal(FiscalSigningState.Unknown,Service(h).Recover(s.Document).State); Equal(1,h.Calls); }
                if (stage == "Signing") Equal(FiscalSigningState.Prepared,Service(h).Sign(s.Document,Scope(),s).State);
            });
        foreach (int? payment in new int?[] { null, 2, 3, 4, 99 }) Test("unsupported payment " + payment + " cannot trigger POS", () => {
            var s = new Source(); s.Invoice.PaymentType = payment; var h = new Http(s.Document);
            Throws(() => Service(h).Sign(s.Document,Scope(),s)); Equal(0,h.Calls); Equal(0,Attempts(s.Document).Length);
        });
        foreach (string field in new[] { "printed", "signature", "qr", "submission", "balance-date", "balance-sign", "filling-sign" })
            Test("legacy history gate " + field, () => {
                var s = new Source(field.StartsWith("balance") ? FiscalDocumentKind.Balance : field == "filling-sign" ? FiscalDocumentKind.TankFilling : FiscalDocumentKind.Invoice);
                if (field == "printed") s.Invoice.IsPrinted = true;
                if (field == "signature") s.Invoice.InvoiceSignature = "-";
                if (field == "qr") s.Invoice.QRCodeData = "old";
                if (field == "submission") s.Legacy = true;
                if (field == "balance-date") s.Balance.PrintDate = DateTime.Now;
                if (field == "balance-sign") s.Balance.DocumentSign = "-";
                if (field == "filling-sign") s.Filling.SignSignature = "-";
                var h = new Http(s.Document); Throws(() => Service(h).Sign(s.Document,Scope(),s)); Equal(0,h.Calls);
            });
        Test("existing submission blocks a fresh signing route", () => {
            var s = new Source(); var store = new SqlMyDataSubmissionStore(database);
            using (store.Acquire("old",s.Document.DocumentId)) store.Save(new MyDataSubmission { AttemptId = Guid.NewGuid(),InvoiceId = s.Document.DocumentId, Scope = "old",RequestXml = "<invoice/>",State = "Sending",StartedUtc = DateTimeOffset.UtcNow });
            var h = new Http(s.Document); Throws(() => Service(h).Sign(s.Document,Scope(),s)); Equal(0,s.Loads); Equal(0,h.Calls);
        });
        foreach (var kind in new[] { FiscalDocumentKind.Alert, FiscalDocumentKind.Titrimetry }) Test(kind + " cannot fabricate a signature", () => {
            var doc = new FiscalDocumentReference(kind,Guid.NewGuid());
            Throws(() => SamtecFiscalSigner.Prepare(doc,Scope(),SamtecRequestBuilder.Create(doc,"text",501)));
        });
        Test("cancellation and invalid tank-filling type fail before transport", () => {
            var s = new Source(); s.Invoice.InvoiceType.OfficialEnumerator = 215; var h = new Http(s.Document);
            Throws(() => Service(h).Sign(s.Document,Scope(),s)); Equal(0,h.Calls);
            Throws(() => HostFiscalSigningMapper.TankFilling(s.Filling,Scope(),"text",999));
        });
        Test("raw payload cannot enable POS or printing", () => {
            var s = new Source(); var original = s.Prepare(s.Document,Scope());
            foreach (string change in new[] { "pos", "print", "token", "uid", "extra" }) {
                var wire = JObject.Parse(Encoding.UTF8.GetString(original.Payload));
                if (change == "pos") wire["Doc"]["Payments"][0]["Type"] = 1;
                if (change == "print") wire["Doc"]["InvoiceDetails"]["PrintDevice"] = 1;
                if (change == "token") wire["Doc"]["InvoiceDetails"]["ReqForToken"] = 1;
                if (change == "uid") wire["Doc"]["InvoiceDetails"]["InvoiceUID"] = Guid.NewGuid().ToString();
                if (change == "extra") wire["DeviceCommand"] = "other";
                var request = new FiscalSigningRequest(Guid.NewGuid(),s.Document,Scope(),SamtecFiscalSigner.Format,Encoding.UTF8.GetBytes(wire.ToString()));
                using(var signer = new SamtecFiscalSigner(new SamtecDeviceProfile(Scope(),new Uri("http://device.invalid/")),new Http(s.Document))) Throws(() => signer.Validate(request));
            }
        });
        Test("concurrent workers cannot prepare or dispatch twice", () => {
            var s = new Source(); var h = new Http(s.Document);
            using(var entered = new ManualResetEventSlim()) using(var release = new ManualResetEventSlim()) {
                h.Respond = () => { entered.Set(); Check(release.Wait(10000),"release timeout"); return Reply(h.Raw); };
                var running = Task.Run(() => Service(h).Sign(s.Document,Scope(),s));
                try { Check(entered.Wait(10000),"sender did not enter"); Throws(() => Service(h).Sign(s.Document,Scope(),s)); }
                finally { release.Set(); }
                Equal(FiscalSigningState.Signed,running.GetAwaiter().GetResult().State); Equal(1,h.Calls); Equal(1,s.Loads);
            }
        });
        Test("optional read-only recovery saves confirmed outcome", () => {
            var s = new Source(); var h = new Http(s.Document) { Raw = "{}" }; Service(h).Sign(s.Document,Scope(),s);
            var signer = new Recovering(); var coordinator = new FiscalSigningCoordinator(new SqlFiscalSigningStore(database),scope => signer);
            Equal(FiscalSigningState.Signed,coordinator.Recover(s.Document).State); Equal(1,signer.Lookups); Equal(0,signer.Signs);
        });
        Test("recovery save failure propagates without swallowing or a second save", () => {
            var s = new Source(); Service(new Http(s.Document) { Raw = "{}" }).Sign(s.Document,Scope(),s);
            var signer = new Recovering(); var coordinator = new FiscalSigningCoordinator(new SqlFiscalSigningStore(database),scope => signer);
            Execute(connection,"CREATE TRIGGER dbo.TestRecoveryFailure ON dbo.FiscalAttemptRevision AFTER INSERT AS IF EXISTS (SELECT 1 FROM inserted WHERE State='Signed') BEGIN RAISERROR('synthetic persistence failure',16,1); ROLLBACK TRANSACTION; END;");
            try { Throws(() => coordinator.Recover(s.Document)); }
            finally { Execute(connection,"DROP TRIGGER dbo.TestRecoveryFailure;"); }
            Equal(FiscalSigningState.Unknown,Attempts(s.Document).Last().State); Equal(1,signer.Lookups); Equal(0,signer.Signs);
        });
    }
    sealed class Recovering : IFiscalSigner, IFiscalSignerRecovery
    {
        public int Signs, Lookups;
        public FiscalSignerCapabilities Capabilities => new FiscalSignerCapabilities(SamtecFiscalSigner.Key,new[] { FiscalDocumentKind.Invoice });
        public FiscalSigningResult Sign(FiscalSigningRequest r) { Signs++; throw new Exception("Recovery must not sign"); }
        public FiscalSigningResult Recover(FiscalSigningRequest r) { Lookups++; return FiscalSigningResult.Signed(r.AttemptId,"recovered",null,null,"verified lookup"); }
    }
    sealed class Source : IFiscalSigningSource
    {
        public FiscalDocumentReference Document; public Invoice Invoice; public Balance Balance; public TankFilling Filling; public int Loads; public bool Legacy;
        public Source(FiscalDocumentKind kind = FiscalDocumentKind.Invoice)
        {
            var id = Guid.NewGuid(); Document = new FiscalDocumentReference(kind,id);
            Invoice = new Invoice { InvoiceId = id, Number = 42, Series = "TEST", PaymentType = 1, TotalAmount = 124, VatAmount = 24, NettoAmount = 100,
                InvoiceType = new InvoiceType { OfficialEnumerator = 100 }, Trader = new Trader { Name = "Synthetic",TaxRegistrationNumber = "111111111",Occupation = "Test",Address = "Road",ZipCode = "10000", City = "Athens" } };
            Invoice.InvoiceLines.Add(new InvoiceLine { InvoiceLineId = Guid.NewGuid(),InvoiceId = id,Volume = 50,VolumeNormalized = 50,TotalPrice = 124,VatAmount = 24,VatPercentage = 24,
                UnitPrice = 2.48m,UnitPriceRetail = 2.50m,UnitPriceWhole = 2m,DiscountAmount = 1, FuelType = new FuelType { EnumeratorValue = 30, Name = "Heating diesel" } });
            Balance = new Balance { BalanceId = id }; Filling = new TankFilling { TankFillingId = id };
        }
        public FiscalSigningRequest Prepare(FiscalDocumentReference document,FiscalSigningScope scope)
        {
            Loads++;
            switch(document.Kind) {
                case FiscalDocumentKind.Invoice: return HostFiscalSigningMapper.Invoice(Invoice,scope,"Rendered\r\nΚείμενο",Legacy);
                case FiscalDocumentKind.Balance: return HostFiscalSigningMapper.Balance(Balance,scope,"Balance\r\nΚείμενο");
                default: return HostFiscalSigningMapper.TankFilling(Filling,scope,"Filling\r\nΚείμενο",40);
            }
        }
    }
    sealed class Http : HttpMessageHandler
    {
        public FiscalDocumentReference Document; public int Calls; public byte[] Bytes; public int Status = 200;
        public string Raw; public Func<HttpResponseMessage> Respond;
        public Http(FiscalDocumentReference document) { Document = document; Raw = "{\"JobType\":\"Sign\",\"InvoiceUID\":\"" + document.DocumentId + "\",\"ResultCode\":\"ΔΦΣΣ,SIGNATURE,https://example.invalid/qr\"}"; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            Calls++; Equal(HttpMethod.Post,request.Method); Equal("device.invalid",request.RequestUri.Host); Equal("text/json",request.Content.Headers.ContentType.MediaType);
            Bytes = request.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
            using(var c = new SqlConnection(connection)) using(var cmd = c.CreateCommand()) {
                c.Open(); cmd.CommandText = "SELECT TOP (1) a.RequestPayload,r.State FROM dbo.FiscalAttempt a JOIN dbo.FiscalAttemptRevision r ON r.AttemptId=a.AttemptId AND r.Revision=a.CurrentRevision WHERE a.DocumentId=@id AND a.Family='Signing' ORDER BY a.AttemptSequence DESC";
                cmd.Parameters.AddWithValue("@id",Document.DocumentId);
                using(var r = cmd.ExecuteReader()) { Check(r.Read(),"No saved signing request"); Check(Bytes.SequenceEqual((byte[])r[0]),"Payload differs from journal"); Equal("Signing",r.GetString(1)); }
            }
            return Task.FromResult(Respond != null ? Respond() : Reply(Raw,Status));
        }
    }
    static FiscalSigningAttempt[] Attempts(FiscalDocumentReference doc) { var store = new SqlFiscalSigningStore(database); using(var lease = store.Acquire(doc)) return store.Load(lease).ToArray(); }
    static FiscalSigningAttempt[] History(FiscalDocumentReference doc) { var store = new SqlFiscalSigningStore(database); using(var lease = store.Acquire(doc)) return store.LoadHistory(lease,store.Load(lease).Last().Request.AttemptId).ToArray(); }
    static HttpResponseMessage Reply(string text,int status = 200) => new HttpResponseMessage((HttpStatusCode)status) { Content = new StringContent(text,Encoding.UTF8) };
    static void Execute(string cs,string sql) { using(var c = new SqlConnection(cs)) using(var cmd = c.CreateCommand()) { c.Open(); cmd.CommandText = sql; cmd.ExecuteNonQuery(); } }
    static void Throws(Action action) { try { action(); } catch(InvalidOperationException) { return; } catch(SqlException) { return; } catch(ArgumentException) { return; } catch(NotSupportedException) { return; } catch(Newtonsoft.Json.JsonException) { return; } throw new Exception("Expected rejection"); }
    static void Equal<T>(T expected,T actual) { if (!object.Equals(expected,actual)) throw new Exception("Expected " + expected + ", got " + actual); }
    static void Check(bool ok,string why) { if (!ok) throw new Exception(why); }
    static void Test(string name,Action action) { try { action(); passed++; Console.WriteLine("PASS " + name); } catch(Exception e) { failed++; Console.WriteLine("FAIL " + name + ": " + e); } }
}
