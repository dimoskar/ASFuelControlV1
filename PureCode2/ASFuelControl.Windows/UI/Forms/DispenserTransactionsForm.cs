using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Telerik.WinControls.UI;

namespace ASFuelControl.Windows.UI.Forms
{
    public partial class DispenserTransactionsForm : RadForm
    {
        private Data.DatabaseModel database = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
        private DateTime dateFrom;
        private DateTime dateTo;

        public Guid DispenserId { set; get; }

        public DispenserTransactionsForm()
        {
            InitializeComponent();
            dateFrom = DateTime.Now.AddHours(-1);
            dateTo = DateTime.Now;

            this.radDateTimePicker1.Value = dateFrom;
            this.radDateTimePicker2.Value = dateTo;
            this.Load += new EventHandler(DispenserTransactionsForm_Load);
        }

        void DispenserTransactionsForm_Load(object sender, EventArgs e)
        {
            if (this.DispenserId == Guid.Empty)
                return;
            List<Data.Nozzle> nozzleList = new List<Data.Nozzle>();
            Data.Nozzle foo = new Data.Nozzle();
            foo.Name = "(Ολα τα ακροσωλήνια)";
            nozzleList.Add(foo);
            nozzleList.AddRange(this.database.Nozzles.Where(n=>n.DispenserId == this.DispenserId).OrderBy(n=>n.OrderId));
            this.radDropDownList1.DataSource = nozzleList;
            this.radDropDownList1.DisplayMember = "Name";
            this.radDropDownList1.ValueMember = "NozzleId";


            GridViewSummaryItem summaryItem1 = new GridViewSummaryItem("TotalVolume", "{0:N2}", GridAggregateFunction.Sum);
            GridViewSummaryItem summaryItem2 = new GridViewSummaryItem("UnitPrice", "{0:N3}", GridAggregateFunction.Avg);
            GridViewSummaryItem summaryItem3 = new GridViewSummaryItem("TotalPrice", "{0:N2}", GridAggregateFunction.Sum);
            GridViewSummaryRowItem summaryRowItem = new GridViewSummaryRowItem();
            summaryRowItem.Add(summaryItem1);
            summaryRowItem.Add(summaryItem2);
            summaryRowItem.Add(summaryItem3);

            this.salesTransactionRadGridView.SummaryRowsBottom.Add(summaryRowItem);
        }

        private void LoadData()
        {
            dateFrom = this.radDateTimePicker1.Value;
            dateTo = this.radDateTimePicker2.Value;

            List<SalesData> sales = new List<SalesData>();
            List<Data.InvoiceLine> q = this.database.InvoiceLines.Where(s => s.SalesTransaction!= null && s.SalesTransaction.Nozzle.DispenserId == this.DispenserId && s.Invoice.TransactionDate <= dateTo && s.Invoice.TransactionDate >= dateFrom).OrderByDescending(n=>n.Invoice.TransactionDate).ToList();
            
            foreach (Data.InvoiceLine sale in q)
            {
                SalesData data = new SalesData();
                data.TransactionDate = sale.Invoice.TransactionDate;
                data.InvoiceData = sale.Invoice.InvoiceType.Abbreviation + " " + sale.Invoice.Number.ToString();
                data.TotalVolume = sale.Volume;
                data.UnitPrice = sale.UnitPrice;
                data.TotalPrice = sale.TotalPrice;
                data.NozzleId = sale.SalesTransaction.NozzleId;
                data.InvoiceId = sale.InvoiceId;
                data.NozzleNumber = sale.SalesTransaction.Nozzle.Name;
                data.FuelType = sale.SalesTransaction.Nozzle.FuelType.Name;
                sales.Add(data);
            }
            if((Guid.Empty.Equals(this.radDropDownList1.SelectedValue)))
                this.salesTransactionRadGridView.DataSource = sales;
            else
            {
                Guid nid = (Guid)this.radDropDownList1.SelectedValue;
                this.salesTransactionRadGridView.DataSource = sales.Where(n=>n.NozzleId == nid).ToList();
            }
        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            this.LoadData();
        }

        private void radButton3_Click(object sender, EventArgs e)
        {
            this.salesTransactionRadGridView.Print(true);
        }

        private void salesTransactionRadGridView_CommandCellClick(object sender, EventArgs e)
        {
            string defaultTaxDevice = Data.Implementation.OptionHandler.Instance.GetOption("DefaultTaxDevice");
            if (defaultTaxDevice != "Samtec")
            {
                MessageBox.Show("Παρακαλώ εκτυπώστε το παραστατικό χρησιμοποιόντας τον φορολογικά σας μηχανισμό", "Σφάλμα Επανεκτύπωσης", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            
            GridCommandCellElement cell = sender as GridCommandCellElement;
            if (cell == null)
                return;
            SalesData sale = cell.RowElement.Data.DataBoundItem as SalesData;
            Data.Invoice invoice = this.database.Invoices.Where(i => i.InvoiceId == sale.InvoiceId).FirstOrDefault();
            if (invoice == null)
                return;
            if (invoice.InvoiceSignature == null || invoice.InvoiceSignature == "")
                return;
            
            DialogResult res = MessageBox.Show("Θέλετε να εκτύπώσετε το επιλεγμένο Παραστατικό;", "Επανεκτύπωση Παραστατικού", MessageBoxButtons.YesNo, MessageBoxIcon.Question );
            if (res == System.Windows.Forms.DialogResult.Yes)
            {
                Threads.PrintAgent.PrintInvoiceDirect(invoice);
            }
        }

        private void salesTransactionRadGridView_ViewCellFormatting(object sender, CellFormattingEventArgs e)
        {
            if(e.CellElement is GridSummaryCellElement)
            {
                e.CellElement.TextAlignment = ContentAlignment.MiddleRight;
                e.CellElement.Font = new System.Drawing.Font(e.CellElement.Font, System.Drawing.FontStyle.Bold);
            }
        }
    }

    public class SalesData
    {
        public DateTime TransactionDate { set; get; }
        public string InvoiceData { set; get; }
        public decimal TotalVolume { set; get; }
        public decimal UnitPrice { set; get; }
        public decimal TotalPrice { set; get; }
        public string NozzleNumber { set; get; }
        public string FuelType { set; get; }
        public Guid NozzleId { set; get; }
        public Guid InvoiceId { set; get; }
    }
}
