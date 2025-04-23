using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.VirtualDevices
{
    public class VirtualNozzle: VirtualDevice
    {
        private decimal lastCounter = 0;
        private decimal currentCounter = 0;
        private Common.Enumerators.NozzleStateEnum status = Common.Enumerators.NozzleStateEnum.Normal;

        public bool StatusChanged
        {
            set;
            get;
        }

        public VirtualDispenser ParentDispenser
        {
            set;
            get;
        }

        public decimal DiscountPercentage { set; get; }
        public decimal CurrentSaleTotalPrice
        {
            set;
            get;
        }
        public decimal CurrentSaleTotalVolume
        {
            set;
            get;
        }
        public decimal CurrentSaleUnitPrice
        {
            set;
            get;
        }
        public decimal CurrentTemperature
        {
            set;
            get;
        }
        //public bool HasOpenSales
        //{
        //    set;
        //    get;
        //}

        public Common.Enumerators.NozzleStateEnum Status
        {
            set
            {
                if(value == this.status)
                    return;
                this.status = value;
                this.StatusChanged = true;
            }
            get
            {
                return this.status;
            }
        }

        public Guid NozzleId
        {
            set;
            get;
        }
        public int NozzleNumber
        {
            set;
            get;
        }
        public int NozzleIndex
        {
            set;
            get;
        }
        public int NozzleSocket
        {
            set;
            get;
        }
        public int NozzleOfficialNumber
        {
            set;
            get;
        }
        public decimal TotalVolumeCounter
        {
            set
            {
                //this.LastVolumeCounter = this.currentCounter;
                this.currentCounter = value;
            }
            get
            {
                return this.currentCounter;
            }
        }
        public string SerialNumber { set; get; }
        public DateTime LastIdleTime { set; get; }

        public DateTime CloseSaleDateTime { set; get; }

        public decimal LastVolumeCounter
        {
            set
            {
                this.lastCounter = value;
            }
            get
            {
                return this.lastCounter;
            }
        }
        public bool HasLockedTank
        {
            get
            {
                foreach (VirtualDevices.VirtualTank tank in this.ConnectedTanks)
                {
                    if (tank.TankStatus != Common.Enumerators.TankStatusEnum.Idle && tank.TankStatus != Common.Enumerators.TankStatusEnum.Selling)
                    {
                        return true;
                    }
                }
                return false;
            }
        }
        public bool TotalsUpdated
        {
            set;
            get;
        }
        public string FuelTypeDescription
        {
            set;
            get;
        }

        public string FuelTypeShortDescription
        {
            set;
            get;
        }

        public string FuelCode { set; get; }

        public int FuelColor
        {
            set;
            get;
        }

        public VirtualTank[] ConnectedTanks
        {
            set;
            get;
        }

        

        public VirtualNozzle()
        {
            this.ConnectedTanks = new VirtualTank[] { };
        }
    }
}
