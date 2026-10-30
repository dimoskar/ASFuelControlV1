using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Exedron.Invoicing.Core;

namespace ASFuelControl.Invoicing.Persistence
{
    // The weak revision stamp ties Save to the exact object read/begun by this store.
    // Copying a stale DTO cannot overwrite a newer result, even under a later lease.
    internal sealed class SubmissionJournal<T> where T : class, IRoutedSubmission
    {
        private readonly SqlFiscalDatabase database;
        private readonly string provider;
        private readonly string routingContext;
        private readonly ConditionalWeakTable<T, Stamp> stamps = new ConditionalWeakTable<T, Stamp>();
        internal SubmissionJournal(SqlFiscalDatabase database, string provider, string routingContext = null)
        { this.database = database; this.provider = provider; this.routingContext = routingContext; }
        internal T Load(Guid invoiceId, Func<T, bool> scopeMatches)
        {
            using (database.EnterInvoice(invoiceId))
            {
                var entries = database.Load(database.InvoiceLease(invoiceId), "Submission");
                var entry = entries.LastOrDefault();
                if (entry == null) return null;
                var prefix = provider + "|";
                if (!entry.Scope.StartsWith(prefix, StringComparison.Ordinal))
                {
                    if (entry.State == "Rejected" || entry.State == "RetryAuthorized") return null;
                    throw new InvalidOperationException("Resolve the invoice through its historical provider before changing route.");
                }
                var value = SqlFiscalDatabase.Read<T>(entry.Snapshot);
                if (!scopeMatches(value))
                {
                    if (entry.State == "Rejected" || entry.State == "RetryAuthorized") return null;
                    throw new InvalidOperationException("The original invoice environment/issuer differs. Reconcile its saved scope first.");
                }
                if (entry.State == "RetryAuthorized") return null;
                stamps.Add(value, new Stamp { Entry = entry });
                return value;
            }
        }
        internal void Save(T value, Guid invoiceId, Guid attemptId, string scope, string payload, string state,
            DateTime createdUtc, bool beginOnly = false, bool completeOnly = false)
        {
            var lease = database.InvoiceLease(invoiceId);
            if (string.IsNullOrWhiteSpace(payload) || string.IsNullOrWhiteSpace(scope)) throw new ArgumentException("Submission scope and exact payload are required.");
            Stamp stamp;
            bool exists = stamps.TryGetValue(value, out stamp);
            if (!exists && routingContext != null)
            {
                if (value.RoutingContext != null && value.RoutingContext != routingContext)
                    throw new InvalidOperationException("The submission routing context does not match the host route.");
                value.RoutingContext = routingContext;
            }
            if (exists && value.RoutingContext != SqlFiscalDatabase.Read<T>(stamp.Entry.Snapshot).RoutingContext)
                throw new InvalidOperationException("The original submission routing context is immutable.");
            if (exists && provider != "Ilyda.Direct" && createdUtc != stamp.Entry.CreatedUtc)
                throw new InvalidOperationException("The original submission timestamp is immutable.");
            if ((beginOnly && exists) || (completeOnly && !exists)) throw new InvalidOperationException("Use the original attempt object for Begin/Complete.");
            if (!exists && state != "Sending") throw new InvalidOperationException("Persist Sending before submission.");
            if (!new[] { "Sending", "Accepted", "Rejected", "Unknown", "Queued", "QueueFailed" }.Contains(state)) throw new ArgumentException("Unknown submission state.");
            var snapshot = SqlFiscalDatabase.Json(value);
            if (state == "Accepted" || state == "Rejected" || state == "Queued" || state == "QueueFailed")
            {
                var evidence = Newtonsoft.Json.Linq.JObject.Parse(snapshot);
                bool hasBody = !string.IsNullOrWhiteSpace((string)evidence["ResponseXml"]) || !string.IsNullOrWhiteSpace((string)evidence["ResponseJson"]);
                bool httpRejected = state == "Rejected" && provider == "iTimologia" &&
                    new int?[] { 401,422,452 }.Contains((int?)evidence["StatusCode"]);
                if (!hasBody && !httpRejected) throw new InvalidOperationException("An outcome requires retained response evidence.");
            }
            Guid? originalAttemptId = null;
            if (!exists)
            {
                var previous = database.Load(lease, "Submission").LastOrDefault();
                if (previous?.State == "RetryAuthorized") originalAttemptId = previous.Id;
            }
            var entry = new SqlFiscalDatabase.JournalEntry { Id = attemptId, Family = "Submission", Operation = "Issue",
                Scope = provider + "|" + scope, Format = provider == "myDATA" ? "xml; charset=utf-8" : "json; charset=utf-8",
                Payload = Encoding.UTF8.GetBytes(payload), CreatedUtc = exists ? stamp.Entry.CreatedUtc : createdUtc,
                UpdatedUtc = DateTime.UtcNow, Revision = exists ? stamp.Entry.Revision + 1 : 0, State = state, Snapshot = snapshot,
                OriginalId = exists ? stamp.Entry.OriginalId : originalAttemptId };
            database.Write(lease, entry, exists ? (long?)stamp.Entry.Revision : null, state != "Rejected", old =>
                (old.State == "Sending" || old.State == "Unknown") && state != "Sending" ||
                (old.State == "Queued" || old.State == "QueueFailed") && new[] { "Queued", "QueueFailed", "Accepted" }.Contains(state) ||
                old.State == "Accepted" && state == "Accepted" && AcceptedEvidenceUnchanged(old.Snapshot, entry.Snapshot));
            stamps.Remove(value);
            stamps.Add(value, new Stamp { Entry = entry });
        }
        private bool AcceptedEvidenceUnchanged(string previous, string updated)
        {
            // Ilyda allows a later PDF-upload flag; no accepted fiscal evidence may change.
            var old = Newtonsoft.Json.Linq.JObject.Parse(previous);
            var next = Newtonsoft.Json.Linq.JObject.Parse(updated);
            if (provider == "iTimologia" && string.IsNullOrWhiteSpace((string)old["PrintMetadataResponseJson"]) &&
                !string.IsNullOrWhiteSpace((string)next["PrintMetadataResponseJson"]))
            {
                var original = Newtonsoft.Json.Linq.JObject.Parse((string)old["ResponseJson"])["data"];
                var metadata = Newtonsoft.Json.Linq.JObject.Parse((string)next["PrintMetadataResponseJson"])["data"];
                if (original == null || metadata == null ||
                    (string)original["uid"] != (string)metadata["uid"] || (string)original["mark"] != (string)metadata["mark"] ||
                    ((string)metadata["ext_system_id"] ?? "").Replace("-", "") != ((string)old["InvoiceId"]).Replace("-", "")) return false;
                foreach (string field in new[] { "authentication_code", "signature_code", "web_view" })
                    if (!string.IsNullOrWhiteSpace((string)original[field]) && (string)original[field] != (string)metadata[field]) return false;
                old.Remove("PrintMetadataResponseJson"); next.Remove("PrintMetadataResponseJson");
            }
            if (provider == "Ilyda.Direct" && (bool?)old["PdfUploaded"] != true && (bool?)next["PdfUploaded"] == true)
            { old.Remove("PdfUploaded"); next.Remove("PdfUploaded"); }
            return Newtonsoft.Json.Linq.JToken.DeepEquals(old, next);
        }
        private sealed class Stamp { internal SqlFiscalDatabase.JournalEntry Entry; }
    }
    public sealed class SqlProviderSubmissionResendStore
    {
        private readonly SqlFiscalDatabase database;
        public SqlProviderSubmissionResendStore(SqlFiscalDatabase database)
        { this.database = database ?? throw new ArgumentNullException(nameof(database)); }

