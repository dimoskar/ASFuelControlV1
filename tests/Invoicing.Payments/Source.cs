using System;
using ASFuelControl.Data;
using ASFuelControl.Invoicing;
using ASFuelControl.Invoicing.Mapping;
using Exedron.Invoicing.Core;
    internal sealed class Source : IInvoiceSubmissionSource
    {
        public Guid Id = Guid.NewGuid(); public Invoice Invoice; public bool Credit; public int Loads; public bool Legacy;
        public Source()
        {
            Invoice = new Invoice { InvoiceId = Id, Number = 42, Series = "TEST", TransactionDate = new DateTime(2026,10,15,10,30,0), PaymentType = 2,
                TotalAmount = 124, VatAmount = 24, NettoAmount = 100,
                InvoiceType = new InvoiceType { OfficialEnumerator = 100, SendToMyData = true },
                Trader = new Trader { TraderId = Guid.NewGuid(), TaxRegistrationNumber = "111111111", Country = "GR", Name = "Synthetic", Address = "Test Road 2", City = "Athens", ZipCode = "10000" } };
            Invoice.InvoiceLines.Add(new InvoiceLine { InvoiceLineId = Guid.NewGuid(), InvoiceId = Id, Volume = 50, TotalPrice = 124, VatAmount = 24, VatPercentage = 24,
                FuelType = new FuelType { FuelTypeId = Guid.NewGuid(), EnumeratorValue = 30, Name = "Heating diesel" } });
        }
        public InvoiceBuildRequest Prepare(Guid id, ProviderRoute route)
        {
            Loads++; DatabaseInvoiceSource.EnsureNoLegacyHistory(Invoice,Legacy);
            var mapped = new HostInvoiceMapper().Map(Invoice,new MappingContext { Environment = route.TestMode ? FiscalEnvironment.Test : FiscalEnvironment.Production,
                Company = new CompanySettings { CompanyTaxId = "000000000", CompanyBranch = "0", CompanyName = "Issuer", CompanyAddress = "Test Road 1", CompanyCity = "Athens", CompanyZipCode = "10000" },
                WholesaleCodes = new System.Collections.Generic.HashSet<int>(new[] {100}), Countries = new[] { new CountryInput { CountryCode = "GR", IsEu = true } } });
            if (Credit) mapped.Invoice.InvoiceSeries.InvoiceType.InvoiceTypeMode = InvoiceMode.Credit;
            return mapped;
        }
    }
