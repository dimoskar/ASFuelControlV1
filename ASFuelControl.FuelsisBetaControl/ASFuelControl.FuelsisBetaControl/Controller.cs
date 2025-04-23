using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASFuelControl.Common;
using ASFuelControl.Common.Enumerators;

namespace ASFuelControl.FuelsisBetaControl
{
    public class Controller : FuelPumpControllerBase
    {
        public Controller()
        {
            base.ControllerType = ControllerTypeEnum.Universal;
            base.Controller = new Protocol();
        }
    }
}
