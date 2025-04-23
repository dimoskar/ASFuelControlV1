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
    public partial class FleetManagmentForm : Form
    {
        private Data.DatabaseModel database = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
        private List<Data.FleetManagmentCotroller> controllers = new List<Data.FleetManagmentCotroller>();

        public FleetManagmentForm()
        {
            InitializeComponent();
            
            this.controllers = this.database.FleetManagmentCotrollers.OrderBy(c => c.ComPort).ToList();
            this.fleetManagmentCotrollerBindingSource.DataSource = controllers;
            this.fleetManagmentCotrollerBindingSource.ResetBindings(false);
            this.fleetManagerDispensersBindingSource.ResetBindings(false);
            this.fleetManagmentSchedulesBindingSource.ResetBindings(false);

            var q = System.IO.Ports.SerialPort.GetPortNames().ToList().Select(l => new { Name = l }).ToList();
            GridViewComboBoxColumn col1 = this.fleetManagmentCotrollerRadGridView.Columns["ComPort"] as GridViewComboBoxColumn;
            col1.DataSource = q;
            col1.DisplayMember = "Name";
            col1.ValueMember = "Name";

            GridViewComboBoxColumn col2 = this.radGridView1.Columns[0] as GridViewComboBoxColumn;
            col2.DataSource = this.database.Dispensers.ToList().OrderBy(d => d.DescriptionExt);
            col2.DisplayMember = "DescriptionExt";
            col2.ValueMember = "DispenserId";

            GridViewComboBoxColumn col3 = this.radGridView1.Columns[1] as GridViewComboBoxColumn;
            col3.DataSource = this.database.InvoiceTypes.Where(i => i.TransactionType == 0).ToList().OrderBy(d => d.Description);
            col3.DisplayMember = "Description";
            col3.ValueMember = "InvoiceTypeId";

            this.database.Events.Added += new Telerik.OpenAccess.AddEventHandler(Events_Added);
            this.database.Events.Changed += new Telerik.OpenAccess.ChangeEventHandler(Events_Changed);
            this.database.Events.Removed += new Telerik.OpenAccess.RemoveEventHandler(Events_Removed);
            this.btnSave.DataBindings.Add("Enabled", this.database, "HasChanges");
            //this.portDropDown.DataSource = q;
            //this.portDropDown.DisplayMember = "Name";
            //this.portDropDown.ValueMember = "Name";

            DeviceType[] devices = new DeviceType[]
            {
                new DeviceType(){ Type=0, Description="Σειριακή Συσκευή" },
                new DeviceType(){ Type=1, Description="Συσκευή TCP" }
            };

            ((GridViewComboBoxColumn)this.fleetManagmentCotrollerRadGridView.Columns["ControlerType"]).DataSource = devices;
            ((GridViewComboBoxColumn)this.fleetManagmentCotrollerRadGridView.Columns["ControlerType"]).ValueMember = "Type";
            ((GridViewComboBoxColumn)this.fleetManagmentCotrollerRadGridView.Columns["ControlerType"]).DisplayMember = "Description";

            this.Disposed += FleetManagmentForm_Disposed;
        }

        private void FleetManagmentForm_Disposed(object sender, EventArgs e)
        {
            this.database.Dispose();
        }

        void Events_Removed(object sender, Telerik.OpenAccess.RemoveEventArgs e)
        {
            this.btnSave.DataBindings[0].ReadValue();
        }

        void Events_Changed(object sender, Telerik.OpenAccess.ChangeEventArgs e)
        {
            this.btnSave.DataBindings[0].ReadValue();
        }

        void Events_Added(object sender, Telerik.OpenAccess.AddEventArgs e)
        {
            this.btnSave.DataBindings[0].ReadValue();
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            Data.FleetManagmentCotroller controller = new Data.FleetManagmentCotroller();
            controller.FleetManagmentCotrollerId = Guid.NewGuid();
            this.database.Add(controller);
            this.controllers.Add(controller);
            this.fleetManagmentCotrollerBindingSource.ResetBindings(false);
            this.fleetManagerDispensersBindingSource.ResetBindings(false);
            this.fleetManagmentSchedulesBindingSource.ResetBindings(false);

            this.fleetManagmentCotrollerBindingSource.Position = this.fleetManagmentCotrollerBindingSource.IndexOf(controller);
        }

        private void brnAddDispenser_Click(object sender, EventArgs e)
        {
            if (this.fleetManagmentCotrollerBindingSource.Position < 0)
                return;
            Data.FleetManagmentCotroller controller = this.fleetManagmentCotrollerBindingSource.Current as Data.FleetManagmentCotroller;
            if (controller == null)
                return;

            Data.FleetManagerDispenser fDispenser = new Data.FleetManagerDispenser();
            fDispenser.FleetManagerDispenserId = Guid.NewGuid();
            fDispenser.FleetManagmentCotrollerId = controller.FleetManagmentCotrollerId;
            fDispenser.FleetManagmentCotroller = controller;
            fDispenser.Dispenser = this.database.Dispensers.FirstOrDefault();
            if (fDispenser != null)
                fDispenser.DispenserId = fDispenser.Dispenser.DispenserId;
            controller.FleetManagerDispensers.Add(fDispenser);
            this.database.Add(fDispenser);

            this.fleetManagerDispensersBindingSource.ResetBindings(false);
            this.fleetManagmentSchedulesBindingSource.ResetBindings(false);

            this.fleetManagerDispensersBindingSource.Position = this.fleetManagerDispensersBindingSource.IndexOf(fDispenser);
        }

        private void btnAddSchedule_Click(object sender, EventArgs e)
        {
            if (this.fleetManagerDispensersBindingSource.Position < 0)
                return;
            Data.FleetManagerDispenser dispenser = this.fleetManagerDispensersBindingSource.Current as Data.FleetManagerDispenser;
            if (dispenser == null)
                return;

            Data.FleetManagmentSchedule schedule = new Data.FleetManagmentSchedule();
            schedule.FleetManagmentScheduleId = Guid.NewGuid();
            schedule.FleetManagerDispenserId = dispenser.FleetManagerDispenserId;
            schedule.FleetManagerDispenser = dispenser;

            dispenser.FleetManagmentSchedules.Add(schedule);
            this.database.Add(schedule);

            this.fleetManagmentSchedulesBindingSource.ResetBindings(false);
            this.fleetManagmentSchedulesBindingSource.Position = this.fleetManagmentSchedulesBindingSource.IndexOf(schedule);
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            this.database.SaveChanges();
            this.btnSave.DataBindings[0].ReadValue();
        }
    }

    public class DeviceType
    {
        public int Type { set; get; }
        public string Description { set; get; }
    }
}
