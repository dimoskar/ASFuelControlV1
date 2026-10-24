using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using ASFuelControl.Invoicing;
using ASFuelControl.Invoicing.Persistence;
using ASFuelControl.Windows.Threads;
using ASFuelControl.Windows.UI.Forms;
using Exedron.Invoicing.Adapters;
using Exedron.Invoicing.Core;
using Telerik.WinControls;
using Telerik.WinControls.UI;

namespace ASFuelControl.Windows.UI.Controls
{
    internal sealed class MyDieselCouponPanel : UserControl
    {
        private readonly Func<bool> ensureSaved;
        private readonly Func<string> traderVat;
        private readonly RadLabel state = new RadLabel { AutoSize = false,Dock = DockStyle.Fill,TextAlignment = ContentAlignment.MiddleLeft };
        private readonly RadLabel details = new RadLabel { AutoSize = false,Dock = DockStyle.Fill,TextAlignment = ContentAlignment.MiddleLeft };
        private readonly RadLabel warning = new RadLabel { AutoSize = false,Dock = DockStyle.Fill,ForeColor = Color.FromArgb(138,80,0),TextAlignment = ContentAlignment.MiddleLeft };
        private readonly FlowLayoutPanel actions = new FlowLayoutPanel { Dock = DockStyle.Fill,FlowDirection = FlowDirection.LeftToRight,WrapContents = false,AutoScroll = true,Padding = new Padding(0,4,0,0) };
        private readonly RadButton capture, reserve, release, refresh, correctMark, changeBranch, evidence;
        private Guid invoiceId;
        private SharedMyDieselStatus status;
        internal bool Busy { get; private set; }
        internal bool HasBlockingCase => status?.CouponCase?.BlocksInvoice == true;
        internal event EventHandler StateChanged;

