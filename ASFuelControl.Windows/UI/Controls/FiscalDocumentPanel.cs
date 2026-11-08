using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ASFuelControl.Windows.Threads;
using ASFuelControl.Windows.UI.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Telerik.WinControls;
using Telerik.WinControls.UI;

namespace ASFuelControl.Windows.UI.Controls
{
    internal sealed class FiscalDocumentPanel : UserControl
    {
        private readonly RadPageView pages = new RadPageView { Dock = DockStyle.Fill };
        private readonly RadTextBox summary, history, payment, printing, evidence;
        private readonly RadPageViewPage fiscalAttemptPage = new RadPageViewPage { Text = "Fiscal Attempt" };
        private readonly RadPageViewPage fiscalAttemptRequestPage = new RadPageViewPage { Text = "Αίτημα" };
        private readonly RadLabel fiscalAttemptInfo = new RadLabel { Dock = DockStyle.Top, Height = 24 };
        private readonly RadTextBox fiscalAttemptRequest = PayloadBox(), fiscalAttemptResponse = PayloadBox();
        private readonly RadButton fiscalAttemptPrintPreview = new RadButton {
            Text = "Προεπισκόπηση εγγράφου", Dock = DockStyle.Top, Height = 32, Visible = false
        };
        private readonly FlowLayoutPanel actions = new FlowLayoutPanel { Dock = DockStyle.Fill,WrapContents = true };
        private readonly FlowLayoutPanel paymentActions = new FlowLayoutPanel {
            Dock = DockStyle.Fill,WrapContents = false,AutoScroll = true,
            FlowDirection = FlowDirection.LeftToRight
        };
        private readonly TableLayoutPanel rootLayout;
        private TableLayoutPanel paymentLayout;
        private readonly RadButton recover, retryPayment, resume, reconcile, changePayment, voidPayment, recoverVoid, refundPayment, recoverRefund, diagnostics;
        private Guid invoiceId;
        private SharedInvoiceStatus status;
        private string lastActionMessage;
        private Guid lastActionInvoiceId;
        private bool fiscalAttemptPageAdded;
        private byte[][] fiscalAttemptPrintPages;
        private string CurrentActionMessage => lastActionInvoiceId == invoiceId ? lastActionMessage : null;
        internal bool Owned => status?.Snapshot != null || status?.Attempts.Any() == true;
        internal string InvoiceSignature => status?.InvoiceSignature;
        internal bool ReadFailed { get; private set; }
        internal bool Busy { get; private set; }
        internal event EventHandler StateChanged;
        internal event EventHandler ActionCompleted;
        internal FiscalDocumentPanel()
        {
            rootLayout = new TableLayoutPanel { Dock = DockStyle.Fill,ColumnCount = 1,RowCount = 2,Margin = new Padding(0) };
            rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute,42));
            rootLayout.Controls.Add(pages,0,0); rootLayout.Controls.Add(actions,0,1);
            Controls.Add(rootLayout);
            pages.SelectedPageChanged += PagesSelectedPageChanged;
            summary = Page("Σύνοψη"); history = Page("Ιστορικό");
            payment = Page("Πληρωμή",paymentActions);
            printing = Page("Εκτύπωση"); evidence = Page("Τεκμήρια");
            BuildFiscalAttemptPage();
            Program.AdminConnectedEvent += AdminConnectedChanged;
            Disposed += (s,e) => Program.AdminConnectedEvent -= AdminConnectedChanged;
            recover = Button("Ανάκτηση κατάστασης",async () => {
                var id = invoiceId; string recovered = null;
                await Run(() => recovered = SharedInvoiceRuntime.RecoverStatus(id));
                if (IsDisposed || invoiceId != id || recovered == null) return;
                if (status?.ProviderResend?.CanResend == true) await ConfirmProviderResend(id,recovered);
                else RadMessageBox.Show(recovered,"Ανάκτηση");
            },paymentActions);
            retryPayment = Button("Επανάληψη πληρωμής",async () => {
                if (Busy || status?.RetryPaymentAttemptId == null) return;
                var id = invoiceId; var attemptId = status.RetryPaymentAttemptId.Value;
                ASFuelControl.Invoicing.InvoiceWorkflowResult issuanceResult = null;
                await Run(() => {
                    var paymentResult = SharedInvoiceRuntime.RetryPayment(id,attemptId);
                    var paymentState = paymentResult.Payment?.State;
                    lastActionInvoiceId = id;
                    var paymentStateLabel = StateLabel(paymentState);
                    var paymentError = paymentResult.Payment?.ErrorMessage;
                    lastActionMessage = "Πληρωμή: " + paymentStateLabel +
                        (string.IsNullOrWhiteSpace(paymentError) ? "" : "\r\n" + paymentError);
                    if (paymentState != "Approved" && paymentState != "Invoiced")
                    {
                        return;
                    }

                    // The approved payment is durable. Re-entering the complete
                    // workflow reuses it, submits the invoice to the provider and
                    // continues printing without authorizing a second payment.
                    issuanceResult = SharedInvoiceRuntime.Process(id,true,null);
                    RecordResult(id,issuanceResult);
                });
                if (!IsDisposed && invoiceId == id && issuanceResult != null)
                {
                    ShowResult(issuanceResult,"Έκδοση μετά την πληρωμή");
                }
            },paymentActions);
            retryPayment.Enabled = false; retryPayment.Visible = false;
            changePayment = Button("Αλλαγή τρόπου πληρωμής",async () => {
                if (Busy || status?.CanChangePaymentMethod != true) return;
                int selected;
                if (!SelectPaymentMethod(status.PaymentType,out selected) || selected == status.PaymentType) return;
                var description = Common.Enumerators.EnumToList.GetDescription<Common.Enumerators.PaymentTypeEnum>(
                    (Common.Enumerators.PaymentTypeEnum)selected);
                if (RadMessageBox.Show("Να αλλάξει ο τρόπος πληρωμής σε «" + description + "»;\r\n\r\n" +
                    "Το προηγούμενο μη ολοκληρωμένο payload θα παραμείνει στο ιστορικό και θα δημιουργηθεί νέο πριν από την επόμενη έκδοση.",
                    "Αλλαγή τρόπου πληρωμής",MessageBoxButtons.YesNo,RadMessageIcon.Exclamation) != DialogResult.Yes) return;
                var id = invoiceId;
                await Run(() => {
                    var message = SharedInvoiceRuntime.ChangePaymentMethod(id,selected);
                    lastActionInvoiceId = id; lastActionMessage = message;
                });
            });
            changePayment.Width = 205;
            changePayment.Visible = changePayment.Enabled = false;
            voidPayment = Button("Ακύρωση πληρωμής",async () => {
                if (!Program.AdminConnected) { RadMessageBox.Show("Απαιτείται σύνδεση διαχειριστή."); return; }
                string reason;
                if (!AskReason("Ακύρωση πληρωμής POS","Αιτιολογία ακύρωσης (υποχρεωτική)",out reason)) return;
                var id = invoiceId;
                if (RadMessageBox.Show("Να ακυρωθεί η εγκεκριμένη συναλλαγή POS; Η ενέργεια αφορά μόνο πληρωμή που δεν συνδέθηκε με αποδεκτό παραστατικό.",
                    "Ακύρωση πληρωμής",MessageBoxButtons.YesNo,RadMessageIcon.Exclamation) != DialogResult.Yes) return;
                await Run(() => { var result = SharedInvoiceRuntime.VoidPayment(id,reason); RecordCompensation(id,result); });
            },paymentActions);
            recoverVoid = Button("Ανάκτηση ακύρωσης",async () => {
                var id = invoiceId;
                await Run(() => { var result = SharedInvoiceRuntime.RecoverVoid(id); RecordCompensation(id,result); });
            },paymentActions);
            refundPayment = Button("Πλήρης επιστροφή",async () => {
                if (!Program.AdminConnected) { RadMessageBox.Show("Απαιτείται σύνδεση διαχειριστή."); return; }
                if (status?.RefundOriginalInvoiceId == null) return;
                var id = invoiceId; var original = status.RefundOriginalInvoiceId.Value;
                if (RadMessageBox.Show("Να εκτελεστεί πλήρης επιστροφή της αρχικής συναλλαγής, διαβίβαση και εκτύπωση του πιστωτικού;\r\n\r\nΔεν υποστηρίζεται μερική ή δεύτερη επιστροφή.",
                    "Πλήρης επιστροφή POS",MessageBoxButtons.YesNo,RadMessageIcon.Exclamation) != DialogResult.Yes) return;
                await Run(() => { var result = SharedInvoiceRuntime.RefundPayment(id,original); RecordPaymentResult(id,"Επιστροφή",result); });
            },paymentActions);
            recoverRefund = Button("Ανάκτηση επιστροφής",async () => {
                if (status?.RefundOriginalInvoiceId == null) return;
                var id = invoiceId; var original = status.RefundOriginalInvoiceId.Value;
                await Run(() => { var result = SharedInvoiceRuntime.RecoverRefund(id,original); RecordPaymentResult(id,"Επιστροφή",result); });
            },paymentActions);
            diagnostics = Button("Διαγνωστικά GBridge",async () => {
                using (var form = new GBridgeDiagnosticsForm(invoiceId)) form.ShowDialog(this);
                await Task.FromResult(true);
            },paymentActions);
            resume = Button("Διαβίβαση",async () => {
                if (!Owned) { await SubmitPrintedInvoice(); return; }
                var id = invoiceId;
                if (status?.State("Submission") == "ReconciliationRequired")
                {
                    string recovered = null;
                    await Run(() => recovered = SharedInvoiceRuntime.RecoverStatus(id));
                    if (!IsDisposed && invoiceId == id && recovered != null)
                    {
                        RadMessageBox.Show(recovered,"Ανάκτηση διαβίβασης");
                    }
                    return;
                }
                if (status?.IssueDateMismatchRejected == true &&
                    status.CanReissueAfterIssueDateMismatch != true)
                {
                    RadMessageBox.Show("Η απόρριψη αφορά την ημερομηνία έκδοσης, αλλά δεν μπορεί να γίνει ασφαλής νέα αρίθμηση.\r\n\r\n" +
                        (status.IssueDateReissueBlockReason ?? "Απαιτείται έλεγχος του φορολογικού ιστορικού."),
                        "Αλλαγή ημερομηνίας",MessageBoxButtons.OK,RadMessageIcon.Exclamation);
                    return;
                }
                if (status?.CanReissueAfterIssueDateMismatch == true)
                {
                    var saved = status.Snapshot?.Invoice?.Invoice;
                    var oldIdentity = saved == null ? string.Empty :
                        "\r\n\r\nΑρχικό παραστατικό: " + (saved.Series ?? string.Empty) + saved.Number +
                        " / " + saved.PrintedDateTime.ToString("dd/MM/yyyy");
                    var reissueQuestion = "Ο πάροχος απέρριψε το παραστατικό επειδή η ημερομηνία έκδοσης δεν είναι η σημερινή." +
                        oldIdentity +
                        "\r\n\r\nΝα εκδοθεί ξανά με τη σημερινή ημερομηνία και νέο επόμενο αριθμό;" +
                        "\r\n\r\nΟ αρχικός αριθμός δεν θα επαναχρησιμοποιηθεί και η απόρριψη θα παραμείνει στο ιστορικό.";
                    if (RadMessageBox.Show(reissueQuestion,"Νέα αρίθμηση και έκδοση",
                        MessageBoxButtons.YesNo,RadMessageIcon.Exclamation) != DialogResult.Yes) return;
                    ASFuelControl.Invoicing.InvoiceWorkflowResult reissued = null;
                    await Run(() => { reissued = SharedInvoiceRuntime.ReissueAfterIssueDateMismatch(id);
                        RecordResult(id,reissued); });
                    if (!IsDisposed && invoiceId == id && reissued != null)
                        ShowResult(reissued,"Νέα έκδοση");
                    return;
                }
                // Date-mismatch recovery has precedence over every ordinary
                // provider resend path because the rejected fiscal identity must
                // never be posted again with its obsolete issue date.
                if (status?.ProviderResend?.CanResend == true) { await ConfirmProviderResend(id,null); return; }
                bool rejectedSubmission = status.State("Submission") == "Rejected";
                bool rejectedSigning = status.State("Signing") == "Rejected";
                var question = rejectedSigning
                    ? "Να δημιουργηθεί νέα αίτηση σήμανσης με τα τρέχοντα διορθωμένα στοιχεία του παραστατικού; Η απορριφθείσα προσπάθεια θα παραμείνει στο ιστορικό."
                    : rejectedSubmission
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
                    if (!IsDisposed && invoiceId == id && result != null) ShowResult(result,"Διαβίβαση");
                }
            });
            reconcile = Button("Έλεγχος εκτύπωσης…",async () => {
                if (!Program.AdminConnected) { RadMessageBox.Show("Απαιτείται σύνδεση διαχειριστή."); return; }
                var id = invoiceId;
                var job = status.Attempts.Last(a => a.Family == "Print");
                bool confirmed; string reason;
                if (!AskPrintOutcome(out reason,out confirmed)) return;
                await Run(() => SharedInvoiceRuntime.ReconcilePrint(id,job.AttemptId,confirmed,reason));
            });
            UpdateActionLayoutHeights();
        }
        private RadTextBox Page(string title, FlowLayoutPanel pageActions = null)
        {
            var page = new RadPageViewPage { Text = title };
            var text = new RadTextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical };
            if (pageActions == null)
                page.Controls.Add(text);
            else
            {
                var pageLayout = new TableLayoutPanel { Dock = DockStyle.Fill,ColumnCount = 1,RowCount = 2,Margin = new Padding(0) };
                pageLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
                pageLayout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
                pageLayout.RowStyles.Add(new RowStyle(SizeType.Absolute,50));
                pageLayout.Controls.Add(text,0,0);
                pageLayout.Controls.Add(pageActions,0,1);
                page.Controls.Add(pageLayout);
                if (pageActions == paymentActions)
                {
                    paymentLayout = pageLayout;
                }
            }
            pages.Pages.Add(page); return text;
        }
        private void PagesSelectedPageChanged(object sender, EventArgs e)
        {
            UpdateActionLayoutHeights();
        }
        private void ActionButtonVisibleChanged(object sender, EventArgs e)
        {
            UpdateActionLayoutHeights();
        }
        private void UpdateActionLayoutHeights()
        {
            bool globalActionsVisible = HasVisibleControls(actions);
            rootLayout.RowStyles[1].Height = globalActionsVisible ? 42F : 0F;
            if (paymentLayout != null)
            {
                bool paymentActionsVisible = HasVisibleControls(paymentActions);
                paymentLayout.RowStyles[1].Height = paymentActionsVisible ? 50F : 0F;
            }
        }
        private static bool HasVisibleControls(Control host)
        {
            foreach (Control control in host.Controls)
            {
                if (control.Visible)
                {
                    return true;
                }
            }
            return false;
        }
        private static RadTextBox PayloadBox()
        {
            return new RadTextBox {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                Font = new Font("Consolas",8.5F),
                WordWrap = false
            };
        }
        private void BuildFiscalAttemptPage()
        {
            var payloads = new RadPageView { Dock = DockStyle.Fill };
            var responsePage = new RadPageViewPage { Text = "Απάντηση" };
            var requestLayout = new TableLayoutPanel {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2,
                Margin = new Padding(0)
            };
            requestLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            requestLayout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
            requestLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            requestLayout.Controls.Add(fiscalAttemptRequest,0,0);
            requestLayout.Controls.Add(fiscalAttemptPrintPreview,0,1);
            fiscalAttemptRequestPage.Controls.Add(requestLayout);
            fiscalAttemptPrintPreview.Click += FiscalAttemptPrintPreviewClick;
            responsePage.Controls.Add(fiscalAttemptResponse);
            payloads.Pages.Add(fiscalAttemptRequestPage);
            payloads.Pages.Add(responsePage);
            fiscalAttemptPage.Controls.Add(payloads);
            fiscalAttemptPage.Controls.Add(fiscalAttemptInfo);
            UpdateFiscalAttemptPageVisibility();
        }
        private void AdminConnectedChanged(object sender,EventArgs e)
        {
            if (IsDisposed)
            {
                return;
            }
            if (InvokeRequired)
            {
                Action refreshAdminState = () =>
                {
                    UpdateFiscalAttemptPageVisibility();
                    UpdatePendingSubmissionAction();
                };
                BeginInvoke(refreshAdminState);
                return;
            }
            UpdateFiscalAttemptPageVisibility();
            UpdatePendingSubmissionAction();
        }
        private void UpdatePendingSubmissionAction()
        {
            if (status?.Snapshot != null || Owned)
            {
                return;
            }
            bool canSubmitPrinted = status?.CanSubmitPrinted == true;
            bool canReplaceLegacyEvidence =
                status?.CanReplaceLegacyFiscalEvidence == true;
            resume.Text = canReplaceLegacyEvidence
                ? "Ανάκτηση σήμανσης"
                : "Διαβίβαση";
            resume.Enabled = !ReadFailed && !Busy &&
                (canSubmitPrinted ||
                 canReplaceLegacyEvidence && Program.AdminConnected);
        }
        private void UpdateFiscalAttemptPageVisibility()
        {
            if (Program.AdminConnected == fiscalAttemptPageAdded) return;
            if (Program.AdminConnected)
            {
                pages.Pages.Add(fiscalAttemptPage);
                fiscalAttemptPageAdded = true;
                RefreshFiscalAttempt();
            }
            else
            {
                pages.Pages.Remove(fiscalAttemptPage);
                fiscalAttemptPageAdded = false;
                fiscalAttemptInfo.Text = fiscalAttemptRequest.Text = fiscalAttemptResponse.Text = "";
                ResetFiscalPrintPreview();
            }
        }
        private void RefreshFiscalAttempt()
        {
            if (!Program.AdminConnected || !fiscalAttemptPageAdded) return;
            ResetFiscalPrintPreview();
            var attempt = status?.Attempts.LastOrDefault();
            if (attempt == null)
            {
                fiscalAttemptInfo.Text = "Δεν υπάρχει αποθηκευμένη φορολογική προσπάθεια.";
                fiscalAttemptRequest.Text = fiscalAttemptResponse.Text = "";
                return;
            }
            fiscalAttemptInfo.Text = attempt.Family + " · " + attempt.Operation + " · " +
                StateLabel(attempt.State) + " · " + attempt.UpdatedUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss") +
                " · " + attempt.AttemptId;
            bool printAttempt = string.Equals(attempt.Family,"Print",
                StringComparison.Ordinal);
            if (printAttempt)
            {
                ShowFiscalPrintDocument(attempt);
            }
            else
            {
                fiscalAttemptRequest.Text = DecodePayload(attempt.RequestPayload,
                    attempt.PayloadFormat);
            }
            fiscalAttemptResponse.Text = attempt.SnapshotJson ?? "";
        }
        private void ResetFiscalPrintPreview()
        {
            fiscalAttemptPrintPages = null;
            fiscalAttemptPrintPreview.Visible = false;
            fiscalAttemptRequestPage.Text = "Αίτημα";
        }
        private void ShowFiscalPrintDocument(
            ASFuelControl.Invoicing.Persistence.FiscalAttemptEvidence attempt)
        {
            fiscalAttemptRequestPage.Text = "Έγγραφο";
            fiscalAttemptPrintPages = ReadPrintPages(attempt.RequestPayload);
            bool previewAvailable = fiscalAttemptPrintPages != null &&
                fiscalAttemptPrintPages.Length > 0;
            fiscalAttemptPrintPreview.Visible = previewAvailable;

            long renderedSize = previewAvailable
                ? fiscalAttemptPrintPages.Sum(page => (long)page.Length)
                : 0L;
            string pageCount = previewAvailable
                ? fiscalAttemptPrintPages.Length.ToString()
                : "Μη διαθέσιμο";
            string renderedSizeText = previewAvailable
                ? FormatByteCount(renderedSize)
                : "Μη διαθέσιμο";
            string payloadSizeText = FormatByteCount(attempt.RequestPayload.Length);
            fiscalAttemptRequest.Text =
                "Αποθηκευμένο έγγραφο εκτύπωσης\r\n\r\n" +
                "Μορφή: " + (attempt.PayloadFormat ?? "-") + "\r\n" +
                "Σελίδες: " + pageCount + "\r\n" +
                "Μέγεθος εικόνων: " + renderedSizeText + "\r\n" +
                "Μέγεθος αποθηκευμένου payload: " + payloadSizeText + "\r\n" +
                "SHA-256: " + (attempt.RequestSha256 ?? "-") +
                (previewAvailable ? string.Empty :
                    "\r\n\r\nΗ προεπισκόπηση δεν είναι διαθέσιμη για αυτό το αποθηκευμένο έγγραφο.");
        }
        private static byte[][] ReadPrintPages(byte[] payload)
        {
            if (payload == null || payload.Length == 0)
            {
                return null;
            }
            try
            {
                string serializedPages = Encoding.UTF8.GetString(payload);
                byte[][] printPages = JsonConvert.DeserializeObject<byte[][]>(
                    serializedPages);
                if (printPages == null || printPages.Length == 0 ||
                    printPages.Any(page => page == null || page.Length == 0))
                {
                    return null;
                }
                foreach (byte[] printPage in printPages)
                {
                    using (var stream = new MemoryStream(printPage,false))
                    using (var image = Image.FromStream(stream))
                    {
                        if (image.Width <= 0 || image.Height <= 0)
                        {
                            return null;
                        }
                    }
                }
                return printPages;
            }
            catch (Exception ex) when (ex is JsonException ||
                ex is ArgumentException || ex is OutOfMemoryException)
            {
                return null;
            }
        }
        private static string FormatByteCount(long bytes)
        {
            if (bytes < 1024L)
            {
                return bytes + " bytes";
            }
            double kilobytes = bytes / 1024D;
            if (kilobytes < 1024D)
            {
                return kilobytes.ToString("N1") + " KB";
            }
            double megabytes = kilobytes / 1024D;
            return megabytes.ToString("N1") + " MB";
        }
        private void FiscalAttemptPrintPreviewClick(object sender, EventArgs e)
        {
            if (fiscalAttemptPrintPages == null ||
                fiscalAttemptPrintPages.Length == 0)
            {
                return;
            }
            try
            {
                using (var preview = new FiscalPrintPreviewForm(
                    fiscalAttemptPrintPages))
                {
                    DialogResult previewResult = preview.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                string errorMessage = "Δεν ήταν δυνατή η εμφάνιση του αποθηκευμένου εγγράφου.\r\n\r\n" +
                    ex.Message;
                RadMessageBox.Show(errorMessage,"Προεπισκόπηση εκτύπωσης",
                    MessageBoxButtons.OK,RadMessageIcon.Exclamation);
            }
        }
        private static string DecodePayload(byte[] payload,string format)
        {
            if (payload == null || payload.Length == 0) return "";
            var value = Encoding.UTF8.GetString(payload);
            if (IsTextPayload(format,value)) return value;
            return "Δυαδικό αίτημα · " + payload.Length + " bytes\r\nΜορφή: " + (format ?? "-") +
                "\r\nBase64:\r\n" + Convert.ToBase64String(payload);
        }
        private static bool IsTextPayload(string format,string value)
        {
            var name = format ?? "";
            if (name.IndexOf("json",StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("xml",StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("text",StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("csv",StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (string.IsNullOrEmpty(value)) return true;
            return value.Take(Math.Min(value.Length,512)).All(c => c == '\r' || c == '\n' || c == '\t' || !char.IsControl(c));
        }
        internal void AddPage(string title, Control content)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            var page = new RadPageViewPage { Text = title };
            content.Dock = DockStyle.Fill;
            page.Controls.Add(content);
            pages.Pages.Add(page);
        }
        internal Control PrimaryAction
        {
            get
            {
                return resume;
            }
        }
        internal void AttachPrimaryAction(Control host)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }
            if (resume.Parent != null)
            {
                resume.Parent.Controls.Remove(resume);
            }
            resume.Dock = DockStyle.Fill;
            resume.Margin = new Padding(3);
            host.Controls.Add(resume);
            UpdateActionLayoutHeights();
        }
        private RadButton Button(string title,Func<Task> action,
            FlowLayoutPanel host = null)
        {
            var button = new RadButton { Text = title,Width = 175,Height = 30 };
            button.Click += async (s,e) => await action();
            button.VisibleChanged += ActionButtonVisibleChanged;
            var buttonHost = host ?? actions;
            buttonHost.Controls.Add(button);
            return button;
        }
        internal void LoadInvoice(Guid id)
        {
            if (invoiceId != id) lastActionMessage = null;
            invoiceId = id;
            try
            {
                ReadFailed = false;
                status = id == Guid.Empty ? null : SharedInvoiceRuntime.ReadStatus(id);
                UpdateFiscalAttemptPageVisibility();
                RefreshFiscalAttempt();
                var snapshot = status?.Snapshot;
                bool paymentRecoveryVisible = status != null &&
                    (status.PaymentType == (int)Common.Enumerators.PaymentTypeEnum.CreditCard ||
                     status.PaymentType == (int)Common.Enumerators.PaymentTypeEnum.IRIS);
                recover.Visible = paymentRecoveryVisible;
                recover.Enabled = false;
                retryPayment.Visible = snapshot?.Payment != null;
                retryPayment.Enabled = snapshot?.Payment != null && status.RetryPaymentAttemptId.HasValue;
                voidPayment.Visible = status?.PaymentAttemptId.HasValue == true && !status.IsRefundPayment;
                voidPayment.Enabled = status?.CanVoidPayment == true;
                recoverVoid.Visible = status?.Compensation?.Operation == "Void";
                recoverVoid.Enabled = status?.CanRecoverVoid == true;
                refundPayment.Visible = refundPayment.Enabled = status?.CanRefundPayment == true;
                recoverRefund.Visible = status?.Compensation?.Operation == "Refund";
                recoverRefund.Enabled = status?.CanRecoverRefund == true;
                diagnostics.Visible = status?.PaymentAttemptId.HasValue == true || status?.Compensation != null || snapshot?.Payment != null;
                diagnostics.Enabled = diagnostics.Visible;
                changePayment.Visible = status?.CanChangePaymentMethod == true;
                changePayment.Enabled = status?.CanChangePaymentMethod == true;
                var submissionState = status?.State("Submission");
                resume.Text = submissionState == "ReconciliationRequired"
                    ? "Ανάκτηση διαβίβασης"
                    : submissionState == "Rejected" || status?.ProviderResend?.CanResend == true
                    ? "Επανάληψη διαβίβασης" : "Διαβίβαση";
                reconcile.Visible = false;
                if (snapshot == null)
                {
                    summary.Text = Owned ? "Υπάρχει κοινό φορολογικό ιστορικό χωρίς αποθηκευμένη ροή. Απαιτείται έλεγχος ιστορικών στοιχείων." :
                        status?.PendingSummary ?? "Δεν έχει ξεκινήσει η κοινή έκδοση. Επιλέξτε ή αποθηκεύστε το παραστατικό για να εμφανιστεί η διαβίβαση.";
                    history.Text = payment.Text = printing.Text = evidence.Text = "";
                    recover.Enabled = retryPayment.Enabled = resume.Enabled = reconcile.Enabled = false;
                    UpdatePendingSubmissionAction();
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
                if (status.CanReissueAfterIssueDateMismatch)
                    summary.Text += "\r\nΗ απόρριψη αφορά παλιά ημερομηνία έκδοσης. Η επανάληψη θα ζητήσει επιβεβαίωση για σημερινή ημερομηνία και νέο αριθμό.";
                else if (status.IssueDateMismatchRejected)
                    summary.Text += "\r\nΗ απόρριψη αφορά την ημερομηνία έκδοσης, αλλά η νέα αρίθμηση εμποδίζεται: " +
                        (status.IssueDateReissueBlockReason ?? "Απαιτείται έλεγχος του φορολογικού ιστορικού.");
                if (status.ProviderResend?.CanResend == true)
                    summary.Text += "\r\nΗ έκβαση δεν επιβεβαιώθηκε. Επιλέξτε «Επανάληψη διαβίβασης» για νέα προσπάθεια μετά από ρητή επιβεβαίωση του κινδύνου διπλής έκδοσης.";
                if (status.State("Submission") == "ReconciliationRequired")
                    summary.Text += "\r\nΟ πάροχος ή η ΑΑΔΕ ανέφερε ότι το παραστατικό έχει ήδη διαβιβαστεί. Δεν επιτρέπεται νέα αποστολή. " +
                        "Επιλέξτε «Ανάκτηση διαβίβασης» για ασφαλή ανάκτηση και επαλήθευση των στοιχείων σήμανσης.";
                if (string.IsNullOrWhiteSpace(CurrentActionMessage) && snapshot.Provider != null && snapshot.Payment == null && snapshot.Signing == null && !status.Attempts.Any(a => a.Family == "Submission"))
                    summary.Text += "\r\nΈχουν αποθηκευτεί τα στοιχεία έκδοσης, αλλά δεν έχει καταγραφεί αποστολή παραστατικού.\r\nΗ σύνδεση / προετοιμασία μπορεί να μην ολοκληρώθηκε. Η «Διαβίβαση» εμφανίζει το αποτέλεσμα.";
                history.Text = string.Join("\r\n",status.Revisions.Select(HistoryLine));
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
                if (status.State("Signing") == "Unknown")
                {
                    var signingDiagnostic = SigningDiagnostic();
                    summary.Text += (signingDiagnostic ?? "").StartsWith(
                        "Automatic signing retries were exhausted",StringComparison.Ordinal)
                        ? "\r\nΗ αυτόματη επανάληψη σήμανσης παραμένει ενεργή ανά 5 λεπτά με το ίδιο Invoice UID."
                        : "\r\nΕλέγξτε την έκβαση στον μηχανισμό. Δεν επιτρέπεται νέα σήμανση.";
                    if (!string.IsNullOrWhiteSpace(signingDiagnostic)) summary.Text += "\r\n" + signingDiagnostic;
                }
                evidence.Text = string.Join("\r\n",status.Attempts.Select(a => a.Family + " · " + a.AttemptId + "\r\nSHA-256: " + a.RequestSha256)) +
                    "\r\n" + Details("Submission") + "\r\n" + Details("Signing");
                if (snapshot.Provider != null) evidence.Text += "\r\nΑποθηκευμένος λογαριασμός / συνδρομή: " + snapshot.Provider.Account + "\r\nΠροφίλ διαπιστευτηρίων: " + snapshot.Provider.CredentialProfile;
                if (configured != null) evidence.Text += "\r\nΠροφίλ από τις τρέχουσες ρυθμίσεις: " + configured.CredentialProfile;
                recover.Enabled = paymentRecoveryVisible && status.Attempts.Any() &&
                    !status.Attempts.Any(a => a.Family == "Print" && a.Operation != "PrintTF2");
                resume.Enabled = status.State("Submission") == "ReconciliationRequired" ||
                    status.ProviderResend?.CanResend == true ||
                    !status.Attempts.Any(a => a.Family == "Print" || a.Family == "Cancellation") &&
                    !status.Attempts.GroupBy(a => a.Family).Select(g => g.Last()).Any(a => a.State == "Unknown" || a.State == "Signing" || a.State == "Sending" || a.State == "Queued" || a.State == "QueueFailed" || a.State == "ReconciliationRequired");
                bool printable = snapshot.LocalOnly || status.Attempts.Any(a => a.Operation == "PrintTF2") || (snapshot.Provider != null ? status.State("Submission") == "Accepted" || status.State("Submission") == "Queued" : status.State("Signing") == "Signed");
                if (printable || status.RetryPaymentAttemptId.HasValue || !string.IsNullOrWhiteSpace(status.ConnectionProblem)) resume.Enabled = false;
                reconcile.Visible = reconcile.Enabled = status.State("Print") == "Unknown" ||
                    status.State("Print") == "Dispatching";
            }
            catch (Exception ex)
            {
                ReadFailed = true;
                status = null; summary.Text = "Δεν ήταν δυνατός ο έλεγχος: " + ex.Message;
                RefreshFiscalAttempt();
                recover.Visible = false;
                reconcile.Visible = false;
                recover.Enabled = retryPayment.Enabled = resume.Enabled = reconcile.Enabled =
                    changePayment.Enabled = voidPayment.Enabled = recoverVoid.Enabled = refundPayment.Enabled = recoverRefund.Enabled = diagnostics.Enabled = false;
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
            var allowed = new[] { "Mark","Uid","Signature","InvoiceSignature","QrCode","QrCodeUrl","TransactionId","ResponseTerminalId","Amount","AmountMinorUnits","OriginalInvoiceId","OriginalPaymentAttemptId","InvoiceUid","ReconciliationUid","ReconciliationMark","QueueState","QueueErrors" };
            if (family == "Payment") allowed = allowed.Concat(new[] { "ErrorMessage" }).ToArray();
            if (family == "Signing") allowed = allowed.Concat(new[] { "ResultDiagnostic" }).ToArray();
            var token = JToken.Parse(entry.SnapshotJson);
            if (family == "Print") return "Αιτιολογία: " + (string)token["Reason"] + "\r\nΤεκμήριο: " + (string)token["Response"];
            return string.Join("\r\n",((JContainer)token).DescendantsAndSelf().OfType<JProperty>().Where(p => allowed.Contains(p.Name) && p.Value is JValue && p.Value.Type != JTokenType.Null)
                .Select(p => p.Name == "ErrorMessage" || p.Name == "ResultDiagnostic" ? (string)p.Value : p.Name + ": " + p.Value));
        }
        private string SigningDiagnostic()
        {
            var entry = status.Attempts.LastOrDefault(a => a.Family == "Signing");
            if (entry == null) return null;
            try { return (string)JObject.Parse(entry.SnapshotJson)["ResultDiagnostic"]; }
            catch (JsonException) { return null; }
        }
        private static string HistoryLine(ASFuelControl.Invoicing.Persistence.FiscalAttemptEvidence attempt)
        {
            var line = attempt.UpdatedUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss") + " · " +
                attempt.Family + " · " + StateLabel(attempt.State) + " · " + attempt.AttemptId;
            if (attempt.Family != "Print" || string.IsNullOrWhiteSpace(attempt.Scope)) return line;
            try
            {
                var profile = JsonConvert.DeserializeObject<SharedPrinterProfile>(attempt.Scope);
                if (!string.IsNullOrWhiteSpace(profile?.PrinterName))
                    return line + " · Εκτυπωτής: " + profile.PrinterName;
            }
            catch (JsonException)
            {
                var legacyName = attempt.Scope.Trim();
                if (!legacyName.StartsWith("{",StringComparison.Ordinal) && !legacyName.StartsWith("[",StringComparison.Ordinal))
                    return line + " · Εκτυπωτής: " + legacyName;
            }
            return line + " · Εκτυπωτής: (δεν καταγράφηκε)";
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
                case "ReconciliationRequired": return "Απαιτείται ανάκτηση από πάροχο / ΑΑΔΕ";
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
            string receiverMessage = string.Empty;
            if (status?.ConfiguredProvider != null)
            {
                string previousReceiver = SharedInvoiceConfiguration.TransmissionLabel(
                    status.Snapshot?.Provider);
                string currentReceiver = SharedInvoiceConfiguration.TransmissionLabel(
                    status.ConfiguredProvider);
                receiverMessage = "Η νέα προσπάθεια θα δημιουργηθεί από τις τρέχουσες ρυθμίσεις." +
                    "\r\nΠροηγούμενος προορισμός: " + previousReceiver +
                    "\r\nΝέος προορισμός: " + currentReceiver + "\r\n\r\n";
            }
            string recoveryPrefix = string.IsNullOrWhiteSpace(recoveryMessage)
                ? string.Empty
                : recoveryMessage + "\r\n\r\n";
            string message = recoveryPrefix + receiverMessage + assessment.Warning;
            if (RadMessageBox.Show(message,"Επανάληψη διαβίβασης",MessageBoxButtons.YesNo,RadMessageIcon.Exclamation) != DialogResult.Yes) return;
            ASFuelControl.Invoicing.InvoiceWorkflowResult result = null;
            await Run(() => { result = SharedInvoiceRuntime.AuthorizeAndResumeSubmission(id); RecordResult(id,result); });
            if (!IsDisposed && invoiceId == id && result != null) ShowResult(result,"Επανάληψη διαβίβασης");
        }
        internal async Task SubmitPrintedInvoice()
        {
            bool canSubmitPrinted = status?.CanSubmitPrinted == true;
            bool replaceLegacyEvidence =
                status?.CanReplaceLegacyFiscalEvidence == true;
            if (ReadFailed || Busy || Owned ||
                (!canSubmitPrinted && !replaceLegacyEvidence))
            {
                return;
            }
            if (replaceLegacyEvidence && !Program.AdminConnected)
            {
                RadMessageBox.Show(
                    "Απαιτείται σύνδεση διαχειριστή για την ανάκτηση σήμανσης παρόχου.",
                    "Ανάκτηση σήμανσης", MessageBoxButtons.OK,
                    RadMessageIcon.Exclamation);
                return;
            }
            string question = replaceLegacyEvidence
                ? "Να διαβιβαστεί το ήδη εκτυπωμένο παραστατικό στον τρέχοντα πάροχο με τον υπάρχοντα αριθμό, σειρά και ημερομηνία;\r\n\r\n" +
                  "Δεν θα γίνει νέα εκτύπωση ή νέα πληρωμή. Η παλιά υπογραφή και το παλιό QR θα διατηρηθούν στο ελεγκτικό ιστορικό και θα αντικατασταθούν στο παραστατικό μόνο αφού ο πάροχος επιστρέψει έγκυρη υπογραφή και QR."
                : "Να διαβιβαστεί το αποθηκευμένο παραστατικό με τον υπάρχοντα αριθμό, σειρά και ημερομηνία; Δεν θα γίνει νέα εκτύπωση.";
            string title = replaceLegacyEvidence
                ? "Ανάκτηση σήμανσης παρόχου"
                : "Διαβίβαση";
            RadMessageIcon icon = replaceLegacyEvidence
                ? RadMessageIcon.Exclamation
                : RadMessageIcon.Question;
            DialogResult confirmation = RadMessageBox.Show(question, title,
                MessageBoxButtons.YesNo, icon);
            if (confirmation != DialogResult.Yes)
            {
                return;
            }
            Guid id = invoiceId;
            ASFuelControl.Invoicing.InvoiceWorkflowResult result = null;
            await Run(() =>
            {
                result = replaceLegacyEvidence
                    ? SharedInvoiceRuntime.SubmitLegacySignedInvoiceToProvider(id)
                    : SharedInvoiceRuntime.SubmitPrintedInvoice(id);
                RecordResult(id, result);
            });
            if (!IsDisposed && invoiceId == id && result != null)
            {
                ShowResult(result, title);
            }
        }
        internal async Task PrintInvoice()
        {
            if (ReadFailed || Busy || !actions.Enabled) return;
            var id = invoiceId;
            bool historicalCopy = status?.Snapshot == null && status?.CanPrintHistorical == true;
            var previous = status?.Attempts.LastOrDefault(a => a.Family == "Print");
            bool reprint = historicalCopy || previous != null &&
                (previous.State == "HandedOff" || previous.State == "Confirmed");
            string selectedPrinter;
            string prompt = reprint
                ? "Θέλετε να εκτυπώσετε ξανά το παραστατικό;"
                : "Θέλετε να εκτυπώσετε το παραστατικό;";
            try
            {
                if (!InvoicePrintConfirmationDialog.Confirm(this,prompt,
                    SharedInvoiceRuntime.CurrentPrinterName(id),out selectedPrinter))
                    return;
            }
            catch (Exception ex)
            {
                RadMessageBox.Show(ex.Message,"Εκτύπωση Παραστατικού",
                    MessageBoxButtons.OK,RadMessageIcon.Error);
                return;
            }
            if (historicalCopy)
            {
                await Run(() => {
                    SharedInvoiceRuntime.PrintHistoricalCopy(id,selectedPrinter);
                    lastActionInvoiceId = id;
                    lastActionMessage = "Εκτυπώθηκε αντίγραφο ιστορικού παραστατικού χωρίς νέα σήμανση ή διαβίβαση.";
                });
                return;
            }
            string reason = reprint ? SharedInvoiceRuntime.OperatorRequestedCopyReason : null;
            // First issuance and recovery use exactly the PrintAgent workflow. Only
            // an explicitly requested copy supplies an audit reason. Printer choice
            // above is a one-off route override and does not alter saved settings.
            ASFuelControl.Invoicing.InvoiceWorkflowResult result = null;
            await Run(() => { result = SharedInvoiceRuntime.Process(id,true,reason,selectedPrinter); RecordResult(id,result); });
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
        private bool SelectPaymentMethod(int current, out int selected)
        {
            using (var dialog = new RadForm { Text = "Αλλαγή τρόπου πληρωμής",
                ClientSize = new Size(460,145),StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,MaximizeBox = false,MinimizeBox = false })
            {
                var label = new RadLabel { Text = "Νέος τρόπος πληρωμής",Dock = DockStyle.Top,Height = 32 };
                var list = new RadDropDownList { Dock = DockStyle.Top,Height = 32,
                    DropDownStyle = RadDropDownStyle.DropDownList,
                    DisplayMember = "Description",ValueMember = "Value" };
                list.DataSource = Common.Enumerators.EnumToList.EnumList<Common.Enumerators.PaymentTypeEnum>();
                list.SelectedValue = current;
                var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom,Height = 44,
                    FlowDirection = FlowDirection.RightToLeft,Padding = new Padding(6) };
                var accept = new RadButton { Text = "Ενημέρωση",Width = 110,Height = 30,DialogResult = DialogResult.OK };
                var cancel = new RadButton { Text = "Άκυρο",Width = 90,Height = 30,DialogResult = DialogResult.Cancel };
                buttons.Controls.Add(accept); buttons.Controls.Add(cancel);
                dialog.AcceptButton = accept; dialog.CancelButton = cancel;
                dialog.Controls.Add(list); dialog.Controls.Add(label); dialog.Controls.Add(buttons);
                bool ok = dialog.ShowDialog(this) == DialogResult.OK && list.SelectedValue != null;
                selected = ok ? Convert.ToInt32(list.SelectedValue) : current;
                return ok;
            }
        }
    }
}
