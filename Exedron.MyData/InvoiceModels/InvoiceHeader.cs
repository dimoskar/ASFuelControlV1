using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Exedron.MyData.Interfaces;

namespace Exedron.MyData.InvoiceModels
{
    public class InvoiceHeader : IInvoiceHeader
    {
        public string Series { get; set; }
        public string AA { get; set; }
        public DateTime IssueDate { get; set; }
        public string InvoiceType { get; set; }
        public bool VATPaymentSuspension { get; set; }
        public string Currency { get; set; }
        public decimal ExchangeRate { get; set; }
        public long[] CorrelatedInvoices { get; set; }
        public bool SelfPricing { get; set; }
        public DateTime DispatchDateTime { get; set; }
        public string VehicleNumber { get; set; }
        public MovePurposeEnum MovePurpose { get; set; }
        public IOtherDeliveryNoteHeader OtherDeliveryNoteHeader { set; get; }

        public string AsXml()
        {
            var xml = Properties.Resources.InvoiceHeader;

            xml = xml.Replace("[invoicetype]", this.InvoiceType);
            xml = xml.Replace("[aa]", this.AA);
            if(string.IsNullOrEmpty(this.Series))
                xml = xml.Replace("[series]", "0");
            else
                xml = xml.Replace("[series]", this.Series);
            xml = xml.Replace("[currency]", this.Currency);
            xml = xml.Replace("[issuedate]", this.IssueDate.ToString("yyyy-MM-dd"));
            if(this.ExchangeRate > 0)
                xml = xml.Replace("[exchangerate]", RequestHelpers.ToString(this.ExchangeRate, 5));
            else
                xml = xml.Replace("[exchangerate]", "");
            xml = xml.Replace("[movepurpose]", ((int)this.MovePurpose).ToString());
            xml = xml.Replace("[selfpricing]", this.SelfPricing.ToString().ToLower());
            xml = xml.Replace("[vatpaymentsuspension]", this.VATPaymentSuspension.ToString().ToLower());
            if (string.IsNullOrEmpty(this.VehicleNumber))
                xml = xml.Replace("[vehiclenumber]", "");
            else
            {
                xml = xml.Replace("[vehiclenumber]", this.VehicleNumber.ToString());
                xml = xml.Replace("[dispatchdatetime]", this.DispatchDateTime.ToString("HH:mm"));
            }

            if (CorrelatedInvoices != null && CorrelatedInvoices.Length > 0)
            {
                var cx = "";
                foreach (var corInv in this.CorrelatedInvoices)
                {
                    if (cx == "")
                        cx = "<correlatedInvoices>" + corInv.ToString() + "</correlatedInvoices>\r\n";
                    else
                        cx = cx + "<correlatedInvoices>" + corInv.ToString() + "</correlatedInvoices>\r\n";
                }
                xml = xml.Replace("<correlatedInvoices>[correlatedinvoices]</correlatedInvoices>", cx);
            }
            else
                xml = xml.Replace("<correlatedInvoices>[correlatedinvoices]</correlatedInvoices>", "");

            if(OtherDeliveryNoteHeader != null)
                xml = xml.Replace("[otherDeliveryNoteHeader]", this.OtherDeliveryNoteHeader.AsXml());
            else
                xml = xml.Replace("[otherDeliveryNoteHeader]", "");

            return xml;
        }
    }
    public class OtherDeliveryNoteHeader : IOtherDeliveryNoteHeader
    {
        public IAddressType LoadingAddress { set; get; }
        public IAddressType DeliveryAddress { set; get; }

        public string AsXml()
        {
            var xml = Properties.Resources.OtherDeliveryNoteHeader;
            var loadingAddress = this.LoadingAddress.AsXml().Replace("<address>", "<loadingAddress>").Replace("</address>", "</loadingAddress>");
            var deliveryAddress = this.DeliveryAddress.AsXml().Replace("<address>", "<deliveryAddress>").Replace("</address>", "</deliveryAddress>");
            xml = xml.Replace("[loadingAddress]", loadingAddress);
            xml = xml.Replace("[deliveryAddress]", deliveryAddress);
            return xml;
        }
    }
}
