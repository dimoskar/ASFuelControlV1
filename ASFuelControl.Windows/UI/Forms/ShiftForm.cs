using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Telerik.WinControls.UI;

namespace ASFuelControl.Windows.UI.Forms
{
    public partial class ShiftForm : Telerik.WinControls.UI.ShapedForm
    {
        Data.DatabaseModel database = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
        private RadGridView paymentTotalsGrid;

        public ShiftForm()
        {
            InitializeComponent();

            this.radDropDownList1.DataSource = this.database.ApplicationUsers.Where(a => a.UserLevel >= 0).OrderByDescending(a=>a.UserLevel).ThenBy(a => a.UserName).ToArray();
            this.radDropDownList1.DisplayMember = "UserName";
            this.radDropDownList1.ValueMember = "ApplicationUserId";

            if (Program.CurrentShiftId != Guid.Empty)
            {
                Data.Shift currentShift = this.database.Shifts.Where(s => s.ShiftId == Program.CurrentShiftId).First();
                this.label1.Text = currentShift.ApplicationUser.UserName;
                this.ConfigureClosingLayout();
                this.panelClose.Visible = true;
                this.panelOpen.Visible = false;
            }
            else
            {
                this.Width = 382;
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

            Data.Shift shift = this.database.Shifts.Where(s => s.ShiftId == id).First();
            Data.Implementation.ShiftSalesTotals totals = Data.Implementation.ShiftSalesTotalsCalculator.Calculate(this.database, shift);
            this.ShowShiftTotals(totals);
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

            Data.Implementation.ShiftSalesTotals totals = Data.Implementation.ShiftSalesTotalsCalculator.Calculate(this.database, shift);

            bool sendMail = Data.Implementation.OptionHandler.Instance.GetBoolOption("MailSendShift", false);
            if (sendMail)
            {
                string name = Data.Implementation.OptionHandler.Instance.GetOption("CompanyName");
                string city = Data.Implementation.OptionHandler.Instance.GetOption("CompanyCity");
                string address = Data.Implementation.OptionHandler.Instance.GetOption("CompanyAddress");

                byte[] repData = Reports.ReportHelper.GetShiftReportData(shift, totals);
                string title = name + " - Αναφορά Βάρδιας : " + shift.ShiftEnd.Value.ToString("dd/MM/yyyy HH:mm");
                string subject = "Στοιχεία βάρδιας:";
                subject = subject + string.Format("\r\nΠρατήριο : {0}", name);
                subject = subject + string.Format("\r\nΠόλη : {0}", city);
                subject = subject + string.Format("\r\nΔιεύθυνση : {0}", address);
                subject = subject + string.Format("\r\n============================================");
                subject = subject + "\r\nΥπεύθυνος Βάρδιας : " + shift.ApplicationUser.UserName;
                subject = subject + string.Format("\r\nΈναρξη Βάρδιας : {0:dd/MM/yyyy HH:mm}", shift.ShiftBegin);
                subject = subject + string.Format("\r\nΛήξη Βάρδιας : {0:dd/MM/yyyy HH:mm}", shift.ShiftEnd);
                foreach (Data.Implementation.ShiftPaymentTotal paymentTotal in totals.PaymentTotals)
                {
                    subject = subject + string.Format("\r\n{0} : {1:N2}€", paymentTotal.Description, paymentTotal.Amount);
                }
                subject = subject + string.Format("\r\nΣυνολικό Ποσό : {0:N2}€", totals.GrandTotal);
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

        private void ConfigureClosingLayout()
        {
            this.Width = 520;
            this.Height = 415;
            this.panelClose.Width = 420;

            this.paymentTotalsGrid = new RadGridView();
            this.paymentTotalsGrid.Location = new Point(16, 68);
            this.paymentTotalsGrid.Size = new Size(388, 190);
            this.paymentTotalsGrid.ReadOnly = true;
            this.paymentTotalsGrid.ShowGroupPanel = false;
            this.paymentTotalsGrid.Visible = false;
            this.paymentTotalsGrid.MasterTemplate.AllowAddNewRow = false;
            this.paymentTotalsGrid.MasterTemplate.AllowDeleteRow = false;
            this.paymentTotalsGrid.MasterTemplate.AutoGenerateColumns = false;
            this.paymentTotalsGrid.MasterTemplate.AutoSizeColumnsMode = GridViewAutoSizeColumnsMode.Fill;
            this.paymentTotalsGrid.MasterTemplate.EnableFiltering = false;
            this.paymentTotalsGrid.MasterTemplate.EnableGrouping = false;
            this.paymentTotalsGrid.MasterTemplate.EnableSorting = false;
            this.paymentTotalsGrid.MasterTemplate.ShowRowHeaderColumn = false;

            GridViewTextBoxColumn descriptionColumn = new GridViewTextBoxColumn();
            descriptionColumn.FieldName = "Description";
            descriptionColumn.HeaderText = "Τρόπος πληρωμής";
            descriptionColumn.Name = "Description";
            descriptionColumn.ReadOnly = true;
            descriptionColumn.Width = 245;

            GridViewDecimalColumn amountColumn = new GridViewDecimalColumn();
            amountColumn.DataType = typeof(decimal);
            amountColumn.DecimalPlaces = 2;
            amountColumn.FieldName = "Amount";
            amountColumn.FormatString = "{0:N2} €";
            amountColumn.HeaderText = "Ποσό";
            amountColumn.Name = "Amount";
            amountColumn.ReadOnly = true;
            amountColumn.TextAlignment = ContentAlignment.MiddleRight;
            amountColumn.Width = 125;

            GridViewDataColumn[] columns = new GridViewDataColumn[]
            {
                descriptionColumn,
                amountColumn
            };
            this.paymentTotalsGrid.MasterTemplate.Columns.AddRange(columns);
            this.panelClose.Controls.Add(this.paymentTotalsGrid);

            this.label4.Location = new Point(16, 271);
            this.label4.Text = "Σύνολο";
            this.radSpinEditor1.Location = new Point(230, 267);
            this.radSpinEditor1.Size = new Size(174, 33);
            this.radButton1.Location = new Point(16, 320);
            this.radButton1.Size = new Size(245, 28);
            this.radButton3.Location = new Point(270, 320);
            this.radButton3.Size = new Size(134, 28);
        }

        private void ShowShiftTotals(Data.Implementation.ShiftSalesTotals totals)
        {
            this.paymentTotalsGrid.DataSource = totals.PaymentTotals;
            this.paymentTotalsGrid.Visible = true;
            this.radSpinEditor1.Value = totals.GrandTotal;
        }
    }
}
