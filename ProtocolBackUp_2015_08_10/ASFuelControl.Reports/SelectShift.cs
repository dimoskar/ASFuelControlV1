using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Telerik.WinControls.UI;

namespace ASFuelControl.Reports
{
    public partial class SelectShift : RadForm
    {
        Data.DatabaseModel database;
        DateTime dateFrom = DateTime.Today.AddDays(-7);
        DateTime dateTo = DateTime.Today;

        public Guid SelectedShift { set; get; }

        public SelectShift(string dBConnectionString)
        {
            InitializeComponent();

            this.database = new Data.DatabaseModel(dBConnectionString);

            this.radDateTimePicker1.Value = dateFrom;
            this.radDateTimePicker2.Value = dateTo;

            this.radDateTimePicker1.ValueChanged += new EventHandler(Date_Changed);
            this.radDateTimePicker2.ValueChanged += new EventHandler(Date_Changed);

            this.shiftBindingSource.DataSource = database.Shifts.Where(s => s.ShiftBegin.Date <= this.dateTo.Date && s.ShiftBegin >= this.dateFrom.Date).OrderByDescending(s => s.ShiftBegin);
            this.shiftBindingSource.ResetBindings(false);

        }

        private void Date_Changed(object sender, EventArgs e)
        {
            this.dateFrom = this.radDateTimePicker1.Value;
            this.dateTo = this.radDateTimePicker2.Value;

            this.shiftBindingSource.DataSource = database.Shifts.Where(s => s.ShiftBegin.Date <= this.dateTo.Date && s.ShiftBegin.Date >= this.dateFrom.Date).OrderByDescending(s => s.ShiftBegin);
            this.shiftBindingSource.ResetBindings(false);
        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            if (this.shiftRadGridView.CurrentRow == null)
                return;
            Data.Shift shift = this.shiftRadGridView.CurrentRow.DataBoundItem as Data.Shift;
            if (shift == null)
                return;
            this.SelectedShift = shift.ShiftId;
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
