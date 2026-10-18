using Exedron.Invoicing.Core;
using Exedron.Invoicing.Core.Models;
using System;
using System.Linq;

namespace Exedron.Invoicing.Adapters
{
    /// <summary>Routes POS invoices through the selected POS implementation.</summary>
    public sealed class InvoiceIssuanceService : IInvoiceIssuanceService
    {
        private readonly IInvoiceProvider provider;
        private readonly IPOSSupport posSupport;

        // Compatibility constructors for existing gateway callers.
        public InvoiceIssuanceService(IInvoiceProvider provider, IPaymentGateway paymentGateway)
            : this(provider, paymentGateway, null) { }

        public InvoiceIssuanceService(IInvoiceProvider provider, IPaymentGateway paymentGateway,
            IProviderPaymentAttemptStore paymentAttemptStore)
            : this(provider, paymentGateway != null
                ? (IPOSSupport)new GBridgePOSSupport(paymentGateway, paymentAttemptStore)
                : provider as IPOSSupport, true) { }

        private InvoiceIssuanceService(IInvoiceProvider provider, IPOSSupport posSupport, bool usePOSSupport)
        {
            this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
            this.posSupport = posSupport;
        }

        public static InvoiceIssuanceService WithPOSSupport(IInvoiceProvider provider, IPOSSupport posSupport)
        {
            return new InvoiceIssuanceService(provider, posSupport, true);
        }

        public InvoiceLookupResult LookupInvoice(Guid invoiceId)
        {
            var lookup = provider as IInvoiceProviderLookup;
            if (lookup == null) throw new InvalidOperationException("Ο πάροχος δεν υποστηρίζει έλεγχο ύπαρξης παραστατικού.");
            return lookup.LookupInvoice(invoiceId);
        }

        public void UploadIlydaInvoiceDocument(Guid invoiceId, byte[] pdf)
        {
            var ilyda = provider as IInvoiceDocumentUpload;
            if (ilyda == null) throw new InvalidOperationException("Δεν έχει επιλεγεί Ilyda (απευθείας).");
            ilyda.UploadInvoiceDocument(invoiceId, pdf);
        }

        public InvoiceResponse RecoverInvoice(Guid invoiceId)
        {
            var recovery = provider as IInvoiceProviderRecovery;
            if (recovery == null) throw new InvalidOperationException("Ο πάροχος δεν υποστηρίζει ανάκτηση παραστατικού.");
            return recovery.RecoverInvoice(invoiceId);
        }

        public InvoiceResponse SendInvoice(InvoiceModel invoice, Guid invoiceId)
        {
            bool hasPosPayment = invoice?.PaymentMethods != null && invoice.PaymentMethods.Any(
                p => p != null && ((int)p.Type == 7 || (int)p.Type == 8));
            if (!hasPosPayment) return provider.SendInvoice(invoice, invoiceId, null);
            if (posSupport == null)
                return new InvoiceResponse
                {
                    Errors = new[] { "The selected invoice provider/POS combination does not support POS/IRIS issuance. Configure a compatible POS integration." },
                    Warnings = new string[0]
                };
            return posSupport.SendInvoiceWithPOS(provider, invoice, invoiceId);
        }

        public void CancelIlydaSignature(Guid invoiceId, Guid attemptId, IProviderPaymentAttemptControl store)
        {
            var lifecycle = provider as IInvoiceProviderPaymentLifecycle;
            if (provider.Descriptor.Key != InvoiceProviderCatalog.DirectIlydaKey || lifecycle == null)
                throw new InvalidOperationException("Επιλέξτε τον αρχικό πάροχο Ilyda (απευθείας).");
            using (lifecycle.AcquirePayment(invoiceId))
            {
                var attempt = store.LoadLatest(invoiceId);
                if (attempt == null || attempt.Id != attemptId || attempt.ProviderId != InvoiceProviderCatalog.Get(InvoiceProviderCatalog.DirectIlydaKey).AadeProviderId ||
                    !new[] { "Prepared", "SignatureReady", "Declined", "Failed", "Voided" }.Contains(attempt.State))
                    throw new InvalidOperationException("Η πληρωμή έχει εκτελεστεί ή έχει αβέβαιο αποτέλεσμα. Δεν επιτρέπεται ακύρωση της υπογραφής.");
                lifecycle.CancelUnusedPaymentSignature(invoiceId, "TRANSACTION_CANCELLED");
                store.MarkFailure(attempt.Id, ProviderPaymentAttemptStates.Failed, "SignatureCancelled", "Η μη χρησιμοποιημένη υπογραφή Ilyda ακυρώθηκε.");
            }
        }

        public void Dispose()
        {
            try
            {
                if (!ReferenceEquals(posSupport, provider)) posSupport?.Dispose();
            }
            finally { provider.Dispose(); }
        }
    }

    public static class InvoiceSignageRouter
    {
        private static readonly IInvoiceSignagePolicy Policy = new FiscalSignagePolicy();
        public static bool UsesInvoiceProvider(bool providerIsEnabled, bool seriesUsesInvoiceProvider)
        {
            return Policy.UsesInvoiceProvider(providerIsEnabled, seriesUsesInvoiceProvider);
        }

        public static bool NeedsSignature(
            bool isOfficial,
            bool hasSignature,
            bool usesInvoiceProvider,
            bool hasTaxDevice)
        {
            return Policy.NeedsSignature(isOfficial, hasSignature, usesInvoiceProvider, hasTaxDevice);
        }
    }
}
