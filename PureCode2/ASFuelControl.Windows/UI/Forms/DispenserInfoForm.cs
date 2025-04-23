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
    public partial class DispenserInfoForm : RadForm
    {
        private VirtualDevices.VirtualDispenser currentDispenser;

        public DispenserInfoForm()
        {
            InitializeComponent();
        }

        public void LoadDispenserInfo(VirtualDevices.VirtualDispenser dispenser)
        {
            dispenserNameLab.Text = string.Format("Αντλία {0}", dispenser.DispenserNumber);
            int totHeight = 0;
            foreach (VirtualDevices.VirtualNozzle nozzle in dispenser.Nozzles)
            {
                UI.Controls.NozzleInfoControl nzControl = new Controls.NozzleInfoControl();
                this.panelNozzles.Controls.Add(nzControl);
                nzControl.Dock = DockStyle.Top;
                nzControl.BringToFront();
                nzControl.LoadNozzle(nozzle);
                totHeight = totHeight + nzControl.Height;
            }
            this.Height = 120 + totHeight + 10;
            this.currentDispenser = dispenser;
        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void radButton2_Click(object sender, EventArgs e)
        {
            
        }

        private void panel4_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Βεβαιωθήτε ότι όλα τα ακροσωλήνια είναι κλειστά", "ΠΡΟΣΟΧΗ", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == System.Windows.Forms.DialogResult.Cancel)
                return;
            this.currentDispenser.Reset = true;
        }
    }
}
