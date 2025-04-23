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
    public partial class UserSettingsForm : RadForm
    {
        private Data.DatabaseModel database = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
        BindingList<Data.ApplicationUser> applicationUsers;

        public bool LimitedView { set; get; }

        public UserSettingsForm()
        {
            InitializeComponent();

            

            this.btnSave.DataBindings.Add(new System.Windows.Forms.Binding("Enabled", this.database, "DatabaseChanged", true));
        }

        public void LoadData()
        {
            if (this.LimitedView)
            {
                applicationUsers = new BindingList<Data.ApplicationUser>(this.database.ApplicationUsers.Where(au=>au.UserLevel != 0).OrderBy(au => au.UserName).ToList());
                this.applicationUserBindingSource.DataSource = applicationUsers;

                List<Common.Enumerators.EnumItem> list = Common.Enumerators.EnumToList.EnumList<Common.Enumerators.ApplicationUserLevelEnum>();
                this.radDropDownList1.DataSource = list.Where(l => l.Value <= 2 && l.Value >= 1);
                this.radDropDownList1.DisplayMember = "Description";
                this.radDropDownList1.ValueMember = "Value";
            }
            else
            {
                applicationUsers = new BindingList<Data.ApplicationUser>(this.database.ApplicationUsers.OrderBy(au => au.UserName ).ToList());
                this.applicationUserBindingSource.DataSource = applicationUsers;

                List<Common.Enumerators.EnumItem> list = Common.Enumerators.EnumToList.EnumList<Common.Enumerators.ApplicationUserLevelEnum>();
                this.radDropDownList1.DataSource = list.Where(l => l.Value <= 2 && l.Value >= 0);
                this.radDropDownList1.DisplayMember = "Description";
                this.radDropDownList1.ValueMember = "Value";
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            Data.ApplicationUser newUser = new Data.ApplicationUser();
            newUser.ApplicationUserId = Guid.NewGuid();
            newUser.UserLevel = (int)Common.Enumerators.ApplicationUserLevelEnum.User;
            newUser.UserName = "(Νέος Χρήστης)";
            
            this.database.Add(newUser);
            this.applicationUserBindingSource.SuspendBinding();
            this.applicationUsers.Add(newUser);
            this.applicationUserBindingSource.ResumeBinding();
            this.applicationUserBindingSource.Position = this.applicationUsers.IndexOf(newUser);

        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            this.database.SaveChanges();
        }

        private void UserSettingsForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!this.database.DatabaseChanged)
                return;
            DialogResult res = Telerik.WinControls.RadMessageBox.Show("Θέλετε να αποθηκευτούν οι αλλαγές που κάνατε;", "Έγιναν αλλαγές", MessageBoxButtons.YesNoCancel, Telerik.WinControls.RadMessageIcon.Exclamation);
            if (res == System.Windows.Forms.DialogResult.No)
                return;
            else if (res == System.Windows.Forms.DialogResult.Yes)
            {
                this.database.SaveChanges();
                return;
            }
            e.Cancel = true;
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            Data.ApplicationUser user = this.applicationUserBindingSource.Current as Data.ApplicationUser;
            if (user == null)
                return;
            if (user.ApplicationUserLoggons.Count > 0)
            {
                return;
            }
            if (user.Balances.Count > 0)
            {
                return;
            }
            if (user.Invoices.Count > 0)
            {
                return;
            }
            if (user.SalesTransactions.Count > 0)
            {
                return;
            }
            if (user.Shifts.Count > 0)
            {
                return;
            }
            if (user.TankFillings.Count > 0)
            {
                return;
            }
            this.applicationUsers.Remove(user);
            this.database.Delete(user);
            
        }
    }
}
