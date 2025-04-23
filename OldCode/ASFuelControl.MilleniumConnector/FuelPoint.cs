using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.MilleniumConnector
{
    public class FuelPoint
    {
        List<Nozzle> nozzles = new List<Nozzle>();
        Common.Enumerators.FuelPointStatusEnum status = Common.Enumerators.FuelPointStatusEnum.Offline;
        private byte[] identity = null;

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
        public Nozzle ActiveNozzle { set; get; }
        public int AddressId { set; get; }
        public Common.Enumerators.FuelPointStatusEnum PreviousStatus { set; get; }
        public Common.Enumerators.FuelPointStatusEnum Status 
        {
            set 
            {
                if (status == value)
                    return;
                this.PreviousStatus = this.status;
                this.status = value; 
            }
            get { return status; }
        }

        //public void AddNozzle(Nozzle nozzle)
        //{
        //    this.nozzles.Add(nozzle);
        //    nozzle.ParentFuelPoint = this;
        //}

        public decimal LastSaleVolume { set; get; }
        public decimal LastSalePrice { set; get; }
        public decimal LastSaleUnitPrice { set; get; }

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
    }
}
