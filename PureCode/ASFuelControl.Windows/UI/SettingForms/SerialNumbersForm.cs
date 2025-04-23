using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Telerik.WinControls.UI;
using ASFuelControl.Windows.UI.Forms;
using Telerik.WinControls;

namespace ASFuelControl.Windows.UI.SettingForms
{
    public partial class SerialNumbersForm : RadForm
    {
        Data.DatabaseModel database = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);

        public SerialNumbersForm()
        {
            InitializeComponent();
            this.deviceSettingBindingSource.DataSource = this.database.DeviceSettings.Where(d => d.IsSerialNumber).OrderBy(d => d.DeviceType).OrderBy(d => d.Description);
        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void radButton2_Click(object sender, EventArgs e)
        {
            this.deviceSettingRadGridView.Print(true);
        }

        private void SerialNumbersForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {

                if (this.database.HasChanges)
                {
                    DialogResult res = Telerik.WinControls.RadMessageBox.Show("Θέλετε να αποθηκευτούν οι αλλαγές που κάνατε;", "Έχουν γίνει αλλαγές", MessageBoxButtons.YesNoCancel, Telerik.WinControls.RadMessageIcon.Question);
                    if (res == System.Windows.Forms.DialogResult.Yes)
                        this.database.SaveChanges();
                    else if (res == System.Windows.Forms.DialogResult.Cancel)
                        e.Cancel = true;
                }
            }
            catch (Exception ex)
            {

            }
        }

        private void radButton3_Click(object sender, EventArgs e)
        {
            if (this.database.HasChanges)
            {
                DialogResult res = Telerik.WinControls.RadMessageBox.Show("Θέλετε να αποθηκευτούν οι αλλαγές που κάνατε;", "Έχουν γίνει αλλαγές", MessageBoxButtons.YesNoCancel, Telerik.WinControls.RadMessageIcon.Question);
                if (res == System.Windows.Forms.DialogResult.Yes)
                    this.database.SaveChanges();
                else if (res == System.Windows.Forms.DialogResult.Cancel)
                    return;
                else
                {
                    this.database.ClearChanges();
                    this.deviceSettingBindingSource.DataSource = this.database.DeviceSettings.Where(d => d.IsSerialNumber).OrderBy(d => d.DeviceType).OrderBy(d => d.Description);
                    this.deviceSettingBindingSource.ResetBindings(false);
                }
            }
            using (DeviceSettingForm dsf = new DeviceSettingForm())
            {
                dsf.CreateDeviceSetting();
                DialogResult res = dsf.ShowDialog(this);
                if (res == System.Windows.Forms.DialogResult.Cancel)
                    return;
                var q = this.database.DeviceSettings.Where(d => d.IsSerialNumber).OrderBy(d => d.DeviceType).OrderBy(d => d.Description);
                this.database.Refresh(Telerik.OpenAccess.RefreshMode.OverwriteChangesFromStore, q);
                this.deviceSettingBindingSource.DataSource = q; 
                this.deviceSettingBindingSource.ResetBindings(false);
            }
        }

        private void radButton4_Click(object sender, EventArgs e)
        {
            if (this.deviceSettingBindingSource.Position < 0)
                return;
            Data.DeviceSetting setting = this.deviceSettingBindingSource.Current as Data.DeviceSetting;
            if (setting == null)
                return;
            if (setting.DeviceId != Guid.Empty)
            {
                RadMessageBox.Show("Δεν επιτρέπεται η διαγραφή της επιλεγμένης καταχώρησης", "Σφάλμα Διαγραφής", MessageBoxButtons.OK, RadMessageIcon.Error);
                return;
            }
            if (RadMessageBox.Show("Θέλετε να διαγράψετε την επιλεγμένη καταχώρηση;", "Διαγραφή...", MessageBoxButtons.YesNo, RadMessageIcon.Question) == System.Windows.Forms.DialogResult.No)
                return;
            this.deviceSettingBindingSource.Remove(setting);
            this.database.Delete(setting);
            this.deviceSettingBindingSource.ResetBindings(false);
        }
    }
}
