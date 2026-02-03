// MyDataSingleFile.cs
// C# 6 compatible (no switch expressions, no init-only, no tuples, no pattern matching)
// myDATA REST API v2.0.0 (Dec 2025) concepts:
// - Income classification per invoice line: lineNumber + incomeClassificationDetailData (SendIncomeClassification)  :contentReference[oaicite:5]{index=5}
// - Invoice types (Appendix 8.1) include 1.x/2.x and delivery note types 9.1/9.2/9.3/10.1/10.2             :contentReference[oaicite:6]{index=6}
// - VAT categories (Appendix 8.2): mainland 1/2/3, islands 4/5/6, exempt 7                                  :contentReference[oaicite:7]{index=7}
// - VAT exemption category required when vatCategory=7 (Appendix 8.3 + validation rules)                    :contentReference[oaicite:8]{index=8}

using System;

namespace Exedron.MyData.Resolver
{
    

    public static class DeliveryNoteFormatter
    {
        public static string ToInvoiceTypeString(DeliveryNoteType type)
        {
            // 91 => "9.1", 102 => "10.2"
            int v = (int)type;
            int major = v / 10;
            int minor = v % 10;
            return major.ToString() + "." + minor.ToString();
        }
    }

    

    // --------------------------- Basic address / delivery header models ---------------------------

    public sealed class AddressInfo
    {
        public string Street { get; set; }
        public string Number { get; set; }
        public string PostalCode { get; set; }
        public string City { get; set; }
        public string CountryCode { get; set; } // e.g. "GR"
    }

    public sealed class DeliveryNoteHeader
    {
        // loadingAddress & deliveryAddress are required for delivery note documents / invoice+DN. :contentReference[oaicite:12]{index=12}
        public AddressInfo LoadingAddress { get; set; }
        public AddressInfo DeliveryAddress { get; set; }

        // Optional (present in delivery header section)
        public int? StartShippingBranch { get; set; }
        public int? CompleteShippingBranch { get; set; }
    }

    // --------------------------- Output DTOs ---------------------------

    public sealed class VatDefinition
    {
        // myDATA vatCategory numeric code (Appendix 8.2): 1/2/3 mainland, 4/5/6 islands, 7 exempt :contentReference[oaicite:13]{index=13}
        public byte VatCategory { get; set; }

        // myDATA vatExemptionCategory (Appendix 8.3) – required when VatCategory = 7 :contentReference[oaicite:14]{index=14}
        public byte? VatExemptionCategory { get; set; }
    }

    public sealed class RowClassificationResult
    {
        // invType string like "1.1", "1.2", "2.2", "9.3", "10.2"
        public string InvType { get; set; }

        // myDATA lineNumber (1-based)
        public int LineNumber { get; set; }

        // Income classification (per row)
        public IncomeTypeCode IncomeType { get; set; }
        public IncomeCategoryCode IncomeCategory { get; set; }

        // Optional: row VAT mapping (useful when building invoiceDetails per row)
        public VatDefinition Vat { get; set; }
    }

    // --------------------------- The main context object (invoice-level) ---------------------------

    /// <summary>
    /// Configure once per document (invoice or delivery note), then resolve per invoice detail line.
    /// Income classification is line-based in SendIncomeClassification (lineNumber + detailData). :contentReference[oaicite:15]{index=15}
    /// Delivery note requirements are header-level (delivery header, invoiceType 9.x/10.x, isDeliveryNote flag). :contentReference[oaicite:16]{index=16}
    /// </summary>
    public sealed class MyDataDocumentContext
    {
        // Document setup
        public DocumentFamily Family { get; private set; }

        // Invoice setup
        public InvoiceKind Kind { get; private set; }
        public CustomerScenario Customer { get; private set; }

        // VAT setup
        public VatRegion VatRegion { get; private set; }
        public byte? DefaultVatExemptionCategory { get; private set; }

        // Delivery note setup (when applicable)
        public DeliveryNoteType? DeliveryType { get; private set; }
        public DeliveryNoteHeader DeliveryHeader { get; private set; }

        // Optional forced invoiceType
        public string InvoiceTypeOverride { get; private set; }

