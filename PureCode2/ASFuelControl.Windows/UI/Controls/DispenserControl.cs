using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ASFuelControl.Logging;

namespace ASFuelControl.Windows.UI.Controls
{
    public partial class DispenserControl : UserControl
    {
        private delegate void RefreshDelegate();

        private int initialWidth = 394;
        private int initialHeight = 424;
        private double ratioX = 1;
        private double ratioY = 1;
        private VirtualDevices.VirtualDispenser dispenser;
        

        public VirtualDevices.VirtualDispenser Dispenser
        {
            set
            {
                this.dispenser = value;
                this.dispenserSaleBindingSource.DataSource = dispenser.LastSales;
                this.dispenser.PropertyChanged += new PropertyChangedEventHandler(dispenser_PropertyChanged);
            }
            get { return this.dispenser; }
        }

        public DispenserControl()
        {
            InitializeComponent();
            this.Load += new EventHandler(DispenserControl_Load);
        }

        void DispenserControl_Load(object sender, EventArgs e)
        {
            this.dispenserSaleRadGridView.CurrentRow = null;

            this.dispenserSaleRadGridView.CurrentRowChanged += new Telerik.WinControls.UI.CurrentRowChangedEventHandler(dispenserSaleRadGridView_CurrentRowChanged);
            this.dispenserSaleRadGridView.SelectionChanged += new EventHandler(dispenserSaleRadGridView_SelectionChanged);
        }

        void dispenserSaleRadGridView_SelectionChanged(object sender, EventArgs e)
        {
            //this.dispenserSaleRadGridView.CurrentRow = null;
        }

        void dispenserSaleRadGridView_CurrentRowChanged(object sender, Telerik.WinControls.UI.CurrentRowChangedEventArgs e)
        {
            this.dispenserSaleRadGridView.CurrentRow = null;
        }

        public void ScaleControl(float ratio)
        {
            this.Scale(new SizeF(ratio, ratio));
            this.ResizeFont(this, ratio);
            this.dispenserSaleRadGridView.GridElement.RowHeight = this.dispenserSaleRadGridView.Height / 3;
        }

