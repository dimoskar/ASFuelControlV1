using System;

namespace ASFuel.AutoPay.Common
{
	[Flags]
	public enum PaymentTypeEnum
	{
		None = 0,
		CreditCard = 1,
		Cash = 2,
		Credit = 4
	}
}
