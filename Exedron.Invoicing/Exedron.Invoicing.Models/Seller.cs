namespace Exedron.Invoicing.Models
{
	public class Seller
	{
		public string sellerVatIdentifier { get; set; }

		public string sellerName { get; set; }

		public Sellercontact sellerContact { get; set; }

		public Sellerpostaladdress sellerPostalAddress { get; set; }

		public int branch { get; set; }
	}
}
