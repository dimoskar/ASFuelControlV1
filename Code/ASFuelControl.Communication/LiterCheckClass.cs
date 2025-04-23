using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.Communication
{
    [Serializable]
    public class LiterCheckClass
    {
        public decimal Capacity { set; get; }
        public decimal Capacity15 { set; get; }
        public Enums.FuelTypeEnum FuelType { set; get; }
        public string NozzleId { set; get; }
        public string TankSN { set; get; }
        public DateTime TransactionDate { set; get; }

        public FuelFlowService.Fuelflows_TypeLiterCheck GetElement()
        {
            FuelFlowService.Fuelflows_TypeLiterCheck literCheck = new FuelFlowService.Fuelflows_TypeLiterCheck();
            literCheck.F_231_1 = this.TransactionDate;
            literCheck.F_CAPACITY = this.Capacity;
            literCheck.F_CAPACITY_15 = this.Capacity15;
            literCheck.F_FUEL_CODE = new FuelFlowService.Fuel_Type();
            literCheck.F_FUEL_CODE.Code = (int)this.FuelType;
            literCheck.F_FUEL_CODE.Description = Enums.LocalizedEnumExtensions.GetLocalizedName(this.FuelType);
            literCheck.F_LABEL_OGKOMETR = this.NozzleId;
            literCheck.F_LABEL_POOL = this.TankSN;
            return literCheck;
        }
    }
}
