using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exedron.MyData.Interfaces
{
    public interface IInvoice: IRequestMember
    {
        string Uid { set; get; }
        long Mark { set; get; }
        long CanceledByMark { set; get; }
        string AuthenticationCode { set; get; }
        IPartyType Issuer { set; get; }
        IPartyType CounterPart { set; get; }
        IInvoiceHeader InvoiceHeader { set; get; }
        IPaymentMethodDetailType[] PaymentMethods { set; get; }
        IInvoiceRowType[] InvoiceDetails { set; get; }
        ITaxTotal[] TaxesTotals { set; get; }
        IInvoiceSummary InvoiceSummary { set; get; }
    }
}
