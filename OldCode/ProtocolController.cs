using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ASFuelControl.Common;
using System.Collections;
using ASFuelControl.Common.Enumerators;

namespace ASFuelControl.Box69
{
    public class ProtocolController : Common.IFuelProtocol
    {
        private System.IO.Ports.SerialPort serialPort = new System.IO.Ports.SerialPort();
        private System.Threading.Thread th = null;
        private List<OpenCommand> commands = new List<OpenCommand>();
        private bool errorOccured = false;

        #region IFuelProtocol Interface

        public event EventHandler<Common.FuelPointValuesArgs> DataChanged;

        public event EventHandler<Common.TotalsEventArgs> TotalsRecieved;

        public event EventHandler<Common.FuelPointValuesArgs> DispenserStatusChanged;

        public string CommunicationPort { set; get; }

        public Common.FuelPoint[] FuelPoints { set; get; }

        public bool IsConnected
        {
            get
            {
                if (this.serialPort == null)
                    return false;
                return this.serialPort.IsOpen;
            }
        }

        public void Connect()
        {
            this.serialPort.PortName = this.CommunicationPort;
            //this.serialPort.DataReceived -= new System.IO.Ports.SerialDataReceivedEventHandler(serialPort_DataReceived);
            //this.serialPort.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(serialPort_DataReceived);
            //this.serialPort.DtrEnable = true;
            //this.serialPort.Handshake = System.IO.Ports.Handshake.RequestToSend;
            this.serialPort.Open();
            th = new System.Threading.Thread(new System.Threading.ThreadStart(this.ThreadRun));
            th.Start();
        }

        public void Disconnect()
        {
            this.serialPort.Close();
        }

        public void StopFuelPoint(Common.FuelPoint fp)
        {
            throw new NotImplementedException();
        }

        public void GetNozzleTotals(Common.Nozzle nozzle)
        {
            nozzle.ParentFuelPoint.QueryTotals = true;
        }

        public bool InitializeFuelPoint(Common.FuelPoint fp)
        {
            throw new NotImplementedException();
        }

        public bool SetPrices(Common.FuelPoint fp)
        {
            fp.QuerySetPrice = true;
            return true;
        }

        public int GetId(Common.FuelPoint fp)
        {
            throw new NotImplementedException();
        }

        public void AuthoriseFuelPoint(Common.FuelPoint fp, decimal price, decimal presetMoney, decimal presetVolume)
        {
            fp.QueryAuthorize = true;
        }

        public Common.Nozzle GetActiveNozzle(Common.FuelPoint fp)
        {
            throw new NotImplementedException();
        }

        public void GetFuelingPointData(Common.FuelPoint fp)
        {
            throw new NotImplementedException();
        }

        public void GetNozzleData(Common.Nozzle fp)
        {
            throw new NotImplementedException();
        }

        #endregion

