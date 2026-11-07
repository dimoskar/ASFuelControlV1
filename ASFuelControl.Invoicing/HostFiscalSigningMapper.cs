using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Exedron.Invoicing.Core;
using Exedron.Invoicing.Adapters.Samtec;

namespace ASFuelControl.Invoicing
{
    // Host entities and rendering stay outside the portable signing library.
    public static class HostFiscalSigningMapper
    {
        public static FiscalSigningRequest Invoice(Data.Invoice invoice, FiscalSigningScope scope, string renderedText,
            bool hasLegacySubmission, string eftPosTID = null)
        {
            DatabaseInvoiceSource.EnsureNoLegacyHistory(invoice, hasLegacySubmission);
            if (invoice.InvoiceType == null || invoice.Number <= 0 || !invoice.TotalAmount.HasValue)
                throw new InvalidOperationException("A saved, numbered invoice with totals is required.");
            if (invoice.PaymentType != 0 && invoice.PaymentType != 1 && invoice.PaymentType != 2 &&
                invoice.PaymentType != 3 && invoice.PaymentType != 4)
                throw new InvalidOperationException("Only explicit cash, credit, fleet card, card or IRIS is supported by Samtec signing.");
            bool integratedPos = invoice.PaymentType == 2 || invoice.PaymentType == 4;
            if (integratedPos && string.IsNullOrWhiteSpace(eftPosTID))
                throw new InvalidOperationException("Samtec card/IRIS signing requires the EftPosTID application option.");
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
                wire.Doc.TransactionLines = lines.Select((l, index) => {
                    decimal price = SamtecVatInclusiveUnitPrice(l, lines.Length == 1);
                    // Quantity is a three-decimal physical measure. The Samtec
                    // contract does not impose that limit on the VAT-inclusive
                    // unit price, so retain the application's five-decimal price.
                    decimal itemAmount = decimal.Round(price, 5,
                        MidpointRounding.ToEven);
                    decimal quantity = decimal.Round(l.Volume, 3);
                    decimal grossAmount = decimal.Round(l.TotalPrice, 2);
                    decimal netAmount = decimal.Round(l.TotalPrice - l.VatAmount, 2);
                    if (HasDiscount(l))
                    {
                        decimal originalGross = decimal.Round(itemAmount * quantity, 2,
                            MidpointRounding.ToEven);
                        if (grossAmount > originalGross)
                            throw new InvalidOperationException(
                                "A discounted Samtec line cannot exceed its original VAT-inclusive unit price.");
                        // Samtec signs the final line arithmetic. Keep the physical
                        // quantity and fold the reduction into the VAT-inclusive
                        // unit price instead of emitting separate discount fields.
                        itemAmount = SamtecFiscalSigner.EffectiveVatInclusiveItemAmount(
                            grossAmount,quantity);
                    }
                    else
                        quantity = ReconcileSamtecSaleQuantity(l, itemAmount, quantity, grossAmount);
                    if (decimal.Round(itemAmount * quantity,2,MidpointRounding.ToEven) != grossAmount)
                        throw new InvalidOperationException(
                            "A Samtec line total does not match its VAT-inclusive item amount and quantity.");
                    int fuelCode = l.FuelType?.EnumeratorValue ?? 0;
                    return new Transactionline { LineNo = index + 1, ClassCategory = 1, FuelCode = fuelCode,
                        Code = fuelCode == 0 ? null : fuelCode.ToString(CultureInfo.InvariantCulture),
                        Description = l.FuelType?.Name, ItemAmount = itemAmount,
                        MeasurementUnit = 3, SaleQty = quantity, NetAmount = netAmount,
                        VatAmount = decimal.Round(l.VatAmount, 2), GrossAmount = sign * grossAmount,
                        VatRate = l.VatPercentage.ToString("0.00", CultureInfo.InvariantCulture),
                        VatExCategory = l.VatPercentage == 0 && trader?.VatExemption == true
                            ? VatExemptionReasonCatalog.GetCodeOrDefault(trader.VatExemptionReason, 11) : 0 };
                }).ToArray();
                wire.Doc.Payments = new[] { new Payment {
                    Type = integratedPos ? 1 : invoice.PaymentType == 0 || invoice.PaymentType == 3 ? 5 : 0,
                    EftposTID = integratedPos ? eftPosTID.Trim() : null,
                    EftposTransType = 0,
                    Value = (code == 169 || code == 175 ? -1 : 1) * decimal.Round(invoice.TotalAmount.Value, 2) } };
            }
            return SamtecFiscalSigner.Prepare(document, scope, wire);
        }

