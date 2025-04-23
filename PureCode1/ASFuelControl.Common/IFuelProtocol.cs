using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.Common
{
    public interface IFuelProtocol
    {
        event EventHandler<FuelPointValuesArgs> DataChanged;
        event EventHandler<TotalsEventArgs> TotalsRecieved;
        event EventHandler<FuelPointValuesArgs> DispenserStatusChanged;

        bool IsConnected { get; }
        string CommunicationPort { set; get; }

        void Connect();
        void Disconnect();
        void AddFuelPoint(Common.FuelPoint fp);
        void ClearFuelPoints();

        FuelPoint[] FuelPoints { set; get; }
    }
}
