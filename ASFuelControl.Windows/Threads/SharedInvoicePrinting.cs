using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text;
using ASFuelControl.Invoicing;
using Newtonsoft.Json;

namespace ASFuelControl.Windows.Threads
{
    // Page geometry and copies are part of the immutable destination snapshot.
    internal sealed class SharedPrinterProfile
    {
        public string PrinterName { get; set; }
        // Historical shared pages used the configurable narrow dimensions. Keep
        // that as the deserialization default; new routes set this explicitly
        // from InvoiceType.IsLaserPrint.
        public bool Thermal { get; set; } = true;
        public string NonThermalPaperSize { get; set; }
        public int WidthHundredths { get; set; } = 315;
        public int HeightHundredths { get; set; } = 1100;
        public short Copies { get; set; } = 1;
        public float FontSize { get; set; } = 8;
    }
    internal sealed class SharedInvoicePrinting : IFiscalPrintTransport
    {
        internal const string Format = "invoice-png-pages/v1";
        internal const int ThermalContentMarginPixels = 24; // 4.064 mm at 150 dpi
        internal const string MissingPrinterMessage = "Δεν βρέθηκε εκτυπωτής για την έκδοση. Ορίστε έναν εγκατεστημένο εκτυπωτή ως προεπιλεγμένο στην εγκατάσταση ή στα Windows.";
        private readonly Func<SharedPrinterProfile> installationDefault;
        internal SharedInvoicePrinting(Func<SharedPrinterProfile> installationDefault = null)
        { this.installationDefault = installationDefault ?? (() => null); }

