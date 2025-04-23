using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.Common
{
    /// <summary>
    /// Interface for all Controller classes
    /// </summary>
    public interface IController
    {
        string CommunicationPort { set; get; }
        Common.Enumerators.CommunicationTypeEnum CommunicationType { set; get; }
        bool IsConnected { get; }

        event EventHandler<TotalsEventArgs> TotalsRecieved;
        event EventHandler<FuelPointValuesArgs> ValuesRecieved;

        void Connect();
        void DisConnect();
        void AddFuelPoint(int channel, int address, int nozzleCount);
        void AddFuelPoint(int channel, int address, int nozzleCount, int amountDecimalPlaces, int volumeDecimalPlaces, int untiPriceDecimalPlaces, int totalDecimalPlaces);
        void AddAtg(int channel, int address);

        void SetEuromatDispenserNumber(int channel, int address, int number);
        bool AuthorizeDispenser(int channel, int address);
        bool AuthorizeDispenserVolumePreset(int channel, int address, decimal volume);
        bool AuthorizeDispenserAmountPreset(int channel, int address, decimal volume);
        bool HaltDispenser(int channel, int address);
        bool ResumeDispenser(int channel, int address);

        bool SetDispenserStatus(int channel, int address, Common.Enumerators.FuelPointStatusEnum status);
        bool SetDispenserSalesPrice(int channel, int address, decimal[] price);
        bool SetNozzlePrice(int channel, int address, int nozzleId, decimal newPrice);
        bool SetNozzleIndex(int channel, int address, int nozzeId, int nozzleIndex);
        FuelPointValues GetDispenserValues(int channel, int address);
        void GetNozzleTotalValues(int channel, int address, int nozzleId);
        decimal GetNozzleTotalVolume(int channel, int address, int nozzleId);
        decimal GetNozzleTotalPrice(int channel, int address, int nozzleId);
        decimal[] GetPrices(int channel, int address);
        decimal GetNozzlePrice(int channel, int address, int nozzleId);
        decimal GetNozzleTotalizer(int channel, int address, int nozzleId);

        bool EuromatEnabled { set; get; }
        string EuromatIp { set; get; }
        int EuromatPort { set; get; }

        Sales.SaleData GetSale(int channel, int address, int nozzleId);

        TankValues GetTankValues(int channel, int address);

        ASFuelControl.Common.Enumerators.ControllerTypeEnum ControllerType { set; get; }
    }

    public class TotalsEventArgs : EventArgs
    {
        public FuelPoint CurrentFuelPoint { private set; get; }
        public int NozzleId { private set; get; }
        public decimal TotalVolume { private set; get; }
        public decimal TotalPrice { private set; get; }

        public TotalsEventArgs(FuelPoint fp, int nozzleId, decimal totalVolume, decimal totalPrice)
        {
            this.TotalPrice = totalPrice;
            this.TotalVolume = totalVolume;
            this.CurrentFuelPoint = fp;
            this.NozzleId = nozzleId;
        }
    }

    public class FuelPointValuesArgs : EventArgs
    {
        public FuelPoint CurrentFuelPoint {  set; get; }
        public int CurrentNozzleId {  set; get; }
        public FuelPointValues Values {  set; get; }
    }
}
