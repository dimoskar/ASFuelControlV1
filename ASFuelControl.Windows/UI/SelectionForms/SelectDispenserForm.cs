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
    public partial class SelectDispenserForm : RadForm
    {
        public Guid SelectedDispenser
        {
            private set;
            get;
        }

        Data.DatabaseModel database = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
        public SelectDispenserForm()
        {
            InitializeComponent();

            try
            {
                this.dispenserBindingSource.DataSource = this.database.Dispensers.ToList().OrderBy(d => d.DescriptionExt);
            }
            catch
            {
            }
        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            if (this.dispenserBindingSource.Position < 0)
                return;
            Data.Dispenser dispenser = this.dispenserBindingSource.Current as Data.Dispenser;
            if(dispenser == null)
                return;
            this.SelectedDispenser = dispenser.DispenserId;
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
        }

        private void radButton2_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        }
    }
}
