namespace ASFuel.AutoPay.Common
{
	public class GetDeviceInfoResponseMessage : MessageDescriptor
	{
		public FuelTypeModel[] FuelTypes { get; set; }

		public GetDeviceInfoResponseMessage()
		{
		}

		public GetDeviceInfoResponseMessage(MessageDescriptor sourceMessage)
			: base(sourceMessage)
		{
			base.MessageType = "GetDeviceInfoResponseMessage";
			base.MessageKind = MessageKindEnum.GetDeviceInfo;
		}
	}
}
