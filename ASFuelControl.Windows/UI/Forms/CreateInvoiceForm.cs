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
        private Data.Dispenser currentDispenser = null;
        private Data.Nozzle currentNozzle = null;
        private Data.InvoiceType invoiceType = null;
        private Data.Vehicle currentVehicle = null;
        private Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
        private Data.Invoice invoiceToCancel;
        private Data.Invoice invoiceToReplace;

        public Guid InvoiceTypeId
        {
            get { return this.invoiceType.InvoiceTypeId; }
            set
            {
                if (value == Guid.Empty)
                {
                    this.invoiceType = null;
                    return;
                }
                this.invoiceType = this.db.InvoiceTypes.Where(i => i.InvoiceTypeId == value).FirstOrDefault();
                this.radDropDownList2.SelectedValue = value;
            }
        }

        public Data.Invoice InvoiceToCancel
        {
            get { return this.invoiceToCancel; }
            set
            {
                try
                {
                    if (value == null)
                        this.InvoiceToCancel = null;
                    else
                    {
                        this.DispenserId = value.InvoiceLines[0].SalesTransaction.Nozzle.DispenserId;
                        this.NozzleId = value.InvoiceLines[0].SalesTransaction.NozzleId;

                        this.invoiceToCancel = this.db.Invoices.Where(i => i.InvoiceId == value.InvoiceId).FirstOrDefault();
                        this.radTextBox3.Text = this.invoiceToCancel.Description;
                        this.unitPrice1.Value = this.invoiceToCancel.InvoiceLines[0].UnitPrice;
                        this.volume1.Value = this.invoiceToCancel.InvoiceLines[0].Volume;
                        this.priceTotal1.Value = this.invoiceToCancel.TotalAmount.Value;
                        this.discount.Value = 100 * (this.InvoiceToReplace.DiscountAmount) / this.invoiceToReplace.PreDiscountTotal;
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
                    this.InvoiceToCancel = null;
                else
                {
                    this.DispenserId = value.InvoiceLines[0].SalesTransaction.Nozzle.DispenserId;
                    this.NozzleId = value.InvoiceLines[0].SalesTransaction.NozzleId;

                    this.invoiceToReplace = this.db.Invoices.Where(i => i.InvoiceId == value.InvoiceId).FirstOrDefault();
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
                this.nozzleId = value;
                if (this.currentDispenser == null)
                    return;
                this.radDropDownList1.SelectedValue = this.nozzleId;
            }
            get { return this.nozzleId; }
        }

        public Guid DispenserId 
        {
            set 
            { 
                this.dispenserId = value;
                this.currentNozzle = null;
                this.currentDispenser = this.db.Dispensers.Where(d => d.DispenserId == this.dispenserId).FirstOrDefault();
                
                if (this.currentDispenser != null)
                {
                    this.radDropDownList1.DataSource = this.currentDispenser.Nozzles;
                    this.radDropDownList1.DisplayMember = "Description";
                    this.radDropDownList1.ValueMember = "NozzleId";
                }

                this.radDropDownList2.DataSource = this.db.InvoiceTypes.Where(it => it.Printable && (!it.IsInternal.HasValue || !it.IsInternal.Value) && it.TransactionType == 0).OrderBy(it => it.Description);
                this.radDropDownList2.DisplayMember = "Description";
                this.radDropDownList2.ValueMember = "InvoiceTypeId";
            }
            get { return this.dispenserId; } 
        }

        public CreateInvoiceForm()
        {
            InitializeComponent();

            this.paymentTypeDropDownList.DataSource = Common.Enumerators.EnumToList.EnumList<Common.Enumerators.PaymentTypeEnum>();
            this.paymentTypeDropDownList.DisplayMember = "Description";
            this.paymentTypeDropDownList.ValueMember = "Value";

            this.paymentTypeDropDownList.SelectedValue = 1;

            this.radLabel11.Enabled = Program.AdminConnected;
            this.radTextBox4.Enabled = Program.AdminConnected;
            this.Disposed += CreateInvoiceForm_Disposed;
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

        private void radDropDownList1_SelectedIndexChanged(object sender, Telerik.WinControls.UI.Data.PositionChangedEventArgs e)
        {
            Data.Nozzle nz = this.radDropDownList1.SelectedItem.DataBoundItem as Data.Nozzle;
            if (nz == null)
                return;
            this.currentNozzle = nz;
            this.unitPrice1.Value = nz.FuelType.CurrentPrice;
        }

        private void radDropDownList2_SelectedIndexChanged(object sender, Telerik.WinControls.UI.Data.PositionChangedEventArgs e)
        {
            this.invoiceType = this.radDropDownList2.SelectedItem.DataBoundItem as Data.InvoiceType;
            
            this.radButton3.Enabled = (invoiceType.NeedsVehicle.HasValue && invoiceType.NeedsVehicle.Value);
            this.radLabel9.Enabled = (invoiceType.NeedsVehicle.HasValue && invoiceType.NeedsVehicle.Value);
            this.radCheckBox2.Enabled = (invoiceType.IsCancelation.HasValue && invoiceType.IsCancelation.Value);
            this.radCheckBox1.Enabled = (!invoiceType.IsCancelation.HasValue || !invoiceType.IsCancelation.Value);
            
            this.radGroupBox1.Enabled = this.IsGroupEnabled();
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
            if (this.invoiceType == null)
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
                if (invoiceToCancel.InvoiceType.IsCancelation.HasValue && invoiceToCancel.InvoiceType.IsCancelation.Value)
                {
                    RadMessageBox.Show("Δεν μπορείτε να ακυρώσετε Ακυρωτικό", "Σφάλμα Επιλογής", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
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

            Common.Enumerators.EnumItem item = this.paymentTypeDropDownList.SelectedItem.DataBoundItem as Common.Enumerators.EnumItem;
            if (item == null)
                invoice.PaymentType = (int)Common.Enumerators.PaymentTypeEnum.Cash;
            else
                invoice.PaymentType = (int)item.Value;

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
