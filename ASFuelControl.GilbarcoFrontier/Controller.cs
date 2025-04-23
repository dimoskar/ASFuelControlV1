using ASFuelControl.Common;
using ASFuelControl.Common.Enumerators;
using System;

namespace ASFuelControl.GilbarcoFrontier
{
	public class Controller : FuelPumpControllerBase
	{
		public Controller()
		{
			base.ControllerType = ControllerTypeEnum.Gilbarco;
			base.Controller = new GilbarcoProtocol();
		}
	}
}