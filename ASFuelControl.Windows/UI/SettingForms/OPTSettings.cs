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
    public partial class OPTSettings : RadForm
    {
        Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
        private bool userView = false;
        BindingList<Data.OutdoorPaymentTerminal> terminals = new BindingList<Data.OutdoorPaymentTerminal>();

        public bool UserView
        {
            set
            {
                this.userView = value;
                this.radGroupBox1.Enabled = !this.userView;
                this.radButton6.Enabled = !this.userView;
                this.radButton5.Enabled = !this.userView;
                this.radDropDownList1.Enabled = !this.userView;
            }
            get { return this.userView; }
        }

        public OPTSettings()
        {
            InitializeComponent();
            this.radGroupBox1.Enabled = false;
            this.radDropDownList1.Enabled = false;
            this.terminals = new BindingList<Data.OutdoorPaymentTerminal>(this.db.OutdoorPaymentTerminals.OrderBy(o => o.Name).ToList());
            this.outdoorPaymentTerminalBindingSource.DataSource = this.terminals;
            this.radDropDownList1.DataSource = this.outdoorPaymentTerminalBindingSource;
            this.radDropDownList1.DisplayMember = "Name";
            this.radDropDownList1.ValueMember = "OutdoorPaymentTerminalId";

            this.radDropDownList4.DataSource = ASFuelControl.Common.Enumerators.EnumToList.EnumList<ASFuelControl.Common.Enumerators.ScheduleTypeEnum>();
            this.radDropDownList4.DisplayMember = "Description";
            this.radDropDownList4.ValueMember = "Value";

            this.radDropDownList2.DataSource = ASFuelControl.Common.Enumerators.EnumToList.EnumList<ASFuelControl.Common.Enumerators.ScheduleWeekDayEnum>();
            this.radDropDownList2.DisplayMember = "Description";
            this.radDropDownList2.ValueMember = "Value";

            this.connectionTypeDropDown.DataSource = ASFuelControl.Common.Enumerators.EnumToList.EnumList<ASFuelControl.Common.Enumerators.OPTConnectionTypeEnum>();
            this.connectionTypeDropDown.DisplayMember = "Description";
            this.connectionTypeDropDown.ValueMember = "Value";

            this.terminalTypeDropDown.DataSource = ASFuelControl.Common.Enumerators.EnumToList.EnumList<ASFuelControl.Common.Enumerators.OPTTypeEnum>();
            this.terminalTypeDropDown.DisplayMember = "Description";
            this.terminalTypeDropDown.ValueMember = "Value";

            GridViewComboBoxColumn col = this.outdoorPaymentTerminalScheduleRadGridView.Columns["ScheduleType"] as GridViewComboBoxColumn;
            col.DataSource = ASFuelControl.Common.Enumerators.EnumToList.EnumList<ASFuelControl.Common.Enumerators.ScheduleTypeEnum>();
            col.DisplayMember = "Description";
            col.ValueMember = "Value";

            GridViewComboBoxColumn col2 = this.outdoorPaymentTerminalScheduleRadGridView.Columns["DayOfWeek"] as GridViewComboBoxColumn;
            col2.DataSource = ASFuelControl.Common.Enumerators.EnumToList.EnumList<ASFuelControl.Common.Enumerators.ScheduleWeekDayEnum>();
            col2.DisplayMember = "Description";
            col2.ValueMember = "Value";
            this.panelNozzle.Location = this.panelDate.Location;
            this.panelDate.Location = this.panelDayOfWeek.Location;
            this.panelDate.Hide();

            this.radPageView1.SelectedPage = this.radPageViewPage1;

            this.db.Events.Changed += new Telerik.OpenAccess.ChangeEventHandler(Events_Changed);
            this.Disposed += OPTSettings_Disposed;
        }

        private void OPTSettings_Disposed(object sender, EventArgs e)
        {
            this.db.Dispose();
        }

        private void ShowHideScheduleTypeControls(Data.OutdoorPaymentTerminalSchedule schedule)
        {
            if (schedule.ScheduleType == (int)Common.Enumerators.ScheduleTypeEnum.Date)
            {
                this.panelDayOfWeek.Hide();
                this.panelDate.Show();
            }
            else if (schedule.ScheduleType == (int)Common.Enumerators.ScheduleTypeEnum.WayOfWeek)
            {
                this.panelDayOfWeek.Show();
                this.panelDate.Hide();
            }
            
        }

        private void AddController(Data.OutdoorPaymentTerminal opt,  Guid g)
        {
            if (opt.OutdoorPaymentTerminalControllers.Where(c => c.CommunicationControllerId == g).Count() > 0)
                return; ;
            Data.OutdoorPaymentTerminalController optController = new Data.OutdoorPaymentTerminalController();
            optController.OutdoorPaymentTerminalControllerId = Guid.NewGuid();
            this.db.Add(optController);
            optController.OutdoorPaymentTerminalId = opt.OutdoorPaymentTerminalId;
            optController.OutdoorPaymentTerminal = opt;
            opt.OutdoorPaymentTerminalControllers.Add(optController);
            optController.CommunicationControllerId = g;
        }

        private void outdoorPaymentTerminalBindingSource_PositionChanged(object sender, EventArgs e)
        {
            this.radGroupBox1.Enabled = this.outdoorPaymentTerminalBindingSource.Position >= 0;
            this.radDropDownList1.Enabled = this.outdoorPaymentTerminalBindingSource.Position >= 0;
        }

        private void outdoorPaymentTerminalScheduleBindingSource_PositionChanged(object sender, EventArgs e)
        {
            if (this.outdoorPaymentTerminalScheduleBindingSource.Position < 0)
                return;
            if (this.outdoorPaymentTerminalScheduleBindingSource.Current == null)
                return;
            Data.OutdoorPaymentTerminalSchedule schedule = this.outdoorPaymentTerminalScheduleBindingSource.Current as Data.OutdoorPaymentTerminalSchedule;
            this.ShowHideScheduleTypeControls(schedule);
        }

        private void radButton6_Click(object sender, EventArgs e)
        {
            Data.OutdoorPaymentTerminal opt = new Data.OutdoorPaymentTerminal();
            opt.OutdoorPaymentTerminalId = Guid.NewGuid();
            this.db.Add(opt);
            this.terminals.Add(opt);
            this.outdoorPaymentTerminalBindingSource.DataSource = opt;
            this.outdoorPaymentTerminalBindingSource.ResetBindings(false);
            this.radDropDownList1.SelectedValue = opt.OutdoorPaymentTerminalId;
        }

        private void radButton9_Click(object sender, EventArgs e)
        {
            this.db.SaveChanges();
        }

        private void radButton5_Click(object sender, EventArgs e)
        {
            if (this.terminals.Count == 0)
                return;
            DialogResult res = Telerik.WinControls.RadMessageBox.Show("Θέλετε να διαγραφεί η επιλεγμένη καταχώρηση;", "Διαγραφή Πωλητή!", MessageBoxButtons.YesNo, Telerik.WinControls.RadMessageIcon.Question);
            if (res == System.Windows.Forms.DialogResult.No)
                return;
            Data.OutdoorPaymentTerminal opt = this.outdoorPaymentTerminalBindingSource.Current as Data.OutdoorPaymentTerminal;
            this.terminals.Remove(opt);
            this.db.Delete(opt.OutdoorPaymentTerminalSchedules.SelectMany(s=>s.OutdoorPaymentTerminalTimeSchedules));
            this.db.Delete(opt.OutdoorPaymentTerminalSchedules);
            this.db.Delete(opt.OutdoorPaymentTerminalNozzles);
            this.db.Delete(opt.OutdoorPaymentTerminalControllers);
            this.db.Delete(opt);
            this.outdoorPaymentTerminalBindingSource.ResetBindings(false);
            //this.db.SaveChanges();
        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            if (this.outdoorPaymentTerminalBindingSource.Current == null)
                return;
            Data.OutdoorPaymentTerminal opt = this.outdoorPaymentTerminalBindingSource.Current as Data.OutdoorPaymentTerminal;
            using (UI.SelectionForms.SelectDispenserNozzleForm sdnf = new SelectionForms.SelectDispenserNozzleForm())
            {
                DialogResult res = sdnf.ShowDialog(this);
                if (res == System.Windows.Forms.DialogResult.Cancel)
                    return;
                foreach (Guid nozzleId in sdnf.SelectedNozzles)
                {
                    int count = opt.OutdoorPaymentTerminalNozzles.Where(n => n.NozzleId == nozzleId).Count();
                    if (count > 0)
                        continue;
                    Data.OutdoorPaymentTerminalNozzle onozzle = new Data.OutdoorPaymentTerminalNozzle();
                    onozzle.OutdoorPaymentTerminalNozzleId = Guid.NewGuid();
                    this.db.Add(onozzle);
                    onozzle.NozzleId = nozzleId;
                    onozzle.Nozzle = this.db.Nozzles.Where(n => n.NozzleId == nozzleId).FirstOrDefault();
                    onozzle.OutdoorPaymentTerminalId = opt.OutdoorPaymentTerminalId;
                    opt.OutdoorPaymentTerminalNozzles.Add(onozzle);
                    onozzle.IsDisabled = false;

                    Guid[] contollerIds = this.db.Nozzles.Select(n => n.Dispenser).Select(d => d.CommunicationControllerId).Distinct().ToArray();
                    foreach (Guid g in contollerIds)
                    {
                        this.AddController(opt, g);
                    }
                }
                this.outdoorPaymentTerminalBindingSource.ResetBindings(false);
                this.outdoorPaymentTerminalNozzleBindingSource.ResetBindings(false);
                this.outdoorPaymentTerminalControllersBindingSource.ResetBindings(false);
            }
        }

        private void radButton2_Click(object sender, EventArgs e)
        {
            if (this.outdoorPaymentTerminalNozzleBindingSource.Position < 0)
                return;
            if (this.outdoorPaymentTerminalNozzleBindingSource.Current == null)
                return;
            DialogResult res = Telerik.WinControls.RadMessageBox.Show("Θέλετε να διαγραφεί η επιλεγμένη καταχώρηση;", "Διαγραφή Ακροσωληνίου!", MessageBoxButtons.YesNo, Telerik.WinControls.RadMessageIcon.Question);
            if (res == System.Windows.Forms.DialogResult.No)
                return;
            Data.OutdoorPaymentTerminalNozzle nozzle = this.outdoorPaymentTerminalNozzleBindingSource.Current as Data.OutdoorPaymentTerminalNozzle;
            nozzle.OutdoorPaymentTerminal.OutdoorPaymentTerminalNozzles.Remove(nozzle);
            this.db.Delete(nozzle.OutdoorPaymentTerminalSchedules);
            this.db.Delete(nozzle);

            this.outdoorPaymentTerminalBindingSource.ResetBindings(false);
            this.outdoorPaymentTerminalNozzleBindingSource.ResetBindings(false);
            this.outdoorPaymentTerminalScheduleBindingSource.ResetBindings(false);
            this.outdoorPaymentTerminalTimeSchedulesBindingSource.ResetBindings(false);
        }

        private void radPageView1_SelectedPageChanged(object sender, EventArgs e)
        {
            Data.OutdoorPaymentTerminal opt = this.outdoorPaymentTerminalBindingSource.Current as Data.OutdoorPaymentTerminal;
            if(opt == null)
                return;

            List<Data.OutdoorPaymentTerminalNozzle> nozzles = new List<Data.OutdoorPaymentTerminalNozzle>();
            Data.OutdoorPaymentTerminalNozzle noSelection = new Data.OutdoorPaymentTerminalNozzle();
            nozzles.Add(noSelection);
            nozzles.AddRange(opt.OutdoorPaymentTerminalNozzles.ToArray());
            this.radDropDownList3.DataSource = null;
            this.radDropDownList3.DataSource = nozzles;
            this.radDropDownList3.DisplayMember = "Description";
            this.radDropDownList3.ValueMember = "OutdoorPaymentTerminalNozzleId";
        }

        private void btnAddSchedule_Click(object sender, EventArgs e)
        {
            if (this.outdoorPaymentTerminalBindingSource.Current == null)
                return;
            Data.OutdoorPaymentTerminal opt = this.outdoorPaymentTerminalBindingSource.Current as Data.OutdoorPaymentTerminal;
            Data.OutdoorPaymentTerminalSchedule schedule = new Data.OutdoorPaymentTerminalSchedule();
            schedule.OutdoorPaymentTerminalScheduleId = Guid.NewGuid();
            this.db.Add(schedule);
            schedule.OutdoorPaymentTerminalId = opt.OutdoorPaymentTerminalId;
            schedule.OutdoorPaymentTerminal = opt;
            opt.OutdoorPaymentTerminalSchedules.Add(schedule);

            //this.outdoorPaymentTerminalScheduleBindingSource.DataSource = opt.OutdoorPaymentTerminalSchedules;
            this.outdoorPaymentTerminalBindingSource.ResetBindings(false);
            this.outdoorPaymentTerminalScheduleBindingSource.ResetBindings(false);
        }

        private void btnAddTimeSchedule_Click(object sender, EventArgs e)
        {

            if (this.outdoorPaymentTerminalScheduleBindingSource.Current == null)
                return;
            Data.OutdoorPaymentTerminalSchedule schedule = this.outdoorPaymentTerminalScheduleBindingSource.Current as Data.OutdoorPaymentTerminalSchedule;
            Data.OutdoorPaymentTerminalTimeSchedule tSchedule = new Data.OutdoorPaymentTerminalTimeSchedule();
            tSchedule.OutdoorPaymentTerminalTimeScheduleId = Guid.NewGuid();
            this.db.Add(tSchedule);
            tSchedule.OutdoorPaymentTerminalScheduleId = schedule.OutdoorPaymentTerminalScheduleId;
            tSchedule.OutdoorPaymentTerminalSchedule = schedule;
            schedule.OutdoorPaymentTerminalTimeSchedules.Add(tSchedule);
            tSchedule.TimeFrom = DateTime.Today.Add(new TimeSpan(23, 0, 0));
            tSchedule.Duration = 480;
            this.outdoorPaymentTerminalScheduleBindingSource.ResetBindings(false);
            this.outdoorPaymentTerminalTimeSchedulesBindingSource.ResetBindings(false);
        }

        private void btnAddController_Click(object sender, EventArgs e)
        {
            if (this.outdoorPaymentTerminalBindingSource.Current == null)
                return;
            Data.OutdoorPaymentTerminal opt = this.outdoorPaymentTerminalBindingSource.Current as Data.OutdoorPaymentTerminal;
            using (SelectionForms.SelectController sc = new SelectionForms.SelectController())
            {
                if (sc.ShowDialog(this) == System.Windows.Forms.DialogResult.Cancel)
                    return;
                foreach (Guid g in sc.SelectedControllers)
                {
                    this.AddController(opt, g);
                }
                this.outdoorPaymentTerminalBindingSource.ResetBindings(false);
                this.outdoorPaymentTerminalControllersBindingSource.ResetBindings(false);
            }
        }

        private void btnDeleteController_Click(object sender, EventArgs e)
        {
            if(this.outdoorPaymentTerminalControllersBindingSource.Position < 0)
                return;
            if(this.outdoorPaymentTerminalControllersBindingSource.Current == null)
                return;
            DialogResult res = Telerik.WinControls.RadMessageBox.Show("Θέλετε να διαγραφεί η επιλεγμένη καταχώρηση;", "Διαγραφή Ελεκτή!", MessageBoxButtons.YesNo, Telerik.WinControls.RadMessageIcon.Question);
            if (res == System.Windows.Forms.DialogResult.No)
                return;
            Data.OutdoorPaymentTerminalController controller = this.outdoorPaymentTerminalControllersBindingSource.Current as Data.OutdoorPaymentTerminalController;
            controller.OutdoorPaymentTerminal.OutdoorPaymentTerminalControllers.Remove(controller);
            this.db.Delete(controller);

            this.outdoorPaymentTerminalBindingSource.ResetBindings(false);
            this.outdoorPaymentTerminalControllersBindingSource.ResetBindings(false);
        }

        private void btnDeleteSchedule_Click(object sender, EventArgs e)
        {
            if (this.outdoorPaymentTerminalScheduleBindingSource.Position < 0)
                return;
            if (this.outdoorPaymentTerminalScheduleBindingSource.Current == null)
                return;

            DialogResult res = Telerik.WinControls.RadMessageBox.Show("Θέλετε να διαγραφεί η επιλεγμένη καταχώρηση;", "Διαγραφή Προγραμματισμού!", MessageBoxButtons.YesNo, Telerik.WinControls.RadMessageIcon.Question);
            if (res == System.Windows.Forms.DialogResult.No)
                return;

            Data.OutdoorPaymentTerminalSchedule schedule = this.outdoorPaymentTerminalScheduleBindingSource.Current as Data.OutdoorPaymentTerminalSchedule;
            schedule.OutdoorPaymentTerminal.OutdoorPaymentTerminalSchedules.Remove(schedule);
            this.db.Delete(schedule.OutdoorPaymentTerminalTimeSchedules);
            this.db.Delete(schedule);
            this.outdoorPaymentTerminalBindingSource.ResetBindings(false);
            this.outdoorPaymentTerminalScheduleBindingSource.ResetBindings(false);
        }

        private void btnDeleteTimeSchedule_Click(object sender, EventArgs e)
        {
            if (this.outdoorPaymentTerminalTimeSchedulesBindingSource.Position < 0)
                return;
            if (this.outdoorPaymentTerminalTimeSchedulesBindingSource.Current == null)
                return;

            DialogResult res = Telerik.WinControls.RadMessageBox.Show("Θέλετε να διαγραφεί η επιλεγμένη καταχώρηση;", "Διαγραφή Χρονικού Προγραμματισμού!", MessageBoxButtons.YesNo, Telerik.WinControls.RadMessageIcon.Question);
            if (res == System.Windows.Forms.DialogResult.No)
                return;

            Data.OutdoorPaymentTerminalTimeSchedule schedule = this.outdoorPaymentTerminalTimeSchedulesBindingSource.Current as Data.OutdoorPaymentTerminalTimeSchedule;
            schedule.OutdoorPaymentTerminalSchedule.OutdoorPaymentTerminalTimeSchedules.Remove(schedule);
            this.db.Delete(schedule);

            this.outdoorPaymentTerminalScheduleBindingSource.ResetBindings(false);
            this.outdoorPaymentTerminalTimeSchedulesBindingSource.ResetBindings(false);
        }

        void Events_Changed(object sender, Telerik.OpenAccess.ChangeEventArgs e)
        {
            if (e.PersistentObject.GetType() == typeof(Data.OutdoorPaymentTerminalSchedule))
            {
                if (e.PropertyName == "ScheduleType")
                {
                    Data.OutdoorPaymentTerminalSchedule schedule = e.PersistentObject as Data.OutdoorPaymentTerminalSchedule;
                    this.ShowHideScheduleTypeControls(schedule);
                    if (schedule.ScheduleType == (int)Common.Enumerators.ScheduleTypeEnum.Date)
                        schedule.DayOfWeek = null;
                    else if (schedule.ScheduleType == (int)Common.Enumerators.ScheduleTypeEnum.WayOfWeek)
                        schedule.ScheduleDate = null;
                }
                else if (e.PropertyName == "OutdoorPaymentTerminalNozzleId")
                {
                    this.outdoorPaymentTerminalScheduleBindingSource.ResetBindings(false);
                }
            }
        }
    }
}
