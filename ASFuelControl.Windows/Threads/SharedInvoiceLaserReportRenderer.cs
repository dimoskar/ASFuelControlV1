using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text;
using ASFuelControl.Invoicing;
using Exedron.Invoicing.Core.Models;
using Newtonsoft.Json;
using Telerik.Reporting;
using Telerik.Reporting.Drawing;
using Telerik.Reporting.Processing;

namespace ASFuelControl.Windows.Threads
{
    /// <summary>
    /// Renders invoices with the established laser or thermal invoice reports. The resulting
    /// PNG pages are persisted by FiscalPrintService, so retries never query or
    /// recreate the report after the first handoff attempt.
    /// </summary>
    internal static class SharedInvoiceLaserReportRenderer
    {
        internal static byte[] Render(InvoiceWorkflowSnapshot snapshot, InvoiceResponse response, SharedPrinterProfile profile)
        {
            if (profile == null)
                throw new InvalidOperationException("A printer profile is required for the invoice report.");

            // This also validates that all mandatory provider print evidence is
            // present before a page is frozen in the print operation.
            var fiscalFooter = SharedInvoicePrinting.Footer(snapshot,response);

            using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                var invoice = db.Invoices.Single(i => i.InvoiceId == snapshot.InvoiceId);
                var qrCodeUrl = response == null ? invoice.QRCodeData : response.QrCodeUrl;
                if (profile.Thermal)
                {
                    string fiscalDeviceUrl = snapshot.Signing == null
                        ? null
                        : invoice.QRCodeData;
                    string responseQrCodeUrl = response?.QrCodeUrl;
                    bool responseRepeatsFiscalQr = !string.IsNullOrWhiteSpace(
                            fiscalDeviceUrl) &&
                        string.Equals(fiscalDeviceUrl, responseQrCodeUrl,
                            StringComparison.OrdinalIgnoreCase);
                    string documentUrl = snapshot.Provider == null ||
                        responseRepeatsFiscalQr
                        ? null
                        : responseQrCodeUrl;
                    return RenderThermalReport(db, invoice, fiscalDeviceUrl,
                        documentUrl, profile, fiscalFooter, response);
                }

                if (invoice.InvoiceType.IsInternal == true)
                {
                    using (var report = new Reports.Invoices.InvoiceReport())
                    using (var reportData = new OpenAccessDataSource(db,"InvoiceLines"))
                    {
                        report.DataSource = reportData;
                        ConfigureInternalReport(report,invoice,qrCodeUrl);
                        return RenderPages(report,profile,fiscalFooter);
                    }
                }

                using (var report = new Reports.Invoices.IncoiceReportShort())
                {
                    report.DataSource = db.InvoiceLines.Where(line => line.InvoiceId == invoice.InvoiceId).ToArray();
                    ConfigureShortReport(report,invoice,qrCodeUrl);
                    return RenderPages(report,profile,fiscalFooter);
                }
            }
        }

        private static byte[] RenderThermalReport(Data.DatabaseModel db, Data.Invoice invoice,
            string fiscalDeviceUrl, string documentUrl, SharedPrinterProfile profile,
            string fiscalFooter, InvoiceResponse response)
        {
            using (var report = new Reports.Invoices.InvoiceReportNarrow())
            {
                report.ApplyThermalPrintSafety();
                var options = Data.Implementation.OptionHandler.Instance;
                var fontSize = Math.Max(9m,options.GetDecimalOption("ThermalPrintFontSize",9));
                var fontName = options.GetOption("ThermalPrintFontName","Tahoma");
                report.SetFont(fontName,(float)fontSize);
                report.ApplyThermalTypography(fontName,(float)fontSize);

                var lines = db.InvoicePrintViews.Where(line => line.InvoiceId == invoice.InvoiceId).ToArray();
                foreach (var line in lines)
                {
                    line.InvoiceNettoAmount = invoice.NettoAmount.GetValueOrDefault();
                    line.InvoiceTotalAmount = invoice.TotalAmount.GetValueOrDefault();
                    line.InvoiceVatAmount = invoice.VatAmount.GetValueOrDefault();
                    line.InvoiceDiscountAmount = invoice.DiscountAmount;
                    line.InvoiceNettoAfterDiscountAmount = invoice.NettoAfterDiscount;
                }

                using (var reportData = new OpenAccessDataSource(lines,""))
                {
                    report.DataSource = reportData;
                    ConfigureThermalReport(report, invoice, fiscalDeviceUrl,
                        documentUrl, options);
                    report.ApplySharedThermalLayout(
                        response == null || string.IsNullOrWhiteSpace(response.InvoiceSignature)
                            ? invoice.InvoiceSignature
                            : response.InvoiceSignature,
                        ThermalEvidence(fiscalFooter,response));
                    report.FlowFooterAfterDetails();
                    // Fiscal evidence remains in the established receipt footer,
                    // which now flows after the detail rows on the fixed paper.
                    return RenderPages(report,profile,null);
                }
            }
        }

