// File: TatsunoPdeClient.Core.cs (net462)
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;

namespace ASFuelControl.Tatsuno
{
    public static class Ctrl
    {
        public const byte SOH = 0x01;
        public const byte STX = 0x02;
        public const byte ETX = 0x03;
        public const byte ENQ = 0x05;
        public const byte ACK = 0x06;
        public const byte NAK = 0x15;
        public const byte SYN = 0x16;
        public const byte CAN = 0x18;
    }

    public class PdeOptions
    {
        public int SynCount = 2;
        public int TkaMs = 200;
        public int TkbMs = 50;
        public int TkcMs = 100;
        public bool ToggleRts = false;
    }

    public class TatsunoPdeClient : IDisposable
    {
        private readonly SerialPort _port;
        private readonly PdeOptions _opt;
        private readonly Encoding _enc = Encoding.ASCII;

        public TatsunoPdeClient(SerialPort port, PdeOptions options)
        {
            _opt = options ?? new PdeOptions();

            _port = port;
            _port.ReadTimeout = 20;
            _port.WriteTimeout = 100;
            _port.Handshake = Handshake.None;
            //_port.RtsEnable = !_opt.ToggleRts;
            _port.Encoding = Encoding.ASCII;
        }

        public void Open()
        {
            if (!_port.IsOpen) _port.Open();
        }

        public bool IsOpen()
        {
            return _port.IsOpen;
        }

        public void Close()
        {
            if (_port.IsOpen) _port.Close();
        }

        public void Dispose()
        {
            try { if (_port.IsOpen) _port.Close(); } catch { }
            _port.Dispose();
        }

        // ----- Public synchronous API -----

        public bool Initialize(byte _addr)
        {
            for (int tries = 0; tries < 10; tries++)
            {
                object incoming = PollOnce(_addr);
                if (incoming is PdeInitReq)
                {
                    var init = (PdeInitReq)incoming;
                    string ack = AuthCompute(init.Code, _addr);
                    if (!SendSelect(_addr, 'I', ack))
                        continue;

                    object next = PollOnce(_addr);
                    
                    if (next is PdeError)
                    {
                        var err = (PdeError)next;
                        Common.Logger.Instance.Trace("Error found. Code: " + err.Code.ToString());
                        if (err.Code == 99)
                        {
                            Common.Logger.Instance.Trace("Exit Initialize: TRUE");
                            return true;
                        }
                        else if (err.Code == 63)
                        {
                            Common.Logger.Instance.Trace("Exit Initialize: FALSE");
                            return false;
                        }
                        else
                            Common.Logger.Instance.Trace("Error Code evaluation failed. Code: " + err.Code.ToString());
                    }
                    if (next is PdeInitReq)
                    {
                        tries--; continue;
                    }
                }
                Thread.Sleep(20);
            }
            Common.Logger.Instance.Trace("Exit Initialize (END of METHOD): FALSE");
            return false;
        }

        public object PollOnce(byte _addr)
        {
            byte[] payload;
            byte[] envelope;
            if (!SendPoll(_addr, out payload, out envelope))
            {
                Common.Logger.Instance.Trace("PollOnce Failed");
                return null;
            }
            if (!VerifyCrc(envelope))
            {
                Common.Logger.Instance.Trace("PollOnce Failed because of CRC Error");
                WriteAck(_addr, Ctrl.NAK);
                return null;
            }
            WriteAck(_addr, Ctrl.ACK);
            char code = (char)payload[0];
            string data = _enc.GetString(payload, 1, payload.Length - 1);
            Common.Logger.Instance.Debug("PollOnce, Code: " + code + ". Data Recieved: " + data);
            try
            {
                switch (code)
                {
                    case 'd':
                        Common.Logger.Instance.Trace("Display Data");
                        return PdeDisplay.Parse(data);
                    case 'e':
                        Common.Logger.Instance.Trace("Error Data");
                        return PdeError.Parse(data);
                    case 's':
                        Common.Logger.Instance.Trace("Status Data");
                        return PdeStatus.Parse(data);
                    case 'h':
                        Common.Logger.Instance.Trace("History Data");
                        return PdeHistory.Parse(data);
                    case 'm':
                        Common.Logger.Instance.Trace("Msssage Data");
                        return PdeText.Parse(data);
                    case 'o':
                        Common.Logger.Instance.Trace("Text Status Data");
                        return PdeTextStatus.Parse(data);
                    case 'x':
                        Common.Logger.Instance.Trace("Registers Data");
                        return PdeRegisters.Parse(data);
                    case 'i':
                        Common.Logger.Instance.Trace("Init Data");
                        return new PdeInitReq(data);
                    default:
                        Common.Logger.Instance.Trace("Other Data");
                        return new PdeRawInbound(code, data);
                }
            }
            catch(Exception ex)
            {
                Common.Logger.Instance.Debug("Wrong Data Recieved");
                return null; 
            }
        }

