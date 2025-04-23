using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Telerik.WinControls;
using Telerik.WinControls.UI;

namespace ASFuelControl.Windows.UI.SelectionForms
{
    public partial class SelectTraderForm : RadForm
    {
        Data.DatabaseModel database = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
        BindingList<Data.Trader> traders = new BindingList<Data.Trader>();

        public bool SelectCustomers { set; get; }
        public bool SelectSuppliers { set; get; }

        public Data.Vehicle SelectedVehicle { set; get; }

        public SelectTraderForm()
        {
            InitializeComponent();
            this.vehiclesRadGridView.GridElement.RowHeight = 50;

            this.radButton1.DataBindings.Add(new System.Windows.Forms.Binding("Enabled", this.database, "DatabaseChanged", true));

            invoiceTypeDropDownList.DataSource = this.database.InvoiceTypes.OrderBy(it => it.Description);
            invoiceTypeDropDownList.DisplayMember = "Description";
            invoiceTypeDropDownList.ValueMember = "InvoiceTypeId";

            this.paymentTypeDropDownList.DataSource = Common.Enumerators.EnumToList.EnumList<Common.Enumerators.PaymentTypeEnum>();
            this.paymentTypeDropDownList.DisplayMember = "Description";
            this.paymentTypeDropDownList.ValueMember = "Value";

            //this.traders = new BindingList<Data.Trader>(this.database.Traders.ToList());
            //this.traderBindingSource.DataSource = this.traders;

            this.IsCustomer.IsChecked = true;

            this.Load += new EventHandler(SelectTraderForm_Load);
        }

        void SelectTraderForm_Load(object sender, EventArgs e)
        {
            this.IsCustomer.IsChecked = this.SelectCustomers;
            this.IsSupplier.IsChecked = this.SelectSuppliers;

            this.LoadTraders();
            this.Disposed += SelectTraderForm_Disposed;
        }

        private void SelectTraderForm_Disposed(object sender, EventArgs e)
        {
            this.database.Dispose();
        }

        private void LoadTraders()
        {
            string filterText = this.SearchTextBox.Text;
            bool isCustomer = false;
            bool isSupplier = false;
            if (this.IsCustomer.IsChecked)
                isCustomer = true;
            if (this.IsSupplier.IsChecked)
                isSupplier = true;

            var qv = this.database.Vehicles.Where(v => v.PlateNumber.Contains(filterText)).Select(v => v.Trader);


            var q = this.database.Traders.Where(t => (t.Name.Contains(filterText) || t.TaxRegistrationNumber.Contains(filterText)));
            if ((q == null || q.Count() == 0) && (qv == null || qv.Count() == 0))
            {
                this.traders.Clear();
                this.traderBindingSource.DataSource = this.traders;
                this.traderBindingSource.ResetBindings(false);
                return;
            }
            var qs = qv.Union(q);

            if (isCustomer)
                qs = qs.Where(t => t.IsCustomer).OrderBy(t => t.Name);
            else if (isSupplier)
                qs = qs.Where(t => t.IsSupplier).OrderBy(t => t.Name);
            this.traders = new BindingList<Data.Trader>(qs.ToList());
            this.traderBindingSource.DataSource = this.traders;
            this.traderBindingSource.ResetBindings(false);
            if (this.traders.Count > 0)
            {
                this.SelectTrader(this.traders[0]);
            }
        }

        private void SelectTrader(Data.Trader trader)
        {
            this.traderBindingSource.Position = this.traders.IndexOf(trader);
            this.radPageView1.SelectedPage = this.radPageViewPage2;

            //this.traderRadGridView.CurrentRow = this.traderRadGridView.Rows.Where(r=>r.DataBoundItem == trader).FirstOrDefault();
        }

        private void radButton6_Click(object sender, EventArgs e)
        {
            this.LoadTraders();
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter)
                return;
            e.Handled = true;
            this.LoadTraders();
        }

