using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.Communication.Enums
{
    public enum FuelTypeEnum
    {
        Diesel = 20,
        DieselPremium = 21,
        DieselHeat = 30,
        DieselHeatPremium = 31,
        Fotistiko = 32,
        Unleaded95 = 10,
        Unleaded95Plus = 11,
        Unlieaded100 = 12,
        Lrp = 13,
        Lpg = 40,
        Mixture = 99,
        AdBlue = 100,
        None = -1
    }
}
