using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;

namespace ASFuelControl.Common
{
    public class FuelPumpControllerBase : IController
    {
        #region public event definitions

        public event EventHandler<TotalsEventArgs> TotalsRecieved;
        public event EventHandler<FuelPointValuesArgs> ValuesRecieved;

        #endregion

        #region private variables

        private Enumerators.ControllerTypeEnum controllerType;
        private bool isConnected = false;
        protected List<FuelPoint> fuelPoints = new List<FuelPoint>();
        protected ConcurrentDictionary<ASFuelControl.Common.FuelPoint, ConcurrentQueue<ASFuelControl.Common.FuelPointValues>> internalQueue = new ConcurrentDictionary<ASFuelControl.Common.FuelPoint, ConcurrentQueue<ASFuelControl.Common.FuelPointValues>>();
        private IFuelProtocol controller = null;
        private List<Guid> salesCompleted = new List<Guid>();

        #endregion

        #region public properties

        public Enumerators.ControllerTypeEnum ControllerType
        {
            get
            {
                return this.controllerType;
            }
            set
            {
                this.controllerType = value;
            }
        }

        public string CommunicationPort
        {
            set 
            {
                if (this.Controller != null)
                    this.Controller.CommunicationPort = value;
            }
            get 
            {
                if (this.Controller == null)
                    return "";
                return this.Controller.CommunicationPort;
            }
        }

        public Common.Enumerators.CommunicationTypeEnum CommunicationType
        {
            set;
            get;
        }

        public bool IsConnected
        {
            get 
            {
                if (this.Controller == null)
                    return false;
                return this.Controller.IsConnected; 
            }
        }

        public IFuelProtocol Controller
        {
            set
            {
                this.controller = value;
                if (this.controller == null)
                    return;
                this.controller.DataChanged += new EventHandler<FuelPointValuesArgs>(controller_DataChanged);
                this.controller.TotalsRecieved += new EventHandler<TotalsEventArgs>(controller_TotalsRecieved);
                this.controller.DispenserStatusChanged += new EventHandler<FuelPointValuesArgs>(controller_DispenserStatusChanged);
            }
            get { return this.controller; }
        }

        #endregion

        #region public Methods

        public virtual void Connect()
        {
            if (this.Controller == null)
                return;
            //this.Controller.DataChanged -= new EventHandler<FuelPointValuesArgs>(Controller_DataChanged);
            //this.Controller.DataChanged += new EventHandler<FuelPointValuesArgs>(Controller_DataChanged);
            //this.Controller.DispenserStatusChanged -= new EventHandler<FuelPointValuesArgs>(Controller_DispenserStatusChanged);
            //this.Controller.DispenserStatusChanged += new EventHandler<FuelPointValuesArgs>(Controller_DispenserStatusChanged);
            //this.Controller.TotalsRecieved -= new EventHandler<TotalsEventArgs>(Controller_TotalsRecieved);
            //this.Controller.TotalsRecieved += new EventHandler<TotalsEventArgs>(Controller_TotalsRecieved);
            this.Controller.CommunicationPort = this.CommunicationPort;
            //this.Controller.FuelPoints = this.fuelPoints.ToArray();
            this.Controller.Connect();
        }

        public virtual void DisConnect()
        {
            if (this.Controller == null)
                return;
            this.fuelPoints.Clear();
            this.Controller.ClearFuelPoints();
            this.Controller.Disconnect();
        }

        #endregion

        #region private methods

        protected FuelPoint GetFulePoint(int channel, int address)
        {
            FuelPoint fp = this.Controller.FuelPoints.Where(f => f.Address == address && f.Channel == channel).FirstOrDefault();
            if (fp == null)
                return null;
            return fp;
        }

