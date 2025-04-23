using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ASFuelControl.Communication;
using System.IO;
using ASFuelControl.Logging;

namespace ASFuelControl.Windows.Threads
{
    public class SendAlertsThread
    {
        public event EventHandler<PrintAlertEventArgs> PrintAlert;

        Data.DatabaseModel database = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
        System.Threading.Thread th = null;
        bool haltThread = false;
        bool isTankCheck = false;
        int tankCheckInterval = 0;
        DateTime lastTankCheck = DateTime.Now.Subtract(TimeSpan.FromMinutes(5));
        private Communication.SendMethods sendMethods = new Communication.SendMethods();

        public DateTime LastAction { set; get; }

        public SendAlertsThread()
        {
            this.tankCheckInterval = Data.Implementation.OptionHandler.Instance.GetIntOption("TankCheckInterval", 60000);
            this.sendMethods.Simulation = !Data.Implementation.OptionHandler.Instance.GetBoolOption("SendData", false);
        }

        public void StartThread()
        {
            this.haltThread = false;
            this.database = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
            th = new System.Threading.Thread(new System.Threading.ThreadStart(this.ThreadRun));
            
            th.Start();
        }

        public void StopThread()
        {
            this.haltThread = true;
            
        }

        private object CreateDeliveryNote(Data.TankFilling tankFilling)
        {
            Data.InvoiceLine invoiceLine = tankFilling.InvoiceLines.FirstOrDefault();
            if (invoiceLine != null && invoiceLine.Invoice.InvoiceTypeId == Data.Implementation.OptionHandler.Instance.GetGuidOption("DeliveryCheckInvoiceType", Guid.Empty))
            {
                Communication.DeliveryNoteClass delivery = new Communication.DeliveryNoteClass();
                delivery.FuelData = new List<Communication.DeliveryNoteFuelDataClass>();
                delivery.Reservoirs = new List<Communication.DeliveryNoteReservoir>();
                delivery.Document = new Communication.DeliveryNoteDocumentClass();
                delivery.Document.Amdika = Data.Implementation.OptionHandler.Instance.GetOption("AMDIKA");
                delivery.Document.DeliveryType = Communication.Enums.DeliveryTypeEnum.Receipt;


                delivery.Document.DocumentDate = invoiceLine.Invoice.TransactionDate;
                delivery.Document.DocumentType = invoiceLine.Invoice.InvoiceType.OfficialEnumerator;
                delivery.Document.InvoiceNumber = invoiceLine.Invoice.Number;
                //delivery.Document.InvoiceSeries = invoiceLine.Invoice.
                if (invoiceLine.Invoice.Vehicle != null)
                    delivery.Document.PlateNumber = invoiceLine.Invoice.Vehicle.PlateNumber;

                if (invoiceLine.Invoice.Trader != null)
                    delivery.Document.SupplyerTin = invoiceLine.Invoice.Trader.TaxRegistrationNumber;

                Communication.DeliveryNoteFuelDataClass fuelData = new Communication.DeliveryNoteFuelDataClass();
                fuelData.Density = invoiceLine.FuelDensity;
                fuelData.Diff = tankFilling.VolumeRealNormalized - invoiceLine.VolumeNormalized;
                fuelData.FuelType = (Communication.Enums.FuelTypeEnum)tankFilling.Tank.FuelType.EnumeratorValue;
                fuelData.InvoicedVolume = invoiceLine.Volume;
                fuelData.InvoicedVolumeNormalized = invoiceLine.VolumeNormalized;
                fuelData.TemperatureLoaded = invoiceLine.Temperature;
                fuelData.TotalVolume = tankFilling.VolumeReal;
                fuelData.TotalVolumeNormalized = tankFilling.VolumeRealNormalized;
                delivery.FuelData.Add(fuelData);

                Communication.DeliveryNoteReservoir reservoir = new Communication.DeliveryNoteReservoir();
                reservoir.DateStart = tankFilling.TransactionTime;
                reservoir.DateEnd = tankFilling.TransactionTimeEnd;
                reservoir.FuelType = (Communication.Enums.FuelTypeEnum)tankFilling.Tank.FuelType.EnumeratorValue;
                reservoir.LevelAfter = tankFilling.LevelEnd;
                reservoir.LevelBefore = tankFilling.LevelStart;
                reservoir.TankId = tankFilling.Tank.TankSerialNumber;
                reservoir.TemperatureAfter = tankFilling.TankTemperatureEnd;
                reservoir.TemperatureBefore = tankFilling.TankTemperatureStart;
                reservoir.TimeEnd = tankFilling.TransactionTimeEnd;
                reservoir.TimeStart = tankFilling.TransactionTime;
                reservoir.VolumeAfter = tankFilling.Tank.GetTankVolume(tankFilling.LevelEnd);
                reservoir.VolumeAfterNormalized = tankFilling.Tank.FuelType.NormalizeVolume(reservoir.VolumeAfter, reservoir.TemperatureAfter, tankFilling.Tank.CurrentDensity);
                reservoir.VolumeBefore = tankFilling.Tank.GetTankVolume(tankFilling.LevelStart);
                reservoir.VolumeBeforeNormalized = tankFilling.Tank.FuelType.NormalizeVolume(reservoir.VolumeBefore, reservoir.TemperatureBefore, tankFilling.Tank.PreviousDensity);
                delivery.Reservoirs.Add(reservoir);

                return delivery;
            }
            else if (invoiceLine == null)
            {
                Communication.DeliveryNoteClass delivery = new Communication.DeliveryNoteClass();
                delivery.FuelData = new List<Communication.DeliveryNoteFuelDataClass>();
                delivery.Reservoirs = new List<Communication.DeliveryNoteReservoir>();
                delivery.Document = new Communication.DeliveryNoteDocumentClass();
                delivery.Document.Amdika = Data.Implementation.OptionHandler.Instance.GetOption("AMDIKA");
                delivery.Document.DeliveryType = Communication.Enums.DeliveryTypeEnum.Receipt;


                delivery.Document.DocumentDate = tankFilling.TransactionTimeEnd;
                //delivery.Document.DocumentType = invoiceLine.Invoice.InvoiceType.
                delivery.Document.InvoiceNumber = 0;
                //delivery.Document.InvoiceSeries = invoiceLine.Invoice.

                Communication.DeliveryNoteFuelDataClass fuelData = new Communication.DeliveryNoteFuelDataClass();
                fuelData.Density = tankFilling.FuelDensity;

                fuelData.FuelType = (Communication.Enums.FuelTypeEnum)tankFilling.Tank.FuelType.EnumeratorValue;
                fuelData.InvoicedVolume = tankFilling.Volume;
                fuelData.InvoicedVolumeNormalized = tankFilling.VolumeNormalized;
                fuelData.TemperatureLoaded = 0;
                fuelData.TotalVolume = tankFilling.VolumeReal;
                fuelData.TotalVolumeNormalized = tankFilling.VolumeRealNormalized;
                delivery.FuelData.Add(fuelData);
                fuelData.Diff = fuelData.TotalVolumeNormalized - fuelData.InvoicedVolumeNormalized;

                Communication.DeliveryNoteReservoir reservoir = new Communication.DeliveryNoteReservoir();
                reservoir.DateStart = tankFilling.TransactionTime;
                reservoir.DateEnd = tankFilling.TransactionTimeEnd;
                reservoir.FuelType = (Communication.Enums.FuelTypeEnum)tankFilling.Tank.FuelType.EnumeratorValue;
                reservoir.LevelAfter = tankFilling.LevelEnd;
                reservoir.LevelBefore = tankFilling.LevelStart;
                reservoir.TankId = tankFilling.Tank.TankSerialNumber;
                reservoir.TemperatureAfter = tankFilling.TankTemperatureEnd;
                reservoir.TemperatureBefore = tankFilling.TankTemperatureStart;
                reservoir.TimeEnd = tankFilling.TransactionTimeEnd;
                reservoir.TimeStart = tankFilling.TransactionTime;
                reservoir.VolumeAfter = tankFilling.Tank.GetTankVolume(tankFilling.LevelEnd);
                reservoir.VolumeAfterNormalized = tankFilling.Tank.FuelType.NormalizeVolume(reservoir.VolumeAfter, reservoir.TemperatureAfter, tankFilling.Tank.CurrentDensity);
                reservoir.VolumeBefore = tankFilling.Tank.GetTankVolume(tankFilling.LevelStart);
                reservoir.VolumeBeforeNormalized = tankFilling.Tank.FuelType.NormalizeVolume(reservoir.VolumeBefore, reservoir.TemperatureBefore, tankFilling.Tank.PreviousDensity);
                delivery.Reservoirs.Add(reservoir);

                return delivery;
            }
            else if (invoiceLine.Invoice.InvoiceTypeId == Data.Implementation.OptionHandler.Instance.GetGuidOption("LiterCheckInvoiceType", Guid.Empty))
            {
                Communication.LiterCheckClass literCheck = new Communication.LiterCheckClass();
                literCheck.Capacity = tankFilling.Volume;
                literCheck.Capacity15 = tankFilling.VolumeNormalized;
                literCheck.FuelType = (Communication.Enums.FuelTypeEnum)tankFilling.Tank.FuelType.EnumeratorValue;
                literCheck.TransactionDate = tankFilling.TransactionTimeEnd;
                literCheck.NozzleId = invoiceLine.SalesTransaction.Nozzle.SerialNumber == null ? "" : invoiceLine.SalesTransaction.Nozzle.SerialNumber;
                literCheck.TankSN = tankFilling.Tank.TankSerialNumber;
                return literCheck;
            }
            return null;
        }

