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
    public partial class SelectFromToForm : RadForm
    {
        private DateTime? dtFrom;
        private DateTime? dtTo;

        public DateTime? StartDate 
        {
            set 
            { 
                this.dtFrom = value;
                if (dtFrom.HasValue)
                    this.radDateTimePicker1.Value = dtFrom.Value;
            }
            get { return this.dtFrom; }
        }
        public DateTime? EndDate 
        {
            set 
            { 
                this.dtTo = value;
                if (dtTo.HasValue)
                    this.radDateTimePicker2.Value = dtTo.Value;
            }
            get { return this.dtTo; }
        }

        public SelectFromToForm()
        {
            InitializeComponent();
        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            this.StartDate = this.radDateTimePicker1.Value;
            this.EndDate = this.radDateTimePicker2.Value;
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
        }

        private void radButton2_Click(object sender, EventArgs e)
        {
            this.StartDate = null;
            this.EndDate = null;
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        }
    }
}
