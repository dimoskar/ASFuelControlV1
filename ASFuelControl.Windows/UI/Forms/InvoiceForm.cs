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

namespace ASFuelControl.Windows.UI.Forms
{
    public partial class InvoiceForm : RadForm
    {
        Data.DatabaseModel database = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
        Data.Invoice currentInvoice = null;

        public Data.Invoice CurrentInvoice 
        {
            get { return this.currentInvoice; }
        }

        public InvoiceForm()
        {
            InitializeComponent();

            this.database.Events.Changed += new Telerik.OpenAccess.ChangeEventHandler(Events_Changed);
            this.invoiceTypeDropDownList.DataSource = database.InvoiceTypes.OrderBy(f => f.Description);
            this.invoiceTypeDropDownList.DisplayMember = "Description";
            this.invoiceTypeDropDownList.ValueMember = "InvoiceTypeId";

            this.radButton2.DataBindings.Add(new System.Windows.Forms.Binding("Enabled", this.database, "DatabaseChanged", true));

            GridViewComboBoxColumn col = this.invoiceLinesRadGridView.Columns["FuelTypeColumn"] as GridViewComboBoxColumn;
            col.DataSource = this.database.FuelTypes.ToList().OrderBy(t => t.Name);
            col.DisplayMember = "Name";
            col.ValueMember = "FuelTypeId";

            this.traderIdLabel1.DataBindings.Add(new Binding("Text", this.invoiceBindingSource, "Trader.Name", true));
            this.vehicleIdLabel1.DataBindings.Add(new Binding("Text", this.invoiceBindingSource, "Vehicle.PlateNumber", true));

            GridViewComboBoxColumn cbx = this.invoiceLinesRadGridView.Columns["TankColumn"] as GridViewComboBoxColumn;
            cbx.DataSource = this.database.Tanks.OrderBy(t => t.TankNumber).ToList();
            cbx.DisplayMember = "Description";
            cbx.ValueMember = "TankId";
            this.Disposed += InvoiceForm_Disposed;
        }

        private void InvoiceForm_Disposed(object sender, EventArgs e)
        {
            this.database.Dispose();
        }

        bool suspendAfterUpdateEvent = false;
        void Events_Changed(object sender, Telerik.OpenAccess.ChangeEventArgs e)
        {

            if(e.PersistentObject.GetType() == typeof(Data.InvoiceLine))
            {
                Data.InvoiceLine invL = e.PersistentObject as Data.InvoiceLine;
                if ((e.PropertyName == "UnitPrice" || e.PropertyName == "VolumeNormalized" || e.PropertyName == "DiscountAmount") && 
                    invL.Invoice.InvoiceType != null && invL.Invoice.InvoiceType.TransactionType == 1)
                {

                    suspendAfterUpdateEvent = true;
                    Data.InvoiceLine invLine = e.PersistentObject as Data.InvoiceLine;
                    invLine.TotalPrice = invLine.UnitPrice * invLine.VolumeNormalized - invLine.DiscountAmount;
                    invLine.VatPercentage = Data.Implementation.OptionHandler.Instance.GetDecimalOption("VATValue", 23);
                    invLine.VatAmount = invLine.TotalPrice * invLine.VatPercentage / 100;
                    if (invLine.Invoice != null)
                    {
                        decimal netto = 0;
                        decimal total = 0;
                        decimal vat = 0;
                        foreach (Data.InvoiceLine il in invLine.Invoice.InvoiceLines)
                        {
                            netto = netto + il.TotalPrice;
                            total = netto + il.VatAmount;
                            vat = vat + il.VatAmount;
                        }
                        invLine.Invoice.NettoAmount = decimal.Round(netto, 2);
                        invLine.Invoice.VatAmount = decimal.Round(vat, 2);
                        invLine.Invoice.TotalAmount = netto + vat;
                    }

                    suspendAfterUpdateEvent = false;
                }
                if (e.PropertyName == "VolumeNormalized" || e.PropertyName == "Volume" || e.PropertyName == "FuelDensity" || e.PropertyName == "Temperature")
                {
                    Data.InvoiceLine invLine = e.PersistentObject as Data.InvoiceLine;
                    if (invLine.Volume == 0 || invLine.VolumeNormalized == 0 || invLine.FuelType == null || invLine.FuelDensity == 0)
                        return;
                    decimal vol = invLine.FuelType.NormalizeVolume(invLine.Volume, invLine.Temperature, invLine.FuelDensity);
                }
            }
            else if(e.PersistentObject.GetType() == typeof(Data.Invoice))
            {
                Data.Invoice inv = e.PersistentObject as Data.Invoice;
                if (e.PropertyName == "InvoiceTypeId")
                {
                    Data.InvoiceType invType = this.database.InvoiceTypes.Where(it=>it.InvoiceTypeId == inv.InvoiceTypeId).FirstOrDefault();
                    if(invType == null)
                        return;
                    if(invType.IsInternal.HasValue && invType.IsInternal.Value)
                    {
                        this.invoiceLinesRadGridView.Columns["TankColumn"].IsVisible = true;
                    }
                    else
                        this.invoiceLinesRadGridView.Columns["TankColumn"].IsVisible = false;
                }
            }
        }

