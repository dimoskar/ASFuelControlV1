using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Telerik.WinControls.UI;

namespace ASFuelControl.Windows.UI.Forms
{
    public partial class InvoiceTransformationForm : RadForm
    {
        public ViewModels.QueryInvoiceDataArgs Data { private set; get; } = new ViewModels.QueryInvoiceDataArgs();

        public InvoiceTransformationForm()
        {
            InitializeComponent();
        }

        public void LoadData(ViewModels.QueryInvoiceDataArgs args)
        {
            this.queryInvoiceDataArgsBindingSource.DataSource = args;
            this.queryInvoiceDataArgsBindingSource.ResetBindings(false);
            this.volumesBindingSource.ResetBindings(false);
            this.Data = args;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
        }

        private void btnSelectTrader_Click(object sender, EventArgs e)
        {
            SelectionForms.TraderSelectForm tf = new SelectionForms.TraderSelectForm();
            tf.ShowDialog(this);
            ViewModels.TraderViewModel tvm = tf.SelectedEntity as ViewModels.TraderViewModel;
            if (tvm != null)
                this.Data.TraderId = tvm.TraderId;
        }

        private void btnSelectVehicle_Click(object sender, EventArgs e)
        {
            SelectionForms.VehicleSelectForm tf = new SelectionForms.VehicleSelectForm();
            tf.ShowDialog(this);
            ViewModels.VehicleViewModel tvm = tf.SelectedEntity as ViewModels.VehicleViewModel;
            if (tvm != null)
                this.Data.VehicleId = tvm.VehicleId;
        }
    }
}
