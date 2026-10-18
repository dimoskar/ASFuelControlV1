using System;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ASFuelControl.Data;
using ASFuelControl.Invoicing;
using ASFuelControl.Invoicing.Mapping;
using ASFuelControl.Invoicing.Persistence;
using Exedron.Invoicing.Core;
using Newtonsoft.Json.Linq;

internal static partial class Program
{
    static string connection;
    static SqlFiscalDatabase database;
    static int passed, failed;
    static int Main(string[] args)
    {
        if (args.Length == 5) return RolloutChild(args);
        if (args.Length != 1 || !Regex.IsMatch(args[0], "^ASFC_InvoicingStep8_[0-9a-f]{32}$")) throw new ArgumentException("Private test instance required.");
        string name = "ASFC_Step8_" + Guid.NewGuid().ToString("N");
        var cs = new SqlConnectionStringBuilder { DataSource = "(localdb)\\" + args[0], InitialCatalog = "master", IntegratedSecurity = true, Pooling = false };
        string master = cs.ConnectionString; cs.InitialCatalog = name; connection = cs.ConnectionString;
        bool created = false;
        try
        {
            Execute(master, "CREATE DATABASE [" + name + "];"); created = true;
            Execute(master, "ALTER DATABASE [" + name + "] SET COMPATIBILITY_LEVEL = 100;");
            database = new SqlFiscalDatabase(connection); database.InstallSchema();
            OwnershipBeforeActivationTests(); WorkflowTests(); ConnectionTests(); ConfigurationTests(); RolloutTests(); Console.WriteLine("RESULT: " + passed + " passed, " + failed + " failed."); return failed == 0 ? 0 : 1;
        }
        finally
        {
            SqlConnection.ClearAllPools();
            if (created) Execute(master, "ALTER DATABASE [" + name + "] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [" + name + "];");
        }
    }
    sealed class Source : IInvoiceSubmissionSource
    {
        public Guid Id = Guid.NewGuid(); public Invoice Invoice; public int Loads; public bool Legacy, AllowPrinted;
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
            Loads++; DatabaseInvoiceSource.EnsureNoLegacyHistory(Invoice,Legacy,AllowPrinted);
            return new HostInvoiceMapper().Map(Invoice,new MappingContext { Environment = route.TestMode ? FiscalEnvironment.Test : FiscalEnvironment.Production,
                Company = new CompanySettings { CompanyTaxId = "000000000", CompanyBranch = "0", CompanyName = "Issuer", CompanyAddress = "Test Road 1", CompanyCity = "Athens", CompanyZipCode = "10000" },
                WholesaleCodes = new System.Collections.Generic.HashSet<int>(new[] {100}), Countries = new[] { new CountryInput { CountryCode = "GR", IsEu = true } } });
        }
    }
    sealed class Credentials : IProviderCredentialsResolver
    {
        public ProviderRoute Last;
        public Func<ProviderRoute,ProviderCredentials> Read;
        public ProviderCredentials Resolve(ProviderRoute route) { Last = route; return Read != null ? Read(route) : new ProviderCredentials { SubscriptionKey = "SECRET-key", MasterToken = "SECRET-token", ApiKey = "SECRET-api" }; }
    }
    sealed class Http : HttpMessageHandler
    {
        public readonly Guid Id; public readonly string Key; public int Calls, Posts, Gets, Uploads, Signatures; public int UploadStatus = 200; public string LastPayload;
        public Func<string,HttpResponseMessage> InvoicePost; public Func<HttpResponseMessage> Lookup, TokenResponse;
        public string TokenPayload, TokenBearer;
        public Http(Guid id,string key) { Id = id; Key = key; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            Calls++; var path = request.RequestUri.AbsolutePath;
            if (path.Contains("signature"))
            {
                Signatures++; var input = JObject.Parse(request.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                var names = (int)input["acq_id"] == 0
                    ? new[] { "amount", "total_net_value", "total_vat_amount", "total_gross_value" }
                    : new[] { "total_net_value", "total_vat_amount", "total_gross_value", "amount" };
                string values = "uid;date;mark;" + string.Join(";",names.Select(n => (decimal.Parse((string)input[n],System.Globalization.CultureInfo.InvariantCulture)*100).ToString("0",System.Globalization.CultureInfo.InvariantCulture))) + ";" + (string)input["tid"];
                return Task.FromResult(Reply(new JObject { ["uid"] = "provider-uid", ["signature"] = "signature", ["input_formatted_values"] = values }.ToString()));
            }
            if (path.Contains("/tokens/"))
            {
                TokenPayload = request.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                TokenBearer = request.Headers.Authorization?.Parameter;
                return Task.FromResult(TokenResponse == null ? Reply("{\"token\":\"SECRET-communication\",\"expires_at\":\"2099-01-01T00:00:00Z\"}") : TokenResponse());
            }
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
    static string TimReply(Guid id) => "{\"data\":{\"ext_system_id\":\"" + id.ToString("N") + "\",\"uid\":\"UID\",\"mark\":\"900010\",\"authentication_code\":\"HASH\",\"signature_code\":\"PROVIDER-SIGNATURE\",\"web_view\":\"https://example.invalid/qr\"}}";
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
