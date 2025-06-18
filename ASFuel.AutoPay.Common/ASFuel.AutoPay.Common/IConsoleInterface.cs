namespace ASFuel.AutoPay.Common
{
	public interface IConsoleInterface
	{
		int VendingMachineId { get; set; }

		OpenSale GetNextSale(int dispenserNumber);

		bool TranactionCompleted(int dispenserNumber, int nozzleNumber, OpenSale saleData);
	}
}
