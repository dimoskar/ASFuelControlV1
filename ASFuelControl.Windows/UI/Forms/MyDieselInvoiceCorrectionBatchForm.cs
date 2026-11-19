using System;
using System.Collections.Generic;
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
    internal sealed class MyDieselInvoiceCorrectionBatchForm : RadForm
    {
        private readonly RadDateTimePicker from = new RadDateTimePicker();
        private readonly RadDateTimePicker to = new RadDateTimePicker();
        private readonly RadDropDownList creditType = new RadDropDownList();
        private readonly RadDropDownList replacementType =
            new RadDropDownList();
        private readonly RadDropDownList eligibilityFilter =
            new RadDropDownList();
        private readonly RadGridView grid = new RadGridView();
        private readonly Label summary = new Label();
        private readonly Label detail = new Label();
        private readonly RadButton scan = new RadButton();
        private readonly RadButton select = new RadButton();
        private readonly RadButton clear = new RadButton();
        private readonly RadButton start = new RadButton();
        private readonly RadButton resume = new RadButton();
        private readonly RadButton pause = new RadButton();
        private readonly RadButton export = new RadButton();
        private readonly RadButton print = new RadButton();
        private BindingList<MyDieselInvoiceCorrectionRow> rows =
            new BindingList<MyDieselInvoiceCorrectionRow>();
        private volatile bool stopRequested;
        private bool busy;
        private bool scanned;

        internal MyDieselInvoiceCorrectionBatchForm(DateTime startDate,
            DateTime endDate)
        {
            MyDieselCorrectionInvoiceTypeChoice[] creditTypes =
                SharedInvoiceRuntime.MyDieselCorrectionCreditTypes();
            MyDieselCorrectionInvoiceTypeChoice[] replacementTypes =
                SharedInvoiceRuntime.MyDieselCorrectionReplacementTypes();
            Initialize(startDate, endDate, creditTypes, replacementTypes);
        }

        internal MyDieselInvoiceCorrectionBatchForm(DateTime startDate,
            DateTime endDate,
            MyDieselCorrectionInvoiceTypeChoice[] creditTypes,
            MyDieselCorrectionInvoiceTypeChoice[] replacementTypes)
        {
            Initialize(startDate, endDate, creditTypes, replacementTypes);
        }

        private void Initialize(DateTime startDate, DateTime endDate,
            MyDieselCorrectionInvoiceTypeChoice[] creditTypes,
            MyDieselCorrectionInvoiceTypeChoice[] replacementTypes)
        {
            Text = "Διόρθωση παραστατικών myDiesel — Πίστωση και επανέκδοση";
            ThemeName = "TelerikMetroBlue";
            ClientSize = new Size(1320, 790);
            MinimumSize = new Size(1080, 720);
            StartPosition = FormStartPosition.CenterParent;

            var top = new Panel
            {
                Dock = DockStyle.Top,
                Height = 190
            };
            Controls.Add(top);
            LabelAt(top, "Από", 16, 17, 40);
            InputAt(top, from, 58, 16, 127);
            from.Value = startDate.Date;
            LabelAt(top, "Έως", 200, 17, 40);
            InputAt(top, to, 242, 16, 127);
            to.Value = endDate.Date;
            from.Format = DateTimePickerFormat.Custom;
            to.Format = DateTimePickerFormat.Custom;
            from.CustomFormat = "dd/MM/yyyy";
            to.CustomFormat = "dd/MM/yyyy";
            LabelAt(top, "Εμφάνιση", 395, 17, 75);
            InputAt(top, eligibilityFilter, 473, 16, 245);
            eligibilityFilter.DropDownStyle =
                RadDropDownStyle.DropDownList;
            eligibilityFilter.Items.Add("Όλες οι γραμμές");
            eligibilityFilter.Items.Add("Μόνο επιλέξιμες");
            eligibilityFilter.Items.Add("Μόνο μη επιλέξιμες");
            eligibilityFilter.SelectedIndex = 0;
            Button(top, scan, "Έλεγχος παραστατικών", 1049, 13, 251);
            scan.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            LabelAt(top, "Πιστωτικό τιμολόγιο", 16, 62, 210);
            InputAt(top, creditType, 230, 61, 1070);
            creditType.Anchor = AnchorStyles.Top | AnchorStyles.Left |
                AnchorStyles.Right;
            LabelAt(top, "Επανέκδοση τιμολογίου", 16, 102, 210);
            InputAt(top, replacementType, 230, 101, 1070);
            replacementType.Anchor = AnchorStyles.Top | AnchorStyles.Left |
                AnchorStyles.Right;
            creditType.DropDownStyle = RadDropDownStyle.DropDownList;
            replacementType.DropDownStyle = RadDropDownStyle.DropDownList;
            creditType.DisplayMember = "Display";
            creditType.ValueMember = "Id";
            replacementType.DisplayMember = "Display";
            replacementType.ValueMember = "Id";
            creditType.DataSource = creditTypes ??
                new MyDieselCorrectionInvoiceTypeChoice[0];
            replacementType.DataSource = replacementTypes ??
                new MyDieselCorrectionInvoiceTypeChoice[0];

            Button(top, select, "Επιλογή όλων των επιλέξιμων", 16, 144,
                290);
            Button(top, clear, "Καθαρισμός επιλογής", 319, 144, 227);
            summary.SetBounds(565, 144, 735, 35);
            summary.Anchor = AnchorStyles.Top | AnchorStyles.Left |
                AnchorStyles.Right;
            summary.Font = new Font(Font, FontStyle.Bold);
            summary.TextAlign = ContentAlignment.MiddleLeft;
            top.Controls.Add(summary);

            var bottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 210
            };
            Controls.Add(bottom);
            detail.SetBounds(16, 8, 1284, 65);
            detail.Anchor = AnchorStyles.Top | AnchorStyles.Left |
                AnchorStyles.Right;
            bottom.Controls.Add(detail);
            Label explanation = LabelAt(bottom,
                "Επανυπολογισμός έκπτωσης κουπονιού και επανέκδοση → διόρθωση συσχέτισης μέσω myDiesel → πίστωση αρχικού. Αν το API απορρίψει τη διόρθωση, πιστώνεται η επανέκδοση και το κουπόνι μένει στο αρχικό.",
                16, 79, 1284);
            explanation.Height = 32;
            var actions = new FlowLayoutPanel
            {
                Left = 13,
                Top = 117,
                Width = 1290,
                Height = 87,
                Anchor = AnchorStyles.Left | AnchorStyles.Right |
                    AnchorStyles.Bottom
            };
            bottom.Controls.Add(actions);
            Button(actions, start, "Έναρξη παρτίδας", 0, 0, 220);
            Button(actions, resume, "Συνέχιση εκκρεμοτήτων", 0, 0, 242);
            Button(actions, pause, "Παύση", 0, 0, 100);
            pause.Margin = new Padding(3, 3, 20, 3);
            Button(actions, print, "Εκτύπωση ολοκληρωμένων", 0, 0, 246);
            Button(actions, export, "Εξαγωγή αναφοράς", 0, 0, 177);

            ConfigureGrid();
            Controls.Add(grid);
            grid.BringToFront();
            grid.DataSource = rows;
            WireEvents();
            detail.Text =
                "Επιλέξτε ημερομηνίες και τους δύο τύπους τιμολογίων. Ο έλεγχος δεν εκδίδει και δεν μεταβάλλει παραστατικά.";
            UpdateButtons();
        }

        private void ConfigureGrid()
        {
            grid.Dock = DockStyle.Fill;
            grid.ShowGroupPanel = false;
            grid.AutoGenerateColumns = false;
            grid.AllowAddNewRow = false;
            grid.AllowDeleteRow = false;
            grid.AutoSizeColumnsMode = GridViewAutoSizeColumnsMode.Fill;
            grid.EnableFiltering = true;
            grid.AutoSizeRows = true;
            var selectionColumn = new GridViewCheckBoxColumn("Selected")
            {
                HeaderText = string.Empty,
                Width = 35
            };
            grid.Columns.Add(selectionColumn);
            string[] fields =
            {
                "Voucher", "Document", "Date", "Customer", "VatNumber",
                "Provider", "Mark", "CouponStatus", "OriginalAmount",
                "Amount", "Credit", "Replacement", "Status"
            };
            string[] labels =
            {
                "Κουπόνι", "Αρχικό", "Ημερομηνία", "Πελάτης", "ΑΦΜ",
                "Πάροχος", "MARK", "Κατάσταση κουπονιού", "Αρχικό €",
                "Τελικό €", "Πιστωτικό", "Επανέκδοση",
                "Κατάσταση / αιτία εξαίρεσης"
            };
            int[] widths = { 145, 105, 80, 120, 75, 120, 90, 105, 75, 75,
                110, 110, 320 };
            for (int index = 0; index < fields.Length; index++)
            {
                var column = new GridViewTextBoxColumn(fields[index])
                {
                    HeaderText = labels[index],
                    Width = widths[index],
                    ReadOnly = true,
                    WrapText = true
                };
                grid.Columns.Add(column);
            }
            grid.Columns["Date"].FormatString = "{0:dd/MM/yyyy}";
            grid.Columns["OriginalAmount"].FormatString = "{0:N2}";
            grid.Columns["Amount"].FormatString = "{0:N2}";
        }

        private void WireEvents()
        {
            grid.CellBeginEdit += GridCellBeginEdit;
            grid.CellValueChanged += GridCellValueChanged;
            scan.Click += ScanClick;
            select.Click += SelectClick;
            clear.Click += ClearClick;
            start.Click += StartClick;
            resume.Click += ResumeClick;
            pause.Click += PauseClick;
            export.Click += ExportClick;
            print.Click += PrintClick;
            from.ValueChanged += SelectionChanged;
            to.ValueChanged += SelectionChanged;
            creditType.SelectedIndexChanged += SelectionChanged;
            replacementType.SelectedIndexChanged += SelectionChanged;
            eligibilityFilter.SelectedIndexChanged +=
                EligibilityFilterChanged;
            FormClosing += CorrectionFormClosing;
        }

        private static Label LabelAt(Control parent, string text, int x,
            int y, int width)
        {
            var label = new Label
            {
                Text = text,
                Bounds = new Rectangle(x, y, width, 27),
                TextAlign = ContentAlignment.MiddleLeft
            };
            parent.Controls.Add(label);
            return label;
        }

        private static void InputAt(Control parent, Control input, int x,
            int y, int width)
        {
            input.AutoSize = false;
            input.SetBounds(x, y, width, 28);
            parent.Controls.Add(input);
        }

        private static void Button(Control parent, RadButton button,
            string text, int x, int y, int width)
        {
            button.Text = text;
            button.SetBounds(x, y, width, 35);
            parent.Controls.Add(button);
        }

        private bool TrySelectedTypes(out Guid creditId,
            out Guid replacementId)
        {
            creditId = creditType.SelectedValue is Guid
                ? (Guid)creditType.SelectedValue
                : Guid.Empty;
            replacementId = replacementType.SelectedValue is Guid
                ? (Guid)replacementType.SelectedValue
                : Guid.Empty;
            return creditId != Guid.Empty && replacementId != Guid.Empty;
        }

        private void InvalidateScan()
        {
            if (busy)
            {
                return;
            }
            scanned = false;
            foreach (MyDieselInvoiceCorrectionRow row in rows)
            {
                row.Selected = false;
                row.Eligible = false;
            }
            rows.ResetBindings();
            ApplyEligibilityFilter();
            UpdateButtons();
        }

        private void UpdateButtons()
        {
            int selectedCount = rows.Count(row =>
                row.Eligible && row.Selected);
            decimal selectedAmount = rows.Where(row =>
                    row.Eligible && row.Selected)
                .Sum(row => row.Amount);
            summary.Text = "Επιλεγμένα: " + selectedCount + "   ·   " +
                selectedAmount.ToString("N2",
                    CultureInfo.GetCultureInfo("el-GR")) + " €";
            scan.Enabled = !busy;
            select.Enabled = !busy;
            clear.Enabled = !busy;
            from.Enabled = !busy;
            to.Enabled = !busy;
            creditType.Enabled = !busy;
            replacementType.Enabled = !busy;
            eligibilityFilter.Enabled = !busy;
            start.Enabled = !busy && scanned && selectedCount > 0;
            resume.Enabled = !busy;
            export.Enabled = !busy && rows.Count > 0;
            print.Enabled = !busy;
            pause.Enabled = busy && !stopRequested;
        }

        private async Task ScanAsync()
        {
            Guid creditId;
            Guid replacementId;
            if (!TrySelectedTypes(out creditId, out replacementId))
            {
                detail.Text =
                    "Επιλέξτε τύπο πιστωτικού και τύπο επανέκδοσης.";
                return;
            }
            DateTime first = from.Value.Date;
            DateTime last = to.Value.Date;
            busy = true;
            scanned = false;
            stopRequested = false;
            UpdateButtons();
            try
            {
                MyDieselInvoiceCorrectionRow[] values = await Task.Run(() =>
                    SharedInvoiceRuntime.ScanMyDieselInvoiceCorrections(first,
                        last, creditId, replacementId,
                        () => stopRequested));
                rows = new BindingList<MyDieselInvoiceCorrectionRow>(
                    values.ToList());
                grid.DataSource = rows;
                ApplyEligibilityFilter();
                scanned = !stopRequested;
                detail.Text = stopRequested
                    ? "Ο έλεγχος διακόπηκε. Εκτελέστε νέο έλεγχο."
                    : "Ο έλεγχος ολοκληρώθηκε. Δεν έγινε έκδοση ή αλλαγή κουπονιού.";
            }
            catch (Exception exception)
            {
                ShowError(exception);
            }
            finally
            {
                busy = false;
                UpdateButtons();
            }
        }

        private async Task RunAsync(bool continuing)
        {
            grid.EndEdit();
            MyDieselInvoiceCorrectionRow[] selectedRows = rows.ToArray();
            busy = true;
            stopRequested = false;
            UpdateButtons();
            IProgress<string> progress = new Progress<string>(text =>
                detail.Text = text);
            try
            {
                MyDieselInvoiceCorrectionRow[] updated = await Task.Run(() =>
                    SharedInvoiceRuntime.RunMyDieselInvoiceCorrections(
                        selectedRows, continuing, () => stopRequested,
                        text => progress.Report(text)));
                rows = new BindingList<MyDieselInvoiceCorrectionRow>(
                    updated.ToList());
                grid.DataSource = rows;
                ApplyEligibilityFilter();
                scanned = false;
                detail.Text = stopRequested
                    ? "Η παρτίδα τέθηκε σε παύση. Η πρόοδος αποθηκεύτηκε."
                    : "Η εκτέλεση ολοκληρώθηκε. Ελέγξτε την κατάσταση κάθε γραμμής.";
            }
            catch (Exception exception)
            {
                scanned = false;
                ShowError(exception);
            }
            finally
            {
                busy = false;
                UpdateButtons();
            }
        }

        private async Task PrintAsync()
        {
            busy = true;
            stopRequested = false;
            UpdateButtons();
            try
            {
                await Task.Run(() =>
                    SharedInvoiceRuntime.PrintMyDieselInvoiceCorrectionsCompleted(
                        () => stopRequested));
                detail.Text = stopRequested
                    ? "Η εκτύπωση τέθηκε σε παύση."
                    : "Τα ολοκληρωμένα διορθωτικά στάλθηκαν στην εκτύπωση.";
            }
            catch (Exception exception)
            {
                ShowError(exception);
            }
            finally
            {
                busy = false;
                UpdateButtons();
            }
        }

        private void ExportReport()
        {
            SharedInvoiceRuntime.RequireMyDieselInvoiceCorrectionAdmin();
            using (var dialog = new SaveFileDialog
            {
                Filter = "CSV (*.csv)|*.csv",
                FileName = "Διόρθωση παραστατικών myDiesel.csv"
            })
            {
                DialogResult result = dialog.ShowDialog(this);
                if (result != DialogResult.OK)
                {
                    return;
                }
                string[] header =
                {
                    "Κουπόνι;Αρχικό;Ημερομηνία;Πελάτης;ΑΦΜ;Πάροχος;MARK;Κατάσταση κουπονιού;Αρχικό σύνολο;Τελικό σύνολο;Πιστωτικό;Επανέκδοση;Κατάσταση"
                };
                CultureInfo greekCulture = CultureInfo.GetCultureInfo("el-GR");
                var dataRows = new List<string>();
                foreach (MyDieselInvoiceCorrectionRow row in rows)
                {
                    string dataRow = FormatCsvRow(row, greekCulture);
                    dataRows.Add(dataRow);
                }
                string[] data = dataRows.ToArray();
                string[] lines = header.Concat(data).ToArray();
                File.WriteAllLines(dialog.FileName, lines,
                    new UTF8Encoding(true));
            }
        }

        private static string FormatCsvRow(
            MyDieselInvoiceCorrectionRow row, CultureInfo culture)
        {
            string date = row.Date.ToString("dd/MM/yyyy");
            string originalAmount = row.OriginalAmount.ToString(culture);
            string amount = row.Amount.ToString(culture);
            string[] values =
            {
                row.Voucher, row.Document, date, row.Customer,
                row.VatNumber, row.Provider, row.Mark, row.CouponStatus,
                originalAmount, amount, row.Credit, row.Replacement,
                row.Status
            };
            IEnumerable<string> quotedValues = values.Select(Quote);
            return string.Join(";", quotedValues);
        }

        private static string Quote(string value)
        {
            string text = value ?? string.Empty;
            bool formula = text.Length > 0 && "=+-@".Contains(text[0]);
            string safeText = formula ? "'" + text : text;
            return "\"" + safeText.Replace("\"", "\"\"") + "\"";
        }

        private void ShowError(Exception exception)
        {
            detail.Text = exception.Message;
            RadMessageBox.Show(this, exception.Message,
                "Διόρθωση παραστατικών myDiesel", MessageBoxButtons.OK,
                RadMessageIcon.Error);
        }

        private void GridCellBeginEdit(object sender,
            GridViewCellCancelEventArgs args)
        {
            var row = grid.CurrentRow == null ? null :
                grid.CurrentRow.DataBoundItem as MyDieselInvoiceCorrectionRow;
            if (busy || row == null || !row.Eligible)
            {
                args.Cancel = true;
            }
        }

        private void GridCellValueChanged(object sender,
            GridViewCellEventArgs args)
        {
            UpdateButtons();
        }

        private void EligibilityFilterChanged(object sender, EventArgs args)
        {
            ApplyEligibilityFilter();
        }

        private void ApplyEligibilityFilter()
        {
            int filterIndex = eligibilityFilter.SelectedIndex;
            foreach (GridViewRowInfo gridRow in grid.Rows)
            {
                MyDieselInvoiceCorrectionRow row =
                    gridRow.DataBoundItem as MyDieselInvoiceCorrectionRow;
                bool visible = row != null &&
                    (filterIndex <= 0 || filterIndex == 1 && row.Eligible ||
                     filterIndex == 2 && !row.Eligible);
                gridRow.IsVisible = visible;
            }
        }

        private async void ScanClick(object sender, EventArgs args)
        {
            await ScanAsync();
        }

        private void SelectClick(object sender, EventArgs args)
        {
            foreach (MyDieselInvoiceCorrectionRow row in rows)
            {
                row.Selected = row.Eligible;
            }
            rows.ResetBindings();
            UpdateButtons();
        }

        private void ClearClick(object sender, EventArgs args)
        {
            foreach (MyDieselInvoiceCorrectionRow row in rows)
            {
                row.Selected = false;
            }
            rows.ResetBindings();
            UpdateButtons();
        }

        private async void StartClick(object sender, EventArgs args)
        {
            await RunAsync(false);
        }

        private async void ResumeClick(object sender, EventArgs args)
        {
            await RunAsync(true);
        }

        private void PauseClick(object sender, EventArgs args)
        {
            stopRequested = true;
            pause.Enabled = false;
            detail.Text =
                "Ζητήθηκε παύση. Ολοκληρώνεται η τρέχουσα επικοινωνία και αποθηκεύεται η πρόοδος.";
        }

        private void ExportClick(object sender, EventArgs args)
        {
            ExportReport();
        }

        private async void PrintClick(object sender, EventArgs args)
        {
            await PrintAsync();
        }

        private void SelectionChanged(object sender, EventArgs args)
        {
            InvalidateScan();
        }

        private void CorrectionFormClosing(object sender,
            FormClosingEventArgs args)
        {
            if (!busy)
            {
                return;
            }
            stopRequested = true;
            args.Cancel = true;
            detail.Text =
                "Η παρτίδα σταματά μετά την τρέχουσα επικοινωνία. Κλείστε όταν ολοκληρωθεί η παύση.";
        }
    }
}
