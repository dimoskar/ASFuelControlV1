using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ASFuelControl.Invoicing;
using ASFuelControl.Invoicing.Persistence;
using ASFuelControl.Windows.Threads;
using Exedron.Invoicing.Core;
using Newtonsoft.Json;

internal static partial class Program
{
    sealed class WorkflowSource : IInvoiceWorkflowSource
    {
        public Source Source = new Source();
        public InvoiceWorkflowSnapshot Snapshot;
        public int Loads;
        public InvoiceWorkflowSnapshot Prepare(IFiscalDocumentLease lease)
        {
            Loads++;
            return Snapshot ?? (Snapshot = new InvoiceWorkflowSnapshot { InvoiceId = Source.Id,
                Provider = Route(InvoiceProviderCatalog.MyDataKey), Invoice = Source.Prepare(Source.Id, Route(InvoiceProviderCatalog.MyDataKey)),
                PrintDestination = "original-printer", PrintFormat = "test/v1", PrintContent = Encoding.UTF8.GetBytes("original invoice") });
        }
    }
    sealed class Printer : IFiscalPrintTransport
    {
        public int Calls; public bool LocalFail, Uncertain; public byte[] Bytes; public string Destination;
        public Action OnDispatch;
        public void Validate(string destination, string format, byte[] bytes) { if (LocalFail) throw new InvalidOperationException("offline"); }
        public string Dispatch(string destination, string format, byte[] bytes, Guid id)
        {
            Calls++; Bytes = bytes; Destination = destination;
            using (var cn = new System.Data.SqlClient.SqlConnection(connection))
            using (var cmd = cn.CreateCommand())
            {
                cn.Open(); cmd.CommandText = "SELECT r.State FROM dbo.FiscalAttempt a JOIN dbo.FiscalAttemptRevision r ON a.AttemptId=r.AttemptId AND a.CurrentRevision=r.Revision WHERE a.AttemptId=@id";
                cmd.Parameters.AddWithValue("@id",id); Equal("Dispatching",(string)cmd.ExecuteScalar());
            }
            OnDispatch?.Invoke(); if (Uncertain) throw new InvalidOperationException("lost spool reply");
            return "fake spool receipt " + id;
        }
    }
    sealed class Signer : IFiscalSigner
    {
        public int Calls;
        public FiscalSignerCapabilities Capabilities { get; } = new FiscalSignerCapabilities("fake",new[] { FiscalDocumentKind.Invoice,FiscalDocumentKind.Balance,FiscalDocumentKind.TankFilling });
        public FiscalSigningResult Sign(FiscalSigningRequest request) { Calls++; return FiscalSigningResult.Signed(request.AttemptId,"real-signature","https://example.invalid/device-qr",null,"accepted"); }
    }
    sealed class Gateway : System.Net.Http.HttpMessageHandler
    {
        public int Calls;
        public bool Decline;
        protected override Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request,CancellationToken token)
        {
            Calls++; var value = Newtonsoft.Json.Linq.JObject.Parse(request.Content.ReadAsStringAsync().GetAwaiter().GetResult());
            var data = value["TransactionData"];
            return Task.FromResult(Reply(new Newtonsoft.Json.Linq.JObject { ["InputType"] = value["InputType"], ["TransUid"] = value["TransUid"],
                ["Status"] = "200", ["ReplyCode"] = "00", ["EftposReplyCode"] = "200",
                ["EFTReplyData"] = new Newtonsoft.Json.Linq.JArray(new Newtonsoft.Json.Linq.JObject { ["EftposTranId"] = "txn-1",
                    ["TransactionData"] = new Newtonsoft.Json.Linq.JObject { ["TID"] = "TID-1", ["Approved"] = !Decline, ["Voided"] = false,
                        ["Amount"] = 12400, ["TxnType"] = data["TxnType"], ["PaymentType"] = data["PaymentType"], ["CustomerReference"] = value["TransUid"] } }) }.ToString()));
        }
    }
    static InvoiceWorkflowService Workflow(Http http, Printer printer, Action<Guid,InvoiceWorkflowResult> project = null,
        Func<InvoiceWorkflowSnapshot,Exedron.Invoicing.Core.Models.InvoiceResponse,byte[]> render = null, Signer signer = null,
        Func<ProviderRoute,ProviderRoute> currentConnection = null, Credentials credentials = null, IMyDieselRedemptionCoordinator myDiesel = null) =>
        new InvoiceWorkflowService(database, Service(http,credentials), new ProviderPaymentService(database,credentials ?? new Credentials(), r => http),
            new FiscalSigningService(database,s => signer), new FiscalPrintService(database,printer),
            render ?? ((s,r) => Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(s.PrintContent) + " MARK=" + r.Mark)), project ?? ((id,r) => {}), currentConnection,myDiesel);
    static FiscalOperationRecord[] Prints(Guid id) { using(var lease = database.EnterInvoice(id)) return new SqlFiscalOperationStore(database).Load(lease,true).ToArray(); }
    static void WorkflowTests()
    {
        Test("explicit workflow resume retries a declined POS payment and shows its reason", () => {
            var source = new WorkflowSource(); source.Source.Invoice.PaymentType = 2;
            using(var lease = database.EnterInvoice(source.Source.Id)) source.Prepare(lease);
            source.Snapshot.Provider = Route(InvoiceProviderCatalog.ITimologiaKey);
            source.Snapshot.Payment = new PaymentRoute(source.Snapshot.Provider,"http://gateway.invalid:1234","TID-1");
            var http = new Http(source.Source.Id,InvoiceProviderCatalog.ITimologiaKey); var gateway = new Gateway { Decline = true }; var printer = new Printer();
            var workflow = new InvoiceWorkflowService(database,Service(http),new ProviderPaymentService(database,new Credentials(),r => http,r => gateway),
                null,new FiscalPrintService(database,printer),(s,r) => s.PrintContent,(id,r) => {});
            var rejected = workflow.Process(source.Source.Id,source,false);
            Equal("Declined",rejected.PaymentState); Check(rejected.Response.Errors.Any(e => e.Contains("POS")),"Payment explanation was lost");
            Equal(0,http.Posts); gateway.Decline = false;
            workflow.Process(source.Source.Id,null,false); Equal(1,gateway.Calls);
            Check(workflow.Process(source.Source.Id,null,false,retryDeclinedPayment:true).Accepted,"Explicit POS retry did not finish issuance");
            Equal(2,gateway.Calls); Equal(2,http.Signatures); Equal(1,http.Posts); Equal(0,printer.Calls);
        });
        PrinterFallbackTests();
        foreach (string key in new[] { "myDATA","iTimologia","Ilyda.Direct" })
            Test(key + " printed-only submission keeps identity and never dispatches printer or signer", () => {
                var source = new WorkflowSource(); source.Source.Invoice.IsPrinted = true; source.Source.AllowPrinted = true;
                var invoice = source.Source.Invoice;
                invoice.InvoiceTypeId = Guid.NewGuid(); invoice.InvoiceType.InvoiceTypeId = invoice.InvoiceTypeId;
                var policy = new InvoiceTypePolicy { InvoiceTypeId = invoice.InvoiceTypeId,
                    Transmission = key == "myDATA" ? InvoiceTransmissionMode.MyData : InvoiceTransmissionMode.Provider };
                invoice.InvoiceType.Printable = true;
                var config = new SharedInvoiceConfiguration { Version = 2,Enabled = true,TestMode = true,
                    FirstInvoiceDate = invoice.TransactionDate.AddDays(1),InvoiceProvider = Route(key),DirectMyData = Route("myDATA"),
                    Policies = new System.Collections.Generic.Dictionary<Guid,InvoiceTypePolicy> { { invoice.InvoiceTypeId,policy } } };
                Check(config.CanSubmitPrintedInvoice(invoice,false),"cannot explicitly submit printed invoice");
                var route = config.PendingRoute(invoice,false,true).Provider;
                source.Snapshot = new InvoiceWorkflowSnapshot { InvoiceId = invoice.InvoiceId,Provider = route,
                    Invoice = source.Source.Prepare(invoice.InvoiceId,route),PrintDestination = "no installed printer",PrintFormat = "test/v1",PrintContent = Encoding.UTF8.GetBytes("already printed") };
                var http = new Http(invoice.InvoiceId,key); var printer = new Printer(); var signer = new Signer(); int rendered = 0;
                var workflow = Workflow(http,printer,render:(s,r) => { rendered++; throw new Exception("send-only must not render"); },signer:signer);
                Check(workflow.Process(invoice.InvoiceId,source,false).Accepted,"submission failed");
                Check(workflow.Process(invoice.InvoiceId,null,false).Accepted,"accepted submission did not recover");
                Equal(1,http.Posts); Equal(0,http.Signatures); Equal(0,printer.Calls); Equal(0,signer.Calls); Equal(0,rendered);
                Equal(0,Prints(invoice.InvoiceId).Length); Equal(42,invoice.Number); Equal("TEST",invoice.Series);
                Equal(new DateTime(2026,10,15,10,30,0),invoice.TransactionDate); Equal((bool?)true,invoice.IsPrinted);
                using (var lease = database.EnterInvoice(invoice.InvoiceId))
                {
                    var saved = JsonConvert.DeserializeObject<InvoiceWorkflowSnapshot>(Encoding.UTF8.GetString(new SqlInvoiceWorkflowStore(database).Load(lease)));
                    Equal(42,saved.Invoice.Invoice.Number); Equal("TEST",saved.Invoice.Invoice.Series);
                    Equal(invoice.TransactionDate,saved.Invoice.Invoice.PrintedDateTime);
                }
            });
        Test("printed invoice authentication failure retains its error and resumes without printing or renumbering", () => {
            var source = new WorkflowSource(); source.Source.AllowPrinted = true; source.Source.Invoice.IsPrinted = true;
            var id = source.Source.Id; var route = Route("iTimologia");
            source.Snapshot = new InvoiceWorkflowSnapshot { InvoiceId = id,Provider = route,Invoice = source.Source.Prepare(id,route),
                PrintDestination = "unused",PrintFormat = "test/v1",PrintContent = Encoding.UTF8.GetBytes("already printed") };
            var http = new Http(id,"iTimologia") { TokenResponse = () => Reply("{\"message\":\"Synthetic subscription denied SECRET-token\",\"token\":\"DO-NOT-DISPLAY\"}",401) };
            var printer = new Printer(); var workflow = Workflow(http,printer);
            var result = workflow.Process(id,source,false);
            Equal("NotSubmitted",result.FiscalState); Check(!result.Accepted,"authentication failure accepted");
            var error = string.Join(";",result.Response.Errors);
            Check(error.Contains("HTTP 401") && error.Contains("Synthetic subscription denied"),"authentication error was discarded");
            Check(!error.Contains("SECRET-token") && !error.Contains("DO-NOT-DISPLAY"),"authentication response leaked secrets");
            Equal(0,http.Posts); Equal(0,History(id).Length); Equal(0,printer.Calls); Equal(0,http.Signatures);
            string saved;
            using(var lease = database.EnterInvoice(id)) saved = Encoding.UTF8.GetString(new SqlInvoiceWorkflowStore(database).Load(lease));
            http.TokenResponse = null;
            Check(workflow.Process(id,null,false).Accepted,"saved submission could not resume");
            Check(workflow.Process(id,null,false).Accepted,"accepted outcome did not recover");
            Equal(1,http.Posts); Equal(0,printer.Calls); Equal(0,Prints(id).Length); Equal(42,source.Source.Invoice.Number); Equal("TEST",source.Source.Invoice.Series);
            using(var lease = database.EnterInvoice(id)) Equal(saved,Encoding.UTF8.GetString(new SqlInvoiceWorkflowStore(database).Load(lease)));
        });
        Test("provider rejection details reach the workflow caller without a print", () => {
            var source = new WorkflowSource(); var id = source.Source.Id; var route = Route("iTimologia");
            source.Snapshot = new InvoiceWorkflowSnapshot { InvoiceId = id,Provider = route,Invoice = source.Source.Prepare(id,route),
                PrintDestination = "unused",PrintFormat = "test/v1",PrintContent = Encoding.UTF8.GetBytes("invoice") };
            var http = new Http(id,"iTimologia") { InvoicePost = body => Reply("{\"errors\":[{\"message\":\"Synthetic invoice validation failed\"}]}",422) };
            var printer = new Printer(); var result = Workflow(http,printer).Process(id,source,false);
            Equal("Rejected",result.FiscalState); Check(result.Response.Errors.Any(e => e.Contains("Synthetic invoice validation failed")),"rejection details discarded");
            Equal(1,http.Posts); Equal(0,printer.Calls); Equal("Rejected",Latest(id).State);
        });
        Test("POS approval and invoice acceptance survive printer failure without a new charge", () => {
            var source = new WorkflowSource(); source.Source.Invoice.PaymentType = 2;
            using(var lease = database.EnterInvoice(source.Source.Id)) source.Prepare(lease);
            source.Snapshot.Provider = Route(InvoiceProviderCatalog.ITimologiaKey);
            source.Snapshot.Payment = new PaymentRoute(source.Snapshot.Provider,"http://gateway.invalid:1234","TID-1");
            var http = new Http(source.Source.Id,InvoiceProviderCatalog.ITimologiaKey); var gateway = new Gateway(); var printer = new Printer { Uncertain = true };
            var workflow = new InvoiceWorkflowService(database,Service(http),new ProviderPaymentService(database,new Credentials(),r => http,r => gateway),
                null,new FiscalPrintService(database,printer),(s,r) => s.PrintContent,(id,r) => {});
            Equal("Unknown",workflow.Process(source.Source.Id,source).Print.State);
            var calls = http.Calls; workflow.Process(source.Source.Id,null); Equal(calls,http.Calls); Equal(1,gateway.Calls); Equal(1,http.Posts); Equal(1,http.Signatures); Equal(1,printer.Calls);
        });
        Test("concurrent automatic and manual callers cannot hand off duplicate jobs", () => {
            var source = new WorkflowSource(); var http = new Http(source.Source.Id,InvoiceProviderCatalog.MyDataKey); var printer = new Printer(); var workflow = Workflow(http,printer);
            using(var entered = new ManualResetEventSlim()) using(var release = new ManualResetEventSlim())
            {
                printer.OnDispatch = () => { entered.Set(); Check(release.Wait(10000),"timed out"); };
                var running = Task.Run(() => workflow.Process(source.Source.Id,source));
                try { Check(entered.Wait(10000),"no dispatch"); Throws(() => workflow.Process(source.Source.Id,source)); }
                finally { release.Set(); }
                Equal("HandedOff",running.GetAwaiter().GetResult().Print.State); Equal(1,printer.Calls); Equal(1,http.Posts);
            }
        });
        foreach (var key in new[] { "myDATA", "iTimologia", "Ilyda.Direct" })
        {
            Test(key + " manual printing continues a saved unfinished workflow with its issued identity", () => {
                var source = new WorkflowSource();
                using (var lease = database.EnterInvoice(source.Source.Id))
                {
                    var snapshot = source.Prepare(lease); snapshot.Provider = Route(key);
                    new SqlInvoiceWorkflowStore(database).Save(lease,Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(snapshot)));
                }
                // A stale/edited form must not replace the frozen, numbered invoice.
                source.Source.Invoice.Number = 0; source.Source.Invoice.TotalAmount = 999;
                var http = new Http(source.Source.Id,key); var printer = new Printer(); var signer = new Signer();
                var workflow = Workflow(http,printer,signer:signer);
                Equal("HandedOff",workflow.Process(source.Source.Id,source).Print.State);
                Equal(1,source.Loads); Equal(1,http.Posts); Equal(1,printer.Calls); Equal(0,signer.Calls);
                workflow.Process(source.Source.Id,null); Equal(1,http.Posts); Equal(1,printer.Calls);
            });
        }
        foreach (var key in new[] { "iTimologia", "Ilyda.Direct" })
        {
            Test(key + " returns its own invoice signature without calling the fiscal device", () => {
                var source = new WorkflowSource();
                source.Snapshot = new InvoiceWorkflowSnapshot { InvoiceId = source.Source.Id,Provider = Route(key),
                    Invoice = source.Source.Prepare(source.Source.Id,Route(key)),PrintDestination = "test",PrintFormat = "test/v1",PrintContent = Encoding.UTF8.GetBytes("invoice") };
                var http = new Http(source.Source.Id,key); var signer = new Signer();
                var workflow = Workflow(http,new Printer(),signer:signer);
                var result = workflow.Process(source.Source.Id,source,false);
                Check(result.Accepted,"Provider invoice not accepted"); Equal(key == "iTimologia" ? "PROVIDER-SIGNATURE" : "HASH",result.Response.InvoiceSignature);
                Equal(0,signer.Calls);
                using (var lease = database.EnterInvoice(source.Source.Id)) Equal(0,new SqlFiscalHistoryStore(database).Load(lease,FiscalJournalFamily.Signing).Count);
                workflow.Process(source.Source.Id,null,false); Equal(0,signer.Calls); Equal(1,http.Posts);
            });
            Test(key + " response loss never falls back to fiscal signing", () => {
                var source = new WorkflowSource();
                source.Snapshot = new InvoiceWorkflowSnapshot { InvoiceId = source.Source.Id,Provider = Route(key),
                    Invoice = source.Source.Prepare(source.Source.Id,Route(key)),PrintDestination = "test",PrintFormat = "test/v1",PrintContent = Encoding.UTF8.GetBytes("invoice") };
                var http = new Http(source.Source.Id,key) { InvoicePost = b => { throw new System.Net.Http.HttpRequestException("lost response"); }, Lookup = () => Reply("{}",404) };
                var signer = new Signer(); var result = Workflow(http,new Printer(),signer:signer).Process(source.Source.Id,source,false);
                Equal("Unknown",result.FiscalState); Equal(0,signer.Calls); Equal(1,http.Posts);
            });
            Test(key + " print footer requires and includes provider identity", () => {
                var snapshot = new InvoiceWorkflowSnapshot { Provider = new ProviderRoute(key,true,"000000000",0,"test","account",softwareLicence:"TEST-LICENCE") };
                var response = new Exedron.Invoicing.Core.Models.InvoiceResponse { Mark = "900010", Uid = "UID", VerificationHash = "HASH", ProviderInvoiceId = "PROVIDER-ID", ProviderUrl = "https://example.invalid/provider", QrCodeUrl = "https://example.invalid/qr" };
                var footer = ASFuelControl.Windows.Threads.SharedInvoicePrinting.Footer(snapshot,response);
                string licence = key == "iTimologia" ? "2025_11_134TECHNORAN_001_iTimologia_V1_17112025" : "2021_09_108ILYDA_001_ Meg myData_V1_08092021";
                Check(footer.Contains(licence) && !footer.Contains("TEST-LICENCE"),"Footer did not use the official catalogue licence");
                if (key == "Ilyda.Direct") Check(footer.Contains("PROVIDER-ID"),"Missing Ilyda invoice ID");
                string historical = JsonConvert.SerializeObject(snapshot.Provider);
                snapshot.Provider = JsonConvert.DeserializeObject<ProviderRoute>(historical);
                Equal(historical,JsonConvert.SerializeObject(snapshot.Provider));
                snapshot.Provider = Route(key);
                Check(ASFuelControl.Windows.Threads.SharedInvoicePrinting.Footer(snapshot,response).Contains(licence),"Old settings still require manual licence entry");
                Check(footer.Contains("900010") && footer.Contains("UID") && footer.Contains("HASH") && footer.Contains("https://example.invalid/provider"),"Missing fiscal footer");
                response.QrCodeUrl = null; Throws(() => ASFuelControl.Windows.Threads.SharedInvoicePrinting.Footer(snapshot,response));
            });
        }
        Test("historical licence fields keep route fingerprints without becoming editable configuration", () => {
            var oldRoute = Route("iTimologia");
            Check(!JsonConvert.SerializeObject(oldRoute).Contains("SoftwareLicence"),"Historical route fingerprint changed");
            var saved = JsonConvert.DeserializeObject<ProviderRoute>(JsonConvert.SerializeObject(new ProviderRoute("iTimologia",true,"000000000",0,"test","account",softwareLicence:"LICENCE-1")));
            Equal("LICENCE-1",saved.SoftwareLicence);
        });
        Test("provider catalogue separates AADE IDs and licence identifiers for UI and POS", () => {
            var ilyda = InvoiceProviderCatalog.Get("Ilyda.Direct"); var tim = InvoiceProviderCatalog.Get("iTimologia");
            Equal((int?)8,ilyda.AadeProviderId); Equal("008",ilyda.AadeProviderCode); Equal(ilyda.AadeProviderId,ilyda.GBridgeProviderId);
            Equal((int?)34,tim.AadeProviderId); Equal("034",tim.AadeProviderCode); Equal(tim.AadeProviderId,tim.GBridgeProviderId);
            Check(ilyda.SoftwareLicence.Contains("108ILYDA") && tim.SoftwareLicence.Contains("134TECHNORAN"),"wrong licence identity");
            Equal(null,InvoiceProviderCatalog.Get("myDATA").AadeProviderId); Equal(null,InvoiceProviderCatalog.Get("myDATA").SoftwareLicence);
        });
        Test("Ilyda TF2 prints QR and warning while remaining queued; reprint never submits", () => {
            var source = new WorkflowSource();
            source.Snapshot = new InvoiceWorkflowSnapshot { InvoiceId = source.Source.Id, Provider = Route("Ilyda.Direct"),
                Invoice = source.Source.Prepare(source.Source.Id,Route("Ilyda.Direct")), PrintDestination = "test", PrintFormat = "test/v1", PrintContent = Encoding.UTF8.GetBytes("invoice") };
            var http = new Http(source.Source.Id,"Ilyda.Direct") { InvoicePost = b => Reply("{\"invoiceMarking\":{\"invoiceIdentifier\":\"UID\",\"qrCode\":\"https://example.invalid/qr\",\"providerUrl\":\"https://example.invalid\"},\"errors\":[{\"code\":\"MQ002\",\"fatal\":false},{\"code\":\"I9999\",\"fatal\":true}]}") };
            var printer = new Printer();
            var workflow = Workflow(http,printer,render:(s,r) => Encoding.UTF8.GetBytes(ASFuelControl.Windows.Threads.SharedInvoicePrinting.Footer(s,r) + r.QrCodeUrl));
            var result = workflow.Process(source.Source.Id,source);
            Equal("Queued",result.FiscalState); Check(!result.Accepted && result.ReadyToPrint,"TF2 falsely marked accepted");
            Equal("HandedOff",result.Print.State); Check(result.Print.OfflineReceipt,"Missing TF2 print provenance");
            var original = printer.Bytes;
            Check(Encoding.UTF8.GetString(original).Contains("Transmission failure 2") && Encoding.UTF8.GetString(original).Contains("https://example.invalid/qr"),"Missing TF2 warning or QR");
            workflow.Process(source.Source.Id,null,true,"test reprint"); Equal(1,http.Posts); Equal(0,http.Gets); Check(printer.Bytes.SequenceEqual(original),"Reprint changed receipt");
            http.Lookup = () => Reply(new Newtonsoft.Json.Linq.JObject { ["uid"] = "UID", ["sellerVatIdentifier"] = "000000000", ["invoiceNumber"] = Newtonsoft.Json.Linq.JObject.Parse(http.LastPayload)["invoiceNumber"], ["invoiceState"] = "SUBMITTED", ["mark"] = "900010", ["verificationHash"] = "HASH" }.ToString());
            Check(Service(http).Recover(source.Source.Id).Accepted,"Queue did not recover after printing");
            result = workflow.Process(source.Source.Id,null,false); Check(result.Accepted,"Recovered MARK was not projected");
            Equal(1,http.Posts); Equal(1,http.Gets);
        });
        Test("Windows renderer freezes readable Greek text and QR on saved pages", () => {
            var snapshot = new InvoiceWorkflowSnapshot { InvoiceId = Guid.NewGuid(),
                PrintDestination = JsonConvert.SerializeObject(new ASFuelControl.Windows.Threads.SharedPrinterProfile { PrinterName = "test-only", WidthHundredths = 315, HeightHundredths = 1100 }),
                PrintContent = Encoding.UTF8.GetBytes("ΔΟΚΙΜΑΣΤΙΚΟ ΠΡΑΤΗΡΙΟ\r\nΑΦΜ: 000000000\r\nΤΙΜΟΛΟΓΙΟ TEST 42\r\nΗμερ/νία: 15/10/2026 10:30\r\nΠΕΛΑΤΗΣ: Συνθετικός πελάτης\r\nΠετρέλαιο θέρμανσης\r\nΠοσότητα: 50,000 Λίτρα\r\nΚαθαρή αξία: 100,00\r\nΦΠΑ 24%: 24,00\r\nΣΥΝΟΛΟ: 124,00 EUR") };
            var bytes = ASFuelControl.Windows.Threads.SharedInvoicePrinting.Render(snapshot,new Exedron.Invoicing.Core.Models.InvoiceResponse { Mark = "900010",Uid = "TEST-UID",QrCodeUrl = "https://example.invalid/invoice/42" });
            var pages = JsonConvert.DeserializeObject<byte[][]>(Encoding.UTF8.GetString(bytes)); Equal(1,pages.Length);
            using(var stream = new System.IO.MemoryStream(pages[0])) using(var image = System.Drawing.Image.FromStream(stream)) { Equal(472,image.Width); Equal(1650,image.Height); }
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"render-preview.png"),pages[0]);
        });
        Test("issuance, acceptance projection and independent print handoff", () => {
            var source = new WorkflowSource(); var http = new Http(source.Source.Id,InvoiceProviderCatalog.MyDataKey); var printer = new Printer();
            int projections = 0; var workflow = Workflow(http,printer,(id,r) => { projections++; Check(r.Accepted,"projected unaccepted result"); if (r.Print == null) Equal(0,printer.Calls); });
            var result = workflow.Process(source.Source.Id,source); Equal("HandedOff",result.Print.State); Equal(2,projections); Equal(1,http.Posts); Equal(1,printer.Calls);
            Check(Encoding.UTF8.GetString(printer.Bytes).Contains("900010"),"missing MARK");
        });
        Test("polling accepted print makes no provider, printer, render or source call", () => {
            var source = new WorkflowSource(); var http = new Http(source.Source.Id,InvoiceProviderCatalog.MyDataKey); var printer = new Printer(); int renders = 0;
            var workflow = Workflow(http,printer,render:(s,r) => { renders++; return s.PrintContent; });
            workflow.Process(source.Source.Id,source); var calls = http.Calls;
            workflow.Process(source.Source.Id,null); Equal(calls,http.Calls); Equal(1,printer.Calls); Equal(1,renders); Equal(1,source.Loads);
        });
        Test("explicit reprint copies original pages and destination after entity/config edits", () => {
            var source = new WorkflowSource(); var http = new Http(source.Source.Id,InvoiceProviderCatalog.MyDataKey); var printer = new Printer(); var workflow = Workflow(http,printer);
            workflow.Process(source.Source.Id,source); var original = printer.Bytes; var calls = http.Calls;
            source.Snapshot.PrintContent = Encoding.UTF8.GetBytes("edited"); source.Snapshot.PrintDestination = "different"; source.Source.Invoice.TotalAmount = 999;
            workflow.Process(source.Source.Id,source,true,"Customer requested copy");
            Check(original.SequenceEqual(printer.Bytes),"rendered current data"); Equal("original-printer",printer.Destination); Equal(2,printer.Calls); Equal(calls,http.Calls); Equal(1,source.Loads);
            Equal("Customer requested copy",Prints(source.Source.Id).Last().Reason);
        });
        Test("unknown printer handoff blocks automatic and explicit reprint", () => {
            var source = new WorkflowSource(); var http = new Http(source.Source.Id,InvoiceProviderCatalog.MyDataKey); var printer = new Printer { Uncertain = true }; var workflow = Workflow(http,printer);
            Equal("Unknown",workflow.Process(source.Source.Id,source).Print.State); var calls = http.Calls;
            workflow.Process(source.Source.Id,null); workflow.Process(source.Source.Id,null,true,"please retry"); Equal(1,printer.Calls); Equal(calls,http.Calls);
        });
        Test("confirmed printer reconciliation never emits another copy", () => {
            var source = new WorkflowSource(); var http = new Http(source.Source.Id,InvoiceProviderCatalog.MyDataKey); var printer = new Printer { Uncertain = true }; var workflow = Workflow(http,printer);
            var result = workflow.Process(source.Source.Id,source);
            using(var lease = database.EnterInvoice(source.Source.Id)) new FiscalPrintService(database,printer).Reconcile(lease,result.Print.AttemptId,true,"Operator found original job and printed copy");
            Equal("Confirmed",workflow.Process(source.Source.Id,null).Print.State); Equal(1,printer.Calls);
        });
        Test("proved not-printed reconciliation permits same saved document", () => {
            var source = new WorkflowSource(); var http = new Http(source.Source.Id,InvoiceProviderCatalog.MyDataKey); var printer = new Printer { Uncertain = true }; var workflow = Workflow(http,printer);
            var result = workflow.Process(source.Source.Id,source); var bytes = printer.Bytes; printer.Uncertain = false;
            using(var lease = database.EnterInvoice(source.Source.Id)) new FiscalPrintService(database,printer).Reconcile(lease,result.Print.AttemptId,false,"Spool administrator proves job rejected before printing");
            Equal("HandedOff",workflow.Process(source.Source.Id,null).Print.State); Equal(2,printer.Calls); Check(bytes.SequenceEqual(printer.Bytes),"changed pages"); Equal(1,http.Posts);
        });
        Test("offline printer is a local failure and does not repeat issuance", () => {
            var source = new WorkflowSource(); var http = new Http(source.Source.Id,InvoiceProviderCatalog.MyDataKey); var printer = new Printer { LocalFail = true }; var workflow = Workflow(http,printer);
            Equal("FailedBeforeDispatch",workflow.Process(source.Source.Id,source).Print.State); Equal(0,printer.Calls);
            printer.LocalFail = false; Equal("HandedOff",workflow.Process(source.Source.Id,null).Print.State); Equal(1,http.Posts); Equal(1,source.Loads);
        });
        Test("an authorized reprint retains its reason across a local printer failure", () => {
            var source = new WorkflowSource(); var http = new Http(source.Source.Id,InvoiceProviderCatalog.MyDataKey); var printer = new Printer(); var workflow = Workflow(http,printer);
            workflow.Process(source.Source.Id,source); printer.LocalFail = true;
            Equal("FailedBeforeDispatch",workflow.Process(source.Source.Id,null,true,"Customer copy").Print.State);
            printer.LocalFail = false; var result = workflow.Process(source.Source.Id,null);
            Equal("HandedOff",result.Print.State); Equal("Customer copy",result.Print.Reason); Equal(2,printer.Calls); Equal(1,http.Posts);
        });
        Test("acceptance projection failure recovers acceptance without another send", () => {
            var source = new WorkflowSource(); var http = new Http(source.Source.Id,InvoiceProviderCatalog.MyDataKey); var printer = new Printer(); bool fail = true;
            var workflow = Workflow(http,printer,(id,r) => { if (fail) throw new InvalidOperationException("synthetic projection failure"); });
            Throws(() => workflow.Process(source.Source.Id,source)); Equal(0,printer.Calls); fail = false;
            workflow.Process(source.Source.Id,null); Equal(1,http.Posts); Equal(1,source.Loads); Equal(1,printer.Calls);
        });
        Test("printed projection failure cannot dispatch a duplicate", () => {
            var source = new WorkflowSource(); var http = new Http(source.Source.Id,InvoiceProviderCatalog.MyDataKey); var printer = new Printer(); bool fail = true;
            var workflow = Workflow(http,printer,(id,r) => { if (fail && r.Print != null) throw new InvalidOperationException("printed write failure"); });
            Throws(() => workflow.Process(source.Source.Id,source)); fail = false; workflow.Process(source.Source.Id,null); Equal(1,printer.Calls); Equal(1,http.Posts);
        });
        Test("failed print outcome write leaves Dispatching for reconciliation", () => {
            var source = new WorkflowSource(); var http = new Http(source.Source.Id,InvoiceProviderCatalog.MyDataKey); var printer = new Printer();
            printer.OnDispatch = () => Execute(connection,"CREATE TRIGGER RejectPrintOutcome ON dbo.FiscalAttemptRevision AFTER INSERT AS IF EXISTS(SELECT 1 FROM inserted WHERE State='HandedOff') BEGIN RAISERROR('test failure',16,1); ROLLBACK TRANSACTION; END;");
            var workflow = Workflow(http,printer);
            try { Throws(() => workflow.Process(source.Source.Id,source)); Equal("Dispatching",Prints(source.Source.Id).Last().State); }
            finally { Execute(connection,"DROP TRIGGER RejectPrintOutcome;"); }
            Equal("Unknown",workflow.Process(source.Source.Id,null).Print.State); Equal(1,printer.Calls); Equal(1,http.Posts);
        });
        Test("failed Dispatching write prevents printer call", () => {
            var source = new WorkflowSource(); var http = new Http(source.Source.Id,InvoiceProviderCatalog.MyDataKey); var printer = new Printer();
            Execute(connection,"CREATE TRIGGER RejectPrintDispatch ON dbo.FiscalAttemptRevision AFTER INSERT AS IF EXISTS(SELECT 1 FROM inserted WHERE State='Dispatching') BEGIN RAISERROR('test failure',16,1); ROLLBACK TRANSACTION; END;");
            try { Throws(() => Workflow(http,printer).Process(source.Source.Id,source)); Equal(0,printer.Calls); Equal("Prepared",Prints(source.Source.Id).Last().State); }
            finally { Execute(connection,"DROP TRIGGER RejectPrintDispatch;"); }
            Workflow(http,printer).Process(source.Source.Id,null); Equal(1,printer.Calls); Equal(1,http.Posts);
        });
        Test("workflow snapshot failure precedes every external call", () => {
            var source = new WorkflowSource(); var http = new Http(source.Source.Id,InvoiceProviderCatalog.MyDataKey); var printer = new Printer();
            Execute(connection,"CREATE TRIGGER RejectSnapshot ON dbo.FiscalInvoiceWorkflow AFTER INSERT AS BEGIN RAISERROR('test failure',16,1); ROLLBACK TRANSACTION; END;");
            try { Throws(() => Workflow(http,printer).Process(source.Source.Id,source)); Equal(0,http.Calls); Equal(0,printer.Calls); }
            finally { Execute(connection,"DROP TRIGGER RejectSnapshot;"); }
        });
        Test("unknown submission never prints or sends again", () => {
            var source = new WorkflowSource(); var http = new Http(source.Source.Id,InvoiceProviderCatalog.MyDataKey) { InvoicePost = b => { throw new System.Net.Http.HttpRequestException("timeout"); } }; var printer = new Printer(); var workflow = Workflow(http,printer);
            Check(!workflow.Process(source.Source.Id,source).Accepted,"unknown accepted"); workflow.Process(source.Source.Id,null); Equal(1,http.Posts); Equal(0,printer.Calls); Equal(1,source.Loads);
        });
        Test("reprint is incapable of starting first issuance", () => {
            var source = new WorkflowSource(); var http = new Http(source.Source.Id,InvoiceProviderCatalog.MyDataKey); var printer = new Printer();
            Throws(() => Workflow(http,printer).Process(source.Source.Id,source,true,"copy")); Equal(0,http.Calls); Equal(0,source.Loads);
        });
        Test("Samtec acceptance composes with direct myDATA and signs only once", () => {
            var source = new WorkflowSource(); using(var lease = database.EnterInvoice(source.Source.Id)) source.Prepare(lease);
            source.Snapshot.Signing = new FiscalSigningRequest(Guid.NewGuid(),new FiscalDocumentReference(FiscalDocumentKind.Invoice,source.Source.Id),
                new FiscalSigningScope("fake","device-v1",FiscalEnvironment.Test,"000000000",0),"fake/v1",Encoding.UTF8.GetBytes("signed bytes"));
            var http = new Http(source.Source.Id,InvoiceProviderCatalog.MyDataKey); var printer = new Printer(); var signer = new Signer();
            var workflow = Workflow(http,printer,signer:signer); var result = workflow.Process(source.Source.Id,source);
            Check(result.Accepted,"composed acceptance missing"); Equal("https://example.invalid/device-qr",result.Response.QrCodeUrl);
            workflow.Process(source.Source.Id,null); Equal(1,signer.Calls); Equal(1,http.Posts); Equal(1,printer.Calls);
        });
        Test("snapshot fingerprint detects mutation before dispatch", () => {
            var source = new WorkflowSource(); var http = new Http(source.Source.Id,InvoiceProviderCatalog.MyDataKey); var printer = new Printer(); var workflow = Workflow(http,printer);
            workflow.Process(source.Source.Id,source,false); Execute(connection,"UPDATE dbo.FiscalInvoiceWorkflow SET Payload=0x01 WHERE InvoiceId='" + source.Source.Id + "';");
            try { workflow.Process(source.Source.Id,null); throw new Exception("expected fingerprint error"); } catch(System.IO.InvalidDataException) { }
            Equal(1,http.Posts); Equal(0,printer.Calls);
        });
        NumberingTests();
        foreach(var kind in new[] { FiscalDocumentKind.Balance,FiscalDocumentKind.TankFilling })
        {
            Test(kind + " signing/printing share one document lease and reuse saved pages", () => {
                var doc = new FiscalDocumentReference(kind,Guid.NewGuid()); var signer = new Signer(); var printer = new Printer(); int loads = 0;
                var service = new SignedDocumentWorkflowService(database,new FiscalSigningService(database,s => signer),new FiscalPrintService(database,printer),(s,r) => s.PrintContent);
                Func<InvoiceWorkflowSnapshot> prepare = () => { loads++; return new InvoiceWorkflowSnapshot { InvoiceId = doc.DocumentId,
                    Signing = new FiscalSigningRequest(Guid.NewGuid(),doc,new FiscalSigningScope("fake","device-v1",FiscalEnvironment.Test,"000000000",0),"fake/v1",Encoding.UTF8.GetBytes("sign me")),
                    PrintContent = Encoding.UTF8.GetBytes("original document"),PrintDestination = "saved printer",PrintFormat = "test/v1" }; };
                Equal("HandedOff",service.Process(doc,prepare,r => {}).Print.State);
                service.Process(doc,null,r => {}); service.Process(doc,null,r => {},"operator requested copy");
                Equal(1,signer.Calls); Equal(2,printer.Calls); Equal(1,loads); Equal("original document",Encoding.UTF8.GetString(printer.Bytes));
            });
            Test(kind + " uncertain printing never signs again", () => {
                var doc = new FiscalDocumentReference(kind,Guid.NewGuid()); var signer = new Signer(); var printer = new Printer { Uncertain = true };
                var service = new SignedDocumentWorkflowService(database,new FiscalSigningService(database,s => signer),new FiscalPrintService(database,printer),(s,r) => s.PrintContent);
                var snapshot = new InvoiceWorkflowSnapshot { InvoiceId = doc.DocumentId,
                    Signing = new FiscalSigningRequest(Guid.NewGuid(),doc,new FiscalSigningScope("fake","device-v1",FiscalEnvironment.Test,"000000000",0),"fake/v1",Encoding.UTF8.GetBytes("sign me")),
                    PrintContent = Encoding.UTF8.GetBytes("original document"),PrintDestination = "saved printer",PrintFormat = "test/v1" };
                Equal("Unknown",service.Process(doc,() => snapshot,r => {}).Print.State);
                Equal("Unknown",service.Process(doc,null,r => {},"copy").Print.State); Equal(1,signer.Calls); Equal(1,printer.Calls);
            });
        }
    }
    static void NumberingTests()
    {
        Execute(connection,"CREATE TABLE dbo.InvoiceType(InvoiceTypeId uniqueidentifier PRIMARY KEY,LastNumber int NOT NULL,DefaultSeries nvarchar(255)); CREATE TABLE dbo.Invoice(InvoiceId uniqueidentifier PRIMARY KEY,InvoiceTypeId uniqueidentifier NOT NULL,Number int NOT NULL,Series nvarchar(255),TransactionDate datetime NOT NULL,IsPrinted bit NULL,InvoiceSignature nvarchar(max) NULL,QRCodeData nvarchar(max) NULL); CREATE TABLE dbo.InvoiceLine(InvoiceId uniqueidentifier); CREATE TABLE dbo.MyDataInvoice(InvoiceId uniqueidentifier);");
        Execute(connection,"CREATE TRIGGER TestInvoiceLines ON dbo.Invoice AFTER INSERT AS INSERT dbo.InvoiceLine(InvoiceId) SELECT InvoiceId FROM inserted;");
        Test("manual numbering allocates older invoices first and is idempotent", () => {
            var type = Guid.NewGuid(); var old = Guid.NewGuid(); var recent = Guid.NewGuid();
            Execute(connection,"INSERT dbo.InvoiceType VALUES('" + type + "',10,'A'); INSERT dbo.Invoice(InvoiceId,InvoiceTypeId,Number,Series,TransactionDate) VALUES('" + old + "','" + type + "',0,NULL,'20261014'),('" + recent + "','" + type + "',0,NULL,'20261015');");
            using(var lease = database.EnterInvoice(recent)) { Equal(12,new SqlInvoiceWorkflowStore(database).AllocateNumbers(lease)); Equal(12,new SqlInvoiceWorkflowStore(database).AllocateNumbers(lease)); }
            using(var lease = database.EnterInvoice(old)) Equal(11,new SqlInvoiceWorkflowStore(database).AllocateNumbers(lease));
        });
        Test("concurrent workers share the series counter without duplicate numbers", () => {
            var type = Guid.NewGuid(); var ids = Enumerable.Range(0,8).Select(i => Guid.NewGuid()).ToArray();
            Execute(connection,"INSERT dbo.InvoiceType VALUES('" + type + "',0,'B');" + string.Join("",ids.Select(id => "INSERT dbo.Invoice(InvoiceId,InvoiceTypeId,Number,Series,TransactionDate) VALUES('" + id + "','" + type + "',0,NULL,'20261015');")));
            var jobs = ids.Select(id => Task.Run(() => { using(var lease = database.EnterInvoice(id)) return new SqlInvoiceWorkflowStore(database).AllocateNumbers(lease); })).ToArray();
            Task.WaitAll(jobs); Equal(8,jobs.Select(t => t.Result).Distinct().Count()); Check(jobs.All(t => t.Result >= 1 && t.Result <= 8),"number range");
        });
        Test("numbering failure rolls back both counter and invoice identities", () => {
            var type = Guid.NewGuid(); var id = Guid.NewGuid();
            Execute(connection,"INSERT dbo.InvoiceType VALUES('" + type + "',3,'C'); INSERT dbo.Invoice(InvoiceId,InvoiceTypeId,Number,Series,TransactionDate) VALUES('" + id + "','" + type + "',0,NULL,'20261015');");
            Execute(connection,"CREATE TRIGGER RejectCounter ON dbo.InvoiceType AFTER UPDATE AS BEGIN RAISERROR('test failure',16,1); ROLLBACK TRANSACTION; END;");
            try { using(var lease = database.EnterInvoice(id)) Throws(() => new SqlInvoiceWorkflowStore(database).AllocateNumbers(lease)); }
            finally { Execute(connection,"DROP TRIGGER RejectCounter;"); }
            using(var lease = database.EnterInvoice(id)) Equal(4,new SqlInvoiceWorkflowStore(database).AllocateNumbers(lease));
        });
        Test("cutover numbering excludes earlier and legacy-issued records", () => {
            var type = Guid.NewGuid(); var old = Guid.NewGuid(); var signed = Guid.NewGuid(); var submitted = Guid.NewGuid(); var current = Guid.NewGuid();
            Execute(connection,"INSERT dbo.InvoiceType VALUES('" + type + "',0,'CUT');" + string.Join("",new[] { old,signed,submitted,current }.Select(id =>
                "INSERT dbo.Invoice(InvoiceId,InvoiceTypeId,Number,Series,TransactionDate) VALUES('" + id + "','" + type + "',0,NULL,'" + (id == old ? "20261013" : id == current ? "20261015" : "20261014") + "');")) +
                "UPDATE dbo.Invoice SET InvoiceSignature='legacy signature' WHERE InvoiceId='" + signed + "'; INSERT dbo.MyDataInvoice VALUES('" + submitted + "');");
            using(var lease = database.EnterInvoice(current)) Equal(1,new SqlInvoiceWorkflowStore(database).AllocateNumbers(lease,new DateTime(2026,10,14)));
            using(var c = new System.Data.SqlClient.SqlConnection(connection)) using(var cmd = c.CreateCommand())
            {
                c.Open(); cmd.CommandText = "SELECT SUM(Number) FROM dbo.Invoice WHERE InvoiceTypeId=@type AND InvoiceId<>@current";
                cmd.Parameters.AddWithValue("@type",type); cmd.Parameters.AddWithValue("@current",current); Equal(0,Convert.ToInt32(cmd.ExecuteScalar()));
            }
        });
    }
    static void PrinterFallbackTests()
    {
        var installed = new[] { "Type thermal","Installation default","Windows default" };
        var defaultProfile = new SharedPrinterProfile { PrinterName = "Installation default" };
        Test("installed invoice printer takes precedence over installation and Windows defaults", () => {
            var profile = SharedInvoicePrinting.ResolveProfile(new SharedPrinterProfile { PrinterName = " type THERMAL " },defaultProfile,installed,"Windows default");
            Equal("Type thermal",profile.PrinterName);
        });
        Test("blank missing and legacy folder printers fall back to the installation default", () => {
            foreach (var name in new[] { null,"","80 Printer",@"C:\ASFuelControl\Sign" })
            {
                var profile = SharedInvoicePrinting.ResolveProfile(new SharedPrinterProfile { PrinterName = name },defaultProfile,installed,"Windows default");
                Equal("Installation default",profile.PrinterName);
            }
        });
        Test("unavailable installation printer falls back to Windows default", () => {
            var profile = SharedInvoicePrinting.ResolveProfile(new SharedPrinterProfile { PrinterName = "80 Printer" },
                new SharedPrinterProfile { PrinterName = "old installation printer" },installed,"Windows default");
            Equal("Windows default",profile.PrinterName);
        });
        Test("undefined printer profiles use Windows default with standard page settings", () => {
            var profile = SharedInvoicePrinting.ResolveProfile(null,null,installed,"Windows default");
            Equal("Windows default",profile.PrinterName); Equal(315,profile.WidthHundredths); Equal(1100,profile.HeightHundredths); Equal((short)1,profile.Copies);
            SharedInvoicePrinting.CheckProfile(new SharedPrinterProfile()); // Blank selection is valid when saving settings.
        });
        Test("fallback after printer removal preserves saved destination geometry copies and font", () => {
            var saved = new SharedPrinterProfile { PrinterName = "Type thermal",WidthHundredths = 825,HeightHundredths = 1169,Copies = 2,FontSize = 10 };
            var original = JsonConvert.SerializeObject(saved); var defaults = JsonConvert.SerializeObject(defaultProfile);
            var profile = SharedInvoicePrinting.ResolveProfile(saved,defaultProfile,new[] { "Installation default","Windows default" },"Windows default");
            Equal("Installation default",profile.PrinterName); Equal(825,profile.WidthHundredths); Equal(1169,profile.HeightHundredths);
            Equal((short)2,profile.Copies); Equal(10f,profile.FontSize);
            Equal(original,JsonConvert.SerializeObject(saved)); Equal(defaults,JsonConvert.SerializeObject(defaultProfile));
        });
        Test("printer fallback requires a valid selection or default instead of choosing an arbitrary printer", () => {
            Equal(null,SharedInvoicePrinting.ResolvePrinterName("old","missing",new[] { "unrelated printer" },null));
            Throws(() => SharedInvoicePrinting.ResolveProfile(null,null,new string[0],null));
            Equal("Installation default",SharedInvoicePrinting.ResolveProfile(null,defaultProfile,installed,null).PrinterName);
        });
        Test("printer fallback does not conceal invalid page or copy settings", () => {
            Throws(() => SharedInvoicePrinting.ResolveProfile(new SharedPrinterProfile { Copies = 0 },defaultProfile,installed,"Windows default"));
            Throws(() => SharedInvoicePrinting.ResolveProfile(new SharedPrinterProfile { WidthHundredths = 0 },defaultProfile,installed,"Windows default"));
        });
    }
}
