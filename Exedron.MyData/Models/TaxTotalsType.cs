using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exedron.MyData.Models
{
    public class TaxTotalsType
    {
        public byte taxType { set; get; }
        public byte? taxCategory { set; get; }
        public decimal? underlyingValue { set; get; }
        public decimal taxAmount { set; get; }
        public byte? id { set; get; }
    }
}
