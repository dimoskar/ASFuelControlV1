using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using ASFuelControl.VirtualDevices;
using System.IO;
using Telerik.OpenAccess;
using ASFuelControl.Logging;
using System.Collections.Concurrent;

namespace ASFuelControl.Windows.Threads
{
    /// <summary>
    /// Class that combines and handles all secondary threads of the Application. This class is acting as an interface between the VirtualDevices of the Tank, Fuelpoints, Nozzles and Alerts and the Database
    /// </summary>
    public class ThreadController
    {
        #region Event Definitions

        public event EventHandler SubmissionFailed;
        public event EventHandler SubmissionSuccess;
        public event EventHandler<WorkFlow.AlarmRaisedEventArgs> AlarmRaised;
        public event EventHandler<QueryResolveAlarmArgs> QueryResolveAlarm;
        public event EventHandler<TimerStartEventArgs> QueryStartTimer;
        public event EventHandler<AlarmResolvedArgs> AlarmResolved;
        public event EventHandler QueryDeviceRefresh;
        public event EventHandler ConntrollerConnectionFailed;
        public event EventHandler ControllerConnectionSuccess;
        public event EventHandler<TankFillingAvaliableArgs> TankFillingAvaliableEvent;
        public event EventHandler RefreshAlerts;
        public event EventHandler OTPStatusChanged;
        #endregion

        #region private variables

        public Dictionary<string, EnrollData> fleetDataDict = new Dictionary<string, EnrollData>();
        private List<ConntrollerDescription> failedControllers = new List<ConntrollerDescription>();
        private Dictionary<Common.IController, string> controllerNames = new Dictionary<Common.IController, string>();
        private ControllerThread controllerThread;// = new ControllerThread();
        private SendAlertsThread sendingThread;// = new SendAlertsThread();
        private TankCheckThread tankThread;
        private FleetManagmentThread fleetThread = new FleetManagmentThread();
        private Data.DatabaseModel database;// = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
        private Dictionary<Guid, Common.IController> controllers = new Dictionary<Guid, Common.IController>();
        private ConcurrentBag<SendLog> logToSave = new ConcurrentBag<SendLog>();

        //private PrintHandler printHandler = new PrintHandler();

        private PrintAgent printAgent = null;// = new PrintAgent();

        private List<Guid> processedInvoices = new List<Guid>();
        private string outFolder;
        private bool restartDatabase = false;
        private int restartDatabaseIndex = 0;
        

        #endregion

        public bool OTPStatus
        {
            get
            {
                return this.controllerThread.OTPStatus;
            }
        }

        public bool IsRunning
        {
            set;
            get;
        }

        public bool StartedOnce { set;get; }

        public bool IsStopped
        {
            get { return !this.IsRunning; }
        }

        public ThreadController()
        {
            
        }

        #region public methods

        public bool HasTankAlerts(Guid tankId)
        {
            return this.database.SystemEvents.Where(s => s.TankId == tankId && !s.ResolvedDate.HasValue &&
                (s.AlarmType == (int)Common.Enumerators.AlarmEnum.FuelDecrease || s.AlarmType == (int)Common.Enumerators.AlarmEnum.FuelIncrease)).Count() > 0;
        }

        public void ApplyPrices()
        {
            if (this.controllerThread == null)
                return;
            lock (this.controllerThread)
            {
                this.controllerThread.PriceChanged = true;
            }
        }

        public VirtualTank[] GetTanks()
        {
            if (this.controllerThread == null)
                return new VirtualTank[] { };
            return this.controllerThread.GetTanks();
        }

        public VirtualDispenser[] GetDispensers()
        {
            if(this.controllerThread == null)
                return new VirtualDispenser[] { };
            return this.controllerThread.GetDispensers();
        }

        public Data.Dispenser GetDispenser(VirtualDispenser disp)
        {
            return this.database.Dispensers.Where(d => d.DispenserId == disp.DispenserId).FirstOrDefault();
        }

        public Data.Tank GetTank(VirtualTank tank)
        {
            return this.database.Tanks.Where(d => d.TankId == tank.TankId).FirstOrDefault();
        }

        public OTPConsoleController[] GetOtpControllers()
        {
            return this.controllerThread.GetOTPControlers();
        }

        public void CheckForDatabaseUpdate()
        {
            int version = this.GetDatabaseVersion();

            if (version == 0)
            {
                version = 1;
            }
            for (int i = version; i < Program.CurrentDBVersion; i++)
            {
                this.UpdateDatabase(i + 1);
                if (i == 21)
                {
                    Data.DatabaseModel dbDummy = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
                    System.Xml.Linq.XDocument doc = System.Xml.Linq.XDocument.Parse(Properties.Resources.OilCompanies);
                    List<System.Xml.Linq.XElement> list = doc.Descendants("Image").ToList();
                    foreach (System.Xml.Linq.XElement el in list)
                    {
                        string id = el.Attribute("Id").Value;
                        string name = el.Attribute("Name").Value;
                        string image = el.Attribute("ImageSource").Value;
                        Data.OilCompany oc = new Data.OilCompany();
                        oc.OilCompanyId = Guid.Parse(id);
                        oc.Name = name;
                        oc.Logo = this.StringToBuffer(image);
                        dbDummy.Add(oc);
                    }
                    dbDummy.SaveChanges();
                }
            }
            Data.Implementation.OptionHandler.ConnectionString = Properties.Settings.Default.DBConnection;

            this.database = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
            this.database.AlertChecker = Threads.AlertChecker.Instance;
            if (this.database.Options.Where(o => o.OptionKey == "OptionsEncrypted").FirstOrDefault() == null)
            {
                List<Data.Option> options = this.database.Options.ToList();
                foreach (Data.Option option in options)
                {
                    option.OptionValue = AESEncryption.Encrypt(option.OptionValue, "Exedron@#");
                }

                List<Data.ApplicationUser> users = this.database.ApplicationUsers.ToList();
                foreach (Data.ApplicationUser user in users)
                {
                    user.PasswordEncrypted = AESEncryption.Encrypt(user.Password, "Exedron@#");
                    user.Password = "";
                }

                Data.Implementation.OptionHandler.Instance.SetOption(this.database, "OptionsEncrypted", true);
                this.database.SaveChanges();
            }

            Data.Implementation.OptionHandler.Instance.SetOption(this.database, "DBVersion", Program.CurrentDBVersion);
        }

        public void StartThreads()
        {
            try
            {
                this.IsRunning = true;
                this.StartedOnce = true;
                if (!System.IO.Directory.Exists(System.Environment.CurrentDirectory + "\\Logs" + "\\SalesHandler"))
                    System.IO.Directory.CreateDirectory(System.Environment.CurrentDirectory + "\\Logs" + "\\SalesHandler");

                this.controllerThread = new ControllerThread();
                this.sendingThread = new SendAlertsThread();
                this.tankThread = new TankCheckThread();
                this.fleetThread = new FleetManagmentThread();
                this.sendingThread.BalanceCreated += new EventHandler(sendingThread_BalanceCreated);
                this.sendingThread.SubmissionFailed += new EventHandler(sendingThread_SubmissionFailed);
                this.sendingThread.SubmissionSuccess += new EventHandler(sendingThread_SubmissionSuccess);

                printAgent = new PrintAgent();
                //Data.Implementation.AlertChecker.Instance.Database = this.database;
                this.controllers.Clear();
                try
                {
                    this.InitializeControllers();

                    Data.Shift currentShift = this.database.Shifts.Where(s => !s.ShiftEnd.HasValue).FirstOrDefault();
                    if (currentShift != null)
                    {
                        Program.CurrentShiftId = currentShift.ShiftId;
                        Program.CurrentUserId = currentShift.ApplicationUserId;
                        Program.CurrentUserName = currentShift.ApplicationUser.UserName;
                        Program.CurrentUserLevel = (Common.Enumerators.ApplicationUserLevelEnum)currentShift.ApplicationUser.UserLevel;
                    }

                    this.sendingThread.PrintAlert += new EventHandler<PrintAlertEventArgs>(sendingThread_PrintAlert);

                    this.printAgent.StartThread();
                    this.controllerThread.HaltThread = false;
                    this.controllerThread.StartControllers();
                    this.sendingThread.StartThread();
                    this.tankThread.StartThread();
                    this.fleetThread.Start();
                    if (this.QueryDeviceRefresh != null)
                        this.QueryDeviceRefresh(this, new EventArgs());

                    this.CheckAndUpdateVersion();
                }
                catch (Exception ex)
                {
                    Logger.Instance.LogToFile("Controller Initialization Exception", ex);
                    Common.Logger.Instance.Error(ex.Message, "", 2);
                    Telerik.WinControls.RadMessageBox.Show("Ελέξτε τις ρυθμίσεις των δεξαμενών και των αντλιών", "Σφάλμα εκκίνησης Κονσόλας", System.Windows.Forms.MessageBoxButtons.OK, Telerik.WinControls.RadMessageIcon.Exclamation);
                }
            }
            catch(Exception ex1)
            {
                Logger.Instance.LogToFile("Controller Initialization Exception", ex1);
            }
        }

        void sendingThread_SubmissionSuccess(object sender, EventArgs e)
        {
            //this.LogSend(sender, 1);

            if(this.SubmissionSuccess != null)
                this.SubmissionSuccess(sender, new EventArgs());
        }

        void sendingThread_SubmissionFailed(object sender, EventArgs e)
        {
            //this.LogSend(sender, 0);

            if (this.SubmissionFailed != null)
                this.SubmissionFailed(sender, e);
        }

