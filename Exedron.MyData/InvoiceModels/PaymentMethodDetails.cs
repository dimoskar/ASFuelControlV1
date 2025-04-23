using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Exedron.MyData.Interfaces;

namespace Exedron.MyData.InvoiceModels
{
    public class PaymentMethodDetails : IPaymentMethodDetailType
    {
        public PaymentMethodEnum Type { set; get; }
        public decimal Amount { set; get; }
        public string PaymentMethodInfo { set; get; }

        public string AsXml()
        {
            string xml = Properties.Resources.PaymentMethod;
            xml = xml.Replace("[type]", ((int)this.Type).ToString());
            xml = xml.Replace("[amount]", RequestHelpers.ToString(this.Amount));
            if(string.IsNullOrEmpty(this.PaymentMethodInfo))
                xml = xml.Replace("<paymentMethodInfo>[paymentmethodinfo]</paymentMethodInfo>", "");
            else
                xml = xml.Replace("[paymentmethodinfo]", this.PaymentMethodInfo);
            return xml;
        }
    }
}
