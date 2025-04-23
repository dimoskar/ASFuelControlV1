using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exedron.MyData.InvoiceModels
{
    public class IncomeClassification : Interfaces.IIncomeClassification
    {
        public string ClassificationType { get; set; }
        public string ClassificationCategory { get; set; }
        public decimal Amount { get; set; }
        public byte Id { get; set; }

        public string AsXml()
        {
            var xml = Properties.Resources.IncomeClassification;

            if(string.IsNullOrEmpty(this.ClassificationType))
                xml = xml.Replace("<icls:classificationType>[classificationtype]</icls:classificationType>", "");
            else
                xml = xml.Replace("[classificationtype]", this.ClassificationType);
            xml = xml.Replace("[classificationcategory]", this.ClassificationCategory);
            xml = xml.Replace("[amount]", RequestHelpers.ToString(this.Amount));
            if(this.Id > 0)
                xml = xml.Replace("[id]", this.Id.ToString("N0"));
            else
                xml = xml.Replace("[id]", "0");
            return xml;
        }
    }
}
