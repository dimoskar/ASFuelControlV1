using System;
using Exedron.Invoicing.Core;
using Newtonsoft.Json;

namespace ASFuelControl.Invoicing
{
    public sealed class InvoiceMarkCancellationRejectedException : InvalidOperationException
    {
        public InvoiceMarkCancellationRejectedException(string message) : base(message) { }
    }

    public sealed class InvoiceTypeCorrectionPlan
    {
        public Guid OriginalId, CreditId, ReplacementId, OriginalTypeId, CreditTypeId, OriginalAttemptId;
        public string OriginalDescription, CreditSeries, ReplacementSeries;
        public long OriginalMark;
        public bool DirectCancellation;
        public InvoiceWorkflowSnapshot Original;
        public Newtonsoft.Json.Linq.JObject Host;

        public InvoiceBuildRequest Request(bool credit, int number, string series, DateTime date)
        {
            var request = Copy(Original.Invoice);
            request.Invoice.InvoiceId = credit ? CreditId : ReplacementId;
            request.Invoice.Number = number;
            request.Invoice.Series = series ?? "";
            request.Invoice.PrintedDateTime = date;
            request.Invoice.InvoiceSeries.Series = series ?? "";
            // A replacement sent after a successful MARK cancellation is a
            // retransmission of the same transaction.  Preserve its payment
            // method; only the reversing credit note is issued on credit.
            request.Invoice.PaymentType = credit
                ? InvoicePaymentType.Credit
                : Original.Invoice.Invoice.PaymentType;
            request.Invoice.InvoiceSeries.InvoiceType.InvoiceTypeMode = credit ? InvoiceMode.Credit : InvoiceMode.Normal;
            request.Invoice.InvoiceSeries.InvoiceType.IsRetail = credit;
            request.Parents = credit
                ? new[] { new InvoiceParent { Invoice = Copy(Original.Invoice.Invoice), Transmission = new TransmissionEvidence { Status = 3, Mark = OriginalMark } } }
                : new InvoiceParent[0];
            request.SaleCorrelations = null;
            request.RequireIssuedIdentity = true;
            return request;
        }

