using System;
using System.Linq;
using System.Text;
using Exedron.Invoicing.Core;

namespace ASFuelControl.Invoicing.Persistence
{
    public sealed class PaymentCompensationRecord
    {
        public Guid Id { get; internal set; }
        public Guid OriginalPaymentAttemptId { get; internal set; }
        public string Operation { get; internal set; }
        public string Scope { get; internal set; }
        public string RequestJson { get; internal set; }
        public FiscalPaymentState State { get; internal set; }
        public long Revision { get; internal set; }
        public string Evidence { get; internal set; }
    }
    public sealed class SqlPaymentCompensationStore
    {
        private readonly SqlFiscalDatabase database;
        public SqlPaymentCompensationStore(SqlFiscalDatabase database) { this.database = database; }
        public PaymentCompensationRecord Load(IFiscalDocumentLease lease)
        {
            var e = database.Load(lease,"Payment").LastOrDefault(a => a.Operation == "Void" || a.Operation == "Refund");
            return e == null ? null : Restore(e);
        }
        public PaymentCompensationRecord Begin(IFiscalDocumentLease lease, Guid originalId, string operation, string requestJson)
        {
            if (operation != "Void" && operation != "Refund") throw new ArgumentException("Unsupported payment operation.");
            if (Load(lease) != null) throw new InvalidOperationException("A payment reversal already exists; reconcile that operation.");
            var original = database.Load(lease,"Payment",originalId).SingleOrDefault();
            if (original == null || original.Operation != "Payment" || (original.State != "Approved" && original.State != "Invoiced"))
                throw new InvalidOperationException("An accepted original payment is required.");
            var now = DateTime.UtcNow;
            var entry = new SqlFiscalDatabase.JournalEntry { Id = Guid.NewGuid(), OriginalId = original.Id, Family = "Payment", Operation = operation,
                Scope = original.Scope, Format = "payment-compensation.v1.json", Payload = Encoding.UTF8.GetBytes(requestJson),
                CreatedUtc = now, UpdatedUtc = now, State = "Prepared", Revision = 0, Snapshot = "{}" };
            database.Write(lease,entry,null,true,null); return Restore(entry);
        }
        public PaymentCompensationRecord Save(IFiscalDocumentLease lease, PaymentCompensationRecord record, FiscalPaymentState state, string evidence)
        {
            if (string.IsNullOrWhiteSpace(evidence)) throw new ArgumentException("Outcome evidence is required.");
            var entry = database.Load(lease,"Payment",record.Id).Single();
            if (entry.Revision != record.Revision) throw new InvalidOperationException("Stale payment operation.");
            entry.Revision++; entry.UpdatedUtc = DateTime.UtcNow; entry.State = state.ToString(); entry.Snapshot = SqlFiscalDatabase.Json(new Outcome { Evidence = evidence });
            database.Write(lease,entry,record.Revision,true,old => FiscalStateTransitions.CanTransition((FiscalPaymentState)Enum.Parse(typeof(FiscalPaymentState),old.State),state));
            return Restore(entry);
        }
        private static PaymentCompensationRecord Restore(SqlFiscalDatabase.JournalEntry entry) => new PaymentCompensationRecord {
            Id = entry.Id, OriginalPaymentAttemptId = entry.OriginalId.Value, Operation = entry.Operation, Scope = entry.Scope,
            RequestJson = Encoding.UTF8.GetString(entry.Payload), State = (FiscalPaymentState)Enum.Parse(typeof(FiscalPaymentState),entry.State),
            Revision = entry.Revision, Evidence = SqlFiscalDatabase.Read<Outcome>(entry.Snapshot).Evidence };
        private sealed class Outcome { public string Evidence; }
    }
}
