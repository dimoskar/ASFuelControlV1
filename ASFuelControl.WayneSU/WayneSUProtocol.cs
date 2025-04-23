using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ASFuelControl.Common;
using ASFuelControl.Common.Enumerators;
using System.IO;
using System.IO.Ports;

namespace ASFuelControl.WayneSU
{
    public class WayneSUProtocol : Common.IFuelProtocol
    {
        #region public events

        public event EventHandler<Common.FuelPointValuesArgs> DataChanged;
        public event EventHandler<Common.TotalsEventArgs> TotalsRecieved;
        public event EventHandler<Common.SaleEventArgs> SaleRecieved;
        public event EventHandler<Common.FuelPointValuesArgs> DispenserStatusChanged;
        public event EventHandler DispenserOffline;

        #endregion

        #region private variables

        private List<Common.FuelPoint> fuelPoints = new List<Common.FuelPoint>();
        private System.IO.Ports.SerialPort serialPort = new System.IO.Ports.SerialPort();
        private System.Threading.Thread th;

        #endregion

        #region public properties
        int NozzleInd = 0;
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

        #endregion

        #region public methods

        public void Connect()
        {
            try
            {
                this.serialPort.PortName = this.CommunicationPort;
                this.serialPort.Parity = System.IO.Ports.Parity.Odd;
                this.serialPort.BaudRate = 9600;
                this.serialPort.Open();
                this.th = new System.Threading.Thread(new System.Threading.ThreadStart(this.ThreadRun));
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

        #region Threading__

        private void ThreadRun()
        {
            foreach (Common.FuelPoint fp in this.fuelPoints)
            {
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
                            if (fp.Status == Common.Enumerators.FuelPointStatusEnum.Auth)
                            {
                                this.Initialize(fp);
                                continue;
                            }
                            if (fp.QueryHalt)
                            {
                                if (this.Lock(fp))
                                {
                                    fp.QueryHalt = false;
                                    fp.Halted = true;
                                    continue;
                                }
                            }
                            if (fp.QueryResume)
                            {
                                if (fp.Status == Common.Enumerators.FuelPointStatusEnum.Close)
                                {
                                    if (this.UnLock(fp))
                                    {
                                        fp.QueryResume = false;
                                        fp.Halted = false;
                                    }
                                }
                            }

                            if (fp.QuerySetPrice)
                            {
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
                            int nozzleForTotals = fp.Nozzles.Where(n => n.QueryTotals).Count();
                            if (nozzleForTotals > 0)
                            {
                                foreach (Common.Nozzle nz in fp.Nozzles)
                                {
                                    if (nz.QueryTotals)
                                    {
                                        this.GetTotals(nz);
                                    }
                                }

                                continue;
                            }

                            if (fp.QueryAuthorize)
                            {
                                if (this.AuthorizeFuelPoint(fp))
                                    fp.QueryAuthorize = false;
                                continue;
                                // }

                            }

                            if (fp.Status == Common.Enumerators.FuelPointStatusEnum.TransactionCompleted)
                            {
                            }
                            Common.Enumerators.FuelPointStatusEnum oldStatus = fp.Status;

                            this.GetStatus(fp);

                        }
                        finally
                        {
                            System.Threading.Thread.Sleep(150);
                        }

                    }
                }
                catch (Exception e)
                {
                    System.Threading.Thread.Sleep(250);
                }
            }
        }
        #endregion

        #region Fuel Point Commands

        private byte[] ExecuteCommand(int responseLength, byte[] cmd)
        {
            string str1 = BitConverter.ToString(cmd);
            Logger.Output("Wayne_" + this.CommunicationPort + ".log", string.Format("TX{0}", str1), "ExecuteCommand");

            this.serialPort.Write(cmd, 0, cmd.Length);
            int waiting = 0;
            while (this.serialPort.BytesToRead < responseLength + cmd.Length && waiting < 300)
            {
                System.Threading.Thread.Sleep(20);
                waiting += 20;
            }
            byte[] upResponse = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(upResponse, 0, this.serialPort.BytesToRead);
            if (upResponse.Length != responseLength + cmd.Length)
                return new byte[0];
            
            string str2 = BitConverter.ToString(upResponse);
            
            Logger.Output("Wayne_" + this.CommunicationPort + ".log", string.Format("RX{0}", str2), "ExecuteCommand");
            
            if (str2.Contains(str1))
                return upResponse.ToList().Skip(cmd.Length).ToArray();
            return new byte[0];
        }

