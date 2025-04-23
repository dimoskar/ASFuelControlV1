using System;

namespace Exedron.Invoicing.Models
{
	public class AadeData
	{
		public bool aadeFuelInvoice { get; set; }

		public string aadeInvoiceTypeCode { get; set; }

		public bool aadeVatPaymentSuspension { get; set; }

		public int? aadeMovePurpose { get; set; }

		public DateTime? aadeDispatchTime { get; set; }

		public string aadeSupplyAccountNo { get; set; }

		public string aadeVehicleNumber { get; set; }

		public Incomeclassification[] incomeClassifications { get; set; }

		public Invoicerowtype[] invoiceRowTypes { get; set; }
	}
}
