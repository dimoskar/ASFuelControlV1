using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ASFuelControl.Common;
using System.Globalization;
using ASFuelControl.Common.Enumerators;
using System.IO;

namespace ASFuelControl.WayneDartV1
{
    public class WayneDartV1Protocol : Common.IFuelProtocol
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

        public List<Tuple<int, int, int>> ConsolePrices = new List<Tuple<int, int, int>>();
        public bool GetPrices = false;
        private void ThreadRun()
        {
            foreach (Common.FuelPoint fp in this.fuelPoints)
            {
                fp.QueryHalt = !this.HaltSend(fp);
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

                                fp.QueryHalt = !this.HaltSend(fp);
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

                                ConsolePrices.Clear();
                                System.Threading.Thread.Sleep(10);
                                foreach (Common.Nozzle nz in fp.Nozzles)
                                {
                                    GetPriceFromConsole(nz);
                                }

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
                            System.Threading.Thread.Sleep(60);
                        }
                    }

                }

                catch (Exception e)
                {

                    //System.IO.Directory.CreateDirectory(System.Environment.CurrentDirectory + "\\logs");
                    //System.IO.File.AppendAllText(System.Environment.CurrentDirectory + "\\logs" + "\\DartError.txt", "\n" + e.ToString());
                    //System.Threading.Thread.Sleep(10);

                }

            }

        }


        public void GetPriceFromConsole(Common.Nozzle nz)
        {
            ConsolePrices.Add(new Tuple<int, int, int>(nz.ParentFuelPoint.Address, nz.Index, nz.UntiPriceInt));
        }


        #region GetDisplay
        private Common.Nozzle evalDisplay(Common.Nozzle nozzle, byte[] response)
        {
            try
            {

                //    0  1  2  3  4   5  6  7   8   9 10 11  12 13 14 15
                //rx 50 37 02 08 [00 00 00 87] [00 00 01 00] 56 30 03 FA (0.87e) (1.00ltr)
                if (response[2] == 0x02 && response.Length == 16 || response.Length == 19 || response.Length == 25)
                {
                    string[] parms = BitConverter.ToString(response).Split('-');

                    string upString = parms[8] + parms[9] + parms[10] + parms[11];
                    string volString = parms[4] + parms[5] + parms[6] + parms[7];

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

        #region Authorise

        public bool AuthorizeFuelPoint(Common.FuelPoint fp)
        {

            try
            {
                ConsolePrices.Clear();
                System.Threading.Thread.Sleep(10);
                foreach (Common.Nozzle nz in fp.Nozzles)
                {
                    GetPriceFromConsole(nz);
                }

                int PumpAddress = fp.Address;
                int ActiveNoz = (int)fp.GetExtendedProperty("CurrentNozzle");


                byte[] buffer = PreAuthorisePrice(fp.Address, ActiveNoz);

                byte[] buffer2 = AuthoriseCMD(fp.Address, ActiveNoz);


                this.serialPort.Write(buffer, 0, buffer.Length);
                if (System.IO.File.Exists("Dart.log"))
                {
                    System.IO.File.AppendAllText("Dart.log", "Authorise\t" + DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " TX: " + BitConverter.ToString(buffer) + "\r\n");
                }
                int waiting = 0;
                while (this.serialPort.BytesToRead < (int)responseLength.RecievedOK && waiting < 300)
                {
                    System.Threading.Thread.Sleep(10);
                    waiting += 20;
                }

                byte[] response = new byte[this.serialPort.BytesToRead];
                this.serialPort.Read(response, 0, this.serialPort.BytesToRead);
                if (System.IO.File.Exists("Dart.log"))
                {
                    System.IO.File.AppendAllText("Dart.log", "Authorise\t" + DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " RX: " + BitConverter.ToString(response) + "\r\n");
                }
                System.Threading.Thread.Sleep(10);
                this.serialPort.Write(buffer2, 0, buffer2.Length);
                if (System.IO.File.Exists("Dart.log"))
                {
                    System.IO.File.AppendAllText("Dart.log", "Authorise\t" + DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " TX: " + BitConverter.ToString(buffer2) + "\r\n");
                }
                while (this.serialPort.BytesToRead < (int)responseLength.RecievedOK && waiting < 300)
                {
                    System.Threading.Thread.Sleep(10);
                    waiting += 20;
                }

                byte[] responseb = new byte[this.serialPort.BytesToRead];
                this.serialPort.Read(responseb, 0, responseb.Length);
                if (System.IO.File.Exists("Dart.log"))
                {
                    System.IO.File.AppendAllText("Dart.log", "Authorise\t" + DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " RX: " + BitConverter.ToString(responseb) + "\r\n");
                }

            }
            catch (Exception ex)
            {

            }
            return true;

        }

        int PriceAA, PriceBB, PriceCC;
        string DataForCRC;
        byte[] PriceCom;

        public byte[] PreAuthorisePrice(int FuelPoint, int NozzleID)
        {
            int count = 0;
            foreach (var listaTimwn2 in ConsolePrices)
            {
                if (listaTimwn2.Item1.Equals(FuelPoint))
                {
                    count += 1;
                }
            }
            int checkcount = count;
            if (count == 1)
            {
                int nozzle1 = count - 1;
                foreach (var nozzlesPr in ConsolePrices)
                {
                    if (nozzlesPr.Item2.Equals(nozzle1))
                        PriceA = nozzlesPr.Item3;
                }
                string PRA = PriceA.ToString();
                if (PRA.Length == 1) { PRA = "000" + PRA; }
                if (PRA.Length == 2) { PRA = "00" + PRA; }
                if (PRA.Length == 3) { PRA = "0" + PRA; }
                DataForCRC = (49 + FuelPoint).ToString() + "30050" + (count * 3).ToString() + "00" + PRA + "02010" + (NozzleID + 1) + "010105";
                priceAA = CRC.ConvertHexStringToByteArray(PRA);
            }
            if (count == 2)
            {
                int nozzle1 = count - 1;
                int nozzle2 = count - 2;
                foreach (var nozzlesPr in ConsolePrices)
                {
                    if (nozzlesPr.Item2.Equals(nozzle1))
                        PriceA = nozzlesPr.Item3;
                    else
                        PriceB = nozzlesPr.Item3;
                }
                string PRA = PriceA.ToString();
                string PRB = PriceB.ToString();
                if (PRA.Length == 1) { PRA = "000" + PRA; }
                if (PRA.Length == 2) { PRA = "00" + PRA; }
                if (PRA.Length == 3) { PRA = "0" + PRA; }

                if (PRB.Length == 1) { PRB = "000" + PRB; }
                if (PRB.Length == 2) { PRB = "00" + PRB; }
                if (PRB.Length == 3) { PRB = "0" + PRB; }
                DataForCRC = (49 + FuelPoint).ToString() + "30050" + (count * 3).ToString() + "00" + PRA + "00" + PRB + "02010" + (NozzleID + 1) + "010105";
                priceAA = CRC.ConvertHexStringToByteArray(PRA);
                priceBB = CRC.ConvertHexStringToByteArray(PRB);
            }
            if (count == 3)
            {
                int nozzle1 = count - 2;
                int nozzle2 = count - 1;
                int nozzle3 = count - 3;
                foreach (var nozzlesPr in ConsolePrices)
                {
                    if (nozzlesPr.Item2.Equals(nozzle1))
                        PriceA = nozzlesPr.Item3;
                    else if (nozzlesPr.Item2.Equals(nozzle2))
                        PriceB = nozzlesPr.Item3;
                    else
                        PriceC = nozzlesPr.Item3;
                }
                string PRA = PriceA.ToString();
                string PRB = PriceB.ToString();
                string PRC = PriceC.ToString();
                if (PRA.Length == 1) { PRA = "000" + PRA; }
                if (PRA.Length == 2) { PRA = "00" + PRA; }
                if (PRA.Length == 3) { PRA = "0" + PRA; }

                if (PRB.Length == 1) { PRB = "000" + PRB; }
                if (PRB.Length == 2) { PRB = "00" + PRB; }
                if (PRB.Length == 3) { PRB = "0" + PRB; }

                if (PRC.Length == 1) { PRC = "000" + PRC; }
                if (PRC.Length == 2) { PRC = "00" + PRC; }
                if (PRC.Length == 3) { PRC = "0" + PRC; }
                DataForCRC = (49 + FuelPoint).ToString() + "30050" + (count * 3).ToString() + "00" + PRA + "00" + PRB + "00" + PRC + "02010" + (NozzleID + 1) + "010105";
                priceAA = CRC.ConvertHexStringToByteArray(PRA);
                priceBB = CRC.ConvertHexStringToByteArray(PRB);
                priceCC = CRC.ConvertHexStringToByteArray(PRC);
            }


            var bytes = CRC.HexToBytes(DataForCRC);
            string hex = CRC.ComputeChecksum(bytes).ToString("x2");
            byte NozzleIdAddress = BitConverter.GetBytes(79 + FuelPoint)[0];
            if (hex.Length == 1) { hex = "000" + hex; }
            if (hex.Length == 2) { hex = "00" + hex; }
            if (hex.Length == 3) { hex = "0" + hex; }
            byte[] crc = CRC.ConvertHexStringToByteArray(hex);
            if (count == 1)
                PriceCom = new byte[] { NozzleIdAddress, 0x30, 0x05, (byte)(count * 3), 0x00, priceAA[0], priceAA[1], 0x02, 0x01, (byte)(NozzleID + 1), 0x01, 0x01, 0x05, crc[1], crc[0], 0x03, 0xFA };
            if (count == 2)
                PriceCom = new byte[] { NozzleIdAddress, 0x30, 0x05, (byte)(count * 3), 0x00, priceAA[0], priceAA[1], 0x00, priceBB[0], priceBB[1], 0x02, 0x01, (byte)(NozzleID + 1), 0x01, 0x01, 0x05, crc[1], crc[0], 0x03, 0xFA };
            if (count == 3)
                PriceCom = new byte[] { NozzleIdAddress, 0x30, 0x05, (byte)(count * 3), 0x00, priceAA[0], priceAA[1], 0x00, priceBB[0], priceBB[1], 0x00, priceCC[0], priceCC[1], 0x02, 0x01, (byte)(NozzleID + 1), 0x01, 0x01, 0x05, crc[1], crc[0], 0x03, 0xFA };

            return PriceCom;
        }

        public byte[] AuthoriseCMD(int FuelPoint, int NozzleID)
        {

            string input = (49 + FuelPoint).ToString() + "3004040099999902010" + (NozzleID + 1).ToString() + "010106";
            var bytes = CRC.HexToBytes(input);
            string hex = CRC.ComputeChecksum(bytes).ToString("x2");
            if (hex.Length == 1) { hex = "000" + hex; }
            if (hex.Length == 2) { hex = "00" + hex; }
            if (hex.Length == 3) { hex = "0" + hex; }
            byte[] crc = CRC.ConvertHexStringToByteArray(hex);
            byte NozzleIdAddress = BitConverter.GetBytes(79 + FuelPoint)[0];
            byte[] send = new byte[] { NozzleIdAddress, 0x30, 0x04, 0x04, 0x00, 0x99, 0x99, 0x99, 0x02, 0x01, (byte)(NozzleID + 1), 0x01, 0x01, 0x06, crc[1], crc[0], 0x03, 0xFA };

            return send;
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

        //public static byte[] GetNozzleCMD(int NozzleID)
        //{
        //    //50 30 01 01 00 9F 5C 03 FA
        //    string input = (49 + NozzleID).ToString() + "30010100";
        //    var bytes = CRC.HexToBytes(input);
        //    string hex = CRC.ComputeChecksum(bytes).ToString("x2");
        //    if (hex.Length == 1) { hex = "000" + hex; }
        //    if (hex.Length == 2) { hex = "00" + hex; }
        //    if (hex.Length == 3) { hex = "0" + hex; }
        //    byte[] crc = CRC.ConvertHexStringToByteArray(hex);
        //    byte NozzleIdAddress = BitConverter.GetBytes(79 + NozzleID)[0];
        //    byte[] send = new byte[] { NozzleIdAddress, 0x30, 0x01, 0x01, 0x00, crc[1], crc[0], 0x03, 0xFA };

        //    return send;
        //}


        //public void GetNozzle(Common.FuelPoint fp)
        //{
        //    //this.serialPort.DiscardInBuffer();
        //    byte[] numArray = DartProtocol.GetNozzleCMD(fp.Address);
        //    this.serialPort.Write(numArray, 0, (int)numArray.Length);



        //    DartProtocol.Logger("EvalStatus", BitConverter.ToString(numArray), "Transmit Get Nozzle");
        //    Trace.WriteLine(BitConverter.ToString(numArray) + " <-- Transmit  | " + DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss.fff"));
        //    int num = 0;
        //    while (true)
        //    {
        //        if ((this.serialPort.BytesToRead >= 24 ? true : num >= 300))
        //        {
        //            break;
        //        }
        //        System.Threading.Thread.Sleep(10);
        //        num = num + 20;
        //    }
        //    byte[] numArray1 = new byte[this.serialPort.BytesToRead];
        //    this.serialPort.Read(numArray1, 0, this.serialPort.BytesToRead);
        //    byte[] getst = DartProtocol.StatusCommand(fp.Address);
        //    this.serialPort.Write(getst, 0, (int)getst.Length);
        //    while (true)
        //    {
        //        if ((this.serialPort.BytesToRead >= 24 ? true : num >= 300))
        //        {
        //            break;
        //        }
        //        System.Threading.Thread.Sleep(10);
        //        num = num + 20;
        //    }
        //    byte[] final = new byte[this.serialPort.BytesToRead];
        //    this.serialPort.Read(final, 0, this.serialPort.BytesToRead);
        //    this.evaluateStatus(fp, final);

        //}
        public void GetStatus(Common.FuelPoint fp)
        {

            byte[] numArray = StatusCommand(fp.Address);
            this.serialPort.Write(numArray, 0, (int)numArray.Length);

            if (System.IO.File.Exists("Dart.log"))
            {
                System.IO.File.AppendAllText("Dart.log", "GetStatus\t" + DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " TX: " + BitConverter.ToString(numArray) + "\r\n");
            }
            int num = 0;
            while (true)
            {
                if ((this.serialPort.BytesToRead >= 25 ? true : num >= 300))
                {
                    break;
                }
                System.Threading.Thread.Sleep(10);
                num = num + 20;
            }
            byte[] numArray1 = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(numArray1, 0, this.serialPort.BytesToRead);
            if (System.IO.File.Exists("Dart.log"))
            {
                System.IO.File.AppendAllText("Dart.log", "GetStatus\t" + DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " RX: " + BitConverter.ToString(numArray1) + "\r\n");
            }
            this.evaluateStatus(fp, numArray1);

        }

        int extendedProperty;
        private void evaluateStatus(Common.FuelPoint fp, byte[] response)
        {
            if (response.Length == 0 && DateTime.Now.Subtract(fp.LastValidResponse).TotalSeconds > 5)
            {
                if (this.DispenserOffline != null)
                    this.DispenserOffline(fp, new EventArgs());
                return;
            }
            if (response.Length == 0)
                return;
            fp.LastValidResponse = DateTime.Now;
            //Logger("EvalStatus", BitConverter.ToString(response), "TX");
            FuelPointValues fuelPointValue = new FuelPointValues();
            FuelPointStatusEnum oldStatus = fp.Status;
            FuelPointStatusEnum newStatus = fp.Status;
            int currentNozzle = (int)fp.GetExtendedProperty("CurrentNozzle", -1);



            if ((int)response.Length <= 3)
            {
                if (fp.Status == FuelPointStatusEnum.Offline)
                {
                    newStatus = FuelPointStatusEnum.Idle;
                    fp.SetExtendedProperty("CurrentNozzle", -1);
                }
            }
            else
            {
                if (response.Length == 25 && response[17] == 0x00 && response[20] == 0x05)
                {
                    this.evalDisplay(fp.ActiveNozzle, response);
                    newStatus = FuelPointStatusEnum.Idle;
                    //if (currentNozzle >= 0)
                    //    fp.Nozzles[currentNozzle].QueryTotals = true;

                    this.extendedProperty = -1;
                    fp.SetExtendedProperty("CurrentNozzle", -1);
                }

                byte num = response[2];

                #region IDLE && NOZZLE && WORK STATUS WITHOUT TRANSACTION COMPLETED COM
                if (num == 0x03)
                {
                    byte num1 = response[7];

                    if (num1 == 0x11 || num1 == 0x12 || num1 == 0x13)
                    {
                        newStatus = FuelPointStatusEnum.Nozzle;
                        if (num1 == 0x11)
                        {
                            this.extendedProperty = 0;
                        }
                        if (num1 == 0x12)
                        {
                            this.extendedProperty = 1;
                        }
                        if (num1 == 0x13)
                        {
                            this.extendedProperty = 2;
                        }
                        fp.SetExtendedProperty("CurrentNozzle", this.extendedProperty);
                    }
                    if (num1 == 0x00 && fp.Status == FuelPointStatusEnum.Nozzle)
                    {
                        this.HaltSend(fp);
                        newStatus = FuelPointStatusEnum.Idle;
                        this.extendedProperty = -1;
                        fp.SetExtendedProperty("CurrentNozzle", -1);
                    }
                    if (num1 == 0x00 && fp.Status == FuelPointStatusEnum.Work)
                    {
                        this.evalDisplay(fp.ActiveNozzle, response);
                        newStatus = FuelPointStatusEnum.Idle;
                        //if (currentNozzle >= 0)
                        //    fp.Nozzles[currentNozzle].QueryTotals = true;
                    }
                }
                #endregion

                #region Display and CloseTransaction
                if (num == 0x02)
                {
                    //Manual TransactionCompleted
                    // 0  1  2  3  4  5  6  7  8  9 10 11 12 13 14 15 16 17 18 19 20 21 22 23 24
                    //50-32-02-08-00-00-00-31-00-00-00-51-03-04-00-16-45-00-01-01-05-B3-85-03-FA      Length 25
                    //53-30-02-08-00-00-03-21-00-00-00-50-03-04-00-15-59-00-01-01-05-2B-B1-03-FA


                    if (response.Length == 25 && fp.Status == FuelPointStatusEnum.Nozzle)
                    {
                        newStatus = FuelPointStatusEnum.Work;
                        this.evalDisplay(fp.ActiveNozzle, response);
                    }
                    //Preset TransactionCompleted
                    //51 33 02 08 00 00 01 31 00 00 02 00 01 01 06 22 48 03 FA       Length= 19
                    if (fp.Status == FuelPointStatusEnum.Work && response[14] == 0x06 && response.Length == 19)
                    {
                        this.evalDisplay(fp.ActiveNozzle, response);
                        newStatus = FuelPointStatusEnum.Idle;
                        //if (currentNozzle >= 0)
                        //    fp.Nozzles[currentNozzle].QueryTotals = true;
                    }

                    //if (response.Length == 25 && response[2] == 0x02 && response[17] != 0x00 && fp.Status != FuelPointStatusEnum.Work)
                    //{
                    //   // fp.Status = FuelPointStatusEnum.Work;
                    //    this.evalDisplay(fp.ActiveNozzle, response);
                    //}

                    //if (response[2] == 0x02)
                    //{
                    //    fp.Status = FuelPointStatusEnum.Work;
                    //}


                    if (response[2] == 0x02 && response.Length == 16)
                    {
                        newStatus = FuelPointStatusEnum.Work;
                        this.evalDisplay(fp.ActiveNozzle, response);
                    }


                }
                #endregion
            }

            fp.Status = newStatus;
            fp.DispenserStatus = fp.Status;
        }

        #endregion

        #region GetTotals
        public static byte[] requestTotals(int NozzleID, int FuelPoint)
        {
            string input = (49 + NozzleID).ToString() + "3065010" + FuelPoint.ToString();
            var bytes = CRC.HexToBytes(input);
            string hex = CRC.ComputeChecksum(bytes).ToString("x2");
            byte NozzleIdAddress = BitConverter.GetBytes(79 + NozzleID)[0];
            byte[] crc = CRC.ConvertHexStringToByteArray(hex);
            byte[] send = new byte[] { NozzleIdAddress, 0x30, 0x65, 0x01, (byte)FuelPoint, crc[1], crc[0], 0x03, 0xFA };
            return send;
        }
        public bool GetTotals(Common.Nozzle nozzle)
        {
            int waiting = 0;


            byte[] Poll = StatusCommand(nozzle.ParentFuelPoint.Address);
            /*********************Send Poll************/
            this.serialPort.Write(Poll, 0, Poll.Length);
            if (System.IO.File.Exists("Dart.log"))
            {
                System.IO.File.AppendAllText("Dart.log", "GetTotals\t" + DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " TX: " + BitConverter.ToString(Poll) + "\r\n");
            }
            while (this.serialPort.BytesToRead < (int)responseLength.RecievedOK && waiting < 300)
            {
                System.Threading.Thread.Sleep(20);
                waiting += 20;
            }
            //Response If OK -->RX 50 70 FA
            byte[] responsePoll = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(responsePoll, 0, this.serialPort.BytesToRead);
            if (System.IO.File.Exists("Dart.log"))
            {
                System.IO.File.AppendAllText("Dart.log", "GetTotals\t" + DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " RX: " + BitConverter.ToString(responsePoll) + "\r\n");
            }



            System.Threading.Thread.Sleep(25);



            /**********************Send Recieve Totals***************************************/

            //TX 52 30 65 01 01 66 83 03 FA
            byte[] upBuffer = requestTotals(nozzle.ParentFuelPoint.Address, nozzle.Index);
            this.serialPort.Write(upBuffer, 0, upBuffer.Length);
            if (System.IO.File.Exists("Dart.log"))
            {
                System.IO.File.AppendAllText("Dart.log", "GetTotals\t" + DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " TX: " + BitConverter.ToString(upBuffer) + "\r\n");
            }

            while (this.serialPort.BytesToRead < (int)responseLength.RecievedOK && waiting < 300)
            {
                System.Threading.Thread.Sleep(20); //15*4 = 70 [ NP 68ms response time]
                waiting += 20;
            }


            //Response If OK -->RX 50 C0 FA
            byte[] responseOK = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(responseOK, 0, this.serialPort.BytesToRead);
            if (System.IO.File.Exists("Dart.log"))
            {
                System.IO.File.AppendAllText("Dart.log", "GetTotals\t" + DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " RX: " + BitConverter.ToString(responseOK) + "\r\n");
            }

            System.Threading.Thread.Sleep(25);

            /**************************Receive Totals With Poll************************/

            byte[] CMDPoll = StatusCommand(nozzle.ParentFuelPoint.Address);
            this.serialPort.Write(CMDPoll, 0, CMDPoll.Length);
            if (System.IO.File.Exists("Dart.log"))
            {
                System.IO.File.AppendAllText("Dart.log", "GetTotals\t" + DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " TX: " + BitConverter.ToString(CMDPoll) + "\r\n");
            }
            while (this.serialPort.BytesToRead < (int)responseLength.totals && waiting < 300)
            {
                System.Threading.Thread.Sleep(20);
                waiting += 20;
            }

            //Recieve Totals
            //Sample 53 30 65 10 02 [00 00 66 64 16] [00-00-66-64-16]-00-00-00-00-00-[57-4F]-[03-FA]
            byte[] response = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(response, 0, this.serialPort.BytesToRead);
            if (System.IO.File.Exists("Dart.log"))
            {
                System.IO.File.AppendAllText("Dart.log", "GetTotals\t" + DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " RX: " + BitConverter.ToString(response) + "\r\n");
            }

            if (response.Length < 24)
                return false;
            else if (response.Length > 24)
                return false;
            else
                return evalTotals(nozzle, response);


        }

        private bool evalTotals(Common.Nozzle nozzle, byte[] response)
        {
            //    0  1  2  3  4   5  6  7  8  9   10 11 12 13 14  15 16 17 18 19 20 21 22 23
            //RX 50 31 65 10 01 [00 00 28 12 49] [00 00 28 12 49] 00 00 00 00 00 60 89 03 FA  // Totals
            //Nozzle 1 Volume Total : 00002812,49 

            if (response.Length == 24)
            {
                string[] Vtotal = BitConverter.ToString(response).Split('-');
                string volume = Vtotal[5] + Vtotal[6] + Vtotal[7] + Vtotal[8] + Vtotal[9];


                nozzle.TotalVolume = decimal.Parse(volume);
                //  50 < 20

                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion

        #region SetPrices


        string PriceBufferNEw;
        byte[] CommandPriceFin;

        int PriceA = 0000, PriceB = 0000, PriceC = 0000;
        byte[] priceAA, priceBB, priceCC;
        public byte[] SetPriceCMD(int FuelPoint)
        {


            int count = 0;
            foreach (var listaTimwn in ConsolePrices)
            {

                if (listaTimwn.Item1.Equals(FuelPoint))
                {
                    count += 1;
                }

            }
            if (count == 1)
            {
                int nozzle1 = count - 1;
                foreach (var nozzlesPr in ConsolePrices)
                {

                    if (nozzlesPr.Item2.Equals(nozzle1))
                        PriceA = nozzlesPr.Item3;
                }
                string PRA = PriceA.ToString();
                if (PRA.Length == 1) { PRA = "000" + PRA; }
                if (PRA.Length == 2) { PRA = "00" + PRA; }
                if (PRA.Length == 3) { PRA = "0" + PRA; }
                PriceBufferNEw = (49 + FuelPoint).ToString() + "30010105050" + (count * 3).ToString() + "00" + PRA;
                priceAA = CRC.ConvertHexStringToByteArray(PriceA.ToString());
            }
            if (count == 2)
            {
                int nozzle1 = count - 1;
                int nozzle2 = count - 2;
                foreach (var nozzlesPr in ConsolePrices)
                {

                    if (nozzlesPr.Item2.Equals(nozzle1))
                        PriceA = nozzlesPr.Item3;
                    else
                        PriceB = nozzlesPr.Item3;
                }
                string PRA = PriceA.ToString();
                string PRB = PriceB.ToString();
                if (PRA.Length == 1) { PRA = "000" + PRA; }
                if (PRA.Length == 2) { PRA = "00" + PRA; }
                if (PRA.Length == 3) { PRA = "0" + PRA; }

                if (PRB.Length == 1) { PRB = "000" + PRB; }
                if (PRB.Length == 2) { PRB = "00" + PRB; }
                if (PRB.Length == 3) { PRB = "0" + PRB; }
                PriceBufferNEw = (49 + FuelPoint).ToString() + "30010105050" + (count * 3).ToString() + "00" + PRA + "00" + PRB;

                priceAA = CRC.ConvertHexStringToByteArray(PRA);
                priceBB = CRC.ConvertHexStringToByteArray(PRB);
            }
            if (count == 3)
            {
                int nozzle1 = count - 2;
                int nozzle2 = count - 1;
                int nozzle3 = count - 3;
                foreach (var nozzlesPr in ConsolePrices)
                {
                    if (nozzlesPr.Item2.Equals(nozzle1))
                        PriceA = nozzlesPr.Item3;
                    else if (nozzlesPr.Item2.Equals(nozzle2))
                        PriceB = nozzlesPr.Item3;
                    else
                        PriceC = nozzlesPr.Item3;
                }
                string PRA = PriceA.ToString();
                string PRB = PriceB.ToString();
                string PRC = PriceC.ToString();
                if (PRA.Length == 1) { PRA = "000" + PRA; }
                if (PRA.Length == 2) { PRA = "00" + PRA; }
                if (PRA.Length == 3) { PRA = "0" + PRA; }

                if (PRB.Length == 1) { PRB = "000" + PRB; }
                if (PRB.Length == 2) { PRB = "00" + PRB; }
                if (PRB.Length == 3) { PRB = "0" + PRB; }

                if (PRC.Length == 1) { PRC = "000" + PRC; }
                if (PRC.Length == 2) { PRC = "00" + PRC; }
                if (PRC.Length == 3) { PRC = "0" + PRC; }

                PriceBufferNEw = (49 + FuelPoint).ToString() + "30010105050" + (count * 3).ToString() + "00" + PRA + "00" + PRB + "00" + PRC;

                priceAA = CRC.ConvertHexStringToByteArray(PRA);
                priceBB = CRC.ConvertHexStringToByteArray(PRB);
                priceCC = CRC.ConvertHexStringToByteArray(PRC);
            }

            var bytes = CRC.HexToBytes(PriceBufferNEw);
            string hex = CRC.ComputeChecksum(bytes).ToString("x2");
            byte NozzleIdAddress = BitConverter.GetBytes(79 + FuelPoint)[0];
            if (hex.Length == 1) { hex = "000" + hex; }
            if (hex.Length == 2) { hex = "00" + hex; }
            if (hex.Length == 3) { hex = "0" + hex; }
            byte[] crc = CRC.ConvertHexStringToByteArray(hex);

            if (count == 1)
                CommandPriceFin = new byte[] { NozzleIdAddress, 0x30, 0x01, 0x01, 0x05, 0x05, (byte)(count * 3), 0x00, priceAA[0], priceAA[1], crc[1], crc[0], 0x03, 0xFA };
            if (count == 2)
                CommandPriceFin = new byte[] { NozzleIdAddress, 0x30, 0x01, 0x01, 0x05, 0x05, (byte)(count * 3), 0x00, priceAA[0], priceAA[1], 0x00, priceBB[0], priceBB[1], crc[1], crc[0], 0x03, 0xFA };
            if (count == 3)
                CommandPriceFin = new byte[] { NozzleIdAddress, 0x30, 0x01, 0x01, 0x05, 0x05, (byte)(count * 3), 0x00, priceAA[0], priceAA[1], 0x00, priceBB[0], priceBB[1], 0x00, priceCC[0], priceCC[1], crc[1], crc[0], 0x03, 0xFA };

            return CommandPriceFin;

        }
        public bool SetPrice(Common.Nozzle nozzle, int unitPrice)
        {
            try
            {
                GetPrices = true;
                int waiting = 0;
                byte[] Poll = StatusCommand(nozzle.ParentFuelPoint.Address);
                this.serialPort.Write(Poll, 0, Poll.Length);
                if (System.IO.File.Exists("Dart.log"))
                {
                    System.IO.File.AppendAllText("Dart.log", "SetPrice \t" + DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " TX: " + BitConverter.ToString(Poll) + "\r\n");
                }
                while (this.serialPort.BytesToRead < (int)responseLength.RecievedOK && waiting < 300)
                {
                    System.Threading.Thread.Sleep(15);
                    waiting += 20;
                }
                byte[] responsePoll = new byte[this.serialPort.BytesToRead];
                this.serialPort.Read(responsePoll, 0, this.serialPort.BytesToRead);
                if (System.IO.File.Exists("Dart.log"))
                {
                    System.IO.File.AppendAllText("Dart.log", "SetPrice \t" + DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " RX: " + BitConverter.ToString(responsePoll) + "\r\n");
                }
                System.Threading.Thread.Sleep(20);
                byte[] buffer = SetPriceCMD(nozzle.ParentFuelPoint.Address);
                this.serialPort.Write(buffer, 0, buffer.Length);

                if (System.IO.File.Exists("Dart.log"))
                {
                    System.IO.File.AppendAllText("Dart.log", "SetPrice \t" + DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " TX: " + BitConverter.ToString(buffer) + "\r\n");
                }
                while (this.serialPort.BytesToRead < (int)responseLength.RecievedOK && waiting < 300)
                {
                    System.Threading.Thread.Sleep(20);
                    waiting += 20;
                }

                byte[] response = new byte[this.serialPort.BytesToRead];
                this.serialPort.Read(response, 0, this.serialPort.BytesToRead);
                if (System.IO.File.Exists("Dart.log"))
                {
                    System.IO.File.AppendAllText("Dart.log", "SetPrice \t" + DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " RX: " + BitConverter.ToString(response) + "\r\n");
                }
            }

            catch (Exception ex)
            {
                //  Logger("SetPrices", ex.ToString(), "Error - >Exception SetPrices");
            }

            return true;

        }

        #endregion

        #region HaltCommand

        public static byte[] Halt(int FuelPoint)
        {
            //[50] 30 01 01 08 [9E 9A] [03 FA]
            string input = (49 + FuelPoint).ToString() + "30010108";
            var bytes = CRC.HexToBytes(input);
            string hex = CRC.ComputeChecksum(bytes).ToString("x2");
            string crcA = hex.Substring(2, 2);
            string crcB = hex.Substring(0, 2);
            if (hex.Length == 1) { hex = "000" + hex; }
            if (hex.Length == 2) { hex = "00" + hex; }
            if (hex.Length == 3) { hex = "0" + hex; }
            byte[] crc = CRC.ConvertHexStringToByteArray(hex);
            byte NozzleIdAddress = BitConverter.GetBytes(79 + FuelPoint)[0];
            byte[] send = new byte[] { NozzleIdAddress, 0x30, 0x01, 0x01, 0x08, crc[1], crc[0], 0x03, 0xFA };
            return send;
        }

        public bool HaltSend(Common.FuelPoint fp)
        {
            int waiting = 0;
            byte[] HaltCMD = Halt(fp.Address);
            this.serialPort.Write(HaltCMD, 0, HaltCMD.Length);
            if (System.IO.File.Exists("Dart.log"))
            {
                System.IO.File.AppendAllText("Dart.log", "Halt     \t" + DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " TX: " + BitConverter.ToString(HaltCMD) + "\r\n");
            }
            while (this.serialPort.BytesToRead < (int)responseLength.RecievedOK && waiting < 300)
            {
                System.Threading.Thread.Sleep(15);
                waiting += 20;
            }
            byte[] responsePoll = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(responsePoll, 0, this.serialPort.BytesToRead);
            if (System.IO.File.Exists("Dart.log"))
            {
                System.IO.File.AppendAllText("Dart.log", "Halt     \t" + DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " RX: " + BitConverter.ToString(responsePoll) + "\r\n");
            }
            if (responsePoll.Length == 3 && responsePoll[1] == 0xC0)
                return true;
            else
                return false;

        }
        #endregion

        #region Tools

        public static void Logger(string FileNameToSave, string Error_Recieve, string VoidMethodName)
        {
            string fileName = "Logs/WayneDart_" + FileNameToSave + "_LOG.txt";
            using (StreamWriter writer = new StreamWriter(fileName, true, Encoding.UTF8))
            {
                writer.Write("-->" + VoidMethodName + "  " + DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss.fff") + "   <--- \r\n\r\n" + Error_Recieve.ToString() + "\r\n\r\n");
            }
        }

        public byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        private enum responseLength
        {
            totals = 24,
            display = 16,
            status = 12,
            RecievedOK = 3,
            TransactionCompleted = 25
        }

        private struct IPumpDebugArgs
        {
            public Common.Enumerators.FuelPointStatusEnum status;
            public decimal totalizer;
            public decimal volume;
            public decimal amount;
            public List<byte[]> comBuffer;

        }

        //Pump Status [2] byte = 0x01
        public enum PumpStatus
        {
            Ready = 0x02,
            Filling = 0x04,
            TransactionCompleted = 0x05,
        }

        #endregion

        #region CRC Calculator
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

        #endregion
    }
}
