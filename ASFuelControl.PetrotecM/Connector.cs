using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASFuelControl.Common;
using ASFuelControl.Common.Enumerators;

namespace ASFuelControl.PetrotecM
{
    public class Controller : FuelPumpControllerBase
    {
        public Controller()
        {
            base.ControllerType = ControllerTypeEnum.Petrotec;
            base.Controller = new PetrotecProtocol();
        }
    }
}
