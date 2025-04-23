using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exedron.MyData.Models
{
    public class PartyType
    {
        public string vatNumber { set; get; }
        public string country { set; get; }
        public int branch { set; get; }
        public string name { set; get; }
        public Address address { set; get; }
    }
}
