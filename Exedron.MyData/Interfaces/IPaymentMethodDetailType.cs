using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exedron.MyData.Interfaces
{
    public interface IPaymentMethodDetailType : IRequestMember
    {
        PaymentMethodEnum Type { set; get; }
        decimal Amount { set; get; }
        string PaymentMethodInfo { set; get; }
    }

    public enum PaymentMethodEnum
    {
        HomelandAccount = 1,
        ForeignAccount = 2,
        Cash = 3,
        Check = 4,
        Credit = 5
    }
}
