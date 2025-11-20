using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASFuelControl.HongYang.Enums
{
    [Flags]
    public enum PumpStateFlags : byte
    {
        PpuChanged = 1 << 0,
        ShiftChanged = 1 << 1,
        TotalShiftChanged = 1 << 2,
        Fuelling = 1 << 3,
        AuthorizationMode = 1 << 4,
        NozzleWaiting = 1 << 5,
        NewVersion = 1 << 6
    }
}
