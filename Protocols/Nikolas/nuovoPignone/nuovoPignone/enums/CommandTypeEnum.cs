using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace nuovoPignone
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
