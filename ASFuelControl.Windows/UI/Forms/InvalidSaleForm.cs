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
    public partial class InvalidSaleForm : RadForm
    {
        private Data.DatabaseModel database = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
        private List<Data.SalesTransaction> sales = new List<Data.SalesTransaction>();
        VirtualDevices.VirtualDispenser currentDispenser;

        public InvalidSaleForm()
        {
            InitializeComponent();
            this.Disposed += InvalidSaleForm_Disposed;
        }

        private void InvalidSaleForm_Disposed(object sender, EventArgs e)
        {
            this.database.Dispose();
        }

        public void LoadSalesTransactions(VirtualDevices.VirtualDispenser dispenser)
        {
            this.currentDispenser = dispenser;
            this.sales = this.database.SalesTransactions.Where(s => dispenser.InvalidSales.Contains(s.SalesTransactionId) && s.InvalidSale.HasValue && s.InvalidSale.Value).ToList();
            this.salesTransactionBindingSource.DataSource = sales;
        }

        private void salesTransactionBindingSource_PositionChanged(object sender, EventArgs e)
        {
            if (this.salesTransactionBindingSource.Position < 0)
                return;
            Data.SalesTransaction sale = this.salesTransactionBindingSource.Current as Data.SalesTransaction;
            if (sale == null)
                return;
            this.volume1.Value = sale.Volume;
            this.priceTotal1.Value = sale.TotalPrice;
            this.unitPrice1.Value = sale.UnitPrice;

            decimal totalDiff = sale.TotalizerDiff / (decimal)Math.Pow(10, this.currentDispenser.TotalDecimalPlaces);

            this.volume2.Value = totalDiff;
            this.priceTotal2.Value = decimal.Round(totalDiff * sale.UnitPrice, 2);
            this.unitPrice2.Value = sale.UnitPrice;

            VirtualDevices.VirtualNozzle currentNozzle = this.currentDispenser.Nozzles.Where(n => n.NozzleId == sale.NozzleId).FirstOrDefault();
            if (currentNozzle == null)
                return;
            if (currentNozzle.Status != Common.Enumerators.NozzleStateEnum.Normal && currentNozzle.Status != Common.Enumerators.NozzleStateEnum.InvoicePrint)
                this.radCheckBox1.Checked = true;
            else
                this.radCheckBox1.Checked = false;
        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            if (this.salesTransactionBindingSource.Position < 0)
                return;
            Data.SalesTransaction sale = this.salesTransactionBindingSource.Current as Data.SalesTransaction;
            if (sale == null)
            {
                if (this.salesTransactionRadGridView.CurrentRow == null)
                    return;
                sale = this.salesTransactionRadGridView.CurrentRow.DataBoundItem as Data.SalesTransaction;
                if(sale== null)
                    return;
            }
            if (this.radRadioButton1.IsChecked)
            {
                sale.Volume = decimal.Round(sale.TotalPrice / sale.UnitPrice, 3);
                sale.VolumeNormalized = sale.Nozzle.FuelType.NormalizeVolume(sale.Volume, sale.TemperatureEnd, sale.Nozzle.FuelType.BaseDensity);
                sale.Nozzle.Dispenser.CreateInvoice(sale, this.radCheckBox1.Checked);
            }
            else
            {
                decimal totalDiff = sale.TotalizerDiff / (decimal)Math.Pow(10, this.currentDispenser.TotalDecimalPlaces);

                sale.Volume = totalDiff;
                sale.TotalPrice = decimal.Round(totalDiff * sale.UnitPrice, 2);
                sale.Nozzle.Dispenser.CreateInvoice(sale, this.radCheckBox1.Checked);
            }

            VirtualDevices.VirtualNozzle nozzle = this.currentDispenser.Nozzles.Where(n=>n.NozzleId == sale.NozzleId).First();

            sale.InvalidSale = false;
            this.currentDispenser.AddSale(nozzle, sale.Volume, sale.TotalPrice);
            this.database.SaveChanges();
            if (this.currentDispenser.InvalidSales.Contains(sale.SalesTransactionId))
                this.currentDispenser.InvalidSales.Remove(sale.SalesTransactionId);
            if(this.currentDispenser.InvalidSales.Count == 0)
                this.currentDispenser.HasInvalidSale = false;

            this.Close();
        }

        private void radRadioButton1_ToggleStateChanged(object sender, StateChangedEventArgs args)
        {
            if (this.radRadioButton1.IsChecked)
            {
                this.volume1.Enabled = true;
                this.priceTotal1.Enabled = true;
                this.unitPrice1.Enabled = true;

                this.volume2.Enabled = false;
                this.priceTotal2.Enabled = false;
                this.unitPrice2.Enabled = false;
            }
            else
            {
                this.volume1.Enabled = false;
                this.priceTotal1.Enabled = false;
                this.unitPrice1.Enabled = false;

                this.volume2.Enabled = true;
                this.priceTotal2.Enabled = true;
                this.unitPrice2.Enabled = true;
            }
        }
    }
}
