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
    public partial class FuelPumpSettings : RadForm
    {
        Data.DatabaseModel database = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
        BindingSource dispensersBindingSource = new BindingSource();
        BindingSource nozzleBindingSource = new BindingSource();
        BindingList<Data.Dispenser> dispensers;

        public FuelPumpSettings()
        {
            InitializeComponent();
            if (this.DesignMode)
                return;

            this.database.Events.Added += new Telerik.OpenAccess.AddEventHandler(Events_Added);
            this.database.Events.Changed += new Telerik.OpenAccess.ChangeEventHandler(Events_Changed);
            this.Width = 920;

            dispensers = new BindingList<Data.Dispenser>(this.database.Dispensers.ToList().OrderBy(d => d.Description).ToList());
            dispensers.ListChanged += new ListChangedEventHandler(dispensers_ListChanged);
            this.radTreeView1.DataSource = dispensers;

            if (dispensers.Count == 0)
            {
                this.radTreeView1.DisplayMember = "Description";
                this.radTreeView1.ValueMember = "DispenserId";
                this.radTreeView1.ChildMember = "Dispensers";
            }
            else
            {
                this.radTreeView1.DisplayMember = "Description\\Description";
                this.radTreeView1.ValueMember = "DispenserId\\NozzleId";
                this.radTreeView1.ChildMember = "Dispensers\\OrderedNozzles";
            }

            this.panelNozzle.Dock = DockStyle.Fill;
            this.panelDispenser.Dock = DockStyle.Fill;

            this.officialNozzleNumberRadSpinEditor.DataBindings.Add(new System.Windows.Forms.Binding("Value", this.nozzleBindingSource, "OfficialNozzleNumber", true));
            this.orderIdRadSpinEditor.DataBindings.Add(new System.Windows.Forms.Binding("Value", this.nozzleBindingSource, "OrderId", true));
            this.serialNumberRadTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.nozzleBindingSource, "SerialNumber", true));
            this.totalCounterRadSpinEditor.DataBindings.Add(new System.Windows.Forms.Binding("Value", this.nozzleBindingSource, "TotalCounter", true));


            //this.decPlacesRadSpinEditor.DataBindings.Clear();
            //this.decPlacesRadSpinEditor.DataBindings.Add(new System.Windows.Forms.Binding("Value", this.dispensersBindingSource, "DecimalPlaces", true));
            //this.upDecPlacesRadSpinEditor.DataBindings.Clear();
            //this.upDecPlacesRadSpinEditor.DataBindings.Add(new System.Windows.Forms.Binding("Value", this.dispensersBindingSource, "UnitPriceDecimalPlaces", true));


            this.fuelTypeDropDownList.DataBindings.Add(new System.Windows.Forms.Binding("SelectedValue", this.nozzleBindingSource, "FuelTypeId", true));
            this.fuelTypeDropDownList.DataSource = database.FuelTypes.OrderBy(f=>f.Name);
            this.fuelTypeDropDownList.DisplayMember = "Name";
            this.fuelTypeDropDownList.ValueMember = "FuelTypeId";

            this.radButton1.DataBindings.Add(new System.Windows.Forms.Binding("Enabled", this.database, "DatabaseChanged", true));

            this.controllerDropDownList.DataBindings.Add(new System.Windows.Forms.Binding("SelectedValue", this.dispenserBindingSource, "CommunicationControllerId", true));
            this.controllerDropDownList.DataSource = this.database.CommunicationControllers.OrderBy(cc => cc.Name);
            this.controllerDropDownList.DisplayMember = "Name";
            this.controllerDropDownList.ValueMember = "CommunicationControllerId";

            this.dispTypeRadDropDownList.DataSource = this.database.DispenserTypes.OrderBy(cc => cc.BrandName);
            this.dispTypeRadDropDownList.DisplayMember = "BrandName";
            this.dispTypeRadDropDownList.ValueMember = "DispenserTypeId";

            GridViewComboBoxColumn col = this.nozzleFlowsRadGridView.Columns["TankColumn"] as GridViewComboBoxColumn;
            col.DataSource = this.database.Tanks.ToList().OrderBy(t => t.Description);
            col.DisplayMember = "Description";
            col.ValueMember = "TankId";

            List<Common.Enumerators.EnumItemSmallInt> list = new List<Common.Enumerators.EnumItemSmallInt>();
            list.Add(new Common.Enumerators.EnumItemSmallInt() { Description = "Ανοιχτή", Value = 1 });
            list.Add(new Common.Enumerators.EnumItemSmallInt() { Description = "Κλειστή", Value = 0 });

            GridViewComboBoxColumn col2 = this.nozzleFlowsRadGridView.Columns["FlowStateColumn"] as GridViewComboBoxColumn;
            col2.DataSource = list;
            col2.DisplayMember = "Description";
            col2.ValueMember = "Value";

            defInvType.DataSource = this.database.InvoiceTypes.Where(i=>i.TransactionType == 0).OrderBy(i => i.Description);
            defInvType.DisplayMember = "Description";
            defInvType.ValueMember = "InvoiceTypeId";
        }

        void Events_Changed(object sender, Telerik.OpenAccess.ChangeEventArgs e)
        {
            if (e.PersistentObject.GetType() == typeof(Data.Nozzle))
            {
                if (e.PropertyName == "OfficialNozzleNumber")
                {
                    Data.Nozzle nozzle = e.PersistentObject as Data.Nozzle;
                    nozzle.Name = nozzle.OfficialNozzleNumber.ToString();
                }
            }
        }

        void Events_Added(object sender, Telerik.OpenAccess.AddEventArgs e)
        {
            if (e.PersistentObject.GetType() == typeof(Data.Nozzle))
            {
                Data.Nozzle nozzle = e.PersistentObject as Data.Nozzle;
                nozzle.Name = "0";
                nozzle.OrderId = 1;
                nozzle.SerialNumber = "-";
            }
            else if(e.PersistentObject.GetType() == typeof(Data.Dispenser))
            {
                Data.Dispenser dispenser = e.PersistentObject as Data.Dispenser;
                dispenser.DecimalPlaces = 2;
                dispenser.UnitPriceDecimalPlaces = 3;
                dispenser.PumpSerialNumber = "-";
                if (this.dispensers.Count > 0)
                    dispenser.DispenserTypeId = this.dispensers[0].DispenserTypeId;
                else
                    dispenser.DispenserTypeId = this.database.DispenserTypes.OrderBy(cc => cc.BrandName).First().DispenserTypeId;
            }
        }

        void dispensers_ListChanged(object sender, ListChangedEventArgs e)
        {
            BindingList<Data.Dispenser> list = sender as BindingList<Data.Dispenser>;
            if (list.Count == 0)
            {
                this.radTreeView1.DisplayMember = "Description";
                this.radTreeView1.ValueMember = "DispenserId";
                this.radTreeView1.ChildMember = "Dispensers";
            }
            else
            {
                this.radTreeView1.DisplayMember = "Description\\Description";
                this.radTreeView1.ValueMember = "DispenserId\\NozzleId";
                this.radTreeView1.ChildMember = "Dispensers\\OrderedNozzles";
            }
        }

        private void radTreeView1_SelectedNodeChanged(object sender, RadTreeViewEventArgs e)
        {
            if (e.Node == null)
                return;
            Data.Dispenser dispenser = e.Node.DataBoundItem as Data.Dispenser;
            Data.Nozzle nozzle = e.Node.DataBoundItem as Data.Nozzle;
            if (dispenser != null)
            {
                this.dispenserBindingSource.DataSource = dispenser;
                this.invoicePrintBindingSource.DataSource = this.dispenserBindingSource;
                this.invoicePrintBindingSource.DataMember = "InvoicePrints";
                this.panelDispenser.Show();
                this.panelNozzle.Hide();
            }
            else if (nozzle != null)
            {
                this.dispenserBindingSource.DataSource = nozzle.Dispenser;
                this.nozzleBindingSource.DataSource = nozzle;
                this.invoicePrintBindingSource.DataSource = this.dispenserBindingSource;
                this.invoicePrintBindingSource.DataMember = "InvoicePrints";
                this.nozzleFlowsBindingSource.DataSource = new BindingList<Data.NozzleFlow>(nozzle.NozzleFlows);
                this.panelDispenser.Hide();
                this.panelNozzle.Show();
            }
            //this.invoicePrintBindingSource.DataSource = null;
            //this.invoicePrintBindingSource.DataSource = this.dispenserBindingSource;
            //this.invoicePrintBindingSource.DataMember = "InvoicePrints";
        }

        private void radMenuButtonItem1_Click(object sender, EventArgs e)
        {
            Data.Dispenser newDispenser = new Data.Dispenser();
            newDispenser.DispenserId = Guid.NewGuid();
            newDispenser.IsValid = true;
            newDispenser.DispenserNumber = 1;
            newDispenser.OfficialPumpNumber = 1;

            if(this.database.CommunicationControllers.Count() > 0)
                newDispenser.CommunicationControllerId = this.database.CommunicationControllers.FirstOrDefault().CommunicationControllerId;
            this.database.Add(newDispenser);

            Data.InvoicePrint newInvPrint = new Data.InvoicePrint();
            newInvPrint.InvoicePrintId = Guid.NewGuid();
            newInvPrint.DispenserId = newDispenser.DispenserId;
            newInvPrint.Dispenser = newDispenser;
            newDispenser.InvoicePrints.Add(newInvPrint);
            this.database.Add(newDispenser);

            
            this.dispensers.Add(newDispenser);
            this.radTreeView1.SelectedNode = this.radTreeView1.Nodes.Where(n => n.DataBoundItem == newDispenser).FirstOrDefault();
            this.radTreeView1.Focus();
        }

        private void radMenuButtonItem2_Click(object sender, EventArgs e)
        {
            if (this.radTreeView1.SelectedNode == null)
                return;

            Data.Dispenser dispenser = this.radTreeView1.SelectedNode.DataBoundItem as Data.Dispenser;
            Data.Nozzle nozzle = this.radTreeView1.SelectedNode.DataBoundItem as Data.Nozzle;
            Data.Nozzle newNozzle = new Data.Nozzle();
            if (this.database.FuelTypes.Count() > 0)
                newNozzle.FuelTypeId = this.database.FuelTypes.OrderBy(ft => ft.Name).First().FuelTypeId;
            if (dispenser != null)
            {
                newNozzle.DispenserId = dispenser.DispenserId;
                newNozzle.Dispenser = dispenser;
                dispenser.Nozzles.Add(newNozzle);
            }
            else if (nozzle != null)
            {
                newNozzle.DispenserId = nozzle.Dispenser.DispenserId;
                newNozzle.Dispenser = nozzle.Dispenser;
                nozzle.Dispenser.Nozzles.Add(newNozzle);
            }
            newNozzle.OfficialNozzleNumber = 1;
            newNozzle.OrderId = 1;
            this.database.Add(newNozzle);

            this.radTreeView1.DataSource = null;
            this.radTreeView1.DataSource = this.dispensers;

            this.radTreeView1.SelectedNode = this.radTreeView1.Nodes.SelectMany(n => n.Nodes).Where(n => n.DataBoundItem == newNozzle).FirstOrDefault();
            this.radTreeView1.Focus();
        }

        private void radButton3_Click(object sender, EventArgs e)
        {
            Data.Nozzle parentNozzle = this.nozzleBindingSource.DataSource as Data.Nozzle;
            if(parentNozzle == null)
                return;
            BindingList<Data.NozzleFlow> nflows = this.nozzleFlowsBindingSource.DataSource as BindingList<Data.NozzleFlow>;
            if (nflows == null)
                return;
            Data.NozzleFlow nf = new Data.NozzleFlow();
            nf.NozzleFlowId = Guid.NewGuid();
            nf.NozzleId = parentNozzle.NozzleId;
            nf.Nozzle = parentNozzle;
            parentNozzle.NozzleFlows.Add(nf);
            this.database.Add(nf);
            this.nozzleFlowsBindingSource.ResetBindings(false);
            
            this.nozzleFlowsBindingSource.Position = nflows.IndexOf(nf);
        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            this.database.SaveChanges();
        }

        private void FuelPumpSettings_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!this.database.DatabaseChanged)
                return;
            DialogResult res = MessageBox.Show("Θέλετε να αποθηκευτούν οι αλλαγές που κάνατε;", "Έγιναν αλλαγές", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
            if (res == System.Windows.Forms.DialogResult.No)
                return;
            else if (res == System.Windows.Forms.DialogResult.Yes)
            {
                this.database.SaveChanges();
                return;
            }
            e.Cancel = true;
        }

        private void radButton5_Click(object sender, EventArgs e)
        {
            string defaultTaxDevice = Data.Implementation.OptionHandler.Instance.GetOption("DefaultTaxDevice");
            if (defaultTaxDevice == "Samtec")
            {
                using (SelectionForms.SelectPrinterForm spf = new SelectionForms.SelectPrinterForm())
                {
                    DialogResult res = spf.ShowDialog();
                    if (res == System.Windows.Forms.DialogResult.Cancel || spf.SelectedPrinter == null || spf.SelectedPrinter.Length == 0)
                        return;

                    Data.InvoicePrint disp = this.invoicePrintBindingSource.Current as Data.InvoicePrint;
                    disp.Printer = spf.SelectedPrinter;
                }
            }
            else
            {
                using (FolderBrowserDialog dlg = new FolderBrowserDialog())
                {
                    DialogResult res = dlg.ShowDialog();
                    if (res == System.Windows.Forms.DialogResult.Cancel)
                        return;

                    Data.InvoicePrint disp = this.invoicePrintBindingSource.Current as Data.InvoicePrint;
                    disp.Printer = dlg.SelectedPath;
                }
            }
        }

        private void radButton4_Click(object sender, EventArgs e)
        {
            Data.NozzleFlow nzf = this.nozzleFlowsBindingSource.Current as Data.NozzleFlow;
            if (nzf == null)
                return;
            DialogResult res = MessageBox.Show("Θέλετε να διαγραφεί η επιλεγμένη εγγραφή;", "Διαγραφή", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (res == System.Windows.Forms.DialogResult.No)
                return;
            nzf.Nozzle.NozzleFlows.Remove(nzf);
            this.database.Delete(nzf);
            this.nozzleFlowsBindingSource.ResetBindings(false);
        }

        private void radButton2_Click(object sender, EventArgs e)
        {
            if (this.radTreeView1.SelectedNode == null)
                return;
            Data.Dispenser disp = this.radTreeView1.SelectedNode.DataBoundItem as Data.Dispenser;
            Data.Nozzle nozzle = this.radTreeView1.SelectedNode.DataBoundItem as Data.Nozzle;
            if (disp == null && nozzle == null)
                return;

            DialogResult res = MessageBox.Show("Θέλετε να διαγραφεί η επιλεγμένη εγγραφή;", "Διαγραφή", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (res == System.Windows.Forms.DialogResult.No)
                return;

            if (disp != null)
            {
                this.dispensers.Remove(disp);
                this.database.Delete(disp.Nozzles);
                this.database.Delete(disp);
            }
            else
            {
                nozzle.Dispenser.Nozzles.Remove(nozzle);
                this.database.Delete(nozzle);
            }

            this.radTreeView1.DataSource = new BindingList<Data.Dispenser>(new List<Data.Dispenser>(new Data.Dispenser[]{new Data.Dispenser()}));
            this.radTreeView1.DataSource = this.dispensers;
        }
    }
}
