namespace Exedron.Invoicing.Models
{
	public class Doctotal
	{
		public float amountDueForPayment { get; set; }

		public float documentLevelAllowancesSum { get; set; }

		public float documentLevelChargesSum { get; set; }

		public float exchangeRate { get; set; }

		public float invoiceLinesNetAmountSum { get; set; }

		public float invoiceTotalAmountWithVat { get; set; }

		public float invoiceTotalVatAmount { get; set; }

		public object invoiceTotalVatAmountInAccountingCurrency { get; set; }

		public float invoiceTotalWithoutVat { get; set; }

		public float paidAmount { get; set; }

		public float roundingAmount { get; set; }

		public AadeDocTotals aadeDocTotals { get; set; }
	}
}
