using ASFuelControl.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FixInvoicingPrices
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            ASFuelControl.Data.Implementation.OptionHandler.ConnectionString = Properties.Settings.Default.DBConnection;
            ASFuelControl.Data.DatabaseModel.ConnectionString = Properties.Settings.Default.DBConnection;
            this.dateTimePicker1.Value = DateTime.Today.AddMonths(-1);
            this.dateTimePicker2.Value = DateTime.Today.AddDays(1).AddSeconds(-1);
            this.dataGridView1.AutoGenerateColumns = false;
            this.comboBox1.SelectedIndexChanged += ComboBox1_SelectedIndexChanged;
            GetInvoiceTypes();
        }

        private void GetInvoiceTypes()
        {
            using (var database = new DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                var invoiceTypes = database.InvoiceTypes.Where(i=>(!i.Invalidated.HasValue || !i.Invalidated.Value) && i.TransactionType == 0 && i.OfficialEnumerator == 165).ToList();
                this.comboBox1.DataSource = invoiceTypes;
                this.comboBox1.DisplayMember = "Description";
                this.comboBox1.ValueMember = "InvoiceTypeId";
            }
        }

        private void ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.comboBox1.SelectedValue == null)
                return;
            Guid invTypeId = Guid.Empty;
            if (this.comboBox1.SelectedValue.GetType() == typeof(ASFuelControl.Data.InvoiceType))
                invTypeId = ((ASFuelControl.Data.InvoiceType)this.comboBox1.SelectedValue).InvoiceTypeId;
            else
                Guid.Parse(this.comboBox1.SelectedValue.ToString());
            using (var database = new DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                var date1 = this.dateTimePicker1.Value;
                var date2 = this.dateTimePicker2.Value;
                var dbInvoices = database.Invoices.Where(i => 
                    i.InvoiceTypeId == invTypeId && 
                    i.TransactionDate.Date <= date2 && 
                    i.TransactionDate.Date >= date1 && 
                    (
                        i.ParentInvoiceRelations.Count > 0 || 
                        i.ChildInvoiceRelations.Count > 0
                    )).ToList();

                var invoices = dbInvoices.OrderBy(i=>i.Number).Select(i => Invoice.CreateInvoice(i)).ToList();
                this.dataGridView1.DataSource = invoices;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var invoices = this.dataGridView1.DataSource as List<Invoice>;
            using (var database = new DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                foreach (var invoice in invoices)
                {
                    if (!invoice.Selected)
                        continue;
                    var dbInvoice = database.Invoices.FirstOrDefault(i => i.InvoiceId == invoice.InvoiceId);
                    foreach(var parentRelation in dbInvoice.ParentInvoiceRelations)
                    {
                        var parentInvoice = parentRelation.ParentInvoice;
                        foreach(var invLine in parentInvoice.InvoiceLines)
                        {
                            invLine.SalesTransaction.UnitPrice = this.numericUpDown1.Value;
                            invLine.SalesTransaction.TotalPrice = decimal.Round(invLine.SalesTransaction.UnitPrice * invLine.SalesTransaction.Volume, 2);
                            invLine.UnitPrice = invLine.SalesTransaction.UnitPrice;
                            invLine.TotalPrice = invLine.SalesTransaction.TotalPrice;
                            invLine.VatAmount = invLine.TotalPrice - decimal.Round(invLine.TotalPrice / ((100 + invLine.VatPercentage) / 100), 2);
                        }
                    }
                }
                database.SaveChanges();
            }
        }
    }

    public class Invoice
    {
        public Guid InvoiceId { set; get; }
        public bool Selected { set; get; }
        public string Description { set; get; }
        public static Invoice CreateInvoice(ASFuelControl.Data.Invoice inv)
        {
            var invoice = new Invoice();
            invoice.InvoiceId = inv.InvoiceId;
            invoice.Selected = false;
            invoice.Description = string.Format("{1}{2}/{0:dd/MM/yyyy} - {3}", inv.TransactionDate, inv.Series, inv.Number, inv.Trader.Name);
            return invoice;
        }
    }
}
