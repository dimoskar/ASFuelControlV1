using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;

namespace ASFuelControl.Box69
{
    public class Controller : Common.IController
    {
        Box69Protocol controller = new Box69Protocol();
        private List<FuelPoint> fuelPoints = new List<FuelPoint>();
        private ConcurrentDictionary<ASFuelControl.Common.FuelPoint, ConcurrentQueue<ASFuelControl.Common.FuelPointValues>> internalQueue = new ConcurrentDictionary<ASFuelControl.Common.FuelPoint, ConcurrentQueue<ASFuelControl.Common.FuelPointValues>>();
        private List<Guid> salesCompleted = new List<Guid>();

        public event EventHandler<Common.TotalsEventArgs> TotalsRecieved;
        public event EventHandler<Common.FuelPointValuesArgs> ValuesRecieved;

        public ASFuelControl.Common.Enumerators.ControllerTypeEnum ControllerType { set; get; }

        public Controller()
        {
            ControllerType = Common.Enumerators.ControllerTypeEnum.Box69;
        }

        public string CommunicationPort
        {
            set;
            get;
        }

        public Common.Enumerators.CommunicationTypeEnum CommunicationType
        {
            set;
            get;
        }

        public bool IsConnected
        {
            get { return this.controller.IsConnected; }
        }

        public void Connect()
        {
            this.controller.CommunicationPort = this.CommunicationPort;
            this.controller.DataChanged -= new EventHandler(fp_DataChanged);
            this.controller.TotalsRecieved -= new EventHandler<TotalsEventArgs>(fp_TotalsRecieved);
            this.controller.DataChanged += new EventHandler(fp_DataChanged);
            this.controller.TotalsRecieved += new EventHandler<TotalsEventArgs>(fp_TotalsRecieved);
            this.controller.Connect();
        }

        public void DisConnect()
        {
            this.controller.DisConnect();
        }

        public virtual Common.Sales.SaleData GetSale(int channel, int address, int nozzleId)
        {
            KeyValuePair<ASFuelControl.Common.FuelPoint, ConcurrentQueue<ASFuelControl.Common.FuelPointValues>> pair = this.internalQueue.Where(iv => iv.Key.Channel == channel && iv.Key.Address == address).FirstOrDefault();
            if (pair.Key == null)
                return null;
            Common.FuelPoint fp = pair.Key;
            if (fp == null)
                return null;
            Common.Nozzle nz = fp.Nozzles.Where(n => n.Index == nozzleId).FirstOrDefault();
            if (nz == null)
                return null;

            Common.Sales.SaleData sale = nz.GetLastSale();
            if (sale == null)
                return null;
            if (this.salesCompleted.Contains(sale.SalesId))
            {
                sale.SaleProcessed = true;
                return null;
            }
            sale.SaleProcessed = true;
            this.salesCompleted.Add(sale.SalesId);
            if (this.salesCompleted.Count > 500)
                this.salesCompleted.RemoveAt(0);

            return sale;
        }

        public void AddFuelPoint(int channel, int address, int nozzleCount)
        {
            this.AddFuelPoint(channel, address, nozzleCount, 2, 2, 3);
        }

