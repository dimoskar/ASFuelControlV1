using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;

namespace ASFuelControl.Elbis
{
    public class ElbhsController : ASFuelControl.Common.IController
    {
        private ConcurrentDictionary<ASFuelControl.Common.FuelPoint, ConcurrentQueue<ASFuelControl.Common.FuelPointValues>> internalQueue = new ConcurrentDictionary<ASFuelControl.Common.FuelPoint, ConcurrentQueue<ASFuelControl.Common.FuelPointValues>>();
        private ConcurrentDictionary<ASFuelControl.Common.Tank, ConcurrentQueue<ASFuelControl.Common.TankValues>> internalTankQueue = new ConcurrentDictionary<ASFuelControl.Common.Tank, ConcurrentQueue<ASFuelControl.Common.TankValues>>();

        private Dictionary<Common.Nozzle, Common.Sales.SaleData> lostSales = new Dictionary<Common.Nozzle, Common.Sales.SaleData>();
        
        ElbisDLL_New.clsComm MyCom = new ElbisDLL_New.clsComm();
        private bool isConnected = false;
        private bool isInitialized = false;
        private List<FuelPoint> fuelPoints = new List<FuelPoint>();
        private List<Common.Tank> tanks = new List<Common.Tank>();
        private System.Threading.Thread th;
        private bool threadRun = false;

        public ASFuelControl.Common.Enumerators.ControllerTypeEnum ControllerType { set; get; }

        public bool EuromatEnabled { set; get; }
        public string EuromatIp { set; get; }
        public int EuromatPort { set; get; }

        public ElbhsController()
        {
            ControllerType = Common.Enumerators.ControllerTypeEnum.Elbis;
        }

        public string ControllerSerialNumber { set; get; }

        public string CommunicationPort
        {
            get
            {
                return "";
            }
            set
            {
                
            }
        }

        public Common.Enumerators.CommunicationTypeEnum CommunicationType
        {
            get
            {
                return Common.Enumerators.CommunicationTypeEnum.RS485;
            }
            set
            {
                
            }
        }

        public bool IsConnected
        {
            get { return this.isConnected; }
        }

        public event EventHandler<Common.TotalsEventArgs> TotalsRecieved;

        public event EventHandler<Common.FuelPointValuesArgs> ValuesRecieved;
        public void AddFuelPoint(int channel, int address, int nozzleCount, int decimalPlaces, int volumeDecimalPlaces, int untiPriceDecimalPlaces)
        {
            throw new NotImplementedException();
        }
        public bool SetNozzleIndex(int channel, int address, int nozzeId, int nozzleIndex)
        {
            throw new NotImplementedException();
        }

        public virtual void SetEuromatDispenserNumber(int channel, int address, int number)
        {
            
        }

        public bool HaltDispenser(int channel, int address)
        {
            return false;
        }

        public bool ResumeDispenser(int channel, int address)
        {
            return false;
        }

        public void Connect()
        {
            long totalDevices = 0;
            
            this.MyCom = new ElbisDLL_New.clsComm();
            this.MyCom.ScanAllDevices(ref totalDevices);
            if (totalDevices > -1)
            {
                for (int i = 0; i <= totalDevices - 1; i++ )
                {
                    if (this.MyCom.FTDI_Device[i].SerialNumber != this.ControllerSerialNumber)
                        continue;
                    this.MyCom.USB_WorkDeviceNr = i;
                    if (this.MyCom.InitDeviceNew())
                    {
                        this.isConnected = true;
                        //this.MyCom.OnPressetingsChange += new ElbisDLL_New.clsComm.OnPressetingsChangeEventHandler(MyCom_OnPressetingsChange);
                        //this.MyCom.OnDeliveryChange += new ElbisDLL_New.clsComm.OnDeliveryChangeEventHandler(MyCom_OnDeliveryChange);
                        this.MyCom.OnChangeUnitPrice += new ElbisDLL_New.clsComm.OnChangeUnitPriceEventHandler(MyCom_OnChangeUnitPrice);
                        this.MyCom.Nozzle_Change += new ElbisDLL_New.clsComm.Nozzle_ChangeEventHandler(MyCom_Nozzle_Change);
                        this.MyCom.OnPressetAmount += new ElbisDLL_New.clsComm.OnPressetAmountEventHandler(MyCom_OnPressetAmount);
                        this.MyCom.OnPressetVolume += new ElbisDLL_New.clsComm.OnPressetVolumeEventHandler(MyCom_OnPressetVolume);
                        //this.MyCom.OnReadTotalsChange += new ElbisDLL_New.clsComm.OnReadTotalsChangeEventHandler(MyCom_OnReadTotalsChange);
                        //this.MyCom.OnStatusChange += new ElbisDLL_New.clsComm.OnStatusChangeEventHandler(MyCom_OnStatusChange);

                        this.MyCom.ReadController_DD(ElbisDLL_New.clsComm.DD_ASK_PRESSETINGS);
                    }
                    else
                        this.isConnected = false;
                }
                this.threadRun = true;
                this.isInitialized = true;
                this.th = new System.Threading.Thread(new System.Threading.ThreadStart(this.ThreadRun));
                this.th.Start();

                return;
            }
        }

        public void DisConnect()
        {
            this.threadRun = false;
        }

        private void ThreadRun()
        {
            while (this.threadRun)
            {
                this.MyCom.ReadController_DD(ElbisDLL_New.clsComm.DD_ASK_PRESSETINGS);
                System.Threading.Thread.Sleep(70);

                if (isInitialized)
                {
                    this.MyCom.ReadController_DD(ElbisDLL_New.clsComm.DD_ASK_DELIVERIES);
                    System.Threading.Thread.Sleep(70);

                    this.MyCom.ReadController_DD(ElbisDLL_New.clsComm.DD_ASK_TOTALS);
                    System.Threading.Thread.Sleep(70);
                    
                    foreach (FuelPoint fp in this.fuelPoints)
                    {
                        foreach (Nozzle nz in fp.Nozzles)
                        {
                            if (nz.SetPriceCompleted)
                                continue;
                            if (nz.UnitPrice == 0)
                                continue;
                            int chan = fp.Channel + nz.NozzleIndex - 1;
                            this.MyCom.ChangeUnitPrice(nz.UnitPrice.ToString(), chan);
                        }
                    }

                    this.CheckDispensers();
                    this.CheckAtgs();
                }
               //System.Threading.Thread.Sleep(100);
            }
        }

