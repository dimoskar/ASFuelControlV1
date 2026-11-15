using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using ASFuelControl.Data;
using ASFuelControl.Windows.Threads;

internal static class Program
{
    private static string appOutput;
    private static int checks;

    private static void Main(string[] args)
    {
        appOutput = Path.GetFullPath(args[0]);
        AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
        Run();
    }

    private static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
    {
        var name = new AssemblyName(args.Name);
        string path = Path.Combine(appOutput, name.Name + ".dll");
        return File.Exists(path) ? Assembly.LoadFrom(path) : null;
    }

    private static void Check(bool passed, string description)
    {
        if (!passed)
        {
            throw new Exception(description);
        }
        checks++;
        Console.WriteLine("PASS " + description);
    }

    private static Invoice NewInvoice(bool credit)
    {
        var invoice = new Invoice();
        invoice.InvoiceId = Guid.NewGuid();
        invoice.InvoiceType = new InvoiceType { IsCancelation = credit, Description = "CREDIT NOTE" };
        invoice.Number = 42;
        invoice.Series = "CR";
        invoice.TransactionDate = new DateTime(2026, 11, 15, 12, 30, 0);
        return invoice;
    }

    private static InvoiceLine AddLine(Invoice invoice, Guid fuelId)
    {
        var line = new InvoiceLine();
        line.InvoiceLineId = Guid.NewGuid();
        line.Invoice = invoice;
        line.InvoiceId = invoice.InvoiceId;
        line.FuelTypeId = fuelId;
        line.FuelType = new FuelType { FuelTypeId = fuelId, Name = "DIESEL" };
        line.Volume = 10;
        line.UnitPrice = 1.24m;
        line.TotalPrice = 12.4m;
        line.VatAmount = 2.4m;
        line.VatPercentage = 24;
        invoice.InvoiceLines.Add(line);
        return line;
    }

    private static void AddSale(InvoiceLine line, int pump, int nozzle)
    {
        var dispenser = new Dispenser { OfficialPumpNumber = pump };
        var sourceNozzle = new Nozzle { NozzleId = Guid.NewGuid(), OfficialNozzleNumber = nozzle, Dispenser = dispenser };
        var sale = new SalesTransaction { SalesTransactionId = Guid.NewGuid(), Nozzle = sourceNozzle };
        line.SalesTransaction = sale;
        line.SaleTransactionId = sale.SalesTransactionId;
    }

    private static void LinkLine(InvoiceLine child, InvoiceLine parent)
    {
        var relation = new InvoiceLineRelation { InvoiceLine_ParentLineId = parent };
        child.InvoiceLineRelations_ChildRelationId.Add(relation);
    }

    private static void LinkInvoice(Invoice child, Invoice parent)
    {
        var relation = new InvoiceRelation { ParentInvoice = parent };
        child.ParentInvoiceRelations.Add(relation);
    }