        private bool Initialize(FuelPoint fp)
        {
            byte NozzleIdAddress = BitConverter.GetBytes(8 * fp.Address)[0];
            int Checksum = (255 - (8 * fp.Address));
            byte[] send = new byte[] { 0x00, 0x00, NozzleIdAddress, (byte)Checksum, 0x87, 0x78, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0xFF };
            send = this.ExecuteCommand(13, send);
            if (send.Length < 12)
            {
                return false;
            }
            byte b1 = send[6];
            byte b2 = send[10];
            if (b1 == 0xC0 && b2 == 0x91)
                return false;
            return true;
        }

        private void GetStatus(FuelPoint fp)
        {
            /* Logger.Output("WayneVista", "GetSTatus " +"Channel " + fp.Channel.ToString()
                    + " NozzleCount " + "  " + fp.NozzleCount.ToString()
                    + " Address " + "  " + fp.Address.ToString()                   
                 // + " _ " + fp.ActiveNozzleIndex.ToString() 
                 // + " _ " + fp.Address.ToString()
                 //+ " _ " + fp.Channel.ToString()
                 //+ " _ " + fp.Nozzles.ToString()
                 //+ " _ " + fp.NozzleCount.ToString()
                    , "DATA");*/
            byte NozzleIdAddress = BitConverter.GetBytes(1 + (8 * fp.Address))[0];
            int Checksum = (255 - (1 + (8 * fp.Address)));
            byte[] send = new byte[] { 0x00, 0x00, NozzleIdAddress, (byte)Checksum, 0xFF };
            send = this.ExecuteCommand(13, send);
            this.EvaluateStatus(fp, send);
        }

        private bool Lock(FuelPoint fp)
        {
            byte NozzleIdAddress = BitConverter.GetBytes(8 * fp.Address)[0];
            int Checksum = (255 - (8 * fp.Address));
            byte[] send = new byte[] { 0x00, 0x00, NozzleIdAddress, (byte)Checksum, 0x20, 0xDF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0xFF };
            send = this.ExecuteCommand(13, send);
            this.GetStatus(fp);
            if (fp.Status == Common.Enumerators.FuelPointStatusEnum.Close)
                return true;
            return false;
        }

        private bool UnLock(FuelPoint fp)
        {
            byte NozzleIdAddress = BitConverter.GetBytes(8 * fp.Address)[0];
            int Checksum = (255 - (8 * fp.Address));
            byte[] send = new byte[] { 0x00, 0x00, NozzleIdAddress, (byte)Checksum, 0x10, 0xEF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0xFF };
            send = this.ExecuteCommand(13, send);
            this.GetStatus(fp);
            if (fp.Status != Common.Enumerators.FuelPointStatusEnum.Close)
                return true;
            return false;
        }

