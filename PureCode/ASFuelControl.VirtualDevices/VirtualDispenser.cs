using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.VirtualDevices
{
    public class VirtualDispenser : VirtualDevice
    {
        public event EventHandler QueryStart;
        public event EventHandler QueryStop;

        #region Private Variables

        private VirtualNozzle activeNozzle = null;
        
        private Guid nextSaleVehicleId = Guid.Empty;
        private Guid nextSaleTraderId = Guid.Empty;
        private Guid invoiceTypeId = Guid.Empty;
        private Common.Enumerators.FuelPointStatusEnum status = Common.Enumerators.FuelPointStatusEnum.Offline;
        private Common.Enumerators.FuelPointStatusEnum targetStatus = Common.Enumerators.FuelPointStatusEnum.Offline;
        private VirtualNozzle[] nozzles = null;
        private Queue<DispenserSale> lastSales = new Queue<DispenserSale>();

        #endregion

        #region Public Properties

        public VirtualNozzle[] Nozzles 
        {
            set 
            { 
                this.nozzles = value;
                if (nozzles != null)
                {
                    foreach(VirtualNozzle nozzle in nozzles)
                        nozzle.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(nozzle_PropertyChanged);
                }
            }
            get { return this.nozzles.OrderBy(n=>n.NozzleNumber).ToArray(); }
        }

        public bool IsValid { set; get; }
        public bool HasInvalidSale { set; get; }
        public List<Guid> InvalidSales { set; get; }
        public bool Initialized { set; get; }
        public bool TotalsMisfunction { set; get; }
        public VirtualNozzle ActiveNozzle 
        {
            set 
            {
                if (activeNozzle == value)
                    return;
                this.LastActiveNozzle = this.activeNozzle;
                this.activeNozzle = value; 
            }
            get { return this.activeNozzle; }
        }
        public VirtualNozzle LastActiveNozzle { set; get; }

        public Guid DispenserId { set; get; }
        public Guid LastValuesId { set; get; }
        public ASFuelControl.Common.IController Controller
        {
            get;
            set;
        }
        public int DispenserNumber { set; get; }
        public int OfficialNumber { set; get; }
        public int EuromatNumber { set; get; }
        public int AddressId { set; get; }
        public int ChannelId { set; get; }
        public int DecimalPlaces { set; get; }
        public int VolumeDecimalPlaces { set; get; }
        public int UnitPriceDecimalPlaces { set; get; }
        public int TotalDecimalPlaces { set; get; }
        public bool Reset 
        {
            set 
            {
                this.OnPropertyChanged(this, "Reset");
            }
        }
        public Common.FuelPointValues LastValues { set; get; }
        public Guid NextSaleVehicle
        {
            set
            {
                if (this.nextSaleVehicleId == value)
                    return;
                this.nextSaleVehicleId = value;
                this.OnPropertyChanged(this, "NextSaleVehicle");
            }
            get { return this.nextSaleVehicleId; }
        }
        public Guid NextSaleTrader
        {
            set
            {
                if (this.nextSaleTraderId == value)
                    return;
                this.nextSaleTraderId = value;
                this.OnPropertyChanged(this, "NextSaleTrader");
            }
            get { return this.nextSaleTraderId; }
        }
        public Guid InvoiceTypeId
        {
            set
            {
                if (this.invoiceTypeId == value)
                    return;
                this.invoiceTypeId = value;
                this.OnPropertyChanged(this, "InvoiceTypeId");
            }
            get { return this.invoiceTypeId; }
        }
        public Common.Enumerators.FuelPointStatusEnum Status
        {
            set
            {
                if (this.status == value)
                    return;
                this.status = value;
                this.OnPropertyChanged(this, "Status");
            }
            get { return this.status; }
        }
        public bool ManualyStart { set; get; }


        public Common.Enumerators.FuelPointStatusEnum TargetStatus
        {
            set
            {
                if (this.targetStatus == value)
                    return;
                this.targetStatus = value;
                this.OnPropertyChanged(this, "TargetStatus");
            }
            get { return this.targetStatus; }
        }

        public List<DispenserSale> LastSales
        {
            get { return this.lastSales.OrderByDescending(ds=>ds.SaleDate).ToList(); }
        }

        #endregion

        public VirtualDispenser()
        {
            this.nozzles = new VirtualNozzle[] { };
            this.InvalidSales = new List<Guid>();

        }

        public void Start()
        {
            if (this.QueryStart != null)
                this.QueryStart(this, new EventArgs());
        }

        public void Stop()
        {
            if (this.QueryStop != null)
                this.QueryStop(this, new EventArgs());
        }

        public void AddSale(VirtualNozzle nozzle, decimal totalVolume, decimal totalPrice)
        {
            lock (this.lastSales)
            {
                DispenserSale ds = new DispenserSale();
                ds.SaleDate = DateTime.Now;
                ds.FuelDescription = nozzle.NozzleOfficialNumber.ToString() + " - " + nozzle.FuelTypeDescription;
                ds.TotalPrice = totalPrice;
                ds.TotalVolume = totalVolume;
                if (lastSales.Count >= 3)
                    lastSales.Dequeue();
                this.lastSales.Enqueue(ds);
                this.OnPropertyChanged(this, "LastSales");
            }
        }

        protected override void OnPropertyChanged(object sender, string propertyName)
        {
            base.OnPropertyChanged(sender, propertyName);
            if (propertyName == "DeviceLocked")
            {
                this.SetRegistryKey(this.DispenserId, this.DeviceLocked);
            }
        }

        void nozzle_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            this.HasChanges = true;
        }
    }

    public class DispenserSale
    {
        public string FuelDescription { set; get; }
        public decimal TotalVolume { set; get; }
        public decimal TotalPrice { set; get; }
        public DateTime SaleDate { set; get; }
    }
}
