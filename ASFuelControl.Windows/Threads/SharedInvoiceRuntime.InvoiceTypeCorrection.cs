using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ASFuelControl.Invoicing;
using ASFuelControl.Invoicing.Mapping;
using ASFuelControl.Invoicing.Persistence;
using Exedron.Invoicing.Core;
using Exedron.Invoicing.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ASFuelControl.Windows.Threads
{
    internal sealed class InvoiceTypeCorrectionRow
    {
        public bool Selected { get; set; }
        public string Document { get; set; }
        public string Customer { get; set; }
        public string VatNumber { get; set; }
        public DateTime Date { get; set; }
        public string Provider { get; set; }
        public long Mark { get; set; }
        public decimal Amount { get; set; }
        public string Method { get; set; }
        public string Credit { get; set; }
        public string Replacement { get; set; }
        public string Status { get; set; }
        public bool Eligible { get; set; }
        internal Guid Id;
        internal InvoiceTypeCorrectionPlan Plan;
    }

    internal sealed class InvoiceTypeCorrectionCreditType
    {
        public Guid Id { get; set; }
        public string Display { get; set; }
    }

    internal static partial class SharedInvoiceRuntime
    {
        internal static bool IsInvoiceTypeCorrectionChild(Guid id) => new SqlInvoiceTypeCorrectionStore(sql.Value).IsChild(id);

        internal static Guid RequireInvoiceTypeCorrectionAdmin()
        {
            var id = Data.DatabaseModel.UserLoggedIn;
            using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
                if (!db.ApplicationUsers.Any(user => user.ApplicationUserId == id && user.UserLevel == 0))
                    throw new InvalidOperationException("Η διόρθωση τύπου myDATA επιτρέπεται μόνο σε συνδεδεμένο διαχειριστή.");
            return id;
        }

        internal static InvoiceTypeCorrectionCreditType[] InvoiceTypeCorrectionCreditTypes()
        {
            RequireInvoiceTypeCorrectionAdmin();
            using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
                return db.InvoiceTypes.Where(type => type.Printable && type.Invalidated != true &&
                    type.OfficialEnumerator == 175 &&
                    type.ForcesDelivery != true && type.TransactionType == 0 && type.HasFinancialTransactions == true).ToArray()
                    .OrderBy(type => type.Description).ThenBy(type => type.Abbreviation)
                    .Select(type => new InvoiceTypeCorrectionCreditType {
                        Id = type.InvoiceTypeId,
                        Display = (string.IsNullOrWhiteSpace(type.Abbreviation) ? "(χωρίς συντόμευση)" : type.Abbreviation.Trim()) +
                            "  —  " + (string.IsNullOrWhiteSpace(type.Description) ? "(χωρίς περιγραφή)" : type.Description.Trim()) +
                            "  |  Σειρά: " + (string.IsNullOrWhiteSpace(type.DefaultSeries) ? "(κενή)" : type.DefaultSeries.Trim()) +
                            "  |  Κωδικός παραστατικού: " + type.OfficialEnumerator
                    }).ToArray();
        }

        internal static InvoiceTypeCorrectionRow[] ScanInvoiceTypeCorrections(DateTime from, DateTime to,
            Guid retailCreditType, Func<bool> pause = null)
        {
            RequireInvoiceTypeCorrectionAdmin();
            if (to.Date < from.Date) throw new ArgumentException("Μη έγκυρο διάστημα ημερομηνιών.");
            var store = new SqlInvoiceTypeCorrectionStore(sql.Value);
            var saved = store.Load();
            var results = new List<InvoiceTypeCorrectionRow>();
            Guid[] ids;
            DateTime end = to.Date.AddDays(1);
            using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
                ids = db.Invoices.Where(invoice => invoice.TransactionDate >= from.Date && invoice.TransactionDate < end)
                    .Select(invoice => invoice.InvoiceId).ToArray();

            foreach (var id in ids.Concat(saved.Where(row => row.State != "Completed")
                .Select(row => row.OriginalInvoiceId)).Distinct())
            {
                if (pause?.Invoke() == true) break;
                var existing = saved.SingleOrDefault(row => row.OriginalInvoiceId == id);
                if (existing != null) { results.Add(InvoiceTypeSavedRow(existing)); continue; }
                using (var lease = sql.Value.EnterInvoice(id))
                {
                    var bytes = new SqlInvoiceWorkflowStore(sql.Value).Load(lease);
                    if (bytes == null) continue;
                    var snapshot = JsonConvert.DeserializeObject<InvoiceWorkflowSnapshot>(Encoding.UTF8.GetString(bytes));
                    if (snapshot?.Provider == null || snapshot.Invoice == null) continue;
                    try
                    {
                        if (new InvoiceBuilder(new InvoiceTypeResolver()).Build(snapshot.Invoice).InvoiceHeader.InvoiceType != "11.1") continue;
                    }
                    catch { continue; }

                    using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
                    {
                        var invoice = db.Invoices.Single(item => item.InvoiceId == id);
                        var row = new InvoiceTypeCorrectionRow {
                            Id = id, Document = invoice.Description, Customer = invoice.Trader?.Name ?? "",
                            VatNumber = invoice.Trader?.TaxRegistrationNumber ?? "", Date = invoice.TransactionDate,
                            Provider = TypeCorrectionProvider(snapshot.Provider), Amount = invoice.TotalAmount ?? 0
                        };
                        try
                        {
                            row.Plan = PrepareInvoiceTypeCorrectionPlan(lease, invoice, snapshot, retailCreditType, store,
                                mark => row.Mark = mark);
                            row.Provider = TypeCorrectionProvider(row.Plan.Original.Provider);
                            row.Mark = row.Plan.OriginalMark;
                            row.Method = TypeCorrectionMethod(row.Plan);
                            row.Eligible = true; row.Selected = true;
                            row.Status = row.Plan.Original.MyDieselCouponCaseId.HasValue
                                ? "Έτοιμο — το 11.1 θα διορθωθεί σε 1.1 και θα ενημερωθεί το MARK στο myDiesel"
                                : "Έτοιμο — το 11.1 θα διορθωθεί σε 1.1";
                        }
                        catch (Exception ex) { row.Status = "Εξαιρείται: " + ex.Message; }
                        results.Add(row);
                    }
                }
            }
            return results.ToArray();
        }

        private static string TypeCorrectionProvider(ProviderRoute route) =>
            InvoiceProviderCatalog.Get(route.ProviderKey).DisplayName + (route.TestMode ? " / Δοκιμαστικό" : " / Παραγωγικό");

        private static string TypeCorrectionMethod(InvoiceTypeCorrectionPlan plan) =>
            (plan.DirectCancellation ? "Ακύρωση MARK · fallback 11.4 → 1.1" : "11.4 → 1.1") +
            (plan.Original.MyDieselCouponCaseId.HasValue ? " · ενημέρωση myDiesel MARK" : "");

        private static InvoiceTypeCorrectionPlan PrepareInvoiceTypeCorrectionPlan(IFiscalDocumentLease lease,
            Data.Invoice invoice, InvoiceWorkflowSnapshot frozen, Guid retailCreditType,
            SqlInvoiceTypeCorrectionStore store, Action<long> acceptedMark = null)
        {
            var history = new SqlFiscalHistoryStore(sql.Value);
            var submission = history.Load(lease, FiscalJournalFamily.Submission).LastOrDefault();
            if (submission?.State != "Accepted")
                throw new InvalidOperationException("Η αρχική διαβίβαση δεν έχει επιβεβαιωθεί. Απαιτείται ανάκτηση κατάστασης.");
            var route = ProviderSubmissionService.ReadAcceptedRoute(submission);
            var response = ProviderSubmissionService.ReadAcceptedResponse(submission);
            long originalMark;
            if (!long.TryParse(response.Mark, System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture, out originalMark) || originalMark <= 0)
                throw new InvalidOperationException("Η αποδεκτή διαβίβαση δεν περιέχει έγκυρο MARK.");
            // The durable fiscal journal is authoritative.  Repair the legacy
            // projection even when a later eligibility check excludes the row,
            // and show the accepted MARK instead of the default numeric zero.
            acceptedMark?.Invoke(originalMark);
            ProjectAcceptedTypeCorrectionEvidence(invoice.InvoiceId, response, originalMark);

            if (invoice.ChildInvoiceRelations.Any())
                throw new InvalidOperationException("Έχουν ήδη εκδοθεί μεταγενέστερα συσχετισμένα παραστατικά / πιστώσεις για το παραστατικό.");
            ValidateInvoiceTypeCorrectionHistory(history, lease, frozen);
            ValidateMyDieselTypeCorrection(frozen, invoice.InvoiceId, originalMark, false);
            new CurrentCredentials().Resolve(route);

            var snapshot = InvoiceTypeCorrectionPlan.Copy(frozen);
            snapshot.Provider = route;
            bool direct = route.ProviderKey == InvoiceProviderCatalog.MyDataKey;
            var config = SharedInvoicingSettings.LoadRuntime();
            Guid creditId = Guid.Empty;
            string creditSeries = "";
            using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                if (!invoice.TraderId.HasValue || invoice.IsEuromat == true)
                    throw new InvalidOperationException("Απαιτείται συναλλασσόμενος και απουσία πληρωμής στόλου.");
                if (invoice.PaymentType != 0 && invoice.PaymentType != 1)
                    throw new InvalidOperationException("Επιτρέπονται μόνο μετρητά ή επί πιστώσει, χωρίς POS/IRIS.");
                if (invoice.InvoiceType.TransactionType != 0 || invoice.InvoiceType.HasFinancialTransactions == false ||
                    invoice.InvoiceType.ForcesDelivery == true)
                    throw new InvalidOperationException("Απαιτείται οικονομικό παραστατικό πώλησης χωρίς υποχρεωτική διακίνηση.");

                var credit = db.InvoiceTypes.SingleOrDefault(type => type.InvoiceTypeId == retailCreditType);
                if (credit == null || credit.OfficialEnumerator != 175 || !credit.Printable ||
                    credit.Invalidated == true || credit.ForcesDelivery == true || credit.TransactionType != 0 ||
                    credit.HasFinancialTransactions == false)
                    throw new InvalidOperationException("Επιλέξτε οικονομικό πιστωτικό λιανικής χωρίς υποχρεωτική διακίνηση.");
                creditId = Guid.NewGuid();
                creditSeries = credit.DefaultSeries ?? "";

                var mapping = InvoiceTypeCorrectionPlan.Copy(config.Mapping);
                mapping.Company = ReadHostCompany();
                mapping.Environment = route.TestMode ? FiscalEnvironment.Test : FiscalEnvironment.Production;
                var live = new HostInvoiceMapper().MapForTypeCorrection(invoice, mapping);
                var builder = new InvoiceBuilder(new InvoiceTypeResolver());
                if (builder.Build(live).InvoiceHeader.InvoiceType != "1.1")
                    throw new InvalidOperationException("Με τις τρέχουσες ρυθμίσεις το παραστατικό δεν επιλύεται ως 1.1.");
                var comparison = InvoiceTypeCorrectionPlan.Copy(live);
                comparison.Invoice.InvoiceSeries.InvoiceType.IsRetail = true;
                if (JsonConvert.SerializeObject(builder.Build(comparison)) != JsonConvert.SerializeObject(builder.Build(snapshot.Invoice)))
                    throw new InvalidOperationException("Τα σημερινά στοιχεία διαφέρουν από την αποθηκευμένη αρχική έκδοση πέρα από τον τύπο 11.1/1.1.");

                var plan = new InvoiceTypeCorrectionPlan {
                    OriginalId = invoice.InvoiceId, CreditId = creditId, ReplacementId = Guid.NewGuid(),
                    OriginalTypeId = invoice.InvoiceTypeId, CreditTypeId = retailCreditType,
                    OriginalAttemptId = submission.AttemptId, OriginalDescription = invoice.Description,
                    CreditSeries = creditSeries, ReplacementSeries = invoice.Series ?? "", OriginalMark = originalMark,
                    DirectCancellation = direct, Original = snapshot, Host = store.ReadHost(invoice.InvoiceId)
                };
                InvoiceTypeCorrectionPolicy.Validate(plan);
                return plan;
            }
        }

        private static void ValidateInvoiceTypeCorrectionHistory(SqlFiscalHistoryStore history,
            IFiscalDocumentLease lease, InvoiceWorkflowSnapshot frozen)
        {
            if (history.Load(lease, FiscalJournalFamily.Payment).Any() ||
                history.Load(lease, FiscalJournalFamily.ProviderSession).Any())
                throw new InvalidOperationException("Υπάρχει ιστορικό πληρωμής POS/IRIS ή συνεδρίας παρόχου που απαιτεί συμφωνία.");

            var cancellations = history.Load(lease, FiscalJournalFamily.Cancellation);
            if (cancellations.Any(item => item.State != "Rejected" && item.State != "FailedBeforeDispatch"))
                throw new InvalidOperationException("Υπάρχει ακύρωση MARK σε επιτυχή, εκκρεμή ή άγνωστη κατάσταση που απαιτεί συμφωνία.");

            var signing = history.Load(lease, FiscalJournalFamily.Signing);
            if (frozen.Signing == null)
            {
                if (signing.Any())
                    throw new InvalidOperationException("Υπάρχει ορφανό ιστορικό φορολογικής σήμανσης που δεν ανήκει στην αποθηκευμένη ροή έκδοσης.");
                return;
            }
            var acceptedSigning = signing.LastOrDefault();
            if (acceptedSigning == null || acceptedSigning.AttemptId != frozen.Signing.AttemptId ||
                acceptedSigning.State != "Signed")
                throw new InvalidOperationException("Η φορολογική σήμανση της αρχικής ροής δεν έχει συμφωνηθεί ως επιτυχής.");
        }

        private static void ProjectAcceptedTypeCorrectionEvidence(Guid invoiceId, InvoiceResponse response,
            long acceptedMark)
        {
            using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                var row = db.MyDataInvoices.SingleOrDefault(item => item.InvoiceId == invoiceId);
                if (row == null)
                {
                    row = new Data.MyDataInvoice {
                        MyDataInvoiceId = Guid.NewGuid(), InvoiceId = invoiceId, DateTimeSent = DateTime.Now
                    };
                    db.Add(row);
                }
                row.Mark = acceptedMark;
                row.Status = 3;
                row.Uid = response.Uid;
                row.QrCodeUrl = response.QrCodeUrl;
                row.VerificationHash = response.VerificationHash;
                row.ProviderUrl = response.ProviderUrl;
                row.InvoiceUrl = response.InvoiceUrl;
                row.Errors = "";
                db.SaveChanges();
            }
        }

        private static InvoiceTypeCorrectionRow InvoiceTypeSavedRow(InvoiceTypeCorrectionRecord row)
        {
            var plan = JsonConvert.DeserializeObject<InvoiceTypeCorrectionPlan>(row.PlanJson);
            using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                var credit = row.CreditInvoiceId.HasValue ? db.Invoices.SingleOrDefault(i => i.InvoiceId == row.CreditInvoiceId.Value) : null;
                var replacement = db.Invoices.SingleOrDefault(i => i.InvoiceId == row.ReplacementInvoiceId);
                return new InvoiceTypeCorrectionRow {
                    Id = row.OriginalInvoiceId, Document = plan.OriginalDescription,
                    Customer = plan.Original.Invoice.Invoice.Trader?.LastName ?? "",
                    VatNumber = plan.Original.Invoice.Invoice.Trader?.TaxRegistrationId ?? "",
                    Date = plan.Original.Invoice.Invoice.PrintedDateTime, Provider = TypeCorrectionProvider(plan.Original.Provider),
                    Mark = plan.OriginalMark, Amount = plan.Original.Invoice.Invoice.TotalAmount,
                    Method = TypeCorrectionMethod(plan),
                    Credit = credit?.Description ?? (plan.DirectCancellation
                        ? (row.State == "CancellationRejected" || row.State == "CreditPending" ? "Εκκρεμεί (fallback)" : "—")
                        : "Εκκρεμεί"),
                    Replacement = replacement?.Description ?? "Εκκρεμεί", Status = row.Message, Eligible = false
                };
            }
        }

        internal static InvoiceTypeCorrectionRow[] RunInvoiceTypeCorrections(InvoiceTypeCorrectionRow[] selected,
            bool resume, Func<bool> pause, Action<string> report)
        {
            var user = RequireInvoiceTypeCorrectionAdmin();
            var store = new SqlInvoiceTypeCorrectionStore(sql.Value);
            using (store.EnterBatch())
            {
                var all = store.Load();
                Guid batch = Guid.NewGuid();
                if (!resume)
                    foreach (var row in selected.Where(item => item.Selected && item.Eligible))
                    {
                        if (pause()) break;
                        RequireInvoiceTypeCorrectionAdmin();
                        using (var lease = sql.Value.EnterInvoice(row.Id))
                        {
                            if (all.Any(item => item.OriginalInvoiceId == row.Id))
                                throw new InvalidOperationException("Το παραστατικό ανήκει ήδη σε παρτίδα. Χρησιμοποιήστε συνέχεια εκκρεμοτήτων.");
                            using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
                            {
                                var latest = PrepareInvoiceTypeCorrectionPlan(lease, db.Invoices.Single(i => i.InvoiceId == row.Id),
                                    row.Plan.Original, row.Plan.CreditTypeId, store);
                                if (!SqlHeatingCorrectionStore.SameHost(row.Plan.Host, latest.Host) || row.Plan.OriginalAttemptId != latest.OriginalAttemptId ||
                                    row.Plan.DirectCancellation != latest.DirectCancellation || row.Plan.CreditSeries != latest.CreditSeries)
                                    throw new InvalidOperationException("Τα στοιχεία άλλαξαν μετά τον έλεγχο. Εκτελέστε νέο έλεγχο παραστατικών.");
                            }
                            store.Reserve(lease, new InvoiceTypeCorrectionRecord {
                                OriginalInvoiceId = row.Id, BatchId = batch,
                                CreditInvoiceId = row.Plan.CreditId,
                                ReplacementInvoiceId = row.Plan.ReplacementId, PlanJson = JsonConvert.SerializeObject(row.Plan), CreatedBy = user
                            });
                        }
                    }

                var work = store.Load().Where(item => item.State != "Completed" && (resume || item.BatchId == batch)).ToArray();
                foreach (var record in work)
                {
                    if (pause()) break;
                    RequireInvoiceTypeCorrectionAdmin();
                    var plan = JsonConvert.DeserializeObject<InvoiceTypeCorrectionPlan>(record.PlanJson);
                    report(plan.OriginalDescription + ": επεξεργασία...");
                    using (var lease = sql.Value.EnterInvoice(record.OriginalInvoiceId))
                    {
                        try
                        {
                            InvoiceTypeCorrectionRunner.Run(plan.DirectCancellation,
                                () => CancelOriginalMark(plan),
                                (credit, newFiscalDocument) => IssueInvoiceTypeCorrectionChild(plan, credit,
                                    newFiscalDocument, record.CreatedBy),
                                (state, message) => {
                                    long? mark = state == "MarkCancelled" ? (long?)ReadAcceptedCancellationMark(lease) : null;
                                    store.Progress(lease, state, message, mark); report(plan.OriginalDescription + ": " + message);
                                }, pause, plan.Original.MyDieselCouponCaseId.HasValue
                                    ? (Func<InvoiceWorkflowResult, string>)(replacement => FinalizeMyDieselTypeCorrection(plan, replacement))
                                    : null);
                        }
                        catch (Exception ex) { store.Progress(lease, "NeedsReview", ex.Message); report(plan.OriginalDescription + ": " + ex.Message); }
                    }
                }
                return store.Load().Select(InvoiceTypeSavedRow).ToArray();
            }
        }

        private static long CancelOriginalMark(InvoiceTypeCorrectionPlan plan)
        {
            // This must run immediately before any external cancellation.  The
            // signature-free retransmission is valid only when the source still
            // represents exactly the invoice that received the original MARK.
            ValidateInvoiceTypeCorrectionSourceUnchanged(plan);
            using (var lease = sql.Value.EnterInvoice(plan.OriginalId))
            {
                var history = new SqlFiscalHistoryStore(sql.Value);
                var submission = history.Load(lease, FiscalJournalFamily.Submission)
                    .SingleOrDefault(item => item.AttemptId == plan.OriginalAttemptId && item.State == "Accepted");
                if (submission == null) throw new InvalidOperationException("Η αποδεκτή αρχική διαβίβαση άλλαξε.");
                var route = ProviderSubmissionService.ReadAcceptedRoute(submission);
                var operations = new SqlFiscalOperationStore(sql.Value);
                var previous = operations.Load(lease, false).LastOrDefault();
                if (previous != null)
                {
                    if (previous.State == FiscalTransmissionState.Accepted.ToString())
                    {
                        long saved = ReadCancellationMark(previous.Evidence);
                        if (saved > 0)
                        {
                            SaveLegacyCancellationEvidence(plan.OriginalId, plan.OriginalMark, saved);
                            return saved;
                        }
                        throw new InvalidOperationException("Η ακύρωση καταγράφηκε ως επιτυχής χωρίς cancellation MARK.");
                    }
                    if (previous.State == FiscalTransmissionState.Prepared.ToString() ||
                        previous.State == FiscalTransmissionState.Sending.ToString() ||
                        previous.State == FiscalTransmissionState.Unknown.ToString())
                        throw new InvalidOperationException("Η έκβαση προηγούμενης ακύρωσης MARK δεν έχει συμφωνηθεί. Δεν έγινε δεύτερη αποστολή.");
                    if (previous.State == FiscalTransmissionState.Rejected.ToString())
                        throw new InvoiceMarkCancellationRejectedException("καταγεγραμμένη απόρριψη από την ΑΑΔΕ");
                }
                var cancellation = ResolveMarkCancellationConnection(route);

                var attempt = operations.BeginCancellation(lease, submission.AttemptId, "application/xml",
                    Encoding.UTF8.GetBytes("<CancelInvoice><mark>" + plan.OriginalMark.ToString(
                        System.Globalization.CultureInfo.InvariantCulture) + "</mark></CancelInvoice>"));
                operations.SaveCancellation(lease, attempt, FiscalTransmissionState.Sending,
                    "Αποστολή ακύρωσης MARK " + plan.OriginalMark.ToString(System.Globalization.CultureInfo.InvariantCulture));
                Exedron.MyData.Models.Responses.ResponseDoc response;
                try
                {
                    response = Exedron.MyData.InvoiceCancelation.MakeRequest(cancellation.Url, plan.OriginalMark,
                        cancellation.User, cancellation.SubscriptionKey, true);
                }
                catch (Exception ex)
                {
                    operations.SaveCancellation(lease, attempt, FiscalTransmissionState.Unknown,
                        "Μη επιβεβαιωμένη έκβαση ακύρωσης: " + ex.GetType().Name + " - " + ex.Message);
                    throw new InvalidOperationException("Η έκβαση της ακύρωσης MARK δεν επιβεβαιώθηκε. Απαιτείται συμφωνία με την ΑΑΔΕ πριν από συνέχεια.", ex);
                }
                long mark = response?.response?.cancellationMark.HasValue == true ? (long)response.response.cancellationMark.Value : 0;
                string evidence = response?.response?.responseData;
                if (string.IsNullOrWhiteSpace(evidence)) evidence = JsonConvert.SerializeObject(new {
                    Status = response?.response?.statusCode, CancellationMark = mark, Errors = response?.response?.errors
                });
                if (mark <= 0)
                {
                    operations.SaveCancellation(lease, attempt, FiscalTransmissionState.Rejected, evidence);
                    string errors = response?.response?.errors == null
                        ? "δεν επιστράφηκε cancellation MARK"
                        : string.Join("; ", response.response.errors.Select(error => error.code + " - " + error.message));
                    throw new InvoiceMarkCancellationRejectedException(errors);
                }
                operations.SaveCancellation(lease, attempt, FiscalTransmissionState.Accepted, evidence);
                SaveLegacyCancellationEvidence(plan.OriginalId, plan.OriginalMark, mark);
                return mark;
            }
        }

        private static long ReadAcceptedCancellationMark(IFiscalDocumentLease lease)
        {
            var accepted = new SqlFiscalOperationStore(sql.Value).Load(lease, false)
                .LastOrDefault(item => item.State == FiscalTransmissionState.Accepted.ToString());
            return accepted == null ? 0 : ReadCancellationMark(accepted.Evidence);
        }

        private static InvoiceWorkflowResult IssueInvoiceTypeCorrectionChild(InvoiceTypeCorrectionPlan plan,
            bool credit, bool newFiscalDocument, Guid user)
        {
            RequireInvoiceTypeCorrectionAdmin();
            // Recheck after cancellation as well: do not create either an
            // unsigned retransmission or a fiscal fallback from changed data.
            ValidateInvoiceTypeCorrectionSourceUnchanged(plan);
            Guid id = credit ? plan.CreditId : plan.ReplacementId;
            using (var lease = sql.Value.EnterInvoice(id))
            {
                var store = new SqlInvoiceTypeCorrectionStore(sql.Value);
                store.CreateChild(lease, plan.Host, credit ? plan.CreditTypeId : plan.OriginalTypeId,
                    credit ? plan.CreditSeries : plan.ReplacementSeries, user,
                    (credit ? "Πλήρης πίστωση 11.4" : "Επανέκδοση 1.1") + " του " + plan.OriginalDescription +
                    " — διόρθωση εσφαλμένου τύπου 11.1. Συμψηφισμός χωρίς νέα είσπραξη ή παράδοση.", DateTime.Now);
                var engine = new InvoiceWorkflowService(sql.Value,
                    new ProviderSubmissionService(sql.Value, new CurrentCredentials()), null,
                    newFiscalDocument && plan.Original.Signing != null ? SigningService() : null,
                    new FiscalPrintService(sql.Value,
                        new SharedInvoicePrinting(() => settings.Value.DefaultPrinter),
                        new StoredInvoicePrintMaterializer(sql.Value)),
                    (snapshot, response) => SharedInvoiceLaserReportRenderer
                        .PrepareStoredData(sql.Value, snapshot, response),
                    (invoiceId, result) => { if (result.Accepted) Project(invoiceId, result); });
                return engine.Process(id, new InvoiceTypeCorrectionSource(plan, credit,
                    newFiscalDocument), false, retryRejected: true);
            }
        }

        private sealed class InvoiceTypeCorrectionSource : IInvoiceWorkflowSource
        {
            private readonly InvoiceTypeCorrectionPlan plan;
            private readonly bool credit;
            private readonly bool newFiscalDocument;
            internal InvoiceTypeCorrectionSource(InvoiceTypeCorrectionPlan value, bool isCredit,
                bool issueAsNewFiscalDocument)
            { plan = value; credit = isCredit; newFiscalDocument = issueAsNewFiscalDocument; }
            public InvoiceWorkflowSnapshot Prepare(IFiscalDocumentLease lease)
            {
                using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
                {
                    var invoice = db.Invoices.Single(item => item.InvoiceId == lease.Document.DocumentId);
                    bool mustSign = newFiscalDocument && plan.Original.Signing != null;
                    var content = PrintAgent.RenderSharedInvoiceContent(db, invoice);
                    // This is a repair of an accepted 11.1 transmission.  Its
                    // durable provider route is authoritative even when the
                    // current 11.1/11.4 type policies say "do not transmit".
                    // Printer selection remains independent of that policy.
                    var configuredPrinter = JsonConvert.DeserializeObject<SharedPrinterProfile>(
                        JsonConvert.SerializeObject(settings.Value.DefaultPrinter ?? new SharedPrinterProfile()));
                    configuredPrinter.Thermal = invoice.InvoiceType.IsLaserPrint != true;
                    configuredPrinter.PrinterName = invoice.InvoiceType.IsLaserPrint == true
                        ? configuredPrinter.PrinterName
                        : SharedInvoiceConfiguration.PrinterName(invoice);
                    var snapshot = new InvoiceWorkflowSnapshot {
                        InvoiceId = invoice.InvoiceId, Provider = plan.Original.Provider,
                        Invoice = plan.Request(credit, invoice.Number, invoice.Series, invoice.TransactionDate),
                        PrintFormat = StoredInvoicePrintMaterializer.StoredFormat,
                        PrintDestination = JsonConvert.SerializeObject(configuredPrinter),
                        PrintContent = Encoding.UTF8.GetBytes(content),
                        CompanyQrContent = Reports.BrandData.SetBrandData(invoice)
                    };
                    if (mustSign)
                        snapshot.Signing = HostFiscalSigningMapper.Invoice(invoice,
                            plan.Original.Signing.Scope, content, false);
                    return snapshot;
                }
            }
        }

        private static void ValidateInvoiceTypeCorrectionSourceUnchanged(
            InvoiceTypeCorrectionPlan plan)
        {
            var store = new SqlInvoiceTypeCorrectionStore(sql.Value);
            if (!SqlHeatingCorrectionStore.SameHost(plan.Host, store.ReadHost(plan.OriginalId)))
                throw new InvalidOperationException("Τα αποθηκευμένα στοιχεία του αρχικού παραστατικού άλλαξαν. Δεν στάλθηκε ακύρωση MARK ή διορθωτικό παραστατικό.");

            using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                var invoice = db.Invoices.SingleOrDefault(item => item.InvoiceId == plan.OriginalId);
                if (invoice == null)
                    throw new InvalidOperationException("Το αρχικό παραστατικό δεν βρέθηκε.");
                var config = SharedInvoicingSettings.LoadRuntime();
                var mapping = InvoiceTypeCorrectionPlan.Copy(config.Mapping);
                mapping.Company = ReadHostCompany();
                mapping.Environment = plan.Original.Provider.TestMode
                    ? FiscalEnvironment.Test : FiscalEnvironment.Production;
                var current = new HostInvoiceMapper().MapForTypeCorrection(invoice, mapping);
                var builder = new InvoiceBuilder(new InvoiceTypeResolver());
                if (builder.Build(current).InvoiceHeader.InvoiceType != "1.1")
                    throw new InvalidOperationException("Με τις τρέχουσες ρυθμίσεις το αρχικό παραστατικό δεν επιλύεται πλέον ως 1.1.");
                current.Invoice.InvoiceSeries.InvoiceType.IsRetail = true;
                if (JsonConvert.SerializeObject(builder.Build(current)) !=
                    JsonConvert.SerializeObject(builder.Build(plan.Original.Invoice)))
                    throw new InvalidOperationException("Τα στοιχεία του αρχικού παραστατικού ή του συναλλασσόμενου άλλαξαν μετά τον έλεγχο. Δεν στάλθηκε ακύρωση MARK ή διορθωτικό παραστατικό.");
            }
            ValidateMyDieselTypeCorrection(plan.Original, plan.OriginalId, plan.OriginalMark, true);
        }

        private static void ValidateMyDieselTypeCorrection(InvoiceWorkflowSnapshot snapshot,
            Guid invoiceId, long originalMark, bool allowCorrectionInProgress)
        {
            if (!snapshot.MyDieselCouponCaseId.HasValue) return;
            if (snapshot.Version != 2 || snapshot.MyDieselScope == null)
                throw new InvalidOperationException("Η αποθηκευμένη σύνδεση myDiesel είναι ελλιπής.");
            var store = new SqlMyDieselStore(sql.Value);
            using (var lease = store.Acquire(snapshot.MyDieselCouponCaseId.Value))
            {
                var coupon = store.Load(lease);
                if (coupon == null || coupon.InvoiceId != invoiceId)
                    throw new InvalidOperationException("Το κουπόνι myDiesel δεν ανήκει στο αρχικό παραστατικό.");
                var scope = snapshot.MyDieselScope;
                if (coupon.Environment != scope.Environment || coupon.CompanyVat != scope.CompanyVat ||
                    coupon.ReservedBranchNum != scope.BranchNum || coupon.CredentialProfileId != scope.CredentialProfileId ||
                    coupon.ReservedStationAmdika != scope.StationAmdika)
                    throw new InvalidOperationException("Τα αποθηκευμένα στοιχεία του κουπονιού myDiesel διαφέρουν από την αρχική έκδοση.");
                string expected = originalMark.ToString(System.Globalization.CultureInfo.InvariantCulture);
                if (coupon.InvoiceMark != expected || coupon.MarkSourceInvoiceId != invoiceId)
                {
                    bool correctionExists = allowCorrectionInProgress && coupon.MarkSourceInvoiceId == invoiceId &&
                        store.LoadAttempts(lease).Any(attempt => attempt.Operation == MyDieselOperation.Redeem &&
                            !string.IsNullOrWhiteSpace(attempt.OperatorReason) && MyDieselAttemptMark(attempt) == coupon.InvoiceMark);
                    if (!correctionExists)
                        throw new InvalidOperationException("Το MARK του κουπονιού myDiesel δεν συμφωνεί με το αρχικό παραστατικό.");
                    return;
                }
                var remote = MyDieselCouponStates.Parse(coupon.RemoteStatus);
                if (!MyDieselCouponStates.CanRedeem(remote) || remote == MyDieselCouponStatus.Reserved)
                    throw new InvalidOperationException("Η κατάσταση του κουπονιού myDiesel (" +
                        (coupon.RemoteStatus ?? "άγνωστη") + ") δεν επιτρέπει ενημέρωση MARK.");
                if (!store.LoadAttempts(lease).Any(attempt => attempt.Operation == MyDieselOperation.Redeem))
                    throw new InvalidOperationException("Δεν βρέθηκε η αρχική εξαργύρωση του κουπονιού myDiesel.");
            }
        }

        private static string FinalizeMyDieselTypeCorrection(InvoiceTypeCorrectionPlan plan,
            InvoiceWorkflowResult replacement)
        {
            if (!plan.Original.MyDieselCouponCaseId.HasValue) return null;
            string mark = replacement?.Response?.Mark;
            MyDieselRedeemRequest.NormalizeMark(mark);
            var result = SharedMyDieselRuntime.CreateService().CorrectMarkOrRecover(
                plan.Original.MyDieselCouponCaseId.Value, plan.OriginalId, mark,
                "Διόρθωση τύπου myDATA 11.1 σε 1.1 — " + plan.OriginalDescription);
            var coupon = result.CouponCase;
            var remote = MyDieselCouponStates.Parse(coupon?.RemoteStatus);
            bool completed = result.OperationState == MyDieselOperationState.Succeeded && coupon != null &&
                (coupon.LocalStage == MyDieselCaseStage.Redeemed ||
                 coupon.LocalStage == MyDieselCaseStage.VerificationPending ||
                 remote == MyDieselCouponStatus.Verified || remote == MyDieselCouponStatus.VerifiedPaid ||
                 remote == MyDieselCouponStatus.VerifiedUnpayable);
            if (completed) return null;
            if (result.OperationState == MyDieselOperationState.Unknown ||
                result.OperationState == MyDieselOperationState.FailedBeforeDispatch)
                return "Η επανέκδοση 1.1 έγινε αποδεκτή, αλλά η ενημέρωση MARK στο myDiesel εκκρεμεί (" +
                    result.OperationState + "). Χρησιμοποιήστε «Συνέχεια εκκρεμοτήτων».";
            throw new InvalidOperationException("Η επανέκδοση 1.1 έγινε αποδεκτή, αλλά το myDiesel δεν αποδέχθηκε την ενημέρωση MARK (" +
                result.OperationState + ", " + (coupon?.RemoteStatus ?? "άγνωστη κατάσταση") + "). Απαιτείται συμφωνία myDiesel.");
        }

        private static string MyDieselAttemptMark(MyDieselAttemptRecord attempt)
        {
            if (attempt == null || string.IsNullOrWhiteSpace(attempt.RequestJson)) return null;
            try { return (string)JObject.Parse(attempt.RequestJson)["mark"]; }
            catch (JsonException) { return null; }
        }

        internal static void PrintInvoiceTypeCorrectionsCompleted(Func<bool> pause)
        {
            RequireInvoiceTypeCorrectionAdmin();
            foreach (var row in new SqlInvoiceTypeCorrectionStore(sql.Value).Load().Where(item => item.State == "Completed"))
            {
                var ids = new List<Guid>();
                if (row.CreditInvoiceId.HasValue)
                    using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
                        if (db.Invoices.Any(invoice => invoice.InvoiceId == row.CreditInvoiceId.Value))
                            ids.Add(row.CreditInvoiceId.Value);
                ids.Add(row.ReplacementInvoiceId);
                foreach (var id in ids)
                {
                    if (pause()) return;
                    RequireInvoiceTypeCorrectionAdmin();
                    var result = Process(id, true);
                    if (result.Print?.State != "HandedOff" && result.Print?.State != "Confirmed")
                        throw new InvalidOperationException("Δεν επιβεβαιώθηκε η εκτύπωση του " + id + " (" + result.Print?.State + ").");
                }
            }
        }
    }
}
