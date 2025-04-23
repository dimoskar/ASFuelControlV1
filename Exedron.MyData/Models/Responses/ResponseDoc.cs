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
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class ResponseDoc
    {

        private ResponseDocResponse responseField;

        /// <remarks/>
        public ResponseDocResponse response
        {
            get
            {
                return this.responseField;
            }
            set
            {
                this.responseField = value;
            }
        }
        public static ResponseDoc Deserialize(string xml)
        {
            var ser = new System.Xml.Serialization.XmlSerializer(typeof(ResponseDoc));
            ResponseDoc result;

            using (TextReader reader = new StringReader(xml))
            {
                result = ser.Deserialize(reader) as ResponseDoc;
            }

            return result;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class ResponseDocResponse
    {

        private int indexField;

        private string statusCodeField;

        private ResponseDocResponseError[] errorsField;

        /// <remarks/>
        public int index
        {
            get
            {
                return this.indexField;
            }
            set
            {
                this.indexField = value;
            }
        }

        /// <remarks/>
        public string statusCode
        {
            get
            {
                return this.statusCodeField;
            }
            set
            {
                this.statusCodeField = value;
            }
        }

        public string invoiceUid { set; get; }
        public string responseData { set; get; }
        public ulong? invoiceMark { set; get; }
        public ulong? cancellationMark { set; get; }
        public string qrUrl { set; get; }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("error", IsNullable = false)]
        public ResponseDocResponseError[] errors
        {
            get
            {
                return this.errorsField;
            }
            set
            {
                this.errorsField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class ResponseDocResponseError
    {

        private string messageField;

        private ushort codeField;

        /// <remarks/>
        public string message
        {
            get
            {
                return this.messageField;
            }
            set
            {
                this.messageField = value;
            }
        }

        /// <remarks/>
        public ushort code
        {
            get
            {
                return this.codeField;
            }
            set
            {
                this.codeField = value;
            }
        }
    }



}

namespace Test
{

    

}