        public void RefreshValues()
        {
            this.labelDispenserNumber.Text = this.dispenser.OfficialNumber.ToString();

            this.btnCustomer.Enabled = this.dispenser.IsValid;
            this.btnInvoices.Enabled = this.dispenser.IsValid;
            this.btnLiterCheck.Enabled = this.dispenser.IsValid;

            if(this.Dispenser.TotalsMisfunction)
                this.panel10.BackgroundImage = Properties.Resources.TotalsError;
            else
                this.panel10.BackgroundImage = Properties.Resources.Information;

            if (this.Dispenser.DeviceLocked)
                this.panel11.BackgroundImage = Properties.Resources.Locked;
            else
                this.panel11.BackgroundImage = Properties.Resources.UnLocked;

            if (!this.dispenser.IsValid)
            {
                this.labelStatus.Text ="Ανενεργή Αντλία";
                this.labelFuelType.Text = "-";
                this.labelNozzleNumber.Text = "-";
                return;
            }

            this.labelStatus.Text = Properties.Resources.ResourceManager.GetString("FuelPointStatusEnum_" + this.dispenser.Status.ToString());
            if (this.dispenser.ActiveNozzle != null)
            {
                this.labelFuelType.Text = this.dispenser.ActiveNozzle.FuelTypeDescription;
                this.SetValue(this.dispenser.ActiveNozzle.CurrentSaleTotalPrice, 2, this.labelTotalPrice, this.labelTotalPriceDec);
                this.SetValue(this.dispenser.ActiveNozzle.CurrentSaleTotalVolume, 2, this.labelTotalVolume, this.labelTotalVolumeDec);
                this.SetValue(this.dispenser.ActiveNozzle.CurrentSaleUnitPrice, 3, this.labelUnitPrice, this.labelUnitPriceDec);

                //int decimalPrice = (int)((this.dispenser.ActiveNozzle.CurrentSaleTotalPrice - (int)this.dispenser.ActiveNozzle.CurrentSaleTotalPrice) * 100);
                //if(decimalPrice == 100)
                //    this.labelTotalPriceDec.Text = "00";
                //else
                //    this.labelTotalPriceDec.Text = decimalPrice.ToString("N0");

                //if (this.labelTotalPriceDec.Text.Length == 1)
                //{
                //    this.labelTotalPriceDec.Text = "0" + this.labelTotalPriceDec.Text;
                //}

               

                //this.labelTotalPrice.Text = ((int)this.dispenser.ActiveNozzle.CurrentSaleTotalPrice).ToString();
                //int decimalVolume = (int)((this.dispenser.ActiveNozzle.CurrentSaleTotalVolume - (int)this.dispenser.ActiveNozzle.CurrentSaleTotalVolume) * 100);
                //this.labelTotalVolume.Text = ((int)this.dispenser.ActiveNozzle.CurrentSaleTotalVolume).ToString();

                //if (decimalVolume == 100)
                //    this.labelTotalVolumeDec.Text = "00";
                //else
                //    this.labelTotalVolumeDec.Text = decimalVolume.ToString("N0");
                //if (this.labelTotalVolumeDec.Text.Length == 1)
                //{
                //    this.labelTotalVolumeDec.Text = "0" + this.labelTotalVolumeDec.Text;
                //}

                //this.labelUnitPrice.Text = ((int)this.dispenser.ActiveNozzle.CurrentSaleUnitPrice).ToString();
                //this.labelUnitPriceDec.Text = ((this.dispenser.ActiveNozzle.CurrentSaleUnitPrice - (int)this.dispenser.ActiveNozzle.CurrentSaleUnitPrice) * 1000).ToString("N0");

                //if (this.labelUnitPriceDec.Text.Length == 1)
                //{
                //    this.labelUnitPriceDec.Text = "00" + this.labelUnitPriceDec.Text;
                //}
                //else if (this.labelUnitPriceDec.Text.Length == 2)
                //{
                //    this.labelUnitPriceDec.Text = "0" + this.labelUnitPriceDec.Text;
                //}

                this.labelNozzleNumber.Text = this.dispenser.ActiveNozzle.NozzleNumber.ToString();
                if (this.dispenser.ActiveNozzle.FuelColor != 0)
                {
                    this.panel1.BackColor = Color.FromArgb(this.dispenser.ActiveNozzle.FuelColor);
                }
            }
            else
            {
                if (this.dispenser.LastActiveNozzle != null)
                {
                    this.SetValue(this.dispenser.LastActiveNozzle.CurrentSaleTotalPrice, 2, this.labelTotalPrice, this.labelTotalPriceDec);
                    this.SetValue(this.dispenser.LastActiveNozzle.CurrentSaleTotalVolume, 2, this.labelTotalVolume, this.labelTotalVolumeDec);
                    this.SetValue(this.dispenser.LastActiveNozzle.CurrentSaleUnitPrice, 3, this.labelUnitPrice, this.labelUnitPriceDec);
                }
                else
                {
                    this.SetValue(0, 2, this.labelTotalPrice, this.labelTotalPriceDec);
                    this.SetValue(0, 2, this.labelTotalVolume, this.labelTotalVolumeDec);
                    this.SetValue(0, 3, this.labelUnitPrice, this.labelUnitPriceDec);
                }
                this.labelFuelType.Text = "-";
                this.labelNozzleNumber.Text = "-";
                if(this.dispenser.Status == Common.Enumerators.FuelPointStatusEnum.Offline)
                    this.panel1.BackColor = Color.Black;//.FromArgb(255, 233, 82, 65);
                else
                    this.panel1.BackColor = Color.Gray;//.FromArgb(255, 233, 82, 65);
            }
            this.dispenserSaleBindingSource.DataSource = null;
            this.dispenserSaleBindingSource.DataSource = dispenser.LastSales;
            this.dispenserSaleBindingSource.ResetBindings(false);

            if (this.dispenser.Nozzles.Where(n => n.Status == Common.Enumerators.NozzleStateEnum.LiterCheck).Count() > 0)
                this.btnLiterCheck.BackColor = Color.Orange;
            else
                this.btnLiterCheck.BackColor = Color.Transparent;

            if (this.Dispenser.NextSaleVehicle == Guid.Empty)
                this.btnCustomer.BackColor = Color.Transparent;
            else
                this.btnCustomer.BackColor = Color.Orange;

            foreach (VirtualDevices.VirtualNozzle nz in this.dispenser.Nozzles)
            {
                if (nz.Status == Common.Enumerators.NozzleStateEnum.Locked)
                {
                    this.btnLiterCheck.BackColor = Color.Red;
                }
            }
        }

