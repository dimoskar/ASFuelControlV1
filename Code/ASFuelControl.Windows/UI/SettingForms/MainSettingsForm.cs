using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Telerik.WinControls.UI;

namespace ASFuelControl.Windows.UI.SettingForms
{
    public partial class MainSettingsForm : RadForm
    {
        

        public MainSettingsForm()
        {
            InitializeComponent();

            this.LoadData();
        }

        private void LoadData()
        {
            this.nameTextBox.Text = Data.Implementation.OptionHandler.Instance.GetOption("CompanyName");
            this.addressTextBox.Text = Data.Implementation.OptionHandler.Instance.GetOption("CompanyAddress");
            this.occupationTextBox.Text = Data.Implementation.OptionHandler.Instance.GetOption("CompanyOccupation");
            this.phoneTextBox.Text = Data.Implementation.OptionHandler.Instance.GetOption("CompanyPhone");
            this.faxTextBox.Text = Data.Implementation.OptionHandler.Instance.GetOption("CompanyFax");
            this.cityTextBox.Text = Data.Implementation.OptionHandler.Instance.GetOption("CompanyCity");
            this.postalCodeextBox.Text = Data.Implementation.OptionHandler.Instance.GetOption("CompanyPostalCode");
            this.taxOfficeTextBox.Text = Data.Implementation.OptionHandler.Instance.GetOption("CompanyTaxOffice");

            this.tinCompanyTextBox.Text = Data.Implementation.OptionHandler.Instance.GetOption("CompanyTIN");
            this.tinSenderTextBox.Text = Data.Implementation.OptionHandler.Instance.GetOption("SenderTIN");
            this.amdikaTextBox.Text = Data.Implementation.OptionHandler.Instance.GetOption("AMDIKA");

            bool finalized = Data.Implementation.OptionHandler.Instance.GetBoolOption("IsFinalized", false);
            this.IsFinalized.IsChecked = finalized;

            bool sd = Data.Implementation.OptionHandler.Instance.GetBoolOption("SendData", false);
            this.sendData.IsChecked = sd;

            int tanckCheckInt = Data.Implementation.OptionHandler.Instance.GetIntOption("TankCheckInterval", 5);
            TimeSpan ts = TimeSpan.FromMinutes(tanckCheckInt);
            this.tankCheckHour.Value = ts.Hours;
            this.tankCheckMinute.Value = ts.Minutes;

            int deliveryWaiting = Data.Implementation.OptionHandler.Instance.GetIntOption("DeliveryWaitingTime", 900);
            TimeSpan ts1 = TimeSpan.FromSeconds(deliveryWaiting);
            this.waitingDeliveryMins.Value = (int)ts1.TotalMinutes;
            this.waitingDeliverySecs.Value = ts1.Seconds;

            int literCheckWaiting = Data.Implementation.OptionHandler.Instance.GetIntOption("LiterCheckWaitingTime", 60);
            TimeSpan ts2 = TimeSpan.FromSeconds(literCheckWaiting);
            this.waitingLiterCheckMins.Value = (int)ts2.TotalMinutes;
            this.waitingLiterCheckSecs.Value = ts2.Seconds;

            this.literCheckInvoiceType.DataSource = Data.Implementation.OptionHandler.Instance.Database.InvoiceTypes;
            this.literCheckInvoiceType.DisplayMember = "Description";
            this.literCheckInvoiceType.ValueMember = "InvoiceTypeId";

            this.deliveryCheckInvoiceType.DataSource = Data.Implementation.OptionHandler.Instance.Database.InvoiceTypes;
            this.deliveryCheckInvoiceType.DisplayMember = "Description";
            this.deliveryCheckInvoiceType.ValueMember = "InvoiceTypeId";

            this.returnInvoiceType.DataSource = Data.Implementation.OptionHandler.Instance.Database.InvoiceTypes;
            this.returnInvoiceType.DisplayMember = "Description";
            this.returnInvoiceType.ValueMember = "InvoiceTypeId";

            this.sendCheckInvoiceType.DataSource = Data.Implementation.OptionHandler.Instance.Database.InvoiceTypes;
            this.sendCheckInvoiceType.DisplayMember = "Description";
            this.sendCheckInvoiceType.ValueMember = "InvoiceTypeId";

            this.literCheckInvoiceType.SelectedValue = Data.Implementation.OptionHandler.Instance.GetGuidOption("LiterCheckInvoiceType", Guid.Empty);
            this.deliveryCheckInvoiceType.SelectedValue = Data.Implementation.OptionHandler.Instance.GetGuidOption("DeliveryCheckInvoiceType", Guid.Empty);
            this.returnInvoiceType.SelectedValue = Data.Implementation.OptionHandler.Instance.GetGuidOption("ReturnInvoiceType", Guid.Empty);
            this.sendCheckInvoiceType.SelectedValue = Data.Implementation.OptionHandler.Instance.GetGuidOption("SendCheckInvoiceType", Guid.Empty);
            this.spinVAT.Value = Data.Implementation.OptionHandler.Instance.GetDecimalOption("VATValue", (decimal)23);

            this.serialNumberText.Text = Data.Implementation.OptionHandler.Instance.GetOption("SerialNumber");
            //this.startControllerButton.IsEnabled = !App.Mediator.MediatorWorking;
            //this.stopControllerButton.IsEnabled = App.Mediator.MediatorWorking;
        }

