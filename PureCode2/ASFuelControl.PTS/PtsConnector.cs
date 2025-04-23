using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;

namespace ASFuelControl.PTS
{
    public class PtsConnector : Common.IController
    {
        #region event definitions

        public event EventHandler<Common.TotalsEventArgs> TotalsRecieved;

        public event EventHandler<Common.FuelPointValuesArgs> ValuesRecieved;

        #endregion

        #region private variables

        private PTSLib.PTS.PTSController controller = new PTSLib.PTS.PTSController();
        private ConcurrentDictionary<ASFuelControl.Common.FuelPoint, ConcurrentQueue<ASFuelControl.Common.FuelPointValues>> internalQueue = new ConcurrentDictionary<ASFuelControl.Common.FuelPoint, ConcurrentQueue<ASFuelControl.Common.FuelPointValues>>();
        private ConcurrentDictionary<ASFuelControl.Common.Tank, ASFuelControl.Common.TankValues> internalTankQueue = new ConcurrentDictionary<ASFuelControl.Common.Tank, ASFuelControl.Common.TankValues>();
        private List<Common.FuelPoint> fuelPoints = new List<Common.FuelPoint>();
        private List<Guid> salesCompleted = new List<Guid>();

        #endregion

        #region public properties

        public string CommunicationPort { set; get; }
        public Common.Enumerators.CommunicationTypeEnum CommunicationType { set; get; }
        public bool IsConnected { get { return this.controller.IsOpen; } }
        
        public ASFuelControl.Common.Enumerators.ControllerTypeEnum ControllerType { set; get; }

        #endregion

        #region Constructors

        public PtsConnector()
        {
            this.ControllerType = Common.Enumerators.ControllerTypeEnum.PTS;
            this.controller.Initialized += new EventHandler(controller_Initialized);
            this.controller.Opened += new EventHandler(controller_Opened);
            this.controller.Closed += new EventHandler(controller_Closed);
        }

        #endregion

        #region public methods

        public void Connect()
        {
            try
            {
                //this.controller.UseExtendedCommands = true;
                this.controller.PortName = this.CommunicationPort;
                this.controller.Open();
            }
            catch
            {
            }
        }

        public void DisConnect()
        {
            this.controller.Close();
        }

        public virtual Common.Sales.SaleData GetSale(int channel, int address, int nozzleId)
        {
            //KeyValuePair<ASFuelControl.Common.FuelPoint, ConcurrentQueue<ASFuelControl.Common.FuelPointValues>> pair = this.internalQueue.Where(iv => iv.Key.Channel == channel && iv.Key.Address == address).FirstOrDefault();
            //if (pair.Key == null)
            //    return null;
            //Common.FuelPoint fuelPoint = pair.Key;
            //if (fuelPoint == null)
            //    return null;
            //Common.Nozzle nz = fuelPoint.Nozzles.Where(n => n.Index == nozzleId).FirstOrDefault();
            //if (nz == null)
            //    return null;
            
            //Common.Sales.SaleData sale = nz.CurrentSale;
            //if (this.controller.FuelPoints == null || this.controller.FuelPoints.Length == 0)
            //    return null;
            //PTSLib.PTS.FuelPoint fp = this.controller.FuelPoints.Where(f => f.PhysicalAddress == address && f.ChannelID == channel).FirstOrDefault();
            //if (fp == null)
            //    return null;
            //if (fp.Nozzles.Where(n => n.SaleCompleted).FirstOrDefault() != null)
            //    return null;
            //if (sale != null && sale.SaleCompleted)
            //{
            //    fp.Nozzles[nz.Index - 1].SaleCompleted = true;
            //    nz.CurrentSale = null;
            //    return sale;
            //}
            //return null;

            KeyValuePair<ASFuelControl.Common.FuelPoint, ConcurrentQueue<ASFuelControl.Common.FuelPointValues>> pair = this.internalQueue.Where(iv => iv.Key.Channel == channel && iv.Key.Address == address).FirstOrDefault();
            if (pair.Key == null)
                return null;
            Common.FuelPoint fuelPoint = pair.Key;
            if (fuelPoint == null)
                return null;
            Common.Nozzle nz = fuelPoint.Nozzles.Where(n => n.Index == nozzleId).FirstOrDefault();
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
            ASFuelControl.Common.FuelPoint fp = new Common.FuelPoint();
            fp.Address = address;
            fp.Channel = channel;
            fp.NozzleCount = nozzleCount;
            fp.DecimalPlaces = 2;
            fp.VolumeDecimalPlaces = 2;
            fp.UnitPriceDecimalPlaces = 3;
            if (this.internalQueue.ContainsKey(fp))
                return;
            this.internalQueue.TryAdd(fp, new ConcurrentQueue<ASFuelControl.Common.FuelPointValues>());
            this.fuelPoints.Add(fp);
        }

