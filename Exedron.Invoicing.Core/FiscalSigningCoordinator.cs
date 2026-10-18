using System;
using System.Linq;

namespace Exedron.Invoicing.Core
{
    // Optional local-only preflight. It must not perform device I/O.
    public interface IFiscalSigningRequestValidator
    {
        void Validate(FiscalSigningRequest request);
    }

    // Portable execution rules; numbering, rendering, configuration and cross-stage
    // policy are supplied by the host. The resolver returns a new owned signer instance.
    public sealed class FiscalSigningCoordinator
    {
        private readonly IFiscalSigningStore store;
        private readonly Func<FiscalSigningScope, IFiscalSigner> resolve;
        public FiscalSigningCoordinator(IFiscalSigningStore store, Func<FiscalSigningScope, IFiscalSigner> resolve)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.resolve = resolve ?? throw new ArgumentNullException(nameof(resolve));
        }

        public FiscalSigningAttempt Sign(FiscalDocumentReference document,
            Func<IFiscalDocumentLease, FiscalSigningRequest> prepare, bool retryDefinitiveFailure = false)
        {
            using (var lease = store.Acquire(document))
            {
                var previous = store.Load(lease).LastOrDefault();
                if (previous != null)
                {
                    if (previous.State == FiscalSigningState.Signed || previous.State == FiscalSigningState.Prepared) return previous;
                    if (previous.State == FiscalSigningState.Signing || previous.State == FiscalSigningState.Unknown) return Recover(lease, previous);
                    if (!retryDefinitiveFailure) return previous;
                }
                if (prepare == null) throw new ArgumentNullException(nameof(prepare));
                var request = prepare(lease);
                if (request == null || request.Document.Kind != document.Kind || request.Document.DocumentId != document.DocumentId)
                    throw new InvalidOperationException("The prepared signing request belongs to a different document.");
                if (previous != null && !SameScope(previous.Request.Scope, request.Scope))
                    throw new InvalidOperationException("A signing retry must retain its historical device and issuer scope.");
                var attempt = store.Begin(lease, request, DateTime.UtcNow);
                return Dispatch(lease, attempt);
            }
        }

        // A durable Prepared record proves no dispatch was begun by this coordinator.
        // Explicit resume sends its original bytes; it never renders a replacement.
        public FiscalSigningAttempt ResumePrepared(FiscalDocumentReference document)
        {
            using (var lease = store.Acquire(document))
            {
                var saved = Required(lease);
                if (saved.State != FiscalSigningState.Prepared) throw new InvalidOperationException("Only a prepared signing request can be resumed.");
                return Dispatch(lease, saved);
            }
        }
        public FiscalSigningAttempt Recover(FiscalDocumentReference document)
        {
            using (var lease = store.Acquire(document)) return Recover(lease, Required(lease));
        }

        private FiscalSigningAttempt Dispatch(IFiscalDocumentLease lease, FiscalSigningAttempt attempt)
        {
            IFiscalSigner signer = null;
            try
            {
                try { signer = Resolve(attempt.Request); }
                catch (Exception ex)
                {
                    return Save(lease, attempt, FiscalSigningState.FailedBeforeDispatch, null,
                        "Local signing validation failed: " + ex.GetType().Name);
                }
                // A failure here must prevent all external I/O.
                attempt = Save(lease, attempt, FiscalSigningState.Signing);
                FiscalSigningResult result;
                try { result = CheckResult(attempt.Request, signer.Sign(attempt.Request)); }
                catch (Exception ex)
                {
                    result = FiscalSigningResult.Unknown(attempt.Request.AttemptId, null,
                        "The signing call did not return a confirmed outcome: " + ex.GetType().Name);
                }
                // Never swallow a failed outcome save or return an undurable signature.
                return Save(lease, attempt, result.State, result);
            }
            finally { (signer as IDisposable)?.Dispose(); }
        }

        private FiscalSigningAttempt Recover(IFiscalDocumentLease lease, FiscalSigningAttempt saved)
        {
            if (saved.State != FiscalSigningState.Signing && saved.State != FiscalSigningState.Unknown) return saved;
            if (saved.State == FiscalSigningState.Signing)
                saved = Save(lease, saved, FiscalSigningState.Unknown, FiscalSigningResult.Unknown(saved.Request.AttemptId,
                    null, "Signing was interrupted; the original outcome must be reconciled."));
            IFiscalSigner signer = null;
            FiscalSigningResult result;
            try
            {
                signer = Resolve(saved.Request);
                var recovery = signer as IFiscalSignerRecovery;
                if (recovery == null) return saved; // No invented lookup or second Sign.
                result = CheckResult(saved.Request, recovery.Recover(saved.Request));
            }
            catch (Exception)
            {
                return saved;
            }
            finally { (signer as IDisposable)?.Dispose(); }
            return Save(lease, saved, result.State, result);
        }
        private IFiscalSigner Resolve(FiscalSigningRequest request)
        {
            var signer = resolve(request.Scope);
            if (signer == null) throw new InvalidOperationException("The historical signer is unavailable.");
            try
            {
                if (signer.Capabilities.SignerKey != request.Scope.SignerKey || !signer.Capabilities.DocumentKinds.Contains(request.Document.Kind))
                    throw new InvalidOperationException("The signer does not support this document or route.");
                (signer as IFiscalSigningRequestValidator)?.Validate(request);
                return signer;
            }
            catch { (signer as IDisposable)?.Dispose(); throw; }
        }
        private FiscalSigningAttempt Required(IFiscalDocumentLease lease)
        { return store.Load(lease).LastOrDefault() ?? throw new InvalidOperationException("No saved signing request exists."); }
        private FiscalSigningAttempt Save(IFiscalDocumentLease lease, FiscalSigningAttempt old, FiscalSigningState state,
            FiscalSigningResult result = null, string diagnostic = null)
        {
            var now = DateTime.UtcNow;
            if (now < old.UpdatedUtc) now = old.UpdatedUtc;
            var updated = new FiscalSigningAttempt(old.Request, state, old.Revision + 1, old.CreatedUtc, now, result, diagnostic);
            store.Save(lease, old.Revision, updated);
            return updated;
        }
        private static FiscalSigningResult CheckResult(FiscalSigningRequest request, FiscalSigningResult result)
        {
            return result != null && result.AttemptId == request.AttemptId ? result :
                FiscalSigningResult.Unknown(request.AttemptId, result?.Response, "The signer returned no result for the original attempt.");
        }
        public static bool SameScope(FiscalSigningScope a, FiscalSigningScope b) => a != null && b != null &&
            a.SignerKey == b.SignerKey && a.DeviceId == b.DeviceId && a.Environment == b.Environment &&
            a.IssuerVat == b.IssuerVat && a.IssuerBranch == b.IssuerBranch;

    }
}
