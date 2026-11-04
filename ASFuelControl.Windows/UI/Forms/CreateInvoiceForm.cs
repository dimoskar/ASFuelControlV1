using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Telerik.WinControls.UI;
using Telerik.WinControls;

namespace ASFuelControl.Windows.UI.Forms
{
    public partial class CreateInvoiceForm : RadForm
    {
        private Guid dispenserId;
        private Guid nozzleId;
        private bool suspendDiscountChange = false;
        private bool suspendPriceChange = false;
        private bool suspendVolumeChange = false;
        private bool suspendLookupSelection = false;
        private Guid requestedNozzleId;
        private Guid requestedInvoiceTypeId;
        private Data.Dispenser currentDispenser = null;
        private Data.Nozzle currentNozzle = null;
        private Data.InvoiceType invoiceType = null;
        private Data.Vehicle currentVehicle = null;
        private Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
        private Data.Invoice invoiceToCancel;
        private Data.Invoice invoiceToReplace;

        public Guid InvoiceTypeId
        {
            get { return this.invoiceType == null ? Guid.Empty : this.invoiceType.InvoiceTypeId; }
            set
            {
                this.requestedInvoiceTypeId = value;
                ApplyRequestedInvoiceType();
            }
        }

        public Data.Invoice InvoiceToCancel
        {
            get { return this.invoiceToCancel; }
            set
            {
                try
                {
                    this.invoiceToCancel = value == null ? null :
                        this.db.Invoices.Where(i => i.InvoiceId == value.InvoiceId).FirstOrDefault();
                    if (this.invoiceToCancel != null)
                    {
                        ApplySourceSalePoint(this.invoiceToCancel);
                        this.radTextBox3.Text = this.invoiceToCancel.Description;
                        this.unitPrice1.Value = this.invoiceToCancel.InvoiceLines[0].UnitPrice;
                        this.volume1.Value = this.invoiceToCancel.InvoiceLines[0].Volume;
                        this.priceTotal1.Value = this.invoiceToCancel.TotalAmount.Value;
                        this.discount.Value = this.invoiceToCancel.PreDiscountTotal == 0 ? 0 :
                            100 * this.invoiceToCancel.DiscountAmount /
                            this.invoiceToCancel.PreDiscountTotal;
                        this.currentVehicle = this.invoiceToCancel.Vehicle;
                        this.radCheckBox2.Checked = true;
                        if (this.currentVehicle != null)
                            this.radTextBox2.Text = this.currentVehicle.PlateNumber + " - " + this.currentVehicle.Trader.Name;
                        
                    }
                }
                catch
                {
                }
            }
        }

        public Data.Invoice InvoiceToReplace
        {
            get { return this.invoiceToReplace; }
            set
            {
                if (value == null)
                    this.invoiceToReplace = null;
                else
                {
                    this.invoiceToReplace = this.db.Invoices.Where(i => i.InvoiceId == value.InvoiceId).FirstOrDefault();
                    ApplySourceSalePoint(this.invoiceToReplace);
                    this.radTextBox1.Text = invoiceToReplace.Description;
                    this.unitPrice1.Value = this.invoiceToReplace.InvoiceLines[0].UnitPrice;
                    this.volume1.Value = this.invoiceToReplace.InvoiceLines[0].Volume;
                    this.suspendPriceChange = true;
                    this.priceTotal1.Value = this.invoiceToReplace.TotalAmount.Value;
                    this.suspendPriceChange = false;
                    this.discount.Value = 100 * (this.InvoiceToReplace.DiscountAmount) / this.invoiceToReplace.PreDiscountTotal;
                    this.currentVehicle = this.invoiceToReplace.Vehicle;
                    this.radCheckBox1.Checked = true;
                    if (this.currentVehicle != null)
                        this.radTextBox2.Text = this.currentVehicle.PlateNumber + " - " + this.currentVehicle.Trader.Name;
                    
                }
            }
        }

