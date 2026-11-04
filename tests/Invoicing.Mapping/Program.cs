using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web.Script.Serialization;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using ASFuelControl.Data;
using ASFuelControl.Invoicing.Mapping;
using Exedron.Invoicing.Core;
using Exedron.Invoicing.Core.Models;
using Exedron.Invoicing.Adapters;
using Entity = ASFuelControl.Data.Invoice;

internal sealed class Fixture
{
    public string Id { get; set; }
    public int OfficialCode { get; set; } = 100;
    public int Payment { get; set; } = 1;
    public string Country { get; set; } = "GR";
    public decimal Gross { get; set; } = 124;
    public decimal Vat { get; set; } = 24;
    public decimal Rate { get; set; } = 24;
    public decimal ExpectedNet { get; set; } = 100;
    public bool VatExemption { get; set; }
    public bool Credit { get; set; }
    public bool SameVat { get; set; }
    public bool TwoLines { get; set; }
    public long? ParentMark { get; set; }
    public bool ParentCancelled { get; set; }
    public string ExpectedProviderType { get; set; } = "1.1";
    public int ExpectedProviderPayment { get; set; } = 3;
    public bool ExpectedProviderCorrelation { get; set; }
    public string ExpectedExemption { get; set; } = "None";
    public string ExpectedClassification { get; set; }
}

