using System;
using System.Linq;
using ASFuelControl.Invoicing.Mapping;
using Exedron.Invoicing.Core;

namespace ASFuelControl.Invoicing
{
    public interface IInvoiceSubmissionSource
    {
        // Called synchronously under the document lease, only for a first issue or an
        // explicitly authorized retry of a definitive rejection. Must gate legacy history.
        InvoiceBuildRequest Prepare(Guid invoiceId, ProviderRoute route);
    }

    public sealed class DatabaseInvoiceSource : IInvoiceSubmissionSource
    {
        private readonly Func<Data.DatabaseModel> open;
        private readonly Func<Data.DatabaseModel, Data.Invoice, MappingContext> settings;
        public DatabaseInvoiceSource(Func<Data.DatabaseModel> open,
            Func<Data.DatabaseModel, Data.Invoice, MappingContext> settings)
        {
            this.open = open ?? throw new ArgumentNullException(nameof(open));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }
        public InvoiceBuildRequest Prepare(Guid invoiceId, ProviderRoute route)
        {
            using (var db = open())
            {
                var invoice = db.Invoices.SingleOrDefault(i => i.InvoiceId == invoiceId);
                if (invoice == null) throw new InvalidOperationException("The saved invoice was not found.");
                // Even a failed/empty legacy summary can represent an interrupted external call.
                EnsureNoLegacyHistory(invoice, db.MyDataInvoices.Any(i => i.InvoiceId == invoiceId));
                if (invoice.InvoiceType == null || !invoice.InvoiceType.SendToMyData)
                    throw new InvalidOperationException("The invoice type is not enabled for electronic submission.");
                var mapping = settings(db, invoice);
                if (mapping == null || mapping.Environment != (route.TestMode ? FiscalEnvironment.Test : FiscalEnvironment.Production))
                    throw new InvalidOperationException("Mapping evidence and provider route use different environments.");
                return new HostInvoiceMapper().Map(invoice, mapping);
            }
        }
        public static void EnsureNoLegacyHistory(Data.Invoice invoice, bool hasLegacySubmission, bool allowPrinted = false)
        {
            if (invoice == null) throw new ArgumentNullException(nameof(invoice));
            if (hasLegacySubmission || !string.IsNullOrWhiteSpace(invoice.InvoiceSignature) ||
                !string.IsNullOrWhiteSpace(invoice.QRCodeData))
                throw new InvalidOperationException("Υπάρχουν φορολογικά στοιχεία εκτός κοινής ροής. Ελέγξτε την προηγούμενη διαβίβαση / σήμανση πριν από νέα αποστολή.");
            if (invoice.IsPrinted == true && (!allowPrinted || invoice.Number <= 0))
                throw new InvalidOperationException("Το εκτυπωμένο παραστατικό χρειάζεται έλεγχο του αποθηκευμένου αριθμού και ρητή διαβίβαση. Δεν επιτρέπεται νέα αρίθμηση.");
        }
    }
}
