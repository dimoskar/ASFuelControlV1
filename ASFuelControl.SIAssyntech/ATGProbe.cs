using ASFuelControl.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ASFuelControl.SIAssytech
{
    public class ATGProbe
    {
        private List<decimal> fuelLevels = new List<decimal>();

        private List<decimal> waterLevels = new List<decimal>();

        private List<decimal> temperatures = new List<decimal>();

        private static int buffer;

        private double avgFuel;

        private double avgWater;

        private double deltaFuel;

        private double deltaWater;

        private double sumFuel;

        private double sumWater;

        public int Address
        {
            get;
            set;
        }

        public Tank CommonTank
        {
            get;
            set;
        }

        public decimal FuelLevel
        {
            get;
            set;
        }

        public decimal Temperature
        {
            get
            {
                return this.temperatures.Average();
            }
            set
            {
                this.temperatures.Add(value);
                if (this.temperatures.Count > 20)
                {
                    this.temperatures.RemoveAt(0);
                }
            }
        }

        public decimal WaterLevel
        {
            get;
            set;
        }

        static ATGProbe()
        {
            ATGProbe.buffer = 3;
        }

        public ATGProbe()
        {
        }

        public void AddNewFuelMeasurements(decimal water, decimal fuel)
        {
            this.fuelLevels.Add(fuel);
            this.waterLevels.Add(water);
            if (this.fuelLevels.Count >= 4)
            {
                int count = this.fuelLevels.Count;
                if ((this.fuelLevels[count - 2] <= this.fuelLevels[count - 1] ? false : this.fuelLevels[count - 2] > this.fuelLevels[count - 3]))
                {
                    this.fuelLevels.RemoveAt(count - 2);
                    this.waterLevels.RemoveAt(count - 2);
                }
                this.FuelLevel = this.fuelLevels[count - 2];
                this.WaterLevel = this.waterLevels[count - 2];
                for (int i = ATGProbe.buffer; i < this.fuelLevels.Count; i++)
                {
                    this.fuelLevels.RemoveAt(0);
                }
                for (int j = ATGProbe.buffer; j < this.fuelLevels.Count; j++)
                {
                    this.fuelLevels.RemoveAt(0);
                }
            }
        }
    }
}