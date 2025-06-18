namespace ASFuel.AutoPay.Common
{
	public class FuelTypeModel
	{
		public string FuelCode { get; set; }

		public string Description { get; set; }

		public NozzleTypeModel[] Nozzles { get; set; }
	}
}