        public Guid NozzleId
        {
            set 
            { 
                this.requestedNozzleId = value;
                ApplyRequestedNozzle();
            }
            get { return this.currentNozzle == null ? Guid.Empty : this.currentNozzle.NozzleId; }
        }

        public Guid DispenserId 
        {
            set 
            { 
                this.dispenserId = value;
                this.currentNozzle = null;
                this.currentDispenser = this.db.Dispensers.Where(d => d.DispenserId == this.dispenserId).FirstOrDefault();
                this.suspendLookupSelection = true;
                try
                {
                    this.radDropDownList1.DataSource = this.currentDispenser == null
                        ? new Data.Nozzle[0]
                        : this.currentDispenser.Nozzles.OrderBy(nozzle => nozzle.OrderId).ToArray();
                    this.radDropDownList1.DisplayMember = "Description";
                    this.radDropDownList1.ValueMember = "NozzleId";
                    this.radDropDownList1.SelectedIndex = -1;
                }
                finally
                {
                    this.suspendLookupSelection = false;
                }
                ApplyRequestedNozzle();
            }
            get { return this.dispenserId; } 
        }

        public CreateInvoiceForm()
        {
            InitializeComponent();

            PopulateInvoiceTypes();

            this.paymentTypeDropDownList.DataSource = Common.Enumerators.EnumToList.EnumList<Common.Enumerators.PaymentTypeEnum>();
            this.paymentTypeDropDownList.DisplayMember = "Description";
            this.paymentTypeDropDownList.ValueMember = "Value";

            this.paymentTypeDropDownList.SelectedValue =
                (int)Common.Enumerators.PaymentTypeEnum.Cash;

            this.radLabel11.Enabled = Program.AdminConnected;
            this.radTextBox4.Enabled = Program.AdminConnected;
            this.Disposed += CreateInvoiceForm_Disposed;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (this.invoiceToCancel != null || this.invoiceToReplace != null)
            {
                Common.Enumerators.EnumItem ignored;
                ValidateLookupSelections(out ignored, true);
            }
        }

        private void PopulateInvoiceTypes()
        {
            var allowedTypes = this.db.InvoiceTypes
                .Where(item => item.Printable &&
                    (!item.IsInternal.HasValue || !item.IsInternal.Value) &&
                    item.TransactionType == 0)
                .OrderBy(item => item.Description)
                .ToArray();
            this.suspendLookupSelection = true;
            try
            {
                this.radDropDownList2.DataSource = allowedTypes;
                this.radDropDownList2.DisplayMember = "Description";
                this.radDropDownList2.ValueMember = "InvoiceTypeId";
                this.radDropDownList2.SelectedIndex = -1;
            }
            finally
            {
                this.suspendLookupSelection = false;
            }
            ApplyRequestedInvoiceType();
        }

        private void ApplyRequestedInvoiceType()
        {
            var allowedTypes = this.radDropDownList2.DataSource as Data.InvoiceType[];
            this.invoiceType = allowedTypes == null ? null : allowedTypes
                .FirstOrDefault(item => item.InvoiceTypeId == this.requestedInvoiceTypeId);
            this.suspendLookupSelection = true;
            try
            {
                this.radDropDownList2.SelectedValue = this.invoiceType == null
                    ? null
                    : (object)this.invoiceType.InvoiceTypeId;
                if (this.invoiceType == null)
                    this.radDropDownList2.SelectedIndex = -1;
            }
            finally
            {
                this.suspendLookupSelection = false;
            }
            ApplyInvoiceTypeState();
        }

