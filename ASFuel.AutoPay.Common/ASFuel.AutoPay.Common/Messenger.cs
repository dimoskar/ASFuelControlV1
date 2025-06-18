using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using SimpleTCP;

namespace ASFuel.AutoPay.Common
{
	public class Messenger
	{
		private SimpleTcpServer server = new SimpleTcpServer();

		private SimpleTcpClient client = new SimpleTcpClient();

		private ConcurrentDictionary<Guid, MessageDescriptor> messages = new ConcurrentDictionary<Guid, MessageDescriptor>();

		private ConcurrentDictionary<Guid, MessageDescriptor> sentMessages = new ConcurrentDictionary<Guid, MessageDescriptor>();

		private ConcurrentDictionary<Guid, MessageDescriptor> recievedMessages = new ConcurrentDictionary<Guid, MessageDescriptor>();

		private List<ClientDescriptor> recievers = new List<ClientDescriptor>();

		private ClientDescriptor localClient = new ClientDescriptor();

		private Thread th;

		private bool cancel = false;

		public IPAddress IP { get; private set; }

		public int Port { get; private set; }

		public int ClientId { get; set; }

		public EndPointEnum EndPointLocation { get; set; }

		public bool IsRunning { get; set; }

		public ClientDescriptor MessengerAsClient
		{
			get
			{
				localClient.Ip = IP;
				localClient.Port = Port;
				localClient.EndPointType = EndPointLocation;
				localClient.Id = ClientId;
				return localClient;
			}
		}

		public event EventHandler<MessageEventArgs> MessageRecieved;

		public event EventHandler<MessageEventArgs> ProxyMessageRecieved;

		public event EventHandler<MessageEventArgs> MessageResponsePrepared;

		public event EventHandler<MessageEventArgs> MessageFailed;

		public Messenger(string address, int port)
		{
			IP = IPAddress.Parse(address);
			Port = port;
			server.DataReceived += Server_DataReceived;
			server.ClientDisconnected += Server_ClientDisconnected;
			server.ClientConnected += Server_ClientConnected;
		}

		private void Server_ClientConnected(object sender, TcpClient e)
		{
			if (cancel && server.ConnectedClientsCount == 0)
			{
				server.Stop();
			}
		}

		private void Server_ClientDisconnected(object sender, TcpClient e)
		{
			if (cancel && server.ConnectedClientsCount == 0)
			{
				server.Stop();
			}
		}

		public Messenger(string address, int port, EndPointEnum endPointType, int id)
		{
			IP = IPAddress.Parse(address);
			Port = port;
			EndPointLocation = endPointType;
			ClientId = id;
			server.DataReceived += Server_DataReceived;
			server.ClientDisconnected += Server_ClientDisconnected;
			server.ClientConnected += Server_ClientConnected;
		}

		public MessageDescriptor CreateMessage(ClientDescriptor transmitter, ClientDescriptor reciever)
		{
			MessageDescriptor messageDescriptor = Activator.CreateInstance(typeof(MessageDescriptor)) as MessageDescriptor;
			messageDescriptor.Transmitter = transmitter;
			messageDescriptor.Reciever = reciever;
			return messageDescriptor;
		}

		public MessageDescriptor CreateMessage(Type messageType, ClientDescriptor transmitter, ClientDescriptor reciever)
		{
			MessageDescriptor messageDescriptor = Activator.CreateInstance(messageType) as MessageDescriptor;
			messageDescriptor.Transmitter = transmitter;
			messageDescriptor.Reciever = reciever;
			return messageDescriptor;
		}

		public MessageDescriptor CreateMessage(Type messageType, MessageKindEnum msgKind, ClientDescriptor transmitter, EndPointEnum reciever, int recieverId)
		{
			MessageDescriptor messageDescriptor = Activator.CreateInstance(messageType) as MessageDescriptor;
			messageDescriptor.MessageKind = msgKind;
			messageDescriptor.Transmitter = transmitter;
			messageDescriptor.Route.Reciever = reciever;
			messageDescriptor.Route.RevieverId = recieverId;
			return messageDescriptor;
		}

		public void Start()
		{
			while (!server.IsStarted)
			{
				try
				{
					server.Start(IP, Port);
				}
				catch (Exception)
				{
					Thread.Sleep(500);
				}
			}
			th = new Thread(ThreadRun);
			th.Start();
		}

		public void Stop()
		{
			cancel = true;
			while (IsRunning)
			{
				Thread.Sleep(100);
			}
			if (server.IsStarted)
			{
				server.Stop();
			}
			while (server.IsStarted)
			{
				Thread.Sleep(100);
			}
		}

		public ClientDescriptor GetClient(EndPointEnum endPointType)
		{
			return recievers.Where((ClientDescriptor c) => c.EndPointType == endPointType).FirstOrDefault();
		}

