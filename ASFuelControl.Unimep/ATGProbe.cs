using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASFuelControl.Unimep
{
    public class ATGProbe
    {
        private List<decimal> fuelLevels = new List<decimal>();
        private List<decimal> waterLevels = new List<decimal>();
        private List<decimal> temperatures = new List<decimal>();
        private static int buffer = 3;
        private double avgFuel, avgWater, deltaFuel, deltaWater, sumFuel, sumWater;

        public void AddNewFuelMeasurements(decimal water, decimal fuel)
        {
            fuelLevels.Add(fuel);
            waterLevels.Add(water);
            if (this.fuelLevels.Count < 4)
            {
                //FuelLevel = 0;
                //WaterLevel = 0;
                return;
            }
            else
            {
                //14000948
                //1878-8575-3562-6492 
                //
                int num = fuelLevels.Count;
                if (this.fuelLevels[num - 2] > this.fuelLevels[num - 1]
                    && this.fuelLevels[num - 2] > this.fuelLevels[num - 3])
                {
                    this.fuelLevels.RemoveAt(num - 2);
                    this.waterLevels.RemoveAt(num - 2);
                }
                FuelLevel = (decimal)this.fuelLevels[num - 2];
                WaterLevel = (decimal)this.waterLevels[num - 2];


                for (int i = buffer; i < this.fuelLevels.Count; i++)
                {
                    this.fuelLevels.RemoveAt(0);
                }
                for (int i = buffer; i < this.fuelLevels.Count; i++)
                {
                    this.fuelLevels.RemoveAt(0);
                }
            }
        }

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
