using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASFuelControl.Data;
using ASFuelControl.Invoicing.Mapping;

namespace ASFuelControl.Windows.ViewModels
{
    public class InvoiceDetailsViewModel : InvoiceViewModel
    {
        public event EventHandler<QueryInvoiceDataArgs> QueryInvoiceData;
        public event EventHandler RefreshView;

        public bool SuspendRecalculation = false;
        private ObservableCollection<InvoiceLineViewModel> lines = new ObservableCollection<InvoiceLineViewModel>();
        private List<InvoiceRelationViewModel> invRelations = new List<InvoiceRelationViewModel>();
        private readonly Dictionary<Guid, Guid> oneToManyLineSelections = new Dictionary<Guid, Guid>();
        private bool isEditLocked;
        private bool isTransformationPricePolicy;
        private bool guideOnlyTransformationFinancialEditEnabled;
        private bool transformationVolumeEditEnabled;
        private bool transformationSourceHasFinancialData = true;
        private bool transformationGuideOnlyTarget;
        private string transformationCounterpartVat;
        private bool vatExemption { set; get; }
        private string vatExemptionReason = "";
        private const decimal DefaultVatPercentage = 24m;
        private sealed class VatExemptionLineState
        {
            public decimal RetailUnitPrice { get; set; }
            public decimal DiscountAmount { get; set; }
            public decimal VatPercentage { get; set; }
        }

        private readonly Dictionary<Guid, VatExemptionLineState> vatExemptionLineStates =
            new Dictionary<Guid, VatExemptionLineState>();

        public virtual InvoiceEditorScenario EditorScenario
        {
            get { return InvoiceEditorScenario.General; }
        }

        public virtual InvoiceCounterpartySelection CounterpartySelection
        {
            get { return InvoiceCounterpartySelection.Any; }
        }

        public bool VatExemption
        {
            set
            {
                if (this.vatExemption == value)
                    return;
                if (this.lines.Count > 0 && this.lines.All(line => line.PreserveFiscalAmounts))
                {
                    // The exemption has already been applied to a saved fiscal
                    // transformation. Loading its trader must not apply VAT
                    // removal a second time.
                    this.vatExemption = value;
                    this.OnPropertyChanged("VatExemption");
                    return;
                }
                decimal configuredVatPercentage = GetConfiguredVatPercentage();
                if (value)
                {
                    vatExemptionLineStates.Clear();
                    foreach (var invoiceLine in lines)
                    {
                        vatExemptionLineStates[invoiceLine.InvoiceLineId] = new VatExemptionLineState
                        {
                            RetailUnitPrice = invoiceLine.UnitPriceRetail,
                            DiscountAmount = invoiceLine.DiscountAmount,
                            VatPercentage = invoiceLine.VatPercentage > 0
                                ? invoiceLine.VatPercentage
                                : configuredVatPercentage
                        };
                    }
                }
                this.vatExemption = value;
                foreach(var line in this.lines)
                {
                    VatExemptionLineState state;
                    if (!vatExemptionLineStates.TryGetValue(line.InvoiceLineId, out state))
                    {
                        state = new VatExemptionLineState
                        {
                            RetailUnitPrice = value
                                ? line.UnitPriceRetail
                                : AddVat(line.UnitPriceRetail, configuredVatPercentage),
                            DiscountAmount = line.DiscountAmount,
                            VatPercentage = configuredVatPercentage
                        };
                    }
                    if (value)
                    {
                        var newDisc = RemoveVat(state.DiscountAmount, state.VatPercentage, 2);
                        if (line.VatPercentage > 0 && newDisc != line.DiscountAmount)
                            line.DiscountAmount = newDisc;
                        if(line.VatPercentage != 0)
                            line.VatPercentage = 0;
                        line.UnitPriceRetail = RemoveVat(state.RetailUnitPrice, state.VatPercentage, 5);
                    }
                    else
                    {
                        line.VatPercentage = state.VatPercentage;
                        line.DiscountAmount = state.DiscountAmount;
                        line.UnitPriceRetail = state.RetailUnitPrice;
                    }
                }
                if (!value)
                    vatExemptionLineStates.Clear();
                if (this.IsEditLocked && this.lines.Count > 0)
                {
                    bool suspended = this.SuspendRecalculation;
                    this.SuspendRecalculation = false;
                    this.RecalculateSums();
                    this.SuspendRecalculation = suspended;
                }
                this.OnPropertyChanged("VatExemption");
            }
            get { return this.vatExemption; }
        }

        internal decimal GetUnitPriceFromRetailPrice(InvoiceLineViewModel line, decimal retailUnitPrice)
        {
            if (!this.VatExemption)
                return retailUnitPrice;

            decimal vatPercentage = GetConfiguredVatPercentage();
            VatExemptionLineState state;
            if (line != null && vatExemptionLineStates.TryGetValue(line.InvoiceLineId, out state))
            {
                state.RetailUnitPrice = retailUnitPrice;
                vatPercentage = state.VatPercentage;
            }
            else if (line != null)
            {
                vatExemptionLineStates[line.InvoiceLineId] = new VatExemptionLineState
                {
                    RetailUnitPrice = retailUnitPrice,
                    DiscountAmount = line.DiscountAmount,
                    VatPercentage = vatPercentage
                };
            }

            return RemoveVat(retailUnitPrice, vatPercentage, 5);
        }

        private static decimal GetConfiguredVatPercentage()
        {
            return Data.Implementation.OptionHandler.Instance.GetDecimalOption("VATValue", DefaultVatPercentage);
        }

        private static decimal RemoveVat(decimal amount, decimal vatPercentage, int decimalPlaces)
        {
            return decimal.Round(amount / ((100 + vatPercentage) / 100), decimalPlaces);
        }

        private static decimal AddVat(decimal amount, decimal vatPercentage)
        {
            return decimal.Round(amount * ((100 + vatPercentage) / 100), 5);
        }

        private void ApplyVatExemptionToTransformedLine(InvoiceLineViewModel targetLine,
            decimal sourceUnitPrice, decimal sourceVatPercentage, bool sourcePriceIncludesVat)
        {
            if (!this.VatExemption || targetLine == null)
                return;
            if (this.SelectedInvoiceType != null &&
                this.SelectedInvoiceType.IsCancelation == true)
                return;

            // Pump sales carry the VAT-inclusive price of the original sale.
            // When the customer is selected before the transformed lines are
            // created, the VatExemption setter cannot adjust those new lines.
            // Apply the same conversion here as for a nozzle-selected line.
            // A transformation can be created after the configured VAT rate has
            // changed. Use the VAT stored on the source line, rather than today's
            // configured rate, to recover the transaction's real net unit price.
            decimal effectiveUnitPrice = sourcePriceIncludesVat && sourceVatPercentage > 0
                ? RemoveVat(sourceUnitPrice, sourceVatPercentage, 5)
                : sourceUnitPrice;

            targetLine.VatPercentage = 0;
            var prices = TransformationUnitPrice.FromRetail(effectiveUnitPrice, 0);
            targetLine.SetTransformationUnitPrices(prices.Retail, prices.Net);
        }

        private static bool SourcePriceIncludesVat(Data.Invoice invoice)
        {
            return invoice != null && invoice.InvoiceType != null &&
                invoice.InvoiceType.TransactionType ==
                    (int)Common.Enumerators.TransactionTypeEnum.Extraction &&
                (invoice.InvoiceType.RetailInvoice == true ||
                 invoice.InvoiceType.DispenserType == true ||
                 HostInvoiceMapper.IsDeliveryDocument(invoice.InvoiceType));
        }

        private bool TargetPriceIncludesVat(Data.InvoiceType invoiceType)
        {
            if (this.EditorScenario == InvoiceEditorScenario.Retail)
                return true;
            if (this.EditorScenario == InvoiceEditorScenario.Wholesale ||
                this.EditorScenario == InvoiceEditorScenario.Supplier)
                return false;
            return invoiceType != null &&
                (invoiceType.RetailInvoice == true || invoiceType.DispenserType == true);
        }

        private static decimal SourceRetailUnitPrice(Data.InvoiceLine line)
        {
            if (line == null)
                return 0;
            // The physical transaction is the authoritative price provenance for
            // a pump-created source invoice. Read it while building the derived
            // line, but do not copy its FK to that derived line.
            if (line.SalesTransaction != null)
                return line.SalesTransaction.UnitPrice;
            if (line.UnitPriceRetail != 0)
                return line.UnitPriceRetail;
            return line.UnitPrice;
        }

        private static decimal SourceNetUnitPrice(Data.InvoiceLine line)
        {
            if (line == null)
                return 0;
            if (line.UnitPriceWhole != 0)
                return line.UnitPriceWhole;
            return line.UnitPrice;
        }

        private static void ApplyAuthoritativeSourcePricing(InvoiceLineViewModel targetLine,
            Data.InvoiceLine sourceLine, bool sourcePriceIncludesVat)
        {
            if (targetLine == null || sourceLine == null)
                return;

            bool retailAuthority = sourcePriceIncludesVat ||
                sourceLine.SalesTransaction != null;
            // Retail, dispenser and sales-delivery sources own the VAT-inclusive
            // price. Purchase and wholesale sources own the VAT-exclusive price.
            // Derive the secondary representation once and then assign both fields
            // atomically so their editor setters cannot round-trip the source value.
            var prices = retailAuthority
                ? TransformationUnitPrice.FromRetail(
                    SourceRetailUnitPrice(sourceLine), sourceLine.VatPercentage)
                : TransformationUnitPrice.FromNet(
                    SourceNetUnitPrice(sourceLine), sourceLine.VatPercentage);
            targetLine.SetTransformationUnitPrices(prices.Retail, prices.Net);
        }

        private static void ApplyEnteredTransformationPrice(
            InvoiceLineViewModel targetLine, decimal enteredPrice,
            decimal vatPercentage, bool enteredPriceIncludesVat)
        {
            var prices = enteredPriceIncludesVat
                ? TransformationUnitPrice.FromRetail(enteredPrice, vatPercentage)
                : TransformationUnitPrice.FromNet(enteredPrice, vatPercentage);
            targetLine.SetTransformationUnitPrices(prices.Retail, prices.Net);
        }

        private static void ApplyEnteredTransformationDiscount(
            InvoiceLineViewModel targetLine, decimal enteredDiscount,
            decimal vatPercentage, bool enteredDiscountIncludesVat)
        {
            decimal multiplier = (100m + vatPercentage) / 100m;
            decimal retailDiscount;
            decimal netDiscount;
            if (enteredDiscountIncludesVat)
            {
                retailDiscount = decimal.Round(enteredDiscount, 2);
                netDiscount = vatPercentage == 0
                    ? retailDiscount
                    : decimal.Round(retailDiscount / multiplier, 2);
            }
            else
            {
                netDiscount = decimal.Round(enteredDiscount, 2);
                retailDiscount = vatPercentage == 0
                    ? netDiscount
                    : decimal.Round(netDiscount * multiplier, 2);
            }
            targetLine.DiscountAmountRetail = retailDiscount;
            targetLine.DiscountAmountWhole = netDiscount;
            targetLine.DiscountAmount = enteredDiscountIncludesVat
                ? retailDiscount
                : netDiscount;
        }

        private static decimal GetConsumedTransformationVolume(Data.InvoiceLine line)
        {
            if (line == null)
                return 0;
            return line.InvoiceLineRelations_ParentLineId
                .Where(r => r.InvoiceRelation != null &&
                    r.InvoiceRelation.RelationType == (int)Common.Enumerators.InvoiceRelationTypeEnum.Replace)
                .Sum(r => r.InvoiceLine_ChildRelationId == null ? 0 : r.InvoiceLine_ChildRelationId.Volume);
        }

        public string VatExemptionReason
        {
            set
            {
                string oldValue = this.vatExemptionReason;
                this.vatExemptionReason = value;
                //if (value != null && value != "")
                //    this.Notes = this.Notes + "\r\n" + this.VatExemptionReason;
                //else
                //{
                //    if (this.Notes == null)
                //        this.Notes = "";
                //    this.Notes = this.Notes.Replace("\r\n" + oldValue, "");
                //}
            }
            get { return this.vatExemptionReason; }
        }

        public InvoiceLineViewModel[] InvoiceLines
        {
            get
            {
                return this.lines.Where(v => v.EntityState != EntityStateEnum.Deleted).ToArray();
            }
        }

        public InvoiceRelationViewModel[] InvoiceRelations
        {
            get
            {
                return this.invRelations.Where(v => v.EntityState != EntityStateEnum.Deleted).ToArray();
            }
        }

        public InvoiceRelationViewModel[] ParentInvoiceRelations
        {
            get
            {
                return this.invRelations.Where(v => v.EntityState != EntityStateEnum.Deleted && v.ChildInvoiceId == this.InvoiceId).ToArray();
            }
        }

        public InvoiceRelationViewModel[] ChildInvoiceRelations
        {
            get
            {
                return this.invRelations.Where(v => v.EntityState != EntityStateEnum.Deleted && v.ParentInvoiceId == this.InvoiceId).ToArray();
            }
        }

        public InvoiceTypeViewModel[] InvoiceTypes { set; get; }

        public FuelTypeViewModel[] FuelTypes { set; get; }

        public TankViewModel[] Tanks { set; get; }

        public DispenserViewModel[] Dispensers { set; get; }

        public InvoiceTypeViewModel SelectedInvoiceType
        {
            get
            {
                return CommonCache.Instance.InvoiceTypes.SingleOrDefault(i => i.InvoiceTypeId == this.InvoiceTypeId);
                //return this.InvoiceTypes
            }
        }

