using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ASFuelControl.Windows.Threads;
using ASFuelControl.Windows.UI.Controls;
using Exedron.Invoicing.Core;
using Exedron.Invoicing.Adapters.Samtec;
using Newtonsoft.Json;
using Telerik.WinControls;
using Telerik.WinControls.UI;

namespace ASFuelControl.Windows.UI.SettingForms
{
    public partial class PrinterSettings
    {
        private SharedInvoiceConfiguration sharedPrinterConfiguration;
        private long sharedPrinterRevision;
        private RadCheckBox sharedDeviceEnabled, sharedBalances, sharedFillings;
        private RadTextBox sharedDeviceEndpoint;
        private RadDropDownList sharedPrinterName, sharedNonThermalPaperSize;
        private RadSpinEditor sharedPaperWidth, sharedPaperHeight, sharedCopies, sharedFontSize;
        private RadCheckBox sharedPrintBalances, sharedPrintAlerts, sharedPrintOtherReports;

        private void InitializeSharedPrinter()
        {
            sharedPrinterConfiguration = SharedInvoicingSettings.Load(out sharedPrinterRevision);
            var oldControls = Controls.Cast<Control>().ToArray();
            var tabs = new RadPageView { Dock = DockStyle.Fill };
            var device = new RadPageViewPage { Text = "Σήμανση" };
            var printing = new RadPageViewPage { Text = "Εκτύπωση" };
            var legacy = new RadPageViewPage { Text = "Παλαιές ρυθμίσεις", AutoScroll = true };
            tabs.Pages.Add(device); tabs.Pages.Add(printing); tabs.Pages.Add(legacy);
            foreach (var control in oldControls) legacy.Controls.Add(control);
            Controls.Add(tabs);
            var save = new RadButton { Text = "Αποθήκευση κοινών ρυθμίσεων", Dock = DockStyle.Bottom, Height = 36 };
            Controls.Add(save);
            ClientSize = new Size(Math.Max(ClientSize.Width,604),635);
            var signing = new InvoicingFields("Κοινός φορολογικός μηχανισμός"); device.Controls.Add(signing.Group);
            sharedDeviceEnabled = signing.Row("Μηχανισμός",new RadCheckBox { Text = "Samtec", IsChecked = sharedPrinterConfiguration.FiscalDevice != null });
            signing.Row("Κοινό περιβάλλον",new RadLabel { Text = sharedPrinterConfiguration.TestMode ? "Δοκιμαστικό" : "Παραγωγή" });
            var devices = sharedPrinterConfiguration.Devices ?? new SamtecDeviceProfile[0];
            var currentDevice = devices.SingleOrDefault(d => FiscalSigningCoordinator.SameScope(d.Scope,sharedPrinterConfiguration.FiscalDevice));
            // The HTTP service is local and normally remains the same across an
            // environment transition. Suggest the last retained endpoint (or the
            // legacy SamtecSettings.json value) without reusing its signing scope.
            string endpoint = currentDevice?.Endpoint.AbsoluteUri ?? devices.LastOrDefault()?.Endpoint.AbsoluteUri ?? LegacySamtecEndpoint();
            sharedDeviceEndpoint = signing.Row("Endpoint Samtec",InvoicingFields.Text(endpoint));
            sharedBalances = signing.Row("Ισοζύγια",new RadCheckBox { Text = "Κοινή ροή σήμανσης / αρχειοθέτησης", IsChecked = sharedPrinterConfiguration.Documents?.ContainsKey(FiscalDocumentKind.Balance) == true });
            sharedFillings = signing.Row("Παραλαβές δεξαμενής",new RadCheckBox { Text = "Κοινή ροή σήμανσης / αρχειοθέτησης", IsChecked = sharedPrinterConfiguration.Documents?.ContainsKey(FiscalDocumentKind.TankFilling) == true });
            signing.Note("Η σήμανση τιμολογίων επιλέγεται στον τύπο παραστατικού.\r\nΗ υπογραφή παρόχου παραμένει μέρος της έκδοσης παρόχου.");
            signing.Note("Οι παλιές εκδόσεις προφίλ διατηρούνται για ανάκτηση.\r\nΤο Sign A παραμένει στην παλαιά ροή.");
            var fields = new InvoicingFields("Κοινές ρυθμίσεις εκτύπωσης"); printing.Controls.Add(fields.Group);
            var p = sharedPrinterConfiguration.DefaultPrinter ?? new SharedPrinterProfile();
            sharedPrinterName = new RadDropDownList();
            foreach (string printer in System.Drawing.Printing.PrinterSettings.InstalledPrinters) sharedPrinterName.Items.Add(printer);
            sharedPrinterName.Text = p.PrinterName ?? "";
            fields.Row("Εκτυπωτής Laser",sharedPrinterName);
            sharedNonThermalPaperSize = fields.Row("Χαρτί Laser",InvoicingFields.Choice("A4 Κατακόρυφα","A5 Οριζόντια"));
            sharedNonThermalPaperSize.SelectedIndex = p.NonThermalPaperSize == "A5" ? 1 : 0;
            sharedPaperWidth = fields.Row("Πλάτος θερμικού (mm)",Millimetres(p.WidthHundredths,200,1200));
            sharedPaperHeight = fields.Row("Μήκος θερμικού (mm)",Millimetres(p.HeightHundredths,400,2000));
            sharedCopies = fields.Row("Αντίγραφα",Number(p.Copies,1,10));
            sharedFontSize = fields.Row("Γράμματα θερμικού",Number((decimal)p.FontSize,6,20));
            sharedPrintBalances = fields.Row("Εκτύπωση Ισοζυγίων / Παραλαβών",new RadCheckBox {
                IsChecked = optionHandler.GetBoolOption("PrintBalancelsOnPrinter",false) });
            sharedPrintAlerts = fields.Row("Εκτύπωση Συναγερμών",new RadCheckBox {
                IsChecked = optionHandler.GetBoolOption("PrintAlertsOnPrinter",false) });
            sharedPrintOtherReports = fields.Row("Εκτύπωση Άλλων Αναφορών",new RadCheckBox {
                IsChecked = optionHandler.GetBoolOption("PrintOnPrinter",false) });
            fields.Note("Τύπος με «Εκτύπωση Laser»: χρησιμοποιείται ο παραπάνω εκτυπωτής και το επιλεγμένο χαρτί.\r\n" +
                "Θερμική πώληση: χρησιμοποιείται ο εκτυπωτής της αντλίας. Χωρίς αυτόν: ο προεπιλεγμένος Windows.\r\n" +
                "Άλλο θερμικό παραστατικό: χρησιμοποιείται ο εκτυπωτής του τύπου παραστατικού και μετά ο προεπιλεγμένος Windows.");
            fields.Note("Το πλάτος, μήκος και μέγεθος γραμμάτων αφορούν μόνο θερμική εκτύπωση.\r\n" +
                "Οι επανεκτυπώσεις διατηρούν τις αρχικές σελίδες και ρυθμίσεις.");
            fields.Note("Τα υπογεγραμμένα ισοζύγια και οι παραλαβές αποθηκεύονται πάντα ως πλήρεις αναφορές PDF.\r\n" +
                "Οι παραπάνω επιλογές ελέγχουν μόνο τη φυσική εκτύπωση σε χαρτί.");
            save.Click += (s,e) => SaveSharedPrinter();
        }
        private static RadSpinEditor Number(decimal value,decimal min,decimal max)
            => new RadSpinEditor { Minimum = min, Maximum = max, Value = Math.Max(min,Math.Min(max,value)) };

