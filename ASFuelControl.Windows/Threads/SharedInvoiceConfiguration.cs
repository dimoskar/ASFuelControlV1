using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ASFuelControl.Invoicing;
using ASFuelControl.Invoicing.Mapping;
using ASFuelControl.Invoicing.Persistence;
using Exedron.Invoicing.Core;
using Exedron.Invoicing.Adapters.Samtec;
using Newtonsoft.Json;

namespace ASFuelControl.Windows.Threads
{
    internal sealed class SharedInvoiceRoute
    {
        public ProviderRoute Provider { get; set; }
        public PaymentRoute Payment { get; set; }
        public FiscalSigningScope Signing { get; set; }
        public SharedPrinterProfile Printer { get; set; }
        public bool LocalOnly { get; set; }
    }
    internal sealed class SharedCredentialProfile
    {
        public ProviderRoute Route { get; set; }
        // Values are Option keys, never the secret itself. Retain historical versions.
        public string SubscriptionKeyOption { get; set; }
        public string MasterTokenOption { get; set; }
        public string ApiKeyOption { get; set; }
        public string PasswordOption { get; set; }
    }
    internal sealed class SharedMyDieselCredentialProfile
    {
        public string Id { get; set; }
        public FiscalEnvironment Environment { get; set; }
        public string CompanyVat { get; set; }
        public string AadeUserId { get; set; }
        public string ProductionSubscriptionKeyOption { get; set; }
        public string UatApiKeyOption { get; set; }
    }
    internal sealed class SharedMyDieselProfile
    {
        public bool Enabled { get; set; }
        public decimal ExciseDiscountPerLitre { get; set; } = MyDieselTaxRules.DefaultExciseDiscountPerLitre;
        public FiscalEnvironment Environment { get; set; }
        public string CompanyVat { get; set; }
        public int BranchNum { get; set; }
        public string StationName { get; set; }
        public string StationAmdika { get; set; }
        public string CredentialProfileId { get; set; }
        public string ContractVersion { get; set; }
        public string OpenApiSha256 { get; set; }
        public DateTime? LastReferenceCheckUtc { get; set; }
        public int PollMinutes { get; set; } = 30;
    }
    internal static class SharedMyDieselProductionContract
    {
        // AADE download published with the ERP Guide v1.1 on 29/10/2026.
        // The OpenAPI document itself identifies the interface as v0.1.
        internal const string ContractVersion = "v0.1";
        internal const string OpenApiSha256 = "F1B2E24738D53DD13D4C92282E060472BCCF46634115D435984655B3A08F7403";

        internal static bool Matches(SharedMyDieselProfile profile)
        {
            return profile != null &&
                string.Equals(profile.ContractVersion,ContractVersion,StringComparison.Ordinal) &&
                string.Equals(profile.OpenApiSha256,OpenApiSha256,StringComparison.OrdinalIgnoreCase);
        }
    }
    internal sealed class SharedInvoiceConfiguration : IProviderCredentialsResolver
    {
        private static readonly int[] DefaultWholesaleCodes = { 161, 165, 221, 222, 178 };

        public int Version { get; set; }
        public DateTime FirstInvoiceDate { get; set; }
        public MappingContext Mapping { get; set; }
        public Dictionary<Guid, SharedInvoiceRoute> InvoiceTypes { get; set; }
        public Dictionary<FiscalDocumentKind, SharedInvoiceRoute> Documents { get; set; }
        public SharedCredentialProfile[] Credentials { get; set; }
        public SamtecDeviceProfile[] Devices { get; set; }
        public SharedMyDieselProfile MyDiesel { get; set; }
        public SharedMyDieselCredentialProfile[] MyDieselCredentials { get; set; }
        public bool Enabled { get; set; }
        public bool TestMode { get; set; } = true;
        public ProviderRoute InvoiceProvider { get; set; }
        public string SelectedProviderKey { get; set; }
        public ProviderRoute DirectMyData { get; set; }
        public PaymentRoute Pos { get; set; }
        public FiscalSigningScope FiscalDevice { get; set; }
        public SharedPrinterProfile DefaultPrinter { get; set; }
        [JsonIgnore] public Dictionary<Guid, InvoiceTypePolicy> Policies { get; set; }
        [JsonIgnore] public HashSet<FiscalDocumentKind> ProtectedKinds { get; set; } = new HashSet<FiscalDocumentKind>();

