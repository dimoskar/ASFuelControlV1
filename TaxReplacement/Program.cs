using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaxReplacement
{
    class Program
    {
        static void Main(string[] args)
        {
            int i = 0;
            System.IO.DirectoryInfo signDir = new System.IO.DirectoryInfo(Properties.Settings.Default.SignFolder);
            System.IO.DirectoryInfo outDir = new System.IO.DirectoryInfo(Properties.Settings.Default.OutFolder);
            while (true)
            {
                System.IO.FileInfo[] files = signDir.GetFiles();
                foreach(System.IO.FileInfo f in files)
                {
                    if (DateTime.Now.Subtract(f.CreationTime).TotalMilliseconds < 300)
                        continue;
                    i++;
                    string outFile = Properties.Settings.Default.OutFolder + "\\" + f.Name.Replace(".txt", ".out");
                    f.Delete();
                    System.Threading.Thread.Sleep(100);
                    string url = "http://147.102.24.100/myweb/q1.php?SIG=CFY9900000100010720416CBF73F32A1E087A3D812F0ED212EBF7B7988F124.00";
                    System.IO.File.WriteAllText(outFile, string.Format("[[ΔΦΣΣ]] ΒΛΑΒΗ ({0}) ΕΑΦΔΣΣ," + url, i), Encoding.GetEncoding(28597));
                    System.Threading.Thread.Sleep(200);
                }
                System.Threading.Thread.Sleep(250);
            }
        }
    }
}
