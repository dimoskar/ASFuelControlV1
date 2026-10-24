using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;

namespace Exedron.Invoicing.Core
{
    public enum FiscalDocumentKind
    {
        Unspecified = 0, Invoice = 1, Balance = 2, TankFilling = 3, Alert = 4, Titrimetry = 5, MyDieselCoupon = 6
    }
    public enum FiscalEnvironment { Unspecified = 0, Test = 1, Production = 2 }

    public sealed class FiscalDocumentReference
    {
        public FiscalDocumentReference(FiscalDocumentKind kind, Guid documentId)
        {
            FiscalContract.Require(kind != FiscalDocumentKind.Unspecified && Enum.IsDefined(typeof(FiscalDocumentKind), kind), "Document kind is required.");
            FiscalContract.Require(documentId != Guid.Empty, "Document ID is required.");
            Kind = kind;
            DocumentId = documentId;
        }
        public FiscalDocumentKind Kind { get; }
        public Guid DocumentId { get; }
    }

    // Snapshot the historical route. Keys identify configured adapters/devices, never passwords.
    public sealed class FiscalSigningScope
    {
        public FiscalSigningScope(string signerKey, string deviceId, FiscalEnvironment environment, string issuerVat, int issuerBranch)
        {
            SignerKey = FiscalContract.Text(signerKey, "Signer key");
            DeviceId = FiscalContract.Text(deviceId, "Device ID");
            FiscalContract.Require(environment != FiscalEnvironment.Unspecified && Enum.IsDefined(typeof(FiscalEnvironment), environment), "Environment is required.");
            FiscalContract.Require(issuerBranch >= 0, "Issuer branch cannot be negative.");
            Environment = environment;
            IssuerVat = FiscalContract.Text(issuerVat, "Issuer VAT");
            IssuerBranch = issuerBranch;
        }
        public string SignerKey { get; }
        public string DeviceId { get; }
        public FiscalEnvironment Environment { get; }
        public string IssuerVat { get; }
        public int IssuerBranch { get; }
    }

    // Created after host numbering and adapter-specific payload preparation, before transport.
    // Sign MUST send these saved bytes; it must not rebuild from mutable database entities.
    public sealed class FiscalSigningRequest
    {
        private readonly byte[] payload;
        public FiscalSigningRequest(Guid attemptId, FiscalDocumentReference document, FiscalSigningScope scope,
            string payloadFormat, byte[] payload)
        {
            FiscalContract.Require(attemptId != Guid.Empty, "Attempt ID is required.");
            Document = document ?? throw new ArgumentNullException(nameof(document));
            Scope = scope ?? throw new ArgumentNullException(nameof(scope));
            PayloadFormat = FiscalContract.Text(payloadFormat, "Versioned payload format");
            FiscalContract.Require(payload != null && payload.Length > 0, "Exact request payload is required.");
            this.payload = (byte[])payload.Clone();
            AttemptId = attemptId;
            using (var sha = SHA256.Create())
                PayloadSha256 = BitConverter.ToString(sha.ComputeHash(this.payload)).Replace("-", "").ToLowerInvariant();
        }
        public Guid AttemptId { get; }
        public FiscalDocumentReference Document { get; }
        public FiscalSigningScope Scope { get; }
        public string PayloadFormat { get; }
        public string PayloadSha256 { get; }
        public byte[] Payload { get { return (byte[])payload.Clone(); } }
    }

    public sealed class FiscalSignerCapabilities
    {
        public FiscalSignerCapabilities(string signerKey, IEnumerable<FiscalDocumentKind> documentKinds)
        {
            SignerKey = FiscalContract.Text(signerKey, "Signer key");
            if (documentKinds == null) throw new ArgumentNullException(nameof(documentKinds));
            var kinds = documentKinds.Distinct().ToArray();
            FiscalContract.Require(kinds.Length > 0 && kinds.All(k => k != FiscalDocumentKind.Unspecified &&
                Enum.IsDefined(typeof(FiscalDocumentKind), k)), "Declare only supported fiscal document kinds.");
            DocumentKinds = Array.AsReadOnly(kinds);
        }
        public string SignerKey { get; }
        public ReadOnlyCollection<FiscalDocumentKind> DocumentKinds { get; }
    }

    public sealed class FiscalSigningResult
    {
        private FiscalSigningResult(Guid attemptId, FiscalSigningState state, string signature, string qrCode,
            string externalReference, string response, string diagnostic)
        {
            FiscalContract.Require(attemptId != Guid.Empty, "Attempt ID is required.");
            AttemptId = attemptId;
            State = state;
            Signature = signature;
            QrCode = qrCode;
            ExternalReference = externalReference;
            Response = response;
            Diagnostic = diagnostic;
        }
        public static FiscalSigningResult Signed(Guid attemptId, string signature, string qrCode, string externalReference, string response)
        {
            FiscalContract.Text(signature, "Signature");
            FiscalContract.Require(signature.Trim() != "-" && signature.Trim() != "Sign Device A", "A legacy placeholder is not a fiscal signature.");
            FiscalContract.Text(response, "Acceptance evidence");
            return new FiscalSigningResult(attemptId, FiscalSigningState.Signed, signature, qrCode, externalReference, response, null);
        }
        // Rejection means authoritative evidence that THIS request was not signed.
        public static FiscalSigningResult Rejected(Guid attemptId, string response, string diagnostic)
        {
            FiscalContract.Text(response, "Rejection evidence");
            return new FiscalSigningResult(attemptId, FiscalSigningState.Rejected, null, null, null, response,
                FiscalContract.Text(diagnostic, "Rejection reason"));
        }
        // Timeouts, malformed replies, lookup 'not found', unsupported lookup => Unknown.
        public static FiscalSigningResult Unknown(Guid attemptId, string response, string diagnostic)
        {
            return new FiscalSigningResult(attemptId, FiscalSigningState.Unknown, null, null, null, response,
                FiscalContract.Text(diagnostic, "Uncertainty reason"));
        }
        public Guid AttemptId { get; }
        public FiscalSigningState State { get; }
        public string Signature { get; }
        public string QrCode { get; }
        public string ExternalReference { get; }
        // Exact response text/evidence, retained even when parsing fails. No credential material.
        public string Response { get; }
        public string Diagnostic { get; }
    }