        public enum DispenserState
        {
            Idle = 0,
            NozzleLifted = 1,
            Fueling = 2,
            TransactionFinished = 3,
            Error = 4,
            Unknown = 255
        }


        public class DispenserStatusResult
        {
            public DispenserState DispenserState { get; set; }
            public int? ActiveNozzleIndex { get; set; }
        }

        public bool SendResetCommand(byte address)
        {
            // 'M99' is commonly used for soft reset or error clear
            if (!SendSelect(address, 'M', "99"))
                return false;

            object response = PollOnce(address);

            // Check for success acknowledgment
            var ack = response as string;
            if (ack != null && ack.StartsWith("PDE"))
            {
                Console.WriteLine("Dispenser reset acknowledged: " + ack);
                return true;
            }

            // Check for error response
            var error = response as PdeError;
            if (error != null)
            {
                Console.WriteLine("Reset failed with error " + error.Code + ": " + PdeErrorCodes.GetDescription(error.Code));
                return false;
            }

            Console.WriteLine("Unexpected response during reset.");
            return false;
        }


        public PdeStatus RequestStatus(byte _addr)
        {
            if (!SendSelect(_addr, 'S', ""))
                return null;

            object response = PollFor("S", _addr, 150);
            var status = response as PdeStatus;
            if (status == null)
                return null;

            return status;

        }

        private object PollFor(string req, byte addr, int durationMs)
        {
            long now = NowMs();
            long end = now + durationMs;
            object msg = null;
            while (NowMs() < end)
            {
                msg = PollOnce(addr);
                if (msg != null)
                {
                    if(req == "S" && msg is PdeStatus)
                        return msg;
                    if(req == "S" && msg is PdeError)
                    {
                        var ret = new PdeStatus();
                        ret.State = 4;
                        ret.Error = msg as PdeError;
                        return ret;
                    }
                    if (req == "X" && msg is PdeRegisters)
                        return msg;
                }
                Thread.Sleep(50);
            }
            return msg;
        }

        public PdeDisplay RequestDisplay(byte addr)
        {
            if (!SendSelect(addr, 'D', ""))
                return null;
            object response = PollOnce(addr);
            var display= response as PdeDisplay;
            if (display == null)
                return null;

            return display;
        }

        public PdeRegisters RequestTotalizers(byte addr, int index)
        {
            string command = index.ToString("D2"); // e.g. "01", "02"
            if (!SendSelect(addr, 'X', command))
                return null;


            object response = PollFor("X", addr, 100);
            var totals = response as PdeRegisters;
            if (totals == null)
                return null;

            return totals;
        }


        public PdeDisplay RequestRegisters(byte address)
        {
            if (!SendSelect(address, 'R', ""))
                return null;

            object response = PollOnce(address);
            var display = response as PdeDisplay;
            if (display == null)
                return null;

            return display;
        }

        public bool Control(byte _addr, int requestCode) { return SendSelect(_addr, 'C', requestCode.ToString()); }

        public bool Authorize(byte _addr, int preType, int preValue, int priceType, int unitPrice, int product)
        {
            string s = preType.ToString() +
                       preValue.ToString("D6") +
                       priceType.ToString() +
                       unitPrice.ToString("D4") +
                       product.ToString();
            return SendSelect(_addr, 'A', s);
        }

        public bool SetPrices(byte address, int[] products, int[] prices)
        {
            if (!SendSelect(address, 'M', "99"))
                return false;
            PollOnce(address);
            if (!SendSelect(address, 'P', "00"))
                return false;
            PollOnce(address);
            string payload = GetPricePayload(products, prices);

            // Send 'T' command with constructed payload
            if (!SendSelect(address, 'T', payload))
                return false;

            // Poll for response
            object response = PollOnce(address);

            // Check for success
            if (response is string version && version.StartsWith("PDE"))
            {
                Console.WriteLine("Price update acknowledged: " + version);
                return true;
            }

            // Check for error
            if (response is PdeError error)
            {
                ASFuelControl.Common.Logger.Instance.Trace($"Price update failed with error {error.Code}: {PdeErrorCodes.GetDescription(error.Code)}");
                return false;
            }
            ASFuelControl.Common.Logger.Instance.Trace($"Unexpected response during price update");
            return false;
        }

        public string GetPricePayload(int[] products, int[] prices)
        {
            string payload = "";
            Dictionary<int, string> payloads = new Dictionary<int, string>();
            for(int i=0; i < products.Length; i++)
            {
                if(i <= products.Length)
                    payloads.Add(products[i], products[i].ToString() + prices[i].ToString("D4"));
            }
            for(int i=1; i <= 8; i++)
            {
                if (payloads.ContainsKey(i))
                    continue;
                payloads.Add(i, i.ToString() + prices[0].ToString("D4"));
            }
            foreach(int i in payloads.Keys)
            {
                payload = payload + payloads[i];
            }
            ASFuelControl.Common.Logger.Instance.Trace("Set Price T Payload: " + payload);
            return payload;
        }

