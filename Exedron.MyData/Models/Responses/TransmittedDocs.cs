using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exedron.MyData.Models.Transmitted
{

    // NOTE: Generated code may require at least .NET Framework 4.5 or .NET Core/Standard 2.0.
    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.aade.gr/myDATA/invoice/v1.0")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://www.aade.gr/myDATA/invoice/v1.0", IsNullable = false)]
    public partial class RequestedDoc
    {

        private RequestedDocContinuationToken continuationTokenField;

        private RequestedDocInvoice[] invoicesDocField;

        /// <remarks/>
        public RequestedDocContinuationToken continuationToken
        {
            get
            {
                return this.continuationTokenField;
            }
            set
            {
                this.continuationTokenField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("invoice", IsNullable = false)]
        public RequestedDocInvoice[] invoicesDoc
        {
            get
            {
                return this.invoicesDocField;
            }
            set
            {
                this.invoicesDocField = value;
            }
        }

        public string ErrorCode { set; get; }
        public string ErrorMessage { set; get; }

        public static RequestedDoc Deserialize(string xml)
        {
            var ser = new System.Xml.Serialization.XmlSerializer(typeof(RequestedDoc));
            RequestedDoc result;

            using (TextReader reader = new StringReader(xml))
            {
                result = ser.Deserialize(reader) as RequestedDoc;
            }

            return result;
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.aade.gr/myDATA/invoice/v1.0")]
    public partial class RequestedDocContinuationToken
    {

        private string nextPartitionKeyField;

        private string nextRowKeyField;

        /// <remarks/>
        public string nextPartitionKey
        {
            get
            {
                return this.nextPartitionKeyField;
            }
            set
            {
                this.nextPartitionKeyField = value;
            }
        }

        /// <remarks/>
        public string nextRowKey
        {
            get
            {
                return this.nextRowKeyField;
            }
            set
            {
                this.nextRowKeyField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.aade.gr/myDATA/invoice/v1.0")]
    public partial class RequestedDocInvoice
    {

        private string uidField;

        private ulong markField;

        private ulong cancelledByMarkField;

        private RequestedDocInvoiceIssuer issuerField;

        private RequestedDocInvoiceInvoiceHeader invoiceHeaderField;

        private RequestedDocInvoicePaymentMethods paymentMethodsField;

        private RequestedDocInvoiceInvoiceDetails[] invoiceDetailsField;

        private RequestedDocInvoiceInvoiceSummary invoiceSummaryField;

        /// <remarks/>
        public string uid
        {
            get
            {
                return this.uidField;
            }
            set
            {
                this.uidField = value;
            }
        }

        /// <remarks/>
        public ulong mark
        {
            get
            {
                return this.markField;
            }
            set
            {
                this.markField = value;
            }
        }

        /// <remarks/>
        public ulong cancelledByMark
        {
            get
            {
                return this.cancelledByMarkField;
            }
            set
            {
                this.cancelledByMarkField = value;
            }
        }

        /// <remarks/>
        public RequestedDocInvoiceIssuer issuer
        {
            get
            {
                return this.issuerField;
            }
            set
            {
                this.issuerField = value;
            }
        }

        /// <remarks/>
        public RequestedDocInvoiceInvoiceHeader invoiceHeader
        {
            get
            {
                return this.invoiceHeaderField;
            }
            set
            {
                this.invoiceHeaderField = value;
            }
        }

        /// <remarks/>
        public RequestedDocInvoicePaymentMethods paymentMethods
        {
            get
            {
                return this.paymentMethodsField;
            }
            set
            {
                this.paymentMethodsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("invoiceDetails")]
        public RequestedDocInvoiceInvoiceDetails[] invoiceDetails
        {
            get
            {
                return this.invoiceDetailsField;
            }
            set
            {
                this.invoiceDetailsField = value;
            }
        }

        /// <remarks/>
        public RequestedDocInvoiceInvoiceSummary invoiceSummary
        {
            get
            {
                return this.invoiceSummaryField;
            }
            set
            {
                this.invoiceSummaryField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.aade.gr/myDATA/invoice/v1.0")]
    public partial class RequestedDocInvoiceIssuer
    {

        private string vatNumberField;

        private string countryField;

        private byte branchField;

        /// <remarks/>
        public string vatNumber
        {
            get
            {
                return this.vatNumberField;
            }
            set
            {
                this.vatNumberField = value;
            }
        }

        /// <remarks/>
        public string country
        {
            get
            {
                return this.countryField;
            }
            set
            {
                this.countryField = value;
            }
        }

        /// <remarks/>
        public byte branch
        {
            get
            {
                return this.branchField;
            }
            set
            {
                this.branchField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.aade.gr/myDATA/invoice/v1.0")]
    public partial class RequestedDocInvoiceInvoiceHeader
    {

        private string seriesField;

        private int aaField;

        private System.DateTime issueDateField;

        private decimal invoiceTypeField;

        private bool vatPaymentSuspensionField;

        private string currencyField;

        /// <remarks/>
        public string series
        {
            get
            {
                return this.seriesField;
            }
            set
            {
                this.seriesField = value;
            }
        }

        /// <remarks/>
        public int aa
        {
            get
            {
                return this.aaField;
            }
            set
            {
                this.aaField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(DataType = "date")]
        public System.DateTime issueDate
        {
            get
            {
                return this.issueDateField;
            }
            set
            {
                this.issueDateField = value;
            }
        }

        /// <remarks/>
        public decimal invoiceType
        {
            get
            {
                return this.invoiceTypeField;
            }
            set
            {
                this.invoiceTypeField = value;
            }
        }

        /// <remarks/>
        public bool vatPaymentSuspension
        {
            get
            {
                return this.vatPaymentSuspensionField;
            }
            set
            {
                this.vatPaymentSuspensionField = value;
            }
        }

        /// <remarks/>
        public string currency
        {
            get
            {
                return this.currencyField;
            }
            set
            {
                this.currencyField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.aade.gr/myDATA/invoice/v1.0")]
    public partial class RequestedDocInvoicePaymentMethods
    {

        private RequestedDocInvoicePaymentMethodsPaymentMethodDetails paymentMethodDetailsField;

        /// <remarks/>
        public RequestedDocInvoicePaymentMethodsPaymentMethodDetails paymentMethodDetails
        {
            get
            {
                return this.paymentMethodDetailsField;
            }
            set
            {
                this.paymentMethodDetailsField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.aade.gr/myDATA/invoice/v1.0")]
    public partial class RequestedDocInvoicePaymentMethodsPaymentMethodDetails
    {

        private byte typeField;

        private decimal amountField;

        /// <remarks/>
        public byte type
        {
            get
            {
                return this.typeField;
            }
            set
            {
                this.typeField = value;
            }
        }

        /// <remarks/>
        public decimal amount
        {
            get
            {
                return this.amountField;
            }
            set
            {
                this.amountField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.aade.gr/myDATA/invoice/v1.0")]
    public partial class RequestedDocInvoiceInvoiceDetails
    {

        private byte lineNumberField;

        private decimal netValueField;

        private byte vatCategoryField;

        private decimal vatAmountField;

        private byte deductionsAmountField;

        private RequestedDocInvoiceInvoiceDetailsIncomeClassification incomeClassificationField;

        /// <remarks/>
        public byte lineNumber
        {
            get
            {
                return this.lineNumberField;
            }
            set
            {
                this.lineNumberField = value;
            }
        }

        /// <remarks/>
        public decimal netValue
        {
            get
            {
                return this.netValueField;
            }
            set
            {
                this.netValueField = value;
            }
        }

        /// <remarks/>
        public byte vatCategory
        {
            get
            {
                return this.vatCategoryField;
            }
            set
            {
                this.vatCategoryField = value;
            }
        }

        /// <remarks/>
        public decimal vatAmount
        {
            get
            {
                return this.vatAmountField;
            }
            set
            {
                this.vatAmountField = value;
            }
        }

        /// <remarks/>
        public byte deductionsAmount
        {
            get
            {
                return this.deductionsAmountField;
            }
            set
            {
                this.deductionsAmountField = value;
            }
        }

        /// <remarks/>
        public RequestedDocInvoiceInvoiceDetailsIncomeClassification incomeClassification
        {
            get
            {
                return this.incomeClassificationField;
            }
            set
            {
                this.incomeClassificationField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.aade.gr/myDATA/invoice/v1.0")]
    public partial class RequestedDocInvoiceInvoiceDetailsIncomeClassification
    {

        private string classificationTypeField;

        private string classificationCategoryField;

        private decimal amountField;

        private byte idField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "https://www.aade.gr/myDATA/incomeClassificaton/v1.0")]
        public string classificationType
        {
            get
            {
                return this.classificationTypeField;
            }
            set
            {
                this.classificationTypeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "https://www.aade.gr/myDATA/incomeClassificaton/v1.0")]
        public string classificationCategory
        {
            get
            {
                return this.classificationCategoryField;
            }
            set
            {
                this.classificationCategoryField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "https://www.aade.gr/myDATA/incomeClassificaton/v1.0")]
        public decimal amount
        {
            get
            {
                return this.amountField;
            }
            set
            {
                this.amountField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "https://www.aade.gr/myDATA/incomeClassificaton/v1.0")]
        public byte id
        {
            get
            {
                return this.idField;
            }
            set
            {
                this.idField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.aade.gr/myDATA/invoice/v1.0")]
    public partial class RequestedDocInvoiceInvoiceSummary
    {

        private decimal totalNetValueField;

        private decimal totalVatAmountField;

        private byte totalWithheldAmountField;

        private byte totalFeesAmountField;

        private byte totalStampDutyAmountField;

        private byte totalOtherTaxesAmountField;

        private byte totalDeductionsAmountField;

        private decimal totalGrossValueField;

        private RequestedDocInvoiceInvoiceSummaryIncomeClassification incomeClassificationField;

        /// <remarks/>
        public decimal totalNetValue
        {
            get
            {
                return this.totalNetValueField;
            }
            set
            {
                this.totalNetValueField = value;
            }
        }

        /// <remarks/>
        public decimal totalVatAmount
        {
            get
            {
                return this.totalVatAmountField;
            }
            set
            {
                this.totalVatAmountField = value;
            }
        }

        /// <remarks/>
        public byte totalWithheldAmount
        {
            get
            {
                return this.totalWithheldAmountField;
            }
            set
            {
                this.totalWithheldAmountField = value;
            }
        }

        /// <remarks/>
        public byte totalFeesAmount
        {
            get
            {
                return this.totalFeesAmountField;
            }
            set
            {
                this.totalFeesAmountField = value;
            }
        }

        /// <remarks/>
        public byte totalStampDutyAmount
        {
            get
            {
                return this.totalStampDutyAmountField;
            }
            set
            {
                this.totalStampDutyAmountField = value;
            }
        }

        /// <remarks/>
        public byte totalOtherTaxesAmount
        {
            get
            {
                return this.totalOtherTaxesAmountField;
            }
            set
            {
                this.totalOtherTaxesAmountField = value;
            }
        }

        /// <remarks/>
        public byte totalDeductionsAmount
        {
            get
            {
                return this.totalDeductionsAmountField;
            }
            set
            {
                this.totalDeductionsAmountField = value;
            }
        }

        /// <remarks/>
        public decimal totalGrossValue
        {
            get
            {
                return this.totalGrossValueField;
            }
            set
            {
                this.totalGrossValueField = value;
            }
        }

        /// <remarks/>
        public RequestedDocInvoiceInvoiceSummaryIncomeClassification incomeClassification
        {
            get
            {
                return this.incomeClassificationField;
            }
            set
            {
                this.incomeClassificationField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.aade.gr/myDATA/invoice/v1.0")]
    public partial class RequestedDocInvoiceInvoiceSummaryIncomeClassification
    {

        private string classificationTypeField;

        private string classificationCategoryField;

        private decimal amountField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "https://www.aade.gr/myDATA/incomeClassificaton/v1.0")]
        public string classificationType
        {
            get
            {
                return this.classificationTypeField;
            }
            set
            {
                this.classificationTypeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "https://www.aade.gr/myDATA/incomeClassificaton/v1.0")]
        public string classificationCategory
        {
            get
            {
                return this.classificationCategoryField;
            }
            set
            {
                this.classificationCategoryField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "https://www.aade.gr/myDATA/incomeClassificaton/v1.0")]
        public decimal amount
        {
            get
            {
                return this.amountField;
            }
            set
            {
                this.amountField = value;
            }
        }
    }


    public class ErrorResponse
    {
        public string statusCode { get; set; }
        public string message { get; set; }
        public static ErrorResponse Deserialize(string json)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<ErrorResponse>(json);
        }
    }

}
