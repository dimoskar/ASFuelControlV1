using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exedron.MyData.Resolver
{
    // --------------------------- Document Family ---------------------------

    public enum DocumentFamily
    {
        IncomeInvoice = 0,           // 1.x / 2.x (sales/service invoices)
        DeliveryNoteOnly = 1,        // 9.x / 10.x (movement docs)
        InvoiceWithDeliveryNote = 2  // invoice plus isDeliveryNote=true (delivery header required)
    }

    // --------------------------- Invoice / Customer ---------------------------

    public enum InvoiceKind
    {
        Normal = 0,
        Correction = 1 // mapping to 5.x invTypes depends on your usage; not included here
    }

    public enum SaleNature
    {
        Goods = 0,
        Services = 1
    }

    public enum CustomerScenario
    {
        DomesticB2B = 0,
        DomesticB2C = 1,
        EuB2B = 2,
        EuB2C = 3,
        ThirdCountry = 4
    }

    // --------------------------- VAT abstractions (no % values) ---------------------------

    public enum VatBand
    {
        Standard = 0,
        Reduced = 1,
        SuperReduced = 2,
        Exempt = 3
    }

    public enum VatRegion
    {
        Mainland = 0,
        IslandReduced = 1
    }

    // --------------------------- Delivery Note Types (invoiceType 9.x / 10.x) ---------------------------

    // Allowed values include: 9.1, 9.2, 9.3, 10.1, 10.2 (Appendix 8.1 / schema restrictions) :contentReference[oaicite:9]{index=9}
    public enum DeliveryNoteType
    {
        DN_Related = 91,       // "9.1"
        DN_Consolidated = 92,  // "9.2"
        DN_Standalone = 93,    // "9.3"
        QRR_Related = 101,     // "10.1"
        QRR_NotRelated = 102   // "10.2"
    }

    // --------------------------- Income classification codes (types & categories) ---------------------------

    // Income classification type codes (Appendix 8.9). Keep only what you use and extend as needed. :contentReference[oaicite:10]{index=10}
    public enum IncomeTypeCode
    {
        // Domestic B2B sales (common)
        E3_561_001,

        // Domestic B2C / retail (common)
        E3_561_003,

        // EU sales (common)
        E3_561_005,

        // Third country sales (common)
        E3_561_006,

        // Corrections (placeholder; use your official correction code)
        E3_561_008
    }

    // Income classification categories (Appendix 8.8). Fill with your official subset. :contentReference[oaicite:11]{index=11}
    public enum IncomeCategoryCode
    {
        // Placeholders you can replace/extend with your actual categories from 8.8
        category1_1,   // goods revenue
        category1_3,   // services revenue
        category1_95   // corrections
    }
}
