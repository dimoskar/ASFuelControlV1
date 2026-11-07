using System;
using System.Collections.Generic;
using System.Linq;
using ASFuelControl.Invoicing;
using Exedron.Invoicing.Core.Models;

namespace ASFuelControl.Windows.Threads
{
    internal sealed class StoredInvoicePrintReference
    {
        public int Version { get; set; }
        public Guid InvoicePrintDataId { get; set; }
        public Guid ReportDefinitionId { get; set; }
    }

    internal sealed class StoredInvoicePrintData
    {
        public int Version { get; set; }
        public Guid InvoiceId { get; set; }
        public string ReportKey { get; set; }
        public string FiscalFooter { get; set; }
        public string FiscalDeviceQr { get; set; }
        public string DocumentQr { get; set; }
        public string BrandQr { get; set; }
        public bool PrintTestQr { get; set; }
        public bool PrintBarcode { get; set; }
        public decimal DefaultVat { get; set; }
        public string ThermalFontName { get; set; }
        public float ThermalFontSize { get; set; }
        public StoredCompanyData Company { get; set; }
        public StoredInvoiceData Invoice { get; set; }
        public List<StoredInvoiceLineData> Lines { get; set; }
        public List<StoredThermalLineData> ThermalLines { get; set; }

        internal static StoredInvoicePrintData Capture(Data.DatabaseModel database,
            Data.Invoice invoice, InvoiceWorkflowSnapshot workflow,
            InvoiceResponse response, SharedPrinterProfile profile, string fiscalFooter)
        {
            if (database == null)
            {
                throw new ArgumentNullException(nameof(database));
            }
            if (invoice == null)
            {
                throw new ArgumentNullException(nameof(invoice));
            }

            var options = Data.Implementation.OptionHandler.Instance;
            string fiscalDeviceQr = workflow.Signing == null ? null : invoice.QRCodeData;
            string responseQr = response == null ? null : response.QrCodeUrl;
            bool repeatedQr = !string.IsNullOrWhiteSpace(fiscalDeviceQr) &&
                string.Equals(fiscalDeviceQr, responseQr,
                    StringComparison.OrdinalIgnoreCase);
            string documentQr = workflow.Provider == null || repeatedQr
                ? null
                : responseQr;
            string reportKey = profile.Thermal
                ? InvoiceReportDefinitionRegistry.ThermalKey
                : invoice.InvoiceType.IsInternal == true
                    ? InvoiceReportDefinitionRegistry.InternalLaserKey
                    : InvoiceReportDefinitionRegistry.StandardLaserKey;
            Data.InvoicePrintView[] thermalLines = database.InvoicePrintViews
                .Where(line => line.InvoiceId == invoice.InvoiceId).ToArray();
            decimal configuredFontSize = options.GetDecimalOption(
                "ThermalPrintFontSize", 9m);
            decimal normalizedFontSize = Math.Max(9m, configuredFontSize);
            var result = new StoredInvoicePrintData
            {
                Version = 1,
                InvoiceId = invoice.InvoiceId,
                ReportKey = reportKey,
                FiscalFooter = fiscalFooter,
                FiscalDeviceQr = fiscalDeviceQr,
                DocumentQr = documentQr,
                BrandQr = Reports.BrandData.SetBrandData(invoice),
                PrintTestQr = options.GetBoolOption("PrintTestQRCode", false),
                PrintBarcode = options.GetBoolOption("PrintInvoiceBarcode", false),
                DefaultVat = options.GetDecimalOption("VATValue", 23m),
                ThermalFontName = options.GetOption("ThermalPrintFontName", "Tahoma"),
                ThermalFontSize = (float)normalizedFontSize,
                Company = StoredCompanyData.Capture(options),
                Invoice = StoredInvoiceData.Capture(invoice),
                Lines = invoice.InvoiceLines.Select(StoredInvoiceLineData.Capture).ToList(),
                ThermalLines = thermalLines.Select(line =>
                    StoredThermalLineData.Capture(line, invoice)).ToList()
            };
            return result;
        }

