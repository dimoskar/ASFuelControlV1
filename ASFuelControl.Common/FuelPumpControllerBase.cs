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
        public event EventHandler<FuelPointValuesArgs> SaleRetentionRequired;
        #endregion

        #region private variables

        private Enumerators.ControllerTypeEnum controllerType;
        private bool isConnected = false;
        protected List<FuelPoint> fuelPoints = new List<FuelPoint>();
        protected ConcurrentDictionary<ASFuelControl.Common.FuelPoint, ValueHolder> internalQueue = new ConcurrentDictionary<ASFuelControl.Common.FuelPoint, ValueHolder>();
        private IFuelProtocol controller = null;
        private List<Guid> salesCompleted = new List<Guid>();
        private static readonly ConcurrentDictionary<Guid, Sales.PendingControllerCompletion> RetainedCompletions = new ConcurrentDictionary<Guid, Sales.PendingControllerCompletion>();
        private readonly ConcurrentDictionary<Guid, Sales.PendingControllerCompletion> pendingCompletions = RetainedCompletions;
        private readonly object completionProcessingSync = new object();
        private long completionSequence;
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
            if (values.Sale != null)
            {
                Sales.PendingSaleRegistry.Instance.RetainFromController(values.Sale, this, fuelPoint.Channel, fuelPoint.Address, values.ActiveNozzle);
            }
            Exception notificationFailure = null;
            try
            {
                if (values.Sale != null && this.SaleRetentionRequired != null)
                {
                    FuelPointValuesArgs retentionArgs = new FuelPointValuesArgs();
                    retentionArgs.CurrentFuelPoint = fuelPoint;
                    retentionArgs.CurrentNozzleId = values.ActiveNozzle;
                    retentionArgs.Values = values;
                    this.SaleRetentionRequired(this, retentionArgs);
                }
            }
            catch (Exception exception)
            {
                notificationFailure = exception;
            }
            finally
            {
                fuelPoint.LastValues = values;
                ValueHolder holder;
                bool queueExists = this.internalQueue.TryGetValue(fuelPoint, out holder);
                if (queueExists)
                {
                    holder.AddValues(values);
                }
                else if (values.Sale != null)
                {
                    throw new InvalidOperationException("Controller value queue is missing; sale remains retained.");
                }
            }
            if (notificationFailure != null)
            {
                string message = "Sale retention subscriber failed; sale remains in the common registry. " + notificationFailure;
                System.Diagnostics.Trace.WriteLine(message);
            }
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
            bool pending = this.HasPendingCompletionOrSale(channel, address);
            if (pending)
            {
                return false;
            }
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
            bool pending = this.HasPendingCompletionOrSale(channel, address);
            if (pending)
            {
                return false;
            }
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
            bool pending = this.HasPendingCompletionOrSale(channel, address);
            if (pending)
            {
                return false;
            }
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
            this.RetryPendingCompletions();
            
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
            Sales.PendingControllerCompletion completion = new Sales.PendingControllerCompletion();
            completion.FuelPoint = e.CurrentFuelPoint;
            completion.NozzleIndex = e.NozzleId;
            completion.ReportedTotal = e.TotalVolume;
            completion.ReportedPriceTotal = e.TotalPrice;
            this.CaptureCompletion(completion);
        }

        protected virtual void OnSaleRecieved(SaleEventArgs e)
        {
            Sales.PendingControllerCompletion completion = new Sales.PendingControllerCompletion();
            completion.FuelPoint = e.CurrentFuelPoint;
            completion.NozzleIndex = e.NozzleId;
            completion.DirectSale = true;
            completion.ReportedTotal = e.TotalVolume;
            completion.ReportedPriceTotal = e.TotalPrice;
            completion.Amount = e.Amount;
            completion.Volume = e.Volume;
            this.CaptureCompletion(completion);
        }

        public int PendingCompletionCount
        {
            get
            {
                return this.pendingCompletions.Values.Count(c => c.Owner == this);
            }
        }

        private void CaptureCompletion(Sales.PendingControllerCompletion completion)
        {
            completion.Owner = this;
            completion.Sequence = System.Threading.Interlocked.Increment(ref this.completionSequence);
            this.pendingCompletions.TryAdd(completion.Id, completion);
            // Capture source values before any subscriber, fleet call or state reset.
            try
            {
                this.CaptureCompletionSnapshot(completion);
            }
            catch (Exception exception)
            {
                completion.LastError = exception.ToString();
            }
            this.RetryPendingCompletions();
        }

        private void CaptureCompletionSnapshot(Sales.PendingControllerCompletion completion)
        {
            lock (completion)
            {
                if (completion.SnapshotCaptured)
                {
                    return;
                }
                FuelPoint point = completion.FuelPoint;
                if (point == null)
                {
                    throw new InvalidOperationException("Completion has no fuel point.");
                }
                if (!completion.SourceValuesCaptured)
                {
                    if (!completion.DirectSale)
                    {
                        completion.Amount = point.DispensedAmount;
                        completion.Volume = point.DispensedVolume;
                    }
                    completion.NeedsInvoice = point.NeedsInvoice;
                    object workValue = point.GetExtendedProperty("Work", false);
                    completion.Work = workValue is bool && (bool)workValue;
                    completion.TotalDecimalPlaces = point.TotalDecimalPlaces;
                    completion.SourceValuesCaptured = true;
                }
                Nozzle nozzle = point.Nozzles.FirstOrDefault(n => n.Index == completion.NozzleIndex);
                if (nozzle == null)
                {
                    throw new InvalidOperationException("Completion nozzle was not found: " + completion.NozzleIndex);
                }
                completion.StartTotal = completion.DirectSale && nozzle.TotalVolume != completion.ReportedTotal ? nozzle.TotalVolume : nozzle.LastTotalVolume;
                completion.UnitPrice = nozzle.UnitPrice;
                completion.SnapshotCaptured = true;
            }
        }

        public void RetryPendingCompletions()
        {
            bool acquired = System.Threading.Monitor.TryEnter(this.completionProcessingSync);
            if (!acquired)
            {
                return;
            }
            try
            {
                HashSet<FuelPoint> blockedPoints = new HashSet<FuelPoint>();
                Sales.PendingControllerCompletion[] completions = this.pendingCompletions.Values.Where(c => c.Owner == this).OrderBy(c => c.Sequence).ToArray();
                foreach (Sales.PendingControllerCompletion completion in completions)
                {
                    if (completion.WaitingForTotals && completion.TotalsNotified)
                    {
                        // Keep the receipt until a later total reading finishes this completion.
                        continue;
                    }
                    bool blocked = blockedPoints.Contains(completion.FuelPoint);
                    if (blocked)
                    {
                        continue;
                    }
                    if (DateTime.UtcNow < completion.RetryAfterUtc)
                    {
                        blockedPoints.Add(completion.FuelPoint);
                        continue;
                    }
                    try
                    {
                        this.ProcessCompletion(completion);
                        if (!completion.WaitingForTotals)
                        {
                            Sales.PendingControllerCompletion removed;
                            this.pendingCompletions.TryRemove(completion.Id, out removed);
                            foreach (Sales.PendingControllerCompletion sample in completions)
                            {
                                if (sample.WaitingForTotals && sample.Sequence < completion.Sequence && sample.FuelPoint == completion.FuelPoint && sample.NozzleIndex == completion.NozzleIndex)
                                {
                                    this.pendingCompletions.TryRemove(sample.Id, out removed);
                                }
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        completion.LastError = exception.ToString();
                        completion.RetryAfterUtc = DateTime.UtcNow.AddSeconds(2);
                        blockedPoints.Add(completion.FuelPoint);
                        string message = "Controller completion retained for retry. EventId=" + completion.Id + " " + completion.LastError;
                        System.Diagnostics.Trace.WriteLine(message);
                    }
                }
            }
            finally
            {
                System.Threading.Monitor.Exit(this.completionProcessingSync);
            }
        }

        private void ProcessCompletion(Sales.PendingControllerCompletion completion)
        {
            if (!completion.SnapshotCaptured)
            {
                this.CaptureCompletionSnapshot(completion);
            }
            FuelPoint point = completion.FuelPoint;
            Nozzle nozzle = point.Nozzles.FirstOrDefault(n => n.Index == completion.NozzleIndex);
            if (nozzle == null)
            {
                throw new InvalidOperationException("Completion nozzle is no longer available.");
            }

            if (!completion.TotalsEvaluated)
            {
                completion.EndTotal = completion.ReportedTotal;
                if (!completion.DirectSale)
                {
                    nozzle.QueryTotals = false;
                    int totalIndex = nozzle.GetTotalsIndex();
                    if (totalIndex < 3)
                    {
                        nozzle.Totals.Add(completion.ReportedTotal);
                        nozzle.SetExtendedProperty("TotalIndex", totalIndex + 1);
                        bool validTotals = nozzle.HasValidTotals();
                        completion.WaitingForTotals = !validTotals;
                    }
                    if (completion.WaitingForTotals)
                    {
                        nozzle.QueryTotals = true;
                    }
                    else
                    {
                        completion.EndTotal = nozzle.GetTotalsFromList();
                        nozzle.Totals.Clear();
                        nozzle.SetExtendedProperty("TotalIndex", 0);
                    }
                }
                completion.TotalsEvaluated = true;
            }

            if (!completion.WaitingForTotals)
            {
                nozzle.TotalVolume = completion.EndTotal;
                if (!completion.DecisionMade)
                {
                    bool hasSaleValues = completion.DirectSale || completion.Volume != 0 || completion.EndTotal != completion.StartTotal;
                    completion.ShouldCreateSale = hasSaleValues && completion.NeedsInvoice && point.NeedsInvoice;
                    completion.DecisionMade = true;
                }
                if (completion.ShouldCreateSale)
                {
                    if (completion.Sale == null)
                    {
                        Sales.SaleData sale = new Sales.SaleData();
                        sale.SaleEndTime = completion.ReceivedUtc.ToLocalTime();
                        sale.DisplayPrice = completion.DirectSale || completion.Work ? completion.Amount : 0;
                        sale.DisplayVolume = completion.DirectSale || completion.Work ? completion.Volume : 0;
                        sale.TotalizerStart = completion.StartTotal;
                        sale.TotalizerEnd = completion.EndTotal;
                        sale.TotalPrice = sale.DisplayPrice;
                        sale.TotalVolume = sale.DisplayVolume;
                        sale.UnitPrice = completion.UnitPrice;
                        completion.Sale = sale;
                    }
                    if (!completion.SaleRetained)
                    {
                        if (!completion.DirectSale)
                        {
                            double multiplier = Math.Pow(10, completion.TotalDecimalPlaces);
                            decimal measuredVolume = (decimal)((double)(completion.Sale.TotalizerEnd - completion.Sale.TotalizerStart) / multiplier);
                            completion.Sale.InvalidSale = measuredVolume > completion.Sale.TotalVolume + 1 || measuredVolume < completion.Sale.TotalVolume - 1;
                        }
                        Sales.PendingSaleRegistry.Instance.RetainFromController(completion.Sale, this, point.Channel, point.Address, completion.NozzleIndex);
                        completion.SaleRetained = true;
                    }
                    // A failure after this point retries the same SaleData/SalesId.
                    point.NeedsInvoice = false;
                    point.SetExtendedProperty("Work", false);
                    if (!completion.SuspendHandled)
                    {
                        if (nozzle.SuspendSale && point.EuromatNumber > 0)
                        {
                            this.euromat.TransactionCompleted(point, point.EuromatNumber, completion.NozzleIndex, completion.UnitPrice, 0, 0);
                        }
                        nozzle.SuspendSale = false;
                        completion.SuspendHandled = true;
                    }
                    if (!completion.FleetHandled)
                    {
                        if (point.EuromatNumber > 0)
                        {
                            this.euromat.TransactionCompleted(point, point.EuromatNumber, completion.NozzleIndex, completion.UnitPrice, completion.Amount, completion.Volume);
                        }
                        completion.FleetHandled = true;
                    }
                    if (!completion.ValuesPublished)
                    {
                        FuelPointValues values = new FuelPointValues();
                        values.Sale = completion.Sale;
                        values.Status = point.DispenserStatus;
                        values.ActiveNozzle = completion.NozzleIndex;
                        values.CurrentPriceTotal = completion.Sale.TotalPrice;
                        values.CurrentSalePrice = completion.Sale.UnitPrice;
                        values.CurrentVolume = completion.Sale.TotalVolume;
                        values.HasTotalMisfunction = nozzle.TotalMisfunction;
                        values.EuromatParameters = point.EuromatTransactionParameters;
                        this.EnqueValues(point, values);
                        completion.ValuesPublished = true;
                    }
                }
                else if (!completion.ValuesPublished)
                {
                    this.CreateValuesForTotals(nozzle);
                    completion.ValuesPublished = true;
                }
            }

            // Notification is last: a throwing subscriber cannot undo sale retention.
            if (!completion.DirectSale && !completion.TotalsNotified)
            {
                if (this.TotalsRecieved != null)
                {
                    TotalsEventArgs args = new TotalsEventArgs(point, completion.NozzleIndex, completion.EndTotal, completion.ReportedPriceTotal);
                    this.TotalsRecieved(this, args);
                }
                completion.TotalsNotified = true;
            }
        }

        private bool HasPendingCompletionOrSale(int channel, int address)
        {
            bool completionPending = this.pendingCompletions.Values.Any(c => c.Owner == this && (c.FuelPoint == null || (c.FuelPoint.Channel == channel && c.FuelPoint.Address == address)));
            if (completionPending)
            {
                return true;
            }
            Sales.PendingSaleRecord[] sales = Sales.PendingSaleRegistry.Instance.Values;
            return sales.Any(s => s.SourceController == this && s.Channel == channel && s.Address == address);
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
        private readonly object syncRoot = new object();

        public FuelPoint FuelPump { set; get; }
        public FuelPointValues[] Values
        {
            get
            {
                lock (this.syncRoot)
                {
                    return this.values.ToArray();
                }
            }
        }

        public void AddValues(FuelPointValues vals)
        {
            lock (this.syncRoot)
            {
                foreach (FuelPointValues fv in this.values)
                {
                    // Separate completed sales must never be coalesced as status updates.
                    if (fv.Sale != null || vals.Sale != null)
                    {
                        if (fv.Sale != null && vals.Sale != null && fv.Sale.SalesId == vals.Sale.SalesId)
                        {
                            return;
                        }
                        continue;
                    }
                    bool sameValues = fv.Equals(vals);
                    if (sameValues)
                    {
                        return;
                    }
                }
                if (vals.Status == Enumerators.FuelPointStatusEnum.Work)
                {
                    this.ClearValues(Enumerators.FuelPointStatusEnum.Work);
                }
                this.values.Add(vals);
            }
        }

        public FuelPointValues GetValues()
        {
            lock (this.syncRoot)
            {
                if (this.values.Count == 0)
                {
                    return null;
                }
                FuelPointValues vals = this.values[0];
                this.values.RemoveAt(0);
                return vals;
            }
        }

        public void ClearValues(Enumerators.FuelPointStatusEnum status)
        {
            lock (this.syncRoot)
            {
                for (int index = this.values.Count - 1; index >= 0; index--)
                {
                    FuelPointValues queuedValues = this.values[index];
                    if (queuedValues.Status == status && queuedValues.Sale == null)
                    {
                        this.values.RemoveAt(index);
                    }
                }
            }
        }
    }
}
