using System;
using System.Collections.Generic;
using System.Text;
using PTSLib.Unipump;

namespace PTSLib.PTS
{
    /// <summary>
    /// Provides methods for various data conversions for processing of PTS controller messages.
    /// </summary>
    public static class AsciiConversion
    {
        /// <summary>
        /// Method for conversion from ascii bytes to byte.
        /// </summary>
        /// <param name="asciiByte">Initial ascii bytes value.</param>
        public static byte AsciiToByte(byte asciiByte)
        {
            if (asciiByte == 0)
                return 0;

            if (asciiByte >= 0x30 && asciiByte <= 0x39)
            {
                asciiByte -= 0x30; //0-9
                return asciiByte;
            }

            if (asciiByte >= 0x3a && asciiByte <= 0x40)
            {
                asciiByte -= 0x30; //for devices with addresses 10 - 16
                return asciiByte;
            }

            if (asciiByte >= 0x41 && asciiByte <= 0x46)
            {
                asciiByte -= 0x37; //A-F
                return asciiByte;
            }

            if (asciiByte >= 0x61 && asciiByte <= 0x66)
            {
                asciiByte -= 0x57; //a-f
                return asciiByte;
            }

            throw new ArgumentOutOfRangeException();
        }

        /// <summary>
        /// Method for conversion from bytes to ascii byte.
        /// </summary>
        /// <param name="ordByte">Initial bytes value.</param>
        public static byte ByteToAscii(byte ordByte)
        {
            if (ordByte > 16) throw new ArgumentOutOfRangeException();
            return (byte)(ordByte | 0x30);
        }

        /// <summary>
        /// Method for conversion from bytes to ascii byte.
        /// </summary>
        /// <param name="ordByte">Initial bytes value.</param>
        public static byte ByteToAsciiExt(byte ordByte)
        {
            if (ordByte > 16) throw new ArgumentOutOfRangeException();

            if (ordByte == 0)
                return 0;
            else
                return (byte)(ordByte | 0x30);
        }

        /// <summary>
        /// Method for conversion from array of ascii bytes to int.
        /// </summary>
        /// <param name="asciiBytes">Initial array of ascii bytes.</param>
        public static int AsciiToInt(params byte[] asciiBytes)
        {
            int result = 0;
            byte bcdByte;

            for (int i = 0; i < asciiBytes.Length; i++)
            {
                bcdByte = asciiBytes[i];
                bcdByte -= 0x30;
                result = result * 10 + bcdByte;
            }

            return result;
        }

        /// <summary>
        /// Method for conversion from int to array of ascii bytes.
        /// </summary>
        /// <param name="value">Initial int value.</param>
        /// <param name="length">Length of int value.</param>
        public static byte[] IntToAscii(int value, int length)
        {
            List<byte> result = new List<byte>();

            while (value != 0)
            {
                result.Insert(0, (byte)((value % 10) | 0x30));
                value /= 10;
            }

            if (length > result.Count)
            {
                int count = length - result.Count;

                for (int i = 0; i < count; i++)
                    result.Insert(0, 0x30);
            }

            return result.ToArray();
        }

        /// <summary>
        /// Method for conversion from int to array of ascii bytes.
        /// </summary>
        /// <param name="value">Initial int value.</param>
        public static byte[] IntToAsciiExt(int value)
        {
            List<byte> result = new List<byte>();

            if (value > 0)
            {
                while (value != 0)
                {
                    result.Insert(0, (byte)((value % 10) | 0x30));
                    value /= 10;
                }
            }
            else
            {
                result.Insert(0, 0x30);
            }

            return result.ToArray();
        }

        /// <summary>
        /// Method for conversion from array of ascii hexidecimal bytes to int.
        /// </summary>
        /// <param name="asciiBytes">Initial array of ascii hexidecimal bytes.</param>
        public static int HexAsciiToInt(params byte[] asciiBytes)
        {
            int result = 0;
            byte asciiByte;

            for (int i = 0; i < asciiBytes.Length; i++)
            {
                asciiByte = asciiBytes[i];

                if (asciiByte >= 0x30 && asciiByte <= 0x39)
                    asciiByte -= 0x30; //0-9
                if (asciiByte >= 0x41 && asciiByte <= 0x46)
                    asciiByte -= 0x37; //A-F
                if (asciiByte >= 0x61 && asciiByte <= 0x66)
                    asciiByte -= 0x57; //a-f

                result = result * 16 + asciiByte;
            }

            return result;
        }

        /// <summary>
        /// Method for conversion from array of bytes to string.
        /// </summary>
        /// <param name="bytesArray">Initial array of bytes.</param>
        public static string BytesArrayToString(params byte[] bytesArray)
        {
            string result = string.Empty;

            if (bytesArray == null)
                return result;

            for (int i = 0; i < bytesArray.Length; i++)
            {
                if (bytesArray[i] >= 0x30 && bytesArray[i] <= 0x39)
                    result += bytesArray[i] - 0x30; //0-9
                if (bytesArray[i] == 0x41 || bytesArray[i] == 0x61) //0xA or 0xa
                    result += 'A';
                if (bytesArray[i] == 0x42 || bytesArray[i] == 0x62) //0xB or 0xb
                    result += 'B';
                if (bytesArray[i] == 0x43 || bytesArray[i] == 0x63) //0xC or 0xc
                    result += 'C';
                if (bytesArray[i] == 0x44 || bytesArray[i] == 0x64) //0xD or 0xd
                    result += 'D';
                if (bytesArray[i] == 0x45 || bytesArray[i] == 0x65) //0xE or 0xe
                    result += 'E';
                if (bytesArray[i] == 0x46 || bytesArray[i] == 0x66) //0xF or 0xf
                    result += 'F';
                if (bytesArray[i] == UnipumpUtils.uExtendedSeparator) //;
                    result += ';';
            }

            return result;
        }

        /// <summary>
        /// Method for conversion from string to array of bytes.
        /// </summary>
        /// <param name="initString">Initial string value.</param>
        public static byte[] StringToBytesArray(string initString)
        {
            List<byte> result = new List<byte>();
            char[] initCharArray = initString.ToCharArray();

            for (int i = 0; i < initCharArray.Length; i++)
                result.Add(Convert.ToByte(initCharArray[i]));

            return result.ToArray();
        }
    }
}
