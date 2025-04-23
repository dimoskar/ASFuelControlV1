using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.Common
{
    /// <summary>
    /// Interface for all pump protocol classes
    /// </summary>
    public interface IFuelProtocol:IPumpDebug
    {
        event EventHandler<FuelPointValuesArgs> DataChanged;
        event EventHandler<TotalsEventArgs> TotalsRecieved;
        event EventHandler<SaleEventArgs> SaleRecieved;
        event EventHandler<FuelPointValuesArgs> DispenserStatusChanged;
        event EventHandler DispenserOffline;

        bool IsConnected { get; }
        string CommunicationPort { set; get; }

        void Connect();
        void Disconnect();
        void AddFuelPoint(Common.FuelPoint fp);
        void ClearFuelPoints();
        //void SetPresetAmount(Common.FuelPoint fp, decimal amount);
        //void SetPresetVolume(Common.FuelPoint fp, decimal volume);

        FuelPoint[] FuelPoints { set; get; }
    }
}