        public Common.Enumerators.EnumItem[] PaymentTypes { set; get; }

        public bool EditEnabled
        {
            get
            {
                if (this.AllowEdit.HasValue && !this.AllowEdit.Value)
                    return false;
                if (this.IsEditLocked)
                    return false;
                if (!this.IsPrinted.HasValue)
                    return true;
                return !this.IsPrinted.Value;
            }
        }

        public bool TransformationUnitPriceEditEnabled
        {
            get
            {
                if (!this.isTransformationPricePolicy)
                    return false;
                if (this.guideOnlyTransformationFinancialEditEnabled &&
                    this.Number == 0 &&
                    (!this.IsPrinted.HasValue || !this.IsPrinted.Value))
                    return true;
                if (this.AllowEdit.HasValue && !this.AllowEdit.Value)
                    return false;
                if (this.IsPrinted.HasValue && this.IsPrinted.Value)
                    return false;
                return TransformationEditPolicy.AllowUnitPriceEdit(
                    this.transformationSourceHasFinancialData,
                    this.transformationGuideOnlyTarget,
                    Data.Implementation.OptionHandler.Instance.GetOption("CompanyTIN"),
                    this.transformationCounterpartVat);
            }
        }

        public bool TransformationVolumeEditEnabled
        {
            get
            {
                if (!this.isTransformationPricePolicy ||
                    !this.transformationVolumeEditEnabled)
                    return false;
                if (this.AllowEdit.HasValue && !this.AllowEdit.Value)
                    return false;
                return !this.IsPrinted.HasValue || !this.IsPrinted.Value;
            }
        }

        public bool LineEditEnabled
        {
            get
            {
                return this.EditEnabled || this.TransformationUnitPriceEditEnabled ||
                    this.TransformationVolumeEditEnabled;
            }
        }

        public bool SelectCustomerEnabled
        {
            get
            {
                if (this.AllowEdit.HasValue && !this.AllowEdit.Value)
                    return false;
                if (this.IsEditLocked && !this.isTransformationPricePolicy)
                    return false;
                if (!this.IsPrinted.HasValue)
                    return true;
                return !this.IsPrinted.Value;
            }
        }

        public bool CanAdd
        {
            get
            {
                if (this.SelectedInvoiceType == null)
                    return false;
                if (this.SelectedInvoiceType.DispenserType.HasValue && this.SelectedInvoiceType.DispenserType.Value && this.lines.Count > 0)
                    return false;
                return this.EditEnabled;
            }
        }

        public bool DetailsEditEnabled
        {
            get
            {
                if(this.IsEditLocked)
                    return false;
                return this.EditEnabled;
            }
        }

        public bool SupportsSupplyNumber
        {
            get
            {
                foreach(var line in lines)
                {
                    var ft = this.FuelTypes.Where(f => f.FuelTypeId == line.FuelTypeId).FirstOrDefault();
                    if (ft == null)
                        continue;
                    if (!ft.SupportsSupplyNumber.HasValue || !ft.SupportsSupplyNumber.Value)
                        continue;
                    return true;
                }
                return false;
            }
        }

        public string TraderName { set; get; }

        public string PlateNumber { set; get; }

        public decimal NewRestAmount
        {
            get;
            set;
        }

        public InvoiceLineViewModel CurrentLine
        {
            set;
            get;
        }

        public InvoiceDetailsViewModel()
        {
            this.InvoiceTypes = CommonCache.Instance.InvoiceTypes.Where(i => !i.InvalidatedEx).ToArray();
            if (Data.DatabaseModel.UserLoggedInLevel != 0)
                this.InvoiceTypes = CommonCache.Instance.InvoiceTypes.Where(i => !i.InvalidatedEx && (!i.AdminView.HasValue || !i.AdminView.Value)).ToArray();
            this.FuelTypes = CommonCache.Instance.FuelTypes;
            this.Dispensers = CommonCache.Instance.Dispensers;
            List<Common.Enumerators.EnumItem> list = Common.Enumerators.EnumToList.EnumList<Common.Enumerators.PaymentTypeEnum>();
            this.PaymentTypes = list.ToArray();
            this.Tanks = CommonCache.Instance.Tanks;
        }

        public InvoiceDetailsViewModel(DatabaseModel db, Invoice entity) : base(db, entity)
        {
            this.InvoiceTypes = CommonCache.Instance.InvoiceTypes.Where(i => !i.InvalidatedEx).ToArray();
            if (Data.DatabaseModel.UserLoggedInLevel != 0)
                this.InvoiceTypes = CommonCache.Instance.InvoiceTypes.Where(i => !i.InvalidatedEx && (!i.AdminView.HasValue || !i.AdminView.Value)).ToArray();
            this.FuelTypes = CommonCache.Instance.FuelTypes;
            this.Dispensers = CommonCache.Instance.Dispensers;
            List<Common.Enumerators.EnumItem> list = Common.Enumerators.EnumToList.EnumList<Common.Enumerators.PaymentTypeEnum>();
            this.PaymentTypes = list.ToArray();
            this.Tanks = CommonCache.Instance.Tanks;
        }

        public bool RetailPrices
        {
            get
            {
                if (this.EditorScenario == InvoiceEditorScenario.Retail)
                    return true;
                if (this.EditorScenario == InvoiceEditorScenario.Wholesale ||
                    this.EditorScenario == InvoiceEditorScenario.Supplier)
                    return false;
                var invType = this.InvoiceTypes.SingleOrDefault(i => i.InvoiceTypeId == this.InvoiceTypeId);
                if (invType == null)
                    return false;
                return invType.RetailInvoice.HasValue ? invType.RetailInvoice.Value : false;
            }
        }

        public bool WholeSalePrices
        {
            get { return !this.PricesIncludeVat; }
        }

        public bool PricesIncludeVat
        {
            get
            {
                if (this.EditorScenario == InvoiceEditorScenario.General)
                    return this.RetailPrices || this.DispenserType;
                return this.RetailPrices;
            }
        }

        public bool ReportPrint
        {
            get
            {
                if (this.InvoiceLines.Length > 1)
                    return true;
                var invType = CommonCache.Instance.InvoiceTypes.SingleOrDefault(i => i.InvoiceTypeId == this.InvoiceTypeId);
                if (invType == null)
                    return false;
                bool simplePrint = (invType.IsLaserPrint.HasValue ? !invType.IsLaserPrint.Value : this.InvoiceLines.Length > 1);
                if (simplePrint)
                    return !simplePrint;
                //if (this.simplePrintTransform.HasValue)
                //    return !this.simplePrintTransform.Value;
                if (!invType.Printable)
                    return true;
                if (invType.DispenserType.HasValue && invType.DispenserType.Value)
                    return false;
                return invType.IsLaserPrint.HasValue ? invType.IsLaserPrint.Value : false;
            }
        }

        public bool DispenserType
        {
            get
            {
                if (this.InvoiceLines.Length > 1)
                    return false;
                var invType = CommonCache.Instance.InvoiceTypes.SingleOrDefault(i => i.InvoiceTypeId == this.InvoiceTypeId);
                if (invType == null)
                    return false;
                if (this.simplePrintTransform.HasValue)
                    return !this.simplePrintTransform.Value;

                return invType.DispenserType.HasValue ? invType.DispenserType.Value : false;
            }
        }

        public bool SimplePrint
        {
            get
            {
                return !this.ReportPrint;
            }
        }

        public bool IsEditLocked
        {
            set
            {
                if (this.isEditLocked == value)
                    return;
                this.isEditLocked = value;
                base.OnPropertyChanged("IsEditLocked", true);
                NotifyLineEditPolicyChanged();
            }
            get { return this.isEditLocked; }
        }

        private void ConfigureTransformationPriceEditPolicy(bool isTransformation,
            bool sourceHasFinancialData, bool guideOnlyTarget, string counterpartVat,
            bool allowVolumeEdit = false)
        {
            this.isTransformationPricePolicy = isTransformation;
            this.transformationSourceHasFinancialData = sourceHasFinancialData;
            this.transformationGuideOnlyTarget = guideOnlyTarget;
            this.transformationCounterpartVat = counterpartVat;
            this.transformationVolumeEditEnabled = isTransformation && allowVolumeEdit;
            if (!isTransformation)
                this.guideOnlyTransformationFinancialEditEnabled = false;
            NotifyLineEditPolicyChanged();
        }

        internal void EnableGuideOnlyTransformationFinancialEditing()
        {
            // The final document of a replacement-with-credit is a new, unsaved
            // invoice. Its source is only a guide, so price and discount editing
            // must not depend on an earlier WinForms binding value or on which
            // duplicate transform row was returned first.
            this.isTransformationPricePolicy = true;
            this.transformationGuideOnlyTarget = true;
            this.guideOnlyTransformationFinancialEditEnabled = true;
            this.AllowEdit = true;
            this.IsPrinted = false;
            NotifyLineEditPolicyChanged();
        }

        private void NotifyLineEditPolicyChanged()
        {
            base.OnPropertyChanged("EditEnabled", true);
            base.OnPropertyChanged("DetailsEditEnabled", true);
            base.OnPropertyChanged("SelectCustomerEnabled", true);
            base.OnPropertyChanged("CanAdd", true);
            base.OnPropertyChanged("TransformationUnitPriceEditEnabled", true);
            base.OnPropertyChanged("TransformationVolumeEditEnabled", true);
            base.OnPropertyChanged("LineEditEnabled", true);
        }

        public decimal TotalRetailAmount
        {
            set
            {
                if (!this.SimplePrint && !this.DispenserType)
                    return;
                if (this.CurrentLine == null)
                    return;
                this.CurrentLine.TotalPrice = value;
                if (this.CurrentLine.VatPercentage > 0)
                    this.CurrentLine.VatAmount = value - 100 * (value / (100 + this.CurrentLine.VatPercentage));
                if (this.CurrentLine.UnitPriceView > 0)
                {
                    if(this.PricesIncludeVat)
                        this.CurrentLine.Volume = decimal.Round((value + this.CurrentLine.DiscountAmountView) / this.CurrentLine.UnitPriceView, 3);
                    else
                        this.CurrentLine.Volume = decimal.Round((value - this.CurrentLine.VatAmount + this.CurrentLine.DiscountAmountView) / this.CurrentLine.UnitPriceView, 3);
                }
                this.TotalAmount = value;
                this.OnPropertyChanged("Volume");
                this.OnPropertyChanged("NettoPrice");
            }
            get
            {
                return this.TotalAmount.HasValue ? this.TotalAmount.Value : 0;

                //if (this.CurrentLine == null)
                //    return 0;
                //if (this.SimplePrint || this.DispenserType)
                //    return this.CurrentLine.TotalPrice;
                //else
                //    return this.TotalAmount.HasValue ? this.TotalAmount.Value : 0;
            }
        }

        public decimal TotalAmountView
        {
            get
            {
                if (this.PricesIncludeVat)
                    return this.TotalRetailAmount;
                else
                    return this.TotalAmount.HasValue ? this.TotalAmount.Value : 0;
            }
            set
            {
                if (this.SimplePrint || this.DispenserType)
                    this.CurrentLine.TotalPrice = value;
                else
                    this.TotalAmount = value;
                OnPropertyChanged("TotalRetailAmount");
                OnPropertyChanged("TotalAmount");
            }
        }

        public decimal NettoAmmountView
        {
            get
            {
                return this.NettoAmount.HasValue ? this.NettoAmount.Value : 0;// this.TotalAmountView - (this.VatAmount.HasValue ? this.VatAmount.Value : 0) + this.DiscountAmount;
            }
        }

        private decimal discountAmountView = 0;
        public decimal DiscountAmountView
        {
            set
            {
                if (this.PricesIncludeVat)
                {
                    if (this.DiscountAmountRetail == value)
                        return;
                    this.DiscountAmountRetail = value;
                    this.DiscountAmountWhole = this.InvoiceLines.Sum(i => i.DiscountAmountWhole);
                }
                else
                {
                    if (this.DiscountAmountWhole == value)
                        return;
                    this.DiscountAmountWhole = value;
                    this.DiscountAmountRetail = this.InvoiceLines.Sum(i => i.DiscountAmountRetail);
                }
                this.DiscountAmount = this.DiscountAmountWhole;
                this.OnPropertyChanged("DiscountAmountWhole");
            }
            get
            {
                if (this.PricesIncludeVat)
                    return this.DiscountAmountRetail;
                else
                    return this.DiscountAmountWhole;
            }
        }

        public void AddInvoiceLine()
        {
            if (this.SelectedInvoiceType == null)
                return;
            if (this.SelectedInvoiceType.DispenserType.HasValue && this.SelectedInvoiceType.DispenserType.Value && this.lines.Count > 0)
                return;
            InvoiceLineViewModel newLine = new InvoiceLineViewModel();
            newLine.InvoiceId = this.InvoiceId;
            newLine.InvoiceLineId = Guid.NewGuid();
            newLine.EntityState = EntityStateEnum.Added;
            if (VatExemption)
                newLine.VatPercentage = 0;
            else
                newLine.VatPercentage = Data.Implementation.OptionHandler.Instance.GetDecimalOption("VATValue", DefaultVatPercentage);
            newLine.ParentInvoice = this;
            newLine.PropertyChanged += Ilvm_PropertyChanged;
            this.lines.Add(newLine);
            this.OnPropertyChanged("InvoiceLines");
        }

        public void DeleteInvoiceLine(Guid invLineId)
        {
            var invLine = this.lines.SingleOrDefault(v => v.InvoiceLineId == invLineId);
            if (invLine == null)
                return;
            invLine.EntityState = EntityStateEnum.Deleted;
            this.OnPropertyChanged("InvoiceLines");
        }