        public void AddFuelPoint(int channel, int address, int nozzleCount, int decimalPlaces, int volumeDecimalPlaces, int untiPriceDecimalPlaces)
        {
            ASFuelControl.Common.FuelPoint fp = new Common.FuelPoint();
            fp.Address = address;
            fp.Channel = channel;
            fp.NozzleCount = nozzleCount;
            fp.DecimalPlaces = decimalPlaces;
            fp.VolumeDecimalPlaces = volumeDecimalPlaces;
            fp.UnitPriceDecimalPlaces = untiPriceDecimalPlaces;
            if (this.internalQueue.ContainsKey(fp))
                return;
            this.internalQueue.TryAdd(fp, new ConcurrentQueue<ASFuelControl.Common.FuelPointValues>());
            this.fuelPoints.Add(fp);
        }

        public void AddAtg(int channel, int address)
        {
            ASFuelControl.Common.Tank tank = new Common.Tank();
            tank.Channel = channel;
            tank.Address = address;
            if (this.internalTankQueue.ContainsKey(tank))
                return;
            this.internalTankQueue.TryAdd(tank, null);
        }

        public bool AuthorizeDispenser(int channel, int address)
        {
            PTSLib.PTS.FuelPoint fuelPoint = this.controller.FuelPoints.Where(fp => fp.ChannelID == channel && fp.PhysicalAddress == address).FirstOrDefault();
            if (fuelPoint == null)
                return false;
            if (fuelPoint.Status != PTSLib.PTS.FuelPointStatus.NOZZLE)
                return false;
            if (!fuelPoint.Locked)
            {
                fuelPoint.Lock();
                System.Threading.Thread.Sleep(70);
            }
            if (fuelPoint.Locked)
            {
                //fuelPoint.Nozzles[0].PricePerLiter = 100;
                if (fuelPoint.ActiveNozzleID > 0)
                {
                    fuelPoint.Authorize(PTSLib.PTS.AuthorizeType.Amount, 999999, fuelPoint.ActiveNozzleID);
                    //PTSLib.Nozzle nozzle = fuelPoint.Nozzles.Where(n => n.ID == fuelPoint.ActiveNozzleID).FirstOrDefault();
                    //fuelPoint.GetTotals(nozzle.ID);
                    return true;
                }
            }
            return false;
        }

        public bool AuthorizeDispenserVolumePreset(int channel, int address, decimal volume)
        {
            return false;
        }

        public bool AuthorizeDispenserAmountPreset(int channel, int address, decimal amount)
        {
            return false;
        }

        public bool SetDispenserStatus(int channel, int address, Common.Enumerators.FuelPointStatusEnum status)
        {
            if (status == ASFuelControl.Common.Enumerators.FuelPointStatusEnum.TransactionStopped)
            {
                PTSLib.PTS.FuelPoint fuelPoint = this.controller.FuelPoints.Where(fp => fp.ChannelID == channel && fp.PhysicalAddress == address).FirstOrDefault();
                if (fuelPoint == null)
                    return false;
                fuelPoint.Halt();
                return true;
            }
            return false;
        }

