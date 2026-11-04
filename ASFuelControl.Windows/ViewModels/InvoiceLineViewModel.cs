using ASFuelControl.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASFuelControl.Invoicing.Mapping;

namespace ASFuelControl.Windows.ViewModels
{
    public partial class InvoiceLineViewModel
    {
        InvoiceDetailsViewModel parentInvoice = null;
        private bool salesValueChanging = false;
        private Guid currentNozzleId { set; get; }
        private Guid currentDispenserId { set; get; }
        private Guid sourceNozzleId;
        private Guid sourceDispenserId;
        private bool sourceSalePointLocked;

        public SalesTransactionViewModel SalesTransactionView { set; get; }

        public bool EditEnabled
        {
            get
            {
                if (this.ParentInvoice == null)
                    return false;
                if (!this.ParentInvoice.IsPrinted.HasValue)
                    return true;
                return !this.ParentInvoice.IsPrinted.Value;
            }
        }

        public Guid CurrentNozzleId
        {
            set
            {
                var selectedValue = this.sourceSalePointLocked
                    ? this.sourceNozzleId
                    : value;
                if (this.currentNozzleId == selectedValue)
                {
                    // A lookup can temporarily select its first row while Telerik
                    // rebuilds the binding. Notify it to read the physical sale's
                    // value back instead of accepting that presentation value.
                    if (value != selectedValue)
                        base.OnPropertyChanged("CurrentNozzleId", true);
                    return;
                }
                this.currentNozzleId = selectedValue;
                if (this.EditEnabled && !this.sourceSalePointLocked)
                {
                    if (this.ParentInvoice.SelectedInvoiceType != null &&
                        this.ParentInvoice.SelectedInvoiceType.DispenserType == true)
                    {
                        var nozzle = this.ParentInvoice.Dispensers.SelectMany(d => d.Nozzles).SingleOrDefault(n => n.NozzleId == this.currentNozzleId);
                        if (nozzle != null)
                        {
                            // A dispenser sale is identified by its nozzle.  The
                            // nozzle is therefore the source of truth for both
                            // fuel and price; the form must not allow an
                            // independent fuel selection.
                            this.FuelTypeId = nozzle.FuelTypeId;
                            // A nozzle price is always the configured retail
                            // price.  The invoice decides whether that price is
                            // shown gross, shown net, or reduced because the
                            // selected customer is VAT exempt.
                            this.UnitPriceRetail = this.ParentInvoice.GetUnitPriceFromRetailPrice(
                                this, nozzle.UnitPrice);
                        }
                    }
                }
                this.OnPropertyChanged("CurrentNozzleId");
            }
            get { return this.currentNozzleId; }
        }

        public Guid CurrentDispenserId
        {
            set
            {
                var selectedValue = this.sourceSalePointLocked
                    ? this.sourceDispenserId
                    : value;
                if (this.currentDispenserId == selectedValue)
                {
                    if (value != selectedValue)
                        base.OnPropertyChanged("CurrentDispenserId", true);
                    return;
                }
                this.currentDispenserId = selectedValue;
                if (this.EditEnabled && !this.sourceSalePointLocked)
                {
                    if (this.ParentInvoice.SelectedInvoiceType != null &&
                        this.ParentInvoice.SelectedInvoiceType.DispenserType == true)
                    {
                        var dispenser = this.ParentInvoice.Dispensers.SingleOrDefault(d => d.DispenserId == this.currentDispenserId);
                        if (dispenser != null && dispenser.Nozzles != null && dispenser.Nozzles.Length > 0)
                            this.CurrentNozzleId = dispenser.Nozzles.First().NozzleId;
                    }
                }
                this.OnPropertyChanged("CurrentDispenserId");
            }
            get { return this.currentDispenserId; }
        }

        public bool CanSelectSalePoint
        {
            get { return !this.sourceSalePointLocked && this.EditEnabled; }
        }

