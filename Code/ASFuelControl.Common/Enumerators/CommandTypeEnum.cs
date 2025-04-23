using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.Common.Enumerators
{
    public enum CommandTypeEnum
    {
        RequestID,
        RequestDisplayData,
        RequestStatus,
        Halt,
        Authorize,
        SendMainDisplayData,
        RequestTotals,
        RequestActiveNozzle,
        AcknowledgeDeactivatedNozzle,
        SendPrices,
        Initialize
    }
}