        private Communication.IncomeRecieptClass CreateSale(Data.SalesTransaction sale)
        {
            Communication.IncomeRecieptClass income = new Communication.IncomeRecieptClass();
            income.NozzleId = sale.Nozzle.SerialNumber == null ? "" : sale.Nozzle.SerialNumber;
            income.PublishDateTime = sale.TransactionTimeStamp;
            income.PumpSerialNumber = sale.Nozzle.Dispenser.PumpSerialNumber;

            Data.TankSale tankSale = sale.TankSales.FirstOrDefault();
            Data.Tank tank = tankSale.Tank;
            Data.InvoiceLine invLine = sale.InvoiceLines.FirstOrDefault();

            income.TankId = tank.TankNumber;
            income.TankSerialNumber = tank.TankSerialNumber;
            income.TankTemperature = tankSale.EndTemperature;

            if (invLine != null && invLine.Invoice.Vehicle != null)
                income.PlateNumber = invLine.Invoice.Vehicle.PlateNumber;
            if (invLine != null && invLine.Invoice.Trader != null)
                income.CustomerTin = invLine.Invoice.Trader.TaxRegistrationNumber;
            income.TotalValue = sale.TotalPrice;
            if (invLine != null)
                income.VatPercentage = invLine.VatPercentage;
            else
                income.VatPercentage = 0;
            income.UnitPrice = sale.UnitPrice;
            income.FuelType = (Communication.Enums.FuelTypeEnum)tank.FuelType.EnumeratorValue;

            income.Volume = sale.Volume;
            income.Totalizer = sale.TotalizerEnd;

            return income;
        }

        private ASFuelControl.Communication.TankCheckClass[] CreateTankCheck()
        {
            bool validValues = true;
            List<ASFuelControl.Communication.TankCheckClass> tankChecks = new List<TankCheckClass>();
            foreach (Data.Tank tank in this.database.Tanks)
            {
                if (tank.PhysicalState == 0)
                {
                    continue;
                }
                ASFuelControl.Communication.TankCheckClass tankCheck = new Communication.TankCheckClass();
                tankCheck.TankId = tank.TankSerialNumber;
                tankCheck.TabkNumber = tank.TankSerialNumber;
                if (tank.FuelLevel == null)
                {
                    continue;
                }
                tankCheck.TankLevel = tank.FuelLevel;
                tankCheck.TankTemperature = tank.Temperatire;
                tankCheck.TankVolume = tank.GetTankVolume(tank.FuelLevel);
                tankCheck.TransactionDate = DateTime.Now;
                tankCheck.FuelDensity = tank.CurrentDensity;
                tankChecks.Add(tankCheck);
            }
            return tankChecks.ToArray();
        }

        //private decimal GetStartLevel(Data.DatabaseModel database, DateTime dt, Guid tankId)
        //{
        //    Data.TankLevelStartView startLevel = database.TankLevelStartViews.Where(t => t.TansDate >= dt && t.TankId == tankId).OrderBy(t => t.TansDate).FirstOrDefault();
        //    if (startLevel != null)
        //        return startLevel.Level.Value;
        //    Data.TankLevelEndView endLevel = database.TankLevelEndViews.Where(t => t.TansDate <= dt && t.TankId == tankId).OrderBy(t => t.TansDate).LastOrDefault();
        //    if (endLevel == null)
        //        return 0;
        //    return endLevel.Level.Value;
        //}

        //private decimal GetEndLevel(Data.DatabaseModel database, DateTime dt, Guid tankId)
        //{
        //    Data.TankLevelEndView endLevel = database.TankLevelEndViews.Where(t => t.TansDate <= dt && t.TankId == tankId).OrderBy(t => t.TansDate).LastOrDefault();
        //    if (endLevel == null)
        //        return 0;
        //    return endLevel.Level.Value;
        //}

        //private Data.Balance CreateBalance(Data.DatabaseModel database, DateTime dt1, DateTime dt2)
        //{
        //    Guid literCheckType = Data.Implementation.OptionHandler.Instance.GetGuidOption("LiterCheckInvoiceType", Guid.Empty);
        //    Guid deliveryType = Data.Implementation.OptionHandler.Instance.GetGuidOption("DeliveryCheckInvoiceType", Guid.Empty);

        //    database.RefreshData();
        //    Communication.BalanceClass balance = new Communication.BalanceClass();
        //    balance.Date = dt1;
        //    balance.TimeStart = dt1;
        //    balance.TimeEnd = dt2;
        //    //period.PeriodEnd = dt2;
        //    balance.Reservoirs = new ASFuelControl.Communication.ReservoirsClass();
        //    balance.Reservoirs.Reservoirs = new List<ASFuelControl.Communication.ReservoirClass>();

        //    balance.PumpsPerFuel = new ASFuelControl.Communication.FuelTypeBalances();
        //    balance.PumpsPerFuel.FuelTypes = new List<ASFuelControl.Communication.FuelTypeClass>();

        //    balance.Movements = new ASFuelControl.Communication.FuelMovementsClass();
        //    balance.Movements.FuelMovements = new List<ASFuelControl.Communication.FuelMovementClass>();

        //    balance.Divergences = new ASFuelControl.Communication.FuelTypeDivsClass();
        //    balance.Divergences.Divergences = new List<ASFuelControl.Communication.FuelTypeDivClass>();

        //    Dictionary<Data.Tank, decimal> fillingsDelivery = new Dictionary<Data.Tank, decimal>();
        //    Dictionary<Data.Tank, decimal> fillingsDelivery15 = new Dictionary<Data.Tank, decimal>();
        //    Dictionary<Data.Tank, decimal> fillingRest = new Dictionary<Data.Tank, decimal>();
        //    Dictionary<Data.Tank, decimal> fillingRest15 = new Dictionary<Data.Tank, decimal>();
        //    Dictionary<Data.Tank, decimal> invoicedDeliveries = new Dictionary<Data.Tank, decimal>();
        //    Dictionary<Data.Tank, decimal> invoicedDeliveries15 = new Dictionary<Data.Tank, decimal>();

        //    foreach (Data.Tank tank in database.Tanks.OrderBy(t => t.TankNumber))
        //    {
        //        ASFuelControl.Communication.ReservoirClass reservoir = new ASFuelControl.Communication.ReservoirClass();

        //        decimal strartLevel = this.GetStartLevel(database, dt1, tank.TankId);
        //        decimal endLevel = this.GetEndLevel(database, dt2, tank.TankId);

