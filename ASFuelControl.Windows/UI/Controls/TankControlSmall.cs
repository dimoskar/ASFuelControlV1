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
    public partial class TankControlSmall : UserControl
    {
        private VirtualDevices.VirtualTank tank;

        public VirtualDevices.VirtualTank Tank
        {
            set 
            { 
                this.tank = value;
                this.radLabel1.Text = "Δεξαμενή " + tank.TankNumber.ToString() + "-" + tank.FuelTypeDescription;
                this.radLabel2.Text = decimal.Round((decimal)0.9 * tank.TotalVolume - tank.CurrentVolume, 0).ToString();
            }
            get { return this.tank; }
        }

        public TankControlSmall()
        {
            InitializeComponent();
        }
    }
}
