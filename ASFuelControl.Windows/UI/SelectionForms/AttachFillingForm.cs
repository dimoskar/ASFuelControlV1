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
    public partial class AttachFillingForm : RadForm
    {
        Data.DatabaseModel database = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
        Guid deliveryType = Guid.Empty;

        private bool emptyInvoice = false;

        public bool EmptyInvoice
        {
            set 
            { 
                this.emptyInvoice = value;
                this.radCheckBox1.Checked = this.emptyInvoice;
                this.radCheckBox2.Checked = !this.emptyInvoice;
            }
            get { return this.emptyInvoice; }
        }

        public AttachFillingForm()
        {
            InitializeComponent();

            this.deliveryType = Data.Implementation.OptionHandler.Instance.GetGuidOption("DeliveryCheckInvoiceType", Guid.Empty);
            this.radDropDownList1.DataSource = this.database.Tanks.OrderBy(t => t.TankNumber);
            this.radDropDownList1.DisplayMember = "Description";
            this.radDropDownList1.ValueMember = "TankId";


        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            DateTime dt1 = this.radDateTimePicker1.Value.Date;
            DateTime dt2 = this.radDateTimePicker2.Value.Date.AddDays(1);

            Guid tankId = (Guid)this.radDropDownList1.SelectedValue;
            Data.Tank tank = this.database.Tanks.Where(t => t.TankId == tankId).FirstOrDefault();
            if(tank == null)
                return;

            var q = database.InvoiceLines.Where(il => il.FuelTypeId == tank.FuelTypeId && !il.TankFillingId.HasValue && il.Invoice.TransactionDate <= dt2 && il.Invoice.TransactionDate >= dt1 && il.Invoice.InvoiceTypeId == this.deliveryType);
            this.invoiceLineRadGridView.DataSource = q;

            var qtf = this.database.TankFillings.Where(tf => tf.TankId == tankId && tf.TransactionTime <= dt2 && tf.TransactionTime >= dt1 && tf.InvoiceLines.Count == 0).OrderBy(tf=>tf.TransactionTime);
            this.tankFillingRadGridView.DataSource = qtf;

            var qtf2 = this.database.TankFillings.Where(tf => tf.TankId == tankId && tf.TransactionTime <= dt2 && tf.TransactionTime >= dt1 && tf.InvoiceLines.Count > 0).OrderBy(tf => tf.TransactionTime);
            this.radGridView1.DataSource = qtf2;

            var qtsl = database.TankLevelStartViews.Where(t => t.TankId == tankId && t.TansDate <= dt2 && t.TansDate >= dt1).OrderBy(t=>t.TansDate);
            this.tankLevelStartViewRadGridView.DataSource = qtsl;
        }

        private void tankLevelStartViewRadGridView_CommandCellClick(object sender, EventArgs e)
        {
            try
            {
                GridCommandCellElement cell = sender as GridCommandCellElement;
                if (cell == null)
                    return;

                Guid tankId = (Guid)this.radDropDownList1.SelectedValue;
                Data.Tank tank = this.database.Tanks.Where(t => t.TankId == tankId).FirstOrDefault();
                if (tank == null)
                    return;
                Data.TankFilling tf = null;
                if (this.tankFillingRadGridView.CurrentRow != null)
                    tf = this.tankFillingRadGridView.CurrentRow.DataBoundItem as Data.TankFilling;
                if (tf == null)
                {

                    tf = new Data.TankFilling();
                    tf.TankFillingId = Guid.NewGuid();
                    tf.TankId = tankId;
                    tf.ApplicationUserId = this.database.ApplicationUsers.Where(a => a.UserLevel == 0).First().ApplicationUserId;
                    this.database.Add(tf);

                }



                if (cell.ColumnInfo.Name == "column1")
                {
                    Data.TankLevelStartView tankLevel = cell.RowInfo.DataBoundItem as Data.TankLevelStartView;
                    if (tankLevel == null)
                        return;

                    DateTime dt = tankLevel.TansDate;
                    if (tf.FuelDensity == 0)
                        tf.FuelDensity = tank.GetDensityAtTime(tankLevel.TansDate);
                    if (tf.TankTemperatureEnd == 0)
                        tf.TankTemperatureEnd = tank.GetTempmeratureAtTime(tankLevel.TansDate);
                    if (tf.TankTemperatureStart == 0)
                        tf.TankTemperatureStart = tank.GetTempmeratureAtTime(tankLevel.TansDate);
                    if (tf.UsagePeriodId == Guid.Empty)
                    {
                        Data.UsagePeriod up = this.database.GetUsagePeriod(tankLevel.TansDate);
                        if (up != null)
                            tf.UsagePeriodId = up.UsagePeriodId;
                    }
                    if (tf.TankPriceId == Guid.Empty)
                    {
                        Data.TankPrice price = this.database.TankPrices.Where(t => t.TankId == tankId).ToList().OrderBy(t => Math.Abs(t.ChangeDate.Subtract(dt).TotalDays)).FirstOrDefault();
                        if (price != null)
                        {
                            tf.TankPriceId = price.TankPriceId;
                            tf.TankPrice = price;
                        }
                    }
                    tf.LevelStart = tankLevel.Level.Value;
                    tf.TransactionTime = dt;
                    tf.TransactionTimeEnd = dt;
                    tf.VolumeReal = tf.Tank.GetTankVolume(tf.LevelEnd) - tf.Tank.GetTankVolume(tf.LevelStart);
                    tf.VolumeRealNormalized = tf.Tank.FuelType.NormalizeVolume(tf.VolumeReal, tf.Tank.GetTempmeratureAtTime(tf.TransactionTime), tf.Tank.GetDensityAtTime(tf.TransactionTime));
                }
                else if (cell.ColumnInfo.Name == "column2")
                {
                    Data.TankLevelStartView tankLevel = cell.RowInfo.DataBoundItem as Data.TankLevelStartView;
                    if (tankLevel == null)
                        return;
                    DateTime dt = tankLevel.TansDate;
                    if (tf.FuelDensity == 0)
                        tf.FuelDensity = tank.GetDensityAtTime(tankLevel.TansDate);
                    if (tf.TankTemperatureEnd == 0)
                        tf.TankTemperatureEnd = tank.GetTempmeratureAtTime(tankLevel.TansDate);
                    if (tf.TankTemperatureStart == 0)
                        tf.TankTemperatureStart = tank.GetTempmeratureAtTime(tankLevel.TansDate);
                    if (tf.UsagePeriodId == Guid.Empty)
                    {
                        Data.UsagePeriod up = this.database.GetUsagePeriod(tankLevel.TansDate);
                        if (up != null)
                            tf.UsagePeriodId = up.UsagePeriodId;
                    }
                    if (tf.TankPriceId == Guid.Empty)
                    {
                        Data.TankPrice price = this.database.TankPrices.Where(t => t.TankId == tankId).ToList().OrderBy(t => Math.Abs(t.ChangeDate.Subtract(dt).TotalDays)).FirstOrDefault();
                        if (price != null)
                        {
                            tf.TankPriceId = price.TankPriceId;
                            tf.TankPrice = price;
                        }
                    }

                    tf.LevelEnd = tankLevel.Level.Value;
                    tf.Tank.GetTankVolume(tf.LevelEnd);
                    tf.TransactionTime = dt;
                    tf.TransactionTimeEnd = dt;
                    tf.VolumeReal = tf.Tank.GetTankVolume(tf.LevelEnd) - tf.Tank.GetTankVolume(tf.LevelStart);
                    tf.VolumeRealNormalized = tf.Tank.FuelType.NormalizeVolume(tf.VolumeReal, tf.Tank.GetTempmeratureAtTime(tf.TransactionTime), tf.Tank.GetDensityAtTime(tf.TransactionTime));
                }
            }
            catch(Exception ex)
            {
                Telerik.WinControls.RadMessageBox.Show(ex.Message, "Σφαλμα", MessageBoxButtons.OK, Telerik.WinControls.RadMessageIcon.Error);
            }
        }

        private void invoiceLineRadGridView_CommandCellClick(object sender, EventArgs e)
        {
              GridCommandCellElement cell = sender as GridCommandCellElement;
            if (cell == null)
                return;

            Data.TankFilling tf = this.tankFillingRadGridView.CurrentRow.DataBoundItem as Data.TankFilling;
            if (tf == null)
                return;

            if (cell.ColumnInfo.Name == "column1")
            {
                Data.InvoiceLine invLine = cell.RowInfo.DataBoundItem as Data.InvoiceLine;
                if (invLine == null)
                    return;
                invLine.TankFillingId = tf.TankFillingId;
            }
        }

        private void radButton2_Click(object sender, EventArgs e)
        {
            this.database.SaveChanges();
        }

        private void radCheckBox1_Click(object sender, EventArgs e)
        {
            this.EmptyInvoice = true;
        }

        private void radCheckBox2_Click(object sender, EventArgs e)
        {
            this.EmptyInvoice = false;
        }

        private void radCheckBox1_ToggleStateChanging(object sender, StateChangingEventArgs args)
        {
            if (this.EmptyInvoice && args.NewValue == Telerik.WinControls.Enumerations.ToggleState.Off)
                args.Cancel = true;
            else if (!this.EmptyInvoice && args.NewValue == Telerik.WinControls.Enumerations.ToggleState.On)
                args.Cancel = true;
        }

        private void radCheckBox2_ToggleStateChanging(object sender, StateChangingEventArgs args)
        {
            if (this.EmptyInvoice && args.NewValue == Telerik.WinControls.Enumerations.ToggleState.On)
                args.Cancel = true;
            else if (!this.EmptyInvoice && args.NewValue == Telerik.WinControls.Enumerations.ToggleState.Off)
                args.Cancel = true;
        }
    }
}
