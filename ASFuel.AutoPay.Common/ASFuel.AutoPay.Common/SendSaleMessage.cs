namespace ASFuel.AutoPay.Common
{
	public class SendSaleMessage : MessageDescriptor
	{
		public OpenSale SaleData { get; set; }

		public SendSaleMessage()
		{
			base.MessageType = "SendSaleMessage";
			base.MessageKind = MessageKindEnum.Sale;
		}
	}
}
