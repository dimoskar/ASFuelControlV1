using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ASFuelControl.RFID
{
    public class Logger
    {
        public static void Output(string FileNameToSave, Exception ExceptionData)
        {
            using (StreamWriter streamWriter = new StreamWriter(string.Concat("RFID_", FileNameToSave, "_LOG.txt"), true, Encoding.UTF8))
            {
                string[] voidMethodName = new string[] { "-->", "RFID Exception", "  ", null, null, null, null };
                voidMethodName[3] = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss.fff");
                voidMethodName[4] = "   <--- \r\n";
                voidMethodName[5] = ExceptionData.ToString();
                voidMethodName[6] = "\r\n\r\n";
                streamWriter.Write(string.Concat(voidMethodName));
            }
        }
    }
}