        public bool SendUnlock(byte _addr)
        {
            object respP = PollOnce(_addr);
            if (SendSelect(_addr, 'C', "1"))
            {
                //object respZ = PollOnce(_addr);
                System.Threading.Thread.Sleep(20);
                return true;
            }

            return false;
        }
        public bool SendLock(byte _addr)
        {
            object respP = PollOnce(_addr);
            if (SendSelect(_addr, 'C', "0"))
            {
                object respZ = PollOnce(_addr);
                System.Threading.Thread.Sleep(20);
                return true;
            }

            return false;
        }

        public bool SendClearDisplay(byte _addr)
        {
            object respP = PollOnce(_addr);

            if (SendSelect(_addr, 'C', "5"))
            {
                object respZ = PollOnce(_addr);
                System.Threading.Thread.Sleep(20);
                return true;
            }
            
            return false;
        }

        public bool SetUnitPrice(byte _addr, int product, int price)
        {
            //if (!SendSelect(_addr, 'M', "99"))
            //    return false;
            //object respM = PollOnce(_addr);
            //if (!SendSelect(_addr, 'P', "00"))
            //    return false;
            if (!SendUnlock(_addr))
                return false;
            object respP = PollOnce(_addr);
            if (SendSelect(_addr, 'Z', product.ToString() + price.ToString("D4")))
            {
                object respZ = PollOnce(_addr);
                return true;
            }
            return false;
        }

        public bool GetUnitPrice(byte _addr, int product)
        {
            if (!SendSelect(_addr, 'M', "99"))
                return false;
            object respM = PollOnce(_addr);
            if (!SendSelect(_addr, 'P', "00"))
                return false;
            object respP = PollOnce(_addr);
            if (SendSelect(_addr, 'M', "03"))
            {
                object respZ = PollOnce(_addr);
                if (respZ != null)
                {
                    if (respZ is PdeText)
                        ASFuelControl.Common.Logger.Instance.Trace($"Get Unit Price Response for Product {product}: {((PdeText)respZ).Text}");
                    else if (respZ is PdeError)
                        ASFuelControl.Common.Logger.Instance.Trace($"Get Unit Price Response for Product {product}: {((PdeError)respZ).Code}");
                    else if (respZ is PdeRegisters)
                        ASFuelControl.Common.Logger.Instance.Trace($"Get Unit Price Response for Product {product}: {((PdeRegisters)respZ).NozzleIndex} {((PdeRegisters)respZ).TransactionType} {((PdeRegisters)respZ).Volume}");
                    else
                        ASFuelControl.Common.Logger.Instance.Trace($"Get Unit Price Response for Product {product} is not recognized {respZ.GetType().FullName}");
                }
                return true;
            }
            return false;
        }

        public bool RequestLastTransaction(byte _addr, int index) { return SendSelect(_addr, 'H', index.ToString("D2")); }

        // ----- Internal I/O -----

        private bool SendPoll(byte _addr, out byte[] payload, out byte[] envelope)
        {
            payload = null;
            envelope = null;

            var frame = new byte[_opt.SynCount + 2];
            for (int i = 0; i < _opt.SynCount; i++) frame[i] = Ctrl.SYN;
            frame[_opt.SynCount] = _addr;
            frame[_opt.SynCount + 1] = Ctrl.ENQ;
            Write(frame);

            long deadline = NowMs() + _opt.TkaMs;
            if (!ReadUntil(Ctrl.SYN, deadline))
                return false;

            int b;
            do
            {
                b = ReadOne(deadline);
            }
            while (b == Ctrl.SYN);

            if (b != _addr)
                return false;

            int next = ReadOne(deadline);
            if (next == Ctrl.CAN)
                return false;
            if (next != Ctrl.STX)
                return false;

            var buf = new byte[64];
            int p = 0;
            while (true)
            {
                int by = ReadOne(deadline);
                if (by == Ctrl.ETX) break;
                if (p == buf.Length) Array.Resize(ref buf, buf.Length * 2);
                buf[p++] = (byte)by;
            }
            int hi = ReadOne(deadline);
            int lo = ReadOne(deadline);

            envelope = new byte[1 + p + 1 + 2];
            int q = 0;
            envelope[q++] = Ctrl.STX;
            Buffer.BlockCopy(buf, 0, envelope, q, p); q += p;
            envelope[q++] = Ctrl.ETX;
            envelope[q++] = (byte)hi;
            envelope[q++] = (byte)lo;

            payload = new byte[p];
            Buffer.BlockCopy(buf, 0, payload, 0, p);
            return true;
        }

