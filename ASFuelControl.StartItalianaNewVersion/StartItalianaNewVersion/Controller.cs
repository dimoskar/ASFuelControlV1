using ASFuelControl.Common;
using ASFuelControl.Common.Enumerators;
using ASFuelControl.Common.Sales;
using ASFuelControl.Euromat;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ASFuelControl.StartItalianaNewVersion
{
	public class Controller : IController
	{
		private Connector controller = new Connector();

		private ConcurrentDictionary<Tank, ConcurrentQueue<TankValues>> internalTankQueue = new ConcurrentDictionary<Tank, ConcurrentQueue<TankValues>>();

		private List<TankValues> lastValues = new List<TankValues>();

		public string CommunicationPort
		{
			get;
			set;
		}

		public CommunicationTypeEnum CommunicationType
		{
			get;
			set;
		}

		public ControllerTypeEnum ControllerType
		{
			get;
			set;
		}

		public bool EuromatEnabled
		{
			get;
			set;
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

		public bool IsConnected
		{
			get
			{
				return this.controller.IsConnected;
			}
		}

		public Controller()
		{
			this.ControllerType = ControllerTypeEnum.StartItalianaNew;
		}

		public void AddAtg(int channel, int address)
		{
			Tank tank = new Tank();
			tank.Channel = channel;
			tank.Address = address;
			Tank tank1 = tank;
			if (!this.internalTankQueue.ContainsKey(tank1))
			{
				this.internalTankQueue.TryAdd(tank1, new ConcurrentQueue<TankValues>());
				this.controller.AddProbe(address).CommonTank = tank1;
			}
		}

		public void AddFuelPoint(int channel, int address, int nozzleCount)
		{
			throw new NotImplementedException();
		}

		public void AddFuelPoint(int channel, int address, int nozzleCount, int decimalPlaces, int volumeDecimalPlaces, int untiPriceDecimalPlaces, int totalDecimalPlaces)
		{
			throw new NotImplementedException();
		}

		private void atg_DataUpdated(object sender, EventArgs e)
		{
			ATGProbe aTGProbe = sender as ATGProbe;
			TankValues tankValue = new TankValues();
			tankValue.CurrentTemperatur = aTGProbe.Temperature;
			tankValue.WaterHeight = aTGProbe.WaterLevel;
			tankValue.FuelHeight = aTGProbe.FuelLevel;
			tankValue.LastMeasureTime = DateTime.Now;
			TankValues tankValue1 = tankValue;
			Tank tankValues = (
				from t in this.internalTankQueue.Keys
				where t.Address == aTGProbe.Address
				select t).FirstOrDefault<Tank>();
			if (tankValues != null)
			{
				if (this.lastValues.Count < 5)
				{
					this.lastValues.Add(tankValue1);
				}
				else
				{
					this.lastValues.RemoveAt(0);
					this.lastValues.Add(tankValue1);
				}
				this.internalTankQueue[tankValues] = new ConcurrentQueue<TankValues>();
				this.internalTankQueue[tankValues].Enqueue(tankValue1);
			}
		}

		public bool AuthorizeDispenser(int channel, int address)
		{
			throw new NotImplementedException();
		}

		public bool AuthorizeDispenserAmountPreset(int channel, int address, decimal volume)
		{
			throw new NotImplementedException();
		}

		public bool AuthorizeDispenserVolumePreset(int channel, int address, decimal volume)
		{
			throw new NotImplementedException();
		}

		public void Connect()
		{
			try
			{
				this.controller.CommunicationPort = this.CommunicationPort;
				this.controller.Connect();
				this.controller.DataUpdated -= new EventHandler(this.atg_DataUpdated);
				this.controller.DataUpdated += new EventHandler(this.atg_DataUpdated);
			}
			catch
			{
			}
		}

		public void DisConnect()
		{
			this.controller.DisConnect();
		}

		private TankValues GetCorrectValues()
		{
			int num = 0;
			while (num < this.lastValues.Count)
			{
				num++;
			}
			return new TankValues();
		}

		public FuelPointValues GetDispenserValues(int channel, int address)
		{
			return null;
		}

		public string GetEuromatParameters(int channel, int address)
		{
			return "";
		}

		public Transaction GetEuromatTransaction(int channel, int address)
		{
			return null;
		}

		public decimal GetNozzlePrice(int channel, int address, int nozzleId)
		{
			throw new NotImplementedException();
		}

		public decimal GetNozzleTotalizer(int channel, int address, int nozzleId)
		{
			throw new NotImplementedException();
		}

		public decimal GetNozzleTotalPrice(int channel, int address, int nozzleId)
		{
			throw new Exception("Not Implemented");
		}

		public void GetNozzleTotalValues(int channel, int address, int nozzleId)
		{
			throw new NotImplementedException();
		}

		public decimal GetNozzleTotalVolume(int channel, int address, int nozzleId)
		{
			throw new Exception("Not Implemented");
		}

		public decimal[] GetPrices(int channel, int address)
		{
			throw new NotImplementedException();
		}

		public virtual SaleData GetSale(int channel, int address, int nozzleId)
		{
			return null;
		}

		public TankValues GetTankValues(int channel, int address)
		{
			TankValues tankValue;
			TankValues tankValue1;
			try
			{
				KeyValuePair<Tank, ConcurrentQueue<TankValues>> keyValuePair = (
					from iv in this.internalTankQueue
					where (iv.Key.Channel == channel ? iv.Key.Address == address : false)
					select iv).FirstOrDefault<KeyValuePair<Tank, ConcurrentQueue<TankValues>>>();
				if (keyValuePair.Key == null)
				{
					tankValue = null;
				}
				else
				{
					Tank key = keyValuePair.Key;
					if (this.internalTankQueue[key].Count > 0)
					{
						this.internalTankQueue[key].TryDequeue(out tankValue1);
						tankValue = tankValue1;
					}
					else
					{
						tankValue = null;
					}
				}
			}
			catch (Exception exception)
			{
				tankValue = null;
			}
			return tankValue;
		}

		public bool HaltDispenser(int channel, int address)
		{
			return false;
		}

		public bool ResumeDispenser(int channel, int address)
		{
			return false;
		}

		public bool SetDispenserSalesPrice(int channel, int address, decimal[] price)
		{
			throw new NotImplementedException();
		}

		public bool SetDispenserStatus(int channel, int address, FuelPointStatusEnum status)
		{
			throw new NotImplementedException();
		}

		public virtual void SetEuromatDispenserNumber(int channel, int address, int number, string ipAddress, int port)
		{
		}

		public bool SetNozzleIndex(int channel, int address, int nozzeId, int nozzleIndex)
		{
			throw new NotImplementedException();
		}

		public bool SetNozzlePrice(int channel, int address, int nozzleId, decimal newPrice)
		{
			throw new NotImplementedException();
		}

		public bool SetNozzleSocket(int channel, int address, int nozzeId, int nozzleIndex)
		{
			throw new NotImplementedException();
		}

		public void SetOtpEnabled(int channel, int address, bool flag)
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

		public event EventHandler<SaleEventArgs> SaleRecieved;

		public event EventHandler<TotalsEventArgs> TotalsRecieved;

		public event EventHandler<FuelPointValuesArgs> ValuesRecieved;
	}
}