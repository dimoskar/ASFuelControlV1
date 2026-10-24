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
        /// Binds the staged coupon to the real pump-sale invoice.  The remote query
        /// supplies the authoritative fuel and litre entitlement; an incompatible
        /// sale is released before normal invoice processing can continue.
        /// </summary>
        internal static void AttachToInvoice(Guid dispenserId, Guid invoiceId)
        {
            var pending = Get(dispenserId);
            if (pending == null || invoiceId == Guid.Empty) return;

            try
            {
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
                if (!SaleMatchesCoupon(invoiceId, reserved, out problem))
                {
                    Logging.Logger.Instance.LogToFile("myDiesel next sale", problem + " Το κουπόνι αποδεσμεύεται.");
                    service.Release(reserved.CouponCaseId);
                    return;
                }

                ApplyExciseDiscount(invoiceId, reserved.ExciseDiscountPerLitre);
            }
            finally
            {
                // A staged coupon belongs to one physical sale only. Never allow a
                // failed attempt to leak into the following customer's transaction.
                Clear(dispenserId);
            }
        }

        private static bool SaleMatchesCoupon(Guid invoiceId, Invoicing.Persistence.MyDieselCouponCaseRecord coupon, out string problem)
        {
            using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                var invoice = db.Invoices.Single(value => value.InvoiceId == invoiceId);
                decimal litres = invoice.InvoiceLines.Sum(value => Math.Abs(value.Volume));
                if (litres <= 0)
                {
                    problem = "Το παραστατικό δεν περιέχει θετική ποσότητα σε λίτρα.";
                    return false;
                }
                if (coupon.IssuedLitres.HasValue && litres > coupon.IssuedLitres.Value)
                {
                    problem = "Τα λίτρα της πώλησης υπερβαίνουν το όριο του κουπονιού.";
                    return false;
                }
                if (!coupon.FuelCode.HasValue || invoice.InvoiceLines.Any(value =>
                    value.FuelType == null || value.FuelType.EnumeratorValue != coupon.FuelCode.Value))
                {
                    problem = "Το καύσιμο της πώλησης δεν συμφωνεί με το επιλέξιμο καύσιμο του κουπονιού.";
                    return false;
                }
            }
            problem = null;
            return true;
        }

        private static void ApplyExciseDiscount(Guid invoiceId, decimal rate)
        {
            rate = MyDieselTaxRules.NormalizeExciseDiscountPerLitre(rate);
            using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                var invoice = db.Invoices.Single(value => value.InvoiceId == invoiceId);
                decimal retailDiscount = 0;
                decimal wholesaleDiscount = 0;
                decimal vatAmount = 0;

                foreach (var line in invoice.InvoiceLines)
                {
                    decimal lineRetail = Math.Min(Math.Round(Math.Abs(line.Volume) * rate, 2), Math.Abs(line.TotalPrice));
                    decimal vatMultiplier = 1 + line.VatPercentage / 100;
                    decimal lineWholesale = Math.Round(lineRetail / vatMultiplier, 2);
                    line.DiscountAmountRetail = lineRetail;
                    line.DiscountAmountWhole = lineWholesale;
                    line.DiscountAmount = lineWholesale;
                    line.TotalPrice = Math.Round(line.TotalPrice - lineRetail, 2);
                    line.VatAmount = Math.Round(line.TotalPrice - line.TotalPrice / vatMultiplier, 2);
                    retailDiscount += lineRetail;
                    wholesaleDiscount += lineWholesale;
                    vatAmount += line.VatAmount;
                    if (line.SalesTransaction != null)
                        line.SalesTransaction.TotalPrice = line.TotalPrice;
                }

                invoice.DiscountAmountRetail = retailDiscount;
                invoice.DiscountAmountWhole = wholesaleDiscount;
                invoice.DiscountAmount = wholesaleDiscount;
                invoice.TotalAmount = Math.Round(invoice.TotalAmount.GetValueOrDefault() - retailDiscount, 2);
                invoice.VatAmount = vatAmount;
                invoice.NettoAfterDiscount = invoice.TotalAmount.GetValueOrDefault() - vatAmount;
                db.SaveChanges();
            }
        }
    }
}
