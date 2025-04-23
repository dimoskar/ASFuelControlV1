using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ASFuelControl.Common;
using ASFuelControl.Common.Enumerators;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Diagnostics;


namespace ASFuelControl.PetrotecCEM01
{
    public class Commands
    {
        #region Commands
        private static byte StartCom = 58;
        private static byte[] EndCom = new byte[] { 13, 10 };
        public static byte[] SetPrice(int NozzleID, int UnitPrice)
        {
            byte[] PriceArray = Encoding.ASCII.GetBytes(UnitPrice.ToString("D4"));
            List<byte> CMD = new List<byte>();
            CMD.AddRange(new byte[] { 89, (byte)(48 + NozzleID) });
            CMD.AddRange(PriceArray);
            byte[] LRCArray = Encoding.ASCII.GetBytes(LRC(CMD.ToArray()).ToString("X2"));
            List<byte> buffer = new List<byte>();
            buffer.Add(StartCom);
            buffer.AddRange(CMD.ToArray());
            buffer.AddRange(LRCArray);
            buffer.AddRange(EndCom);
            return buffer.ToArray();
        }
        public static byte[] Authorise(int NozzleID, int UnitPrice)
        {
            byte[] PriceArray = Encoding.ASCII.GetBytes(UnitPrice.ToString("D4"));
            byte[] command = new byte[] { 116, (byte)(48 + NozzleID) };
            List<byte> CMD = new List<byte>();
            CMD.AddRange(command);
            CMD.AddRange(PriceArray);
            CMD.AddRange(new byte[] { 0x20, (byte)(PresetAuthorise.Amount + 30), 0x39, 0x39, 0x39, 0x39, 0x39, 0x39 });
            List<byte> buffer = new List<byte>();
            buffer.Add(StartCom);
            buffer.AddRange(CMD);
            buffer.AddRange(Encoding.ASCII.GetBytes(LRC(CMD.ToArray()).ToString("X2")));
            buffer.AddRange(EndCom);
            return buffer.ToArray();
        }
        public static byte[] AuthorisePreset(int NozzleID, int UnitPrice)
        {
            byte[] PriceArray = Encoding.ASCII.GetBytes(UnitPrice.ToString("D4"));
            byte[] command = new byte[] { 116, (byte)(48 + NozzleID) };
            List<byte> CMD = new List<byte>();
            CMD.AddRange(command);
            CMD.AddRange(PriceArray);
            CMD.AddRange(new byte[] { 0x20, (byte)(PresetAuthorise.Amount + 30), 0x39, 0x39, 0x39, 0x39, 0x39, 0x39 });
            List<byte> buffer = new List<byte>();
            buffer.Add(StartCom);
            buffer.AddRange(CMD);
            buffer.AddRange(Encoding.ASCII.GetBytes(LRC(CMD.ToArray()).ToString("X2")));
            buffer.AddRange(EndCom);
            return buffer.ToArray();
        }
        public static byte[] VolumeTotal(int NozzleID)
        {
            int TypeCom = 86;
            byte[] CMD = new byte[] { (byte)TypeCom, (byte)(48 + NozzleID) };
            byte[] LRCArray = Encoding.ASCII.GetBytes(LRC(CMD).ToString("X2"));
            List<byte> buffer = new List<byte>();
            buffer.Add(StartCom);
            buffer.AddRange(CMD);
            buffer.AddRange(LRCArray);
            buffer.AddRange(EndCom);
            return buffer.ToArray();
        }
        public static byte[] GetDisplay(int NozzleID)
        {
            int TypeCom = 82;
            byte[] CMD = new byte[] { (byte)TypeCom, (byte)(48 + NozzleID) };
            byte[] LRCArray = Encoding.ASCII.GetBytes(LRC(CMD).ToString("X2"));
            List<byte> buffer = new List<byte>();
            buffer.Add(StartCom);
            buffer.AddRange(CMD);
            buffer.AddRange(LRCArray);
            buffer.AddRange(EndCom);
            return buffer.ToArray();
        }
        public static byte[] Halt(int NozzleID)
        {
            int TypeCom = 80;
            byte[] CMD = new byte[] { (byte)TypeCom };
            byte[] LRCArray = Encoding.ASCII.GetBytes(LRC(CMD).ToString("X2"));
            List<byte> buffer = new List<byte>();
            buffer.Add(StartCom);
            buffer.AddRange(CMD);
            buffer.AddRange(LRCArray);
            buffer.AddRange(EndCom);
            return buffer.ToArray();
        }
        public static byte[] PowerONOFF(int NozzleID)
        {
            int TypeCom = 83;
            byte[] CMD = new byte[] { (byte)TypeCom };
            byte[] LRCArray = Encoding.ASCII.GetBytes(LRC(CMD).ToString("X2"));
            List<byte> buffer = new List<byte>();
            buffer.Add(StartCom);
            buffer.AddRange(CMD);
            buffer.AddRange(LRCArray);
            buffer.AddRange(EndCom);
            return buffer.ToArray();
        }
        public static byte[] Status(int NozzleID)
        {
            int TypeCom = 255;
            byte[] CMD = new byte[] { (byte)TypeCom };
            List<byte> buffer = new List<byte>();
            buffer.AddRange(CMD);
            buffer.AddRange(EndCom);
            return buffer.ToArray();
        }
        public static byte[] Acknowledge()
        {
            byte[] buffer = new byte[] { 0x21 };
            return buffer;
        }
        private enum PresetAuthorise
        {
            Volume = 0,
            Amount = 1
        }

