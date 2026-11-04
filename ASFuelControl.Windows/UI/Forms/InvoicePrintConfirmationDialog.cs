using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;
using ASFuelControl.Windows.Threads;
using Telerik.WinControls.UI;

namespace ASFuelControl.Windows.UI.Forms
{
    internal sealed class InvoicePrintConfirmationDialog : RadForm
    {
        private readonly RadTextBox printerName;
        private readonly RadButton print;
        private readonly string originalPrinter;

        internal string SelectedPrinter { get; private set; }
        private bool PrinterChanged => !string.Equals(originalPrinter,SelectedPrinter,
            StringComparison.OrdinalIgnoreCase);

        private InvoicePrintConfirmationDialog(string prompt, string selectedPrinter)
        {
            Text = "Εκτύπωση Παραστατικού";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(570,190);

            var message = new RadLabel { Text = prompt, Dock = DockStyle.Top, Height = 42, AutoSize = false };
            var printerLabel = new RadLabel { Text = "Επιλεγμένος εκτυπωτής", Dock = DockStyle.Top, Height = 25 };
            var printerRow = new TableLayoutPanel { Dock = DockStyle.Top, Height = 35, ColumnCount = 2, Padding = new Padding(0) };
            printerRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            printerRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,155));
            printerName = new RadTextBox { Dock = DockStyle.Fill, ReadOnly = true };
            var change = new RadButton { Text = "Αλλαγή εκτυπωτή", Dock = DockStyle.Fill };
            change.Click += ChangePrinter;
            printerRow.Controls.Add(printerName,0,0);
            printerRow.Controls.Add(change,1,0);

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 45, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(5) };
            var cancel = new RadButton { Text = "Άκυρο", Width = 120, Height = 30, DialogResult = DialogResult.Cancel };
            print = new RadButton { Text = "Εκτύπωση", Width = 120, Height = 30, DialogResult = DialogResult.Yes };
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(print);
            Controls.Add(buttons);
            Controls.Add(printerRow);
            Controls.Add(printerLabel);
            Controls.Add(message);
            AcceptButton = print;
            CancelButton = cancel;
            originalPrinter = NormalizePrinter(selectedPrinter);
            SetPrinter(originalPrinter);
        }

        private void ChangePrinter(object sender, EventArgs e)
        {
            using (var dialog = new SelectionForms.SelectPrinterForm(SelectedPrinter))
                if (dialog.ShowDialog(this) == DialogResult.OK)
                    SetPrinter(dialog.SelectedPrinter);
        }

        private void SetPrinter(string value)
        {
            SelectedPrinter = NormalizePrinter(value);
            printerName.Text = SelectedPrinter ?? "Δεν βρέθηκε εγκατεστημένος εκτυπωτής";
            print.Enabled = SelectedPrinter != null;
        }

        private static string NormalizePrinter(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        internal static bool Confirm(IWin32Window owner, string prompt, string selectedPrinter, out string result)
        {
            using (var dialog = new InvoicePrintConfirmationDialog(prompt,selectedPrinter))
            {
                bool accepted = dialog.ShowDialog(owner) == DialogResult.Yes;
                // Null means "keep the configured printer".
                result = accepted && dialog.PrinterChanged ? dialog.SelectedPrinter : null;
                return accepted;
            }
        }

        internal static string ResolvePrinter(Data.Invoice invoice)
        {
            var installed = PrinterSettings.InstalledPrinters.Cast<string>().ToArray();
            string windowsDefault = installed.Length == 0 ? null : new PrinterSettings().PrinterName;
            string requested = null;
            string installationDefault = null;
            try
            {
                if (invoice?.InvoiceType?.IsLaserPrint == true)
                {
                    long revision;
                    var config = SharedInvoicingSettings.Load(out revision);
                    requested = config?.DefaultPrinter?.PrinterName;
                    installationDefault = requested;
                }
                else
                {
                    requested = SharedInvoiceConfiguration.PrinterName(invoice) ?? invoice?.Printer;
                    long revision;
                    installationDefault = SharedInvoicingSettings.Load(out revision)?.DefaultPrinter?.ThermalPrinterName;
                }
            }
            catch
            {
                requested = invoice?.Printer;
            }
            return SharedInvoicePrinting.ResolvePrinterName(requested,installationDefault,installed,windowsDefault);
        }
    }
}