        public void Authorize(IFiscalDocumentLease lease, Guid attemptId, string providerKey,
            string authorizedBy, string reason)
        {
            if (lease == null) throw new ArgumentNullException(nameof(lease));
            if (string.IsNullOrWhiteSpace(authorizedBy)) throw new ArgumentException("The operator identity is required.", nameof(authorizedBy));
            if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("The resend reason is required.", nameof(reason));
            var current = database.Load(lease, "Submission").LastOrDefault();
            if (current == null || current.Id != attemptId ||
                !current.Scope.StartsWith(providerKey + "|", StringComparison.Ordinal))
                throw new InvalidOperationException("The provider submission changed. Recover its current state before resending.");
            if (current.State == "RetryAuthorized") return;
            if (current.State != "Unknown" && current.State != "Sending")
                throw new InvalidOperationException("The current provider state does not permit an operator-authorized resend.");

            var snapshot = Newtonsoft.Json.Linq.JObject.Parse(current.Snapshot);
            snapshot["OperatorResendAuthorization"] = Newtonsoft.Json.Linq.JObject.FromObject(new {
                Version = 1, AuthorizedBy = authorizedBy.Trim(), Reason = reason.Trim(),
                AuthorizedUtc = DateTime.UtcNow, PreviousState = current.State, PreviousAttemptId = current.Id
            });
            database.Write(lease, new SqlFiscalDatabase.JournalEntry {
                Id = current.Id, Family = current.Family, Operation = current.Operation, Scope = current.Scope,
                Format = current.Format, Payload = current.Payload, CreatedUtc = current.CreatedUtc,
                UpdatedUtc = DateTime.UtcNow, Revision = current.Revision + 1, State = "RetryAuthorized",
                Snapshot = snapshot.ToString(Newtonsoft.Json.Formatting.None), OriginalId = current.OriginalId
            }, current.Revision, false, old => old.State == "Unknown" || old.State == "Sending");
        }
    }
    public sealed class SqlMyDataSubmissionStore : IMyDataSubmissionStore
    {
        private readonly SqlFiscalDatabase database;
        private readonly SubmissionJournal<MyDataSubmission> journal;
        public SqlMyDataSubmissionStore(SqlFiscalDatabase database, string routingContext = null) { this.database = database; journal = new SubmissionJournal<MyDataSubmission>(database, "myDATA", routingContext); }
        public IDisposable Acquire(string scope, Guid invoiceId) { return database.EnterInvoice(invoiceId); }
        public MyDataSubmission Load(string scope, Guid invoiceId) { return journal.Load(invoiceId, value => value.Scope == scope); }
        public void Save(MyDataSubmission attempt)
        { journal.Save(attempt, attempt.InvoiceId, attempt.AttemptId, attempt.Scope, attempt.RequestXml, attempt.State, attempt.StartedUtc.UtcDateTime); }
    }
    public sealed class SqlIlydaSubmissionStore : IIlydaSubmissionStore
    {
        private readonly SqlFiscalDatabase database;
        private readonly SubmissionJournal<IlydaSubmission> journal;
        public SqlIlydaSubmissionStore(SqlFiscalDatabase database, string routingContext = null) { this.database = database; journal = new SubmissionJournal<IlydaSubmission>(database, "Ilyda.Direct", routingContext); }
        public IDisposable Acquire(Guid invoiceId) { return database.EnterInvoice(invoiceId); }
        public IlydaSubmission Load(Guid invoiceId) { return journal.Load(invoiceId, value => true); }
        public void Save(IlydaSubmission attempt)
        { journal.Save(attempt, attempt.InvoiceId, attempt.AttemptId, SqlFiscalDatabase.Json(new { attempt.TestMode, attempt.IssuerVat }), attempt.RequestJson, attempt.State, DateTime.UtcNow); }
    }
    public sealed class SqlITimologiaSubmissionStore : IITimologiaSubmissionStore
    {
        private readonly SqlFiscalDatabase database;
        private readonly SubmissionJournal<ITimologiaSubmissionAttempt> journal;
        public SqlITimologiaSubmissionStore(SqlFiscalDatabase database, string routingContext = null) { this.database = database; journal = new SubmissionJournal<ITimologiaSubmissionAttempt>(database, "iTimologia", routingContext); }
        public IDisposable Acquire(Guid invoiceId) { return database.EnterInvoice(invoiceId); }
        public ITimologiaSubmissionAttempt LoadLatest(Guid invoiceId, bool testMode, string issuerVat)
        { return journal.Load(invoiceId, a => a.TestMode == testMode && a.IssuerVat == issuerVat); }
        public void Begin(ITimologiaSubmissionAttempt attempt) { Save(attempt, true); }
        public void Complete(ITimologiaSubmissionAttempt attempt) { Save(attempt, false); }
        private void Save(ITimologiaSubmissionAttempt attempt, bool begin)
        { journal.Save(attempt, attempt.InvoiceId, attempt.AttemptId, SqlFiscalDatabase.Json(new { attempt.TestMode, attempt.IssuerVat, attempt.Endpoint }),
            attempt.RequestJson, attempt.State, attempt.CreatedUtc, beginOnly: begin, completeOnly: !begin); }
    }
}
