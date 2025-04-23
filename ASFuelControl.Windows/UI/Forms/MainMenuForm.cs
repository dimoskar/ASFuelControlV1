using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Telerik.WinControls.UI;
using Telerik.WinControls;

namespace ASFuelControl.Windows.UI.Forms
{
    public partial class MainMenuForm : RadForm
    {
        private delegate void EnableButtonDelegate();

        Data.DatabaseModel database = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);

        public MainMenuForm()
        {
            InitializeComponent();
            this.LoadNozzles();
            this.radDateTimePicker1.Value = DateTime.Today;
            this.radDateTimePicker2.Value = DateTime.Now;
            this.radDateTimePicker4.Value = DateTime.Today;
            this.radDateTimePicker3.Value = DateTime.Now;
            this.radDateTimePicker6.Value = DateTime.Today;
            this.radDateTimePicker5.Value = DateTime.Now;
            this.radDateTimePicker8.Value = DateTime.Today;
            this.radDateTimePicker7.Value = DateTime.Now;
            this.radButton4.DataBindings.Add("Enabled", this.database, "HasChanges");
            this.database.Events.Changed += new Telerik.OpenAccess.ChangeEventHandler(Events_Changed);
            this.FormClosing += new FormClosingEventHandler(MainMenuForm_FormClosing);
            this.radPageView1.SelectedPage = this.radPageViewPage1;
            this.Disposed += MainMenuForm_Disposed;
        }

        private void MainMenuForm_Disposed(object sender, EventArgs e)
        {
            database.Dispose();
        }

        private void LoadNozzles()
        {
            this.nozzleBindingSource.DataSource = this.database.Nozzles.OrderBy(n => n.OfficialNozzleNumber).OrderBy(n => n.Dispenser.OfficialPumpNumber);
            this.nozzleBindingSource.ResetBindings(false);
        }

        private void LoadTanks()
        {
            try
            {
                this.tankBindingSource.DataSource = this.database.Tanks.OrderBy(t => t.TankNumber);
                this.tankBindingSource.ResetBindings(false);
            }
            catch
            {
            }
        }

        private void LoadTankChecks()
        {
            this.tankCheckBindingSource.DataSource = this.database.TankChecks.Where(t=>t.CheckDate < this.radDateTimePicker2.Value && t.CheckDate >= this.radDateTimePicker1.Value).OrderByDescending(t => t.CheckDate);
            this.tankCheckBindingSource.ResetBindings(false);
        }

        private void LoadTankFillings()
        {
            this.tankFillingBindingSource.DataSource = this.database.TankFillings.Where(t => t.TransactionTime <= this.radDateTimePicker3.Value && t.TransactionTime >= this.radDateTimePicker4.Value &&
                t.LevelEnd > t.LevelStart && t.InvoiceLines.Count > 0 && Math.Abs(t.VolumeNormalized - t.VolumeRealNormalized) > this.radSpinEditor1.Value).OrderByDescending(t => t.TransactionTime);
            this.tankFillingBindingSource.ResetBindings(false);
        }

        private void LoadPriceChanges()
        {
            this.fuelTypePriceBindingSource.DataSource = this.database.FuelTypePrices.Where(f => f.ChangeDate <= this.radDateTimePicker5.Value && f.ChangeDate >= this.radDateTimePicker6.Value).OrderByDescending(f => f.ChangeDate);
            this.fuelTypePriceBindingSource.ResetBindings(false);
        }

        private void LoadAlarms()
        {
            this.systemEventBindingSource.DataSource = this.database.SystemEvents.Where(f => f.EventDate <= this.radDateTimePicker7.Value && f.EventDate >= this.radDateTimePicker8.Value).OrderByDescending(f => f.EventDate);
            this.systemEventBindingSource.ResetBindings(false);
        }

        private void nozzleBindingSource_PositionChanged(object sender, EventArgs e)
        {
            this.nozzleFlowPanel.SuspendLayout();
            try
            {
                if (this.nozzleBindingSource.Position < 0)
                {
                    nozzleFlowPanel.Controls.Clear();
                    return;
                }
                Data.Nozzle nozzle = this.nozzleBindingSource.Current as Data.Nozzle;
                if (nozzle == null)
                    return;
                nozzleFlowPanel.Controls.Clear();
                foreach (Data.NozzleFlow nf in nozzle.NozzleFlows)
                {
                    
                    UI.Controls.NozzleFlowControl nfc = new Controls.NozzleFlowControl();
                    nfc.NozzleFlow = nf;
                    this.nozzleFlowPanel.Controls.Add(nfc);
                    nfc.Dock = DockStyle.Top;
                }
            }
            finally
            {
                this.nozzleFlowPanel.ResumeLayout(true);
                try
                {
                    System.GC.Collect();
                }
                catch { }
            }
        }

        private void radPageView1_SelectedPageChanged(object sender, EventArgs e)
        {
            if (this.radPageView1.SelectedPage == this.radPageViewPage2)
                this.LoadTanks();
            else if (this.radPageView1.SelectedPage == this.radPageViewPage3)
                this.LoadTankChecks();
            else if (this.radPageView1.SelectedPage == this.radPageViewPage4)
                this.LoadTankFillings();
            else if (this.radPageView1.SelectedPage == this.radPageViewPage5)
                this.LoadPriceChanges();
            else if (this.radPageView1.SelectedPage == this.radPageViewPage7)
                this.LoadPriceChanges();
             
        }