        public MyDataDocumentContext(
            DocumentFamily family,
            InvoiceKind kind,
            CustomerScenario customer,
            VatRegion vatRegion,
            byte? defaultVatExemptionCategory = null,
            DeliveryNoteType? deliveryType = null,
            DeliveryNoteHeader deliveryHeader = null,
            string invoiceTypeOverride = null)
        {
            Family = family;
            Kind = kind;
            Customer = customer;
            VatRegion = vatRegion;
            DefaultVatExemptionCategory = defaultVatExemptionCategory;

            DeliveryType = deliveryType;
            DeliveryHeader = deliveryHeader;

            InvoiceTypeOverride = invoiceTypeOverride;

            ValidateSetup();
        }

        private void ValidateSetup()
        {
            if (Family == DocumentFamily.DeliveryNoteOnly)
            {
                if (!DeliveryType.HasValue)
                    throw new ArgumentException("DeliveryType is required for DeliveryNoteOnly documents.");

                RequireDeliveryHeader();
            }

            if (Family == DocumentFamily.InvoiceWithDeliveryNote)
            {
                RequireDeliveryHeader();
            }
        }

        private void RequireDeliveryHeader()
        {
            if (DeliveryHeader == null ||
                DeliveryHeader.LoadingAddress == null ||
                DeliveryHeader.DeliveryAddress == null)
            {
                throw new ArgumentException("DeliveryHeader with LoadingAddress and DeliveryAddress is required for delivery note documents.");
            }
        }

        /// <summary>
        /// Resolve the header invoiceType string:
        /// - DeliveryNoteOnly => 9.x/10.x
        /// - IncomeInvoice / InvoiceWithDeliveryNote => 1.x/2.x (based on row nature + customer scenario)
        /// Allowed invoice types include 9.1/9.2/9.3/10.1/10.2 (Appendix 8.1). :contentReference[oaicite:17]{index=17}
        /// </summary>
        public string GetHeaderInvoiceType(SaleNature rowNature)
        {
            if (!string.IsNullOrWhiteSpace(InvoiceTypeOverride))
                return InvoiceTypeOverride;

            if (Family == DocumentFamily.DeliveryNoteOnly)
                return DeliveryNoteFormatter.ToInvoiceTypeString(DeliveryType.Value);

            // Otherwise: normal invoice types
            return GetIncomeInvoiceType(rowNature);
        }

        private string GetIncomeInvoiceType(SaleNature rowNature)
        {
            // Correction invoice types are in Appendix 8.1 (5.x etc). Implement if needed.
            if (Kind == InvoiceKind.Correction)
                throw new NotSupportedException("Correction invType mapping (5.x) not configured in this file.");

            // Normal invoices: Domestic 1.1/2.1, EU 1.2/2.2, Third 1.3/2.3
            if (Customer == CustomerScenario.DomesticB2B || Customer == CustomerScenario.DomesticB2C)
                return (rowNature == SaleNature.Goods) ? "1.1" : "2.1";

            if (Customer == CustomerScenario.EuB2B || Customer == CustomerScenario.EuB2C)
                return (rowNature == SaleNature.Goods) ? "1.2" : "2.2";

            if (Customer == CustomerScenario.ThirdCountry)
                return (rowNature == SaleNature.Goods) ? "1.3" : "2.3";

            throw new ArgumentOutOfRangeException("Customer");
        }

        /// <summary>
        /// Main method: invoice-level context + row parameters => row classification result.
        /// Use this to build SendIncomeClassification payload: per invoiceMark, per lineNumber. :contentReference[oaicite:18]{index=18}
        /// </summary>
        public RowClassificationResult ResolveRow(
            int lineNumber,
            SaleNature rowNature,
            VatBand rowVatBand,
            byte? rowVatExemptionCategory = null,
            IncomeTypeCode? incomeTypeOverride = null,
            IncomeCategoryCode? incomeCategoryOverride = null)
        {
            if (lineNumber <= 0)
                throw new ArgumentOutOfRangeException("lineNumber", "myDATA lineNumber is 1-based and must be > 0.");

            var result = new RowClassificationResult();
            result.LineNumber = lineNumber;

            // invType is header-level but returned for convenience
            result.InvType = GetHeaderInvoiceType(rowNature);

            // Income type/category
            result.IncomeType = incomeTypeOverride.HasValue ? incomeTypeOverride.Value : ResolveIncomeType();
            result.IncomeCategory = incomeCategoryOverride.HasValue ? incomeCategoryOverride.Value : ResolveIncomeCategory(rowNature);

            // Row VAT (optional but typically needed for invoiceDetails)
            result.Vat = ResolveVat(rowVatBand, rowVatExemptionCategory);

            return result;
        }