        DateTime lastOffline = DateTime.MinValue;
        private void ThreadRun()
        {
            while (this.IsConnected)
            {
                if (!(this.serialPort.IsOpen || errorOccured))
                    break;
                try
                {
                    foreach (Common.FuelPoint fp in this.FuelPoints)
                    {
                        OpenCommand command = fp.CreateNextCommand();
                        if (command != null)
                            this.commands.Add(command);
                    }
                    //foreach (OpenCommand cmd in this.commands)
                    //{
                    //    System.Diagnostics.Trace.WriteLine("COMMAND :" + cmd.CommandType.ToString() + " --- " + cmd.Fpoint.AddressId.ToString() + " -- " + cmd.CommandString);
                    //}
                    DateTime dtStart = DateTime.Now;
                    List<OpenCommand> toRemove = new List<OpenCommand>();
                    foreach (OpenCommand cmd in this.commands)
                    {


                        //this.serialPort.DiscardInBuffer();
                        //this.serialPort.DiscardOutBuffer();

                        DateTime dt = DateTime.Now;
                        this.serialPort.Write(cmd.Command, 0, cmd.Command.Length);
                        if (Properties.Settings.Default.LogCommunication)
                        {
                            System.IO.File.AppendAllLines("Box69Log.txt", new List<string>() { cmd.CommandString });
                        }
                        while (DateTime.Now.Subtract(dt).TotalMilliseconds < cmd.MinimumTimeNeeded)
                        {
                            System.Threading.Thread.Sleep(10);
                        }
                        DateTime dtNow = DateTime.Now;
                        while (this.serialPort.BytesToRead < cmd.ResponseLength && DateTime.Now.Subtract(dtNow).TotalMilliseconds < 50)
                        {
                            System.Threading.Thread.Sleep(50);
                        }
                        if (this.serialPort.BytesToRead < cmd.ResponseLength)
                        {
                            //System.Diagnostics.Trace.WriteLine("NO DATA :" + this.serialPort.BytesToRead.ToString()  + " - " +  cmd.ResponseLength.ToString());
                            if (this.serialPort.BytesToRead == 0)
                            {
                                if (Properties.Settings.Default.LogCommunication)
                                {
                                    System.IO.File.AppendAllLines("Box69Log.txt", new List<string>() { "NO ANSWER" });
                                }

                                OpenCommand cmd2 = cmd.Fpoint.GetId();

                                this.serialPort.Write(cmd2.Command, 0, cmd2.Command.Length);
                                if (Properties.Settings.Default.LogCommunication)
                                {
                                    System.IO.File.AppendAllLines("Box69Log.txt", new List<string>() { cmd2.CommandString });
                                }
                                System.Threading.Thread.Sleep(100);
                                Byte[] dummy = new byte[this.serialPort.BytesToRead];
                                this.serialPort.Read(dummy, 0, this.serialPort.BytesToRead);
                                if (DateTime.Now.Subtract(this.lastOffline).TotalSeconds > 2)
                                {
                                    cmd.Fpoint.DispenserStatus = FuelPointStatusEnum.Offline;
                                    this.lastOffline = DateTime.Now;
                                }
                                continue;
                            }
                            //OpenCommand deqComd = null;
                            toRemove.Add(cmd);
                            //this.commands.Remove(deqComd);
                            System.Threading.Thread.Sleep(100);
                            this.serialPort.DiscardInBuffer();
                            this.serialPort.DiscardOutBuffer();
                            continue;
                        }
                        if ((bool)cmd.Fpoint.GetExtendedProperty("NoAnswer", false))
                            cmd.Fpoint.SetExtendedProperty("NoAnswer", false);
                        Byte[] buffer = new byte[this.serialPort.BytesToRead];
                        //System.Diagnostics.Trace.WriteLine("Commad Write :" + this.commands.Count().ToString() + " --- " +  cmd.CommandType.ToString() + " --- " + cmd.Fpoint.AddressId.ToString() + " -- " + cmd.CommandString);
                        this.serialPort.Read(buffer, 0, this.serialPort.BytesToRead);
                        byte[] response = buffer.Take(cmd.ResponseLength).ToArray();

                        if (!this.CheckBuffer(response))
                        {
                            System.Diagnostics.Trace.WriteLine(cmd.CommandType);
                            toRemove.Add(cmd);
                            System.Diagnostics.Trace.WriteLine("Error Data :");

                            if (Properties.Settings.Default.LogCommunication)
                            {
                                System.IO.File.AppendAllLines("Box69Log.txt", new List<string>() { BitConverter.ToString(response) });
                            }

                            continue;
                        }

                        response = this.GetBuffer(response);

                        if (Properties.Settings.Default.LogCommunication)
                        {
                            System.IO.File.AppendAllLines("Box69Log.txt", new List<string>() { BitConverter.ToString(response) });
                        }

                        //System.Diagnostics.Trace.WriteLine("Commad Read :"+ cmd.CommandType.ToString() + " --- " + cmd.Fpoint.AddressId.ToString() + " -- " + BitConverter.ToString(response));
                        switch (cmd.CommandType)
                        {
                            case CommandTypeEnum.RequestStatus:
                            case CommandTypeEnum.SendMainDisplayData:
                                Common.Enumerators.FuelPointStatusEnum newStatus = this.EvaluateStatus(response, cmd.Fpoint.DispenserStatus);

                                cmd.Fpoint.SetExtendedProperty("InitializeSent", true);

                                if (cmd.CommandType == CommandTypeEnum.RequestStatus && newStatus == Common.Enumerators.FuelPointStatusEnum.Nozzle && cmd.Fpoint.ActiveNozzle == null)
                                    cmd.Fpoint.QueryNozzle = true;
                                else
                                    cmd.Fpoint.DispenserStatus = newStatus;
                                if (newStatus != FuelPointStatusEnum.Nozzle)
                                    cmd.Fpoint.QueryAuthorize = false;

                                break;
                            case CommandTypeEnum.Halt:
                                Common.Enumerators.FuelPointStatusEnum newStatus1 = this.EvaluateStatus(response, cmd.Fpoint.DispenserStatus);
                                cmd.Fpoint.DispenserStatus = newStatus1;
                                cmd.Fpoint.QueryStop = false;
                                break;
                            case CommandTypeEnum.Authorize:
                                Common.Enumerators.FuelPointStatusEnum newStatus2 = this.EvaluateStatus(response, cmd.Fpoint.DispenserStatus);
                                cmd.Fpoint.DispenserStatus = newStatus2;
                                cmd.Fpoint.QueryAuthorize = false;
                                break;
                            case CommandTypeEnum.RequestActiveNozzle:
                                cmd.Fpoint.ActiveNozzle = this.EvaluateActiveNozzle(cmd.Fpoint, response);
                                cmd.Fpoint.DispenserStatus = Common.Enumerators.FuelPointStatusEnum.Nozzle;
                                cmd.Fpoint.QueryNozzle = false;
                                break;
                            case CommandTypeEnum.SendPrices:
                                if (response[0] != 0xB0)
                                {
                                    //continue;
                                }
                                cmd.Fpoint.SetExtendedProperty("PriceSet", true);
                                cmd.Fpoint.QuerySetPrice = false;
                                cmd.Fpoint.SetExtendedProperty("PriceSetSent", true);
                                break;
                            case CommandTypeEnum.RequestTotals:
                                if (cmd.CurrentNozzle != null)
                                {
                                    bool ignore = false;
                                    if (cmd.Fpoint.ErrorCount > 2)
                                        ignore = true;
                                    bool ret = this.EvaluateTotals(cmd.CurrentNozzle, response, ignore);
                                    if (ret)
                                    {
                                        cmd.Fpoint.QueryTotals = false;
                                        cmd.Fpoint.ErrorCount = 0;
                                    }
                                    else
                                    {
                                        cmd.Fpoint.ErrorCount++;
                                    }
                                }

                                break;
                            case CommandTypeEnum.RequestID:
                                if (cmd.Fpoint.QueryTotals)
                                {
                                    Nozzle nz = cmd.Fpoint.LastActiveNozzle;
                                    if (cmd.Fpoint.ActiveNozzle != null)
                                        nz = cmd.Fpoint.ActiveNozzle;
                                    cmd.Fpoint.QueryTotals = false;
                                    nz.TotalVolume = nz.TotalVolume + (nz.CurrentSale.TotalVolume * 100);
                                    nz.TotalPrice = nz.TotalPrice + (nz.CurrentSale.TotalPrice * 100);
                                    if (this.TotalsRecieved != null)
                                        this.TotalsRecieved(cmd.Fpoint, new TotalsEventArgs(cmd.Fpoint, nz.Index, nz.TotalVolume, nz.TotalPrice));
                                }
                                break;
                            case CommandTypeEnum.RequestDisplayData:
                                if (cmd.CurrentNozzle != null)
                                    this.EvaluateDisplayData(cmd.CurrentNozzle, response);
                                break;
                        }
                        toRemove.Add(cmd);
                        //this.commands.TryDequeue(out this.curCommand);

                    }
                    //foreach (OpenCommand cmd in toRemove)
                    this.commands.Clear();
                    DateTime dtEnd = DateTime.Now;
                    int millisecs = (int)dtEnd.Subtract(dtStart).TotalMilliseconds;
                    if (millisecs < 250)
                        System.Threading.Thread.Sleep(500 - millisecs);
                }
                catch (Exception ex)
                {
                    errorOccured = true;
                    System.Threading.Thread.Sleep(100);
                    System.Diagnostics.Trace.WriteLine(ex.Message);
                    if (!this.serialPort.IsOpen)
                        return;
                }
            }
        }