        //private void LogSend(object sendObject, int status)
        //{
        //    string desc = "";
        //    Common.Enumerators.SendLogActionEnum action = Common.Enumerators.SendLogActionEnum.Balance;
        //    string identity = "";
        //    switch (sendObject.GetType().Name)
        //    {
        //        case "SystemEvent":
        //            Data.SystemEvent se = sendObject as Data.SystemEvent;
        //            desc = "Συναγερμός : " + se.Message + " " + se.DeviceDescription + " " + se.EventDate.ToString("dd/MM/yyyy HH:mm");
        //            action = Common.Enumerators.SendLogActionEnum.SystemEvent;
        //            identity = se.EventId.ToString();
        //            break;
        //        case "TankFilling":
        //            Data.TankFilling tf = sendObject as Data.TankFilling;
        //            desc = "Παραλαβή : " + tf.Tank.Description + " " + tf.TransactionTimeEnd.ToString("dd/MM/yyyy HH:mm");
        //            action = Common.Enumerators.SendLogActionEnum.TankFilling;
        //            identity = tf.TankFillingId.ToString();
        //            break;
        //        case "SalesTransaction":
        //            Data.SalesTransaction st = sendObject as Data.SalesTransaction;
        //            desc = "Πώληση : " + st.Nozzle.Description + " " + st.TransactionTimeStamp.ToString("dd/MM/yyyy HH:mm");
        //            action = Common.Enumerators.SendLogActionEnum.SalesTransaction;
        //            identity = st.SalesTransactionId.ToString();
        //            break;
        //        case "FuelTypePrice":
        //            Data.FuelTypePrice ftp = sendObject as Data.FuelTypePrice;
        //            desc = "Αλλαγή Τιμής : " + ftp.FuelType.Name + " " + ftp.ChangeDate.ToString("dd/MM/yyyy HH:mm");
        //            action = Common.Enumerators.SendLogActionEnum.FuelTypePrice;
        //            identity = ftp.FuelTypePriceId.ToString();
        //            break;
        //        case "ChangePriceClass":
        //            Communication.ChangePriceClass cpc = sendObject as Communication.ChangePriceClass;
        //            desc = "Αλλαγή Τιμής : " + cpc.FuelType.ToString() + " " + cpc.Price.ToString("N3") + " " + cpc.ChangeTime.ToString("dd/MM/yyyy HH:mm:ss");
        //            action = Common.Enumerators.SendLogActionEnum.FuelTypePrice;
        //            identity = desc;
        //            break;
        //        case "IncomeRecieptClass":
        //            Communication.IncomeRecieptClass irc = sendObject as Communication.IncomeRecieptClass;
        //            desc = "Πώληση : " + irc.FuelType.ToString() + " " + irc.TotalValue.ToString("N2") + " " + irc.PublishDateTime.ToString("dd/MM/yyyy HH:mm:ss:fff") + " " + irc.PumpSerialNumber;
        //            action = Common.Enumerators.SendLogActionEnum.SalesTransaction;
        //            identity = desc;
        //            break;
        //        case "BalanceClass":
        //            Communication.BalanceClass bc = sendObject as Communication.BalanceClass;
        //            desc = "Ισοζύγιο : " + bc.TimeStart.ToString("dd/MM/yyyy HH:mm") + " - " + bc.TimeEnd.ToString("dd/MM/yyyy HH:mm");
        //            action = Common.Enumerators.SendLogActionEnum.Balance;
        //            identity = desc;
        //            break;
        //        case "Balance":
        //            Data.Balance bc1 = sendObject as Data.Balance;
        //            desc = "Ισοζύγιο : " + bc1.StartDate.ToString("dd/MM/yyyy HH:mm") + " - " + bc1.EndDate.ToString("dd/MM/yyyy HH:mm");
        //            action = Common.Enumerators.SendLogActionEnum.Balance;
        //            identity = bc1.BalanceId.ToString();
        //            break;
        //    }
        //    //Console.WriteLine(sendObject.GetType().Name);
        //    SendLog log = new SendLog();
        //    log.Action = action;
        //    log.Data = desc;
        //    log.Identity = identity;
        //    log.Status = status;
        //    log.SentTime = DateTime.Now;
        //    this.logToSave.Add(log);
            
        //}

        public void StopThreads(bool isAuthorized)
        {
            this.IsRunning = false;

            if (isAuthorized)
            {
                Data.SystemEvent ev = new Data.SystemEvent();
                ev.EventId = Guid.NewGuid();
                ev.EventDate = DateTime.Now;
                ev.EventType = (int)Common.Enumerators.AlertTypeEnum.ProgramTermination;
                ev.Message = "Τεματισμός Εφαρμογής από τεχνικό";

                this.database.Add(ev);
                this.database.SaveChanges();
            }
            else
            {
                Data.SystemEvent ev = new Data.SystemEvent();
                ev.EventId = Guid.NewGuid();
                ev.EventDate = DateTime.Now;
                ev.EventType = (int)Common.Enumerators.AlertTypeEnum.ProgramTermination;
                ev.Message = "Τεματισμός Εφαρμογής";
                try
                {
                    this.database.Add(ev);
                    this.database.SaveChanges();
                }
                catch (Exception ex)
                {
                    Logger.Instance.LogToFile("StopThreads", ex);
                }
            }
            this.controllerThread.StopControllers();
            this.controllerThread.HaltThread = true;
            this.fleetThread.Stop();
            this.sendingThread.StopThread();
            this.tankThread.StopThread();
            this.printAgent.StopThread();
            foreach (OTPConsoleController opt in this.controllerThread.GetOTPControlers())
                opt.CloseServer();
        }

        List<VirtualDispenser> lockedDispensers = new List<VirtualDispenser>();
        public void LockDispensers()
        {
            var dispensers = this.GetDispensers();
            lockedDispensers.AddRange(dispensers.Where(d => d.DeviceLocked).ToArray());
            foreach (var dispenser in dispensers)
            {
                dispenser.DeviceLocked = true;
            }
        }

        public void UnlockDispensers()
        {
            var dispensers = this.GetDispensers();
            foreach (var dispenser in dispensers)
            {
                if (lockedDispensers.Contains(dispenser))
                    continue;
                dispenser.DeviceLocked = false;
            }
            lockedDispensers.Clear();
        }

        /// <summary>
        /// Creates an sale for the nozzle identified by nozzleId
        /// </summary>
        /// <param name="nozzleId"></param>
        /// <param name="startTotal"></param>
        /// <param name="endTotal"></param>
        public void CreateSale(Guid nozzleId, decimal startTotal, decimal endTotal)
        {
            try
            {
                Data.Nozzle nozzle = this.database.Nozzles.Where(n => n.NozzleId == nozzleId).FirstOrDefault();
                if (nozzle == null)
                {
                    Logger.Instance.LogToFile("controllerThread_SaleAvaliable", string.Format("Nozzle:{0} not found", nozzleId));
                    return;
                }
                Common.Sales.SaleData sale = new Common.Sales.SaleData();
                sale.ErrorResolving = false;
                sale.FuelTypeDescription = nozzle.FuelType.Name;
                try
                {
                    sale.InvoiceTypeId = nozzle.Dispenser.InvoicePrints.First().DefaultInvoiceType;
                }
                catch(Exception ex)
                {
                    Logging.Logger.Instance.LogToFile("ThreadController::CreateSale", ex);
                    return;
                }
                sale.IsOnSale = false;
                sale.LiterCheck = false;
                sale.NozzleId = nozzleId;
                sale.NozzleNumber = nozzle.OfficialNozzleNumber;
                sale.SaleCompleted = true;
                sale.SaleEndTime = DateTime.Now;
                sale.TotalizerStart = startTotal;
                sale.TotalizerEnd = endTotal;
                sale.TotalVolume = (endTotal - startTotal) / 100;
                sale.UnitPrice = nozzle.FuelType.GetCurrentPrice();
                sale.TotalPrice = sale.UnitPrice * sale.TotalVolume;

                List<Common.Sales.TankSaleData> tds = new List<Common.Sales.TankSaleData>();
                foreach(Data.NozzleFlow nf in nozzle.NozzleFlows)
                {
                    if (nf.FlowState != 1)
                        continue;
                    Common.Sales.TankSaleData td = new Common.Sales.TankSaleData();
                    td.StartLevel = nf.Tank.FuelLevel;
                    td.StartTemperature = nf.Tank.Temperatire;
                    td.StartWaterLevel = nf.Tank.WaterLevel;
                    td.EndLevel = nf.Tank.FuelLevel;
                    td.EndTemperature = nf.Tank.Temperatire;
                    td.EndWaterLevel = nf.Tank.WaterLevel;
                    td.ReadyToProcess = true;
                    td.TankId = nf.TankId;

                    tds.Add(td);
                }
                sale.TankData = tds.ToArray();

                this.controllerThread.AddSale(sale);
            }
            catch (Exception ex)
            {
                Logging.Logger.Instance.LogToFile("ThreadController::CreateSale", ex);
            }
        }

        public void RedefineOTPComntrollers()
        {
            this.controllerThread.ClearOTPConsoles();

            VirtualDispenser[] dispensers = this.controllerThread.GetDispensers();

            List<Data.OutdoorPaymentTerminal> otpTerminals = this.database.OutdoorPaymentTerminals.ToList().ToList();
            foreach (Data.OutdoorPaymentTerminal terminal in otpTerminals)
            {
                this.controllerThread.AddOTPConsole(terminal.TerminalId, terminal.ServerIp, terminal.ServerPort, terminal.ClientIp, terminal.ClientPort);
                Data.OutdoorPaymentTerminalNozzle[] nozzles = terminal.OutdoorPaymentTerminalNozzles.ToArray();
                VirtualDispenser[] disps = dispensers.Where(d => terminal.OutdoorPaymentTerminalNozzles.Select(n => n.Nozzle.DispenserId).Contains(d.DispenserId)).Distinct().ToArray();

                foreach (VirtualDispenser disp in disps)
                {
                    Guid[] nozzleIds = disp.Nozzles.Where(n => terminal.OutdoorPaymentTerminalNozzles.Select(nz => nz.NozzleId).Contains(n.NozzleId)).
                        Select(n => n.NozzleId).ToArray();
                    this.controllerThread.AddOTPDispenser(terminal.TerminalId, disp, nozzleIds);
                }
            }
        }

        #endregion

        #region private methods

        private byte[] StringToBuffer(string imageString)
        {

            if (imageString == null)
                throw new ArgumentNullException("imageString");

            byte[] array = Convert.FromBase64String(imageString);
            return array;
        }

        /// <summary>
        /// Checks and update the version of the current application
        /// </summary>
        private void CheckAndUpdateVersion()
        {
            string curVersion = Data.Implementation.OptionHandler.Instance.GetOption("SoftwareVersion");

            string swVersion = Program.MainVersion.ToString() + "." + Program.SubVersion.ToString() + "." + Program.Revision.ToString();
            if (curVersion != swVersion)
            {
                bool updated = this.sendingThread.SendSoftwareChange(swVersion);
                if (updated)
                    Data.Implementation.OptionHandler.Instance.SetOption("SoftwareVersion", swVersion);
            }
        }

