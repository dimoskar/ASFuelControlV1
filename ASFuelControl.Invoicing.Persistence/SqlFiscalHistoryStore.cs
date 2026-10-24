using System;
using System.Collections.Generic;
using System.Linq;
using Exedron.Invoicing.Core;

namespace ASFuelControl.Invoicing.Persistence
{
    public enum FiscalJournalFamily { Signing, Submission, ProviderSession, Payment, Print, Cancellation, MyDiesel }
    public sealed class FiscalAttemptEvidence
    {
        private readonly byte[] payload;
        internal FiscalAttemptEvidence(SqlFiscalDatabase.JournalEntry entry)
        {
            AttemptId = entry.Id; Family = entry.Family; Operation = entry.Operation; Scope = entry.Scope;
            PayloadFormat = entry.Format; payload = (byte[])entry.Payload.Clone(); RequestSha256 = entry.Hash;
            CreatedUtc = entry.CreatedUtc; UpdatedUtc = entry.UpdatedUtc; Revision = entry.Revision;
            State = entry.State; SnapshotJson = entry.Snapshot; OriginalAttemptId = entry.OriginalId;
        }
        public Guid AttemptId { get; }
        public Guid? OriginalAttemptId { get; }
        public string Family { get; }
        public string Operation { get; }
        public string Scope { get; }
        public string PayloadFormat { get; }
        public byte[] RequestPayload { get { return (byte[])payload.Clone(); } }
        public string RequestSha256 { get; }
        public DateTime CreatedUtc { get; }
        public DateTime UpdatedUtc { get; }
        public long Revision { get; }
        public string State { get; }
        public string SnapshotJson { get; }
    }
    // Read model for reconciliation and later UI. Do not expose bodies without host authorization.
    public sealed class SqlFiscalHistoryStore
    {
        private readonly SqlFiscalDatabase database;
        public SqlFiscalHistoryStore(SqlFiscalDatabase database) { this.database = database ?? throw new ArgumentNullException(nameof(database)); }
        public IReadOnlyList<FiscalAttemptEvidence> Load(IFiscalDocumentLease lease, FiscalJournalFamily family)
        {
            Validate(family);
            return database.Load(lease, family.ToString()).Select(e => new FiscalAttemptEvidence(e)).ToArray();
        }
        public IReadOnlyList<FiscalAttemptEvidence> LoadHistory(IFiscalDocumentLease lease, FiscalJournalFamily family, Guid attemptId)
        {
            Validate(family);
            return database.Load(lease, family.ToString(), attemptId, true).Select(e => new FiscalAttemptEvidence(e)).ToArray();
        }
        private static void Validate(FiscalJournalFamily family)
        { if (!Enum.IsDefined(typeof(FiscalJournalFamily), family)) throw new ArgumentException("Unknown fiscal journal family."); }
    }
}
