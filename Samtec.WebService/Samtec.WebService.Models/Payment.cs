namespace Samtec.WebService.Models
{
	public class Payment
	{
		public int Type { get; set; }

		public object Description { get; set; }

		public object EftposTID { get; set; }

		public int EftposTransType { get; set; }

		public decimal Value { get; set; }

		public string PrintLine { get; set; }
	}
}
