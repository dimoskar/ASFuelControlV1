using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;

namespace ASFuelControl.Millenium
{
    public class Controller : Common.IController
    {
        public event EventHandler<Common.TotalsEventArgs> TotalsRecieved;

        public event EventHandler<Common.FuelPointValuesArgs> ValuesRecieved;

        MilleniumConnector.Controller controller = new MilleniumConnector.Controller();
        Common.Enumerators.CommunicationTypeEnum comType = Common.Enumerators.CommunicationTypeEnum.RS232;

        private ConcurrentDictionary<ASFuelControl.Common.FuelPoint, ConcurrentQueue<ASFuelControl.Common.FuelPointValues>> internalQueue = new ConcurrentDictionary<ASFuelControl.Common.FuelPoint, ConcurrentQueue<ASFuelControl.Common.FuelPointValues>>();

        public string CommunicationPort { set; get; }
        public Common.Enumerators.CommunicationTypeEnum CommunicationType
        {
            get
            {
                return comType;
            }
            set
            {
                comType = value;
            }
        }

        public ASFuelControl.Common.Enumerators.ControllerTypeEnum ControllerType { set; get; }

        public bool IsConnected
        {
            get { return this.controller.IsConnected; }
        }

        public Controller()
        {
            ControllerType = Common.Enumerators.ControllerTypeEnum.Millenium;
            controller.StatusChanged += new EventHandler<MilleniumConnector.StatusChangedEventArgs>(controller_StatusChanged);
            controller.TotalsUpdated += new EventHandler<MilleniumConnector.TotalsUpdatedEventArgs>(controller_TotalsUpdated);
            controller.ValuesChanged += new EventHandler<MilleniumConnector.ValuesChangedEventArgs>(controller_ValuesChanged);
        }

        public void Connect()
        {
            this.controller.Connect(this.CommunicationPort);
        }

        public void DisConnect()
        {
            this.controller.Disconnect();
        }

        public virtual Common.Sales.SaleData GetSale(int channel, int address, int nozzleId)
        {
            return null;
        }

        public void AddFuelPoint(int channel, int address, int nozzleCount)
        {
            this.AddFuelPoint(channel, address, nozzleCount, 2, 3);
        }

        public decimal GetNozzleTotalVolume(int channel, int address, int nozzleId)
        {
            throw (new Exception("Not Implemented"));
        }

        public decimal GetNozzleTotalPrice(int channel, int address, int nozzleId)
        {
            throw (new Exception("Not Implemented"));
        }

        public void AddFuelPoint(int channel, int address, int nozzleCount, int decimalPlaces, int untiPriceDecimalPlaces)
        {
            byte[] bytes = BitConverter.GetBytes(channel);
            MilleniumConnector.FuelPoint fpi = controller.AddFuelPoint(address, nozzleCount);
            foreach (MilleniumConnector.Nozzle nozzle in fpi.Nozzles)
            {
                nozzle.PriceDecimalPlaces = untiPriceDecimalPlaces;
                nozzle.VolumeDecimalPlaces = decimalPlaces;
            }

            ASFuelControl.Common.FuelPoint fp1 = new Common.FuelPoint();
            fp1.Address = (byte)address;
            fp1.Channel = channel;
            fp1.NozzleCount = nozzleCount;
            fp1.DecimalPlaces = 2;
            fp1.UnitPriceDecimalPlaces = 3;
            if (this.internalQueue.ContainsKey(fp1))
                return;
            this.internalQueue.TryAdd(fp1, new ConcurrentQueue<ASFuelControl.Common.FuelPointValues>());
        }

        public void AddAtg(int channel, int address)
        {
        }

        public bool AuthorizeDispenser(int channel, int address)
        {
            byte adressId = (byte)address;
            MilleniumConnector.FuelPoint fuelPoint = controller.FuelPoints.Where(fp => fp.Channel == channel && fp.AddressId == adressId).FirstOrDefault();
            if (fuelPoint == null)
                return false;
            if (fuelPoint.ActiveNozzle != null)
            {
                controller.AuthorizeNozzle(fuelPoint.ActiveNozzle);
            }
            return true;
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
            MilleniumConnector.FuelPoint fp = this.GetFuelPoint(channel, address);
            if (fp == null)
                return false;
            MilleniumConnector.Nozzle nozzle = fp.Nozzles[nozzleId - 1];
            nozzle.UnitPrice = (int)((double)newPrice * Math.Pow(10, nozzle.PriceDecimalPlaces));
            return true;
        }

