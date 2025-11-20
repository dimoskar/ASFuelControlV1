using ASFuelControl.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ASFuelControl.HongYang
{
    public class SerialTransport : IDisposable
    {
        private readonly SerialPort _port;
        private readonly string _logFilePath = "HongYang_log.txt";


        public SerialTransport(string portName)
        {
            _port = new SerialPort(portName, 2400, Parity.Mark, 8, StopBits.One);
            _port.WriteTimeout = 500;
            _port.ReadTimeout = 500;
            //_port.ErrorReceived+= (s, e) =>
            //{
            //    Logger.Instance.Error($"Serial port error: {e.EventType}");
            //};
            //_port.PinChanged += (s, e) =>
            //{
            //    Logger.Instance.Warn($"Serial port pin changed: {e.EventType}");
            //};
        }

        bool sending = false;
        public ResponsePacket Send(byte address, byte[] data)
        {
            try
            {
                sending = true;
                DateTime dt = DateTime.Now;
                LogBuffer("SEND -> ", new byte[] { address }.Concat(data).ToArray());

                if (_port.IsOpen)
                {
                    _port.Close();
                    Thread.Sleep(5); // Give OS time to release handle
                }
                // Send address with MARK parity
                _port.Parity = Parity.Mark;
                _port.Open();
                _port.Write(new byte[] { address }, 0, 1);
                _port.BaseStream.Flush();
                System.Threading.Thread.Sleep(SerialDiagnostics.AddressDelayMs);
                
                _port.Close();
                System.Threading.Thread.Sleep(SerialDiagnostics.AddressDelayMs);

                // Send rest with SPACE parity

                _port.Parity = Parity.Space;
                _port.Open();
                _port.Write(data, 0, data.Length);
                _port.BaseStream.Flush();
                System.Threading.Thread.Sleep(SerialDiagnostics.PayloadDelayMs);

                // Read response
                int lengthByte = _port.ReadByte();
                if (lengthByte < 0)
                    throw new TimeoutException("No response received.");
                
                List<byte> foo = new List<byte>();
                foo.Add((byte)lengthByte);
                int bytesRead = 1;
                var timeout = DateTime.Now.AddMilliseconds(500); // configurable timeout
                while (bytesRead < lengthByte)
                {
                    try
                    {
                        var buffer = new byte[lengthByte - bytesRead];
                        int chunk = _port.Read(buffer, 0, lengthByte - bytesRead);
                        if (chunk == 0)
                            throw new TimeoutException("No data received — device stalled or disconnected.");
                        foo.AddRange(buffer.Take(chunk));
                        bytesRead += chunk;
                    }
                    catch (TimeoutException)
                    {
                        SerialDiagnostics.Log($"Timeout while reading at offset {bytesRead}");
                        break; // or throw, depending on your protocol expectations
                    }
                }

                //for (int i = 0; i < lengthByte; i++)
                //{
                //    try
                //    {
                //        int b = _port.ReadByte(); // blocks until byte arrives or timeout
                //        if (b < 0)
                //            throw new TimeoutException("No byte received — stream ended unexpectedly.");

                //        foo.Add((byte)b);
                //    }
                //    catch (TimeoutException)
                //    {
                //        SerialDiagnostics.Log($"Timeout while reading byte[{i}]");
                //        break; // or throw, depending on your protocol expectations
                //    }
                //}
                byte[] response = foo.ToArray();
                System.Threading.Thread.Sleep(50);
                LogBuffer("RECV <- ", new byte[] { (byte)lengthByte }.Concat(response).ToArray(), dt);
                
                return ResponsePacket.Parse(response);//, data[1]);
            }
            catch (InvalidDataException ide)
            {
                Logger.Instance.Error($"ERROR Parsing response: {ide.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"ERROR Sending: {ex.Message}");
                Logger.Instance.Error(ex.StackTrace);
                return null;
            }
            finally
            {
                sending = false;
            }
        }

        public byte[] Receive()
        {
            var buffer = new byte[256];
            int count = _port.Read(buffer, 0, buffer.Length);
            var received = buffer.Take(count).ToArray();
            LogBuffer("RECV", received);
            return received;
        }

        public byte[] ReceiveFullResponse()
        {
            var buffer = new List<byte>();
            var timeout = DateTime.Now.AddMilliseconds(100); // configurable

            while (DateTime.Now < timeout)
            {
                var chunk = Receive(); // ideally non-blocking or with timeout
                if (chunk != null && chunk.Length > 0)
                {
                    buffer.AddRange(chunk);

                    // Check if we have enough bytes to read declared length
                    if (buffer.Count >= 2)
                    {
                        int declaredLength = buffer[1];
                        if (buffer.Count >= declaredLength)
                            break; // full packet received
                    }
                }

                Thread.Sleep(5); // small delay to avoid busy loop
            }

            return buffer.ToArray();
        }
        public ResponsePacket ReceiveAndParse()
        {
            var buffer = new List<byte>();
            var timeout = DateTime.Now.AddMilliseconds(50); // configurable timeout

            while (DateTime.Now < timeout)
            {
                var chunk = Receive(); // should be non-blocking or timeout-aware
                if (chunk != null && chunk.Length > 0)
                {
                    buffer.AddRange(chunk);

                    // Wait until we have at least 2 bytes to read declared length
                    if (buffer.Count >= 2)
                    {
                        int declaredLength = buffer[1];

                        // Wait until full packet is received
                        if (buffer.Count >= declaredLength)
                        {
                            var packetBytes = buffer.Take(declaredLength).ToArray();
                            return ResponsePacket.Parse(packetBytes);
                        }
                    }
                }

                Thread.Sleep(10); // avoid busy loop
            }

            throw new TimeoutException("Incomplete response received from dispenser.");
        }
        public void Dispose() => _port?.Close();
        public bool IsConnected()
        {
            if (_port == null)
                return false;
            if (sending)
                return true;
            return _port.IsOpen ? true : false;
        }
        public void Open()
        {
            _port.Open();
        }
        public void Close()
        {
            _port.Close();
        }
        private void LogBuffer(string direction, byte[] buffer)
        {
            LogBuffer(direction, buffer, DateTime.MinValue);
        }
        private void LogBuffer(string direction, byte[] buffer, DateTime dt)
        {
            if(!System.IO.File.Exists(_logFilePath))
            {
                return;
            }

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var hexString = BitConverter.ToString(buffer).Replace("-", " ");
            var milliseconds = dt != DateTime.MinValue ? " Elapsed Time(ms): " + (DateTime.Now - dt).TotalMilliseconds.ToString("F0") : "";
            var logEntry = $"{timestamp} [{direction}] {hexString}{milliseconds}{Environment.NewLine}";
            System.IO.File.AppendAllText(_logFilePath, logEntry);
        }

    }
    public static class SerialDiagnostics
    {
        public static bool EnableParityLogging { get; set; } = true;
        public static bool EnableTimingDelays { get; set; } = true;
        public static int AddressDelayMs { get; set; } = 20;
        public static int PayloadDelayMs { get; set; } = 10;

        public static void Log(string message)
        {
            if (EnableParityLogging)
                Logger.Instance.Debug($"[SerialDiagnostics] {message}");
        }

        public static void Delay(int ms, string reason)
        {
            if (EnableTimingDelays)
            {
                Log($"Sleeping {ms}ms after {reason}");
                Thread.Sleep(ms);
            }
        }
    }
}