        /// <summary>
        /// Initializes all controllers for tanks and fuelpoints
        /// </summary>
        private void InitializeControllers()
        {
            this.outFolder = Data.Implementation.OptionHandler.Instance.GetOption("Samtec_OutFolder");
            foreach (Data.CommunicationController controller in this.database.CommunicationControllers)
            {
                Common.IController controllerInterFace = this.CreateControllerInterface(controller);
                if (controllerInterFace == null)
                    continue;
                controllerInterFace.EuromatIp = controller.EuromatIpAddress;
                controllerInterFace.EuromatPort = controller.EuromatPort.HasValue ? controller.EuromatPort.Value : 0;

                controllerInterFace.EuromatEnabled = controller.EuromatEnabled.HasValue ? controller.EuromatEnabled.Value : false;
                controllers.Add(controller.CommunicationControllerId, controllerInterFace);
            }
            List<Data.Tank> dbTanks = this.database.Tanks.ToList().Where(d => !d.Removed).ToList();
            foreach (Data.Tank tank in dbTanks)
            {
                Guid controllerId = this.controllers.Keys.Where(k => k == tank.CommunicationControllerId).FirstOrDefault();
                if (controllerId == Guid.Empty)
                    continue;
                if (tank.Removed)
                    continue;
                Common.IController controllerInterFace = this.controllers.Where(c => c.Key == controllerId).Select(c => c.Value).FirstOrDefault();

                //VirtualDevices.VirtualTank vtank = this.controllerThread.AddTankWorkFlow(controllerInterFace, tank.TankId, tank.Channel, tank.Address, (Common.Enumerators.TankStatusEnum)tank.PhysicalState, tank.PreviousState);
                Common.Enumerators.TankStatusEnum curStatus = tank.PhysicalState > 1 ? (Common.Enumerators.TankStatusEnum)tank.PhysicalState : Common.Enumerators.TankStatusEnum.Offline;
                VirtualDevices.VirtualTank vtank = this.controllerThread.AddTankWorkFlow(controllerInterFace, tank.TankId, tank.Channel, tank.Address, curStatus, tank.PreviousState);
                tank.PhysicalState = 0;
                List<Data.SystemEvent> tankAlerts = this.database.SystemEvents.Where(se => se.TankId == tank.TankId && !se.ResolvedDate.HasValue).ToList();
                //AlertHandler.AddExistingTankAlerts(vtank, tankAlerts);
                vtank.TankAlertMargin = tank.TankAlertMargin;
                if (vtank.TankStatus == Common.Enumerators.TankStatusEnum.Filling || vtank.TankStatus == Common.Enumerators.TankStatusEnum.FuelExtraction || vtank.TankStatus == Common.Enumerators.TankStatusEnum.Waiting ||
                    vtank.TankStatus == Common.Enumerators.TankStatusEnum.FillingInit || vtank.TankStatus == Common.Enumerators.TankStatusEnum.FuelExtractionInit)
                {
                    vtank.InvoiceLineId = tank.FillingInvoiceId;
                    vtank.InvoiceTypeId = tank.InvoiceTypeId;
                    vtank.IsLiterCheck = tank.IsLiterCheck;
                    vtank.InitializeFilling = tank.InitializeFilling;
                    vtank.WaitingStarted = tank.WaitingStarted;
                    vtank.WaitingShouldEnd = tank.WaitingShouldEnd;
                    vtank.VehicleId = tank.VehicleId;
                    vtank.FillingFuelTypeId = tank.FillingFuelTypeId;
                    vtank.DeliveryStarted = tank.DeliveryStrarted;
                    vtank.LastCalculatedStart = tank.FuelLevel;
                    vtank.FillingStartTankLevel = tank.FillingStartLevel;
                }
                
                if (tank.Titrimetries.Count > 0)
                    vtank.TitrimetryLevels = tank.Titrimetries.OrderBy(t=>t.TitrationDate).LastOrDefault().TitrimetryLevels.
                        Select(t => new VirtualTitrimetryLevel(t.Volume.Value, t.Height.Value, t.UncertaintyVolume.Value, t.UncertaintyPercent.Value)).ToArray();
                if (tank.AlarmThreshold.HasValue)
                    vtank.AlarmThreshold = tank.AlarmThreshold.Value;
                else
                    vtank.AlarmThreshold = 4;

                vtank.DeliveryTime = Data.Implementation.OptionHandler.Instance.GetIntOption("DeliveryWaitingTime", 900) * 1000;
                vtank.LiterCheckTime = Data.Implementation.OptionHandler.Instance.GetIntOption("LiterCheckWaitingTime", 15) * 1000;
                //vtank.LastFuelHeight = tank.GetLastValidLevel();
                //vtank.LastTemperature = tank.GetLastValidTemperatur();
                //vtank.LastWaterHeight = tank.GetLastValidWaterLevel();
                vtank.CurrentTemperature = tank.Temperatire;
                decimal[] validValues = tank.GetLastValidValues();
                vtank.LastFuelHeight = validValues[0];
                vtank.LastWaterHeight = validValues[1];
                vtank.LastTemperature = validValues[2];
                if (tank.FuelType != null)
                {
                    vtank.BaseDensity = tank.FuelType.BaseDensity;
                    vtank.CurrentDensity = tank.CurrentDensity;
                    vtank.ThermalCoefficient = tank.FuelType.ThermalCoeficient;
                    vtank.FuelTypeDescription = tank.FuelType.Name;
                    vtank.FuelTypeShort = tank.FuelType.Code;
                    vtank.FuelTypeId = tank.FuelTypeId;
                    vtank.BaseColor = tank.FuelType.Color.HasValue ? tank.FuelType.Color.Value : 0;
                }
                else
                {
                    vtank.BaseColor = 0;
                    vtank.FuelTypeDescription = "";
                    vtank.FuelTypeShort = "";
                }
                vtank.TankNumber = tank.TankNumber;
                vtank.SerialNumber = tank.TankSerialNumber;
                vtank.MaxHeight = tank.MaxFuelHeight;
                vtank.MinHeight = tank.MinFuelHeight;
                vtank.MinAllowedHeight = tank.GaugeMinimumHeight;
                vtank.MaxWaterHeight = tank.MaxWaterHeight;
                vtank.PriceAverage = tank.GetLastPrice();
                vtank.TotalVolume = tank.TotalVolume;
                vtank.CurrentTemperature = tank.Temperatire;
                vtank.FuelOffset = tank.OffsetVolume;
                vtank.WaterOffset = tank.OffestWater;
                vtank.OrderLimit = tank.OrderLimit.HasValue ? tank.OrderLimit.Value : 0;
                vtank.CurrentFuelLevel = tank.FuelLevel;
                vtank.CurrentWaterLevel = tank.WaterLevel;
                vtank.LastValuesUpdate = DateTime.Now;
                vtank.LastSalesVolumeDifference = tank.LastSalesDifference(this.database, 10);
                vtank.IsVirtualTank = tank.IsVirtual.HasValue ? tank.IsVirtual.Value : false;
            }
            List<VirtualDispenser> dispensers = new List<VirtualDispenser>();
            List<Data.Dispenser> dbDispensers = this.database.Dispensers.ToList().Where(d=>!d.Removed).ToList();
            foreach (Data.Dispenser dispenser in dbDispensers)
            {
                Guid controllerId = this.controllers.Keys.Where(k => k == dispenser.CommunicationControllerId).FirstOrDefault();
                if (controllerId == Guid.Empty)
                    continue;
                if (dispenser.Removed)
                    continue;
                Common.IController controllerInterFace = this.controllers.Where(c => c.Key == controllerId).Select(c => c.Value).FirstOrDefault();
                VirtualDispenser vDispenser = this.controllerThread.AddDispenserWorkFlow(controllerInterFace, dispenser.DispenserId, dispenser.Channel, dispenser.PhysicalAddress);
                if (dispenser.EuromatEnabled)
                {
                    vDispenser.EuromatNumber = dispenser.EuromatDispenserNumber;
                    vDispenser.EuromatServerIp = controllerInterFace.EuromatIp;
                    vDispenser.EuromatServerPort = controllerInterFace.EuromatPort;
                }
                vDispenser.SaleTimeOut = dispenser.SaleTimeOut;
                vDispenser.FleetSales = dispenser.FleetSales;
                vDispenser.Status = Common.Enumerators.FuelPointStatusEnum.Offline;// (Common.Enumerators.FuelPointStatusEnum)dispenser.PhysicalState;
                dispenser.PhysicalState = 0;
                vDispenser.OfficialNumber = dispenser.OfficialPumpNumber;
                vDispenser.IsValid = dispenser.IsValid;
                vDispenser.DispenserNumber = dispenser.DispenserNumber;
                vDispenser.SerialNumber = dispenser.PumpSerialNumber;
                vDispenser.DecimalPlaces = dispenser.DecimalPlaces.HasValue ? dispenser.DecimalPlaces.Value : 2;
                vDispenser.UnitPriceDecimalPlaces = dispenser.UnitPriceDecimalPlaces.HasValue ? dispenser.UnitPriceDecimalPlaces.Value : 2;
                vDispenser.VolumeDecimalPlaces = dispenser.VolumeDecimalPlaces.HasValue ? dispenser.VolumeDecimalPlaces.Value : 2;
                vDispenser.TotalDecimalPlaces = dispenser.TotalVolumeDecimalPlaces;

                dispensers.Add(vDispenser);

            }
            database.SaveChanges();
            foreach (Data.Dispenser dispenser in this.database.Dispensers)
            {
                foreach (Data.Nozzle nozzle in dispenser.Nozzles)
                {
                    VirtualDispenser vDisp = dispensers.Where(d => d.DispenserId == nozzle.DispenserId).FirstOrDefault();
                    if (vDisp == null)
                        continue;
                    VirtualNozzle vNozzle = this.controllerThread.AddNozzle(dispenser.DispenserId, nozzle.NozzleId, nozzle.OrderId, nozzle.OfficialNozzleNumber, nozzle.NozzleIndex.Value, nozzle.NozzleState, nozzle.NozzleSocket);
                    vNozzle.ParentDispenser = vDisp;
                    vNozzle.CurrentSaleUnitPrice = nozzle.FuelType.CurrentPrice;
                    if (nozzle.FuelType != null)
                    {
                        vNozzle.FuelTypeDescription = nozzle.FuelType.Name;
                        vNozzle.FuelTypeShortDescription = nozzle.FuelType.Code;
                        vNozzle.FuelCode = nozzle.FuelType.EnumeratorValue.ToString();
                        vNozzle.FuelColor = nozzle.FuelType.Color.HasValue ? nozzle.FuelType.Color.Value : 0;
                    }
                    vNozzle.NozzleIndex = nozzle.NozzleIndex.Value ;
                    vNozzle.TotalVolumeCounter = nozzle.TotalCounter;
                    vNozzle.LastVolumeCounter = nozzle.LastValidTotalizer;
                    vNozzle.SerialNumber = nozzle.SerialNumber;
                    var q1 = this.database.SalesTransactions.Where(s => s.NozzleId == nozzle.NozzleId && s.InvalidSale.HasValue && s.InvalidSale.Value && s.TransactionTimeStamp.Date == DateTime.Today);
                    int invalidSales = q1.Count();
                    if (invalidSales > 0)
                    {
                        vDisp.HasInvalidSale = true;
                        Guid[] invSales = this.database.SalesTransactions.Where(s => s.NozzleId == nozzle.NozzleId && s.InvalidSale.HasValue && s.InvalidSale.Value && s.TransactionTimeStamp.Date == DateTime.Today).Select(s => s.SalesTransactionId).ToArray();
                        vDisp.InvalidSales.AddRange(invSales);
                    }
                }
                VirtualDispenser vDispenser = dispensers.Where(d => d.DispenserId == dispenser.DispenserId).FirstOrDefault();
                if (vDispenser == null)
                    continue;
                List<VirtualFleetDispenser> list = new List<VirtualFleetDispenser>();
                foreach (Data.FleetManagerDispenser fd in dispenser.FleetManagerDispensers)
                {
                    
                    VirtualFleetDispenser vfd = new VirtualFleetDispenser();
                    if(fd.FleetManagmentCotroller.ControlerType.HasValue && fd.FleetManagmentCotroller.ControlerType.Value == 1)
                        vfd.ComPort = fd.FleetManagmentCotroller.DeviceIp;
                    else
                        vfd.ComPort = fd.FleetManagmentCotroller.ComPort;
                    vfd.InvoiceTypeId = fd.InvoiceTypeId;
                    list.Add(vfd);
                    List<VirtualDevices.FleetDispenserSchedule> schList = new List<FleetDispenserSchedule>();
                    foreach (Data.FleetManagmentSchedule sch in fd.FleetManagmentSchedules)
                    {
                        VirtualDevices.FleetDispenserSchedule vsch = new FleetDispenserSchedule();
                        vsch.DayMask = sch.DayMask;
                        vsch.TimeFrom = sch.TimeFrom;
                        vsch.TimeTo = sch.TimeTo;
                        schList.Add(vsch);

                    }
                    vfd.Schedules = schList.ToArray();
                }
                
                vDispenser.FleetDispensers = list.ToArray();
            }
            foreach (Data.FleetManagmentCotroller fc in this.database.FleetManagmentCotrollers)
            {
                this.fleetThread.AddVontroller(fc);
            }

            this.RedefineOTPComntrollers();
            //List<Data.OutdoorPaymentTerminal> otpTerminals = this.database.OutdoorPaymentTerminals.ToList().ToList();
            //foreach (Data.OutdoorPaymentTerminal terminal in otpTerminals)
            //{
            //    this.controllerThread.AddOTPConsole(terminal.TerminalId, terminal.ServerIp, terminal.ServerPort, terminal.ClientIp, terminal.ClientPort);
            //    Data.OutdoorPaymentTerminalNozzle[] nozzles = terminal.OutdoorPaymentTerminalNozzles.ToArray();
            //    VirtualDispenser[] disps = dispensers.Where(d => terminal.OutdoorPaymentTerminalNozzles.Select(n => n.Nozzle.DispenserId).Contains(d.DispenserId)).Distinct().ToArray();

            //    foreach(VirtualDispenser disp in disps)
            //    {
            //        Guid[] nozzleIds = disp.Nozzles.Where(n=>terminal.OutdoorPaymentTerminalNozzles.Select(nz => nz.NozzleId).Contains(n.NozzleId)).
            //            Select(n => n.NozzleId).ToArray();
            //        this.controllerThread.AddOTPDispenser(terminal.TerminalId, disp, nozzleIds);
            //    }
            //}

            this.fleetThread.DataRecieved += new EventHandler<FleetManagementDataRecievedArgs>(fleetThread_DataRecieved);
            foreach (Data.Tank tank in this.database.Tanks)
            {
                foreach (Data.NozzleFlow nf in tank.NozzleFlows)
                {
                    if (nf.FlowState == 0)
                        continue;
                    this.controllerThread.AddTankNozzle(nf.TankId, nf.NozzleId);
                }
            }
            this.controllerThread.SaleAvaliable += new EventHandler<WorkFlow.SaleCompletedArgs>(controllerThread_SaleAvaliable);
            this.controllerThread.TankFillingAvaliable += new EventHandler(controllerThread_TankFillingAvaliable);
            this.controllerThread.TankValuesAvaliable += new EventHandler<TankValuesEventArgs>(controllerThread_TankValuesAvaliable);
            this.controllerThread.DispenserValuesAvaliable += new EventHandler<DispenserValuesEventArgs>(controllerThread_DispenserValuesAvaliable);
            //this.controllerThread.AlarmRaised += new EventHandler<WorkFlow.AlarmRaisedEventArgs>(controllerThread_AlarmRaised);
            this.controllerThread.CheckInvoices += new EventHandler(controllerThread_CheckInvoices);
            this.controllerThread.QueryTotalsUpdate += new EventHandler<FuelPump.QueryTotalsUpdateArgs>(controllerThread_QueryTotalsUpdate);
            //this.controllerThread.QueryAlarmResolved += new EventHandler<WorkFlow.QueryAlarmResolvedArgs>(controllerThread_QueryAlarmResolved);
            controllerThread.QueryResolveAlarm += new EventHandler<QueryResolveAlarmArgs>(controllerThread_QueryResolveAlarm);
            //this.controllerThread.UpdateNozzleValues += new EventHandler(controllerThread_UpdateNozzleValues);
            this.controllerThread.QueryPrices += new EventHandler(controllerThread_QueryPrices);
            this.controllerThread.PrintInvoice += new EventHandler<PrintInvoiceArgs>(controllerThread_PrintInvoice);
            this.controllerThread.QueryStartTimer += new EventHandler<TimerStartEventArgs>(controllerThread_QueryStartTimer);
            this.controllerThread.UpdateNozzleStatus += new EventHandler<FuelPump.NozzleStatusChangedArgs>(controllerThread_UpdateNozzleStatus);
            this.controllerThread.ControllerConnectionFailed += new EventHandler(controllerThread_ControllerConnectionFailed);
            this.controllerThread.ControllerConnectionSuccess+=new EventHandler(controllerThread_ControllerConnectionSuccess);
            //this.controllerThread.RefreshAlerts += new EventHandler(controllerThread_RefreshAlerts);
            this.controllerThread.ReadFleetData += new EventHandler(controllerThread_ReadFleetData);
            this.controllerThread.OTPStatusChanged += ControllerThread_OTPStatusChanged;
        }

