using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Exedron.MyData.Interfaces;

namespace Exedron.MyData.InvoiceModels
{
    public class TaxTotals : ITaxTotal
    {
        public TaxTypeEnum TaxType { get; set; }
        public int TaxCategory { get; set; }
        public decimal UnderlyingValue { get; set; }
        public decimal TaxAmount { get; set; }
        public int Id { get; set; }

        public string AsXml()
        {
            return "";
        }
    }
}
