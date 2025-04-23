using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace ASFuelControl.Windows.UI.Controls
{
    public partial class TankControl : UserControl
    {
        private Color baseColor = Color.Black;

        private delegate void RefreshDelegate();

        private VirtualDevices.VirtualTank tank;

        public decimal TankPercentage { set; get; }

        public Color BaseColor
        {
            set 
            { 
                this.baseColor = value;
                this.panel2.BackColor = value;
                this.panel3.BackColor = value;
            }
            get { return this.baseColor; }
        }

        public VirtualDevices.VirtualTank Tank 
        {
            set 
            { 
                this.tank = value;
                this.tank.PropertyChanged += new PropertyChangedEventHandler(tank_PropertyChanged);
            }
            get { return this.tank; }
        }

        public TankControl()
        {
            InitializeComponent();
            this.tankGauge1.BaseColor = this.panel3.BackColor;
            this.tankGauge1.ForeColor = Color.White;
            this.tankLinearGauge1.BaseColor = this.panel3.BackColor;
            this.tankLinearGauge1.ForeColor = Color.White;
            this.tankLinearGauge2.BaseColor = this.panel3.BackColor;
            this.tankLinearGauge2.ForeColor = Color.White;
            this.TankPercentage = (decimal)0.65;

            this.btnFillingCancel.Enabled = false;
            this.btnFillingEnd.Enabled = false;
        }

        public void ScaleControl(float ratio)
        {
            this.Scale(new SizeF(ratio, ratio));
            this.ResizeFont(this, ratio);
        }

        public void RefreshValues()
        {
            if(this.tank.TotalVolume == 0)
                this.tankGauge1.PercentageValue = 50f;
            else
                this.tankGauge1.PercentageValue = 100f * ((float)this.tank.CurrentVolume / (float)this.tank.TotalVolume);

            this.tankGauge1.Temperature = this.tank.CurrentTemperature;

            this.label3.Text = string.Format("Διαθέσιμος Όγκος : {0} Lt", decimal.Round((decimal)0.9 * this.Tank.TotalVolume - this.Tank.CurrentVolume, 0));

            if (this.tank.MaxHeight == 0)
            {
                this.tankLinearGauge1.PercentageValue = 50f;
                this.tankLinearGauge1.CurrentLevel = 0;
            }
            else
            {
                this.tankLinearGauge1.PercentageValue = 100f * ((float)this.tank.CurrentFuelLevel / (float)this.tank.MaxHeight);
                this.tankLinearGauge1.CurrentLevel = this.tank.CurrentFuelLevel;
            }

            if (this.tank.MaxHeight == 0)
            {
                this.tankLinearGauge2.PercentageValue = 50f;
                this.tankLinearGauge2.CurrentLevel = 0;
            }
            else
            {
                this.tankLinearGauge2.PercentageValue = 100f * ((float)this.tank.CurrentWaterLevel / (float)this.tank.MaxWaterHeight);
                this.tankLinearGauge2.CurrentLevel = this.tank.CurrentWaterLevel;
            }

            //this.labelTemp.Text = string.Format("{0:N2} oC", this.tank.CurrentTemperature);
            this.labelVolume.Text = string.Format("{0:N2} Lt", this.tank.CurrentVolume);
            this.labelVolumeNormalized.Text = string.Format("{0:N2} Lt", this.tank.CurrentVolumeNormalized);
            this.labelAveragePrice.Text = string.Format("{0:N2} €", this.tank.PriceAverage);
            this.labelNumber.Text = this.tank.TankNumber.ToString();
            this.labelFuelTypeDesc.Text = this.tank.FuelTypeDescription;
            this.labelStatus.Text = Properties.Resources.ResourceManager.GetString("TankStatusEnum_" + tank.TankStatus.ToString());
            if (tank.TankStatus == Common.Enumerators.TankStatusEnum.Offline)
            {
                this.tankGauge1.StateColor = Color.Black;
            }
            else if (tank.TankStatus == Common.Enumerators.TankStatusEnum.Idle)
                this.tankGauge1.StateColor = Color.FromArgb(150,150, 255);//.Blue;
            else if (tank.TankStatus == Common.Enumerators.TankStatusEnum.Selling)
                this.tankGauge1.StateColor = Color.Green;
            else if (tank.TankStatus == Common.Enumerators.TankStatusEnum.Filling || tank.TankStatus == Common.Enumerators.TankStatusEnum.FuelExtraction)
                this.tankGauge1.StateColor = Color.Green;
            else if (tank.TankStatus == Common.Enumerators.TankStatusEnum.Error || tank.TankStatus == Common.Enumerators.TankStatusEnum.LowLevel ||
                tank.TankStatus == Common.Enumerators.TankStatusEnum.LevelDecrease || tank.TankStatus == Common.Enumerators.TankStatusEnum.LevelIncrease || tank.TankStatus == Common.Enumerators.TankStatusEnum.HighWaterLevel)
                this.tankGauge1.StateColor = Color.Red;
            else
                this.tankGauge1.StateColor = Color.Orange;
            this.labelStatus.BackColor = this.tankGauge1.StateColor;

            if (this.Tank.TankStatus == Common.Enumerators.TankStatusEnum.FillingInit || this.Tank.TankStatus == Common.Enumerators.TankStatusEnum.FuelExtractionInit)
            {
                this.btnFillingStart.Enabled = false;
                this.btnFillingCancel.Enabled = true;
                this.btnFillingEnd.Enabled = false;
            }
            else if (this.Tank.TankStatus == Common.Enumerators.TankStatusEnum.Filling || this.Tank.TankStatus == Common.Enumerators.TankStatusEnum.FuelExtraction)
            {
                this.btnFillingStart.Enabled = false;
                this.btnFillingCancel.Enabled = false;
                this.btnFillingEnd.Enabled = true;
            }
            else if (this.Tank.TankStatus == Common.Enumerators.TankStatusEnum.Waiting)
            {
                this.btnFillingStart.Enabled = false;
                this.btnFillingCancel.Enabled = false;
                this.btnFillingEnd.Enabled = false;
            }
            else
            {
                this.btnFillingStart.Enabled = true;
                this.btnFillingCancel.Enabled = false;
                this.btnFillingEnd.Enabled = false;
            }

            this.tankGauge1.Invalidate();
            this.tankLinearGauge1.Invalidate();
            this.tankLinearGauge2.Invalidate();
            //if (tank.TankStatus == Common.Enumerators.TankStatusEnum.Offline || tank.TankStatus == Common.Enumerators.TankStatusEnum.Error)
            //    this.Enabled = false;
            //else
            //    this.Enabled = true;
        }

        private void ResizeFont(Control ctrl, float ratio)
        {
            foreach (Control c in ctrl.Controls)
            {
                this.ResizeFont(c, ratio);
                if (c.Font != null)
                    c.Font = new Font(c.Font.FontFamily, c.Font.Size * ratio);
            }
        }

        void tank_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!this.IsHandleCreated)
                return;
            if (this.IsDisposed)
                return;
            this.Invoke(new RefreshDelegate(this.RefreshValues), null);
            //this.RefreshValues();
        }

        private void panel3_BackColorChanged(object sender, EventArgs e)
        {
            this.tankGauge1.BaseColor = this.panel3.BackColor;
            this.tankGauge1.ForeColor = Color.White;
            this.tankLinearGauge1.BaseColor = this.panel3.BackColor;
            this.tankLinearGauge1.ForeColor = Color.White;
            this.tankLinearGauge2.BaseColor = this.panel3.BackColor;
            this.tankLinearGauge2.ForeColor = Color.White;
            this.tankGauge1.Invalidate();
        }

        private void btnEnabled_Changed(object sender, EventArgs e)
        {
            Panel p = sender as Panel;
            if (p == null)
                return;
            if (p.Enabled)
                p.BackColor = Color.FromKnownColor(KnownColor.Control);
            else
                p.BackColor = Color.LightGray;
        }

        private void btnFillingStart_Click(object sender, EventArgs e)
        {

            using (SelectionForms.SelectTankActionForm staf = new SelectionForms.SelectTankActionForm())
            {
                staf.ShowDialog();
                if (!staf.Fill)
                {
                    using (SelectionForms.SelectTraderForm stf = new SelectionForms.SelectTraderForm())
                    {
                        stf.ShowDialog(this);
                        if (stf.SelectedVehicle == null)
                            return;
                        tank.VehicleId = stf.SelectedVehicle.VehicleId;
                    }

                    using (SelectionForms.SelectInvoiceTypeForm sitf = new SelectionForms.SelectInvoiceTypeForm())
                    {
                        sitf.InternalType = 1;
                        sitf.TransactionType = 0;
                        DialogResult res = sitf.ShowDialog(this);
                        if (res == DialogResult.Cancel)
                            return;

                        using (SelectionForms.SelectFuelTypeForm sftf = new SelectionForms.SelectFuelTypeForm())
                        {
                            sftf.StartPosition = FormStartPosition.CenterScreen;
                            sftf.SelectedFuelTypeId = tank.FuelTypeId;
                            DialogResult res2 = sftf.ShowDialog(this);
                            if(res2 == DialogResult.Cancel || sftf.SelectedFuelTypeId == Guid.Empty)
                                return;
                            tank.FillingFuelTypeId = sftf.SelectedFuelTypeId;
                        }

                        tank.InvoiceTypeId = sitf.SelectedInvoiceTypeId;

                        this.Tank.InitializeExtraction = true;
                    }
                }
                else
                {
                    using (SelectionForms.SelectInvoiceForm siv = new SelectionForms.SelectInvoiceForm())
                    {
                        siv.TankId = this.Tank.TankId;
                        siv.FuelTypeId = this.Tank.FuelTypeId;
                        siv.StartPosition = FormStartPosition.CenterScreen;
                        DialogResult res = siv.ShowDialog(this);
                        if (res == DialogResult.Cancel)
                            return;



                        decimal allowedVolume = this.Tank.GetTankVolume(this.Tank.MaxHeight) - this.Tank.CurrentVolume;
                        if (siv.InvoicedVolume > allowedVolume)
                        {
                            decimal diff = siv.InvoicedVolume - allowedVolume;
                            DialogResult res2 = MessageBox.Show(string.Format("Ο Ογκος που θέλετε να παραλάβετε δεν χωράει στην επιλεγμένη δεξαμενή.\r\nΠερισσεύουν {0:N2} Λίτρα", diff), "ΠΡΟΣΟΧΗ ΑΔΥΝΑΤΗ ΠΑΡΑΛΑΒΗ", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                            if (res2 == DialogResult.No)
                                return;
                        }

                        this.Tank.InvoiceLineId = siv.SelectedInvoiceLineId;


                        if (siv.SelectionFillingMode == SelectionForms.SelectInvoiceControl.FillingModeEnum.Delivery || siv.SelectionFillingMode == SelectionForms.SelectInvoiceControl.FillingModeEnum.LiterCheck)
                        {
                            this.Tank.InitializeFilling = true;
                            this.Tank.IsLiterCheck = siv.SelectionFillingMode == SelectionForms.SelectInvoiceControl.FillingModeEnum.LiterCheck;
                        }
                        else if (siv.SelectionFillingMode == SelectionForms.SelectInvoiceControl.FillingModeEnum.Return)
                            this.Tank.InitializeExtraction = true;
                    }
                }
            }
        }

        private void btnFillingCancel_Click(object sender, EventArgs e)
        {
            if (this.tank.TankStatus == Common.Enumerators.TankStatusEnum.FillingInit)
                this.Tank.InitializeFilling = false;
            else if (this.tank.TankStatus == Common.Enumerators.TankStatusEnum.FuelExtractionInit)
                this.Tank.InitializeExtraction = false;
        }

        private void btnFillingEnd_Click(object sender, EventArgs e)
        {
            if (this.tank.TankStatus == Common.Enumerators.TankStatusEnum.Filling)
                this.Tank.FillingFinished = true;
            else if (this.tank.TankStatus == Common.Enumerators.TankStatusEnum.FuelExtraction)
                this.Tank.ExtractionFinished = true;
        }
    }
}
