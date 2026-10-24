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
        private RadDropDownList sharedPrinterName;
        private RadSpinEditor sharedPaperWidth, sharedPaperHeight, sharedCopies, sharedFontSize;

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
            var currentDevice = (sharedPrinterConfiguration.Devices ?? new SamtecDeviceProfile[0]).SingleOrDefault(d => FiscalSigningCoordinator.SameScope(d.Scope,sharedPrinterConfiguration.FiscalDevice));
            sharedDeviceEndpoint = signing.Row("Endpoint Samtec",InvoicingFields.Text(currentDevice?.Endpoint.AbsoluteUri));
            sharedBalances = signing.Row("Ισοζύγια",new RadCheckBox { Text = "Κοινή ροή σήμανσης / εκτύπωσης", IsChecked = sharedPrinterConfiguration.Documents?.ContainsKey(FiscalDocumentKind.Balance) == true });
            sharedFillings = signing.Row("Παραλαβές δεξαμενής",new RadCheckBox { Text = "Κοινή ροή σήμανσης / εκτύπωσης", IsChecked = sharedPrinterConfiguration.Documents?.ContainsKey(FiscalDocumentKind.TankFilling) == true });
            signing.Note("Η σήμανση τιμολογίων επιλέγεται στον τύπο παραστατικού.\r\nΗ υπογραφή παρόχου παραμένει μέρος της έκδοσης παρόχου.");
            signing.Note("Οι παλιές εκδόσεις προφίλ διατηρούνται για ανάκτηση.\r\nΤο Sign A παραμένει στην παλαιά ροή.");
            var fields = new InvoicingFields("Κοινές ρυθμίσεις εκτύπωσης"); printing.Controls.Add(fields.Group);
            var p = sharedPrinterConfiguration.DefaultPrinter ?? new SharedPrinterProfile();
            sharedPrinterName = new RadDropDownList();
            foreach (string printer in System.Drawing.Printing.PrinterSettings.InstalledPrinters) sharedPrinterName.Items.Add(printer);
            sharedPrinterName.Text = p.PrinterName ?? "";
            fields.Row("Προεπιλεγμένος εκτυπωτής",sharedPrinterName);
            sharedPaperWidth = fields.Row("Πλάτος (1/100 ίντσας)",Number(p.WidthHundredths,200,1200));
            sharedPaperHeight = fields.Row("Ύψος (1/100 ίντσας)",Number(p.HeightHundredths,400,2000));
            sharedCopies = fields.Row("Αντίγραφα",Number(p.Copies,1,10));
            sharedFontSize = fields.Row("Μέγεθος γραμμάτων",Number((decimal)p.FontSize,6,20));
            fields.Note("Αν ο εκτυπωτής του τύπου λείπει, χρησιμοποιείται ο παραπάνω.\r\nΑν λείπει και αυτός, χρησιμοποιείται ο προεπιλεγμένος Windows.");
            fields.Note("Κενή επιλογή: προεπιλεγμένος Windows. Οι επανεκτυπώσεις\r\nδιατηρούν τις αρχικές σελίδες με την ίδια εφεδρική επιλογή.");
            save.Click += (s,e) => SaveSharedPrinter();
        }
        private static RadSpinEditor Number(decimal value,decimal min,decimal max)
            => new RadSpinEditor { Minimum = min, Maximum = max, Value = Math.Max(min,Math.Min(max,value)) };

        private void SaveSharedPrinter()
        {
            try
            {
                if (sharedPrinterConfiguration.Version != 2 && sharedPrinterConfiguration.Version != 3) throw new InvalidOperationException("Αποθηκεύστε πρώτα τις κοινές ρυθμίσεις στην φόρμα Γενικές Επιλογές.");
                var value = JsonConvert.DeserializeObject<SharedInvoiceConfiguration>(JsonConvert.SerializeObject(sharedPrinterConfiguration));
                value.DefaultPrinter = new SharedPrinterProfile { PrinterName = sharedPrinterName.Text.Trim(), WidthHundredths = (int)sharedPaperWidth.Value,
                    HeightHundredths = (int)sharedPaperHeight.Value, Copies = (short)sharedCopies.Value, FontSize = (float)sharedFontSize.Value };
                SharedInvoicePrinting.CheckProfile(value.DefaultPrinter);
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
                    if (enabled) value.Documents[kind] = new SharedInvoiceRoute { Signing = value.FiscalDevice, Printer = value.DefaultPrinter };
                    else value.Documents.Remove(kind);
                }
                sharedPrinterRevision = SharedInvoicingSettings.Save(value,sharedPrinterRevision);
                sharedPrinterConfiguration = value;
                RadMessageBox.Show("Αποθηκεύτηκε. Απαιτείται επανεκκίνηση της εφαρμογής.","Σήμανση / Εκτύπωση");
            }
            catch (Exception ex) { RadMessageBox.Show(ex.Message,"Σήμανση / Εκτύπωση",MessageBoxButtons.OK,RadMessageIcon.Error); }
        }
    }
}