        private void ApplyRequestedNozzle()
        {
            this.currentNozzle = this.currentDispenser == null ? null :
                this.currentDispenser.Nozzles.FirstOrDefault(
                    nozzle => nozzle.NozzleId == this.requestedNozzleId);
            this.nozzleId = this.currentNozzle == null ? Guid.Empty : this.currentNozzle.NozzleId;
            this.suspendLookupSelection = true;
            try
            {
                this.radDropDownList1.SelectedValue = this.currentNozzle == null
                    ? null
                    : (object)this.currentNozzle.NozzleId;
                if (this.currentNozzle == null)
                    this.radDropDownList1.SelectedIndex = -1;
            }
            finally
            {
                this.suspendLookupSelection = false;
            }
            if (this.currentNozzle != null && this.currentNozzle.FuelType != null)
                this.unitPrice1.Value = this.currentNozzle.FuelType.CurrentPrice;
        }

        private void ApplySourceSalePoint(Data.Invoice invoice)
        {
            var line = invoice == null ? null : invoice.InvoiceLines.FirstOrDefault();
            var sale = line == null ? null : line.SalesTransaction;
            var nozzle = sale == null ? null : sale.Nozzle;
            if (nozzle == null)
            {
                this.DispenserId = Guid.Empty;
                this.requestedNozzleId = Guid.Empty;
                ApplyRequestedNozzle();
                return;
            }
            this.DispenserId = nozzle.DispenserId;
            this.NozzleId = nozzle.NozzleId;
        }

        private void CreateInvoiceForm_Disposed(object sender, EventArgs e)
        {
            this.db.Dispose();
        }

        private bool IsGroupEnabled()
        {
            if (this.radCheckBox1.Checked || this.radCheckBox2.Checked)
                return false;
            if (this.invoiceType != null && this.invoiceType.IsCancelation.HasValue && this.invoiceType.IsCancelation.Value)
                return false;
            return true;
        }

        private void ApplyInvoiceTypeState()
        {
            bool needsVehicle = this.invoiceType != null &&
                this.invoiceType.NeedsVehicle == true;
            bool isCancellation = this.invoiceType != null &&
                this.invoiceType.IsCancelation == true;
            this.radButton3.Enabled = needsVehicle;
            this.radLabel9.Enabled = needsVehicle;
            this.radCheckBox2.Enabled = isCancellation;
            this.radCheckBox1.Enabled = this.invoiceType != null && !isCancellation;
            this.radGroupBox1.Enabled = this.IsGroupEnabled();
        }

        private void radDropDownList1_SelectedIndexChanged(object sender, Telerik.WinControls.UI.Data.PositionChangedEventArgs e)
        {
            if (this.suspendLookupSelection)
                return;
            Data.Nozzle nz = this.radDropDownList1.SelectedItem == null ? null :
                this.radDropDownList1.SelectedItem.DataBoundItem as Data.Nozzle;
            if (nz == null)
            {
                this.currentNozzle = null;
                this.requestedNozzleId = Guid.Empty;
                this.nozzleId = Guid.Empty;
                return;
            }
            if (this.currentDispenser == null || nz.DispenserId != this.currentDispenser.DispenserId)
            {
                this.currentNozzle = null;
                this.requestedNozzleId = Guid.Empty;
                this.nozzleId = Guid.Empty;
                return;
            }
            this.currentNozzle = nz;
            this.requestedNozzleId = nz.NozzleId;
            this.nozzleId = nz.NozzleId;
            this.unitPrice1.Value = nz.FuelType.CurrentPrice;
        }

        private void radDropDownList2_SelectedIndexChanged(object sender, Telerik.WinControls.UI.Data.PositionChangedEventArgs e)
        {
            if (this.suspendLookupSelection)
                return;
            this.invoiceType = this.radDropDownList2.SelectedItem == null ? null :
                this.radDropDownList2.SelectedItem.DataBoundItem as Data.InvoiceType;
            this.requestedInvoiceTypeId = this.invoiceType == null
                ? Guid.Empty
                : this.invoiceType.InvoiceTypeId;
            ApplyInvoiceTypeState();
        }