        private void SaveData()
        {
            Data.Implementation.OptionHandler.Instance.SetOption("CompanyTIN", this.tinCompanyTextBox.Text);
            Data.Implementation.OptionHandler.Instance.SetOption("SenderTIN", this.tinSenderTextBox.Text);
            Data.Implementation.OptionHandler.Instance.SetOption("AMDIKA", this.amdikaTextBox.Text);
            int duration = (int)this.tankCheckHour.Value * 60 + (int)this.tankCheckMinute.Value;
            int waitDelivery = (int)this.waitingDeliveryMins.Value * 60 + (int)this.waitingDeliverySecs.Value;
            int waitLiterCheck = (int)this.waitingLiterCheckMins.Value * 60 + (int)this.waitingLiterCheckSecs.Value;

            Data.Implementation.OptionHandler.Instance.SetOption("TankCheckInterval", duration);
            Data.Implementation.OptionHandler.Instance.SetOption("DeliveryWaitingTime", waitDelivery);
            Data.Implementation.OptionHandler.Instance.SetOption("LiterCheckWaitingTime", waitLiterCheck);

            Data.Implementation.OptionHandler.Instance.SetOption("IsFinalized", this.IsFinalized.IsChecked);
            Data.Implementation.OptionHandler.Instance.SetOption("SendData", this.sendData.IsChecked);

            Data.Implementation.OptionHandler.Instance.SetOption("LiterCheckInvoiceType", this.literCheckInvoiceType.SelectedValue);
            Data.Implementation.OptionHandler.Instance.SetOption("DeliveryCheckInvoiceType", this.deliveryCheckInvoiceType.SelectedValue);
            Data.Implementation.OptionHandler.Instance.SetOption("ReturnInvoiceType", this.returnInvoiceType.SelectedValue);
            Data.Implementation.OptionHandler.Instance.SetOption("SendCheckInvoiceType", this.sendCheckInvoiceType.SelectedValue);

            Data.Implementation.OptionHandler.Instance.SetOption("CompanyName", this.nameTextBox.Text);
            Data.Implementation.OptionHandler.Instance.SetOption("CompanyAddress", this.addressTextBox.Text);
            Data.Implementation.OptionHandler.Instance.SetOption("CompanyOccupation", this.occupationTextBox.Text);
            Data.Implementation.OptionHandler.Instance.SetOption("CompanyPhone", this.phoneTextBox.Text);
            Data.Implementation.OptionHandler.Instance.SetOption("CompanyFax", this.faxTextBox.Text);
            Data.Implementation.OptionHandler.Instance.SetOption("CompanyCity", this.cityTextBox.Text);
            Data.Implementation.OptionHandler.Instance.SetOption("CompanyPostalCode", this.postalCodeextBox.Text);
            Data.Implementation.OptionHandler.Instance.SetOption("CompanyTaxOffice", this.taxOfficeTextBox.Text);

            Data.Implementation.OptionHandler.Instance.SetOption("VATValue", this.spinVAT.Value.ToString("N2"));
            Data.Implementation.OptionHandler.Instance.SetOption("SerialNumber", this.serialNumberText.Text);
        }

        private void MainSettingsForm_Load(object sender, EventArgs e)
        {
            this.stopControllerButton.DataBindings.Add("Enabled", Program.ApplicationMainForm.ThreadControllerInstance, "IsRunning");
            this.startControllerButton.DataBindings.Add("Enabled", Program.ApplicationMainForm.ThreadControllerInstance, "IsStopped");
        }

