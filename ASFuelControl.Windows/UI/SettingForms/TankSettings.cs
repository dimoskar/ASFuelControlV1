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
    public partial class TankSettings : RadForm
    {
        BindingList<Data.Tank> tanks;
        Data.DatabaseModel database = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);

        public TankSettings()
        {
            InitializeComponent();

            if (this.DesignMode)
                return;

            this.Width = 920;

            tanks = new BindingList<Data.Tank>(this.database.Tanks.ToList().OrderBy(d => d.Description).ToList());

            tanks.ListChanged += new ListChangedEventHandler(tanks_ListChanged);

            this.alarmDelayTime.DataBindings.Add(new System.Windows.Forms.Binding("Value", this.tankBindingSource, "AlarmDelayTime", true));
            this.literCheckAlarmInterval.DataBindings.Add(new System.Windows.Forms.Binding("Value", this.tankBindingSource, "LiterCheckAlarmInterval", true));
            this.fuelLevelRadSpinEditor.DataBindings.Add(new System.Windows.Forms.Binding("Value", this.tankBindingSource, "FuelLevel", true));
            this.fuelLevelRadSpinEditor.DataBindings.Add(new System.Windows.Forms.Binding("ReadOnly", this.tankBindingSource, "IsRealTank", true));
            this.addressRadSpinEditor.DataBindings.Add(new System.Windows.Forms.Binding("Value", this.tankBindingSource, "Address", true));
            this.channelRadSpinEditor.DataBindings.Add(new System.Windows.Forms.Binding("Value", this.tankBindingSource, "Channel", true));
            this.currentDensityRadSpinEditor.DataBindings.Add(new System.Windows.Forms.Binding("Value", this.tankBindingSource, "CurrentDensity", true));
            this.currentNettoFuelNomalizedVolumeRadSpinEditor.DataBindings.Add(new System.Windows.Forms.Binding("Value", this.tankBindingSource, "CurrentNettoFuelNomalizedVolume", true));
            this.lastNettoFuelNormalizedVolumeRadSpinEditor.DataBindings.Add(new System.Windows.Forms.Binding("Value", this.tankBindingSource, "LastNettoFuelNormalizedVolume", true));
            this.lastValidLevelRadSpinEditor.DataBindings.Add(new System.Windows.Forms.Binding("Value", this.tankBindingSource, "LastValidLevel", true));
            this.maxFuelHeightRadSpinEditor.DataBindings.Add(new System.Windows.Forms.Binding("Value", this.tankBindingSource, "MaxFuelHeight", true));
            this.maxWaterHeightRadSpinEditor.DataBindings.Add(new System.Windows.Forms.Binding("Value", this.tankBindingSource, "MaxWaterHeight", true));
            this.minFuelHeightRadSpinEditor.DataBindings.Add(new System.Windows.Forms.Binding("Value", this.tankBindingSource, "MinFuelHeight", true));
            this.offsetVolumeRadSpinEditor.DataBindings.Add(new System.Windows.Forms.Binding("Value", this.tankBindingSource, "OffsetVolume", true));
            this.offsetWaterVolumeRadSpinEditor.DataBindings.Add(new System.Windows.Forms.Binding("Value", this.tankBindingSource, "OffestWater", true));
            this.tankNumberNumericUpDown.DataBindings.Add(new System.Windows.Forms.Binding("Value", this.tankBindingSource, "TankNumber", true));
            this.tankSerialNumberRadTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.tankBindingSource, "TankSerialNumber", true));
            this.temperatireRadSpinEditor.DataBindings.Add(new System.Windows.Forms.Binding("Value", this.tankBindingSource, "Temperatire", true));
            this.totalVolumeRadSpinEditor.DataBindings.Add(new System.Windows.Forms.Binding("Value", this.tankBindingSource, "TotalVolume", true));
            this.waterLevelRadSpinEditor.DataBindings.Add(new System.Windows.Forms.Binding("Value", this.tankBindingSource, "WaterLevel", true));
            this.orderLimitSpinButton.DataBindings.Add(new System.Windows.Forms.Binding("Value", this.tankBindingSource, "OrderLimit", true));
            this.radCheckBox1.DataBindings.Add(new System.Windows.Forms.Binding("Checked", this.tankBindingSource, "IsVirtual", true));
            this.removedCheckBox.DataBindings.Add(new System.Windows.Forms.Binding("Checked", this.tankBindingSource, "Removed", true));
            

            this.alarmThresholdSpin.DataBindings.Clear();
            this.alarmThresholdSpin.DataBindings.Add(new System.Windows.Forms.Binding("Value", this.tankBindingSource, "AlarmThreshold", true));
            this.gaugeMinValue.DataBindings.Add(new System.Windows.Forms.Binding("Value", this.tankBindingSource, "GaugeMinimumHeight", true));
            

            this.radTreeView1.DataSource = tanks;
            if (tanks.Count == 0)
            {
                this.radTreeView1.DisplayMember = "Description";
                this.radTreeView1.ValueMember = "TankId";
                this.radTreeView1.ChildMember = "Tanks";
            }
            else
            {
                this.radTreeView1.DisplayMember = "Description\\TitrationDate";
                this.radTreeView1.ValueMember = "TankId\\TitrimetryId";
                this.radTreeView1.ChildMember = "Tanks\\Titrimetries";
            }

            this.panelTank.Dock = DockStyle.Fill;
            this.panelTitrimetry.Dock = DockStyle.Fill;

            this.atgTypeDropDownList.DataBindings.Add(new System.Windows.Forms.Binding("SelectedValue", this.tankBindingSource, "AtgProbeTypeId", true));
            this.atgTypeDropDownList.DataSource = database.AtgProbeTypes.OrderBy(a => a.BrandName);
            this.atgTypeDropDownList.DisplayMember = "BrandName";
            this.atgTypeDropDownList.ValueMember = "AtgProbeTypeId";

            this.fuelTypeDropDownList.DataBindings.Add(new System.Windows.Forms.Binding("SelectedValue", this.tankBindingSource, "FuelTypeId", true));
            this.fuelTypeDropDownList.DataSource = database.FuelTypes.OrderBy(f => f.Name);
            this.fuelTypeDropDownList.DisplayMember = "Name";
            this.fuelTypeDropDownList.ValueMember = "FuelTypeId";

            this.radButton1.DataBindings.Add(new System.Windows.Forms.Binding("Enabled", this.database, "DatabaseChanged", true));

            this.controllerDropDownList.DataBindings.Add(new System.Windows.Forms.Binding("SelectedValue", this.tankBindingSource, "CommunicationControllerId", true));
            this.controllerDropDownList.DataSource = this.database.CommunicationControllers.OrderBy(cc => cc.Name);
            this.controllerDropDownList.DisplayMember = "Name";
            this.controllerDropDownList.ValueMember = "CommunicationControllerId";
            string efk = Data.Implementation.OptionHandler.Instance.GetOption("CompanyEFK");
            currentDensityRadSpinEditor.ReadOnly = efk == null || efk == "";
            this.Disposed += TankSettings_Disposed;
        }

        private void TankSettings_Disposed(object sender, EventArgs e)
        {
            this.database.Dispose();
        }

        private decimal CalculateMaxLevel(Data.Titrimetry currentTitrimetry)
        {
            decimal? v = (decimal)0.95 * currentTitrimetry.TitrimetryLevels.Max(v1 => v1.Volume);
            decimal vv = v.HasValue ? v.Value : 0;
            Data.TitrimetryLevel maxLevel = currentTitrimetry.TitrimetryLevels.OrderBy(l => l.Height).Where(l => l.Volume <= vv).LastOrDefault();
            return maxLevel.Height.Value;
        }

        void tanks_ListChanged(object sender, ListChangedEventArgs e)
        {
            BindingList<Data.Tank> list = sender as BindingList<Data.Tank>;
            if (list.Count == 0)
            {
                this.radTreeView1.DisplayMember = "Description";
                this.radTreeView1.ValueMember = "TankId";
                this.radTreeView1.ChildMember = "Tanks";

            }
            else
            {
                this.radTreeView1.DisplayMember = "Description\\TitrationDate";
                this.radTreeView1.ValueMember = "TankId\\TitrimetryId";
                this.radTreeView1.ChildMember = "Tanks\\Titrimetries";
            }
        }

        private void radTreeView1_SelectedNodeChanged(object sender, RadTreeViewEventArgs e)
        {
            if (e.Node == null)
                return;
            Data.Tank tank = e.Node.DataBoundItem as Data.Tank;
            Data.Titrimetry titrimetry = e.Node.DataBoundItem as Data.Titrimetry;
            if (tank != null)
            {
                this.tankBindingSource.DataSource = tank;
                this.tankBindingSource.ResetBindings(false);
                this.panelTank.Show();
                this.panelTitrimetry.Hide();
            }
            else if (titrimetry != null)
            {
                this.tankBindingSource.DataSource = titrimetry.Tank;
                this.tankBindingSource.ResetBindings(false);
                this.titrimetryBindingSource.DataSource = titrimetry;
                this.titrimetryLevelsBindingSource.DataSource = new BindingList<Data.TitrimetryLevel>(titrimetry.TitrimetryLevels.OrderBy(tl=>tl.Height).ToList());
                this.panelTank.Hide();
                this.panelTitrimetry.Show();
            }
        }

        private void panelTank_Paint(object sender, PaintEventArgs e)
        {

        }

        private void radButton4_Click(object sender, EventArgs e)
        {
            Data.Titrimetry currentTitrimetry = this.titrimetryBindingSource.Current as Data.Titrimetry;
            if (currentTitrimetry == null)
                return;

            this.database.Delete(currentTitrimetry.TitrimetryLevels);
            currentTitrimetry.TitrimetryLevels.Clear();

            OpenFileDialog dlg = new OpenFileDialog();
            dlg.DefaultExt = ".txt"; // Default file extension 
            dlg.Filter = "Text documents (.txt)|*.txt"; // Filter files by extension 
            DialogResult res = dlg.ShowDialog();
            if(res == System.Windows.Forms.DialogResult.Cancel)
                return;

            string fileName = dlg.FileName;
            string[] lines = System.IO.File.ReadAllLines(fileName);
            BindingList<Data.TitrimetryLevel> newList = new BindingList<Data.TitrimetryLevel>();
            foreach (string line in lines)
            {
                string[] entries = line.Split('\t');
                if (entries.Length < 2)
                    continue;
                try
                {
                    string sep = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
                    string sep2 = " ";
                    bool hasBoth0 = entries[0].Contains(",") && entries[0].Contains(".");
                    bool hasBoth1 = entries[1].Contains(",") && entries[1].Contains(".");
                    if(hasBoth0)
                    {
                        if (entries[0].IndexOf(",") > entries[0].IndexOf("."))
                        {
                            sep2 = ".";
                        }
                        else if (entries[0].IndexOf(",") > 0)
                        {
                            sep2 = ",";
                        }
                    }
                    if (hasBoth1)
                    {
                        if (entries[1].IndexOf(",") > entries[1].IndexOf("."))
                        {
                            sep2 = ".";
                        }
                        else if(entries[1].IndexOf(",") > 0)
                        {
                            sep2 = ",";
                        }
                    }
                    decimal height = decimal.Parse(entries[0].Replace(sep2, "").Replace(".", sep));
                    decimal volume = decimal.Parse(entries[1].Replace(sep2, "").Replace(".", sep));
                    decimal uncertaintyVolume = 0;
                    decimal uncertaintyPercentage = 0;
                    if (entries.Length == 4)
                    {
                        uncertaintyVolume = decimal.Parse(entries[2].Replace(",", sep).Replace(".", sep));
                        uncertaintyPercentage = decimal.Parse(entries[3].Replace(",", sep).Replace(".", sep));
                    }
                    Data.TitrimetryLevel newLevel = new Data.TitrimetryLevel();
                    newLevel.TitrimetryLevelId = Guid.NewGuid();
                    newLevel.TitrimetryId = currentTitrimetry.TitrimetryId;
                    newLevel.Titrimetry = currentTitrimetry;
                    newLevel.Height = height;
                    newLevel.Volume = volume;
                    newLevel.UncertaintyVolume = uncertaintyVolume;
                    newLevel.UncertaintyPercent = uncertaintyPercentage;
                    currentTitrimetry.TitrimetryLevels.Add(newLevel);
                }
                catch
                {
                    continue;
                }
            }
            this.titrimetryLevelsBindingSource.DataSource = new BindingList<Data.TitrimetryLevel>(currentTitrimetry.TitrimetryLevels);
            if (currentTitrimetry.TitrimetryLevels.Count > 0)
            {
                currentTitrimetry.Tank.TotalVolume = currentTitrimetry.TitrimetryLevels.Max(t => t.Volume).Value;

                //decimal v = (decimal)0.95 * currentTitrimetry.Tank.TotalVolume;
                //Data.TitrimetryLevel maxLevel = currentTitrimetry.TitrimetryLevels.OrderBy(l => l.Height).Where(l => l.Volume <= v).LastOrDefault();

                //currentTitrimetry.Tank.MaxFuelHeight = maxLevel.Height.Value;// (decimal)0.95 * currentTitrimetry.TitrimetryLevels.Max(t => t.Height).Value;
                currentTitrimetry.Tank.MaxFuelHeight = this.CalculateMaxLevel(currentTitrimetry);

                currentTitrimetry.Tank.MinFuelHeight = 150;
                currentTitrimetry.Tank.MaxWaterHeight = 50;
                currentTitrimetry.Tank.OrderLimit = (currentTitrimetry.Tank.TotalVolume * 30) / 100;

                if (currentTitrimetry.Tank.TotalVolume > 20000)
                    currentTitrimetry.Tank.AlarmThreshold = 2;
                else if(currentTitrimetry.Tank.TotalVolume > 12000)
                    currentTitrimetry.Tank.AlarmThreshold = 3;
                else
                    currentTitrimetry.Tank.AlarmThreshold = 4;
            }
        }

        private void radMenuItem1_Click(object sender, EventArgs e)
        {
            Data.Tank newTank = new Data.Tank();
            newTank.TankSerialNumber = "-";
            newTank.OrderLimit = 0;
            newTank.TankId = Guid.NewGuid();
            if (this.database.CommunicationControllers.Count() > 0)
                newTank.CommunicationControllerId = this.database.CommunicationControllers.FirstOrDefault().CommunicationControllerId;
            if (this.database.FuelTypes.Count() > 0)
            {
                newTank.FuelTypeId = this.database.FuelTypes.First().FuelTypeId;
                newTank.FuelType = this.database.FuelTypes.First();
            }

            if (this.database.AtgProbeTypes.Count() > 0)
            {
                newTank.AtgProbeTypeId = this.database.AtgProbeTypes.First().AtgProbeTypeId;
                newTank.AtgProbeType = this.database.AtgProbeTypes.First();
            }

            this.database.Add(newTank);
            this.tanks.Add(newTank);
            this.radTreeView1.SelectedNode = this.radTreeView1.Nodes.Where(n => n.DataBoundItem == newTank).FirstOrDefault();
            this.radTreeView1.Focus();
        }

        private void radMenuItem2_Click(object sender, EventArgs e)
        {
            if (this.radTreeView1.SelectedNode == null)
                return;

            Data.Tank tank = this.radTreeView1.SelectedNode.DataBoundItem as Data.Tank;
            Data.Titrimetry titrimetry = this.radTreeView1.SelectedNode.DataBoundItem as Data.Titrimetry;
            Data.Titrimetry newTitrimetry = new Data.Titrimetry();
            newTitrimetry.TitrationDate = DateTime.Now;
            if (tank != null)
            {
                newTitrimetry.TankId = tank.TankId;
                newTitrimetry.Tank = tank;
                tank.Titrimetries.Add(newTitrimetry);
            }
            else if (titrimetry != null)
            {
                newTitrimetry.TankId = titrimetry.Tank.TankId;
                newTitrimetry.Tank = titrimetry.Tank;
                titrimetry.Tank.Titrimetries.Add(newTitrimetry);
            }
            this.database.Add(newTitrimetry);

            this.radTreeView1.DataSource = null;
            this.radTreeView1.DataSource = this.tanks;

            this.radTreeView1.SelectedNode = this.radTreeView1.Nodes.SelectMany(n => n.Nodes).Where(n => n.DataBoundItem == newTitrimetry).FirstOrDefault();
            this.radTreeView1.Focus();
        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            this.database.SaveChanges();
        }

        private void TankSettings_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!this.database.DatabaseChanged)
                return;
            DialogResult res = Telerik.WinControls.RadMessageBox.Show("Θέλετε να αποθηκευτούν οι αλλαγές που κάνατε;", "Έγιναν αλλαγές", MessageBoxButtons.YesNoCancel, Telerik.WinControls.RadMessageIcon.Question);
            if (res == System.Windows.Forms.DialogResult.No)
                return;
            else if (res == System.Windows.Forms.DialogResult.Yes)
            {
                this.database.SaveChanges();
                return;
            }
            e.Cancel = true;
        }

        private void radButton2_Click(object sender, EventArgs e)
        {
            if (this.radTreeView1.SelectedNode == null)
                return;
            Data.Tank tank = this.radTreeView1.SelectedNode.DataBoundItem as Data.Tank;
            if (tank == null)
                return;
            if (tank.Titrimetries.Count > 0)
            {
                Telerik.WinControls.RadMessageBox.Show("Η εγγραφή που επιλέξατε περιέχει Ογκομετρικούς Πίνακες", "Δεν επιτρέπεται η διαγραφή", MessageBoxButtons.OK, Telerik.WinControls.RadMessageIcon.Exclamation);
                return;
            }
            if (tank.TankFillings.Count > 0)
            {
                Telerik.WinControls.RadMessageBox.Show("Στη δεξαμενή που επιλέξατε έχουν γίνει παραλαβές!", "Δεν επιτρέπεται η διαγραφή", MessageBoxButtons.OK, Telerik.WinControls.RadMessageIcon.Exclamation);
                return;
            }
            if (tank.TankSales.Count > 0)
            {
                Telerik.WinControls.RadMessageBox.Show("Από την δεξαμενή που επιλέξατε έχουν γίνει πωλήσεις!", "Δεν επιτρέπεται η διαγραφή", MessageBoxButtons.OK, Telerik.WinControls.RadMessageIcon.Exclamation);
                return;
            }
            if (tank.NozzleFlows.Count > 0)
            {
                Telerik.WinControls.RadMessageBox.Show("Η δεξαμενή που επιλέξατε είναι συνδεδεμένη με αντλίες!", "Δεν επιτρέπεται η διαγραφή", MessageBoxButtons.OK, Telerik.WinControls.RadMessageIcon.Exclamation);
                return;
            }
            if (tank.InvoiceLines.Count > 0)
            {
                Telerik.WinControls.RadMessageBox.Show("Η δεξαμενή που επιλέξατε είναι συνδεδεμένη με παραστατικά!", "Δεν επιτρέπεται η διαγραφή", MessageBoxButtons.OK, Telerik.WinControls.RadMessageIcon.Exclamation);
                return;
            }
            this.tanks.Remove(tank);
            if(tank.TankPrices.Count > 0)
                this.database.Delete(tank.TankPrices);
            if (tank.TankChecks.Count > 0)
                this.database.Delete(tank.TankChecks);
            if (tank.SystemEvents.Count > 0)
            {
                try
                {
                    this.database.Delete(tank.SystemEvents.SelectMany(s => s.SystemEventData));
                }
                catch { }
                this.database.Delete(tank.SystemEvents);
            }
            this.database.Delete(tank);
            
        }

        private void radButton6_Click(object sender, EventArgs e)
        {
            if (this.titrimetryBindingSource.Position < 0 || this.titrimetryBindingSource.Current == null)
                return;
            Data.Titrimetry titration = this.titrimetryBindingSource.Current as Data.Titrimetry;
            if (titration == null)
                return;
            List<Data.TitrimetryLevel> levels = titration.TitrimetryLevels.OrderBy(l => l.Height).ToList();
            List<string> lines = new List<string>();
            foreach (Data.TitrimetryLevel level in levels)
            {
                string str = string.Format("{0}\t{1}\t{2}\t{3}", level.Height, level.Volume, level.UncertaintyVolume, level.UncertaintyPercent);
                lines.Add(str);
            }
            string dir = System.Environment.CurrentDirectory+"\\" + "Ογκομετρικοί Πίνακες";
            if(!System.IO.Directory.Exists(dir))
                System.IO.Directory.CreateDirectory(dir);
            string fileName = string.Format("Δεξαμενή {0}({1}).txt", titration.Tank.TankNumber, titration.Tank.TankSerialNumber);
            System.IO.File.WriteAllLines(dir + "\\" + fileName, lines.ToArray());

            Telerik.WinControls.RadMessageBox.Show("Ο ογκομετρικός πίνακας αποθηκεύτηκε στη διαδρομη\r\n" + dir + "\\" + fileName, "Επιτυχής δημιουργεία αρχείου", MessageBoxButtons.OK, Telerik.WinControls.RadMessageIcon.Info);

        }

        private void radButton7_Click(object sender, EventArgs e)
        {
            Data.Tank tank = this.radTreeView1.SelectedNode.DataBoundItem as Data.Tank;
            if (tank == null)
            {
                if(this.radTreeView1.SelectedNode.Parent != null)
                    tank = this.radTreeView1.SelectedNode.Parent.DataBoundItem as Data.Tank;
            }

            if (tank == null)
                return;

            Data.Titrimetry currentTitrimetry = tank.Titrimetries.OrderBy(t => t.TitrationDate).LastOrDefault();
            if (currentTitrimetry == null)
                return;
            if (currentTitrimetry.TitrimetryLevels.Count == 0)
                return;
            currentTitrimetry.Tank.TotalVolume = currentTitrimetry.TitrimetryLevels.Max(t => t.Volume).Value;

            //decimal? v = (decimal)0.95 * currentTitrimetry.TitrimetryLevels.Max(v1 => v1.Volume);
            //decimal vv = v.HasValue ? v.Value : 0;
            //Data.TitrimetryLevel maxLevel = currentTitrimetry.TitrimetryLevels.OrderBy(l => l.Height).Where(l => l.Volume <= vv).LastOrDefault();

            currentTitrimetry.Tank.MaxFuelHeight = this.CalculateMaxLevel(currentTitrimetry);
        }
    }
}
