using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.NuovoPignoneV1
{
    public class Controller : Common.FuelPumpControllerBase
    {
        public event EventHandler<Common.TotalsEventArgs> TotalsRecieved;
        public Controller()
        {
            this.ControllerType = Common.Enumerators.ControllerTypeEnum.NuovoPignone;
            this.Controller = new NuovoPignoneV1Protocol();
        }

    }
}
