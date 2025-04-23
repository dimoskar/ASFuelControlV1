using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exedron.MyData.Interfaces
{
    public interface IInvoiceRowType : IRequestMember
    {
        int LineNumber { set; get; }
        decimal Quantity { set; get; }
        MeasurementUnitEnum MeasurementUnit { set; get; }
        InvoiceDetailTypeEnum InvoiceDetailType { set; get; }
        decimal NetValue { set; get; }
        VATCategoryEnum VATCategory { set; get; }
        decimal VATAmount { set; get; }
        VATExemptionCategoryEnum VATExemptionCategory { set; get; }
        /// <summary>
        /// Διενεργεια
        /// </summary>
        IShipType Conduct { set; get; }
        bool DiscountOption { set; get; }
        decimal WithheldAmount { set; get; }
        WithheldPercentCategoryEnum WithheldPercentCategory { set; get; }
        decimal StampDutyAmount { set; get; }
        StampDutyPercentCategoryEnum StampDutyPercentCategory { set; get; }
        decimal FeesAmount { set; get; }
        FeesPercentCategoryEnum FeesPercentCategory { set; get; }
        decimal OtherTaxesAmount { set; get; }
        OtherTaxesCategoryEnum OtherTaxesCategory { set; get; }
        decimal DeductionsAmount { set; get; }
        string LineComments { set; get; }
        string ItemDescription { set; get; }
        IIncomeClassification IncomeClassification { set; get; }
        IIncomeClassification[] IncomeClassifications { set; get; }
        IExpenseClassification ExpenseClassification { set; get; }
    }

    public enum MeasurementUnitEnum
    {
        None = 0,
        Items = 1,
        Kilogramms,
        Liters
    }
    public enum InvoiceDetailTypeEnum
    {
        None = 0,
        ClearingThirdPartySales,
        RemunerationThirdPartySales
    }
    public enum VATCategoryEnum
    {
        VAT24 = 1,
        VAT13,
        VAT06,
        VAT17,
        VAT09,
        VAT04,
        NoVAT,
        NoVATEntry
    }
    public enum VATExemptionCategoryEnum
    {
        None = 0,
        Article3 = 1,
        Article5,
        Article13,
        Article14,
        Article16,
        Article19,
        Article22,
        Article24,
        Article25,
        Article26,
        Article27,
        Article27OpenSeeShips,
        Article27_1_G,
        Article28,
        Article39,
        Article39A,
        Article40,
        Article41,
        Article47,
        IncludedArticle43,
        IncludedArticle44,
        IncludedArticle45,
        IncludedArticle46,
    }
    public enum WithheldPercentCategoryEnum
    {
        None = 0,
        /// <summary>
        /// 15%
        /// </summary>
        Interests = 1,
        /// <summary>
        /// 20%
        /// </summary>
        Royalties = 2,
        /// <summary>
        /// 20%
        /// </summary>
        Consulting = 3,
        /// <summary>
        /// 3%
        /// </summary>
        TechnicalProjects = 4,
        /// <summary>
        /// 1%
        /// </summary>
        LiquidFuelTobacco = 5,
        /// <summary>
        /// 4%
        /// </summary>
        OtherGoods = 6,
        /// <summary>
        /// 8%
        /// </summary>
        Services = 7,
        /// <summary>
        /// 4%
        /// </summary>
        EngineersArchitectsStudies = 8,
        /// <summary>
        /// 10%
        /// </summary>
        EngineersArchitectsOther = 9,
        /// <summary>
        /// 15%
        /// </summary>
        Lawyers = 10,
        /// <summary>
        /// 4172/2013 Amount
        /// </summary>
        PayedServices_1_15 = 11,
        /// <summary>
        /// 4172/2013 15% Officers Merchant Navy
        /// </summary>
        PayedServices_2_15A = 12,
        /// <summary>
        /// 4172/2013 10% Lower crew Merchant Navy
        /// </summary>
        PayedServices_2_15B = 13,
        /// <summary>
        /// Amount
        /// </summary>
        SpecialSolidarityContribution = 14,
        /// <summary>
        /// 4172/2013 Compensation for termination of employment Amount
        /// </summary>
        PayedServices_3_15A = 15
    }
    public enum StampDutyPercentCategoryEnum
    {
        None = 0,
        Rate_1_2 = 1,
        Rate_2_4 = 2,
        Rate_3_6 = 3
    }
    public enum FeesPercentCategoryEnum
    {
        None = 0,
        /// <summary>
        /// 12%
        /// </summary>
        MonthlyTotalSum_0_50 = 1,
        /// <summary>
        /// 15%
        /// </summary>
        MonthlyTotalSum_50_100 = 2,
        /// <summary>
        /// 18%
        /// </summary>
        MonthlyTotalSum_100_150 = 3,
        /// <summary>
        /// 20%
        /// </summary>
        MonthlyTotalSum_150_Above = 4,
        /// <summary>
        /// 12%
        /// </summary>
        CardPhone = 5,
        /// <summary>
        /// 10%
        /// </summary>
        CableTV = 6,
        /// <summary>
        /// 5%
        /// </summary>
        LandLinePhone = 7,
        /// <summary>
        /// Amount
        /// </summary>
        PlatciBagFee = 8,
        /// <summary>
        /// Δακοκτονία 2%
        /// </summary>
        Dakos = 9
    }
    public enum OtherTaxesCategoryEnum
    {
        None,
        /// <summary>
        /// 20%
        /// </summary>
        FireInsurance_A1_20,
        /// <summary>
        /// 20%
        /// </summary>
        FireInsurance_A2_20,
        /// <summary>
        /// 4%
        /// </summary>
        LifeInsurance_04,
        /// <summary>
        /// 15%
        /// </summary>
        OtrherInsurance_15,
        /// <summary>
        /// 0%
        /// </summary>
        ΕxemptInsurance_0,
        /// <summary>
        /// 0,50€
        /// </summary>
        Hotels_1_2_050,
        /// <summary>
        /// 1,50€
        /// </summary>
        Hotels_3_150,
        /// <summary>
        /// 3,00€
        /// </summary>
        Hotels_4_300,
        /// <summary>
        /// 4,00€
        /// </summary>
        Hotels_4_400,
        /// <summary>
        /// 0,50€
        /// </summary>
        Rooms_050,
        /// <summary>
        /// 5%
        /// </summary>
        TVAdvertisment_5,
        /// <summary>
        /// 10%
        /// </summary>
        ThirdCountryLuxury_10,
        /// <summary>
        /// 10%
        /// </summary>
        DomesticLuxury_10,
        /// <summary>
        /// 80%
        /// </summary>
        Casinos_80
    }
}