        private void ControllerThread_OTPStatusChanged(object sender, EventArgs e)
        {
            if (this.OTPStatusChanged != null)
                this.OTPStatusChanged(this, new EventArgs());
        }

        void controllerThread_ReadFleetData(object sender, EventArgs e)
        {
            VirtualDispenser dispenser = sender as VirtualDispenser;
            if (dispenser == null)
                return;
            foreach (VirtualFleetDispenser vd in dispenser.FleetDispensers)
            {
                if (!this.fleetDataDict.ContainsKey(vd.ComPort))
                    continue;
                EnrollData enrollData = this.fleetDataDict[vd.ComPort];
                if (DateTime.Now.Subtract(enrollData.EnrollDate).TotalMinutes > 3)
                {
                    dispenser.NextSaleVehicle = Guid.Empty;
                    dispenser.NextSaleTrader = Guid.Empty;
                    this.fleetDataDict.Remove(vd.ComPort);
                    continue;
                }
                Data.Vehicle vehicle = this.database.Vehicles.Where(v => v.CardId == enrollData.EnrollId).FirstOrDefault();
                if (vehicle == null)
                    continue;
                dispenser.NextSaleVehicle = vehicle.VehicleId;
                dispenser.NextSaleTrader = vehicle.TraderId;
                if(vehicle.Trader.InvoiceTypeId.HasValue)
                    dispenser.InvoiceTypeId = vehicle.Trader.InvoiceTypeId.Value;
                else
                    dispenser.InvoiceTypeId = vd.InvoiceTypeId;

                this.fleetDataDict.Remove(vd.ComPort);
                return;
            }
        }

        void fleetThread_DataRecieved(object sender, FleetManagementDataRecievedArgs e)
        {
            if (!fleetDataDict.ContainsKey(e.ComPort))
                fleetDataDict.Add(e.ComPort, new EnrollData() { EnrollId = e.Data, EnrollDate = e.Date });
            else
                fleetDataDict[e.ComPort] = new EnrollData() { EnrollId = e.Data, EnrollDate = e.Date };
        }

        //void controllerThread_RefreshAlerts(object sender, EventArgs e)
        //{
        //    if (this.RefreshAlerts != null)
        //        this.RefreshAlerts(this, new EventArgs());
        //    //try
        //    //{
        //    //    foreach (SendLog log in this.logToSave)
        //    //    {
        //    //        Data.SendLog slog = this.database.SendLogs.Where(s => s.EntityIdentity == log.Identity).FirstOrDefault();
        //    //        if (slog == null)
        //    //        {
        //    //            slog = new Data.SendLog();
        //    //            slog.SendLogId = Guid.NewGuid();
        //    //            slog.SendDate = log.SentTime;
        //    //            slog.EntityIdentity = log.Identity;
        //    //            slog.SendData = log.Data;
        //    //            this.database.Add(slog);
        //    //        }
        //    //        slog.Action = log.Action.ToString();
        //    //        slog.LastSent = log.SentTime;
        //    //        slog.SentStatus = log.Status;

        //    //    }
        //    //    this.database.SaveChanges();
        //    //    logToSave = new ConcurrentBag<SendLog>();
        //    //}
        //    //catch
        //    //{

        //    //}
        //}

        //public void ResolveAlarm(VirtualBaseAlarm alarm)
        //{
        //    Data.SystemEvent sysEv = this.database.SystemEvents.Where(s => s.EventId == alarm.DatabaseEntityId).FirstOrDefault();
        //    if (sysEv == null)
        //        return;
        //    if (sysEv.ResolvedDate.HasValue)
        //        return;
        //    if (!sysEv.AlarmType.HasValue)
        //        return;
        //    Common.Enumerators.AlarmEnum alarmType = (Common.Enumerators.AlarmEnum)sysEv.AlarmType.Value;
        //    switch (alarmType)
        //    {
        //        case Common.Enumerators.AlarmEnum.NozzleTotalError:

        //            decimal lastTotals = this.database.Nozzles.Where(n => n.NozzleId == sysEv.NozzleId.Value).First().LastValidTotalizer;
        //            decimal newTotals = this.database.Nozzles.Where(n => n.NozzleId == sysEv.NozzleId.Value).First().TotalCounter;
        //            VirtualNozzle nozzle = this.controllerThread.GetNozzle(sysEv.NozzleId.Value);
        //            if (lastTotals == newTotals)
        //            {
        //                //alarms.Remove(alarm);
        //                nozzle.LastVolumeCounter = this.database.Nozzles.Where(n => n.NozzleId == sysEv.NozzleId.Value).First().LastValidTotalizer;
        //                nozzle.TotalVolumeCounter = this.database.Nozzles.Where(n => n.NozzleId == sysEv.NozzleId.Value).First().TotalCounter;
        //                return;
        //            }
        //            this.database.RemoveCounterAlarm(sysEv);


        //            nozzle.LastVolumeCounter = this.database.Nozzles.Where(n => n.NozzleId == sysEv.NozzleId.Value).First().LastValidTotalizer;
        //            nozzle.TotalVolumeCounter = this.database.Nozzles.Where(n => n.NozzleId == sysEv.NozzleId.Value).First().TotalCounter;


        //            break;
        //        case Common.Enumerators.AlarmEnum.FuelDecrease:
        //            this.database.RemoveLevelAlarm(sysEv);
        //            break;
        //        case Common.Enumerators.AlarmEnum.FuelIncrease:
        //            this.database.RemoveLevelAlarm(sysEv);
        //            break;
        //    }
        //    if (alarm.ResolvedTime.Year < 1900)
        //        alarm.ResolvedTime = DateTime.Now;
        //    if (alarm.ResolveText == null || alarm.ResolveText == "")
        //        alarm.ResolveText = "";
        //    sysEv.ResolvedDate = alarm.ResolvedTime;//.ResolveDateTime;
        //    sysEv.ResolveMessage = alarm.ResolveText;

        //    this.database.SaveChanges();
        //}

