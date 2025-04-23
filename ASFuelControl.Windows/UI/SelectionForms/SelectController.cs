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
    public partial class SelectController : RadForm
    {
        Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);

        List<Guid> selectedControllers = new List<Guid>();

        public Guid[] SelectedControllers
        {
            get { return this.selectedControllers.ToArray(); }
        }

        public SelectController()
        {
            InitializeComponent();
            this.communicationControllerBindingSource.DataSource = this.db.CommunicationControllers.Where(c => c.Dispensers.Count > 0).OrderBy(c => c.Name);
            this.radListControl1.DisplayMember = "Name";
            this.radListControl1.ValueMember = "CommunicationControllerId";
            this.communicationControllerBindingSource.ResetBindings(false);
            this.Disposed += SelectController_Disposed;
        }

        private void SelectController_Disposed(object sender, EventArgs e)
        {
            this.db.Dispose();
        }

        private void radListControl1_SelectedItemsChanged(object sender, Telerik.WinControls.Data.NotifyCollectionChangedEventArgs e)
        {
            try
            {
                if (e.NewItems != null)
                {
                    foreach (RadListDataItem entity in e.NewItems)
                    {
                        Data.CommunicationController controller = entity.DataBoundItem as Data.CommunicationController;
                        if (!this.selectedControllers.Contains(controller.CommunicationControllerId))
                            this.selectedControllers.Add(controller.CommunicationControllerId);
                    }
                }
                if (e.OldItems != null)
                {
                    foreach (RadListDataItem entity in e.OldItems)
                    {
                        Data.CommunicationController controller = entity.DataBoundItem as Data.CommunicationController;
                        if (this.selectedControllers.Contains(controller.CommunicationControllerId))
                            this.selectedControllers.Remove(controller.CommunicationControllerId);
                    }
                }
            }
            catch(Exception ex)
            {
            }
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
