namespace Samtec.WebService.Models
{
	public class Doc
	{
		public Receiverheader ReceiverHeader { get; set; }

		public Invoicedetails InvoiceDetails { get; set; }

		public Transactionline[] TransactionLines { get; set; }

		public Payment[] Payments { get; set; }

		public Invoicefooter InvoiceFooter { get; set; }
	}
}
