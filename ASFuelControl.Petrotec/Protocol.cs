using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Ports;
using System.Threading;
using ASFuelControl.Common;
using ASFuelControl.Common.Enumerators;
using System.Diagnostics;

namespace ASFuelControl.Petrotec
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
        public static byte[] NotAcknowledge()
        {
            byte[] buffer = new byte[] { 0x3F };
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


    public class PetrotecProtocol : IFuelProtocol, IPumpDebug
    {
        public event EventHandler<Common.FuelPointValuesArgs> DataChanged;
        public event EventHandler<Common.TotalsEventArgs> TotalsRecieved;
        public event EventHandler<Common.SaleEventArgs> SaleRecieved;
        public event EventHandler<Common.FuelPointValuesArgs> DispenserStatusChanged;
        public event EventHandler DispenserOffline;

        private List<Common.FuelPoint> fuelPoints = new List<Common.FuelPoint>();

        private SerialPort SerPort = new System.IO.Ports.SerialPort();
        private System.Threading.Thread th;
        private DateTime lastAccess = DateTime.Now;

        public DebugValues DebugStatusDialog(FuelPoint fp)
        {
            throw new NotImplementedException();
        }
        public Common.DebugValues foo = new Common.DebugValues();
        public void AddFuelPoint(Common.FuelPoint fp)
        {
            this.fuelPoints.Add(fp);
        }
        public void ClearFuelPoints()
        {
            this.fuelPoints.Clear();
        }
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
                return this.SerPort.IsOpen;
            }
        }
        public string CommunicationPort
        {
            set;
            get;
        }
        public void Connect()
        {
            this.SerPort.PortName = this.CommunicationPort;
            this.SerPort.BaudRate = 1200;
            this.SerPort.StopBits = StopBits.Two;
            this.SerPort.Parity = Parity.Even;
            this.SerPort.DataBits = 7;
            //this.SerPort.DataReceived += SerPort_DataReceived;
            this.SerPort.Open();
            this.th = new Thread(PetrotecThread);
            this.th.Start();
        }
        public void Disconnect()
        {
            if (this.SerPort.IsOpen)
                this.SerPort.Close();
            else
                return;
        }

        List<byte> DataBuffer = new List<byte>();
        private void PetrotecThread()
        {
            foreach (FuelPoint fp in fuelPoints)
            {
                foreach (Nozzle nz in fp.Nozzles)
                {
                    nz.QueryTotals = true;
                }
                
            }
            while (IsConnected)
            {
                try
                {
                    if(this.FuelPoints[0].Status == FuelPointStatusEnum.Offline)
                        System.Threading.Thread.Sleep(300);
                    System.Threading.Thread.Sleep(500);

                    if (this.SerPort.BytesToRead > 0)
                    {
                        lastAccess = DateTime.Now;
                        byte[] d = new byte[this.SerPort.BytesToRead];
                        this.SerPort.Read(d, 0, d.Length);
                        Logger(this.SerPort.PortName, d, "PumpData");
                        if (d.Contains((byte)'!'))
                        {
                            if (this.FuelPoints[0].Status == FuelPointStatusEnum.Offline)
                            {
                                this.SetFuelPointStatus(FuelPointStatusEnum.Idle, -1);
                                //this.FuelPoints[0].SetExtendedProperty("LastPendingCommand", null);
                            }
                            foreach (FuelPoint fp in this.fuelPoints)
                            {
                                bool NeedSwitchOff = false;
                                PendingCommand pc = fp.GetExtendedProperty("LastPendingCommand", null) as PendingCommand;
                                if (pc != null)
                                {
                                    foreach (Nozzle nz in fp.Nozzles)
                                    {
                                        if (pc.CommandName == "GetTotals")
                                        {
                                            //nz.QueryTotals = false;
                                            NeedSwitchOff = true;
                                        }
                                        else if (pc.CommandName == "Authorise")
                                        {
                                            nz.ParentFuelPoint.QueryAuthorize = false;
                                        }
                                        else if (pc.CommandName == "SwitchOff")
                                        {
                                            switchOffAttemps = 0;
                                        }
                                        else if (pc.CommandName == "Halt")
                                        {
                                            fp.SetExtendedProperty("NeedsSwitchOFf", false);
                                        }

                                    }
                                    fp.SetExtendedProperty("LastPendingCommand", null);
                                    if (NeedSwitchOff)
                                    {
                                        fp.SetExtendedProperty("LastPendingCommand", new PendingCommand() { CommandName = "SwitchOff", NozzleIndex = -1 });
                                    }
                                }
                            }
                        }
                        List<byte> foo = new List<byte>();
                        foreach (byte value in d)
                        {
                            if (value == (byte)'!')
                                continue;

                            foo.Add(value);
                        }

                        DataBuffer.AddRange(foo);
                        EvaluateData();
                    }
                    else if (this.HasPendingCommands())
                    {
                        Thread.Sleep(1000);
                    }
                    if (DateTime.Now.Subtract(lastAccess).TotalSeconds > 10)
                    {
                        this.fuelPoints[0].SetExtendedProperty("LastPendingCommand", new PendingCommand() { CommandName = "SwitchOff", NozzleIndex = -1 });
                        lastAccess = DateTime.Now;
                    }
                    foreach (FuelPoint fp in fuelPoints)
                    {
                        bool NeedsSwitchOFf = (bool)(fp.GetExtendedProperty("NeedsSwitchOFf", true));
                        if (!fp.Initialized && NeedsSwitchOFf)
                        {
                            fp.SetExtendedProperty("LastPendingCommand", new PendingCommand() { CommandName = "Halt", NozzleIndex = -1 });
                        }
                    }
                    foreach (FuelPoint fp in fuelPoints)
                    {
                        if (fp.Initialized && fp.QueryHalt)
                        {
                            fp.SetExtendedProperty("LastPendingCommand", new PendingCommand() { CommandName = "Halt", NozzleIndex = -1 });
                        }
                    }
                    if (!HasPendingCommands())
                    {
                        foreach (FuelPoint fp in this.fuelPoints)
                        {
                            bool NeedsSwitchOFf = (bool)(fp.GetExtendedProperty("NeedsSwitchOFf", true));
                            if (!fp.Initialized && !NeedsSwitchOFf)
                            {

                                fp.Initialized = fp.Nozzles.Where(n => n.QueryTotals).Count() == 0;

                                if (fp.Initialized)
                                {
                                    FuelPointValues fpvalues = new FuelPointValues();
                                    fpvalues.ActiveNozzle = -1;
                                    fpvalues.Status = FuelPointStatusEnum.Idle;
                                    fp.Status = FuelPointStatusEnum.Idle;
                                    fp.DispenserStatus = FuelPointStatusEnum.Idle;
                                    this.DispenserStatusChanged(this, new FuelPointValuesArgs()
                                    {
                                        CurrentFuelPoint = this.fuelPoints[0],
                                        CurrentNozzleId = -1,
                                        Values = fpvalues
                                    });
                                }
                            }
                        }
                    }
                    //Thread.Sleep(25);
                    if (HasPendingCommands())
                    {
                        //Thread.Sleep(1000);
                        continue;
                    }
                    bool QueryDisplay = false;
                    foreach (FuelPoint fp in this.fuelPoints)
                    {
                        if (fp.Status == FuelPointStatusEnum.Work)
                        {
                            fp.SetExtendedProperty("LastPendingCommand", new PendingCommand() { CommandName = "NeedDisplay", NozzleIndex = fp.ActiveNozzle.Index });
                            QueryDisplay = true;
                        }
                    }
                    if (QueryDisplay)
                        continue;
                    bool NZBreak = false;
                    foreach (FuelPoint fp in this.fuelPoints)
                    {
                        #region Authorise
                        if (fp.QueryAuthorize)
                        {
                            fp.SetExtendedProperty("LastPendingCommand", new PendingCommand() { CommandName = "Authorise", NozzleIndex = fp.ActiveNozzle.Index });
                            NZBreak = true;
                            break;
                        }
                        #endregion
                        foreach (Nozzle nz in fp.Nozzles)
                        {
                            if (nz.QueryTotals)
                            {
                                fp.SetExtendedProperty("LastPendingCommand", new PendingCommand() { CommandName = "GetTotals", NozzleIndex = nz.Index });
                                NZBreak = true;
                                break;
                            }


                        }
                        if (NZBreak)
                            break;

                    }
                }
                catch (Exception ex)
                {
                    Logger(this.CommunicationPort, ex.Message);
                }
            }
        }

        private void SetFuelPointStatus(Common.Enumerators.FuelPointStatusEnum status, int nzIndex)
        {
            FuelPointValues fpvalues = new FuelPointValues();
            fpvalues.ActiveNozzle = nzIndex;
            fpvalues.Status = status;
            this.fuelPoints[0].Status = status;
            this.fuelPoints[0].DispenserStatus = status;
            this.DispenserStatusChanged(this, new FuelPointValuesArgs()
            {
                CurrentFuelPoint = this.fuelPoints[0],
                CurrentNozzleId = nzIndex,
                Values = fpvalues
            });
        }

        int switchOffAttemps = 0;

        private bool HasPendingCommands()
        {
            try
            {
                foreach (FuelPoint fp in this.fuelPoints)
                {
                    PendingCommand pc = fp.GetExtendedProperty("LastPendingCommand", null) as PendingCommand;
                    foreach (Nozzle nz in fp.Nozzles)
                    {
                        if(pc != null && pc.CommandName == "SwitchOff" && switchOffAttemps > 5)
                        {
                            this.SetFuelPointStatus(FuelPointStatusEnum.Offline, -1);
                            switchOffAttemps = 0;
                        }
                        else if(pc != null && DateTime.Now.Subtract(pc.LastAction).TotalMilliseconds > 500)
                        {
                            if(pc.NozzleIndex >= 0 && pc.NozzleIndex != nz.Index)
                                continue;
                            if(pc.CommandName == "SwitchOff")
                            {
                                switchOffAttemps++;
                            }
                            pc.LastAction = DateTime.Now;
                            if (pc.CommandName == "GetTotals")
                            {
                                byte[] CMDTotal = Commands.VolumeTotal(nz.Index);
                                ExecuteCommand(CMDTotal, "GetTotals");
                            }
                            else if (pc.CommandName == "NeedDisplay")
                            {
                                byte[] cmd = Commands.GetDisplay(nz.Index);
                                ExecuteCommand(cmd, "NeedDisplay");
                            }
                            else if (pc.CommandName == "Authorise")
                            {
                                byte[] cmd = Commands.Authorise(nz.Index, fp.ActiveNozzle.UntiPriceInt);
                                ExecuteCommand(cmd, "Authorise");
                            }
                            else if (pc.CommandName == "SetPrice")
                            {
                                byte[] cmd = Commands.SetPrice(nz.Index, nz.UntiPriceInt);
                                ExecuteCommand(cmd, "SetPrice");
                            }
                            else if (pc.CommandName == "SwitchOff")
                            {
                                byte[] cmd = Commands.PowerONOFF(nz.Index);
                                ExecuteCommand(cmd, "SwitchOff");
                                
                            }
                            else if (pc.CommandName == "Halt")
                            {
                                byte[] cmd = Commands.Halt(nz.Index);
                                ExecuteCommand(cmd, "Halt");
                            }
                            return true;
                        }
                    }
                }
                return false;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        private bool CheckLRC(byte[] dataBuf)
        {
            if (dataBuf.Length < 4)
                return false;
            byte[] dataforlrc = dataBuf.Skip(1).Take(dataBuf.Length - 5).ToArray();
            byte[] LRCArray = Encoding.ASCII.GetBytes(Commands.LRC(dataforlrc).ToString("X2"));
            byte[] LRCDATABUF = dataBuf.Skip(dataBuf.Length - 4).Take(2).ToArray();
            if (LRCArray.SequenceEqual(LRCDATABUF))
                return true;
            return false;

        }
        private void RecognizeCommand(byte[] Command)
        {
            if (Command.Length < 2)
                return;
            char operation = (char)Command[1];
            switch (operation)
            {
                case 'A':
                case 'a':
                    this.EvaluateNozzle(Command);
                    break;
                case 'B':
                    return;
                case 'C':
                    this.EvaluateWork(Command);
                    break;
                case 'D':
                    this.EvaluateNozzleBack(Command);
                    break;
                case 'E':
                    return;
                case 'e':
                    this.EvaluateDisplay(Command);
                    break;
                case 'f':
                    return;
                case 'H':
                    return;
                case 'k':
                    return;
                case 'L':
                    return;
                case 'l':
                    ExecuteCommand(Commands.Acknowledge(), "PowerOn");
                    break;
                case 'b':
                    return;
                case 'O':
                    ExecuteCommand(Commands.Acknowledge(), "PowerOn");
                    break;
                case 'N':
                    return;
                case 'W':
                    this.EvaluateVolumeTotals(Command);
                    break;
            }

        }


        //private void EvaluateData()
        //{
        //    int currentIndex = 0;
        //    if (DataBuffer.Count == 0)
        //        return;
        //    lock (DataBuffer)
        //    {
        //        while (currentIndex >= 0)
        //        {
        //            int posSTX = DataBuffer.IndexOf((byte)58, currentIndex);
        //            if (posSTX < 0)
        //            {
        //                currentIndex = -1;
        //                break;
        //            }
        //            int posETX1 = DataBuffer.IndexOf((byte)13, posSTX);
        //            int posETX2 = -1;
        //            if (posETX1 >= 0)
        //            {
        //                if (DataBuffer.Count > posETX1 + 1 && DataBuffer[posETX1 + 1] == (byte)10)
        //                    posETX2 = posETX1 + 1;
        //                else
        //                {
        //                    currentIndex = posETX1 + 1;
        //                    continue;
        //                };

        //            }
        //            else
        //                break;
        //            byte[] bytcom = DataBuffer.Skip(posSTX).Take(posETX2 - posSTX+1).ToArray();
        //            RecognizeCommand(bytcom);
        //            currentIndex = posETX2 + 1;
        //        }
        //        DataBuffer = DataBuffer.Skip(currentIndex).ToList();
        //    }
        //}

        private void EvaluateData()
        {
            try
            {
                Logger(this.CommunicationPort, DataBuffer.ToArray(), "RawData");


                int currentIndex = 0;
                if (DataBuffer.Count == 0)
                    return;
                lock (DataBuffer)
                {
                    while (currentIndex >= 0)
                    {
                        int posSTX = DataBuffer.IndexOf((byte)58, currentIndex);
                        if (posSTX < 0)
                            break;
                        int posETX1 = DataBuffer.IndexOf((byte)13, posSTX);
                        if (posETX1 + 1 < DataBuffer.Count)
                        {
                            int posETX2 = posETX1 + 1;
                            byte[] bufRecognize = DataBuffer.Skip(posSTX).Take(posETX2).ToArray();
                            currentIndex = posETX2;
                            RecognizeCommand(bufRecognize);
                            break;
                        }
                        else
                            break;
                    }
                    DataBuffer = DataBuffer.Skip(currentIndex).ToList();
                }
            }
            catch(Exception e)
            {

            }
        }

        #region  Evaluates
        string volString, upString;
        private void EvaluateDisplay(byte[] command)
        {
            SendAK();
            //Logger(command, "EvaluateDisplay");
            string[] parms = null;
            int NozzleNum = 0;
            if (command.Length == 17)
            {
                parms = BitConverter.ToString(command).Split('-');                
                volString =

                         parms[2].Substring(1)
                        + parms[3].Substring(1)
                        + parms[4].Substring(1)
                        + parms[5].Substring(1)
                        + parms[6].Substring(1);

                upString =
                    parms[7].Substring(1) +
                     parms[8].Substring(1)
                    + parms[9].Substring(1)
                    + parms[10].Substring(1)
                    + parms[11].Substring(1);

                NozzleNum = int.Parse(parms[12])-31;
            }
            else if (command.Length == 19)
            {
                //:e00000700000915A
                parms = BitConverter.ToString(command).Split('-');
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
                NozzleNum = int.Parse(parms[14])-31;
            }


            this.fuelPoints[0].Nozzles[NozzleNum].ParentFuelPoint.DispensedAmount = decimal.Parse(upString) / 100;
            this.fuelPoints[0].Nozzles[NozzleNum].ParentFuelPoint.DispensedVolume = decimal.Parse(volString) / 100;


            if (this.DataChanged != null)
            {

                Common.FuelPointValues values = new Common.FuelPointValues();
                values.CurrentSalePrice = this.fuelPoints[0].Nozzles[NozzleNum].UnitPrice;
                values.CurrentPriceTotal = this.fuelPoints[0].DispensedAmount;
                values.CurrentVolume = this.fuelPoints[0].DispensedVolume;


                this.DataChanged(this, new Common.FuelPointValuesArgs()
                {
                    CurrentFuelPoint = this.fuelPoints[0],
                    CurrentNozzleId = this.fuelPoints[0].Nozzles[NozzleNum].Index,
                    Values = values
                });
            }
            System.Threading.Thread.Sleep(50);
        }


        private void EvaluateVolumeTotals(byte[] command)
        {
            if (!CheckLRC(command))
            {
                SendNAK();
                return;
            }
            SendAK();
            int NozzleNum = command[2] - 49;
            string[] strArrays1 = BitConverter.ToString(command).Split(new char[] { '-' });
            string str1 = string.Concat(new string[] { strArrays1[3].Substring(1), strArrays1[4].Substring(1), strArrays1[5].Substring(1), strArrays1[6].Substring(1), strArrays1[7].Substring(1), strArrays1[8].Substring(1), strArrays1[9].Substring(1), strArrays1[10].Substring(1) });
            decimal totals = decimal.Parse(str1);
            if (totals == 0)
                totals = 1;
            this.fuelPoints[0].Nozzles[NozzleNum].TotalVolume = totals;
            
            if (this.TotalsRecieved != null)
            {
                this.TotalsRecieved(this, new TotalsEventArgs(this.fuelPoints[0], NozzleNum + 1, totals, 0));
            }
            this.fuelPoints[0].Nozzles[NozzleNum].QueryTotals = false;
        }
        private void SendAK()
        {
            ExecuteCommand(Commands.Acknowledge(), "AK");
        }

        private void SendNAK()
        {
            ExecuteCommand(Commands.NotAcknowledge(), "NAK");
        }

        private void EvaluateNozzle(byte[] Command)
        {
            //:a150<cr><lF>
            if (Command.Length != 7)
            {
                SendNAK();
                return;
            }
            else
            {
                if (!CheckLRC(Command))
                    return;
                SendAK();
                int NozzleNum = ((int)Command[2] - 49);


                FuelPointValues fpvalues = new FuelPointValues();
                fpvalues.ActiveNozzle = NozzleNum;
                this.fuelPoints[0].ActiveNozzleIndex = NozzleNum;
                fpvalues.Status = FuelPointStatusEnum.Nozzle;

                this.fuelPoints[0].Status = FuelPointStatusEnum.Nozzle;
                this.fuelPoints[0].DispenserStatus = FuelPointStatusEnum.Nozzle;

                if (this.DispenserStatusChanged != null)
                {
                    this.DispenserStatusChanged(this, new FuelPointValuesArgs()
                    {
                        CurrentFuelPoint = this.fuelPoints[0],
                        CurrentNozzleId = NozzleNum,
                        Values = fpvalues
                    });
                }
            }
        }
        private void EvaluateWork(byte[] Command)
        {
            //:a150<cr><lF>
            if (Command.Length != 6)
                return;
            else
            {
                if (!CheckLRC(Command))
                {
                    SendNAK();
                    return;
                }
                SendAK();
                int NozzleNum = (int)Command[2] - 49;


                if (this.DispenserStatusChanged != null)
                {
                    FuelPointValues fpvalues = new FuelPointValues();
                    fpvalues.ActiveNozzle = NozzleNum;
                    fpvalues.Status = FuelPointStatusEnum.Work;
                    this.fuelPoints[0].Status = FuelPointStatusEnum.Work;
                    this.fuelPoints[0].DispenserStatus = FuelPointStatusEnum.Work;
                    this.DispenserStatusChanged(this, new FuelPointValuesArgs()
                    {
                        CurrentFuelPoint = this.fuelPoints[0],
                        CurrentNozzleId = NozzleNum,
                        Values = fpvalues
                    });
                }
            }
        }
        private void EvaluateNozzleBack(byte[] Command)
        {
            //:a150<cr><lF>
            if (Command.Length != 6)
                return;
            else
            {
                if (!CheckLRC(Command))
                {
                    SendNAK();
                    return;
                }

                int NozzleNum = -1;
                if (this.fuelPoints[0].ActiveNozzle != null && this.fuelPoints[0].ActiveNozzle.GetExtendedProperty("LastPendingCommand", "").ToString() != "")
                    return;

                SendAK();
                int LastActiveNozzle = this.fuelPoints[0].ActiveNozzleIndex;
                if (this.DispenserStatusChanged != null)
                {

                    FuelPointValues fpvalues = new FuelPointValues();
                    fpvalues.ActiveNozzle = NozzleNum;
                    fpvalues.Status = FuelPointStatusEnum.Idle;
                    
                    this.fuelPoints[0].ActiveNozzleIndex = NozzleNum;
                    this.fuelPoints[0].Status = FuelPointStatusEnum.Idle;
                    this.fuelPoints[0].DispenserStatus = FuelPointStatusEnum.Idle;
                    this.DispenserStatusChanged(this, new FuelPointValuesArgs()
                    {
                        CurrentFuelPoint = this.fuelPoints[0],
                        CurrentNozzleId = NozzleNum,
                        Values = fpvalues
                    });

                }
                this.fuelPoints[0].SetExtendedProperty("LastPendingCommand",
                    new PendingCommand()
                    {
                        CommandName = "GetTotals",
                        NozzleIndex = LastActiveNozzle
                    });
            }
        }
        #endregion

        private void ExecuteCommand(byte[] command, string method)
        {
            //Thread.Sleep(50);
            this.SerPort.Write(command, 0, command.Length);
            Logger(this.SerPort.PortName, command, method);
        }
        public static void Logger(string comport, byte[] data, string VoidMethodName)
        {
            string fileName = "Petrotec_" + comport + "_LOG.txt";
            if (!System.IO.File.Exists(fileName))
                return;
            using (StreamWriter writer = new StreamWriter(fileName, true, Encoding.UTF8))
            {
                writer.Write("-->" + VoidMethodName + "  " + DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss.fff") + "   <-- " + BitConverter.ToString(data) + " | " + Encoding.UTF8.GetString(data) + "\r\n");
            }
        }

        public static void Logger(string comport, string message)
        {
            string fileName = "Petrotec_" + comport + "_LOG.txt";
            if (!System.IO.File.Exists(fileName))
                return;
            string msg = string.Format("Exception: {0}, {1:dd/MM/yyyy HH:mm:ss.fff}", message, DateTime.Now);
            System.IO.File.WriteAllText(fileName, msg);
        }
    }

    public class PendingCommand
    {
        public PendingCommand()
        {
            this.LastAction = DateTime.Now;
        }
        public string CommandName
        {
            get; set;
        }
        public int NozzleIndex
        {
            get;
            set;
        }
        public DateTime LastAction
        {
            get;
            set;
        }
    }

}