        private bool SendSelect(byte _addr, char msgCode, string asciiPayload)
        {
            byte[] pld = _enc.GetBytes(msgCode + asciiPayload);
            var inner = new byte[1 + pld.Length + 1];
            inner[0] = Ctrl.STX;
            Buffer.BlockCopy(pld, 0, inner, 1, pld.Length);
            inner[inner.Length - 1] = Ctrl.ETX;

            ushort crc = CrcSum(inner);
            byte crcHi = (byte)((crc >> 8) & 0x7F);
            byte crcLo = (byte)(crc & 0x7F);

            var frame = new byte[_opt.SynCount + 1 + inner.Length + 2];
            int p = 0;
            for (int i = 0; i < _opt.SynCount; i++) frame[p++] = Ctrl.SYN;
            frame[p++] = _addr;
            Buffer.BlockCopy(inner, 0, frame, p, inner.Length); p += inner.Length;
            frame[p++] = crcHi;
            frame[p++] = crcLo;

            Common.Logger.Instance.Debug(string.Format("SendSelect, Data Sent: {0},  Data Frame Start: {1}", msgCode, BitConverter.ToString(frame)));
            Write(frame);
            Common.Logger.Instance.Debug(string.Format("SendSelect, Data Sent: {0},  Data Frame End: {1}", msgCode, BitConverter.ToString(frame)));
            long deadline = NowMs() + _opt.TkbMs;
            if (!ReadUntil(Ctrl.SYN, deadline))
            {
                Common.Logger.Instance.Debug(string.Format("SendSelect Read Until Failed, Data Sent: {0},  Data Frame End: {1}", msgCode, BitConverter.ToString(frame)));
                return false;
            }
            int b;
            do
            {
                b = ReadOne(deadline);
            }
            while (b == Ctrl.SYN);

            if (b != _addr)
            {
                Common.Logger.Instance.Debug(string.Format("SendSelect Read One Mismatsh, Data Sent: {0},  Data Frame End: {1}", msgCode, BitConverter.ToString(frame)));
                return false;
            }
            int ack = ReadOne(deadline);
            return ack == Ctrl.ACK;
        }

        private void WriteAck(byte _addr, byte ack)
        {
            var w = new byte[_opt.SynCount + 2];
            int p = 0;
            for (int i = 0; i < _opt.SynCount; i++) w[p++] = Ctrl.SYN;
            w[p++] = _addr;
            w[p++] = ack;
            Write(w);
        }

        int countOpen = 0;
        private void Write(byte[] data)
        {
            _port.RtsEnable = false;
            _port.Write(data, 0, data.Length);
            //_port.BaseStream.Flush();
        }

        private bool VerifyCrc(byte[] envelopeStxToCrc)
        {
            if (envelopeStxToCrc.Length < 4) return false;
            int len = envelopeStxToCrc.Length;
            ushort calc = CrcSum(SubArray(envelopeStxToCrc, 0, len - 2));
            byte hi = (byte)((calc >> 8) & 0x7F);
            byte lo = (byte)(calc & 0x7F);
            return envelopeStxToCrc[len - 2] == hi && envelopeStxToCrc[len - 1] == lo;
        }

        private static ushort CrcSum(byte[] data)
        {
            uint sum = 0;
            for (int i = 0; i < data.Length; i++) sum = (sum + data[i]) & 0xFFFF;
            return (ushort)sum;
        }

        private static string AuthCompute(string received8Ascii, byte adr)
        {
            if (received8Ascii.Length != 8) throw new ArgumentException("Auth requires 8 chars.");
            uint C = (uint)(adr + 1);
            var R = new char[8];
            for (int i = 0; i < 8; i++)
            {
                C = C * (byte)received8Ascii[i];
                byte low = (byte)(C & 0xFF);
                low = (byte)(low & 0x7F);
                if (low < 0x20) C += 0x20;
                R[i] = (char)(C & 0x7F);
                C = (C >> 8) + adr;
            }
            return new string(R);
        }

        // ---------- Reading helpers ----------

        private bool ReadUntil(byte target, long deadlineMs)
        {
            List<byte> foo = new List<byte>();
            while (NowMs() < deadlineMs)
            {
                int b = TryReadByte(deadlineMs);
                if (b < 0)
                    continue;
                foo.Add((byte)b);
                if (b == target)
                {
                    ASFuelControl.Common.Logger.Instance.Trace("ReadUntil (SUCCESS) Data: " + BitConverter.ToString(foo.ToArray()));
                    return true;
                }
            }
            ASFuelControl.Common.Logger.Instance.Trace("ReadUntil (FAILURE) Data: " + BitConverter.ToString(foo.ToArray()));
            return false;
        }

        private int ReadOne(long deadlineMs)
        {
            while (NowMs() < deadlineMs)
            {
                int b = TryReadByte(deadlineMs);
                if (b >= 0) return b;
            }
            throw new TimeoutException("Read timed out.");
        }

