using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Common.Enumerators
{
    public enum FuelPointStatusEnum
    {
        Offline = 0,
        Idle = 1,
        Nozzle = 3,
        Ready = 4,
        Work = 5,
        TransactionCompleted = 6,
        TransactionStopped = 7,
        SalingCompleted = 8,
        Error = 9,
        CheckState = 10,
        InitializeSale = 11,
        TankLocked,
        GetEndTotals
    }
}
