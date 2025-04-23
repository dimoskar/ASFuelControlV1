using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ASFuelControl.Common.Enumerators;
using System.Configuration;

namespace ASFuelControl.Millennium
{
    public class Controller: Common.FuelPumpControllerBase
    {
        public Controller()
        {
            //string logBool = ConfigurationManager.AppSettings["LogController"];
            this.ControllerType = Common.Enumerators.ControllerTypeEnum.Millennium;
         
            this.Controller = new MillenniumConnector();
            
        }
        
     
       
    }
}
