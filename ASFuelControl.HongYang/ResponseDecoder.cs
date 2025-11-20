using ASFuelControl.HongYang.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASFuelControl.HongYang
{
    public static class ResponseDecoder
    {
        public static PumpStateFlags DecodePumpState(byte stateByte) =>
            (PumpStateFlags)stateByte;

        public static int DecodeLongInt(byte[] data) =>
            BitConverter.ToInt32(data, 0);

        public static void DecodeTotals(byte[] param, ref int volume, ref int amount)
        {
            // Replace range operator with Array.Copy for C# 7.3 compatibility
            var volumeBytes = new byte[6];
            var amountBytes = new byte[6];
            Array.Copy(param, 0, volumeBytes, 0, 6);
            Array.Copy(param, 6, amountBytes, 0, 6);
            volume = BcdConverter.Decode(volumeBytes);
            amount = BcdConverter.Decode(amountBytes);
        }

        public static double DecodePrice(byte[] param, byte decimalType) =>
            BitConverter.ToInt32(param, 0) / Math.Pow(10, decimalType);
    }
}