        private void CheckDispensers()
        {
            foreach (FuelPoint fp in this.fuelPoints)
            {
                Common.Enumerators.FuelPointStatusEnum newStatus = this.GetStatus(MyCom.Channel[fp.Channel].ActionPoint.APointStatus, fp);
                int nozzle = MyCom.Channel[fp.Channel].ActionPoint.NozzlesNozzle % 16;
                Common.FuelPointValues values = new Common.FuelPointValues();
                values.Status = newStatus;
                values.ActiveNozzle = nozzle - 1;
                fp.CommonFP.ActiveNozzleIndex = values.ActiveNozzle;

                switch (newStatus)
                {
                    case Common.Enumerators.FuelPointStatusEnum.Offline:
                        fp.ActiveNozzle = null;
                        fp.DispenserStatus = newStatus;
                        this.EnqueValues(fp.CommonFP, values);

                        break;
                    case Common.Enumerators.FuelPointStatusEnum.Idle:
                        fp.ActiveNozzle = null;
                        foreach (Nozzle nz in fp.Nozzles)
                        {
                            nz.Totalizer = MyCom.Channel[fp.Channel + nz.NozzleIndex - 1].Totals.TotalVolume;
                            fp.CommonFP.Nozzles[nz.NozzleIndex - 1].TotalVolume = MyCom.Channel[fp.Channel + nz.NozzleIndex - 1].Totals.TotalVolume;
                            fp.CommonFP.Nozzles[nz.NozzleIndex - 1].TotalPrice = MyCom.Channel[fp.Channel + nz.NozzleIndex - 1].Totals.TotalAmount;
                        }
                        fp.DispenserStatus = newStatus;
                        this.EnqueValues(fp.CommonFP, values);
                        
                        break;
                    case Common.Enumerators.FuelPointStatusEnum.Nozzle:
                        fp.ActiveNozzle = fp.Nozzles[values.ActiveNozzle];
                        fp.ActiveNozzle.Totalizer = MyCom.Channel[fp.Channel + nozzle - 1].Totals.TotalVolume;
                        fp.CommonFP.ActiveNozzle.TotalVolume = MyCom.Channel[fp.Channel + nozzle - 1].Totals.TotalVolume;
                        fp.CommonFP.ActiveNozzle.TotalPrice = MyCom.Channel[fp.Channel + nozzle - 1].Totals.TotalAmount;
                        fp.CommonFP.ActiveNozzle.CurrentSale = new Common.Sales.SaleData();
                        fp.CommonFP.ActiveNozzle.CurrentSale.TotalizerStart = fp.ActiveNozzle.Totalizer;
                        fp.DispenserStatus = newStatus;
                        this.EnqueValues(fp.CommonFP, values);
                        break;
                    case Common.Enumerators.FuelPointStatusEnum.Ready:
                        fp.ActiveNozzle = fp.Nozzles[values.ActiveNozzle];
                        fp.DispenserStatus = newStatus;
                        this.EnqueValues(fp.CommonFP, values);
                        break;
                    case Common.Enumerators.FuelPointStatusEnum.Work:
                        fp.ActiveNozzle = fp.Nozzles[values.ActiveNozzle];
                        values.CurrentPriceTotal = (decimal)MyCom.Channel[fp.Channel + nozzle - 1].Delivery.CurrentAmount / (decimal)Math.Pow(10, fp.CommonFP.AmountDecimalPlaces);
                        values.CurrentSalePrice = (decimal)MyCom.Channel[fp.Channel + nozzle - 1].Delivery.CurrentPrice / (decimal)Math.Pow(10, fp.CommonFP.UnitPriceDecimalPlaces);
                        values.CurrentVolume = (decimal)MyCom.Channel[fp.Channel + nozzle - 1].Delivery.CurrentVolume / (decimal)Math.Pow(10, fp.CommonFP.AmountDecimalPlaces);
                        fp.DispenserStatus = newStatus;
                        this.EnqueValues(fp.CommonFP, values);
                        break;
                    case Common.Enumerators.FuelPointStatusEnum.TransactionStopped:
                    case Common.Enumerators.FuelPointStatusEnum.TransactionCompleted:
                        Nozzle saleNozzle = null;
                        Common.Nozzle commonNozzle;
                        if (nozzle > 0)
                        {
                            fp.ActiveNozzle = fp.Nozzles[values.ActiveNozzle];
                            saleNozzle = fp.ActiveNozzle;
                            commonNozzle = fp.CommonFP.ActiveNozzle;
                        }
                        else
                        {
                            fp.ActiveNozzle = null;
                            saleNozzle = fp.LastNozzle;
                            commonNozzle = fp.CommonFP.LastActiveNozzle;
                        }
                        if (saleNozzle == null)
                            return;
                        
                        decimal newTotal = MyCom.Channel[fp.Channel + nozzle - 1].Totals.TotalVolume;
                        if (commonNozzle.CurrentSale!= null && !commonNozzle.CurrentSale.SaleCompleted)
                        {
                            saleNozzle.Totalizer = newTotal;
                            commonNozzle.TotalVolume = newTotal;
                            values.CurrentPriceTotal = (decimal)MyCom.Channel[fp.Channel + nozzle - 1].Delivery.CurrentAmount / (decimal)Math.Pow(10, fp.CommonFP.AmountDecimalPlaces);
                            values.CurrentSalePrice = (decimal)MyCom.Channel[fp.Channel + nozzle - 1].Delivery.CurrentPrice / (decimal)Math.Pow(10, fp.CommonFP.UnitPriceDecimalPlaces);
                            values.CurrentVolume = (decimal)MyCom.Channel[fp.Channel + nozzle - 1].Delivery.CurrentVolume / (decimal)Math.Pow(10, fp.CommonFP.AmountDecimalPlaces);
                            commonNozzle.CurrentSale.UnitPrice = values.CurrentSalePrice;
                            if (this.UpdateSale(commonNozzle, values))
                            {
                                fp.DispenserStatus = newStatus;
                                this.EnqueValues(fp.CommonFP, values);
                                this.MyCom.Communication.set_ChannelAction(fp.Channel, ElbisDLL_New.clsComm.CHANNEL_CLEAR_GET_TRANSACTION_FLAG);
                            }
                        }
                        else if (commonNozzle.CurrentSale == null)
                        {
                            fp.DispenserStatus = newStatus;
                            this.MyCom.Communication.set_ChannelAction(fp.Channel, ElbisDLL_New.clsComm.CHANNEL_CLEAR_GET_TRANSACTION_FLAG);
                        }
                        break;
                }
            }
        }

        private void CheckAtgs()
        {
            foreach (Common.Tank tank in this.tanks)
            {
                Common.TankValues vals = new Common.TankValues();
                vals.Status = Common.Enumerators.TankStatusEnum.Idle;
                vals.FuelHeight = (decimal)MyCom.Channel[tank.Channel].Delivery.DisplayAmount / 10;
                vals.WaterHeight = (decimal)MyCom.Channel[tank.Channel].Delivery.DisplayVolume / 10;
                if ((char)MyCom.Channel[tank.Channel].ActionPoint.PumpErrorCode == '+')
                    vals.CurrentTemperatur = (decimal)MyCom.Channel[tank.Channel].Delivery.DisplayPrice / 10;
                else
                    vals.CurrentTemperatur = -(decimal)MyCom.Channel[tank.Channel].Delivery.DisplayPrice / 10;

                this.internalTankQueue[tank] = new ConcurrentQueue<Common.TankValues>();
                this.internalTankQueue[tank].Enqueue(vals);
            }
        }

