using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Telerik.WinControls;
using Telerik.WinControls.UI;

namespace ASFuelControl.Windows.UI.Forms
{
    public partial class InvoiceSimpleForm : RadForm
    {
        ViewModels.InvoiceSimpleViewModel vm = new ViewModels.InvoiceSimpleViewModel();
        public InvoiceSimpleForm()
        {
            InitializeComponent();
            this.invoiceSimpleViewModelBindingSource.DataSource = vm;
            this.FormClosing += InvoiceSimpleForm_FormClosing;
        }

        public void CreateNew()
        {
            vm.CreateNew();
            this.invoiceSimpleViewModelBindingSource.ResetBindings(false);
            this.dispensersBindingSource.ResetBindings(false);
            this.nozzlesBindingSource.ResetBindings(false);
            this.invoiceTypesBindingSource.ResetBindings(false);
            this.paymentTypesBindingSource.ResetBindings(false);
        }

        public void CreateNew(Guid invoiceTypeId)
        {
            vm.CreateNew();
            vm.CreateSale = true;
            vm.InvoiceTypeId = invoiceTypeId;
            this.invoiceSimpleViewModelBindingSource.ResetBindings(false);
            this.dispensersBindingSource.ResetBindings(false);
            this.nozzlesBindingSource.ResetBindings(false);
            this.invoiceTypesBindingSource.ResetBindings(false);
            this.paymentTypesBindingSource.ResetBindings(false);
        }

        public void LoadInvoice(Guid invoiceId)
        {
            vm.Load(invoiceId);
            this.invoiceSimpleViewModelBindingSource.ResetBindings(false);
            this.dispensersBindingSource.ResetBindings(false);
            this.nozzlesBindingSource.ResetBindings(false);
            this.invoiceTypesBindingSource.ResetBindings(false);
            this.paymentTypesBindingSource.ResetBindings(false);
        }

        private void CreateAndPrintSale()
        {
            if (this.vm.CreateSale)
            {
                if (this.vm.InvoiceTypeId == Data.Implementation.OptionHandler.Instance.GetGuidOption("LiterCheckInvoiceType", Guid.Empty))
                    this.vm.CreateNewSale(true);
                else
                    this.vm.CreateNewSale(false);
                this.vm.HasChanges = false;
                this.Close();
            }
        }

        private void btnSelectVehicle_Click(object sender, EventArgs e)
        {
            using (SelectionForms.VehicleSelectForm vsf = new SelectionForms.VehicleSelectForm())
            {
                var res = vsf.ShowDialog(this);
                if (res != DialogResult.OK)
                    return;
                this.vm.VehicleId = ((ViewModels.VehicleViewModel)vsf.SelectedEntity).VehicleId;
            }
        }

        private void InvoiceSimpleForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!this.vm.HasChanges)
                return;
            DialogResult res = DialogResult.Abort;
            if (!this.vm.CreateSale)
                res = RadMessageBox.Show("Θέλετε να αποθηκεύσετε τις αλλαγές?", "Εχούν γίνει αλλαγές...", MessageBoxButtons.YesNoCancel, RadMessageIcon.Question);
            else
                res = RadMessageBox.Show("Θέλετε να εκτυπωθεί το παραστατικό?", "Εχούν γίνει αλλαγές...", MessageBoxButtons.YesNoCancel, RadMessageIcon.Question);
            if (res == DialogResult.Cancel)
                e.Cancel = true;
            else if (res == DialogResult.Yes)
            {
                if(this.vm.CreateSale)
                    CreateAndPrintSale();
                else
                    this.vm.Save(this.vm.InvoiceId);
            }
        }

        private void btnPrint_Click(object sender, EventArgs e)
        {
            CreateAndPrintSale();
        }
    }
}
