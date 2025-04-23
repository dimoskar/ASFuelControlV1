using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.Common
{
    public interface IOutdoorPaymentTerminal
    {
        Enumerators.OPTTypeEnum OPTType { set; get; }
        Enumerators.OPTConnectionTypeEnum ConnectionType { set; get; }
        bool IsEnabled { set; get; }
        Nozzle[] ConnectedNozzles { get; }

        bool Connect();
        bool Disconnect();
        bool IsWorkingAtTime(DateTime dt);
        decimal GetPayedAmount();
        void AddNozzle(Nozzle nozzle);
    }
}