        private bool ValidateLookupSelections(out Common.Enumerators.EnumItem paymentItem,
            bool showMessage)
        {
            paymentItem = this.paymentTypeDropDownList.SelectedItem == null ? null :
                this.paymentTypeDropDownList.SelectedItem.DataBoundItem as Common.Enumerators.EnumItem;
            var errors = new List<string>();

            var allowedTypes = this.radDropDownList2.DataSource as Data.InvoiceType[];
            bool validType = this.invoiceType != null && allowedTypes != null &&
                allowedTypes.Any(item => item.InvoiceTypeId == this.invoiceType.InvoiceTypeId) &&
                object.Equals(this.radDropDownList2.SelectedValue,
                    this.invoiceType.InvoiceTypeId);
            if (!validType)
                errors.Add("Δεν έχει επιλεγεί έγκυρος τύπος παραστατικού.");

            if (this.currentDispenser == null ||
                this.currentDispenser.DispenserId != this.dispenserId)
                errors.Add("Δεν έχει επιλεγεί έγκυρη αντλία.");

            bool validNozzle = this.currentDispenser != null && this.currentNozzle != null &&
                this.currentNozzle.DispenserId == this.currentDispenser.DispenserId &&
                this.currentDispenser.Nozzles.Any(
                    nozzle => nozzle.NozzleId == this.currentNozzle.NozzleId) &&
                object.Equals(this.radDropDownList1.SelectedValue,
                    this.currentNozzle.NozzleId);
            if (!validNozzle)
                errors.Add("Το ακροσωλήνιο δεν ανήκει στην επιλεγμένη αντλία.");
            else if (this.currentNozzle.FuelType == null ||
                this.currentNozzle.FuelTypeId == Guid.Empty ||
                !this.db.FuelTypes.Any(
                    fuelType => fuelType.FuelTypeId == this.currentNozzle.FuelTypeId))
                errors.Add("Το καύσιμο του ακροσωληνίου δεν είναι διαθέσιμο.");

            int paymentValue = -1;
            try
            {
                if (paymentItem != null)
                    paymentValue = Convert.ToInt32(paymentItem.Value);
            }
            catch
            {
                paymentValue = -1;
            }
            if (paymentItem == null ||
                !Enum.IsDefined(typeof(Common.Enumerators.PaymentTypeEnum), paymentValue) ||
                !object.Equals(this.paymentTypeDropDownList.SelectedValue,
                    paymentItem.Value))
                errors.Add("Δεν έχει επιλεγεί έγκυρος τρόπος πληρωμής.");

            if (errors.Count == 0)
                return true;
            if (showMessage)
            {
                RadMessageBox.Show(
                    "Η δημιουργία του παραστατικού ακυρώθηκε επειδή υπάρχουν ασυνεπείς επιλογές:\r\n\r\n" +
                    string.Join("\r\n", errors.Distinct().Select(error => "• " + error)),
                    "Έλεγχος επιλογών παραστατικού", MessageBoxButtons.OK,
                    RadMessageIcon.Error);
            }
            return false;
        }

        private void volume1_ValueChanged(object sender, EventArgs e)
        {
            if (suspendVolumeChange)
                return;
            decimal price = this.volume1.Value * this.unitPrice1.Value;
            price = price - price * discount.Value / 100;
            this.suspendPriceChange = true;
            if (this.priceTotal1.Value != price)
                this.priceTotal1.Value = price;
            this.suspendPriceChange = false;

        }

        private void priceTotal1_ValueChanged(object sender, EventArgs e)
        {
            if (suspendPriceChange)
                return;
            if (this.unitPrice1.Value == 0)
                return;

            decimal totalPrice = this.priceTotal1.Value / (1 - this.discount.Value / 100);

            decimal volume = decimal.Round(totalPrice / this.unitPrice1.Value, 2);
            this.suspendVolumeChange = true;
            if (this.volume1.Value != volume)
                this.volume1.Value = volume;
            this.suspendVolumeChange = false;

        }