        //        reservoir.Capacity = tank.TotalVolume;
        //        reservoir.FuelType = (Communication.Enums.FuelTypeEnum)tank.FuelType.EnumeratorValue;
        //        reservoir.LevelStart = strartLevel;                            //F_2232_MM
        //        reservoir.LevelEnd = endLevel;                                  //F_2234_MM
        //        reservoir.TankId = tank.TankNumber;
        //        reservoir.TankSerialNumber = tank.TankSerialNumber;
        //        reservoir.TemperatureStart = tank.GetTempmeratureAtTime(dt1);                 //F_2232_TEMP
        //        reservoir.TemperatureEnd = tank.GetTempmeratureAtTime(dt2);                     //F_2234_TEMP
        //        reservoir.VolumeStart = tank.GetTankVolume(reservoir.LevelStart);                           //F_2232_VOL
        //        reservoir.VolumeEnd = tank.GetTankVolume(reservoir.LevelEnd);                               //F_2234_VOL
        //        reservoir.VolumeStartNormalized = tank.FuelType.NormalizeVolume(reservoir.VolumeStart, reservoir.TemperatureStart, tank.GetDensityAtTime(dt1));   //F_2233
        //        reservoir.VolumeEndNormalized = tank.FuelType.NormalizeVolume(reservoir.VolumeEnd, reservoir.TemperatureEnd, tank.GetDensityAtTime(dt2));           //F_2235

        //        decimal fillingsVol = database.TankFillingInvoiceViews.Where(tfi => tfi.TankId == tank.TankId && tfi.TransactionTimeEnd <= balance.TimeEnd && tfi.TransactionTimeEnd >= balance.TimeStart && tfi.InvoiceTypeId == deliveryType).Sum(tfi => tfi.VolumeReal);
        //        decimal fillingsVol15 = database.TankFillingInvoiceViews.Where(tfi => tfi.TankId == tank.TankId && tfi.TransactionTimeEnd <= balance.TimeEnd && tfi.TransactionTimeEnd >= balance.TimeStart && tfi.InvoiceTypeId == deliveryType).Sum(tfi => tfi.VolumeRealNormalized);
        //        decimal fillingsRestVol = database.TankFillingInvoiceViews.Where(tfi => tfi.TankId == tank.TankId && tfi.TransactionTimeEnd <= balance.TimeEnd && tfi.TransactionTimeEnd >= balance.TimeStart && tfi.InvoiceTypeId != deliveryType && (tfi.InvoiceTypeId != literCheckType || tfi.TransactionType == 1)).Sum(tfi => tfi.VolumeReal);
        //        decimal fillingsRestVol15 = database.TankFillingInvoiceViews.Where(tfi => tfi.TankId == tank.TankId && tfi.TransactionTimeEnd <= balance.TimeEnd && tfi.TransactionTimeEnd >= balance.TimeStart && tfi.InvoiceTypeId != deliveryType && (tfi.InvoiceTypeId != literCheckType || tfi.TransactionType == 1)).Sum(tfi => tfi.VolumeRealNormalized);
        //        decimal invoicedVol = database.TankFillingInvoiceViews.Where(tfi => tfi.TankId == tank.TankId && tfi.TransactionTimeEnd <= balance.TimeEnd && tfi.TransactionTimeEnd >= balance.TimeStart && tfi.InvoiceTypeId == deliveryType).Sum(tfi => tfi.InvoiceVolume);
        //        decimal invoicedVol15 = database.TankFillingInvoiceViews.Where(tfi => tfi.TankId == tank.TankId && tfi.TransactionTimeEnd <= balance.TimeEnd && tfi.TransactionTimeEnd >= balance.TimeStart && tfi.InvoiceTypeId == deliveryType).Sum(tfi => tfi.InvoiceVolumeNormalized);

        //        fillingsDelivery.Add(tank, fillingsVol);
        //        fillingsDelivery15.Add(tank, fillingsVol15);
        //        fillingRest.Add(tank, fillingsRestVol);
        //        fillingRest15.Add(tank, fillingsRestVol15);
        //        invoicedDeliveries.Add(tank, invoicedVol);
        //        invoicedDeliveries15.Add(tank, invoicedVol15);

        //        balance.Reservoirs.Reservoirs.Add(reservoir);
        //    }

        //    if (balance.Reservoirs.Reservoirs.Count == 0)
        //        return null;

        //    List<Data.FuelType> fuelTypes = database.Tanks.Select(t => t.FuelType).Distinct().ToList();
        //    foreach (Data.FuelType ft in fuelTypes)
        //    {
        //        List<Data.Nozzle> nozzles = database.Tanks.Where(t => t.FuelTypeId == ft.FuelTypeId).SelectMany(t => t.NozzleFlows).Select(nf => nf.Nozzle).Distinct().ToList();
        //        if (nozzles.Count == 0)
        //            continue;
        //        ASFuelControl.Communication.FuelTypeClass ftc = new ASFuelControl.Communication.FuelTypeClass();
        //        ftc.FuelPumps = new List<ASFuelControl.Communication.FuelTypePumpClass>();
        //        ftc.FuelType = (Communication.Enums.FuelTypeEnum)ft.EnumeratorValue;
        //        var qst = ft.Nozzles.SelectMany(n => n.SalesTransactions).Where(st => st.TransactionTimeStamp <= balance.TimeEnd && st.TransactionTimeStamp >= balance.TimeStart);
        //        foreach (Data.Nozzle n in nozzles)
        //        {
        //            var qs = n.SalesTransactions.Where(s => s.TransactionTimeStamp <= balance.TimeEnd && s.TransactionTimeStamp >= balance.TimeStart);
        //            if (qs.Count() == 0)
        //                continue;

        //            ASFuelControl.Communication.FuelTypePumpClass ftpc = new ASFuelControl.Communication.FuelTypePumpClass();
        //            ftpc.FuelPumpId = n.Dispenser.OfficialPumpNumber.ToString();
        //            ftpc.FuelPumpSerialNumber = n.Dispenser.PumpSerialNumber;
        //            ftpc.FuelType = (Communication.Enums.FuelTypeEnum)ft.EnumeratorValue;
        //            ftpc.TotalizerStart = n.GetTotalizerStartAtTime(balance.TimeStart);             //F_2242
        //            ftpc.TotalizerEnd = n.GetTotalizerEndAtTime(balance.TimeEnd);                   //F_2243

        //            ftpc.TotalLiterCheck = n.GetLitercheckSum(balance.TimeStart, balance.TimeEnd);  //F_2244D
        //            ftpc.TotalLiterCheckNormalized = n.GetLitercheckSum(balance.TimeStart, balance.TimeEnd);  //F_2244D
        //            ftpc.TotalSales = qs.Sum(s => s.Volume) - ftpc.TotalLiterCheck;                 //F_2244A
        //            ftpc.TotalSalesNormalized = qs.Sum(s => s.VolumeNormalized) - ftpc.TotalLiterCheckNormalized;
        //            //ftpc.TotalOut = qs.Sum(s => s.Volume) + ftpc.TotalLiterCheck;                 //F_2244B
        //            //ftpc.TotalOutNormalized = qs.Sum(s => s.VolumeNormalized);                    //F_2244C

        //            ftpc.TotalizerDifference = ftpc.TotalizerEnd - ftpc.TotalizerStart;             //F_2245A
        //            ftpc.NozzleId = n.OfficialNozzleNumber;

        //            var qd = n.Dispenser.Nozzles.Where(nn => nn.FuelTypeId == n.FuelTypeId);
        //            var qsd = qd.SelectMany(nn => nn.SalesTransactions).Where(s => s.TransactionTimeStamp <= balance.TimeEnd && s.TransactionTimeStamp >= balance.TimeStart);

        //            //ftpc.SumTotalOut = qs.Sum(s => s.Volume);// qsd.Sum(s => s.Volume);                                                      //F_2244E
        //            //ftpc.SumTotalOutNormalized = qs.Sum(s => s.VolumeNormalized); // qsd.Sum(s => s.VolumeNormalized);                                  //F_2244F
        //            //ftpc.SumTotalOutTotalizer =  ftpc.TotalizerEnd - ftpc.TotalizerStart;                            //F_2245A   qs.Sum(s => s.TotalizerEnd - s.TotalizerStart);
        //            //ftpc.SumTotalOutTotalizerNormalized = qsd.Sum(s => s.TotalizerEnd - s.TotalizerStart);          //F_2245B

