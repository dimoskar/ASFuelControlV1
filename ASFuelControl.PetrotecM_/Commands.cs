using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASFuelControl.PetrotecM_
{
    public class Commands
    {
        #region Commands
        private static byte StartCom = 58;
        private static byte[] EndCom = new byte[] { 13, 10 };

        public static byte[] SetPrice(int NozzleID, int UnitPrice)
        {
            byte[] PriceArray = Encoding.ASCII.GetBytes(UnitPrice.ToString("D4"));
            List<byte> CMD = new List<byte>();
            CMD.AddRange(new byte[] { 89, (byte)(48 + NozzleID) });
            CMD.AddRange(PriceArray);
            byte[] LRCArray = Encoding.ASCII.GetBytes(LRC(CMD.ToArray()).ToString("X2"));
            List<byte> buffer = new List<byte>();
            buffer.Add(StartCom);
            buffer.AddRange(CMD.ToArray());
            buffer.AddRange(LRCArray);
            buffer.AddRange(EndCom);
            return buffer.ToArray();
        }
        public static byte[] Authorise(int NozzleID, int UnitPrice)
        {
            byte[] PriceArray = Encoding.ASCII.GetBytes(UnitPrice.ToString("D4"));
            //byte[] command = new byte[] { 84, (byte)(49 + NozzleID) };
            byte[] command = new byte[] { 116, (byte)(49 + NozzleID) };
            List<byte> CMD = new List<byte>();
            CMD.AddRange(command);
            CMD.AddRange(PriceArray);
            CMD.AddRange(new byte[] { 0x20, 0x31, 0x39, 0x39, 0x39, 0x39, 0x39, 0x39 });
            List<byte> buffer = new List<byte>();
            buffer.Add(StartCom);
            buffer.AddRange(CMD);
            buffer.AddRange(Encoding.ASCII.GetBytes(LRC(CMD.ToArray()).ToString("X2")));
            buffer.AddRange(EndCom);
            return buffer.ToArray();
        }
        public static byte[] AuthorisePreset(int NozzleID, int UnitPrice)
        {
            byte[] PriceArray = Encoding.ASCII.GetBytes(UnitPrice.ToString("D4"));
            byte[] command = new byte[] { 116, (byte)(48 + NozzleID) };
            List<byte> CMD = new List<byte>();
            CMD.AddRange(command);
            CMD.AddRange(PriceArray);
            CMD.AddRange(new byte[] { 0x20, (byte)(PresetAuthorise.Amount + 30), 0x39, 0x39, 0x39, 0x39, 0x39, 0x39 });
            List<byte> buffer = new List<byte>();
            buffer.Add(StartCom);
            buffer.AddRange(CMD);
            buffer.AddRange(Encoding.ASCII.GetBytes(LRC(CMD.ToArray()).ToString("X2")));
            buffer.AddRange(EndCom);
            return buffer.ToArray();
        }
        public static byte[] VolumeTotal(int NozzleID)
        {
            int TypeCom = 86;
            byte[] CMD = new byte[] { (byte)TypeCom, (byte)(48 + NozzleID) };
            byte[] LRCArray = Encoding.ASCII.GetBytes(LRC(CMD).ToString("X2"));
            List<byte> buffer = new List<byte>();
            buffer.Add(StartCom);
            buffer.AddRange(CMD);
            buffer.AddRange(LRCArray);
            buffer.AddRange(EndCom);
            return buffer.ToArray();
        }
        public static byte[] GetDisplay(int FuelpointAddress)
        {
            int TypeCom = 82;
            byte[] CMD = new byte[] { (byte)TypeCom, (byte)(48 + FuelpointAddress) };
            byte[] LRCArray = Encoding.ASCII.GetBytes(LRC(CMD).ToString("X2"));
            List<byte> buffer = new List<byte>();
            buffer.Add(StartCom);
            buffer.AddRange(CMD);
            buffer.AddRange(LRCArray);
            buffer.AddRange(EndCom);
            return buffer.ToArray();
        }
        public static byte[] Halt()
        {
            int TypeCom = 80;
            byte[] CMD = new byte[] { (byte)TypeCom };
            byte[] LRCArray = Encoding.ASCII.GetBytes(LRC(CMD).ToString("X2"));
            List<byte> buffer = new List<byte>();
            buffer.Add(StartCom);
            buffer.AddRange(CMD);
            buffer.AddRange(LRCArray);
            buffer.AddRange(EndCom);
            return buffer.ToArray();
        }
        public static byte[] PowerONOFF()
        {
            int TypeCom = 83;
            byte[] CMD = new byte[] { (byte)TypeCom };
            byte[] LRCArray = Encoding.ASCII.GetBytes(LRC(CMD).ToString("X2"));
            List<byte> buffer = new List<byte>();
            buffer.Add(StartCom);
            buffer.AddRange(CMD);
            buffer.AddRange(LRCArray);
            buffer.AddRange(EndCom);
            return buffer.ToArray();
        }
        public static byte[] Status()
        {
            int TypeCom = 255;
            byte[] CMD = new byte[] { (byte)TypeCom };
            List<byte> buffer = new List<byte>();
            buffer.AddRange(CMD);
            buffer.AddRange(EndCom);
            return buffer.ToArray();
        }
        public static byte[] Acknowledge()
        {
            byte[] buffer = new byte[] { 0x21 };
            return buffer;
        }
        private enum PresetAuthorise
        {
            Volume = 0,
            Amount = 1
        }

        #endregion

        #region CRC
        public static int LRC(byte[] bytes)
        {
            int num = 0;
            for (int i = 0; i < (int)bytes.Length; i++)
            {
                num = num ^ bytes[i];
            }
            return num;
        }
        public static byte[] LRCtoByte(byte[] bytes)
        {
            string str = LRC(bytes).ToString();
            if (str.Length == 1)
            {
                str = string.Concat(str, "0");
            }
            int num = int.Parse(str.Substring(0, 1));
            int num1 = int.Parse(str.Substring(1, 1));
            byte[] numArray = new byte[] { (byte)(48 + num), (byte)(48 + num1) };
            return numArray;
        }
        #endregion
    }

}
