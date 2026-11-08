using System;
using Exedron.Invoicing.Core.Models;

namespace ASFuelControl.Invoicing
{
    public static class InvoiceProviderEvidenceProjection
    {
        public static void Apply(Data.Invoice invoice, InvoiceResponse response,
            string fiscalQrCodeUrl)
        {
            if (invoice == null)
            {
                throw new ArgumentNullException(nameof(invoice));
            }
            if (response != null &&
                !string.IsNullOrWhiteSpace(response.InvoiceSignature))
            {
                invoice.InvoiceSignature = response.InvoiceSignature;
            }
            string qrCode = response == null ? null : response.QrCodeUrl;
            if (string.IsNullOrWhiteSpace(qrCode))
            {
                qrCode = fiscalQrCodeUrl;
            }
            if (!string.IsNullOrWhiteSpace(qrCode))
            {
                invoice.QRCodeData = qrCode;
            }
        }
    }
}
