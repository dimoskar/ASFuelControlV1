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
        private RadTextBox sharedDeviceEndpoint, sharedDeviceTerminalId;
        private RadDropDownList sharedPrinterName, sharedThermalPrinterName, sharedNonThermalPaperSize;
        private RadSpinEditor sharedPaperWidth, sharedPaperHeight, sharedCopies,
            sharedFontSize, sharedThermalDpi;
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
            // application-option value) without reusing its signing scope.
            string endpoint = currentDevice?.Endpoint.AbsoluteUri ?? devices.LastOrDefault()?.Endpoint.AbsoluteUri ?? ConfiguredSamtecEndpoint();
            sharedDeviceEndpoint = signing.Row("Endpoint Samtec",InvoicingFields.Text(endpoint));
            sharedDeviceTerminalId = signing.Row("Terminal ID Samtec",InvoicingFields.Text(ConfiguredSamtecTerminalId() ?? ""));
            sharedBalances = signing.Row("Ισοζύγια",new RadCheckBox { Text = "Κοινή ροή σήμανσης / αρχειοθέτησης", IsChecked = sharedPrinterConfiguration.Documents?.ContainsKey(FiscalDocumentKind.Balance) == true });
            sharedFillings = signing.Row("Παραλαβές δεξαμενής",new RadCheckBox { Text = "Κοινή ροή σήμανσης / αρχειοθέτησης", IsChecked = sharedPrinterConfiguration.Documents?.ContainsKey(FiscalDocumentKind.TankFilling) == true });
            signing.Note("Η σήμανση τιμολογίων επιλέγεται στον τύπο παραστατικού.\r\nΗ υπογραφή παρόχου παραμένει μέρος της έκδοσης παρόχου.");
            signing.Note("Για αποκλειστική σήμανση Samtec, οι πληρωμές κάρτας / IRIS χρησιμοποιούν το παραπάνω Terminal ID.");
            signing.Note("Οι παλιές εκδόσεις προφίλ διατηρούνται για ανάκτηση.\r\nΤο Sign A παραμένει στην παλαιά ροή.");
            var printingLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1,
                RowCount = 3, AutoScroll = true, Margin = new Padding(0), Padding = new Padding(0) };
            printingLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            printingLayout.RowStyles.Add(new RowStyle(SizeType.Absolute,135));
            printingLayout.RowStyles.Add(new RowStyle(SizeType.Absolute,230));
            printingLayout.RowStyles.Add(new RowStyle(SizeType.Absolute,175));
            printing.Controls.Add(printingLayout);

            var laserFields = new InvoicingFields("Παραστατικά Laser");
            var thermalFields = new InvoicingFields("Θερμικά παραστατικά");
            var reportFields = new InvoicingFields("Φυσική εκτύπωση αναφορών");
            printingLayout.Controls.Add(laserFields.Group,0,0);
            printingLayout.Controls.Add(thermalFields.Group,0,1);
            printingLayout.Controls.Add(reportFields.Group,0,2);
            var p = sharedPrinterConfiguration.DefaultPrinter ?? new SharedPrinterProfile();
            sharedPrinterName = PrinterChoice(p.PrinterName);
            laserFields.Row("Εκτυπωτής Laser",sharedPrinterName);
            sharedNonThermalPaperSize = laserFields.Row("Χαρτί Laser",InvoicingFields.Choice("A4 Κατακόρυφα","A5 Οριζόντια"));
            sharedNonThermalPaperSize.SelectedIndex = p.NonThermalPaperSize == "A5" ? 1 : 0;
            sharedCopies = laserFields.Row("Αντίγραφα Laser",Number(SharedPrinterProfile.NormalizeLaserCopies(p.LaserCopies),1,10));

            sharedThermalPrinterName = PrinterChoice(p.ThermalPrinterName);
            thermalFields.Row("Εφεδρικός εκτυπωτής",sharedThermalPrinterName);
            sharedPaperWidth = thermalFields.Row("Πλάτος χαρτιού (mm)",Millimetres(p.WidthHundredths,200,1200));
            sharedPaperHeight = thermalFields.Row("Μήκος χαρτιού (mm)",Millimetres(p.HeightHundredths,400,2000));
            RadSpinEditor thermalDpiEditor = Number(p.ThermalDpi, 100, 600);
            sharedThermalDpi = thermalFields.Row("Ανάλυση θερμικού (DPI)",
                thermalDpiEditor);
            sharedFontSize = thermalFields.Row("Μέγεθος γραμμάτων",Number((decimal)p.FontSize,6,20));
            thermalFields.Note("Πώληση αντλίας: εκτυπωτής αντλίας → εφεδρικός θερμικός → προεπιλεγμένος Windows.\r\n" +
                "Άλλο θερμικό: εκτυπωτής τύπου → εφεδρικός θερμικός → προεπιλεγμένος Windows.");

            sharedPrintBalances = reportFields.Row("Εκτύπωση Ισοζυγίων / Παραλαβών",new RadCheckBox {
                IsChecked = optionHandler.GetBoolOption("PrintBalancelsOnPrinter",false) });
            sharedPrintAlerts = reportFields.Row("Εκτύπωση Συναγερμών",new RadCheckBox {
                IsChecked = optionHandler.GetBoolOption("PrintAlertsOnPrinter",false) });
            sharedPrintOtherReports = reportFields.Row("Εκτύπωση Άλλων Αναφορών",new RadCheckBox {
                IsChecked = optionHandler.GetBoolOption("PrintOnPrinter",false) });
            reportFields.Note("Τα υπογεγραμμένα ισοζύγια και οι παραλαβές αποθηκεύονται πάντα ως πλήρεις αναφορές PDF.\r\n" +
                "Οι παραπάνω επιλογές ελέγχουν μόνο τη φυσική εκτύπωση σε χαρτί.");
            save.Click += (s,e) => SaveSharedPrinter();
        }
        private static RadSpinEditor Number(decimal value,decimal min,decimal max)
            => new RadSpinEditor { Minimum = min, Maximum = max, Value = Math.Max(min,Math.Min(max,value)) };

        private const string WindowsDefaultPrinterChoice = "(Προεπιλεγμένος Windows)";

        private static RadDropDownList PrinterChoice(string selectedPrinter)
        {
            var control = new RadDropDownList { DropDownStyle = RadDropDownStyle.DropDownList };
            control.Items.Add(WindowsDefaultPrinterChoice);
            foreach (string printer in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
                control.Items.Add(printer);
            int selectedIndex = 0;
            if (!string.IsNullOrWhiteSpace(selectedPrinter))
            {
                string selected = selectedPrinter.Trim();
                selectedIndex = Enumerable.Range(1,control.Items.Count - 1).FirstOrDefault(index =>
                    string.Equals(control.Items[index].Text,selected,StringComparison.OrdinalIgnoreCase));
                if (selectedIndex == 0)
                {
                    control.Items.Add(selected);
                    selectedIndex = control.Items.Count - 1;
                }
            }
            control.SelectedIndex = selectedIndex;
            return control;
        }

        private static string SelectedPrinter(RadDropDownList control)
        {
            string selected = (control?.Text ?? "").Trim();
            return selected == WindowsDefaultPrinterChoice || selected.Length == 0 ? null : selected;
        }

        private static RadSpinEditor Millimetres(int hundredths,int minHundredths,int maxHundredths)
            => new RadSpinEditor { Minimum = ToMillimetres(minHundredths), Maximum = ToMillimetres(maxHundredths),
                DecimalPlaces = 1, Increment = 1, Value = ToMillimetres(hundredths) };

        private static decimal ToMillimetres(int hundredths)
            => Math.Round(hundredths * 0.254m,1,MidpointRounding.AwayFromZero);

        private static int ToHundredthsOfInch(decimal millimetres)
            => (int)Math.Round(millimetres / 0.254m,0,MidpointRounding.AwayFromZero);

        private static string ConfiguredSamtecEndpoint()
        {
            try { return SharedInvoicingSettings.LoadSamtecApplicationOptions().Endpoint?.AbsoluteUri ?? ""; }
            catch { return ""; }
        }

        private static string ConfiguredSamtecTerminalId()
        {
            try { return SharedInvoicingSettings.LoadSamtecApplicationOptions().TerminalId; }
            catch { return null; }
        }

        private void SaveSharedPrinter()
        {
            try
            {
                if (sharedPrinterConfiguration.Version != 2 && sharedPrinterConfiguration.Version != 3) throw new InvalidOperationException("Αποθηκεύστε πρώτα τις κοινές ρυθμίσεις στην φόρμα Γενικές Επιλογές.");
                var value = JsonConvert.DeserializeObject<SharedInvoiceConfiguration>(JsonConvert.SerializeObject(sharedPrinterConfiguration));
                value.DefaultPrinter = new SharedPrinterProfile { PrinterName = SelectedPrinter(sharedPrinterName),
                    ThermalPrinterName = SelectedPrinter(sharedThermalPrinterName), WidthHundredths = ToHundredthsOfInch(sharedPaperWidth.Value),
                    HeightHundredths = ToHundredthsOfInch(sharedPaperHeight.Value),
                    ThermalDpi = (int)sharedThermalDpi.Value, Copies = 1,
                    LaserCopies = (short)sharedCopies.Value,
                    FontSize = (float)sharedFontSize.Value,
                    NonThermalPaperSize = sharedNonThermalPaperSize.SelectedIndex == 1 ? "A5" : "A4" };
                SharedInvoicePrinting.CheckProfile(value.DefaultPrinter);
                if (sharedPrintBalances.IsChecked)
                    SharedInvoicePrinting.ResolveProfile(new SharedPrinterProfile { Thermal = false, NonThermalPaperSize = "A4",
                        WidthHundredths = 315, HeightHundredths = 1100, Copies = 1, FontSize = 8 },null);
                var previousScope = value.FiscalDevice;
                var previousProfile = previousScope == null ? null :
                    (value.Devices ?? new SamtecDeviceProfile[0]).SingleOrDefault(device =>
                        FiscalSigningCoordinator.SameScope(device.Scope,previousScope));
                value.FiscalDevice = null;
                string terminalId = sharedDeviceTerminalId.Text.Trim();
                SharedInvoicingSettings.SaveSamtecTerminalId(terminalId);
                if (sharedDeviceEnabled.IsChecked)
                {
                    var endpoint = new Uri(sharedDeviceEndpoint.Text.Trim(),UriKind.Absolute);
                    SharedInvoicingSettings.SaveSamtecEndpoint(endpoint);
                    var environment = value.TestMode ? FiscalEnvironment.Test : FiscalEnvironment.Production;
                    var issuerVat = optionHandler.GetOption("CompanyTIN");
                    var issuerBranch = optionHandler.GetIntOption("CompanyBranch",0);
                    string normalizedTerminal = string.IsNullOrWhiteSpace(terminalId) ? null : terminalId;
                    bool unchangedProfile = previousProfile != null &&
                        previousScope.Environment == environment && previousScope.IssuerVat == issuerVat &&
                        previousScope.IssuerBranch == issuerBranch && previousProfile.Endpoint == endpoint &&
                        string.Equals(previousProfile.EftPosTID,normalizedTerminal,StringComparison.Ordinal);
                    value.FiscalDevice = unchangedProfile ? previousScope :
                        new FiscalSigningScope(SamtecFiscalSigner.Key,Guid.NewGuid().ToString("N"),environment,
                            issuerVat,issuerBranch);
                    var profile = new SamtecDeviceProfile(value.FiscalDevice,
                        endpoint,normalizedTerminal);
                    if (!unchangedProfile)
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
                optionHandler.SetOption(SharedInvoicingSettings.LaserCopiesOption,value.DefaultPrinter.LaserCopies);
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
