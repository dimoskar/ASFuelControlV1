using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exedron.MyData.Models.Responses
{

    // NOTE: Generated code may require at least .NET Framework 4.5 or .NET Core/Standard 2.0.
    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.aade.gr/myDATA/invoice/v1.0")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://www.aade.gr/myDATA/invoice/v1.0", IsNullable = false)]
    public partial class RequestedDoc
    {

        private Invoice[] invoicesDocField;

        private CancelledInvoice[] cancelledInvoicesDocField;

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("invoice", IsNullable = false)]
        public Invoice[] invoicesDoc
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

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("cancelledInvoice", IsNullable = false)]
        public CancelledInvoice[] cancelledInvoicesDoc
        {
            get
            {
                return this.cancelledInvoicesDocField;
            }
            set
            {
                this.cancelledInvoicesDocField = value;
            }
        }

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
}
