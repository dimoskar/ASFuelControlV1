using System;
using System.Collections.Generic;
using System.Linq;
using ASFuelControl.Invoicing;
using Exedron.Invoicing.Core;

namespace ASFuelControl.Windows.Threads
{
    internal sealed class PendingMyDieselSelection
    {
        internal PendingMyDieselSelection(MyDieselCouponIdentity identity, MyDieselQrCoupon qr)
        {
            Identity = identity ?? throw new ArgumentNullException(nameof(identity));
            Qr = qr;
        }

        internal MyDieselCouponIdentity Identity { get; private set; }
        internal MyDieselQrCoupon Qr { get; private set; }
    }

    /// <summary>
    /// A coupon selected before a pump sale cannot yet be persisted as a coupon case,
    /// because the case must reference a real invoice. Keep only its normalized,
    /// non-secret identity until the sale creates that invoice.
    /// </summary>
    internal static class PendingMyDieselSelectionStore
    {
        private static readonly object SyncRoot = new object();
        private static readonly Dictionary<Guid, PendingMyDieselSelection> Values =
            new Dictionary<Guid, PendingMyDieselSelection>();

        internal static PendingMyDieselSelection Get(Guid dispenserId)
        {
            lock (SyncRoot)
            {
                PendingMyDieselSelection value;
                return Values.TryGetValue(dispenserId, out value) ? value : null;
            }
        }

        internal static void Set(Guid dispenserId, MyDieselCouponIdentity identity, MyDieselQrCoupon qr)
        {
            if (dispenserId == Guid.Empty) throw new ArgumentException("Dispenser ID is required.", nameof(dispenserId));
            lock (SyncRoot) Values[dispenserId] = new PendingMyDieselSelection(identity, qr);
        }

        internal static void Clear(Guid dispenserId)
        {
            lock (SyncRoot) Values.Remove(dispenserId);
        }

        /// <summary>
        /// Binds the staged coupon to the real pump-sale invoice. After the remote
        /// reservation, recheck the local sale before applying the EFK discount.
        /// Coupon issued litres are not a blocking limit: they cap the litres that
        /// receive the discount, while myDiesel verifies the actual invoice litres
        /// asynchronously through myDATA.
        /// </summary>
        internal static void AttachToInvoice(Guid dispenserId, Guid invoiceId)
        {
            var pending = Get(dispenserId);
            if (pending == null || invoiceId == Guid.Empty) return;

            try
            {
                string eligibilityProblem = SharedMyDieselRuntime.InvoiceEligibilityProblem(invoiceId);
                if (eligibilityProblem != null)
                {
                    Logging.Logger.Instance.LogToFile("myDiesel next sale",
                        eligibilityProblem + " Το κουπόνι δεν δεσμεύτηκε και το παραστατικό συνεχίζει κανονικά.");
                    return;
                }

                using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
                {
                    var invoice = db.Invoices.Single(value => value.InvoiceId == invoiceId);
                    string traderVat = invoice.Trader == null
                        ? null
                        : MyDieselCouponIdentity.NormalizeVat(invoice.Trader.TaxRegistrationNumber, "ΑΦΜ πελάτη");
                    if (traderVat != pending.Identity.BeneficiaryVat)
                    {
                        Logging.Logger.Instance.LogToFile("myDiesel next sale",
                            "Το ΑΦΜ του παραστατικού δεν συμφωνεί με το staged κουπόνι. Το κουπόνι δεν δεσμεύτηκε.");
                        return;
                    }
                }

                var configuration = SharedInvoicingSettings.LoadRuntime();
                var service = SharedMyDieselRuntime.CreateService(configuration);
                var scope = SharedMyDieselRuntime.NewReservationScope(configuration);
                decimal rate = configuration.MyDiesel.ExciseDiscountPerLitre;
                var coupon = pending.Qr == null
                    ? service.Capture(invoiceId, pending.Identity, scope, null, null, rate)
                    : service.Capture(invoiceId, pending.Qr, scope, rate);
                var reservation = service.Reserve(coupon.CouponCaseId);
                var reserved = reservation.CouponCase;
                if (reserved == null || reserved.LocalStage != MyDieselCaseStage.Reserved ||
                    MyDieselCouponStates.Parse(reserved.RemoteStatus) != MyDieselCouponStatus.Reserved)
                    return;

                string problem;
                if (!SaleRemainsEligible(invoiceId, out problem))
                {
                    Logging.Logger.Instance.LogToFile("myDiesel next sale", problem + " Το κουπόνι αποδεσμεύεται.");
                    service.Release(reserved.CouponCaseId);
                    return;
                }
                if (!reserved.IssuedLitres.HasValue || reserved.IssuedLitres.Value <= 0)
                {
                    Logging.Logger.Instance.LogToFile("myDiesel next sale",
                        "Δεν επιστράφηκαν έγκυρα λίτρα κουπονιού. Το κουπόνι αποδεσμεύεται.");
                    service.Release(reserved.CouponCaseId);
                    return;
                }

                ApplyExciseDiscount(invoiceId, reserved.IssuedLitres.Value, reserved.ExciseDiscountPerLitre);
            }
            finally
            {
                // A staged coupon belongs to one physical sale only. Never allow a
                // failed attempt to leak into the following customer's transaction.
                Clear(dispenserId);
            }
        }

