namespace Exedron.Invoicing.Models
{
	public class InvoiceCreationResponse
	{
		public Invoicemarking invoiceMarking { get; set; }

		public Error[] errors { get; set; }
	}
}
