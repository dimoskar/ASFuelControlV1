using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using ASFuelControl.Invoicing;
using ASFuelControl.Invoicing.Persistence;
using Exedron.Invoicing.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static partial class Program
{
    static string connection; static SqlFiscalDatabase database; static int passed, failed;
    static int Main(string[] args)
    {
        if (args.Length != 1 || !Regex.IsMatch(args[0],"^ASFC_InvoicingStep7_[0-9a-f]{32}$")) throw new ArgumentException("Private instance required.");
        var name = "ASFC_Step7_" + Guid.NewGuid().ToString("N");
        var cs = new SqlConnectionStringBuilder { DataSource = "(localdb)\\" + args[0], InitialCatalog = "master",IntegratedSecurity = true,Pooling = false };
        var master = cs.ConnectionString; cs.InitialCatalog = name; connection = cs.ConnectionString; bool created = false;
        try { Execute(master,"CREATE DATABASE ["+name+"];"); created = true; Execute(master,"ALTER DATABASE ["+name+"] SET COMPATIBILITY_LEVEL = 100;");
            database = new SqlFiscalDatabase(connection); database.InstallSchema(); Tests(); RecoveryTests(); ExtendedRecoveryTests(); Console.WriteLine("RESULT: "+passed+" passed, "+failed+" failed."); return failed == 0 ? 0 : 1; }
        finally { SqlConnection.ClearAllPools(); if(created) Execute(master,"ALTER DATABASE ["+name+"] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE ["+name+"];"); }
    }
    static PaymentRoute Route(string key = "iTimologia",int format = 0) => new PaymentRoute(new ProviderRoute(key,true,"000000000",0,"test-profile-v1","account"),"http://gateway.invalid:7902/","TID-1",format);
    static ProviderPaymentService Service(Http h) => new ProviderPaymentService(database,new Credentials(),route => h,route => h);
    static void Tests()
    {
        foreach(string key in new[] { "iTimologia", "Ilyda.Direct" }) Test(key+" card persists signature/payment before transport and submits only after approval", () => {
            var s = new Source(); var h = new Http(s.Id,key); var service = Service(h);
            var result = service.Send(s.Id,Route(key),s); Accepted(result); Equal("Invoiced",result.Payment.State);
            Equal(1,h.Signatures); Equal(1,h.Authorizations); Equal(1,h.Invoices); Equal(0,h.Voids);
            Equal(0,result.Payment.PaymentType); Equal(12400,result.Payment.ResponseAmountMinorUnits.Value);
            Equal(key == "iTimologia" ? 34 : 8,(int)h.LastAuthorization["TransactionData"]["ProviderId"]);
            s.Invoice.TotalAmount = 999; result = service.Send(s.Id,Route(key == "iTimologia" ? "Ilyda.Direct" : "iTimologia"),null);
            Accepted(result); Equal(1,h.Signatures); Equal(1,h.Authorizations); Equal(1,s.Loads);
            Check(s.Invoice.IsPrinted != true && s.Invoice.InvoiceSignature == null,"Payment changed legacy print/sign fields");
            if(key == "Ilyda.Direct") {
                Equal("010203",(string)h.LastAuthorization["TransactionData"]["ProviderSignature"]);
                Equal("008",(string)h.LastInvoice["paymentMethods"].Single(p => (int)p["type"] == 7)["signingAuthor"]);
                var xml = XDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String((string)h.LastInvoice["additionalSupportDocs"][0]["reference"])));
                XNamespace ns = "http://www.aade.gr/myDATA/invoice/v1.0";
                Check(!xml.Descendants(ns + "paymentMethods").Any(),"Ilyda embeds duplicate POS payment methods");
            }
        });
        Test("iTimologia IRIS is supported with Mellon", () => {
            var s = new Source(); s.Invoice.PaymentType = 4; var h = new Http(s.Id,"iTimologia");
            var route = PaymentRoute.ForMode(Route().Provider,Route().Endpoint,Route().TerminalId,"MELLON");
            var r = Service(h).Send(s.Id,route,s); Accepted(r); Equal(1,r.Payment.PaymentType);
            Equal(1,(int)h.LastAuthorization["TransactionData"]["PaymentType"]); Equal(1,(int)h.LastSignature["acq_id"]);
        });
        foreach (var mode in PosModeCatalog.ForProvider("iTimologia"))
        {
            Test("iTimologia card acq_id for " + mode.Key, () => {
                var s = new Source(); var h = new Http(s.Id,"iTimologia");
                var route = PaymentRoute.ForMode(Route().Provider,Route().Endpoint,Route().TerminalId,mode.Key);
                Accepted(Service(h).Send(s.Id,route,s));
                Equal(mode.Key == "EDPS" || mode.Key == "VIVA" ? 0 : 1,(int)h.LastSignature["acq_id"]);
            });
            if (mode.Key != "MELLON") Test("IRIS blocked before HTTP on " + mode.Key, () => {
                var s = new Source(); s.Invoice.PaymentType = 4; var h = new Http(s.Id,"iTimologia");
                var route = PaymentRoute.ForMode(Route().Provider,Route().Endpoint,Route().TerminalId,mode.Key);
                Throws(() => Service(h).Send(s.Id,route,s)); Equal(0,h.Calls);
            });
        }
        Test("Ilyda IRIS is rejected before provider or gateway HTTP", () => {
            var s = new Source(); s.Invoice.PaymentType = 4; var h = new Http(s.Id,"Ilyda.Direct");
            Throws(() => Service(h).Send(s.Id,Route(h.Key),s)); Equal(0,h.Calls);
        });
        Test("direct myDATA cannot be selected as a GBridge signer", () => Throws(() => Route("myDATA")));
        foreach(string key in new[] { "iTimologia", "Ilyda.Direct" }) Test(key+" payment timeout recovers without another signature or authorization", () => {
            var s = new Source(); var h = new Http(s.Id,key) { UnknownPayment = true }; var service = Service(h);
            Equal("Unknown",service.Send(s.Id,Route(key),s).Payment.State); Equal(1,h.Authorizations); Equal(0,h.Invoices);
            Equal("Unknown",service.Send(s.Id,null,null,true).Payment.State); Equal(1,h.Signatures); Equal(1,h.Authorizations);
            h.UnknownPayment = false;
            var recovered = service.Recover(s.Id); Equal("Approved",recovered.Payment.State); Equal(0,h.Invoices);
            Equal(null,recovered.Payment.ErrorCode); Equal(null,recovered.Payment.ErrorMessage);
            Accepted(service.Send(s.Id,null,null)); Equal(1,h.Authorizations); Equal(1,h.Invoices);
        });
        foreach(string mode in new[] { "terminal", "amount", "type", "payment", "uid", "missing-type", "missing-payment" }) Test("gateway mismatch "+mode+" blocks issuance and new charges", () => {
            var s = new Source(); var h = new Http(s.Id,"iTimologia") { Mismatch = mode }; var service = Service(h);
            var r = service.Send(s.Id,Route(),s); Check(r.Payment.State == "Mismatch" || r.Payment.State == "Unknown","Mismatch was accepted");
            service.Send(s.Id,null,null,true); Equal(1,h.Authorizations); Equal(0,h.Invoices);
        });
        Test("definitive decline requires explicit retry with a new transaction UID", () => {
            var s = new Source(); var h = new Http(s.Id,"iTimologia") { Decline = true }; var service = Service(h);
            var first = service.Send(s.Id,Route(),s); Equal("Declined",first.Payment.State);
            Equal("Declined",service.Send(s.Id,null,null).Payment.State); Equal(1,h.Authorizations);
            h.Decline = false; var next = service.Send(s.Id,null,null,true); Accepted(next); Equal(2,h.Authorizations);
            Check(next.Payment.TransactionUid != first.Payment.TransactionUid,"Retry reused charge identity"); Equal(1,s.Loads);
        });
        foreach(string key in new[] { "iTimologia", "Ilyda.Direct" }) Test(key+" uncertain invoice retains approval and never voids or charges again", () => {
            var s = new Source(); var h = new Http(s.Id,key) { UnknownInvoice = true }; var service = Service(h);
            var first = service.Send(s.Id,Route(key),s); Equal("Approved",first.Payment.State); Check(!first.Submission.Accepted,"Unknown accepted");
            service.Send(s.Id,null,null); Equal(1,h.Authorizations); Equal(1,h.Invoices); Equal(0,h.Voids);
            Throws(() => service.VoidAcceptedPayment(s.Id,"test")); Equal(0,h.Voids);
        });
        Test("unknown iTimologia issuance is recovered and marks existing payment invoiced", () => {
            var s = new Source(); var h = new Http(s.Id,"iTimologia") { UnknownInvoice = true }; var service = Service(h);
            service.Send(s.Id,Route(),s); h.UnknownInvoice = false; var r = service.Recover(s.Id); Accepted(r); Equal("Invoiced",r.Payment.State);
            Equal(1,h.Authorizations); Equal(1,h.Invoices); Equal(0,h.Voids);
        });
        foreach(string key in new[] { "iTimologia", "Ilyda.Direct" }) Test(key+" interrupted provider signature cannot create a second session", () => {
            var s = new Source(); var h = new Http(s.Id,key) { BadSignature = true }; var service = Service(h);
            Throws(() => service.Send(s.Id,Route(key),s)); Equal("SignaturePending",service.Send(s.Id,null,null).Payment.State);
            Equal(1,h.Signatures); Equal(0,h.Authorizations);
        });
        foreach(string state in new[] { "SignaturePending", "SignatureReady", "Pending", "Approved", "Invoiced" }) Test("SQL failure at "+state+" does not duplicate payment", () => {
            var s = new Source(); var h = new Http(s.Id,"iTimologia"); var service = Service(h);
            Execute(connection,"CREATE TRIGGER dbo.TestPaymentFailure ON dbo.FiscalAttemptRevision AFTER INSERT AS IF EXISTS (SELECT 1 FROM inserted WHERE State='"+state+"') BEGIN RAISERROR('synthetic persistence failure',16,1); ROLLBACK TRANSACTION; END;");
            try { Throws(() => service.Send(s.Id,Route(),s)); } finally { Execute(connection,"DROP TRIGGER dbo.TestPaymentFailure;"); }
            if(state == "SignaturePending") { Equal(0,h.Signatures); Equal(0,h.Authorizations); }
            if(state == "SignatureReady") { Equal(1,h.Signatures); Equal("SignaturePending",service.Recover(s.Id).Payment.State); Equal(0,h.Authorizations); }
            if(state == "Pending") { Equal("SignatureReady",service.Recover(s.Id).Payment.State); Equal(0,h.Authorizations); Accepted(service.Send(s.Id,null,null)); }
            if(state == "Approved" || state == "Invoiced") { service.Recover(s.Id); Equal(1,h.Authorizations); }
        });
        Test("missing legacy payment metadata blocks route switching", () => {
            var s = new Source(); using(database.EnterInvoice(s.Id)) new SqlProviderPaymentStore(database,"legacy").GetOrCreate(new ProviderPaymentAttemptRequest {
                InvoiceId = s.Id,Gateway = "GBridge",InvoiceProvider = "iTimologia",ProviderId = 34,PaymentType = 0,TerminalId = "TID-1",Amount = 124,AmountMinorUnits = 12400 });
            var h = new Http(s.Id,"iTimologia"); Throws(() => Service(h).Send(s.Id,Route(),s)); Equal(0,h.Calls);
        });
        Test("legacy printed evidence blocks POS before signature", () => {
            var s = new Source(); s.Invoice.IsPrinted = true; var h = new Http(s.Id,"iTimologia"); Throws(() => Service(h).Send(s.Id,Route(),s)); Equal(0,h.Calls);
        });
        Test("legacy submission evidence blocks POS before signature", () => {
            var s = new Source { Legacy = true }; var h = new Http(s.Id,"iTimologia"); Throws(() => Service(h).Send(s.Id,Route(),s)); Equal(0,h.Calls);
        });
        foreach (int rejectionCode in new[] { 401, 422, 452 })
        Test("rejected invoice " + rejectionCode + " retries reuse the already approved payment", () => {
            var s = new Source(); var h = new Http(s.Id,"iTimologia") { RejectInvoice = true, RejectionCode = rejectionCode }; var service = Service(h);
            var r = service.Send(s.Id,Route(),s); Equal("Rejected",r.Submission.State); Equal("Approved",r.Payment.State);
            h.RejectInvoice = false; Accepted(service.Send(s.Id,null,null,retryRejectedInvoice:true)); Equal(1,h.Authorizations); Equal(1,h.Signatures);
        });
        foreach(string state in new[] { "Processing", "Accepted" }) Test("void SQL failure at "+state+" preserves lookup-only recovery", () => {
            var s = new Source(); var h = new Http(s.Id,"iTimologia") { RejectInvoice = true }; var service = Service(h); service.Send(s.Id,Route(),s);
            Execute(connection,"CREATE TRIGGER dbo.TestVoidFailure ON dbo.FiscalAttemptRevision AFTER INSERT AS IF EXISTS (SELECT 1 FROM inserted i JOIN dbo.FiscalAttempt a ON a.AttemptId=i.AttemptId WHERE a.Operation='Void' AND i.State='"+state+"') BEGIN RAISERROR('synthetic void save failure',16,1); ROLLBACK TRANSACTION; END;");
            try { Throws(() => service.VoidAcceptedPayment(s.Id,"test")); } finally { Execute(connection,"DROP TRIGGER dbo.TestVoidFailure;"); }
            if(state == "Processing") { Equal(0,h.Voids); Equal(FiscalPaymentState.Prepared,service.RecoverVoid(s.Id).State); Equal(FiscalPaymentState.Accepted,service.VoidAcceptedPayment(s.Id,"resume saved request").State); }
            else { Equal(FiscalPaymentState.Accepted,service.RecoverVoid(s.Id).State); }
            Equal(1,h.Voids); Equal(1,h.Authorizations);
        });
        Test("explicit void is separately journalled and never reauthorizes", () => {
            var s = new Source(); var h = new Http(s.Id,"iTimologia") { RejectInvoice = true }; var service = Service(h);
            Equal("Approved",service.Send(s.Id,Route(),s).Payment.State); Equal(0,h.Voids);
            var op = service.VoidAcceptedPayment(s.Id,"Customer cancelled after rejection"); Equal(FiscalPaymentState.Accepted,op.State); Equal("Void",op.Operation);
            Equal(1,h.Voids); Equal(op.Id,service.RecoverVoid(s.Id).Id); Equal(1,h.Voids);
            Throws(() => service.Send(s.Id,null,null,true)); Equal(1,h.Authorizations);
        });
        Test("uncertain void uses lookup-only recovery and checks original transaction ID", () => {
            var s = new Source(); var h = new Http(s.Id,"iTimologia") { RejectInvoice = true, UnknownVoid = true }; var service = Service(h);
            service.Send(s.Id,Route(),s); Equal(FiscalPaymentState.Unknown,service.VoidAcceptedPayment(s.Id,"test").State); Equal(1,h.Voids);
            Equal(FiscalPaymentState.Unknown,service.RecoverVoid(s.Id).State); Equal(1,h.Voids);
            h.UnknownVoid = false; h.VoidWrongId = true; Equal(FiscalPaymentState.Unknown,service.RecoverVoid(s.Id).State);
            h.VoidWrongId = false; Equal(FiscalPaymentState.Accepted,service.RecoverVoid(s.Id).State); Equal(1,h.Voids);
        });
        Test("accepted invoice cannot be voided through failure compensation", () => {
            var s = new Source(); var h = new Http(s.Id,"iTimologia"); var service = Service(h); Accepted(service.Send(s.Id,Route(),s));
            Throws(() => service.VoidAcceptedPayment(s.Id,"test")); Equal(0,h.Voids);
        });
        foreach(string key in new[] { "iTimologia", "Ilyda.Direct" }) Test(key+" full refund links original payment and allows only one credit", () => {
            var s = new Source(); var h = new Http(s.Id,key); var service = Service(h); var sale = service.Send(s.Id,Route(key),s); Accepted(sale);
            var credit = new Source { Credit = true }; h.Id = credit.Id;
            var refund = service.Refund(credit.Id,s.Id,credit); Accepted(refund); Equal(ProviderPaymentOperation.Refund,refund.Payment.Operation);
            Equal(sale.Payment.Id,refund.Payment.OriginalPaymentAttemptId.Value); Equal(2,h.Authorizations);
            Equal(1,(int)h.LastAuthorization["TransactionData"]["TxnType"]);
            Accepted(service.Refund(credit.Id,s.Id,null)); Equal(2,h.Authorizations); Equal(1,credit.Loads);
            var other = new Source { Credit = true }; Throws(() => service.Refund(other.Id,s.Id,other)); Equal(2,h.Authorizations);
        });
        Test("unknown refund recovers against original payment without another refund", () => {
            var s = new Source(); var h = new Http(s.Id,"iTimologia"); var service = Service(h); Accepted(service.Send(s.Id,Route(),s));
            var credit = new Source { Credit = true }; h.Id = credit.Id; h.UnknownPayment = true;
            Equal("Unknown",service.Refund(credit.Id,s.Id,credit).Payment.State); h.UnknownPayment = false;
            Equal("Approved",service.Refund(credit.Id,s.Id,null,recoveryOnly:true).Payment.State);
            Accepted(service.Refund(credit.Id,s.Id,null)); Equal(2,h.Authorizations);
        });
        Test("credit without original reference cannot start a refund", () => {
            var s = new Source { Credit = true }; var h = new Http(s.Id,"iTimologia"); Throws(() => Service(h).Send(s.Id,Route(),s)); Equal(0,h.Calls);
        });
        Test("refund rejects a different customer before reservation or transport", () => {
            var s = new Source(); var h = new Http(s.Id,"iTimologia"); var service = Service(h); Accepted(service.Send(s.Id,Route(),s));
            var credit = new Source { Credit = true }; credit.Invoice.Trader.TaxRegistrationNumber = "222222222";
            Throws(() => service.Refund(credit.Id,s.Id,credit)); Equal(1,h.Authorizations);
        });
        Test("refund accepted-save failure recovers the same child and original reference", () => {
            var s = new Source(); var h = new Http(s.Id,"iTimologia"); var service = Service(h); Accepted(service.Send(s.Id,Route(),s));
            var credit = new Source { Credit = true }; h.Id = credit.Id;
            Execute(connection,"CREATE TRIGGER dbo.TestRefundFailure ON dbo.FiscalAttemptRevision AFTER INSERT AS IF EXISTS (SELECT 1 FROM inserted i JOIN dbo.FiscalAttempt a ON a.AttemptId=i.AttemptId WHERE a.DocumentId='"+credit.Id+"' AND i.State='Approved') BEGIN RAISERROR('synthetic refund save failure',16,1); ROLLBACK TRANSACTION; END;");
            try { Throws(() => service.Refund(credit.Id,s.Id,credit)); } finally { Execute(connection,"DROP TRIGGER dbo.TestRefundFailure;"); }
            Equal("Approved",service.Refund(credit.Id,s.Id,null,recoveryOnly:true).Payment.State);
            Accepted(service.Refund(credit.Id,s.Id,null)); Equal(2,h.Authorizations); Equal(1,credit.Loads);
        });
        Test("refund reservation survives a crash before child creation without remapping credit", () => {
            var s = new Source(); var h = new Http(s.Id,"iTimologia"); var service = Service(h); Accepted(service.Send(s.Id,Route(),s));
            var credit = new Source { Credit = true }; h.Id = credit.Id;
            Execute(connection,"CREATE TRIGGER dbo.TestChildFailure ON dbo.FiscalAttempt AFTER INSERT AS IF EXISTS (SELECT 1 FROM inserted WHERE DocumentId='"+credit.Id+"') BEGIN RAISERROR('synthetic child creation failure',16,1); ROLLBACK TRANSACTION; END;");
            try { Throws(() => service.Refund(credit.Id,s.Id,credit)); } finally { Execute(connection,"DROP TRIGGER dbo.TestChildFailure;"); }
            credit.Invoice.TotalAmount = 999;
            Accepted(service.Refund(credit.Id,s.Id,null)); Equal(2,h.Authorizations); Equal(1,credit.Loads);
        });
        Test("expired Ilyda signature cannot dispatch payment", () => {
            var s = new Source(); var h = new Http(s.Id,"Ilyda.Direct") { ExpiredSignature = true };
            Throws(() => Service(h).Send(s.Id,Route(h.Key),s)); Equal(0,h.Authorizations); Equal(0,h.Invoices);
        });
        Test("gateway evidence failure prevents charge and remains unknown", () => {
            var s = new Source(); var h = new Http(s.Id,"iTimologia");
            Execute(connection,"CREATE TRIGGER dbo.TestEvidenceFailure ON dbo.FiscalHttpExchange AFTER INSERT AS IF EXISTS (SELECT 1 FROM inserted WHERE RequestJson LIKE '%gateway.invalid%') BEGIN RAISERROR('synthetic request evidence failure',16,1); ROLLBACK TRANSACTION; END;");
            try { Equal("Unknown",Service(h).Send(s.Id,Route(),s).Payment.State); } finally { Execute(connection,"DROP TRIGGER dbo.TestEvidenceFailure;"); }
            Equal(0,h.Authorizations); Equal(0,h.Invoices);
        });
        Test("concurrent callers cannot charge twice", () => {
            var s = new Source(); var h = new Http(s.Id,"iTimologia"); using(var entered = new ManualResetEventSlim()) using(var release = new ManualResetEventSlim()) {
                h.BeforeCharge = () => { entered.Set(); Check(release.Wait(10000),"timeout"); };
                var running = Task.Run(() => Service(h).Send(s.Id,Route(),s));
                try { Check(entered.Wait(10000),"charge never entered"); Throws(() => Service(h).Send(s.Id,Route(),s)); } finally { release.Set(); }
                Accepted(running.GetAwaiter().GetResult()); Equal(1,h.Authorizations);
            }
        });
    }
    sealed class Credentials : IProviderCredentialsResolver
    { public ProviderCredentials Resolve(ProviderRoute route) => new ProviderCredentials { MasterToken = "SECRET-token",ApiKey = "SECRET-key" }; }
    sealed class Http : HttpMessageHandler
    {
        public Guid Id; public string Key; public int Calls,Signatures,Authorizations,Invoices,Voids,Lookups,Cancellations;
        public Func<JObject,HttpResponseMessage> GatewayOverride;
        public bool FailCancellation;
        public bool RejectSignature;
        public bool UnknownPayment,UnknownInvoice,Decline,BadSignature,RejectInvoice,UnknownVoid,VoidWrongId,ExpiredSignature; public string Mismatch;
        public int RejectionCode = 401;
        public JObject LastAuthorization,LastSignature,LastInvoice; public Action BeforeCharge;
        private readonly Dictionary<string,JObject> transactions = new Dictionary<string,JObject>();
        private readonly HashSet<string> voided = new HashSet<string>();
        public Http(Guid id,string key) { Id = id; Key = key; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            Calls++; var path = request.RequestUri.AbsolutePath; var raw = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            if(request.RequestUri.Host == "gateway.invalid") return Task.FromResult(Gateway(JObject.Parse(raw),raw));
            if(path.Contains("/tokens/")) return Task.FromResult(Reply("{\"token\":\"SECRET-communication\",\"expires_at\":\"2099-01-01T00:00:00Z\"}"));
            if(path.EndsWith("/cancel")) {
                Cancellations++; CheckLogged(raw);
                if(FailCancellation) return Task.FromResult(Reply("{}",500));
                return Task.FromResult(Reply(new JObject { ["invoiceSignatures"] = new JArray(new JObject {
                    ["signature"] = JObject.Parse(raw)["signature"], ["cancelled"] = true }) }.ToString()));
            }
            if(path.Contains("signature") || path.EndsWith("/sign")) {
                Signatures++; LastSignature = JObject.Parse(raw); Equal("SignaturePending",LatestState(Id)); CheckLogged(raw);
                if(BadSignature) return Task.FromResult(Reply("broken"));
                if(RejectSignature) return Task.FromResult(Reply(Key == "iTimologia" ? "{\"message\":\"Invalid signing settings\"}" : "{\"invoiceSignatures\":[],\"errors\":[{\"fatal\":true,\"code\":\"INVALID\"}]}",422));
                if(Key == "iTimologia") {
                    var names = (int)LastSignature["acq_id"] == 0 ? new[] {"amount","total_net_value","total_vat_amount","total_gross_value"} : new[] {"total_net_value","total_vat_amount","total_gross_value","amount"};
                    string input = "uid;date;mark;" + string.Join(";",names.Select(n => (decimal.Parse((string)LastSignature[n],CultureInfo.InvariantCulture)*100).ToString("0",CultureInfo.InvariantCulture))) + ";"+(string)LastSignature["tid"];
                    return Task.FromResult(Reply(new JObject { ["uid"] = "provider-uid",["signature"] = "signature",["input_formatted_values"] = input }.ToString()));
                }
                var sig = (JObject)LastSignature.DeepClone(); sig["uid"] = "provider-uid";
                sig["signature"] = (string)LastSignature["nspProtocol"] == "DEFAULT" ? "AQID" : "010203"; sig["signedContent"] = "signed-input";
                sig["signingAuthor"] = "008"; sig["signatureExpirationDate"] = ExpiredSignature ? "2000-01-01T00:00:00Z" : "2099-01-01T00:00:00Z";
                return Task.FromResult(Reply(new JObject { ["invoiceSignatures"] = new JArray(sig) }.ToString()));
            }
            if(request.Method == HttpMethod.Get) {
                if(UnknownInvoice) return Task.FromResult(Reply("{}",404));
                return Task.FromResult(Reply(TimReply(Id)));
            }
            Invoices++; Equal("Approved",LatestState(Id)); CheckLogged(raw); LastInvoice = JObject.Parse(raw);
            if(UnknownInvoice) throw new HttpRequestException("synthetic lost reply");
            if(RejectInvoice) return Task.FromResult(Reply("{\"message\":\"Synthetic definitive rejection\"}",RejectionCode));
            return Task.FromResult(Reply(Key == "iTimologia" ? TimReply(Id) : "{\"invoiceMarking\":{\"invoiceIdentifier\":\"UID\",\"invoiceId\":\"provider-id\",\"mark\":\"900010\",\"verificationHash\":\"HASH\"},\"errors\":[]}"));
        }
        private HttpResponseMessage Gateway(JObject request,string raw)
        {
            var kind = (string)request["InputType"]; var uid = (string)request["TransUid"]; CheckLogged(raw);
            if(kind == "1") {
                Authorizations++; Equal("Pending",LatestState(Id)); LastAuthorization = (JObject)request.DeepClone(); transactions[uid] = LastAuthorization;
                BeforeCharge?.Invoke(); if(UnknownPayment) throw new HttpRequestException("synthetic payment timeout");
            }
            else if(kind == "2") { Voids++; Equal("Processing",OperationState(Id)); voided.Add(uid); if(UnknownVoid) throw new HttpRequestException("synthetic void timeout"); }
            else { Lookups++; Check(kind == "3" || kind == "4","Unexpected gateway operation"); }
            var custom = GatewayOverride?.Invoke(request); if(custom != null) return custom;
            if((kind == "3" || kind == "4") && (UnknownPayment || UnknownVoid)) return Reply("{\"Status\":\"200\",\"ReplyCode\":\"0E\",\"EFTReplyData\":[]}");
            var original = transactions[uid];
            var data = new JObject { ["TID"] = "TID-1",["Approved"] = !Decline,["Voided"] = voided.Contains(uid),["Amount"] = 12400,
                ["TxnType"] = original["TransactionData"]["TxnType"], ["PaymentType"] = original["TransactionData"]["PaymentType"], ["CustomerReference"] = uid };
            if(Mismatch == "terminal") data["TID"] = "wrong";
            if(Mismatch == "amount") data["Amount"] = 1;
            if(Mismatch == "type") data["TxnType"] = 99;
            if(Mismatch == "payment") data["PaymentType"] = 99;
            if(Mismatch == "missing-type") data.Remove("TxnType");
            if(Mismatch == "missing-payment") data.Remove("PaymentType");
            return Reply(new JObject { ["InputType"] = kind,["TransUid"] = Mismatch == "uid" ? "wrong" : uid,["Status"] = "200",["ReplyCode"] = "00",["EftposReplyCode"] = "200",
                ["EFTReplyData"] = new JArray(new JObject { ["EftposTranId"] = VoidWrongId && voided.Contains(uid) ? "wrong-original" : "transaction-"+uid,["TransactionData"] = data }) }.ToString());
        }
    }
    static string LatestState(Guid id) => Scalar("SELECT TOP (1) r.State FROM dbo.FiscalAttempt a JOIN dbo.FiscalAttemptRevision r ON r.AttemptId=a.AttemptId AND r.Revision=a.CurrentRevision WHERE a.DocumentId='"+id+"' AND a.Family='Payment' AND a.Operation='Payment' ORDER BY a.AttemptSequence DESC") as string;
    static string OperationState(Guid id) => Scalar("SELECT TOP (1) r.State FROM dbo.FiscalAttempt a JOIN dbo.FiscalAttemptRevision r ON r.AttemptId=a.AttemptId AND r.Revision=a.CurrentRevision WHERE a.DocumentId='"+id+"' AND a.Family='Payment' AND a.Operation='Void' ORDER BY a.AttemptSequence DESC") as string;
    static void CheckLogged(string raw) {
        using(var c = new SqlConnection(connection)) using(var cmd = c.CreateCommand()) { c.Open(); cmd.CommandText = "SELECT RequestJson FROM dbo.FiscalHttpExchange WHERE ResponseJson IS NULL";
            using(var r = cmd.ExecuteReader()) { bool found = false; while(r.Read()) if((string)JObject.Parse(r.GetString(0))["Body"] == raw) found = true; Check(found,"Exact HTTP body was not saved before transport"); } }
    }
    static string TimReply(Guid id) => "{\"data\":{\"ext_system_id\":\""+id.ToString("N")+"\",\"uid\":\"UID\",\"mark\":\"900010\",\"authentication_code\":\"HASH\"}}";
    static HttpResponseMessage Reply(string raw,int status = 200) => new HttpResponseMessage((HttpStatusCode)status) { Content = new StringContent(raw,Encoding.UTF8,"application/json") };
    static object Scalar(string sql) { using(var c = new SqlConnection(connection)) using(var cmd = c.CreateCommand()) { c.Open(); cmd.CommandText = sql; return cmd.ExecuteScalar(); } }
    static void Execute(string cs,string sql) { using(var c = new SqlConnection(cs)) using(var cmd = c.CreateCommand()) { c.Open(); cmd.CommandText = sql; cmd.ExecuteNonQuery(); } }
    static void Accepted(ProviderPaymentResult result) { Check(result.Submission?.Accepted == true,"Not accepted: "+result.Payment?.State+" / "+result.Submission?.State+" / "+string.Join(";",result.Submission?.Response?.Errors ?? new string[0])); }
    static void Throws(Action action) { try { action(); } catch(InvalidOperationException) { return; } catch(ArgumentException) { return; } catch(SqlException) { return; } catch(JsonException) { return; } throw new Exception("Expected rejection"); }
    static void Equal<T>(T expected,T actual) { if(!object.Equals(expected,actual)) throw new Exception("Expected "+expected+", got "+actual); }
    static void Check(bool value,string message) { if(!value) throw new Exception(message); }
    static void Test(string name,Action test) { var filter = Environment.GetEnvironmentVariable("ASFC_PAYMENT_TEST_FILTER"); if (!string.IsNullOrEmpty(filter) && !Regex.IsMatch(name,filter,RegexOptions.IgnoreCase)) return; try { test(); passed++; Console.WriteLine("PASS "+name); } catch(Exception ex) { failed++; Console.WriteLine("FAIL "+name+": "+ex); } }
}
