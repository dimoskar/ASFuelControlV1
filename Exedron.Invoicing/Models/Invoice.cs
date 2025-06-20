using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exedron.Invoicing.Models
{

    public class Invoice
    {
        public AadeData aadeData { set; get; }
        public bool b2g { get; set; }
        public Seller seller { get; set; }
        public string invoiceTypeCode { get; set; }
        public Delivery delivery { get; set; }
        public Vatbreakdown[] vatBreakdowns { get; set; }
        public Credittransfer[] creditTransfers { get; set; }
        public DateTime creationDate { get; set; }
        public Invoiceline[] invoiceLines { get; set; }
        public DateTime invoiceIssueDate { get; set; }
        public string paymentTerms { get; set; }
        public string invoiceCurrencyCode { get; set; }
        public Buyer buyer { get; set; }
        public Additionalsupportdoc[] additionalSupportDocs { get; set; }
        public object docLevelAllowances { get; set; }
        public object docLevelCharges { get; set; }
        public Doctotal docTotal { get; set; }
        public string serialNumber { set; get; }
        public string seriesNumber { set; get; }
        public PaymentMethod[] paymentMethods { set; get; }
    }


    public class AadeData
    {
        public bool aadeFuelInvoice { set; get; }
        public string aadeInvoiceTypeCode { get; set; }
        public bool aadeVatPaymentSuspension { set; get; }
        public int? aadeMovePurpose { set; get; }
        public DateTime? aadeDispatchTime { set; get; }
        public string aadeSupplyAccountNo { set; get; }
        public string aadeVehicleNumber { set; get; }
        public Incomeclassification[] incomeClassifications { get; set; }
        public Invoicerowtype[] invoiceRowTypes { get; set; }
    }

    public class Incomeclassification
    {
        public int classificationType { get; set; }
        public int classificationCategory { get; set; }
        public float amount { get; set; }
        public int id { get; set; }
    }

    public class Invoicerowtype
    {
        public object deductionsAmount { get; set; }
        public object dienergia { get; set; }
        public object discountOption { get; set; }
        public object expensesClassification { get; set; }
        public object feesAmount { get; set; }
        public object feesPercentCategory { get; set; }
        public Incomeclassification[] incomeClassification { get; set; }
        public object fuelCode { get; set; }
        public object invoiceDetailType { get; set; }
        public object itemDescr { get; set; }
        public object lineComments { get; set; }
        public int lineNumber { get; set; }
        public object measurementUnit { get; set; }
        public float netValue { get; set; }
        public object otherTaxesAmount { get; set; }
        public object otherTaxesPercentCategory { get; set; }
        public object quantity { get; set; }
        public object quantity15 { get; set; }
        public object recType { get; set; }
        public object stampDutyAmount { get; set; }
        public object stampDutyPercentCategory { get; set; }
        public float vatAmount { get; set; }
        public int vatCategory { get; set; }
        public object vatExemptionCategory { get; set; }
        public object withheldAmount { get; set; }
        public object withheldPercentCategory { get; set; }
    }

    public class Seller
    {
        public string sellerVatIdentifier { get; set; }
        public string sellerName { get; set; }
        public Sellercontact sellerContact { get; set; }
        public Sellerpostaladdress sellerPostalAddress { get; set; }
        public int branch { get; set; }
    }

    public class Sellercontact
    {
        public string sellerContactEmail { get; set; }
        public string sellerContactPoint { get; set; }
        public string sellerContactPhoneNumber { get; set; }
    }

    public class Sellerpostaladdress
    {
        public string sellerCountrySubdivision { get; set; }
        public string sellerCountryCode { get; set; }
        public string sellerAddressLine1 { get; set; }
        public string sellerAddressLine2 { get; set; }
        public string sellerPostCode { get; set; }
        public string sellerCity { get; set; }
    }

    public class Delivery
    {
        public string partyName { get; set; }
        public Deliveryaddress deliveryAddress { get; set; }
    }

    public class Deliveryaddress
    {
        public string deliveryAddressLine1 { get; set; }
        public string deliveryCity { get; set; }
        public string deliveryPostCode { get; set; }
        public string deliveryCountryCode { get; set; }
    }

    public class Buyer
    {
        public Buyerpostaladdress buyerPostalAddress { get; set; }
        public string buyerVatIdentifier { get; set; }
        public string buyerTradingName { get; set; }
        public string buyerName { get; set; }
        public int buyerBranch { get; set; }
    }

    public class Buyerpostaladdress
    {
        public string buyerCountryCode { get; set; }
        public string buyerAddressLine1 { get; set; }
        public string buyerPostCode { get; set; }
        public string buyerCity { get; set; }
    }

    public class Doctotal
    {
        public float amountDueForPayment { get; set; }
        public float documentLevelAllowancesSum { get; set; }
        public float documentLevelChargesSum { get; set; }
        public float exchangeRate { get; set; }
        public float invoiceLinesNetAmountSum { get; set; }
        public float invoiceTotalAmountWithVat { get; set; }
        public float invoiceTotalVatAmount { get; set; }
        public object invoiceTotalVatAmountInAccountingCurrency { get; set; }
        public float invoiceTotalWithoutVat { get; set; }
        public float paidAmount { get; set; }
        public float roundingAmount { get; set; }
        public AadeDocTotals aadeDocTotals { set; get; }
    }


    public class AadeDocTotals
    {
        public float aadeTotalStampDutyAmount { get; set; }
        public float aadeTotalWitheldAmount { get; set; }
        public float aadeTotalFeesAmount { get; set; }
        public float aadeTotalOtherTaxesAmount { get; set; }
        public float aadeTotalDeductionsAmount { get; set; }
        public float aadeTotalGrossValue { get; set; }
        public float aadeTotalNetValue { get; set; }
        public float aadeTotalVatAmount { get; set; }
    }


    public class Vatbreakdown
    {
        public Aadevatdata aadeVatData { get; set; }
        public string categoryCode { get; set; }
        public float categoryRate { get; set; }
        public float categoryTaxAmount { get; set; }
        public float categoryTaxableAmount { get; set; }
        public object exemptionReasonCode { get; set; }
        public object exemptionReasonText { get; set; }
    }

    public class Aadevatdata
    {
        public object aadeVatExemptionCategory { get; set; }
        public int aadeVatCategory { get; set; }
    }

    public class Credittransfer
    {
        public string accountName { get; set; }
        public string accountIdentifier { get; set; }
    }

    public class Invoiceline
    {
        public int lineNumber { get; set; }
        public string note { get; set; }
        public float invoicedQuantity { get; set; }
        public string invoicedQuantityUnits { get; set; }
        public float netAmount { get; set; }
        public float discountPercentage1 { get; set; }
        public float discountPercentage2 { get; set; }
        public float discountPercentage3 { get; set; }
        public float discountAmount { get; set; }
        public float discountTotalAmount { get; set; }
        public Iteminfo itemInfo { get; set; }
        public Linevatinfo lineVatInfo { get; set; }
        public Pricedetails priceDetails { get; set; }
        public Invoicelineallowance[] invoiceLineAllowances { get; set; }
        public Itemclassificationidentifier[] itemClassificationIdentifiers { get; set; }
    }

    public class Iteminfo
    {
        public string itemInfoName { get; set; }
        public string itemInfoDescription { get; set; }
        public string sellerIdentifier { get; set; }
        public string countryOfOrigin { get; set; }
    }

    public class Linevatinfo
    {
        public string vatCategoryCode { get; set; }
        public float vatAmount { get; set; }
        public float vatRate { get; set; }
        public Aadevatdata1 aadeVatData { get; set; }
    }

    public class Aadevatdata1
    {
        public string aadeVatExemptionCategory { get; set; }
        public int aadeVatCategory { get; set; }
    }

    public class Pricedetails
    {
        public float itemNetPrice { get; set; }
        public float itemPriceDiscount { get; set; }
        public object itemGrossPrice { get; set; }
        public float itemPriceBaseQuantity { get; set; }
        public string itemPriceBaseQuantityUnitsCode { get; set; }
    }

    public class Invoicelineallowance
    {
        public float baseAmount { get; set; }
        public float amount { get; set; }
        public float percentage { get; set; }
        public object reason { get; set; }
        public string reasonCode { get; set; }
    }

    public class Itemclassificationidentifier
    {
        public string classificationIdentifier { get; set; }
        public string classificationIdentifierScheme { get; set; }
        public object classificationIdentifierSchemeVersion { get; set; }
    }

    public class Additionalsupportdoc
    {
        public string reference { get; set; }
        public string description { get; set; }
    }


    public class PaymentMethod
    {
        public float amount { get; set; }
        public int type { get; set; }
    }

}
