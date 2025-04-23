namespace Exedron.Invoicing.Models
{
	public class Invoiceline
	{
		public int lineNumber { get; set; }

		public string note { get; set; }

		public float invoicedQuantity { get; set; }

		public string invoicedQuantityUnits { get; set; }

		public float netAmount { get; set; }

		public float discountPercentage1 { get; set; }

		public float discountPercentage2 { get; set; }

		public float discountPercentage3 { get; set; }

		public float discountAmount { get; set; }

		public float discountTotalAmount { get; set; }

		public Iteminfo itemInfo { get; set; }

		public Linevatinfo lineVatInfo { get; set; }

		public Pricedetails priceDetails { get; set; }

		public Invoicelineallowance[] invoiceLineAllowances { get; set; }

		public Itemclassificationidentifier[] itemClassificationIdentifiers { get; set; }
	}
}