        internal static bool SaleRemainsEligible(Guid invoiceId, out string problem)
        {
            using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                var invoice = db.Invoices.Single(value => value.InvoiceId == invoiceId);
                if (invoice.InvoiceLines.Any(value => value.Volume <= 0))
                {
                    problem = "Το παραστατικό δεν περιέχει θετική ποσότητα σε λίτρα.";
                    return false;
                }
                var fuelCodes = invoice.InvoiceLines.Select(value => value.FuelType == null
                    ? (int?)null
                    : value.FuelType.EnumeratorValue).ToArray();
                if (fuelCodes.Any(value => !value.HasValue || !MyDieselFuelRules.IsEligible(value.Value)))
                {
                    problem = "Το επιλεγμένο καύσιμο δεν είναι επιλέξιμο για myDiesel.";
                    return false;
                }
                if (fuelCodes.Select(value => value.Value).Distinct().Count() != 1)
                {
                    problem = "Το παραστατικό πρέπει να περιέχει ένα μόνο επιλέξιμο καύσιμο myDiesel.";
                    return false;
                }
            }
            problem = null;
            return true;
        }

        internal static void ApplyExciseDiscount(Guid invoiceId, decimal voucherLitres, decimal rate)
        {
            if (voucherLitres <= 0) throw new ArgumentOutOfRangeException(nameof(voucherLitres));
            rate = MyDieselTaxRules.NormalizeExciseDiscountPerLitre(rate);
            using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                var invoice = db.Invoices.Single(value => value.InvoiceId == invoiceId);
                var lines = invoice.InvoiceLines.ToArray();
                decimal invoiceLitres = lines.Sum(value => Math.Abs(value.Volume));
                decimal discountedLitres = Math.Min(voucherLitres, invoiceLitres);
                decimal remainingLitres = discountedLitres;
                decimal retailDiscount = 0;
                decimal wholesaleDiscount = 0;
                decimal vatAmount = 0;
                decimal totalAmount = 0;

                foreach (var line in lines)
                {
                    decimal originalTotal = Math.Abs(line.TotalPrice);
                    decimal applicableLitres = Math.Min(Math.Abs(line.Volume), remainingLitres);
                    decimal lineRetail = applicableLitres <= 0
                        ? 0
                        : MyDieselTaxRules.CalculateVatInclusiveExciseDiscount(
                            applicableLitres, applicableLitres, rate, line.VatPercentage);
                    decimal lineWholesale = MyDieselTaxRules.CalculateNetDiscountFromGross(
                        lineRetail, line.VatPercentage);
                    if (lineRetail > originalTotal)
                    {
                        lineRetail = originalTotal;
                        lineWholesale = MyDieselTaxRules.CalculateNetDiscountFromGross(
                            lineRetail, line.VatPercentage);
                    }
                    remainingLitres -= applicableLitres;
                    decimal vatMultiplier = 1 + line.VatPercentage / 100;
                    decimal discountPercentage = originalTotal == 0 ? 0 : Math.Round(100 * lineRetail / originalTotal, 4);
                    line.DiscountAmountRetail = lineRetail;
                    line.DiscountAmountWhole = lineWholesale;
                    line.DiscountAmount = lineRetail;
                    line.DiscountPercentage = discountPercentage;
                    line.TotalPrice = Math.Round(line.TotalPrice - lineRetail, 2);
                    line.VatAmount = Math.Round(line.TotalPrice - line.TotalPrice / vatMultiplier, 2);
                    retailDiscount += lineRetail;
                    wholesaleDiscount += lineWholesale;
                    vatAmount += line.VatAmount;
                    totalAmount += line.TotalPrice;
                    if (line.SalesTransaction != null)
                    {
                        line.SalesTransaction.TotalPrice = line.TotalPrice;
                        line.SalesTransaction.DiscountPercentage = discountPercentage;
                    }
                }

                invoice.DiscountAmountRetail = retailDiscount;
                invoice.DiscountAmountWhole = wholesaleDiscount;
                invoice.DiscountAmount = retailDiscount;
                invoice.TotalAmount = Math.Round(totalAmount, 2);
                invoice.VatAmount = vatAmount;
                invoice.NettoAfterDiscount = invoice.TotalAmount.GetValueOrDefault() - vatAmount;
                db.SaveChanges();
            }
        }

        /// <summary>
        /// Makes the reserved coupon the source of truth immediately before fiscal
        /// mapping. Invoice editors and legacy RecalculateInvoice implementations can
        /// rebuild the percentage fields after reservation; restore the authoritative
        /// net EFK amount and its corresponding VAT without subtracting either twice.
        /// </summary>
        internal static void ReconcileExciseDiscount(Guid invoiceId, decimal voucherLitres, decimal rate)
        {
            if (voucherLitres <= 0) throw new ArgumentOutOfRangeException(nameof(voucherLitres));
            rate = MyDieselTaxRules.NormalizeExciseDiscountPerLitre(rate);
            using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                var invoice = db.Invoices.Single(value => value.InvoiceId == invoiceId);
                var lines = invoice.InvoiceLines.ToArray();
                decimal invoiceLitres = lines.Sum(line => Math.Abs(line.Volume));
                decimal remainingLitres = Math.Min(voucherLitres, invoiceLitres);
                decimal retailDiscount = 0;
                decimal wholesaleDiscount = 0;
                decimal totalAmount = 0;
                decimal vatAmount = 0;
                decimal netBeforeDiscount = 0;
                decimal netAfterDiscount = 0;

                foreach (var line in lines)
                {
                    decimal unitPrice = line.UnitPriceRetail != 0
                        ? Math.Abs(line.UnitPriceRetail)
                        : Math.Abs(line.UnitPrice);
                    decimal originalTotal = Math.Round(Math.Abs(line.Volume) * unitPrice, 2);
                    if (originalTotal <= 0)
                        originalTotal = Math.Round(Math.Abs(line.TotalPrice) +
                            Math.Abs(line.DiscountAmountRetail), 2);

                    decimal applicableLitres = Math.Min(Math.Abs(line.Volume), remainingLitres);
                    decimal lineRetail = applicableLitres <= 0
                        ? 0
                        : MyDieselTaxRules.CalculateVatInclusiveExciseDiscount(
                            applicableLitres, applicableLitres, rate, line.VatPercentage);
                    decimal lineWholesale = MyDieselTaxRules.CalculateNetDiscountFromGross(
                        lineRetail, line.VatPercentage);
                    if (lineRetail > originalTotal)
                    {
                        lineRetail = originalTotal;
                        lineWholesale = MyDieselTaxRules.CalculateNetDiscountFromGross(
                            lineRetail, line.VatPercentage);
                    }
                    remainingLitres -= applicableLitres;

                    decimal vatMultiplier = 1 + line.VatPercentage / 100;
                    decimal discountedTotal = Math.Round(originalTotal - lineRetail, 2);
                    decimal lineVat = Math.Round(discountedTotal - discountedTotal / vatMultiplier, 2,
                        MidpointRounding.ToEven);
                    decimal discountPercentage = originalTotal == 0
                        ? 0
                        : Math.Round(100 * lineRetail / originalTotal, 4);

                    line.DiscountAmountRetail = lineRetail;
                    line.DiscountAmountWhole = lineWholesale;
                    line.DiscountAmount = lineRetail;
                    line.DiscountPercentage = discountPercentage;
                    line.TotalPrice = discountedTotal;
                    line.VatAmount = lineVat;
                    if (line.SalesTransaction != null)
                    {
                        line.SalesTransaction.TotalPrice = discountedTotal;
                        line.SalesTransaction.DiscountPercentage = discountPercentage;
                    }

                    retailDiscount += lineRetail;
                    wholesaleDiscount += lineWholesale;
                    totalAmount += discountedTotal;
                    vatAmount += lineVat;
                    netBeforeDiscount += discountedTotal - lineVat + lineWholesale;
                    netAfterDiscount += discountedTotal - lineVat;
                }

                invoice.DiscountAmountRetail = Math.Round(retailDiscount, 2);
                invoice.DiscountAmountWhole = Math.Round(wholesaleDiscount, 2);
                invoice.DiscountAmount = Math.Round(retailDiscount, 2);
                invoice.TotalAmount = Math.Round(totalAmount, 2);
                invoice.VatAmount = Math.Round(vatAmount, 2);
                invoice.NettoAmount = Math.Round(netBeforeDiscount, 2);
                invoice.NettoAfterDiscount = Math.Round(netAfterDiscount, 2);
                db.SaveChanges();
            }
        }

        internal static void ClearExciseDiscount(Guid invoiceId)
        {
            using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                var invoice = db.Invoices.Single(value => value.InvoiceId == invoiceId);
                decimal totalAmount = 0;
                decimal vatAmount = 0;
                decimal netAmount = 0;

                foreach (var line in invoice.InvoiceLines)
                {
                    decimal discountRetail = Math.Abs(line.DiscountAmountRetail);
                    decimal restoredTotal = Math.Round(line.TotalPrice + discountRetail, 2);
                    decimal retailUnitPrice = line.UnitPriceRetail != 0 ? line.UnitPriceRetail : line.UnitPrice;
                    decimal expectedTotal = Math.Round(Math.Abs(line.Volume) * Math.Abs(retailUnitPrice), 2);

                    // A build with the old invoice recalculation could persist the undiscounted
                    // line total while retaining the coupon discount. Prefer the independently
                    // calculated gross total when adding the discount back would overstate it.
                    if (expectedTotal > 0 && Math.Abs(restoredTotal - expectedTotal) > 0.01m)
                        restoredTotal = expectedTotal;

                    decimal vatMultiplier = 1 + line.VatPercentage / 100;
                    decimal lineVat = vatMultiplier == 0
                        ? 0
                        : Math.Round(restoredTotal - restoredTotal / vatMultiplier, 2);

                    line.DiscountAmountRetail = 0;
                    line.DiscountAmountWhole = 0;
                    line.DiscountAmount = 0;
                    line.DiscountPercentage = 0;
                    line.TotalPrice = restoredTotal;
                    line.VatAmount = lineVat;

                    totalAmount += restoredTotal;
                    vatAmount += lineVat;
                    netAmount += restoredTotal - lineVat;

                    if (line.SalesTransaction != null)
                    {
                        line.SalesTransaction.TotalPrice = restoredTotal;
                        line.SalesTransaction.DiscountPercentage = 0;
                    }
                }

                invoice.DiscountAmountRetail = 0;
                invoice.DiscountAmountWhole = 0;
                invoice.DiscountAmount = 0;
                invoice.TotalAmount = Math.Round(totalAmount, 2);
                invoice.VatAmount = Math.Round(vatAmount, 2);
                invoice.NettoAmount = Math.Round(netAmount, 2);
                invoice.NettoAfterDiscount = Math.Round(netAmount, 2);
                db.SaveChanges();
            }
        }
    }
}
