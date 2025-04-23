namespace Exedron.Invoicing.Models
{
	public class Vatbreakdown
	{
		public Aadevatdata aadeVatData { get; set; }

		public string categoryCode { get; set; }

		public float categoryRate { get; set; }

		public float categoryTaxAmount { get; set; }

		public float categoryTaxableAmount { get; set; }

		public object exemptionReasonCode { get; set; }

		public object exemptionReasonText { get; set; }
	}
}
