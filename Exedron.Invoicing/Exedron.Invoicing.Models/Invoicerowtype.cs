namespace Exedron.Invoicing.Models
{
	public class Invoicerowtype
	{
		public object deductionsAmount { get; set; }

		public object dienergia { get; set; }

		public object discountOption { get; set; }

		public object expensesClassification { get; set; }

		public object feesAmount { get; set; }

		public object feesPercentCategory { get; set; }

		public Incomeclassification[] incomeClassification { get; set; }

		public object fuelCode { get; set; }

		public object invoiceDetailType { get; set; }

		public object itemDescr { get; set; }

		public object lineComments { get; set; }

		public int lineNumber { get; set; }

		public object measurementUnit { get; set; }

		public float netValue { get; set; }

		public object otherTaxesAmount { get; set; }

		public object otherTaxesPercentCategory { get; set; }

		public object quantity { get; set; }

		public object quantity15 { get; set; }

		public object recType { get; set; }

		public object stampDutyAmount { get; set; }

		public object stampDutyPercentCategory { get; set; }

		public float vatAmount { get; set; }

		public int vatCategory { get; set; }

		public object vatExemptionCategory { get; set; }

		public object withheldAmount { get; set; }

		public object withheldPercentCategory { get; set; }
	}
}