        internal Data.Invoice RestoreInvoice()
        {
            Data.Invoice invoice = Invoice.Restore();
            List<Data.InvoiceLine> lines = Lines.Select(line => line.Restore(invoice))
                .ToList();
            invoice.InvoiceLines = lines;
            return invoice;
        }

        internal Data.InvoicePrintView[] RestoreThermalLines()
        {
            return ThermalLines.Select(line => line.Restore()).ToArray();
        }
    }

    internal sealed class StoredCompanyData
    {
        public string Name { get; set; }
        public string Occupation { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string PostalCode { get; set; }
        public string VatNumber { get; set; }
        public string TaxOffice { get; set; }
        public string Phone { get; set; }
        public string Fax { get; set; }
        public string MainAddress { get; set; }
        public string ReferenceNumber { get; set; }
        public string Efk { get; set; }
        public string Bank { get; set; }
        public string Iban { get; set; }
        public string Bank2 { get; set; }
        public string Iban2 { get; set; }
        public string LogoBase64 { get; set; }

        internal static StoredCompanyData Capture(
            Data.Implementation.OptionHandler options)
        {
            return new StoredCompanyData
            {
                Name = options.GetOption("CompanyName"),
                Occupation = options.GetOption("CompanyOccupation"),
                Address = options.GetOption("CompanyAddress"),
                City = options.GetOption("CompanyCity"),
                PostalCode = options.GetOption("CompanyPostalCode"),
                VatNumber = options.GetOption("CompanyTIN"),
                TaxOffice = options.GetOption("CompanyTaxOffice"),
                Phone = options.GetOption("CompanyPhone"),
                Fax = options.GetOption("CompanyFax"),
                MainAddress = options.GetOption("CompanyMainAddress"),
                ReferenceNumber = options.GetOption("CompanyReferenceNumber"),
                Efk = options.GetOption("CompanyEFK"),
                Bank = options.GetOption("CompanyBank"),
                Iban = options.GetOption("CompanyIBAN"),
                Bank2 = options.GetOption("CompanyBank2"),
                Iban2 = options.GetOption("CompanyIBAN2"),
                LogoBase64 = options.GetOption("CompanyLogo")
            };
        }
    }

    internal sealed class StoredInvoiceData
    {
        public Guid InvoiceId { get; set; }
        public int Number { get; set; }
        public string Series { get; set; }
        public DateTime TransactionDate { get; set; }
        public decimal? NettoAmount { get; set; }
        public decimal? VatAmount { get; set; }
        public decimal? TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal DiscountAmountWhole { get; set; }
        public decimal NettoAfterDiscount { get; set; }
        public int? PaymentType { get; set; }
        public string Notes { get; set; }
        public string InvoiceSignature { get; set; }
        public string QrCodeData { get; set; }
        public string SupplyNumber { get; set; }
        public int? VehicleOdometer { get; set; }
        public string DeliveryAddress { get; set; }
        public decimal? LastRestAmount { get; set; }
        public string TraderName { get; set; }
        public string TraderVat { get; set; }
        public string TraderTaxOffice { get; set; }
        public string TraderAddress { get; set; }
        public string TraderCity { get; set; }
        public string TraderOccupation { get; set; }
        public bool? TraderVatExemption { get; set; }
        public string TraderVatExemptionReason { get; set; }
        public bool? PrintDebtOnInvoice { get; set; }
        public string VehiclePlate { get; set; }
        public string InvoiceTypeDescription { get; set; }
        public bool? ShowFinancialData { get; set; }
        public bool? IsInternal { get; set; }

