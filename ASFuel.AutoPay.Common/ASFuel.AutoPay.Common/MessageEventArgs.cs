using System;

namespace ASFuel.AutoPay.Common
{
	public class MessageEventArgs : EventArgs
	{
		public MessageDescriptor Message { get; private set; }

		public MessageEventArgs(MessageDescriptor msg)
		{
			Message = msg;
		}
	}
}
