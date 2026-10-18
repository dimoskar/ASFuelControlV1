using System;
using System.Collections.Generic;
using Exedron.Invoicing.Core;

namespace ASFuelControl.Invoicing.Mapping
{
    // Capture these settings once per issuance under the host's document lease.
    // No static OptionHandler or current-provider singleton is read by the mapper.
    public sealed class MappingContext
    {
        public CompanySettings Company { get; set; }
        public FiscalEnvironment Environment { get; set; }
        public ISet<int> WholesaleCodes { get; set; }
        public IEnumerable<CountryInput> Countries { get; set; }
        public InvoicePaymentType? FleetCardPayment { get; set; }
        public bool IncludeSaleCorrelations { get; set; }
        public DeliveryContext Delivery { get; set; }
        // Explicit non-fuel/service overrides keyed by the legacy FuelType primary key.
        public IDictionary<Guid, ItemMapping> Items { get; } = new Dictionary<Guid, ItemMapping>();
        // Historical, reconciled evidence, never a MARK inferred from current settings.
        public IDictionary<Guid, ParentEvidence> Parents { get; } = new Dictionary<Guid, ParentEvidence>();
    }

    public sealed class ItemMapping
    {
        public int SalesItemType { get; set; } // 0 merchandise, 1 products, 2 services
        public int MeasurementUnit { get; set; }
        public int? OfficialFuelCode { get; set; }
        public int? VatExemptionArticle { get; set; }
    }

    public sealed class ParentEvidence
    {
        public Guid InvoiceId { get; set; }
        public string IssuerVat { get; set; }
        public int IssuerBranch { get; set; }
        public FiscalEnvironment Environment { get; set; }
        public TransmissionEvidence Transmission { get; set; }
    }
}
