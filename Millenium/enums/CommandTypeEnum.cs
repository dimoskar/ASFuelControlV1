using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MillenniumAPI
{
    public enum CommandTypeEnum
    {   FetchBuffer,
        CloseNozzle,
        OpenNozzle,
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
        GetLastSalesId
    }
}
