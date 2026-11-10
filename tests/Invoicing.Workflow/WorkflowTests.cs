using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ASFuelControl.Invoicing;
using ASFuelControl.Invoicing.Persistence;
using ASFuelControl.Windows.Threads;
using Exedron.Invoicing.Core;
using Exedron.Invoicing.Adapters.Samtec;
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
        public Signer(string signerKey = "fake")
        {
            Capabilities = new FiscalSignerCapabilities(signerKey,
                new[] { FiscalDocumentKind.Invoice,FiscalDocumentKind.Balance,FiscalDocumentKind.TankFilling });
        }
        public FiscalSignerCapabilities Capabilities { get; }
        public FiscalSigningResult Sign(FiscalSigningRequest request) { Calls++; return FiscalSigningResult.Signed(request.AttemptId,"real-signature","https://example.invalid/device-qr",null,"accepted"); }
    }
    sealed class RejectThenSign : IFiscalSigner
    {
        public int Calls; public readonly List<byte[]> Payloads = new List<byte[]>();
        public FiscalSignerCapabilities Capabilities { get; } = new FiscalSignerCapabilities(
            SamtecFiscalSigner.Key,new[] { FiscalDocumentKind.Invoice });
        public FiscalSigningResult Sign(FiscalSigningRequest request)
        {
            Calls++; Payloads.Add(request.Payload);
            return Calls == 1
                ? FiscalSigningResult.Rejected(request.AttemptId,"synthetic rejection","calculation rejected")
                : FiscalSigningResult.Signed(request.AttemptId,"corrected-signature",null,null,"accepted corrected request");
        }
    }
    sealed class AmountMismatchSigner : IFiscalSigner
    {
        public int Calls; public bool AlwaysReject;
        public readonly List<byte[]> Payloads = new List<byte[]>();
        public FiscalSignerCapabilities Capabilities { get; } = new FiscalSignerCapabilities(
            SamtecFiscalSigner.Key,new[] { FiscalDocumentKind.Invoice });
        public FiscalSigningResult Sign(FiscalSigningRequest request)
        {
            Calls++; Payloads.Add(request.Payload);
            if (AlwaysReject || Calls == 1)
                return FiscalSigningResult.Rejected(request.AttemptId,
                    "{\"JobType\":\"Sign\",\"InvoiceUID\":\"" + request.Document.DocumentId +
                    "\",\"ResultCode\":\"Error019,ReplyCodeError,137,89,Amounts of JSON transaction line does not match,89/D0/02\"}",
                    "The device explicitly rejected the matching signing request: Error019");
            return FiscalSigningResult.Signed(request.AttemptId,"corrected-signature",null,null,
                "accepted corrected request");
        }
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
        Func<InvoiceWorkflowSnapshot,Exedron.Invoicing.Core.Models.InvoiceResponse,byte[]> render = null, IFiscalSigner signer = null,
        Func<ProviderRoute,ProviderRoute> currentConnection = null, Credentials credentials = null, IMyDieselRedemptionCoordinator myDiesel = null) =>
        new InvoiceWorkflowService(database, Service(http,credentials), new ProviderPaymentService(database,credentials ?? new Credentials(), r => http),
            new FiscalSigningService(database,s => signer), new FiscalPrintService(database,printer),
            render ?? ((s,r) => Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(s.PrintContent) + " MARK=" + r.Mark)), project ?? ((id,r) => {}), currentConnection,myDiesel);
    static FiscalOperationRecord[] Prints(Guid id) { using(var lease = database.EnterInvoice(id)) return new SqlFiscalOperationStore(database).Load(lease,true).ToArray(); }
    static void WorkflowTests()
    {
        Test("provider URL projection does not overwrite the verification hash", () => {
            var row = new ASFuelControl.Data.MyDataInvoice { VerificationHash = "verification-hash" };
            row.ProviderUrl = "https://provider.invalid/invoices/123";
            Equal("verification-hash",row.VerificationHash);
            Equal("https://provider.invalid/invoices/123",row.ProviderUrl);
        });
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
        Test("iTimologia operator-authorized invoice resend reuses the approved POS payment", () => {
            var key = InvoiceProviderCatalog.ITimologiaKey;
            var source = new WorkflowSource(); source.Source.Invoice.PaymentType = 2;
            using(var lease = database.EnterInvoice(source.Source.Id)) source.Prepare(lease);
            source.Snapshot.Provider = Route(key);
            source.Snapshot.Invoice = source.Source.Prepare(source.Source.Id,source.Snapshot.Provider);
            source.Snapshot.Payment = PaymentRoute.ForMode(source.Snapshot.Provider,"http://gateway.invalid:1234","TID-1","EDPS");
            var http = new Http(source.Source.Id,key) { InvoicePost = b => { throw new System.Net.Http.HttpRequestException("timeout"); } };
            http.Lookup = () => Reply("{}",404);
            var gateway = new Gateway(); var printer = new Printer(); var credentials = new Credentials();
            var workflow = new InvoiceWorkflowService(database,Service(http,credentials),
                new ProviderPaymentService(database,credentials,r => http,r => gateway),null,
                new FiscalPrintService(database,printer),(s,r) => s.PrintContent,(id,r) => {});
            var unknown = workflow.Process(source.Source.Id,source,false);
            Equal("Approved",unknown.PaymentState); Equal("Unknown",unknown.FiscalState); Equal(1,gateway.Calls); Equal(1,http.Posts);
            Service(http,credentials).AuthorizeResend(source.Source.Id,"synthetic-operator","confirmed duplicate risk");
            http.InvoicePost = b => Reply(TimReply(source.Source.Id));
            var completed = workflow.Process(source.Source.Id,null,false,retryRejected:true);
            Check(completed.Accepted,"authorized provider resend did not finish");
            Equal(1,gateway.Calls); Equal(2,http.Posts); Equal(0,printer.Calls);
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
        foreach (string providerKey in new[] {
            InvoiceProviderCatalog.ITimologiaKey,
            InvoiceProviderCatalog.DirectIlydaKey })
        {
            Test(providerKey + " replaces legacy fiscal evidence only after provider acceptance", () => {
                var source = new WorkflowSource();
                Guid invoiceId = source.Source.Id;
                ProviderRoute provider = Route(providerKey);
                InvoiceBuildRequest invoiceRequest =
                    source.Source.Prepare(invoiceId, provider);
                source.Snapshot = new InvoiceWorkflowSnapshot {
                    InvoiceId = invoiceId,
                    Provider = provider,
                    Invoice = invoiceRequest,
                    PrintDestination = "unused",
                    PrintFormat = "test/v1",
                    PrintContent = Encoding.UTF8.GetBytes("already printed"),
                    LegacyFiscalReplacement =
                        new LegacyFiscalEvidenceReplacement {
                            PreviousInvoiceSignature = "legacy-device-signature",
                            PreviousQrCode = "https://device.invalid/legacy-qr",
                            AuthorizedBy = "administrator",
                            AuthorizedUtc = new System.DateTime(2026, 11, 8,
                                10, 0, 0, System.DateTimeKind.Utc),
                            Reason = "Synthetic provider recovery"
                        }
                };
                var http = new Http(invoiceId, providerKey);
                var printer = new Printer();
                var signer = new Signer();
                int projectionCalls = 0;
                InvoiceWorkflowService workflow = Workflow(http, printer,
                    project: (id, projectedResult) =>
                    {
                        projectionCalls++;
                    }, signer: signer);
                InvoiceWorkflowResult result = workflow.Process(invoiceId,
                    source, false);
                Check(result.Accepted, "provider recovery was not accepted");
                string expectedSignature = providerKey ==
                    InvoiceProviderCatalog.ITimologiaKey
                    ? "PROVIDER-SIGNATURE"
                    : "HASH";
                Equal(expectedSignature, result.Response.InvoiceSignature);
                Equal("https://example.invalid/qr",
                    result.Response.QrCodeUrl);
                Equal(1, http.Posts);
                Equal(0, signer.Calls);
                Equal(0, printer.Calls);
                Equal(1, projectionCalls);
                using (var lease = database.EnterInvoice(invoiceId))
                {
                    byte[] workflowBytes =
                        new SqlInvoiceWorkflowStore(database).Load(lease);
                    string workflowJson = Encoding.UTF8.GetString(
                        workflowBytes);
                    InvoiceWorkflowSnapshot saved =
                        JsonConvert.DeserializeObject<InvoiceWorkflowSnapshot>(
                            workflowJson);
                    Equal("legacy-device-signature",
                        saved.LegacyFiscalReplacement
                            .PreviousInvoiceSignature);
                    Equal("https://device.invalid/legacy-qr",
                        saved.LegacyFiscalReplacement.PreviousQrCode);
                }
            });
        }
        Test("provider evidence projection overwrites the legacy signature and QR together", () => {
            var invoice = new ASFuelControl.Data.Invoice {
                InvoiceSignature = "legacy-device-signature",
                QRCodeData = "https://device.invalid/legacy-qr"
            };
            var response =
                new Exedron.Invoicing.Core.Models.InvoiceResponse {
                    InvoiceSignature = "provider-signature",
                    QrCodeUrl = "https://provider.invalid/qr"
                };
            InvoiceProviderEvidenceProjection.Apply(invoice, response, null);
            Equal("provider-signature", invoice.InvoiceSignature);
            Equal("https://provider.invalid/qr", invoice.QRCodeData);
        });
        Test("legacy fiscal evidence remains projected when the provider omits its signature or QR", () => {
            var source = new WorkflowSource();
            Guid invoiceId = source.Source.Id;
            ProviderRoute provider = Route(
                InvoiceProviderCatalog.ITimologiaKey);
            InvoiceBuildRequest invoiceRequest =
                source.Source.Prepare(invoiceId, provider);
            source.Snapshot = new InvoiceWorkflowSnapshot {
                InvoiceId = invoiceId,
                Provider = provider,
                Invoice = invoiceRequest,
                PrintDestination = "unused",
                PrintFormat = "test/v1",
                PrintContent = Encoding.UTF8.GetBytes("already printed"),
                LegacyFiscalReplacement =
                    new LegacyFiscalEvidenceReplacement {
                        PreviousInvoiceSignature = "legacy-device-signature",
                        PreviousQrCode = "https://device.invalid/legacy-qr",
                        AuthorizedBy = "administrator",
                        AuthorizedUtc = System.DateTime.UtcNow,
                        Reason = "Synthetic incomplete response"
                    }
            };
            var http = new Http(invoiceId,
                InvoiceProviderCatalog.ITimologiaKey) {
                InvoicePost = body => Reply(
                    "{\"data\":{\"ext_system_id\":\"" +
                    invoiceId.ToString("N") +
                    "\",\"uid\":\"UID\",\"mark\":\"900010\"}}")
            };
            int projectionCalls = 0;
            var printer = new Printer();
            InvoiceWorkflowService workflow = Workflow(http, printer,
                project: (id, projectedResult) =>
                {
                    projectionCalls++;
                });
            Throws(() => workflow.Process(invoiceId, source, false));
            Equal(1, http.Posts);
            Equal(0, projectionCalls);
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
        Test("definitive provider rejection starts a new attempt with regenerated fiscal identity", () =>
        {
            var source = new Source();
            Guid invoiceId = source.Id;
            ProviderRoute route = Route(InvoiceProviderCatalog.ITimologiaKey);
            var http = new Http(invoiceId, InvoiceProviderCatalog.ITimologiaKey)
            {
                InvoicePost = body => Reply(
                    "{\"errors\":[{\"message\":\"Synthetic validation rejection\"}]}",
                    422)
            };
            ProviderSubmissionService service = Service(http);
            ProviderSubmissionResult rejected = service.Send(invoiceId, route,
                source);
            Equal("Rejected", rejected.State);

            source.Invoice.Number += 1;
            http.InvoicePost = body =>
            {
                string responseJson = TimReply(invoiceId);
                HttpResponseMessage response = Reply(responseJson);
                return response;
            };
            ProviderSubmissionResult accepted = service.Send(invoiceId, route,
                source, true);
            Check(accepted.Accepted,
                "the regenerated attempt after a definitive rejection was blocked");
            Equal(2, http.Posts);
            FiscalAttemptEvidence[] attempts = History(invoiceId);
            Equal(2, attempts.Length);
        });
        Test("composed workflow replaces a definitively rejected invoice snapshot", () =>
        {
            var source = new WorkflowSource();
            Guid invoiceId = source.Source.Id;
            ProviderRoute route = Route(InvoiceProviderCatalog.ITimologiaKey);
            InvoiceBuildRequest originalRequest = source.Source.Prepare(invoiceId,
                route);
            source.Snapshot = new InvoiceWorkflowSnapshot
            {
                InvoiceId = invoiceId,
                Provider = route,
                Invoice = originalRequest,
                PrintDestination = "unused",
                PrintFormat = "test/v1",
                PrintContent = Encoding.UTF8.GetBytes("rejected invoice")
            };
            var http = new Http(invoiceId, InvoiceProviderCatalog.ITimologiaKey)
            {
                InvoicePost = body => Reply(
                    "{\"errors\":[{\"message\":\"Synthetic validation rejection\"}]}",
                    422)
            };
            var printer = new Printer();
            InvoiceWorkflowService workflow = Workflow(http, printer);
            InvoiceWorkflowResult rejected = workflow.Process(invoiceId, source,
                false);
            Equal("Rejected", rejected.FiscalState);

            source.Snapshot.Invoice.Invoice.Number += 1;
            source.Snapshot.PrintContent = Encoding.UTF8.GetBytes(
                "regenerated invoice");
            http.InvoicePost = body =>
            {
                string responseJson = TimReply(invoiceId);
                HttpResponseMessage response = Reply(responseJson);
                return response;
            };
            InvoiceWorkflowResult accepted = workflow.Process(invoiceId, source,
                false, retryRejected: true);
            Check(accepted.Accepted,
                "the composed workflow retained the definitively rejected snapshot");

            using (var lease = database.EnterInvoice(invoiceId))
            {
                var store = new SqlInvoiceWorkflowStore(database);
                byte[] savedBytes = store.Load(lease);
                string savedJson = Encoding.UTF8.GetString(savedBytes);
                var saved = JsonConvert.DeserializeObject<InvoiceWorkflowSnapshot>(
                    savedJson);
                Equal(43, saved.Invoice.Invoice.Number);
                string printContent = Encoding.UTF8.GetString(saved.PrintContent);
                Equal("regenerated invoice", printContent);
            }
        });
        Test("audited issue-date reissue may replace a definitively rejected provider identity", () => {
            var source=new WorkflowSource();var id=source.Source.Id;var route=Route("iTimologia");
            source.Snapshot=new InvoiceWorkflowSnapshot { InvoiceId=id,Provider=route,
                Invoice=source.Source.Prepare(id,route),PrintDestination="unused",PrintFormat="test/v1",
                PrintContent=Encoding.UTF8.GetBytes("old invoice") };
            var http=new Http(id,"iTimologia") { InvoicePost=body=>Reply(
                "{\"message\":\"IssueDate is invalid, it must be equal with current date\"}",452) };
            var workflow=Workflow(http,new Printer());
            Equal("Rejected",workflow.Process(id,source,false).FiscalState);
            Check(ProviderSubmissionService.IsIssueDateMismatchRejection(Latest(id)),
                "provider-neutral current issue-date rejection was not recognized");
            byte[] original,replacement;
            using(var lease=database.EnterInvoice(id))
            {
                var store=new SqlInvoiceWorkflowStore(database);original=store.Load(lease);
                var snapshot=JsonConvert.DeserializeObject<InvoiceWorkflowSnapshot>(Encoding.UTF8.GetString(original));
                var old=snapshot.Invoice.Invoice;var oldModel=new InvoiceBuilder(new InvoiceTypeResolver()).Build(snapshot.Invoice);
                var oldNumber=old.Number;var oldDate=old.PrintedDateTime;
                old.Number++;old.PrintedDateTime=oldDate.AddDays(1);
                snapshot.PrintContent=Encoding.UTF8.GetBytes("new invoice");
                snapshot.IssueDateReissues=new System.Collections.Generic.List<InvoiceIssueDateReissue> {
                    new InvoiceIssueDateReissue { PreviousNumber=oldNumber,PreviousSeries=old.Series,
                        PreviousIssueDate=oldDate,Number=old.Number,Series=old.Series,
                        IssueDate=old.PrintedDateTime,InvoiceType=oldModel.InvoiceHeader.InvoiceType,
                        ChangedBy="test",ChangedUtc=DateTime.UtcNow,Reason="date mismatch" } };
                replacement=Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(snapshot));
                store.ReplaceAfterIssueDateReissue(lease,original,replacement);
            }
            http.InvoicePost=body=>Reply(TimReply(id));
            Check(workflow.Process(id,null,false,retryRejected:true).Accepted,"reissued identity was not accepted");
            Equal(2,http.Posts);
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
        Test("laser fiscal line keeps only provider and MARK on one line", () => {
            var expectedProviderLines = new Dictionary<string,string> {
                { InvoiceProviderCatalog.MyDataKey,"MyData" },
                { InvoiceProviderCatalog.ITimologiaKey,"Πάροχος: iTimologia" },
                { InvoiceProviderCatalog.DirectIlydaKey,"Πάροχος: Ilyda" }
            };
            foreach (var expectedProviderLine in expectedProviderLines)
            {
                var snapshot = new InvoiceWorkflowSnapshot {
                    Provider = Route(expectedProviderLine.Key)
                };
                var response = new Exedron.Invoicing.Core.Models.InvoiceResponse {
                    Mark = "900010",Uid = "UID",VerificationHash = "HASH",
                    ProviderInvoiceId = "PROVIDER-ID",
                    ProviderUrl = "https://example.invalid/provider",
                    QrCodeUrl = "https://example.invalid/qr",
                    InvoiceSignature = "SIGNATURE"
                };
                string footer = SharedInvoicePrinting.LaserFiscalLine(snapshot,response);
                Equal(expectedProviderLine.Value + "  MARK: 900010",footer);
                bool containsCarriageReturn = footer.Contains("\r");
                bool containsLineFeed = footer.Contains("\n");
                Check(!containsCarriageReturn && !containsLineFeed,
                    "Laser fiscal line wrapped onto multiple lines");
                bool containsUid = footer.Contains("UID");
                bool containsHash = footer.Contains("HASH");
                bool containsProviderId = footer.Contains("PROVIDER-ID");
                bool containsSignature = footer.Contains("SIGNATURE");
                bool containsUrl = footer.Contains("https://");
                Check(!containsUid && !containsHash && !containsProviderId &&
                    !containsSignature && !containsUrl,
                    "Laser fiscal line retained verbose evidence");
            }
            var localSnapshot = new InvoiceWorkflowSnapshot();
            var localResponse = new Exedron.Invoicing.Core.Models.InvoiceResponse();
            string localFooter = SharedInvoicePrinting.LaserFiscalLine(localSnapshot,
                localResponse);
            Equal("",localFooter);
        });
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
            Check(InvoiceProviderCatalog.Get("myDATA").SupportsInvoiceMarkCancellation,
                "direct myDATA must expose ERP CancelInvoice");
            Check(!ilyda.SupportsInvoiceMarkCancellation && !tim.SupportsInvoiceMarkCancellation,
                "provider-issued financial MARKs must not be routed to ERP CancelInvoice");
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
            using(var stream = new System.IO.MemoryStream(pages[0])) using(var image = System.Drawing.Image.FromStream(stream)) { Equal(639,image.Width); Equal(2233,image.Height); }
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"render-preview.png"),pages[0]);
        });
        Test("thermal invoices print the optional company QR beside the fiscal QR", () => {
            var snapshot = new InvoiceWorkflowSnapshot { InvoiceId = Guid.NewGuid(),
                PrintDestination = JsonConvert.SerializeObject(new SharedPrinterProfile { PrinterName = "test-only",WidthHundredths = 315,HeightHundredths = 1100 }),
                CompanyQrContent = "1;STORE-01;AMDIKA-001;2026-10-31;12:45:00;TIM;57270;A;1;30;Diesel;2.079;94.56;196.59;57270",
                PrintContent = Encoding.UTF8.GetBytes(string.Join("\r\n",new[] {
                    "FISCAL INVOICE", "Date: 31/10/2026  12:45", "Invoice: TIM-57270", "Customer: SAMPLE CUSTOMER",
                    "----------------------------------------", "Description       Qty    Price    Total", "Diesel           94.56   2.079   196.59",
                    "----------------------------------------", "Net amount:                    158.54", "VAT 24%:                        38.05",
                    "TOTAL:                         EUR 196.59", "Payment: Credit", "", "Thank you"
                })) };
            var bytes = SharedInvoicePrinting.Render(snapshot,new Exedron.Invoicing.Core.Models.InvoiceResponse {
                Mark = "40000523", InvoiceSignature = "SAMPLE-FISCAL-SIGNATURE",
                QrCodeUrl = "https://example.invalid/invoice/57270"
            });
            var pages = JsonConvert.DeserializeObject<byte[][]>(Encoding.UTF8.GetString(bytes)); Equal(1,pages.Length);
            using(var stream = new System.IO.MemoryStream(pages[0])) using(var image = new System.Drawing.Bitmap(stream)) {
                int dark = 0;
                for(int y=200;y<650;y+=2) for(int x=10;x<image.Width-10;x+=2) {
                    var pixel = image.GetPixel(x,y); if(pixel.R < 80 && pixel.G < 80 && pixel.B < 80) dark++;
                }
                Check(dark > 2500,"The fiscal and company QR codes were not rendered");
                for(int y=0;y<image.Height;y+=2)
                    for(int x=image.Width-SharedInvoicePrinting.ThermalContentMarginPixels;x<image.Width;x++)
                    {
                        var pixel = image.GetPixel(x,y);
                        Check(pixel.R > 245 && pixel.G > 245 && pixel.B > 245,"Thermal content entered the right-edge safety gutter");
                    }
            }
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"thermal-layout-preview.png"),pages[0]);
        });
        Test("laser renderer uses the legacy A5 landscape paper option instead of thermal dimensions", () => {
            var snapshot = new InvoiceWorkflowSnapshot { InvoiceId = Guid.NewGuid(),
                PrintDestination = JsonConvert.SerializeObject(new SharedPrinterProfile { PrinterName = "test-only",Thermal = false,
                    NonThermalPaperSize = "A5",WidthHundredths = 315,HeightHundredths = 1100 }),
                PrintContent = Encoding.UTF8.GetBytes("A5 LASER TEST") };
            var bytes = SharedInvoicePrinting.Render(snapshot,new Exedron.Invoicing.Core.Models.InvoiceResponse());
            var pages = JsonConvert.DeserializeObject<byte[][]>(Encoding.UTF8.GetString(bytes)); Equal(1,pages.Length);
            using(var stream = new System.IO.MemoryStream(pages[0])) using(var image = System.Drawing.Image.FromStream(stream))
            { Equal(1240,image.Width); Equal(874,image.Height); }
        });
        Test("laser profiles delegate to the full invoice report renderer", () => {
            var snapshot = new InvoiceWorkflowSnapshot { InvoiceId = Guid.NewGuid(),
                PrintDestination = JsonConvert.SerializeObject(new SharedPrinterProfile { PrinterName = "test-only",Thermal = false,
                    NonThermalPaperSize = "A4" }), PrintContent = Encoding.UTF8.GetBytes("thermal text must not be rendered") };
            int calls = 0;
            var expected = Encoding.UTF8.GetBytes("full invoice report pages");
            var actual = SharedInvoicePrinting.Render(snapshot,new Exedron.Invoicing.Core.Models.InvoiceResponse(),(saved,response,profile) => {
                calls++;
                Equal(snapshot.InvoiceId,saved.InvoiceId);
                Check(!profile.Thermal,"Laser renderer received a thermal profile");
                return expected;
            });
            Equal(1,calls);
            Check(actual.SequenceEqual(expected),"The thermal text renderer replaced the laser invoice report");
        });
        Test("thermal profiles delegate to the configured narrow invoice report renderer", () => {
            var snapshot = new InvoiceWorkflowSnapshot { InvoiceId = Guid.NewGuid(),
                PrintDestination = JsonConvert.SerializeObject(new SharedPrinterProfile { PrinterName = "test-only",Thermal = true }),
                PrintContent = Encoding.UTF8.GetBytes("generic thermal text must not be rendered") };
            int calls = 0;
            var expected = Encoding.UTF8.GetBytes("narrow invoice report pages");
            var actual = SharedInvoicePrinting.Render(snapshot,new Exedron.Invoicing.Core.Models.InvoiceResponse(),(saved,response,profile) => {
                calls++;
                Equal(snapshot.InvoiceId,saved.InvoiceId);
                Check(profile.Thermal,"The narrow report renderer did not receive the thermal profile");
                return expected;
            });
            Equal(1,calls);
            Check(actual.SequenceEqual(expected),"The generic text receipt replaced the configured narrow invoice report");
        });
        Test("laser printing selects the driver A5 paper and rejects silent A4 fallback", () => {
            var a4 = new System.Drawing.Printing.PaperSize("A4 driver paper",827,1169) {
                RawKind = (int)System.Drawing.Printing.PaperKind.A4 };
            var a5 = new System.Drawing.Printing.PaperSize("EPSON A5 148 x 210 mm",583,827) {
                RawKind = (int)System.Drawing.Printing.PaperKind.A5 };
            var profile = new SharedPrinterProfile { Thermal = false,NonThermalPaperSize = "A5" };
            var selected = SharedInvoicePrinting.ResolvePaperSize(profile,new[] { a4,a5 });
            Equal((int)System.Drawing.Printing.PaperKind.A5,selected.RawKind);
            Equal("EPSON A5 148 x 210 mm",selected.PaperName);
            Throws(() => SharedInvoicePrinting.ResolvePaperSize(profile,new[] { a4 }));
        });
        Test("A5 spool geometry follows the landscape page when a driver reports portrait printable bounds", () => {
            var portrait = new System.Drawing.RectangleF(12,18,559,803);
            var normalized = SharedInvoicePrinting.NormalizePrintableArea(portrait,1240,874);
            Equal(18f,normalized.X); Equal(12f,normalized.Y);
            Equal(803f,normalized.Width); Equal(559f,normalized.Height);
            var thermal = SharedInvoicePrinting.NormalizePrintableArea(portrait,472,1650);
            Equal(portrait,thermal);
            var safeThermal = SharedInvoicePrinting.ApplyThermalSafetyInset(new System.Drawing.RectangleF(0,0,315,1100),true);
            Equal(8f,safeThermal.X); Equal(299f,safeThermal.Width); Equal(1100f,safeThermal.Height);
            Equal(portrait,SharedInvoicePrinting.ApplyThermalSafetyInset(portrait,false));
        });
        Test("thermal spool paper uses the fixed configured height", () => {
            byte[] bytes;
            using (var bitmap = new System.Drawing.Bitmap(300,600))
            using (var stream = new System.IO.MemoryStream())
            {
                bitmap.Save(stream,System.Drawing.Imaging.ImageFormat.Png);
                bytes = stream.ToArray();
            }
            var paper = SharedInvoicePrinting.ResolveRenderedThermalPaperSize(
                new SharedPrinterProfile { Thermal = true,WidthHundredths = 315,HeightHundredths = 1100 },bytes);
            Equal(315,paper.Width);
            Equal(1100,paper.Height);
        });
        Test("thermal page encoding retains RGB and the configured DPI", () => {
            byte[] encodedPage;
            using (var bitmap = new System.Drawing.Bitmap(640,480,
                System.Drawing.Imaging.PixelFormat.Format24bppRgb))
            {
                bitmap.SetResolution(203,203);
                using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
                using (var font = new System.Drawing.Font("Tahoma",12))
                {
                    graphics.Clear(System.Drawing.Color.White);
                    graphics.DrawString("ΔΟΚΙΜΗ ΘΕΡΜΙΚΗΣ ΕΚΤΥΠΩΣΗΣ",
                        font,System.Drawing.Brushes.Black,20,20);
                    graphics.FillRectangle(System.Drawing.Brushes.Black,
                        20,80,300,300);
                }
                int insetPixels = SharedInvoicePrinting
                    .ThermalSafetyInsetPixels(203);
                Equal(16,insetPixels);
                encodedPage = SharedInvoicePrinting.EncodePagePng(bitmap,
                    insetPixels);
            }
            using (var stream = new System.IO.MemoryStream(encodedPage))
            using (var image = new System.Drawing.Bitmap(stream))
            {
                Equal(System.Drawing.Imaging.PixelFormat.Format24bppRgb,
                    image.PixelFormat);
                float dpiDifference = Math.Abs(203f -
                    image.HorizontalResolution);
                Check(dpiDifference < 0.1f,
                    "The RGB PNG did not preserve the thermal DPI.");
            }
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
        Test("explicit reprint copies original pages and uses the freshly resolved destination", () => {
            var source = new WorkflowSource(); var http = new Http(source.Source.Id,InvoiceProviderCatalog.MyDataKey); var printer = new Printer(); var workflow = Workflow(http,printer);
            workflow.Process(source.Source.Id,source); var original = printer.Bytes; var calls = http.Calls;
            source.Snapshot.PrintContent = Encoding.UTF8.GetBytes("edited"); source.Snapshot.PrintDestination = "different"; source.Source.Invoice.TotalAmount = 999;
            var request = "ASFC-REPRINT-V1:" + JsonConvert.SerializeObject(new { Reason = "Customer requested copy",PrintDestination = "corrected-printer" });
            workflow.Process(source.Source.Id,source,true,request);
            Check(original.SequenceEqual(printer.Bytes),"rendered current data"); Equal("corrected-printer",printer.Destination); Equal(2,printer.Calls); Equal(calls,http.Calls); Equal(1,source.Loads);
            Equal("Customer requested copy",Prints(source.Source.Id).Last().Reason);
        });
        Test("an unfinished first print can use an operator-selected printer without becoming a reprint", () => {
            var source = new WorkflowSource(); var http = new Http(source.Source.Id,InvoiceProviderCatalog.MyDataKey); var printer = new Printer(); var workflow = Workflow(http,printer);
            workflow.Process(source.Source.Id,source,false);
            workflow.Process(source.Source.Id,null,true,null,false,false,"operator-selected-printer");
            Equal("operator-selected-printer",printer.Destination);
            Equal(null,Prints(source.Source.Id).Last().Reason);
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
        Test("unknown submission requires operator confirmation before resend", () =>
        {
            var source = new WorkflowSource();
            Guid invoiceId = source.Source.Id;
            var http = new Http(invoiceId, InvoiceProviderCatalog.MyDataKey)
            {
                InvoicePost = body =>
                {
                    throw new System.Net.Http.HttpRequestException("timeout");
                }
            };
            var printer = new Printer();
            InvoiceWorkflowService workflow = Workflow(http, printer);
            InvoiceWorkflowResult unknown = workflow.Process(invoiceId, source);
            Check(!unknown.Accepted, "unknown accepted");

            InvoiceWorkflowResult recovered = workflow.Process(invoiceId, null);
            Equal("Unknown", recovered.FiscalState);
            Equal(1, http.Posts);
            Equal(0, printer.Calls);
            Equal(1, source.Loads);

            ProviderSubmissionService service = Service(http);
            ProviderResendAssessment assessment = service.GetResendAssessment(invoiceId);
            Check(assessment.CanResend && assessment.RequiresAuthorization,
                "the unknown provider result did not offer an explicit resend");
            service.AuthorizeResend(invoiceId, "synthetic-operator",
                "confirmed duplicate risk");
            http.InvoicePost = body => Reply(MyDataReply);
            InvoiceWorkflowResult accepted = workflow.Process(invoiceId, null, false,
                retryRejected: true);
            Check(accepted.Accepted,
                "the operator-authorized resend did not complete");
            Equal(2, http.Posts);
            Equal(0, printer.Calls);
        });
        Test("operator-authorized unknown submission blocks changed payload and restores recovery", () =>
        {
            var source = new WorkflowSource();
            Guid invoiceId = source.Source.Id;
            ProviderRoute originalRoute = Route(InvoiceProviderCatalog.ITimologiaKey);
            using (var lease = database.EnterInvoice(invoiceId))
            {
                source.Prepare(lease);
            }
            source.Snapshot.Provider = originalRoute;
            source.Snapshot.Invoice = source.Source.Prepare(invoiceId, originalRoute);
            var http = new Http(invoiceId, InvoiceProviderCatalog.ITimologiaKey)
            {
                InvoicePost = body =>
                {
                    throw new HttpRequestException("timeout");
                }
            };
            http.Lookup = () => Reply("{}", 404);
            var printer = new Printer();
            InvoiceWorkflowService workflow = Workflow(http, printer);
            InvoiceWorkflowResult unknown = workflow.Process(invoiceId, source,
                false);
            Equal("Unknown", unknown.FiscalState);

            ProviderSubmissionService service = Service(http);
            service.AuthorizeResend(invoiceId, "synthetic-operator",
                "change receiver after unknown outcome");
            ProviderRoute newRoute = Route(InvoiceProviderCatalog.MyDataKey);
            source.Snapshot.Provider = newRoute;
            source.Snapshot.Invoice = source.Source.Prepare(invoiceId, newRoute);
            source.Snapshot.Invoice.Invoice.Trader.TaxRegistrationId =
                "BG204985348";
            source.Snapshot.Invoice.Country = new CountryInput
            {
                CountryCode = "BG",
                IsEu = true
            };
            http.InvoicePost = body => Reply(MyDataReply);
            Throws(() => workflow.Process(invoiceId, source, false,
                retryRejected: true));
            Equal(1, http.Posts);
            Equal("Unknown", History(invoiceId).Last().State);
            ProviderSubmissionResult recovered = service.Recover(invoiceId);
            Equal("Unknown", recovered.State);
            Equal(2, http.Gets);

            using (var lease = database.EnterInvoice(invoiceId))
            {
                var store = new SqlInvoiceWorkflowStore(database);
                byte[] savedBytes = store.Load(lease);
                string savedJson = Encoding.UTF8.GetString(savedBytes);
                var saved = JsonConvert.DeserializeObject<InvoiceWorkflowSnapshot>(
                    savedJson);
                Equal(InvoiceProviderCatalog.ITimologiaKey,
                    saved.Provider.ProviderKey);
            }
        });
        Test("receiver change reuses an approved payment without a second charge", () =>
        {
            var source = new WorkflowSource();
            Guid invoiceId = source.Source.Id;
            source.Source.Invoice.PaymentType = 2;
            using (var lease = database.EnterInvoice(invoiceId))
            {
                source.Prepare(lease);
            }
            ProviderRoute originalRoute = Route(InvoiceProviderCatalog.ITimologiaKey);
            source.Snapshot.Provider = originalRoute;
            source.Snapshot.Invoice = source.Source.Prepare(invoiceId, originalRoute);
            source.Snapshot.Payment = PaymentRoute.ForMode(originalRoute,
                "http://gateway.invalid:1234", "TID-1", "EDPS");
            var http = new Http(invoiceId, InvoiceProviderCatalog.ITimologiaKey)
            {
                InvoicePost = body =>
                {
                    throw new HttpRequestException("timeout");
                }
            };
            http.Lookup = () => Reply("{}", 404);
            var gateway = new Gateway();
            var printer = new Printer();
            var credentials = new Credentials();
            var paymentService = new ProviderPaymentService(database, credentials,
                route => http, route => gateway);
            InvoiceWorkflowService workflow = new InvoiceWorkflowService(database,
                Service(http, credentials), paymentService, null,
                new FiscalPrintService(database, printer),
                (snapshot, response) => snapshot.PrintContent,
                (id, result) => { });
            InvoiceWorkflowResult unknown = workflow.Process(invoiceId, source,
                false);
            Equal("Approved", unknown.PaymentState);
            Equal("Unknown", unknown.FiscalState);
            Equal(1, gateway.Calls);

            ProviderSubmissionService service = Service(http, credentials);
            service.AuthorizeResend(invoiceId, "synthetic-operator",
                "change receiver after unknown outcome");
            ProviderRoute newRoute = Route(InvoiceProviderCatalog.MyDataKey);
            source.Snapshot.Provider = newRoute;
            source.Snapshot.Payment = null;
            source.Snapshot.Signing = null;
            source.Snapshot.Invoice = source.Source.Prepare(invoiceId, newRoute);
            http.InvoicePost = body => Reply(MyDataReply);
            InvoiceWorkflowResult accepted = workflow.Process(invoiceId, source,
                false, retryRejected: true);
            Check(accepted.Accepted,
                "the receiver change after an approved payment was blocked");
            Equal(1, gateway.Calls);
            Equal(2, http.Posts);
        });
        foreach (var key in new[] { InvoiceProviderCatalog.ITimologiaKey,InvoiceProviderCatalog.DirectIlydaKey })
            Test(key + " operator-authorized resend continues the frozen workflow", () => {
                var source = new WorkflowSource();
                using(var lease = database.EnterInvoice(source.Source.Id)) source.Prepare(lease);
                source.Snapshot.Provider = Route(key);
                source.Snapshot.Invoice = source.Source.Prepare(source.Source.Id,source.Snapshot.Provider);
                var http = new Http(source.Source.Id,key) { InvoicePost = b => { throw new System.Net.Http.HttpRequestException("timeout"); } };
                if (key == InvoiceProviderCatalog.ITimologiaKey) http.Lookup = () => Reply("{}",404);
                var printer = new Printer(); var workflow = Workflow(http,printer);
                Equal("Unknown",workflow.Process(source.Source.Id,source,false).FiscalState); Equal(1,http.Posts);
                var service = Service(http); Check(service.GetResendAssessment(source.Source.Id).CanResend,"resend was not offered");
                service.AuthorizeResend(source.Source.Id,"synthetic-operator","confirmed duplicate risk");
                http.InvoicePost = b => Reply(key == InvoiceProviderCatalog.ITimologiaKey ? TimReply(source.Source.Id) : IlydaReply);
                Check(workflow.Process(source.Source.Id,null,false,retryRejected:true).Accepted,"authorized resend did not complete");
                Equal(2,http.Posts); Equal(0,printer.Calls); Equal(2,History(source.Source.Id).Length);
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
        Test("signed direct myDATA rejection can explicitly retry submission without signing again", () => {
            var source = new WorkflowSource(); using(var lease = database.EnterInvoice(source.Source.Id)) source.Prepare(lease);
            source.Snapshot.Signing = new FiscalSigningRequest(Guid.NewGuid(),
                new FiscalDocumentReference(FiscalDocumentKind.Invoice,source.Source.Id),
                new FiscalSigningScope("fake","device-v1",FiscalEnvironment.Test,"000000000",0),
                "fake/v1",Encoding.UTF8.GetBytes("signed bytes"));
            var http = new Http(source.Source.Id,InvoiceProviderCatalog.MyDataKey) {
                InvoicePost = body => Reply("<ResponseDoc><response><statusCode>ValidationError</statusCode><errors><error><code>1</code><message>synthetic</message></error></errors></response></ResponseDoc>")
            };
            var signer = new Signer(); var workflow = Workflow(http,new Printer(),signer:signer);
            Equal("Rejected",workflow.Process(source.Source.Id,source,false).FiscalState);
            http.InvoicePost = body => Reply(MyDataReply);
            var result = workflow.Process(source.Source.Id,null,false,retryRejected:true);
            Check(result.Accepted,"the signed direct-myDATA submission retry was not accepted");
            Equal(1,signer.Calls); Equal(2,http.Posts);
        });
        Test("provider reconciliation 404 can switch to signed direct myDATA", () =>
        {
            var source = new WorkflowSource();
            Guid invoiceId = source.Source.Id;
            using (var lease = database.EnterInvoice(invoiceId))
            {
                source.Prepare(lease);
            }

            ProviderRoute providerRoute = Route(
                InvoiceProviderCatalog.ITimologiaKey);
            source.Snapshot.Provider = providerRoute;
            source.Snapshot.Invoice = source.Source.Prepare(invoiceId,
                providerRoute);
            source.Snapshot.Signing = null;
            var http = new Http(invoiceId,
                InvoiceProviderCatalog.ITimologiaKey);
            http.InvoicePost = body => body.TrimStart().StartsWith("<")
                ? Reply(MyDataReply)
                : Reply("{\"message\":\"Invoice already registered at myData. " +
                    "Do not resend.\"}", 452);
            http.Lookup = () => Reply("{}", 404);
            var signer = new Signer();
            var printer = new Printer();
            InvoiceWorkflowService workflow = Workflow(http, printer,
                signer: signer);
            InvoiceWorkflowResult rejected = workflow.Process(invoiceId, source,
                false);
            Equal("Rejected", rejected.FiscalState);

            ProviderRoute myDataRoute = Route(InvoiceProviderCatalog.MyDataKey);
            source.Snapshot.Provider = myDataRoute;
            source.Snapshot.Invoice = source.Source.Prepare(invoiceId,
                myDataRoute);
            var document = new FiscalDocumentReference(
                FiscalDocumentKind.Invoice, invoiceId);
            var scope = new FiscalSigningScope("fake", "device-v1",
                FiscalEnvironment.Test, "000000000", 0);
            byte[] payload = Encoding.UTF8.GetBytes("signed fallback bytes");
            Guid signingAttemptId = Guid.NewGuid();
            source.Snapshot.Signing = new FiscalSigningRequest(signingAttemptId,
                document, scope, "fake/v1", payload);

            InvoiceWorkflowResult accepted = workflow.Process(invoiceId, source,
                false, retryRejected: true);
            Check(accepted.Accepted,
                "the released provider submission did not switch to signed myDATA");
            Equal(1, signer.Calls);
            Equal(2, http.Posts);
            Equal(1, http.Gets);
        });
        Test("a signed resend retains fiscal values and accepts current trader metadata", () =>
        {
            var source = new WorkflowSource();
            using (var lease = database.EnterInvoice(source.Source.Id))
            {
                source.Prepare(lease);
            }

            Guid signedTraderId = source.Snapshot.Invoice.Invoice.TraderId;
            Guid signedVehicleId = Guid.NewGuid();
            source.Snapshot.Invoice.Invoice.VehicleId = signedVehicleId;
            source.Snapshot.Invoice.Invoice.Trader.TaxRegistrationId = "BG204985348";
            source.Snapshot.Invoice.Country = new CountryInput
            {
                CountryCode = "GR",
                IsEu = true
            };
            var document = new FiscalDocumentReference(FiscalDocumentKind.Invoice,
                source.Source.Id);
            var scope = new FiscalSigningScope(SamtecFiscalSigner.Key, "device-v1",
                FiscalEnvironment.Test, "000000000", 0);
            byte[] rejectedSigningPayload = Encoding.UTF8.GetBytes(
                "rejected signing bytes");
            Guid rejectedSigningAttemptId = Guid.NewGuid();
            source.Snapshot.Signing = new FiscalSigningRequest(
                rejectedSigningAttemptId, document, scope, "fake/v1",
                rejectedSigningPayload);
            var http = new Http(source.Source.Id, InvoiceProviderCatalog.MyDataKey);
            http.InvoicePost = body =>
            {
                string rejection = "<ResponseDoc><response><statusCode>ValidationError" +
                    "</statusCode><errors><error><code>202</code>" +
                    "<message>Invalid Greek VAT number</message></error></errors>" +
                    "</response></ResponseDoc>";
                HttpResponseMessage response = Reply(rejection);
                return response;
            };
            var signer = new RejectThenSign();
            var printer = new Printer();
            InvoiceWorkflowService workflow = Workflow(http, printer, signer: signer);
            InvoiceWorkflowResult signingRejected = workflow.Process(source.Source.Id,
                source, false);
            Equal("Rejected", signingRejected.FiscalState);
            Equal(0, http.Posts);

            byte[] signingPayload = Encoding.UTF8.GetBytes("signed bytes");
            Guid signingAttemptId = Guid.NewGuid();
            source.Snapshot.Signing = new FiscalSigningRequest(signingAttemptId,
                document, scope, "fake/v1", signingPayload);
            InvoiceWorkflowResult rejected = workflow.Process(source.Source.Id,
                source, false, retryRejected: true);
            Equal("Rejected", rejected.FiscalState);
            string rejectedPayload = http.LastPayload;
            bool sentGreekCountry = rejectedPayload.Contains("<country>GR</country>");
            bool sentDomesticType = rejectedPayload.Contains("<invoiceType>1.1</invoiceType>");
            Check(sentGreekCountry && sentDomesticType,
                "the synthetic old payload was not domestic");

            source.Snapshot.Invoice.Country = new CountryInput
            {
                CountryCode = "BG",
                IsEu = true
            };
            source.Snapshot.Invoice.Invoice.NettoAmount = 800m;
            source.Snapshot.Invoice.Invoice.VATAmount = 199m;
            source.Snapshot.Invoice.Invoice.TotalAmount = 999m;
            source.Snapshot.Invoice.Invoice.VehicleId = Guid.NewGuid();
            byte[] regeneratedSigningPayload = Encoding.UTF8.GetBytes(
                "regenerated provider-specific signing bytes");
            Guid regeneratedSigningAttemptId = Guid.NewGuid();
            source.Snapshot.Signing = new FiscalSigningRequest(regeneratedSigningAttemptId,
                document, scope, "fake/v1", regeneratedSigningPayload);
            http.InvoicePost = body =>
            {
                HttpResponseMessage response = Reply(MyDataReply);
                return response;
            };
            InvoiceWorkflowResult accepted = workflow.Process(source.Source.Id,
                source, false, retryRejected: true);
            Check(accepted.Accepted,
                "the signed invoice was not resent with its frozen payload");
            string acceptedPayload = http.LastPayload;
            bool usedCurrentCountry = acceptedPayload.Contains(
                "<country>BG</country>");
            bool retainedSignedTotal = acceptedPayload.Contains(
                "<totalGrossValue>124.00</totalGrossValue>");
            Check(usedCurrentCountry && retainedSignedTotal,
                "the resend did not combine current trader metadata with the signed totals");
            Equal(2, signer.Calls);
            Equal(2, http.Posts);
            using (var lease = database.EnterInvoice(source.Source.Id))
            {
                byte[] savedBytes = new SqlInvoiceWorkflowStore(database).Load(lease);
                string savedJson = Encoding.UTF8.GetString(savedBytes);
                InvoiceWorkflowSnapshot savedSnapshot =
                    JsonConvert.DeserializeObject<InvoiceWorkflowSnapshot>(savedJson);
                string savedSigningPayload = Encoding.UTF8.GetString(
                    savedSnapshot.Signing.Payload);
                Equal("signed bytes", savedSigningPayload);
                Equal("BG", savedSnapshot.Invoice.Country.CountryCode);
                Equal(124m, savedSnapshot.Invoice.Invoice.TotalAmount);
                Equal(signedTraderId, savedSnapshot.Invoice.Invoice.TraderId);
                Equal(signedVehicleId,
                    savedSnapshot.Invoice.Invoice.VehicleId.Value);
            }
        });
        Test("a rejected signing attempt may rebuild through the current provider route", () =>
        {
            var source = new WorkflowSource();
            Guid invoiceId = source.Source.Id;
            using (var lease = database.EnterInvoice(invoiceId))
            {
                source.Prepare(lease);
            }

            var document = new FiscalDocumentReference(FiscalDocumentKind.Invoice,
                invoiceId);
            var scope = new FiscalSigningScope(SamtecFiscalSigner.Key, "device-v1",
                FiscalEnvironment.Test, "000000000", 0);
            source.Snapshot.Signing = new FiscalSigningRequest(Guid.NewGuid(),
                document, scope, "fake/v1", Encoding.UTF8.GetBytes(
                    "rejected signing bytes"));
            var signer = new AmountMismatchSigner
            {
                AlwaysReject = true
            };
            var http = new Http(invoiceId, InvoiceProviderCatalog.ITimologiaKey);
            InvoiceWorkflowService workflow = Workflow(http, new Printer(),
                signer: signer);
            InvoiceWorkflowResult rejected = workflow.Process(invoiceId, source,
                false);
            Equal("Rejected", rejected.FiscalState);
            Equal(0, http.Posts);

            ProviderRoute providerRoute = Route(InvoiceProviderCatalog.ITimologiaKey);
            source.Snapshot.Provider = providerRoute;
            source.Snapshot.Invoice = source.Source.Prepare(invoiceId, providerRoute);
            source.Snapshot.Signing = null;
            InvoiceWorkflowResult accepted = workflow.Process(invoiceId, source,
                false, retryRejected: true);
            Check(accepted.Accepted,
                "the rejected signing attempt did not rebuild through the provider");
            Equal(1, signer.Calls);
            Equal(1, http.Posts);
        });
        Test("definitive Samtec rejection regenerates a recalculated workflow before retry", () => {
            var source = new WorkflowSource(); var id = source.Source.Id;
            var scope = new FiscalSigningScope(SamtecFiscalSigner.Key,"device-v1",FiscalEnvironment.Test,"000000000",0);
            source.Snapshot = new InvoiceWorkflowSnapshot { InvoiceId = id,
                Signing = new FiscalSigningRequest(Guid.NewGuid(),new FiscalDocumentReference(FiscalDocumentKind.Invoice,id),
                    scope,"synthetic/v1",Encoding.UTF8.GetBytes("incorrect calculated request")),
                PrintDestination = "saved printer",PrintFormat = "test/v1",
                PrintContent = Encoding.UTF8.GetBytes("incorrect calculated print") };
            var signer = new RejectThenSign(); var workflow = Workflow(new Http(id,InvoiceProviderCatalog.MyDataKey),
                new Printer(),signer:signer);
            Equal("Rejected",workflow.Process(id,source,false).FiscalState);

            source.Snapshot.Signing = new FiscalSigningRequest(Guid.NewGuid(),
                new FiscalDocumentReference(FiscalDocumentKind.Invoice,id),scope,"synthetic/v1",
                Encoding.UTF8.GetBytes("recalculated request"));
            source.Snapshot.PrintContent = Encoding.UTF8.GetBytes("recalculated print");
            var result = workflow.Process(id,source,false,retryRejected:true);
            Check(result.Accepted,"the regenerated signing request was not accepted");
            Equal(2,source.Loads); Equal(2,signer.Calls);
            Equal("incorrect calculated request",Encoding.UTF8.GetString(signer.Payloads[0]));
            Equal("recalculated request",Encoding.UTF8.GetString(signer.Payloads[1]));
            using (var lease = database.EnterInvoice(id))
            {
                var saved = JsonConvert.DeserializeObject<InvoiceWorkflowSnapshot>(Encoding.UTF8.GetString(
                    new SqlInvoiceWorkflowStore(database).Load(lease)));
                Equal("recalculated request",Encoding.UTF8.GetString(saved.Signing.Payload));
                Equal("recalculated print",Encoding.UTF8.GetString(saved.PrintContent));
            }
        });
        Test("definitive Samtec rejection permits a re-saved device profile for the same issuer", () => {
            var source = new WorkflowSource(); var id = source.Source.Id;
            var oldScope = new FiscalSigningScope(SamtecFiscalSigner.Key,"device-v1",
                FiscalEnvironment.Test,"000000000",0);
            source.Snapshot = new InvoiceWorkflowSnapshot { InvoiceId = id,
                Signing = new FiscalSigningRequest(Guid.NewGuid(),
                    new FiscalDocumentReference(FiscalDocumentKind.Invoice,id),oldScope,"synthetic/v1",
                    Encoding.UTF8.GetBytes("request with old payment")),
                PrintDestination = "saved printer",PrintFormat = "test/v1",
                PrintContent = Encoding.UTF8.GetBytes("old print") };
            var signer = new RejectThenSign(); var workflow = Workflow(
                new Http(id,InvoiceProviderCatalog.MyDataKey),new Printer(),signer:signer);
            Equal("Rejected",workflow.Process(id,source,false).FiscalState);

            var reSavedScope = new FiscalSigningScope(SamtecFiscalSigner.Key,"device-v2",
                FiscalEnvironment.Test,"000000000",0);
            source.Snapshot.Signing = new FiscalSigningRequest(Guid.NewGuid(),
                new FiscalDocumentReference(FiscalDocumentKind.Invoice,id),reSavedScope,"synthetic/v1",
                Encoding.UTF8.GetBytes("request with corrected payment"));
            source.Snapshot.PrintContent = Encoding.UTF8.GetBytes("corrected print");
            var result = workflow.Process(id,source,false,retryRejected:true);
            Check(result.Accepted,"the corrected request did not use the re-saved device profile");
            Equal(2,signer.Calls);
            using (var lease = database.EnterInvoice(id))
            {
                var saved = JsonConvert.DeserializeObject<InvoiceWorkflowSnapshot>(Encoding.UTF8.GetString(
                    new SqlInvoiceWorkflowStore(database).Load(lease)));
                Equal("device-v2",saved.Signing.Scope.DeviceId);
                Equal("request with corrected payment",Encoding.UTF8.GetString(saved.Signing.Payload));
            }
        });
        Test("worker automatically regenerates a changed Samtec amount-mismatch request", () => {
            var source = new WorkflowSource(); var id = source.Source.Id;
            var scope = new FiscalSigningScope(SamtecFiscalSigner.Key,"device-v1",
                FiscalEnvironment.Test,"000000000",0);
            source.Snapshot = new InvoiceWorkflowSnapshot { InvoiceId = id,
                Signing = new FiscalSigningRequest(Guid.NewGuid(),
                    new FiscalDocumentReference(FiscalDocumentKind.Invoice,id),scope,"synthetic/v1",
                    Encoding.UTF8.GetBytes("{\"Doc\":{\"Payments\":[{\"Type\":0}]},\"Version\":\"old\"}")),
                PrintDestination = "saved printer",PrintFormat = "test/v1",
                PrintContent = Encoding.UTF8.GetBytes("incorrect calculated print") };
            var signer = new AmountMismatchSigner();
            var workflow = Workflow(new Http(id,InvoiceProviderCatalog.MyDataKey),new Printer(),signer:signer);
            Equal("Rejected",workflow.Process(id,source,false).FiscalState);

            source.Snapshot.Signing = new FiscalSigningRequest(Guid.NewGuid(),
                new FiscalDocumentReference(FiscalDocumentKind.Invoice,id),scope,"synthetic/v1",
                Encoding.UTF8.GetBytes("{\"Doc\":{\"Payments\":[{\"Type\":0}]},\"Version\":\"corrected\"}"));
            source.Snapshot.PrintContent = Encoding.UTF8.GetBytes("corrected print");
            // Recreate the service to prove the persisted rejection is resumed by
            // the worker after an application restart.
            workflow = Workflow(new Http(id,InvoiceProviderCatalog.MyDataKey),new Printer(),signer:signer);
            var result = workflow.ProcessAutomaticSamtecAmountCorrection(id,source,false);
            Check(result.Accepted,"the worker did not sign the regenerated amount request");
            Equal(2,signer.Calls);
            Equal("{\"Doc\":{\"Payments\":[{\"Type\":0}]},\"Version\":\"corrected\"}",
                Encoding.UTF8.GetString(signer.Payloads[1]));
        });
        Test("worker stops after the corrected Samtec amount request is rejected again", () => {
            var source = new WorkflowSource(); var id = source.Source.Id;
            var scope = new FiscalSigningScope(SamtecFiscalSigner.Key,"device-v1",
                FiscalEnvironment.Test,"000000000",0);
            source.Snapshot = new InvoiceWorkflowSnapshot { InvoiceId = id,
                Signing = new FiscalSigningRequest(Guid.NewGuid(),
                    new FiscalDocumentReference(FiscalDocumentKind.Invoice,id),scope,"synthetic/v1",
                    Encoding.UTF8.GetBytes("{\"Doc\":{\"Payments\":[{\"Type\":0}]},\"Version\":\"old\"}")),
                PrintDestination = "saved printer",PrintFormat = "test/v1",
                PrintContent = Encoding.UTF8.GetBytes("old print") };
            var signer = new AmountMismatchSigner { AlwaysReject = true };
            var workflow = Workflow(new Http(id,InvoiceProviderCatalog.MyDataKey),new Printer(),signer:signer);
            Equal("Rejected",workflow.Process(id,source,false).FiscalState);

            source.Snapshot.Signing = new FiscalSigningRequest(Guid.NewGuid(),
                new FiscalDocumentReference(FiscalDocumentKind.Invoice,id),scope,"synthetic/v1",
                Encoding.UTF8.GetBytes("{\"Doc\":{\"Payments\":[{\"Type\":0}]},\"Version\":\"corrected\"}"));
            Equal("Rejected",workflow.ProcessAutomaticSamtecAmountCorrection(id,source,false).FiscalState);
            Equal(2,signer.Calls);
            Equal("Rejected",workflow.ProcessAutomaticSamtecAmountCorrection(id,source,false).FiscalState);
            Equal(2,signer.Calls);
        });
        Test("worker does not automatically retry a Samtec amount mismatch with integrated EFTPOS", () => {
            var source = new WorkflowSource(); var id = source.Source.Id;
            var scope = new FiscalSigningScope(SamtecFiscalSigner.Key,"device-v1",
                FiscalEnvironment.Test,"000000000",0);
            source.Snapshot = new InvoiceWorkflowSnapshot { InvoiceId = id,
                Signing = new FiscalSigningRequest(Guid.NewGuid(),
                    new FiscalDocumentReference(FiscalDocumentKind.Invoice,id),scope,"synthetic/v1",
                    Encoding.UTF8.GetBytes("{\"Doc\":{\"Payments\":[{\"Type\":1}]},\"Version\":\"old\"}")),
                PrintDestination = "saved printer",PrintFormat = "test/v1",
                PrintContent = Encoding.UTF8.GetBytes("old print") };
            var signer = new AmountMismatchSigner();
            var workflow = Workflow(new Http(id,InvoiceProviderCatalog.MyDataKey),new Printer(),signer:signer);
            Equal("Rejected",workflow.Process(id,source,false).FiscalState);

            source.Snapshot.Signing = new FiscalSigningRequest(Guid.NewGuid(),
                new FiscalDocumentReference(FiscalDocumentKind.Invoice,id),scope,"synthetic/v1",
                Encoding.UTF8.GetBytes("{\"Doc\":{\"Payments\":[{\"Type\":1}]},\"Version\":\"corrected\"}"));
            Equal("Rejected",workflow.ProcessAutomaticSamtecAmountCorrection(id,source,false).FiscalState);
            Equal(1,signer.Calls);
        });
        Test("worker does not automatically retry other definitive Samtec rejections", () => {
            var source = new WorkflowSource(); var id = source.Source.Id;
            var scope = new FiscalSigningScope(SamtecFiscalSigner.Key,"device-v1",
                FiscalEnvironment.Test,"000000000",0);
            source.Snapshot = new InvoiceWorkflowSnapshot { InvoiceId = id,
                Signing = new FiscalSigningRequest(Guid.NewGuid(),
                    new FiscalDocumentReference(FiscalDocumentKind.Invoice,id),scope,"synthetic/v1",
                    Encoding.UTF8.GetBytes("{\"Doc\":{\"Payments\":[{\"Type\":0}]},\"Version\":\"old\"}")),
                PrintDestination = "saved printer",PrintFormat = "test/v1",
                PrintContent = Encoding.UTF8.GetBytes("old print") };
            var signer = new RejectThenSign();
            var workflow = Workflow(new Http(id,InvoiceProviderCatalog.MyDataKey),new Printer(),signer:signer);
            Equal("Rejected",workflow.Process(id,source,false).FiscalState);

            source.Snapshot.Signing = new FiscalSigningRequest(Guid.NewGuid(),
                new FiscalDocumentReference(FiscalDocumentKind.Invoice,id),scope,"synthetic/v1",
                Encoding.UTF8.GetBytes("{\"Doc\":{\"Payments\":[{\"Type\":0}]},\"Version\":\"corrected\"}"));
            Equal("Rejected",workflow.ProcessAutomaticSamtecAmountCorrection(id,source,false).FiscalState);
            Equal(1,signer.Calls);
        });
        foreach (int payment in new[] { 2,4 })
            Test("direct myDATA accepts Samtec integrated POS payment " + payment, () => {
                var source = new WorkflowSource(); source.Source.Invoice.PaymentType = payment;
                using(var lease = database.EnterInvoice(source.Source.Id)) source.Prepare(lease);
                var scope = new FiscalSigningScope(SamtecFiscalSigner.Key,"samtec-pos",FiscalEnvironment.Test,"000000000",0);
                source.Snapshot.Signing = new FiscalSigningRequest(Guid.NewGuid(),
                    new FiscalDocumentReference(FiscalDocumentKind.Invoice,source.Source.Id),scope,
                    "samtec-test/v1",Encoding.UTF8.GetBytes("signed card/IRIS bytes"));
                Equal(null,source.Snapshot.Payment);
                var http = new Http(source.Source.Id,InvoiceProviderCatalog.MyDataKey);
                var printer = new Printer(); var signer = new Signer(SamtecFiscalSigner.Key);
                var result = Workflow(http,printer,signer:signer).Process(source.Source.Id,source,false);
                Check(result.Accepted,"integrated Samtec POS did not reach direct myDATA");
                Equal(1,signer.Calls); Equal(1,http.Posts); Equal(0,printer.Calls);
            });
        Test("direct myDATA card without a POS owner remains rejected", () => {
            var source = new WorkflowSource(); source.Source.Invoice.PaymentType = 2;
            var http = new Http(source.Source.Id,InvoiceProviderCatalog.MyDataKey); var printer = new Printer();
            Throws(() => Workflow(http,printer).Process(source.Source.Id,source,false));
            Equal(0,http.Calls); Equal(0,printer.Calls);
        });
        Test("snapshot fingerprint detects mutation before dispatch", () => {
            var source = new WorkflowSource(); var http = new Http(source.Source.Id,InvoiceProviderCatalog.MyDataKey); var printer = new Printer(); var workflow = Workflow(http,printer);
            workflow.Process(source.Source.Id,source,false); Execute(connection,"UPDATE dbo.FiscalInvoiceWorkflow SET Payload=0x01 WHERE InvoiceId='" + source.Source.Id + "';");
            try { workflow.Process(source.Source.Id,null); throw new Exception("expected fingerprint error"); } catch(System.IO.InvalidDataException) { }
            Equal(1,http.Posts); Equal(0,printer.Calls);
        });
        NumberingTests();
        IssueDateReissueTests();
        PaymentMethodChangeTests();
        DispatchDateRecoveryStoreTests();
        foreach(var kind in new[] { FiscalDocumentKind.Balance,FiscalDocumentKind.TankFilling })
        {
            Test(kind + " archives after signing without requiring physical printing", () => {
                var doc = new FiscalDocumentReference(kind,Guid.NewGuid()); var signer = new Signer(); var printer = new Printer();
                int projects = 0, renders = 0;
                var service = new SignedDocumentWorkflowService(database,new FiscalSigningService(database,s => signer),
                    new FiscalPrintService(database,printer),(s,r) => { renders++; return s.PrintContent; });
                var snapshot = new InvoiceWorkflowSnapshot { InvoiceId = doc.DocumentId,
                    Signing = new FiscalSigningRequest(Guid.NewGuid(),doc,new FiscalSigningScope("fake","device-v1",FiscalEnvironment.Test,"000000000",0),"fake/v1",Encoding.UTF8.GetBytes("sign me")),
                    PrintContent = Encoding.UTF8.GetBytes("original document"),PrintDestination = "saved printer",PrintFormat = "test/v1" };
                var result = service.Process(doc,() => snapshot,r => projects++,null,false);
                Equal("Signed",result.FiscalState); Check(result.Print == null,"physical print was created");
                Equal(1,signer.Calls); Equal(1,projects); Equal(0,renders); Equal(0,printer.Calls);
            });
            Test(kind + " projects the signed archive before rendering and dispatch", () => {
                var doc = new FiscalDocumentReference(kind,Guid.NewGuid()); var signer = new Signer(); var printer = new Printer();
                var timeline = new List<string>();
                printer.OnDispatch = () => timeline.Add("dispatch");
                var service = new SignedDocumentWorkflowService(database,new FiscalSigningService(database,s => signer),
                    new FiscalPrintService(database,printer),(s,r) => { timeline.Add("render"); return s.PrintContent; });
                var snapshot = new InvoiceWorkflowSnapshot { InvoiceId = doc.DocumentId,
                    Signing = new FiscalSigningRequest(Guid.NewGuid(),doc,new FiscalSigningScope("fake","device-v1",FiscalEnvironment.Test,"000000000",0),"fake/v1",Encoding.UTF8.GetBytes("sign me")),
                    PrintContent = Encoding.UTF8.GetBytes("original document"),PrintDestination = "saved printer",PrintFormat = "test/v1" };
                service.Process(doc,() => snapshot,r => timeline.Add("project"));
                Check(timeline.IndexOf("project") >= 0 && timeline.IndexOf("project") < timeline.IndexOf("render"),"archive projection did not precede rendering");
                Check(timeline.IndexOf("render") < timeline.IndexOf("dispatch"),"rendering did not precede dispatch");
            });
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
        Execute(connection,"CREATE TABLE dbo.InvoiceType(InvoiceTypeId uniqueidentifier PRIMARY KEY,LastNumber int NOT NULL,DefaultSeries nvarchar(255)); CREATE TABLE dbo.Invoice(InvoiceId uniqueidentifier PRIMARY KEY,InvoiceTypeId uniqueidentifier NOT NULL,Number int NOT NULL,Series nvarchar(255),TransactionDate datetime NOT NULL,IsPrinted bit NULL,InvoiceSignature nvarchar(max) NULL,QRCodeData nvarchar(max) NULL,PaymentType int NULL); CREATE TABLE dbo.InvoiceLine(InvoiceId uniqueidentifier); CREATE TABLE dbo.MyDataInvoice(InvoiceId uniqueidentifier,Mark bigint NULL);");
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
        Test("counter validation ignores inconsistencies older than its ten-invoice rolling window", () => {
            var type = Guid.NewGuid(); var target = Guid.NewGuid();
            var recent = Enumerable.Range(1,10).Select(i => new { Id = Guid.NewGuid(),Number = 10 + i,Day = i }).ToArray();
            Execute(connection,"INSERT dbo.InvoiceType VALUES('" + type + "',20,'WINDOW');" +
                "INSERT dbo.Invoice(InvoiceId,InvoiceTypeId,Number,Series,TransactionDate) VALUES('" + Guid.NewGuid() + "','" + type + "',150,'WINDOW','20260101');" +
                string.Join("",recent.Select(item => "INSERT dbo.Invoice(InvoiceId,InvoiceTypeId,Number,Series,TransactionDate) VALUES('" + item.Id + "','" + type + "'," + item.Number + ",'WINDOW','202610" + item.Day.ToString("00") + "');")) +
                "INSERT dbo.Invoice(InvoiceId,InvoiceTypeId,Number,Series,TransactionDate) VALUES('" + target + "','" + type + "',0,NULL,'20261015');");
            using(var lease = database.EnterInvoice(target)) Equal(21,new SqlInvoiceWorkflowStore(database).AllocateNumbers(lease));
        });
        Test("counter validation still rejects an inconsistency in the ten most recent invoices", () => {
            var type = Guid.NewGuid(); var target = Guid.NewGuid();
            Execute(connection,"INSERT dbo.InvoiceType VALUES('" + type + "',20,'RECENT');" +
                "INSERT dbo.Invoice(InvoiceId,InvoiceTypeId,Number,Series,TransactionDate) VALUES('" + Guid.NewGuid() + "','" + type + "',150,'RECENT','20261014');" +
                "INSERT dbo.Invoice(InvoiceId,InvoiceTypeId,Number,Series,TransactionDate) VALUES('" + target + "','" + type + "',0,NULL,'20261015');");
            using(var lease = database.EnterInvoice(target)) Throws(() => new SqlInvoiceWorkflowStore(database).AllocateNumbers(lease));
        });
        Test("cutover numbering excludes earlier and legacy-issued records", () => {
            var type = Guid.NewGuid(); var old = Guid.NewGuid(); var signed = Guid.NewGuid(); var submitted = Guid.NewGuid(); var current = Guid.NewGuid();
            Execute(connection,"INSERT dbo.InvoiceType VALUES('" + type + "',0,'CUT');" + string.Join("",new[] { old,signed,submitted,current }.Select(id =>
                "INSERT dbo.Invoice(InvoiceId,InvoiceTypeId,Number,Series,TransactionDate) VALUES('" + id + "','" + type + "',0,NULL,'" + (id == old ? "20261013" : id == current ? "20261015" : "20261014") + "');")) +
                "UPDATE dbo.Invoice SET InvoiceSignature='legacy signature' WHERE InvoiceId='" + signed + "'; INSERT dbo.MyDataInvoice(InvoiceId) VALUES('" + submitted + "');");
            using(var lease = database.EnterInvoice(current)) Equal(1,new SqlInvoiceWorkflowStore(database).AllocateNumbers(lease,new DateTime(2026,10,14)));
            using(var c = new System.Data.SqlClient.SqlConnection(connection)) using(var cmd = c.CreateCommand())
            {
                c.Open(); cmd.CommandText = "SELECT SUM(Number) FROM dbo.Invoice WHERE InvoiceTypeId=@type AND InvoiceId<>@current";
                cmd.Parameters.AddWithValue("@type",type); cmd.Parameters.AddWithValue("@current",current); Equal(0,Convert.ToInt32(cmd.ExecuteScalar()));
            }
        });
    }
    static void IssueDateReissueTests()
    {
        Test("issue-date reissue consumes the next number and changes the host issue date atomically", () => {
            var type=Guid.NewGuid();var id=Guid.NewGuid();var issueDate=new DateTime(2026,11,6,9,30,0);
            Execute(connection,"INSERT dbo.InvoiceType VALUES('"+type+"',50,'R'); " +
                "INSERT dbo.Invoice(InvoiceId,InvoiceTypeId,Number,Series,TransactionDate,PaymentType,IsPrinted) VALUES('"+id+"','"+type+"',49,'R','20261105 21:00',1,1);");
            using(var lease=database.EnterInvoice(id))
                Equal(51,new SqlInvoiceWorkflowStore(database).AllocateIssueDateReissueIdentity(
                    lease,49,"R",new DateTime(2026,11,5,21,0,0),issueDate));
            using(var cn=new System.Data.SqlClient.SqlConnection(connection))using(var cmd=cn.CreateCommand())
            {
                cn.Open();cmd.CommandText="SELECT i.Number,i.TransactionDate,t.LastNumber,i.IsPrinted FROM dbo.Invoice i JOIN dbo.InvoiceType t ON t.InvoiceTypeId=i.InvoiceTypeId WHERE i.InvoiceId=@id";
                cmd.Parameters.AddWithValue("@id",id);
                using(var reader=cmd.ExecuteReader())
                {Check(reader.Read(),"reissued invoice");Equal(51,reader.GetInt32(0));Equal(issueDate,reader.GetDateTime(1));Equal(51,reader.GetInt32(2));Equal(false,reader.GetBoolean(3));}
            }
        });
        Test("Ilyda ER-30 is the only structured date rejection that enables reissue", () => {
            var type=Guid.NewGuid();var source=new Source();
            Execute(connection,"INSERT dbo.InvoiceType VALUES('"+type+"',60,'R'); " +
                "INSERT dbo.Invoice(InvoiceId,InvoiceTypeId,Number,Series,TransactionDate,PaymentType) VALUES('"+source.Id+"','"+type+"',60,'R','20261105',1);");
            var original=Encoding.UTF8.GetBytes("{\"identity\":\"old\"}");
            using(var lease=database.EnterInvoice(source.Id))new SqlInvoiceWorkflowStore(database).Save(lease,original);
            var http=new Http(source.Id,InvoiceProviderCatalog.DirectIlydaKey) { InvoicePost = body => Reply(
                "{\"invoiceMarking\":null,\"errors\":[{\"code\":\"ER-30\",\"fatal\":true,\"message\":\"invoiceIssueDate must be current date\"}]}",422) };
            Equal("Rejected",Service(http).Send(source.Id,Route(InvoiceProviderCatalog.DirectIlydaKey),source).State);
            Check(ProviderSubmissionService.IsIssueDateMismatchRejection(Latest(source.Id)),"ER-30 date rejection");
            var replacement=Encoding.UTF8.GetBytes("{\"identity\":\"new\"}");
            using(var lease=database.EnterInvoice(source.Id))
            {
                var store=new SqlInvoiceWorkflowStore(database);
                store.ReplaceAfterIssueDateReissue(lease,original,replacement);
                Equal(Encoding.UTF8.GetString(replacement),Encoding.UTF8.GetString(store.Load(lease)));
            }
        });
    }
    static void PaymentMethodChangeTests()
    {
        Test("numbered invoice without a payload can change payment method", () => {
            var type=Guid.NewGuid();var id=Guid.NewGuid();
            Execute(connection,"INSERT dbo.InvoiceType VALUES('"+type+"',1,'P'); INSERT dbo.Invoice(InvoiceId,InvoiceTypeId,Number,Series,TransactionDate,PaymentType) VALUES('"+id+"','"+type+"',1,'P','20261104',1);");
            using(var lease=database.EnterInvoice(id))
                new SqlInvoiceWorkflowStore(database).ChangePaymentMethod(lease,null,null,1,0);
            using(var cn=new System.Data.SqlClient.SqlConnection(connection))using(var cmd=cn.CreateCommand())
            {cn.Open();cmd.CommandText="SELECT PaymentType FROM dbo.Invoice WHERE InvoiceId='"+id+"'";Equal(0,Convert.ToInt32(cmd.ExecuteScalar()));}
        });
        Test("payment change supersedes an undispatched signing payload and keeps its history", () => {
            var type=Guid.NewGuid();var id=Guid.NewGuid();
            Execute(connection,"INSERT dbo.InvoiceType VALUES('"+type+"',2,'P'); INSERT dbo.Invoice(InvoiceId,InvoiceTypeId,Number,Series,TransactionDate,PaymentType) VALUES('"+id+"','"+type+"',2,'P','20261104',1);");
            var original=Encoding.UTF8.GetBytes("{\"payment\":1}");var replacement=Encoding.UTF8.GetBytes("{\"payment\":0}");
            var request=new FiscalSigningRequest(Guid.NewGuid(),new FiscalDocumentReference(FiscalDocumentKind.Invoice,id),
                new FiscalSigningScope("fake","device",FiscalEnvironment.Test,"000000000",0),"test/v1",Encoding.UTF8.GetBytes("old request"));
            using(var lease=database.EnterInvoice(id))
            {
                var workflows=new SqlInvoiceWorkflowStore(database);workflows.Save(lease,original);
                new SqlFiscalSigningStore(database).Begin(lease,request,DateTime.UtcNow);
                workflows.ChangePaymentMethod(lease,original,replacement,1,0);
                Equal("{\"payment\":0}",Encoding.UTF8.GetString(workflows.Load(lease)));
                var signing=new SqlFiscalSigningStore(database);
                Equal(FiscalSigningState.FailedBeforeDispatch,signing.Load(lease).Last().State);
                Equal(2,signing.LoadHistory(lease,request.AttemptId).Count);
            }
        });
        Test("payment change cannot invalidate a confirmed signature", () => {
            var type=Guid.NewGuid();var id=Guid.NewGuid();
            Execute(connection,"INSERT dbo.InvoiceType VALUES('"+type+"',3,'P'); INSERT dbo.Invoice(InvoiceId,InvoiceTypeId,Number,Series,TransactionDate,PaymentType) VALUES('"+id+"','"+type+"',3,'P','20261104',1);");
            var original=Encoding.UTF8.GetBytes("{\"payment\":1}");var replacement=Encoding.UTF8.GetBytes("{\"payment\":0}");
            var request=new FiscalSigningRequest(Guid.NewGuid(),new FiscalDocumentReference(FiscalDocumentKind.Invoice,id),
                new FiscalSigningScope("fake","device",FiscalEnvironment.Test,"000000000",0),"test/v1",Encoding.UTF8.GetBytes("request"));
            using(var lease=database.EnterInvoice(id))
            {
                var workflows=new SqlInvoiceWorkflowStore(database);workflows.Save(lease,original);
                var signingStore=new SqlFiscalSigningStore(database);var prepared=signingStore.Begin(lease,request,DateTime.UtcNow);
                var sending=new FiscalSigningAttempt(request,FiscalSigningState.Signing,prepared.Revision+1,prepared.CreatedUtc,DateTime.UtcNow);
                signingStore.Save(lease,prepared.Revision,sending);
                var signed=new FiscalSigningAttempt(request,FiscalSigningState.Signed,sending.Revision+1,sending.CreatedUtc,DateTime.UtcNow,
                    FiscalSigningResult.Signed(request.AttemptId,"signature",null,null,"ok"));
                signingStore.Save(lease,sending.Revision,signed);
                Throws(()=>workflows.ChangePaymentMethod(lease,original,replacement,1,0));
            }
        });
        Test("payment change can supersede a definitively rejected provider POS session", () => {
            var type=Guid.NewGuid();var id=Guid.NewGuid();
            Execute(connection,"INSERT dbo.InvoiceType VALUES('"+type+"',4,'P'); INSERT dbo.Invoice(InvoiceId,InvoiceTypeId,Number,Series,TransactionDate,PaymentType) VALUES('"+id+"','"+type+"',4,'P','20261104',2);");
            var original=Encoding.UTF8.GetBytes("{\"payment\":2}");var replacement=Encoding.UTF8.GetBytes("{\"payment\":1}");
            using(var lease=database.EnterInvoice(id))
            {
                var workflows=new SqlInvoiceWorkflowStore(database);workflows.Save(lease,original);
                var sessions=new SqlIlydaPosStore(database);
                var session=new IlydaPosSession { Id=Guid.NewGuid(),InvoiceId=id,TestMode=true,
                    IssuerVat="000000000",Endpoint="https://provider.invalid/sign",RequestJson="{}",State="Requesting" };
                sessions.Save(session);session.State="Rejected";sessions.Save(session);
                workflows.ChangePaymentMethod(lease,original,replacement,2,1);
                Equal("{\"payment\":1}",Encoding.UTF8.GetString(workflows.Load(lease)));
            }
        });
    }
    static void DispatchDateRecoveryStoreTests()
    {
        Execute(connection,"ALTER TABLE dbo.InvoiceType ADD OfficialEnumerator int NULL; ALTER TABLE dbo.InvoiceLine ADD SaleTransactionId uniqueidentifier NULL;");
        Test("dispatch recovery changes only the frozen direct-pump delivery payload", () => {
            var type=Guid.NewGuid();var id=Guid.NewGuid();var sale=Guid.NewGuid();
            Execute(connection,"INSERT dbo.InvoiceType(InvoiceTypeId,LastNumber,DefaultSeries,OfficialEnumerator) VALUES('"+type+"',1,'D',222); " +
                "INSERT dbo.Invoice(InvoiceId,InvoiceTypeId,Number,Series,TransactionDate,PaymentType) VALUES('"+id+"','"+type+"',1,'D','20261103',1); " +
                "INSERT dbo.InvoiceLine(InvoiceId,SaleTransactionId) VALUES('"+id+"','"+sale+"');");
            var original=Encoding.UTF8.GetBytes("{\"dispatchDate\":\"2026-11-03\"}");
            var replacement=Encoding.UTF8.GetBytes("{\"dispatchDate\":\"2026-11-04\"}");
            using(var lease=database.EnterInvoice(id))
            {
                var workflows=new SqlInvoiceWorkflowStore(database);workflows.Save(lease,original);
                workflows.ReplaceDeliveryDispatchForRecovery(lease,original,replacement);
                Equal(Encoding.UTF8.GetString(replacement),Encoding.UTF8.GetString(workflows.Load(lease)));
            }
            using(var cn=new System.Data.SqlClient.SqlConnection(connection))using(var cmd=cn.CreateCommand())
            {
                cn.Open();cmd.CommandText="SELECT TransactionDate FROM dbo.Invoice WHERE InvoiceId=@id";
                cmd.Parameters.AddWithValue("@id",id);
                Equal(new DateTime(2026,11,3),Convert.ToDateTime(cmd.ExecuteScalar()));
            }
        });
        Test("dispatch recovery rejects an invoice without a pump-sale line", () => {
            var type=Guid.NewGuid();var id=Guid.NewGuid();
            Execute(connection,"INSERT dbo.InvoiceType(InvoiceTypeId,LastNumber,DefaultSeries,OfficialEnumerator) VALUES('"+type+"',2,'D',222); " +
                "INSERT dbo.Invoice(InvoiceId,InvoiceTypeId,Number,Series,TransactionDate,PaymentType) VALUES('"+id+"','"+type+"',2,'D','20261103',1);");
            var original=Encoding.UTF8.GetBytes("{\"dispatchDate\":\"2026-11-03\"}");
            var replacement=Encoding.UTF8.GetBytes("{\"dispatchDate\":\"2026-11-04\"}");
            using(var lease=database.EnterInvoice(id))
            {
                var workflows=new SqlInvoiceWorkflowStore(database);workflows.Save(lease,original);
                Throws(()=>workflows.ReplaceDeliveryDispatchForRecovery(lease,original,replacement));
                Equal(Encoding.UTF8.GetString(original),Encoding.UTF8.GetString(workflows.Load(lease)));
            }
        });
        // Heating-store fixtures add their own full InvoiceLine shape later.
        Execute(connection,"ALTER TABLE dbo.InvoiceLine DROP COLUMN SaleTransactionId;");
    }
    static void PrinterFallbackTests()
    {
        var installed = new[] { "Type thermal","Thermal fallback","Installation default","Windows default" };
        var defaultProfile = new SharedPrinterProfile { PrinterName = "Installation default",
            ThermalPrinterName = "Thermal fallback" };
        Test("installed invoice printer takes precedence over installation and Windows defaults", () => {
            var profile = SharedInvoicePrinting.ResolveProfile(new SharedPrinterProfile { PrinterName = " type THERMAL " },defaultProfile,installed,"Windows default");
            Equal("Type thermal",profile.PrinterName);
        });
        Test("blank missing and legacy folder thermal printers use the configured thermal fallback", () => {
            foreach (var name in new[] { null,"","80 Printer",@"C:\ASFuelControl\Sign" })
            {
                var profile = SharedInvoicePrinting.ResolveProfile(new SharedPrinterProfile { PrinterName = name },defaultProfile,installed,"Windows default");
                Equal("Thermal fallback",profile.PrinterName);
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
            var saved = new SharedPrinterProfile { PrinterName = "Type laser",Thermal = false,WidthHundredths = 825,HeightHundredths = 1169,Copies = 2,FontSize = 10 };
            var original = JsonConvert.SerializeObject(saved); var defaults = JsonConvert.SerializeObject(defaultProfile);
            var profile = SharedInvoicePrinting.ResolveProfile(saved,defaultProfile,new[] { "Installation default","Windows default" },"Windows default");
            Equal("Installation default",profile.PrinterName); Equal(825,profile.WidthHundredths); Equal(1169,profile.HeightHundredths);
            Equal((short)2,profile.Copies); Equal(10f,profile.FontSize);
            Equal(original,JsonConvert.SerializeObject(saved)); Equal(defaults,JsonConvert.SerializeObject(defaultProfile));
        });
        Test("thermal printer removal uses the thermal fallback rather than the configured laser printer", () => {
            var saved = new SharedPrinterProfile { PrinterName = "Removed dispenser",Thermal = true };
            var profile = SharedInvoicePrinting.ResolveProfile(saved,defaultProfile,
                new[] { "Thermal fallback","Installation default","Windows default" },"Windows default");
            Equal("Thermal fallback",profile.PrinterName);
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