        private static bool HasDiscount(Data.InvoiceLine line)
        {
            return decimal.Round(Math.Abs(line.DiscountAmount),2,MidpointRounding.ToEven) > 0 ||
                decimal.Round(Math.Abs(line.DiscountAmountRetail),2,MidpointRounding.ToEven) > 0 ||
                decimal.Round(Math.Abs(line.DiscountAmountWhole),2,MidpointRounding.ToEven) > 0;
        }

        private static decimal SamtecVatInclusiveUnitPrice(Data.InvoiceLine line,
            bool singleLineInvoice)
        {
            // A dispenser sale anywhere in the transformation ancestry is the
            // physical source of truth. Do not use a possibly edited, recalculated,
            // or stale invoice-line price for it.
            var sale = RelatedSalesTransaction(line, singleLineInvoice);
            if (sale != null)
            {
                if (sale.UnitPrice <= 0)
                    throw new InvalidOperationException(
                        "A sale-linked Samtec line requires a positive SalesTransaction retail unit price.");
                return sale.UnitPrice;
            }

            // Samtec ItemAmount is VAT-inclusive even for wholesale documents
            // (for example official type 165). The form may display UnitPriceWhole,
            // but its coupled UnitPriceRetail is the wire value. Never choose a
            // larger/stale generic UnitPrice and never reconstruct retail from the
            // rounded wholesale value.
            if (line.VatPercentage == 0 && line.UnitPriceWhole > 0)
                return line.UnitPriceWhole;
            if (line.UnitPriceRetail > 0)
                return line.UnitPriceRetail;
            throw new InvalidOperationException(
                "A VAT-bearing Samtec line requires a positive stored VAT-inclusive UnitPriceRetail; it cannot use or reconstruct the wholesale unit price.");
        }

        private static Data.SalesTransaction RelatedSalesTransaction(Data.InvoiceLine line,
            bool singleLineInvoice)
        {
            if (line == null)
                return null;
            if (line.SaleTransactionId.HasValue && line.SaleTransactionId.Value != Guid.Empty)
                return RequireLoadedSale(line);

            // An invoice total cannot identify which transformed line supplied a
            // price override. Multi-line transformations therefore keep their
            // explicitly saved line prices.
            if (!singleLineInvoice)
                return null;

            var sources = new Dictionary<Guid, Data.InvoiceLine>();
            var visited = new HashSet<Guid>();
            if (line.InvoiceLineId != Guid.Empty)
                visited.Add(line.InvoiceLineId);
            if (line.InvoiceLineRelations_ChildRelationId != null)
                foreach (var relation in line.InvoiceLineRelations_ChildRelationId)
                    CollectRelatedSalesTransactions(relation.InvoiceLine_ParentLineId, visited, sources);
            if (sources.Count == 0)
                return null;

            // Compare gross values before discount. This identifies legacy unit-price
            // adjustments that only reconciled the same invoice total, while a real
            // price edit changes the pre-discount total. Discount-only edits continue
            // to use the physical sale price before the reduction is folded into
            // the Samtec VAT-inclusive item amount.
            decimal targetBeforeDiscount = GrossBeforeDiscount(line);
            decimal sourceBeforeDiscount = decimal.Round(
                sources.Values.Sum(GrossBeforeDiscount), 2);
            decimal targetVolume = decimal.Round(Math.Abs(line.Volume), 3);
            decimal sourceVolume = decimal.Round(
                sources.Values.Sum(source => Math.Abs(source.Volume)), 3);
            if (targetBeforeDiscount != sourceBeforeDiscount ||
                targetVolume != sourceVolume)
                return null;
            var prices = sources.Values.Select(source =>
                RequireLoadedSale(source).UnitPrice).Distinct().ToArray();
            return prices.Length == 1 ? RequireLoadedSale(sources.Values.First()) : null;
        }

