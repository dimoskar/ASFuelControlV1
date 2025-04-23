using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System
{
    public static class StringExtensions
    {
        public static string CenterString(this string stringToCenter, int totalLength)
        {
            return stringToCenter.PadLeft(((totalLength - stringToCenter.Length) / 2)
                                + stringToCenter.Length).PadRight(totalLength);
        }

        public static string RightString(this string stringToRight, int totalLength)
        {
            return stringToRight.PadLeft((totalLength - stringToRight.Length) + stringToRight.Length);
        }

        public static string LeftString(this string stringToLeft, int totalLength)
        {
            return stringToLeft.PadRight((totalLength - stringToLeft.Length) + stringToLeft.Length);
        }

        public static string EquilizeString(this string input, int totalLength)
        {
            if (input.Length > totalLength)
                return input.Replace("#", "").Substring(0, totalLength);
            int diff = totalLength - input.Length;
            diff = diff + 1;
            string diffString = "";
            diffString = diffString.PadLeft(diff);
            return input.Replace("#", diffString);
        }
    }
}
