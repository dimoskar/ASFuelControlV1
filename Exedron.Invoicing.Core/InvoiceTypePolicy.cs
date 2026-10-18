using System;

namespace Exedron.Invoicing.Core
{
    public enum InvoiceTransmissionMode { None = 0, Provider = 1, MyData = 2 }

    // Per-type intent. Accounts, environment, POS and devices belong to host settings.
    public sealed class InvoiceTypePolicy
    {
        public Guid InvoiceTypeId { get; set; }
        public InvoiceTransmissionMode Transmission { get; set; }
        public bool SignWithFiscalDevice { get; set; }
        public long Revision { get; set; }
        public bool LocalOnly => Transmission == InvoiceTransmissionMode.None && !SignWithFiscalDevice;

        public void Validate()
        {
            if (InvoiceTypeId == Guid.Empty || !Enum.IsDefined(typeof(InvoiceTransmissionMode), Transmission))
                throw new InvalidOperationException("Select an invoice type and a valid transmission mode.");
            if (Transmission == InvoiceTransmissionMode.Provider && SignWithFiscalDevice)
                throw new InvalidOperationException("Provider issuance with an additional fiscal device is not supported. Provider signatures are supplied by the provider.");
        }
    }
}
