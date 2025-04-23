using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Telerik.WinControls.UI;

namespace ASFuelControl.Windows.UI.SelectionForms
{
    public partial class SelectPrintedInvoiceForm : RadForm
    {
        Data.DatabaseModel database = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);

        public Guid NozzleId { set; get; }

        public Data.Invoice SelectedInvoice { private set; get; }

        public SelectPrintedInvoiceForm()
        {
            InitializeComponent();

            List<Data.InvoiceType> invoiceTypes = new List<Data.InvoiceType>();
            invoiceTypes = this.database.InvoiceTypes.Where(it => it.Printable && it.TransactionType == 0 && it.HasFinancialTransactions.Value).OrderBy(it => it.Description).ToList();
            Data.InvoiceType dummy = new Data.InvoiceType();
            dummy.Description = "(Όλά τα παραστατικά)";
            invoiceTypes.Insert(0, dummy);
            
            this.radDropDownList1.DataSource = invoiceTypes;
            this.radDropDownList1.DisplayMember = "Description";
            this.radDropDownList1.ValueMember = "InvoiceTypeId";

            this.radDropDownList1.SelectedIndex = 0;

            this.radDateTimePicker1.Value = DateTime.Now;
            this.radDateTimePicker2.Value = DateTime.Now;
            this.Load += new EventHandler(SelectPrintedInvoiceForm_Load);
            this.Disposed += SelectPrintedInvoiceForm_Disposed;
            
        }

        private void SelectPrintedInvoiceForm_Disposed(object sender, EventArgs e)
        {
            this.database.Dispose();
        }

        void SelectPrintedInvoiceForm_Load(object sender, EventArgs e)
        {
            this.LoadData();
        }

        private void LoadData()
        {
            DateTime dateFrom = this.radDateTimePicker1.Value.Date;
            DateTime dateTo = this.radDateTimePicker2.Value.Date;
            Data.InvoiceType invType = this.radDropDownList1.SelectedItem.DataBoundItem as Data.InvoiceType;
            int number = (int)this.radSpinEditor1.Value;
            if (this.NozzleId != Guid.Empty)
            {
                var q = this.database.InvoiceLines.Where(il=>il.SalesTransaction.NozzleId == this.NozzleId).Select(il=>il.Invoice).
                    Where(i => i.TransactionDate.Date <= dateTo && i.TransactionDate >= dateFrom);
                if (invType.InvoiceTypeId != Guid.Empty)
                    q = q.Where(i => i.InvoiceTypeId == invType.InvoiceTypeId);
                if (number > 0)
                    q = q.Where(i => i.Number == number);
                this.invoiceBindingSource.DataSource = q.OrderByDescending(i => i.TransactionDate);
            }
            else
            {
                var q = this.database.Invoices.Where(i => i.TransactionDate.Date <= dateTo && i.TransactionDate >= dateFrom);
                if (invType.InvoiceTypeId != Guid.Empty)
                    q = q.Where(i => i.InvoiceTypeId == invType.InvoiceTypeId);
                if (number > 0)
                    q = q.Where(i => i.Number == number);
                this.invoiceBindingSource.DataSource = q.OrderByDescending(i => i.TransactionDate);
            }
                

            
            this.invoiceBindingSource.ResetBindings(false);
        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            this.LoadData();
        }

        private void radButton2_Click(object sender, EventArgs e)
        {
            this.SelectedInvoice = null;
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        }

        private void radButton3_Click(object sender, EventArgs e)
        {
            if(this.invoiceBindingSource.Position >= 0)
                this.SelectedInvoice = this.invoiceBindingSource.Current as Data.Invoice;
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
        }
    }
}
