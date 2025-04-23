using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.FafnirDirect
{
    public class ATGProbe
    {
        private List<decimal> temperatures = new List<decimal>();

        public int Address { set; get; }
        public decimal FuelLevel { set; get; }
        public decimal Temperature
        {
            set
            {
                this.temperatures.Add(value);
                if (this.temperatures.Count > 20)
                    this.temperatures.RemoveAt(0);
            }
            get
            {
                return this.temperatures.Average();
            }
        }
        public decimal WaterLevel { set; get; }
        public Common.Tank CommonTank { set; get; }
    }
}
