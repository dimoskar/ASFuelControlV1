using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;

namespace ASFuelControl.DartFalcon
{
    public class Commands
    {
        private static byte[] EncodePackedBcd(int value, int byteCount)
        {
            int safeValue = Math.Max(0, value);
            string digits = safeValue.ToString(CultureInfo.InvariantCulture).PadLeft(byteCount * 2, '0');
            if (digits.Length > byteCount * 2)
                throw new ArgumentOutOfRangeException("value", "Packed BCD value is larger than the target field.");

            return CRC.ConvertHexStringToByteArray(digits);
        }

        public static byte[] Poll(int Address)
        {
            byte[] cmd = new byte[] { (byte)(79 + Address), 0x20, 0xFA };
            return cmd;
        }
        public static byte[] ACK(int Address)
        {
            byte[] cmd = new byte[] { (byte)(79 + Address), 0xC0, 0xFA };
            return cmd;
        }

        public static byte[] Authorize(int Address)
        {
            byte[] Command = new byte[9];
            Command[0] = (byte)(79 + Address);
            Command[1] = 0x30;
            Command[2] = 0x01;
            Command[3] = 0x01;
            Command[4] = 0x06;
            byte[] ComCRC = Command.Take(5).ToArray();
            string CRC_ = CRC.ComputeChecksum(ComCRC).ToString("x2");
            byte[] crc4 = CRC.ConvertHexStringToByteArray(CRC_.PadLeft(4, '0'));
            Command[5] = crc4[1];
            Command[6] = crc4[0];
            Command[7] = 0x03;
            Command[8] = 0xFA;
            return Command;
        }

        public static byte[] GetDisplay(int Address)
        {
            byte[] Command = new byte[9];
            Command[0] = (byte)(79 + Address);
            Command[1] = 0x30;
            Command[2] = 0x01;
            Command[3] = 0x01;
            Command[4] = 0x04;
            byte[] ComCRC = Command.Take(5).ToArray();
            string CRC_ = CRC.ComputeChecksum(ComCRC).ToString("x2");
            byte[] crc4 = CRC.ConvertHexStringToByteArray(CRC_.PadLeft(4, '0'));
            Command[5] = crc4[1];
            Command[6] = crc4[0];
            Command[7] = 0x03;
            Command[8] = 0xFA;
            return Command;
        }

        public static byte[] Stop(int Address)
        {
            //50 30 01 01 05 5F 5F 03 FA
            byte[] Command = new byte[9];
            Command[0] = (byte)(79 + Address);
            Command[1] = 0x30;
            Command[2] = 0x01;
            Command[3] = 0x01;
            Command[4] = 0x08;
            byte[] ComCRC = Command.Take(5).ToArray();
            string CRC_ = CRC.ComputeChecksum(ComCRC).ToString("x2");
            byte[] crc4 = CRC.ConvertHexStringToByteArray(CRC_.PadLeft(4, '0'));
            Command[5] = crc4[1];
            Command[6] = crc4[0];
            Command[7] = 0x03;
            Command[8] = 0xFA;
            return Command;
        }
        public static byte[] GetStatus(int Address)
        {
            //50 30 01 01 05 5F 5F 03 FA
            byte[] Command = new byte[9];
            Command[0] = (byte)(79 + Address);
            Command[1] = 0x30;
            Command[2] = 0x01;
            Command[3] = 0x01;
            Command[4] = 0x00;
            byte[] ComCRC = Command.Take(5).ToArray();
            string CRC_ = CRC.ComputeChecksum(ComCRC).ToString("x2");
            byte[] crc4 = CRC.ConvertHexStringToByteArray(CRC_.PadLeft(4, '0'));
            Command[5] = crc4[1];
            Command[6] = crc4[0];
            Command[7] = 0x03;
            Command[8] = 0xFA;
            return Command;
        }
        public static byte[] Reset(int Address)
        {
            //50 30 01 01 05 5F 5F 03 FA
            byte[] Command = new byte[9];
            Command[0] = (byte)(79 + Address);
            Command[1] = 0x30;
            Command[2] = 0x01;
            Command[3] = 0x01;
            Command[4] = 0x05;
            byte[] ComCRC = Command.Take(5).ToArray();
            string CRC_ = CRC.ComputeChecksum(ComCRC).ToString("x2");
            byte[] crc4 = CRC.ConvertHexStringToByteArray(CRC_.PadLeft(4, '0'));
            Command[5] = crc4[1];
            Command[6] = crc4[0];
            Command[7] = 0x03;
            Command[8] = 0xFA;
            return Command;
        }
        public static byte[] AllowedNozzle(int Address, int noz)
        {
            byte[] Command = new byte[9];
            Command[0] = (byte)(79 + Address);
            Command[1] = 0x30;
            Command[2] = 0x02;
            Command[3] = 0x01;
            Command[4] = (byte)noz;
            byte[] ComCRC = Command.Take(5).ToArray();
            string CRC_ = CRC.ComputeChecksum(ComCRC).ToString("x2");
            byte[] crc4 = CRC.ConvertHexStringToByteArray(CRC_.PadLeft(4, '0'));
            Command[5] = crc4[1];
            Command[6] = crc4[0];
            Command[7] = 0x03;
            Command[8] = 0xFA;
            return Command;

        }

