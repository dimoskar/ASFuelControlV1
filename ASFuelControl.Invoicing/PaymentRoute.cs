using System;
using Exedron.Invoicing.Adapters;
using Exedron.Invoicing.Core;
using Newtonsoft.Json;

namespace ASFuelControl.Invoicing
{
    public sealed class PaymentRoute
    {
        [JsonConstructor]
        public PaymentRoute(ProviderRoute provider, string endpoint, string terminalId, int iTimologiaSignatureFormat = 0, string ilydaProtocol = "DEFAULT", string posMode = null)
        {
            if (provider == null || provider.ProviderKey == InvoiceProviderCatalog.MyDataKey)
                throw new ArgumentException("GBridge requires a provider with a payment-signature capability.");
            string normalized, error;
            if (string.IsNullOrWhiteSpace(endpoint) || !GBridgeClient.TryNormalizeEndpoint(endpoint, out normalized, out error) ||
                !string.IsNullOrEmpty(new Uri(endpoint).UserInfo)) throw new ArgumentException("An explicit, credential-free gateway endpoint is required.");
            if (string.IsNullOrWhiteSpace(terminalId) || iTimologiaSignatureFormat < 0 || iTimologiaSignatureFormat > 1)
                throw new ArgumentException("Terminal and a supported signature format are required.");
            ilydaProtocol = (ilydaProtocol ?? "DEFAULT").Trim().ToUpperInvariant();
            if (Array.IndexOf(IlydaInvoiceProvider.PosProtocols, ilydaProtocol) < 0) throw new ArgumentException("Unsupported Ilyda POS protocol.");
            if (posMode != null)
            {
                var mode = PosModeCatalog.Get(provider.ProviderKey,posMode);
                if (provider.ProviderKey == InvoiceProviderCatalog.ITimologiaKey && mode.ITimologiaSignatureFormat != iTimologiaSignatureFormat ||
                    provider.ProviderKey == InvoiceProviderCatalog.DirectIlydaKey && mode.IlydaProtocol != ilydaProtocol)
                    throw new ArgumentException("POS mode does not match the provider signature settings.");
            }
            Provider = provider; Endpoint = normalized; TerminalId = terminalId.Trim();
            ITimologiaSignatureFormat = iTimologiaSignatureFormat; IlydaProtocol = ilydaProtocol;
            PosMode = posMode;
        }
        public static PaymentRoute ForMode(ProviderRoute provider, string endpoint, string terminalId, string posMode)
        {
            if (provider == null) throw new ArgumentException("Ορίστε πρώτα τον πάροχο τιμολόγησης για το POS.");
            var mode = PosModeCatalog.Get(provider.ProviderKey,posMode);
            return new PaymentRoute(provider,endpoint,terminalId,
                provider.ProviderKey == InvoiceProviderCatalog.ITimologiaKey ? mode.ITimologiaSignatureFormat.Value : 0,
                provider.ProviderKey == InvoiceProviderCatalog.DirectIlydaKey ? mode.IlydaProtocol : "DEFAULT",mode.Key);
        }
        public ProviderRoute Provider { get; }
        public string Endpoint { get; }
        public string TerminalId { get; }
        public int ITimologiaSignatureFormat { get; }
        public string IlydaProtocol { get; }
        // Omit for historical routes: their serialized payloads/fingerprints stay unchanged.
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string PosMode { get; }
    }
}
