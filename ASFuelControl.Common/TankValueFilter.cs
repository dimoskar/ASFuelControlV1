using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.Common
{
    public class TankValueFilter
    {
        private List<double> lastValues = new List<double>();
        private double initialErrorSuspectedValue = 0;
        DateTime dtErrorSuspected = DateTime.MinValue;
        bool errorSuspected = false;

        public void AddValue(double value)
        {
            if (this.lastValues.Count > 0)
            {
                double lastValue = this.lastValues[lastValues.Count - 1];
                if (!this.errorSuspected && lastValue - value > 1)
                {
                    this.errorSuspected = true;
                    this.dtErrorSuspected = DateTime.Now;
                }
                else if (errorSuspected && lastValue - value > 1)
                {

                }
            }
            this.lastValues.Add(value);
            if (lastValues.Count > 15)
                this.lastValues.RemoveAt(0);
        }

        public double GetValue()
        {
            if (lastValues.Count == 0)
                return 0;
            if (lastValues.Count == 1)
                return lastValues[0];
            if (!errorSuspected)
                return lastValues.Last();
            else
            {
                return initialErrorSuspectedValue;
            }
        }
    }
}
