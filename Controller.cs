using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASFuelControl.Common;
using ASFuelControl.Common.Enumerators;
using System.Diagnostics;

namespace ASFuelControl.Gilbarco
{
    public class Controller : Common.FuelPumpControllerBase
    {
        public Controller()
        {
            this.ControllerType = Common.Enumerators.ControllerTypeEnum.Gilbarco;
            this.Controller = new Gilbarco();
        }
    }

    public class Commands
    {
        public static byte[] Halt(int FuelPointAddress)
        {
            byte[] cmd = new byte[] { BitConverter.GetBytes(48 + FuelPointAddress)[0] };
            return cmd;
        }
        public static byte[] GetStatus(int FuelPointAddress)
        {
            byte[] cmd = new byte[] { BitConverter.GetBytes(FuelPointAddress)[0] };
            return cmd;
        }
        public static byte[] SetPrice(int CurrentNozzle, int UnitPrice)
        {
            string price = UnitPrice.ToString();
            int NozzleID = (223 + CurrentNozzle);
            if (price.Length == 1) { price = "E0E0E0E" + price.Substring(0, 1); }
            if (price.Length == 2) { price = "E0E0E" + price.Substring(0, 1) + "E" + price.Substring(1, 1); }
            if (price.Length == 3) { price = "E0E" + price.Substring(0, 1) + "E" + price.Substring(1, 1) + "E" + price.Substring(2, 1); }
            if (price.Length == 4) { price = "E" + price.Substring(0, 1) + "E" + price.Substring(1, 1) + "E" + price.Substring(2, 1) + "E" + price.Substring(3, 1); }
            byte[] pricearray = StringToByteArray(price);
            byte[] Command = new byte[] { 0xFF, 0xE5, 0xF4, 0xF6, (byte)NozzleID, 0xF7, pricearray[3], pricearray[2], pricearray[1], pricearray[0], 0xFB };
            int LRC = GilbarcoLRC(Command);
            byte[] buffer = new byte[] { 0xFF, 0xE5, 0xF4, 0xF6, (byte)NozzleID, 0xF7, pricearray[3], pricearray[2], pricearray[1], pricearray[0], 0xFB, (byte)LRC, 0xF0 };
            
            return buffer;

        }
        public static byte[] GetTotals(int FuelPointAddress)
        {
            byte[] cmd = new byte[] { BitConverter.GetBytes(80 + FuelPointAddress)[0] };
            return cmd;
        }
        public static byte[] AuthorizeFuelPoint(int FuelPointAddress)
        {
            byte[] cmd = new byte[] { BitConverter.GetBytes(16 + FuelPointAddress)[0] };
            return cmd;
        }
        public static byte[] GetTransaction(int FuelPointAddress)
        {            
            byte startByte = BitConverter.GetBytes(64 + FuelPointAddress)[0];
            byte[] cmd = new byte[] { startByte };
            return cmd;
        }
        public static byte[] GetDisplay(int FuelPointAddress)
        {
            byte startByte = BitConverter.GetBytes(96 + FuelPointAddress)[0];
            byte[] cmd = new byte[] { startByte };
            return cmd;
        }
        public static byte[] ListenMode(int FuelPointAddress)
        {
            byte startByte = BitConverter.GetBytes(32 + FuelPointAddress)[0];
            byte[] cmd = new byte[] { startByte };
            return cmd;
        }

