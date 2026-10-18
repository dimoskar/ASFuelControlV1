using System;
using System.Linq;
using System.Text;
using ASFuelControl.Invoicing.Persistence;
using Exedron.Invoicing.Core;
using Exedron.Invoicing.Adapters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ASFuelControl.Invoicing
{
    public sealed partial class ProviderPaymentService
    {
        // One full refund per accepted original payment. Partial/multiple refunds need
        // an allocation ledger and are intentionally not inferred from credit totals.
        public ProviderPaymentResult Refund(Guid creditInvoiceId, Guid originalInvoiceId, IInvoiceSubmissionSource source,
            bool retryDeclined = false, bool retryRejectedInvoice = false, bool recoveryOnly = false)
        {
            if (creditInvoiceId == originalInvoiceId || creditInvoiceId == Guid.Empty || originalInvoiceId == Guid.Empty)
                throw new ArgumentException("Distinct saved credit and original invoice identities are required.");
            var first = creditInvoiceId.CompareTo(originalInvoiceId) < 0 ? creditInvoiceId : originalInvoiceId;
            var second = first == creditInvoiceId ? originalInvoiceId : creditInvoiceId;
            using (database.EnterInvoice(first)) using (database.EnterInvoice(second))
            using (var parentLease = database.EnterInvoice(originalInvoiceId)) using (var creditLease = database.EnterInvoice(creditInvoiceId))
            {
                var original = history.Load(parentLease,FiscalJournalFamily.Payment).LastOrDefault(a => a.Operation == "Payment");
                if (original == null || original.State != "Invoiced" || original.PayloadFormat != "provider-payment.v2.json" ||
                    history.Load(parentLease,FiscalJournalFamily.Submission).LastOrDefault()?.State != "Accepted")
                    throw new InvalidOperationException("Refund requires an accepted original payment and invoice.");
                if (history.Load(parentLease,FiscalJournalFamily.Cancellation).Any())
                    throw new InvalidOperationException("Original invoice cancellation requires reconciliation before refund.");
                var originalRequest = JsonConvert.DeserializeObject<ProviderPaymentAttemptRequest>(Encoding.UTF8.GetString(original.RequestPayload));
                var originalSnapshot = JsonConvert.DeserializeObject<PaymentSnapshot>(originalRequest.WorkflowContext);
                if (originalRequest.Operation != ProviderPaymentOperation.Sale || originalSnapshot?.Version != 1 ||
                    originalSnapshot.InvoiceId != originalInvoiceId || JsonConvert.SerializeObject(originalSnapshot.Route) != original.Scope)
                    throw new InvalidOperationException("Original payment routing is incomplete or is itself a refund.");
                var operations = new SqlPaymentCompensationStore(database);
                var reservation = operations.Load(parentLease);
                if (reservation != null && (reservation.Operation != "Refund" ||
                    (Guid)JObject.Parse(reservation.RequestJson)["CreditInvoiceId"] != creditInvoiceId))
                    throw new InvalidOperationException("A void or another refund already owns this original payment.");
                if (reservation == null)
                {
                    if (recoveryOnly) throw new InvalidOperationException("No refund exists to recover.");
                    if (source == null) throw new ArgumentNullException(nameof(source));
                    if (history.Load(creditLease,FiscalJournalFamily.Payment).Any()) throw new InvalidOperationException("Credit already has unrelated payment history.");
                    var input = source.Prepare(creditInvoiceId,originalSnapshot.Route.Provider);
                    if (input?.Invoice?.InvoiceId != creditInvoiceId || !input.RequireIssuedIdentity)
                        throw new InvalidOperationException("The refund source must return the saved credit invoice.");
                    var model = new InvoiceBuilder(new InvoiceTypeResolver()).Build(input);
                    Validate(model,originalSnapshot.Route,true);
                    if (model.PaymentMethods.Single().Amount != originalRequest.Amount ||
                        model.CounterPart?.VATNumber != originalSnapshot.Invoice.CounterPart?.VATNumber ||
                        (int)model.PaymentMethods.Single().Type != (int)originalSnapshot.Invoice.PaymentMethods.Single().Type)
                        throw new InvalidOperationException("A full refund must match the original amount, customer and payment method.");
                    source = new PreparedRefundSource(input);
                    reservation = operations.Begin(parentLease,original.AttemptId,"Refund",JsonConvert.SerializeObject(new {
                        CreditInvoiceId = creditInvoiceId, OriginalTransactionId = new SqlProviderPaymentStore(database,original.Scope).LoadLatest(originalInvoiceId).TransactionId,
                        Amount = originalRequest.Amount, Input = input }));
                }
                // A crash between reservation and child creation must still use the
                // validated credit snapshot, never reload mutable source entities.
                source = new PreparedRefundSource(JObject.Parse(reservation.RequestJson)["Input"].ToObject<InvoiceBuildRequest>());
                if (reservation.State == FiscalPaymentState.Prepared && recoveryOnly) return new ProviderPaymentResult();
                if (reservation.State == FiscalPaymentState.Prepared)
                    reservation = operations.Save(parentLease,reservation,FiscalPaymentState.Processing,"Original payment reserved for one full credit refund.");
                var result = Run(creditInvoiceId,originalSnapshot.Route,source,retryDeclined,retryRejectedInvoice,recoveryOnly,originalInvoiceId,original.AttemptId);
                if (reservation.State != FiscalPaymentState.Accepted)
                {
                    var state = result.Payment != null && (result.Payment.State == "Approved" || result.Payment.State == "Invoiced")
                        ? FiscalPaymentState.Accepted : FiscalPaymentState.Unknown;
                    operations.Save(parentLease,reservation,state,"Credit payment attempt " + result.Payment?.Id + ": " + result.Payment?.State);
                }
                return result;
            }
        }
        private sealed class PreparedRefundSource : IInvoiceSubmissionSource
        {
            private readonly InvoiceBuildRequest request;
            public PreparedRefundSource(InvoiceBuildRequest request) { this.request = request; }
            public InvoiceBuildRequest Prepare(Guid id, ProviderRoute route) { return request; }
        }
        public PaymentCompensationRecord VoidAcceptedPayment(Guid invoiceId, string reason)
        { return Void(invoiceId, reason, false); }
        public PaymentCompensationRecord RecoverVoid(Guid invoiceId) { return Void(invoiceId, null, true); }
        private PaymentCompensationRecord Void(Guid invoiceId, string reason, bool recoveryOnly)
        {
            using (var lease = database.EnterInvoice(invoiceId))
            {
                var operations = new SqlPaymentCompensationStore(database);
                var record = operations.Load(lease);
                if (record?.Operation == "Refund") throw new InvalidOperationException("An existing refund prevents voiding this payment.");
                if (record != null && record.State == FiscalPaymentState.Accepted) return record;
                var original = history.Load(lease,FiscalJournalFamily.Payment).LastOrDefault(a => a.Operation == "Payment");
                if (original == null || original.PayloadFormat != "provider-payment.v2.json" || original.State != "Approved")
                    throw new InvalidOperationException("Void requires an approved payment that has not been attached to an accepted invoice.");
                if (history.Load(lease,FiscalJournalFamily.Submission).Any(a => a.State != "Rejected") ||
                    history.Load(lease,FiscalJournalFamily.Cancellation).Any() || history.Load(lease,FiscalJournalFamily.Print).Any())
                    throw new InvalidOperationException("Invoice issuance is accepted or uncertain; reconcile it before a payment reversal.");
                var storedRequest = JsonConvert.DeserializeObject<ProviderPaymentAttemptRequest>(Encoding.UTF8.GetString(original.RequestPayload));
                var snapshot = JsonConvert.DeserializeObject<PaymentSnapshot>(storedRequest.WorkflowContext);
                if (snapshot?.Version != 1 || snapshot.InvoiceId != invoiceId || JsonConvert.SerializeObject(snapshot.Route) != original.Scope)
                    throw new InvalidOperationException("Historical payment routing is unavailable.");
                var route = snapshot.Route;
                var payment = new SqlProviderPaymentStore(database,original.Scope).LoadLatest(invoiceId);
                PaymentVoidRequest request;
                if (record == null)
                {
                    if (recoveryOnly) throw new InvalidOperationException("No void exists to recover.");
                    if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("An explicit void reason is required.");
                    request = new PaymentVoidRequest { TransactionUid = payment.TransactionUid, OriginalTransactionId = payment.TransactionId,
                        TerminalId = payment.TerminalId, AmountMinorUnits = payment.AmountMinorUnits, PaymentType = payment.PaymentType,
                        TransactionType = (int)payment.Operation };
                    record = operations.Begin(lease,payment.Id,"Void",JsonConvert.SerializeObject(new { Request = request, Reason = reason }));
                }
                else request = JObject.Parse(record.RequestJson)["Request"].ToObject<PaymentVoidRequest>();
                bool lookup = record.State == FiscalPaymentState.Processing || record.State == FiscalPaymentState.Unknown;
                if (record.State == FiscalPaymentState.Prepared && recoveryOnly) return record;
                if (!lookup && record.State != FiscalPaymentState.Prepared) return record;
                request.RecoverOnly = lookup;
                using (var gateway = new GBridgeClient(new GBridgeOptions { Endpoint = route.Endpoint, TerminalId = route.TerminalId,
                    Logger = new ProviderSubmissionService.InvoiceLogger(new SqlEvidenceLogger(database), invoiceId), EventSink = new SqlEvidenceLogger(database) },transport?.Invoke(route)))
                {
                    if (record.State == FiscalPaymentState.Prepared)
                        record = operations.Save(lease,record,FiscalPaymentState.Processing,"Saved original transaction before void dispatch.");
                    FiscalPaymentState outcome; string evidence;
                    try { gateway.Void(request); outcome = FiscalPaymentState.Accepted;
                        evidence = "Gateway confirmed void of original transaction " + request.OriginalTransactionId + "; HTTP evidence uses TransUid " + request.TransactionUid; }
                    catch (Exception ex) { outcome = FiscalPaymentState.Unknown; evidence = "Void remains unconfirmed: " + ex.GetType().Name; }
                    return operations.Save(lease,record,outcome,evidence);
                }
            }
        }
    }
}
