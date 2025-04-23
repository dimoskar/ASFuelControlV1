using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ASFuelControl.Common.Enumerators;
using ASFuelControl.Common;

namespace ASFuelControl.NuovoPignone
{
    public class Controller: Common.FuelPumpControllerBase
    {
        public event EventHandler<Common.TotalsEventArgs> TotalsRecieved;
        public Controller()
        {
            this.ControllerType = Common.Enumerators.ControllerTypeEnum.NuovoPignone;
            this.Controller = new NuovoPignoneProtocol();
        }

    }
}
