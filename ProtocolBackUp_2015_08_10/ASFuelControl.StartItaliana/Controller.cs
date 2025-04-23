using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;

namespace ASFuelControl.StartItaliana
{
    public class Controller : Common.IController
    {
        public event EventHandler<Common.TotalsEventArgs> TotalsRecieved;
        public event EventHandler<Common.FuelPointValuesArgs> ValuesRecieved;

        private Connector controller = new Connector();
        private ConcurrentDictionary<ASFuelControl.Common.Tank, ConcurrentQueue<ASFuelControl.Common.TankValues>> internalTankQueue = new ConcurrentDictionary<ASFuelControl.Common.Tank, ConcurrentQueue<ASFuelControl.Common.TankValues>>();

        public string CommunicationPort { set; get; }

        public Common.Enumerators.CommunicationTypeEnum CommunicationType { set; get; }
        public ASFuelControl.Common.Enumerators.ControllerTypeEnum ControllerType { set; get; }
        public bool EuromatEnabled { set; get; }
        public string EuromatIp { set; get; }
        public int EuromatPort { set; get; }

        public string GetEuromatParameters(int channel, int address)
        {
            return "";
        }

        public Controller()
        {
            this.ControllerType = Common.Enumerators.ControllerTypeEnum.StartItaliana;
        }

        public bool IsConnected
        {
            get { return this.controller.IsConnected; }
        }

        public void Connect()
        {
            this.controller.CommunicationPort = this.CommunicationPort;
            this.controller.Connect();
            this.controller.DataUpdated -= new EventHandler(atg_DataUpdated);
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

        public bool HaltDispenser(int channel, int address)
        {
            return false;
        }

        public bool ResumeDispenser(int channel, int address)
        {
            return false;
        }

        public virtual Common.Sales.SaleData GetSale(int channel, int address, int nozzleId)
        {
            return null;
        }

        public decimal GetNozzleTotalVolume(int channel, int address, int nozzleId)
        {
            throw (new Exception("Not Implemented"));
        }

        public decimal GetNozzleTotalPrice(int channel, int address, int nozzleId)
        {
            throw (new Exception("Not Implemented"));
        }
        
        public void AddFuelPoint(int channel, int address, int nozzleCount )
        {
            throw new NotImplementedException();
        }

        public virtual void SetEuromatDispenserNumber(int channel, int address, int number)
        {
            
        }

        public void AddFuelPoint(int channel, int address, int nozzleCount, int decimalPlaces, int volumeDecimalPlaces, int untiPriceDecimalPlaces, int totalDecimalPlaces)
        {
            throw new NotImplementedException();
        }

        public bool AuthorizeDispenser(int channel, int address)
        {
            throw new NotImplementedException();
        }
        public bool SetNozzleIndex(int channel, int address, int nozzeId, int nozzleIndex)
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

        List<Common.TankValues> lastValues = new List<Common.TankValues>();

        private Common.TankValues GetCorrectValues()
        {
            for (int i = 0; i < lastValues.Count; i++)
            {

            }
            return new Common.TankValues();
        }

        void atg_DataUpdated(object sender, EventArgs e)
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

            if (lastValues.Count < 5)
                lastValues.Add(tankValues);
            else
            {
                lastValues.RemoveAt(0);
                lastValues.Add(tankValues);

            }

            this.internalTankQueue[tank] = new ConcurrentQueue<Common.TankValues>();
            this.internalTankQueue[tank].Enqueue(tankValues);
        }
    }
}
