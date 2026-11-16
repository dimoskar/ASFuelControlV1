using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ASFuelControl.Invoicing;
using ASFuelControl.Invoicing.Persistence;
using Exedron.Invoicing.Adapters;
using Exedron.Invoicing.Core;
using Newtonsoft.Json;

namespace ASFuelControl.Windows.Threads
{
    internal sealed class SharedMyDieselStatus
    {
        internal MyDieselCouponCaseRecord CouponCase { get; set; }
        internal MyDieselAttemptRecord[] Attempts { get; set; } = new MyDieselAttemptRecord[0];
        internal MyDieselHttpEvidenceRecord[] HttpEvidence { get; set; } = new MyDieselHttpEvidenceRecord[0];
        internal bool CanAttachNewCoupon { get; set; }
        internal bool HistoricalReassignmentSource { get; set; }
        internal string HistoricalCouponMark { get; set; }
    }

    internal sealed class SharedMyDieselReassignmentCandidate
    {
        internal MyDieselCouponCaseRecord CouponCase { get; set; }
        internal string InvoiceReference { get; set; }

        public override string ToString()
        {
            MyDieselCouponCaseRecord value = CouponCase;
            return value.CouponCode + " · ΑΦΜ " + value.BeneficiaryVat +
                " · " + InvoiceReference + " · MARK " + value.InvoiceMark;
        }
    }

    internal sealed class SharedMyDieselClientFactory : IMyDieselClientFactory
    {
        private readonly SharedInvoiceConfiguration configuration;
        private readonly SqlFiscalDatabase database;
        private readonly Func<string,string> readOption;

        internal SharedMyDieselClientFactory(SharedInvoiceConfiguration configuration, SqlFiscalDatabase database, Func<string,string> optionReader = null)
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.database = database ?? throw new ArgumentNullException(nameof(database));
            readOption = optionReader ?? (key => Data.Implementation.OptionHandler.Instance.GetOption(key,""));
        }

        public IMyDieselClient Create(MyDieselCouponCaseRecord couponCase)
        {
            if (couponCase == null) throw new ArgumentNullException(nameof(couponCase));
            var credential = (configuration.MyDieselCredentials ?? new SharedMyDieselCredentialProfile[0])
                .SingleOrDefault(p => p.Id == couponCase.CredentialProfileId);
            if (credential == null) throw new InvalidOperationException("Δεν είναι διαθέσιμο το ιστορικό προφίλ διαπιστευτηρίων myDiesel.");
            if (credential.Environment != couponCase.Environment || credential.CompanyVat != couponCase.CompanyVat)
                throw new InvalidOperationException("Το ιστορικό προφίλ myDiesel δεν συμφωνεί με την αποθηκευμένη εταιρεία/περιβάλλον.");
            // Profiles created before versioned myDATA credentials stored the
            // legacy option name in AadeUserId. Keep those historical coupon
            // cases recoverable while new profiles retain the actual user id.
            string productionUser = credential.Environment == FiscalEnvironment.Production ?
                (credential.AadeUserId == "MyDataUserName" ? Read(credential.AadeUserId) : credential.AadeUserId) : null;
            string productionKey = credential.Environment == FiscalEnvironment.Production ? Read(credential.ProductionSubscriptionKeyOption) : null;
            if (credential.Environment == FiscalEnvironment.Production && string.IsNullOrWhiteSpace(productionUser))
                throw new InvalidOperationException("Λείπει ο αποθηκευμένος χρήστης myDATA για το myDiesel.");
            // The myDiesel UAT ERP key is a public, shared test fixture distributed
            // with the official examples. UAT deliberately does not reuse myDATA
            // test credentials or a secret saved by an older configuration.
            string uatKey = credential.Environment == FiscalEnvironment.Test ? MyDieselUatCatalog.PrimaryErpApiKey : null;
            return new MyDieselClient(new MyDieselClientOptions {
                Scope = new MyDieselScope(couponCase.Environment,couponCase.CompanyVat,couponCase.ReservedBranchNum,
                    couponCase.CredentialProfileId,couponCase.ReservedStationAmdika),
                AadeUserId = productionUser, ProductionSubscriptionKey = productionKey, UatApiKey = uatKey,
                CouponCaseId = couponCase.CouponCaseId, InvoiceId = couponCase.InvoiceId, Logger = new SqlEvidenceLogger(database)
            });
        }

