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
    public partial class TankLevelHistory : RadForm
    {
        Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);

        public TankLevelHistory()
        {
            InitializeComponent();

            List<Data.Tank> tanks = new List<Data.Tank>();
            Data.Tank fooTank = new Data.Tank();
            fooTank.DescriptionExt = "(Ολες οι Δεξαμενές)";
            tanks.Add(fooTank);
            try
            {
                tanks.AddRange(this.db.Tanks.ToList().OrderBy(t => t.Description).ToArray());
            }
            catch(Exception ex)
            {
            }
            this.radDropDownList1.DataSource = tanks;
            this.radDropDownList1.DisplayMember = "DescriptionExt";
            this.radDropDownList1.ValueMember = "TankId";
            this.radDateTimePicker1.Value = DateTime.Now.AddDays(-1);
            this.radDateTimePicker2.Value = DateTime.Now;
        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            Data.DatabaseModel database = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
            Guid? id = this.radDropDownList1.SelectedValue as Guid?;
            DateTime dtFrom = this.radDateTimePicker1.Value;
            DateTime dtTo = this.radDateTimePicker2.Value;
            if (!id.HasValue || id.Value == Guid.Empty)
            {
                var q = database.TankChecks.Where(t => t.CheckDate <= dtTo && t.CheckDate >= dtFrom).OrderByDescending(t => t.CheckDate);
                this.tankCheckBindingSource.DataSource = q.ToList();

            }
            else
            {
                var q = database.TankChecks.Where(t => t.CheckDate <= dtTo && t.CheckDate >= dtFrom && t.TankId == id.Value).OrderByDescending(t => t.CheckDate);
                this.tankCheckBindingSource.DataSource = q.ToList();
            }
            this.tankCheckBindingSource.ResetBindings(false);
        }

        private void radDropDownList1_SelectedIndexChanged(object sender, Telerik.WinControls.UI.Data.PositionChangedEventArgs e)
        {
            Guid? id = this.radDropDownList1.SelectedValue as Guid?;
            if (!id.HasValue || id.Value == Guid.Empty)
            {
                this.tankCheckRadGridView.Columns["DiffColumn"].IsVisible = false;
                this.tankCheckRadGridView.Columns["DiffVolColumn"].IsVisible = false;
            }
            else
            {
                this.tankCheckRadGridView.Columns["DiffColumn"].IsVisible = true;
                this.tankCheckRadGridView.Columns["DiffVolColumn"].IsVisible = true;
            }
        }

        private void tankCheckRadGridView_CellFormatting(object sender, CellFormattingEventArgs e)
        {
            if(!e.Column.IsVisible)
                return;
            
            if (e.Column.Name == "DiffColumn")
            {
                if (e.RowIndex >= this.tankCheckRadGridView.Rows.Count - 1)
                {
                    decimal dif = 0;
                    if (dif.Equals(e.CellElement.Value))
                        return;
                    e.CellElement.Value = 0;
                    return;
                }
                try
                {
                    Data.TankCheck check = e.Row.DataBoundItem as Data.TankCheck;
                    Data.TankCheck previousCheck = this.tankCheckRadGridView.Rows[e.RowIndex + 1].DataBoundItem as Data.TankCheck;
                    if (check == null)
                        return;
                    decimal dif = check.TankLevel - previousCheck.TankLevel;
                    if (dif.Equals(e.CellElement.Value))
                        return;
                    e.CellElement.Value = dif;
                }
                catch
                {
                    decimal dif = -1;
                    if (dif.Equals(e.CellElement.Value))
                        return;
                    e.CellElement.Value = -1;
                }
            }
            else if (e.Column.Name == "DiffVolColumn")
            {
                if (e.RowIndex >= this.tankCheckRadGridView.Rows.Count - 1)
                {
                    decimal dif = 0;
                    if (dif.Equals(e.CellElement.Value))
                        return;
                    e.CellElement.Value = 0;
                    return;
                }
                try
                {
                    Data.TankCheck check = e.Row.DataBoundItem as Data.TankCheck;
                    Data.TankCheck previousCheck = this.tankCheckRadGridView.Rows[e.RowIndex + 1].DataBoundItem as Data.TankCheck;
                    if (check == null)
                        return;
                    decimal dif = check.Tank.GetTankVolume(check.TankLevel) - previousCheck.Tank.GetTankVolume(previousCheck.TankLevel);
                    if (dif.Equals(e.CellElement.Value))
                        return;
                    e.CellElement.Value = dif;
                }
                catch
                {
                    decimal dif = -1;
                    if (dif.Equals(e.CellElement.Value))
                        return;
                    e.CellElement.Value = -1;
                }

            }
            
            
        }
    }
}
