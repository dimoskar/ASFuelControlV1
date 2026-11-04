using System;
using System.Linq;
using System.Text;
using ASFuelControl.Invoicing;
using ASFuelControl.Invoicing.Mapping;
using ASFuelControl.Invoicing.Persistence;
using Exedron.Invoicing.Core;
using Exedron.Invoicing.Adapters;
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
        internal bool CanPrintHistorical;
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
        internal bool CanChangePaymentMethod;
        internal int PaymentType;
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
                bool paymentWasSuperseded = status.Snapshot?.PaymentMethodChanges?.Count > 0 &&
                    (paymentEntry?.State == "Declined" || paymentEntry?.State == "Failed");
                if (status.Snapshot?.Payment != null && paymentEntry?.PayloadFormat == "provider-payment.v2.json" &&
                    !paymentWasSuperseded)
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
                    status.CanPrintHistorical = status.Snapshot == null && entries.Length == 0 &&
                        invoice?.IsPrinted == true && invoice.Number > 0;
                    status.PaymentType = invoice?.PaymentType ??
                        (int)Common.Enumerators.PaymentTypeEnum.Cash;
                    status.CanChangePaymentMethod = PaymentMethodCanChange(db,invoice,status,entries);
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
        internal static string ChangePaymentMethod(Guid id, int paymentType)
        {
            if (!Enum.IsDefined(typeof(Common.Enumerators.PaymentTypeEnum),paymentType))
                throw new ArgumentOutOfRangeException(nameof(paymentType));
            using (var lease = sql.Value.EnterInvoice(id))
            {
                int previous;
                using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
                {
                    var invoice = db.Invoices.SingleOrDefault(value => value.InvoiceId == id);
                    if (invoice == null) throw new InvalidOperationException("Το παραστατικό δεν βρέθηκε.");
                    previous = invoice.PaymentType ?? (int)Common.Enumerators.PaymentTypeEnum.Cash;
                }
                if (previous == paymentType) return "Ο τρόπος πληρωμής δεν άλλαξε.";

                var store = new SqlInvoiceWorkflowStore(sql.Value);
                var originalBytes = store.Load(lease);
                byte[] replacementBytes = null;
                if (originalBytes != null)
                {
                    var saved = JsonConvert.DeserializeObject<InvoiceWorkflowSnapshot>(
                        Encoding.UTF8.GetString(originalBytes));
                    var replacement = new Source(false,false,null,paymentType).Prepare(lease);
                    RequireSamePaymentChangeScope(saved,replacement);

                    // The provider account and selected printer remain frozen. POS
                    // routing is regenerated only when the new method requires it.
                    replacement.Provider = saved.Provider;
                    if (replacement.Payment != null)
                    {
                        var provider = saved.Provider ?? throw new InvalidOperationException(
                            "Η νέα πληρωμή POS απαιτεί τον αποθηκευμένο πάροχο του παραστατικού.");
                        replacement.Payment = new PaymentRoute(provider,replacement.Payment.Endpoint,
                            replacement.Payment.TerminalId,replacement.Payment.ITimologiaSignatureFormat,
                            replacement.Payment.IlydaProtocol,replacement.Payment.PosMode);
                    }
                    replacement.PrintDestination = saved.PrintDestination;
                    replacement.PrintFormat = saved.PrintFormat;
                    replacement.Version = Math.Max(saved.Version,replacement.Version);
                    replacement.PaymentMethodChanges = saved.PaymentMethodChanges == null
                        ? new System.Collections.Generic.List<InvoicePaymentMethodChange>()
                        : new System.Collections.Generic.List<InvoicePaymentMethodChange>(saved.PaymentMethodChanges);
                    replacement.PaymentMethodChanges.Add(new InvoicePaymentMethodChange {
                        PreviousPaymentType = previous, PaymentType = paymentType,
                        ChangedBy = Program.CurrentUserName, ChangedUtc = DateTime.UtcNow
                    });
                    replacementBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(replacement));
                }
                store.ChangePaymentMethod(lease,originalBytes,replacementBytes,previous,paymentType);
                return "Ο τρόπος πληρωμής ενημερώθηκε. Το προηγούμενο αίτημα ακυρώθηκε τοπικά και η επόμενη έκδοση θα χρησιμοποιήσει νέο payload.";
            }
        }
        private static void RequireSamePaymentChangeScope(InvoiceWorkflowSnapshot saved,
            InvoiceWorkflowSnapshot replacement)
        {
            if (saved == null || replacement == null || saved.InvoiceId != replacement.InvoiceId ||
                saved.LocalOnly != replacement.LocalOnly ||
                !SameProviderIdentity(saved.Provider,replacement.Provider) ||
                (saved.Signing == null) != (replacement.Signing == null) ||
                saved.Signing != null && !FiscalSigningCoordinator.SameIssuerScope(
                    saved.Signing.Scope,replacement.Signing.Scope) ||
                saved.MyDieselCouponCaseId != replacement.MyDieselCouponCaseId ||
                JsonConvert.SerializeObject(saved.MyDieselScope) !=
                    JsonConvert.SerializeObject(replacement.MyDieselScope))
                throw new InvalidOperationException(
                    "Η αλλαγή πληρωμής πρέπει να διατηρεί διαδρομή, εκδότη και δεσμευμένο myDiesel του παραστατικού.");
        }
        private static bool SameProviderIdentity(ProviderRoute left, ProviderRoute right)
        {
            if (left == null || right == null) return left == null && right == null;
            return left.ProviderKey == right.ProviderKey && left.TestMode == right.TestMode &&
                left.IssuerVat == right.IssuerVat && left.IssuerBranch == right.IssuerBranch;
        }
        private static bool PaymentMethodCanChange(Data.DatabaseModel db, Data.Invoice invoice,
            SharedInvoiceStatus status, FiscalAttemptEvidence[] entries)
        {
            if (invoice == null || invoice.Number <= 0 || invoice.IsPrinted == true ||
                !string.IsNullOrWhiteSpace(invoice.InvoiceSignature) ||
                !string.IsNullOrWhiteSpace(invoice.QRCodeData) ||
                db.MyDataInvoices.Any(value => value.InvoiceId == invoice.InvoiceId && value.Mark > 0))
                return false;
            if (!Enum.IsDefined(typeof(Common.Enumerators.PaymentTypeEnum),status.PaymentType))
                return false;
            if (status.Snapshot == null) return entries.Length == 0;
            return entries.All(entry =>
                entry.Family == "Signing" && entry.Operation == "Sign" &&
                    (entry.State == "Prepared" || entry.State == "Rejected" || entry.State == "FailedBeforeDispatch") ||
                entry.Family == "Submission" && entry.Operation == "Issue" && entry.State == "Rejected" ||
                entry.Family == "ProviderSession" && entry.Operation == "SignPayment" &&
                    (entry.State == "Rejected" || entry.State == "Cancelled" || entry.State == "Expired") ||
                entry.Family == "Payment" && entry.Operation == "Payment" &&
                    (entry.State == "Declined" || entry.State == "Failed"));
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
        private static string CurrentPrintDestination(Guid id, string selectedPrinter = null)
        {
            // Reprints preserve the original rendered document, but routing is
            // recalculated from the invoice type actually stored in the database.
            var current = SharedInvoicingSettings.LoadRuntime();
            using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                var invoice = db.Invoices.SingleOrDefault(i => i.InvoiceId == id);
                if (invoice == null) throw new InvalidOperationException("Το παραστατικό δεν βρέθηκε.");
                var route = current.Route(invoice);
                if (!string.IsNullOrWhiteSpace(selectedPrinter))
                    route.Printer.PrinterName = selectedPrinter.Trim();
                var printer = SharedInvoicePrinting.ResolveProfile(route.Printer,current.DefaultPrinter);
                return JsonConvert.SerializeObject(printer);
            }
        }
        internal static string CurrentPrinterName(Guid id)
        {
            return JsonConvert.DeserializeObject<SharedPrinterProfile>(CurrentPrintDestination(id)).PrinterName;
        }
        private static FiscalSigningService SigningService() => new FiscalSigningService(sql.Value,scope =>
            new SamtecFiscalSigner((settings.Value.Devices ?? new SamtecDeviceProfile[0]).Single(p => FiscalSigningCoordinator.SameScope(p.Scope,scope))));

        private static bool ApplyStaleDirectPumpDispatchDate(Data.Invoice invoice,
            InvoiceWorkflowSnapshot snapshot, DateTime currentDispatch)
        {
            if (invoice == null || snapshot?.Provider == null || snapshot.LocalOnly ||
                snapshot.Invoice?.Delivery == null ||
                invoice.Number <= 0 || invoice.IsPrinted == true ||
                !HostInvoiceMapper.IsInvoiceWithDelivery(invoice.InvoiceType) ||
                !invoice.InvoiceLines.Any(line => line.SaleTransactionId.HasValue &&
                    line.SaleTransactionId.Value != Guid.Empty))
                return false;

            var previous = snapshot.Invoice.Delivery.Departure;
            if (previous == default(DateTime) || previous.Date >= currentDispatch.Date)
                return false;
            if (currentDispatch.Kind == DateTimeKind.Utc)
                throw new InvalidOperationException(
                    "Η ημερομηνία διακίνησης πρέπει να είναι τοπική φορολογική ώρα.");

            snapshot.Invoice.Delivery.Departure = currentDispatch;
            snapshot.DispatchDateChanges = snapshot.DispatchDateChanges == null
                ? new System.Collections.Generic.List<InvoiceDispatchDateChange>()
                : new System.Collections.Generic.List<InvoiceDispatchDateChange>(
                    snapshot.DispatchDateChanges);
            snapshot.DispatchDateChanges.Add(new InvoiceDispatchDateChange {
                PreviousDispatchDate = previous,
                DispatchDate = currentDispatch,
                ChangedBy = Program.CurrentUserName ?? Environment.UserName,
                ChangedUtc = DateTime.UtcNow
            });
            return true;
        }

        private static bool HasStaleDirectPumpDispatchCandidate(Guid id)
        {
            using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                var invoice = db.Invoices.SingleOrDefault(value => value.InvoiceId == id);
                return invoice != null && invoice.Number > 0 && invoice.IsPrinted != true &&
                    invoice.TransactionDate.Date < DateTime.Today &&
                    HostInvoiceMapper.IsInvoiceWithDelivery(invoice.InvoiceType) &&
                    invoice.InvoiceLines.Any(line => line.SaleTransactionId.HasValue &&
                        line.SaleTransactionId.Value != Guid.Empty);
            }
        }

        private static bool RecoverStaleDirectPumpDispatchDate(Guid id,
            IFiscalDocumentLease lease, SharedInvoiceStatus status)
        {
            if (status?.Snapshot == null ||
                status.Attempts.Any(attempt => attempt.Family == "ProviderSession" ||
                    attempt.Family == "Payment" || attempt.Family == "MyDiesel") ||
                status.Attempts.Any(attempt => attempt.Family == "Submission" &&
                    attempt.State != "Rejected"))
                return false;
            var signing = status.Attempts.LastOrDefault(attempt => attempt.Family == "Signing");
            if (signing != null && signing.State != "Signed" && signing.State != "Rejected" &&
                signing.State != "FailedBeforeDispatch")
                return false;

            var store = new SqlInvoiceWorkflowStore(sql.Value);
            var original = store.Load(lease);
            if (original == null) return false;
            var snapshot = JsonConvert.DeserializeObject<InvoiceWorkflowSnapshot>(
                Encoding.UTF8.GetString(original));
            using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                var invoice = db.Invoices.SingleOrDefault(value => value.InvoiceId == id);
                if (invoice == null || db.MyDataInvoices.Any(value =>
                    value.InvoiceId == id && value.Mark > 0) ||
                    !ApplyStaleDirectPumpDispatchDate(invoice,snapshot,DateTime.Now))
                    return false;
            }

            // Validate the changed provider payload before replacing the durable
            // snapshot. The issued identity and printable document remain frozen.
            var model = new InvoiceBuilder(new InvoiceTypeResolver()).Build(snapshot.Invoice);
            new InvoiceAdapterFactory(new InvoiceTypeResolver())
                .CreatePayloadMapper(snapshot.Provider.ProviderKey).Serialize(model,id);
            var replacement = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(snapshot));
            store.ReplaceDeliveryDispatchForRecovery(lease,original,replacement);
            status.Snapshot = snapshot;
            return true;
        }

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
            if (new SqlInvoiceTypeCorrectionStore(sql.Value).IsChild(id,true))
                throw new InvalidOperationException("Η επανέκδοση συνεχίζεται μόνο από την παρτίδα διόρθωσης τύπου myDATA.");
            using (var lease = sql.Value.EnterInvoice(id))
            {
                var status = ReadStatus(id);
                if (status.Snapshot == null) throw new InvalidOperationException("Απαιτείται αποθηκευμένη κοινή ροή.");
                if (status.Attempts.Any(a => a.Family == "Print" || a.Family == "Cancellation")) throw new InvalidOperationException("Η έκδοση έχει ήδη ολοκληρωθεί ή ακυρωθεί. Ελέγξτε το ιστορικό.");
                if (status.State("Signing") == "Prepared")
                {
                    if (status.Attempts.Any(a => a.Family != "Signing")) throw new InvalidOperationException("Υπάρχει άλλη εξωτερική προσπάθεια. Απαιτείται έλεγχος.");
                    SigningService().ResumePrepared(new FiscalDocumentReference(FiscalDocumentKind.Invoice,id));
                    status = ReadStatus(id);
                }
                RecoverStaleDirectPumpDispatchDate(id,lease,status);
                bool retryableSigning = status.State("Signing") == "Rejected" ||
                    status.State("Signing") == "FailedBeforeDispatch";
                // A definitive device rejection may be corrected from the current
                // invoice calculation. Source.Prepare recalculates and remaps the
                // full workflow before a new Samtec request is persisted.
                IInvoiceWorkflowSource correctionSource = retryableSigning ? new Source(false) : null;
                return workflow.Value.Process(id,correctionSource,false,retryRejected: retryableSigning ||
                    status.State("Submission") == "Rejected" || status.State("Submission") == "RetryAuthorized",
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
                        () => SharedInvoicePrinting.Render(snapshot,result.Submission.Response,SharedInvoiceLaserReportRenderer.Render));
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
