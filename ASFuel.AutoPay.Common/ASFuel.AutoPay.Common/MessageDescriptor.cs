using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace ASFuel.AutoPay.Common
{
	public class MessageDescriptor : IComparable<MessageDescriptor>, IEquatable<MessageDescriptor>
	{
		private ClientDescriptor transmitter = null;

		private ClientDescriptor reciever = null;

		public bool IsResponse { get; set; }

		public Guid MessageId { get; set; }

		public Guid ReferenceId { get; set; }

		public RouteDescriptor Route { get; set; }

		public MessageKindEnum MessageKind { get; set; }

		public string MessageType { get; set; }

		[XmlIgnore]
		public ClientDescriptor Transmitter
		{
			get
			{
				if (transmitter == null && Route != null)
				{
					transmitter = new ClientDescriptor
					{
						EndPointType = Route.Transmitter,
						Id = Route.TransmitterId,
						Ip = Route.TrasmitterAddress,
						Port = Route.TrasmitterPort
					};
				}
				return transmitter;
			}
			set
			{
				if (value == null)
				{
					transmitter = new ClientDescriptor();
					return;
				}
				if (transmitter == null)
				{
					transmitter = new ClientDescriptor();
				}
				transmitter.EndPointType = value.EndPointType;
				transmitter.Id = value.Id;
				transmitter.Ip = value.Ip;
				transmitter.Port = value.Port;
				Route.Transmitter = value.EndPointType;
				Route.TransmitterId = value.Id;
				Route.TrasmitterAddress = value.Ip;
				Route.TrasmitterPort = value.Port;
			}
		}

		[XmlIgnore]
		public ClientDescriptor Reciever
		{
			get
			{
				if (reciever == null && Route != null)
				{
					reciever = new ClientDescriptor
					{
						EndPointType = Route.Reciever,
						Id = Route.RevieverId,
						Ip = Route.RecieverAddress,
						Port = Route.RecieverPort
					};
				}
				return reciever;
			}
			set
			{
				if (value == null)
				{
					reciever = new ClientDescriptor();
					return;
				}
				if (reciever == null)
				{
					reciever = new ClientDescriptor();
				}
				reciever.EndPointType = value.EndPointType;
				reciever.Id = value.Id;
				reciever.Ip = value.Ip;
				reciever.Port = value.Port;
				Route.Reciever = value.EndPointType;
				Route.RevieverId = value.Id;
				Route.RecieverAddress = value.Ip;
				Route.RecieverPort = value.Port;
			}
		}

		[XmlIgnore]
		public Type ResponseMessageType { get; set; }

		[XmlIgnore]
		public DateTime SentDateTime { get; set; }

		[XmlIgnore]
		public DateTime LastSentDateTime { get; set; }

		public string ResponseMessageTypeName
		{
			get
			{
				return (ResponseMessageType == null) ? "" : ResponseMessageType.FullName;
			}
			set
			{
				if (value == null || value == "")
				{
					ResponseMessageType = null;
				}
				else
				{
					ResponseMessageType = GetType().Assembly.GetType(value);
				}
			}
		}

		public MessageDescriptor()
		{
			MessageId = Guid.NewGuid();
			ResponseMessageType = typeof(AckMessage);
			Route = new RouteDescriptor();
			MessageType = "MessageDescriptor";
		}

		public MessageDescriptor(Type responseType)
		{
			MessageId = Guid.NewGuid();
			ResponseMessageType = responseType;
			Route = new RouteDescriptor();
			MessageType = "MessageDescriptor";
		}

		public MessageDescriptor(MessageDescriptor sourceMessage)
		{
			ResponseMessageType = typeof(AckMessage);
			MessageId = Guid.NewGuid();
			Route = new RouteDescriptor();
			IsResponse = true;
			MessageType = "MessageDescriptor";
			Route = new RouteDescriptor
			{
				Reciever = sourceMessage.Route.Transmitter,
				Transmitter = sourceMessage.Route.Reciever,
				RevieverId = sourceMessage.Route.TransmitterId,
				TransmitterId = sourceMessage.Route.RevieverId,
				TrasmitterAddress = sourceMessage.Route.RecieverAddress,
				TrasmitterPort = sourceMessage.Route.RecieverPort,
				RecieverAddress = sourceMessage.Route.TrasmitterAddress,
				RecieverPort = sourceMessage.Route.TrasmitterPort
			};
		}

		public bool Equals(MessageDescriptor other)
		{
			if (other == null)
			{
				return false;
			}
			if (MessageType != other.MessageType)
			{
				return false;
			}
			if (MessageKind != other.MessageKind)
			{
				return false;
			}
			return Route.Equals(other.Route);
		}

		public static string SerializeMessage(MessageDescriptor msg)
		{
			XmlSerializer xmlSerializer = new XmlSerializer(msg.GetType());
			using (StringWriter stringWriter = new StringWriter())
			{
				xmlSerializer.Serialize(stringWriter, msg);
				return stringWriter.ToString();
			}
		}

		public static MessageDescriptor[] Deserialize(string xml)
		{
			MessageDescriptor messageDescriptor = null;
			string[] array = xml.Split(new string[1] { "<?xml version=\"1.0\" encoding=\"utf-16\"?>" }, StringSplitOptions.RemoveEmptyEntries);
			List<MessageDescriptor> list = new List<MessageDescriptor>();
			string[] array2 = array;
			foreach (string text in array2)
			{
				int num = text.LastIndexOf(">");
				string text2 = text.Substring(0, num + 1);
				XDocument xDocument = XDocument.Parse(text2);
				try
				{
					XElement xElement = (from d in xDocument.Descendants()
						where d.Name == "MessageType"
						select d).FirstOrDefault();
					if (xElement == null)
					{
						continue;
					}
					XmlSerializer xmlSerializer = null;
					switch (xElement.Value)
					{
					case "AckMessage":
						xmlSerializer = new XmlSerializer(typeof(AckMessage));
						break;
					case "MessageDescriptor":
						xmlSerializer = new XmlSerializer(typeof(MessageDescriptor));
						break;
					case "ErrorMessage":
						xmlSerializer = new XmlSerializer(typeof(ErrorMessage));
						break;
					case "SendSaleMessage":
						xmlSerializer = new XmlSerializer(typeof(SendSaleMessage));
						break;
					case "StatusResponseMessage":
						xmlSerializer = new XmlSerializer(typeof(StatusResponseMessage));
						break;
					case "GetDeviceInfoResponseMessage":
						xmlSerializer = new XmlSerializer(typeof(GetDeviceInfoResponseMessage));
						break;
					case "GetNozzleInfoResponseMessage":
						xmlSerializer = new XmlSerializer(typeof(GetNozzleInfoResponseMessage));
						break;
					}
					if (xmlSerializer != null)
					{
						using (TextReader textReader = new StringReader(text2))
						{
							messageDescriptor = xmlSerializer.Deserialize(textReader) as MessageDescriptor;
						}
					}
					if (messageDescriptor != null)
					{
						list.Add(messageDescriptor);
					}
				}
				catch (Exception)
				{
				}
			}
			return list.ToArray();
		}

		public static MessageDescriptor Deserialize(string xml, Guid messageId)
		{
			MessageDescriptor[] source = Deserialize(xml);
			return source.Where((MessageDescriptor m) => m.MessageId == messageId).FirstOrDefault();
		}

		public MessageDescriptor CreateResponse()
		{
			if (ResponseMessageType == null)
			{
				return null;
			}
			MessageDescriptor messageDescriptor = Activator.CreateInstance(ResponseMessageType, this) as MessageDescriptor;
			messageDescriptor.Transmitter = Reciever;
			messageDescriptor.Reciever = Transmitter;
			messageDescriptor.ReferenceId = MessageId;
			if (messageDescriptor.GetType() == typeof(AckMessage))
			{
				((AckMessage)messageDescriptor).SourceMessageKind = MessageKind;
			}
			messageDescriptor.ResponseMessageType = null;
			return messageDescriptor;
		}

		public int Compare(MessageDescriptor x, MessageDescriptor y)
		{
			if (x == null && y == null)
			{
				return 0;
			}
			if (x.MessageType != y.MessageType)
			{
				return x.MessageType.CompareTo(y.MessageType);
			}
			if (x.MessageKind != y.MessageKind)
			{
				return x.MessageKind.CompareTo(y.MessageKind);
			}
			if (x.Route != y.Route)
			{
				return x.Route.CompareTo(y.Route);
			}
			return 0;
		}

		public int CompareTo(MessageDescriptor other)
		{
			return Compare(this, other);
		}
	}
}
