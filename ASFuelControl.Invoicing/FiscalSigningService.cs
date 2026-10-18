using System;
using System.Linq;
using Exedron.Invoicing.Core;
using ASFuelControl.Invoicing.Persistence;

namespace ASFuelControl.Invoicing
{
    public interface IFiscalSigningSource
    {
        // Called under the durable lease, before Prepared; no device I/O or SaveChanges.
        FiscalSigningRequest Prepare(FiscalDocumentReference document, FiscalSigningScope scope);
    }
    public sealed class FiscalSigningService
    {
        private readonly FiscalSigningCoordinator coordinator;
        private readonly SqlFiscalHistoryStore history;
        public FiscalSigningService(SqlFiscalDatabase database, Func<FiscalSigningScope, IFiscalSigner> resolve)
        {
            coordinator = new FiscalSigningCoordinator(new SqlFiscalSigningStore(database), resolve);
            history = new SqlFiscalHistoryStore(database);
        }
        public FiscalSigningAttempt Sign(FiscalDocumentReference document, FiscalSigningScope scope, IFiscalSigningSource source,
            bool retryDefinitiveFailure = false)
        {
            return coordinator.Sign(document, lease => {
                foreach (var family in new[] { FiscalJournalFamily.Submission, FiscalJournalFamily.ProviderSession,
                    FiscalJournalFamily.Payment, FiscalJournalFamily.Cancellation, FiscalJournalFamily.Print })
                    if (history.Load(lease, family).Any()) throw new InvalidOperationException("Existing " + family + " history requires the complete fiscal workflow.");
                if (scope == null || source == null) throw new ArgumentException("A device scope and source are required for a new signing attempt.");
                var request = source.Prepare(document, scope);
                if (request == null || !FiscalSigningCoordinator.SameScope(request.Scope, scope))
                    throw new InvalidOperationException("Host document and selected signing scope differ.");
                return request;
            }, retryDefinitiveFailure);
        }
        public FiscalSigningAttempt Recover(FiscalDocumentReference document) => coordinator.Recover(document);
        // Explicit operator/workflow action; reuses original bytes and historical profile.
        public FiscalSigningAttempt ResumePrepared(FiscalDocumentReference document) => coordinator.ResumePrepared(document);
    }

    public sealed class DatabaseFiscalSigningSource : IFiscalSigningSource
    {
        private readonly Func<Data.DatabaseModel> open;
        private readonly Func<Data.DatabaseModel, FiscalDocumentReference, FiscalSigningScope> readScope;
        private readonly Func<Data.DatabaseModel, FiscalDocumentReference, string> render;
        private readonly int fillingCode;
        public DatabaseFiscalSigningSource(Func<Data.DatabaseModel> open,
            Func<Data.DatabaseModel, FiscalDocumentReference, FiscalSigningScope> readScope,
            Func<Data.DatabaseModel, FiscalDocumentReference, string> render, int fillingCode = 40)
        {
            this.open = open ?? throw new ArgumentNullException(nameof(open));
            this.readScope = readScope ?? throw new ArgumentNullException(nameof(readScope));
            this.render = render ?? throw new ArgumentNullException(nameof(render));
            this.fillingCode = fillingCode;
        }
        public FiscalSigningRequest Prepare(FiscalDocumentReference document, FiscalSigningScope scope)
        {
            using (var db = open())
            {
                if (!FiscalSigningCoordinator.SameScope(readScope(db, document), scope))
                    throw new InvalidOperationException("Saved host issuer/branch/environment/device settings differ from the selected signing scope.");
                switch (document.Kind)
                {
                    case FiscalDocumentKind.Invoice:
                        var invoice = db.Invoices.Single(i => i.InvoiceId == document.DocumentId);
                        return HostFiscalSigningMapper.Invoice(invoice, scope, render(db, document), db.MyDataInvoices.Any(i => i.InvoiceId == document.DocumentId));
                    case FiscalDocumentKind.Balance:
                        return HostFiscalSigningMapper.Balance(db.Balances.Single(b => b.BalanceId == document.DocumentId), scope, render(db, document));
                    case FiscalDocumentKind.TankFilling:
                        return HostFiscalSigningMapper.TankFilling(db.TankFillings.Single(f => f.TankFillingId == document.DocumentId), scope, render(db, document), fillingCode);
                    default: throw new NotSupportedException("No fiscal signing operation is implemented for this document kind.");
                }
            }
        }
    }
}
