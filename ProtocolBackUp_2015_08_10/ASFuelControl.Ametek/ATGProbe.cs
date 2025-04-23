using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.Ametek
{
    public class ATGProbe
    {
        public int Address { set; get; }
        public decimal FuelLevel { set; get; }
        public decimal Temperature { set; get; }
        public decimal WaterLevel { set; get; }
        public Common.Tank CommonTank { set; get; }
    }
}