        public void AddNewInvoice()
        {
            ExecuteWithInvoiceTypeBindingReadOnly(() =>
            {
                this.currentInvoice = new Data.Invoice();
                this.currentInvoice.InvoiceId = Guid.NewGuid();
                this.database.Add(currentInvoice);
                this.currentInvoice.TransactionDate = DateTime.Now;
                this.currentInvoice.NettoAmount = 0;
                this.currentInvoice.ApplicationUserId = Data.DatabaseModel.CurrentUserId;
                this.currentInvoice.PaymentType = (int)Common.Enumerators.PaymentTypeEnum.Cash;
                this.currentInvoice.TotalAmount = 0;
                this.currentInvoice.VatAmount = 0;
                this.currentInvoice.Number = 1;
                this.invoiceBindingSource.DataSource = this.currentInvoice;
                SynchronizeInvoiceTypeSelection();
            });
        }

        public void LoadInvoice(Guid invoiceId)
        {
            ExecuteWithInvoiceTypeBindingReadOnly(() =>
            {
                this.currentInvoice = this.database.Invoices.Where(
                    invoiceGrid => invoiceGrid.InvoiceId == invoiceId).FirstOrDefault();
                this.invoiceBindingSource.DataSource = this.currentInvoice;
                SynchronizeInvoiceTypeSelection();
            });
            if (this.currentInvoice == null)
                return;
            if (this.currentInvoice.IsPrinted.HasValue && this.currentInvoice.IsPrinted.Value)
            {
                this.panel1.Enabled = false;
                this.panel2.Enabled = false;
                this.panel3.Enabled = false;
                this.invoiceLinesRadGridView.ReadOnly = true;
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (this.currentInvoice.InvoiceTypeId == Guid.Empty)
            {
                Telerik.WinControls.RadMessageBox.Show("Δεν έχετε επιλέξει Τύπο Παραστατικού", "Σφάλμα Επιλογής", MessageBoxButtons.OK, Telerik.WinControls.RadMessageIcon.Exclamation);
                return;
            }
            if (!this.currentInvoice.TraderId.HasValue)
            {
                Telerik.WinControls.RadMessageBox.Show("Δεν έχετε επιλέξει Συναλλασσόμενο", "Σφάλμα Επιλογής", MessageBoxButtons.OK, Telerik.WinControls.RadMessageIcon.Exclamation);
                return;
            }
            Data.InvoiceLine invLine = new Data.InvoiceLine();
            invLine.InvoiceLineId = Guid.NewGuid();
            this.database.Add(invLine);
            invLine.InvoiceId = this.currentInvoice.InvoiceId;
            invLine.Invoice = this.currentInvoice;
            this.currentInvoice.InvoiceLines.Add(invLine);
            ResetBindingsPreservingInvoiceType();
        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            using (SelectionForms.SelectTraderForm stf = new SelectionForms.SelectTraderForm())
            {
                stf.SelectSuppliers = true;
                stf.ShowDialog(this);
                if (stf.SelectedVehicle != null)
                {
                    Data.Vehicle vehiucle = this.database.Vehicles.Where(v => v.VehicleId == stf.SelectedVehicle.VehicleId).FirstOrDefault();
                    if (vehicleIdLabel1 == null)
                        return;
                    this.currentInvoice.TraderId = vehiucle.TraderId;
                    this.currentInvoice.TraderId = vehiucle.TraderId;
                    this.currentInvoice.Trader = vehiucle.Trader;
                    this.currentInvoice.Vehicle = vehiucle;

                    if (this.currentInvoice.InvoiceTypeId == Guid.Empty && vehiucle.Trader.InvoiceTypeId.HasValue)
                    {
                        this.currentInvoice.InvoiceTypeId = vehiucle.Trader.InvoiceTypeId.Value;
                    }
                }
            }
        }

        private void radButton2_Click(object sender, EventArgs e)
        {
            if (!ValidateLookupSelections(true))
                return;
            this.database.SaveChanges();

        }

        private void InvoiceForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!this.database.DatabaseChanged)
                return;
            DialogResult res = Telerik.WinControls.RadMessageBox.Show("Θέλετε να αποθηκευτούν οι αλλαγές που κάνατε;", "Έγιναν αλλαγές", MessageBoxButtons.YesNoCancel, Telerik.WinControls.RadMessageIcon.Question);
            if (res == System.Windows.Forms.DialogResult.No)
                return;
            else if (res == System.Windows.Forms.DialogResult.Yes)
            {
                if (!ValidateLookupSelections(true))
                {
                    e.Cancel = true;
                    return;
                }
                this.database.SaveChanges();
                return;
            }
            e.Cancel = true;
        }