        private void unitPrice1_ValueChanged(object sender, EventArgs e)
        {
            decimal volume = decimal.Round(this.priceTotal1.Value / this.unitPrice1.Value, 2);
            if (this.volume1.Value != volume)
                this.volume1.Value = volume;
        }

        private void radCheckBox1_ToggleStateChanged(object sender, StateChangedEventArgs args)
        {
            this.radButton5.Enabled = this.radCheckBox1.Checked;
            if (this.radCheckBox1.Checked)
            {
                this.radCheckBox2.Checked = false;
                this.invoiceToCancel = null;
            }
            this.radGroupBox1.Enabled = this.IsGroupEnabled();
        }

        private void radCheckBox2_ToggleStateChanged(object sender, StateChangedEventArgs args)
        {
            this.radButton4.Enabled = this.radCheckBox2.Checked;
            if (this.radCheckBox2.Checked)
            {
                this.radCheckBox1.Checked = false;
                this.invoiceToReplace = null;
            }
            this.radGroupBox1.Enabled = this.IsGroupEnabled();
        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            Common.Enumerators.EnumItem paymentItem;
            if (!ValidateLookupSelections(out paymentItem, true))
                return;

            if (this.invoiceType.IsCancelation.HasValue && this.invoiceType.IsCancelation.Value && this.invoiceToCancel == null)
            {
                RadMessageBox.Show("Δεν έχετε επιλέξει παραστατικό για ακύρωση", "Σφάλμα Επιλογής", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
                return;
            }

            if (this.invoiceType.NeedsVehicle.HasValue && this.invoiceType.NeedsVehicle.Value && this.currentVehicle == null)
            {
                RadMessageBox.Show("Δεν έχετε επιλέξει όχημα.", "Σφάλμα Επιλογής", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
                return;
            }

            if (this.invoiceType.IsCancelation.HasValue && this.invoiceType.IsCancelation.Value && this.invoiceToCancel != null)
            {
                if (invoiceToCancel.InvoiceType.IsCancelation == true &&
                    this.invoiceType.OfficialEnumerator != 215)
                {
                    RadMessageBox.Show("Δεν μπορείτε να ακυρώσετε Ακυρωτικό", "Σφάλμα Επιλογής", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
                    return;
                }
                try
                {
                    Threads.SharedInvoiceRuntime.ValidateCancellationTransformation(
                        new[] { invoiceToCancel.InvoiceId }, this.invoiceType.InvoiceTypeId);
                }
                catch (InvalidOperationException ex)
                {
                    RadMessageBox.Show(ex.Message, "Ακύρωση παραστατικού", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
                    return;
                }

                if (invoiceToCancel.ChildInvoiceRelations.Count > 0)
                {
                    if (invoiceToCancel.ChildInvoiceRelations[0].RelationType == (int)Common.Enumerators.InvoiceRelationTypeEnum.Cancel)
                    {
                        string msg = string.Format("Το παραστατικό έχει ήδη ακυρωθεί με το {0}", invoiceToCancel.ChildInvoiceRelations[0].ChildInvoice.Description);
                        RadMessageBox.Show(msg, "Σφάλμα Επιλογής", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
                        return;
                    }
                    else if (invoiceToCancel.ChildInvoiceRelations[0].RelationType == (int)Common.Enumerators.InvoiceRelationTypeEnum.Cancel)
                    {
                        string msg = string.Format("Το παραστατικό έχει αντικατασταθεί με το {0}", invoiceToCancel.ChildInvoiceRelations[0].ChildInvoice.Description);
                        RadMessageBox.Show(msg, "Σφάλμα Επιλογής", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
                        return;
                    }

                }
            }

            Common.Sales.SaleData newSale = new Common.Sales.SaleData();
            newSale.DisplayPrice = this.priceTotal1.Value;
            newSale.DisplayVolume = this.volume1.Value;
            newSale.FuelTypeDescription = this.currentNozzle.FuelType.Name;
            newSale.InvoiceTypeId = this.invoiceType.InvoiceTypeId;
            newSale.LiterCheck = false;
            newSale.NozzleId = this.currentNozzle.NozzleId;
            newSale.NozzleNumber = this.currentNozzle.OfficialNozzleNumber;
            newSale.TotalizerEnd = this.currentNozzle.TotalCounter;
            newSale.TotalizerStart = this.currentNozzle.TotalCounter;
            decimal totalPrice = this.priceTotal1.Value / (1 - this.discount.Value / 100);
            
            newSale.TotalPrice = totalPrice;
            newSale.TotalVolume = this.volume1.Value;
            newSale.UnitPrice = this.unitPrice1.Value;
            newSale.TankData = new List<Common.Sales.TankSaleData>().ToArray();
            if (this.currentVehicle != null)
            {
                if (this.currentVehicle == null)
                {
                    RadMessageBox.Show("Δεν έχετε επιλέξει όχημα!", "Σφάλμα Επιλογής", MessageBoxButtons.OK, RadMessageIcon.Error);
                    return;
                }
                newSale.VehicleId = this.currentVehicle.VehicleId;
            }
            this.currentNozzle.DiscountPercentage = this.discount.Value;
            Data.Invoice invoice = this.currentDispenser.CreateSale(newSale);
            invoice.Notes = this.radTextBox4.Text;
            if (this.invoiceToReplace != null)
            {
                Data.InvoiceRelation invRel = new Data.InvoiceRelation();
                invRel.InvoiceRelationId = Guid.NewGuid();
                this.db.Add(invRel);
                invRel.ParentInvoiceId = this.invoiceToReplace.InvoiceId;
                invRel.ChildInvoiceId = invoice.InvoiceId;
                invRel.RelationType = (int)Common.Enumerators.InvoiceRelationTypeEnum.Replace;
                invoice.InvoiceLines[0].SalesTransaction.Volume = 0;
                invoice.InvoiceLines[0].SalesTransaction.VolumeNormalized = 0;
                invoice.InvoiceLines[0].SalesTransaction.TotalPrice = 0;

                if (invoiceToReplace.FinTransactions.Count > 0)
                {
                    invoiceToReplace.FinTransactions[0].Amount = 0;
                }
            }

            if (this.invoiceToCancel != null)
            {
                Data.InvoiceRelation invRel = new Data.InvoiceRelation();
                invRel.InvoiceRelationId = Guid.NewGuid();
                this.db.Add(invRel);
                invRel.ParentInvoiceId = this.invoiceToCancel.InvoiceId;
                invRel.ChildInvoiceId = invoice.InvoiceId;
                invRel.RelationType = (int)Common.Enumerators.InvoiceRelationTypeEnum.Cancel;
                invoice.InvoiceLines[0].SalesTransaction.Volume = -this.invoiceToCancel.InvoiceLines[0].SalesTransaction.Volume;
                invoice.InvoiceLines[0].SalesTransaction.VolumeNormalized = -this.invoiceToCancel.InvoiceLines[0].SalesTransaction.VolumeNormalized;
                invoice.InvoiceLines[0].SalesTransaction.TotalPrice = -this.invoiceToCancel.InvoiceLines[0].SalesTransaction.TotalPrice;
            }

            invoice.PaymentType = Convert.ToInt32(paymentItem.Value);

            this.db.SaveChanges();
            this.Close();
        }

        private void radButton2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void radButton3_Click(object sender, EventArgs e)
        {
            using (SelectionForms.SelectTraderForm stf = new SelectionForms.SelectTraderForm())
            {
                stf.SelectSuppliers = true;
                DialogResult res = stf.ShowDialog(this);
                if (stf.SelectedVehicle == null)
                    return;
                this.currentVehicle = this.db.Vehicles.Where(v => v.VehicleId == stf.SelectedVehicle.VehicleId).FirstOrDefault();
                this.paymentTypeDropDownList.SelectedValue = this.currentVehicle.Trader.PaymentType;
                this.radTextBox2.Text = this.currentVehicle.PlateNumber + " - " + this.currentVehicle.Trader.Name;
            }
        }

        private void radButton4_Click(object sender, EventArgs e)
        {
            using (SelectionForms.SelectPrintedInvoiceForm spif = new SelectionForms.SelectPrintedInvoiceForm())
            {
                if(this.currentNozzle != null)
                    spif.NozzleId = this.currentNozzle.NozzleId;
                DialogResult res = spif.ShowDialog(this);
                if (res == System.Windows.Forms.DialogResult.Cancel)
                    return;
                if (spif.SelectedInvoice == null)
                    return;
                this.radTextBox3.Text = spif.SelectedInvoice.Description;
                this.invoiceToCancel = this.db.Invoices.Where(i => i.InvoiceId == spif.SelectedInvoice.InvoiceId).FirstOrDefault();
                ApplySourceSalePoint(this.invoiceToCancel);
                this.volume1.Value = this.invoiceToCancel.InvoiceLines[0].Volume;
                this.priceTotal1.Value = this.invoiceToCancel.TotalAmount.Value;
                this.unitPrice1.Value = this.invoiceToCancel.InvoiceLines[0].UnitPrice;
                this.currentVehicle = this.invoiceToCancel.Vehicle;
                if(this.currentVehicle != null)
                    this.radTextBox2.Text = this.currentVehicle.PlateNumber + " - " + this.currentVehicle.Trader.Name;
            }
        }

        private void radButton5_Click(object sender, EventArgs e)
        {
            using (SelectionForms.SelectPrintedInvoiceForm spif = new SelectionForms.SelectPrintedInvoiceForm())
            {
                if (this.currentNozzle != null)
                    spif.NozzleId = this.currentNozzle.NozzleId;
                DialogResult res = spif.ShowDialog(this);
                if (res == System.Windows.Forms.DialogResult.Cancel)
                    return;
                if (spif.SelectedInvoice == null)
                    return;
                this.radTextBox1.Text = spif.SelectedInvoice.Description;
                this.invoiceToReplace = this.db.Invoices.Where(i => i.InvoiceId == spif.SelectedInvoice.InvoiceId).FirstOrDefault();
                ApplySourceSalePoint(this.invoiceToReplace);
                this.volume1.Value = this.invoiceToReplace.InvoiceLines[0].Volume;
                this.priceTotal1.Value = this.invoiceToReplace.TotalAmount.Value;
                this.unitPrice1.Value = this.invoiceToReplace.InvoiceLines[0].UnitPrice;
                this.currentVehicle = this.invoiceToReplace.Vehicle;
                if (this.currentVehicle != null)
                    this.radTextBox2.Text = this.currentVehicle.PlateNumber + " - " + this.currentVehicle.Trader.Name;
            }
        }

        private void radCheckBox1_EnabledChanged(object sender, EventArgs e)
        {
            if (!this.radCheckBox1.Enabled)
            {
                this.invoiceToReplace = null;
                this.radTextBox1.Text = "";
                this.radCheckBox1.Checked = false;
            }
        }

        private void radCheckBox2_EnabledChanged(object sender, EventArgs e)
        {
            if (!this.radCheckBox2.Enabled)
            {
                this.invoiceToCancel = null;
                this.radTextBox3.Text = "";
                this.radCheckBox2.Checked = false;
            }
        }

        private void radSpinEditor1_ValueChanged(object sender, EventArgs e)
        {
            if (suspendDiscountChange)
                return;
            decimal total = this.unitPrice1.Value * this.volume1.Value;
            total = total - total * discount.Value / 100;
            this.priceTotal1.Value = total;
        }

        
    }
}
