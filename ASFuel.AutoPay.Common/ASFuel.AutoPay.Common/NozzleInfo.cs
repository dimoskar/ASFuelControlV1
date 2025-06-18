namespace ASFuel.AutoPay.Common
{
	public class NozzleInfo
	{
		public int NozzleNumber { get; set; }

		public int DispenserNumber { get; set; }

		public decimal UnitPrice { get; set; }

		public decimal RemainingTankVolume { get; set; }

		public NozzleStatusEnum Status { get; set; }
	}
}