        //public void RaiseAlarm(VirtualBaseAlarm alarm)
        //{
        //    Data.SystemEvent existingEvent = new Data.SystemEvent();
        //    if (alarm.GetType() == typeof(VirtualTankAlarm))
        //    {
        //        existingEvent = this.database.SystemEvents.Where(s => !s.ResolvedDate.HasValue && s.TankId == alarm.DeviceId && s.Message == alarm.MessageText).FirstOrDefault();
        //        if (existingEvent != null)
        //            alarm.DeviceDescription = "Δεξαμενή : " + existingEvent.Tank.TankNumber;
        //    }
        //    else if (alarm.GetType() == typeof(VirtualNozzleAlarm))
        //    {
        //        existingEvent = this.database.SystemEvents.Where(s => !s.ResolvedDate.HasValue && s.NozzleId == alarm.DeviceId && s.Message == alarm.MessageText).FirstOrDefault();
        //        if (existingEvent != null)
        //            alarm.DeviceDescription = "Ακροσωλήνιο : " + existingEvent.Nozzle.Dispenser.OfficialPumpNumber + " " + existingEvent.Nozzle.OfficialNozzleNumber;
        //    }
        //    else if (alarm.GetType() == typeof(VirtualDispenserAlarm))
        //    {
        //        existingEvent = this.database.SystemEvents.Where(s => !s.ResolvedDate.HasValue && s.DispenserId == alarm.DeviceId && s.Message == alarm.MessageText).FirstOrDefault();
        //        if (existingEvent != null)
        //            alarm.DeviceDescription = "Αντλία : " + existingEvent.Dispenser.OfficialPumpNumber;
        //    }
        //    if (existingEvent == null)
        //    {
        //        Data.SystemEvent sysEvent = new Data.SystemEvent();
        //        sysEvent.EventId = Guid.NewGuid();
        //        database.Add(sysEvent);
        //        sysEvent.EventDate = DateTime.Now;

        //        foreach (VirtualAlarmData data in alarm.Data)
        //        {
        //            Data.SystemEventDatum sysData = new Data.SystemEventDatum();
        //            sysData.SystemEventDataId = Guid.NewGuid();
        //            sysData.SystemEventId = sysEvent.EventId;
        //            sysData.PropertyName = data.PropertyName;
        //            sysData.Value = data.Value;
        //            this.database.Add(sysData);
        //        }

        //        if (alarm.GetType() == typeof(VirtualTankAlarm))
        //        {
        //            Data.Tank tank = this.database.Tanks.Where(n => n.TankId == alarm.DeviceId).FirstOrDefault();
        //            if (tank == null)
        //                return;
        //            VirtualTankAlarm tAlarm = alarm as VirtualTankAlarm;
        //            sysEvent.EventType = (int)Common.Enumerators.AlertTypeEnum.TankAlert;
        //            sysEvent.TankId = tAlarm.DeviceId;
        //            sysEvent.AlarmType = (int)tAlarm.AlertType;
        //            sysEvent.Message = tAlarm.MessageText == null ? "" : tAlarm.MessageText;

        //            alarm.DeviceDescription = "Δεξαμενή : " + tank.TankNumber;
        //        }
        //        else if (alarm.GetType() == typeof(VirtualNozzleAlarm))
        //        {
        //            Data.Nozzle nozzle = this.database.Nozzles.Where(n => n.NozzleId == alarm.DeviceId).FirstOrDefault();
        //            if (nozzle == null)
        //                return;
        //            VirtualNozzleAlarm nAlarm = alarm as VirtualNozzleAlarm;
        //            sysEvent.EventType = (int)Common.Enumerators.AlertTypeEnum.FuelPointError;
        //            sysEvent.NozzleId = nAlarm.DeviceId;
        //            sysEvent.AlarmType = (int)nAlarm.AlertType;
        //            sysEvent.Message = nAlarm.MessageText == null ? "" : nAlarm.MessageText;

        //            alarm.DeviceDescription = "Ακροσωλήνιο : " + nozzle.Dispenser.OfficialPumpNumber + " " + nozzle.OfficialNozzleNumber;

        //        }
        //        else if (alarm.GetType() == typeof(VirtualDispenserAlarm))
        //        {
        //            Data.Dispenser dispenser = this.database.Dispensers.Where(n => n.DispenserId == alarm.DeviceId).FirstOrDefault();
        //            if (dispenser == null)
        //                return;
        //            VirtualDispenserAlarm nAlarm = alarm as VirtualDispenserAlarm;
        //            sysEvent.EventType = (int)Common.Enumerators.AlertTypeEnum.CommunicationLossFuelPoint;
        //            sysEvent.DispenserId = nAlarm.DeviceId;
        //            sysEvent.AlarmType = (int)nAlarm.AlertType;
        //            sysEvent.Message = nAlarm.MessageText == null ? "" : nAlarm.MessageText;

        //            alarm.DeviceDescription = "Αντλία : " + dispenser.OfficialPumpNumber;

        //        }
        //        alarm.DatabaseEntityId = sysEvent.EventId;

        //        sysEvent.CRC = this.CalculateCRC32();
        //        if(database.HasChanges)
        //            database.SaveChanges();

        //    }
        //    else
        //    {
        //        alarm.AlarmTime = existingEvent.EventDate;
        //        alarm.DatabaseEntityId = existingEvent.EventId;
        //    }
        //}

        //private void ApplySign(string fileName, string fullName)
        //{
        //    string str = System.IO.File.ReadAllText(fullName);
        //    string id = fileName.Replace(".out", "");
        //    Guid invoiceId = Guid.Parse(id);
        //    Data.Invoice invoice = this.database.Invoices.Where(inv => inv.InvoiceId == invoiceId).FirstOrDefault();
        //    if (invoice == null)
        //        return;
        //    int signIndex = str.IndexOf(",");
        //    if (signIndex < 0)
        //        return;
        //    str = str.Substring(7, signIndex - 6);
        //    invoice.InvoiceSignature = str;
        //    System.IO.File.Delete(fullName);
        //}

        /// <summary>
        /// Creates the IController for all CommunicationController entries in the database 
        /// </summary>
        /// <param name="controller"></param>
        /// <returns></returns>
        private Common.IController CreateControllerInterface(Data.CommunicationController controller)
        {
            Common.IController controllerInterface = null;
            if (controller.ControllerAssembly == null)
            {
                return null;
            }
            if (!System.IO.File.Exists(controller.ControllerAssembly))
            {
                return null;
            }
            if (!controller.CommunicationProtocol.HasValue)
            {
                return null;
            }
            if (controller.CommunicationPort == null || controller.CommunicationPort == "")
            {
                return null;
            }

            Assembly asm = Assembly.LoadFile(System.Environment.CurrentDirectory + "\\" + controller.ControllerAssembly);
            Type[] types = asm.GetExportedTypes();
            foreach (Type t in types)
            {
                Type interfn = t.GetInterface("ASFuelControl.Common.IController");
                if (interfn != null)
                {
                    try
                    {
                        controllerInterface = asm.CreateInstance(t.FullName) as ASFuelControl.Common.IController;
                        controllerInterface.CommunicationPort = controller.CommunicationPort;
                        controllerInterface.CommunicationType = (ASFuelControl.Common.Enumerators.CommunicationTypeEnum)controller.CommunicationProtocol.Value;
                        //controllerInterface.TotalsRecieved += new EventHandler<Common.TotalsEventArgs>(controllerInterface_TotalsRecieved);

                        controllerNames.Add(controllerInterface, controller.Name);

                        return controllerInterface;
                    }
                    catch
                    {
                        return null;
                    }
                    break;
                }
            }
            return null;
        }
        
        /// <summary>
        /// Updates the database if needed.
        /// </summary>
        /// <param name="version"></param>
        private void UpdateDatabase(int version)
        {
            Common.Logger.Instance.Debug("Update Database to version: {value1}", version, "", 2);
            string scriptName = "UpdateDatabase" + version.ToString();
            
            string str = Properties.Resources.ResourceManager.GetString(scriptName);
            string[] commands = str.Split(new string[] {"GO\r\n"}, StringSplitOptions.RemoveEmptyEntries);
            foreach (string command in commands)
            {
                try
                {
                    System.Data.SqlClient.SqlCommand c = new System.Data.SqlClient.SqlCommand(command);
                    c.Connection = new System.Data.SqlClient.SqlConnection(Properties.Settings.Default.DBConnection);
                    c.CommandTimeout = 1000;
                    c.Connection.Open();
                    c.ExecuteNonQuery();//(command, new System.Data.Common.DbParameter[] { });
                    c.Connection.Close();
                }
                catch (Exception ex)
                {
                    Common.Logger.Instance.Error("Update Database to version: {value1}. Command {value2} failed", version, command, "", 2);
                    Logging.Logger.Instance.LogToFile(string.Format("UpdateDatabase Exception Veriosn {0}", version), ex);
                }

            }
            
        }

        /// <summary>
        /// Gets the current database version
        /// </summary>
        /// <returns></returns>
        private int GetDatabaseVersion()
        {
            string command = "Select OptionValue from dbo.[Option] Where OptionKey = 'DBVersion'";
            System.Data.SqlClient.SqlCommand c = new System.Data.SqlClient.SqlCommand(command);
            c.Connection = new System.Data.SqlClient.SqlConnection(Properties.Settings.Default.DBConnection);
            c.Connection.Open();
            object ret = c.ExecuteScalar();
            c.Connection.Close();

            string ver = ret.ToString();
            int version = -1;
            if(!int.TryParse(ver, out version))
                ver = AESEncryption.Decrypt(ret.ToString(), "Exedron@#");

            return int.Parse(ver);
              
        }

        #endregion

        #region Controller Events

        /// <summary>
        /// Event fired after an Balance is created. Initiates an rebuild of the database object.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void sendingThread_BalanceCreated(object sender, EventArgs e)
        {
            this.restartDatabase = true;

            //this.database = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
            //Data.Shift currentShift = this.database.Shifts.Where(s => !s.ShiftEnd.HasValue).FirstOrDefault();
            //if (currentShift != null)
            //{
            //    Program.CurrentShiftId = currentShift.ShiftId;
            //    Program.CurrentUserId = currentShift.ApplicationUserId;
            //    Program.CurrentUserName = currentShift.ApplicationUser.UserName;
            //    Program.CurrentUserLevel = (Common.Enumerators.ApplicationUserLevelEnum)currentShift.ApplicationUser.UserLevel;
            //}
        }

        /// <summary>
        /// Update the nozzle status in the database
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void controllerThread_UpdateNozzleStatus(object sender, FuelPump.NozzleStatusChangedArgs e)
        {
            Data.Nozzle nozzle = this.database.Nozzles.Where(n => n.NozzleId == e.NozzleId).FirstOrDefault();
            if (nozzle.NozzleState == (int)e.NozzleStatus)
                return;
            nozzle.NozzleState = (int)e.NozzleStatus;
            this.database.SaveChanges();
        }

        ///// <summary>
        ///// Fired when an alert is resolved in the controller and informs the main form
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        //void controllerThread_QueryAlarmResolved(object sender, WorkFlow.QueryAlarmResolvedArgs e)
        //{
        //    if (sender.GetType().GetInterface("ASFuelControl.WorkFlow.ITankWorkFlow") != null)
        //    {
        //        var q = this.database.SystemEvents.Where(se => se.TankId == e.DeviceId && !se.ResolvedDate.HasValue);
        //        this.database.Refresh(RefreshMode.OverwriteChangesFromStore, q);
        //        List<Data.SystemEvent> events = q.ToList();
        //        foreach (Data.SystemEvent se in events)
        //        {
        //            se.ResolvedDate = DateTime.Now;
        //            se.ResolveMessage = "Auto Resolved";
        //            if (this.AlarmResolved != null)
        //                this.AlarmResolved(this, new AlarmResolvedArgs(se.EventId));
        //        }
        //        if(events.Count > 0)
        //            this.database.SaveChanges();
        //    }
        //    else if (sender.GetType().GetInterface("ASFuelControl.WorkFlow.IFuelPumpWorkFlow") != null)
        //    {
        //        var q = this.database.SystemEvents.Where(se => se.NozzleId == e.DeviceId && !se.ResolvedDate.HasValue);
        //        this.database.Refresh(RefreshMode.OverwriteChangesFromStore, q);
        //        List<Data.SystemEvent> events = q.ToList();
        //        foreach (Data.SystemEvent se in events)
        //        {
        //            se.ResolvedDate = DateTime.Now;
        //            se.ResolveMessage = "Auto Resolved";
        //            if (this.AlarmResolved != null)
        //                this.AlarmResolved(this, new AlarmResolvedArgs(se.EventId));

                    

