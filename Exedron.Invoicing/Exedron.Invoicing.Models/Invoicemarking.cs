namespace Exedron.Invoicing.Models
{
	public class Invoicemarking
	{
		public string mark { get; set; }

		public string providerUrl { get; set; }

		public string qrCode { get; set; }

		public string invoiceId { get; set; }

		public string verificationHash { get; set; }

		public string invoiceIdentifier { get; set; }

		public bool previouslySubmitted { get; set; }
	}
}
