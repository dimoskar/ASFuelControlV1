using System;
using System.Linq;
using System.Net.Http;
using ASFuelControl.Invoicing;
using ASFuelControl.Invoicing.Persistence;
using Exedron.Invoicing.Core;
using Exedron.Invoicing.Adapters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static partial class Program
{
    static void RecoveryTests()
    {
        foreach (string key in new[] { "iTimologia", "Ilyda.Direct" })
        foreach (string lookup in new[] { "4", "3" }) Test(key + " recovered decline from " + lookup + " permits explicit retry", () => {
            var s = new Source(); var h = new Http(s.Id,key) { UnknownPayment = true }; var service = Service(h);
            Equal("Unknown",service.Send(s.Id,Route(key),s).Payment.State);
            h.UnknownPayment = false; h.Decline = true;
            h.GatewayOverride = r => (string)r["InputType"] == "4" && lookup == "3" ? EmptyGateway(r,"F2",400) : null;
            var declined = service.Recover(s.Id); Equal("Declined",declined.Payment.State);
            Equal(1,h.Authorizations); Equal(0,h.Invoices); Check(!string.IsNullOrWhiteSpace(declined.Payment.ErrorMessage),"No actionable message");
            Equal(0,h.Cancellations); // Recovery never cancels/replaces a provider signature.
            h.Decline = false; Accepted(service.Send(s.Id,null,null,retryDeclined:true));
            Equal(2,h.Authorizations); Equal(2,h.Signatures); Equal(1,h.Invoices);
        });
        foreach (string code in new[] { "F7", "F0" })
        foreach (int status in new[] { 200,400,401 }) Test("NSP credentials " + code + " HTTP " + status + " is rejected without lookup", () => {
            var s = new Source(); var h = new Http(s.Id,"iTimologia");
            h.GatewayOverride = r => CredentialsReply(r,code,status);
            var result = Service(h).Send(s.Id,Route(),s);
            Equal("Declined",result.Payment.State); Equal("CredentialsRejected",result.Payment.ErrorCode);
            Check(result.Payment.ErrorMessage.Contains("NSP"),"NSP explanation missing"); Equal(0,h.Lookups); Equal(1,h.Authorizations);
        });
        foreach (string key in new[] { "iTimologia", "Ilyda.Direct" })
        foreach (string rejection in new[] { "SignatureRejected", "CredentialsRejected" })
        Test(key + " repairs stored unknown " + rejection + " without another authorization", () => {
            var s = new Source(); var h = new Http(s.Id,key) { UnknownPayment = true }; var service = Service(h);
            var first = service.Send(s.Id,Route(key),s); var request = (JObject)h.LastAuthorization.DeepClone();
            // Emulate a retained authorization response from a version that misclassified the rejection.
            var response = rejection == "CredentialsRejected" ? CredentialsReply(request,"F7",401) : SignatureReply(request);
            var raw = response.Content.ReadAsStringAsync().Result;
            using(var c = new System.Data.SqlClient.SqlConnection(connection)) using(var cmd = c.CreateCommand()) {
                c.Open(); cmd.CommandText = "SELECT EventId,EventJson FROM dbo.FiscalPaymentEvent WHERE TransactionUid=@uid"; cmd.Parameters.AddWithValue("@uid",first.Payment.TransactionUid);
                var events = new System.Collections.Generic.List<Tuple<Guid,PaymentGatewayEvent>>();
                using(var reader = cmd.ExecuteReader()) while(reader.Read()) events.Add(Tuple.Create(reader.GetGuid(0),JsonConvert.DeserializeObject<PaymentGatewayEvent>(reader.GetString(1))));
                foreach(var item in events) {
                    if(item.Item2.InputType == "1") { item.Item2.ResponseJson = raw; item.Item2.HttpStatus = (int)response.StatusCode; }
                    else if(item.Item2.InputType == "3" || item.Item2.InputType == "4") item.Item2.ResponseJson = EmptyGateway(JObject.Parse(item.Item2.RequestJson),"0E").Content.ReadAsStringAsync().Result;
                    using(var update = c.CreateCommand()) { update.CommandText = "UPDATE dbo.FiscalPaymentEvent SET EventJson=@json WHERE EventId=@id";
                        update.Parameters.AddWithValue("@json",JsonConvert.SerializeObject(item.Item2)); update.Parameters.AddWithValue("@id",item.Item1); update.ExecuteNonQuery(); }
                }
            }
            int lookups = h.Lookups; var recovered = service.Recover(s.Id);
            Equal("Declined",recovered.Payment.State); Equal(rejection,recovered.Payment.ErrorCode); Equal(lookups,h.Lookups); Equal(1,h.Authorizations);
            h.UnknownPayment = false; Accepted(service.Send(s.Id,null,null,retryDeclined:true)); Equal(2,h.Authorizations);
            Equal(key == "Ilyda.Direct" ? 1 : 0,h.Cancellations);
        });
        foreach (string key in new[] { "iTimologia", "Ilyda.Direct" }) Test(key + " single F4 waits for explicit retry with audited new UID", () => {
            var s = new Source(); var h = new Http(s.Id,key); string originalUid = null;
            h.GatewayOverride = r => { if(h.Authorizations > 1) return null; originalUid = (string)r["TransUid"]; return F4Sequence(r); };
            var service = Service(h); var first = service.Send(s.Id,Route(key),s); Equal("Unknown",first.Payment.State);
            Equal(first.Payment.Id,service.GetRetryablePaymentAttemptId(s.Id).Value);
            for(int i = 0; i < 3; i++) Equal(first.Payment.Id,service.Send(s.Id,null,null).Payment.Id);
            Equal(1,h.Authorizations); Equal(1,h.Signatures); Equal(0,h.Lookups); Equal(0,h.Invoices); Equal(0,h.Cancellations);
            var result = service.RetryPayment(s.Id,first.Payment.Id); Equal("Approved",result.Payment.State);
            Equal(2,h.Authorizations); Equal(0,h.Lookups); Equal(2,h.Signatures); Equal(0,h.Invoices); Equal(0,h.Voids);
            Throws(() => service.RetryPayment(s.Id,first.Payment.Id)); // Stale click cannot create a third attempt.
            Check(!service.GetRetryablePaymentAttemptId(s.Id).HasValue,"Approved payment offers retry");
            Check(originalUid != result.Payment.TransactionUid,"New payment reused the original UID");
            Equal(key == "Ilyda.Direct" ? 1 : 0,h.Cancellations);
            using(var lease = database.EnterInvoice(s.Id)) {
                var history = new SqlFiscalHistoryStore(database).Load(lease,FiscalJournalFamily.Payment);
                Equal(2,history.Count); Equal("Unknown",history[0].State); Equal(history[0].AttemptId,history[1].OriginalAttemptId.Value);
                var audit = JObject.Parse(history[0].SnapshotJson)["OperatorRetry"];
                Equal("GBridge.F4.v1",(string)audit["Policy"]); Equal(history[1].AttemptId,(Guid)audit["NewAttemptId"]);
                Equal(result.Payment.TransactionUid,(string)audit["NewTransactionUid"]);
                Check(JObject.Parse(history[0].SnapshotJson)["AutomaticRetry"].Type == JTokenType.Null,"Operator retry recorded as automatic");
            }
            Accepted(service.Send(s.Id,null,null)); Equal(2,h.Authorizations); Equal(1,h.Invoices);
        });
        foreach (string key in new[] { "iTimologia", "Ilyda.Direct" }) Test(key + " recovery stays lookup-only after F4 and F4 F2 0E", () => {
            var s = new Source(); var h = new Http(s.Id,key) { GatewayOverride = F4Sequence }; var service = Service(h);
            var first = service.Send(s.Id,Route(key),s);
            for(int i = 0; i < 3; i++) { Equal(first.Payment.Id,service.Recover(s.Id).Payment.Id); Equal(first.Payment.Id,service.Send(s.Id,null,null).Payment.Id); }
            Equal(1,h.Authorizations); Equal(1,h.Signatures); Equal(6,h.Lookups); Equal(0,h.Cancellations); Equal(0,h.Invoices);
            Equal(first.Payment.Id,service.GetRetryablePaymentAttemptId(s.Id).Value);
            h.GatewayOverride = null; Equal("Approved",service.RetryPayment(s.Id,first.Payment.Id).Payment.State);
            Equal(2,h.Authorizations); Equal(2,h.Signatures); Equal(0,h.Invoices);
        });
        Test("repeated F4 responses require a fresh operator action for every replacement", () => {
            var s = new Source(); var h = new Http(s.Id,"iTimologia") { GatewayOverride = F4Sequence };
            var service = Service(h); var first = service.Send(s.Id,Route(),s); Equal(1,h.Authorizations);
            var second = service.RetryPayment(s.Id,first.Payment.Id); Equal("Unknown",second.Payment.State); Equal(2,h.Authorizations);
            Throws(() => service.RetryPayment(s.Id,first.Payment.Id));
            for(int i = 0; i < 3; i++) { service.Send(s.Id,null,null); service.Recover(s.Id); }
            Equal(2,h.Authorizations); Equal(2,h.Signatures); Equal(0,h.Invoices);
            var third = service.RetryPayment(s.Id,second.Payment.Id); Equal("Unknown",third.Payment.State); Equal(3,h.Authorizations);
        });
        Test("operator replacement and decision roll back together on SQL failure", () => {
            var s = new Source(); var h = new Http(s.Id,"iTimologia"); h.GatewayOverride = r => h.Authorizations == 1 ? F4Sequence(r) : null;
            var service = Service(h); var first = service.Send(s.Id,Route(),s);
            Execute(connection,"CREATE TRIGGER dbo.TestRetryFailure ON dbo.FiscalAttempt AFTER INSERT AS IF EXISTS (SELECT 1 FROM inserted WHERE Family='Payment' AND OriginalAttemptId IS NOT NULL) BEGIN RAISERROR('synthetic retry failure',16,1); ROLLBACK TRANSACTION; END;");
            try { Throws(() => service.RetryPayment(s.Id,first.Payment.Id)); } finally { Execute(connection,"DROP TRIGGER dbo.TestRetryFailure;"); }
            Equal(1,h.Authorizations);
            using(var lease = database.EnterInvoice(s.Id)) { var history = new SqlFiscalHistoryStore(database).Load(lease,FiscalJournalFamily.Payment);
                Equal(1,history.Count); Equal("Unknown",history[0].State); Check(JObject.Parse(history[0].SnapshotJson)["OperatorRetry"].Type == JTokenType.Null,"Partial retry decision persisted"); }
            Equal("Unknown",service.Recover(s.Id).Payment.State); Equal(1,h.Authorizations);
            var result = service.RetryPayment(s.Id,first.Payment.Id); Equal("Approved",result.Payment.State); Equal(2,h.Authorizations); Equal(0,h.Invoices);
            Accepted(service.Send(s.Id,null,null)); Equal(2,h.Authorizations);
        });
        Test("interrupted Ilyda cancellation needs explicit resume of the saved operator replacement", () => {
            var s = new Source(); var h = new Http(s.Id,"Ilyda.Direct") { FailCancellation = true };
            h.GatewayOverride = r => h.Authorizations == 1 ? F4Sequence(r) : null;
            var service = Service(h); var first = service.Send(s.Id,Route(h.Key),s);
            Throws(() => service.RetryPayment(s.Id,first.Payment.Id)); Equal(1,h.Authorizations); Equal("Prepared",LatestState(s.Id));
            Guid replacement; using(var lease = database.EnterInvoice(s.Id)) replacement = new SqlFiscalHistoryStore(database).Load(lease,FiscalJournalFamily.Payment).Last().AttemptId;
            h.FailCancellation = false;
            Equal("Prepared",service.Recover(s.Id).Payment.State); Equal("Prepared",service.Send(s.Id,null,null).Payment.State);
            Equal(1,h.Cancellations); Equal(1,h.Authorizations); Equal(replacement,service.GetRetryablePaymentAttemptId(s.Id).Value);
            var result = service.RetryPayment(s.Id,replacement); Equal(replacement,result.Payment.Id); Equal("Approved",result.Payment.State);
            Equal(2,h.Cancellations); Equal(2,h.Signatures); Equal(2,h.Authorizations); Equal(0,h.Invoices);
            Accepted(Service(h).Send(s.Id,null,null)); Equal(2,h.Authorizations);
        });
        foreach (string mutation in new[] { "F0", "wrong-uid", "wrong-tid", "amount", "approved", "incomplete", "timeout", "extra-authorization", "wrong-order" })
        Test("operator retry rejects evidence " + mutation, () => {
            var request = new JObject { ["InputType"] = "1", ["TransUid"] = "uid", ["Tid"] = "TID-1",
                ["TransactionData"] = new JObject { ["ProviderInput"] = "signed-input",["TxnType"] = 0,["PaymentType"] = 0,["ProviderId"] = 34 } };
            var events = new System.Collections.Generic.List<PaymentGatewayEvent>();
            foreach(string kind in new[] { "1","4","3" }) {
                request["InputType"] = kind; var response = F4Sequence(request);
                events.Add(new PaymentGatewayEvent { TransactionUid = "uid", InputType = kind, RequestJson = request.ToString(),
                    ResponseJson = response.Content.ReadAsStringAsync().Result, HttpStatus = (int)response.StatusCode });
            }
            if(mutation == "extra-authorization") events.Insert(1,events[0]);
            else if(mutation == "wrong-order") events.Reverse();
            else if(mutation == "timeout") events[0].ResponseJson = null;
            else {
                var json = JObject.Parse(events[0].ResponseJson);
                if(mutation == "F0") json["ReplyCode"] = "F0";
                if(mutation == "wrong-uid") json["TransUid"] = "other";
                if(mutation == "approved") json["EFTReplyData"] = new JArray(new JObject { ["TransactionData"] = new JObject { ["Approved"] = true } });
                if(mutation == "incomplete") json.Remove("EFTReplyData");
                events[0].ResponseJson = json.ToString();
                if(mutation == "wrong-tid" || mutation == "amount") { var raw = JObject.Parse(events[0].RequestJson);
                    if(mutation == "wrong-tid") raw["Tid"] = "other"; else raw["TransactionData"]["ProviderInput"] = "different-signed-amount";
                    events[0].RequestJson = raw.ToString(); }
            }
            Check(!GBridgeClient.CanRetryAfterF4("uid","TID-1",events,"signed-input",0,0,34),"Unmatched evidence enabled retry");
        });
        foreach(string kind in new[] { "credentials", "signature" }) Test("rejection reconciliation does not override recovered transaction evidence " + kind, () => {
            var r = new JObject { ["InputType"] = "1",["TransUid"] = "uid",["Tid"] = "TID-1" };
            var response = kind == "credentials" ? CredentialsReply(r,"F0",401) : SignatureReply(r);
            var events = new[] { new PaymentGatewayEvent { InputType = "1",TransactionUid = "uid",RequestJson = r.ToString(),ResponseJson = response.Content.ReadAsStringAsync().Result,HttpStatus = (int)response.StatusCode },
                new PaymentGatewayEvent { InputType = "4",TransactionUid = "uid",ResponseJson = "{\"ReplyCode\":\"00\",\"EFTReplyData\":[{\"TransactionData\":{\"Approved\":true}}]}" } };
            Check(!GBridgeClient.HasOnlyRejectedAuthorizations("uid",events,34),"Transaction evidence was overridden");
        });
    }
    static HttpResponseMessage F4Sequence(JObject request)
    { string kind = (string)request["InputType"]; return EmptyGateway(request,kind == "1" ? "F4" : kind == "4" ? "F2" : "0E",kind == "4" ? 400 : 200); }
    static HttpResponseMessage EmptyGateway(JObject request,string code,int status = 200)
    { return Reply(new JObject { ["InputType"] = request["InputType"], ["TransUid"] = request["TransUid"], ["Status"] = status.ToString(),
        ["ReplyCode"] = code, ["EftposReplyCode"] = (string)request["InputType"] == "4" && code == "F2" ? "" : "200", ["EFTReplyData"] = new JArray() }.ToString(),status); }
    static HttpResponseMessage CredentialsReply(JObject request,string code,int httpStatus)
    { return Reply(new JObject { ["InputType"] = request["InputType"], ["TransUid"] = request["TransUid"], ["Status"] = "401", ["ReplyCode"] = code,
        ["EftposReplyCode"] = "401", ["ReplyDesc"] = "NSP Credentials Issue. Call Support Department", ["EFTReplyData"] = new JArray() }.ToString(),httpStatus); }
    static HttpResponseMessage SignatureReply(JObject request)
    { return Reply(new JObject { ["InputType"] = request["InputType"], ["TransUid"] = request["TransUid"], ["Status"] = "400", ["ReplyCode"] = "FE",
        ["EftposReplyCode"] = "400", ["ReplyDesc"] = "Eftpos Communication Error: 400 BAD_REQUEST & Invalid provider signature", ["EFTReplyData"] = new JArray() }.ToString(),400); }
}
