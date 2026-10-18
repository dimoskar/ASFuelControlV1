using System;

namespace ASFuelControl.Windows.Threads
{
    public partial class PrintAgent
    {
        // Rendering must not initialize device SDKs, create directories or read credentials.
        private PrintAgent(bool renderingOnly) { }
        internal static string RenderSharedInvoiceContent(Data.DatabaseModel db, Data.Invoice invoice)
        {
            var agent = new PrintAgent(true);
            try
            {
                var text = string.Join("\r\n", agent.CreateInvoiceText(db, invoice));
                // The old narrow layout omits buyer details when no vehicle is linked.
                if (invoice.Vehicle == null && invoice.Trader != null)
                    text += "\r\nΠΕΛΑΤΗΣ: " + invoice.Trader.Name + "\r\nΑΦΜ: " + invoice.Trader.TaxRegistrationNumber +
                        "\r\nΔΟΥ: " + invoice.Trader.TaxRegistrationOffice + "\r\n" + invoice.Trader.Address +
                        "\r\n" + invoice.Trader.ZipCode + " " + invoice.Trader.City;
                return text;
            }
            finally { agent.signWatcher.Dispose(); }
        }

        public static void ReprintSharedInvoice(Guid invoiceId, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("A reprint reason is required.");
            if (!SharedInvoiceRuntime.Handles(invoiceId)) throw new InvalidOperationException("Reconcile the legacy document before using shared reprinting.");
            SharedInvoiceRuntime.Process(invoiceId, true, reason);
        }
    }
}
