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
            this.labelSerial.Text = nozzle.SerialNumber;
            this.nozzleTotalsLabel.Text = string.Format("{0:N2} Lt", (nozzle.LastVolumeCounter / (decimal)Math.Pow(10, nozzle.ParentDispenser.TotalDecimalPlaces)));
            foreach(VirtualDevices.VirtualTank t in nozzle.ConnectedTanks)
            {
                Label l = new Label();
                l.Text = "Δεξαμενή: " + t.SerialNumber;
                l.AutoSize = false;
                l.Font = this.labelSerial.Font;
                l.Dock = DockStyle.Fill;
                this.panel4.Controls.Add(l);
                l.Dock = DockStyle.Bottom;
                l.Height = this.labelSerial.Height;
            }
            this.Height = this.Height + (nozzle.ConnectedTanks.Length * this.labelSerial.Height);
        }
    }
}
