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
    public partial class AlertsForm : RadForm
    {
        Data.DatabaseModel database = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);

        public AlertsForm()
        {
            InitializeComponent();
            this.systemEventRadGridView.Visible = true;
        }

        public void LoadData()
        {
            
            this.systemEventBindingSource.DataSource = this.database.SystemEvents.OrderByDescending(se => se.EventDate);
            this.systemEventDatumBindingSource.DataSource = this.systemEventBindingSource;
            this.systemEventDatumBindingSource.DataMember = "SystemEventData";
            this.systemEventBindingSource.ResetBindings(false);
            this.systemEventDatumBindingSource.ResetBindings(false);
        }
    }
}
