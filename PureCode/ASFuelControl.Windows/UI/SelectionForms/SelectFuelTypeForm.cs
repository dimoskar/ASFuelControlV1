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
    public partial class SelectFuelTypeForm : RadForm
    {
        private Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);

        public Guid SelectedFuelTypeId { set; get; }

        public SelectFuelTypeForm()
        {
            InitializeComponent();
        }

        private void SelectFuelTypeForm_Load(object sender, EventArgs e)
        {
            List<Data.FuelType> list = db.Tanks.Select(t => t.FuelType).Union(db.FuelTypes.Where(ft => ft.EnumeratorValue == 99)).OrderBy(f=>f.Name).ToList();
            this.fuelTypeBindingSource.DataSource = list;
            
            Data.FuelType fts = this.db.FuelTypes.Where(f=>f.FuelTypeId == this.SelectedFuelTypeId).FirstOrDefault();
            if (fts != null)
                this.fuelTypeBindingSource.Position = list.IndexOf(fts);
        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            if (this.fuelTypeRadGridView.CurrentRow == null)
                this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            else
            {
                Data.FuelType ft = this.fuelTypeRadGridView.CurrentRow.DataBoundItem as Data.FuelType;
                if (ft == null)
                    this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
                else
                {
                    this.SelectedFuelTypeId = ft.FuelTypeId;
                    this.DialogResult = System.Windows.Forms.DialogResult.OK;
                }
            }
        }

        private void radButton2_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
        }
    }
}
