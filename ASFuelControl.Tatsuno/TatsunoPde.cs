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
    public enum PayloadFormat
    {
        Ascii,
        Hex
    }


    public class TatsunoPdeClient : IDisposable
    {
        private readonly SerialPort _port;
        private readonly PdeOptions _opt;
        private readonly Encoding _enc = Encoding.ASCII;

        private readonly Queue<Tuple<byte, object>> _responsePool = new Queue<Tuple<byte, object>>();
        private readonly object _responseLock = new object();

        private readonly AckTracker _ackTracker = new AckTracker();
        private readonly List<string> _log = new List<string>();
        private readonly object _logLock = new object();

        public PayloadFormat LogPayloadFormat { get; set; }

        public TatsunoPdeClient(SerialPort port, PdeOptions options = null)
        {
            _opt = options ?? new PdeOptions();
            _port = port;
            _port.ReadTimeout = 20;
            _port.WriteTimeout = 100;
            _port.Handshake = Handshake.None;
            _port.Encoding = Encoding.ASCII;
            LogPayloadFormat = PayloadFormat.Hex;
        }

        public void Open() { if (!_port.IsOpen) _port.Open(); }
        public bool IsOpen() { return _port.IsOpen; }
        public void Close() { if (_port.IsOpen) _port.Close(); }
        public void Dispose() { try { Close(); } catch { } _port.Dispose(); }

        public IEnumerable<string> GetLogLines()
        {
            lock (_logLock)
            {
                var lines = _log.ToList();
                _log.Clear();             
                return lines;
            }
        }

        private void LogEvent(string addressHex, string message)
        {
            lock (_logLock)
            {
                var line = string.Format("{0:HH:mm:ss.fff} [{1}] {2}", DateTime.UtcNow, addressHex, message);
                _log.Add(line);
                if (_log.Count > 1000) _log.RemoveAt(0);
            }
        }

        private string FormatPayload(byte[] payload)
        {
            if (LogPayloadFormat == PayloadFormat.Hex)
                return BitConverter.ToString(payload);
            return _enc.GetString(payload);
        }

        public AckResult SendRequest(byte address, char code, string payload)
        {
            var bytes = _enc.GetBytes(payload);
            return SendRequest(address, code, bytes);
        }

        public AckResult SendRequest(byte address, char code, byte[] payload)
        {
            var inner = new byte[1 + 1 + payload.Length + 1];
            int p = 0;
            inner[p++] = Ctrl.STX;
            inner[p++] = (byte)code;
            Buffer.BlockCopy(payload, 0, inner, p, payload.Length);
            p += payload.Length;
            inner[p] = Ctrl.ETX;

            ushort crc = CrcSum(inner);
            byte crcHi = (byte)((crc >> 8) & 0x7F);
            byte crcLo = (byte)(crc & 0x7F);

            var frame = new byte[_opt.SynCount + 1 + inner.Length + 2];
            p = 0;
            for (int i = 0; i < _opt.SynCount; i++) frame[p++] = Ctrl.SYN;
            frame[p++] = address;
            Buffer.BlockCopy(inner, 0, frame, p, inner.Length);
            p += inner.Length;
            frame[p++] = crcHi;
            frame[p++] = crcLo;

            Write(frame);
            LogEvent(address.ToString("X2"), "Request: " + code + " " + FormatPayload(frame));

            
            long deadline = NowMs() + _opt.TkbMs;
            if (!ReadUntil(Ctrl.SYN, deadline))
            {
                Common.Logger.Instance.Debug(string.Format("SendSelect Read Until Failed, Data Sent: {0},  Data Frame End: {1}", code, BitConverter.ToString(frame)));
                return null;    
            }
            int b;
            do
            {
                b = ReadOne(deadline);
            }
            while (b == Ctrl.SYN);
            int ack = ReadOne(NowMs() + _opt.TkbMs);
            _ackTracker.RegisterPendingAck(address);
            
            var result = _ackTracker.ProcessAck((byte)b, (byte)ack, address);

            string status = (ack == Ctrl.ACK) ? "ACK" : "NAK";
            string match = result.AddressMatched ? "matched" : "mismatched (expected " + address.ToString("X2") + ")";
            LogEvent(b.ToString("X2"), status + ": " + match);

            return result;
        }
        private bool SendPoll(byte _addr, out byte responseAddress, out byte[] payload, out byte[] envelope)
        {
            responseAddress = _addr;
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
                responseAddress = (byte)b;

            int next = ReadOne(deadline);
            if (next == Ctrl.CAN)
                return true;
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
            LogEvent(responseAddress.ToString("X2"), "Poll Response: " + FormatPayload(envelope));
            return true;
        }
        public Tuple<byte, object> PollOnce(byte _addr)
        {
            byte[] payload;
            byte[] envelope;
            byte responseAddress = 0;
            if (!SendPoll(_addr, out responseAddress, out payload, out envelope))
            {
                Common.Logger.Instance.Trace("PollOnce Failed");
                return null;
            }
            if (!VerifyCrc(envelope))
            {
                Common.Logger.Instance.Trace("PollOnce Failed because of CRC Error");
                WriteAck(responseAddress, Ctrl.NAK);
                return null;
            }
            WriteAck(responseAddress, Ctrl.ACK);
            char code = (char)payload[0];
            string data = _enc.GetString(payload, 1, payload.Length - 1);
            Common.Logger.Instance.Debug("PollOnce, Code: " + code + ". Data Recieved: " + data);
            try
            {
                switch (code)
                {
                    case 'd':
                        Common.Logger.Instance.Trace("Display Data");
                        return new Tuple<byte, object>(responseAddress, PdeDisplay.Parse(data));
                    case 'e':
                        Common.Logger.Instance.Trace("Error Data");
                        return new Tuple<byte, object>(responseAddress, PdeError.Parse(data));
                    case 's':
                        Common.Logger.Instance.Trace("Status Data");
                        return new Tuple<byte, object>(responseAddress, PdeStatus.Parse(data));
                    case 'h':
                        Common.Logger.Instance.Trace("History Data");
                        return new Tuple<byte, object>(responseAddress, PdeHistory.Parse(data));
                    case 'm':
                        Common.Logger.Instance.Trace("Msssage Data");
                        return new Tuple<byte, object>(responseAddress, PdeText.Parse(data));
                    case 'o':
                        Common.Logger.Instance.Trace("Text Status Data");
                        return new Tuple<byte, object>(responseAddress, PdeTextStatus.Parse(data));
                    case 'x':
                        Common.Logger.Instance.Trace("Registers Data");
                        return new Tuple<byte, object>(responseAddress, PdeRegisters.Parse(data));
                    case 'i':
                        Common.Logger.Instance.Trace("Init Data");
                        return new Tuple<byte, object>(responseAddress, new PdeInitReq(data));
                    default:
                        Common.Logger.Instance.Trace("Other Data");
                        return new Tuple<byte, object>(responseAddress, new PdeRawInbound(code, data));
                }
            }
            catch (Exception ex)
            {
                Common.Logger.Instance.Debug("Wrong Data Recieved");
                return null;
            }
        }
        public void ReadAndPoolResponse(byte address)
        {
            var response = PollOnce(address);
            if (response != null)
            {
                lock (_responseLock)
                {
                    _responsePool.Enqueue(response);
                    if (_responsePool.Count > 100)
                        _responsePool.Dequeue();
                }
            }
            else
            {
                LogEvent("00", "No valid response received.");
            }
        }

        public Tuple<byte, object> GetNextResponse(int timeoutMs)
        {
            long deadline = NowMs() + timeoutMs;
            while (NowMs() < deadline)
            {
                lock (_responseLock)
                {
                    if (_responsePool.Count > 0)
                        return _responsePool.Dequeue();
                }
                Thread.Sleep(10);
            }
            return null;
        }

        private Tuple<byte, object> TryReadResponse(byte address)
        {
            if (!ReadUntil(Ctrl.SYN, NowMs() + _opt.TkaMs)) return null;

            int b;
            do { b = ReadOne(NowMs() + _opt.TkaMs); } while (b == Ctrl.SYN);
            byte fromAddr = (byte)b;

            int next = ReadOne(NowMs() + _opt.TkaMs);
            if (next == Ctrl.CAN || next != Ctrl.STX) return null;

            var buf = new byte[64];
            int p = 0;
            while (true)
            {
                int by = ReadOne(NowMs() + _opt.TkaMs);
                if (by == Ctrl.ETX) break;
                if (p == buf.Length) Array.Resize(ref buf, buf.Length * 2);
                buf[p++] = (byte)by;
            }

            int hi = ReadOne(NowMs() + _opt.TkaMs);
            int lo = ReadOne(NowMs() + _opt.TkaMs);

            var envelope = new byte[1 + p + 1 + 2];
            int q = 0;
            envelope[q++] = Ctrl.STX;
            Buffer.BlockCopy(buf, 0, envelope, q, p); q += p;
            envelope[q++] = Ctrl.ETX;
            envelope[q++] = (byte)hi;
            envelope[q++] = (byte)lo;

            bool crcOk = VerifyCrc(envelope);
            if (!crcOk)
            {
                WriteAck(fromAddr, Ctrl.NAK);
                LogEvent(fromAddr.ToString("X2"), "NAK for response: CRC error");
                return null;
            }

            WriteAck(fromAddr, Ctrl.ACK);
            LogEvent(fromAddr.ToString("X2"), "ACK for response: valid CRC");

            char code = (char)buf[0];
            string data = _enc.GetString(buf, 1, p - 1);
            LogEvent(fromAddr.ToString("X2"), FormatPayload(buf));

            object parsed;
            if (code == 'd')
                parsed = PdeDisplay.Parse(data);
            else if (code == 'e')
                parsed = PdeError.Parse(data);
            else if (code == 's')
                parsed = PdeStatus.Parse(data);
            else if (code == 'h')
                parsed = PdeHistory.Parse(data);
            else if (code == 'm')
                parsed = PdeText.Parse(data);
            else if (code == 'o')
                parsed = PdeTextStatus.Parse(data);
            else if (code == 'x')
                parsed = PdeRegisters.Parse(data);
            else if (code == 'i')
                parsed = new PdeInitReq(data);
            else
                parsed = new PdeRawInbound(code, data);

            return Tuple.Create(fromAddr, parsed);
        }

        private void WriteAck(byte addr, byte ack)
        {
            var w = new byte[_opt.SynCount + 2];
            int p = 0;
            for (int i = 0; i < _opt.SynCount; i++) w[p++] = Ctrl.SYN;
            w[p++] = addr;
            w[p++] = ack;
            Write(w);
        }

        private void Write(byte[] data)
        {
            _port.RtsEnable = false;
            _port.Write(data, 0, data.Length);
        }

        private bool ReadUntil(byte target, long deadlineMs)
        {
            while (NowMs() < deadlineMs)
            {
                int b = TryReadByte(deadlineMs);
                if (b == target) return true;
            }
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
                if (!_port.IsOpen) _port.Open();
                if (_port.BytesToRead > 0)
                    return _port.ReadByte();
                Thread.Sleep(1);
                return -1;
            }
            catch
            {
                Thread.Sleep(10);
                return -1;
            }
        }

        private static long NowMs()
        {
            return Environment.TickCount;
        }

        private static ushort CrcSum(byte[] data)
        {
            uint sum = 0;
            for (int i = 0; i < data.Length; i++)
                sum = (sum + data[i]) & 0xFFFF;
            return (ushort)sum;
        }

        private bool VerifyCrc(byte[] envelope)
        {
            if (envelope.Length < 4) return false;
            int len = envelope.Length;
            ushort calc = CrcSum(SubArray(envelope, 0, len - 2));
            byte hi = (byte)((calc >> 8) & 0x7F);
            byte lo = (byte)(calc & 0x7F);
            return envelope[len - 2] == hi && envelope[len - 1] == lo;
        }

        private static byte[] SubArray(byte[] src, int index, int count)
        {
            var r = new byte[count];
            Buffer.BlockCopy(src, index, r, 0, count);
            return r;
        }

        public static byte[] AuthCompute(string received8Ascii, byte adr)
        {
            if (received8Ascii.Length != 8)
                throw new ArgumentException("Auth requires 8 chars.");

            byte[] R = new byte[8];
            int C = adr + 1;

            for (int i = 0; i < 8; i++)
            {
                C = C * (byte)received8Ascii[i];
                int low = C & 0xFF;
                low &= 0x7F;
                C = (C & ~0xFF) | low;
                if ((C & 0xFF) < 0x20)
                    C += 0x20;
                R[i] = (byte)(C & 0xFF);
                C = (C >> 8) + adr;
            }

            return R;
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
}