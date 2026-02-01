using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exedron.ProviderInvoicing
{

    public class InvoiceModel
    {
        public string Uid { get; set; }
        public int Mark { get; set; }
        public int CanceledByMark { get; set; }
        public string AuthenticationCode { get; set; }
        public Trader Issuer { get; set; }
        public Trader CounterPart { get; set; }
        public InvoiceHeader InvoiceHeader { get; set; }
        public PaymentMethod[] PaymentMethods { get; set; }
        public InvoiceDetail[] InvoiceDetails { get; set; }
        public TaxesTotal[] TaxesTotals { get; set; }
        public InvoiceSummary InvoiceSummary { get; set; }
        public string PaymentTerms
        {
            get
            {
                if (PaymentMethods == null)
                    return "";
                return string.Join(",", PaymentMethods.Select(p => p.Type.ToString()));
            }
        }
    }

    public class Trader
    {
        public string VATNumber { get; set; }
        public string Country { get; set; }
        public int Branch { get; set; }
        public string Name { get; set; }
        public bool KeepName { get; set; }
        public Address Address { get; set; }
        public string TaxOffice { set; get; }
        public string Email { set; get; }
        public string Phone { set; get; }
        public string Job { set; get; }
        public string Gemi { set; get; }
        public string SupplyAccountNo { set; get; }
    }

    public class Address
    {
        public string Street { get; set; }
        public string Number { get; set; }
        public string PostalCode { get; set; }
        public string City { get; set; }
        public void ReplaceNumber()
        {
            var numberPos = this.Street.LastIndexOf(" ");
            if (numberPos > 0)
            {
                string subString = this.Street.Substring(numberPos);
                var hasNumber = false;
                foreach (var ch in subString)
                {
                    int num = 0;
                    if (int.TryParse(ch.ToString(), out num))
                    {
                        hasNumber = true;
                        break;
                    }
                }
                if (hasNumber)
                {
                    this.Number = subString.Trim();
                    this.Street = this.Street.Substring(0, numberPos);
                }
                else
                    this.Number = "-";
            }
            else
            {
                this.Number = "-";
            }
        }
        public override string ToString()
        {
            return string.Format("{0} {1}", Street, Number);
        }
    }

    public class InvoiceHeader
    {
        public string Series { get; set; }
        public string AA { get; set; }
        public DateTime IssueDate { get; set; }
        public string InvoiceType { get; set; }
        public bool VATPaymentSuspension { get; set; }
        public string Currency { get; set; }
        public decimal ExchangeRate { get; set; }
        public long[] CorrelatedInvoices { get; set; }
        public string CorrelatedInvoice { set; get; }
        public bool SelfPricing { get; set; }
        public DateTime DispatchDateTime { get; set; }
        public bool DeliveryNote { set; get; }
        public string VehicleNumber { get; set; }
        public MovePurposeEnum MovePurpose { get; set; }
        public bool FuelInvoice { set; get; }
        public OtherDeliveryNoteHeader OtherDeliveryNoteHeader { get; set; }
    }

    public class InvoiceSummary
    {
        public decimal TotalNetValue { get; set; }
        public decimal TotalVATAmount { get; set; }
        public decimal TotalWithheldAmount { get; set; }
        public decimal TotalFeesAmount { get; set; }
        public decimal TotalStampDutyAmount { get; set; }
        public decimal TotalOtherTaxesAmount { get; set; }
        public decimal TotalDeductionsAmount { get; set; }
        public decimal TotalGrossValue { get; set; }
        public IncomeClassification[] IncomeClassification { get; set; }
        public object[] ExpenseClassification { get; set; }
    }

    public class IncomeClassification
    {
        public string ClassificationType { get; set; }
        public string ClassificationCategory { get; set; }
        public decimal Amount { get; set; }
        public int Id { get; set; }
    }

    public class PaymentMethod
    {
        public PaymentMethodEnum Type { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethodInfo { get; set; }
    }

    public class InvoiceDetail
    {
        public VATInvoicingCategoryEnum VATInvoicingCategory { set; get; }
        public int LineNumber { get; set; }
        public decimal Quantity { get; set; }
        public MeasurementUnitEnum MeasurementUnit { get; set; }
        public string FuelCode { set; get; }
        public string InvoiceDetailType { get; set; }
        public decimal NetValue { get; set; }
        public VATCategoryEnum VATCategory { get; set; }
        public decimal VATAmount { get; set; }
        public object VATExemptionCategory { get; set; }
        public Conduct Conduct { get; set; }
        public bool DiscountOption { get; set; }
        public decimal WithheldAmount { get; set; }
        public object WithheldPercentCategory { get; set; }
        public decimal StampDutyAmount { get; set; }
        public object StampDutyPercentCategory { get; set; }
        public decimal FeesAmount { get; set; }
        public object FeesPercentCategory { get; set; }
        public decimal OtherTaxesAmount { get; set; }
        public object OtherTaxesCategory { get; set; }
        public decimal DeductionsAmount { get; set; }
        public string LineComments { get; set; }
        public string ItemDescription { get; set; }
        public string ItemCode { get; set; }
        public IncomeClassification IncomeClassification { get; set; }
        public IncomeClassification[] IncomeClassifications { get; set; }
        public object ExpenseClassification { get; set; }
        public static VATCategoryEnum GetVATCategory(decimal vat)
        {
            if (vat == 0)
                return VATCategoryEnum.NoVAT;

            VATCategoryEnum cat;
            string[] names = Enum.GetNames(typeof(VATCategoryEnum));
            var values = Enum.GetValues(typeof(VATCategoryEnum));
            for (int i = 0; i < names.Length; i++)
            {
                var name = names[i];
                string vatStr = vat.ToString("N0");
                if (name.Contains(vatStr))
                {
                    return (VATCategoryEnum)values.GetValue(i);
                }
            }
            return VATCategoryEnum.NoVAT;
        }
    }

    public class Conduct
    {
        public string ApplicationId { get; set; }
        public DateTime ApplicationDate { get; set; }
        public string DOY { get; set; }
        public string ShipId { get; set; }
    }

    public class TaxesTotal
    {
        public string TaxType { get; set; }
        public int TaxCategory { get; set; }
        public decimal UnderlyingValue { get; set; }
        public decimal TaxAmount { get; set; }
        public int Id { get; set; }
    }

    public class OtherDeliveryNoteHeader
    {
        public bool IsDeliveryNote { set; get; }
        public Address LoadingAddress { set; get; }
        public Address DeliveryAddress { set; get; }
        public int StartShippingBranch { set; get; }
        public int CompleteShippingBranch { set; get; }
    }
    public enum VATInvoicingCategoryEnum
    {
        Standard,
        Excemption,
        Zero,
        Export
    }
    public enum MovePurposeEnum
    {
        None = 0,
        Sales = 1,
        ThirdPartySales,
        Sampling,
        Exhibition,
        Return,
        ProcessingAssembling,
        Internal,
        Safekeeping,
        Purchase,
        SupplyShipPlane,
        ForFree,
        Garantie,
        Leasing,
        StoringThirdparty,
        Other,
        Transport
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
        IRIS = 8
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
