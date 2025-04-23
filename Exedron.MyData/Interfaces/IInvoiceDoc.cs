using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exedron.MyData.Interfaces
{
    public interface IInvoiceDoc : IRequestMember
    {
        IInvoice[] Invoices { set; get; }
    }
}
