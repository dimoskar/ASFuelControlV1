using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ASFuelControl.GilbarcoFrontier
{
	public class Commands
	{
		public Commands()
		{
		}

		public static byte[] AuthorizeFuelPoint(int FuelPointAddress)
		{
			return new byte[] { BitConverter.GetBytes(16 + FuelPointAddress)[0] };
		}

		public static byte[] GetDisplay(int FuelPointAddress)
		{
			byte bytes = BitConverter.GetBytes(96 + FuelPointAddress)[0];
			return new byte[] { bytes };
		}

		public static byte[] GetStatus(int FuelPointAddress)
		{
			return new byte[] { BitConverter.GetBytes(FuelPointAddress)[0] };
		}

		public static byte[] GetTotals(int FuelPointAddress)
		{
			return new byte[] { BitConverter.GetBytes(80 + FuelPointAddress)[0] };
		}

		public static byte[] GetTransaction(int FuelPointAddress)
		{
			byte bytes = BitConverter.GetBytes(64 + FuelPointAddress)[0];
			return new byte[] { bytes };
		}

		public static int GilbarcoLRC(byte[] bytearray)
		{
			int num = 0;
			for (int i = 0; i < (int)bytearray.Length; i++)
			{
				num = num + bytearray[i] & 15;
			}
			num = ((num ^ 15) + 1 & 15) + 224;
			return num;
		}

		public static byte[] Halt(int FuelPointAddress)
		{
			return new byte[] { BitConverter.GetBytes(48 + FuelPointAddress)[0] };
		}

		public static byte[] ListenMode(int FuelPointAddress)
		{
			byte bytes = BitConverter.GetBytes(32 + FuelPointAddress)[0];
			return new byte[] { bytes };
		}

		public static byte[] SetPrice(int CurrentNozzle, int UnitPrice)
		{
			string str = UnitPrice.ToString();
			int currentNozzle = 223 + CurrentNozzle;
			if (str.Length == 1)
			{
				str = string.Concat("E0E0E0E", str.Substring(0, 1));
			}
			if (str.Length == 2)
			{
				str = string.Concat("E0E0E", str.Substring(0, 1), "E", str.Substring(1, 1));
			}
			if (str.Length == 3)
			{
				str = string.Concat(new string[] { "E0E", str.Substring(0, 1), "E", str.Substring(1, 1), "E", str.Substring(2, 1) });
			}
			if (str.Length == 4)
			{
				str = string.Concat(new string[] { "E", str.Substring(0, 1), "E", str.Substring(1, 1), "E", str.Substring(2, 1), "E", str.Substring(3, 1) });
			}
			byte[] byteArray = Commands.StringToByteArray(str);
			byte[] numArray = new byte[] { 255, 229, 244, 246, 0, 247, 0, 0, 0, 0, 251 };
			numArray[4] = (byte)currentNozzle;
			numArray[6] = byteArray[3];
			numArray[7] = byteArray[2];
			numArray[8] = byteArray[1];
			numArray[9] = byteArray[0];
			int num = Commands.GilbarcoLRC(numArray);
			byte[] numArray1 = new byte[] { 255, 229, 244, 246, 0, 247, 0, 0, 0, 0, 251, 0, 240 };
			numArray1[4] = (byte)currentNozzle;
			numArray1[6] = byteArray[3];
			numArray1[7] = byteArray[2];
			numArray1[8] = byteArray[1];
			numArray1[9] = byteArray[0];
			numArray1[11] = (byte)num;
			return numArray1;
		}

		public static byte[] StringToByteArray(string hex)
		{
			return (
				from x in Enumerable.Range(0, hex.Length)
				where x % 2 == 0
				select Convert.ToByte(hex.Substring(x, 2), 16)).ToArray<byte>();
		}
	}
}