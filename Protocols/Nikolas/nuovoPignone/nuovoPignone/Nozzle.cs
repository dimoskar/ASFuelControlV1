using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace nuovoPignone
{
    public class Nozzle
    {
        Common.Enumerators.FuelPointStatusEnum status = Common.Enumerators.FuelPointStatusEnum.Offline;

        public FuelPoint ParentFuelPoint { set; get; }

        public bool QueryAuthorize { set; get; }
        public bool QueryTotals { set; get; }
        public int NozzleId { set; get; }
        public int NozzleIndex { set; get; }
        public int UnitPrice { set; get; }
        public byte[] PriceBuffer { set; get; }
        public decimal Totalizer { set; get; }

        public decimal SaleVolume { set; get; }
        public decimal SalePrice { set; get; }
        public decimal SaleUnitPrice { set; get; }
        public int PriceDecimalPlaces { set; get; }
        public int VolumeDecimalPlaces { set; get; }

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
    }
}
