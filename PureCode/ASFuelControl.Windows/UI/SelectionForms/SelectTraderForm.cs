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
    }
}
