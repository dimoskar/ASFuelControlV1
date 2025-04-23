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
    public partial class AlarmControl : UserControl
    {
        private bool disableResolve = false;

        public event EventHandler<Threads.QueryResolveAlarmArgs> QueryResolveAlert;

        private VirtualDevices.VirtualBaseAlarm currentAlarm = null;
        
        public VirtualDevices.VirtualBaseAlarm CurrentAlarm 
        {
            set 
            { 
                this.currentAlarm = value;
                if (Program.CurrentUserLevel != Common.Enumerators.ApplicationUserLevelEnum.Administrator)
                    disableResolve = false;
                else
                {
                    if (this.currentAlarm.GetType() == typeof(VirtualDevices.VirtualTankFillingInfo))
                    {
                        disableResolve = true;
                    }
                    else
                    {
                        if (this.currentAlarm.AlertType == Common.Enumerators.AlarmEnum.FuelTooHigh || this.currentAlarm.AlertType == Common.Enumerators.AlarmEnum.FuelTooLow || this.currentAlarm.AlertType == Common.Enumerators.AlarmEnum.WaterTooHigh)
                        {
                            this.label1.Cursor = Cursors.Default;
                            this.label3.Cursor = Cursors.Default;
                            disableResolve = true;
                        }
                        else
                        {
                            this.label1.Cursor = Cursors.Hand;
                            this.label3.Cursor = Cursors.Hand;
                            disableResolve = false;
                        }
                    }
                }
                this.ApplyText();
            }
            get { return this.currentAlarm; }
        }
        public string DeviceType { set; get; }
        public string Message { set; get; }
        public DateTime AlarmDateTime { set; get; }

        public AlarmControl()
        {
            InitializeComponent();
        }

        private void ApplyText()
        {
            this.label1.Text = this.CurrentAlarm.DeviceDescription;
            this.label3.Text = this.CurrentAlarm.AlarmTime.ToString("dd/MM/yyyy HH:mm");
            this.label2.Text = this.CurrentAlarm.MessageText;
            if (this.currentAlarm.GetType() == typeof(VirtualDevices.VirtualTankFillingInfo))
            {
                radPanel1.BackColor1 = Color.FromArgb(0, 87, 242);
                radPanel1.BackColor2 = Color.FromArgb(0, 6, 98);
                radPanel1.ApplyBackColor();
            }

        }

        private Common.Enumerators.ApplicationUserLevelEnum VerifyUser()
        {
            if (Program.AdminConnected)
                return Common.Enumerators.ApplicationUserLevelEnum.Administrator;
            using (UI.Forms.UserVerificationForm uvf = new UI.Forms.UserVerificationForm())
            {
                uvf.ShowDialog();
                return uvf.CurrentUserLevel;
            }
        }

        private void Resolve_Click(object sender, EventArgs e)
        {
            if (this.disableResolve)
                return;

            Common.Enumerators.ApplicationUserLevelEnum ul = this.VerifyUser();
            if (ul != Common.Enumerators.ApplicationUserLevelEnum.Administrator)
                return;
            
            Forms.ResolveAlarmForm raf = new Forms.ResolveAlarmForm();
            raf.CurrentAlert = this.currentAlarm;
            raf.Show(this);
            raf.FormClosed += new FormClosedEventHandler(raf_FormClosed);
        }

        void raf_FormClosed(object sender, FormClosedEventArgs e)
        {
            Forms.ResolveAlarmForm raf = sender as Forms.ResolveAlarmForm;
            if (raf.DialogResult == DialogResult.Cancel)
                return;
            if (this.currentAlarm.ResolveText == null || this.currentAlarm.ResolveText == "")
                return;
            if (this.QueryResolveAlert != null)
            {
                Threads.ResolveAlarmData data = new Threads.ResolveAlarmData();
                data.AlamId = this.currentAlarm.DatabaseEntityId;
                data.ResolveDateTime = this.currentAlarm.AlarmTime;
                data.ResolveText = this.currentAlarm.ResolveText;
                this.QueryResolveAlert(this, new Threads.QueryResolveAlarmArgs() { Alarms = new Threads.ResolveAlarmData[] { data } });
            }
        }
    }
}
