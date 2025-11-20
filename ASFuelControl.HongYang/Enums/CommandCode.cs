using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASFuelControl.HongYang.Enums
{
    public enum CommandCode : byte
    {
        Authorise = 0x08,         // Authorize fueling
        Status = 0x12,             // Get dispenser status
        Lock = 0x15,               // Enter authorization mode
        Unlock = 0x14,             // Exit authorization mode
        ChangePrice = 0x80,        // Set unit price
        GetPrice = 0x8C,           // Read current price
        GetTotals = 0x8E,          // Read cumulative totals
        ReadFuelledAmount = 0x8F,  // Read current transaction amount
        Stop = 0x16,               // Stop fueling
        ClearDisplay = 0xC0        // Clear dispenser display

    }
}
