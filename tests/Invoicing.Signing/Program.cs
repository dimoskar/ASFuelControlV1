using System;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ASFuelControl.Data;
using ASFuelControl.Invoicing;
using ASFuelControl.Invoicing.Persistence;
using Exedron.Invoicing.Core;
using Exedron.Invoicing.Adapters.Samtec;
using Newtonsoft.Json.Linq;

internal static class Program
{
    static string connection;
    static SqlFiscalDatabase database;
    static int passed, failed;
    static int Main(string[] args)
    {
        if (args.Length != 1 || !Regex.IsMatch(args[0], "^ASFC_InvoicingStep6_[0-9a-f]{32}$")) throw new ArgumentException("Private test instance required.");
        string name = "ASFC_Step6_" + Guid.NewGuid().ToString("N");
        var cs = new SqlConnectionStringBuilder { DataSource = "(localdb)\\" + args[0], InitialCatalog = "master", IntegratedSecurity = true, Pooling = false };
        string master = cs.ConnectionString; cs.InitialCatalog = name; connection = cs.ConnectionString;
        bool created = false;
        try
        {
            Execute(master, "CREATE DATABASE [" + name + "];"); created = true;
            Execute(master, "ALTER DATABASE [" + name + "] SET COMPATIBILITY_LEVEL = 100;");
            database = new SqlFiscalDatabase(connection); database.InstallSchema();
            Tests(); Console.WriteLine("RESULT: " + passed + " passed, " + failed + " failed."); return failed == 0 ? 0 : 1;
        }
        finally
        {
            SqlConnection.ClearAllPools();
            if (created) Execute(master, "ALTER DATABASE [" + name + "] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [" + name + "];");
        }
    }
    static FiscalSigningScope Scope(string device = "samtec-test-v1") => new FiscalSigningScope(SamtecFiscalSigner.Key, device, FiscalEnvironment.Test, "000000000", 0);
    static FiscalSigningService Service(Http transport) => new FiscalSigningService(database, scope => new SamtecFiscalSigner(
        new SamtecDeviceProfile(scope, new Uri("http://device.invalid:8080/"), "POS-TID"), transport));
    static void Tests()
    {
        foreach (var kind in new[] { FiscalDocumentKind.Invoice, FiscalDocumentKind.Balance, FiscalDocumentKind.TankFilling })
            Test(kind + " saves original bytes before HTTP and retains signature/QR without print mutations", () => {
                var s = new Source(kind); var h = new Http(s.Document); var service = Service(h);
                var result = service.Sign(s.Document, Scope(), s); Equal(FiscalSigningState.Signed, result.State);
                Equal("SIGNATURE", result.Result.Signature); Equal("https://example.invalid/qr", result.Result.QrCode);
                Equal(h.Raw, result.Result.Response); Equal(1, h.Calls); Equal(3, History(s.Document).Length);
                Equal(FiscalSigningState.Prepared, History(s.Document)[0].State); Equal(FiscalSigningState.Signing, History(s.Document)[1].State);
                Equal(FiscalSigningState.Signed, service.Sign(s.Document, Scope("changed"), null).State); Equal(1,h.Calls); Equal(1,s.Loads);
                Check(s.Invoice.IsPrinted != true && s.Invoice.InvoiceSignature == null && s.Invoice.QRCodeData == null, "Invoice was modified");
                Check(s.Balance.PrintDate == null && s.Balance.DocumentSign == null && s.Filling.SignSignature == null, "Non-invoice was modified");
            });
        foreach (var culture in new[] { "el-GR", "en-US" })
            foreach (int code in new[] { 100, 169, 175, 316, 158, 501 })
                Test("legacy wire parity " + culture + " code " + code, () => {
                    var old = Thread.CurrentThread.CurrentCulture;
                    try {
                        Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                        var s = new Source(); s.Invoice.InvoiceType.OfficialEnumerator = code;
                        s.Invoice.InvoiceLines[0].VolumeNormalized = 50.1239m;
                        var legacy = new ASFuelControl.Invoicing.Tests.LegacySamtec().Build(s.Invoice, "Rendered\r\nΚείμενο");
                        var actual = s.Prepare(s.Document, Scope());
                        Check(JToken.DeepEquals(ExpectedLegacyWire(legacy), JToken.Parse(Encoding.UTF8.GetString(actual.Payload))), "Wire contract differs from PrintAgent");
                    } finally { Thread.CurrentThread.CurrentCulture = old; }
                });
        Test("code 316 supports sub-litre normalized volume", () => {
            var s = new Source(); s.Invoice.InvoiceType.OfficialEnumerator = 316; s.Invoice.InvoiceLines[0].VolumeNormalized = .1239m;
            Equal("?30!0.123", (string)JObject.Parse(Encoding.UTF8.GetString(s.Prepare(s.Document,Scope()).Payload))["Doc"]["InvoiceDetails"]["PrintLine"]);
        });
        Test("zero VAT exemption, credit and five-decimal price preserve legacy wire", () => {
            var s = new Source(); s.Invoice.PaymentType = 0; s.Invoice.Trader.VatExemption = true;
            var line = s.Invoice.InvoiceLines[0]; line.VatPercentage = 0; line.VatAmount = 0; line.UnitPriceWhole = 1.23456m;
            s.Invoice.TotalAmount = 61.73m; line.TotalPrice = 61.73m; line.DiscountAmount = 0;
            var legacy = new ASFuelControl.Invoicing.Tests.LegacySamtec().Build(s.Invoice,"Rendered\r\nΚείμενο");
            Check(JToken.DeepEquals(ExpectedLegacyWire(legacy),JToken.Parse(Encoding.UTF8.GetString(s.Prepare(s.Document,Scope()).Payload))),"exemption wire changed");
        });
        foreach (int payment in new[] { 2,4 }) Test("Samtec sign-only payment " + payment + " preserves the legacy integrated POS wire", () => {
            var s = new Source(); s.Invoice.PaymentType = payment; s.EftPosTID = "unused";
            var legacy = new ASFuelControl.Invoicing.Tests.LegacySamtec().Build(s.Invoice,"Rendered\r\nΚείμενο");
            var actual = JObject.Parse(Encoding.UTF8.GetString(s.Prepare(s.Document,Scope()).Payload));
            Check(JToken.DeepEquals(ExpectedLegacyWire(legacy),actual),"integrated POS wire changed");
            Equal(1,(int)actual["Doc"]["Payments"][0]["Type"]);
            Equal("unused",(string)actual["Doc"]["Payments"][0]["EftposTID"]);
            Equal(0,(int)actual["Doc"]["Payments"][0]["EftposTransType"]);
        });
        Test("Samtec signs FleetCard as credit without EFTPOS", () => {
            var s = new Source(); s.Invoice.PaymentType = 3; s.EftPosTID = null;
            var legacy = new ASFuelControl.Invoicing.Tests.LegacySamtec().Build(s.Invoice,"Rendered\r\nΚείμενο");
            var actual = JObject.Parse(Encoding.UTF8.GetString(s.Prepare(s.Document,Scope()).Payload));
            Check(JToken.DeepEquals(ExpectedLegacyWire(legacy),actual),"FleetCard wire changed");
            Equal(5,(int)actual["Doc"]["Payments"][0]["Type"]);
            Equal(JTokenType.Null,actual["Doc"]["Payments"][0]["EftposTID"].Type);
        });
        Test("discounted pump line folds the reduction into VAT-inclusive ItemAmount", () => {
            var s = new Source();
            s.Invoice.TotalAmount = 753.39m; s.Invoice.VatAmount = 145.82m; s.Invoice.NettoAmount = 619.98m;
            var line = s.Invoice.InvoiceLines[0];
            line.Volume = 369.780m; line.TotalPrice = 753.39m; line.VatAmount = 145.82m;
            line.UnitPrice = line.UnitPriceRetail = 2.079m; line.UnitPriceWhole = 1.67661m;
            line.DiscountAmount = 15.38m; line.DiscountAmountWhole = 12.41m;
            var actual = JObject.Parse(Encoding.UTF8.GetString(s.Prepare(s.Document,Scope()).Payload));
            var wire = actual["Doc"]["TransactionLines"][0];
            Equal(2.03740m,(decimal)wire["ItemAmount"]);
            Equal(369.780m,(decimal)wire["SaleQty"]);
            Equal((decimal)wire["GrossAmount"],decimal.Round(
                (decimal)wire["ItemAmount"] * (decimal)wire["SaleQty"],2));
            Equal((decimal)wire["GrossAmount"],(decimal)wire["NetAmount"] + (decimal)wire["VatAmount"]);
        });
        Test("Samtec applies the 12.30 gross discount to ItemAmount", () => {
            var s = new Source();
            s.Invoice.TotalAmount = 51.87m; s.Invoice.VatAmount = 10.04m; s.Invoice.NettoAmount = 41.83m;
            var line = s.Invoice.InvoiceLines[0];
            line.Volume = 30m; line.TotalPrice = 51.87m; line.VatAmount = 10.04m;
            line.UnitPrice = line.UnitPriceRetail = 2.139m; line.UnitPriceWhole = 1.725m;
            line.DiscountAmount = 12.30m; line.DiscountAmountWhole = 9.92m;
            var wire = JObject.Parse(Encoding.UTF8.GetString(s.Prepare(s.Document,Scope()).Payload))
                ["Doc"]["TransactionLines"][0];
            Equal(1.729m,(decimal)wire["ItemAmount"]);
            Equal(41.83m,(decimal)wire["NetAmount"]);
            Equal(decimal.Round((decimal)wire["ItemAmount"] * (decimal)wire["SaleQty"],2),
                (decimal)wire["GrossAmount"]);
        });
        Test("Samtec type 165 always sends the stored VAT-inclusive unit price", () => {
            var s = new Source();
            s.Invoice.InvoiceType.OfficialEnumerator = 165;
            s.Invoice.InvoiceType.RetailInvoice = false;
            s.Invoice.TotalAmount = 23.74m; s.Invoice.VatAmount = 4.59m;
            s.Invoice.NettoAmount = 19.15m;
            var line = s.Invoice.InvoiceLines[0];
            line.Volume = 12.540m; line.TotalPrice = 23.74m; line.VatAmount = 4.59m;
            line.VatPercentage = 24m;
            // The wholesale value is what the form displays. UnitPriceRetail is
            // the exact VAT-inclusive source used by the Samtec sample.
            line.UnitPriceWhole = 1.52661m;
            line.UnitPrice = 1.950m; // deliberately stale generic value
            line.UnitPriceRetail = 1.893m;
            line.DiscountAmount = line.DiscountAmountRetail =
                line.DiscountAmountWhole = 0;
            var wire = JObject.Parse(Encoding.UTF8.GetString(s.Prepare(s.Document,Scope()).Payload))
                ["Doc"]["TransactionLines"][0];
            Equal(1.893m,(decimal)wire["ItemAmount"]);
            Equal(12.540m,(decimal)wire["SaleQty"]);
            Equal(19.15m,(decimal)wire["NetAmount"]);
            Equal(4.59m,(decimal)wire["VatAmount"]);
            Equal(23.74m,(decimal)wire["GrossAmount"]);
        });
        Test("Samtec type 165 folds the discount into VAT-inclusive ItemAmount", () => {
            var s = new Source();
            s.Invoice.InvoiceType.OfficialEnumerator = 165;
            s.Invoice.InvoiceType.RetailInvoice = false;
            s.Invoice.TotalAmount = 22.50m; s.Invoice.VatAmount = 4.35m;
            s.Invoice.NettoAmount = 19.15m;
            var line = s.Invoice.InvoiceLines[0];
            line.Volume = 12.540m; line.TotalPrice = 22.50m; line.VatAmount = 4.35m;
            line.VatPercentage = 24m; line.UnitPriceWhole = 1.52661m;
            line.UnitPriceRetail = 1.893m;
            line.DiscountAmount = line.DiscountAmountWhole = 1.00m;
            line.DiscountAmountRetail = 1.24m;
            var wire = JObject.Parse(Encoding.UTF8.GetString(s.Prepare(s.Document,Scope()).Payload))
                ["Doc"]["TransactionLines"][0];
            Equal(1.79426m,(decimal)wire["ItemAmount"]);
            Equal(18.15m,(decimal)wire["NetAmount"]);
            Equal(4.35m,(decimal)wire["VatAmount"]);
            Equal(22.50m,(decimal)wire["GrossAmount"]);
            Equal(22.50m,decimal.Round((decimal)wire["ItemAmount"] *
                (decimal)wire["SaleQty"],2));
        });
        Test("Samtec type 169 sends post-discount amounts through ItemAmount", () => {
            var s = new Source();
            s.Invoice.InvoiceType.OfficialEnumerator = 169;
            s.Invoice.InvoiceType.RetailInvoice = false;
            s.Invoice.TotalAmount = 75.28m; s.Invoice.VatAmount = 14.57m;
            s.Invoice.NettoAmount = 60.71m;
            var line = s.Invoice.InvoiceLines[0];
            line.Volume = 50m; line.TotalPrice = 75.28m; line.VatAmount = 14.57m;
            line.VatPercentage = 24m; line.UnitPriceWhole = 1.62419m;
            line.UnitPriceRetail = 2.014m; line.UnitPrice = 2.014m;
            line.DiscountAmount = line.DiscountAmountWhole = 20.50m;
            line.DiscountAmountRetail = 25.42m;
            var actual = JObject.Parse(Encoding.UTF8.GetString(
                s.Prepare(s.Document,Scope()).Payload));
            var wire = actual["Doc"]["TransactionLines"][0];
            Equal(1.5056m,(decimal)wire["ItemAmount"]);
            Equal(50.000m,(decimal)wire["SaleQty"]);
            Equal(60.71m,(decimal)wire["NetAmount"]);
            Equal(14.57m,(decimal)wire["VatAmount"]);
            Equal(75.28m,(decimal)wire["GrossAmount"]);
            Equal(75.28m,(decimal)actual["Doc"]["InvoiceDetails"]["InvoiceTotal"]);
            Equal(-75.28m,(decimal)actual["Doc"]["Payments"][0]["Value"]);
        });
        Test("Samtec repairs a rejected type-169 payload by correcting ItemAmount", () => {
            var s = new Source();
            s.Invoice.InvoiceType.OfficialEnumerator = 169;
            s.Invoice.InvoiceType.RetailInvoice = false;
            s.Invoice.TotalAmount = 75.28m; s.Invoice.VatAmount = 14.57m;
            s.Invoice.NettoAmount = 60.71m;
            var line = s.Invoice.InvoiceLines[0];
            line.Volume = 50m; line.TotalPrice = 75.28m; line.VatAmount = 14.57m;
            line.VatPercentage = 24m; line.UnitPriceWhole = 1.62419m;
            line.UnitPriceRetail = line.UnitPrice = 2.014m;
            line.DiscountAmount = line.DiscountAmountWhole = 20.50m;
            line.DiscountAmountRetail = 25.42m;
            var prepared = s.Prepare(s.Document,Scope());
            var legacy = JObject.Parse(Encoding.UTF8.GetString(prepared.Payload));
            var legacyLine = legacy["Doc"]["TransactionLines"][0];
            legacyLine["ItemAmount"] = 2.014m;
            var oldRequest = new FiscalSigningRequest(prepared.AttemptId,
                prepared.Document,prepared.Scope,prepared.PayloadFormat,
                Encoding.UTF8.GetBytes(legacy.ToString(Newtonsoft.Json.Formatting.None)));
            var corrected = JObject.Parse(Encoding.UTF8.GetString(
                SamtecFiscalSigner.CorrectLineAmountsForRetry(oldRequest).Payload));
            var wire = corrected["Doc"]["TransactionLines"][0];
            Equal(60.71m,(decimal)wire["NetAmount"]);
            Equal(14.57m,(decimal)wire["VatAmount"]);
            Equal(75.28m,(decimal)wire["GrossAmount"]);
            Equal(1.5056m,(decimal)wire["ItemAmount"]);
        });
        Test("Samtec three-decimal retail price preserves the dispenser gross total", () => {
            var s = new Source();
            s.Invoice.TotalAmount = 60m; s.Invoice.VatAmount = 11.61m; s.Invoice.NettoAmount = 48.39m;
            var line = s.Invoice.InvoiceLines[0];
            var sale = new SalesTransaction { SalesTransactionId = Guid.NewGuid(), Volume = 28.857m,
                UnitPrice = 2.079m, TotalPrice = 60m };
            line.SaleTransactionId = sale.SalesTransactionId; line.SalesTransaction = sale;
            line.Volume = sale.Volume;
            line.UnitPrice = line.UnitPriceRetail = 2.079m; line.UnitPriceWhole = 1.67661m;
            line.TotalPrice = 60m; line.VatAmount = 11.61m; line.DiscountAmount = 0;
            var wire = JObject.Parse(Encoding.UTF8.GetString(s.Prepare(s.Document,Scope()).Payload))
                ["Doc"]["TransactionLines"][0];
            Equal(60m,(decimal)wire["GrossAmount"]);
            Equal(2.079m,(decimal)wire["ItemAmount"]);
            Equal(28.860m,(decimal)wire["SaleQty"]);
            Equal(60m,decimal.Round((decimal)wire["ItemAmount"] * (decimal)wire["SaleQty"],2));
        });
        Test("Samtec takes a directly linked sale retail price from SalesTransaction", () => {
            var s = new Source();
            s.Invoice.TotalAmount = 10m; s.Invoice.VatAmount = 1.94m; s.Invoice.NettoAmount = 8.06m;
            var line = s.Invoice.InvoiceLines[0];
            var sale = new SalesTransaction { SalesTransactionId = Guid.NewGuid(), Volume = 4.610m,
                UnitPrice = 2.169m, TotalPrice = 10m };
            line.SaleTransactionId = sale.SalesTransactionId; line.SalesTransaction = sale;
            line.Volume = sale.Volume; line.TotalPrice = 10m; line.VatAmount = 1.94m;
            line.VatPercentage = 24m; line.DiscountAmount = 0;
            // Deliberately stale invoice-line prices must not leak into a physical sale request.
            line.UnitPrice = line.UnitPriceRetail = 2.000m; line.UnitPriceWhole = 1.61290m;
            var wire = JObject.Parse(Encoding.UTF8.GetString(s.Prepare(s.Document,Scope()).Payload))
                ["Doc"]["TransactionLines"][0];
            Equal(2.169m,(decimal)wire["ItemAmount"]);
            Equal(4.610m,(decimal)wire["SaleQty"]);
            Equal(8.06m,(decimal)wire["NetAmount"]);
            Equal(1.94m,(decimal)wire["VatAmount"]);
            Equal(10m,(decimal)wire["GrossAmount"]);
        });
        Test("Samtec accepts the reported 20-euro 2.049 retail-price case", () => {
            var s = new Source();
            s.Invoice.TotalAmount = 20m; s.Invoice.VatAmount = 3.87m; s.Invoice.NettoAmount = 16.13m;
            var line = s.Invoice.InvoiceLines[0];
            var sale = new SalesTransaction { SalesTransactionId = Guid.NewGuid(), Volume = 9.760m,
                UnitPrice = 2.049m, TotalPrice = 20m };
            line.SaleTransactionId = sale.SalesTransactionId; line.SalesTransaction = sale;
            line.Volume = sale.Volume; line.TotalPrice = 20m; line.VatAmount = 3.87m;
            line.VatPercentage = 24m; line.DiscountAmount = line.DiscountAmountRetail = 0;
            line.UnitPrice = line.UnitPriceRetail = 2.049m; line.UnitPriceWhole = 1.65242m;
            var wire = JObject.Parse(Encoding.UTF8.GetString(s.Prepare(s.Document,Scope()).Payload))
                ["Doc"]["TransactionLines"][0];
            Equal(2.049m,(decimal)wire["ItemAmount"]);
            Equal(9.760m,(decimal)wire["SaleQty"]);
            Equal(16.13m,(decimal)wire["NetAmount"]);
            Equal(3.87m,(decimal)wire["VatAmount"]);
            Equal(20m,(decimal)wire["GrossAmount"]);
        });
        Test("Samtec preserves a forty-euro retail sale at its retail unit price", () => {
            var s = new Source();
            s.Invoice.TotalAmount = 40m; s.Invoice.VatAmount = 7.74m; s.Invoice.NettoAmount = 32.26m;
            var line = s.Invoice.InvoiceLines[0];
            var sale = new SalesTransaction { SalesTransactionId = Guid.NewGuid(), Volume = 19.650m,
                UnitPrice = 2.035m, TotalPrice = 40m };
            line.SaleTransactionId = sale.SalesTransactionId; line.SalesTransaction = sale;
            line.Volume = sale.Volume;
            line.UnitPrice = line.UnitPriceRetail = 2.035m; line.UnitPriceWhole = 1.64113m;
            line.TotalPrice = 40m; line.VatAmount = 7.74m; line.DiscountAmount = 0;
            var wire = JObject.Parse(Encoding.UTF8.GetString(s.Prepare(s.Document,Scope()).Payload))
                ["Doc"]["TransactionLines"][0];
            Equal(40m,(decimal)wire["GrossAmount"]);
            Equal(2.035m,(decimal)wire["ItemAmount"]);
            Equal(19.656m,(decimal)wire["SaleQty"]);
            Equal(40m,decimal.Round((decimal)wire["ItemAmount"] * (decimal)wire["SaleQty"],2));
        });
        Test("Samtec micro-adjusts only the wire quantity for a sale-derived dispenser total", () => {
            var s = new Source();
            s.Invoice.TotalAmount = 20m; s.Invoice.VatAmount = 3.87m; s.Invoice.NettoAmount = 16.13m;
            var line = s.Invoice.InvoiceLines[0];
            var sale = new SalesTransaction { SalesTransactionId = Guid.NewGuid(), Volume = 9.660m,
                UnitPrice = 2.069m, TotalPrice = 20m };
            line.SaleTransactionId = sale.SalesTransactionId; line.SalesTransaction = sale;
            line.Volume = sale.Volume;
            line.UnitPrice = line.UnitPriceRetail = 2.069m; line.UnitPriceWhole = 1.66855m;
            line.TotalPrice = 20m; line.VatAmount = 3.87m; line.DiscountAmount = 0;
            var wire = JObject.Parse(Encoding.UTF8.GetString(s.Prepare(s.Document,Scope()).Payload))
                ["Doc"]["TransactionLines"][0];
            Equal(9.660m,line.Volume);
            Equal(9.667m,(decimal)wire["SaleQty"]);
            Equal(2.069m,(decimal)wire["ItemAmount"]);
            Equal(20m,decimal.Round((decimal)wire["ItemAmount"] * (decimal)wire["SaleQty"],2));
        });
        Test("Samtec does not micro-adjust an unrelated manual invoice line", () => {
            var s = new Source();
            s.Invoice.TotalAmount = 20m; s.Invoice.VatAmount = 3.87m; s.Invoice.NettoAmount = 16.13m;
            var line = s.Invoice.InvoiceLines[0];
            line.Volume = 9.660m; line.UnitPrice = line.UnitPriceRetail = 2.069m;
            line.UnitPriceWhole = 1.66855m;
            line.TotalPrice = 20m; line.VatAmount = 3.87m; line.DiscountAmount = 0;
            Throws(() => s.Prepare(s.Document,Scope()));
            Equal(9.660m,line.Volume);
        });
        Test("Samtec never reconstructs a VAT-inclusive ItemAmount from UnitPriceWhole", () => {
            var s = new Source();
            var line = s.Invoice.InvoiceLines[0];
            line.UnitPrice = line.UnitPriceRetail = 0;
            line.UnitPriceWhole = 2m;
            line.VatPercentage = 24m;
            Throws(() => s.Prepare(s.Document,Scope()));
        });
        Test("Samtec recognizes a transformed line whose parent came from a sale", () => {
            var s = new Source();
            s.Invoice.TotalAmount = 20m; s.Invoice.VatAmount = 3.87m; s.Invoice.NettoAmount = 16.13m;
            var line = s.Invoice.InvoiceLines[0];
            line.Volume = 9.660m;
            // Simulate a legacy automatic unit-price adjustment. The unchanged
            // pre-discount total must still select the original sale price.
            line.UnitPrice = line.UnitPriceRetail = 2.070m;
            line.UnitPriceWhole = 1.66935m;
            line.TotalPrice = 20m; line.VatAmount = 3.87m; line.DiscountAmount = 0;
            var sale = new SalesTransaction { SalesTransactionId = Guid.NewGuid(), UnitPrice = 2.069m,
                Volume = 9.660m, TotalPrice = 20m };
            var parent = new InvoiceLine { InvoiceLineId = Guid.NewGuid(),
                SaleTransactionId = sale.SalesTransactionId, SalesTransaction = sale,
                Volume = 9.660m, TotalPrice = 20m };
            line.InvoiceLineRelations_ChildRelationId.Add(new InvoiceLineRelation {
                InvoiceLineRelationId = Guid.NewGuid(), ParentLineId = parent.InvoiceLineId,
                ChildRelationId = line.InvoiceLineId, InvoiceLine_ParentLineId = parent,
                InvoiceLine_ChildRelationId = line
            });
            var wire = JObject.Parse(Encoding.UTF8.GetString(s.Prepare(s.Document,Scope()).Payload))
                ["Doc"]["TransactionLines"][0];
            Equal(9.667m,(decimal)wire["SaleQty"]);
            Equal(2.069m,(decimal)wire["ItemAmount"]);
            Equal(9.660m,line.Volume);
        });
        Test("Samtec chooses the closest quantity for the reported 2.019 retail case", () => {
            var s = new Source();
            s.Invoice.TotalAmount = 20m; s.Invoice.VatAmount = 3.87m; s.Invoice.NettoAmount = 16.13m;
            var line = s.Invoice.InvoiceLines[0];
            var sale = new SalesTransaction { SalesTransactionId = Guid.NewGuid(), Volume = 9.900m,
                UnitPrice = 2.0189928m, TotalPrice = 20m };
            line.SaleTransactionId = sale.SalesTransactionId; line.SalesTransaction = sale;
            line.Volume = sale.Volume; line.TotalPrice = 20m; line.VatAmount = 3.87m;
            line.VatPercentage = 24m; line.DiscountAmount = line.DiscountAmountRetail = 0;
            line.UnitPrice = line.UnitPriceRetail = 2.019m; line.UnitPriceWhole = 1.62822m;
            var wire = JObject.Parse(Encoding.UTF8.GetString(s.Prepare(s.Document,Scope()).Payload))
                ["Doc"]["TransactionLines"][0];
            Equal(9.900m,line.Volume);
            Equal(2.01899m,(decimal)wire["ItemAmount"]);
            Equal(9.906m,(decimal)wire["SaleQty"]);
            Equal(20.00011494m,(decimal)wire["ItemAmount"] * (decimal)wire["SaleQty"]);
            Equal(16.13m,(decimal)wire["NetAmount"]);
        });
        Test("Samtec compares transformed totals before discount", () => {
            var s = new Source();
            s.Invoice.TotalAmount = 19m; s.Invoice.VatAmount = 3.68m; s.Invoice.NettoAmount = 15.32m;
            var line = s.Invoice.InvoiceLines[0];
            line.Volume = 9.660m; line.UnitPrice = line.UnitPriceRetail = 2.069m;
            line.UnitPriceWhole = 1.66855m;
            line.TotalPrice = 19m; line.VatAmount = 3.68m;
            line.DiscountAmount = line.DiscountAmountRetail = 1m;
            line.DiscountAmountWhole = 0.81m;
            var sale = new SalesTransaction { SalesTransactionId = Guid.NewGuid(), UnitPrice = 2.069m,
                Volume = 9.660m, TotalPrice = 20m };
            var parent = new InvoiceLine { InvoiceLineId = Guid.NewGuid(),
                SaleTransactionId = sale.SalesTransactionId, SalesTransaction = sale,
                Volume = 9.660m, TotalPrice = 20m };
            line.InvoiceLineRelations_ChildRelationId.Add(new InvoiceLineRelation {
                InvoiceLineRelationId = Guid.NewGuid(), ParentLineId = parent.InvoiceLineId,
                ChildRelationId = line.InvoiceLineId, InvoiceLine_ParentLineId = parent,
                InvoiceLine_ChildRelationId = line
            });
            var wire = JObject.Parse(Encoding.UTF8.GetString(s.Prepare(s.Document,Scope()).Payload))
                ["Doc"]["TransactionLines"][0];
            Equal(1.96687m,(decimal)wire["ItemAmount"]);
            Equal(9.660m,(decimal)wire["SaleQty"]);
            Equal(19m,decimal.Round((decimal)wire["ItemAmount"] *
                (decimal)wire["SaleQty"],2));
        });
        Test("Samtec preserves an explicitly edited transformed target price", () => {
            var s = new Source();
            s.Invoice.TotalAmount = 19.17m; s.Invoice.VatAmount = 3.71m; s.Invoice.NettoAmount = 15.46m;
            var line = s.Invoice.InvoiceLines[0];
            line.Volume = 9.660m; line.UnitPrice = line.UnitPriceRetail = 1.984m;
            line.UnitPriceWhole = 1.60000m;
            line.TotalPrice = 19.17m; line.VatAmount = 3.71m; line.DiscountAmount = 0;
            var sale = new SalesTransaction { SalesTransactionId = Guid.NewGuid(), UnitPrice = 2.069m,
                Volume = 9.660m, TotalPrice = 20m };
            var parent = new InvoiceLine { InvoiceLineId = Guid.NewGuid(),
                SaleTransactionId = sale.SalesTransactionId, SalesTransaction = sale,
                Volume = 9.660m, TotalPrice = 20m };
            line.InvoiceLineRelations_ChildRelationId.Add(new InvoiceLineRelation {
                InvoiceLineRelationId = Guid.NewGuid(), ParentLineId = parent.InvoiceLineId,
                ChildRelationId = line.InvoiceLineId, InvoiceLine_ParentLineId = parent,
                InvoiceLine_ChildRelationId = line
            });
            var wire = JObject.Parse(Encoding.UTF8.GetString(s.Prepare(s.Document,Scope()).Payload))
                ["Doc"]["TransactionLines"][0];
            Equal(1.984m,(decimal)wire["ItemAmount"]);
            Equal(9.660m,(decimal)wire["SaleQty"]);
            Equal(15.46m,(decimal)wire["NetAmount"]);
            Equal(19.17m,(decimal)wire["GrossAmount"]);
        });
        foreach (string raw in new[] { "broken", "{}", "{\"ResultCode\":\"Error,declined\"}", "{\"ResultCode\":\"ΔΦΣΣ\"}",
            "{\"ResultCode\":\"ΔΦΣΣ,-\"}", "{\"ResultCode\":\"ΔΦΣΣ,Sign Device A\"}", "{\"ResultCode\":\"prefix ΔΦΣΣ,SIGN\"}",
            "{\"JobType\":\"Other\",\"ResultCode\":\"ΔΦΣΣ,SIGN\"}", "{\"InvoiceUID\":\"wrong\",\"ResultCode\":\"ΔΦΣΣ,SIGN\"}" })
            Test("uncertain reply is retained and never re-signed: " + raw, () => {
                var s = new Source(); var h = new Http(s.Document) { Raw = raw }; var service = Service(h);
                var result = service.Sign(s.Document, Scope(), s); Equal(FiscalSigningState.Unknown, result.State); Equal(raw,result.Result.Response);
                Equal(FiscalSigningState.Unknown, service.Sign(s.Document,Scope("new-device"),null,true).State); Equal(1,h.Calls); Equal(1,s.Loads);
            });
        Test("a signature without a QR remains signed", () => {
            var s = new Source(); var h = new Http(s.Document) { Raw = "{\"ResultCode\":\"ΔΦΣΣ,SIGN\"}" };
            var result = Service(h).Sign(s.Document,Scope(),s); Equal(FiscalSigningState.Signed,result.State); Equal<string>(null,result.Result.QrCode);
        });
        Test("matching explicit Samtec error is rejected and permits a corrected retry", () => {
            var s = new Source(); var h = new Http(s.Document) { Raw = "{\"JobType\":\"Sign\",\"ResultCode\":\"Error019,Success,ReplyCodeError,137,89,Amounts of JSON transaction line does not match\",\"InvoiceUID\":\"" + s.Document.DocumentId + "\"}" };
            var service = Service(h); var rejected = service.Sign(s.Document,Scope(),s);
            Equal(FiscalSigningState.Rejected,rejected.State); Equal(1,h.Calls);
            Equal(0,h.Lookups);
            h.Raw = "{\"JobType\":\"Sign\",\"InvoiceUID\":\"" + s.Document.DocumentId + "\",\"ResultCode\":\"ΔΦΣΣ,SIGNATURE\"}";
            Equal(FiscalSigningState.Signed,service.Sign(s.Document,Scope(),s,true).State);
            Equal(2,h.Calls); Equal(0,h.Lookups); Equal(2,s.Loads);
        });
        Test("Error019 recovers the signed invoice through GetLastInvoiceInfo", () =>
        {
            var s = new Source();
            string invoiceUid = s.Document.DocumentId.ToString();
            var h = new Http(s.Document)
            {
                Raw = "{\"JobType\":\"Sign\",\"InvoiceUID\":\"" +
                    invoiceUid + "\",\"ResultCode\":\"Error019,Success," +
                    "ReplyCodeError,Wrong JSON Invoice UID,93/D0/02\"}",
                LookupRaw = "{\"JobType\":\"GetLastInvoiceInfo\"," +
                    "\"InvoiceUID\":\"" + invoiceUid + "\"," +
                    "\"ResultCode\":\"ΑΑΠΤΠ,RECOVERED-SIGNATURE," +
                    "https://example.invalid/recovered\"}"
            };
            FiscalSigningAttempt result = Service(h).Sign(s.Document, Scope(),
                s);
            Equal(FiscalSigningState.Signed, result.State);
            Equal("RECOVERED-SIGNATURE", result.Result.Signature);
            Equal("https://example.invalid/recovered", result.Result.QrCode);
            Equal(1, h.Calls);
            Equal(1, h.Lookups);
            var lookup = JObject.Parse(Encoding.UTF8.GetString(h.LookupBytes));
            Equal("GetLastInvoiceInfo", (string)lookup["JobType"]);
            Equal(invoiceUid, (string)lookup["JobParams"]);
        });
        Test("Wrong JSON Invoice UID with another code does not query history", () =>
        {
            var s = new Source();
            string invoiceUid = s.Document.DocumentId.ToString();
            var h = new Http(s.Document)
            {
                Raw = "{\"JobType\":\"Sign\",\"InvoiceUID\":\"" +
                    invoiceUid + "\",\"ResultCode\":\"Error019,Success," +
                    "ReplyCodeError,Wrong JSON Invoice UID,93/D0/00\"}"
            };
            FiscalSigningAttempt result = Service(h).Sign(s.Document, Scope(),
                s);
            Equal(FiscalSigningState.Rejected, result.State);
            Equal(1, h.Calls);
            Equal(0, h.Lookups);
        });
        Test("saved Wrong JSON Invoice UID is recovered without a new Sign", () =>
        {
            var s = new Source();
            string invoiceUid = s.Document.DocumentId.ToString();
            FiscalSigningRequest original = s.Prepare(s.Document, Scope());
            string response = "{\"JobType\":\"Sign\",\"InvoiceUID\":\"" +
                invoiceUid + "\",\"ResultCode\":\"Error019,Success," +
                "ReplyCodeError,Wrong JSON Invoice UID,93/D0/02\"}";
            var store = new SqlFiscalSigningStore(database);
            using (var lease = store.Acquire(s.Document))
            {
                FiscalSigningAttempt prepared = store.Begin(lease, original,
                    DateTime.UtcNow);
                var signing = new FiscalSigningAttempt(original,
                    FiscalSigningState.Signing, 1, prepared.CreatedUtc,
                    prepared.CreatedUtc);
                store.Save(lease, 0, signing);
                FiscalSigningResult rejectedResult =
                    FiscalSigningResult.Rejected(original.AttemptId, response,
                        "saved rejection");
                var rejected = new FiscalSigningAttempt(original,
                    FiscalSigningState.Rejected, 2, prepared.CreatedUtc,
                    prepared.CreatedUtc, rejectedResult);
                store.Save(lease, 1, rejected);
            }
            var h = new Http(s.Document)
            {
                LookupRaw = "{\"JobType\":\"GetLastInvoiceInfo\"," +
                    "\"InvoiceUID\":\"" + invoiceUid + "\"," +
                    "\"ResultCode\":\"ΑΑΠΤΠ,RECOVERED-SIGNATURE," +
                    "https://example.invalid/recovered\"}"
            };
            FiscalSigningAttempt recovered = Service(h).Recover(s.Document);
            Equal(FiscalSigningState.Signed, recovered.State);
            Equal("RECOVERED-SIGNATURE", recovered.Result.Signature);
            Equal(0, h.Calls);
            Equal(1, h.Lookups);
        });
        Test("legacy Unknown Error019 is recovered locally and retried with the effective item amount", () => {
            var s = new Source();
            s.Invoice.TotalAmount = 753.39m; s.Invoice.VatAmount = 145.82m; s.Invoice.NettoAmount = 619.98m;
            var line = s.Invoice.InvoiceLines[0]; line.Volume = 369.780m; line.TotalPrice = 753.39m;
            line.VatAmount = 145.82m; line.UnitPrice = line.UnitPriceRetail = 2.079m;
            line.UnitPriceWhole = 1.67661m; line.DiscountAmount = 15.38m;
            var prepared = s.Prepare(s.Document,Scope());
            var original = new FiscalSigningRequest(prepared.AttemptId,prepared.Document,prepared.Scope,prepared.PayloadFormat,
                prepared.Payload);
            string response = "{\"JobType\":\"Sign\",\"ResultCode\":\"Error019,Success,ReplyCodeError,137,89,Amounts of JSON transaction line does not match\",\"InvoiceUID\":\"" + s.Document.DocumentId + "\"}";
            var store = new SqlFiscalSigningStore(database);
            using (var lease = store.Acquire(s.Document))
            {
                var attempt = store.Begin(lease,original,DateTime.UtcNow);
                var signing = new FiscalSigningAttempt(original,FiscalSigningState.Signing,1,attempt.CreatedUtc,attempt.CreatedUtc);
                store.Save(lease,0,signing);
                store.Save(lease,1,new FiscalSigningAttempt(original,FiscalSigningState.Unknown,2,attempt.CreatedUtc,attempt.CreatedUtc,
                    FiscalSigningResult.Unknown(original.AttemptId,response,"legacy classification")));
            }
            var h = new Http(s.Document);
            Equal(FiscalSigningState.Rejected,Service(h).Recover(s.Document).State); Equal(0,h.Calls);
            Equal(FiscalSigningState.Signed,Service(h).Sign(s.Document,Scope(),s,true).State); Equal(1,h.Calls);
            Equal(2.03740m,(decimal)JObject.Parse(Encoding.UTF8.GetString(h.Bytes))["Doc"]["TransactionLines"][0]["ItemAmount"]);
        });
        Test("legacy timeout Unknown resumes its saved request after restart", () => {
            var s = new Source(); var original = s.Prepare(s.Document,Scope());
            var store = new SqlFiscalSigningStore(database);
            using (var lease = store.Acquire(s.Document))
            {
                var prepared = store.Begin(lease,original,DateTime.UtcNow);
                var signing = new FiscalSigningAttempt(original,FiscalSigningState.Signing,1,
                    prepared.CreatedUtc,prepared.CreatedUtc);
                store.Save(lease,0,signing);
                store.Save(lease,1,new FiscalSigningAttempt(original,FiscalSigningState.Unknown,2,
                    prepared.CreatedUtc,prepared.CreatedUtc,FiscalSigningResult.Unknown(original.AttemptId,null,
                        "Unconfirmed device response: HttpRequestException")));
            }
            var h = new Http(s.Document);
            Equal(FiscalSigningState.Signed,Service(h).Sign(s.Document,Scope(),null).State);
            Equal(1,h.Calls); Equal(1,s.Loads);
        });
        Test("legacy transient rejection is reclassified and retried after restart", () => {
            var s = new Source(); var original = s.Prepare(s.Document,Scope());
            string response = "{\"JobType\":\"Sign\",\"InvoiceUID\":\"" + s.Document.DocumentId +
                "\",\"ResultCode\":\"Error019,ReplyCodeError,23,17,Η συσκευή είναι σε Χρήση μενού,17/F0/02\"}";
            var store = new SqlFiscalSigningStore(database);
            using (var lease = store.Acquire(s.Document))
            {
                var prepared = store.Begin(lease,original,DateTime.UtcNow);
                var signing = new FiscalSigningAttempt(original,FiscalSigningState.Signing,1,
                    prepared.CreatedUtc,prepared.CreatedUtc);
                store.Save(lease,0,signing);
                store.Save(lease,1,new FiscalSigningAttempt(original,FiscalSigningState.Rejected,2,
                    prepared.CreatedUtc,prepared.CreatedUtc,FiscalSigningResult.Rejected(original.AttemptId,
                        response,"The device explicitly rejected the matching signing request: Error019")));
            }
            var h = new Http(s.Document);
            Equal(FiscalSigningState.Signed,Service(h).Sign(s.Document,Scope(),null).State);
            Equal(1,h.Calls); Equal(1,s.Loads);
        });
        Test("legacy duplicate rejection becomes operator-visible without another dispatch", () => {
            var s = new Source(); var original = s.Prepare(s.Document,Scope());
            string response = "{\"JobType\":\"Sign\",\"InvoiceUID\":\"" + s.Document.DocumentId +
                "\",\"ResultCode\":\"Error019,Duplicate InvoiceUID already signed\"}";
            var store = new SqlFiscalSigningStore(database);
            using (var lease = store.Acquire(s.Document))
            {
                var prepared = store.Begin(lease,original,DateTime.UtcNow);
                var signing = new FiscalSigningAttempt(original,FiscalSigningState.Signing,1,
                    prepared.CreatedUtc,prepared.CreatedUtc);
                store.Save(lease,0,signing);
                store.Save(lease,1,new FiscalSigningAttempt(original,FiscalSigningState.Rejected,2,
                    prepared.CreatedUtc,prepared.CreatedUtc,FiscalSigningResult.Rejected(original.AttemptId,
                        response,"legacy duplicate classification")));
            }
            var h = new Http(s.Document); var result = Service(h).Sign(s.Document,Scope(),null);
            Equal(FiscalSigningState.Unknown,result.State); Equal(0,h.Calls);
            Check(result.Result.Diagnostic.Contains("operator recovery"),"operator notification was lost");
        });
        Test("transient HTTP errors retry the persisted request and then remain unknown", () => {
            var s = new Source(); var h = new Http(s.Document);
            h.Respond = () => Reply(h.Raw,500);
            var result = Service(h).Sign(s.Document,Scope(),s); Equal(FiscalSigningState.Unknown,result.State);
            Equal(h.Raw,result.Result.Response); Equal(3,h.Calls); Equal(1,s.Loads);
            Equal(FiscalSigningState.Unknown,Service(h).Sign(s.Document,Scope(),null,true).State); Equal(3,h.Calls);
        });
        Test("HTTP 404 retries the exact persisted signing request", () => {
            var s = new Source(); var h = new Http(s.Document); int calls = 0;
            h.Respond = () => ++calls < 2 ? Reply("missing",404) : Reply(h.Raw);
            Equal(FiscalSigningState.Signed,Service(h).Sign(s.Document,Scope(),s).State);
            Equal(2,h.Calls); Equal(1,s.Loads);
            Equal(1,History(s.Document).Select(item => item.Request.PayloadSha256)
                .Distinct().Count());
        });
        Test("transport failures retry exact saved bytes and can recover", () => {
            var s = new Source(); var h = new Http(s.Document); int calls = 0;
            h.Respond = () => { if (++calls < 3) throw new HttpRequestException("synthetic timeout"); return Reply(h.Raw); };
            Equal(FiscalSigningState.Signed,Service(h).Sign(s.Document,Scope(),s).State);
            Equal(3,h.Calls); Equal(1,s.Loads);
            var history = History(s.Document);
            Equal(3,history.Count(item => item.State == FiscalSigningState.Signing));
            Equal(1,history.Select(item => item.Request.PayloadSha256).Distinct().Count());
        });
        Test("persistent timeout stops after the bounded automatic retry policy", () => {
            var s = new Source(); var h = new Http(s.Document) { Respond = () => { throw new HttpRequestException("synthetic timeout"); } };
            Equal(FiscalSigningState.Unknown,Service(h).Sign(s.Document,Scope(),s).State);
            Equal(FiscalSigningState.Unknown,Service(h).Sign(s.Document,Scope("changed"),null,true).State); Equal(3,h.Calls);
        });
        Test("exhausted Samtec request retries every five minutes until confirmed", () => {
            var s = new Source(); var h = new Http(s.Document) {
                Respond = () => { throw new HttpRequestException("synthetic timeout"); }
            };
            var service = Service(h);
            var first = service.Sign(s.Document,Scope(),s);
            Equal(FiscalSigningState.Unknown,first.State); Equal(3,h.Calls);
            Equal(FiscalSigningState.Unknown,service.Sign(s.Document,Scope(),null).State);
            Equal(3,h.Calls);

            var queue = new SqlFiscalSigningStore(database);
            Equal(false,queue.LoadDeferredSamtecInvoiceRetries(DateTime.UtcNow.AddMinutes(-5),1000)
                .Contains(s.Document.DocumentId));
            AgeCurrentSigningRevision(s.Document.DocumentId);
            Check(queue.LoadDeferredSamtecInvoiceRetries(DateTime.UtcNow.AddMinutes(-5),1000)
                .Contains(s.Document.DocumentId),"due signing retry was not queued");
            Equal(FiscalSigningState.Unknown,service.Sign(s.Document,Scope(),null).State);
            Equal(4,h.Calls);

            // A failed deferred dispatch gets another five-minute window; it is
            // not converted to a terminal failure after the first queue retry.
            Equal(FiscalSigningState.Unknown,service.Sign(s.Document,Scope(),null).State);
            Equal(4,h.Calls);
            AgeCurrentSigningRevision(s.Document.DocumentId);
            h.Respond = null;
            var signed = service.Sign(s.Document,Scope(),null);
            Equal(FiscalSigningState.Signed,signed.State); Equal(5,h.Calls);
            Equal(first.Request.PayloadSha256,signed.Request.PayloadSha256);
        });
        Test("deferred Samtec queue is bounded and oldest first", () => {
            var older = new Source(); var newer = new Source();
            var olderHttp = new Http(older.Document) { Respond = () => { throw new HttpRequestException("older timeout"); } };
            var newerHttp = new Http(newer.Document) { Respond = () => { throw new HttpRequestException("newer timeout"); } };
            Equal(FiscalSigningState.Unknown,Service(olderHttp).Sign(older.Document,Scope(),older).State);
            Equal(FiscalSigningState.Unknown,Service(newerHttp).Sign(newer.Document,Scope(),newer).State);
            AgeCurrentSigningRevision(older.Document.DocumentId,7);
            AgeCurrentSigningRevision(newer.Document.DocumentId,6);
            var queue = new SqlFiscalSigningStore(database);
            var due = DateTime.UtcNow.AddMinutes(-5);
            Check(queue.CountDeferredSamtecInvoiceRetries(due) >= 2,"due queue count omitted signing attempts");
            var first = queue.LoadDeferredSamtecInvoiceRetries(due,1);
            Equal(1,first.Count); Equal(older.Document.DocumentId,first[0]);
        });
        Test("interrupted Signing retries the persisted request without preparing again", () => {
            var s = new Source(); var store = new SqlFiscalSigningStore(database);
            using (var lease = store.Acquire(s.Document)) { var a = store.Begin(lease,s.Prepare(s.Document,Scope()),DateTime.UtcNow);
                store.Save(lease,0,new FiscalSigningAttempt(a.Request,FiscalSigningState.Signing,1,a.CreatedUtc,a.CreatedUtc)); }
            var h = new Http(s.Document); Equal(FiscalSigningState.Unknown,Service(h).Recover(s.Document).State); Equal(0,h.Calls);
            Equal(FiscalSigningState.Signed,Service(h).Sign(s.Document,Scope(),null,true).State); Equal(1,h.Calls); Equal(1,s.Loads);
        });
        Test("transient Samtec menu rejection retries the saved request", () => {
            var s = new Source(); var h = new Http(s.Document); int calls = 0;
            h.Respond = () => ++calls == 1
                ? Reply("{\"JobType\":\"Sign\",\"InvoiceUID\":\"" + s.Document.DocumentId + "\",\"ResultCode\":\"Error019,ReplyCodeError,23,17,Η συσκευή είναι σε Χρήση μενού,17/F0/02\"}")
                : Reply(h.Raw);
            Equal(FiscalSigningState.Signed,Service(h).Sign(s.Document,Scope(),s).State);
            Equal(2,h.Calls); Equal(1,s.Loads);
        });
        Test("duplicate InvoiceUID stops retries and requires signature recovery", () => {
            var s = new Source(); var h = new Http(s.Document); int calls = 0;
            h.Respond = () => { if (++calls == 1) throw new HttpRequestException("lost response");
                return Reply("{\"JobType\":\"Sign\",\"InvoiceUID\":\"" + s.Document.DocumentId + "\",\"ResultCode\":\"Error019,Duplicate InvoiceUID already signed\"}"); };
            var result = Service(h).Sign(s.Document,Scope(),s);
            Equal(FiscalSigningState.Unknown,result.State); Equal(2,h.Calls);
            Check(result.Result.Diagnostic.Contains("evidence that the original request was signed"),"duplicate evidence was lost");
            Equal(FiscalSigningState.Unknown,Service(h).Sign(s.Document,Scope(),null,true).State); Equal(2,h.Calls);
        });
        Test("duplicate InvoiceUID response recovers an included original signature", () => {
            var s = new Source(); var h = new Http(s.Document); int calls = 0;
            h.Respond = () => { if (++calls == 1) throw new HttpRequestException("lost response");
                return Reply("{\"JobType\":\"Sign\",\"InvoiceUID\":\"" + s.Document.DocumentId + "\",\"ResultCode\":\"Error019,Duplicate InvoiceUID already signed\",\"Signature\":\"ORIGINAL\",\"QrCode\":\"https://example.invalid/original\"}"); };
            var result = Service(h).Sign(s.Document,Scope(),s);
            Equal(FiscalSigningState.Signed,result.State); Equal("ORIGINAL",result.Result.Signature); Equal(2,h.Calls);
        });
        Test("Prepared requires explicit resume and sends original rendered bytes", () => {
            var s = new Source(); var store = new SqlFiscalSigningStore(database); var original = s.Prepare(s.Document,Scope());
            using (var lease = store.Acquire(s.Document)) store.Begin(lease,original,DateTime.UtcNow);
            s.Invoice.Number = 999; var h = new Http(s.Document); var service = Service(h);
            Equal(FiscalSigningState.Prepared,service.Sign(s.Document,Scope("changed"),s).State); Equal(0,h.Calls);
            Equal(FiscalSigningState.Signed,service.ResumePrepared(s.Document).State);
            Check(original.Payload.SequenceEqual(h.Bytes),"Prepared bytes were rebuilt"); Equal(1,s.Loads);
            Throws(() => service.ResumePrepared(s.Document)); Equal(1,h.Calls);
        });
        Test("local device mismatch is durably failed before dispatch; explicit same-scope retry works", () => {
            var s = new Source(); var h = new Http(s.Document);
            var wrong = new FiscalSigningService(database, scope => new SamtecFiscalSigner(new SamtecDeviceProfile(Scope("wrong"),new Uri("http://device.invalid/")),h));
            Equal(FiscalSigningState.FailedBeforeDispatch,wrong.Sign(s.Document,Scope(),s).State); Equal(0,h.Calls);
            Equal(FiscalSigningState.FailedBeforeDispatch,Service(h).Sign(s.Document,Scope(),s).State);
            Throws(() => Service(h).Sign(s.Document,Scope("changed"),s,true)); Equal(0,h.Calls);
            Equal(FiscalSigningState.Signed,Service(h).Sign(s.Document,Scope(),s,true).State); Equal(1,h.Calls);
        });
        foreach (string stage in new[] { "Prepared", "Signing", "Signed" })
            Test("SQL failure saving " + stage + " never reports success or permits duplicate signing", () => {
                var s = new Source(); var h = new Http(s.Document);
                Execute(connection,"CREATE TRIGGER dbo.TestSigningFailure ON dbo.FiscalAttemptRevision AFTER INSERT AS IF EXISTS (SELECT 1 FROM inserted WHERE State='" + stage + "') BEGIN RAISERROR('synthetic persistence failure',16,1); ROLLBACK TRANSACTION; END;");
                try { Throws(() => Service(h).Sign(s.Document,Scope(),s)); }
                finally { Execute(connection,"DROP TRIGGER dbo.TestSigningFailure;"); }
                Equal(stage == "Signed" ? 1 : 0,h.Calls);
                if (stage == "Signed") { Equal(FiscalSigningState.Unknown,Service(h).Recover(s.Document).State); Equal(1,h.Calls); }
                if (stage == "Signing") Equal(FiscalSigningState.Prepared,Service(h).Sign(s.Document,Scope(),s).State);
            });
        foreach (int? payment in new int?[] { null, 99 }) Test("unsupported payment " + payment + " cannot trigger POS", () => {
            var s = new Source(); s.Invoice.PaymentType = payment; var h = new Http(s.Document);
            Throws(() => Service(h).Sign(s.Document,Scope(),s)); Equal(0,h.Calls); Equal(0,Attempts(s.Document).Length);
        });
        foreach (int payment in new[] { 2,4 }) Test("Samtec payment " + payment + " requires a saved terminal before dispatch", () => {
            var s = new Source(); s.Invoice.PaymentType = payment; s.EftPosTID = null; var h = new Http(s.Document);
            Throws(() => Service(h).Sign(s.Document,Scope(),s)); Equal(0,h.Calls); Equal(0,Attempts(s.Document).Length);
        });
        Test("Samtec integrated POS terminal is frozen with the device profile", () => {
            var s = new Source(); s.Invoice.PaymentType = 2; s.EftPosTID = "OTHER"; var h = new Http(s.Document);
            Equal(FiscalSigningState.FailedBeforeDispatch,Service(h).Sign(s.Document,Scope(),s).State);
            Equal(0,h.Calls);
        });
        foreach (string field in new[] { "printed", "signature", "qr", "submission", "balance-date", "balance-sign", "filling-sign" })
            Test("legacy history gate " + field, () => {
                var s = new Source(field.StartsWith("balance") ? FiscalDocumentKind.Balance : field == "filling-sign" ? FiscalDocumentKind.TankFilling : FiscalDocumentKind.Invoice);
                if (field == "printed") s.Invoice.IsPrinted = true;
                if (field == "signature") s.Invoice.InvoiceSignature = "-";
                if (field == "qr") s.Invoice.QRCodeData = "old";
                if (field == "submission") s.Legacy = true;
                if (field == "balance-date") s.Balance.PrintDate = DateTime.Now;
                if (field == "balance-sign") s.Balance.DocumentSign = "-";
                if (field == "filling-sign") s.Filling.SignSignature = "-";
                var h = new Http(s.Document); Throws(() => Service(h).Sign(s.Document,Scope(),s)); Equal(0,h.Calls);
            });
        Test("existing submission blocks a fresh signing route", () => {
            var s = new Source(); var store = new SqlMyDataSubmissionStore(database);
            using (store.Acquire("old",s.Document.DocumentId)) store.Save(new MyDataSubmission { AttemptId = Guid.NewGuid(),InvoiceId = s.Document.DocumentId, Scope = "old",RequestXml = "<invoice/>",State = "Sending",StartedUtc = DateTimeOffset.UtcNow });
            var h = new Http(s.Document); Throws(() => Service(h).Sign(s.Document,Scope(),s)); Equal(0,s.Loads); Equal(0,h.Calls);
        });
        foreach (var kind in new[] { FiscalDocumentKind.Alert, FiscalDocumentKind.Titrimetry }) Test(kind + " cannot fabricate a signature", () => {
            var doc = new FiscalDocumentReference(kind,Guid.NewGuid());
            Throws(() => SamtecFiscalSigner.Prepare(doc,Scope(),SamtecRequestBuilder.Create(doc,"text",501)));
        });
        Test("cancellation and invalid tank-filling type fail before transport", () => {
            var s = new Source(); s.Invoice.InvoiceType.OfficialEnumerator = 215; var h = new Http(s.Document);
            Throws(() => Service(h).Sign(s.Document,Scope(),s)); Equal(0,h.Calls);
            Throws(() => HostFiscalSigningMapper.TankFilling(s.Filling,Scope(),"text",999));
        });
        Test("raw payload cannot enable POS or printing", () => {
            var s = new Source(); var original = s.Prepare(s.Document,Scope());
            foreach (string change in new[] { "pos", "print", "token", "uid", "extra" }) {
                var wire = JObject.Parse(Encoding.UTF8.GetString(original.Payload));
                if (change == "pos") wire["Doc"]["Payments"][0]["Type"] = 1;
                if (change == "print") wire["Doc"]["InvoiceDetails"]["PrintDevice"] = 1;
                if (change == "token") wire["Doc"]["InvoiceDetails"]["ReqForToken"] = 1;
                if (change == "uid") wire["Doc"]["InvoiceDetails"]["InvoiceUID"] = Guid.NewGuid().ToString();
                if (change == "extra") wire["DeviceCommand"] = "other";
                var request = new FiscalSigningRequest(Guid.NewGuid(),s.Document,Scope(),SamtecFiscalSigner.Format,Encoding.UTF8.GetBytes(wire.ToString()));
                using(var signer = new SamtecFiscalSigner(new SamtecDeviceProfile(Scope(),new Uri("http://device.invalid/")),new Http(s.Document))) Throws(() => signer.Validate(request));
            }
        });
        Test("concurrent workers cannot prepare or dispatch twice", () => {
            var s = new Source(); var h = new Http(s.Document);
            using(var entered = new ManualResetEventSlim()) using(var release = new ManualResetEventSlim()) {
                h.Respond = () => { entered.Set(); Check(release.Wait(10000),"release timeout"); return Reply(h.Raw); };
                var running = Task.Run(() => Service(h).Sign(s.Document,Scope(),s));
                try { Check(entered.Wait(10000),"sender did not enter"); Throws(() => Service(h).Sign(s.Document,Scope(),s)); }
                finally { release.Set(); }
                Equal(FiscalSigningState.Signed,running.GetAwaiter().GetResult().State); Equal(1,h.Calls); Equal(1,s.Loads);
            }
        });
        Test("optional read-only recovery saves confirmed outcome", () => {
            var s = new Source(); var h = new Http(s.Document) { Raw = "{}" }; Service(h).Sign(s.Document,Scope(),s);
            var signer = new Recovering(); var coordinator = new FiscalSigningCoordinator(new SqlFiscalSigningStore(database),scope => signer);
            Equal(FiscalSigningState.Signed,coordinator.Recover(s.Document).State); Equal(1,signer.Lookups); Equal(0,signer.Signs);
        });
        Test("recovery save failure propagates without swallowing or a second save", () => {
            var s = new Source(); Service(new Http(s.Document) { Raw = "{}" }).Sign(s.Document,Scope(),s);
            var signer = new Recovering(); var coordinator = new FiscalSigningCoordinator(new SqlFiscalSigningStore(database),scope => signer);
            Execute(connection,"CREATE TRIGGER dbo.TestRecoveryFailure ON dbo.FiscalAttemptRevision AFTER INSERT AS IF EXISTS (SELECT 1 FROM inserted WHERE State='Signed') BEGIN RAISERROR('synthetic persistence failure',16,1); ROLLBACK TRANSACTION; END;");
            try { Throws(() => coordinator.Recover(s.Document)); }
            finally { Execute(connection,"DROP TRIGGER dbo.TestRecoveryFailure;"); }
            Equal(FiscalSigningState.Unknown,Attempts(s.Document).Last().State); Equal(1,signer.Lookups); Equal(0,signer.Signs);
        });
    }
    sealed class Recovering : IFiscalSigner, IFiscalSignerRecovery
    {
        public int Signs, Lookups;
        public FiscalSignerCapabilities Capabilities => new FiscalSignerCapabilities(SamtecFiscalSigner.Key,new[] { FiscalDocumentKind.Invoice });
        public FiscalSigningResult Sign(FiscalSigningRequest r) { Signs++; throw new Exception("Recovery must not sign"); }
        public FiscalSigningResult Recover(FiscalSigningRequest r) { Lookups++; return FiscalSigningResult.Signed(r.AttemptId,"recovered",null,null,"verified lookup"); }
    }
    sealed class Source : IFiscalSigningSource
    {
        public FiscalDocumentReference Document; public Invoice Invoice; public Balance Balance; public TankFilling Filling;
        public int Loads; public bool Legacy; public string EftPosTID = "POS-TID";
        public Source(FiscalDocumentKind kind = FiscalDocumentKind.Invoice)
        {
            var id = Guid.NewGuid(); Document = new FiscalDocumentReference(kind,id);
            Invoice = new Invoice { InvoiceId = id, Number = 42, Series = "TEST", PaymentType = 1, TotalAmount = 124, VatAmount = 24, NettoAmount = 100,
                InvoiceType = new InvoiceType { OfficialEnumerator = 100, RetailInvoice = true }, Trader = new Trader { Name = "Synthetic",TaxRegistrationNumber = "111111111",Occupation = "Test",Address = "Road",ZipCode = "10000", City = "Athens" } };
            Invoice.InvoiceLines.Add(new InvoiceLine { InvoiceLineId = Guid.NewGuid(),InvoiceId = id,Volume = 50,VolumeNormalized = 50,TotalPrice = 124,VatAmount = 24,VatPercentage = 24,
                UnitPrice = 2.48m,UnitPriceRetail = 2.50m,UnitPriceWhole = 2m,DiscountAmount = 1, FuelType = new FuelType { EnumeratorValue = 30, Name = "Heating diesel" } });
            Balance = new Balance { BalanceId = id }; Filling = new TankFilling { TankFillingId = id };
        }
        public FiscalSigningRequest Prepare(FiscalDocumentReference document,FiscalSigningScope scope)
        {
            Loads++;
            switch(document.Kind) {
                case FiscalDocumentKind.Invoice: return HostFiscalSigningMapper.Invoice(Invoice,scope,"Rendered\r\nΚείμενο",Legacy,EftPosTID);
                case FiscalDocumentKind.Balance: return HostFiscalSigningMapper.Balance(Balance,scope,"Balance\r\nΚείμενο");
                default: return HostFiscalSigningMapper.TankFilling(Filling,scope,"Filling\r\nΚείμενο",40);
            }
        }
    }
    sealed class Http : HttpMessageHandler
    {
        public FiscalDocumentReference Document; public int Calls, Lookups; public byte[] Bytes, LookupBytes; public int Status = 200;
        public string Raw, LookupRaw; public Func<HttpResponseMessage> Respond;
        public Http(FiscalDocumentReference document)
        {
            Document = document;
            Raw = "{\"JobType\":\"Sign\",\"InvoiceUID\":\"" + document.DocumentId + "\",\"ResultCode\":\"ΔΦΣΣ,SIGNATURE,https://example.invalid/qr\"}";
            LookupRaw = "{\"JobType\":\"GetLastInvoiceInfo\",\"JobParams\":\"" + document.DocumentId + "\",\"ResultCode\":\"Error019,Success,GetLastInvoiceInfoUidMismatch\"}";
        }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            Equal(HttpMethod.Post,request.Method); Equal("device.invalid",request.RequestUri.Host); Equal("text/json",request.Content.Headers.ContentType.MediaType);
            byte[] requestBytes = request.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
            var requestJson = JObject.Parse(Encoding.UTF8.GetString(requestBytes));
            if ((string)requestJson["JobType"] == "GetLastInvoiceInfo")
            {
                Lookups++;
                LookupBytes = requestBytes;
                Equal(Document.DocumentId.ToString(),
                    (string)requestJson["JobParams"]);
                HttpResponseMessage lookupResponse = Reply(LookupRaw, Status);
                return Task.FromResult(lookupResponse);
            }
            Calls++;
            Bytes = requestBytes;
            using(var c = new SqlConnection(connection)) using(var cmd = c.CreateCommand()) {
                c.Open(); cmd.CommandText = "SELECT TOP (1) a.RequestPayload,r.State FROM dbo.FiscalAttempt a JOIN dbo.FiscalAttemptRevision r ON r.AttemptId=a.AttemptId AND r.Revision=a.CurrentRevision WHERE a.DocumentId=@id AND a.Family='Signing' ORDER BY a.AttemptSequence DESC";
                cmd.Parameters.AddWithValue("@id",Document.DocumentId);
                using(var r = cmd.ExecuteReader()) { Check(r.Read(),"No saved signing request"); Check(Bytes.SequenceEqual((byte[])r[0]),"Payload differs from journal"); Equal("Signing",r.GetString(1)); }
            }
            return Task.FromResult(Respond != null ? Respond() : Reply(Raw,Status));
        }
    }
    static FiscalSigningAttempt[] Attempts(FiscalDocumentReference doc) { var store = new SqlFiscalSigningStore(database); using(var lease = store.Acquire(doc)) return store.Load(lease).ToArray(); }
    static FiscalSigningAttempt[] History(FiscalDocumentReference doc) { var store = new SqlFiscalSigningStore(database); using(var lease = store.Acquire(doc)) return store.LoadHistory(lease,store.Load(lease).Last().Request.AttemptId).ToArray(); }
    static HttpResponseMessage Reply(string text,int status = 200) => new HttpResponseMessage((HttpStatusCode)status) { Content = new StringContent(text,Encoding.UTF8) };
    static void Execute(string cs,string sql) { using(var c = new SqlConnection(cs)) using(var cmd = c.CreateCommand()) { c.Open(); cmd.CommandText = sql; cmd.ExecuteNonQuery(); } }
    static void AgeCurrentSigningRevision(Guid documentId, int minutes = 6)
    {
        Execute(connection,"UPDATE dbo.FiscalAttempt SET CreatedUtc=DATEADD(minute,-10,CreatedUtc) " +
            "WHERE DocumentId='" + documentId + "' AND Family='Signing'; " +
            "UPDATE r SET RecordedUtc=DATEADD(minute,-" + minutes + ",GETUTCDATE()) " +
            "FROM dbo.FiscalAttempt a JOIN dbo.FiscalAttemptRevision r ON r.AttemptId=a.AttemptId AND r.Revision=a.CurrentRevision " +
            "WHERE a.DocumentId='" + documentId + "' AND a.Family='Signing';");
    }
    static void Throws(Action action) { try { action(); } catch(InvalidOperationException) { return; } catch(SqlException) { return; } catch(ArgumentException) { return; } catch(NotSupportedException) { return; } catch(Newtonsoft.Json.JsonException) { return; } throw new Exception("Expected rejection"); }
    static JToken ExpectedLegacyWire(string json)
    {
        var wire = JToken.Parse(json);
        var lines = wire["Doc"]?["TransactionLines"] as JArray;
        if (lines != null)
            for (int index = 0; index < lines.Count; index++)
            {
                var line = lines[index];
                var allowed = typeof(Transactionline).GetProperties()
                    .Select(property => property.Name).ToArray();
                foreach (var property in ((JObject)line).Properties().ToArray())
                    if (!allowed.Contains(property.Name)) property.Remove();
                line["LineNo"] = index + 1;
                int fuelCode = (int)line["FuelCode"];
                line["Code"] = fuelCode == 0 ? JValue.CreateNull() : new JValue(fuelCode.ToString(CultureInfo.InvariantCulture));
                decimal itemAmount = decimal.Round((decimal)line["ItemAmount"],5,
                    MidpointRounding.ToEven);
                decimal quantity = (decimal)line["SaleQty"];
                decimal gross = Math.Abs((decimal)line["GrossAmount"]);
                if (decimal.Round(itemAmount * quantity,2,MidpointRounding.ToEven) != gross)
                    itemAmount = SamtecFiscalSigner.EffectiveVatInclusiveItemAmount(
                        gross,quantity);
                line["ItemAmount"] = itemAmount;
            }
        return wire;
    }
    static void Equal<T>(T expected,T actual) { if (!object.Equals(expected,actual)) throw new Exception("Expected " + expected + ", got " + actual); }
    static void Check(bool ok,string why) { if (!ok) throw new Exception(why); }
    static void Test(string name,Action action) { try { action(); passed++; Console.WriteLine("PASS " + name); } catch(Exception e) { failed++; Console.WriteLine("FAIL " + name + ": " + e); } }
}
