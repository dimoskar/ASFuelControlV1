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
            bool credit = type.InvoiceTypeMode == InvoiceMode.Credit;
            bool cancellation = type.InvoiceTypeMode == InvoiceMode.Cancel;
            if (credit || cancellation)
            {
                if (cancellation)
                    Require(linked.Length == 1,
                        "Το ακυρωτικό παραστατικό πρέπει να συνδέεται με ακριβώς ένα αρχικό παραστατικό.");
                foreach (var parent in linked)
                {
                    Require(parent != null && parent.Invoice != null && parent.Invoice.InvoiceSeries != null &&
                        parent.Invoice.InvoiceSeries.InvoiceType != null && parent.Invoice.Trader != null,
                        "Από το συνδεδεμένο αρχικό παραστατικό λείπουν απαραίτητα στοιχεία.");
                    Require(parent.Invoice.InvoiceId != invoice.InvoiceId,
                        "Το παραστατικό δεν μπορεί να συσχετιστεί ως πιστωτικό με τον εαυτό του.");
                    var parentType = parent.Invoice.InvoiceSeries.InvoiceType;
                    if (credit || cancellation)
                        Require(parentType.InvoiceTypeMode == InvoiceMode.Normal &&
                            (parentType.MyDataDocumentKind == (int)MyDataDocumentKind.Goods || parentType.MyDataDocumentKind == (int)MyDataDocumentKind.Services),
                            cancellation
                                ? "Το ακυρωτικό παραστατικό που διαβιβάζεται ως πιστωτικό πρέπει να συνδέεται με αρχικό παραστατικό πώλησης και όχι με άλλο πιστωτικό ή ακυρωτικό."
                                : "Το πιστωτικό παραστατικό πρέπει να συνδέεται με αρχικά παραστατικά πώλησης και όχι με πιστωτικά, ακυρωτικά ή αυτοτελή δελτία αποστολής.");
                    Require(parentType.IsRetail == type.IsRetail,
                        "Το πιστωτικό και όλα τα αρχικά παραστατικά πρέπει να έχουν τον ίδιο χαρακτηρισμό λιανικής ή χονδρικής.");
                    Require(string.Equals(Clean(parent.Invoice.Trader.TaxRegistrationId), Clean(invoice.Trader.TaxRegistrationId), StringComparison.OrdinalIgnoreCase) &&
                        (Clean(invoice.Trader.TaxRegistrationId) != null || parent.Invoice.TraderId == invoice.TraderId),
                        "Ο πελάτης του πιστωτικού πρέπει να συμφωνεί με τον πελάτη κάθε συνδεδεμένου αρχικού παραστατικού.");
                }
            }
            int validMarks = linked.Count(p => p != null && p.Transmission != null && p.Transmission.Status == 3 &&
                p.Transmission.Mark.GetValueOrDefault() > 0 && !p.Transmission.CancelationMark.HasValue && !p.Transmission.CanceledByMark.HasValue);
            bool usesForeignVatExemption = UsesForeignVatExemption(invoice, country);
            var resolved = ResolveKind((MyDataDocumentKind)type.MyDataDocumentKind,
                type.InvoiceTypeMode, type.IsRetail, country.CountryCode, country.IsEu,
                usesForeignVatExemption,
                credit || cancellation ? linked.Length : 0,
                credit || cancellation ? validMarks : 0);
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
            bool isRetail, string countryCode, bool isEu, bool usesForeignVatExemption,
            int parentCount, int validMarkCount)
        {
            if (mode != InvoiceMode.Normal && mode != InvoiceMode.Credit && mode != InvoiceMode.Cancel)
                throw new ProviderInvoiceValidationException("The invoice mode is not supported.");
            if (kind == MyDataDocumentKind.Unspecified || !Enum.IsDefined(typeof(MyDataDocumentKind), kind))
                throw new ProviderInvoiceValidationException("Select the myDATA document kind in Invoice Types before issuing this document.");
            if (string.IsNullOrWhiteSpace(countryCode))
                throw new ProviderInvoiceValidationException("The customer's country is missing or unknown.");
            if (parentCount < 0 || validMarkCount < 0 || validMarkCount > parentCount)
                throw new ProviderInvoiceValidationException(
                    "Τα στοιχεία συσχέτισης του πιστωτικού παραστατικού δεν είναι έγκυρα.");

            if (mode == InvoiceMode.Credit || mode == InvoiceMode.Cancel)
            {
                if (kind == MyDataDocumentKind.Delivery || kind == MyDataDocumentKind.ConsolidatedDelivery || kind == MyDataDocumentKind.CorrelatedQuantityReceipt)
                    throw new ProviderInvoiceValidationException(
                        "Ένα αυτοτελές δελτίο αποστολής δεν μπορεί να εκδοθεί ως οικονομικό πιστωτικό παραστατικό.");
                if (parentCount != validMarkCount)
                    throw new ProviderInvoiceValidationException(
                        "Πριν από την έκδοση του πιστωτικού, κάθε συνδεδεμένο αρχικό παραστατικό πρέπει να έχει επιτυχημένη διαβίβαση στο myDATA, έγκυρο MARK και να μην έχει ακυρωθεί.");
                if (mode == InvoiceMode.Cancel && parentCount != 1)
                    throw new ProviderInvoiceValidationException(
                        "Το ακυρωτικό παραστατικό πρέπει να συνδέεται με ακριβώς ένα αρχικό παραστατικό.");
                return isRetail ? "11.4" : parentCount > 0 ? "5.1" : "5.2";
            }

            if (kind == MyDataDocumentKind.Delivery) return "9.3";
            if (kind == MyDataDocumentKind.ConsolidatedDelivery) return "9.2";
            if (kind == MyDataDocumentKind.CorrelatedQuantityReceipt) return "10.1";
            if (isRetail) return kind == MyDataDocumentKind.Goods ? "11.1" : "11.2";
            string prefix = kind == MyDataDocumentKind.Goods ? "1." : "2.";
            bool foreignCountry = !string.Equals(countryCode, "GR",
                StringComparison.OrdinalIgnoreCase);
            if (!foreignCountry || !usesForeignVatExemption)
            {
                return prefix + "1";
            }

            return prefix + (isEu ? "2" : "3");
        }

        internal static bool UsesForeignVatExemption(InvoiceInput invoice,
            CountryInput country)
        {
            if (invoice == null || country == null ||
                string.Equals(country.CountryCode, "GR", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            InvoiceLineInput[] lines = (invoice.InvoiceLines ??
                new List<InvoiceLineInput>()).ToArray();
            if (lines.Length == 0)
            {
                return false;
            }

            return lines.All(line => line != null && line.VATPercentage == 0 &&
                line.VatAmount.HasValue && line.VatAmount.Value == 0 &&
                line.SalesItem != null &&
                line.SalesItem.VATExcemptionArticle.GetValueOrDefault() > 0);
        }

        private static string Clean(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new ProviderInvoiceValidationException(message);
            }
        }
    }
}
