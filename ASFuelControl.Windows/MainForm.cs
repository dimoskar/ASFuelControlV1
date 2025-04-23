using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ASFuelControl.Windows.Threads;
using Telerik.WinControls;
using Telerik.WinControls.UI;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace ASFuelControl.Windows
{
    public partial class MainForm : RadForm
    {
        ThreadController controller = new ThreadController();
        UI.Catalogs.TankGrid tankGrid = null;
        UI.Catalogs.DispenserGrid dispenserGrid = null;
        UI.Catalogs.SettingsGrid settingsGrid = null;
        UI.Catalogs.AlertGrid alertGrid = new UI.Catalogs.AlertGrid();
        UI.Catalogs.TraderCatalog traderCatalog = null;
        UI.Catalogs.InvoiceCatalog invoceCatalog = null;
        List<ResolveAlarmData> alertsToResolve = new List<ResolveAlarmData>();
        Timer t1 = new Timer();
        ConcurrentBag<object> submissionSuccessList = new ConcurrentBag<object>();
        ConcurrentBag<object> submissionFailedList = new ConcurrentBag<object>();
        private bool otpEnabled = false;

        List<PopupData> popups = new List<PopupData>();

        private delegate void AddAlarmControlDelegate(VirtualDevices.VirtualBaseAlarm alarm);
        private delegate void ShowFillingDataDelegate(VirtualDevices.VirtualTankFillingInfo info);
        private delegate void ShowHidePanelDelegate(Panel p, bool visible);
        private delegate void SetTextDelegate(Label p, string text, int c);
        private delegate void RemoveControlDelegate(Control c);
        private delegate void UpdateControlDelegate(UI.Controls.AlarmControl c, VirtualDevices.VirtualBaseAlarm alarm);

        /// <summary>
        /// Gets the hreadController instance for the application
        /// </summary>
        public ThreadController ThreadControllerInstance
        {
            get { return this.controller; }
        }

        public MainForm()
        {
            InitializeComponent();

            this.panelSendMessages.Hide();

            this.Load += new EventHandler(MainForm_Load);
            this.Size = new System.Drawing.Size(1280, 768);
            this.mainPanel.ControlRemoved += new ControlEventHandler(mainPanel_ControlRemoved);
            this.controller.SubmissionFailed += new EventHandler(controller_SubmissionFailed);
            this.controller.SubmissionSuccess += new EventHandler(controller_SubmissionSuccess);
            this.controller.OTPStatusChanged += Controller_OTPStatusChanged;

            this.alertGrid = new UI.Catalogs.AlertGrid();
            this.mainPanel.Controls.Add(this.alertGrid);
            this.alertGrid.Dock = DockStyle.Fill;
            this.alertGrid.Hide();
            //this.alertGrid.QueryRemovrControl += AlertGrid_QueryRemovrControl;
            this.alertGrid.QueryCreateSale += AlertGrid_QueryCreateSale;
            this.alertGrid.QueryShowFillingNeededInfo += AlertGrid_QueryShowFillingNeededInfo;
            this.alertGrid.QueryShowDecreasingNeededInfo += AlertGrid_QueryShowDecreasingNeededInfo;
            lockPanel.Parent.Controls.Remove(lockPanel);
            this.Controls.Add(lockPanel);
            lockPanel.Location = new Point(this.panel1.Width + 10, 10);

            this.SizeChanged += MainForm_SizeChanged;
            this.panel1.SizeChanged += MainForm_SizeChanged;
        }

        private void MainForm_SizeChanged(object sender, EventArgs e)
        {
            lockPanel.Location = new Point(this.panel1.Width + 10, 10);
            lockPanel.Size = new Size(this.Width - this.panel1.Width - 40, this.Height - 80);
            lockPanel.BringToFront();
        }

        private void ApplyOTPStatus()
        {
            this.controller.RedefineOTPComntrollers();
            Data.DatabaseModel database = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
            if (otpEnabled)
            {

                Guid id = Program.CurrentShiftId;
                Data.ApplicationUser saleUser = database.ApplicationUsers.Where(a => a.UserLevel == -1).FirstOrDefault();
                Program.CurrentUserName = saleUser.UserName;
                if (saleUser != null)
                {
                    if (id != Guid.Empty)
                    {

                        Data.Shift shift = database.Shifts.Where(s => s.ShiftId == id).First();
                        shift.ShiftEnd = DateTime.Now;
                    }
                    Data.Shift newShift = new Data.Shift();
                    newShift.ShiftId = Guid.NewGuid();
                    newShift.ApplicationUserId = saleUser.ApplicationUserId;
                    newShift.ShiftBegin = DateTime.Now;
                    database.Add(newShift);
                    database.SaveChanges();
                    Program.CurrentShiftId = newShift.ShiftId;
                    Program.CurrentUserLevel = (Common.Enumerators.ApplicationUserLevelEnum)saleUser.UserLevel;
                }

                this.radButton2.Image = Properties.Resources.VendingMachine32;
                this.radButton2.Text = "Απενεργοποίηση Πωλητών";

            }
            else
            {
                Guid id = Program.CurrentShiftId;
                if (id != Guid.Empty)
                {
                    Data.Shift shift = database.Shifts.Where(s => s.ShiftId == id).FirstOrDefault();
                    if (shift != null)
                    {
                        shift.ShiftEnd = DateTime.Now;
                        database.SaveChanges();
                    }
                    Program.CurrentShiftId = Guid.Empty;
                    Program.CurrentUserName = "";
                }
                this.radButton2.Image = Properties.Resources.VendingMachine32_Red;
                this.radButton2.Text = "Ενεργοποίηση Πωλητών";

            }
            Data.Implementation.OptionHandler.Instance.SetOption("[OTPEnabled]", otpEnabled);
            btnChangeShift.Visible = !otpEnabled;

            if (Program.CurrentShiftId == Guid.Empty)
            {
                this.btnChangeShift.Text = "Εναρξη Βάρδιας";
                this.btnChangeShift.Image = Properties.Resources.UserShift;
                //banner.ShowDialog();

            }
            else
            {
                this.btnChangeShift.Text = "Λήξη Βάρδιας";
                this.btnChangeShift.Image = Properties.Resources.UserShiftEnd;

                //banner.TopMost = true;
                //if (banner.Visible)
                //    banner.Close();

            }

            this.label2.Text = Program.CurrentUserName;
            this.ApplyUser();
        }

        private void Controller_OTPStatusChanged(object sender, EventArgs e)
        {
            
            this.otpEnabled = this.controller.OTPStatus;
            this.Invoke(new Action(() =>
                {
                    this.ApplyOTPStatus();
                }));
        }

        //private void AlertGrid_QueryRemovrControl(object sender, UI.Catalogs.QueryRemoveAlert e)
        //{
        //    VirtualDevices.VirtualTank vt = this.controller.GetTanks().Where(t => t.TankId == e.DeviceId).FirstOrDefault();
        //    if (vt != null)
        //    {

        //        if (vt.Alerts.Where(a => a.DatabaseEntityId == e.DatabaseEntryId).Count() > 0)
        //            return;
        //        else
        //        {
        //            e.Remove = true;
        //            if(this.openAlerts.ContainsKey(e.DatabaseEntryId))
        //            {
        //                RadDesktopAlert[] alerts = this.openAlerts[e.DatabaseEntryId].ToArray();
        //                foreach (RadDesktopAlert alert in alerts)
        //                    alert.Hide();
        //            }
        //            this.openAlerts.Remove(e.DatabaseEntryId);
        //        }
        //    }
        //    VirtualDevices.VirtualDispenser vd = this.controller.GetDispensers().Where(t => t.DispenserId == e.DeviceId).FirstOrDefault();
        //    if (vt != null)
        //    {

        //        if (vd.Alerts.Where(a => a.DatabaseEntityId == e.DatabaseEntryId).Count() > 0)
        //            return;
        //        else
        //            e.Remove = true;
        //    }
        //    VirtualDevices.VirtualNozzle vn = this.controller.GetDispensers().SelectMany(d => d.Nozzles).Where(t => t.NozzleId == e.DeviceId).FirstOrDefault();
        //    if (vn != null)
        //    {

        //        if (vn.Alerts.Where(a => a.DatabaseEntityId == e.DatabaseEntryId).Count() > 0)
        //            return;
        //        else
        //            e.Remove = true;
        //    }
        //}

        private void AlertGrid_QueryShowFillingNeededInfo(object sender, EventArgs e)
        {
            VirtualDevices.VirtualBaseAlarm alarm = sender as VirtualDevices.VirtualBaseAlarm;
            if (alarm == null)
                return;
            this.Invoke(new Action(() => this.ShowFillingNeededInfo(alarm)));
        }

        private void AlertGrid_QueryShowDecreasingNeededInfo(object sender, EventArgs e)
        {
            VirtualDevices.VirtualBaseAlarm alarm = sender as VirtualDevices.VirtualBaseAlarm;
            if (alarm == null)
                return;
            this.Invoke(new Action(() => this.ShowDecreasingNeededInfo(alarm)));
        }

        private void AlertGrid_QueryCreateSale(object sender, UI.Catalogs.QueryCreateSaleEventArguments e)
        {
            this.controller.CreateSale(e.DeviceId, e.PreviousVolume, e.CurrentVolume);
        }

        public void SimulateSendSuccess(object sender)
        {
            this.submissionSuccessList.Add(sender);
        }

        void controller_SubmissionSuccess(object sender, EventArgs e)
        {
            this.submissionSuccessList.Add(sender);

            //this.ShowSendSuccessInfo(sender);
        }

        void controller_SubmissionFailed(object sender, EventArgs e)
        {
            this.submissionFailedList.Add(sender);
            //this.ShowSendFailedInfo(sender);
        }

        public void RefreshTankOrderData(Guid tankId, decimal orderLimit)
        {
            VirtualDevices.VirtualTank[] tanks = this.controller.GetTanks();
            VirtualDevices.VirtualTank tank = tanks.Where(t => t.TankId == tankId).FirstOrDefault();
            if (tank == null)
                return;
            tank.OrderLimit = orderLimit;
        }

        void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                controller.CheckForDatabaseUpdate();
                lockPanel.Visible = Data.Implementation.OptionHandler.Instance.GetIntOption("ApplicationLocked", 777) != 777;
                Exedron.MyData.Settings.IsActive = Data.Implementation.OptionHandler.Instance.GetBoolOption("MyDataIsActive", false);
                Exedron.MyData.Settings.Username = Data.Implementation.OptionHandler.Instance.GetOption("MyDataUserName", "");
                Exedron.MyData.Settings.SubscriptionKey = Data.Implementation.OptionHandler.Instance.GetOption("MyDataSubscriptionKey", "");

                this.radButton2.Visible = Data.Implementation.OptionHandler.Instance.HasOTPEnabled;
                this.otpEnabled = Data.Implementation.OptionHandler.Instance.GetBoolOption("[OTPEnabled]", false);
                if (!otpEnabled)
                {
                    this.radButton2.Image = Properties.Resources.VendingMachine32_Red;
                    this.radButton2.Text = "Ενεργοποίηση Πωλητών";
                }
                else
                {
                    this.radButton2.Image = Properties.Resources.VendingMachine32;
                    this.radButton2.Text = "Απενεργοποίηση Πωλητών";
                }
                btnChangeShift.Visible = !otpEnabled;

                //Threads.AlertHandler.Instance.AlarmAdded += new EventHandler(Instance_AlarmAdded);
                //Threads.AlertHandler.Instance.AlarmResolved += new EventHandler(Instance_AlarmResolved);
                //Threads.AlertHandler.Instance.AlarmUpdated += new EventHandler(Instance_AlarmUpdated);

                Threads.AlertChecker.Instance.AlarmAdded += new EventHandler(Instance_AlarmAdded);
                Threads.AlertChecker.Instance.AlarmResolved += new EventHandler(Instance_AlarmResolved);
                Threads.AlertChecker.Instance.AlarmUpdated += new EventHandler(Instance_AlarmUpdated);

                t1.Interval = 500;
                t1.Start();
                t1.Tick += new EventHandler(t1_Tick);
                bool startThreads = Data.Implementation.OptionHandler.Instance.GetBoolOption("StartThreadsOnStart", false);
                if (startThreads)
                    UI.Forms.LoadWaitingForm.ShowForm();


                controller.QueryDeviceRefresh += new EventHandler(controller_QueryDeviceRefresh);
                controller.QueryStartTimer += new EventHandler<TimerStartEventArgs>(controller_QueryStartTimer);
                controller.ConntrollerConnectionFailed += new EventHandler(controller_ConntrollerConnectionFailed);
                controller.ControllerConnectionSuccess += new EventHandler(controller_ControllerConnectionSuccess);
                controller.TankFillingAvaliableEvent += new EventHandler<TankFillingAvaliableArgs>(controller_TankFillingAvaliableEvent);
                //controller.RefreshAlerts += new EventHandler(controller_RefreshAlerts);

                if (startThreads)
                    controller.StartThreads();



                //this.label2.Text = Program.CurrentUserName;
                //this.ApplyUser();
                if (Program.CurrentShiftId == Guid.Empty)
                {
                    this.btnChangeShift.Text = "Εναρξη Βάρδιας";
                    this.btnChangeShift.Image = Properties.Resources.UserShift;
                }
                else
                {
                    this.btnChangeShift.Text = "Λήξη Βάρδιας";
                    this.btnChangeShift.Image = Properties.Resources.UserShiftEnd;
                }
                this.label2.Text = Program.CurrentUserName;
                this.ApplyUser();
                Program.AdminConnectedEvent += new EventHandler(Program_AdminConnectedEvent);

                UI.Forms.LoadWaitingForm.CloseDialog();

                this.radButton2_Click(this.btnPumps, new EventArgs());
                this.radButton2.Visible = this.controller.GetOtpControllers().Length > 0;
            }
            catch (Exception ex)
            {
                Logging.Logger.Instance.LogToFile("MainForm_Load", ex);
            }
        }

        void Instance_AlarmUpdated(object sender, EventArgs e)
        {
            if (sender.GetType() != typeof(Data.SystemEvent))
                return;
            if (this.IsDisposed)
                return;
            Data.SystemEvent se = sender as Data.SystemEvent;
            VirtualDevices.VirtualBaseAlarm alarm = this.CreateAlarm(se);
            //VirtualDevices.VirtualBaseAlarm alarm = sender as VirtualDevices.VirtualBaseAlarm;
            if (alarm == null)
                return;
            if (!this.IsDisposed)
            {
                if (this.InvokeRequired)
                {
                    if (this.alertGrid.IsDisposed)
                        return;
                    this.Invoke(new Action(() => this.alertGrid.UpdateAlert(alarm)));
                    this.Invoke(new Action(() => this.SetAlertButtonStyle()));
                    return;
                }
                this.alertGrid.UpdateAlert(alarm);
                SetAlertButtonStyle();
            }
        }

        /// <summary>
        /// Removes the Alert Notification control after an alert is removed
        /// </summary>
        /// <param name="c"></param>
        //private void RemoveAlertControl(Control c)
        //{
        //    UI.Controls.AlarmControl ac = c as UI.Controls.AlarmControl;
            
        //    this.panel5.Controls.Remove(c);
        //    try
        //    {
        //        if (ac != null)
        //        {
        //            PopupData data = this.popups.Where(p => p.AlertId == ac.CurrentAlarm.DatabaseEntityId).FirstOrDefault();
        //            if (data == null)
        //                return;
        //            data.PopupWindow.Hide();
        //            data.PopupWindow.Dispose();
        //            this.popups.Remove(data);
        //        }
        //    }
        //    catch(Exception ex)
        //    {
        //        Logging.Logger.Instance.LogToFile("RemoveAlertControl", ex);
        //    }
            
        //}

        //private void UpdateAlertControl(UI.Controls.AlarmControl c, VirtualDevices.VirtualBaseAlarm alarm)
        //{
        //    if (c == null)
        //    {
        //        this.AddAlarmControl(alarm);
        //        return;
        //    }
        //    c.CurrentAlarm = alarm;
        //}

        /// <summary>
        /// Event tha fires after an alert is resolved
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Instance_AlarmResolved(object sender, EventArgs e)
        {
            if (sender.GetType() != typeof(Data.SystemEvent))
                return;
            Data.SystemEvent se = sender as Data.SystemEvent;
            if(se.TankId.HasValue)
            {
                VirtualDevices.VirtualTank tank = this.controller.GetTanks().Where(t => t.TankId == se.TankId.Value).FirstOrDefault();
                if (tank != null)
                {
                    if (
                        (Common.Enumerators.AlarmEnum)se.AlarmType == Common.Enumerators.AlarmEnum.FuelDecrease || 
                        (Common.Enumerators.AlarmEnum)se.AlarmType == Common.Enumerators.AlarmEnum.FuelIncrease)
                        if (System.IO.File.Exists("TankLock.txt"))
                        {
                            System.IO.File.AppendAllText("TankLock.txt", string.Format("Tankk {0} Unlocked at {1:dd/MM/yyyy HH:mm:ss}\r\n", tank.TankNumber, DateTime.Now));
                        }
                    tank.TankDeliverySensed = false; 
                }
            }
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => this.alertGrid.RemoveAlertControl(se.EventId)));
                this.Invoke(new Action(() => this.SetAlertButtonStyle()));
                this.Invoke(new Action(() => 
                    {
                        if (se.TankId.HasValue)
                        {
                            lock (this.openAlerts)
                            {
                                if (this.openAlerts.ContainsKey(se.EventId))
                                {
                                    RadDesktopAlert[] alerts = this.openAlerts[se.EventId].ToArray();
                                    foreach (RadDesktopAlert alert in alerts)
                                        alert.Hide();
                                    //VirtualDevices.VirtualTank tank = this.controller.GetTanks().FirstOrDefault(t => t.TankId == se.TankId);
                                    //if(tank != null)
                                    //    tank.TankDeliverySensed = false;
                                }
                                this.openAlerts.Remove(se.EventId);
                            }
                        }
                    }));
                return;
            }
            this.alertGrid.RemoveAlertControl(se.EventId);
            this.SetAlertButtonStyle();
            if (se.TankId.HasValue)
            {
                lock (this.openAlerts)
                {
                    if (this.openAlerts.ContainsKey(se.EventId))
                    {
                        RadDesktopAlert[] alerts = this.openAlerts[se.EventId].ToArray();
                        foreach (RadDesktopAlert alert in alerts)
                            alert.Hide();
                    }
                    this.openAlerts.Remove(se.EventId);
                }
            }
        }

        /// <summary>
        /// Event tha fires after an alert is added
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Instance_AlarmAdded(object sender, EventArgs e)
        {
            if (sender.GetType() != typeof(Data.SystemEvent))
                return;
            Data.SystemEvent se = sender as Data.SystemEvent;
            VirtualDevices.VirtualBaseAlarm alarm = this.CreateAlarm(se);
            if (alarm == null)
                return;
            try
            {
                bool checkState = Data.Implementation.OptionHandler.Instance.GetBoolOption("LockOnAlert", true);
                if (checkState)
                {
                    VirtualDevices.VirtualTank tank = this.controller.GetTanks().Where(t => t.TankId == alarm.DeviceId).FirstOrDefault();
                    if (tank != null)
                    {
                        if (alarm.AlertType == Common.Enumerators.AlarmEnum.FuelDecrease || alarm.AlertType == Common.Enumerators.AlarmEnum.FuelIncrease)
                        {
                            if (System.IO.File.Exists("TankLock.txt"))
                            {
                                System.IO.File.AppendAllText("TankLock.txt", string.Format("Tankk {0} Locked at {1:dd/MM/yyyy HH:mm:ss}\r\n", tank.TankNumber, DateTime.Now));
                            }
                            tank.TankDeliverySensed = true;
                        }
                    }
                }
            }
            catch
            {

            }
            this.Invoke(new AddAlarmControlDelegate(this.AddAlarmControl), new object[] { alarm });
        }

        private VirtualDevices.VirtualBaseAlarm CreateAlarm(Data.SystemEvent se)
        {
            
            VirtualDevices.VirtualBaseAlarm newAlarm = null;


            if (se.TankId.HasValue)
            {
                newAlarm = new VirtualDevices.VirtualBaseAlarm();
                newAlarm.DeviceId = se.TankId.Value;
            }
            else if (se.DispenserId.HasValue)
            {
                newAlarm = new VirtualDevices.VirtualDispenserAlarm();
                newAlarm.DeviceId = se.DispenserId.Value;
            }
            else if (se.NozzleId.HasValue)
            {
                newAlarm = new VirtualDevices.VirtualNozzleAlarm();
                newAlarm.DeviceId = se.NozzleId.Value;
            }
            else
            {
                newAlarm = new VirtualDevices.VirtualBaseAlarm();
            }
            newAlarm.AlarmTime = se.EventDate;
            newAlarm.DatabaseEntityId = se.EventId;
            newAlarm.DeviceDescription = se.DeviceDescription;
            newAlarm.MessageText = se.Message;
            if(se.AlarmType.HasValue)
                newAlarm.AlertType = (Common.Enumerators.AlarmEnum)se.AlarmType;
            else
                newAlarm.AlertType = Common.Enumerators.AlarmEnum.ProgramTermination;
            foreach (Data.SystemEventDatum sed in se.SystemEventData)
            {
                newAlarm.AddData(sed.PropertyName, sed.Value);
            }

            return newAlarm;
        }

        //void controller_RefreshAlerts(object sender, EventArgs e)
        //{
        //    VirtualDevices.VirtualTank[] tanks = this.controller.GetTanks();
        //    VirtualDevices.VirtualDispenser[] dispensers = this.controller.GetDispensers();
        //    VirtualDevices.VirtualBaseAlarm[] alarms = this.panel5.Controls.OfType<UI.Controls.AlarmControl>().Select(ac=>ac.CurrentAlarm).ToArray();
        //    List<VirtualDevices.VirtualBaseAlarm> alertsToKeep = new List<VirtualDevices.VirtualBaseAlarm>();
        //    List<VirtualDevices.VirtualBaseAlarm> newAlerts = new List<VirtualDevices.VirtualBaseAlarm>();
        //    foreach (VirtualDevices.VirtualTank tank in tanks)
        //    {
        //        foreach (VirtualDevices.VirtualTankAlarm tAlarm in tank.Alerts)
        //        {
        //            if (tAlarm.DeviceId != tank.TankId)
        //                continue;
        //            List<VirtualDevices.VirtualBaseAlarm> validAlerts = alarms.Where(a=>a.DeviceId == tAlarm.DeviceId && ((VirtualDevices.VirtualTankAlarm)a).AlarmType == tAlarm.AlarmType).ToList();
        //            if (validAlerts.Count > 0)
        //            {
        //                foreach (VirtualDevices.VirtualBaseAlarm va in validAlerts)
        //                    alertsToKeep.Add(va);
        //            }
        //            else
        //                newAlerts.Add(tAlarm);
        //            tAlarm.ExistingAlarm = false;
        //        }
        //    }
            
        //    foreach (VirtualDevices.VirtualDispenser dispenser in dispensers)
        //    {
        //        foreach (VirtualDevices.VirtualDispenserAlarm tAlarm in dispenser.Alerts)
        //        {
        //            if (tAlarm.DeviceId != dispenser.DispenserId)
        //                continue;
        //            List<VirtualDevices.VirtualBaseAlarm> validAlerts = alarms.Where(a => a.DeviceId == tAlarm.DeviceId && ((VirtualDevices.VirtualDispenserAlarm)a).AlertType == tAlarm.AlertType).ToList();
        //            if (validAlerts.Count > 0)
        //            {
        //                foreach (VirtualDevices.VirtualBaseAlarm va in validAlerts)
        //                    alertsToKeep.Add(va);
        //            }
        //            else
        //                newAlerts.Add(tAlarm);
        //            tAlarm.ExistingAlarm = false;
        //        }
        //        foreach (VirtualDevices.VirtualNozzle nozzle in dispenser.Nozzles)
        //        {
        //            foreach (VirtualDevices.VirtualNozzleAlarm tAlarm in nozzle.Alerts)
        //            {
        //                if (tAlarm.DeviceId != nozzle.NozzleId)
        //                    continue;
        //                List<VirtualDevices.VirtualBaseAlarm> validAlerts = alarms.Where(a => a.DeviceId == tAlarm.DeviceId && ((VirtualDevices.VirtualNozzleAlarm)a).AlertType == tAlarm.AlertType).ToList();
        //                if (validAlerts.Count > 0)
        //                {
        //                    foreach (VirtualDevices.VirtualBaseAlarm va in validAlerts)
        //                        alertsToKeep.Add(va);
        //                }
        //                else
        //                    newAlerts.Add(tAlarm);
        //                tAlarm.ExistingAlarm = false;
        //            }
        //        }
        //    }
        //    foreach (VirtualDevices.VirtualBaseAlarm alert in newAlerts)
        //    {
        //        this.controller.RaiseAlarm(alert);
        //        this.Invoke(new AddAlarmControlDelegate(this.AddAlarmControl), new object[] { alert });
        //    }
        //    foreach (VirtualDevices.VirtualBaseAlarm alert in alarms)
        //    {
        //        if (alertsToKeep.Contains(alert))
        //            continue;
        //        UI.Controls.AlarmControl control = this.panel5.Controls.OfType<UI.Controls.AlarmControl>().Where(ac => ac.CurrentAlarm.Equals(alert)).FirstOrDefault();
        //        this.Invoke(new RemoveControlDelegate(this.RemoveAlertControl), new object[] { control });
        //    }

        //}

        int secondIndex = 0;
        int tankAlarmCheck = 0;
        void t1_Tick(object sender, EventArgs e)
        {
            if (this.controller == null)
                return;

            this.clockLabel.Text = DateTime.Now.ToString("HH:mm:ss");
            secondIndex++;
            tankAlarmCheck++;

            if(tankAlarmCheck > 30)
            {
                VirtualDevices.VirtualTank[] tanks = this.controller.GetTanks();
                foreach (VirtualDevices.VirtualTank vt in tanks)
                {
                    if (System.IO.File.Exists("TankLock.txt"))
                    {
                        System.IO.File.AppendAllText("TankLock.txt", string.Format("Tank {0} Unlocked at {1:dd/MM/yyyy HH:mm:ss} Schedule\r\n", vt.TankNumber, DateTime.Now));
                    }
                    vt.TankDeliverySensed = controller.HasTankAlerts(vt.TankId);
                }
                tankAlarmCheck = 0;
            }
            if (secondIndex > 5)
            {
                lock (submissionSuccessList)
                {
                    if (this.submissionSuccessList.Count > 0)
                    {
                        for (int i = 0; i < this.submissionSuccessList.Count; i++)
                        {
                            object obj = null;
                            this.submissionSuccessList.TryPeek(out obj);
                            if (obj != null)
                                this.ShowSendSuccessInfo(obj);
                        }
                        this.submissionSuccessList = new ConcurrentBag<object>();
                    }
                    //else
                    //    this.HideSendSuccessInfo();
                }
                lock (submissionFailedList)
                {
                    if (this.submissionFailedList.Count > 0)
                    {
                        for (int i = 0; i < this.submissionFailedList.Count; i++)
                        {
                            object obj = null;
                            this.submissionFailedList.TryPeek(out obj);
                            if (obj != null)
                                this.ShowSendFailedInfo(obj);
                        }
                        this.submissionFailedList = new ConcurrentBag<object>();
                    }
                }
                secondIndex = 0;
            }
        }

        /// <summary>
        /// Creates a sale initiated outside of the main thread of the controller. i.e. Alarm.
        /// </summary>
        /// <param name="nozzleId"></param>
        /// <param name="prevVol"></param>
        /// <param name="curVol"></param>
        public void CreateSale(Guid nozzleId, decimal prevVol, decimal curVol)
        {
            this.controller.CreateSale(nozzleId, prevVol, curVol);
//            this.controller.Sa
        }

        //void controller_RefreshAlerts(object sender, EventArgs e)
        //{
        //    if (this.InvokeRequired)
        //    {
        //        this.Invoke(new Action(() => this.alertGrid.RefreshAlerts()));
        //        return;
        //    }
        //    this.alertGrid.RefreshAlerts();
        //    this.SetAlertButtonStyle();
        //}

        void Program_AdminConnectedEvent(object sender, EventArgs e)
        {
            this.adminPanel.Visible = Program.AdminConnected;
            if (Program.AdminConnected)
                this.footerPanel.Visible = true;
            else
            {
                if (controllerPanel.Visible)
                    this.footerPanel.Visible = true;
                else
                    this.footerPanel.Visible = false;
            }
        }

        private void ShowHidePanel(Panel p, bool visible)
        {
            if (Program.AdminConnected && !visible)
            {
                controllerPanel.Visible = visible;
            }
            else
            {
                footerPanel.Visible = visible;
                controllerPanel.Visible = visible;
            }
        }

        private void SetText(Label label, string text, int c)
        {
            label.Text = text;
            label.Height = 25 * c;
        }

        private void ApplyUser()
        {
            return;
            if (Program.CurrentShiftId == Guid.Empty)
            {
                //this.btnSettings.Enabled = false;
                this.btnPumps.Enabled = false;
                this.btnTanks.Enabled = false;
                //this.btnStats.Enabled = false;
                return;
            }
            if (Program.CurrentUserLevel == Common.Enumerators.ApplicationUserLevelEnum.Administrator)
            {
                //this.btnSettings.Enabled = true;
                this.btnPumps.Enabled = true;
                this.btnTanks.Enabled = true;
                //this.btnStats.Enabled = true;
                this.btnChangeUser.Enabled = true;
            }
            else if (Program.CurrentUserLevel == Common.Enumerators.ApplicationUserLevelEnum.SuperUser)
            {
                //this.btnSettings.Enabled = true;
                this.btnPumps.Enabled = true;
                this.btnTanks.Enabled = true;
                //this.btnStats.Enabled = true;
                this.btnChangeUser.Enabled = true;
            }
            else if (Program.CurrentUserLevel == Common.Enumerators.ApplicationUserLevelEnum.User)
            {
                //this.btnSettings.Enabled = false;
                this.btnPumps.Enabled = true;
                this.btnTanks.Enabled = true;
                //this.btnStats.Enabled = false;
                this.btnChangeUser.Enabled = true;
            }
            else
            {
                //this.btnSettings.Enabled = false;
                this.btnPumps.Enabled = true;
                this.btnTanks.Enabled = true;
                //this.btnStats.Enabled = true;
                this.btnChangeUser.Enabled = false;
            }
            if (this.settingsGrid != null)
                this.settingsGrid.ApplyUser();
        }

        private void SetAlertButtonStyle()
        {
            string text = this.alertGrid.CurrentAlerts > 0 ? " (" + this.alertGrid.CurrentAlerts.ToString() + ")" : "";
            this.btnAlerts.Text = "Συναγερμοί" + text;
            if (text != "")
            {
                this.btnAlerts.Font = new Font(this.btnPumps.Font, FontStyle.Bold);
                this.btnAlerts.Image = Properties.Resources.Alert32Active;
                this.btnAlerts.Visible = true;

            }
            else
            {
                this.btnAlerts.Font = new Font(this.btnPumps.Font, FontStyle.Regular);
                this.btnAlerts.Image = Properties.Resources.Alert32;
                this.btnAlerts.Visible = false;
            }
        }

        private void AddAlarmControl(VirtualDevices.VirtualBaseAlarm alarm)
        {
            this.alertGrid.AddAlert(alarm);
            SetAlertButtonStyle();
            //foreach (UI.Controls.AlarmControl aControl in this.panel5.Controls)
            //{
            //    if (aControl.CurrentAlarm.DatabaseEntityId == alarm.DatabaseEntityId && aControl.CurrentAlarm.AlertType == alarm.AlertType)
            //    {
            //        return;
            //    }
            //}
            //this.panel5.SuspendLayout();
            //UI.Controls.AlarmControl control = new UI.Controls.AlarmControl();
            //control.QueryCreateSaleEvent += new EventHandler(control_QueryCreateSaleEvent);
            ////control.Height = 60;
            //if (alarm.GetType() == typeof(VirtualDevices.VirtualBaseAlarm))
            //{
            //    //control.DeviceType = "Δεξαμενή";
            //    //VirtualDevices.VirtualTankAlarm tankAlert = (VirtualDevices.VirtualTankAlarm)alarm;
            //    if (alarm.AlertType == Common.Enumerators.AlarmEnum.FuelIncrease)
            //    {
            //        this.ShowFillingNeededInfo(alarm);
            //    }

            //}
            //else if (alarm.GetType() == typeof(VirtualDevices.VirtualNozzleAlarm))
            //{
            //    control.DeviceType = "Ακροσωλήνιο";
            //}
            //else if (alarm.GetType() == typeof(VirtualDevices.VirtualTankFillingInfo))
            //{
            //    control.DeviceType = "Παραλαβή";
            //}
            //else
            //{
            //    int pos = alarm.MessageText.IndexOf("Σύνολο Πωλήσεων:");
            //    if (pos > 0)
            //    {
            //        string str = alarm.MessageText.Substring(pos);
            //        str = str.Replace("Σύνολο Πωλήσεων: ", "");
            //        str = str.Replace("Lt", "");
            //        decimal volume = 0;
            //        if (decimal.TryParse(str, out volume))
            //        {
            //            if (volume < 1000)
            //            {
            //                control.QueryCreateSaleEvent -= new EventHandler(control_QueryCreateSaleEvent);
            //                control.Dispose();
            //                this.panel5.ResumeLayout();
            //                return;
            //            }
            //        }
            //    }
            //}
            //control.CurrentAlarm = alarm;
            //control.Width = this.panel5.Width - 25;
            //this.panel5.Controls.Add(control);

            //this.panel5.ResumeLayout();
            ////Control maxC = null;
            ////foreach (Control c in this.panel5.Controls)
            ////{
            ////    if (maxC == null)
            ////        maxC = c;
            ////    if (maxC.Location.Y < c.Location.Y)
            ////        maxC = c;
            ////}
            ////if (maxC.Height + maxC.Location.Y > this.panel5.Height)
            ////{
            ////    this.panel5.Height = maxC.Height + maxC.Location.Y;
            ////}
            ////control.QueryResolveAlert += new EventHandler<QueryResolveAlarmArgs>(control_QueryResolveAlert);
        }

        private void ShowFillingNeededInfo(VirtualDevices.VirtualBaseAlarm tankAlert)
        {
            try
            {
                VirtualDevices.VirtualAlarmData cd = tankAlert.Data.Where(d => d.PropertyName == "CurrentFuelLevel").FirstOrDefault();
                VirtualDevices.VirtualAlarmData ld = tankAlert.Data.Where(d => d.PropertyName == "LastFuelLevel").FirstOrDefault();
                
                decimal currentLevel = cd != null ? decimal.Parse(cd.Value) : 0;
                decimal lastLevel = ld != null ? decimal.Parse(ld.Value) : 0;

                RadDesktopAlert alert = new RadDesktopAlert();
                alert.ShowOptionsButton = false;
                alert.ShowPinButton = false;
                alert.FixedSize = new Size(300, 200);
                alert.AutoClose = false;
                alert.ShowCloseButton = false;
                //ankAlert.
                alert.CaptionText = tankAlert.DeviceDescription + "\r\n(" + tankAlert.AlarmTime.ToString("dd/MM/yyyy HH:mm" + ")");
                alert.ContentText = string.Format("Ανιχνέυτηκε αύξηση στην στάθμη της δεξαμενής.\r\nΤελ. Στάθμη : {0:N2}.\r\nΤωρινή στάθμη : {1:N2}\r\nΠατήστε 'Παραλαβή' για την καταχώρηση του παραστατικού", lastLevel, currentLevel);
                RadButtonElement buttonElement = new RadButtonElement("Παραλαβή");
                buttonElement.Font = new System.Drawing.Font(this.Font.FontFamily, 12);
                buttonElement.Click += new EventHandler(buttonElement_Click);
                buttonElement.BorderThickness = new System.Windows.Forms.Padding(1);
                buttonElement.Image = new Bitmap(Properties.Resources.FillingStart, new Size(20, 20));
                buttonElement.TextImageRelation = TextImageRelation.ImageBeforeText;
                buttonElement.Size = new System.Drawing.Size(100, buttonElement.Size.Height);
                buttonElement.BackColor = Color.DarkGray;
                buttonElement.ShowBorder = true;

                VirtualDevices.VirtualTank tank = this.controller.GetTanks().Where(t => t.TankId == tankAlert.DeviceId).FirstOrDefault();
                //if(tank != null && Data.Implementation.OptionHandler.Instance.GetBoolOption("LockOnAlert", true))
                //{
                //    tank.TankDeliverySensed = true;
                //}

                PopupData data = new PopupData();
                data.TankId = tankAlert.DeviceId;
                data.PopupWindow = alert;
                data.AlertId = tankAlert.DatabaseEntityId;
                data.Alert = tankAlert;
                buttonElement.Tag = data;
                alert.ButtonItems.Add(buttonElement);
                popups.Add(data);
                alert.Closed += Alert_Closed;
                
                if (openAlerts.ContainsKey(tankAlert.DatabaseEntityId))
                    this.openAlerts[tankAlert.DatabaseEntityId].Add(alert);
                else
                    this.openAlerts.Add(tankAlert.DatabaseEntityId, new List<RadDesktopAlert>() { alert });
                alert.Show();
            }
            catch
            {
            }
        }

        private void ShowDecreasingNeededInfo(VirtualDevices.VirtualBaseAlarm tankAlert)
        {
            try
            {
                VirtualDevices.VirtualAlarmData cd = tankAlert.Data.Where(d => d.PropertyName == "CurrentFuelLevel").FirstOrDefault();
                VirtualDevices.VirtualAlarmData ld = tankAlert.Data.Where(d => d.PropertyName == "LastFuelLevel").FirstOrDefault();

                decimal currentLevel = cd != null ? decimal.Parse(cd.Value) : 0;
                decimal lastLevel = ld != null ? decimal.Parse(ld.Value) : 0;

                RadDesktopAlert alert = new RadDesktopAlert();
                
                alert.ShowOptionsButton = false;
                alert.ShowPinButton = false;
                alert.FixedSize = new Size(300, 200);
                alert.AutoClose = false;
                alert.ShowCloseButton = false;
                
                //ankAlert.
                alert.CaptionText = tankAlert.DeviceDescription + "\r\n(" + tankAlert.AlarmTime.ToString("dd/MM/yyyy HH:mm" + ")");
                alert.ContentText = string.Format("Ανιχνέυτηκε μείωση στην στάθμη της δεξαμενής.\r\nΤελ. Στάθμη : {0:N2}.\r\nΤωρινή στάθμη : {1:N2}\r\nΠατήστε 'Παραλαβή' για την καταχώρηση του παραστατικού", lastLevel, currentLevel);
                RadButtonElement buttonElement = new RadButtonElement("Εξαγωγή");
                buttonElement.Font = new System.Drawing.Font(this.Font.FontFamily, 12);
                buttonElement.Click += new EventHandler(buttonElementDecrease_Click);
                buttonElement.BorderThickness = new System.Windows.Forms.Padding(1);
                buttonElement.Image = new Bitmap(Properties.Resources.FillingStart, new Size(20, 20));
                buttonElement.TextImageRelation = TextImageRelation.ImageBeforeText;
                buttonElement.Size = new System.Drawing.Size(100, buttonElement.Size.Height);
                buttonElement.BackColor = Color.DarkGray;
                buttonElement.ShowBorder = true;
                


                VirtualDevices.VirtualTank tank = this.controller.GetTanks().Where(t => t.TankId == tankAlert.DeviceId).FirstOrDefault();
                //if (tank != null && Data.Implementation.OptionHandler.Instance.GetBoolOption("LockOnAlert", true))
                //{
                //    tank.TankDeliverySensed = true;
                //}

                PopupData data = new PopupData();
                data.TankId = tankAlert.DeviceId;
                data.PopupWindow = alert;
                data.AlertId = tankAlert.DatabaseEntityId;
                data.Alert = tankAlert;
                buttonElement.Tag = data;
                alert.ButtonItems.Add(buttonElement);
                popups.Add(data);
                alert.Closed += Alert_Closed;
                
                if (openAlerts.ContainsKey(tankAlert.DatabaseEntityId))
                    this.openAlerts[tankAlert.DatabaseEntityId].Add(alert);
                else
                    this.openAlerts.Add(tankAlert.DatabaseEntityId, new List<RadDesktopAlert>() { alert });
                alert.Show();

            }
            catch
            {
            }
        }

        Dictionary<Guid, List<RadDesktopAlert>> openAlerts = new Dictionary<Guid, List<RadDesktopAlert>>();

        private void Alert_Closed(object sender, RadPopupClosedEventArgs args)
        {
            RadDesktopAlert alert = sender as RadDesktopAlert;
            List<RadDesktopAlert> list = this.openAlerts.Values.Where(v => v.Contains(alert)).FirstOrDefault();
            list.Remove(alert);
            List<Guid> toRemove = new List<Guid>();
            foreach (Guid key in this.openAlerts.Keys)
            {
                if (openAlerts[key].Count == 0)
                    toRemove.Add(key);
            }
            foreach (Guid key in toRemove)
                this.openAlerts.Remove(key);
        }

        void buttonElement_Click(object sender, EventArgs e)
        {
            using (UI.SelectionForms.SelectInvoiceForm siv = new UI.SelectionForms.SelectInvoiceForm())
            {
                RadButtonElement buttonElement = (RadButtonElement)sender;
                PopupData data = buttonElement.Tag as PopupData;
                decimal lastValue = -1;
                if (data.Alert != null)
                {
                    var level = data.Alert.Data.Where(a => a.PropertyName == "LastFuelLevel").FirstOrDefault();
                    if (level != null)
                    {
                        decimal.TryParse(level.Value, out lastValue);
                    }
                }

                Guid tankId = data.TankId;
                VirtualDevices.VirtualTank[] tanks = this.controller.GetTanks();
                VirtualDevices.VirtualTank tank = tanks.Where(t => t.TankId == tankId).FirstOrDefault();
                if(tank == null)
                    return;

                siv.TankId = tank.TankId;
                siv.FuelTypeId = tank.FuelTypeId;
                siv.StartPosition = FormStartPosition.CenterScreen;
                
                DialogResult res = siv.ShowDialog(this);
                if (res == DialogResult.Cancel)
                    return;

                decimal allowedVolume = tank.GetTankVolume(tank.MaxHeight) - tank.CurrentVolume;
                if (siv.InvoicedVolume > allowedVolume)
                {
                    decimal diff = siv.InvoicedVolume - allowedVolume;
                    DialogResult res2 = Telerik.WinControls.RadMessageBox.Show(string.Format("Ο Ογκος που θέλετε να παραλάβετε δεν χωράει στην επιλεγμένη δεξαμενή.\r\nΠερισσεύουν {0:N2} Λίτρα", diff), "ΠΡΟΣΟΧΗ ΑΔΥΝΑΤΗ ΠΑΡΑΛΑΒΗ", MessageBoxButtons.YesNo, Telerik.WinControls.RadMessageIcon.Question);
                    if (res2 == DialogResult.No)
                        return;
                }

                tank.InvoiceLineId = siv.SelectedInvoiceLineId;
                tank.FillingStartTankLevel = lastValue >= 0 ? lastValue : this.controller.GetTank(tank).ReferenceLevel;// LastValidLevel;
                if (siv.SelectionFillingMode == ASFuelControl.Windows.UI.SelectionForms.SelectInvoiceControl.FillingModeEnum.Delivery ||
                    siv.SelectionFillingMode == ASFuelControl.Windows.UI.SelectionForms.SelectInvoiceControl.FillingModeEnum.LiterCheck)
                {
                    tank.FillingByError = true;
                    tank.IsLiterCheck = siv.SelectionFillingMode == ASFuelControl.Windows.UI.SelectionForms.SelectInvoiceControl.FillingModeEnum.LiterCheck;
                }
                else if (siv.SelectionFillingMode == ASFuelControl.Windows.UI.SelectionForms.SelectInvoiceControl.FillingModeEnum.Return)
                {
                    tank.ExtractingByError = true;
                }
                data.PopupWindow.Hide();
                if (this.popups.Contains(data))
                    popups.Remove(data);
            }
        }

        void buttonElementDecrease_Click(object sender, EventArgs e)
        {
            using (UI.SelectionForms.SelectInvoiceForm siv = new UI.SelectionForms.SelectInvoiceForm())
            {
                RadButtonElement buttonElement = (RadButtonElement)sender;
                PopupData data = buttonElement.Tag as PopupData;
                decimal lastValue = -1;
                if (data.Alert != null)
                {
                    var level = data.Alert.Data.Where(a => a.PropertyName == "LastFuelLevel").FirstOrDefault();
                    if(level != null)
                    {
                        decimal.TryParse(level.Value, out lastValue);
                    }
                }
                Guid tankId = data.TankId;
                VirtualDevices.VirtualTank[] tanks = this.controller.GetTanks();
                VirtualDevices.VirtualTank tank = tanks.Where(t => t.TankId == tankId).FirstOrDefault();
                if (tank == null)
                    return;

                siv.TankId = tank.TankId;
                siv.FuelTypeId = tank.FuelTypeId;
                siv.StartPosition = FormStartPosition.CenterScreen;
                DialogResult res = siv.ShowDialog(this);
                if (res == DialogResult.Cancel)
                    return;

                decimal allowedVolume = tank.CurrentVolume;
                if (siv.InvoicedVolume > allowedVolume)
                {
                    decimal diff = siv.InvoicedVolume - allowedVolume;
                    DialogResult res2 = Telerik.WinControls.RadMessageBox.Show(string.Format("Ο Ογκος που θέλετε να εξάγετε δεν υπάρχει στην επιλεγμένη δεξαμενή.\r\nΧρειάζονται ακόμα {0:N2} Λίτρα", diff), "ΠΡΟΣΟΧΗ ΑΔΥΝΑΤΗ ΕΞΑΓΩΓΗ", MessageBoxButtons.YesNo, Telerik.WinControls.RadMessageIcon.Question);
                    if (res2 == DialogResult.No)
                        return;
                }

                tank.InvoiceLineId = siv.SelectedInvoiceLineId;
                tank.FillingStartTankLevel = lastValue >= 0 ? lastValue : this.controller.GetTank(tank).ReferenceLevel;
                if (siv.SelectionFillingMode == ASFuelControl.Windows.UI.SelectionForms.SelectInvoiceControl.FillingModeEnum.Delivery ||
                    siv.SelectionFillingMode == ASFuelControl.Windows.UI.SelectionForms.SelectInvoiceControl.FillingModeEnum.LiterCheck)
                {
                    tank.FillingByError = true;
                    tank.IsLiterCheck = siv.SelectionFillingMode == ASFuelControl.Windows.UI.SelectionForms.SelectInvoiceControl.FillingModeEnum.LiterCheck;
                }
                else if (siv.SelectionFillingMode == ASFuelControl.Windows.UI.SelectionForms.SelectInvoiceControl.FillingModeEnum.Return)
                {
                    tank.ExtractingByError = true;
                }
                data.PopupWindow.Hide();
                if (this.popups.Contains(data))
                    popups.Remove(data);
            }
        }

        //private void CreateTankFilling(VirtualDevices.VirtualTank tank)
        //{
        //    Common.TankValues startValues = new Common.TankValues();
        //    Common.TankValues endValues = new Common.TankValues();



        //    Data.Tank dbTank = this.controller.GetTank(tank);
        //    Data.TankFilling tf = dbTank.CreateTankFilling(tank.InvoiceLineId, startValues, endValues, DateTime.Now);
        //    Data.DatabaseModel db = Data.DatabaseModel.GetContext(dbTank) as Data.DatabaseModel;
        //    db.Add(tf);
        //    db.SaveChanges();
        //    tank.LastFuelHeight = dbTank.GetLastValidLevel();//vtank.FillingStartTankLevel;//;
        //    //vtank.FillingStartTankLevel = 0;

        //    tank.LastTemperature = dbTank.GetLastValidTemperatur();
        //    tank.LastWaterHeight = dbTank.GetLastValidWaterLevel();
        //    //if (this.TankFillingAvaliableEvent != null)
        //    //{
        //    //    VirtualTankFillingInfo info = new VirtualTankFillingInfo();
        //    //    info.VolumeInvoiced = tf.VolumeNormalized;
        //    //    info.VolumeReal = tf.VolumeRealNormalized;
        //    //    info.Difference = tf.VolumeNormalized - tf.VolumeRealNormalized;
        //    //    info.DeviceDescription = string.Format("Παραλαβή Δεξαμενής {0}", tank.TankNumber);
        //    //    info.MessageText = string.Format("Όγκος Παραστατικού: {0:N2} lt Όγκος Μέτρησης: {1:N2} lt", info.VolumeInvoiced, info.VolumeReal);
        //    //    info.AlarmTime = DateTime.Now;
        //    //    this.TankFillingAvaliableEvent(this, new TankFillingAvaliableArgs() { TankFillingInfo = info });
        //    //}
        //}

        private void ShowSendSuccessInfo(object sendObject)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new MethodInvoker
                    (
                        delegate
                        {
                            ShowSendSuccessInfo(sendObject);
                        }
                    ));
            }
            try
            {
                string desc = "";
                switch (sendObject.GetType().Name)
                {
                    case "SystemEvent":
                        Data.SystemEvent se = sendObject as Data.SystemEvent;
                        desc = "Συναγερμός : " + se.Message + " " + se.DeviceDescription + " " + se.EventDate.ToString("dd/MM/yyyy HH:mm");
                        break;
                    case "TankFilling":
                        Data.TankFilling tf = sendObject as Data.TankFilling;
                        desc = "Παραλαβή : " + tf.Tank.Description + " " + tf.TransactionTimeEnd.ToString("dd/MM/yyyy HH:mm");
                        break;
                    case "SalesTransaction":
                        Data.SalesTransaction st = sendObject as Data.SalesTransaction;
                        desc = "Πώληση : " + st.Nozzle.Description + " " + st.TransactionTimeStamp.ToString("dd/MM/yyyy HH:mm");
                        break;
                    case "FuelTypePrice":
                        Data.FuelTypePrice ftp = sendObject as Data.FuelTypePrice;
                        desc = "Αλλαγή Τιμής : " + ftp.FuelType.Name + " " + ftp.ChangeDate.ToString("dd/MM/yyyy HH:mm");
                        break;
                    case "ChangePriceClass":
                        Communication.ChangePriceClass cpc = sendObject as Communication.ChangePriceClass;
                        desc = "Αλλαγή Τιμής : " + cpc.FuelType.ToString() + " " + cpc.Price.ToString("N3") + " " + cpc.ChangeTime.ToString("dd/MM/yyyy HH:mm:ss");
                        break;
                    case "IncomeRecieptClass":
                        Communication.IncomeRecieptClass irc = sendObject as Communication.IncomeRecieptClass;
                        desc = "Πώληση : " + irc.FuelType.ToString() + " " + irc.TotalValue.ToString("N2") + " " + irc.PublishDateTime.ToString("dd/MM/yyyy HH:mm:ss:fff") + " " + irc.PumpSerialNumber;
                        break;
                    case "BalanceClass":
                        Communication.BalanceClass bc = sendObject as Communication.BalanceClass;
                        desc = "Ισοζύγιο : " + bc.TimeStart.ToString("dd/MM/yyyy HH:mm") + " - " + bc.TimeEnd.ToString("dd/MM/yyyy HH:mm");
                        break;
                    case "Balance":
                        Data.Balance bc1 = sendObject as Data.Balance;
                        desc = "Ισοζύγιο : " + bc1.StartDate.ToString("dd/MM/yyyy HH:mm") + " - " + bc1.EndDate.ToString("dd/MM/yyyy HH:mm");
                        break;
                }

                this.labelSendTitle.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                this.labelSendMessage.Text = desc;
                this.panelSendMessagesInner.BackColor = Color.Green;
                this.panelSendMessages.Show();
            }
            catch
            {
            }
        }

        private void HideSendSuccessInfo()
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new MethodInvoker
                    (
                        delegate
                        {
                            HideSendSuccessInfo();
                        }
                    ));
            }
            try
            {
                this.labelSendTitle.Text = "";
                this.labelSendMessage.Text = "";
                this.panelSendMessages.Hide();
            }
            catch
            {
            }
        }

        private void ShowSendFailedInfo(object sendObject)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new MethodInvoker
                    (
                        delegate
                        {
                            ShowSendFailedInfo(sendObject);
                        }
                    ));
            }
            try
            {
                

                //RadDesktopAlert alert = new RadDesktopAlert();
                //alert.ShowOptionsButton = false;
                //alert.ShowPinButton = false;
                //alert.ScreenPosition = AlertScreenPosition.BottomLeft;
                //alert.FixedSize = new Size(300, 150);
                //alert.AutoClose = false;
                string desc = "";
                string title = "";
                switch (sendObject.GetType().Name)
                {
                    case "SystemEvent":
                        Data.SystemEvent se = sendObject as Data.SystemEvent;
                        desc = "Συναγερμός : " + se.Message + " " + se.DeviceDescription + " " + se.EventDate.ToString("dd/MM/yyyy HH:mm");
                        break;
                    case "TankFilling":
                        Data.TankFilling tf = sendObject as Data.TankFilling;
                        desc = "Παραλαβή : " + tf.Tank.Description + " " + tf.TransactionTimeEnd.ToString("dd/MM/yyyy HH:mm");
                        break;
                    case "SalesTransaction":
                        Data.SalesTransaction st = sendObject as Data.SalesTransaction;
                        desc = "Πώληση : " + st.Nozzle.Description + " " + st.TransactionTimeStamp.ToString("dd/MM/yyyy HH:mm");
                        break;
                    case "FuelTypePrice":
                        Data.FuelTypePrice ftp = sendObject as Data.FuelTypePrice;
                        desc = "Αλλαγή Τιμής : " + ftp.FuelType.Name + " " + ftp.ChangeDate.ToString("dd/MM/yyyy HH:mm");
                        break;
                    case "ChangePriceClass":
                        Communication.ChangePriceClass cpc = sendObject as Communication.ChangePriceClass;
                        desc = "Αλλαγή Τιμής : " + cpc.FuelType.ToString() + " " + cpc.Price.ToString("N3") + " " + cpc.ChangeTime.ToString("dd/MM/yyyy HH:mm:ss");
                        break;
                    case "IncomeRecieptClass":
                        Communication.IncomeRecieptClass irc = sendObject as Communication.IncomeRecieptClass;
                        desc = "Πώληση : " + irc.FuelType.ToString() + " " + irc.TotalValue.ToString("N2") + " " + irc.PublishDateTime.ToString("dd/MM/yyyy HH:mm:ss:fff") + " " + irc.PumpSerialNumber;
                        break;
                    case "BalanceClass":
                        Communication.BalanceClass bc = sendObject as Communication.BalanceClass;
                        desc = "Ισοζύγιο : " + bc.TimeStart.ToString("dd/MM/yyyy HH:mm") + " - " + bc.TimeEnd.ToString("dd/MM/yyyy HH:mm");
                        break;
                    case "Balance":
                        Data.Balance bc1 = sendObject as Data.Balance;
                        desc = "Ισοζύγιο : " + bc1.StartDate.ToString("dd/MM/yyyy HH:mm") + " - " + bc1.EndDate.ToString("dd/MM/yyyy HH:mm");
                        break;
                }

                this.labelSendTitle.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                this.labelSendMessage.Text = desc;
                this.panelSendMessagesInner.BackColor = Color.Red;
                this.panelSendMessages.Show();

                //alert.CaptionText = string.Format("{0:dd/MM/yyyy HH:mm}, Αποτυχία Αποστολής", DateTime.Now);
                //alert.ContentText = desc;
                //alert.ContentImage = Properties.Resources.error32;
                //alert.Show();
            }
            catch
            {
            }
        }

        private void ShowTankFillingInfo(VirtualDevices.VirtualTankFillingInfo info)
        {
            try
            {
                RadDesktopAlert alert = new RadDesktopAlert();
                alert.ShowOptionsButton = false;
                alert.ShowPinButton = false;
                alert.FixedSize = new Size(300, 150);
                alert.AutoClose = false;
                alert.CaptionText = info.DeviceDescription + "\r\n(" + info.AlarmTime.ToString("dd/MM/yyyy HH:mm" + ")");
                decimal diff = Math.Abs(info.VolumeReal - info.VolumeInvoiced);
                decimal errorPercentage = 100 * (diff / info.VolumeInvoiced);
                alert.ContentText = string.Format("Όγκος Παραστατικού: {0:N2} lt\r\nΌγκος Μέτρησης: {1:N2} lt\r\nΔιαφορά: {2:N2} ({3:N2}%)", info.VolumeInvoiced, info.VolumeReal, info.VolumeReal - info.VolumeInvoiced, errorPercentage);

                if (errorPercentage <= 1)
                {
                    alert.ContentImage = Properties.Resources.Ok;
                }
                else
                {
                    alert.ContentImage = Properties.Resources.Warning;
                }
                alert.Show();
            }
            catch
            {
            }
        }

        private List<ResolveAlarmData> GetActiveAlarms()
        {
            List<ResolveAlarmData> alarms = new List<ResolveAlarmData>();

            foreach (UI.Controls.AlarmControl ac in this.panel5.Controls)
            {
                ResolveAlarmData data = new ResolveAlarmData();
                data.AlamId = ac.CurrentAlarm.DatabaseEntityId;
                alarms.Add(data);
            }

            return alarms;
        }

        /// <summary>
        /// Event that initiates an creation of a sale.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void control_QueryCreateSaleEvent(object sender, EventArgs e)
        {
            VirtualDevices.VirtualNozzleAlarm alarm = sender as VirtualDevices.VirtualNozzleAlarm;
            VirtualDevices.VirtualAlarmData dataNow =  alarm.Data.Where(d => d.PropertyName == "TotalVolumeCounter").FirstOrDefault();
            VirtualDevices.VirtualAlarmData dataPrev = alarm.Data.Where(d => d.PropertyName == "LastVolumeCounter").FirstOrDefault();
            if (!(dataNow == null || dataPrev == null))
            {
                decimal nowVol = decimal.Parse(dataNow.Value);
                decimal prevVol = decimal.Parse(dataPrev.Value);
                this.controller.CreateSale(alarm.DeviceId, prevVol, nowVol);
            }
        }

        /// <summary>
        /// Event for refreshing all controles for tanks and fuelpoints after restarting the main controller thread.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void controller_QueryDeviceRefresh(object sender, EventArgs e)
        {
            if (this.dispenserGrid == null)
            {
                this.dispenserGrid = new UI.Catalogs.DispenserGrid();
                this.mainPanel.Controls.Add(this.dispenserGrid);
                this.dispenserGrid.Dock = DockStyle.Fill;
            }
            this.dispenserGrid.LoadGrid(this.controller.GetDispensers());
            this.dispenserGrid.LoadTanks(this.controller.GetTanks());
            this.dispenserGrid.RearangeControls2(true);
            if (this.tankGrid == null)
            {
                this.tankGrid = new UI.Catalogs.TankGrid();
                this.mainPanel.Controls.Add(this.tankGrid);

                this.tankGrid.Dock = DockStyle.Fill;
            }
            this.tankGrid.LoadGrid(this.controller.GetTanks());
            this.tankGrid.RearangeControls2(true);
        }

        ///// <summary>
        ///// Event for handling the resolving of an alert.
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        //void control_QueryResolveAlert(object sender, QueryResolveAlarmArgs e)
        //{
        //    lock (this.alertsToResolve)
        //    {
        //        if(alertsToResolve.Where(a=>a.AlamId == e.Alarms[0].AlamId).FirstOrDefault() == null)
        //            this.alertsToResolve.Add(e.Alarms[0]);
        //        UI.Controls.AlarmControl alarmControl = sender as UI.Controls.AlarmControl;
        //        RemoveAlertControl(alarmControl);
        //        //this.panel5.Controls.Remove(alarmControl);
        //    }
        //}

        /////// <summary>
        /////// Event for handling the raising of an Alert
        /////// </summary>
        /////// <param name="sender"></param>
        /////// <param name="e"></param>
        ////void controller_AlarmRaised(object sender, WorkFlow.AlarmRaisedEventArgs e)
        ////{
        ////    if (!this.IsHandleCreated)
        ////        return;
        ////    this.Invoke(new AddAlarmControlDelegate(this.AddAlarmControl), new object[] {e.Alarm });
        ////}

        /////// <summary>
        /////// Event fired when the controller detects an Alert resolve
        /////// </summary>
        /////// <param name="sender"></param>
        /////// <param name="e"></param>
        ////void controller_QueryResolveAlarm(object sender, QueryResolveAlarmArgs e)
        ////{
        ////    ResolveAlarmData[] alarms = null;
        ////    lock (this.alertsToResolve)
        ////    {
        ////        alarms = this.alertsToResolve.ToArray();
        ////    }
        ////    e.Alarms = alarms;
        ////    lock (this.alertsToResolve)
        ////    {
        ////        this.alertsToResolve.Clear();
        ////    }
        ////}

        ///// <summary>
        ///// Event fired when the controller detects an Alert
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        //void controller_AlarmResolved(object sender, AlarmResolvedArgs e)
        //{
        //    foreach (Windows.UI.Controls.AlarmControl alarmControl in this.panel5.Controls)
        //    {
        //        if(alarmControl.CurrentAlarm.DatabaseEntityId != e.AlarmId)
        //            continue;
        //        this.panel5.Controls.Remove(alarmControl);
        //        alarmControl.Dispose();
        //        return;
        //    }
        //}

        /// <summary>
        /// Event for initiating an timer for the waiting time after a tank filling is executed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void controller_QueryStartTimer(object sender, TimerStartEventArgs e)
        {
            if (this.tankGrid == null)
                return;
            this.tankGrid.AddTimer(e.WaitingTime);
        }

        /// <summary>
        /// Event fired after an controller of tanks or fuelpoints connected successfully.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void controller_ControllerConnectionSuccess(object sender, EventArgs e)
        {
            List<ConntrollerDescription> failedControllers = sender as List<ConntrollerDescription>;
            if(failedControllers == null)
                return;
            if (!this.IsHandleCreated || this.IsDisposed)
                return;
            if (failedControllers.Count == 0)
            {
                this.Invoke(new ShowHidePanelDelegate(this.ShowHidePanel), new object[] { this.controllerPanel, false });
            }
            else
            {
                this.Invoke(new ShowHidePanelDelegate(this.ShowHidePanel), new object[] { this.controllerPanel, true });
                string text = "";
                foreach (Threads.ConntrollerDescription cdesc in failedControllers)
                {
                    text = text + cdesc.Name + "\r\n";
                }
                this.Invoke(new SetTextDelegate(this.SetText), new object[] { this.controllerLabel, text, failedControllers.Count });
            }
        }

        /// <summary>
        /// Event fired after an controller of tanks or fuelpoints connection failed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void controller_ConntrollerConnectionFailed(object sender, EventArgs e)
        {
            try
            {
                List<ConntrollerDescription> failedControllers = sender as List<ConntrollerDescription>;
                if (failedControllers == null)
                    return;
                if (!this.IsHandleCreated || this.IsDisposed)
                    return;
                if (failedControllers.Count == 0)
                    this.Invoke(new ShowHidePanelDelegate(this.ShowHidePanel), new object[] { this.controllerPanel, false });
                else
                {
                    this.Invoke(new ShowHidePanelDelegate(this.ShowHidePanel), new object[] { this.controllerPanel, true });
                    string text = "";
                    foreach (Threads.ConntrollerDescription cdesc in failedControllers)
                    {
                        text = text + string.Format("Δεν μπόρεσε να γίνει σύνδεση του Ελεγκτη \"{0}\" στην θύρα {1}", cdesc.Name, cdesc.ConnectionPort) + "\r\n";
                    }
                    this.Invoke(new SetTextDelegate(this.SetText), new object[] { this.controllerLabel, text, failedControllers.Count });
                }
            }
            catch
            {
            }
        }

        void controller_TankFillingAvaliableEvent(object sender, TankFillingAvaliableArgs e)
        {
            this.Invoke(new ShowFillingDataDelegate(this.ShowTankFillingInfo), new object[] { e.TankFillingInfo });
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //this.dispenser1.ScaleControl((float)0.75);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //this.dispenser1.ScaleControl((float)1 / (float)0.75);
        }


        /// <summary>
        /// Load tank grid button clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void radButton3_Click(object sender, EventArgs e)
        {
            if (!this.controller.StartedOnce)
                return;
            if (this.tankGrid == null)
            {
                this.tankGrid = new UI.Catalogs.TankGrid();
                this.tankGrid.LoadGrid(this.controller.GetTanks());
                this.mainPanel.Controls.Add(this.tankGrid);

                this.tankGrid.Dock = DockStyle.Fill;

                //VirtualDevices.VirtualTankFillingInfo info = new VirtualDevices.VirtualTankFillingInfo();
                //info.VolumeInvoiced = 1000;
                //info.VolumeReal = 1010;
                //info.Difference = 10;
                //info.DeviceDescription = string.Format("Παραλαβή Δεξαμενής {0}", 3);
                //info.MessageText = string.Format("Όγκος Παραστατικού: {0:N2} lt\r\nΌγκος Μέτρησης: {1:N2} lt", info.VolumeInvoiced, info.VolumeReal);
                //info.AlarmTime = DateTime.Now;
                //this.ShowTankFillingInfo(info);
            }
            if (this.dispenserGrid != null)
                this.dispenserGrid.Hide();
            if (this.settingsGrid != null)
                this.settingsGrid.Hide();
            if(this.traderCatalog != null)
                this.traderCatalog.Hide();
            if (this.invoceCatalog != null)
                this.invoceCatalog.Hide();
            //if (this.statistics != null)
            //    this.statistics.Hide();
            this.alertGrid.Hide();
            this.tankGrid.Show();
        }

        /// <summary>
        /// Load fuelpump grid button clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void radButton2_Click(object sender, EventArgs e)
        {
            if (!this.controller.StartedOnce)
                return;
            if (this.dispenserGrid == null)
            {
                this.dispenserGrid = new UI.Catalogs.DispenserGrid();
                this.dispenserGrid.LoadGrid(this.controller.GetDispensers());
                this.dispenserGrid.LoadTanks(this.controller.GetTanks());
                try
                {
                    this.mainPanel.Controls.Add(this.dispenserGrid);
                }
                catch
                {
                }
                this.dispenserGrid.Dock = DockStyle.Fill;
            }
            if (this.tankGrid != null)
                this.tankGrid.Hide();
            if (this.settingsGrid != null)
                this.settingsGrid.Hide();
            if (this.traderCatalog != null)
                this.traderCatalog.Hide();
            if (this.invoceCatalog != null)
                this.invoceCatalog.Hide();
            this.alertGrid.Hide();
            this.dispenserGrid.Show();
        }

        private void ToggleStateChanged(object sender, Telerik.WinControls.UI.StateChangedEventArgs args)
        {
            RadToggleButton btn = sender as RadToggleButton;
            if (!btn.IsChecked)
                return;
            foreach (Control ctrl in this.panel1.Controls)
            {
                RadToggleButton b = ctrl as RadToggleButton;
                if (b == null)
                    continue;
                if (b.Equals(btn))
                    continue;
                b.IsChecked = false;
            }
        }

        private void radToggleButton1_Click(object sender, EventArgs e)
        {
            Common.Enumerators.ApplicationUserLevelEnum ul = this.VerifyUser();
            if (!(ul == Common.Enumerators.ApplicationUserLevelEnum.SuperUser || ul == Common.Enumerators.ApplicationUserLevelEnum.Administrator))
                return;
            if (ul == Common.Enumerators.ApplicationUserLevelEnum.Administrator)
            {
                Program.AdminConnected = true;
                this.lockPanel.Visible = false;
            }
            this.alertGrid.Hide();
            if (this.settingsGrid == null)
            {
                this.settingsGrid = new UI.Catalogs.SettingsGrid();
                this.settingsGrid.PriceChanged += new EventHandler(settingsGrid_PriceChanged);
                //this.settingsGrid.LoadGrid(this.controller.GetDispensers());
                this.mainPanel.Controls.Add(this.settingsGrid);

                this.settingsGrid.Dock = DockStyle.Fill;
            }
            if (this.tankGrid != null)
                this.tankGrid.Hide();
            if (this.dispenserGrid != null)
                this.dispenserGrid.Hide();
            if (this.traderCatalog != null)
                this.traderCatalog.Hide();
            if (this.invoceCatalog != null)
                this.invoceCatalog.Hide();
            this.settingsGrid.Show();
            
        }

        /// <summary>
        /// Event for applying new prices to fuelpumps.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void settingsGrid_PriceChanged(object sender, EventArgs e)
        {

            this.controller.ApplyPrices();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            DialogResult res = RadMessageBox.Show("Είστε σίγουρος/η ότι θέλετε να κλείσει το ASFuelControl;", "Κλείσιμο Εφαρμογής...", MessageBoxButtons.YesNo, RadMessageIcon.Question);
            if (res == System.Windows.Forms.DialogResult.No)
            {
                e.Cancel = true;
                return;
            }
            if (this.controller.IsRunning)
            {
                if (Program.CurrentUserLevel == Common.Enumerators.ApplicationUserLevelEnum.Administrator)
                    this.controller.StopThreads(true);
                else
                    this.controller.StopThreads(false);
            }
        }

        private void btnChangeUser_Click(object sender, EventArgs e)
        {
            UI.Forms.LoginForm lf = new UI.Forms.LoginForm();
            lf.InternalUse = true;
            lf.ShowDialog();
            this.label2.Text = Program.CurrentUserName;
            this.ApplyUser();
        }

        private void btnStats_Click(object sender, EventArgs e)
        {
            Common.Enumerators.ApplicationUserLevelEnum ul = this.VerifyUser();
            if (!(ul == Common.Enumerators.ApplicationUserLevelEnum.SuperUser || ul == Common.Enumerators.ApplicationUserLevelEnum.Administrator))
                return;
            if (ul == Common.Enumerators.ApplicationUserLevelEnum.Administrator)
                Program.AdminConnected = true;
            if (this.tankGrid != null)
                this.tankGrid.Hide();
            if (this.settingsGrid != null)
                this.settingsGrid.Hide();
            if (this.dispenserGrid != null)
                this.dispenserGrid.Hide();
            if (this.traderCatalog != null)
                this.traderCatalog.Hide();
            if (this.invoceCatalog != null)
                this.invoceCatalog.Hide();
            this.alertGrid.Hide();
        }

        private void btnChangeShift_Click(object sender, EventArgs e)
        {
            if(this.otpEnabled)
            {
                RadMessageBox.Show("Ειναι ενεργός ο Αυτόματος Πωλητής.\r\nΠαρακαλώ απενεργοποιήστε τον Πωλήτή", "Σφάλμα επιλογής...", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
                return;
            }
            using (UI.Forms.ShiftForm shForm = new UI.Forms.ShiftForm())
            {
                shForm.ShowDialog();
            }
           
           using(UI.Forms.ShiftPopUpForm banner = new UI.Forms.ShiftPopUpForm())
           {
               //banner.TopMost = true;
               banner.StartPosition = FormStartPosition.CenterScreen;

               if(Program.CurrentShiftId == Guid.Empty)
               {
                   this.btnChangeShift.Text = "Εναρξη Βάρδιας";
                   this.btnChangeShift.Image = Properties.Resources.UserShift;
                   //banner.ShowDialog();

               }
               else
               {
                   this.btnChangeShift.Text = "Λήξη Βάρδιας";
                   this.btnChangeShift.Image = Properties.Resources.UserShiftEnd;

                   banner.TopMost = true;
                   if(banner.Visible)
                       banner.Close();

               }
           }
           this.label2.Text = Program.CurrentUserName;
           this.ApplyUser();
        }

        private Common.Enumerators.ApplicationUserLevelEnum VerifyUser()
        {
            if (Program.AdminConnected)
                return Common.Enumerators.ApplicationUserLevelEnum.Administrator;

            using (UI.Forms.UserVerificationForm uvf = new UI.Forms.UserVerificationForm())
            {
                uvf.ShowDialog();
                Data.DatabaseModel.UserLoggedIn = uvf.CurrentUserId;
                Data.DatabaseModel.UserLoggedInLevel = (int)uvf.CurrentUserLevel;
                return uvf.CurrentUserLevel;
            }
        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            Process.Start("http://www.pratiriouxos.gr");
        }

        private void panel4_Click(object sender, EventArgs e)
        {
            UI.Forms.AboutForm aform = new UI.Forms.AboutForm();
            aform.ShowDialog();
        }

        private void adminPanel_Click(object sender, EventArgs e)
        {
            Program.AdminConnected = false;
            Data.DatabaseModel.UserLoggedIn = Guid.Empty;
            Data.DatabaseModel.UserLoggedInLevel = -1;
            this.lockPanel.Visible = Data.Implementation.OptionHandler.Instance.GetIntOption("ApplicationLocked", 777) != 777;
            if (this.settingsGrid != null && this.settingsGrid.Visible)
            {
                
                this.radButton2_Click(this.btnPumps, new EventArgs());
            }
        }

        void mainPanel_ControlRemoved(object sender, ControlEventArgs e)
        {
            if (e.Control.GetType() == typeof(UI.Catalogs.DispenserGrid))
            {
                this.dispenserGrid = null;
            }
            else if (e.Control.GetType() == typeof(UI.Catalogs.TankGrid))
            {
                this.tankGrid = null;
            }
        }

        internal class PopupData
        {
            public Guid TankId { set; get; }
            public Guid AlertId { set; get; }
            public RadDesktopAlert PopupWindow { set; get; }
            public VirtualDevices.VirtualBaseAlarm Alert { set; get; }
        }

        private void ShowSendHistory_Click(object sender, EventArgs e)
        {
            UI.Forms.SendHistoryForm shf = new UI.Forms.SendHistoryForm();
            shf.Show(this);
            shf.FormClosed += new FormClosedEventHandler(shf_FormClosed);
        }

        void shf_FormClosed(object sender, FormClosedEventArgs e)
        {
            UI.Forms.SendHistoryForm shf = sender as UI.Forms.SendHistoryForm;
            if (shf == null)
                return;
            shf.Dispose();
        }

        private void splitPanel1_Resize(object sender, EventArgs e)
        {
            
        }

        private void btnAlerts_Click(object sender, EventArgs e)
        {
            if (this.tankGrid != null)
                this.tankGrid.Hide();
            if (this.settingsGrid != null)
                this.settingsGrid.Hide();
            if (this.dispenserGrid != null)
                this.dispenserGrid.Hide(); 
            this.alertGrid.Show();
            this.alertGrid.ResumeAlertLayout();
        }

        private void radButton2_Click_1(object sender, EventArgs e)
        {
            //bool otpEnabled = Data.Implementation.OptionHandler.Instance.GetBoolOption("[OTPEnabled]", false);
            OTPConsoleController[] otpControllers = this.controller.GetOtpControllers();
            
            foreach(OTPConsoleController otpController in otpControllers)
            {
                //bool completed = false;
                if (!otpEnabled)
                    otpController.EnableOTP();
                else
                    otpController.DisableOTP();
            }
        }

        private void radButton3_Click_1(object sender, EventArgs e)
        {
            
            if (this.tankGrid != null)
                this.tankGrid.Hide();
            if (this.settingsGrid != null)
                this.settingsGrid.Hide();
            if (this.dispenserGrid != null)
                this.dispenserGrid.Hide();
            if (this.alertGrid != null)
                this.alertGrid.Hide();
            if (this.invoceCatalog != null)
                this.invoceCatalog.Hide();
            if (traderCatalog == null)
            {
                this.traderCatalog = new UI.Catalogs.TraderCatalog();
                this.mainPanel.Controls.Add(this.traderCatalog);
                this.traderCatalog.Dock = DockStyle.Fill;
            }
            this.traderCatalog.Show();
            this.traderCatalog.RefreshData();
        }

        private void radButton4_Click(object sender, EventArgs e)
        {
            if (this.tankGrid != null)
                this.tankGrid.Hide();
            if (this.settingsGrid != null)
                this.settingsGrid.Hide();
            if (this.dispenserGrid != null)
                this.dispenserGrid.Hide();
            if (this.alertGrid != null)
                this.alertGrid.Hide();
            if(this.traderCatalog != null)
                this.traderCatalog.Hide();
            if (this.invoceCatalog == null)
            {
                this.invoceCatalog = new UI.Catalogs.InvoiceCatalog();
                this.mainPanel.Controls.Add(this.invoceCatalog);
                this.invoceCatalog.Dock = DockStyle.Fill;
            }
            this.invoceCatalog.Show();
            this.invoceCatalog.RefreshData();
        }
    }
}