using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ASFuelControl.Communication;
using System.IO;
using ASFuelControl.Logging;

namespace ASFuelControl.Windows.Threads
{
    /// <summary>
    /// Class providing functionality for sending data to GGPS 
    /// </summary>
    public class SendAlertsThread
    {
        public event EventHandler<PrintAlertEventArgs> PrintAlert;
        public event EventHandler BalanceCreated;
        public event EventHandler SubmissionFailed;
        public event EventHandler SubmissionSuccess;

        //Data.DatabaseModel database = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
        System.Threading.Thread th = null;
        bool haltThread = false;
        bool isTankCheck = false;
        int tankCheckInterval = 0;
        DateTime lastTankCheck = DateTime.Now.Subtract(TimeSpan.FromMinutes(5));
        DateTime officialStartDate;
        private bool sendData = false;
        private Communication.SendMethods sendMethods = new Communication.SendMethods();

        public DateTime LastAction { set; get; }

        public SendAlertsThread()
        {
            this.sendData = Data.Implementation.OptionHandler.Instance.GetBoolOption("SendData", false);
            this.tankCheckInterval = Data.Implementation.OptionHandler.Instance.GetIntOption("TankCheckInterval", 60000);
            this.sendMethods.Simulation = !Data.Implementation.OptionHandler.Instance.GetBoolOption("SendData", false);
            DateTime dtStart = DateTime.Now;
            string datestart = Data.Implementation.OptionHandler.Instance.GetOption("StartDate");
            if (datestart != null && datestart != "")
            {
                DateTime.TryParse(datestart, out dtStart);
            }
            officialStartDate = dtStart;
        }

        /// <summary>
        /// Start the main thread of the agent
        /// </summary>
        public void StartThread()
        {
            this.sendData = Data.Implementation.OptionHandler.Instance.GetBoolOption("SendData", false);
            this.haltThread = false;
            //this.database = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
            //this.database.AlertChecker = AlertChecker.Instance;
            th = new System.Threading.Thread(new System.Threading.ThreadStart(this.ThreadRun));
            th.Start();
        }


        /// <summary>
        /// Stops the main thread of the agent
        /// </summary>
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
                delivery.Document.DeliveryType = (Communication.Enums.DeliveryTypeEnum)invoiceLine.Invoice.InvoiceType.DeliveryType;


                delivery.Document.DocumentDate = invoiceLine.Invoice.TransactionDate;
                delivery.Document.DocumentType = invoiceLine.Invoice.InvoiceType.OfficialEnumerator;
                delivery.Document.InvoiceNumber = invoiceLine.Invoice.Number;
                delivery.Document.InvoiceSeries = invoiceLine.Invoice.Series;
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
                delivery.Document.DeliveryType = 
                    tankFilling.LevelStart < tankFilling.LevelEnd ? Communication.Enums.DeliveryTypeEnum.Delivery : Communication.Enums.DeliveryTypeEnum.Return;


                delivery.Document.DocumentDate = tankFilling.TransactionTimeEnd;
                //delivery.Document.DocumentType = invoiceLine.Invoice.InvoiceType.
                delivery.Document.InvoiceNumber = 0;
                delivery.Document.InvoiceSeries = "";// invoiceLine.Invoice.Series;

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
            else
            {
                Communication.DeliveryNoteClass delivery = new Communication.DeliveryNoteClass();
                delivery.FuelData = new List<Communication.DeliveryNoteFuelDataClass>();
                delivery.Reservoirs = new List<Communication.DeliveryNoteReservoir>();
                delivery.Document = new Communication.DeliveryNoteDocumentClass();
                delivery.Document.Amdika = Data.Implementation.OptionHandler.Instance.GetOption("AMDIKA");
                delivery.Document.DeliveryType = (Communication.Enums.DeliveryTypeEnum)invoiceLine.Invoice.InvoiceType.DeliveryType;


                delivery.Document.DocumentDate = invoiceLine.Invoice.TransactionDate;
                delivery.Document.DocumentType = invoiceLine.Invoice.InvoiceType.OfficialEnumerator;
                delivery.Document.InvoiceNumber = invoiceLine.Invoice.Number;
                delivery.Document.InvoiceSeries = invoiceLine.Invoice.Series;
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
            return null;
        }

