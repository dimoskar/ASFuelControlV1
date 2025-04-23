using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ASFuelControl.Common;
using ASFuelControl.Common.Enumerators;
using System.Globalization;
using System.IO;
using System.Diagnostics;

namespace ASFuelControl.Prime
{
    public class PrimeProtocol : Common.IFuelProtocol
    {
        #region Basics
        public event EventHandler<Common.FuelPointValuesArgs> DataChanged;
        public event EventHandler<Common.TotalsEventArgs> TotalsRecieved;
        public event EventHandler<Common.SaleEventArgs> SaleRecieved;
        public event EventHandler<Common.FuelPointValuesArgs> DispenserStatusChanged;
        public event EventHandler DispenserOffline;

        private List<Common.FuelPoint> fuelPoints = new List<Common.FuelPoint>();

        private System.IO.Ports.SerialPort serialPort = new System.IO.Ports.SerialPort();
        private System.Threading.Thread th;

        public Common.DebugValues foo = new Common.DebugValues();
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
                this.serialPort.Parity = System.IO.Ports.Parity.Even;
                this.serialPort.DtrEnable = true;
                this.serialPort.BaudRate = 9600;
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
        public Common.DebugValues DebugStatusDialog(Common.FuelPoint fp)
        {
            foo = null;
            GetStatus(fp);
            foo.Status = fp.Status;
            return foo;
        }
        #endregion

        #region Workflow Dispensers

