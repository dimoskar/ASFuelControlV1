using System;
using System.Linq;
using System.Text;
using ASFuelControl.Invoicing;
using ASFuelControl.Invoicing.Persistence;
using Exedron.Invoicing.Core;
using Exedron.Invoicing.Adapters.Samtec;
using Newtonsoft.Json;

namespace ASFuelControl.Windows.Threads
{
    internal sealed class SharedInvoiceStatus
    {
        internal InvoiceWorkflowSnapshot Snapshot;
        internal FiscalAttemptEvidence[] Attempts;
        internal FiscalAttemptEvidence[] Revisions;
        internal string PendingSummary;
        internal bool CanSubmitPrinted;
        internal bool AlreadyPrinted;
        internal ProviderRoute ConfiguredProvider;
        internal string ConnectionProblem;
        internal Guid? RetryPaymentAttemptId;
        internal string State(string family) => Attempts.LastOrDefault(a => a.Family == family)?.State ?? "Δεν ξεκίνησε";
    }
    internal static partial class SharedInvoiceRuntime
    {
        internal static SharedInvoiceStatus ReadStatus(Guid id)
        {
            using (var lease = sql.Value.EnterInvoice(id))
            {
                var bytes = new SqlInvoiceWorkflowStore(sql.Value).Load(lease);
                var history = new SqlFiscalHistoryStore(sql.Value);
                var entries = Enum.GetValues(typeof(FiscalJournalFamily)).Cast<FiscalJournalFamily>().SelectMany(f => history.Load(lease,f)).OrderBy(a => a.CreatedUtc).ToArray();
                var status = new SharedInvoiceStatus { Snapshot = bytes == null ? null : JsonConvert.DeserializeObject<InvoiceWorkflowSnapshot>(Encoding.UTF8.GetString(bytes)), Attempts = entries,
                    Revisions = entries.SelectMany(a => history.LoadHistory(lease,(FiscalJournalFamily)Enum.Parse(typeof(FiscalJournalFamily),a.Family),a.AttemptId)).OrderBy(a => a.UpdatedUtc).ToArray() };
                var paymentEntry = entries.LastOrDefault(a => a.Family == "Payment" && a.Operation == "Payment");
                if (status.Snapshot?.Payment != null && paymentEntry?.PayloadFormat == "provider-payment.v2.json")
                {
                    var paymentRequest = JsonConvert.DeserializeObject<ProviderPaymentAttemptRequest>(Encoding.UTF8.GetString(paymentEntry.RequestPayload));
                    var effectiveRoute = Newtonsoft.Json.Linq.JObject.Parse(paymentRequest.WorkflowContext)["Route"].ToObject<PaymentRoute>();
                    status.Snapshot.Payment = effectiveRoute; status.Snapshot.Provider = effectiveRoute.Provider;
                    status.RetryPaymentAttemptId = new ProviderPaymentService(sql.Value,new CurrentCredentials()).GetRetryablePaymentAttemptId(id);
                }
                if (status.Snapshot?.Provider != null && entries.Length == 0)
                {
                    try { status.ConfiguredProvider = CurrentConnection(status.Snapshot.Provider); }
                    catch (Exception ex) { status.ConnectionProblem = ex.Message; }
                }
                // Reading status previews the current connection without rewriting
                // the workflow or starting any external action.
                using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
                {
                    var invoice = db.Invoices.SingleOrDefault(i => i.InvoiceId == id);
                    status.AlreadyPrinted = invoice?.IsPrinted == true;
                    if (status.Snapshot == null && entries.Length == 0)
                    {
                        bool hasLegacy = db.MyDataInvoices.Any(i => i.InvoiceId == id);
                        status.PendingSummary = settings.Value.PendingInvoiceSummary(invoice,hasLegacy);
                        status.CanSubmitPrinted = settings.Value.CanSubmitPrintedInvoice(invoice,hasLegacy);
                    }
                }
                return status;
            }
        }
        private static FiscalSigningService SigningService() => new FiscalSigningService(sql.Value,scope =>
            new SamtecFiscalSigner((settings.Value.Devices ?? new SamtecDeviceProfile[0]).Single(p => FiscalSigningCoordinator.SameScope(p.Scope,scope))));

