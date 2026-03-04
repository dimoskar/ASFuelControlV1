using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Telerik.WinControls.UI;
using System.IO;
using Telerik.WinControls;

namespace ASFuelControl.Windows.UI.SettingForms
{
    public partial class MainSettingsForm : RadForm
    {
        List<Data.FleetManagmentCotroller> controllers = new List<Data.FleetManagmentCotroller>();

        public MainSettingsForm()
        {
            InitializeComponent();
            //this.radPageView1.Pages.Remove(this.radPageViewPage5);
            this.LoadData();
        }

        private void LoadData()
        {
            this.radPageView1.SelectedPage = this.radPageViewPage1;
            this.nameTextBox.Text = Data.Implementation.OptionHandler.Instance.GetOption("CompanyName");
            this.addressTextBox.Text = Data.Implementation.OptionHandler.Instance.GetOption("CompanyAddress");
            this.mainAddressTextBox.Text = Data.Implementation.OptionHandler.Instance.GetOption("CompanyMainAddress");
            this.occupationTextBox.Text = Data.Implementation.OptionHandler.Instance.GetOption("CompanyOccupation");
            this.phoneTextBox.Text = Data.Implementation.OptionHandler.Instance.GetOption("CompanyPhone");
            this.faxTextBox.Text = Data.Implementation.OptionHandler.Instance.GetOption("CompanyFax");
            this.cityTextBox.Text = Data.Implementation.OptionHandler.Instance.GetOption("CompanyCity");
            this.postalCodeextBox.Text = Data.Implementation.OptionHandler.Instance.GetOption("CompanyPostalCode");
            this.taxOfficeTextBox.Text = Data.Implementation.OptionHandler.Instance.GetOption("CompanyTaxOffice");
            this.companyEmail.Text = Data.Implementation.OptionHandler.Instance.GetOption("CompanyEmail");
            this.posTerminalIdTxt.Text = Data.Implementation.OptionHandler.Instance.GetOption("POSTerminalID");

            this.tinCompanyTextBox.Text = Data.Implementation.OptionHandler.Instance.GetOption("CompanyTIN");
            this.efkTextBox.Text = Data.Implementation.OptionHandler.Instance.GetOption("CompanyEFK");
            this.sxetArithAnaf.Text = Data.Implementation.OptionHandler.Instance.GetOption("CompanyReferenceNumber");
            this.tinSenderTextBox.Text = Data.Implementation.OptionHandler.Instance.GetOption("SenderTIN");
            this.amdikaTextBox.Text = Data.Implementation.OptionHandler.Instance.GetOption("AMDIKA");

            this.storeIdCode.Text = Data.Implementation.OptionHandler.Instance.GetOption("StoreId");
            int brand = Data.Implementation.OptionHandler.Instance.GetIntOption("Brand", 0);
            
            this.brandDropDown.SelectedItem = this.brandDropDown.Items.FirstOrDefault(i => i.Tag.ToString() == brand.ToString());
            this.hasBrandQRCode.IsChecked = Data.Implementation.OptionHandler.Instance.GetBoolOption("HasBrandQRCode", false);

            bool finalized = Data.Implementation.OptionHandler.Instance.GetBoolOption("IsFinalized", false);
            this.IsFinalized.IsChecked = finalized;

            bool startOnStart = Data.Implementation.OptionHandler.Instance.GetBoolOption("StartThreadsOnStart", false);
            this.startThreadsOnStartCheck.IsChecked = startOnStart;

            bool lockTank = Data.Implementation.OptionHandler.Instance.GetBoolOption("LockOnAlert", true);
            this.chkLockTank.IsChecked = lockTank;

            int balanceThresholdMin = Data.Implementation.OptionHandler.Instance.GetIntOption("BalanceThreshold", 10);
            this.balanceThreshold.Value = balanceThresholdMin;

            bool applicationLocked = Data.Implementation.OptionHandler.Instance.GetIntOption("ApplicationLocked", 777) != 777;
            this.appLock.IsChecked = applicationLocked;
            bool sd = Data.Implementation.OptionHandler.Instance.GetBoolOption("SendData", false);
            this.sendData.IsChecked = sd;

            bool mb = Data.Implementation.OptionHandler.Instance.GetBoolOption("MonthBalanceEnabled", false);
            this.monthBalance.IsChecked = mb;

            int tanckCheckInt = Data.Implementation.OptionHandler.Instance.GetIntOption("TankCheckInterval", 5);
            TimeSpan ts = TimeSpan.FromMinutes(tanckCheckInt);
            this.tankCheckHour.Value = ts.Hours;
            this.tankCheckMinute.Value = ts.Minutes;

            int deliveryWaiting = Data.Implementation.OptionHandler.Instance.GetIntOption("DeliveryWaitingTime", 900);
            TimeSpan ts1 = TimeSpan.FromSeconds(deliveryWaiting);
            this.waitingDeliveryMins.Value = (int)ts1.TotalMinutes;
            this.waitingDeliverySecs.Value = ts1.Seconds;

            int literCheckWaiting = Data.Implementation.OptionHandler.Instance.GetIntOption("LiterCheckWaitingTime", 60);
            TimeSpan ts2 = TimeSpan.FromSeconds(literCheckWaiting);
            this.waitingLiterCheckMins.Value = (int)ts2.TotalMinutes;
            this.waitingLiterCheckSecs.Value = ts2.Seconds;

            this.literCheckInvoiceType.DataSource = Data.Implementation.OptionHandler.Instance.Database.InvoiceTypes;
            this.literCheckInvoiceType.DisplayMember = "Description";
            this.literCheckInvoiceType.ValueMember = "InvoiceTypeId";

            this.deliveryCheckInvoiceType.DataSource = Data.Implementation.OptionHandler.Instance.Database.InvoiceTypes;
            this.deliveryCheckInvoiceType.DisplayMember = "Description";
            this.deliveryCheckInvoiceType.ValueMember = "InvoiceTypeId";

            this.returnInvoiceType.DataSource = Data.Implementation.OptionHandler.Instance.Database.InvoiceTypes;
            this.returnInvoiceType.DisplayMember = "Description";
            this.returnInvoiceType.ValueMember = "InvoiceTypeId";

            this.sendCheckInvoiceType.DataSource = Data.Implementation.OptionHandler.Instance.Database.InvoiceTypes;
            this.sendCheckInvoiceType.DisplayMember = "Description";
            this.sendCheckInvoiceType.ValueMember = "InvoiceTypeId";

            this.literCheckInvoiceType.SelectedValue = Data.Implementation.OptionHandler.Instance.GetGuidOption("LiterCheckInvoiceType", Guid.Empty);
            this.deliveryCheckInvoiceType.SelectedValue = Data.Implementation.OptionHandler.Instance.GetGuidOption("DeliveryCheckInvoiceType", Guid.Empty);
            this.returnInvoiceType.SelectedValue = Data.Implementation.OptionHandler.Instance.GetGuidOption("ReturnInvoiceType", Guid.Empty);
            this.sendCheckInvoiceType.SelectedValue = Data.Implementation.OptionHandler.Instance.GetGuidOption("SendCheckInvoiceType", Guid.Empty);
            this.spinVAT.Value = Data.Implementation.OptionHandler.Instance.GetDecimalOption("VATValue", (decimal)23);

            this.invReplCodeFrom.Value = Data.Implementation.OptionHandler.Instance.GetIntOption("InvoiceReplaceCodeFrom", 173);
            this.invReplCodeTo.Value = Data.Implementation.OptionHandler.Instance.GetIntOption("InvoiceReplaceCodeTo", 222);

            this.serialNumberText.Text = Data.Implementation.OptionHandler.Instance.GetOption("SerialNumber");

            this.txtBank.Text = Data.Implementation.OptionHandler.Instance.GetOption("CompanyBank");
            this.txtIBAN.Text = Data.Implementation.OptionHandler.Instance.GetOption("CompanyIBAN");
            this.txtBank2.Text = Data.Implementation.OptionHandler.Instance.GetOption("CompanyBank2");
            this.txtIBAN2.Text = Data.Implementation.OptionHandler.Instance.GetOption("CompanyIBAN2");


            this.myDataActiveChbx.Checked = Data.Implementation.OptionHandler.Instance.GetBoolOption("MyDataIsActive", false);
            this.myDataSubscriptionKey.Text = Data.Implementation.OptionHandler.Instance.GetOption("MyDataSubscriptionKey", "");
            this.branchSpinEditor.Value = Data.Implementation.OptionHandler.Instance.GetIntOption("CompanyBranch", 0);
            this.myDataUrl.Text = Data.Implementation.OptionHandler.Instance.GetOption("MyDataUrl", "https://mydata-dev.azure-api.net");
            this.myDataUserName.Text = Data.Implementation.OptionHandler.Instance.GetOption("MyDataUserName", "");
            DateTime dtMyDataStart = DateTime.Now;
            string datestartMyData = Data.Implementation.OptionHandler.Instance.GetOption("MyDataStartDate");
            if (!string.IsNullOrEmpty(datestartMyData))
            {
                if (!DateTime.TryParse(datestartMyData, out dtMyDataStart))
                    dtMyDataStart = new DateTime(2022, 1, 1);


            }
            this.dateTimeMyDataStart.Value = dtMyDataStart;


            this.ggpsUserName.Text = Data.Implementation.OptionHandler.Instance.GetOption("AadeServiceUserName", "");
            this.ggpsPassword.Text = Data.Implementation.OptionHandler.Instance.GetOption("AadeServicePassword", "");

            DateTime dtStart = DateTime.Now;
            string datestart = Data.Implementation.OptionHandler.Instance.GetOption("StartDate");
            if (datestart != null && datestart != "")
            {
                DateTime.TryParse(datestart, out dtStart);
            }
            this.startDateDatetimePicker.Value = dtStart;

            string logo = Data.Implementation.OptionHandler.Instance.GetOption("CompanyLogo");
            if (logo != null && logo != "")
            {
                Image img = this.StringToImage(logo);
                if (img != null)
                {
                    img = new Bitmap(img, new Size(140, 140));
                    this.radButton7.Image = img;
                }
            }

            this.radButton9.Enabled = !this.myDataActiveChbx.Checked;

            string colorStr = Data.Implementation.OptionHandler.Instance.GetOption("CompanyInvoiceColor");
            if (colorStr != null && colorStr != "")
            {
                int colorInt = int.Parse(colorStr);
                Image img2 = new Bitmap(30, 30);
                using (Graphics gfx = Graphics.FromImage(img2))
                {
                    using (SolidBrush brush = new SolidBrush(Color.FromArgb(colorInt)))
                    {
                        gfx.FillRectangle(brush, 0, 0, img2.Width, img2.Height);
                    }
                }

                this.radButton8.Image = img2;
            }

            this.arbitransUserName.Text = Data.Implementation.OptionHandler.Instance.GetOption("ProviderInvoiceArbitransName", "");
            this.arbitransKey.Text = Data.Implementation.OptionHandler.Instance.GetOption("ProviderInvoiceArbitransKey", "");
            this.providerUserName.Text = Data.Implementation.OptionHandler.Instance.GetOption("ProviderInvoiceUserName", "");
            this.providerPassword.Text = Data.Implementation.OptionHandler.Instance.GetOption("ProviderInvoiceUserPassword", "");
            this.providerEnabled.Checked = Data.Implementation.OptionHandler.Instance.GetBoolOption("ProviderEnabled", false);
            this.providerUserNameTest.Text = Data.Implementation.OptionHandler.Instance.GetOption("ProviderInvoiceTestUserName", "arambatsis_test");
            this.providerPasswordTest.Text = Data.Implementation.OptionHandler.Instance.GetOption("ProviderInvoiceUserTestPassword", "Gfykd@DbB!RD1$tdwxD&K6hl");
            this.providerIsTest.Checked = Data.Implementation.OptionHandler.Instance.GetBoolOption("ProviderTestMode", true);
            this.mellonGroupApiKeyTxt.Text = Data.Implementation.OptionHandler.Instance.GetOption("MellonGroupApiKey", "");
            this.vivaWalletClientIdTxt.Text = Data.Implementation.OptionHandler.Instance.GetOption("VivaWalletClientId", "");
            this.vivaWalletClientSecretTxt.Text = Data.Implementation.OptionHandler.Instance.GetOption("VivaWalletClientSecret", "");
            var posDatasource = new Dictionary<string, string>()
            {
                { "", "(Χωρίς επιλογή)" },
                { "Mellon.JCC", "JCC - Mellon" },
                { "Mellon.AtticaBank", "Attica Bank - Mellon" },
                { "Mellon.Pancreta", "Pancreta - Mellon" },
                { "Mellon.Tora", "Tora - Mellon" },
                { "Mellon.CardLink", "CardLink - Mellon" },
                { "Mellon.PBT", "PBT (Thessalias) - Mellon" },
                { "Mellon.Nexi", "Nexi - Mellon" },
                { "Mellon.NBG", "NBG (National Bank of Greece) - Mellon" },
                { "Mellon.Worldline", "Worldline - Mellon" },
                { "VivaWallet.VivaWallet", "VivaWallet" }
            };

            this.posTypeCombo.DataSource = new BindingSource(posDatasource, null);
            this.posTypeCombo.DisplayMember = "Value";
            this.posTypeCombo.ValueMember = "Key";
            var posType = Data.Implementation.OptionHandler.Instance.GetOption("POSType", "");
            this.posTypeCombo.SelectedValue = posType;
            //this.startControllerButton.IsEnabled = !App.Mediator.MediatorWorking;
            //this.stopControllerButton.IsEnabled = App.Mediator.MediatorWorking;
        }

