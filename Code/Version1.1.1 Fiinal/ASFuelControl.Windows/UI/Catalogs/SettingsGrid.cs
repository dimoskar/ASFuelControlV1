using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ASFuelControl.Windows.UI.Catalogs
{
    public partial class SettingsGrid : UserControl
    {
        public event EventHandler PriceChanged;

        public SettingsGrid()
        {
            InitializeComponent();
            this.ApplyUser();
        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            UI.SettingForms.MainSettingsForm settingsForm = new SettingForms.MainSettingsForm();
            settingsForm.Show(this);
        }

        private void radButton2_Click(object sender, EventArgs e)
        {
            UI.SettingForms.FuelPumpSettings fpForm = new SettingForms.FuelPumpSettings();
            fpForm.Show(this);
        }

        private void radButton3_Click(object sender, EventArgs e)
        {
            UI.SettingForms.TankSettings tankForm = new SettingForms.TankSettings();
            tankForm.Show(this);
        }

        private void radButton5_Click(object sender, EventArgs e)
        {
            UI.SettingForms.InvoiceTypesForm invoiceTypeForm = new SettingForms.InvoiceTypesForm();
            invoiceTypeForm.Show(this);
        }

        private void radButton6_Click(object sender, EventArgs e)
        {
            UI.SettingForms.PriceSettingsForm priceForm = new SettingForms.PriceSettingsForm();
            priceForm.FormClosed += new FormClosedEventHandler(priceForm_FormClosed);
            priceForm.Show(this);
        }

        void priceForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            UI.SettingForms.PriceSettingsForm f = sender as UI.SettingForms.PriceSettingsForm;
            f.FormClosed -= new FormClosedEventHandler(priceForm_FormClosed);
            if (f.PriceChanged)
            {
                if (this.PriceChanged != null)
                    this.PriceChanged(this, new EventArgs());
            }
        }

        private void radButton8_Click(object sender, EventArgs e)
        {
            UI.SelectionForms.SelectTraderForm traderForm = new UI.SelectionForms.SelectTraderForm();
            traderForm.Show(this);
        }

        private void btnAlerts_Click(object sender, EventArgs e)
        {
            UI.Forms.AlertsForm alertForm = new UI.Forms.AlertsForm();
            alertForm.Show(this);
            alertForm.LoadData();
        }

        private void btnUsers_Click(object sender, EventArgs e)
        {
            UI.SettingForms.UserSettingsForm userForm = new UI.SettingForms.UserSettingsForm();
            userForm.LoadData();
            userForm.Show(this);
        }

        private void btnUsersLimited_Click(object sender, EventArgs e)
        {
            UI.SettingForms.UserSettingsForm userForm = new UI.SettingForms.UserSettingsForm();
            userForm.LimitedView = true;
            userForm.LoadData();
            userForm.Show(this);
        }
        public void ApplyUser()
        {
            return;
            if (Program.CurrentUserLevel == Common.Enumerators.ApplicationUserLevelEnum.Administrator)
            {
                this.btnControllers.Show();
                this.btnCustomers.Show();
                this.btnDispensers.Show();
                this.btnInvoiceTypes.Show();
                this.btnPrices.Show();
                this.btnSettings.Show();
                this.btnTanks.Show();
                this.btnUsers.Show();
                this.btnAlerts.Show();
            }
            else if (Program.CurrentUserLevel == Common.Enumerators.ApplicationUserLevelEnum.SuperUser)
            {
                this.btnControllers.Hide();
                this.btnCustomers.Show();
                this.btnDispensers.Hide();
                this.btnInvoiceTypes.Show();
                this.btnPrices.Show();
                this.btnSettings.Hide();
                this.btnTanks.Hide();
                this.btnUsers.Hide();
                this.btnAlerts.Hide();
            }
            else 
            {
                this.btnControllers.Hide();
                this.btnCustomers.Hide();
                this.btnDispensers.Hide();
                this.btnInvoiceTypes.Hide();
                this.btnPrices.Hide();
                this.btnSettings.Hide();
                this.btnTanks.Hide();
                this.btnUsers.Hide();
                this.btnAlerts.Hide();
            }
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

        private void radPageView1_PageIndexChanging(object sender, Telerik.WinControls.UI.RadPageViewIndexChangingEventArgs e)
        {
            
        }

        private void radPageView1_SelectedPageChanging(object sender, Telerik.WinControls.UI.RadPageViewCancelEventArgs e)
        {
            if (e.Page != this.radPageViewPage2)
                return;
            e.Cancel = this.VerifyUser() != Common.Enumerators.ApplicationUserLevelEnum.Administrator;
            if (!e.Cancel)
                Program.AdminConnected = true;
        }

        private void btnControllers_Click(object sender, EventArgs e)
        {
            UI.SettingForms.ControllerSettings controllerForm = new SettingForms.ControllerSettings();
            controllerForm.Show(this);
        }

        private void btnPrinter_Click(object sender, EventArgs e)
        {
            UI.SettingForms.PrinterSettings printerForm = new SettingForms.PrinterSettings();
            printerForm.Show(this);

        }

        private void radButton1_Click_1(object sender, EventArgs e)
        {
            UI.SettingForms.CommunicationSettingsForm userForm = new UI.SettingForms.CommunicationSettingsForm();
            userForm.Show(this);
        }
    }
}