        private string Read(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return null;
            string value = readOption(key);
            if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException("Λείπει αποθηκευμένο κλειδί myDiesel: " + key);
            return value;
        }
    }

    internal sealed class SharedMyDieselRedemptionCoordinator : IMyDieselRedemptionCoordinator
    {
        private readonly MyDieselCouponService service;
        internal SharedMyDieselRedemptionCoordinator(SqlFiscalDatabase database)
        {
            var configuration = SharedInvoicingSettings.LoadRuntime();
            service = new MyDieselCouponService(database,new SharedMyDieselClientFactory(configuration,database));
        }
        public MyDieselServiceResult RedeemOrRecover(Guid couponCaseId, Guid invoiceId, string mark)
        {
            return service.RedeemOrRecover(couponCaseId,invoiceId,mark);
        }
    }

    internal static class SharedMyDieselRuntime
    {
        private const string ProviderRouteRequired = "Το κουπόνι myDiesel επιτρέπεται μόνο σε παραστατικό που διαβιβάζεται μέσω παρόχου τιμολόγησης. Δεν επιτρέπεται στην απευθείας διαβίβαση myDATA.";

        internal static string InvoiceTypeEligibilityProblem(Guid invoiceTypeId)
        {
            if (invoiceTypeId == Guid.Empty)
            {
                return "Επιλέξτε τύπο παραστατικού πριν χρησιμοποιήσετε κουπόνι myDiesel.";
            }

            SharedInvoiceConfiguration configuration = SharedInvoicingSettings.LoadRuntime();
            SharedInvoiceRoute route = configuration.Route(invoiceTypeId, null);
            return RouteEligibilityProblem(route);
        }

        internal static string InvoiceEligibilityProblem(Guid invoiceId)
        {
            if (invoiceId == Guid.Empty) return "Δεν έχει δημιουργηθεί παραστατικό.";
            using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                var invoice = db.Invoices.SingleOrDefault(value => value.InvoiceId == invoiceId);
                if (invoice == null) return "Δεν βρέθηκε το παραστατικό.";
                SharedInvoiceConfiguration configuration = SharedInvoicingSettings.LoadRuntime();
                SharedInvoiceRoute route = configuration.Route(invoice);
                string routeProblem = RouteEligibilityProblem(route);
                if (routeProblem != null)
                {
                    return routeProblem;
                }
                if (!invoice.TraderId.HasValue)
                    return "Επιλέξτε πελάτη πριν χρησιμοποιήσετε κουπόνι myDiesel.";

                var lines = invoice.InvoiceLines.ToArray();
                if (lines.Length == 0 || lines.Any(value => value.Volume <= 0))
                    return "Το myDiesel απαιτεί θετική ποσότητα λίτρων.";

                var fuelCodes = lines.Select(value => value.FuelType == null
                    ? (int?)null
                    : value.FuelType.EnumeratorValue).ToArray();
                if (fuelCodes.Any(value => !value.HasValue || !MyDieselFuelRules.IsEligible(value.Value)))
                    return "Το επιλεγμένο καύσιμο δεν είναι επιλέξιμο για myDiesel.";
                if (fuelCodes.Select(value => value.Value).Distinct().Count() != 1)
                    return "Το παραστατικό πρέπει να περιέχει ένα μόνο επιλέξιμο καύσιμο myDiesel.";
            }
            return null;
        }

        private static string RouteEligibilityProblem(SharedInvoiceRoute route)
        {
            if (route == null || route.LocalOnly || route.Provider == null ||
                route.Provider.ProviderKey == InvoiceProviderCatalog.MyDataKey)
            {
                return ProviderRouteRequired;
            }

            return null;
        }

        internal static MyDieselCouponService CreateService(SharedInvoiceConfiguration configuration = null)
        {
            var current = configuration ?? SharedInvoicingSettings.LoadRuntime();
            var database = new SqlFiscalDatabase(Properties.Settings.Default.DBConnection);
            return new MyDieselCouponService(database,new SharedMyDieselClientFactory(current,database));
        }

        internal static SharedMyDieselReassignmentCandidate[] ReassignmentCandidates(
            Guid replacementInvoiceId, string beneficiaryVat)
        {
            if (replacementInvoiceId == Guid.Empty)
            {
                throw new ArgumentException("Replacement invoice ID is required.", nameof(replacementInvoiceId));
            }
            string normalizedVat = MyDieselCouponIdentity.NormalizeVat(beneficiaryVat,
                "Invoice trader VAT");
            var database = new SqlFiscalDatabase(Properties.Settings.Default.DBConnection);
            var store = new SqlMyDieselStore(database);
            IReadOnlyList<MyDieselCouponCaseRecord> available = store.LoadReassignmentCandidates();
            var result = new List<SharedMyDieselReassignmentCandidate>();
            using (var host = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                foreach (MyDieselCouponCaseRecord coupon in available)
                {
                    bool alreadyReleased = coupon.LocalStage == MyDieselCaseStage.ReassignmentAvailable;
                    if (coupon.BeneficiaryVat != normalizedVat ||
                        !alreadyReleased && !MyDieselCouponService.CanReleaseForReassignment(coupon))
                    {
                        continue;
                    }

                    InvoiceWorkflowSnapshot snapshot = ReadWorkflow(database, coupon.InvoiceId);
                    bool directMyData = snapshot != null && snapshot.Provider != null &&
                        snapshot.Provider.ProviderKey == InvoiceProviderCatalog.MyDataKey;
                    bool currentlyCoupled = directMyData &&
                        snapshot.MyDieselCouponCaseId == coupon.CouponCaseId;
                    MyDieselDetachmentAudit priorRelease = directMyData
                        ? snapshot.MyDieselDetachments?.LastOrDefault(audit =>
                            audit.CouponCaseId == coupon.CouponCaseId)
                        : null;
                    bool previouslyReleased = priorRelease != null &&
                        (priorRelease.ReplacementInvoiceId == Guid.Empty ||
                         priorRelease.ReplacementInvoiceId == replacementInvoiceId);
                    if (!currentlyCoupled && !previouslyReleased)
                    {
                        continue;
                    }

                    Data.Invoice invoice = host.Invoices.SingleOrDefault(value =>
                        value.InvoiceId == coupon.InvoiceId);
                    if (invoice == null)
                    {
                        continue;
                    }

                    string invoiceReference = (invoice.Series ?? string.Empty) + " " +
                        invoice.Number + " · " + invoice.TransactionDate.ToString("dd/MM/yyyy");
                    result.Add(new SharedMyDieselReassignmentCandidate
                    {
                        CouponCase = coupon,
                        InvoiceReference = invoiceReference.Trim()
                    });
                }
            }

            return result.ToArray();
        }

        private static InvoiceWorkflowSnapshot ReadWorkflow(SqlFiscalDatabase database,
            Guid invoiceId)
        {
            using (IFiscalDocumentLease lease = database.EnterInvoice(invoiceId))
            {
                var workflowStore = new SqlInvoiceWorkflowStore(database);
                byte[] bytes = workflowStore.Load(lease);
                if (bytes == null)
                {
                    return null;
                }

                string json = Encoding.UTF8.GetString(bytes);
                return JsonConvert.DeserializeObject<InvoiceWorkflowSnapshot>(json);
            }
        }

        internal static MyDieselScope NewReservationScope(SharedInvoiceConfiguration configuration)
        {
            var profile = configuration == null ? null : configuration.MyDiesel;
            if (profile == null || !profile.Enabled) throw new InvalidOperationException("Η δημιουργία νέων δεσμεύσεων myDiesel δεν είναι ενεργή.");
            string amdika = Data.Implementation.OptionHandler.Instance.GetOption("AMDIKA", "");
            if (string.IsNullOrWhiteSpace(amdika))
                throw new InvalidOperationException("Δεν έχει οριστεί ΑΜΔΙΚΑ στις γενικές ρυθμίσεις.");
            return new MyDieselScope(profile.Environment,profile.CompanyVat,profile.BranchNum,profile.CredentialProfileId,amdika);
        }

        internal static SharedMyDieselStatus ReadInvoice(Guid invoiceId, bool includeHistory = false)
        {
            if (invoiceId == Guid.Empty) return new SharedMyDieselStatus();
            var database = new SqlFiscalDatabase(Properties.Settings.Default.DBConnection);
            var store = new SqlMyDieselStore(database);
            bool canAttachNewCoupon = store.CanAttachNewCoupon(invoiceId);
            InvoiceWorkflowSnapshot snapshot = ReadWorkflow(database, invoiceId);
            MyDieselDetachmentAudit detachment = snapshot?.MyDieselDetachments?
                .LastOrDefault(audit => audit.CouponCaseId != Guid.Empty);
            bool historicalReassignmentSource = detachment != null &&
                !snapshot.MyDieselCouponCaseId.HasValue;
            MyDieselCouponCaseRecord coupon = historicalReassignmentSource
                ? null
                : store.FindLatestByInvoice(invoiceId);
            if (historicalReassignmentSource)
            {
                using (var historicalLease = store.Acquire(detachment.CouponCaseId))
                {
                    coupon = store.Load(historicalLease);
                }
            }
            if (coupon == null) return new SharedMyDieselStatus { CanAttachNewCoupon = canAttachNewCoupon };
            using (var lease = store.Acquire(coupon.CouponCaseId))
            {
                coupon = store.Load(lease);
                return new SharedMyDieselStatus {
                    CouponCase = coupon,
                    CanAttachNewCoupon = canAttachNewCoupon,
                    HistoricalReassignmentSource = historicalReassignmentSource,
                    HistoricalCouponMark = detachment?.CouponMark,
                    Attempts = store.LoadAttempts(lease,null,includeHistory).ToArray(),
                    HttpEvidence = includeHistory ? store.LoadHttpEvidence(coupon.CouponCaseId).ToArray() : new MyDieselHttpEvidenceRecord[0]
                };
            }
        }
    }
}