        private bool CheckBuffer(byte[] buffer)
        {
            if (buffer.Length % 2 != 0)
                return false;
            for (int i = 0; i < buffer.Length / 2; i++)
            {
                int j = (2 * i);
                if (buffer[j] + buffer[j + 1] != 255)
                    return false;
            }
            return true;
        }

        private byte[] GetBuffer(byte[] buffer)
        {
            if (buffer.Length % 2 != 0)
                return new byte[] { };
            List<byte> newBuffer = new List<byte>();
            for (int i = 0; i < buffer.Length / 2; i++)
            {
                int j = (2 * i);
                newBuffer.Add(buffer[j]);
            }
            return newBuffer.ToArray();
        }

        private int ToNumeral(BitArray binary, int length)
        {
            int numeral = 0;
            for (int i = 0; i < length; i++)
            {
                if (binary[i])
                {
                    numeral = numeral | (((int)1) << (length - 1 - i));
                }
            }
            return numeral;
        }

        private Common.Enumerators.FuelPointStatusEnum EvaluateStatus(byte[] buffer, Common.Enumerators.FuelPointStatusEnum defStatus)
        {
            byte b1 = buffer[0];
            if (b1 == 0x2F)
                return Common.Enumerators.FuelPointStatusEnum.Offline;
            
            else if (b1 == 0x20)
                return Common.Enumerators.FuelPointStatusEnum.Idle;
            else if (b1 == 0xA0)
                return Common.Enumerators.FuelPointStatusEnum.Nozzle;
            else if (b1 == 0x90)
                return Common.Enumerators.FuelPointStatusEnum.Ready;
            else if (b1 == 0xD0)
                return Common.Enumerators.FuelPointStatusEnum.Work;
            else if (b1 == 0xF0)
                return Common.Enumerators.FuelPointStatusEnum.Work;
            else if (b1 == 0x91)
                return Common.Enumerators.FuelPointStatusEnum.TransactionStopped;
            else if (b1 == 0x92)
                return Common.Enumerators.FuelPointStatusEnum.TransactionStopped;
            else if (b1 == 0x99)
                return Common.Enumerators.FuelPointStatusEnum.TransactionStopped;


            return defStatus;
        }

