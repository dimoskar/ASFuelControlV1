using System;
using Exedron.Invoicing.Core.Models;
namespace Exedron.Invoicing.Core
{
    public sealed class IlydaPosSession
    {
        public Guid Id { get; set; }
        public Guid InvoiceId { get; set; }
        public bool TestMode { get; set; }
        public string IssuerVat { get; set; }
        public string Endpoint { get; set; }
        public string RequestJson { get; set; }
        public string ResponseJson { get; set; }
        public string State { get; set; }
        public string PaymentRequestJson { get; set; }
        public string PaymentResponseJson { get; set; }
        public string PaymentState { get; set; }
        public string CancelReason { get; set; }
    }

    public interface IIlydaPosStore
    {
        IDisposable Acquire(Guid invoiceId);
        IlydaPosSession Load(Guid invoiceId);
        void Save(IlydaPosSession session);
    }

}