        public bool CreateNewSale(bool isLiterCheck)
        {
            if (this.CurrentLine == null)
            {
                this.AddError("Σφάλμα παραστατικού", "Δεν υπάρχει επιλεγμένη γραμμή παραστατικού.");
                return false;
            }
            if (!this.PaymentType.HasValue || !Enum.IsDefined(typeof(Common.Enumerators.PaymentTypeEnum), this.PaymentType.Value))
            {
                this.AddError("Σφάλμα παραστατικού", "Δεν έχει επιλεγεί έγκυρος τρόπος πληρωμής.");
                return false;
            }
            if (!this.PaymentTypes.Any(item =>
                Convert.ToInt32(item.Value) == this.PaymentType.Value))
            {
                this.AddError("Σφάλμα παραστατικού", "Ο τρόπος πληρωμής δεν ανήκει στη διαθέσιμη λίστα.");
                return false;
            }
            if (this.InvoiceTypeId == Guid.Empty ||
                !this.InvoiceTypes.Any(item => item.InvoiceTypeId == this.InvoiceTypeId))
            {
                this.AddError("Σφάλμα παραστατικού", "Ο τύπος παραστατικού δεν ανήκει στη διαθέσιμη λίστα.");
                return false;
            }
            var selectedDispenser = this.Dispensers.FirstOrDefault(
                item => item.DispenserId == this.CurrentLine.CurrentDispenserId);
            var selectedNozzle = selectedDispenser == null || selectedDispenser.Nozzles == null
                ? null
                : selectedDispenser.Nozzles.FirstOrDefault(
                    item => item.NozzleId == this.CurrentLine.CurrentNozzleId);
            if (selectedDispenser == null || selectedNozzle == null)
            {
                this.AddError("Σφάλμα παραστατικού",
                    "Η αντλία ή το ακροσωλήνιο δεν ανήκει στη διαθέσιμη λίστα.");
                return false;
            }
            if (selectedNozzle.FuelTypeId != this.CurrentLine.FuelTypeId ||
                !this.FuelTypes.Any(item => item.FuelTypeId == this.CurrentLine.FuelTypeId))
            {
                this.AddError("Σφάλμα παραστατικού",
                    "Το καύσιμο της γραμμής δεν συμφωνεί με το καύσιμο του ακροσωληνίου.");
                return false;
            }
            if (this.CurrentLine.Volume <= 0)
            {
                this.AddError("Σφάλμα παραστατικού",
                    "Ο όγκος της γραμμής πρέπει να είναι μεγαλύτερος από μηδέν.");
                return false;
            }
            if (this.CurrentLine.SaleTransactionId.HasValue)
            {
                try
                {
                    using (DatabaseModel db = new DatabaseModel(Properties.Settings.Default.DBConnection))
                    {
                        var invoice = db.Invoices.FirstOrDefault(i => i.InvoiceId == this.InvoiceId);
                        if (invoice == null)
                            throw new InvalidOperationException("Δεν βρέθηκε αποθηκευμένο παραστατικό ή τρόπος πληρωμής.");
                        var invoiceLine = invoice.InvoiceLines
                            .FirstOrDefault(line => line.InvoiceLineId == this.CurrentLine.InvoiceLineId) ??
                            invoice.InvoiceLines.FirstOrDefault(line =>
                                line.SaleTransactionId == this.CurrentLine.SaleTransactionId);
                        if (invoiceLine == null || invoiceLine.SalesTransaction == null)
                            throw new InvalidOperationException("Δεν βρέθηκε η συνδεδεμένη κίνηση πώλησης του παραστατικού.");
                        var sale = invoiceLine.SalesTransaction;
                        if (sale.Nozzle == null)
                            throw new InvalidOperationException("Η συνδεδεμένη κίνηση πώλησης δεν έχει ακροσωλήνιο.");
                        if (this.CurrentLine.CurrentNozzleId != sale.NozzleId ||
                            this.CurrentLine.CurrentDispenserId != sale.Nozzle.DispenserId)
                            throw new InvalidOperationException(
                                "Η αντλία ή το ακροσωλήνιο της γραμμής δεν συμφωνεί με τη φυσική κίνηση πώλησης. " +
                                "Η αποθήκευση και η έκδοση ακυρώθηκαν.");
                        if (this.CurrentLine.FuelTypeId != sale.Nozzle.FuelTypeId)
                            throw new InvalidOperationException(
                                "Το καύσιμο της γραμμής δεν συμφωνεί με το καύσιμο της φυσικής κίνησης πώλησης.");

                        bool hasDifferentPaymentType = invoice.PaymentType != this.PaymentType.Value;
                        // This is an existing pump invoice. Header data may be
                        // completed before issuance, but the physical sale and the
                        // line values created from it are immutable.
                        invoice.TransactionDate = this.TransactionDate;
                        invoice.PaymentType = this.PaymentType.Value;
                        // A direct pump sale already owns its persisted invoice and
                        // SaleTransaction. Saving the editor must still persist the
                        // customer selected immediately before a myDiesel capture;
                        // otherwise the subsequent reservation reloads an anonymous
                        // invoice and rejects the beneficiary VAT.
                        invoice.TraderId = this.TraderId.HasValue && this.TraderId.Value != Guid.Empty
                            ? this.TraderId
                            : null;
                        invoice.VehicleId = this.VehicleId.HasValue && this.VehicleId.Value != Guid.Empty
                            ? this.VehicleId
                            : null;
                        invoice.VehiclePlateNumber = invoice.VehicleId.HasValue ? this.PlateNumber : null;

                        // Do not write Volume, UnitPrice, DiscountPercentage,
                        // TotalPrice, normalized volume or CRC back to the physical
                        // SalesTransaction. Do not rewrite its persisted InvoiceLine
                        // from editor values either. RecalculateInvoice reads the
                        // linked transaction and preserves its authoritative gross.
                        invoice.RecalculateInvoice();
                        if(hasDifferentPaymentType)
                        {
                            //invoice.CreateFinTransactions
                        }
                        db.SaveChanges();
                    }
                    return true;
                }
                catch(Exception ex)
                {
                    Logging.Logger.Instance.LogToFile("CreateNewSale::", ex);
                    this.AddError("Σφάλμα παραστατικού", ex.Message);
                    return false;
                }
            }
            if(this.lines.Count > 1)
            {
                var lns = lines.ToArray();
                foreach(var line in lns)
                {
                    if (line.InvoiceLineId != this.CurrentLine.InvoiceLineId)
                        lines.Remove(line);
                }
            }
            using (DatabaseModel db = new DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                try
                {
                    decimal vatValue = GetConfiguredVatPercentage();
                    Nozzle lastNozzle = db.Nozzles.Where(n => n.NozzleId == this.CurrentLine.CurrentNozzleId).FirstOrDefault();
                    if (lastNozzle == null)
                        throw new InvalidOperationException("Δεν βρέθηκε το επιλεγμένο ακροσωλήνιο.");
                    if (lastNozzle.DispenserId != this.CurrentLine.CurrentDispenserId)
                        throw new InvalidOperationException("Το επιλεγμένο ακροσωλήνιο δεν ανήκει στην επιλεγμένη αντλία.");
                    if (lastNozzle.FuelTypeId != this.CurrentLine.FuelTypeId)
                        throw new InvalidOperationException("Το επιλεγμένο καύσιμο δεν συμφωνεί με το καύσιμο του ακροσωληνίου.");
                    SalesTransaction sale = new SalesTransaction();
                    sale.SalesTransactionId = Guid.NewGuid();


                    sale.ApplicationUserId = DatabaseModel.CurrentUserId;
                    sale.NozzleId = lastNozzle.NozzleId;
                    sale.Nozzle = lastNozzle;
                    sale.TransactionTimeStamp = DateTime.Now;
                    sale.DiscountPercentage = lastNozzle.DiscountPercentage;
                    lastNozzle.DiscountPercentage = 0;
                    sale.TotalizerStart = lastNozzle.TotalCounter;
                    sale.TotalizerEnd = lastNozzle.TotalCounter;
                    sale.Volume = this.CurrentLine.Volume;//nSale.TotalVolume;
                    if (Data.DatabaseModel.CurrentShiftId != Guid.Empty)
                        sale.ShiftId = Data.DatabaseModel.CurrentShiftId;
                    sale.UnitPrice = this.CurrentLine.UnitPrice;

                    db.Add(sale);

                    ////nSale.TotalPrice;
                    //decimal totalVol = 0;
                    //decimal densityAvg = 0;
                    //List<decimal> densities = new List<decimal>();
                    foreach (NozzleFlow nzt in lastNozzle.NozzleFlows)
                    {
                        if (nzt.FlowState == 0)
                            continue;
                        Data.Tank tank = db.Tanks.Where(t => t.TankId == nzt.TankId).FirstOrDefault();
                        TankSale tSale = new TankSale();
                        tSale.TankSaleId = Guid.NewGuid();
                        decimal temperature = tank.Temperatire;
                        tSale.TankId = tank.TankId;
                        sale.TankSales.Add(tSale);
                        tSale.SalesTransactionId = sale.SalesTransactionId;
                        tSale.SalesTransaction = sale;
                        tSale.StartTemperature = temperature;
                        tSale.EndTemperature = temperature;
                        tSale.StartLevel = tank.FuelLevel;
                        tSale.EndLevel = tank.FuelLevel;
                        tSale.StartVolume = tank.GetTankVolume(tSale.StartLevel);
                        tSale.EndVolume = tank.GetTankVolume(tSale.EndLevel.Value);
                        tSale.StartVolumeNormalized = lastNozzle.FuelType.NormalizeVolume(tSale.StartVolume, tSale.StartTemperature.Value, tank.CurrentDensity);
                        tSale.EndVolumeNormalized = lastNozzle.FuelType.NormalizeVolume(tSale.EndVolume.Value, tSale.EndTemperature, tank.CurrentDensity);
                        tSale.FuelDensity = tank.CurrentDensity;
                    }
                    //if (sale.TankSales.Count == 0)
                    //    return;
                    //decimal startTempAvg = 0;
                    //decimal endTempAvg = 0;
                    //if (totalVol != 0)
                    //{
                    //    foreach (TankSale tSale in sale.TankSales)
                    //    {
                    //        decimal vol = tSale.EndVolumeNormalized.Value - tSale.StartVolumeNormalized;
                    //        startTempAvg = startTempAvg + tSale.StartTemperature.Value * (vol / totalVol);
                    //        endTempAvg = endTempAvg + tSale.EndTemperature * (vol / totalVol);
                    //    }
                    //}
                    //else
                    //{
                    //    if (sale.TankSales.Count > 0)
                    //    {
                    //        foreach (TankSale tSale in sale.TankSales)
                    //        {
                    //            startTempAvg = startTempAvg + tSale.StartTemperature.Value;
                    //            endTempAvg = endTempAvg + tSale.EndTemperature;
                    //        }
                    //        startTempAvg = startTempAvg / sale.TankSales.Count;
                    //        endTempAvg = endTempAvg / sale.TankSales.Count;
                    //    }

                    //}
                    //sale.TemperatureStart = startTempAvg;
                    //sale.TemperatureEnd = endTempAvg;
                    //try
                    //{
                    //    if (totalVol == 0)
                    //    {
                    //        if (densities.Count > 0)
                    //        {
                    //            densityAvg = densities.Sum() / densities.Count;
                    //            sale.VolumeNormalized = lastNozzle.FuelType.NormalizeVolume(sale.Volume, sale.TemperatureEnd, densityAvg);
                    //        }
                    //        else
                    //            sale.VolumeNormalized = lastNozzle.FuelType.NormalizeVolume(sale.Volume, sale.TemperatureEnd, lastNozzle.FuelType.BaseDensity);

                    //    }
                    //    else
                    //        sale.VolumeNormalized = lastNozzle.FuelType.NormalizeVolume(sale.Volume, sale.TemperatureEnd, densityAvg / totalVol);
                    //}
                    //catch
                    //{
                    //    sale.VolumeNormalized = lastNozzle.FuelType.NormalizeVolume(sale.Volume, sale.TemperatureEnd, lastNozzle.FuelType.BaseDensity);
                    //}

                    //if ((sale.Volume > sale.VolumeNormalized && (sale.VolumeNormalized == 0 || sale.Volume / sale.VolumeNormalized > 2)) ||
                    //    (sale.VolumeNormalized > sale.Volume && (sale.Volume == 0 || sale.VolumeNormalized / sale.Volume > 2)))
                    //{
                    //    try
                    //    {
                    //        Guid[] tankIds = lastNozzle.NozzleFlows.Where(n => n.FlowState == 0).Select(t => t.TankId).ToArray();
                    //        decimal dencityAvg = db.Tanks.Where(t => tankIds.Contains(t.TankId)).ToList().Select(t => t.CurrentDensity).Average();
                    //        sale.VolumeNormalized = lastNozzle.FuelType.NormalizeVolume(sale.Volume, sale.TemperatureEnd, dencityAvg);
                    //    }
                    //    catch
                    //    {
                    //        sale.VolumeNormalized = lastNozzle.FuelType.NormalizeVolume(sale.Volume, sale.TemperatureEnd, lastNozzle.FuelType.BaseDensity);
                    //    }
                    //}
                    //if (sale.Volume > 0 && sale.VolumeNormalized / sale.Volume < (decimal)0.8)
                    //    sale.VolumeNormalized = sale.Volume;

                    Data.Tank ct = lastNozzle.NozzleFlows.Where(n => n.FlowState == 1).First().Tank;
                    sale.TemperatureStart = ct.Temperatire;
                    sale.TemperatureEnd = ct.Temperatire;
                    sale.VolumeNormalized = lastNozzle.FuelType.NormalizeVolume(sale.Volume, sale.TemperatureEnd, ct.CurrentDensity);

                    UsagePeriod up = db.GetUsagePeriod();
                    sale.UsagePeriodId = up.UsagePeriodId;
                    sale.UsagePeriod = up;

                    sale.DiscountPercentage = this.CurrentLine.DiscountPercentage;
                    sale.TotalPrice = decimal.Round(this.CurrentLine.TotalPrice, 2);
                    sale.CRC = Common.Helpers.CalculateCRC32(sale);


                    InvoicePrint ip = sale.Nozzle.Dispenser.InvoicePrints.FirstOrDefault();
                    if (ip == null)
                        throw new InvalidOperationException("Δεν υπάρχει ρύθμιση εκτύπωσης για τον επιλεγμένο διανομέα.");

                    Invoice invoice = new Invoice();
                    invoice.ApplicationUserId = DatabaseModel.CurrentUserId;
                    invoice.InvoiceId = this.InvoiceId;
                    db.Add(invoice);
                    invoice.TransactionDate = DateTime.Now;
                    if (isLiterCheck)// lastNozzle.NozzleState == (int)Common.Enumerators.NozzleStateEnum.LiterCheck)
                    {
                        Guid invoiceType = Data.Implementation.OptionHandler.Instance.GetGuidOption("LiterCheckInvoiceType", Guid.Empty);
                        InvoiceType invType = db.InvoiceTypes.Where(it => it.InvoiceTypeId == invoiceType).FirstOrDefault();
                        invoice.InvoiceTypeId = invoiceType;
                        invoice.InvoiceType = invType;
                        //invoice.Number = invType.LastNumber + 1;
                        //invType.LastNumber = invoice.Number;
                        lastNozzle.NozzleState = (int)Common.Enumerators.NozzleStateEnum.Normal;
                        Console.WriteLine("LITER CHECK CREATED");
                    }
                    else
                    {
                        Guid invoiceType = this.InvoiceTypeId;
                        if (invoiceType == Guid.Empty)
                            invoiceType = ip.DefaultInvoiceType;
                        InvoiceType invType = db.InvoiceTypes.Where(it => it.InvoiceTypeId == invoiceType).FirstOrDefault();

                        invoice.InvoiceTypeId = invoiceType;
                        invoice.InvoiceType = invType;
                        //invoice.Number = invType.LastNumber + 1;
                        //invType.LastNumber = invoice.Number;
                        // A trader may be selected without a vehicle (notably the
                        // myDiesel pump-sale flow). Persist the two selections
                        // independently; previously TraderId was silently dropped
                        // unless VehicleId was also present.
                        if (this.TraderId.HasValue && this.TraderId.Value != Guid.Empty)
                            invoice.TraderId = this.TraderId;
                        if (this.VehicleId.HasValue && this.VehicleId.Value != Guid.Empty)
                        {
                            invoice.VehicleId = this.VehicleId;
                            invoice.VehiclePlateNumber = this.PlateNumber;
                        }
                    }
                    // Payment belongs to the invoice, not to its optional trader or
                    // vehicle. Anonymous retail pump sales must persist it as well.
                    invoice.PaymentType = this.PaymentType.Value;
                    invoice.InvoiceFormId = invoice.InvoiceType.InvoiceFormId;
                    if (lastNozzle.Dispenser.InvoicePrints.Count > 0)
                        invoice.Printer = lastNozzle.Dispenser.InvoicePrints.First().Printer;
                    if (invoice.Printer == null || invoice.Printer == "" || (invoice.InvoiceType != null && invoice.InvoiceType.IsLaserPrint.HasValue && invoice.InvoiceType.IsLaserPrint.Value))
                        invoice.Printer = invoice.InvoiceType.Printer;

                    decimal vatMult = 1 + vatValue / 100;
                    if (this.TraderId.HasValue)
                    {
                        var trader = db.Traders.SingleOrDefault(t => t.TraderId == this.TraderId.Value);
                        if (trader == null)
                            throw new InvalidOperationException("Δεν βρέθηκε ο επιλεγμένος συναλλασσόμενος.");
                        if (trader.VatExemption.HasValue && trader.VatExemption.Value)
                        {
                            decimal vatAmount = this.CurrentLine.TotalPrice - decimal.Round(this.CurrentLine.TotalPrice / vatMult, 2);
                            invoice.TotalAmount = this.CurrentLine.TotalPrice;// this.CurrentLine.TotalPrice - vatAmount;
                            invoice.VatAmount = 0;
                            invoice.NettoAmount = invoice.TotalAmount;
                            if (invoice.Notes == null || invoice.Notes == "")
                                invoice.Notes = trader.VatExemptionReason;
                            else
                                invoice.Notes = invoice.Notes + "\r\n" + trader.VatExemptionReason;
                            vatMult = 1;
                            vatValue = 0;
                        }
                        else
                        {
                            invoice.TotalAmount = this.CurrentLine.TotalPrice;
                            invoice.VatAmount = this.CurrentLine.TotalPrice - decimal.Round(invoice.TotalAmount.Value / vatMult, 2);
                            invoice.NettoAmount = this.CurrentLine.TotalPrice - invoice.VatAmount;
                        }
                        if (sale.Nozzle.FuelType.SupportsSupplyNumber.HasValue && sale.Nozzle.FuelType.SupportsSupplyNumber.Value)
                        {
                            invoice.SupplyNumber = trader.SupplyNumber == null ? "" : trader.SupplyNumber;
                        }
                        invoice.DeliveryAddress = trader.DeliveryAddress == null ? "" : trader.DeliveryAddress;
                    }
                    else
                    {
                        invoice.TotalAmount = this.CurrentLine.TotalPrice;
                        invoice.VatAmount = this.CurrentLine.TotalPrice - decimal.Round(invoice.TotalAmount.Value / vatMult, 2);
                        invoice.NettoAmount = this.CurrentLine.TotalPrice - invoice.VatAmount;
                    }
                    invoice.DiscountAmount = 0;


                    InvoiceLine invLine = new InvoiceLine();
                    invLine.InvoiceLineId = Guid.NewGuid();
                    db.Add(invLine);
                    invLine.InvoiceId = invoice.InvoiceId;
                    invLine.Invoice = invoice;
                    invoice.InvoiceLines.Add(invLine);
                    invLine.SaleTransactionId = sale.SalesTransactionId;
                    invLine.SalesTransaction = sale;
                    invLine.Temperature = sale.TemperatureStart;
                    invLine.FuelDensity = sale.TankSales.Count > 0 ? sale.TankSales.First().FuelDensity : sale.Nozzle.FuelType.BaseDensity;
                    invLine.TotalPrice = invoice.TotalAmount.Value;// currentSale.TotalPrice;
                    invLine.FuelTypeId = lastNozzle.FuelTypeId;
                    invLine.FuelType = lastNozzle.FuelType;
                    invLine.UnitPrice = sale.UnitPrice;
                    invLine.UnitPriceWhole = sale.UnitPrice / vatMult;
                    invLine.UnitPriceRetail = sale.UnitPrice;
                    invLine.Volume = sale.Volume;
                    invLine.VolumeNormalized = sale.VolumeNormalized;
                    invLine.VatAmount = invoice.VatAmount.Value;// currentSale.TotalPrice - decimal.Round(invLine.TotalPrice / vatMult, 2);
                    invLine.VatPercentage = vatValue;
                    invLine.DiscountAmount = 0;
                    sale.InvoiceLines.Add(invLine);
                    if (isLiterCheck)
                    {
                        Guid tankId = lastNozzle.NozzleFlows.Select(n => n.TankId).FirstOrDefault();
                        invLine.TankId = tankId;
                    }
                    invoice.InvoiceLines.Add(invLine);

                    invLine.DiscountPercentage = this.CurrentLine.DiscountPercentage;
                    // CurrentLine.TotalPrice and sale.TotalPrice are already after
                    // discount. Recalculate once from that payable gross; the former
                    // code subtracted a second discount while saving the pump sale.
                    invoice.RecalculateInvoice();
                    // Creation only persists the sale and invoice. The shared workflow
                    // assigns the fiscal number, submits it and marks it printed after
                    // the printer handoff succeeds.
                    invoice.IsPrinted = false;
                    invoice.Notes = this.Notes;
                    db.SaveChanges();
                    return true;
                }
                catch (Exception ex)
                {
                    Logging.Logger.Instance.LogToFile("CreateNewSale::", ex);
                    this.AddError("Σφάλμα παραστατικού", ex.Message);
                    return false;
                }
            }
        }

