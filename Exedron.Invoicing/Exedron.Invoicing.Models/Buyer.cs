namespace Exedron.Invoicing.Models
{
	public class Buyer
	{
		public Buyerpostaladdress buyerPostalAddress { get; set; }

		public string buyerVatIdentifier { get; set; }

		public string buyerTradingName { get; set; }

		public string buyerName { get; set; }

		public int buyerBranch { get; set; }
	}
}