        public bool SetDispenserSalesPrice(int channel, int address, decimal[] price)
        {
            PTSLib.PTS.FuelPoint fuelPoint = this.controller.FuelPoints.Where(fp => fp.ChannelID == channel && fp.PhysicalAddress == address).FirstOrDefault();
            if (fuelPoint == null)
                return false;
            for (int i = 0; i < price.Length; i++)
            {
                fuelPoint.Nozzles[i].PricePerLiter = (int)(1000 * price[i]);
            }

            fuelPoint.SetPrices();
            return true;
        }

        public bool SetNozzlePrice(int channel, int address, int nozzleId, decimal newPrice)
        {
            if (this.controller.FuelPoints == null || this.controller.FuelPoints.Count() == 0)
                return false;
            PTSLib.PTS.FuelPoint fuelPoint = this.controller.FuelPoints.Where(fp => fp.ChannelID == channel && fp.PhysicalAddress == address).FirstOrDefault();
            if (fuelPoint == null)
                return false;
            PTSLib.PTS.Nozzle nozzle = fuelPoint.Nozzles.Where(n => n.ID == nozzleId).FirstOrDefault();
            if (nozzle == null)
                return false;

            KeyValuePair<ASFuelControl.Common.FuelPoint, ConcurrentQueue<ASFuelControl.Common.FuelPointValues>> pair = this.internalQueue.Where(iv => iv.Key.Channel == fuelPoint.ChannelID && iv.Key.Address == fuelPoint.PhysicalAddress).FirstOrDefault();
            if (pair.Key == null)
                return false;
            Common.FuelPoint cfp = pair.Key;
            if (cfp == null)
                return false;

            decimal dividor = (decimal)Math.Pow(10, (double)cfp.UnitPriceDecimalPlaces);

            nozzle.PricePerLiter = (int)(dividor * newPrice);
            cfp.Nozzles[nozzleId - 1].UnitPrice = newPrice;
            cfp.Nozzles[nozzleId - 1].UntiPriceInt = nozzle.PricePerLiter;
            if (!fuelPoint.Locked)
            {
                fuelPoint.Lock();
                System.Threading.Thread.Sleep(70);
            }

            if (fuelPoint.Locked)
            {
                fuelPoint.SetPrices();
                System.Threading.Thread.Sleep(1000);
                fuelPoint.Unlock();
            }
            return true;
        }

        public decimal[] GetPrices(int channel, int address)
        {
            List<decimal> prices = new List<decimal>();
            PTSLib.PTS.FuelPoint fuelPoint = this.controller.FuelPoints.Where(f => f.ChannelID == channel && f.PhysicalAddress == address).FirstOrDefault();
            if (fuelPoint == null)
                return prices.ToArray();

            var q = fuelPoint.Nozzles.OrderBy(n => n.ID);
            foreach (PTSLib.PTS.Nozzle nozzle in q)
            {
                prices.Add((decimal)nozzle.PricePerLiter / 1000);
            }

            KeyValuePair<ASFuelControl.Common.FuelPoint, ConcurrentQueue<ASFuelControl.Common.FuelPointValues>> pair = this.internalQueue.Where(iv => iv.Key.Channel == fuelPoint.ChannelID && iv.Key.Address == fuelPoint.PhysicalAddress).FirstOrDefault();
            if (pair.Key == null)
                return prices.ToArray();
            Common.FuelPoint fp = pair.Key;

            for (int i = 0; i < fp.NozzleCount; i++)
            {
                fp.Nozzles[i].UnitPrice = prices[i];
                fp.Nozzles[i].UntiPriceInt = (int)(prices[i] * (decimal)Math.Pow(10, fp.UnitPriceDecimalPlaces));
            }
                 

            return prices.ToArray();
        }