    private static void Run()
    {
        Guid fuel = Guid.NewGuid();
        Invoice normal = NewInvoice(false);
        bool normalCredit = CreditInvoiceThermalData.IsCreditInvoice(normal);
        Check(!normalCredit, "normal invoices remain outside the credit path");
        Invoice credit = NewInvoice(true);
        InvoiceLine creditLine = AddLine(credit, fuel);
        InvoiceLine original = AddLine(normal, fuel);
        AddSale(original, 3, 2);
        original.TotalPrice = 124;
        original.Volume = 100;
        LinkLine(creditLine, original);
        CreditInvoiceThermalRow[] rows = CreditInvoiceThermalData.CreateRows(credit);
        Check(rows.Length == 1 && rows[0].OfficialPumpNumber == 3 && rows[0].OfficialNozzleNumber == 2, "credit without a sale link resolves its parent line's pump/nozzle");
        Check(rows[0].Volume == 10 && rows[0].UnitPrice == 1.24m && rows[0].NettoAmount == 10 && rows[0].VatAmount == 2.4m && rows[0].TotalAmount == 12.4m, "partial credit retains its own fiscal amounts and quantity");
        Check(rows[0].InvoiceId == credit.InvoiceId && rows[0].Number == 42 && rows[0].Series == "CR" && !creditLine.SaleTransactionId.HasValue, "credit identity and original sale ownership are preserved");
        RenderCredit(credit, rows);

        Invoice middle = NewInvoice(false);
        InvoiceLine middleLine = AddLine(middle, fuel);
        LinkLine(middleLine, original);
        creditLine.InvoiceLineRelations_ChildRelationId.Clear();
        LinkLine(creditLine, middleLine);
        rows = CreditInvoiceThermalData.CreateRows(credit);
        Check(rows[0].OfficialPumpNumber == 3, "multi-generation line provenance resolves the original sale");

        creditLine.InvoiceLineRelations_ChildRelationId.Clear();
        LinkInvoice(credit, normal);
        Guid otherFuel = Guid.NewGuid();
        InvoiceLine differentProduct = AddLine(normal, otherFuel);
        AddSale(differentProduct, 8, 1);
        rows = CreditInvoiceThermalData.CreateRows(credit);
        Check(rows[0].OfficialPumpNumber == 3, "invoice-only parent relation matches the credited product");
        InvoiceLine ambiguous = AddLine(normal, fuel);
        AddSale(ambiguous, 9, 4);
        rows = CreditInvoiceThermalData.CreateRows(credit);
        string ambiguousPoint = CreditInvoiceThermalData.SalePointText(rows);
        Check(!rows[0].OfficialPumpNumber.HasValue && ambiguousPoint == "" && rows.Length == 1, "ambiguous parent sales never select an arbitrary pump or omit the credit line");
        LinkLine(creditLine, original);
        rows = CreditInvoiceThermalData.CreateRows(credit);
        Check(rows[0].OfficialPumpNumber == 3, "explicit line relation takes precedence over ambiguous invoice parents");

        AddSale(creditLine, 7, 6);
        rows = CreditInvoiceThermalData.CreateRows(credit);
        Check(rows[0].OfficialPumpNumber == 7, "direct sale takes precedence over parent provenance");
        Invoice standalone = NewInvoice(true);
        InvoiceLine standaloneLine = AddLine(standalone, fuel);
        standaloneLine.VatPercentage = 0;
        standaloneLine.VatAmount = 0;
        rows = CreditInvoiceThermalData.CreateRows(standalone);
        Check(rows.Length == 1 && !rows[0].OfficialNozzleNumber.HasValue && rows[0].VATPercentage == 0, "standalone credit and zero VAT print without requiring a physical sale");
        LinkInvoice(standalone, standalone);
        LinkLine(standaloneLine, standaloneLine);
        rows = CreditInvoiceThermalData.CreateRows(standalone);
        Check(rows.Length == 1 && !rows[0].OfficialPumpNumber.HasValue, "cyclic provenance terminates without losing the line");

        InvoiceLine secondCreditLine = AddLine(credit, otherFuel);
        AddSale(secondCreditLine, 8, 1);
        rows = CreditInvoiceThermalData.CreateRows(credit);
        string multiplePoint = CreditInvoiceThermalData.SalePointText(rows);
        Check(rows.Length == 2 && multiplePoint == "", "multi-pump credit is not labelled with just its first pump");
        bool rejected = false;
        try
        {
            Invoice empty = NewInvoice(true);
            CreditInvoiceThermalData.CreateRows(empty);
        }
        catch (InvalidOperationException)
        {
            rejected = true;
        }
        Check(rejected, "empty credit invoice cannot produce a header-only receipt");
        Console.WriteLine("Passed " + checks + " checks.");
    }

    private static void RenderCredit(Invoice invoice, CreditInvoiceThermalRow[] rows)
    {
        using (var report = new ASFuelControl.Reports.Invoices.InvoiceReportNarrow())
        {
            report.ApplyThermalPrintSafety();
            report.SetFont("Tahoma", 9f);
            report.ApplyThermalTypography("Tahoma", 9f);
            report.DataSource = rows;
            string number = invoice.Number.ToString();
            string salePoint = CreditInvoiceThermalData.SalePointText(rows);
            report.SetCreditInvoiceDetails(invoice.InvoiceType.Description, number, invoice.Series, salePoint);
            report.SetTransactionDate(invoice.TransactionDate);
            foreach (Telerik.Reporting.ReportParameter parameter in report.ReportParameters)
            {
                if (parameter.Value == null)
                {
                    parameter.Value = "Test";
                }
            }
            report.ReportParameters["CompanyName"].Value = "THERMAL CREDIT REGRESSION";
            report.FlowFooterAfterDetails();
            var source = new Telerik.Reporting.InstanceReportSource { ReportDocument = report };
            var processor = new Telerik.Reporting.Processing.ReportProcessor();
            var csv = processor.RenderReport("CSV", source, null);
            string csvText = Encoding.UTF8.GetString(csv.DocumentBytes);
            bool containsLine = csvText.Contains("DIESEL");
            Check(!csv.HasErrors && containsLine, "real Telerik report renders a credit line without SaleTransactionId");
            var settings = new Hashtable { { "OutputFormat", "TIFF" } };
            var image = processor.RenderReport("IMAGE", source, settings);
            Check(!image.HasErrors && image.DocumentBytes.Length > 0, "thermal credit renders as an image");
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "credit-preview.png");
            using (var stream = new MemoryStream(image.DocumentBytes))
            using (var preview = System.Drawing.Image.FromStream(stream))
            {
                preview.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            }
        }
    }
}