        internal static StoredInvoiceData Capture(Data.Invoice invoice)
        {
            return new StoredInvoiceData
            {
                InvoiceId = invoice.InvoiceId,
                Number = invoice.Number,
                Series = invoice.Series,
                TransactionDate = invoice.TransactionDate,
                NettoAmount = invoice.NettoAmount,
                VatAmount = invoice.VatAmount,
                TotalAmount = invoice.TotalAmount,
                DiscountAmount = invoice.DiscountAmount,
                DiscountAmountWhole = invoice.DiscountAmountWhole,
                NettoAfterDiscount = invoice.NettoAfterDiscount,
                PaymentType = invoice.PaymentType,
                Notes = invoice.Notes,
                InvoiceSignature = invoice.InvoiceSignature,
                QrCodeData = invoice.QRCodeData,
                SupplyNumber = invoice.SupplyNumber,
                VehicleOdometer = invoice.VehicleOdometer,
                DeliveryAddress = invoice.DeliveryAddress,
                LastRestAmount = invoice.LastRestAmount,
                TraderName = invoice.Trader == null ? null : invoice.Trader.Name,
                TraderVat = invoice.Trader == null ? null :
                    invoice.Trader.TaxRegistrationNumber,
                TraderTaxOffice = invoice.Trader == null ? null :
                    invoice.Trader.TaxRegistrationOffice,
                TraderAddress = invoice.Trader == null ? null : invoice.Trader.Address,
                TraderCity = invoice.Trader == null ? null : invoice.Trader.City,
                TraderOccupation = invoice.Trader == null ? null : invoice.Trader.Occupation,
                TraderVatExemption = invoice.Trader == null ? null :
                    invoice.Trader.VatExemption,
                TraderVatExemptionReason = invoice.Trader == null ? null :
                    invoice.Trader.VatExemptionReason,
                PrintDebtOnInvoice = invoice.Trader == null ? null :
                    invoice.Trader.PrintDebtOnInvoice,
                VehiclePlate = invoice.Vehicle == null ? invoice.VehiclePlateNumber :
                    invoice.Vehicle.PlateNumber,
                InvoiceTypeDescription = invoice.InvoiceType.Description,
                ShowFinancialData = invoice.InvoiceType.ShowFinancialData,
                IsInternal = invoice.InvoiceType.IsInternal
            };
        }

        internal Data.Invoice Restore()
        {
            var trader = string.IsNullOrWhiteSpace(TraderName) &&
                string.IsNullOrWhiteSpace(TraderVat) ? null : new Data.Trader
                {
                    Name = TraderName,
                    TaxRegistrationNumber = TraderVat,
                    TaxRegistrationOffice = TraderTaxOffice,
                    Address = TraderAddress,
                    City = TraderCity,
                    Occupation = TraderOccupation,
                    VatExemption = TraderVatExemption,
                    VatExemptionReason = TraderVatExemptionReason,
                    PrintDebtOnInvoice = PrintDebtOnInvoice
                };
            var invoiceType = new Data.InvoiceType
            {
                Description = InvoiceTypeDescription,
                ShowFinancialData = ShowFinancialData,
                IsInternal = IsInternal
            };
            var vehicle = string.IsNullOrWhiteSpace(VehiclePlate) ? null :
                new Data.Vehicle { PlateNumber = VehiclePlate };
            return new Data.Invoice
            {
                InvoiceId = InvoiceId,
                Number = Number,
                Series = Series,
                TransactionDate = TransactionDate,
                NettoAmount = NettoAmount,
                VatAmount = VatAmount,
                TotalAmount = TotalAmount,
                DiscountAmount = DiscountAmount,
                DiscountAmountWhole = DiscountAmountWhole,
                NettoAfterDiscount = NettoAfterDiscount,
                PaymentType = PaymentType,
                Notes = Notes,
                InvoiceSignature = InvoiceSignature,
                QRCodeData = QrCodeData,
                SupplyNumber = SupplyNumber,
                VehicleOdometer = VehicleOdometer,
                DeliveryAddress = DeliveryAddress,
                LastRestAmount = LastRestAmount,
                Trader = trader,
                Vehicle = vehicle,
                VehiclePlateNumber = VehiclePlate,
                InvoiceType = invoiceType
            };
        }
    }

