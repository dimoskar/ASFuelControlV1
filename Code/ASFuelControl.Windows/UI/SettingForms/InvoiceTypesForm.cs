using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Telerik.WinControls.UI;

namespace ASFuelControl.Windows.UI.SettingForms
{
    public partial class InvoiceTypesForm : RadForm
    {
        BindingList<Data.InvoiceType> invoiceTypes;
        Data.DatabaseModel database = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);

        public InvoiceTypesForm()
        {
            InitializeComponent();
            if (this.DesignMode)
                return;
            invoiceTypes = new BindingList<Data.InvoiceType>(this.database.InvoiceTypes.OrderBy(it => it.Description).ToList());
            this.invoiceTypeBindingSource.DataSource = invoiceTypes;
            this.radButton1.DataBindings.Add(new System.Windows.Forms.Binding("Enabled", this.database, "DatabaseChanged", true));
        }

        private void radButton2_Click(object sender, EventArgs e)
        {
            Data.InvoiceType newInvType = new Data.InvoiceType();
            newInvType.LastNumber = 0;
            newInvType.InvoiceTypeId = Guid.NewGuid();

            this.database.Add(newInvType);
            this.invoiceTypes.Add(newInvType);
            this.invoiceTypeBindingSource.Position = this.invoiceTypes.IndexOf(newInvType);
            this.invoiceTypeRadGridView.Focus();
        }

        private void InvoiceTypesForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!this.database.DatabaseChanged)
                return;
            DialogResult res = MessageBox.Show("Θέλετε να αποθηκευτούν οι αλλαγές που κάνατε;", "Έγιναν αλλαγές", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
            if (res == System.Windows.Forms.DialogResult.No)
                return;
            else if (res == System.Windows.Forms.DialogResult.Yes)
            {
                this.database.SaveChanges();
                return;
            }
            e.Cancel = true;
        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            this.database.SaveChanges();
        }

        private void radButton3_Click(object sender, EventArgs e)
        {
            Data.InvoiceType invType = this.invoiceTypeBindingSource.Current as Data.InvoiceType;
            if (invType == null)
                return;
            if (invType.Invoices.Count > 0)
            {
                MessageBox.Show("Ο τύπος παραστατικόυ που προσπαθείτε να διαγράψετε\r\nέχει ήδη χρησιμοποιηθεί", "Δεν επιτρέπεται η διαγραφή", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            this.invoiceTypes.Remove(invType);
            this.database.Delete(invType);
        }

        private void invoiceTypeRadGridView_CommandCellClick(object sender, EventArgs e)
        {
            GridCommandCellElement cell = sender as GridCommandCellElement;
            if (cell == null)
                return;

            Data.InvoiceType invType = cell.RowElement.Data.DataBoundItem as Data.InvoiceType;
            if (invType == null)
                return;

            string defaultTaxDevice = Data.Implementation.OptionHandler.Instance.GetOption("DefaultTaxDevice");
            if (defaultTaxDevice == "Samtec")
            {
                using (SelectionForms.SelectPrinterForm spf = new SelectionForms.SelectPrinterForm())
                {
                    DialogResult res = spf.ShowDialog();
                    if (res == System.Windows.Forms.DialogResult.Cancel || spf.SelectedPrinter == null || spf.SelectedPrinter.Length == 0)
                        return;

                    invType.Printer = spf.SelectedPrinter;
                }
            }
            else
            {
                using (FolderBrowserDialog dlg = new FolderBrowserDialog())
                {
                    DialogResult res = dlg.ShowDialog();
                    if (res == System.Windows.Forms.DialogResult.Cancel)
                        return;

                    invType.Printer = dlg.SelectedPath;
                }
            }
        }
    }
}
