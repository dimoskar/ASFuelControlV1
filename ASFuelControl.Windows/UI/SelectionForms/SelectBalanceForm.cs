using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Telerik.WinControls.UI;

namespace ASFuelControl.Windows.UI.SelectionForms
{
    public partial class SelectBalanceForm : RadForm
    {
        Data.DatabaseModel database = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
        DateTime dateFrom = DateTime.Today.AddDays(-7);
        DateTime dateTo = DateTime.Today;

        public Guid SelectedBalance { set; get; }

        public SelectBalanceForm()
        {
            InitializeComponent();

            this.radDateTimePicker1.Value = dateFrom;
            this.radDateTimePicker2.Value = dateTo;

            this.radDateTimePicker1.ValueChanged += new EventHandler(Date_Changed);
            this.radDateTimePicker2.ValueChanged += new EventHandler(Date_Changed);

            this.balanceBindingSource.DataSource = database.Balances.Where(b => b.EndDate.Date <= this.dateTo.Date && b.EndDate.Date >= this.dateFrom.Date).OrderByDescending(b => b.EndDate);
            this.balanceBindingSource.ResetBindings(false);
        }

        private void Date_Changed(object sender, EventArgs e)
        {
            this.dateFrom = this.radDateTimePicker1.Value;
            this.dateTo = this.radDateTimePicker2.Value;

            this.balanceBindingSource.DataSource = database.Balances.Where(b => b.EndDate.Date <= this.dateTo.Date && b.EndDate.Date >= this.dateFrom.Date).OrderByDescending(b => b.EndDate);
            this.balanceBindingSource.ResetBindings(false);
        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            if (this.shiftRadGridView.CurrentRow == null)
                return;
            Data.Balance balance = this.shiftRadGridView.CurrentRow.DataBoundItem as Data.Balance;
            if (balance == null)
                return;
            this.SelectedBalance = balance.BalanceId;
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.Close();
        }

        private void radButton2_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Close();
        }
    }
}
