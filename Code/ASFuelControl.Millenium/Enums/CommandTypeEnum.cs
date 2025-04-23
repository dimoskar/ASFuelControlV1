using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.Millenium.Enums
{
    public enum CmdEnum
    {   
        FetchBuffer,
        CloseNozzle,
        OpenNozzle,
        //RequestID,
        RequestDisplayData,
        RequestStatus,
        //Halt,
        Authorize,
        SendMainDisplayData,
        RequestTotals,
        //RequestActiveNozzle,
        //AcknowledgeDeactivatedNozzle,
        SendPrices,
        GetLastSalesId,
        InforForId
    }
}