        private bool UpdateSale(Common.Nozzle nozzle, Common.FuelPointValues values)
        {
            if (nozzle.CurrentSale == null)
                return false;
            if (nozzle.CurrentSale.TotalizerEnd == nozzle.TotalVolume)
                return false;
            nozzle.CurrentSale.TotalizerEnd = nozzle.TotalVolume;
            nozzle.CurrentSale.TotalVolume = values.CurrentVolume;
            nozzle.CurrentSale.TotalPrice = values.CurrentPriceTotal;
            nozzle.CurrentSale.SaleCompleted = true;
            return true;
        }

        //void MyCom_OnReadTotalsChange(object AnswerOnExplane)
        //{
        //    for (int i = 1; i < 41; i++)
        //    {
        //        if (MyCom.Channel[i].ActionPoint.DeviceModel <= 0)
        //            continue;
        //        int devModel = MyCom.Channel[i].ActionPoint.DeviceModel;
        //        if (((string[])MyCom.DeviceModel)[devModel] == "Italiana") //MyCom.Channel[i].ActionPoint.DeviceModel == 
        //        {
        //            continue;
        //        }
        //        if (MyCom.Channel[i].ActionPoint.APointStatus.IsMaster)
        //        {
        //            FuelPoint fp = this.fuelPoints.Where(f => f.AddressId == MyCom.Channel[i].ActionPoint.UseIdentity).FirstOrDefault();
        //            fp.QueryTotals = false;
        //            fp.Status = fp.DispenserStatus;

        //            Common.FuelPointValues values = new Common.FuelPointValues();
        //            values.Status = fp.Status;

        //            if (fp.ActiveNozzle == null)
        //                values.ActiveNozzle = -1;
        //            else
        //                values.ActiveNozzle = fp.ActiveNozzle.NozzleIndex - 1;

        //            Nozzle saleNozzle = fp.ActiveNozzle;
        //            if (saleNozzle == null)
        //                saleNozzle = fp.LastNozzle;
        //            if (saleNozzle != null)
        //            {
        //                Common.Nozzle salesCommonNozzle = fp.CommonFP.Nozzles[saleNozzle.NozzleId];
        //                if (salesCommonNozzle.CurrentSale != null)
        //                {
        //                    values.CurrentSalePrice = saleNozzle.SaleUnitPrice;

        //                    salesCommonNozzle.CurrentSale.TotalizerEnd = (decimal)MyCom.Channel[i + saleNozzle.NozzleId].Totals.TotalVolume / (decimal)100;
        //                    decimal total = salesCommonNozzle.CurrentSale.TotalizerEnd - salesCommonNozzle.CurrentSale.TotalizerStart;
        //                    decimal price = decimal.Round(total * values.CurrentSalePrice, fp.VolumeDecimalPlaces);
        //                    //values.CurrentPriceTotal = price;
        //                    //values.CurrentVolume = total;

        //                    values.CurrentPriceTotal = (decimal)MyCom.Channel[i + fp.LastNozzle.NozzleIndex - 1].Delivery.CurrentAmount / (decimal)Math.Pow(10, fp.CommonFP.DecimalPlaces);
        //                    values.CurrentSalePrice = (decimal)MyCom.Channel[i + fp.LastNozzle.NozzleIndex - 1].Delivery.CurrentPrice / (decimal)Math.Pow(10, fp.CommonFP.UnitPriceDecimalPlaces);
        //                    values.CurrentVolume = (decimal)MyCom.Channel[i + fp.LastNozzle.NozzleIndex - 1].Delivery.CurrentVolume / (decimal)Math.Pow(10, fp.CommonFP.DecimalPlaces);


        //                    if (total == 0)
        //                        continue;
        //                    values.Status = Common.Enumerators.FuelPointStatusEnum.TransactionCompleted;
        //                    salesCommonNozzle.CurrentSale.TotalVolume = total;
        //                    salesCommonNozzle.CurrentSale.SaleCompleted = true;
        //                    this.EnqueValues(fp.CommonFP, values);
        //                }
        //                else
        //                {
        //                }
        //            }
                   

        //            //Common.FuelPointValues values2 = new Common.FuelPointValues();
        //            //values2.ActiveNozzle = -1;
        //            //values2.Status = Common.Enumerators.FuelPointStatusEnum.Idle;

        //            //this.EnqueValues(fp.CommonFP, values2);
        //        }
        //    }
        //}

        void MyCom_OnPressetVolume(string Answer)
        {
        }

        void MyCom_OnPressetAmount(string Answer)
        {
            
        }

        void MyCom_Nozzle_Change(int OnChannel)
        {
        }

        void MyCom_OnChangeUnitPrice(string Answer)
        {
            for (int i = 1; i < 41; i++)
            {
                if (MyCom.Channel[i].ActionPoint.DeviceModel <= 0)
                    continue;
                int devModel = MyCom.Channel[i].ActionPoint.DeviceModel;
                if (((string[])MyCom.DeviceModel)[devModel] == "Italiana") //MyCom.Channel[i].ActionPoint.DeviceModel == 
                {
                    //this.AddAtgInternal(i, (int)MyCom.Channel[i].ActionPoint.UseIdentity);
                    continue;
                }
                else
                {
                    long price = this.MyCom.Channel[i].Delivery.CurrentPrice;
                    FuelPoint fp = this.fuelPoints.Where(f => f.AddressId == MyCom.Channel[i].ActionPoint.UseIdentity).FirstOrDefault();
                    if (fp == null)
                        continue;
                    int nozzle = i - fp.Channel;
                    if (fp.Nozzles[nozzle].UnitPrice == price)
                    {
                        fp.Nozzles[nozzle].SetPriceCompleted = true;
                        fp.Nozzles[nozzle].SetPriceRequest = false;

                        Common.FuelPointValues values = new Common.FuelPointValues();
                        values.Status = (ASFuelControl.Common.Enumerators.FuelPointStatusEnum)(int)fp.DispenserStatus;
                        this.EnqueValues(fp.CommonFP, values);
                    }
                }
            }
        }

