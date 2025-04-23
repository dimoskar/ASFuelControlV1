using System;

namespace Exedron.Invoicing.Models
{
	public class Invoice
	{
		public AadeData aadeData { get; set; }

		public bool b2g { get; set; }

		public Seller seller { get; set; }

		public string invoiceTypeCode { get; set; }

		public Delivery delivery { get; set; }

		public Vatbreakdown[] vatBreakdowns { get; set; }

		public Credittransfer[] creditTransfers { get; set; }

		public DateTime creationDate { get; set; }

		public Invoiceline[] invoiceLines { get; set; }

		public DateTime invoiceIssueDate { get; set; }

		public string paymentTerms { get; set; }

		public string invoiceCurrencyCode { get; set; }

		public Buyer buyer { get; set; }

		public Additionalsupportdoc[] additionalSupportDocs { get; set; }

		public object docLevelAllowances { get; set; }

		public object docLevelCharges { get; set; }

		public Doctotal docTotal { get; set; }

		public string serialNumber { get; set; }

		public string seriesNumber { get; set; }

		public PaymentMethod[] paymentMethods { get; set; }
	}
}
