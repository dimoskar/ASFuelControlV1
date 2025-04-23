using ASFuelControl.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CreateTankFilling
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            ASFuelControl.Data.Implementation.OptionHandler.ConnectionString = Properties.Settings.Default.DBConnection;
            DatabaseModel.ConnectionString = Properties.Settings.Default.DBConnection;
            GetTanks();
        }

        private void GetTanks()
        {
            using (var database = new DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                var tanks = database.Tanks.OrderBy(t => t.TankNumber).ToList();
                this.comboBox1.DataSource = tanks;
                this.comboBox1.DisplayMember = "Description";
                this.comboBox1.ValueMember = "TankId";
                this.comboBox1.SelectedIndexChanged += ComboBox1_SelectedIndexChanged;
            }
            GetInvoiceLines();
        }

        private void ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            GetInvoiceLines();
        }

        private void GetInvoiceLines()
        {
            if (this.comboBox1.SelectedValue == null)
                return;
            Guid tankId = Guid.Parse(this.comboBox1.SelectedValue.ToString());
            using (var database = new DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                var tank = database.Tanks.FirstOrDefault(t => t.TankId == tankId);
                if (tank == null)
                    return;
                var q1 = database.InvoiceLines.Where(il =>
                !il.TankFillingId.HasValue &&
                il.Invoice.InvoiceType.TransactionType == 1 &&
                il.FuelTypeId == tank.FuelTypeId &&
                il.Invoice.TransactionDate.Date <= DateTime.Now &&
                il.Invoice.TransactionDate.Date >= DateTime.Now.AddDays(-10) &&
                il.Invoice.Number > 0 &&
                il.VolumeNormalized > 0 &&
                il.Temperature < 60 &&
                il.Volume > 0 &&
                il.FuelDensity > 500 &&
                il.FuelDensity < 900 &&
                il.Invoice.InvoiceType.DeliveryType.HasValue &&
                (
                    il.Invoice.InvoiceType.DeliveryType.Value == (int)ASFuelControl.Common.Enumerators.DeliveryTypeEnum.Delivery ||
                    il.Invoice.InvoiceType.DeliveryType.Value == (int)ASFuelControl.Common.Enumerators.DeliveryTypeEnum.Return ||
                    il.Invoice.InvoiceType.DeliveryType.Value == (int)ASFuelControl.Common.Enumerators.DeliveryTypeEnum.TransfusionIn
                ));
                this.comboBox2.DataSource = q1.Select(i=> new InvoiceData() { InvoiceDesc = i.Invoice.Series + i.Invoice.Number.ToString(), InvoiceLineId = i.InvoiceLineId, Volume = i.Volume }).ToList();
                this.comboBox2.DisplayMember = "Description";
                this.comboBox2.ValueMember = "InvoiceLineId";
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (this.comboBox2.SelectedValue == null)
                    return;
                Guid invLineId = Guid.Parse(this.comboBox2.SelectedValue.ToString());
                if (this.comboBox1.SelectedValue == null)
                    return;
                Guid tankId = Guid.Parse(this.comboBox1.SelectedValue.ToString());
                ASFuelControl.Common.Sales.TankFillingData filling = new ASFuelControl.Common.Sales.TankFillingData();

                filling.InvoiceLineId = invLineId;
                filling.TankId = tankId;
                filling.DeliveryStarted = DateTime.Now.AddMinutes(-10);
                filling.StartValues = new ASFuelControl.Common.TankValues()
                {
                    CurrentTemperatur = this.numericUpDown3.Value,
                    FuelHeight = this.numericUpDown1.Value,
                    FuelRipple = false,
                    LastMeasureTime = DateTime.Now.AddSeconds(-10),
                    WaterHeight = 0
                };
                filling.EndValues = new ASFuelControl.Common.TankValues()
                {
                    CurrentTemperatur = this.numericUpDown3.Value,
                    FuelHeight = this.numericUpDown2.Value,
                    FuelRipple = false,
                    LastMeasureTime = DateTime.Now.AddSeconds(-10),
                    WaterHeight = 0
                };
                using (var database = new DatabaseModel(Properties.Settings.Default.DBConnection))
                {
                    
                    Tank tank = database.Tanks.Where(t => t.TankId == filling.TankId).FirstOrDefault();
                    if (filling.InvoiceLineId == Guid.Empty)
                    {
                        filling.InvoiceLineId = tank.CreateFillingInvoice(filling);
                    }

                    TankFilling tf = tank.CreateTankFilling(filling.InvoiceLineId, filling.StartValues, filling.EndValues, filling.DeliveryStarted);
                    //tank.ReferenceLevel = tank.FuelLevel;
                    database.Add(tf);
                    database.SaveChanges();
                }
            }
            catch(Exception ex)
            {

            }
        }
    }
    public class InvoiceData
    {
        public Guid InvoiceLineId { set; get; }
        public decimal Volume { set; get; }
        public string InvoiceDesc { set; get; }
        public string Description
        {
            get
            {
                return InvoiceDesc + " " + Volume.ToString();
            }
        }
    }
}
