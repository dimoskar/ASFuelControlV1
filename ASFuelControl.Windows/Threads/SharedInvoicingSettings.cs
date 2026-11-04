using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ASFuelControl.Invoicing.Persistence;
using ASFuelControl.Invoicing.Mapping;
using Exedron.Invoicing.Adapters;
using Exedron.Invoicing.Adapters.Samtec;
using Exedron.Invoicing.Core;
using Newtonsoft.Json;

namespace ASFuelControl.Windows.Threads
{
    internal static class SharedInvoicingSettings
    {
        internal const string SamtecUrlOption = "SamtecUrl";
        internal const string SamtecPortOption = "SamtecURLPort";
        internal const string SamtecTerminalIdOption = "EftPosTID";
        internal const string LaserCopiesOption = "InvoiceCopies";

        internal sealed class SamtecApplicationOptions
        {
            internal string Url { get; set; }
            internal string Port { get; set; }
            internal string TerminalId { get; set; }
            internal Uri Endpoint { get; set; }
        }

        internal static SqlFiscalConfigurationStore Store() => new SqlFiscalConfigurationStore(new SqlFiscalDatabase(Properties.Settings.Default.DBConnection));
        internal static SharedInvoiceConfiguration Load(out long revision)
        {
            var record = Store().Load();
            revision = record?.Revision ?? 0;
            if (record != null) return ApplyLegacyPrinterOptions(JsonConvert.DeserializeObject<SharedInvoiceConfiguration>(record.Json));
            var options = Data.Implementation.OptionHandler.Instance;
            var file = options.GetOption("SharedInvoicingConfiguration", "");
            if (!string.IsNullOrWhiteSpace(file))
            {
                if (!Path.IsPathRooted(file)) throw new InvalidOperationException("The legacy shared configuration path must be absolute.");
                var previous = JsonConvert.DeserializeObject<SharedInvoiceConfiguration>(File.ReadAllText(file));
                if (previous == null || previous.Version != 1) throw new InvalidOperationException("Invalid legacy configuration.");
                previous.Enabled = options.GetBoolOption("SharedInvoicingEnabled",false);
                return ApplyLegacyPrinterOptions(previous);
            }
            if (options.GetBoolOption("SharedInvoicingEnabled",false)) throw new InvalidOperationException("Shared invoicing is enabled without configuration.");
            return ApplyLegacyPrinterOptions(new SharedInvoiceConfiguration { Version = 3, FirstInvoiceDate = DateTime.Today,
                Mapping = new MappingContext { WholesaleCodes = new HashSet<int>(), Countries = new[] { new CountryInput { CountryCode = "GR", DisplayMember = "Ελλάδα", IsEu = true } } },
                DefaultPrinter = new SharedPrinterProfile(), InvoiceTypes = new Dictionary<Guid,SharedInvoiceRoute>(),
                Documents = new Dictionary<FiscalDocumentKind,SharedInvoiceRoute>(), Credentials = new SharedCredentialProfile[0],
                Devices = new Exedron.Invoicing.Adapters.Samtec.SamtecDeviceProfile[0], MyDieselCredentials = new SharedMyDieselCredentialProfile[0] });
        }
        private static SharedInvoiceConfiguration ApplyLegacyPrinterOptions(SharedInvoiceConfiguration value)
        {
            if (value == null) return null;
            value.EnsureWholesaleCodes();
            ApplySamtecApplicationOptions(value);
            if (value.DefaultPrinter == null) value.DefaultPrinter = new SharedPrinterProfile();
            short optionCopies = GetLaserCopiesOption();
            if (value.DefaultPrinter.LaserCopies <= 0)
                value.DefaultPrinter.LaserCopies = optionCopies;
            else
            {
                value.DefaultPrinter.LaserCopies = SharedPrinterProfile.NormalizeLaserCopies(value.DefaultPrinter.LaserCopies);
                if (value.DefaultPrinter.LaserCopies != optionCopies)
                    Data.Implementation.OptionHandler.Instance.SetOption(LaserCopiesOption,value.DefaultPrinter.LaserCopies);
            }
            if (string.IsNullOrWhiteSpace(value.DefaultPrinter.NonThermalPaperSize))
                value.DefaultPrinter.NonThermalPaperSize = Data.Implementation.OptionHandler.Instance.GetOptionOrAdd("InvoicePageSetting","A4");
            if (value.DefaultPrinter.NonThermalPaperSize != "A4" && value.DefaultPrinter.NonThermalPaperSize != "A5")
                value.DefaultPrinter.NonThermalPaperSize = "A4";
            return value;
        }
        internal static short GetLaserCopiesOption()
        {
            var options = Data.Implementation.OptionHandler.Instance;
            int configured = options.GetIntOption(LaserCopiesOption,2);
            short normalized = SharedPrinterProfile.NormalizeLaserCopies(configured);
            if (!options.OptionExists(LaserCopiesOption) || configured != normalized)
                options.SetOption(LaserCopiesOption,normalized);
            return normalized;
        }
        internal static SamtecApplicationOptions LoadSamtecApplicationOptions()
        {
            EnsureSamtecApplicationOptions();
            var options = Data.Implementation.OptionHandler.Instance;
            string url = (options.GetOption(SamtecUrlOption,"") ?? "").Trim();
            string port = (options.GetOption(SamtecPortOption,"") ?? "").Trim();
            string terminal = (options.GetOption(SamtecTerminalIdOption,"") ?? "").Trim();
            return new SamtecApplicationOptions {
                Url = url,
                Port = port,
                TerminalId = string.IsNullOrWhiteSpace(terminal) ? null : terminal,
                Endpoint = BuildSamtecEndpoint(url,port)
            };
        }

