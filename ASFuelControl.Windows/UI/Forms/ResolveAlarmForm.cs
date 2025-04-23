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
        public event EventHandler QueryCreateSaleEvent;

        public VirtualDevices.VirtualBaseAlarm currentAlert;

        public VirtualDevices.VirtualBaseAlarm CurrentAlert
        {
            set 
            { 
                this.currentAlert = value;
                this.radLabel3.Text = this.currentAlert.DeviceDescription + "-" + this.currentAlert.MessageText;
                this.radGridView1.DataSource = this.currentAlert.Data.OrderBy(d=>d.Description);
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
                //this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
                //this.Close();
                return;
            }
            
            this.currentAlert.ResolveText = this.radTextBox1.Text;
            this.currentAlert.ResolvedTime = DateTime.Now;
            if (this.currentAlert.AlertType == Common.Enumerators.AlarmEnum.NozzleTotalError)
            {
                VirtualDevices.VirtualAlarmData dataNow = this.currentAlert.Data.Where(d => d.PropertyName == "TotalVolumeCounter").FirstOrDefault();
                VirtualDevices.VirtualAlarmData dataPrev = this.currentAlert.Data.Where(d => d.PropertyName == "LastVolumeCounter").FirstOrDefault();
                VirtualDevices.VirtualAlarmData volData = this.currentAlert.Data.Where(d => d.PropertyName == "VolumeDifference").FirstOrDefault();
                VirtualDevices.VirtualNozzle nz = Program.ApplicationMainForm.ThreadControllerInstance.GetDispensers().SelectMany(d => d.Nozzles).Where(n => n.NozzleId == currentAlert.DeviceId).FirstOrDefault();
                //VirtualDevices.VirtualNozzle = this.currentAlert.DeviceId
                //decimal totalDiff = (decimal.Parse(dataNow.Value) - decimal.Parse(dataPrev.Value)) / (decimal)Math.Pow(10, nz.ParentDispenser.TotalDecimalPlaces);
                decimal volDiff = decimal.Parse(volData.Value);
                if (volDiff < 0)
                {
                    //Telerik.WinControls.RadMessageBox.Show("Ο όγκος προς τιμολόγηση είναι αρνητικός", "Σφάλμα δημιουργίας Παραστατικού", MessageBoxButtons.OK, Telerik.WinControls.RadMessageIcon.Exclamation);
                    this.DialogResult = System.Windows.Forms.DialogResult.OK;
                    Threads.AlertChecker.Instance.ResolveAlert(this.currentAlert.DatabaseEntityId, this.radTextBox1.Text);
                    this.Close();
                    return;
                }

                long liters = (long)volDiff;
                long mls = (long)(volDiff * 1000 - ((long)volDiff) * 1000);
                string message = string.Format("Θέλετε να καταχωρηθεί πώληση {0} Lt {1} ml;\r\nΠροσοχή θα δημιουργηθεί απόδειξη πώλησης", liters, mls);
                DialogResult res = Telerik.WinControls.RadMessageBox.Show(message, "Εκτύπωση Παραστατικού", MessageBoxButtons.YesNo, Telerik.WinControls.RadMessageIcon.Question, MessageBoxDefaultButton.Button2);
                if (res == System.Windows.Forms.DialogResult.Yes)
                {
                    if (this.QueryCreateSaleEvent != null)
                    {
                        this.QueryCreateSaleEvent(this.currentAlert, new EventArgs());
                    }
                }
            }
            
            Threads.AlertChecker.Instance.ResolveAlert(this.currentAlert.DatabaseEntityId, this.radTextBox1.Text);
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
