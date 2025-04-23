using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ASFuelControl.Common.Enumerators;

namespace ASFuelControl.Millennium
{
    public class Controller: Common.FuelPumpControllerBase
    {
        public Controller()
        {
            this.ControllerType = Common.Enumerators.ControllerTypeEnum.Millennium;
            this.Controller = new MillenniumConnector();
        }

        public override decimal GetNozzleTotalizer(int channel, int address, int nozzleId)
        {
            Common.FuelPoint fp = this.GetFulePoint(channel, address);
            if (fp == null)
                return 0;
            if (fp.NozzleCount > nozzleId)
                return 0;
            if (!(bool)fp.GetExtendedProperty("TotalsInitialized", false))
                return -1;
            return fp.Nozzles[nozzleId - 1].TotalVolume;
        }
    }
}
