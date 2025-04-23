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
    public partial class ControllerSettings : RadForm
    {
        private Data.DatabaseModel database = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
        private BindingList<Data.CommunicationController> controllers;
        public ControllerSettings()
        {
            InitializeComponent();

            controllers = new BindingList<Data.CommunicationController>(this.database.CommunicationControllers.OrderBy(c=>c.Name).ToList());
            this.communicationControllerBindingSource.DataSource = controllers;
            this.radListControl1.DataSource = this.communicationControllerBindingSource;
            this.radListControl1.DisplayMember = "Name";
            this.radListControl1.ValueMember = "CommunicationControllerId";

            List<Common.Enumerators.EnumItem> list = Common.Enumerators.EnumToList.EnumList<Common.Enumerators.CommunicationTypeEnum>();
            
            this.communicationProtocolRadDropDownList.DataSource = list;
            this.communicationProtocolRadDropDownList.DisplayMember = "Description";
            this.communicationProtocolRadDropDownList.ValueMember = "Value";

            this.portDropDown.Text = "";
            var q = System.IO.Ports.SerialPort.GetPortNames().ToList().Select(l => new { Name = l }).ToList();
            this.portDropDown.DataSource = q;
            this.portDropDown.DisplayMember = "Name";
            this.portDropDown.ValueMember = "Name";

            this.communicationControllerBindingSource.ResetBindings(false);

            this.radButton1.DataBindings.Add(new System.Windows.Forms.Binding("Enabled", this.database, "DatabaseChanged", true));
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            Data.CommunicationController newController = new Data.CommunicationController();
            newController.CommunicationProtocol = (int)Common.Enumerators.CommunicationTypeEnum.RS232;
            newController.Name = "(Νέος Ελεγκτής)";
            newController.CommunicationControllerId = Guid.NewGuid();

            this.database.Add(newController);
            this.controllers.Add(newController);
            this.communicationControllerBindingSource.ResetBindings(false);
            this.communicationControllerBindingSource.Position = this.controllers.Count - 1;
        }

        private void ControllerSettings_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!this.database.DatabaseChanged)
                return;
            DialogResult res = MessageBox.Show("Θέλετε να αποθηκευτούν οι αλλαγές που κάνατε;", "Έγιναν αλλαγές", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
            if (res == System.Windows.Forms.DialogResult.No)
                return;
            else if (res == System.Windows.Forms.DialogResult.Yes)
            {
                this.database.SaveChanges();
                return;
            }
            e.Cancel = true;
        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            this.database.SaveChanges();
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if(this.radListControl1.SelectedItem == null)
                return;

            Data.CommunicationController controller = this.radListControl1.SelectedItem.DataBoundItem as Data.CommunicationController;
            if (controller == null)
                return;
            if (controller.Tanks.Count > 0 || controller.Dispensers.Count > 0)
            {
                MessageBox.Show("Ο Ελεγκτής που θέλετε να διαγράψετε περιέχει συνδέσεις.", "Σφάλμα διαγραφής", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (MessageBox.Show("Θέλετε να διαγράψετε την επιλεγμένη επιλογή;", "Διαγραφή Ελεγκτή", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.No)
                return;
            this.controllers.Remove(controller);
            this.database.Delete(controller);
        }
    }
}
