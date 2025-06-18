using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.Common.Enumerators
{
    public enum AlertTypeEnum
    {
        CommunicationLossFuelPoint = 1,
        CommunicationLossTank = 2,
        FuelLoss = 3,
        WrongDataFuelPoint = 4,
        WrongDataTank = 5,
        ProgramTermination = 6,
        FuelPointError = 7,
        TankAlert = 8,
        TitrimetryDataChange = 9,
        BalanceDifference = 10,
        TankDensityError = 100
    }

    public enum AlarmEnum
    {
        FuelTooHigh = 1,
        FuelTooLow,
        FuelIncrease,
        FuelDecrease,
        WaterTooHigh,
        NozzleTotalError,
        FuelPumpOffline,
        TankOffline,
        BalanceDifference,
        LiterCheckNotReturned,
        CommunicationLossFuelPoint,
        CommunicationLossTank,
        TitrimetryDataChange,
        ProgramTermination,
        FuelLeak,
        SensorError,
        TankDensityError = 100

    }
}
