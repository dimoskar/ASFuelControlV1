namespace Exedron.Invoicing.Models
{
	public class Pricedetails
	{
		public float itemNetPrice { get; set; }

		public float itemPriceDiscount { get; set; }

		public object itemGrossPrice { get; set; }

		public float itemPriceBaseQuantity { get; set; }

		public string itemPriceBaseQuantityUnitsCode { get; set; }
	}
}
