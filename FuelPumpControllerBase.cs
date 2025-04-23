using ASFuelControl.Common.Enumerators;
using ASFuelControl.Common.Sales;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ASFuelControl.Common
{
    public class FuelPumpControllerBase : IController
    {
        private ControllerTypeEnum controllerType;

        private bool isConnected = false;

        protected List<FuelPoint> fuelPoints = new List<FuelPoint>();

        protected ConcurrentDictionary<FuelPoint, ConcurrentQueue<FuelPointValues>> internalQueue = new ConcurrentDictionary<FuelPoint, ConcurrentQueue<FuelPointValues>>();

        private IFuelProtocol controller = null;

        private List<Guid> salesCompleted = new List<Guid>();

        private bool euromatEnabled = false;

        private EuromatClient euromat = new EuromatClient();

        private string euromatIp;

        private int euromatPort;

        private DateTime lastValuesSent = DateTime.MinValue;

        [CompilerGenerated]
        private static Func<Nozzle, decimal> CS$<>9__CachedAnonymousMethodDelegateb;

        [CompilerGenerated]
        private static Func<Nozzle, decimal> CS$<>9__CachedAnonymousMethodDelegate13;

        [CompilerGenerated]
        private static Func<Nozzle, decimal> CS$<>9__CachedAnonymousMethodDelegate15;

        public string CommunicationPort
        {
            get
            {
                string str;
                str = (this.Controller != null ? this.Controller.CommunicationPort : "");
                return str;
            }
            set
            {
                if (this.Controller != null)
                {
                    this.Controller.CommunicationPort = value;
                }
            }
        }

        public CommunicationTypeEnum CommunicationType
        {
            get;
            set;
        }

        public IFuelProtocol Controller
        {
            get
            {
                return this.controller;
            }
            set
            {
                this.controller = value;
                if (this.controller != null)
                {
                    this.controller.DataChanged += new EventHandler<FuelPointValuesArgs>(this.controller_DataChanged);
                    this.controller.TotalsRecieved += new EventHandler<TotalsEventArgs>(this.controller_TotalsRecieved);
                    this.controller.DispenserStatusChanged += new EventHandler<FuelPointValuesArgs>(this.controller_DispenserStatusChanged);
                }
            }
        }

        public ControllerTypeEnum ControllerType
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

        public bool EuromatEnabled
        {
            get
            {
                return this.euromatEnabled;
            }
            set
            {
                this.euromatEnabled = value;
                if (value)
                {
                    this.euromat.ServerIp = this.EuromatIp;
                    this.euromat.ServrerPort = this.EuromatPort;
                }
            }
        }

        public string EuromatIp
        {
            get
            {
                return this.euromatIp;
            }
            set
            {
                this.euromatIp = value;
                this.euromat.ServerIp = this.EuromatIp;
            }
        }

        public int EuromatPort
        {
            get
            {
                return this.euromatPort;
            }
            set
            {
                this.euromatPort = value;
                this.euromat.ServrerPort = this.euromatPort;
            }
        }

        public bool IsConnected
        {
            get
            {
                bool flag;
                flag = (this.Controller != null ? this.Controller.IsConnected : false);
                return flag;
            }
        }

        public FuelPumpControllerBase()
        {
            this.euromat.EuromatAuthorize += new EventHandler<EuromatAuthorizeEventArgs>(this.euromat_EuromatAuthorize);
        }

        [CompilerGenerated]
        private static decimal <controller_DataChanged>b__14(Nozzle n)
        {
            return n.TotalVolume;
        }

        [CompilerGenerated]
        private static decimal <GetPrices>b__a(Nozzle n)
        {
            return n.UnitPrice;
        }

        [CompilerGenerated]
        private static decimal <OnStatusChanged>b__12(Nozzle n)
        {
            return n.TotalVolume;
        }

        public void AddAtg(int channel, int address)
        {
            throw new NotImplementedException();
        }

        public virtual void AddFuelPoint(int channel, int address, int nozzleCount)
        {
            this.AddFuelPoint(channel, address, nozzleCount, 2, 2, 3);
        }

        public virtual void AddFuelPoint(int channel, int address, int nozzleCount, int amountDecimalPlaces, int volumeDecimalPlaces, int untiPriceDecimalPlaces)
        {
            FuelPoint fuelPoint = new FuelPoint()
            {
                NozzleCount = nozzleCount
            };
            this.fuelPoints.Add(fuelPoint);
            fuelPoint.Address = address;
            fuelPoint.Channel = channel;
            fuelPoint.UnitPriceDecimalPlaces = untiPriceDecimalPlaces;
            fuelPoint.AmountDecimalPlaces = amountDecimalPlaces;
            fuelPoint.VolumeDecimalPlaces = volumeDecimalPlaces;
            this.internalQueue.TryAdd(fuelPoint, new ConcurrentQueue<FuelPointValues>());
            this.Controller.AddFuelPoint(fuelPoint);
        }

        public virtual bool AuthorizeDispenser(int channel, int address)
        {
            bool flag;
            FuelPoint fulePoint = this.GetFulePoint(channel, address);
            if (fulePoint == null)
            {
                flag = false;
            }
            else if (fulePoint.ActiveNozzle != null)
            {
                if ((!this.euromatEnabled ? false : fulePoint.EuromatNumber > 0))
                {
                    this.euromat.PumpNotification(fulePoint.EuromatNumber, fulePoint.ActiveNozzleIndex + 1);
                }
                else
                {
                    fulePoint.QueryAuthorize = true;
                    fulePoint.PresetVolume = new decimal(999999);
                    fulePoint.PresetAmount = new decimal(999999);
                }
                flag = true;
            }
            else
            {
                flag = false;
            }
            return flag;
        }

        public virtual bool AuthorizeDispenserAmountPreset(int channel, int address, decimal price)
        {
            bool flag;
            FuelPoint fulePoint = this.GetFulePoint(channel, address);
            if (fulePoint == null)
            {
                flag = false;
            }
            else if (fulePoint.ActiveNozzle != null)
            {
                fulePoint.QueryAuthorize = true;
                fulePoint.PresetVolume = new decimal(999999);
                fulePoint.PresetAmount = price;
                flag = true;
            }
            else
            {
                flag = false;
            }
            return flag;
        }

        public virtual bool AuthorizeDispenserVolumePreset(int channel, int address, decimal volume)
        {
            bool flag;
            FuelPoint fulePoint = this.GetFulePoint(channel, address);
            if (fulePoint == null)
            {
                flag = false;
            }
            else if (fulePoint.ActiveNozzle != null)
            {
                fulePoint.QueryAuthorize = true;
                fulePoint.PresetVolume = volume;
                fulePoint.PresetAmount = new decimal(999999);
                flag = true;
            }
            else
            {
                flag = false;
            }
            return flag;
        }

        public virtual void Connect()
        {
            if (this.Controller != null)
            {
                this.Controller.CommunicationPort = this.CommunicationPort;
                this.Controller.Connect();
            }
        }

        private void controller_DataChanged(object sender, FuelPointValuesArgs e)
        {
            FuelPoint currentFuelPoint = e.CurrentFuelPoint;
            if (currentFuelPoint != null)
            {
                decimal num = (decimal)((double)Math.Pow(10, (double)currentFuelPoint.AmountDecimalPlaces));
                if ((currentFuelPoint.ActiveNozzle == null ? false : currentFuelPoint.ActiveNozzle.CurrentSale != null))
                {
                    currentFuelPoint.ActiveNozzle.CurrentSale.UnitPrice = e.Values.CurrentSalePrice;
                    currentFuelPoint.ActiveNozzle.CurrentSale.SaleEndTime = DateTime.Now;
                    e.Values.HasTotalMisfunction = currentFuelPoint.ActiveNozzle.TotalMisfunction;
                }
                e.Values.ActiveNozzle = e.CurrentNozzleId - 1;
                e.Values.TotalVolumes = (
                    from n in (IEnumerable<Nozzle>)currentFuelPoint.Nozzles
                    select n.TotalVolume).ToArray<decimal>();
                e.Values.Status = currentFuelPoint.Status;
                currentFuelPoint.Status = e.Values.Status;
                this.EnqueValues(currentFuelPoint, e.Values);
            }
        }

        private void controller_DispenserStatusChanged(object sender, FuelPointValuesArgs e)
        {
            this.OnStatusChanged(e);
        }

        private void controller_TotalsRecieved(object sender, TotalsEventArgs e)
        {
            this.OnTransactionCompleted(e);
        }

        public virtual DebugValues DebugStatusDialog(FuelPoint fp)
        {
            throw new NotImplementedException();
        }

        public virtual void DisConnect()
        {
            if (this.Controller != null)
            {
                this.fuelPoints.Clear();
                this.Controller.ClearFuelPoints();
                this.Controller.Disconnect();
            }
        }

        protected void EnqueValues(FuelPoint fuelPoint, FuelPointValues values)
        {
            FuelPointValues fuelPointValue;
            fuelPoint.LastValues = values;
            if (this.internalQueue.ContainsKey(fuelPoint))
            {
                if (this.internalQueue[fuelPoint].Count == 0)
                {
                    this.internalQueue[fuelPoint].Enqueue(values);
                }
                else if (this.internalQueue[fuelPoint].Last<FuelPointValues>().Status == values.Status)
                {
                    this.internalQueue[fuelPoint].TryDequeue(out fuelPointValue);
                    this.internalQueue[fuelPoint].Enqueue(values);
                }
                else
                {
                    this.internalQueue[fuelPoint].Enqueue(values);
                }
            }
        }

        private void euromat_EuromatAuthorize(object sender, EuromatAuthorizeEventArgs e)
        {
            FuelPoint volumeCash = (
                from f in this.fuelPoints
                where f.EuromatNumber == e.EuromatVars.EuromatDispenserNumber
                select f).FirstOrDefault<FuelPoint>();
            if (volumeCash != null)
            {
                if (volumeCash.DispenserStatus == FuelPointStatusEnum.Nozzle)
                {
                    lock (volumeCash)
                    {
                        volumeCash.QueryAuthorize = true;
                        if (e.EuromatVars.RateType != RateTypeEnum.Cash)
                        {
                            volumeCash.PresetVolume = e.EuromatVars.Nozzles[volumeCash.ActiveNozzleIndex].VolumeCash;
                            volumeCash.PresetAmount = e.EuromatVars.Nozzles[volumeCash.ActiveNozzleIndex].AmountCash;
                        }
                        else
                        {
                            volumeCash.PresetVolume = e.EuromatVars.Nozzles[volumeCash.ActiveNozzleIndex].VolumeCredit;
                            volumeCash.PresetAmount = e.EuromatVars.Nozzles[volumeCash.ActiveNozzleIndex].AmountCredit;
                        }
                    }
                }
            }
        }

        public virtual FuelPointValues GetDispenserValues(int channel, int address)
        {
            FuelPointValues fuelPointValue;
            FuelPointValues fuelPointValue1;
            KeyValuePair<FuelPoint, ConcurrentQueue<FuelPointValues>> keyValuePair = (
                from iv in this.internalQueue
                where (iv.Key.Channel != channel ? false : iv.Key.Address == address)
                select iv).FirstOrDefault<KeyValuePair<FuelPoint, ConcurrentQueue<FuelPointValues>>>();
            if (keyValuePair.Key != null)
            {
                FuelPoint key = keyValuePair.Key;
                if (this.internalQueue[key].Count <= 0)
                {
                    fuelPointValue1 = null;
                }
                else
                {
                    this.internalQueue[key].TryDequeue(out fuelPointValue);
                    fuelPointValue1 = fuelPointValue;
                }
            }
            else
            {
                fuelPointValue1 = null;
            }
            return fuelPointValue1;
        }

        protected FuelPoint GetFulePoint(int channel, int address)
        {
            FuelPoint fuelPoint;
            FuelPoint fuelPoint1 = (
                from f in this.Controller.FuelPoints
                where (f.Address != address ? false : f.Channel == channel)
                select f).FirstOrDefault<FuelPoint>();
            if (fuelPoint1 != null)
            {
                fuelPoint = fuelPoint1;
            }
            else
            {
                fuelPoint = null;
            }
            return fuelPoint;
        }

        public virtual decimal GetNozzlePrice(int channel, int address, int nozzleId)
        {
            decimal num;
            FuelPoint fulePoint = this.GetFulePoint(channel, address);
            if (fulePoint != null)
            {
                num = (fulePoint.NozzleCount <= nozzleId ? fulePoint.Nozzles[nozzleId - 1].UnitPrice : new decimal(0));
            }
            else
            {
                num = new decimal(0);
            }
            return num;
        }

        public virtual decimal GetNozzleTotalizer(int channel, int address, int nozzleId)
        {
            decimal num;
            FuelPoint fulePoint = this.GetFulePoint(channel, address);
            if (fulePoint == null)
            {
                File.AppendAllText(string.Concat(Environment.CurrentDirectory, "\\InitializeERROR.txt"), "\nGetNozzleTotalizer Failed: fp == null");
                num = new decimal(-1);
            }
            else if (fulePoint.NozzleCount >= nozzleId)
            {
                num = (fulePoint.Initialized ? fulePoint.Nozzles[nozzleId - 1].TotalVolume : new decimal(-1));
            }
            else
            {
                File.AppendAllText(string.Concat(Environment.CurrentDirectory, "\\InitializeERROR.txt"), "\nGetNozzleTotalizer Failed: fp.NozzleCount < nozzleId");
                num = new decimal(-1);
            }
            return num;
        }

        public virtual decimal GetNozzleTotalPrice(int channel, int address, int nozzleId)
        {
            decimal num;
            FuelPoint fulePoint = this.GetFulePoint(channel, address);
            if (fulePoint != null)
            {
                num = (fulePoint.NozzleCount <= nozzleId ? fulePoint.Nozzles[nozzleId - 1].TotalVolume : new decimal(0));
            }
            else
            {
                num = new decimal(0);
            }
            return num;
        }

        public virtual void GetNozzleTotalValues(int channel, int address, int nozzleId)
        {
            FuelPoint fulePoint = this.GetFulePoint(channel, address);
            if (fulePoint != null)
            {
                fulePoint.Nozzles[nozzleId - 1].QueryTotals = true;
            }
        }

        public virtual decimal GetNozzleTotalVolume(int channel, int address, int nozzleId)
        {
            decimal num;
            FuelPoint fulePoint = this.GetFulePoint(channel, address);
            if (fulePoint != null)
            {
                num = (fulePoint.NozzleCount <= nozzleId ? fulePoint.Nozzles[nozzleId - 1].TotalVolume : new decimal(0));
            }
            else
            {
                num = new decimal(0);
            }
            return num;
        }

        public virtual decimal[] GetPrices(int channel, int address)
        {
            decimal[] numArray;
            FuelPoint fulePoint = this.GetFulePoint(channel, address);
            numArray = (fulePoint != null ? (
                from n in (IEnumerable<Nozzle>)fulePoint.Nozzles
                select n.UnitPrice).ToArray<decimal>() : new decimal[0]);
            return numArray;
        }

        public virtual SaleData GetSale(int channel, int address, int nozzleId)
        {
            SaleData saleDatum;
            FuelPoint fulePoint = this.GetFulePoint(channel, address);
            if (fulePoint != null)
            {
                Nozzle nozzle = (
                    from n in fulePoint.Nozzles
                    where n.Index == nozzleId
                    select n).FirstOrDefault<Nozzle>();
                if (nozzle != null)
                {
                    SaleData lastSale = nozzle.GetLastSale();
                    if (lastSale != null)
                    {
                        lastSale.SaleProcessed = true;
                        saleDatum = lastSale;
                    }
                    else
                    {
                        saleDatum = null;
                    }
                }
                else
                {
                    saleDatum = null;
                }
            }
            else
            {
                saleDatum = null;
            }
            return saleDatum;
        }

        public TankValues GetTankValues(int channel, int address)
        {
            throw new NotImplementedException();
        }

        public virtual bool HaltDispenser(int channel, int address)
        {
            bool flag;
            try
            {
                this.GetFulePoint(channel, address).QueryHalt = true;
                flag = true;
            }
            catch
            {
                flag = false;
            }
            return flag;
        }

        protected virtual void OnStatusChanged(FuelPointValuesArgs e)
        {
            FuelPoint currentFuelPoint = e.CurrentFuelPoint;
            if (currentFuelPoint != null)
            {
                decimal num = (decimal)((double)Math.Pow(10, (double)currentFuelPoint.AmountDecimalPlaces));
                FuelPointValues fuelPointValue = new FuelPointValues()
                {
                    ActiveNozzle = currentFuelPoint.ActiveNozzleIndex
                };
                if (currentFuelPoint.Status != FuelPointStatusEnum.Offline)
                {
                    fuelPointValue.CurrentPriceTotal = currentFuelPoint.DispensedAmount;
                    fuelPointValue.CurrentVolume = currentFuelPoint.DispensedVolume;
                }
                if (currentFuelPoint.ActiveNozzle != null)
                {
                    fuelPointValue.ActiveNozzle = currentFuelPoint.ActiveNozzleIndex;
                }
                if (currentFuelPoint.DispenserStatus == FuelPointStatusEnum.Nozzle)
                {
                    currentFuelPoint.DispensedAmount = new decimal(0);
                    currentFuelPoint.DispensedVolume = new decimal(0);
                }
                fuelPointValue.Status = currentFuelPoint.Status;
                fuelPointValue.TotalVolumes = (
                    from n in (IEnumerable<Nozzle>)currentFuelPoint.Nozzles
                    select n.TotalVolume).ToArray<decimal>();
                Nozzle activeNozzle = currentFuelPoint.ActiveNozzle;
                if (activeNozzle == null)
                {
                    activeNozzle = currentFuelPoint.LastActiveNozzle;
                }
                if (activeNozzle != null)
                {
                    fuelPointValue.HasTotalMisfunction = activeNozzle.TotalMisfunction;
                }
                if (currentFuelPoint.DispenserStatus == FuelPointStatusEnum.Idle)
                {
                    if (currentFuelPoint.EuromatNumber > 0)
                    {
                        this.euromat.NozzleClosed(currentFuelPoint.EuromatNumber);
                    }
                    if (currentFuelPoint.ActiveNozzle != null)
                    {
                        currentFuelPoint.ActiveNozzle.QueryTotals = true;
                    }
                    else if (currentFuelPoint.LastActiveNozzle != null)
                    {
                        currentFuelPoint.LastActiveNozzle.QueryTotals = true;
                    }
                }
                else if (currentFuelPoint.DispenserStatus == FuelPointStatusEnum.Offline)
                {
                    Nozzle[] nozzles = currentFuelPoint.Nozzles;
                    for (int i = 0; i < (int)nozzles.Length; i++)
                    {
                        nozzles[i].CurrentSale = null;
                    }
                }
                currentFuelPoint.Status = fuelPointValue.Status;
                this.EnqueValues(currentFuelPoint, fuelPointValue);
            }
        }

        protected virtual void OnTransactionCompleted(TotalsEventArgs e)
        {
            FuelPoint currentFuelPoint = e.CurrentFuelPoint;
            if (currentFuelPoint != null)
            {
                Nozzle nozzle = (
                    from n in currentFuelPoint.Nozzles
                    where n.Index == e.NozzleId
                    select n).FirstOrDefault<Nozzle>();
                if (this.TotalsRecieved != null)
                {
                    this.TotalsRecieved(this, new TotalsEventArgs(currentFuelPoint, e.NozzleId, nozzle.TotalVolume, new decimal(0)));
                }
                SaleData saleDatum = nozzle.saleHandler.AddTotalizer(e.TotalVolume, nozzle.UnitPrice, e.CurrentFuelPoint.DispensedVolume, e.CurrentFuelPoint.DispensedAmount);
                currentFuelPoint.Status = currentFuelPoint.DispenserStatus;
                FuelPointValues fuelPointValue = new FuelPointValues()
                {
                    Status = currentFuelPoint.DispenserStatus,
                    CurrentPriceTotal = saleDatum.TotalPrice,
                    CurrentSalePrice = saleDatum.UnitPrice,
                    CurrentVolume = saleDatum.TotalVolume,
                    HasTotalMisfunction = nozzle.TotalMisfunction
                };
                if (currentFuelPoint.EuromatNumber > 0)
                {
                    this.euromat.TransactionCompleted(currentFuelPoint.EuromatNumber, nozzle.Index, saleDatum.UnitPrice, saleDatum.TotalPrice, saleDatum.TotalVolume);
                }
                this.EnqueValues(currentFuelPoint, fuelPointValue);
            }
        }

        public virtual bool ResumeDispenser(int channel, int address)
        {
            bool flag;
            try
            {
                FuelPoint fulePoint = this.GetFulePoint(channel, address);
                if (fulePoint.Halted)
                {
                    fulePoint.QueryResume = true;
                    flag = true;
                }
                else
                {
                    flag = true;
                }
            }
            catch
            {
                flag = false;
            }
            return flag;
        }

        public virtual bool SetDispenserSalesPrice(int channel, int address, decimal[] price)
        {
            bool flag;
            FuelPoint fulePoint = this.GetFulePoint(channel, address);
            if (fulePoint == null)
            {
                flag = false;
            }
            else if (fulePoint.NozzleCount == (int)price.Length)
            {
                for (int i = 0; i < (int)price.Length; i++)
                {
                    this.SetNozzlePrice(channel, address, fulePoint.Nozzles[i].Index, price[i]);
                    Thread.Sleep(100);
                }
                flag = true;
            }
            else
            {
                flag = false;
            }
            return flag;
        }

        public virtual bool SetDispenserStatus(int channel, int address, FuelPointStatusEnum status)
        {
            throw new NotImplementedException();
        }

        public virtual void SetEuromatDispenserNumber(int channel, int address, int number)
        {
            FuelPoint fulePoint = this.GetFulePoint(channel, address);
            if (fulePoint != null)
            {
                fulePoint.EuromatNumber = number;
            }
        }

        public virtual bool SetNozzleIndex(int channel, int address, int nozzeId, int nozzleIndex)
        {
            bool flag;
            FuelPoint fulePoint = this.GetFulePoint(channel, address);
            if (fulePoint != null)
            {
                fulePoint.Nozzles[nozzeId - 1].NozzleIndex = nozzleIndex;
                flag = true;
            }
            else
            {
                flag = false;
            }
            return flag;
        }

        public virtual bool SetNozzlePrice(int channel, int address, int nozzleId, decimal newPrice)
        {
            bool flag;
            FuelPoint fulePoint = this.GetFulePoint(channel, address);
            if (fulePoint != null)
            {
                fulePoint.Nozzles[nozzleId - 1].UnitPrice = newPrice;
                fulePoint.Nozzles[nozzleId - 1].UntiPriceInt = (int)(newPrice * (decimal)((double)Math.Pow(10, (double)fulePoint.UnitPriceDecimalPlaces)));
                fulePoint.QuerySetPrice = true;
                flag = true;
            }
            else
            {
                flag = false;
            }
            return flag;
        }

        public event EventHandler<TotalsEventArgs> TotalsRecieved;

        public event EventHandler<FuelPointValuesArgs> ValuesRecieved;

        [CompilerGenerated]
        private sealed class <>c__DisplayClass1
        {
            public int channel;

            public int address;

            public <>c__DisplayClass1()
            {
            }

            public bool <GetFulePoint>b__0(FuelPoint f)
            {
                return (f.Address != this.address ? false : f.Channel == this.channel);
            }
        }

        [CompilerGenerated]
        private sealed class <>c__DisplayClass10
        {
            public TotalsEventArgs e;

            public <>c__DisplayClass10()
            {
            }

            public bool <OnTransactionCompleted>b__f(Nozzle n)
            {
                return n.Index == this.e.NozzleId;
            }
        }

        [CompilerGenerated]
        private sealed class <>c__DisplayClass5
        {
            public EuromatAuthorizeEventArgs e;

            public <>c__DisplayClass5()
            {
            }

            public bool <euromat_EuromatAuthorize>b__4(FuelPoint f)
            {
                return f.EuromatNumber == this.e.EuromatVars.EuromatDispenserNumber;
            }
        }

        [CompilerGenerated]
        private sealed class <>c__DisplayClass8
        {
            public int channel;

            public int address;

            public <>c__DisplayClass8()
            {
            }

            public bool <GetDispenserValues>b__7(KeyValuePair<FuelPoint, ConcurrentQueue<FuelPointValues>> iv)
            {
                return (iv.Key.Channel != this.channel ? false : iv.Key.Address == this.address);
            }
        }

        [CompilerGenerated]
        private sealed class <>c__DisplayClassd
        {
            public int nozzleId;

            public <>c__DisplayClassd()
            {
            }

            public bool <GetSale>b__c(Nozzle n)
            {
                return n.Index == this.nozzleId;
            }
        }
    }
}