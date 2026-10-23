using System.Linq;
using Exedron.Invoicing.Core.Models;

namespace Exedron.Invoicing.Core
{
    public static class MyDataHeaderFields
    {
        // Derived only for transmission; never write this back into the invoice model.
        public static int? SpecialInvoiceCategory(InvoiceModel invoice)
        {
            var header = invoice?.InvoiceHeader;
            if (header == null || !header.FuelInvoice ||
                string.IsNullOrWhiteSpace(invoice.CounterPart?.SupplyAccountNo)) return null;
            switch (header.InvoiceType)
            {
                case "1.1": case "1.4": case "1.6": case "5.1": case "5.2": case "11.1": case "11.4":
                    return invoice.InvoiceDetails != null && invoice.InvoiceDetails.Any(line =>
                        line != null && (line.FuelCode == "30" || line.FuelCode == "31")) ? (int?)11 : null;
                default: return null;
            }
        }
    }
}
