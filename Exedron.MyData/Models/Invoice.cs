using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Exedron.MyData.Models
{
    public class Invoice
    {
        public string uid { set; get; }

        public long? mark { set; get; }

        public long? cancelledByMark { set; get; }

        public string authenticationCode { set; get; }

        public byte? transmissionFailure { set; get; }

        public PartyType issuer { set; get; }
        public PartyType counterpart { set; get; }

        public InvoiceHeader invoiceHeader { set; get; }

        [XmlArray("paymentMethods")]
        public PaymentMethodDetailType[] paymentMethods { set; get; }

        [XmlElement]
        public InvoiceRowType[] invoiceDetails { set; get; }

        public TaxTotalsType[] taxesTotals { set; get; }
        public InvoiceSummaryType invoiceSummary { set; get; }
    }

    public partial class CancelledInvoice
    {
        public ulong invoiceMark { set; get; }
        public ulong cancellationMark { set; get; }
        public System.DateTime cancellationDate { set; get; }
    }

    [XmlRoot(ElementName = "InvoicesDoc", Namespace = "http://www.aade.gr/myDATA/invoice/v1.0")]
    public class InvoiceDoc
    {
        [XmlNamespaceDeclarations]
        public XmlSerializerNamespaces Namespaces = new XmlSerializerNamespaces(new System.Xml.XmlQualifiedName[]
        {
            //new System.Xml.XmlQualifiedName("xs", "http://www.w3.org/2001/XMLSchema-instance"),
            //new System.Xml.XmlQualifiedName("inv", "http://www.aade.gr/myDATA/invoice/v1.0"),
            new System.Xml.XmlQualifiedName("icls", "https://www.aade.gr/myDATA/incomeClassificaton/v1.0"),
            new System.Xml.XmlQualifiedName("ecls", "https://www.aade.gr/myDATA/expensesClassificaton/v1.0")
        });

        [XmlElement(ElementName = "invoice")]
        public Invoice[] invoice { set; get; }

        public string Serialize()
        {
            XmlWriterSettings xmlWriterSettings = new XmlWriterSettings
            {
                Indent = true,
                OmitXmlDeclaration = false,
                Encoding = Encoding.UTF8
            };
            string xml = "";
            MemoryStream memoryStream = new MemoryStream();
            
            XmlWriter xmlWriter = XmlWriter.Create(memoryStream, xmlWriterSettings);
            
            var x = new System.Xml.Serialization.XmlSerializer(this.GetType());
            x.Serialize(xmlWriter, this);

            memoryStream.Position = 0;
            StreamReader sr = new StreamReader(memoryStream);
            xml = sr.ReadToEnd();
                
            xmlWriter.Close();
            xmlWriter.Dispose();
            sr.Close();
            sr.Dispose();
            memoryStream.Close();
            memoryStream.Dispose();

            var document = XDocument.Parse(xml);
            document.Descendants()
                    .Where(e1 => e1.IsEmpty || String.IsNullOrWhiteSpace(e1.Value))
                    .Remove();
            xml = document.ToString();

            return xml;
        }

        public static InvoiceDoc Deserialize(string xml)
        {
            var ser = new System.Xml.Serialization.XmlSerializer(typeof(InvoiceDoc));
            InvoiceDoc result;

            using (TextReader reader = new StringReader(xml))
            {
                result = ser.Deserialize(reader) as InvoiceDoc;
            }

            return result;
        }
    }

    [XmlRoot(ElementName = "CancelledInvoicesDoc", Namespace = "http://www.aade.gr/myDATA/invoice/v1.0")]
    public class CancelledInvoicesDoc
    {
        [XmlNamespaceDeclarations]
        public XmlSerializerNamespaces Namespaces = new XmlSerializerNamespaces(new System.Xml.XmlQualifiedName[]
        {
            //new System.Xml.XmlQualifiedName("xs", "http://www.w3.org/2001/XMLSchema-instance"),
            //new System.Xml.XmlQualifiedName("inv", "http://www.aade.gr/myDATA/invoice/v1.0"),
            new System.Xml.XmlQualifiedName("icls", "https://www.aade.gr/myDATA/incomeClassificaton/v1.0"),
            new System.Xml.XmlQualifiedName("ecls", "https://www.aade.gr/myDATA/expensesClassificaton/v1.0")
        });

        //[System.Xml.Serialization.XmlArrayItemAttribute("cancelledInvoice", IsNullable = false)]
        public CancelledInvoice[] cancelledInvoice { set; get; }

        public string Serialize()
        {
            XmlWriterSettings xmlWriterSettings = new XmlWriterSettings
            {
                Indent = true,
                OmitXmlDeclaration = false,
                Encoding = Encoding.UTF8
            };
            string xml = "";
            MemoryStream memoryStream = new MemoryStream();

            XmlWriter xmlWriter = XmlWriter.Create(memoryStream, xmlWriterSettings);

            var x = new System.Xml.Serialization.XmlSerializer(this.GetType());
            x.Serialize(xmlWriter, this);

            memoryStream.Position = 0;
            StreamReader sr = new StreamReader(memoryStream);
            xml = sr.ReadToEnd();

            xmlWriter.Close();
            xmlWriter.Dispose();
            sr.Close();
            sr.Dispose();
            memoryStream.Close();
            memoryStream.Dispose();

            var document = XDocument.Parse(xml);
            document.Descendants()
                    .Where(e1 => e1.IsEmpty || String.IsNullOrWhiteSpace(e1.Value))
                    .Remove();
            xml = document.ToString();

            return xml;
        }

        public static CancelledInvoicesDoc Deserialize(string xml)
        {
            var ser = new System.Xml.Serialization.XmlSerializer(typeof(CancelledInvoicesDoc));
            CancelledInvoicesDoc result;

            using (TextReader reader = new StringReader(xml))
            {
                result = ser.Deserialize(reader) as CancelledInvoicesDoc;
            }

            return result;
        }
    }

    public class RequestedProviderDoc
    {
        [XmlNamespaceDeclarations]
        public XmlSerializerNamespaces Namespaces = new XmlSerializerNamespaces(new System.Xml.XmlQualifiedName[]
        {
            //new System.Xml.XmlQualifiedName("xs", "http://www.w3.org/2001/XMLSchema-instance"),
            //new System.Xml.XmlQualifiedName("inv", "http://www.aade.gr/myDATA/invoice/v1.0"),
            new System.Xml.XmlQualifiedName("icls", "https://www.aade.gr/myDATA/incomeClassificaton/v1.0"),
            new System.Xml.XmlQualifiedName("ecls", "https://www.aade.gr/myDATA/expensesClassificaton/v1.0")
        });

        //[System.Xml.Serialization.XmlAttribute(AttributeName = "schemaLocation", Namespace = "http://www.w3.org/2001/XMLSchema-instance")]
        //public string SchemaLocation = "http://www.aade.gr/myDATA/invoice/v1.0/InvoicesDoc-v1.0.xsd";

        public Invoice[] invoice { set; get; }

        public string Serialize()
        {
            XmlWriterSettings xmlWriterSettings = new XmlWriterSettings
            {
                Indent = true,
                OmitXmlDeclaration = false,
                Encoding = Encoding.UTF8
            };
            string xml = "";
            MemoryStream memoryStream = new MemoryStream();

            XmlWriter xmlWriter = XmlWriter.Create(memoryStream, xmlWriterSettings);

            var x = new System.Xml.Serialization.XmlSerializer(this.GetType());
            x.Serialize(xmlWriter, this);

            memoryStream.Position = 0;
            StreamReader sr = new StreamReader(memoryStream);
            xml = sr.ReadToEnd();

            xmlWriter.Close();
            xmlWriter.Dispose();
            sr.Close();
            sr.Dispose();
            memoryStream.Close();
            memoryStream.Dispose();


            return xml;
        }

        public static RequestedProviderDoc Deserialize(string xml)
        {
            var ser = new System.Xml.Serialization.XmlSerializer(typeof(RequestedProviderDoc));
            RequestedProviderDoc result;

            using (TextReader reader = new StringReader(xml))
            {
                result = ser.Deserialize(reader) as RequestedProviderDoc;
            }

            return result;
        }
    }
}