        private int TryReadByte(long deadlineMs)
        {
            try
            {
                if (!_port.IsOpen)
                {
                    _port.Open();
                    countOpen++;
                    Common.Logger.Instance.Debug("Opend port: " + countOpen.ToString());
                }
                if (_port.BytesToRead > 0)
                    return _port.ReadByte();
                Thread.Sleep(1);
                return -1;
            }
            catch (TimeoutException tex)
            {
                Common.Logger.Instance.Debug("TryReadByte TimeoutException: " + tex.Message);
                if(tex.InnerException != null)
                    Common.Logger.Instance.Debug("TryReadByte TimeoutException Inner: " + tex.InnerException.Message);
                if (NowMs() >= deadlineMs) throw(tex);
                return -1;
            }
            catch(Exception ex)
            {
                Common.Logger.Instance.Debug("TryReadByte Exception: " + ex.Message);
                if (ex.InnerException != null)
                    Common.Logger.Instance.Debug("TryReadByte TimeoutException Inner: " + ex.InnerException.Message);
                Thread.Sleep(10);
                return -1;
            }
        }

        private static long NowMs() { return Environment.TickCount; }

        private static byte[] SubArray(byte[] src, int index, int count)
        {
            var r = new byte[count];
            Buffer.BlockCopy(src, index, r, 0, count);
            return r;
        }
    }

    // ---------- Message models ----------

    public class PdeDisplay
    {
        public int Txn, Nozzle, Amount, Volume, UnitPrice;
        public static PdeDisplay Parse(string data)
        {
            int pos = 0;
            var obj = new PdeDisplay();
            obj.Txn = int.Parse(data.Substring(pos, 3)); pos += 3;
            obj.Nozzle = int.Parse(data.Substring(pos, 1)); pos += 1;
            obj.Amount = int.Parse(data.Substring(pos, 6)); pos += 6;
            obj.Volume = int.Parse(data.Substring(pos, 6)); pos += 6;
            obj.UnitPrice = int.Parse(data.Substring(pos, 4));
            return obj;
        }
    }

    public static class PdeErrorCodes
    {
        private static readonly Dictionary<int, string> _errorDescriptions = new Dictionary<int, string>
    {
        { 1, "Display error" },
        { 6, "Electromechanical totalizer error" },
        { 7, "Memory error" },
        { 9, "Processor error" },
        { 10, "Temperature sensor error" },
        { 11, "Data error" },
        { 13, "EPROM CRC error" },
        { 15, "Maximum time of fueling exceeded" },
        { 16, "Communication error with PDECRE unit" },
        { 17, "Communication error with POS" },
        { 19, "Low voltage (<180V)" },
        { 20, "Fueling interrupted due to power off" },
        { 21, "Preselected amount/volume is zero" },
        { 25, "Electronic totalizer error" },
        { 26, "TOTAL STOP button pressed" },
        { 30, "Unit price is zero" },
        { 31, "Pulse channel error (pulser 1A)" },
        { 32, "Pulse channel error (pulser 2A)" },
        { 33, "Pulse channel error (pulser 3A)" },
        { 34, "Pulse channel error (pulser 4A)" },
        { 36, "Pulse channel error (pulser 1B)" },
        { 37, "Pulse channel error (pulser 2B)" },
        { 38, "Pulse channel error (pulser 3B)" }, // ← your case
        { 39, "Pulse channel error (pulser 4B)" },
        { 41, "Pulser power error (pulser 1A)" },
        { 42, "Pulser power error (pulser 2A)" },
        { 43, "Pulser power error (pulser 3A)" },
        { 44, "Pulser power error (pulser 4A)" },
        { 46, "Pulser power error (pulser 1B)" },
        { 47, "Pulser power error (pulser 2B)" },
        { 48, "Pulser power error (pulser 3B)" },
        { 49, "Pulser power error (pulser 4B)" },
        { 51, "Slow down valve error (1A)" },
        { 52, "Slow down valve error (2A)" },
        { 53, "Slow down valve error (3A)" },
        { 54, "Slow down valve error (4A)" },
        { 56, "Slow down valve error (1B)" },
        { 57, "Slow down valve error (2B)" },
        { 58, "Slow down valve error (3B)" },
        { 59, "Slow down valve error (4B)" },
        { 63, "Pulser power error (pulser 3B) (63)" },
        { 70, "Display error" },
        { 71, "Pulse channel error" },
        { 72, "Pulse channel short circuit error" },
        { 73, "Pulser power supply error" },
        { 99, "No Error" }
    };

        public static string GetDescription(int code)
        {
            string description;
            if (_errorDescriptions.TryGetValue(code, out description))
                return description;

            return "Unknown error code: " + code;
        }
    }

    public class PdeError
    {
        public int Code;
        public static PdeError Parse(string data)
        {
            var e = new PdeError();
            e.Code = int.Parse(data);
            return e;
        }
    }

    public class PdeStatus
    {
        public int Mode, Keyboard, State, Nozzles;
        public PdeError Error { set; get; }
        public static PdeStatus Parse(string data)
        {
            if (data.Length < 4) throw new FormatException("Invalid status");
            var s = new PdeStatus();
            s.Mode = data[0] - '0';
            s.Keyboard = data[1] - '0';
            s.State = data[2] - '0';
            s.Nozzles = data[3] - '0';
            return s;
        }
    }

    public class PdeHistory
    {
        public uint Price, Volume;
        public ushort UnitPrice, Time, Date;
        public int Index;