        //void MyCom_OnDeliveryChange(object AnswerOnExplane)
        //{
        //    if (AnswerOnExplane.ToString() != "OK")
        //        return;
        //    for (int i = 1; i < 41; i++)
        //    {
        //        if (MyCom.Channel[i].ActionPoint.DeviceModel <= 0)
        //            continue;
        //        int devModel = MyCom.Channel[i].ActionPoint.DeviceModel;
        //        if (((string[])MyCom.DeviceModel)[devModel] == "Italiana") //MyCom.Channel[i].ActionPoint.DeviceModel == 
        //        {
        //            Common.Tank tank = tanks.Where(t => t.Address == MyCom.Channel[i].ActionPoint.UseIdentity).FirstOrDefault();
        //            Common.TankValues vals = new Common.TankValues();
        //            vals.Status = Common.Enumerators.TankStatusEnum.Idle;
        //            vals.FuelHeight = (decimal)MyCom.Channel[i].Delivery.DisplayAmount / 10;
        //            vals.WaterHeight = (decimal)MyCom.Channel[i].Delivery.DisplayVolume / 10;
        //            if((char)MyCom.Channel[i].ActionPoint.PumpErrorCode == '+')
        //                vals.CurrentTemperatur = (decimal)MyCom.Channel[i].Delivery.DisplayPrice / 10;
        //            else
        //                vals.CurrentTemperatur = -(decimal)MyCom.Channel[i].Delivery.DisplayPrice / 10;

        //            this.internalTankQueue[tank] = new ConcurrentQueue<Common.TankValues>();
        //            this.internalTankQueue[tank].Enqueue(vals);
        //            continue;
        //        }
        //        else
        //        {
        //            if (MyCom.Channel[i].ActionPoint.APointStatus.IsMaster)
        //            {
        //                FuelPoint fp = this.fuelPoints.Where(f => f.AddressId == MyCom.Channel[i].ActionPoint.UseIdentity).FirstOrDefault();
        //                if (fp == null)
        //                    continue;
        //                int nozzle = MyCom.Channel[i].ActionPoint.NozzlesNozzle % 16;
        //                if (!MyCom.Channel[i].ActionPoint.APointStatus.NozzleOut)
        //                    nozzle = -1;
                        
        //                Common.FuelPointValues values = new Common.FuelPointValues();
        //                values.Status = fp.Status;
        //                if (fp.DispenserStatus == Common.Enumerators.FuelPointStatusEnum.Offline || fp.DispenserStatus == Common.Enumerators.FuelPointStatusEnum.Idle)
        //                {
        //                    values.ActiveNozzle = -1;
        //                    fp.CommonFP.ActiveNozzle = null;
        //                }
        //                else
        //                {
        //                    values.ActiveNozzle = nozzle - 1;
        //                    if (fp.CommonFP.Nozzles.Length <= values.ActiveNozzle + 1)
        //                        fp.CommonFP.ActiveNozzle = fp.CommonFP.Nozzles[values.ActiveNozzle];
        //                    else
        //                        fp.CommonFP.ActiveNozzle = null;
        //                }
        //                if (values.ActiveNozzle < 0)
        //                    values.ActiveNozzle = -1;


        //                if (values.ActiveNozzle >= 0)
        //                {
        //                    if (fp.CommonFP.Nozzles.Length <= values.ActiveNozzle + 1)
        //                        fp.ActiveNozzle = fp.Nozzles[values.ActiveNozzle];
        //                    else
        //                        fp.ActiveNozzle = null;
        //                }
        //                else
        //                    fp.ActiveNozzle = null;


        //                values.CurrentPriceTotal = (decimal)MyCom.Channel[i].Delivery.DisplayAmount / (decimal)Math.Pow(10, fp.CommonFP.DecimalPlaces);
        //                values.CurrentSalePrice = (decimal)MyCom.Channel[i].Delivery.DisplayPrice / (decimal)Math.Pow(10, fp.CommonFP.UnitPriceDecimalPlaces);
        //                values.CurrentVolume = (decimal)MyCom.Channel[i].Delivery.DisplayVolume / (decimal)Math.Pow(10, fp.CommonFP.DecimalPlaces);
        //                if (fp.CommonFP.ActiveNozzle != null)
        //                {
        //                    fp.CommonFP.ActiveNozzle.CurrentSale.TotalPrice = values.CurrentPriceTotal;
        //                    fp.CommonFP.ActiveNozzle.CurrentSale.TotalVolume = values.CurrentVolume;
        //                    fp.CommonFP.ActiveNozzle.CurrentSale.UnitPrice = values.CurrentSalePrice;
        //                }

        //                this.EnqueValues(fp.CommonFP, values);
        //            }
        //        }
        //    }
        //}

        //void MyCom_OnStatusChange(object AnswerOnExplane)
        //{
        //    //if (!this.isInitialized)
        //    //    return;
        //    for (int i = 1; i < 41; i++)
        //    {
        //        if (MyCom.Channel[i].ActionPoint.DeviceModel <= 0)
        //            continue;
        //        int devModel = MyCom.Channel[i].ActionPoint.DeviceModel;
        //        if (((string[])MyCom.DeviceModel)[devModel] == "Italiana") //MyCom.Channel[i].ActionPoint.DeviceModel == 
        //        {

        //            continue;
        //        }
        //        else
        //        {
        //            if (MyCom.Channel[i].ActionPoint.APointStatus.IsMaster)
        //            {
        //                FuelPoint fp = this.fuelPoints.Where(f => f.AddressId == MyCom.Channel[i].ActionPoint.UseIdentity).FirstOrDefault();
        //                if (fp == null)
        //                    continue;
        //                lock (fp)
        //                {
        //                    Common.Enumerators.FuelPointStatusEnum newStatus = this.GetStatus(MyCom.Channel[i].ActionPoint.APointStatus, fp);
        //                    //if (newStatus != Common.Enumerators.FuelPointStatusEnum.Idle)
        //                    //    Console.WriteLine("Elbis Status {0}, {1}", newStatus, fp.AddressId);
        //                    if (fp.DispenserStatus == newStatus)
        //                        continue;

        //                    fp.DispenserStatus = newStatus;

        //                    int nozzle = MyCom.Channel[i].ActionPoint.NozzlesNozzle % 16;
        //                    if (!MyCom.Channel[i].ActionPoint.APointStatus.NozzleOut)
        //                        nozzle = -1;

        //                    Common.FuelPointValues values = new Common.FuelPointValues();
        //                    values.Status = (ASFuelControl.Common.Enumerators.FuelPointStatusEnum)(int)fp.Status;
        //                    if (fp.DispenserStatus == Common.Enumerators.FuelPointStatusEnum.Offline || fp.DispenserStatus == Common.Enumerators.FuelPointStatusEnum.Idle)
        //                    {
        //                        values.ActiveNozzle = -1;
        //                        fp.CommonFP.ActiveNozzle = null;
        //                    }
        //                    else
        //                    {
        //                        values.ActiveNozzle = nozzle - 1;
        //                        if (fp.CommonFP.Nozzles.Length <= values.ActiveNozzle + 1)
        //                            fp.CommonFP.ActiveNozzle = fp.CommonFP.Nozzles[values.ActiveNozzle];
        //                        else
        //                            fp.CommonFP.ActiveNozzle = null;
        //                    }
        //                    if (values.ActiveNozzle < 0)
        //                        values.ActiveNozzle = -1;

