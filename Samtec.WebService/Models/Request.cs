using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Samtec.WebService.Models
{
    public class Request
    {
        public string JobType { set; get; }
        public int InputType { set; get; }
        public Doc Doc { get; set; }
        public string AtxtContent { set; get; }
        public string JobParams { set; get; }
        public override string ToString()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(this);
        }
    }

    public class Doc
    {
        public Receiverheader ReceiverHeader { get; set; }
        public Invoicedetails InvoiceDetails { get; set; }
        public Transactionline[] TransactionLines { get; set; }
        public Payment[] Payments { get; set; }
        public Invoicefooter InvoiceFooter { get; set; }
    }

    public class Receiverheader
    {
        public string TaxID { get; set; }
        public string PrintLine { get; set; }
    }

    public class Invoicedetails
    {
        public string InvoiceUID { get; set; }
        public int InvoiceType { get; set; }
        public int PrintDevice { get; set; }
        public int ReqForToken { get; set; }
        public int? CancelInvType { get; set; }
        public int? CancelDevDailyNum { get; set; }
        public string CancelInvNo { get; set; }
        public string CancelInvSeries { get; set; }
        public string InvoiceNo { get; set; }
        public string InvoiceSeries { get; set; }
        public float InvoiceTotal { get; set; }
        public string Operator { get; set; }
        public string Machine { get; set; }
        public int GasStationLicNum { get; set; }
        public int GasStationInstalNum { get; set; }
        public float InvWithholdingTaxTotal { get; set; }
        public string PrintLine { get; set; }
    }

    public class Invoicefooter
    {
        public string Line1 { get; set; }
        public string Line2 { get; set; }
        public string Line3 { get; set; }
        public string Line4 { get; set; }
        public string Line5 { get; set; }
        public string Line6 { get; set; }
        public string PrintLine { get; set; }
    }

    public class Transactionline
    {
        public int LineNo { get; set; }
        public int ClassCategory { get; set; }
        public int FuelCode { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public decimal ItemAmount { get; set; }
        public int MeasurementUnit { get; set; }
        public decimal SaleQty { get; set; }
        public decimal NetAmount { get; set; }
        public decimal VatAmount { get; set; }
        public decimal GrossAmount { get; set; }
        public int DMType { set; get; }
        public decimal DMValue { set; get; }
        public string VatRate { get; set; }
        public int VatExCategory { get; set; }
        public int FeeCategory { get; set; }
        public string PrintLine { get; set; }
    }

    public class Payment
    {
        public int Type { get; set; }
        public object Description { get; set; }
        public object EftposTID { get; set; }
        public int EftposTransType { get; set; }
        public decimal Value { get; set; }
        public string PrintLine { get; set; }
    }
}
