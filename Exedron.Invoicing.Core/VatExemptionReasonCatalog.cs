using System;
using System.Collections.Generic;
using System.Globalization;

namespace Exedron.Invoicing.Core
{
    public sealed class VatExemptionReasonOption
    {
        public VatExemptionReasonOption(int code, string description)
        {
            Code = code;
            Description = description;
            StoredValue = code == 0 ? string.Empty : code.ToString(CultureInfo.InvariantCulture) + " - " + description;
            DisplayText = code == 0 ? description : code.ToString(CultureInfo.InvariantCulture) + " — " + description;
        }

        public int Code { get; private set; }
        public string Description { get; private set; }
        public string StoredValue { get; private set; }
        public string DisplayText { get; private set; }
    }

    /// <summary>
    /// VAT exemption reasons enabled for operator selection.
    /// The persisted value intentionally contains both the myDATA category code and printable
    /// description because the legacy Trader schema only has a VatExemptionReason text column.
    /// </summary>
    public static class VatExemptionReasonCatalog
    {
        private static readonly VatExemptionReasonOption[] selectable =
        {
            new VatExemptionReasonOption(11, "Αλιευτικό παράκτιας αλιείας"),
        };

        public static IList<VatExemptionReasonOption> ForSelection()
        {
            var result = new List<VatExemptionReasonOption>
            {
                new VatExemptionReasonOption(0, "— Επιλέξτε λόγο απαλλαγής ΦΠΑ —")
            };
            result.AddRange(selectable);
            return result;
        }

        public static bool TryGetCode(string storedValue, out int code)
        {
            code = 0;
            if (string.IsNullOrWhiteSpace(storedValue)) return false;
            string value = storedValue.Trim();
            int separator = value.IndexOfAny(new[] { '-', '—', ':', ' ' });
            string codeText = separator < 0 ? value : value.Substring(0, separator);
            return int.TryParse(codeText, NumberStyles.Integer, CultureInfo.InvariantCulture, out code) && code >= 1 && code <= 31;
        }

        public static int GetCodeOrDefault(string storedValue, int defaultCode)
        {
            int code;
            return TryGetCode(storedValue, out code) ? code : defaultCode;
        }
    }
}