        private static string ThermalEvidence(string fiscalFooter, InvoiceResponse response)
        {
            var lines = (fiscalFooter ?? "")
                .Split(new[] { "\r\n" },StringSplitOptions.RemoveEmptyEntries)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToList();
            string mark = lines.FirstOrDefault(line => line.StartsWith("MARK:",StringComparison.OrdinalIgnoreCase));
            string uid = lines.FirstOrDefault(line => line.StartsWith("UID:",StringComparison.OrdinalIgnoreCase));
            lines.RemoveAll(line => string.Equals(line,mark,StringComparison.Ordinal) ||
                string.Equals(line,uid,StringComparison.Ordinal) ||
                response != null && !string.IsNullOrWhiteSpace(response.InvoiceSignature) &&
                string.Equals(line,response.InvoiceSignature,StringComparison.Ordinal));
            var ordered = new List<string>();
            if (!string.IsNullOrWhiteSpace(mark)) ordered.Add(mark);
            if (!string.IsNullOrWhiteSpace(uid)) ordered.Add(uid);
            ordered.AddRange(lines);
            return string.Join("\r\n",ordered);
        }

        private static void ConfigureThermalReport(Reports.Invoices.InvoiceReportNarrow report,
            Data.Invoice invoice, string fiscalDeviceUrl, string documentUrl,
            Data.Implementation.OptionHandler options)
        {
            report.ReportParameters["InvoiceId"].Value = invoice.InvoiceId.ToString().ToLowerInvariant();
            report.ReportParameters["PrintBarcode"].Value = options.GetBoolOption("PrintInvoiceBarcode",false);
            report.ReportParameters["CompanyName"].Value = options.GetOption("CompanyName");
            report.ReportParameters["Occupation"].Value = options.GetOption("CompanyOccupation");
            report.ReportParameters["Address"].Value = options.GetOption("CompanyAddress") + " " + options.GetOption("CompanyCity");
            report.ReportParameters["City"].Value = options.GetOption("CompanyCity");
            report.ReportParameters["VATNumber"].Value = options.GetOption("CompanyTIN");
            report.ReportParameters["TaxOffice"].Value = options.GetOption("CompanyTaxOffice");
            report.ReportParameters["Phone"].Value = options.GetOption("CompanyPhone");
            report.ReportParameters["Fax"].Value = options.GetOption("CompanyFax");
            report.ReportParameters["MainAddress"].Value = options.GetOption("CompanyMainAddress");
            report.ReportParameters["efk"].Value = options.GetOption("CompanyEFK") ?? "";
            report.SetTransactionDate(invoice.TransactionDate);

            // SetSupplyNumber contains legacy VAT-exemption display logic which
            // changes persisted line objects. Rendering must never change invoice
            // values, so restore the prices immediately after report setup.
            var prices = invoice.InvoiceLines
                .Select(line => new { Line = line, UnitPrice = line.UnitPrice })
                .ToArray();
            try
            {
                report.SetSupplyNumber(invoice, documentUrl);
                report.CompactQrCodes(fiscalDeviceUrl, documentUrl);
            }
            finally
            {
                foreach (var price in prices)
                    price.Line.UnitPrice = price.UnitPrice;
            }
        }

        private static void ConfigureInternalReport(Reports.Invoices.InvoiceReport report, Data.Invoice invoice, string qrCodeUrl)
        {
            var options = Data.Implementation.OptionHandler.Instance;
            report.ReportParameters["InvoiceId"].Value = invoice.InvoiceId.ToString().ToLowerInvariant();
            report.ReportParameters["CompanyName"].Value = options.GetOption("CompanyName");
            report.ReportParameters["CompanyAddress"].Value = options.GetOption("CompanyAddress");
            report.ReportParameters["CompanyCity"].Value = options.GetOption("CompanyPostalCode") + " " + options.GetOption("CompanyCity");
            report.ReportParameters["CompanyVATNumber"].Value = options.GetOption("CompanyTIN");
            report.ReportParameters["CompanyVATOffice"].Value = options.GetOption("CompanyTaxOffice");
            report.ReportParameters["CompanyOccupation"].Value = options.GetOption("CompanyOccupation");
            report.ReportParameters["CompanyReferenceNumber"].Value = options.GetOption("CompanyReferenceNumber");
            report.ReportParameters["ShowAdditionalData"].Value = true;
            report.ReportParameters["SignLine"].Value = "";
            report.ReportParameters["CompanyEfk"].Value = options.GetOption("CompanyEFK");
            report.SetSupplyNumber(invoice,qrCodeUrl);
        }

