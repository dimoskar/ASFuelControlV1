using System.Collections.Generic;
using Exedron.Invoicing.Models;

namespace Exedron.Invoicing
{
	public interface IProcessInvoice
	{
		InvoiceCreationResponse ProcessInvoice(Invoice invoice);

		void SetSettings(Dictionary<string, string> settings);
	}
}