        private void radButton4_Click(object sender, EventArgs e)
        {
            this.SaveData();
            this.Close();
        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);

            Data.Balance firstBalance = db.Balances.OrderBy(b => b.StartDate).FirstOrDefault();
            Data.Balance lastBalance = db.Balances.OrderBy(b => b.StartDate).LastOrDefault();

            UI.SelectionForms.SelectFromToForm sftf = new SelectionForms.SelectFromToForm();
            if(firstBalance != null)
                sftf.StartDate = firstBalance.StartDate;
            if (lastBalance != null)
                sftf.EndDate = lastBalance.EndDate;

            DialogResult res = sftf.ShowDialog();
            if (res == System.Windows.Forms.DialogResult.Cancel)
                return;

            var q = db.Balances.Where(b=>b.StartDate>= sftf.StartDate && b.StartDate <= sftf.EndDate);
            db.Delete(q);
            db.SaveChanges();

            db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);

            DateTime dt = sftf.StartDate.Value.Date;
            while (dt <= sftf.EndDate)
            {
                DateTime dt1 = dt;
                DateTime dt2 = dt1.AddDays(1).AddMilliseconds(-1);

                Data.Balance.CreateBalance(db, dt1, dt2);
                db.SaveChanges();
                dt = dt.AddDays(1);
            }
            
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

        //private void CreateBalance(Data.DatabaseModel database, DateTime dt1, DateTime dt2)
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

        //    foreach(Data.Tank tank in database.Tanks.OrderBy(t=>t.TankNumber))
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
        //        return;

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
        //            if(fuelType == null)
        //                continue;
        //            ftpc.TotalOut = ftc.FuelPumps.Where(fp => fp.FuelType == ftc.FuelType).Sum(fp => fp.TotalSales + fp.TotalLiterCheck);                                   //F_2244B 
        //            ftpc.TotalOutNormalized = ftc.FuelPumps.Where(fp => fp.FuelType == ftc.FuelType).Sum(fp => fp.TotalSalesNormalized + fp.TotalLiterCheckNormalized);     //F_2244C
        //            ftpc.SumTotalOut = ftc.FuelPumps.Where(fp => fp.FuelType == ftc.FuelType).Sum(fp => fp.TotalLiterCheck);                                                //F_2244E 
        //            ftpc.SumTotalOutNormalized = ftc.FuelPumps.Where(fp => fp.FuelType == ftc.FuelType).Sum(fp => fp.TotalLiterCheckNormalized);                            //F_2244F
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
        //        div.Divergence = mov.DaylyMove - balance.PumpsPerFuel.FuelTypes.Where(f => f.FuelType == mov.FuelType).SelectMany(f => f.FuelPumps).Sum(fp => fp.TotalSales);
        //        div.DivergenceNormalized = mov.DaylyMoveNormalized - balance.PumpsPerFuel.FuelTypes.Where(f => f.FuelType == mov.FuelType).SelectMany(f => f.FuelPumps).Sum(fp => fp.TotalSalesNormalized);
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
        //}

        //private void CreateBalance(Data.DatabaseModel database, DateTime dt1, DateTime dt2)
        //{
        //    Guid literCheckType = Data.Implementation.OptionHandler.Instance.GetGuidOption("LiterCheckInvoiceType", Guid.Empty);
        //    Guid deliveryType = Data.Implementation.OptionHandler.Instance.GetGuidOption("DeliveryCheckInvoiceType", Guid.Empty);

        //    Data.UsagePeriod period = null;

        //    period = database.GetUsagePeriod(dt1);

        //    //if (!period.PeriodEnd.HasValue && DateTime.Now.Subtract(period.PeriodStart).TotalDays < 1)
        //    //    return;

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
            

