using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASFuelControl.Data;

namespace ASFuelControl.Windows.ViewModels
{
    public partial class InvoiceTypeViewModel
    {
        public string ViewDescription
        {
            get
            {
                if (this.InvoiceTypeId == Guid.Empty)
                    return "(Χωρίς Επιλογή)";
                return (this.Invalidated.HasValue && this.Invalidated.Value ? "(Ανενεργό) " : "" ) + this.Description;
            }
        }
    }

    public class InvoiceTypeDetailViewModel : InvoiceTypeViewModel
    {
        private List<InvoiceTypeTransformViewModel> allowedTransformations = new List<InvoiceTypeTransformViewModel>();

        public bool DispenserTypeEx
        {
            set { base.DispenserType = value; }
            get { return base.DispenserType.HasValue ? base.DispenserType.Value : false; }
        }

        public bool ForcesDeliveryEx
        {
            set { base.ForcesDelivery = value; }
            get { return base.ForcesDelivery.HasValue ? base.ForcesDelivery.Value : false; }
        }

        public bool InvalidatedEx
        {
            set { base.Invalidated = value; }
            get { return base.Invalidated.HasValue ? base.Invalidated.Value : false; }
        }

        public bool AdminViewEx
        {
            set { base.AdminView = value; }
            get { return base.AdminView.HasValue ? base.AdminView.Value : false; }
        }

        public bool ShowFinancialDataEx
        {
            set { base.ShowFinancialData = value; }
            get { return base.ShowFinancialData.HasValue ? base.ShowFinancialData.Value : false; }
        }

        public bool IsLaserPrintEx
        {
            set { base.IsLaserPrint = value; }
            get { return base.IsLaserPrint.HasValue ? base.IsLaserPrint.Value : false; }
        }

        public bool HasFinancialTransactionsEx
        {
            set { base.HasFinancialTransactions = value; }
            get { return base.HasFinancialTransactions.HasValue ? base.HasFinancialTransactions.Value : false; }
        }

        public bool IsCancelationEx
        {
            set { base.IsCancelation = value; }
            get { return base.IsCancelation.HasValue ? base.IsCancelation.Value : false; }
        }

        public bool NeedsVehicleEx
        {
            set { base.NeedsVehicle = value; }
            get { return base.NeedsVehicle.HasValue ? base.NeedsVehicle.Value : false; }
        }

        public bool IsInternalEx
        {
            set { base.IsInternal = value; }
            get { return base.IsInternal.HasValue ? base.IsInternal.Value : false; }
        }

        public bool RetailInvoiceEx
        {
            set { base.RetailInvoice = value; }
            get { return base.RetailInvoice.HasValue ? base.RetailInvoice.Value : false; }
        }

        public bool IncludeInBalanceEx
        {
            set { base.IncludeInBalance = value; }
            get { return base.IncludeInBalance.HasValue ? base.IncludeInBalance.Value : false; }
        }

        public Common.Enumerators.EnumItem[] InvoiceTransformationTypes
        {
            set;get;
        }

        public InvoiceTypeViewModel[] InvoiceTypes
        {
            get
            {
                return CommonCache.Instance.InvoiceTypes.OrderBy(i => i.Description).ToArray();
            }
        }

        public InvoiceTypeViewModel[] InvoiceTypesNullable
        {
            get
            {
                List<InvoiceTypeViewModel> list = new List<InvoiceTypeViewModel>();
                InvoiceTypeViewModel empty = new InvoiceTypeViewModel();
                empty.InvoiceTypeId = Guid.Empty;
                list.Add(empty);
                list.AddRange(CommonCache.Instance.InvoiceTypes.OrderBy(i => i.Description).ToArray());
                return list.ToArray();
            }
        }

        public InvoiceTypeTransformViewModel[] AllowedTransformations
        {
            get { return this.allowedTransformations.Where(i => i.EntityState != EntityStateEnum.Deleted).ToArray(); }
        }

        public InvoiceTypeTransformViewModel[] InvoiceTypeTransforms_ParentInvoiceTypeId
        {
            get { return this.AllowedTransformations; }
        }

        public override void Load(DatabaseModel db, InvoiceType entity)
        {
            base.Load(db, entity);

            List<Common.Enumerators.EnumItem> list = Common.Enumerators.EnumToList.EnumList<Common.Enumerators.InvoiceTransformationTypeEnum>();
            this.InvoiceTransformationTypes = list.ToArray();

            this.allowedTransformations.Clear();
            foreach (var it in entity.InvoiceTypeTransforms_ParentInvoiceTypeId)
            {
                InvoiceTypeTransformViewModel ittv = new InvoiceTypeTransformViewModel();
                ittv.Load(db, it);
                ittv.PropertyChanged += Ittv_PropertyChanged;
                this.allowedTransformations.Add(ittv);
            }
            this.OnPropertyChanged("AllowedTransformations");
        }

