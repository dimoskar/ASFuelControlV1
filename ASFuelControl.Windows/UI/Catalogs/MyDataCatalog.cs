using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using FiscalPanel = ASFuelControl.Windows.UI.Controls.FiscalDocumentPanel;

namespace ASFuelControl.Windows.UI.Catalogs
{
    public partial class MyDataCatalog : UserControl
    {
        private DateTime dateFrom;
        private DateTime dateTo;
        private string filter = "";
        private readonly bool myDieselVisible;
        private readonly FiscalPanel fiscalPanel = new FiscalPanel { Dock = DockStyle.Bottom, Height = 220 };

        public MyDataCatalog()
        {
            InitializeComponent();
            myDieselVisible = Data.Implementation.OptionHandler.Instance.MyDieselEnabled;
            this.Controls.Remove(errorsTextBox);
            this.Controls.Add(fiscalPanel);
            this.Controls.SetChildIndex(fiscalPanel,1);
            this.radLabel1.Text = "Φίλτρο";
            this.invoiceRadGridView.AutoGenerateColumns = false;
            this.invoiceRadGridView.Columns.Clear();
            var fields = new List<string> { "Description","TransactionDate","TraderName","Route","Signing","Payment","Submission","Printing","Mark" };
            var titles = new List<string> { "Παραστατικό","Ημερομηνία (έως 1000)","Συναλλασσόμενος","Διαδρομή","Σήμανση","Πληρωμή","Διαβίβαση","Εκτύπωση","MARK" };
            if (myDieselVisible)
            {
                fields.AddRange(new[] { "MyDieselCoupon","MyDieselStatus","MyDieselIntervention" });
                titles.AddRange(new[] { "Κουπόνι myDiesel","Κατάσταση myDiesel","Χρειάζεται παρέμβαση" });
            }
            for (int i = 0; i < fields.Count; i++) this.invoiceRadGridView.Columns.Add(new Telerik.WinControls.UI.GridViewTextBoxColumn(fields[i]) { HeaderText = titles[i], Width = i == 0 || i == 2 ? 180 : 130, ReadOnly = true });
            dateFrom = DateTime.Today;
            dateTo = DateTime.Today;
            this.radDateTimePicker1.Value = dateFrom;
            this.radDateTimePicker2.Value = dateTo;
            this.invoiceRadGridView.CurrentRowChanged += InvoiceRadGridView_CurrentRowChanged;
            this.invoiceRadGridView.CellDoubleClick += (s,e) => {
                if (!myDieselVisible) return;
                var row = this.invoiceRadGridView.CurrentRow?.DataBoundItem as MyDataInvoice;
                if (row == null || string.IsNullOrWhiteSpace(row.MyDieselCoupon)) return;
                using (var form = new ASFuelControl.Windows.UI.Forms.MyDieselEvidenceForm(row.InvoiceId)) form.ShowDialog(this);
            };
            fiscalPanel.ActionCompleted += (s,e) => {
                try { this.Load(); } catch (Exception ex) { Telerik.WinControls.RadMessageBox.Show(ex.Message,"Κατάλογος τιμολόγησης"); }
            };
        }

        private void InvoiceRadGridView_CurrentRowChanged(object sender, Telerik.WinControls.UI.CurrentRowChangedEventArgs e)
        {
            if (e.CurrentRow == null)
                fiscalPanel.LoadInvoice(Guid.Empty);
            else
            {
                var invoice = e.CurrentRow.DataBoundItem as MyDataInvoice;
                if(invoice == null) { fiscalPanel.LoadInvoice(Guid.Empty); return; }
                this.errorsTextBox.Lines = invoice.Errors;
                fiscalPanel.LoadInvoice(invoice.InvoiceId);
                fiscalPanel.ShowLegacyErrors(invoice.Errors);
            }
        }

        private void Load()
        {
            var selected = (invoiceRadGridView.CurrentRow?.DataBoundItem as MyDataInvoice)?.InvoiceId;
            var store = new ASFuelControl.Invoicing.Persistence.SqlFiscalCatalogStore(
                new ASFuelControl.Invoicing.Persistence.SqlFiscalDatabase(Properties.Settings.Default.DBConnection));
            var rows = store.Load(dateFrom.Date,dateTo.Date.AddDays(1),filter);
            this.invoiceRadGridView.DataSource = rows.Select(row => new MyDataInvoice {
                InvoiceId = row.InvoiceId, TransactionDate = row.Date, InvoiceDesc = row.Description,
                Number = row.Number, Series = row.Series, Trader = row.Trader, Mark = row.Mark,
                MyDataInvoiceId = row.LegacyId ?? Guid.Empty, Status = row.LegacyStatus,
                Errors = new[] { row.LegacyErrors ?? "" }, Owned = row.Owned,
                Route = row.Route == "Ilyda.Direct" ? "Ilyda" : row.Route,
                Signing = FiscalPanel.StateLabel(row.Signing) ?? "—", Payment = FiscalPanel.StateLabel(row.Payment) ?? "—",
                Submission = FiscalPanel.StateLabel(row.Submission) ?? (row.LegacyStatus == 3 ? "Παλαιό: αποδεκτό" : "—"),
                Printing = FiscalPanel.StateLabel(row.Printing) ?? "—", MyDieselCoupon = row.MyDieselCouponCode,
                MyDieselStatus = string.IsNullOrWhiteSpace(row.MyDieselStatus) ? "—" : ASFuelControl.Windows.UI.Forms.MyDieselEvidenceForm.StageLabel(row.MyDieselStatus),
                MyDieselIntervention = row.MyDieselIntervention ? "Ναι" : ""
            }).ToArray();
            if (selected.HasValue)
                invoiceRadGridView.CurrentRow = invoiceRadGridView.Rows.FirstOrDefault(r => (r.DataBoundItem as MyDataInvoice)?.InvoiceId == selected.Value);
        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            this.dateFrom = this.radDateTimePicker1.Value;
            this.dateTo = this.radDateTimePicker2.Value;
            this.filter = this.radTextBox1.Text == null ? "" : this.radTextBox1.Text;
            try { this.Load(); }
            catch (Exception ex) { Telerik.WinControls.RadMessageBox.Show(ex.Message,"Κατάλογος τιμολόγησης"); }
        }

        private void resendToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.invoiceRadGridView.CurrentRow == null)
                return;
            var invoice = this.invoiceRadGridView.CurrentRow.DataBoundItem as MyDataInvoice;
            if (invoice == null)
                return;

            if (invoice.Owned)
            {
                Telerik.WinControls.RadMessageBox.Show("Χρησιμοποιήστε τις ξεχωριστές ενέργειες ανάκτησης και εκτύπωσης στο κάτω πλαίσιο.","Κοινή τιμολόγηση");
                return;
            }
            if (invoice.MyDataInvoiceId == Guid.Empty)
            {
                Telerik.WinControls.RadMessageBox.Show("Η αρχική έκδοση γίνεται από τη φόρμα παραστατικού.","Τιμολόγηση");
                return;
            }

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
            if (item.Owned || item.MyDataInvoiceId == Guid.Empty) { this.radMenuItem1.Enabled = false; this.radMenuItem2.Enabled = false; return; }
            this.radMenuItem1.Enabled = true;
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
            if (invoice == null || invoice.Owned) return;
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
        public bool Owned { set; get; }
        public string Route { set; get; }
        public string Signing { set; get; }
        public string Payment { set; get; }
        public string Submission { set; get; }
        public string Printing { set; get; }
        public string MyDieselCoupon { set; get; }
        public string MyDieselStatus { set; get; }
        public string MyDieselIntervention { set; get; }
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

