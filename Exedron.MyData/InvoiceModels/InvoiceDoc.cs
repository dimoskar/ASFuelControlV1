using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Exedron.MyData.Interfaces;

namespace Exedron.MyData.InvoiceModels
{
    public class InvoiceDoc : Interfaces.IInvoiceDoc
    {
        public IInvoice[] Invoices { get; set; }

        public string AsXml()
        {
            var xml = Properties.Resources.InvoiceDoc;
            string invXml = "";
            foreach(var invoice in this.Invoices)
            {
                if (invXml == "")
                    invXml = invoice.AsXml();
                else
                    invXml = invXml + "\r\n" + invoice.AsXml();
            }
            xml = xml.Replace("[invoices]", invXml);
            xml = RequestHelpers.PrintXML(xml);
            return xml;
        }
    }
}
