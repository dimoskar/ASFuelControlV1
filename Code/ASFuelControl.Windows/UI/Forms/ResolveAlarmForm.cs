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
    public partial class ResolveAlarmForm : RadForm
    {
        public VirtualDevices.VirtualBaseAlarm currentAlert;

        public VirtualDevices.VirtualBaseAlarm CurrentAlert
        {
            set 
            { 
                this.currentAlert = value;
                this.radLabel3.Text = this.currentAlert.DeviceDescription + "-" + this.currentAlert.MessageText;
                this.radGridView1.DataSource = this.currentAlert.Data;
            }
            get { return this.currentAlert; }
        }

        public ResolveAlarmForm()
        {
            InitializeComponent();
        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            if (this.radTextBox1.Text.Length == 0)
            {
                this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
                this.Close();
                return;
            }
            this.currentAlert.ResolveText = this.radTextBox1.Text;
            this.currentAlert.ResolvedTime = DateTime.Now;
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.Close();
        }

        private void radButton2_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Close();
        }
    }
}