        public void AddFuelPoint(int channel, int address, int nozzleCount, int decimalPlaces, int volumeDecimalPlaces, int untiPriceDecimalPlaces)
        {
            byte[] bytes = BitConverter.GetBytes(channel);
            FuelPoint fpi = new FuelPoint(nozzleCount);
            this.fuelPoints.Add(fpi);
            fpi.AddressId = address;
            fpi.PriceDecimalPlaces = untiPriceDecimalPlaces;
            fpi.VolumeDecimalPlaces = decimalPlaces;
            foreach (Nozzle nozzle in fpi.Nozzles)
            {
                nozzle.PriceDecimalPlaces = untiPriceDecimalPlaces;
                nozzle.VolumeDecimalPlaces = decimalPlaces;
            }
            this.controller.AddFuelPoint(fpi);
            fpi.StatusChanged += new EventHandler<StatusChangedEventArgs>(fp_StatusChanged);

            ASFuelControl.Common.FuelPoint fp1 = new Common.FuelPoint();
            fp1.Address = address;
            fp1.Channel = channel;
            fp1.NozzleCount = nozzleCount;
            fp1.DecimalPlaces = decimalPlaces;
            fp1.VolumeDecimalPlaces = volumeDecimalPlaces;
            fp1.UnitPriceDecimalPlaces = untiPriceDecimalPlaces;
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
            FuelPoint fuelPoint = controller.FuelPoints.Where(fp => fp.AddressId == adressId).FirstOrDefault();
            if (fuelPoint == null)
                return false;
            if (fuelPoint.ActiveNozzle != null)
            {
                fuelPoint.ActiveNozzle.QueryAuthorize = true;
                fuelPoint.QueryAuthorize = true;
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
            FuelPoint fp = this.GetFuelPoint(channel, address);
            if (fp == null)
                return false;
            Nozzle nozzle = fp.Nozzles[nozzleId - 1];
            nozzle.UnitPrice = (int)(newPrice * (decimal)Math.Pow(10, nozzle.PriceDecimalPlaces));
            fp.QuerySetPrice = true;
            //this.controller.SetPrices(fp);
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
            FuelPoint fuelPoint = this.GetFuelPoint(channel, address);
            Nozzle nz = fuelPoint.Nozzles.Where(n => n.NozzleIndex == nozzleId).FirstOrDefault();
            if (nz == null)
                return;
            nz.QueryTotals = true;
            nz.ParentFuelPoint.QueryTotals = true;
        }

        public decimal GetNozzleTotalVolume(int channel, int address, int nozzleId)
        {
            FuelPoint fp = this.GetFuelPoint(channel, address);
            if (fp == null)
                return 0;
            return fp.Nozzles[nozzleId - 1].Totalizer;
        }

        public decimal GetNozzleTotalPrice(int channel, int address, int nozzleId)
        {
            throw (new Exception("Not Implemented"));
        }

        public decimal[] GetPrices(int channel, int address)
        {
            FuelPoint fp = this.GetFuelPoint(channel, address);
            List<decimal> pr = new List<decimal>();
            foreach (Nozzle nozzle in fp.Nozzles)
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
            FuelPoint fuelPoint = this.GetFuelPoint(channel, address);
            if (fuelPoint == null)
                return 0;
            Nozzle nz = fuelPoint.Nozzles.Where(n => n.NozzleIndex == nozzleId).FirstOrDefault();
            if (nz == null)
                return -1;
            return nz.Totalizer;
        }

        public Common.TankValues GetTankValues(int channel, int address)
        {
            throw new NotImplementedException();
        }

        private FuelPoint GetFuelPoint(int channel, int address)
        {
            byte adressId = (byte)address;
            FuelPoint fuelPoint = controller.FuelPoints.Where(fp => fp.AddressId == adressId).FirstOrDefault();
            return fuelPoint;
        }

        private void EnqueValues(ASFuelControl.Common.FuelPoint fuelPoint, ASFuelControl.Common.FuelPointValues values)
        {
            try
            {
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
            catch
            {
            }
        }

        void fp_TotalsRecieved(object sender, TotalsEventArgs e)
        {
            FuelPoint fuelPoint = sender as FuelPoint;
            KeyValuePair<ASFuelControl.Common.FuelPoint, ConcurrentQueue<ASFuelControl.Common.FuelPointValues>> pair =
                this.internalQueue.Where(iv => iv.Key.Address == fuelPoint.AddressId).FirstOrDefault();
            if (pair.Key == null)
                return;

            Common.FuelPoint fp = pair.Key;
            if (fp == null)
                return;

            Common.Nozzle nz = fp.Nozzles.Where(n => n.Index == e.NozzleID).FirstOrDefault();
            if (this.TotalsRecieved != null)
            {
                this.TotalsRecieved(this, new Common.TotalsEventArgs(fp, (int)e.NozzleID, (decimal)nz.TotalVolume, (decimal)0));
            }

            Common.Sales.SaleData sale = nz.saleHandler.AddTotalizer(e.Nozzle.Totalizer, e.Nozzle.ParentFuelPoint.LastSaleUnitPrice, e.Nozzle.ParentFuelPoint.LastSaleVolume, e.Nozzle.ParentFuelPoint.LastSalePrice);
            fp.Status = fp.DispenserStatus;
            Common.FuelPointValues values = new Common.FuelPointValues();
            values.Status = fp.DispenserStatus;
            values.CurrentPriceTotal = sale.TotalPrice;
            values.CurrentSalePrice = sale.UnitPrice;
            values.CurrentVolume = sale.TotalVolume;

            this.EnqueValues(fp, values);
        }

        void fp_DataChanged(object sender, EventArgs e)
        {
            FuelPoint fp = sender as FuelPoint;
            if (fp == null)
                return;
            
            KeyValuePair<ASFuelControl.Common.FuelPoint, ConcurrentQueue<ASFuelControl.Common.FuelPointValues>> pair = this.internalQueue.Where(iv => iv.Key.Address == fp.AddressId).FirstOrDefault();
            if (pair.Key == null)
                return;
            Common.FuelPoint fuelPoint = pair.Key;
            if (fuelPoint == null)
                return;
            decimal dividor = (decimal)Math.Pow(10, (double)fuelPoint.DecimalPlaces);

            ASFuelControl.Common.FuelPointValues values = new ASFuelControl.Common.FuelPointValues();

            values.ActiveNozzle = -1;
            values.CurrentPriceTotal = fp.LastSalePrice;
            values.CurrentVolume = fp.LastSaleVolume;

            //Console.WriteLine(fp.DispensedAmount);

            if (fp.ActiveNozzle != null)
            {
                fuelPoint.ActiveNozzleIndex = fp.ActiveNozzle.NozzleIndex - 1;
                values.ActiveNozzle = fp.ActiveNozzle.NozzleIndex - 1;
                values.CurrentSalePrice = fp.LastSaleUnitPrice;
                if(fuelPoint.ActiveNozzle.CurrentSale == null)
                    fuelPoint.ActiveNozzle.CurrentSale = new Common.Sales.SaleData();
                fuelPoint.ActiveNozzle.CurrentSale.UnitPrice = fp.LastSaleUnitPrice;
                
                fuelPoint.ActiveNozzle.CurrentSale.TotalizerEnd = fuelPoint.ActiveNozzle.TotalVolume;
                if (values.Status == Common.Enumerators.FuelPointStatusEnum.TransactionCompleted || values.Status == Common.Enumerators.FuelPointStatusEnum.TransactionStopped)
                {
                    fuelPoint.ActiveNozzle.CurrentSale.TotalVolume = (fuelPoint.ActiveNozzle.CurrentSale.TotalizerEnd - fuelPoint.ActiveNozzle.CurrentSale.TotalizerStart) / 100;
                    fuelPoint.ActiveNozzle.CurrentSale.TotalPrice = decimal.Round(fuelPoint.ActiveNozzle.CurrentSale.TotalVolume * fuelPoint.ActiveNozzle.CurrentSale.UnitPrice, fuelPoint.DecimalPlaces);
                }
                else
                {
                    fuelPoint.ActiveNozzle.CurrentSale.TotalVolume = fp.LastSaleVolume;
                    fuelPoint.ActiveNozzle.CurrentSale.TotalPrice = decimal.Round(fp.LastSaleVolume * fuelPoint.ActiveNozzle.CurrentSale.UnitPrice, fuelPoint.DecimalPlaces);
                }
                values.CurrentPriceTotal = fuelPoint.ActiveNozzle.CurrentSale.TotalPrice;
                fuelPoint.ActiveNozzle.CurrentSale.SaleEndTime = DateTime.Now;
                
                
            }
            values.TotalVolumes = fuelPoint.Nozzles.Select(n => n.TotalVolume).ToArray();
            values.Status = (ASFuelControl.Common.Enumerators.FuelPointStatusEnum)(int)fp.DispenserStatus;
            
            this.EnqueValues(fuelPoint, values);
        }

        void fp_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            FuelPoint fuelPoint = sender as FuelPoint;

            KeyValuePair<ASFuelControl.Common.FuelPoint, ConcurrentQueue<ASFuelControl.Common.FuelPointValues>> pair = this.internalQueue.Where(iv => iv.Key.Address == fuelPoint.AddressId).FirstOrDefault();
            if (pair.Key == null)
                return;
            Common.FuelPoint fp = pair.Key;
            if (fp == null)
                return;

            //Common.FuelPoint fuelPoint = sender as Common.FuelPoint;
            if (fuelPoint == null)
                return;


            decimal dividor = (decimal)Math.Pow(10, (double)fuelPoint.VolumeDecimalPlaces);

            ASFuelControl.Common.FuelPointValues values = new ASFuelControl.Common.FuelPointValues();
            values.ActiveNozzle = fuelPoint.ActiveNozzle == null ? -1 : fuelPoint.ActiveNozzle.NozzleIndex - 1;
           
            if (fuelPoint.ActiveNozzle != null)
            {
                values.CurrentPriceTotal = fuelPoint.ActiveNozzle.SalePrice;
                values.CurrentVolume = fuelPoint.ActiveNozzle.SaleVolume;
            }
            values.Status = fuelPoint.Status;
            values.TotalVolumes = fuelPoint.Nozzles.Select(n => n.Totalizer).ToArray();


            Nozzle salesNozzle = fuelPoint.ActiveNozzle;
            if (salesNozzle == null)
                salesNozzle = fuelPoint.LastNozzle;

            fuelPoint.DispenserStatus = values.Status;
            fuelPoint.Status = values.Status;

            if (fuelPoint.Status == Common.Enumerators.FuelPointStatusEnum.Idle)
            {
                if (fuelPoint.ActiveNozzle != null)
                {
                    fuelPoint.ActiveNozzle.QueryTotals = true;
                    fuelPoint.QueryTotals = true;
                }
                else if (fuelPoint.LastNozzle != null)
                {
                    fuelPoint.LastNozzle.QueryTotals = true;
                    fuelPoint.QueryTotals = true;
                }
            }
            else if (fuelPoint.Status == Common.Enumerators.FuelPointStatusEnum.Offline)
            {
                foreach (Common.Nozzle n in fp.Nozzles)
                    n.CurrentSale = null;
            }
            fp.Initialized = fuelPoint.Initialized;

            this.EnqueValues(fp, values);
           
        }
    }
}
