using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.GilbarcoCn
{
    public class Controller : Common.FuelPumpControllerBase
    {
        public Controller()
        {
            this.ControllerType = Common.Enumerators.ControllerTypeEnum.GilbarcoCn;
            this.Controller = new GilbarcoProtocolCn();
        }
    }
}
