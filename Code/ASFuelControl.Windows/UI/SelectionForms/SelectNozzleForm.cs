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
    public partial class SelectNozzleForm : RadForm
    {
        public VirtualDevices.VirtualNozzle SelectedNozzle
        {
            set;
            get;
        }

        public SelectNozzleForm()
        {
            InitializeComponent();
        }

        public void LoadDispenser(VirtualDevices.VirtualDispenser dispenser)
        {
            this.panel2.Controls.Clear();
            int h = 0;
            foreach (VirtualDevices.VirtualNozzle nozzle in dispenser.Nozzles)
            {
                UI.Controls.NozzleControl nc = new Controls.NozzleControl();
                this.panel2.Controls.Add(nc);
                nc.Dock = DockStyle.Top;
                nc.CurrentNozzle = nozzle;
                nc.BringToFront();
                h = h + nc.Height;
            }
            this.Height = h + 70;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Close();
        }
    }
}