        public void RecalculateSums()
        {
            if (SuspendRecalculation)
                return;
            if (this.lines.Count > 0 && this.InvoiceLines.All(line => line.PreserveFiscalAmounts))
                return;

            if (this.VatExemption)
            {
                foreach (var invLine in this.InvoiceLines)
                {
                    if (invLine.SalesTransactionView != null)
                    {
                        var unitPrice = this.GetUnitPriceFromRetailPrice(invLine,
                            invLine.SalesTransactionView.UnitPrice);
                        invLine.UnitPriceRetail = unitPrice;
                        invLine.UnitPrice = unitPrice;
                        invLine.UnitPriceWhole = unitPrice;
                    }
                    if (invLine.VatPercentage != 0)
                        invLine.VatPercentage = 0;
                }
            }

            decimal net = 0;
            decimal netBeforeDiscount = 0;
            decimal tot = 0;
            decimal vat = 0;
            decimal disc = 0;
            decimal discWhole = 0;
            foreach (var line in this.InvoiceLines)
            {
                decimal totalDiscounted = 0;
                decimal totalPreDiscount = 0;
                decimal discPercentage = 0;
                decimal discountRetail = 0;
                decimal discountWhole = 0;
                decimal netPreDiscount = 0;
                decimal netDiscounted = 0;
                decimal lineVat = 0;
                decimal volume = line.Volume;
                if(this.SelectedInvoiceType.TransactionType == 1 && (!this.SelectedInvoiceType.IsInternal.HasValue || !this.SelectedInvoiceType.IsInternal.Value))
                    volume = line.VolumeNormalized;
                if (line.SalesTransactionView != null && (this.SelectedInvoiceType == null || this.SelectedInvoiceType.OfficialEnumerator != 178))
                {
                    totalPreDiscount = decimal.Round(volume * line.UnitPrice, 2);
                    discPercentage = line.SalesTransactionView.DiscountPercentage.HasValue ? line.SalesTransactionView.DiscountPercentage.Value : 0;
                    discountRetail = decimal.Round((discPercentage * totalPreDiscount) / 100, 2);
                    // The physical sale is immutable and its saved dispenser total
                    // is authoritative. Independently rounded volume and unit price
                    // can multiply one cent away from that total.
                    totalDiscounted = line.UsesAuthoritativeDispenserTotal
                        ? decimal.Round(line.SalesTransactionView.TotalPrice, 2)
                        : totalPreDiscount - discountRetail;
                    totalPreDiscount = totalDiscounted + discountRetail;
                }
                else
                {
                    if (this.PricesIncludeVat)
                    {
                        discPercentage = line.DiscountPercentage;
                        discountRetail = decimal.Round((discPercentage * decimal.Round(line.Volume * line.UnitPrice, 2)) / 100, 2);
                        totalDiscounted = decimal.Round(volume * line.UnitPrice, 2) - discountRetail;
                        totalPreDiscount = totalDiscounted + discountRetail;
                    }
                    else
                    {
                        discountRetail = line.DiscountAmountRetail;
                        totalDiscounted = decimal.Round(volume * line.UnitPrice, 2) - discountRetail;
                        totalPreDiscount = totalDiscounted + discountRetail;
                    }
                }
                discountWhole = decimal.Round((100 * discountRetail) / (100 + line.VatPercentage), 2);
                netPreDiscount = decimal.Round((100 * totalPreDiscount) / (100 + line.VatPercentage), 2);
                netDiscounted = decimal.Round((100 * totalDiscounted) / (100 + line.VatPercentage), 2);
                lineVat = totalDiscounted - netDiscounted;

                line.TotalPrice = totalDiscounted;
                line.DiscountAmount = discountRetail;
                line.DiscountAmountRetail = discountRetail;
                line.DiscountAmountWhole = discountWhole;
                line.VatAmount = lineVat;

                netBeforeDiscount = netBeforeDiscount + netPreDiscount;
                net = net + netDiscounted;// line.TotalPrice - line.VatAmount + line.DiscountAmountWhole;
                tot = tot + line.TotalPrice;
                vat = vat + line.VatAmount;
                disc = disc + discountRetail;
                discWhole = discWhole + discountWhole;
            }
            // decimal.Round(tot, 2);
            net = decimal.Round(net, 2);
            netBeforeDiscount = decimal.Round(netBeforeDiscount, 2);
            vat = decimal.Round(vat, 2);
            disc = decimal.Round(disc, 2);
            //tot = net + vat - disc;
            // Fiscal totals are composed from the independently rounded payable
            // net and net discount. Dividing the pre-discount gross can differ by
            // one cent and produces a value the provider cannot reconcile.
            this.NettoAmount = net + discWhole;
            this.NettoAfterDiscount = net;
            this.TotalAmount = decimal.Round(tot, 2);
            this.DiscountAmountWhole = discWhole;
            this.DiscountAmount = disc;
            this.DiscountAmountRetail = disc;
            //this.DiscountAmountView = disc;
            this.VatAmount = vat;
        }

