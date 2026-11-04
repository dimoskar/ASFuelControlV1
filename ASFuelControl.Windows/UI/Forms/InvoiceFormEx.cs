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
    public partial class InvoiceFormEx : RadForm
    {
        protected readonly ViewModels.InvoiceDetailsViewModel ivm;
        private readonly ViewModels.InvoiceEditorLayout editorLayout = ViewModels.InvoiceEditorLayout.Legacy;
        private bool invoiceTypeBindingValid = true;
        private DataSourceUpdateMode invoiceTypeBindingUpdateMode;
        private bool invoiceTypeBindingUpdateModeCaptured;
        private bool invoiceTypeControlLoaded;
        private Guid expectedInvoiceTypeId;
        private bool cancelTransformationOnTypeMismatch;
        private bool validateLookupsAfterLoad;
        private bool lookupValidationMessageShown;
        private ViewModels.InvoiceLineViewModel nozzleLookupLine;
        private Guid nozzleLookupDispenserId;
        private bool nozzleLookupInitialized;
        private bool lineEditorDefaultsCaptured;
        private bool volumeEditorReadOnly;
        private bool unitPriceEditorReadOnly;
        private bool discountEditorReadOnly;
        private bool totalEditorReadOnly;
        private bool guideOnlyFinancialEdit;

        private Guid[] relatedIds;
        private Guid[] pendingRelatedProcessingIds = new Guid[0];

        public InvoiceFormEx() : this(new ViewModels.InvoiceDetailsViewModel())
        {
        }

        protected InvoiceFormEx(ViewModels.InvoiceDetailsViewModel viewModel)
            : this(viewModel, ViewModels.InvoiceEditorLayout.Legacy)
        {
        }

        protected InvoiceFormEx(ViewModels.InvoiceDetailsViewModel viewModel,
            ViewModels.InvoiceEditorLayout layout)
        {
            if (viewModel == null)
                throw new ArgumentNullException("viewModel");
            ivm = viewModel;
            editorLayout = layout;
            if (layout == ViewModels.InvoiceEditorLayout.Legacy)
                InitializeComponent();
            else
                InitializeFixedLayout(layout);
            CaptureLineEditorDefaults();
            RemoveTopPaymentSelector();
            ConfigureCommonEditorAppearance();
            ConfigureLineGridEditors();
            InitializeFiscalStatus();
            fiscalPanel.ActionCompleted += (s, e) => {
                if (!IsDisposed && fiscalPanel.Owned) LoadInvoice(ivm.InvoiceId);
            };

            //var dispColumn = this.radGridView1.Columns["Dispenser"] as GridViewComboBoxColumn;
            //dispColumn.DataSource = dispensersBindingSource;
            //dispColumn.ValueMember = "DispenserId";
            //dispColumn.DisplayMember = "Description";

            //var nozzleColumn = this.radGridView1.Columns["Nozzle"] as GridViewComboBoxColumn;
            //nozzleColumn.DataSource = this.nozzlesBindingSource;
            //nozzleColumn.ValueMember = "NozzleId";
            //nozzleColumn.DisplayMember = "Description";

            PrepareInvoiceTypeBinding();
            PreparePaymentTypeBinding();
            this.invoiceViewModelBindingSource.DataSource = ivm;
            PrepareLineSalePointBindings();
            SynchronizeInvoiceTypeBinding();
            SynchronizePaymentTypeBindings();
            this.FormClosing += InvoiceFormEx_FormClosing;
            ivm.QueryInvoiceData += Ivm_QueryInvoiceData;
            ivm.PropertyChanged += Ivm_PropertyChanged;
            ivm.RefreshView += Ivm_RefreshView;
        }

        private void CaptureLineEditorDefaults()
        {
            if (lineEditorDefaultsCaptured)
                return;
            if (volume1 != null)
                volumeEditorReadOnly = volume1.ReadOnly;
            if (unitPrice1 != null)
                unitPriceEditorReadOnly = unitPrice1.ReadOnly;
            if (discount != null)
                discountEditorReadOnly = discount.ReadOnly;
            if (priceTotal1 != null)
                totalEditorReadOnly = priceTotal1.ReadOnly;
            lineEditorDefaultsCaptured = true;
        }

        protected override void OnLoad(EventArgs e)
        {
            try
            {
                // Keep control-to-model writes disabled throughout the initial
                // WinForms/Telerik binding lifecycle. The requested type is the
                // authority until every child control has finished loading.
                ExecuteWithPaymentTypeBindingsReadOnly(() =>
                {
                    ExecuteWithInvoiceTypeBindingReadOnly(() =>
                    {
                        ExecuteWithLineLookupBindingsReadOnly(() =>
                        {
                            base.OnLoad(e);
                            if (expectedInvoiceTypeId != Guid.Empty)
                            {
                                if (ivm.InvoiceTypeId != expectedInvoiceTypeId)
                                    ivm.InvoiceTypeId = expectedInvoiceTypeId;
                                SynchronizeInvoiceTypeBinding();
                            }
                            SynchronizePaymentTypeBindings();
                        });
                    });
                });
                invoiceTypeControlLoaded = true;
                ValidateExpectedInvoiceType();
                // TransformInvoices runs before Show(). Reapply the policy after
                // WinForms/Telerik completes its initial binding pass so a
                // guide-only target keeps its price/discount cells enabled.
                ApplyPriceBindings();
                SetView();
            }
            finally
            {
                RestoreInvoiceTypeBindingUpdateMode();
            }
            if (validateLookupsAfterLoad && invoiceTypeBindingValid)
            {
                validateLookupsAfterLoad = false;
                ValidateEditorLookupSelections(true);
            }
        }

        private void RemoveTopPaymentSelector()
        {
            // Payment is edited in the invoice body. The toolbar copy was only
            // made visible for saved/printed invoices and duplicated that value.
            // Remove it for every InvoiceFormEx layout, including the legacy
            // editor and all scenario-specific forms built by the factory.
            if (panel6 == null)
                return;
            // A control removed from the visual tree is still alive and its
            // two-way binding still writes to the view model. Detach it before
            // removing the duplicate toolbar editor.
            if (radDropDownList5 != null)
            {
                radDropDownList5.DataBindings.Clear();
                radDropDownList5.DataSource = null;
            }
            panel6.Visible = false;
            if (panel6.Parent != null)
                panel6.Parent.Controls.Remove(panel6);
        }

        private void ConfigureCommonEditorAppearance()
        {
            StartPosition = FormStartPosition.CenterScreen;
            // Use the same compact, unambiguous date presentation in every
            // InvoiceFormEx editor while preserving the time in the bound value.
            radDateTimePicker1.Format = DateTimePickerFormat.Custom;
            radDateTimePicker1.CustomFormat = "dd/MM/yyyy";

            // A line created by a physical dispenser sale keeps the pump/nozzle
            // from its SalesTransaction. Only manually-created lines may select a
            // different sale point. Apply this to the legacy and every fixed form.
            if (radDropDownList3 != null)
                radDropDownList3.DataBindings.Add("Enabled", invoiceLinesBindingSource,
                    "CanSelectSalePoint", true, DataSourceUpdateMode.Never);
            if (radDropDownList2 != null)
                radDropDownList2.DataBindings.Add("Enabled", invoiceLinesBindingSource,
                    "CanSelectSalePoint", true, DataSourceUpdateMode.Never);
        }

        private void ConfigureLineGridEditors()
        {
            if (radGridView1 == null)
                return;

            // Lookup columns use independent arrays. They must not inherit the
            // invoice BindingSource currency or be reset when the invoice header
            // and line bindings are refreshed.
            var fuelColumn = radGridView1.Columns["FuelType"] as GridViewComboBoxColumn;
            if (fuelColumn != null)
                fuelColumn.DataSource = (ivm.FuelTypes ?? new ViewModels.FuelTypeViewModel[0]).ToArray();
            var tankColumn = radGridView1.Columns["TankId"] as GridViewComboBoxColumn;
            if (tankColumn != null)
                tankColumn.DataSource = (ivm.Tanks ?? new ViewModels.TankViewModel[0]).ToArray();
            var dispenserColumn = radGridView1.Columns.OfType<GridViewComboBoxColumn>()
                .FirstOrDefault(column => IsDispenserColumn(column));
            if (dispenserColumn != null)
            {
                dispenserColumn.DisplayMember = "Description";
                dispenserColumn.ValueMember = "DispenserId";
                dispenserColumn.DataSource = (ivm.Dispensers ??
                    new ViewModels.DispenserViewModel[0]).ToArray();
            }
            var nozzleColumn = radGridView1.Columns.OfType<GridViewComboBoxColumn>()
                .FirstOrDefault(column => IsNozzleColumn(column));
            if (nozzleColumn != null)
            {
                // The column needs the complete list to render values belonging to
                // different dispensers. Only its active row editor is filtered.
                nozzleColumn.DisplayMember = "Description";
                nozzleColumn.ValueMember = "NozzleId";
                nozzleColumn.DataSource = AllNozzles();
            }
            radGridView1.CellEditorInitialized += InvoiceLineGrid_CellEditorInitialized;
        }

        private void InvoiceLineGrid_CellEditorInitialized(object sender, GridViewCellEventArgs e)
        {
            if (e.Column == null || e.Row == null)
                return;
            var line = e.Row.DataBoundItem as ViewModels.InvoiceLineViewModel;
            var editor = radGridView1.ActiveEditor as RadDropDownListEditor;
            var element = editor == null ? null : editor.EditorElement as RadDropDownListEditorElement;
            if (line == null || element == null)
                return;

            if (e.Column.Name == "FuelType")
            {
                element.DisplayMember = "Name";
                element.ValueMember = "FuelTypeId";
                element.DataSource = (ivm.FuelTypes ?? new ViewModels.FuelTypeViewModel[0]).ToArray();
            }
            else if (e.Column.Name == "TankId")
            {
                element.DisplayMember = "Description";
                element.ValueMember = "TankId";
                element.DataSource = (ivm.Tanks ?? new ViewModels.TankViewModel[0])
                    .Where(tank => tank.FuelTypeId == line.FuelTypeId)
                    .OrderBy(tank => tank.TankNumber)
                    .ToArray();
            }
            else if (IsDispenserColumn(e.Column))
            {
                element.DisplayMember = "Description";
                element.ValueMember = "DispenserId";
                element.DataSource = (ivm.Dispensers ??
                    new ViewModels.DispenserViewModel[0]).ToArray();
                element.SelectedValue = line.CurrentDispenserId;
            }
            else if (IsNozzleColumn(e.Column))
            {
                element.DisplayMember = "Description";
                element.ValueMember = "NozzleId";
                element.DataSource = NozzlesFor(line.CurrentDispenserId);
                // Reapply the row value after replacing the editor list. Telerik
                // otherwise presents row zero as the selected value.
                element.SelectedValue = line.CurrentNozzleId;
            }
        }

        private static bool IsDispenserColumn(GridViewColumn column)
        {
            return column != null &&
                (column.FieldName == "CurrentDispenserId" ||
                 column.Name == "Dispenser" || column.Name == "DispenserId");
        }

        private static bool IsNozzleColumn(GridViewColumn column)
        {
            return column != null &&
                (column.FieldName == "CurrentNozzleId" ||
                 column.Name == "Nozzle" || column.Name == "NozzleId");
        }

        private ViewModels.NozzleViewModel[] AllNozzles()
        {
            return (ivm.Dispensers ?? new ViewModels.DispenserViewModel[0])
                .Where(dispenser => dispenser.Nozzles != null)
                .SelectMany(dispenser => dispenser.Nozzles)
                .ToArray();
        }

        private ViewModels.NozzleViewModel[] NozzlesFor(Guid dispenserId)
        {
            var dispenser = (ivm.Dispensers ?? new ViewModels.DispenserViewModel[0])
                .FirstOrDefault(item => item.DispenserId == dispenserId);
            return dispenser == null || dispenser.Nozzles == null
                ? new ViewModels.NozzleViewModel[0]
                : dispenser.Nozzles.ToArray();
        }

        public void CreateNew()
        {
            invoiceTypeBindingValid = true;
            ivm.CreateNew();
            ResetEditorBindings();
            SetView();
        }

        public void CreateNew(Guid invoiceTypeId)
        {
            if (!EnsureInvoiceTypeAvailable(invoiceTypeId))
            {
                DisableInvoiceActions();
                return;
            }
            invoiceTypeBindingValid = true;
            ExecuteWithInvoiceTypeBindingReadOnly(() =>
            {
                ivm.CreateNew();
                ivm.InvoiceTypeId = invoiceTypeId;
            });
            ResetEditorBindings();
            SetView();
            ScheduleInvoiceTypeValidation(invoiceTypeId, false);
        }

        public void LoadInvoice(Guid invoiceId)
        {
            invoiceTypeBindingValid = true;
            ExecuteWithPaymentTypeBindingsReadOnly(() =>
                ExecuteWithInvoiceTypeBindingReadOnly(() => ivm.Load(invoiceId)));
            var loadedInvoiceTypeId = ivm.InvoiceTypeId;

            ResetEditorBindings();
            this.panel6.Visible = false;
            ApplyPriceBindings();
            this.SetView();
            ScheduleInvoiceTypeValidation(loadedInvoiceTypeId, false);
            ScheduleLookupValidation();
            if (fiscalStatusVisible) fiscalPanel.LoadInvoice(invoiceId);
            if (myDieselVisible) myDieselPanel.LoadInvoice(invoiceId);
        }

        private void PrepareInvoiceTypeBinding()
        {
            // Do not make the type list a child of the invoice BindingSource.
            // ResetBindings on the parent resets a child list's currency; Telerik
            // then selects row zero and the two-way SelectedValue binding writes
            // that unrelated type back to InvoiceTypeId.
            var valueBinding = radDropDownList1.DataBindings["SelectedValue"];
            if (valueBinding != null)
            {
                invoiceTypeBindingUpdateMode = valueBinding.DataSourceUpdateMode;
                invoiceTypeBindingUpdateModeCaptured = true;
                valueBinding.DataSourceUpdateMode = DataSourceUpdateMode.Never;
            }
            invoiceTypesBindingSource.DataSource = null;
            invoiceTypesBindingSource.DataMember = string.Empty;
            invoiceTypesBindingSource.DataSource = new ViewModels.InvoiceTypeViewModel[0];
            radDropDownList1.DataSource = invoiceTypesBindingSource;
        }

        private void PreparePaymentTypeBinding()
        {
            // Payment methods are an independent lookup. They must not be a
            // child of the invoice BindingSource because ResetBindings moves a
            // child list to row zero and can write that value into PaymentType.
            ExecuteWithPaymentTypeBindingsReadOnly(() =>
            {
                paymentTypesBindingSource.DataSource = null;
                paymentTypesBindingSource.DataMember = string.Empty;
                paymentTypesBindingSource.DataSource =
                    (ivm.PaymentTypes ?? new Common.Enumerators.EnumItem[0]).ToArray();
            });
        }

        private void PrepareLineSalePointBindings()
        {
            if (radDropDownList3 == null || radDropDownList2 == null)
                return;
            ExecuteWithLineLookupBindingsReadOnly(() =>
            {
                // Do not use a dispenser child BindingSource for nozzles. Moving
                // parent currency can select row zero and write it through the
                // two-way CurrentNozzleId binding. Both lists are independent;
                // the nozzle list is replaced explicitly for the current line.
                dispensersBindingSource.DataSource = null;
                dispensersBindingSource.DataMember = string.Empty;
                dispensersBindingSource.DataSource = (ivm.Dispensers ??
                    new ViewModels.DispenserViewModel[0]).ToArray();
                radDropDownList3.DataSource = dispensersBindingSource;

                nozzlesBindingSource.DataSource = null;
                nozzlesBindingSource.DataMember = string.Empty;
                nozzlesBindingSource.DataSource = new ViewModels.NozzleViewModel[0];
                radDropDownList2.DataSource = nozzlesBindingSource;
                nozzleLookupInitialized = false;
            });
        }

        private void SynchronizeInvoiceTypeBinding()
        {
            var selectedTypeId = ivm.InvoiceTypeId;
            // InvoiceTypes is the view model's scenario/layout/permission-filtered
            // list. Do not fall back to the global cache here: that would expose
            // types which do not belong in this editor.
            var visibleTypes = ivm.InvoiceTypes ?? new ViewModels.InvoiceTypeViewModel[0];
            var valueBinding = radDropDownList1.DataBindings["SelectedValue"];
            var updateMode = valueBinding == null
                ? DataSourceUpdateMode.Never
                : valueBinding.DataSourceUpdateMode;
            try
            {
                // Replacing/resetting the list is a control refresh, not a user edit.
                // Disable control-to-model writes until the original value is restored.
                if (valueBinding != null)
                    valueBinding.DataSourceUpdateMode = DataSourceUpdateMode.Never;
                invoiceTypesBindingSource.DataSource = visibleTypes;
                if (selectedTypeId == Guid.Empty)
                    radDropDownList1.SelectedIndex = -1;
                else
                {
                    var selectedIndex = Array.FindIndex(visibleTypes,
                        item => item.InvoiceTypeId == selectedTypeId);
                    if (selectedIndex < 0)
                    {
                        radDropDownList1.SelectedIndex = -1;
                    }
                    else
                    {
                        invoiceTypesBindingSource.Position = selectedIndex;
                        radDropDownList1.SelectedIndex = selectedIndex;
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
            // Preserve the model value before resetting the parent BindingSource.
            // Telerik can move SelectedValue to item zero while ResetBindings is
            // rebuilding the control. The binding must be read-only for that entire
            // operation, not only while the independent lookup list is replaced.
            var selectedTypeId = ivm.InvoiceTypeId;
            var selectedPaymentType = ivm.PaymentType;
            ExecuteWithPaymentTypeBindingsReadOnly(() =>
            {
                ExecuteWithInvoiceTypeBindingReadOnly(() =>
                {
                    ExecuteWithLineLookupBindingsReadOnly(() =>
                    {
                        this.invoiceViewModelBindingSource.ResetBindings(false);
                        this.invoiceLinesBindingSource.ResetBindings(false);

                        // ResetBindings is a presentation refresh and is never allowed
                        // to alter lookup values. Reassert the captured invariants
                        // before restoring normal two-way binding.
                        if (ivm.InvoiceTypeId != selectedTypeId)
                            ivm.InvoiceTypeId = selectedTypeId;
                        if (ivm.PaymentType != selectedPaymentType)
                            ivm.PaymentType = selectedPaymentType;
                        SynchronizeInvoiceTypeBinding();
                        SynchronizePaymentTypeBindings();
                    });
                });
            });
        }

        private RadDropDownList[] GetPaymentTypeControls()
        {
            return new[] { paymentTypeDropDownList, radDropDownList4 }
                .Where(control => control != null && !control.IsDisposed &&
                    control.DataBindings["SelectedValue"] != null)
                .Distinct()
                .ToArray();
        }

        private void ExecuteWithPaymentTypeBindingsReadOnly(Action action)
        {
            var bindings = GetPaymentTypeControls()
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

        private void SynchronizePaymentTypeBindings()
        {
            ExecuteWithPaymentTypeBindingsReadOnly(() =>
            {
                int selectedIndex = -1;
                var paymentTypes = ivm.PaymentTypes ??
                    new Common.Enumerators.EnumItem[0];
                if (ivm.PaymentType.HasValue)
                {
                    selectedIndex = Array.FindIndex(paymentTypes,
                        item => item.Value == ivm.PaymentType.Value);
                }

                // RadDropDownList follows the CurrencyManager of its BindingSource.
                // Setting only SelectedIndex is temporary: when the form completes
                // binding, the list's unchanged Position (normally zero) wins and
                // makes every saved invoice display the first payment method.
                // Move the lookup currency and each visible editor atomically.
                paymentTypesBindingSource.Position = selectedIndex;
                foreach (var control in GetPaymentTypeControls())
                    control.SelectedIndex = selectedIndex;
            });
        }

        private void ExecuteWithLineLookupBindingsReadOnly(Action action)
        {
            var dispenserBinding = radDropDownList3 == null
                ? null
                : radDropDownList3.DataBindings["SelectedValue"];
            var nozzleBinding = radDropDownList2 == null
                ? null
                : radDropDownList2.DataBindings["SelectedValue"];
            var dispenserMode = dispenserBinding == null
                ? DataSourceUpdateMode.Never
                : dispenserBinding.DataSourceUpdateMode;
            var nozzleMode = nozzleBinding == null
                ? DataSourceUpdateMode.Never
                : nozzleBinding.DataSourceUpdateMode;
            try
            {
                // Telerik selects lookup row zero while a parent BindingSource is
                // reset. That is a presentation event, never a user edit.
                if (dispenserBinding != null)
                    dispenserBinding.DataSourceUpdateMode = DataSourceUpdateMode.Never;
                if (nozzleBinding != null)
                    nozzleBinding.DataSourceUpdateMode = DataSourceUpdateMode.Never;
                action();
                SynchronizeLineLookupBindings();
            }
            finally
            {
                var currentLine = invoiceLinesBindingSource.Current as ViewModels.InvoiceLineViewModel;
                if (currentLine == null)
                    currentLine = (ivm.InvoiceLines ?? new ViewModels.InvoiceLineViewModel[0]).FirstOrDefault();
                bool allowSalePointChange = ivm.DetailsEditEnabled &&
                    (currentLine == null || currentLine.CanSelectSalePoint);
                if (dispenserBinding != null)
                    dispenserBinding.DataSourceUpdateMode = allowSalePointChange
                        ? dispenserMode
                        : DataSourceUpdateMode.Never;
                if (nozzleBinding != null)
                    nozzleBinding.DataSourceUpdateMode = allowSalePointChange
                        ? nozzleMode
                        : DataSourceUpdateMode.Never;
            }
        }

        private void SynchronizeLineLookupBindings()
        {
            if (radDropDownList3 == null || radDropDownList2 == null)
                return;
            var line = invoiceLinesBindingSource.Current as ViewModels.InvoiceLineViewModel;
            if (line == null)
                line = (ivm.InvoiceLines ?? new ViewModels.InvoiceLineViewModel[0]).FirstOrDefault();
            var dispenserId = line == null ? Guid.Empty : line.CurrentDispenserId;
            if (!nozzleLookupInitialized || !object.ReferenceEquals(nozzleLookupLine, line) ||
                nozzleLookupDispenserId != dispenserId)
            {
                nozzlesBindingSource.DataSource = NozzlesFor(dispenserId);
                nozzleLookupLine = line;
                nozzleLookupDispenserId = dispenserId;
                nozzleLookupInitialized = true;
            }
            SelectLookupValue(radDropDownList3,
                dispenserId);
            SelectLookupValue(radDropDownList2,
                line == null ? Guid.Empty : line.CurrentNozzleId);
        }

        private static void SelectLookupValue(RadDropDownList control, Guid value)
        {
            SelectLookupValue(control, (object)value);
        }

        private static void SelectLookupValue(RadDropDownList control, object value)
        {
            int selectedIndex = -1;
            if (value != null && !object.Equals(value, Guid.Empty))
            {
                for (int index = 0; index < control.Items.Count; index++)
                {
                    if (LookupValuesEqual(control.Items[index].Value, value))
                    {
                        selectedIndex = index;
                        break;
                    }
                }
            }
            control.SelectedIndex = selectedIndex;
        }

        private static bool LookupValuesEqual(object first, object second)
        {
            if (first == null || first == DBNull.Value || second == null || second == DBNull.Value)
                return first == null && second == null;
            if (object.Equals(first, second))
                return true;
            try
            {
                if (first is Guid || second is Guid)
                    return new Guid(Convert.ToString(first)) == new Guid(Convert.ToString(second));
                return Convert.ToInt64(first) == Convert.ToInt64(second);
            }
            catch
            {
                return string.Equals(Convert.ToString(first), Convert.ToString(second),
                    StringComparison.OrdinalIgnoreCase);
            }
        }

        private void ExecuteWithInvoiceTypeBindingReadOnly(Action action)
        {
            var valueBinding = radDropDownList1.DataBindings["SelectedValue"];
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

        private T ExecuteWithInvoiceTypeBindingReadOnly<T>(Func<T> action)
        {
            T result = default(T);
            ExecuteWithInvoiceTypeBindingReadOnly(() => { result = action(); });
            return result;
        }

        private void RestoreInvoiceTypeBindingUpdateMode()
        {
            if (!invoiceTypeBindingUpdateModeCaptured)
                return;
            var valueBinding = radDropDownList1.DataBindings["SelectedValue"];
            if (valueBinding != null)
                valueBinding.DataSourceUpdateMode = invoiceTypeBindingUpdateMode;
            invoiceTypeBindingUpdateModeCaptured = false;
        }

        private void ScheduleInvoiceTypeValidation(Guid invoiceTypeId,
            bool cancelTransformationOnFailure)
        {
            expectedInvoiceTypeId = invoiceTypeId;
            cancelTransformationOnTypeMismatch = cancelTransformationOnFailure;
            if (invoiceTypeControlLoaded)
                ValidateExpectedInvoiceType();
        }

        private void ValidateExpectedInvoiceType()
        {
            if (expectedInvoiceTypeId == Guid.Empty)
                return;
            var expected = expectedInvoiceTypeId;
            var cancelTransformation = cancelTransformationOnTypeMismatch;
            expectedInvoiceTypeId = Guid.Empty;
            cancelTransformationOnTypeMismatch = false;

            ExecuteWithInvoiceTypeBindingReadOnly(() =>
            {
                if (ivm.InvoiceTypeId != expected)
                    ivm.InvoiceTypeId = expected;
                SynchronizeInvoiceTypeBinding();
            });
            if (VerifyInvoiceTypeSelection(expected))
                return;

            DisableInvoiceActions();
            if (!cancelTransformation)
                return;
            DeleteRelatedInvoices();
            if (IsHandleCreated)
                BeginInvoke(new Action(Close));
        }

        private bool EnsureInvoiceTypeAvailable(Guid invoiceTypeId)
        {
            if (invoiceTypeId != Guid.Empty &&
                (ivm.InvoiceTypes ?? new ViewModels.InvoiceTypeViewModel[0])
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
            var selectedItem = radDropDownList1.SelectedItem == null
                ? null
                : radDropDownList1.SelectedItem.DataBoundItem as ViewModels.InvoiceTypeViewModel;
            if (ivm.InvoiceTypeId == expectedInvoiceTypeId && selectedItem != null &&
                selectedItem.InvoiceTypeId == expectedInvoiceTypeId)
                return true;

            var expectedType = ivm.InvoiceTypes
                .Single(item => item.InvoiceTypeId == expectedInvoiceTypeId);
            RadMessageBox.Show(
                "Δεν ήταν δυνατή η σύνδεση του επιλεγμένου τύπου παραστατικού \"" +
                expectedType.Description + "\" με τη φόρμα. Η ενέργεια ακυρώθηκε.",
                "Σφάλμα Τύπου Παραστατικού", MessageBoxButtons.OK, RadMessageIcon.Error);
            return false;
        }

        private void ScheduleLookupValidation()
        {
            validateLookupsAfterLoad = true;
            if (!invoiceTypeControlLoaded || !invoiceTypeBindingValid)
                return;
            validateLookupsAfterLoad = false;
            ValidateEditorLookupSelections(true);
        }

        private bool ValidateEditorLookupSelections(bool showMessage)
        {
            var errors = new List<string>();
            var invoiceTypes = ivm.InvoiceTypes ?? new ViewModels.InvoiceTypeViewModel[0];
            var selectedInvoiceType = invoiceTypes.FirstOrDefault(
                item => item.InvoiceTypeId == ivm.InvoiceTypeId);
            if (ivm.InvoiceTypeId == Guid.Empty || selectedInvoiceType == null)
                errors.Add("Ο τύπος παραστατικού δεν ανήκει στη διαθέσιμη λίστα της φόρμας.");
            else if (radDropDownList1 == null ||
                !LookupValuesEqual(radDropDownList1.SelectedValue, ivm.InvoiceTypeId))
                errors.Add("Ο τύπος παραστατικού της φόρμας δεν συμφωνεί με το παραστατικό.");

            var paymentTypes = ivm.PaymentTypes ?? new Common.Enumerators.EnumItem[0];
            bool validPayment = ivm.PaymentType.HasValue && paymentTypes.Any(item =>
            {
                try { return Convert.ToInt32(item.Value) == ivm.PaymentType.Value; }
                catch { return false; }
            });
            if (!validPayment)
            {
                errors.Add("Δεν έχει επιλεγεί έγκυρος τρόπος πληρωμής.");
            }
            else
            {
                foreach (var control in GetPaymentTypeControls())
                {
                    if (!LookupValuesEqual(control.SelectedValue, ivm.PaymentType.Value))
                    {
                        errors.Add("Ο τρόπος πληρωμής της φόρμας δεν συμφωνεί με το παραστατικό.");
                        break;
                    }
                }
            }

            var fuelTypes = ivm.FuelTypes ?? new ViewModels.FuelTypeViewModel[0];
            var tanks = ivm.Tanks ?? new ViewModels.TankViewModel[0];
            var dispensers = ivm.Dispensers ?? new ViewModels.DispenserViewModel[0];
            var lines = ivm.InvoiceLines ?? new ViewModels.InvoiceLineViewModel[0];
            bool usesSalePoint = editorLayout == ViewModels.InvoiceEditorLayout.RetailReceipt ||
                (editorLayout == ViewModels.InvoiceEditorLayout.Legacy &&
                    (ivm.SimplePrint || ivm.DispenserType));

            for (int index = 0; index < lines.Length; index++)
            {
                var line = lines[index];
                string prefix = "Γραμμή " + (index + 1).ToString() + ": ";
                if (line.FuelTypeId == Guid.Empty ||
                    !fuelTypes.Any(item => item.FuelTypeId == line.FuelTypeId))
                    errors.Add(prefix + "το καύσιμο δεν ανήκει στη διαθέσιμη λίστα.");

                if (line.TankId.HasValue && line.TankId.Value != Guid.Empty)
                {
                    var tank = tanks.FirstOrDefault(item => item.TankId == line.TankId.Value);
                    if (tank == null)
                        errors.Add(prefix + "η δεξαμενή δεν ανήκει στη διαθέσιμη λίστα.");
                    else if (tank.FuelTypeId != line.FuelTypeId)
                        errors.Add(prefix + "η δεξαμενή δεν αντιστοιχεί στο επιλεγμένο καύσιμο.");
                }

                if (!usesSalePoint)
                    continue;
                var dispenser = dispensers.FirstOrDefault(
                    item => item.DispenserId == line.CurrentDispenserId);
                if (line.CurrentDispenserId == Guid.Empty || dispenser == null)
                {
                    errors.Add(prefix + "δεν έχει επιλεγεί έγκυρη αντλία.");
                    continue;
                }
                var nozzles = dispenser.Nozzles ?? new ViewModels.NozzleViewModel[0];
                var nozzle = nozzles.FirstOrDefault(
                    item => item.NozzleId == line.CurrentNozzleId);
                if (line.CurrentNozzleId == Guid.Empty || nozzle == null)
                    errors.Add(prefix + "το ακροσωλήνιο δεν ανήκει στην επιλεγμένη αντλία.");
                else if (nozzle.FuelTypeId != line.FuelTypeId)
                    errors.Add(prefix + "το καύσιμο δεν συμφωνεί με το καύσιμο του ακροσωληνίου.");
                if (line.SalesTransactionView != null &&
                    line.SalesTransactionView.NozzleId != Guid.Empty &&
                    line.SalesTransactionView.NozzleId != line.CurrentNozzleId)
                    errors.Add(prefix + "το ακροσωλήνιο δεν συμφωνεί με τη συνδεδεμένη πώληση.");
            }

            if (usesSalePoint && lines.Length > 0)
            {
                var currentLine = invoiceLinesBindingSource.Current as ViewModels.InvoiceLineViewModel;
                if (currentLine == null)
                    currentLine = lines[0];
                if (radDropDownList3 == null ||
                    !LookupValuesEqual(radDropDownList3.SelectedValue,
                        currentLine.CurrentDispenserId))
                    errors.Add("Η αντλία της φόρμας δεν συμφωνεί με την επιλεγμένη γραμμή.");
                if (radDropDownList2 == null ||
                    !LookupValuesEqual(radDropDownList2.SelectedValue,
                        currentLine.CurrentNozzleId))
                    errors.Add("Το ακροσωλήνιο της φόρμας δεν συμφωνεί με την επιλεγμένη γραμμή.");
            }

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
            invoiceTypeBindingValid = false;
            if (btnSave != null)
                btnSave.Enabled = false;
            if (btnPrint != null)
                btnPrint.Enabled = false;
        }

        public Guid TransformInvoices(Guid[] invoiceIds, Guid invoiceType, Guid? traderId, Guid? vehicleId,
            int mode, bool createFiscalRelations = true)
        {
            invoiceTypeBindingValid = true;
            if (!EnsureInvoiceTypeAvailable(invoiceType))
            {
                DeleteRelatedInvoices();
                return Guid.Empty;
            }
            guideOnlyFinancialEdit = !createFiscalRelations ||
                relatedIds != null && relatedIds.Length > 0;
            var invoiceId = ExecuteWithInvoiceTypeBindingReadOnly<Guid>(() =>
                ivm.TransformInvoices(invoiceIds, invoiceType, traderId, vehicleId, mode, createFiscalRelations));
            if (invoiceId != Guid.Empty && guideOnlyFinancialEdit)
                ivm.EnableGuideOnlyTransformationFinancialEditing();
            if (invoiceId == Guid.Empty)
                DeleteRelatedInvoices();
            ResetEditorBindings();
            ApplyPriceBindings();
            SetView();
            if (invoiceId != Guid.Empty)
            {
                ScheduleInvoiceTypeValidation(invoiceType, true);
                ScheduleLookupValidation();
            }
            return invoiceId;
        }

        public Guid[] CreateIntermediateTransformation(Guid[] invoiceIds, Guid invoiceType,
            Guid? traderId, Guid? vehicleId, int mode, string notesAddition)
        {
            var newIds = new List<Guid>();
            try
            {
                foreach (var sourceId in invoiceIds)
                {
                    var related = new ViewModels.InvoiceDetailsViewModel();
                    var relatedId = related.TransformInvoices(new[] { sourceId }, invoiceType,
                        traderId, vehicleId, mode);
                    if (relatedId == Guid.Empty)
                        throw new InvalidOperationException("Δεν ήταν δυνατή η δημιουργία του πιστωτικού παραστατικού.");
                    if (!string.IsNullOrWhiteSpace(notesAddition))
                        related.Notes = string.IsNullOrWhiteSpace(related.Notes)
                            ? notesAddition
                            : notesAddition + "\r\n" + related.Notes;
                    lock (Threads.PrintAgent.ExcludeInvoices)
                        Threads.PrintAgent.ExcludeInvoices.Add(relatedId);
                    newIds.Add(relatedId);
                    if (!related.Save(relatedId))
                        throw new InvalidOperationException(string.Join("\r\n",
                            (related.Errors ?? new ViewModels.ValidationError[0]).Select(error => error.ErrorMessage)));
                }
                this.relatedIds = newIds.ToArray();
                return this.relatedIds;
            }
            catch
            {
                this.relatedIds = newIds.ToArray();
                DeleteRelatedInvoices();
                throw;
            }
        }

        private bool SaveInvoice()
        {
            lookupValidationMessageShown = false;
            CommitEditorValues();
            if (!ValidateEditorLookupSelections(true))
            {
                lookupValidationMessageShown = true;
                return false;
            }
            var success = this.ivm.Save(this.ivm.InvoiceId);
            if (success)
            {
                var savedRelatedIds = this.relatedIds ?? new Guid[0];
                this.pendingRelatedProcessingIds = this.pendingRelatedProcessingIds
                    .Concat(savedRelatedIds)
                    .Distinct()
                    .ToArray();
                lock (Threads.PrintAgent.ExcludeInvoices)
                {
                    foreach (var id in savedRelatedIds)
                        Threads.PrintAgent.ExcludeInvoices.Remove(id);
                }
                this.relatedIds = null;
            }
            return success;
        }

        private void ProcessPendingRelatedInvoices()
        {
            var pendingIds = this.pendingRelatedProcessingIds
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToArray();
            if (pendingIds.Length == 0)
                return;

            foreach (var relatedId in pendingIds)
            {
                try
                {
                    if (Threads.SharedInvoiceRuntime.Handles(relatedId))
                    {
                        var result = Threads.SharedInvoiceRuntime.Process(relatedId, true);
                        if (result == null || !result.ReadyToPrint)
                        {
                            string detail = result == null
                                ? "Δεν επιστράφηκε αποτέλεσμα έκδοσης."
                                : ASFuelControl.Windows.UI.Controls.FiscalDocumentPanel.ResultMessage(result);
                            throw new InvalidOperationException(detail);
                        }
                    }
                    else
                    {
                        using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
                        {
                            var related = db.Invoices.SingleOrDefault(invoice =>
                                invoice.InvoiceId == relatedId);
                            if (related == null)
                                throw new InvalidOperationException(
                                    "Το συνδεδεμένο πιστωτικό παραστατικό δεν βρέθηκε.");

                            // The legacy path is not journal-idempotent. Do not call
                            // the device/provider a second time if the polling worker
                            // completed the credit immediately after it was released.
                            if (related.IsPrinted != true)
                                new Threads.PrintAgent().SignInvoice(db, related);
                            if (related.IsPrinted != true)
                                throw new InvalidOperationException(
                                    "Το πιστωτικό δεν ολοκλήρωσε τη σήμανση ή τη διαβίβασή του.");
                        }
                    }

                    this.pendingRelatedProcessingIds = this.pendingRelatedProcessingIds
                        .Where(id => id != relatedId)
                        .ToArray();
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        "Δεν ολοκληρώθηκε η έκδοση του συνδεδεμένου πιστωτικού παραστατικού. " +
                        "Το τελικό παραστατικό δεν εκδόθηκε.\r\n" + ex.Message, ex);
                }
            }
        }

        private void ShowSaveErrors()
        {
            if (lookupValidationMessageShown)
                return;
            string message = string.Join("\r\n", this.ivm.Errors.Select(s => s.ErrorMessage));
            if (!string.IsNullOrWhiteSpace(message))
                RadMessageBox.Show(message, "Σφάλμα Αποθήκευσης", MessageBoxButtons.OK,
                    RadMessageIcon.Exclamation);
        }

        private void CommitEditorValues()
        {
            // A toolbar click does not necessarily move focus out of a Telerik
            // dropdown/grid editor. Push every visible editor into the view model
            // before deciding whether the invoice has changes or saving it.
            if (this.radGridView1 != null)
                this.radGridView1.EndEdit();
            this.ValidateChildren();
            var dateBinding = this.radDateTimePicker1 == null
                ? null
                : this.radDateTimePicker1.DataBindings["Value"];
            if (dateBinding != null)
                dateBinding.WriteValue();
            if (this.invoiceLinesBindingSource != null)
                this.invoiceLinesBindingSource.EndEdit();
            if (this.invoiceViewModelBindingSource != null)
                this.invoiceViewModelBindingSource.EndEdit();
        }

        private void DeleteRelatedInvoices()
        {
            if (this.relatedIds == null || this.relatedIds.Length == 0)
                return;
            using (Threads.SharedInvoiceRuntime.EditUnissued(this.relatedIds))
            using (Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                foreach (var invoiceId in this.relatedIds)
                {
                    var invoice = db.Invoices.SingleOrDefault(item => item.InvoiceId == invoiceId);
                    if (invoice == null)
                        continue;
                    foreach (var line in invoice.InvoiceLines.ToArray())
                    {
                        if (line.InvoiceLineRelations_ChildRelationId.Count > 0)
                            db.Delete(line.InvoiceLineRelations_ChildRelationId);
                        if (line.InvoiceLineRelations_ParentLineId.Count > 0)
                            db.Delete(line.InvoiceLineRelations_ParentLineId);
                    }
                    if (invoice.ParentInvoiceRelations.Count > 0)
                        db.Delete(invoice.ParentInvoiceRelations);
                    if (invoice.ChildInvoiceRelations.Count > 0)
                        db.Delete(invoice.ChildInvoiceRelations);
                    if (invoice.FinTransactions.Count > 0)
                        db.Delete(invoice.FinTransactions);
                    if (invoice.InvoiceLines.Count > 0)
                        db.Delete(invoice.InvoiceLines);
                    db.Delete(invoice);
                }
                db.SaveChanges();
            }
            lock (Threads.PrintAgent.ExcludeInvoices)
            {
                foreach (var id in this.relatedIds)
                    Threads.PrintAgent.ExcludeInvoices.Remove(id);
            }
            this.relatedIds = null;
        }

        private void InvoiceFormEx_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (fiscalPanel.Busy || (myDieselVisible && myDieselPanel.Busy))
            {
                e.Cancel = true;
                RadMessageBox.Show(this,"Υπάρχει ενέργεια έκδοσης σε εξέλιξη. Περιμένετε να ολοκληρωθεί πριν κλείσετε το παραστατικό.",
                    "Κλείσιμο παραστατικού",MessageBoxButtons.OK,RadMessageIcon.Info);
                return;
            }
            if (myDieselVisible && myDieselPanel.HasBlockingCase && !fiscalPanel.Owned &&
                RadMessageBox.Show(this,"Η υπόθεση myDiesel παραμένει ενεργή ή χρειάζεται ανάκτηση. Επιλέξτε «Όχι» για να επιστρέψετε στις διαθέσιμες ενέργειες ή «Ναι» για να την αφήσετε καταγεγραμμένη και να κλείσετε.",
                    "Ενεργή υπόθεση myDiesel",MessageBoxButtons.YesNo,RadMessageIcon.Exclamation) != DialogResult.Yes)
            { e.Cancel = true; return; }
            try { CloseInvoice(e); }
            catch (Exception ex)
            {
                e.Cancel = true;
                RadMessageBox.Show(ex.Message,"Έλεγχος παραστατικού",MessageBoxButtons.OK,RadMessageIcon.Exclamation);
            }
        }

        private void CloseInvoice(FormClosingEventArgs e)
        {
            if (Threads.SharedInvoiceRuntime.Owns(this.ivm.InvoiceId)) return;
            if (!this.ivm.HasChanges)
            {
                DeleteRelatedInvoices();
                return;
            }
            var res = RadMessageBox.Show(this,"Θέλετε να αποθηκεύσετε τις αλλαγές?", "Εχούν γίνει αλλαγές...", MessageBoxButtons.YesNoCancel, RadMessageIcon.Question);
            if (res == DialogResult.Cancel)
            {
                e.Cancel = true;
            }
            else if (res == DialogResult.Yes)
            {
                var success = this.SaveInvoice();
                if (!success)
                {
                    ShowSaveErrors();
                    e.Cancel = true;
                }
            }
            else if(res == DialogResult.No)
            {
                DeleteRelatedInvoices();
            }
        }

        private void btnSelectTrader_Click(object sender, EventArgs e)
        {
            SelectionForms.TraderSelectForm tf = new SelectionForms.TraderSelectForm();
            tf.SelectionMode = this.ivm.CounterpartySelection;
            tf.ShowDialog(this);
            ViewModels.TraderViewModel tvm = tf.SelectedEntity as ViewModels.TraderViewModel;
            if (tvm != null)
                this.ivm.TraderId = tvm.TraderId;
        }

        private void btnSelectVehicle_Click(object sender, EventArgs e)
        {
            SelectionForms.VehicleSelectForm tf = new SelectionForms.VehicleSelectForm();
            tf.ShowDialog(this);
            ViewModels.VehicleViewModel tvm = tf.SelectedEntity as ViewModels.VehicleViewModel;
            if (tvm != null)
                this.ivm.VehicleId = tvm.VehicleId;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (Threads.SharedInvoiceRuntime.Owns(this.ivm.InvoiceId)) return;
            
            var success = this.SaveInvoice();
            if (!success)
                ShowSaveErrors();
            else if (fiscalStatusVisible) fiscalPanel.LoadInvoice(this.ivm.InvoiceId);
        }

        private void btnDeleteVehicle_Click(object sender, EventArgs e)
        {
            if (this.radGridView1.CurrentRow == null)
                return;
            var invLine = this.radGridView1.CurrentRow.DataBoundItem as ViewModels.InvoiceLineViewModel;
            if (invLine == null)
                return;
            if(!invLine.CanDelete)
            {
                RadMessageBox.Show("Η επιλεγμένη εγγραφή περιέχει συνδέσεις.\r\nΔεν μπορεί να γίνει διαγραφή!", "Ενημέρωση διαγραφής γραμμής Παραστατικού", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
                return;
            }
            if (RadMessageBox.Show("Θέλετε να διαγράψετε την επιλεγμένη εγγραφή", "Διαγραφή Γραμμής Παραστατικού", MessageBoxButtons.YesNo, RadMessageIcon.Question) == DialogResult.No)
                return;
            this.ivm.DeleteInvoiceLine(invLine.InvoiceLineId);
        }

        private void btnAddVehicle_Click(object sender, EventArgs e)
        {
            this.ivm.AddInvoiceLine();
        }

        private void radGridView1_CellBeginEdit(object sender, GridViewCellCancelEventArgs e)
        {
            if (e.Row == null)
                return;
            var line = e.Row.DataBoundItem as ViewModels.InvoiceLineViewModel;
            if (line == null)
                return;
            bool financialColumn = e.Column != null &&
                (e.Column.Name == "UnitPrice" ||
                 e.Column.Name == "DiscountAmountView" ||
                 e.Column.FieldName == "UnitPriceView" ||
                 e.Column.FieldName == "DiscountAmountView");
            bool volumeColumn = e.Column != null &&
                (e.Column.Name == "Volume" || e.Column.FieldName == "Volume");
            // A transformed guide may override price/discount. One-to-many
            // transformations may also select the target quantity here; fuel and
            // the physical sale identity remain immutable.
            e.Cancel = !line.CanEdit &&
                !(financialColumn &&
                    (line.CanEditTransformationFinancials || guideOnlyFinancialEdit)) &&
                !(volumeColumn && line.CanEditTransformationVolume);
        }

        private async void btnPrint_Click(object sender, EventArgs e)
        {
            if (fiscalPanel.Busy || (myDieselVisible && myDieselPanel.Busy)) return;
            if (!Threads.SharedInvoiceRuntime.Owns(this.ivm.InvoiceId))
            {
                CommitEditorValues();
                lookupValidationMessageShown = false;
                if (!ValidateEditorLookupSelections(true))
                {
                    lookupValidationMessageShown = true;
                    return;
                }
            }
            // Owned documents use their saved workflow; never save/recalculate edited
            // screen data before a reprint. The status panel locks issued fields.
            if (Threads.SharedInvoiceRuntime.Handles(this.ivm.InvoiceId))
            {
                try
                {
                    if (!Threads.SharedInvoiceRuntime.Owns(this.ivm.InvoiceId))
                    {
                        if (this.ivm.HasChanges && !this.SaveInvoice())
                        {
                            if (lookupValidationMessageShown)
                                return;
                            throw new InvalidOperationException(string.Join("\r\n", this.ivm.Errors.Select(s => s.ErrorMessage)));
                        }
                    }
                    ProcessPendingRelatedInvoices();
                    fiscalPanel.LoadInvoice(this.ivm.InvoiceId);
                    await fiscalPanel.PrintInvoice();
                }
                catch (Exception ex)
                {
                    Logging.Logger.Instance.LogToFile("Shared invoice print", ex);
                    RadMessageBox.Show(ex.Message, "Σφάλμα Εκτύπωσης", MessageBoxButtons.OK, RadMessageIcon.Error);
                }
                return;
            }
            if (this.ivm.HasChanges)
            {
                bool success = this.SaveInvoice();
                if (!success)
                {
                    ShowSaveErrors();
                    return;
                }
            }
            try
            {
                ProcessPendingRelatedInvoices();
            }
            catch (Exception ex)
            {
                Logging.Logger.Instance.LogToFile("Related credit issue", ex);
                RadMessageBox.Show(ex.Message, "Σφάλμα Έκδοσης Πιστωτικού",
                    MessageBoxButtons.OK, RadMessageIcon.Error);
                return;
            }
            using (Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                try
                {
                    Data.Invoice invoice = db.Invoices.SingleOrDefault(i => i.InvoiceId == this.ivm.InvoiceId);
                    if (invoice == null)
                        return;
                    
                    if (invoice.InvoiceLines.Count == 0)
                    {
                        Telerik.WinControls.RadMessageBox.Show("Το επιλεγμένο Παραστατικό δεν περιέχει γραμμές παραστατικού", "Σφάλμα εκτύπωσης", MessageBoxButtons.OK, Telerik.WinControls.RadMessageIcon.Error);
                        return;
                    }
                    string selectedPrinter;
                    if (!InvoicePrintConfirmationDialog.Confirm(this,
                        "Θέλετε να εκτυπώσετε το επιλεγμένο Παραστατικό;",
                        InvoicePrintConfirmationDialog.ResolvePrinter(invoice),out selectedPrinter))
                        return;
                    invoice.RecalculateInvoice();
                    db.SaveChanges();
                    if (invoice.InvoiceSignature == null || invoice.InvoiceSignature == "")
                    {
                        Threads.PrintAgent pa = new Threads.PrintAgent();
                        pa.SignInvoice(db, invoice,selectedPrinter);
                    }
                    else
                        Threads.PrintAgent.PrintInvoiceDirect(invoice,selectedPrinter);
                }
                catch(Exception ex)
                {
                    Logging.Logger.Instance.LogToFile("Print", ex);

                }
            }
        }

        private void Ivm_QueryInvoiceData(object sender, ViewModels.QueryInvoiceDataArgs e)
        {
            // The target document is edited in this form. The former
            // InvoiceTransformationForm duplicated trader, vehicle and financial
            // fields and is intentionally no longer shown.
        }

        private void Ivm_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "InvoiceTypes")
                SynchronizeInvoiceTypeBinding();
            if (e.PropertyName == "PaymentTypes")
            {
                PreparePaymentTypeBinding();
                SynchronizePaymentTypeBindings();
            }
            else if (e.PropertyName == "PaymentType")
                SynchronizePaymentTypeBindings();
            if(e.PropertyName == "InvoiceTypeId" || e.PropertyName == "IsPrinted" ||
                e.PropertyName == "InvoiceSignature" ||
                e.PropertyName == "TransformationUnitPriceEditEnabled" ||
                e.PropertyName == "TransformationVolumeEditEnabled" ||
                e.PropertyName == "LineEditEnabled" ||
                e.PropertyName == "DetailsEditEnabled")
            {
                ApplyPriceBindings();
                SetView();
            }
            if (myDieselVisible && myDieselPanel != null)
                myDieselPanel.RefreshEligibility();
        }

        private void ApplyPriceBindings()
        {
            bool wholesalePrices = !this.ivm.PricesIncludeVat;
            if (this.radGridView1 != null && this.radGridView1.Columns["UnitPrice"] != null)
            {
                // Keep one stable descriptor for the lifetime of the grid. The
                // UnitPriceView getter/setter handles both ordinary retail/net
                // invoices and the restricted transformation editing policy.
                this.radGridView1.Columns["UnitPrice"].FieldName = "UnitPriceView";
            }
            if (this.radSpinEditor3 != null)
            {
                this.radSpinEditor3.DataBindings.Clear();
                this.radSpinEditor3.DataBindings.Add(new Binding("Value", this.invoiceViewModelBindingSource,
                    wholesalePrices ? "DiscountAmountWhole" : "DiscountAmount", true));
            }
        }

        private void invoiceLinesBindingSource_CurrentItemChanged(object sender, EventArgs e)
        {
            if (this.invoiceLinesBindingSource.Current == null)
                this.ivm.CurrentLine = null;
            else
                this.ivm.CurrentLine = this.invoiceLinesBindingSource.Current as ViewModels.InvoiceLineViewModel;
            ExecuteWithLineLookupBindingsReadOnly(() => { });
            ApplyLineEditingPolicy();
        }

        private void Ivm_RefreshView(object sender, EventArgs e)
        {
            ResetEditorBindings();
        }

        private void SetView()
        {
            ApplyLineEditingPolicy();
            if (editorLayout != ViewModels.InvoiceEditorLayout.Legacy)
            {
                this.btnSave.Visible = true;
                this.panel6.Visible = false;
                return;
            }
            this.btnSave.Visible = !this.ivm.SimplePrint;
            this.panel6.Visible = false;
            if (ivm.IsPrinted.HasValue && ivm.IsPrinted.Value)
            {
                //if (string.IsNullOrEmpty(ivm.InvoiceSignature))
                //{
                this.btnSave.Visible = true;
                //}
            }
            if (this.ivm.SimplePrint || this.ivm.DispenserType)
            {
                this.tableLayoutPanel12.ColumnStyles[0].Width = 0;
                this.tableLayoutPanel12.ColumnStyles[1].Width = 100;
                this.tableLayoutPanel12.RowStyles[0].SizeType = SizeType.Absolute;
                this.tableLayoutPanel12.RowStyles[0].Height = 150;
                this.tableLayoutPanel12.RowStyles[1].SizeType = SizeType.Percent;
                this.tableLayoutPanel12.RowStyles[1].Height = 100;
                this.tableLayoutPanel12.RowStyles[2].Height = 30;
            }
            else
            {
                this.tableLayoutPanel12.ColumnStyles[0].Width = 100;
                this.tableLayoutPanel12.ColumnStyles[1].Width = 0;
                this.tableLayoutPanel12.RowStyles[0].SizeType = SizeType.Percent;
                this.tableLayoutPanel12.RowStyles[0].Height = 100;
                this.tableLayoutPanel12.RowStyles[1].SizeType = SizeType.Absolute;
                this.tableLayoutPanel12.RowStyles[1].Height = 120;
                this.tableLayoutPanel12.RowStyles[2].Height = 30;
            }
        }

        private void ApplyLineEditingPolicy()
        {
            bool financialOverride = (ivm.TransformationUnitPriceEditEnabled ||
                guideOnlyFinancialEdit) &&
                !ivm.DetailsEditEnabled;
            bool volumeOverride = ivm.TransformationVolumeEditEnabled &&
                !ivm.DetailsEditEnabled;
            bool transformationOverride = financialOverride || volumeOverride;
            if (tableLayoutPanel2 != null)
                tableLayoutPanel2.Enabled = ivm.LineEditEnabled || guideOnlyFinancialEdit;

            if (radGridView1 != null && guideOnlyFinancialEdit)
            {
                radGridView1.Enabled = true;
                radGridView1.ReadOnly = false;
                radGridView1.MasterTemplate.AllowEditRow = true;
                var priceColumn = radGridView1.Columns["UnitPrice"];
                var discountColumn = radGridView1.Columns["DiscountAmountView"];
                if (priceColumn != null)
                    priceColumn.ReadOnly = false;
                if (discountColumn != null)
                    discountColumn.ReadOnly = false;
            }

            if (volume1 != null)
                volume1.ReadOnly = volumeOverride ? false :
                    financialOverride || volumeEditorReadOnly;
            if (unitPrice1 != null)
                unitPrice1.ReadOnly = financialOverride ? false : unitPriceEditorReadOnly;
            if (discount != null)
                discount.ReadOnly = financialOverride ? false : discountEditorReadOnly;
            if (priceTotal1 != null)
                priceTotal1.ReadOnly = transformationOverride || totalEditorReadOnly;

            if (transformationOverride)
            {
                if (radDropDownList3 != null)
                    radDropDownList3.Enabled = false;
                if (radDropDownList2 != null)
                    radDropDownList2.Enabled = false;
            }
            else
            {
                var dispenserEnabled = radDropDownList3 == null
                    ? null
                    : radDropDownList3.DataBindings["Enabled"];
                var nozzleEnabled = radDropDownList2 == null
                    ? null
                    : radDropDownList2.DataBindings["Enabled"];
                if (dispenserEnabled != null)
                    dispenserEnabled.ReadValue();
                if (nozzleEnabled != null)
                    nozzleEnabled.ReadValue();
            }
        }
    }
}
