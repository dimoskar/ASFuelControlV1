using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASFuelControl.Data;

namespace ASFuelControl.Windows.ViewModels
{
    public class InvoiceDetailsViewModel : InvoiceViewModel
    {
        public event EventHandler<QueryInvoiceDataArgs> QueryInvoiceData;
        public event EventHandler RefreshView;

        public bool SuspendRecalculation = false;
        private ObservableCollection<InvoiceLineViewModel> lines = new ObservableCollection<InvoiceLineViewModel>();
        private List<InvoiceRelationViewModel> invRelations = new List<InvoiceRelationViewModel>();
        private bool vatExemption { set; get; }
        private string vatExemptionReason = "";
        private decimal initialDiscount = 0;

        public Guid[] RelatedInvoiceIds { set; get; }

        public bool VatExemption
        {
            set
            {
                this.vatExemption = value;
                foreach(var line in this.lines)
                {
                    decimal up = line.UnitPriceView;
                    decimal disc = line.DiscountAmountView;
                    if (initialDiscount == 0)
                        initialDiscount = line.DiscountAmount;
                    decimal vatPercentage = Data.Implementation.OptionHandler.Instance.GetDecimalOption("VATValue", (decimal)24);
                    if (value && (this.RetailPrices || this.DispenserType))
                        up = line.UnitPriceRetail;
                    else if (!this.RetailPrices && !this.DispenserType)
                        up = decimal.Round(line.UnitPriceRetail / ((100 + vatPercentage) / 100), 3);
                    else
                        up = line.UnitPriceView;
                    if (value)
                    {
                        var newDisc = decimal.Round(initialDiscount / ((100 + vatPercentage) / 100), 2);
                        if (line.VatPercentage > 0 && newDisc != line.DiscountAmount)
                            line.DiscountAmount = newDisc;
                        if(line.VatPercentage != 0)
                            line.VatPercentage = 0;
                        
                    }
                    else
                    {
                        line.VatPercentage = vatPercentage;
                        line.DiscountAmount = initialDiscount;
                    }
                    if(line.UnitPriceView != up)
                        line.UnitPriceView = up;
                }
            }
            get { return this.vatExemption; }
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
                if (!this.IsPrinted.HasValue)
                    return true;
                if (this.IsEditLocked)
                    return false;
                return !this.IsPrinted.Value;
            }
        }

        public bool SelectCustomerEnabled
        {
            get
            {
                if (!this.IsPrinted.HasValue)
                    return true;
                if (this.IsEditLocked)
                    return false;
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
                var invType = this.InvoiceTypes.SingleOrDefault(i => i.InvoiceTypeId == this.InvoiceTypeId);
                if (invType == null)
                    return false;
                return invType.RetailInvoice.HasValue ? invType.RetailInvoice.Value : false;
            }
        }