        public decimal GetNozzlePrice(int channel, int address, int nozzleId)
        {
            List<decimal> prices = new List<decimal>();
            PTSLib.PTS.FuelPoint fuelPoint = this.controller.FuelPoints.Where(f => f.ChannelID == channel && f.PhysicalAddress == address).FirstOrDefault();
            if (fuelPoint == null)
                return 0;

            KeyValuePair<ASFuelControl.Common.FuelPoint, ConcurrentQueue<ASFuelControl.Common.FuelPointValues>> pair = this.internalQueue.Where(iv => iv.Key.Channel == fuelPoint.ChannelID && iv.Key.Address == fuelPoint.PhysicalAddress).FirstOrDefault();
            if (pair.Key == null)
                return 0;
            Common.FuelPoint fp = pair.Key;
            if (fp == null)
                return 0;

            decimal dividor = (decimal)Math.Pow(10, (double)fp.UnitPriceDecimalPlaces);

            PTSLib.PTS.Nozzle nozzle = fuelPoint.Nozzles.Where(n => n.ID == nozzleId).FirstOrDefault();
            return nozzle == null ? 0 : ((decimal)nozzle.PricePerLiter) / dividor;
        }

        private ConcurrentDictionary<Guid, KeyValuePair<PTSLib.PTS.Nozzle, decimal>> totalRequests = new ConcurrentDictionary<Guid, KeyValuePair<PTSLib.PTS.Nozzle, decimal>>();
        public decimal GetNozzleTotalizer(int channel, int address, int nozzleId)
        {
            List<decimal> prices = new List<decimal>();
            PTSLib.PTS.FuelPoint fuelPoint = this.controller.FuelPoints.Where(f => f.ChannelID == channel && f.PhysicalAddress == address).FirstOrDefault();
            if (fuelPoint == null)
                return 0;
            PTSLib.PTS.Nozzle nozzle = fuelPoint.Nozzles.Where(n => n.ID == nozzleId).FirstOrDefault();

            Guid guid = Guid.NewGuid();
            if (!fuelPoint.Locked)
            {
                fuelPoint.Lock();
                System.Threading.Thread.Sleep(70);
            }
            if (fuelPoint.Locked)
            {
                totalRequests.TryAdd(guid, new KeyValuePair<PTSLib.PTS.Nozzle, decimal>(nozzle, -1));
                fuelPoint.GetTotalsByThread((byte)(nozzleId));
                while (true)
                {
                    System.Threading.Thread.Sleep(10);
                    if (fuelPoint.Nozzles[nozzleId - 1].TotalDispensedVolume > 0 && !fuelPoint.Nozzles[nozzleId - 1].QueryTotals)
                        return fuelPoint.Nozzles[nozzleId - 1].TotalDispensedVolume;
                }
            }

            //totalRequests.TryAdd(guid, new KeyValuePair<PTSLib.Nozzle, decimal>(nozzle, 0)); ;
            //fuelPoint.GetTotals((byte)(nozzleId));
            
            return 0;
        }

        public void GetNozzleTotalValues(int channel, int address, int nozzleId)
        {
            PTSLib.PTS.FuelPoint fuelPoint = this.controller.FuelPoints.Where(fp => fp.ChannelID == channel && fp.PhysicalAddress == address).FirstOrDefault();
            if (fuelPoint == null)
                return;
            //System.Threading.Thread.Sleep(200);
            fuelPoint.GetTotals((byte)nozzleId);
        }

        public decimal GetNozzleTotalVolume(int channel, int address, int nozzleId)
        {
            if (this.controller.FuelPoints == null || this.controller.FuelPoints.Count() == 0)
                return 0;
            PTSLib.PTS.FuelPoint fuelPoint = this.controller.FuelPoints.Where(fp => fp.ChannelID == channel && fp.PhysicalAddress == address).FirstOrDefault();
            if (fuelPoint == null)
                return 0;
            return fuelPoint.Nozzles[nozzleId - 1].TotalDispensedVolume;
        }