        protected override void SaveArrayProperty(DatabaseModel db, InvoiceType entity, string propName)
        {
            base.SaveArrayProperty(db, entity, propName);
            if (propName == "InvoiceTypeTransforms_ParentInvoiceTypeId")
            {
                foreach (var child in this.allowedTransformations)
                {
                    bool childSaved = child.Save(db,
                        child.InvoiceTypeTransformId);
                    if (!childSaved)
                    {
                        throw new InvalidOperationException(
                            "Δεν αποθηκεύτηκε ο μετασχηματισμός του τύπου παραστατικού.");
                    }
                }
            }
        }

        public void AddTransformation()
        {
            InvoiceTypeTransformViewModel itnew = new InvoiceTypeTransformViewModel();
            itnew.InvoiceTypeTransformId = Guid.NewGuid();
            itnew.ParentInvoiceTypeId = this.InvoiceTypeId;
            itnew.TransformationMode = 0;
            itnew.NotesAddition = string.Empty;
            itnew.EntityState = EntityStateEnum.Added;
            itnew.PropertyChanged += Ittv_PropertyChanged;
            this.allowedTransformations.Add(itnew);
            this.OnPropertyChanged("AllowedTransformations");
        }

        public void DeleteTransformation(InvoiceTypeTransformViewModel item)
        {
            item.EntityState = EntityStateEnum.Deleted;
            this.OnPropertyChanged("AllowedTransformations");
        }

        private void Ittv_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            this.HasChanges = true;
        }

        internal sealed class SaveState
        {
            internal EntityStateEnum EntityState { get; set; }
            internal bool HasChanges { get; set; }
            internal TransformationSaveState[] Transformations { get; set; }
        }

        internal sealed class TransformationSaveState
        {
            internal InvoiceTypeTransformViewModel Transformation { get; set; }
            internal EntityStateEnum EntityState { get; set; }
            internal bool HasChanges { get; set; }
        }

        internal void ValidateForSave()
        {
            if (this.EntityState == EntityStateEnum.Deleted)
            {
                return;
            }
            if (this.InvoiceTypeId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "Ο τύπος παραστατικού δεν έχει έγκυρο αναγνωριστικό.");
            }
            if (string.IsNullOrWhiteSpace(this.Description))
            {
                throw new InvalidOperationException(
                    "Συμπληρώστε την περιγραφή του τύπου παραστατικού.");
            }
            if (this.Description.Length > 100)
            {
                throw new InvalidOperationException(
                    "Η περιγραφή του τύπου παραστατικού δεν μπορεί να ξεπερνά τους 100 χαρακτήρες.");
            }
            if (string.IsNullOrWhiteSpace(this.Abbreviation))
            {
                throw new InvalidOperationException(
                    "Συμπληρώστε τη συντομογραφία του τύπου παραστατικού.");
            }
            if (this.Abbreviation.Length > 10)
            {
                throw new InvalidOperationException(
                    "Η συντομογραφία του τύπου παραστατικού δεν μπορεί να ξεπερνά τους 10 χαρακτήρες.");
            }