        //    foreach (Data.Tank tank in database.Tanks)
        //    {
        //        var q = tank.TankFillings.Where(tf => tf.TransactionTimeEnd >= balance.TimeStart && tf.TransactionTimeEnd <= balance.TimeEnd);
        //        ASFuelControl.Communication.ReservoirClass reservoir = new ASFuelControl.Communication.ReservoirClass();
        //        reservoir.Capacity = tank.TotalVolume;
        //        reservoir.FuelType = (Communication.Enums.FuelTypeEnum)tank.FuelType.EnumeratorValue;
        //        reservoir.LevelStart = tank.GetLevelAtTime(balance.TimeStart);                              //F_2232_MM
        //        reservoir.LevelEnd = tank.GetLevelAtTime(balance.TimeEnd);                                  //F_2234_MM
        //        reservoir.TankId = tank.TankNumber;
        //        reservoir.TankSerialNumber = tank.TankSerialNumber;
        //        reservoir.TemperatureStart = tank.GetTempmeratureAtTime(balance.TimeStart);                 //F_2232_TEMP
        //        reservoir.TemperatureEnd = tank.GetTempmeratureAtTime(balance.TimeEnd);                     //F_2234_TEMP
        //        reservoir.VolumeStart = tank.GetTankVolume(reservoir.LevelStart);                           //F_2232_VOL
        //        reservoir.VolumeEnd = tank.GetTankVolume(reservoir.LevelEnd);                               //F_2234_VOL
        //        reservoir.VolumeStartNormalized = tank.FuelType.NormalizeVolume(reservoir.VolumeStart, reservoir.TemperatureStart, tank.GetDensityAtTime(balance.TimeStart));   //F_2233
        //        reservoir.VolumeEndNormalized = tank.FuelType.NormalizeVolume(reservoir.VolumeEnd, reservoir.TemperatureEnd, tank.GetDensityAtTime(balance.TimeEnd));           //F_2235

        //        balance.Reservoirs.Reservoirs.Add(reservoir);
        //    }
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
        //        Dictionary<string, decimal> f_2245b = new Dictionary<string, decimal>();
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

        //            ftpc.TotalSales = qs.Sum(s => s.Volume);                                        //F_2244A
        //            ftpc.TotalLiterCheck = n.GetLitercheckSum(balance.TimeStart, balance.TimeEnd);  //F_2244D
        //            ftpc.TotalOut = qs.Sum(s => s.Volume) + ftpc.TotalLiterCheck;                   //F_2244B
        //            ftpc.TotalOutNormalized = qs.Sum(s => s.VolumeNormalized);                      //F_2244C

        //            ftpc.TotalizerDifference = ftpc.TotalizerEnd - ftpc.TotalizerStart;
        //            ftpc.NozzleId = n.OfficialNozzleNumber;

        //            var qd = n.Dispenser.Nozzles.Where(nn => nn.FuelTypeId == n.FuelTypeId);
        //            var qsd = qd.SelectMany(nn => nn.SalesTransactions).Where(s => s.TransactionTimeStamp <= balance.TimeEnd && s.TransactionTimeStamp >= balance.TimeStart);

        //            ftpc.SumTotalOut = qsd.Sum(s => s.Volume);                                                      //F_2244E
        //            ftpc.SumTotalOutNormalized = qsd.Sum(s => s.VolumeNormalized);                                  //F_2244F

        //            ftpc.SumTotalOutTotalizer = ftpc.TotalizerEnd - ftpc.TotalizerStart;                            //F_2245A   qs.Sum(s => s.TotalizerEnd - s.TotalizerStart);
        //            if (!f_2245b.ContainsKey(n.Dispenser.PumpSerialNumber))
        //                f_2245b.Add(n.Dispenser.PumpSerialNumber, ftpc.TotalizerEnd - ftpc.TotalizerStart);
        //            else
        //                f_2245b[n.Dispenser.PumpSerialNumber] = f_2245b[n.Dispenser.PumpSerialNumber] + ftpc.TotalizerEnd - ftpc.TotalizerStart;

        //            ftpc.SumTotalOutTotalizerNormalized = qsd.Sum(s => s.TotalizerEnd - s.TotalizerStart);          //F_2245B

        //            // ********* ftpc.SumTotalOutTotalizerNormalized *************//
        //            var qt = n.SalesTransactions.SelectMany(s => s.TankSales).Select(ts => ts.Tank).Distinct();
        //            Data.Tank tank = qt.First();
        //            ftpc.TankSerialNumber = tank.TankSerialNumber;
        //            ftc.FuelPumps.Add(ftpc);
        //        }

        //        var qGroups = ftc.FuelPumps.GroupBy(fp => fp.FuelPumpSerialNumber).Select(g => new
        //            GroupSum
        //        {
        //            PumpSerialNumber = g.Key,
        //            TotalIn = g.Sum(f => f.TotalizerEnd - f.TotalizerStart),
        //            TotalInNormalized = 0,
        //            TotalOut = g.Sum(f => f.TotalOut),
        //            TotalOutNormalized = g.Sum(f => f.TotalOutNormalized)
        //        });