        // --------------------------- Internal resolvers ---------------------------

        private IncomeTypeCode ResolveIncomeType()
        {
            if (Kind == InvoiceKind.Correction)
                return IncomeTypeCode.E3_561_008; // placeholder: set to your official correction type

            // Practical mapping by jurisdiction; adjust to your accounting policy / codes.
            if (Customer == CustomerScenario.DomesticB2B) return IncomeTypeCode.E3_561_001;
            if (Customer == CustomerScenario.DomesticB2C) return IncomeTypeCode.E3_561_003;
            if (Customer == CustomerScenario.EuB2B || Customer == CustomerScenario.EuB2C) return IncomeTypeCode.E3_561_005;
            if (Customer == CustomerScenario.ThirdCountry) return IncomeTypeCode.E3_561_006;

            throw new ArgumentOutOfRangeException("Customer");
        }

        private IncomeCategoryCode ResolveIncomeCategory(SaleNature rowNature)
        {
            if (Kind == InvoiceKind.Correction)
                return IncomeCategoryCode.category1_95;

            return (rowNature == SaleNature.Goods)
                ? IncomeCategoryCode.category1_1
                : IncomeCategoryCode.category1_3;
        }

        private VatDefinition ResolveVat(VatBand band, byte? rowExemptionCategory)
        {
            var vat = new VatDefinition();

            if (band == VatBand.Exempt)
            {
                vat.VatCategory = 7; // Without VAT (Appendix 8.2)
                byte? ex = rowExemptionCategory.HasValue ? rowExemptionCategory : DefaultVatExemptionCategory;

                if (!ex.HasValue)
                    throw new ArgumentException("VatExemptionCategory is required when vatCategory=7 (Exempt).");

                vat.VatExemptionCategory = ex.Value;
                return vat;
            }

            // Mainland: 1/2/3 ; Islands: 4/5/6 (Appendix 8.2) :contentReference[oaicite:19]{index=19}
            if (VatRegion == VatRegion.Mainland)
            {
                if (band == VatBand.Standard) vat.VatCategory = 1;
                else if (band == VatBand.Reduced) vat.VatCategory = 2;
                else if (band == VatBand.SuperReduced) vat.VatCategory = 3;
                else throw new ArgumentOutOfRangeException("band");
            }
            else
            {
                if (band == VatBand.Standard) vat.VatCategory = 4;
                else if (band == VatBand.Reduced) vat.VatCategory = 5;
                else if (band == VatBand.SuperReduced) vat.VatCategory = 6;
                else throw new ArgumentOutOfRangeException("band");
            }

            return vat;
        }
    }

    // --------------------------- Example usage (optional) ---------------------------
    // Remove this in production; included here as a quick reference.

    public static class Example
    {
        public static void Run()
        {
            // Example: Invoice + Delivery Note (isDeliveryNote=true) for EU B2B services
            var dnHeader = new DeliveryNoteHeader
            {
                LoadingAddress = new AddressInfo { Street = "A", Number = "1", PostalCode = "11111", City = "Athens", CountryCode = "GR" },
                DeliveryAddress = new AddressInfo { Street = "B", Number = "2", PostalCode = "22222", City = "Athens", CountryCode = "GR" }
            };

            var ctx = new MyDataDocumentContext(
                family: DocumentFamily.InvoiceWithDeliveryNote,
                kind: InvoiceKind.Normal,
                customer: CustomerScenario.EuB2B,
                vatRegion: VatRegion.Mainland,
                defaultVatExemptionCategory: 14,   // example exemption reason code (Appendix 8.3)
                deliveryType: null,
                deliveryHeader: dnHeader
            );

            // Row 1 classification (lineNumber=1)
            RowClassificationResult row1 = ctx.ResolveRow(
                lineNumber: 1,
                rowNature: SaleNature.Services,
                rowVatBand: VatBand.Exempt
            );

            // row1.InvType -> "2.2" (EU services)
            // row1.IncomeType -> E3_561_005 (EU income type mapping)
            // row1.Vat.VatCategory -> 7 (exempt), VatExemptionCategory -> 14
        }
    }
}
