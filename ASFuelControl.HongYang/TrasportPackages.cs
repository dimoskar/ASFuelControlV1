using ASFuelControl.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASFuelControl.HongYang
{
    public class CommandPacket
    {
        public byte Address { get; set; }
        public Enums.CommandCode Command { get; set; }
        public byte[] Parameter { get; set; } = Array.Empty<byte>();

        public byte[] ToBytes()
        {
            var body = new List<byte>();

            // Length = Address + Command + Parameters + Checksum
            byte length = (byte)(1 + 1 + Parameter.Length + 1);

            body.Add(length);
            body.Add(Address);
            body.Add((byte)Command);
            body.AddRange(Parameter);

            byte checksum = 0;
            foreach (var b in body)
                checksum ^= b;

            var packet = new List<byte> { 0x02 }; // STX
            packet.AddRange(body);
            packet.Add(checksum);
            packet.Add(0x03); // ETX

            return packet.ToArray();

        }
    }
    public class ResponsePacket
    {
        public byte Length { get; set; }
        public byte State { get; set; }
        public byte[] Parameter { get; set; }
        public byte CRC { get; set; }
        public bool IsUnlocked
        {
            get
            {
                return (State & (1 << 4)) != 0;
            }
        }

        public static ResponsePacket Parse(byte[] data)
        {
            
            byte length = data[0];
            byte state = data[1];
            byte[] param = data.Skip(2).Take(length - 2).ToArray();
            byte crc = data[length - 1];

            if (crc != CalculateChecksum(data.Take(length - 1).ToArray()))
                throw new InvalidDataException("Checksum mismatch");

            return new ResponsePacket { Length = length, State = state, Parameter = param, CRC = crc };
        }
        public Common.Enumerators.FuelPointStatusEnum ParseStatus()
        {
            if((State & (1 << 3)) != 0)
                return Common.Enumerators.FuelPointStatusEnum.Work;
            if ((State & (1 << 5)) != 0)
                return Common.Enumerators.FuelPointStatusEnum.Nozzle;
            return Common.Enumerators.FuelPointStatusEnum.Idle;
        }
        public long ConvertBytesToLong(int skip, int take, bool isBigEndian = true)
        {
            var bytes = this.Parameter.Skip(skip).Take(take).ToArray();

            if (bytes == null || bytes.Length == 0 || bytes.Length > sizeof(long))
                throw new ArgumentException("Byte array must be 1 to 8 bytes long.");

            byte[] padded = new byte[8]; // long = 8 bytes

            if (isBigEndian)
            {
                // Copy to the end of padded array
                Buffer.BlockCopy(bytes, 0, padded, 8 - bytes.Length, bytes.Length);
                Array.Reverse(padded); // Convert to little-endian for BitConverter
            }
            else
            {
                // Copy to the start of padded array
                Buffer.BlockCopy(bytes, 0, padded, 0, bytes.Length);
            }

            return BitConverter.ToInt64(padded, 0);
        }
        public long ExtractUInt16LittleEndian(int skip, int take)
        {
            byte[] buffer = this.Parameter.Skip(skip).Take(take).ToArray();
            if (buffer == null || buffer.Length < 2)
                throw new ArgumentException("Buffer must contain at least 2 bytes.");

            return buffer[0] | (buffer[1] << 8);
        }
        public long DecodeDecimalFromReversedBytes(int skip, int take)
        {
            byte[] input = this.Parameter.Skip(skip).Take(take).ToArray();
            if (input == null || input.Length == 0)
                throw new ArgumentException("Input cannot be null or empty.");

            // Reverse the byte order
            byte[] reversed = input.Reverse().ToArray();

            // Concatenate each byte as a 2-digit decimal string (zero-padded)
            string digitString = string.Concat(reversed.Select(b => b.ToString("X2")));

            // Parse the result as an integer
            return long.Parse(digitString);
        }

        public static byte[] LongToBytes(long value, int length, bool bigEndian = true)
        {
            if (length < 1 || length > sizeof(long))
                throw new ArgumentOutOfRangeException(nameof(length), "Length must be between 1 and 8.");

            byte[] fullBytes = BitConverter.GetBytes(value); // little-endian by default

            if (bigEndian)
                Array.Reverse(fullBytes);

            byte[] result = new byte[length];

            // Copy the least significant bytes
            Array.Copy(fullBytes, fullBytes.Length - length, result, 0, length);

            return result;
        }

        public static byte CalculateChecksum(params byte[] data)
        {
            byte cs = 0;
            for (int i = 0; i < data.Length; i++)
            {
                cs -= data[i];
            }
            return cs;
        }
    }

    public static class BcdConverter
    {
        public static int Decode(byte[] bcd)
        {
            int result = 0;
            for (int i = 0; i < bcd.Length; i++)
            {
                int high = (bcd[i] >> 4) & 0x0F;
                int low = bcd[i] & 0x0F;
                result += (high * 10 + low) * (int)Math.Pow(100, i);
            }
            return result;
        }
    }

}
