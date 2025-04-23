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

namespace ASFuelControl.Windows
{
    public partial class MainForm : RadForm
    {
        ThreadController controller = new ThreadController();
        UI.Catalogs.TankGrid tankGrid = null;
        UI.Catalogs.ReportGrid statistics = null;
        UI.Catalogs.DispenserGrid dispenserGrid = null;
        UI.Catalogs.SettingsGrid settingsGrid = null;
        List<ResolveAlarmData> alertsToResolve = new List<ResolveAlarmData>();
        Timer t1 = new Timer();

        List<PopupData> popups = new List<PopupData>();

        private delegate void AddAlarmControlDelegate(VirtualDevices.VirtualBaseAlarm alarm);
        private delegate void ShowFillingDataDelegate(VirtualDevices.VirtualTankFillingInfo info);
        private delegate void ShowHidePanelDelegate(Panel p, bool visible);
        private delegate void SetTextDelegate(Label p, string text, int c);
        private delegate void RemoveControlDelegate(Control c);

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

            this.Load += new EventHandler(MainForm_Load);
            this.Size = new System.Drawing.Size(1280, 768);
            this.mainPanel.ControlRemoved += new ControlEventHandler(mainPanel_ControlRemoved);
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
            controller.CheckForDatabaseUpdate();

            Threads.AlertHandler.Instance.AlarmAdded += new EventHandler(Instance_AlarmAdded);
            Threads.AlertHandler.Instance.AlarmResolved += new EventHandler(Instance_AlarmResolved);

            t1.Interval = 500;
            t1.Start();
            t1.Tick += new EventHandler(t1_Tick);
            UI.Forms.LoadWaitingForm.ShowForm();
            
            
            controller.QueryDeviceRefresh += new EventHandler(controller_QueryDeviceRefresh);
            controller.QueryStartTimer += new EventHandler<TimerStartEventArgs>(controller_QueryStartTimer);
            controller.ConntrollerConnectionFailed += new EventHandler(controller_ConntrollerConnectionFailed);
            controller.ControllerConnectionSuccess += new EventHandler(controller_ControllerConnectionSuccess);
            controller.TankFillingAvaliableEvent += new EventHandler<TankFillingAvaliableArgs>(controller_TankFillingAvaliableEvent);
            controller.RefreshAlerts += new EventHandler(controller_RefreshAlerts);
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
        }

        /// <summary>
        /// Removes the Alert Notification control after an alert is removed
        /// </summary>
        /// <param name="c"></param>
        private void RemoveAlertControl(Control c)
        {
            UI.Controls.AlarmControl ac = c as UI.Controls.AlarmControl;
            
            this.panel5.Controls.Remove(c);
            try
            {
                if (ac != null)
                {
                    PopupData data = this.popups.Where(p => p.AlertId == ac.CurrentAlarm.DatabaseEntityId).FirstOrDefault();
                    if (data == null)
                        return;
                    data.PopupWindow.Hide();
                    data.PopupWindow.Dispose();
                    this.popups.Remove(data);
                }
            }
            catch(Exception ex)
            {
                Logging.Logger.Instance.LogToFile("RemoveAlertControl", ex);
            }
            
        }

        /// <summary>
        /// Event tha fires after an alert is resolved
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Instance_AlarmResolved(object sender, EventArgs e)
        {
            VirtualDevices.VirtualBaseAlarm alarm = sender as VirtualDevices.VirtualBaseAlarm;
            if (alarm == null)
                return;
            UI.Controls.AlarmControl control = this.panel5.Controls.OfType<UI.Controls.AlarmControl>().Where(ac => ac.CurrentAlarm.Equals(alarm)).FirstOrDefault();
            this.Invoke(new RemoveControlDelegate(this.RemoveAlertControl), new object[] { control });
        }

