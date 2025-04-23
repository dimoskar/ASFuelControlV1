using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Telerik.WinControls.UI;

namespace ASFuelControl.Windows.UI.Catalogs
{
    public partial class Statistics : UserControl
    {
        private Data.DatabaseModel database = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);

        public Statistics()
        {
            InitializeComponent();
            this.salesViewRadGridView.GroupDescriptors[0].Aggregates.Add("Sum(Volume)");
            this.salesViewRadGridView.GroupDescriptors[0].Aggregates.Add("Sum(VolumeNormalized)");
            this.salesViewRadGridView.GroupDescriptors[0].Aggregates.Add("Sum(TotalPrice)");
            this.salesViewRadGridView.GroupDescriptors[0].Format = "Χρήστης :{1}  Όγκος :{2:N2}  Όγκος 15οC :{3:N2}  Σύνολο :{4:N2}";

            this.salesViewRadGridView.GroupDescriptors[1].Aggregates.Add("Sum(Volume)");
            this.salesViewRadGridView.GroupDescriptors[1].Aggregates.Add("Sum(VolumeNormalized)");
            this.salesViewRadGridView.GroupDescriptors[1].Aggregates.Add("Sum(TotalPrice)");
            this.salesViewRadGridView.GroupDescriptors[1].Format = "Καύσιμο :{1}  Όγκος :{2:N2}  Όγκος 15οC :{3:N2}  Σύνολο :{4:N2}";

            this.salesViewRadGridView.GroupDescriptors[2].Aggregates.Add("Sum(Volume)");
            this.salesViewRadGridView.GroupDescriptors[2].Aggregates.Add("Sum(VolumeNormalized)");
            this.salesViewRadGridView.GroupDescriptors[2].Aggregates.Add("Sum(TotalPrice)");
            this.salesViewRadGridView.GroupDescriptors[2].Format = "Αντλία :{1}  Όγκος :{2:N2}  Όγκος 15οC :{3:N2}  Σύνολο :{4:N2}";

            this.salesViewRadGridView.GroupDescriptors[3].Format = "Ακροσωλήνιο :{1}";

            this.invoiceRadGridView.GroupDescriptors[1].Aggregates.Add("Sum(NettoAmount)");
            this.invoiceRadGridView.GroupDescriptors[1].Aggregates.Add("Sum(VatAmount)");
            this.invoiceRadGridView.GroupDescriptors[1].Aggregates.Add("Sum(TotalAmount)");
            this.invoiceRadGridView.GroupDescriptors[1].Format = "{1} Ποσό :{2:N2}  ΦΠΑ :{3:N2}  Σύνολο :{4:N2}";

            this.salesDateFrom.Value = DateTime.Today;
            this.salesDateTo.Value = DateTime.Today;

            this.fillingsFrom.Value = DateTime.Today;
            this.fillingsTo.Value = DateTime.Today;

            this.invoiceDateFrom.Value = DateTime.Today;
            this.invoiceDateTo.Value = DateTime.Today;
        }

        private void RetrieveSales()
        {
            DateTime dt1 = DateTime.Today;
            DateTime dt2 = DateTime.Today;
            if (this.salesDateFrom.NullableValue.HasValue)
                dt1 = this.salesDateFrom.NullableValue.Value.Date;
            if (this.salesDateTo.NullableValue.HasValue)
                dt2 = this.salesDateFrom.NullableValue.Value.Date;

            this.salesViewBindingSource.DataSource = database.SalesViews.Where(s => s.TransactionTimeStamp.Date <= dt2 && s.TransactionTimeStamp.Date >= dt1 && s.UnitPrice > 0).OrderBy(s => s.TransactionTimeStamp);
            this.salesViewBindingSource.ResetBindings(false);
        }

        private void RetrieveTankFillings()
        {
            DateTime dt1 = DateTime.Today;
            DateTime dt2 = DateTime.Today;
            if (this.fillingsFrom.NullableValue.HasValue)
                dt1 = this.fillingsFrom.NullableValue.Value.Date;
            if (this.fillingsTo.NullableValue.HasValue)
                dt2 = this.fillingsTo.NullableValue.Value.Date;

            this.tankFillingViewBindingSource.DataSource = database.TankFillingViews.Where(s => s.EndTime.Value.Date <= dt2 && s.EndTime.Value.Date >= dt1 &&  s.Description != null).OrderBy(s => s.EndTime);
            this.tankFillingViewBindingSource.ResetBindings(false);
        }

        private void RetriveInvoices()
        {
            DateTime dt1 = DateTime.Today;
            DateTime dt2 = DateTime.Today;
            if (this.invoiceDateFrom.NullableValue.HasValue)
                dt1 = this.invoiceDateFrom.NullableValue.Value.Date;
            if (this.invoiceDateTo.NullableValue.HasValue)
                dt2 = this.invoiceDateTo.NullableValue.Value.Date;

            this.invoiceBindingSource.DataSource = database.Invoices.Where(s => s.TransactionDate.Date <= dt2 && s.TransactionDate.Date >= dt1).OrderBy(s => s.TransactionDate);
            this.invoiceBindingSource.ResetBindings(false);
        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            this.RetrieveSales();
        }

        private void radButton6_Click(object sender, EventArgs e)
        {
            this.RetriveInvoices();
        }

        private void radButton2_Click(object sender, EventArgs e)
        {
            this.salesViewRadGridView.PrintPreview();
        }

        private void radButton4_Click(object sender, EventArgs e)
        {
            this.RetrieveTankFillings();
        }

        private void radButton3_Click(object sender, EventArgs e)
        {
            this.tankFillingViewRadGridView.PrintPreview();
        }

        private void radButton5_Click(object sender, EventArgs e)
        {
            this.invoiceRadGridView.PrintPreview();
        }

        private void invoiceRadGridView_CellDoubleClick(object sender, GridViewCellEventArgs e)
        {
            Data.Invoice invoice = e.Row.DataBoundItem as Data.Invoice;
            Forms.InvoiceForm invForm = new Forms.InvoiceForm();
            invForm.LoadInvoice(invoice.InvoiceId);
            invForm.Show(this);
        }

        
    }
}
