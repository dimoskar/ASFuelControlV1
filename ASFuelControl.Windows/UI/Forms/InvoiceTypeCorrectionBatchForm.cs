using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ASFuelControl.Windows.Threads;
using Telerik.WinControls;
using Telerik.WinControls.UI;

namespace ASFuelControl.Windows.UI.Forms
{
    internal sealed class InvoiceTypeCorrectionBatchForm : RadForm
    {
        private readonly RadDateTimePicker from = new RadDateTimePicker(), to = new RadDateTimePicker();
        private readonly RadDropDownList retailCredit = new RadDropDownList();
        private readonly RadGridView grid = new RadGridView();
        private readonly Label summary = new Label(), detail = new Label();
        private readonly RadButton scan = new RadButton(), select = new RadButton(), clear = new RadButton(),
            start = new RadButton(), resume = new RadButton(), pause = new RadButton(), export = new RadButton(), print = new RadButton();
        private BindingList<InvoiceTypeCorrectionRow> rows = new BindingList<InvoiceTypeCorrectionRow>();
        private readonly InvoiceTypeCorrectionCreditType[] creditTypes;
        private volatile bool stopRequested;
        private bool busy, scanned;

        internal InvoiceTypeCorrectionBatchForm()
            : this(DateTime.Today.AddDays(-1), DateTime.Today, SharedInvoiceRuntime.InvoiceTypeCorrectionCreditTypes()) { }

        internal InvoiceTypeCorrectionBatchForm(DateTime startDate, DateTime endDate, InvoiceTypeCorrectionCreditType[] creditTypes)
        {
            Text = "Διόρθωση τύπου myDATA — 11.1 σε 1.1";
            ThemeName = "TelerikMetroBlue";
            ClientSize = new Size(1330, 780); MinimumSize = new Size(1110, 720); StartPosition = FormStartPosition.CenterParent;

            this.creditTypes = creditTypes ?? new InvoiceTypeCorrectionCreditType[0];

            var top = new Panel { Dock = DockStyle.Top, Height = 185 }; Controls.Add(top);
            LabelAt(top, "Από", 16, 17, 40); InputAt(top, from, 58, 16, 127); from.Value = startDate.Date;
            LabelAt(top, "Έως", 200, 17, 40); InputAt(top, to, 242, 16, 127); to.Value = endDate.Date;
            from.Format = to.Format = DateTimePickerFormat.Custom; from.CustomFormat = to.CustomFormat = "dd/MM/yyyy";
            LabelAt(top, "Προεπιλογή: χθες έως σήμερα", 399, 17, 310);
            Button(top, scan, "Έλεγχος παραστατικών", 1059, 13, 251); scan.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            LabelAt(top, "Η επανέκδοση κρατά αυτόματα τον τύπο του αρχικού παραστατικού. Η επιλογή αφορά μόνο το πιστωτικό λιανικής 11.4.", 16, 49, 1294);
            LabelAt(top, "Τύπος πιστωτικού 11.4", 16, 79, 350);
            InputAt(top, retailCredit, 373, 78, 570);
            retailCredit.DropDownStyle = Telerik.WinControls.RadDropDownStyle.DropDownList;
            retailCredit.DropDownMinSize = new Size(570, 160);
            LabelAt(top, "myDATA: fallback μόνο αν απορριφθεί η ακύρωση MARK.", 960, 79, 350);
            Button(top, select, "Επιλογή όλων των επιλέξιμων", 16, 132, 290);
            Button(top, clear, "Καθαρισμός επιλογής", 319, 132, 227);
            summary.SetBounds(565, 132, 745, 35); summary.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            summary.Font = new Font(Font, FontStyle.Bold); summary.TextAlign = ContentAlignment.MiddleLeft; top.Controls.Add(summary);

            var bottom = new Panel { Dock = DockStyle.Bottom, Height = 210 }; Controls.Add(bottom);
            detail.SetBounds(16, 8, 1294, 65); detail.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; bottom.Controls.Add(detail);
            var explanation = LabelAt(bottom,
                "Η παρτίδα χρησιμοποιεί την αρχική διαδρομή και το αποδεκτό MARK. Δεν δημιουργεί νέα είσπραξη ή παράδοση καυσίμου. Κάθε βήμα αποθηκεύεται πριν συνεχίσει.",
                16, 79, 1294); explanation.Height = 32;
            var actions = new FlowLayoutPanel { Left = 13, Top = 117, Width = 1300, Height = 87,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom }; bottom.Controls.Add(actions);
            Button(actions, start, "Έναρξη παρτίδας", 0, 0, 220);
            Button(actions, resume, "Συνέχιση εκκρεμοτήτων", 0, 0, 242);
            Button(actions, pause, "Παύση", 0, 0, 100); pause.Margin = new Padding(3, 3, 20, 3);
            Button(actions, print, "Εκτύπωση ολοκληρωμένων", 0, 0, 246);
            Button(actions, export, "Εξαγωγή αναφοράς", 0, 0, 177);

            grid.Dock = DockStyle.Fill; grid.ShowGroupPanel = false; grid.AutoGenerateColumns = false;
            grid.AllowAddNewRow = false; grid.AllowDeleteRow = false; grid.AutoSizeColumnsMode = GridViewAutoSizeColumnsMode.Fill;
            grid.EnableFiltering = true; grid.AutoSizeRows = true;
            grid.Columns.Add(new GridViewCheckBoxColumn("Selected") { HeaderText = "", Width = 35 });
            string[] fields = { "Document", "Date", "Customer", "VatNumber", "Provider", "Mark", "Amount", "Method", "Credit", "Replacement", "Status" };
            string[] labels = { "Αρχικό", "Ημερομηνία", "Πελάτης", "ΑΦΜ", "Διαδρομή", "MARK", "Σύνολο €", "Μέθοδος", "11.4", "1.1", "Κατάσταση / αιτία εξαίρεσης" };
            int[] widths = { 110, 85, 130, 80, 140, 90, 75, 125, 105, 105, 300 };
            for (int i = 0; i < fields.Length; i++)
                grid.Columns.Add(new GridViewTextBoxColumn(fields[i]) { HeaderText = labels[i], Width = widths[i], ReadOnly = true, WrapText = true });
            grid.Columns["Date"].FormatString = "{0:dd/MM/yyyy}"; grid.Columns["Amount"].FormatString = "{0:N2}";
            Controls.Add(grid); grid.BringToFront(); grid.DataSource = rows;

            grid.CellBeginEdit += (sender, args) => {
                var row = grid.CurrentRow?.DataBoundItem as InvoiceTypeCorrectionRow;
                if (busy || row?.Eligible != true) args.Cancel = true;
            };
            grid.CellValueChanged += (sender, args) => UpdateButtons();
            scan.Click += async (sender, args) => await Scan();
            select.Click += (sender, args) => { foreach (var row in rows) row.Selected = row.Eligible; rows.ResetBindings(); UpdateButtons(); };
            clear.Click += (sender, args) => { foreach (var row in rows) row.Selected = false; rows.ResetBindings(); UpdateButtons(); };
            start.Click += async (sender, args) => await Run(false);
            resume.Click += async (sender, args) => await Run(true);
            pause.Click += (sender, args) => { stopRequested = true; detail.Text = "Ζητήθηκε παύση. Ολοκληρώνεται η τρέχουσα επικοινωνία και αποθηκεύεται η πρόοδος."; pause.Enabled = false; };
            export.Click += (sender, args) => Export(); print.Click += async (sender, args) => await Print();
            from.ValueChanged += (sender, args) => InvalidateScan(); to.ValueChanged += (sender, args) => InvalidateScan();
            retailCredit.SelectedIndexChanged += (sender, args) => InvalidateScan();
            FormClosing += (sender, args) => { if (busy) { stopRequested = true; args.Cancel = true; detail.Text = "Η παρτίδα σταματά μετά την τρέχουσα επικοινωνία. Κλείστε όταν ολοκληρωθεί η παύση."; } };
            retailCredit.DataSource = this.creditTypes;
            retailCredit.DisplayMember = "Display"; retailCredit.ValueMember = "Id";
            detail.Text = "Το διάστημα ξεκινά από χθες έως σήμερα. Ο Έλεγχος δεν στέλνει, δεν ακυρώνει και δεν εκτυπώνει τίποτα.";
            UpdateButtons();
        }