        private Communication.IncomeRecieptClass CreateSale(Data.SalesTransaction sale)
        {
            if (sale == null)
                return null;
            Communication.IncomeRecieptClass income = new Communication.IncomeRecieptClass();
            income.NozzleId = sale.Nozzle.SerialNumber == null ? "" : sale.Nozzle.SerialNumber;
            income.PublishDateTime = sale.TransactionTimeStamp;
            income.PumpSerialNumber = sale.Nozzle.Dispenser.PumpSerialNumber;
            income.SubmitterDetails = Data.Implementation.OptionHandler.Instance.GetOption("CompanyTIN");
            Data.TankSale tankSale;
            Data.Tank tank;

            if (sale.TankSales == null || sale.TankSales.Count == 0)
            {
                tank = sale.Nozzle.NozzleFlows.Where(n => n.FlowState == 1).Select(n => n.Tank).FirstOrDefault();
                if (tank != null)
                    income.TankTemperature = tank.Temperatire;
                else
                    income.TankTemperature = 15;
            }
            else
            {
                tankSale = sale.TankSales.FirstOrDefault();
                tank = tankSale.Tank;
                income.TankTemperature = tankSale.EndTemperature;
            }

            Data.InvoiceLine invLine = sale.InvoiceLines.FirstOrDefault();
            income.TankId = tank.TankNumber;
            income.TankSerialNumber = tank.TankSerialNumber;
            

            if (invLine != null && invLine.Invoice.Vehicle != null)
                income.PlateNumber = invLine.Invoice.Vehicle.PlateNumber;
            if (invLine != null && invLine.Invoice.Trader != null)
                income.CustomerTin = invLine.Invoice.Trader.TaxRegistrationNumber;
            decimal sign = 1;
            if (invLine != null && invLine.Invoice.InvoiceType.IsCancelation.HasValue && invLine.Invoice.InvoiceType.IsCancelation.Value)
            {
                if(sale.TotalPrice < 0 && sale.Volume < 0)
                    sign = -1;
            }
            income.TotalValue = sign * sale.TotalPrice;
            if (invLine != null)
                income.VatPercentage = invLine.VatPercentage;
            else
                income.VatPercentage = 0;
            income.UnitPrice = sale.UnitPrice;
            income.FuelType = (Communication.Enums.FuelTypeEnum)tank.FuelType.EnumeratorValue;

            income.Volume = sign * sale.Volume;
            income.Volume15 = sign * sale.VolumeNormalized;
            income.TankTemperature = sale.TemperatureStart;
            income.FuelDensity = sale.AvgDencity;
            income.Totalizer = sale.TotalizerEnd;

            return income;
        }

