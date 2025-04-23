using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;

namespace ASFuelControl.Box69
{
    public class Box69Controller : Common.FuelPumpControllerBase
    {
        public Box69Controller()
        {
            this.ControllerType = Common.Enumerators.ControllerTypeEnum.Box69;
            this.Controller = new ProtocolController();
        }
    }
}
