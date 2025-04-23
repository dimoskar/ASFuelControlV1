namespace Exedron.Invoicing.Models
{
	public class Invoicelineallowance
	{
		public float baseAmount { get; set; }

		public float amount { get; set; }

		public float percentage { get; set; }

		public object reason { get; set; }

		public string reasonCode { get; set; }
	}
}
