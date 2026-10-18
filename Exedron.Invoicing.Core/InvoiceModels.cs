using System;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

// Public data contracts copied from the existing model shape. No vendor SDK or transport dependency.
namespace Exedron.Invoicing.Core.Models
{
    public sealed class InvoiceModel
    {
        public System.String Uid { get; set; }
        public System.Int32 Mark { get; set; }
        public System.Int32 CanceledByMark { get; set; }
        public System.String AuthenticationCode { get; set; }
        public Trader Issuer { get; set; }
        public Trader CounterPart { get; set; }
        public InvoiceHeader InvoiceHeader { get; set; }
        public PaymentMethod[] PaymentMethods { get; set; }
        public InvoiceDetail[] InvoiceDetails { get; set; }
        public TaxesTotal[] TaxesTotals { get; set; }
        public InvoiceSummary InvoiceSummary { get; set; }
        public System.String PaymentTerms { get; set; } = "";
    }
    public sealed class Trader
    {
        public System.String VATNumber { get; set; }
        public System.String Country { get; set; }
        public System.Int32 Branch { get; set; }
        public System.String Name { get; set; }
        public System.Boolean KeepName { get; set; }
        public Address Address { get; set; }
        public System.String TaxOffice { get; set; }
        public System.String Email { get; set; }
        public System.String Phone { get; set; }
        public System.String Job { get; set; }
        public System.String Gemi { get; set; }
        public System.String SupplyAccountNo { get; set; }
    }
    public sealed class Address
    {
        public System.String Street { get; set; }
        public System.String Number { get; set; }
        public System.String PostalCode { get; set; }
        public System.String City { get; set; }
        public void ReplaceNumber()
        {
            if (string.IsNullOrWhiteSpace(Street)) { Number = "-"; return; }
            var match = Regex.Match(Street.Trim(), @"^(.*\S)\s+(\d[^\s]*)$");
            if (match.Success) { Street = match.Groups[1].Value; Number = match.Groups[2].Value; }
            else Number = "-";
        }
    }
    public sealed class InvoiceHeader
    {
        public System.String Series { get; set; }
        public System.String AA { get; set; }
        public System.DateTime IssueDate { get; set; }
        public System.String InvoiceType { get; set; }
        public System.Boolean VATPaymentSuspension { get; set; }
        public System.String Currency { get; set; }
        public System.Decimal ExchangeRate { get; set; }
        public System.Int64[] CorrelatedInvoices { get; set; }
        public System.String CorrelatedInvoice { get; set; }
        public System.Boolean SelfPricing { get; set; }
        public System.DateTime DispatchDateTime { get; set; }
        public System.Boolean DeliveryNote { get; set; }
        public System.String VehicleNumber { get; set; }
        public MovePurposeEnum MovePurpose { get; set; }
        public System.Boolean FuelInvoice { get; set; }
        public OtherDeliveryNoteHeader OtherDeliveryNoteHeader { get; set; }
    }
    public sealed class InvoiceSummary
    {
        public System.Decimal TotalNetValue { get; set; }
        public System.Decimal TotalVATAmount { get; set; }
        public System.Decimal TotalWithheldAmount { get; set; }
        public System.Decimal TotalFeesAmount { get; set; }
        public System.Decimal TotalStampDutyAmount { get; set; }
        public System.Decimal TotalOtherTaxesAmount { get; set; }
        public System.Decimal TotalDeductionsAmount { get; set; }
        public System.Decimal TotalGrossValue { get; set; }
        public IncomeClassification[] IncomeClassification { get; set; }
        public System.Object[] ExpenseClassification { get; set; }
    }
    public sealed class IncomeClassification
    {
        public System.String ClassificationType { get; set; }
        public System.String ClassificationCategory { get; set; }
        public System.Decimal Amount { get; set; }
        public System.Int32 Id { get; set; }
    }
    public sealed class PaymentMethod
    {
        public PaymentMethodEnum Type { get; set; }
        public System.Decimal Amount { get; set; }
        public System.String PaymentMethodInfo { get; set; }
    }
    [XmlInclude(typeof(VATInvoicingCategoryEnum))]
    [XmlInclude(typeof(MovePurposeEnum))]
    [XmlInclude(typeof(PaymentMethodEnum))]
    [XmlInclude(typeof(MeasurementUnitEnum))]
    [XmlInclude(typeof(InvoiceDetailTypeEnum))]
    [XmlInclude(typeof(VATCategoryEnum))]
    [XmlInclude(typeof(VATExemptionCategoryEnum))]
    [XmlInclude(typeof(WithheldPercentCategoryEnum))]
    [XmlInclude(typeof(StampDutyPercentCategoryEnum))]
    [XmlInclude(typeof(FeesPercentCategoryEnum))]
    [XmlInclude(typeof(OtherTaxesCategoryEnum))]
    public sealed class InvoiceDetail
    {
        public VATInvoicingCategoryEnum VATInvoicingCategory { get; set; }
        public System.Int32 LineNumber { get; set; }
        public System.Decimal Quantity { get; set; }
        public MeasurementUnitEnum MeasurementUnit { get; set; }
        public System.String FuelCode { get; set; }
        public System.String InvoiceDetailType { get; set; }
        public System.Decimal NetValue { get; set; }
        public VATCategoryEnum VATCategory { get; set; }
        public System.Decimal VATAmount { get; set; }
        public System.Object VATExemptionCategory { get; set; }
        public Conduct Conduct { get; set; }
        public System.Boolean DiscountOption { get; set; }
        public System.Decimal WithheldAmount { get; set; }
        public System.Object WithheldPercentCategory { get; set; }
        public System.Decimal StampDutyAmount { get; set; }
        public System.Object StampDutyPercentCategory { get; set; }
        public System.Decimal FeesAmount { get; set; }
        public System.Object FeesPercentCategory { get; set; }
        public System.Decimal OtherTaxesAmount { get; set; }
        public System.Object OtherTaxesCategory { get; set; }
        public System.Decimal DeductionsAmount { get; set; }
        public System.String LineComments { get; set; }
        public System.String ItemDescription { get; set; }
        public System.String ItemCode { get; set; }
        public IncomeClassification IncomeClassification { get; set; }
        public IncomeClassification[] IncomeClassifications { get; set; }
        public System.Object ExpenseClassification { get; set; }
    }
    public sealed class Conduct
    {
        public System.String ApplicationId { get; set; }
        public System.DateTime ApplicationDate { get; set; }
        public System.String DOY { get; set; }
        public System.String ShipId { get; set; }
    }
    public sealed class TaxesTotal
    {
        public System.String TaxType { get; set; }
        public System.Int32 TaxCategory { get; set; }
        public System.Decimal UnderlyingValue { get; set; }
        public System.Decimal TaxAmount { get; set; }
        public System.Int32 Id { get; set; }
    }
    public sealed class OtherDeliveryNoteHeader
    {
        public System.Boolean IsDeliveryNote { get; set; }
        public Address LoadingAddress { get; set; }
        public Address DeliveryAddress { get; set; }
        public System.Int32 StartShippingBranch { get; set; }
        public System.Int32 CompleteShippingBranch { get; set; }
    }
    public enum VATInvoicingCategoryEnum
    {
        Standard = 0,
        Excemption = 1,
        Zero = 2,
        Export = 3,
    }
    public enum MovePurposeEnum
    {
        None = 0,
        Sales = 1,
        ThirdPartySales = 2,
        Sampling = 3,
        Exhibition = 4,
        Return = 5,
        ProcessingAssembling = 6,
        Internal = 7,
        Safekeeping = 8,
        Purchase = 9,
        SupplyShipPlane = 10,
        ForFree = 11,
        Garantie = 12,
        Leasing = 13,
        StoringThirdparty = 14,
        Other = 15,
        Transport = 16,
    }
    public enum PaymentMethodEnum
    {
        HomelandAccount = 1,
        ForeignAccount = 2,
        Cash = 3,
        Check = 4,
        Credit = 5,
        WebBanking = 6,
        POS = 7,
        IRIS = 8,
    }
    public enum MeasurementUnitEnum
    {
        None = 0,
        Items = 1,
        Kilogramms = 2,
        Liters = 3,
    }
    public enum InvoiceDetailTypeEnum
    {
        None = 0,
        ClearingThirdPartySales = 1,
        RemunerationThirdPartySales = 2,
    }
    public enum VATCategoryEnum
    {
        VAT24 = 1,
        VAT13 = 2,
        VAT06 = 3,
        VAT17 = 4,
        VAT09 = 5,
        VAT04 = 6,
        NoVAT = 7,
        NoVATEntry = 8,
    }
    public enum VATExemptionCategoryEnum
    {
        None = 0,
        Article3 = 1,
        Article5 = 2,
        Article13 = 3,
        Article14 = 4,
        Article16 = 5,
        Article19 = 6,
        Article22 = 7,
        Article24 = 8,
        Article25 = 9,
        Article26 = 10,
        Article27 = 11,
        Article27OpenSeeShips = 12,
        Article27_1_G = 13,
        Article28 = 14,
        Article39 = 15,
        Article39A = 16,
        Article40 = 17,
        Article41 = 18,
        Article47 = 19,
        IncludedArticle43 = 20,
        IncludedArticle44 = 21,
        IncludedArticle45 = 22,
        IncludedArticle46 = 23,
    }
    public enum WithheldPercentCategoryEnum
    {
        None = 0,
        Interests = 1,
        Royalties = 2,
        Consulting = 3,
        TechnicalProjects = 4,
        LiquidFuelTobacco = 5,
        OtherGoods = 6,
        Services = 7,
        EngineersArchitectsStudies = 8,
        EngineersArchitectsOther = 9,
        Lawyers = 10,
        PayedServices_1_15 = 11,
        PayedServices_2_15A = 12,
        PayedServices_2_15B = 13,
        SpecialSolidarityContribution = 14,
        PayedServices_3_15A = 15,
    }
    public enum StampDutyPercentCategoryEnum
    {
        None = 0,
        Rate_1_2 = 1,
        Rate_2_4 = 2,
        Rate_3_6 = 3,
    }
    public enum FeesPercentCategoryEnum
    {
        None = 0,
        MonthlyTotalSum_0_50 = 1,
        MonthlyTotalSum_50_100 = 2,
        MonthlyTotalSum_100_150 = 3,
        MonthlyTotalSum_150_Above = 4,
        CardPhone = 5,
        CableTV = 6,
        LandLinePhone = 7,
        PlatciBagFee = 8,
        Dakos = 9,
    }
    public enum OtherTaxesCategoryEnum
    {
        None = 0,
        FireInsurance_A1_20 = 1,
        FireInsurance_A2_20 = 2,
        LifeInsurance_04 = 3,
        OtrherInsurance_15 = 4,
        ΕxemptInsurance_0 = 5,
        Hotels_1_2_050 = 6,
        Hotels_3_150 = 7,
        Hotels_4_300 = 8,
        Hotels_4_400 = 9,
        Rooms_050 = 10,
        TVAdvertisment_5 = 11,
        ThirdCountryLuxury_10 = 12,
        DomesticLuxury_10 = 13,
        Casinos_80 = 14,
    }
    public sealed class InvoiceResponse
    {
        public System.String Mark { get; set; }
        public System.String Uid { get; set; }
        public System.String Url { get; set; }
        public System.String InvoiceUrl { get; set; }
        public System.String QrCodeUrl { get; set; }
        public System.String VerificationHash { get; set; }
        public System.String ProviderUrl { get; set; }
        public System.String ProviderInvoiceId { get; set; }
        public bool TransmissionFailure2 { get; set; }
        public System.String InvoiceSignature { get; set; }
        public bool HasErrors { get { return Errors != null && Errors.Length > 0; } }
        public System.String[] Errors { get; set; }
        public System.String[] Warnings { get; set; }
        public bool HasWarnings { get { return Warnings != null && Warnings.Length > 0; } }
    }
}