        //        foreach (ASFuelControl.Communication.FuelTypePumpClass ftpc in ftc.FuelPumps)
        //        {
        //            GroupSum gr = qGroups.Where(g => g.PumpSerialNumber == ftpc.FuelPumpSerialNumber).FirstOrDefault();
        //            if (gr == null)
        //                continue;
        //            Data.Nozzle nozzle = database.Nozzles.Where(n => n.Dispenser.OfficialPumpNumber == int.Parse(ftpc.FuelPumpId) && n.Dispenser.PumpSerialNumber == ftpc.FuelPumpSerialNumber && n.OfficialNozzleNumber == ftpc.NozzleId).FirstOrDefault();
        //            var qd = nozzle.Dispenser.Nozzles.Where(nn => nn.FuelTypeId == nozzle.FuelTypeId);
        //            var qsd = qd.SelectMany(nn => nn.SalesTransactions).Where(s => s.TransactionTimeStamp <= balance.TimeEnd && s.TransactionTimeStamp >= balance.TimeStart);
        //            decimal temp = nozzle.GetAvgTemperature(qsd);
        //            decimal density = nozzle.GetAvgDensity(qsd);
        //            ftpc.SumTotalOutTotalizer = f_2245b[ftpc.FuelPumpSerialNumber];
        //            ftpc.SumTotalOutTotalizerNormalized = nozzle.FuelType.NormalizeVolume(ftpc.SumTotalOutTotalizer, temp, density);
                    
        //        }
        //        if (ftc.FuelPumps.Count > 0)
        //        {
        //            var qsdiff = ft.Nozzles.SelectMany(n => n.SalesTransactions).Where(s => s.TransactionTimeStamp <= balance.TimeEnd && s.TransactionTimeStamp >= balance.TimeStart);
        //            ftc.SumTotalizerDifference = qsdiff.Sum(s => s.TotalizerEnd - s.TotalizerStart);
        //            ftc.SumTotalizerDifferenceNormalized = ft.NormalizeVolume(ftc.SumTotalizerDifference, database.GetAvgTemperature(qsdiff), database.GetAvgDensity(qsdiff, ft));

        //        }
        //        else
        //        {
        //            ftc.SumTotalizerDifference = 0;
        //            ftc.SumTotalizerDifferenceNormalized = 0;
        //        }
        //        ftc.TotalPumpsNumber = ftc.FuelPumps.Count;

        //        balance.PumpsPerFuel.FuelTypes.Add(ftc);
        //        ASFuelControl.Communication.FuelMovementClass mov = new ASFuelControl.Communication.FuelMovementClass();
        //        mov.FuelType = (Communication.Enums.FuelTypeEnum)ft.EnumeratorValue;

        //        var qFill = database.TankFillings.Where(tf => tf.TransactionTimeEnd >= balance.TimeStart && tf.TransactionTime <= balance.TimeEnd && tf.Tank.FuelTypeId == ft.FuelTypeId).ToList();
        //        //varqFillInv = 
        //        var qInvFillings = database.InvoiceLines.Where(il => il.FuelTypeId == ft.FuelTypeId && il.Invoice.InvoiceTypeId == deliveryType && il.Invoice.TransactionDate <= balance.TimeEnd && il.Invoice.TransactionDate >= balance.TimeStart && !il.TankFillingId.HasValue);
        //        var qInvOther = qFill.SelectMany(tf => tf.InvoiceLines).Where(il => il.FuelTypeId == ft.FuelTypeId && il.Invoice.InvoiceTypeId == literCheckType || (il.Invoice.InvoiceTypeId != deliveryType && il.Invoice.InvoiceType.TransactionType == 1));
                
