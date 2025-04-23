using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ASFuelControl.Windows.UI.Forms
{
    public partial class UserVerificationForm : Telerik.WinControls.UI.ShapedForm
    {
        Data.DatabaseModel database = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
        Data.ApplicationUser currentUser = null;

        public Common.Enumerators.ApplicationUserLevelEnum CurrentUserLevel
        {
            get 
            {
                if (this.currentUser == null)
                    return Common.Enumerators.ApplicationUserLevelEnum.None;
                return (Common.Enumerators.ApplicationUserLevelEnum)this.currentUser.UserLevel; 
            }
        }

        public Guid CurrentUserId
        {
            get
            {
                if (this.currentUser == null)
                    return Guid.Empty;
                return this.currentUser.ApplicationUserId;
            }
        }

        public UserVerificationForm()
        {
            InitializeComponent();
            this.radDropDownList1.DataSource = this.database.ApplicationUsers.Where(a => a.UserLevel >= 0).OrderByDescending(a => a.UserLevel).ThenBy(a => a.UserName).ToArray();
            this.radDropDownList1.DisplayMember = "UserName";
            this.radDropDownList1.ValueMember = "ApplicationUserId";
            this.radButton1.Enabled = false;
        }

        private void PasswordText_KeyUp(object sender, KeyEventArgs e)
        {
            
        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void radButton2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void PasswordText_Leave(object sender, EventArgs e)
        {
            //this.radButton1.Enabled = false;
            //Guid id = (Guid)this.radDropDownList1.SelectedValue;
            //Data.ApplicationUser user = this.database.ApplicationUsers.Where(u => u.ApplicationUserId == id).FirstOrDefault();
            //if (user == null)
            //{
            //    MessageBox.Show("Λάθος όνομα χρήστη ή κωδικού πρόσβασης", "Σφάλμα ταυτοποίησης", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //    return;
            //}
            //if (user.PasswordEnc != this.PasswordText.Text)
            //{
            //    MessageBox.Show("Λάθος όνομα χρήστη ή κωδικού πρόσβασης", "Σφάλμα ταυτοποίησης", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //    return;
            //}
            //this.currentUser = user;
            //this.radButton1.Enabled = true;
            //this.Close();
        }

        private void PasswordText_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode != Keys.Enter)
                return;
            e.Handled = true;

            this.radButton1.Enabled = false;
            Guid id = (Guid)this.radDropDownList1.SelectedValue;
            Data.ApplicationUser user = this.database.ApplicationUsers.Where(u => u.ApplicationUserId == id).FirstOrDefault();
            if(user == null)
            {
                Telerik.WinControls.RadMessageBox.Show(this, "Λάθος όνομα χρήστη ή κωδικού πρόσβασης", "Σφάλμα ταυτοποίησης", MessageBoxButtons.OK, Telerik.WinControls.RadMessageIcon.Error);
                return;
            }
            if(user.PasswordEnc != this.PasswordText.Text)
            {
                Telerik.WinControls.RadMessageBox.Show(this, "Λάθος όνομα χρήστη ή κωδικού πρόσβασης", "Σφάλμα ταυτοποίησης", MessageBoxButtons.OK, Telerik.WinControls.RadMessageIcon.Error);
                return;
            }
            this.currentUser = user;
            this.radButton1.Enabled = true;
            this.Close();

        }
    }
}
