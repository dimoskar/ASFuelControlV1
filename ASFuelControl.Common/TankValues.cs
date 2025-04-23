using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.Common
{
    /// <summary>
    /// class used to transfer tank values to the main program
    /// </summary>
    public class TankValues
    {
        public decimal FuelHeight { set; get; }
        public decimal WaterHeight { set; get; }
        public decimal CurrentTemperatur { set; get; }
        public bool FuelRipple { set; get; }
        public Common.Enumerators.TankStatusEnum Status { set; get; }
        public string ErrorText { set; get; }
        public DateTime LastMeasureTime { set; get; }
    }
}