        public void CreateNew()
        {
            this.IsEditLocked = false;
            ConfigureTransformationPriceEditPolicy(false, true, false, null);
            this.InvoiceId = Guid.NewGuid();
            this.TransactionDate = DateTime.Now;
            this.PaymentType = this.PaymentTypes.First().Value;
            this.ApplicationUserId = Data.DatabaseModel.CurrentUserId;
            this.EntityState = EntityStateEnum.Added;
            this.OnPropertyChanged("ReportPrint");
            this.OnPropertyChanged("SimplePrint");
        }

        bool? simplePrintTransform = null;

        public Guid TransformInvoices(Guid[] invoiceIds, Guid invoiceType, Guid? traderId, Guid? vehicleId, int mode)
        {
            return TransformInvoices(invoiceIds, invoiceType, traderId, vehicleId, mode, true);
        }

        public Guid TransformInvoices(Guid[] invoiceIds, Guid invoiceType, Guid? traderId, Guid? vehicleId,
            int mode, bool createFiscalRelations)
        {
            Threads.SharedInvoiceRuntime.ValidateCancellationTransformation(invoiceIds, invoiceType);
            Common.Enumerators.InvoiceTransformationTypeEnum transMode = (Common.Enumerators.InvoiceTransformationTypeEnum)mode;
            using (Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                var invoice = db.Invoices.Where(i => i.InvoiceId == invoiceIds[0]).SingleOrDefault();
                if (invoice == null)
                    return Guid.Empty;

                var invType = db.InvoiceTypes.SingleOrDefault(i => i.InvoiceTypeId == invoiceType);
                if (invType == null)
                    return Guid.Empty;

                Guid fromType = db.Invoices.Where(i => invoiceIds.Contains(i.InvoiceId)).Select(i => i.InvoiceTypeId).FirstOrDefault();
                string prName = db.Invoices.OrderBy(i => i.TransactionDate).Last().Printer;
                var transformationType = db.InvoiceTypeTransforms.Where(i =>
                    i.ParentInvoiceTypeId == fromType &&
                    i.ChildInvoiceTypeId == invoiceType &&
                    i.TransformationMode == mode)
                    .OrderByDescending(i => i.CreationInvoiceTypeId.HasValue)
                    .FirstOrDefault();
                if (transformationType == null)
                    return Guid.Empty;
                bool sourceHasFinancialData = invoice.InvoiceType != null && invoice.InvoiceType.ShowFinancialData == true;
                bool sourcePriceIncludesVat = SourcePriceIncludesVat(invoice);
                bool targetPriceIncludesVat = TargetPriceIncludesVat(invType);
                bool guideOnlyTarget = !createFiscalRelations ||
                    transformationType.CreationInvoiceTypeId.HasValue;
                bool oneToMany = transMode == Common.Enumerators.InvoiceTransformationTypeEnum.OneToMany;
                bool oneToOne = transMode == Common.Enumerators.InvoiceTransformationTypeEnum.OneToOne;
                string supplierVat = Data.Implementation.OptionHandler.Instance.GetOption("CompanyTIN");
                Guid? initialTraderId = traderId;
                if (!initialTraderId.HasValue && vehicleId.HasValue)
                {
                    var initialVehicle = db.Vehicles.SingleOrDefault(v => v.VehicleId == vehicleId.Value);
                    if (initialVehicle != null)
                        initialTraderId = initialVehicle.TraderId;
                }
                string initialCounterpartVat = initialTraderId.HasValue
                    ? db.Traders.Where(t => t.TraderId == initialTraderId.Value)
                        .Select(t => t.TaxRegistrationNumber).FirstOrDefault()
                    : null;
                bool sameVat = TransformationEditPolicy.VatNumbersEqual(
                    supplierVat, initialCounterpartVat);
                bool intrinsicLineSelection = oneToMany || (oneToOne && !sourceHasFinancialData);
                bool priceOverrideSelection = this.QueryInvoiceData != null &&
                    (guideOnlyTarget || sameVat);
                bool requiresLineSelection = intrinsicLineSelection || priceOverrideSelection;
                this.EntityState = EntityStateEnum.Added;
                this.AllowEdit = true;
                this.IsPrinted = false;
                this.InvoiceId = Guid.NewGuid();
                this.TraderId = traderId;
                this.VehicleId = vehicleId;
                this.InvoiceTypeId = invoiceType;
                this.Series = invType.DefaultSeries;
                this.TransactionDate = DateTime.Now;
                this.NettoAmount = 0;
                this.VatAmount = 0;
                this.DiscountAmount = 0;
                this.TotalAmount = 0;
                this.IsEditLocked = true;
                this.Printer = prName;

                this.PaymentType = invoice.PaymentType;
                this.lines.Clear();
                this.oneToManyLineSelections.Clear();
                var lines = db.InvoiceLines.Where(i => invoiceIds.Contains(i.InvoiceId)).ToArray().
                    OrderBy(i=>i.InvoiceDate.Date).ThenBy(i=>i.InvoiceId).ToArray();
                var desc = db.Invoices.Where(i => invoiceIds.Contains(i.InvoiceId)).ToArray().Select(i => i.Description).ToArray();
                this.Notes = transformationType.NotesAddition + ": " + string.Join(", ", desc);
                if (this.VatExemptionReason != null && this.VatExemptionReason != "")
                    this.Notes = this.Notes + "\r\n" + this.VatExemptionReason;
                Dictionary<Guid, Guid> lineRelations = new Dictionary<Guid, Guid>();
                if (requiresLineSelection)
                {
                    if (oneToMany && invoiceIds.Length != 1)
                        return Guid.Empty;
                    if (this.QueryInvoiceData != null)
                    {
                        this.TraderId = null;
                        this.VehicleId = null;
                        List<VolumeData> volumeData = new List<VolumeData>();
                        foreach (var line in lines)
                        {
                            decimal consumedVolume = transMode == Common.Enumerators.InvoiceTransformationTypeEnum.OneToMany
                                ? GetConsumedTransformationVolume(line)
                                : 0;
                            decimal remainingVolume = line.Volume - consumedVolume;
                            if (remainingVolume <= 0)
                                continue;
                            VolumeData vd = new VolumeData();
                            vd.Description = line.FuelType.Name;
                            vd.InvoiceLineId = line.InvoiceLineId;
                            vd.TaxValue = line.VatAmount;
                            vd.VatPercentage = line.VatPercentage;
                            bool linePriceIncludesVat = sourcePriceIncludesVat ||
                                line.SalesTransaction != null;
                            var sourcePrices = linePriceIncludesVat
                                ? TransformationUnitPrice.FromRetail(
                                    SourceRetailUnitPrice(line), line.VatPercentage)
                                : TransformationUnitPrice.FromNet(
                                    SourceNetUnitPrice(line), line.VatPercentage);
                            vd.UnitPrice = targetPriceIncludesVat
                                ? sourcePrices.Retail
                                : sourcePrices.Net;
                            vd.MaxAllowedVolume = remainingVolume;
                            vd.Volume = transMode == Common.Enumerators.InvoiceTransformationTypeEnum.OneToMany
                                ? remainingVolume
                                : line.Volume;
                            vd.DiscountAmount = targetPriceIncludesVat
                                ? line.DiscountAmountRetail
                                : line.DiscountAmountWhole;
                            volumeData.Add(vd);
                        }
                        QueryInvoiceDataArgs args = new QueryInvoiceDataArgs();
                        args.Volumes = volumeData.ToArray();
                        args.AllowVolumeEdit = TransformationEditPolicy.AllowVolumeEdit(
                            oneToMany, guideOnlyTarget);
                        args.ConfigureUnitPriceEditPolicy(sourceHasFinancialData,
                            guideOnlyTarget, supplierVat);
                        if (traderId.HasValue)
                            args.TraderId = traderId.Value;
                        if (vehicleId.HasValue)
                            args.VehicleId = vehicleId.Value;
                        this.QueryInvoiceData(this, args);
                        if (args.Cancel)
                            return Guid.Empty;
                        
                        this.TraderId = args.TraderId;
                        if (args.VehicleId != Guid.Empty)
                            this.VehicleId = args.VehicleId;

                        foreach (var vd in args.Volumes)
                        {
                            var line = lines.SingleOrDefault(i => i.InvoiceLineId == vd.InvoiceLineId);
                            if (line == null)
                                continue;
                            InvoiceLineViewModel invLine = new InvoiceLineViewModel();
                            invLine.IsTransformation = true;
                            invLine.ParentInvoice = this;
                            invLine.EntityState = EntityStateEnum.Added;
                            invLine.DiscountAmount = line.DiscountAmount;
                            invLine.DiscountAmountRetail = line.DiscountAmountRetail;
                            invLine.DiscountAmountWhole = line.DiscountAmountWhole;
                            invLine.DiscountPercentage = line.DiscountPercentage;
                            invLine.FuelDensity = line.FuelDensity;
                            invLine.FuelTypeId = line.FuelTypeId;
                            invLine.InvoiceId = this.InvoiceId;
                            invLine.InvoiceLineId = Guid.NewGuid();
                            invLine.TankId = line.TankId;
                            invLine.Temperature = line.Temperature;
                            invLine.TotalPrice = line.TotalPrice;
                            invLine.VatAmount = line.VatAmount;
                            invLine.VatPercentage = line.VatPercentage;
                            invLine.Volume = vd.Volume > vd.MaxAllowedVolume ? vd.MaxAllowedVolume : vd.Volume;
                            decimal volumeRatio = line.Volume == 0 ? 0 : invLine.Volume / line.Volume;
                            invLine.VolumeNormalized = line.Volume == 0
                                ? invLine.Volume
                                : decimal.Round(line.VolumeNormalized * volumeRatio, 3);
                            if (args.AllowUnitPriceEdit)
                            {
                                ApplyEnteredTransformationDiscount(invLine,
                                    vd.DiscountAmount, line.VatPercentage,
                                    targetPriceIncludesVat);
                            }
                            else
                            {
                                invLine.DiscountAmount = decimal.Round(line.DiscountAmount * volumeRatio, 2);
                                invLine.DiscountAmountRetail = decimal.Round(line.DiscountAmountRetail * volumeRatio, 2);
                                invLine.DiscountAmountWhole = decimal.Round(line.DiscountAmountWhole * volumeRatio, 2);
                            }
                            // Transformation provenance is represented by the invoice
                            // and line relations created below. The physical pump sale
                            // remains owned only by its directly-created invoice line.
                            invLine.TankFillingId = line.TankFillingId;
                            invLine.SetSourceNozzleContext(line);
                            if (args.AllowUnitPriceEdit)
                                ApplyEnteredTransformationPrice(invLine, vd.UnitPrice,
                                    line.VatPercentage, targetPriceIncludesVat);
                            else
                                ApplyAuthoritativeSourcePricing(invLine, line, sourcePriceIncludesVat);
                            bool linePriceIncludesVat = sourcePriceIncludesVat ||
                                line.SalesTransaction != null;
                            this.ApplyVatExemptionToTransformedLine(invLine,
                                args.AllowUnitPriceEdit ? vd.UnitPrice :
                                    (linePriceIncludesVat ? SourceRetailUnitPrice(line) : SourceNetUnitPrice(line)),
                                line.VatPercentage,
                                args.AllowUnitPriceEdit ? targetPriceIncludesVat : linePriceIncludesVat);
                            invLine.PropertyChanged += Ilvm_PropertyChanged;
                            this.lines.Add(invLine);
                            invLine.SetComputedColumns(false, false);
                            lineRelations.Add(line.InvoiceLineId, invLine.InvoiceLineId);
                            if (transMode == Common.Enumerators.InvoiceTransformationTypeEnum.OneToMany)
                                this.oneToManyLineSelections[line.InvoiceLineId] = invLine.InvoiceLineId;
                        }
                    }
                }
                else
                {
                    if (this.SimplePrint || this.DispenserType)
                    {
                        this.lines.Clear();
                    }
                    foreach (var line in lines)
                    {
                        InvoiceLineViewModel invLine = new InvoiceLineViewModel();
                        invLine.IsTransformation = true;
                        invLine.ParentInvoice = this;
                        invLine.EntityState = EntityStateEnum.Added;
                        invLine.DiscountAmount = line.DiscountAmount;
                        invLine.DiscountAmountRetail = line.DiscountAmountRetail;
                        invLine.DiscountAmountWhole = line.DiscountAmountWhole;
                        invLine.DiscountPercentage = line.DiscountPercentage;
                        invLine.FuelDensity = line.FuelDensity;
                        invLine.FuelTypeId = line.FuelTypeId;
                        invLine.InvoiceId = this.InvoiceId;
                        invLine.InvoiceLineId = Guid.NewGuid();
                        invLine.TankId = line.TankId;
                        invLine.Temperature = line.Temperature;
                        invLine.VatPercentage = line.VatPercentage;
                        invLine.VatAmount = line.VatAmount;
                        invLine.TotalPrice = line.TotalPrice;
                        ApplyAuthoritativeSourcePricing(invLine, line, sourcePriceIncludesVat);
                        invLine.Volume = line.Volume;
                        invLine.VolumeNormalized = line.VolumeNormalized;
                        invLine.TankFillingId = line.TankFillingId;
                        invLine.PropertyChanged += Ilvm_PropertyChanged;

                        // Display the physical source pump/nozzle for every
                        // transformed fuel line, including ordinary credit notes.
                        // Do not copy SaleTransactionId to the derived document.
                        invLine.SetSourceNozzleContext(line);

                        // Setting volume/nozzle raises the ordinary invoice-line
                        // calculations. Restore the fiscal source values after all
                        // such setters so a locked transformation remains exact.
                        invLine.DiscountAmount = line.DiscountAmount;
                        invLine.DiscountAmountRetail = line.DiscountAmountRetail;
                        invLine.DiscountAmountWhole = line.DiscountAmountWhole;
                        invLine.DiscountPercentage = line.DiscountPercentage;
                        ApplyAuthoritativeSourcePricing(invLine, line, sourcePriceIncludesVat);
                        invLine.VatPercentage = line.VatPercentage;
                        invLine.VatAmount = line.VatAmount;
                        invLine.TotalPrice = line.TotalPrice;
                        bool linePriceIncludesVat = sourcePriceIncludesVat ||
                            line.SalesTransaction != null;
                        this.ApplyVatExemptionToTransformedLine(invLine,
                            linePriceIncludesVat ? SourceRetailUnitPrice(line) : SourceNetUnitPrice(line),
                            line.VatPercentage, linePriceIncludesVat);

                        this.lines.Add(invLine);
                        lineRelations.Add(line.InvoiceLineId, invLine.InvoiceLineId);
                    }
                }
                foreach (var id in invoiceIds)
                {
                    if (invoice == null)
                        continue;
                    InvoiceRelationViewModel relation = new InvoiceRelationViewModel();
                    relation.EntityState = EntityStateEnum.Added;
                    relation.InvoiceRelationId = Guid.NewGuid();
                    relation.ParentInvoiceId = id;
                    relation.ChildInvoiceId = this.InvoiceId;
                    var invPar = db.Invoices.SingleOrDefault(i => i.InvoiceId == id);
                    bool localDeliveryToSale = invPar != null &&
                        Invoicing.Mapping.HostInvoiceMapper.IsDeliveryDocument(invPar.InvoiceType) &&
                        !Invoicing.Mapping.HostInvoiceMapper.IsDeliveryDocument(invType) &&
                        invType.HasFinancialTransactions != false &&
                        invType.IsCancelation != true;
                    bool fiscalRelation = createFiscalRelations && !localDeliveryToSale;
                    relation.RelationType = fiscalRelation
                        ? (int)Common.Enumerators.InvoiceRelationTypeEnum.Replace
                        : (int)Common.Enumerators.InvoiceRelationTypeEnum.LocalCopy;
                    if (fiscalRelation &&
                        (transformationType.TransformationMode == (int)Common.Enumerators.InvoiceTransformationTypeEnum.Cancelation ||
                         (this.SelectedInvoiceType != null && this.SelectedInvoiceType.IsCancelation == true)))
                        relation.RelationType = (int)Common.Enumerators.InvoiceRelationTypeEnum.Cancel;
                    this.invRelations.Add(relation);
                    foreach (Guid plid in lineRelations.Keys)
                    {
                        if (invPar.InvoiceLines.SingleOrDefault(l => l.InvoiceLineId == plid) == null)
                            continue;
                        InvoiceLineRelationViewModel lineRelation = new InvoiceLineRelationViewModel();
                        lineRelation.InvoiceLineRelationId = Guid.NewGuid();
                        lineRelation.ParentLineId = plid;
                        lineRelation.ChildRelationId = lineRelations[plid];
                        lineRelation.InvoiceRelationId = relation.InvoiceRelationId;
                        lineRelation.EntityState = EntityStateEnum.Added;

                        relation.AddRelation(lineRelation);
                    }
                }
                if (requiresLineSelection ||
                    (this.VatExemption &&
                     (sourcePriceIncludesVat || lines.Any(line => line.SalesTransaction != null)) &&
                     transMode != Common.Enumerators.InvoiceTransformationTypeEnum.Cancelation))
                {
                    this.SuspendRecalculation = false;
                    this.RecalculateSums();
                    this.SuspendRecalculation = true;
                }
                else
                    this.SetInvoiceSums(db, invoiceIds, mode);

                ConfigureTransformationPriceEditPolicy(true,
                    sourceHasFinancialData, guideOnlyTarget,
                    ResolveCounterpartVat(db, this.TraderId, this.VehicleId),
                    TransformationEditPolicy.AllowVolumeEdit(oneToMany,guideOnlyTarget));

                this.ApplicationUserId = Data.DatabaseModel.CurrentUserId;

                var fit = db.InvoiceTypes.SingleOrDefault(i => i.InvoiceTypeId == fromType);
                if (fit != null)
                {
                    if (this.SelectedInvoiceType != null && this.SelectedInvoiceType.IsCancelation == true)
                        this.simplePrintTransform = !(fit.IsLaserPrint.HasValue ? fit.IsLaserPrint.Value : this.lines.Count != 1);
                }
            }
            return this.InvoiceId;
        }

