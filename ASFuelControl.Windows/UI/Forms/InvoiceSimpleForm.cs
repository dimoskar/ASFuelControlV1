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
        private DataSourceUpdateMode invoiceTypeBindingUpdateMode;
        private bool invoiceTypeBindingUpdateModeCaptured;
        private bool invoiceTypeControlLoaded;
        private Guid expectedInvoiceTypeId;
        private bool validateLookupsAfterLoad;

        public InvoiceSimpleForm()
        {
            InitializeComponent();
            PrepareInvoiceTypeBinding();
            PrepareIndependentLookupBindings();
            this.invoiceSimpleViewModelBindingSource.DataSource = vm;
            SynchronizeInvoiceTypeBinding();
            SynchronizeLookupBindings();
            vm.PropertyChanged += Vm_PropertyChanged;
            this.FormClosing += InvoiceSimpleForm_FormClosing;
        }

        private void Vm_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "CurrentDispenserId" ||
                e.PropertyName == "CurrentNozzleId" ||
                e.PropertyName == "PaymentType")
                SynchronizeLookupBindings();
        }

        protected override void OnLoad(EventArgs e)
        {
            try
            {
                ExecuteWithLookupBindingsReadOnly(() =>
                {
                    ExecuteWithInvoiceTypeBindingReadOnly(() =>
                    {
                        base.OnLoad(e);
                        if (expectedInvoiceTypeId != Guid.Empty)
                        {
                            if (vm.InvoiceTypeId != expectedInvoiceTypeId)
                                vm.InvoiceTypeId = expectedInvoiceTypeId;
                            SynchronizeInvoiceTypeBinding();
                        }
                        SynchronizeLookupBindings();
                    });
                });
                invoiceTypeControlLoaded = true;
                ValidateExpectedInvoiceType();
            }
            finally
            {
                RestoreInvoiceTypeBindingUpdateMode();
            }
            if (validateLookupsAfterLoad)
            {
                validateLookupsAfterLoad = false;
                ValidateLookupSelections(true);
            }
        }

        private void PrepareIndependentLookupBindings()
        {
            ExecuteWithLookupBindingsReadOnly(() =>
            {
                paymentTypesBindingSource.DataSource = null;
                paymentTypesBindingSource.DataMember = string.Empty;
                paymentTypesBindingSource.DataSource =
                    (vm.PaymentTypes ?? new Common.Enumerators.EnumItem[0]).ToArray();

                dispensersBindingSource.DataSource = null;
                dispensersBindingSource.DataMember = string.Empty;
                dispensersBindingSource.DataSource =
                    (vm.Dispensers ?? new ViewModels.DispenserViewModel[0]).ToArray();

                nozzlesBindingSource.DataSource = null;
                nozzlesBindingSource.DataMember = string.Empty;
                nozzlesBindingSource.DataSource = new ViewModels.NozzleViewModel[0];
            });
        }

        private void PrepareInvoiceTypeBinding()
        {
            var valueBinding = radDropDownList2.DataBindings["SelectedValue"];
            if (valueBinding != null)
            {
                invoiceTypeBindingUpdateMode = valueBinding.DataSourceUpdateMode;
                invoiceTypeBindingUpdateModeCaptured = true;
                valueBinding.DataSourceUpdateMode = DataSourceUpdateMode.Never;
            }
            invoiceTypesBindingSource.DataSource = null;
            invoiceTypesBindingSource.DataMember = string.Empty;
            invoiceTypesBindingSource.DataSource = new ViewModels.InvoiceTypeViewModel[0];
            radDropDownList2.DataSource = invoiceTypesBindingSource;
        }

        private void SynchronizeInvoiceTypeBinding()
        {
            var selectedTypeId = vm.InvoiceTypeId;
            // The simple view model already filters this list to the document types
            // supported by the compact editor. Keep that filter authoritative.
            var visibleTypes = vm.InvoiceTypes ?? new ViewModels.InvoiceTypeViewModel[0];
            var valueBinding = radDropDownList2.DataBindings["SelectedValue"];
            var updateMode = valueBinding == null
                ? DataSourceUpdateMode.Never
                : valueBinding.DataSourceUpdateMode;
            try
            {
                if (valueBinding != null)
                    valueBinding.DataSourceUpdateMode = DataSourceUpdateMode.Never;
                invoiceTypesBindingSource.DataSource = visibleTypes;
                if (selectedTypeId == Guid.Empty)
                    radDropDownList2.SelectedIndex = -1;
                else
                {
                    var selectedIndex = Array.FindIndex(visibleTypes,
                        item => item.InvoiceTypeId == selectedTypeId);
                    if (selectedIndex < 0)
                    {
                        radDropDownList2.SelectedIndex = -1;
                    }
                    else
                    {
                        invoiceTypesBindingSource.Position = selectedIndex;
                        radDropDownList2.SelectedIndex = selectedIndex;
                    }
                }
            }
            finally
            {
                if (valueBinding != null)
                    valueBinding.DataSourceUpdateMode = updateMode;
            }
        }

        private void ResetEditorBindings()
        {
            var selectedTypeId = vm.InvoiceTypeId;
            var selectedPaymentType = vm.PaymentType;
            var selectedDispenserId = vm.CurrentDispenserId;
            var selectedNozzleId = vm.CurrentNozzleId;
            ExecuteWithLookupBindingsReadOnly(() =>
            {
                ExecuteWithInvoiceTypeBindingReadOnly(() =>
                {
                    this.invoiceSimpleViewModelBindingSource.ResetBindings(false);
                    if (vm.InvoiceTypeId != selectedTypeId)
                        vm.InvoiceTypeId = selectedTypeId;
                    if (vm.PaymentType != selectedPaymentType)
                        vm.PaymentType = selectedPaymentType;
                    if (vm.CurrentDispenserId != selectedDispenserId)
                        vm.CurrentDispenserId = selectedDispenserId;
                    if (vm.CurrentNozzleId != selectedNozzleId)
                        vm.CurrentNozzleId = selectedNozzleId;
                    SynchronizeInvoiceTypeBinding();
                    SynchronizeLookupBindings();
                });
            });
        }

        private void ExecuteWithLookupBindingsReadOnly(Action action)
        {
            var controls = new[] { paymentTypeDropDownList, radDropDownList3, radDropDownList1 };
            var bindings = controls.Where(control => control != null)
                .Select(control => control.DataBindings["SelectedValue"])
                .Where(binding => binding != null)
                .Select(binding => new KeyValuePair<Binding, DataSourceUpdateMode>(
                    binding, binding.DataSourceUpdateMode))
                .ToArray();
            try
            {
                foreach (var item in bindings)
                    item.Key.DataSourceUpdateMode = DataSourceUpdateMode.Never;
                action();
            }
            finally
            {
                foreach (var item in bindings)
                    item.Key.DataSourceUpdateMode = item.Value;
            }
        }

        private void SynchronizeLookupBindings()
        {
            ExecuteWithLookupBindingsReadOnly(() =>
            {
                var dispenser = (vm.Dispensers ?? new ViewModels.DispenserViewModel[0])
                    .FirstOrDefault(item => item.DispenserId == vm.CurrentDispenserId);
                nozzlesBindingSource.DataSource = dispenser == null || dispenser.Nozzles == null
                    ? new ViewModels.NozzleViewModel[0]
                    : dispenser.Nozzles.ToArray();
                var paymentTypes = vm.PaymentTypes ??
                    new Common.Enumerators.EnumItem[0];
                int paymentIndex = vm.PaymentType.HasValue
                    ? Array.FindIndex(paymentTypes,
                        item => item.Value == vm.PaymentType.Value)
                    : -1;
                // Keep the lookup CurrencyManager on the saved payment method;
                // otherwise it restores row zero after the control is shown.
                paymentTypesBindingSource.Position = paymentIndex;
                paymentTypeDropDownList.SelectedIndex = paymentIndex;
                SelectLookupValue(radDropDownList3, vm.CurrentDispenserId);
                SelectLookupValue(radDropDownList1, vm.CurrentNozzleId);
            });
        }

        private static void SelectLookupValue(RadDropDownList control, object value)
        {
            int selectedIndex = -1;
            if (value != null && !object.Equals(value, Guid.Empty))
            {
                for (int index = 0; index < control.Items.Count; index++)
                {
                    if (object.Equals(control.Items[index].Value, value))
                    {
                        selectedIndex = index;
                        break;
                    }
                }
            }
            control.SelectedIndex = selectedIndex;
        }

        private void ExecuteWithInvoiceTypeBindingReadOnly(Action action)
        {
            var valueBinding = radDropDownList2.DataBindings["SelectedValue"];
            var updateMode = valueBinding == null
                ? DataSourceUpdateMode.Never
                : valueBinding.DataSourceUpdateMode;
            try
            {
                if (valueBinding != null)
                    valueBinding.DataSourceUpdateMode = DataSourceUpdateMode.Never;
                action();
            }
            finally
            {
                if (valueBinding != null)
                    valueBinding.DataSourceUpdateMode = updateMode;
            }
        }

        private void RestoreInvoiceTypeBindingUpdateMode()
        {
            if (!invoiceTypeBindingUpdateModeCaptured)
                return;
            var valueBinding = radDropDownList2.DataBindings["SelectedValue"];
            if (valueBinding != null)
                valueBinding.DataSourceUpdateMode = invoiceTypeBindingUpdateMode;
            invoiceTypeBindingUpdateModeCaptured = false;
        }

        private void ScheduleInvoiceTypeValidation(Guid invoiceTypeId)
        {
            expectedInvoiceTypeId = invoiceTypeId;
            if (invoiceTypeControlLoaded)
                ValidateExpectedInvoiceType();
        }

        private void ValidateExpectedInvoiceType()
        {
            if (expectedInvoiceTypeId == Guid.Empty)
                return;
            var expected = expectedInvoiceTypeId;
            expectedInvoiceTypeId = Guid.Empty;
            ExecuteWithInvoiceTypeBindingReadOnly(() =>
            {
                if (vm.InvoiceTypeId != expected)
                    vm.InvoiceTypeId = expected;
                SynchronizeInvoiceTypeBinding();
            });
            if (!VerifyInvoiceTypeSelection(expected))
                DisableInvoiceActions();
        }

        private bool EnsureInvoiceTypeAvailable(Guid invoiceTypeId)
        {
            if (invoiceTypeId != Guid.Empty &&
                (vm.InvoiceTypes ?? new ViewModels.InvoiceTypeViewModel[0])
                    .Any(item => item.InvoiceTypeId == invoiceTypeId))
                return true;

            var knownType = ViewModels.CommonCache.Instance.InvoiceTypes
                .SingleOrDefault(item => item.InvoiceTypeId == invoiceTypeId);
            var typeDescription = knownType == null
                ? invoiceTypeId.ToString()
                : knownType.Description;
            RadMessageBox.Show(
                "Ο επιλεγμένος τύπος παραστατικού \"" + typeDescription +
                "\" δεν είναι διαθέσιμος στη φιλτραρισμένη λίστα αυτού του παραστατικού. " +
                "Η ενέργεια ακυρώθηκε.",
                "Σφάλμα Τύπου Παραστατικού", MessageBoxButtons.OK, RadMessageIcon.Error);
            return false;
        }

        private bool VerifyInvoiceTypeSelection(Guid expectedInvoiceTypeId)
        {
            if (!EnsureInvoiceTypeAvailable(expectedInvoiceTypeId))
                return false;
            var selectedItem = radDropDownList2.SelectedItem == null
                ? null
                : radDropDownList2.SelectedItem.DataBoundItem as ViewModels.InvoiceTypeViewModel;
            if (vm.InvoiceTypeId == expectedInvoiceTypeId && selectedItem != null &&
                selectedItem.InvoiceTypeId == expectedInvoiceTypeId)
                return true;

            var expectedType = vm.InvoiceTypes
                .Single(item => item.InvoiceTypeId == expectedInvoiceTypeId);
            RadMessageBox.Show(
                "Δεν ήταν δυνατή η σύνδεση του επιλεγμένου τύπου παραστατικού \"" +
                expectedType.Description + "\" με τη φόρμα. Η ενέργεια ακυρώθηκε.",
                "Σφάλμα Τύπου Παραστατικού", MessageBoxButtons.OK, RadMessageIcon.Error);
            return false;
        }

        private bool ValidateLookupSelections(bool showMessage)
        {
            var errors = new List<string>();
            var invoiceTypes = vm.InvoiceTypes ?? new ViewModels.InvoiceTypeViewModel[0];
            if (vm.InvoiceTypeId == Guid.Empty ||
                !invoiceTypes.Any(item => item.InvoiceTypeId == vm.InvoiceTypeId) ||
                !object.Equals(radDropDownList2.SelectedValue, vm.InvoiceTypeId))
                errors.Add("Δεν έχει επιλεγεί έγκυρος τύπος παραστατικού.");

            var paymentTypes = vm.PaymentTypes ?? new Common.Enumerators.EnumItem[0];
            bool validPayment = vm.PaymentType.HasValue && paymentTypes.Any(item =>
            {
                try { return Convert.ToInt32(item.Value) == vm.PaymentType.Value; }
                catch { return false; }
            });
            if (!validPayment ||
                !object.Equals(paymentTypeDropDownList.SelectedValue, vm.PaymentType))
                errors.Add("Δεν έχει επιλεγεί έγκυρος τρόπος πληρωμής.");

            var dispenser = (vm.Dispensers ?? new ViewModels.DispenserViewModel[0])
                .FirstOrDefault(item => item.DispenserId == vm.CurrentDispenserId);
            if (dispenser == null ||
                !object.Equals(radDropDownList3.SelectedValue, vm.CurrentDispenserId))
                errors.Add("Δεν έχει επιλεγεί έγκυρη αντλία.");

            var nozzle = dispenser == null || dispenser.Nozzles == null ? null :
                dispenser.Nozzles.FirstOrDefault(item => item.NozzleId == vm.CurrentNozzleId);
            if (nozzle == null ||
                !object.Equals(radDropDownList1.SelectedValue, vm.CurrentNozzleId))
                errors.Add("Το ακροσωλήνιο δεν ανήκει στην επιλεγμένη αντλία.");
            else if (nozzle.FuelTypeId == Guid.Empty ||
                !(vm.FuelTypes ?? new ViewModels.FuelTypeViewModel[0])
                    .Any(item => item.FuelTypeId == nozzle.FuelTypeId))
                errors.Add("Το καύσιμο του ακροσωληνίου δεν ανήκει στη διαθέσιμη λίστα καυσίμων.");

            if (vm.InvoiceLine != null && vm.InvoiceLine.SalesTransaction != null &&
                vm.InvoiceLine.SalesTransaction.NozzleId != Guid.Empty &&
                vm.InvoiceLine.SalesTransaction.NozzleId != vm.CurrentNozzleId)
                errors.Add("Το ακροσωλήνιο της φόρμας δεν συμφωνεί με τη συνδεδεμένη πώληση.");

            var distinctErrors = errors.Distinct().ToArray();
            if (distinctErrors.Length == 0)
                return true;
            if (showMessage)
            {
                RadMessageBox.Show(
                    "Η αποθήκευση ή έκδοση ακυρώθηκε επειδή υπάρχουν ασυνεπείς επιλογές:\r\n\r\n" +
                    string.Join("\r\n", distinctErrors.Select(error => "• " + error)),
                    "Έλεγχος επιλογών παραστατικού", MessageBoxButtons.OK,
                    RadMessageIcon.Error);
            }
            return false;
        }

        private void DisableInvoiceActions()
        {
            btnSave.Enabled = false;
            btnPrint.Enabled = false;
        }

        public void CreateNew()
        {
            vm.CreateNew();
            ResetEditorBindings();
        }

        public void CreateNew(Guid invoiceTypeId)
        {
            if (!EnsureInvoiceTypeAvailable(invoiceTypeId))
            {
                DisableInvoiceActions();
                return;
            }
            ExecuteWithInvoiceTypeBindingReadOnly(() =>
            {
                vm.CreateNew();
                vm.CreateSale = true;
                vm.InvoiceTypeId = invoiceTypeId;
            });
            ResetEditorBindings();
            ScheduleInvoiceTypeValidation(invoiceTypeId);
        }

        public void LoadInvoice(Guid invoiceId)
        {
            ExecuteWithLookupBindingsReadOnly(() =>
                ExecuteWithInvoiceTypeBindingReadOnly(() => vm.Load(invoiceId)));
            var loadedInvoiceTypeId = vm.InvoiceTypeId;
            ResetEditorBindings();
            ScheduleInvoiceTypeValidation(loadedInvoiceTypeId);
            validateLookupsAfterLoad = true;
            if (invoiceTypeControlLoaded)
            {
                validateLookupsAfterLoad = false;
                ValidateLookupSelections(true);
            }
        }

        private void CreateAndPrintSale()
        {
            if (this.vm.CreateSale)
            {
                this.ValidateChildren();
                this.invoiceSimpleViewModelBindingSource.EndEdit();
                if (!ValidateLookupSelections(true))
                    return;
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
                else if (ValidateLookupSelections(true))
                    this.vm.Save(this.vm.InvoiceId);
            }
        }

        private void btnPrint_Click(object sender, EventArgs e)
        {
            CreateAndPrintSale();
        }
    }
}
