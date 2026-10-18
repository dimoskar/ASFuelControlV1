using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Data.SqlClient;
using ASFuelControl.Invoicing;
using ASFuelControl.Invoicing.Persistence;
using Exedron.Invoicing.Adapters;
using Exedron.Invoicing.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static partial class Program
{
    static void ExtendedRecoveryTests()
    {
        foreach (string key in new[] { "iTimologia", "Ilyda.Direct" })
        foreach (string field in new[] { "TxnType", "PaymentType", "TID", "Amount" })
        Test(key + " incomplete approval " + field + " recovers same UID", () => {
            var s = new Source(); var h = new Http(s.Id,key);
            h.GatewayOverride = r => { var body = ApprovalBody(r,h.LastAuthorization); ((JObject)body["EFTReplyData"][0]["TransactionData"]).Remove(field); return Reply(body.ToString()); };
            var service = Service(h); var first = service.Send(s.Id,Route(key),s);
            Equal("Unknown",first.Payment.State); Equal("IncompleteResponse",first.Payment.ErrorCode);
            Check(first.Payment.ErrorMessage.Contains(field),"Missing field is not explained");
            h.GatewayOverride = null;
            var recovered = service.Recover(s.Id); Equal("Approved",recovered.Payment.State); Equal(first.Payment.TransactionUid,recovered.Payment.TransactionUid);
            Accepted(service.Send(s.Id,null,null)); Equal(1,h.Authorizations); Equal(1,h.Signatures);
        });
        Test("conflicting amount plus missing type stays mismatch", () => {
            var s = new Source(); var h = new Http(s.Id,"iTimologia");
            h.GatewayOverride = r => { var body = ApprovalBody(r,h.LastAuthorization); var data = (JObject)body["EFTReplyData"][0]["TransactionData"];
                data.Remove("TxnType"); data["Amount"] = 1; return Reply(body.ToString()); };
            Equal("Mismatch",Service(h).Send(s.Id,Route(),s).Payment.State);
            h.GatewayOverride = null; Equal("Mismatch",Service(h).Recover(s.Id).Payment.State); Equal(1,h.Authorizations); Equal(0,h.Lookups);
        });
        Test("incomplete database lookup continues to terminal lookup without reauthorization", () => {
            var s = new Source(); var h = new Http(s.Id,"iTimologia") { UnknownPayment = true }; var service = Service(h);
            service.Send(s.Id,Route(),s); h.UnknownPayment = false;
            h.GatewayOverride = r => { if((string)r["InputType"] != "4") return null;
                var body = ApprovalBody(r,h.LastAuthorization); ((JObject)body["EFTReplyData"][0]["TransactionData"]).Remove("PaymentType"); return Reply(body.ToString()); };
            int lookups = h.Lookups; Equal("Approved",service.Recover(s.Id).Payment.State); Equal(lookups+2,h.Lookups); Equal(1,h.Authorizations);
        });
        Test("decline with missing payment type remains incomplete until lookup confirms it", () => {
            var s = new Source(); var h = new Http(s.Id,"iTimologia") { Decline = true };
            h.GatewayOverride = r => { var body = ApprovalBody(r,h.LastAuthorization); var data = (JObject)body["EFTReplyData"][0]["TransactionData"];
                data["Approved"] = false; data.Remove("PaymentType"); return Reply(body.ToString()); };
            var first = Service(h).Send(s.Id,Route(),s); Equal("Unknown",first.Payment.State); Equal("IncompleteResponse",first.Payment.ErrorCode);
            Equal("Unknown",Service(h).Recover(s.Id).Payment.State); Equal(1,h.Authorizations);
            h.GatewayOverride = null; Equal("Declined",Service(h).Recover(s.Id).Payment.State); Equal(1,h.Authorizations);
        });
        foreach(string field in new[] { "EftposTranId", "TransUid", "Approved" })
        Test("missing response identity " + field + " remains lookup-only", () => {
            var s = new Source(); var h = new Http(s.Id,"iTimologia");
            h.GatewayOverride = r => { var body = ApprovalBody(r,h.LastAuthorization);
                if(field == "TransUid") body.Remove(field);
                else if(field == "Approved") ((JObject)body["EFTReplyData"][0]["TransactionData"]).Remove(field);
                else ((JObject)body["EFTReplyData"][0]).Remove(field);
                return Reply(body.ToString()); };
            var first = Service(h).Send(s.Id,Route(),s); Equal("Unknown",first.Payment.State); Check(first.Payment.ErrorMessage.Contains(field),"Missing identity field is not explained");
            h.GatewayOverride = null; Equal("Approved",Service(h).Recover(s.Id).Payment.State); Equal(1,h.Authorizations);
        });
        Test("old missing-field mismatch is repaired from retained evidence", () => {
            var s = new Source(); var h = new Http(s.Id,"iTimologia") { Mismatch = "missing-type" }; var service = Service(h);
            var first = service.Send(s.Id,Route(),s);
            RewriteLatestSnapshot(s.Id,"Payment",json => { json["Attempt"]["State"] = "Mismatch"; json["ErrorCode"] = "Mismatch"; },"Mismatch");
            h.Mismatch = null; Equal("Approved",service.Recover(s.Id).Payment.State); Equal(1,h.Authorizations);
        });
        foreach (string key in new[] { "iTimologia", "Ilyda.Direct" })
        Test(key + " saved approval survives SQL failure and offline gateway", () => {
            var s = new Source(); var h = new Http(s.Id,key); var service = Service(h);
            Execute(connection,"CREATE TRIGGER dbo.TestRestoreApproval ON dbo.FiscalAttemptRevision AFTER INSERT AS IF EXISTS(SELECT 1 FROM inserted WHERE State='Approved') BEGIN RAISERROR('test',16,1); ROLLBACK TRANSACTION; END;");
            try { Throws(() => service.Send(s.Id,Route(key),s)); } finally { Execute(connection,"DROP TRIGGER dbo.TestRestoreApproval;"); }
            int calls = h.Calls; h.UnknownPayment = true;
            Equal("Approved",service.Recover(s.Id).Payment.State); Equal(calls,h.Calls); Equal(0,h.Lookups);
            Accepted(service.Send(s.Id,null,null)); Equal(1,h.Authorizations);
        });
        foreach (string key in new[] { "iTimologia", "Ilyda.Direct" })
        Test(key + " database short reference recovers approval", () => {
            var s = new Source(); var h = new Http(s.Id,key) { UnknownPayment = true }; var service = Service(h);
            service.Send(s.Id,Route(key),s); h.UnknownPayment = false;
            h.GatewayOverride = r => { var body = ApprovalBody(r,h.LastAuthorization); body["EFTReplyData"][0]["TransactionData"]["CustomerReference"] = ((string)r["TransUid"]).Substring(0,8); return Reply(body.ToString()); };
            int lookups = h.Lookups; Equal("Approved",service.Recover(s.Id).Payment.State); Equal(lookups+1,h.Lookups);
            Accepted(service.Send(s.Id,null,null)); Equal(1,h.Authorizations);
        });
        foreach (string mutation in new[] { "valid", "wrong-reference", "wrong-uid", "wrong-tid", "wrong-amount", "missing-type", "missing-payment", "multiple", "terminal-lookup", "void", "extra-auth" })
        Test("recorded short approval guard " + mutation, () => {
            var uid = Guid.NewGuid().ToString("N");
            var request = new JObject { ["InputType"] = "4",["TransUid"] = uid,["Tid"] = "TID-1" };
            var body = ApprovalBody(request,null); var data = (JObject)body["EFTReplyData"][0]["TransactionData"];
            data["CustomerReference"] = uid.Substring(0,8);
            if(mutation == "wrong-reference") data["CustomerReference"] = "ffffffff";
            if(mutation == "wrong-uid") body["TransUid"] = "other";
            if(mutation == "wrong-tid") data["TID"] = "other";
            if(mutation == "wrong-amount") data["Amount"] = 100;
            if(mutation == "missing-type") data.Remove("TxnType");
            if(mutation == "missing-payment") data.Remove("PaymentType");
            if(mutation == "multiple") ((JArray)body["EFTReplyData"]).Add(body["EFTReplyData"][0].DeepClone());
            if(mutation == "terminal-lookup") { request["InputType"] = "3"; body["InputType"] = "3"; }
            var events = new List<PaymentGatewayEvent> { new PaymentGatewayEvent { TransactionUid = uid,InputType = (string)request["InputType"],HttpStatus = 200,RequestJson = request.ToString(),ResponseJson = body.ToString() } };
            if(mutation == "void") events.Add(new PaymentGatewayEvent { TransactionUid = uid,InputType = "2" });
            if(mutation == "extra-auth") { events.Add(new PaymentGatewayEvent { TransactionUid = uid,InputType = "1" }); events.Add(new PaymentGatewayEvent { TransactionUid = uid,InputType = "1" }); }
            var result = GBridgeClient.ReadRecordedOutcome(new ProviderPaymentAttempt { TransactionUid = uid,TerminalId = "TID-1",AmountMinorUnits = 12400 },events);
            Equal(mutation == "valid",result?.Approval != null);
        });
        foreach (string key in new[] { "iTimologia", "Ilyda.Direct" })
        Test(key + " saved F4 F2 0E waits for operator retry", () => {
            var s = new Source(); var h = new Http(s.Id,key) { UnknownPayment = true }; var service = Service(h);
            var first = service.Send(s.Id,Route(key),s);
            RewriteEvents(first.Payment.TransactionUid,e => { if(e.InputType == "SIGNATURE") return;
                var response = F4Sequence(JObject.Parse(e.RequestJson)); e.ResponseJson = response.Content.ReadAsStringAsync().Result; e.HttpStatus = (int)response.StatusCode; });
            h.UnknownPayment = false;
            Equal(first.Payment.Id,service.GetRetryablePaymentAttemptId(s.Id).Value);
            Equal("Unknown",service.Send(s.Id,null,null).Payment.State); Equal(1,h.Authorizations); Equal(2,h.Lookups);
            var retried = service.RetryPayment(s.Id,first.Payment.Id); Equal("Approved",retried.Payment.State); Equal(2,h.Authorizations); Equal(2,h.Lookups);
            using(var lease = database.EnterInvoice(s.Id)) { var old = new SqlFiscalHistoryStore(database).Load(lease,FiscalJournalFamily.Payment).First();
                Equal("GBridge.F4-F2-0E.v1",(string)JObject.Parse(old.SnapshotJson)["OperatorRetry"]["Policy"]); }
        });
        foreach (string outcome in new[] { "approved", "mismatch", "incomplete" })
        Test("F4 recovery " + outcome + " removes permission for a replacement", () => {
            var s = new Source(); var h = new Http(s.Id,"iTimologia") { GatewayOverride = F4Sequence }; var service = Service(h);
            var first = service.Send(s.Id,Route(),s); Equal(first.Payment.Id,service.GetRetryablePaymentAttemptId(s.Id).Value);
            h.GatewayOverride = r => { var body = ApprovalBody(r,h.LastAuthorization); var data = body["EFTReplyData"][0]["TransactionData"];
                if(outcome == "mismatch") data["Amount"] = 1;
                if(outcome == "incomplete") ((JObject)data).Remove("PaymentType");
                return Reply(body.ToString()); };
            var recovered = service.Recover(s.Id);
            Equal(outcome == "approved" ? "Approved" : outcome == "mismatch" ? "Mismatch" : "Unknown",recovered.Payment.State);
            Check(!service.GetRetryablePaymentAttemptId(s.Id).HasValue,"New transaction evidence did not revoke retry eligibility");
            Throws(() => service.RetryPayment(s.Id,first.Payment.Id));
            Equal(1,h.Authorizations); Equal(1,h.Signatures); Equal(0,h.Invoices);
        });
        foreach (string key in new[] { "iTimologia", "Ilyda.Direct" })
        Test(key + " old prepared automatic replacement waits for explicit operator action", () => {
            var s = new Source(); var h = new Http(s.Id,key) { GatewayOverride = F4Sequence }; var service = Service(h);
            var first = service.Send(s.Id,Route(key),s); ProviderPaymentAttempt prepared;
            using(var lease = database.EnterInvoice(s.Id)) {
                var old = new SqlFiscalHistoryStore(database).Load(lease,FiscalJournalFamily.Payment).Last();
                var request = JsonConvert.DeserializeObject<ProviderPaymentAttemptRequest>(Encoding.UTF8.GetString(old.RequestPayload));
                prepared = new SqlProviderPaymentStore(database,old.Scope).TryCreateOperatorRetry(request,first.Payment.Id,
                    (a,e) => GBridgeClient.CanRetryAfterF4(a.TransactionUid,a.TerminalId,e,a.ProviderInput,(int)a.Operation,a.PaymentType,a.ProviderId));
            }
            RewriteLatestSnapshot(s.Id,"Payment",j => { j["Attempt"]["AutomaticRetryOfAttemptId"] = j["Attempt"]["OperatorRetryOfAttemptId"]; ((JObject)j["Attempt"]).Remove("OperatorRetryOfAttemptId"); });
            RewriteLatestSnapshot(s.Id,"Payment",j => { j["AutomaticRetry"] = j["OperatorRetry"]; j.Remove("OperatorRetry"); },attemptId:first.Payment.Id);
            int calls = h.Calls;
            Equal("Prepared",service.Recover(s.Id).Payment.State); Equal("Prepared",service.Send(s.Id,null,null).Payment.State); Equal(calls,h.Calls);
            Equal(prepared.Id,service.GetRetryablePaymentAttemptId(s.Id).Value);
            h.GatewayOverride = null; Equal("Approved",service.RetryPayment(s.Id,prepared.Id).Payment.State);
            Equal(2,h.Authorizations); Equal(2,h.Signatures); Equal(0,h.Invoices);
        });
        foreach (string key in new[] { "iTimologia", "Ilyda.Direct" })
        Test(key + " interrupted signed operator replacement cannot be dispatched by recovery or polling", () => {
            var s = new Source(); var h = new Http(s.Id,key) { GatewayOverride = F4Sequence }; var service = Service(h);
            var first = service.Send(s.Id,Route(key),s);
            Execute(connection,"CREATE TRIGGER dbo.TestBeforeRetryCharge ON dbo.FiscalAttemptRevision AFTER INSERT AS IF EXISTS(SELECT 1 FROM inserted WHERE State='Pending') BEGIN RAISERROR('test',16,1); ROLLBACK TRANSACTION; END;");
            try { Throws(() => service.RetryPayment(s.Id,first.Payment.Id)); } finally { Execute(connection,"DROP TRIGGER dbo.TestBeforeRetryCharge;"); }
            Guid prepared = service.GetRetryablePaymentAttemptId(s.Id).Value; int calls = h.Calls;
            Equal("SignatureReady",service.Recover(s.Id).Payment.State); Equal("SignatureReady",service.Send(s.Id,null,null).Payment.State);
            Equal(calls,h.Calls); Equal(1,h.Authorizations);
            h.GatewayOverride = null; var retried = service.RetryPayment(s.Id,prepared); Equal("Approved",retried.Payment.State); Equal(prepared,retried.Payment.Id); Equal(2,h.Authorizations);
        });
        Test("concurrent operator clicks cannot authorize two replacements", () => {
            var s = new Source(); var h = new Http(s.Id,"iTimologia") { GatewayOverride = F4Sequence }; var first = Service(h).Send(s.Id,Route(),s);
            using(var entered = new System.Threading.ManualResetEventSlim()) using(var release = new System.Threading.ManualResetEventSlim()) {
                h.GatewayOverride = null; h.BeforeCharge = () => { entered.Set(); if(!release.Wait(10000)) throw new Exception("retry test timeout"); };
                var running = System.Threading.Tasks.Task.Run(() => Service(h).RetryPayment(s.Id,first.Payment.Id));
                try { Check(entered.Wait(10000),"retry never entered"); Throws(() => Service(h).RetryPayment(s.Id,first.Payment.Id)); } finally { release.Set(); }
                Equal("Approved",running.GetAwaiter().GetResult().Payment.State); Equal(2,h.Authorizations);
            }
        });
        foreach (string mutation in new[] { "valid", "null-data", "absent-data", "wrong-uid", "wrong-tid", "transaction-data", "wrong-http", "wrong-code", "extra-lookup" })
        Test("single F4 policy " + mutation, () => {
            var request = new JObject { ["InputType"] = "1",["TransUid"] = "uid",["Tid"] = "TID-1",["TransactionData"] = new JObject { ["ProviderInput"] = "input",["TxnType"] = 0,["PaymentType"] = 0,["ProviderId"] = 34 } };
            var response = JObject.Parse(F4Sequence(request).Content.ReadAsStringAsync().Result);
            if(mutation == "null-data") response["EFTReplyData"] = null;
            if(mutation == "absent-data") response.Remove("EFTReplyData");
            if(mutation == "wrong-uid") response["TransUid"] = "other";
            if(mutation == "wrong-tid") request["Tid"] = "other";
            if(mutation == "transaction-data") response["EFTReplyData"] = new JArray(new JObject());
            if(mutation == "wrong-code") response["ReplyCode"] = "F0";
            var events = new List<PaymentGatewayEvent> { new PaymentGatewayEvent { InputType = "1",TransactionUid = "uid",HttpStatus = mutation == "wrong-http" ? 500 : 200,RequestJson = request.ToString(),ResponseJson = response.ToString() } };
            if(mutation == "extra-lookup") events.Add(new PaymentGatewayEvent { InputType = "4",TransactionUid = "uid" });
            Equal(new[] { "valid","null-data","absent-data" }.Contains(mutation),GBridgeClient.CanRetryAfterF4("uid","TID-1",events,"input",0,0,34));
        });
        foreach (string key in new[] { "iTimologia", "Ilyda.Direct" })
        Test(key + " definitive signature rejection can explicitly retry", () => {
            var s = new Source(); var h = new Http(s.Id,key) { RejectSignature = true }; var service = Service(h);
            Equal("Failed",service.Send(s.Id,Route(key),s).Payment.State); Equal(0,h.Authorizations);
            Equal("Failed",service.Recover(s.Id).Payment.State); h.RejectSignature = false;
            Accepted(service.Send(s.Id,null,null,retryDeclined:true)); Equal(2,h.Signatures); Equal(1,h.Authorizations);
        });
        foreach (string key in new[] { "iTimologia", "Ilyda.Direct" })
        Test(key + " old rejected SignaturePending is reconciled without a new signature", () => {
            var s = new Source(); var h = new Http(s.Id,key) { RejectSignature = true }; var service = Service(h);
            service.Send(s.Id,Route(key),s);
            RewriteLatestSnapshot(s.Id,"Payment",json => { json["Attempt"]["State"] = "SignaturePending"; },"SignaturePending");
            Equal("Failed",service.Recover(s.Id).Payment.State); Equal(1,h.Signatures); Equal(0,h.Authorizations);
        });
        Test("corrected mode is used after a definitive decline", () => {
            var s = new Source(); var h = new Http(s.Id,"iTimologia") { Decline = true }; var configured = Route();
            var service = new ProviderPaymentService(database,new Credentials(),r => h,r => h,r => configured);
            var first = service.Send(s.Id,configured,s); Equal("Declined",first.Payment.State);
            configured = PaymentRoute.ForMode(configured.Provider,configured.Endpoint,configured.TerminalId,"EDPS"); h.Decline = false;
            var next = service.Send(s.Id,null,null,retryDeclined:true); Accepted(next); Equal(0,(int)h.LastSignature["acq_id"]);
            Check(first.Payment.TransactionUid != next.Payment.TransactionUid,"Correction reused charge UID");
        });
        Test("unknown payment never reads corrected signing settings", () => {
            var s = new Source(); var h = new Http(s.Id,"iTimologia") { UnknownPayment = true }; var calls = 0;
            var service = new ProviderPaymentService(database,new Credentials(),r => h,r => h,r => { calls++; throw new Exception("Must retain uncertain route"); });
            service.Send(s.Id,Route(),s); service.Send(s.Id,null,null,retryDeclined:true); Equal(0,calls); Equal(1,h.Authorizations);
        });
        Test("single F4 refund remains lookup-only", () => {
            var s = new Source(); var h = new Http(s.Id,"iTimologia"); var service = Service(h); Accepted(service.Send(s.Id,Route(),s));
            var credit = new Source { Credit = true }; h.Id = credit.Id; h.GatewayOverride = F4Sequence;
            Equal("Unknown",service.Refund(credit.Id,s.Id,credit).Payment.State); Equal(2,h.Authorizations);
            Equal("Unknown",service.Refund(credit.Id,s.Id,null,recoveryOnly:true).Payment.State); Equal(2,h.Authorizations);
            Check(!service.GetRetryablePaymentAttemptId(credit.Id).HasValue,"Refund enabled the sale retry button");
        });
        foreach (string key in new[] { "iTimologia", "Ilyda.Direct" })
        Test(key + " corrected POS mode replaces unsent signature with retained history", () => {
            var s = new Source(); var h = new Http(s.Id,key); PaymentRoute configured = Route(key);
            var service = new ProviderPaymentService(database,new Credentials(),r => h,r => h,r => configured);
            Execute(connection,"CREATE TRIGGER dbo.TestBeforeCharge ON dbo.FiscalAttemptRevision AFTER INSERT AS IF EXISTS(SELECT 1 FROM inserted WHERE State='Pending') BEGIN RAISERROR('test',16,1); ROLLBACK TRANSACTION; END;");
            try { Throws(() => service.Send(s.Id,configured,s)); } finally { Execute(connection,"DROP TRIGGER dbo.TestBeforeCharge;"); }
            Equal("SignatureReady",LatestState(s.Id)); Equal(0,h.Authorizations);
            configured = PaymentRoute.ForMode(configured.Provider,configured.Endpoint,configured.TerminalId,key == "iTimologia" ? "EDPS" : "MELLON");
            Accepted(service.Send(s.Id,null,null)); Equal(2,h.Signatures); Equal(1,h.Authorizations);
            if(key == "iTimologia") Equal(0,(int)h.LastSignature["acq_id"]); else Equal("MELLON",(string)h.LastSignature["nspProtocol"]);
        });
        Test("Ilyda unused signature that expires is renewed before authorization", () => {
            var s = new Source(); var h = new Http(s.Id,"Ilyda.Direct"); var service = Service(h);
            Execute(connection,"CREATE TRIGGER dbo.TestBeforeCharge ON dbo.FiscalAttemptRevision AFTER INSERT AS IF EXISTS(SELECT 1 FROM inserted WHERE State='Pending') BEGIN RAISERROR('test',16,1); ROLLBACK TRANSACTION; END;");
            try { Throws(() => service.Send(s.Id,Route(h.Key),s)); } finally { Execute(connection,"DROP TRIGGER dbo.TestBeforeCharge;"); }
            RewriteLatestSnapshot(s.Id,"ProviderSession",json => { var response = JObject.Parse((string)json["ResponseJson"]);
                response["invoiceSignatures"][0]["signatureExpirationDate"] = "2000-01-01T00:00:00Z"; json["ResponseJson"] = response.ToString(); });
            Accepted(service.Send(s.Id,null,null)); Equal(2,h.Signatures); Equal(1,h.Authorizations); Equal(0,h.Cancellations);
        });
    }
    static JObject ApprovalBody(JObject request,JObject authorization)
    {
        return new JObject { ["InputType"] = request["InputType"],["TransUid"] = request["TransUid"],["Status"] = "200",["ReplyCode"] = "00",["EftposReplyCode"] = "200",
            ["EFTReplyData"] = new JArray(new JObject { ["EftposTranId"] = "transaction-"+(string)request["TransUid"],["TransactionData"] = new JObject {
                ["Approved"] = true,["Voided"] = false,["TID"] = "TID-1",["Amount"] = 12400,["TxnType"] = authorization?["TransactionData"]?["TxnType"] ?? new JValue(0),
                ["PaymentType"] = authorization?["TransactionData"]?["PaymentType"] ?? new JValue(0),["CustomerReference"] = request["TransUid"] } }) };
    }
    static void RewriteEvents(string uid,Action<PaymentGatewayEvent> edit)
    {
        using(var c = new SqlConnection(connection)) using(var cmd = c.CreateCommand()) {
            c.Open(); cmd.CommandText = "SELECT EventId,EventJson FROM dbo.FiscalPaymentEvent WHERE TransactionUid=@uid"; cmd.Parameters.AddWithValue("@uid",uid);
            var rows = new List<Tuple<Guid,PaymentGatewayEvent>>();
            using(var r = cmd.ExecuteReader()) while(r.Read()) rows.Add(Tuple.Create(r.GetGuid(0),JsonConvert.DeserializeObject<PaymentGatewayEvent>(r.GetString(1))));
            foreach(var row in rows) { edit(row.Item2); using(var update = c.CreateCommand()) { update.CommandText = "UPDATE dbo.FiscalPaymentEvent SET EventJson=@json WHERE EventId=@id";
                update.Parameters.AddWithValue("@json",JsonConvert.SerializeObject(row.Item2)); update.Parameters.AddWithValue("@id",row.Item1); update.ExecuteNonQuery(); } }
        }
    }
    static void RewriteLatestSnapshot(Guid id,string family,Action<JObject> edit,string state = null,Guid? attemptId = null)
    {
        using(var c = new SqlConnection(connection)) using(var cmd = c.CreateCommand()) {
            c.Open(); cmd.CommandText = "SELECT TOP(1) r.AttemptId,r.Revision,r.SnapshotJson FROM dbo.FiscalAttempt a JOIN dbo.FiscalAttemptRevision r ON r.AttemptId=a.AttemptId AND r.Revision=a.CurrentRevision WHERE a.DocumentId=@id AND a.Family=@family" + (attemptId.HasValue ? " AND a.AttemptId=@attempt" : "") + " ORDER BY a.AttemptSequence DESC";
            if(attemptId.HasValue) cmd.Parameters.AddWithValue("@attempt",attemptId.Value);
            cmd.Parameters.AddWithValue("@id",id); cmd.Parameters.AddWithValue("@family",family); Guid attempt; long revision; JObject json;
            using(var r = cmd.ExecuteReader()) { r.Read(); attempt = r.GetGuid(0); revision = r.GetInt64(1); json = JObject.Parse(r.GetString(2)); }
            edit(json); cmd.Parameters.Clear(); cmd.CommandText = "UPDATE dbo.FiscalAttemptRevision SET SnapshotJson=@json"+(state == null ? "" : ",State=@state")+" WHERE AttemptId=@id AND Revision=@revision";
            cmd.Parameters.AddWithValue("@json",json.ToString()); cmd.Parameters.AddWithValue("@id",attempt); cmd.Parameters.AddWithValue("@revision",revision); if(state != null) cmd.Parameters.AddWithValue("@state",state); cmd.ExecuteNonQuery();
        }
    }
}