        private ASFuelControl.Communication.TankCheckClass[] CreateTankCheck(Data.DatabaseModel database)
        {
            List<ASFuelControl.Communication.TankCheckClass> tankChecks = new List<TankCheckClass>();
            try
            {
                bool validValues = true;
                var q = database.TankChecks.Where(t => !t.SentDatetime.HasValue).ToList();
                foreach (Data.TankCheck tc in q)
                {
                    ASFuelControl.Communication.TankCheckClass tankCheck = new Communication.TankCheckClass();
                    tankCheck.TankId = tc.Tank.TankSerialNumber;
                    tankCheck.TabkNumber = tc.Tank.TankSerialNumber;
                    tankCheck.TankLevel = tc.TankLevel;
                    tankCheck.TankTemperature = tc.Temperature.Value;
                    tankCheck.TankVolume = tc.Tank.GetTankVolume(tc.TankLevel);
                    tankCheck.TankVolume15 = tc.Tank.GetTankVolumeNormalized(tc.TankLevel);
                    tankCheck.TransactionDate = DateTime.Now;
                    tankCheck.FuelDensity = tc.Tank.CurrentDensity;
                    tankChecks.Add(tankCheck);
                    tc.SentDatetime = DateTime.Now;

                }
            }
            catch (Exception etc)
            {
                Logger.Instance.LogToFile("CheckForSend:TankCheck", etc);
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

        /// <summary>
        /// Checks if the current time if in the first hour of the day
        /// </summary>
        /// <returns></returns>
        private bool IsStartOfDay()
        {
            if (DateTime.Now.Hour == 0 && DateTime.Now.Minute < 59)
                return true;
            return false;
        }

        private Data.Balance CheckForBalance(Data.DatabaseModel database)
        {
            return this.CheckForBalance(database, true);
        }

        /// <summary>
        /// Checks if a creation of Balance is necessary 
        /// </summary>
        /// <returns></returns>
        /// 
        private Data.Balance CheckForBalance(Data.DatabaseModel database, bool checkIsStartOfDay)
        {
            DateTime dtStart = DateTime.Today.AddDays(-1);
            DateTime dtEnd = dtStart.AddDays(1).AddMilliseconds(-1);
            DateTime checkDate = dtStart.AddHours(12);

            bool monthBalance = Data.Implementation.OptionHandler.Instance.GetBoolOption("MonthBalanceEnabled", false);
            bool runningMonth = false;

            DateTime dtPrev = DateTime.Today.AddMonths(-3);
            if (dtPrev < officialStartDate)
                dtPrev = officialStartDate;
            if (dtPrev > DateTime.Today.AddMonths(-2))
                dtPrev = DateTime.Today.AddMonths(-2);
            var qBalances = database.Balances.
                Where(b => b.StartDate.Date >= dtPrev).
                Select(b => new BalanceCheckClass() { BalanceId = b.BalanceId, StartDate = b.StartDate, EndDate = b.EndDate }).
                OrderByDescending(b => b.EndDate).ToArray();



            bool monthBalanceOK = qBalances.Where(b => b.IsMonthBalance()).Count() > 0;
            bool currentBalanceOK = qBalances.Where(b => b.IsCurrentBalance()).Count() > 0;
            //DateTime dtReference = DateTime.Today.AddDays(1).AddMinutes(-4);
            var bt = Data.Implementation.OptionHandler.Instance.GetIntOption("BalanceThreshold", 10);
            DateTime dtReference = DateTime.Now.AddMinutes(bt);
            if (!currentBalanceOK)
            {
                if (dtReference.Date > DateTime.Today)
                    currentBalanceOK = false;
                else
                    currentBalanceOK = true;
            }
            //Common.Logger.Instance.Debug("Current Balance: " + currentBalanceOK.ToString());
            if (monthBalance && !monthBalanceOK)
            {
                DateTime previousDay = DateTime.Today.AddMonths(-1).Date;
                
                dtStart = new DateTime(previousDay.Year, previousDay.Month, 1);
                DateTime nextMonthDay = dtStart.AddMonths(1).Date;
                dtEnd = nextMonthDay.AddMilliseconds(-1);
                checkDate = dtStart.AddDays(15);
                runningMonth = true;
            }
            else if (!currentBalanceOK)
            {
                dtStart = DateTime.Today;
                dtEnd = dtStart.AddDays(1).AddMilliseconds(-1);
            }
            else
            {
                bool balanceToCreate = false;

                var qMindate = database.Balances.Where(b => b.StartDate.Date >= dtPrev).Min(b => b.StartDate);
                int days = DateTime.Today.Subtract(qMindate).Days;
                var q = database.Balances.Where(b => b.StartDate.Date >= dtPrev).Select(b => new { b.BalanceId, b.StartDate, b.EndDate }).OrderByDescending(b => b.EndDate).ToArray();
                for (int i = 0; i < days; i++)
                {
                    DateTime cdate = qMindate.Date.AddDays(i);
                    if (cdate.Date <= officialStartDate.Date)
                        continue;
                    if (cdate.Date == DateTime.Today)
                        continue;
                    var oldBalance = q.FirstOrDefault(b => b.StartDate.Date == cdate && b.EndDate.AddHours(-1).Date == cdate);
                    if (oldBalance == null)
                    {
                        dtStart = cdate;
                        dtEnd = cdate.AddDays(1).AddMilliseconds(-1);
                        balanceToCreate = true;
                        break;
                    }
                }
                if (balanceToCreate == false)
                    return null;
            }
            
            Data.UsagePeriod period = null;
            period = database.GetCreateUsagePeriod(dtStart);
            
            Common.Logger.Instance.Debug(string.Format("Balance Period: {0:dd-MM-yyyy HH:mm} - {1:dd-MM-yyyy HH:mm}", dtStart, dtEnd));
            if (dtStart.Date == DateTime.Today)
            {
                if (currentBalanceOK)
                    return null;
                Program.ApplicationMainForm.Invoke(new Action(() =>
                {
                    Program.ApplicationMainForm.ThreadControllerInstance.LockDispensers();
                    Common.Logger.Instance.Debug(string.Format("Dispensers Locked"));
                }));
            }

            var balance = Data.Balance.CreateBalance(dtStart, dtEnd, AlertChecker.Instance);
            Common.Logger.Instance.Debug(string.Format("Balance Ready"));
            if (dtStart.Date == DateTime.Today)
            {
                if (!currentBalanceOK)
                {
                    Program.ApplicationMainForm.Invoke(new Action(() =>
                    {
                        Program.ApplicationMainForm.ThreadControllerInstance.UnlockDispensers();
                        Common.Logger.Instance.Debug(string.Format("Dispensers Unlocked"));
                    }));
                }
            }
            return balance;

        }

        private void LockDispensers()
        {
            var dtStart = DateTime.Now;
            
            Program.ApplicationMainForm.Invoke(new Action(() =>
            {
                Program.ApplicationMainForm.ThreadControllerInstance.LockDispensers();
                Common.Logger.Instance.Debug(string.Format("Dispensers Locked"));
                while (true)
                {
                    if (dtStart.Date < DateTime.Today)
                        break;
                    System.Threading.Thread.Sleep(250);
                }
                Program.ApplicationMainForm.Invoke(new Action(() => Program.ApplicationMainForm.ThreadControllerInstance.UnlockDispensers()));
                Common.Logger.Instance.Debug(string.Format("Dispensers Unlocked"));
            }));
        }

        /// <summary>
        /// Helper class to store Group Sums
        /// </summary>
        private class GroupSum
        {
            public string PumpSerialNumber { set; get; }
            public decimal TotalOut { set; get; }
            public decimal TotalOutNormalized { set; get; }
            public decimal TotalIn { set; get; }
            public decimal TotalInNormalized { set; get; }
        }

        private void LogSend(Data.DatabaseModel database, object sendObject, int status)
        {
            try
            {
                string desc = "";
                Common.Enumerators.SendLogActionEnum action = Common.Enumerators.SendLogActionEnum.Balance;
                string identity = "";
                switch (sendObject.GetType().Name)
                {
                    case "SystemEvent":
                        Data.SystemEvent se = sendObject as Data.SystemEvent;
                        desc = "Συναγερμός : " + se.Message + " " + se.DeviceDescription + " " + se.EventDate.ToString("dd/MM/yyyy HH:mm");
                        action = Common.Enumerators.SendLogActionEnum.SystemEvent;
                        identity = se.EventId.ToString();
                        break;
                    case "TankFilling":
                        Data.TankFilling tf = sendObject as Data.TankFilling;
                        desc = "Παραλαβή : " + tf.Tank.Description + " " + tf.TransactionTimeEnd.ToString("dd/MM/yyyy HH:mm");
                        action = Common.Enumerators.SendLogActionEnum.TankFilling;
                        identity = tf.TankFillingId.ToString();
                        break;
                    case "SalesTransaction":
                        Data.SalesTransaction st = sendObject as Data.SalesTransaction;
                        desc = "Πώληση : " + st.Nozzle.Description + " " + st.TransactionTimeStamp.ToString("dd/MM/yyyy HH:mm");
                        action = Common.Enumerators.SendLogActionEnum.SalesTransaction;
                        identity = st.SalesTransactionId.ToString();
                        break;
                    case "FuelTypePrice":
                        Data.FuelTypePrice ftp = sendObject as Data.FuelTypePrice;
                        desc = "Αλλαγή Τιμής : " + ftp.FuelType.Name + " " + ftp.ChangeDate.ToString("dd/MM/yyyy HH:mm");
                        action = Common.Enumerators.SendLogActionEnum.FuelTypePrice;
                        identity = ftp.FuelTypePriceId.ToString();
                        break;
                    case "ChangePriceClass":
                        Communication.ChangePriceClass cpc = sendObject as Communication.ChangePriceClass;
                        desc = "Αλλαγή Τιμής : " + cpc.FuelType.ToString() + " " + cpc.Price.ToString("N3") + " " + cpc.ChangeTime.ToString("dd/MM/yyyy HH:mm:ss");
                        action = Common.Enumerators.SendLogActionEnum.FuelTypePrice;
                        identity = desc;
                        break;
                    case "IncomeRecieptClass":
                        Communication.IncomeRecieptClass irc = sendObject as Communication.IncomeRecieptClass;
                        desc = "Πώληση : " + irc.FuelType.ToString() + " " + irc.TotalValue.ToString("N2") + " " + irc.PublishDateTime.ToString("dd/MM/yyyy HH:mm:ss:fff") + " " + irc.PumpSerialNumber;
                        action = Common.Enumerators.SendLogActionEnum.SalesTransaction;
                        identity = desc;
                        break;
                    case "BalanceClass":
                        Communication.BalanceClass bc = sendObject as Communication.BalanceClass;
                        desc = "Ισοζύγιο : " + bc.TimeStart.ToString("dd/MM/yyyy HH:mm") + " - " + bc.TimeEnd.ToString("dd/MM/yyyy HH:mm");
                        action = Common.Enumerators.SendLogActionEnum.Balance;
                        identity = desc;
                        break;
                    case "Balance":
                        Data.Balance bc1 = sendObject as Data.Balance;
                        desc = "Ισοζύγιο : " + bc1.StartDate.ToString("dd/MM/yyyy HH:mm") + " - " + bc1.EndDate.ToString("dd/MM/yyyy HH:mm");
                        action = Common.Enumerators.SendLogActionEnum.Balance;
                        identity = bc1.BalanceId.ToString();
                        break;
                }
                //Console.WriteLine(sendObject.GetType().Name);
                SendLog log = new SendLog();
                log.Action = action;
                log.Data = desc;
                log.Identity = identity;
                log.Status = status;
                log.SentTime = DateTime.Now;

                Data.SendLog slog = database.SendLogs.Where(s => s.EntityIdentity == log.Identity).FirstOrDefault();
                if (slog == null)
                {
                    slog = new Data.SendLog();
                    slog.SendLogId = Guid.NewGuid();
                    slog.SendDate = log.SentTime;
                    slog.EntityIdentity = log.Identity;
                    slog.SendData = log.Data;
                    database.Add(slog);
                }
                slog.Action = log.Action.ToString();
                slog.LastSent = log.SentTime;
                slog.SentStatus = log.Status;
                database.SaveChanges();

            }
            catch (Exception ex)
            {
                Logging.Logger.Instance.LogToFile("SendAlertsThread::LogSend ", ex);
            }
            //this.logToSave.Add(log);
        }

        /// <summary>
        /// Checks if there are data for sending avaliable
        /// </summary>
        private void CheckForSend(Data.DatabaseModel database)
        {
            try
            {
                this.LastAction = DateTime.Now;
                int interval = this.tankCheckInterval;
                DateTime dt = DateTime.Now;
                try
                {
                    var qtc = database.TankChecks.Where(t => !t.SentDatetime.HasValue).OrderByDescending(t=>t.CheckDate).Take(10).ToList();

                    foreach (Data.TankCheck tc in qtc)
                    {
                        if (tc.Tank.FuelType.ExcludeFromBalance.HasValue && tc.Tank.FuelType.ExcludeFromBalance.Value)
                            continue;
                        try
                        {
                            this.SendObject(tc);
                            System.Threading.Thread.Sleep(250);
                        }
                        catch (Exception etc)
                        {
                            Logger.Instance.LogToFile("CheckForSend:TankCheck", etc);
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.Instance.LogToFile("CheckForSend:TankCheck", e);
                }
                database.SaveChanges();
                database.Refresh(Telerik.OpenAccess.RefreshMode.OverwriteChangesFromStore, database.SystemEvents.Where(se => !se.SentDate.HasValue));
                //DateTime dtAlertCheck = DateTime.Now.AddMinutes(-1);
                var q = database.SystemEvents.Where(se => !se.SentDate.HasValue && se.EventDate >= this.officialStartDate
                    &&
                    (
                        se.DocumentSign != null &&
                        se.DocumentSign != "" &&
                        !se.DocumentSign.Contains("ΔΟΚΙΜΑΣΤΙΚΗ ΛΕΙΤΟΥΡΓΙΑ") &&
                        (se.DocumentSign == "-" || se.DocumentSign.Length >= 15) &&
                        !se.ResolvedDate.HasValue 
                    //    se.EventDate < dtAlertCheck
                    )).OrderByDescending(t => t.EventDate).Take(10).Take(10).ToList();

                try
                {
                    foreach (Data.SystemEvent se in q)
                    {
                        try
                        {
                            if(se.Tank != null)
                            {
                                if (se.Tank.FuelType.ExcludeFromBalance.HasValue && se.Tank.FuelType.ExcludeFromBalance.Value)
                                    continue;
                            }
                            if (this.SendObject(se))
                            {
                                //if (!sendMethods.Simulation)
                                se.SentDate = DateTime.Now;
                                if (this.SubmissionSuccess != null)
                                    this.SubmissionSuccess(se, new EventArgs());
                                LogSend(database, se, 1);
                            }
                            else
                            {
                                if (this.SubmissionFailed != null)
                                    this.SubmissionFailed(se, new EventArgs());
                                LogSend(database, se, 0);
                            }
                            System.Threading.Thread.Sleep(1000);
                        }
                        catch (Exception ex)
                        {
                            Logger.Instance.LogToFile("CheckForSend:Alarm", ex);
                            Logger.Instance.LogToFile("CheckForSend:Alarm::Object", database.SerializeEntity(se));
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.Instance.LogToFile("CheckForSend:Alarm", e);
                }
                database.SaveChanges();
                database.Refresh(Telerik.OpenAccess.RefreshMode.OverwriteChangesFromStore, database.TankFillings.Where(tf => !tf.SentDateTime.HasValue));
                var q1 = database.TankFillings.Where(se => !se.SentDateTime.HasValue && se.TransactionTime >= this.officialStartDate).
                    OrderByDescending(t => t.TransactionTimeEnd).Take(10).ToList();
                try
                {
                    foreach (Data.TankFilling se in q1)
                    {
                        try
                        {
                            if (se.Tank != null && se.Tank.FuelType.ExcludeFromBalance.HasValue && se.Tank.FuelType.ExcludeFromBalance.Value)
                                continue;
                            if (se.InvoiceLines.Count == 0)
                                continue;
                            
                            object obj = this.CreateDeliveryNote(se);
                            if (this.SendObject(obj))
                            {
                                if (!sendMethods.Simulation)
                                    se.SentDateTime = DateTime.Now;
                                if (this.SubmissionSuccess != null)
                                    this.SubmissionSuccess(se, new EventArgs());
                                this.LogSend(database, se, 1);
                            }
                            else
                            {
                                if (this.SubmissionFailed != null)
                                    this.SubmissionFailed(se, new EventArgs());
                                this.LogSend(database, se, 0);
                            }
                            System.Threading.Thread.Sleep(1000);
                        }
                        catch (Exception ex)
                        {
                            Logger.Instance.LogToFile("CheckForSend:TankFilling", ex);
                            Logger.Instance.LogToFile("CheckForSend:TankFilling::Object", database.SerializeEntity(se));
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.Instance.LogToFile("CheckForSend:Delivery", e);
                }
                database.SaveChanges();
                database.Refresh(Telerik.OpenAccess.RefreshMode.OverwriteChangesFromStore, database.SalesTransactions.Where(tf => !tf.SentDateTime.HasValue));


                var qst = database.SalesTransactions.Where(se => !se.SentDateTime.HasValue && se.InvoiceLines.Count > 0 && se.TransactionTimeStamp >= this.officialStartDate).ToList();



                var qil = database.InvoiceLines.Where(il => il.Invoice.InvoiceSignature != null && il.Invoice.InvoiceSignature != "" && il.Invoice.InvoiceSignature.Length > 20);
                var qse = qil.Where(il => il.SaleTransactionId.HasValue).Select(il => il.SalesTransaction);
                qse = qse.Where(se => !se.SentDateTime.HasValue);
                qse = qse.Where(se => se.InvoiceLines.Count > 0);
                qse = qse.Where(se => se.TransactionTimeStamp >= this.officialStartDate);


                //var q5 = qst.Where(se => 
                //        (
                //            se.InvoiceLines.Where(il=>
                //                //il.Invoice.InvoiceSignature != null &&
                //                //il.Invoice.InvoiceSignature != "" &&
                //                //!il.Invoice.InvoiceSignature.Contains("ΔΟΚΙΜΑΣΤΙΚΗ ΛΕΙΤΟΥΡΓΙΑ") &&
                //                il.Invoice.InvoiceSignature.Length > 20
                //             ).Count() == 0
                //        )
                //    ).ToList();
                var q2 = qst.OrderByDescending(t => t.TransactionTimeStamp).Take(10).ToList();//qse.OrderByDescending(t => t.TransactionTimeStamp).Take(10).ToList();
                //var q2 = database.SalesTransactions.Where(se => !se.SentDateTime.HasValue && se.InvoiceLines.Count > 0 && se.TransactionTimeStamp >= this.officialStartDate &&
                //        (
                //            se.InvoiceLines.Where(il=>
                //                il.Invoice.InvoiceSignature != null &&
                //                il.Invoice.InvoiceSignature != "" &&
                //                !il.Invoice.InvoiceSignature.Contains("ΔΟΚΙΜΑΣΤΙΚΗ ΛΕΙΤΟΥΡΓΙΑ") &&
                //                il.Invoice.InvoiceSignature.Length > 20
                //             ).Count() == 0
                //        )
                //    ).ToList();

                try
                {
                    foreach (Data.SalesTransaction se in q2)
                    {
                        try
                        {
                            var invLines = database.InvoiceLines.Where(s => s.SaleTransactionId == se.SalesTransactionId).Select(i=>i.Invoice).ToList();
                            if (invLines.Where(i => i.InvoiceSignature != null && i.InvoiceSignature != "" && i.InvoiceSignature.Length > 20).Count() == 0)
                                continue;
                            Communication.IncomeRecieptClass income = this.CreateSale(se);
                            if (income == null)
                                continue;
                            if (se.Volume == 0 || se.TotalPrice == 0)
                                se.SentDateTime = DateTime.Now;
                            else
                            {
                                if (this.SendObject(income))
                                {
                                    if (!sendMethods.Simulation)
                                        se.SentDateTime = DateTime.Now;
                                    if (this.SubmissionSuccess != null)
                                        this.SubmissionSuccess(se, new EventArgs());
                                    this.LogSend(database, se, 1);
                                }
                                else
                                {
                                    if (this.SubmissionFailed != null)
                                        this.SubmissionFailed(income, new EventArgs());
                                    this.LogSend(database, se, 0);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Instance.LogToFile("CheckForSend:SalesTransaction", ex);
                            Logger.Instance.LogToFile("CheckForSend:SalesTransaction::Object", database.SerializeEntity(se));
                        }
                        System.Threading.Thread.Sleep(1000);
                    }
                }
                catch (Exception e)
                {
                    Logger.Instance.LogToFile("CheckForSend:SalesTransaction", e);
                }
                database.SaveChanges();
                database.Refresh(Telerik.OpenAccess.RefreshMode.OverwriteChangesFromStore, database.FuelTypePrices.Where(tf => !tf.SentDateTime.HasValue));
                var q4 = database.FuelTypePrices.Where(se => !se.SentDateTime.HasValue && se.ChangeDate >= this.officialStartDate).ToList();
                try
                {
                    foreach (Data.FuelTypePrice ftp in q4)
                    {
                        try
                        {
                            if (ftp.FuelType.ExcludeFromBalance.HasValue && ftp.FuelType.ExcludeFromBalance.Value)
                                continue;
                            ChangePriceClass cpc = new ChangePriceClass();
                            cpc.ChangeTime = ftp.ChangeDate;
                            cpc.FuelType = (Communication.Enums.FuelTypeEnum)ftp.FuelType.EnumeratorValue;
                            cpc.Price = ftp.Price;

                            if (this.SendObject(cpc))
                            {
                                if (!sendMethods.Simulation)
                                    ftp.SentDateTime = DateTime.Now;
                                if (this.SubmissionSuccess != null)
                                    this.SubmissionSuccess(ftp, new EventArgs());
                                this.LogSend(database, cpc, 1);
                            }
                            else
                            {
                                if (this.SubmissionFailed != null)
                                    this.SubmissionFailed(cpc, new EventArgs());
                                this.LogSend(database, cpc, 0);
                            }
                            System.Threading.Thread.Sleep(1000);
                        }
                        catch (Exception ex)
                        {
                            Logger.Instance.LogToFile("CheckForSend:FuelPrice", ex);
                            Logger.Instance.LogToFile("CheckForSend:FuelPrice::Object", database.SerializeEntity(ftp));
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.Instance.LogToFile("CheckForSend:ChangePrice", e);
                }

                //try
                //{
                //    Data.Balance balance = this.CheckForBalance(database);
                //    if (balance != null)
                //    {
                //        System.Xml.Serialization.XmlSerializer ser = new System.Xml.Serialization.XmlSerializer(typeof(Communication.BalanceClass));
                //        using (TextReader reader = new StringReader(balance.BalanceText))
                //        {
                //            Communication.BalanceClass result = ser.Deserialize(reader) as Communication.BalanceClass;
                //            if (result.Movements.FuelMovements.Count == 0)
                //            {
                //                balance.SentDateTime = DateTime.Now;
                //            }
                //            else
                //            {
                //                if (this.SendObject(result))
                //                {
                //                    if (!sendMethods.Simulation)
                //                        balance.SentDateTime = DateTime.Now;
                //                    if (this.SubmissionSuccess != null)
                //                        this.SubmissionSuccess(balance, new EventArgs());
                //                    this.LogSend(database, balance, 1);
                //                }
                //                else
                //                {
                //                    if (this.SubmissionFailed != null)
                //                        this.SubmissionFailed(balance, new EventArgs());
                //                    this.LogSend(database, balance, 0);
                //                }
                //            }
                //            System.Threading.Thread.Sleep(1000);
                //        }
                //        if (this.BalanceCreated != null)
                //            this.BalanceCreated(this, new EventArgs());
                //    }
                //}
                //catch (Exception e)
                //{
                //    Logger.Instance.LogToFile("CheckForSend:Balance", e);
                //}
                var q3 = database.Balances.Where(se => !se.SentDateTime.HasValue && se.EndDate >= this.officialStartDate && 
                            (
                                se.DocumentSign != null && 
                                se.DocumentSign != "" && 
                                !se.DocumentSign.Contains("ΔΟΚΙΜΑΣΤΙΚΗ ΛΕΙΤΟΥΡΓΙΑ") &&
                                se.DocumentSign.Length > 15
                            )
                        ).ToList();
                try
                {
                    foreach (Data.Balance bal in q3)
                    {
                        try
                        {
                            System.Xml.Serialization.XmlSerializer ser = new System.Xml.Serialization.XmlSerializer(typeof(Communication.BalanceClass));
                            using (TextReader reader = new StringReader(bal.BalanceText))
                            {
                                object result = ser.Deserialize(reader);
                                if (this.SendObject(result))
                                {
                                    if (!sendMethods.Simulation)
                                        bal.SentDateTime = DateTime.Now;
                                    if (this.SubmissionSuccess != null)
                                        this.SubmissionSuccess(bal, new EventArgs());
                                    this.LogSend(database, bal, 1);
                                }
                                else
                                {
                                    if (this.SubmissionFailed != null)
                                        this.SubmissionFailed(bal, new EventArgs());
                                    this.LogSend(database, bal, 0);
                                }
                                System.Threading.Thread.Sleep(1000);
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Instance.LogToFile("CheckForSend:Balance", ex);
                            Logger.Instance.LogToFile("CheckForSend:Balance::Object", database.SerializeEntity(bal));
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.Instance.LogToFile("CheckForSend:BalanceSend", e);
                }
                database.SaveChanges();


                if ((int)dt.TimeOfDay.TotalMinutes % 5 == 0 && !this.logCleared)
                {
                    try
                    {
                        System.Data.SqlClient.SqlCommand c = new System.Data.SqlClient.SqlCommand("EXECUTE dbo.ClearLog");
                        c.Connection = new System.Data.SqlClient.SqlConnection(Properties.Settings.Default.DBConnection);
                        c.Connection.Open();
                        c.ExecuteNonQuery();//(command, new System.Data.Common.DbParameter[] { });
                        c.Connection.Close();
                        this.logCleared = true;
                    }
                    catch (Exception ex)
                    {
                        Logging.Logger.Instance.LogToFile(string.Format("ClearLog Exception"), ex);
                    }
                }
                else if ((int)dt.TimeOfDay.TotalMinutes % 5 == 0)
                {
                }
                else
                {
                    logCleared = false;
                }
                System.Threading.Thread.Sleep(1000);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogToFile("CheckForSend", ex);
            }
        }
        
        /// <summary>
        /// Raises an event for each Alert is not printed yet
        /// </summary>
        private void CheckForPrint(Data.DatabaseModel database)
        {
            if (this.PrintAlert == null)
                return;
            try
            {
                List<Data.SystemEvent> q = database.SystemEvents.Where(se => !se.ResolvedDate.HasValue && !se.PrintedDate.HasValue).ToList();
                foreach (Data.SystemEvent ev in q)
                {
                    this.PrintAlert(this, new PrintAlertEventArgs() { AlertId = ev.EventId });
                    //ev.PrintedDate = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogToFile("CheckForSend", ex);
                return;
            }

            database.SaveChanges();
        }

        DateTime dtAlertPrint = DateTime.Now;

        bool logCleared = false;
        private void ThreadRun()
        {
            try
            {
                using (Data.DatabaseModel database = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
                {
                    Data.Balance bal = this.CheckForBalance(database, false);

                    if (bal == null)
                    {
                    }
                }
            }
            catch(Exception ex1)
            {
                Logging.Logger.Instance.LogToFile("SendAlertsThread::CheckForBalance", ex1);
            }

            int i = 0;
            while (!haltThread)
            {
                try
                {
                    using (Data.DatabaseModel database = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
                    {
                        database.AlertChecker = AlertChecker.Instance;
                        i++;
                        if (i % 1 == 0)
                        {
                            Data.Balance bal = this.CheckForBalance(database, false);
                            i = 0;
                        }
                        if (!this.sendData)
                        {
                            System.Threading.Thread.Sleep(10000);
                            continue;
                        }
                        this.CheckForSend(database);
                        if (DateTime.Now.Subtract(dtAlertPrint).TotalSeconds < 20)
                            continue;
                        this.CheckForPrint(database);
                    }
                    
                }
                catch(Exception ex1)
                {
                    Logging.Logger.Instance.LogToFile("SendAlertsThread::Loop", ex1);
                }
                if(!haltThread)
                    System.Threading.Thread.Sleep(10000);

            }
            using (Data.DatabaseModel database = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                this.CheckForSend(database);
            }
            System.Threading.Thread.CurrentThread.Abort();
        }


        /// <summary>
        /// Send an object to the GGPS service
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
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
                header.TaxisBranch = Data.Implementation.OptionHandler.Instance.GetIntOption("CompanyBranch", 0);
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
                    case "TankCheck":
                        Data.TankCheck tcc = obj as Data.TankCheck;

                        decimal volume = tcc.Tank.GetTankVolume(tcc.TankLevel);

                        ASFuelControl.Communication.TankCheckClass tankCheck = new Communication.TankCheckClass();
                        tankCheck.TankId = tcc.Tank.TankSerialNumber;
                        tankCheck.TabkNumber = tcc.Tank.TankSerialNumber;
                        tankCheck.TankLevel = tcc.TankLevel == 0 ? (decimal)0.01 : tcc.TankLevel;
                        tankCheck.TankTemperature = tcc.Temperature.Value;
                        tankCheck.TankVolume = volume == 0 ? (decimal)0.01 : volume;
                        tankCheck.TankVolume15 = volume == 0 ? (decimal)0.01 : volume;
                        tankCheck.TransactionDate = DateTime.Now;
                        tankCheck.FuelDensity = tcc.Tank.CurrentDensity;
                        if (tcc.Tank.CurrentDensity >= 900 || tcc.Tank.CurrentDensity <= 500)
                        {
                            return false;
                        }
                        else
                        {
                            seriallizedData = sendMethods.SendTankCheck(header, tankCheck);
                            if (seriallizedData == "[ERROR]" || seriallizedData.Contains("[ERROR]"))
                            {
                                this.LogSend("SendTankCheck", seriallizedData);
                                return false;
                            }
                            tcc.SentDatetime = DateTime.Now;
                            this.LogSend("SendTankCheck", seriallizedData);
                        }
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
                        //if (se.AlertDefinition != null)
                        //    alert.Alert = (Communication.Enums.AlertIdEnum)se.AlertDefinition.AlertEnumValue.Value;
                        //else
                        //{
                        //    if(se.AlarmType.HasValue)
                        //        alert.Alert = (Communication.Enums.AlertIdEnum)se.AlarmType.Value;
                        //    else
                        //        alert.Alert = Communication.Enums.AlertIdEnum.ProgramTermination;
                        //}
                        if(se.AlarmType.HasValue)
                            alert.SetAlertType(se.AlarmType.Value);
                        else
                            alert.Alert = (Communication.Enums.AlertIdEnum)se.EventType;
                        alert.AlertTime = se.EventDate;
                        alert.Description = se.OfficialDescription;
                        if (se.TankId.HasValue)
                            alert.DeviceId = se.Tank.TankSerialNumber;
                        else if (se.NozzleId.HasValue)
                            alert.DeviceId = se.Nozzle.SerialNumber;
                        else if (se.DispenserId.HasValue)
                            alert.DeviceId = se.Dispenser.PumpSerialNumber;

                        bool sendSms = Data.Implementation.OptionHandler.Instance.GetBoolOption("SmsSendAlarm", false);
                        if(sendSms)
                            MailSender.Instance.SendSms(alert.Description);
                        if (alert.Alert == Communication.Enums.AlertIdEnum.TankDensityError)
                        {
                            return true;
                        }
                        else
                        {
                            seriallizedData = sendMethods.SendAlert(header, alert);
                            if (seriallizedData == "[ERROR]" || seriallizedData.Contains("[ERROR]"))
                            {
                                this.LogSend("SendAlert", seriallizedData);
                                return false;
                            }
                            this.LogSend("SendAlert", seriallizedData);
                        }
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

        /// <summary>
        /// Sends a notification to the GGPS that the version is changed
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        public bool SendSoftwareChange(string version)
        {
            string amdika = Data.Implementation.OptionHandler.Instance.GetOption("AMDIKA");
            bool updated = this.sendMethods.SendSWUpdate(amdika, version);
            return updated;
        }

        private Dictionary<string, DateTime> lastLoggedActions = new Dictionary<string, DateTime>();
        /// <summary>
        /// Method for logging the send status of each sending process
        /// </summary>
        /// <param name="action"></param>
        /// <param name="data"></param>
        /// <returns></returns>
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
                    if (lastLoggedActions.ContainsKey(action) && DateTime.Now.Subtract(lastLoggedActions[action]).TotalSeconds < 60)
                        return true;
                    if (!this.lastLoggedActions.ContainsKey(action))
                        this.lastLoggedActions.Add(action, DateTime.Now);
                    else
                        this.lastLoggedActions[action] = DateTime.Now;

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
                    if (this.lastLoggedActions.ContainsKey(action))
                        this.lastLoggedActions[action] = DateTime.MinValue;
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

    public class BalanceCheckClass
    {
        public Guid BalanceId { set; get; }
        public DateTime StartDate { set; get; }
        public DateTime EndDate { set; get; }

        public bool IsMonthBalance()
        {
            if (this.EndDate.Subtract(this.StartDate.Date).TotalDays <= 1)
                return false;
            var dt1 = DateTime.Today.AddDays(1 - DateTime.Today.Day);
            var dt2 = this.StartDate.Date.AddMonths(1);
            var dt3 = dt2.AddDays(1 - dt2.Day);
            if (dt1 == dt3)
                return true;
            return false;
        }

        public bool IsCurrentBalance()
        {
            if (this.StartDate.Date != DateTime.Today) //DateTime.Today.AddDays(-1))
                return false;
            if(EndDate.Date == DateTime.Today.Date.AddDays(1))
                return true;
            return false;
        }
    }
}
