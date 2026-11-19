using System;
using System.Collections.Generic;
using System.Linq;
using Exedron.Invoicing.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ASFuelControl.Invoicing
{
    public sealed class MyDieselInvoiceCorrectionPlan
    {
        public Guid OriginalId { get; set; }
        public Guid CreditId { get; set; }
        public Guid ReplacementId { get; set; }
        public Guid CouponCaseId { get; set; }
        public Guid CreditTypeId { get; set; }
        public Guid ReplacementTypeId { get; set; }
        public Guid OriginalAttemptId { get; set; }
        public string CouponCode { get; set; }
        public string OriginalDescription { get; set; }
        public string CreditSeries { get; set; }
        public string ReplacementSeries { get; set; }
        public long OriginalMark { get; set; }
        public decimal CouponIssuedLitres { get; set; }
        public decimal ExciseDiscountPerLitre { get; set; }
        public InvoiceWorkflowSnapshot Original { get; set; }
        public JObject Host { get; set; }
        public JObject ReplacementHost { get; set; }

        public InvoicePaymentType CorrectivePaymentType()
        {
            if (Original == null || Original.Invoice == null ||
                Original.Invoice.Invoice == null)
            {
                throw new InvalidOperationException(
                    "The original invoice payment method is unavailable.");
            }
            InvoicePaymentType sourcePayment =
                Original.Invoice.Invoice.PaymentType;
            if (sourcePayment == InvoicePaymentType.Card)
            {
                return InvoicePaymentType.Credit;
            }
            return sourcePayment;
        }

        public static bool SameCreditProviderRoute(ProviderRoute original,
            ProviderRoute current)
        {
            if (original == null || current == null)
            {
                return original == null && current == null;
            }
            return original.ProviderKey == current.ProviderKey &&
                original.TestMode == current.TestMode &&
                original.IssuerVat == current.IssuerVat &&
                original.IssuerBranch == current.IssuerBranch &&
                original.Account == current.Account &&
                original.MyDataBaseUrl == current.MyDataBaseUrl;
        }

        public static bool SameCreditSigningRoute(FiscalSigningScope original,
            FiscalSigningScope current)
        {
            if (original == null || current == null)
            {
                return original == null && current == null;
            }

            // DeviceId identifies a saved Samtec profile revision. Saving the
            // same physical signer can create a new profile ID, so myDiesel
            // correction eligibility follows the stable issuer scope instead.
            return FiscalSigningCoordinator.SameIssuerScope(original, current);
        }

        public static T Copy<T>(T value)
        {
            string json = JsonConvert.SerializeObject(value);
            T copy = JsonConvert.DeserializeObject<T>(json);
            return copy;
        }

        public decimal ReplacementTotal()
        {
            if (ReplacementHost == null ||
                ReplacementHost["Invoice"] == null)
            {
                throw new InvalidOperationException(
                    "The recalculated replacement invoice is unavailable.");
            }

            return DecimalValue(
                ReplacementHost["Invoice"]["TotalAmount"]);
        }

        public static JObject RecalculateReplacementHost(JObject source,
            decimal voucherLitres, decimal rate)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (voucherLitres <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(voucherLitres));
            }

            decimal normalizedRate =
                MyDieselTaxRules.NormalizeExciseDiscountPerLitre(rate);
            JObject replacement = (JObject)source.DeepClone();
            JObject invoice = replacement["Invoice"] as JObject;
            JArray lines = replacement["Lines"] as JArray;
            if (invoice == null || lines == null || lines.Count == 0)
            {
                throw new InvalidOperationException(
                    "The source invoice has no persisted invoice lines.");
            }

            IEnumerable<JObject> invoiceLines = lines.OfType<JObject>();
            decimal invoiceLitres = invoiceLines.Sum(LineLitres);
            decimal remainingLitres = Math.Min(voucherLitres,
                invoiceLitres);
            decimal retailDiscount = 0;
            decimal wholesaleDiscount = 0;
            decimal totalAmount = 0;
            decimal vatAmount = 0;
            decimal netBeforeDiscount = 0;
            decimal netAfterDiscount = 0;

            foreach (JObject line in lines.OfType<JObject>())
            {
                decimal volume = LineLitres(line);
                decimal originalTotal = OriginalLineTotal(line, volume);
                decimal applicableLitres = Math.Min(volume,
                    remainingLitres);
                decimal vatPercentage = DecimalValue(
                    line["VatPercentage"]);
                decimal lineRetail = applicableLitres <= 0
                    ? 0
                    : MyDieselTaxRules.CalculateVatInclusiveExciseDiscount(
                        applicableLitres, applicableLitres, normalizedRate,
                        vatPercentage);
                if (lineRetail > originalTotal)
                {
                    lineRetail = originalTotal;
                }
                decimal lineWholesale =
                    MyDieselTaxRules.CalculateNetDiscountFromGross(
                        lineRetail, vatPercentage);
                remainingLitres -= applicableLitres;

                decimal vatMultiplier = 1 + vatPercentage / 100;
                decimal discountedTotal = Math.Round(originalTotal -
                    lineRetail, 2);
                decimal discountedNet = Math.Round(discountedTotal /
                    vatMultiplier, 2, MidpointRounding.ToEven);
                decimal lineVat = discountedTotal - discountedNet;
                decimal discountPercentage = originalTotal == 0
                    ? 0
                    : Math.Round(100 * lineRetail / originalTotal, 4);

                line["DiscountAmountRetail"] = lineRetail;
                line["DiscountAmountWhole"] = lineWholesale;
                line["DiscountAmount"] = lineRetail;
                line["DiscountPercentage"] = discountPercentage;
                line["TotalPrice"] = discountedTotal;
                line["VatAmount"] = lineVat;

                retailDiscount += lineRetail;
                wholesaleDiscount += lineWholesale;
                totalAmount += discountedTotal;
                vatAmount += lineVat;
                netBeforeDiscount += discountedNet + lineWholesale;
                netAfterDiscount += discountedNet;
            }

            invoice["DiscountAmountRetail"] = Math.Round(retailDiscount, 2);
            invoice["DiscountAmountWhole"] = Math.Round(wholesaleDiscount, 2);
            invoice["DiscountAmount"] = Math.Round(retailDiscount, 2);
            invoice["TotalAmount"] = Math.Round(totalAmount, 2);
            invoice["VatAmount"] = Math.Round(vatAmount, 2);
            invoice["NettoAmount"] = Math.Round(netBeforeDiscount, 2);
            invoice["NettoAfterDiscount"] = Math.Round(netAfterDiscount, 2);
            return replacement;
        }

        private static decimal OriginalLineTotal(JObject line,
            decimal volume)
        {
            decimal retailUnitPrice = DecimalValue(
                line["UnitPriceRetail"]);
            decimal savedUnitPrice = DecimalValue(line["UnitPrice"]);
            decimal unitPrice = retailUnitPrice != 0
                ? Math.Abs(retailUnitPrice)
                : Math.Abs(savedUnitPrice);
            decimal total = Math.Round(volume * unitPrice, 2);
            if (total <= 0)
            {
                decimal totalValue = DecimalValue(line["TotalPrice"]);
                decimal discountValue = DecimalValue(
                    line["DiscountAmountRetail"]);
                decimal savedTotal = Math.Abs(totalValue);
                decimal savedDiscount = Math.Abs(discountValue);
                total = Math.Round(savedTotal + savedDiscount, 2);
            }
            return total;
        }

        private static decimal LineLitres(JObject line)
        {
            decimal savedVolume = DecimalValue(line["Volume"]);
            return Math.Abs(savedVolume);
        }

        private static decimal DecimalValue(JToken value)
        {
            if (value == null || value.Type == JTokenType.Null)
            {
                return 0;
            }
            return value.Value<decimal>();
        }
    }

    public static class MyDieselInvoiceCorrectionRunner
    {
        public static void Run(Func<InvoiceWorkflowResult> issueReplacement,
            Func<InvoiceWorkflowResult, MyDieselServiceResult> correctCoupon,
            Func<bool, InvoiceWorkflowResult> issueCredit,
            Action<bool> verifyCouponAssociation,
            Action<string, string> save, Func<bool> paused)
        {
            if (paused())
            {
                return;
            }

            InvoiceWorkflowResult replacementResult = issueReplacement();
            if (!replacementResult.Accepted)
            {
                string replacementMessage = Describe("Επανέκδοση",
                    replacementResult);
                save("ReplacementPending", replacementMessage);
                return;
            }
            save("ReplacementAccepted",
                "Η επανέκδοση έγινε αποδεκτή. Εκκρεμεί η διόρθωση συσχέτισης του κουπονιού μέσω myDiesel.");
            if (paused())
            {
                return;
            }

            MyDieselServiceResult correctionResult = correctCoupon(
                replacementResult);
            if (correctionResult.OperationState ==
                MyDieselOperationState.Rejected)
            {
                save("CouponCorrectionRejected",
                    "Το myDiesel απέρριψε τη διόρθωση. Το αρχικό παραστατικό και το κουπόνι παραμένουν συνδεδεμένα· εκκρεμεί πίστωση της επανέκδοσης.");
                InvoiceWorkflowResult compensationResult = issueCredit(true);
                if (!compensationResult.Accepted)
                {
                    string compensationMessage = Describe(
                        "Πιστωτικό επανέκδοσης", compensationResult);
                    save("CompensationCreditPending", compensationMessage);
                    return;
                }
                verifyCouponAssociation(false);
                save("Completed",
                    "Η διόρθωση κουπονιού απορρίφθηκε. Η επανέκδοση πιστώθηκε και το αρχικό παραστατικό με το κουπόνι έμειναν αμετάβλητα.");
                return;
            }
            if (correctionResult.OperationState !=
                MyDieselOperationState.Succeeded)
            {
                string operationState = correctionResult.OperationState.ToString();
                save("CouponCorrectionPending",
                    "Η επανέκδοση έγινε αποδεκτή, αλλά το αποτέλεσμα της διόρθωσης myDiesel δεν είναι οριστικό (" +
                    operationState + "). Δεν εκδόθηκε πιστωτικό.");
                return;
            }
            save("CouponCorrected",
                "Το κουπόνι συνδέθηκε με το MARK της επανέκδοσης. Εκκρεμεί η πίστωση του αρχικού παραστατικού.");
            if (paused())
            {
                return;
            }

            InvoiceWorkflowResult creditResult = issueCredit(false);
            if (!creditResult.Accepted)
            {
                string creditMessage = Describe("Πιστωτικό αρχικού",
                    creditResult);
                save("OriginalCreditPending", creditMessage);
                return;
            }
            verifyCouponAssociation(true);
            save("Completed",
                "Ολοκληρώθηκε η επανέκδοση, η διόρθωση συσχέτισης του κουπονιού και η πίστωση του αρχικού παραστατικού.");
        }

        private static string Describe(string stage, InvoiceWorkflowResult result)
        {
            string[] errors = result.Response == null || result.Response.Errors == null
                ? new string[0]
                : result.Response.Errors;
            string errorText = string.Join("; ", errors);
            return stage + ": " + result.FiscalState + ". " + errorText;
        }
    }
}
