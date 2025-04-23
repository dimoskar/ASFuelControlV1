using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exedron.MyData.Interfaces
{
    public interface IInvoiceHeader : IRequestMember
    {
        string Series { set; get; }
        string AA { set; get; }
        DateTime IssueDate { set; get; }
        string InvoiceType { set; get; }
        bool VATPaymentSuspension { set; get; }
        string Currency { set; get; }
        decimal ExchangeRate { set; get; }
        long[] CorrelatedInvoices { set; get; }
        bool SelfPricing { set; get; }
        DateTime DispatchDateTime { set; get; }
        string VehicleNumber { set; get; }
        MovePurposeEnum MovePurpose { set; get; }
        IOtherDeliveryNoteHeader OtherDeliveryNoteHeader { set; get; }
    }

    public interface IOtherDeliveryNoteHeader : IRequestMember
    {
        IAddressType LoadingAddress { set; get; }
        IAddressType DeliveryAddress { set; get; }
    }

    public enum MovePurposeEnum
    {
        Sales = 1,
        ThirdPartySales,
        Sampling,
        Exhibition,
        Return,
        Safekeeping,
        ProcessingAssembling,
        Internal
    }
}
