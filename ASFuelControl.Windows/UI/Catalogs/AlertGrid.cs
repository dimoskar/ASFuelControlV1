using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ASFuelControl.Windows.UI.Catalogs
{
    public partial class AlertGrid : UserControl
    {
        private delegate void UpdateControlDelegate(UI.Controls.AlarmControl c, VirtualDevices.VirtualBaseAlarm alarm);

        public event EventHandler<QueryCreateSaleEventArguments> QueryCreateSale;
        public event EventHandler QueryShowFillingNeededInfo;
        public event EventHandler QueryShowDecreasingNeededInfo;
        //public event EventHandler<QueryRemoveAlert> QueryRemovrControl;

        public int CurrentAlerts
        {
            get
            {
                return this.panelDispenser.Controls.Count + this.panelTanks.Controls.Count + this.panelGeneral.Controls.Count;
            }
        }

        public AlertGrid()
        {
            InitializeComponent();
        }

        public void UpdateAlert(VirtualDevices.VirtualBaseAlarm alarm)
        {
            if (this.IsDisposed)
                return;
            UI.Controls.AlarmControl control = this.panelDispenser.Controls.OfType<UI.Controls.AlarmControl>().Where(ac => ac.CurrentAlarm.DatabaseEntityId == alarm.DatabaseEntityId).FirstOrDefault();
            if (control == null)
            {
                control = this.panelGeneral.Controls.OfType<UI.Controls.AlarmControl>().Where(ac => ac.CurrentAlarm.DatabaseEntityId == alarm.DatabaseEntityId).FirstOrDefault();
            }
            if (control == null)
            {
                control = this.panelTanks.Controls.OfType<UI.Controls.AlarmControl>().Where(ac => ac.CurrentAlarm.DatabaseEntityId == alarm.DatabaseEntityId).FirstOrDefault();
            }
            
            this.Invoke(new UpdateControlDelegate(this.UpdateAlertControl), new object[] { control, alarm });
        }

        public void AddAlert(VirtualDevices.VirtualBaseAlarm alarm)
        {

            foreach (UI.Controls.AlarmControl aControl in this.panelDispenser.Controls)
            {
                if (aControl.CurrentAlarm.DatabaseEntityId == alarm.DatabaseEntityId && aControl.CurrentAlarm.AlertType == alarm.AlertType)
                {
                    return;
                }
            }
            foreach (UI.Controls.AlarmControl aControl in this.panelGeneral.Controls)
            {
                if (aControl.CurrentAlarm.DatabaseEntityId == alarm.DatabaseEntityId && aControl.CurrentAlarm.AlertType == alarm.AlertType)
                {
                    return;
                }
            }
            foreach (UI.Controls.AlarmControl aControl in this.panelTanks.Controls)
            {
                if (aControl.CurrentAlarm.DatabaseEntityId == alarm.DatabaseEntityId && aControl.CurrentAlarm.AlertType == alarm.AlertType)
                {
                    return;
                }
            }

            UI.Controls.AlarmControl control = new UI.Controls.AlarmControl();
            control.QueryCreateSaleEvent += new EventHandler(control_QueryCreateSaleEvent);

            if (alarm.GetType() == typeof(VirtualDevices.VirtualDispenserAlarm) || alarm.GetType() == typeof(VirtualDevices.VirtualNozzleAlarm))
            {
                this.panelDispenser.SuspendLayout();
                control.Width = this.panelDispenser.Width - 25;
                this.panelDispenser.Controls.Add(control);
                this.panelDispenser.ResumeLayout();
            }
            else if (alarm.AlertType == Common.Enumerators.AlarmEnum.BalanceDifference || alarm.AlertType == Common.Enumerators.AlarmEnum.ProgramTermination)
            {
                this.panelGeneral.SuspendLayout();
                control.Width = this.panelGeneral.Width - 25;
                this.panelGeneral.Controls.Add(control);
                this.panelGeneral.ResumeLayout();
            }
            else
            {
                this.panelTanks.SuspendLayout();
                control.Width = this.panelTanks.Width - 25;
                this.panelTanks.Controls.Add(control);
                this.panelTanks.ResumeLayout();
            }

                    //control.Height = 60;
            if (alarm.GetType() == typeof(VirtualDevices.VirtualBaseAlarm))
            {
                if (alarm.AlertType == Common.Enumerators.AlarmEnum.FuelIncrease)
                {
                    if (this.QueryShowFillingNeededInfo != null)
                        this.QueryShowFillingNeededInfo(alarm, new EventArgs());
                }
                if (alarm.AlertType == Common.Enumerators.AlarmEnum.FuelDecrease)
                {
                    if (this.QueryShowDecreasingNeededInfo != null)
                        this.QueryShowDecreasingNeededInfo(alarm, new EventArgs());
                }

            }
            else if (alarm.GetType() == typeof(VirtualDevices.VirtualNozzleAlarm))
            {
                control.DeviceType = "Ακροσωλήνιο";
            }
            else if (alarm.GetType() == typeof(VirtualDevices.VirtualTankFillingInfo))
            {
                control.DeviceType = "Παραλαβή";
            }
            else
            {
                int pos = alarm.MessageText.IndexOf("Σύνολο Πωλήσεων:");
                if (pos > 0)
                {
                    string str = alarm.MessageText.Substring(pos);
                    str = str.Replace("Σύνολο Πωλήσεων: ", "");
                    str = str.Replace("Lt", "");
                    decimal volume = 0;
                    if (decimal.TryParse(str, out volume))
                    {
                        if (volume < 1000)
                        {
                            control.QueryCreateSaleEvent -= new EventHandler(control_QueryCreateSaleEvent);
                            control.Dispose();
                            //this.panelDispenser.ResumeLayout();
                            //this.panelGeneral.ResumeLayout();
                            //this.panelTanks.ResumeLayout();
                            return;
                        }
                    }
                }
            }
            control.CurrentAlarm = alarm;
        }

        public void RemoveAlertControl(Guid databaseEntryId)
        {
            UI.Controls.AlarmControl ac1 = this.panelDispenser.Controls.OfType<UI.Controls.AlarmControl>().Where(c => c.CurrentAlarm.DatabaseEntityId == databaseEntryId).FirstOrDefault();
            if (ac1 != null)
            {
                this.panelDispenser.Controls.Remove(ac1);
                return;
            }
            UI.Controls.AlarmControl ac2 = this.panelTanks.Controls.OfType<UI.Controls.AlarmControl>().Where(c => c.CurrentAlarm.DatabaseEntityId == databaseEntryId).FirstOrDefault();
            if (ac2 != null)
            {
                this.panelTanks.Controls.Remove(ac2);
                return;
            }
            UI.Controls.AlarmControl ac3 = this.panelGeneral.Controls.OfType<UI.Controls.AlarmControl>().Where(c => c.CurrentAlarm.DatabaseEntityId == databaseEntryId).FirstOrDefault();
            if (ac3 != null)
            {
                this.panelGeneral.Controls.Remove(ac3);
                return;
            }
        }

        public void RemoveAlertControl(Control c, FlowLayoutPanel panel)
        {
            UI.Controls.AlarmControl ac = c as UI.Controls.AlarmControl;

            panel.Controls.Remove(c);
            try
            {
                //if (ac != null)
                //{
                //    PopupData data = this.popups.Where(p => p.AlertId == ac.CurrentAlarm.DatabaseEntityId).FirstOrDefault();
                //    if (data == null)
                //        return;
                //    data.PopupWindow.Hide();
                //    data.PopupWindow.Dispose();
                //    this.popups.Remove(data);
                //}
            }
            catch (Exception ex)
            {
                Logging.Logger.Instance.LogToFile("RemoveAlertControl", ex);
            }
        }

        //public void RefreshAlerts()
        //{
        //    List<UI.Controls.AlarmControl> toRemoveDisp = new List<UI.Controls.AlarmControl>();
        //    List<UI.Controls.AlarmControl> toRemoveTank = new List<UI.Controls.AlarmControl>();
        //    List<UI.Controls.AlarmControl> toRemoveGen = new List<UI.Controls.AlarmControl>();

        //    foreach (UI.Controls.AlarmControl ac in this.panelDispenser.Controls)
        //    {
        //        if (this.QueryRemovrControl != null)
        //        {
        //            QueryRemoveAlert args = new QueryRemoveAlert()
        //            { DatabaseEntryId = ac.CurrentAlarm.DatabaseEntityId, DeviceId = ac.CurrentAlarm.DeviceId };
        //            this.QueryRemovrControl(this, args);
        //            if (args.Remove)
        //                toRemoveDisp.Add(ac);
        //            continue;
        //        }
        //    }
        //    foreach (UI.Controls.AlarmControl ac in this.panelTanks.Controls)
        //    {
        //        if (this.QueryRemovrControl != null)
        //        {
        //            QueryRemoveAlert args = new QueryRemoveAlert()
        //            { DatabaseEntryId = ac.CurrentAlarm.DatabaseEntityId, DeviceId = ac.CurrentAlarm.DeviceId };
        //            this.QueryRemovrControl(this, args);
        //            if (args.Remove)
        //                toRemoveTank.Add(ac);
        //            continue;
        //        }
        //    }
        //    foreach (UI.Controls.AlarmControl ac in this.panelGeneral.Controls)
        //    {
        //        if (this.QueryRemovrControl != null)
        //        {
        //            QueryRemoveAlert args = new QueryRemoveAlert()
        //            { DatabaseEntryId = ac.CurrentAlarm.DatabaseEntityId, DeviceId = ac.CurrentAlarm.DeviceId };
        //            this.QueryRemovrControl(this, args);
        //            if (args.Remove)
        //                toRemoveGen.Add(ac);
        //            continue;
        //        }
        //    }
        //    foreach (UI.Controls.AlarmControl ac in toRemoveDisp)
        //        this.RemoveAlertControl(ac, this.panelDispenser);
        //    foreach (UI.Controls.AlarmControl ac in toRemoveTank)
        //        this.RemoveAlertControl(ac, this.panelTanks);
        //    foreach (UI.Controls.AlarmControl ac in toRemoveGen)
        //        this.RemoveAlertControl(ac, this.panelGeneral);
        //}

        public void ResumeAlertLayout()
        {
            this.panelDispenser.SuspendLayout();
            foreach (Control control in this.panelDispenser.Controls)
                control.Width = this.panelDispenser.Width - 25;
            this.panelDispenser.ResumeLayout();
            this.panelGeneral.SuspendLayout();
            foreach (Control control in this.panelGeneral.Controls)
                control.Width = this.panelDispenser.Width - 25;
            this.panelGeneral.ResumeLayout();
            this.panelTanks.SuspendLayout();
            foreach (Control control in this.panelTanks.Controls)
                control.Width = this.panelDispenser.Width - 25;
            this.panelTanks.ResumeLayout();
        }

        private void UpdateAlertControl(UI.Controls.AlarmControl c, VirtualDevices.VirtualBaseAlarm alarm)
        {
            if (c == null)
            {
                this.AddAlert(alarm);
                return;
            }
            c.CurrentAlarm = alarm;
        }

        void control_QueryCreateSaleEvent(object sender, EventArgs e)
        {
            VirtualDevices.VirtualNozzleAlarm alarm = sender as VirtualDevices.VirtualNozzleAlarm;
            VirtualDevices.VirtualAlarmData dataNow = alarm.Data.Where(d => d.PropertyName == "TotalVolumeCounter").FirstOrDefault();
            VirtualDevices.VirtualAlarmData dataPrev = alarm.Data.Where(d => d.PropertyName == "LastVolumeCounter").FirstOrDefault();
            VirtualDevices.VirtualAlarmData dataTotalDiff = alarm.Data.Where(d => d.PropertyName == "TotalDifference").FirstOrDefault();
            
            if (!(dataNow == null || dataPrev == null))
            {
                decimal nowVol = decimal.Parse(dataNow.Value);
                decimal prevVol = decimal.Parse(dataPrev.Value);
                if (dataTotalDiff != null)
                {
                    decimal totalDiff = decimal.Parse(dataTotalDiff.Value);
                    if (this.QueryCreateSale != null)
                        this.QueryCreateSale(this, new QueryCreateSaleEventArguments() { DeviceId = alarm.DeviceId, PreviousVolume = nowVol - totalDiff, CurrentVolume = nowVol });
                    return;
                }
                if (this.QueryCreateSale != null)
                    this.QueryCreateSale(this, new QueryCreateSaleEventArguments() { DeviceId = alarm.DeviceId, PreviousVolume = prevVol, CurrentVolume = nowVol });
            }
        }

        private void AlertGrid_VisibleChanged(object sender, EventArgs e)
        {
            if (!this.Visible)
                return;
            this.ResumeAlertLayout();
        }

        private void AlertGrid_SizeChanged(object sender, EventArgs e)
        {
            ResumeAlertLayout();
        }
    }

    public class QueryCreateSaleEventArguments : EventArgs
    {
        public Guid DeviceId { set; get; }
        public decimal PreviousVolume { set; get; }
        public decimal CurrentVolume { set; get; }
    }

    public class QueryRemoveAlert
    {
        public Guid DeviceId { set; get; }
        public Guid DatabaseEntryId { set; get; }
        public bool Remove { set; get; }
    }
}
