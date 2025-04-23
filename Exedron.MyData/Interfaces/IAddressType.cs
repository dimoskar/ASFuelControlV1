using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exedron.MyData.Interfaces
{
    public interface IAddressType : IRequestMember
    {
        string Street { set; get; }
        string Number { set; get; }
        string PostalCode { set; get; }
        string City { set; get; }
    }
}
