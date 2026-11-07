using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
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
        // PrinterName is the effective/requested destination of one job. The
        // installation keeps a separate fallback for thermal invoices so it does
        // not accidentally reuse the configured laser printer.
        public string ThermalPrinterName { get; set; }
        // Historical shared pages used the configurable narrow dimensions. Keep
        // that as the deserialization default; new routes set this explicitly
        // from InvoiceType.IsLaserPrint.
        public bool Thermal { get; set; } = true;
        public string NonThermalPaperSize { get; set; }
        public int WidthHundredths { get; set; } = 315;
        public int HeightHundredths { get; set; } = 1100;
        public short Copies { get; set; } = 1;
        // Copies is the effective value frozen into a print operation. Keep a
        // separate installation value so laser copies do not affect thermal jobs.
        public short LaserCopies { get; set; }
        public float FontSize { get; set; } = 8;
        // Thermal pages are frozen at the target device resolution. Most POS
        // printers use 203 DPI; installations with a 300-DPI device may override
        // this value in the printer settings before the fiscal page is created.
        public int ThermalDpi { get; set; } = 203;

        internal static short NormalizeLaserCopies(int copies)
        {
            return (short)(copies < 1 || copies > 10 ? 2 : copies);
        }
    }
    internal sealed class SharedInvoicePrinting : IFiscalPrintTransport
    {
        internal const string LegacyFormat = "invoice-png-pages/v1";
        internal const string Format = "invoice-png-pages/v2";
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
                profile.Thermal ? installationDefault?.ThermalPrinterName : installationDefault?.PrinterName,
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
            Func<InvoiceWorkflowSnapshot,Exedron.Invoicing.Core.Models.InvoiceResponse,SharedPrinterProfile,byte[]> reportRenderer)
        {
            var profile = JsonConvert.DeserializeObject<SharedPrinterProfile>(snapshot.PrintDestination);
            CheckProfile(profile);
            // The common workflow must use the same established invoice reports as
            // legacy printing. Previously the renderer was used only for laser
            // profiles, so every thermal invoice silently fell back to the generic
            // monospaced text receipt regardless of its configured report.
            if (reportRenderer != null)
                return reportRenderer(snapshot,response,profile);
            var text = Encoding.UTF8.GetString(snapshot.PrintContent) + "\r\n" + Footer(snapshot,response);
            int dpi = profile.Thermal ? profile.ThermalDpi : 150;
            int pageWidth, pageHeight;
            bool landscape;
            PageGeometry(profile,out pageWidth,out pageHeight,out landscape);
            int width = (landscape ? pageHeight : pageWidth) * dpi / 100;
            int height = (landscape ? pageWidth : pageHeight) * dpi / 100;
            int contentMarginPixels = profile.Thermal
                ? (int)Math.Round(ThermalContentMarginPixels * dpi / 150D,
                    MidpointRounding.AwayFromZero)
                : ThermalContentMarginPixels;
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
                            var area = new RectangleF(contentMarginPixels,
                                contentMarginPixels,
                                width - contentMarginPixels * 2,
                                height - contentMarginPixels * 2);
                            graphics.MeasureString(text, font, area.Size, format, out chars, out lines);
                            if (chars <= 0) throw new InvalidOperationException("The invoice cannot fit the configured paper.");
                            graphics.DrawString(text.Substring(0, chars), font, Brushes.Black, area, format);
                            text = text.Substring(chars);
                            if (text.Length == 0 && (fiscalQrImage != null || companyQrImage != null))
                            {
                                int top = contentMarginPixels * 2 +
                                    (int)Math.Ceiling(font.GetHeight(graphics) * lines);
                                qrCodesPrinted = DrawQrCodes(graphics,fiscalQrImage,companyQrImage,width,height,top,font,
                                    contentMarginPixels) >= 0;
                            }
                        }
                        int horizontalInsetPixels = profile.Thermal
                            ? ThermalSafetyInsetPixels(dpi)
                            : 0;
                        byte[] pageBytes = EncodePagePng(page,
                            horizontalInsetPixels);
                        pages.Add(pageBytes);
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
                                contentMarginPixels,font,contentMarginPixels);
                            if (bottom < 0) throw new InvalidOperationException("The QR codes cannot fit the configured paper.");
                            graphics.DrawString("Invoice: " + snapshot.InvoiceId + "\r\nMARK: " + response?.Mark + "\r\n" + response?.QrCodeUrl,
                                font, Brushes.Black, new RectangleF(
                                    contentMarginPixels,
                                    bottom + contentMarginPixels,
                                    width - contentMarginPixels * 2,
                                    height - bottom - contentMarginPixels * 2));
                        }
                        int horizontalInsetPixels = profile.Thermal
                            ? ThermalSafetyInsetPixels(dpi)
                            : 0;
                        byte[] pageBytes = EncodePagePng(page,
                            horizontalInsetPixels);
                        pages.Add(pageBytes);
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
                p.FontSize < 6 || p.FontSize > 20 || p.ThermalDpi < 100 ||
                p.ThermalDpi > 600)
            {
                throw new InvalidOperationException(
                    "Ελέγξτε τις ρυθμίσεις χαρτιού, αντιγράφων, ανάλυσης και μεγέθους γραμμάτων.");
            }
            if (!p.Thermal && p.NonThermalPaperSize != "A4" && p.NonThermalPaperSize != "A5")
                throw new InvalidOperationException("Επιλέξτε μέγεθος χαρτιού Laser A4 ή A5.");
        }

        internal static byte[] EncodePagePng(Bitmap source,
            int horizontalInsetPixels)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            using (var output = new MemoryStream())
            using (var preparedPage = PrepareThermalPage(source,
                horizontalInsetPixels))
            {
                preparedPage.Save(output, ImageFormat.Png);
                return output.ToArray();
            }
        }

        internal static int ThermalSafetyInsetPixels(int dpi)
        {
            const float insetInches = 0.08F;
            float inset = dpi * insetInches;
            return (int)Math.Round(inset, MidpointRounding.AwayFromZero);
        }

        private static Bitmap PrepareThermalPage(Bitmap source,
            int horizontalInsetPixels)
        {
            if (horizontalInsetPixels <= 0)
            {
                var sourceBounds = new Rectangle(0, 0, source.Width,
                    source.Height);
                return source.Clone(sourceBounds,
                    PixelFormat.Format24bppRgb);
            }

            int availableWidth = source.Width - horizontalInsetPixels * 2;
            if (availableWidth <= 0)
            {
                throw new InvalidOperationException(
                    "The configured thermal safety inset leaves no printable width.");
            }

            var prepared = new Bitmap(source.Width, source.Height,
                PixelFormat.Format24bppRgb);
            prepared.SetResolution(source.HorizontalResolution,
                source.VerticalResolution);
            using (var graphics = Graphics.FromImage(prepared))
            {
                graphics.Clear(Color.White);
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                var destination = new Rectangle(horizontalInsetPixels, 0,
                    availableWidth, source.Height);
                var sourceBounds = new Rectangle(0, 0, source.Width,
                    source.Height);
                graphics.DrawImage(source, destination, sourceBounds,
                    GraphicsUnit.Pixel);
            }

            return prepared;
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
        internal static PaperSize ResolveRenderedThermalPaperSize(SharedPrinterProfile profile, byte[] page)
        {
            if (profile == null || !profile.Thermal) throw new ArgumentException("A thermal profile is required.");
            if (page == null || page.Length == 0) throw new InvalidOperationException("No saved thermal page.");
            using (var stream = new MemoryStream(page))
            using (var image = Image.FromStream(stream))
            {
                if (image.Width <= 0 || image.Height <= 0) throw new InvalidOperationException("Invalid saved thermal page.");
                return new PaperSize("Thermal",profile.WidthHundredths,profile.HeightHundredths);
            }
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
            bool supportedFormat = format == Format || format == LegacyFormat;
            if (!supportedFormat)
            {
                throw new InvalidOperationException("Unsupported saved print format.");
            }
            var p = ResolveProfile(JsonConvert.DeserializeObject<SharedPrinterProfile>(destination),installationDefault());
            var printer = new PrinterSettings { PrinterName = p.PrinterName };
            if (!printer.IsValid) throw new InvalidOperationException(MissingPrinterMessage);
            var pages = JsonConvert.DeserializeObject<byte[][]>(Encoding.UTF8.GetString(document));
            if (pages == null || pages.Length == 0) throw new InvalidOperationException("No saved pages.");
            if (p.Thermal)
            {
                foreach (var bytes in pages) ResolveRenderedThermalPaperSize(p,bytes);
            }
            else
            {
                ResolvePaperSize(p,printer.PaperSizes.Cast<PaperSize>());
                foreach (var bytes in pages) using (var stream = new MemoryStream(bytes)) using (var image = Image.FromStream(stream))
                    if (image.Width <= 0) throw new InvalidOperationException("Invalid saved page.");
            }
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
                bool landscape = !p.Thermal && p.NonThermalPaperSize == "A5";
                var papers = p.Thermal
                    ? pages.Select(page => ResolveRenderedThermalPaperSize(p,page)).ToArray()
                    : Enumerable.Repeat(ResolvePaperSize(p,job.PrinterSettings.PaperSizes.Cast<PaperSize>()),pages.Length).ToArray();
                ApplyPageSettings(job.DefaultPageSettings,papers[0],landscape);
                job.QueryPageSettings += (sender,args) => ApplyPageSettings(args.PageSettings,
                    papers[Math.Min(index,papers.Length - 1)],landscape);
                job.PrintController = new StandardPrintController();
                job.PrintPage += (sender, args) => {
                    using (var stream = new MemoryStream(pages[index++]))
                    using (var image = Image.FromStream(stream))
                    {
                        var reportedArea = args.PageSettings.PrintableArea;
                        var area = NormalizePrintableArea(reportedArea,
                            image.Width, image.Height);
                        bool swapped = area.Width == reportedArea.Height && area.Height == reportedArea.Width &&
                            (reportedArea.Width != reportedArea.Height);
                        float hardMarginX = swapped ? args.PageSettings.HardMarginY : args.PageSettings.HardMarginX;
                        float hardMarginY = swapped ? args.PageSettings.HardMarginX : args.PageSettings.HardMarginY;
                        args.Graphics.TranslateTransform(-hardMarginX,-hardMarginY);
                        bool nativeThermalPage = p.Thermal && format == Format;
                        if (nativeThermalPage)
                        {
                            // The report already contains its safe hardware margins.
                            // Draw the frozen page at the physical size for which it
                            // was rendered; another fit-to-width pass softens text and
                            // applies the margin twice.
                            args.Graphics.InterpolationMode =
                                InterpolationMode.NearestNeighbor;
                            args.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
                            float horizontalResolution =
                                image.HorizontalResolution > 0F
                                    ? image.HorizontalResolution
                                    : p.ThermalDpi;
                            float verticalResolution =
                                image.VerticalResolution > 0F
                                    ? image.VerticalResolution
                                    : p.ThermalDpi;
                            float physicalWidth = image.Width * 100F /
                                horizontalResolution;
                            float physicalHeight = image.Height * 100F /
                                verticalResolution;
                            var thermalPageBounds = new RectangleF(0F, 0F,
                                physicalWidth, physicalHeight);
                            var source = new RectangleF(0F, 0F, image.Width,
                                image.Height);
                            args.Graphics.DrawImage(image, thermalPageBounds, source,
                                GraphicsUnit.Pixel);
                        }
                        else
                        {
                            if (p.Thermal)
                            {
                                area = ApplyThermalSafetyInset(area, true);
                            }
                            float horizontalScale = area.Width / image.Width;
                            float verticalScale = area.Height / image.Height;
                            float scale = Math.Min(horizontalScale, verticalScale);
                            float renderedWidth = image.Width * scale;
                            float renderedHeight = image.Height * scale;
                            args.Graphics.DrawImage(image, area.X, area.Y,
                                renderedWidth, renderedHeight);
                        }
                    }
                    args.HasMorePages = index < pages.Length;
                };
                job.Print();
                return "Windows spool handoff: " + job.DocumentName + "; printer: " + p.PrinterName +
                    "; paper: " + papers[0].PaperName + " " + papers[0].Width + "x" + papers[0].Height +
                    "; landscape: " + landscape;
            }
        }
    }
}
