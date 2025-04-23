using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.Millenium.Enums
{
    public enum ResponseLengthEnum
    {   //legnth of responses
        State = 35,

        ConfirmOC=38,
        //      id                   ch
        //02 01 07 00 00 E0 00 05 01 22 00 3D 00

        AuthConfirm=44,
        //      id                   ch
        //02 01 07 00 00 E0 00 07 01 22 00 19 00 3E 00

        SendPriceConfirm = 53,     
        //       id                                 ch 
        //02 01 [04] 00 00 E0 00 0A 06 61 00 00 00 [02] 11 00 02 00


        Status = 47,
        //       id                     ch
        //02 01 [07] 00 00 20 00 08 01 [22] 14 01 09 15 01 01

        SelfResponseStatus=65,
        //02 01 [07] 00 00 80 00 0E 01 [22] 64 00 14 01 [02] 15 01 00 16 02 02 01 //2 closed,3 idle, 4 request auth,6 authed,8 working,9 freezed


        DisplayWhileFueling = 101,

        Totals = 86,

        LastSalesID = 113,                                                                                          //12 14
        //02 01 07 00 00 20 00 1E 02 21 22 14 07 0A 00 00 12 23 51 62 15 07 0A 00 00 16 78 34 90 16 07 0C 00 00 00 00 12 14

        TransactionSummary = 119,

        LastSalesInfo=137,
        //02 01 07 00 00 22 00 26 04 02 21 12 15 01 02 12 15 05 05 04 00 00 72 00 06 05 04 00 00 58 00 07 04 01 12 34 00 08 01 01 0C 01 00 15 01 01

    }
}
