using System;
using System.Collections.Generic;
using Exedron.Invoicing.Core.Models;

namespace Exedron.Invoicing.Core
{
    public static class ProviderPrintMetadata
    {
        // This check concerns printing only. A missing footer never revokes fiscal
        // acceptance and must never authorize another invoice POST or POS charge.
        public static string[] Missing(string providerKey, InvoiceResponse response, string softwareLicence)
        {
            // Retain the earlier signature for callers restoring historical routes.
            // Licence identity is now supplied only by the provider catalogue.
            return Missing(providerKey,response);
        }
        public static string[] Missing(string providerKey, InvoiceResponse response)
        {
            var missing = new List<string>();
            if (providerKey == InvoiceProviderCatalog.MyDataKey || providerKey == null) return missing.ToArray();
            bool offline = providerKey == InvoiceProviderCatalog.DirectIlydaKey && response?.TransmissionFailure2 == true;
            if (!offline && string.IsNullOrWhiteSpace(response?.Mark)) missing.Add("MARK");
            if (string.IsNullOrWhiteSpace(response?.Uid)) missing.Add("UID");
            if (!offline && string.IsNullOrWhiteSpace(response?.VerificationHash)) missing.Add("Authentication code");
            if (!WebUrl(response?.QrCodeUrl)) missing.Add("QR URL");
            if (!WebUrl(response?.ProviderUrl)) missing.Add("Provider URL");
            if (string.IsNullOrWhiteSpace(InvoiceProviderCatalog.Get(providerKey).SoftwareLicence)) missing.Add("Provider software licence");
            if (!offline && providerKey == InvoiceProviderCatalog.DirectIlydaKey && string.IsNullOrWhiteSpace(response?.ProviderInvoiceId)) missing.Add("Ilyda invoiceId");
            return missing.ToArray();
        }
        private static bool WebUrl(string value)
        {
            Uri uri;
            return Uri.TryCreate(value, UriKind.Absolute, out uri) && (uri.Scheme == "https" || uri.Scheme == "http") && uri.UserInfo.Length == 0;
        }
    }
}