        private void radButton3_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void invoiceLinesRadGridView_CellValidating(object sender, CellValidatingEventArgs e)
        {
            var currentRow = this.invoiceLinesRadGridView.CurrentRow;
            var cell = e.Row.Cells[e.ColumnIndex];

            if (invoiceLinesRadGridView.Columns[e.ColumnIndex].Name == "FuelDensity")
            {
                Data.InvoiceLine invLine = currentRow.DataBoundItem as Data.InvoiceLine;
                if (invLine == null || invLine.FuelTypeId == Guid.Empty)
                {
                    Telerik.WinControls.RadMessageBox.Show("Δεν έχετε επιλέξει Τύπο Καυσίμου", "Σφάλμα Επιλογής", MessageBoxButtons.OK, Telerik.WinControls.RadMessageIcon.Exclamation);
                    
                    e.Cancel = false;
                    cell.Value = 0;
                    cell.EndEdit();
                }
                else
                {
                    if (e.Value == null || Convert.ToDecimal(e.Value) <= invLine.FuelType.BaseDensity - 100 || Convert.ToDecimal(e.Value) >= invLine.FuelType.BaseDensity + 100)
                    {
                        Telerik.WinControls.RadMessageBox.Show(string.Format("Σφάλμα Πυκνότητας. Η τιμή στο πεδίο πυκνότητα πρέπει να είναι ανάμεσα στο {0} και το {1}", invLine.FuelType.BaseDensity - 100, invLine.FuelType.BaseDensity + 100), "Σφάλμα Καταχώρησης", MessageBoxButtons.OK, Telerik.WinControls.RadMessageIcon.Error);
                        e.Cancel = true;
                        cell.ErrorText = "Σφάλμα!";
                    }
                    else
                    {
                        cell.ErrorText = string.Empty;
                    }
                }
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (this.invoiceLinesBindingSource.Position < 0)
                return;
            Data.InvoiceLine il = this.invoiceLinesBindingSource.Current as Data.InvoiceLine;
            if (il == null)
                return;

            if (RadMessageBox.Show("Θέλετε να διαγράψετε την επιλεγμένη εγγραφή;", "Διαγραφή..", MessageBoxButtons.YesNo, RadMessageIcon.Exclamation) != DialogResult.Yes)
                return;
            if (il.TankFillingId.HasValue)
            {
                RadMessageBox.Show("Η εγγραφή συνδέεται με παραλαβή καυσίμου.\r\nΔεν μπορεί να γίνει διαγραφή.", "Σφάλμα Διαγραφής", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
                return;
            }
            if (il.SaleTransactionId.HasValue)
            {
                RadMessageBox.Show("Η εγγραφή συνδέεται με πώληση καυσίμου.\r\nΔεν μπορεί να γίνει διαγραφή.", "Σφάλμα Διαγραφής", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
                return;
            }
            
            il.Invoice.InvoiceLines.Remove(il);
            this.database.Delete(il);
            ResetBindingsPreservingInvoiceType();
        }

        private void ExecuteWithInvoiceTypeBindingReadOnly(Action action)
        {
            var binding = this.invoiceTypeDropDownList.DataBindings["SelectedValue"];
            var mode = binding == null ? DataSourceUpdateMode.Never :
                binding.DataSourceUpdateMode;
            try
            {
                if (binding != null)
                    binding.DataSourceUpdateMode = DataSourceUpdateMode.Never;
                action();
            }
            finally
            {
                if (binding != null)
                    binding.DataSourceUpdateMode = mode;
            }
        }

        private void ResetBindingsPreservingInvoiceType()
        {
            var invoiceTypeId = this.currentInvoice == null ? Guid.Empty :
                this.currentInvoice.InvoiceTypeId;
            ExecuteWithInvoiceTypeBindingReadOnly(() =>
            {
                this.invoiceBindingSource.ResetBindings(false);
                this.invoiceLinesBindingSource.ResetBindings(false);
                if (this.currentInvoice != null &&
                    this.currentInvoice.InvoiceTypeId != invoiceTypeId)
                    this.currentInvoice.InvoiceTypeId = invoiceTypeId;
                SynchronizeInvoiceTypeSelection();
            });
        }

        private void SynchronizeInvoiceTypeSelection()
        {
            if (this.currentInvoice == null ||
                this.currentInvoice.InvoiceTypeId == Guid.Empty)
                this.invoiceTypeDropDownList.SelectedIndex = -1;
            else
                this.invoiceTypeDropDownList.SelectedValue =
                    this.currentInvoice.InvoiceTypeId;
        }

        private bool ValidateLookupSelections(bool showMessage)
        {
            if (this.currentInvoice == null)
                return false;
            var errors = new List<string>();
            if (this.currentInvoice.InvoiceTypeId == Guid.Empty ||
                !this.database.InvoiceTypes.Any(item =>
                    item.InvoiceTypeId == this.currentInvoice.InvoiceTypeId) ||
                !object.Equals(this.invoiceTypeDropDownList.SelectedValue,
                    this.currentInvoice.InvoiceTypeId))
                errors.Add("Δεν έχει επιλεγεί έγκυρος τύπος παραστατικού.");
            if (!this.currentInvoice.PaymentType.HasValue ||
                !Enum.IsDefined(typeof(Common.Enumerators.PaymentTypeEnum),
                    this.currentInvoice.PaymentType.Value))
                errors.Add("Δεν έχει επιλεγεί έγκυρος τρόπος πληρωμής.");

            int index = 0;
            foreach (var line in this.currentInvoice.InvoiceLines)
            {
                index++;
                string prefix = "Γραμμή " + index.ToString() + ": ";
                var fuelType = this.database.FuelTypes.FirstOrDefault(
                    item => item.FuelTypeId == line.FuelTypeId);
                if (line.FuelTypeId == Guid.Empty || fuelType == null)
                    errors.Add(prefix + "δεν έχει επιλεγεί έγκυρο καύσιμο.");
                if (line.TankId.HasValue && line.TankId.Value != Guid.Empty)
                {
                    var tank = this.database.Tanks.FirstOrDefault(
                        item => item.TankId == line.TankId.Value);
                    if (tank == null)
                        errors.Add(prefix + "δεν έχει επιλεγεί έγκυρη δεξαμενή.");
                    else if (tank.FuelTypeId != line.FuelTypeId)
                        errors.Add(prefix + "η δεξαμενή δεν αντιστοιχεί στο καύσιμο.");
                }
            }

            var distinctErrors = errors.Distinct().ToArray();
            if (distinctErrors.Length == 0)
                return true;
            if (showMessage)
                RadMessageBox.Show(
                    "Η αποθήκευση ακυρώθηκε επειδή υπάρχουν ασυνεπείς επιλογές:\r\n\r\n" +
                    string.Join("\r\n", distinctErrors.Select(error => "• " + error)),
                    "Έλεγχος επιλογών παραστατικού", MessageBoxButtons.OK,
                    RadMessageIcon.Error);
            return false;
        }
    }
}