        internal static void SaveSamtecEndpoint(Uri endpoint)
        {
            if (endpoint == null || !endpoint.IsAbsoluteUri ||
                (endpoint.Scheme != Uri.UriSchemeHttp && endpoint.Scheme != Uri.UriSchemeHttps))
                throw new InvalidOperationException("Ορίστε έγκυρο HTTP/HTTPS endpoint Samtec.");
            var withoutPort = new UriBuilder(endpoint) { Port = -1 };
            string url = withoutPort.Uri.AbsoluteUri.TrimEnd('/');
            var options = Data.Implementation.OptionHandler.Instance;
            options.SetOption(SamtecUrlOption,url);
            options.SetOption(SamtecPortOption,endpoint.Port.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        internal static void SaveSamtecTerminalId(string terminalId)
        {
            Data.Implementation.OptionHandler.Instance.SetOption(
                SamtecTerminalIdOption,(terminalId ?? "").Trim());
        }

        private static void EnsureSamtecApplicationOptions()
        {
            var options = Data.Implementation.OptionHandler.Instance;
            string url = options.GetOption(SamtecUrlOption,"");
            string port = options.GetOption(SamtecPortOption,"");
            string terminal = options.GetOption(SamtecTerminalIdOption,"");
            bool endpointMissing = string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(port);
            bool terminalMissing = string.IsNullOrWhiteSpace(terminal);
            if (!endpointMissing && !terminalMissing) return;

            Samtec.WebService.SamtecSettings file = null;
            try { file = Samtec.WebService.SamtecSettings.ReadSettings(); }
            catch { }

            // URL and port are one setting pair. If either option is empty, copy
            // both values only when the file contains a complete pair.
            if (file != null && endpointMissing && !string.IsNullOrWhiteSpace(file.SamtecUrl) &&
                !string.IsNullOrWhiteSpace(file.SamtecURLPort))
            {
                options.SetOption(SamtecUrlOption,file.SamtecUrl.Trim());
                options.SetOption(SamtecPortOption,file.SamtecURLPort.Trim());
            }
            // The terminal belongs to a separate concern and is migrated on its own.
            if (file != null && terminalMissing && !string.IsNullOrWhiteSpace(file.EftPosTID))
                options.SetOption(SamtecTerminalIdOption,file.EftPosTID.Trim());

            // Keep all three settings visible as application options even when
            // the legacy JSON file does not exist.
            if (!options.OptionExists(SamtecUrlOption)) options.SetOption(SamtecUrlOption,"");
            if (!options.OptionExists(SamtecPortOption)) options.SetOption(SamtecPortOption,"");
            if (!options.OptionExists(SamtecTerminalIdOption)) options.SetOption(SamtecTerminalIdOption,"");
        }

        private static Uri BuildSamtecEndpoint(string url, string port)
        {
            Uri baseUri;
            int portNumber;
            if (string.IsNullOrWhiteSpace(url) ||
                !Uri.TryCreate(url,UriKind.Absolute,out baseUri) ||
                (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps) ||
                !int.TryParse(port,out portNumber) || portNumber < 1 || portNumber > 65535)
                return null;
            var endpoint = new UriBuilder(baseUri) { Port = portNumber }.Uri;
            return endpoint;
        }

        private static bool ApplySamtecApplicationOptions(SharedInvoiceConfiguration value)
        {
            if (value?.FiscalDevice == null || value.Devices == null) return false;
            var settings = LoadSamtecApplicationOptions();
            int currentIndex = Array.FindIndex(value.Devices,device =>
                FiscalSigningCoordinator.SameScope(device.Scope,value.FiscalDevice));
            if (currentIndex < 0) return false;
            var current = value.Devices[currentIndex];
            Uri endpoint = settings.Endpoint ?? current.Endpoint;
            string terminal = settings.TerminalId ?? current.EftPosTID;
            if (endpoint == current.Endpoint && string.Equals(terminal,current.EftPosTID,StringComparison.Ordinal))
                return false;
            var devices = value.Devices.ToArray();
            devices[currentIndex] = new SamtecDeviceProfile(current.Scope,endpoint,terminal);
            value.Devices = devices;
            return true;
        }

        internal static void EnsureApplicationOptionsOnStartup()
        {
            EnsureSamtecApplicationOptions();
            GetLaserCopiesOption();
            var store = Store();
            var record = store.Load();
            if (record == null) return;
            var value = JsonConvert.DeserializeObject<SharedInvoiceConfiguration>(record.Json);
            if (value == null) return;
            bool changed = value.EnsureWholesaleCodes();
            changed = ApplySamtecApplicationOptions(value) || changed;
            if (!changed) return;
            store.Save(JsonConvert.SerializeObject(value),record.Revision,"application startup");
        }
        internal static SharedInvoiceConfiguration LoadRuntime()
        {
            long revision;
            var value = Load(out revision);
            if (value == null || (value.Version != 1 && value.Version != 2 && value.Version != 3)) throw new InvalidOperationException("Unsupported invoicing configuration.");
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
            if (value.Version != 2 && value.Version != 3) problems.Add("Απαιτούνται κοινές ρυθμίσεις έκδοσης 2 ή 3.");
            MyDieselProblems(value,problems);
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
                        var printer = value.Route(type).Printer ?? value.DefaultPrinter ?? new SharedPrinterProfile();
                        SharedInvoicePrinting.CheckProfile(printer);
                        if (SharedInvoicePrinting.ResolveProfile(printer,value.DefaultPrinter,printers,windowsDefault).PrinterName == null)
                            missingPrinter = true;
                    }
                    catch (Exception ex) { problems.Add(type.Description + ": " + ex.Message); }
                }
            }
            var documents = value.Documents ?? new Dictionary<FiscalDocumentKind,SharedInvoiceRoute>();
            foreach (var route in documents.Values)
            {
                if (route.Signing == null || route.Provider != null || route.Payment != null || route.LocalOnly ||
                    !FiscalSigningCoordinator.SameScope(route.Signing,value.FiscalDevice))
                    problems.Add("Τα ισοζύγια / οι παραλαβές πρέπει να χρησιμοποιούν τον κοινό μηχανισμό. Ελέγξτε τη φόρμα εκτύπωσης.");
            }
            if (documents.Count != 0 && Data.Implementation.OptionHandler.Instance.GetBoolOption("PrintBalancelsOnPrinter",false))
            {
                try
                {
                    var printer = new SharedPrinterProfile { Thermal = false, NonThermalPaperSize = "A4",
                        WidthHundredths = 315, HeightHundredths = 1100, Copies = 1, FontSize = 8 };
                    SharedInvoicePrinting.CheckProfile(printer);
                    if (SharedInvoicePrinting.ResolvePrinterName(null,null,printers,windowsDefault) == null)
                        missingPrinter = true;
                }
                catch (Exception ex) { problems.Add(ex.Message); }
            }
            if (missingPrinter) problems.Add(SharedInvoicePrinting.MissingPrinterMessage);
            return problems;
        }
        private static void MyDieselProblems(SharedInvoiceConfiguration value, List<string> problems)
        {
            var profile = value.MyDiesel;
            if (profile == null || !profile.Enabled) return;
            if (value.Version != 3) problems.Add("Η ενεργοποίηση myDiesel απαιτεί αποθήκευση ρυθμίσεων έκδοσης 3.");
            if (profile.Environment != FiscalEnvironment.Test && profile.Environment != FiscalEnvironment.Production)
                problems.Add("myDiesel: επιλέξτε περιβάλλον.");
            if ((profile.Environment == FiscalEnvironment.Test) != value.TestMode)
                problems.Add("myDiesel: το περιβάλλον δεν συμφωνεί με το κοινό περιβάλλον τιμολόγησης.");
            bool myDieselTest = profile.Environment == FiscalEnvironment.Test;
            var policies = value.Policies == null
                ? new InvoiceTypePolicy[0]
                : value.Policies.Values.ToArray();
            if (policies.Any(policy => policy.Transmission == InvoiceTransmissionMode.Provider) &&
                value.InvoiceProvider != null && value.InvoiceProvider.TestMode != myDieselTest)
                problems.Add("myDiesel: το περιβάλλον του παρόχου τιμολόγησης πρέπει να συμφωνεί με το περιβάλλον myDiesel.");
            if (policies.Any(policy => policy.Transmission == InvoiceTransmissionMode.MyData) &&
                value.DirectMyData != null && value.DirectMyData.TestMode != myDieselTest)
                problems.Add("myDiesel: το περιβάλλον της απευθείας διαβίβασης myDATA πρέπει να συμφωνεί με το περιβάλλον myDiesel.");
            try { MyDieselCouponIdentity.NormalizeVat(profile.CompanyVat,"Company VAT"); }
            catch (ArgumentException ex) { problems.Add("myDiesel: " + ex.Message); }
            if (profile.Environment == FiscalEnvironment.Test && !MyDieselUatCatalog.IsCompanyVat(profile.CompanyVat))
                problems.Add("myDiesel UAT: επιλέξτε έναν από τους επίσημους δοκιμαστικούς ΑΦΜ επιχείρησης της έκδοσης 0.1.");
            if (profile.BranchNum < 0 || string.IsNullOrWhiteSpace(profile.StationName) || string.IsNullOrWhiteSpace(profile.StationAmdika))
                problems.Add("myDiesel: επιλέξτε καταχωρημένο πρατήριο μετά από επιτυχή έλεγχο σύνδεσης.");
            try { MyDieselTaxRules.NormalizeExciseDiscountPerLitre(profile.ExciseDiscountPerLitre); }
            catch (ArgumentOutOfRangeException) { problems.Add("myDiesel: η έκπτωση ΕΦΚ πρέπει να είναι θετική, έως 10 €/λίτρο και με έως τέσσερα δεκαδικά."); }
            if (profile.PollMinutes < 5 || profile.PollMinutes > 1440) problems.Add("myDiesel: η συχνότητα ελέγχου πρέπει να είναι από 5 έως 1440 λεπτά.");
            var credential = (value.MyDieselCredentials ?? new SharedMyDieselCredentialProfile[0]).SingleOrDefault(p => p.Id == profile.CredentialProfileId);
            if (credential == null) problems.Add("myDiesel: λείπει το ιστορικό προφίλ διαπιστευτηρίων.");
            else
            {
                if (credential.Environment != profile.Environment || credential.CompanyVat != profile.CompanyVat)
                    problems.Add("myDiesel: το προφίλ διαπιστευτηρίων δεν συμφωνεί με εταιρεία/περιβάλλον.");
                if (profile.Environment == FiscalEnvironment.Production && (string.IsNullOrWhiteSpace(credential.AadeUserId) || string.IsNullOrWhiteSpace(credential.ProductionSubscriptionKeyOption)))
                    problems.Add("myDiesel: λείπουν χρήστης ΑΑΔΕ ή subscription key παραγωγής.");
            }
            if (!profile.LastReferenceCheckUtc.HasValue) problems.Add("myDiesel: απαιτείται επιτυχής έλεγχος εταιρείας, πρατηρίου και καυσίμων.");
            if (profile.Environment == FiscalEnvironment.Test && profile.ContractVersion != MyDieselUatCatalog.ContractVersion)
                problems.Add("myDiesel UAT: οι ρυθμίσεις πρέπει να αποθηκευτούν με τα επίσημα δεδομένα δοκιμών έκδοσης 0.1.");
            if (profile.Environment == FiscalEnvironment.Production && !SharedMyDieselProductionContract.Matches(profile))
                problems.Add("myDiesel: απαιτείται το ελεγμένο συμβόλαιο παραγωγής " + SharedMyDieselProductionContract.ContractVersion + ".");
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
