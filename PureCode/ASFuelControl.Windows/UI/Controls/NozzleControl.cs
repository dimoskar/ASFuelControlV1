using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ASFuelControl.Windows.UI.Controls
{
    public partial class NozzleControl : UserControl
    {
        bool selected = false;
        VirtualDevices.VirtualNozzle currentNozzle = null;

        public VirtualDevices.VirtualNozzle CurrentNozzle 
        {
            set 
            { 
                this.currentNozzle = value;
                this.label1.Text = this.currentNozzle.FuelTypeDescription;
                string tankDesc = "Δεξαμενή : ";
                if (this.currentNozzle.ConnectedTanks.Length > 1)
                    tankDesc = "Δεξαμενές : ";
                foreach (VirtualDevices.VirtualTank tank in this.currentNozzle.ConnectedTanks)
                {
                    tankDesc = tankDesc + tank.TankNumber;
                    if (tank != this.currentNozzle.ConnectedTanks.Last())
                        tankDesc = tankDesc + ", ";
                }
                this.label2.Text = tankDesc;
                this.label3.Text = this.currentNozzle.NozzleOfficialNumber.ToString();
                this.SetBackgroundColors();
            }
            get { return this.currentNozzle; }
        }

        public NozzleControl()
        {
            InitializeComponent();
        }

        private void SetBackgroundColors()
        {
            if (this.currentNozzle.Status == Common.Enumerators.NozzleStateEnum.LiterCheck)
            {
                this.BackColor = Color.Orange;
                this.label1.ForeColor = Color.White;
                this.label2.ForeColor = Color.White;
                this.label3.ForeColor = Color.White;
                this.label4.ForeColor = Color.White;
                this.label4.Text = "(Λιτρομέτρηση)";
            }
            else if (this.currentNozzle.Status == Common.Enumerators.NozzleStateEnum.Locked)
            {
                this.BackColor = Color.Red;
                this.label1.ForeColor = Color.White;
                this.label2.ForeColor = Color.White;
                this.label3.ForeColor = Color.White;
                this.label4.ForeColor = Color.White;
                this.label4.Text = "(Κλειδωμένο)";
            }
            else
            {
                this.BackColor = Color.White;
                this.label1.ForeColor = Color.Black;
                this.label2.ForeColor = Color.Black;
                this.label3.ForeColor = Color.Black;
                this.label4.ForeColor = Color.Black;
                this.label4.Text = "(Πώληση)";
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {
            if (this.currentNozzle.Status == Common.Enumerators.NozzleStateEnum.Normal)
            {
                this.currentNozzle.Status = Common.Enumerators.NozzleStateEnum.LiterCheck;
            }
            else if (this.currentNozzle.Status == Common.Enumerators.NozzleStateEnum.Locked)
            {
                this.currentNozzle.Status = Common.Enumerators.NozzleStateEnum.Normal;
            }
            else if (this.currentNozzle.Status == Common.Enumerators.NozzleStateEnum.LiterCheck)
            {
                this.currentNozzle.Status = Common.Enumerators.NozzleStateEnum.Normal;
            }
            this.SetBackgroundColors();
        }
    }
}