        private static void ConfigureShortReport(Reports.Invoices.IncoiceReportShort report, Data.Invoice invoice, string qrCodeUrl)
        {
            report.ReportParameters["ReplaceParameter"].Value = invoice.Notes ?? "";
            var firstLine = invoice.InvoiceLines.FirstOrDefault();
            var vat = firstLine == null ? 0 : firstLine.VatPercentage;
            if (vat == 0 && invoice.VatAmount > 0)
                vat = Data.Implementation.OptionHandler.Instance.GetDecimalOption("VATValue",23m);
            report.ReportParameters["VATValue"].Value = vat;
            report.SetSupplyNumber(invoice,qrCodeUrl);
            report.SetRestAmounts(invoice);
        }

        internal static byte[] RenderPages(Telerik.Reporting.Report report, SharedPrinterProfile profile, string fiscalFooter)
        {
            ConfigurePaper(report,profile);
            AddFiscalFooter(report,fiscalFooter);

            var source = new InstanceReportSource { ReportDocument = report };
            int horizontalDpi = profile.Thermal ? profile.ThermalDpi : 150;
            int verticalDpi = profile.Thermal ? profile.ThermalDpi : 150;
            var deviceInfo = new Hashtable
            {
                { "OutputFormat", "TIFF" },
                { "DpiX", horizontalDpi },
                { "DpiY", verticalDpi }
            };
            var result = new ReportProcessor().RenderReport("IMAGE",source,deviceInfo);
            if (result.HasErrors)
                throw new InvalidOperationException("The invoice report could not be rendered.",result.Errors.FirstOrDefault());
            if (result.DocumentBytes == null || result.DocumentBytes.Length == 0)
                throw new InvalidOperationException("The invoice report produced no printable pages.");

            var pages = ConvertTiffToPngPages(result.DocumentBytes,
                profile.Thermal);
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(pages));
        }

        private static void ConfigurePaper(Telerik.Reporting.Report report, SharedPrinterProfile profile)
        {
            if (profile.Thermal)
            {
                report.PageSettings.PaperKind = PaperKind.Custom;
                report.PageSettings.Landscape = false;
                report.PageSettings.PaperSize = new SizeU(
                    Unit.Mm(profile.WidthHundredths * 0.254),
                    Unit.Mm(profile.HeightHundredths * 0.254));
                return;
            }
            bool a5 = string.Equals(profile.NonThermalPaperSize,"A5",StringComparison.OrdinalIgnoreCase);
            report.PageSettings.PaperKind = a5 ? PaperKind.A5 : PaperKind.A4;
            report.PageSettings.Landscape = a5;
        }

        private static void AddFiscalFooter(Telerik.Reporting.Report report, string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            var footer = report.Items.OfType<ReportFooterSection>().FirstOrDefault();
            if (footer == null)
            {
                footer = new ReportFooterSection();
                report.Items.Add(footer);
            }
            int lineCount = text.Split(new[] { "\r\n" },StringSplitOptions.None).Length;
            var height = Unit.Mm(Math.Max(12,lineCount * 4));
            footer.Style.Visible = true;
            footer.Height = height;
            footer.Items.Clear();
            var textBox = new Telerik.Reporting.TextBox
            {
                Value = text,
                Size = new SizeU(report.Width,height)
            };
            textBox.Style.Font.Name = "Arial";
            textBox.Style.Font.Size = Unit.Point(7);
            footer.Items.Add(textBox);
        }

        private static IList<byte[]> ConvertTiffToPngPages(byte[] tiff,
            bool thermal)
        {
            var pages = new List<byte[]>();
            using (var input = new MemoryStream(tiff))
            using (var image = Image.FromStream(input))
            {
                int count = image.GetFrameCount(FrameDimension.Page);
                for (int index = 0; index < count; index++)
                {
                    image.SelectActiveFrame(FrameDimension.Page,index);
                    using (var page = new Bitmap(image.Width,image.Height,PixelFormat.Format24bppRgb))
                    {
                        float horizontal = image.HorizontalResolution <= 0 ? 150 : image.HorizontalResolution;
                        float vertical = image.VerticalResolution <= 0 ? 150 : image.VerticalResolution;
                        page.SetResolution(horizontal,vertical);
                        using (var graphics = Graphics.FromImage(page))
                        {
                            graphics.Clear(Color.White);
                            graphics.DrawImageUnscaled(image,0,0);
                        }
                        int roundedHorizontalDpi = (int)Math.Round(horizontal,
                            MidpointRounding.AwayFromZero);
                        int horizontalInsetPixels = thermal
                            ? SharedInvoicePrinting.ThermalSafetyInsetPixels(
                                roundedHorizontalDpi)
                            : 0;
                        byte[] pageBytes = SharedInvoicePrinting.EncodePagePng(
                            page, horizontalInsetPixels);
                        pages.Add(pageBytes);
                    }
                }
            }
            if (pages.Count == 0) throw new InvalidOperationException("The invoice report produced no image frames.");
            return pages;
        }
    }
}