        #region ToolsCommands
        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }
        public static int GilbarcoLRC(byte[] bytearray)
        {
            int i;
            int LRC;
            int bLRC = 0;
            for (i = 0; i < (int)bytearray.Length; i++)
            {
                bLRC = (bLRC + bytearray[i]) & 15;
            }
            LRC = ((bLRC ^ 15) + 1) & 15;
            bLRC = LRC + 224;
            return bLRC;
        }
        #endregion 
    }
    public class Gilbarco : Common.IFuelProtocol
    {
        public event EventHandler<Common.FuelPointValuesArgs> DataChanged;
        public event EventHandler<Common.TotalsEventArgs> TotalsRecieved;
        public event EventHandler<Common.FuelPointValuesArgs> DispenserStatusChanged;
        public event EventHandler DispenserOffline;
        public Common.DebugValues DebugStatusDialog(Common.FuelPoint fp) { throw new NotImplementedException(); }
        private double speed
        {
            set;
            get;
        }
        private List<Common.FuelPoint> fuelPoints = new List<Common.FuelPoint>();
        private System.IO.Ports.SerialPort serialPort = new System.IO.Ports.SerialPort();
        private System.Threading.Thread th;

        public Common.FuelPoint[] FuelPoints
        {
            get
            {
                return this.fuelPoints.ToArray();
            }
            set
            {
                this.fuelPoints = new List<Common.FuelPoint>(value);
            }
        }

        public bool IsConnected
        {
            get
            {
                return this.serialPort.IsOpen;
            }
        }

        public string CommunicationPort
        {
            set;
            get;
        }

        public void Connect()
        {
            try
            {
                this.serialPort.PortName = this.CommunicationPort;
                this.serialPort.BaudRate = 5787;
                this.serialPort.Parity = System.IO.Ports.Parity.Even;
                this.serialPort.StopBits = System.IO.Ports.StopBits.One;
                this.serialPort.DataBits = 8;
                this.serialPort.Open();
                this.th = new System.Threading.Thread(new System.Threading.ThreadStart(this.WorkFlow));
                th.Start();
            }
            catch
            {
            }
        }

        public void Disconnect()
        {
            if (this.serialPort.IsOpen)
                this.serialPort.Close();
        }

        public void AddFuelPoint(Common.FuelPoint fp)
        {
            this.fuelPoints.Add(fp);
        }

        public void ClearFuelPoints()
        {
            this.fuelPoints.Clear();
        }
        private void WorkFlow()
        {
            #region Running::Initialize
            foreach (Common.FuelPoint fp in this.fuelPoints)
            {
                fp.QueryHalt = this.Halt(fp);
            }
            #endregion

            #region Running::GetFirstTotals
            foreach (Common.FuelPoint fp in this.fuelPoints)
            {
                fp.Nozzles[0].QueryTotals = true;
            }
            #endregion
         

            while (this.IsConnected)
            {
                try
                {
                    foreach (Common.FuelPoint fp in this.fuelPoints)
                    {
                        try
                        {
                            #region Halt
                            if (fp.QueryHalt)
                            {
                                fp.QueryHalt = !this.Halt(fp);
                                continue;
                            }
                            #endregion

                            #region Totals

                            int nozzleForTotals = fp.Nozzles.Where(n => n.QueryTotals).Count();

                            if (nozzleForTotals > 0)
                            {
                                foreach (Common.Nozzle nz in fp.Nozzles)
                                {
                                    if (nz.QueryTotals)
                                    {
                                        if (this.GetTotals(nz))
                                        {
                                            fp.Initialized = true;
                                            if (this.TotalsRecieved != null)
                                            {
                                                this.TotalsRecieved(this, new Common.TotalsEventArgs(fp, nz.Index, nz.TotalVolume, nz.TotalPrice));
                                            }
                                        }
                                    }
                                }
                                continue;
                            }
                            #endregion

                            #region SetPrice
                            if (fp.QuerySetPrice)
                            {

                                System.Threading.Thread.Sleep(10);
                                foreach (Common.Nozzle nz in fp.Nozzles)
                                {


                                    if (this.SetPrice(nz))
                                        nz.QuerySetPrice = false;
                                    System.Threading.Thread.Sleep(50);
                                }

                                if (fp.Nozzles.Where(n => n.QuerySetPrice).Count() == 0)
                                    fp.QuerySetPrice = false;
                                continue;

                            }
                            #endregion

                            #region Authorise
                            if (fp.QueryAuthorize)
                            {
                                if (this.AuthorizeFuelPoint(fp))
                                    fp.QueryAuthorize = false;
                                continue;
                            }
                            #endregion

                            Common.Enumerators.FuelPointStatusEnum oldStatus = fp.Status;

                            this.GetStatus(fp);


                            if (oldStatus != fp.Status && this.DispenserStatusChanged != null)
                            {

                                Common.FuelPointValues values = new Common.FuelPointValues();
                                int currentNozzle = (int)fp.GetExtendedProperty("CurrentNozzle", -1);
                                if (fp.Status != Common.Enumerators.FuelPointStatusEnum.Idle && fp.Status != Common.Enumerators.FuelPointStatusEnum.Offline)
                                {
                                    fp.ActiveNozzleIndex = (int)fp.GetExtendedProperty("CurrentNozzle");
                                    values.ActiveNozzle = currentNozzle;
                                }
                                else
                                {
                                    fp.ActiveNozzleIndex = -1;
                                    values.ActiveNozzle = -1;
                                }
                                values.Status = fp.Status;
                                this.DispenserStatusChanged(this, new Common.FuelPointValuesArgs()
                                {
                                    CurrentFuelPoint = fp,
                                    CurrentNozzleId = values.ActiveNozzle + 1,
                                    Values = values
                                });
                            }
                            if (fp.Status == Common.Enumerators.FuelPointStatusEnum.Work)
                            {
                                fp.SetExtendedProperty("iNeedDisplay", true);
                            }
                        }
                        finally
                        {
                            System.Threading.Thread.Sleep(110);
                        }
                    }
                }
                catch (Exception error_workflow)
                {
                    if (System.IO.File.Exists(LogPath()))
                    {
                        System.IO.File.AppendAllText(LogPath(), DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + "Thread Exception:  " + error_workflow.ToString() + "\r\n");
                        
                    }
                }
            }
        }

        #region Method::Totals
        public bool GetTotals(Common.Nozzle nz)
        {
            int waiting = 0;
            byte[] cmd = Commands.GetTotals(nz.ParentFuelPoint.Address);
            this.serialPort.Write(cmd, 0, cmd.Length);
            if (System.IO.File.Exists(LogPath()))
            {
                this.LogAdd("TX GetTotals", cmd, null);
            }
            if (nz.ParentFuelPoint.Channel == 100)
            {
                while (this.serialPort.BytesToRead < 185 && waiting < 300)
                {
                    System.Threading.Thread.Sleep(50);
                    waiting += 20;
                }
            }
            else
            {
                while (this.serialPort.BytesToRead < 96 && waiting < 300)
                {
                    System.Threading.Thread.Sleep(80);
                    waiting += 20;
                }
            }
            byte[] responsePoll = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(responsePoll, 0, this.serialPort.BytesToRead);
            if (System.IO.File.Exists(LogPath()))
            {
                this.LogAdd("RX GetTotals", responsePoll, null);
            }
            Trace.WriteLine(BitConverter.ToString(responsePoll));
            return EvalGetTotals(nz, responsePoll);
        }
        private bool EvalGetTotals(Common.Nozzle nozzle, byte[] response)
        {
            try
            {
                int id = response[0] - 80;
                if (id == nozzle.ParentFuelPoint.Address)
                {
                    byte[] volumeBuffer = response.Skip(5 + (30 * (nozzle.Index - 1))).Take(8).ToArray();
                    volumeBuffer = volumeBuffer.Reverse().ToArray();
                    string volumeBufferStr = BitConverter.ToString(volumeBuffer, 0, volumeBuffer.Length).Replace("-", "").Replace("E", "");

                    byte[] priceBuffer = response.Skip(14 + (30 * (nozzle.Index - 1))).Take(8).ToArray();
                    priceBuffer = priceBuffer.Reverse().ToArray();
                    string priceBufferStr = BitConverter.ToString(priceBuffer, 0, priceBuffer.Length).Replace("-", "").Replace("E", "");

                    decimal totalVolume = 0;
                    try
                    {
                        totalVolume = decimal.Parse(volumeBufferStr);
                        decimal diff = (totalVolume - nozzle.TotalVolume) - (int)((nozzle.ParentFuelPoint.DispensedVolume) * (decimal)Math.Pow(10, nozzle.ParentFuelPoint.TotalDecimalPlaces));

                        if (Math.Abs(totalVolume - nozzle.TotalVolume) > 0 && Math.Abs(diff) > 100 && nozzle.TotalVolume >= 0)
                        {
                            int totalMishMatchIndex = 0;
                            if (nozzle.GetExtendedProperty("TotalMischMatchIndex") != null)
                                totalMishMatchIndex = (int)nozzle.GetExtendedProperty("TotalMischMatchIndex");
                            if (totalMishMatchIndex < 5)
                            {
                                totalMishMatchIndex = totalMishMatchIndex + 1;
                                nozzle.SetExtendedProperty("TotalMischMatchIndex", totalMishMatchIndex);
                              
                                return false;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (System.IO.File.Exists("Gilbarco.log"))
                            System.IO.File.AppendAllText("Gilbarco.log", "-----------------------------Internal Exceptiopn Error " + ex.Message + "\r\n");
                        return false;
                    }
                    nozzle.SetExtendedProperty("TotalMischMatchIndex", 0);
                    nozzle.TotalVolume = totalVolume;
                    nozzle.TotalPrice = decimal.Parse(priceBufferStr);
                    //1  2  3  4  5  6  7  8  9  10 11 12 13 14 15 16 17 18 19 20 21 22 23 24 25 26 27 28                   
                    //51-FF-F6-E0-F9-E7-E5-E2-E1-E5-E1-E2-E0-FA-E3-E8-E0-E4-E3-E4-E2-E0-F4-E0-E4-E4-E1-F5-E0-E0-E0-E0-F6-E1-F9-E0-E0-E0-E0-E0-E0-E0-E0-FA-E0-E0-E0-E0-E0-E0-E0-E0-F4-E0-E0-E0-E0-F5-E0-E0-E0-E0-F6-E2-F9-E0-E0-E0-E0-E0-E0-E0-E0-FA-E0-E0-E0-E0-E0-E0-E0-E0-F4-E0-E0-E0-E0-F5-E0-E0-E0-E0-FB-E5-F0
                    return true;
                }
                else return false;
            }
            catch (Exception e)
            {
                if (System.IO.File.Exists(LogPath()))
                {
                    this.LogAdd("Exception Thread EvalTotals", null, e.ToString());
                }
                return false;
            }

        }
        #endregion

        #region Method::Authorise
        public bool AuthorizeFuelPoint(Common.FuelPoint fp)
        {

            byte[] cmd = Commands.AuthorizeFuelPoint(fp.Address);
            this.serialPort.Write(cmd, 0, cmd.Length);
            if (System.IO.File.Exists(LogPath()))
            {
                this.LogAdd("TX Authorise", cmd, null);
            }
            System.Threading.Thread.Sleep(25);

            byte[] trash = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(trash, 0, trash.Length);
            if (System.IO.File.Exists(LogPath()))
            {
                this.LogAdd("RX Authorise", trash, null);
            }
            return true;
        }
        #endregion

        #region Method::Status
        int extendedProperty;
        public void GetStatus(Common.FuelPoint fp)
        {
            int waiting = 0;
            byte[] cmd = Commands.GetStatus(fp.Address);
            this.serialPort.Write(cmd, 0, cmd.Length);
            if (System.IO.File.Exists(LogPath()))
            {
                this.LogAdd("TX Status", cmd, null);
            }
            while (this.serialPort.BytesToRead < 2 && waiting < 300)
            {
                System.Threading.Thread.Sleep(30);
                waiting += 20;
            }       
            byte[] responsePoll = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(responsePoll, 0, this.serialPort.BytesToRead);
            if (System.IO.File.Exists(LogPath()))
            {
                this.LogAdd("RX Status", responsePoll, null);
            }
            this.EvalStatus(fp, responsePoll);
        }        
        private void EvalStatus(Common.FuelPoint fp, byte[] response)
        {
            FuelPointValues fuelPointValue = new FuelPointValues();
            FuelPointStatusEnum oldStatus = fp.Status;
            FuelPointStatusEnum newStatus = fp.Status;
            int currentNozzle = (int)fp.GetExtendedProperty("CurrentNozzle", -1);
          
            if(fp.Status == FuelPointStatusEnum.Offline && response.Length > 1)
            {
               newStatus = FuelPointStatusEnum.Idle;
            }
            byte b1 = response[1];

            if (b1 >= 0x61 && b1 <= 0x6F && fp.Status != FuelPointStatusEnum.Work)
            {
                newStatus = Common.Enumerators.FuelPointStatusEnum.Idle;
            }
            else if (b1 >= 0x61 && b1 <= 0x6F && fp.Status == FuelPointStatusEnum.Work)
            {
                this.GetTransaction(fp.ActiveNozzle);                
                newStatus = FuelPointStatusEnum.Idle;
                if (currentNozzle >= 0)
                    fp.Nozzles[currentNozzle].QueryTotals = true;
            }
            else if (b1 >= 0xB1 && b1 <= 0xBF && fp.Status == FuelPointStatusEnum.Work)
            {                
                this.GetTransaction(fp.ActiveNozzle);                
                newStatus = FuelPointStatusEnum.Idle;
                if (currentNozzle >= 0)
                    fp.Nozzles[currentNozzle].QueryTotals = true;
            }
            else if (b1 >= 0xA1 && b1 <= 0xAF && fp.Status == FuelPointStatusEnum.Work)
            {
                this.GetTransaction(fp.ActiveNozzle);
                newStatus = FuelPointStatusEnum.Idle;
                if (currentNozzle >= 0)
                    fp.Nozzles[currentNozzle].QueryTotals = true;
            }
            else if (b1 >= 0x71 && b1 <= 0x7F && fp.Status != FuelPointStatusEnum.Work)
            {
                byte[] buf = NozzleAddress(fp);
                if (buf.Length == 28)
                {
                    if (buf[23] == 0xB0) { currentNozzle = -1; }
                    else if (buf[23] == 0xB1) { currentNozzle = 0; }
                    else if (buf[23] == 0xB2) { currentNozzle = 1; }
                    else if (buf[23] == 0xB3) { currentNozzle = 2; }
                    else { currentNozzle = -1; }
                    fp.SetExtendedProperty("CurrentNozzle", currentNozzle);
                    newStatus = FuelPointStatusEnum.Nozzle;
                }                
            }
            else if (b1 >= 0x81 && b1 <= 0x8F)
            {
                newStatus = Common.Enumerators.FuelPointStatusEnum.Ready;
            }
            else if (b1 >= 0x91 && b1 <= 0x9F)
            {
                newStatus = Common.Enumerators.FuelPointStatusEnum.Work;
            }
            else
            {
                newStatus = fp.Status;
            }

            if (fp.Status == FuelPointStatusEnum.Work)
            {
                GetDisplay(fp.ActiveNozzle);
            }

            fp.Status = newStatus;
            fp.DispenserStatus = fp.Status;

        }
        #endregion

        #region Method::GetDisplay
        public void GetDisplay(Common.Nozzle nozzle)
        {            
            //Response 61 E0 E0 E0 E0 E0 E0
            int waiting=0;
            byte[] cmd = Commands.GetDisplay(nozzle.ParentFuelPoint.Address);
            this.serialPort.Write(cmd, 0, cmd.Length);
            if (System.IO.File.Exists(LogPath()))
            {
                this.LogAdd("TX GetDisplay", cmd, null);
            }
            while (this.serialPort.BytesToRead < 7 && waiting < 300)
            {
                System.Threading.Thread.Sleep(20);
                waiting += 20;
            }

            byte[] response = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(response, 0, response.Length);
            if (System.IO.File.Exists(LogPath()))
            {
                this.LogAdd("RX GetDisplay", response, null);
            }
            EvalGetDisplay(nozzle, response);
        }
        private void EvalGetDisplay(Common.Nozzle nozzle, byte[] buffer)
        {            
            try
            {
                if (buffer.Length == 7)
                {
                    var q = buffer.Skip(1).Take(6).Reverse<byte>();
                    string amountString = BitConverter.ToString(q.ToArray()).Replace("-", "").Replace("E", "");
                    nozzle.ParentFuelPoint.DispensedAmount = decimal.Parse(amountString) / (decimal)Math.Pow(10, nozzle.ParentFuelPoint.AmountDecimalPlaces);
                    nozzle.ParentFuelPoint.DispensedVolume = decimal.Round(nozzle.ParentFuelPoint.DispensedAmount / nozzle.UnitPrice, nozzle.ParentFuelPoint.AmountDecimalPlaces);


                    if (this.DataChanged != null)
                    {
                        Common.FuelPointValues values = new Common.FuelPointValues();
                        values.CurrentSalePrice = nozzle.UnitPrice;
                        values.CurrentPriceTotal = nozzle.ParentFuelPoint.DispensedAmount;
                        values.CurrentVolume = nozzle.ParentFuelPoint.DispensedVolume;

                        this.DataChanged(this, new Common.FuelPointValuesArgs()
                        {
                            CurrentFuelPoint = nozzle.ParentFuelPoint,
                            CurrentNozzleId = 1,
                            Values = values
                        });

                    }
                }
            }
            catch(Exception ex)
            {
                if (System.IO.File.Exists(LogPath()))
                {
                    this.LogAdd("Exception Thread EvalTransaction", null, ex.ToString());
                }
            }
        }
        #endregion

        #region Method::GetTransaction
        public void GetTransaction(Common.Nozzle nozzle)
        {
            int waiting = 0;
            byte[] cmd = Commands.GetTransaction(nozzle.ParentFuelPoint.Address);
            this.serialPort.Write(cmd, 0, cmd.Length);
            if (System.IO.File.Exists(LogPath()))
            {
                this.LogAdd("TX GetTransaction", cmd, null);
            }
            while (this.serialPort.BytesToRead < 34 && waiting < 300)
            {
                System.Threading.Thread.Sleep(25);
                waiting += 20;
            }

            byte[] response = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(response, 0, response.Length);
            if (System.IO.File.Exists(LogPath()))
            {
                this.LogAdd("RX GetTransaction", response, null);
            }
            EvalTransaction(nozzle, response);
        }
        private void EvalTransaction(Common.Nozzle nozzle, byte[] buffer)
        {

            if (buffer.Length < 34)
                return;
            try
            {
                byte[] unitPrice = buffer.Skip(13).Take(4).ToArray();
                unitPrice = unitPrice.Reverse().ToArray();
                byte[] priceBuffer = buffer.Skip(25).Take(6).ToArray();
                priceBuffer = priceBuffer.Reverse().ToArray();
                byte[] volumeBuffer = buffer.Skip(18).Take(6).ToArray();
                volumeBuffer = volumeBuffer.Reverse().ToArray();
                string upString = BitConverter.ToString(unitPrice).Replace("-", "").Replace("E", "");
                string volString = BitConverter.ToString(volumeBuffer).Replace("-", "").Replace("E", ""); 
                string priceString = BitConverter.ToString(priceBuffer).Replace("-", "").Replace("E", "");

                if (nozzle.ParentFuelPoint.Channel == 10)
                {
                    //GBR Settings
                    //VolumeDecimalPlaces = 3
                    //AmountDecimalPlaces = 3
                    nozzle.UnitPrice = decimal.Parse(upString) / (decimal)Math.Pow(10, nozzle.ParentFuelPoint.UnitPriceDecimalPlaces);
                    nozzle.UntiPriceInt = int.Parse(upString);
                    nozzle.ParentFuelPoint.DispensedVolume = decimal.Parse(volString) / (decimal)Math.Pow(10, 3);
                    nozzle.ParentFuelPoint.DispensedAmount = decimal.Parse(priceString) / (decimal)Math.Pow(10, 3);
                }
                else if (nozzle.ParentFuelPoint.Channel == 20)
                {
                    //GBR Settings
                    //VolumeDecimalPlaces = 3
                    //AmountDecimalPlaces = 2
                    nozzle.UnitPrice = decimal.Parse(upString) / (decimal)Math.Pow(10, nozzle.ParentFuelPoint.UnitPriceDecimalPlaces);
                    nozzle.UntiPriceInt = int.Parse(upString);
                    nozzle.ParentFuelPoint.DispensedVolume = decimal.Parse(volString) / (decimal)Math.Pow(10, 3);
                    nozzle.ParentFuelPoint.DispensedAmount = decimal.Parse(priceString) / (decimal)Math.Pow(10, 2);
                }
                else if (nozzle.ParentFuelPoint.Channel == 30)
                {
                    //GBR Settings
                    //VolumeDecimalPlaces = 2
                    //AmountDecimalPlaces = 1
                    nozzle.UnitPrice = decimal.Parse(upString) / (decimal)Math.Pow(10, nozzle.ParentFuelPoint.UnitPriceDecimalPlaces);
                    nozzle.UntiPriceInt = int.Parse(upString);
                    nozzle.ParentFuelPoint.DispensedVolume = decimal.Parse(volString) / (decimal)Math.Pow(10, 2);
                    nozzle.ParentFuelPoint.DispensedAmount = decimal.Parse(priceString) / (decimal)Math.Pow(10, 1);
                }
                else
                {
                    nozzle.UnitPrice = decimal.Parse(upString) / (decimal)Math.Pow(10, nozzle.ParentFuelPoint.UnitPriceDecimalPlaces);
                    nozzle.UntiPriceInt = int.Parse(upString);
                    nozzle.ParentFuelPoint.DispensedVolume = decimal.Parse(volString) / (decimal)Math.Pow(10, nozzle.ParentFuelPoint.VolumeDecimalPlaces);
                    nozzle.ParentFuelPoint.DispensedAmount = decimal.Parse(priceString) / (decimal)Math.Pow(10, nozzle.ParentFuelPoint.AmountDecimalPlaces);
                }

                decimal amountDiff = nozzle.ParentFuelPoint.DispensedVolume * nozzle.UnitPrice - nozzle.ParentFuelPoint.DispensedAmount;
                if (amountDiff > (decimal)0.10)
                {
                    nozzle.ParentFuelPoint.DispensedAmount = decimal.Round(nozzle.ParentFuelPoint.DispensedVolume * nozzle.UnitPrice, nozzle.ParentFuelPoint.AmountDecimalPlaces);
                }               
                if (this.DataChanged != null)
                {
                    Common.FuelPointValues values = new Common.FuelPointValues();
                    values.CurrentSalePrice = nozzle.UnitPrice;
                    values.CurrentPriceTotal = nozzle.ParentFuelPoint.DispensedAmount;
                    values.CurrentVolume = nozzle.ParentFuelPoint.DispensedVolume;

                    this.DataChanged(this, new Common.FuelPointValuesArgs()
                    {
                        CurrentFuelPoint = nozzle.ParentFuelPoint,
                        CurrentNozzleId = 1,
                        Values = values
                    });

                }
            }
            catch(Exception ex)
            {
                if (System.IO.File.Exists(LogPath()))
                {
                    this.LogAdd("Exception Thread EvalTransaction", null, ex.ToString());
                }
            }
        }
        #endregion

        #region Method::SetPrice
        public bool SetPrice(Common.Nozzle nozzle)
        {
            try
            {
                int waiting = 0;
                byte[] Litsen = Commands.ListenMode(nozzle.ParentFuelPoint.Address);
                this.serialPort.Write(Litsen, 0, Litsen.Length);
              
                if (System.IO.File.Exists(LogPath()))
                {
                    this.LogAdd("TX ListenMode", Litsen, null);
                }
                System.Threading.Thread.Sleep(30);
                byte[] responsePoll = new byte[this.serialPort.BytesToRead];
                this.serialPort.Read(responsePoll, 0, this.serialPort.BytesToRead);
                if (System.IO.File.Exists(LogPath()))
                {
                    this.LogAdd("RX ListenMode", responsePoll, null);
                }
                byte b1 = responsePoll[1];
                if (b1 >= 0xD1 && b1 <= 0xDF)
                {
                    System.Threading.Thread.Sleep(10);
                    byte[] cmd = Commands.SetPrice(nozzle.Index, nozzle.UntiPriceInt);
                    this.serialPort.Write(cmd, 0, cmd.Length);
                    Trace.WriteLine(BitConverter.ToString(cmd));

                    if (System.IO.File.Exists(LogPath()))
                    {
                        this.LogAdd("TX SetPrice", cmd, null);
                    }
                    System.Threading.Thread.Sleep(50);
                    byte[] dd = new byte[this.serialPort.BytesToRead];
                    this.serialPort.Read(dd, 0, this.serialPort.BytesToRead);
                    if (System.IO.File.Exists(LogPath()))
                    {
                        this.LogAdd("RX SetPrice", dd, null);
                    }
                    return true;
                }
                else
                    return false;
                
            }
            catch (Exception e)
            {
                if (System.IO.File.Exists(LogPath()))
                {
                    this.LogAdd("Exception Thread SetPrice", null, e.ToString());
                }
                return false;
            }
        }
        #endregion

        #region Method::Halt
        private bool Halt(Common.FuelPoint fp)
        {
            byte[] buf = Commands.Halt(fp.Address);
            this.serialPort.Write(buf, 0, (int)buf.Length);
            if (System.IO.File.Exists(LogPath()))
            {
                this.LogAdd("TX Halt", buf, null);
            }
            System.Threading.Thread.Sleep(50);
            byte[] response = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(response, 0, this.serialPort.BytesToRead);
            if (System.IO.File.Exists(LogPath()))
            {
                this.LogAdd("RX Halt", response, null);
            }
            System.Threading.Thread.Sleep(50);
            
            byte[] buf2 = Commands.Halt(fp.Address);
            this.serialPort.Write(buf2, 0, (int)buf2.Length);
            if (System.IO.File.Exists(LogPath()))
            {
                this.LogAdd("TX Halt", buf2, null);
            }
            System.Threading.Thread.Sleep(50);
            byte[] response2 = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(response2, 0, this.serialPort.BytesToRead);
            if (System.IO.File.Exists(LogPath()))
            {
                this.LogAdd("RX Halt", response2, null);
            }
            return true;
        }
        #endregion

        #region Method::GetNozzle

        private byte[] CMDGetNozzle()
        {
            byte[] CMD = new byte[] { 0xFF, 0xE9, 0xFE, 0xE0, 0xE1, 0xE0, 0xFB, 0xEE, 0xF0 };
            return CMD;
        }
        public byte[] NozzleAddress(Common.FuelPoint fp)
        {
            try
            {
                int waiting = 0;
                byte[] Litsen = Commands.ListenMode(fp.Address);
                this.serialPort.Write(Litsen, 0, Litsen.Length);
                if (System.IO.File.Exists(LogPath()))
                {
                    this.LogAdd("TX ListenMode", Litsen, null);
                }
                System.Threading.Thread.Sleep(30);


                byte[] responsePoll = new byte[this.serialPort.BytesToRead];
                this.serialPort.Read(responsePoll, 0, this.serialPort.BytesToRead);
                if (System.IO.File.Exists(LogPath()))
                {
                    this.LogAdd("RX SetPrice", responsePoll, null);
                }
                byte b1 = responsePoll[1];
                if (b1 >= 0xD1 && b1 <= 0xDF)
                {
                    System.Threading.Thread.Sleep(10);
                    byte[] cmdnoz = CMDGetNozzle();
                    this.serialPort.Write(cmdnoz, 0, cmdnoz.Length);
                    if (System.IO.File.Exists(LogPath()))
                    {
                        this.LogAdd("TX GetNozzle", cmdnoz, null);
                    }
                    while (this.serialPort.BytesToRead < 28 && waiting <300)
                    {
                        waiting += 10;
                        System.Threading.Thread.Sleep(20);
                    }
                    byte[] responseNozzle = new byte[this.serialPort.BytesToRead];
                    this.serialPort.Read(responseNozzle, 0, responseNozzle.Length);
                    if (System.IO.File.Exists(LogPath()))
                    {
                        this.LogAdd("RX GetNozzle", responseNozzle, null);
                    }
                    return responseNozzle;
                }
                else
                    return null;

            }
            catch (Exception e)
            {
                return null;
            }
        }
      

        #endregion

        #region Tools
        public string LogPath()
        {
            string LogPath = "Gilbarco_" + this.CommunicationPort + ".log";
            return LogPath;
        }
        public void LogAdd(string Method, byte[] DataBuffer, string Exception)
        {
            if (Exception != null)
            {
                System.IO.File.AppendAllText(LogPath(), DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " " + Method + "\t" + Exception + "\r\n");
            }
            else
            {
                System.IO.File.AppendAllText(LogPath(), DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " " + Method + "\t" + BitConverter.ToString(DataBuffer) + "\r\n");
            }
        }


        #endregion
    }
}