        public decimal GetNozzleTotalPrice(int channel, int address, int nozzleId)
        {
            throw (new Exception("Not Implemented"));
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

        public Common.TankValues GetTankValues(int channel, int address)
        {
            try
            {
                lock (this.internalTankQueue)
                {
                    Common.Tank ctank = this.internalTankQueue.Keys.Where(t => t.Channel == channel && t.Address == address).FirstOrDefault();
                    if (ctank == null)
                        return null;
                    Common.TankValues values = this.internalTankQueue[ctank];
                    this.internalTankQueue[ctank] = null;
                    return values;
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        #endregion

        #region private methods

        private void EnqueValues(ASFuelControl.Common.FuelPoint fuelPoint, ASFuelControl.Common.FuelPointValues values)
        {
            PTSLib.PTS.FuelPoint fPoint = this.controller.FuelPoints.Where(fp => fp.ChannelID == fuelPoint.Channel && fp.PhysicalAddress == fuelPoint.Address).FirstOrDefault();
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

        #endregion

        #region events

        void controller_Closed(object sender, EventArgs e)
        {
            
        }

        void controller_Opened(object sender, EventArgs e)
        {
            
            foreach (PTSLib.PTS.FuelPoint fp in this.controller.FuelPoints)//.Where(fp => fp.PhysicalAddress > 0))
            {
                Common.FuelPoint fuelPoint = this.fuelPoints.Where(f => f.Address == fp.PhysicalAddress && f.Channel == fp.ChannelID).FirstOrDefault();
                if (fuelPoint != null)
                {
                    for (int i = 0; i < fuelPoint.NozzleCount; i++)
                    {
                        fp.Nozzles[i].NozzleAlive = true;
                        fp.GetTotals(fp.Nozzles[i].ID);
                        System.Threading.Thread.Sleep(150);
                    }
                }
            }
        }

        void controller_Initialized(object sender, EventArgs e)
        {
            foreach (PTSLib.PTS.FuelPoint fp in this.controller.FuelPoints)//.Where(fp => fp.PhysicalAddress > 0))
            {
                fp.StatusChanged += new EventHandler<PTSLib.PTS.StatusChangedEventArgs>(fp_StatusChanged);
                fp.DispensedValuesChanged += new EventHandler(fp_DispensedValuesChanged);
                fp.IsActive = true;
                fp.OrderMode = PTSLib.PTS.OrderModes.Manual;
                fp.GetPrices();

                
                
                fp.TotalsUpdated -= new EventHandler<PTSLib.PTS.TotalsEventArgs>(fp_TotalsUpdated);
                fp.TotalsUpdated += new EventHandler<PTSLib.PTS.TotalsEventArgs>(fp_TotalsUpdated);
            }
            foreach (PTSLib.PTS.FuelPoint fp in controller.Configuration.FuelPoints.Where(fp => fp.PhysicalAddress > 0))
            {
                fp.OrderMode = PTSLib.PTS.OrderModes.Manual;
            }
            foreach (PTSLib.PTS.ATG atg in controller.Configuration.ATGs.Where(a => a.PhysicalAddress > 0))
            {
                atg.DataUpdated += new EventHandler(atg_DataUpdated);
                atg.IsActive = true;
            }

            //controller.SetFuelPointConfiguration();
        }

        void atg_DataUpdated(object sender, EventArgs e)
        {
            PTSLib.PTS.ATG atg = sender as PTSLib.PTS.ATG;
            
            ASFuelControl.Common.TankValues tankValues = new ASFuelControl.Common.TankValues();
            if (atg.TemperaturePresent)
            {
                tankValues.CurrentTemperatur = (decimal)atg.Temperature;
                atg.LastMeasurmentTime = DateTime.Now;
            }
            if (atg.WaterHeightPresent)
            {
                tankValues.WaterHeight = (decimal)atg.WaterHeight;
                atg.LastMeasurmentTime = DateTime.Now;
            }
            if (atg.ProductHeightPresent)
            {
                tankValues.FuelHeight = (decimal)atg.ProductHeight;
                tankValues.LastMeasureTime = DateTime.Now;
                atg.LastMeasurmentTime = DateTime.Now;
            }
            
            ASFuelControl.Common.Tank tank = this.internalTankQueue.Keys.Where(t => (t.Address == atg.PhysicalAddress && t.Channel == atg.ChannelID && t.Address <=100) ||
                (t.Channel == atg.ID && t.Address > 100)).FirstOrDefault();
            if (tank == null)
                return;
            lock (this.internalTankQueue)
            {
                tankValues.Status = Common.Enumerators.TankStatusEnum.Idle;
                this.internalTankQueue[tank] = tankValues;
                //Console.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff"));
            }
        }

        Dictionary<Common.FuelPoint, Dictionary<int, decimal>> totalsList = new Dictionary<Common.FuelPoint, Dictionary<int, decimal>>();
        Dictionary<Common.FuelPoint, Dictionary<int, int>> totalsCount = new Dictionary<Common.FuelPoint, Dictionary<int, int>>();

        void fp_TotalsUpdated(object sender, PTSLib.PTS.TotalsEventArgs e)
        {
            PTSLib.PTS.FuelPoint fuelPoint = e.Nozzle.FuelPoint;
            KeyValuePair<ASFuelControl.Common.FuelPoint, ConcurrentQueue<ASFuelControl.Common.FuelPointValues>> pair = this.internalQueue.Where(iv => iv.Key.Channel == fuelPoint.ChannelID && iv.Key.Address == fuelPoint.PhysicalAddress).FirstOrDefault();
            if (pair.Key == null)
                return;
            Common.FuelPoint fp = pair.Key;
            if (fp == null)
                return;
            fp.Initialized = true;
            Common.Nozzle nz = fp.Nozzles.Where(n => n.Index == e.NozzleID).FirstOrDefault();
            if (this.TotalsRecieved != null)
            {
                this.TotalsRecieved(this, new Common.TotalsEventArgs(fp, (int)e.NozzleID, (decimal)nz.TotalVolume, (decimal)0));
            }
            fp.ActiveNozzleIndex = (int)fuelPoint.ActiveNozzleID - 1;

            Common.Sales.SaleData sale = nz.saleHandler.AddTotalizer((decimal)e.Nozzle.TotalDispensedVolume, 
                (decimal)e.Nozzle.PricePerLiter / (decimal)Math.Pow(10, fp.UnitPriceDecimalPlaces), 
                (decimal)e.Nozzle.FuelPoint.DispensedVolume / (decimal)Math.Pow(10, fp.DecimalPlaces), 
                (decimal)e.Nozzle.FuelPoint.DispensedAmount / (decimal)Math.Pow(10, fp.DecimalPlaces));

            fp.Status = fp.DispenserStatus;
            Common.FuelPointValues values = new Common.FuelPointValues();
            values.Status = fp.DispenserStatus;
            values.CurrentPriceTotal = sale.TotalPrice;
            values.CurrentSalePrice = sale.UnitPrice;
            values.CurrentVolume = sale.TotalVolume;

            this.EnqueValues(fp, values);
        }

        void fp_DispensedValuesChanged(object sender, EventArgs e)
        {
            PTSLib.PTS.FuelPoint fp = sender as PTSLib.PTS.FuelPoint;
            KeyValuePair<ASFuelControl.Common.FuelPoint, ConcurrentQueue<ASFuelControl.Common.FuelPointValues>> pair = this.internalQueue.Where(iv => iv.Key.Channel == fp.ChannelID && iv.Key.Address == fp.PhysicalAddress).FirstOrDefault();
            if (pair.Key == null)
                return;
            Common.FuelPoint fuelPoint = pair.Key;

            if (fuelPoint == null)
                return;

            decimal dividor = (decimal)Math.Pow(10, (double)fuelPoint.DecimalPlaces);
            fuelPoint.ActiveNozzleIndex = (int)fp.ActiveNozzleID - 1;
            if (fuelPoint.ActiveNozzle != null && fuelPoint.ActiveNozzle.CurrentSale != null)
            {
                fuelPoint.ActiveNozzle.CurrentSale.UnitPrice = fuelPoint.ActiveNozzle.UnitPrice;
                fuelPoint.ActiveNozzle.CurrentSale.SaleEndTime = DateTime.Now;
            
            }

            Common.FuelPointValues values = new Common.FuelPointValues();
            values.CurrentVolume = fp.DispensedVolume / (decimal)Math.Pow(10, fuelPoint.DecimalPlaces);
            values.CurrentPriceTotal = (decimal)fp.DispensedAmount  * (100 / (decimal)Math.Pow(10, fuelPoint.UnitPriceDecimalPlaces)) / (decimal)Math.Pow(10, fuelPoint.DecimalPlaces);
            values.ActiveNozzle = (int)fp.ActiveNozzleID -1;
            values.TotalVolumes = fuelPoint.Nozzles.Select(n => n.TotalVolume).ToArray();
            values.Status = (ASFuelControl.Common.Enumerators.FuelPointStatusEnum)(int)fuelPoint.Status;
            fuelPoint.DispenserStatus = values.Status;
            fuelPoint.Status = values.Status;

            this.EnqueValues(fuelPoint, values);
        }

        void fp_StatusChanged(object sender, PTSLib.PTS.StatusChangedEventArgs e)
        {
            PTSLib.PTS.FuelPoint fp = sender as PTSLib.PTS.FuelPoint;
            KeyValuePair<ASFuelControl.Common.FuelPoint, ConcurrentQueue<ASFuelControl.Common.FuelPointValues>> pair = this.internalQueue.Where(iv => iv.Key.Channel == fp.ChannelID && iv.Key.Address == fp.PhysicalAddress).FirstOrDefault();
            if (pair.Key == null)
                return;
            Common.FuelPoint fuelPoint = pair.Key;
            if (fuelPoint == null)
                return;


            decimal dividor = (decimal)Math.Pow(10, (double)fuelPoint.DecimalPlaces);

            ASFuelControl.Common.FuelPointValues values = new ASFuelControl.Common.FuelPointValues();
            fuelPoint.ActiveNozzleIndex = (int)fp.ActiveNozzleID - 1;
            values.ActiveNozzle = fuelPoint.ActiveNozzleIndex;
            if (fuelPoint.Status != Common.Enumerators.FuelPointStatusEnum.Offline)
            {
                values.CurrentPriceTotal = fuelPoint.DispensedAmount;
                values.CurrentVolume = fuelPoint.DispensedVolume;
            }
            if (fuelPoint.ActiveNozzle != null)
            {
                values.ActiveNozzle = fuelPoint.ActiveNozzleIndex;
            }
            values.Status = (Common.Enumerators.FuelPointStatusEnum)((int)fp.Status);
            values.TotalVolumes = fuelPoint.Nozzles.Select(n => n.TotalVolume).ToArray();


            Common.Nozzle salesNozzle = fuelPoint.ActiveNozzle;
            if (salesNozzle == null)
                salesNozzle = fuelPoint.LastActiveNozzle;
            if (fp.Status == PTSLib.PTS.FuelPointStatus.IDLE)
            {
                if (fuelPoint.ActiveNozzle != null)
                {
                    fuelPoint.ActiveNozzle.QueryTotals = true;
                    fp.GetTotalsByThread((byte)fuelPoint.ActiveNozzle.Index);
                }
                else if (fuelPoint.LastActiveNozzle != null)
                {
                    fuelPoint.LastActiveNozzle.QueryTotals = true;
                    fp.GetTotalsByThread((byte)fuelPoint.LastActiveNozzle.Index);
                }
            }
            else if (fp.Status == PTSLib.PTS.FuelPointStatus.OFFLINE)
            {
                foreach (Common.Nozzle n in fuelPoint.Nozzles)
                    n.CurrentSale = null;
                    
            }
            fuelPoint.DispenserStatus = values.Status;
            fuelPoint.Status = values.Status;
            this.EnqueValues(fuelPoint, values);
        }

        #endregion
    }
}
