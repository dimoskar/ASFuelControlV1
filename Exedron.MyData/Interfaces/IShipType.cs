using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exedron.MyData.Interfaces
{
    public interface IShipType : IRequestMember
    {
        string ApplicationId { set; get; }
        DateTime ApplicationDate { set; get; }
        string DOY { set; get; }
        string ShipId { set; get; }
    }
}