        private static Data.SalesTransaction RequireLoadedSale(Data.InvoiceLine line)
        {
            if (line.SalesTransaction == null)
                throw new InvalidOperationException(
                    "A sale-linked Samtec line requires its SalesTransaction to be loaded.");
            return line.SalesTransaction;
        }

        private static decimal GrossBeforeDiscount(Data.InvoiceLine line)
        {
            decimal discount = line.DiscountAmountRetail != 0
                ? line.DiscountAmountRetail
                : line.DiscountAmount;
            return decimal.Round(Math.Abs(line.TotalPrice) + Math.Abs(discount), 2);
        }

        private static void CollectRelatedSalesTransactions(Data.InvoiceLine line,
            ISet<Guid> visited, IDictionary<Guid, Data.InvoiceLine> sources)
        {
            if (line == null)
                return;
            if (line.InvoiceLineId != Guid.Empty && !visited.Add(line.InvoiceLineId))
                return;
            if (line.SaleTransactionId.HasValue && line.SaleTransactionId.Value != Guid.Empty)
            {
                RequireLoadedSale(line);
                sources[line.SaleTransactionId.Value] = line;
                return;
            }
            if (line.InvoiceLineRelations_ChildRelationId == null)
                return;
            foreach (var relation in line.InvoiceLineRelations_ChildRelationId)
                CollectRelatedSalesTransactions(relation.InvoiceLine_ParentLineId, visited, sources);
        }

        private static decimal ReconcileSamtecSaleQuantity(Data.InvoiceLine line,
            decimal itemAmount, decimal quantity, decimal grossAmount)
        {
            if (!IsSaleDerived(line) || itemAmount <= 0 || quantity <= 0 ||
                decimal.Round(line.DiscountAmount, 2, MidpointRounding.ToEven) != 0 ||
                decimal.Round(line.DiscountAmountRetail, 2, MidpointRounding.ToEven) != 0)
                return quantity;

            decimal calculated = decimal.Round(itemAmount * quantity, 2,
                MidpointRounding.ToEven);
            decimal difference = grossAmount - calculated;
            if (difference == 0)
                return quantity;

            // The dispenser's total is authoritative for a physical sale. Some
            // dispenser protocols calculate it with greater precision than
            // the three-decimal quantity exposed to us. Adjust only the Samtec wire
            // quantity. Among all millilitre candidates that reconcile at two
            // decimals, choose the raw product closest to the saved gross amount
            // instead of accepting the first candidate that happens to round.
            // The saved invoice line and its SalesTransaction remain untouched.
            decimal source = decimal.Round(quantity, 3, MidpointRounding.ToEven);
            decimal? bestAny = null;
            decimal bestAnyError = decimal.MaxValue;
            int bestAnyOffset = int.MaxValue;
            for (int offset = -20; offset <= 20; offset++)
            {
                if (offset == 0)
                    continue;
                decimal candidate = source + offset * 0.001m;
                if (candidate <= 0)
                    continue;
                decimal product = candidate * itemAmount;
                if (decimal.Round(product, 2, MidpointRounding.ToEven) != grossAmount)
                    continue;

                decimal error = Math.Abs(product - grossAmount);
                int absoluteOffset = Math.Abs(offset);
                if (error < bestAnyError ||
                    (error == bestAnyError && absoluteOffset < bestAnyOffset))
                {
                    bestAny = candidate;
                    bestAnyError = error;
                    bestAnyOffset = absoluteOffset;
                }
            }
            return bestAny ?? quantity;
        }
        private static bool IsSaleDerived(Data.InvoiceLine line)
        {
            return IsSaleDerived(line, new HashSet<Guid>());
        }
        private static bool IsSaleDerived(Data.InvoiceLine line, ISet<Guid> visited)
        {
            if (line == null)
                return false;
            if (line.SaleTransactionId.HasValue && line.SaleTransactionId.Value != Guid.Empty)
                return true;
            if (line.InvoiceLineId != Guid.Empty && !visited.Add(line.InvoiceLineId))
                return false;
            return line.InvoiceLineRelations_ChildRelationId != null &&
                line.InvoiceLineRelations_ChildRelationId.Any(r =>
                    IsSaleDerived(r.InvoiceLine_ParentLineId, visited));
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
