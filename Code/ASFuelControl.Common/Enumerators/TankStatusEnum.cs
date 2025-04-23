using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.Common.Enumerators
{
    public enum TankStatusEnum
    {
        Offline = 0,
        Idle = 1,
        Selling = 2,
        FillingInit = 3,
        Filling = 4,
        Waiting = 5,
        FuelExtractionInit = 6,
        FuelExtraction = 7,
        LevelIncrease = 8,
        LevelDecrease = 9,
        WaitingEllapsed = 10,
        Error = 11,
        LowLevel = 12,
        HighWaterLevel = 13
    }
}