        private void SetValue(decimal amount, int decPlaces, Label labelInt, Label labelDec)
        {
            int decMultiplier = (int)Math.Pow(10, decPlaces);

            labelInt.Text = ((int)amount).ToString();
            int decimalValue = (int)((amount - (int)amount) * decMultiplier);
            labelDec.Text = "";
            if (decimalValue == decMultiplier)
            {
                for(int i=0; i < decPlaces; i++)
                    labelDec.Text = labelDec.Text + "0";
            }
            else
                labelDec.Text = decimalValue.ToString("N0");

            if (labelDec.Text.Length < 2)
            {
                for (int i = labelDec.Text.Length; i < 2; i++)
                {
                    labelDec.Text = "0" + labelDec.Text;
                }
            }
        }

        private void ResizeFont(Control ctrl, float ratio)
        {
            foreach (Control c in ctrl.Controls)
            {
                this.ResizeFont(c, ratio);
                if(c.Font != null)
                    c.Font = new Font(c.Font.FontFamily, c.Font.Size * ratio);
            }
        }

        void dispenser_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                if (!this.IsHandleCreated)
                    return;

                if (e.PropertyName == "UpdateUI" && this.dispenser.UpdateUI)
                    this.Invoke(new RefreshDelegate(this.RefreshValues), null);
            }
            catch(Exception ex)
            {
                Logger.Instance.LogToFile("dispenser_PropertyChanged", ex);
            }
            //if (e.PropertyName == "LastSales")
            //{
            //    lock (dispenser.LastSales)
            //    {
            //        this.dispenserSaleBindingSource.DataSource = dispenser.LastSales;
            //        this.dispenserSaleBindingSource.ResetBindings(false);
            //    }
            //}
        }

        private void btnInvoices_Click(object sender, EventArgs e)
        {
            Forms.DispenserTransactionsForm dtf = new Forms.DispenserTransactionsForm();
            dtf.DispenserId = this.dispenser.DispenserId;
            dtf.Show(this);
        }

        private void btnLiterCheck_Click(object sender, EventArgs e)
        {
            UI.SelectionForms.SelectNozzleForm snf = new SelectionForms.SelectNozzleForm();
            snf.LoadDispenser(this.dispenser);
            snf.StartPosition = FormStartPosition.CenterScreen;
            DialogResult res = snf.ShowDialog();
            this.RefreshValues();
        }

        private void btnCustomer_Click(object sender, EventArgs e)
        {
            if (this.Dispenser.NextSaleVehicle != Guid.Empty)
            {
                this.Dispenser.NextSaleVehicle = Guid.Empty;
                this.Dispenser.NextSaleTrader = Guid.Empty;
                this.Dispenser.InvoiceTypeId = Guid.Empty;
                this.btnCustomer.BackColor = Color.Transparent;
            }
            else
            {
                using (SelectionForms.SelectTraderForm stf = new SelectionForms.SelectTraderForm())
                {
                    stf.ShowDialog(this);
                    if (stf.SelectedVehicle == null)
                        return;
                    
                    using(SelectionForms.SelectInvoiceTypeForm sif = new SelectionForms.SelectInvoiceTypeForm())
                    {
                        sif.SelectedTrader = stf.SelectedVehicle.TraderId;
                        sif.ShowDialog();
                        if (sif.DialogResult == DialogResult.Cancel)
                            return;
                        if (sif.SelectedInvoiceTypeId == Guid.Empty)
                            return;

                        this.Dispenser.InvoiceTypeId = sif.SelectedInvoiceTypeId;
                        this.Dispenser.NextSaleVehicle = stf.SelectedVehicle.VehicleId;
                        this.Dispenser.NextSaleTrader = stf.SelectedVehicle.TraderId;
                        this.btnCustomer.BackColor = Color.Orange;
                    }
                }
            }
        }

        private void btnInvoices_MouseMove(object sender, MouseEventArgs e)
        {
            Panel p = sender as Panel;
            if (p == null)
                return;
            if (p.BackColor == Color.Orange || p.BackColor == Color.Red)
                return;
            p.BackColor = Color.LightGray;
        }

        private void btnInvoices_MouseLeave(object sender, EventArgs e)
        {
            Panel p = sender as Panel;
            if (p == null)
                return;
            if (p.BackColor == Color.Orange || p.BackColor == Color.Red)
                return;
            p.BackColor = Color.Transparent;
        }

        private void panel10_Click(object sender, EventArgs e)
        {
            using (UI.Forms.DispenserInfoForm dispInfo = new Forms.DispenserInfoForm())
            {
                dispInfo.LoadDispenserInfo(this.dispenser);
                dispInfo.ShowDialog(this);
            }
        }

        private void panel11_Click(object sender, EventArgs e)
        {
            this.Dispenser.DeviceLocked = !this.Dispenser.DeviceLocked;
            this.RefreshValues();
        }
    }
}