        //        }
        //        if (events.Count > 0)
        //            this.database.SaveChanges();

        //    }

        //}

        ///// <summary>
        ///// Fired when an alert is generated in the controller and informs the main form
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        //void controllerThread_AlarmRaised(object sender, WorkFlow.AlarmRaisedEventArgs e)
        //{
        //    Data.SystemEvent existingEvent = new Data.SystemEvent();
        //    if (e.Alarm.GetType() == typeof(VirtualTankAlarm))
        //    {
        //        existingEvent = this.database.SystemEvents.Where(s => !s.ResolvedDate.HasValue && s.TankId == e.Alarm.DeviceId && s.Message == e.Alarm.MessageText).FirstOrDefault();
        //        if (existingEvent != null)
        //            e.Alarm.DeviceDescription = "Δεξαμενή : " + existingEvent.Tank.TankNumber;
        //    }
        //    else if (e.Alarm.GetType() == typeof(VirtualNozzleAlarm))
        //    {
        //        existingEvent = this.database.SystemEvents.Where(s => !s.ResolvedDate.HasValue && s.NozzleId == e.Alarm.DeviceId && s.Message == e.Alarm.MessageText).FirstOrDefault();
        //        if (existingEvent != null)
        //            e.Alarm.DeviceDescription = "Ακροσωλήνιο : " + existingEvent.Nozzle.Dispenser.OfficialPumpNumber + " " + existingEvent.Nozzle.OfficialNozzleNumber;
        //    }
        //    else if (e.Alarm.GetType() == typeof(VirtualDispenserAlarm))
        //    {
        //        existingEvent = this.database.SystemEvents.Where(s => !s.ResolvedDate.HasValue && s.DispenserId == e.Alarm.DeviceId && s.Message == e.Alarm.MessageText).FirstOrDefault();
        //        if (existingEvent != null)
        //            e.Alarm.DeviceDescription = "Αντλία : " + existingEvent.Dispenser.OfficialPumpNumber;
        //    }
        //    if (existingEvent == null)
        //    {
        //        Data.SystemEvent sysEvent = new Data.SystemEvent();
        //        sysEvent.EventId = Guid.NewGuid();
        //        database.Add(sysEvent);
        //        sysEvent.EventDate = DateTime.Now;

        //        foreach (VirtualAlarmData data in e.Alarm.Data)
        //        {
        //            Data.SystemEventDatum sysData = new Data.SystemEventDatum();
        //            sysData.SystemEventDataId = Guid.NewGuid();
        //            sysData.SystemEventId = sysEvent.EventId;
        //            sysData.PropertyName = data.PropertyName;
        //            sysData.Value = data.Value;
        //            this.database.Add(sysData);
        //        }

        //        if (e.Alarm.GetType() == typeof(VirtualTankAlarm))
        //        {
        //            Data.Tank tank = this.database.Tanks.Where(n => n.TankId == e.Alarm.DeviceId).FirstOrDefault();
        //            if (tank == null)
        //                return;
        //            VirtualTankAlarm tAlarm = e.Alarm as VirtualTankAlarm;
        //            sysEvent.EventType = (int)Common.Enumerators.AlertTypeEnum.TankAlert;
        //            sysEvent.TankId = tAlarm.DeviceId;
        //            sysEvent.AlarmType = (int)tAlarm.AlertType;
        //            sysEvent.Message = tAlarm.MessageText == null ? "" : tAlarm.MessageText;

        //            e.Alarm.DeviceDescription = "Δεξαμενή : " + tank.TankNumber;
        //        }
        //        else if (e.Alarm.GetType() == typeof(VirtualNozzleAlarm))
        //        {
        //            Data.Nozzle nozzle =  this.database.Nozzles.Where(n=>n.NozzleId == e.Alarm.DeviceId).FirstOrDefault();
        //            if(nozzle == null)
        //                return;
        //            VirtualNozzleAlarm nAlarm = e.Alarm as VirtualNozzleAlarm;
        //            sysEvent.EventType = (int)Common.Enumerators.AlertTypeEnum.FuelPointError;
        //            sysEvent.NozzleId = nAlarm.DeviceId;
        //            sysEvent.AlarmType = (int)nAlarm.AlertType;
        //            sysEvent.Message = nAlarm.MessageText == null ? "" : nAlarm.MessageText;

        //            e.Alarm.DeviceDescription = "Ακροσωλήνιο : " + nozzle.Dispenser.OfficialPumpNumber + " " + nozzle.OfficialNozzleNumber;

        //        }
        //        else if (e.Alarm.GetType() == typeof(VirtualDispenserAlarm))
        //        {
        //            Data.Dispenser dispenser = this.database.Dispensers.Where(n => n.DispenserId == e.Alarm.DeviceId).FirstOrDefault();
        //            if (dispenser == null)
        //                return;
        //            VirtualDispenserAlarm nAlarm = e.Alarm as VirtualDispenserAlarm;
        //            sysEvent.EventType = (int)Common.Enumerators.AlertTypeEnum.CommunicationLossFuelPoint;
        //            sysEvent.DispenserId = nAlarm.DeviceId;
        //            sysEvent.AlarmType = (int)nAlarm.AlertType;
        //            sysEvent.Message = nAlarm.MessageText == null ? "" : nAlarm.MessageText;

        //            e.Alarm.DeviceDescription = "Αντλία : " + dispenser.OfficialPumpNumber;

        //        }
        //        e.Alarm.DatabaseEntityId = sysEvent.EventId;
                
        //        sysEvent.CRC = this.CalculateCRC32();
                
        //        database.SaveChanges();
                
        //    }
        //    else
        //    {
        //        e.Alarm.AlarmTime = existingEvent.EventDate;
        //        e.Alarm.DatabaseEntityId = existingEvent.EventId;
        //    }
        //    if (this.AlarmRaised != null)
        //    {
        //        this.AlarmRaised(this, e);
        //    }
        //}

        void controllerThread_QueryResolveAlarm(object sender, QueryResolveAlarmArgs e)
        { 
            if (this.QueryResolveAlarm != null)
            {
                this.QueryResolveAlarm(this, e);
                List<ResolveAlarmData> alarms = new List<ResolveAlarmData>(e.Alarms);
                foreach (ResolveAlarmData alarm in e.Alarms)
                {
                    Data.SystemEvent sysEv = this.database.SystemEvents.Where(s => s.EventId == alarm.AlamId).FirstOrDefault();
                    if (sysEv == null)
                        continue;
                    if (sysEv.ResolvedDate.HasValue)
                        continue;
                    if (!sysEv.AlarmType.HasValue)
                        continue;
                    Common.Enumerators.AlarmEnum alarmType = (Common.Enumerators.AlarmEnum)sysEv.AlarmType.Value;
                    switch (alarmType)
                    {
                        case Common.Enumerators.AlarmEnum.NozzleTotalError:

                            decimal lastTotals = this.database.Nozzles.Where(n => n.NozzleId == sysEv.NozzleId.Value).First().LastValidTotalizer;
                            decimal newTotals = this.database.Nozzles.Where(n => n.NozzleId == sysEv.NozzleId.Value).First().TotalCounter;
                            VirtualNozzle nozzle = this.controllerThread.GetNozzle(sysEv.NozzleId.Value);
                            if (lastTotals == newTotals)
                            {
                                alarms.Remove(alarm);
                                nozzle.LastVolumeCounter = this.database.Nozzles.Where(n => n.NozzleId == sysEv.NozzleId.Value).First().LastValidTotalizer;
                                nozzle.TotalVolumeCounter = this.database.Nozzles.Where(n => n.NozzleId == sysEv.NozzleId.Value).First().TotalCounter;
                                continue;
                            }
                            this.database.RemoveCounterAlarm(sysEv);

                            
                            nozzle.LastVolumeCounter = this.database.Nozzles.Where(n => n.NozzleId == sysEv.NozzleId.Value).First().LastValidTotalizer;
                            nozzle.TotalVolumeCounter = this.database.Nozzles.Where(n => n.NozzleId == sysEv.NozzleId.Value).First().TotalCounter;
                            
                            
                            break;
                        case Common.Enumerators.AlarmEnum.FuelDecrease:
                            this.database.RemoveLevelAlarm(sysEv);
                            break;
                        case Common.Enumerators.AlarmEnum.FuelIncrease:
                            this.database.RemoveLevelAlarm(sysEv);
                            break;
                    }
                    sysEv.ResolvedDate = alarm.ResolveDateTime;
                    sysEv.ResolveMessage = alarm.ResolveText + " (controllerThread_QueryResolveAlarm)";
                }
                if (alarms.Count > 0)
                    this.database.SaveChanges();
            }
           
        }

        /// <summary>
        /// Event for saving the new totals of the fuelpoint nozzles in the database
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void controllerThread_QueryTotalsUpdate(object sender, FuelPump.QueryTotalsUpdateArgs e)
        {
            Data.Nozzle nozzle = this.database.Nozzles.Where(t => t.NozzleId == e.NozzleId).FirstOrDefault();
            if (nozzle == null)
                return;
            if(nozzle.TotalCounter == e.TotalVolumeCounter)
                return;
            nozzle.TotalCounter = e.TotalVolumeCounter;
            database.SaveChanges();
        }

