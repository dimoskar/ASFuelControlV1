using System;
using Exedron.Invoicing.Core;

namespace ASFuelControl.Invoicing
{
    /// <summary>
    /// Shares product-neutral iTimologia communication tokens for the current Windows user.
    /// </summary>
    public sealed class UserEnvironmentCommunicationTokenCache : ICommunicationTokenCache
    {
        private const string ScopePrefix = "EXEDRON_INVOICING_ITIMOLOGIA_TOKEN_";

        public string Load(string scope)
        {
            return Environment.GetEnvironmentVariable(VariableName(scope), EnvironmentVariableTarget.User);
        }

        public void Save(string scope, string serializedToken)
        {
            Environment.SetEnvironmentVariable(
                VariableName(scope),
                serializedToken,
                EnvironmentVariableTarget.User);
        }

        internal static string VariableName(string scope)
        {
            if (string.IsNullOrEmpty(scope) ||
                !scope.StartsWith(ScopePrefix, StringComparison.Ordinal))
                throw new ArgumentException("Unsupported communication-token cache scope.", nameof(scope));

            return scope;
        }
    }
}
