using ASFuelControl.Common;
using ASFuelControl.Common.Enumerators;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ASFuelControl.GilbarcoFrontier
{
	public class GilbarcoProtocol : IFuelProtocol, IPumpDebug
	{
		private List<FuelPoint> fuelPoints = new List<FuelPoint>();

		private SerialPort serialPort = new SerialPort();

		private Thread th;

		private int extendedProperty;

		public string CommunicationPort
		{
			get;
			set;
		}

		public FuelPoint[] FuelPoints
		{
			get
			{
				return this.fuelPoints.ToArray();
			}
			set
			{
				this.fuelPoints = new List<FuelPoint>(value);
			}
		}

		public bool IsConnected
		{
			get
			{
				return this.serialPort.IsOpen;
			}
		}

		private double speed
		{
			get;
			set;
		}

		public GilbarcoProtocol()
		{
		}

		public void AddFuelPoint(FuelPoint fp)
		{
			this.fuelPoints.Add(fp);
		}

		public bool AuthorizeFuelPoint(FuelPoint fp)
		{
			byte[] numArray = Commands.AuthorizeFuelPoint(fp.Address);
			this.serialPort.Write(numArray, 0, (int)numArray.Length);
			if (File.Exists(this.LogPath()))
			{
				this.LogAdd("TX Authorise", numArray, null);
			}
			Thread.Sleep(25);
			byte[] numArray1 = new byte[this.serialPort.BytesToRead];
			this.serialPort.Read(numArray1, 0, (int)numArray1.Length);
			if (File.Exists(this.LogPath()))
			{
				this.LogAdd("RX Authorise", numArray1, null);
			}
			return true;
		}

		public void ClearFuelPoints()
		{
			this.fuelPoints.Clear();
		}

		private byte[] CMDGetNozzle()
		{
			return new byte[] { 255, 233, 254, 224, 225, 224, 251, 238, 240 };
		}

		public void Connect()
		{
			try
			{
				this.serialPort.PortName = this.CommunicationPort;
				this.serialPort.BaudRate = 5787;
				this.serialPort.Parity = Parity.Even;
				this.serialPort.StopBits = StopBits.One;
				this.serialPort.DataBits = 8;
				this.serialPort.Open();
				this.th = new Thread(new ThreadStart(this.WorkFlow));
				this.th.Start();
			}
			catch
			{
			}
		}

		public DebugValues DebugStatusDialog(FuelPoint fp)
		{
			throw new NotImplementedException();
		}

		public void Disconnect()
		{
			if (this.serialPort.IsOpen)
			{
				this.serialPort.Close();
			}
		}

		private void EvalGetDisplay(Nozzle nozzle, byte[] buffer)
		{
			try
			{
				if ((int)buffer.Length == 7)
				{
					string str = BitConverter.ToString(buffer.Skip<byte>(1).Take<byte>(6).Reverse<byte>().ToArray<byte>()).Replace("-", "").Replace("E", "");
					nozzle.ParentFuelPoint.DispensedAmount = decimal.Parse(str) / (decimal)Math.Pow(10, (double)nozzle.ParentFuelPoint.AmountDecimalPlaces);
					nozzle.ParentFuelPoint.DispensedVolume = decimal.Round(nozzle.ParentFuelPoint.DispensedAmount / nozzle.UnitPrice, nozzle.ParentFuelPoint.AmountDecimalPlaces);
					if (this.DataChanged != null)
					{
						FuelPointValues fuelPointValue = new FuelPointValues()
						{
							CurrentSalePrice = nozzle.UnitPrice,
							CurrentPriceTotal = nozzle.ParentFuelPoint.DispensedAmount,
							CurrentVolume = nozzle.ParentFuelPoint.DispensedVolume
						};
						this.DataChanged(this, new FuelPointValuesArgs()
						{
							CurrentFuelPoint = nozzle.ParentFuelPoint,
							CurrentNozzleId = 1,
							Values = fuelPointValue
						});
					}
				}
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (File.Exists(this.LogPath()))
				{
					this.LogAdd("Exception Thread EvalTransaction", null, exception.ToString());
				}
			}
		}

		private bool EvalGetTotals(Nozzle nozzle, byte[] response)
		{
			bool flag;
			try
			{
				if (response[0] - 80 == nozzle.ParentFuelPoint.Address)
				{
					byte[] array = response.Skip<byte>(5 + 30 * (nozzle.NozzleIndex - 1)).Take<byte>(8).ToArray<byte>();
					array = array.Reverse<byte>().ToArray<byte>();
					decimal num = decimal.Parse(BitConverter.ToString(array, 0, (int)array.Length).Replace("-", "").Replace("E", ""));
					nozzle.TotalVolume = num;
					if (File.Exists(this.LogPath()))
					{
						this.LogAdd("RX EvalTotals", null, string.Concat("Volume Total: ", num.ToString()));
					}
					Thread.Sleep(50);
					nozzle.ParentFuelPoint.QueryTotals = false;
					flag = true;
				}
				else
				{
					flag = false;
				}
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (File.Exists(this.LogPath()))
				{
					this.LogAdd("RX GetTotals", null, exception.ToString());
				}
				flag = false;
			}
			return flag;
		}

		private void EvalStatus(FuelPoint fp, byte[] response)
		{
			FuelPointValues fuelPointValue = new FuelPointValues();
			FuelPointStatusEnum status = fp.Status;
			FuelPointStatusEnum fuelPointStatusEnum = fp.Status;
			int extendedProperty = (int)fp.GetExtendedProperty("CurrentNozzle", -1);
			if (fp.Status != FuelPointStatusEnum.Offline && (int)response.Length <= 1)
			{
				fuelPointStatusEnum = FuelPointStatusEnum.Offline;
				fp.Status = fuelPointStatusEnum;
				fp.DispenserStatus = fp.Status;
				return;
			}
			if (fp.Status == FuelPointStatusEnum.Offline && (int)response.Length > 1)
			{
				fuelPointStatusEnum = FuelPointStatusEnum.Idle;
			}
			byte num = response[1];
			if (num >= 97 && num <= 111 && fp.Status != FuelPointStatusEnum.Work)
			{
				fuelPointStatusEnum = FuelPointStatusEnum.Idle;
			}
			else if (num >= 97 && num <= 111 && fp.Status == FuelPointStatusEnum.Work)
			{
				this.GetTransaction(fp.ActiveNozzle);
				fuelPointStatusEnum = FuelPointStatusEnum.Idle;
				if (extendedProperty >= 0)
				{
					fp.Nozzles[extendedProperty].QueryTotals = true;
				}
			}
			else if (num >= 177 && num <= 191 && fp.Status == FuelPointStatusEnum.Work)
			{
				this.GetTransaction(fp.ActiveNozzle);
				fuelPointStatusEnum = FuelPointStatusEnum.Idle;
				if (extendedProperty >= 0)
				{
					fp.Nozzles[extendedProperty].QueryTotals = true;
				}
			}
			else if (num >= 161 && num <= 175 && fp.Status == FuelPointStatusEnum.Work)
			{
				this.GetTransaction(fp.ActiveNozzle);
				fuelPointStatusEnum = FuelPointStatusEnum.Idle;
				if (extendedProperty >= 0)
				{
					fp.Nozzles[extendedProperty].QueryTotals = true;
				}
			}
			else if (num >= 113 && num <= 127 && fp.Status != FuelPointStatusEnum.Work)
			{
				if ((int)this.NozzleAddress(fp).Length == 28)
				{
					fp.SetExtendedProperty("CurrentNozzle", 0);
					fuelPointStatusEnum = FuelPointStatusEnum.Nozzle;
				}
			}
			else if (num < 129 || num > 143)
			{
				fuelPointStatusEnum = (num < 145 || num > 159 ? fp.Status : FuelPointStatusEnum.Work);
			}
			else
			{
				fuelPointStatusEnum = FuelPointStatusEnum.Work;
			}
			if (fp.Status == FuelPointStatusEnum.Work)
			{
				this.GetDisplay(fp.ActiveNozzle);
			}
			fp.Status = fuelPointStatusEnum;
			fp.DispenserStatus = fp.Status;
		}

		private void EvalTransaction(Nozzle nozzle, byte[] buffer)
		{
			if ((int)buffer.Length < 34)
			{
				return;
			}
			try
			{
				byte[] array = buffer.Skip<byte>(13).Take<byte>(4).ToArray<byte>();
				array = array.Reverse<byte>().ToArray<byte>();
				byte[] numArray = buffer.Skip<byte>(25).Take<byte>(6).ToArray<byte>().Reverse<byte>().ToArray<byte>();
				byte[] array1 = buffer.Skip<byte>(18).Take<byte>(6).ToArray<byte>().Reverse<byte>().ToArray<byte>();
				string str = BitConverter.ToString(array).Replace("-", "").Replace("E", "");
				string str1 = BitConverter.ToString(array1).Replace("-", "").Replace("E", "");
				string str2 = BitConverter.ToString(numArray).Replace("-", "").Replace("E", "");
				if (nozzle.ParentFuelPoint.Channel == 10)
				{
					nozzle.UnitPrice = decimal.Parse(str) / (decimal)Math.Pow(10, (double)nozzle.ParentFuelPoint.UnitPriceDecimalPlaces);
					nozzle.UntiPriceInt = int.Parse(str);
					nozzle.ParentFuelPoint.DispensedVolume = decimal.Parse(str1) / (decimal)Math.Pow(10, 3);
					nozzle.ParentFuelPoint.DispensedAmount = decimal.Parse(str2) / (decimal)Math.Pow(10, 3);
				}
				else if (nozzle.ParentFuelPoint.Channel == 20)
				{
					nozzle.UnitPrice = decimal.Parse(str) / (decimal)Math.Pow(10, (double)nozzle.ParentFuelPoint.UnitPriceDecimalPlaces);
					nozzle.UntiPriceInt = int.Parse(str);
					nozzle.ParentFuelPoint.DispensedVolume = decimal.Parse(str1) / (decimal)Math.Pow(10, 3);
					nozzle.ParentFuelPoint.DispensedAmount = decimal.Parse(str2) / (decimal)Math.Pow(10, 2);
				}
				else if (nozzle.ParentFuelPoint.Channel != 30)
				{
					nozzle.UnitPrice = decimal.Parse(str) / (decimal)Math.Pow(10, (double)nozzle.ParentFuelPoint.UnitPriceDecimalPlaces);
					nozzle.UntiPriceInt = int.Parse(str);
					nozzle.ParentFuelPoint.DispensedVolume = decimal.Parse(str1) / (decimal)Math.Pow(10, (double)nozzle.ParentFuelPoint.VolumeDecimalPlaces);
					nozzle.ParentFuelPoint.DispensedAmount = decimal.Parse(str2) / (decimal)Math.Pow(10, (double)nozzle.ParentFuelPoint.AmountDecimalPlaces);
				}
				else
				{
					nozzle.UnitPrice = decimal.Parse(str) / (decimal)Math.Pow(10, (double)nozzle.ParentFuelPoint.UnitPriceDecimalPlaces);
					nozzle.UntiPriceInt = int.Parse(str);
					nozzle.ParentFuelPoint.DispensedVolume = decimal.Parse(str1) / (decimal)Math.Pow(10, 2);
					nozzle.ParentFuelPoint.DispensedAmount = decimal.Parse(str2) / (decimal)Math.Pow(10, 1);
				}
				if (((nozzle.ParentFuelPoint.DispensedVolume * nozzle.UnitPrice) - nozzle.ParentFuelPoint.DispensedAmount) > new decimal(1, 0, 0, false, 1))
				{
					nozzle.ParentFuelPoint.DispensedAmount = decimal.Round(nozzle.ParentFuelPoint.DispensedVolume * nozzle.UnitPrice, nozzle.ParentFuelPoint.AmountDecimalPlaces);
				}
				if (this.DataChanged != null)
				{
					FuelPointValues fuelPointValue = new FuelPointValues()
					{
						CurrentSalePrice = nozzle.UnitPrice,
						CurrentPriceTotal = nozzle.ParentFuelPoint.DispensedAmount,
						CurrentVolume = nozzle.ParentFuelPoint.DispensedVolume
					};
					this.DataChanged(this, new FuelPointValuesArgs()
					{
						CurrentFuelPoint = nozzle.ParentFuelPoint,
						CurrentNozzleId = 1,
						Values = fuelPointValue
					});
				}
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (File.Exists(this.LogPath()))
				{
					this.LogAdd("Exception Thread EvalTransaction", null, exception.ToString());
				}
			}
		}

		public void GetDisplay(Nozzle nozzle)
		{
			int num = 0;
			byte[] display = Commands.GetDisplay(nozzle.ParentFuelPoint.Address);
			this.serialPort.Write(display, 0, (int)display.Length);
			if (File.Exists(this.LogPath()))
			{
				this.LogAdd("TX GetDisplay", display, null);
			}
			while (this.serialPort.BytesToRead < 7 && num < 300)
			{
				Thread.Sleep(30);
				num = num + 20;
			}
			byte[] numArray = new byte[this.serialPort.BytesToRead];
			this.serialPort.Read(numArray, 0, (int)numArray.Length);
			if (File.Exists(this.LogPath()))
			{
				this.LogAdd("RX GetDisplay", numArray, null);
			}
			this.EvalGetDisplay(nozzle, numArray);
		}

		public void GetStatus(FuelPoint fp)
		{
			int num = 0;
			byte[] status = Commands.GetStatus(fp.Address);
			this.serialPort.Write(status, 0, (int)status.Length);
			if (File.Exists(this.LogPath()))
			{
				this.LogAdd("TX Status", status, null);
			}
			while (this.serialPort.BytesToRead < 2 && num < 300)
			{
				Thread.Sleep(30);
				num = num + 20;
			}
			byte[] numArray = new byte[this.serialPort.BytesToRead];
			this.serialPort.Read(numArray, 0, this.serialPort.BytesToRead);
			if (File.Exists(this.LogPath()))
			{
				this.LogAdd("RX Status", numArray, null);
			}
			if ((int)numArray.Length <= 1)
			{
				fp.Status = FuelPointStatusEnum.Offline;
				return;
			}
			this.EvalStatus(fp, numArray);
		}

		public bool GetTotals(Nozzle nz)
		{
			byte[] totals = Commands.GetTotals(nz.ParentFuelPoint.Address);
			this.serialPort.Write(totals, 0, (int)totals.Length);
			if (File.Exists(this.LogPath()))
			{
				this.LogAdd("TX GetTotals", totals, null);
			}
			Thread.Sleep(750);
			byte[] numArray = new byte[this.serialPort.BytesToRead];
			this.serialPort.Read(numArray, 0, this.serialPort.BytesToRead);
			if (File.Exists(this.LogPath()))
			{
				this.LogAdd("RX GetTotals", numArray, null);
			}
			if ((int)numArray.Length <= 10 || numArray[0] != totals[0])
			{
				return false;
			}
			return this.EvalGetTotals(nz, numArray);
		}

		public void GetTransaction(Nozzle nozzle)
		{
			int num = 0;
			byte[] transaction = Commands.GetTransaction(nozzle.ParentFuelPoint.Address);
			this.serialPort.Write(transaction, 0, (int)transaction.Length);
			if (File.Exists(this.LogPath()))
			{
				this.LogAdd("TX GetTransaction", transaction, null);
			}
			while (this.serialPort.BytesToRead < 34 && num < 300)
			{
				Thread.Sleep(30);
				num = num + 20;
			}
			byte[] numArray = new byte[this.serialPort.BytesToRead];
			this.serialPort.Read(numArray, 0, (int)numArray.Length);
			if (File.Exists(this.LogPath()))
			{
				this.LogAdd("RX GetTransaction", numArray, null);
			}
			this.EvalTransaction(nozzle, numArray);
		}

		private bool Halt(FuelPoint fp)
		{
			byte[] numArray = Commands.Halt(fp.Address);
			this.serialPort.Write(numArray, 0, (int)numArray.Length);
			if (File.Exists(this.LogPath()))
			{
				this.LogAdd("TX Halt", numArray, null);
			}
			Thread.Sleep(40);
			byte[] numArray1 = new byte[this.serialPort.BytesToRead];
			this.serialPort.Read(numArray1, 0, this.serialPort.BytesToRead);
			if (File.Exists(this.LogPath()))
			{
				this.LogAdd("RX Halt", numArray1, null);
			}
			Thread.Sleep(40);
			byte[] numArray2 = Commands.Halt(fp.Address);
			this.serialPort.Write(numArray2, 0, (int)numArray2.Length);
			if (File.Exists(this.LogPath()))
			{
				this.LogAdd("TX Halt", numArray2, null);
			}
			Thread.Sleep(40);
			byte[] numArray3 = new byte[this.serialPort.BytesToRead];
			this.serialPort.Read(numArray3, 0, this.serialPort.BytesToRead);
			if (File.Exists(this.LogPath()))
			{
				this.LogAdd("RX Halt", numArray3, null);
			}
			return true;
		}

		public void LogAdd(string Method, byte[] DataBuffer, string Exception)
		{
			DateTime now;
			if (Exception != null)
			{
				string str = this.LogPath();
				string[] method = new string[6];
				now = DateTime.Now;
				method[0] = now.ToString("dd-MM HH:mm:ss.fff");
				method[1] = " ";
				method[2] = Method;
				method[3] = "\t";
				method[4] = Exception;
				method[5] = "\r\n";
				File.AppendAllText(str, string.Concat(method));
				return;
			}
			string str1 = this.LogPath();
			string[] strArrays = new string[6];
			now = DateTime.Now;
			strArrays[0] = now.ToString("dd-MM HH:mm:ss.fff");
			strArrays[1] = " ";
			strArrays[2] = Method;
			strArrays[3] = "\t";
			strArrays[4] = BitConverter.ToString(DataBuffer);
			strArrays[5] = "\r\n";
			File.AppendAllText(str1, string.Concat(strArrays));
		}

		public string LogPath()
		{
			return string.Concat("Gilbarco_", this.CommunicationPort, ".log");
		}

		public byte[] NozzleAddress(FuelPoint fp)
		{
			byte[] numArray;
			try
			{
				int num = 0;
				byte[] numArray1 = Commands.ListenMode(fp.Address);
				this.serialPort.Write(numArray1, 0, (int)numArray1.Length);
				if (File.Exists(this.LogPath()))
				{
					this.LogAdd("TX ListenMode", numArray1, null);
				}
				Thread.Sleep(40);
				byte[] numArray2 = new byte[this.serialPort.BytesToRead];
				this.serialPort.Read(numArray2, 0, this.serialPort.BytesToRead);
				if (File.Exists(this.LogPath()))
				{
					this.LogAdd("RX SetPrice", numArray2, null);
				}
				byte num1 = numArray2[1];
				if (num1 < 209 || num1 > 223)
				{
					numArray = null;
				}
				else
				{
					Thread.Sleep(10);
					byte[] numArray3 = this.CMDGetNozzle();
					this.serialPort.Write(numArray3, 0, (int)numArray3.Length);
					if (File.Exists(this.LogPath()))
					{
						this.LogAdd("TX GetNozzle", numArray3, null);
					}
					while (this.serialPort.BytesToRead < 28 && num < 300)
					{
						num = num + 10;
						Thread.Sleep(20);
					}
					byte[] numArray4 = new byte[this.serialPort.BytesToRead];
					this.serialPort.Read(numArray4, 0, (int)numArray4.Length);
					if (File.Exists(this.LogPath()))
					{
						this.LogAdd("RX GetNozzle", numArray4, null);
					}
					numArray = numArray4;
				}
			}
			catch (Exception exception)
			{
				numArray = null;
			}
			return numArray;
		}

		public bool SetPrice(Nozzle nozzle)
		{
			bool flag;
			try
			{
				byte[] numArray = Commands.ListenMode(nozzle.ParentFuelPoint.Address);
				this.serialPort.Write(numArray, 0, (int)numArray.Length);
				if (File.Exists(this.LogPath()))
				{
					this.LogAdd("TX ListenMode", numArray, null);
				}
				Thread.Sleep(30);
				byte[] numArray1 = new byte[this.serialPort.BytesToRead];
				this.serialPort.Read(numArray1, 0, this.serialPort.BytesToRead);
				if (File.Exists(this.LogPath()))
				{
					this.LogAdd("RX ListenMode", numArray1, null);
				}
				byte num = numArray1[1];
				if (num < 209 || num > 223)
				{
					flag = false;
				}
				else
				{
					Thread.Sleep(10);
					byte[] numArray2 = Commands.SetPrice(nozzle.NozzleIndex, nozzle.UntiPriceInt);
					this.serialPort.Write(numArray2, 0, (int)numArray2.Length);
					Trace.WriteLine(BitConverter.ToString(numArray2));
					if (File.Exists(this.LogPath()))
					{
						this.LogAdd("TX SetPrice", numArray2, null);
					}
					Thread.Sleep(50);
					byte[] numArray3 = new byte[this.serialPort.BytesToRead];
					this.serialPort.Read(numArray3, 0, this.serialPort.BytesToRead);
					if (File.Exists(this.LogPath()))
					{
						this.LogAdd("RX SetPrice", numArray3, null);
					}
					flag = true;
				}
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (File.Exists(this.LogPath()))
				{
					this.LogAdd("Exception Thread SetPrice", null, exception.ToString());
				}
				flag = false;
			}
			return flag;
		}

		private void WorkFlow()
		{
			Nozzle[] nozzles;
			int i;
			foreach (FuelPoint fuelPoint in this.fuelPoints)
			{
				fuelPoint.QueryHalt = this.Halt(fuelPoint);
			}
			foreach (FuelPoint fuelPoint1 in this.fuelPoints)
			{
				fuelPoint1.Nozzles[0].QueryTotals = true;
			}
			while (this.IsConnected)
			{
				try
				{
					foreach (FuelPoint extendedProperty in this.fuelPoints)
					{
						try
						{
							if (extendedProperty.QueryHalt)
							{
								extendedProperty.QueryHalt = !this.Halt(extendedProperty);
							}
							else if ((
								from n in (IEnumerable<Nozzle>)extendedProperty.Nozzles
								where n.QueryTotals
								select n).Count<Nozzle>() > 0)
							{
								nozzles = extendedProperty.Nozzles;
								for (i = 0; i < (int)nozzles.Length; i++)
								{
									Nozzle nozzle = nozzles[i];
									if (nozzle.QueryTotals && this.GetTotals(nozzle))
									{
										extendedProperty.Initialized = true;
										if (this.TotalsRecieved != null)
										{
											this.TotalsRecieved(this, new TotalsEventArgs(extendedProperty, nozzle.Index, nozzle.TotalVolume, nozzle.TotalPrice));
										}
									}
								}
							}
							else if (!extendedProperty.QuerySetPrice)
							{
								if (File.Exists(this.LogPath()))
								{
									string str = extendedProperty.QueryAuthorize.ToString();
									i = extendedProperty.ActiveNozzleIndex;
									this.LogAdd("AuthoriseCheck", null, string.Concat(str, " | ActiveNozzle:", i.ToString()));
								}
								if (!extendedProperty.QueryAuthorize)
								{
									FuelPointStatusEnum status = extendedProperty.Status;
									this.GetStatus(extendedProperty);
									if (status != extendedProperty.Status && this.DispenserStatusChanged != null)
									{
										FuelPointValues fuelPointValue = new FuelPointValues();
										int num = (int)extendedProperty.GetExtendedProperty("CurrentNozzle", -1);
										if (File.Exists(this.LogPath()))
										{
											this.LogAdd("CurrentNozzle", null, string.Concat("ActiveNozzle:", num.ToString()));
										}
										if (extendedProperty.Status == FuelPointStatusEnum.Idle || extendedProperty.Status == FuelPointStatusEnum.Offline)
										{
											extendedProperty.ActiveNozzleIndex = -1;
											fuelPointValue.ActiveNozzle = -1;
										}
										else
										{
											extendedProperty.ActiveNozzleIndex = (int)extendedProperty.GetExtendedProperty("CurrentNozzle");
											fuelPointValue.ActiveNozzle = num;
										}
										fuelPointValue.Status = extendedProperty.Status;
										this.DispenserStatusChanged(this, new FuelPointValuesArgs()
										{
											CurrentFuelPoint = extendedProperty,
											CurrentNozzleId = fuelPointValue.ActiveNozzle + 1,
											Values = fuelPointValue
										});
									}
									if (extendedProperty.Status == FuelPointStatusEnum.Work)
									{
										extendedProperty.SetExtendedProperty("iNeedDisplay", true);
									}
								}
								else if (this.AuthorizeFuelPoint(extendedProperty))
								{
									extendedProperty.QueryAuthorize = false;
								}
							}
							else
							{
								Thread.Sleep(10);
								nozzles = extendedProperty.Nozzles;
								for (i = 0; i < (int)nozzles.Length; i++)
								{
									Nozzle nozzle1 = nozzles[i];
									if (this.SetPrice(nozzle1))
									{
										nozzle1.QuerySetPrice = false;
									}
									Thread.Sleep(50);
								}
								if ((
									from n in (IEnumerable<Nozzle>)extendedProperty.Nozzles
									where n.QuerySetPrice
									select n).Count<Nozzle>() == 0)
								{
									extendedProperty.QuerySetPrice = false;
								}
							}
						}
						finally
						{
							Thread.Sleep(200);
						}
					}
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (File.Exists(this.LogPath()))
					{
						string str1 = this.LogPath();
						DateTime now = DateTime.Now;
						File.AppendAllText(str1, string.Concat(now.ToString("dd-MM HH:mm:ss.fff"), "Thread Exception:  ", exception.ToString(), "\r\n"));
					}
				}
			}
		}

		public event EventHandler<FuelPointValuesArgs> DataChanged;

		public event EventHandler DispenserOffline;

		public event EventHandler<FuelPointValuesArgs> DispenserStatusChanged;

		public event EventHandler<TotalsEventArgs> TotalsRecieved;
        public event EventHandler<Common.SaleEventArgs> SaleRecieved;
    }
}