        //            // ********* ftpc.SumTotalOutTotalizerNormalized *************//
        //            var qt = n.SalesTransactions.SelectMany(s => s.TankSales).Select(ts => ts.Tank).Distinct();
        //            Data.Tank tank = qt.First();
        //            ftpc.TankSerialNumber = tank.TankSerialNumber;
        //            ftc.FuelPumps.Add(ftpc);
        //        }
        //        var qsdiff = ft.Nozzles.SelectMany(n => n.SalesTransactions).Where(s => s.TransactionTimeStamp <= balance.TimeEnd && s.TransactionTimeStamp >= balance.TimeStart);
        //        ftc.SumTotalizerDifference = ftc.FuelPumps.Sum(f => f.TotalizerEnd - f.TotalizerStart);                                             //F_2245B
        //        ftc.SumTotalizerDifferenceNormalized = ft.NormalizeVolume(ftc.SumTotalizerDifference, database.GetAvgTemperature(qsdiff), database.GetAvgDensity(qsdiff, ft));//F_2245C
        //        ftc.TotalPumpsNumber = ftc.FuelPumps.Count;
        //        foreach (ASFuelControl.Communication.FuelTypePumpClass ftpc in ftc.FuelPumps)
        //        {
        //            Data.FuelType fuelType = fuelTypes.Where(f => f.EnumeratorValue == (int)ftpc.FuelType).FirstOrDefault();
        //            if (fuelType == null)
        //                continue;
        //            ftpc.TotalOut = ftc.FuelPumps.Where(fp => fp.FuelType == ftc.FuelType).Sum(fp => fp.TotalSales);                                //F_2244B 
        //            ftpc.TotalOutNormalized = ftc.FuelPumps.Where(fp => fp.FuelType == ftc.FuelType).Sum(fp => fp.TotalSalesNormalized);            //F_2244C
        //            ftpc.SumTotalOut = ftc.FuelPumps.Where(fp => fp.FuelType == ftc.FuelType).Sum(fp => fp.TotalLiterCheck);                        //F_2244E 
        //            ftpc.SumTotalOutNormalized = ftc.FuelPumps.Where(fp => fp.FuelType == ftc.FuelType).Sum(fp => fp.TotalLiterCheckNormalized);    //F_2244F
        //        }
        //        balance.PumpsPerFuel.FuelTypes.Add(ftc);
        //    }
        //    foreach (Data.FuelType ft in fuelTypes)
        //    {
        //        ASFuelControl.Communication.FuelMovementClass mov = new ASFuelControl.Communication.FuelMovementClass();
        //        mov.FuelType = (Communication.Enums.FuelTypeEnum)ft.EnumeratorValue;

        //        //var qFill = database.TankFillings.Where(tf => tf.TransactionTimeEnd >= balance.TimeStart && tf.TransactionTime <= balance.TimeEnd && tf.Tank.FuelTypeId == ft.FuelTypeId).ToList();
        //        //var qInvFillings = database.InvoiceLines.Where(il => il.FuelTypeId == ft.FuelTypeId && il.Invoice.InvoiceTypeId == deliveryType && il.TankFilling.TransactionTime <= balance.TimeEnd && il.TankFilling.TransactionTime >= balance.TimeStart && !il.TankFillingId.HasValue);
        //        //var qInvOther = qFill.SelectMany(tf => tf.InvoiceLines).Where(il => il.FuelTypeId == ft.FuelTypeId && il.Invoice.InvoiceTypeId == literCheckType || (il.Invoice.InvoiceTypeId != deliveryType && il.Invoice.InvoiceType.TransactionType == 1) && il.TankFilling.TransactionTime <= balance.TimeEnd && il.TankFilling.TransactionTime >= balance.TimeStart);

        //        mov.SumIn = fillingsDelivery.Where(t => t.Key.FuelTypeId == ft.FuelTypeId).Sum(t => t.Value);                           //F_2236A1
        //        mov.SumAdditionalIn = fillingRest.Where(t => t.Key.FuelTypeId == ft.FuelTypeId).Sum(t => t.Value);                      //F_2236A2
        //        mov.SumInNormalized = fillingsDelivery15.Where(t => t.Key.FuelTypeId == ft.FuelTypeId).Sum(t => t.Value);               //F_2236B1
        //        mov.SumInInvoicedNormalized = fillingRest15.Where(t => t.Key.FuelTypeId == ft.FuelTypeId).Sum(t => t.Value);            //F_2236B2
        //        mov.SumInInvoiced = invoicedDeliveries.Where(t => t.Key.FuelTypeId == ft.FuelTypeId).Sum(t => t.Value);                 //F_2237
        //        mov.SumInInvoicedNormalized = invoicedDeliveries15.Where(t => t.Key.FuelTypeId == ft.FuelTypeId).Sum(t => t.Value);     //F_2238
        //        mov.Diff = mov.SumIn - mov.SumInInvoiced;                                                                               //F_2239A
        //        mov.DiffNormalized = mov.SumInNormalized - mov.SumInInvoicedNormalized;                                                 //F_2239B

        //        mov.DaylyMove = balance.Reservoirs.Reservoirs.Where(t => t.FuelType == mov.FuelType).Sum(t => t.VolumeStart - t.VolumeEnd) + mov.SumIn + mov.SumAdditionalIn; //F_22310 
        //        mov.DaylyMoveNormalized = balance.Reservoirs.Reservoirs.Where(t => t.FuelType == mov.FuelType).Sum(t => t.VolumeStartNormalized - t.VolumeEndNormalized) + mov.SumInNormalized + mov.SumAdditionalInNormalized;

        //        balance.Movements.FuelMovements.Add(mov);

        //        ASFuelControl.Communication.FuelTypeDivClass div = new ASFuelControl.Communication.FuelTypeDivClass();
        //        div.FuelType = (Communication.Enums.FuelTypeEnum)ft.EnumeratorValue;
        //        div.Divergence = mov.DaylyMove - balance.PumpsPerFuel.FuelTypes.Where(f => f.FuelType == mov.FuelType).SelectMany(f => f.FuelPumps).Sum(fp => fp.TotalSales - fp.TotalLiterCheck);
        //        div.DivergenceNormalized = mov.DaylyMoveNormalized - balance.PumpsPerFuel.FuelTypes.Where(f => f.FuelType == mov.FuelType).SelectMany(f => f.FuelPumps).Sum(fp => fp.TotalSalesNormalized - fp.TotalLiterCheckNormalized);
        //        if (mov.DaylyMove != 0)
        //            div.Percentage = 100 * (div.Divergence / mov.DaylyMove);
        //        else
        //            div.Percentage = 0;

        //        if (mov.DaylyMoveNormalized != 0)
        //            div.PercentageNormalized = 100 * (div.DivergenceNormalized / mov.DaylyMoveNormalized);
        //        else
        //            div.PercentageNormalized = 0;

        //        balance.Divergences.Divergences.Add(div);
        //    }
        //    System.Xml.Serialization.XmlSerializer ser = new System.Xml.Serialization.XmlSerializer(typeof(Communication.BalanceClass));
        //    System.IO.StringWriter textWriter = new System.IO.StringWriter();
        //    ser.Serialize(textWriter, balance);
        //    string data = textWriter.ToString();
        //    textWriter.Close();
        //    textWriter.Dispose();
        //    Data.Balance bal = new Data.Balance();// this.database.CreateEntity<Data.Balance>();
        //    bal.BalanceId = Guid.NewGuid();
        //    database.Add(bal);
        //    bal.BalanceText = data;
        //    bal.StartDate = balance.TimeStart;
        //    bal.EndDate = balance.TimeEnd;
        //    bal.ApplicationUserId = Data.DatabaseModel.CurrentUserId;
        //    Data.TankFilling lastFil = database.TankFillings.Where(f => f.TransactionTime <= balance.TimeEnd).OrderBy(f => f.TransactionTimeEnd).LastOrDefault();
        //    if (lastFil != null)
        //        bal.LastFilling = lastFil.TankFillingId;

        //    return bal;
        //}

        private bool IsStartOfDay()
        {
            if (DateTime.Now.Hour == 0 && DateTime.Now.Minute < 59)
                return true;
            return false;
        }

