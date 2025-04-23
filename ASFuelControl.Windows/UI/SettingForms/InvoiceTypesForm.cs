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
        DeliveryTypeDatasource[] deliveryTypes = new DeliveryTypeDatasource[]
        {
            new DeliveryTypeDatasource(){ DeliveryType = null, Description = "(Χωρίς Επιλογή)" },
            new DeliveryTypeDatasource(){ DeliveryType = Common.Enumerators.DeliveryTypeEnum.Delivery, Description = "Παραλαβή" },
            new DeliveryTypeDatasource(){ DeliveryType = Common.Enumerators.DeliveryTypeEnum.Return, Description = "Επιστροφή"},
            new DeliveryTypeDatasource(){ DeliveryType = Common.Enumerators.DeliveryTypeEnum.Drain, Description = "Κένωση"},
            new DeliveryTypeDatasource(){ DeliveryType = Common.Enumerators.DeliveryTypeEnum.TransfusionOut, Description = "Μετάγγιση (Έξοδος)"},
            new DeliveryTypeDatasource(){ DeliveryType = Common.Enumerators.DeliveryTypeEnum.TransfusionIn, Description = "Μετάγγιση (Είσοδος)"}
        };

        public InvoiceTypesForm()
        {
            InitializeComponent();
            if (this.DesignMode)
                return;
            invoiceTypes = new BindingList<Data.InvoiceType>(this.database.InvoiceTypes.OrderBy(it => it.Description).ToList());
            this.invoiceTypeBindingSource.DataSource = invoiceTypes;
            this.radButton1.DataBindings.Add(new System.Windows.Forms.Binding("Enabled", this.database, "DatabaseChanged", true));
            this.Disposed += InvoiceTypesForm_Disposed;
            ((GridViewComboBoxColumn)this.invoiceTypeRadGridView.Columns["DeliveryType"]).DataSource = deliveryTypes;
            ((GridViewComboBoxColumn)this.invoiceTypeRadGridView.Columns["DeliveryType"]).DisplayMember = "Description";
            ((GridViewComboBoxColumn)this.invoiceTypeRadGridView.Columns["DeliveryType"]).ValueMember = "DeliveryTypeValue";
            ((GridViewComboBoxColumn)this.invoiceTypeRadGridView.Columns["DeliveryType"]).Width = 150;
        }

        private void InvoiceTypesForm_Disposed(object sender, EventArgs e)
        {
            this.database.Dispose();
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
            DialogResult res = Telerik.WinControls.RadMessageBox.Show("Θέλετε να αποθηκευτούν οι αλλαγές που κάνατε;", "Έγιναν αλλαγές", MessageBoxButtons.YesNoCancel, Telerik.WinControls.RadMessageIcon.Exclamation);
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
                Telerik.WinControls.RadMessageBox.Show("Ο τύπος παραστατικόυ που προσπαθείτε να διαγράψετε\r\nέχει ήδη χρησιμοποιηθεί", "Δεν επιτρέπεται η διαγραφή", MessageBoxButtons.OK, Telerik.WinControls.RadMessageIcon.Error);
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

    public class DeliveryTypeDatasource
    {
        public Common.Enumerators.DeliveryTypeEnum? DeliveryType { set; get; }
        public int? DeliveryTypeValue
        {
            get
            {
                if(this.DeliveryType.HasValue)
                    return (int)this.DeliveryType;
                return null;
            }
        }

        public string Description { set; get; }
    }
}
