using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace ASFuelControl.Common
{
    /// <summary>
    /// class that provides functionality that is used in fuel dispenser protocols
    /// </summary>
    public partial class FuelPoint : INotifyPropertyChanged
    {
        private Dictionary<string, object> extendedProperties = new Dictionary<string, object>();

        private int nozzleCount = 0;
        private List<Nozzle> nozzles = new List<Nozzle>();
        private int activeNozzle = -1;
        private decimal dispensedVolume = 0;
        private decimal dispensedAmount = 0;
        private int lastActiveNozzle = -1;
        private Enumerators.FuelPointStatusEnum currentStatus = Enumerators.FuelPointStatusEnum.Offline;
        private Enumerators.FuelPointStatusEnum status = Enumerators.FuelPointStatusEnum.Offline;

        public event PropertyChangedEventHandler PropertyChanged;

        public bool QueryStop { set; get; }
        public bool QueryStart { set; get; }
        public bool QueryTotals { set; get; }
        public bool QueryAuthorize { set; get; }
        public bool QuerySetPrice { set; get; }
        public bool QueryNozzle { set; get; }
        public bool QueryResume { set; get; }
        public bool QueryHalt { set; get; }
        public bool Initialized { set; get; }
        public bool Halted { set; get; }

        public decimal PresetVolume { set; get; }
        public decimal PresetAmount { set; get; }

        public int Channel { set; get; }
        public int Address { set; get; }
        public int EuromatNumber { set; get; }
        public int DispencerProtocol { set; get; }
        public int  NozzleCount
        {
            set 
            {
                if (this.nozzleCount == value)
                    return;
                this.nozzleCount = value;
                this.nozzles.Clear();
                for (int i = 0; i < this.nozzleCount; i++)
                {
                    Nozzle nz = new Nozzle();
                    nz.Index = i + 1;
                    nz.ParentFuelPoint = this;
                    nz.TotalVolume = -1;
                    this.nozzles.Add(nz);
                }
            }
            get { return this.nozzleCount; } 
        }
        public int ActiveNozzleIndex
        {
            set
            {
                if (this.activeNozzle == value)
                    return;
                if (value >= this.NozzleCount)
                    return;
                this.lastActiveNozzle = this.activeNozzle;
                this.activeNozzle = value;
                if (this.PropertyChanged != null)
                    this.PropertyChanged(this, new PropertyChangedEventArgs("ActiveNozzleIndex"));

            }
            get { return this.activeNozzle; }
        }
        public int LastActiveNozzleIndex
        {
            set
            {
                if (this.lastActiveNozzle == value)
                    return;
                if (value >= this.NozzleCount)
                    return;
                this.lastActiveNozzle = value;
                if (this.PropertyChanged != null)
                    this.PropertyChanged(this, new PropertyChangedEventArgs("LastActiveNozzleIndex"));

            }
            get { return this.lastActiveNozzle; }
        }
        public int ErrorCount { set; get; }

        public Enumerators.FuelPointStatusEnum DispenserStatus 
        {
            set 
            {
                if (this.currentStatus == value)
                    return;
                this.DispenserLastStatus = this.currentStatus;
                this.currentStatus = value;
                //if (this.currentStatus == Enumerators.FuelPointStatusEnum.Nozzle)
                //{
                //    //if (this.ActiveNozzle != null)
                //    //{
                //    //    this.ActiveNozzle.QueryTotals = true;
                //    //}
                //}
                if(this.PropertyChanged != null)
                    this.PropertyChanged(this, new PropertyChangedEventArgs("DispenserStatus"));
            }
            get { return this.currentStatus; } 
        }
        public Enumerators.FuelPointStatusEnum DispenserLastStatus { set; get; }

        public Enumerators.FuelPointStatusEnum Status
        {
            set
            {
                if (this.status == value)
                    return;
                this.LastStatus = this.status;
                this.status = value;
                if(this.PropertyChanged != null)
                    this.PropertyChanged(this, new PropertyChangedEventArgs("Status"));
            }
            get { return this.status; }
        }
        public Enumerators.FuelPointStatusEnum LastStatus { set; get; }
        
        public Nozzle ActiveNozzle
        {
            get 
            { 
                if(this.activeNozzle >= 0 && this.activeNozzle < this.nozzles.Count)
                    return this.nozzles[this.activeNozzle];
                return null;
            }
            set
            {
                if (this.activeNozzle >= 0 && value == this.nozzles[this.activeNozzle])
                    return;
                if(this.activeNozzle >= 0)
                    this.lastActiveNozzle = this.activeNozzle;
                if (value == null)
                {
                    this.activeNozzle = -1;
                    return;
                }
                this.ActiveNozzleIndex = value.Index - 1;
            }
        }
        public Nozzle LastActiveNozzle
        {
            get
            {
                if (this.lastActiveNozzle >= 0 && this.lastActiveNozzle < this.nozzles.Count)
                    return this.nozzles[this.lastActiveNozzle];
                return null;
            }
        }

        public DateTime LastValidResponse { set; get; }

        public int AmountDecimalPlaces { set; get; }
        public int VolumeDecimalPlaces { set; get; }
        public int UnitPriceDecimalPlaces { set; get; }
        public int TotalDecimalPlaces { set; get; }
        public Nozzle[] Nozzles { get { return this.nozzles.ToArray(); } }
        public FuelPointValues LastValues { set; get; }

        public bool IsEuromatEnabled { set; get; }

        public decimal DispensedVolume 
        {
            set
            {
                this.dispensedVolume = value;
            }
            get
            {
                return this.dispensedVolume;
            }
        }
        public decimal DispensedAmount 
        {
            set
            {
                this.dispensedAmount = value;
            }
            get
            {
                return this.dispensedAmount;
            }
        }

        public object GetExtendedProperty(string name)
        {
            if (!this.extendedProperties.ContainsKey(name))
                return null;
            return this.extendedProperties[name];
        }

        public object GetExtendedProperty(string name, object defaultValue)
        {
            if (!this.extendedProperties.ContainsKey(name))
                return defaultValue;
            return this.extendedProperties[name];
        }

        public void SetExtendedProperty(string name, object value)
        {
            if (!this.extendedProperties.ContainsKey(name))
                this.extendedProperties.Add(name, value);
            else
                this.extendedProperties[name] = value;
        }
    }
}
