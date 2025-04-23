using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.Europump
{
    public class Controller : Common.FuelPumpControllerBase
    {
        public event EventHandler<Common.TotalsEventArgs> TotalsRecieved;
        public Controller()
        {
            this.ControllerType = Common.Enumerators.ControllerTypeEnum.Europump;
            this.Controller = new EuropumpProtocol();
        }

    }
}
