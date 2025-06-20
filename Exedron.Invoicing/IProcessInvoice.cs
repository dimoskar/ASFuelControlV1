using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exedron.Invoicing
{
    public interface IProcessInvoice
    {
        Models.InvoiceCreationResponse ProcessInvoice(Models.Invoice invoice);
        void SetSettings(Dictionary<string, string> settings);
    }
}
