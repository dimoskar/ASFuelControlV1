using System;
using System.Linq;
using ASFuelControl.Invoicing;
using ASFuelControl.Invoicing.Persistence;
using Exedron.Invoicing.Adapters;
using Exedron.Invoicing.Core;

namespace ASFuelControl.Windows.Threads
{
    internal sealed class SharedMyDieselStatus
    {
        internal MyDieselCouponCaseRecord CouponCase { get; set; }
        internal MyDieselAttemptRecord[] Attempts { get; set; } = new MyDieselAttemptRecord[0];
        internal MyDieselHttpEvidenceRecord[] HttpEvidence { get; set; } = new MyDieselHttpEvidenceRecord[0];
        internal bool CanAttachNewCoupon { get; set; }
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
            string productionKey = Read(credential.ProductionSubscriptionKeyOption);
            // The myDiesel UAT ERP key is a public, shared test fixture distributed
            // with the official examples. UAT deliberately does not reuse myDATA
            // test credentials or a secret saved by an older configuration.
            string uatKey = credential.Environment == FiscalEnvironment.Test ? MyDieselUatCatalog.PrimaryErpApiKey : null;
            return new MyDieselClient(new MyDieselClientOptions {
                Scope = new MyDieselScope(couponCase.Environment,couponCase.CompanyVat,couponCase.ReservedBranchNum,couponCase.CredentialProfileId),
                AadeUserId = credential.AadeUserId, ProductionSubscriptionKey = productionKey, UatApiKey = uatKey,
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
        internal static MyDieselCouponService CreateService(SharedInvoiceConfiguration configuration = null)
        {
            var current = configuration ?? SharedInvoicingSettings.LoadRuntime();
            var database = new SqlFiscalDatabase(Properties.Settings.Default.DBConnection);
            return new MyDieselCouponService(database,new SharedMyDieselClientFactory(current,database));
        }

        internal static MyDieselScope NewReservationScope(SharedInvoiceConfiguration configuration)
        {
            var profile = configuration == null ? null : configuration.MyDiesel;
            if (profile == null || !profile.Enabled) throw new InvalidOperationException("Η δημιουργία νέων δεσμεύσεων myDiesel δεν είναι ενεργή.");
            return new MyDieselScope(profile.Environment,profile.CompanyVat,profile.BranchNum,profile.CredentialProfileId);
        }

        internal static SharedMyDieselStatus ReadInvoice(Guid invoiceId, bool includeHistory = false)
        {
            if (invoiceId == Guid.Empty) return new SharedMyDieselStatus();
            var database = new SqlFiscalDatabase(Properties.Settings.Default.DBConnection);
            var store = new SqlMyDieselStore(database);
            bool canAttachNewCoupon = store.CanAttachNewCoupon(invoiceId);
            var coupon = store.FindLatestByInvoice(invoiceId);
            if (coupon == null) return new SharedMyDieselStatus { CanAttachNewCoupon = canAttachNewCoupon };
            using (var lease = store.Acquire(coupon.CouponCaseId))
            {
                coupon = store.Load(lease);
                return new SharedMyDieselStatus {
                    CouponCase = coupon,
                    CanAttachNewCoupon = canAttachNewCoupon,
                    Attempts = store.LoadAttempts(lease,null,includeHistory).ToArray(),
                    HttpEvidence = includeHistory ? store.LoadHttpEvidence(coupon.CouponCaseId).ToArray() : new MyDieselHttpEvidenceRecord[0]
                };
            }
        }
    }
}
