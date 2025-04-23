using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Exedron.MyData.Interfaces;

namespace Exedron.MyData.InvoiceModels
{
    public class Issuer : IPartyType
    {
        public string VATNumber { set; get; }
        public string Country { set; get; }
        public int Branch { set; get; }
        public string Name { set; get; }
        public bool KeepName { set; get; }
        public IAddressType Address { set; get; }

        public string AsXml()
        {
            string xml = Properties.Resources.PartyType;
            xml = xml.Replace("[partytype]", "issuer");
            xml = xml.Replace("[vatnumber]", this.VATNumber);
            if(!KeepName)
                xml = xml.Replace("<name>[name]</name>", "");
            else
                xml = xml.Replace("[name]", this.Name);
            xml = xml.Replace("[country]", this.Country);
            xml = xml.Replace("[branch]", this.Branch.ToString());
            
            if (this.Address == null)
                xml = xml.Replace("[address]", "");
            else
                xml = xml.Replace("[address]", this.Address.AsXml());
            return xml;
        }
    }
}
