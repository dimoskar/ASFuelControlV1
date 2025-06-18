namespace ASFuel.AutoPay.Common
{
	public class StatusResponseMessage : MessageDescriptor
	{
		public StatusEnum Status { get; set; }

		public StatusResponseMessage()
		{
		}

		public StatusResponseMessage(MessageDescriptor sourceMessage)
			: base(sourceMessage)
		{
			base.MessageType = "StatusResponseMessage";
			base.MessageKind = MessageKindEnum.GetStatus;
		}
	}
}