        internal static string RecoverStatus(Guid id)
        {
            using (var lease = sql.Value.EnterInvoice(id))
            {
                var status = ReadStatus(id);
                if (status.Snapshot == null) throw new InvalidOperationException("Το παραστατικό δεν έχει αποθηκευμένη κοινή ροή προς ανάκτηση. Ελέγξτε τη σύνοψη.");
                if (status.Attempts.Any(a => a.Family == "Print" && a.Operation != "PrintTF2")) return "Η εκτύπωση έχει δικό της ιστορικό. Χρησιμοποιήστε έλεγχο έκβασης ή επανεκτύπωση.";
                if (status.Snapshot.Payment != null && status.Attempts.Any(a => a.Family == "Payment" || a.Family == "ProviderSession"))
                {
                    var payment = new ProviderPaymentService(sql.Value,new CurrentCredentials()).Recover(id);
                    return "Πληρωμή: " + payment.Payment?.State + " / Διαβίβαση: " + payment.Submission?.State +
                        (string.IsNullOrWhiteSpace(payment.Payment?.ErrorMessage) ? "" : "\r\n" + payment.Payment.ErrorMessage);
                }
                if (status.Attempts.Any(a => a.Family == "Submission"))
                {
                    var result = new ProviderSubmissionService(sql.Value,new CurrentCredentials()).Recover(id);
                    return result.State + "\r\n" + string.Join("\r\n", result.Response?.Errors ?? new string[0]) + "\r\n" + string.Join("\r\n", result.Response?.Warnings ?? new string[0]);
                }
                if (status.Attempts.Any(a => a.Family == "Signing"))
                    return SigningService().Recover(new FiscalDocumentReference(FiscalDocumentKind.Invoice,id)).State.ToString();
                return "Δεν υπάρχει εξωτερική προσπάθεια προς ανάκτηση.";
            }
        }
        internal static InvoiceWorkflowResult ResumeIssuance(Guid id)
        {
            using (var lease = sql.Value.EnterInvoice(id))
            {
                var status = ReadStatus(id);
                if (status.Snapshot == null) throw new InvalidOperationException("Απαιτείται αποθηκευμένη κοινή ροή.");
                if (status.Attempts.Any(a => a.Family == "Print" || a.Family == "Cancellation")) throw new InvalidOperationException("Η έκδοση έχει ήδη ολοκληρωθεί ή ακυρωθεί. Ελέγξτε το ιστορικό.");
                if (status.State("Signing") == "Prepared")
                {
                    if (status.Attempts.Any(a => a.Family != "Signing")) throw new InvalidOperationException("Υπάρχει άλλη εξωτερική προσπάθεια. Απαιτείται έλεγχος.");
                    SigningService().ResumePrepared(new FiscalDocumentReference(FiscalDocumentKind.Invoice,id));
                }
                return workflow.Value.Process(id,null,false,retryRejected: status.State("Submission") == "Rejected",
                    retryDeclinedPayment: status.State("Payment") == "Declined" || status.State("Payment") == "Failed");
            }
        }
        internal static ProviderPaymentResult RetryPayment(Guid id, Guid expectedAttemptId)
        {
            return new ProviderPaymentService(sql.Value,new CurrentCredentials()).RetryPayment(id,expectedAttemptId);
        }
        internal static void ReconcilePrint(Guid id,Guid jobId,bool printed,string evidence)
        {
            if (!Program.AdminConnected) throw new InvalidOperationException("Απαιτείται σύνδεση διαχειριστή.");
            if (string.IsNullOrWhiteSpace(evidence)) throw new ArgumentException("Απαιτείται τεκμήριο έκβασης.");
            using (var lease = sql.Value.EnterInvoice(id))
            {
                new FiscalPrintService(sql.Value,new SharedInvoicePrinting(() => settings.Value.DefaultPrinter)).Reconcile(lease,jobId,printed,Program.CurrentUserName + ": " + evidence);
                // Reproject only an explicitly confirmed handoff. Rejected is evidence
                // for a later operator print action, never an automatic dispatch here.
                if (printed) Process(id,false);
            }
        }
    }
}