        //                    if (values.ActiveNozzle >= 0)
        //                    {
        //                        if (fp.CommonFP.Nozzles.Length <= values.ActiveNozzle + 1)
        //                            fp.ActiveNozzle = fp.Nozzles[values.ActiveNozzle];
        //                        else
        //                            fp.ActiveNozzle = null;
        //                    }
        //                    else
        //                        fp.ActiveNozzle = null;

        //                    if (fp.DispenserStatus == Common.Enumerators.FuelPointStatusEnum.Nozzle)
        //                    {
        //                        if (fp.CommonFP.ActiveNozzle.CurrentSale == null)
        //                        {
        //                            Common.Sales.SaleData sale = new Common.Sales.SaleData();
        //                            sale.NozzleNumber = nozzle + 1;
        //                            sale.TotalizerStart = (decimal)MyCom.Channel[i + fp.ActiveNozzle.NozzleIndex - 1].Totals.TotalVolume / (decimal)100;
        //                            sale.UnitPrice = (decimal)MyCom.Channel[i].Delivery.DisplayPrice / (decimal)Math.Pow(10, fp.CommonFP.UnitPriceDecimalPlaces);
        //                            fp.CommonFP.ActiveNozzle.CurrentSale = sale;
        //                            values.Status = Common.Enumerators.FuelPointStatusEnum.Nozzle;
        //                            values.CurrentPriceTotal = 0;
        //                            values.CurrentSalePrice = 0;
        //                            values.CurrentVolume = 0;
        //                        }
        //                        else
        //                        {
        //                            fp.CommonFP.ActiveNozzle.CurrentSale.TotalizerEnd = (decimal)MyCom.Channel[i + fp.ActiveNozzle.NozzleIndex - 1].Totals.TotalVolume / (decimal)100;
        //                            continue;
        //                        }
        //                    }
        //                    else if (fp.DispenserStatus == Common.Enumerators.FuelPointStatusEnum.TransactionCompleted)
        //                    {
        //                        decimal dispVolume = 0;
        //                        Common.Sales.SaleData sale = null;
        //                        Common.Nozzle saleNozzle = null;
        //                        if (fp.ActiveNozzle == null && fp.LastNozzle != null)
        //                        {
        //                            values.ActiveNozzle = fp.LastNozzle.NozzleIndex - 1;
        //                            values.CurrentPriceTotal = (decimal)MyCom.Channel[i + fp.LastNozzle.NozzleIndex - 1].Delivery.CurrentAmount / (decimal)Math.Pow(10, fp.CommonFP.DecimalPlaces);
        //                            values.CurrentSalePrice = (decimal)MyCom.Channel[i + fp.LastNozzle.NozzleIndex - 1].Delivery.CurrentPrice / (decimal)Math.Pow(10, fp.CommonFP.UnitPriceDecimalPlaces);
        //                            values.CurrentVolume = (decimal)MyCom.Channel[i + fp.LastNozzle.NozzleIndex - 1].Delivery.CurrentVolume / (decimal)Math.Pow(10, fp.CommonFP.DecimalPlaces);

        //                            decimal lastTotal = (decimal)MyCom.Channel[i + fp.LastNozzle.NozzleIndex - 1].Totals.TotalVolume / (decimal)100;
        //                            if (lastTotal > fp.LastNozzle.Totalizer)
        //                                fp.LastNozzle.Totalizer = (decimal)MyCom.Channel[i + fp.LastNozzle.NozzleIndex - 1].Totals.TotalVolume / (decimal)100;

        //                            dispVolume = values.CurrentVolume;
                                    
        //                            if (fp.CommonFP.LastActiveNozzle.CurrentSale != null)
        //                            {
        //                                fp.CommonFP.LastActiveNozzle.CurrentSale.TotalizerEnd = fp.LastNozzle.Totalizer;
        //                                fp.CommonFP.LastActiveNozzle.CurrentSale.TotalVolume = values.CurrentVolume;// fp.CommonFP.LastActiveNozzle.CurrentSale.TotalizerEnd - fp.CommonFP.LastActiveNozzle.CurrentSale.TotalizerStart;
        //                                fp.CommonFP.LastActiveNozzle.CurrentSale.TotalPrice = values.CurrentPriceTotal;// decimal.Round(fp.CommonFP.LastActiveNozzle.CurrentSale.TotalVolume * values.CurrentSalePrice, fp.VolumeDecimalPlaces);

        //                                sale = fp.CommonFP.LastActiveNozzle.CurrentSale;
        //                                saleNozzle = fp.CommonFP.LastActiveNozzle;
        //                                Console.WriteLine("SALE IS NOT NULL {0} ", fp.AddressId);
        //                            }
                                   
        //                        }
        //                        else
        //                        {
        //                            values.CurrentPriceTotal = (decimal)MyCom.Channel[i].Delivery.DisplayAmount / (decimal)Math.Pow(10, fp.CommonFP.DecimalPlaces);
        //                            values.CurrentSalePrice = (decimal)MyCom.Channel[i].Delivery.DisplayPrice / (decimal)Math.Pow(10, fp.CommonFP.UnitPriceDecimalPlaces);
        //                            values.CurrentVolume = (decimal)MyCom.Channel[i].Delivery.DisplayVolume / (decimal)Math.Pow(10, fp.CommonFP.DecimalPlaces);
        //                            dispVolume = values.CurrentVolume;
        //                            if (fp.ActiveNozzle != null)
        //                            {                                        
        //                                decimal lastTotal = (decimal)MyCom.Channel[i + fp.ActiveNozzle.NozzleIndex - 1].Totals.TotalVolume / (decimal)100;
        //                                if (lastTotal > fp.ActiveNozzle.Totalizer)
        //                                    fp.ActiveNozzle.Totalizer = (decimal)MyCom.Channel[i + fp.ActiveNozzle.NozzleIndex - 1].Totals.TotalVolume / (decimal)100;

        //                                if (fp.CommonFP.ActiveNozzle.CurrentSale != null)
        //                                {
        //                                    fp.CommonFP.ActiveNozzle.CurrentSale.TotalizerEnd = fp.ActiveNozzle.Totalizer;
        //                                    fp.CommonFP.ActiveNozzle.CurrentSale.TotalVolume = values.CurrentVolume;// fp.CommonFP.ActiveNozzle.CurrentSale.TotalizerEnd - fp.CommonFP.ActiveNozzle.CurrentSale.TotalizerStart;
        //                                    fp.CommonFP.ActiveNozzle.CurrentSale.TotalPrice = values.CurrentPriceTotal;// decimal.Round(fp.CommonFP.ActiveNozzle.CurrentSale.TotalVolume * values.CurrentSalePrice, fp.VolumeDecimalPlaces); //values.CurrentPriceTotal;

