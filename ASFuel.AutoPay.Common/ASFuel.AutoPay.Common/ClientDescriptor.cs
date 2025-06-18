using System;
using System.Net;

namespace ASFuel.AutoPay.Common
{
	public class ClientDescriptor : IEquatable<ClientDescriptor>
	{
		public EndPointEnum EndPointType { get; set; }

		public IPAddress Ip { get; set; }

		public int Port { get; set; }

		public int Id { get; set; }

		public bool Equals(ClientDescriptor other)
		{
			if (EndPointType != other.EndPointType)
			{
				return false;
			}
			if (Id != other.Id)
			{
				return false;
			}
			if (Ip.ToString() != other.Ip.ToString())
			{
				return false;
			}
			if (Port != other.Port)
			{
				return false;
			}
			return true;
		}
	}
}