        private void vehiclesRadGridView_CommandCellClick(object sender, EventArgs e)
        {
            GridCommandCellElement cellElement = sender as GridCommandCellElement;
            if (cellElement == null)
                return;
            Data.Vehicle vehicle = cellElement.RowInfo.DataBoundItem as Data.Vehicle;
            if (vehicle == null)
                return;
            
            Telerik.OpenAccess.ObjectState state = this.database.GetState(vehicle);
            if (this.database.HasChanges)
                this.database.SaveChanges();

            this.SelectedVehicle = vehicle;

            //if ((state & Telerik.OpenAccess.ObjectState.MaskNew) == Telerik.OpenAccess.ObjectState.MaskNew || (state & Telerik.OpenAccess.ObjectState.MaskDirty) == Telerik.OpenAccess.ObjectState.MaskDirty)
            //{
            //    this.database.SaveChanges();
            //}
            this.Close();
        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            var qtu = this.database.GetChanges().GetUpdates<Data.Trader>();
            var qti = this.database.GetChanges().GetInserts<Data.Trader>();
            var qt = qtu.Union(qti);
            foreach (var tr in qt)
            {
                if (tr.TaxRegistrationNumber != null && tr.TaxRegistrationNumber.Length > 0)
                {
                    if (tr.TaxRegistrationNumber.Length < 9 || tr.TaxRegistrationNumber.Length > 17)
                    {
                        if (tr.TaxRegistrationNumber.Where(a => char.IsLetter(a)).Count() == 0)
                            continue;
                        string msg = string.Format("To ΑΦΜ του Συναλλασσόμενου {0} πρέπει\r\nνα έχει μήκος από 9 εώς 17 χαρακτήρες.", tr.Name);
                        RadMessageBox.Show(msg, "Σφάλμα καταχώρησης ΑΦΜ", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
                        return;
                    }
                }
            }
            var qvu = this.database.GetChanges().GetUpdates<Data.Vehicle>().ToArray();
            var qvi = this.database.GetChanges().GetInserts<Data.Vehicle>().ToArray();
            var qv = qvu.Union(qvi);
            foreach (var tr in qv)
            {
                if (tr.PlateNumber != null && tr.PlateNumber.Length > 0)
                {
                    if (tr.PlateNumber.Length < 6 || tr.PlateNumber.Length > 15)
                    {
                        if (tr.PlateNumber.Where(a => char.IsDigit(a)).Count() == 0)
                            continue;
                        string msg = string.Format("Ο αριθ. κυκλοφορίας του Οχήματος {0} του Συναλλασσόμενου {1} πρέπει\r\nνα έχει μήκος από 6 εώς 15 χαρακτήρες.", tr.PlateNumber, tr.Trader.Name);
                        RadMessageBox.Show(msg, "Σφάλμα καταχώρησης Οχήματος", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
                        return;
                    }
                }
            }
            this.database.SaveChanges();
        }

        private void SelectTraderForm_FormClosing(object sender, FormClosingEventArgs e)
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

        private void radButton2_Click(object sender, EventArgs e)
        {
            Data.Trader newTrader = new Data.Trader();
            newTrader.TraderId = Guid.NewGuid();
            this.database.Add(newTrader);
            newTrader.IsCustomer = true;
            newTrader.PaymentType = (int)Common.Enumerators.PaymentTypeEnum.Cash;
            this.traders.Add(newTrader);
            this.radPageView1.SelectedPage = this.radPageViewPage1;
        }

        private void radButton5_Click(object sender, EventArgs e)
        {
            Data.Trader trader = this.traderBindingSource.Current as Data.Trader;
            if (trader == null)
                return;
            Data.Vehicle newVehicle = new Data.Vehicle();
            newVehicle.VehicleId = Guid.NewGuid();
            this.database.Add(newVehicle);
            newVehicle.TraderId = trader.TraderId;
            newVehicle.Trader = trader;
            trader.Vehicles.Add(newVehicle);
            this.vehiclesBindingSource.ResetBindings(false);
        }

        private void radButton4_Click(object sender, EventArgs e)
        {
            Data.Vehicle vehicle = this.vehiclesBindingSource.Current as Data.Vehicle;
            if (vehicle == null)
                return;

            DialogResult res = Telerik.WinControls.RadMessageBox.Show(", Θέλετε να διαγράψετε την επιλεγμένη εγγραφή;", "Διαγραφή...", MessageBoxButtons.YesNo, Telerik.WinControls.RadMessageIcon.Question);
            if (res == System.Windows.Forms.DialogResult.No)
                return;

            if (vehicle.Invoices.Count > 0)
            {
                Telerik.WinControls.RadMessageBox.Show("Η εγγραφή που επιλέξατε περιέχει συνδέσεις", "Δεν επιτρέπεται η διαγραφή", MessageBoxButtons.OK, Telerik.WinControls.RadMessageIcon.Exclamation);
                return;
            }
            vehicle.Trader.Vehicles.Remove(vehicle);
            this.database.Delete(vehicle);
            this.vehiclesBindingSource.ResetBindings(false);
        }

        private void radButton3_Click(object sender, EventArgs e)
        {
            if(this.traderBindingSource.Position < 0)
                return;
            Data.Trader trader = this.traderBindingSource.Current as Data.Trader;
            if (trader == null)
                return;
            if (trader.Invoices.Count > 0)
            {
                Telerik.WinControls.RadMessageBox.Show("Η εγγραφή που επιλέξατε περιέχει συνδέσεις", "Δεν επιτρέπεται η διαγραφή", MessageBoxButtons.OK, Telerik.WinControls.RadMessageIcon.Exclamation);
                return;
            }
            this.traders.Remove(trader);
            this.traderBindingSource.ResetBindings(false);
            this.vehiclesBindingSource.ResetBindings(false);
            if (trader.Vehicles.Count > 0)
                this.database.Delete(trader.Vehicles);
            this.database.Delete(trader);
            
            
        }

        private void radButton7_Click(object sender, EventArgs e)
        {
            Data.Trader trader = this.traderBindingSource.Current as Data.Trader;
            if (trader == null)
                return;
            Data.Vehicle newVehicle = new Data.Vehicle();
            newVehicle.VehicleId = Guid.NewGuid();
            this.database.Add(newVehicle);
            newVehicle.TraderId = trader.TraderId;
            newVehicle.Trader = trader;
            newVehicle.PlateNumber = "Χωρίς Πινακίδα";
            trader.Vehicles.Add(newVehicle);
            this.vehiclesBindingSource.ResetBindings(false);
        }

        private void taxRegistrationNumberRadTextBox_Leave(object sender, EventArgs e)
        {
            
        }

        public bool ValidateAFM(string afm)
        {
            if (afm == null || afm == "")
                return false;
            if (afm == "000000000")
                return true;
            if (!char.IsNumber(afm.First()) && !char.IsNumber(afm.Skip(1).First()))
                return true;
            int _numAFM = 0;
            if (afm.Length != 9 || !int.TryParse(afm, out _numAFM))
                return false;
            else
            {
                double sum = 0;
                int iter = afm.Length - 1;
                afm.ToCharArray().Take(iter).ToList().ForEach(c =>
                {
                    sum += double.Parse(c.ToString()) * Math.Pow(2, iter);
                    iter--;
                });
                if (sum % 11 == double.Parse(afm.Substring(8)) || double.Parse(afm.Substring(8)) == 0)
                    return true;
                else
                    return false;
            }
        }

        private void taxRegistrationNumberRadTextBox_Validating(object sender, CancelEventArgs e)
        {
            Data.Trader trader = this.traderBindingSource.Current as Data.Trader;
            if (trader == null)
                return;
            if (this.ValidateAFM(this.taxRegistrationNumberRadTextBox.Text))
                return;
            e.Cancel = true;
            this.taxRegistrationNumberRadTextBox.Focus();
            RadMessageBox.Show("Το Α.Φ.Μ. που εισάγατε δεν είναι έγκυρο.\r\nΕαν δεν γνωρίζετε το A.Φ.Μ. εισάγετε 000000000", "Σφάλμα Α.Φ.Μ.", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
        }

        private void traderRadGridView_CellDoubleClick(object sender, GridViewCellEventArgs e)
        {
            
        }
    }
}
