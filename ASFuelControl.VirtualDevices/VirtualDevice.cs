using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Microsoft.Win32;

namespace ASFuelControl.VirtualDevices
{
    public class VirtualDevice : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler AlertAdded;
        public event EventHandler AlertRemoved;

        private bool hasChanges = false;
        private bool updateUI = false;
        private bool deviceLocked = false;
        private DateTime lastValuesUpdate;
        private List<VirtualBaseAlarm> alerts = new List<VirtualBaseAlarm>();
        private List<VirtualBaseAlarm> suspectedAlerts = new List<VirtualBaseAlarm>();

        public bool DeviceLocked 
        {
            set 
            { 
                this.deviceLocked = value;
                this.OnPropertyChanged(this, "DeviceLocked");
            }
            get { return this.deviceLocked; }
        }
        public bool SuspendChangeEvent { set; get; }
        public DateTime LastValuesUpdate 
        {
            set 
            { 
                this.lastValuesUpdate = value;
                this.OnPropertyChanged(this, "LastValuesUpdate");
            }
            get { return this.lastValuesUpdate; }
        }
        public DateTime LastIdleTime { set;get;}
        public List<string> PropertiesChanged = new List<string>();
        public VirtualBaseAlarm[] Alerts
        {
            get
            {
                return this.alerts.ToArray();
            }
        }
        public int ErrorIndex { set; get; }

        public bool UpdateUI 
        {
            set 
            {
                if (this.updateUI == value)
                    return;
                this.updateUI = value;
                this.OnPropertyChanged(this, "UpdateUI");
            }
            get { return this.updateUI; }
        }

        public bool HasChanges
        {
            set
            {
                this.hasChanges = value;
                if (!this.hasChanges)
                    this.PropertiesChanged.Clear();
            }
            get { return this.hasChanges; }
        }

        public void AddAlert(VirtualBaseAlarm alert)
        {
            VirtualBaseAlarm curAlarm = this.alerts.Where(a => a.AlertType == alert.AlertType).FirstOrDefault();
            if (curAlarm != null)
                return;
            VirtualBaseAlarm alarm = this.suspectedAlerts.Where(a => a.AlertType == alert.AlertType).FirstOrDefault();

            if (alarm == null && !alert.ExistingAlarm)
                this.suspectedAlerts.Add(alert);
            else if (alarm == null)
            {
                this.alerts.Add(alert);
                if (this.AlertAdded != null)
                    this.AlertAdded(alert, new EventArgs());
            }
            else
            {
                if (alarm.DiscoverIndex < 3)
                    alarm.DiscoverIndex++;
                else
                {
                    this.suspectedAlerts.Remove(alarm);
                    this.alerts.Add(alarm);
                    if (this.AlertAdded != null)
                        this.AlertAdded(alarm, new EventArgs());
                }
            }
        }

        public void RemoveAlert(VirtualBaseAlarm alert)
        {
            if (this.alerts.Contains(alert))
                this.alerts.Remove(alert);
        }

        public void ClearAlerts()
        {

            List<VirtualBaseAlarm> oldAlerts = new List<VirtualBaseAlarm>();
            oldAlerts.AddRange(this.alerts.Where(a=>a.ResolvedTime.Year > 1900).ToArray());
            foreach (VirtualBaseAlarm alert in oldAlerts)
            {
                this.alerts.Remove(alert);
                if (this.AlertRemoved != null)
                    this.AlertRemoved(alert, new EventArgs());
            }
        }

        public bool HasAlerts
        {
            get { return this.alerts.Count > 0; }
        }

        protected virtual void OnPropertyChanged(object sender, string propertyName)
        {
            try
            {
                this.HasChanges = true;
                if (!SuspendChangeEvent)
                {
                    if (this.PropertyChanged != null)
                        this.PropertyChanged(sender, new PropertyChangedEventArgs(propertyName));
                }
                if (this.PropertiesChanged.Contains(propertyName))
                    return;
                this.PropertiesChanged.Add(propertyName);
            }
            catch
            {
            }
        }

        protected void SetRegistryKey(Guid deviceId, bool value)
        {
            try
            {
                RegistryKey key = Registry.CurrentUser.CreateSubKey("AsFuelControl");
                key = key.CreateSubKey(deviceId.ToString());
                key.SetValue("DeviceLocked", value);
            }
            catch
            {

            }
        }

        public bool IsLocked(Guid deviceId)
        {
            try
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey("AsFuelControl");
                key = key.OpenSubKey(deviceId.ToString());
                return bool.Parse(key.GetValue("DeviceLocked").ToString());
            }
            catch
            {
                return true;
            }
        }
    }
}
