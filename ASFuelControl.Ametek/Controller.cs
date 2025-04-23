using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using ASFuelControl.Common;

namespace ASFuelControl.Ametec
{
    public class Controller : Common.IController
    {
        public event EventHandler<Common.TotalsEventArgs> TotalsRecieved;
        public event EventHandler<Common.SaleEventArgs> SaleRecieved;
        public event EventHandler<Common.FuelPointValuesArgs> ValuesRecieved;
        private Connector controller = new Connector();
        private ConcurrentDictionary<ASFuelControl.Common.Tank, ConcurrentQueue<ASFuelControl.Common.TankValues>> internalTankQueue = new ConcurrentDictionary<ASFuelControl.Common.Tank, ConcurrentQueue<ASFuelControl.Common.TankValues>>();

        public string CommunicationPort { set; get; }
        public Common.Enumerators.CommunicationTypeEnum CommunicationType { set; get; }
        public ASFuelControl.Common.Enumerators.ControllerTypeEnum ControllerType { set; get; }

        public Euromat.Transaction GetEuromatTransaction(int channel, int address)
        {
            return null;
        }

        public string GetEuromatParameters(int channel, int address)
        {
            return "";
        }

        public Controller()
        {
            this.ControllerType = Common.Enumerators.ControllerTypeEnum.Ametek;
        }

        public bool IsConnected
        {
            get { return this.controller.IsConnected; }
        }
        public void Connect()
        {
            this.controller.CommunicationPort = this.CommunicationPort;
            this.controller.Connect();
            this.controller.DataUpdated += new EventHandler(atg_DataUpdated);
        }
        public void DisConnect()
        {
            this.controller.DisConnect();
        }
        public void AddAtg(int channel, int address)
        {
            ASFuelControl.Common.Tank tank = new Common.Tank();
            tank.Channel = channel;
            tank.Address = address;
            if (this.internalTankQueue.ContainsKey(tank))
                return;
            this.internalTankQueue.TryAdd(tank, new ConcurrentQueue<ASFuelControl.Common.TankValues>());

            ATGProbe probe = controller.AddProbe(address);
            probe.CommonTank = tank;
        }
        #region dispenser Methods

        public decimal GetNozzleTotalVolume(int channel, int address, int nozzleId)
        {
            throw (new Exception("Not Implemented"));
        }

        public decimal GetNozzleTotalPrice(int channel, int address, int nozzleId)
        {
            throw (new Exception("Not Implemented"));
        }

        public void AddFuelPoint(int channel, int address, int nozzleCount)
        {
            
        }

        public void AddFuelPoint(int channel, int address, int nozzleCount, int decimalPlaces, int volumeDecimalPlaces, int untiPriceDecimalPlaces, int totalDecimalPlaces)
        {
            
        }

        public bool AuthorizeDispenser(int channel, int address)
        {
            throw new NotImplementedException();
        }

        public bool AuthorizeDispenserVolumePreset(int channel, int address, decimal volume)
        {
            throw new NotImplementedException();
        }

        public bool AuthorizeDispenserAmountPreset(int channel, int address, decimal volume)
        {
            throw new NotImplementedException();
        }

        public bool SetDispenserStatus(int channel, int address, Common.Enumerators.FuelPointStatusEnum status)
        {
            throw new NotImplementedException();
        }

        public bool SetDispenserSalesPrice(int channel, int address, decimal[] price)
        {
            throw new NotImplementedException();
        }

        public bool SetNozzlePrice(int channel, int address, int nozzleId, decimal newPrice)
        {
            throw new NotImplementedException();
        }

        public Common.FuelPointValues GetDispenserValues(int channel, int address)
        {
            return null;
        }

        public void GetNozzleTotalValues(int channel, int address, int nozzleId)
        {
            throw new NotImplementedException();
        }

        public decimal[] GetPrices(int channel, int address)
        {
            throw new NotImplementedException();
        }

        public decimal GetNozzlePrice(int channel, int address, int nozzleId)
        {
            throw new NotImplementedException();
        }

        public decimal GetNozzleTotalizer(int channel, int address, int nozzleId)
        {
            throw new NotImplementedException();
        }

        #endregion
        public Common.TankValues GetTankValues(int channel, int address)
        {
            try
            {
                KeyValuePair<ASFuelControl.Common.Tank, ConcurrentQueue<ASFuelControl.Common.TankValues>> pair = this.internalTankQueue.Where(iv =>
                    iv.Key.Channel == channel && iv.Key.Address == address).FirstOrDefault();
                if (pair.Key == null)
                    return null;
                Common.Tank ctank = pair.Key;

                if (this.internalTankQueue[ctank].Count > 0)
                {
                    ASFuelControl.Common.TankValues vals;
                    this.internalTankQueue[ctank].TryDequeue(out vals);
                    return vals;
                }
                return null;

            }
            catch (Exception ex)
            {
                return null;
            }
        }
        private void atg_DataUpdated(object sender, EventArgs e)
        {
            try
            {
                ATGProbe atg = sender as ATGProbe;

                ASFuelControl.Common.TankValues tankValues = new ASFuelControl.Common.TankValues();

                tankValues.CurrentTemperatur = (decimal)atg.Temperature;
                tankValues.WaterHeight = (decimal)atg.WaterLevel;
                tankValues.FuelHeight = (decimal)atg.FuelLevel;
                tankValues.LastMeasureTime = DateTime.Now;


                ASFuelControl.Common.Tank tank = this.internalTankQueue.Keys.Where(t => (t.Address == atg.Address)).FirstOrDefault();
                if (tank == null)
                    return;
                this.internalTankQueue[tank] = new ConcurrentQueue<Common.TankValues>();
                this.internalTankQueue[tank].Enqueue(tankValues);

                
            }
            catch (Exception)
            {

            }
        }
        public Common.Sales.SaleData GetSale(int channel, int address, int nozzleId)
        {
            return new Common.Sales.SaleData();
        }
        public string EuromatIp
        {
            get;
            set;
        }

        public int EuromatPort
        {
            get;
            set;
        }
        public bool EuromatEnabled
        {
            get;
            set;
        }
        public virtual void SetEuromatDispenserNumber(int channel, int address, int number, string ipAddress, int port)
        {
        }
        public bool ResumeDispenser(int channel, int address)
        {
            return false;
        }
        public bool HaltDispenser(int channel, int address)
        {
            return false;
        }
        public void AddFuelPoint(int channel, int address, int nozzleCount, int decimalPlaces, int volumeDecimalPlaces, int untiPriceDecimalPlaces)
        {
            throw new NotImplementedException();
        }
        public bool SetNozzleIndex(int channel, int address, int nozzeId, int nozzleIndex)
        {
            throw new NotImplementedException();
        }

        public bool SetNozzleSocket(int channel, int address, int nozzeId, int nozzleIndex)
        {
            throw new NotImplementedException();
        }

        public bool SetPresetAmount(int channel, int address, decimal amount)
        {
            throw new NotImplementedException();
        }

        public bool SetPresetVolume(int channel, int address, decimal volume)
        {
            throw new NotImplementedException();
        }

        public void SetOtpEnabled(int channel, int address, bool flag)
        {
            throw new NotImplementedException();
        }
    }
}