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
    public partial class SendHistoryForm : RadForm
    {
        Data.DatabaseModel database = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);

        public SendHistoryForm()
        {
            InitializeComponent();
            this.radDateTimePicker1.Value = DateTime.Today.AddDays(-7);
            this.radDateTimePicker2.Value = DateTime.Today;
            this.Disposed += SendHistoryForm_Disposed;
        }

        private void SendHistoryForm_Disposed(object sender, EventArgs e)
        {
            database.Dispose();
        }

        private void radButton3_Click(object sender, EventArgs e)
        {
            var q = this.database.SendLogs.Where(s => s.LastSent.Value.Date <= this.radDateTimePicker2.Value && s.LastSent.Value.Date >= this.radDateTimePicker1.Value);
            this.database.Refresh(Telerik.OpenAccess.RefreshMode.OverwriteChangesFromStore, q);
            this.sendLogBindingSource.DataSource = q;
            this.sendLogBindingSource.ResetBindings(false);
        }

        private void radButton2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void sendLogRadGridView_CommandCellClick(object sender, EventArgs e)
        {
            var cell = sender as Telerik.WinControls.UI.GridCommandCellElement;
            if (cell == null)
                return;
            if (cell.CommandButton.Text != "Ακύρωση Αποστολής")
                return;
            if (this.sendLogRadGridView.CurrentRow == null)
                return;
            try
            {
                var rowData = cell.RowElement.Data.DataBoundItem as Data.SendLog;
                object relatedData = null;
                switch(rowData.Action)
                {
                    case "SystemEvent":
                        var se = database.SystemEvents.SingleOrDefault(s => s.EventId.ToString().ToLower() == rowData.EntityIdentity.ToLower());
                        if (se != null)
                            se.SentDate = new DateTime(1900,01,01);
                        relatedData = se;
                        break;
                    case "TankFilling":
                        var tf = database.TankFillings.SingleOrDefault(s => s.TankFillingId.ToString().ToLower() == rowData.EntityIdentity.ToLower());
                        if (tf != null)
                            tf.SentDateTime = new DateTime(1900, 01, 01);
                        relatedData = tf;
                        break;
                    case "SalesTransaction":
                        var sale = database.SalesTransactions.SingleOrDefault(s => s.SalesTransactionId.ToString().ToLower() == rowData.EntityIdentity.ToLower());
                        if (sale != null)
                            sale.SentDateTime = new DateTime(1900, 01, 01);
                        relatedData = sale;
                        break;
                    case "FuelTypePrice":
                        var fp = database.FuelTypePrices.SingleOrDefault(s => s.FuelTypePriceId.ToString().ToLower() == rowData.EntityIdentity.ToLower());
                        if (fp != null)
                            fp.SentDateTime = new DateTime(1900, 01, 01);
                        relatedData = fp;
                        break;
                    case "ChangePriceClass":
                        //var fp = database.cha.SingleOrDefault(s => s.FuelTypePriceId.ToString().ToLower() == rowData.EntityIdentity.ToLower());
                        //if (fp != null)
                        //    fp.SentDateTime = new DateTime(1900, 01, 01);
                        //Communication.ChangePriceClass cpc = sendObject as Communication.ChangePriceClass;
                        break;
                    case "IncomeRecieptClass":
                        //Communication.IncomeRecieptClass irc = sendObject as Communication.IncomeRecieptClass;
                        break;
                    case "BalanceClass":
                        var bc = database.Balances.SingleOrDefault(b=>b.BalanceId.ToString().ToLower() == rowData.EntityIdentity.ToLower());
                        if(bc != null)
                            bc.SentDateTime = new DateTime(1900, 01, 01);
                        relatedData = bc;
                        break;
                    case "Balance":
                        var bc1 = database.Balances.SingleOrDefault(b => b.BalanceId.ToString().ToLower() == rowData.EntityIdentity.ToLower());
                        if (bc1 != null)
                            bc1.SentDateTime = new DateTime(1900, 01, 01);
                        relatedData = bc1;
                        break;
                }
                rowData.SentStatus = 1;
                database.SaveChanges();
                Program.ApplicationMainForm.SimulateSendSuccess(relatedData);
            }
            catch
            {
            }
        }

        private void sendLogRadGridView_CellFormatting(object sender, CellFormattingEventArgs e)
        {
            if(e.ColumnIndex == 5)
            {
                var rowData = e.Row.DataBoundItem as Data.SendLog;
                if (rowData == null)
                    return;
                if (rowData.SentStatus == 1 || !Program.AdminConnected)
                    e.CellElement.Visibility = Telerik.WinControls.ElementVisibility.Hidden;
            }
        }
    }
}
