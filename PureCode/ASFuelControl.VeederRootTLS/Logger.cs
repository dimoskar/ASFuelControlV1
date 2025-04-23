using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ASFuelControl.VeederRootTLS
{
    public class Logger
    {
        public static void Output(string FileNameToSave, string Error_Recieve, string VoidMethodName)
        {
            string fileName = "Logs/VeederRootTLS_" + FileNameToSave + "_LOG.txt";

            using (StreamWriter writer = new StreamWriter(fileName, true, Encoding.UTF8))
            {
                writer.Write("-->" + VoidMethodName + "  " + DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss.fff") + "   <--- \r\n" + Error_Recieve.ToString() + "\r\n\r\n");
            }
        }
    }
}
