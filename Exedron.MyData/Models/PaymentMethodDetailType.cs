using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Exedron.MyData.Models
{
    [XmlType(TypeName = "paymentMethodDetails")]
    public class PaymentMethodDetailType
    {
        public int type { set; get; }
        public decimal amount { set; get; }
        public string paymentMethodInfo { set; get; }
    }
}