        public static PdeHistory Parse(string data)
        {
            var h = new PdeHistory();
            h.Index = int.Parse(data.Substring(0, 2));
            string hex = data.Substring(2);
            if (hex.Length != 28) throw new FormatException("Invalid history hex length.");
            byte[] bytes = HexToBytes(hex);
            h.Price = ToUInt32BE(bytes, 0);
            h.Volume = ToUInt32BE(bytes, 4);
            h.UnitPrice = ToUInt16BE(bytes, 8);
            h.Time = ToUInt16BE(bytes, 10);
            h.Date = ToUInt16BE(bytes, 12);
            return h;
        }

        private static byte[] HexToBytes(string h)
        {
            var arr = new byte[h.Length / 2];
            for (int i = 0; i < arr.Length; i++)
                arr[i] = Convert.ToByte(h.Substring(i * 2, 2), 16);
            return arr;
        }
        private static uint ToUInt32BE(byte[] b, int o) { return (uint)(b[o] << 24 | b[o + 1] << 16 | b[o + 2] << 8 | b[o + 3]); }
        private static ushort ToUInt16BE(byte[] b, int o) { return (ushort)(b[o] << 8 | b[o + 1]); }
    }

    public class PdeText
    {
        public int Number;
        public string Text;
        public static PdeText Parse(string data)
        {
            var t = new PdeText();
            t.Number = int.Parse(data.Substring(0, 2));
            t.Text = Decode7BitText(data.Substring(2));
            ASFuelControl.Common.Logger.Instance.Trace(string.Format("Message DATA: {0}, {1}", t.Number, t.Text));
            return t;
        }

