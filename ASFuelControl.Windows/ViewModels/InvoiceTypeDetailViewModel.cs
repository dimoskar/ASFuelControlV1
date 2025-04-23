using System;
using System.Collections.Generic;
using System.ComponentModel;
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
                    child.Save(db, child.InvoiceTypeTransformId);
                }
            }
        }

        public void AddTransformation()
        {
            InvoiceTypeTransformViewModel itnew = new InvoiceTypeTransformViewModel();
            itnew.InvoiceTypeTransformId = Guid.NewGuid();
            itnew.ParentInvoiceTypeId = this.InvoiceTypeId;
            itnew.TransformationMode = 0;
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

        public Common.Enumerators.EnumItem[] TransactionTypes { set; get; }
        public Common.Enumerators.EnumItem[] TransactionSigns { set; get; }
        public Common.Enumerators.EnumItem[] DeliveryTypes { set; get; }
        public string[] Printers { set; get; }

        public InvoiceTypeCatalogViewModel()
        {
            try
            {
                InvoiceTypeDetailViewModel[] array = new InvoiceTypeDetailViewModel[CommonCache.Instance.InvoiceTypes.Length];
                CommonCache.Instance.InvoiceTypes.CopyTo(array, 0);
                invoiceTypes.Clear();
                invoiceTypes.AddRange(array);
                foreach (var it in array)
                    it.PropertyChanged += It_PropertyChanged;

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
            
            nit.EntityState = EntityStateEnum.Added;
            this.invoiceTypes.Add(nit);
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs("InvoiceTypes"));
            }
            this.HasChanges = true;
            return nit;
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
            foreach (var it in this.InvoiceTypes)
            {
                if (it.HasChanges)
                {
                    it.Save(it.InvoiceTypeId);
                }
            }
            if(this.invoiceTypes.Where(i => i.HasChanges).Count() == 0)
            {
                this.HasChanges = false;
            }
            CommonCache.Instance.Refresh("InvoiceTypes");
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