        private void WorkFlow()
        {
            foreach (Common.FuelPoint fp in this.fuelPoints)
            {
                foreach (Nozzle nz in fp.Nozzles)
                    fp.Nozzles[0].QueryTotals = true;
            }
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


                                    if (this.SetPrice(nz, nz.UntiPriceInt))
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
                            System.Threading.Thread.Sleep(90);
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


        #endregion

        #region GetStatus

        int extendedProperty;
        public static byte[] StatusCommand(int NozzleID)
        {
            byte FPAd1, FPAd2;
            //05 30 31
            if (NozzleID <= 9)
            {
                FPAd1 = BitConverter.GetBytes(48)[0];
                FPAd2 = BitConverter.GetBytes(48 + NozzleID)[0];
            }
            else
            {
                string id = NozzleID.ToString();
                FPAd1 = BitConverter.GetBytes(48 + Int32.Parse(id.Substring(0, 1)))[0];
                FPAd2 = BitConverter.GetBytes(48 + Int32.Parse(id.Substring(1)))[0];
            }

            byte[] send = new byte[] { 0x05, FPAd1, FPAd2 };
            return send;
        }
        public void GetStatus(Common.FuelPoint fp)
        {
            byte[] numArray = PrimeProtocol.StatusCommand(fp.Address);
            this.serialPort.Write(numArray, 0, (int)numArray.Length);

            if (System.IO.File.Exists(LogPath()))
            {
                System.IO.File.AppendAllText(LogPath(), DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " " + BitConverter.ToString(numArray) + "\r\n");
            }



            if (fp.Status == FuelPointStatusEnum.Idle)
            {
                System.Threading.Thread.Sleep(35);
            }
            else
            {
                int num = 0;
                while (this.serialPort.BytesToRead < 22 && num < 300)
                {
                    System.Threading.Thread.Sleep(25);
                    num += 20;
                }
            }

            byte[] numArray1 = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(numArray1, 0, this.serialPort.BytesToRead);
            if (System.IO.File.Exists(LogPath()))
            {
                System.IO.File.AppendAllText(LogPath(), DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " " + BitConverter.ToString(numArray1) + "\r\n");
            }
            this.evaluateStatus(fp, numArray1);
        }
        private void evaluateStatus(Common.FuelPoint fp, byte[] response)
        {
            FuelPointValues fuelPointValue = new FuelPointValues();
            FuelPointStatusEnum oldStatus = fp.Status;
            FuelPointStatusEnum newStatus = fp.Status;
            int currentNozzle = (int)fp.GetExtendedProperty("CurrentNozzle", -1);

            if (response.Length == 0 && DateTime.Now.Subtract(fp.LastValidResponse).TotalSeconds > 6)
            {
                fp.Status = Common.Enumerators.FuelPointStatusEnum.Offline;
            }

            if (response.Length == 0)
                return;
            fp.LastValidResponse = DateTime.Now;


            if (response.Length == 1 && response[0] == 0x06 && fp.Status == FuelPointStatusEnum.Work)
            {
                System.Threading.Thread.Sleep(20);
                GetTransaction(fp);
                System.Threading.Thread.Sleep(20);

                newStatus = FuelPointStatusEnum.Idle;
                //if (currentNozzle >= 0)
                //    fp.Nozzles[currentNozzle].QueryTotals = true;

            }
            if (response.Length == 1 && response[0] == 0x06 && fp.Status == FuelPointStatusEnum.Offline)
            {
                newStatus = FuelPointStatusEnum.Idle;
                fp.SetExtendedProperty("CurrentNozzle", -1);
            }
            if (response.Length == 1 && response[0] == 0x06 && fp.Status == FuelPointStatusEnum.Nozzle)
            {
                newStatus = FuelPointStatusEnum.Idle;
                this.extendedProperty = -1;
                fp.SetExtendedProperty("CurrentNozzle", -1);
            }
            if (response.Length == 8 && response[4] == 0x41 && response[5] == 0x51 && fp.Status == FuelPointStatusEnum.Offline)
            {
                newStatus = FuelPointStatusEnum.Idle;
                fp.SetExtendedProperty("CurrentNozzle", -1);
            }

            //Nozzle
            if (response.Length == 8 && response[4] == 0x41 && response[5] == 0x51 && fp.Status != FuelPointStatusEnum.Work)
            {
                this.extendedProperty = 0;
                newStatus = FuelPointStatusEnum.Nozzle;
                fp.SetExtendedProperty("CurrentNozzle", this.extendedProperty);
            }
            //Work
            //01 30 34 02 50 50 30 30 30 30 30 30 30 30 30 30 30 30 30 30 03 05
            if (response.Length == 22 && response[4] == 0x50 && response[5] == 0x50 && fp.Status == FuelPointStatusEnum.Nozzle)
            {
                newStatus = FuelPointStatusEnum.Work;
            }
            //Eval Display
            if (response.Length == 22 && response[4] == 0x50 && response[5] == 0x50 && fp.Status == FuelPointStatusEnum.Work)
            {
                this.evalDisplay(fp.ActiveNozzle, response);
            }
            //TransactionCompleted
            //01-30-34-02-4C-4B-03-02
            if (response.Length == 8 && response[4] == 0x4C && response[5] == 0x4B && fp.Status == FuelPointStatusEnum.Work)
            {
                System.Threading.Thread.Sleep(20);
                GetTransaction(fp);
                System.Threading.Thread.Sleep(20);
                newStatus = FuelPointStatusEnum.Idle;
                //if (currentNozzle >= 0)
                //    fp.Nozzles[currentNozzle].QueryTotals = true;
            }

            fp.Status = newStatus;
            fp.DispenserStatus = fp.Status;
        }

        #endregion

        #region GetDisplay
        private Common.Nozzle evalDisplay(Common.Nozzle nozzle, byte[] response)
        {
            try
            {
                //Last Transaction
                // 0  1  2  3  4  5  6  7  8  9  10 11 12 13 14 15 16   17 18 19 20 21 22 23  24 25
                //01 30 33 02 54 52 30 36 39 37 [30 30 30 30 30 37 30] [30 30 30 30 30 30 34] 03 0F
                //<SOH>03<STX>TR0697[0000070][0000004]<ETX><SI>
                #region TransactionCompletedDisplay
                if (response.Length == 26)
                {
                    string[] parms = BitConverter.ToString(response).Split('-');

                    string upString = parms[17].Substring(1)
                        + parms[18].Substring(1)
                        + parms[19].Substring(1)
                        + parms[20].Substring(1)
                        + parms[21].Substring(1)
                        + parms[22].Substring(1)
                        + parms[23].Substring(1);
                    string volString = parms[10].Substring(1)
                        + parms[11].Substring(1)
                        + parms[12].Substring(1)
                        + parms[13].Substring(1)
                        + parms[14].Substring(1)
                        + parms[15].Substring(1)
                        + parms[16].Substring(1);

                    nozzle.ParentFuelPoint.DispensedAmount = decimal.Parse(upString) / (decimal)Math.Pow(10, nozzle.ParentFuelPoint.AmountDecimalPlaces);
                    nozzle.ParentFuelPoint.DispensedVolume = decimal.Parse(volString) / (decimal)Math.Pow(10, nozzle.ParentFuelPoint.VolumeDecimalPlaces);

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
                #endregion



                //Display
                // 0  1  2  3  4  5   6  7  8  9 10 11  12  13 14 15 16 17 18 19  20 21
                //01 30 34 02 50 50 [30 30 30 30 34 35 35] [30 30 30 30 30 35 30] 03 04 
                //01 30 31 02 50 50  31 38 32 38 33 33 30   30 31 37 35 33 33 36  03-06
                //<SOH>03<STX>PP[0000,070][00000,04]<ETX><SOH>
                //01 30 31 02 50 50 30 30 30 30 32 30 30 30 30 30 30 30 31 39 03 0A
                //<SOH>01<STX>PP[0000,200][00000,19]<ETX><LF>


                //<SOH>01<STX>PP[2000000][0191800]<ETX><ETX>
                //<SOH>03<STX>PP[3000000][0213300]<ETX><STX>
                //2000,000 L
                //01918,00 €
                #region SaleData
                if (response.Length == 22)
                {
                    string[] parms = BitConverter.ToString(response).Split('-');

                    string upString = parms[13].Substring(1)
                        + parms[14].Substring(1)
                        + parms[15].Substring(1)
                        + parms[16].Substring(1)
                        + parms[17].Substring(1)
                        + parms[18].Substring(1)
                        + parms[19].Substring(1);

                    string volString = parms[6].Substring(1)
                        + parms[7].Substring(1)
                        + parms[8].Substring(1)
                        + parms[9].Substring(1)
                        + parms[10].Substring(1)
                        + parms[11].Substring(1)
                        + parms[12].Substring(1);

                    nozzle.ParentFuelPoint.DispensedAmount = decimal.Parse(upString) / (decimal)Math.Pow(10, nozzle.ParentFuelPoint.AmountDecimalPlaces);
                    nozzle.ParentFuelPoint.DispensedVolume = decimal.Parse(volString) / (decimal)Math.Pow(10, nozzle.ParentFuelPoint.VolumeDecimalPlaces);

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
                #endregion



            }
            catch (Exception ex)
            {
                if (System.IO.File.Exists(LogPath()))
                {
                    System.IO.File.AppendAllText(LogPath(), DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " Exception Display:\r\n" + ex.ToString() + "\r\n");
                }
            }
            return nozzle;
        }

        #endregion

        #region GetTotals

        public static byte[] CMDTotals(int FuelPoint)
        {
            byte FPAd1, FPAd2;
            //05 30 31
            if (FuelPoint <= 9)
            {
                FPAd1 = BitConverter.GetBytes(48)[0];
                FPAd2 = BitConverter.GetBytes(48 + FuelPoint)[0];
            }
            else
            {
                string id = FuelPoint.ToString();
                FPAd1 = BitConverter.GetBytes(48 + Int32.Parse(id.Substring(0, 1)))[0];
                FPAd2 = BitConverter.GetBytes(48 + Int32.Parse(id.Substring(1)))[0];
            }
            byte[] CMD = new byte[] { FPAd1, FPAd2, 0x02, 0x43, 0x51, 0x03 };
            byte LRC_ = LRC(CMD);
            byte[] send = new byte[] { 0x01, FPAd1, FPAd2, 0x02, 0x43, 0x51, 0x03, LRC_ };
            return send;
        }

        public bool GetTotals(Common.Nozzle nozzle)
        {
            int waiting = 0;
            byte[] cmd = CMDTotals(nozzle.ParentFuelPoint.Address);
            this.serialPort.Write(cmd, 0, cmd.Length);
            if (System.IO.File.Exists(LogPath()))
            {
                System.IO.File.AppendAllText(LogPath(), DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " " + BitConverter.ToString(cmd) + "\r\n");
            }
            while (this.serialPort.BytesToRead < 18 && waiting < 300)
            {
                System.Threading.Thread.Sleep(35);
                waiting += 20;
            }

            byte[] response = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(response, 0, this.serialPort.BytesToRead);

            if (System.IO.File.Exists(LogPath()))
            {
                System.IO.File.AppendAllText(LogPath(), DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " " + BitConverter.ToString(response) + "\r\n");
            }
            return evalTotals(nozzle, response);
        }

        private bool evalTotals(Common.Nozzle nozzle, byte[] response)
        {
            if (response.Length == 18)
            {
                //     0  1  2  3  4  5  6  7  8  9 10 11 12 13 14 15 16 17
                //RX  01 30 33 02 43 54 30 32 38 38 37 34 32 38 30 38 03 16
                //    01 30 31 02 43 54 30 30 31 35 32 37 32 39 34 30 03 19  
                //<SOH> 0  1 <STX> CT[0015272940]<ETX><EM>
                //01 30 31 02 43 54 30 30 30 34 38 32 34 33 34 30 03 1A 
                //Nozzle 1 Volume Total : 0.288.742,80 
                string[] Vtotal = BitConverter.ToString(response).Split('-');
                string volume = Vtotal[6].Substring(1)
                              + Vtotal[7].Substring(1)
                              + Vtotal[8].Substring(1)
                              + Vtotal[9].Substring(1)
                              + Vtotal[10].Substring(1)
                              + Vtotal[11].Substring(1)
                              + Vtotal[12].Substring(1)
                              + Vtotal[13].Substring(1)
                              + Vtotal[14].Substring(1);

                nozzle.TotalVolume = decimal.Parse(volume);
                return true;
            }
            else
                return false;
        }
        #endregion

        #region GetLastTransaction

        private static byte[] GetLastTransaction(int FuelPoint)
        {
            byte FPAd1, FPAd2;
            //HEX   01-30-31-02-54-51-03-05
            //ASCII <SOH>01<STX>TQ<ETX><ENQ>

            //01 30 33 02 54 52 30 36 39 37 30 30 30 30 30 37 30 30 30 30 30 30 30 34 03 0F
            //<SOH>01<STX>TR0959 0000200 0000019<ETX><HT>
            if (FuelPoint <= 9)
            {
                FPAd1 = BitConverter.GetBytes(48)[0];
                FPAd2 = BitConverter.GetBytes(48 + FuelPoint)[0];
            }
            else
            {
                string id = FuelPoint.ToString();
                FPAd1 = BitConverter.GetBytes(48 + Int32.Parse(id.Substring(0, 1)))[0];
                FPAd2 = BitConverter.GetBytes(48 + Int32.Parse(id.Substring(1)))[0];
            }
            byte[] CMD = new byte[] { FPAd1, FPAd2, 0x02, 0x54, 0x51, 0x03 };
            byte LRC_ = LRC(CMD);
            byte[] send = new byte[] { 0x01, FPAd1, FPAd2, 0x02, 0x54, 0x51, 0x03, LRC_ };
            return send;
        }

        private void GetTransaction(Common.FuelPoint fp)
        {
            int waiting = 0;
            byte[] cmd = GetLastTransaction(fp.Address);
            this.serialPort.Write(cmd, 0, cmd.Length);

            if (System.IO.File.Exists(LogPath()))
            {
                System.IO.File.AppendAllText(LogPath(), DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " " + BitConverter.ToString(cmd) + "\r\n");
            }

            System.Threading.Thread.Sleep(35);
            while (this.serialPort.BytesToRead < 26 && waiting < 300)
            {
                System.Threading.Thread.Sleep(20);
                waiting += 20;
            }

            byte[] response = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(response, 0, this.serialPort.BytesToRead);

            if (System.IO.File.Exists(LogPath()))
            {
                System.IO.File.AppendAllText(LogPath(), DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " " + BitConverter.ToString(response) + "\r\n");
            }

            this.evalDisplay(fp.ActiveNozzle, response);

        }

        #endregion

        #region Authorise

        public byte[] AuthoriseCMD(int FuelPoint)
        {
            //01 30 33 02 41 50 03 lrc
            int FPAddress = FuelPoint + 48;
            byte[] CMD = new byte[] { 0x30, (byte)FPAddress, 0x02, 0x41, 0x50, 0x03 };
            byte LRC_ = LRC(CMD);
            byte[] send = new byte[] { 0x01, 0x30, (byte)FPAddress, 0x02, 0x41, 0x50, 0x03, LRC_ };
            return send;
        }

        public bool AuthorizeFuelPoint(Common.FuelPoint fp)
        {


            byte[] buffer = AuthoriseCMD(fp.Address);
            this.serialPort.Write(buffer, 0, buffer.Length);
            if (System.IO.File.Exists(LogPath()))
            {
                System.IO.File.AppendAllText(LogPath(), DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " " + BitConverter.ToString(buffer) + "\r\n");
            }
            int waiting = 0;
            while (this.serialPort.BytesToRead < 2 && waiting < 300)
            {
                System.Threading.Thread.Sleep(20);
                waiting += 20;
            }

            byte[] response = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(response, 0, this.serialPort.BytesToRead);
            if (System.IO.File.Exists(LogPath()))
            {
                System.IO.File.AppendAllText(LogPath(), DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " " + BitConverter.ToString(response) + "\r\n");
            }
            return true;
        }

        #endregion

        #region SetPrice
        private byte[] SetPriceCMD(int FuelPoint, int UnitPrice)
        {
            // 0  1  2  3  4  5  6  7  8  9 10 11
            //01 30 33 02 50 43 31 30 39 37 03 LRC
            byte FPAd1, FPAd2;
            //05 30 31
            if (FuelPoint <= 9)
            {
                FPAd1 = BitConverter.GetBytes(48)[0];
                FPAd2 = BitConverter.GetBytes(48 + FuelPoint)[0];
            }
            else
            {
                string id = FuelPoint.ToString();
                FPAd1 = BitConverter.GetBytes(48 + Int32.Parse(id.Substring(0, 1)))[0];
                FPAd2 = BitConverter.GetBytes(48 + Int32.Parse(id.Substring(1)))[0];
            }
            byte[] PR = this.GetNumberAsByte(UnitPrice, 4);
            byte[] CMD = new byte[] { FPAd1, FPAd2, 0x02, 0x50, 0x43, PR[0], PR[1], PR[2], PR[3], 0x03 };
            byte LRC_ = LRC(CMD);
            byte[] send = new byte[] { 0x01, FPAd1, FPAd2, 0x02, 0x50, 0x43, PR[0], PR[1], PR[2], PR[3], 0x03, LRC_ };
            return send;
        }
        private byte[] GetNumberAsByte(long number, int length)
        {
            List<byte> buffer = new List<byte>();
            string str = number.ToString();
            for (int i = str.Length; i < length; i++)
                buffer.Add(48);
            for (int i = 0; i < str.Length; i++)
                buffer.Add((byte)(48 + int.Parse(str[i].ToString())));

            return buffer.ToArray();
        }
        public bool SetPrice(Common.Nozzle nozzle, int unitPrice)
        {

            int waiting = 0;
            byte[] Poll = SetPriceCMD(nozzle.ParentFuelPoint.Address, nozzle.UntiPriceInt);
            this.serialPort.Write(Poll, 0, Poll.Length);
            if (System.IO.File.Exists(LogPath()))
            {
                System.IO.File.AppendAllText(LogPath(), DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " " + BitConverter.ToString(Poll) + "\r\n");
            }
            while (this.serialPort.BytesToRead < 10 && waiting < 300)
            {
                System.Threading.Thread.Sleep(32);
                waiting += 20;
            }
            byte[] responsePoll = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(responsePoll, 0, this.serialPort.BytesToRead);
            if (System.IO.File.Exists(LogPath()))
            {
                System.IO.File.AppendAllText(LogPath(), DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " " + BitConverter.ToString(responsePoll) + "\r\n");
            }
            return true;

        }

        #endregion

        #region Halt
        private byte[] HaltCMD(int FuelPoint)
        {
            // 0  1  2  3  4  5  6  7  8  9 10 11
            //01 30 33 02 50 43 31 30 39 37 03 LRC
            byte FPAd1, FPAd2;
            //05 30 31
            if (FuelPoint <= 9)
            {
                FPAd1 = BitConverter.GetBytes(48)[0];
                FPAd2 = BitConverter.GetBytes(48 + FuelPoint)[0];
            }
            else
            {
                string id = FuelPoint.ToString();
                FPAd1 = BitConverter.GetBytes(48 + Int32.Parse(id.Substring(0, 1)))[0];
                FPAd2 = BitConverter.GetBytes(48 + Int32.Parse(id.Substring(1)))[0];
            }
            byte[] CMD = new byte[] { FPAd1, FPAd2, 0x02, 0x53, 0x43, 0x03 };
            byte LRC_ = LRC(CMD);
            byte[] send = new byte[] { 0x01, FPAd1, FPAd2, 0x02, 0x53, 0x43, 0x03, LRC_ };
            return send;
        }
        public bool Halt(Common.FuelPoint fp)
        {

            int waiting = 0;
            byte[] Poll = HaltCMD(fp.Address);
            this.serialPort.Write(Poll, 0, Poll.Length);
            if (System.IO.File.Exists(LogPath()))
            {
                System.IO.File.AppendAllText(LogPath(), DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " " + BitConverter.ToString(Poll) + "\r\n");
            }
            while (this.serialPort.BytesToRead < 10 && waiting < 300)
            {
                System.Threading.Thread.Sleep(25);
                waiting += 20;
            }
            byte[] responsePoll = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(responsePoll, 0, this.serialPort.BytesToRead);
            if (System.IO.File.Exists(LogPath()))
            {
                System.IO.File.AppendAllText(LogPath(), DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " " + BitConverter.ToString(responsePoll) + "\r\n");
            }
            return true;

        }

        #endregion

        #region Tools

        public static byte LRC(byte[] bytes)
        {
            int LRC = 0;
            for (int i = 0; i < bytes.Length; i++)
            {
                LRC ^= bytes[i];
            }
            return (byte)(LRC);
        }
        public string LogPath()
        {
            string LogPath = "Prime_" + this.CommunicationPort + ".log";
            return LogPath;
        }

        #endregion

    }
}