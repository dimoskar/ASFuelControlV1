using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ASFuelControl.Invoicing;
using ASFuelControl.Invoicing.Persistence;
using ASFuelControl.Windows.Threads;
using Exedron.Invoicing.Core;
using Newtonsoft.Json;

internal static partial class Program
{
    static void ThrowsArgument(Action action)
    {
        try { action(); } catch (ArgumentException) { return; }
        throw new Exception("Expected invalid argument rejection");
    }
    static WorkflowSource LocalSource()
    {
        var source = new WorkflowSource();
        source.Snapshot = new InvoiceWorkflowSnapshot { InvoiceId = source.Source.Id,LocalOnly = true,
            PrintDestination = "original-printer",PrintFormat = "test/v1",PrintContent = Encoding.UTF8.GetBytes("original local document") };
        return source;
    }
    static void ConfigurationTests()
    {
        PendingStatusTests();
        Test("named POS modes resolve documented provider-specific signature formats", () => {
            var ilyda = Route(InvoiceProviderCatalog.DirectIlydaKey); var itim = Route(InvoiceProviderCatalog.ITimologiaKey);
            Equal(9,PosModeCatalog.ForProvider(ilyda.ProviderKey).Count); Equal(6,PosModeCatalog.ForProvider(itim.ProviderKey).Count);
            foreach (var key in new[] { "EDPS","MELLON","VIVA","EPAY","CARDLINK","NEXI","NEOSOFT","ATTICA","WORLDLINE" })
            {
                var route = PaymentRoute.ForMode(ilyda,"http://gateway.invalid:1234","TID",key);
                Equal(key == "EDPS" ? "DEFAULT" : key,route.IlydaProtocol); Equal(key,route.PosMode);
                Equal(JsonConvert.SerializeObject(route),JsonConvert.SerializeObject(JsonConvert.DeserializeObject<PaymentRoute>(JsonConvert.SerializeObject(route))));
            }
            foreach (var key in new[] { "EDPS","MELLON","VIVA","EPAY","CARDLINK","NEXI" })
                Equal(key == "EDPS" || key == "VIVA" ? 1 : 0,PaymentRoute.ForMode(itim,"http://gateway.invalid:1234","TID",key).ITimologiaSignatureFormat);
            foreach (var key in new[] { "NEOSOFT","ATTICA","WORLDLINE",null,"" }) ThrowsArgument(() => PaymentRoute.ForMode(itim,"http://gateway.invalid:1234","TID",key));
            ThrowsArgument(() => PaymentRoute.ForMode(Route(InvoiceProviderCatalog.MyDataKey),"http://gateway.invalid:1234","TID","EDPS"));
            ThrowsArgument(() => new PaymentRoute(itim,"http://gateway.invalid:1234","TID",0,"DEFAULT","EDPS"));
            ThrowsArgument(() => new PaymentRoute(ilyda,"http://gateway.invalid:1234","TID",0,"MELLON","VIVA"));
        });
        Test("historical POS routes keep exact serialization without inferring an ambiguous mode", () => {
            foreach (var key in new[] { InvoiceProviderCatalog.ITimologiaKey,InvoiceProviderCatalog.DirectIlydaKey })
            {
                string json = JsonConvert.SerializeObject(new PaymentRoute(Route(key),"http://gateway.invalid:1234","TID"));
                Check(!json.Contains("PosMode"),"historical request representation changed");
                var restored = JsonConvert.DeserializeObject<PaymentRoute>(json);
                Equal(null,restored.PosMode); Equal(json,JsonConvert.SerializeObject(restored));
            }
        });
        Test("per-type policy resolves five supported transmission/signing combinations", () => {
            var type = Guid.NewGuid();
            var policy = new InvoiceTypePolicy { InvoiceTypeId = type };
            var config = new SharedInvoiceConfiguration { Version = 2,TestMode = true,
                Policies = new Dictionary<Guid,InvoiceTypePolicy> { { type,policy } },
                InvoiceProvider = Route(InvoiceProviderCatalog.DirectIlydaKey), DirectMyData = Route(InvoiceProviderCatalog.MyDataKey),
                FiscalDevice = new FiscalSigningScope("Samtec","device-v1",FiscalEnvironment.Test,"000000000",0),
                DefaultPrinter = new SharedPrinterProfile { PrinterName = "global" } };
            foreach (InvoiceTransmissionMode mode in Enum.GetValues(typeof(InvoiceTransmissionMode)))
            foreach (bool sign in new[] { false,true })
            {
                policy.Transmission = mode; policy.SignWithFiscalDevice = sign;
                if (mode == InvoiceTransmissionMode.Provider && sign) { Throws(() => config.Route(type,null)); continue; }
                var route = config.Route(type,"type-printer");
                Equal(sign,route.Signing != null); Equal(mode != InvoiceTransmissionMode.None,route.Provider != null);
                Equal(mode == InvoiceTransmissionMode.None && !sign,route.LocalOnly); Equal("type-printer",route.Printer.PrinterName);
                Equal("global",config.DefaultPrinter.PrinterName);
            }
        });
        Test("missing reviewed policy and conflicting environment cannot silently fall back", () => {
            var type = Guid.NewGuid(); var config = new SharedInvoiceConfiguration { Version = 2,TestMode = true,
                Policies = new Dictionary<Guid,InvoiceTypePolicy>(),InvoiceProvider = Route(InvoiceProviderCatalog.ITimologiaKey,false) };
            Throws(() => config.Route(type,null));
            config.Policies[type] = new InvoiceTypePolicy { InvoiceTypeId = type,Transmission = InvoiceTransmissionMode.Provider };
            Throws(() => config.Route(type,null));
        });
        Test("Ilyda and iTimologia are global choices without changing type policy", () => {
            var type = Guid.NewGuid(); var policy = new InvoiceTypePolicy { InvoiceTypeId = type,Transmission = InvoiceTransmissionMode.Provider };
            var config = new SharedInvoiceConfiguration { Version = 2,TestMode = true,Policies = new Dictionary<Guid,InvoiceTypePolicy> { { type,policy } },InvoiceProvider = Route(InvoiceProviderCatalog.DirectIlydaKey) };
            var original = config.Route(type,null);
            config.InvoiceProvider = Route(InvoiceProviderCatalog.ITimologiaKey);
            Equal(InvoiceProviderCatalog.ITimologiaKey,config.Route(type,null).Provider.ProviderKey);
            Equal(InvoiceProviderCatalog.DirectIlydaKey,original.Provider.ProviderKey);
            config.Pos = new PaymentRoute(original.Provider,"http://gateway.invalid:1234","TID");
            Throws(() => config.Route(type,null));
        });
        Test("configuration revisions are append-only and reject stale writers", () => {
            var store = new SqlFiscalConfigurationStore(database);
            Equal(null,store.Load());
            var first = store.Save("{\"Version\":2,\"TestMode\":true}",0,"test");
            Throws(() => store.Save("{}",0,"stale"));
            var next = store.Save("{\"Version\":2,\"TestMode\":false}",first,"test");
            Check(next > first,"revision did not advance"); Equal(next,store.Load().Revision);
            using (var c = new System.Data.SqlClient.SqlConnection(connection)) using(var cmd = c.CreateCommand())
            { c.Open(); cmd.CommandText = "SELECT COUNT(*) FROM dbo.FiscalConfiguration"; Equal(2,(int)cmd.ExecuteScalar()); }
        });
        Test("type policies persist independently and stale batches roll back", () => {
            var store = new SqlFiscalConfigurationStore(database);
            var a = new InvoiceTypePolicy { InvoiceTypeId = Guid.Parse("00000000-0000-0000-0000-000000000001"),Transmission = InvoiceTransmissionMode.MyData,SignWithFiscalDevice = true };
            var b = new InvoiceTypePolicy { InvoiceTypeId = Guid.Parse("00000000-0000-0000-0000-000000000002"),Transmission = InvoiceTransmissionMode.None };
            store.SavePolicies(new[] { a,b });
            var stale = store.LoadPolicies()[b.InvoiceTypeId]; b.Transmission = InvoiceTransmissionMode.Provider; store.SavePolicies(new[] { b });
            a.Transmission = InvoiceTransmissionMode.None;
            Throws(() => store.SavePolicies(new[] { a,stale }));
            Equal(InvoiceTransmissionMode.MyData,store.LoadPolicies()[a.InvoiceTypeId].Transmission);
            Equal(1L,a.Revision); Equal(InvoiceTransmissionMode.Provider,store.LoadPolicies()[b.InvoiceTypeId].Transmission);
        });
        Test("POS mode changes persist for new configuration without rewriting the frozen workflow", () => {
            var provider = Route(InvoiceProviderCatalog.DirectIlydaKey);
            var config = new SharedInvoiceConfiguration { Version = 2,InvoiceProvider = provider,Pos = PaymentRoute.ForMode(provider,"http://gateway.invalid:1234","TID","VIVA") };
            var snapshot = new InvoiceWorkflowSnapshot { InvoiceId = Guid.NewGuid(),Provider = provider,Payment = config.Pos };
            var workflows = new SqlInvoiceWorkflowStore(database);
            string original = JsonConvert.SerializeObject(snapshot);
            using (var lease = database.EnterInvoice(snapshot.InvoiceId)) workflows.Save(lease,Encoding.UTF8.GetBytes(original));
            config.Pos = PaymentRoute.ForMode(provider,"http://gateway.invalid:1234","TID","MELLON");
            var store = new SqlFiscalConfigurationStore(database);
            store.Save(JsonConvert.SerializeObject(config),store.Load()?.Revision ?? 0,"test");
            Equal("MELLON",JsonConvert.DeserializeObject<SharedInvoiceConfiguration>(store.Load().Json).Pos.PosMode);
            using (var lease = database.EnterInvoice(snapshot.InvoiceId)) Equal(original,Encoding.UTF8.GetString(workflows.Load(lease)));
        });
        Test("local-only printing creates no fabricated fiscal acceptance and freezes copies", () => {
            var source = LocalSource(); var printer = new Printer(); var http = new Http(source.Source.Id,InvoiceProviderCatalog.MyDataKey);
            var workflow = Workflow(http,printer,render:(s,r) => s.PrintContent);
            var result = workflow.Process(source.Source.Id,source);
            Equal("LocalOnly",result.FiscalState); Check(!result.Accepted && result.ReadyToPrint,"local status must not pretend acceptance");
            Equal("HandedOff",result.Print.State); Equal(null,result.Print.OriginalAttemptId);
            source.Snapshot.PrintContent = Encoding.UTF8.GetBytes("edited"); source.Snapshot.PrintDestination = "changed";
            workflow.Process(source.Source.Id,source); Equal(1,printer.Calls);
            workflow.Process(source.Source.Id,null,true,"operator copy"); Equal(2,printer.Calls);
            Equal("original local document",Encoding.UTF8.GetString(printer.Bytes)); Equal("original-printer",printer.Destination); Equal(0,http.Calls);
            using(var lease = database.EnterInvoice(source.Source.Id))
                foreach(FiscalJournalFamily family in Enum.GetValues(typeof(FiscalJournalFamily)))
                    if(family != FiscalJournalFamily.Print) Equal(0,new SqlFiscalHistoryStore(database).Load(lease,family).Count);
        });
        Test("local-only uncertain printing requires reconciliation before another copy", () => {
            var source = LocalSource(); var printer = new Printer { Uncertain = true }; var http = new Http(source.Source.Id,InvoiceProviderCatalog.MyDataKey);
            var workflow = Workflow(http,printer,render:(s,r) => s.PrintContent);
            var job = workflow.Process(source.Source.Id,source).Print;
            Equal("Unknown",job.State); workflow.Process(source.Source.Id,null,true,"copy"); Equal(1,printer.Calls);
            using(var lease = database.EnterInvoice(source.Source.Id)) new FiscalPrintService(database,printer).Reconcile(lease,job.AttemptId,false,"operator verified spool rejection");
            printer.Uncertain = false;
            Equal("HandedOff",workflow.Process(source.Source.Id,null,true,"operator retry").Print.State); Equal(2,printer.Calls); Equal(0,http.Calls);
        });
        Test("a missing local-only policy or conflicting provider cannot authorize unsigned printing", () => {
            var source = LocalSource(); source.Snapshot.LocalOnly = false;
            var printer = new Printer(); var workflow = Workflow(new Http(source.Source.Id,InvoiceProviderCatalog.MyDataKey),printer,render:(s,r) => s.PrintContent);
            Throws(() => workflow.Process(source.Source.Id,source));
            source.Snapshot.LocalOnly = true; source.Snapshot.Provider = Route(InvoiceProviderCatalog.MyDataKey);
            Throws(() => workflow.Process(source.Source.Id,source)); Equal(0,printer.Calls);
            using(var lease = database.EnterInvoice(Guid.NewGuid()))
                Throws(() => new SqlFiscalOperationStore(database).BeginPrint(lease,null,"printer","test/v1",new byte[] { 1 }));
        });
        Test("confirmed print reconciliation reprojects without dispatch or fiscal calls", () => {
            var source = LocalSource(); var printer = new Printer { Uncertain = true }; string projected = null;
            var workflow = Workflow(new Http(source.Source.Id,InvoiceProviderCatalog.MyDataKey),printer,(id,r) => projected = r.Print?.State, (s,r) => s.PrintContent);
            var job = workflow.Process(source.Source.Id,source).Print;
            using(var lease = database.EnterInvoice(source.Source.Id)) new FiscalPrintService(database,printer).Reconcile(lease,job.AttemptId,true,"operator verified paper");
            workflow.Process(source.Source.Id,null,false); Equal("Confirmed",projected); Equal(1,printer.Calls);
        });
        CatalogTests();
    }
    static void PendingStatusTests()
    {
        var invoice = new Source().Invoice;
        invoice.Number = 0;
        invoice.InvoiceTypeId = Guid.NewGuid();
        invoice.InvoiceType.InvoiceTypeId = invoice.InvoiceTypeId;
        invoice.InvoiceType.Printable = true;
        invoice.InvoiceType.LastNumber = 17;
        var policy = new InvoiceTypePolicy { InvoiceTypeId = invoice.InvoiceTypeId,Transmission = InvoiceTransmissionMode.Provider };
        var config = new SharedInvoiceConfiguration { Version = 2,Enabled = true,TestMode = true,
            FirstInvoiceDate = new DateTime(2026,10,14),Policies = new Dictionary<Guid,InvoiceTypePolicy> { { invoice.InvoiceTypeId,policy } },
            DirectMyData = Route("myDATA"),DefaultPrinter = new SharedPrinterProfile { PrinterName = "test" } };
        foreach (var key in new[] { "myDATA","iTimologia","Ilyda.Direct","none" })
        {
            Test(key + " pending invoice shows configured destination without claiming issuance or allocating a number", () => {
                policy.Transmission = key == "none" ? InvoiceTransmissionMode.None : key == "myDATA" ? InvoiceTransmissionMode.MyData : InvoiceTransmissionMode.Provider;
                config.InvoiceProvider = key == "none" || key == "myDATA" ? null : Route(key);
                var summary = config.PendingInvoiceSummary(invoice,false);
                Check(summary.Contains(key == "Ilyda.Direct" ? "Ilyda" : key == "none" ? "Χωρίς διαβίβαση" : key),"missing configured destination");
                Check(summary.Contains("Δεν ξεκίνησε") && !summary.Contains("Παλαιό") && !summary.Contains("Αποδεκτό"),"misleading issuance status");
                Equal(0,invoice.Number); Equal(17,invoice.InvoiceType.LastNumber);
            });
        }
        Test("pending summary distinguishes inactive and paused runtime configuration", () => {
            config.Enabled = false;
            Check(config.PendingInvoiceSummary(invoice,false).Contains("Ανενεργή"),"missing inactive runtime");
            config.ProtectedKinds.Add(FiscalDocumentKind.Invoice);
            Check(config.PendingInvoiceSummary(invoice,false).Contains("Σε παύση"),"missing pause status");
            config.Enabled = true;
        });
        Test("a newly created cancellation invoice shows the unsupported workflow instead of a legacy label", () => {
            invoice.InvoiceType.IsCancelation = true;
            invoice.InvoiceType.OfficialEnumerator = 215;
            var summary = config.PendingInvoiceSummary(invoice,false);
            Check(summary.Contains("ακυρωτικός") && summary.Contains("ειδική ροή ακύρωσης") && !summary.Contains("Παλαιό"),"missing cancellation explanation");
            Equal(0,invoice.Number); invoice.InvoiceType.IsCancelation = false; invoice.InvoiceType.OfficialEnumerator = 100;
        });
        foreach (int code in new[] { 169,175 })
            Test("credit type " + code + " is eligible without a cancellation blocker", () => {
                invoice.InvoiceType.IsCancelation = true; invoice.InvoiceType.OfficialEnumerator = code;
                Equal(null,SharedInvoiceConfiguration.SpecialWorkflowProblem(invoice.InvoiceType));
                config.PendingRoute(invoice,false);
                Check(!config.PendingInvoiceSummary(invoice,false).Contains("ειδική ροή ακύρωσης"),"credit blocked as cancellation");
                invoice.InvoiceType.IsCancelation = false; invoice.InvoiceType.OfficialEnumerator = 100;
            });
        Test("special cancellation and tank types remain individually protected", () => {
            invoice.InvoiceType.OfficialEnumerator = 215;
            Throws(() => config.PendingRoute(invoice,false));
            invoice.InvoiceType.OfficialEnumerator = 100; invoice.InvoiceType.ForcesDelivery = true;
            Throws(() => config.PendingRoute(invoice,false));
            invoice.InvoiceType.ForcesDelivery = false;
        });
        Test("printed historical invoices allow explicit submission with the saved identity", () => {
            invoice.IsPrinted = true; invoice.Number = 42;
            policy.Transmission = InvoiceTransmissionMode.MyData;
            config.FirstInvoiceDate = invoice.TransactionDate.AddDays(1);
            Check(config.CanSubmitPrintedInvoice(invoice,false),"printed invoice was blocked");
            Throws(() => config.PendingRoute(invoice,false)); // No automatic pre-cutover adoption.
            Equal(config.DirectMyData,config.PendingRoute(invoice,false,true).Provider);
            Check(config.PendingInvoiceSummary(invoice,false).Contains("χωρίς νέα εκτύπωση"),"missing send-only action");
            Equal(42,invoice.Number); Equal(17,invoice.InvoiceType.LastNumber);
            config.FirstInvoiceDate = invoice.TransactionDate.AddDays(-1);
            config.PendingRoute(invoice,false);
        });
        Test("printed submission never bypasses fiscal evidence or re-numbers an invoice", () => {
            Check(!config.CanSubmitPrintedInvoice(invoice,true),"legacy submission was ignored");
            invoice.InvoiceSignature = "signature";
            Check(!config.CanSubmitPrintedInvoice(invoice,false),"legacy signing was ignored");
            invoice.InvoiceSignature = null; invoice.QRCodeData = "qr";
            Check(!config.CanSubmitPrintedInvoice(invoice,false),"legacy QR was ignored");
            invoice.QRCodeData = null; invoice.Number = 0;
            Check(!config.CanSubmitPrintedInvoice(invoice,false),"printed number zero was accepted");
            invoice.Number = 42;
        });
        Test("printed submission and credits cannot start a fresh POS payment", () => {
            policy.Transmission = InvoiceTransmissionMode.Provider; config.InvoiceProvider = Route("iTimologia");
            config.Pos = PaymentRoute.ForMode(config.InvoiceProvider,"http://gateway.invalid:1234","TID","VIVA");
            foreach (int payment in new[] { 2,4 })
            {
                invoice.PaymentType = payment; invoice.IsPrinted = true;
                Check(!config.CanSubmitPrintedInvoice(invoice,false),"printed POS document would be charged again");
                Throws(() => config.PendingRoute(invoice,false));
                invoice.IsPrinted = false; invoice.InvoiceType.IsCancelation = true;
                Throws(() => config.PendingRoute(invoice,false));
                invoice.InvoiceType.IsCancelation = false;
            }
            invoice.PaymentType = 1; invoice.IsPrinted = true; config.Pos = null;
        });
        Test("printed submission cannot sign again or bypass inactive configuration", () => {
            policy.Transmission = InvoiceTransmissionMode.MyData; policy.SignWithFiscalDevice = true;
            config.FiscalDevice = new FiscalSigningScope("fake","device-v1",FiscalEnvironment.Test,"000000000",0);
            Check(!config.CanSubmitPrintedInvoice(invoice,false),"printed invoice would be signed");
            policy.SignWithFiscalDevice = false; config.Enabled = false;
            Check(!config.CanSubmitPrintedInvoice(invoice,false),"inactive runtime accepted submission");
            config.Enabled = true; invoice.IsPrinted = false; invoice.Number = 0;
        });
        Test("pending summary exposes missing policy without guessing a provider", () => {
            config.Policies.Clear();
            Check(config.PendingInvoiceSummary(invoice,false).Contains("Δεν μπορεί να προσδιοριστεί"),"missing configuration error");
            config.Policies.Add(invoice.InvoiceTypeId,policy);
        });
        Test("pending summary reports legacy evidence only when evidence exists", () => {
            Check(!config.PendingInvoiceSummary(invoice,false).Contains("εκτός κοινής ροής"),"new invoice labelled legacy");
            Check(config.PendingInvoiceSummary(invoice,true).Contains("εκτός κοινής ροής"),"missing legacy submission evidence");
            invoice.InvoiceSignature = "existing signature";
            Check(config.PendingInvoiceSummary(invoice,false).Contains("εκτός κοινής ροής"),"missing legacy signature evidence");
            invoice.InvoiceSignature = null;
        });
        Test("pending summary explains cutover date and unsaved invoices", () => {
            config.FirstInvoiceDate = invoice.TransactionDate.AddDays(1);
            Check(config.PendingInvoiceSummary(invoice,false).Contains("πριν από την έναρξη"),"missing cutover reason");
            Check(config.PendingInvoiceSummary(null,false).Contains("Αποθηκεύστε"),"missing unsaved status");
        });
    }
    static void CatalogTests()
    {
        // Extend only the disposable host-schema fixture created by NumberingTests.
        Execute(connection,"ALTER TABLE dbo.InvoiceType ADD Description nvarchar(100); ALTER TABLE dbo.Invoice ADD TraderId uniqueidentifier NULL,VehiclePlateNumber nvarchar(100) NULL; CREATE TABLE dbo.Trader(TraderId uniqueidentifier,Name nvarchar(100)); ALTER TABLE dbo.MyDataInvoice ADD Mark bigint NULL,MyDataInvoiceId uniqueidentifier NULL,Status int NULL,Errors nvarchar(max),DateTimeSent datetime NULL;");
        Test("catalog includes local documents without myDATA rows and treats filter text literally", () => {
            var type = Guid.NewGuid(); var source = LocalSource();
            Execute(connection,"INSERT dbo.InvoiceType(InvoiceTypeId,LastNumber,DefaultSeries,Description) VALUES('" + type + "',1,'L',N'sample%test'); INSERT dbo.Invoice(InvoiceId,InvoiceTypeId,Number,Series,TransactionDate) VALUES('" + source.Source.Id + "','" + type + "',1,'L','20261016');");
            Workflow(new Http(source.Source.Id,InvoiceProviderCatalog.MyDataKey),new Printer(),render:(s,r) => s.PrintContent).Process(source.Source.Id,source);
            var store = new SqlFiscalCatalogStore(database);
            var row = store.Load(new DateTime(2026,10,16),new DateTime(2026,10,17),"sample%").Single();
            Equal(source.Source.Id,row.InvoiceId); Check(row.Owned,"owned invoice missing"); Equal("Τοπικό",row.Route); Equal("HandedOff",row.Printing); Equal(null,row.Mark);
            Equal(0,store.Load(new DateTime(2026,10,16),new DateTime(2026,10,17),"' OR 1=1 --").Count);
            Equal(0,store.Load(new DateTime(2026,10,16),new DateTime(2026,10,17),"sample_").Count);
            Execute(connection,"UPDATE dbo.Invoice SET IsPrinted=1 WHERE InvoiceId='" + source.Source.Id + "';");
            Equal(0,new SqlFiscalConfigurationStore(database).CountLegacyInCutover(new DateTime(2026,10,16)));
            var legacy = Guid.NewGuid();
            Execute(connection,"INSERT dbo.Invoice(InvoiceId,InvoiceTypeId,Number,Series,TransactionDate,InvoiceSignature) VALUES('" + legacy + "','" + type + "',2,'L','20261016','legacy signature');");
            Equal(1,new SqlFiscalConfigurationStore(database).CountLegacyInCutover(new DateTime(2026,10,16)));
        });
    }
}
