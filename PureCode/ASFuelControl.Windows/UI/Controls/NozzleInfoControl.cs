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
    public partial class NozzleInfoControl : UserControl
    {
        public NozzleInfoControl()
        {
            InitializeComponent();
        }

        public void LoadNozzle(VirtualDevices.VirtualNozzle nozzle)
        {
            this.nozzleNumberLabel.Text = nozzle.NozzleNumber.ToString();
            this.nozzleDescriptionLabel.Text = nozzle.FuelTypeDescription;
            this.nozzleTotalsLabel.Text = string.Format("{0:N2} Lt", (nozzle.TotalVolumeCounter / (decimal)Math.Pow(10, nozzle.ParentDispenser.TotalDecimalPlaces)));
        }
    }
}