        private static Label LabelAt(Control parent, string text, int x, int y, int width)
        { var label = new Label { Text = text, Bounds = new Rectangle(x, y, width, 27), TextAlign = ContentAlignment.MiddleLeft }; parent.Controls.Add(label); return label; }
        private static void InputAt(Control parent, Control input, int x, int y, int width)
        { input.AutoSize = false; input.SetBounds(x, y, width, 28); parent.Controls.Add(input); }
        private static void Button(Control parent, RadButton button, string text, int x, int y, int width)
        { button.Text = text; button.SetBounds(x, y, width, 35); parent.Controls.Add(button); }

        private void InvalidateScan()
        {
            if (busy) return;
            scanned = false;
            foreach (var row in rows) { row.Selected = false; row.Eligible = false; }
            rows.ResetBindings(); UpdateButtons();
        }

        private void UpdateButtons()
        {
            int count = rows.Count(row => row.Eligible && row.Selected);
            decimal total = rows.Where(row => row.Eligible && row.Selected).Sum(row => row.Amount);
            summary.Text = "Επιλεγμένα: " + count + "   ·   " + total.ToString("N2", CultureInfo.GetCultureInfo("el-GR")) + " €";
            scan.Enabled = select.Enabled = clear.Enabled = from.Enabled = to.Enabled = retailCredit.Enabled = !busy;
            start.Enabled = !busy && scanned && count > 0; resume.Enabled = !busy; export.Enabled = !busy && rows.Count > 0;
            print.Enabled = !busy; pause.Enabled = busy && !stopRequested;
        }

