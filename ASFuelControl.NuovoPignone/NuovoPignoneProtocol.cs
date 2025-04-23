using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.IO;
using System.Diagnostics;
using ASFuelControl.Common;
using ASFuelControl.Common.Enumerators;
using System.Collections;

namespace ASFuelControl.NuovoPignone
{
    public class Commands
    {
        public static byte[] InterfaceCMD(int Address)
        {
            byte FuelPointAddress = BitConverter.GetBytes(Address)[0];
            byte Channel = BitConverter.GetBytes(183 + Address)[0];
            byte CR = BitConverter.GetBytes(90 - (64 * (Address - 1)))[0];
            byte[] Command = new byte[] { 0xFE, 0xFD, 0xFC, 0xFB, FuelPointAddress, CR, Channel };
            return Command;
        }
        public static byte[] Initialize(int Address)
        {
            byte FuelPointAddress = BitConverter.GetBytes(7 + (8 * Address))[0];
            byte CRC = BitConverter.GetBytes(255 - (7 + (8 * Address)))[0];
            byte[] Command = new byte[] { 0x00, FuelPointAddress, CRC };
            return Command;
        }
        public static byte[] GetStatus(int Address)
        {
            byte FuelPointAddress = BitConverter.GetBytes(1 + (8 * Address))[0];
            byte[] Command = new byte[] { 0x00, FuelPointAddress };
            return Command;
        }
        public static byte[] Authorise(int Address)
        {
            byte FuelPointAddress = BitConverter.GetBytes(4 + (8 * Address))[0];
            byte CMD = BitConverter.GetBytes(225)[0];
            byte[] Command = new byte[] { 0x00, FuelPointAddress, CMD };
            return Command;
        }
        public static byte[] Halt(int Address)
        {
            byte FuelPointAddress = BitConverter.GetBytes(4 + (8 * Address))[0];
            byte CMD = BitConverter.GetBytes(210)[0];
            byte[] Command = new byte[] { 0x00, FuelPointAddress, CMD };
            return Command;
        }
        public static byte[] ExecuteCommand(int Address)
        {
            byte FuelPointAddress = BitConverter.GetBytes(5 + (8 * Address))[0];
            byte CRC = BitConverter.GetBytes(255 - (5 + (8 * Address)))[0];
            byte[] Command = new byte[] { 0x00, FuelPointAddress, CRC };
            return Command;
        }
        public static byte[] GetDisplayCMD(int Address)
        {
            byte FuelPointAddress = BitConverter.GetBytes(2 + (8 * Address))[0];
            byte CMD = BitConverter.GetBytes(180)[0];
            byte[] Command = new byte[] { 0x00, FuelPointAddress, CMD };
            return Command;
        }
        public static byte[] GetVolumeTotals(int Address)
        {
            byte FuelPointAddress = BitConverter.GetBytes(2 + (8 * Address))[0];
            byte CMD = BitConverter.GetBytes(135)[0];
            byte[] Command = new byte[] { 0x00, FuelPointAddress, CMD };
            return Command;
        }
        public static byte[] GetAmountTotals(int Address)
        {
            byte FuelPointAddress = BitConverter.GetBytes(2 + (8 * Address))[0];
            byte CMD = BitConverter.GetBytes(120)[0];
            byte[] Command = new byte[] { 0x00, FuelPointAddress, CMD };
            return Command;
        }
        public static byte[] GetPrice(int Address)
        {
            byte FuelPointAddress = BitConverter.GetBytes(2 + (8 * Address))[0];
            byte CMD = BitConverter.GetBytes(195)[0];
            byte[] Command = new byte[] { 0x00, FuelPointAddress, CMD };
            return Command;
        }
        public static byte[] SetPrice(int UnitPrice, int Address)
        {

            if (UnitPrice > 9999)
            {
                throw new ArgumentException("max value 9999", "unitPrice");
            }
            byte FuelPointAddress = BitConverter.GetBytes(4 + (8 * Address))[0];
            byte CMD = BitConverter.GetBytes(105)[0];
            string str = Convert.ToString(UnitPrice);
            if (str.Length == 3) { str = "0" + str; }
            if (str.Length == 2) { str = "00" + str; }
            if (str.Length == 1) { str = "000" + str; }
            byte[] price = StringToByteArray(str);
            byte[] Command = new byte[] { 0x00, FuelPointAddress, CMD, price[0], price[1] };
            return Command;


        }
        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }
    }
    public class NuovoPignoneProtocol : Common.IFuelProtocol
    {
        const int INITIALIZE = 7;
        const int STATUS1 = 1;
        const int STATUS2 = 3;
        const int STATUS3 = 6;
        const int STATUS4 = 2;
        const int GETPRICE = 2;
        const int GETDATA = 2;
        const int TOTALSVOLUME = 2;
        const int TOTALSAMOUNT = 2;
        const int AUTHORIZE = 4;

        const int SUB_STATUS4 = 75;
        const int SUB_GETPRICE1 = 195;
        const int SUB_GETPRICE2 = 165;
        const int SUB_GETPRICE3 = 90;
        const int SUB_GETDATA = 180;
        const int SUB_TOTALSVOLUME = 135;
        const int SUB_TOTALSAMOUNT = 170;
        const int SUB_TOTALSVOLUMEMULTI = 45;
        const int SUB_TOTALSAMOUNTMULTI = 30;
        const int SUB_AUTHORIZE = 225;

        private byte[] GetCommand(int address, int cmd, int? subCommand = null)
        {
            BitArray baCmd = new BitArray(new int[] { cmd });
            BitArray baAddress = new BitArray(new int[] { address });
            BitArray baTotal = subCommand.HasValue ? new BitArray(16) : new BitArray(8);
            int offset = subCommand.HasValue ? 8 : 0;
            for (int i = 0; i < 5; i++)
            {
                baTotal.Set(i + offset + 3, baAddress.Get(i));
            }
            for (int i = 0; i < 2; i++)
            {
                baTotal.Set(i + offset, baCmd.Get(i));
            }
            if (subCommand.HasValue)
            {
                BitArray baSubCommand = new BitArray(new int[] { subCommand.Value });
                for (int i = 0; i < 8; i++)
                {
                    baTotal.Set(i, baSubCommand.Get(i));
                }
            }

            byte[] bytes = subCommand.HasValue ? new byte[2] : new byte[1];
            baTotal.CopyTo(bytes, 0);

            return bytes;
        }

        private void ExecuteCommand(int address, int cmd, int? subCommand = null)
        {
            var command = GetCommand(address, cmd, subCommand);
            this.serialPort.Write(command, 0, command.Length);
            Trace.WriteLine(BitConverter.ToString(command));
            int num = 0;
            while (this.serialPort.BytesToRead < 3 && num <= 50)
            {
                System.Threading.Thread.Sleep(10);
                num += 10;
            }
            
            byte[] response = new byte[this.serialPort.BytesToRead];
            if(response.Length == 0)
                return;

            this.serialPort.Read(response, 0, this.serialPort.BytesToRead);
            if (System.IO.File.Exists(LogPath()))
            {
                System.IO.File.AppendAllText(LogPath(), DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " " + BitConverter.ToString(response) + "\r\n");
            }

            EvaluateCommand(address, cmd, subCommand, response);
        }

        private void EvaluateCommand(int address, int cmd, int? subCommand, byte[] response)
        {

        }

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
                this.serialPort.Parity = System.IO.Ports.Parity.Odd;
                this.serialPort.DtrEnable = true;
                this.serialPort.BaudRate = 2400;
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
            #region InitializeFirst
            foreach (FuelPoint fuelPoint in this.fuelPoints)
            {
                ExecuteCommand(fuelPoint.Address, INITIALIZE, null);
                //this.InitializeDispenser(fuelPoint);
            }
            #endregion
            #region TakeTotals
            foreach (Common.FuelPoint fp in this.fuelPoints)
            {
                foreach (Nozzle nz in fp.Nozzles)
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
                                        System.Threading.Thread.Sleep(50);

                                        fp.Initialized = true;
                                        if (this.TotalsRecieved != null)
                                        {
                                            this.TotalsRecieved(this, new Common.TotalsEventArgs(fp, nz.Index, nz.TotalVolume, nz.TotalPrice));
                                        }
                                    }
                                }
                                continue;
                            }
                            if (fp.QueryAuthorize)
                            {
                                if (this.AuthorizeFuelPoint(fp))
                                    fp.QueryAuthorize = false;
                                continue;
                            }
                            if (fp.Status == Common.Enumerators.FuelPointStatusEnum.Work)
                            {
                                fp.SetExtendedProperty("iNeedDisplay", true);
                                this.GetDisplay(fp.ActiveNozzle);
                            }
                            Common.Enumerators.FuelPointStatusEnum oldStatus = fp.Status;
                            this.GetStatus(fp);
                            if (oldStatus != fp.Status && this.DispenserStatusChanged != null)
                            {
                                Common.FuelPointValues values = new Common.FuelPointValues();
                                if (fp.Status != Common.Enumerators.FuelPointStatusEnum.Idle && fp.Status != Common.Enumerators.FuelPointStatusEnum.Offline)
                                {
                                    fp.ActiveNozzleIndex = 0;
                                    values.ActiveNozzle = 0;
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
                        }
                        finally
                        {
                            System.Threading.Thread.Sleep(150);
                        }
                    }
                }
                catch (Exception errorWorkflow)
                {

                    //System.IO.Directory.CreateDirectory(System.Environment.CurrentDirectory + "\\logs");
                    //System.IO.File.AppendAllText(System.Environment.CurrentDirectory + "\\logs" + "\\NuovoPignoneError.txt", "\n" + errorWorkflow.ToString());
                    //System.Threading.Thread.Sleep(100);
                }
            }
        }

        #endregion

        #region Initialize
        public void InitializeDispenser(FuelPoint fp)
        {
            byte[] CMD = Commands.Initialize(fp.Address);
            this.serialPort.Write(CMD, 0, CMD.Length);
            int num = 0;
            if (System.IO.File.Exists(LogPath()))
            {
                System.IO.File.AppendAllText(LogPath(), DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " " + BitConverter.ToString(CMD) + "\r\n");
            }
            while (this.serialPort.BytesToRead < 3 && num < 300)
            {
                System.Threading.Thread.Sleep(25);
                num += 20;
            }

            byte[] numArray = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(numArray, 0, this.serialPort.BytesToRead);
            if (System.IO.File.Exists(LogPath()))
            {
                System.IO.File.AppendAllText(LogPath(), DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " " + BitConverter.ToString(numArray) + "\r\n");
            }
        }

        #endregion

        #region GetStatus
        private void GetStatus(FuelPoint fp)
        {
            byte[] CMD = Commands.GetStatus(fp.Address);
            this.serialPort.Write(CMD, 0, CMD.Length);
            Trace.WriteLine(BitConverter.ToString(CMD));
            int num = 0;
            if (System.IO.File.Exists(LogPath()))
            {
                System.IO.File.AppendAllText(LogPath(), DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " " + BitConverter.ToString(CMD) + "\r\n");
            }
            while (this.serialPort.BytesToRead < 3 && num < 300)
            {
                System.Threading.Thread.Sleep(25);
                num += 20;
            }

            byte[] response = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(response, 0, this.serialPort.BytesToRead);
            if (System.IO.File.Exists(LogPath()))
            {
                System.IO.File.AppendAllText(LogPath(), DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " " + BitConverter.ToString(response) + "\r\n");
            }
            Trace.WriteLine(BitConverter.ToString(response) + fp.Status.ToString());
            if (response.Length > 0)
                this.EvalStatus(fp, response);
        }
        int extendedProperty;
        private void EvalStatus(Common.FuelPoint fp, byte[] buffer)
        {
            FuelPointValues fuelPointValue = new FuelPointValues();
            FuelPointStatusEnum oldStatus = fp.Status;
            FuelPointStatusEnum newStatus = fp.Status;
            int currentNozzle = (int)fp.GetExtendedProperty("CurrentNozzle", -1);



            if (buffer[1] == 0x00 && fp.Status == FuelPointStatusEnum.Offline)
            {
                newStatus = FuelPointStatusEnum.Idle;
            }
            else if ((buffer[1] == 0x80 || buffer[1] == 0x88) && fp.Status != FuelPointStatusEnum.Work)
            {
                this.InitializeDispenser(fp);
                newStatus = FuelPointStatusEnum.Idle;
            }
            else if (buffer[1] == 0x0A && fp.Status == FuelPointStatusEnum.Work)
            {
                this.GetDisplay(fp.ActiveNozzle);
            }

            else if ((buffer[1] == 0x00 || buffer[1] == 0x80) && fp.Status == FuelPointStatusEnum.Work)
            {
                this.GetDisplay(fp.ActiveNozzle);
                System.Threading.Thread.Sleep(25);
                newStatus = FuelPointStatusEnum.Idle;
                //if (currentNozzle >= 0)
                //    fp.Nozzles[currentNozzle].QueryTotals = true;
            }

            else if (buffer[1] == 0x0A && fp.Status == FuelPointStatusEnum.Nozzle)
            {
                newStatus = FuelPointStatusEnum.Work;
            }
            else if (buffer[1] == 0x00 && fp.Status == FuelPointStatusEnum.Nozzle)
            {
                newStatus = FuelPointStatusEnum.Idle;
            }

            else if (buffer[1] == 0x08 && fp.Status == FuelPointStatusEnum.Idle)
            {
                this.extendedProperty = 0;
                newStatus = FuelPointStatusEnum.Nozzle;
                fp.SetExtendedProperty("CurrentNozzle", this.extendedProperty);
            }
            else
            {
                newStatus = fp.Status;
            }

            fp.Status = newStatus;
            fp.DispenserStatus = fp.Status;
        }

        #endregion

        #region GetDisplay
        public Common.Nozzle GetDisplay(Common.Nozzle nozzle)
        {
            byte[] upBuffer = Commands.GetDisplayCMD(nozzle.ParentFuelPoint.Address);
            this.serialPort.Write(upBuffer, 0, upBuffer.Length);
            int waiting = 0;
            if (System.IO.File.Exists(LogPath()))
            {
                System.IO.File.AppendAllText(LogPath(), DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " " + BitConverter.ToString(upBuffer) + "\r\n");
            }
            while (this.serialPort.BytesToRead < 16 && waiting < 300)
            {
                System.Threading.Thread.Sleep(20); //15*4 = 70 [ NP 68ms response time]
                waiting += 20;
            }

            byte[] response = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(response, 0, this.serialPort.BytesToRead);
            if (System.IO.File.Exists(LogPath()))
            {
                System.IO.File.AppendAllText(LogPath(), DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " " + BitConverter.ToString(response) + "\r\n");
            }
            if (response.Length > 0)
            {
                return evalDisplay(nozzle, response);
            }
            else
            {
                return null;
            }
        }
        private Common.Nozzle evalDisplay(Common.Nozzle nozzle, byte[] response)
        {
            //tx 00 0A B4 //NZ1
            //rx 0A B4 00 FF 07 F8 29 D6 00 FF 04 FB 45 BA 00 FF (7.29e) (4.45ltr)
            string[] parms = BitConverter.ToString(response).Split('-');
            for (int i = 1; i < 8; i++)
            {
                if (Commands.StringToByteArray(parms[2 * i])[0] + StringToByteArray(parms[2 * i + 1])[0] != 255)
                {
                    throw new Exception("evalDisplay() Failed");
                }
            }

            string upString = parms[2] + parms[4] + parms[6];
            string volString = parms[8] + parms[10] + parms[12];

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
                    CurrentNozzleId = 1,
                    Values = values
                });
            }
            return nozzle;


        }
        #endregion

        #region GetTotals
        public bool GetTotals(Common.Nozzle nozzle)
        {
            byte[] upBuffer = Commands.GetVolumeTotals(nozzle.ParentFuelPoint.Address);
            this.serialPort.Write(upBuffer, 0, upBuffer.Length);
            int waiting = 0;
            if (System.IO.File.Exists(LogPath()))
            {
                System.IO.File.AppendAllText(LogPath(), DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " " + BitConverter.ToString(upBuffer) + "\r\n");
            }
            while (this.serialPort.BytesToRead < 14 && waiting < 300)
            {
                System.Threading.Thread.Sleep(25); //15*4 = 70 [ NP 68ms response time]
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
            //0A 87 00 FF 00 FF 00 FF 00 FF 25 DA 09 F6 = 2509,00


            if (response.Length == 14)
            {
                string[] Vtotal = BitConverter.ToString(response).Split('-');
                if (Vtotal[2] == "F0" || Vtotal[2] == "0F") { Vtotal[2] = "00"; }
                if (Vtotal[4] == "F0" || Vtotal[4] == "0F") { Vtotal[4] = "00"; }
                if (Vtotal[6] == "F0" || Vtotal[6] == "0F") { Vtotal[6] = "00"; }
                if (Vtotal[8] == "F0" || Vtotal[8] == "0F") { Vtotal[8] = "00"; }
                if (Vtotal[10] == "F0" || Vtotal[10] == "0F") { Vtotal[10] = "00"; }
                if (Vtotal[12] == "F0" || Vtotal[12] == "0F") { Vtotal[12] = "00"; }
                string volume = Vtotal[2] + Vtotal[4] + Vtotal[6] + Vtotal[8] + Vtotal[10] + Vtotal[12];

                nozzle.TotalVolume = decimal.Parse(volume);
                // nozzle.LastTotalVolume = 0;
                return true;
            }
            else
                return false;
        }
        #endregion

        #region Halt

        private bool Halt(FuelPoint fp)
        {
            int num = 0;
            int address = 5 + 8 * fp.Address;
            byte[] numArray = Commands.Halt(fp.Address);
            this.serialPort.Write(numArray, 0, (int)numArray.Length);

            Trace.WriteLine(BitConverter.ToString(numArray));

            while (this.serialPort.BytesToRead < 2 && num < 300)
            {
                System.Threading.Thread.Sleep(20);
                num = num + 20;
            }

            byte[] numArray1 = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(numArray1, 0, this.serialPort.BytesToRead);
            Trace.WriteLine(BitConverter.ToString(numArray1));

            /////////////////////////////Execute//////////////////////////////////

            byte[] numArray2 = Commands.ExecuteCommand(fp.Address);
            this.serialPort.Write(numArray2, 0, (int)numArray2.Length);
            Trace.WriteLine(BitConverter.ToString(numArray1));
            while (this.serialPort.BytesToRead < 1 && num < 300)
            {
                System.Threading.Thread.Sleep(20);
                num = num + 20;
            }
            byte[] numArray3 = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(numArray3, 0, this.serialPort.BytesToRead);

            Trace.WriteLine(BitConverter.ToString(numArray3));
            int executeNum = 5 + (8 * fp.Address);
            if (numArray3[0] == executeNum)
            {
                return true;
            }
            return false;
        }

        #endregion

        #region SetPrice
        public bool SetPrice(Common.Nozzle nozzle, int unitPrice)
        {
            try
            {

                byte[] buffer = Commands.SetPrice(unitPrice, nozzle.ParentFuelPoint.Address);
                this.serialPort.Write(buffer, 0, buffer.Length);
                int waiting = 0;
                if (System.IO.File.Exists(LogPath()))
                {
                    System.IO.File.AppendAllText(LogPath(), DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " " + BitConverter.ToString(buffer) + "\r\n");
                }
                while (this.serialPort.BytesToRead < 4 && waiting < 300)
                {
                    System.Threading.Thread.Sleep(25);
                    waiting += 20;
                }
                byte[] response = new byte[this.serialPort.BytesToRead];
                this.serialPort.Read(response, 0, this.serialPort.BytesToRead);
                if (System.IO.File.Exists(LogPath()))
                {
                    System.IO.File.AppendAllText(LogPath(), DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " " + BitConverter.ToString(response) + "\r\n");
                }
                System.Threading.Thread.Sleep(35);
                //  this.serialPort.Write(buffer, 0, buffer.Length);

                /********************Execute Command Halt***********************/

                byte[] Execute = Commands.ExecuteCommand(nozzle.ParentFuelPoint.Address);
                this.serialPort.Write(Execute, 0, (int)Execute.Length);
                if (System.IO.File.Exists(LogPath()))
                {
                    System.IO.File.AppendAllText(LogPath(), DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " " + BitConverter.ToString(response) + "\r\n");
                }
                System.Threading.Thread.Sleep(25);
                byte[] res = new byte[this.serialPort.BytesToRead];
                this.serialPort.Read(res, 0, this.serialPort.BytesToRead);
                if (System.IO.File.Exists(LogPath()))
                {
                    System.IO.File.AppendAllText(LogPath(), DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " " + BitConverter.ToString(res) + "\r\n");
                }

            }
            catch
            {
                return false;
            }
            return true;
        }

        #endregion

        #region Authorise
        public bool AuthorizeFuelPoint(Common.FuelPoint fp)
        {
            try
            {

                byte[] buffer = Commands.Authorise(fp.Address);
                this.serialPort.Write(buffer, 0, buffer.Length);
                int waiting = 0;
                if (System.IO.File.Exists(LogPath()))
                {
                    System.IO.File.AppendAllText(LogPath(), DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " " + BitConverter.ToString(buffer) + "\r\n");
                }
                while (this.serialPort.BytesToRead < 1 && waiting < 300)
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
                /********************Execute Command Halt***********************/

                byte[] Execute = Commands.ExecuteCommand(fp.Address);
                this.serialPort.Write(Execute, 0, (int)Execute.Length);
                if (System.IO.File.Exists(LogPath()))
                {
                    System.IO.File.AppendAllText(LogPath(), DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " " + BitConverter.ToString(Execute) + "\r\n");
                }
                while (this.serialPort.BytesToRead < 1 && waiting < 300)
                {
                    System.Threading.Thread.Sleep(20);
                    waiting += 20;
                }
                byte[] res = new byte[this.serialPort.BytesToRead];
                this.serialPort.Read(res, 0, this.serialPort.BytesToRead);
                if (System.IO.File.Exists(LogPath()))
                {
                    System.IO.File.AppendAllText(LogPath(), DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " " + BitConverter.ToString(res) + "\r\n");
                }
            }
            catch (Exception ex)
            {
                if (System.IO.File.Exists(LogPath()))
                {
                    System.IO.File.AppendAllText(LogPath(), DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + " " + ex.ToString() + "\r\n");
                }
                return false;
            }
            return true;
        }

        #endregion

        #region Tools
        public int CRC(int data)
        {
            int result = 255 - data;
            return result;
        }
        public byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }
        public string LogPath()
        {
            string LogPath = "NP_" + this.CommunicationPort + ".log";
            return LogPath;
        }

        #endregion

        #region New Commands
        StatusCommand statuscCommand = new StatusCommand();
        #endregion

        private struct IPumpDebugArgs
        {

            public Common.Enumerators.FuelPointStatusEnum status;
            public decimal totalizer;
            public decimal volume;
            public decimal amount;

        }

    }

    public class StatusCommand : BaseNuovoPignoneCommand
    {
        public StatusCommand() : base(1, -1)
        {

        }

        protected override void EvaluateCommand(int address, byte[] response)
        {
            
        }
    }

    public class GetPriceCommand : BaseNuovoPignoneCommand
    {
        public GetPriceCommand() : base(1, -1)
        {

        }

        protected override void EvaluateCommand(int address, byte[] response)
        {

        }
    }

    public class BaseNuovoPignoneCommand
    {
        public System.IO.Ports.SerialPort SerialPort { set; get; }
        public int Command { get; }
        public int SubCommand { get; }
        public bool SkipEvaluation { set; get; }

        public BaseNuovoPignoneCommand(int comand, int subCommand = -1)
        {
            this.Command = Command;
            this.SubCommand = subCommand;
        }

        private byte[] GetCommand(int address)
        {
            BitArray baCmd = new BitArray(new int[] { this.Command });
            BitArray baAddress = new BitArray(new int[] { address });
            BitArray baTotal = this.SubCommand >= 0 ? new BitArray(16) : new BitArray(8);
            int offset = this.SubCommand >= 0 ? 8 : 0;
            for (int i = 0; i < 5; i++)
            {
                baTotal.Set(i + offset + 3, baAddress.Get(i));
            }
            for (int i = 0; i < 2; i++)
            {
                baTotal.Set(i + offset, baCmd.Get(i));
            }
            if (this.SubCommand >= 0)
            {
                BitArray baSubCommand = new BitArray(new int[] { this.SubCommand });
                for (int i = 0; i < 8; i++)
                {
                    baTotal.Set(i, baSubCommand.Get(i));
                }
            }

            byte[] bytes = this.SubCommand >= 0 ? new byte[2] : new byte[1];
            baTotal.CopyTo(bytes, 0);

            return bytes;
        }

        public void ExecuteCommand(int address)
        {
            var command = GetCommand(address);
            this.SerialPort.Write(command, 0, command.Length);
            Trace.WriteLine(BitConverter.ToString(command));
            int num = 0;
            while (this.SerialPort.BytesToRead < 3 && num <= 50)
            {
                System.Threading.Thread.Sleep(10);
                num += 10;
            }

            byte[] response = new byte[this.SerialPort.BytesToRead];
            if (response.Length == 0)
                return;

            this.SerialPort.Read(response, 0, this.SerialPort.BytesToRead);

            EvaluateCommand(address, response);
        }

        protected virtual void EvaluateCommand(int address, byte[] response)
        {

        }
    }
}