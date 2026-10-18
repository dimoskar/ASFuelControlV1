using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ASFuelControl.Invoicing.Persistence;
using ASFuelControl.Invoicing.Mapping;
using Exedron.Invoicing.Core;
using Newtonsoft.Json;

namespace ASFuelControl.Windows.Threads
{
    internal static class SharedInvoicingSettings
    {
        internal static SqlFiscalConfigurationStore Store() => new SqlFiscalConfigurationStore(new SqlFiscalDatabase(Properties.Settings.Default.DBConnection));
        internal static SharedInvoiceConfiguration Load(out long revision)
        {
            var record = Store().Load();
            revision = record?.Revision ?? 0;
            if (record != null) return JsonConvert.DeserializeObject<SharedInvoiceConfiguration>(record.Json);
            var options = Data.Implementation.OptionHandler.Instance;
            var file = options.GetOption("SharedInvoicingConfiguration", "");
            if (!string.IsNullOrWhiteSpace(file))
            {
                if (!Path.IsPathRooted(file)) throw new InvalidOperationException("The legacy shared configuration path must be absolute.");
                var previous = JsonConvert.DeserializeObject<SharedInvoiceConfiguration>(File.ReadAllText(file));
                if (previous == null || previous.Version != 1) throw new InvalidOperationException("Invalid legacy configuration.");
                previous.Enabled = options.GetBoolOption("SharedInvoicingEnabled",false);
                return previous;
            }
            if (options.GetBoolOption("SharedInvoicingEnabled",false)) throw new InvalidOperationException("Shared invoicing is enabled without configuration.");
            return new SharedInvoiceConfiguration { Version = 2, FirstInvoiceDate = DateTime.Today,
                Mapping = new MappingContext { WholesaleCodes = new HashSet<int>(), Countries = new[] { new CountryInput { CountryCode = "GR", DisplayMember = "Ελλάδα", IsEu = true } } },
                DefaultPrinter = new SharedPrinterProfile(), InvoiceTypes = new Dictionary<Guid,SharedInvoiceRoute>(),
                Documents = new Dictionary<FiscalDocumentKind,SharedInvoiceRoute>(), Credentials = new SharedCredentialProfile[0],
                Devices = new Exedron.Invoicing.Adapters.Samtec.SamtecDeviceProfile[0] };
        }
        internal static SharedInvoiceConfiguration LoadRuntime()
        {
            long revision;
            var value = Load(out revision);
            if (value == null || (value.Version != 1 && value.Version != 2)) throw new InvalidOperationException("Unsupported invoicing configuration.");
            value.Policies = Store().LoadPolicies();
            value.ProtectedKinds = new SqlFiscalOwnershipStore(new SqlFiscalDatabase(Properties.Settings.Default.DBConnection)).ProtectedKinds();
            return value;
        }
        internal static string Preflight(SharedInvoiceConfiguration value)
        {
            var warnings = new List<string>();
            var problems = Problems(value,warnings);
            var result = problems.Count == 0 ? "Οι ρυθμίσεις είναι συνεπείς. Δεν έγινε αποστολή σε πάροχο, POS, μηχανισμό ή εκτυπωτή. Απαιτούνται οι δοκιμές εγκατάστασης πριν την παραγωγική μετάβαση." : string.Join("\r\n",problems);
            if (value.FirstInvoiceDate >= new DateTime(1753,1,1))
            {
                int legacy = Store().CountLegacyInCutover(value.FirstInvoiceDate);
                if (legacy != 0) warnings.Add(legacy + " παραστατικά έχουν εκτύπωση ή φορολογικά στοιχεία εκτός κοινής ροής. Δεν εμποδίζουν την ενεργοποίηση. Ελέγχονται ξεχωριστά πριν από τυχόν διαβίβαση.");
            }
            return result + (warnings.Count == 0 ? "" : "\r\n\r\nΕνημέρωση (δεν εμποδίζει την ενεργοποίηση):\r\n" + string.Join("\r\n",warnings));
        }
        private static List<string> Problems(SharedInvoiceConfiguration value, List<string> warnings = null)
        {
            var problems = new List<string>();
            if (value.FirstInvoiceDate < new DateTime(1753,1,1)) problems.Add("Ορίστε έγκυρη ημερομηνία έναρξης.");
            if (value.Version != 2) problems.Add("Απαιτούνται κοινές ρυθμίσεις έκδοσης 2.");
            var policies = Store().LoadPolicies();
            value.Policies = policies;
            var printers = new HashSet<string>(System.Drawing.Printing.PrinterSettings.InstalledPrinters.Cast<string>(),StringComparer.OrdinalIgnoreCase);
            string windowsDefault = printers.Count == 0 ? null : new System.Drawing.Printing.PrinterSettings().PrinterName;
            bool missingPrinter = false;
            using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                foreach (var type in db.InvoiceTypes.Where(t => t.Printable && t.Invalidated != true).ToArray())
                {
                    var special = SharedInvoiceConfiguration.SpecialWorkflowProblem(type);
                    if (special != null)
                    {
                        warnings?.Add(type.Description + ": " + special);
                        continue;
                    }
                    try
                    {
                        var printer = value.Route(type.InvoiceTypeId,type.Printer).Printer ?? value.DefaultPrinter ?? new SharedPrinterProfile();
                        SharedInvoicePrinting.CheckProfile(printer);
                        if (SharedInvoicePrinting.ResolvePrinterName(printer.PrinterName,value.DefaultPrinter?.PrinterName,printers,windowsDefault) == null)
                            missingPrinter = true;
                    }
                    catch (Exception ex) { problems.Add(type.Description + ": " + ex.Message); }
                }
            }
            foreach (var route in (value.Documents ?? new Dictionary<FiscalDocumentKind,SharedInvoiceRoute>()).Values)
            {
                if (route.Signing == null || route.Provider != null || route.Payment != null || route.LocalOnly ||
                    !FiscalSigningCoordinator.SameScope(route.Signing,value.FiscalDevice))
                    problems.Add("Τα ισοζύγια / οι παραλαβές πρέπει να χρησιμοποιούν τον κοινό μηχανισμό. Ελέγξτε τη φόρμα εκτύπωσης.");
                try
                {
                    var printer = route.Printer ?? value.DefaultPrinter ?? new SharedPrinterProfile();
                    SharedInvoicePrinting.CheckProfile(printer);
                    if (SharedInvoicePrinting.ResolvePrinterName(printer.PrinterName,value.DefaultPrinter?.PrinterName,printers,windowsDefault) == null)
                        missingPrinter = true;
                }
                catch (Exception ex) { problems.Add(ex.Message); }
            }
            if (missingPrinter) problems.Add(SharedInvoicePrinting.MissingPrinterMessage);
            return problems;
        }
        internal static void ValidateActivation(SharedInvoiceConfiguration value)
        {
            if (!value.Enabled) return;
            var problems = Problems(value);
            if (problems.Count != 0) throw new InvalidOperationException(string.Join("\r\n",problems));
        }
        internal static long Save(SharedInvoiceConfiguration value,long revision)
        {
            ValidateActivation(value);
            return Store().Save(JsonConvert.SerializeObject(value),revision,Program.CurrentUserName ?? Environment.UserName);
        }
    }
}