        public bool WholeSalePrices
        {
            get { return !this.RetailPrices; }
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

        public bool IsEditLocked { set; get; }

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
                    if(this.RetailPrices || this.DispenserType)
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
                if (this.RetailPrices || this.DispenserType)
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
                if (this.RetailPrices || this.DispenserType)
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
                if (this.RetailPrices || this.DispenserType)
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
                newLine.VatPercentage = Data.Implementation.OptionHandler.Instance.GetDecimalOption("VATValue", (decimal)23);
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

        public void CreateNewSale(bool isLiterCheck)
        {
            if (this.CurrentLine == null)
                return;
            if (this.CurrentLine.SaleTransactionId.HasValue)
            {
                try
                {
                    using (DatabaseModel db = new DatabaseModel(Properties.Settings.Default.DBConnection))
                    {
                        var invoice = db.Invoices.FirstOrDefault(i => i.InvoiceId == this.InvoiceId);
                        bool hasDifferentPaymentType = invoice.PaymentType != this.PaymentType.Value;
                        invoice.PaymentType = this.PaymentType.Value;
                        invoice.RecalculateInvoice();
                        if(hasDifferentPaymentType)
                        {
                            //invoice.CreateFinTransactions
                        }
                        db.SaveChanges();
                    }
                }
                catch(Exception ex)
                {
                    Logging.Logger.Instance.LogToFile("CreateNewSale::", ex);
                }
                return;
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
                    decimal vatValue = Data.Implementation.OptionHandler.Instance.GetDecimalOption("VATValue", (decimal)23);
                    Nozzle lastNozzle = db.Nozzles.Where(n => n.NozzleId == this.CurrentLine.CurrentNozzleId).FirstOrDefault();
                    if (lastNozzle == null)
                        return;
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

                    sale.TotalPrice = decimal.Round(this.CurrentLine.TotalPrice, 2);
                    sale.CRC = Common.Helpers.CalculateCRC32(sale);


                    InvoicePrint ip = sale.Nozzle.Dispenser.InvoicePrints.FirstOrDefault();
                    if (ip == null)
                        return;

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
                        if (this.VehicleId != Guid.Empty)
                        {
                            invoice.VehicleId = this.VehicleId;
                            invoice.TraderId = this.TraderId;
                            invoice.VehiclePlateNumber = this.PlateNumber;
                            invoice.PaymentType = this.PaymentType;
                        }
                    }
                    invoice.InvoiceFormId = invoice.InvoiceType.InvoiceFormId;
                    if (lastNozzle.Dispenser.InvoicePrints.Count > 0)
                        invoice.Printer = lastNozzle.Dispenser.InvoicePrints.First().Printer;
                    if (invoice.Printer == null || invoice.Printer == "" || (invoice.InvoiceType != null && invoice.InvoiceType.IsLaserPrint.HasValue && invoice.InvoiceType.IsLaserPrint.Value))
                        invoice.Printer = invoice.InvoiceType.Printer;

                    decimal vatMult = 1 + vatValue / 100;
                    if (this.TraderId.HasValue)
                    {
                        var trader = db.Traders.SingleOrDefault(t => t.TraderId == this.TraderId.Value);
                        if (trader != null && trader.VatExemption.HasValue && invoice.Trader.VatExemption.Value)
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
                        invoice.DeliveryAddress = invoice.Trader.DeliveryAddress == null ? "" : invoice.Trader.DeliveryAddress;
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

                    if (this.CurrentLine.DiscountPercentage > 0)
                    {
                        decimal discount = Math.Round(invoice.NettoAmount.Value * this.CurrentLine.DiscountPercentage / 100, 2);
                        decimal disountRetail = Math.Round(invoice.TotalAmount.Value * this.CurrentLine.DiscountPercentage / 100, 2);
                        sale.TotalPrice = invoice.TotalAmount.Value - discount;
                        invoice.DiscountAmount = discount;
                        invLine.DiscountAmount = discount;
                        invLine.DiscountPercentage = this.CurrentLine.DiscountPercentage;
                        invoice.TotalAmount = invoice.TotalAmount - discount;
                        invLine.TotalPrice = invLine.TotalPrice - discount;
                        invoice.VatAmount = invoice.TotalAmount.Value - decimal.Round(invoice.TotalAmount.Value / vatMult, 2);
                        invoice.NettoAfterDiscount = invoice.NettoAmount.Value - invoice.DiscountAmount;
                        invoice.DiscountAmountWhole = discount;
                        invoice.DiscountAmountRetail = disountRetail;
                        invLine.VatAmount = invoice.VatAmount.Value;
                    }
                    invoice.IsPrinted = true;
                    invoice.Notes = this.Notes;
                    db.SaveChanges();
                }
                catch (Exception ex)
                {

                }
            }
        }

