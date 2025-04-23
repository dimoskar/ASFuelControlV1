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
        

        private delegate void AddAlarmControlDelegate(VirtualDevices.VirtualBaseAlarm alarm);
        private delegate void ShowHidePanelDelegate(Panel p, bool visible);
        private delegate void SetTextDelegate(Label p, string text, int c);

        public ThreadController ThreadControllerInstance
        {
            get { return this.controller; }
        }

        public MainForm()
        {
            ThemeResolutionService.ApplicationThemeName = "Windows8";//"TelerikMetroBlue";

            InitializeComponent();

            this.Load += new EventHandler(MainForm_Load);
            this.Size = new System.Drawing.Size(1280, 768);
            
        }

        void MainForm_Load(object sender, EventArgs e)
        {
            UI.Forms.LoadWaitingForm.ShowForm();

            controller.StartThreads();
            controller.QueryDeviceRefresh += new EventHandler(controller_QueryDeviceRefresh);
            controller.AlarmRaised += new EventHandler<WorkFlow.AlarmRaisedEventArgs>(controller_AlarmRaised);
            controller.QueryResolveAlarm += new EventHandler<QueryResolveAlarmArgs>(controller_QueryResolveAlarm);
            controller.AlarmResolved += new EventHandler<AlarmResolvedArgs>(controller_AlarmResolved);
            controller.QueryStartTimer += new EventHandler<TimerStartEventArgs>(controller_QueryStartTimer);
            controller.ConntrollerConnectionFailed += new EventHandler(controller_ConntrollerConnectionFailed);
            controller.ControllerConnectionSuccess += new EventHandler(controller_ControllerConnectionSuccess);
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
            control.Height = 80;
            if (alarm.GetType() == typeof(VirtualDevices.VirtualTankAlarm))
            {
                control.DeviceType = "Δεξαμενή";
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
            control.QueryResolveAlert += new EventHandler<QueryResolveAlarmArgs>(control_QueryResolveAlert);
        }

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
            this.dispenserGrid.RearangeControls(true);
            if (this.tankGrid == null)
            {
                this.tankGrid = new UI.Catalogs.TankGrid();
                this.mainPanel.Controls.Add(this.tankGrid);

                this.tankGrid.Dock = DockStyle.Fill;
            }
            this.tankGrid.LoadGrid(this.controller.GetTanks());
            this.tankGrid.RearangeControls(true);
        }

        void control_QueryResolveAlert(object sender, QueryResolveAlarmArgs e)
        {
            lock (this.alertsToResolve)
            {
                if(alertsToResolve.Where(a=>a.AlamId == e.Alarms[0].AlamId).FirstOrDefault() == null)
                    this.alertsToResolve.Add(e.Alarms[0]);
                UI.Controls.AlarmControl alarmControl = sender as UI.Controls.AlarmControl;
                this.panel5.Controls.Remove(alarmControl);
            }
        }

        void controller_AlarmRaised(object sender, WorkFlow.AlarmRaisedEventArgs e)
        {
            if (!this.IsHandleCreated)
                return;
            this.Invoke(new AddAlarmControlDelegate(this.AddAlarmControl), new object[] {e.Alarm });
        }

        void controller_QueryResolveAlarm(object sender, QueryResolveAlarmArgs e)
        {
            ResolveAlarmData[] alarms = null;
            lock (this.alertsToResolve)
            {
                alarms = this.alertsToResolve.ToArray();
            }
            e.Alarms = alarms;
            lock (this.alertsToResolve)
            {
                this.alertsToResolve.Clear();
            }
        }

        void controller_AlarmResolved(object sender, AlarmResolvedArgs e)
        {
            foreach (Windows.UI.Controls.AlarmControl alarmControl in this.panel5.Controls)
            {
                if(alarmControl.CurrentAlarm.DatabaseEntityId != e.AlarmId)
                    continue;
                this.panel5.Controls.Remove(alarmControl);
                alarmControl.Dispose();
                return;
            }
        }

        void controller_QueryStartTimer(object sender, TimerStartEventArgs e)
        {
            if (this.tankGrid == null)
                return;
            this.tankGrid.AddTimer(e.WaitingTime);
        }

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

        void controller_ConntrollerConnectionFailed(object sender, EventArgs e)
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

        private void button1_Click(object sender, EventArgs e)
        {
            //this.dispenser1.ScaleControl((float)0.75);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //this.dispenser1.ScaleControl((float)1 / (float)0.75);
        }

        private void radButton3_Click(object sender, EventArgs e)
        {
            if (this.tankGrid == null)
            {
                this.tankGrid = new UI.Catalogs.TankGrid();
                this.tankGrid.LoadGrid(this.controller.GetTanks());
                this.mainPanel.Controls.Add(this.tankGrid);

                this.tankGrid.Dock = DockStyle.Fill;
            }
            if (this.dispenserGrid != null)
                this.dispenserGrid.Hide();
            if (this.settingsGrid != null)
                this.settingsGrid.Hide();
            if (this.statistics != null)
                this.statistics.Hide();
            this.tankGrid.Show();
        }

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
                this.statistics.Hide();
            this.settingsGrid.Show();
        }

        void settingsGrid_PriceChanged(object sender, EventArgs e)
        {

            this.controller.ApplyPrices();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
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
        }

        private Common.Enumerators.ApplicationUserLevelEnum VerifyUser()
        {
            if (Program.AdminConnected)
                return Common.Enumerators.ApplicationUserLevelEnum.Administrator;

            using (UI.Forms.UserVerificationForm uvf = new UI.Forms.UserVerificationForm())
            {
                uvf.ShowDialog();
                
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
        }
    }
}