        internal MyDieselCouponPanel(Func<bool> ensureSaved, Func<string> traderVat)
        {
            this.ensureSaved = ensureSaved ?? throw new ArgumentNullException(nameof(ensureSaved));
            this.traderVat = traderVat ?? throw new ArgumentNullException(nameof(traderVat));
            Height = 132; MinimumSize = new Size(760,132); BackColor = SystemColors.ControlLightLight;
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill,ColumnCount = 2,RowCount = 4,Padding = new Padding(9,6,9,5) };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,130)); layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute,24)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute,25));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute,27)); layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
            layout.Controls.Add(new RadLabel { Text = "myDiesel",Font = new Font(Font,FontStyle.Bold),Dock = DockStyle.Fill },0,0);
            layout.Controls.Add(state,1,0); layout.Controls.Add(new RadLabel { Text = "Στοιχεία",Dock = DockStyle.Fill },0,1); layout.Controls.Add(details,1,1);
            layout.Controls.Add(new RadLabel { Text = "Προειδοποίηση",Dock = DockStyle.Fill },0,2); layout.Controls.Add(warning,1,2);
            layout.Controls.Add(actions,0,3); layout.SetColumnSpan(actions,2); Controls.Add(layout);

            capture = Button("Σάρωση / καταχώριση",CaptureAndReserve);
            reserve = Button("Έλεγχος και δέσμευση",Reserve);
            release = Button("Αποδέσμευση",Release);
            refresh = Button("Ανανέωση κατάστασης",Refresh);
            correctMark = Button("Διόρθωση MARK",CorrectMark);
            changeBranch = Button("Αλλαγή πρατηρίου",ChangeBranch);
            evidence = Button("Πλήρη τεκμήρια…",ShowEvidence);
            Render();
        }

        internal void LoadInvoice(Guid id)
        {
            invoiceId = id;
            try { status = SharedMyDieselRuntime.ReadInvoice(id); }
            catch (Exception ex)
            {
                status = null; state.Text = "Δεν ήταν δυνατός ο έλεγχος myDiesel"; details.Text = ex.Message; warning.Text = "";
                Enable(false); StateChanged?.Invoke(this,EventArgs.Empty); return;
            }
            Render(); StateChanged?.Invoke(this,EventArgs.Empty);
        }

        private void Render()
        {
            var value = status?.CouponCase;
            if (value == null)
            {
                state.Text = "Δεν έχει συνδεθεί κουπόνι";
                details.Text = "Σαρώστε το QR ή καταχωρίστε κωδικό και ΑΦΜ δικαιούχου.";
                warning.Text = "";
            }
            else
            {
                state.Text = MyDieselEvidenceForm.StageLabel(value.LocalStage.ToString()) +
                    (string.IsNullOrWhiteSpace(value.RemoteStatus) ? "" : " · ΑΑΔΕ: " + value.RemoteStatus);
                details.Text = value.CouponCode + " · ΑΦΜ " + value.BeneficiaryVat + " · πρατήριο " +
                    (value.CurrentBranchNum ?? value.ReservedBranchNum) +
                    (value.IssuedLitres.HasValue ? " · " + value.IssuedLitres.Value.ToString("0.00") + " L" : "") +
                    (string.IsNullOrWhiteSpace(value.InvoiceMark) ? "" : " · MARK " + value.InvoiceMark);
                warning.Text = value.LowBalance ? value.LowBalanceMessageEl : value.LocalStage == MyDieselCaseStage.ReserveUnknown || value.LocalStage == MyDieselCaseStage.RedeemUnknown
                    ? "Η έκβαση είναι άγνωστη. Μην επαναλάβετε τυφλά την ενέργεια." : "";
            }
            bool active = !Busy && invoiceId != Guid.Empty;
            capture.Visible = value == null; capture.Enabled = active && NewReservationsEnabled();
            reserve.Visible = value != null && (value.LocalStage == MyDieselCaseStage.Captured || value.LocalStage == MyDieselCaseStage.ReserveUnknown);
            reserve.Enabled = active && reserve.Visible;
            release.Visible = value != null && value.LocalStage == MyDieselCaseStage.Reserved;
            release.Enabled = active && release.Visible;
            refresh.Visible = value != null && (value.LocalStage == MyDieselCaseStage.RedeemUnknown || value.LocalStage == MyDieselCaseStage.Redeemed ||
                value.LocalStage == MyDieselCaseStage.VerificationPending || value.LocalStage == MyDieselCaseStage.InterventionRequired);
            refresh.Enabled = active && refresh.Visible;
            var remote = value == null ? MyDieselCouponStatus.UnknownValue : MyDieselCouponStates.Parse(value.RemoteStatus);
            bool admin = Program.AdminConnected;
            correctMark.Visible = value != null && MyDieselCouponStates.CanRedeem(remote) && remote != MyDieselCouponStatus.Reserved;
            correctMark.Enabled = active && admin && correctMark.Visible;
            changeBranch.Visible = value != null && MyDieselCouponStates.CanChangeBranch(remote);
            changeBranch.Enabled = active && admin && changeBranch.Visible;
            evidence.Visible = value != null; evidence.Enabled = active && evidence.Visible;
        }

        private bool NewReservationsEnabled()
        {
            try { return SharedInvoicingSettings.LoadRuntime().MyDiesel?.Enabled == true; }
            catch { return false; }
        }

        private async void CaptureAndReserve(object sender, EventArgs e)
        {
            if (Busy) return;
            CaptureInput input;
            using (var dialog = new CaptureDialog())
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                input = dialog.Value;
            }
            if (!ensureSaved()) return;
            string invoiceVat;
            try { invoiceVat = MyDieselCouponIdentity.NormalizeVat(traderVat(),"Invoice trader VAT"); }
            catch (Exception ex) { RadMessageBox.Show(ex.Message,"myDiesel",MessageBoxButtons.OK,RadMessageIcon.Exclamation); return; }
            if (invoiceVat != input.Identity.BeneficiaryVat)
            {
                RadMessageBox.Show("Το ΑΦΜ του πελάτη στο παραστατικό δεν συμφωνεί με το ΑΦΜ δικαιούχου του κουπονιού.","myDiesel",MessageBoxButtons.OK,RadMessageIcon.Exclamation); return;
            }
            await Run(() => {
                var configuration = SharedInvoicingSettings.LoadRuntime();
                var service = SharedMyDieselRuntime.CreateService(configuration);
                var scope = SharedMyDieselRuntime.NewReservationScope(configuration);
                var coupon = input.Qr == null ? service.Capture(invoiceId,input.Identity,scope) : service.Capture(invoiceId,input.Qr,scope);
                service.Reserve(coupon.CouponCaseId);
            });
        }

        private async void Reserve(object sender, EventArgs e)
        {
            var value = status?.CouponCase; if (value == null) return;
            await Run(() => SharedMyDieselRuntime.CreateService().Reserve(value.CouponCaseId));
        }

        private async void Release(object sender, EventArgs e)
        {
            var value = status?.CouponCase; if (value == null) return;
            if (RadMessageBox.Show("Να αποδεσμευτεί το κουπόνι από το παραστατικό;","myDiesel",MessageBoxButtons.YesNo,RadMessageIcon.Question) != DialogResult.Yes) return;
            await Run(() => SharedMyDieselRuntime.CreateService().Release(value.CouponCaseId));
        }

        private async void Refresh(object sender, EventArgs e)
        {
            var value = status?.CouponCase; if (value == null) return;
            bool reconcile = value.LocalStage == MyDieselCaseStage.RedeemUnknown || value.LocalStage == MyDieselCaseStage.InterventionRequired;
            if (reconcile && RadMessageBox.Show("Θα γίνει μόνο ασφαλής έλεγχος της τρέχουσας κατάστασης στην ΑΑΔΕ. Συνέχεια;","Συμφιλίωση myDiesel",MessageBoxButtons.YesNo,RadMessageIcon.Question) != DialogResult.Yes) return;
            await Run(() => SharedMyDieselRuntime.CreateService().RefreshStatus(value.CouponCaseId,reconcile));
        }

        private async void CorrectMark(object sender, EventArgs e)
        {
            var value = status?.CouponCase; if (value == null || !Program.AdminConnected) return;
            string mark, reason;
            if (!CorrectionDialog("Διόρθωση MARK","Νέο MARK (15 ψηφία)",value.InvoiceMark,out mark,out reason)) return;
            await Run(() => SharedMyDieselRuntime.CreateService().CorrectMark(value.CouponCaseId,value.InvoiceId,mark,reason));
        }

        private async void ChangeBranch(object sender, EventArgs e)
        {
            var value = status?.CouponCase; if (value == null || !Program.AdminConnected) return;
            string branch, reason;
            if (!CorrectionDialog("Αλλαγή πρατηρίου","Νέος αριθμός υποκαταστήματος",(value.CurrentBranchNum ?? value.ReservedBranchNum).ToString(),out branch,out reason)) return;
            int number; if (!int.TryParse(branch,out number) || number < 0) { RadMessageBox.Show("Ο αριθμός υποκαταστήματος δεν είναι έγκυρος."); return; }
            await Run(() => SharedMyDieselRuntime.CreateService().ChangeBranch(value.CouponCaseId,number,reason));
        }

        private void ShowEvidence(object sender, EventArgs e)
        {
            if (invoiceId == Guid.Empty) return;
            using (var form = new MyDieselEvidenceForm(invoiceId)) form.ShowDialog(this);
        }

        private async Task Run(Action action)
        {
            if (Busy) return; Busy = true; Render(); StateChanged?.Invoke(this,EventArgs.Empty);
            try { await Task.Run(action); }
            catch (Exception ex) { if (!IsDisposed) RadMessageBox.Show(ex.Message,"myDiesel",MessageBoxButtons.OK,RadMessageIcon.Error); }
            finally { Busy = false; if (!IsDisposed) LoadInvoice(invoiceId); }
        }

        private RadButton Button(string text, EventHandler click)
        {
            var value = new RadButton { Text = text,Height = 30,AutoSize = true,MinimumSize = new Size(128,30),Margin = new Padding(0,0,7,0) };
            value.Click += click; actions.Controls.Add(value); return value;
        }

        private void Enable(bool enabled)
        {
            foreach (Control control in actions.Controls) control.Enabled = enabled;
        }

        private static bool CorrectionDialog(string title, string valueLabel, string initial, out string value, out string reason)
        {
            using (var dialog = new RadForm { Text = title,StartPosition = FormStartPosition.CenterParent,ClientSize = new Size(580,260),MinimumSize = new Size(580,260),ShowIcon = false })
            {
                var table = new TableLayoutPanel { Dock = DockStyle.Fill,ColumnCount = 1,RowCount = 5,Padding = new Padding(14) };
                table.RowStyles.Add(new RowStyle(SizeType.Absolute,28)); table.RowStyles.Add(new RowStyle(SizeType.Absolute,34));
                table.RowStyles.Add(new RowStyle(SizeType.Absolute,28)); table.RowStyles.Add(new RowStyle(SizeType.Percent,100)); table.RowStyles.Add(new RowStyle(SizeType.Absolute,38));
                var input = new RadTextBox { Text = initial ?? "",Dock = DockStyle.Fill };
                var note = new RadTextBox { Multiline = true,Dock = DockStyle.Fill };
                var accept = new RadButton { Text = "Καταχώριση",Dock = DockStyle.Right,Width = 150 };
                accept.Click += (s,e) => { if (!string.IsNullOrWhiteSpace(input.Text) && !string.IsNullOrWhiteSpace(note.Text)) dialog.DialogResult = DialogResult.OK; };
                table.Controls.Add(new RadLabel { Text = valueLabel,Dock = DockStyle.Fill },0,0); table.Controls.Add(input,0,1);
                table.Controls.Add(new RadLabel { Text = "Αιτιολογία / τεκμήριο (υποχρεωτικό)",Dock = DockStyle.Fill },0,2); table.Controls.Add(note,0,3); table.Controls.Add(accept,0,4);
                dialog.Controls.Add(table); bool ok = dialog.ShowDialog() == DialogResult.OK;
                value = input.Text.Trim(); reason = note.Text.Trim(); return ok;
            }
        }

        private sealed class CaptureInput
        {
            internal MyDieselCouponIdentity Identity { get; set; }
            internal MyDieselQrCoupon Qr { get; set; }
        }

        private sealed class CaptureDialog : RadForm
        {
            private readonly RadTextBox qr = new RadTextBox { Multiline = true,Dock = DockStyle.Fill };
            private readonly RadTextBox code = new RadTextBox { Dock = DockStyle.Fill };
            private readonly RadTextBox vat = new RadTextBox { Dock = DockStyle.Fill };
            internal CaptureInput Value { get; private set; }
            internal CaptureDialog()
            {
                Text = "Σάρωση κουπονιού myDiesel"; StartPosition = FormStartPosition.CenterParent; ShowIcon = false;
                ClientSize = new Size(720,390); MinimumSize = new Size(680,360);
                var table = new TableLayoutPanel { Dock = DockStyle.Fill,ColumnCount = 2,RowCount = 5,Padding = new Padding(16) };
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,190)); table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
                table.RowStyles.Add(new RowStyle(SizeType.Absolute,32)); table.RowStyles.Add(new RowStyle(SizeType.Percent,100));
                table.RowStyles.Add(new RowStyle(SizeType.Absolute,36)); table.RowStyles.Add(new RowStyle(SizeType.Absolute,36)); table.RowStyles.Add(new RowStyle(SizeType.Absolute,45));
                table.Controls.Add(new RadLabel { Text = "QR JSON (σάρωση ή επικόλληση)",Dock = DockStyle.Fill },0,0); table.SetColumnSpan(table.GetControlFromPosition(0,0),2);
                table.Controls.Add(qr,0,1); table.SetColumnSpan(qr,2);
                table.Controls.Add(new RadLabel { Text = "ή χειροκίνητος κωδικός",Dock = DockStyle.Fill },0,2); table.Controls.Add(code,1,2);
                table.Controls.Add(new RadLabel { Text = "ΑΦΜ δικαιούχου (9 ψηφία)",Dock = DockStyle.Fill },0,3); table.Controls.Add(vat,1,3);
                var accept = new RadButton { Text = "Έλεγχος και δέσμευση",Dock = DockStyle.Right,Width = 190,Height = 34 };
                accept.Click += Accept; table.Controls.Add(accept,1,4); Controls.Add(table);
            }
            private void Accept(object sender, EventArgs e)
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(qr.Text))
                    {
                        var parsed = MyDieselQrParser.Parse(qr.Text.Trim()); Value = new CaptureInput { Identity = parsed.Identity,Qr = parsed };
                    }
                    else Value = new CaptureInput { Identity = new MyDieselCouponIdentity(code.Text,vat.Text) };
                    DialogResult = DialogResult.OK;
                }
                catch (Exception ex) { RadMessageBox.Show(ex.Message,"Κουπόνι myDiesel",MessageBoxButtons.OK,RadMessageIcon.Exclamation); }
            }
        }
    }
}