        public void CreateInvoices(Guid[] invoiceIds, Guid invoiceType, Guid? traderId, Guid? vehicleId, int mode)
        {
            Threads.SharedInvoiceRuntime.ValidateCancellationTransformation(invoiceIds, invoiceType);
            Common.Enumerators.InvoiceTransformationTypeEnum transMode = (Common.Enumerators.InvoiceTransformationTypeEnum)mode;
            using (Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                var invoice = db.Invoices.Where(i => i.InvoiceId == invoiceIds[0]).SingleOrDefault();
                if (invoice == null)
                    return;

                var invType = db.InvoiceTypes.SingleOrDefault(i => i.InvoiceTypeId == invoiceType);
                if (invType == null)
                    return;
                this.InvoiceTypeId = invoiceType;
                Guid fromType = db.Invoices.Where(i => invoiceIds.Contains(i.InvoiceId)).Select(i => i.InvoiceTypeId).FirstOrDefault();
                var transformationType = db.InvoiceTypeTransforms.Where(i => i.ParentInvoiceTypeId == fromType && i.ChildInvoiceTypeId == invoiceType).FirstOrDefault();
                if (transformationType == null)
                    return;
                bool sourcePriceIncludesVat = SourcePriceIncludesVat(invoice);
                bool targetPriceIncludesVat = TargetPriceIncludesVat(invType);
                this.EntityState = EntityStateEnum.Added;
                this.InvoiceId = Guid.NewGuid();
                this.TraderId = traderId;
                this.VehicleId = vehicleId;
                this.InvoiceTypeId = invoiceType;
                this.Series = invType.DefaultSeries;
                this.TransactionDate = DateTime.Now;
                this.NettoAmount = 0;
                this.VatAmount = 0;
                this.DiscountAmount = 0;
                this.TotalAmount = 0;
                this.IsEditLocked = true;
                this.lines.Clear();
                var lines = db.InvoiceLines.Where(i => invoiceIds.Contains(i.InvoiceId)).ToArray().
                    OrderBy(i => i.InvoiceDate.Date).ThenBy(i => i.InvoiceId).ToArray();
                var desc = db.Invoices.Where(i => invoiceIds.Contains(i.InvoiceId)).ToArray().Select(i => i.Description).ToArray();
                this.Notes = transformationType.NotesAddition + ": " + string.Join(", ", desc);
                if (this.VatExemptionReason != null && this.VatExemptionReason != "")
                    this.Notes = this.Notes + "\r\n" + this.VatExemptionReason;
                Dictionary<Guid, Guid> lineRelations = new Dictionary<Guid, Guid>();
                if (transformationType.TransformationMode == (int)Common.Enumerators.InvoiceTransformationTypeEnum.OneToMany)
                {
                    if (invoiceIds.Length != 1)
                        return;
                    if (this.QueryInvoiceData != null)
                    {
                        this.TraderId = null;
                        this.VehicleId = null;
                        List<VolumeData> volumeData = new List<VolumeData>();
                        foreach (var line in lines)
                        {
                            var children = line.InvoiceLineRelations_ParentLineId.Select(c => c.InvoiceLine_ChildRelationId).ToArray();
                            decimal sumVolume = children.Sum(c => c.Volume);
                            if (line.Volume - sumVolume <= 0)
                                continue;
                            VolumeData vd = new VolumeData();
                            vd.Description = line.FuelType.Name;
                            vd.InvoiceLineId = line.InvoiceLineId;
                            vd.TaxValue = line.VatAmount;
                            vd.VatPercentage = line.VatPercentage;
                            bool linePriceIncludesVat = sourcePriceIncludesVat ||
                                line.SalesTransaction != null;
                            var sourcePrices = linePriceIncludesVat
                                ? TransformationUnitPrice.FromRetail(
                                    SourceRetailUnitPrice(line), line.VatPercentage)
                                : TransformationUnitPrice.FromNet(
                                    SourceNetUnitPrice(line), line.VatPercentage);
                            vd.UnitPrice = targetPriceIncludesVat
                                ? sourcePrices.Retail
                                : sourcePrices.Net;
                            vd.MaxAllowedVolume = line.Volume - sumVolume;
                            vd.Volume = line.Volume > vd.MaxAllowedVolume ? vd.MaxAllowedVolume : line.Volume;
                            vd.DiscountAmount = line.DiscountAmount;
                            volumeData.Add(vd);
                        }
                        QueryInvoiceDataArgs args = new QueryInvoiceDataArgs();
                        args.Volumes = volumeData.ToArray();
                        this.QueryInvoiceData(this, args);
                        if (args.Cancel)
                            return;

                        this.TraderId = args.TraderId;
                        if (args.VehicleId != Guid.Empty)
                            this.VehicleId = args.VehicleId;

                        foreach (var vd in args.Volumes)
                        {
                            var line = invoice.InvoiceLines.Where(i => i.InvoiceLineId == vd.InvoiceLineId).SingleOrDefault();
                            if (line == null)
                                continue;
                            InvoiceLineViewModel invLine = new InvoiceLineViewModel();
                            invLine.IsTransformation = true;
                            invLine.ParentInvoice = this;
                            invLine.EntityState = EntityStateEnum.Added;
                            invLine.DiscountAmount = line.DiscountAmount;
                            invLine.FuelDensity = line.FuelDensity;
                            invLine.FuelTypeId = line.FuelTypeId;
                            invLine.InvoiceId = this.InvoiceId;
                            invLine.InvoiceLineId = Guid.NewGuid();
                            invLine.TankId = line.TankId;
                            invLine.Temperature = line.Temperature;
                            invLine.TotalPrice = line.TotalPrice;
                            ApplyEnteredTransformationPrice(invLine, vd.UnitPrice,
                                line.VatPercentage, targetPriceIncludesVat);
                            invLine.VatAmount = line.VatAmount;
                            invLine.VatPercentage = line.VatPercentage;
                            invLine.Volume = vd.Volume > vd.MaxAllowedVolume ? vd.MaxAllowedVolume : vd.Volume;
                            invLine.VolumeNormalized = vd.Volume;
                            invLine.TankFillingId = line.TankFillingId;
                            invLine.SetSourceNozzleContext(line);
                            bool linePriceIncludesVat = sourcePriceIncludesVat ||
                                line.SalesTransaction != null;
                            this.ApplyVatExemptionToTransformedLine(invLine,
                                linePriceIncludesVat ? SourceRetailUnitPrice(line) : SourceNetUnitPrice(line),
                                line.VatPercentage, linePriceIncludesVat);
                            invLine.PropertyChanged += Ilvm_PropertyChanged;
                            this.lines.Add(invLine);
                            lineRelations.Add(line.InvoiceLineId, invLine.InvoiceLineId);
                        }
                    }
                }
                else
                {
                    if (this.SimplePrint || this.DispenserType)
                    {
                        this.lines.Clear();
                    }
                    foreach (var line in lines)
                    {
                        InvoiceLineViewModel invLine = new InvoiceLineViewModel();
                        invLine.IsTransformation = true;
                        invLine.ParentInvoice = this;
                        invLine.EntityState = EntityStateEnum.Added;
                        invLine.DiscountAmount = line.DiscountAmount;
                        invLine.FuelDensity = line.FuelDensity;
                        invLine.FuelTypeId = line.FuelTypeId;
                        invLine.InvoiceId = this.InvoiceId;
                        invLine.InvoiceLineId = Guid.NewGuid();
                        invLine.TankId = line.TankId;
                        invLine.Temperature = line.Temperature;
                        invLine.VatPercentage = line.VatPercentage;
                        invLine.VatAmount = line.VatAmount;
                        invLine.TotalPrice = line.TotalPrice;
                        ApplyAuthoritativeSourcePricing(invLine, line,
                            sourcePriceIncludesVat);
                        invLine.Volume = line.Volume;
                        invLine.VolumeNormalized = line.VolumeNormalized;
                        invLine.TankFillingId = line.TankFillingId;
                        bool linePriceIncludesVat = sourcePriceIncludesVat ||
                            line.SalesTransaction != null;
                        this.ApplyVatExemptionToTransformedLine(invLine,
                            linePriceIncludesVat ? SourceRetailUnitPrice(line) : SourceNetUnitPrice(line),
                            line.VatPercentage, linePriceIncludesVat);
                        invLine.PropertyChanged += Ilvm_PropertyChanged;

                        invLine.SetSourceNozzleContext(line);

                        this.lines.Add(invLine);
                        lineRelations.Add(line.InvoiceLineId, invLine.InvoiceLineId);
                    }
                }
                this.RecalculateSums();
                this.ApplicationUserId = Data.DatabaseModel.CurrentUserId;
                //this.AllowEdit = false;

                var fit = db.InvoiceTypes.SingleOrDefault(i => i.InvoiceTypeId == fromType);
                if (fit != null)
                {
                    if (this.SelectedInvoiceType == null)
                        return;
                    if (!this.SelectedInvoiceType.IsCancelation.HasValue || !this.SelectedInvoiceType.IsCancelation.Value)
                        return;
                    this.simplePrintTransform = !(fit.IsLaserPrint.HasValue ? fit.IsLaserPrint.Value : this.InvoiceLines.Length != 1);
                }
                
            }
        }

