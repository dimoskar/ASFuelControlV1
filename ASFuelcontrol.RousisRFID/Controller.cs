using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASFuelControl.RousisRFID
{
    public class Controller
    {
        private System.IO.Ports.SerialPort serialPort = new System.IO.Ports.SerialPort();
        private int _address = 0;
        private byte[] address;
        private string lastId = "";
        private DateTime lastIdSet = DateTime.Now;
        private Crc crcComputer = new Crc(CrcStdParams.StandartParameters[CrcAlgorithms.Crc16Buypass]);
        private bool isConnected = false;

        public string ComPort { set; get; } = "COM1";
        public int Address
        {
            set
            {
                this._address = value;
                string addr = _address.ToString();
                List<byte> addressList = new List<byte>();
                for(int i=0; i < addr.Length; i++)
                {
                    byte str = (byte)(addr[0]);
                    addressList.Add(str);
                }
                if (addressList.Count == 1)
                    addressList.Insert(0, 0x30);
                this.address = addressList.ToArray();
            }
            get { return this._address; }
        }

        public bool EnrollmentDevice { set; get; }

        public bool IsConnected { get { return this.isConnected; } }

        public string LastScannedId { set; get; } = null;

        public Controller()
        {

        }

        public void Connect()
        {
            try
            {
                if (this.serialPort.IsOpen)
                    return;
                isConnected = true;
                serialPort.PortName = this.ComPort;
                serialPort.BaudRate = 9600;
                serialPort.Parity = System.IO.Ports.Parity.None;
                serialPort.StopBits = System.IO.Ports.StopBits.One;
                serialPort.DataBits = 8;
                serialPort.Open();
            }
            catch
            {

            }
        }

        public void DisConnect()
        {
            try
            {

                if (!this.serialPort.IsOpen)
                    return;
                isConnected = false;
                System.Threading.Thread.Sleep(1000);
                while (!this.SetDiplayLine1("DISCONNECTED"))
                {
                    System.Threading.Thread.Sleep(100);
                }
                while (!this.SetDiplayLine2(""))
                {
                    System.Threading.Thread.Sleep(100);
                }

                this.serialPort.Close();
            }
            catch
            {

            }
        }

        public bool ChangeTimeout(int timeOut)
        {
            byte[] timeOutBuffer = BitConverter.GetBytes(timeOut);
            byte[] header = new byte[] { 0x01, 0x55, 0xAA };
            byte[] command = new byte[] { 0xb2 };
            List<byte> commandList = new List<byte>();
            commandList.AddRange(header);
            commandList.AddRange(this.address);
            commandList.AddRange(command);
            commandList.AddRange(timeOutBuffer);
            commandList.Add(0x04);
            var result = ExcecuteCommand(commandList.ToArray());
            if (result.Length < 9)
                return false;
            var resAddress = result.Skip(2).Take(2).ToArray();
            if(resAddress[0] == address[0] && resAddress[1] == address[1])
            {
                var okres = result.Skip(4).Take(3).ToArray();
                if((char)okres[0] == 'O' && (char)okres[1] == 'K' && (char)okres[2] == '!')
                    return true;
                return false;
            }
            return false;
        }

        public string GetLastId()
        {
            byte[] header = new byte[] { 0x01, 0x55, 0xAA };
            byte[] command = new byte[] { 0xA0 };
            List<byte> commandList = new List<byte>();
            commandList.AddRange(header);
            commandList.AddRange(this.address);
            commandList.AddRange(command);
            commandList.Add(0x04);
            var result = ExcecuteCommand(commandList.ToArray());
            if (result.Length < 9)
                return "";
            var resAddress = result.Skip(2).Take(2).ToArray();
            string str = "";
            if (resAddress[0] == address[0] && resAddress[1] == address[1])
            {
                var strRes = result.Skip(5).ToArray();
                for(int i=0; i < strRes.Length - 2; i++)
                {
                    str = str + (char)strRes[i];
                }
                if (str == lastId && DateTime.Now.Subtract(this.lastIdSet).Seconds < 10)
                    return "";
                lastId = str;
                lastIdSet = DateTime.Now;
                return str;
            }
            return "";
        }

        public bool SendLedStatus()
        {
            byte[] header = new byte[] { 0x01, 0x55, 0xAA };
            byte[] command = new byte[] { 0xA4 };
            List<byte> commandList = new List<byte>();
            commandList.AddRange(header);
            commandList.AddRange(this.address);
            commandList.AddRange(command);
            commandList.Add(0x03);
            commandList.Add(0x04);
            var result = ExcecuteCommand(commandList.ToArray());
            if (result.Length < 9)
                return false;
            var resAddress = result.Skip(2).Take(2).ToArray();
            if (resAddress[0] == address[0] && resAddress[1] == address[1])
            {
                var okres = result.Skip(4).Take(3).ToArray();
                if ((char)okres[0] == 'O' && (char)okres[1] == 'K' && (char)okres[2] == '!')
                    return true;
                return false;
            }
            return false;
        }

        public bool SetDiplayLine1(string line)
        {
            if (line.Length < 16)
            {
                while (line.Length < 16)
                {
                    line = " " + line + " ";
                }
            }

            byte[] header = new byte[] { 0x01, 0x55, 0xAA };
            byte[] command = new byte[] { 0xD1 };
            List<byte> commandList = new List<byte>();
            commandList.AddRange(header);
            commandList.AddRange(this.address);
            commandList.AddRange(command);
            for(int i=0; i < (line.Length >16 ? 16 : line.Length); i++)
            {
                commandList.Add((byte)line[i]);
            }
            commandList.Add(0x04);
            var result = ExcecuteCommand(commandList.ToArray());
            if (result.Length < 9)
                return false;
            var resAddress = result.Skip(2).Take(2).ToArray();
            if (resAddress[0] == address[0] && resAddress[1] == address[1])
            {
                var okres = result.Skip(4).Take(3).ToArray();
                if ((char)okres[0] == 'O' && (char)okres[1] == 'K' && (char)okres[2] == '!')
                    return true;
                return false;
            }
            return false;
        }

        public bool SetDiplayLine2(string line)
        {
            if (line.Length < 16)
            {
                while (line.Length < 16)
                {
                    line = " " + line + " ";
                }
            }

            byte[] header = new byte[] { 0x01, 0x55, 0xAA };
            byte[] command = new byte[] { 0xD2 };
            List<byte> commandList = new List<byte>();
            commandList.AddRange(header);
            commandList.AddRange(this.address);
            commandList.AddRange(command);
            for (int i = 0; i < (line.Length > 16 ? 16 : line.Length); i++)
            {
                commandList.Add((byte)line[i]);
            }
            commandList.Add(0x04);
            var result = ExcecuteCommand(commandList.ToArray());
            if (result.Length < 9)
                return false;
            var resAddress = result.Skip(2).Take(2).ToArray();
            if (resAddress[0] == address[0] && resAddress[1] == address[1])
            {
                var okres = result.Skip(4).Take(3).ToArray();
                if ((char)okres[0] == 'O' && (char)okres[1] == 'K' && (char)okres[2] == '!')
                    return true;
                return false;
            }
            return false;
        }

        private byte[] ExcecuteCommand(byte[] buffer)
        {
            try
            {
                if (!this.serialPort.IsOpen)
                    return new byte[] { };
                var crcBytes = crcComputer.ComputeHash(buffer);
                var crc = crcBytes.Skip(crcBytes.Length - 2).ToArray();

                List<byte> total = new List<byte>();
                total.AddRange(buffer);
                total.AddRange(crc);
                serialPort.Write(total.ToArray(), 0, total.Count);
                System.Threading.Thread.Sleep(100);
                if (!this.serialPort.IsOpen)
                    return new byte[] { };
                if (serialPort.BytesToRead == 0)
                    return new byte[] { };

                byte[] outBuffer = new byte[serialPort.BytesToRead];
                serialPort.Read(outBuffer, 0, outBuffer.Length);

                if (outBuffer.Length > 2)
                {
                    var crcOut = crcComputer.ComputeHash(outBuffer, 0, outBuffer.Length - 2);
                    var buf1 = crcOut.Skip(crcOut.Length - 2).ToArray();
                    var buf2 = outBuffer.Skip(outBuffer.Length - 2).ToArray();
                    if (buf1[0] == buf2[0] && buf1[1] == buf2[1])
                    {
                        return outBuffer;
                    }
                }
                return new byte[] { };
            }
            catch
            {
                return new byte[] { };
            }
        }
    }
}
