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
        private bool oTPEnabled = false;
        private Enumerators.FuelPointStatusEnum currentStatus = Enumerators.FuelPointStatusEnum.Offline;
        private Enumerators.FuelPointStatusEnum status = Enumerators.FuelPointStatusEnum.Offline;
        private bool presetRaised = false;

        public event PropertyChangedEventHandler PropertyChanged;

        public string EuromatTransactionParameters { set; get; }
        public Euromat.Transaction EuromatTransaction { set; get; }
        public string EuromatIP { set; get; }
        public int EuromatPort { set; get; }
        public bool QueryStop { set; get; }
        public bool QueryStart { set; get; }
        public bool QueryTotals { set; get; }
        public bool QueryAuthorize { set; get; }
        
        public bool QuerySetPrice { set; get; }
        public bool QueryNozzle { set; get; }
        public bool QueryResume { set; get; }
        public bool QueryHalt { set; get; }
        public bool Initialized { set; get; }

        private bool halted = false;
        public bool Halted
        {
            set { this.halted = value; }
            get { return this.halted; }
        }

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

                if(value < 0)
                {
                    try
                    {
                        if (this.IsEuromatEnabled && this.EuromatTransaction != null)
                        {
                            // &&
                            //(
                            //    this.EuromatTransaction.TransactionState == Euromat.TransactionStateEnum.NzOpenAckRecieved ||
                            //    this.EuromatTransaction.TransactionState == Euromat.TransactionStateEnum.CtRecieved ||
                            //    this.EuromatTransaction.TransactionState == Euromat.TransactionStateEnum.DtRecieved
                            //)

                            // NzOpenAck,
                            //NzOpenAckRecieved,
                            //NzCloseAck,
                            //NzCloseAckRecieved,
                            //CtRecieved,
                            //DtRecieved,

                            if (this.EuromatTransaction.TransactionState == Euromat.TransactionStateEnum.NzOpenAck ||
                                this.EuromatTransaction.TransactionState == Euromat.TransactionStateEnum.NzOpenAckRecieved ||
                                this.EuromatTransaction.TransactionState == Euromat.TransactionStateEnum.CtRecieved ||
                                this.EuromatTransaction.TransactionState == Euromat.TransactionStateEnum.DtRecieved)
                            {
                                bool sent2 = this.EuromatTransaction.SentNzDown();
                                int i = 0;
                                while (!sent2)
                                {
                                    if (!
                                        (   this.EuromatTransaction.TransactionState == Euromat.TransactionStateEnum.NzOpenAck ||
                                            this.EuromatTransaction.TransactionState == Euromat.TransactionStateEnum.NzOpenAckRecieved ||
                                            this.EuromatTransaction.TransactionState == Euromat.TransactionStateEnum.CtRecieved ||
                                            this.EuromatTransaction.TransactionState == Euromat.TransactionStateEnum.DtRecieved
                                         ))
                                        break;

                                    sent2 = this.EuromatTransaction.SentNzDown();
                                    System.Threading.Thread.Sleep(500);
                                    i++;
                                    if (i > 4)
                                        break;
                                }
                            }
                        }
                    }
                    catch(Exception ex)
                    {

                    }
                }
                else
                {
                    if (this.IsEuromatEnabled)
                    {
                        if (this.EuromatTransaction == null)
                        {
                            this.EuromatTransaction = new Euromat.Transaction()
                            {
                                FuelPoint = this.EuromatNumber,
                                Nozzle = value + 1,
                                IPAddress = this.EuromatIP,
                                Port = this.EuromatPort
                            };
                            this.EuromatTransaction.TransactionCompleted += EuromatTransaction_TransactionCompleted;
                            this.EuromatTransaction.TransactionAllowed += EuromatTransaction_TransactionAllowed;
                        }

                        //if (this.EuromatTransaction.TransactionState == Euromat.TransactionStateEnum.NzCloseAckRecieved)
                        //{
                        //    bool sent2 = this.EuromatTransaction.SendFt(0, 0, 0);
                        //    while (!sent2)
                        //    {
                        //        sent2 = this.EuromatTransaction.SendFt(0, 0, 0);
                        //        System.Threading.Thread.Sleep(500);
                        //    }
                        //}

                        this.EuromatTransaction.TransactionState = Euromat.TransactionStateEnum.Pending;
                        this.EuromatTransaction.Amount = 0;
                        this.EuromatTransaction.Volume = 0;
                        this.EuromatTransaction.EuromatInvoiceNumber = 0;
                        this.EuromatTransaction.InvoiceNumber = 0;

                        bool sent = this.EuromatTransaction.SentNzUp();
                        //int sentCount = 0;
                        //while (!sent && sentCount < 4)
                        //{
                        //    sent = this.EuromatTransaction.SentNzUp();
                        //    System.Threading.Thread.Sleep(500);
                        //    sentCount++;
                        //}
                    }
                }
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

        public bool NeedsInvoice { set; get; }

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
                if (value == Enumerators.FuelPointStatusEnum.Work)
                    this.NeedsInvoice = true;
                this.LastStatus = this.status;
                this.status = value;
                if (this.status == Enumerators.FuelPointStatusEnum.Work && this.PresetRaised)
                {
                    this.PresetRaised = false;
                }
                if(this.Status == Enumerators.FuelPointStatusEnum.Work)
                {
                    this.DispensedAmount = 0;
                    this.DispensedVolume = 0;
                    this.SetExtendedProperty("Work", true);
                }
                if(this.Status == Enumerators.FuelPointStatusEnum.Nozzle)
                {
                }
                if (this.PropertyChanged != null)
                    this.PropertyChanged(this, new PropertyChangedEventArgs("Status"));
            }
            get { return this.status; }
        }
        public Enumerators.FuelPointStatusEnum LastStatus { set; get; }
        
        public Nozzle ActiveNozzle
        {
            get 
            {
                Nozzle actNz = null;
                if (this.activeNozzle >= 0 && this.activeNozzle < this.nozzles.Count)
                    actNz = this.nozzles[this.activeNozzle];
                return actNz;
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

        private void EuromatTransaction_TransactionAllowed(object sender, EventArgs e)
        {
            this.QueryAuthorize = true;
        }

        private void EuromatTransaction_TransactionCompleted(object sender, EventArgs e)
        {
            if (this.EuromatTransaction.UnitPrice == 0 && this.EuromatTransaction.Volume == 0 && this.EuromatTransaction.Amount == 0)
                return;

            //NR
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

        private FuelPointValues lastValues = null;
        public FuelPointValues LastValues
        {
            set
            {
                this.lastValues = value;
                if (lastValues != null && lastValues.CurrentVolume > 0 && this.EuromatTransaction != null && this.lastValues.CurrentVolume > 0)
                    this.EuromatTransaction.Volume = this.lastValues.CurrentVolume;
            }
            get { return this.lastValues; }
        }

        public bool IsEuromatEnabled { set; get; }

        public bool OTPEnabled
        {
            set
            {
                this.oTPEnabled = value;
                if (!this.oTPEnabled)
                    this.PresetRaised = false;
            }
            get { return this.oTPEnabled; }
        }

        public bool PresetRaised
        {
            set
            {
                this.presetRaised = value;
                if(!this.presetRaised)
                {
                    this.PresetAmount = 0;
                    this.PresetVolume = 0;
                }
            }
            get { return this.presetRaised; }
        }

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

        public int GetTotalsIndex()
        {
            if (this.GetExtendedProperty("TotalIndex") == null)
            {
                this.SetExtendedProperty("TotalIndex", 0);
                return 0;
            }
            return (int)this.GetExtendedProperty("TotalIndex");
        }
    }
}
