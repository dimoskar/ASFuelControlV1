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
    public partial class SelectDispenserNozzleForm : RadForm
    {
        private List<Guid> selectedNozzles = new List<Guid>();
        private Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);

        public Guid[] SelectedNozzles
        {
            get
            {
                foreach (RadTreeNode pnode in this.radTreeView1.Nodes)
                {
                    foreach (RadTreeNode cnode in pnode.Nodes)
                    {
                        if(cnode.CheckState == Telerik.WinControls.Enumerations.ToggleState.On)
                        {
                            Data.Nozzle nozzle = cnode.Tag as Data.Nozzle;
                            if (nozzle != null)
                                this.selectedNozzles.Add(nozzle.NozzleId);
                        }
                    }
                }
                return this.selectedNozzles.ToArray();
            }
        }

        public SelectDispenserNozzleForm()
        {
            InitializeComponent();
            this.LoadTree();
            this.Disposed += SelectDispenserNozzleForm_Disposed;
        }

        private void SelectDispenserNozzleForm_Disposed(object sender, EventArgs e)
        {
            this.db.Dispose();
        }

        private void LoadTree()
        {
            var q = this.db.Dispensers.OrderBy(d => d.OfficialPumpNumber);
            foreach (Data.Dispenser dispenser in q)
            {
                RadTreeNode node = new RadTreeNode(dispenser.Description);
                node.Tag = dispenser;
                this.radTreeView1.Nodes.Add(node);
                foreach (Data.Nozzle nozzle in dispenser.Nozzles)
                {
                    RadTreeNode cnode = new RadTreeNode(nozzle.Description);
                    cnode.Tag = nozzle;
                    node.Nodes.Add(cnode);
                }
            }
            
            this.radTreeView1.ExpandAll();
        }

        private void SelectNozzle(Data.Nozzle nozzle, RadTreeNode node)
        {
            if (node.CheckState == Telerik.WinControls.Enumerations.ToggleState.On)
            {
                if (!this.selectedNozzles.Contains(nozzle.NozzleId))
                    this.selectedNozzles.Add(nozzle.NozzleId);
            }
            else if (node.CheckState == Telerik.WinControls.Enumerations.ToggleState.Off)
            {
                if (this.selectedNozzles.Contains(nozzle.NozzleId))
                    this.selectedNozzles.Remove(nozzle.NozzleId);
            }
        }

        private void radTreeView1_NodeCheckedChanged(object sender, TreeNodeCheckedEventArgs e)
        {
            //Data.Dispenser dispenser = e.Node.Tag as Data.Dispenser;
            //Data.Nozzle nozzle = e.Node.Tag as Data.Nozzle;
            //if (dispenser == null && nozzle == null)
            //    return;
            //if (dispenser != null)
            //{
            //    if (e.Node.CheckState != Telerik.WinControls.Enumerations.ToggleState.Indeterminate)
            //    {
            //        foreach (RadTreeNode cnode in e.Node.Nodes)
            //        {
            //            nozzle = cnode.Tag as Data.Nozzle;
            //            cnode.CheckState = e.Node.CheckState;
            //            this.SelectNozzle(nozzle, cnode);
            //        }
            //    }
            //}
            //else
            //{
            //    foreach (RadTreeNode cnode in e.Node.Parent.Nodes)
            //    {
            //        if (cnode == e.Node)
            //            continue;
            //        if (cnode.CheckState != e.Node.CheckState)
            //        {
            //            e.Node.Parent.CheckState = Telerik.WinControls.Enumerations.ToggleState.Indeterminate;
            //            break;
            //        }
            //        this.SelectNozzle(nozzle, cnode);
            //    }
            //}
        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
        }

        private void radButton2_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        }
    }
}