        //public void CreateInvoice(Guid invoiceId, Guid invoiceType, Guid? traderId, Guid? vehicleId)
        //{
        //    using (Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
        //    {
        //        var invoice = db.Invoices.Where(i => i.InvoiceId == invoiceId).SingleOrDefault();
        //        if (invoice == null)
        //            return;

        //        var invType = db.InvoiceTypes.SingleOrDefault(i => i.InvoiceTypeId == invoiceType);
        //        if (invType == null)
        //            return;

        //        Guid fromType = invoice.InvoiceTypeId;
        //        var transformationType = db.InvoiceTypeTransforms.Where(i => i.ParentInvoiceTypeId == fromType && i.ChildInvoiceTypeId == invoiceType).FirstOrDefault();
        //        if (transformationType == null)
        //            return;
        //        this.EntityState = EntityStateEnum.Added;
        //        this.InvoiceId = Guid.NewGuid();
        //        this.TraderId = traderId;
        //        this.VehicleId = vehicleId;
        //        this.InvoiceTypeId = invoiceType;
        //        this.TransactionDate = DateTime.Now;
        //        this.NettoAmount = 0;
        //        this.VatAmount = 0;
        //        this.DiscountAmount = 0;
        //        this.TotalAmount = 0;
        //        this.IsEditLocked = true;
        //        this.lines.Clear();
        //        var lines = db.InvoiceLines.Where(i => i.InvoiceId == invoiceId).ToArray();
                    
        //        var desc = invoice.Description;
        //        this.Notes = transformationType.NotesAddition + ": " + string.Join(", ", desc);
        //        if (this.VatExemptionReason != null && this.VatExemptionReason != "")
        //            this.Notes = this.Notes + "\r\n" + this.VatExemptionReason;
        //        Dictionary<Guid, Guid> lineRelations = new Dictionary<Guid, Guid>();
        //        if (!transformationType.CreationType.HasValue || transformationType.CreationType.Value == 0)
        //        {
        //            if (this.SimplePrint || this.DispenserType)
        //            {
        //                this.lines.Clear();
        //            }
        //            foreach (var line in lines)
        //            {
        //                InvoiceLineViewModel invLine = new InvoiceLineViewModel();
        //                invLine.ParentInvoice = this;
        //                invLine.EntityState = EntityStateEnum.Added;
        //                invLine.DiscountAmount = line.DiscountAmount;
        //                invLine.FuelDensity = line.FuelDensity;
        //                invLine.FuelTypeId = line.FuelTypeId;
        //                invLine.InvoiceId = this.InvoiceId;
        //                invLine.InvoiceLineId = Guid.NewGuid();
        //                invLine.TankId = line.TankId;
        //                invLine.Temperature = line.Temperature;
        //                invLine.VatPercentage = line.VatPercentage;
        //                invLine.VatAmount = line.VatAmount;
        //                invLine.TotalPrice = line.TotalPrice;
        //                invLine.UnitPrice = line.UnitPrice;
        //                invLine.Volume = line.Volume;
        //                invLine.VolumeNormalized = line.VolumeNormalized;
        //                invLine.TankFillingId = line.TankFillingId;
        //                invLine.PropertyChanged += Ilvm_PropertyChanged;

        //                if (transformationType.TransformationMode == (int)Common.Enumerators.InvoiceTransformationTypeEnum.Cancelation)
        //                {
        //                    if (line.SalesTransaction != null)
        //                    {
        //                        invLine.CurrentDispenserId = line.SalesTransaction.Nozzle.DispenserId;
        //                        invLine.CurrentNozzleId = line.SalesTransaction.NozzleId;
        //                    }
        //                }

        //                this.lines.Add(invLine);
        //                lineRelations.Add(line.InvoiceLineId, invLine.InvoiceLineId);
        //            }
        //        }
        //        this.RecalculateSums();
        //        this.ApplicationUserId = Data.DatabaseModel.CurrentUserId;

        //        var fit = db.InvoiceTypes.SingleOrDefault(i => i.InvoiceTypeId == fromType);
        //        if (fit != null)
        //        {
        //            if (this.SelectedInvoiceType == null)
        //                return;
        //            if (!this.SelectedInvoiceType.IsCancelation.HasValue || !this.SelectedInvoiceType.IsCancelation.Value)
        //                return;
        //            this.simplePrintTransform = !(fit.IsLaserPrint.HasValue ? fit.IsLaserPrint.Value : false);
        //        }
        //    }
        //}

        public override void Load(DatabaseModel db, Guid id)
        {
            var persistedInvoice = db.Invoices.SingleOrDefault(i => i.InvoiceId == id);
            // Make the persisted type available before BaseViewModel raises the
            // InvoiceTypeId notification. Otherwise a bound Telerik dropdown whose
            // active list omits a hidden/invalidated historical type selects its
            // first item and writes that unrelated value back into this view model.
            var persistedTypeId = persistedInvoice == null
                ? Guid.Empty
                : persistedInvoice.InvoiceTypeId;
            var persistedPaymentType = persistedInvoice == null
                ? (int?)null
                : persistedInvoice.PaymentType;
            var persistedType = LoadPersistedInvoiceType(db,persistedTypeId);
            if (persistedType != null)
                this.InvoiceTypes = ReplaceInvoiceType(this.InvoiceTypes,persistedType);
            else if (persistedTypeId != Guid.Empty &&
                !this.InvoiceTypes.Any(i => i.InvoiceTypeId == persistedTypeId))
            {
                this.InvoiceTypes = CommonCache.Instance.InvoiceTypes.ToArray();
                this.OnPropertyChanged("InvoiceTypes");
            }
            base.Load(db, id);
            
            if(!this.EditEnabled)
            {
                this.InvoiceTypes = CommonCache.Instance.InvoiceTypes.ToArray();
                this.SuspendRecalculation = true;
            }
            if (persistedType != null)
                this.InvoiceTypes = ReplaceInvoiceType(this.InvoiceTypes,persistedType);

            // Replacing InvoiceTypes does not notify the nested BindingSource.
            // Publish the final list, then reassert the value read from the
            // invoice so Telerik cannot leave its first filtered item selected.
            if (persistedTypeId != Guid.Empty)
            {
                if (!this.InvoiceTypes.Any(i => i.InvoiceTypeId == persistedTypeId))
                    this.InvoiceTypes = CommonCache.Instance.InvoiceTypes.ToArray();
                this.OnPropertyChanged("InvoiceTypes", true);
                if (this.InvoiceTypeId != persistedTypeId)
                    this.InvoiceTypeId = persistedTypeId;
                this.OnPropertyChanged("InvoiceTypeId", true);
                this.EntityState = EntityStateEnum.Unchanged;
                this.HasChanges = false;
            }

            var inv = persistedInvoice;
            if (inv == null)
                return;
            ConfigureLoadedTransformationPriceEditPolicy(db, inv);
            if (inv.Trader != null)
            {
                this.TraderName = inv.Trader.Name;
                if (inv.Trader.VatExemption.HasValue && inv.Trader.VatExemption.Value)
                {
                    this.VatExemption = true;
                    this.VatExemptionReason = inv.Trader.VatExemptionReason;
                }
                if (inv.VehicleId.HasValue)
                {
                    var veh = db.Vehicles.SingleOrDefault(v => v.VehicleId == inv.VehicleId.Value);
                    if (veh != null)
                        this.PlateNumber = veh.PlateNumber;
                }
            }
            this.CurrentLine = this.InvoiceLines.FirstOrDefault();
            // Trader/vehicle notifications and Telerik lookup initialization may
            // temporarily select their defaults while Base.Load is running. Apply
            // the saved invoice value last so it remains authoritative.
            if (this.PaymentType != persistedPaymentType)
                this.PaymentType = persistedPaymentType;
            this.OnPropertyChanged("PaymentType", true);
            this.EntityState = EntityStateEnum.Unchanged;
            this.HasChanges = false;
        }

        private void ConfigureLoadedTransformationPriceEditPolicy(DatabaseModel db,
            Data.Invoice invoice)
        {
            var parentRelations = db.InvoiceRelations
                .Where(relation => relation.ChildInvoiceId == invoice.InvoiceId)
                .ToArray();
            if (parentRelations.Length == 0)
            {
                this.IsEditLocked = false;
                ConfigureTransformationPriceEditPolicy(false, true, false, null);
                return;
            }

            var parentIds = parentRelations.Select(relation => relation.ParentInvoiceId)
                .Distinct().ToArray();
            var parents = db.Invoices.Where(parent => parentIds.Contains(parent.InvoiceId))
                .ToArray();
            bool sourceHasFinancialData = parents.Any(parent =>
                parent.InvoiceType == null || parent.InvoiceType.ShowFinancialData == true);

            var guideTransforms = db.InvoiceTypeTransforms.Where(transform =>
                transform.ChildInvoiceTypeId == invoice.InvoiceTypeId &&
                transform.CreationInvoiceTypeId.HasValue).ToArray();
            bool guideOnlyTarget = parentRelations.Any(relation =>
                relation.RelationType == (int)Common.Enumerators.InvoiceRelationTypeEnum.LocalCopy &&
                parents.Any(parent => parent.InvoiceId == relation.ParentInvoiceId &&
                    guideTransforms.Any(transform =>
                        transform.ParentInvoiceTypeId == parent.InvoiceTypeId)));

            this.IsEditLocked = true;
            ConfigureTransformationPriceEditPolicy(true, sourceHasFinancialData,
                guideOnlyTarget, ResolveCounterpartVat(db, invoice.TraderId, invoice.VehicleId));
        }

        private static string ResolveCounterpartVat(DatabaseModel db, Guid? traderId,
            Guid? vehicleId)
        {
            Guid? resolvedTraderId = traderId;
            if ((!resolvedTraderId.HasValue || resolvedTraderId.Value == Guid.Empty) &&
                vehicleId.HasValue && vehicleId.Value != Guid.Empty)
            {
                resolvedTraderId = db.Vehicles.Where(vehicle =>
                    vehicle.VehicleId == vehicleId.Value)
                    .Select(vehicle => (Guid?)vehicle.TraderId).FirstOrDefault();
            }
            if (!resolvedTraderId.HasValue || resolvedTraderId.Value == Guid.Empty)
                return null;
            return db.Traders.Where(trader => trader.TraderId == resolvedTraderId.Value)
                .Select(trader => trader.TaxRegistrationNumber).FirstOrDefault();
        }

        private static InvoiceTypeViewModel LoadPersistedInvoiceType(DatabaseModel db,Guid invoiceTypeId)
        {
            if (invoiceTypeId == Guid.Empty) return null;
            var entity = db.InvoiceTypes.SingleOrDefault(i => i.InvoiceTypeId == invoiceTypeId);
            if (entity == null) return null;
            var result = new InvoiceTypeViewModel();
            result.Load(db,entity);
            return result;
        }

        private static InvoiceTypeViewModel[] ReplaceInvoiceType(InvoiceTypeViewModel[] source,InvoiceTypeViewModel persisted)
        {
            return (source ?? new InvoiceTypeViewModel[0])
                .Where(item => item.InvoiceTypeId != persisted.InvoiceTypeId)
                .Concat(new[] { persisted })
                .ToArray();
        }

        public override bool Save(Guid id)
        {
            try
            {
                using (Threads.SharedInvoiceRuntime.EditUnissued(id)) return base.Save(id);
            }
            catch (Exception ex) { this.AddError("Σφάλμα παραστατικού",ex.Message); return false; }
        }

