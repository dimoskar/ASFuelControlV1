namespace ASFuel.AutoPay.Common
{
	public class ErrorMessage : MessageDescriptor
	{
		public int ErrorCode { get; set; }

		public string ErrorDescription { get; set; }

		public ErrorMessage(MessageDescriptor sourceMessage)
			: base(sourceMessage)
		{
			base.MessageType = "ErrorMessage";
			base.MessageKind = MessageKindEnum.Error;
		}
	}
}
