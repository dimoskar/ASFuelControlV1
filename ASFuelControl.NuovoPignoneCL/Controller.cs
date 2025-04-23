using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.NuovoPignoneCL
{
    public class Controller : Common.FuelPumpControllerBase
    {
        public event EventHandler<Common.TotalsEventArgs> TotalsRecieved;
        public Controller()
        {
            this.ControllerType = Common.Enumerators.ControllerTypeEnum.NuovoPignoneCL;
            this.Controller = new NuovoPignoneCLProtocol();
        }

    }
}
