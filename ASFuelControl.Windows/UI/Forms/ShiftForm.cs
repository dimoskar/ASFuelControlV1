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
    public partial class ShiftForm : Telerik.WinControls.UI.ShapedForm
    {
        Data.DatabaseModel database = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);

        public ShiftForm()
        {
            InitializeComponent();

            this.radDropDownList1.DataSource = this.database.ApplicationUsers.Where(a => a.UserLevel >= 0).OrderByDescending(a=>a.UserLevel).ThenBy(a => a.UserName).ToArray();
            this.radDropDownList1.DisplayMember = "UserName";
            this.radDropDownList1.ValueMember = "ApplicationUserId";

            this.Width = 382;
            if (Program.CurrentShiftId != Guid.Empty)
            {
                Data.Shift currentShift = this.database.Shifts.Where(s => s.ShiftId == Program.CurrentShiftId).First();
                this.label1.Text = currentShift.ApplicationUser.UserName;
                //var qi = this.database.Invoices.Where
                //    (i =>
                //        i.TransactionDate >= currentShift.ShiftBegin &&
                //        (!i.InvoiceType.IsInternal.HasValue || !i.InvoiceType.IsInternal.Value) &&
                //        (!i.InvoiceType.TransactionSign.HasValue || i.InvoiceType.TransactionSign.Value == 1) &&
                //        i.InvoiceType.TransactionType == 0
                //    ).Select(i=>i.InvoiceId);
                //var qs = this.database.InvoiceLines.Where(i => qi.Contains(i.InvoiceId) && i.SalesTransaction != null).Select(i => i.SalesTransaction);
                //qs = qs.Where(s => !s.IsErrorResolving.HasValue || !s.IsErrorResolving.Value);
                ////var qs = this.database.SalesTransactions.Where(s => s.TransactionTimeStamp >= currentShift.ShiftBegin && (!s.IsErrorResolving.HasValue || !s.IsErrorResolving.Value));
                //decimal ShiftSum = qs.Sum(s => s.TotalPrice);

                decimal ShiftSum = 0;
                var qi = this.database.Invoices.Where(i => i.TransactionDate >= currentShift.ShiftBegin).ToArray();
                for(int i=0; i < qi.Length; i++)
                {
                    var invoice = qi[i];
                    if (!invoice.InvoiceType.DispenserType.HasValue || !invoice.InvoiceType.DispenserType.Value)
                        continue;
                    if (invoice.IsCanceled)
                        continue;
                    ShiftSum = ShiftSum + invoice.TotalAmountEx;
                }

                this.radSpinEditor1.Value = ShiftSum;
                this.panelClose.Visible = true;
                this.panelOpen.Visible = false;
            }
            else
            {
                this.panelClose.Visible = false;
                this.panelOpen.Visible = true;
            }
            this.Disposed += ShiftForm_Disposed;
        }

        private void ShiftForm_Disposed(object sender, EventArgs e)
        {
            this.database.Dispose();
        }

        private void PasswordText_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter)
                return;
            e.Handled = true;

            Guid id = Program.CurrentShiftId;
            

            Data.ApplicationUser user = this.database.Shifts.Where(s=>s.ShiftId == id).First().ApplicationUser;
            if (user == null)
            {
                Telerik.WinControls.RadMessageBox.Show("Λάθος όνομα χρήστη ή κωδικού πρόσβασης", "Σφάλμα ταυτοποίησης", MessageBoxButtons.OK, Telerik.WinControls.RadMessageIcon.Error);
                this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
                return;
            }
            if (user.PasswordEnc != this.PasswordText.Text)
            {
                Telerik.WinControls.RadMessageBox.Show("Λάθος όνομα χρήστη ή κωδικού πρόσβασης", "Σφάλμα ταυτοποίησης", MessageBoxButtons.OK, Telerik.WinControls.RadMessageIcon.Error);
                this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
                return;
            }
            //if (Program.CurrentUserId != Guid.Empty)
            //{
            //    Data.ApplicationUser oldUser = this.database.ApplicationUsers.Where(u => u.ApplicationUserId == Program.CurrentUserId).First();
            //    Data.ApplicationUserLoggon oldLoggon = oldUser.ApplicationUserLoggons.Where(al => !al.LogoffTime.HasValue).FirstOrDefault();
            //    if (oldLoggon != null)
            //        oldLoggon.LogoffTime = DateTime.Now;
            //}

            //Program.CurrentUserId = user.ApplicationUserId;
            //Program.CurrentUserLevel = (Common.Enumerators.ApplicationUserLevelEnum)user.UserLevel;

            //Data.DatabaseModel.CurrentUserId = Program.CurrentUserId;

            //Data.ApplicationUserLoggon loggon = new Data.ApplicationUserLoggon();// this.database.CreateEntity<Data.ApplicationUserLoggon>();
            //loggon.ApplicationUserLoggonId = Guid.Empty;
            //loggon.LoggonTime = DateTime.Now;
            //loggon.ApplicationUserId = Program.CurrentUserId;

            this.radSpinEditor1.Visible = true;
            this.radButton1.Visible = true;
            this.label4.Visible = true;
            //this.database.SaveChanges();
        }

        private void PasswordTextOpen_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter)
                return;
            e.Handled = true;

            Guid id = (Guid)this.radDropDownList1.SelectedValue;
            Data.ApplicationUser user = this.database.ApplicationUsers.Where(u => u.ApplicationUserId == id).FirstOrDefault();
            if (user == null)
            {
                Telerik.WinControls.RadMessageBox.Show("Λάθος όνομα χρήστη ή κωδικού πρόσβασης", "Σφάλμα ταυτοποίησης", MessageBoxButtons.OK, Telerik.WinControls.RadMessageIcon.Error);
                return;
            }
            if (user.PasswordEnc != this.PasswordTextOpen.Text)
            {
                Telerik.WinControls.RadMessageBox.Show("Λάθος όνομα χρήστη ή κωδικού πρόσβασης", "Σφάλμα ταυτοποίησης", MessageBoxButtons.OK, Telerik.WinControls.RadMessageIcon.Error);
                return;
            }
            this.radButton2.Show();
        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            Guid id = Program.CurrentShiftId;

            Data.Shift shift = this.database.Shifts.Where(s => s.ShiftId == id).First();
            shift.ShiftEnd = DateTime.Now;
            this.database.SaveChanges();

            bool sendMail = Data.Implementation.OptionHandler.Instance.GetBoolOption("MailSendShift", false);
            if (sendMail)
            {
                string name = Data.Implementation.OptionHandler.Instance.GetOption("CompanyName");
                string city = Data.Implementation.OptionHandler.Instance.GetOption("CompanyCity");
                string address = Data.Implementation.OptionHandler.Instance.GetOption("CompanyAddress");

                decimal ShiftSum = this.database.SalesTransactions.Where(s => s.TransactionTimeStamp >= shift.ShiftBegin && (!s.IsErrorResolving.HasValue || !s.IsErrorResolving.Value)).Sum(s => s.TotalPrice);
                byte[] repData = Reports.ReportHelper.GetShiftReportData(shift);
                string title = name + " - Αναφορά Βάρδιας : " + shift.ShiftEnd.Value.ToString("dd/MM/yyyy HH:mm");
                string subject = "Στοιχεία βάρδιας:";
                subject = subject + string.Format("\r\nΠρατήριο : {0}", name);
                subject = subject + string.Format("\r\nΠόλη : {0}", city);
                subject = subject + string.Format("\r\nΔιεύθυνση : {0}", address);
                subject = subject + string.Format("\r\n============================================");
                subject = subject + "\r\nΥπεύθυνος Βάρδιας : " + shift.ApplicationUser.UserName;
                subject = subject + string.Format("\r\nΈναρξη Βάρδιας : {0:dd/MM/yyyy HH:mm}", shift.ShiftBegin);
                subject = subject + string.Format("\r\nΛήξη Βάρδιας : {0:dd/MM/yyyy HH:mm}", shift.ShiftEnd);
                subject = subject + string.Format("\r\nΣυνολικό Ποσό : {0:N2}€", ShiftSum);
                Threads.MailSender.Instance.SendMail(repData, "Αναφορά Βάρδιας.pdf", title, subject);
            }
            Program.CurrentShiftId = Guid.Empty;
            Program.CurrentUserName = "";
            this.Close();
        }

        private void radButton2_Click(object sender, EventArgs e)
        {
            Guid id = (Guid)this.radDropDownList1.SelectedValue;
            Data.ApplicationUser user = this.database.ApplicationUsers.Where(u => u.ApplicationUserId == id).FirstOrDefault();
            if (user == null)
            {
                Telerik.WinControls.RadMessageBox.Show("Λάθος όνομα χρήστη ή κωδικού πρόσβασης", "Σφάλμα ταυτοποίησης", MessageBoxButtons.OK, Telerik.WinControls.RadMessageIcon.Error);
                return;
            }

            Data.Shift newShift = new Data.Shift();
            newShift.ShiftId = Guid.NewGuid();
            newShift.ApplicationUserId = id;
            newShift.ShiftBegin = DateTime.Now;

            

            this.database.Add(newShift);
            this.database.SaveChanges();
            Program.CurrentShiftId = newShift.ShiftId;

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

            this.Close();
        }

        

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
