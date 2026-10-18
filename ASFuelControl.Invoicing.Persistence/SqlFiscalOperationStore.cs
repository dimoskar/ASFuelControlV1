using System;
using System.Collections.Generic;
using System.Linq;
using Exedron.Invoicing.Core;

namespace ASFuelControl.Invoicing.Persistence
{
    // Host jobs for cancellation and physical printing. No external I/O is performed here.
    public sealed class FiscalOperationRecord
    {
        public Guid AttemptId { get; internal set; }
        public Guid? OriginalAttemptId { get; internal set; }
        public string Kind { get; internal set; }
        public bool OfflineReceipt { get; internal set; }
        public string Scope { get; internal set; }
        public string PayloadFormat { get; internal set; }
        private byte[] payload;
        public byte[] Payload { get { return (byte[])payload.Clone(); } internal set { payload = (byte[])value.Clone(); } }
        public long Revision { get; internal set; }
        public DateTime CreatedUtc { get; internal set; }
        public DateTime UpdatedUtc { get; internal set; }
        public string State { get; internal set; }
        public string Evidence { get; internal set; }
        public string Reason { get; internal set; }
    }
    public sealed class SqlFiscalOperationStore
    {
        private readonly SqlFiscalDatabase database;
        public SqlFiscalOperationStore(SqlFiscalDatabase database) { this.database = database ?? throw new ArgumentNullException(nameof(database)); }
        public FiscalOperationRecord BeginCancellation(IFiscalDocumentLease lease, Guid acceptedSubmissionId, string payloadFormat, byte[] payload)
        {
            var source = database.Load(lease, "Submission", acceptedSubmissionId).SingleOrDefault();
            if (source == null || source.State != "Accepted") throw new InvalidOperationException("Cancellation requires an accepted submission on this document.");
            return Begin(lease, "Cancellation", source, source.Scope, payloadFormat, payload, null);
        }
        public FiscalOperationRecord BeginPrint(IFiscalDocumentLease lease, Guid? acceptedAttemptId, string printerSnapshot,
            string payloadFormat, byte[] renderedDocument, string reprintReason = null, bool offlineReceipt = false)
        {
            SqlFiscalDatabase.JournalEntry source = null;
            if (offlineReceipt && !acceptedAttemptId.HasValue) throw new InvalidOperationException("TF2 printing requires a provider submission reference.");
            if (acceptedAttemptId.HasValue)
            {
                source = database.Load(lease, "Submission", acceptedAttemptId).Concat(database.Load(lease, "Signing", acceptedAttemptId)).SingleOrDefault();
                if (offlineReceipt)
                {
                    if (source == null || !source.Scope.StartsWith("Ilyda.Direct|",StringComparison.Ordinal) ||
                        !new[] { "Queued", "QueueFailed", "Accepted" }.Contains(source.State)) throw new InvalidOperationException("An offline receipt requires retained Ilyda TF2 evidence.");
                    // The original queue response is immutable revision evidence. A
                    // later accepted state may still reprint that original receipt.
                    bool queuedEvidence = database.Load(lease,"Submission",source.Id,true).Any(e => {
                        if (e.State != "Queued") return false;
                        var value = Newtonsoft.Json.Linq.JObject.Parse(e.Snapshot);
                        var body = Newtonsoft.Json.Linq.JObject.Parse((string)value["ResponseJson"]);
                        var marking = body["invoiceMarking"];
                        return !string.IsNullOrWhiteSpace((string)marking?["invoiceIdentifier"]) &&
                            Uri.IsWellFormedUriString((string)marking?["qrCode"], UriKind.Absolute) &&
                            (body["errors"] as Newtonsoft.Json.Linq.JArray)?.Any(error => (string)error["code"] == "MQ001" || (string)error["code"] == "MQ002") == true;
                    });
                    if (!queuedEvidence) throw new InvalidOperationException("No retained TF2 response with UID and QR.");
                }
                else if (source == null || source.State != "Accepted" && source.State != "Signed") throw new InvalidOperationException("Printing requires accepted fiscal evidence on this document.");
            }
            else
            {
                var bytes = new SqlInvoiceWorkflowStore(database).Load(lease);
                var snapshot = bytes == null ? null : Newtonsoft.Json.Linq.JObject.Parse(System.Text.Encoding.UTF8.GetString(bytes));
                if (lease.Document.Kind != FiscalDocumentKind.Invoice || snapshot == null || (bool?)snapshot["LocalOnly"] != true ||
                    (Guid?)snapshot["InvoiceId"] != lease.Document.DocumentId || (int?)snapshot["Version"] != 1 ||
                    new[] { "Provider", "Signing", "Payment" }.Any(k => snapshot[k] != null && snapshot[k].Type != Newtonsoft.Json.Linq.JTokenType.Null) ||
                    Enum.GetValues(typeof(FiscalJournalFamily)).Cast<FiscalJournalFamily>().Where(f => f != FiscalJournalFamily.Print).Any(f => database.Load(lease,f.ToString()).Any()))
                    throw new InvalidOperationException("Printing without fiscal evidence requires a saved, explicit local-only invoice policy.");
            }
            if (database.Load(lease, "Print").Any(p => p.State == "HandedOff" || p.State == "Confirmed") && string.IsNullOrWhiteSpace(reprintReason))
                throw new InvalidOperationException("An explicit reprint reason is required after a previous printer handoff.");
            return Begin(lease, "Print", source, printerSnapshot, payloadFormat, renderedDocument, reprintReason, offlineReceipt);
        }
        private FiscalOperationRecord Begin(IFiscalDocumentLease lease, string family, SqlFiscalDatabase.JournalEntry source,
            string scope, string format, byte[] payload, string reason, bool offlineReceipt = false)
        {
            var entry = new SqlFiscalDatabase.JournalEntry { Id = Guid.NewGuid(), Family = family, Operation = offlineReceipt ? "PrintTF2" : family,
                Scope = scope, Format = format, Payload = payload, CreatedUtc = DateTime.UtcNow, UpdatedUtc = DateTime.UtcNow,
                Revision = 0, State = "Prepared", OriginalId = source?.Id, Snapshot = SqlFiscalDatabase.Json(new Evidence { Reason = reason }) };
            database.Write(lease, entry, null, true, null);
            return Restore(entry);
        }
        public void SaveCancellation(IFiscalDocumentLease lease, FiscalOperationRecord attempt, FiscalTransmissionState next, string evidence)
        {
            if (attempt.Kind != "Cancellation") throw new ArgumentException("Not a cancellation.");
            Save(lease, attempt, next.ToString(), evidence, old => FiscalStateTransitions.CanTransition(
                (FiscalTransmissionState)Enum.Parse(typeof(FiscalTransmissionState), old), next),
                next != FiscalTransmissionState.Rejected && next != FiscalTransmissionState.FailedBeforeDispatch);
        }
        public void SavePrint(IFiscalDocumentLease lease, FiscalOperationRecord attempt, FiscalPrintState next, string evidence)
        {
            if (attempt.Kind != "Print") throw new ArgumentException("Not a print job.");
            Save(lease, attempt, next.ToString(), evidence, old => FiscalStateTransitions.CanTransition(
                (FiscalPrintState)Enum.Parse(typeof(FiscalPrintState), old), next),
                next == FiscalPrintState.Dispatching || next == FiscalPrintState.Unknown || next == FiscalPrintState.Prepared);
        }
        private void Save(IFiscalDocumentLease lease, FiscalOperationRecord record, string state, string evidence, Func<string, bool> allowed, bool blocking)
        {
            if (string.IsNullOrWhiteSpace(evidence)) throw new ArgumentException("Operation evidence/reason required.");
            var entry = database.Load(lease, record.Kind, record.AttemptId).Single();
            if (entry.Revision != record.Revision) throw new InvalidOperationException("Stale operation revision.");
            long expected = entry.Revision;
            entry.Revision++; entry.UpdatedUtc = DateTime.UtcNow; entry.State = state;
            entry.Snapshot = SqlFiscalDatabase.Json(new Evidence { Response = evidence, Reason = record.Reason });
            database.Write(lease, entry, expected, blocking, old => allowed(old.State));
            record.Revision = entry.Revision; record.State = state; record.Evidence = evidence; record.UpdatedUtc = entry.UpdatedUtc;
        }
        public IReadOnlyList<FiscalOperationRecord> Load(IFiscalDocumentLease lease, bool printing)
        { return database.Load(lease, printing ? "Print" : "Cancellation").Select(Restore).ToArray(); }
        public IReadOnlyList<FiscalOperationRecord> LoadHistory(IFiscalDocumentLease lease, bool printing, Guid attemptId)
        { return database.Load(lease, printing ? "Print" : "Cancellation", attemptId, true).Select(Restore).ToArray(); }
        private static FiscalOperationRecord Restore(SqlFiscalDatabase.JournalEntry entry)
        {
            var evidence = SqlFiscalDatabase.Read<Evidence>(entry.Snapshot);
            return new FiscalOperationRecord { AttemptId = entry.Id, OriginalAttemptId = entry.OriginalId, Kind = entry.Family,
                OfflineReceipt = entry.Operation == "PrintTF2",
                Scope = entry.Scope, PayloadFormat = entry.Format, Payload = entry.Payload, Revision = entry.Revision,
                CreatedUtc = entry.CreatedUtc, UpdatedUtc = entry.UpdatedUtc, State = entry.State, Evidence = evidence.Response, Reason = evidence.Reason };
        }
        private sealed class Evidence { public string Response, Reason; }
    }
}
