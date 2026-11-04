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
    public partial class InvoiceTypesForm : RadForm
    {
        ViewModels.InvoiceTypeCatalogViewModel vm = new ViewModels.InvoiceTypeCatalogViewModel();

        public InvoiceTypesForm()
        {
            InitializeComponent();
            PrepareTransformationLookupBindings();
            InitializeInvoicingPolicy();
            vm.PropertyChanged += Vm_PropertyChanged;
            this.Load += InvoiceTypesForm_Load;
        }

        private void PrepareTransformationLookupBindings()
        {
            // These lookup lists describe the values allowed in transformation
            // rows. They are not children of the currently selected invoice type:
            // changing/resetting that row must never move a combo editor to item 0
            // and write the new position into ChildInvoiceTypeId/CreationTypeId.
            var filteredTypes = vm.InvoiceTypes
                .Cast<ViewModels.InvoiceTypeViewModel>()
                .OrderBy(item => item.Description)
                .ToArray();
            var nullableTypes = new List<ViewModels.InvoiceTypeViewModel>
            {
                new ViewModels.InvoiceTypeViewModel { InvoiceTypeId = Guid.Empty }
            };
            nullableTypes.AddRange(filteredTypes);
            var transformationTypes = Common.Enumerators.EnumToList
                .EnumList<Common.Enumerators.InvoiceTransformationTypeEnum>()
                .ToArray();

            SetIndependentLookupSource(invoiceTypesBindingSource, filteredTypes);
            SetIndependentLookupSource(invoiceTypesNullableBindingSource, nullableTypes.ToArray());
            SetIndependentLookupSource(invoiceTransformationTypesBindingSource, transformationTypes);
        }

        private static void SetIndependentLookupSource(BindingSource source, object values)
        {
            source.DataSource = null;
            source.DataMember = string.Empty;
            source.DataSource = values;
        }

        private void Vm_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "InvoiceTypes")
                return;
            // Commit a user's active grid choice before replacing only the lookup
            // lists. The transformation row values themselves are never rebound.
            radGridView2.EndEdit();
            PrepareTransformationLookupBindings();
        }

        private void InvoiceTypesForm_Load(object sender, EventArgs e)
        {
            try
            {
                this.invoiceTypeCatalogViewModelBindingSource.DataSource = vm;
                this.invoiceTypeCatalogViewModelBindingSource.ResetBindings(false);
            }
            catch
            { }
        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            try { SaveInvoicingPolicies(); }
            catch (Exception ex) { RadMessageBox.Show(ex.Message,"Ρυθμίσεις τιμολόγησης",MessageBoxButtons.OK,RadMessageIcon.Error); }
           
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            var it = this.vm.AddNewInvoiceType();
            this.invoiceTypeViewModelBindingSource.ResetBindings(false);
            this.invoiceTypeViewModelBindingSource.Position = this.invoiceTypeViewModelBindingSource.List.IndexOf(it);
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (this.radGridView1.CurrentRow == null)
                return;
            ViewModels.InvoiceTypeDetailViewModel it = this.radGridView1.CurrentRow.DataBoundItem as ViewModels.InvoiceTypeDetailViewModel;
            if (it == null)
                return;
            var res = RadMessageBox.Show("Θέλετε να διαγράψετε την επιλεγμένη εγγραφή;", "Διαγραφή Παραστατικού", MessageBoxButtons.YesNo, RadMessageIcon.Question);
            if (res == DialogResult.No)
                return;
            if (vm.DeleteInvoiceType(it))
                return;
            RadMessageBox.Show("Η επιλεγμένη εγγραφή περιέχει συνδέσεις. Δεν μπορεί να διαγραφεί!", "Σφάλμα Διαγραφής Παραστατικού", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
        }

        private void btnAddVehicle_Click(object sender, EventArgs e)
        {
            ViewModels.InvoiceTypeDetailViewModel it = this.radGridView1.CurrentRow.DataBoundItem as ViewModels.InvoiceTypeDetailViewModel;
            if (it == null)
                return;
            it.AddTransformation();
        }

        private void btnDeleteVehicle_Click(object sender, EventArgs e)
        {
            ViewModels.InvoiceTypeDetailViewModel it = this.radGridView1.CurrentRow.DataBoundItem as ViewModels.InvoiceTypeDetailViewModel;
            if (it == null)
                return;
            if (this.radGridView2.CurrentRow == null)
                return;
            ViewModels.InvoiceTypeTransformViewModel itt = this.radGridView2.CurrentRow.DataBoundItem as ViewModels.InvoiceTypeTransformViewModel;
            if (itt == null)
                return;
            var res = RadMessageBox.Show("Θέλετε να διαγράψετε την επιλεγμένη εγγραφή;", "Διαγραφή Μετασχηματισμού Παραστατικού", MessageBoxButtons.YesNo, RadMessageIcon.Question);
            if (res == DialogResult.No)
                return;
            it.DeleteTransformation(itt);
        }
    }
}
