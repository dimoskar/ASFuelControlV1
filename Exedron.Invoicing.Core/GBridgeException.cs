using System;
namespace Exedron.Invoicing.Core
{
    public sealed class GBridgeException : Exception
    {
        public GBridgeException(string message)
            : this(message, GBridgeFailureKind.Failed, null) { }

        public GBridgeException(string message, Exception innerException)
            : this(message, GBridgeFailureKind.Failed, innerException) { }

        public GBridgeException(string message, GBridgeFailureKind failureKind)
            : this(message, failureKind, null) { }

        public GBridgeException(string message, GBridgeFailureKind failureKind, Exception innerException)
            : base(message, innerException)
        {
            FailureKind = failureKind;
        }

        public GBridgeFailureKind FailureKind { get; private set; }
    }

    public enum GBridgeFailureKind
    {
        Failed,
        Unknown,
        Mismatch,
        Declined,
        SignatureRejected,
        CredentialsRejected,
        IncompleteResponse
    }
}
