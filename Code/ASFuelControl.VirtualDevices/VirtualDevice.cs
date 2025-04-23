using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace ASFuelControl.VirtualDevices
{
    public class VirtualDevice : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private bool hasChanges = false;
        private DateTime lastValuesUpdate;

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

        public List<string> PropertiesChanged = new List<string>();

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
    }
}
