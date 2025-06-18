namespace ASFuel.AutoPay.Common
{
	public class GetNozzleInfoResponseMessage : MessageDescriptor
	{
		public NozzleInfo[] Nozzles { get; set; }

		public GetNozzleInfoResponseMessage()
		{
		}

		public GetNozzleInfoResponseMessage(MessageDescriptor sourceMessage)
			: base(sourceMessage)
		{
			base.MessageType = "GetNozzleInfoResponseMessage";
			base.MessageKind = MessageKindEnum.GetNozzleInfo;
		}
	}
}
