using Exedron.Invoicing.Core;
using System;
using System.Collections.Generic;

namespace Exedron.Invoicing.Adapters
{
    /// <summary>
    /// Explicit provider registration avoids reflection/plugin loading while keeping
    /// provider selection independent of concrete implementations.
    /// </summary>
    public sealed class InvoiceProviderRegistry : IInvoiceProviderRegistry
    {
        private readonly Dictionary<string, Func<IInvoiceProvider>> factories =
            new Dictionary<string, Func<IInvoiceProvider>>(StringComparer.OrdinalIgnoreCase);

        public void Register(string providerKey, Func<IInvoiceProvider> factory)
        {
            InvoiceProviderDefinition descriptor = InvoiceProviderCatalog.Get(providerKey);
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            factories[descriptor.Key] = factory;
        }

        public IInvoiceProvider Resolve(string providerKey)
        {
            InvoiceProviderDefinition descriptor = InvoiceProviderCatalog.Get(providerKey);
            Func<IInvoiceProvider> factory;
            if (!factories.TryGetValue(descriptor.Key, out factory))
            {
                throw new InvalidOperationException(
                    "No implementation is registered for invoice provider " + descriptor.DisplayName + ".");
            }

            IInvoiceProvider provider = factory();
            if (provider == null)
            {
                throw new InvalidOperationException(
                    "The registered invoice-provider factory returned no implementation for " +
                    descriptor.DisplayName + ".");
            }

            return provider;
        }
    }
}
