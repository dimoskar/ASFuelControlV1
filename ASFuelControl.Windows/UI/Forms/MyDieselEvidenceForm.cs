using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ASFuelControl.Invoicing.Persistence;
using ASFuelControl.Windows.Threads;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Telerik.WinControls;
using Telerik.WinControls.UI;

namespace ASFuelControl.Windows.UI.Forms
{
    internal sealed class MyDieselEvidenceForm : RadForm
    {
        private readonly Guid invoiceId;
        private readonly RadPageView pages = new RadPageView { Dock = DockStyle.Fill };
        private readonly DataGridView attemptGrid = Grid();
        private readonly DataGridView httpGrid = Grid();
        private readonly RichTextBox attemptRequest = JsonBox();
        private readonly RichTextBox attemptResponse = JsonBox();
        private readonly RichTextBox httpRequest = JsonBox();
        private readonly RichTextBox httpResponse = JsonBox();
        private readonly TableLayoutPanel summary = new TableLayoutPanel { Dock = DockStyle.Fill,ColumnCount = 2,AutoScroll = true,Padding = new Padding(18) };
        private SharedMyDieselStatus status;

        internal MyDieselEvidenceForm(Guid invoiceId)
        {
            this.invoiceId = invoiceId;
            Text = "Τεκμήρια myDiesel";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(1100,650);
            ClientSize = new Size(1180,720);
            ShowIcon = false;
            summary.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,210));
            summary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            Controls.Add(pages);
            Controls.Add(BuildFooter());
            BuildSummaryPage();
            BuildAttemptPage();
            BuildHttpPage();
            Shown += (s,e) => Reload();
        }

        private Control BuildFooter()
        {
            var footer = new FlowLayoutPanel { Dock = DockStyle.Bottom,Height = 48,FlowDirection = FlowDirection.RightToLeft,Padding = new Padding(8),WrapContents = false };
            var close = ActionButton("Κλείσιμο",(s,e) => Close());
            var export = ActionButton("Εξαγωγή τεκμηρίων υποστήριξης",Export);
            var copy = ActionButton("Αντιγραφή αναγνωριστικών",CopyIdentifiers);
            footer.Controls.Add(close); footer.Controls.Add(export); footer.Controls.Add(copy);
            return footer;
        }

        private void BuildSummaryPage()
        {
            var page = new RadPageViewPage { Text = "Σύνοψη" };
            page.Controls.Add(summary); pages.Pages.Add(page);
        }

        private void BuildAttemptPage()
        {
            var page = new RadPageViewPage { Text = "Ιστορικό myDiesel" };
            var split = new SplitContainer { Dock = DockStyle.Fill,Orientation = Orientation.Horizontal,SplitterDistance = 290,Panel1MinSize = 180,Panel2MinSize = 180 };
            split.Panel1.Controls.Add(attemptGrid);
            split.Panel2.Controls.Add(PayloadPair("Ακριβές αίτημα",attemptRequest,"Απάντηση / διαγνωστικό",attemptResponse));
            page.Controls.Add(split); pages.Pages.Add(page);
            attemptGrid.SelectionChanged += (s,e) => ShowAttempt();
        }

        private void BuildHttpPage()
        {
            var page = new RadPageViewPage { Text = "HTTP τεκμήρια" };
            var split = new SplitContainer { Dock = DockStyle.Fill,Orientation = Orientation.Horizontal,SplitterDistance = 290,Panel1MinSize = 180,Panel2MinSize = 180 };
            split.Panel1.Controls.Add(httpGrid);
            split.Panel2.Controls.Add(PayloadPair("Αίτημα χωρίς μυστικά headers",httpRequest,"Απάντηση",httpResponse));
            page.Controls.Add(split); pages.Pages.Add(page);
            httpGrid.SelectionChanged += (s,e) => ShowHttp();
        }

        private static Control PayloadPair(string leftTitle, Control left, string rightTitle, Control right)
        {
            var split = new SplitContainer { Dock = DockStyle.Fill,SplitterDistance = 555,Panel1MinSize = 300,Panel2MinSize = 300 };
            split.Panel1.Controls.Add(left); split.Panel1.Controls.Add(Header(leftTitle));
            split.Panel2.Controls.Add(right); split.Panel2.Controls.Add(Header(rightTitle));
            return split;
        }

        private void Reload()
        {
            try
            {
                status = SharedMyDieselRuntime.ReadInvoice(invoiceId,true);
                if (status.CouponCase == null) { Text = "Τεκμήρια myDiesel — δεν υπάρχει υπόθεση"; return; }
                var value = status.CouponCase;
                Text = "Τεκμήρια myDiesel — " + value.CouponCode;
                AddSummary("Κωδικός κουπονιού",value.CouponCode);
                AddSummary("ΑΦΜ δικαιούχου",value.BeneficiaryVat);
                AddSummary("Εταιρεία / περιβάλλον",value.CompanyVat + " · " + EnvironmentLabel(value.Environment));
                AddSummary("Πρατήριο",(value.CurrentBranchNum ?? value.ReservedBranchNum).ToString());
                AddSummary("Τοπικό στάδιο",StageLabel(value.LocalStage.ToString()));
                AddSummary("Κατάσταση ΑΑΔΕ",string.IsNullOrWhiteSpace(value.RemoteStatus) ? "—" : value.RemoteStatus);
                AddSummary("Λίτρα / καύσιμο",(value.IssuedLitres.HasValue ? value.IssuedLitres.Value.ToString("0.00") + " L" : "—") +
                    (string.IsNullOrWhiteSpace(value.FuelType) ? "" : " · " + value.FuelType + " (" + value.FuelCode + ")"));
                AddSummary("Ισχύει έως",Local(value.ValidUntilUtc));
                AddSummary("MARK",string.IsNullOrWhiteSpace(value.InvoiceMark) ? "—" : value.InvoiceMark);
                AddSummary("Τελευταίος έλεγχος",Local(value.LastCheckedUtc));
                AddSummary("Coupon case ID",value.CouponCaseId.ToString());
                AddSummary("Invoice ID",value.InvoiceId.ToString());
                AddSummary("Προφίλ διαπιστευτηρίων",value.CredentialProfileId);
                if (value.LowBalance) AddSummary("Προειδοποίηση ΑΑΔΕ",value.LowBalanceMessageEl);

                attemptGrid.DataSource = status.Attempts.Select(a => new AttemptRow(a)).ToArray();
                httpGrid.DataSource = status.HttpEvidence.Select(a => new HttpRow(a)).ToArray();
                HideInternal(attemptGrid,"Value"); HideInternal(httpGrid,"Value");
                if (attemptGrid.Rows.Count > 0) attemptGrid.Rows[attemptGrid.Rows.Count - 1].Selected = true;
                if (httpGrid.Rows.Count > 0) httpGrid.Rows[httpGrid.Rows.Count - 1].Selected = true;
            }
            catch (Exception ex)
            {
                RadMessageBox.Show(ex.Message,"Τεκμήρια myDiesel",MessageBoxButtons.OK,RadMessageIcon.Error);
            }
        }

        private void ShowAttempt()
        {
            var row = attemptGrid.CurrentRow == null ? null : attemptGrid.CurrentRow.DataBoundItem as AttemptRow;
            var value = row == null ? null : row.Value;
            attemptRequest.Text = Pretty(value == null ? null : value.RequestJson);
            attemptResponse.Text = value == null ? "" : Pretty(value.ResponseBody) +
                (string.IsNullOrWhiteSpace(value.ErrorCode) ? "" : "\r\n\r\nError code: " + value.ErrorCode) +
                (string.IsNullOrWhiteSpace(value.Diagnostic) ? "" : "\r\nDiagnostic: " + value.Diagnostic);
        }

        private void ShowHttp()
        {
            var row = httpGrid.CurrentRow == null ? null : httpGrid.CurrentRow.DataBoundItem as HttpRow;
            var value = row == null ? null : row.Value;
            httpRequest.Text = value == null || value.Request == null ? "" : Pretty(value.Request.Body);
            httpResponse.Text = value == null || value.Response == null ? "Δεν έχει καταγραφεί απάντηση." :
                Pretty(value.Response.Body) + (string.IsNullOrWhiteSpace(value.Response.CommunicationError) ? "" : "\r\n\r\n" + value.Response.CommunicationError);
        }

        private void CopyIdentifiers(object sender, EventArgs e)
        {
            if (status == null || status.CouponCase == null) return;
            Clipboard.SetText("CouponCaseId: " + status.CouponCase.CouponCaseId + Environment.NewLine + "InvoiceId: " + status.CouponCase.InvoiceId);
        }

        private void Export(object sender, EventArgs e)
        {
            if (status == null || status.CouponCase == null) return;
            using (var dialog = new SaveFileDialog { Filter = "JSON (*.json)|*.json",FileName = "mydiesel-evidence-" + status.CouponCase.CouponCode + ".json" })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                var payload = new { CouponCase = status.CouponCase,Attempts = status.Attempts,HttpEvidence = status.HttpEvidence };
                File.WriteAllText(dialog.FileName,JsonConvert.SerializeObject(payload,Formatting.Indented));
            }
        }

        private void AddSummary(string label, string value)
        {
            int row = summary.RowCount++;
            summary.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            summary.Controls.Add(new RadLabel { Text = label,AutoSize = true,Margin = new Padding(3,7,3,7) },0,row);
            summary.Controls.Add(new RadLabel { Text = string.IsNullOrWhiteSpace(value) ? "—" : value,AutoSize = true,MaximumSize = new Size(800,0),Margin = new Padding(3,7,3,7) },1,row);
        }

        private static string Pretty(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";
            try { return JToken.Parse(value).ToString(Formatting.Indented); }
            catch { return value; }
        }

        private static DataGridView Grid()
        {
            return new DataGridView { Dock = DockStyle.Fill,ReadOnly = true,AutoGenerateColumns = true,AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,AllowUserToDeleteRows = false,MultiSelect = false,SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,BackgroundColor = SystemColors.Window,BorderStyle = BorderStyle.FixedSingle };
        }

        private static RichTextBox JsonBox() { return new RichTextBox { Dock = DockStyle.Fill,ReadOnly = true,Font = new Font("Consolas",9),BackColor = SystemColors.ControlLightLight,WordWrap = false }; }
        private static RadLabel Header(string text) { return new RadLabel { Text = text,Dock = DockStyle.Top,Height = 28,Padding = new Padding(6) }; }
        private static RadButton ActionButton(string text, EventHandler action) { var button = new RadButton { Text = text,Width = text.Length > 15 ? 220 : 130,Height = 30,Margin = new Padding(6,0,0,0) }; button.Click += action; return button; }
        private static void HideInternal(DataGridView grid, string name) { if (grid.Columns.Contains(name)) grid.Columns[name].Visible = false; }
        private static string Local(DateTime? utc) { return utc.HasValue ? utc.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss") : "—"; }
        private static string EnvironmentLabel(Exedron.Invoicing.Core.FiscalEnvironment value) { return value == Exedron.Invoicing.Core.FiscalEnvironment.Production ? "Παραγωγή" : "Δοκιμαστικό"; }
        internal static string StageLabel(string value)
        {
            switch (value)
            {
                case "Captured": return "Καταχωρίστηκε — δεν εστάλη";
                case "ReserveUnknown": return "Άγνωστη έκβαση δέσμευσης";
                case "Reserved": return "Δεσμευμένο";
                case "Released": return "Αποδεσμευμένο";
                case "InvoiceAccepted": return "Παραστατικό αποδεκτό — αναμονή εξαργύρωσης";
                case "RedeemUnknown": return "Άγνωστη έκβαση εξαργύρωσης";
                case "Redeemed": return "Εξαργυρωμένο";
                case "VerificationPending": return "Αναμονή επαλήθευσης ΑΑΔΕ";
                case "Final": return "Ολοκληρωμένο";
                case "InterventionRequired": return "Χρειάζεται παρέμβαση";
                default: return value;
            }
        }

        private sealed class AttemptRow
        {
            internal AttemptRow(MyDieselAttemptRecord value) { Value = value; }
            [Browsable(false)] public MyDieselAttemptRecord Value { get; }
            [DisplayName("Ώρα")] public string Time => Value.UpdatedUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss");
            [DisplayName("Ενέργεια")] public string Operation => Value.Operation.ToString();
            [DisplayName("Κατάσταση")] public string State => Value.State.ToString();
            [DisplayName("Αναθεώρηση")] public long Revision => Value.Revision;
            [DisplayName("HTTP")] public string Http => Value.HttpStatus.HasValue ? Value.HttpStatus.Value.ToString() : "—";
            [DisplayName("Attempt ID")] public string AttemptId => Value.AttemptId.ToString();
            [DisplayName("SHA-256 αιτήματος")] public string Hash => Value.RequestSha256;
        }

        private sealed class HttpRow
        {
            internal HttpRow(MyDieselHttpEvidenceRecord value) { Value = value; }
            [Browsable(false)] public MyDieselHttpEvidenceRecord Value { get; }
            [DisplayName("Ώρα")] public string Time => Value.Request.StartedUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss");
            [DisplayName("Μέθοδος")] public string Method => Value.Request.Method;
            [DisplayName("Endpoint")] public string Endpoint => Value.Request.Endpoint;
            [DisplayName("HTTP")] public string Http => Value.Response == null || !Value.Response.HttpStatus.HasValue ? "—" : Value.Response.HttpStatus.Value.ToString();
            [DisplayName("Exchange ID")] public string ExchangeId => Value.Request.ExchangeId.ToString();
        }
    }
}
