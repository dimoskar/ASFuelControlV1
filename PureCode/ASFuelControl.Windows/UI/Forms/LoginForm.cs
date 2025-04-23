using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Linq;

namespace ASFuelControl.Windows.UI.Forms
{
    public partial class LoginForm : Telerik.WinControls.UI.ShapedForm
    {
        Data.DatabaseModel database = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);

        public bool InternalUse { set; get; }

        public LoginForm()
        {
            InitializeComponent();

            this.radDropDownList1.DataSource = this.database.ApplicationUsers;
            this.radDropDownList1.DisplayMember = "UserName";
            this.radDropDownList1.ValueMember = "ApplicationUserId";
        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            Guid id = (Guid)this.radDropDownList1.SelectedValue;

            Data.ApplicationUser user = this.database.ApplicationUsers.Where(u => u.ApplicationUserId == id).FirstOrDefault();
            if (user == null)
            {
                Telerik.WinControls.RadMessageBox.Show("Λάθος όνομα χρήστη ή κωδικού πρόσβασης", "Σφάλμα ταυτοποίησης", MessageBoxButtons.OK, Telerik.WinControls.RadMessageIcon.Info);
                this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
                return;
            }
            if (user.PasswordEnc != this.PasswordText.Text)
            {
                Telerik.WinControls.RadMessageBox.Show("Λάθος όνομα χρήστη ή κωδικού πρόσβασης", "Σφάλμα ταυτοποίησης", MessageBoxButtons.OK, Telerik.WinControls.RadMessageIcon.Error);
                this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
                return;
            }
            if (Program.CurrentUserId != Guid.Empty)
            {
                Data.ApplicationUser oldUser = this.database.ApplicationUsers.Where(u => u.ApplicationUserId == Program.CurrentUserId).First();
                Data.ApplicationUserLoggon oldLoggon = oldUser.ApplicationUserLoggons.Where(al => !al.LogoffTime.HasValue).FirstOrDefault();
                if (oldLoggon != null)
                    oldLoggon.LogoffTime = DateTime.Now;
            }

            Program.CurrentUserId = user.ApplicationUserId;
            Program.CurrentUserName = user.UserName;
            Program.CurrentUserLevel = (Common.Enumerators.ApplicationUserLevelEnum)user.UserLevel;

            Data.DatabaseModel.CurrentUserId = Program.CurrentUserId;

            Data.ApplicationUserLoggon loggon = new Data.ApplicationUserLoggon();// this.database.CreateEntity<Data.ApplicationUserLoggon>();
            loggon.ApplicationUserLoggonId = Guid.Empty;
            loggon.LoggonTime = DateTime.Now;
            loggon.ApplicationUserId = Program.CurrentUserId;
            this.database.SaveChanges();
            if (InternalUse)
            {
                this.Close();
                return;
            }

            this.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.Close();
            
        }
    }
}