        private static RadSpinEditor Millimetres(int hundredths,int minHundredths,int maxHundredths)
            => new RadSpinEditor { Minimum = ToMillimetres(minHundredths), Maximum = ToMillimetres(maxHundredths),
                DecimalPlaces = 1, Increment = 1, Value = ToMillimetres(hundredths) };

        private static decimal ToMillimetres(int hundredths)
            => Math.Round(hundredths * 0.254m,1,MidpointRounding.AwayFromZero);

        private static int ToHundredthsOfInch(decimal millimetres)
            => (int)Math.Round(millimetres / 0.254m,0,MidpointRounding.AwayFromZero);

        private static string LegacySamtecEndpoint()
        {
            try
            {
                var settings = Samtec.WebService.SamtecSettings.ReadSettings();
                string candidate = (settings?.SamtecUrl ?? "").TrimEnd('/') + ":" + (settings?.SamtecURLPort ?? "").Trim();
                Uri endpoint;
                return Uri.TryCreate(candidate,UriKind.Absolute,out endpoint) &&
                    (endpoint.Scheme == Uri.UriSchemeHttp || endpoint.Scheme == Uri.UriSchemeHttps) ? endpoint.AbsoluteUri : "";
            }
            catch { return ""; }
        }

        private void SaveSharedPrinter()
        {
            try
            {
                if (sharedPrinterConfiguration.Version != 2 && sharedPrinterConfiguration.Version != 3) throw new InvalidOperationException("Αποθηκεύστε πρώτα τις κοινές ρυθμίσεις στην φόρμα Γενικές Επιλογές.");
                var value = JsonConvert.DeserializeObject<SharedInvoiceConfiguration>(JsonConvert.SerializeObject(sharedPrinterConfiguration));
                value.DefaultPrinter = new SharedPrinterProfile { PrinterName = sharedPrinterName.Text.Trim(), WidthHundredths = ToHundredthsOfInch(sharedPaperWidth.Value),
                    HeightHundredths = ToHundredthsOfInch(sharedPaperHeight.Value), Copies = (short)sharedCopies.Value, FontSize = (float)sharedFontSize.Value,
                    NonThermalPaperSize = sharedNonThermalPaperSize.SelectedIndex == 1 ? "A5" : "A4" };
                SharedInvoicePrinting.CheckProfile(value.DefaultPrinter);
                if (sharedPrintBalances.IsChecked)
                    SharedInvoicePrinting.ResolveProfile(new SharedPrinterProfile { Thermal = false, NonThermalPaperSize = "A4",
                        WidthHundredths = 315, HeightHundredths = 1100, Copies = 1, FontSize = 8 },null);
                value.FiscalDevice = null;
                if (sharedDeviceEnabled.IsChecked)
                {
                    value.FiscalDevice = new FiscalSigningScope(SamtecFiscalSigner.Key,Guid.NewGuid().ToString("N"),value.TestMode ? FiscalEnvironment.Test : FiscalEnvironment.Production,
                        optionHandler.GetOption("CompanyTIN"),optionHandler.GetIntOption("CompanyBranch",0));
                    var profile = new SamtecDeviceProfile(value.FiscalDevice,new Uri(sharedDeviceEndpoint.Text.Trim(),UriKind.Absolute));
                    value.Devices = (value.Devices ?? new SamtecDeviceProfile[0]).Concat(new[] { profile }).ToArray();
                }
                if ((sharedBalances.IsChecked || sharedFillings.IsChecked) && value.FiscalDevice == null) throw new InvalidOperationException("Ορίστε μηχανισμό για ισοζύγια / παραλαβές.");
                if (value.Documents == null) value.Documents = new System.Collections.Generic.Dictionary<FiscalDocumentKind,SharedInvoiceRoute>();
                foreach (var kind in new[] { FiscalDocumentKind.Balance,FiscalDocumentKind.TankFilling })
                {
                    bool enabled = kind == FiscalDocumentKind.Balance ? sharedBalances.IsChecked : sharedFillings.IsChecked;
                    if (enabled) value.Documents[kind] = new SharedInvoiceRoute { Signing = value.FiscalDevice };
                    else value.Documents.Remove(kind);
                }
                sharedPrinterRevision = SharedInvoicingSettings.Save(value,sharedPrinterRevision);
                optionHandler.SetOption("InvoicePageSetting",value.DefaultPrinter.NonThermalPaperSize);
                optionHandler.SetOption("PrintBalancelsOnPrinter",sharedPrintBalances.IsChecked);
                optionHandler.SetOption("PrintAlertsOnPrinter",sharedPrintAlerts.IsChecked);
                optionHandler.SetOption("PrintOnPrinter",sharedPrintOtherReports.IsChecked);
                sharedPrinterConfiguration = value;
                RadMessageBox.Show("Αποθηκεύτηκε. Οι επιλογές φυσικής εκτύπωσης εφαρμόζονται άμεσα.\r\n" +
                    "Αλλαγές μηχανισμού ή δρομολόγησης εφαρμόζονται μετά από επανεκκίνηση.","Σήμανση / Εκτύπωση");
            }
            catch (Exception ex) { RadMessageBox.Show(ex.Message,"Σήμανση / Εκτύπωση",MessageBoxButtons.OK,RadMessageIcon.Error); }
        }
    }
}
