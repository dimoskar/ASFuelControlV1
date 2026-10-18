using System;
using System.Linq;
using System.Text;
using Exedron.Invoicing.Core;

namespace ASFuelControl.Invoicing.Persistence
{
    // Requires the host to hold database.EnterInvoice over the complete payment workflow.
    // The older interface has no lease/revision parameters; these methods enforce the ambient SQL lease.
    public sealed class SqlProviderPaymentStore : IProviderPaymentWorkflowStore
    {
        private readonly SqlFiscalDatabase database;
        // Scope is host-supplied environment/issuer/endpoint identity, without credentials.
        private readonly string scope;
        public SqlProviderPaymentStore(SqlFiscalDatabase database, string historicalScope)
        {
            this.database = database ?? throw new ArgumentNullException(nameof(database));
            if (string.IsNullOrWhiteSpace(historicalScope)) throw new ArgumentException("Payment environment/issuer scope required.");
            scope = historicalScope;
        }
        public ProviderPaymentAttempt LoadLatest(Guid invoiceId)
        {
            using (database.EnterInvoice(invoiceId))
            {
                var entry = database.Load(database.InvoiceLease(invoiceId), "Payment").LastOrDefault(e => e.Operation == "Payment");
                if (entry == null) return null;
                var saved = SqlFiscalDatabase.Read<Snapshot>(entry.Snapshot);
                saved.Attempt.ErrorCode = saved.ErrorCode;
                saved.Attempt.ErrorMessage = saved.ErrorMessage;
                return saved.Attempt;
            }
        }
        public ProviderPaymentAttempt GetOrCreate(ProviderPaymentAttemptRequest request)
        {
            var lease = database.InvoiceLease(request.InvoiceId);
            if (request.Operation == ProviderPaymentOperation.Refund)
            {
                if (!request.OriginalInvoiceId.HasValue || !request.OriginalPaymentAttemptId.HasValue || request.OriginalInvoiceId == request.InvoiceId)
                    throw new ArgumentException("A refund requires the original invoice and accepted payment.");
                var parentLease = database.InvoiceLease(request.OriginalInvoiceId.Value);
                var parent = database.Load(parentLease, "Payment", request.OriginalPaymentAttemptId).SingleOrDefault();
                if (parent == null || parent.Operation != "Payment" || parent.State != "Invoiced" || parent.Scope != scope)
                    throw new InvalidOperationException("Refund source payment is unavailable or uses a different historical route.");
            }
            if (request.Amount <= 0 || request.AmountMinorUnits <= 0 || request.Amount * 100 != request.AmountMinorUnits ||
                request.CurrencyCode != 978 || request.PaymentType != 0 && request.PaymentType != 1 ||
                !Enum.IsDefined(typeof(ProviderPaymentOperation), request.Operation) || string.IsNullOrWhiteSpace(request.TerminalId) ||
                string.IsNullOrWhiteSpace(request.Gateway) || string.IsNullOrWhiteSpace(request.InvoiceProvider) || request.ProviderId <= 0)
                throw new ArgumentException("Invalid payment request.");
            var history = database.Load(lease, "Payment").ToArray();
            if (history.Any(e => e.Operation != "Payment"))
                throw new InvalidOperationException("An existing reversal blocks new payment authorization on this invoice.");
            var latest = history.LastOrDefault();
            var payload = Encoding.UTF8.GetBytes(SqlFiscalDatabase.Json(request));
            if (latest != null && latest.State != "Voided" && latest.State != "Declined" && latest.State != "Failed")
            {
                if (latest.Scope != scope || !latest.Payload.SequenceEqual(payload)) throw new InvalidOperationException("Payment settings/amount changed; reconcile the original payment first.");
                return SqlFiscalDatabase.Read<Snapshot>(latest.Snapshot).Attempt;
            }
            var id = Guid.NewGuid();
            var attempt = new ProviderPaymentAttempt { Id = id, InvoiceId = request.InvoiceId, OriginalPaymentAttemptId = latest?.Id ?? request.OriginalPaymentAttemptId,
                AttemptNumber = history.Length + 1, ProviderId = request.ProviderId, State = "Prepared", TransactionUid = id.ToString("N"),
                TerminalId = request.TerminalId, Amount = request.Amount, AmountMinorUnits = request.AmountMinorUnits,
                Operation = request.Operation, PaymentType = request.PaymentType, RequiresSignatureRenewal = latest != null };
            var entry = new SqlFiscalDatabase.JournalEntry { Id = id, Family = "Payment", Operation = "Payment", Scope = scope,
                Format = "provider-payment.v2.json", Payload = payload, CreatedUtc = DateTime.UtcNow, UpdatedUtc = DateTime.UtcNow,
                State = "Prepared", Revision = 0, OriginalId = latest?.Id ?? request.OriginalPaymentAttemptId, Snapshot = SqlFiscalDatabase.Json(new Snapshot { Attempt = attempt }) };
            database.Write(lease, entry, null, true, null);
            return attempt;
        }
        private static PaymentGatewayEvent[] ReadEvents(SqlFiscalDatabase.Lease lease, System.Data.SqlClient.SqlTransaction tx, string uid)
        {
            var events = new System.Collections.Generic.List<PaymentGatewayEvent>();
            using (var command = SqlFiscalDatabase.Command(lease.Connection, tx, SqlStatements.ReadPaymentEvents, "@uid", uid))
            using (var reader = command.ExecuteReader())
                while (reader.Read()) events.Add(SqlFiscalDatabase.Read<PaymentGatewayEvent>(reader.GetString(0)));
            // Exchange timestamps retain ordering even when SQL RecordedUtc has equal precision.
            return events.OrderBy(e => e.StartedUtc).ThenBy(e => e.CompletedUtc).ToArray();
        }
        public ProviderPaymentAttempt ReconcileSignatureRejection(ProviderPaymentAttemptRequest request,
            Func<PaymentGatewayEvent[], bool> rejected)
        {
            var lease = database.InvoiceLease(request.InvoiceId);
            database.Locked(lease, (held, tx) => {
                var entries = database.Load(held, tx, "Payment"); var latest = entries.LastOrDefault();
                if (latest?.State != "SignaturePending" || entries.Any(e => e.Operation != "Payment") ||
                    database.Load(held, tx, "Submission").Any()) return false;
                var saved = SqlFiscalDatabase.Read<Snapshot>(latest.Snapshot);
                RequireUnresolvedRequest(latest, saved.Attempt, request);
                var events = ReadEvents(held, tx, saved.Attempt.TransactionUid);
                if (events.Any(e => e.TransactionUid != saved.Attempt.TransactionUid || e.InputType != "SIGNATURE") || !rejected(events)) return false;
                saved.Attempt.State = "Failed"; saved.ErrorCode = "SignatureRequestRejected"; saved.ErrorMessage = new PaymentSignatureRejectedException().Message;
                long revision = latest.Revision;
                latest.Revision++; latest.State = "Failed"; latest.UpdatedUtc = DateTime.UtcNow; latest.Snapshot = SqlFiscalDatabase.Json(saved);
                database.Write(held, tx, latest, revision, false, old => old.State == "SignaturePending");
                return true;
            });
            return LoadLatest(request.InvoiceId);
        }
        public ProviderPaymentAttempt ReconcileRecordedOutcome(ProviderPaymentAttemptRequest request,
            Func<ProviderPaymentAttempt, PaymentGatewayEvent[], PaymentRecoveryEvidence> read)
        {
            var lease = database.InvoiceLease(request.InvoiceId);
            database.Locked(lease, (held, tx) => {
                var entries = database.Load(held, tx, "Payment"); var latest = entries.LastOrDefault();
                if (latest == null || entries.Any(e => e.Operation != "Payment") ||
                    !new[] { "Pending", "Unknown", "Failed", "Mismatch" }.Contains(latest.State)) return false;
                var saved = SqlFiscalDatabase.Read<Snapshot>(latest.Snapshot);
                RequireUnresolvedRequest(latest, saved.Attempt, request);
                var evidence = read(saved.Attempt, ReadEvents(held, tx, saved.Attempt.TransactionUid));
                if (evidence == null || evidence.Approval == null &&
                    (latest.State != "Mismatch" || string.IsNullOrWhiteSpace(evidence.IncompleteMessage))) return false;
                var oldState = latest.State;
                if (evidence.Approval != null) { ApplyApproval(saved, evidence.Approval); saved.Attempt.State = "Approved"; }
                else { saved.Attempt.State = "Unknown"; saved.ErrorCode = "IncompleteResponse";
                    saved.ErrorMessage = ProviderPaymentCoordinator.FailureMessage(GBridgeFailureKind.IncompleteResponse) + " " + evidence.IncompleteMessage; }
                long revision = latest.Revision;
                latest.Revision++; latest.State = saved.Attempt.State; latest.UpdatedUtc = DateTime.UtcNow; latest.Snapshot = SqlFiscalDatabase.Json(saved);
                database.Write(held, tx, latest, revision, true, old => old.State == oldState);
                return true;
            });
            return LoadLatest(request.InvoiceId);
        }
        public ProviderPaymentAttempt ReconcileRejectedAuthorization(ProviderPaymentAttemptRequest request,
            Func<ProviderPaymentAttempt, PaymentGatewayEvent[], GBridgeFailureKind?> classify)
        {
            var lease = database.InvoiceLease(request.InvoiceId);
            database.Locked(lease, (held, tx) => {
                var entries = database.Load(held, tx, "Payment");
                var latest = entries.LastOrDefault();
                if (latest == null || entries.Any(e => e.Operation != "Payment") ||
                    latest.State != "Unknown" && latest.State != "Pending") return false;
                var saved = SqlFiscalDatabase.Read<Snapshot>(latest.Snapshot);
                RequireUnresolvedRequest(latest, saved.Attempt, request);
                if (database.Load(held, tx, "Submission").Any()) return false;
                var kind = classify(saved.Attempt, ReadEvents(held, tx, saved.Attempt.TransactionUid));
                if (kind != GBridgeFailureKind.SignatureRejected && kind != GBridgeFailureKind.CredentialsRejected) return false;
                saved.Attempt.State = "Declined"; saved.ErrorCode = kind.ToString();
                saved.ErrorMessage = ProviderPaymentCoordinator.FailureMessage(kind.Value);
                long revision = latest.Revision;
                latest.Revision++; latest.State = "Declined"; latest.UpdatedUtc = DateTime.UtcNow; latest.Snapshot = SqlFiscalDatabase.Json(saved);
                database.Write(held, tx, latest, revision, false, old => old.State == "Unknown" || old.State == "Pending");
                return true;
            });
            return LoadLatest(request.InvoiceId);
        }
        public ProviderPaymentAttempt GetOperatorRetryCandidate(ProviderPaymentAttemptRequest request,
            Func<ProviderPaymentAttempt, PaymentGatewayEvent[], bool> qualifies)
        {
            return database.Locked(database.InvoiceLease(request.InvoiceId), (held, tx) =>
                OperatorRetryCandidate(request, held, tx, qualifies));
        }
        private ProviderPaymentAttempt OperatorRetryCandidate(ProviderPaymentAttemptRequest request, SqlFiscalDatabase.Lease held,
            System.Data.SqlClient.SqlTransaction tx, Func<ProviderPaymentAttempt, PaymentGatewayEvent[], bool> qualifies)
        {
            var entries = database.Load(held, tx, "Payment");
            var latest = entries.LastOrDefault();
            if (latest == null || request.Operation != ProviderPaymentOperation.Sale || entries.Any(e => e.Operation != "Payment") ||
                new[] { "Submission", "Signing", "Cancellation", "Print" }.Any(f => database.Load(held, tx, f).Any())) return null;
            if (latest.State != "Unknown" && latest.State != "Prepared" && latest.State != "SignatureReady") return null;
            var saved = SqlFiscalDatabase.Read<Snapshot>(latest.Snapshot);
            RequireUnresolvedRequest(latest, saved.Attempt, request);
            if (latest.State == "Unknown")
                return qualifies(saved.Attempt, ReadEvents(held, tx, saved.Attempt.TransactionUid)) ? saved.Attempt : null;
            // A crash before authorization leaves a replacement to resume only by an explicit click.
            // This also holds back unsent automatic replacements from older builds.
            var parentId = saved.Attempt.OperatorRetryOfAttemptId ?? saved.Attempt.AutomaticRetryOfAttemptId;
            var parent = entries.FirstOrDefault(e => e.Id == parentId && e.State == "Unknown");
            if (parent == null || latest.OriginalId != parentId ||
                ReadEvents(held, tx, saved.Attempt.TransactionUid).Any(e => e.TransactionUid != saved.Attempt.TransactionUid || e.InputType != "SIGNATURE")) return null;
            var previous = SqlFiscalDatabase.Read<Snapshot>(parent.Snapshot);
            RequireUnresolvedRequest(parent, previous.Attempt, request);
            var decision = previous.OperatorRetry ?? previous.AutomaticRetry;
            if (decision == null || decision.NewAttemptId != latest.Id || decision.NewTransactionUid != saved.Attempt.TransactionUid ||
                decision.PreviousTransactionUid != previous.Attempt.TransactionUid) return null;
            return qualifies(previous.Attempt, ReadEvents(held, tx, previous.Attempt.TransactionUid)) ? saved.Attempt : null;
        }
        public ProviderPaymentAttempt TryCreateOperatorRetry(ProviderPaymentAttemptRequest request, Guid previousAttemptId,
            Func<ProviderPaymentAttempt, PaymentGatewayEvent[], bool> qualifies,
            Func<PaymentGatewayEvent[], string> policy = null)
        {
            var lease = database.InvoiceLease(request.InvoiceId);
            return database.Locked(lease, (held, tx) => {
                var candidate = OperatorRetryCandidate(request, held, tx, qualifies);
                if (candidate == null || candidate.Id != previousAttemptId) return null;
                if (candidate.State == "Prepared" || candidate.State == "SignatureReady") return candidate;
                var entries = database.Load(held, tx, "Payment");
                var latest = entries.Last();
                var saved = SqlFiscalDatabase.Read<Snapshot>(latest.Snapshot);
                RequireUnresolvedRequest(latest, saved.Attempt, request);
                var events = ReadEvents(held, tx, saved.Attempt.TransactionUid);
                if (!qualifies(saved.Attempt, events)) return null;
                var id = Guid.NewGuid();
                var attempt = new ProviderPaymentAttempt { Id = id, InvoiceId = request.InvoiceId,
                    OriginalPaymentAttemptId = latest.Id, OperatorRetryOfAttemptId = latest.Id,
                    AttemptNumber = entries.Count + 1, ProviderId = request.ProviderId, State = "Prepared", TransactionUid = id.ToString("N"),
                    TerminalId = request.TerminalId, Amount = request.Amount, AmountMinorUnits = request.AmountMinorUnits,
                    Operation = request.Operation, PaymentType = request.PaymentType, RequiresSignatureRenewal = true };
                // Keep the predecessor Unknown. The decision, linked child and both UIDs commit together.
                saved.OperatorRetry = new RetryDecision { Policy = policy == null ? "GBridge.F4-F2-0E.v1" : policy(events), PreviousOutcome = "Unknown",
                    PreviousTransactionUid = saved.Attempt.TransactionUid, NewAttemptId = id, NewTransactionUid = attempt.TransactionUid,
                    EvidenceCount = events.Length, RecordedUtc = DateTime.UtcNow };
                long revision = latest.Revision;
                latest.Revision++; latest.UpdatedUtc = DateTime.UtcNow; latest.Snapshot = SqlFiscalDatabase.Json(saved);
                database.Write(held, tx, latest, revision, false, old => old.State == "Unknown");
                var next = new SqlFiscalDatabase.JournalEntry { Id = id, Family = "Payment", Operation = "Payment", Scope = scope,
                    Format = latest.Format, Payload = latest.Payload, CreatedUtc = DateTime.UtcNow, UpdatedUtc = DateTime.UtcNow,
                    Revision = 0, OriginalId = latest.Id, State = "Prepared", Snapshot = SqlFiscalDatabase.Json(new Snapshot { Attempt = attempt }) };
                database.Write(held, tx, next, null, true, null);
                return attempt;
            });
        }
        private void RequireUnresolvedRequest(SqlFiscalDatabase.JournalEntry entry, ProviderPaymentAttempt attempt, ProviderPaymentAttemptRequest request)
        {
            if (entry.Scope != scope || !entry.Payload.SequenceEqual(Encoding.UTF8.GetBytes(SqlFiscalDatabase.Json(request))) ||
                !string.IsNullOrEmpty(attempt.TransactionId) || attempt.ResponseAmountMinorUnits.HasValue || !string.IsNullOrEmpty(attempt.VoidTransactionUid))
                throw new InvalidOperationException("The unresolved payment or its original request has changed.");
        }
        public void SaveSignature(Guid id, ProviderPaymentSignature signature)
        {
            if (signature == null || string.IsNullOrWhiteSpace(signature.ProviderSignature) || string.IsNullOrWhiteSpace(signature.ProviderInput)) throw new ArgumentException("Provider signature required.");
            Update(id, new[] { "Prepared", "SignaturePending", "SignatureReady" }, "SignatureReady", s => {
                s.Attempt.ProviderUid = signature.ProviderUid; s.Attempt.ProviderInput = signature.ProviderInput; s.Attempt.ProviderSignature = signature.ProviderSignature;
            });
        }
        public void MarkSignaturePending(Guid id) { Update(id, new[] { "Prepared", "SignatureReady" }, "SignaturePending", s => { }); }
        // The legacy coordinator calls MarkPending before lookup too. Preserve Unknown/Pending
        // in that case; this call never grants permission for another authorization.
        public void MarkPending(Guid id) { Update(id, new[] { "SignatureReady", "Pending", "Unknown" }, "Pending", s => { }, true); }
        public void MarkApproved(Guid id, PaymentAuthorizationResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            Update(id, new[] { "Pending", "Unknown" }, "Approved", s => ApplyApproval(s, result));
        }
        private static void ApplyApproval(Snapshot s, PaymentAuthorizationResult result)
        {
                var a = s.Attempt;
                if (result.ResponseTransactionUid != a.TransactionUid || result.TerminalId != a.TerminalId ||
                    result.AmountMinorUnits != a.AmountMinorUnits || result.PaymentType != a.PaymentType ||
                    result.TransactionType != (a.Operation == ProviderPaymentOperation.Sale ? 0 : 1) || string.IsNullOrWhiteSpace(result.TransactionId))
                    throw new InvalidOperationException("Payment result does not match its original request.");
                a.TransactionId = result.TransactionId; a.ResponseTerminalId = result.TerminalId; a.Rrn = result.Rrn;
                a.AuthorizationCode = result.AuthorizationCode; a.ResponseAmountMinorUnits = result.AmountMinorUnits;
                a.ResponseTransactionType = result.TransactionType; a.ResponsePaymentType = result.PaymentType;
                s.ErrorCode = null; s.ErrorMessage = null;
                s.Authorization = result;
        }
        public void MarkFailure(Guid id, string state, string errorCode, string errorMessage)
        {
            if (!new[] { "Unknown", "Declined", "Mismatch", "Failed" }.Contains(state)) throw new ArgumentException("Invalid payment failure state.");
            // Failed means no authorization was dispatched; after Pending an ambiguous failure is Unknown.
            var allowed = state == "Failed" ? new[] { "Prepared", "SignaturePending", "SignatureReady", "Pending", "Unknown" } : new[] { "Pending", "Unknown" };
            Update(id, allowed, state, s => { s.ErrorCode = errorCode; s.ErrorMessage = errorMessage; });
        }
        public void MarkInvoiceSubmitted(Guid id) { Update(id, new[] { "Approved", "Invoiced" }, "Invoiced", s => { }); }
        public void MarkVoidPending(Guid id, string voidTransactionUid)
        {
            if (string.IsNullOrWhiteSpace(voidTransactionUid)) throw new ArgumentException("Void transaction identity required.");
            Update(id, new[] { "Approved", "VoidPending", "VoidFailed" }, "VoidPending", s => {
                if (s.Attempt.VoidTransactionUid != null && s.Attempt.VoidTransactionUid != voidTransactionUid) throw new InvalidOperationException("Recover the existing void identity.");
                s.Attempt.VoidTransactionUid = voidTransactionUid;
            });
        }
        public void MarkVoided(Guid id) { Update(id, new[] { "VoidPending", "VoidFailed" }, "Voided", s => { }); }
        public void MarkVoidFailed(Guid id, string errorMessage) { Update(id, new[] { "VoidPending", "VoidFailed" }, "VoidFailed", s => s.ErrorMessage = errorMessage); }
        private void Update(Guid id, string[] allowed, string state, Action<Snapshot> change, bool preserveUnknown = false)
        {
            // Discover the document, then require its already-held workflow lease. Never acquire an
            // independent short lock here: it would allow two callers to authorize between writes.
            Guid invoiceId;
            using (var connection = database.Open())
            using (var command = SqlFiscalDatabase.Command(connection, null,
                SqlStatements.FindPaymentDocument, "@id", id))
            {
                var value = command.ExecuteScalar();
                if (value == null) throw new InvalidOperationException("Payment attempt not found.");
                invoiceId = (Guid)value;
            }
            var lease = database.InvoiceLease(invoiceId);
            var entry = database.Load(lease, "Payment", id).Single();
            if (entry.Scope != scope || !allowed.Contains(entry.State)) throw new InvalidOperationException("Forbidden payment transition or changed scope.");
            if (preserveUnknown && entry.State == "Unknown" || state == "Failed" && (entry.State == "Pending" || entry.State == "Unknown")) state = "Unknown";
            var snapshot = SqlFiscalDatabase.Read<Snapshot>(entry.Snapshot);
            change(snapshot);
            snapshot.Attempt.State = state;
            long revision = entry.Revision;
            entry.Revision++; entry.State = state; entry.UpdatedUtc = DateTime.UtcNow; entry.Snapshot = SqlFiscalDatabase.Json(snapshot);
            database.Write(lease, entry, revision, state != "Voided" && state != "Declined" && state != "Failed", old => allowed.Contains(old.State));
        }
        private sealed class Snapshot
        {
            public ProviderPaymentAttempt Attempt;
            public PaymentAuthorizationResult Authorization;
            public string ErrorCode, ErrorMessage;
            public RetryDecision AutomaticRetry;
            public RetryDecision OperatorRetry;
        }
        private sealed class RetryDecision
        {
            public string Policy, PreviousOutcome, PreviousTransactionUid, NewTransactionUid;
            public Guid NewAttemptId;
            public int EvidenceCount;
            public DateTime RecordedUtc;
        }
    }
}
