using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ASFuelControl.Common;
using ASFuelControl.Common.Enumerators;
using System.Globalization;
using System.IO;
using System.Diagnostics;


namespace ASFuelControl.Europump
{
    public class EuropumpProtocol : Common.IFuelProtocol
    {
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
                this.serialPort.Parity = System.IO.Ports.Parity.None;
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
        int extendedProperty;
        private void ThreadRun()
        {
            foreach (Common.FuelPoint fp in this.fuelPoints)
            {
                foreach (Nozzle nz in fp.Nozzles)
                {
                    if (this.Halt(fp))
                        continue;
                }
            }
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
                                if (this.Halt(fp))
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

                            #region Status
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
                            #endregion


                        finally
                        {
                            System.Threading.Thread.Sleep(100);
                        }
                    }

                }

                catch (Exception e)
                {

                    System.IO.Directory.CreateDirectory(System.Environment.CurrentDirectory + "\\logs");
                    System.IO.File.AppendAllText(System.Environment.CurrentDirectory + "\\logs" + "\\Europump_Thread_error.txt", "\n" + e.ToString());
                    System.Threading.Thread.Sleep(100);

                }

            }

        }

        #region Commands

        public static byte[] ACK(int NozzleID)
        {
            //[50] C0 [FA]
            byte NozzleIdAddress = BitConverter.GetBytes(79 + NozzleID)[0];
            byte[] send = new byte[] { NozzleIdAddress, 0xC0, 0xFA };
            return send;
        }
        private bool GetDisplayCommand(Common.FuelPoint fp)
        {
            byte[] Buffer = ACK(fp.Address);
            this.serialPort.Write(Buffer, 0, (int)Buffer.Length);
            System.Threading.Thread.Sleep(50);
            byte[] ReadBuffer = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(ReadBuffer, 0, this.serialPort.BytesToRead);
            return true;
        }
        public bool ClearData(int fpAddress)
        {

            byte[] ACKb = ACK(fpAddress);
            this.serialPort.Write(ACKb, 0, (int)ACKb.Length);
            if (System.IO.File.Exists("Europump.log"))
            {
                System.IO.File.AppendAllText("Europump.log", "ClearData\t" + DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " TX: " + BitConverter.ToString(ACKb) + "\r\n");
            }
            System.Threading.Thread.Sleep(25);
            byte[] responseACK = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(responseACK, 0, this.serialPort.BytesToRead);
            if (System.IO.File.Exists("Europump.log"))
            {
                System.IO.File.AppendAllText("Europump.log", "ClearData\t" + DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " RX: " + BitConverter.ToString(responseACK) + "\r\n");
            }
            return true;
        }
        private byte[] GetDisplayCMD(int FuelPoint)
        {
            //50 30 01 01 04 9E 9F 03 FA
            string input = (49 + FuelPoint).ToString() + "30010104";
            var bytes = CRC.HexToBytes(input);
            string hex = CRC.ComputeChecksum(bytes).ToString("x2");
            if (hex.Length == 1) { hex = "000" + hex; }
            if (hex.Length == 2) { hex = "00" + hex; }
            if (hex.Length == 3) { hex = "0" + hex; }
            byte[] crc = CRC.ConvertHexStringToByteArray(hex);
            byte FuelPointAddress = BitConverter.GetBytes(79 + FuelPoint)[0];
            byte[] send = new byte[] { FuelPointAddress, 0x30, 0x01, 0x01, 0x04, crc[1], crc[0], 0x03, 0xFA };

            return send;
        }

        #endregion

        #region Halt

        private byte[] HaltCMD(int FuelPoint)
        {
            //50 30 01 01 08 9E 9A 03 FA
            string input = (49 + FuelPoint).ToString() + "30010106";
            var bytes = CRC.HexToBytes(input);
            string hex = CRC.ComputeChecksum(bytes).ToString("x2");
            if (hex.Length == 1) { hex = "000" + hex; }
            if (hex.Length == 2) { hex = "00" + hex; }
            if (hex.Length == 3) { hex = "0" + hex; }
            byte[] crc = CRC.ConvertHexStringToByteArray(hex);
            byte FuelPointAddress = BitConverter.GetBytes(79 + FuelPoint)[0];
            byte[] send = new byte[] { FuelPointAddress, 0x30, 0x01, 0x01, 0x08, crc[1], crc[0], 0x03, 0xFA };

            return send;
        }
        private bool Halt(Common.FuelPoint fp)
        {
            byte[] buffer = HaltCMD(fp.Address);
            this.serialPort.Write(buffer, 0, (int)buffer.Length);
            if (System.IO.File.Exists("Europump.log"))
            {
                System.IO.File.AppendAllText("Europump.log", "Halt     \t" + DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " TX: " + BitConverter.ToString(buffer) + "\r\n");
            }
            System.Threading.Thread.Sleep(50);
            byte[] response = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(response, 0, this.serialPort.BytesToRead);
            if (System.IO.File.Exists("Europump.log"))
            {
                System.IO.File.AppendAllText("Europump.log", "Halt     \t" + DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " RX: " + BitConverter.ToString(response) + "\r\n");
            }
            return true;
        }

        #endregion

        #region SetPrice
        private byte[] SetPriceCMD(int FuelPoint, int Price)
        {
            //50 30 01 01 05 05 03 00 11 11 86 01 03 FA
            string pr = Price.ToString();
            if (pr.Length == 1) { pr = "000" + pr; }
            if (pr.Length == 2) { pr = "00" + pr; }
            if (pr.Length == 3) { pr = "0" + pr; }
            string input = (49 + FuelPoint).ToString() + "30010105050300" + pr;
            var bytes = CRC.HexToBytes(input);
            string hex = CRC.ComputeChecksum(bytes).ToString("x2");
            if (hex.Length == 1) { hex = "000" + hex; }
            if (hex.Length == 2) { hex = "00" + hex; }
            if (hex.Length == 3) { hex = "0" + hex; }
            byte[] PriceVAR = CRC.ConvertHexStringToByteArray(pr);
            byte[] crc = CRC.ConvertHexStringToByteArray(hex);
            byte FuelPointAddress = BitConverter.GetBytes(79 + FuelPoint)[0];
            byte[] CommandPriceFin = new byte[] { FuelPointAddress, 0x30, 0x01, 0x01, 0x05, 0x05, 0x03, 0x00, PriceVAR[0], PriceVAR[1], crc[1], crc[0], 0x03, 0xFA };
            return CommandPriceFin;
        }
        private bool SetPrice(Common.Nozzle nz, int UnitPrice)
        {
            int waiting = 0;
            byte[] buffer = SetPriceCMD(nz.ParentFuelPoint.Address, UnitPrice);
            this.serialPort.Write(buffer, 0, (int)buffer.Length);
            if (System.IO.File.Exists("Europump.log"))
            {
                System.IO.File.AppendAllText("Europump.log", "SetPrice \t" + DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " TX: " + BitConverter.ToString(buffer) + "\r\n");
            }
            //51-30-01-01-05-05-03-00-10-01-D7-98-03-FA-51-C0-FA
            while (this.serialPort.BytesToRead < 18 && waiting < 300)
            {
                System.Threading.Thread.Sleep(20); //15*4 = 70 [ NP 68ms response time]
                waiting += 20;
            }

            byte[] response = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(response, 0, this.serialPort.BytesToRead);
            if (System.IO.File.Exists("Europump.log"))
            {
                System.IO.File.AppendAllText("Europump.log", "SetPrice \t" + DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " RX: " + BitConverter.ToString(response) + "\r\n");
            }


            byte[] ACKb = ACK(nz.ParentFuelPoint.Address);
            this.serialPort.Write(ACKb, 0, (int)ACKb.Length);

            if (System.IO.File.Exists("Europump.log"))
            {
                System.IO.File.AppendAllText("Europump.log", "SetPrice \t" + DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " TX: " + BitConverter.ToString(ACKb) + "\r\n");
            }
            waiting = 0;
            while (this.serialPort.BytesToRead < 16 && waiting < 300)
            {
                System.Threading.Thread.Sleep(20); //15*4 = 70 [ NP 68ms response time]
                waiting += 20;
            }

            byte[] responseACK = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(responseACK, 0, this.serialPort.BytesToRead);
            //Logger("Totals", BitConverter.ToString(response), "RX");
            if (System.IO.File.Exists("Europump.log"))
            {
                System.IO.File.AppendAllText("Europump.log", "SetPrice \t" + DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " RX: " + BitConverter.ToString(responseACK) + "\r\n");
            }
            return true;
        }

        #endregion

        #region Authorise

        private byte[] AuthoriseCMD(int FuelPoint)
        {
            //50 30 01 01 06 1F 5E 03 FA
            string input = (49 + FuelPoint).ToString() + "30010106";
            var bytes = CRC.HexToBytes(input);
            string hex = CRC.ComputeChecksum(bytes).ToString("x2");
            if (hex.Length == 1) { hex = "000" + hex; }
            if (hex.Length == 2) { hex = "00" + hex; }
            if (hex.Length == 3) { hex = "0" + hex; }
            byte[] crc = CRC.ConvertHexStringToByteArray(hex);
            byte FuelPointAddress = BitConverter.GetBytes(79 + FuelPoint)[0];
            byte[] send = new byte[] { FuelPointAddress, 0x30, 0x01, 0x01, 0x06, crc[1], crc[0], 0x03, 0xFA };

            return send;
        }
        private bool AuthorizeFuelPoint(Common.FuelPoint fp)
        {
            byte[] buffer = AuthoriseCMD(fp.Address);
            this.serialPort.Write(buffer, 0, (int)buffer.Length);
            if (System.IO.File.Exists("Europump.log"))
            {
                System.IO.File.AppendAllText("Europump.log", "Authorise\t" + DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " TX: " + BitConverter.ToString(buffer) + "\r\n");
            }
            System.Threading.Thread.Sleep(80);
            byte[] response = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(response, 0, this.serialPort.BytesToRead);
            if (System.IO.File.Exists("Europump.log"))
            {
                System.IO.File.AppendAllText("Europump.log", "Authorise\t" + DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " RX: " + BitConverter.ToString(response) + "\r\n");
            }
            return true;
        }
        #endregion

        #region GetStatus

        public static byte[] StatusCommand(int NozzleID)
        {
            //[50] 20 [FA]
            byte NozzleIdAddress = BitConverter.GetBytes(79 + NozzleID)[0];
            byte[] send = new byte[] { NozzleIdAddress, 0x20, 0xFA };
            return send;
        }
        public void GetStatus(Common.FuelPoint fp)
        {
            int waiting = 0;
            byte[] buffer = StatusCommand(fp.Address);
            this.serialPort.Write(buffer, 0, (int)buffer.Length);
            if (System.IO.File.Exists("Europump.log"))
            {
                System.IO.File.AppendAllText("Europump.log", "GetStatus\t" + DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " TX: " + BitConverter.ToString(buffer) + "\r\n");
            }
            while (this.serialPort.BytesToRead < 36 && waiting < 300)
            {
                System.Threading.Thread.Sleep(10);
                waiting += 30;
            }
            byte[] response = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(response, 0, this.serialPort.BytesToRead);
            if (System.IO.File.Exists("Europump.log"))
            {
                System.IO.File.AppendAllText("Europump.log", "GetStatus\t" + DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " RX: " + BitConverter.ToString(response) + "\r\n");
            }
            evaluateStatus(fp, response);
        }
        private void evaluateStatus(Common.FuelPoint fp, byte[] response)
        {

            FuelPointValues fuelPointValue = new FuelPointValues();
            FuelPointStatusEnum oldStatus = fp.Status;
            FuelPointStatusEnum newStatus = fp.Status;
            int currentNozzle = (int)fp.GetExtendedProperty("CurrentNozzle", -1);

            if (response.Length == 0 && DateTime.Now.Subtract(fp.LastValidResponse).TotalSeconds > 5)
            {
                fp.Status = Common.Enumerators.FuelPointStatusEnum.Offline;
            }

            if (response.Length == 0)
                return;
            fp.LastValidResponse = DateTime.Now;

            if (fp.Status == FuelPointStatusEnum.Offline)
            {
                newStatus = FuelPointStatusEnum.Idle;
                fp.SetExtendedProperty("CurrentNozzle", -1);
            }

            if ((int)response.Length <= 6)
            {

            }
            else if ((int)response.Length == 52)
            {
                this.ClearData(fp.Address);
            }
            else
            {
                //Eval Display
                // 0  1  2  3  4  5  6  7  8  9 10 11 12 13 14 15
                //50-31-02-08-00-00-00-68-00-00-00-73-DD-1A-03-FA

                if (response.Length == 19 & response[5] == 0x02 && fp.Status == FuelPointStatusEnum.Work)
                {
                    this.evalDisplay(fp.ActiveNozzle, response);
                }
                //Closed 
                // 0  1  2  3  4  5  6  7  8  9 10 11 12 13 14 15 16 17 18 19 20 21 22 23 24 25 26 27 28 29 30 31 32 33
                //50-20-FA-50-31-01-01-02-01-01-01-01-01-02-02-08-00-00-00-00-00-00-00-00-03-04-00-10-69-01-71-57-03-FA
                if (response.Length == 34 && response[5] == 0x01 && response[29] == 0x01)
                {
                    newStatus = FuelPointStatusEnum.Idle;
                    this.extendedProperty = -1;
                    fp.SetExtendedProperty("CurrentNozzle", -1);
                    this.ClearData(fp.Address);
                }
                // 0  1  2  3  4  5  6  7  8  9 10 11 12 13 14 15 16 17 18 19 20 21 22 23 24 25 26 27 28 29 30
                //50-20-FA-50-31-01-01-05-01-01-06-02-08-00-00-09-35-00-00-10-00-03-04-00-10-69-11-82-F3-03-FA   
                if (response.Length == 31 && response[5] == 0x01 && response[23] == 0x11 && fp.Status == FuelPointStatusEnum.Work)
                {
                    this.evalDisplay(fp.ActiveNozzle, response);
                }
                // 0  1  2  3  4  5  6  7  8  9 10 11 12 13 14 15 16 17 18 19 20 21 22 23 24 25 26 27
                //50-20-FA-50-31-01-01-06-02-08-00-00-09-35-00-00-10-00-03-04-00-10-69-11-CF-00-03-FA
                if (response.Length == 28 && response[5] == 0x01 && response[23] == 0x11 && fp.Status == FuelPointStatusEnum.Work)
                {
                    this.evalDisplay(fp.ActiveNozzle, response);
                }

                // 0  1  2  3  4  5  6  7  8  9 10 11 12 13 14 15 16 17 18 19 20
                //50-20-FA-50-31-01-01-05-01-01-05-03-04-00-10-69-01-7C-F4-03-FA
                if (response.Length == 21 && response[5] == 0x01 && response[16] == 0x01)
                {
                    newStatus = FuelPointStatusEnum.Idle;
                    this.extendedProperty = -1;
                    fp.SetExtendedProperty("CurrentNozzle", -1);
                    this.ClearData(fp.Address);
                }

                // 0  1  2  3  4  5  6  7  8  9 10 11 12 13 14 15 16 17
                //50-20-FA-50-31-01-01-05-03-04-00-10-69-01-D6-C1-03-FA
                if (response.Length == 18 && response[5] == 0x01 && response[13] == 0x01 && fp.Status != FuelPointStatusEnum.Idle)
                {
                    newStatus = FuelPointStatusEnum.Idle;
                    this.extendedProperty = -1;
                    fp.SetExtendedProperty("CurrentNozzle", -1);
                    this.ClearData(fp.Address);
                }
                //Nozzle Status
                // 0  1  2  3  4  5  6  7  8  9 10 11 12 13 14 15 16 17 18 19 20
                //53-20-FA-53-31-01-01-05-01-01-05-03-04-00-14-19-11-1A-3A-03-FA
                if (response.Length == 21 && response[5] == 0x01 && response[16] != 0x01)
                {
                    int num1 = response[16];
                    newStatus = FuelPointStatusEnum.Nozzle;
                    if (num1 == 0x11)
                    {
                        this.extendedProperty = 0;
                    }
                    fp.SetExtendedProperty("CurrentNozzle", this.extendedProperty);
                    this.ClearData(fp.Address);
                }
                //Nozzle Status
                // 0  1  2  3  4  5  6  7  8  9 10 11 12 13 14 15 16 17
                //50-20-FA-50-31-01-01-05-03-04-00-12-09-11-5E-CD-03-FA 
                if (response.Length == 18 && response[5] == 0x01 && response[13] != 0x01)
                {
                    int num1 = response[13];
                    newStatus = FuelPointStatusEnum.Nozzle;
                    if (num1 == 0x11)
                    {
                        this.extendedProperty = 0;
                    }
                    fp.SetExtendedProperty("CurrentNozzle", this.extendedProperty);
                    this.ClearData(fp.Address);
                }


                //WorkState
                // 0  1  2  3  4  5  6  7  8  9 10 11 12 13 14 15 16 17 18 19 20 21 22 23 24 25 26 27 28 29 30 31 32 33
                //50 20 FA 50 31 01 01 01 01 01 02 01 01 04 02 0C 00 00 00 00 00 00 00 00 03 04 00 10 69 11 83 EC 03 FA
                if (response.Length == 34 && response[5] == 0x01 && response[29] == 0x11 && fp.Status == FuelPointStatusEnum.Nozzle)
                {
                    newStatus = FuelPointStatusEnum.Work;
                    this.ClearData(fp.Address);
                }



                //Clear Buffer Europump
                // 0  1  2  3  4  5  6  7  8  9 10 11
                //50-31-01-01-05-03-04-00-10-69-01-65-10-01-00-66-33-49-88-00-00-00-00-00-00-00-00-00-00-A7-8E-03-FA
                //51-31-65-10-01-00-08-79-69-14-00-00-00-00-00-00-00-00-00-00-37-AE-03-FA
                if (response.Length == 27 && response[5] == 0x65)
                {
                    this.ClearData(fp.Address);
                }
                if (response.Length == 36 && response[5] == 0x01 && response[14] == 0x65)
                {
                    this.ClearData(fp.Address);
                }



                //Manual End Transaction
                if (response.Length == 31 && response[5] == 0x01 && fp.Status == FuelPointStatusEnum.Work)
                {
                    this.evalDisplay(fp.ActiveNozzle, response);
                    newStatus = FuelPointStatusEnum.Idle;
                    this.extendedProperty = -1;
                    fp.SetExtendedProperty("CurrentNozzle", -1);
                    this.ClearData(fp.Address);
                }
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

                // 0  1  2  3  4  5  6  7  8  9 10 11 12 13 14 15 16 17 18 19 20 21 22 23 24 25 26 27
                //50-20-FA-50-31-01-01-06-02-08-00-00-09-35-00-00-10-00-03-04-00-10-69-11-CF-00-03-FA
                //50 20 FA 50 37 02 08 00 00 00 87 00 00 01 00 56 30 03 FA 
                //50-31-02-08-00-00-00-26-00-00-00-31-35-25-03-FA
                //rx 50 37 02 08 [00 00 00 87] [00 00 01 00] 56 30 03 FA (0.87e) (1.00ltr)
                if (response[5] == 0x02 && response.Length == 19 || response.Length == 22)
                {
                    string[] parms = BitConverter.ToString(response).Split('-');

                    string upString = parms[11] + parms[12] + parms[13] + parms[14];
                    string volString = parms[7] + parms[8] + parms[9] + parms[10];

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
                // 0  1  2  3  4  5  6  7  8  9 10 11 12 13 14 15 16 17 18 19 20 21 22 23 24 25 26 27
                //53-20-FA-53-31-01-01-06-02-08-00-00-00-64-00-00-01-00-03-04-00-15-59-11-21-13-03-FA
                if (response[5] == 0x01 && response.Length == 28)
                {


                    string[] parms = BitConverter.ToString(response).Split('-');

                    string upString = parms[14] + parms[15] + parms[16] + parms[17];
                    string volString = parms[10] + parms[11] + parms[12] + parms[13];

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

            }
            catch (Exception)
            {

            }
            return nozzle;
        }

        #endregion

        #region GetTotals
        public static byte[] requestTotals(int NozzleID)
        {
            string input = (49 + NozzleID).ToString() + "30650101";
            var bytes = CRC.HexToBytes(input);
            string hex = CRC.ComputeChecksum(bytes).ToString("x2");
            byte NozzleIdAddress = BitConverter.GetBytes(79 + NozzleID)[0];
            byte[] crc = CRC.ConvertHexStringToByteArray(hex);
            byte[] send = new byte[] { NozzleIdAddress, 0x30, 0x65, 0x01, 0x01, crc[1], crc[0], 0x03, 0xFA };
            return send;
        }
        public bool GetTotals(Common.Nozzle nozzle)
        {
            int waiting = 0;
            byte[] ACKb = ACK(nozzle.ParentFuelPoint.Address);
            this.serialPort.Write(ACKb, 0, (int)ACKb.Length);

            if (System.IO.File.Exists("Europump.log"))
            {
                System.IO.File.AppendAllText("Europump.log", "GetTotals\t" + DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " TX: " + BitConverter.ToString(ACKb) + "\r\n");
            }
            while (this.serialPort.BytesToRead < 16 && waiting < 300)
            {
                System.Threading.Thread.Sleep(40); //15*4 = 70 [ NP 68ms response time]
                waiting += 20;
            }

            byte[] response = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(response, 0, this.serialPort.BytesToRead);
            //Logger("Totals", BitConverter.ToString(response), "RX");
            if (System.IO.File.Exists("Europump.log"))
            {
                System.IO.File.AppendAllText("Europump.log", "GetTotals\t" + DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " RX: " + BitConverter.ToString(response) + "\r\n");
            }

            byte[] bufferTotal = requestTotals(nozzle.ParentFuelPoint.Address);
            this.serialPort.Write(bufferTotal, 0, (int)bufferTotal.Length);
            if (System.IO.File.Exists("Europump.log"))
            {
                System.IO.File.AppendAllText("Europump.log", "GetTotals\t" + DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " TX: " + BitConverter.ToString(bufferTotal) + "\r\n");
            }
            //Logger("Totals", BitConverter.ToString(bufferTotal), "TX");

            System.Threading.Thread.Sleep(50);
            byte[] responseCMD = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(responseCMD, 0, this.serialPort.BytesToRead);
            if (System.IO.File.Exists("Europump.log"))
            {
                System.IO.File.AppendAllText("Europump.log", "GetTotals\t" + DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " RX: " + BitConverter.ToString(responseCMD) + "\r\n");
            }
            //Logger("Totals", BitConverter.ToString(responseCMD), "RX");

            ////////////////// GetTotals With Poll
            byte[] bufferTot = StatusCommand(nozzle.ParentFuelPoint.Address);
            this.serialPort.Write(bufferTot, 0, (int)bufferTot.Length);
            //Logger("Totals", BitConverter.ToString(bufferTot), "TX");
            if (System.IO.File.Exists("Europump.log"))
            {
                System.IO.File.AppendAllText("Europump.log", "GetTotals\t" + DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " TX: " + BitConverter.ToString(bufferTot) + "\r\n");
            }
            System.Threading.Thread.Sleep(100);
            byte[] responseF = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(responseF, 0, this.serialPort.BytesToRead);
            //Logger("Totals", BitConverter.ToString(responseF), "RX");
            if (System.IO.File.Exists("Europump.log"))
            {
                System.IO.File.AppendAllText("Europump.log", "GetTotals\t" + DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " RX: " + BitConverter.ToString(responseF) + "\r\n");
            }
            return evalTotals(nozzle, responseF);

        }
        private bool evalTotals(Common.Nozzle nozzle, byte[] response)
        {
            //    0  1  2  3  4  5  6  7  8  9 10 11 12 13 14 15 16 17 18 19 20 21 22 23 24 25 26
            //   50 31 65 10 01 00 55 61 27 16 00 00 00 00 00 00 00 00 00 00 94 6A 03 FA
            //   50 20 FA 50 31 65 10 01 00 66 49 92 22 00 00 00 00 00 00 00 00 00 00 63 73 03 FA


            if (response[5] == 0x65)
            {
                string[] Vtotal = BitConverter.ToString(response).Split('-');
                string volume = Vtotal[8] + Vtotal[9] + Vtotal[10] + Vtotal[11] + Vtotal[12];

                nozzle.TotalVolume = decimal.Parse(volume);
                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion

        #region Tools
        private enum responseLength
        {
            ack = 3,
            totals = 27,
            display = 16,
            status = 12,
            RecievedOK = 6,
            TransactionCompleted = 25
        }
        public byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }
        public static class CRC
        {
            const ushort polynomial = 0xA001;
            static readonly ushort[] table = new ushort[256];

            public static ushort ComputeChecksum(byte[] bytes)
            {
                ushort crc = 0;
                for (int i = 0; i < bytes.Length; ++i)
                {
                    byte index = (byte)(crc ^ bytes[i]);
                    crc = (ushort)((crc >> 8) ^ table[index]);
                }
                return crc;
            }
            static CRC()
            {
                ushort value;
                ushort temp;
                for (ushort i = 0; i < table.Length; ++i)
                {
                    value = 0;
                    temp = i;
                    for (byte j = 0; j < 8; ++j)
                    {
                        if (((value ^ temp) & 0x0001) != 0)
                        {
                            value = (ushort)((value >> 1) ^ polynomial);
                        }
                        else
                        {
                            value >>= 1;
                        }
                        temp >>= 1;
                    }
                    table[i] = value;
                }
            }

            public static byte[] ConvertHexStringToByteArray(string hexString)
            {
                if (hexString.Length % 2 != 0)
                {
                    throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, "The binary key cannot have an odd number of digits: {0}", hexString));
                }

                byte[] HexAsBytes = new byte[hexString.Length / 2];
                for (int index = 0; index < HexAsBytes.Length; index++)
                {
                    string byteValue = hexString.Substring(index * 2, 2);
                    HexAsBytes[index] = byte.Parse(byteValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                }

                return HexAsBytes;
            }
            public static byte[] HexToBytes(string input)
            {
                byte[] result = new byte[input.Length / 2];
                for (int i = 0; i < result.Length; i++)
                {
                    result[i] = Convert.ToByte(input.Substring(2 * i, 2), 16);
                }
                return result;
            }
        }
        public static void Logger(string FileNameToSave, string Error_Recieve, string VoidMethodName)
        {
            try
            {
                string fileName = "Logs/Europump_" + FileNameToSave + "_.txt";
                using (StreamWriter writer = new StreamWriter(fileName, true, Encoding.UTF8))
                {
                    writer.Write("-->" + VoidMethodName + "  " + DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss.fff") + "   <--- \r\n\r\n" + Error_Recieve.ToString() + "\r\n\r\n");
                }
            }
            catch (Exception ex)
            {

            }
        }
        #endregion
    }
}
