using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.Schwelm
{
    public class Controller : Common.FuelPumpControllerBase
    {
        public event EventHandler<Common.TotalsEventArgs> TotalsRecieved;
        public Controller()
        {
            this.ControllerType = Common.Enumerators.ControllerTypeEnum.Schwelm;
            this.Controller = new SchwelmProtocol();
        }
    }
}
