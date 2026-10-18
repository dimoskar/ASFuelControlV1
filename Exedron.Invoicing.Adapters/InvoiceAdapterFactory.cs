using System;
using Exedron.Invoicing.Core;
using Exedron.Invoicing.Core.Models;

namespace Exedron.Invoicing.Adapters
{
    public sealed class InvoiceAdapterFactory : IInvoiceAdapterFactory
    {
        private readonly IInvoiceBuilder builder;
        private readonly IInvoiceXmlSerializer xml;
        public InvoiceAdapterFactory(IInvoiceTypeResolver resolver, IInvoiceXmlSerializer xml = null)
        {
            builder = new InvoiceBuilder(resolver ?? throw new ArgumentNullException(nameof(resolver)));
            this.xml = xml ?? new InvoiceXmlSerializer();
        }
        public IInvoiceProvider CreateITimologia(ITimologiaProviderOptions options) { return new ITimologiaInvoiceProvider(options); }
        public IInvoiceProvider CreateIlyda(IlydaProviderOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            options.XmlSerializer = xml;
            return new IlydaInvoiceProvider(options);
        }
        public IMyDataClient CreateMyData(MyDataOptions options) { return new MyDataClient(options, xml); }
        public IPaymentGateway CreateGBridge(GBridgeOptions options) { return new GBridgeClient(options); }
        public IPOSSupport CreatePOSSupport(IPaymentGateway gateway, IProviderPaymentAttemptStore store) { return new GBridgePOSSupport(gateway, store); }
        public IInvoicingService CreateService(IInvoiceProvider provider, IPOSSupport posSupport = null)
        { return new InvoicingService(builder, InvoiceIssuanceService.WithPOSSupport(provider, posSupport)); }
        public IInvoicePayloadMapper CreatePayloadMapper(string providerKey)
        { return new PayloadMapper(InvoiceProviderCatalog.Get(providerKey).Key, xml); }
        private sealed class PayloadMapper : IInvoicePayloadMapper
        {
            private readonly string provider;
            private readonly IInvoiceXmlSerializer xml;
            internal PayloadMapper(string provider, IInvoiceXmlSerializer xml) { this.provider = provider; this.xml = xml; }
            public string Serialize(InvoiceModel invoice, Guid invoiceId)
            {
                if (provider == InvoiceProviderCatalog.ITimologiaKey) return ITimologiaInvoiceProvider.PrepareRequestJson(invoice, invoiceId);
                if (provider == InvoiceProviderCatalog.DirectIlydaKey) return IlydaInvoiceProvider.PrepareRequestJson(invoice, xml.SerializeForProvider());
                return xml.Serialize(invoice, true);
            }
        }
    }
}
