using Exedron.MyData.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Exedron.MyData
{
    public class RequestHelpers
    {
        public static string AsXml(Interfaces.IRequestMember member)
        {
            var interfaces = member.GetType().GetInterfaces();
            
            return "";
        }

        public static string ToString(decimal d, int decimalPlaces = 2)
        {
            if(System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator == ",")
                return d.ToString("N" + decimalPlaces.ToString()).Replace(".", "").Replace(",", ".");
            else
                return d.ToString("N" + decimalPlaces.ToString()).Replace(",", "");
        }

        public static string PrintXML(string xml)
        {
            string result = "";

            MemoryStream mStream = new MemoryStream();
            XmlTextWriter writer = new XmlTextWriter(mStream, Encoding.Unicode);
            XmlDocument document = new XmlDocument();

            try
            {
                // Load the XmlDocument with the XML.
                document.LoadXml(xml);

                writer.Formatting = Formatting.Indented;

                // Write the XML into a formatting XmlTextWriter
                document.WriteContentTo(writer);
                writer.Flush();
                mStream.Flush();

                // Have to rewind the MemoryStream in order to read
                // its contents.
                mStream.Position = 0;

                // Read MemoryStream contents into a StreamReader.
                StreamReader sReader = new StreamReader(mStream);

                // Extract the text from the StreamReader.
                string formattedXml = sReader.ReadToEnd();

                result = formattedXml;
            }
            catch (XmlException)
            {
                // Handle the exception
            }

            mStream.Close();
            writer.Close();

            return result;
        }

        public static VATCategoryEnum GetVATCategory(decimal vat)
        {
            if (vat == 0)
                return VATCategoryEnum.NoVAT;

            VATCategoryEnum cat;
            string[] names = Enum.GetNames(typeof(VATCategoryEnum));
            var values = Enum.GetValues(typeof(VATCategoryEnum));
            for (int i=0; i < names.Length; i++)
            {
                var name = names[i];
                string vatStr = vat.ToString("N0");
                if (name.Contains(vatStr))
                {
                    return (VATCategoryEnum)values.GetValue(i);
                }
            }
            return VATCategoryEnum.NoVAT;
        }
    }
}
