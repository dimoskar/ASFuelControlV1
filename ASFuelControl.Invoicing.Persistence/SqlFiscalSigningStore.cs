using System;
using System.Collections.Generic;
using System.Linq;
using Exedron.Invoicing.Core;

namespace ASFuelControl.Invoicing.Persistence
{
    public sealed class SqlFiscalSigningStore : IFiscalSigningStore
    {
        private readonly SqlFiscalDatabase database;
        public SqlFiscalSigningStore(SqlFiscalDatabase database) { this.database = database ?? throw new ArgumentNullException(nameof(database)); }
        public IFiscalDocumentLease Acquire(FiscalDocumentReference document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            return database.EnterDocument(document);
        }
        public IReadOnlyList<FiscalSigningAttempt> Load(IFiscalDocumentLease lease)
        { return database.Load(lease, "Signing").Select(e => Restore(lease.Document, e)).ToArray(); }
        public IReadOnlyList<FiscalSigningAttempt> LoadHistory(IFiscalDocumentLease lease, Guid attemptId)
        { return database.Load(lease, "Signing", attemptId, true).Select(e => Restore(lease.Document, e)).ToArray(); }
        public FiscalSigningAttempt Begin(IFiscalDocumentLease lease, FiscalSigningRequest request, DateTime createdUtc)
        {
            ValidateDocument(lease, request);
            var attempt = new FiscalSigningAttempt(request, FiscalSigningState.Prepared, 0, createdUtc, createdUtc);
            database.Write(lease, Entry(attempt), null, true, null);
            return attempt;
        }
        public void Save(IFiscalDocumentLease lease, long expectedRevision, FiscalSigningAttempt updated)
        {
            if (updated == null) throw new ArgumentNullException(nameof(updated));
            ValidateDocument(lease, updated.Request);
            database.Write(lease, Entry(updated), expectedRevision,
                updated.State != FiscalSigningState.Rejected && updated.State != FiscalSigningState.FailedBeforeDispatch,
                old => FiscalStateTransitions.CanTransition((FiscalSigningState)Enum.Parse(typeof(FiscalSigningState), old.State), updated.State));
        }
        private static void ValidateDocument(IFiscalDocumentLease lease, FiscalSigningRequest request)
        {
            if (lease == null || request == null || lease.Document.Kind != request.Document.Kind || lease.Document.DocumentId != request.Document.DocumentId)
                throw new InvalidOperationException("The request does not belong to the leased document.");
        }
        private static SqlFiscalDatabase.JournalEntry Entry(FiscalSigningAttempt attempt)
        {
            return new SqlFiscalDatabase.JournalEntry { Id = attempt.Request.AttemptId, Family = "Signing", Operation = "Sign",
                Scope = SqlFiscalDatabase.Json(attempt.Request.Scope), Format = attempt.Request.PayloadFormat,
                Payload = attempt.Request.Payload, CreatedUtc = attempt.CreatedUtc, UpdatedUtc = attempt.UpdatedUtc,
                Revision = attempt.Revision, State = attempt.State.ToString(),
                Snapshot = SqlFiscalDatabase.Json(new Outcome { Signature = attempt.Result?.Signature, QrCode = attempt.Result?.QrCode,
                    ExternalReference = attempt.Result?.ExternalReference, Response = attempt.Result?.Response,
                    ResultDiagnostic = attempt.Result?.Diagnostic, Diagnostic = attempt.Diagnostic }) };
        }
        private static FiscalSigningAttempt Restore(FiscalDocumentReference document, SqlFiscalDatabase.JournalEntry entry)
        {
            var request = new FiscalSigningRequest(entry.Id, document, SqlFiscalDatabase.Read<FiscalSigningScope>(entry.Scope), entry.Format, entry.Payload);
            var state = (FiscalSigningState)Enum.Parse(typeof(FiscalSigningState), entry.State);
            var evidence = SqlFiscalDatabase.Read<Outcome>(entry.Snapshot);
            FiscalSigningResult result = null;
            if (state == FiscalSigningState.Signed) result = FiscalSigningResult.Signed(entry.Id, evidence.Signature, evidence.QrCode, evidence.ExternalReference, evidence.Response);
            if (state == FiscalSigningState.Rejected) result = FiscalSigningResult.Rejected(entry.Id, evidence.Response, evidence.ResultDiagnostic);
            if (state == FiscalSigningState.Unknown) result = FiscalSigningResult.Unknown(entry.Id, evidence.Response, evidence.ResultDiagnostic);
            return new FiscalSigningAttempt(request, state, entry.Revision, entry.CreatedUtc, entry.UpdatedUtc, result, evidence.Diagnostic);
        }
        private sealed class Outcome
        {
            public string Signature, QrCode, ExternalReference, Response, ResultDiagnostic, Diagnostic;
        }
    }
}