internal static class Program
{
    static readonly HostInvoiceMapper Mapper = new HostInvoiceMapper();
    static readonly InvoiceBuilder Builder = new InvoiceBuilder(new InvoiceTypeResolver());
    static readonly Guid FuelId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    static int passed, failed;
    static int Main()
    {
        Console.WriteLine("OFFLINE host mapping using the real enhanced Data entities; no database context or transport.");
        var fixtures = new JavaScriptSerializer().Deserialize<Fixture[]>(File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fixtures/cases.json")));
        foreach (string culture in new[] { "en-US", "el-GR" })
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            foreach (var fixture in fixtures) Run(culture + "/baseline/" + fixture.Id, () => Baseline(fixture));
        }
        Run("discount is subtracted once", () => {
            var i = Invoice(new Fixture { Gross = 111.60m, Vat = 21.60m }); Discount(i, 12.40m, 10m);
            var r = Mapper.Map(i, Context()); Equal(100m, r.Invoice.NettoAmount); Equal(10m, r.Invoice.DiscountAmount);
            Equal(90m, Builder.Build(r).InvoiceSummary.TotalNetValue); Equal(111.60m, i.TotalAmount.Value);
            i.NettoAmount = 90; Equal(90m, Mapper.Build(i, Context()).InvoiceSummary.TotalNetValue);
        });
        Run("saved half-cent discount uses banker's rounding", () => {
            var i = Invoice(new Fixture { Gross = 1.17m, Vat = .05m, Rate = 4m });
            // 0.13 / 1.04 = 0.125 -> 0.12, matching the saved legacy calculation.
            Discount(i, .13m, .12m);
            Equal(.12m, Mapper.Map(i, Context()).Invoice.DiscountAmount);
        });
        Run("myDiesel net EFK plus corresponding VAT maps once", () => {
            var i = Invoice(new Fixture { Gross = 212.20m, Vat = 41.07m, Rate = 24m });
            i.InvoiceLines[0].TotalPrice = 196.95m;
            i.InvoiceLines[0].VatAmount = 38.12m;
            i.TotalAmount = 196.95m;
            i.VatAmount = 38.12m;
            i.NettoAmount = 171.13m;
            i.NettoAfterDiscount = 158.83m;
            Discount(i, 15.25m, 12.30m);
            i.NettoAmount = 171.13m;
            var mapped = Mapper.Map(i, Context());
            Equal(12.30m, mapped.Invoice.DiscountAmount);
            Equal(158.83m, Builder.Build(mapped).InvoiceSummary.TotalNetValue);
        });
        Run("direct pump explicit gross and net discounts override the ambiguous generic column", () => {
            var i = Invoice(new Fixture { Gross = 753.39m, Vat = 145.82m, Rate = 24m });
            var line = i.InvoiceLines[0];
            line.SaleTransactionId = Guid.NewGuid();
            line.DiscountAmount = 12.40m; // Legacy direct-pump compatibility column contains net.
            line.DiscountAmountWhole = 12.40m;
            line.DiscountAmountRetail = 15.38m;
            i.DiscountAmount = 12.40m;
            i.DiscountAmountWhole = 12.40m;
            i.DiscountAmountRetail = 15.38m;
            i.NettoAmount = 617.57m; // Legacy creator used (gross + net discount) / 1.24.
            var mapped = Mapper.Map(i, Context());
            Equal(619.97m, mapped.Invoice.NettoAmount);
            Equal(12.40m, mapped.Invoice.DiscountAmount);
            var built = Builder.Build(mapped);
            Equal(607.57m, built.InvoiceSummary.TotalNetValue);
            Equal(753.39m, built.InvoiceSummary.TotalGrossValue);
        });
        Run("legacy pump net-total exception requires an actual linked pump sale", () => {
            var i = Invoice(new Fixture { Gross = 753.39m, Vat = 145.82m, Rate = 24m });
            var line = i.InvoiceLines[0];
            line.DiscountAmount = line.DiscountAmountWhole = 12.40m;
            line.DiscountAmountRetail = 15.38m;
            i.DiscountAmount = i.DiscountAmountWhole = 12.40m;
            i.DiscountAmountRetail = 15.38m;
            i.NettoAmount = 617.57m;
            Throws(() => Mapper.Map(i, Context()));
        });
        Run("linked pump sale accepts one-cent pre-discount net rounding", () => {
            var i = Invoice(new Fixture { Gross = 753.39m, Vat = 145.82m, Rate = 24m });
            var line = i.InvoiceLines[0];
            line.SaleTransactionId = Guid.NewGuid();
            line.DiscountAmount = line.DiscountAmountWhole = 12.40m;
            line.DiscountAmountRetail = 15.38m;
            i.DiscountAmount = i.DiscountAmountWhole = 12.40m;
            i.DiscountAmountRetail = 15.38m;
            i.NettoAmount = 619.98m;
            var built = Mapper.Build(i, Context());
            Equal(607.57m, built.InvoiceSummary.TotalNetValue);
            Equal(753.39m, built.InvoiceSummary.TotalGrossValue);
        });
        Run("sub-cent Samtec price accepts normalized one-cent net rounding without a sale link", () => {
            var i = Invoice(new Fixture { Gross = 753.39m, Vat = 145.82m, Rate = 24m });
            var line = i.InvoiceLines[0];
            line.Volume = 369.780m;
            line.UnitPrice = line.UnitPriceRetail = 2.079m;
            line.DiscountAmount = line.DiscountAmountRetail = 15.38m;
            line.DiscountAmountWhole = 12.40m;
            i.DiscountAmount = i.DiscountAmountRetail = 15.38m;
            i.DiscountAmountWhole = 12.40m;
            i.NettoAmount = 619.98m;
            var built = Mapper.Build(i, Context());
            Equal(607.57m, built.InvoiceSummary.TotalNetValue);
            Equal(753.39m, built.InvoiceSummary.TotalGrossValue);
        });
        Run("retail transformation never rebuilds gross from rounded net", () => {
            var prices = TransformationUnitPrice.FromRetail(2.14900m, 24m);
            Equal(2.14900m, prices.Retail);
            Equal(1.73306m, prices.Net);
            Equal(1246.42m, decimal.Round(prices.Retail * 580m, 2,
                MidpointRounding.ToEven));
            // The former net -> retail round-trip loses one cent at this volume.
            Equal(1246.41m, decimal.Round(
                decimal.Round(prices.Net * 1.24m, 5,
                    MidpointRounding.ToEven) * 580m, 2,
                MidpointRounding.ToEven));
        });
        Run("wholesale transformation keeps net price authoritative", () => {
            var prices = TransformationUnitPrice.FromNet(1.73306m, 24m);
            Equal(1.73306m, prices.Net);
            Equal(2.14899m, prices.Retail);
        });
        Run("financial transformation keeps unit price locked", () => {
            Equal(false, TransformationEditPolicy.AllowUnitPriceEdit(
                true, false, "123456789", "987654321"));
        });
        Run("guide-only final target permits price but locks volume", () => {
            Equal(true, TransformationEditPolicy.AllowUnitPriceEdit(
                true, true, "123456789", "987654321"));
            Equal(false, TransformationEditPolicy.AllowVolumeEdit(true, true));
            Equal(true, TransformationEditPolicy.RequiresLineSelection(
                false, true, true, true, false));
        });
        Run("same supplier and counterpart VAT permits target price editing", () => {
            Equal(true, TransformationEditPolicy.VatNumbersEqual(
                "123 456 789", "EL123456789"));
            Equal(true, TransformationEditPolicy.AllowUnitPriceEdit(
                true, false, "123456789", "GR-123456789"));
            Equal(true, TransformationEditPolicy.RequiresLineSelection(
                false, true, true, false, true));
        });
        Run("nonfinancial source still requires an entered target price", () => {
            Equal(true, TransformationEditPolicy.AllowUnitPriceEdit(
                false, false, "123456789", "987654321"));
        });
        Run("two-decimal pump price does not hide a normalized net mismatch", () => {
            var i = Invoice(new Fixture { Gross = 753.39m, Vat = 145.82m, Rate = 24m });
            var line = i.InvoiceLines[0];
            line.SaleTransactionId = Guid.NewGuid();
            line.Volume = 369.780m;
            line.UnitPrice = line.UnitPriceRetail = 2.08m;
            line.DiscountAmount = line.DiscountAmountRetail = 15.38m;
            line.DiscountAmountWhole = 12.40m;
            i.DiscountAmount = i.DiscountAmountRetail = 15.38m;
            i.DiscountAmountWhole = 12.40m;
            i.NettoAmount = 619.98m;
            Throws(() => Mapper.Map(i, Context()));
        });
        Run("RecalculateInvoice writes a coherent direct-pump discount pair", () => {
            var i = Invoice(new Fixture { Gross = 753.39m, Vat = 145.82m, Rate = 24m });
            i.InvoiceType.OfficialEnumerator = 173;
            var line = i.InvoiceLines[0];
            var sale = new SalesTransaction { SalesTransactionId = Guid.NewGuid(),
                Volume = 369.780m,UnitPrice = 2.079m,TotalPrice = 753.39m,
                DiscountPercentage = 2m };
            line.SaleTransactionId = sale.SalesTransactionId;
            line.SalesTransaction = sale;
            line.Volume = sale.Volume;
            line.UnitPrice = sale.UnitPrice;
            i.RecalculateInvoice();
            Equal(15.38m,line.DiscountAmount);
            Equal(15.38m,line.DiscountAmountRetail);
            Equal(12.40m,line.DiscountAmountWhole);
            Equal(753.39m,line.TotalPrice);
            Equal(145.82m,line.VatAmount);
            Equal(15.38m,i.DiscountAmount);
            Equal(15.38m,i.DiscountAmountRetail);
            Equal(12.40m,i.DiscountAmountWhole);
            Equal(619.97m,i.NettoAmount.Value);
            Equal(607.57m,i.NettoAfterDiscount);
            Equal(753.39m,i.TotalAmount.Value);
            Equal(607.57m,Mapper.Build(i,Context()).InvoiceSummary.TotalNetValue);
        });
        Run("RecalculateInvoice preserves the dispenser gross across a one-cent multiplication difference", () => {
            var i = Invoice(new Fixture { Gross = 60m, Vat = 11.61m, Rate = 24m });
            i.InvoiceType.OfficialEnumerator = 173;
            var line = i.InvoiceLines[0];
            var sale = new SalesTransaction { SalesTransactionId = Guid.NewGuid(),
                Volume = 28.857m,UnitPrice = 2.079m,TotalPrice = 60m,
                DiscountPercentage = 0m };
            line.SaleTransactionId = sale.SalesTransactionId;
            line.SalesTransaction = sale;
            line.Volume = sale.Volume;
            line.UnitPrice = line.UnitPriceRetail = sale.UnitPrice;
            i.RecalculateInvoice();
            Equal(59.99m,decimal.Round(line.Volume * line.UnitPrice,2));
            Equal(60m,line.TotalPrice);
            Equal(60m,i.TotalAmount.Value);
            Equal(11.61m,i.VatAmount.Value);
        });
        Run("RecalculateInvoice preserves a manual-line discount", () => {
            var i = Invoice(new Fixture { Gross = 111.60m,Vat = 21.60m,Rate = 24m });
            var line = i.InvoiceLines[0];
            line.Volume = 50m; line.UnitPrice = 2.48m; line.DiscountPercentage = 10m;
            i.RecalculateInvoice();
            Equal(12.40m,line.DiscountAmountRetail);
            Equal(10m,line.DiscountAmountWhole);
            Equal(111.60m,line.TotalPrice);
            Equal(100m,i.NettoAmount.Value);
            Equal(90m,i.NettoAfterDiscount);
        });
        Reject("mismatched gross total", (i,c) => i.TotalAmount += .01m);
        Reject("mismatched VAT total", (i,c) => i.VatAmount += .01m);
        Reject("mismatched net total", (i,c) => i.NettoAmount += .01m);
        Reject("mismatched discount", (i,c) => i.InvoiceLines[0].DiscountAmount = 1m);
        Reject("missing payment", (i,c) => i.PaymentType = null);
        Reject("unknown payment", (i,c) => i.PaymentType = 99);
        Run("FleetCard always maps to AADE credit", () => { var i = Invoice(); i.PaymentType = 3; Equal(5, (int)Mapper.Build(i,Context()).PaymentMethods[0].Type); });
        Reject("missing saved number", (i,c) => i.Number = 0);
        Reject("UTC fiscal date", (i,c) => i.TransactionDate = DateTime.UtcNow);
        Reject("unknown country", (i,c) => i.Trader.Country = "XX");
        Reject("ambiguous country", (i,c) => c.Countries = c.Countries.Concat(new[] { new CountryInput { CountryCode = "GR" } }));
        Reject("foreign VAT without country", (i,c) => { i.Trader.Country = ""; i.Trader.TaxRegistrationNumber = "DE111"; });
        Reject("missing wholesale customer VAT", (i,c) => i.Trader.TaxRegistrationNumber = "");
        Run("anonymous retail and wholesale-code precedence", () => { var i = Invoice(); i.Trader = null; i.InvoiceType.OfficialEnumerator = 200; i.InvoiceType.RetailInvoice = false; Equal("11.1",Mapper.Build(i,Context()).InvoiceHeader.InvoiceType); });
        Run("wholesale flag does not override configured codes", () => { var i = Invoice(); i.InvoiceType.RetailInvoice = true; Equal("1.1",Mapper.Build(i,Context()).InvoiceHeader.InvoiceType); });
        Reject("zero VAT requires exemption", (i,c) => { i.InvoiceLines[0].VatPercentage = 0; i.InvoiceLines[0].VatAmount = 0; i.VatAmount = 0; i.NettoAmount = i.TotalAmount; });
        Reject("unsupported VAT", (i,c) => i.InvoiceLines[0].VatPercentage = 20);
        Reject("negative VAT rate", (i,c) => i.InvoiceLines[0].VatPercentage = -100);
        Reject("missing item", (i,c) => i.InvoiceLines[0].FuelType = null);
        Reject("unknown item needs mapping", (i,c) => i.InvoiceLines[0].FuelType.EnumeratorValue = 0);
        foreach (int code in new[] { 99, 101, 999, 1000, int.MaxValue })
            Run("fuel code " + code + " transmits as 999 without modifying catalogue", () => {
                var i = Invoice(new Fixture { Gross = 248, Vat = 48, TwoLines = true });
                i.InvoiceLines[1].FuelType.EnumeratorValue = code;
                var model = Mapper.Build(i, Context());
                Equal("30", model.InvoiceDetails[0].FuelCode); Equal("999", model.InvoiceDetails[1].FuelCode);
                Equal(code, i.InvoiceLines[1].FuelType.EnumeratorValue);
                foreach (var provider in new[] { "myDATA", "iTimologia", "Ilyda.Direct" })
                    new InvoiceAdapterFactory(new InvoiceTypeResolver()).CreatePayloadMapper(provider).Serialize(model, i.InvoiceId);
            });
        foreach (int code in new[] { 98, 100 })
            Reject("unsupported fuel code " + code, (i,c) => i.InvoiceLines[0].FuelType.EnumeratorValue = code);
        Reject("other charges alone cannot form a fuel invoice", (i,c) => i.InvoiceLines[0].FuelType.EnumeratorValue = 101);
        Run("other charges cannot exceed actual fuel", () => {
            var i = Invoice(new Fixture { Gross = 372, Vat = 72, TwoLines = true });
            i.InvoiceLines[1].FuelType.EnumeratorValue = 101;
            i.InvoiceLines[0].TotalPrice = 124; i.InvoiceLines[0].VatAmount = 24;
            i.InvoiceLines[1].TotalPrice = 248; i.InvoiceLines[1].VatAmount = 48;
            Throws(() => Mapper.Build(i,Context()));
        });
        Run("two other-charge codes cannot produce two 999 lines", () => {
            var i = Invoice(new Fixture { Gross = 372, Vat = 72, TwoLines = true });
            var extra = Invoice().InvoiceLines[0]; extra.InvoiceId = i.InvoiceId; extra.InvoiceLineId = Guid.NewGuid();
            i.InvoiceLines[0].TotalPrice = i.InvoiceLines[1].TotalPrice = 124;
            i.InvoiceLines[0].VatAmount = i.InvoiceLines[1].VatAmount = 24;
            i.InvoiceLines[1].FuelType.EnumeratorValue = 101;
            extra.FuelType.EnumeratorValue = 102; i.InvoiceLines.Add(extra);
            Throws(() => Mapper.Build(i,Context()));
        });
        Reject("AADE series length", (i,c) => i.Series = new string('S',51));
        Run("VAT arithmetic is validated", () => Throws(() => Mapper.Build(Invoice(new Fixture { Gross = 124, Vat = 23 }), Context())));
        Run("explicit services classification", () => { var c = Context(); c.Items[FuelId] = new ItemMapping { SalesItemType = 2, MeasurementUnit = 0 }; var m = Mapper.Build(Invoice(),c); Equal("2.1",m.InvoiceHeader.InvoiceType); Equal("category1_3",m.InvoiceDetails[0].IncomeClassification.ClassificationCategory); Equal(false,m.InvoiceHeader.FuelInvoice); });
        Run("mixed fuel and merchandise uses per-line units", () => {
            var i = Invoice(new Fixture { Gross = 248, Vat = 48, TwoLines = true }); var c = Context();
            var itemId = Guid.NewGuid(); i.InvoiceLines[1].FuelType = new FuelType { FuelTypeId = itemId, Name = "Merchandise", Code = "AB" };
            c.Items[itemId] = new ItemMapping { SalesItemType = 0, MeasurementUnit = 1 };
            var m = Mapper.Build(i,c); Equal(1,m.InvoiceDetails.Count(l => l.FuelCode == "30")); Equal(1,m.InvoiceDetails.Count(l => (int)l.MeasurementUnit == 1));
        });
        Reject("mixed goods/services blocked", (i,c) => { var extra = Invoice().InvoiceLines[0]; extra.InvoiceId = i.InvoiceId; extra.InvoiceLineId = Guid.NewGuid(); extra.FuelType = new FuelType { FuelTypeId = Guid.NewGuid(), Name = "Service" }; c.Items[extra.FuelType.FuelTypeId] = new ItemMapping { SalesItemType = 2, MeasurementUnit = 0 }; i.InvoiceLines.Add(extra); });
        Run("saved supply number and independent contact fields", () => { var i = Invoice(); i.SupplyNumber = "11112222"; i.Trader.SupplyNumber = "NEW"; i.Trader.Email = null; var m = Mapper.Build(i,Context()); Equal("11112222",m.CounterPart.SupplyAccountNo); Equal(null,m.CounterPart.Email); Equal(2,m.CounterPart.Branch); i.SupplyNumber = null; Equal(null,Mapper.Build(i,Context()).CounterPart.SupplyAccountNo); });
        foreach (int officialCode in new[] { 100,200,158,159 })
            Run("saved SupplyNumber reaches all providers for fuel document " + officialCode, () => {
                var c = Context(); if (officialCode == 158 || officialCode == 159) c.Delivery = Delivery();
                var i = Invoice(new Fixture { OfficialCode = officialCode },c);
                i.SupplyNumber = " 11112222 "; i.Trader.SupplyNumber = "CURRENT-TRADER-NUMBER";
                var request = Mapper.Map(i,c); i.SupplyNumber = "CHANGED-AFTER-SNAPSHOT";
                var model = Builder.Build(request); var factory = new InvoiceAdapterFactory(new InvoiceTypeResolver());
                var direct = XDocument.Parse(factory.CreatePayloadMapper("myDATA").Serialize(model,i.InvoiceId));
                Equal("11112222",direct.Descendants().Single(e => e.Name.LocalName == "supplyAccountNo").Value);
                var tim = JObject.Parse(factory.CreatePayloadMapper("iTimologia").Serialize(model,i.InvoiceId));
                Equal("11112222",(string)tim["counterpart"]["supply_account_no"]);
                var ilyda = JObject.Parse(factory.CreatePayloadMapper("Ilyda.Direct").Serialize(model,i.InvoiceId));
                var doc = ilyda["additionalSupportDocs"].Single(d => (string)d["description"] == "##InvoicesDoc##");
                var xml = XDocument.Parse(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String((string)doc["reference"])));
                Equal("11112222",xml.Descendants().Single(e => e.Name.LocalName == "supplyAccountNo").Value);
                Equal("111111111",(string)ilyda["buyer"]["buyerVatIdentifier"]);
            });
        Run("non-fuel goods do not transmit SupplyNumber", () => {
            var i = Invoice(); i.SupplyNumber = "11112222"; var c = Context();
            c.Items[FuelId] = new ItemMapping { SalesItemType = 0,MeasurementUnit = 1 };
            var model = Mapper.Build(i,c); Equal(false,model.InvoiceHeader.FuelInvoice);
            Equal(null,MyDataPartyFields.SupplyAccountNumber(model));
            foreach (var key in new[] { "myDATA","iTimologia","Ilyda.Direct" })
            {
                var payload = new InvoiceAdapterFactory(new InvoiceTypeResolver()).CreatePayloadMapper(key).Serialize(model,i.InvoiceId);
                if (payload.Contains("11112222")) throw new Exception("Non-fuel payload included supply number");
            }
        });
        foreach (var category in new[] { new { Exemption = 7,Code = "E" }, new { Exemption = 14,Code = "K" },new { Exemption = 8,Code = "G" },new { Exemption = 16,Code = "AE" },new { Exemption = 4,Code = "O" } })
            Run("Ilyda preserves VAT basis " + category.Code + " in line and breakdown", () => {
                var i = Invoice(new Fixture { Gross = 100,Vat = 0,Rate = 0 }); var c = Context();
                c.Items[FuelId] = new ItemMapping { SalesItemType = 0,MeasurementUnit = 1,VatExemptionArticle = category.Exemption };
                var model = Mapper.Build(i,c);
                var json = JObject.Parse(new InvoiceAdapterFactory(new InvoiceTypeResolver()).CreatePayloadMapper("Ilyda.Direct").Serialize(model,i.InvoiceId));
                Equal(category.Code,(string)json["invoiceLines"][0]["lineVatInfo"]["vatCategoryCode"]);
                Equal(category.Code,(string)json["vatBreakdowns"][0]["categoryCode"]);
            });
        Run("quantity remains ambient volume", () => { var i = Invoice(); i.InvoiceLines[0].VolumeNormalized = 999; Equal(50m,Mapper.Build(i,Context()).InvoiceDetails[0].Quantity); });
        Run("snapshot detached from entities and settings", () => {
            var i = Invoice(); var c = Context(); var r = Mapper.Map(i,c); i.Trader.Name = "CHANGED"; i.InvoiceLines[0].TotalPrice = 0; c.Company.CompanyName = "CHANGED"; c.Countries.First().CountryCode = "XX";
            var m = Builder.Build(r); Equal("A & B CUSTOMER",m.CounterPart.Name); Equal(124m,m.InvoiceSummary.TotalGrossValue); Equal("SYNTHETIC ISSUER",m.Issuer.Name); Equal("GR",m.CounterPart.Country);
        });
        Run("stable line order", () => { var i = Invoice(new Fixture { TwoLines = true, Gross = 248, Vat = 48 }); var c = Context(); var before = Mapper.Map(i,c).Invoice.InvoiceLines.Select(l => l.ItemDescription).ToArray(); var first = i.InvoiceLines[0]; i.InvoiceLines.RemoveAt(0); i.InvoiceLines.Add(first); Equal(string.Join("|",before),string.Join("|",Mapper.Map(i,c).Invoice.InvoiceLines.Select(l => l.ItemDescription))); });
        Reject("missing line identity", (i,c) => i.InvoiceLines[0].InvoiceLineId = Guid.Empty);
        foreach (string fault in new[] { "missing", "issuer", "branch", "environment", "status", "MARK", "cancel", "customer", "self", "credit-parent", "policy" })
            Run("parent rejects " + fault, () => {
                var c = Context(); var i = Invoice(new Fixture { ParentMark = 900001 }, c); var p = i.ParentInvoiceRelations[0].ParentInvoice; var e = c.Parents[p.InvoiceId];
                switch(fault) {
                    case "missing": c.Parents.Clear(); break;
                    case "issuer": e.IssuerVat = "BAD"; break;
                    case "branch": e.IssuerBranch = 4; break;
                    case "environment": e.Environment = FiscalEnvironment.Production; break;
                    case "status": e.Transmission.Status = 1; break;
                    case "MARK": e.Transmission.Mark = null; break;
                    case "cancel": e.Transmission.CancelationMark = 99; break;
                    case "customer": p.Trader.TaxRegistrationNumber = "OTHER"; break;
                    case "self": i.ParentInvoiceRelations[0].ParentInvoice = i; break;
                    case "credit-parent": p.InvoiceType.IsCancelation = true; break;
                    case "policy": c.IncludeSaleCorrelations = false; break;
                }
                Throws(() => Mapper.Map(i,c));
            });
        Run("local transformation provenance is not an external correlation", () => {
            var c = Context(); c.IncludeSaleCorrelations = false;
            var i = Invoice(); var p = Invoice();
            i.ParentInvoiceRelations.Add(new InvoiceRelation { ParentInvoice = p, RelationType = 2 });
            var request = Mapper.Map(i,c);
            Equal(0,request.Parents.Count());
            Equal(null,request.SaleCorrelations);
            Equal(null,Mapper.Build(i,c).InvoiceHeader.CorrelatedInvoices);
        });
        Run("legacy delivery to sale transformation is local provenance", () => {
            var c = Context(); c.IncludeSaleCorrelations = false;
            var i = Invoice(); var p = Invoice();
            p.InvoiceType.OfficialEnumerator = 158;
            p.InvoiceType.HasFinancialTransactions = false;
            i.ParentInvoiceRelations.Add(new InvoiceRelation { ParentInvoice = p, RelationType = 1 });
            var request = Mapper.Map(i,c);
            Equal(0,request.Parents.Count());
            Equal(null,request.SaleCorrelations);
            Equal(null,Mapper.Build(i,c).InvoiceHeader.CorrelatedInvoices);
        });
        Run("special cancellation 215 is a new correlated wholesale credit document", () => {
            var c = Context();
            var i = Invoice(new Fixture { OfficialCode = 215, ParentMark = 900215 },c);
            var original = i.ParentInvoiceRelations.Single().ParentInvoice;
            original.InvoiceType.OfficialEnumerator = 100;
            original.InvoiceType.IsCancelation = false;
            var request = Mapper.Map(i,c);
            Equal(InvoiceMode.Cancel,request.Invoice.InvoiceSeries.InvoiceType.InvoiceTypeMode);
            Equal(InvoicePaymentType.Credit,request.Invoice.PaymentType);
            var model = Builder.Build(request);
            Equal("5.1",model.InvoiceHeader.InvoiceType);
            Equal((long)900215,model.InvoiceHeader.CorrelatedInvoices.Single());
        });
        Run("special cancellation 215 cannot be transmitted as another credit against a credit invoice", () => {
            var c = Context(); c.WholesaleCodes.Add(169);
            var i = Invoice(new Fixture { OfficialCode = 215, ParentMark = 900216 },c);
            var original = i.ParentInvoiceRelations.Single().ParentInvoice;
            original.InvoiceType.OfficialEnumerator = 169;
            original.InvoiceType.IsCancelation = true;
            Throws(() => Mapper.Build(i,c));
        });
        Run("special cancellation 215 inherits retail classification from its original", () => {
            var c = Context();
            var i = Invoice(new Fixture { OfficialCode = 215, ParentMark = 900217 },c);
            var original = i.ParentInvoiceRelations.Single().ParentInvoice;
            original.InvoiceType.OfficialEnumerator = 200;
            original.InvoiceType.IsCancelation = false;
            var model = Mapper.Build(i,c);
            Equal("11.4",model.InvoiceHeader.InvoiceType);
            Equal(null,model.InvoiceHeader.CorrelatedInvoices);
        });
        Run("delivery requires structured dispatch", () => { var i = Invoice(new Fixture { OfficialCode = 158 }); Throws(() => Mapper.Map(i,Context())); });
        Run("delivery snapshot detached, multiple lines, no financial values", () => {
            var i = Invoice(new Fixture { OfficialCode = 159, Gross = 248, Vat = 48, TwoLines = true }); var c = Context(); c.Delivery = Delivery();
            var r = Mapper.Map(i,c); c.Delivery.LoadingAddress.City = "CHANGED";
            var m = Builder.Build(r); Equal(2,m.InvoiceDetails.Length); Equal(0m,m.InvoiceSummary.TotalGrossValue); Equal(0,m.PaymentMethods.Length); Equal("Test City",m.InvoiceHeader.OtherDeliveryNoteHeader.LoadingAddress.City); Equal(248m,i.TotalAmount.Value);
        });
        Run("financial delivery type keeps 1.1 values and emits the delivery flag in every channel", () => {
            var c = Context(); c.WholesaleCodes.Add(222);
            var i = Invoice(new Fixture { OfficialCode = 222 });
            // Classification comes from the official invoice type, not from
            // the independent vehicle requirement setting.
            i.InvoiceType.NeedsVehicle = false;
            i.InvoiceType.HasFinancialTransactions = true;
            i.InvoiceType.TransactionType = 0;
            i.VehiclePlateNumber = "TEST-222";

            Equal(true,HostInvoiceMapper.IsInvoiceWithDelivery(i.InvoiceType));
            var request = Mapper.Map(i,c);
            Equal(true,request.Invoice.InvoiceSeries.IsDeliveryNote);
            Equal((int)MyDataDocumentKind.Goods,request.Invoice.InvoiceSeries.InvoiceType.MyDataDocumentKind);
            Equal("TEST-222",request.Delivery.Vehicle);
            Equal(100m,request.Invoice.NettoAmount);
            Equal(124m,request.Invoice.TotalAmount);
            var m = Builder.Build(request);
            Equal("1.1",m.InvoiceHeader.InvoiceType);
            Equal(true,m.InvoiceHeader.DeliveryNote);
            Equal(124m,m.InvoiceSummary.TotalGrossValue);
            Equal(1,m.PaymentMethods.Length);

            var factory = new InvoiceAdapterFactory(new InvoiceTypeResolver());
            var direct = XDocument.Parse(factory.CreatePayloadMapper(InvoiceProviderCatalog.MyDataKey).Serialize(m,i.InvoiceId));
            Equal("1.1",direct.Descendants().Single(e => e.Name.LocalName == "invoiceType").Value);
            Equal("true",direct.Descendants().Single(e => e.Name.LocalName == "isDeliveryNote").Value);
            Equal("1",direct.Descendants().Single(e => e.Name.LocalName == "movePurpose").Value);

            var tim = JObject.Parse(factory.CreatePayloadMapper(InvoiceProviderCatalog.ITimologiaKey).Serialize(m,i.InvoiceId));
            Equal("1.1",(string)tim["invoice_header"]["invoice_type"]);
            Equal(true,(bool)tim["invoice_header"]["is_delivery_note"]);
            Equal(124m,(decimal)tim["invoice_summary"]["total_gross_value"]);

            var ilyda = JObject.Parse(factory.CreatePayloadMapper(InvoiceProviderCatalog.DirectIlydaKey).Serialize(m,i.InvoiceId));
            var support = ilyda["additionalSupportDocs"].Single(d => (string)d["description"] == "##InvoicesDoc##");
            var ilydaXml = XDocument.Parse(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String((string)support["reference"])));
            Equal("1.1",ilydaXml.Descendants().Single(e => e.Name.LocalName == "invoiceType").Value);
            Equal("true",ilydaXml.Descendants().Single(e => e.Name.LocalName == "isDeliveryNote").Value);
            Equal(124m,(decimal)ilyda["docTotal"]["invoiceTotalAmountWithVat"]);
        });
        Run("official type 222 also carries delivery when configured as retail", () => {
            var i = Invoice(new Fixture { OfficialCode = 222 });
            i.InvoiceType.NeedsVehicle = true;
            i.InvoiceType.HasFinancialTransactions = true;
            i.InvoiceType.TransactionType = 0;
            i.VehicleId = Guid.NewGuid();
            i.VehiclePlateNumber = "TEST-200";
            var model = Mapper.Build(i,Context());
            Equal("11.1",model.InvoiceHeader.InvoiceType);
            Equal(true,model.InvoiceHeader.DeliveryNote);
            Equal(124m,model.InvoiceSummary.TotalGrossValue);
            var factory = new InvoiceAdapterFactory(new InvoiceTypeResolver());
            foreach (var provider in new[] { InvoiceProviderCatalog.MyDataKey, InvoiceProviderCatalog.ITimologiaKey, InvoiceProviderCatalog.DirectIlydaKey })
            {
                var payload = factory.CreatePayloadMapper(provider).Serialize(model,i.InvoiceId);
                if (!payload.Contains("111111111")) throw new Exception(provider + " lost the retail delivery recipient");
                if (provider == InvoiceProviderCatalog.DirectIlydaKey)
                {
                    var json = JObject.Parse(payload);
                    var support = json["additionalSupportDocs"].Single(d => (string)d["description"] == "##InvoicesDoc##");
                    payload = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String((string)support["reference"]));
                }
                if (!payload.Contains(provider == InvoiceProviderCatalog.ITimologiaKey ? "is_delivery_note" : "isDeliveryNote"))
                    throw new Exception(provider + " lost the retail delivery flag");
            }
        });
        Run("needs-vehicle delivery permits a selected vehicle without a plate", () => {
            var i = Invoice(new Fixture { OfficialCode = 222 });
            i.InvoiceType.NeedsVehicle = true;
            i.InvoiceType.HasFinancialTransactions = true;
            i.InvoiceType.TransactionType = 0;
            i.VehicleId = Guid.NewGuid();
            i.VehiclePlateNumber = "";

            var request = Mapper.Map(i,Context());
            Equal(null,request.Delivery.Vehicle);
            var model = Builder.Build(request);
            Equal(null,model.InvoiceHeader.VehicleNumber);
            Equal(new DateTime(2026,10,15,10,30,0),model.InvoiceHeader.DispatchDateTime);

            var factory = new InvoiceAdapterFactory(new InvoiceTypeResolver());
            var direct = XDocument.Parse(factory.CreatePayloadMapper(
                InvoiceProviderCatalog.MyDataKey).Serialize(model,i.InvoiceId));
            Equal(0,direct.Descendants().Count(e => e.Name.LocalName == "vehicleNumber"));

            var tim = JObject.Parse(factory.CreatePayloadMapper(
                InvoiceProviderCatalog.ITimologiaKey).Serialize(model,i.InvoiceId));
            var timVehicle = tim["invoice_header"]["vehicle_number"];
            Equal(true,timVehicle == null || timVehicle.Type == JTokenType.Null ||
                string.IsNullOrWhiteSpace((string)timVehicle));

            var ilyda = JObject.Parse(factory.CreatePayloadMapper(
                InvoiceProviderCatalog.DirectIlydaKey).Serialize(model,i.InvoiceId));
            var support = ilyda["additionalSupportDocs"].Single(d =>
                (string)d["description"] == "##InvoicesDoc##");
            var ilydaXml = XDocument.Parse(System.Text.Encoding.UTF8.GetString(
                Convert.FromBase64String((string)support["reference"])));
            Equal(0,ilydaXml.Descendants().Count(e => e.Name.LocalName == "vehicleNumber"));
        });
        Run("needs-vehicle invoice still requires a selected vehicle", () => {
            var i = Invoice(new Fixture { OfficialCode = 165 });
            i.InvoiceType.NeedsVehicle = true;
            i.InvoiceType.HasFinancialTransactions = true;
            i.InvoiceType.TransactionType = 0;
            i.VehicleId = null;
            Throws(() => Mapper.Map(i,Context()));
        });
        Run("official type 165 stays an ordinary 1.1 even when it requires a vehicle", () => {
            var i = Invoice(new Fixture { OfficialCode = 165 });
            i.InvoiceType.NeedsVehicle = true;
            i.InvoiceType.HasFinancialTransactions = true;
            i.InvoiceType.TransactionType = 0;
            i.VehicleId = Guid.NewGuid();
            Equal(false,HostInvoiceMapper.IsInvoiceWithDelivery(i.InvoiceType));
            var c = Context(); c.WholesaleCodes.Add(165);
            var model = Mapper.Build(i,c);
            Equal("1.1",model.InvoiceHeader.InvoiceType);
            Equal(false,model.InvoiceHeader.DeliveryNote);
        });
        Run("code 160 self delivery accepts same VAT and reaches every channel as internal 9.3", () => {
            var i = Invoice(new Fixture { OfficialCode = 160, SameVat = true });
            i.InvoiceType.NeedsVehicle = true;
            i.InvoiceType.HasFinancialTransactions = false;
            i.InvoiceType.TransactionType = 0;
            i.VehicleId = Guid.NewGuid();
            i.VehiclePlateNumber = "TEST-160";
            var m = Mapper.Build(i,Context());
            Equal("9.3",m.InvoiceHeader.InvoiceType);
            Equal(MovePurposeEnum.Internal,m.InvoiceHeader.MovePurpose);
            Equal("000000000",m.Issuer.VATNumber);
            Equal("000000000",m.CounterPart.VATNumber);
            Equal("TEST-160",m.InvoiceHeader.VehicleNumber);
            Equal(0m,m.InvoiceSummary.TotalGrossValue);
            Equal(0,m.PaymentMethods.Length);

            var factory = new InvoiceAdapterFactory(new InvoiceTypeResolver());
            var direct = XDocument.Parse(factory.CreatePayloadMapper(InvoiceProviderCatalog.MyDataKey).Serialize(m,i.InvoiceId));
            Equal("9.3",direct.Descendants().Single(e => e.Name.LocalName == "invoiceType").Value);
            Equal("8",direct.Descendants().Single(e => e.Name.LocalName == "movePurpose").Value);
            Equal(0,direct.Descendants().Count(e => e.Name.LocalName == "currency"));
            Equal(2,direct.Descendants().Count(e => e.Name.LocalName == "vatNumber" && e.Value == "000000000"));

            var tim = JObject.Parse(factory.CreatePayloadMapper(InvoiceProviderCatalog.ITimologiaKey).Serialize(m,i.InvoiceId));
            Equal("9.3",(string)tim["invoice_header"]["invoice_type"]);
            Equal(8,(int)tim["invoice_header"]["move_purpose"]);
            Equal(null,tim["invoice_header"]["currency"]);
            Equal("000000000",(string)tim["counterpart"]["vat_number"]);

            var ilyda = JObject.Parse(factory.CreatePayloadMapper(InvoiceProviderCatalog.DirectIlydaKey).Serialize(m,i.InvoiceId));
            Equal(null,ilyda["invoiceCurrencyCode"]);
            var support = ilyda["additionalSupportDocs"].Single(d => (string)d["description"] == "##InvoicesDoc##");
            var ilydaXml = XDocument.Parse(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String((string)support["reference"])));
            Equal("9.3",ilydaXml.Descendants().Single(e => e.Name.LocalName == "invoiceType").Value);
            Equal("8",ilydaXml.Descendants().Single(e => e.Name.LocalName == "movePurpose").Value);
            Equal(0,ilydaXml.Descendants().Count(e => e.Name.LocalName == "currency"));
            Equal("000000000",(string)ilyda["buyer"]["buyerVatIdentifier"]);
        });
        Run("code 160 self delivery rejects a different counterpart VAT", () => {
            var i = Invoice(new Fixture { OfficialCode = 160 });
            i.InvoiceType.NeedsVehicle = true;
            i.InvoiceType.HasFinancialTransactions = false;
            i.InvoiceType.TransactionType = 0;
            i.VehicleId = Guid.NewGuid();
            i.VehiclePlateNumber = "TEST-160";
            Throws(() => Mapper.Map(i,Context()));
        });
        Run("different-VAT delivery keeps sales purpose instead of internal", () => {
            var i = Invoice(new Fixture { OfficialCode = 158 });
            var c = Context();
            c.Delivery = Delivery();
            var m = Mapper.Build(i,c);
            Equal(MovePurposeEnum.Sales,m.InvoiceHeader.MovePurpose);
            var direct = XDocument.Parse(new InvoiceAdapterFactory(new InvoiceTypeResolver())
                .CreatePayloadMapper(InvoiceProviderCatalog.MyDataKey).Serialize(m,i.InvoiceId));
            Equal("1",direct.Descendants().Single(e => e.Name.LocalName == "movePurpose").Value);
        });
        Run("legacy code 160 litre document is not reclassified as delivery", () => {
            var i = Invoice(new Fixture { OfficialCode = 160 });
            Equal(false,HostInvoiceMapper.IsSelfDeliveryDocument(i.InvoiceType));
            Equal("11.1",Mapper.Build(i,Context()).InvoiceHeader.InvoiceType);
        });
        Console.WriteLine("RESULT: " + passed + " passed, " + failed + " failed.");
        return failed == 0 ? 0 : 1;
    }

    static void Baseline(Fixture f)
    {
        var c = Context(); var i = Invoice(f,c);
        if (f.OfficialCode == 158 || f.OfficialCode == 159) c.Delivery = Delivery();
        if (f.SameVat || f.ParentCancelled) { Throws(() => Mapper.Map(i,c)); return; }
        var m = Mapper.Build(i,c);
        Equal(f.ExpectedProviderType,m.InvoiceHeader.InvoiceType);
        Equal(f.ExpectedProviderPayment,m.PaymentMethods.Length == 0 ? 0 : (int)m.PaymentMethods[0].Type);
        Equal(f.ExpectedNet,m.InvoiceSummary.TotalNetValue);
        Equal(f.ExpectedProviderCorrelation,m.InvoiceHeader.CorrelatedInvoices != null);
        Equal(f.ExpectedExemption,m.InvoiceDetails[0].VATExemptionCategory == null ? "None" : m.InvoiceDetails[0].VATExemptionCategory.ToString());
        if (f.ExpectedClassification != null) Equal(f.ExpectedClassification,m.InvoiceDetails[0].IncomeClassification.ClassificationType ?? "");
        var factory = new InvoiceAdapterFactory(new InvoiceTypeResolver());
        foreach (string provider in new[] { InvoiceProviderCatalog.MyDataKey, InvoiceProviderCatalog.ITimologiaKey, InvoiceProviderCatalog.DirectIlydaKey })
        {
            if (f.VatExemption && provider == InvoiceProviderCatalog.DirectIlydaKey) { ThrowsAny(() => factory.CreatePayloadMapper(provider).Serialize(m,i.InvoiceId)); continue; }
            string payload = factory.CreatePayloadMapper(provider).Serialize(m,i.InvoiceId);
            if (string.IsNullOrWhiteSpace(payload)) throw new Exception("Empty offline payload");
            if (provider == InvoiceProviderCatalog.MyDataKey)
            {
                var xml = XDocument.Parse(payload);
                Equal(f.ExpectedProviderType,xml.Descendants().Single(e => e.Name.LocalName == "invoiceType").Value);
                Equal(f.ExpectedNet,decimal.Parse(xml.Descendants().Single(e => e.Name.LocalName == "totalNetValue").Value,CultureInfo.InvariantCulture));
            }
        }
        Equal(f.Gross,i.TotalAmount.Value); Equal(null,i.IsPrinted); Equal(null,i.InvoiceSignature);
    }

    static MappingContext Context() => new MappingContext {
        Environment = FiscalEnvironment.Test, IncludeSaleCorrelations = true,
        WholesaleCodes = new System.Collections.Generic.HashSet<int>(new[] { 100,158,159 }),
        Company = new CompanySettings { CompanyTaxId = "000000000", CompanyBranch = "0", CompanyName = "SYNTHETIC ISSUER", CompanyEmail = "issuer@example.invalid", CompanyAddress = "Issuer Road 10", CompanyCity = "Test City", CompanyZipCode = "10000" },
        Countries = new[] { new CountryInput { CountryCode = "GR", IsEu = true }, new CountryInput { CountryCode = "DE", IsEu = true }, new CountryInput { CountryCode = "US", IsEu = false } }
    };
    static Entity Invoice(Fixture f = null, MappingContext c = null)
    {
        f = f ?? new Fixture();
        var i = new Entity { InvoiceId = Guid.NewGuid(), Number = 42, Series = "TEST", TransactionDate = new DateTime(2026,10,15,10,30,0),
            InvoiceType = new InvoiceType { InvoiceTypeId = Guid.NewGuid(), OfficialEnumerator = f.OfficialCode, IsCancelation = f.Credit, SendToMyData = true },
            Trader = new ASFuelControl.Data.Trader { TraderId = Guid.NewGuid(), TaxRegistrationNumber = f.SameVat ? "000000000" : "111111111", Country = f.Country, Name = "A & B CUSTOMER", Address = "Customer Road 20", City = "Test City", ZipCode = "20000", Branch = 2, VatExemption = f.VatExemption },
            PaymentType = f.Payment, TotalAmount = f.Gross, VatAmount = f.Vat, NettoAmount = f.Gross - f.Vat, NettoAfterDiscount = f.Gross - f.Vat };
        int count = f.TwoLines ? 2 : 1;
        for (int n = 0; n < count; n++) i.InvoiceLines.Add(new InvoiceLine { InvoiceLineId = new Guid(n + 1,0,0,new byte[8]), InvoiceId = i.InvoiceId,
            FuelType = new FuelType { FuelTypeId = FuelId, EnumeratorValue = 30, Name = "Heating Diesel " + n },
            Volume = 50, TotalPrice = f.Gross/count, VatAmount = f.Vat/count, VatPercentage = f.Rate });
        if (f.ParentMark.HasValue)
        {
            var p = Invoice(new Fixture { OfficialCode = f.OfficialCode, Country = f.Country });
            i.ParentInvoiceRelations.Add(new InvoiceRelation { ParentInvoice = p });
            c.Parents[p.InvoiceId] = new ParentEvidence { InvoiceId = p.InvoiceId, IssuerVat = "000000000", IssuerBranch = 0, Environment = FiscalEnvironment.Test,
                Transmission = new TransmissionEvidence { Status = 3, Mark = f.ParentMark, CanceledByMark = f.ParentCancelled ? (long?)900002 : null } };
        }
        return i;
    }
    static void Discount(Entity i, decimal gross, decimal net) { i.InvoiceLines[0].DiscountAmount = gross; i.InvoiceLines[0].DiscountAmountWhole = net; i.DiscountAmount = gross; i.DiscountAmountWhole = net; i.NettoAmount = i.TotalAmount - i.VatAmount + net; }
    static DeliveryContext Delivery() => new DeliveryContext { Vehicle = "TEST1234", Departure = new DateTime(2026,10,15,10,30,0), LoadingAddress = new Address { Street = "Issuer Road", Number = "10", City = "Test City", PostalCode = "10000" }, DeliveryAddress = new Address { Street = "Customer Road", Number = "20", City = "Test City", PostalCode = "20000" } };
    static void Reject(string name, Action<Entity,MappingContext> change) => Run(name, () => { var i = Invoice(); var c = Context(); change(i,c); Throws(() => Mapper.Map(i,c)); });
    static void Throws(Action action) { try { action(); } catch(ProviderInvoiceValidationException) { return; } throw new Exception("Expected mapping validation failure"); }
    static void ThrowsAny(Action action) { try { action(); } catch(InvalidOperationException) { return; } throw new Exception("Expected unsupported payload failure"); }
    static void Equal<T>(T expected,T actual) { if (!object.Equals(expected,actual)) throw new Exception("Expected " + expected + ", got " + actual); }
    static void Run(string name,Action action) { try { action(); passed++; Console.WriteLine("PASS " + name); } catch(Exception e) { failed++; Console.WriteLine("FAIL " + name + ": " + e); } }
}
