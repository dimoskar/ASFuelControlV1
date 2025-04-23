using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.Communication.Enums
{
    public enum AlertIdEnum
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
        BalanceDifference = 10
    }
}