        //                                    sale = fp.CommonFP.ActiveNozzle.CurrentSale;
        //                                    saleNozzle = fp.CommonFP.ActiveNozzle;
        //                                    Console.WriteLine("SALE IS NOT NULL {0} ", fp.AddressId);
        //                                }
        //                            }
        //                        }
        //                        if (sale != null)
        //                            sale.IsOnSale = false;
        //                        if (dispVolume > 0 && sale != null && sale.TotalVolume == 0)
        //                        {
        //                            continue;
        //                        }
        //                        if (sale != null)
        //                        {
        //                            sale.SaleCompleted = true;
                                    
        //                            Console.WriteLine("SALE IS NOT NULL {0} ", fp.AddressId);
        //                        }
        //                        else
        //                        {
        //                            Console.WriteLine("SALE IS NULL {0} ", fp.AddressId);
        //                        }

        //                    }
        //                    else if (fp.DispenserStatus == Common.Enumerators.FuelPointStatusEnum.Work && fp.CommonFP.ActiveNozzle != null && fp.CommonFP.ActiveNozzle.CurrentSale != null)
        //                    {
        //                        values.CurrentPriceTotal = (decimal)MyCom.Channel[i].Delivery.DisplayAmount / (decimal)Math.Pow(10, fp.CommonFP.DecimalPlaces);
        //                        values.CurrentSalePrice = (decimal)MyCom.Channel[i].Delivery.DisplayPrice / (decimal)Math.Pow(10, fp.CommonFP.UnitPriceDecimalPlaces);
        //                        values.CurrentVolume = (decimal)MyCom.Channel[i].Delivery.DisplayVolume / (decimal)Math.Pow(10, fp.CommonFP.DecimalPlaces);
        //                        fp.CommonFP.ActiveNozzle.CurrentSale.TotalVolume = values.CurrentVolume;
        //                        fp.CommonFP.ActiveNozzle.CurrentSale.TotalPrice = values.CurrentPriceTotal;
        //                        fp.CommonFP.ActiveNozzle.CurrentSale.IsOnSale = true;
        //                    }

        //                    else
        //                    {
        //                        values.CurrentPriceTotal = 0;
        //                        values.CurrentSalePrice = 0;
        //                        values.CurrentVolume = 0;
        //                        if (fp.DispenserStatus == Common.Enumerators.FuelPointStatusEnum.Idle)
        //                            values.Status = Common.Enumerators.FuelPointStatusEnum.Idle;
        //                        else if(fp.DispenserStatus == Common.Enumerators.FuelPointStatusEnum.Work)
        //                            fp.CommonFP.ActiveNozzle.CurrentSale.IsOnSale = true;
        //                    }

        //                    this.EnqueValues(fp.CommonFP, values);
        //                    //if (values.Status == Common.Enumerators.FuelPointStatusEnum.GetEndTotals)
        //                    //{
        //                    //    Common.FuelPointValues values2 = new Common.FuelPointValues();
        //                    //    values2.ActiveNozzle = -1;
        //                    //    values2.Status = Common.Enumerators.FuelPointStatusEnum.Idle;

        //                    //    this.EnqueValues(fp.CommonFP, values2);
        //                    //}
        //                    //if (fp.DispenserStatus == Common.Enumerators.FuelPointStatusEnum.TransactionCompleted)
        //                    //{
        //                    //    this.MyCom.Communication.set_ChannelAction(fp.Channel, ElbisDLL_New.clsComm.CHANNEL_CLEAR_GET_TRANSACTION_FLAG);
        //                    //}
        //                }
        //            }
        //        }
        //    }
            
        //}

        //void MyCom_OnPressetingsChange(object AnswerOnExplane)
        //{
        //    if (!this.isInitialized)
        //    {
        //        Dictionary<long, List<long>> fps = new Dictionary<long, List<long>>();
        //        Dictionary<long, List<long>> ts = new Dictionary<long, List<long>>();
        //        for (int i = 1; i < 41; i++)
        //        {
        //            if (MyCom.Channel[i].ActionPoint.DeviceModel <= 0)
        //                continue;
        //            int devModel = MyCom.Channel[i].ActionPoint.DeviceModel;
        //            if (((string[])MyCom.DeviceModel)[devModel] == "Italiana") //MyCom.Channel[i].ActionPoint.DeviceModel == 
        //            {
        //                if (!ts.ContainsKey(MyCom.Channel[i].ActionPoint.UseIdentity))
        //                    ts.Add(MyCom.Channel[i].ActionPoint.UseIdentity, new List<long>());

        //                ts[MyCom.Channel[i].ActionPoint.UseIdentity].Add(i);
        //            }
        //            else
        //            {
        //                if (!fps.ContainsKey(MyCom.Channel[i].ActionPoint.UseIdentity))
        //                    fps.Add(MyCom.Channel[i].ActionPoint.UseIdentity, new List<long>());

        //                fps[MyCom.Channel[i].ActionPoint.UseIdentity].Add(i);
        //            }
        //        }
        //        foreach (long address in fps.Keys)
        //        {
        //            this.AddFuelPointInternal((int)fps[address].Min(), (int)address, fps[address].Count, 2, 3);
        //        }
        //        foreach (long address in ts.Keys)
        //        {
        //            this.AddAtgInternal((int)ts[address].First(), (int)address);
        //        }
        //        //this.isInitialized = true;
        //    }
        //    else
        //    {

        //    }
        //}

