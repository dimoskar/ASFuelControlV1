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

        // Category 11 documents are transmitted as delivery notes even when the
        // host invoice type was not explicitly configured as one. Keep this as a
        // derived transmission value so the original invoice model remains intact.
        public static bool IsDeliveryNote(InvoiceModel invoice)
        {
            return invoice?.InvoiceHeader != null &&
                (invoice.InvoiceHeader.DeliveryNote || SpecialInvoiceCategory(invoice) == 11);
        }

        // myDATA requires movePurpose whenever isDeliveryNote is true. Older saved
        // heating invoices can carry the legacy None value because they were not
        // originally treated as delivery documents. Derive Sales only on the wire.
        public static MovePurposeEnum EffectiveMovePurpose(InvoiceModel invoice)
        {
            var header = invoice?.InvoiceHeader;
            if (header == null) return MovePurposeEnum.None;
            return IsDeliveryNote(invoice) && header.MovePurpose == MovePurposeEnum.None
                ? MovePurposeEnum.Sales
                : header.MovePurpose;
        }

        // Older heating invoices were stored before category 11 implied a delivery
        // document, so they may not contain an explicit delivery header. Complete a
        // transmission copy from the fiscal issuer/recipient snapshots.
        public static OtherDeliveryNoteHeader EffectiveDeliveryHeader(InvoiceModel invoice)
        {
            var header = invoice?.InvoiceHeader;
            if (header == null || !IsDeliveryNote(invoice)) return header?.OtherDeliveryNoteHeader;
            var saved = header.OtherDeliveryNoteHeader;
            var result = new OtherDeliveryNoteHeader
            {
                LoadingAddress = Address(saved?.LoadingAddress, invoice.Issuer?.Address),
                DeliveryAddress = Address(saved?.DeliveryAddress, invoice.CounterPart?.Address),
                StartShippingBranch = saved == null ? invoice.Issuer?.Branch ?? 0 : saved.StartShippingBranch,
                CompleteShippingBranch = saved == null ? invoice.CounterPart?.Branch ?? 0 : saved.CompleteShippingBranch
            };
            RequireAddress(result.LoadingAddress, "loading");
            RequireAddress(result.DeliveryAddress, "delivery");
            return result;
        }

        private static Address Address(Address preferred, Address fallback)
        {
            return new Address
            {
                Street = Value(preferred?.Street, fallback?.Street),
                Number = Value(preferred?.Number, fallback?.Number),
                PostalCode = Value(preferred?.PostalCode, fallback?.PostalCode),
                City = Value(preferred?.City, fallback?.City)
            };
        }

        private static string Value(string preferred, string fallback)
        {
            return !string.IsNullOrWhiteSpace(preferred) ? preferred : fallback;
        }

        private static void RequireAddress(Address address, string role)
        {
            if (address == null || string.IsNullOrWhiteSpace(address.Street) ||
                string.IsNullOrWhiteSpace(address.PostalCode) || string.IsNullOrWhiteSpace(address.City))
                throw new ProviderInvoiceValidationException(
                    "A category 11 delivery invoice requires a complete " + role +
                    " address (street, postal code and city). No invoice was submitted.");
        }
    }
}