        private void radButton2_Click(object sender, EventArgs e)
        {
            this.LoadTankChecks();
        }

        private void radButton3_Click(object sender, EventArgs e)
        {
            this.LoadTankFillings();
        }

        private void radButton4_Click(object sender, EventArgs e)
        {
            this.database.SaveChanges();
            this.radButton4.DataBindings[0].ReadValue();
            this.radButton5.Enabled = true;
            foreach (Data.Tank tank in this.database.Tanks)
                Program.ApplicationMainForm.RefreshTankOrderData(tank.TankId, (tank.OrderLimit.HasValue ? tank.OrderLimit.Value : 0));
        }

        void Events_Changed(object sender, Telerik.OpenAccess.ChangeEventArgs e)
        {
            this.radButton4.DataBindings[0].ReadValue();
        }

        private void radButton5_Click(object sender, EventArgs e)
        {
            this.radButton5.Enabled = false;
            Program.ApplicationMainForm.ThreadControllerInstance.StopThreads(true);
            Program.ApplicationMainForm.ThreadControllerInstance.StartThreads();
            
        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        void MainMenuForm_FormClosing(object sender, FormClosingEventArgs e)
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

        private void radButton6_Click(object sender, EventArgs e)
        {
            this.LoadPriceChanges();
        }

        private void radButton7_Click(object sender, EventArgs e)
        {
            this.LoadAlarms();
        }

        private void radButton8_Click(object sender, EventArgs e)
        {
            Threads.PrintAgent.CloseSales();
        }

        private void radButton9_Click(object sender, EventArgs e)
        {
            this.radButton9.Enabled = false;
            System.Threading.Thread pt = new System.Threading.Thread(new System.Threading.ThreadStart(this.PrintBalanceThread));
            pt.Start();

        }

        private void EnablePrintBalanceButton()
        {
            this.radButton9.Enabled = true;

        }

        private void PrintBalanceThread()
        {
            try
            {
                Threads.PrintAgent agent = new Threads.PrintAgent();
                List<Data.Balance> balances = this.database.Balances.ToList();
                foreach (Data.Balance bal in balances)
                {
                    agent.PrintToPrinter(bal, false);
                    System.Threading.Thread.Sleep(1000);
                }
                this.Invoke(new EnableButtonDelegate(this.EnablePrintBalanceButton));
            }
            catch (Exception ex)
            {
                Logging.Logger.Instance.LogToFile("MainMenuForm::PrintBalanceThread", ex);
            }
        }

        private void radButton10_Click(object sender, EventArgs e)
        {
            try
            {
                using (SelectionForms.SelectFromToForm sftf = new SelectionForms.SelectFromToForm())
                {
                    sftf.StartDate = DateTime.Today.AddDays(-DateTime.Today.Day + 1).Date;
                    sftf.EndDate = DateTime.Now;

                    sftf.ShowDialog(this);
                    DateTime? dtFrom = sftf.StartDate;
                    DateTime? dtTo = sftf.EndDate;
                    if (!dtFrom.HasValue)
                    {
                        RadMessageBox.Show("Πρέπει να επιλέξετε ημερομηνία έναρξης", "Σφάλμα Επιλογής", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
                        return;
                    }
                    if (!dtTo.HasValue)
                    {
                        RadMessageBox.Show("Πρέπει να επιλέξετε ημερομηνία λήξης", "Σφάλμα Επιλογής", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
                        return;
                    }
                    if (dtFrom.Value.Date == dtTo.Value.Date)
                    {
                        RadMessageBox.Show("Η ημερομηνία έναρξης και λήξης πρέπει να είναι διαφορετικές", "Σφάλμα Επιλογής", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
                        return;
                    }
                    if (dtFrom.Value.Date > dtTo.Value.Date)
                    {
                        RadMessageBox.Show("Η ημερομηνία λήξης πρέπει να είναι μεταγενέστερη της ημερομηνίας έναρξης", "Σφάλμα Επιλογής", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
                        return;
                    }
                    if (dtFrom.Value.Year < 2014)
                    {
                        RadMessageBox.Show("Η ημερομηνία έναρξης δεν επιλέχτηκε σωστά", "Σφάλμα Επιλογής", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
                        return;
                    }
                    if (dtTo.Value.Year < 2014)
                    {
                        RadMessageBox.Show("Η ημερομηνία λήξης δεν επιλέχτηκε σωστά", "Σφάλμα Επιλογής", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
                        return;
                    }

                    Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
                    Data.Balance bal = Data.Balance.CreateBalance(dtFrom.Value, dtTo.Value, Threads.AlertChecker.Instance);
                    if (bal != null)
                    {
                        Threads.PrintAgent agent = new Threads.PrintAgent();
                        agent.PrintToPrinter(bal, false);
                        agent.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

        private void radButton11_Click(object sender, EventArgs e)
        {
            if (discountText1.Text != discountText2.Text)
            {
                RadMessageBox.Show("Η επιβεβαίωση του κωδικού δεν ταιριάζει με τον κωδικό που δώσατε", "Σφάλμα καταχώρησης κωδικού", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
                return;
            }
            Data.Implementation.OptionHandler.Instance.SetOption("[DiscountEnablePassword]", this.discountText1.Text);
        }
    }
}
