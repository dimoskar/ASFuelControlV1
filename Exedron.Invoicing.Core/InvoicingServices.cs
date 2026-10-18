using System;
using System.Collections.Generic;
using Exedron.Invoicing.Core.Models;

namespace Exedron.Invoicing.Core
{
    public interface IInvoiceTypeResolver
    {
        string Resolve(InvoiceInput invoice, CountryInput country, IEnumerable<InvoiceParent> parents, MovementContext movement = null);
    }
    public interface IInvoiceBuilder
    {
        InvoiceModel Build(InvoiceBuildRequest request);
        CountryInput ResolveCountry(IEnumerable<CountryInput> countries, string value);
    }
    public interface IInvoiceXmlSerializer
    {
        string Serialize(InvoiceModel invoice, bool directMyData);
    }
    public interface IInvoicePayloadMapper
    {
        string Serialize(InvoiceModel invoice, Guid invoiceId);
    }
    public interface IInvoiceProviderRegistry
    {
        void Register(string providerKey, Func<IInvoiceProvider> factory);
        IInvoiceProvider Resolve(string providerKey);
    }
    public interface IInvoiceIssuanceService : IDisposable
    {
        InvoiceResponse SendInvoice(InvoiceModel invoice, Guid invoiceId);
        InvoiceResponse RecoverInvoice(Guid invoiceId);
        InvoiceLookupResult LookupInvoice(Guid invoiceId);
        void UploadIlydaInvoiceDocument(Guid invoiceId, byte[] pdf);
        void CancelIlydaSignature(Guid invoiceId, Guid attemptId, IProviderPaymentAttemptControl store);
    }
    // Entry point for all destinations: every send builds through the SAME resolver.
    public interface IInvoicingService : IDisposable
    {
        InvoiceResponse SendInvoice(InvoiceBuildRequest request);
        InvoiceResponse RecoverInvoice(Guid invoiceId);
        InvoiceLookupResult LookupInvoice(Guid invoiceId);
    }
    public interface IInvoiceDocumentUpload { void UploadInvoiceDocument(Guid invoiceId, byte[] pdf); }
    public interface IInvoiceSignagePolicy
    {
        bool UsesInvoiceProvider(bool providerIsEnabled, bool seriesUsesInvoiceProvider);
        bool NeedsSignature(bool isOfficial, bool hasSignature, bool usesInvoiceProvider, bool hasTaxDevice);
    }
    // Optional host-owned credential cache. No environment-variable/registry persistence in the library.
    public interface ICommunicationTokenCache
    {
        string Load(string scope);
        void Save(string scope, string serializedToken);
    }
    public interface IInvoiceAdapterFactory
    {
        IInvoiceProvider CreateITimologia(ITimologiaProviderOptions options);
        IInvoiceProvider CreateIlyda(IlydaProviderOptions options);
        IMyDataClient CreateMyData(MyDataOptions options);
        IPaymentGateway CreateGBridge(GBridgeOptions options);
        IPOSSupport CreatePOSSupport(IPaymentGateway gateway, IProviderPaymentAttemptStore store);
        IInvoicingService CreateService(IInvoiceProvider provider, IPOSSupport posSupport = null);
        IInvoicePayloadMapper CreatePayloadMapper(string providerKey);
    }
}