        public static byte[] RequestVolumeTotalizer(int Address, int Noz)
        {
            //50 30 65 01 01 1F 43 03 FA
            byte[] Command = new byte[9];
            Command[0] = (byte)(79 + Address);
            Command[1] = 0x30;
            Command[2] = 0x65;
            Command[3] = 0x01;
            Command[4] = (byte)Noz;

            byte[] ComCRC = Command.Take(5).ToArray();
            string CRC_ = CRC.ComputeChecksum(ComCRC).ToString("x2");
            byte[] crc4 = CRC.ConvertHexStringToByteArray(CRC_.PadLeft(4, '0'));
            Command[5] = crc4[1];
            Command[6] = crc4[0];
            Command[7] = 0x03;
            Command[8] = 0xFA;
            return Command;
        }

        public static byte[] SetPrice(Common.FuelPoint fp)
        {
            try
            {
                int totalNozzles = fp.Nozzles.Count();
                if (totalNozzles <= 0)
                    return null;

                byte[] command = new byte[8 + (3 * totalNozzles)];
                command[0] = (byte)(79 + fp.Address);
                command[1] = 0x30;
                command[2] = 0x05;
                command[3] = (byte)(3 * totalNozzles);

                for (int i = 0; i < totalNozzles; i++)
                {
                    byte[] priceBytes = EncodePackedBcd(fp.Nozzles[i].UntiPriceInt, 3);
                    int offset = 4 + (i * 3);
                    command[offset] = priceBytes[0];
                    command[offset + 1] = priceBytes[1];
                    command[offset + 2] = priceBytes[2];
                }

                byte[] commandForCrc = command.Take(4 + (3 * totalNozzles)).ToArray();
                string crc = CRC.ComputeChecksum(commandForCrc).ToString("x2");
                byte[] crcBytes = CRC.ConvertHexStringToByteArray(crc.PadLeft(4, '0'));
                int crcOffset = 4 + (3 * totalNozzles);
                command[crcOffset] = crcBytes[1];
                command[crcOffset + 1] = crcBytes[0];
                command[crcOffset + 2] = 0x03;
                command[crcOffset + 3] = 0xFA;

                Trace.WriteLine(BitConverter.ToString(command));
                return command;
            }
            catch (Exception)
            {
                return null;
            }
        }



        public static class CRC
        {
            const ushort polynomial = 0xA001;
            static readonly ushort[] table = new ushort[256];

            public static ushort ComputeChecksum(byte[] bytes)
            {
                ushort crc = 0;
                for (int i = 0; i < bytes.Length; ++i)
                {
                    byte index = (byte)(crc ^ bytes[i]);
                    crc = (ushort)((crc >> 8) ^ table[index]);
                }
                return crc;
            }
            static CRC()
            {
                ushort value;
                ushort temp;
                for (ushort i = 0; i < table.Length; ++i)
                {
                    value = 0;
                    temp = i;
                    for (byte j = 0; j < 8; ++j)
                    {
                        if (((value ^ temp) & 0x0001) != 0)
                        {
                            value = (ushort)((value >> 1) ^ polynomial);
                        }
                        else
                        {
                            value >>= 1;
                        }
                        temp >>= 1;
                    }
                    table[i] = value;
                }
            }

            public static byte[] ConvertHexStringToByteArray(string hexString)
            {
                if (hexString.Length % 2 != 0)
                {
                    throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, "The binary key cannot have an odd number of digits: {0}", hexString));
                }

                byte[] HexAsBytes = new byte[hexString.Length / 2];
                for (int index = 0; index < HexAsBytes.Length; index++)
                {
                    string byteValue = hexString.Substring(index * 2, 2);
                    HexAsBytes[index] = byte.Parse(byteValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                }

                return HexAsBytes;
            }
            public static byte[] HexToBytes(string input)
            {
                byte[] result = new byte[input.Length / 2];
                for (int i = 0; i < result.Length; i++)
                {
                    result[i] = Convert.ToByte(input.Substring(2 * i, 2), 16);
                }
                return result;
            }
        }
    }
}
