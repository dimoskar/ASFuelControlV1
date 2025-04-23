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
    public partial class PriceSettingsForm : RadForm
    {
        BindingList<Data.FuelType> fuelTypes;
        Data.DatabaseModel database = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
        public bool PriceChanged { set; get; }
        public PriceSettingsForm()
        {
            InitializeComponent();

            this.radButton1.DataBindings.Add(new System.Windows.Forms.Binding("Enabled", this.database, "DatabaseChanged", true));

            this.LoadData();
        }

        private void LoadData()
        {
            this.fuelTypes = new BindingList<Data.FuelType>(this.database.Tanks.Select(t=>t.FuelType).Distinct().OrderBy(ft => ft.Name).ToList());
            foreach(Data.FuelType ft in this.fuelTypes)
            {
                UI.Controls.PriceControl pc = new Controls.PriceControl();
                pc.CurrentFuelType = ft;
                pc.Margin = new Padding(10, 10, 10, 30);
                this.flowLayoutPanel1.Controls.Add(pc);
            }
        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            if (database.HasChanges)
                this.PriceChanged = true;
            this.database.SaveChanges();
        }

        private void PriceSettingsForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!this.database.DatabaseChanged)
                return;
            DialogResult res = Telerik.WinControls.RadMessageBox.Show("Θέλετε να αποθηκευτούν οι αλλαγές που κάνατε;", "Έγιναν αλλαγές", MessageBoxButtons.YesNoCancel, Telerik.WinControls.RadMessageIcon.Exclamation);
            if (res == System.Windows.Forms.DialogResult.No)
                return;
            else if (res == System.Windows.Forms.DialogResult.Yes)
            {
                if (database.HasChanges)
                    this.PriceChanged = true;
                this.database.SaveChanges();
                return;
            }
            e.Cancel = true;
        }
    }
}
