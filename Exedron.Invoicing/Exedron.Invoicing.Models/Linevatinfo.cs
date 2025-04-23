namespace Exedron.Invoicing.Models
{
	public class Linevatinfo
	{
		public string vatCategoryCode { get; set; }

		public float vatAmount { get; set; }

		public float vatRate { get; set; }

		public Aadevatdata1 aadeVatData { get; set; }
	}
}
