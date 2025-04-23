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
    public partial class DeliveryForm : RadForm
    {
        private enum FillModeEnum
        {
            Filling,
            Draining
        }

        private Data.DatabaseModel database = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
        private Guid deliveriesGuid;
        private Guid literCheckGuid;
        private Guid returnsGuid;
        private Guid vehicleId;
        private Guid traderId;
        private Guid invoiceTypeId;
        private FillModeEnum fillMode = FillModeEnum.Filling;
        private List<Data.InvoiceLine> selectedLines = new List<Data.InvoiceLine>();

        public VirtualDevices.VirtualTank[] Tanks { set; get; }

        public DeliveryForm()
        {
            InitializeComponent();
        }

        private void LoadInvoices()
        {

            DateTime dateFrom = this.radDateTimePicker1.Value;
            DateTime dateTo = this.radDateTimePicker2.Value;

            this.deliveriesGuid = Data.Implementation.OptionHandler.Instance.GetGuidOption("DeliveryCheckInvoiceType", Guid.Empty);
            this.literCheckGuid = Data.Implementation.OptionHandler.Instance.GetGuidOption("LiterCheckInvoiceType", Guid.Empty);
            this.returnsGuid = Data.Implementation.OptionHandler.Instance.GetGuidOption("ReturnInvoiceType", Guid.Empty);


            var q = this.database.Invoices.Where(i => (i.InvoiceTypeId == this.deliveriesGuid || i.InvoiceTypeId == this.literCheckGuid || i.InvoiceTypeId == this.returnsGuid) &&
                i.TransactionDate <= dateTo && i.TransactionDate >= dateFrom);

            this.invoiceBindingSource.DataSource = q.ToList();
            this.invoiceBindingSource.ResetBindings(false);
            //this.invoiceLinesBindingSource.DataSource = this.invoiceBindingSource;
            //this.invoiceLinesBindingSource.DataMember = "InvoiceLines";
        }

        private void LoadTraders()
        {
            string filterText = this.SearchTextBox.Text;

            var q = this.database.Traders.Where(t => (t.Name.Contains(filterText) || t.TaxRegistrationNumber.Contains(filterText)));
            if (q != null && q.Count() > 0)
            {
                this.traderBindingSource.DataSource = q.ToList();
                this.traderBindingSource.ResetBindings(false);
            }
        }

        private bool ValidateFillingInvoicePage()
        {
            this.selectedLines.Clear();
            foreach (GridViewRowInfo row in this.invoiceLinesRadGridView.Rows)
            {
                Data.InvoiceLine line = row.DataBoundItem as Data.InvoiceLine;
                if (line == null)
                    continue;
                if (row.Cells["IsSelected"].Value == null || !(bool)row.Cells["IsSelected"].Value)
                    continue;
                selectedLines.Add(line);
            }
            if (selectedLines.Count == 0)
                return false;
            return true;
        }

        private bool ValidateInvoicetypePage()
        {
            this.invoiceTypeId = Guid.Empty;
            this.traderId = Guid.Empty;
            this.vehicleId = Guid.Empty;

            foreach (GridViewRowInfo row in this.vehiclesRadGridView.Rows)
            {
                Data.Vehicle vehicle = row.DataBoundItem as Data.Vehicle;
                if (vehicle == null)
                    continue;
                if (row.Cells["IsSelected"].Value == null || !(bool)row.Cells["IsSelected"].Value)
                    continue;
                this.vehicleId = vehicle.VehicleId;
                this.traderId = vehicle.TraderId;
                break;
            }

            foreach (GridViewRowInfo row in this.invoiceTypeRadGridView.Rows)
            {
                Data.InvoiceType invtype = row.DataBoundItem as Data.InvoiceType;
                if (invtype == null)
                    continue;
                if (row.Cells["IsSelected"].Value == null || !(bool)row.Cells["IsSelected"].Value)
                    continue;
                this.invoiceTypeId = invtype.InvoiceTypeId;
                break;
            }

            if (this.invoiceTypeId != Guid.Empty && this.traderId != Guid.Empty && this.vehicleId != Guid.Empty)
                return true;
            return false;
        }

        private bool ValidatePage()
        {
            if (this.radWizard1.SelectedPage == this.wizardPage1)
                return true;
            else if (this.radWizard1.SelectedPage == this.wizardPage2)
            {
                return this.ValidateFillingInvoicePage();
            }
            else if (this.radWizard1.SelectedPage == this.wizardPage3)
            {
                return ValidateInvoicetypePage();
            }
            return true;
        }

        private void Addtanks(VirtualDevices.VirtualTank[] tanks)
        {
            foreach (VirtualDevices.VirtualTank tank in tanks)
            {
                UI.Controls.TankControlSmall tc = new Controls.TankControlSmall();
                tc.Tank = tank;
                this.flowLayoutPanel1.Controls.Add(tc);
            }
        }

        private void radWizard1_SelectedPageChanged(object sender, SelectedPageChangedEventArgs e)
        {
            if (e.SelectedPage == this.wizardPage2)
            {
                this.radDateTimePicker1.Value = DateTime.Today;
                this.radDateTimePicker2.Value = DateTime.Today.AddDays(1);
                this.LoadInvoices();
            }
            else if (e.SelectedPage == this.wizardPage3)
            {
                var q = this.database.InvoiceTypes.Where(i => i.TransactionType == 0 && i.InternalDeliveryDescription != null && i.InternalDeliveryDescription != "");
                this.invoiceTypeBindingSource.DataSource = q.ToArray();
            }
            else if (e.SelectedPage == this.wizardPage4)
            {
                if (this.fillMode == FillModeEnum.Filling)
                {
                    List<VirtualDevices.VirtualTank> tanks = new List<VirtualDevices.VirtualTank>();
                    foreach (VirtualDevices.VirtualTank tank in this.Tanks)
                    {
                        int c = this.selectedLines.Where(sl => sl.FuelTypeId == tank.FuelTypeId).Count();
                        if (c == 0)
                            continue;
                        tanks.Add(tank);
                    }
                    this.Addtanks(tanks.ToArray());
                }
                else
                {
                    this.Addtanks(this.Tanks);
                }
            }
            this.radWizard1.NextButton.Enabled = this.ValidatePage();
        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            this.LoadInvoices();
        }

        private void label1_Click(object sender, EventArgs e)
        {
            this.radRadioButton2.IsChecked = true;
        }

        private void label2_Click(object sender, EventArgs e)
        {
            this.radRadioButton1.IsChecked = true;
        }

        private void radRadioButton2_ToggleStateChanged(object sender, StateChangedEventArgs args)
        {
            if (args.ToggleState == Telerik.WinControls.Enumerations.ToggleState.On)
                this.fillMode = FillModeEnum.Filling;
            else
                this.fillMode = FillModeEnum.Draining;
        }

        private void radWizard1_Next(object sender, WizardCancelEventArgs e)
        {
            if (this.radWizard1.SelectedPage == wizardPage1)
            {
                e.Cancel = true;
                if (this.fillMode == FillModeEnum.Filling)
                    this.radWizard1.SelectedPage = this.wizardPage2;
                else
                    this.radWizard1.SelectedPage = this.wizardPage3;
            }
            else if (this.radWizard1.SelectedPage == wizardPage2)
            {
                e.Cancel = true;
                this.radWizard1.SelectedPage = this.wizardPage4;
            }
            else if (this.radWizard1.SelectedPage == wizardPage3)
            {
                e.Cancel = true;
                this.radWizard1.SelectedPage = this.wizardPage4;
            }
        }

        private void radWizard1_Previous(object sender, WizardCancelEventArgs e)
        {
            if (this.radWizard1.SelectedPage == wizardPage3)
            {
                e.Cancel = true;
                this.radWizard1.SelectedPage = this.wizardPage1;
            }
            else if (this.radWizard1.SelectedPage == wizardPage2)
            {
                e.Cancel = true;
                this.radWizard1.SelectedPage = this.wizardPage1;
            }
        }

        private void radButton6_Click(object sender, EventArgs e)
        {
            this.LoadTraders();
        }

        private void traderBindingSource_PositionChanged(object sender, EventArgs e)
        {

        }

        private void traderBindingSource_CurrentChanged(object sender, EventArgs e)
        {
            foreach (GridViewRowInfo row in this.invoiceTypeRadGridView.Rows)
            {
                row.Cells["IsSelected"].Value = false;
            }

            Data.Trader trader = this.traderBindingSource.Current as Data.Trader;
            if (trader == null)
                return;
            Guid invtype = trader.InvoiceTypeId.HasValue ? trader.InvoiceTypeId.Value : Guid.Empty;
            if (invtype != Guid.Empty)
            {
                foreach (GridViewRowInfo row in this.invoiceTypeRadGridView.Rows)
                {
                    Data.InvoiceType it = row.DataBoundItem as Data.InvoiceType;
                    if (it.InvoiceTypeId != invtype)
                        continue;
                    row.Cells["IsSelected"].Value = true;
                }
            }
        }

        private void invoiceLinesRadGridView_CellValueChanged(object sender, GridViewCellEventArgs e)
        {
            if (e.Column.Name == "IsSelected")
            {
                this.radWizard1.NextButton.Enabled = this.ValidatePage();
            }
        }

        private void vehiclesRadGridView_CellValueChanged(object sender, GridViewCellEventArgs e)
        {
            if (e.Column.Name == "IsSelected")
            {
                this.radWizard1.NextButton.Enabled = this.ValidatePage();
            }
        }
    }
}
