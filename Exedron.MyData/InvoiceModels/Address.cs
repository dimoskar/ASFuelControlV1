using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exedron.MyData.InvoiceModels
{
    public class Address : Exedron.MyData.Interfaces.IAddressType
    {
        public string Street { set; get; }
        public string Number { set; get; }
        public string PostalCode { set; get; }
        public string City { set; get; }

        public void ReplaceNumber()
        {
            var numberPos = this.Street.LastIndexOf(" ");
            if (numberPos > 0)
            {
                string subString = this.Street.Substring(numberPos);
                var hasNumber = false;
                foreach (var ch in subString)
                {
                    int num = 0;
                    if (int.TryParse(ch.ToString(), out num))
                    {
                        hasNumber = true;
                        break;
                    }
                }
                if (hasNumber)
                {
                    this.Number = subString.Trim();
                    this.Street = this.Street.Substring(0, numberPos);
                }
                else
                    this.Number = "-";
            }
            else
            {
                this.Number = "-";
            }
        }

        public string AsXml()
        {
            string xml = Properties.Resources.AddressType;
            xml = xml.Replace("[street]", this.Street);
            xml = xml.Replace("[number]", this.Number);
            xml = xml.Replace("[postalcode]", this.PostalCode);
            xml = xml.Replace("[city]", this.City);
            return xml;
        }
    }
}
