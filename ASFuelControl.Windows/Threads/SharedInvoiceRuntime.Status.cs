using System;
using System.Linq;
using System.Text;
using ASFuelControl.Invoicing;
using ASFuelControl.Invoicing.Persistence;
using Exedron.Invoicing.Core;
using Exedron.Invoicing.Adapters.Samtec;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
        internal string InvoiceSignature;
        internal Guid? RetryPaymentAttemptId;
        internal Guid? PaymentAttemptId;
        internal Guid? OriginalPaymentAttemptId;
        internal Guid? RefundOriginalInvoiceId;
        internal PaymentCompensationRecord Compensation;
        internal bool CanVoidPayment;
        internal bool CanRecoverVoid;
        internal bool CanRefundPayment;
        internal bool CanRecoverRefund;
        internal bool IsRefundPayment;
        internal ProviderResendAssessment ProviderResend;
        internal string State(string family) => Attempts.LastOrDefault(a => a.Family == family)?.State ?? "Δεν ξεκίνησε";
    }
    internal static partial class SharedInvoiceRuntime
    {
        internal static SharedInvoiceStatus ReadStatus(Guid id)
        {
            SharedInvoiceStatus status;
            using (var lease = sql.Value.EnterInvoice(id))
            {
                var bytes = new SqlInvoiceWorkflowStore(sql.Value).Load(lease);
                var history = new SqlFiscalHistoryStore(sql.Value);
                var entries = Enum.GetValues(typeof(FiscalJournalFamily)).Cast<FiscalJournalFamily>().SelectMany(f => history.Load(lease,f)).OrderBy(a => a.CreatedUtc).ToArray();
                status = new SharedInvoiceStatus { Snapshot = bytes == null ? null : JsonConvert.DeserializeObject<InvoiceWorkflowSnapshot>(Encoding.UTF8.GetString(bytes)), Attempts = entries,
                    Revisions = entries.SelectMany(a => history.LoadHistory(lease,(FiscalJournalFamily)Enum.Parse(typeof(FiscalJournalFamily),a.Family),a.AttemptId)).OrderBy(a => a.UpdatedUtc).ToArray() };
                status.InvoiceSignature = ReadInvoiceSignature(entries);
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
            }
            ReadPaymentCompensationStatus(id,status);
            if (status.Snapshot?.Provider != null && status.Attempts.Any(a => a.Family == "Submission"))
            {
                try { status.ProviderResend = new ProviderSubmissionService(sql.Value,new CurrentCredentials()).GetResendAssessment(id); }
                catch { status.ProviderResend = null; }
            }
            return status;
        }
        private static void ReadPaymentCompensationStatus(Guid id, SharedInvoiceStatus status)
        {
            var payment = status.Attempts.LastOrDefault(a => a.Family == "Payment" && a.Operation == "Payment");
            status.PaymentAttemptId = payment?.AttemptId;
            status.OriginalPaymentAttemptId = payment?.OriginalAttemptId;
            PaymentCompensationRecord local;
            using (var lease = sql.Value.EnterInvoice(id)) local = new SqlPaymentCompensationStore(sql.Value).Load(lease);
            if (local?.Operation == "Void")
            {
                status.Compensation = local;
                status.CanRecoverVoid = local.State == FiscalPaymentState.Processing || local.State == FiscalPaymentState.Unknown;
            }
            bool issuanceUncertain = status.Attempts.Any(a => a.Family == "Submission" && a.State != "Rejected") ||
                status.Attempts.Any(a => a.Family == "Cancellation" || a.Family == "Print");
            status.CanVoidPayment = payment?.State == "Approved" && !issuanceUncertain &&
                (local == null || local.Operation == "Void" && local.State == FiscalPaymentState.Prepared);

            Guid[] parents;
            using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
                parents = db.InvoiceRelations.Where(r => r.ChildInvoiceId == id).Select(r => r.ParentInvoiceId).Distinct().ToArray();
            if (parents.Length != 1) return;
            status.RefundOriginalInvoiceId = parents[0];
            PaymentCompensationRecord parentOperation;
            FiscalAttemptEvidence[] originalPayments, originalSubmissions, originalCancellations;
            using (var parentLease = sql.Value.EnterInvoice(parents[0]))
            {
                var history = new SqlFiscalHistoryStore(sql.Value);
                parentOperation = new SqlPaymentCompensationStore(sql.Value).Load(parentLease);
                originalPayments = history.Load(parentLease,FiscalJournalFamily.Payment).ToArray();
                originalSubmissions = history.Load(parentLease,FiscalJournalFamily.Submission).ToArray();
                originalCancellations = history.Load(parentLease,FiscalJournalFamily.Cancellation).ToArray();
            }
            bool sameRefund = parentOperation?.Operation == "Refund" && RefundCredit(parentOperation) == id;
            if (sameRefund)
            {
                status.Compensation = parentOperation;
                status.IsRefundPayment = true;
                status.CanRecoverRefund = (parentOperation.State == FiscalPaymentState.Processing || parentOperation.State == FiscalPaymentState.Unknown) &&
                    payment != null && (payment.State == "Pending" || payment.State == "Unknown" || payment.State == "Mismatch");
            }
            var originalPayment = originalPayments.LastOrDefault(a => a.Operation == "Payment");
            bool originalEligible = originalPayment?.State == "Invoiced" && originalSubmissions.LastOrDefault()?.State == "Accepted" &&
                originalCancellations.Length == 0;
            bool creditComplete = status.State("Submission") == "Accepted" && status.Attempts.Any(a => a.Family == "Print" &&
                (a.State == "HandedOff" || a.State == "Confirmed"));
            status.CanRefundPayment = originalEligible && !creditComplete &&
                (parentOperation == null && !status.Attempts.Any(a => a.Family == "Payment") || sameRefund && !status.CanRecoverRefund);
            if (payment?.PayloadFormat == "provider-payment.v2.json")
            {
                try
                {
                    var request = JsonConvert.DeserializeObject<ProviderPaymentAttemptRequest>(Encoding.UTF8.GetString(payment.RequestPayload));
                    status.IsRefundPayment = request.Operation == ProviderPaymentOperation.Refund;
                }
                catch { }
            }
        }
        private static Guid? RefundCredit(PaymentCompensationRecord record)
        {
            try { return (Guid?)JObject.Parse(record.RequestJson)["CreditInvoiceId"]; }
            catch { return null; }
        }
        private static string ReadInvoiceSignature(FiscalAttemptEvidence[] entries)
        {
            var signed = entries.LastOrDefault(a => a.Family == FiscalJournalFamily.Signing.ToString() && a.State == "Signed");
            if (signed != null)
            {
                var value = (string)JObject.Parse(signed.SnapshotJson)["Signature"];
                if (!string.IsNullOrWhiteSpace(value)) return value;
            }
            var accepted = entries.LastOrDefault(a => a.Family == FiscalJournalFamily.Submission.ToString() && a.State == "Accepted");
            if (accepted == null) return null;
            try { return ProviderSubmissionService.ReadAcceptedResponse(accepted).InvoiceSignature; }
            catch { return null; }
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
            if (new SqlHeatingCorrectionStore(sql.Value).IsChild(id,true))
                throw new InvalidOperationException("Η επανέκδοση συνεχίζεται μόνο από την παρτίδα διόρθωσης επιδόματος θέρμανσης.");
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
                return workflow.Value.Process(id,null,false,retryRejected: status.State("Submission") == "Rejected" ||
                    status.State("Submission") == "RetryAuthorized",
                    retryDeclinedPayment: status.State("Payment") == "Declined" || status.State("Payment") == "Failed");
            }
        }
        internal static InvoiceWorkflowResult AuthorizeAndResumeSubmission(Guid id)
        {
            var providers = new ProviderSubmissionService(sql.Value,new CurrentCredentials());
            providers.AuthorizeResend(id,Program.CurrentUserName,
                "Ο χειριστής επιβεβαίωσε νέα διαβίβαση μετά από μη επιβεβαιωμένη έκβαση παρόχου.");
            return workflow.Value.Process(id,null,false,retryRejected:true);
        }
        internal static ProviderPaymentResult RetryPayment(Guid id, Guid expectedAttemptId)
        {
            return new ProviderPaymentService(sql.Value,new CurrentCredentials()).RetryPayment(id,expectedAttemptId);
        }
        internal static PaymentCompensationRecord VoidPayment(Guid id, string reason)
        {
            return new ProviderPaymentService(sql.Value,new CurrentCredentials()).VoidAcceptedPayment(id,Program.CurrentUserName + ": " + reason);
        }
        internal static PaymentCompensationRecord RecoverVoid(Guid id)
        {
            return new ProviderPaymentService(sql.Value,new CurrentCredentials()).RecoverVoid(id);
        }
        internal static ProviderPaymentResult RefundPayment(Guid creditInvoiceId, Guid originalInvoiceId)
        {
            var route = ReadOriginalPaymentRoute(originalInvoiceId);
            InvoiceWorkflowSnapshot snapshot;
            using (var lease = sql.Value.EnterInvoice(creditInvoiceId))
            {
                var store = new SqlInvoiceWorkflowStore(sql.Value);
                var bytes = store.Load(lease);
                if (bytes == null)
                {
                    snapshot = new Source(true).Prepare(lease);
                    if (snapshot.Provider == null || snapshot.Provider.ProviderKey != route.Provider.ProviderKey ||
                        snapshot.Provider.TestMode != route.Provider.TestMode || snapshot.Provider.IssuerVat != route.Provider.IssuerVat)
                        throw new InvalidOperationException("Το πιστωτικό και η αρχική πληρωμή πρέπει να χρησιμοποιούν τον ίδιο πάροχο, περιβάλλον και εκδότη.");
                    snapshot.Provider = route.Provider;
                    snapshot.Payment = route;
                    store.Save(lease,Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(snapshot)));
                }
                else snapshot = JsonConvert.DeserializeObject<InvoiceWorkflowSnapshot>(Encoding.UTF8.GetString(bytes));
                if (snapshot.Payment == null || JsonConvert.SerializeObject(snapshot.Payment) != JsonConvert.SerializeObject(route))
                    throw new InvalidOperationException("Η αποθηκευμένη διαδρομή POS του πιστωτικού δεν συμφωνεί με την αρχική πληρωμή.");
            }
            var result = new ProviderPaymentService(sql.Value,new CurrentCredentials()).Refund(creditInvoiceId,originalInvoiceId,snapshot,
                retryDeclined:true,retryRejectedInvoice:true);
            if (result.Submission?.Accepted == true)
            {
                FiscalOperationRecord printed;
                using (var lease = sql.Value.EnterInvoice(creditInvoiceId))
                    printed = new FiscalPrintService(sql.Value,new SharedInvoicePrinting(() => settings.Value.DefaultPrinter)).Print(lease,
                        result.Submission.AttemptId,snapshot.PrintDestination,snapshot.PrintFormat,
                        () => SharedInvoicePrinting.Render(snapshot,result.Submission.Response));
                ProjectRefund(creditInvoiceId,result,printed);
            }
            return result;
        }
        internal static ProviderPaymentResult RecoverRefund(Guid creditInvoiceId, Guid originalInvoiceId)
        {
            return new ProviderPaymentService(sql.Value,new CurrentCredentials()).Refund(creditInvoiceId,originalInvoiceId,null,recoveryOnly:true);
        }
        private static PaymentRoute ReadOriginalPaymentRoute(Guid originalInvoiceId)
        {
            using (var lease = sql.Value.EnterInvoice(originalInvoiceId))
            {
                var entry = new SqlFiscalHistoryStore(sql.Value).Load(lease,FiscalJournalFamily.Payment)
                    .LastOrDefault(a => a.Operation == "Payment" && a.PayloadFormat == "provider-payment.v2.json");
                if (entry == null) throw new InvalidOperationException("Δεν βρέθηκε η αρχική αποθηκευμένη πληρωμή GBridge.");
                var request = JsonConvert.DeserializeObject<ProviderPaymentAttemptRequest>(Encoding.UTF8.GetString(entry.RequestPayload));
                return JObject.Parse(request.WorkflowContext)["Route"].ToObject<PaymentRoute>();
            }
        }
        private static void ProjectRefund(Guid invoiceId, ProviderPaymentResult result, FiscalOperationRecord printed)
        {
            using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                var invoice = db.Invoices.Single(i => i.InvoiceId == invoiceId);
                var response = result.Submission.Response;
                if (!string.IsNullOrWhiteSpace(response.InvoiceSignature)) invoice.InvoiceSignature = response.InvoiceSignature;
                invoice.QRCodeData = response.QrCodeUrl;
                long mark;
                if (long.TryParse(response.Mark,out mark) && mark > 0)
                {
                    var row = db.MyDataInvoices.SingleOrDefault(i => i.InvoiceId == invoiceId);
                    if (row == null) { row = new Data.MyDataInvoice { MyDataInvoiceId = Guid.NewGuid(),InvoiceId = invoiceId,DateTimeSent = DateTime.Now }; db.Add(row); }
                    row.Mark = mark; row.Uid = response.Uid; row.Status = 3; row.QrCodeUrl = response.QrCodeUrl;
                    row.VerificationHash = response.VerificationHash; row.ProviderUrl = response.ProviderUrl;
                    row.InvoiceUrl = response.InvoiceUrl; row.Errors = "";
                }
                if (printed?.State == "HandedOff" || printed?.State == "Confirmed") invoice.IsPrinted = true;
                else if (printed != null && invoice.IsPrinted != true) invoice.IsPrinted = false;
                db.SaveChanges();
            }
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
