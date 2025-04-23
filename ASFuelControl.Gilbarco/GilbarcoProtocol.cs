using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASFuelControl.Common;
using ASFuelControl.Common.Enumerators;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace ASFuelControl.Gilbarco
{
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

        public static byte[] PresetAmount(int fpAddress, int amount)
        {
            //FF-E5-F2-F4-F6-E0-F8-E0-E0-E1-E0-E0-E0-E[LRC]-F0
            string amountstr = amount.ToString();
            if (amountstr.Length == 1) { amountstr = "E0E0E0E0E" + amountstr.Substring(0, 1); }
            if (amountstr.Length == 2) { amountstr = "E0E0E0E" + amountstr.Substring(0, 1) + "E" + amountstr.Substring(1, 1); }
            if (amountstr.Length == 3) { amountstr = "E0E0E" + amountstr.Substring(0, 1) + "E" + amountstr.Substring(1, 1) + "E" + amountstr.Substring(2, 1); }
            if (amountstr.Length == 4) { amountstr = "E0E" + amountstr.Substring(0, 1) + "E" + amountstr.Substring(1, 1) + "E" + amountstr.Substring(2, 1) + "E" + amountstr.Substring(3, 1); }
            if (amountstr.Length == 5) { amountstr = "E" + amountstr.Substring(0, 1) + "E" + amountstr.Substring(1, 1) + "E" + amountstr.Substring(2, 1) + "E" + amountstr.Substring(3, 1) + "E" + amountstr.Substring(4, 1); }
            //if (amountstr.Length == 6) { amountstr = "E" + amountstr.Substring(0, 1) + "E" + amountstr.Substring(1, 1) + "E" + amountstr.Substring(2, 1) + "E" + amountstr.Substring(3, 1) + "E" + amountstr.Substring(4, 1) + "E" + amountstr.Substring(5, 1); }
            byte[] aAr = StringToByteArray(amountstr);

            byte[] Command = new byte[] { 0xFF, 0xE6, 0xF2, 0xF8, aAr[4], aAr[3], aAr[2], aAr[1], aAr[0], 0xFB };
            int LRC = GilbarcoLRC(Command);
            byte[] buffer = new byte[] { 0xFF, 0xE6, 0xF2, 0xF8, aAr[4], aAr[3], aAr[2], aAr[1], aAr[0], 0xFB, (byte)LRC, 0xF0 };

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
    public class GilbarcoProtocol : Common.IFuelProtocol
    {
        public event EventHandler<Common.FuelPointValuesArgs> DataChanged;
        public event EventHandler<Common.TotalsEventArgs> TotalsRecieved;
        public event EventHandler<Common.SaleEventArgs> SaleRecieved;
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
                foreach(var nz in fp.Nozzles)
                    nz.QueryTotals = true;
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

                            byte[] statusFoo = this.GetStatusFoo(fp);

                            if (statusFoo.Length >= 2)
                            {
                                if (statusFoo[1] == 1)
                                {

                                }
                            }

                            int nozzleForTotals = fp.Nozzles.Where(n => n.QueryTotals).Count();

                            if (nozzleForTotals > 0)
                            {
                                bool initialize = true;
                                foreach (Common.Nozzle nz in fp.Nozzles)
                                {
                                    if (nz.QueryTotals)
                                    {
                                        if (this.GetTotals(nz))
                                        {
                                            if (this.TotalsRecieved != null)
                                            {
                                                this.TotalsRecieved(this, new Common.TotalsEventArgs(fp, nz.Index, nz.TotalVolume, nz.TotalPrice));
                                            }
                                        }
                                        else
                                            initialize = false;
                                    }
                                }
                                fp.Initialized = initialize;
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
                            else if (fp.Status == FuelPointStatusEnum.Idle && fp.PresetAmount > 0 && fp.PresetAmount < 1000)
                            {
                                fp.PresetRaised = this.Preset(fp);
                            }
                        }
                        finally
                        {
                            System.Threading.Thread.Sleep(200);
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
        public bool GetTotals(Nozzle nz)
        {
            byte[] totals = Commands.GetTotals(nz.ParentFuelPoint.Address);
            this.serialPort.Write(totals, 0, (int)totals.Length);
            if (File.Exists(this.LogPath()))
            {
                this.LogAdd("TX GetTotals", totals, null);
            }
            Thread.Sleep(750);
            byte[] numArray = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(numArray, 0, this.serialPort.BytesToRead);
            if (File.Exists(this.LogPath()))
            {
                this.LogAdd("RX GetTotals", numArray, null);
            }
            if ((int)numArray.Length <= 10 || numArray[0] != totals[0])
            {
                return false;
            }
            return this.EvalGetTotals(nz, numArray);
        }

        private bool EvalGetTotals(Nozzle nozzle, byte[] response)
        {
            bool flag;
            try
            {
                if (response[0] - 80 != nozzle.ParentFuelPoint.Address)
                {
                    flag = false;
                }
                else
                {
                    //nozzle.NozzleIndex
                    byte[] array = response.Skip<byte>(5 + 30 * (nozzle.Index - 1)).Take<byte>(8).ToArray<byte>();
                    array = array.Reverse<byte>().ToArray<byte>();
                    decimal num = decimal.Parse(BitConverter.ToString(array, 0, (int)array.Length).Replace("-", "").Replace("E", ""));
                    nozzle.TotalVolume = num;
                    if (File.Exists(this.LogPath()))
                    {
                        this.LogAdd("RX EvalTotals", null, string.Concat("Volume Total: ", num.ToString()));
                    }
                    Thread.Sleep(50);
                    nozzle.ParentFuelPoint.QueryTotals = false;
                    flag = true;
                }
            }
            catch (Exception exception1)
            {
                Exception exception = exception1;
                if (File.Exists(this.LogPath()))
                {
                    this.LogAdd("RX GetTotals", null, exception.ToString());
                }
                flag = false;
            }
            return flag;
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

        public byte[] GetStatusFoo(FuelPoint fp)
        {
            int num = 0;
            byte[] status = Commands.GetStatus(fp.Address);
            this.serialPort.Write(status, 0, (int)status.Length);
            if (File.Exists(this.LogPath()))
            {
                this.LogAdd("TX Status", status, null);
            }
            while (this.serialPort.BytesToRead < 2 && num < 300)
            {
                Thread.Sleep(30);
                num = num + 20;
            }
            byte[] numArray = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(numArray, 0, this.serialPort.BytesToRead);
            if (File.Exists(this.LogPath()))
            {
                this.LogAdd("RX Status", numArray, null);
            }
            if ((int)numArray.Length <= 1)
            {
                fp.Status = FuelPointStatusEnum.Offline;
                return new byte[] { };
            }
            return numArray;
        }

        private void EvalStatusFoo(Common.FuelPoint fp, byte[] response)
        {
            FuelPointValues fuelPointValue = new FuelPointValues();
            FuelPointStatusEnum oldStatus = fp.Status;
            FuelPointStatusEnum newStatus = fp.Status;
            int currentNozzle = (int)fp.GetExtendedProperty("CurrentNozzle", -1);

            
            byte b1 = response[1];

            if (b1 >= 0x61 && b1 <= 0x6F && fp.Status != FuelPointStatusEnum.Work)
            {
                newStatus = Common.Enumerators.FuelPointStatusEnum.Idle;
            }
            else if (b1 >= 0x61 && b1 <= 0x6F && fp.Status == FuelPointStatusEnum.Work)
            {
                this.GetTransaction(fp.ActiveNozzle);
                newStatus = FuelPointStatusEnum.Idle;
                //if (currentNozzle >= 0)
                //    fp.Nozzles[currentNozzle].QueryTotals = true;
            }
            else if (b1 >= 0xB1 && b1 <= 0xBF && fp.Status == FuelPointStatusEnum.Work)
            {
                this.GetTransaction(fp.ActiveNozzle);
                newStatus = FuelPointStatusEnum.Idle;
                //if (currentNozzle >= 0)
                //    fp.Nozzles[currentNozzle].QueryTotals = true;
            }
            else if (b1 >= 0xA1 && b1 <= 0xAF && fp.Status == FuelPointStatusEnum.Work)
            {
                this.GetTransaction(fp.ActiveNozzle);
                newStatus = FuelPointStatusEnum.Idle;
                //if (currentNozzle >= 0)
                //    fp.Nozzles[currentNozzle].QueryTotals = true;
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
                    else if (buf[23] == 0xB4) { currentNozzle = 3; }
                    else { currentNozzle = -1; }
                    fp.SetExtendedProperty("CurrentNozzle", currentNozzle);
                    newStatus = FuelPointStatusEnum.Nozzle;
                }
            }
            else if (b1 >= 0x81 && b1 <= 0x8F)
            {
                newStatus = Common.Enumerators.FuelPointStatusEnum.Work;
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
        }

        public void GetStatus(FuelPoint fp)
        {
            int num = 0;
            byte[] status = Commands.GetStatus(fp.Address);
            this.serialPort.Write(status, 0, (int)status.Length);
            if (File.Exists(this.LogPath()))
            {
                this.LogAdd("TX Status", status, null);
            }
            while (this.serialPort.BytesToRead < 2 && num < 300)
            {
                Thread.Sleep(30);
                num = num + 20;
            }
            byte[] numArray = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(numArray, 0, this.serialPort.BytesToRead);
            if (File.Exists(this.LogPath()))
            {
                this.LogAdd("RX Status", numArray, null);
            }
            if ((int)numArray.Length <= 1)
            {
                fp.Status = FuelPointStatusEnum.Offline;
                return;
            }
            this.EvalStatus(fp, numArray);
        }

        private void EvalStatus(Common.FuelPoint fp, byte[] response)
        {
            FuelPointValues fuelPointValue = new FuelPointValues();
            FuelPointStatusEnum oldStatus = fp.Status;
            FuelPointStatusEnum newStatus = fp.Status;
            int currentNozzle = (int)fp.GetExtendedProperty("CurrentNozzle", -1);

            if (fp.Status != FuelPointStatusEnum.Offline && response.Length <= 1)
            {
                newStatus = FuelPointStatusEnum.Offline;
                fp.Status = newStatus;
                fp.DispenserStatus = fp.Status;
                return;
            }
            if (fp.Status == FuelPointStatusEnum.Offline && response.Length > 1)
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
                //if (currentNozzle >= 0)
                //    fp.Nozzles[currentNozzle].QueryTotals = true;
            }
            else if (b1 >= 0xB1 && b1 <= 0xBF && fp.Status == FuelPointStatusEnum.Work)
            {
                this.GetTransaction(fp.ActiveNozzle);
                newStatus = FuelPointStatusEnum.Idle;
                //if (currentNozzle >= 0)
                //    fp.Nozzles[currentNozzle].QueryTotals = true;
            }
            else if (b1 >= 0xA1 && b1 <= 0xAF && fp.Status == FuelPointStatusEnum.Work)
            {
                this.GetTransaction(fp.ActiveNozzle);
                newStatus = FuelPointStatusEnum.Idle;
                //if (currentNozzle >= 0)
                //    fp.Nozzles[currentNozzle].QueryTotals = true;
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
                    else if (buf[23] == 0xB4) { currentNozzle = 3; }
                    else { currentNozzle = -1; }
                    fp.SetExtendedProperty("CurrentNozzle", currentNozzle);
                    newStatus = FuelPointStatusEnum.Nozzle;
                }
            }
            else if (b1 >= 0x81 && b1 <= 0x8F)
            {
                newStatus = Common.Enumerators.FuelPointStatusEnum.Work;
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
            int waiting = 0;
            byte[] cmd = Commands.GetDisplay(nozzle.ParentFuelPoint.Address);
            this.serialPort.Write(cmd, 0, cmd.Length);
            if (System.IO.File.Exists(LogPath()))
            {
                this.LogAdd("TX GetDisplay", cmd, null);
            }
            while (this.serialPort.BytesToRead < 7 && waiting < 300)
            {
                System.Threading.Thread.Sleep(30);
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

                        this.LogAdd("Display for Nozzle", null, string.Format("Nozzle: {0}. Volume: {1:N3}, Amount: {2:N3}, Unit Price: {3:N3}", nozzle.Index, values.CurrentVolume, values.CurrentPriceTotal, values.CurrentSalePrice));

                        this.DataChanged(this, new Common.FuelPointValuesArgs()
                        {
                            CurrentFuelPoint = nozzle.ParentFuelPoint,
                            CurrentNozzleId = nozzle.Index,
                            Values = values
                        });

                    }
                }
            }
            catch (Exception ex)
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
                System.Threading.Thread.Sleep(30);
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
                int index = buffer.ToList().IndexOf(0xf8);
                if (index < 0)
                    return;

                //byte[] pumpData = new byte[] { buffer[index + 2] };
                //string pds = BitConverter.ToString(pumpData).Replace("E", "");
                //int adddress = int.Parse(pds) + 1;
                //if (adddress != nozzle.ParentFuelPoint.Address)
                //    return;

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
                        CurrentNozzleId = nozzle.Index,
                        Values = values
                    });

                }
            }
            catch (Exception ex)
            {
                if (System.IO.File.Exists(LogPath()))
                {
                    this.LogAdd("Exception Thread EvalTransaction", null, ex.ToString());
                }
            }
        }
        #endregion

        #region Method::Preset

        public bool Preset(FuelPoint fp)
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
                System.Threading.Thread.Sleep(50);
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
                    int amount = (int)(fp.PresetAmount * (decimal)Math.Pow(10, 2));
                    byte[] cmd = Commands.PresetAmount(fp.Address, amount);
                    this.serialPort.Write(cmd, 0, cmd.Length);
                    Trace.WriteLine(BitConverter.ToString(cmd));

                    if (System.IO.File.Exists(LogPath()))
                    {
                        this.LogAdd("TX SetPrice", cmd, null);
                    }
                    System.Threading.Thread.Sleep(50);
                    byte[] dd = new byte[this.serialPort.BytesToRead];
                    this.serialPort.Read(dd, 0, this.serialPort.BytesToRead);
                    if (System.IO.File.Exists(LogPath()) && dd.Length > 0)
                    {
                        this.LogAdd("RX SetPrice", dd, null);
                    }

                    byte[] foo = this.GetStatusFoo(fp);
                    if(foo.Length >= 2)
                    {
                        if (foo[1] == 1)
                        {
                            return false;
                        }
                    }
                    fp.PresetAmount = 0;
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
            System.Threading.Thread.Sleep(40);
            byte[] response = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(response, 0, this.serialPort.BytesToRead);
            if (System.IO.File.Exists(LogPath()))
            {
                this.LogAdd("RX Halt", response, null);
            }
            System.Threading.Thread.Sleep(40);

            byte[] buf2 = Commands.Halt(fp.Address);
            this.serialPort.Write(buf2, 0, (int)buf2.Length);
            if (System.IO.File.Exists(LogPath()))
            {
                this.LogAdd("TX Halt", buf2, null);
            }
            System.Threading.Thread.Sleep(40);
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
                System.Threading.Thread.Sleep(40);


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
                    byte[] cmdnoz = CMDGetNozzle();
                    this.serialPort.Write(cmdnoz, 0, cmdnoz.Length);
                    if (System.IO.File.Exists(LogPath()))
                    {
                        this.LogAdd("TX GetNozzle", cmdnoz, null);
                    }
                    while (this.serialPort.BytesToRead < 28 && waiting < 300)
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