        public static T Copy<T>(T value) => JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value));
    }

    public static class InvoiceTypeCorrectionPolicy
    {
        public static void Validate(InvoiceTypeCorrectionPlan plan)
        {
            if (plan?.Original?.Invoice?.Invoice == null || plan.Original.Provider == null ||
                plan.OriginalMark <= 0 || plan.OriginalAttemptId == Guid.Empty)
                throw new InvalidOperationException("Απαιτείται αποθηκευμένη και αποδεκτή αρχική διαβίβαση.");
            if (plan.Original.Payment != null ||
                plan.Original.Invoice.Invoice.PaymentType != InvoicePaymentType.Cash &&
                plan.Original.Invoice.Invoice.PaymentType != InvoicePaymentType.Credit)
                throw new InvalidOperationException("Εξαιρείται: πληρωμή POS/IRIS/στόλου.");
            if (plan.Original.Signing != null &&
                plan.Original.Provider.ProviderKey != InvoiceProviderCatalog.MyDataKey)
                throw new InvalidOperationException("Η αποθηκευμένη φορολογική σήμανση δεν ανήκει σε σύνθετη ροή απευθείας myDATA.");
            if (plan.Original.Invoice.Movement != null || plan.Original.Invoice.Delivery != null ||
                plan.Original.Invoice.Invoice.InvoiceSeries.IsDeliveryNote ||
                (plan.Original.Invoice.SaleCorrelations != null && System.Linq.Enumerable.Any(plan.Original.Invoice.SaleCorrelations)) ||
                (plan.Original.Invoice.Parents != null && System.Linq.Enumerable.Any(plan.Original.Invoice.Parents)))
                throw new InvalidOperationException("Εξαιρείται: διακίνηση ή συσχετισμένο παραστατικό.");

            var builder = new InvoiceBuilder(new InvoiceTypeResolver());
            var original = builder.Build(plan.Original.Invoice);
            if (original.InvoiceHeader.InvoiceType != "11.1" ||
                plan.Original.Invoice.Invoice.InvoiceSeries.InvoiceType.InvoiceTypeMode != InvoiceMode.Normal)
                throw new InvalidOperationException("Η αποθηκευμένη αρχική έκδοση δεν είναι κανονική απόδειξη 11.1.");

            bool direct = plan.Original.Provider.ProviderKey == InvoiceProviderCatalog.MyDataKey;
            if (direct != plan.DirectCancellation)
                throw new InvalidOperationException("Η μέθοδος διόρθωσης δεν συμφωνεί με την αρχική διαδρομή.");
            if (plan.CreditId == Guid.Empty || plan.CreditTypeId == Guid.Empty)
                throw new InvalidOperationException("Η διόρθωση απαιτεί τύπο πιστωτικού λιανικής 11.4 (και ως εναλλακτική αν απορριφθεί η ακύρωση MARK).");

            var replacement = builder.Build(plan.Request(false, 1, plan.ReplacementSeries, DateTime.Now));
            if (replacement.InvoiceHeader.InvoiceType != "1.1")
                throw new InvalidOperationException("Η επανέκδοση δεν επιλύεται ως τιμολόγιο 1.1.");
            var credit = builder.Build(plan.Request(true, 1, plan.CreditSeries, DateTime.Now));
            if (credit.InvoiceHeader.InvoiceType != "11.4")
                throw new InvalidOperationException("Το διορθωτικό δεν επιλύεται ως πιστωτικό λιανικής 11.4.");
        }
    }

    public static class InvoiceTypeCorrectionRunner
    {
        public static void Run(bool directCancellation, Func<long> cancelMark,
            Func<bool, bool, InvoiceWorkflowResult> issue, Action<string, string> save, Func<bool> paused)
        { Run(directCancellation, cancelMark, issue, save, paused, null); }

        public static void Run(bool directCancellation, Func<long> cancelMark,
            Func<bool, bool, InvoiceWorkflowResult> issue, Action<string, string> save, Func<bool> paused,
            Func<InvoiceWorkflowResult, string> finalizeReplacement)
        {
            if (paused()) return;
            bool useCredit = !directCancellation;
            if (directCancellation)
            {
                try
                {
                    long mark = cancelMark();
                    if (mark <= 0) { save("CancellationPending", "Η έκβαση της ακύρωσης MARK δεν επιβεβαιώθηκε."); return; }
                    save("MarkCancelled", "Ακυρώθηκε το αρχικό MARK. Εκκρεμεί η επανέκδοση 1.1.");
                }
                catch (InvoiceMarkCancellationRejectedException ex)
                {
                    useCredit = true;
                    save("CancellationRejected", "Η ΑΑΔΕ απέρριψε την ακύρωση MARK (" + ex.Message +
                        "). Θα χρησιμοποιηθεί πιστωτικό 11.4 πριν από την επανέκδοση 1.1.");
                }
            }
            if (paused()) return;
            if (useCredit)
            {
                var credit = issue(true, directCancellation);
                if (!credit.Accepted) { save("CreditPending", Describe("Πιστωτικό 11.4", credit)); return; }
                save("CreditAccepted", "Το πιστωτικό 11.4 έγινε αποδεκτό. Εκκρεμεί η επανέκδοση 1.1.");
            }
            if (paused()) return;
            var replacement = issue(false, directCancellation && useCredit);
            if (!replacement.Accepted) { save("ReplacementPending", Describe("Επανέκδοση 1.1", replacement)); return; }
            if (finalizeReplacement != null)
            {
                save("ReplacementAccepted", "Η επανέκδοση 1.1 έγινε αποδεκτή. Εκκρεμεί η ενημέρωση του MARK στο myDiesel.");
                if (paused()) return;
                string pending = finalizeReplacement(replacement);
                if (!string.IsNullOrWhiteSpace(pending)) { save("MyDieselPending", pending); return; }
            }
            string completed = directCancellation && !useCredit
                ? "Ολοκληρώθηκε η ακύρωση MARK και η επανέκδοση 1.1."
                : directCancellation
                    ? "Η ακύρωση MARK απορρίφθηκε. Ολοκληρώθηκε η εναλλακτική πίστωση 11.4 και η επανέκδοση 1.1."
                    : "Ολοκληρώθηκε η πίστωση 11.4 και η επανέκδοση 1.1.";
            if (finalizeReplacement != null) completed += " Ενημερώθηκε το MARK στο myDiesel.";
            save("Completed", completed);
        }

        private static string Describe(string stage, InvoiceWorkflowResult result) =>
            stage + ": " + result.FiscalState + ". " + string.Join("; ", result.Response?.Errors ?? new string[0]);
    }
}
