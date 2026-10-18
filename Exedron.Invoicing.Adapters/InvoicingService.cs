using System;
using Exedron.Invoicing.Core;
using Exedron.Invoicing.Core.Models;

namespace Exedron.Invoicing.Adapters
{
    public sealed class InvoicingService : IInvoicingService
    {
        private readonly IInvoiceBuilder builder;
        private readonly IInvoiceIssuanceService issuance;
        public InvoicingService(IInvoiceBuilder builder, IInvoiceIssuanceService issuance)
        {
            this.builder = builder ?? throw new ArgumentNullException(nameof(builder));
            this.issuance = issuance ?? throw new ArgumentNullException(nameof(issuance));
        }
        public InvoiceResponse SendInvoice(InvoiceBuildRequest request)
        {
            // Build once for every destination; providers never resolve types independently.
            var invoice = builder.Build(request);
            return WithInvoice(request.Invoice.InvoiceId, () => issuance.SendInvoice(invoice, request.Invoice.InvoiceId));
        }
        public InvoiceResponse RecoverInvoice(Guid invoiceId) { return WithInvoice(invoiceId, () => issuance.RecoverInvoice(invoiceId)); }
        public InvoiceLookupResult LookupInvoice(Guid invoiceId) { return WithInvoice(invoiceId, () => issuance.LookupInvoice(invoiceId)); }
        private static T WithInvoice<T>(Guid invoiceId, Func<T> action)
        {
            var previous = LoggedHttpClient.CurrentInvoice.Value;
            LoggedHttpClient.CurrentInvoice.Value = invoiceId;
            try { return action(); }
            finally { LoggedHttpClient.CurrentInvoice.Value = previous; }
        }
        public void Dispose() { issuance.Dispose(); }
    }

    public sealed class InvoiceXmlSerializer : IInvoiceXmlSerializer
    {
        public string Serialize(InvoiceModel invoice, bool directMyData)
        { return directMyData ? WorkflowInvoiceXml.BuildForDirectMyData(invoice) : WorkflowInvoiceXml.Build(invoice); }
    }
    internal static class XmlSerializerExtensions
    {
        internal static Func<InvoiceModel, string> SerializeForProvider(this IInvoiceXmlSerializer serializer)
        { return serializer == null ? null : (Func<InvoiceModel, string>)(invoice => serializer.Serialize(invoice, false)); }
    }
    public sealed class InvoiceSignagePolicy : IInvoiceSignagePolicy
    {
        public bool UsesInvoiceProvider(bool providerIsEnabled, bool seriesUsesInvoiceProvider)
        { return InvoiceSignageRouter.UsesInvoiceProvider(providerIsEnabled, seriesUsesInvoiceProvider); }
        public bool NeedsSignature(bool isOfficial, bool hasSignature, bool usesInvoiceProvider, bool hasTaxDevice)
        { return InvoiceSignageRouter.NeedsSignature(isOfficial, hasSignature, usesInvoiceProvider, hasTaxDevice); }
    }
}
