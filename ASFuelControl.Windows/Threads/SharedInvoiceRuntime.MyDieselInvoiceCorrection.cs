using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ASFuelControl.Invoicing;
using ASFuelControl.Invoicing.Mapping;
using ASFuelControl.Invoicing.Persistence;
using Exedron.Invoicing.Adapters.Samtec;
using Exedron.Invoicing.Core;
using Exedron.Invoicing.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ASFuelControl.Windows.Threads
{
    internal sealed class MyDieselInvoiceCorrectionRow
    {
        public bool Selected { get; set; }
        public string Voucher { get; set; }
        public string Document { get; set; }
        public DateTime Date { get; set; }
        public string Customer { get; set; }
        public string VatNumber { get; set; }
        public string Provider { get; set; }
        public string Mark { get; set; }
        public string CouponStatus { get; set; }
        public decimal OriginalAmount { get; set; }
        public decimal Amount { get; set; }
        public string Credit { get; set; }
        public string Replacement { get; set; }
        public string Status { get; set; }
        public bool Eligible { get; set; }
        internal Guid Id { get; set; }
        internal MyDieselInvoiceCorrectionPlan Plan { get; set; }
    }

    internal sealed class MyDieselCorrectionInvoiceTypeChoice
    {
        public Guid Id { get; set; }
        public string Display { get; set; }
    }

    internal static partial class SharedInvoiceRuntime
    {
        internal static bool IsMyDieselInvoiceCorrectionChild(Guid invoiceId)
        {
            var store = new SqlMyDieselInvoiceCorrectionStore(sql.Value);
            return store.IsChild(invoiceId);
        }

        internal static Guid RequireMyDieselInvoiceCorrectionAdmin()
        {
            Guid userId = Data.DatabaseModel.UserLoggedIn;
            using (var database = new Data.DatabaseModel(
                Properties.Settings.Default.DBConnection))
            {
                bool administrator = database.ApplicationUsers.Any(user =>
                    user.ApplicationUserId == userId && user.UserLevel == 0);
                if (!administrator)
                {
                    throw new InvalidOperationException(
                        "Η διόρθωση παραστατικών myDiesel επιτρέπεται μόνο σε συνδεδεμένο διαχειριστή.");
                }
            }
            return userId;
        }

        internal static MyDieselCorrectionInvoiceTypeChoice[]
            MyDieselCorrectionCreditTypes()
        {
            return MyDieselCorrectionInvoiceTypes(true);
        }

        internal static MyDieselCorrectionInvoiceTypeChoice[]
            MyDieselCorrectionReplacementTypes()
        {
            return MyDieselCorrectionInvoiceTypes(false);
        }

        private static MyDieselCorrectionInvoiceTypeChoice[]
            MyDieselCorrectionInvoiceTypes(bool credit)
        {
            RequireMyDieselInvoiceCorrectionAdmin();
            SharedInvoiceConfiguration configuration =
                SharedInvoicingSettings.LoadRuntime();
            using (var database = new Data.DatabaseModel(
                Properties.Settings.Default.DBConnection))
            {
                Data.InvoiceType[] types = database.InvoiceTypes.Where(type =>
                    type.Printable && type.Invalidated != true &&
                    type.ForcesDelivery != true && type.TransactionType == 0 &&
                    type.HasFinancialTransactions == true &&
                    (credit ? type.IsCancelation == true :
                        type.IsCancelation != true)).ToArray();
                Data.InvoiceType[] wholesaleTypes = types.Where(type =>
                    configuration.IsWholesaleCode(
                        type.OfficialEnumerator)).ToArray();
                return wholesaleTypes.OrderBy(type => type.Description)
                    .ThenBy(type => type.DefaultSeries)
                    .Select(type => new MyDieselCorrectionInvoiceTypeChoice
                    {
                        Id = type.InvoiceTypeId,
                        Display = (string.IsNullOrWhiteSpace(type.Description)
                            ? "(χωρίς περιγραφή)"
                            : type.Description.Trim()) + " / " +
                            (type.DefaultSeries ?? string.Empty)
                    }).ToArray();
            }
        }

        internal static MyDieselInvoiceCorrectionRow[]
            ScanMyDieselInvoiceCorrections(DateTime from, DateTime to,
                Guid creditTypeId, Guid replacementTypeId,
                Func<bool> pause = null)
        {
            RequireMyDieselInvoiceCorrectionAdmin();
            if (to.Date < from.Date)
            {
                throw new ArgumentException("Μη έγκυρο διάστημα ημερομηνιών.");
            }

            var correctionStore = new SqlMyDieselInvoiceCorrectionStore(sql.Value);
            MyDieselInvoiceCorrectionRecord[] saved = correctionStore.Load();
            var couponStore = new SqlMyDieselStore(sql.Value);
            IReadOnlyList<MyDieselCouponCaseRecord> couponCases =
                couponStore.LoadInvoiceCorrectionCandidates();
            MyDieselCouponService statusService = null;
            string statusServiceError = null;
            try
            {
                statusService = SharedMyDieselRuntime.CreateService();
            }
            catch (Exception exception)
            {
                statusServiceError = exception.Message;
            }
            DateTime end = to.Date.AddDays(1);
            var results = new List<MyDieselInvoiceCorrectionRow>();

            using (var database = new Data.DatabaseModel(
                Properties.Settings.Default.DBConnection))
            {
                foreach (MyDieselCouponCaseRecord couponCandidate in couponCases)
                {
                    if (pause != null && pause())
                    {
                        break;
                    }
                    string statusRefreshNote;
                    MyDieselCouponCaseRecord coupon =
                        RefreshMyDieselCorrectionCoupon(statusService,
                            statusServiceError, couponCandidate,
                            out statusRefreshNote);
                    MyDieselInvoiceCorrectionRecord existing = saved.SingleOrDefault(
                        savedRow => savedRow.CouponCaseId == coupon.CouponCaseId);
                    if (existing != null)
                    {
                        MyDieselInvoiceCorrectionRow savedRow =
                            MyDieselInvoiceCorrectionSavedRow(existing);
                        bool completedInRange = existing.State == "Completed" &&
                            savedRow.Date >= from.Date && savedRow.Date < end;
                        if (existing.State != "Completed" || completedInRange)
                        {
                            results.Add(savedRow);
                        }
                        continue;
                    }
                    Data.Invoice invoice = database.Invoices.SingleOrDefault(value =>
                        value.InvoiceId == coupon.InvoiceId);
                    if (invoice == null || invoice.TransactionDate < from.Date ||
                        invoice.TransactionDate >= end)
                    {
                        continue;
                    }

                    MyDieselInvoiceCorrectionRow row =
                        CreateMyDieselInvoiceCorrectionRow(invoice, coupon);
                    try
                    {
                        using (IFiscalDocumentLease lease =
                            sql.Value.EnterInvoice(invoice.InvoiceId))
                        {
                            row.Provider = MyDieselCorrectionProviderDisplay(
                                lease);
                            row.Plan = PrepareMyDieselInvoiceCorrectionPlan(lease,
                                invoice, coupon, creditTypeId,
                                replacementTypeId, correctionStore);
                        }
                        row.Amount = row.Plan.ReplacementTotal();
                        row.Eligible = true;
                        row.Selected = true;
                        row.Status =
                            "Έτοιμο — επανυπολογισμός έκπτωσης, επανέκδοση, διόρθωση συσχέτισης μέσω myDiesel και κατόπιν πίστωση του αρχικού" +
                            statusRefreshNote;
                    }
                    catch (Exception exception)
                    {
                        row.Status = "Εξαιρείται: " + exception.Message;
                    }
                    results.Add(row);
                }
            }

            foreach (MyDieselInvoiceCorrectionRecord pending in saved.Where(row =>
                row.State != "Completed" && results.All(result =>
                    result.Id != row.OriginalInvoiceId)))
            {
                MyDieselInvoiceCorrectionRow savedRow =
                    MyDieselInvoiceCorrectionSavedRow(pending);
                results.Add(savedRow);
            }
            return results.OrderBy(row => row.Date).ThenBy(row => row.Document)
                .ToArray();
        }

        private static MyDieselCouponCaseRecord
            RefreshMyDieselCorrectionCoupon(MyDieselCouponService service,
                string serviceError, MyDieselCouponCaseRecord coupon,
                out string note)
        {
            if (service == null)
            {
                note = " — δεν ήταν δυνατή η ανάκτηση κατάστασης myDiesel: " +
                    (serviceError ?? "μη διαθέσιμη σύνδεση");
                return coupon;
            }
            MyDieselCouponStatus status = MyDieselCouponStates.Parse(
                coupon.RemoteStatus);
            if (!MyDieselCouponStates.SafeForBackgroundQuery(status))
            {
                note = " — η κατάσταση δεν ανανεώθηκε αυτόματα, επειδή το query μπορεί να μεταβάλει μη ασφαλή κατάσταση (" +
                    (coupon.RemoteStatus ?? "άγνωστη") + ")";
                return coupon;
            }

            MyDieselServiceResult refresh;
            try
            {
                refresh = service.RefreshStatus(coupon.CouponCaseId);
            }
            catch (Exception exception)
            {
                note = " — αποτυχία ανάκτησης κατάστασης myDiesel: " +
                    exception.Message;
                return coupon;
            }
            MyDieselCouponCaseRecord refreshedCoupon = refresh.CouponCase ??
                coupon;
            if (refresh.OperationState == MyDieselOperationState.Succeeded)
            {
                note = " — κατάσταση myDiesel ανανεώθηκε";
            }
            else
            {
                note = " — η ανανέωση κατάστασης myDiesel δεν ολοκληρώθηκε (" +
                    refresh.OperationState + ")";
            }
            return refreshedCoupon;
        }

        private static MyDieselInvoiceCorrectionRow
            CreateMyDieselInvoiceCorrectionRow(Data.Invoice invoice,
                MyDieselCouponCaseRecord coupon)
        {
            return new MyDieselInvoiceCorrectionRow
            {
                Id = invoice.InvoiceId,
                Voucher = coupon.CouponCode,
                Document = invoice.Description,
                Date = invoice.TransactionDate,
                Customer = invoice.Trader == null ? string.Empty :
                    invoice.Trader.Name,
                VatNumber = invoice.Trader == null ? string.Empty :
                    invoice.Trader.TaxRegistrationNumber,
                Mark = coupon.InvoiceMark,
                CouponStatus = coupon.RemoteStatus ?? string.Empty,
                OriginalAmount = invoice.TotalAmount ?? 0,
                Amount = invoice.TotalAmount ?? 0
            };
        }

        private static MyDieselInvoiceCorrectionPlan
            PrepareMyDieselInvoiceCorrectionPlan(IFiscalDocumentLease lease,
                Data.Invoice invoice, MyDieselCouponCaseRecord coupon,
                Guid creditTypeId, Guid replacementTypeId,
                SqlMyDieselInvoiceCorrectionStore correctionStore)
        {
            if (coupon.InvoiceId != invoice.InvoiceId ||
                coupon.MarkSourceInvoiceId != invoice.InvoiceId)
            {
                throw new InvalidOperationException(
                    "Το κουπόνι δεν ανήκει πλέον στο αρχικό παραστατικό.");
            }
            if (!MyDieselCouponService.CanReleaseForReassignment(coupon))
            {
                throw new InvalidOperationException(
                    "Η κατάσταση του κουπονιού (" +
                    (coupon.RemoteStatus ?? "άγνωστη") + ", τοπικά " +
                    coupon.LocalStage +
                    ") δεν επιτρέπει διόρθωση συσχέτισης μέσω του API myDiesel.");
            }
            bool hasFiscalChild = invoice.ChildInvoiceRelations.Any(relation => relation.IsExternalFiscalCorrelation);
            bool hasFiscalParent = invoice.ParentInvoiceRelations.Any(relation => relation.IsExternalFiscalCorrelation);
            if (hasFiscalChild || hasFiscalParent)
            {
                throw new InvalidOperationException(
                    "Υπάρχουν ήδη συσχετισμένα παραστατικά ή πιστώσεις.");
            }
            if (!invoice.TraderId.HasValue || invoice.IsEuromat == true)
            {
                throw new InvalidOperationException(
                    "Απαιτείται συναλλασσόμενος και απουσία πληρωμής στόλου.");
            }
            SharedInvoiceConfiguration configuration =
                SharedInvoicingSettings.LoadRuntime();
            bool wholesaleInvoice = invoice.InvoiceType != null &&
                configuration.IsWholesaleCode(
                    invoice.InvoiceType.OfficialEnumerator);
            if (invoice.InvoiceType == null ||
                !wholesaleInvoice ||
                invoice.InvoiceType.TransactionType != 0 ||
                invoice.InvoiceType.HasFinancialTransactions != true)
            {
                throw new InvalidOperationException(
                    "Η εργασία αφορά μόνο τιμολόγια χονδρικής με οικονομική κίνηση.");
            }
            var workflowStore = new SqlInvoiceWorkflowStore(sql.Value);
            byte[] workflowBytes = workflowStore.Load(lease);
            if (workflowBytes == null)
            {
                throw new InvalidOperationException(
                    "Δεν υπάρχει αποθηκευμένη ροή έκδοσης του αρχικού παραστατικού.");
            }
            string workflowJson = Encoding.UTF8.GetString(workflowBytes);
            InvoiceWorkflowSnapshot snapshot =
                JsonConvert.DeserializeObject<InvoiceWorkflowSnapshot>(workflowJson);
            if (snapshot == null ||
                snapshot.MyDieselCouponCaseId != coupon.CouponCaseId ||
                snapshot.MyDieselScope == null)
            {
                throw new InvalidOperationException(
                    "Η αποθηκευμένη ροή δεν είναι συνδεδεμένη με αυτό το κουπόνι myDiesel.");
            }

            var history = new SqlFiscalHistoryStore(sql.Value);
            FiscalAttemptEvidence submission = history.Load(lease,
                FiscalJournalFamily.Submission).LastOrDefault();
            if (submission == null || submission.State != "Accepted")
            {
                throw new InvalidOperationException(
                    "Η αρχική διαβίβαση δεν έχει επιβεβαιωθεί.");
            }
            InvoiceResponse response =
                ProviderSubmissionService.ReadAcceptedResponse(submission);
            long originalMark;
            bool validMark = long.TryParse(response.Mark,
                System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture,
                out originalMark);
            if (!validMark || originalMark <= 0 ||
                coupon.InvoiceMark != originalMark.ToString(
                    System.Globalization.CultureInfo.InvariantCulture))
            {
                throw new InvalidOperationException(
                    "Το MARK του κουπονιού δεν συμφωνεί με την αποδεκτή αρχική διαβίβαση.");
            }

            Data.InvoiceType creditType;
            Data.InvoiceType replacementType;
            using (var database = new Data.DatabaseModel(
                Properties.Settings.Default.DBConnection))
            {
                creditType = database.InvoiceTypes.SingleOrDefault(type =>
                    type.InvoiceTypeId == creditTypeId);
                replacementType = database.InvoiceTypes.SingleOrDefault(type =>
                    type.InvoiceTypeId == replacementTypeId);
                ValidateMyDieselCorrectionType(creditType, true,
                    configuration);
                ValidateMyDieselCorrectionType(replacementType, false,
                    configuration);
            }
            SharedInvoiceRoute creditRoute = configuration.Route(creditType);
            SharedInvoiceRoute replacementRoute = configuration.Route(replacementType);
            ValidateMyDieselCorrectionCreditRoute(snapshot, creditRoute);
            ValidateMyDieselCorrectionReplacementRoute(replacementRoute);
            var credentials = new CurrentCredentials();
            credentials.Resolve(snapshot.Provider);
            credentials.Resolve(replacementRoute.Provider);

            if (!coupon.IssuedLitres.HasValue ||
                coupon.IssuedLitres.Value <= 0)
            {
                throw new InvalidOperationException(
                    "Το κουπόνι δεν περιέχει έγκυρη ποσότητα λίτρων για τον επανυπολογισμό της έκπτωσης.");
            }
            JObject originalHost = correctionStore.ReadHost(
                invoice.InvoiceId);
            JObject replacementHost = MyDieselInvoiceCorrectionPlan
                .RecalculateReplacementHost(originalHost,
                    coupon.IssuedLitres.Value,
                    coupon.ExciseDiscountPerLitre);

            return new MyDieselInvoiceCorrectionPlan
            {
                OriginalId = invoice.InvoiceId,
                CreditId = Guid.NewGuid(),
                ReplacementId = Guid.NewGuid(),
                CouponCaseId = coupon.CouponCaseId,
                CreditTypeId = creditTypeId,
                ReplacementTypeId = replacementTypeId,
                OriginalAttemptId = submission.AttemptId,
                CouponCode = coupon.CouponCode,
                OriginalDescription = invoice.Description,
                CreditSeries = creditType.DefaultSeries ?? string.Empty,
                ReplacementSeries = replacementType.DefaultSeries ?? string.Empty,
                OriginalMark = originalMark,
                CouponIssuedLitres = coupon.IssuedLitres.Value,
                ExciseDiscountPerLitre = coupon.ExciseDiscountPerLitre,
                Original = MyDieselInvoiceCorrectionPlan.Copy(snapshot),
                Host = originalHost,
                ReplacementHost = replacementHost
            };
        }

        private static string MyDieselCorrectionProviderDisplay(
            IFiscalDocumentLease lease)
        {
            var workflowStore = new SqlInvoiceWorkflowStore(sql.Value);
            byte[] workflowBytes = workflowStore.Load(lease);
            if (workflowBytes == null)
            {
                return string.Empty;
            }
            string workflowJson = Encoding.UTF8.GetString(workflowBytes);
            InvoiceWorkflowSnapshot snapshot =
                JsonConvert.DeserializeObject<InvoiceWorkflowSnapshot>(
                    workflowJson);
            if (snapshot == null || snapshot.Provider == null)
            {
                return string.Empty;
            }
            InvoiceProviderDefinition providerDefinition =
                InvoiceProviderCatalog.Get(snapshot.Provider.ProviderKey);
            return providerDefinition.DisplayName;
        }

        private static void ValidateMyDieselCorrectionType(
            Data.InvoiceType invoiceType, bool credit,
            SharedInvoiceConfiguration configuration)
        {
            bool wholesaleType = invoiceType != null &&
                configuration.IsWholesaleCode(
                    invoiceType.OfficialEnumerator);
            bool valid = invoiceType != null && invoiceType.Printable &&
                invoiceType.Invalidated != true &&
                invoiceType.ForcesDelivery != true &&
                invoiceType.TransactionType == 0 &&
                invoiceType.HasFinancialTransactions == true &&
                wholesaleType &&
                (credit ? invoiceType.IsCancelation == true :
                    invoiceType.IsCancelation != true);
            if (!valid)
            {
                string kind = credit ? "πιστωτικού τιμολογίου" :
                    "τιμολογίου επανέκδοσης";
                throw new InvalidOperationException("Επιλέξτε έγκυρο τύπο " +
                    kind + " χωρίς υποχρεωτική διακίνηση.");
            }
        }

        private static void ValidateMyDieselCorrectionCreditRoute(
            InvoiceWorkflowSnapshot original, SharedInvoiceRoute credit)
        {
            ProviderRoute originalProvider = original == null ? null :
                original.Provider;
            FiscalSigningScope originalSigning = original == null ||
                original.Signing == null ? null : original.Signing.Scope;
            bool sameProvider = SameMyDieselCorrectionProvider(
                originalProvider, credit == null ? null : credit.Provider);
            bool sameSigning = SameMyDieselCorrectionSigning(
                originalSigning, credit == null ? null : credit.Signing);
            bool sameLocalOnly = original != null && credit != null &&
                original.LocalOnly == credit.LocalOnly;
            if (originalProvider == null || !sameProvider || !sameSigning ||
                !sameLocalOnly)
            {
                throw new InvalidOperationException(
                    "Ο τύπος πιστωτικού πρέπει να έχει την ίδια διαδρομή " +
                    "διαβίβασης και σήμανσης με το αρχικό παραστατικό.");
            }
        }

        private static void ValidateMyDieselCorrectionReplacementRoute(
            SharedInvoiceRoute replacement)
        {
            if (replacement == null || replacement.LocalOnly ||
                replacement.Provider == null || replacement.Signing != null ||
                replacement.Provider.ProviderKey ==
                    InvoiceProviderCatalog.MyDataKey)
            {
                throw new InvalidOperationException(
                    "Το νέο παραστατικό myDiesel πρέπει να εκδίδεται μέσω παρόχου και όχι απευθείας στο myDATA.");
            }
        }

        private static bool SameMyDieselCorrectionProvider(
            ProviderRoute left, ProviderRoute right)
        {
            return MyDieselInvoiceCorrectionPlan.SameCreditProviderRoute(
                left, right);
        }

        private static bool SameMyDieselCorrectionSigning(
            FiscalSigningScope left, FiscalSigningScope right)
        {
            return MyDieselInvoiceCorrectionPlan.SameCreditSigningRoute(
                left, right);
        }

        private static MyDieselInvoiceCorrectionRow
            MyDieselInvoiceCorrectionSavedRow(
                MyDieselInvoiceCorrectionRecord record)
        {
            MyDieselInvoiceCorrectionPlan plan =
                JsonConvert.DeserializeObject<MyDieselInvoiceCorrectionPlan>(
                    record.PlanJson);
            var couponStore = new SqlMyDieselStore(sql.Value);
            MyDieselCouponCaseRecord coupon;
            using (IFiscalDocumentLease couponLease = couponStore.Acquire(
                record.CouponCaseId))
            {
                coupon = couponStore.Load(couponLease);
            }
            using (var database = new Data.DatabaseModel(
                Properties.Settings.Default.DBConnection))
            {
                Data.Invoice original = database.Invoices.SingleOrDefault(invoice =>
                    invoice.InvoiceId == record.OriginalInvoiceId);
                Data.Invoice credit = database.Invoices.SingleOrDefault(invoice =>
                    invoice.InvoiceId == record.CreditInvoiceId);
                Data.Invoice replacement = database.Invoices.SingleOrDefault(invoice =>
                    invoice.InvoiceId == record.ReplacementInvoiceId);
                return new MyDieselInvoiceCorrectionRow
                {
                    Id = record.OriginalInvoiceId,
                    Voucher = plan.CouponCode ?? string.Empty,
                    Document = plan.OriginalDescription,
                    Date = original == null ? plan.Original.Invoice.Invoice.PrintedDateTime :
                        original.TransactionDate,
                    Customer = original == null || original.Trader == null ?
                        string.Empty : original.Trader.Name,
                    VatNumber = original == null || original.Trader == null ?
                        string.Empty : original.Trader.TaxRegistrationNumber,
                    Provider = plan.Original.Provider == null ? string.Empty :
                        InvoiceProviderCatalog.Get(
                            plan.Original.Provider.ProviderKey).DisplayName,
                    Mark = plan.OriginalMark.ToString(
                        System.Globalization.CultureInfo.InvariantCulture),
                    CouponStatus = coupon == null ? string.Empty :
                        coupon.RemoteStatus ?? string.Empty,
                    OriginalAmount = original == null ? 0 :
                        original.TotalAmount ?? 0,
                    Amount = replacement == null
                        ? plan.ReplacementHost == null
                            ? original == null ? 0 :
                                original.TotalAmount ?? 0
                            : plan.ReplacementTotal()
                        : replacement.TotalAmount ?? 0,
                    Credit = credit == null ? "Εκκρεμεί" : credit.Description,
                    Replacement = replacement == null ? "Εκκρεμεί" :
                        replacement.Description,
                    Status = record.Message,
                    Eligible = false,
                    Plan = plan
                };
            }
        }

        internal static MyDieselInvoiceCorrectionRow[]
            RunMyDieselInvoiceCorrections(
                MyDieselInvoiceCorrectionRow[] selected, bool resume,
                Func<bool> pause, Action<string> report)
        {
            Guid userId = RequireMyDieselInvoiceCorrectionAdmin();
            var store = new SqlMyDieselInvoiceCorrectionStore(sql.Value);
            using (store.EnterBatch())
            {
                MyDieselInvoiceCorrectionRecord[] existing = store.Load();
                Guid batchId = Guid.NewGuid();
                if (!resume)
                {
                    foreach (MyDieselInvoiceCorrectionRow row in selected.Where(
                        value => value.Selected && value.Eligible))
                    {
                        if (pause())
                        {
                            break;
                        }
                        RequireMyDieselInvoiceCorrectionAdmin();
                        if (existing.Any(value =>
                            value.OriginalInvoiceId == row.Id))
                        {
                            throw new InvalidOperationException(
                                "Το παραστατικό ανήκει ήδη σε παρτίδα. Χρησιμοποιήστε συνέχεια εκκρεμοτήτων.");
                        }
                        using (IFiscalDocumentLease lease =
                            sql.Value.EnterInvoice(row.Id))
                        using (var database = new Data.DatabaseModel(
                            Properties.Settings.Default.DBConnection))
                        {
                            Data.Invoice invoice = database.Invoices.Single(value =>
                                value.InvoiceId == row.Id);
                            var couponStore = new SqlMyDieselStore(sql.Value);
                            MyDieselCouponCaseRecord coupon;
                            using (IFiscalDocumentLease couponLease =
                                couponStore.Acquire(row.Plan.CouponCaseId))
                            {
                                coupon = couponStore.Load(couponLease);
                            }
                            MyDieselInvoiceCorrectionPlan latest =
                                PrepareMyDieselInvoiceCorrectionPlan(lease,
                                    invoice, coupon, row.Plan.CreditTypeId,
                                    row.Plan.ReplacementTypeId, store);
                            bool sameHost = SqlHeatingCorrectionStore.SameHost(
                                row.Plan.Host, latest.Host);
                            bool sameReplacement =
                                SqlHeatingCorrectionStore.SameHost(
                                    row.Plan.ReplacementHost,
                                    latest.ReplacementHost);
                            bool sameAttempt = row.Plan.OriginalAttemptId ==
                                latest.OriginalAttemptId;
                            if (!sameHost || !sameReplacement || !sameAttempt)
                            {
                                throw new InvalidOperationException(
                                    "Τα στοιχεία άλλαξαν μετά τον έλεγχο. Εκτελέστε νέο έλεγχο παραστατικών.");
                            }
                        }

                        string creditNote =
                            "Πλήρης πίστωση παραστατικού της εργασίας διόρθωσης " +
                            row.Plan.OriginalDescription +
                            " κατόπιν ελέγχου ΑΑΔΕ.";
                        string replacementNote = "Επανέκδοση του " +
                            row.Plan.OriginalDescription +
                            " — διόρθωση στοιχείων myDiesel κατόπιν ελέγχου ΑΑΔΕ.";
                        var record = new MyDieselInvoiceCorrectionRecord
                        {
                            OriginalInvoiceId = row.Id,
                            BatchId = batchId,
                            CouponCaseId = row.Plan.CouponCaseId,
                            CreditInvoiceId = row.Plan.CreditId,
                            ReplacementInvoiceId = row.Plan.ReplacementId,
                            PlanJson = JsonConvert.SerializeObject(row.Plan),
                            CreatedBy = userId
                        };
                        using (IFiscalDocumentLease lease =
                            sql.Value.EnterInvoice(row.Id))
                        {
                            store.ReserveAndCreateChildren(lease, record,
                                row.Plan.Host, row.Plan.ReplacementHost,
                                row.Plan.CreditTypeId,
                                row.Plan.CreditSeries, creditNote,
                                row.Plan.ReplacementTypeId,
                                row.Plan.ReplacementSeries, replacementNote,
                                DateTime.Now);
                        }
                    }
                }

                MyDieselInvoiceCorrectionRecord[] loadedRecords = store.Load();
                MyDieselInvoiceCorrectionRecord[] work = loadedRecords.Where(row =>
                    row.State != "Completed" &&
                    (resume || row.BatchId == batchId)).ToArray();
                foreach (MyDieselInvoiceCorrectionRecord record in work)
                {
                    if (pause())
                    {
                        break;
                    }
                    RequireMyDieselInvoiceCorrectionAdmin();
                    MyDieselInvoiceCorrectionPlan plan =
                        JsonConvert.DeserializeObject<MyDieselInvoiceCorrectionPlan>(
                            record.PlanJson);
                    report(plan.OriginalDescription + ": επεξεργασία...");
                    using (IFiscalDocumentLease lease =
                        sql.Value.EnterInvoice(record.OriginalInvoiceId))
                    {
                        try
                        {
                            Func<InvoiceWorkflowResult> issueReplacement =
                                delegate
                                {
                                    return IssueMyDieselCorrectionChild(plan,
                                        false, false, record.CreatedBy);
                                };
                            Func<InvoiceWorkflowResult, MyDieselServiceResult>
                                correctCoupon = delegate(
                                    InvoiceWorkflowResult replacementResult)
                            {
                                return CorrectMyDieselInvoiceAssociation(plan,
                                    replacementResult);
                            };
                            Func<bool, InvoiceWorkflowResult> issueCredit =
                                delegate(bool creditReplacement)
                                {
                                    return IssueMyDieselCorrectionChild(plan,
                                        true, creditReplacement,
                                        record.CreatedBy);
                                };
                            Action<bool> verifyCouponAssociation =
                                delegate(bool couponOnReplacement)
                                {
                                    VerifyMyDieselInvoiceAssociation(plan,
                                        couponOnReplacement);
                                };
                            Action<string, string> save = delegate(
                                string state, string message)
                            {
                                store.Progress(lease, state, message);
                                report(plan.OriginalDescription + ": " + message);
                            };
                            MyDieselInvoiceCorrectionRunner.Run(
                                issueReplacement, correctCoupon, issueCredit,
                                verifyCouponAssociation, save, pause);
                        }
                        catch (Exception exception)
                        {
                            store.Progress(lease, "NeedsReview",
                                exception.Message);
                            report(plan.OriginalDescription + ": " +
                                exception.Message);
                        }
                    }
                }
                MyDieselInvoiceCorrectionRecord[] completedRecords =
                    store.Load();
                return completedRecords.Select(
                    MyDieselInvoiceCorrectionSavedRow).ToArray();
            }
        }

        private static MyDieselServiceResult
            CorrectMyDieselInvoiceAssociation(
                MyDieselInvoiceCorrectionPlan plan,
                InvoiceWorkflowResult replacementResult)
        {
            RequireMyDieselInvoiceCorrectionAdmin();
            string authorizedBy = string.IsNullOrWhiteSpace(
                    Program.CurrentUserName)
                ? Environment.UserName
                : Program.CurrentUserName;
            string reason = "Πλήρης πίστωση και επανέκδοση του " +
                plan.OriginalDescription + " κατόπιν ελέγχου ΑΑΔΕ.";
            MyDieselCouponService service =
                SharedMyDieselRuntime.CreateService();
            string replacementMark = AcceptedMyDieselCorrectionMark(
                plan.ReplacementId, replacementResult);
            string originalMark = plan.OriginalMark.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
            MyDieselServiceResult result =
                service.CorrectInvoiceAssociationOrRecover(
                    plan.CouponCaseId, plan.OriginalId,
                    plan.ReplacementId, originalMark, replacementMark,
                    authorizedBy, reason);
            return result;
        }

        private static InvoiceWorkflowResult IssueMyDieselCorrectionChild(
            MyDieselInvoiceCorrectionPlan plan, bool credit,
            bool creditReplacement, Guid userId)
        {
            RequireMyDieselInvoiceCorrectionAdmin();
            Guid invoiceId = credit ? plan.CreditId : plan.ReplacementId;
            Guid typeId = credit ? plan.CreditTypeId :
                plan.ReplacementTypeId;
            string series = credit ? plan.CreditSeries :
                plan.ReplacementSeries;
            string note = (credit ? "Πλήρης πίστωση" : "Επανέκδοση") +
                " του " + plan.OriginalDescription +
                " — διόρθωση στοιχείων myDiesel κατόπιν ελέγχου ΑΑΔΕ.";
            JObject childHost = credit ? plan.Host : plan.ReplacementHost;
            if (childHost == null)
            {
                throw new InvalidOperationException(
                    "Δεν υπάρχει αποθηκευμένο σχέδιο αξιών για το διορθωτικό παραστατικό.");
            }
            var hostStore = new SqlHeatingCorrectionStore(sql.Value);
            using (IFiscalDocumentLease lease = sql.Value.EnterInvoice(invoiceId))
            {
                hostStore.CreateChild(lease, childHost, typeId, series, userId,
                    note, DateTime.Now, 0);
            }
            InvoiceParent creditParent = credit ?
                MyDieselCorrectionCreditParent(plan, creditReplacement) :
                null;
            var source = new MyDieselInvoiceCorrectionSource(plan, credit,
                creditParent);
            InvoiceWorkflowResult result = workflow.Value.Process(invoiceId,
                source, false, retryRejected: true);
            return result;
        }

        private static string AcceptedMyDieselCorrectionMark(Guid invoiceId,
            InvoiceWorkflowResult result = null)
        {
            string resultMark = result == null || result.Response == null ?
                null : result.Response.Mark;
            if (!string.IsNullOrWhiteSpace(resultMark))
            {
                return MyDieselRedeemRequest.NormalizeMark(resultMark);
            }
            using (IFiscalDocumentLease lease = sql.Value.EnterInvoice(
                invoiceId))
            {
                var history = new SqlFiscalHistoryStore(sql.Value);
                FiscalAttemptEvidence acceptedAttempt = history.Load(lease,
                    FiscalJournalFamily.Submission).LastOrDefault(attempt =>
                        attempt.State == "Accepted");
                if (acceptedAttempt == null)
                {
                    throw new InvalidOperationException(
                        "Δεν βρέθηκε αποδεκτή διαβίβαση της επανέκδοσης.");
                }
                InvoiceResponse response =
                    ProviderSubmissionService.ReadAcceptedResponse(
                        acceptedAttempt);
                return MyDieselRedeemRequest.NormalizeMark(response.Mark);
            }
        }

        private static InvoiceParent MyDieselCorrectionCreditParent(
            MyDieselInvoiceCorrectionPlan plan, bool creditReplacement)
        {
            if (!creditReplacement)
            {
                return new InvoiceParent
                {
                    Invoice = MyDieselInvoiceCorrectionPlan.Copy(
                        plan.Original.Invoice.Invoice),
                    Transmission = new TransmissionEvidence
                    {
                        Status = 3,
                        Mark = plan.OriginalMark
                    }
                };
            }

            string replacementMark = AcceptedMyDieselCorrectionMark(
                plan.ReplacementId);
            using (IFiscalDocumentLease lease = sql.Value.EnterInvoice(
                plan.ReplacementId))
            {
                var workflowStore = new SqlInvoiceWorkflowStore(sql.Value);
                byte[] workflowBytes = workflowStore.Load(lease);
                if (workflowBytes == null)
                {
                    throw new InvalidOperationException(
                        "Δεν υπάρχει αποθηκευμένη ροή της επανέκδοσης.");
                }
                string workflowJson = Encoding.UTF8.GetString(workflowBytes);
                InvoiceWorkflowSnapshot replacementSnapshot =
                    JsonConvert.DeserializeObject<InvoiceWorkflowSnapshot>(
                        workflowJson);
                if (replacementSnapshot == null ||
                    replacementSnapshot.Invoice == null ||
                    replacementSnapshot.Invoice.Invoice == null)
                {
                    throw new InvalidOperationException(
                        "Η αποθηκευμένη επανέκδοση δεν είναι έγκυρη.");
                }
                long numericMark;
                bool validMark = long.TryParse(replacementMark,
                    System.Globalization.NumberStyles.None,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out numericMark);
                if (!validMark || numericMark <= 0)
                {
                    throw new InvalidOperationException(
                        "Το MARK της επανέκδοσης δεν είναι έγκυρο.");
                }
                return new InvoiceParent
                {
                    Invoice = MyDieselInvoiceCorrectionPlan.Copy(
                        replacementSnapshot.Invoice.Invoice),
                    Transmission = new TransmissionEvidence
                    {
                        Status = 3,
                        Mark = numericMark
                    }
                };
            }
        }

        private static void VerifyMyDieselInvoiceAssociation(
            MyDieselInvoiceCorrectionPlan plan,
            bool couponOnReplacement)
        {
            MyDieselCouponService service =
                SharedMyDieselRuntime.CreateService();
            Guid expectedInvoiceId = couponOnReplacement ?
                plan.ReplacementId : plan.OriginalId;
            Guid excludedInvoiceId = couponOnReplacement ?
                plan.OriginalId : plan.ReplacementId;
            string expectedMark = couponOnReplacement ?
                AcceptedMyDieselCorrectionMark(plan.ReplacementId) :
                plan.OriginalMark.ToString(
                    System.Globalization.CultureInfo.InvariantCulture);
            service.VerifyCouponAssociation(plan.CouponCaseId,
                expectedInvoiceId, expectedMark, excludedInvoiceId);
        }

        private sealed class MyDieselInvoiceCorrectionSource :
            IInvoiceWorkflowSource
        {
            private readonly MyDieselInvoiceCorrectionPlan plan;
            private readonly bool credit;
            private readonly InvoiceParent creditParent;

            internal MyDieselInvoiceCorrectionSource(
                MyDieselInvoiceCorrectionPlan value, bool isCredit,
                InvoiceParent parent)
            {
                plan = value;
                credit = isCredit;
                creditParent = parent;
            }

            public InvoiceWorkflowSnapshot Prepare(IFiscalDocumentLease lease)
            {
                using (var database = new Data.DatabaseModel(
                    Properties.Settings.Default.DBConnection))
                {
                    Data.Invoice invoice = database.Invoices.Single(value =>
                        value.InvoiceId == lease.Document.DocumentId);
                    SharedInvoiceConfiguration configuration =
                        SharedInvoicingSettings.LoadRuntime();
                    SharedInvoiceRoute configuredRoute =
                        configuration.Route(invoice);
                    if (credit)
                    {
                        ValidateMyDieselCorrectionCreditRoute(plan.Original,
                            configuredRoute);
                    }
                    else
                    {
                        ValidateMyDieselCorrectionReplacementRoute(
                            configuredRoute);
                    }
                    SharedPrinterProfile printer = configuredRoute.Printer ??
                        configuration.DefaultPrinter ??
                        new SharedPrinterProfile();
                    string content = PrintAgent.RenderSharedInvoiceContent(
                        database, invoice);
                    MappingContext mapping =
                        MyDieselInvoiceCorrectionPlan.Copy(
                            configuration.Mapping);
                    if (mapping == null)
                    {
                        throw new InvalidOperationException(
                            "Απαιτούνται αποθηκευμένες ρυθμίσεις χαρτογράφησης.");
                    }
                    mapping.Company = ReadHostCompany();
                    ProviderRoute provider = credit ? plan.Original.Provider :
                        configuredRoute.Provider;
                    FiscalSigningScope signingScope = credit &&
                        plan.Original.Signing != null ?
                        plan.Original.Signing.Scope : null;
                    bool localOnly = credit && plan.Original.LocalOnly;
                    mapping.Environment = provider.TestMode
                        ? FiscalEnvironment.Test
                        : FiscalEnvironment.Production;
                    var mapper = new HostInvoiceMapper();
                    InvoiceBuildRequest request = mapper.Map(invoice, mapping);
                    InvoicePaymentType correctivePayment =
                        plan.CorrectivePaymentType();
                    request.Invoice.PaymentType = correctivePayment;
                    request.SaleCorrelations = null;
                    request.RequireIssuedIdentity = true;
                    if (credit)
                    {
                        if (creditParent == null)
                        {
                            throw new InvalidOperationException(
                                "Δεν ορίστηκε το παραστατικό που θα πιστωθεί.");
                        }
                        request.Parents = new[] { creditParent };
                    }
                    else
                    {
                        request.Parents = new InvoiceParent[0];
                    }

                    FiscalSigningRequest signing = null;
                    if (signingScope != null)
                    {
                        SamtecDeviceProfile device =
                            configuration.SamtecDevice(signingScope);
                        string eftPosTID = device == null ? null :
                            device.EftPosTID;
                        signing = HostFiscalSigningMapper.Invoice(invoice,
                            signingScope, content, false, eftPosTID);
                    }

                    var snapshot = new InvoiceWorkflowSnapshot
                    {
                        InvoiceId = invoice.InvoiceId,
                        Provider = provider,
                        Signing = signing,
                        LocalOnly = localOnly,
                        Invoice = request,
                        PrintFormat = SharedInvoicePrinting.Format,
                        PrintDestination = JsonConvert.SerializeObject(printer),
                        PrintContent = Encoding.UTF8.GetBytes(content),
                        CompanyQrContent = Reports.BrandData.SetBrandData(invoice)
                    };
                    return snapshot;
                }
            }
        }

        internal static void PrintMyDieselInvoiceCorrectionsCompleted(
            Func<bool> pause)
        {
            RequireMyDieselInvoiceCorrectionAdmin();
            var store = new SqlMyDieselInvoiceCorrectionStore(sql.Value);
            MyDieselInvoiceCorrectionRecord[] loadedRecords = store.Load();
            foreach (MyDieselInvoiceCorrectionRecord row in loadedRecords.Where(
                value => value.State == "Completed"))
            {
                Guid[] invoiceIds =
                    { row.CreditInvoiceId, row.ReplacementInvoiceId };
                foreach (Guid invoiceId in invoiceIds)
                {
                    if (pause())
                    {
                        return;
                    }
                    RequireMyDieselInvoiceCorrectionAdmin();
                    InvoiceWorkflowResult result = Process(invoiceId, true);
                    bool printed = result.Print != null &&
                        (result.Print.State == "HandedOff" ||
                         result.Print.State == "Confirmed");
                    if (!printed)
                    {
                        string state = result.Print == null ? "—" :
                            result.Print.State;
                        throw new InvalidOperationException(
                            "Δεν επιβεβαιώθηκε η εκτύπωση του " + invoiceId +
                            " (" + state + ").");
                    }
                }
            }
        }
    }
}