    internal sealed class StoredInvoiceLineData
    {
        public Guid InvoiceLineId { get; set; }
        public decimal Volume { get; set; }
        public decimal VolumeNormalized { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal VatAmount { get; set; }
        public decimal VatPercentage { get; set; }
        public decimal DiscountAmount { get; set; }
        public string FuelName { get; set; }
        public int FuelEnumerator { get; set; }
        public bool? SupportsSupplyNumber { get; set; }
        public decimal TemperatureEnd { get; set; }
        public decimal AverageDensity { get; set; }
        public int OfficialPumpNumber { get; set; }
        public int OfficialNozzleNumber { get; set; }
        public string PumpSerialNumber { get; set; }
        public int? TankNumber { get; set; }
        public string TankSerialNumber { get; set; }

        internal static StoredInvoiceLineData Capture(Data.InvoiceLine line)
        {
            Data.SalesTransaction sale = line.SalesTransaction;
            Data.Nozzle nozzle = sale == null ? null : sale.Nozzle;
            Data.Dispenser dispenser = nozzle == null ? null : nozzle.Dispenser;
            Data.Tank tank = line.CurrentTank;
            return new StoredInvoiceLineData
            {
                InvoiceLineId = line.InvoiceLineId,
                Volume = line.Volume,
                VolumeNormalized = line.VolumeNormalized,
                UnitPrice = line.UnitPrice,
                TotalPrice = line.TotalPrice,
                VatAmount = line.VatAmount,
                VatPercentage = line.VatPercentage,
                DiscountAmount = line.DiscountAmount,
                FuelName = line.FuelType == null ? null : line.FuelType.Name,
                FuelEnumerator = line.FuelType == null ? 0 : line.FuelType.EnumeratorValue,
                SupportsSupplyNumber = line.FuelType == null ? null :
                    line.FuelType.SupportsSupplyNumber,
                TemperatureEnd = sale == null ? line.Temperature : sale.TemperatureEnd,
                AverageDensity = sale == null ? line.FuelDensity : sale.AvgDencity,
                OfficialPumpNumber = dispenser == null ? 0 : dispenser.OfficialPumpNumber,
                OfficialNozzleNumber = nozzle == null ? 0 : nozzle.OfficialNozzleNumber,
                PumpSerialNumber = dispenser == null ? null : dispenser.PumpSerialNumber,
                TankNumber = tank == null ? (int?)null : tank.TankNumber,
                TankSerialNumber = tank == null ? null : tank.TankSerialNumber
            };
        }

        internal Data.InvoiceLine Restore(Data.Invoice invoice)
        {
            var fuel = new Data.FuelType
            {
                Name = FuelName,
                EnumeratorValue = FuelEnumerator,
                SupportsSupplyNumber = SupportsSupplyNumber,
                BaseDensity = AverageDensity
            };
            var dispenser = new Data.Dispenser
            {
                OfficialPumpNumber = OfficialPumpNumber,
                PumpSerialNumber = PumpSerialNumber
            };
            var nozzle = new Data.Nozzle
            {
                OfficialNozzleNumber = OfficialNozzleNumber,
                Dispenser = dispenser,
                FuelType = fuel
            };
            var tank = TankNumber.HasValue ? new Data.Tank
            {
                TankNumber = TankNumber.Value,
                TankSerialNumber = TankSerialNumber
            } : null;
            var sale = new Data.SalesTransaction
            {
                TemperatureEnd = TemperatureEnd,
                Nozzle = nozzle,
                TankSales = new List<Data.TankSale>()
            };
            if (tank != null)
            {
                var tankSale = new Data.TankSale
                {
                    Tank = tank,
                    FuelDensity = AverageDensity,
                    SalesTransaction = sale
                };
                sale.TankSales.Add(tankSale);
            }
            var line = new Data.InvoiceLine
            {
                InvoiceLineId = InvoiceLineId,
                InvoiceId = invoice.InvoiceId,
                Invoice = invoice,
                Volume = Volume,
                VolumeNormalized = VolumeNormalized,
                UnitPrice = UnitPrice,
                TotalPrice = TotalPrice,
                VatAmount = VatAmount,
                VatPercentage = VatPercentage,
                DiscountAmount = DiscountAmount,
                FuelType = fuel,
                SalesTransaction = sale,
                Tank = tank
            };
            sale.InvoiceLines = new List<Data.InvoiceLine> { line };
            return line;
        }
    }