        private void Merge(InvoiceTypeCorrectionRow[] updated)
        {
            foreach (var row in updated)
            {
                var old = rows.FirstOrDefault(value => value.Id == row.Id);
                if (old != null) rows[rows.IndexOf(old)] = row; else rows.Add(row);
            }
            rows.ResetBindings();
        }

        private bool TrySelectedCreditType(out Guid id)
        {
            id = Guid.Empty;
            if (!(retailCredit.SelectedValue is Guid)) return false;
            var selected = (Guid)retailCredit.SelectedValue;
            if (!creditTypes.Any(type => type.Id == selected)) return false;
            id = selected;
            return true;
        }

        private async Task Scan()
        {
            DateTime first = from.Value.Date, last = to.Value.Date;
            Guid credit;
            if (!TrySelectedCreditType(out credit))
            {
                detail.Text = "Επιλέξτε τύπο πιστωτικού 11.4 από την αναπτυσσόμενη λίστα.";
                retailCredit.Focus(); retailCredit.ShowDropDown(); return;
            }
            busy = true; scanned = false; stopRequested = false; UpdateButtons();
            try
            {
                var values = await Task.Run(() => SharedInvoiceRuntime.ScanInvoiceTypeCorrections(first, last, credit, () => stopRequested));
                rows = new BindingList<InvoiceTypeCorrectionRow>(values.ToList()); grid.DataSource = rows; scanned = !stopRequested;
                detail.Text = stopRequested ? "Ο έλεγχος διακόπηκε. Εκτελέστε νέο έλεγχο πριν ξεκινήσετε παρτίδα."
                    : "Ο έλεγχος ολοκληρώθηκε. Δεν έγινε αποστολή, ακύρωση ή εκτύπωση. Οι εξαιρέσεις δεν μπορούν να επιλεγούν.";
            }
            catch (Exception ex) { Error(ex); }
            finally { busy = false; UpdateButtons(); }
        }

        private async Task Run(bool continuing)
        {
            grid.EndEdit(); var selectedRows = rows.ToArray(); busy = true; stopRequested = false; UpdateButtons();
            IProgress<string> progress = new Progress<string>(text => detail.Text = text);
            try
            {
                var updated = await Task.Run(() => SharedInvoiceRuntime.RunInvoiceTypeCorrections(selectedRows, continuing,
                    () => stopRequested, text => progress.Report(text)));
                Merge(updated); scanned = false;
                detail.Text = stopRequested ? "Η παρτίδα τέθηκε σε παύση. Η πρόοδος αποθηκεύτηκε."
                    : "Η εκτέλεση ολοκληρώθηκε. Η Συνέχιση χειρίζεται μόνο τις αποθηκευμένες εκκρεμότητες.";
            }
            catch (Exception ex) { scanned = false; Error(ex); }
            finally { busy = false; UpdateButtons(); }
        }

        private async Task Print()
        {
            busy = true; stopRequested = false; UpdateButtons();
            try
            {
                await Task.Run(() => SharedInvoiceRuntime.PrintInvoiceTypeCorrectionsCompleted(() => stopRequested));
                detail.Text = stopRequested ? "Η εκτύπωση τέθηκε σε παύση." : "Τα διαθέσιμα ολοκληρωμένα διορθωτικά στάλθηκαν στην εκτύπωση.";
            }
            catch (Exception ex) { Error(ex); }
            finally { busy = false; UpdateButtons(); }
        }

        private void Export()
        {
            SharedInvoiceRuntime.RequireInvoiceTypeCorrectionAdmin();
            using (var dialog = new SaveFileDialog { Filter = "CSV (*.csv)|*.csv", FileName = "Διόρθωση τύπου myDATA 11.1 σε 1.1.csv" })
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    Func<string, string> quote = value => "\"" + ((value ?? "").Length > 0 && "=+-@".Contains((value ?? "")[0]) ? "'" : "") +
                        (value ?? "").Replace("\"", "\"\"") + "\"";
                    var lines = new[] { "Αρχικό;Ημερομηνία;Πελάτης;ΑΦΜ;Διαδρομή;MARK;Σύνολο;Μέθοδος;Πιστωτικό;Επανέκδοση;Κατάσταση" }
                        .Concat(rows.Select(row => string.Join(";", new[] { row.Document, row.Date.ToString("dd/MM/yyyy"), row.Customer,
                            row.VatNumber, row.Provider, row.Mark.ToString(CultureInfo.InvariantCulture),
                            row.Amount.ToString(CultureInfo.GetCultureInfo("el-GR")), row.Method, row.Credit, row.Replacement, row.Status }.Select(quote))));
                    File.WriteAllLines(dialog.FileName, lines, new UTF8Encoding(true));
                }
        }

        private void Error(Exception ex)
        {
            detail.Text = ex.Message;
            RadMessageBox.Show(this, ex.Message, "Διόρθωση τύπου myDATA 11.1 → 1.1", MessageBoxButtons.OK, RadMessageIcon.Error);
        }
    }
}
