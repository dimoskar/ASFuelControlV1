using System;

namespace ASFuel.AutoPay.Common
{
	public class OpenSale
	{
		private decimal presetAmount = default(decimal);

		private decimal salesAmount = default(decimal);

		public int SaleNumber { get; set; }

		public DateTime DateTimeStarted { get; set; }

		public DateTime ExipationDate { get; set; }

		public int NozzleId { get; set; }

		public int DispenserId { get; set; }

		public string NozzleDescription { get; set; }

		public string FuelTypeDesc { get; set; }

		public decimal PresetAmount
		{
			get
			{
				return presetAmount;
			}
			set
			{
				presetAmount = value;
			}
		}

		public decimal PresetVolume { get; set; }

		public decimal SalesAmount
		{
			get
			{
				return salesAmount;
			}
			set
			{
				salesAmount = value;
			}
		}

		public decimal SalesVolume { get; set; }

		public decimal UnitPrice { get; set; }

		public string VehiclePlates { get; set; }

		public SaleStateEnum SaleState { get; set; }

		public string CardId { get; set; }

		public PaymentTypeEnum PaymentType { get; set; }

		public Guid SaleId { get; set; }

		public int VendingMachineId { get; set; }
	}
}
