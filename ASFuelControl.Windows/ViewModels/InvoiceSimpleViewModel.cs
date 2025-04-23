using ASFuelControl.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASFuelControl.Windows.ViewModels
{
    public class InvoiceSimpleViewModel : InvoiceViewModel
    {
        private bool salesValueChanging = false;
        private Guid currentNozzleId { set; get; }
        private Guid currentDispenserId { set; get; }

        private ObservableCollection<InvoiceLineSimpleViewModel> lines = new ObservableCollection<InvoiceLineSimpleViewModel>();

        public InvoiceLineSimpleViewModel InvoiceLine
        {
            get
            {
                return this.lines.FirstOrDefault();
            }
        }

        public SalesTransactionViewModel SalesTransaction
        {
            get
            {
                if (this.InvoiceLine == null)
                    return new SalesTransactionViewModel();
                if (this.InvoiceLine.SalesTransaction == null)
                    this.InvoiceLine.SalesTransaction = new SalesTransactionViewModel();
                return this.InvoiceLine.SalesTransaction;
            }
        }

        public InvoiceTypeViewModel[] InvoiceTypes { set; get; }

        public Common.Enumerators.EnumItem[] PaymentTypes { set; get; }

        public FuelTypeViewModel[] FuelTypes { set; get; }

        public DispenserViewModel[] Dispensers { set; get; }

        public decimal TotalPrice
        {
            set
            {
                if (salesValueChanging)
                {
                    this.SalesTransaction.TotalPrice = value;
                    this.OnPropertyChanged("TotalPrice");
                    return;
                }
                salesValueChanging = true;
                this.SalesTransaction.TotalPrice = value;
                var price = this.DiscountPercentage < 100 ? value * 100 / (100 - this.DiscountPercentage) : 0;
                if(this.UnitPrice > 0)
                    this.Volume = price / this.UnitPrice;
                salesValueChanging = false;
                this.OnPropertyChanged("TotalPrice");
            }
            get { return this.SalesTransaction.TotalPrice; }
        }

        public decimal Volume
        {
            set
            {
                if (salesValueChanging)
                {
                    this.SalesTransaction.Volume = value;
                    this.OnPropertyChanged("Volume");
                    return;
                }
                salesValueChanging = true;
                this.SalesTransaction.Volume = value;
                var price = value * this.UnitPrice;
                this.TotalPrice = decimal.Round(price * ((100 - this.DiscountPercentage) / 100), 2);
                salesValueChanging = false;
                this.OnPropertyChanged("Volume");
            }
            get { return this.SalesTransaction.Volume; }
        }

        public decimal UnitPrice
        {
            set
            {
                if (salesValueChanging)
                {
                    this.SalesTransaction.UnitPrice = value;
                    this.OnPropertyChanged("UnitPrice");
                    return;
                }
                salesValueChanging = true;
                this.SalesTransaction.UnitPrice = value;
                var price = value * this.Volume;
                this.TotalPrice = decimal.Round(price * ((100 - this.DiscountPercentage) / 100), 2);
                salesValueChanging = false;
                this.OnPropertyChanged("TotalPrice");
                this.OnPropertyChanged("UnitPrice");
            }
            get { return this.SalesTransaction.UnitPrice; }
        }

        public decimal DiscountPercentage
        {
            set
            {
                if (salesValueChanging)
                {
                    this.SalesTransaction.UnitPrice = value;
                    this.OnPropertyChanged("DiscountPercentage");
                    return;
                }
                salesValueChanging = true;
                var price = this.UnitPrice * this.Volume;
                this.TotalPrice = decimal.Round(price * ((100 - value) / 100), 2);
                salesValueChanging = false;
                this.OnPropertyChanged("DiscountPercentage");
            }
            get { return this.SalesTransaction.DiscountPercentage.HasValue ? this.SalesTransaction.DiscountPercentage.Value : 0; }
        }

        public Guid CurrentDispenserId
        {
            set
            {
                if (this.currentDispenserId == value)
                    return;
                this.currentDispenserId = value;
                if(this.EditEnabled)
                {
                    var dispenser = this.Dispensers.SingleOrDefault(d => d.DispenserId == this.currentDispenserId);
                    if (dispenser != null && dispenser.Nozzles != null && dispenser.Nozzles.Length > 0)
                        this.CurrentNozzleId = dispenser.Nozzles.First().NozzleId;
                }
            }
            get { return this.currentDispenserId; }
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
                    var nozzle = this.Dispensers.SelectMany(d => d.Nozzles).SingleOrDefault(n => n.NozzleId == this.currentNozzleId);
                    if (nozzle != null)
                        this.UnitPrice = nozzle.UnitPrice;
                }
                this.OnPropertyChanged("CurrentNozzleId");
            }
            get { return this.currentNozzleId; }
        }

        public bool EditEnabled
        {
            get
            {
                if (!this.IsPrinted.HasValue)
                    return true;
                return !this.IsPrinted.Value;
            }
        }

        public string TraderName { set; get; }

        public string PlateNumber { set; get; }

        public string VehicleDescription
        {
            get
            {
                if(this.PlateNumber != null && this.PlateNumber != "")
                    return this.PlateNumber + " - " + this.TraderName;
                else
                    return this.TraderName;
            }
        }

        public bool CreateSale { set; get; }

        public bool VisibleSave { get { return !this.CreateSale; } }

        public InvoiceSimpleViewModel()
        {
            this.InvoiceTypes = CommonCache.Instance.InvoiceTypes.Where(i=>!i.IsLaserPrintEx).ToArray();
            this.FuelTypes = CommonCache.Instance.FuelTypes;
            this.Dispensers = CommonCache.Instance.Dispensers;
            List<Common.Enumerators.EnumItem> list = Common.Enumerators.EnumToList.EnumList<Common.Enumerators.PaymentTypeEnum>();
            this.PaymentTypes = list.ToArray();
        }

        public void CreateNew()
        {
            this.PaymentType = (int)Common.Enumerators.PaymentTypeEnum.Cash;
            this.InvoiceId = Guid.NewGuid();
            this.ApplicationUserId = Data.DatabaseModel.CurrentUserId;
            this.EntityState = EntityStateEnum.Added;
            InvoiceLineSimpleViewModel newLine = new InvoiceLineSimpleViewModel();
            newLine.InvoiceId = this.InvoiceId;
            newLine.EntityState = EntityStateEnum.Added;
            this.lines.Add(newLine);
            this.CurrentDispenserId = this.Dispensers.First().DispenserId;
        }

        public void CreateNewSale(bool isLiterCheck)
        {
            DatabaseModel db = new DatabaseModel(Properties.Settings.Default.DBConnection);
            try
            {
                decimal vatValue = Data.Implementation.OptionHandler.Instance.GetDecimalOption("VATValue", (decimal)23);
                Nozzle lastNozzle = db.Nozzles.Where(n => n.NozzleId == this.currentNozzleId).FirstOrDefault();
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
                sale.Volume = this.Volume;//nSale.TotalVolume;
                if (Data.DatabaseModel.CurrentShiftId != Guid.Empty)
                    sale.ShiftId = Data.DatabaseModel.CurrentShiftId;
                sale.UnitPrice = this.UnitPrice;

                db.Add(sale);

                //nSale.TotalPrice;
                decimal totalVol = 0;
                decimal densityAvg = 0;
                List<decimal> densities = new List<decimal>();
                foreach (NozzleFlow nzt in lastNozzle.NozzleFlows)
                {
                    if (nzt.FlowState == 0)
                        continue;
                    Data.Tank tank = db.Tanks.Where(t => t.TankId == nzt.TankId).FirstOrDefault();
                    TankSale tSale = new TankSale();
                    tSale.TankSaleId = Guid.NewGuid();
                    decimal temperature = tank.Temperatire;
                    tank.PeriodMaximum = tank.FuelLevel;
                    tank.PeriodMinimum = tank.FuelLevel;
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
                    decimal diff = tSale.EndVolumeNormalized.Value - tSale.StartVolumeNormalized;
                    densityAvg = tank.CurrentDensity * diff;
                    totalVol = totalVol + diff;
                    densities.Add(tank.CurrentDensity);
                }
                if (sale.TankSales.Count == 0)
                    return;
                decimal startTempAvg = 0;
                decimal endTempAvg = 0;
                if (totalVol != 0)
                {
                    foreach (TankSale tSale in sale.TankSales)
                    {
                        decimal vol = tSale.EndVolumeNormalized.Value - tSale.StartVolumeNormalized;
                        startTempAvg = startTempAvg + tSale.StartTemperature.Value * (vol / totalVol);
                        endTempAvg = endTempAvg + tSale.EndTemperature * (vol / totalVol);
                    }
                }
                else
                {
                    if (sale.TankSales.Count > 0)
                    {
                        foreach (TankSale tSale in sale.TankSales)
                        {
                            startTempAvg = startTempAvg + tSale.StartTemperature.Value;
                            endTempAvg = endTempAvg + tSale.EndTemperature;
                        }
                        startTempAvg = startTempAvg / sale.TankSales.Count;
                        endTempAvg = endTempAvg / sale.TankSales.Count;
                    }

                }
                sale.TemperatureStart = startTempAvg;
                sale.TemperatureEnd = endTempAvg;
                try
                {
                    if (totalVol == 0)
                    {
                        if (densities.Count > 0)
                        {
                            densityAvg = densities.Sum() / densities.Count;
                            sale.VolumeNormalized = lastNozzle.FuelType.NormalizeVolume(sale.Volume, sale.TemperatureEnd, densityAvg);
                        }
                        else
                            sale.VolumeNormalized = lastNozzle.FuelType.NormalizeVolume(sale.Volume, sale.TemperatureEnd, lastNozzle.FuelType.BaseDensity);

                    }
                    else
                        sale.VolumeNormalized = lastNozzle.FuelType.NormalizeVolume(sale.Volume, sale.TemperatureEnd, densityAvg / totalVol);
                }
                catch
                {
                    sale.VolumeNormalized = lastNozzle.FuelType.NormalizeVolume(sale.Volume, sale.TemperatureEnd, lastNozzle.FuelType.BaseDensity);
                }

                if ((sale.Volume > sale.VolumeNormalized && (sale.VolumeNormalized == 0 || sale.Volume / sale.VolumeNormalized > 2)) ||
                    (sale.VolumeNormalized > sale.Volume && (sale.Volume == 0 || sale.VolumeNormalized / sale.Volume > 2)))
                {
                    try
                    {
                        Guid[] tankIds = lastNozzle.NozzleFlows.Where(n => n.FlowState == 0).Select(t => t.TankId).ToArray();
                        decimal dencityAvg = db.Tanks.Where(t => tankIds.Contains(t.TankId)).ToList().Select(t => t.CurrentDensity).Average();
                        sale.VolumeNormalized = lastNozzle.FuelType.NormalizeVolume(sale.Volume, sale.TemperatureEnd, dencityAvg);
                    }
                    catch
                    {
                        sale.VolumeNormalized = lastNozzle.FuelType.NormalizeVolume(sale.Volume, sale.TemperatureEnd, lastNozzle.FuelType.BaseDensity);
                    }
                }
                if (sale.Volume > 0 && sale.VolumeNormalized / sale.Volume < (decimal)0.8)
                    sale.VolumeNormalized = sale.Volume;
                UsagePeriod up = db.GetUsagePeriod();
                sale.UsagePeriodId = up.UsagePeriodId;
                sale.UsagePeriod = up;

                sale.TotalPrice = decimal.Round(this.TotalPrice, 2);
                sale.CRC = Common.Helpers.CalculateCRC32(sale);


                InvoicePrint ip = sale.Nozzle.Dispenser.InvoicePrints.FirstOrDefault();
                if (ip == null)
                    return;

                Invoice invoice = new Invoice();
                invoice.ApplicationUserId = DatabaseModel.CurrentUserId;
                invoice.InvoiceId = Guid.NewGuid();
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
                        decimal vatAmount = this.TotalPrice - decimal.Round(this.TotalPrice / vatMult, 2);
                        invoice.TotalAmount = this.TotalPrice - vatAmount;
                        invoice.VatAmount = 0;
                        invoice.NettoAmount = invoice.TotalAmount;
                        vatMult = 1;
                        vatValue = 0;
                    }
                    else
                    {
                        invoice.TotalAmount = this.TotalPrice;
                        invoice.VatAmount = this.TotalPrice - decimal.Round(invoice.TotalAmount.Value / vatMult, 2);
                        invoice.NettoAmount = this.TotalPrice - invoice.VatAmount;
                    }
                }
                else
                {
                    invoice.TotalAmount = this.TotalPrice;
                    invoice.VatAmount = this.TotalPrice - decimal.Round(invoice.TotalAmount.Value / vatMult, 2);
                    invoice.NettoAmount = this.TotalPrice - invoice.VatAmount;
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

                if (this.DiscountPercentage > 0)
                {
                    decimal discount = Math.Round(invoice.TotalAmount.Value * this.DiscountPercentage / 100, 2);
                    sale.TotalPrice = invoice.TotalAmount.Value - discount;
                    invoice.DiscountAmount = discount;
                    invLine.DiscountAmount = discount;
                    invoice.TotalAmount = invoice.TotalAmount - discount;
                    invLine.TotalPrice = invLine.TotalPrice - discount;
                    invoice.VatAmount = invoice.TotalAmount.Value - decimal.Round(invoice.TotalAmount.Value / vatMult, 2);
                    invLine.VatAmount = invoice.VatAmount.Value;
                }
                invoice.IsPrinted = true;
                invoice.Notes = this.Notes;
                db.SaveChanges();
            }
            catch
            {

            }
            finally
            {
                db.Dispose();
            }
        }

        public override void Load(DatabaseModel db, Guid id)
        {
            base.Load(db, id);
            var inv = db.Invoices.SingleOrDefault(i => i.InvoiceId == this.InvoiceId);
            if (inv == null)
                return;
            if(inv.Trader != null)
                this.TraderName = inv.Trader.Name;
            if (inv.VehicleId.HasValue)
            {
                var veh = db.Vehicles.SingleOrDefault(v => v.VehicleId == inv.VehicleId.Value);
                if (veh != null)
                    this.PlateNumber = veh.PlateNumber;
            }
        }

        protected override void LoadArrayProperty(Data.DatabaseModel db, Data.Invoice entity, string propName)
        {
            if (propName == "InvoiceLines")
            {
                this.lines.Clear();
                foreach (var line in entity.InvoiceLines)
                {
                    InvoiceLineSimpleViewModel ilvm = new InvoiceLineSimpleViewModel();
                    ilvm.Load(db, line.InvoiceLineId);
                    ilvm.PropertyChanged += Ilvm_PropertyChanged;
                    this.lines.Add(ilvm);
                }
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
                    this.OnPropertyChanged("VehicleDescription", false);
                    return;
                }
                this.PlateNumber = "";
                this.TraderName = trader.Name;
                this.OnPropertyChanged("TraderName", false);
                this.OnPropertyChanged("PlateNumber", false);
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
                    this.OnPropertyChanged("VehicleDescription", false);
                    return;
                }
                this.PlateNumber = vehicle.PlateNumber;
                this.VehiclePlateNumber = this.PlateNumber;
                if (this.TraderId != vehicle.TraderId)
                    this.SetTrader(vehicle.TraderId);
                this.TraderName = vehicle.Trader.Name;
                this.OnPropertyChanged("TraderName", false);
                this.OnPropertyChanged("PlateNumber", false);
                this.OnPropertyChanged("VehicleDescription", false);
                
                db.Dispose();
            }
        }

        private void Ilvm_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            
        }
    }

    public class InvoiceLineSimpleViewModel : InvoiceLineViewModel
    {
        public SalesTransactionViewModel SalesTransaction { set; get; }

        protected override void LoadChild(DatabaseModel db, InvoiceLine entity, string propName)
        {
            if(propName == "SalesTransaction" && entity.SaleTransactionId.HasValue)
            {
                this.SalesTransaction = new SalesTransactionViewModel();
                this.SalesTransaction.Load(db, entity.SaleTransactionId.Value);
            }
        }
    }
}