        private Data.Balance CheckForBalance()
        {
            if (!this.IsStartOfDay())
                return null;

            DateTime dtStart = DateTime.Today.AddDays(-1);
            DateTime dtEnd = dtStart.AddDays(1).AddMilliseconds(-1);
            DateTime checkDate = dtStart.AddHours(12);
            Data.Balance lastBalance = this.database.Balances.OrderByDescending(b => b.EndDate).FirstOrDefault();

            if (lastBalance != null && lastBalance.EndDate >= checkDate && lastBalance.StartDate <= checkDate)
                return null;

            if (lastBalance != null)
            {
                dtStart = lastBalance.EndDate;
                dtEnd = dtStart.Date.AddDays(1).AddMilliseconds(-1);
            }
            Data.UsagePeriod period = null;

            if (lastBalance != null)
            {
                period = this.database.GetCreateUsagePeriod(dtStart);
            }
            else
            {
                period = this.database.GetUsagePeriod();
            }

            return Data.Balance.CreateBalance(this.database, dtStart, dtEnd);
            
            //if (!period.PeriodEnd.HasValue && DateTime.Now.Subtract(period.PeriodStart).TotalDays < 1)
            //    return;

            //this.database.RefreshData();
            //Communication.BalanceClass balance = new Communication.BalanceClass();
            //balance.Date = DateTime.Now;
            //balance.TimeStart = period.PeriodStart;
            //balance.TimeEnd = DateTime.Now;
            //period.PeriodEnd = DateTime.Now;
            //balance.Reservoirs = new ASFuelControl.Communication.ReservoirsClass();
            //balance.Reservoirs.Reservoirs = new List<ASFuelControl.Communication.ReservoirClass>();

            //balance.PumpsPerFuel = new ASFuelControl.Communication.FuelTypeBalances();
            //balance.PumpsPerFuel.FuelTypes = new List<ASFuelControl.Communication.FuelTypeClass>();

            //balance.Movements = new ASFuelControl.Communication.FuelMovementsClass();
            //balance.Movements.FuelMovements = new List<ASFuelControl.Communication.FuelMovementClass>();

            //balance.Divergences = new ASFuelControl.Communication.FuelTypeDivsClass();
            //balance.Divergences.Divergences = new List<ASFuelControl.Communication.FuelTypeDivClass>();

            //foreach (Data.Tank tank in this.database.Tanks)
            //{
            //    var q = tank.TankFillings.Where(tf => tf.TransactionTimeEnd >= balance.TimeStart && tf.TransactionTimeEnd <= balance.TimeEnd);
            //    ASFuelControl.Communication.ReservoirClass reservoir = new ASFuelControl.Communication.ReservoirClass();
            //    reservoir.Capacity = tank.TotalVolume;
            //    reservoir.FuelType = (Communication.Enums.FuelTypeEnum)tank.FuelType.EnumeratorValue;
            //    reservoir.LevelStart = tank.GetLevelAtTime(balance.TimeStart);                              //F_2232_MM
            //    reservoir.LevelEnd = tank.GetLevelAtTime(balance.TimeEnd);                                  //F_2234_MM
            //    reservoir.TankId = tank.TankNumber;
            //    reservoir.TankSerialNumber = tank.TankSerialNumber;
            //    reservoir.TemperatureStart = tank.GetTempmeratureAtTime(balance.TimeStart);                 //F_2232_TEMP
            //    reservoir.TemperatureEnd = tank.GetTempmeratureAtTime(balance.TimeEnd);                     //F_2234_TEMP
            //    reservoir.VolumeStart = tank.GetTankVolume(reservoir.LevelStart);                           //F_2232_VOL
            //    reservoir.VolumeEnd = tank.GetTankVolume(reservoir.LevelEnd);                               //F_2234_VOL
            //    reservoir.VolumeStartNormalized = tank.FuelType.NormalizeVolume(reservoir.VolumeStart, reservoir.TemperatureStart, tank.GetDensityAtTime(balance.TimeStart));   //F_2233
            //    reservoir.VolumeEndNormalized = tank.FuelType.NormalizeVolume(reservoir.VolumeEnd, reservoir.TemperatureEnd, tank.GetDensityAtTime(balance.TimeEnd));           //F_2235
            //    balance.Reservoirs.Reservoirs.Add(reservoir);
            //}
            //List<Data.FuelType> fuelTypes = this.database.Tanks.Select(t => t.FuelType).Distinct().ToList();
            //foreach (Data.FuelType ft in fuelTypes)
            //{
            //    List<Data.Nozzle> nozzles = this.database.Tanks.Where(t => t.FuelTypeId == ft.FuelTypeId).SelectMany(t => t.NozzleFlows).Select(nf => nf.Nozzle).Distinct().ToList();
            //    if (nozzles.Count == 0)
            //        continue;
            //    ASFuelControl.Communication.FuelTypeClass ftc = new ASFuelControl.Communication.FuelTypeClass();
            //    ftc.FuelPumps = new List<ASFuelControl.Communication.FuelTypePumpClass>();
            //    ftc.FuelType = (Communication.Enums.FuelTypeEnum)ft.EnumeratorValue;
            //    var qst = ft.Nozzles.SelectMany(n => n.SalesTransactions).Where(st => st.TransactionTimeStamp <= balance.TimeEnd && st.TransactionTimeStamp >= balance.TimeStart);
            //    Dictionary<string, decimal> f_2245b = new Dictionary<string, decimal>();
            //    foreach (Data.Nozzle n in nozzles)
            //    {
            //        var qs = n.SalesTransactions.Where(s => s.TransactionTimeStamp <= balance.TimeEnd && s.TransactionTimeStamp >= balance.TimeStart);
            //        if (qs.Count() == 0)
            //            continue;

            //        ASFuelControl.Communication.FuelTypePumpClass ftpc = new ASFuelControl.Communication.FuelTypePumpClass();
            //        ftpc.FuelPumpId = n.Dispenser.OfficialPumpNumber.ToString();
            //        ftpc.FuelPumpSerialNumber = n.Dispenser.PumpSerialNumber;
            //        ftpc.FuelType = (Communication.Enums.FuelTypeEnum)ft.EnumeratorValue;
            //        ftpc.TotalizerStart = n.GetTotalizerStartAtTime(balance.TimeStart);             //F_2242
            //        ftpc.TotalizerEnd = n.GetTotalizerEndAtTime(balance.TimeEnd);                   //F_2243

            //        ftpc.TotalSales = qs.Sum(s => s.Volume);                                        //F_2244A
            //        ftpc.TotalLiterCheck = n.GetLitercheckSum(balance.TimeStart, balance.TimeEnd);  //F_2244D
            //        ftpc.TotalOut = qs.Sum(s => s.Volume) + ftpc.TotalLiterCheck;                   //F_2244B
            //        ftpc.TotalOutNormalized = qs.Sum(s => s.VolumeNormalized);                      //F_2244C

            //        ftpc.TotalizerDifference = ftpc.TotalizerEnd - ftpc.TotalizerStart;
            //        ftpc.NozzleId = n.OfficialNozzleNumber;

            //        var qd = n.Dispenser.Nozzles.Where(nn => nn.FuelTypeId == n.FuelTypeId);
            //        var qsd = qd.SelectMany(nn => nn.SalesTransactions).Where(s => s.TransactionTimeStamp <= balance.TimeEnd && s.TransactionTimeStamp >= balance.TimeStart);

            //        ftpc.SumTotalOut = qsd.Sum(s => s.Volume);                                                      //F_2244E
            //        ftpc.SumTotalOutNormalized = qsd.Sum(s => s.VolumeNormalized);                                  //F_2244F

            //        ftpc.SumTotalOutTotalizer = ftpc.TotalizerEnd - ftpc.TotalizerStart;                            //F_2245A   qs.Sum(s => s.TotalizerEnd - s.TotalizerStart);
            //        if (!f_2245b.ContainsKey(n.Dispenser.PumpSerialNumber))
            //            f_2245b.Add(n.Dispenser.PumpSerialNumber, ftpc.TotalizerEnd - ftpc.TotalizerStart);
            //        else
            //            f_2245b[n.Dispenser.PumpSerialNumber] = f_2245b[n.Dispenser.PumpSerialNumber] + ftpc.TotalizerEnd - ftpc.TotalizerStart;

            //        ftpc.SumTotalOutTotalizerNormalized = qsd.Sum(s => s.TotalizerEnd - s.TotalizerStart);          //F_2245B

            //        // ********* ftpc.SumTotalOutTotalizerNormalized *************//
            //        var qt = n.SalesTransactions.SelectMany(s => s.TankSales).Select(ts => ts.Tank).Distinct();
            //        Data.Tank tank = qt.First();
            //        ftpc.TankSerialNumber = tank.TankSerialNumber;
            //        ftc.FuelPumps.Add(ftpc);
            //    }

            //    var qGroups = ftc.FuelPumps.GroupBy(fp => fp.FuelPumpSerialNumber).Select(g => new
            //        GroupSum
            //    {
            //        PumpSerialNumber = g.Key,
            //        TotalIn = g.Sum(f => f.TotalizerEnd - f.TotalizerStart),
            //        TotalInNormalized = 0,
            //        TotalOut = g.Sum(f => f.TotalOut),
            //        TotalOutNormalized = g.Sum(f => f.TotalOutNormalized)
            //    });

            //    foreach (ASFuelControl.Communication.FuelTypePumpClass ftpc in ftc.FuelPumps)
            //    {
            //        GroupSum gr = qGroups.Where(g => g.PumpSerialNumber == ftpc.FuelPumpSerialNumber).FirstOrDefault();
            //        if (gr == null)
            //            continue;
            //        Data.Nozzle nozzle = this.database.Nozzles.Where(n => n.Dispenser.OfficialPumpNumber == int.Parse(ftpc.FuelPumpId) && n.Dispenser.PumpSerialNumber == ftpc.FuelPumpSerialNumber && n.OfficialNozzleNumber == ftpc.NozzleId).FirstOrDefault();
            //        var qd = nozzle.Dispenser.Nozzles.Where(nn => nn.FuelTypeId == nozzle.FuelTypeId);
            //        var qsd = qd.SelectMany(nn => nn.SalesTransactions).Where(s => s.TransactionTimeStamp <= balance.TimeEnd && s.TransactionTimeStamp >= balance.TimeStart);
            //        decimal temp = nozzle.GetAvgTemperature(qsd);
            //        decimal density = nozzle.GetAvgDensity(qsd);
            //        ftpc.SumTotalOutTotalizer = f_2245b[ftpc.FuelPumpSerialNumber];
            //        ftpc.SumTotalOutTotalizerNormalized = nozzle.FuelType.NormalizeVolume(ftpc.SumTotalOutTotalizer, temp, density);
            //    }
            //    if (ftc.FuelPumps.Count > 0)
            //    {
            //        var qsdiff = ft.Nozzles.SelectMany(n => n.SalesTransactions).Where(s => s.TransactionTimeStamp <= balance.TimeEnd && s.TransactionTimeStamp >= balance.TimeStart);
            //        ftc.SumTotalizerDifference = qsdiff.Sum(s => s.TotalizerEnd - s.TotalizerStart);
            //        ftc.SumTotalizerDifferenceNormalized = ft.NormalizeVolume(ftc.SumTotalizerDifference, this.database.GetAvgTemperature(qsdiff), this.database.GetAvgDensity(qsdiff, ft));

            //    }
            //    ftc.TotalPumpsNumber = ftc.FuelPumps.Count;

            //    balance.PumpsPerFuel.FuelTypes.Add(ftc);
            //    ASFuelControl.Communication.FuelMovementClass mov = new ASFuelControl.Communication.FuelMovementClass();
            //    mov.FuelType = (Communication.Enums.FuelTypeEnum)ft.EnumeratorValue;

            //    //var qInvOther = this.database.InvoiceLines.Where(il => il.Invoice.InvoiceTypeId == literCheckType || (il.Invoice.InvoiceTypeId != deliveryType && il.Invoice.InvoiceType.TransactionType == 1)).
            //    //    Select(il => il.TankFilling).Where(tf => tf.TransactionTimeEnd >= balance.TimeStart && tf.TransactionTime <= balance.TimeEnd);

            //    var qFill = this.database.TankFillings.Where(tf => tf.TransactionTimeEnd >= balance.TimeStart && tf.TransactionTime <= balance.TimeEnd && tf.Tank.FuelTypeId == ft.FuelTypeId).ToList();
            //    var qInvFillings = qFill.SelectMany(tf => tf.InvoiceLines).Where(il => il.Invoice.InvoiceTypeId == deliveryType);
            //    var qInvOther = qFill.SelectMany(tf => tf.InvoiceLines).Where(il => il.Invoice.InvoiceTypeId == literCheckType || (il.Invoice.InvoiceTypeId != deliveryType && il.Invoice.InvoiceType.TransactionType == 1));
            //    //var qInvFillings = this.database.InvoiceLines.Where(il => il.Invoice.InvoiceTypeId == deliveryType).ToList();
            //    //var qInvFillings1 = qInvFillings.Select(il => il.TankFilling).ToList();
            //    //var qInvFillings2 = qInvFillings1.Where(tf => tf.TransactionTimeEnd >= balance.TimeStart && tf.TransactionTime <= balance.TimeEnd).ToList();


            //    mov.SumIn = qFill.Sum(tf => tf.Volume);                                                                  //F_2236A1
            //    mov.SumAdditionalIn = qInvOther.Sum(tf => tf.Volume);                                                    //F_2236A2
            //    mov.SumInNormalized = qFill.Sum(tf => tf.VolumeNormalized);                                        //F_2236B1
            //    mov.SumAdditionalInNormalized = qInvOther.Sum(tf => tf.Volume);                                          //F_2236B2
            //    mov.SumInInvoiced = qInvFillings.Sum(il => il.Volume);                                                   //F_2237
            //    mov.SumInInvoicedNormalized = qInvFillings.Sum(il => il.VolumeNormalized);                               //F_2238
            //    mov.Diff = mov.SumIn - mov.SumInInvoiced;                                                                       //F_2239A
            //    mov.DiffNormalized = mov.SumInNormalized - mov.SumInInvoicedNormalized;                                   //F_2239B

            //    mov.DaylyMove = balance.Reservoirs.Reservoirs.Where(t => t.FuelType == mov.FuelType).Sum(t => t.VolumeStart - t.VolumeEnd) + mov.SumIn + mov.SumAdditionalIn;
            //    mov.DaylyMoveNormalized = balance.Reservoirs.Reservoirs.Where(t => t.FuelType == mov.FuelType).Sum(t => t.VolumeStartNormalized - t.VolumeEndNormalized) + mov.SumInNormalized + mov.SumAdditionalInNormalized;

            //    balance.Movements.FuelMovements.Add(mov);

            //    decimal sumOut = ftc.FuelPumps.Where(f => (int)f.FuelType == ft.EnumeratorValue).Sum(f => f.SumTotalOut);
            //    decimal sumOutNormal = ftc.FuelPumps.Where(f => (int)f.FuelType == ft.EnumeratorValue).Sum(f => f.SumTotalOutNormalized);

            //    ASFuelControl.Communication.FuelTypeDivClass div = new ASFuelControl.Communication.FuelTypeDivClass();
            //    div.FuelType = (Communication.Enums.FuelTypeEnum)ft.EnumeratorValue;
            //    div.Divergence = mov.DaylyMove - sumOut;
            //    div.DivergenceNormalized = mov.DaylyMoveNormalized - sumOutNormal;
            //    if (mov.DaylyMove != 0)
            //        div.Percentage = 100 * (div.Divergence / mov.DaylyMove);
            //    if (mov.DaylyMoveNormalized != 0)
            //        div.PercentageNormalized = 100 * (div.DivergenceNormalized / mov.DaylyMoveNormalized);
            //    if (ftc.FuelPumps.Count > 0)
            //        balance.Divergences.Divergences.Add(div);
            //}

            //System.Xml.Serialization.XmlSerializer ser = new System.Xml.Serialization.XmlSerializer(typeof(Communication.BalanceClass));
            //System.IO.StringWriter textWriter = new System.IO.StringWriter();
            //ser.Serialize(textWriter, balance);
            //string data = textWriter.ToString();
            //textWriter.Close();
            //textWriter.Dispose();
            //Data.Balance bal = new Data.Balance();// this.database.CreateEntity<Data.Balance>();
            //bal.BalanceId = Guid.NewGuid();
            //this.database.Add(bal);
            //bal.BalanceText = data;
            //bal.StartDate = balance.TimeStart;
            //bal.EndDate = balance.TimeEnd;
            //bal.ApplicationUserId = Data.DatabaseModel.CurrentUserId;
            //Data.TankFilling lastFil = this.database.TankFillings.OrderBy(f => f.TransactionTimeEnd).LastOrDefault();
            //if (lastFil != null)
            //    bal.LastFilling = lastFil.TankFillingId;

            //Data.SalesTransaction lastSale = this.database.SalesTransactions.OrderBy(f => f.TransactionTimeStamp).LastOrDefault();
            //if (lastSale != null)
            //    bal.LastSale = lastSale.SalesTransactionId;

            //return bal;
        }