        private void GetTotals(Nozzle nz)
        {
            try
            {
                System.Threading.Thread.Sleep(800);
                Logger.Output("Wayne.log", "Before Get Totals", "GetTotals");
                nz.ParentFuelPoint.Initialized = true;
                byte[] buffer1 = this.GetTotalsVolume1(nz);
                byte[] buffer2 = this.GetTotalsVolume2(nz);

                //System.Threading.Thread.Sleep(400);
                if (buffer1.Length < 11 || buffer2.Length < 11)
                {
                    Logger.Output("Wayne.log", "Returned < 11", "GetTotals");
                    return;
                }
                Logger.Output("Wayne.log", "Get Totals OK", "GetTotals");

                byte b1 = buffer1[10];
                byte b2 = buffer1[8];
                byte b3 = buffer2[10];
                byte b4 = buffer2[8];
                byte[] buffer = new byte[] { b1, b2, b3, b4 };
                string val = BitConverter.ToString(buffer).Replace("-", "");

                Logger.Output("Wayne.log", string.Format("Totals:{0}", val), "GetTotals");

                //nz.QueryTotals = false;
                decimal lastVolume = nz.TotalVolume;
                nz.TotalVolume = decimal.Parse(val);
                decimal diff = (nz.TotalVolume - lastVolume) / (decimal)Math.Pow(10, nz.ParentFuelPoint.TotalDecimalPlaces);
                if (diff > 0 && diff < 10000)
                {
                    nz.ParentFuelPoint.DispensedVolume = diff;
                    nz.ParentFuelPoint.DispensedAmount = decimal.Round(diff * nz.UnitPrice, nz.ParentFuelPoint.AmountDecimalPlaces);


                    Logger.Output("Wayne.log", "Before DataChanged", "GetTotals");

                    Common.FuelPointValues values = new FuelPointValues();

                    if (this.DataChanged != null)
                    { 
                        values.CurrentVolume = diff;
                        values.CurrentSalePrice = nz.UnitPrice;
                        values.CurrentPriceTotal = decimal.Round(diff * nz.UnitPrice, nz.ParentFuelPoint.AmountDecimalPlaces);
                        values.ActiveNozzle = nz.Index;
                        values.Status = nz.ParentFuelPoint.Status;
                        this.DataChanged(this, new FuelPointValuesArgs() { CurrentFuelPoint = nz.ParentFuelPoint, CurrentNozzleId = nz.Index, Values = values });
                        Logger.Output("Wayne.log", "After DataChanged", "GetTotals");
                    }

                    System.Threading.Thread.Sleep(1000);

                    values = new Common.FuelPointValues();
                    nz.ParentFuelPoint.ActiveNozzle = nz;
                    values.ActiveNozzle = nz.Index;

                    values.Status = FuelPointStatusEnum.TransactionCompleted;
                    this.DispenserStatusChanged(this, new Common.FuelPointValuesArgs()
                    {
                        CurrentFuelPoint = nz.ParentFuelPoint,
                        CurrentNozzleId = nz.Index,
                        Values = values
                    });
                    System.Threading.Thread.Sleep(1000);
                }
                
                Logger.Output("Wayne.log", "Before TotalsRecieved", "GetTotals");
                if (this.TotalsRecieved != null)
                {
                    this.TotalsRecieved(this, new Common.TotalsEventArgs(nz.ParentFuelPoint, nz.Index, nz.TotalVolume, nz.TotalPrice));
                    Logger.Output("Wayne.log", string.Format("After TotalsRecieved : {0}, {1}, {2}, {3}", nz.ParentFuelPoint.Address, nz.Index, nz.TotalVolume, nz.TotalPrice), "GetTotals");
                }
            }
            catch(Exception ex)
            {

                Logger.Output("Wayne.log", ex.Message, "GetTotals");
            }
        }

        private byte[] GetTotalsVolume1(Nozzle nz)
        {
            byte bytes = BitConverter.GetBytes(47 + nz.Index)[0];
            int index = 255 - (47 + nz.Index);
            byte num = BitConverter.GetBytes(7 + 8 * nz.ParentFuelPoint.Address)[0];
            int address = 255 - (7 + 8 * nz.ParentFuelPoint.Address);
            byte[] numArray = new byte[] { 0, 0, 0, 0, 2, 253, 0, 0, 0, 255, 0, 255, 255 };
            numArray[2] = num;
            numArray[3] = (byte)address;
            if ((nz.ParentFuelPoint.Channel == 100 ? true : nz.ParentFuelPoint.Channel == 90))
            {
                numArray[6] = 0;
                numArray[7] = 255;
            }
            else
            {
                numArray[6] = bytes;
                numArray[7] = (byte)index;
            }
            return this.ExecuteCommand(13, numArray);
        }

        private byte[] GetTotalsVolume2(Nozzle nz)
        {
            byte bytes = BitConverter.GetBytes(47 + nz.Index)[0];
            int index = 255 - (47 + nz.Index);
            byte num = BitConverter.GetBytes(7 + 8 * nz.ParentFuelPoint.Address)[0];
            int address = 255 - (7 + 8 * nz.ParentFuelPoint.Address);
            byte[] numArray = new byte[] { 0, 0, 0, 0, 4, 251, 0, 0, 0, 255, 0, 255, 255 };
            numArray[2] = num;
            numArray[3] = (byte)address;
            if ((nz.ParentFuelPoint.Channel == 100 ? true : nz.ParentFuelPoint.Channel == 90))
            {
                numArray[6] = 0;
                numArray[7] = 255;
            }
            else
            {
                numArray[6] = bytes;
                numArray[7] = (byte)index;
            }
            return this.ExecuteCommand(13, numArray);
        }

        private byte[] ClearDisplay(FuelPoint fp)
        {
            byte NozzleIdAddress = BitConverter.GetBytes(8 * fp.Address)[0];
            int Checksum = (255 - (8 * fp.Address));
            byte[] send = new byte[] { 0x00, 0x00, NozzleIdAddress, (byte)Checksum, 0x88, 0x77, 0x14, 0xEB, 0x00, 0xFF, 0x00, 0xFF, 0xFF };
            send = this.ExecuteCommand(13, send);
            return send;
        }