        /// <summary>
        /// Event fired after new values are avaliable in an VirtualDispenser
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void controllerThread_DispenserValuesAvaliable(object sender, DispenserValuesEventArgs e)
        {
            try
            {

                Data.Dispenser dispenser = this.database.Dispensers.Where(t => t.DispenserId == e.DispenserId).FirstOrDefault();
                if(dispenser == null)
                    return;
                dispenser.PhysicalState = (int)e.Values.Status;
                List<Data.Nozzle> nozzles = dispenser.OrderedNozzles.ToList();
                List<VirtualTank> affectedTanks = new List<VirtualTank>();
                for(int i = 0; i < nozzles.Count; i++)
                {
                    if (e.Values.TotalVolumes != null && e.Values.TotalVolumes[i] > 0)
                    {
                        nozzles[i].TotalCounter = e.Values.TotalVolumes[i];
                        VirtualNozzle nozzle = this.controllerThread.GetNozzle(nozzles[i].NozzleId);
                        foreach(VirtualTank tank in nozzle.ConnectedTanks)
                        {
                            if(affectedTanks.Contains(tank))
                                continue;
                            affectedTanks.Add(tank);
                        }
                        Threads.AlertChecker.Instance.CheckNozzleAlerts(nozzles[i].NozzleId);
                    }
                    else if(e.Values.Sale != null)
                    {
                        Threads.AlertChecker.Instance.CheckNozzleAlerts(nozzles[i].NozzleId);
                    }
                }

                this.database.SaveChanges();
                //foreach(VirtualTank vTank in affectedTanks)
                //{
                //    Data.Tank tank = this.database.Tanks.Where(t => t.TankId == vTank.TankId).FirstOrDefault();
                //    if(tank == null)
                //        continue;
                //    //vTank.LastFuelHeight = tank.GetLastValidLevel();
                //    //vTank.LastWaterHeight = tank.GetLastValidWaterLevel();
                //    //vTank.LastTemperature = tank.GetLastValidTemperatur();
                //    decimal[] validValues = tank.GetLastValidValues();
                //    vTank.LastFuelHeight = validValues[0];
                //    vTank.LastWaterHeight = validValues[1];
                //    vTank.LastTemperature = validValues[2];
                //}
            }
            catch(Exception ex)
            {
                Logging.Logger.Instance.LogToFile("controllerThread_DispenserValuesAvaliable::AffectedTanks", ex);
            }
        }

        /// <summary>
        /// Event fired after new values are avaliable in an VirtualTank
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void controllerThread_TankValuesAvaliable(object sender, TankValuesEventArgs e)
        {   
            try
            {
                Data.Tank tank = this.database.Tanks.Where(t => t.TankId == e.TankId).FirstOrDefault();
                if (tank == null)
                    return;
                VirtualTank vTank = this.controllerThread.GetTank(tank.TankId);
                if (e.Values != null)
                {
                    tank.FuelLevelBase = e.Values.FuelHeight;
                    tank.WaterLevelBase = e.Values.WaterHeight;
                    tank.Temperatire = e.Values.CurrentTemperatur;
                    tank.ReferenceLevel = tank.FuelLevel;
                    vTank.LastFuelHeight = tank.ReferenceLevel;
                    //tank.SetReferenceValues();
                    if (tank.PeriodMaximum < tank.FuelLevel)
                    {
                        tank.PeriodMaximum = tank.FuelLevel;
                        Console.WriteLine(string.Format("Maximum:{0}", tank.PeriodMaximum));

                    }
                    if (tank.PeriodMinimum > tank.FuelLevel)
                    {
                        tank.PeriodMinimum = tank.FuelLevel;
                        Console.WriteLine(string.Format("Minimum:{0}", tank.PeriodMinimum));
                    }
                }
                
                this.database.SaveChanges();
                
                //vTank.LastFuelHeight = tank.GetLastValidLevel();
                //vTank.LastWaterHeight = tank.GetLastValidWaterLevel();
                //vTank.LastTemperature = tank.GetLastValidTemperatur();
                decimal[] ValidValues = tank.GetLastValidValues();
                //vTank.LastFuelHeight = ValidValues[0];
                vTank.LastWaterHeight = ValidValues[1];
                vTank.LastTemperature = ValidValues[2];
                //vTank.LastSalesVolumeDifference = tank.LastSalesDifference(10);

                if (vTank.TankStatus != Common.Enumerators.TankStatusEnum.Waiting && vTank.TankStatus != Common.Enumerators.TankStatusEnum.WaitingEllapsed)
                    tank.PhysicalState = (int)vTank.TankStatus;

                tank.FillingInvoiceId = vTank.InvoiceLineId;
                tank.IsLiterCheck = vTank.IsLiterCheck;
                tank.InitializeFilling = vTank.InitializeFilling;
                tank.InvoiceTypeId = vTank.InvoiceTypeId;
                tank.WaitingStarted = vTank.WaitingStarted;
                tank.WaitingShouldEnd = vTank.WaitingShouldEnd;
                tank.VehicleId = vTank.VehicleId;
                tank.FillingFuelTypeId = vTank.FillingFuelTypeId;
                tank.PreviousState = vTank.PreviousStatus;
                tank.DeliveryStrarted = vTank.DeliveryStarted;
                tank.FillingStartLevel = vTank.FillingStartTankLevel;
                
                //if (e.Values == null)
                if(database.HasChanges)
                    this.database.SaveChanges();
                restartDatabaseIndex++;
                if (restartDatabaseIndex % 50 == 0)
                {
                    try
                    {
                        System.GC.WaitForPendingFinalizers();
                        System.GC.Collect();
                        System.GC.WaitForPendingFinalizers();
                        System.GC.Collect();
                        System.GC.WaitForPendingFinalizers();
                        System.GC.Collect();
                    }
                    catch
                    {
                    }
                    //restartDatabase = true;
                    restartDatabaseIndex = 0;
                }
                if (this.restartDatabase)
                {
                    this.database.Dispose();
                    this.database = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
                    this.database.AlertChecker = Threads.AlertChecker.Instance;
                    //Data.Implementation.AlertChecker.Instance.Database = this.database;
                    Data.Shift currentShift = this.database.Shifts.Where(s => !s.ShiftEnd.HasValue).FirstOrDefault();
                    if (currentShift != null)
                    {
                        Program.CurrentShiftId = currentShift.ShiftId;
                        Program.CurrentUserId = currentShift.ApplicationUserId;
                        Program.CurrentUserName = currentShift.ApplicationUser.UserName;
                        Program.CurrentUserLevel = (Common.Enumerators.ApplicationUserLevelEnum)currentShift.ApplicationUser.UserLevel;
                    }
                    this.restartDatabase = false;

                    try
                    {
                        System.GC.WaitForPendingFinalizers();
                        System.GC.Collect();
                        System.GC.WaitForPendingFinalizers();
                        System.GC.Collect();
                        System.GC.WaitForPendingFinalizers();
                        System.GC.Collect();
                    }
                    catch
                    {
                    }

                }
            }
            catch(Exception ex)
            {
                Logger.Instance.LogToFile("TankValuesAvaliable Event", ex);
            }
        }

        /// <summary>
        /// Event fired after a tank filling process ends
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void controllerThread_TankFillingAvaliable(object sender, EventArgs e)
        {
            Common.Sales.TankFillingData filling = this.controllerThread.GetNextFilling();
            if (filling.DeliveryStarted.Year < 2000)
                filling.DeliveryStarted = DateTime.Now;
            
            Data.Tank tank = this.database.Tanks.Where(t => t.TankId == filling.TankId).FirstOrDefault();
            VirtualTank vtank = this.controllerThread.GetTank(tank.TankId);
            if (tank == null || vtank == null)
                return;

            if (filling.InvoiceTypeId != Guid.Empty && filling.InvoiceLineId == Guid.Empty)
            {
                filling.InvoiceLineId = tank.CreateFillingInvoice(filling);
            }

            Data.TankFilling tf = tank.CreateTankFilling(filling.InvoiceLineId, filling.StartValues, filling.EndValues, filling.DeliveryStarted);
            //tank.ReferenceLevel = tank.FuelLevel;
            this.database.Add(tf);
            this.database.SaveChanges();
            vtank.LastFuelHeight = tank.GetLastValidLevel();//vtank.FillingStartTankLevel;//;
            //vtank.FillingStartTankLevel = 0;

            vtank.LastTemperature = tank.GetLastValidTemperatur();
            vtank.LastWaterHeight = tank.GetLastValidWaterLevel();
            if (this.TankFillingAvaliableEvent != null)
            {
                VirtualTankFillingInfo info = new VirtualTankFillingInfo();
                info.VolumeInvoiced = tf.VolumeNormalized;
                info.VolumeReal = tf.VolumeRealNormalized;
                info.Difference = tf.VolumeNormalized - tf.VolumeRealNormalized;
                info.DeviceDescription = string.Format("Παραλαβή Δεξαμενής {0}", tank.TankNumber);
                info.MessageText = string.Format("Όγκος Παραστατικού: {0:N2} lt Όγκος Μέτρησης: {1:N2} lt", info.VolumeInvoiced, info.VolumeReal);
                info.AlarmTime = DateTime.Now;
                this.TankFillingAvaliableEvent(this, new TankFillingAvaliableArgs() { TankFillingInfo = info });
            }
            //if (this.AlarmRaised != null)
            //{
            //    VirtualTankFillingInfo info = new VirtualTankFillingInfo();
            //    info.VolumeInvoiced = tf.VolumeNormalized;
            //    info.VolumeReal = tf.VolumeRealNormalized;
            //    info.Difference = tf.VolumeNormalized - tf.VolumeRealNormalized;
            //    info.DeviceDescription = string.Format("Παραλαβή Δεξαμενής {0}", tank.TankNumber);
            //    info.MessageText = string.Format("Όγκος Παραστατικού: {0:N2} lt Όγκος Μέτρησης: {1:N2} lt", info.VolumeInvoiced, info.VolumeReal);
            //    info.AlarmTime = DateTime.Now;
            //    this.AlarmRaised(this, new WorkFlow.AlarmRaisedEventArgs(info));
            //}
        }


