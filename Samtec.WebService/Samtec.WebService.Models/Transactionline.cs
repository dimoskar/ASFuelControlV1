namespace Samtec.WebService.Models
{
	public class Transactionline
	{
		public int LineNo { get; set; }

		public int ClassCategory { get; set; }

		public int FuelCode { get; set; }

		public string Code { get; set; }

		public string Description { get; set; }

		public decimal ItemAmount { get; set; }

		public int MeasurementUnit { get; set; }

		public decimal SaleQty { get; set; }

		public decimal NetAmount { get; set; }

		public decimal VatAmount { get; set; }

		public decimal GrossAmount { get; set; }

		public int DMType { get; set; }

		public decimal DMValue { get; set; }

		public string VatRate { get; set; }

		public int VatExCategory { get; set; }

		public int FeeCategory { get; set; }

		public string PrintLine { get; set; }
	}
}