        private class GroupSum
        {
            public string PumpSerialNumber { set; get; }
            public decimal TotalOut { set; get; }
            public decimal TotalOutNormalized { set; get; }
            public decimal TotalIn { set; get; }
            public decimal TotalInNormalized { set; get; }
        }

        private void CheckForSend()
        {
            try
            {
                this.LastAction = DateTime.Now;
                int interval = this.tankCheckInterval;
                DateTime dt = DateTime.Now;
                try
                {
                    if ((int)dt.TimeOfDay.TotalMinutes % interval == 0 && dt.Subtract(this.lastTankCheck).TotalMinutes > interval)
                    {
                        this.lastTankCheck = dt;
                        Communication.TankCheckClass[] tankChecks = this.CreateTankCheck();
                        foreach (Communication.TankCheckClass tc in tankChecks)
                        {
                            this.SendObject(tc);
                            System.Threading.Thread.Sleep(1000);
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.Instance.LogToFile("CheckForSend:TankCheck", e);
                }
                database.Refresh(Telerik.OpenAccess.RefreshMode.OverwriteChangesFromStore, database.SystemEvents.Where(se => !se.SentDate.HasValue));
                var q = database.SystemEvents.Where(se => !se.SentDate.HasValue).ToList();
                try
                {
                    foreach (Data.SystemEvent se in q)
                    {
                        if(this.SendObject(se))
                            se.SentDate = DateTime.Now;
                        System.Threading.Thread.Sleep(1000);
                    }
                }
                catch (Exception e)
                {
                    Logger.Instance.LogToFile("CheckForSend:Alarm", e);
                }
                database.Refresh(Telerik.OpenAccess.RefreshMode.OverwriteChangesFromStore, database.TankFillings.Where(tf => !tf.SentDateTime.HasValue));
                var q1 = database.TankFillings.Where(se => !se.SentDateTime.HasValue).ToList();
                try
                {
                    foreach (Data.TankFilling se in q1)
                    {
                        object obj = this.CreateDeliveryNote(se);
                        if(this.SendObject(obj))
                            se.SentDateTime = DateTime.Now;
                        System.Threading.Thread.Sleep(1000);
                    }
                }
                catch (Exception e)
                {
                    Logger.Instance.LogToFile("CheckForSend:Delivery", e);
                }
                database.Refresh(Telerik.OpenAccess.RefreshMode.OverwriteChangesFromStore, database.SalesTransactions.Where(tf => !tf.SentDateTime.HasValue));
                var q2 = database.SalesTransactions.Where(se => !se.SentDateTime.HasValue).ToList();

                try
                {
                    foreach (Data.SalesTransaction se in q2)
                    {
                        Communication.IncomeRecieptClass income = this.CreateSale(se);
                        if(this.SendObject(income))
                            se.SentDateTime = DateTime.Now;
                        System.Threading.Thread.Sleep(1000);
                    }
                }
                catch (Exception e)
                {
                    Logger.Instance.LogToFile("CheckForSend:SalesTransaction", e);
                }
                database.Refresh(Telerik.OpenAccess.RefreshMode.OverwriteChangesFromStore, database.FuelTypePrices.Where(tf => !tf.SentDateTime.HasValue));
                var q4 = database.FuelTypePrices.Where(se => !se.SentDateTime.HasValue).ToList();
                try
                {
                    foreach (Data.FuelTypePrice ftp in q4)
                    {
                        ChangePriceClass cpc = new ChangePriceClass();
                        cpc.ChangeTime = ftp.ChangeDate;
                        cpc.FuelType = (Communication.Enums.FuelTypeEnum)ftp.FuelType.EnumeratorValue;
                        cpc.Price = ftp.Price;

                        if(this.SendObject(cpc))
                            ftp.SentDateTime = DateTime.Now;
                        System.Threading.Thread.Sleep(1000);
                    }
                }
                catch (Exception e)
                {
                    Logger.Instance.LogToFile("CheckForSend:ChangePrice", e);
                }

                try
                {
                    Data.Balance balance = this.CheckForBalance();
                    if (balance != null)
                    {

                        System.Xml.Serialization.XmlSerializer ser = new System.Xml.Serialization.XmlSerializer(typeof(Communication.BalanceClass));
                        using (TextReader reader = new StringReader(balance.BalanceText))
                        {
                            object result = ser.Deserialize(reader);
                            if(this.SendObject(result))
                                balance.SentDateTime = DateTime.Now;
                            System.Threading.Thread.Sleep(1000);
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.Instance.LogToFile("CheckForSend:Balance", e);
                }
                var q3 = database.Balances.Where(se => !se.SentDateTime.HasValue).ToList();
                try
                {
                    foreach (Data.Balance bal in q3)
                    {
                        System.Xml.Serialization.XmlSerializer ser = new System.Xml.Serialization.XmlSerializer(typeof(Communication.BalanceClass));
                        using (TextReader reader = new StringReader(bal.BalanceText))
                        {
                            object result = ser.Deserialize(reader);
                            if(this.SendObject(result))
                                bal.SentDateTime = DateTime.Now;
                            System.Threading.Thread.Sleep(1000);
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.Instance.LogToFile("CheckForSend:BalanceSend", e);
                }
                database.SaveChanges();
                System.Threading.Thread.Sleep(1000);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogToFile("CheckForSend", ex);
            }
        }

        private void CheckForPrint()
        {
            if (this.PrintAlert == null)
                return;
            try
            {
                List<Data.SystemEvent> q = this.database.SystemEvents.Where(se => !se.ResolvedDate.HasValue && !se.PrintedDate.HasValue).ToList();
                foreach (Data.SystemEvent ev in q)
                {
                    this.PrintAlert(this, new PrintAlertEventArgs() { AlertId = ev.EventId });
                    ev.PrintedDate = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogToFile("CheckForSend", ex);
                return;
            }

            this.database.SaveChanges();
        }

        DateTime dtAlertPrint = DateTime.Now;

        private void ThreadRun()
        {
            while (!haltThread)
            {
                try
                {
                    this.CheckForSend();
                    if (DateTime.Now.Subtract(dtAlertPrint).TotalSeconds < 20)
                        continue;
                    this.CheckForPrint();
                }
                catch
                {
                }
                if(!haltThread)
                    System.Threading.Thread.Sleep(10000);
            }
            this.CheckForSend();
            System.Threading.Thread.CurrentThread.Abort();
        }

        private bool SendObject(object obj)
        {
            string typeName = "";
            try
            {
                string seriallizedData = "";
                typeName = obj.GetType().Name;
                ClientHeader header = new ClientHeader();
                Communication.CommunicationMethods.CompanyTin = Data.Implementation.OptionHandler.Instance.GetOption("CompanyTIN");
                header.SubmitterTIN = Data.Implementation.OptionHandler.Instance.GetOption("SenderTIN");
                switch (typeName)
                {
                    case "TankCheckClass":
                        TankCheckClass tc = obj as TankCheckClass;
                        seriallizedData = sendMethods.SendTankCheck(header, tc);
                        if (seriallizedData == "[ERROR]" || seriallizedData.Contains("[ERROR]"))
                        {
                            this.LogSend("SendTankCheck", seriallizedData);
                            return false;
                        }
                        this.LogSend("SendTankCheck", seriallizedData);
                        return true;
                    case "AlertClass":
                        AlertClass ac = obj as AlertClass;
                        
                        seriallizedData = sendMethods.SendAlert(header, ac);
                        if (seriallizedData == "[ERROR]" || seriallizedData.Contains("[ERROR]"))
                        {
                            this.LogSend("SendAlert", seriallizedData);
                            return false;
                        }
                        this.LogSend("SendAlert", seriallizedData);
                        return true;
                    case "SystemEvent":
                        Data.SystemEvent se = obj as Data.SystemEvent;
                        if (se.AlertDefinition != null && !se.AlertDefinition.AlertEnumValue.HasValue)
                            return true;
                        AlertClass alert = new AlertClass();
                        if (se.AlertDefinition != null)
                            alert.Alert = (Communication.Enums.AlertIdEnum)se.AlertDefinition.AlertEnumValue.Value;
                        else
                            alert.Alert = (Communication.Enums.AlertIdEnum)se.EventType;

                        alert.AlertTime = se.EventDate;
                        alert.Description = se.Description;
                        if (se.TankId.HasValue)
                            alert.DeviceId = se.Tank.TankSerialNumber;
                        else if (se.NozzleId.HasValue)
                            alert.DeviceId = se.Nozzle.SerialNumber;
                        else if (se.DispenserId.HasValue)
                            alert.DeviceId = se.Dispenser.PumpSerialNumber;

                        bool sendSms = Data.Implementation.OptionHandler.Instance.GetBoolOption("SmsSendAlarm", false);
                        if(sendSms)
                            MailSender.Instance.SendSms(alert.Description);

                        seriallizedData = sendMethods.SendAlert(header, alert);
                        if (seriallizedData == "[ERROR]" || seriallizedData.Contains("[ERROR]"))
                        {
                            this.LogSend("SendAlert", seriallizedData);
                            return false;
                        }
                        this.LogSend("SendAlert", seriallizedData);
                        return true;
                    case "ChangePriceClass":
                        ChangePriceClass cp = obj as ChangePriceClass;
                        seriallizedData = sendMethods.SendChangePrice(header, cp);

                        bool sendPriceChange = Data.Implementation.OptionHandler.Instance.GetBoolOption("SmsSendPriceChange", false);
                        if (sendPriceChange)
                        {
                            string smsText = string.Format("Αλλαγή τιμής {0}. Νέα τιμή : {1:N3}€", cp.FuelType, cp.Price);
                            MailSender.Instance.SendSms(smsText);
                        }
                        if (seriallizedData == "[ERROR]" || seriallizedData.Contains("[ERROR]"))
                        {
                            this.LogSend("SendChangePrice", seriallizedData);
                            return false;
                        }
                        this.LogSend("SendChangePrice", seriallizedData);
                        return true;
                    case "IncomeRecieptClass":
                        IncomeRecieptClass inc = obj as IncomeRecieptClass;
                        seriallizedData = sendMethods.SendIncome(header, inc);
                        if (seriallizedData == "[ERROR]" || seriallizedData.Contains("[ERROR]"))
                        {
                            this.LogSend("SendIncome", seriallizedData);
                            return false;
                        }
                        this.LogSend("SendIncome", seriallizedData);
                        return true;

                    case "DeliveryNoteClass":
                        DeliveryNoteClass del = obj as DeliveryNoteClass;
                        seriallizedData = sendMethods.SendDelivery(header, del);

                        bool sendDelivery = Data.Implementation.OptionHandler.Instance.GetBoolOption("SmsSendDelivery", false);
                        if (sendDelivery)
                        {
                            try
                            {
                                decimal vol1 = del.FuelData[0].TotalVolumeNormalized;
                                decimal vol2 = del.FuelData[0].InvoicedVolumeNormalized;
                                Decimal diff = vol1 - vol2;
                            
                                string smsText = string.Format("Παραλαβή Καυσίμου {0}. Τιμολογημένος όγκος {1:N2}, Πραγματικός όγκος {2:N2}. Διαφορά {3:N2}", del.FuelData[0].FuelType, vol2, vol1, diff);
                                MailSender.Instance.SendSms(smsText);
                            }
                            catch
                            {
                            }
                        }

                        if (seriallizedData == "[ERROR]" || seriallizedData.Contains("[ERROR]"))
                        {
                            this.LogSend("SendDelivery", seriallizedData);
                            return false;
                        }
                        this.LogSend("SendDelivery", seriallizedData);
                        return true;
                    case "LiterCheckClass":
                        LiterCheckClass lc = obj as LiterCheckClass;
                        seriallizedData = sendMethods.SendLiterCheck(header, lc);
                        if (seriallizedData == "[ERROR]" || seriallizedData.Contains("[ERROR]"))
                        {
                            this.LogSend("SendLiterCheck", seriallizedData);
                            return false;
                        }
                        this.LogSend("SendLiterCheck", seriallizedData);
                        return true;
                    case "BalanceClass":
                        BalanceClass bc = obj as BalanceClass;
                        seriallizedData = sendMethods.SendBalance(header, bc);
                        if (seriallizedData == "[ERROR]" || seriallizedData.Contains("[ERROR]"))
                        {
                            this.LogSend("SendBalance", seriallizedData);
                            return false;
                        }
                        this.LogSend("SendBalance", seriallizedData);
                        return true;

                }
                return false;
            }
            catch (Exception ex)
            {
                Logger.Instance.LogToFile("SendObject::" + typeName, ex);
                return false;
            }
        }

        public bool SendSoftwareChange(string version)
        {
            string amdika = Data.Implementation.OptionHandler.Instance.GetOption("AMDIKA");
            bool updated = this.sendMethods.SendSWUpdate(amdika, version);
            return updated;
        }

        private bool LogSend(string action, string data)
        {
            try
            {
                if (!Properties.Settings.Default.LogSend)
                    return true;
                if (!System.IO.Directory.Exists(System.Environment.CurrentDirectory + "\\Logs"))
                    System.IO.Directory.CreateDirectory(System.Environment.CurrentDirectory + "\\Logs");

                string dir = string.Format(System.Environment.CurrentDirectory + "\\Logs\\{1}", DateTime.Now, action);
                if (!System.IO.Directory.Exists(dir))
                    System.IO.Directory.CreateDirectory(dir);
                string fileName = string.Format(System.Environment.CurrentDirectory + "\\Logs\\{1}\\{0:yyyyMMddHHmmssfff}.log", DateTime.Now, action);

                if (data.StartsWith("[ERROR]") || data == "[ERROR]")
                {
                    System.IO.File.AppendAllText(fileName, "==============================================================\r\n");
                    System.IO.File.AppendAllText(fileName, string.Format("Date: {0:dd/MM/yyyy HH:mm:dd.fff}\r\n", DateTime.Now));
                    System.IO.File.AppendAllText(fileName, string.Format("Action: {0}\r\n", action));
                    System.IO.File.AppendAllText(fileName, "--------------------------------------------------------------\r\n");
                    System.IO.File.AppendAllText(fileName, "---------------------------ERROR------------------------------\r\n");
                    System.IO.File.AppendAllText(fileName, data + "\r\n");
                    System.IO.File.AppendAllText(fileName, "==============================================================\r\n");
                    System.IO.File.AppendAllText(fileName, "");
                }
                else
                {
                    System.IO.File.AppendAllText(fileName, "==============================================================\r\n");
                    System.IO.File.AppendAllText(fileName, string.Format("Date: {0:dd/MM/yyyy HH:mm:dd.fff}\r\n", DateTime.Now));
                    System.IO.File.AppendAllText(fileName, string.Format("Action: {0}\r\n", action));
                    System.IO.File.AppendAllText(fileName, "--------------------------------------------------------------\r\n");
                    System.IO.File.AppendAllText(fileName, "--------------------------SENT OK-----------------------------\r\n");
                    System.IO.File.AppendAllText(fileName, data + "\r\n");
                    System.IO.File.AppendAllText(fileName, "==============================================================\r\n");
                    System.IO.File.AppendAllText(fileName, "");
                }
                //Data.SendLog log = new Data.SendLog();
                //log.SendLogId = Guid.NewGuid();
                //log.Action = action;
                //log.SendDate = DateTime.Now;
                //log.SendData = data;
                //this.database.Add(log);
                //this.database.SaveChanges();

                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    public class PrintAlertEventArgs : EventArgs
    {
        public Guid AlertId { set; get; }
    }
}