        internal bool EnsureWholesaleCodes()
        {
            if (Mapping == null)
                Mapping = new MappingContext();
            if (Mapping.WholesaleCodes != null && Mapping.WholesaleCodes.Count != 0)
                return false;
            Mapping.WholesaleCodes = new HashSet<int>(DefaultWholesaleCodes);
            return true;
        }

        internal SharedInvoiceRoute Route(Guid typeId, string printerName, bool laserPrint = false)
        {
            if (Version == 1)
            {
                SharedInvoiceRoute legacy;
                if (InvoiceTypes == null || !InvoiceTypes.TryGetValue(typeId,out legacy)) throw new InvalidOperationException("An explicit invoice-type route is required.");
                return legacy;
            }
            InvoiceTypePolicy policy;
            if (Policies == null || !Policies.TryGetValue(typeId,out policy)) throw new InvalidOperationException("Ελέγξτε και αποθηκεύστε τον τρόπο διαβίβασης και τη σήμανση του τύπου παραστατικού.");
            policy.Validate();
            var provider = policy.Transmission == InvoiceTransmissionMode.Provider ? InvoiceProvider : policy.Transmission == InvoiceTransmissionMode.MyData ? DirectMyData : null;
            if (policy.Transmission != InvoiceTransmissionMode.None && provider == null) throw new InvalidOperationException("Δεν έχουν οριστεί οι κοινές ρυθμίσεις διαβίβασης.");
            if (provider != null && (policy.Transmission == InvoiceTransmissionMode.MyData ? provider.ProviderKey != InvoiceProviderCatalog.MyDataKey : provider.ProviderKey == InvoiceProviderCatalog.MyDataKey))
                throw new InvalidOperationException("Ο πάροχος δεν συμφωνεί με τον τρόπο διαβίβασης.");
            if (policy.Transmission == InvoiceTransmissionMode.Provider && Pos != null && JsonConvert.SerializeObject(Pos.Provider) != JsonConvert.SerializeObject(provider))
                throw new InvalidOperationException("Το POS δεν συμφωνεί με τον κοινό πάροχο.");
            if (policy.SignWithFiscalDevice && FiscalDevice == null) throw new InvalidOperationException("Δεν έχει οριστεί κοινός φορολογικός μηχανισμός.");
            if (provider != null && provider.TestMode != TestMode || policy.SignWithFiscalDevice && (FiscalDevice.Environment == FiscalEnvironment.Test) != TestMode)
                throw new InvalidOperationException("Το προφίλ δεν συμφωνεί με το κοινό περιβάλλον.");
            var printer = JsonConvert.DeserializeObject<SharedPrinterProfile>(JsonConvert.SerializeObject(DefaultPrinter ?? new SharedPrinterProfile()));
            printer.Thermal = !laserPrint;
            // Thermal documents are single-copy. Laser documents use the
            // installation setting, defaulting old/missing profiles to two.
            printer.Copies = laserPrint
                ? SharedPrinterProfile.NormalizeLaserCopies(printer.LaserCopies)
                : (short)1;
            // The configured common printer belongs to laser/full-page invoices.
            // A compact invoice uses only its related dispenser printer and then
            // the Windows default; it must not inherit the configured laser device.
            // For thermal invoices keep only the pump/type destination here.
            // ResolveProfile applies ThermalPrinterName when that destination is
            // empty or no longer installed, followed by the Windows default.
            printer.PrinterName = laserPrint ? printer.PrinterName :
                (string.IsNullOrWhiteSpace(printerName) ? null : printerName.Trim());
            return new SharedInvoiceRoute { Provider = provider, Payment = policy.Transmission == InvoiceTransmissionMode.Provider ? Pos : null,
                Signing = policy.SignWithFiscalDevice ? FiscalDevice : null, Printer = printer, LocalOnly = policy.LocalOnly };
        }
        internal SharedInvoiceRoute Route(Data.Invoice invoice)
        {
            if (invoice == null || invoice.InvoiceType == null)
                throw new InvalidOperationException("Απαιτείται αποθηκευμένος τύπος παραστατικού.");
            return Route(invoice.InvoiceTypeId,PrinterName(invoice),invoice.InvoiceType.IsLaserPrint == true);
        }
        internal SharedInvoiceRoute Route(Data.InvoiceType type)
        {
            if (type == null)
                throw new InvalidOperationException("Απαιτείται τύπος παραστατικού.");
            return Route(type.InvoiceTypeId,null,type.IsLaserPrint == true);
        }
        internal SamtecDeviceProfile SamtecDevice(FiscalSigningScope scope)
        {
            if (scope == null || scope.SignerKey != SamtecFiscalSigner.Key) return null;
            return (Devices ?? new SamtecDeviceProfile[0]).SingleOrDefault(device =>
                FiscalSigningCoordinator.SameScope(device.Scope,scope));
        }
        private bool UsesIntegratedSamtecPos(SharedInvoiceRoute route)
        {
            if (route == null || route.Payment != null || route.Signing == null) return false;
            // Direct myDATA is only the fiscal-submission destination. Samtec still
            // owns the card/IRIS transaction as part of signing, after which the
            // workflow submits the signed invoice to AADE. Invoice-provider routes
            // continue to own their POS flow and must not use the Samtec terminal.
            if (route.Provider != null && route.Provider.ProviderKey != InvoiceProviderCatalog.MyDataKey) return false;
            var device = SamtecDevice(route.Signing);
            return device != null && !string.IsNullOrWhiteSpace(device.EftPosTID);
        }
        internal static string PrinterName(Data.Invoice invoice)
        {
            if (invoice == null || invoice.InvoiceType == null)
                return null;
            if (invoice.InvoiceType.IsLaserPrint == true)
                return null;
            bool saleInvoice = false;
            foreach (var line in invoice.InvoiceLines)
            {
                if (line.SalesTransaction == null)
                    continue;
                saleInvoice = true;
                var dispenser = line.SalesTransaction.Nozzle?.Dispenser;
                var dispenserPrinter = dispenser?.InvoicePrints
                    .FirstOrDefault(item => !string.IsNullOrWhiteSpace(item.Printer));
                if (dispenserPrinter != null)
                    return dispenserPrinter.Printer.Trim();
            }
            // A sale always follows its dispenser route and then the Windows
            // default. Other compact documents may use their invoice-type printer.
            if (saleInvoice || string.IsNullOrWhiteSpace(invoice.InvoiceType.Printer))
                return null;
            return invoice.InvoiceType.Printer.Trim();
        }
        internal static string TransmissionLabel(ProviderRoute provider)
        {
            if (provider == null) return "Χωρίς διαβίβαση";
            if (provider.ProviderKey == InvoiceProviderCatalog.MyDataKey) return "Απευθείας myDATA (ΑΑΔΕ)";
            return "Πάροχος τιμολόγησης: " + (provider.ProviderKey == InvoiceProviderCatalog.DirectIlydaKey ? "Ilyda" : provider.ProviderKey);
        }
        internal static string SpecialWorkflowProblem(Data.InvoiceType type)
        {
            if (type?.ForcesDelivery == true)
                return "Ο τύπος απαιτεί την ειδική ροή ελέγχου δεξαμενής.";
            return null;
        }
        // Shared eligibility for runtime preparation and the read-only status panel.
        internal SharedInvoiceRoute PendingRoute(Data.Invoice invoice, bool hasLegacySubmission, bool historicalPrinted = false)
        {
            DatabaseInvoiceSource.EnsureNoLegacyHistory(invoice,hasLegacySubmission,allowPrinted: true);
            if (invoice.InvoiceType == null || !invoice.InvoiceType.Printable || invoice.InvoiceLines.Count == 0)
                throw new InvalidOperationException("Απαιτείται εκτυπώσιμος τύπος και αποθηκευμένες γραμμές.");
            var problem = SpecialWorkflowProblem(invoice.InvoiceType);
            if (problem != null) throw new InvalidOperationException(problem);
            if (invoice.TransactionDate < FirstInvoiceDate && !(historicalPrinted && invoice.IsPrinted == true))
                throw new InvalidOperationException("Η ημερομηνία παραστατικού είναι πριν από την έναρξη κοινής τιμολόγησης. Για ήδη εκτυπωμένο παραστατικό χρησιμοποιήστε τη Διαβίβαση.");
            var route = Route(invoice);
            bool pos = invoice.PaymentType == 2 || invoice.PaymentType == 4;
            if (invoice.IsPrinted == true && (pos || route.Signing != null))
                throw new InvalidOperationException("Το ήδη εκτυπωμένο παραστατικό χρειάζεται έλεγχο της προηγούμενης πληρωμής / σήμανσης. Δεν θα γίνει νέα χρέωση POS ή σήμανση.");
            if (invoice.InvoiceType.IsCancelation == true && pos)
                throw new InvalidOperationException("Το πιστωτικό με κάρτα / IRIS απαιτεί τη ροή επιστροφής της αρχικής πληρωμής. Δεν επιτρέπεται νέα χρέωση.");
            if (Version == 1 && invoice.InvoiceType.SendToMyData && route.Provider == null)
                throw new InvalidOperationException("The invoice requires electronic submission.");
            if (!route.LocalOnly && route.Provider == null && route.Signing == null)
                throw new InvalidOperationException("An explicit fiscal or local-only policy is required.");
            if (pos && route.Payment == null && !UsesIntegratedSamtecPos(route))
                throw new InvalidOperationException(route.Signing?.SignerKey == SamtecFiscalSigner.Key
                    ? "Η σήμανση Samtec με κάρτα / IRIS απαιτεί την επιλογή εφαρμογής EftPosTID."
                    : "Η πληρωμή κάρτας / IRIS απαιτεί διαδρομή POS μέσω παρόχου ή σήμανση Samtec με EftPosTID.");
            if (invoice.PaymentType == 4 && route.Provider?.ProviderKey == InvoiceProviderCatalog.DirectIlydaKey)
                throw new InvalidOperationException("Η πληρωμή IRIS μέσω Ilyda δεν υποστηρίζεται.");
            return route;
        }
        internal bool CanSubmitPrintedInvoice(Data.Invoice invoice, bool hasLegacySubmission)
        {
            if (!Enabled || invoice?.IsPrinted != true) return false;
            try { return PendingRoute(invoice,hasLegacySubmission,true).Provider != null; }
            catch (InvalidOperationException) { return false; }
        }
        // Read-only explanation of the route for the SAVED invoice, using the same
        // startup configuration as PrintAgent. No numbering, mapping or dispatch.
        internal string PendingInvoiceSummary(Data.Invoice invoice, bool hasLegacySubmission)
        {
            var lines = new List<string> { "Κοινή έκδοση: Δεν ξεκίνησε." };
            lines.Add("Κοινή τιμολόγηση: " + (Enabled ? "Ενεργή." :
                ProtectedKinds.Contains(FiscalDocumentKind.Invoice) ? "Σε παύση. Δεν επιτρέπεται νέα έκδοση." :
                "Ανενεργή στην τρέχουσα εκτέλεση. Η κοινή διαδρομή δεν χρησιμοποιείται ακόμη."));
            if (invoice == null || invoice.InvoiceType == null)
            {
                lines.Add("Αποθηκεύστε το παραστατικό με τον τύπο του για να εμφανιστεί η ρυθμισμένη διαβίβαση.");
                return string.Join("\r\n",lines);
            }
            var type = invoice.InvoiceType;
            try
            {
                var route = Route(invoice);
                lines.Add("Ρυθμισμένη διαβίβαση: " + TransmissionLabel(route.Provider));
                if (route.Provider != null || route.Signing != null)
                {
                    bool test = route.Provider != null ? route.Provider.TestMode : route.Signing.Environment == FiscalEnvironment.Test;
                    lines.Add("Περιβάλλον: " + (test ? "Δοκιμαστικό" : "Παραγωγή"));
                }
                lines.Add("Σήμανση: " + (route.Provider != null && route.Provider.ProviderKey != InvoiceProviderCatalog.MyDataKey ?
                    "Από τον πάροχο" : route.Signing != null ? "Φορολογικός μηχανισμός" : "Δεν απαιτείται"));
                if ((invoice.PaymentType == 2 || invoice.PaymentType == 4) && route.Payment == null && !UsesIntegratedSamtecPos(route))
                    lines.Add("Έλεγχος: Η πληρωμή κάρτας / IRIS απαιτεί διαδρομή POS μέσω παρόχου ή σήμανση Samtec με EftPosTID.");
                if (invoice.PaymentType == 4 && route.Provider?.ProviderKey == InvoiceProviderCatalog.DirectIlydaKey)
                    lines.Add("Έλεγχος: Η πληρωμή IRIS μέσω Ilyda δεν υποστηρίζεται.");
                if (route.Printer == null || string.IsNullOrWhiteSpace(route.Printer.PrinterName))
                    lines.Add("Εκτυπωτής: Προεπιλεγμένος εγκατάστασης / Windows.");
            }
            catch (Exception ex) { lines.Add("Ρυθμισμένη διαβίβαση: Δεν μπορεί να προσδιοριστεί. " + ex.Message); }
            if (invoice.Number == 0) lines.Add("Αρίθμηση: Εκκρεμεί — θα αποθηκευτεί πριν από την αποστολή.");
            if (CanSubmitPrintedInvoice(invoice,hasLegacySubmission))
                lines.Add("Ήδη εκτυπωμένο: Επιλέξτε Διαβίβαση για αποστολή με τον υπάρχοντα αριθμό, σειρά και ημερομηνία, χωρίς νέα εκτύπωση. Η αποδοχή ελέγχεται κατά την αποστολή.");
            else
                try { PendingRoute(invoice,hasLegacySubmission); }
                catch (InvalidOperationException ex) { lines.Add("Έλεγχος: " + ex.Message); }
            return string.Join("\r\n",lines);
        }
        internal ProviderRoute ConfiguredConnection(ProviderRoute saved)
        {
            // Version 1 has per-type routes, without a common connection to refresh.
            if (Version == 1) return saved;
            if (Version != 2 && Version != 3) throw new InvalidOperationException("Unsupported invoicing configuration.");
            var current = saved.ProviderKey == InvoiceProviderCatalog.MyDataKey ? DirectMyData : InvoiceProvider;
            if (current == null) throw new InvalidOperationException("Δεν έχει οριστεί σύνδεση για τη διαβίβαση του παραστατικού.");
            if (current.ProviderKey != saved.ProviderKey || current.TestMode != saved.TestMode || current.IssuerVat != saved.IssuerVat ||
                current.IssuerBranch != saved.IssuerBranch || current.MyDataBaseUrl != saved.MyDataBaseUrl)
                throw new InvalidOperationException("Οι τρέχουσες ρυθμίσεις αλλάζουν πάροχο, περιβάλλον, εκδότη ή διεύθυνση myDATA. Η αποθηκευμένη ροή απαιτεί έλεγχο πριν από τη διαβίβαση.");
            return current;
        }
        public ProviderCredentials Resolve(ProviderRoute route) =>
            Resolve(route,key => Data.Implementation.OptionHandler.Instance.GetOption(key,""));
        internal ProviderCredentials Resolve(ProviderRoute route, Func<string,string> readOption)
        {
            var profile = (Credentials ?? new SharedCredentialProfile[0]).SingleOrDefault(p => JsonConvert.SerializeObject(p.Route) == JsonConvert.SerializeObject(route));
            if (profile == null) throw new InvalidOperationException("The historical provider credential profile is unavailable.");
            Func<string, string> read = key => string.IsNullOrWhiteSpace(key) ? null : readOption(key);
            return new ProviderCredentials { SubscriptionKey = read(profile.SubscriptionKeyOption), MasterToken = read(profile.MasterTokenOption),
                ApiKey = read(profile.ApiKeyOption), Password = read(profile.PasswordOption) };
        }
    }
}
