using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.WayneSU
{
    public class Controller : Common.FuelPumpControllerBase
    {
        public event EventHandler<Common.TotalsEventArgs> TotalsRecieved;
        public Controller()
        {
            this.ControllerType = Common.Enumerators.ControllerTypeEnum.WayneSU;
            this.Controller = new WayneSUProtocol();
        }

    }
}