        private byte[] ResetStatus(FuelPoint fp)
        {
            byte NozzleIdAddress = BitConverter.GetBytes(8 * fp.Address)[0];
            int Checksum = (255 - (8 * fp.Address));
            byte[] send = new byte[] { 0x00, 0x00, NozzleIdAddress, (byte)Checksum, 0x00, 0xFF, 0x02, 0xFD, 0x00, 0xFF, 0x00, 0xFF, 0xFF };
            send = this.ExecuteCommand(13, send);
            return send;
        }

        private byte[] GetPrice(Nozzle nz)
        {
            byte FuelPoint = BitConverter.GetBytes(7 + (8 * nz.ParentFuelPoint.Address))[0];
            int ChecksumF = (255 - (7 + (8 * nz.ParentFuelPoint.Address)));
            byte NozzleIdAddress = BitConverter.GetBytes(nz.Index - 1)[0];
            int ChecksumN = (255 - (nz.Index));
            byte[] send = new byte[] { 0x00, 0x00, FuelPoint, (byte)ChecksumF, 0x00, 0xFF, NozzleIdAddress, (byte)ChecksumN, 0x00, 0xFF, 0x00, 0xFF, 0xFF };
            send = this.ExecuteCommand(13, send);
            return send;
        }

        private bool SetPrice(Nozzle nz, int price)
        {
            string priceValue = price.ToString("X4");
            string val1 = priceValue.Substring(0, 2);
            string val2 = priceValue.Substring(2, 2);
            byte b1 = (byte)int.Parse(val1, System.Globalization.NumberStyles.HexNumber);
            byte b2 = (byte)int.Parse(val2, System.Globalization.NumberStyles.HexNumber);

            byte FuelPointID = BitConverter.GetBytes(7 + (8 * nz.ParentFuelPoint.Address))[0];
            int FuelPointCRC = (255 - (7 + (8 * nz.ParentFuelPoint.Address)));

            byte Nozzle = BitConverter.GetBytes(nz.Index - 1)[0];
            int NozzleCRC = (255 - (nz.Index - 1));

            byte[] send = new byte[] { 0x00, 0x00, FuelPointID, (byte)FuelPointCRC, 0x01, 0xFE, Nozzle, (byte)NozzleCRC, b2, (byte)(255 - b2), b1, (byte)(255 - b1), 0xFF };
            byte[] resp = this.ExecuteCommand(13, send);
            /* Logger.Output("SETPRICES", BitConverter.ToString(send) + "<--"
                + " Amtk_a: " + nz.ParentFuelPoint.Address.ToString()
                + " Ajqosyk^mio: " + nz.Index.ToString()
                , "TX");*/
            send = this.ExecuteCommand(13, send);

            if (BitConverter.ToString(resp) == BitConverter.ToString(send))
                return true;
            return false;
        }

        private byte[] PreAuthoriseFuelPoint(int address, int nz)
        {
            //00 00 [10] EF [88] 77 14 EB 00 FF 00 FF FF
            //00 00 10 EF 88 77 14 EB 00 FF 00 FF FF
            //       FP      NZ
            //byte NozzleIdAddress = BitConverter.GetBytes(135 + nz.Index)[0];
            byte NozzleIdAddress = BitConverter.GetBytes(135 + nz)[0];
            int ChecksumNozzle = (255 - (135 + nz));
            byte FuelPointAddress = BitConverter.GetBytes(8 * address)[0];
            int ChecksumFuel = (255 - (8 * address));
            byte[] send = new byte[] { 0x00, 0x00, FuelPointAddress, (byte)ChecksumFuel, NozzleIdAddress, (byte)ChecksumNozzle, 0x14, 0xEB, 0x00, 0xFF, 0x00, 0xFF, 0xFF };
            return send;
        }

        private bool Pre(int address, int nz)
        {
            byte NozzleIdAddress = BitConverter.GetBytes(135 + nz)[0];
            int ChecksumNozzle = (255 - (135 + nz));
            byte FuelPointAddress = BitConverter.GetBytes(8 * address)[0];
            int ChecksumFuel = (255 - (8 * address));
            byte[] send = new byte[] { 0x00, 0x00, FuelPointAddress, (byte)ChecksumFuel, NozzleIdAddress, (byte)ChecksumNozzle, 0x14, 0xEB, 0x00, 0xFF, 0x00, 0xFF, 0xFF };
            send = this.ExecuteCommand(13, send);
            return true;
        }

