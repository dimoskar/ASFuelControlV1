using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.GVR
{
    public static class Extensions
    {
        public static byte[] skip(this byte[] value, int number)
        {
            int index1 = 0;
            byte[] numArray = new byte[value.Length - number + 1];
            for (int index2 = number; index2 < value.Length; ++index2)
            {
                numArray[index1] = value[index2];
                ++index1;
            }
            return numArray;
        }

        public static byte[] take(this byte[] value, int number)
        {
            byte[] numArray = new byte[number];
            for (int index = 0; index < number; ++index)
                numArray[index] = value[index];
            return numArray;
        }

        public static Decimal takeToDecimal(this byte[] value, int number)
        {
            return Decimal.Parse(BitConverter.ToString(Extensions.take(value, number)).Replace("-", ""));
        }

        public static List<byte[]> ValidResponses(this byte[] value)
        {
            List<byte[]> list = new List<byte[]>();
            byte target = (byte)253;
            byte num = (byte)251;
            byte[] numArray1 = new byte[0];
            int length = value.Length;
            foreach (int count in Extensions.indexesOfByte(value, target))
            {
                for (int index = count; index < length; ++index)
                {
                    if ((int)value[index] == (int)num)
                    {
                        byte[] numArray2 = Enumerable.ToArray<byte>(Enumerable.Take<byte>(Enumerable.Skip<byte>((IEnumerable<byte>)Enumerable.ToArray<byte>((IEnumerable<byte>)value), count), index - count + 1));
                        list.Add(numArray2);
                        break;
                    }
                }
            }
            return list;
        }

        public static List<int> indexesOfByte(this byte[] value, byte target)
        {
            List<int> list = new List<int>();
            for (int index = 0; index < value.Length; ++index)
            {
                if ((int)value[index] == (int)target)
                    list.Add(index);
            }
            return list;
        }
    }
}
