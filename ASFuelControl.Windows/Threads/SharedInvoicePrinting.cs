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
        public int WidthHundredths { get; set; } = 315;
        public int HeightHundredths { get; set; } = 1100;
        public short Copies { get; set; } = 1;
        public float FontSize { get; set; } = 8;
    }
    internal sealed class SharedInvoicePrinting : IFiscalPrintTransport
    {
        internal const string Format = "invoice-png-pages/v1";
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
            profile.PrinterName = ResolvePrinterName(profile.PrinterName,installationDefault?.PrinterName,installedPrinters,windowsDefault);
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
            var profile = JsonConvert.DeserializeObject<SharedPrinterProfile>(snapshot.PrintDestination);
            CheckProfile(profile);
            var text = Encoding.UTF8.GetString(snapshot.PrintContent) + "\r\n" + Footer(snapshot,response);
            const int dpi = 150;
            int width = profile.WidthHundredths * dpi / 100, height = profile.HeightHundredths * dpi / 100;
            var pages = new List<byte[]>();
            bool qrPrinted = false;
            using (var font = new Font("Consolas", profile.FontSize))
            using (var format = new StringFormat { Trimming = StringTrimming.None })
            using (var qrImage = CreateQr(response?.QrCodeUrl))
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
                            var area = new RectangleF(15, 15, width - 30, height - 30);
                            graphics.MeasureString(text, font, area.Size, format, out chars, out lines);
                            if (chars <= 0) throw new InvalidOperationException("The invoice cannot fit the configured paper.");
                            graphics.DrawString(text.Substring(0, chars), font, Brushes.Black, area, format);
                            text = text.Substring(chars);
                            if (text.Length == 0 && qrImage != null)
                            {
                                int size = Math.Min(width - 30, 300);
                                int top = 30 + (int)Math.Ceiling(font.GetHeight(graphics) * lines);
                                if (top + size < height - 15)
                                {
                                    graphics.DrawImage(qrImage, 15, top, size, size);
                                    qrPrinted = true;
                                }
                            }
                        }
                        using (var output = new MemoryStream()) { page.Save(output, ImageFormat.Png); pages.Add(output.ToArray()); }
                    }
                }
                if (qrImage != null && !qrPrinted)
                {
                    using (var page = new Bitmap(width, height))
                    {
                        page.SetResolution(dpi, dpi);
                        using (var graphics = Graphics.FromImage(page))
                        {
                            graphics.Clear(Color.White);
                            int size = Math.Min(width - 30, 300);
                            graphics.DrawImage(qrImage, 15, 15, size, size);
                            graphics.DrawString("Invoice: " + snapshot.InvoiceId + "\r\nMARK: " + response.Mark + "\r\n" + response.QrCodeUrl,
                                font, Brushes.Black, new RectangleF(15, size + 30, width - 30, height - size - 45));
                        }
                        using (var output = new MemoryStream()) { page.Save(output, ImageFormat.Png); pages.Add(output.ToArray()); }
                    }
                }
            }
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(pages));
        }
        private static Bitmap CreateQr(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            using (var generator = new QRCoder.QRCodeGenerator())
            using (var data = generator.CreateQrCode(url, QRCoder.QRCodeGenerator.ECCLevel.M))
            using (var code = new QRCoder.QRCode(data)) return code.GetGraphic(6);
        }
        internal static void CheckProfile(SharedPrinterProfile p)
        {
            if (p == null || p.Copies < 1 || p.Copies > 10 ||
                p.WidthHundredths < 200 || p.WidthHundredths > 1200 || p.HeightHundredths < 400 || p.HeightHundredths > 2000 ||
                p.FontSize < 6 || p.FontSize > 20) throw new InvalidOperationException("Ελέγξτε τις ρυθμίσεις χαρτιού, αντιγράφων και μεγέθους γραμμάτων.");
        }
        public void Validate(string destination, string format, byte[] document)
        {
            if (format != Format) throw new InvalidOperationException("Unsupported saved print format.");
            var p = ResolveProfile(JsonConvert.DeserializeObject<SharedPrinterProfile>(destination),installationDefault());
            if (!new PrinterSettings { PrinterName = p.PrinterName }.IsValid) throw new InvalidOperationException(MissingPrinterMessage);
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
                job.DefaultPageSettings.PaperSize = new PaperSize("Saved invoice", p.WidthHundredths, p.HeightHundredths);
                job.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
                job.PrintController = new StandardPrintController();
                job.PrintPage += (sender, args) => {
                    using (var stream = new MemoryStream(pages[index++]))
                    using (var image = Image.FromStream(stream))
                    {
                        // Account for the physical printer's non-printable margins.
                        args.Graphics.TranslateTransform(-args.PageSettings.HardMarginX, -args.PageSettings.HardMarginY);
                        var area = args.PageSettings.PrintableArea;
                        float scale = Math.Min(area.Width / image.Width, area.Height / image.Height);
                        args.Graphics.DrawImage(image, area.X, area.Y, image.Width * scale, image.Height * scale);
                    }
                    args.HasMorePages = index < pages.Length;
                };
                job.Print();
                return "Windows spool handoff: " + job.DocumentName + "; printer: " + p.PrinterName;
            }
        }
    }
}
