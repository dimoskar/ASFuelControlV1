using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ASFuelControl.Common.Enumerators;

namespace ASFuelControl.AK6_Mono
{
    public class Controller : Common.FuelPumpControllerBase
    {
        public Controller()
        {
            this.ControllerType = Common.Enumerators.ControllerTypeEnum.AK6_Mono;
            this.Controller = new AK6_MonoProtocol();
        }
    }
}