        #endregion

        #region CRC
        public static int LRC(byte[] bytes)
        {
            int num = 0;
            for (int i = 0; i < (int)bytes.Length; i++)
            {
                num = num ^ bytes[i];
            }
            return num;
        }
        public static byte[] LRCtoByte(byte[] bytes)
        {
            string str = LRC(bytes).ToString();
            if (str.Length == 1)
            {
                str = string.Concat(str, "0");
            }
            int num = int.Parse(str.Substring(0, 1));
            int num1 = int.Parse(str.Substring(1, 1));
            byte[] numArray = new byte[] { (byte)(48 + num), (byte)(48 + num1) };
            return numArray;
        }
        #endregion
    }


    public class PetrotecProtocol : Common.IFuelProtocol
    {
        #region Basics
        public event EventHandler<Common.FuelPointValuesArgs> DataChanged;
        public event EventHandler<Common.TotalsEventArgs> TotalsRecieved;
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
                this.serialPort.DataBits = 7;
                this.serialPort.StopBits = System.IO.Ports.StopBits.Two;
                this.serialPort.BaudRate = 1200;
                this.serialPort.RtsEnable = true;
                this.serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
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
        public string LogPath()
        {
            string LogPath = "Petrotec_" + this.CommunicationPort + ".log";
            return LogPath;
        }

        #endregion

        #region Workflow Dispenser


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

