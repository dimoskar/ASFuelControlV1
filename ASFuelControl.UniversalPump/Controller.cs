using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.UniversalPump
{
    public class Controller : Common.FuelPumpControllerBase
    {
        public Controller()
        {
            this.ControllerType = Common.Enumerators.ControllerTypeEnum.Universal;
            this.Controller = new UniversalProtocol();
        }
    }
}
