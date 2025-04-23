using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using ASFuelControl.VirtualDevices;
using System.IO;
using Telerik.OpenAccess;
using ASFuelControl.Logging;

namespace ASFuelControl.Windows.Threads
{
    public class ThreadController
    {
        #region Event Definitions

        public event EventHandler<WorkFlow.AlarmRaisedEventArgs> AlarmRaised;
        public event EventHandler<QueryResolveAlarmArgs> QueryResolveAlarm;
        public event EventHandler<TimerStartEventArgs> QueryStartTimer;
        public event EventHandler<AlarmResolvedArgs> AlarmResolved;
        public event EventHandler QueryDeviceRefresh;
        public event EventHandler ConntrollerConnectionFailed;
        public event EventHandler ControllerConnectionSuccess;

        #endregion

        #region private variables

        private List<ConntrollerDescription> failedControllers = new List<ConntrollerDescription>();
        private Dictionary<Common.IController, string> controllerNames = new Dictionary<Common.IController, string>();
        private ControllerThread controllerThread;// = new ControllerThread();
        private SendAlertsThread sendingThread;// = new SendAlertsThread();
        private Data.DatabaseModel database;// = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
        private Dictionary<Guid, Common.IController> controllers = new Dictionary<Guid, Common.IController>();
        //private PrintHandler printHandler = new PrintHandler();

        private PrintAgent printAgent = null;// = new PrintAgent();

        private List<Guid> processedInvoices = new List<Guid>();
        private string outFolder;

        
        private int CurrentDBVersion = 8;

        #endregion

        public bool IsRunning
        {
            set;
            get;
        }

        public bool IsStopped
        {
            get { return !this.IsRunning; }
        }

        public ThreadController()
        {
            
        }

        #region public methods

        public void ApplyPrices()
        {
            lock (this.controllerThread)
            {
                this.controllerThread.PriceChanged = true;
            }
        }

        public VirtualTank[] GetTanks()
        {
            return this.controllerThread.GetTanks();
        }

        public VirtualDispenser[] GetDispensers()
        {
            return this.controllerThread.GetDispensers();
        }

        public void StartThreads()
        {
            this.IsRunning = true;

            int version = this.GetDatabaseVersion();
            
            if (version == 0)
            {
                version = 1;
            }
            for(int i = version; i < this.CurrentDBVersion; i++)
            {
                this.UpdateDatabase(i + 1);
            }

            this.database = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
            
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

            //List<Data.ApplicationUser> users1 = this.database.ApplicationUsers.ToList();
            //foreach(Data.ApplicationUser user in users1)
            //{
            //    user.PasswordEncrypted = AESEncryption.Encrypt(user.Password, "Exedron@#");
            //    user.Password = "";
            //}

            //Data.Implementation.OptionHandler.Instance.SetOption(this.database, "OptionsEncrypted", true);
            //this.database.SaveChanges();

            Data.Implementation.OptionHandler.Instance.SetOption(this.database, "DBVersion", this.CurrentDBVersion);

            this.controllerThread = new ControllerThread();
            this.sendingThread = new SendAlertsThread();
            printAgent = new PrintAgent();

            this.controllers.Clear();
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

            if(this.QueryDeviceRefresh != null)
                this.QueryDeviceRefresh(this, new EventArgs());

            this.CheckAndUpdateVersion();
        }

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

