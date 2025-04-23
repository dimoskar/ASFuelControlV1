using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.Common
{
    public interface IInvoicePrint
    {
        event EventHandler ThreadStopped;
        event EventHandler ThreadStarted;

        void EnqueueInvoiceData(Guid invoiceId);
        void PrintCopy(Guid invoiceId);
        void StartThread();
        void StopThread();
    }
}