        //        mov.SumIn = qFill.Sum(tf => tf.VolumeReal);                                                                  //F_2236A1
        //        mov.SumAdditionalIn = qInvOther.Sum(tf => tf.Volume);                                                    //F_2236A2
        //        mov.SumInNormalized = qFill.Sum(tf => tf.VolumeRealNormalized);                                        //F_2236B1
        //        mov.SumAdditionalInNormalized = qInvOther.Sum(tf => tf.Volume);                                          //F_2236B2
        //        mov.SumInInvoiced = qInvFillings.Sum(il => il.Volume);                                                   //F_2237
        //        mov.SumInInvoicedNormalized = qInvFillings.Sum(il => il.VolumeNormalized);                               //F_2238
        //        mov.Diff = mov.SumIn - mov.SumInInvoiced;                                                                       //F_2239A
        //        mov.DiffNormalized = mov.SumInNormalized - mov.SumInInvoicedNormalized;                                   //F_2239B

        //        mov.DaylyMove = balance.Reservoirs.Reservoirs.Where(t => t.FuelType == mov.FuelType).Sum(t => t.VolumeStart - t.VolumeEnd) + mov.SumIn + mov.SumAdditionalIn;
        //        mov.DaylyMoveNormalized = balance.Reservoirs.Reservoirs.Where(t => t.FuelType == mov.FuelType).Sum(t => t.VolumeStartNormalized - t.VolumeEndNormalized) + mov.SumInNormalized + mov.SumAdditionalInNormalized;

        //        balance.Movements.FuelMovements.Add(mov);

        //        decimal sumOut = ftc.FuelPumps.Where(f => (int)f.FuelType == ft.EnumeratorValue).Sum(f => f.SumTotalOut);
        //        decimal sumOutNormal = ftc.FuelPumps.Where(f => (int)f.FuelType == ft.EnumeratorValue).Sum(f => f.SumTotalOutNormalized);

        //        ASFuelControl.Communication.FuelTypeDivClass div = new ASFuelControl.Communication.FuelTypeDivClass();
        //        div.FuelType = (Communication.Enums.FuelTypeEnum)ft.EnumeratorValue;
        //        div.Divergence = mov.DaylyMove -sumOut;
        //        div.DivergenceNormalized = mov.DaylyMoveNormalized - sumOutNormal;
        //        if (mov.DaylyMove != 0)
        //            div.Percentage = 100 * (div.Divergence / mov.DaylyMove);
        //        else
        //            div.Percentage = 0;

        //        if (mov.DaylyMoveNormalized != 0)
        //            div.PercentageNormalized = 100 * (div.DivergenceNormalized / mov.DaylyMoveNormalized);
        //        else
        //            div.PercentageNormalized = 0;

        //        if (ftc.FuelPumps.Count > 0)
        //            balance.Divergences.Divergences.Add(div);
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
        //    Data.TankFilling lastFil = database.TankFillings.Where(f=>f.TransactionTime <= balance.TimeEnd).OrderBy(f => f.TransactionTimeEnd).LastOrDefault();
        //    if (lastFil != null)
        //        bal.LastFilling = lastFil.TankFillingId;

        //    Data.SalesTransaction lastSale = database.SalesTransactions.Where(s => s.TransactionTimeStamp <= balance.TimeEnd).OrderBy(f => f.TransactionTimeStamp).LastOrDefault();
        //    if (lastSale != null)
        //        bal.LastSale = lastSale.SalesTransactionId;
        //}

        private class GroupSum
        {
            public string PumpSerialNumber { set; get; }
            public decimal TotalOut { set; get; }
            public decimal TotalOutNormalized { set; get; }
            public decimal TotalIn { set; get; }
            public decimal TotalInNormalized { set; get; }
        }

        private void stopControllerButton_Click(object sender, EventArgs e)
        {
            Program.ApplicationMainForm.ThreadControllerInstance.StopThreads(true);
            this.stopControllerButton.DataBindings[0].ReadValue();
            this.startControllerButton.DataBindings[0].ReadValue();
        }

        private void startControllerButton_Click(object sender, EventArgs e)
        {
            Program.ApplicationMainForm.ThreadControllerInstance.StartThreads();
            this.stopControllerButton.DataBindings[0].ReadValue();
            this.startControllerButton.DataBindings[0].ReadValue();
        }

        private void radButton5_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void radButton2_Click(object sender, EventArgs e)
        {
            UI.SelectionForms.AttachFillingForm aff = new SelectionForms.AttachFillingForm();
            aff.Show();
        }

        private void radButton6_Click(object sender, EventArgs e)
        {
            Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
            Data.Balance bal = db.Balances.FirstOrDefault();
            if (bal == null)
                return;
            Threads.PrintAgent agent = new Threads.PrintAgent();
            agent.PrintToPrinter(bal);
        }
    }
}