        private Nozzle EvaluateActiveNozzle(FuelPoint fp, byte[] buffer)
        {
            BitArray answer = new BitArray(new byte[] { buffer[0] });
            BitArray nozzleNrArray = new BitArray(7);
            nozzleNrArray.Set(0, answer.Get(6));
            nozzleNrArray.Set(1, answer.Get(5));
            nozzleNrArray.Set(2, answer.Get(4));
            nozzleNrArray.Set(3, answer.Get(3));
            nozzleNrArray.Set(4, answer.Get(2));
            nozzleNrArray.Set(5, answer.Get(1));
            nozzleNrArray.Set(6, answer.Get(0));
            int nozzleIndex = this.ToNumeral(nozzleNrArray, 7);
            nozzleIndex = nozzleIndex - 1;
            if (nozzleIndex < 0)
                return null;
            return fp.Nozzles[nozzleIndex];
        }

        private bool EvaluateTotals(Nozzle nozzle, byte[] buffer, bool ignoreError)
        {
            byte[] volumeBuffer = buffer.Take(5).ToArray();
            volumeBuffer = volumeBuffer.Reverse().ToArray();
            byte[] priceBuffer = buffer.Skip(5).Take(5).ToArray();
            priceBuffer = priceBuffer.Reverse().ToArray();

            byte[] status = buffer.Skip(10).Take(1).ToArray();
            string volString = BitConverter.ToString(volumeBuffer).Replace("-", "");// volumeBuffer[0].ToString() + volumeBuffer[1].ToString() + volumeBuffer[2].ToString() + volumeBuffer[3].ToString() + volumeBuffer[4].ToString();
            string priceString = BitConverter.ToString(priceBuffer).Replace("-", ""); //priceBuffer[0].ToString() + priceBuffer[1].ToString() + priceBuffer[2].ToString() + priceBuffer[3].ToString() + priceBuffer[4].ToString();
            nozzle.ParentFuelPoint.DispenserStatus = this.EvaluateStatus(status, nozzle.ParentFuelPoint.DispenserStatus);
            decimal newTotal = decimal.Parse(volString);
            decimal newPrice = decimal.Parse(priceString);

            Console.WriteLine("TOTALS Bytes : {2}, String : {0},  Value : {1}", volString, newTotal, BitConverter.ToString(volumeBuffer));


            if (nozzle.TotalVolume > 0 && (nozzle.TotalVolume + (nozzle.CurrentSale.TotalVolume * 100) != newTotal) && !ignoreError)
                return false;
            nozzle.TotalVolume = newTotal;

            if (this.TotalsRecieved != null)
                this.TotalsRecieved(nozzle.ParentFuelPoint, new TotalsEventArgs(nozzle.ParentFuelPoint, nozzle.Index, newTotal, newPrice));
            return true;
        }

