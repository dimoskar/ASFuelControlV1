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
        internal const string OperatorRequestedCopyReason = "Operator requested an invoice copy.";

        // Routing/device settings are stable for this process. Unused provider
        // connections read the latest saved account and credential profile.
        internal static bool Enabled => settings.Value.Enabled;
        internal static bool ProtectsInvoices => Enabled || settings.Value.ProtectedKinds.Contains(FiscalDocumentKind.Invoice);
        internal static bool SupportsIris => settings.Value.Pos?.Provider.ProviderKey == InvoiceProviderCatalog.ITimologiaKey && settings.Value.Pos.PosMode == "MELLON";
        // The PrintAgent and an administrator action can briefly meet on the
        // same invoice.  Keep the SQL application lock exclusive, but wait for
        // the current worker instead of turning a transient overlap into an
        // immediate "owned by another worker" failure in correction batches.
        private static readonly Lazy<SqlFiscalDatabase> sql = new Lazy<SqlFiscalDatabase>(() =>
            new SqlFiscalDatabase(Properties.Settings.Default.DBConnection, 15000));
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
                new FiscalPrintService(database, new SharedInvoicePrinting(() => settings.Value.DefaultPrinter)),
                (snapshot,response) => SharedInvoicePrinting.Render(snapshot,response,SharedInvoiceLaserReportRenderer.Render), Project, CurrentConnection,
                new SharedMyDieselRedemptionCoordinator(database));
        });
        internal static bool Handles(Guid invoiceId)
        {
            if (ProtectsInvoices) return true;
            return Owns(invoiceId);
        }
        internal static void EnsureLegacyFiscalSigningAllowed(
            Data.Invoice invoice)
        {
            if (invoice == null)
            {
                throw new ArgumentNullException(nameof(invoice));
            }
            SharedInvoiceConfiguration current =
                SharedInvoicingSettings.LoadRuntime();
            bool providerOwnsSigning;
            try
            {
                providerOwnsSigning = current.ProviderOwnsFiscalSigning(invoice);
            }
            catch (InvalidOperationException)
            {
                return;
            }
            if (providerOwnsSigning)
            {
                throw new InvalidOperationException(
                    "Ο τύπος παραστατικού χρησιμοποιεί πάροχο τιμολόγησης. " +
                    "Η legacy σήμανση από φορολογικό μηχανισμό απαγορεύτηκε και δεν στάλθηκε αίτημα στη συσκευή.");
            }
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
        internal static void ValidateCancellationTransformation(IEnumerable<Guid> invoiceIds,
            Guid targetInvoiceTypeId)
        {
            var ids = (invoiceIds ?? Enumerable.Empty<Guid>()).Where(id => id != Guid.Empty)
                .Distinct().ToArray();
            if (ids.Length == 0)
                return;
            using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                var target = db.InvoiceTypes.SingleOrDefault(type =>
                    type.InvoiceTypeId == targetInvoiceTypeId);
                if (!HostInvoiceMapper.IsCancellationDocument(target))
                    return;
                var originals = db.Invoices.Where(invoice => ids.Contains(invoice.InvoiceId)).ToArray();
                foreach (var original in originals)
                {
                    if (!IsCancellationOrCreditType(db, original.InvoiceType) ||
                        !HasExternalTransmissionEvidence(db, original))
                        continue;
                    throw new InvalidOperationException(
                        "Το πιστωτικό " + original.InvoiceType.Abbreviation + " " + original.Number +
                        " έχει ήδη σημανθεί ή διαβιβαστεί. Δεν επιτρέπεται δεύτερο πιστωτικό 5.1 ή 11.4 " +
                        "πάνω στο πιστωτικό. Αν εκδόθηκε απευθείας μέσω myDATA ακυρώνεται το MARK του· " +
                        "αν εκδόθηκε μέσω παρόχου απαιτείται θετικό διορθωτικό παραστατικό. " +
                        "Δεν δημιουργήθηκε ακυρωτικό παραστατικό.");
                }
            }
        }
        internal static bool TryCancelCancellationInvoiceMark(Guid invoiceId,
            out long cancellationMark)
        {
            cancellationMark = 0;
            Data.MyDataInvoice legacyEvidence;
            using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                var invoice = db.Invoices.SingleOrDefault(item => item.InvoiceId == invoiceId);
                if (invoice == null)
                    throw new InvalidOperationException("Το παραστατικό δεν βρέθηκε.");
                // Old databases do not set IsCancelation consistently on credit
                // types. A type configured as the child of a cancellation
                // transformation is also a cancellation/credit document.
                if (!IsCancellationOrCreditType(db, invoice.InvoiceType))
                    return false;
                legacyEvidence = db.MyDataInvoices.FirstOrDefault(item =>
                    item.InvoiceId == invoiceId);
                if (legacyEvidence?.CanceledByMark.GetValueOrDefault() > 0)
                {
                    cancellationMark = legacyEvidence.CanceledByMark.Value;
                    return true;
                }
            }

            if (!Owns(invoiceId))
            {
                using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
                {
                    var evidence = db.MyDataInvoices.FirstOrDefault(item =>
                        item.InvoiceId == invoiceId);
                    // A local/unissued credit has no MARK to cancel. Let the caller
                    // continue with the configured local cancellation
                    // transformation instead of dereferencing a missing legacy row.
                    if (evidence == null || evidence.Mark.GetValueOrDefault() <= 0)
                        return false;
                    if (!string.IsNullOrWhiteSpace(evidence.ProviderUrl))
                        throw new InvalidOperationException(
                            "Το παραστατικό έχει εκδοθεί μέσω παρόχου. Η ΑΑΔΕ δεν επιτρέπει " +
                            "την ακύρωση MARK οικονομικού παραστατικού παρόχου (σφάλμα 249) " +
                            "και το API παρόχων διαθέτει ακύρωση μόνο για δελτία διακίνησης. " +
                            "Δεν στάλθηκε αίτηση ακύρωσης και το παραστατικό παραμένει ενεργό. " +
                            "Απαιτείται το κατάλληλο διορθωτικό παραστατικό.");
                    PrintAgent.CancelInvoice(db, evidence);
                    if (evidence.CanceledByMark.GetValueOrDefault() <= 0)
                        throw new InvalidOperationException(
                            "Η ΑΑΔΕ δεν επιβεβαίωσε την ακύρωση του MARK.");
                    cancellationMark = evidence.CanceledByMark.Value;
                    return true;
                }
            }

            long sourceMark;
            long acceptedCancellationMark = 0;
            using (var lease = sql.Value.EnterInvoice(invoiceId))
            {
                var history = new SqlFiscalHistoryStore(sql.Value);
                var acceptedSubmission = history.Load(lease, FiscalJournalFamily.Submission)
                    .LastOrDefault(item => item.State == FiscalTransmissionState.Accepted.ToString());
                if (acceptedSubmission == null)
                    return false;
                var acceptedRoute = ProviderSubmissionService.ReadAcceptedRoute(acceptedSubmission);
                var acceptedResponse = ProviderSubmissionService.ReadAcceptedResponse(acceptedSubmission);
                if (acceptedResponse == null || !long.TryParse(acceptedResponse.Mark,
                    System.Globalization.NumberStyles.None,
                    System.Globalization.CultureInfo.InvariantCulture, out sourceMark) || sourceMark <= 0)
                    throw new InvalidOperationException(
                        "Η επιβεβαιωμένη διαβίβαση δεν περιέχει έγκυρο MARK.");

                var operations = new SqlFiscalOperationStore(sql.Value);
                var previous = operations.Load(lease, false).LastOrDefault();
                if (previous != null)
                {
                    if (previous.State == FiscalTransmissionState.Accepted.ToString())
                    {
                        acceptedCancellationMark = ReadCancellationMark(previous.Evidence);
                        if (acceptedCancellationMark <= 0)
                            throw new InvalidOperationException(
                                "Η ακύρωση έχει καταγραφεί ως επιτυχής χωρίς έγκυρο cancellation MARK. Απαιτείται συμφωνία ιστορικού.");
                    }
                    else if (previous.State == FiscalTransmissionState.Prepared.ToString() ||
                             previous.State == FiscalTransmissionState.Sending.ToString() ||
                             previous.State == FiscalTransmissionState.Unknown.ToString())
                        throw new InvalidOperationException(
                            "Υπάρχει ήδη ακύρωση MARK σε εκκρεμή ή άγνωστη κατάσταση. Δεν έγινε δεύτερη αποστολή.");
                }

                if (acceptedCancellationMark <= 0)
                {
                    // Cancellation must use the same fiscal channel that issued the
                    // MARK. AADE rejects an ERP CancelInvoice for provider-issued
                    // financial documents with validation error 249.
                    var cancellation = ResolveMarkCancellationConnection(acceptedRoute);
                    var attempt = operations.BeginCancellation(lease,
                        acceptedSubmission.AttemptId, "application/xml",
                        Encoding.UTF8.GetBytes("<CancelInvoice><mark>" +
                            sourceMark.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                            "</mark></CancelInvoice>"));
                    operations.SaveCancellation(lease, attempt,
                        FiscalTransmissionState.Sending,
                        "Αποστολή ακύρωσης MARK " + sourceMark.ToString(
                            System.Globalization.CultureInfo.InvariantCulture));

                    Exedron.MyData.Models.Responses.ResponseDoc response;
                    try
                    {
                        response = Exedron.MyData.InvoiceCancelation.MakeRequest(
                            cancellation.Url, sourceMark, cancellation.User,
                            cancellation.SubscriptionKey, true);
                    }
                    catch (Exception ex)
                    {
                        operations.SaveCancellation(lease, attempt,
                            FiscalTransmissionState.Unknown,
                            "Μη επιβεβαιωμένη έκβαση ακύρωσης: " + ex.GetType().Name + " - " + ex.Message);
                        throw new InvalidOperationException(
                            "Η έκβαση της ακύρωσης MARK δεν επιβεβαιώθηκε. Δεν επιτρέπεται νέα προσπάθεια πριν από συμφωνία με την ΑΑΔΕ.", ex);
                    }

                    acceptedCancellationMark = response?.response?.cancellationMark.HasValue == true
                        ? (long)response.response.cancellationMark.Value
                        : 0;
                    string responseEvidence = response?.response?.responseData;
                    if (string.IsNullOrWhiteSpace(responseEvidence))
                        responseEvidence = JsonConvert.SerializeObject(new {
                            Status = response?.response?.statusCode,
                            CancellationMark = acceptedCancellationMark,
                            Errors = response?.response?.errors
                        });
                    if (acceptedCancellationMark <= 0)
                    {
                        operations.SaveCancellation(lease, attempt,
                            FiscalTransmissionState.Rejected, responseEvidence);
                        string errors = response?.response?.errors == null
                            ? "Η ΑΑΔΕ δεν επέστρεψε cancellation MARK."
                            : string.Join("\r\n", response.response.errors.Select(error =>
                                error.code + " - " + error.message));
                        throw new InvalidOperationException(errors);
                    }
                    operations.SaveCancellation(lease, attempt,
                        FiscalTransmissionState.Accepted, responseEvidence);
                }
            }

            SaveLegacyCancellationEvidence(invoiceId, sourceMark,
                acceptedCancellationMark);
            cancellationMark = acceptedCancellationMark;
            return true;
        }
        private sealed class MarkCancellationConnection
        {
            internal string Url;
            internal string User;
            internal string SubscriptionKey;
        }
        private static MarkCancellationConnection ResolveMarkCancellationConnection(
            ProviderRoute acceptedRoute)
        {
            if (acceptedRoute == null)
                throw new InvalidOperationException(
                    "Δεν είναι διαθέσιμη η αρχική διαδρομή διαβίβασης του MARK.");

            var provider = InvoiceProviderCatalog.Get(acceptedRoute.ProviderKey);
            if (!provider.SupportsInvoiceMarkCancellation)
                throw new InvalidOperationException(
                    "Το παραστατικό έχει εκδοθεί μέσω " + provider.DisplayName + ". Η ΑΑΔΕ " +
                    "απορρίπτει την ακύρωση MARK οικονομικών παραστατικών παρόχου (σφάλμα 249) " +
                    "και το επίσημο API παρόχων δεν διαθέτει CancelInvoice — μόνο " +
                    "CancelDeliveryNote για δελτία διακίνησης. Δεν στάλθηκε νέα αίτηση, " +
                    "το πιστωτικό παραμένει ενεργό και το αρχικό παραστατικό δεν " +
                    "αποδεσμεύτηκε. Απαιτείται το κατάλληλο διορθωτικό παραστατικό.");

            var secret = new CurrentCredentials().Resolve(acceptedRoute);
            if (secret == null || string.IsNullOrWhiteSpace(secret.SubscriptionKey) ||
                string.IsNullOrWhiteSpace(acceptedRoute.Account) ||
                string.IsNullOrWhiteSpace(acceptedRoute.MyDataBaseUrl))
                throw new InvalidOperationException(
                    "Δεν είναι διαθέσιμα τα αποθηκευμένα διαπιστευτήρια myDATA για την ακύρωση του MARK.");
            return new MarkCancellationConnection {
                Url = acceptedRoute.MyDataBaseUrl.TrimEnd('/'),
                User = acceptedRoute.Account.Trim(),
                SubscriptionKey = secret.SubscriptionKey.Trim()
            };
        }
        private static long ReadCancellationMark(string evidence)
        {
            if (string.IsNullOrWhiteSpace(evidence))
                return 0;
            try
            {
                var response = Exedron.MyData.Models.Responses.ResponseDoc.Deserialize(evidence);
                if (response?.response?.cancellationMark.HasValue == true)
                    return (long)response.response.cancellationMark.Value;
            }
            catch { }
            try
            {
                return (long?)Newtonsoft.Json.Linq.JObject.Parse(evidence)["CancellationMark"] ?? 0;
            }
            catch { return 0; }
        }
        private static void SaveLegacyCancellationEvidence(Guid invoiceId,
            long sourceMark, long cancellationMark)
        {
            using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                var evidence = db.MyDataInvoices.FirstOrDefault(item =>
                    item.InvoiceId == invoiceId);
                if (evidence == null)
                {
                    evidence = new Data.MyDataInvoice {
                        MyDataInvoiceId = Guid.NewGuid(),
                        InvoiceId = invoiceId,
                        Mark = sourceMark,
                        Status = 3
                    };
                    db.Add(evidence);
                }
                // The journal can contain the accepted source MARK even when an
                // older/partial legacy projection row was left with MARK 0.
                evidence.Mark = sourceMark;
                if (evidence.Status <= 0) evidence.Status = 3;
                evidence.CanceledByMark = cancellationMark;
                db.SaveChanges();
            }
        }
        private static bool IsCancellationOrCreditType(Data.DatabaseModel db,
            Data.InvoiceType type)
        {
            if (type == null)
                return false;
            if (type.IsCancelation == true || type.OfficialEnumerator == 215)
                return true;
            int cancellation = (int)Common.Enumerators.InvoiceTransformationTypeEnum.Cancelation;
            return db.InvoiceTypeTransforms.Any(transform =>
                transform.ChildInvoiceTypeId == type.InvoiceTypeId &&
                transform.TransformationMode == cancellation);
        }
        private static bool HasExternalTransmissionEvidence(Data.DatabaseModel db,
            Data.Invoice original)
        {
            // A device signature proves local fiscal signing, not transmission.
            // Signed-only invoices are cancelled by the configured local
            // cancellation transformation. Only durable myDATA/provider evidence
            // makes the original an externally issued document.
            if (db.MyDataInvoices.Any(item => item.InvoiceId == original.InvoiceId &&
                (item.Status == 3 || item.Mark.GetValueOrDefault() > 0 ||
                 item.CanceledByMark.GetValueOrDefault() > 0 || item.CancelationMark.GetValueOrDefault() > 0)))
                return true;
            using (var parentLease = sql.Value.EnterInvoice(original.InvoiceId))
            {
                var history = new SqlFiscalHistoryStore(sql.Value);
                return history.Load(parentLease,FiscalJournalFamily.Submission).Any(item =>
                    item.State == "Sending" || item.State == "Unknown" ||
                    item.State == "Queued" || item.State == "Accepted");
            }
        }
        private static bool HasUnresolvedSigningAttempt(Guid invoiceId)
        {
            using (var lease = sql.Value.EnterInvoice(invoiceId))
            {
                var signing = new SqlFiscalHistoryStore(sql.Value)
                    .Load(lease,FiscalJournalFamily.Signing).LastOrDefault();
                return signing != null && (signing.State == "Prepared" ||
                    signing.State == "Signing" || signing.State == "Unknown");
            }
        }
        internal static InvoiceWorkflowResult Process(Guid id, bool print = true, string reason = null,
            string selectedPrinter = null, bool automaticSamtecAmountCorrection = false)
        {
            if (new SqlHeatingCorrectionStore(sql.Value).IsChild(id,true))
                throw new InvalidOperationException("Το παραστατικό ελέγχεται από την παρτίδα διόρθωσης επιδόματος θέρμανσης. Συνεχίστε την παρτίδα από τις Ενέργειες του καταλόγου.");
            if (new SqlInvoiceTypeCorrectionStore(sql.Value).IsChild(id,true))
                throw new InvalidOperationException("Το παραστατικό ελέγχεται από την παρτίδα διόρθωσης τύπου myDATA. Συνεχίστε την παρτίδα από τις Ενέργειες του καταλόγου.");
            string destination = !string.IsNullOrWhiteSpace(reason) || !string.IsNullOrWhiteSpace(selectedPrinter)
                ? CurrentPrintDestination(id,selectedPrinter)
                : null;
            var source = new Source(print,false,selectedPrinter);
            bool recoveredDispatch = false;
            bool retryRecoveredSubmission = false;
            if (HasStaleDirectPumpDispatchCandidate(id))
            {
                using (var lease = sql.Value.EnterInvoice(id))
                {
                    var status = ReadStatus(id);
                    recoveredDispatch = RecoverStaleDirectPumpDispatchDate(id,lease,status);
                    retryRecoveredSubmission = recoveredDispatch &&
                        status.State("Submission") == "Rejected";
                }
            }
            return automaticSamtecAmountCorrection && !retryRecoveredSubmission
                ? workflow.Value.ProcessAutomaticSamtecAmountCorrection(id,source,print,reason,destination)
                : workflow.Value.Process(id,source,print,reason,recoveredDispatch,false,destination);
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
        internal static InvoiceWorkflowResult SubmitLegacySignedInvoiceToProvider(
            Guid id)
        {
            if (!Program.AdminConnected)
            {
                throw new InvalidOperationException(
                    "Απαιτείται σύνδεση διαχειριστή για αντικατάσταση legacy φορολογικών στοιχείων.");
            }
            bool owned;
            using (var lease = sql.Value.EnterInvoice(id))
            {
                var ownership = new SqlFiscalOwnershipStore(sql.Value);
                owned = ownership.Owns(lease);
            }
            if (owned)
            {
                throw new InvalidOperationException(
                    "Η κοινή έκδοση έχει ήδη ξεκινήσει. Ανανεώστε τη σύνοψη και συνεχίστε την αποθηκευμένη ροή.");
            }
            var source = new Source(false, true,
                replaceLegacyFiscalEvidence: true);
            return workflow.Value.Process(id, source, false);
        }
        internal static void PrintHistoricalCopy(Guid id, string selectedPrinter = null)
        {
            // Pre-shared-workflow invoices have no frozen snapshot or print job.
            // A copy must therefore use the stored host invoice, but must never
            // adopt it into the issuance workflow or repeat fiscal side effects.
            using (var lease = sql.Value.EnterInvoice(id))
            {
                if (new SqlFiscalOwnershipStore(sql.Value).Owns(lease))
                    throw new InvalidOperationException(
                        "Το παραστατικό έχει ήδη κοινό φορολογικό ιστορικό. Χρησιμοποιήστε την αποθηκευμένη ροή επανεκτύπωσης.");
                using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
                {
                    var invoice = db.Invoices.SingleOrDefault(value => value.InvoiceId == id);
                    if (invoice == null)
                        throw new InvalidOperationException("Το παραστατικό δεν βρέθηκε.");
                    if (invoice.Number <= 0 || invoice.IsPrinted != true)
                        throw new InvalidOperationException(
                            "Μόνο ήδη εκτυπωμένο ιστορικό παραστατικό με αποθηκευμένο αριθμό μπορεί να επανεκτυπωθεί.");
                    PrintAgent.PrintHistoricalInvoiceCopy(invoice,selectedPrinter);
                }
            }
        }
        private sealed class Source : IInvoiceWorkflowSource
        {
            private readonly bool printRequested, historicalPrinted,
                replaceLegacyFiscalEvidence;
            private readonly string selectedPrinter;
            private readonly int? paymentTypeOverride;
            internal Source(bool printRequested, bool historicalPrinted = false, string selectedPrinter = null,
                int? paymentTypeOverride = null,
                bool replaceLegacyFiscalEvidence = false)
            {
                this.printRequested = printRequested;
                this.historicalPrinted = historicalPrinted;
                this.selectedPrinter = string.IsNullOrWhiteSpace(selectedPrinter) ? null : selectedPrinter.Trim();
                this.paymentTypeOverride = paymentTypeOverride;
                this.replaceLegacyFiscalEvidence =
                    replaceLegacyFiscalEvidence;
            }
            public InvoiceWorkflowSnapshot Prepare(IFiscalDocumentLease lease)
            {
                if (!Enabled) throw new InvalidOperationException("First issuance through the shared workflow is disabled.");
                var id = lease.Document.DocumentId;
                var config = settings.Value;
                // Check route/history before touching the counter. Open a new context after
                // SQL numbering; an old OpenAccess identity map must never render number 0.
                bool needsNumber;
                MyDieselCouponCaseRecord pendingCoupon;
                using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
                {
                    var pending = Check(db,id,config);
                    pendingCoupon = ValidateMyDiesel(pending,config.Route(pending),config);
                    needsNumber = pending.Number == 0;
                }
                if (pendingCoupon != null)
                {
                    if (!pendingCoupon.IssuedLitres.HasValue || pendingCoupon.IssuedLitres.Value <= 0)
                        throw new InvalidOperationException(
                            "Το δεσμευμένο κουπόνι myDiesel δεν περιέχει έγκυρη ποσότητα λίτρων.");
                    PendingMyDieselSelectionStore.ReconcileExciseDiscount(id,
                        pendingCoupon.IssuedLitres.Value,pendingCoupon.ExciseDiscountPerLitre);
                }
                if (needsNumber) new SqlInvoiceWorkflowStore(sql.Value).AllocateNumbers(lease,config.FirstInvoiceDate);
                using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
                {
                    var invoice = Check(db, id, config);
                    // Normalize the host totals exactly once, before the first shared
                    // workflow snapshot freezes the print/provider/signing payloads.
                    // Existing snapshots are reused by retries and reprints, while a
                    // historical printed invoice must retain its originally issued data.
                    if (!historicalPrinted && !paymentTypeOverride.HasValue)
                    {
                        invoice.RecalculateInvoice();
                        db.SaveChanges();
                    }
                    var route = CancellationRoute(db, invoice, config.Route(invoice));
                    var coupon = ValidateMyDiesel(invoice,route,config);
                    if (selectedPrinter != null)
                        route.Printer.PrinterName = selectedPrinter;
                    var printer = printRequested ? SharedInvoicePrinting.ResolveProfile(route.Printer,config.DefaultPrinter) :
                        route.Printer ?? config.DefaultPrinter ?? new SharedPrinterProfile();
                    var content = PrintAgent.RenderSharedInvoiceContent(db, invoice);
                    var value = new InvoiceWorkflowSnapshot { InvoiceId = id, Provider = route.Provider, LocalOnly = route.LocalOnly,
                        Payment = invoice.PaymentType == 2 || invoice.PaymentType == 4 ? route.Payment : null,
                        PrintContent = Encoding.UTF8.GetBytes(content), PrintDestination = JsonConvert.SerializeObject(printer), PrintFormat = SharedInvoicePrinting.Format,
                        CompanyQrContent = Reports.BrandData.SetBrandData(invoice) };
                    if (replaceLegacyFiscalEvidence)
                    {
                        string authorizedBy = string.IsNullOrWhiteSpace(
                                Program.CurrentUserName)
                            ? Environment.UserName
                            : Program.CurrentUserName;
                        value.LegacyFiscalReplacement =
                            new LegacyFiscalEvidenceReplacement
                            {
                                PreviousInvoiceSignature =
                                    invoice.InvoiceSignature,
                                PreviousQrCode = invoice.QRCodeData,
                                AuthorizedBy = authorizedBy,
                                AuthorizedUtc = DateTime.UtcNow,
                                Reason = "Replace legacy local fiscal-device evidence with the configured invoice-provider evidence."
                            };
                    }
                    if (coupon != null)
                    {
                        value.Version = 2; value.MyDieselCouponCaseId = coupon.CouponCaseId;
                        value.MyDieselScope = new MyDieselScope(coupon.Environment,coupon.CompanyVat,coupon.ReservedBranchNum,
                            coupon.CredentialProfileId,coupon.ReservedStationAmdika);
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
                        // If a direct-pump invoice-delivery could not be issued on
                        // its transaction day, its first electronic dispatch uses
                        // the actual recovery time. The host invoice date and the
                        // balance-linked sale remain unchanged.
                        ApplyStaleDirectPumpDispatchDate(invoice,value,DateTime.Now);
                    }
                    if (route.Signing != null)
                    {
                        var device = config.SamtecDevice(route.Signing);
                        value.Signing = HostFiscalSigningMapper.Invoice(invoice, route.Signing, content, false,
                            device?.EftPosTID);
                    }
                    return value;
                }
            }
            private SharedInvoiceRoute CancellationRoute(Data.DatabaseModel db, Data.Invoice invoice,
                SharedInvoiceRoute configured)
            {
                if (!HostInvoiceMapper.IsCancellationDocument(invoice.InvoiceType))
                    return configured;
                var relations = invoice.ParentInvoiceRelations.Where(relation =>
                    relation.RelationType == (int)Common.Enumerators.InvoiceRelationTypeEnum.Cancel).ToArray();
                if (relations.Length != 1 || relations[0].ParentInvoice == null)
                    throw new InvalidOperationException(
                        "Το ειδικό ακυρωτικό πρέπει να συνδέεται με ακριβώς ένα αρχικό παραστατικό.");

                var original = relations[0].ParentInvoice;
                bool externallyIssued = HasExternalTransmissionEvidence(db, original);
                if (!externallyIssued)
                {
                    if (HasUnresolvedSigningAttempt(original.InvoiceId))
                        throw new InvalidOperationException(
                            "Η σήμανση του αρχικού παραστατικού είναι σε εκκρεμή ή άγνωστη κατάσταση. " +
                            "Ολοκληρώστε πρώτα την ανάκτηση της σήμανσης.");
                    // There is nothing external to reverse. Keep the cancellation
                    // local even when type 215 normally has a provider policy.
                    // A completed device signature on its own is intentionally a
                    // local case and does not require MARK/provider cancellation.
                    return new SharedInvoiceRoute {
                        LocalOnly = true,
                        Printer = configured.Printer
                    };
                }
                if (IsCancellationOrCreditType(db, relations[0].ParentInvoice.InvoiceType))
                    throw new InvalidOperationException(
                        "Το αρχικό πιστωτικό έχει ήδη σημανθεί ή διαβιβαστεί και δεν επιτρέπεται " +
                        "δεύτερο πιστωτικό 5.1 ή 11.4 πάνω του. Αν εκδόθηκε απευθείας μέσω myDATA " +
                        "ακυρώνεται το MARK του· αν εκδόθηκε μέσω παρόχου απαιτείται θετικό " +
                        "διορθωτικό παραστατικό.");
                if (configured.LocalOnly)
                    throw new InvalidOperationException(
                        "Το αρχικό παραστατικό έχει ήδη σημανθεί ή διαβιβαστεί. Το ακυρωτικό δεν μπορεί να παραμείνει μόνο τοπικό.");
                if (configured.Signing != null)
                    throw new InvalidOperationException(
                        "Το αρχικό παραστατικό έχει φορολογικό ιστορικό. Η ακύρωση μέσω φορολογικού μηχανισμού απαιτεί επιβεβαιωμένα στοιχεία του αρχικού μηχανισμού και δεν είναι διαθέσιμη από αυτή τη ροή.");
                if (configured.Provider == null)
                    throw new InvalidOperationException(
                        "Το αρχικό παραστατικό έχει ήδη διαβιβαστεί. Ορίστε διαβίβαση του ακυρωτικού σε πάροχο ή απευθείας myDATA.");
                // A provider cancellation is a new financial document. It must not
                // start a new POS payment; the mapper emits an on-credit method.
                return new SharedInvoiceRoute {
                    Provider = configured.Provider,
                    Payment = null,
                    Printer = configured.Printer,
                    LocalOnly = false
                };
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
                bool couponIsTest = coupon.Environment == FiscalEnvironment.Test;
                if (route.Provider.TestMode != couponIsTest)
                {
                    string transmission = route.Provider.ProviderKey == InvoiceProviderCatalog.MyDataKey
                        ? "η απευθείας διαβίβαση myDATA"
                        : "ο πάροχος τιμολόγησης";
                    throw new InvalidOperationException("Το περιβάλλον myDiesel πρέπει να συμφωνεί με το περιβάλλον που χρησιμοποιεί " + transmission + ".");
                }
                // UAT uses AADE-provided demo coupon/company data which need not match
                // the issuer VAT configured on the test invoice provider route.
                if (!couponIsTest && route.Provider.IssuerVat != coupon.CompanyVat)
                    throw new InvalidOperationException("Το ΑΦΜ εκδότη του παραστατικού διαφέρει από τη δεσμευμένη κλήση myDiesel.");
                decimal litres = invoice.InvoiceLines.Sum(line => Math.Abs(line.Volume));
                if (litres <= 0) throw new InvalidOperationException("Το παραστατικό myDiesel πρέπει να περιέχει θετική ποσότητα καυσίμου σε λίτρα.");
                var fuelCodes = invoice.InvoiceLines.Select(line => line.FuelType == null
                    ? (int?)null
                    : line.FuelType.EnumeratorValue).ToArray();
                if (fuelCodes.Any(code => !code.HasValue || !MyDieselFuelRules.IsEligible(code.Value)))
                    throw new InvalidOperationException("Το επιλεγμένο καύσιμο του παραστατικού δεν είναι επιλέξιμο για myDiesel.");
                if (fuelCodes.Select(code => code.Value).Distinct().Count() != 1)
                    throw new InvalidOperationException("Το παραστατικό πρέπει να περιέχει ένα μόνο επιλέξιμο καύσιμο myDiesel.");
                var profile = config.MyDiesel;
                if (profile == null || profile.CompanyVat != coupon.CompanyVat || profile.Environment != coupon.Environment)
                    throw new InvalidOperationException("Οι τρέχουσες ρυθμίσεις myDiesel δεν συμφωνούν με τη δεσμευμένη υπόθεση.");
                return coupon;
            }
            private Data.Invoice Check(Data.DatabaseModel db, Guid id, SharedInvoiceConfiguration config)
            {
                var invoice = db.Invoices.Single(i => i.InvoiceId == id);
                if (paymentTypeOverride.HasValue)
                    invoice.PaymentType = paymentTypeOverride.Value;
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
                SharedInvoiceRoute route;
                if (replaceLegacyFiscalEvidence)
                {
                    if (!historicalPrinted)
                    {
                        throw new InvalidOperationException(
                            "Η αντικατάσταση legacy σήμανσης απαιτεί ιστορικό εκτυπωμένο παραστατικό.");
                    }
                    route = config.LegacyProviderReplacementRoute(invoice,
                        hasLegacy);
                }
                else
                {
                    if (historicalPrinted &&
                        !config.CanSubmitPrintedInvoice(invoice,hasLegacy))
                    {
                        throw new InvalidOperationException("Το παραστατικό δεν είναι διαθέσιμο για διαβίβαση ήδη εκτυπωμένου. Ελέγξτε τη σύνοψη και τα προηγούμενα φορολογικά στοιχεία.");
                    }
                    route = config.PendingRoute(invoice,hasLegacy,
                        historicalPrinted);
                }
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
                    InvoiceProviderEvidenceProjection.Apply(invoice, response,
                        result.FiscalQrCodeUrl);
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
