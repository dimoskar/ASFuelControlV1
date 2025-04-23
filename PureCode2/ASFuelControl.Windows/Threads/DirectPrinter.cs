using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace ASFuelControl.Windows.Threads
{
    [StructLayout(LayoutKind.Sequential)]
    public struct DOCINFO
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string pDocName;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string pOutputFile;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string pDataType;
    }

    public class DirectPrinter
    {
        public DirectPrinter()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        [DllImport("winspool.drv", CharSet = CharSet.Unicode, ExactSpelling = false,
             CallingConvention = CallingConvention.StdCall)]
        public static extern long OpenPrinter(string pPrinterName, ref IntPtr phPrinter, int pDefault);

        [DllImport("winspool.drv", CharSet = CharSet.Unicode, ExactSpelling = false,
             CallingConvention = CallingConvention.StdCall)]
        public static extern long StartDocPrinter(IntPtr hPrinter, int Level, ref DOCINFO pDocInfo);

        [DllImport("winspool.drv", CharSet = CharSet.Unicode, ExactSpelling = true,
             CallingConvention = CallingConvention.StdCall)]
        public static extern long StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", CharSet = CharSet.Ansi, ExactSpelling = true,
             CallingConvention = CallingConvention.StdCall)]
        public static extern long WritePrinter(IntPtr hPrinter, string data, int buf, ref int pcWritten);

        [DllImport("winspool.drv", CharSet = CharSet.Unicode, ExactSpelling = true,
             CallingConvention = CallingConvention.StdCall)]
        public static extern long EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", CharSet = CharSet.Unicode, ExactSpelling = true,
             CallingConvention = CallingConvention.StdCall)]
        public static extern long EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", CharSet = CharSet.Unicode, ExactSpelling = true,
             CallingConvention = CallingConvention.StdCall)]
        public static extern long ClosePrinter(IntPtr hPrinter);




        public static void SendToPrinter(string jobName, string PCL5Commands, string printerName)
        {
            System.IntPtr lhPrinter = new System.IntPtr();

            DOCINFO di = new DOCINFO();
            int pcWritten = 0;
            di.pDocName = jobName;
            di.pDataType = "RAW";

            //lhPrinter contains the handle for the printer opened
            //If lhPrinter is 0 then an error has occured

            // This code assumes you have a printer at share \\192.168.1.101\hpl
            // This code sends Hewlett Packard PCL5 codes to the printer

            //OpenPrinter("\\\\192.168.1.101\\hpl",ref lhPrinter,0);
            OpenPrinter(printerName, ref lhPrinter, 0);
            StartDocPrinter(lhPrinter, 1, ref di);
            StartPagePrinter(lhPrinter);
            WritePrinter(lhPrinter, PCL5Commands, PCL5Commands.Length, ref pcWritten);
            EndPagePrinter(lhPrinter);
            EndDocPrinter(lhPrinter);
            ClosePrinter(lhPrinter);

        }


    }
}
