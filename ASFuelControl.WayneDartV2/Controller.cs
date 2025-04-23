using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.WayneDartV2
{
    public class Controller : Common.FuelPumpControllerBase
    {
        public event EventHandler<Common.TotalsEventArgs> TotalsRecieved;
        public Controller()
        {
            this.ControllerType = Common.Enumerators.ControllerTypeEnum.WayneDartV2;
            this.Controller = new WayneDartV2Protocol();
        }

    }
}
