using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ASFuelControl.Windows.UI.Catalogs
{
    public partial class MyDataCatalog : UserControl
    {
        private DateTime dateFrom;
        private DateTime dateTo;
        private string filter = "";

        public MyDataCatalog()
        {
            InitializeComponent();
            dateFrom = DateTime.Today;
            dateTo = DateTime.Today;
            this.radDateTimePicker1.Value = dateFrom;
            this.radDateTimePicker2.Value = dateTo;
            this.invoiceRadGridView.CurrentRowChanged += InvoiceRadGridView_CurrentRowChanged;
        }

        private void InvoiceRadGridView_CurrentRowChanged(object sender, Telerik.WinControls.UI.CurrentRowChangedEventArgs e)
        {
            if (e.CurrentRow == null)
                this.errorsTextBox.Text = "";
            else
            {
                var invoice = e.CurrentRow.DataBoundItem as MyDataInvoice;
                if(invoice == null)
                    this.errorsTextBox.Text = "";
                this.errorsTextBox.Lines = invoice.Errors;
            }
        }

        private void Load()
        {
            List<MyDataInvoice> invoices = new List<MyDataInvoice>();
            using (System.Data.SqlClient.SqlConnection connection = new System.Data.SqlClient.SqlConnection(Properties.Settings.Default.DBConnection))
            {
                connection.Open();
                string sql = Properties.Resources.Command_MyDataInvoice.
                    Replace("[DateFrom]", dateFrom.ToString("yyyy/MM/dd")).
                    Replace("[DateTo]", dateTo.ToString("yyyy/MM/dd")).
                    Replace("[Filter]", this.filter);
                System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand(sql, connection);
                var reader = cmd.ExecuteReader();
                while (true)
                {
                    bool hasRows = reader.Read();
                    if (!hasRows)
                        break;
                    try
                    {
                        MyDataInvoice myInv = new MyDataInvoice();

                        myInv.TransactionDate = reader.GetDateTime(0);
                        myInv.Number = reader.GetInt32(1);
                        myInv.Series = reader.IsDBNull(2) ? null : reader.GetString(2);
                        myInv.Trader = reader.IsDBNull(3) ? null : reader.GetString(3);
                        myInv.InvoiceDesc = reader.GetString(4);
                        myInv.Mark = reader.IsDBNull(5) ? null : (long?)reader.GetInt64(5);
                        myInv.InvoiceId = reader.GetGuid(6);
                        myInv.DateTimeSent = reader.IsDBNull(7) ? null : (DateTime?)reader.GetDateTime(7);
                        myInv.Status = reader.GetInt32(8);
                        myInv.CanceledByMark = reader.IsDBNull(9) ? null : (long?)reader.GetInt64(9);
                        myInv.CancelationMark = reader.IsDBNull(10) ? null : (long?)reader.GetInt64(10);
                        myInv.MyDataInvoiceId = reader.GetGuid(11);
                        myInv.UId = reader.IsDBNull(12) ? null : reader.GetString(12);
                        myInv.Vehicle = reader.IsDBNull(13) ? null : reader.GetString(13);
                        string errors = reader.IsDBNull(14) ? "" : reader.GetString(14);
                        if(!string.IsNullOrEmpty(errors))
                        {
                            var errorList = errors.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                            myInv.Errors = errorList;
                        }
                        else
                            myInv.Errors = new string[] { };
                        invoices.Add(myInv);
                    }
                    catch(Exception ex)
                    {
                        MyDataInvoice myInv = new MyDataInvoice();
                        myInv.InvoiceDesc = "Error loading data";
                        invoices.Add(myInv);
                    }
                }
            }
            this.invoiceRadGridView.DataSource = invoices;
        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            this.dateFrom = this.radDateTimePicker1.Value;
            this.dateTo = this.radDateTimePicker2.Value;
            this.filter = this.radTextBox1.Text == null ? "" : this.radTextBox1.Text;
            this.Load();
        }

        private void resendToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.invoiceRadGridView.CurrentRow == null)
                return;
            var invoice = this.invoiceRadGridView.CurrentRow.DataBoundItem as MyDataInvoice;
            if (invoice == null)
                return;

            Threads.PrintAgent pa = new Threads.PrintAgent();
            pa.SendMyDataToPrint(invoice.InvoiceId);
            this.Load();

            //if (invoice.Status == 3)
            //{
            //    Telerik.WinControls.RadMessageBox.Show("Δεν είναι δυνατή η επαναλληψή αποστολής παραστατικών με \r\nStatus = 3 (Επιτυχής Αποστολή).", "Προσοχή...", MessageBoxButtons.OK, Telerik.WinControls.RadMessageIcon.Exclamation);
            //    return;
            //}
            //else if (invoice.Status == 0 || invoice.Status == 1)
            //{
            //    Telerik.WinControls.RadMessageBox.Show("Το παραστατικό είναι ήδη προγραμματισμένο για επανάληψη αποστολής", "Προσοχή...", MessageBoxButtons.OK, Telerik.WinControls.RadMessageIcon.Exclamation);
            //    return;
            //}
            //if (Telerik.WinControls.RadMessageBox.Show("Να γίνει επαναλληψή αποστολής του ςπιλεγμένου παραστατικού;\r\n" + invoice.Description, "Προσοχή...", MessageBoxButtons.YesNo, Telerik.WinControls.RadMessageIcon.Exclamation) == DialogResult.No)
            //    return;

            //using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
            //{
            //    var mdInvoice = db.MyDataInvoices.FirstOrDefault(i => i.MyDataInvoiceId == invoice.MyDataInvoiceId);
            //    if (mdInvoice == null)
            //        return;
            //    mdInvoice.Status = 0;
            //    db.SaveChanges();
            //    this.Load();
            //}
        }

        private void invoiceRadGridView_ContextMenuOpening(object sender, Telerik.WinControls.UI.ContextMenuOpeningEventArgs e)
        {
            e.Cancel = true;
        }

        private void radContextMenu1_DropDownOpening(object sender, CancelEventArgs e)
        {
            if (this.invoiceRadGridView.CurrentRow == null)
                return;
            var item = this.invoiceRadGridView.CurrentRow.DataBoundItem as MyDataInvoice;
            if (item == null)
                return;
            if (item.Status == 3)
            {
                //this.radMenuItem1.Enabled = false;
                this.radMenuItem2.Enabled = true;
            }
            else
            {
                //this.radMenuItem1.Enabled = true;
                this.radMenuItem2.Enabled = false;
            }
        }

        private void radMenuItem2_Click(object sender, EventArgs e)
        {
            if (this.invoiceRadGridView.CurrentRow == null)
                return;
            var invoice = this.invoiceRadGridView.CurrentRow.DataBoundItem as MyDataInvoice;
            using (var db = new Data.DatabaseModel())
            {
                var invoiceToCancel = db.MyDataInvoices.FirstOrDefault(m => m.MyDataInvoiceId == invoice.MyDataInvoiceId);
                if (invoiceToCancel == null)
                    return;
                Threads.PrintAgent.CancelInvoice(db, invoiceToCancel);
                this.Load();
            }
        }
    }

    public class MyDataInvoice
    {
        public string Description
        {
            get
            {
                return string.Format("{0} {2}{1}", this.InvoiceDesc, Number, Series);
            }
        }

        public string TraderName
        {
            get
            {
                return this.Trader;
            }
        }

        public string InvoiceDesc { set; get; }
        public int Number { set; get; }
        public string Series { set; get; }
        public string Trader { set; get; }
        public string Vehicle { set; get; }
        public long? Mark { set; get; }
        public long? CancelationMark { set; get; }
        public long? CanceledByMark { set; get; }
        public DateTime? DateTimeSent { set; get; }
        public DateTime TransactionDate { set; get; }
        public int Status { set; get; }
        public Guid InvoiceId { set; get; }
        public Guid MyDataInvoiceId { set; get; }
        public string UId { set; get; }
        public string[] Errors { set; get; }
    }

    //  0     Invoice.TransactionDate, 
    //  1     Invoice.Number, 
    //  2     Invoice.Series, 
    //  3     Trader.Name, 
    //  4     InvoiceType.Description, 
    //  5     MyDataInvoice.Mark, 
    //  6     MyDataInvoice.InvoiceId, 
    //  7     MyDataInvoice.DateTimeSent,
    //  8     MyDataInvoice.Status, 
    //  9     MyDataInvoice.CanceledByMark, 
    //  10    MyDataInvoice.CancelationMark, 
    //  11    MyDataInvoice.MyDataInvoiceId, 
    //  12    MyDataInvoice.Uid, 
    //  13    Invoice.VehiclePlateNumber
    //  14    MyDataInvoice.Errors

}

