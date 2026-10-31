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
    /// Renders laser invoices with the established invoice reports. The resulting
    /// PNG pages are persisted by FiscalPrintService, so retries never query or
    /// recreate the report after the first handoff attempt.
    /// </summary>
    internal static class SharedInvoiceLaserReportRenderer
    {
        internal static byte[] Render(InvoiceWorkflowSnapshot snapshot, InvoiceResponse response, SharedPrinterProfile profile)
        {
            if (profile == null || profile.Thermal)
                throw new InvalidOperationException("A laser printer profile is required for the invoice report.");

            // This also validates that all mandatory provider print evidence is
            // present before a page is frozen in the print operation.
            var fiscalFooter = SharedInvoicePrinting.Footer(snapshot,response);

            using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                var invoice = db.Invoices.Single(i => i.InvoiceId == snapshot.InvoiceId);
                var qrCodeUrl = response == null ? invoice.QRCodeData : response.QrCodeUrl;
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

        private static byte[] RenderPages(Telerik.Reporting.Report report, SharedPrinterProfile profile, string fiscalFooter)
        {
            ConfigurePaper(report,profile);
            AddFiscalFooter(report,fiscalFooter);

            var source = new InstanceReportSource { ReportDocument = report };
            var deviceInfo = new Hashtable
            {
                { "OutputFormat", "TIFF" },
                { "DpiX", 150 },
                { "DpiY", 150 }
            };
            var result = new ReportProcessor().RenderReport("IMAGE",source,deviceInfo);
            if (result.HasErrors)
                throw new InvalidOperationException("The invoice report could not be rendered.",result.Errors.FirstOrDefault());
            if (result.DocumentBytes == null || result.DocumentBytes.Length == 0)
                throw new InvalidOperationException("The invoice report produced no printable pages.");

            var pages = ConvertTiffToPngPages(result.DocumentBytes);
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(pages));
        }

        private static void ConfigurePaper(Telerik.Reporting.Report report, SharedPrinterProfile profile)
        {
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

        private static IList<byte[]> ConvertTiffToPngPages(byte[] tiff)
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
                        using (var output = new MemoryStream())
                        {
                            page.Save(output,ImageFormat.Png);
                            pages.Add(output.ToArray());
                        }
                    }
                }
            }
            if (pages.Count == 0) throw new InvalidOperationException("The invoice report produced no image frames.");
            return pages;
        }
    }
}