            foreach (InvoiceTypeTransformViewModel transformation in
                this.allowedTransformations)
            {
                transformation.ValidateForSave();
            }
        }

        internal SaveState CaptureSaveState()
        {
            var transformationStates =
                new List<TransformationSaveState>();
            foreach (InvoiceTypeTransformViewModel transformation in
                this.allowedTransformations)
            {
                var transformationState = new TransformationSaveState
                {
                    Transformation = transformation,
                    EntityState = transformation.EntityState,
                    HasChanges = transformation.HasChanges
                };
                transformationStates.Add(transformationState);
            }

            TransformationSaveState[] transformationStateArray =
                transformationStates.ToArray();
            return new SaveState
            {
                EntityState = this.EntityState,
                HasChanges = this.HasChanges,
                Transformations = transformationStateArray
            };
        }

        internal void RestoreSaveState(SaveState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException("state");
            }

            foreach (TransformationSaveState transformationState in
                state.Transformations)
            {
                InvoiceTypeTransformViewModel transformation =
                    transformationState.Transformation;
                transformation.EntityState = transformationState.EntityState;
                transformation.HasChanges = transformationState.HasChanges;
            }
            this.EntityState = state.EntityState;
            this.HasChanges = state.HasChanges;
        }

        internal void AcceptSaveState(SaveState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException("state");
            }

            foreach (TransformationSaveState transformationState in
                state.Transformations)
            {
                if (transformationState.EntityState != EntityStateEnum.Deleted)
                {
                    continue;
                }
                this.allowedTransformations.Remove(
                    transformationState.Transformation);
            }
        }

        internal InvoiceTypeDetailViewModel CreateEditableCopy()
        {
            var copy = new InvoiceTypeDetailViewModel
            {
                InvoiceTypeId = this.InvoiceTypeId,
                Description = this.Description,
                Abbreviation = this.Abbreviation,
                LastNumber = this.LastNumber,
                TransactionType = this.TransactionType,
                Printable = this.Printable,
                OfficialEnumerator = this.OfficialEnumerator,
                Printer = this.Printer,
                InvoiceFormId = this.InvoiceFormId,
                IsInternal = this.IsInternal,
                InternalDeliveryDescription = this.InternalDeliveryDescription,
                NeedsVehicle = this.NeedsVehicle,
                IsCancelation = this.IsCancelation,
                HasFinancialTransactions = this.HasFinancialTransactions,
                IsLaserPrint = this.IsLaserPrint,
                TransactionSign = this.TransactionSign,
                ShowFinancialData = this.ShowFinancialData,
                DeliveryType = this.DeliveryType,
                AdminView = this.AdminView,
                Invalidated = this.Invalidated,
                DispenserType = this.DispenserType,
                ForcesDelivery = this.ForcesDelivery,
                RetailInvoice = this.RetailInvoice,
                IncludeInBalance = this.IncludeInBalance,
                DefaultSeries = this.DefaultSeries,
                SendToMyData = this.SendToMyData,
                InvoiceTransformationTypes = this.InvoiceTransformationTypes,
                EntityState = EntityStateEnum.Unchanged
            };

            foreach (InvoiceTypeTransformViewModel source in
                this.AllowedTransformations)
            {
                var transformation = new InvoiceTypeTransformViewModel
                {
                    InvoiceTypeTransformId = source.InvoiceTypeTransformId,
                    ParentInvoiceTypeId = source.ParentInvoiceTypeId,
                    ChildInvoiceTypeId = source.ChildInvoiceTypeId,
                    TransformationMode = source.TransformationMode,
                    NotesAddition = source.NotesAddition,
                    CreationInvoiceTypeId = source.CreationInvoiceTypeId,
                    CreationType = source.CreationType,
                    CreationNotesAddition = source.CreationNotesAddition,
                    EntityState = EntityStateEnum.Unchanged
                };
                transformation.HasChanges = false;
                transformation.PropertyChanged += copy.Ittv_PropertyChanged;
                copy.allowedTransformations.Add(transformation);
            }
            copy.EntityState = EntityStateEnum.Unchanged;
            copy.HasChanges = false;
            return copy;
        }

        public InvoiceTypeDetailViewModel CreateCopy()
        {
            var copy = new InvoiceTypeDetailViewModel
            {
                InvoiceTypeId = Guid.NewGuid(),
                Description = (this.Description ?? string.Empty) + " (Copy)",
                Abbreviation = (this.Abbreviation ?? string.Empty) + " (C)",
                LastNumber = 0,
                TransactionType = this.TransactionType,
                Printable = this.Printable,
                OfficialEnumerator = this.OfficialEnumerator,
                Printer = this.Printer,
                InvoiceFormId = this.InvoiceFormId,
                IsInternal = this.IsInternal,
                InternalDeliveryDescription = this.InternalDeliveryDescription,
                NeedsVehicle = this.NeedsVehicle,
                IsCancelation = this.IsCancelation,
                HasFinancialTransactions = this.HasFinancialTransactions,
                IsLaserPrint = this.IsLaserPrint,
                TransactionSign = this.TransactionSign,
                ShowFinancialData = this.ShowFinancialData,
                DeliveryType = this.DeliveryType,
                AdminView = this.AdminView,
                Invalidated = this.Invalidated,
                DispenserType = this.DispenserType,
                ForcesDelivery = this.ForcesDelivery,
                RetailInvoice = this.RetailInvoice,
                IncludeInBalance = this.IncludeInBalance,
                DefaultSeries = this.DefaultSeries,
                SendToMyData = this.SendToMyData,
                InvoiceTransformationTypes = this.InvoiceTransformationTypes,
                EntityState = EntityStateEnum.Added
            };

            // Transformations shown on this type are settings owned by the
            // parent type. Recreate those rows with new identities and the copy
            // as parent; their configured target/creation types remain intact.
            foreach (var source in this.AllowedTransformations)
            {
                var transformation = new InvoiceTypeTransformViewModel
                {
                    InvoiceTypeTransformId = Guid.NewGuid(),
                    ParentInvoiceTypeId = copy.InvoiceTypeId,
                    ChildInvoiceTypeId = source.ChildInvoiceTypeId,
                    TransformationMode = source.TransformationMode,
                    NotesAddition = source.NotesAddition,
                    CreationInvoiceTypeId = source.CreationInvoiceTypeId,
                    CreationType = source.CreationType,
                    CreationNotesAddition = source.CreationNotesAddition,
                    EntityState = EntityStateEnum.Added
                };
                transformation.PropertyChanged += copy.Ittv_PropertyChanged;
                copy.allowedTransformations.Add(transformation);
            }
            copy.EntityState = EntityStateEnum.Added;
            copy.HasChanges = true;
            return copy;
        }
    }

    public class InvoiceTypeCatalogViewModel : INotifyPropertyChanged
    {
        private List<InvoiceTypeDetailViewModel> invoiceTypes = new List<InvoiceTypeDetailViewModel>();
        private bool hasChanges = false;
        private bool showValid = true;

        public event PropertyChangedEventHandler PropertyChanged;

        public bool HasChanges
        {
            get { return this.hasChanges; }
            set
            {
                if (this.hasChanges == value)
                    return;
                this.hasChanges = value;
                if (this.PropertyChanged != null)
                    this.PropertyChanged(this, new PropertyChangedEventArgs("HasChanges"));
            }
        }

        public bool ShowValid
        {
            set
            {
                if (value == this.showValid)
                    return;
                this.showValid = value;
                this.PropertyChanged(this, new PropertyChangedEventArgs("ShowValid"));
                this.PropertyChanged(this, new PropertyChangedEventArgs("HideValid"));
                this.PropertyChanged(this, new PropertyChangedEventArgs("InvoiceTypes"));
            }
            get { return this.showValid; }
        }

        public bool HideValid
        {
            set
            {
                this.ShowValid = !value;
            }
            get { return !this.showValid; }
        }
        public InvoiceTypeDetailViewModel[] InvoiceTypes
        {
            get
            {
                return this.invoiceTypes.Where(i=>i.InvalidatedEx == this.HideValid).ToArray();
            }
        }

        public InvoiceTypeDetailViewModel[] ChangedInvoiceTypes
        {
            get
            {
                return this.invoiceTypes.Where(invoiceType =>
                    invoiceType.HasChanges).ToArray();
            }
        }

        public Common.Enumerators.EnumItem[] TransactionTypes { set; get; }
        public Common.Enumerators.EnumItem[] TransactionSigns { set; get; }
        public Common.Enumerators.EnumItem[] DeliveryTypes { set; get; }
        public string[] Printers { set; get; }

        public InvoiceTypeCatalogViewModel()
        {
            try
            {
                invoiceTypes.Clear();
                InvoiceTypeDetailViewModel[] cachedInvoiceTypes =
                    CommonCache.Instance.InvoiceTypes;
                foreach (InvoiceTypeDetailViewModel cachedInvoiceType in
                    cachedInvoiceTypes)
                {
                    InvoiceTypeDetailViewModel editableInvoiceType =
                        cachedInvoiceType.CreateEditableCopy();
                    editableInvoiceType.PropertyChanged += It_PropertyChanged;
                    invoiceTypes.Add(editableInvoiceType);
                }

                List<Common.Enumerators.EnumItem> list = Common.Enumerators.EnumToList.EnumList<Common.Enumerators.TransactionTypeEnum>();
                this.TransactionTypes = list.ToArray();

                List<Common.Enumerators.EnumItem> listS = Common.Enumerators.EnumToList.EnumList<Common.Enumerators.TransactionSignEnum>();
                this.TransactionSigns = listS.ToArray();

                List<Common.Enumerators.EnumItemNullable> list2 = Common.Enumerators.EnumToList.EnumNullableList<Common.Enumerators.DeliveryTypeEnum>();
                list2.Insert(0, new Common.Enumerators.EnumItemNullable() { Description = "(Χωρίς Επιλογή)", Value = null });
                this.DeliveryTypes = list2.ToArray();

                List<string> printers = new List<string>();

                var defaultTaxDevice = Data.Implementation.OptionHandler.Instance.GetOption("DefaultTaxDevice");
                if (defaultTaxDevice.ToLower() == "samtec")
                {
                    printers.Add(Data.Implementation.OptionHandler.Instance.GetOption("Samtec_SignFolder"));
                }
                else if (defaultTaxDevice.ToLower() == "signa")
                {
                    printers.Add(Data.Implementation.OptionHandler.Instance.GetOption("SignA_SignFolder"));
                }

                foreach (string printer in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
                {
                    printers.Add(printer);
                }
                this.Printers = printers.ToArray();
                this.HasChanges = false;
            }
            catch { }
        }

        public InvoiceTypeViewModel AddNewInvoiceType()
        {
            InvoiceTypeDetailViewModel nit = new InvoiceTypeDetailViewModel();
            nit.InvoiceTypeId = Guid.NewGuid();
            nit.Description = string.Empty;
            nit.Abbreviation = string.Empty;
            nit.AdminView = false;
            nit.HasFinancialTransactions = true;
            nit.Invalidated = false;
            nit.IsCancelation = false;
            nit.IsInternal = true;
            nit.IsLaserPrint = true;
            nit.NeedsVehicle = true;
            nit.Printable = true;
            nit.ShowFinancialData = true;
            nit.TransactionSign = 1;
            nit.TransactionType = 0;
            List<Common.Enumerators.EnumItem> transformationTypes =
                Common.Enumerators.EnumToList.EnumList<
                    Common.Enumerators.InvoiceTransformationTypeEnum>();
            Common.Enumerators.EnumItem[] transformationTypeArray =
                transformationTypes.ToArray();
            nit.InvoiceTransformationTypes = transformationTypeArray;

            nit.EntityState = EntityStateEnum.Added;
            nit.PropertyChanged += It_PropertyChanged;
            this.invoiceTypes.Add(nit);
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs("InvoiceTypes"));
            }
            this.HasChanges = true;
            return nit;
        }

        public InvoiceTypeDetailViewModel CopyInvoiceType(InvoiceTypeDetailViewModel source)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            var copy = source.CreateCopy();
            copy.PropertyChanged += It_PropertyChanged;
            this.invoiceTypes.Add(copy);
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs("InvoiceTypes"));
            this.HasChanges = true;
            return copy;
        }

        public bool DeleteInvoiceType(InvoiceTypeDetailViewModel it)
        {
            Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
            if (db.Invoices.Count(i => i.InvoiceTypeId == it.InvoiceTypeId) > 0)
                return false;
            if (db.Traders.Count(t => t.InvoiceTypeId.HasValue && t.InvoiceTypeId == it.InvoiceTypeId) > 0)
                return false;
            this.invoiceTypes.Remove(it);
            it.EntityState = EntityStateEnum.Deleted;
            it.Save(it.InvoiceTypeId);
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs("InvoiceTypes"));
            }
            CommonCache.Instance.Refresh("InvoiceTypes");
            return true;
        }

        public void SaveChanges()
        {
            SaveChanges(null);
        }

        public void SaveChanges(Action<DbConnection> stageRelatedChanges)
        {
            InvoiceTypeDetailViewModel[] changedInvoiceTypes =
                this.ChangedInvoiceTypes;
            var saveStates =
                new Dictionary<InvoiceTypeDetailViewModel,
                    InvoiceTypeDetailViewModel.SaveState>();
            foreach (InvoiceTypeDetailViewModel invoiceType in
                changedInvoiceTypes)
            {
                invoiceType.ValidateForSave();
                InvoiceTypeDetailViewModel.SaveState saveState =
                    invoiceType.CaptureSaveState();
                saveStates.Add(invoiceType, saveState);
            }

            try
            {
                using (var db = new Data.DatabaseModel(
                    Properties.Settings.Default.DBConnection))
                {
                    foreach (InvoiceTypeDetailViewModel invoiceType in
                        changedInvoiceTypes)
                    {
                        bool invoiceTypeSaved = invoiceType.Save(db,
                            invoiceType.InvoiceTypeId);
                        if (!invoiceTypeSaved)
                        {
                            string failureMessage = BuildSaveFailureMessage(
                                invoiceType);
                            throw new InvalidOperationException(failureMessage);
                        }
                    }

                    if (stageRelatedChanges == null)
                    {
                        db.SaveChanges();
                    }
                    else
                    {
                        using (DbConnection contextConnection = db.Connection)
                        {
                            stageRelatedChanges(contextConnection);
                            db.SaveChanges();
                        }
                    }
                }
            }
            catch
            {
                foreach (KeyValuePair<InvoiceTypeDetailViewModel,
                    InvoiceTypeDetailViewModel.SaveState> saveState in
                    saveStates)
                {
                    saveState.Key.RestoreSaveState(saveState.Value);
                }
                this.HasChanges = true;
                throw;
            }

            foreach (KeyValuePair<InvoiceTypeDetailViewModel,
                InvoiceTypeDetailViewModel.SaveState> saveState in saveStates)
            {
                saveState.Key.AcceptSaveState(saveState.Value);
            }

            bool pendingChanges = this.invoiceTypes.Any(invoiceType =>
                invoiceType.HasChanges);
            this.HasChanges = pendingChanges;
            RefreshInvoiceTypeCache();
        }

        private static string BuildSaveFailureMessage(
            InvoiceTypeDetailViewModel invoiceType)
        {
            string invoiceTypeName = string.IsNullOrWhiteSpace(
                invoiceType.Description)
                ? invoiceType.InvoiceTypeId.ToString()
                : invoiceType.Description;
            ValidationError[] validationErrors = invoiceType.Errors ??
                new ValidationError[0];
            string[] errorMessages = validationErrors
                .Where(error => !string.IsNullOrWhiteSpace(error.ErrorMessage))
                .Select(error => error.ErrorMessage)
                .Distinct()
                .ToArray();
            if (errorMessages.Length == 0)
            {
                return "Δεν αποθηκεύτηκε ο τύπος παραστατικού \"" +
                    invoiceTypeName + "\".";
            }

            string errorDetails = string.Join(Environment.NewLine,
                errorMessages);
            return "Δεν αποθηκεύτηκε ο τύπος παραστατικού \"" +
                invoiceTypeName + "\"." + Environment.NewLine +
                errorDetails;
        }

        private void RefreshInvoiceTypeCache()
        {
            CommonCache cache = CommonCache.Instance;
            try
            {
                cache.Refresh("InvoiceTypes");
            }
            catch
            {
                var cacheInvoiceTypes =
                    new List<InvoiceTypeDetailViewModel>();
                foreach (InvoiceTypeDetailViewModel invoiceType in
                    this.invoiceTypes)
                {
                    InvoiceTypeDetailViewModel cacheInvoiceType =
                        invoiceType.CreateEditableCopy();
                    cacheInvoiceTypes.Add(cacheInvoiceType);
                }
                InvoiceTypeDetailViewModel[] orderedInvoiceTypes =
                    cacheInvoiceTypes.OrderBy(invoiceType =>
                        invoiceType.Description).ToArray();
                cache.InvoiceTypes = orderedInvoiceTypes;
            }
        }

        private void It_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "HasChanges")
            {
                InvoiceTypeViewModel it = sender as InvoiceTypeViewModel;
                if (it == null)
                    return;
                if (it.HasChanges)
                    this.HasChanges = it.HasChanges;
            }
        }
    }

    public partial class InvoiceTypeTransformViewModel
    {
        internal void ValidateForSave()
        {
            if (this.EntityState == EntityStateEnum.Deleted)
            {
                return;
            }
            if (this.InvoiceTypeTransformId == Guid.Empty ||
                this.ParentInvoiceTypeId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "Ο μετασχηματισμός δεν έχει έγκυρο αναγνωριστικό.");
            }
            if (this.ChildInvoiceTypeId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "Επιλέξτε τύπο παραστατικού για κάθε μετασχηματισμό.");
            }
            if (this.NotesAddition == null)
            {
                this.NotesAddition = string.Empty;
            }
            if (this.NotesAddition.Length > 500)
            {
                throw new InvalidOperationException(
                    "Οι σημειώσεις μετασχηματισμού δεν μπορούν να ξεπερνούν τους 500 χαρακτήρες.");
            }
        }

        public Guid CreationTypeId
        {
            get
            {
                if (this.CreationInvoiceTypeId.HasValue)
                    return this.CreationInvoiceTypeId.Value;
                return Guid.Empty;
            }
            set
            {
                if (value == Guid.Empty)
                    this.CreationInvoiceTypeId = null;
                this.CreationInvoiceTypeId = value;
                OnPropertyChanged("CreationInvoiceTypeId");
            }
        }
    }
}
