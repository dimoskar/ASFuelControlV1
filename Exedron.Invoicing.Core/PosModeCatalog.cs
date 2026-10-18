using System;
using System.Collections.Generic;
using System.Linq;

namespace Exedron.Invoicing.Core
{
    // Ilyda A1155 POS v1.3.2, section 7.7 (pp. 27-28); GBridge Development notes 2.1,
    // lines 54-61. Numeric iTimologia wire translation remains adapter-owned.
    // GBridge is the transport, independent of the terminal's signature mode.
    public static class PosModeCatalog
    {
        private static readonly PosModeDefinition[] Modes =
        {
            new PosModeDefinition("EDPS", "EDPS", 1),
            new PosModeDefinition("MELLON", "Mellon", 0),
            new PosModeDefinition("VIVA", "Viva", 1),
            new PosModeDefinition("EPAY", "ePay", 0),
            new PosModeDefinition("CARDLINK", "Cardlink", 0),
            new PosModeDefinition("NEXI", "Nexi", 0),
            new PosModeDefinition("NEOSOFT", "Neosoft", null),
            new PosModeDefinition("ATTICA", "Attica", null),
            new PosModeDefinition("WORLDLINE", "Worldline", null)
        };

        public static IReadOnlyList<PosModeDefinition> ForProvider(string providerKey)
        {
            return Array.AsReadOnly(Modes.Where(mode =>
                providerKey == InvoiceProviderCatalog.DirectIlydaKey ||
                providerKey == InvoiceProviderCatalog.ITimologiaKey && mode.ITimologiaSignatureFormat.HasValue).ToArray());
        }

        public static PosModeDefinition Get(string providerKey, string modeKey)
        {
            var mode = ForProvider(providerKey).SingleOrDefault(value => value.Key == modeKey);
            if (mode == null) throw new ArgumentException("Επιλέξτε υποστηριζόμενο τρόπο POS για τον πάροχο τιμολόγησης.");
            return mode;
        }
    }

    public sealed class PosModeDefinition
    {
        internal PosModeDefinition(string key, string displayName, int? iTimologiaSignatureFormat)
        { Key = key; DisplayName = displayName; ITimologiaSignatureFormat = iTimologiaSignatureFormat; }
        public string Key { get; }
        public string DisplayName { get; }
        public int? ITimologiaSignatureFormat { get; }
        public string IlydaProtocol => Key == "EDPS" ? "DEFAULT" : Key;
        // The supplied GBridge contract supports PaymentType=1 only on Mellon.
        public bool SupportsIris(string providerKey) => providerKey == InvoiceProviderCatalog.ITimologiaKey && Key == "MELLON";
    }
}
