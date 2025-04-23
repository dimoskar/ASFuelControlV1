namespace Samtec.WebService.Models
{
	public class Invoicedetails
	{
		public string InvoiceUID { get; set; }

		public int InvoiceType { get; set; }

		public int PrintDevice { get; set; }

		public int ReqForToken { get; set; }

		public int? CancelInvType { get; set; }

		public int? CancelDevDailyNum { get; set; }

		public string CancelInvNo { get; set; }

		public string CancelInvSeries { get; set; }

		public string InvoiceNo { get; set; }

		public string InvoiceSeries { get; set; }

		public float InvoiceTotal { get; set; }

		public string Operator { get; set; }

		public string Machine { get; set; }

		public int GasStationLicNum { get; set; }

		public int GasStationInstalNum { get; set; }

		public float InvWithholdingTaxTotal { get; set; }

		public string PrintLine { get; set; }
	}
}
