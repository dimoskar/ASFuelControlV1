using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ASFuelControl.StartItalianaNewVersion
{
	public class Connector
	{
		private SerialPort serialPort = new SerialPort();

		private Thread th;

		private List<ATGProbe> probes = new List<ATGProbe>();

		private bool isConnected;

		private bool errorOccured;

		private string buffer = "";

		public string CommunicationPort
		{
			get;
			set;
		}

		public bool IsConnected
		{
			get
			{
				return this.isConnected;
			}
		}

		public ATGProbe[] Probes
		{
			get
			{
				return this.probes.ToArray();
			}
		}

		public Connector()
		{
		}

		public ATGProbe AddProbe(int addressId)
		{
			ATGProbe aTGProbe;
			if ((
				from p in this.probes
				where p.Address == addressId
				select p).Count<ATGProbe>() <= 0)
			{
				ATGProbe aTGProbe1 = new ATGProbe()
				{
					Address = addressId
				};
				this.probes.Add(aTGProbe1);
				aTGProbe = aTGProbe1;
			}
			else
			{
				aTGProbe = null;
			}
			return aTGProbe;
		}

		public void Connect()
		{
			try
			{
				this.serialPort.PortName = this.CommunicationPort;
				this.serialPort.DtrEnable = true;
				this.serialPort.Open();
				this.isConnected = true;
				this.th = new Thread(new ThreadStart(this.ThreadRun));
				this.th.Start();
			}
			catch
			{
			}
		}

		public void DisConnect()
		{
			this.isConnected = false;
			this.serialPort.Close();
		}

		private void EvaluateResponse(string response)
		{
			string[] strArrays = response.Replace("\n\r", "").Split(new char[] { '=' });
			int num = int.Parse(strArrays[0].Substring(0, 5));
			int num1 = int.Parse(strArrays[0].Substring(6, 1));
			decimal num2 = int.Parse(strArrays[1].Substring(1, 3));
			decimal num3 = int.Parse(strArrays[2].Replace(".", ""));
			decimal num4 = int.Parse(strArrays[3].Replace(".", ""));
			ATGProbe aTGProbe = (
				from p in this.probes
				where p.Address == num
				select p).FirstOrDefault<ATGProbe>();
			if (aTGProbe != null)
			{
				if (num1 == 0)
				{
					aTGProbe.FuelLevel = num3 / new decimal(100);
					aTGProbe.WaterLevel = num4 / new decimal(100);
					aTGProbe.Temperature = num2 / new decimal(10);
					if (this.DataUpdated != null)
					{
						this.DataUpdated(aTGProbe, new EventArgs());
					}
				}
			}
		}

		private void ThreadRun()
		{
			while (this.IsConnected)
			{
				try
				{
					if (this.errorOccured)
					{
						this.serialPort.Close();
						this.serialPort.Open();
						this.errorOccured = false;
					}
					int num = 0;
					foreach (ATGProbe probe in this.probes)
					{
						SerialPort serialPort = this.serialPort;
						int address = probe.Address;
						serialPort.Write(string.Concat("M", address.ToString(), "\n\r"));
						Thread.Sleep(100);
						num += 100;
						string str = "";
						DateTime now = DateTime.Now;
						while (true)
						{
							if (this.serialPort.BytesToRead > 0)
							{
								str = string.Concat(str, this.serialPort.ReadExisting());
								now = DateTime.Now;
								if (!str.Contains("\n\r"))
								{
									Console.WriteLine("{0:HH:mm:ss.fff} - DATA OK", DateTime.Now);
								}
								else
								{
									break;
								}
							}
							else if (DateTime.Now.Subtract(now).TotalMilliseconds > 500)
							{
								break;
							}
							Thread.Sleep(20);
						}
						if (str.Length > 0)
						{
							int num1 = str.IndexOf("\n\r");
							if (num1 >= 0)
							{
								str = str.Substring(0, num1);
							}
							this.EvaluateResponse(str);
						}
						else
						{
							Console.WriteLine("{0:HH:mm:ss.fff} - NO DATA RETURNED", DateTime.Now);
						}
					}
					Thread.Sleep(1500 - num);
				}
				catch
				{
					this.errorOccured = true;
					Thread.Sleep(500);
				}
			}
		}

		public event EventHandler DataUpdated;
	}
}