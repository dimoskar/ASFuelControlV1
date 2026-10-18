using System;
using System.Linq;
using Exedron.Invoicing.Core;
using Newtonsoft.Json;

namespace ASFuelControl.Invoicing
{
    // Non-secret identity. CredentialProfile names a host-managed credential set/version.
    public sealed class ProviderRoute
    {
        [JsonConstructor]
        public ProviderRoute(string providerKey, bool testMode, string issuerVat, int issuerBranch,
            string credentialProfile, string account, string myDataBaseUrl = null, string softwareLicence = null)
        {
            ProviderKey = InvoiceProviderCatalog.Get(providerKey).Key;
            IssuerVat = (issuerVat ?? "").Trim();
            if (IssuerVat.Length != 9 || IssuerVat.Any(c => c < '0' || c > '9') || issuerBranch < 0)
                throw new ArgumentException("A nine-digit issuer VAT and non-negative branch are required.");
            if (string.IsNullOrWhiteSpace(credentialProfile) || string.IsNullOrWhiteSpace(account))
                throw new ArgumentException("A credential profile and original provider account are required.");
            TestMode = testMode; IssuerBranch = issuerBranch;
            CredentialProfile = credentialProfile.Trim(); Account = account.Trim();
            SoftwareLicence = string.IsNullOrWhiteSpace(softwareLicence) ? null : softwareLicence.Trim();
            if (ProviderKey != InvoiceProviderCatalog.MyDataKey && myDataBaseUrl != null)
                throw new ArgumentException("Only myDATA supports a base URL override.");
            if (ProviderKey == InvoiceProviderCatalog.MyDataKey)
            {
                MyDataBaseUrl = (string.IsNullOrWhiteSpace(myDataBaseUrl)
                    ? testMode ? MyDataOptions.TestUrl : MyDataOptions.ProductionUrl : myDataBaseUrl.Trim()).TrimEnd('/') + "/";
                Uri uri;
                if (!Uri.TryCreate(MyDataBaseUrl, UriKind.Absolute, out uri) || uri.Scheme != "https" ||
                    uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0)
                    throw new ArgumentException("myDATA requires an HTTPS endpoint without credentials, query or fragment.");
                if ((testMode && MyDataBaseUrl.Equals(MyDataOptions.ProductionUrl, StringComparison.OrdinalIgnoreCase)) ||
                    (!testMode && MyDataBaseUrl.Equals(MyDataOptions.TestUrl, StringComparison.OrdinalIgnoreCase)))
                    throw new ArgumentException("myDATA endpoint contradicts the selected environment.");
            }
        }
        public string ProviderKey { get; }
        public bool TestMode { get; }
        public string IssuerVat { get; }
        public int IssuerBranch { get; }
        public string CredentialProfile { get; }
        // myDATA user ID, iTimologia subscription ID, or Ilyda user/account identifier.
        public string Account { get; }
        public string MyDataBaseUrl { get; }
        // Retained verbatim for historical route/credential fingerprints. New settings
        // use the catalogue value; printing reads official identity from the catalogue.
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string SoftwareLicence { get; }
    }

    public interface IProviderCredentialsResolver
    {
        // Resolve precisely this saved route. Never fall back to the currently selected account.
        ProviderCredentials Resolve(ProviderRoute route);
    }
    public sealed class ProviderCredentials
    {
        public string SubscriptionKey { get; set; }
        public string MasterToken { get; set; }
        public string ApiKey { get; set; }
        public string Password { get; set; }
    }
}
