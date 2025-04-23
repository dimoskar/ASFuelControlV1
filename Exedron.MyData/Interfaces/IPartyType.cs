using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exedron.MyData.Interfaces
{
    public interface IPartyType : IRequestMember
    {
        string VATNumber { set; get; }
        string Country { set; get; }
        int Branch { set; get; }
        string Name { set; get; }
        bool KeepName { set; get; }
        IAddressType Address { set; get; }
    }
}