        internal static string ResolvePrinterName(string requested, string installationDefault,
            IEnumerable<string> installedPrinters, string windowsDefault)
        {
            var installed = installedPrinters.Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();
            foreach (var candidate in new[] { requested,installationDefault,windowsDefault })
            {
                if (string.IsNullOrWhiteSpace(candidate)) continue;
                var match = installed.FirstOrDefault(p => string.Equals(p,candidate.Trim(),StringComparison.OrdinalIgnoreCase));
                if (match != null) return match;
            }
            return null;
        }
        internal static SharedPrinterProfile ResolveProfile(SharedPrinterProfile requested, SharedPrinterProfile installationDefault)
        {
            var installed = PrinterSettings.InstalledPrinters.Cast<string>().ToArray();
            return ResolveProfile(requested,installationDefault,installed,installed.Length == 0 ? null : new PrinterSettings().PrinterName);
        }
        internal static SharedPrinterProfile ResolveProfile(SharedPrinterProfile requested, SharedPrinterProfile installationDefault,
            IEnumerable<string> installedPrinters, string windowsDefault)
        {
            // Preserve saved page geometry/copies and never rewrite a historical
            // destination. Only the physical printer may fall back for this handoff.
            var profile = JsonConvert.DeserializeObject<SharedPrinterProfile>(JsonConvert.SerializeObject(requested ?? installationDefault ?? new SharedPrinterProfile()));
            CheckProfile(profile);
            profile.PrinterName = ResolvePrinterName(profile.PrinterName,
                profile.Thermal ? null : installationDefault?.PrinterName,
                installedPrinters,windowsDefault);
            if (profile.PrinterName == null) throw new InvalidOperationException(MissingPrinterMessage);
            return profile;
        }
        internal static string Footer(InvoiceWorkflowSnapshot snapshot, Exedron.Invoicing.Core.Models.InvoiceResponse response)
        {
            var provider = snapshot.Provider == null ? null : Exedron.Invoicing.Core.InvoiceProviderCatalog.Get(snapshot.Provider.ProviderKey);
            var missing = Exedron.Invoicing.Core.ProviderPrintMetadata.Missing(provider?.Key, response);
            if (missing.Length != 0) throw new InvalidOperationException("Η έκδοση έχει επιβεβαιωθεί, αλλά λείπουν στοιχεία εκτύπωσης: " + string.Join(", ", missing) + ". Ανακτήστε τα στοιχεία από τον πάροχο. Μην εκδώσετε ξανά το παραστατικό.");
            var footer = new List<string>();
            if (response?.TransmissionFailure2 == true) footer.Add("Αδυναμία διαβίβασης παρόχου προς myDATA (Transmission failure 2).\r\nΣε ουρά Ilyda — αναμένεται MARK.");
            if (snapshot.Provider != null && snapshot.Provider.ProviderKey != "myDATA") footer.Add("Πάροχος: " + (snapshot.Provider.ProviderKey == "Ilyda.Direct" ? "Ilyda" : "iTimologia"));
            if (!string.IsNullOrWhiteSpace(provider?.SoftwareLicence)) footer.Add("Άδεια λογισμικού παρόχου: " + provider.SoftwareLicence);
            if (!string.IsNullOrWhiteSpace(response?.ProviderInvoiceId)) footer.Add("Provider invoiceId: " + response.ProviderInvoiceId);
            if (!string.IsNullOrWhiteSpace(response?.InvoiceSignature)) footer.Add(response.InvoiceSignature);
            if (!string.IsNullOrWhiteSpace(response?.Mark)) footer.Add("MARK: " + response.Mark);
            if (!string.IsNullOrWhiteSpace(response?.Uid)) footer.Add("UID: " + response.Uid);
            if (!string.IsNullOrWhiteSpace(response?.VerificationHash)) footer.Add(response.VerificationHash);
            if (!string.IsNullOrWhiteSpace(response?.ProviderUrl)) footer.Add(response.ProviderUrl);
            return string.Join("\r\n", footer);
        }
        internal static byte[] Render(InvoiceWorkflowSnapshot snapshot, Exedron.Invoicing.Core.Models.InvoiceResponse response)
        {
            return Render(snapshot,response,null);
        }
        internal static byte[] Render(InvoiceWorkflowSnapshot snapshot, Exedron.Invoicing.Core.Models.InvoiceResponse response,
            Func<InvoiceWorkflowSnapshot,Exedron.Invoicing.Core.Models.InvoiceResponse,SharedPrinterProfile,byte[]> laserRenderer)
        {
            var profile = JsonConvert.DeserializeObject<SharedPrinterProfile>(snapshot.PrintDestination);
            CheckProfile(profile);
            if (!profile.Thermal && laserRenderer != null)
                return laserRenderer(snapshot,response,profile);
            var text = Encoding.UTF8.GetString(snapshot.PrintContent) + "\r\n" + Footer(snapshot,response);
            const int dpi = 150;
            int pageWidth, pageHeight;
            bool landscape;
            PageGeometry(profile,out pageWidth,out pageHeight,out landscape);
            int width = (landscape ? pageHeight : pageWidth) * dpi / 100;
            int height = (landscape ? pageWidth : pageHeight) * dpi / 100;
            var pages = new List<byte[]>();
            bool qrCodesPrinted = false;
            using (var font = new Font("Consolas", profile.FontSize))
            using (var format = new StringFormat { Trimming = StringTrimming.None })
            using (var fiscalQrImage = CreateQr(response?.QrCodeUrl,QRCoder.QRCodeGenerator.ECCLevel.M))
            using (var companyQrImage = CreateQr(snapshot.CompanyQrContent,QRCoder.QRCodeGenerator.ECCLevel.L))
            {
                format.FormatFlags = StringFormatFlags.LineLimit;
                while (text.Length > 0)
                {
                    using (var page = new Bitmap(width, height))
                    {
                        page.SetResolution(dpi, dpi);
                        using (var graphics = Graphics.FromImage(page))
                        {
                            graphics.Clear(Color.White);
                            int chars, lines;
                            var area = new RectangleF(ThermalContentMarginPixels,ThermalContentMarginPixels,
                                width - ThermalContentMarginPixels * 2,height - ThermalContentMarginPixels * 2);
                            graphics.MeasureString(text, font, area.Size, format, out chars, out lines);
                            if (chars <= 0) throw new InvalidOperationException("The invoice cannot fit the configured paper.");
                            graphics.DrawString(text.Substring(0, chars), font, Brushes.Black, area, format);
                            text = text.Substring(chars);
                            if (text.Length == 0 && (fiscalQrImage != null || companyQrImage != null))
                            {
                                int top = ThermalContentMarginPixels * 2 + (int)Math.Ceiling(font.GetHeight(graphics) * lines);
                                qrCodesPrinted = DrawQrCodes(graphics,fiscalQrImage,companyQrImage,width,height,top,font,
                                    ThermalContentMarginPixels) >= 0;
                            }
                        }
                        using (var output = new MemoryStream()) { page.Save(output, ImageFormat.Png); pages.Add(output.ToArray()); }
                    }
                }
                if ((fiscalQrImage != null || companyQrImage != null) && !qrCodesPrinted)
                {
                    using (var page = new Bitmap(width, height))
                    {
                        page.SetResolution(dpi, dpi);
                        using (var graphics = Graphics.FromImage(page))
                        {
                            graphics.Clear(Color.White);
                            int bottom = DrawQrCodes(graphics,fiscalQrImage,companyQrImage,width,height,
                                ThermalContentMarginPixels,font,ThermalContentMarginPixels);
                            if (bottom < 0) throw new InvalidOperationException("The QR codes cannot fit the configured paper.");
                            graphics.DrawString("Invoice: " + snapshot.InvoiceId + "\r\nMARK: " + response?.Mark + "\r\n" + response?.QrCodeUrl,
                                font, Brushes.Black, new RectangleF(ThermalContentMarginPixels,bottom + ThermalContentMarginPixels,
                                    width - ThermalContentMarginPixels * 2,height - bottom - ThermalContentMarginPixels * 2));
                        }
                        using (var output = new MemoryStream()) { page.Save(output, ImageFormat.Png); pages.Add(output.ToArray()); }
                    }
                }
            }
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(pages));
        }
        private static int DrawQrCodes(Graphics graphics, Bitmap fiscalQr, Bitmap companyQr, int pageWidth, int pageHeight,
            int top, Font font, int margin)
        {
            var images = new List<Bitmap>();
            var labels = new List<string>();
            if (fiscalQr != null) { images.Add(fiscalQr); labels.Add("QR Φορολογικό"); }
            if (companyQr != null) { images.Add(companyQr); labels.Add("QR Εταιρείας"); }
            if (images.Count == 0) return top;

            const int gap = 15;
            int availableWidth = pageWidth - margin * 2;
            int size = images.Count == 1 ? Math.Min(availableWidth,300) : Math.Min((availableWidth - gap) / 2,240);
            int labelHeight = (int)Math.Ceiling(font.GetHeight(graphics));
            int bottom = top + labelHeight + size;
            if (bottom > pageHeight - margin) return -1;
            int totalWidth = images.Count * size + (images.Count - 1) * gap;
            int left = (pageWidth - totalWidth) / 2;
            for (int index = 0; index < images.Count; index++)
            {
                int x = left + index * (size + gap);
                graphics.DrawString(labels[index],font,Brushes.Black,new RectangleF(x,top,size,labelHeight));
                graphics.DrawImage(images[index],x,top + labelHeight,size,size);
            }
            return bottom;
        }
        private static Bitmap CreateQr(string content, QRCoder.QRCodeGenerator.ECCLevel level)
        {
            if (string.IsNullOrWhiteSpace(content)) return null;
            using (var generator = new QRCoder.QRCodeGenerator())
            using (var data = generator.CreateQrCode(content,level))
            using (var code = new QRCoder.QRCode(data)) return code.GetGraphic(6);
        }
        internal static void CheckProfile(SharedPrinterProfile p)
        {
            if (p == null || p.Copies < 1 || p.Copies > 10 ||
                p.WidthHundredths < 200 || p.WidthHundredths > 1200 || p.HeightHundredths < 400 || p.HeightHundredths > 2000 ||
                p.FontSize < 6 || p.FontSize > 20) throw new InvalidOperationException("Ελέγξτε τις ρυθμίσεις χαρτιού, αντιγράφων και μεγέθους γραμμάτων.");
            if (!p.Thermal && p.NonThermalPaperSize != "A4" && p.NonThermalPaperSize != "A5")
                throw new InvalidOperationException("Επιλέξτε μέγεθος χαρτιού Laser A4 ή A5.");
        }
        internal static void PageGeometry(SharedPrinterProfile profile, out int width, out int height, out bool landscape)
        {
            CheckProfile(profile);
            if (profile.Thermal)
            {
                width = profile.WidthHundredths;
                height = profile.HeightHundredths;
                landscape = false;
                return;
            }
            landscape = profile.NonThermalPaperSize == "A5";
            width = landscape ? 583 : 827;
            height = landscape ? 827 : 1169;
        }
        internal static PaperSize ResolvePaperSize(SharedPrinterProfile profile, IEnumerable<PaperSize> driverPaperSizes)
        {
            int width, height;
            bool landscape;
            PageGeometry(profile,out width,out height,out landscape);
            if (profile.Thermal) return new PaperSize("Thermal",width,height);

            var expectedKind = profile.NonThermalPaperSize == "A5" ? PaperKind.A5 : PaperKind.A4;
            var papers = (driverPaperSizes ?? Enumerable.Empty<PaperSize>()).ToArray();
            var selected = papers.FirstOrDefault(paper => paper.Kind == expectedKind) ??
                papers.FirstOrDefault(paper => paper.PaperName != null &&
                    paper.PaperName.IndexOf(profile.NonThermalPaperSize,StringComparison.OrdinalIgnoreCase) >= 0);
            if (selected == null)
                throw new InvalidOperationException("Ο εκτυπωτής δεν δηλώνει υποστήριξη για χαρτί " +
                    profile.NonThermalPaperSize + ". Εγκαταστήστε τον πλήρη οδηγό του εκτυπωτή και ελέγξτε τα υποστηριζόμενα μεγέθη χαρτιού.");

            // Use the driver's RawKind instead of a custom PaperSize. Drivers may
            // silently replace a custom A5 request with their A4 default.
            return new PaperSize(selected.PaperName,selected.Width,selected.Height) { RawKind = selected.RawKind };
        }
        private static void ApplyPageSettings(PageSettings settings, PaperSize paper, bool landscape)
        {
            settings.PaperSize = paper;
            settings.Landscape = landscape;
            settings.Margins = new Margins(0,0,0,0);
        }
        internal static RectangleF NormalizePrintableArea(RectangleF area, int imageWidth, int imageHeight)
        {
            bool imageLandscape = imageWidth > imageHeight;
            bool areaLandscape = area.Width > area.Height;
            if (imageWidth == imageHeight || area.Width == area.Height || imageLandscape == areaLandscape)
                return area;
            return new RectangleF(area.Y,area.X,area.Height,area.Width);
        }
        internal static RectangleF ApplyThermalSafetyInset(RectangleF area, bool thermal)
        {
            // Some POS80 drivers report the complete 80 mm roll as printable,
            // although the print head cannot reach its outer edges. Apply this at
            // handoff too, so reprints of already-frozen pages remain complete.
            const float insetHundredths = 8f; // 2.032 mm on each side
            if (!thermal || area.Width <= insetHundredths * 2) return area;
            return new RectangleF(area.X + insetHundredths,area.Y,
                area.Width - insetHundredths * 2,area.Height);
        }
        public void Validate(string destination, string format, byte[] document)
        {
            if (format != Format) throw new InvalidOperationException("Unsupported saved print format.");
            var p = ResolveProfile(JsonConvert.DeserializeObject<SharedPrinterProfile>(destination),installationDefault());
            var printer = new PrinterSettings { PrinterName = p.PrinterName };
            if (!printer.IsValid) throw new InvalidOperationException(MissingPrinterMessage);
            ResolvePaperSize(p,printer.PaperSizes.Cast<PaperSize>());
            var pages = JsonConvert.DeserializeObject<byte[][]>(Encoding.UTF8.GetString(document));
            if (pages == null || pages.Length == 0) throw new InvalidOperationException("No saved pages.");
            foreach (var bytes in pages) using (var stream = new MemoryStream(bytes)) using (var image = Image.FromStream(stream))
                if (image.Width <= 0) throw new InvalidOperationException("Invalid saved page.");
        }
        public string Dispatch(string destination, string format, byte[] document, Guid jobId)
        {
            var p = ResolveProfile(JsonConvert.DeserializeObject<SharedPrinterProfile>(destination),installationDefault());
            var pages = JsonConvert.DeserializeObject<byte[][]>(Encoding.UTF8.GetString(document));
            int index = 0;
            using (var job = new PrintDocument())
            {
                job.DocumentName = "Fiscal-" + jobId.ToString("N");
                job.PrinterSettings.PrinterName = p.PrinterName; job.PrinterSettings.Copies = p.Copies;
                int pageWidth, pageHeight;
                bool landscape;
                PageGeometry(p,out pageWidth,out pageHeight,out landscape);
                var paper = ResolvePaperSize(p,job.PrinterSettings.PaperSizes.Cast<PaperSize>());
                ApplyPageSettings(job.DefaultPageSettings,paper,landscape);
                job.QueryPageSettings += (sender,args) => ApplyPageSettings(args.PageSettings,paper,landscape);
                job.PrintController = new StandardPrintController();
                job.PrintPage += (sender, args) => {
                    using (var stream = new MemoryStream(pages[index++]))
                    using (var image = Image.FromStream(stream))
                    {
                        // Account for the physical printer's non-printable margins.
                        var reportedArea = args.PageSettings.PrintableArea;
                        var area = ApplyThermalSafetyInset(
                            NormalizePrintableArea(reportedArea,image.Width,image.Height),p.Thermal);
                        bool swapped = area.Width == reportedArea.Height && area.Height == reportedArea.Width &&
                            (reportedArea.Width != reportedArea.Height);
                        float hardMarginX = swapped ? args.PageSettings.HardMarginY : args.PageSettings.HardMarginX;
                        float hardMarginY = swapped ? args.PageSettings.HardMarginX : args.PageSettings.HardMarginY;
                        args.Graphics.TranslateTransform(-hardMarginX,-hardMarginY);
                        float scale = Math.Min(area.Width / image.Width, area.Height / image.Height);
                        args.Graphics.DrawImage(image, area.X, area.Y, image.Width * scale, image.Height * scale);
                    }
                    args.HasMorePages = index < pages.Length;
                };
                job.Print();
                return "Windows spool handoff: " + job.DocumentName + "; printer: " + p.PrinterName +
                    "; paper: " + paper.PaperName + "; landscape: " + landscape;
            }
        }
    }
}
