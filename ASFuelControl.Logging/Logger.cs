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
                string caption2 = string.Format("{0} :  {1:dd/MM/yyyy HH:mm:ss.fff}", caption, DateTime.Now);
                File.AppendAllText(this.LogFile, "=====================" + caption2 + "=====================\r\n");
                File.AppendAllText(this.LogFile, ex.Message + "\r\n");
                Exception parent = ex;
                int level = 0;
                string tabbed = "\t";
                while (parent.InnerException != null && level < 5)
                {
                    File.AppendAllText(this.LogFile, tabbed + "--------------------" + "InnerExeption" + "--------------------\r\n");
                    File.AppendAllText(this.LogFile, tabbed + ex.Message + "\r\n");
                    parent = ex.InnerException;
                    level++;
                    tabbed = tabbed + "\t";
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
                string caption2 = string.Format("{0} :  {1:dd/MM/yyyy HH:mm:ss.fff}", caption, DateTime.Now);
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
                string caption2 = string.Format("{0} :  {1:dd/MM/yyyy HH:mm:ss.fff}", caption, DateTime.Now);
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
                FileInfo fi = new FileInfo("Sales.Log");
                FileStream f = null;
                if (!fi.Exists)
                {
                    f = fi.Create();
                    f.Close();
                }
                if (fi.Length > 100000000)
                {
                    string newFilName = "Sales_" + DateTime.Now.ToString("yyyy_MM_dd_HHmmss") + ".log";
                    File.Copy("Sales.Log", newFilName);
                    File.WriteAllText(fi.Name, "");
                }
                string caption2 = string.Format("{0} :  {1:dd/MM/yyyy HH:mm:ss.fff}", caption, DateTime.Now);
                File.AppendAllText("Sales.Log", "=====================" + caption2 + "=====================\r\n");

                string logData = string.Format("Nozzle Id : {0}", sale.NozzleId);
                logData = logData + "\r\n" + string.Format("Nozzle Id : {0}", sale.NozzleId);
                logData = logData + "\r\n" + string.Format("SalesId : {0}", sale.SalesId);
                logData = logData + "\r\n" + string.Format("FuelType : {0}", sale.FuelTypeDescription);
                logData = logData + "\r\n" + string.Format("TotalizerStart : {0}", sale.TotalizerStart);
                logData = logData + "\r\n" + string.Format("TotalizerEnd : {0}", sale.TotalizerEnd);
                logData = logData + "\r\n" + string.Format("TotalVolume : {0}", sale.TotalVolume);
                logData = logData + "\r\n" + string.Format("TotalPrice : {0}", sale.TotalPrice);
                logData = logData + "\r\n" + string.Format("UnitPrice : {0}", sale.UnitPrice);
                logData = logData + "\r\n" + string.Format("-----------------------------------------------------------");
                if (sale.TankData != null)
                {
                    foreach (Common.Sales.TankSaleData tankData in sale.TankData)
                    {
                        logData = logData + "\r\n" + string.Format("StartLevel : {0}, EndLevel : {1}, TankId : {2}", tankData.StartLevel, tankData.EndLevel, tankData.TankId);
                    }
                }
                logData = logData + "\r\n" + string.Format("-----------------------------------------------------------");
                logData = logData + "\r\n";
                File.AppendAllText("Sales.Log", logData);
                File.AppendAllText("Sales.Log", "------------------------------------------------\r\n");
                File.AppendAllText("Sales.Log", "\r\n");
            }
            catch(Exception ex)
            {
                if (System.IO.File.Exists("Sales.Log"))
                {
                    System.Threading.Thread.Sleep(500);
                    this.LogToFile(caption, sale);
                }
                else
                {
                    LogToFile("LogToFil::Sale", ex);
                }
            }
        }

        public void LogToFile(string caption, Common.FuelPointValues values)
        {
            if (this.LogLevel < 3)
                return;
            if (this.LogLevel < 3)
                return;
            if (values == null)
                return;
            try
            {
                FileInfo fi = new FileInfo("Values.Log");
                FileStream f = null;
                if (!fi.Exists)
                {
                    f = fi.Create();
                    f.Close();
                }
                if (fi.Length > 100000000)
                {
                    string newFilName = "Values_" + DateTime.Now.ToString("yyyy_MM_dd_HHmmss") + ".log";
                    File.Copy("Values.Log", newFilName);
                    File.WriteAllText(fi.Name, "");
                }
                string caption2 = string.Format("{0} :  {1:dd/MM/yyyy HH:mm:ss.fff}", caption, DateTime.Now);
                File.AppendAllText("Values.Log", "=====================" + caption2 + "=====================\r\n");

                string logData = string.Format("Status : {0}", values.Status);
                logData = logData + "\r\n" + string.Format("CurrentVolume : {0}", values.CurrentVolume);
                logData = logData + "\r\n" + string.Format("CurrentUnitPrice : {0}", values.CurrentSalePrice);
                logData = logData + "\r\n" + string.Format("CurrentPrice : {0}", values.CurrentPriceTotal);
                logData = logData + "\r\n" + string.Format("Has Sale : {0}", values.Sale != null);
                logData = logData + "\r\n" + string.Format("-----------------------------------------------------------");
                logData = logData + "\r\n";
                File.AppendAllText("Values.Log", logData);
                File.AppendAllText("Values.Log", "------------------------------------------------\r\n");
                File.AppendAllText("Values.Log", "\r\n");
                File.AppendAllText("Values.Log", "\r\n");
            }
            catch (Exception ex)
            {
                if (System.IO.File.Exists("Values.Log"))
                {
                    System.Threading.Thread.Sleep(500);
                    this.LogToFile(caption, values);
                }
                else
                {
                    LogToFile("LogToFil::Sale", ex);
                }
            }
        }


        DateTime dtLastLog = DateTime.Now;
        public void LogToGivenFile(string fileName, string caption)
        {
            try
            {
                string diff = DateTime.Now.Subtract(dtLastLog).TotalMilliseconds.ToString();
                FileInfo fi = new FileInfo(fileName);
                FileStream f = null;
                if (!fi.Exists)
                {
                    return;
                }

                string str = string.Format("{0:dd/MM/yyyy HH:mm:ss.fff} : {1} TimeDifference(ms) : {2}\r\n", caption, DateTime.Now, diff);
                File.AppendAllText(fi.FullName, str);
                dtLastLog = DateTime.Now;
            }
            catch
            {
                return;
            }
        }
    }
}