        /// <summary>
        /// Carries the physical pump/nozzle identity into a transformed line
        /// without transferring ownership of the original SalesTransaction and
        /// without running the ordinary editable-line price/fuel setters.
        /// </summary>
        internal bool SetSourceNozzleContext(InvoiceLine sourceLine)
        {
            var sale = FindSourceSale(sourceLine, new HashSet<Guid>());
            if (sale == null || sale.Nozzle == null)
                return false;

            this.sourceSalePointLocked = true;
            this.sourceDispenserId = sale.Nozzle.DispenserId;
            this.sourceNozzleId = sale.NozzleId;
            bool dispenserChanged = this.currentDispenserId != sale.Nozzle.DispenserId;
            bool nozzleChanged = this.currentNozzleId != sale.NozzleId;
            this.currentDispenserId = sale.Nozzle.DispenserId;
            this.currentNozzleId = sale.NozzleId;
            if (dispenserChanged)
                base.OnPropertyChanged("CurrentDispenserId", true);
            if (nozzleChanged)
                base.OnPropertyChanged("CurrentNozzleId", true);
            base.OnPropertyChanged("CanSelectSalePoint", true);
            return true;
        }

        private static SalesTransaction FindSourceSale(InvoiceLine line, HashSet<Guid> visited)
        {
            if (line == null || !visited.Add(line.InvoiceLineId))
                return null;
            if (line.SalesTransaction != null && line.SalesTransaction.Nozzle != null)
                return line.SalesTransaction;

            // A saved transformation intentionally has no SaleTransactionId of
            // its own. Follow its line provenance so reopening it still shows
            // the pump/nozzle of the physical source sale.
            foreach (var link in line.InvoiceLineRelations_ChildRelationId)
            {
                var sale = FindSourceSale(link.InvoiceLine_ParentLineId, visited);
                if (sale != null)
                    return sale;
            }
            return null;
        }

