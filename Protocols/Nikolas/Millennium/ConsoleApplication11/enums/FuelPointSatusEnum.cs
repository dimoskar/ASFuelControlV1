using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MillenniumAPI
{
    public enum FuelPointStatusEnum
    {
        Offline = 0,
        Idle = 1,
        Nozzle = 3,
        Ready = 4,
        Auth = 5,
        Work = 6 ,
        TransactionCompleted = 7,
        TransactionStopped,
        SalingCompleted,
        Error,
        CheckState,
        InitializeSale,
        TankLocked,
        GetEndTotals,
        Close

    }
}
