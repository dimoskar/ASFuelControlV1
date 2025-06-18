namespace ASFuel.AutoPay.Common
{
	public class AckMessage : MessageDescriptor
	{
		public MessageKindEnum SourceMessageKind { get; set; }

		public AckMessage()
		{
			base.ResponseMessageType = null;
			base.MessageType = "AckMessage";
			base.MessageKind = MessageKindEnum.Ack;
		}

		public AckMessage(MessageDescriptor sourceMessage)
			: base(sourceMessage)
		{
			base.ResponseMessageType = null;
			base.MessageType = "AckMessage";
			base.MessageKind = MessageKindEnum.Ack;
			SourceMessageKind = sourceMessage.MessageKind;
		}
	}
}