        public bool CanDelete
        {
            get
            {
                var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
                try
                {
                    if (this.SaleTransactionId.HasValue)
                        return false;
                    if(this.TankFillingId.HasValue)
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

        public bool CanEdit
        {
            get
            {
                try
                {
                    if (this.ParentInvoice != null && this.ParentInvoice.AllowEdit.HasValue && !this.ParentInvoice.AllowEdit.Value)
                        return false;
                    if (this.SaleTransactionId.HasValue)
                        return false;
                    if (this.TankFillingId.HasValue)
                        return false;
                    if (this.ParentInvoice != null && this.ParentInvoice.IsEditLocked)
                        return false;
                    return true;
                }
                catch
                {
                    return false;
                }
                finally
                {
                }
            }
        }

        public bool CanEditTransformationFinancials
        {
            get
            {
                return this.IsTransformation && this.ParentInvoice != null &&
                    this.ParentInvoice.TransformationUnitPriceEditEnabled;
            }
        }

        public bool CanEditTransformationVolume
        {
            get
            {
                return this.IsTransformation && this.ParentInvoice != null &&
                    this.ParentInvoice.TransformationVolumeEditEnabled;
            }
        }

        public bool IsTransformation { set; get; }

        // A saved transformation already contains the fiscal values selected or
        // copied when it was created. Reopening it must not rebuild those values
        // from rounded quantity/unit-price columns.
        internal bool PreserveFiscalAmounts { set; get; }

        // UnitPriceRetail and UnitPriceWhole are coupled editor properties: setting
        // either one recalculates the other. A transformation already knows which
        // side is authoritative, so assign the prepared five-decimal pair together
        // and never round-trip the authoritative value through VAT.
        internal void SetTransformationUnitPrices(decimal retailPrice,
            decimal netPrice)
        {
            this.unitPriceRetail = retailPrice;
            this.unitPriceWhole = netPrice;
            // Existing totals, Samtec and report code use UnitPrice as gross.
            this.unitprice = retailPrice;
            base.OnPropertyChanged("UnitPriceView");
        }

        private decimal volumeCalc = 0;
        public decimal VolumeCalc
        {
            set
            {
                if (this.volumeCalc == value)
                    return;
                this.volumeCalc = value;
                this.OnPropertyChanged("VolumeCalc");
            }
            get { return this.volumeCalc; }
        }

        private decimal nettoPrice = 0;
        public decimal NettoPrice
        {
            get
            {
                return this.TotalPrice - this.VatAmount;
            }
        }

        private decimal unitPriceView = 0;
        public decimal UnitPriceView
        {
            set
            {
                if (this.CanEditTransformationFinancials)
                {
                    ApplyEditableTransformationUnitPrice(value);
                    return;
                }
                if (this.parentInvoice == null)
                {
                    if (this.UnitPriceRetail == value)
                        return;
                    this.UnitPriceRetail = value;
                    this.unitPriceWhole = this.UnitPriceRetail / ((100 + this.VatPercentageView) / 100);
                    this.unitprice = value;
                    this.OnPropertyChanged("UnitPriceView");
                    return;
                }
                if (this.ParentInvoice.PricesIncludeVat)
                {
                    if (this.UnitPriceRetail == value)
                        return;
                    this.UnitPriceRetail = value;
                    this.unitprice = value;
                    this.unitPriceWhole = this.UnitPriceRetail / ((100 + this.VatPercentageView) / 100);
                }
                else
                {
                    if (this.UnitPriceWhole == value)
                        return;
                    this.UnitPriceWhole = value;
                    this.unitprice = this.UnitPriceRetail;
                }
                this.OnPropertyChanged("UnitPriceView");
            }
            get
            {
                if (this.parentInvoice == null)
                    return this.UnitPriceRetail;
                if (this.ParentInvoice.PricesIncludeVat)
                    return this.UnitPriceRetail;
                else
                    return this.UnitPriceWhole;
            }
        }

        private void ApplyEditableTransformationUnitPrice(decimal enteredPrice)
        {
            if (!this.CanEditTransformationFinancials)
                return;
            var prices = this.ParentInvoice.PricesIncludeVat
                ? TransformationUnitPrice.FromRetail(enteredPrice, this.VatPercentage)
                : TransformationUnitPrice.FromNet(enteredPrice, this.VatPercentage);
            if (this.UnitPriceRetail == prices.Retail &&
                this.UnitPriceWhole == prices.Net)
                return;

            EnableTransformationRecalculation();
            this.SetTransformationUnitPrices(prices.Retail, prices.Net);
            RecalculateEditedTransformationLine(false);
        }

        private void EnableTransformationRecalculation()
        {
            // Loading a saved transformation preserves its exact fiscal amounts.
            // A policy-authorized price or discount edit explicitly changes those
            // amounts, while quantity and source identity remain locked.
            this.PreserveFiscalAmounts = false;
        }

        private void RecalculateEditedTransformationLine(bool discountPercentageSet)
        {
            bool suspended = this.ParentInvoice.SuspendRecalculation;
            this.ParentInvoice.SuspendRecalculation = false;
            this.SetComputedColumns(discountPercentageSet, !discountPercentageSet);
            this.ParentInvoice.SuspendRecalculation = suspended;
        }
        //public decimal UnitPriceWhole
        //{
        //    set
        //    {
        //        if (this.parentInvoice == null)
        //        {
        //            if (this.UnitPriceRetail == value)
        //                return;
        //            this.UnitPriceRetail = value;
        //            this.unitPriceWhole = this.UnitPriceRetail / ((100 + this.VatPercentageView) / 100);
        //            this.unitprice = value;
        //            this.OnPropertyChanged("UnitPriceView");
        //            return;
        //        }
        //        if (this.ParentInvoice.RetailPrices || this.ParentInvoice.DispenserType)
        //        {
        //            if (this.UnitPriceRetail == value)
        //                return;
        //            this.UnitPriceRetail = value;
        //            this.unitprice = value;
        //            this.unitPriceWhole = this.UnitPriceRetail / ((100 + this.VatPercentageView) / 100);
        //        }
        //        else
        //        {
        //            if (this.UnitPriceWhole == value)
        //                return;
        //            this.UnitPriceWhole = value;
        //            this.unitprice = value;
        //            this.unitPriceRetail = this.UnitPriceWhole * ((100 + this.VatPercentageView) / 100);
        //        }
        //        this.OnPropertyChanged("UnitPriceView");
        //    }
        //    get
        //    {
        //        if (this.parentInvoice == null)
        //            return this.UnitPriceRetail;
        //        if (this.ParentInvoice.RetailPrices || this.ParentInvoice.DispenserType)
        //            return this.UnitPriceRetail;
        //        else
        //            return this.UnitPriceWhole;
        //    }
        //}
        private decimal discountAmountView = 0;

        public decimal DiscountPercentageView
        {
            set
            {
                if (this.DiscountPercentage == value)
                    return;
                if (this.CanEditTransformationFinancials)
                    EnableTransformationRecalculation();
                this.DiscountPercentage = value;
            }
            get { return this.DiscountPercentage; }
        }

        public decimal DiscountAmountView
        {
            set
            {
                if (this.CanEditTransformationFinancials)
                {
                    decimal currentValue = this.ParentInvoice.PricesIncludeVat
                        ? this.DiscountAmountRetail
                        : this.DiscountAmountWhole;
                    if (currentValue == value)
                        return;
                    EnableTransformationRecalculation();
                }
                if (this.parentInvoice == null)
                {
                    if (this.DiscountAmountRetail == value)
                        return;
                    this.DiscountAmountRetail = value;
                    this.discountamountWhole = this.DiscountAmountRetail / ((100 + this.VatPercentageView) / 100);
                    this.discountamount = value;
                    this.OnPropertyChanged("DiscountAmountView");
                    return;
                }
                if (this.ParentInvoice.PricesIncludeVat)
                {
                    if (this.DiscountAmountRetail == value)
                        return;
                    this.DiscountAmountRetail = value;
                    this.discountamount = value;
                    this.discountamountWhole = this.DiscountAmountRetail / ((100 + this.VatPercentageView) / 100);
                }
                else
                {
                    if (this.DiscountAmountWhole == value)
                        return;
                    this.DiscountAmountWhole = value;
                    this.discountamount = value;
                    this.discountamountRetail = this.DiscountAmountWhole * ((100 + this.VatPercentageView) / 100);
                }
                this.OnPropertyChanged("DiscountAmountView");
            }
            get
            {
                if (this.parentInvoice == null)
                    return this.DiscountAmountRetail;
                if (this.ParentInvoice.PricesIncludeVat)
                    return this.DiscountAmountRetail;
                else
                    return this.DiscountAmountWhole;
            }
        }

        //private decimal discountAmountNetto = 0;
        //public decimal DiscountNettoAmount
        //{
        //    set
        //    {
        //        if (this.discountAmountNetto == value)
        //            return;
        //        this.discountAmountNetto = value;
        //        this.OnPropertyChanged("DiscountNettoAmount");
        //    }
        //    get { return this.discountAmountNetto; }
        //}

        public decimal VatPercentageView
        {
            get { return this.GetVatPercentage(); }
        }

        //public decimal VolumeCalc
        //{
        //    get
        //    {
        //        if (this.ParentInvoice == null)
        //            return this.Volume;
        //        if(this.ParentInvoice.SelectedInvoiceType == null)
        //            return this.Volume;
        //        if (this.parentInvoice.SelectedInvoiceType.RetailInvoice.HasValue && this.parentInvoice.SelectedInvoiceType.RetailInvoice.Value)
        //            return this.Volume;
        //        if (this.ParentInvoice.SelectedInvoiceType.TransactionType == 1)
        //            return this.VolumeNormalized;
        //        else
        //            return this.Volume;
        //    }
        //}

        //public decimal NettoPrice
        //{
        //    get { return this.TotalPrice - this.VatAmount; }
        //}

        //public decimal UnitPriceView
        //{
        //    get
        //    {
        //        if (this.ParentInvoice == null)
        //            return this.UnitPrice;
        //        if (this.ParentInvoice.RetailPrices || this.ParentInvoice.DispenserType)
        //            return this.UnitPrice;
        //        return this.UnitPriceNetto;
        //    }
        //    set
        //    {
        //        if (this.ParentInvoice == null)
        //            return;
        //        if (this.ParentInvoice.RetailPrices || this.ParentInvoice.DispenserType)
        //            this.UnitPrice = value;
        //        else
        //            this.UnitPriceNetto = value;
        //        this.OnPropertyChanged("UnitPriceView");
        //    }
        //}

        //public decimal UnitPriceNetto
        //{
        //    set
        //    {
        //        this.UnitPrice = value * ((100 + this.VatPercentage) / 100);
        //    }
        //    get { return this.UnitPrice / ((100 + this.VatPercentage) / 100); }
        //}

        //public decimal DiscountAmountView
        //{
        //    get
        //    {
        //        if (this.ParentInvoice == null)
        //            return this.DiscountAmount;
        //        if (this.ParentInvoice.RetailPrices || this.ParentInvoice.DispenserType)
        //        {
        //            if (this.ParentInvoice.VatExemption)
        //                return this.DiscountAmountNetto;
        //            return this.DiscountAmount;
        //        }
        //        return this.DiscountAmountNetto;
        //    }
        //    set
        //    {
        //        if (this.ParentInvoice == null)
        //            return;
        //        if (this.ParentInvoice.RetailPrices || this.ParentInvoice.DispenserType)
        //            this.DiscountAmount = value;
        //        else
        //            this.DiscountAmountNetto = value;
        //        this.OnPropertyChanged("DiscountAmountView");
        //    }
        //}

        //public decimal DiscountAmountNetto
        //{
        //    set
        //    {
        //        this.DiscountAmount = decimal.Round(value * ((100 + this.VatPercentage) / 100), 2);
        //    }
        //    get { return this.DiscountAmount / ((100 + this.VatPercentage) / 100); }
        //}

        //public decimal DiscountPercentage
        //{
        //    set
        //    {
        //        if (this.ParentInvoice == null)
        //            return;
        //        if (this.SalesTransactionView == null)
        //            return;
        //        if (salesValueChanging)
        //        {
        //            this.SalesTransactionView.UnitPrice = value;
        //            this.OnPropertyChanged("DiscountPercentage");
        //            return;
        //        }
        //        salesValueChanging = true;
        //        var price = this.UnitPrice * this.Volume;
        //        this.TotalPrice = decimal.Round(price * ((100 - value) / 100), 2);
        //        salesValueChanging = false;
        //        this.OnPropertyChanged("DiscountPercentage");
        //    }
        //    get
        //    {
        //        if (this.SalesTransactionView == null)
        //            return 0;
        //        return this.SalesTransactionView.DiscountPercentage.HasValue ? this.SalesTransactionView.DiscountPercentage.Value : 0;
        //    }
        //}

        public InvoiceDetailsViewModel ParentInvoice
        {
            set
            {
                this.parentInvoice = value;
                if (this.parentInvoice == null)
                    return;
                if(this.parentInvoice.EditEnabled)
                    this.SetComputedColumns();
                this.SetNozzleData();
                this.parentInvoice.PropertyChanged -= ParentInvoice_PropertyChanged;
                this.parentInvoice.PropertyChanged += ParentInvoice_PropertyChanged;
            }
            get
            {
                return this.parentInvoice;
            }
        }

        private decimal GetVatPercentage()
        {
            if (this.parentInvoice == null)
                return this.VatPercentage;
            if (parentInvoice.VatExemption)
                return 0;
            return this.VatPercentage;
        }

        private void ParentInvoice_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "TransformationUnitPriceEditEnabled" ||
                e.PropertyName == "TransformationVolumeEditEnabled" ||
                e.PropertyName == "LineEditEnabled" ||
                e.PropertyName == "IsPrinted" || e.PropertyName == "AllowEdit")
            {
                base.OnPropertyChanged("CanEditTransformationFinancials", true);
                base.OnPropertyChanged("CanEditTransformationVolume", true);
            }
            if(e.PropertyName == "InvoiceTypeId")
            {
                if (this.ParentInvoice.SelectedInvoiceType != null)
                {
                    if (this.ParentInvoice.SelectedInvoiceType.DispenserType.HasValue && this.ParentInvoice.SelectedInvoiceType.DispenserType.Value)
                    {
                        var nozzle = this.ParentInvoice.Dispensers.SelectMany(d => d.Nozzles).SingleOrDefault(n => n.NozzleId == this.currentNozzleId);
                        if (nozzle != null)
                            this.UnitPrice = nozzle.UnitPrice;
                    }
                }
                this.OnPropertyChanged("UnitPriceView");
                this.OnPropertyChanged("DiscountAmountView");
            }
        }

        public override void Load(DatabaseModel db, InvoiceLine entity)
        {
            base.Load(db, entity);
            this.SetSourceNozzleContext(entity);
            
            if (this.ParentInvoice == null)
                return;
            
            this.SetComputedColumns();
        }

        public void SetComputedColumns(bool discountPercentageSet = false, bool unitPriceSet = false)
        {
            if (this.ParentInvoice == null)
                return;

            if (this.ParentInvoice.SelectedInvoiceType.TransactionType == 1)
                this.VolumeCalc = this.VolumeNormalized;
            else
                this.VolumeCalc = this.Volume;

            if (this.PreserveFiscalAmounts)
                return;

            if (IsTransformation)
            {
                decimal preDiscountTotal = this.VolumeCalc * this.UnitPriceRetail;
                if (discountPercentageSet)
                {
                    decimal retailDiscount = decimal.Round(
                        preDiscountTotal * this.DiscountPercentage / 100m, 2);
                    decimal vatMultiplier = (100m + this.GetVatPercentage()) / 100m;
                    decimal netDiscount = this.GetVatPercentage() == 0
                        ? retailDiscount
                        : decimal.Round(retailDiscount / vatMultiplier, 2);
                    this.DiscountAmountRetail = retailDiscount;
                    this.DiscountAmountWhole = netDiscount;
                    this.DiscountAmount = this.ParentInvoice.PricesIncludeVat
                        ? retailDiscount
                        : netDiscount;
                }
                else
                {
                    decimal percentage = preDiscountTotal == 0
                        ? 0
                        : decimal.Round(100m * this.DiscountAmountRetail /
                            preDiscountTotal, 5);
                    if (this.DiscountPercentage != percentage)
                    {
                        bool wasSuspended = this.suspendChange;
                        this.suspendChange = true;
                        this.DiscountPercentage = percentage;
                        this.suspendChange = wasSuspended;
                    }
                }
                this.TotalPrice = decimal.Round(
                    preDiscountTotal - this.DiscountAmountRetail, 2);
                this.VatAmount = decimal.Round(this.TotalPrice - (this.TotalPrice / ((100 + this.GetVatPercentage()) / 100)), 2);
                if (this.ParentInvoice != null)
                    this.ParentInvoice.RecalculateSums();
                return;
            }
            

            //if (unitPriceSet)
            //{
            //    if (this.ParentInvoice.RetailPrices || this.ParentInvoice.DispenserType)
            //    {
            //        this.UnitPrice = this.UnitPriceView;
            //        this.UnitPriceRetail = this.UnitPriceView / ((100 + this.VatPercentage) / 100);
            //    }
            //    else
            //    {
            //        this.UnitPriceRetail = this.UnitPriceView;
            //        this.UnitPrice = this.UnitPriceView * ((100 + this.VatPercentage) / 100);
            //    }
            //}
            //else
            //{
            //    this.UnitPriceRetail = this.UnitPrice / ((100 + this.VatPercentage) / 100);
            //    if (this.ParentInvoice.RetailPrices || this.ParentInvoice.DispenserType)
            //        this.UnitPriceView = this.UnitPrice;
            //    else
            //        this.UnitPriceView = this.UnitPriceRetail;
            //}

            //if (!discountPercentageSet)
            //{
            //    if (this.ParentInvoice.RetailPrices || this.ParentInvoice.DispenserType)
            //    {
            //        this.DiscountAmount = this.DiscountAmountView;
            //        this.DiscountNettoAmount = this.DiscountAmountView / ((100 + this.GetVatPercentage()) / 100);
            //    }
            //    else
            //    {
            //        this.DiscountNettoAmount = this.DiscountAmountView;
            //        this.DiscountAmount = this.DiscountAmountView * ((100 + this.GetVatPercentage()) / 100);
            //    }
            //    var totalPrice = this.UnitPriceRetail * this.VolumeCalc;
            //    this.DiscountPercentage = totalPrice == 0 ? 0 : this.DiscountNettoAmount / totalPrice;
            //}
            if(discountPercentageSet)
            {
                var totalPrice = this.UnitPriceRetail * this.VolumeCalc;
                decimal discAmount = totalPrice * this.DiscountPercentage / 100;
                decimal discAmountWhole = discAmount / ((100 + this.GetVatPercentage()) / 100);
                if (this.ParentInvoice.PricesIncludeVat)
                {
                    if (!this.ParentInvoice.VatExemption)
                        this.DiscountAmountView = discAmount;
                    else
                        this.DiscountAmountView = discAmountWhole;
                }
                else
                    this.DiscountAmountView = discAmountWhole;
            }
            else
            {
                var totalPrice = this.UnitPriceRetail * this.VolumeCalc;
                this.DiscountPercentage = totalPrice == 0 ? 0 : 100 * this.DiscountAmountRetail / totalPrice;
            }

            decimal calculatedTotal = decimal.Round(
                (this.VolumeCalc * this.UnitPriceRetail) - this.DiscountAmountRetail, 2);
            // A dispenser owns the payable gross. Its separately rounded volume
            // and unit price can multiply to one cent less or more. An invoice
            // linked to that physical sale must always use its persisted total.
            this.TotalPrice = UsesAuthoritativeDispenserTotal
                ? decimal.Round(this.SalesTransactionView.TotalPrice, 2)
                : calculatedTotal;
            this.VatAmount = decimal.Round(this.TotalPrice - (this.TotalPrice / ((100 + this.GetVatPercentage()) / 100)), 2);
            //this.NettoPrice = this.TotalPrice - this.VatAmount;
            if (this.ParentInvoice != null)
                this.ParentInvoice.RecalculateSums();
        }

        internal bool UsesAuthoritativeDispenserTotal
        {
            get
            {
                return this.SaleTransactionId.HasValue &&
                    this.SalesTransactionView != null &&
                    this.ParentInvoice != null &&
                    !this.ParentInvoice.VatExemption &&
                    (this.ParentInvoice.SelectedInvoiceType == null ||
                     this.ParentInvoice.SelectedInvoiceType.OfficialEnumerator != 178);
            }
        }

        public void InitDiscount(InvoiceDetailsViewModel inv)
        {
            if (inv.PricesIncludeVat)
            {
                this.discountAmountView = this.discountamount;
                this.unitPriceView = this.unitprice;
            }
            else
            {
                this.discountAmountView = this.discountamount / ((100 + this.GetVatPercentage()) / 100);
                this.unitPriceView = this.unitprice / ((100 + this.GetVatPercentage()) / 100);
            }
        }

        bool suspendChange = false;
        string[] propNames = new string[] 
        {
            "Volume",
            "VatPercentage",
            "DiscountAmountView",
            "VolumeNormalized",
            "DiscountPercentage",
            "DiscountNettoAmount",
            "UnitPriceView"
        };

        protected override void OnPropertyChanged(string propertyName)
        {
            base.OnPropertyChanged(propertyName);
            if(propNames.Contains(propertyName))
            {
                if (suspendChange)
                    return;
                suspendChange = true;
                if (propertyName == "DiscountPercentage")
                    SetComputedColumns(true);
                else if (propertyName == "UnitPriceView")
                {
                    if (this.ParentInvoice != null)
                    {
                        bool originalValue = ParentInvoice.SuspendRecalculation;
                        ParentInvoice.SuspendRecalculation = false;
                        SetComputedColumns(false, true);
                        ParentInvoice.SuspendRecalculation = originalValue;
                    }
                    else
                        SetComputedColumns(false, true);
                }
                else
                    SetComputedColumns();

                this.suspendChange = false;

                if(this.ParentInvoice != null)
                {
                    this.ParentInvoice.RecalculateSums();
                }
            }
        }

        protected override void LoadChild(DatabaseModel db, InvoiceLine entity, string propName)
        {
            if (propName == "SalesTransaction" && entity.SaleTransactionId.HasValue)
            {
                this.SalesTransactionView = new SalesTransactionViewModel();
                this.SalesTransactionView.Load(db, entity.SaleTransactionId.Value);
            }
        }

        private void SetNozzleData()
        {
            if (this.SalesTransactionView != null)
            {
                if (this.parentInvoice == null)
                    return;
                var dispenser = this.ParentInvoice.Dispensers.Where(d => d.Nozzles.Select(n => n.NozzleId).Contains(this.SalesTransactionView.NozzleId)).FirstOrDefault();
                if (dispenser != null)
                {
                    this.sourceSalePointLocked = true;
                    this.sourceDispenserId = dispenser.DispenserId;
                    this.sourceNozzleId = this.SalesTransactionView.NozzleId;
                    this.currentDispenserId = dispenser.DispenserId;
                    this.currentNozzleId = this.SalesTransactionView.NozzleId;
                    base.OnPropertyChanged("CurrentDispenserId", true);
                    base.OnPropertyChanged("CurrentNozzleId", true);
                    base.OnPropertyChanged("CanSelectSalePoint", true);
                }
            }
        }
    }
}