        /// <summary>
        /// Event tha fires after an alert is added
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Instance_AlarmAdded(object sender, EventArgs e)
        {
            VirtualDevices.VirtualBaseAlarm alarm = sender as VirtualDevices.VirtualBaseAlarm;
            if (alarm == null)
                return;
            this.Invoke(new AddAlarmControlDelegate(this.AddAlarmControl), new object[] { alarm });
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

        void t1_Tick(object sender, EventArgs e)
        {
            this.clockLabel.Text = DateTime.Now.ToString("HH:mm:ss");
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

        void controller_RefreshAlerts(object sender, EventArgs e)
        {
            List<UI.Controls.AlarmControl> toRemove = new List<UI.Controls.AlarmControl>();
            foreach (UI.Controls.AlarmControl ac in this.panel5.Controls)
            {
                VirtualDevices.VirtualTank vt = this.controller.GetTanks().Where(t => t.TankId == ac.CurrentAlarm.DeviceId).FirstOrDefault();
                if(vt != null)
                {
                    
                    if (vt.Alerts.Where(a => a.DatabaseEntityId == ac.CurrentAlarm.DatabaseEntityId).Count() > 0)
                        continue;
                    toRemove.Add(ac);
                    continue;
                }
                VirtualDevices.VirtualDispenser vd = this.controller.GetDispensers().Where(t => t.DispenserId == ac.CurrentAlarm.DeviceId).FirstOrDefault();
                if (vd != null)
                {
                    if (vd.Alerts.Where(a => a.DatabaseEntityId == ac.CurrentAlarm.DatabaseEntityId).Count() > 0)
                        continue;
                    toRemove.Add(ac);
                    continue;
                }
                VirtualDevices.VirtualNozzle vn = this.controller.GetDispensers().SelectMany(d=>d.Nozzles).Where(t => t.NozzleId == ac.CurrentAlarm.DeviceId).FirstOrDefault();
                if (vn != null)
                {
                    if (vn.Alerts.Where(a => a.DatabaseEntityId == ac.CurrentAlarm.DatabaseEntityId).Count() > 0)
                        continue;
                    toRemove.Add(ac);
                    continue;
                }
            }
            foreach (UI.Controls.AlarmControl ac in toRemove)
                this.Invoke(new RemoveControlDelegate(this.RemoveAlertControl), new object[] { ac });
        }

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

        private void AddAlarmControl(VirtualDevices.VirtualBaseAlarm alarm)
        {
            foreach (UI.Controls.AlarmControl aControl in this.panel5.Controls)
            {
                if (aControl.CurrentAlarm.DatabaseEntityId == alarm.DatabaseEntityId && aControl.CurrentAlarm.AlertType == alarm.AlertType)
                {
                    return;
                }
            }
            UI.Controls.AlarmControl control = new UI.Controls.AlarmControl();
            control.QueryCreateSaleEvent += new EventHandler(control_QueryCreateSaleEvent);
            control.Height = 60;
            if (alarm.GetType() == typeof(VirtualDevices.VirtualTankAlarm))
            {
                control.DeviceType = "Δεξαμενή";
                VirtualDevices.VirtualTankAlarm tankAlert = (VirtualDevices.VirtualTankAlarm)alarm;
                if (tankAlert.AlertType == Common.Enumerators.AlarmEnum.FuelIncrease)
                {
                    this.ShowFillingNeededInfo(tankAlert);
                }
            }
            else if (alarm.GetType() == typeof(VirtualDevices.VirtualNozzleAlarm))
            {
                control.DeviceType = "Ακροσωλήνιο";
            }
            else if (alarm.GetType() == typeof(VirtualDevices.VirtualTankFillingInfo))
            {
                control.DeviceType = "Παραλαβή";
                control.Height = 100;
            }
            control.CurrentAlarm = alarm;
            control.Dock = DockStyle.Top;
            
            this.panel5.Controls.Add(control);
            Control maxC = null;
            foreach (Control c in this.panel5.Controls)
            {
                if (maxC == null)
                    maxC = c;
                if (maxC.Location.Y < c.Location.Y)
                    maxC = c;
            }
            if (maxC.Height + maxC.Location.Y > this.panel5.Height)
            {
                this.panel5.Height = maxC.Height + maxC.Location.Y;
            }
            //control.QueryResolveAlert += new EventHandler<QueryResolveAlarmArgs>(control_QueryResolveAlert);
        }

        private void ShowFillingNeededInfo(VirtualDevices.VirtualTankAlarm tankAlert)
        {
            try
            {
                RadDesktopAlert alert = new RadDesktopAlert();
                alert.ShowOptionsButton = false;
                alert.ShowPinButton = false;
                alert.FixedSize = new Size(300, 200);
                alert.AutoClose = false;
                alert.ShowCloseButton = false;
                
                alert.CaptionText = tankAlert.DeviceDescription + "\r\n(" + tankAlert.AlarmTime.ToString("dd/MM/yyyy HH:mm" + ")");
                alert.ContentText = string.Format("Ανιχνέυτηκε αύξηση στην στάθμη της δεξαμενής.\r\nΠατήστε 'Παραλαβή' για την καταχώρηση του παραστατικού");
                RadButtonElement buttonElement = new RadButtonElement("Παραλαβή");
                buttonElement.Font = new System.Drawing.Font(this.Font.FontFamily, 12);
                buttonElement.Click += new EventHandler(buttonElement_Click);
                buttonElement.BorderThickness = new System.Windows.Forms.Padding(1);
                buttonElement.Image = new Bitmap(Properties.Resources.FillingStart, new Size(20, 20));
                buttonElement.TextImageRelation = TextImageRelation.ImageBeforeText;
                buttonElement.Size = new System.Drawing.Size(100, buttonElement.Size.Height);
                buttonElement.BackColor = Color.DarkGray;
                buttonElement.ShowBorder = true;
                PopupData data = new PopupData();
                data.TankId = tankAlert.DeviceId;
                data.PopupWindow = alert;
                data.AlertId = tankAlert.DatabaseEntityId;
                buttonElement.Tag = data;
                alert.ButtonItems.Add(buttonElement);
                popups.Add(data);
                alert.Show();
            }
            catch
            {
            }
        }

        void buttonElement_Click(object sender, EventArgs e)
        {
            using (UI.SelectionForms.SelectInvoiceForm siv = new UI.SelectionForms.SelectInvoiceForm())
            {
                RadButtonElement buttonElement = (RadButtonElement)sender;
                PopupData data = buttonElement.Tag as PopupData;

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

                if (siv.SelectionFillingMode == ASFuelControl.Windows.UI.SelectionForms.SelectInvoiceControl.FillingModeEnum.Delivery || 
                    siv.SelectionFillingMode == ASFuelControl.Windows.UI.SelectionForms.SelectInvoiceControl.FillingModeEnum.LiterCheck)
                {
                    tank.InitializeFilling = true;
                    tank.IsLiterCheck = siv.SelectionFillingMode == ASFuelControl.Windows.UI.SelectionForms.SelectInvoiceControl.FillingModeEnum.LiterCheck;
                }
                else if (siv.SelectionFillingMode == ASFuelControl.Windows.UI.SelectionForms.SelectInvoiceControl.FillingModeEnum.Return)
                    tank.InitializeExtraction = true;
                
                data.PopupWindow.Hide();
                if (this.popups.Contains(data))
                    popups.Remove(data);
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

            if (this.statistics != null)
            {
                this.mainPanel.Controls.Remove(this.statistics);
                this.statistics.Dispose();
                this.statistics = null;
            }

            //if (this.statistics != null)
            //    this.statistics.Hide();
            this.tankGrid.Show();
        }

        /// <summary>
        /// Load fuelpump grid button clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void radButton2_Click(object sender, EventArgs e)
        {
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
            if (this.statistics != null)
                this.statistics.Hide();
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
            if(ul == Common.Enumerators.ApplicationUserLevelEnum.Administrator)
                Program.AdminConnected = true;

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
            if (this.statistics != null)
            {
                this.mainPanel.Controls.Remove(this.statistics);
                this.statistics.Dispose();
                this.statistics = null;
            }
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

            this.controller.StopThreads(false);
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
            if (this.statistics == null)
            {
                this.statistics = new UI.Catalogs.ReportGrid();

                this.mainPanel.Controls.Add(this.statistics);

                this.statistics.Dock = DockStyle.Fill;
            }
            if (this.tankGrid != null)
                this.tankGrid.Hide();
            if (this.settingsGrid != null)
                this.settingsGrid.Hide();
            if (this.dispenserGrid != null)
                this.dispenserGrid.Hide();
            this.statistics.Show();
        }

        private void btnChangeShift_Click(object sender, EventArgs e)
        {
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
        }
    }
}