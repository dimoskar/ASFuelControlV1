using System;
using System.Globalization;
using System.Linq;
using Exedron.Invoicing.Core;
using Exedron.Invoicing.Adapters.Samtec;

namespace ASFuelControl.Invoicing
{
    // Host entities and rendering stay outside the portable signing library.
    public static class HostFiscalSigningMapper
    {
        public static FiscalSigningRequest Invoice(Data.Invoice invoice, FiscalSigningScope scope, string renderedText, bool hasLegacySubmission)
        {
            DatabaseInvoiceSource.EnsureNoLegacyHistory(invoice, hasLegacySubmission);
            if (invoice.InvoiceType == null || invoice.Number <= 0 || !invoice.TotalAmount.HasValue)
                throw new InvalidOperationException("A saved, numbered invoice with totals is required.");
            if (invoice.PaymentType != 0 && invoice.PaymentType != 1)
                throw new InvalidOperationException("Only explicit cash or credit is supported before payment integration.");
            var document = new FiscalDocumentReference(FiscalDocumentKind.Invoice, invoice.InvoiceId);
            int code = invoice.InvoiceType.OfficialEnumerator;
            var wire = SamtecRequestBuilder.Create(document, renderedText, code);
            var details = wire.Doc.InvoiceDetails;
            details.InvoiceNo = invoice.Number.ToString(CultureInfo.InvariantCulture);
            details.InvoiceSeries = invoice.Series;
            int sign = code == 175 ? -1 : 1;
            details.InvoiceTotal = sign * (float)invoice.TotalAmount.Value;
            var lines = invoice.InvoiceLines.OrderBy(l => l.InvoiceLineId).ToArray();
            if (lines.Length == 0 || lines.Any(l => l.InvoiceLineId == Guid.Empty || l.InvoiceId != invoice.InvoiceId) ||
                lines.Select(l => l.InvoiceLineId).Distinct().Count() != lines.Length)
                throw new InvalidOperationException("Owned invoice lines with unique saved identities are required.");
            if (code == 316)
            {
                if (lines.Length != 1 || lines[0].FuelType == null || lines[0].VolumeNormalized < 0)
                    throw new InvalidOperationException("Code 316 requires one fuel line with a nonnegative normalized volume.");
                details.PrintLine = "?" + lines[0].FuelType.EnumeratorValue.ToString(CultureInfo.InvariantCulture) + "!" +
                    (decimal.Truncate(lines[0].VolumeNormalized * 1000) / 1000).ToString("0.000", CultureInfo.InvariantCulture);
            }
            var trader = invoice.Trader;
            if (trader != null) wire.Doc.ReceiverHeader = new Receiverheader { TaxID = trader.TaxRegistrationNumber,
                PrintLine = string.Format("{0}\n{1}\n{2} {3} {4}\nΑΦΜ:{5}\n{6}", trader.Name, trader.Occupation,
                    trader.Address, trader.ZipCode, trader.City, trader.TaxRegistrationNumber, trader.TaxRegistrationOffice) };
            if (wire.InputType == 1)
            {
                wire.Doc.TransactionLines = lines.Select(l => {
                    decimal price = l.VatPercentage == 0 ? l.UnitPriceWhole : Math.Max(l.UnitPrice, l.UnitPriceRetail);
                    return new Transactionline { ClassCategory = 1, FuelCode = l.FuelType?.EnumeratorValue ?? 0,
                        Description = l.FuelType?.Name, ItemAmount = decimal.Round(price, decimal.Round(price, 3) == decimal.Round(price, 5) ? 3 : 5),
                        MeasurementUnit = 3, SaleQty = decimal.Round(l.Volume, 3), NetAmount = decimal.Round(l.TotalPrice - l.VatAmount, 2),
                        VatAmount = decimal.Round(l.VatAmount, 2), GrossAmount = sign * decimal.Round(l.TotalPrice, 2),
                        VatRate = l.VatPercentage.ToString("0.00", CultureInfo.InvariantCulture),
                        VatExCategory = l.VatPercentage == 0 && trader?.VatExemption == true ? 11 : 0,
                        DMType = l.DiscountAmount > 0 ? 1 : 0, DMValue = l.DiscountAmount > 0 ? sign * decimal.Round(l.DiscountAmount, 2) : 0 };
                }).ToArray();
                wire.Doc.Payments = new[] { new Payment { Type = invoice.PaymentType == 0 ? 5 : 0,
                    Value = (code == 169 || code == 175 ? -1 : 1) * decimal.Round(invoice.TotalAmount.Value, 2) } };
            }
            return SamtecFiscalSigner.Prepare(document, scope, wire);
        }
        public static FiscalSigningRequest Balance(Data.Balance balance, FiscalSigningScope scope, string renderedText)
        {
            if (balance == null) throw new ArgumentNullException(nameof(balance));
            if (balance.PrintDate.HasValue || !string.IsNullOrWhiteSpace(balance.DocumentSign))
                throw new InvalidOperationException("Legacy balance history requires reconciliation.");
            var document = new FiscalDocumentReference(FiscalDocumentKind.Balance, balance.BalanceId);
            return SamtecFiscalSigner.Prepare(document, scope, SamtecRequestBuilder.Create(document, renderedText, 501));
        }
        public static FiscalSigningRequest TankFilling(Data.TankFilling filling, FiscalSigningScope scope, string renderedText, int officialCode)
        {
            if (filling == null) throw new ArgumentNullException(nameof(filling));
            if (!string.IsNullOrWhiteSpace(filling.SignSignature)) throw new InvalidOperationException("Legacy filling history requires reconciliation.");
            var document = new FiscalDocumentReference(FiscalDocumentKind.TankFilling, filling.TankFillingId);
            return SamtecFiscalSigner.Prepare(document, scope, SamtecRequestBuilder.Create(document, renderedText, officialCode));
        }
    }
}