                this.database.Add(ev);
                this.database.SaveChanges();
            }
            this.controllerThread.StopControllers();
            this.controllerThread.HaltThread = true;
            this.sendingThread.StopThread();
            this.printAgent.StopThread();
        }

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

        #endregion

        #region private methods

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

        private void InitializeControllers()
        {
            bool suspendAlarms = Data.Implementation.OptionHandler.Instance.GetBoolOption("SuspendAlamrs", false);
            this.outFolder = Data.Implementation.OptionHandler.Instance.GetOption("Samtec_OutFolder");
            foreach (Data.CommunicationController controller in this.database.CommunicationControllers)
            {
                Common.IController controllerInterFace = this.CreateControllerInterface(controller);
                controllers.Add(controller.CommunicationControllerId, controllerInterFace);
            }
            List<Data.Tank> dbTanks = this.database.Tanks.ToList();
            foreach (Data.Tank tank in dbTanks)
            {
                Guid controllerId = this.controllers.Keys.Where(k => k == tank.CommunicationControllerId).FirstOrDefault();
                if (controllerId == Guid.Empty)
                    continue;
                Common.IController controllerInterFace = this.controllers.Where(c => c.Key == controllerId).Select(c => c.Value).FirstOrDefault();
                VirtualDevices.VirtualTank vtank = this.controllerThread.AddTankWorkFlow(controllerInterFace, tank.TankId, tank.Channel, tank.Address, suspendAlarms);

                if (tank.Titrimetries.Count > 0)
                    vtank.TitrimetryLevels = tank.Titrimetries.LastOrDefault().TitrimetryLevels.Select(t => new VirtualTitrimetryLevel(t.Volume.Value, t.Height.Value)).ToArray();

                vtank.DeliveryTime = Data.Implementation.OptionHandler.Instance.GetIntOption("DeliveryWaitingTime", 900) * 1000;
                vtank.LiterCheckTime = Data.Implementation.OptionHandler.Instance.GetIntOption("LiterCheckWaitingTime", 15) * 1000;
                vtank.LastFuelHeight = tank.GetLastValidLevel();
                vtank.LastTemperature = tank.GetLastValidTemperatur();
                vtank.CurrentTemperature = tank.Temperatire;
                vtank.LastWaterHeight = tank.GetLastValidWaterLevel();

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
                vtank.MaxHeight = tank.MaxFuelHeight;
                vtank.MinHeight = tank.MinFuelHeight;
                vtank.MaxWaterHeight = tank.MaxWaterHeight;
                vtank.PriceAverage = tank.GetLastPrice();
                vtank.TotalVolume = tank.TotalVolume;
                vtank.CurrentTemperature = tank.Temperatire;
                vtank.FuelOffset = tank.OffsetVolume;
                vtank.WaterOffset = tank.OffestWater;
                vtank.OrderLimit = tank.OrderLimit.HasValue ? tank.OrderLimit.Value : 0;
                vtank.CurrentFuelLevel = tank.FuelLevel;
                vtank.CurrentWaterLevel = tank.WaterLevel;
                vtank.TankStatus = (Common.Enumerators.TankStatusEnum)tank.PhysicalState;
                vtank.LastValuesUpdate = DateTime.Now;
                vtank.IsVirtualTank = tank.IsVirtual.HasValue ? tank.IsVirtual.Value : false;
            }
            List<VirtualDispenser> dispensers = new List<VirtualDispenser>();
            List<Data.Dispenser> dbDispensers = this.database.Dispensers.ToList();
            foreach (Data.Dispenser dispenser in dbDispensers)
            {
                Guid controllerId = this.controllers.Keys.Where(k => k == dispenser.CommunicationControllerId).FirstOrDefault();
                if (controllerId == Guid.Empty)
                    continue;
                Common.IController controllerInterFace = this.controllers.Where(c => c.Key == controllerId).Select(c => c.Value).FirstOrDefault();
                VirtualDispenser vDispenser = this.controllerThread.AddDispenserWorkFlow(controllerInterFace, dispenser.DispenserId, dispenser.Channel, dispenser.PhysicalAddress, suspendAlarms);
                vDispenser.Status = (Common.Enumerators.FuelPointStatusEnum)dispenser.PhysicalState;
                vDispenser.OfficialNumber = dispenser.OfficialPumpNumber;
                vDispenser.IsValid = dispenser.IsValid;
                vDispenser.DispenserNumber = dispenser.DispenserNumber;
                vDispenser.DecimalPlaces = dispenser.DecimalPlaces.HasValue ? dispenser.DecimalPlaces.Value : 2;
                vDispenser.UnitPriceDecimalPlaces = dispenser.UnitPriceDecimalPlaces.HasValue ? dispenser.UnitPriceDecimalPlaces.Value : 2;
                dispensers.Add(vDispenser);

            }
            foreach (Data.Dispenser dispenser in this.database.Dispensers)
            {
                foreach (Data.Nozzle nozzle in dispenser.Nozzles)
                {
                    VirtualDispenser vDisp = dispensers.Where(d => d.DispenserId == nozzle.DispenserId).FirstOrDefault();
                    if (vDisp == null)
                        continue;
                    VirtualNozzle vNozzle = this.controllerThread.AddNozzle(dispenser.DispenserId, nozzle.NozzleId, nozzle.OrderId, nozzle.OfficialNozzleNumber, nozzle.NozzleState);
                    vNozzle.ParentDispenser = vDisp;
                    vNozzle.CurrentSaleUnitPrice = nozzle.FuelType.CurrentPrice;
                    if (nozzle.FuelType != null)
                    {
                        vNozzle.FuelTypeDescription = nozzle.FuelType.Name;
                        vNozzle.FuelColor = nozzle.FuelType.Color.HasValue ? nozzle.FuelType.Color.Value : 0;
                    }
                    vNozzle.TotalVolumeCounter = nozzle.TotalCounter;
                    vNozzle.LastVolumeCounter = nozzle.LastValidTotalizer;
                    
                }
            }
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
            this.controllerThread.AlarmRaised += new EventHandler<WorkFlow.AlarmRaisedEventArgs>(controllerThread_AlarmRaised);
            this.controllerThread.CheckInvoices += new EventHandler(controllerThread_CheckInvoices);
            this.controllerThread.QueryTotalsUpdate += new EventHandler<FuelPump.QueryTotalsUpdateArgs>(controllerThread_QueryTotalsUpdate);
            this.controllerThread.QueryAlarmResolved += new EventHandler<WorkFlow.QueryAlarmResolvedArgs>(controllerThread_QueryAlarmResolved);
            controllerThread.QueryResolveAlarm += new EventHandler<QueryResolveAlarmArgs>(controllerThread_QueryResolveAlarm);
            this.controllerThread.UpdateNozzleValues += new EventHandler(controllerThread_UpdateNozzleValues);
            this.controllerThread.QueryPrices += new EventHandler(controllerThread_QueryPrices);
            this.controllerThread.PrintInvoice += new EventHandler<PrintInvoiceArgs>(controllerThread_PrintInvoice);
            this.controllerThread.QueryStartTimer += new EventHandler<TimerStartEventArgs>(controllerThread_QueryStartTimer);
            this.controllerThread.UpdateNozzleStatus += new EventHandler<FuelPump.NozzleStatusChangedArgs>(controllerThread_UpdateNozzleStatus);
            this.controllerThread.ControllerConnectionFailed += new EventHandler(controllerThread_ControllerConnectionFailed);
            this.controllerThread.ControllerConnectionSuccess+=new EventHandler(controllerThread_ControllerConnectionSuccess);
        }

        private void ApplySign(string fileName, string fullName)
        {
            string str = System.IO.File.ReadAllText(fullName);
            string id = fileName.Replace(".out", "");
            Guid invoiceId = Guid.Parse(id);
            Data.Invoice invoice = this.database.Invoices.Where(inv => inv.InvoiceId == invoiceId).FirstOrDefault();
            if (invoice == null)
                return;
            int signIndex = str.IndexOf(",");
            if (signIndex < 0)
                return;
            str = str.Substring(7, signIndex - 6);
            invoice.InvoiceSignature = str;
            System.IO.File.Delete(fullName);
        }

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
        
        private void UpdateDatabase(int version)
        {
            string scriptName = "UpdateDatabase" + version.ToString();
            
            string str = Properties.Resources.ResourceManager.GetString(scriptName);
            string[] commands = str.Split(new string[] {"GO\r\n"}, StringSplitOptions.RemoveEmptyEntries);
            foreach (string command in commands)
            {
                try
                {
                    System.Data.SqlClient.SqlCommand c = new System.Data.SqlClient.SqlCommand(command);
                    c.Connection = new System.Data.SqlClient.SqlConnection(Properties.Settings.Default.DBConnection);
                    c.Connection.Open();
                    c.ExecuteNonQuery();//(command, new System.Data.Common.DbParameter[] { });
                    c.Connection.Close();
                }
                catch (Exception ex)
                {
                    Logging.Logger.Instance.LogToFile(string.Format("UpdateDatabase Exception Veriosn {0}", version), ex);
                }

            }
            
        }

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

        void controllerThread_UpdateNozzleStatus(object sender, FuelPump.NozzleStatusChangedArgs e)
        {
            Data.Nozzle nozzle = this.database.Nozzles.Where(n => n.NozzleId == e.NozzleId).FirstOrDefault();
            if (nozzle.NozzleState == (int)e.NozzleStatus)
                return;
            nozzle.NozzleState = (int)e.NozzleStatus;
            this.database.SaveChanges();
        }

        void controllerThread_QueryAlarmResolved(object sender, WorkFlow.QueryAlarmResolvedArgs e)
        {
            if (sender.GetType().GetInterface("ASFuelControl.WorkFlow.ITankWorkFlow") != null)
            {
                var q = this.database.SystemEvents.Where(se => se.TankId == e.DeviceId && !se.ResolvedDate.HasValue);
                this.database.Refresh(RefreshMode.OverwriteChangesFromStore, q);
                List<Data.SystemEvent> events = q.ToList();
                foreach (Data.SystemEvent se in events)
                {
                    se.ResolvedDate = DateTime.Now;
                    se.ResolveMessage = "Auto Resolved";
                    if (this.AlarmResolved != null)
                        this.AlarmResolved(this, new AlarmResolvedArgs(se.EventId));
                }
                if(events.Count > 0)
                    this.database.SaveChanges();
            }
            else if (sender.GetType().GetInterface("ASFuelControl.WorkFlow.IFuelPumpWorkFlow") != null)
            {
                var q = this.database.SystemEvents.Where(se => se.NozzleId == e.DeviceId && !se.ResolvedDate.HasValue);
                this.database.Refresh(RefreshMode.OverwriteChangesFromStore, q);
                List<Data.SystemEvent> events = q.ToList();
                foreach (Data.SystemEvent se in events)
                {
                    se.ResolvedDate = DateTime.Now;
                    se.ResolveMessage = "Auto Resolved";
                    if (this.AlarmResolved != null)
                        this.AlarmResolved(this, new AlarmResolvedArgs(se.EventId));

                    

                }
                if (events.Count > 0)
                    this.database.SaveChanges();

            }

        }

        void controllerThread_AlarmRaised(object sender, WorkFlow.AlarmRaisedEventArgs e)
        {
            Data.SystemEvent existingEvent = new Data.SystemEvent();
            if (e.Alarm.GetType() == typeof(VirtualTankAlarm))
            {
                existingEvent = this.database.SystemEvents.Where(s => !s.ResolvedDate.HasValue && s.TankId == e.Alarm.DeviceId && s.Message == e.Alarm.MessageText).FirstOrDefault();
                if(existingEvent != null)
                    e.Alarm.DeviceDescription = "Δεξαμενή : " + existingEvent.Tank.TankNumber;
            }
            else if (e.Alarm.GetType() == typeof(VirtualNozzleAlarm))
            {
                existingEvent = this.database.SystemEvents.Where(s => !s.ResolvedDate.HasValue && s.NozzleId == e.Alarm.DeviceId && s.Message == e.Alarm.MessageText).FirstOrDefault();
                if (existingEvent != null)
                    e.Alarm.DeviceDescription = "Ακροσωλήνιο : " + existingEvent.Nozzle.Dispenser.OfficialPumpNumber + " " + existingEvent.Nozzle.OfficialNozzleNumber;
            }
            if (existingEvent == null)
            {
                Data.SystemEvent sysEvent = new Data.SystemEvent();
                sysEvent.EventId = Guid.NewGuid();
                database.Add(sysEvent);
                sysEvent.EventDate = DateTime.Now;

                foreach (VirtualAlarmData data in e.Alarm.Data)
                {
                    Data.SystemEventDatum sysData = new Data.SystemEventDatum();
                    sysData.SystemEventDataId = Guid.NewGuid();
                    sysData.SystemEventId = sysEvent.EventId;
                    sysData.PropertyName = data.PropertyName;
                    sysData.Value = data.Value;
                    this.database.Add(sysData);
                }

                if (e.Alarm.GetType() == typeof(VirtualTankAlarm))
                {
                    Data.Tank tank = this.database.Tanks.Where(n => n.TankId == e.Alarm.DeviceId).FirstOrDefault();
                    if (tank == null)
                        return;
                    VirtualTankAlarm tAlarm = e.Alarm as VirtualTankAlarm;
                    sysEvent.EventType = (int)Common.Enumerators.AlertTypeEnum.TankAlert;
                    sysEvent.TankId = tAlarm.DeviceId;
                    sysEvent.AlarmType = (int)tAlarm.AlertType;
                    sysEvent.Message = tAlarm.MessageText == null ? "" : tAlarm.MessageText;

                    e.Alarm.DeviceDescription = "Δεξαμενή : " + tank.TankNumber;
                }
                else if (e.Alarm.GetType() == typeof(VirtualNozzleAlarm))
                {
                    Data.Nozzle nozzle =  this.database.Nozzles.Where(n=>n.NozzleId == e.Alarm.DeviceId).FirstOrDefault();
                    if(nozzle == null)
                        return;
                    VirtualNozzleAlarm nAlarm = e.Alarm as VirtualNozzleAlarm;
                    sysEvent.EventType = (int)Common.Enumerators.AlertTypeEnum.FuelPointError;
                    sysEvent.NozzleId = nAlarm.DeviceId;
                    sysEvent.AlarmType = (int)nAlarm.AlertType;
                    sysEvent.Message = nAlarm.MessageText == null ? "" : nAlarm.MessageText;

                    e.Alarm.DeviceDescription = "Ακροσωλήνιο : " + nozzle.Dispenser.OfficialPumpNumber + " " + nozzle.OfficialNozzleNumber;

                }
                e.Alarm.DatabaseEntityId = sysEvent.EventId;
                
                sysEvent.CRC = this.CalculateCRC32();
                
                database.SaveChanges();
                
            }
            else
            {
                e.Alarm.AlarmTime = existingEvent.EventDate;
                e.Alarm.DatabaseEntityId = existingEvent.EventId;
            }
            if (this.AlarmRaised != null)
            {
                this.AlarmRaised(this, e);
            }
        }

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
                    sysEv.ResolveMessage = alarm.ResolveText;
                }
                if (alarms.Count > 0)
                    this.database.SaveChanges();
            }
        }

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

        void controllerThread_DispenserValuesAvaliable(object sender, DispenserValuesEventArgs e)
        {

            Data.Dispenser dispenser = this.database.Dispensers.Where(t => t.DispenserId == e.DispenserId).FirstOrDefault();
            if (dispenser == null)
                return;
            List<Data.Nozzle> nozzles = dispenser.OrderedNozzles.ToList();
            List<VirtualTank> affectedTanks = new List<VirtualTank>();
            for (int i = 0; i < nozzles.Count; i++)
            {
                if (e.Values.TotalVolumes != null && e.Values.TotalVolumes[i] > 0)
                {
                    nozzles[i].TotalCounter = e.Values.TotalVolumes[i];
                    VirtualNozzle nozzle = this.controllerThread.GetNozzle(nozzles[i].NozzleId);
                    foreach (VirtualTank tank in nozzle.ConnectedTanks)
                    {
                        if (affectedTanks.Contains(tank))
                            continue;
                        affectedTanks.Add(tank);
                    }
                }
            }
            try
            {
                this.database.SaveChanges();
                foreach (VirtualTank vTank in affectedTanks)
                {
                    Data.Tank tank = this.database.Tanks.Where(t => t.TankId == vTank.TankId).FirstOrDefault();
                    if (tank == null)
                        continue;
                    vTank.LastFuelHeight = tank.GetLastValidLevel();
                    vTank.LastWaterHeight = tank.GetLastValidWaterLevel();
                    vTank.LastTemperature = tank.GetLastValidTemperatur();
                }
            }
            catch (Exception ex)
            {
                Logging.Logger.Instance.LogToFile("controllerThread_DispenserValuesAvaliable::AffectedTanks", ex);
            }
        }

        void controllerThread_TankValuesAvaliable(object sender, TankValuesEventArgs e)
        {
            Data.Tank tank = this.database.Tanks.Where(t => t.TankId == e.TankId).FirstOrDefault();
            if (tank == null)
                return;
            tank.FuelLevelBase = e.Values.FuelHeight;
            tank.WaterLevelBase = e.Values.WaterHeight;
            tank.Temperatire = e.Values.CurrentTemperatur;
            this.database.SaveChanges();
            try
            {
                VirtualTank vTank = this.controllerThread.GetTank(tank.TankId);
                vTank.LastFuelHeight = tank.GetLastValidLevel();
                vTank.LastWaterHeight = tank.GetLastValidWaterLevel();
                vTank.LastTemperature = tank.GetLastValidTemperatur();
            }
            catch
            {
            }
            
        }

        void controllerThread_TankFillingAvaliable(object sender, EventArgs e)
        {
            Common.Sales.TankFillingData filling = this.controllerThread.GetNextFilling();
            Data.Tank tank = this.database.Tanks.Where(t => t.TankId == filling.TankId).FirstOrDefault();
            VirtualTank vtank = this.controllerThread.GetTank(tank.TankId);
            if (tank == null || vtank == null)
                return;

            if (filling.InvoiceTypeId != Guid.Empty)
            {
                filling.InvoiceLineId = tank.CreateFillingInvoice(filling);
            }

            Data.TankFilling tf = tank.CreateTankFilling(filling.InvoiceLineId, filling.Values, filling.DeliveryStarted);
            this.database.Add(tf);
            this.database.SaveChanges();
            vtank.LastFuelHeight = tank.GetLastValidLevel();
            vtank.LastTemperature = tank.GetLastValidTemperatur();
            vtank.LastWaterHeight = tank.GetLastValidWaterLevel();
            if (this.AlarmRaised != null)
            {
                VirtualTankFillingInfo info = new VirtualTankFillingInfo();
                info.VolumeInvoiced = tf.VolumeNormalized;
                info.VolumeReal = tf.VolumeRealNormalized;
                info.Difference = tf.VolumeNormalized - tf.VolumeRealNormalized;
                info.DeviceDescription = string.Format("Παραλαβή Δεξαμενής {0}", tank.TankNumber);
                info.MessageText = string.Format("Όγκος Παραστατικού: {0:N2} lt Όγκος Μέτρησης: {1:N2} lt", info.VolumeInvoiced, info.VolumeReal);
                info.AlarmTime = DateTime.Now;
                this.AlarmRaised(this, new WorkFlow.AlarmRaisedEventArgs(info));
            }
        }

        void controllerThread_SaleAvaliable(object sender, WorkFlow.SaleCompletedArgs e)
        {
            Common.Sales.SaleData sale = this.controllerThread.GetNextSale();
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
                    Logger.Instance.LogToFile("controllerThread_SaleAvaliable", string.Format("Nozzle:{0} not found", sale.NozzleId));
                    return;
                }
                newInvoice = nozzle.Dispenser.CreateSale(sale);
                if (newInvoice == null)
                {
                    Logger.Instance.LogToFile("controllerThread_SaleAvaliable", "Invoice not Created");
                    this.controllerThread.RenoveSale();
                    return;
                }
                Data.InvoiceLine invLine = newInvoice.InvoiceLines.FirstOrDefault();
                if (invLine != null)
                    e.InvoiceLineId = invLine.InvoiceLineId;
                //nozzle.TotalCounter = sale.TotalizerEnd;
                //if (sale.TankData == null)
                //{
                //    Logger.Instance.LogToFile("controllerThread_SaleAvaliable", string.Format("TankData not found"));
                //    this.controllerThread.RenoveSale();
                //}
                //foreach (Data.NozzleFlow nf in nozzle.NozzleFlows)
                //{
                //    Common.Sales.TankSaleData tdata = sale.TankData.Where(t => t.TankId == nf.TankId).FirstOrDefault();
                //    if (tdata == null)
                //        continue;
                //    Data.Tank tank = this.database.Tanks.Where(t => t.TankId == nf.TankId).FirstOrDefault();
                //    if (tank == null)
                //        continue;
                //    tdata.EndLevel = tank.FuelLevel;
                //    tdata.EndTemperature = tank.Temperatire;
                //    tdata.EndWaterLevel = tank.WaterLevel;
                //}
                this.controllerThread.RenoveSale();
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

        void controllerThread_UpdateNozzleValues(object sender, EventArgs e)
        {
            VirtualNozzle vNozzle = sender as VirtualNozzle;
            if (vNozzle == null)
                return;
            Data.Nozzle nozzle = this.database.Nozzles.Where(n => n.NozzleId == vNozzle.NozzleId).FirstOrDefault();
            if (nozzle == null)
                return;
            if (nozzle.LastValidTotalizer != vNozzle.LastVolumeCounter || nozzle.TotalCounter != vNozzle.TotalVolumeCounter)
            {
                nozzle.TotalCounter = vNozzle.TotalVolumeCounter;
                vNozzle.LastVolumeCounter = nozzle.LastValidTotalizer; 
                this.database.SaveChanges();
                
            }
        }

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

        void controllerThread_ControllerConnectionFailed(object sender, EventArgs e)
        {
            
            Common.IController controller = sender as Common.IController;
            if(controller == null)
                return;
            if(!this.controllerNames.ContainsKey(controller))
                return;
            string name = this.controllerNames[controller];

            ConntrollerDescription cdesc = this.failedControllers.Where(c=>c.Name == name).FirstOrDefault();
            if(cdesc == null)
            {
                cdesc = new ConntrollerDescription();
                cdesc.Name = name;
                cdesc.ConnectionPort = controller.CommunicationPort;
                this.failedControllers.Add(cdesc);
            }
            if (this.ConntrollerConnectionFailed != null)
                this.ConntrollerConnectionFailed(this.failedControllers, e);
        }

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
}
