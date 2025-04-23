using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.VeederRootTLSIB
{
    public class Commands
    {
        public static byte[] GetStatus(string NozzleID)
        {
            int NozzleID2 = 48 + Int32.Parse(NozzleID);
            byte[] send = new byte[] { 0x01, 0x69, 0x32, 0x30, 0x31, 0x30, (byte)NozzleID2 };
            return send;
        }
       
        public static string ConvertHex(String hexString)
        {
            try
            {
                string ascii = string.Empty;

                for (int i = 0; i < hexString.Length; i += 2)
                {
                    String hs = string.Empty;

                    hs = hexString.Substring(i, 2);
                    uint decval = System.Convert.ToUInt32(hs, 16);
                    char character = System.Convert.ToChar(decval);
                    ascii += character;

                }

                return ascii;
            }
            catch (Exception ex) { }

            return string.Empty;
        }
    }
}