        private void SaveData()
        {
            Data.Implementation.OptionHandler.Instance.SetOption("CompanyTIN", this.tinCompanyTextBox.Text);
            Data.Implementation.OptionHandler.Instance.SetOption("CompanyEFK", this.efkTextBox.Text);
            Data.Implementation.OptionHandler.Instance.SetOption("CompanyReferenceNumber", this.sxetArithAnaf.Text);

            Data.Implementation.OptionHandler.Instance.SetOption("SenderTIN", this.tinSenderTextBox.Text);
            Data.Implementation.OptionHandler.Instance.SetOption("AMDIKA", this.amdikaTextBox.Text);
            int duration = (int)this.tankCheckHour.Value * 60 + (int)this.tankCheckMinute.Value;
            int waitDelivery = (int)this.waitingDeliveryMins.Value * 60 + (int)this.waitingDeliverySecs.Value;
            int waitLiterCheck = (int)this.waitingLiterCheckMins.Value * 60 + (int)this.waitingLiterCheckSecs.Value;

            Data.Implementation.OptionHandler.Instance.SetOption("TankCheckInterval", duration);
            Data.Implementation.OptionHandler.Instance.SetOption("DeliveryWaitingTime", waitDelivery);
            Data.Implementation.OptionHandler.Instance.SetOption("LiterCheckWaitingTime", waitLiterCheck);

            Data.Implementation.OptionHandler.Instance.SetOption("IsFinalized", this.IsFinalized.IsChecked);
            Data.Implementation.OptionHandler.Instance.SetOption("StartThreadsOnStart", this.startThreadsOnStartCheck.IsChecked);
            Data.Implementation.OptionHandler.Instance.SetOption("LockOnAlert", this.chkLockTank.IsChecked);
            Data.Implementation.OptionHandler.Instance.SetOption("BalanceThreshold", this.balanceThreshold.Value);


            Data.Implementation.OptionHandler.Instance.SetOption("CompanyBank", this.txtBank.Text);
            Data.Implementation.OptionHandler.Instance.SetOption("CompanyIBAN", this.txtIBAN.Text);
            Data.Implementation.OptionHandler.Instance.SetOption("CompanyBank2", this.txtBank2.Text);
            Data.Implementation.OptionHandler.Instance.SetOption("CompanyIBAN2", this.txtIBAN2.Text);

            Data.Implementation.OptionHandler.Instance.SetOption("StoreId", this.storeIdCode.Text);
            if(this.brandDropDown.SelectedItem != null)
            {
                int val = int.Parse(this.brandDropDown.SelectedItem.Tag.ToString());
                Data.Implementation.OptionHandler.Instance.SetOption("Brand", val);
            }
            Data.Implementation.OptionHandler.Instance.SetOption("HasBrandQRCode", this.hasBrandQRCode.IsChecked);

            if (this.sendData.IsChecked)
            {
                Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);

                bool serialNumberError = false;
                foreach (Data.Tank t in db.Tanks)
                {
                    if (t.FuelType.ExcludeFromBalance.HasValue && t.FuelType.ExcludeFromBalance.Value)
                        continue;
                    if (t.Removed || (t.IsVirtual.HasValue && t.IsVirtual.Value))
                        continue;
                    if (t.TankSerialNumber.Contains("-T-") || t.TankSerialNumber.Contains("-Τ-"))
                        continue;
                    if (t.TankNumber > 0)
                        continue;
                    serialNumberError = true;
                }
                
                foreach(Data.Dispenser fp in db.Dispensers)
                {
                    if (fp.Removed)
                        continue;
                    if (!(fp.PumpSerialNumber.Contains("-P-") || fp.PumpSerialNumber.Contains("-Ρ-")))
                    {
                        serialNumberError = true;
                    }
                    foreach(Data.Nozzle t in fp.Nozzles)
                    {
                        if (t.NozzleFlows.Count == 0)
                            continue;
                        if (t.SerialNumber.Contains("-N-") || t.SerialNumber.Contains("-Ν-"))
                            continue;
                        serialNumberError = true;
                    }
                }
                if (serialNumberError)
                {
                    RadMessageBox.Show("Ελέγξτε τους σειριακούς αριθμούς\r\nΔεξαμενών, Αντλιών και Ακροσωληνίων", "Σφάλμα Στοιχείων ΓΓΠΣ", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
                    this.sendData.IsChecked = false;
                }
                bool traderError = false;
                var qt = db.Traders.Where(t => t.TaxRegistrationNumber != null && t.TaxRegistrationNumber != "").ToArray();
                foreach(Data.Trader tr in qt)
                {
                    if (tr.TaxRegistrationNumber.Length >= 9 && tr.TaxRegistrationNumber.Length <= 17)
                        continue;
                    traderError = true;
                }
                if (traderError)
                {
                    RadMessageBox.Show("Ελέγξτε τα ΑΦΜ των πελατών.\r\nΑν υπάρχει πρέπει να έχει τιμή από 9 έως 17 χαρακτήρες", "Σφάλμα Στοιχείων ΓΓΠΣ", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
                    this.sendData.IsChecked = false;
                }
                var qv = db.Vehicles.ToArray();
                bool vehicleError = false;
                List<Data.Vehicle> errorVehicles = new List<Data.Vehicle>();
                foreach (Data.Vehicle tr in qv)
                {
                    if (tr.PlateNumber != null && tr.PlateNumber.Length >= 6 && tr.PlateNumber.Length <= 15)
                        continue;
                    if (tr.PlateNumber == null || tr.PlateNumber == "")
                        continue;
                    if(tr.PlateNumber.Where(a=>char.IsDigit(a)).Count() == 0)
                        continue;
                    errorVehicles.Add(tr);
                    vehicleError = true;
                }
                if (vehicleError)
                {
                    string errors = String.Join("\r\n", errorVehicles.Select(v=>v.Trader.Name + " - " + v.PlateNumber));
                    RadMessageBox.Show("Σφάλμα στις πινακίδες των παρακάτω οχημάτων:\r\n\r\n" + errors + "\r\n\r\nΟι πινακίδες πρέπει να έχούν τιμή από 6 έως 15 χαρακτήρες ή\r\nνα είναι μόνο κείμενο.", "Σφάλμα Στοιχείων ΓΓΠΣ", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
                    this.sendData.IsChecked = false;
                }
                this.radButton9.Enabled = !this.myDataActiveChbx.Checked;
                db.Dispose();
                
            }

            Data.Implementation.OptionHandler.Instance.SetOption("SendData", this.sendData.IsChecked);
            Data.Implementation.OptionHandler.Instance.SetOption("MonthBalanceEnabled", this.monthBalance.IsChecked);
            

            Data.Implementation.OptionHandler.Instance.SetOption("LiterCheckInvoiceType", this.literCheckInvoiceType.SelectedValue);
            Data.Implementation.OptionHandler.Instance.SetOption("DeliveryCheckInvoiceType", this.deliveryCheckInvoiceType.SelectedValue);
            Data.Implementation.OptionHandler.Instance.SetOption("ReturnInvoiceType", this.returnInvoiceType.SelectedValue);
            Data.Implementation.OptionHandler.Instance.SetOption("SendCheckInvoiceType", this.sendCheckInvoiceType.SelectedValue);

            Data.Implementation.OptionHandler.Instance.SetOption("CompanyName", this.nameTextBox.Text);
            Data.Implementation.OptionHandler.Instance.SetOption("CompanyAddress", this.addressTextBox.Text);
            Data.Implementation.OptionHandler.Instance.SetOption("CompanyMainAddress", this.mainAddressTextBox.Text);
            Data.Implementation.OptionHandler.Instance.SetOption("CompanyOccupation", this.occupationTextBox.Text);
            Data.Implementation.OptionHandler.Instance.SetOption("CompanyPhone", this.phoneTextBox.Text);
            Data.Implementation.OptionHandler.Instance.SetOption("CompanyFax", this.faxTextBox.Text);
            Data.Implementation.OptionHandler.Instance.SetOption("CompanyCity", this.cityTextBox.Text);
            Data.Implementation.OptionHandler.Instance.SetOption("CompanyPostalCode", this.postalCodeextBox.Text);
            Data.Implementation.OptionHandler.Instance.SetOption("CompanyTaxOffice", this.taxOfficeTextBox.Text);
            Data.Implementation.OptionHandler.Instance.SetOption("CompanyEmail", this.companyEmail.Text);
            Data.Implementation.OptionHandler.Instance.SetOption("POSTerminalID", this.posTerminalIdTxt.Text);
            

            Data.Implementation.OptionHandler.Instance.SetOption("ProviderInvoiceArbitransName", this.arbitransUserName.Text);
            Data.Implementation.OptionHandler.Instance.SetOption("ProviderInvoiceArbitransKey", this.arbitransKey.Text);
            Data.Implementation.OptionHandler.Instance.SetOption("ProviderInvoiceUserName", this.providerUserName.Text);
            Data.Implementation.OptionHandler.Instance.SetOption("ProviderInvoiceUserPassword", this.providerPassword.Text);
            Data.Implementation.OptionHandler.Instance.SetOption("ProviderInvoiceTestUserName", this.providerUserNameTest.Text);
            Data.Implementation.OptionHandler.Instance.SetOption("ProviderInvoiceUserTestPassword", this.providerPasswordTest.Text);
            Data.Implementation.OptionHandler.Instance.SetOption("ProviderTestMode", this.providerIsTest.IsChecked);
            Data.Implementation.OptionHandler.Instance.SetOption("POSType", this.posTypeCombo.SelectedValue.ToString());
            Data.Implementation.OptionHandler.Instance.SetOption("MellonGroupApiKey", this.mellonGroupApiKeyTxt.Text);
            Data.Implementation.OptionHandler.Instance.SetOption("VivaWalletClientId", this.vivaWalletClientIdTxt.Text);
            Data.Implementation.OptionHandler.Instance.SetOption("VivaWalletClientSecret", this.vivaWalletClientSecretTxt.Text);

            Data.Implementation.OptionHandler.Instance.SetOption("VATValue", this.spinVAT.Value.ToString("N2"));
            Data.Implementation.OptionHandler.Instance.SetOption("SerialNumber", this.serialNumberText.Text);
            Data.Implementation.OptionHandler.Instance.SetOption("StartDate", this.startDateDatetimePicker.Value.ToString("yyyy/MM/dd HH:mm:ss.fff"));

            Data.Implementation.OptionHandler.Instance.SetOption("InvoiceReplaceCodeFrom", this.invReplCodeFrom.Value.ToString("N0"));
            Data.Implementation.OptionHandler.Instance.SetOption("InvoiceReplaceCodeTo", this.invReplCodeTo.Value.ToString("N0"));

            Data.Implementation.OptionHandler.Instance.SetOption("MyDataIsActive", this.myDataActiveChbx.IsChecked);
            Data.Implementation.OptionHandler.Instance.SetOption("MyDataUrl", this.myDataUrl.Text);
            Data.Implementation.OptionHandler.Instance.SetOption("MyDataUserName", this.myDataUserName.Text);
            Data.Implementation.OptionHandler.Instance.SetOption("MyDataSubscriptionKey", this.myDataSubscriptionKey.Text);
            Data.Implementation.OptionHandler.Instance.SetOption("CompanyBranch", this.branchSpinEditor.Value);
            Data.Implementation.OptionHandler.Instance.SetOption("MyDataStartDate", this.dateTimeMyDataStart.Value.Date.ToString("yyyy/MM/dd HH:mm:ss.fff"));

            Data.Implementation.OptionHandler.Instance.SetOption("AadeServiceUserName", this.ggpsUserName.Text);
            Data.Implementation.OptionHandler.Instance.SetOption("AadeServicePassword", this.ggpsPassword.Text);
            Data.Implementation.OptionHandler.Instance.SetOption("ApplicationLocked", (this.appLock.IsChecked ? 776 : 777));

            Exedron.MyData.Settings.IsActive = Data.Implementation.OptionHandler.Instance.GetBoolOption("MyDataIsActive", false);
            Exedron.MyData.Settings.Username = Data.Implementation.OptionHandler.Instance.GetOption("MyDataUserName", "");
            Exedron.MyData.Settings.SubscriptionKey = Data.Implementation.OptionHandler.Instance.GetOption("MyDataSubscriptionKey", "");

            Data.Implementation.OptionHandler.Instance.SetOption("ProviderEnabled", this.providerEnabled.IsChecked);
        }

        private void MainSettingsForm_Load(object sender, EventArgs e)
        {
            this.stopControllerButton.DataBindings.Add("Enabled", Program.ApplicationMainForm.ThreadControllerInstance, "IsRunning");
            this.startControllerButton.DataBindings.Add("Enabled", Program.ApplicationMainForm.ThreadControllerInstance, "IsStopped");
        }

        private void radButton4_Click(object sender, EventArgs e)
        {
            if (sendData.Checked)
            {
                if (this.amdikaTextBox.Text.Length <= 1)
                {
                    RadMessageBox.Show("Δεν έχετε ορίσει τον ΑΜΔΙΚΑ του πρατηρίου. Δεν μπορεί να γίνει οριστικοποίηση.", "Σφάλμα Οριστικοποίησης", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
                    return;
                }

                Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
                List<Data.Tank> tanks = db.Tanks.Where(t => t.IsVirtual.HasValue && t.IsVirtual.Value).ToList();
                foreach (Data.Tank tank in tanks)
                {
                    if (tank.Removed)
                        continue;
                    if (tank.FuelType.BalanceExclusion)
                        continue;
                    RadMessageBox.Show("Υπάρχουν δεξαμενές που είναι εικονικές. Δεν μπορεί να γίνει οριστικοποίηση.", "Σφάλμα Οριστικοποίησης", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
                    return;
                }

                
            }
            this.SaveData();
            this.Close();
        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);

            Data.Balance firstBalance = db.Balances.OrderBy(b => b.StartDate).FirstOrDefault();
            Data.Balance lastBalance = db.Balances.OrderBy(b => b.StartDate).LastOrDefault();

            UI.SelectionForms.SelectFromToForm sftf = new SelectionForms.SelectFromToForm();
            if(firstBalance != null)
                sftf.StartDate = DateTime.Today.AddDays(-1);// firstBalance.StartDate;
            if (lastBalance != null)
                sftf.EndDate = DateTime.Today.AddSeconds(-1);// lastBalance.EndDate;

            DialogResult res = sftf.ShowDialog();
            if (res == System.Windows.Forms.DialogResult.Cancel)
                return;

            var q = db.Balances.Where(b=>b.StartDate>= sftf.StartDate && b.StartDate <= sftf.EndDate);
            db.Delete(q);
            db.SaveChanges();

            db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);

            DateTime dt = sftf.StartDate.Value.Date;
            while (dt <= sftf.EndDate)
            {
                DateTime dt1 = dt;
                DateTime dt2 = dt1.AddDays(1).AddMilliseconds(-1);

                Data.Balance.CreateBalance(dt1, dt2, Threads.AlertChecker.Instance);
                db.SaveChanges();
                dt = dt.AddDays(1);
            }
            
        }

        private Image StringToImage(string imageString)
        {
            try
            {
                if (imageString == null)
                    throw new ArgumentNullException("imageString");

                byte[] array = Convert.FromBase64String(imageString);
                Image image = Image.FromStream(new MemoryStream(array));
                return image;
            }
            catch
            {
                return null;
            }
        }

        public string ImageToString(Image im)
        {
            MemoryStream ms = new MemoryStream();
            im.Save(ms, im.RawFormat);
            byte[] array = ms.ToArray();

            return Convert.ToBase64String(array);
        }

        private class GroupSum
        {
            public string PumpSerialNumber { set; get; }
            public decimal TotalOut { set; get; }
            public decimal TotalOutNormalized { set; get; }
            public decimal TotalIn { set; get; }
            public decimal TotalInNormalized { set; get; }
        }

        private void stopControllerButton_Click(object sender, EventArgs e)
        {
            Program.ApplicationMainForm.ThreadControllerInstance.StopThreads(true);
            this.stopControllerButton.DataBindings[0].ReadValue();
            this.startControllerButton.DataBindings[0].ReadValue();
        }

        private void startControllerButton_Click(object sender, EventArgs e)
        {
            Program.ApplicationMainForm.ThreadControllerInstance.StartThreads();
            this.stopControllerButton.DataBindings[0].ReadValue();
            this.startControllerButton.DataBindings[0].ReadValue();
        }

        private void radButton5_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void radButton2_Click(object sender, EventArgs e)
        {
            UI.SelectionForms.AttachFillingForm aff = new SelectionForms.AttachFillingForm();
            aff.Show();
        }

        private void radButton6_Click(object sender, EventArgs e)
        {
            Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
            Data.Balance bal = db.Balances.FirstOrDefault();
            if (bal == null)
                return;
            Threads.PrintAgent agent = new Threads.PrintAgent();
            agent.PrintToPrinter(bal, false);
        }

        private void radButton7_Click(object sender, EventArgs e)
        {
            using (SelectionForms.SelectCompanyForm scf = new SelectionForms.SelectCompanyForm())
            {
                if (scf.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
                    return;
                if (scf.SelectedImage == null)
                    return;

                this.radButton7.Image = new Bitmap(scf.SelectedImage, new Size(140, 140));
                Data.Implementation.OptionHandler.Instance.SetOption("CompanyLogo", this.ImageToString(scf.SelectedImage));
                //if (logo != null && logo != "")
                //{
                //    Image img = this.StringToImage(logo);
                //    if (img == null)
                //        return;
                //    img = new Bitmap(img, new Size(120, 120));
                //    this.radButton7.Image = img;
                //}
            }
        }

        private void radButton8_Click(object sender, EventArgs e)
        {
            using (SelectionForms.SelectInvoiceColorForm sicf = new SelectionForms.SelectInvoiceColorForm())
            {
                sicf.LogoImage = this.radButton7.Image;
                if (sicf.LogoImage == null)
                    return;
                DialogResult res = sicf.ShowDialog();
                if (res == System.Windows.Forms.DialogResult.Cancel)
                    return;
                Data.Implementation.OptionHandler.Instance.SetOption("CompanyInvoiceColor", sicf.SelectedColor.ToArgb());

                Image img = new Bitmap(30, 30);
                using (Graphics gfx = Graphics.FromImage(img))
                {
                    using (SolidBrush brush = new SolidBrush(sicf.SelectedColor))
                    {
                        gfx.FillRectangle(brush, 0, 0, img.Width, img.Height);
                    }
                }

                this.radButton8.Image = img;
            }

        }

        private void radButton9_Click(object sender, EventArgs e)
        {
            var myDataRunning = Data.Implementation.OptionHandler.Instance.GetBoolOption("MyDataIsActive", false);
            if(myDataRunning)
                Data.Implementation.OptionHandler.Instance.SetOption("MyDataIsActive", false);
            using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                string command = "UPDATE [MyDataInvoice] SET Status = -10 where Status = 3";
                var rows = db.ExecuteNonQuery(command, new System.Data.Common.DbParameter[] { });
                db.SaveChanges();
            }
            if (myDataRunning)
                Data.Implementation.OptionHandler.Instance.SetOption("MyDataIsActive", true);
        }

        private void myDataActiveChbx_ToggleStateChanging(object sender, StateChangingEventArgs args)
        {
            //if(args.NewValue == Telerik.WinControls.Enumerations.ToggleState.On)
            //    this.radButton9.Enabled = false;
            //else
            //    this.radButton9.Enabled = true;
        }

        private void radButton10_Click(object sender, EventArgs e)
        {
            var arbName = this.arbitransUserName.Text;
            var arbKey = this.arbitransKey.Text;
            var isTest = this.providerIsTest.Checked;
            if(string.IsNullOrEmpty(arbName) || string.IsNullOrEmpty(arbKey))
            {
                MessageBox.Show(this, "Δεν εχετε ορισει σωστά τις παραμέτρους για την τιμολόγηση μέσω παρόχου", "Σφαλμα ρυθμίσεων...");
                return;
            }
            var otp = this.mellonGroupOtpTxt.Text;
            if (string.IsNullOrEmpty(otp))
            {
                MessageBox.Show(this, "Δεν εχετε ορισει OTP (Δειτε οδηγιες διασύνδεσης του POS)", "Σφαλμα ρυθμίσεων...");
                return;
            }
            var posType = this.posTypeCombo.SelectedValue.ToString();
            int nsp = Exedron.ProviderInvoicing.MellonGroupHelper.GetMellonNsp(posType);
            if (nsp <= 0)
            {
                MessageBox.Show(this, "Δεν ειναι σωστή η επιλογή πρωτοκόλλου", "Σφαλμα ρυθμίσεων...");
            }
            string apiKey = Exedron.ProviderInvoicing.MellonGroupHelper.ApiKeyGenerator(otp, arbName, arbKey, 1, isTest);
            this.mellonGroupApiKeyTxt.Text = apiKey;
        }

        private void radPageViewPage3_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}
