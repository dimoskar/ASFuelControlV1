using System;
using System.Linq;
using System.Text;
using ASFuelControl.Invoicing;
using ASFuelControl.Invoicing.Persistence;
using ASFuelControl.Windows.Threads;
using Exedron.Invoicing.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static partial class Program
{
    static ProviderRoute ConnectionRoute(string account, string key = "iTimologia") =>
        new ProviderRoute(key,true,"000000000",0,"profile-" + account,account);
    static byte[] WorkflowBytes(Guid id)
    {
        using (var lease = database.EnterInvoice(id)) return new SqlInvoiceWorkflowStore(database).Load(lease);
    }
    static WorkflowSource ConnectedSource(ProviderRoute route, bool printed = false)
    {
        var source = new WorkflowSource();
        source.Source.Invoice.IsPrinted = printed; source.Source.AllowPrinted = printed;
        source.Snapshot = new InvoiceWorkflowSnapshot { InvoiceId = source.Source.Id,Provider = route,
            Invoice = source.Source.Prepare(source.Source.Id,route),PrintDestination = "original printer",
            PrintFormat = "test/v1",PrintContent = Encoding.UTF8.GetBytes("original pages") };
        return source;
    }
    static void ConnectionTests()
    {
        Test("current credential reads use exact saved profile keys, including historical recovery", () => {
            var oldRoute = ConnectionRoute("3"); var newRoute = ConnectionRoute("1");
            var config = new SharedInvoiceConfiguration { Version = 2,InvoiceProvider = newRoute,Credentials = new[] {
                new SharedCredentialProfile { Route = oldRoute,MasterTokenOption = "old-key" },
                new SharedCredentialProfile { Route = newRoute,MasterTokenOption = "new-key" } } };
            var values = new System.Collections.Generic.Dictionary<string,string> { { "old-key","old-secret" }, { "new-key","new-secret" } };
            string readKey = null; Func<string,string> read = key => { readKey = key; return values[key]; };
            Equal("new-secret",config.Resolve(config.ConfiguredConnection(oldRoute),read).MasterToken); Equal("new-key",readKey);
            Equal("old-secret",config.Resolve(oldRoute,read).MasterToken); Equal("old-key",readKey);
            Throws(() => config.Resolve(ConnectionRoute("missing"),read)); Equal("old-key",readKey);
        });
        Test("authentication-only failure refreshes subscription 3 to configured 1 and current credentials on resume", () => {
            var source = ConnectedSource(ConnectionRoute("3"),true); var id = source.Source.Id;
            var config = new SharedInvoiceConfiguration { Version = 2,InvoiceProvider = source.Snapshot.Provider };
            var credentials = new Credentials { Read = route => new ProviderCredentials { MasterToken = "synthetic-token-" + route.CredentialProfile } };
            var http = new Http(id,"iTimologia") { TokenResponse = () => Reply("{\"message\":\"The selected subscription id is invalid.\"}",422) };
            var printer = new Printer(); int resolves = 0;
            var workflow = Workflow(http,printer,currentConnection: saved => { resolves++; return config.ConfiguredConnection(saved); },credentials:credentials);
            Equal("NotSubmitted",workflow.Process(id,source,false).FiscalState);
            Equal("3",(string)JObject.Parse(http.TokenPayload)["subscription_id"]);
            Equal(0,History(id).Length); Equal(0,http.Posts);
            var before = JObject.Parse(Encoding.UTF8.GetString(WorkflowBytes(id)));
            config.InvoiceProvider = ConnectionRoute("1"); http.TokenResponse = null;
            // Changing the live ORM/source must not change already frozen invoice data.
            source.Source.Invoice.Number = 999; source.Source.Invoice.TotalAmount = 999;
            Check(workflow.Process(id,null,false).Accepted,"retry did not use corrected subscription");
            Equal("1",(string)JObject.Parse(http.TokenPayload)["subscription_id"]);
            Equal("synthetic-token-profile-1",http.TokenBearer); Equal("1",credentials.Last.Account);
            Equal(1,http.Posts); Equal(0,printer.Calls); Equal(0,http.Signatures); Equal(0,Prints(id).Length);
            var afterBytes = WorkflowBytes(id); var after = JObject.Parse(Encoding.UTF8.GetString(afterBytes));
            Equal("1",(string)after["Provider"]["Account"]);
            Equal("3",(string)after["PreviousProviderConnections"][0]["Provider"]["Account"]);
            Check(JToken.DeepEquals(before["Invoice"],after["Invoice"]),"Frozen invoice was remapped");
            Check(JToken.DeepEquals(before["PrintContent"],after["PrintContent"]),"Original pages changed");
            var journalRoute = JObject.Parse((string)JObject.Parse(Latest(id).SnapshotJson)["RoutingContext"])["Route"];
            Equal("1",(string)journalRoute["Account"]); Equal("profile-1",(string)journalRoute["CredentialProfile"]);
            config.InvoiceProvider = ConnectionRoute("2");
            Check(workflow.Process(id,null,false).Accepted,"accepted result not recovered");
            Equal(2,resolves); Equal(1,http.Posts); Check(afterBytes.SequenceEqual(WorkflowBytes(id)),"accepted connection changed");
        });
        foreach (var key in new[] { "myDATA","iTimologia","Ilyda.Direct" })
            Test(key + " first submission reads configured account and freezes it after rejection", () => {
                var source = ConnectedSource(ConnectionRoute("3",key)); var id = source.Source.Id;
                var route = ConnectionRoute("1",key);
                var config = new SharedInvoiceConfiguration { Version = 2,InvoiceProvider = route,DirectMyData = route };
                var credentials = new Credentials(); int resolves = 0;
                var http = new Http(id,key) { InvoicePost = body => Reply(key == "myDATA"
                    ? "<ResponseDoc><response><statusCode>ValidationError</statusCode><errors><error><message>Synthetic rejection</message><code>201</code></error></errors></response></ResponseDoc>"
                    : "{\"invoiceMarking\":null,\"errors\":[{\"message\":\"Synthetic rejection\",\"fatal\":true,\"code\":\"201\"}]}",key == "myDATA" ? 200 : 422) };
                var workflow = Workflow(http,new Printer(),currentConnection:saved => { resolves++; return config.ConfiguredConnection(saved); },credentials:credentials);
                Equal("Rejected",workflow.Process(id,source,false).FiscalState); Equal("1",credentials.Last.Account);
                var frozen = WorkflowBytes(id); route = ConnectionRoute("2",key); config.InvoiceProvider = config.DirectMyData = route;
                http.InvoicePost = null;
                Check(workflow.Process(id,null,false,retryRejected:true).Accepted,"explicit rejected retry failed");
                Equal(1,resolves); Equal("1",credentials.Last.Account); Equal(2,http.Posts);
                Check(frozen.SequenceEqual(WorkflowBytes(id)),"rejected attempt changed connection");
            });
        Test("lost submission response cannot refresh the account or issue another POST", () => {
            var source = ConnectedSource(ConnectionRoute("3")); var id = source.Source.Id; int resolves = 0;
            var route = ConnectionRoute("1"); var credentials = new Credentials();
            var http = new Http(id,"iTimologia") { InvoicePost = body => { throw new System.Net.Http.HttpRequestException("synthetic lost response"); },Lookup = () => Reply("{}",404) };
            var workflow = Workflow(http,new Printer(),currentConnection:r => { resolves++; return route; },credentials:credentials);
            Equal("Unknown",workflow.Process(id,source,false).FiscalState);
            var frozen = WorkflowBytes(id); route = ConnectionRoute("2");
            Equal("Unknown",workflow.Process(id,null,false).FiscalState);
            Equal(1,resolves); Equal("1",credentials.Last.Account); Equal(1,http.Posts);
            Check(frozen.SequenceEqual(WorkflowBytes(id)),"uncertain submission switched connection");
            using (var lease = database.EnterInvoice(id))
                Throws(() => new SqlInvoiceWorkflowStore(database).RefreshProviderConnection(lease,frozen,JsonConvert.SerializeObject(route)));
        });
        Test("POS connection refresh precedes provider signature and charges only once", () => {
            var source = ConnectedSource(ConnectionRoute("3")); var id = source.Source.Id;
            source.Source.Invoice.PaymentType = 2; source.Snapshot.Invoice = source.Source.Prepare(id,source.Snapshot.Provider);
            source.Snapshot.Payment = PaymentRoute.ForMode(source.Snapshot.Provider,"http://gateway.invalid:1234","TID-1","EDPS");
            var http = new Http(id,"iTimologia"); var gateway = new Gateway(); var credentials = new Credentials(); int resolves = 0;
            var workflow = new InvoiceWorkflowService(database,Service(http,credentials),new ProviderPaymentService(database,credentials,r => http,r => gateway),
                null,new FiscalPrintService(database,new Printer()),(s,r) => s.PrintContent,(i,r) => {},saved => { resolves++; return ConnectionRoute("1"); });
            Check(workflow.Process(id,source,false).Accepted,"POS invoice failed");
            var after = JsonConvert.DeserializeObject<InvoiceWorkflowSnapshot>(Encoding.UTF8.GetString(WorkflowBytes(id)));
            Equal("1",after.Provider.Account); Equal(JsonConvert.SerializeObject(after.Provider),JsonConvert.SerializeObject(after.Payment.Provider));
            Equal("EDPS",after.Payment.PosMode); Equal("TID-1",after.Payment.TerminalId); Equal(source.Snapshot.Payment.Endpoint,after.Payment.Endpoint);
            Equal("1",credentials.Last.Account); Equal("1",(string)JObject.Parse(http.TokenPayload)["subscription_id"]);
            workflow.Process(id,null,false); Equal(1,resolves); Equal(1,gateway.Calls); Equal(1,http.Signatures); Equal(1,http.Posts);
        });
        Test("connection-only update preserves unknown input, audit and rejects stale snapshots or scope changes", () => {
            var source = ConnectedSource(ConnectionRoute("3")); var store = new SqlInvoiceWorkflowStore(database);
            var original = JObject.FromObject(source.Snapshot); original["FutureField"] = "2026-10-16T12:13:14.000+03:00";
            original["FutureAmount"] = 1234567890.123456789012345678m;
            byte[] bytes = Encoding.UTF8.GetBytes(original.ToString(Formatting.None));
            using (var lease = database.EnterInvoice(source.Source.Id))
            {
                store.Save(lease,bytes);
                var updated = store.RefreshProviderConnection(lease,bytes,JsonConvert.SerializeObject(ConnectionRoute("1")));
                Check(updated.SequenceEqual(store.Load(lease)),"Updated hash/payload not persisted");
                Check(Encoding.UTF8.GetString(updated).Contains("2026-10-16T12:13:14.000+03:00"),"Unknown date-like string changed");
                Check(Encoding.UTF8.GetString(updated).Contains("1234567890.123456789012345678"),"Decimal precision changed");
                Check(updated.SequenceEqual(store.RefreshProviderConnection(lease,updated,JsonConvert.SerializeObject(ConnectionRoute("1")))),"Unchanged settings wrote another revision");
                Throws(() => store.RefreshProviderConnection(lease,bytes,JsonConvert.SerializeObject(ConnectionRoute("2"))));
                foreach (var route in new[] { ConnectionRoute("1","Ilyda.Direct"),new ProviderRoute("iTimologia",false,"000000000",0,"profile-1","1"),
                    new ProviderRoute("iTimologia",true,"111111111",0,"profile-1","1"),new ProviderRoute("iTimologia",true,"000000000",1,"profile-1","1") })
                {
                    Throws(() => store.RefreshProviderConnection(lease,updated,JsonConvert.SerializeObject(route)));
                    Throws(() => new SharedInvoiceConfiguration { Version = 2,InvoiceProvider = route }.ConfiguredConnection(ConnectionRoute("3")));
                }
                var twice = store.RefreshProviderConnection(lease,updated,JsonConvert.SerializeObject(ConnectionRoute("2")));
                var audit = JObject.Parse(Encoding.UTF8.GetString(twice))["PreviousProviderConnections"];
                Equal(2,audit.Count()); Equal("3",(string)audit[0]["Provider"]["Account"]); Equal("1",(string)audit[1]["Provider"]["Account"]);
            }
        });
        Test("prepared signing freezes direct myDATA account before submission", () => {
            var source = ConnectedSource(ConnectionRoute("3","myDATA")); var id = source.Source.Id;
            using (var lease = database.EnterInvoice(id))
            {
                var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(source.Snapshot)); var store = new SqlInvoiceWorkflowStore(database); store.Save(lease,bytes);
                var request = new FiscalSigningRequest(Guid.NewGuid(),lease.Document,new FiscalSigningScope("samtec","test-device",FiscalEnvironment.Test,"000000000",0),"test/v1",Encoding.UTF8.GetBytes("sign"));
                new SqlFiscalSigningStore(database).Begin(lease,request,DateTime.UtcNow);
                Throws(() => store.RefreshProviderConnection(lease,bytes,JsonConvert.SerializeObject(ConnectionRoute("1","myDATA"))));
                int resolves = 0; var http = new Http(id,"myDATA");
                // Deliberately incomplete composition must fail without refreshing or dispatching.
                Throws(() => Workflow(http,new Printer(),currentConnection:r => { resolves++; return ConnectionRoute("1","myDATA"); }).Process(id,null,false));
                Equal(0,resolves); Equal(0,http.Calls); Check(bytes.SequenceEqual(store.Load(lease)),"Signed connection changed");
            }
        });
        Test("missing configuration and changed myDATA endpoint cannot silently reroute a saved invoice", () => {
            Throws(() => new SharedInvoiceConfiguration { Version = 2 }.ConfiguredConnection(ConnectionRoute("3")));
            var oldRoute = ConnectionRoute("3","myDATA");
            var changed = new ProviderRoute("myDATA",true,"000000000",0,"profile-1","1","https://example.invalid/new-api/");
            Throws(() => new SharedInvoiceConfiguration { Version = 2,DirectMyData = changed }.ConfiguredConnection(oldRoute));
            var source = ConnectedSource(oldRoute); var http = new Http(source.Source.Id,"myDATA");
            Throws(() => Workflow(http,new Printer(),currentConnection:r => changed).Process(source.Source.Id,source,false)); Equal(0,http.Calls);
            Equal("3",(string)JObject.Parse(Encoding.UTF8.GetString(WorkflowBytes(source.Source.Id)))["Provider"]["Account"]);
        });
    }
}
