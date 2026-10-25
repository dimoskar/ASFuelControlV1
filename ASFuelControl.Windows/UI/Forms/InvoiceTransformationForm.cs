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
            this.radGridView1.Columns["Volume"].ReadOnly = !args.AllowVolumeEdit;
            this.radGridView1.Columns["UnitPrice"].ReadOnly = !args.AllowUnitPriceEdit;
            this.radGridView1.Columns["DiscountAmount"].ReadOnly = true;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (this.Data.Volumes.Any(v => v.Volume <= 0 || v.Volume > v.MaxAllowedVolume))
            {
                Telerik.WinControls.RadMessageBox.Show(this,
                    "Ο όγκος πρέπει να είναι μεγαλύτερος από μηδέν και να μην υπερβαίνει τον επιτρεπόμενο όγκο.",
                    "Έλεγχος μετασχηματισμού", MessageBoxButtons.OK,
                    Telerik.WinControls.RadMessageIcon.Exclamation);
                return;
            }
            if (this.Data.RequireUnitPrice && this.Data.Volumes.Any(v => v.UnitPrice <= 0))
            {
                Telerik.WinControls.RadMessageBox.Show(this,
                    "Συμπληρώστε τιμή μονάδος για όλες τις γραμμές.",
                    "Έλεγχος μετασχηματισμού", MessageBoxButtons.OK,
                    Telerik.WinControls.RadMessageIcon.Exclamation);
                return;
            }
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