        private void EvaluateDisplayData(Nozzle nozzle, byte[] buffer)
        {
            byte[] unitPrice = buffer.Take(2).ToArray();
            unitPrice = unitPrice.Reverse().ToArray();
            byte[] priceBuffer = buffer.Skip(2).Take(3).ToArray();
            priceBuffer = priceBuffer.Reverse().ToArray();
            byte[] volumeBuffer = buffer.Skip(5).Take(3).ToArray();
            volumeBuffer = volumeBuffer.Reverse().ToArray();


            byte[] status = buffer.Skip(8).Take(1).ToArray();
            string upString = BitConverter.ToString(unitPrice).Replace("-", "");// unitPrice[0].ToString() + unitPrice[1].ToString();
            string volString = BitConverter.ToString(volumeBuffer).Replace("-", "");// volumeBuffer[0].ToString() + volumeBuffer[1].ToString() + volumeBuffer[2].ToString();
            string priceString = BitConverter.ToString(priceBuffer).Replace("-", "");// priceBuffer[0].ToString() + priceBuffer[1].ToString() + priceBuffer[2].ToString();

            nozzle.ParentFuelPoint.DispenserStatus = this.EvaluateStatus(status, nozzle.ParentFuelPoint.DispenserStatus);
            nozzle.CurrentSale.UnitPrice = decimal.Parse(upString) / (decimal)Math.Pow(10, nozzle.ParentFuelPoint.UnitPriceDecimalPlaces);
            nozzle.CurrentSale.TotalVolume = decimal.Parse(volString) / (decimal)Math.Pow(10, nozzle.ParentFuelPoint.DecimalPlaces);
            nozzle.CurrentSale.TotalPrice = decimal.Parse(priceString) / 100;

            if (this.DataChanged != null)
            {
                FuelPointValuesArgs args = new FuelPointValuesArgs();
                args.CurrentFuelPoint = nozzle.ParentFuelPoint;
                args.CurrentNozzleId = nozzle.Index;
                args.Values = new FuelPointValues();
                args.Values.CurrentVolume = nozzle.CurrentSale.TotalVolume;
                args.Values.CurrentPriceTotal = nozzle.CurrentSale.TotalPrice;
                args.Values.CurrentSalePrice = nozzle.CurrentSale.UnitPrice;
                args.Values.ActiveNozzle = nozzle.Index - 1;
                args.Values.LastNozzle = nozzle.ParentFuelPoint.LastActiveNozzleIndex;
                
                this.DataChanged(this, args);
            }
        }
    }
}
