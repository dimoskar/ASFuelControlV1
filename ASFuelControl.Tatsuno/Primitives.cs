// File: Pde.Protocol.cs (net462)
using System;
using System.Linq;
using System.Text;

namespace ASFuelControl.Tatsuno
{
    public class PdeProtocolOptions
    {
        public byte Stx { get; set; } = 0x02;
        public byte Etx { get; set; } = 0x03;
        public bool UseCrc16Modbus { get; set; } = true;

        // Scaling factors (tune to your installation)
        public int AmountMinorUnit { get; set; } = 100;   // cents per currency unit
        public int PriceMinorUnit { get; set; } = 1000;   // e.g., price x 1000
        public int VolumeMinorUnit { get; set; } = 1000;  // e.g., liters x 1000
        public string DefaultCurrency { get; set; } = "EUR";
    }

    public static class PdeCommands
    {
        // NOTE: Replace with actual Tatsuno PDE command IDs for your firmware
        public const byte Initialize = 0x01;
        public const byte Status = 0x10;
        public const byte Display = 0x11;
        public const byte LastTransaction = 0x12;
        public const byte Registers = 0x13;
        public const byte TextMessage = 0x14;
        public const byte TextStatus = 0x15;
        public const byte Error = 0x16;
        public const byte Authorize = 0x20;
        public const byte Control = 0x21;
        public const byte SetUnitPrice = 0x22;
    }

    internal struct PdeFrame
    {
        public byte Address;
        public byte Command;
        public byte[] Payload;
        public byte[] Raw;

        public PdeFrame(byte address, byte command, byte[] payload, byte[] raw)
        {
            Address = address;
            Command = command;
            Payload = payload;
            Raw = raw;
        }
    }

    internal static class Crc16
    {
        private static ushort CrcSum(byte[] data)
        {
            uint sum = 0;
            for (int i = 0; i < data.Length; i++) sum = (sum + data[i]) & 0xFFFF;
            return (ushort)sum;
        }
        // Modbus/RTU CRC16 (poly 0xA001), init 0xFFFF
        public static ushort Compute(byte[] frame)
        {
            int sum = 0;
            foreach (byte b in frame)
                sum += b;

            return (ushort)(sum & 0xFFFF); // modulo 2^16


            //ushort crc = 0xFFFF;
            //for (int i = offset; i < offset + count; i++)
            //{
            //    crc ^= data[i];
            //    for (int j = 0; j < 8; j++)
            //    {
            //        bool lsb = (crc & 0x0001) != 0;
            //        crc >>= 1;
            //        if (lsb) crc ^= 0xA001;
            //    }
            //}
            //return crc;
        }
    }

    internal static class Le
    {
        public static ushort ReadUInt16(byte[] p, int offset)
        {
            return (ushort)(p[offset] | (p[offset + 1] << 8));
        }

        public static uint ReadUInt32(byte[] p, int offset)
        {
            return (uint)(p[offset] | (p[offset + 1] << 8) | (p[offset + 2] << 16) | (p[offset + 3] << 24));
        }

        public static ulong ReadUInt64(byte[] p, int offset)
        {
            uint lo = ReadUInt32(p, offset);
            uint hi = ReadUInt32(p, offset + 4);
            return ((ulong)hi << 32) | lo;
        }

        public static void WriteUInt32(byte[] p, int offset, uint value)
        {
            p[offset + 0] = (byte)(value & 0xFF);
            p[offset + 1] = (byte)((value >> 8) & 0xFF);
            p[offset + 2] = (byte)((value >> 16) & 0xFF);
            p[offset + 3] = (byte)((value >> 24) & 0xFF);
        }
    }

    internal class PdeFrameCodec
    {
        private readonly PdeProtocolOptions _opt;

        public PdeFrameCodec(PdeProtocolOptions opt)
        {
            _opt = opt;
        }

        public byte[] Build(byte address, byte command, byte[] payload)
        {
            int len = payload != null ? payload.Length : 0;
            var raw = new byte[1 + 1 + 1 + 1 + len + 2 + 1]; // STX, Addr, Cmd, Len, Payload, CRC(2), ETX
            int i = 0;
            raw[0] = _opt.Stx;
            raw[1] = address;
            raw[2] = command;
            raw[3] = (byte)len;

            if (len > 0) Buffer.BlockCopy(payload, 0, raw, 3, len);
            i = 3 + len;
            raw[6 + len] = _opt.Etx;
            ushort crc = _opt.UseCrc16Modbus
                ? Crc16.Compute(raw)
                : (ushort)0;
            raw[4 + len] = (byte)((crc >> 8) & 0x7F);// (byte)((crc >> 8) & 0xFF);
            raw[5 + len] = (byte)(crc & 0x7F);// (byte)(crc & 0xFF);

            return raw;
        }

        public bool TryParse(byte[] buffer, out PdeFrame frame, out string error)
        {
            frame = default(PdeFrame);
            error = null;

            if (buffer == null || buffer.Length < 7) { error = "Too short"; return false; }
            if (buffer[0] != _opt.Stx) { error = "STX mismatch"; return false; }
            if (buffer[buffer.Length - 1] != _opt.Etx) { error = "ETX mismatch"; return false; }

            byte address = buffer[1];
            byte command = buffer[2];
            int len = buffer[3];

            if (buffer.Length != 1 + 1 + 1 + 1 + len + 2 + 1)
            {
                error = "Length mismatch";
                return false;
            }

            ushort crcRead = (ushort)(buffer[4 + len] | (buffer[4 + len + 1] << 8));
            ushort crcCalc = _opt.UseCrc16Modbus
                ? Crc16.Compute(buffer)
                : (ushort)0;

            if (crcCalc != crcRead)
            {
                error = "CRC mismatch";
                return false;
            }

            var payload = new byte[len];
            if (len > 0) Buffer.BlockCopy(buffer, 4, payload, 0, len);

            frame = new PdeFrame(address, command, payload, (byte[])buffer.Clone());
            return true;
        }

        public static string ReadAscii(byte[] p, int offset, int count)
        {
            if (p == null || p.Length == 0 || count <= 0) return string.Empty;
            int end = Math.Min(p.Length, offset + count);
            int n = 0;
            for (int i = offset; i < end; i++)
            {
                if (p[i] == 0x00) break;
                n++;
            }
            return Encoding.ASCII.GetString(p, offset, n);
        }
    }
}
