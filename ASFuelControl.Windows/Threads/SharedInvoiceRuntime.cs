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
    internal static partial class SharedInvoiceRuntime
    {
        // Routing/device settings are stable for this process. Unused provider
        // connections read the latest saved account and credential profile.
        internal static bool Enabled => settings.Value.Enabled;
        internal static bool ProtectsInvoices => Enabled || settings.Value.ProtectedKinds.Contains(FiscalDocumentKind.Invoice);
        internal static bool SupportsIris => settings.Value.Pos?.Provider.ProviderKey == InvoiceProviderCatalog.ITimologiaKey && settings.Value.Pos.PosMode == "MELLON";
        private static readonly Lazy<SqlFiscalDatabase> sql = new Lazy<SqlFiscalDatabase>(() => new SqlFiscalDatabase(Properties.Settings.Default.DBConnection));
        private static readonly Lazy<SharedInvoiceConfiguration> settings = new Lazy<SharedInvoiceConfiguration>(() => SharedInvoicingSettings.LoadRuntime());
        private static SharedInvoiceConfiguration CurrentSettings()
        {
            long revision;
            return SharedInvoicingSettings.Load(out revision);
        }
        private static ProviderRoute CurrentConnection(ProviderRoute saved) => CurrentSettings().ConfiguredConnection(saved);
        private static CompanySettings ReadHostCompany()
        {
            var options=Data.Implementation.OptionHandler.Instance;
            return new CompanySettings { CompanyTaxId=options.GetOption("CompanyTIN"),
                CompanyBranch=options.GetIntOption("CompanyBranch",0).ToString(System.Globalization.CultureInfo.InvariantCulture),
                CompanyName=options.GetOption("CompanyName"),CompanyAddress=options.GetOption("CompanyAddress"),
                CompanyCity=options.GetOption("CompanyCity"),CompanyZipCode=options.GetOption("CompanyPostalCode"),
                CompanyTaxOffice=options.GetOption("CompanyTaxOffice"),CompanyEmail=options.GetOption("CompanyEmail"),
                CompanyPhone=options.GetOption("CompanyPhone"),CompanyJob=options.GetOption("CompanyOccupation") };
        }
        private static PaymentRoute CurrentPaymentConnection(PaymentRoute saved)
        {
            var current = CurrentSettings();
            if (current.Version == 1) return saved;
            var pos = current.Pos ?? throw new InvalidOperationException("Δεν έχει οριστεί POS στις ρυθμίσεις τιμολόγησης.");
            return new PaymentRoute(current.ConfiguredConnection(saved.Provider), pos.Endpoint, pos.TerminalId,
                pos.ITimologiaSignatureFormat, pos.IlydaProtocol, pos.PosMode);
        }
        private sealed class CurrentCredentials : IProviderCredentialsResolver
        {
            // The latest configuration retains historical profiles. Resolve the
            // exact requested route so recovery never switches accounts/secrets.
            public ProviderCredentials Resolve(ProviderRoute route)
            {
                var current = CurrentSettings();
                // OptionHandler caches keys per process. Another settings window
                // may have saved a new immutable key since this worker started.
                using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
                    return current.Resolve(route,key => {
                        var stored = db.Options.Where(o => o.OptionKey == key).Select(o => o.OptionValue).SingleOrDefault();
                        if (stored == null) return "";
                        try { return Data.Implementation.OptionHandler.Instance.DecryptValue(stored); }
                        // Match OptionHandler's support for retained plaintext options.
                        catch (System.Security.Cryptography.CryptographicException) { return stored; }
                        catch (FormatException) { return stored; }
                    });
            }
        }
        private static readonly Lazy<InvoiceWorkflowService> workflow = new Lazy<InvoiceWorkflowService>(() => {
            var database = sql.Value;
            var providers = new ProviderSubmissionService(database, new CurrentCredentials());
            var payments = new ProviderPaymentService(database, new CurrentCredentials(), currentPaymentRoute: CurrentPaymentConnection);
            var signing = new FiscalSigningService(database, scope => {
                var device = (settings.Value.Devices ?? new SamtecDeviceProfile[0]).SingleOrDefault(d => FiscalSigningCoordinator.SameScope(d.Scope, scope));
                if (device == null) throw new InvalidOperationException("The historical Samtec profile is unavailable.");
                return new SamtecFiscalSigner(device);
            });
            return new InvoiceWorkflowService(database, providers, payments, signing,
                new FiscalPrintService(database, new SharedInvoicePrinting(() => settings.Value.DefaultPrinter)), SharedInvoicePrinting.Render, Project, CurrentConnection,
                new SharedMyDieselRedemptionCoordinator(database));
        });
        internal static bool Handles(Guid invoiceId)
        {
            if (ProtectsInvoices) return true;
            return Owns(invoiceId);
        }
        internal static bool Owns(Guid invoiceId)
        {
            // Disabling the rollout option cannot send an owned invoice down legacy code.
            using (var lease = sql.Value.EnterInvoice(invoiceId)) return new SqlFiscalOwnershipStore(sql.Value).Owns(lease);
        }
        internal static IDisposable EditUnissued(Guid invoiceId) => new SqlFiscalOwnershipStore(sql.Value).AcquireUnissuedInvoice(invoiceId);
        internal static IDisposable EditUnissued(IEnumerable<Guid> ids)
        {
            var group = new EditLeases();
            try
            {
                foreach (var id in ids.Where(i => i != Guid.Empty).Distinct().OrderBy(i => i)) group.Leases.Add(EditUnissued(id));
                return group;
            }
            catch { group.Dispose(); throw; }
        }
        private sealed class EditLeases : IDisposable
        {
            internal readonly List<IDisposable> Leases = new List<IDisposable>();
            public void Dispose() { for (int i = Leases.Count - 1; i >= 0; i--) Leases[i].Dispose(); }
        }
        internal static InvoiceWorkflowResult Process(Guid id, bool print = true, string reason = null)
        {
            if (new SqlHeatingCorrectionStore(sql.Value).IsChild(id,true))
                throw new InvalidOperationException("Το παραστατικό ελέγχεται από την παρτίδα διόρθωσης επιδόματος θέρμανσης. Συνεχίστε την παρτίδα από τις Ενέργειες του καταλόγου.");
            return workflow.Value.Process(id, new Source(print), print, reason);
        }
        internal static InvoiceWorkflowResult SubmitPrintedInvoice(Guid id)
        {
            // Explicit operator action: a pre-cutover printed invoice is not picked
            // up automatically and must never be renumbered, re-signed or charged.
            using (var lease = sql.Value.EnterInvoice(id))
            {
                if (new SqlFiscalOwnershipStore(sql.Value).Owns(lease))
                    throw new InvalidOperationException("Η κοινή έκδοση έχει ήδη ξεκινήσει. Ανανεώστε τη σύνοψη και συνεχίστε την αποθηκευμένη ροή.");
                return workflow.Value.Process(id,new Source(false,true),false);
            }
        }
        private sealed class Source : IInvoiceWorkflowSource
        {
            private readonly bool printRequested, historicalPrinted;
            internal Source(bool printRequested, bool historicalPrinted = false)
            { this.printRequested = printRequested; this.historicalPrinted = historicalPrinted; }
            public InvoiceWorkflowSnapshot Prepare(IFiscalDocumentLease lease)
            {
                if (!Enabled) throw new InvalidOperationException("First issuance through the shared workflow is disabled.");
                var id = lease.Document.DocumentId;
                var config = settings.Value;
                // Check route/history before touching the counter. Open a new context after
                // SQL numbering; an old OpenAccess identity map must never render number 0.
                bool needsNumber;
                using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
                {
                    var pending = Check(db,id,config);
                    ValidateMyDiesel(pending,config.Route(pending.InvoiceTypeId,pending.InvoiceType.Printer),config);
                    needsNumber = pending.Number == 0;
                }
                if (needsNumber) new SqlInvoiceWorkflowStore(sql.Value).AllocateNumbers(lease,config.FirstInvoiceDate);
                using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
                {
                    var invoice = Check(db, id, config);
                    var route = config.Route(invoice.InvoiceTypeId, invoice.InvoiceType.Printer);
                    var coupon = ValidateMyDiesel(invoice,route,config);
                    var printer = printRequested ? SharedInvoicePrinting.ResolveProfile(route.Printer,config.DefaultPrinter) :
                        route.Printer ?? config.DefaultPrinter ?? new SharedPrinterProfile();
                    var content = PrintAgent.RenderSharedInvoiceContent(db, invoice);
                    var value = new InvoiceWorkflowSnapshot { InvoiceId = id, Provider = route.Provider, LocalOnly = route.LocalOnly,
                        Payment = invoice.PaymentType == 2 || invoice.PaymentType == 4 ? route.Payment : null,
                        PrintContent = Encoding.UTF8.GetBytes(content), PrintDestination = JsonConvert.SerializeObject(printer), PrintFormat = SharedInvoicePrinting.Format };
                    if (coupon != null)
                    {
                        value.Version = 2; value.MyDieselCouponCaseId = coupon.CouponCaseId;
                        value.MyDieselScope = new MyDieselScope(coupon.Environment,coupon.CompanyVat,coupon.ReservedBranchNum,coupon.CredentialProfileId);
                        value.MyDieselExciseDiscountPerLitre = coupon.ExciseDiscountPerLitre;
                    }
                    if (route.Provider != null)
                    {
                        var mapping = JsonConvert.DeserializeObject<MappingContext>(JsonConvert.SerializeObject(config.Mapping));
                        if (mapping == null) throw new InvalidOperationException("Explicit mapping configuration is required.");
                        // The same host company fields are used by CreateInvoiceText.
                        // Freeze them in this snapshot, rather than printing a different
                        // issuer from a separately maintained JSON company definition.
                        mapping.Company = ReadHostCompany();
                        mapping.Environment = route.Provider.TestMode ? FiscalEnvironment.Test : FiscalEnvironment.Production;
                        var provider = new ProviderSubmissionService(sql.Value, config);
                        foreach (var parent in invoice.ParentInvoiceRelations.Where(relation =>
                            relation.IsExternalFiscalCorrelation))
                            mapping.Parents[parent.ParentInvoiceId] = provider.LoadParentEvidence(parent.ParentInvoiceId);
                        value.Invoice = new HostInvoiceMapper().Map(invoice, mapping);
                    }
                    if (route.Signing != null) value.Signing = HostFiscalSigningMapper.Invoice(invoice, route.Signing, content, false);
                    return value;
                }
            }
            private MyDieselCouponCaseRecord ValidateMyDiesel(Data.Invoice invoice, SharedInvoiceRoute route, SharedInvoiceConfiguration config)
            {
                var coupon = new SqlMyDieselStore(sql.Value).FindBlockingByInvoice(invoice.InvoiceId);
                if (coupon == null) return null;
                if (coupon.LocalStage != MyDieselCaseStage.Reserved || MyDieselCouponStates.Parse(coupon.RemoteStatus) != MyDieselCouponStatus.Reserved)
                    throw new InvalidOperationException("Το κουπόνι myDiesel δεν έχει επιβεβαιωμένη κατάσταση RESERVED. Ολοκληρώστε πρώτα την ανάκτηση ή αποδέσμευση.");
                if (invoice.Trader == null || MyDieselCouponIdentity.NormalizeVat(invoice.Trader.TaxRegistrationNumber,"Invoice trader VAT") != coupon.BeneficiaryVat)
                    throw new InvalidOperationException("Το ΑΦΜ του πελάτη δεν συμφωνεί με το ΑΦΜ δικαιούχου του κουπονιού myDiesel.");
                if (route.LocalOnly || route.Provider == null)
                    throw new InvalidOperationException("Το myDiesel απαιτεί ηλεκτρονική διαβίβαση που επιστρέφει οριστικό MARK.");
                if ((route.Provider.TestMode ? FiscalEnvironment.Test : FiscalEnvironment.Production) != coupon.Environment || route.Provider.IssuerVat != coupon.CompanyVat)
                    throw new InvalidOperationException("Το περιβάλλον ή το ΑΦΜ εκδότη του παραστατικού διαφέρει από τη δεσμευμένη κλήση myDiesel.");
                decimal litres = invoice.InvoiceLines.Sum(line => Math.Abs(line.Volume));
                if (litres <= 0) throw new InvalidOperationException("Το παραστατικό myDiesel πρέπει να περιέχει θετική ποσότητα καυσίμου σε λίτρα.");
                if (!coupon.FuelCode.HasValue || invoice.InvoiceLines.Any(line =>
                    line.FuelType == null || line.FuelType.EnumeratorValue != coupon.FuelCode.Value))
                    throw new InvalidOperationException("Το καύσιμο του παραστατικού δεν συμφωνεί με το επιλέξιμο καύσιμο του κουπονιού myDiesel.");
                if (coupon.IssuedLitres.HasValue && litres > coupon.IssuedLitres.Value)
                    throw new InvalidOperationException("Τα λίτρα του παραστατικού υπερβαίνουν τα διαθέσιμα λίτρα του κουπονιού myDiesel. Διαχωρίστε την επιπλέον ποσότητα.");
                var profile = config.MyDiesel;
                if (profile == null || profile.CompanyVat != coupon.CompanyVat || profile.Environment != coupon.Environment)
                    throw new InvalidOperationException("Οι τρέχουσες ρυθμίσεις myDiesel δεν συμφωνούν με τη δεσμευμένη υπόθεση.");
                return coupon;
            }
            private Data.Invoice Check(Data.DatabaseModel db, Guid id, SharedInvoiceConfiguration config)
            {
                var invoice = db.Invoices.Single(i => i.InvoiceId == id);
                bool hasLegacy = db.MyDataInvoices.Any(i => i.InvoiceId == id);
                // Older InvoiceFormEx/InvoiceSimpleForm builds saved a newly created
                // invoice as printed before assigning its number. Recover that exact
                // impossible first-issue state only during an explicit print request
                // and only when no external fiscal evidence exists.
                if (printRequested && !historicalPrinted && invoice.Number == 0 && invoice.IsPrinted == true && !hasLegacy &&
                    string.IsNullOrWhiteSpace(invoice.InvoiceSignature) && string.IsNullOrWhiteSpace(invoice.QRCodeData))
                {
                    invoice.IsPrinted = false;
                    db.SaveChanges();
                }
                if (historicalPrinted && !config.CanSubmitPrintedInvoice(invoice,hasLegacy))
                    throw new InvalidOperationException("Το παραστατικό δεν είναι διαθέσιμο για διαβίβαση ήδη εκτυπωμένου. Ελέγξτε τη σύνοψη και τα προηγούμενα φορολογικά στοιχεία.");
                var route = config.PendingRoute(invoice,hasLegacy,historicalPrinted);
                if (route.Signing != null && (route.Signing.IssuerVat != Data.Implementation.OptionHandler.Instance.GetOption("CompanyTIN") ||
                    route.Signing.IssuerBranch != Data.Implementation.OptionHandler.Instance.GetIntOption("CompanyBranch",0)))
                    throw new InvalidOperationException("The signing profile differs from the host issuer/branch.");
                if (printRequested) SharedInvoicePrinting.ResolveProfile(route.Printer,config.DefaultPrinter);
                else SharedInvoicePrinting.CheckProfile(route.Printer ?? config.DefaultPrinter ?? new SharedPrinterProfile());
                return invoice;
            }
        }
        private static void Project(Guid invoiceId, InvoiceWorkflowResult result)
        {
            if (!result.ReadyToPrint) return;
            using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                var invoice = db.Invoices.Single(i => i.InvoiceId == invoiceId);
                var response = result.Response;
                if (response != null)
                {
                    if (!string.IsNullOrWhiteSpace(response.InvoiceSignature)) invoice.InvoiceSignature = response.InvoiceSignature;
                    invoice.QRCodeData = response.QrCodeUrl;
                    long mark;
                    if (long.TryParse(response.Mark, out mark) && mark > 0)
                    {
                        var row = db.MyDataInvoices.SingleOrDefault(i => i.InvoiceId == invoiceId);
                        if (row == null) { row = new Data.MyDataInvoice { MyDataInvoiceId = Guid.NewGuid(), InvoiceId = invoiceId, DateTimeSent = DateTime.Now }; db.Add(row); }
                        row.Mark = mark; row.Uid = response.Uid; row.Status = 3;
                        row.QrCodeUrl = response.QrCodeUrl; row.VerificationHash = response.VerificationHash;
                        row.ProviderUrl = response.ProviderUrl; row.InvoiceUrl = response.InvoiceUrl;
                        row.Errors = ""; // New workflows never encode print commands in Errors.
                    }
                }
                // A failed copy does not erase an earlier successful handoff.
                if (result.Print?.State == "HandedOff" || result.Print?.State == "Confirmed") invoice.IsPrinted = true;
                else if (result.Print != null && invoice.IsPrinted != true) invoice.IsPrinted = false;
                db.SaveChanges();
            }
        }
    }
}
