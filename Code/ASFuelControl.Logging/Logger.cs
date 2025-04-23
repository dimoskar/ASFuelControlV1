using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ASFuelControl.Logging
{
    public class Logger
    {
        private static Logger instance = null;

        public static Logger Instance
        {
            get
            {
                if (instance == null)
                    instance = new Logger();
                return instance;
            }
        }

        public string LogFile { set; get; }
        public int LogLevel { set; get; }

        //public void LogToFile(Common.FuelPointValues values, VirtualDevices.VirtualDispenser dispenser)
        //{
        //    if (this.LogLevel < 2)
        //        return;
        //    string fileName = this.LogFile.ToLower().Replace(".log", "") + "_" + dispenser.OfficialNumber.ToString() + "_" + dispenser.DispenserNumber.ToString() + ".log";
        //    FileInfo fi = new FileInfo(fileName);
        //    FileStream f = null;
        //    if (!fi.Exists)
        //    {
        //        f = fi.Create();
        //        f.Close();
        //    }
        //    string dt = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff");
        //    List<string> list = new List<string>();
        //    list.Add(string.Format("********************{0}********************", dt));
        //    list.Add(string.Format("AddressId               : {0}", dispenser.AddressId));
        //    list.Add(string.Format("ActiveNozzle            : {0}", values.ActiveNozzle));
        //    list.Add(string.Format("Current Price Total     : {0}", values.CurrentPriceTotal));
        //    list.Add(string.Format("Current Sale Price      : {0}", values.CurrentSalePrice));
        //    list.Add(string.Format("Current Temperatur      : {0}", values.CurrentTemperatur));
        //    list.Add(string.Format("Current Volume          : {0}", values.CurrentVolume));
        //    list.Add(string.Format("Status                  : {0}", values.Status));
        //    list.Add(string.Format("****************************{0}****************************", "LOG END"));
        //    File.AppendAllLines(fileName, list);
        //}

        public void LogToFile(string caption, Exception ex)
        {
            if (this.LogLevel == 0)
                return;
            try
            {
                FileInfo fi = new FileInfo(this.LogFile);
                FileStream f = null;
                if (!fi.Exists)
                {
                    f = fi.Create();
                    f.Close();
                }
                if (fi.Length > 100000000)
                {
                    string newFilName = this.LogFile.Replace(".log", "") + DateTime.Now.ToString("yyyy_MM_dd_HHmmss") + ".log";
                    File.Copy(this.LogFile, newFilName);
                    File.WriteAllText(fi.Name, "");
                }
                string caption2 = string.Format("{0} :  {1:dd/MM/yyyy HH/mm/ss.fff}", caption, DateTime.Now);
                File.AppendAllText(this.LogFile, "=====================" + caption2 + "=====================\r\n");
                File.AppendAllText(this.LogFile, ex.Message + "\r\n");
                Exception parent = ex;
                while (parent.InnerException != null)
                {
                    File.AppendAllText(this.LogFile, "--------------------" + "InnerExeption" + "--------------------\r\n");
                    File.AppendAllText(this.LogFile, ex.Message + "\r\n");
                    parent = ex.InnerException;
                }

                File.AppendAllText(this.LogFile, "--------------------" + "Trace" + "--------------------\r\n");
                File.AppendAllText(this.LogFile, ex.StackTrace + "\r\n");

                File.AppendAllText(this.LogFile, "\r\n");
            }
            catch
            {
                if (System.IO.File.Exists(this.LogFile))
                {
                    System.Threading.Thread.Sleep(500);
                    this.LogToFile(caption, ex);
                }
            }
        }

        public void LogToFile(string caption, string message)
        {
            if (this.LogLevel == 0)
                return;
            try
            {
                FileInfo fi = new FileInfo(this.LogFile);
                FileStream f = null;
                if (!fi.Exists)
                {
                    f = fi.Create();
                    f.Close();
                }
                if (fi.Length > 100000000)
                {
                    string newFilName = this.LogFile.Replace(".log", "") + DateTime.Now.ToString("yyyy_MM_dd_HHmmss") + ".log";
                    File.Copy(this.LogFile, newFilName);
                    File.WriteAllText(fi.Name, "");
                }
                string caption2 = string.Format("{0} :  {1:dd/MM/yyyy HH/mm/ss.fff}", caption, DateTime.Now);
                File.AppendAllText(this.LogFile, "=====================" + caption2 + "=====================\r\n");
                File.AppendAllText(this.LogFile, message + "\r\n");
                File.AppendAllText(this.LogFile, "------------------------------------------------\r\n");
                File.AppendAllText(this.LogFile, "\r\n");
            }
            catch
            {
                if (System.IO.File.Exists(this.LogFile))
                {
                    System.Threading.Thread.Sleep(500);
                    this.LogToFile(caption, message);
                }
            }
        }

        public void LogToFile(string caption, string message, int level)
        {
            if (this.LogLevel < level)
                return;
            try
            {
                FileInfo fi = new FileInfo(this.LogFile);
                FileStream f = null;
                if (!fi.Exists)
                {
                    f = fi.Create();
                    f.Close();
                }
                if (fi.Length > 100000000)
                {
                    string newFilName = this.LogFile.Replace(".log", "") + DateTime.Now.ToString("yyyy_MM_dd_HHmmss") + ".log";
                    File.Copy(this.LogFile, newFilName);
                    File.WriteAllText(fi.Name, "");
                }
                string caption2 = string.Format("{0} :  {1:dd/MM/yyyy HH/mm/ss.fff}", caption, DateTime.Now);
                File.AppendAllText(this.LogFile, "=====================" + caption2 + "=====================\r\n");
                File.AppendAllText(this.LogFile, message + "\r\n");
                File.AppendAllText(this.LogFile, "------------------------------------------------\r\n");
                File.AppendAllText(this.LogFile, "\r\n");
            }
            catch
            {
                if (System.IO.File.Exists(this.LogFile))
                {
                    System.Threading.Thread.Sleep(500);
                    this.LogToFile(caption, message);
                }
            }
        }

        public void LogToFile(string caption, Common.Sales.SaleData sale)
        {
            if (this.LogLevel < 3)
                return;
            try
            {
                FileInfo fi = new FileInfo(this.LogFile);
                FileStream f = null;
                if (!fi.Exists)
                {
                    f = fi.Create();
                    f.Close();
                }
                if (fi.Length > 100000000)
                {
                    string newFilName = this.LogFile.Replace(".log", "") + DateTime.Now.ToString("yyyy_MM_dd_HHmmss") + ".log";
                    File.Copy(this.LogFile, newFilName);
                    File.WriteAllText(fi.Name, "");
                }
                string caption2 = string.Format("{0} :  {1:dd/MM/yyyy HH:mm:ss.fff}", caption, DateTime.Now);
                File.AppendAllText(this.LogFile, "=====================" + caption2 + "=====================\r\n");
                string logData = string.Format("Nozzle Id : {0}", sale.NozzleId);
                logData = logData + "\r\n" + string.Format("FuelType : {0}", sale.FuelTypeDescription);
                logData = logData + "\r\n" + string.Format("TotalizerStart : {0}", sale.TotalizerStart);
                logData = logData + "\r\n" + string.Format("TotalizerEnd : {0}", sale.TotalizerEnd);
                logData = logData + "\r\n" + string.Format("TotalVolume : {0}", sale.TotalVolume);
                logData = logData + "\r\n" + string.Format("TotalPrice : {0}", sale.TotalPrice);
                logData = logData + "\r\n" + string.Format("UnitPrice : {0}", sale.UnitPrice);
                logData = logData + "\r\n" + string.Format("-----------------------------------------------------------");
                foreach (Common.Sales.TankSaleData tankData in sale.TankData)
                {
                    logData = logData + "\r\n" + string.Format("StartLevel : {0}, EndLevel : {1}, TankId : {2}", tankData.StartLevel, tankData.EndLevel, tankData.TankId);
                }
                logData = logData + "\r\n" + string.Format("-----------------------------------------------------------");
                logData = logData + "\r\n";
                File.AppendAllText(this.LogFile, logData);
                File.AppendAllText(this.LogFile, "------------------------------------------------\r\n");
                File.AppendAllText(this.LogFile, "\r\n");
            }
            catch
            {
                if (System.IO.File.Exists(this.LogFile))
                {
                    System.Threading.Thread.Sleep(500);
                    this.LogToFile(caption, sale);
                }
            }
        }
    }
}