                            #region Authorise
                            if (fp.QueryAuthorize)
                            {
                                if (this.AuthorizeFuelPoint(fp))
                                    fp.QueryAuthorize = false;
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
                                        while (true)
                                        {
                                            this.GetTotals(nz);
                                            if (this.evalTotals(nz) && TotalBuffer.Length > 14)
                                            {
                                                fp.Initialized = true;
                                                if (this.TotalsRecieved != null)
                                                {
                                                    this.TotalsRecieved(this, new Common.TotalsEventArgs(fp, nz.Index, nz.TotalVolume, nz.TotalPrice));
                                                }

                                            }
                                            break;
                                        }
                                    }
                                }
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
                            #endregion

                        }
                        finally
                        {
                            System.Threading.Thread.Sleep(140);
                        }
                    }
                }
                catch (Exception ErrorThread)
                {
                    Console.WriteLine("THREAD Exception:->    " + ErrorThread.ToString());
                }
            }
        }
        #endregion

        #region BufferCalculator

        public byte[] TotalBuffer;
        public byte[] StatusBuffer = new byte[] { 0x00, 0x00 };

        public bool DispenserPowerOn;
        public bool Fuelpoint_NoneToRecieve = true;
        public byte[] LastDisplay;
        public byte[] LostLastSale;



        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {

                Fuelpoint_NoneToRecieve = false;
                int num = 0;
                SerialPort sp = (SerialPort)sender;
                while (sp.BytesToRead < 21 && num < 300)
                {
                    System.Threading.Thread.Sleep(25);
                    num += 20;
                }
                byte[] buffer = new byte[sp.BytesToRead];
                sp.Read(buffer, 0, sp.BytesToRead);

                if (System.IO.File.Exists(LogPath()))
                {
                    System.IO.File.AppendAllText(LogPath(), DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " RecieveBuffer -> " + System.Text.Encoding.ASCII.GetString(buffer) + "\r\n");
                }

                if (buffer.Length > 2)
                {
                    if (buffer[1] == 0x4F)
                    {
                        DispenserPowerOn = true;
                        byte[] SendAkno = Commands.Acknowledge();
                        this.serialPort.Write(SendAkno, 0, SendAkno.Length);
                        if (System.IO.File.Exists(LogPath()))
                        {
                            System.IO.File.AppendAllText(LogPath(), DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " ResponseBuffer -> " + System.Text.Encoding.ASCII.GetString(SendAkno) + "\r\n");
                        }
                    }
                    //3A-6C-30-30-30-30-32-33-30-30-30-30-33-30-31-32-38-38-36-44-0D-0A
                    if (buffer[1] == 0x6C)
                    {

                        byte[] SendAkno = Commands.Acknowledge();
                        this.serialPort.Write(SendAkno, 0, SendAkno.Length);
                        if (System.IO.File.Exists(LogPath()))
                        {
                            System.IO.File.AppendAllText(LogPath(), DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " ResponseBuffer -> " + System.Text.Encoding.ASCII.GetString(SendAkno) + "\r\n");
                        }
                        LostLastSale = buffer;
                    }
                    if (buffer[1] == 0x57 || buffer[2] == 0x57) //tOTALS
                    {

                        byte[] SendAkno = Commands.Acknowledge();
                        this.serialPort.Write(SendAkno, 0, SendAkno.Length);
                        if (System.IO.File.Exists(LogPath()))
                        {
                            System.IO.File.AppendAllText(LogPath(), DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " ResponseBuffer -> " + System.Text.Encoding.ASCII.GetString(SendAkno) + "\r\n");
                        }
                        TotalBuffer = buffer;
                    }
                    if (buffer[1] == 0x61 || buffer[2] == 0x61)//a -> Nozzle
                    {

                        byte[] SendAkno = Commands.Acknowledge();
                        this.serialPort.Write(SendAkno, 0, SendAkno.Length);
                        if (System.IO.File.Exists(LogPath()))
                        {
                            System.IO.File.AppendAllText(LogPath(), DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " ResponseBuffer -> " + System.Text.Encoding.ASCII.GetString(SendAkno) + "\r\n");
                        }
                        StatusBuffer = buffer;
                    }
                    if (buffer[1] == 0x43 || buffer[2] == 0x43)//C -> Work
                    {

                        byte[] SendAkno = Commands.Acknowledge();
                        this.serialPort.Write(SendAkno, 0, SendAkno.Length);
                        if (System.IO.File.Exists(LogPath()))
                        {
                            System.IO.File.AppendAllText(LogPath(), DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " ResponseBuffer -> " + System.Text.Encoding.ASCII.GetString(SendAkno) + "\r\n");
                        }
                        StatusBuffer = buffer;
                    }
                    if (buffer[1] == 0x44 || buffer[2] == 0x44)//D -> Return to Idle
                    {

                        byte[] SendAkno = Commands.Acknowledge();
                        this.serialPort.Write(SendAkno, 0, SendAkno.Length);
                        if (System.IO.File.Exists(LogPath()))
                        {
                            System.IO.File.AppendAllText(LogPath(), DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " ResponseBuffer -> " + System.Text.Encoding.ASCII.GetString(SendAkno) + "\r\n");
                        }
                        StatusBuffer = buffer;
                    }
                    //!:e00002200002815E
                    if (buffer[1] == 0x65 || buffer[2] == 0x65)//return Display
                    {
                        byte[] SendAkno = Commands.Acknowledge();
                        this.serialPort.Write(SendAkno, 0, SendAkno.Length);
                        if (System.IO.File.Exists(LogPath()))
                        {
                            System.IO.File.AppendAllText(LogPath(), DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " ResponseBuffer -> " + System.Text.Encoding.ASCII.GetString(SendAkno) + "\r\n");
                        }
                        LastDisplay = buffer;
                    }
                }


                Fuelpoint_NoneToRecieve = true;

            }
            catch (Exception ex)
            {
                if (System.IO.File.Exists(LogPath()))
                {
                    System.IO.File.AppendAllText(LogPath(), DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + "\t AllRecieveException -> " + ex.ToString() + "\r\n");
                }
            }
        }
        #endregion

        #region Totals
        public void GetTotals(Common.Nozzle nozzle)
        {

            if (Fuelpoint_NoneToRecieve)
            {
                byte[] cmd = Commands.VolumeTotal(nozzle.ParentFuelPoint.Address);
                this.serialPort.Write(cmd, 0, cmd.Length);
                if (System.IO.File.Exists(LogPath()))
                {
                    System.IO.File.AppendAllText(LogPath(), DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " GetTotals TX -> " + System.Text.Encoding.ASCII.GetString(cmd) + "\r\n");
                }
            }


        }
        private bool evalTotals(Common.Nozzle nozzle)
        {
            //!:W10026145260<CR><LF>
            //:W10026145260<CR><LF>
            byte[] response = TotalBuffer;

            if (response.Length > 12)
            {

                if (response[0] == 0x21 && response[1] == 0x3A && response[2] == 0x57)
                {
                    string[] Vtotal = BitConverter.ToString(response).Split('-');
                    string volume = Vtotal[4].Substring(1)
                                  + Vtotal[5].Substring(1)
                                  + Vtotal[6].Substring(1)
                                  + Vtotal[7].Substring(1)
                                  + Vtotal[8].Substring(1)
                                  + Vtotal[9].Substring(1)
                                  + Vtotal[10].Substring(1)
                                  + Vtotal[11].Substring(1);

                    if (nozzle.TotalVolume == decimal.Parse(volume) && nozzle.ParentFuelPoint.Status == FuelPointStatusEnum.Work)
                    {
                        return false;
                    }
                    else
                    {
                        nozzle.TotalVolume = decimal.Parse(volume);
                        return true;
                    }


                }
                if (response[0] == 0x3A && response[1] == 0x57)
                {
                    string[] Vtotal = BitConverter.ToString(response).Split('-');
                    string volume = Vtotal[3].Substring(1)
                                  + Vtotal[4].Substring(1)
                                  + Vtotal[5].Substring(1)
                                  + Vtotal[6].Substring(1)
                                  + Vtotal[7].Substring(1)
                                  + Vtotal[8].Substring(1)
                                  + Vtotal[9].Substring(1)
                                  + Vtotal[10].Substring(1);

                    if (nozzle.TotalVolume == decimal.Parse(volume) && nozzle.ParentFuelPoint.Status == FuelPointStatusEnum.Work)
                    {
                        return false;
                    }
                    else
                    {
                        nozzle.TotalVolume = decimal.Parse(volume);
                        return true;
                    }
                }
            }
            else
            {
                return false;
            }


            return false;

        }

        #endregion

        #region Authorise

        public bool AuthorizeFuelPoint(Common.FuelPoint fp)
        {

            if (Fuelpoint_NoneToRecieve == true)
            {
                byte[] buffer = Commands.Authorise(fp.Address, fp.ActiveNozzle.UntiPriceInt);
                this.serialPort.Write(buffer, 0, buffer.Length);
                if (System.IO.File.Exists(LogPath()))
                {
                    System.IO.File.AppendAllText(LogPath(), DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " Authorise TX -> " + System.Text.Encoding.ASCII.GetString(buffer) + "\r\n");
                }
            }
            return true;

        }
        #endregion

        #region Display

        string volString, upString;
        private Common.Nozzle evalDisplay(Common.Nozzle nozzle)
        {
            byte[] response = LastDisplay;
            //Display
            // 0  1  2  3  4  5   6  7  8  9 10 11  12  13 14 15 16 17 18 19  20 21
            //:e[000017][000030][1][51]<CR><LF>
            //:e[000030][000039]15D<CR><LF>
            // !  :  e  [0  0  0  0  1  8]  [0  0  0  0  2  3]  [1]  [5  C]
            if (response.Length > 14)
            {
                string[] parms = BitConverter.ToString(response).Split('-');

                if (response[0] == 0x21)
                {
                    volString =
                             parms[3].Substring(1)
                            + parms[4].Substring(1)
                            + parms[5].Substring(1)
                            + parms[6].Substring(1)
                            + parms[7].Substring(1)
                            + parms[8].Substring(1);

                    upString =
                         parms[9].Substring(1)
                        + parms[10].Substring(1)
                        + parms[11].Substring(1)
                        + parms[12].Substring(1)
                        + parms[13].Substring(1)
                        + parms[14].Substring(1);
                }
                if (response[0] == 0x3A)
                {
                    volString =

                             parms[2].Substring(1)
                            + parms[3].Substring(1)
                            + parms[4].Substring(1)
                            + parms[5].Substring(1)
                            + parms[6].Substring(1)
                            + parms[7].Substring(1);

                    upString =

                         parms[8].Substring(1)
                        + parms[9].Substring(1)
                        + parms[10].Substring(1)
                        + parms[11].Substring(1)
                        + parms[12].Substring(1)
                        + parms[13].Substring(1);
                }


                nozzle.ParentFuelPoint.DispensedAmount = decimal.Parse(upString) / 100;
                nozzle.ParentFuelPoint.DispensedVolume = decimal.Parse(volString) / 100;


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
                System.Threading.Thread.Sleep(50);
            }


            return nozzle;

        }
        public void getDisplay(Common.FuelPoint fp)
        {
            if (Fuelpoint_NoneToRecieve)
            {
                byte[] buffer = Commands.GetDisplay(fp.Address);
                this.serialPort.Write(buffer, 0, buffer.Length);
                if (System.IO.File.Exists(LogPath()))
                {
                    System.IO.File.AppendAllText(LogPath(), DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " GetDisplay TX -> " + System.Text.Encoding.ASCII.GetString(buffer) + "\r\n");
                }
            }
        }

        #endregion

        #region Status
        int extendedProperty;
        DateTime LastSale;
        DateTime NowResponse;
        DateTime LastNozzle;
        int ResetStatusInt;

        public void GetStatus(Common.FuelPoint fp)
        {

            evaluateStatus(fp);
            System.Threading.Thread.Sleep(180);
        }
        private void evaluateStatus(Common.FuelPoint fp)
        {
            
            byte[] response = StatusBuffer;
            FuelPointValues fuelPointValue = new FuelPointValues();
            FuelPointStatusEnum oldStatus = fp.Status;
            FuelPointStatusEnum newStatus = fp.Status;
            int currentNozzle = (int)fp.GetExtendedProperty("CurrentNozzle", -1);
            DateTime TimeNowResponse = DateTime.Now;
            if (fp.Status == FuelPointStatusEnum.Idle)
            {
                NowResponse = DateTime.Now;
            }

            //var secondsLastSale = (NowResponse - LastSale).TotalSeconds;

            var minutessLastSale = ((NowResponse - LastSale).Minutes) * 60;
            var secondsLastResponse = (NowResponse - LastSale).Seconds;
            int TotalLastSale = minutessLastSale + secondsLastResponse;
            Trace.WriteLine("TotalLastSale:" + TotalLastSale.ToString());
            //////////////////////////////////////LastNozzle
            //var minutessLastNozzle = ((TimeNowResponse - LastNozzle).Minutes) * 60;
            //var secondsLastNozzleResponse = (TimeNowResponse - LastNozzle).Seconds;
            //int TotalLastNozzle = minutessLastNozzle + secondsLastNozzleResponse;
            //Trace.WriteLine("TotalLastNozzle:" + TotalLastNozzle.ToString());

            if (System.IO.File.Exists(LogPath()) && TotalLastSale < 10)
            {
                System.IO.File.AppendAllText(LogPath(), DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + "TotalMin:" + TotalLastSale.ToString() + "\r\n");
            }

            if (fp.Status == FuelPointStatusEnum.Idle)
            {
                if (TotalLastSale >= 5 && TotalLastSale <= 6)
                {
                    ResetStatus(fp);
                }
            }

            //if (fp.Status == FuelPointStatusEnum.Nozzle)
            //{
            //    if (TotalLastNozzle >12)
            //    {
            //        ResetStatus(fp);
            //    }
            //}

            if (fp.Status == FuelPointStatusEnum.Offline)
            {
                newStatus = FuelPointStatusEnum.Idle;
                ResetStatus(fp);
            }
            if (response.Length > 2)
            {
                //!:a150<CR><LF> 
                //Nozzle
                if (response[1] == 0x61 || response[2] == 0x61)
                {
                    //if(fp.Status == FuelPointStatusEnum.Idle)
                    //{
                    //    LastNozzle = DateTime.Now;
                    //}
                    this.extendedProperty = 0;
                    newStatus = FuelPointStatusEnum.Nozzle;
                    fp.SetExtendedProperty("CurrentNozzle", this.extendedProperty);
                }

                //Nozzle/Work to Idle
                if ((response[1] == 0x44 || response[2] == 0x44) && fp.Status != FuelPointStatusEnum.Idle)
                {
                    this.getDisplay(fp);
                    this.evalDisplay(fp.ActiveNozzle);
                    System.Threading.Thread.Sleep(500);
                    this.getDisplay(fp);
                    this.evalDisplay(fp.ActiveNozzle);
                    System.Threading.Thread.Sleep(500);

                    if (currentNozzle >= 0)
                        fp.Nozzles[currentNozzle].QueryTotals = true;

                    System.Threading.Thread.Sleep(500);

                    if (currentNozzle >= 0)
                        fp.Nozzles[currentNozzle].QueryTotals = true;

                    System.Threading.Thread.Sleep(100);
                    LastSale = DateTime.Now;
                    newStatus = FuelPointStatusEnum.Idle;
                }
                //Work State
                if (response[1] == 0x43 || response[2] == 0x43)
                {
                    this.getDisplay(fp);
                    this.evalDisplay(fp.ActiveNozzle);
                    newStatus = FuelPointStatusEnum.Work;
                }
            }
            fp.Status = newStatus;
            fp.DispenserStatus = fp.Status;
        }
        private void ResetStatus(Common.FuelPoint fp)
        {

            byte[] buffer = Commands.Halt(fp.Address);
            this.serialPort.Write(buffer, 0, buffer.Length);
            if (System.IO.File.Exists(LogPath()))
            {
                System.IO.File.AppendAllText(LogPath(), DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " TransmitHalt TX -> " + System.Text.Encoding.ASCII.GetString(buffer) + "\r\n");
            }
            int waiting = 0;
            while (this.serialPort.BytesToRead < 2 && waiting < 300)
            {
                System.Threading.Thread.Sleep(20);
                waiting += 20;
            }
            byte[] response = new byte[serialPort.BytesToRead];
            serialPort.Read(response, 0, response.Length);


            ///////////////////////////////

            byte[] buffer2 = Commands.PowerONOFF(fp.Address);
            this.serialPort.Write(buffer2, 0, buffer2.Length);
            if (System.IO.File.Exists(LogPath()))
            {
                System.IO.File.AppendAllText(LogPath(), DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " OpenPump TX -> " + System.Text.Encoding.ASCII.GetString(buffer2) + "\r\n");
            }

            while (this.serialPort.BytesToRead < 2 && waiting < 300)
            {
                System.Threading.Thread.Sleep(20);
                waiting += 20;
            }
            byte[] response2 = new byte[serialPort.BytesToRead];
            serialPort.Read(response2, 0, response2.Length);
        }
        #endregion

    }
}
