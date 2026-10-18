using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Exedron.Invoicing.Core;

namespace ASFuelControl.Invoicing.Persistence
{
    public sealed class SqlIlydaPosStore : IIlydaPosStore
    {
        private readonly SqlFiscalDatabase database;
        private readonly ConditionalWeakTable<IlydaPosSession, Stamp> stamps = new ConditionalWeakTable<IlydaPosSession, Stamp>();
        public SqlIlydaPosStore(SqlFiscalDatabase database) { this.database = database ?? throw new ArgumentNullException(nameof(database)); }
        public IDisposable Acquire(Guid invoiceId) { return database.EnterInvoice(invoiceId); }
        public IlydaPosSession Load(Guid invoiceId)
        {
            using (database.EnterInvoice(invoiceId))
            {
                var entry = database.Load(database.InvoiceLease(invoiceId), "ProviderSession").LastOrDefault();
                if (entry == null) return null;
                var session = SqlFiscalDatabase.Read<IlydaPosSession>(entry.Snapshot);
                stamps.Add(session, new Stamp { Entry = entry });
                return session;
            }
        }
        public void Save(IlydaPosSession session)
        {
            var lease = database.InvoiceLease(session.InvoiceId);
            Stamp stamp;
            bool exists = stamps.TryGetValue(session, out stamp);
            if (!exists && session.State != "Requesting") throw new InvalidOperationException("Persist Requesting before obtaining a POS signature.");
            if (string.IsNullOrWhiteSpace(session.IssuerVat) || string.IsNullOrWhiteSpace(session.Endpoint) || string.IsNullOrWhiteSpace(session.RequestJson))
                throw new ArgumentException("The original POS scope and request are required.");
            var entry = new SqlFiscalDatabase.JournalEntry { Id = session.Id, Family = "ProviderSession", Operation = "SignPayment",
                Scope = SqlFiscalDatabase.Json(new { Provider = "Ilyda.Direct", session.TestMode, session.IssuerVat, session.Endpoint }),
                Format = "json; charset=utf-8", Payload = Encoding.UTF8.GetBytes(session.RequestJson),
                CreatedUtc = exists ? stamp.Entry.CreatedUtc : DateTime.UtcNow, UpdatedUtc = DateTime.UtcNow,
                Revision = exists ? stamp.Entry.Revision + 1 : 0, State = session.State, Snapshot = SqlFiscalDatabase.Json(session) };
            database.Write(lease, entry, exists ? (long?)stamp.Entry.Revision : null,
                session.State != "Cancelled" && session.State != "Rejected" && session.State != "Expired", old => Valid(SqlFiscalDatabase.Read<IlydaPosSession>(old.Snapshot), session));
            stamps.Remove(session); stamps.Add(session, new Stamp { Entry = entry });
        }
        private static bool Valid(IlydaPosSession old, IlydaPosSession next)
        {
            bool state = old.State == "Requesting" && new[] { "Requesting", "Ready", "Rejected", "Unknown" }.Contains(next.State) ||
                old.State == "Ready" && new[] { "Ready", "Expired", "CancelPending" }.Contains(next.State) ||
                old.State == "Unknown" && new[] { "Unknown", "Ready", "Rejected", "CancelPending" }.Contains(next.State) ||
                old.State == "CancelPending" && new[] { "CancelPending", "Cancelled", "Expired" }.Contains(next.State);
            if (!state) return false;
            if (old.State == "Ready" && old.ResponseJson != next.ResponseJson) return false;
            if (old.PaymentRequestJson != null && old.PaymentRequestJson != next.PaymentRequestJson) return false;
            if (old.PaymentState == "Accepted") return next.PaymentState == "Accepted" && old.PaymentResponseJson == next.PaymentResponseJson;
            if (old.PaymentState == "Sending" || old.PaymentState == "Unknown") return next.PaymentState == "Accepted" || next.PaymentState == "Unknown";
            return string.IsNullOrEmpty(next.PaymentState) || next.PaymentState == "Sending";
        }
        private sealed class Stamp { internal SqlFiscalDatabase.JournalEntry Entry; }
    }
}
