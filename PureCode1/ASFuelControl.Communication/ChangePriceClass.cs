using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.Communication
{
    [Serializable]
    public class ChangePriceClass
    {
        public Enums.FuelTypeEnum FuelType { set; get; }
        public DateTime ChangeTime { set; get; }
        public decimal Price { set; get; }
    }
}
