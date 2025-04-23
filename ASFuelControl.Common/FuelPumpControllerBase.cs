using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;

namespace ASFuelControl.Common
{
    /// <summary>
    /// Base functionality for handling pump and tank protocols
    /// </summary>
    public class FuelPumpControllerBase : IController
    {
        #region public event definitions

        public event EventHandler<TotalsEventArgs> TotalsRecieved;
        public event EventHandler<FuelPointValuesArgs> ValuesRecieved;
        public event EventHandler<SaleEventArgs> SaleRecieved;
        #endregion

        #region private variables

        private Enumerators.ControllerTypeEnum controllerType;
        private bool isConnected = false;
        protected List<FuelPoint> fuelPoints = new List<FuelPoint>();
        protected ConcurrentDictionary<ASFuelControl.Common.FuelPoint, ValueHolder> internalQueue = new ConcurrentDictionary<ASFuelControl.Common.FuelPoint, ValueHolder>();
        private IFuelProtocol controller = null;
        private List<Guid> salesCompleted = new List<Guid>();
        private bool euromatEnabled = false;
        private EuromatClient euromat = new EuromatClient();
        private string euromatIp;
        private int euromatPort;
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
                this.controller.SaleRecieved += new EventHandler<SaleEventArgs>(controller_SaleRecieved);
                this.controller.DispenserStatusChanged += new EventHandler<FuelPointValuesArgs>(controller_DispenserStatusChanged);
                this.controller.DispenserOffline += new EventHandler(controller_DispenserOffline);
            }
            get { return this.controller; }
        }

        public bool EuromatEnabled 
        {
            set 
            { 
                this.euromatEnabled = value;
                if (value)
                {
                    this.euromat.ServerIp = this.EuromatIp;
                    this.euromat.ServrerPort = this.EuromatPort;
                }

            }
            get { return this.euromatEnabled; } 
        }
        
        public string EuromatIp 
        {
            set 
            { 
                this.euromatIp = value;
                this.euromat.ServerIp = this.EuromatIp;
            }
            get 
            {
                return this.euromatIp;
            }
        }
        public int EuromatPort 
        {
            set
            { 
                this.euromatPort = value;
                this.euromat.ServrerPort = this.euromatPort;
            }
            get 
            {
                return this.euromatPort;
            }
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
            this.internalQueue[fuelPoint].AddValues(values);
            //if (this.internalQueue[fuelPoint].Count == 0)
            //    this.internalQueue[fuelPoint].Enqueue(values);
            //else
            //{
            //    this.internalQueue[fuelPoint].Enqueue(values);
                
            //    //ASFuelControl.Common.FuelPointValues oldValues = this.internalQueue[fuelPoint].Last();
            //    //if (oldValues.Status != values.Status)
            //    //    this.internalQueue[fuelPoint].Enqueue(values);
            //    //else
            //    //{
            //    //    ASFuelControl.Common.FuelPointValues vals;
            //    //    this.internalQueue[fuelPoint].TryDequeue(out vals);
            //    //    this.internalQueue[fuelPoint].Enqueue(values);
            //    //}
            //}
        }

        #endregion

        public FuelPumpControllerBase()
        {
            //euromat.EuromatAuthorize += new EventHandler<EuromatAuthorizeEventArgs>(euromat_EuromatAuthorize);
        }

        #region public fuelpoint methods
        public virtual DebugValues DebugStatusDialog(FuelPoint fp)
        {
            throw new NotImplementedException();
        }
        public virtual void AddFuelPoint(int channel, int address, int nozzleCount)
        {
            this.AddFuelPoint(channel, address, nozzleCount, 2, 2, 3, 2);
        }

        public virtual void AddFuelPoint(int channel, int address, int nozzleCount, int amountDecimalPlaces, int volumeDecimalPlaces, int untiPriceDecimalPlaces, int totalDecimalPlaces)
        {
            FuelPoint fpi = new FuelPoint();
            fpi.NozzleCount = nozzleCount;
            this.fuelPoints.Add(fpi);
            fpi.Address = address;
            fpi.Channel = channel;
            fpi.UnitPriceDecimalPlaces = untiPriceDecimalPlaces;
            fpi.TotalDecimalPlaces = totalDecimalPlaces;

            fpi.AmountDecimalPlaces = amountDecimalPlaces;
            fpi.VolumeDecimalPlaces = volumeDecimalPlaces;
            this.internalQueue.TryAdd(fpi, new ValueHolder() { FuelPump = fpi });

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

            if (!this.euromatEnabled || fp.EuromatNumber <= 0)
            {
                if (!fp.OTPEnabled)
                {
                    fp.PresetVolume = 999999;
                    fp.PresetAmount = 999999;
                    fp.QueryAuthorize = true;
                }
                else
                {
                    if(fp.PresetRaised)
                    {
                        fp.QueryAuthorize = true;
                    }
                }
            }
            else
            {
                //this.euromat.PumpNotification(fp.EuromatNumber, fp.ActiveNozzleIndex + 1);
            }
            return true;
        }

        public virtual void SetEuromatDispenserNumber(int channel, int address, int number, string ipAddress, int port)
        {
            FuelPoint fp = this.GetFulePoint(channel, address);
            if (fp == null)
                return;
            fp.IsEuromatEnabled = true;
            fp.EuromatNumber = number;
            fp.EuromatIP = ipAddress;
            fp.EuromatPort = port;
        }

        //void euromat_EuromatAuthorize(object sender, EuromatAuthorizeEventArgs e)
        //{
        //    FuelPoint fp = this.fuelPoints.Where(f => f.EuromatNumber == e.Transaction.PumpNumber).FirstOrDefault();
        //    if (fp == null)
        //        return;
        //    if (fp.DispenserStatus != Enumerators.FuelPointStatusEnum.Nozzle)
        //        return;
        //    lock (fp)
        //    {
        //        fp.QueryAuthorize = true;
        //        if (e.Transaction.RateType == RateTypeEnum.Cash)
        //        {
        //            fp.PresetVolume = e.Transaction.VolumeCredit;
        //            fp.PresetAmount = e.Transaction.AmountCredit;
        //        }
        //        else
        //        {
        //            fp.PresetVolume = e.Transaction.VolumeCash;
        //            fp.PresetAmount = e.Transaction.AmountCash;
        //        }
        //    }
        //}

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

        public virtual bool SetPresetAmount(int channel, int address, decimal amount)
        {
            FuelPoint fp = this.GetFulePoint(channel, address);
            if (fp == null)
                return false;
            
            if(amount < 0)
                fp.PresetVolume = -1;
            else
                fp.PresetVolume = 0;
            fp.PresetAmount = amount;
            
            return true;
        }

        public virtual bool SetPresetVolume(int channel, int address, decimal volume)
        {
            FuelPoint fp = this.GetFulePoint(channel, address);
            if (fp == null)
                return false;
            
            fp.PresetVolume = volume;
            fp.PresetAmount = 0;
            return true;
        }

        public void SetOtpEnabled(int channel, int address, bool flag)
        {
            FuelPoint fp = this.GetFulePoint(channel, address);
            if (fp == null)
                return;
            if (fp.ActiveNozzle == null)
                return;
            fp.OTPEnabled = flag;
        }

        public virtual bool HaltDispenser(int channel, int address)
        {
            try
            {
                FuelPoint fp = this.GetFulePoint(channel, address);
                fp.QueryHalt = true;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public virtual bool ResumeDispenser(int channel, int address)
        {
            try
            {
                FuelPoint fp = this.GetFulePoint(channel, address);
                if (!fp.Halted)
                    return true;
                fp.QueryResume = true;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public virtual bool SetDispenserStatus(int channel, int address, Enumerators.FuelPointStatusEnum status)
        {
            FuelPoint fp = this.GetFulePoint(channel, address);
            if (fp == null)
                return false;
            fp.DispenserStatus = status;
            fp.Status = status;
            if (status == Enumerators.FuelPointStatusEnum.Offline)
            {
                fp.QueryTotals = true;
                foreach (Nozzle nz in fp.Nozzles)
                    nz.QueryTotals = true;
                fp.Initialized = false;
            }
            return true;
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
                this.SetNozzlePrice(channel, address, fp.Nozzles[i].Index,price[i]);
                
                System.Threading.Thread.Sleep(100);
            }
            return true;
        }

        public virtual bool SetNozzleIndex(int channel, int address, int nozzeId, int nozzleIndex)
        {
            FuelPoint fp = this.GetFulePoint(channel, address);
            if(fp == null)
                return false;
            fp.Nozzles[nozzeId - 1].NozzleIndex = nozzleIndex;
            return true;
        }

        public virtual bool SetNozzleSocket(int channel, int address, int index, int nozzleSocket)
        {
            FuelPoint fp = this.GetFulePoint(channel, address);
            if (fp == null)
                return false;
            fp.Nozzles[index - 1].NozzleSocket = nozzleSocket;
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
            
            KeyValuePair<ASFuelControl.Common.FuelPoint, ValueHolder> pair = this.internalQueue.Where(iv => iv.Key.Channel == channel && iv.Key.Address == address).FirstOrDefault();
            if (pair.Key == null)
                return null;
            Common.FuelPoint fuelPoint = pair.Key;
            var values = this.internalQueue[fuelPoint].GetValues();
            return values;
            //if (this.internalQueue[fuelPoint].Count > 0)
            //{
            //    ASFuelControl.Common.FuelPointValues vals;
            //    this.internalQueue[fuelPoint].TryDequeue(out vals);
            //    lastValuesSent = DateTime.Now;
            //    return vals;
            //}
            //if (lastValuesSent != DateTime.MinValue &&  DateTime.Now.Subtract(lastValuesSent).TotalSeconds > 10 )
            //{
            //    fuelPoint.Status = Enumerators.FuelPointStatusEnum.Offline;
            //    fuelPoint.DispenserStatus = Enumerators.FuelPointStatusEnum.Offline;

            //    ASFuelControl.Common.FuelPointValues vals = new FuelPointValues();
            //    vals.ActiveNozzle = -1;
            //    vals.TotalVolumes = fuelPoint.Nozzles.Select(n => n.TotalVolume).ToArray();
            //    vals.Status = (ASFuelControl.Common.Enumerators.FuelPointStatusEnum)(int)fuelPoint.Status;

            //    lastValuesSent = DateTime.Now;
            //    return vals;
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
            if (fp == null)
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
            if(fp == null)
            {
                while (true)
                {
                    try
                    {
                        System.IO.File.AppendAllText(System.Environment.CurrentDirectory + "\\InitializeERROR.txt", "\nGetNozzleTotalizer Failed: fp == null");
                        return -1;
                    }
                    catch
                    {
                        System.Threading.Thread.Sleep(100);
                    }
                }
            }
            if(fp.NozzleCount < nozzleId)
            {
                while (true)
                {
                    try
                    {
                        System.IO.File.AppendAllText(System.Environment.CurrentDirectory + "\\InitializeERROR.txt", "\nGetNozzleTotalizer Failed: fp.NozzleCount < nozzleId");
                        return -1;
                    }
                    catch
                    {
                        System.Threading.Thread.Sleep(100);
                    }
                }
            }
            if (!fp.Initialized)
                return -1;
            return fp.Nozzles[nozzleId - 1].TotalVolume;
        }

        //public virtual Sales.SaleData GetSale(int channel, int address, int nozzleId)
        //{
        //    FuelPoint fp  = this.GetFulePoint(channel, address);
        //    if (fp == null)
        //        return null;
        //    Common.Nozzle nz = fp.Nozzles.Where(n => n.Index == nozzleId).FirstOrDefault();
        //    if (nz == null)
        //        return null;

        //    Common.Sales.SaleData lastSale = nz.GetLastSale();
        //    if (lastSale == null)
        //        return null;
        //    lastSale.SaleProcessed = true;
        //    return lastSale;

        //    //Common.Sales.SaleData sale = nz.GetLastSale();
        //    //if (sale == null)
        //    //    return null;
        //    //if(sale.SaleProcessed)
        //    //    return null;
        //    //if (this.salesCompleted.Contains(sale.SalesId))
        //    //{
        //    //    sale.SaleProcessed = true;
        //    //    this.salesCompleted.Remove(sale.SalesId);
        //    //    return null;
        //    //}
        //    //sale.SaleProcessed = true;
        //    //this.salesCompleted.Add(sale.SalesId);
        //    //if (this.salesCompleted.Count > 500)
        //    //    this.salesCompleted.RemoveAt(0);
            
        //    //return sale;

        //    //Common.Sales.SaleData sale = nz.CurrentSale;
        //    //if (sale != null && sale.SaleCompleted)
        //    //{
        //    //    nz.CurrentSale = null;
        //    //    return sale;
        //    //}
        //    //return null;
        //}

        public virtual string GetEuromatParameters(int channel, int address)
        {
            FuelPoint fp = this.GetFulePoint(channel, address);
            if (fp == null)
                return "NOT FOUND";
            string parameters = fp.EuromatTransactionParameters;
            fp.EuromatTransactionParameters = "";
            return parameters;
        }

        public Euromat.Transaction GetEuromatTransaction(int channel, int address)
        {
            FuelPoint fp = this.GetFulePoint(channel, address);
            if (fp == null)
                return null;
            return fp.EuromatTransaction;
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

        private void CreateValuesForTotals(Common.Nozzle nz)
        {
            if (nz == null)
                return;
            FuelPoint fp = nz.ParentFuelPoint;
            Common.FuelPointValues values = new Common.FuelPointValues();

            fp.Status = fp.DispenserStatus;
            values.Status = fp.Status;
            values.ActiveNozzle = nz.Index;
            values.TotalVolumes = fp.Nozzles.Select(n => n.TotalVolume).ToArray();
            if (System.IO.File.Exists("TransactionCompleted.log"))
            {
                System.IO.File.AppendAllText("TransactionCompleted.log", "Values Enquee\r\n");
            }
            this.EnqueValues(fp, values);
        }

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
            nz.QueryTotals = false;
            int totalIndex = nz.GetTotalsIndex();
            if(totalIndex < 3)
            {
                nz.Totals.Add(nz.TotalVolume);
                nz.SetExtendedProperty("TotalIndex", totalIndex + 1);
                if (!nz.HasValidTotals())
                {
                    nz.QueryTotals = true;
                    return;
                }
            }
            decimal totals = nz.GetTotalsFromList();
            nz.Totals.Clear();
            nz.SetExtendedProperty("TotalIndex", 0);
            nz.TotalVolume = totals;
            //if (nz.LastTotalVolume >= nz.TotalVolume || nz.LastTotalVolume < 0)
            //{
            //    if (e.CurrentFuelPoint.DispensedVolume > 0 && totalIndex < 5)
            //    {
            //        nz.SetExtendedProperty("TotalIndex", totalIndex + 1);
            //        nz.QueryTotals = true;
            //        return;
            //    }
            //    if(e.CurrentFuelPoint.DispensedVolume == 0)
            //        return;
            //}
            if (e.CurrentFuelPoint.DispensedVolume == 0 && nz.TransactionVolume == 0)
            {
                CreateValuesForTotals(nz);
                return;
            }
            //if (nz.GetExtendedProperty("LastTotalVolume") != null && (decimal)nz.GetExtendedProperty("LastTotalVolume") == nz.TotalVolume)
            //{
            //    if (e.CurrentFuelPoint.DispensedVolume > 0 && totalIndex < 5)
            //    {
            //        nz.SetExtendedProperty("TotalIndex", totalIndex + 1);
            //        nz.QueryTotals = true;
            //        return;
            //    }
            //    if (e.CurrentFuelPoint.DispensedVolume == 0)
            //    {
            //        if (System.IO.File.Exists("TransactionCompleted.log"))
            //        {
            //            System.IO.File.AppendAllText("TransactionCompleted.log", "DispensedVolume is 0\r\n");
            //        }
            //        return;
            //    }
            //}
            if(nz.TransactionVolume == 0 && e.CurrentFuelPoint.DispensedVolume > 0)
            {

            }
            else if(nz.TransactionVolume > 0 && e.CurrentFuelPoint.DispensedVolume == 0)
            {

            }
            bool work = (bool)nz.ParentFuelPoint.GetExtendedProperty("Work", false);
            nz.ParentFuelPoint.SetExtendedProperty("Work", false);

            nz.SetExtendedProperty("LastTotalVolume", nz.TotalVolume);
            nz.SetExtendedProperty("TotalIndex", 0);

            if (nz.SuspendSale)
            {
                nz.SuspendSale = false;
                if (fp.EuromatNumber > 0)
                {
                    this.euromat.TransactionCompleted(fp, fp.EuromatNumber, nz.Index, nz.UnitPrice, 0, 0);
                }
                //return;
            }
            if (!fp.NeedsInvoice)
            {
                if (System.IO.File.Exists("TransactionCompleted.log"))
                {
                    System.IO.File.AppendAllText("TransactionCompleted.log", "fp.NeedsInvoice is FALSE\r\n");
                }
                CreateValuesForTotals(nz);
                return;
            }
            fp.NeedsInvoice = false;
            Common.FuelPointValues values = new Common.FuelPointValues();
            if (fp.EuromatNumber > 0)
            {
                //fp.EuromatTransactionParameters = "";
                this.euromat.TransactionCompleted(fp, fp.EuromatNumber, nz.Index, nz.UnitPrice, e.CurrentFuelPoint.DispensedAmount, e.CurrentFuelPoint.DispensedVolume);
                values.EuromatParameters = fp.EuromatTransactionParameters;
            }
            Common.Sales.SaleData sale = new Sales.SaleData();//nz.saleHandler.AddTotalizer(e.TotalVolume, nz.UnitPrice, e.CurrentFuelPoint.DispensedVolume, e.CurrentFuelPoint.DispensedAmount);
            sale.DisplayPrice = work ? e.CurrentFuelPoint.DispensedAmount : 0;
            sale.DisplayVolume = work ? e.CurrentFuelPoint.DispensedVolume : 0;
            sale.TotalizerStart = nz.LastTotalVolume;
            sale.TotalizerEnd = nz.TotalVolume;
            sale.TotalPrice = work ? e.CurrentFuelPoint.DispensedAmount : 0;
            sale.TotalVolume = work ? e.CurrentFuelPoint.DispensedVolume : 0;
            sale.UnitPrice = nz.UnitPrice;

            decimal totalVolume = (decimal)((double)(sale.TotalizerEnd - sale.TotalizerStart) / Math.Pow(10, e.CurrentFuelPoint.TotalDecimalPlaces));

            if (totalVolume > sale.TotalVolume + (decimal)1 || totalVolume < sale.TotalVolume - (decimal)1)
            {
                sale.InvalidSale = true;
            }

            fp.Status = fp.DispenserStatus;
            values.Sale = sale;
            values.Status = fp.Status;
            values.CurrentPriceTotal = sale.TotalPrice;
            values.CurrentSalePrice = sale.UnitPrice;
            values.CurrentVolume = sale.TotalVolume;
            values.HasTotalMisfunction = nz.TotalMisfunction;
            values.ActiveNozzle = nz.Index;
            if(System.IO.File.Exists("TransactionCompleted.log"))
            {
                System.IO.File.AppendAllText("TransactionCompleted.log", "Values Enquee\r\n");
            }
            this.EnqueValues(fp, values);

            //nz.ParentFuelPoint.DispensedAmount = 0;
            //nz.ParentFuelPoint.DispensedVolume = 0;
        }

        protected virtual void OnSaleRecieved(SaleEventArgs e)
        {
            Common.FuelPoint fp = e.CurrentFuelPoint;
            if (fp == null)
                return;

            Common.Nozzle nz = fp.Nozzles.Where(n => n.Index == e.NozzleId).FirstOrDefault();
            
            bool work = (bool)nz.ParentFuelPoint.GetExtendedProperty("Work", false);
            nz.ParentFuelPoint.SetExtendedProperty("Work", false);
            nz.TotalVolume = e.TotalVolume;
            if (nz.SuspendSale)
            {
                nz.SuspendSale = false;
                if (fp.EuromatNumber > 0)
                {
                    this.euromat.TransactionCompleted(fp, fp.EuromatNumber, nz.Index, nz.UnitPrice, 0, 0);
                }
                //return;
            }
            if (!fp.NeedsInvoice)
            {
                if (System.IO.File.Exists("TransactionCompleted.log"))
                {
                    System.IO.File.AppendAllText("TransactionCompleted.log", "fp.NeedsInvoice is FALSE\r\n");
                }
                CreateValuesForTotals(nz);
                return;
            }
            fp.NeedsInvoice = false;
            Common.FuelPointValues values = new Common.FuelPointValues();
            if (fp.EuromatNumber > 0)
            {
                //fp.EuromatTransactionParameters = "";
                this.euromat.TransactionCompleted(fp, fp.EuromatNumber, nz.Index, nz.UnitPrice, e.CurrentFuelPoint.DispensedAmount, e.CurrentFuelPoint.DispensedVolume);
                values.EuromatParameters = fp.EuromatTransactionParameters;
            }
            Common.Sales.SaleData sale = new Sales.SaleData();//nz.saleHandler.AddTotalizer(e.TotalVolume, nz.UnitPrice, e.CurrentFuelPoint.DispensedVolume, e.CurrentFuelPoint.DispensedAmount);
            sale.DisplayPrice = e.Amount;
            sale.DisplayVolume = e.Volume;
            sale.TotalizerStart = nz.LastTotalVolume;
            sale.TotalizerEnd = e.TotalVolume;
            sale.TotalPrice = e.Amount;
            sale.TotalVolume = e.Volume;
            sale.UnitPrice = nz.UnitPrice;
            fp.Status = fp.DispenserStatus;// Enumerators.FuelPointStatusEnum.Idle;
            values.Sale = sale;
            values.Status = fp.Status;
            values.CurrentPriceTotal = sale.TotalPrice;
            values.CurrentSalePrice = sale.UnitPrice;
            values.CurrentVolume = sale.TotalVolume;
            values.HasTotalMisfunction = nz.TotalMisfunction;
            values.ActiveNozzle = nz.Index;
            if (System.IO.File.Exists("TransactionCompleted.log"))
            {
                System.IO.File.AppendAllText("TransactionCompleted.log", "Values Enquee\r\n");
            }
            this.EnqueValues(fp, values);
        }

        protected virtual void OnStatusChanged(FuelPointValuesArgs e)
        {
            Common.FuelPoint fuelPoint = e.CurrentFuelPoint;
            if (fuelPoint == null)
                return;
            
            
            decimal dividor = (decimal)Math.Pow(10, (double)fuelPoint.AmountDecimalPlaces);

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

            //if (fuelPoint.DispenserStatus == Enumerators.FuelPointStatusEnum.Nozzle)
            //{
            //    fuelPoint.DispensedAmount = 0;
            //    fuelPoint.DispensedVolume = 0;
            //}

            values.Status = fuelPoint.Status;
            //values.TotalVolumes = fuelPoint.Nozzles.Select(n => n.TotalVolume).ToArray();

            //if(fuelPoint.Status == Enumerators.FuelPointStatusEnum.Work)
            //{
            //    fuelPoint.SetExtendedProperty("Work", true);
            //}

            Common.Nozzle salesNozzle = fuelPoint.ActiveNozzle;
            if (salesNozzle == null)
                salesNozzle = fuelPoint.LastActiveNozzle;

            if (salesNozzle != null)
                values.HasTotalMisfunction = salesNozzle.TotalMisfunction;

            if (fuelPoint.DispenserStatus == Enumerators.FuelPointStatusEnum.Idle)
            {
                if( (bool)fuelPoint.GetExtendedProperty("Work", false))
                {
                    if(fuelPoint.ActiveNozzle != null)
                        fuelPoint.ActiveNozzle.QueryTotals = true;
                    else if(fuelPoint.LastActiveNozzle != null)
                        fuelPoint.LastActiveNozzle.QueryTotals = true;
                }

                if (fuelPoint.EuromatNumber > 0 && fuelPoint.DispenserLastStatus != Enumerators.FuelPointStatusEnum.Offline)
                {
                    //this.euromat.NozzleClosed(fuelPoint.EuromatNumber);
                }

                if (fuelPoint.ActiveNozzle != null)
                {
                    fuelPoint.ActiveNozzle.QueryTotals = true;
                }
                else if (fuelPoint.LastStatus == Enumerators.FuelPointStatusEnum.Work || fuelPoint.LastStatus == Enumerators.FuelPointStatusEnum.TransactionCompleted ||
                    fuelPoint.LastStatus == Enumerators.FuelPointStatusEnum.TransactionStopped ||
                    fuelPoint.LastStatus == Enumerators.FuelPointStatusEnum.SalingCompleted)
                {
                    if (fuelPoint.LastActiveNozzle != null)
                        fuelPoint.LastActiveNozzle.QueryTotals = true;
                }
            }
            else if (fuelPoint.Status == Enumerators.FuelPointStatusEnum.Offline || fuelPoint.Status == Enumerators.FuelPointStatusEnum.Close)
            {
                fuelPoint.DispensedVolume = 0;
                fuelPoint.DispensedAmount = 0;
                
                foreach (Common.Nozzle n in fuelPoint.Nozzles)
                {
                    n.SuspendSale = true;
                    ///n.CurrentSale = null;
                    //if(n.saleHandler != null)
                    //    n.saleHandler.ClearTotalizers();
                }
            }

            //if (fuelPoint.Status == Enumerators.FuelPointStatusEnum.Offline || fuelPoint.Status == Enumerators.FuelPointStatusEnum.Close)
            //{
            //    foreach (Common.Nozzle n in fuelPoint.Nozzles)
            //    {
            //        n.SuspendSale = true;
            //        //n.saleHandler = new Sales.SaleHandler();
            //        //if(n.TotalVolume > 0)
            //        //    n.saleHandler.AddDummyTotalizer(n.TotalVolume);
            //        //n.saleHandler.ParentNozzle = n;
            //        //if (n.saleHandler != null)
            //        //    n.saleHandler.ClearTotalizers();
            //    }
            //}
            //if (fuelPoint.Status == Enumerators.FuelPointStatusEnum.Offline)
            //{
            //    fuelPoint.Initialized = false;
            //}

            if (fuelPoint.Status == Enumerators.FuelPointStatusEnum.Nozzle || fuelPoint.Status == Enumerators.FuelPointStatusEnum.Work)
            {
                foreach (Common.Nozzle n in fuelPoint.Nozzles)
                    n.SuspendSale = false;
            }

            if (fuelPoint.Status == Enumerators.FuelPointStatusEnum.Idle && fuelPoint.LastStatus == Enumerators.FuelPointStatusEnum.Offline && fuelPoint.Initialized)
            {
                foreach (Common.Nozzle n in fuelPoint.Nozzles)
                {
                    n.QueryTotals = true;
                }
            }

            fuelPoint.Status = values.Status;
            this.EnqueValues(fuelPoint, values);
        }

        void controller_TotalsRecieved(object sender, TotalsEventArgs e)
        {
            this.OnTransactionCompleted(e);
        }

        private void controller_SaleRecieved(object sender, SaleEventArgs e)
        {
            OnSaleRecieved(e);
        }

        void controller_DataChanged(object sender, FuelPointValuesArgs e)
        {
            Common.FuelPoint fuelPoint = e.CurrentFuelPoint;

            if (fuelPoint == null)
                return;

            decimal dividor = (decimal)Math.Pow(10, (double)fuelPoint.AmountDecimalPlaces);

            //if (fuelPoint.ActiveNozzle != null && fuelPoint.ActiveNozzle.CurrentSale != null)
            //{
            //    fuelPoint.ActiveNozzle.CurrentSale.UnitPrice = e.Values.CurrentSalePrice;
            //    fuelPoint.ActiveNozzle.CurrentSale.SaleEndTime = DateTime.Now;
            //    e.Values.HasTotalMisfunction = fuelPoint.ActiveNozzle.TotalMisfunction;
            //}
            e.Values.ActiveNozzle = e.CurrentNozzleId - 1;
            //e.Values.TotalVolumes = fuelPoint.Nozzles.Select(n => n.TotalVolume).ToArray();
            e.Values.Status = (ASFuelControl.Common.Enumerators.FuelPointStatusEnum)(int)fuelPoint.Status;
            fuelPoint.Status = e.Values.Status;
            //if(fuelPoint.Status == Enumerators.FuelPointStatusEnum.Work)
            //    fuelPoint.SetExtendedProperty("Work", true);

            this.EnqueValues(fuelPoint, e.Values);
        }

        void controller_DispenserStatusChanged(object sender, FuelPointValuesArgs e)
        {
            this.OnStatusChanged(e);

        }

        void controller_DispenserOffline(object sender, EventArgs e)
        {
            FuelPoint fp = sender as FuelPoint;
            if (fp == null)
                return;

            Common.FuelPointValues values = new Common.FuelPointValues();
            fp.Status = Enumerators.FuelPointStatusEnum.Offline;
            
            values.Status = fp.Status;
            values.CurrentPriceTotal = 0;
            values.CurrentSalePrice = 0;
            values.CurrentVolume = 0;
            values.HasTotalMisfunction = false;
            values.ActiveNozzle = -1;
            this.OnStatusChanged(new FuelPointValuesArgs() { CurrentFuelPoint = fp, CurrentNozzleId = -1, Values = values });
            //this.EnqueValues(fp, values);
        }

        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }
    }

    public class ValueHolder
    {
        private List<FuelPointValues> values = new List<FuelPointValues>();

        public FuelPoint FuelPump { set; get; }
        public FuelPointValues[] Values
        {
            get { return this.values.ToArray(); }
        }

        public void AddValues(FuelPointValues vals)
        {
            foreach(FuelPointValues fv in this.values)
            {
                if (fv.Equals(vals))
                    return;
            }
            if (vals.Status == Enumerators.FuelPointStatusEnum.Work)
            {
                var q1 = this.values.Where(v => v.Status == Enumerators.FuelPointStatusEnum.Work);
                if (q1.Count() > 0)
                {
                    this.ClearValues(Enumerators.FuelPointStatusEnum.Work);
                }
            }
            this.values.Add(vals);
        }

        public FuelPointValues GetValues()
        {
            if (this.values.Count == 0)
                return null;
            FuelPointValues vals = this.values[0];
            this.values.RemoveAt(0);
            if (values.Count % 10 == 0 && values.Count > 1)
                Console.WriteLine("Values COUNT : {0}", values.Count);
            return vals;
        }

        public void ClearValues(Enumerators.FuelPointStatusEnum status)
        {
            var q = this.values.ToArray();
            foreach (FuelPointValues vals in q)
                this.values.Remove(vals);
        }
    }
}
