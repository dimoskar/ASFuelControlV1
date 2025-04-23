using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace ASFuelControl.Elbis
{
    public class FuelPoint
    {
        public event EventHandler<StatusChangedEventArgs> StatusChanged;
        public event EventHandler DataChanged;
        public event EventHandler<TotalsEventArgs> TotalsRecieved;

        List<Nozzle> nozzles = new List<Nozzle>();
        Common.Enumerators.FuelPointStatusEnum status = Common.Enumerators.FuelPointStatusEnum.Offline;
        Common.Enumerators.FuelPointStatusEnum dispenserStatus = Common.Enumerators.FuelPointStatusEnum.Offline;
        private byte[] identity = null;
        private DateTime lastCommand = DateTime.Now.AddMilliseconds(-1000);
        private DateTime lastOffline = DateTime.Now.AddMilliseconds(-15000);
        private bool noAnswer = false;
        Nozzle activeNozzle = null;

        public Common.FuelPoint CommonFP { set; get; }

        public byte[] Identity
        {
            set
            {
                this.identity = value;
                this.Channel = BitConverter.ToInt32(this.identity, 0);
            }
            get { return this.identity; }
        }

        public int Channel
        {
            set;
            get;
        }

        public Nozzle[] Nozzles
        {
            get { return this.nozzles.ToArray(); }
        }
        public bool QueryAuthorize { set; get; }
        public bool QueryTotals { set; get; }
        public bool QueryStop { set; get; }
        public bool QuerySetPrice { set; get; }
        public bool QueryNozzle { set; get; }
        public Nozzle ActiveNozzle 
        {
            set 
            {
                if(this.activeNozzle != null)
                    this.LastNozzle = this.activeNozzle;
                this.activeNozzle = value;
                
            }
            get { return this.activeNozzle; } 
        }
        public Nozzle LastNozzle { set; get; }
        public bool Initialized { set; get; }
        public bool NoAnswer
        {
            set 
            {
                if (value == noAnswer)
                {
                    this.lastOffline = DateTime.Now;
                }
                this.noAnswer = value; 
            }
            get { return noAnswer; }
        }
        public bool InitializeSent { set; get; }
        public bool PriceSet { set; get; }
        public bool PriceSetSent { set; get; }
        public int AddressId { set; get; }
        
        public Common.Enumerators.FuelPointStatusEnum PreviousStatus { set; get; }
        public Common.Enumerators.FuelPointStatusEnum DispenserStatus
        {
            set
            {
                if (dispenserStatus == value)
                    return;
                //if (value == Common.Enumerators.FuelPointStatusEnum.Offline)
                //{
                //    if (DateTime.Now.Subtract(this.lastOffline).TotalSeconds < 10)
                //        return;
                //    else
                //        this.lastOffline = DateTime.Now;
                //}

                this.dispenserStatus = value;
                //if (this.dispenserStatus == Common.Enumerators.FuelPointStatusEnum.Idle && (this.status == Common.Enumerators.FuelPointStatusEnum.Work ||
                //    this.status == Common.Enumerators.FuelPointStatusEnum.TransactionCompleted || this.status == Common.Enumerators.FuelPointStatusEnum.TransactionStopped ||
                //    this.status == Common.Enumerators.FuelPointStatusEnum.Ready))
                //{
                //    this.QueryTotals = true;
                //}
                //else if (this.dispenserStatus == Common.Enumerators.FuelPointStatusEnum.Nozzle && this.status == Common.Enumerators.FuelPointStatusEnum.Idle)
                //{
                //    this.QueryTotals = true;
                //}
                //else
               this.Status = this.dispenserStatus;
            }
            get { return dispenserStatus; }
        }
        public Common.Enumerators.FuelPointStatusEnum Status
        {
            set
            {
                if (status == value)
                    return;
                //if (value == Common.Enumerators.FuelPointStatusEnum.Offline)
                //{
                //    if (DateTime.Now.Subtract(this.lastOffline).TotalSeconds < 10)
                //        return;
                //    else
                //        this.lastOffline = DateTime.Now;
                //}
                this.PreviousStatus = this.status;
                this.status = value;
                if (this.status == Common.Enumerators.FuelPointStatusEnum.Offline)
                    this.Initialized = false;
                else
                    this.Initialized = true;

                if (this.StatusChanged != null)
                    this.StatusChanged(this, new StatusChangedEventArgs(this.status, this.PreviousStatus));
            }
            get { return status; }
        }

        public decimal LastSaleVolume { set; get; }
        public decimal LastSalePrice { set; get; }
        public decimal LastSaleUnitPrice { set; get; }

        public int PriceDecimalPlaces { set; get; }
        public int VolumeDecimalPlaces { set; get; }

        public FuelPoint()
        {
            for (int i = 0; i < 6; i++)
            {
                Nozzle nozzle = new Nozzle();
                nozzle.NozzleIndex = i + 1;
                nozzle.ParentFuelPoint = this;
                this.nozzles.Add(nozzle);
            }
        }

        public FuelPoint(int nozzleCount)
        {
            for (int i = 0; i < nozzleCount; i++)
            {
                Nozzle nozzle = new Nozzle();
                nozzle.NozzleIndex = i + 1;
                nozzle.ParentFuelPoint = this;
                this.nozzles.Add(nozzle);
            }
        }

        public byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }
    }

    public class StatusChangedEventArgs : EventArgs
    {
        public StatusChangedEventArgs(Common.Enumerators.FuelPointStatusEnum currentStatus, Common.Enumerators.FuelPointStatusEnum previousStatus)
        {
            this.CurrentStatus = currentStatus;
            this.PreviousStatus = previousStatus;
        }

        public Common.Enumerators.FuelPointStatusEnum CurrentStatus { private set; get; }
        public Common.Enumerators.FuelPointStatusEnum PreviousStatus { private set; get; }
    }

    public class TotalsEventArgs : EventArgs
    {
        public TotalsEventArgs(int address, byte nozzleId, Nozzle nozzle)
        {
            this.Address = address;
            this.Nozzle = nozzle;
            this.NozzleID = nozzleId;
        }

        public int Address { private set; get; }
        public Nozzle Nozzle { private set; get; }
        public byte NozzleID { private set; get; }
    }
}