    internal sealed class StoredThermalLineData
    {
        public Guid InvoiceLineId { get; set; }
        public Guid InvoiceId { get; set; }
        public decimal Volume { get; set; }
        public decimal VolumeNormalized { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal? TotalAmount { get; set; }
        public decimal? NettoAmount { get; set; }
        public decimal? VatAmount { get; set; }
        public DateTime TransactionDate { get; set; }
        public int Number { get; set; }
        public string Series { get; set; }
        public string Name { get; set; }
        public string VehicleNumber { get; set; }
        public string TraderTin { get; set; }
        public string TraderName { get; set; }
        public int OfficialPumpNumber { get; set; }
        public int OfficialNozzleNumber { get; set; }
        public string InvoiceSignature { get; set; }
        public decimal InvoiceNettoAmount { get; set; }
        public decimal InvoiceVatAmount { get; set; }
        public decimal InvoiceTotalAmount { get; set; }
        public decimal InvoiceDiscountAmount { get; set; }
        public decimal InvoiceNettoAfterDiscountAmount { get; set; }

        internal static StoredThermalLineData Capture(Data.InvoicePrintView line,
            Data.Invoice invoice)
        {
            return new StoredThermalLineData
            {
                InvoiceLineId = line.InvoiceLineId,
                InvoiceId = line.InvoiceId,
                Volume = line.Volume,
                VolumeNormalized = line.VolumeNormalized,
                UnitPrice = line.UnitPrice,
                TotalAmount = line.TotalAmount,
                NettoAmount = line.NettoAmount,
                VatAmount = line.VatAmount,
                TransactionDate = line.TransactionDate,
                Number = line.Number,
                Series = line.Series,
                Name = line.Name,
                VehicleNumber = line.VehicleNumber,
                TraderTin = line.TraderTin,
                TraderName = line.TraderName,
                OfficialPumpNumber = line.OfficialPumpNumber,
                OfficialNozzleNumber = line.OfficialNozzleNumber,
                InvoiceSignature = line.InvoiceSignature,
                InvoiceNettoAmount = invoice.NettoAmount.GetValueOrDefault(),
                InvoiceVatAmount = invoice.VatAmount.GetValueOrDefault(),
                InvoiceTotalAmount = invoice.TotalAmount.GetValueOrDefault(),
                InvoiceDiscountAmount = invoice.DiscountAmount,
                InvoiceNettoAfterDiscountAmount = invoice.NettoAfterDiscount
            };
        }

        internal Data.InvoicePrintView Restore()
        {
            return new Data.InvoicePrintView
            {
                InvoiceLineId = InvoiceLineId,
                InvoiceId = InvoiceId,
                Volume = Volume,
                VolumeNormalized = VolumeNormalized,
                UnitPrice = UnitPrice,
                TotalAmount = TotalAmount,
                NettoAmount = NettoAmount,
                VatAmount = VatAmount,
                TransactionDate = TransactionDate,
                Number = Number,
                Series = Series,
                Name = Name,
                VehicleNumber = VehicleNumber,
                TraderTin = TraderTin,
                TraderName = TraderName,
                OfficialPumpNumber = OfficialPumpNumber,
                OfficialNozzleNumber = OfficialNozzleNumber,
                InvoiceSignature = InvoiceSignature,
                InvoiceNettoAmount = InvoiceNettoAmount,
                InvoiceVatAmount = InvoiceVatAmount,
                InvoiceTotalAmount = InvoiceTotalAmount,
                InvoiceDiscountAmount = InvoiceDiscountAmount,
                InvoiceNettoAfterDiscountAmount = InvoiceNettoAfterDiscountAmount
            };
        }
    }
}
