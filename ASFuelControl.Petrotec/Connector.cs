using System;
using ASFuelControl.Common;
using ASFuelControl.Common.Enumerators;

namespace ASFuelControl.Petrotec
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
