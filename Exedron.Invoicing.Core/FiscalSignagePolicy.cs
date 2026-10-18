namespace Exedron.Invoicing.Core
{
    // Routing only. A provider/payment signature is not an IFiscalSigner device result.
    // hasSignature must mean verified saved evidence, never IsPrinted or a legacy placeholder.
    public sealed class FiscalSignagePolicy : IInvoiceSignagePolicy
    {
        public bool UsesInvoiceProvider(bool providerIsEnabled, bool seriesUsesInvoiceProvider)
        {
            return providerIsEnabled && seriesUsesInvoiceProvider;
        }
        public bool NeedsSignature(bool isOfficial, bool hasSignature, bool usesInvoiceProvider, bool hasTaxDevice)
        {
            return isOfficial && !hasSignature && (usesInvoiceProvider || hasTaxDevice);
        }
    }
}
