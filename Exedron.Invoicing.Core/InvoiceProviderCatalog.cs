using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Exedron.Invoicing.Core
{
    /// <summary>
    /// Application-owned invoice-provider registry. External protocol identifiers belong
    /// here rather than in user configuration or UI controls.
    /// </summary>
    public static class InvoiceProviderCatalog
    {
        public const string MyDataKey = "myDATA";
        public const string ITimologiaKey = "iTimologia";
        public const string DirectIlydaKey = "Ilyda.Direct";

        // Single source for the numeric AADE identifiers used by invoice and POS APIs.
        public const int DirectIlydaProviderId = 8;
        public const int ITimologiaProviderId = 34;
        // Compatibility aliases; GBridge uses the same AADE provider identity.
        public const int DirectIlydaGBridgeProviderId = DirectIlydaProviderId;
        public const int ITimologiaGBridgeProviderId = ITimologiaProviderId;

        // AADE licensed-software registry, checked 2026-10-16. These licence
        // identifiers are distinct from provider codes 008/034 and list row numbers.
        // https://www.aade.gr/node/14465
        public const string DirectIlydaSoftwareLicence = "2021_09_108ILYDA_001_ Meg myData_V1_08092021";
        // https://www.aade.gr/node/14610
        public const string ITimologiaSoftwareLicence = "2025_11_134TECHNORAN_001_iTimologia_V1_17112025";

        private static readonly InvoiceProviderDefinition[] Providers =
        {
            new InvoiceProviderDefinition(MyDataKey, "myDATA (direct ERP)", null),
            new InvoiceProviderDefinition(DirectIlydaKey, "Ilyda (απευθείας)", DirectIlydaProviderId, DirectIlydaSoftwareLicence),

            // AADE assigns TECHNORAN/iTimologia provider code 034 (https://www.aade.gr/node/14610).
            // The licensed-software list row number 35 is not the protocol identifier.
            new InvoiceProviderDefinition(ITimologiaKey, "iTimologia", ITimologiaProviderId, ITimologiaSoftwareLicence)
        };

        public static IReadOnlyList<InvoiceProviderDefinition> All
        {
            get { return Providers; }
        }

        public static InvoiceProviderDefinition Get(string key)
        {
            string normalized = (key ?? "").Trim();
            InvoiceProviderDefinition provider = Providers.FirstOrDefault(value =>
                string.Equals(value.Key, normalized, StringComparison.OrdinalIgnoreCase));
            if (provider == null)
            {
                throw new InvalidOperationException("Unsupported invoice provider: " + normalized + ".");
            }

            return provider;
        }
    }

    public sealed class InvoiceProviderDefinition
    {
        internal InvoiceProviderDefinition(string key, string displayName, int? aadeProviderId, string softwareLicence = null)
        {
            Key = key;
            DisplayName = displayName;
            AadeProviderId = aadeProviderId;
            SoftwareLicence = softwareLicence;
        }

        public string Key { get; private set; }
        public string DisplayName { get; private set; }
        public int? AadeProviderId { get; private set; }
        public string AadeProviderCode => AadeProviderId?.ToString("000",CultureInfo.InvariantCulture);
        public int? GBridgeProviderId => AadeProviderId;
        public string SoftwareLicence { get; private set; }
    }
}