        public Common.FuelPointValues GetDispenserValues(int channel, int address)
        {
            KeyValuePair<ASFuelControl.Common.FuelPoint, ConcurrentQueue<ASFuelControl.Common.FuelPointValues>> pair = this.internalQueue.Where(iv => iv.Key.Channel == channel && iv.Key.Address == address).FirstOrDefault();
            if (pair.Key == null)
                return null;
            Common.FuelPoint fuelPoint = pair.Key;
            //Console.WriteLine("QUEUE COUNT : " + this.internalQueue[fuelPoint].Count);
            if (this.internalQueue[fuelPoint].Count > 0)
            {
                ASFuelControl.Common.FuelPointValues vals;
                this.internalQueue[fuelPoint].TryDequeue(out vals);
                return vals;
            }
            return null;
        }

        public void GetNozzleTotalValues(int channel, int address, int nozzleId)
        {
            MilleniumConnector.FuelPoint fuelPoint = this.GetFuelPoint(channel, address);
            MilleniumConnector.Nozzle nz = fuelPoint.Nozzles.Where(n=>n.NozzleId == nozzleId).FirstOrDefault();  
            if(nz == null)
                return;
            controller.GetTotalsNozzle(nz);
        }

        public decimal[] GetPrices(int channel, int address)
        {
            MilleniumConnector.FuelPoint fp = this.GetFuelPoint(channel, address);
            List<decimal> pr = new List<decimal>();
            foreach (MilleniumConnector.Nozzle nozzle in fp.Nozzles)
            {
                pr.Add((decimal)nozzle.UnitPrice / (decimal)Math.Pow(10, nozzle.PriceDecimalPlaces));
            }
            return pr.ToArray();
        }

        public decimal GetNozzlePrice(int channel, int address, int nozzleId)
        {
            throw new NotImplementedException();
        }

        public decimal GetNozzleTotalizer(int channel, int address, int nozzleId)
        {
            throw new NotImplementedException();
        }

        public Common.TankValues GetTankValues(int channel, int address)
        {
            return new Common.TankValues();
        }

        private void EnqueValues(ASFuelControl.Common.FuelPoint fuelPoint, ASFuelControl.Common.FuelPointValues values)
        {
            MilleniumConnector.FuelPoint fPoint = this.GetFuelPoint(fuelPoint.Channel, fuelPoint.Address);
            if (fPoint == null)
                return;
            if (this.internalQueue[fuelPoint].Count == 0)
                this.internalQueue[fuelPoint].Enqueue(values);
            else
            {
                ASFuelControl.Common.FuelPointValues oldValues = this.internalQueue[fuelPoint].Last();
                if (oldValues.Status != values.Status)
                    this.internalQueue[fuelPoint].Enqueue(values);
                else
                {
                    ASFuelControl.Common.FuelPointValues vals;
                    this.internalQueue[fuelPoint].TryDequeue(out vals);
                    this.internalQueue[fuelPoint].Enqueue(values);
                }
            }
        }

        private MilleniumConnector.FuelPoint GetFuelPoint(int channel, int address)
        {
            byte adressId = (byte)address;
            MilleniumConnector.FuelPoint fuelPoint = controller.FuelPoints.Where(fp => fp.Channel == channel && fp.AddressId == adressId).FirstOrDefault();
            return fuelPoint;
        }

