using System;
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
        public InvoiceWorkflowSnapshot Original { get; set; }
        public JObject Host { get; set; }

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