        /// <summary>
        /// Event that initiates a creation of a sale Invoice
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void controllerThread_SaleAvaliable(object sender, WorkFlow.SaleCompletedArgs e)
        {
            Common.Sales.SaleData sale = e.Sale;
            if (sale == null)
            {
                Logger.Instance.LogToFile("controllerThread_SaleAvaliable", "Sale is Null");
                return;
            }
            Data.Invoice newInvoice = null;
            Data.Nozzle nozzle = null;
            try
            {
                this.database.SaveChanges();
                
                nozzle = this.database.Nozzles.Where(n => n.NozzleId == sale.NozzleId).FirstOrDefault();
                
                if (nozzle == null)
                {
                    this.database.SaveChanges();
                    Logger.Instance.LogToFile("controllerThread_SaleAvaliable", string.Format("Nozzle:{0} not found", sale.NozzleId));
                    return;
                }
                VirtualDevices.VirtualNozzle vnz = this.controllerThread.GetNozzle(nozzle.NozzleId);
                nozzle.TotalCounter = vnz.TotalVolumeCounter;
                this.database.SaveChanges();
                if (sale.InvalidSale)
                {
                    Guid saleTransId = nozzle.Dispenser.CreateSalesTransaction(sale);
                    VirtualNozzle nz = this.controllerThread.GetNozzle(sale.NozzleId);
                    if (nz == null || saleTransId == Guid.Empty)
                    {
                        //this.controllerThread.RenoveSale();
                        return;
                    }
                    nz.ParentDispenser.HasInvalidSale = true;
                    nz.ParentDispenser.InvalidSales.Add(saleTransId);
                    //this.controllerThread.RenoveSale();
                    return;
                }
                else
                {
                    VirtualDispenser[] dispensers = this.GetDispensers();
                    VirtualNozzle nz = dispensers.SelectMany(d => d.Nozzles).Where(n => n.NozzleId == nozzle.NozzleId).FirstOrDefault();
                    nozzle.DiscountPercentage = nz.DiscountPercentage;

                    newInvoice = nozzle.Dispenser.CreateSale(sale);
                    
                    nozzle.DiscountPercentage = 0;
                    nz.DiscountPercentage = 0;
                    if (newInvoice == null)
                    {
                        Logger.Instance.LogToFile("controllerThread_SaleAvaliable", "Invoice not Created");
                        //this.controllerThread.RenoveSale();
                        return;
                    }
                }
                if (nozzle != null)
                {
                    VirtualDispenser[] dispensers = this.GetDispensers();
                    VirtualNozzle nz = dispensers.SelectMany(d => d.Nozzles).Where(n => n.NozzleId == nozzle.NozzleId).FirstOrDefault();
                    if (nz != null)
                    {
                        foreach (VirtualTank vTank in nz.ConnectedTanks)
                        {
                            Data.Tank tank = this.database.Tanks.Where(t => t.TankId == vTank.TankId).FirstOrDefault();
                            if (tank == null)
                                continue;
                            decimal[] validValues = tank.GetLastValidValues();
                            vTank.LastFuelHeight = validValues[0];
                            vTank.LastWaterHeight = validValues[1];
                            vTank.LastTemperature = validValues[2];
                        }
                    }
                }
                if (newInvoice != null)
                {
                    Data.InvoiceLine invLine = newInvoice.InvoiceLines.FirstOrDefault();
                    if (invLine != null)
                        e.InvoiceLineId = invLine.InvoiceLineId;
                }
                //this.controllerThread.RenoveSale();
                if (nozzle.Dispenser.EuromatEnabled)
                {
                    if (sale.EuromatTransaction != null)
                    {

                        if (sale.EuromatTransaction.TranType != Euromat.TranTypeEnum.Attendant)
                        {
                            newInvoice.IsEuromat = sale.EuromatTransaction.TranType == Euromat.TranTypeEnum.Euromat;
                            sale.EuromatTransaction.InvoiceNumber = newInvoice.Number;
                            sale.EuromatTransaction.InvoiceDate = newInvoice.TransactionDate;
                        }

                        bool sent = sale.EuromatTransaction.SendFt(sale.UnitPrice, sale.TotalPrice, sale.TotalVolume);
                        if (!sent)
                        {
                            Logger.Instance.LogToFile("Euromat Error", "Euromat error sending FT Command", 0);
                        }
                        if (sale.EuromatTransaction.TranType != Euromat.TranTypeEnum.Attendant && !sent)
                        {
                            newInvoice.IsEuromat = sale.EuromatTransaction.TranType == Euromat.TranTypeEnum.Euromat;
                            sale.EuromatTransaction.InvoiceNumber = newInvoice.Number;
                            sale.EuromatTransaction.InvoiceDate = newInvoice.TransactionDate;
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Logger.Instance.LogToFile("Create Sale", ex);
                this.database.ClearChanges();
                System.Threading.Thread.Sleep(500);
                this.controllerThread_SaleAvaliable(sender, e);
                return;
            }

            this.database.SaveChanges();
            ///////////////////////////
            //here updates the tanks.//
            ///////////////////////////
        }

        void controllerThread_CheckInvoices(object sender, EventArgs e)
        {
            //foreach (Guid inv in this.processedInvoices)
            //{
            //    string fileName = inv.ToString() + ".out";
            //    string path = this.outFolder.Trim().EndsWith("\\") ? this.outFolder + fileName : this.outFolder + "\\" + fileName;
            //    FileInfo fInfo = new FileInfo(path);
            //    if (fInfo.Exists)
            //    {
            //        try
            //        {
            //            this.ApplySign(fileName, path);
            //            processedInvoices.Remove(inv);
            //        }
            //        catch
            //        {
            //        }
            //    }
            //}
        }

        //void controllerThread_UpdateNozzleValues(object sender, EventArgs e)
        //{
        //    VirtualNozzle vNozzle = sender as VirtualNozzle;
        //    if (vNozzle == null)
        //        return;
        //    Data.Nozzle nozzle = this.database.Nozzles.Where(n => n.NozzleId == vNozzle.NozzleId).FirstOrDefault();
        //    if (nozzle == null)
        //        return;
        //    if (nozzle.LastValidTotalizer != vNozzle.LastVolumeCounter || nozzle.TotalCounter != vNozzle.TotalVolumeCounter)
        //    {
        //        nozzle.TotalCounter = vNozzle.TotalVolumeCounter;
        //        vNozzle.LastVolumeCounter = nozzle.LastValidTotalizer; 
        //        this.database.SaveChanges();
                
        //    }
        //}

        /// <summary>
        /// Reads all prices and updates the VirtualDispensers.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void controllerThread_QueryPrices(object sender, EventArgs e)
        {
            VirtualDispenser[] dispensers = this.GetDispensers();
            this.database.Refresh(RefreshMode.OverwriteChangesFromStore, this.database.Dispensers);
            this.database.Refresh(RefreshMode.OverwriteChangesFromStore, this.database.Nozzles);
            this.database.Refresh(RefreshMode.OverwriteChangesFromStore, this.database.FuelTypes);
            this.database.Refresh(RefreshMode.OverwriteChangesFromStore, this.database.FuelTypePrices);
            List<string> ftlist = new List<string>();
            foreach (VirtualDispenser dispenser in dispensers)
            {
                foreach (VirtualNozzle nozzle in dispenser.Nozzles)
                {
                    Data.Nozzle dNozzle = this.database.Nozzles.Where(n=>n.NozzleId == nozzle.NozzleId).FirstOrDefault();
                    if(dNozzle == null)
                        continue;
                    //if (nozzle.CurrentSaleUnitPrice != dNozzle.FuelType.CurrentPrice)
                    //{
                    //    if (!ftlist.Contains(nozzle.FuelTypeDescription))
                    //        ftlist.Add(nozzle.FuelTypeDescription);
                    //}
                    nozzle.CurrentSaleUnitPrice = dNozzle.FuelType.CurrentPrice;
                    Common.IController controller = this.controllerThread.GetController(nozzle);
                    if (controller == null)
                        continue;
                    controller.SetNozzlePrice(nozzle.ParentDispenser.ChannelId, nozzle.ParentDispenser.AddressId, nozzle.NozzleNumber, dNozzle.FuelType.CurrentPrice);
                    System.Threading.Thread.Sleep(200);
                }
            }
            //foreach (string ftdesc in ftlist)
            //{
            //    Data.FuelType ft = this.database.FuelTypes.Where(f => f.Name == ftdesc).FirstOrDefault();
            //    if (ft == null)
            //        continue;
            //    decimal price = ft.GetCurrentPrice();
                
            //}
        }

        void controllerThread_PrintInvoice(object sender, PrintInvoiceArgs e)
        {
            //Data.Invoice invoice = this.database.Invoices.Where(i => i.InvoiceId == e.InvoiceId).FirstOrDefault();
            //if (invoice == null)
            //    return;
            //if (e.SignString == null || e.SignString == "")
            //    return;
            //if (invoice.InvoiceSignature != null && invoice.InvoiceSignature != "")
            //    return;
            //invoice.InvoiceSignature = e.SignString;
            //this.printHandler.PrintInvoice(invoice);
            //invoice.IsPrinted = true;
            //e.Competed = true;
            //this.database.SaveChanges();
        }

        /// <summary>
        /// Event used to inform the Main Form that an Filling timer is to be started
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void controllerThread_QueryStartTimer(object sender, TimerStartEventArgs e)
        {
            if (this.QueryStartTimer != null)
                this.QueryStartTimer(this, e);
        }

        void sendingThread_PrintAlert(object sender, PrintAlertEventArgs e)
        {
            //Data.SystemEvent ev = this.database.SystemEvents.Where(se => se.EventId == e.AlertId).FirstOrDefault();
            //if (ev == null)
            //    return;

            //string text = "ΣΥΝΑΓΕΡΜΟΣ " + ev.EventDate.ToString("dd/MM/yyyy HH:mm:ss") + "\r\n";
            //if (ev.TankId != null)
            //    text = text + "ΔΕΞΑΜΕΝΗ : " + ev.Tank.TankNumber + " (" + ev.Tank.TankSerialNumber + ")" + "\r\n";
            //else if (ev.NozzleId != null)
            //    text = text + string.Format("ΑΝΤΛΙΑ : {0} ΑΚΡΟΣΩΛΗΝΙΟ : {1}", ev.Nozzle.Dispenser.OfficialPumpNumber, ev.Nozzle.OfficialNozzleNumber) + "\r\n";
            //else
            //    text = text + "ΓΕΝΙΚΟΣ ΣΥΝΑΓΕΡΜΟΣ" + "\r\n";

            //this.printHandler.PrintText(text, "Alert_" + ev.EventId.ToString());
        }

        /// <summary>
        /// Event fired after a controller failed to connect. This event informs the Main form to display an failure message 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void controllerThread_ControllerConnectionFailed(object sender, EventArgs e)
        {
            
            Common.IController controller = sender as Common.IController;
            if(controller == null)
                return;
            if(!this.controllerNames.ContainsKey(controller))
                return;
            string name = this.controllerNames[controller];

            lock (this.failedControllers)
            {
                ConntrollerDescription cdesc = this.failedControllers.Where(c => c.Name == name).FirstOrDefault();
                if (cdesc == null)
                {
                    cdesc = new ConntrollerDescription();
                    cdesc.Name = name;
                    cdesc.ConnectionPort = controller.CommunicationPort;
                    this.failedControllers.Add(cdesc);
                }
                if (this.ConntrollerConnectionFailed != null)
                    this.ConntrollerConnectionFailed(this.failedControllers, e);
            }
        }

        /// <summary>
        /// Event fired after a controller connection success. This event informs the Main form to remove the failure message
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void  controllerThread_ControllerConnectionSuccess(object sender, EventArgs e)
        {
            Common.IController controller = sender as Common.IController;
            if (controller == null)
                return;
            if (!this.controllerNames.ContainsKey(controller))
                return;
            string name = this.controllerNames[controller];

            ConntrollerDescription cdesc = this.failedControllers.Where(c => c.Name == name).FirstOrDefault();
            if (cdesc == null)
                return;
            this.failedControllers.Remove(cdesc);
            if (this.ControllerConnectionSuccess != null)
                this.ControllerConnectionSuccess(this.failedControllers, e);
        }

        #endregion
    }

    public class TankFillingAvaliableArgs : EventArgs
    {
        public VirtualTankFillingInfo TankFillingInfo { set; get; }
    }

    public class QueryResolveAlarmArgs : EventArgs
    {
        public ResolveAlarmData[] Alarms { set; get; }

        public QueryResolveAlarmArgs()
        {
        }
    }

    public class AlarmResolvedArgs : EventArgs
    {
        public Guid AlarmId { set; get; }

        public AlarmResolvedArgs(Guid alarmId)
        {
            this.AlarmId = alarmId;
        }
    }

    public class ResolveAlarmData
    {
        public Guid AlamId { set; get; }
        public string ResolveText { set; get; }
        public DateTime ResolveDateTime { set; get; }
    }

    public class ConntrollerDescription
    {
        public string Name { set; get; }
        public string ConnectionPort { set; get; }
    }

    public class SendLog
    {
        public string Identity { set; get; }
        public Common.Enumerators.SendLogActionEnum Action { set; get; }
        public DateTime SentTime { set; get; }
        public int Status { set; get; }
        public string Data { set; get; }
    }
}