        protected void EnqueValues(ASFuelControl.Common.FuelPoint fuelPoint, ASFuelControl.Common.FuelPointValues values)
        {
            fuelPoint.LastValues = values;
            if (!this.internalQueue.ContainsKey(fuelPoint))
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

        #endregion

        #region public fuelpoint methods

        public virtual void AddFuelPoint(int channel, int address, int nozzleCount)
        {
            this.AddFuelPoint(channel, address, nozzleCount, 2, 2, 3);
        }

        public virtual void AddFuelPoint(int channel, int address, int nozzleCount, int decimalPlaces, int volumeDecimalPlaces, int untiPriceDecimalPlaces)
        {
            FuelPoint fpi = new FuelPoint();
            fpi.NozzleCount = nozzleCount;
            this.fuelPoints.Add(fpi);
            fpi.Address = address;
            fpi.Channel = channel;
            fpi.UnitPriceDecimalPlaces = untiPriceDecimalPlaces;
            fpi.DecimalPlaces = decimalPlaces;
            this.internalQueue.TryAdd(fpi, new ConcurrentQueue<ASFuelControl.Common.FuelPointValues>());

            this.Controller.AddFuelPoint(fpi);
            //fpi.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(fpi_PropertyChanged);
        }

        public virtual bool AuthorizeDispenser(int channel, int address)
        {
            FuelPoint fp = this.GetFulePoint(channel, address);
            if (fp == null)
                return false;
            if (fp.ActiveNozzle == null)
                return false;
            fp.QueryAuthorize = true;
            fp.PresetVolume = 999999;
            fp.PresetAmount = 999999;
            return true;
        }

        public virtual bool AuthorizeDispenserVolumePreset(int channel, int address, decimal volume)
        {
            FuelPoint fp = this.GetFulePoint(channel, address);
            if (fp == null)
                return false;
            if (fp.ActiveNozzle == null)
                return false;
            fp.QueryAuthorize = true;
            fp.PresetVolume = volume;
            fp.PresetAmount = 999999;
            return true;
        }

        public virtual bool AuthorizeDispenserAmountPreset(int channel, int address, decimal price)
        {
            FuelPoint fp = this.GetFulePoint(channel, address);
            if (fp == null)
                return false;
            if (fp.ActiveNozzle == null)
                return false;
            fp.QueryAuthorize = true;
            fp.PresetVolume = 999999;
            fp.PresetAmount = price;
            return true;
        }

        public virtual bool SetDispenserStatus(int channel, int address, Enumerators.FuelPointStatusEnum status)
        {
            throw new NotImplementedException();
        }

        public virtual bool SetDispenserSalesPrice(int channel, int address, decimal[] price)
        {
            FuelPoint fp = this.GetFulePoint(channel, address);
            if (fp == null)
                return false;
            if(fp.NozzleCount != price.Length)
                return false;
            for (int i = 0; i < price.Length; i++)
            {
                this.SetNozzlePrice(channel, address, fp.Nozzles[i].Index, price[i]);
                System.Threading.Thread.Sleep(100);
            }
            return true;
        }

        public virtual bool SetNozzlePrice(int channel, int address, int nozzleId, decimal newPrice)
        {
            FuelPoint fp = this.GetFulePoint(channel, address);
            if (fp == null)
                return false;
            fp.Nozzles[nozzleId - 1].UnitPrice = newPrice;
            fp.Nozzles[nozzleId - 1].UntiPriceInt = (int)(newPrice * (decimal)Math.Pow(10, fp.UnitPriceDecimalPlaces));
            fp.QuerySetPrice = true;
            return true;
        }

        DateTime lastValuesSent = DateTime.MinValue;
        public virtual FuelPointValues GetDispenserValues(int channel, int address)
        {
            
            KeyValuePair<ASFuelControl.Common.FuelPoint, ConcurrentQueue<ASFuelControl.Common.FuelPointValues>> pair = this.internalQueue.Where(iv => iv.Key.Channel == channel && iv.Key.Address == address).FirstOrDefault();
            if (pair.Key == null)
                return null;
            Common.FuelPoint fuelPoint = pair.Key;
            if (this.internalQueue[fuelPoint].Count > 0)
            {
                ASFuelControl.Common.FuelPointValues vals;
                this.internalQueue[fuelPoint].TryDequeue(out vals);
                return vals;
            }
            //if (DateTime.Now.Subtract(lastValuesSent).TotalSeconds > 2)
            //{
            //    lastValuesSent = DateTime.Now;
            //    return fuelPoint.LastValues;
            //}
            return null;
        }

        public virtual void GetNozzleTotalValues(int channel, int address, int nozzleId)
        {
            FuelPoint fp = this.GetFulePoint(channel, address);
            if (fp == null)
                return;
            fp.Nozzles[nozzleId - 1].QueryTotals = true;
        }

        public virtual decimal GetNozzleTotalVolume(int channel, int address, int nozzleId)
        {
            FuelPoint fp = this.GetFulePoint(channel, address);
            if(fp == null)
                return 0;
            if (fp.NozzleCount > nozzleId)
                return 0;
            return fp.Nozzles[nozzleId - 1].TotalVolume;
        }

        public virtual decimal GetNozzleTotalPrice(int channel, int address, int nozzleId)
        {
            FuelPoint fp = this.GetFulePoint(channel, address);
            if (fp == null)
                return 0;
            if (fp.NozzleCount > nozzleId)
                return 0;
            return fp.Nozzles[nozzleId - 1].TotalVolume;
        }

        public virtual decimal[] GetPrices(int channel, int address)
        {
            FuelPoint fp = this.GetFulePoint(channel, address);
            if (fp == null)
                return new decimal[]{};
            return fp.Nozzles.Select(n=>n.UnitPrice).ToArray();
        }

        public virtual decimal GetNozzlePrice(int channel, int address, int nozzleId)
        {
            FuelPoint fp = this.GetFulePoint(channel, address);
            if (fp == null)
                return 0;
            if (fp.NozzleCount > nozzleId)
                return 0;
            return fp.Nozzles[nozzleId - 1].UnitPrice;
        }

        public virtual decimal GetNozzleTotalizer(int channel, int address, int nozzleId)
        {
            FuelPoint fp = this.GetFulePoint(channel, address);
            if (fp == null)
                return 0;
            if (fp.NozzleCount > nozzleId)
                return 0;
            if (!fp.Initialized)
                return -1;
            return fp.Nozzles[nozzleId - 1].TotalVolume;
        }

        public virtual Sales.SaleData GetSale(int channel, int address, int nozzleId)
        {
            FuelPoint fp  = this.GetFulePoint(channel, address);
            if (fp == null)
                return null;
            Common.Nozzle nz = fp.Nozzles.Where(n => n.Index == nozzleId).FirstOrDefault();
            if (nz == null)
                return null;

            Common.Sales.SaleData lastSale = nz.GetLastSale();
            if (lastSale == null)
                return null;
            lastSale.SaleProcessed = true;
            return lastSale;

            //Common.Sales.SaleData sale = nz.GetLastSale();
            //if (sale == null)
            //    return null;
            //if(sale.SaleProcessed)
            //    return null;
            //if (this.salesCompleted.Contains(sale.SalesId))
            //{
            //    sale.SaleProcessed = true;
            //    this.salesCompleted.Remove(sale.SalesId);
            //    return null;
            //}
            //sale.SaleProcessed = true;
            //this.salesCompleted.Add(sale.SalesId);
            //if (this.salesCompleted.Count > 500)
            //    this.salesCompleted.RemoveAt(0);
            
            //return sale;

            //Common.Sales.SaleData sale = nz.CurrentSale;
            //if (sale != null && sale.SaleCompleted)
            //{
            //    nz.CurrentSale = null;
            //    return sale;
            //}
            //return null;
        }

        #endregion

        #region public tank methods

        public void AddAtg(int channel, int address)
        {
            throw new NotImplementedException();
        }

        public TankValues GetTankValues(int channel, int address)
        {
            throw new NotImplementedException();
        }

        #endregion

        protected virtual void OnTransactionCompleted(TotalsEventArgs e)
        {
            Common.FuelPoint fp = e.CurrentFuelPoint;
            if (fp == null)
                return;

            Common.Nozzle nz = fp.Nozzles.Where(n => n.Index == e.NozzleId).FirstOrDefault();
            if (this.TotalsRecieved != null)
            {
                this.TotalsRecieved(this, new Common.TotalsEventArgs(fp, (int)e.NozzleId, (decimal)nz.TotalVolume, (decimal)0));
            }

            Common.Sales.SaleData sale = nz.saleHandler.AddTotalizer(e.TotalVolume, nz.UnitPrice, e.CurrentFuelPoint.DispensedVolume, e.CurrentFuelPoint.DispensedAmount);
            fp.Status = fp.DispenserStatus;
            Common.FuelPointValues values = new Common.FuelPointValues();
            values.Status = fp.DispenserStatus;
            values.CurrentPriceTotal = sale.TotalPrice;
            values.CurrentSalePrice = sale.UnitPrice;
            values.CurrentVolume = sale.TotalVolume;
            values.HasTotalMisfunction = nz.TotalMisfunction;

            this.EnqueValues(fp, values);
        }

        protected virtual void OnStatusChanged(FuelPointValuesArgs e)
        {
            Common.FuelPoint fuelPoint = e.CurrentFuelPoint;
            if (fuelPoint == null)
                return;
            
            
            decimal dividor = (decimal)Math.Pow(10, (double)fuelPoint.DecimalPlaces);

            ASFuelControl.Common.FuelPointValues values = new ASFuelControl.Common.FuelPointValues();
            values.ActiveNozzle = fuelPoint.ActiveNozzleIndex;
            if (fuelPoint.Status !=  Enumerators.FuelPointStatusEnum.Offline)
            {
                values.CurrentPriceTotal = fuelPoint.DispensedAmount;
                values.CurrentVolume = fuelPoint.DispensedVolume;
            }
            if (fuelPoint.ActiveNozzle != null)
            {
                values.ActiveNozzle = fuelPoint.ActiveNozzleIndex;
            }

            if (fuelPoint.DispenserStatus == Enumerators.FuelPointStatusEnum.Nozzle)
            {
                fuelPoint.DispensedAmount = 0;
                fuelPoint.DispensedVolume = 0;
            }

            values.Status = fuelPoint.Status;
            values.TotalVolumes = fuelPoint.Nozzles.Select(n => n.TotalVolume).ToArray();

            
            Common.Nozzle salesNozzle = fuelPoint.ActiveNozzle;
            if (salesNozzle == null)
                salesNozzle = fuelPoint.LastActiveNozzle;

            if (salesNozzle != null)
                values.HasTotalMisfunction = salesNozzle.TotalMisfunction;

            if (fuelPoint.DispenserStatus == Enumerators.FuelPointStatusEnum.Idle)
            {
                if (fuelPoint.ActiveNozzle != null)
                    fuelPoint.ActiveNozzle.QueryTotals = true;

                else if (fuelPoint.LastActiveNozzle != null)
                    fuelPoint.LastActiveNozzle.QueryTotals = true;
            }
            else if(fuelPoint.DispenserStatus == Enumerators.FuelPointStatusEnum.Offline)
            {
                foreach (Common.Nozzle n in fuelPoint.Nozzles)
                    n.CurrentSale = null;
            }
            
            fuelPoint.Status = values.Status;
            this.EnqueValues(fuelPoint, values);
        }

        void controller_TotalsRecieved(object sender, TotalsEventArgs e)
        {
            this.OnTransactionCompleted(e);
        }

        void controller_DataChanged(object sender, FuelPointValuesArgs e)
        {
            Common.FuelPoint fuelPoint = e.CurrentFuelPoint;

            if (fuelPoint == null)
                return;

            decimal dividor = (decimal)Math.Pow(10, (double)fuelPoint.DecimalPlaces);

            if (fuelPoint.ActiveNozzle != null && fuelPoint.ActiveNozzle.CurrentSale != null)
            {
                fuelPoint.ActiveNozzle.CurrentSale.UnitPrice = e.Values.CurrentSalePrice;
                fuelPoint.ActiveNozzle.CurrentSale.SaleEndTime = DateTime.Now;
                e.Values.HasTotalMisfunction = fuelPoint.ActiveNozzle.TotalMisfunction;
            }
            e.Values.ActiveNozzle = e.CurrentNozzleId - 1;
            e.Values.TotalVolumes = fuelPoint.Nozzles.Select(n => n.TotalVolume).ToArray();
            e.Values.Status = (ASFuelControl.Common.Enumerators.FuelPointStatusEnum)(int)fuelPoint.Status;
            fuelPoint.Status = e.Values.Status;
            
            this.EnqueValues(fuelPoint, e.Values);
        }

        void controller_DispenserStatusChanged(object sender, FuelPointValuesArgs e)
        {
            this.OnStatusChanged(e);

        }
    }
}
