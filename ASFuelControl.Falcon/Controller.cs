using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ASFuelControl.Common.Enumerators;

namespace ASFuelControl.Falcon
{
    public class Controller: Common.FuelPumpControllerBase
    {
        public Controller()
        {
            this.ControllerType = Common.Enumerators.ControllerTypeEnum.Gilbarco;
            this.Controller = new FalconProtocol();
        }
    }
}