        public override bool Save(DatabaseModel db, Guid id)
        {
            if (Threads.SharedInvoiceRuntime.Owns(id))
            {
                this.AddError("Σφάλμα παραστατικού","Το παραστατικό έχει κοινό ιστορικό έκδοσης και δεν επιτρέπεται αλλαγή ή διαγραφή.");
                return false;
            }
            bool dispenserInvoice = this.SelectedInvoiceType != null &&
                this.SelectedInvoiceType.DispenserType == true;
            bool requiresPumpSalePersistence = dispenserInvoice && this.EditEnabled &&
                !this.IsEditLocked && (this.EntityState == EntityStateEnum.Added ||
                    this.CurrentLine != null && this.CurrentLine.SaleTransactionId.HasValue);
            if (requiresPumpSalePersistence)
            {
                this.Errors = new ValidationError[] { };
                if (!this.CreateNewSale(false))
                    return false;
                this.Load(this.InvoiceId);
                if (this.RefreshView != null)
                    this.RefreshView(this, new EventArgs());
                return true;
            }
            else
            {
                this.Errors = new ValidationError[] { };
                if (!this.PaymentType.HasValue || !Enum.IsDefined(typeof(Common.Enumerators.PaymentTypeEnum), this.PaymentType.Value))
                    this.AddError("Σφάλμα παραστατικού", "Δεν έχει επιλεγεί έγκυρος τρόπος πληρωμής.");
                if (this.InvoiceLines.Length == 0)
                    this.AddError("Σφάλμα παραστατικού", "Δεν έχετε εισάγει γραμμές στο παραστατικό");
                else if (dispenserInvoice && this.InvoiceLines.Any(line => line.Volume <= 0))
                    this.AddError("Σφάλμα παραστατικού",
                        "Ο όγκος της γραμμής πρέπει να είναι μεγαλύτερος από μηδέν.");

                if (this.InvoiceTypeId == Guid.Empty || this.SelectedInvoiceType == null)
                    this.AddError("Σφάλμα παραστατικού", "Δεν έχετε επιλέξει τύπο παραστατικού");

                foreach (var line in this.InvoiceLines)
                {
                    if (line.FuelTypeId == Guid.Empty ||
                        !this.FuelTypes.Any(item => item.FuelTypeId == line.FuelTypeId))
                        this.AddError("Σφάλμα παραστατικού",
                            "Το καύσιμο γραμμής δεν ανήκει στη διαθέσιμη λίστα.");
                    if (line.TankId.HasValue && line.TankId.Value != Guid.Empty)
                    {
                        var tank = this.Tanks.FirstOrDefault(
                            item => item.TankId == line.TankId.Value);
                        if (tank == null)
                            this.AddError("Σφάλμα παραστατικού",
                                "Η δεξαμενή γραμμής δεν ανήκει στη διαθέσιμη λίστα.");
                        else if (tank.FuelTypeId != line.FuelTypeId)
                            this.AddError("Σφάλμα παραστατικού",
                                "Η δεξαμενή γραμμής δεν αντιστοιχεί στο επιλεγμένο καύσιμο.");
                    }
                }

                if (this.Errors.Length > 0)
                    return false;

                foreach (var selection in this.oneToManyLineSelections)
                {
                    var sourceLine = db.InvoiceLines.SingleOrDefault(line => line.InvoiceLineId == selection.Key);
                    var selectedLine = this.InvoiceLines.SingleOrDefault(line =>
                        line.InvoiceLineId == selection.Value);
                    decimal selectedVolume = selectedLine == null ? 0 : selectedLine.Volume;
                    decimal remainingVolume = sourceLine == null
                        ? 0
                        : sourceLine.Volume - GetConsumedTransformationVolume(sourceLine);
                    if (selectedVolume <= 0 || selectedVolume > remainingVolume)
                    {
                        this.AddError("Σφάλμα μετασχηματισμού",
                            "Ο διαθέσιμος όγκος του αρχικού παραστατικού έχει αλλάξει. Ανοίξτε ξανά τον μετασχηματισμό.");
                        break;
                    }
                }
                if (this.Errors.Length > 0)
                    return false;

                this.RecalculateSums();

                if (this.SelectedInvoiceType.DeliveryType.HasValue && this.SelectedInvoiceType.DeliveryType == (int)Common.Enumerators.DeliveryTypeEnum.Delivery)
                {
                    foreach (var line in this.InvoiceLines)
                    {
                        var ft = this.FuelTypes.SingleOrDefault(f => f.FuelTypeId == line.FuelTypeId);
                        if (ft == null)
                        {
                            this.AddError("Σφάλμα παραστατικού", "Δεν έχετε επιλέξει τύπο καυσίμου σε κάποια γραμμή");
                            break;
                        }
                        if (line.FuelDensity >= ft.BaseDensity + 150 || line.FuelDensity <= ft.BaseDensity - 150)
                        {
                            this.AddError("Σφάλμα παραστατικού", "Ελέξτε τις πυκνότητες καυσίμων!");
                            break;
                        }
                        else if (line.FuelDensity >= 900 || line.FuelDensity <= 500)
                        {
                            this.AddError("Σφάλμα παραστατικού", "Ελέξτε τις πυκνότητες καυσίμων!");
                            break;
                        }
                    }
                }                
                if (this.SelectedInvoiceType.ForcesDelivery.HasValue && this.SelectedInvoiceType.ForcesDelivery.Value)
                {
                    int c = this.lines.Count(i => !i.TankId.HasValue || i.TankId.Value == Guid.Empty);
                    if (c > 0)
                        this.AddError("Σφάλμα παραστατικού", "Δεν έχετε επιλέξει δεξαμενή σε κάποια γραμμή");
                }
                if (this.SelectedInvoiceType.NeedsVehicle.HasValue && this.SelectedInvoiceType.NeedsVehicle.Value)
                {
                    if (!this.VehicleId.HasValue || this.VehicleId.Value == Guid.Empty)
                        this.AddError("Σφάλμα παραστατικού", "Δεν έχετε επιλέξει όχημα");
                }
                if (this.Errors.Length > 0)
                    return false;
                base.Save(db, id);
                foreach (var rel in this.invRelations)
                {
                    rel.Save(db, rel.InvoiceRelationId);
                }
                var childRelations = this.invRelations.SelectMany(i => i.LineRelations).Distinct().ToArray();
                foreach (var rel in childRelations)
                {
                    rel.Save(db, rel.InvoiceLineRelationId);
                }
                //var dbInv = db.Invoices.SingleOrDefault(i => i.InvoiceId == id);
                //dbInv.CreateFinTransactions(db);
                //db.SaveChanges();
                return true;
            }
        }

        private void Ilvm_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            InvoiceLineViewModel ilv = sender as InvoiceLineViewModel;
            if (ilv.HasChanges)
                this.HasChanges = true;
            if (e.PropertyName == "FuelTypeId")
            {
                if (ilv.TankId.HasValue)
                {
                    var selectedTank = (this.Tanks ?? new TankViewModel[0])
                        .SingleOrDefault(tank => tank.TankId == ilv.TankId.Value);
                    if (selectedTank == null || selectedTank.FuelTypeId != ilv.FuelTypeId)
                        ilv.TankId = null;
                }
                this.OnPropertyChanged("SupportsSupplyNumber");
            }
        }

        private void SetInvoiceSums(DatabaseModel db, Guid[] invoiceIds, int mode)
        {
            var invoices = db.Invoices.Where(i => invoiceIds.Contains(i.InvoiceId)).ToArray().
                    OrderBy(i => i.TransactionDate).ThenBy(i => i.InvoiceId).ToArray();
            this.NettoAmount = 0;
            this.NettoAfterDiscount = 0;
            this.VatAmount = 0;
            this.TotalAmount = 0;
            this.DiscountAmount = 0;
            this.DiscountAmountWhole = 0;
            this.DiscountAmountRetail = 0;
            foreach (var inv in invoices)
            {
                this.NettoAmount = this.NettoAmount + decimal.Round(inv.NettoAmount.HasValue ? inv.NettoAmount.Value : 0, 2);
                this.NettoAfterDiscount = this.NettoAfterDiscount + decimal.Round(inv.NettoAfterDiscount, 2);
                this.VatAmount = this.VatAmount + decimal.Round(inv.VatAmount.HasValue ? inv.VatAmount.Value : 0, 2);
                this.TotalAmount = this.TotalAmount + decimal.Round(inv.TotalAmount.HasValue ? inv.TotalAmount.Value : 0, 2);
                this.DiscountAmount = this.DiscountAmount + decimal.Round(inv.DiscountAmount, 2);
                this.DiscountAmountWhole = this.DiscountAmountWhole + decimal.Round(inv.DiscountAmountWhole, 2);
                this.DiscountAmountRetail = this.DiscountAmountRetail + decimal.Round(inv.DiscountAmountRetail, 2);
            }
            if(mode != (int)Common.Enumerators.InvoiceTransformationTypeEnum.OneToMany)
                this.SuspendRecalculation = true;
        }

        protected override void LoadArrayProperty(Data.DatabaseModel db, Data.Invoice entity, string propName)
        {
            if (propName == "InvoiceLines")
            {
                bool preserveTransformation = entity.ParentInvoiceRelations != null &&
                    entity.ParentInvoiceRelations.Count > 0;
                if (preserveTransformation)
                    this.SuspendRecalculation = true;
                this.lines.Clear();
                foreach (var line in entity.InvoiceLines)
                {
                    InvoiceLineViewModel ilvm = new InvoiceLineViewModel();
                    ilvm.Load(db, line.InvoiceLineId);
                    ilvm.InitDiscount(this);
                    ilvm.IsTransformation = preserveTransformation;
                    ilvm.PreserveFiscalAmounts = preserveTransformation;
                    ilvm.PropertyChanged += Ilvm_PropertyChanged;
                    ilvm.ParentInvoice = this;
                    this.lines.Add(ilvm);
                }
                if (!preserveTransformation)
                    this.RecalculateSums();
            }
        }

        protected override void SaveArrayProperty(Data.DatabaseModel db, Data.Invoice entity, string propName)
        {
            if (propName == "InvoiceLines")
            {
                foreach (var line in this.lines)
                {
                    if (this.EntityState == EntityStateEnum.Deleted)
                        line.EntityState = EntityStateEnum.Deleted;
                    line.Save(db, line.InvoiceLineId);
                }
            }
            //else if(propName == "ChildInvoiceRelations")
            //{
            //    foreach (var rel in this.ChildInvoiceRelations)
            //    {
            //        if (this.EntityState == EntityStateEnum.Deleted)
            //            rel.EntityState = EntityStateEnum.Deleted;
            //        rel.Save(db, rel.InvoiceRelationId);
            //    }
            //}
            //else if (propName == "ParentInvoiceRelations")
            //{
            //    foreach (var rel in this.ParentInvoiceRelations)
            //    {
            //        if (this.EntityState == EntityStateEnum.Deleted)
            //            rel.EntityState = EntityStateEnum.Deleted;
            //        rel.Save(db, rel.InvoiceRelationId);
            //    }
            //}
        }

        protected override void OnPropertyChanged(string propertyName)
        {
            base.OnPropertyChanged(propertyName);
            if (propertyName == "TraderId")
            {
                DatabaseModel db = new DatabaseModel(Properties.Settings.Default.DBConnection);
                var trader = db.Traders.SingleOrDefault(i => i.TraderId == this.TraderId);
                this.transformationCounterpartVat = trader == null
                    ? null
                    : trader.TaxRegistrationNumber;
                NotifyLineEditPolicyChanged();
                if (trader == null)
                {
                    db.Dispose();
                    this.TraderName = "";
                    this.PlateNumber = "";
                    this.OnPropertyChanged("TraderName", false);
                    return;
                }
                if (trader.PaymentType.HasValue)
                    this.PaymentType = trader.PaymentType;
                if (trader.VatExemption.HasValue)
                {
                    this.VatExemption = trader.VatExemption.Value;
                    this.VatExemptionReason = trader.VatExemptionReason;
                }
                else
                {
                    this.VatExemption = false;
                    this.VatExemptionReason = "";
                }
                this.PlateNumber = "";
                this.TraderName = trader.Name;
                this.OnPropertyChanged("TraderName", false);
                this.OnPropertyChanged("PlateNumber", false);
                this.SupplyNumber = trader.SupplyNumber;
                db.Dispose();
            }
            else if (propertyName == "VehicleId")
            {
                DatabaseModel db = new DatabaseModel(Properties.Settings.Default.DBConnection);
                var vehicle = db.Vehicles.SingleOrDefault(i => i.VehicleId == this.VehicleId);
                if (vehicle == null)
                {
                    db.Dispose();
                    this.PlateNumber = "";
                    this.VehiclePlateNumber = "";
                    this.OnPropertyChanged("PlateNumber", false);
                    return;
                }
                this.PlateNumber = vehicle.PlateNumber;
                this.VehiclePlateNumber = this.PlateNumber;
                if (this.TraderId != vehicle.TraderId)
                {
                    this.SetTrader(vehicle.TraderId);
                    this.transformationCounterpartVat = vehicle.Trader == null
                        ? null
                        : vehicle.Trader.TaxRegistrationNumber;
                    NotifyLineEditPolicyChanged();
                    if (vehicle.Trader.VatExemption.HasValue)
                    {
                        this.VatExemption = vehicle.Trader.VatExemption.Value;
                        this.VatExemptionReason = vehicle.Trader.VatExemptionReason;
                    }
                    else
                    {
                        this.VatExemption = false;
                        this.VatExemptionReason = "";
                    }
                }
                if (vehicle.Trader.PaymentType.HasValue)
                    this.PaymentType = vehicle.Trader.PaymentType;
                this.SupplyNumber = vehicle.Trader.SupplyNumber;
                this.TraderName = vehicle.Trader.Name;
                this.OnPropertyChanged("TraderName", false);
                this.OnPropertyChanged("PlateNumber", false);
                db.Dispose();
            }
            else if (propertyName == "InvoiceTypeId")
            {
                if (this.lines.Count == 0)
                {
                    this.AddInvoiceLine();
                    this.CurrentLine = this.lines.FirstOrDefault();
                    this.PaymentType = (int)Common.Enumerators.PaymentTypeEnum.Cash;

                }
                this.OnPropertyChanged("RetailPrices");
                this.OnPropertyChanged("WholeSalePrices");
                this.OnPropertyChanged("PricesIncludeVat");
                this.OnPropertyChanged("ReportPrint");
                this.OnPropertyChanged("SimplePrint");
                this.OnPropertyChanged("CanAdd");
            }
            if (propertyName == "IsPrinted" || propertyName == "AllowEdit")
                NotifyLineEditPolicyChanged();
        }
    }

    public partial class InvoiceViewModel
    {
        public bool CanDelete
        {
            get
            {
                var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
                try
                {
                    if (Threads.SharedInvoiceRuntime.Owns(this.invoiceid)) return false;
                    if (db.InvoiceLines.Where(i => i.InvoiceId == this.invoiceid && (i.TankFillingId.HasValue || i.SaleTransactionId.HasValue)).Count() > 0)
                        return false;

                    return true;
                }
                catch
                {
                    return false;
                }
                finally
                {
                    db.Dispose();
                }
            }
        }

        public bool IsCanceled { set; get; }

        public bool IsReplaced { set; get; }

        public InvoiceViewModel()
        {
        }

        public InvoiceViewModel(DatabaseModel db, Invoice entity) : base(db, entity)
        {
        }

        protected void SetTrader(Guid traderId)
        {
            this.traderid = traderId;
        }
    }
}