        private byte[] Authorize(FuelPoint fp)
        {
            //00 00 10 EF 00 FF 02 FD 00 FF 00 FF FF
            byte nozzleIdAddress = BitConverter.GetBytes(8 * fp.Address)[0];
            int checksum = (255 - (8 * fp.Address));
            byte[] send = new byte[] { 0x00, 0x00, nozzleIdAddress, (byte)checksum, 0x00, 0xFF, 0x02, 0xFD, 0x00, 0xFF, 0x00, 0xFF, 0xFF };
            return send;
        }

        private bool AuthorizeFuelPoint(FuelPoint fp)
        {
            try
            {
                //00 00 10 EF 00 FF 02 FD 00 FF 00 FF FF <-- Only For IFSF SU86
                //00 00 08 F7 8F 70 20 DF 00 FF 00 FF FF <-- Authorise All Protocols
                byte nozzleIdAddress = BitConverter.GetBytes(8 * fp.Address)[0];
                int checksum = (255 - (8 * fp.Address));
                byte[] send = new byte[] { 0x00, 0x00, nozzleIdAddress, (byte)checksum, 0x8F, 0x70, 0x20, 0xDF, 0x00, 0xFF, 0x00, 0xFF, 0xFF };
                send = this.ExecuteCommand(0, send);
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Command Evaluation Methods
        private void EvaluateStatus(FuelPoint fp, byte[] response)
        {
            if (System.IO.File.Exists("WAYNESU_" + fp.Address.ToString() + ".log"))
            {
                System.IO.File.AppendAllText("WAYNESU_" + fp.Address.ToString() + ".log", "GetStatus\t" + DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " RX: " + BitConverter.ToString(response) + "\r\n");
            }
            //07 - 27           -> IDLE
            //17 - 37           -> LOCKED
            //00 - 20 / 21 / 22 -> NOZZLE
            //88 - A8 / A9 / AA -> WORK
            //8F - AF           -> TRANSACTION COMPLETED
            //Logger.Output("sale", BitConverter.ToString(response), "RX");
            int currentNozzle = (int)fp.GetExtendedProperty("CurrentNozzle", -1);

            try
            {
                if (response.Length == 0 && DateTime.Now.Subtract(fp.LastValidResponse).TotalSeconds > 5)
                {
                    if (this.DispenserOffline != null)
                        this.DispenserOffline(fp, new EventArgs());
                    return;
                }
                //if (response.Length == 0 && DateTime.Now.Subtract(fp.LastValidResponse).TotalSeconds > 5)
                //{
                //    fp.Status = Common.Enumerators.FuelPointStatusEnum.Offline;
                //    if (this.DispenserStatusChanged != null)
                //    {
                //        Common.FuelPointValues values = new Common.FuelPointValues();
                //        values.Status = Common.Enumerators.FuelPointStatusEnum.Offline;
                //        this.DispenserStatusChanged(this, new Common.FuelPointValuesArgs()
                //        {
                //            CurrentFuelPoint = fp,
                //            CurrentNozzleId = 0,
                //            Values = values
                //        });
                //    }
                //    return;
                //}
                if (response.Length == 0)
                    return;
                fp.LastValidResponse = DateTime.Now;

                Common.Enumerators.FuelPointStatusEnum status = fp.Status;
                Common.Enumerators.FuelPointStatusEnum oldStatus = fp.Status;


                //IDLE 
                if (response[10] == (byte)0x07 || response[10] == (byte)0x27)
                {
                    status = Common.Enumerators.FuelPointStatusEnum.Idle;
                    currentNozzle = -1;
                }
                //IDLE AUTHORIZED
                if (response[10] == (byte)0x2f || response[10] == (byte)0x0F)
                {
                    status = Common.Enumerators.FuelPointStatusEnum.Idle;
                    currentNozzle = -1;
                }
                //LOCKED
                else if (response[10] == (byte)0x17 || response[10] == (byte)0x37)
                {
                    status = Common.Enumerators.FuelPointStatusEnum.Close;
                    if (fp.Status != Common.Enumerators.FuelPointStatusEnum.Work)
                    {
                        fp.Halted = true;
                        fp.QueryResume = true;
                    }
                }


                //Nozzle Status
                else if (response[10] == (byte)0x00 || response[10] == (byte)0x20 || response[10] == (byte)0x21 || response[10] == (byte)0x22)
                {
                    status = Common.Enumerators.FuelPointStatusEnum.Nozzle;
                    if (response[10] == (byte)0x20 || response[10] == (byte)0x00) { currentNozzle = 0; }
                    if (response[10] == (byte)0x21 || response[10] == (byte)0x01) { currentNozzle = 1; }
                    if (response[10] == (byte)0x22 || response[10] == (byte)0x02) { currentNozzle = 2; }
                    fp.SetExtendedProperty("CurrentNozzle", currentNozzle);
                }

                //Ready Status
                else if (response[10] == (byte)0xE8 || response[10] == (byte)0xE9 || response[10] == (byte)0xEA || response[10] == (byte)0x08)
                {
                    status = Common.Enumerators.FuelPointStatusEnum.Ready;
                }
                //Work Status
                else if (response[10] == (byte)0xA8 || response[10] == (byte)0xA9 || response[10] == (byte)0xAA || response[10] == (byte)0x88)
                {
                    status = Common.Enumerators.FuelPointStatusEnum.Work;
                }
                else if (response[10] == (byte)0xAF || response[10] == (byte)0x8F)
                {
                    status = Common.Enumerators.FuelPointStatusEnum.TransactionCompleted;
                    if (fp.Channel == 100)
                    {
                        byte[] respBuffer = this.ClearDisplay(fp);
                        System.Threading.Thread.Sleep(150);
                        while (respBuffer == null || respBuffer.Length == 0)
                        {
                            respBuffer = this.ClearDisplay(fp);
                            System.Threading.Thread.Sleep(150);
                        }
                        respBuffer = this.ResetStatus(fp);
                    }
                    System.Threading.Thread.Sleep(25);
                    status = Common.Enumerators.FuelPointStatusEnum.Idle;
                    if (currentNozzle >= 0)
                        fp.Nozzles[currentNozzle].QueryTotals = true;
                }
                else
                {
                    if(fp.Status == FuelPointStatusEnum.Work)
                    {
                        Logger.Output("Wayne_" + this.CommunicationPort + ".log", BitConverter.ToString(response), "EvaluateStatus");
                    }
                }
                if (status != fp.Status && status != Common.Enumerators.FuelPointStatusEnum.Auth)
                {
                    fp.Status = status;
                    fp.DispenserStatus = status;
                    FuelPointValues vals = new FuelPointValues();
                    vals.Status = status;
                    //EventHandler<FuelPointValuesArgs> eventHandler1 = this.DispenserStatusChanged;
                    //FuelPointValuesArgs fuelPointValuesArg1 = new FuelPointValuesArgs()
                    //{
                    //    CurrentFuelPoint = fp,
                    //    CurrentNozzleId = fp.ActiveNozzleIndex,
                    //    Values = vals
                    //};
                    //eventHandler1(this, fuelPointValuesArg1);
                }
                fp.SetExtendedProperty("CurrentNozzle", currentNozzle);
                if (status != oldStatus)
                {
                    Common.FuelPointValues values = new Common.FuelPointValues();
                    if (status == Common.Enumerators.FuelPointStatusEnum.Idle || status == Common.Enumerators.FuelPointStatusEnum.Offline)
                    {
                        values.ActiveNozzle = -1;
                        fp.ActiveNozzle = null;
                    }
                    else
                    {
                        fp.ActiveNozzle = fp.Nozzles[currentNozzle];
                        values.ActiveNozzle = currentNozzle;
                    }
                    values.Status = status;
                    this.DispenserStatusChanged(this, new Common.FuelPointValuesArgs()
                    {
                        CurrentFuelPoint = fp,
                        CurrentNozzleId = currentNozzle,
                        Values = values
                    });
                }
            }
            catch
            {
            }
        }
        #endregion

        #region Logger
        public class Logger
        {
            public static void Output(string FileNameToSave, string Error_Recieve, string VoidMethodName)
            {
                string fileName = FileNameToSave;
                if (!System.IO.File.Exists(fileName))
                    return;
                using (StreamWriter writer = new StreamWriter(fileName, true, Encoding.UTF8))
                {
                    writer.Write("-->" + VoidMethodName + "  " + DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss.fff") + "   <--- \r\n" + Error_Recieve.ToString() + "\r\n\r\n");
                }
            }
        }
        #endregion
    }
}