    public interface IFiscalSigner
    {
        FiscalSignerCapabilities Capabilities { get; }
        // Validate document capability and historical signer/device/environment against this
        // adapter's configuration before dispatch. Unsupported kinds must never be simulated.
        // Caller must hold a durable document lease and persist Signing before this call.
        // An exception after dispatch is uncertain; it is never proof of rejection.
        FiscalSigningResult Sign(FiscalSigningRequest request);
    }
    // Optional capability. Implement only when the device can identify the original request.
    public interface IFiscalSignerRecovery
    {
        // Read-only lookup using the original scope/payload. Never dispatch Sign from here.
        // An empty/not-found response MUST return Unknown, not Rejected.
        FiscalSigningResult Recover(FiscalSigningRequest originalRequest);
    }

    public sealed class FiscalSigningAttempt
    {
        public FiscalSigningAttempt(FiscalSigningRequest request, FiscalSigningState state, long revision,
            DateTime createdUtc, DateTime updatedUtc, FiscalSigningResult result = null, string diagnostic = null)
        {
            Request = request ?? throw new ArgumentNullException(nameof(request));
            FiscalContract.Require(Enum.IsDefined(typeof(FiscalSigningState), state) &&
                state != FiscalSigningState.Unspecified && state != FiscalSigningState.NotRequired, "Invalid signing attempt state.");
            FiscalContract.Require(revision >= 0, "Revision cannot be negative.");
            FiscalContract.Require(createdUtc.Kind == DateTimeKind.Utc && updatedUtc.Kind == DateTimeKind.Utc && updatedUtc >= createdUtc,
                "Audit times must be ordered UTC timestamps.");
            bool hasOutcome = state == FiscalSigningState.Signed || state == FiscalSigningState.Rejected || state == FiscalSigningState.Unknown;
            FiscalContract.Require(hasOutcome ? result != null && result.AttemptId == request.AttemptId && result.State == state : result == null,
                "The outcome must match this attempt and state.");
            if (state == FiscalSigningState.FailedBeforeDispatch) FiscalContract.Text(diagnostic, "Local failure reason");
            State = state;
            Revision = revision;
            CreatedUtc = createdUtc;
            UpdatedUtc = updatedUtc;
            Result = result;
            Diagnostic = diagnostic;
        }
        public FiscalSigningRequest Request { get; }
        public FiscalSigningState State { get; }
        public long Revision { get; }
        public DateTime CreatedUtc { get; }
        public DateTime UpdatedUtc { get; }
        public FiscalSigningResult Result { get; }
        public string Diagnostic { get; }
    }

    // SQL implementation is supplied by the host. In-process locks are insufficient.
    public interface IFiscalDocumentLease : IDisposable
    {
        FiscalDocumentReference Document { get; }
        // Monotonic fencing token: stale owners cannot commit after lease replacement.
        long FencingToken { get; }
    }
    public interface IFiscalSigningStore
    {
        // Lock by document kind + ID across ALL routes, devices and environments.
        // Throw on contention; never return an unowned/null lease.
        IFiscalDocumentLease Acquire(FiscalDocumentReference document);
        // Includes historical scopes; retained attempts are append-only audit evidence.
        IReadOnlyList<FiscalSigningAttempt> Load(IFiscalDocumentLease lease);
        IReadOnlyList<FiscalSigningAttempt> LoadHistory(IFiscalDocumentLease lease, Guid attemptId);
        // Atomically persist Prepared, revision 0 and the immutable request before dispatch.
        // Reject duplicate attempt IDs and any document with Signed/Prepared/Signing/Unknown attempts,
        // even if its settings or selected device have changed. No implicit retry policy.
        FiscalSigningAttempt Begin(IFiscalDocumentLease lease, FiscalSigningRequest request, DateTime createdUtc);
        // Validate fencing token, expected revision, unchanged request/CreatedUtc, incremented revision
        // and FiscalStateTransitions. Append a revision and update the current projection atomically;
        // retain ALL prior responses/diagnostics. Mismatch must throw, never overwrite.
        void Save(IFiscalDocumentLease lease, long expectedRevision, FiscalSigningAttempt updated);
    }

    internal static class FiscalContract
    {
        internal static void Require(bool condition, string message)
        {
            if (!condition) throw new ArgumentException(message);
        }
        internal static string Text(string value, string name)
        {
            Require(!string.IsNullOrWhiteSpace(value), name + " is required.");
            return value.Trim();
        }
    }
}