        public static string Decode7BitText(string data)
        {
            var bytes = Encoding.ASCII.GetBytes(data);
            var sb = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                byte b = bytes[i];
                if (b == 0x7E || b == 0x7F)
                {
                    if (i + 1 >= bytes.Length) break;
                    byte next = bytes[++i];
                    byte val = (byte)(next + (b == 0x7E ? 0x80 : 0xC0));
                    sb.Append((char)val);
                }
                else sb.Append((char)b);
            }
            return sb.ToString();
        }
    }

    public class PdeTextStatus
    {
        public int Number;
        public int Enabled;
        public static PdeTextStatus Parse(string data)
        {
            var ts = new PdeTextStatus();
            ts.Number = int.Parse(data.Substring(0, 2));
            ts.Enabled = int.Parse(data.Substring(2, 1));
            return ts;
        }
    }

    public class PdeRegisters
    {
        public int NozzleIndex;
        public int TransactionType;
        public decimal Volume;
        public static PdeRegisters Parse(string data)
        {
            if (string.IsNullOrEmpty(data) || data.Length < 14)
                throw new FormatException("Invalid X response");

            return new PdeRegisters
            {
                NozzleIndex = int.Parse(data.Substring(0, 1)),
                TransactionType = int.Parse(data.Substring(1, 1)),
                Volume = decimal.Parse(data.Substring(2, 12))
            };
        }
    }

    public class PdeTotalizers
    {
        public decimal TotalVolume;
        public decimal TotalAmount;

        public static PdeTotalizers Parse(string data)
        {
            if (data == null || data.Length < 18)
                throw new FormatException("Invalid totalizer data");

            var t = new PdeTotalizers();
            t.TotalVolume = decimal.Parse(data.Substring(0, 9)) / 100;
            t.TotalAmount = decimal.Parse(data.Substring(9, 9)) / 100;
            return t;
        }
    }


    public class PdeInitReq
    {
        public string Code;
        public PdeInitReq(string code) { Code = code; }
    }

    public class PdeRawInbound
    {
        public char Code;
        public string Data;
        public PdeRawInbound(char c, string d) { Code = c; Data = d; }
    }
    //public partial class TatsunoPdeClient : IDisposable
    //{
    //    private readonly IPdeTransport _transport;
    //    private readonly PdeFrameCodec _codec;
    //    private readonly PdeProtocolOptions _opt;

    //    public TatsunoPdeClient(IPdeTransport transport, PdeProtocolOptions options)
    //    {
    //        if (transport == null) throw new ArgumentNullException("transport");
    //        _transport = transport;
    //        _opt = options ?? new PdeProtocolOptions();
    //        _codec = new PdeFrameCodec(_opt);
    //    }

    //    public bool IsOpen { get { return _transport.IsOpen; } }

    //    public void Open() { _transport.Open(); }
    //    public void Close() { _transport.Close(); }
    //    public void Dispose() { _transport.Dispose(); }

    //    private PdeResult<T> Fail<T>(byte address, byte command, string error, byte[] raw, byte[] payload)
    //    {
    //        return new PdeResult<T>
    //        {
    //            Success = false,
    //            Error = error ?? "Error",
    //            Address = address,
    //            Command = command,
    //            RawFrame = raw ?? new byte[0],
    //            Payload = payload ?? new byte[0],
    //            Data = default(T)
    //        };
    //    }

    //    private PdeResult<T> Ok<T>(byte address, byte command, byte[] raw, byte[] payload, T data)
    //    {
    //        return new PdeResult<T>
    //        {
    //            Success = true,
    //            Error = string.Empty,
    //            Address = address,
    //            Command = command,
    //            RawFrame = raw ?? new byte[0],
    //            Payload = payload ?? new byte[0],
    //            Data = data
    //        };
    //    }

    //    private PdeResult<byte[]> SendAndReceiveRaw(byte address, byte command, byte[] payload, int timeoutMs, CancellationToken ct)
    //    {
    //        try
    //        {
    //            var frame = _codec.Build(address, command, payload ?? new byte[0]);
    //            var request = BitConverter.ToString(frame).Replace("-", "");

    //            Common.Logger.Instance.Debug("Request:" + request);
    //            _transport.Write(frame);

    //            // Read header: STX, Addr, Cmd, Len
    //            var hdr = new byte[4];
    //            int r = ReadExact(hdr, 0, hdr.Length, timeoutMs, ct);

    //            if (r == 0)
    //            {
    //                Common.Logger.Instance.Debug("Response: NO RESPONSE");
    //            }
    //            else
    //            {
    //                var response = BitConverter.ToString(hdr).Replace("-", "");
    //                Common.Logger.Instance.Debug("Response:" + response);
    //            }
    //            if (r != hdr.Length) return Fail<byte[]>(address, command, "Header timeout", new byte[0], new byte[0]);

    //            if (hdr[0] != _opt.Stx) return Fail<byte[]>(address, command, string.Format("Bad STX 0x{0:X2}", hdr[0]), new byte[0], new byte[0]);

    //            byte respAddr = hdr[1];
    //            byte respCmd = hdr[2];
    //            int len = hdr[3];

    //            // Read rest: payload + CRC(2) + ETX
    //            var rest = new byte[len + 2 + 1];
    //            r = ReadExact(rest, 0, rest.Length, timeoutMs, ct);
    //            if (r != rest.Length) return Fail<byte[]>(address, command, "Body timeout", new byte[0], new byte[0]);

    //            // Re-assemble raw
    //            var raw = new byte[hdr.Length + rest.Length];
    //            Buffer.BlockCopy(hdr, 0, raw, 0, hdr.Length);
    //            Buffer.BlockCopy(rest, 0, raw, hdr.Length, rest.Length);

    //            PdeFrame parsed;
    //            string parseErr;
    //            if (!_codec.TryParse(raw, out parsed, out parseErr))
    //                return Fail<byte[]>(address, command, parseErr ?? "Parse error", raw, new byte[0]);

    //            return Ok<byte[]>(address, command, raw, parsed.Payload, parsed.Payload);
    //        }
    //        catch (Exception ex)
    //        {
    //            return Fail<byte[]>(address, command, ex.Message, new byte[0], new byte[0]);
    //        }
    //    }

    //    private int ReadExact(byte[] buffer, int offset, int count, int timeoutMs, CancellationToken ct)
    //    {
    //        int total = 0;
    //        DateTime? deadline = timeoutMs > 0 ? (DateTime?)DateTime.UtcNow.AddMilliseconds(timeoutMs) : null;

    //        while (total < count)
    //        {
    //            if (ct.IsCancellationRequested) break;
    //            int budgetMs = 0;
    //            if (deadline.HasValue)
    //            {
    //                budgetMs = (int)Math.Ceiling((deadline.Value - DateTime.UtcNow).TotalMilliseconds);
    //                if (budgetMs <= 0) break;
    //            }
    //            int read = _transport.Read(buffer, offset + total, count - total, budgetMs, ct);
    //            if (read <= 0) continue;
    //            total += read;
    //        }
    //        return total;
    //    }

    //    // Parsing helpers for typical payload layouts (replace per your spec)
    //    private PdeAck ParseAck(byte[] p)
    //    {
    //        // [0]=status(0=OK,1=ERR), [1]=code, [2..]=ascii message (optional)
    //        bool ok = p != null && p.Length > 0 && p[0] == 0x00;
    //        byte code = p != null && p.Length > 1 ? p[1] : (byte)0;
    //        string msg = p != null && p.Length > 2 ? PdeFrameCodec.ReadAscii(p, 2, p.Length - 2) : string.Empty;
    //        return new PdeAck { Ok = ok, Code = code, Message = msg };
    //    }

    //    private PdeStatus ParseStatus(byte[] p)
    //    {
    //        // Example layout: [0]=flags, [1]=activeNozzle, [2..3]=errorCode(LE)
    //        byte flags = p != null && p.Length > 0 ? p[0] : (byte)0;
    //        return new PdeStatus
    //        {
    //            InService = (flags & 0x01) != 0,
    //            Busy = (flags & 0x02) != 0,
    //            Error = (flags & 0x04) != 0,
    //            ActiveNozzle = p != null && p.Length > 1 ? p[1] : (byte)0,
    //            ErrorCode = p != null && p.Length >= 4 ? Le.ReadUInt16(p, 2) : (ushort)0
    //        };
    //    }

    //    private PdeDisplay ParseDisplay(byte[] p)
    //    {
    //        // Example layout:
    //        // [0]=hose, [1..4]=amount minor unit (u32 LE), [5..8]=volume minor unit (u32 LE), [9..12]=price minor unit (u32 LE)
    //        int hose = p != null && p.Length > 0 ? p[0] : 0;
    //        decimal amount = p != null && p.Length >= 5 ? (decimal)Le.ReadUInt32(p, 1) / (decimal)_opt.AmountMinorUnit : 0m;
    //        decimal volume = p != null && p.Length >= 9 ? (decimal)Le.ReadUInt32(p, 5) / (decimal)_opt.VolumeMinorUnit : 0m;
    //        decimal price = p != null && p.Length >= 13 ? (decimal)Le.ReadUInt32(p, 9) / (decimal)_opt.PriceMinorUnit : 0m;
    //        return new PdeDisplay
    //        {
    //            HoseNumber = hose,
    //            Amount = amount,
    //            Volume = volume,
    //            UnitPrice = price,
    //            Currency = _opt.DefaultCurrency
    //        };
    //    }

    //    private static DateTime UnixSecondsToLocalDateTime(uint seconds)
    //    {
    //        var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    //        return epoch.AddSeconds(seconds).ToLocalTime();
    //    }

    //    private PdeHistory ParseHistory(int index, byte[] p)
    //    {
    //        // Example layout:
    //        // [0]=hose, [1..4]=amount, [5..8]=volume, [9..12]=price, [13..16]=unixTime(LE), [17..]=receipt ASCII
    //        int hose = p != null && p.Length > 0 ? p[0] : 0;
    //        decimal amount = p != null && p.Length >= 5 ? (decimal)Le.ReadUInt32(p, 1) / (decimal)_opt.AmountMinorUnit : 0m;
    //        decimal volume = p != null && p.Length >= 9 ? (decimal)Le.ReadUInt32(p, 5) / (decimal)_opt.VolumeMinorUnit : 0m;
    //        decimal price = p != null && p.Length >= 13 ? (decimal)Le.ReadUInt32(p, 9) / (decimal)_opt.PriceMinorUnit : 0m;
    //        DateTime ts = p != null && p.Length >= 17
    //            ? UnixSecondsToLocalDateTime(Le.ReadUInt32(p, 13))
    //            : DateTime.MinValue;
    //        string receipt = p != null && p.Length > 17 ? PdeFrameCodec.ReadAscii(p, 17, p.Length - 17) : string.Empty;
    //        return new PdeHistory
    //        {
    //            Index = index,
    //            HoseNumber = hose,
    //            Amount = amount,
    //            Volume = volume,
    //            UnitPrice = price,
    //            Timestamp = ts,
    //            ReceiptId = receipt
    //        };
    //    }

    //    private PdeRegisters ParseRegisters(int nozzle, byte[] p)
    //    {
    //        // Example layout:
    //        // [0..7]=volume impulses (u64 LE), [8..15]=amount cents (u64 LE)
    //        long volImp = p != null && p.Length >= 8 ? (long)Le.ReadUInt64(p, 0) : 0L;
    //        long amtCents = p != null && p.Length >= 16 ? (long)Le.ReadUInt64(p, 8) : 0L;
    //        return new PdeRegisters
    //        {
    //            HoseNumber = nozzle,
    //            TotalVolumeImpulses = volImp,
    //            TotalAmountCents = amtCents
    //        };
    //    }

    //    private PdeText ParseText(int number, byte[] p)
    //    {
    //        return new PdeText { Number = number, Value = PdeFrameCodec.ReadAscii(p ?? new byte[0], 0, p != null ? p.Length : 0) };
    //    }

    //    private PdeTextStatus ParseTextStatus(int number, byte[] p)
    //    {
    //        // [0]=flags bit0=displayed, bit1=ack
    //        byte flags = p != null && p.Length > 0 ? p[0] : (byte)0;
    //        return new PdeTextStatus
    //        {
    //            Number = number,
    //            Displayed = (flags & 0x01) != 0,
    //            Acknowledged = (flags & 0x02) != 0
    //        };
    //    }

    //    private PdeError ParseError(byte[] p)
    //    {
    //        // [0..1]=code, [2]=severity, [3..]=ascii
    //        ushort code = p != null && p.Length >= 2 ? Le.ReadUInt16(p, 0) : (ushort)0;
    //        byte sev = p != null && p.Length >= 3 ? p[2] : (byte)0;
    //        string desc = p != null && p.Length > 3 ? PdeFrameCodec.ReadAscii(p, 3, p.Length - 3) : string.Empty;
    //        return new PdeError { Code = code, Severity = sev, Description = desc };
    //    }
    //}
}