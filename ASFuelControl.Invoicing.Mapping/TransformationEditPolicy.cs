using System;
using System.Linq;

namespace ASFuelControl.Invoicing.Mapping
{
    public static class TransformationEditPolicy
    {
        public static bool AllowUnitPriceEdit(bool sourceHasFinancialData,
            bool guideOnlyTarget, string supplierVat, string counterpartVat)
        {
            return !sourceHasFinancialData || guideOnlyTarget ||
                VatNumbersEqual(supplierVat, counterpartVat);
        }

        public static bool AllowVolumeEdit(bool oneToMany, bool guideOnlyTarget)
        {
            return oneToMany && !guideOnlyTarget;
        }

        public static bool RequiresLineSelection(bool oneToMany, bool oneToOne,
            bool sourceHasFinancialData, bool guideOnlyTarget, bool sameVat)
        {
            return oneToMany || (oneToOne && !sourceHasFinancialData) ||
                guideOnlyTarget || sameVat;
        }

        public static bool VatNumbersEqual(string supplierVat, string counterpartVat)
        {
            string supplier = NormalizeVat(supplierVat);
            string counterpart = NormalizeVat(counterpartVat);
            return supplier != null && counterpart != null &&
                string.Equals(supplier, counterpart, StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeVat(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;
            string normalized = new string(value.Where(char.IsLetterOrDigit).ToArray())
                .ToUpperInvariant();
            if ((normalized.StartsWith("EL", StringComparison.Ordinal) ||
                 normalized.StartsWith("GR", StringComparison.Ordinal)) &&
                normalized.Length > 2)
                normalized = normalized.Substring(2);
            return normalized.Length == 0 ? null : normalized;
        }
    }
}