        private Common.Enumerators.FuelPointStatusEnum GetStatus(ElbisDLL_New.clsComm.AnyActionPointStatus status, FuelPoint fp)
        {
            lock (fp)
            {
                Common.Enumerators.FuelPointStatusEnum newStatus = Common.Enumerators.FuelPointStatusEnum.Offline;
                try
                {
                    if (status.GetTransaction)
                    {
                        if (fp.DispenserStatus == Common.Enumerators.FuelPointStatusEnum.Offline && fp.PreviousStatus == Common.Enumerators.FuelPointStatusEnum.Offline)
                        {
                            this.MyCom.Communication.set_ChannelAction(fp.Channel, ElbisDLL_New.clsComm.CHANNEL_CLEAR_GET_TRANSACTION_FLAG);
                            newStatus = Common.Enumerators.FuelPointStatusEnum.Idle;
                            //return newStatus;
                        }
                        newStatus = Common.Enumerators.FuelPointStatusEnum.TransactionCompleted;
                    }
                    else if (status.NozzleBlocked)
                    {
                        this.MyCom.Communication.set_ChannelAction(fp.Channel, ElbisDLL_New.clsComm.CHANNEL_RESUME);
                        newStatus = fp.DispenserStatus;
                        //return newStatus;
                    }
                    else if (status.NozzleOut && !status.Authorized && !status.Filling && !status.NozzleBlocked && !status.GetTransaction)
                        newStatus = Common.Enumerators.FuelPointStatusEnum.Nozzle;
                    else if (status.NozzleOut && status.Authorized && !status.Filling && !status.NozzleBlocked && !status.GetTransaction)
                        newStatus = Common.Enumerators.FuelPointStatusEnum.Ready;
                    else if (status.NozzleOut && status.Filling && !status.NozzleBlocked)
                        newStatus = Common.Enumerators.FuelPointStatusEnum.Work;
                    else if (status.NozzleOut && status.NozzleBlocked)
                        newStatus = Common.Enumerators.FuelPointStatusEnum.Error;
                    else
                    {
                        newStatus = Common.Enumerators.FuelPointStatusEnum.Idle;
                    }
                    
                    return newStatus;

                }
                finally
                {
                    //if (newStatus == Common.Enumerators.FuelPointStatusEnum.Idle)
                    //Console.WriteLine("====================================={8} : {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}", newStatus, status.Authorized, status.Filling, status.GetTransaction, status.NozzleAlive, status.NozzleBlocked, status.NozzleOut, status.RequestAuthorize, fp.AddressId);
                }
            }
        }

        private void AddFuelPointInternal(int channel, int address, int nozzleCount, int decimalPlaces, int untiPriceDecimalPlaces)
        {
            byte[] bytes = BitConverter.GetBytes(channel);
            FuelPoint fpi = new FuelPoint(nozzleCount);
            this.fuelPoints.Add(fpi);
            fpi.AddressId = address;
            fpi.Channel = channel;
            fpi.PriceDecimalPlaces = untiPriceDecimalPlaces;
            fpi.VolumeDecimalPlaces = decimalPlaces;
            foreach (Nozzle nozzle in fpi.Nozzles)
            {
                nozzle.PriceDecimalPlaces = untiPriceDecimalPlaces;
                nozzle.VolumeDecimalPlaces = decimalPlaces;
            }
            //this.controller.AddFuelPoint(fpi);

            ASFuelControl.Common.FuelPoint fp1 = new Common.FuelPoint();
            fp1.Address = address;
            fp1.Channel = channel;
            fp1.NozzleCount = nozzleCount;
            fp1.AmountDecimalPlaces = 2;
            fp1.UnitPriceDecimalPlaces = 3;
            fpi.CommonFP = fp1;
            
            foreach(Common.Nozzle nz in fp1.Nozzles)
                nz.SaleLostEvent+=new EventHandler<Common.LostSaleEventArgs>(nz_SaleLostEvent);

            if (this.internalQueue.ContainsKey(fp1))
                return;
            this.internalQueue.TryAdd(fp1, new ConcurrentQueue<ASFuelControl.Common.FuelPointValues>());
        }

        public void AddFuelPoint(int channel, int address, int nozzleCount)
        {
            
        }

        public void AddFuelPoint(int channel, int address, int nozzleCount, int decimalPlaces, int untiPriceDecimalPlaces)
        {
            byte[] bytes = BitConverter.GetBytes(channel);
            FuelPoint fpi = new FuelPoint(nozzleCount);
            this.fuelPoints.Add(fpi);
            fpi.AddressId = address;
            fpi.Channel = channel;
            fpi.PriceDecimalPlaces = untiPriceDecimalPlaces;
            fpi.VolumeDecimalPlaces = decimalPlaces;
            foreach (Nozzle nozzle in fpi.Nozzles)
            {
                nozzle.PriceDecimalPlaces = untiPriceDecimalPlaces;
                nozzle.VolumeDecimalPlaces = decimalPlaces;
            }
            //this.controller.AddFuelPoint(fpi);

            ASFuelControl.Common.FuelPoint fp1 = new Common.FuelPoint();
            fp1.Address = address;
            fp1.Channel = channel;
            fp1.NozzleCount = nozzleCount;
            fp1.AmountDecimalPlaces = 2;
            fp1.UnitPriceDecimalPlaces = 3;
            fpi.CommonFP = fp1;

            foreach (Common.Nozzle nz in fp1.Nozzles)
                nz.SaleLostEvent += new EventHandler<Common.LostSaleEventArgs>(nz_SaleLostEvent);

            if (this.internalQueue.ContainsKey(fp1))
                return;
            this.internalQueue.TryAdd(fp1, new ConcurrentQueue<ASFuelControl.Common.FuelPointValues>());
        }

        public void AddAtgInternal(int channel, int address)
        {
            Common.Tank t = new Common.Tank();
            t.Address = address;
            t.Channel = channel;
            this.internalTankQueue.TryAdd(t, new ConcurrentQueue<Common.TankValues>());
            this.tanks.Add(t);
        }

        public void AddAtg(int channel, int address)
        {
            Common.Tank t = new Common.Tank();
            t.Address = address;
            t.Channel = channel;
            this.internalTankQueue.TryAdd(t, new ConcurrentQueue<Common.TankValues>());
            this.tanks.Add(t);
        }

