// File: Pde.Transport.cs (net462)
using System;
using System.IO.Ports;
using System.Threading;

namespace ASFuelControl.Tatsuno
{
    public interface IPdeTransport : IDisposable
    {
        void Open();
        void Close();
        void Write(byte[] data);
        int Read(byte[] buffer, int offset, int count, int timeoutMs, CancellationToken ct);
        bool IsOpen { get; }
    }

    public class SerialPortTransport : IPdeTransport
    {
        private readonly SerialPort _port;

        public SerialPortTransport(
            string portName,
            int baudRate = 9600,
            Parity parity = Parity.Even,
            int dataBits = 7,
            StopBits stopBits = StopBits.Two)
        {
            _port = new SerialPort(portName, baudRate, parity, dataBits, stopBits)
            {
                Handshake = Handshake.None,
                ReadTimeout = 100,
                WriteTimeout = 1000
            };
        }

        public bool IsOpen { get { return _port.IsOpen; } }

        public void Open()
        {
            if (!_port.IsOpen) _port.Open();
        }

        public void Close()
        {
            if (_port.IsOpen) _port.Close();
        }

        public void Dispose()
        {
            _port.Dispose();
        }

        public void Write(byte[] data)
        {
            if (data == null || data.Length == 0) return;
            _port.Write(data, 0, data.Length);
        }

        public int Read(byte[] buffer, int offset, int count, int timeoutMs, CancellationToken ct)
        {
            if (buffer == null) throw new ArgumentNullException("buffer");
            if (offset < 0 || count < 0 || offset + count > buffer.Length) throw new ArgumentOutOfRangeException("offset/count");

            int total = 0;
            DateTime? deadline = timeoutMs > 0 ? (DateTime?)DateTime.UtcNow.AddMilliseconds(timeoutMs) : null;

            while (total < count)
            {
                if (ct.IsCancellationRequested) break;

                int budgetMs = 0;
                if (deadline.HasValue)
                {
                    budgetMs = (int)Math.Ceiling((deadline.Value - DateTime.UtcNow).TotalMilliseconds);
                    if (budgetMs <= 0) break;
                }

                try
                {
                    _port.ReadTimeout = deadline.HasValue ? Math.Max(1, Math.Min(100, budgetMs)) : SerialPort.InfiniteTimeout;
                    int read = _port.Read(buffer, offset + total, count - total);
                    if (read <= 0) continue;
                    total += read;
                }
                catch (TimeoutException)
                {
                    // loop and recompute remaining budget
                }
            }

            return total;
        }
    }
}