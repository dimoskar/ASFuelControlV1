using ASFuelControl.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASFuelControl.Windows.ViewModels
{
    public partial class InvoiceLineViewModel
    {
        InvoiceDetailsViewModel parentInvoice = null;
        private bool salesValueChanging = false;
        private Guid currentNozzleId { set; get; }
        private Guid currentDispenserId { set; get; }

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
                if (this.currentNozzleId == value)
                    return;
                this.currentNozzleId = value;
                if (this.EditEnabled)
                {
                    if (this.ParentInvoice.SelectedInvoiceType.DispenserType.HasValue && this.ParentInvoice.SelectedInvoiceType.DispenserType.Value)
                    {
                        var nozzle = this.ParentInvoice.Dispensers.SelectMany(d => d.Nozzles).SingleOrDefault(n => n.NozzleId == this.currentNozzleId);
                        if (nozzle != null)
                        {
                            if (this.ParentInvoice.RetailPrices || this.ParentInvoice.DispenserType)
                            {
                                this.UnitPriceView = nozzle.UnitPrice;
                            }
                            else
                            {
                                this.UnitPriceView = nozzle.UnitPrice * ((100 + this.VatPercentageView) / 100);
                            }
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
                if (this.currentDispenserId == value)
                    return;
                this.currentDispenserId = value;
                if (this.EditEnabled)
                {
                    if (this.ParentInvoice.SelectedInvoiceType.DispenserType.HasValue && this.ParentInvoice.SelectedInvoiceType.DispenserType.Value)
                    {
                        var dispenser = this.ParentInvoice.Dispensers.SingleOrDefault(d => d.DispenserId == this.currentDispenserId);
                        if (dispenser != null && dispenser.Nozzles != null && dispenser.Nozzles.Length > 0)
                            this.CurrentNozzleId = dispenser.Nozzles.First().NozzleId;
                    }
                }
            }
            get { return this.currentDispenserId; }
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

        public bool IsTransformation { set; get; }

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
                if (this.ParentInvoice.RetailPrices || this.ParentInvoice.DispenserType)
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
                    this.UnitPriceWhole = value / ((100 + this.VatPercentageView) / 100);
                    this.unitprice = value;
                    this.unitPriceRetail = value;// this.UnitPriceWhole * ((100 + this.VatPercentageView) / 100);
                }
                this.OnPropertyChanged("UnitPriceView");
            }
            get
            {
                if (this.parentInvoice == null)
                    return this.UnitPriceRetail;
                if (this.ParentInvoice.RetailPrices || this.ParentInvoice.DispenserType)
                    return this.UnitPriceRetail;
                else
                    return this.UnitPriceWhole;
            }
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
        public decimal DiscountAmountView
        {
            set
            {
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
                if (this.ParentInvoice.RetailPrices || this.ParentInvoice.DispenserType)
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
                if (this.ParentInvoice.RetailPrices || this.ParentInvoice.DispenserType)
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

            if (IsTransformation)
            {
                this.TotalPrice = decimal.Round((this.VolumeCalc * this.UnitPriceRetail) - this.DiscountAmount, 2);
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
                if (this.ParentInvoice.RetailPrices || this.ParentInvoice.DispenserType)
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

            this.TotalPrice = decimal.Round((this.VolumeCalc * this.UnitPriceRetail) - this.DiscountAmountRetail, 2);
            this.VatAmount = decimal.Round(this.TotalPrice - (this.TotalPrice / ((100 + this.GetVatPercentage()) / 100)), 2);
            //this.NettoPrice = this.TotalPrice - this.VatAmount;
            if (this.ParentInvoice != null)
                this.ParentInvoice.RecalculateSums();
        }

        public void InitDiscount(InvoiceDetailsViewModel inv)
        {
            if (inv.RetailPrices || inv.DispenserType)
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
                    this.currentDispenserId = dispenser.DispenserId;
                    this.currentNozzleId = this.SalesTransactionView.NozzleId;
                }
            }
        }
    }
}