		public void SendMessage(MessageDescriptor msg)
		{
			List<MessageDescriptor> list = messages.Values.ToList();
			if (list.Contains(msg))
			{
				return;
			}
			list = sentMessages.Values.ToList();
			if (list.Contains(msg))
			{
				return;
			}
			if (msg.Route.RecieverAddress == null)
			{
				ClientDescriptor clientDescriptor = recievers.Where((ClientDescriptor r) => r.EndPointType == msg.Route.Reciever && r.Id == msg.Route.RevieverId).FirstOrDefault();
				if (clientDescriptor == null)
				{
					return;
				}
				msg.Route.RecieverAddress = clientDescriptor.Ip;
				msg.Route.RecieverPort = clientDescriptor.Port;
			}
			messages.TryAdd(msg.MessageId, msg);
		}

		public void AddReciever(ClientDescriptor reciever)
		{
			if (!recievers.Contains(reciever))
			{
				recievers.Add(reciever);
			}
		}

		private void Server_DataReceived(object sender, Message e)
		{
			if (!IsRunning)
			{
				e.TcpClient.Close();
				return;
			}
			MessageDescriptor[] array = MessageDescriptor.Deserialize(e.MessageString);
			HandleRecievedMessages(array);
		}

		private void HandleRecievedMessages(MessageDescriptor[] messages)
		{
			foreach (MessageDescriptor msg in messages)
			{
				if (msg.Route.Reciever == MessengerAsClient.EndPointType && msg.Route.RecieverAddressValue == IP.ToString() && msg.Route.RecieverPort == Port)
				{
					if (msg.IsResponse)
					{
						sentMessages.TryRemove(msg.ReferenceId, out var _);
						if (this.MessageRecieved != null)
						{
							this.MessageRecieved(this, new MessageEventArgs(msg));
						}
						continue;
					}
					if (this.MessageRecieved != null)
					{
						this.MessageRecieved(this, new MessageEventArgs(msg));
					}
					MessageDescriptor msg2 = msg.CreateResponse();
					if (this.MessageResponsePrepared != null)
					{
						this.MessageResponsePrepared(this, new MessageEventArgs(msg2));
					}
					SendMessage(msg2);
				}
				else
				{
					ClientDescriptor clientDescriptor = recievers.Where((ClientDescriptor r) => r.EndPointType == msg.Route.Reciever && r.Id == msg.Route.RevieverId).FirstOrDefault();
					if (clientDescriptor != null)
					{
						msg.Route.RecieverAddress = clientDescriptor.Ip;
						msg.Route.RecieverPort = clientDescriptor.Port;
					}
					if (this.ProxyMessageRecieved != null)
					{
						this.ProxyMessageRecieved(this, new MessageEventArgs(msg));
					}
					SendMessage(msg);
				}
			}
		}

		private void ThreadRun()
		{
			IsRunning = true;
			while (!cancel)
			{
				try
				{
					string text = "";
					List<MessageDescriptor> list = new List<MessageDescriptor>();
					Guid[] array = messages.Keys.ToArray();
					Guid[] array2 = array;
					foreach (Guid key in array2)
					{
						if (messages.TryRemove(key, out var value))
						{
							try
							{
								string data = MessageDescriptor.SerializeMessage(value);
								client.Connect(value.Route.RecieverAddress.ToString(), value.Route.RecieverPort);
								client.Write(data);
								client.Disconnect();
							}
							catch (Exception)
							{
							}
							value.SentDateTime = DateTime.Now;
							value.LastSentDateTime = value.SentDateTime;
							sentMessages.TryAdd(value.MessageId, value);
							Thread.Sleep(10);
						}
					}
					array = sentMessages.Keys.ToArray();
					Guid[] array3 = array;
					foreach (Guid key2 in array3)
					{
						if (!sentMessages.TryGetValue(key2, out var value2) || DateTime.Now.Subtract(value2.LastSentDateTime).TotalSeconds < 2.0)
						{
							continue;
						}
						try
						{
							string data2 = MessageDescriptor.SerializeMessage(value2);
							client.Connect(value2.Route.RecieverAddress.ToString(), value2.Route.RecieverPort);
							client.Write(data2);
							client.Disconnect();
						}
						catch
						{
						}
						if (DateTime.Now.Subtract(value2.SentDateTime).TotalSeconds > 10.0 && sentMessages.TryRemove(value2.MessageId, out var value3))
						{
							if (this.MessageFailed != null)
							{
								this.MessageFailed(this, new MessageEventArgs(value3));
							}
							Console.WriteLine("To Send :{1}, Sent : {0}", sentMessages.Keys.Count, messages.Keys.Count);
						}
						value2.LastSentDateTime = DateTime.Now;
						Thread.Sleep(10);
					}
					array = sentMessages.Keys.ToArray();
				}
				catch
				{
					Thread.Sleep(100);
				}
				Thread.Sleep(100);
			}
			IsRunning = false;
		}
	}
}