        public bool AuthorizeDispenser(int channel, int address)
        {
            FuelPoint fp = this.fuelPoints.Where(f => f.AddressId == address).FirstOrDefault();
            if (fp == null)
                return false;
            this.MyCom.Communication.set_ChannelAction(fp.Channel, ElbisDLL_New.clsComm.CHANNEL_MAKE_AUTHORIZE);
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
            try
            {
                FuelPoint fp = this.fuelPoints.Where(f => f.AddressId == address).FirstOrDefault();
                if (fp == null)
                    return false;
                int newPriceInt = (int)(newPrice * (decimal)Math.Pow(10, fp.PriceDecimalPlaces));
                if (fp.Nozzles[nozzleId - 1].UnitPrice == newPriceInt)
                    return true;
                fp.Nozzles[nozzleId - 1].SetPriceRequest = true;
                fp.Nozzles[nozzleId - 1].SetPriceCompleted = false;
                fp.Nozzles[nozzleId - 1].UnitPrice = newPriceInt;

                System.Threading.Thread.Sleep(500);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public Common.FuelPointValues GetDispenserValues(int channel, int address)
        {
            FuelPoint fp = this.fuelPoints.Where(f => f.AddressId == address ).FirstOrDefault();
            if (fp == null)
                return null;
            Common.FuelPoint fuelPoint = fp.CommonFP;
            if (this.internalQueue[fuelPoint].Count > 0)
            {
                ASFuelControl.Common.FuelPointValues vals;
                this.internalQueue[fuelPoint].TryDequeue(out vals);
                if ((vals.Status == Common.Enumerators.FuelPointStatusEnum.Idle || vals.Status == Common.Enumerators.FuelPointStatusEnum.Offline) && vals.ActiveNozzle >= 0)
                    vals.ActiveNozzle = -1;
                return vals;
            }
            return null;
        }

        public void GetNozzleTotalValues(int channel, int address, int nozzleId)
        {
            try
            {
                FuelPoint fp = this.fuelPoints.Where(f => f.AddressId == address).FirstOrDefault();
                if (fp == null)
                    return;
                if (this.TotalsRecieved != null)
                {
                    System.Threading.Thread.Sleep(100);
                    decimal totalVol = this.MyCom.Channel[fp.Channel + nozzleId - 1].Totals.TotalVolume;
                    decimal totalPrice = this.MyCom.Channel[fp.Channel + nozzleId - 1].Totals.TotalVolume;
                    this.TotalsRecieved(this, new Common.TotalsEventArgs(fp.CommonFP, nozzleId, totalVol, totalPrice));
                }
            }
            catch
            {

            }
        }

        public decimal GetNozzleTotalVolume(int channel, int address, int nozzleId)
        {
            FuelPoint fp = this.fuelPoints.Where(f => f.AddressId == address).FirstOrDefault();
            if (fp == null)
                return 0;
            if (fp.Nozzles.Length < nozzleId)
                return 0;
            return fp.Nozzles[nozzleId - 1].Totalizer;
        }

        public decimal GetNozzleTotalPrice(int channel, int address, int nozzleId)
        {
            throw (new Exception("Not Implemented"));
        }

        public decimal[] GetPrices(int channel, int address)
        {
            FuelPoint fp = this.fuelPoints.Where(f => f.AddressId == address ).FirstOrDefault();
            if (fp == null)
                return null;
            return fp.Nozzles.OrderBy(n => n.NozzleIndex).Select(n => n.UnitPrice / (decimal)Math.Pow(10, fp.CommonFP.UnitPriceDecimalPlaces)).ToArray();

        }

        public decimal GetNozzlePrice(int channel, int address, int nozzleId)
        {
            FuelPoint fp = this.fuelPoints.Where(f => f.AddressId == address).FirstOrDefault();
            if (fp == null)
                return 0;
            return fp.Nozzles.Where(n => n.NozzleIndex == nozzleId).First().UnitPrice / (decimal)Math.Pow(10, fp.CommonFP.UnitPriceDecimalPlaces);
        }

        public decimal GetNozzleTotalizer(int channel, int address, int nozzleId)
        {
            FuelPoint fp = this.fuelPoints.Where(f => f.AddressId == address).FirstOrDefault();
            if (fp == null)
                return 0;
            return fp.Nozzles.Where(n => n.NozzleIndex == nozzleId).First().Totalizer;
        }

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
                   
                    this.internalTankQueue[ctank].TryPeek(out vals);
                    return vals;
                }
                return null;

            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public virtual Common.Sales.SaleData GetSale(int channel, int address, int nozzleId)
        {
            FuelPoint fp = this.fuelPoints.Where(f => f.AddressId == address).FirstOrDefault();
            if (fp == null)
                return null;
            Common.FuelPoint fuelPoint = fp.CommonFP;

            if (fuelPoint == null)
                return null;
            Common.Nozzle nz = fuelPoint.Nozzles.Where(n => n.Index == nozzleId).FirstOrDefault();
            if (nz == null)
                return null;
            if (nz.CurrentSale != null && nz.CurrentSale.SaleCompleted && nz.CurrentSale.TotalizerEnd > 0)
            {
                Common.Sales.SaleData sale = nz.CurrentSale;
                nz.CurrentSale = null;
                this.MyCom.Communication.set_ChannelAction(fp.Channel, ElbisDLL_New.clsComm.CHANNEL_CLEAR_GET_TRANSACTION_FLAG);
               // Console.WriteLine("SALE {0} IS COMPELETED", fp.AddressId);
                return sale;
            }
            else if (nz.CurrentSale != null)
            {
                //Console.WriteLine("SALE {0} IS NOT READY {2} {1}", fp.AddressId, nz.TotalVolume, nz.LastTotalVolume);
            }
            return null;
        }

        private void EnqueValues(ASFuelControl.Common.FuelPoint fuelPoint, ASFuelControl.Common.FuelPointValues values)
        {
            //FuelPoint fPoint = this.GetFuelPoint(fuelPoint.Channel, fuelPoint.Address);

            //if (fPoint == null)
            //    return;
            if (!this.internalQueue.ContainsKey(fuelPoint))
                return;
            if (this.internalQueue[fuelPoint].Count == 0)
                this.internalQueue[fuelPoint].Enqueue(values);
            else
            {
                try
                {
                    ASFuelControl.Common.FuelPointValues oldValues = this.internalQueue[fuelPoint].LastOrDefault();
                    if (oldValues == null)
                        this.internalQueue[fuelPoint].Enqueue(values);
                    else
                    {
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
                    this.internalQueue[fuelPoint].Enqueue(values);
                }
            }
        }

        void fp_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            FuelPoint fp = sender as FuelPoint;
            if (fp == null)
                return;

            KeyValuePair<ASFuelControl.Common.FuelPoint, ConcurrentQueue<ASFuelControl.Common.FuelPointValues>> pair = this.internalQueue.Where(iv => iv.Key.Address == fp.AddressId).FirstOrDefault();
            if (pair.Key == null)
                return;
            System.Diagnostics.Trace.WriteLine("FulePoint Status Changed Address :" + fp.AddressId.ToString());
            Common.FuelPoint fuelPoint = pair.Key;

            ASFuelControl.Common.FuelPointValues values = new ASFuelControl.Common.FuelPointValues();

            values.ActiveNozzle = -1;
            values.CurrentPriceTotal = fp.LastSalePrice;
            values.CurrentVolume = fp.LastSaleVolume;

            if (fp.ActiveNozzle != null)
            {
                values.ActiveNozzle = fp.ActiveNozzle.NozzleIndex - 1;
                values.CurrentSalePrice = fp.LastSaleUnitPrice;
            }
            values.Status = (ASFuelControl.Common.Enumerators.FuelPointStatusEnum)(int)fp.DispenserStatus;


            this.EnqueValues(fuelPoint, values);
        }

        void nz_SaleLostEvent(object sender, Common.LostSaleEventArgs e)
        {
            Common.Nozzle nozzle = sender as Common.Nozzle;
            if (nozzle == null || e.Sale == null)
                return;

            lostSales.Add(nozzle, e.Sale);
        }
    }
}
