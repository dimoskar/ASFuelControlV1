using System;
using System.IO;
using System.Net;
using System.Xml;
using System.Xml.Schema;
using Exedron.Invoicing.Core;

namespace Exedron.Invoicing.Adapters
{
    public static class MyDataSchema
    {
        private static readonly Lazy<XmlSchemaSet> Schemas = new Lazy<XmlSchemaSet>(() => {
            var resolver = new EmbeddedResolver();
            var schemas = new XmlSchemaSet { XmlResolver = resolver };
            using (var stream = (Stream)resolver.GetEntity(new Uri(EmbeddedResolver.Root + "InvoicesDoc-v2.0.0.xsd"),null,null))
            using (var reader = XmlReader.Create(stream,new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null },EmbeddedResolver.Root + "InvoicesDoc-v2.0.0.xsd"))
                schemas.Add(null,reader);
            schemas.Compile();
            return schemas;
        });
        public static void Validate(string xml)
        {
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null,
                ValidationType = ValidationType.Schema, Schemas = Schemas.Value, MaxCharactersInDocument = 32 * 1024 * 1024 };
            settings.ValidationEventHandler += (sender,args) => { throw new ProviderInvoiceValidationException("myDATA v2.0.0: " + args.Message); };
            using (var input = new StringReader(xml)) using (var reader = XmlReader.Create(input,settings)) while (reader.Read()) { }
        }
        private sealed class EmbeddedResolver : XmlResolver
        {
            internal const string Root = "https://schemas.exedron.invalid/mydata/v2.0.0/";
            public override ICredentials Credentials { set {} }
            public override object GetEntity(Uri uri,string role,Type type)
            {
                if (!uri.AbsoluteUri.StartsWith(Root,StringComparison.Ordinal)) throw new XmlException("Only embedded myDATA schemas can be resolved.");
                var name = uri.AbsoluteUri.Substring(Root.Length);
                if (name.Contains("/") || name.Contains("..") || !name.EndsWith(".xsd",StringComparison.Ordinal)) throw new XmlException("Invalid schema reference.");
                return typeof(MyDataSchema).Assembly.GetManifestResourceStream("Exedron.Invoicing.Adapters.Schemas.MyDataV2." + name)
                    ?? throw new XmlException("Missing embedded myDATA schema: " + name);
            }
        }
    }
}
