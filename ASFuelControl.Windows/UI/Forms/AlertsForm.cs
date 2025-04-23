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
    public partial class AlertsForm : RadForm
    {
        Data.DatabaseModel database = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);

        public AlertsForm()
        {
            InitializeComponent();
            this.systemEventRadGridView.Visible = true;
            this.radDateTimePicker1.Value = DateTime.Today;
            this.radDateTimePicker2.Value = DateTime.Today;
            this.radDateTimePicker1.ValueChanged += new EventHandler(radDateTimePicker1_ValueChanged);
            this.radDateTimePicker2.ValueChanged += new EventHandler(radDateTimePicker1_ValueChanged);
            this.systemEventBindingSource.CurrentChanged += new EventHandler(systemEventBindingSource_CurrentChanged);
            this.LoadData();
            this.Disposed += AlertsForm_Disposed;
        }

        private void AlertsForm_Disposed(object sender, EventArgs e)
        {
            this.database.Dispose();
        }

        void systemEventBindingSource_CurrentChanged(object sender, EventArgs e)
        {
            Data.SystemEvent alert = this.systemEventBindingSource.Current as Data.SystemEvent;
            if (alert == null)
            {
                this.radButton1.Hide();
                return;
            }
            if (alert.EventType == (int)Common.Enumerators.AlertTypeEnum.FuelPointError)
                this.radButton1.Show();
            else
                this.radButton1.Hide();

        }

        void radDateTimePicker1_ValueChanged(object sender, EventArgs e)
        {
            this.LoadData();
        }

        public void LoadData()
        {   
            var q = this.database.SystemEvents.Where(e => e.EventDate.Date <= this.radDateTimePicker2.Value && e.EventDate.Date >= this.radDateTimePicker1.Value);
            if (q.Count() > 0)
            {
                this.systemEventBindingSource.DataSource = q.OrderByDescending(se => se.EventDate);
                this.systemEventDatumBindingSource.DataSource = this.systemEventBindingSource;
                this.systemEventDatumBindingSource.DataMember = "SystemEventData";
                this.systemEventBindingSource.ResetBindings(false);
                this.systemEventDatumBindingSource.ResetBindings(false);
            }
        }

        private void CreateSale(Guid nozzleId, decimal startTotal, decimal endTotal)
        {
            try
            {
                Data.Nozzle nozzle = this.database.Nozzles.Where(n => n.NozzleId == nozzleId).FirstOrDefault();
                if (nozzle == null)
                {
                    ASFuelControl.Logging.Logger.Instance.LogToFile("controllerThread_SaleAvaliable", string.Format("Nozzle:{0} not found", nozzleId));
                    return;
                }
                Common.Sales.SaleData sale = new Common.Sales.SaleData();
                sale.ErrorResolving = false;
                sale.FuelTypeDescription = nozzle.FuelType.Name;
                try
                {
                    sale.InvoiceTypeId = nozzle.Dispenser.InvoicePrints.First().DefaultInvoiceType;
                }
                catch (Exception ex)
                {
                    Logging.Logger.Instance.LogToFile("ThreadController::CreateSale", ex);
                    return;
                }
                sale.IsOnSale = false;
                sale.LiterCheck = false;
                sale.NozzleId = nozzleId;
                sale.NozzleNumber = nozzle.OfficialNozzleNumber;
                sale.SaleCompleted = true;
                sale.SaleEndTime = DateTime.Now;
                sale.TotalizerStart = startTotal;
                sale.TotalizerEnd = endTotal;
                sale.TotalVolume = (endTotal - startTotal) / 100;
                sale.UnitPrice = nozzle.FuelType.GetCurrentPrice();
                sale.TotalPrice = sale.UnitPrice * sale.TotalVolume;

                List<Common.Sales.TankSaleData> tds = new List<Common.Sales.TankSaleData>();
                foreach (Data.NozzleFlow nf in nozzle.NozzleFlows)
                {
                    if (nf.FlowState != 1)
                        continue;
                    Common.Sales.TankSaleData td = new Common.Sales.TankSaleData();
                    td.StartLevel = nf.Tank.FuelLevel;
                    td.StartTemperature = nf.Tank.Temperatire;
                    td.StartWaterLevel = nf.Tank.WaterLevel;
                    td.EndLevel = nf.Tank.FuelLevel;
                    td.EndTemperature = nf.Tank.Temperatire;
                    td.EndWaterLevel = nf.Tank.WaterLevel;
                    td.ReadyToProcess = true;
                    td.TankId = nf.TankId;

                    tds.Add(td);
                }
                sale.TankData = tds.ToArray();
            }
            catch (Exception ex)
            {
                Logging.Logger.Instance.LogToFile("ThreadController::CreateSale", ex);
            }
        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            Data.SystemEvent alert = this.systemEventBindingSource.Current as Data.SystemEvent;
            if (alert == null)
            {
                this.radButton1.Hide();
            }
            if (alert.EventType != (int)Common.Enumerators.AlertTypeEnum.FuelPointError)
                return;


            Data.SystemEventDatum dataNow = alert.SystemEventData.Where(d => d.PropertyName == "TotalVolumeCounter").FirstOrDefault();
            Data.SystemEventDatum dataPrev = alert.SystemEventData.Where(d => d.PropertyName == "LastVolumeCounter").FirstOrDefault();
            if (!(dataNow == null || dataPrev == null))
            {
                decimal nowVol = decimal.Parse(dataNow.Value);
                decimal prevVol = decimal.Parse(dataPrev.Value);

                decimal totalDiff = (decimal.Parse(dataNow.Value) - decimal.Parse(dataPrev.Value)) / 100;
                long liters = (long)totalDiff;
                int mls = (int)(totalDiff * 1000 - ((int)totalDiff) * 1000);
                if (totalDiff < 0)
                {
                    RadMessageBox.Show("Ο όγκος προς τιμολόγηση είναι αρνητικός", "Σφάλμα δημιουργίας Παραστατικού", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
                    return;
                }
                string message = string.Format("Θέλετε να καταχωρηθεί πώληση {0} Lt {1} ml;\r\nΠροσοχή θα δημιουργηθεί απόδειξη πώλησης", liters, mls);
                DialogResult res = RadMessageBox.Show(message, "Εκτύπωση Παραστατικού", MessageBoxButtons.YesNo, RadMessageIcon.Question, MessageBoxDefaultButton.Button2);
                if (res == System.Windows.Forms.DialogResult.Yes)
                    Program.ApplicationMainForm.CreateSale(alert.NozzleId.Value, prevVol, nowVol);
            }
        }
    }
}
