using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ASFuelControl.Common;
using ASFuelControl.Common.Enumerators;

namespace ASFuelControl.NuovoPignoneCL
{
    public class NuovoPignoneCLProtocol : Common.IFuelProtocol
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
                this.serialPort.StopBits = System.IO.Ports.StopBits.One;
                this.serialPort.DataBits = 8;
                this.serialPort.BaudRate = 57600;
                this.serialPort.Open();
                this.th = new System.Threading.Thread(new System.Threading.ThreadStart(this.Thread));
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

        /**************************Start Thread********************************/

        #region Thread

        public void Thread()
        {

            foreach (Common.FuelPoint fp in this.fuelPoints)
            {
                InitializeDispenser(fp);
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
                                fp.QueryHalt = this.Halt(fp);
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
                    System.IO.File.AppendAllText(System.Environment.CurrentDirectory + "\\logs" + "\\NuovoPignone_error.txt", "\n" + e.ToString());
                    System.Threading.Thread.Sleep(50);

                }

            }

        }
        #endregion

        /**************************End Thread**********************************/






        /*************************Create Commands*****************************/

        #region CreateCommands
        private byte[] InterfaceCMD(int Address)
        {
            byte FuelPointAddress = BitConverter.GetBytes(Address)[0];
            byte Channel = BitConverter.GetBytes(183 + Address)[0];
            byte CR = BitConverter.GetBytes(90 - (64 * (Address-1)))[0];
            byte[] Command = new byte[] { 0xFE, 0xFD, 0xFC, 0xFB, FuelPointAddress, CR, Channel };
            return Command;
        }
        private byte[] Initialize(int Address)
        {
            byte FuelPointAddress = BitConverter.GetBytes(7 + (8 * Address))[0];
            byte CRC = BitConverter.GetBytes(255-(7 + (8 * Address)))[0];
            byte[] New = InterfaceCMD(Address);
            byte[] Command = new byte[] { New[0], New[1], New[2], New[3], New[4], New[5], New[6], 0x00, FuelPointAddress, CRC };
            return Command;
        }
        private byte[] GetStatus(int Address)
        {
            byte FuelPointAddress = BitConverter.GetBytes(1 + (8 * Address))[0];
            byte[] New = InterfaceCMD(Address);
            byte[] Command = new byte[] { New[0], New[1], New[2], New[3], New[4], New[5], New[6], 0x00, FuelPointAddress };
            return Command;
        }
        private byte[] Authorise(int Address)
        {
            byte FuelPointAddress = BitConverter.GetBytes(4 + (8 * Address))[0];
            byte CMD = BitConverter.GetBytes(225)[0];
            byte[] New = InterfaceCMD(Address);
            byte[] Command = new byte[] { New[0], New[1], New[2], New[3], New[4], New[5], New[6], 0x00, FuelPointAddress, CMD };
            return Command;
        }
        private byte[] Halt(int Address)
        {
            byte FuelPointAddress = BitConverter.GetBytes(4 + (8 * Address))[0];
            byte CMD = BitConverter.GetBytes(210)[0];
            byte[] New = InterfaceCMD(Address);
            byte[] Command = new byte[] { New[0], New[1], New[2], New[3], New[4], New[5], New[6], 0x00, FuelPointAddress, CMD };
            return Command;
        }
        private byte[] ExecuteCommand(int Address)
        {
            byte FuelPointAddress = BitConverter.GetBytes(5 + (8 * Address))[0];
            byte CRC = BitConverter.GetBytes(255 - (5 + (8 * Address)))[0];
            byte[] New = InterfaceCMD(Address);
            byte[] Command = new byte[] { New[0], New[1], New[2], New[3], New[4], New[5], New[6], 0x00, FuelPointAddress, CRC };
            return Command;
        }
        private byte[] GetDisplayCMD(int Address)
        {
            byte FuelPointAddress = BitConverter.GetBytes(2 + (8 * Address))[0];
            byte CMD = BitConverter.GetBytes(180)[0];
            byte[] New = InterfaceCMD(Address);
            byte[] Command = new byte[] { New[0], New[1], New[2], New[3], New[4], New[5], New[6], 0x00, FuelPointAddress, CMD };
            return Command;
        }
        private byte[] GetVolumeTotals(int Address)
        {
            byte FuelPointAddress = BitConverter.GetBytes(2 + (8 * Address))[0];
            byte CMD = BitConverter.GetBytes(135)[0];
            byte[] New = InterfaceCMD(Address);
            byte[] Command = new byte[] { New[0], New[1], New[2], New[3], New[4], New[5], New[6], 0x00, FuelPointAddress, CMD };
            return Command;
        }
        private byte[] GetAmountTotals(int Address)
        {
            byte FuelPointAddress = BitConverter.GetBytes(2 + (8 * Address))[0];
            byte CMD = BitConverter.GetBytes(120)[0];
            byte[] New = InterfaceCMD(Address);
            byte[] Command = new byte[] { New[0], New[1], New[2], New[3], New[4], New[5], New[6], 0x00, FuelPointAddress, CMD };
            return Command;
        }
        private byte[] GetPrice(int Address)
        {
            byte FuelPointAddress = BitConverter.GetBytes(2 + (8 * Address))[0];
            byte CMD = BitConverter.GetBytes(195)[0];
            byte[] New = InterfaceCMD(Address);
            byte[] Command = new byte[] { New[0], New[1], New[2], New[3], New[4], New[5], New[6], 0x00, FuelPointAddress, CMD };
            return Command;
        }
        private byte[] SetPrice(int Address, int UnitPrice)
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
            byte[] New = InterfaceCMD(Address);
            byte[] Command = new byte[] { New[0], New[1], New[2], New[3], New[4], New[5], New[6], 0x00, FuelPointAddress, CMD, price[0], price[1] };
            return Command;        
        }      


        #endregion

        /***********************End CreateCommands***************************/



        #region InitializeDispenser
        public void InitializeDispenser(FuelPoint fp)
        {
            byte[] CMD = this.Initialize(fp.Address);
            this.serialPort.Write(CMD, 0, CMD.Length);
            int num = 0;
            while (this.serialPort.BytesToRead <= 3 && num < 300)
            {
                System.Threading.Thread.Sleep(10);
                num += 20;
            }

            byte[] numArray = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(numArray, 0, this.serialPort.BytesToRead);
            System.Threading.Thread.Sleep(20);
            for (int i = 0; i <= 2; i++)
            {
                byte[] fuelPointStatus = this.GetStatus(fp.Address);
                this.serialPort.Write(fuelPointStatus, 0, (int)fuelPointStatus.Length);
                System.Threading.Thread.Sleep(100);

                byte[] Buffer = new byte[this.serialPort.BytesToRead];
                this.serialPort.Read(Buffer, 0, this.serialPort.BytesToRead);
            }
        }

        #endregion

        #region Status

        public void GetStatus(FuelPoint fp)
        {
            byte[] fuelPointStatus = this.GetStatus(fp.Address);
            this.serialPort.Write(fuelPointStatus, 0, (int)fuelPointStatus.Length);
            int num = 0;
            while (this.serialPort.BytesToRead <= 3 && num < 300)
            {
                System.Threading.Thread.Sleep(20);
                num += 20;
            }          
            byte[] numArray = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(numArray, 0, this.serialPort.BytesToRead);            
            this.EvaluateStatus(fp, numArray);
        }

        private bool IsValidResponse(Common.FuelPoint fp, byte[] response)
        {
            if (response.Length == 0)
                return false;
            if (response[0] != (fp.Address * 8) + 1)
                return false;
            return true;
        }
        
        int extendedProperty;
        private void EvaluateStatus(FuelPoint fp, byte[] response)
        {
            FuelPointValues fuelPointValue = new FuelPointValues();
            FuelPointStatusEnum oldStatus = fp.Status;
            FuelPointStatusEnum newStatus = fp.Status;
            int currentNozzle = (int)fp.GetExtendedProperty("CurrentNozzle", -1);

            if (!this.IsValidResponse(fp, response) && DateTime.Now.Subtract(fp.LastValidResponse).TotalSeconds > 10)
            {
                if (this.DispenserOffline != null)
                    this.DispenserOffline(fp, new EventArgs());
                return;
                //fp.Status = Common.Enumerators.FuelPointStatusEnum.Offline;
            }
            if (!this.IsValidResponse(fp, response))
                return;
            fp.LastValidResponse = DateTime.Now;

            if(fp.Status == FuelPointStatusEnum.Offline)
            {
                this.InitializeDispenser(fp);
            }

            if (response.Length == 3)
            {
                if (response[1] == 0x0A && fp.Status == FuelPointStatusEnum.Work)
                {
                    this.GetDisplay(fp);
                }
                if (response[1] == 0x00 && fp.Status == FuelPointStatusEnum.Offline)
                {
                    newStatus = FuelPointStatusEnum.Idle;
                }
                if (response[1] == 0x00 && fp.Status == FuelPointStatusEnum.Work)
                {
                    newStatus = FuelPointStatusEnum.Idle;
                    //if (currentNozzle >= 0)
                    //{
                    //    fp.Nozzles[currentNozzle].QueryTotals = true;
                    //}
                    this.extendedProperty = -1;
                    fp.SetExtendedProperty("CurrentNozzle", -1);
                }
               
                if (response[1] == 0x0A && fp.Status == FuelPointStatusEnum.Nozzle)
                {
                    newStatus = FuelPointStatusEnum.Work;
                }
                if (response[1] == 0x00 && fp.Status == FuelPointStatusEnum.Nozzle)
                {
                    newStatus = FuelPointStatusEnum.Idle;
                }
                ////*****************************************/
                if (response[1] == 0x08 && fp.Status == FuelPointStatusEnum.Idle)
                {
                    this.extendedProperty = 0;
                    newStatus = FuelPointStatusEnum.Nozzle;
                    fp.SetExtendedProperty("CurrentNozzle", this.extendedProperty);
                }
                
            }
            fp.Status = newStatus;
            fp.DispenserStatus = fp.Status;
        }
        
        #endregion

        #region Totals

        private bool GetTotals(Nozzle nz)
        {
            byte[] fuelPointStatus = this.GetVolumeTotals(nz.ParentFuelPoint.Address);
            this.serialPort.Write(fuelPointStatus, 0, (int)fuelPointStatus.Length);

            int num = 0;
            while (this.serialPort.BytesToRead <= 14 && num < 300)
            {
                System.Threading.Thread.Sleep(40);
                num += 20;
            }

            byte[] Buffer = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(Buffer, 0, this.serialPort.BytesToRead);

            return this.EvaluateTotals(nz, Buffer);
        }

        private bool EvaluateTotals(Nozzle nz, byte[] response)
        {             
            try
            {
                if(response.Length == 14)
                {                    
                    string[] Vtotal = BitConverter.ToString(response).Split('-');
                    if (Vtotal[2] == "F0" || Vtotal[2] == "0F") { Vtotal[2] = "00"; }
                    if (Vtotal[4] == "F0" || Vtotal[4] == "0F") { Vtotal[4] = "00"; }
                    if (Vtotal[6] == "F0" || Vtotal[6] == "0F") { Vtotal[6] = "00"; }
                    if (Vtotal[8] == "F0" || Vtotal[8] == "0F") { Vtotal[8] = "00"; }
                    if (Vtotal[10] == "F0" || Vtotal[10] == "0F") { Vtotal[10] = "00"; }
                    if (Vtotal[12] == "F0" || Vtotal[12] == "0F") { Vtotal[12] = "00"; }
                    string volume = Vtotal[2] + Vtotal[4] + Vtotal[6] + Vtotal[8] + Vtotal[10] + Vtotal[12];
                    nz.TotalVolume = decimal.Parse(volume);
                    return true;
                }
                else
                {
                    return false;
                }
               
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Halt
        private bool Halt(FuelPoint fp)
        {
            int num = 0;
            int FPAddress = 5 + (8 * fp.Address);
            byte[] CMD = this.Halt(fp.Address);

            this.serialPort.Write(CMD, 0, (int)CMD.Length);
            while (this.serialPort.BytesToRead <= 3 && num < 300)
            {
                System.Threading.Thread.Sleep(20);
                num += 20;
            }
            byte[] numArray = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(numArray, 0, this.serialPort.BytesToRead);
            int haltcom = 1 + (8 * fp.Address);

            /********************Execute Command Halt***********************/
            this.serialPort.DiscardInBuffer();

            byte[] Execute = this.ExecuteCommand(fp.Address);
            this.serialPort.Write(Execute, 0, (int)Execute.Length);

            byte[] res = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(res, 0, this.serialPort.BytesToRead);

            /**********************Verify Result*******************************/
            if (res[0] == FPAddress)
                return true;
            else
                return false;
        }
        #endregion

        #region Authorise
        private bool AuthorizeFuelPoint(FuelPoint fp)
        {
            byte[] CMD = this.Authorise(fp.Address);
            this.serialPort.Write(CMD, 0, (int)CMD.Length);
            int num = 0;
            while (this.serialPort.BytesToRead <= 2 && num < 300)
            {
                System.Threading.Thread.Sleep(20);
                num += 20;
            }
            byte[] numArray = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(numArray, 0, this.serialPort.BytesToRead);
            /********************Execute Command Halt***********************/
            byte[] Execute = this.ExecuteCommand(fp.Address);
            this.serialPort.Write(Execute, 0, Execute.Length);
            num = 0;
            while (this.serialPort.BytesToRead <= 2 && num < 300)
            {
                System.Threading.Thread.Sleep(20);
                num += 20;
            }
            byte[] result = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(result, 0, this.serialPort.BytesToRead);
            /**********************Verify Result*******************************/
            byte FuelPointAddress = BitConverter.GetBytes(5 + (8 * fp.Address))[0];
            if (result.Length == 1 && result[0] == (byte)FuelPointAddress)
                return true;
            else
                return false;
        }
        #endregion

        #region DisplayData
        public bool GetDisplay(FuelPoint fp)
        {
            byte[] numArray = this.GetDisplayCMD(fp.Address);
            this.serialPort.Write(numArray, 0, (int)numArray.Length);
            int num = 0;
            while (this.serialPort.BytesToRead <= 14 && num < 300)
            {
                System.Threading.Thread.Sleep(25);
                num += 20;
            }
            byte[] numArray1 = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(numArray1, 0, this.serialPort.BytesToRead);
            this.evalDisplay(fp, numArray1);
            return true;
        }
        private bool evalDisplay(FuelPoint fp, byte[] response)
        {            
            try
            {
                string[] parms = BitConverter.ToString(response).Split('-');
                string upString = parms[2] + parms[4] + parms[6];
                string volString = parms[8] + parms[10] + parms[12];

                fp.ActiveNozzle.ParentFuelPoint.DispensedAmount = decimal.Parse(upString) / (decimal)Math.Pow(10, fp.ActiveNozzle.ParentFuelPoint.AmountDecimalPlaces);
                fp.ActiveNozzle.ParentFuelPoint.DispensedVolume = decimal.Parse(volString) / (decimal)Math.Pow(10, fp.ActiveNozzle.ParentFuelPoint.VolumeDecimalPlaces);

                if (this.DataChanged != null)
                {
                    FuelPointValues fuelPointValue = new FuelPointValues()
                    {                        
                        CurrentSalePrice = fp.ActiveNozzle.UnitPrice,
                        CurrentPriceTotal = fp.ActiveNozzle.ParentFuelPoint.DispensedAmount,
                        CurrentVolume = fp.ActiveNozzle.ParentFuelPoint.DispensedVolume
                    };
                    EventHandler<FuelPointValuesArgs> eventHandler = this.DataChanged;
                    FuelPointValuesArgs fuelPointValuesArg = new FuelPointValuesArgs()
                    {
                        CurrentFuelPoint = fp.ActiveNozzle.ParentFuelPoint,
                        CurrentNozzleId = 1,
                        Values = fuelPointValue
                    };
                    eventHandler(this, fuelPointValuesArg);
                }                
            }
            catch
            {               
            }
            return true;
        }
        #endregion

        #region SetPrices
        public bool SetPrice(Nozzle nozzle, int unitPrice)
        {
            this.serialPort.DiscardInBuffer();
            byte[] numArray = this.SetPrice(nozzle.ParentFuelPoint.Address, unitPrice);
            this.serialPort.Write(numArray, 0, (int)numArray.Length);            
            int num = 0;
            while (this.serialPort.BytesToRead <= 4 && num < 300)
            {
                System.Threading.Thread.Sleep(20);
                num += 20;
            }
            byte[] numArray1 = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(numArray1, 0, this.serialPort.BytesToRead);
            /********************Execute Command Halt***********************/
            byte[] Execute = this.ExecuteCommand(nozzle.ParentFuelPoint.Address);
            this.serialPort.Write(Execute, 0, (int)Execute.Length);
            num = 0;
            while (this.serialPort.BytesToRead <= 2 && num < 300)
            {
                System.Threading.Thread.Sleep(20);
                num += 20;
            }
            byte[] result = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(result, 0, this.serialPort.BytesToRead);
            /**********************Verify Result*******************************/
            byte FuelPointAddress = BitConverter.GetBytes(5 + (8 * nozzle.ParentFuelPoint.Address))[0];
            if (result.Length == 1 && result[0] == (byte)FuelPointAddress)
                return true;
            else
                return false;
        }
        #endregion

        /*************************Tools NuovoPignone**************************/

        private enum StatusError
        {
            ErrorStatus = 0,
            Contactor = 1,
            LowLevelStatus = 2,
            NozzleOut=3,
            LocalPriceChangeUnderWay = 4,
            MetricTestUnderWay = 5,
            InitializeMessage = 6,
            HostGenerateBlock = 7
        }
        private enum DispenserError
        {
            Ram = 17,
            Display1 = 33,
            CalcGi = 49,
            Calculations=65,
            Gi=81,
            Price=97,
            Display2=113,
            Totalizer=129,
            Watchdog=145,
            Calibration=161,
            AntiSpill=209,
            Temperature=225
        }
        private enum responseLength
        {
            //09 00 F7
            Status = 3,
            //0C E1
            Authorise = 2,
            //0D
            ExecuteCommand = 1,
            //0A B4 00 FF 00 FF 00 FF 00 FF 00 FF 00 FF
            Display = 14,
            //0A 87 00 FF 00 FF 14 EB 18 E7 45 BA 40 BF
            Totals = 14,
            //0A C3 [21] E9 [23] 78
            GetPrice = 6,
            //0C 69 [21] [23]
            SetPrice = 5,
            //0C 78 00 00 47
            Preset = 5,
            //0B 0A F5
            Halt = 3,
            //0F 12 ED
            Initialize = 3

        }
        public byte[] StringToByteArray(string hex)
        {
            return (
                from x in Enumerable.Range(0, hex.Length)
                where x % 2 == 0
                select Convert.ToByte(hex.Substring(x, 2), 16)).ToArray<byte>();
        }

        /*********************End Tools NuovoPignone**************************/
    }
}
