using System;
using System.Globalization;
using System.Linq;
using Exedron.Invoicing.Core.Models;

namespace Exedron.Invoicing.Core
{
    // AADE v2.0.0 domain rules, shared by every transport and payment preflight.
    public static class InvoiceValidation
    {
        private static readonly int[] FuelCodes = { 10,11,12,13,14,15,20,21,30,31,32,33,34,35,36,37,38,40,41,42,43,44,50,60,61,70,71,72,999 };

        public static string TransmissionFuelCode(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            int code;
            Require(int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out code), "Invalid fuel code.");
            // Host product code 99 and codes above 100 denote other charges. Do not modify the
            // saved product/catalogue code; normalize only the transmitted model.
            if (code == 99 || code > 100) code = 999;
            Require(FuelCodes.Contains(code), "Unsupported AADE fuel code: " + value);
            return code.ToString(CultureInfo.InvariantCulture);
        }

        public static void Validate(InvoiceModel model)
        {
            Require(model?.InvoiceHeader != null && model.InvoiceDetails != null && model.InvoiceDetails.Length > 0 && model.InvoiceSummary != null,
                "An invoice header, lines and totals are required.");
            Require(!string.IsNullOrWhiteSpace(model.InvoiceHeader.Series) && model.InvoiceHeader.Series.Length <= 50,
                "The invoice series must contain 1 to 50 characters.");
            foreach (var line in model.InvoiceDetails)
            {
                Require(line != null, "An invoice line is missing.");
                var code = TransmissionFuelCode(line.FuelCode);
                Require(code == line.FuelCode, "Normalize the fuel code before serializing the invoice.");
                Require(code == null || model.InvoiceHeader.FuelInvoice, "Fuel codes require fuelInvoice=true.");
                Money(line.NetValue); Money(line.VATAmount);
                var category = (int)line.VATCategory;
                Require(category >= 1 && category <= 8, "Unsupported VAT category.");
                decimal rate = new[] { 24m,13m,6m,17m,9m,4m,0m,0m }[category - 1];
                // Saved gross-price calculations can differ by one cent from net
                // rounding; larger differences must be corrected before payment.
                Require(Math.Abs(line.VATAmount - decimal.Round(line.NetValue * rate / 100m, 2, MidpointRounding.AwayFromZero)) <= .01m,
                    "The VAT amount does not agree with its net amount and rate.");
                Require(category < 7 || line.VATAmount == 0, "Zero/outside-scope VAT must have a zero VAT amount.");
                Require(category != 7 || line.VATExemptionCategory != null, "Zero VAT requires an exemption reason.");
            }
            if (model.InvoiceHeader.FuelInvoice)
            {
                var charges = model.InvoiceDetails.Where(l => l.FuelCode == "999").ToArray();
                var fuels = model.InvoiceDetails.Where(l => !string.IsNullOrEmpty(l.FuelCode) && l.FuelCode != "999").ToArray();
                Require(fuels.Length > 0, "AADE 268: a fuel invoice requires an actual fuel line, not only other charges (999).");
                Require(charges.Length <= 1, "AADE 270: only one other-charges line (999) is allowed.");
                Require(charges.Length == 0 || charges[0].NetValue <= fuels.Sum(l => l.NetValue),
                    "AADE 269: other charges (999) cannot exceed the actual fuel net total.");
            }
            Money(model.InvoiceSummary.TotalNetValue); Money(model.InvoiceSummary.TotalVATAmount); Money(model.InvoiceSummary.TotalGrossValue);
        }
        private static void Money(decimal value) => Require(value >= 0 && value == decimal.Round(value,2), "Invoice amounts require non-negative values with at most two decimals.");
        private static void Require(bool condition, string message)
        { if (!condition) throw new ProviderInvoiceValidationException(message); }
    }
}
