using System.Collections.Generic;
using System.Linq;

using System;

namespace Exedron.Invoicing.Core
{
    public sealed class ProviderInvoiceValidationException : InvalidOperationException
    {
        public ProviderInvoiceValidationException(string message) : base(message) { }
    }

    public sealed class InvoiceTypeResolver : IInvoiceTypeResolver
    {
        public string Resolve(InvoiceInput invoice, CountryInput country, IEnumerable<InvoiceParent> parents,
            MovementContext tanker = null)
        {
            Require(invoice != null, "The invoice was not found.");
            Require(invoice.InvoiceSeries != null && invoice.InvoiceSeries.InvoiceType != null, "The invoice series or type is missing.");
            Require(invoice.Trader != null, "The invoice customer is missing.");
            Require(country != null, "The customer's country is missing or unknown.");
            var type = invoice.InvoiceSeries.InvoiceType;
            var linked = (parents ?? Enumerable.Empty<InvoiceParent>()).ToArray();
            if (type.InvoiceTypeMode == InvoiceMode.Credit)
            {
                foreach (var parent in linked)
                {
                    Require(parent != null && parent.Invoice != null && parent.Invoice.InvoiceSeries != null &&
                        parent.Invoice.InvoiceSeries.InvoiceType != null && parent.Invoice.Trader != null,
                        "A linked original invoice is missing required data.");
                    Require(parent.Invoice.InvoiceId != invoice.InvoiceId, "An invoice cannot credit itself.");
                    var parentType = parent.Invoice.InvoiceSeries.InvoiceType;
                    Require(parentType.InvoiceTypeMode == InvoiceMode.Normal &&
                        (parentType.MyDataDocumentKind == (int)MyDataDocumentKind.Goods || parentType.MyDataDocumentKind == (int)MyDataDocumentKind.Services),
                        "A credit invoice must reference original sales invoices, not credits, cancellations or standalone delivery notes.");
                    Require(parentType.IsRetail == type.IsRetail, "The credit invoice and its original invoices must agree on retail or wholesale classification.");
                    Require(string.Equals(Clean(parent.Invoice.Trader.TaxRegistrationId), Clean(invoice.Trader.TaxRegistrationId), StringComparison.OrdinalIgnoreCase) &&
                        (Clean(invoice.Trader.TaxRegistrationId) != null || parent.Invoice.TraderId == invoice.TraderId),
                        "The credit invoice customer must match every linked original invoice.");
                }
            }
            int validMarks = linked.Count(p => p != null && p.Transmission != null && p.Transmission.Status == 3 &&
                p.Transmission.Mark.GetValueOrDefault() > 0 && !p.Transmission.CancelationMark.HasValue && !p.Transmission.CanceledByMark.HasValue);
            var resolved = ResolveKind((MyDataDocumentKind)type.MyDataDocumentKind,
                type.InvoiceTypeMode, type.IsRetail, country.CountryCode, country.IsEu,
                type.InvoiceTypeMode == InvoiceMode.Credit ? linked.Length : 0,
                type.InvoiceTypeMode == InvoiceMode.Credit ? validMarks : 0);
            // A customer delivery linked to the tanker's loading MARK is correlated
            // 9.1, regardless of provider or direct myDATA transmission channel.
            if (resolved == "9.3" && tanker?.Role == "Delivery")
            {
                Require(tanker.ParentMark.GetValueOrDefault() > 0, "The parent tanker document has no accepted MARK.");
                return "9.1";
            }
            return resolved;
        }

        private static string ResolveKind(MyDataDocumentKind kind, InvoiceMode mode,
            bool isRetail, string countryCode, bool isEu, int parentCount, int validMarkCount)
        {
            if (mode == InvoiceMode.Cancel)
                throw new ProviderInvoiceValidationException("Cancellation documents cannot be issued as credit invoices. Cancel the original document through the cancellation procedure.");
            if (mode != InvoiceMode.Normal && mode != InvoiceMode.Credit)
                throw new ProviderInvoiceValidationException("The invoice mode is not supported.");
            if (kind == MyDataDocumentKind.Unspecified || !Enum.IsDefined(typeof(MyDataDocumentKind), kind))
                throw new ProviderInvoiceValidationException("Select the myDATA document kind in Invoice Types before issuing this document.");
            if (string.IsNullOrWhiteSpace(countryCode))
                throw new ProviderInvoiceValidationException("The customer's country is missing or unknown.");
            if (parentCount < 0 || validMarkCount < 0 || validMarkCount > parentCount)
                throw new ProviderInvoiceValidationException("Invalid credit-invoice correlation data.");

            if (mode == InvoiceMode.Credit)
            {
                if (kind == MyDataDocumentKind.Delivery || kind == MyDataDocumentKind.ConsolidatedDelivery || kind == MyDataDocumentKind.CorrelatedQuantityReceipt)
                    throw new ProviderInvoiceValidationException("A standalone delivery document cannot be issued as a financial credit invoice.");
                if (parentCount != validMarkCount)
                    throw new ProviderInvoiceValidationException("Every linked original invoice must have a successful, uncancelled myDATA MARK before issuing the credit invoice.");
                return isRetail ? "11.4" : parentCount > 0 ? "5.1" : "5.2";
            }

            if (kind == MyDataDocumentKind.Delivery) return "9.3";
            if (kind == MyDataDocumentKind.ConsolidatedDelivery) return "9.2";
            if (kind == MyDataDocumentKind.CorrelatedQuantityReceipt) return "10.1";
            if (isRetail) return kind == MyDataDocumentKind.Goods ? "11.1" : "11.2";
            string prefix = kind == MyDataDocumentKind.Goods ? "1." : "2.";
            return prefix + (string.Equals(countryCode, "GR", StringComparison.OrdinalIgnoreCase) ? "1" : isEu ? "2" : "3");
        }

        private static string Clean(string value) { return string.IsNullOrWhiteSpace(value) ? null : value.Trim(); }
        private static void Require(bool condition, string message) { if (!condition) throw new ProviderInvoiceValidationException(message); }
    }
}