        void controller_ValuesChanged(object sender, MilleniumConnector.ValuesChangedEventArgs e)
        {
            MilleniumConnector.FuelPoint fp = e.FuelPoint;
            if (fp == null)
                return;

            int channelId = BitConverter.ToInt32(e.FuelPoint.Identity, 0);
            KeyValuePair<ASFuelControl.Common.FuelPoint, ConcurrentQueue<ASFuelControl.Common.FuelPointValues>> pair = this.internalQueue.Where(iv => iv.Key.Channel == channelId && iv.Key.Address == fp.AddressId).FirstOrDefault();
            if (pair.Key == null)
                return;
            Common.FuelPoint fuelPoint = pair.Key;
            if (fuelPoint == null)
                return;

            ASFuelControl.Common.FuelPointValues values = new ASFuelControl.Common.FuelPointValues();

            if (fp.ActiveNozzle != null)
                values.ActiveNozzle = fp.ActiveNozzle.NozzleIndex - 1;
            else
                values.ActiveNozzle = -1;

            if (fp.Status == Common.Enumerators.FuelPointStatusEnum.Work)
            {
                values.CurrentPriceTotal = fp.Nozzles[0].SalePrice;
                values.CurrentVolume = fp.Nozzles[0].SaleVolume;
            }
            else
            {
                values.CurrentPriceTotal = fp.LastSalePrice;
                values.CurrentVolume = fp.LastSaleVolume;
            }

            //Console.WriteLine(fp.DispensedAmount);

            if (fp.ActiveNozzle != null)
            {
                values.CurrentSalePrice = fp.ActiveNozzle.UnitPrice;
            }
            values.Status = (ASFuelControl.Common.Enumerators.FuelPointStatusEnum)(int)fp.Status;

            this.EnqueValues(fuelPoint, values);
        }

        void controller_TotalsUpdated(object sender, MilleniumConnector.TotalsUpdatedEventArgs e)
        {
            if (this.TotalsRecieved != null)
            {
                int nozzleIndex = e.Nozzle.NozzleIndex;
                int channelId = BitConverter.ToInt32(e.Nozzle.ParentFuelPoint.Identity, 0);
                KeyValuePair<ASFuelControl.Common.FuelPoint, ConcurrentQueue<ASFuelControl.Common.FuelPointValues>> pair = this.internalQueue.Where(iv => iv.Key.Channel == channelId && iv.Key.Address == e.Nozzle.ParentFuelPoint.AddressId).FirstOrDefault();
                if (pair.Key == null)
                    return;
                Common.FuelPoint fuelPoint = pair.Key;
                if (nozzleIndex > fuelPoint.NozzleCount)
                    return;
                this.TotalsRecieved(this, new Common.TotalsEventArgs(fuelPoint, (int)nozzleIndex, (decimal)e.Nozzle.Totalizer, 0));
            }
        }

        void controller_StatusChanged(object sender, MilleniumConnector.StatusChangedEventArgs e)
        {
            MilleniumConnector.FuelPoint fp = e.FuelPoint;
            if (fp == null)
                return;

            int channelId = BitConverter.ToInt32(e.FuelPoint.Identity, 0);

            KeyValuePair<ASFuelControl.Common.FuelPoint, ConcurrentQueue<ASFuelControl.Common.FuelPointValues>> pair = this.internalQueue.Where(iv => iv.Key.Channel == channelId && iv.Key.Address == fp.AddressId).FirstOrDefault();
            if (pair.Key == null)
                return;
            Common.FuelPoint fuelPoint = pair.Key;


            ASFuelControl.Common.FuelPointValues values = new ASFuelControl.Common.FuelPointValues();
            if (fp.ActiveNozzle != null)
                values.ActiveNozzle = fp.ActiveNozzle.NozzleIndex - 1;
            else
                values.ActiveNozzle = -1;

            if (fp.Status == Common.Enumerators.FuelPointStatusEnum.Work)
            {
                values.CurrentPriceTotal = fp.ActiveNozzle.SalePrice;
                values.CurrentVolume = fp.ActiveNozzle.SaleVolume;
            }
            else
            {
                values.CurrentPriceTotal = fp.LastSalePrice;
                values.CurrentVolume = fp.LastSaleVolume;
            }

            if (fp.ActiveNozzle != null)
            {
                values.CurrentSalePrice = fp.ActiveNozzle.UnitPrice;
            }
            values.Status = (ASFuelControl.Common.Enumerators.FuelPointStatusEnum)(int)fp.Status;


            this.EnqueValues(fuelPoint, values);
        }
    }
}