        public void RecalculateSums()
        {
            if (SuspendRecalculation)
                return;

            var vatInitial = Data.Implementation.OptionHandler.Instance.GetDecimalOption("VATValue", 23);

            if (this.VatExemption)
            {
                foreach (var invLine in this.InvoiceLines)
                {
                    if (invLine.SalesTransactionView != null)
                    {
                        var retailPrice = Math.Round((invLine.SalesTransactionView.UnitPrice * 100) / (100 + vatInitial), 3);
                        invLine.UnitPriceRetail = retailPrice;
                        invLine.UnitPrice = retailPrice;
                        invLine.UnitPriceWhole = retailPrice;
                    }
                    if (invLine.VatPercentage != 0)
                        invLine.VatPercentage = 0;
                }
            }

            decimal net = 0;
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
                    totalDiscounted = decimal.Round(volume * line.UnitPrice, 2);
                    discPercentage = line.SalesTransactionView.DiscountPercentage.HasValue ? line.SalesTransactionView.DiscountPercentage.Value : 0;
                    discountRetail = decimal.Round((discPercentage * decimal.Round(volume * line.UnitPrice, 2)) / 100, 2);
                    totalPreDiscount = totalDiscounted + discountRetail;
                }
                else
                {
                    if (this.RetailPrices || this.DispenserType)
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
                line.DiscountAmountWhole = discountWhole;
                line.VatAmount = lineVat;

                net = net + netDiscounted;// line.TotalPrice - line.VatAmount + line.DiscountAmountWhole;
                tot = tot + line.TotalPrice;
                vat = vat + line.VatAmount;
                disc = disc + discountRetail;
                discWhole = discWhole + discountWhole;
            }
            // decimal.Round(tot, 2);
            net = decimal.Round(net, 2);
            vat = decimal.Round(vat, 2);
            disc = decimal.Round(disc, 2);
            //tot = net + vat - disc;
            this.NettoAmount = net + discWhole;
            this.NettoAfterDiscount = net;
            this.TotalAmount = decimal.Round(tot, 2);
            this.DiscountAmountWhole = discWhole;
            this.DiscountAmount = disc;
            //this.DiscountAmountView = disc;
            this.VatAmount = vat;
        }

        public void CreateNew()
        {
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
                var transformationType = db.InvoiceTypeTransforms.Where(i => i.ParentInvoiceTypeId == fromType && i.ChildInvoiceTypeId == invoiceType).FirstOrDefault();
                if (transformationType == null)
                    return Guid.Empty;
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
                this.Printer = prName;

                this.PaymentType = invoice.PaymentType;
                this.lines.Clear();
                var lines = db.InvoiceLines.Where(i => invoiceIds.Contains(i.InvoiceId)).ToArray().
                    OrderBy(i=>i.InvoiceDate.Date).ThenBy(i=>i.InvoiceId).ToArray();
                var desc = db.Invoices.Where(i => invoiceIds.Contains(i.InvoiceId)).ToArray().Select(i => i.Description).ToArray();
                this.Notes = transformationType.NotesAddition + ": " + string.Join(", ", desc);
                if (this.VatExemptionReason != null && this.VatExemptionReason != "")
                    this.Notes = this.Notes + "\r\n" + this.VatExemptionReason;
                Dictionary<Guid, Guid> lineRelations = new Dictionary<Guid, Guid>();
                if (transformationType.TransformationMode == (int)Common.Enumerators.InvoiceTransformationTypeEnum.OneToMany)
                {
                    if (invoiceIds.Length != 1)
                        return Guid.Empty;
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
                            vd.UnitPrice = line.UnitPrice;
                            vd.MaxAllowedVolume = line.Volume - sumVolume;
                            vd.Volume = line.Volume > vd.MaxAllowedVolume ? vd.MaxAllowedVolume : line.Volume;
                            vd.DiscountAmount = line.DiscountAmount;
                            volumeData.Add(vd);
                        }
                        QueryInvoiceDataArgs args = new QueryInvoiceDataArgs();
                        args.Volumes = volumeData.ToArray();
                        //args.TraderId = traderId.HasValue ? traderId.Value : Guid.Empty;
                        //args.VehicleId = vehicleId.HasValue ? vehicleId.Value : Guid.Empty;
                        this.QueryInvoiceData(this, args);
                        if (args.Cancel)
                            return Guid.Empty;
                        
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
                            invLine.UnitPrice = vd.UnitPrice;
                            invLine.VatAmount = line.VatAmount;
                            invLine.VatPercentage = line.VatPercentage;
                            invLine.Volume = vd.Volume > vd.MaxAllowedVolume ? vd.MaxAllowedVolume : vd.Volume;
                            invLine.VolumeNormalized = vd.Volume;
                            invLine.SaleTransactionId = line.SaleTransactionId;
                            invLine.TankFillingId = line.TankFillingId;
                            invLine.PropertyChanged += Ilvm_PropertyChanged;
                            this.lines.Add(invLine);
                            invLine.SetComputedColumns(false, false);
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
                        invLine.UnitPriceRetail = line.UnitPriceRetail;
                        invLine.UnitPriceWhole = line.UnitPriceWhole;
                        invLine.UnitPrice = line.UnitPrice;
                        invLine.Volume = line.Volume;
                        invLine.VolumeNormalized = line.VolumeNormalized;
                        invLine.SaleTransactionId = line.SaleTransactionId;
                        invLine.TankFillingId = line.TankFillingId;
                        invLine.PropertyChanged += Ilvm_PropertyChanged;

                        if (transformationType.TransformationMode == (int)Common.Enumerators.InvoiceTransformationTypeEnum.Cancelation && invoiceIds.Length == 1)
                        {
                            if (line.SalesTransaction != null)
                            {
                                invLine.CurrentDispenserId = line.SalesTransaction.Nozzle.DispenserId;
                                invLine.CurrentNozzleId = line.SalesTransaction.NozzleId;
                            }
                        }

                        this.lines.Add(invLine);
                        invLine.SetComputedColumns(false, false);
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
                    relation.RelationType = (int)Common.Enumerators.InvoiceRelationTypeEnum.Replace;
                    if (transformationType.TransformationMode == (int)Common.Enumerators.InvoiceTransformationTypeEnum.Cancelation)
                        relation.RelationType = (int)Common.Enumerators.InvoiceRelationTypeEnum.Cancel;
                    var invPar = db.Invoices.SingleOrDefault(i => i.InvoiceId == id);
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
                //this.RecalculateSums();
                this.SetInvoiceSums(db, invoiceIds, mode);
                

                this.ApplicationUserId = Data.DatabaseModel.CurrentUserId;

                var fit = db.InvoiceTypes.SingleOrDefault(i => i.InvoiceTypeId == fromType);
                if (fit != null)
                {
                    if (this.SelectedInvoiceType == null)
                        return Guid.Empty;
                    if (!this.SelectedInvoiceType.IsCancelation.HasValue || !this.SelectedInvoiceType.IsCancelation.Value)
                        return Guid.Empty;
                    this.simplePrintTransform = !(fit.IsLaserPrint.HasValue ? fit.IsLaserPrint.Value : this.lines.Count != 1);
                }
            }
            return this.InvoiceId;
        }

        public void CreateInvoices(Guid[] invoiceIds, Guid invoiceType, Guid? traderId, Guid? vehicleId, int mode)
        {
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
                            vd.UnitPrice = line.UnitPrice;
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
                            invLine.UnitPriceView = vd.UnitPrice;
                            invLine.VatAmount = line.VatAmount;
                            invLine.VatPercentage = line.VatPercentage;
                            invLine.Volume = vd.Volume > vd.MaxAllowedVolume ? vd.MaxAllowedVolume : vd.Volume;
                            invLine.VolumeNormalized = vd.Volume;
                            invLine.SaleTransactionId = line.SaleTransactionId;
                            invLine.TankFillingId = line.TankFillingId;
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
                        decimal pr1 = line.UnitPriceWhole;
                        decimal pr2 = line.UnitPriceRetail;
                        decimal pr3 = line.UnitPrice;

                        invLine.UnitPriceWhole = line.UnitPriceWhole;
                        invLine.UnitPriceRetail = line.UnitPriceRetail;
                        invLine.UnitPrice = line.UnitPrice;
                        invLine.Volume = line.Volume;
                        invLine.VolumeNormalized = line.VolumeNormalized;
                        invLine.SaleTransactionId = line.SaleTransactionId;
                        invLine.TankFillingId = line.TankFillingId;
                        invLine.PropertyChanged += Ilvm_PropertyChanged;

                        if (transformationType.TransformationMode == (int)Common.Enumerators.InvoiceTransformationTypeEnum.Cancelation && invoiceIds.Length == 1)
                        {
                            if (line.SalesTransaction != null)
                            {
                                invLine.CurrentDispenserId = line.SalesTransaction.Nozzle.DispenserId;
                                invLine.CurrentNozzleId = line.SalesTransaction.NozzleId;
                            }
                        }

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
        //                invLine.SaleTransactionId = line.SaleTransactionId;
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
            base.Load(db, id);
            
            if(!this.EditEnabled)
            {
                this.InvoiceTypes = CommonCache.Instance.InvoiceTypes.ToArray();
                this.SuspendRecalculation = true;
            }

            var inv = db.Invoices.SingleOrDefault(i => i.InvoiceId == this.InvoiceId);
            if (inv == null)
                return;
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
        }

        public override bool Save(DatabaseModel db, Guid id)
        {
            if (this.SelectedInvoiceType != null && this.SelectedInvoiceType.DispenserType.HasValue && this.SelectedInvoiceType.DispenserType.Value && this.EditEnabled && !this.IsEditLocked)
            {
                this.CreateNewSale(false);
                this.Load(this.InvoiceId);
                if (this.RefreshView != null)
                    this.RefreshView(this, new EventArgs());
                return true;
            }
            else
            {
                this.RecalculateSums();
                this.Errors = new ValidationError[] { };
                if (this.InvoiceLines.Length == 0)
                    this.AddError("Σφάλμα παραστατικού", "Δεν έχετε εισάγει γραμμές στο παραστατικό");

                if (this.InvoiceTypeId == Guid.Empty)
                    this.AddError("Σφάλμα παραστατικού", "Δεν έχετε επιλέξει τύπο παραστατικού");

                if (this.SelectedInvoiceType.DeliveryType.HasValue && this.SelectedInvoiceType.DeliveryType == (int)Common.Enumerators.DeliveryTypeEnum.Delivery)
                {
                    foreach (var line in this.InvoiceLines)
                    {
                        var ft = this.FuelTypes.SingleOrDefault(f => f.FuelTypeId == line.FuelTypeId);
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
                if(this.RelatedInvoiceIds != null)
                {
                    lock (Threads.PrintAgent.ExcludeInvoices)
                    {
                        foreach (var invId in this.RelatedInvoiceIds)
                            Threads.PrintAgent.ExcludeInvoices.Remove(invId);
                    }
                    this.RelatedInvoiceIds = new Guid[] { };
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
                this.OnPropertyChanged("SupportsSupplyNumber");
            }
        }

        private void SetInvoiceSums(DatabaseModel db, Guid[] invoiceIds, int mode)
        {
            var invoices = db.Invoices.Where(i => invoiceIds.Contains(i.InvoiceId)).ToArray().
                    OrderBy(i => i.TransactionDate).ThenBy(i => i.InvoiceId).ToArray();
            this.NettoAmount = 0;
            this.VatAmount = 0;
            this.TotalAmount = 0;
            this.DiscountAmount = 0;
            foreach (var inv in invoices)
            {
                this.NettoAmount = this.NettoAmount + decimal.Round(inv.NettoAmount.HasValue ? inv.NettoAmount.Value : 0, 2);
                this.NettoAfterDiscount = this.NettoAfterDiscount + decimal.Round(inv.NettoAfterDiscount, 2);
                this.VatAmount = this.VatAmount + decimal.Round(inv.VatAmount.HasValue ? inv.VatAmount.Value : 0, 2);
                this.TotalAmount = this.TotalAmount + decimal.Round(inv.TotalAmount.HasValue ? inv.TotalAmount.Value : 0, 2);
                this.DiscountAmount = this.DiscountAmount + decimal.Round(inv.DiscountAmount, 2);
            }
            if(mode != (int)Common.Enumerators.InvoiceTransformationTypeEnum.OneToMany)
                this.SuspendRecalculation = true;
        }

        protected override void LoadArrayProperty(Data.DatabaseModel db, Data.Invoice entity, string propName)
        {
            if (propName == "InvoiceLines")
            {
                this.lines.Clear();
                foreach (var line in entity.InvoiceLines)
                {
                    InvoiceLineViewModel ilvm = new InvoiceLineViewModel();
                    ilvm.Load(db, line.InvoiceLineId);
                    ilvm.InitDiscount(this);
                    ilvm.PropertyChanged += Ilvm_PropertyChanged;
                    ilvm.ParentInvoice = this;
                    this.lines.Add(ilvm);
                }
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
                this.OnPropertyChanged("ReportPrint");
                this.OnPropertyChanged("SimplePrint");
                this.OnPropertyChanged("CanAdd");
            }
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
