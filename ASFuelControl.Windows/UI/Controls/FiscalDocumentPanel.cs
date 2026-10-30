using System;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ASFuelControl.Windows.Threads;
using ASFuelControl.Windows.UI.Forms;
using Newtonsoft.Json.Linq;
using Telerik.WinControls;
using Telerik.WinControls.UI;

namespace ASFuelControl.Windows.UI.Controls
{
    internal sealed class FiscalDocumentPanel : UserControl
    {
        private readonly RadPageView pages = new RadPageView { Dock = DockStyle.Fill };
        private readonly RadTextBox summary, history, payment, printing, evidence;
        private readonly FlowLayoutPanel actions = new FlowLayoutPanel { Dock = DockStyle.Fill,WrapContents = true };
        private readonly RadButton recover, retryPayment, resume, print, reconcile, voidPayment, recoverVoid, refundPayment, recoverRefund, diagnostics;
        private Guid invoiceId;
        private SharedInvoiceStatus status;
        private string lastActionMessage;
        private Guid lastActionInvoiceId;
        private string CurrentActionMessage => lastActionInvoiceId == invoiceId ? lastActionMessage : null;
        internal bool Owned => status?.Snapshot != null || status?.Attempts.Any() == true;
        internal string InvoiceSignature => status?.InvoiceSignature;
        internal bool ReadFailed { get; private set; }
        internal bool Busy { get; private set; }
        internal event EventHandler StateChanged;
        internal event EventHandler ActionCompleted;
        internal FiscalDocumentPanel()
        {
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill,ColumnCount = 1,RowCount = 2,Margin = new Padding(0) };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute,42));
            layout.Controls.Add(pages,0,0); layout.Controls.Add(actions,0,1);
            Controls.Add(layout);
            summary = Page("Σύνοψη"); history = Page("Ιστορικό"); payment = Page("Πληρωμή"); printing = Page("Εκτύπωση"); evidence = Page("Τεκμήρια");
            recover = Button("Ανάκτηση κατάστασης",async () => {
                var id = invoiceId; string recovered = null;
                await Run(() => recovered = SharedInvoiceRuntime.RecoverStatus(id));
                if (IsDisposed || invoiceId != id || recovered == null) return;
                if (status?.ProviderResend?.CanResend == true) await ConfirmProviderResend(id,recovered);
                else RadMessageBox.Show(recovered,"Ανάκτηση");
            });
            retryPayment = Button("Επανάληψη πληρωμής",async () => {
                if (Busy || status?.RetryPaymentAttemptId == null) return;
                var id = invoiceId; var attemptId = status.RetryPaymentAttemptId.Value;
                await Run(() => {
                    var result = SharedInvoiceRuntime.RetryPayment(id,attemptId);
                    lastActionInvoiceId = id;
                    lastActionMessage = "Πληρωμή: " + StateLabel(result.Payment?.State) +
                        (string.IsNullOrWhiteSpace(result.Payment?.ErrorMessage) ? "" : "\r\n" + result.Payment.ErrorMessage);
                    if (result.Payment?.State == "Approved") lastActionMessage += "\r\nΗ πληρωμή εγκρίθηκε. Η έκδοση μπορεί να συνεχιστεί με την ίδια πληρωμή.";
                });
            });
            retryPayment.Enabled = false; retryPayment.Visible = false;
            voidPayment = Button("Ακύρωση πληρωμής",async () => {
                if (!Program.AdminConnected) { RadMessageBox.Show("Απαιτείται σύνδεση διαχειριστή."); return; }
                string reason;
                if (!AskReason("Ακύρωση πληρωμής POS","Αιτιολογία ακύρωσης (υποχρεωτική)",out reason)) return;
                var id = invoiceId;
                if (RadMessageBox.Show("Να ακυρωθεί η εγκεκριμένη συναλλαγή POS; Η ενέργεια αφορά μόνο πληρωμή που δεν συνδέθηκε με αποδεκτό παραστατικό.",
                    "Ακύρωση πληρωμής",MessageBoxButtons.YesNo,RadMessageIcon.Exclamation) != DialogResult.Yes) return;
                await Run(() => { var result = SharedInvoiceRuntime.VoidPayment(id,reason); RecordCompensation(id,result); });
            });
            recoverVoid = Button("Ανάκτηση ακύρωσης",async () => {
                var id = invoiceId;
                await Run(() => { var result = SharedInvoiceRuntime.RecoverVoid(id); RecordCompensation(id,result); });
            });
            refundPayment = Button("Πλήρης επιστροφή",async () => {
                if (!Program.AdminConnected) { RadMessageBox.Show("Απαιτείται σύνδεση διαχειριστή."); return; }
                if (status?.RefundOriginalInvoiceId == null) return;
                var id = invoiceId; var original = status.RefundOriginalInvoiceId.Value;
                if (RadMessageBox.Show("Να εκτελεστεί πλήρης επιστροφή της αρχικής συναλλαγής, διαβίβαση και εκτύπωση του πιστωτικού;\r\n\r\nΔεν υποστηρίζεται μερική ή δεύτερη επιστροφή.",
                    "Πλήρης επιστροφή POS",MessageBoxButtons.YesNo,RadMessageIcon.Exclamation) != DialogResult.Yes) return;
                await Run(() => { var result = SharedInvoiceRuntime.RefundPayment(id,original); RecordPaymentResult(id,"Επιστροφή",result); });
            });
            recoverRefund = Button("Ανάκτηση επιστροφής",async () => {
                if (status?.RefundOriginalInvoiceId == null) return;
                var id = invoiceId; var original = status.RefundOriginalInvoiceId.Value;
                await Run(() => { var result = SharedInvoiceRuntime.RecoverRefund(id,original); RecordPaymentResult(id,"Επιστροφή",result); });
            });
            diagnostics = Button("Διαγνωστικά GBridge",async () => {
                using (var form = new GBridgeDiagnosticsForm(invoiceId)) form.ShowDialog(this);
                await Task.FromResult(true);
            });
            resume = Button("Συνέχιση έκδοσης",async () => {
                if (!Owned) { await SubmitPrintedInvoice(); return; }
                var id = invoiceId;
                if (status?.ProviderResend?.CanResend == true) { await ConfirmProviderResend(id,null); return; }
                bool rejectedSubmission = status.State("Submission") == "Rejected";
                var question = rejectedSubmission
                    ? "Να γίνει νέα διαβίβαση με το διορθωμένο αίτημα; Η απορριφθείσα προσπάθεια θα παραμείνει στο ιστορικό."
                    : status.State("Payment") == "Declined" || status.State("Payment") == "Failed"
                    ? "Νέα προσπάθεια πληρωμής POS και συνέχιση έκδοσης με τα αρχικά στοιχεία;"
                    : status.ConfiguredProvider != null && !status.Attempts.Any()
                    ? "Συνέχιση με τα αποθηκευμένα στοιχεία παραστατικού και τον λογαριασμό / συνδρομή από τις ρυθμίσεις: " + status.ConfiguredProvider.Account + ";"
                    : "Συνέχιση με τα αρχικά αποθηκευμένα στοιχεία;";
                if (RadMessageBox.Show(question, "Έκδοση",MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    ASFuelControl.Invoicing.InvoiceWorkflowResult result = null;
                    await Run(() => { result = SharedInvoiceRuntime.ResumeIssuance(id); RecordResult(id,result); });
                    if (!IsDisposed && invoiceId == id && result != null) ShowResult(result,"Συνέχιση έκδοσης");
                }
            });
            print = Button("Εκτύπωση / αντίγραφο",async () => await PrintSaved());
            reconcile = Button("Έλεγχος εκτύπωσης…",async () => {
                if (!Program.AdminConnected) { RadMessageBox.Show("Απαιτείται σύνδεση διαχειριστή."); return; }
                var id = invoiceId;
                var job = status.Attempts.Last(a => a.Family == "Print");
                bool confirmed; string reason;
                if (!AskPrintOutcome(out reason,out confirmed)) return;
                await Run(() => SharedInvoiceRuntime.ReconcilePrint(id,job.AttemptId,confirmed,reason));
            });
        }
        private RadTextBox Page(string title)
        {
            var page = new RadPageViewPage { Text = title };
            var text = new RadTextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical };
            page.Controls.Add(text); pages.Pages.Add(page); return text;
        }
        internal void AddPage(string title, Control content)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            var page = new RadPageViewPage { Text = title };
            content.Dock = DockStyle.Fill;
            page.Controls.Add(content);
            pages.Pages.Add(page);
        }
        private RadButton Button(string title,Func<Task> action)
        {
            var button = new RadButton { Text = title,Width = 175,Height = 30 };
            button.Click += async (s,e) => await action(); actions.Controls.Add(button); return button;
        }
        internal void LoadInvoice(Guid id)
        {
            if (invoiceId != id) lastActionMessage = null;
            invoiceId = id;
            try
            {
                ReadFailed = false;
                status = id == Guid.Empty ? null : SharedInvoiceRuntime.ReadStatus(id);
                var snapshot = status?.Snapshot;
                retryPayment.Visible = snapshot?.Payment != null;
                retryPayment.Enabled = snapshot?.Payment != null && status.RetryPaymentAttemptId.HasValue;
                voidPayment.Visible = status?.PaymentAttemptId.HasValue == true && !status.IsRefundPayment;
                voidPayment.Enabled = status?.CanVoidPayment == true;
                recoverVoid.Visible = status?.Compensation?.Operation == "Void";
                recoverVoid.Enabled = status?.CanRecoverVoid == true;
                refundPayment.Visible = status?.RefundOriginalInvoiceId.HasValue == true;
                refundPayment.Enabled = status?.CanRefundPayment == true;
                recoverRefund.Visible = status?.Compensation?.Operation == "Refund";
                recoverRefund.Enabled = status?.CanRecoverRefund == true;
                diagnostics.Visible = status?.PaymentAttemptId.HasValue == true || status?.Compensation != null || snapshot?.Payment != null;
                diagnostics.Enabled = diagnostics.Visible;
                resume.Text = snapshot == null && status?.CanSubmitPrinted == true ? "Διαβίβαση" :
                    status?.State("Submission") == "Rejected" || status?.ProviderResend?.CanResend == true
                    ? "Επανάληψη διαβίβασης" : "Συνέχιση έκδοσης";
                if (snapshot == null)
                {
                    summary.Text = Owned ? "Υπάρχει κοινό φορολογικό ιστορικό χωρίς αποθηκευμένη ροή. Απαιτείται έλεγχος ιστορικών στοιχείων." :
                        status?.PendingSummary ?? "Δεν έχει ξεκινήσει η κοινή έκδοση. Επιλέξτε ή αποθηκεύστε το παραστατικό για να εμφανιστεί η διαβίβαση.";
                    history.Text = payment.Text = printing.Text = evidence.Text = "";
                    recover.Enabled = retryPayment.Enabled = resume.Enabled = print.Enabled = reconcile.Enabled = false;
                    resume.Enabled = !Owned && status?.CanSubmitPrinted == true;
                    return;
                }
                var configured = !status.Attempts.Any() ? status.ConfiguredProvider : null;
                var provider = configured ?? snapshot.Provider;
                summary.Text = "Αποθηκευμένη διαβίβαση: " + SharedInvoiceConfiguration.TransmissionLabel(snapshot.Provider) + "\r\n" + (provider == null ? snapshot.Signing?.Scope.DeviceId :
                    (provider.TestMode ? "Δοκιμαστικό" : "Παραγωγή") + " · Λογαριασμός / συνδρομή: " + provider.Account + (configured == null ? "" : " (ρυθμίσεις)")) +
                    "\r\nΣήμανση: " + (snapshot.Signing != null ? StateLabel(status.State("Signing")) : snapshot.Provider != null && snapshot.Provider.ProviderKey != "myDATA" ? "Μέσω παρόχου" : "Δεν απαιτείται") + "    Πληρωμή: " + (snapshot.Payment == null ? "Δεν απαιτείται" : status.RetryPaymentAttemptId.HasValue ? "Αναμονή επανάληψης" : StateLabel(status.State("Payment"))) +
                    "\r\nΔιαβίβαση: " + (snapshot.Provider == null ? "Χωρίς διαβίβαση" : StateLabel(status.State("Submission"))) + "    Εκτύπωση: " + PrintStatus();
                if (!string.IsNullOrWhiteSpace(status.ConnectionProblem)) summary.Text += "\r\nΈλεγχος σύνδεσης: " + status.ConnectionProblem;
                if (status.State("Submission") == "Rejected")
                    summary.Text += "\r\nΗ προηγούμενη διαβίβαση απορρίφθηκε. Επιλέξτε «Επανάληψη διαβίβασης» για νέα προσπάθεια με το διορθωμένο αίτημα.";
                if (status.ProviderResend?.CanResend == true)
                    summary.Text += "\r\nΗ έκβαση δεν επιβεβαιώθηκε. Επιλέξτε «Επανάληψη διαβίβασης» για νέα προσπάθεια μετά από ρητή επιβεβαίωση του κινδύνου διπλής έκδοσης.";
                if (string.IsNullOrWhiteSpace(CurrentActionMessage) && snapshot.Provider != null && snapshot.Payment == null && snapshot.Signing == null && !status.Attempts.Any(a => a.Family == "Submission"))
                    summary.Text += "\r\nΈχουν αποθηκευτεί τα στοιχεία έκδοσης, αλλά δεν έχει καταγραφεί αποστολή παραστατικού.\r\nΗ σύνδεση / προετοιμασία μπορεί να μην ολοκληρώθηκε. Η «Συνέχιση έκδοσης» εμφανίζει το αποτέλεσμα.";
                history.Text = string.Join("\r\n",status.Revisions.Select(a => a.UpdatedUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss") + " · " + a.Family + " · " + StateLabel(a.State) + " · " + a.AttemptId));
                payment.Text = snapshot.Payment == null ? "Δεν υπάρχει ενσωματωμένη πληρωμή POS." : "GBridge · " + snapshot.Payment.TerminalId +
                    "\r\nΤρόπος POS: " + (snapshot.Payment.PosMode ?? (snapshot.Payment.Provider.ProviderKey == "Ilyda.Direct" ? snapshot.Payment.IlydaProtocol :
                    snapshot.Payment.ITimologiaSignatureFormat == 1 ? "EDPS / Viva (παλαιό προφίλ)" : "Mellon / ePay / Cardlink / Nexi (παλαιό προφίλ)")) +
                    "\r\n" + (status.RetryPaymentAttemptId.HasValue ? "Αναμονή επανάληψης" : StateLabel(status.State("Payment"))) + "\r\n" + Details("Payment");
                var latestPayment = status.Attempts.LastOrDefault(a => a.Family == "Payment" && a.Operation == "Payment");
                var latestAttempt = latestPayment == null ? null : JObject.Parse(latestPayment.SnapshotJson)["Attempt"];
                var operatorRetryId = latestAttempt?["OperatorRetryOfAttemptId"];
                var automaticRetryId = latestAttempt?["AutomaticRetryOfAttemptId"];
                if (operatorRetryId?.Type == JTokenType.String || automaticRetryId?.Type == JTokenType.String)
                {
                    bool byOperator = operatorRetryId?.Type == JTokenType.String;
                    var previousId = (Guid)(byOperator ? operatorRetryId : automaticRetryId);
                    var previous = status.Attempts.FirstOrDefault(a => a.AttemptId == previousId);
                    var policy = previous == null ? null : (string)JObject.Parse(previous.SnapshotJson)[byOperator ? "OperatorRetry" : "AutomaticRetry"]?["Policy"];
                    payment.Text += "\r\n" + (byOperator ? "Νέα προσπάθεια κατόπιν επιλογής χειριστή μετά από " : "Παλαιότερη αυτόματη προσπάθεια μετά από ") +
                        (policy == "GBridge.F4.v1" ? "F4" : "F4 → F2 → 0E") + ". Η προηγούμενη συναλλαγή διατηρείται στο ιστορικό.";
                }
                if (status.RetryPaymentAttemptId.HasValue)
                {
                    const string retryMessage = "Η πληρωμή δεν ολοκληρώθηκε. Ελέγξτε το POS και επιλέξτε «Επανάληψη πληρωμής» για νέα προσπάθεια.\r\nΗ «Ανάκτηση κατάστασης» ελέγχει μόνο την προηγούμενη συναλλαγή.";
                    summary.Text += "\r\n" + retryMessage;
                    payment.Text += "\r\n" + retryMessage;
                }
                if (status.PaymentAttemptId.HasValue)
                    payment.Text += "\r\nΠροσπάθεια: " + status.PaymentAttemptId +
                        (status.OriginalPaymentAttemptId.HasValue ? "\r\nΑρχική / αντικατασταθείσα: " + status.OriginalPaymentAttemptId : "");
                if (status.Compensation != null)
                    payment.Text += "\r\n" + (status.Compensation.Operation == "Void" ? "Ακύρωση" : "Επιστροφή") +
                        ": " + StateLabel(status.Compensation.State.ToString()) +
                        "\r\nΕνέργεια: " + status.Compensation.Id +
                        "\r\nΑρχική πληρωμή: " + status.Compensation.OriginalPaymentAttemptId +
                        (string.IsNullOrWhiteSpace(status.Compensation.Evidence) ? "" : "\r\n" + status.Compensation.Evidence);
                if (status.RefundOriginalInvoiceId.HasValue)
                    payment.Text += "\r\nΑρχικό παραστατικό επιστροφής: " + status.RefundOriginalInvoiceId;
                printing.Text = PrintStatus() + "\r\n" + snapshot.PrintDestination + "\r\nΗ παράδοση στα Windows δεν επιβεβαιώνει φυσική εκτύπωση.\r\n" + Details("Print");
                if (status.State("Signing") == "Unknown") summary.Text += "\r\nΕλέγξτε την έκβαση στον μηχανισμό. Δεν επιτρέπεται νέα σήμανση.";
                evidence.Text = string.Join("\r\n",status.Attempts.Select(a => a.Family + " · " + a.AttemptId + "\r\nSHA-256: " + a.RequestSha256)) +
                    "\r\n" + Details("Submission") + "\r\n" + Details("Signing");
                if (snapshot.Provider != null) evidence.Text += "\r\nΑποθηκευμένος λογαριασμός / συνδρομή: " + snapshot.Provider.Account + "\r\nΠροφίλ διαπιστευτηρίων: " + snapshot.Provider.CredentialProfile;
                if (configured != null) evidence.Text += "\r\nΠροφίλ από τις τρέχουσες ρυθμίσεις: " + configured.CredentialProfile;
                recover.Enabled = status.Attempts.Any() && !status.Attempts.Any(a => a.Family == "Print" && a.Operation != "PrintTF2");
                resume.Enabled = status.ProviderResend?.CanResend == true ||
                    !status.Attempts.Any(a => a.Family == "Print" || a.Family == "Cancellation") &&
                    !status.Attempts.GroupBy(a => a.Family).Select(g => g.Last()).Any(a => a.State == "Unknown" || a.State == "Signing" || a.State == "Sending" || a.State == "Queued" || a.State == "QueueFailed");
                bool printable = snapshot.LocalOnly || status.Attempts.Any(a => a.Operation == "PrintTF2") || (snapshot.Provider != null ? status.State("Submission") == "Accepted" || status.State("Submission") == "Queued" : status.State("Signing") == "Signed");
                if (printable || status.RetryPaymentAttemptId.HasValue || !string.IsNullOrWhiteSpace(status.ConnectionProblem)) resume.Enabled = false;
                print.Enabled = printable && status.State("Print") != "Unknown" && status.State("Print") != "Dispatching";
                reconcile.Enabled = status.State("Print") == "Unknown" || status.State("Print") == "Dispatching";
            }
            catch (Exception ex)
            {
                ReadFailed = true;
                status = null; summary.Text = "Δεν ήταν δυνατός ο έλεγχος: " + ex.Message;
                recover.Enabled = retryPayment.Enabled = resume.Enabled = print.Enabled = reconcile.Enabled =
                    voidPayment.Enabled = recoverVoid.Enabled = refundPayment.Enabled = recoverRefund.Enabled = diagnostics.Enabled = false;
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(CurrentActionMessage)) summary.Text += "\r\n\r\nΤελευταία ενέργεια:\r\n" + CurrentActionMessage;
                StateChanged?.Invoke(this,EventArgs.Empty);
            }
        }
        private string PrintStatus() => status.AlreadyPrinted && !status.Attempts.Any(a => a.Family == "Print")
            ? "Ήδη εκτυπωμένο · δεν έγινε νέα εκτύπωση" : StateLabel(status.State("Print"));
        internal void RecordResult(Guid id,ASFuelControl.Invoicing.InvoiceWorkflowResult result)
        { if (result != null) { lastActionInvoiceId = id; lastActionMessage = ResultMessage(result); } }
        private void RecordCompensation(Guid id,ASFuelControl.Invoicing.Persistence.PaymentCompensationRecord result)
        {
            lastActionInvoiceId = id;
            lastActionMessage = (result.Operation == "Void" ? "Ακύρωση πληρωμής: " : "Επιστροφή: ") +
                StateLabel(result.State.ToString()) + (string.IsNullOrWhiteSpace(result.Evidence) ? "" : "\r\n" + result.Evidence);
        }
        private void RecordPaymentResult(Guid id,string title,ASFuelControl.Invoicing.ProviderPaymentResult result)
        {
            lastActionInvoiceId = id;
            lastActionMessage = title + ": " + StateLabel(result.Payment?.State) +
                "\r\nΔιαβίβαση: " + StateLabel(result.Submission?.State) +
                (string.IsNullOrWhiteSpace(result.Payment?.ErrorMessage) ? "" : "\r\n" + result.Payment.ErrorMessage) +
                (result.Submission?.Response?.Errors == null ? "" : "\r\n" + string.Join("\r\n",result.Submission.Response.Errors));
        }
        internal static string ResultMessage(ASFuelControl.Invoicing.InvoiceWorkflowResult result)
        {
            var message = "Διαβίβαση: " + StateLabel(result.FiscalState);
            var errors = (result.Response?.Errors ?? new string[0]).Where(e => !string.IsNullOrWhiteSpace(e));
            var warnings = (result.Response?.Warnings ?? new string[0]).Where(e => !string.IsNullOrWhiteSpace(e));
            if (errors.Any()) message += "\r\n" + string.Join("\r\n",errors);
            if (warnings.Any()) message += "\r\n" + string.Join("\r\n",warnings);
            if (Data.Implementation.OptionHandler.Instance.MyDieselEnabled && !string.IsNullOrWhiteSpace(result.MyDieselState))
                message += "\r\nmyDiesel: " + MyDieselEvidenceForm.StageLabel(result.MyDieselState);
            if (result.Print != null) message += "\r\nΕκτύπωση: " + StateLabel(result.Print.State);
            return message;
        }
        private void ShowResult(ASFuelControl.Invoicing.InvoiceWorkflowResult result,string title)
        {
            RadMessageBox.Show(ResultMessage(result),title,MessageBoxButtons.OK,
                result.Accepted ? RadMessageIcon.Info : RadMessageIcon.Exclamation);
        }
        private string Details(string family)
        {
            var entry = status.Attempts.LastOrDefault(a => a.Family == family);
            if (entry == null) return "";
            // Whitelist result fields; session credentials, communication tokens and
            // raw request/response bodies are deliberately not placed in UI controls.
            var allowed = new[] { "Mark","Uid","Signature","InvoiceSignature","QrCode","QrCodeUrl","TransactionId","ResponseTerminalId","Amount","AmountMinorUnits","OriginalInvoiceId","OriginalPaymentAttemptId","QueueState","QueueErrors" };
            if (family == "Payment") allowed = allowed.Concat(new[] { "ErrorMessage" }).ToArray();
            var token = JToken.Parse(entry.SnapshotJson);
            if (family == "Print") return "Αιτιολογία: " + (string)token["Reason"] + "\r\nΤεκμήριο: " + (string)token["Response"];
            return string.Join("\r\n",((JContainer)token).DescendantsAndSelf().OfType<JProperty>().Where(p => allowed.Contains(p.Name) && p.Value is JValue && p.Value.Type != JTokenType.Null)
                .Select(p => p.Name == "ErrorMessage" ? (string)p.Value : p.Name + ": " + p.Value));
        }
        internal static string StateLabel(string state)
        {
            if (state != null && state.StartsWith("Payment: ",StringComparison.Ordinal)) return "Πληρωμή: " + StateLabel(state.Substring(9));
            switch (state)
            {
                case "Prepared": return "Προετοιμάστηκε";
                case "NotSubmitted": return "Δεν εστάλη";
                case "LocalOnly": return "Χωρίς διαβίβαση";
                case "Signing": return "Σε σήμανση";
                case "Signed": return "Σημάνθηκε";
                case "Sending": return "Σε αποστολή";
                case "Accepted": return "Αποδεκτό";
                case "Approved": return "Εγκρίθηκε";
                case "Invoiced": return "Τιμολογήθηκε";
                case "Unknown": return "Άγνωστη έκβαση";
                case "Rejected": return "Απορρίφθηκε";
                case "Queued": return "Σε ουρά παρόχου";
                case "QueueFailed": return "Σφάλμα ουράς — απαιτείται παρέμβαση";
                case "RetryAuthorized": return "Εγκρίθηκε νέα διαβίβαση";
                case "Declined": return "Μη εγκεκριμένο";
                case "FailedBeforeDispatch": return "Τοπική αποτυχία";
                case "Dispatching": return "Σε εκτύπωση";
                case "HandedOff": return "Παραδόθηκε στα Windows";
                case "Confirmed": return "Επιβεβαιώθηκε";
                default: return state;
            }
        }
        internal void ShowLegacyErrors(string[] errors)
        {
            if (!Owned && !ReadFailed && errors != null) summary.Text += "\r\n" + string.Join("\r\n",errors);
        }
        private async Task Run(Action action)
        {
            if (Busy) return;
            var actionInvoiceId = invoiceId;
            Busy = true;
            lastActionMessage = null;
            actions.Enabled = false;
            StateChanged?.Invoke(this,EventArgs.Empty);
            try { await Task.Run(action); }
            catch (Exception ex) { lastActionInvoiceId = actionInvoiceId; lastActionMessage = ex.Message; if (invoiceId == actionInvoiceId) RadMessageBox.Show(ex.Message,"Κοινή τιμολόγηση",MessageBoxButtons.OK,RadMessageIcon.Error); }
            finally { Busy = false; if (!IsDisposed) { actions.Enabled = true; LoadInvoice(invoiceId); ActionCompleted?.Invoke(this,EventArgs.Empty); } }
        }
        private async Task ConfirmProviderResend(Guid id,string recoveryMessage)
        {
            var assessment = status?.ProviderResend;
            if (assessment?.CanResend != true || invoiceId != id) return;
            var message = (string.IsNullOrWhiteSpace(recoveryMessage) ? "" : recoveryMessage + "\r\n\r\n") + assessment.Warning;
            if (RadMessageBox.Show(message,"Επανάληψη διαβίβασης",MessageBoxButtons.YesNo,RadMessageIcon.Exclamation) != DialogResult.Yes) return;
            ASFuelControl.Invoicing.InvoiceWorkflowResult result = null;
            await Run(() => { result = SharedInvoiceRuntime.AuthorizeAndResumeSubmission(id); RecordResult(id,result); });
            if (!IsDisposed && invoiceId == id && result != null) ShowResult(result,"Επανάληψη διαβίβασης");
        }
        internal async Task PrintSaved()
        {
            if (!Owned || !print.Enabled || !actions.Enabled) return;
            await PrintInvoice();
        }
        internal async Task SubmitPrintedInvoice()
        {
            if (ReadFailed || Busy || Owned || status?.CanSubmitPrinted != true) return;
            if (RadMessageBox.Show("Να διαβιβαστεί το αποθηκευμένο παραστατικό με τον υπάρχοντα αριθμό, σειρά και ημερομηνία; Δεν θα γίνει νέα εκτύπωση.",
                "Διαβίβαση",MessageBoxButtons.YesNo,RadMessageIcon.Question) != DialogResult.Yes) return;
            var id = invoiceId;
            ASFuelControl.Invoicing.InvoiceWorkflowResult result = null;
            await Run(() => { result = SharedInvoiceRuntime.SubmitPrintedInvoice(id); RecordResult(id,result); });
            if (!IsDisposed && invoiceId == id && result != null)
                ShowResult(result,"Διαβίβαση");
        }
        internal async Task PrintInvoice()
        {
            if (ReadFailed || Busy || !actions.Enabled) return;
            var id = invoiceId;
            var previous = status?.Attempts.LastOrDefault(a => a.Family == "Print");
            bool reprint = previous != null && (previous.State == "HandedOff" || previous.State == "Confirmed");
            if (reprint && RadMessageBox.Show("Θέλετε να εκτυπώσετε ξανά το παραστατικό;",
                "Επανεκτύπωση Παραστατικού",MessageBoxButtons.YesNo,RadMessageIcon.Question) != DialogResult.Yes)
                return;
            string reason = reprint ? SharedInvoiceRuntime.OperatorRequestedCopyReason : null;
            // First issuance and recovery use exactly the PrintAgent workflow. Only
            // an explicitly requested copy supplies an audit reason; the print click
            // itself is sufficient and does not require a second operator dialog.
            ASFuelControl.Invoicing.InvoiceWorkflowResult result = null;
            await Run(() => { result = SharedInvoiceRuntime.Process(id,true,reason); RecordResult(id,result); });
            if (!IsDisposed && invoiceId == id && result != null && (result.Print == null ||
                (result.Print.State != "HandedOff" && result.Print.State != "Confirmed")))
                ShowResult(result,"Εκτύπωση Παραστατικού");
        }
        private bool AskPrintOutcome(out string reason,out bool confirmed)
        {
            using (var dialog = new RadForm { Text = "Έκβαση εκτύπωσης",ClientSize = new Size(550,220),StartPosition = FormStartPosition.CenterParent })
            {
                var label = new RadLabel { Text = "Αιτιολογία / τεκμήριο (υποχρεωτικό)",Dock = DockStyle.Top,Height = 30 };
                var text = new RadTextBox { Multiline = true,Dock = DockStyle.Fill };
                var choice = InvoicingFields.Choice("Επιβεβαιωμένη εκτύπωση","Επιβεβαιωμένη μη εκτύπωση"); choice.Dock = DockStyle.Top;
                var accept = new RadButton { Text = "Καταχώριση",Dock = DockStyle.Bottom,Height = 36 };
                accept.Click += (s,e) => { if (!string.IsNullOrWhiteSpace(text.Text)) dialog.DialogResult = DialogResult.OK; };
                dialog.Controls.Add(text); dialog.Controls.Add(choice); dialog.Controls.Add(label); dialog.Controls.Add(accept);
                bool ok = dialog.ShowDialog(this) == DialogResult.OK;
                reason = text.Text.Trim(); confirmed = choice.SelectedIndex == 0; return ok;
            }
        }
        private bool AskReason(string title,string prompt,out string reason)
        {
            using (var dialog = new RadForm { Text = title,ClientSize = new Size(550,180),StartPosition = FormStartPosition.CenterParent })
            {
                var label = new RadLabel { Text = prompt,Dock = DockStyle.Top,Height = 30 };
                var text = new RadTextBox { Multiline = true,Dock = DockStyle.Fill };
                var accept = new RadButton { Text = "Συνέχεια",Dock = DockStyle.Bottom,Height = 36 };
                accept.Click += (s,e) => { if (!string.IsNullOrWhiteSpace(text.Text)) dialog.DialogResult = DialogResult.OK; };
                dialog.Controls.Add(text); dialog.Controls.Add(label); dialog.Controls.Add(accept);
                bool ok = dialog.ShowDialog(this) == DialogResult.OK;
                reason = text.Text.Trim(); return ok;
            }
        }
    }
}
