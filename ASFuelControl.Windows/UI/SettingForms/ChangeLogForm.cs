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
    public partial class ChangeLogForm : RadForm
    {
        List<string> tablesToLog = new List<string>();
        Data.DatabaseModel database = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
        bool initialized = false;

        public ChangeLogForm()
        {
            InitializeComponent();

            this.tablesToLog.Add("");
            this.tablesToLog.Add("ApplicationUser");
            this.tablesToLog.Add("Balance");
            this.tablesToLog.Add("CommunicationController");
            this.tablesToLog.Add("Dispenser");
            this.tablesToLog.Add("DispenserSetting");
            this.tablesToLog.Add("FuelType");
            this.tablesToLog.Add("Invoice");
            this.tablesToLog.Add("InvoiceLine");
            this.tablesToLog.Add("InvoiceType");
            this.tablesToLog.Add("Nozzle");
            this.tablesToLog.Add("NozzleFlow");
            this.tablesToLog.Add("Option");
            this.tablesToLog.Add("SendLog");
            this.tablesToLog.Add("SystemEvent");
            this.tablesToLog.Add("SystemEventData");
            this.tablesToLog.Add("Tank");
            this.tablesToLog.Add("Titrimetry");
            this.tablesToLog.Add("TitrimetryLevel");

            this.radDropDownList1.DataSource = this.tablesToLog.OrderBy(s => s);

            List<Data.ApplicationUser> users = new List<Data.ApplicationUser>();
            Data.ApplicationUser user = new Data.ApplicationUser();
            user.UserName = "(Ολοι οι Χρήστες)";
            users.Add(user);
            users.AddRange(this.database.ApplicationUsers.OrderBy(a => a.UserName).ToArray());

            this.radDropDownList2.DataSource = users;
            this.radDropDownList2.DisplayMember = "UserName";
            this.radDropDownList2.ValueMember = "ApplicationUserId";

            this.radDateTimePicker1.Value = DateTime.Now.AddMonths(-1);
            this.radDateTimePicker2.Value = DateTime.Now;

            this.Load += new EventHandler(ChangeLogForm_Load);
            initialized = true;
        }

        private void LoadData()
        {
            if (!initialized)
                return;
            DateTime dt1 = this.radDateTimePicker1.Value;
            DateTime dt2 = this.radDateTimePicker2.Value;
            string user = "";
            
            var q = this.database.ChangeLogs.Where(c => c.DateTimeStamp <= dt2 && c.DateTimeStamp >= dt1);
            
            Guid userId = (Guid)this.radDropDownList2.SelectedValue;
            if (userId != Guid.Empty)
            {
                q = q.Where(c => c.ApplicationUserName == this.radDropDownList2.Text);
            }

            if (this.radDropDownList1.Text != "")
            {
                q = q.Where(c => c.TableName == this.radDropDownList1.Text);
            }
            this.changeLogBindingSource.DataSource = q.OrderByDescending(c => c.DateTimeStamp);
        }

        void ChangeLogForm_Load(object sender, EventArgs e)
        {
            this.LoadData();
        }

        private void DateTimeFilter_Changed(object sender, EventArgs e)
        {
            this.LoadData();
        }

        private void FilterSelection_Changed(object sender, Telerik.WinControls.UI.Data.PositionChangedEventArgs e)
        {
            this.LoadData();
        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            if (Telerik.WinControls.RadMessageBox.Show("Θέλετε να διαγραφεί το ιστορικό αλλαγών;", "Φιαγραφή ιστορικού", MessageBoxButtons.YesNo, Telerik.WinControls.RadMessageIcon.Exclamation) == System.Windows.Forms.DialogResult.No)
                return;
            this.database.Delete(this.database.ChangeLogs);
            this.database.SaveChanges();
            this.LoadData();
        }
    }
}
