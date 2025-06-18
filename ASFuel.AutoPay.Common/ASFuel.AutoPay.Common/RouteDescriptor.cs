using System;
using System.Net;
using System.Xml.Serialization;

namespace ASFuel.AutoPay.Common
{
	public class RouteDescriptor : IComparable<RouteDescriptor>, IEquatable<RouteDescriptor>
	{
		public EndPointEnum Transmitter { get; set; }

		public int TransmitterId { get; set; }

		public string TrasmitterAddressValue
		{
			get
			{
				return (TrasmitterAddress == null) ? "" : TrasmitterAddress.ToString();
			}
			set
			{
				IPAddress address = null;
				if (IPAddress.TryParse(value, out address))
				{
					TrasmitterAddress = address;
				}
			}
		}

		[XmlIgnore]
		public IPAddress TrasmitterAddress { get; set; }

		public int TrasmitterPort { get; set; }

		public EndPointEnum Reciever { get; set; }

		public int RevieverId { get; set; }

		public string RecieverAddressValue
		{
			get
			{
				return (RecieverAddress == null) ? "" : RecieverAddress.ToString();
			}
			set
			{
				IPAddress address = null;
				if (IPAddress.TryParse(value, out address))
				{
					RecieverAddress = address;
				}
			}
		}

		[XmlIgnore]
		public IPAddress RecieverAddress { get; set; }

		public int RecieverPort { get; set; }

		public bool Equals(RouteDescriptor other)
		{
			if (Transmitter != other.Transmitter)
			{
				return false;
			}
			if (Reciever != other.Reciever)
			{
				return false;
			}
			if (RevieverId != other.RevieverId)
			{
				return false;
			}
			if (TransmitterId != other.TransmitterId)
			{
				return false;
			}
			if (TrasmitterAddress != other.TrasmitterAddress)
			{
				return false;
			}
			if (TrasmitterPort != other.TrasmitterPort)
			{
				return false;
			}
			if (RecieverAddress != other.RecieverAddress)
			{
				return false;
			}
			if (RecieverPort != other.RecieverPort)
			{
				return false;
			}
			return true;
		}

		public bool EqualsTransmitter(ClientDescriptor other)
		{
			if (Transmitter != other.EndPointType)
			{
				return false;
			}
			if (TransmitterId != other.Id)
			{
				return false;
			}
			if (TrasmitterAddress.ToString() != other.Ip.ToString())
			{
				return false;
			}
			if (TrasmitterPort != other.Port)
			{
				return false;
			}
			return true;
		}

		public bool EqualsReciever(ClientDescriptor other)
		{
			if (Reciever != other.EndPointType)
			{
				return false;
			}
			if (RevieverId != other.Id)
			{
				return false;
			}
			if (RecieverAddress.ToString() != other.Ip.ToString())
			{
				return false;
			}
			if (RecieverPort != other.Port)
			{
				return false;
			}
			return true;
		}

		public int Compare(RouteDescriptor x, RouteDescriptor y)
		{
			if (x.Transmitter != y.Transmitter)
			{
				return x.Transmitter.CompareTo(y.Transmitter);
			}
			if (x.Reciever != y.Reciever)
			{
				return x.Reciever.CompareTo(y.Reciever);
			}
			if (x.RevieverId != y.RevieverId)
			{
				return x.RevieverId.CompareTo(y.RevieverId);
			}
			if (x.TransmitterId != y.TransmitterId)
			{
				return x.TransmitterId.CompareTo(y.TransmitterId);
			}
			if (x.TrasmitterAddress != y.TrasmitterAddress)
			{
				return x.TrasmitterAddress.ToString().CompareTo(y.TrasmitterAddress.ToString());
			}
			if (x.TrasmitterPort != y.TrasmitterPort)
			{
				return x.TrasmitterPort.CompareTo(y.TrasmitterPort);
			}
			if (x.RecieverAddress != y.RecieverAddress)
			{
				return x.RecieverAddress.ToString().CompareTo(y.RecieverAddress.ToString());
			}
			if (x.RecieverPort != y.RecieverPort)
			{
				return x.RecieverPort.CompareTo(y.RecieverPort);
			}
			return 0;
		}

		public int CompareTo(RouteDescriptor other)
		{
			return Compare(this, other);
		}
	}
}
