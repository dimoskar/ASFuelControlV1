using System;
using System.Collections.Generic;
using System.Linq;
using Exedron.Invoicing.Core;

namespace ASFuelControl.Invoicing.Mapping
{
    /// <summary>
    /// Resolves the counterpart country from the saved country and, when present,
    /// the ISO-style prefix of its VAT number. Greece remains the default for
    /// unprefixed VAT numbers used by the legacy application.
    /// </summary>
    public static class TraderCountryResolver
    {
        public static CountryInput Resolve(IEnumerable<CountryInput> countries,
            string savedCountry, string vatNumber)
        {
            CountryInput[] catalogue = (countries ?? Enumerable.Empty<CountryInput>())
                .Where(value => value != null)
                .ToArray();
            Require(catalogue.Length > 0, "The country catalogue is empty.");

            string saved = Clean(savedCountry);
            CountryInput savedMatch = saved == null ? null : Find(catalogue, saved);
            string prefix = VatCountryPrefix(vatNumber);
            CountryInput prefixMatch = null;
            if (prefix != null)
            {
                string countryCode = string.Equals(prefix, "EL",
                    StringComparison.OrdinalIgnoreCase) ? "GR" : prefix;
                prefixMatch = Find(catalogue, countryCode);
            }

            CountryInput selected;
            if (prefixMatch != null)
            {
                // Quick-created legacy traders were always saved as GR. Treat GR
                // as the default selection and let an explicit foreign VAT prefix
                // correct it. A different non-Greek selection is a real conflict.
                if (savedMatch != null &&
                    !CodeEquals(savedMatch, "GR") &&
                    !CodeEquals(savedMatch, prefixMatch.CountryCode))
                    throw new ProviderInvoiceValidationException(
                        "The customer's VAT prefix does not agree with the selected country.");
                selected = prefixMatch;
            }
            else if (savedMatch != null)
                selected = savedMatch;
            else
            {
                string vat = Clean(vatNumber);
                Require(vat == null || vat.All(IsAsciiDigit),
                    "A foreign VAT number requires a recognized country prefix or an explicit country.");
                selected = Find(catalogue, "GR");
            }

            string code = Clean(selected.CountryCode);
            Require(code != null, "The selected country has no ISO code.");
            return new CountryInput
            {
                CountryCode = code.ToUpperInvariant(),
                DisplayMember = selected.DisplayMember,
                IsEu = selected.IsEu
            };
        }

        private static CountryInput Find(CountryInput[] countries, string value)
        {
            string key = Clean(value);
            Require(key != null, "The customer's country is missing.");
            CountryInput[] matches = countries
                .Where(country => string.Equals(Clean(country.CountryCode), key,
                    StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (matches.Length == 0)
                matches = countries
                    .Where(country => string.Equals(Clean(country.DisplayMember), key,
                        StringComparison.OrdinalIgnoreCase))
                    .ToArray();
            Require(matches.Length == 1,
                "The customer's country is unknown or ambiguous: " + key);
            Require(Clean(matches[0].CountryCode) != null,
                "The customer's country has no ISO code.");
            return matches[0];
        }

        private static string VatCountryPrefix(string vatNumber)
        {
            string vat = Clean(vatNumber);
            if (vat == null || vat.Length < 2 ||
                !IsAsciiLetter(vat[0]) || !IsAsciiLetter(vat[1]))
                return null;
            return vat.Substring(0, 2).ToUpperInvariant();
        }

        private static bool CodeEquals(CountryInput country, string code)
        {
            return string.Equals(Clean(country == null ? null : country.CountryCode),
                Clean(code), StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsAsciiLetter(char value)
        {
            return (value >= 'A' && value <= 'Z') ||
                (value >= 'a' && value <= 'z');
        }

        private static bool IsAsciiDigit(char value)
        {
            return value >= '0' && value <= '9';
        }

        private static string Clean(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new ProviderInvoiceValidationException(message);
        }
    }
}
