using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ASFuelControl.Invoicing;
using ASFuelControl.Invoicing.Persistence;
using Exedron.Invoicing.Core;
using Newtonsoft.Json;

internal static partial class Program
{
    static void OwnershipBeforeActivationTests()
    {
        Test("a never activated installation has no rollback latch", () => {
            var store = new SqlFiscalOwnershipStore(database);
            Equal(0,store.ProtectedKinds().Count);
            using (store.AcquireUnissuedInvoice(Guid.NewGuid())) { }
            Equal(0,store.ProtectedKinds().Count); // An edit lease is not fiscal ownership.
        });
        Test("an editor lease excludes a simultaneous issuance worker until commit", () => {
            var id = Guid.NewGuid();
            using (new SqlFiscalOwnershipStore(database).AcquireUnissuedInvoice(id))
                Task.Run(() => Throws(() => new SqlFiscalDatabase(connection).EnterInvoice(id))).GetAwaiter().GetResult();
            using (database.EnterInvoice(id)) { }
        });
    }
    static void RolloutTests()
    {
        Test("all journal families protect documents even without a workflow snapshot", () => {
            // Each already-tested store supplies real rows, including pre-workflow records.
            foreach (var key in new[] { InvoiceProviderCatalog.MyDataKey,InvoiceProviderCatalog.DirectIlydaKey,InvoiceProviderCatalog.ITimologiaKey })
            {
                var source = new Source(); var http = new Http(source.Id,key);
                Check(Service(http).Send(source.Id,Route(key),source).Accepted,"acceptance failed");
                using (var lease = database.EnterInvoice(source.Id))
                {
                    Equal(null,new SqlInvoiceWorkflowStore(database).Load(lease));
                    Check(new SqlFiscalOwnershipStore(database).Owns(lease),"journal-only ownership lost");
                }
                Throws(() => new SqlFiscalOwnershipStore(database).AcquireUnissuedInvoice(source.Id));
            }
            // Query deliberately has no family/state filter. Verify every permitted
            // family with a disposable journal row derived from a valid saved attempt.
            foreach (FiscalJournalFamily family in Enum.GetValues(typeof(FiscalJournalFamily)))
            {
                var id = Guid.NewGuid(); var attempt = Guid.NewGuid();
                using (var lease = database.EnterInvoice(id))
                {
                Execute(connection,"INSERT dbo.FiscalAttempt(AttemptId,DocumentKind,DocumentId,Family,Operation,Scope,PayloadFormat,RequestPayload,RequestSha256,CreatedUtc,CurrentRevision,BlocksNewAttempt) SELECT TOP(1) '" + attempt + "',1,'" + id + "','" + family + "',Operation,Scope,PayloadFormat,RequestPayload,RequestSha256,CreatedUtc,0,0 FROM dbo.FiscalAttempt;");
                Check(new SqlFiscalOwnershipStore(database).Owns(lease),family + " lost ownership");
                }
                Throws(() => new SqlFiscalOwnershipStore(database).AcquireUnissuedInvoice(id));
            }
        });
        Test("disabling and changing configuration cannot clear prior activation or journal ownership", () => {
            var configs = new SqlFiscalConfigurationStore(database);
            long revision = configs.Load().Revision;
            revision = configs.Save("{\"Version\":2,\"Enabled\":true,\"Documents\":{\"Balance\":{}}}",revision,"rollout test");
            configs.Save("{\"Version\":2,\"Enabled\":false,\"Documents\":{}}",revision,"rollback test");
            var kinds = new SqlFiscalOwnershipStore(new SqlFiscalDatabase(connection)).ProtectedKinds();
            Check(kinds.Contains(FiscalDocumentKind.Invoice),"invoice rollback latch cleared");
            Check(kinds.Contains(FiscalDocumentKind.Balance),"document rollout latch cleared");
            Check(kinds.Contains(FiscalDocumentKind.TankFilling),"journal-only protection cleared");
        });
        foreach (var key in new[] { InvoiceProviderCatalog.MyDataKey,InvoiceProviderCatalog.DirectIlydaKey,InvoiceProviderCatalog.ITimologiaKey })
        Test(key + " process termination during submission survives restart without another POST", () => {
            var source = PrepareCrashSnapshot(key,false); var id = source.Source.Id;
            Equal(23,RunRolloutChild(id,"submit",key));
            Equal("Sending",Latest(id).State);
            var http = new Http(id,key) { Lookup = () => Reply("{}",404) }; var printer = new Printer();
            Workflow(http,printer).Process(id,null,false);
            Equal(0,http.Posts); Equal(0,printer.Calls); Equal(1,History(id).Length);
        });
        Test("approved POS payment survives process termination before invoice reply", () => {
            var source = PrepareCrashSnapshot(InvoiceProviderCatalog.ITimologiaKey,true); var id = source.Source.Id;
            Equal(23,RunRolloutChild(id,"submit",InvoiceProviderCatalog.ITimologiaKey));
            using (var lease = database.EnterInvoice(id))
                Equal("Approved",new SqlFiscalHistoryStore(database).Load(lease,FiscalJournalFamily.Payment).Last().State);
            var http = new Http(id,InvoiceProviderCatalog.ITimologiaKey) { Lookup = () => Reply("{}",404) };
            var gateway = new Gateway(); var printer = new Printer();
            var service = new InvoiceWorkflowService(database,Service(http),new ProviderPaymentService(database,new Credentials(),r => http,r => gateway),
                null,new FiscalPrintService(database,printer),(s,r) => s.PrintContent,(i,r) => {});
            service.Process(id,null,false); Equal(0,gateway.Calls); Equal(0,http.Posts); Equal(0,printer.Calls);
        });
        Test("process termination in printer dispatch requires reconciliation and retains original pages", () => {
            var source = PrepareCrashSnapshot(InvoiceProviderCatalog.MyDataKey,false); var id = source.Source.Id;
            Equal(24,RunRolloutChild(id,"print",InvoiceProviderCatalog.MyDataKey));
            Equal("Dispatching",Prints(id).Single().State);
            var http = new Http(id,InvoiceProviderCatalog.MyDataKey); var printer = new Printer();
            Equal("Unknown",Workflow(http,printer).Process(id,null).Print.State); Equal(0,http.Calls); Equal(0,printer.Calls);
            using (var lease = database.EnterInvoice(id)) new FiscalPrintService(database,printer).Reconcile(lease,Prints(id).Single().AttemptId,false,"test spool confirmed no handoff");
            var result = Workflow(http,printer,render:(s,r) => { throw new Exception("Must use original pages"); }).Process(id,null,true,"operator recovered failed handoff");
            Equal("HandedOff",result.Print.State); Equal(0,http.Calls); Equal(1,printer.Calls);
            Equal("original invoice",Encoding.UTF8.GetString(printer.Bytes));
        });
        MigrationReportTests();
    }
    static WorkflowSource PrepareCrashSnapshot(string key,bool pos)
    {
        var source = new WorkflowSource(); if (pos) source.Source.Invoice.PaymentType = 2;
        using (var lease = database.EnterInvoice(source.Source.Id))
        {
            source.Prepare(lease); source.Snapshot.Provider = Route(key);
            if (pos) source.Snapshot.Payment = new PaymentRoute(source.Snapshot.Provider,"http://gateway.invalid:1234","TID-1");
            new SqlInvoiceWorkflowStore(database).Save(lease,Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(source.Snapshot)));
        }
        return source;
    }
    static int RunRolloutChild(Guid id,string point,string key)
    {
        var cs = new SqlConnectionStringBuilder(connection);
        string instance = cs.DataSource.Substring("(localdb)\\".Length);
        var start = new ProcessStartInfo(Assembly.GetExecutingAssembly().Location,
            instance + " " + cs.InitialCatalog + " " + id + " " + point + " " + key) { UseShellExecute = false,CreateNoWindow = true,WindowStyle = ProcessWindowStyle.Hidden };
        using (var child = Process.Start(start))
        {
            if (!child.WaitForExit(30000)) { child.Kill(); child.WaitForExit(); throw new Exception("Crash probe timed out"); }
            return child.ExitCode;
        }
    }
    static int RolloutChild(string[] args)
    {
        if (!Regex.IsMatch(args[0],"^ASFC_InvoicingStep8_[0-9a-f]{32}$") || !Regex.IsMatch(args[1],"^ASFC_Step8_[0-9a-f]{32}$"))
            throw new ArgumentException("Disposable instance and database required");
        connection = new SqlConnectionStringBuilder { DataSource = "(localdb)\\" + args[0],InitialCatalog = args[1],IntegratedSecurity = true,Pooling = false }.ConnectionString;
        database = new SqlFiscalDatabase(connection);
        var id = Guid.Parse(args[2]); var http = new Http(id,args[4]); var printer = new Printer();
        if (args[3] == "submit") http.InvoicePost = b => { Environment.Exit(23); throw new Exception("unreachable"); };
        else if (args[3] == "print") printer.OnDispatch = () => Environment.Exit(24);
        else throw new ArgumentException("Invalid crash point");
        var service = new InvoiceWorkflowService(database,Service(http),new ProviderPaymentService(database,new Credentials(),r => http,r => new Gateway()),
            null,new FiscalPrintService(database,printer),(s,r) => s.PrintContent,(i,r) => {});
        service.Process(id,null); return 99;
    }
    static void MigrationReportTests()
    {
        Execute(connection,"ALTER TABLE dbo.MyDataInvoice ADD CanceledByMark bigint NULL,CancelationMark bigint NULL,Data ntext NULL;");
        Test("evidence preview paginates equal timestamps and never upgrades legacy evidence", () => {
            var type = Guid.NewGuid(); var ids = Enumerable.Range(0,5).Select(i => Guid.NewGuid()).ToArray();
            Execute(connection,"INSERT dbo.InvoiceType(InvoiceTypeId,LastNumber,DefaultSeries,Description) VALUES('" + type + "',5,'R','review');" +
                string.Join("",ids.Select((id,i) => "INSERT dbo.Invoice(InvoiceId,InvoiceTypeId,Number,Series,TransactionDate) VALUES('" + id + "','" + type + "'," + (i+1) + ",'R','20261101');")));
            Execute(connection,"INSERT dbo.MyDataInvoice(InvoiceId,Mark,Status,Data) VALUES('" + ids[0] + "',123,3,N'<invoice><aa>0</aa></invoice>');" +
                "INSERT dbo.MyDataInvoice(InvoiceId,Mark,CancelationMark,Status) VALUES('" + ids[1] + "',124,125,3); UPDATE dbo.Invoice SET IsPrinted=1 WHERE InvoiceId='" + ids[2] + "';");
            var source = new Source { Id = ids[3] }; source.Invoice.InvoiceId = source.Id;
            foreach (var line in source.Invoice.InvoiceLines) line.InvoiceId = source.Id;
            Service(new Http(source.Id,InvoiceProviderCatalog.MyDataKey)).Send(source.Id,Route(InvoiceProviderCatalog.MyDataKey),source);
            var before = History(source.Id).Length;
            var report = new SqlFiscalMigrationStore(database); var rows = new List<FiscalMigrationRow>();
            DateTime? date = null; Guid? cursor = null;
            do
            {
                var page = report.Load(new DateTime(2026,11,1),new DateTime(2026,11,2),date,cursor,2);
                rows.AddRange(page.Rows); if (!page.HasMore) break;
                date = page.Rows.Last().Date; cursor = page.Rows.Last().InvoiceId;
            } while (rows.Count < 10);
            Equal(5,rows.Count); Equal(5,rows.Select(r => r.InvoiceId).Distinct().Count());
            Equal("Legacy MARK evidence",rows.Single(r => r.InvoiceId == ids[0]).Classification);
            Check(rows.Single(r => r.InvoiceId == ids[0]).ZeroAaPayload,"zero AA not found");
            Equal("Legacy cancellation evidence",rows.Single(r => r.InvoiceId == ids[1]).Classification);
            Equal("Legacy outcome needs reconciliation",rows.Single(r => r.InvoiceId == ids[2]).Classification);
            Equal("Shared attempts: reconciliation required",rows.Single(r => r.InvoiceId == ids[3]).Classification);
            Equal("No recorded fiscal evidence",rows.Single(r => r.InvoiceId == ids[4]).Classification);
            Equal(before,History(source.Id).Length);
            var catalog = new SqlFiscalCatalogStore(database).Load(new DateTime(2026,11,1),new DateTime(2026,11,2),"review");
            Check(catalog.Single(r => r.InvoiceId == source.Id).Owned,"catalog offered legacy actions for a shared attempt");
            using (var lease = database.EnterInvoice(ids[0])) Equal(null,new SqlInvoiceWorkflowStore(database).Load(lease));
        });
    }
}
