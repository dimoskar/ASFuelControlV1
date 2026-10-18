using Exedron.Invoicing.Core.Models;

namespace Exedron.Invoicing.Core
{
    public static class MyDataPartyFields
    {
        // AADE ERP v2.0.0, section 5.1: supplyAccountNo belongs to the
        // recipient and is permitted for fuel invoices, independently of delivery.
        public static string SupplyAccountNumber(InvoiceModel invoice)
        {
            var value = invoice.InvoiceHeader.FuelInvoice ? invoice.CounterPart?.SupplyAccountNo : null;
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
