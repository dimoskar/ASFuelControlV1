using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.StartItalianaXMT
{
    public class ATGProbe
    {
        private List<decimal> fuelLevels = new List<decimal>();
        private List<decimal> waterLevels = new List<decimal>();
        private List<decimal> temperatures = new List<decimal>();
        private static int buffer = 3;
        private double avgFuel, avgWater, deltaFuel, deltaWater,sumFuel,sumWater;
        //private decimal[] deltaWater = new decimal[buffer - 1];
        //private decimal[] deltaFuel = new decimal[buffer-1];

        public void AddNewFuelMeasurements(decimal water, decimal fuel)
        {
            fuelLevels.Add(fuel);
            waterLevels.Add(water);
            if(this.fuelLevels.Count < 4)
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
                if(this.fuelLevels[num - 2] > this.fuelLevels[num - 1]
                    && this.fuelLevels[num - 2] > this.fuelLevels[num - 3])
                {
                    this.fuelLevels.RemoveAt(num - 2);
                    this.waterLevels.RemoveAt(num - 2);
                }
                FuelLevel = (decimal)this.fuelLevels[num - 2];
                WaterLevel = (decimal)this.waterLevels[num - 2];


                for(int i = buffer; i < this.fuelLevels.Count; i++)
                {
                    this.fuelLevels.RemoveAt(0);
                }
                for(int i = buffer; i < this.fuelLevels.Count; i++)
                {
                    this.fuelLevels.RemoveAt(0);
                }
            }

            //if(this.fuelLevels.Count>0)
            //{
           //    avgFuel += fuelLevels.Average();
           //    avgWater += waterLevels.Average();
           //    sumFuel = fuelLevels.Sum(d => Math.Pow(d - avgFuel, 2));
           //    sumWater = waterLevels.Sum(d => Math.Pow(d - avgWater, 2));
           //    Math.Sqrt((sumWater) / (waterLevels.Count() - 1));
           //    Math.Sqrt((sumFuel) / (fuelLevels.Count() - 1));
           // }
           //decimal percentOfError = Math.Abs(fuel - avgFuel) / avgFuel;
           //if(percentOfError < 0.03m)
           //{
           //    fuelLevels.Add(fuel);
           //    waterLevels.Add(water);
           //    for(int i = buffer; i < this.fuelLevels.Count; i++)
           //    {
           //        this.fuelLevels.RemoveAt(0);
           //    }
           //    for(int i = buffer; i < this.fuelLevels.Count; i++)
           //    {
           //        this.fuelLevels.RemoveAt(0);
           //    }
           //    return true;
            //}
            //else
              //  return false;
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
