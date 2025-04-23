using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeosisController
{
    public class TeosisConnector
    {
        private System.IO.Ports.SerialPort serialPort = new System.IO.Ports.SerialPort();
        private double speed = 0;
        private System.Threading.Thread th;
        private List<ExecutionCommand> executionCommands = new List<ExecutionCommand>();
        private byte[] readBuffer = new byte[] { };
        private int lastTX = 0;

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
                this.serialPort.BaudRate = 9600;
                this.serialPort.PortName = this.CommunicationPort;
                this.serialPort.Parity = System.IO.Ports.Parity.Odd;
                this.speed = 1 / (serialPort.BaudRate / 8000);
                this.serialPort.Open();
                this.th = new System.Threading.Thread(new System.Threading.ThreadStart(ThreadRun));
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

        public void ExecuteGetStatus(int fpAddress)
        {
            var cmd = this.CreateCommand(fpAddress, ExecutionCommand.GETPUMPSTATUS);
            if (cmd == null)
                return;
            cmd.CreateCommand(0);
        }

        public void ExecutePaid(int fpAddress)
        {
            var cmd = this.CreateCommand(fpAddress, ExecutionCommand.PAID);
            if (cmd == null)
                return;
            cmd.CreateCommand(0);
        }

        public void ExecuteFillingReport(int fpAddress, int nozzleIdx)
        {
            var cmd = this.CreateCommand(fpAddress, ExecutionCommand.FILLINGREPORT);
            if (cmd == null)
                return;
            cmd.NozzleIndex = nozzleIdx;
            cmd.CreateCommand(0);
        }

        public void ExecuteGetTotals(int fpAddress, int nozzleIdx)
        {
            var cmd = this.CreateCommand(fpAddress, ExecutionCommand.GETTOTALS);
            if (cmd == null)
                return;
            cmd.NozzleIndex = nozzleIdx;
            cmd.CreateCommand(0);
        }

        public void ExecuteSetPrice(int fpAddress, int nozzleIdx, decimal price)
        {
            var cmd = this.CreateCommand(fpAddress, ExecutionCommand.SETPRICE);
            if (cmd == null)
                return;
            cmd.UnitPriceInt = int.Parse(price.ToString("N3").Replace(".", "").Replace(",", ""));
            cmd.NozzleIndex = nozzleIdx;
            cmd.CreateCommand(0);
        }

        public void ExecuteAuthorize(int fpAddress, int nozzleIdx)
        {
            var cmd = this.CreateCommand(fpAddress, ExecutionCommand.AUTHORIZE);
            if (cmd == null)
                return;
            cmd.NozzleIndex = nozzleIdx;
            cmd.PresetAmount = -1;
            cmd.PresetVolume = -1;
            cmd.CreateCommand(0);
        }

        public void ExecuteAuthorizeVolume(int fpAddress, int nozzleIdx, int volume)
        {
            var cmd = this.CreateCommand(fpAddress, ExecutionCommand.AUTHORIZE);
            if (cmd == null)
                return;
            cmd.PresetAmount = -1;
            cmd.PresetVolume = volume;
            cmd.CreateCommand(0);
        }

        public void ExecuteAuthorizeAmount(int fpAddress, int nozzleIdx, int amount)
        {
            var cmd = this.CreateCommand(fpAddress, ExecutionCommand.AUTHORIZE);
            if (cmd == null)
                return;
            cmd.PresetAmount = amount;
            cmd.PresetVolume = -1;
            cmd.CreateCommand(0);
        }

        public void ExecuteDCRStatus()
        {
            var cmd = this.CreateCommand(1, ExecutionCommand.GETDCRTATUS);
            if (cmd == null)
                return;
            cmd.CreateCommand(0);
        }

        private ExecutionCommand CreateCommand(int address, byte cmdByte)
        {
            var command = executionCommands.FirstOrDefault(e => e.CommandByte == cmdByte && e.FuelPumpAddress == address);
            if (command == null)
            {
                command = new ExecutionCommand();
                command.TX = 0;
                command.FuelPumpAddress = address;
                command.CommandByte = cmdByte;
                this.executionCommands.Add(command);
            }
            else
            {
                command.TX = 0;
                command.Status = CommandStatus.Initialized;
                command.NextCommand = command.Request;
            }
            return command;
        }

        private void ThreadRun()
        {
            DateTime lastStatus = DateTime.MinValue;

            while (true)
            {
                if (!this.IsConnected)
                    break;
                try
                {
                    if (DateTime.Now.Subtract(lastStatus).TotalMilliseconds > 250)
                    {
                        if (this.executionCommands.Count == 0)
                        {
                            ExecuteGetStatus(1);
                            lastStatus = DateTime.Now;
                        }
                    }

                    var currentCommand = this.executionCommands.OrderBy(c => c.CreationTimeStamp).FirstOrDefault(c => c.Status == CommandStatus.Initialized);
                    if (currentCommand != null)
                    {
                        while (true)
                        {
                            var status = currentCommand.Status;
                            if (currentCommand.NextCommand != null && currentCommand.NextCommand.Length > 0)
                            {
                                //Console.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff") + " - TX : " + BitConverter.ToString(currentCommand.NextCommand));
                                serialPort.Write(currentCommand.NextCommand, 0, currentCommand.NextCommand.Length);
                                System.Threading.Thread.Sleep(30);
                                currentCommand.SetNewStatus();
                                if(currentCommand.Status != CommandStatus.Poll)
                                    currentCommand.NextCommand = new byte[] { };
                                
                            }
                            ReadData();
                            if(currentCommand.TotalsRecieved)
                            {
                                Console.WriteLine("Totals : {0}", currentCommand.VolumeTotals);
                                currentCommand.TotalsRecieved = false;
                            }
                            if(currentCommand.StatusChanged)
                            {
                                if (currentCommand.DispenserStatus == 8)
                                {
                                    if (this.executionCommands.FirstOrDefault(c => c.CommandByte == ExecutionCommand.FILLINGREPORT) == null)
                                        this.ExecuteFillingReport(currentCommand.FuelPumpAddress, currentCommand.NozzleIndex);
                                }
                                else if (currentCommand.DispenserStatus == 4)
                                {
                                    if (this.executionCommands.FirstOrDefault(c => c.CommandByte == ExecutionCommand.PAID) == null)
                                        this.ExecutePaid(currentCommand.FuelPumpAddress);
                                }
                                Console.WriteLine("Status : {0}", currentCommand.DispenserStatus);
                                currentCommand.StatusChanged = false;
                            }
                            if (currentCommand.ReportRecieved)
                            {
                                Console.WriteLine("Sales Amount : {0}", currentCommand.SalesPrice);
                                Console.WriteLine("Sales Volume : {0}", currentCommand.SalesVolume);
                                Console.WriteLine("Sales Unti Price : {0}", currentCommand.SalesUnitPrice);
                                currentCommand.StatusChanged = false;
                            }
                            if (currentCommand.DisplaySet)
                            {
                                Console.WriteLine("Display : {0}", currentCommand.Display);
                                currentCommand.DisplaySet = false;
                            }
                            if (status == CommandStatus.Executed)
                                break;
                        }
                        this.executionCommands.Remove(currentCommand);
                    }
                    System.Threading.Thread.Sleep(10);
                }
                catch
                {

                }
            }
        }

        private void ReadData()
        {
            if (this.serialPort.BytesToRead == 0)
                return;
            try
            {
                byte[] buffer = new byte[this.serialPort.BytesToRead];

                this.serialPort.Read(buffer, 0, this.serialPort.BytesToRead);
                List<byte> list = new List<byte>(this.readBuffer);
                list.AddRange(buffer);
                this.readBuffer = list.ToArray();
                var responses = this.SplitResponses();
                foreach (var resp in responses)
                {
                    //Console.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff") + " - RX : " + BitConverter.ToString(resp));
                    EvaluateResponse(resp);
                }
            }
            catch(Exception ex)
            {

            }
        }

        private void EvaluateResponse(byte[] response)
        {
            try
            {
                if (response.Length < 3)
                    return;

                int address = (int)response[0] - 80;
                byte masterControlByte = (byte)((int)response[1] >> 4);
                var tx = (byte)(response[1] & 0x0F);
                var cmd = this.executionCommands.FirstOrDefault(c => c.DCRAddress == address);
                if(cmd != null)
                    cmd.EvaluateResponse(response);
            }
            catch(Exception ex)
            {
                
            }
        }

        private byte[][] SplitResponses()
        {
            int lastIndex = 0;
            List<byte[]> responses = new List<byte[]>();
            for (int i = 0; i < this.readBuffer.Length; i++)
            {
                if (readBuffer[i] == 0xFA)
                {
                    var response = readBuffer.Skip(lastIndex).Take(i + 1 - lastIndex).ToArray();
                    responses.Add(response);
                    lastIndex = i + 1;
                }
            }
            readBuffer = readBuffer.Skip(lastIndex).ToArray();
            return responses.ToArray();
        }

        //public Controller()
        //{
        //    //string logBool = ConfigurationManager.AppSettings["LogController"];

        //}
        //public class TeosisConnector
        //{
        //    #region commands

        //    private const byte PRESET = 0x7F;
        //    private const byte GETPUMPSTATUS = 0x80;
        //    private const byte SUSPEND = 0x84;
        //    private const byte RESUME = 0x84;
        //    private const byte GETTOTALS = 0x87;
        //    private const byte AUTHORIZE = 0x88;
        //    private const byte SETPRICE = 0x8C;
        //    private const byte GETDCRTATUS = 0x94;
        //    private const byte GETPRICE = 0x99;

        //    #endregion

        //    #region ControlCharacters
        //    //private static byte POLL = 0x20;
        //    private static byte IAP = 0x40;
        //    // private static byte ACK = 0xC0;
        //    private static byte ACKPOLL = 0xE0;
        //    //private static byte EOT = 0x70;
        //    private byte blockSequence = 0x00;

        //    #endregion

        //    private byte[] readBuffer = new byte[] { };

        //    private byte[] cmd;
        //    private List<byte> buffer = new List<byte>();
        //    public int offset = 0; //product1 Super, Product2 Diese_heat
        //    public event EventHandler DispenserOffline;

        //    int lastTX = 0;
        //    Dictionary<int, ExecutionCommand> executedCommands = new Dictionary<int, ExecutionCommand>();

        //    private System.Threading.Thread th;
        //    private System.IO.Ports.SerialPort serialPort = new System.IO.Ports.SerialPort();
        //    //public Common.DebugValues foo = new Common.DebugValues();

        //    private double speed
        //    {
        //        set;
        //        get;
        //    }

        //    public bool IsConnected
        //    {
        //        get
        //        {
        //            return this.serialPort.IsOpen;
        //        }
        //    }

        //    public string CommunicationPort
        //    {
        //        set;
        //        get;
        //    }

        //    public void Connect()
        //    {
        //        try
        //        {
        //            this.serialPort.BaudRate = 9600;
        //            this.serialPort.PortName = this.CommunicationPort;
        //            this.serialPort.Parity = System.IO.Ports.Parity.Odd;
        //            this.speed = 1 / (serialPort.BaudRate / 8000);
        //            this.serialPort.Open();
        //            //this.serialPort.DataReceived -= SerialPort_DataReceived;
        //            //this.serialPort.DataReceived += SerialPort_DataReceived;
        //            this.th = new System.Threading.Thread(new System.Threading.ThreadStart(ThreadRun));
        //            th.Start();
        //        }
        //        catch
        //        {
        //        }
        //    }

        //    public void GetStatus(int address)
        //    {
        //        this.SendCommand(GETPUMPSTATUS, 1, 0);
        //    }

        //    //private void SerialPort_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        //    //{


        //    //}

        //    private byte[][] SplitResponses()
        //    {
        //        int lastIndex = 0;
        //        List<byte[]> responses = new List<byte[]>();
        //        for(int i=0; i < this.readBuffer.Length; i++)
        //        {
        //            if(readBuffer[i] == 0xFA)
        //            {
        //                var response = readBuffer.Skip(lastIndex).Take(i + 1 - lastIndex).ToArray();
        //                responses.Add(response);
        //                lastIndex = i + 1;
        //            }
        //        }
        //        readBuffer = readBuffer.Skip(lastIndex).ToArray();
        //        return responses.ToArray();
        //    }

        //    private void SendCommand(byte cmdByte, int address, int nozzle)
        //    {

        //        ExecutionCommand command = new ExecutionCommand();
        //        switch(cmdByte)
        //        {
        //            case GETPUMPSTATUS:
        //                var cmd = new List<byte> { ADR(), CTR(MasterControlByte.data, this.lastTX), cmdByte, 0x01 };
        //                cmd.Add(this.PNG(address, nozzle));
        //                command.Request = cmd.ToArray().CRC().FIN();
        //                break;
        //        }

        //        command.TX = lastTX;
        //        command.Address = 0;
        //        if (executedCommands.ContainsKey(lastTX))
        //            executedCommands[lastTX] = command;
        //        else
        //            this.executedCommands.Add(command.TX, command);
        //        this.lastTX++;
        //        if (lastTX > 15)
        //        {
        //            lastTX = 0;
        //        }
        //        command.ExecuteStatusCommand(this.serialPort);
        //    }

        //    private void SendPoll(int tx)
        //    {
        //        var command = this.POLL(tx);
        //        this.serialPort.Write(command, 0, command.Length);
        //    }

        //    private void SendAck(int tx)
        //    {
        //        var command = this.ACK(tx);
        //        this.serialPort.Write(command, 0, command.Length);
        //    }

        //    private void SendNack(int tx)
        //    {
        //        var command = this.NAK(tx);
        //        this.serialPort.Write(command, 0, command.Length);
        //    }

        //    private void EvaluateResponse(byte[] response)
        //    {
        //        if (response.Length < 3)
        //            return;
        //        byte tx = 0;
        //        if(response.Length > 3 && !response.validateCRC())
        //        {
        //            tx = (byte)(response[0] & 0x0F);
        //            SendNack(tx);
        //            return;
        //        }

        //        //var low = response[0] & 0x0F;
        //        //var high = b >> 4;

        //        int address = (int)response[0] - 80;
        //        byte masterControlByte = (byte)((int)response[1] >> 4);
        //        tx = (byte)(response[0] & 0x0F);

        //        if (!this.executedCommands.ContainsKey(tx))
        //            return;
        //        var cmd = this.executedCommands[tx];
        //        cmd.EvaluateResponse(this.serialPort, response);
        //        //if (this.AreEqual(response, this.ACK(tx)))
        //        //{
        //        //    cmd.Status = (CommandStatus)((int)cmd.Status + 1);
        //        //}
        //        //if (this.AreEqual(response, this.EOT(tx)))
        //        //{
        //        //    if (cmd.Status != CommandStatus.Ack)
        //        //    {
        //        //        SendAck(tx);
        //        //        return;
        //        //    }
        //        //}
        //        //if (cmd.Status == CommandStatus.Sent)
        //        //{
        //        //    if (this.AreEqual(response, this.ACK(tx)))
        //        //    {
        //        //        this.SendPoll(tx);
        //        //        cmd.Status = CommandStatus.Poll;
        //        //    }
        //        //}
        //        //else if (cmd.Status == CommandStatus.Poll)
        //        //{
        //        //    //Evaluate response
        //        //    bool isValid = true;
        //        //    var data = this.GetCommandResponse(response, cmd.CommandByte);
        //        //    if (cmd.CommandByte == GETPUMPSTATUS)
        //        //    {

        //        //    }

        //        //    if (isValid)
        //        //    {
        //        //        SendAck(tx);
        //        //        cmd.Status = CommandStatus.Ack;
        //        //    }
        //        //    else
        //        //    {
        //        //        SendNack(tx);
        //        //        cmd.Status = CommandStatus.Nack;
        //        //    }
        //        //}
        //        //else if (cmd.Status == CommandStatus.Ack)
        //        //{
        //        //    if (this.AreEqual(response, this.POLL(tx)))
        //        //    {
        //        //        this.executedCommands.Remove(tx);
        //        //    }
        //        //}

        //    }

        //    private void EvaluateStatus(byte[] response)
        //    {

        //    }

        //    private void ReadData()
        //    {
        //        if (this.serialPort.BytesToRead == 0)
        //            return;
        //        try
        //        {
        //            byte[] buffer = new byte[this.serialPort.BytesToRead];

        //            this.serialPort.Read(buffer, 0, this.serialPort.BytesToRead);
        //            List<byte> list = new List<byte>(this.readBuffer);
        //            list.AddRange(buffer);
        //            this.readBuffer = list.ToArray();
        //            var responses = this.SplitResponses();
        //            foreach (var resp in responses)
        //            {
        //                EvaluateResponse(resp);
        //            }
        //        }
        //        catch
        //        {

        //        }
        //    }

        //    public void Disconnect()
        //    {
        //        if (this.serialPort.IsOpen)
        //            this.serialPort.Close();
        //    }

        //    private void ThreadRun()
        //    {
        //        while(true)
        //        {
        //            ReadData();
        //        }

        //        //this.GetDCRStatus(0);
        //        //foreach (Common.FuelPoint fp in this.fuelPoints)
        //        //{
        //        //    fp.Nozzles[0].QueryTotals = true;
        //        //    fp.SetExtendedProperty("statusFails", (int)0);

        //        //    //this.EndOfFilling(fp);
        //        //    foreach (Common.Nozzle nz in fp.Nozzles)
        //        //        this.Paid(nz);
        //        //}

        //        //while (this.IsConnected)
        //        //{
        //        //    //int dcr = 0;
        //        //    try
        //        //    {

        //        //        //dcr++;
        //        //        //if (dcr > 15)
        //        //        //    dcr = 0;
        //        //        foreach (Common.FuelPoint fp in this.fuelPoints)
        //        //        {
        //        //            try
        //        //            {
        //        //                this.GetStatus(fp);

        //        //                byte[] cmdt = this.POLL();
        //        //                this.ExecuteCommand(0);
        //        //                if (fp.Status == Common.Enumerators.FuelPointStatusEnum.TransactionCompleted)
        //        //                {
        //        //                    this.EndOfFilling(fp);
        //        //                    continue;
        //        //                }

        //        //                int nozzleForTotals = fp.Nozzles.Where(n => n.QueryTotals).Count();
        //        //                if (nozzleForTotals > 0)
        //        //                {

        //        //                    foreach (Common.Nozzle nz in fp.Nozzles)
        //        //                    {
        //        //                        if (nz.QueryTotals)
        //        //                        {
        //        //                            this.GetTotals(nz);

        //        //                        }
        //        //                    }

        //        //                    continue;
        //        //                }

        //        //                if (fp.QueryAuthorize)
        //        //                {

        //        //                    //this.SetPrice(fp);
        //        //                    //System.Threading.Thread.Sleep(15);

        //        //                    this.Authorize(fp);
        //        //                    continue;
        //        //                }


        //        //                if (fp.Status == Common.Enumerators.FuelPointStatusEnum.Work)
        //        //                {
        //        //                    //this.GetDisplay(fp);
        //        //                    continue;
        //        //                }




        //        //                if (fp.QuerySetPrice)
        //        //                {
        //        //                    foreach (Common.Nozzle nz in fp.Nozzles)
        //        //                    { this.SetPrice(nz); }

        //        //                    continue;
        //        //                }


        //        //            }
        //        //            finally
        //        //            {
        //        //                System.Threading.Thread.Sleep(300);
        //        //            }
        //        //        }
        //        //    }
        //        //    catch (Exception ex)
        //        //    {
        //        //        //StackFrame frame = new StackFrame(1);
        //        //        //var method = frame.GetMethod();


        //        //        System.Threading.Thread.Sleep(200);
        //        //    }
        //        //}
        //    }

        //    #region protocol specific helper methods
        //    private byte ADR(int address = 0)
        //    {
        //        return (byte)(80 + address);
        //    }

        //    /// <summary>
        //    /// This set the control
        //    /// </summary>
        //    /// <param name="type"> ACK NACK etc</param>
        //    /// <param name="BlockSequence">current block sequence [just ignore it]</param>
        //    /// <returns></returns>
        //    private byte CTR(MasterControlByte type, int BlockSequence = 0)
        //    {
        //        return (byte)((int)type << 4 | (byte)BlockSequence);

        //    }

        //    private enum SlaveControlByte
        //    {
        //        data = 3,
        //        nak = 5,
        //        eot = 7,
        //        ack = 12
        //    }

        //    private enum responseLength
        //    {
        //        ack = 3
        //    }
        //    private enum CmdByte
        //    {
        //        preset = (int)0x7F,
        //        status = (int)0x80,
        //        paidConfirm = (int)0x82,
        //        suspend = (int)0x84,
        //        authorize = (int)0x88,
        //        display = (int)0x91,
        //        dcrStatus = (int)0x94
        //    }

        //    private byte PNG(int address, int index)
        //    {
        //        byte msb, lsb;
        //        msb = (byte)address;
        //        lsb = (byte)(index);
        //        msb = (byte)(msb << 4);
        //        return (byte)(lsb | msb);
        //    }

        //    private byte PNGTotals(int address, int index)
        //    {
        //        byte msb, lsb;
        //        msb = (byte)(address + 4);
        //        lsb = (byte)(index);
        //        msb = (byte)(msb << 4);
        //        return (byte)(lsb | msb);
        //    }

        //    //private byte[] PNG(Common.FuelPoint fp)
        //    //{
        //    //    byte[] png = new byte[fp.NozzleCount];
        //    //    byte msb, lsb;
        //    //    for (int i = 0; i < fp.NozzleCount; i++)
        //    //    {

        //    //        msb = (byte)fp.Address;
        //    //        lsb = (byte)(fp.Nozzles[i].NozzleIndex);
        //    //        msb = (byte)(msb << 4);
        //    //        png[i] = (byte)(lsb | msb);

        //    //    }
        //    //    return png;
        //    //}

        //    private static byte[] intToBCD(int number)
        //    {
        //        ///Example intToBCD(123)
        //        ///return 0x00 0x00 0x01 0x23
        //        int len = (int)Math.Ceiling(Math.Log10(number));
        //        int bcd = 0;
        //        byte[] bcdr = new byte[len / 2 + len % 2];

        //        int k = 0;
        //        for (int i = 0; i < len; i++)
        //        {
        //            int nibble = number % 10;
        //            bcd |= (nibble << i * 4);
        //            if (i % 2 == 0)
        //            {
        //                bcdr[k] = (byte)nibble;
        //            }
        //            else
        //            {
        //                bcdr[k] |= (byte)(nibble << 4);
        //                k++;

        //            }
        //            number /= 10;
        //        }
        //        List<byte> result = new List<byte>();
        //        for (int l = 0; l < 4 - bcdr.Length; l++)
        //        {
        //            result.Add(0x00);
        //        }
        //        result.AddRange(bcdr.ToArray().Reverse());
        //        return result.ToArray();
        //    }
        //    #endregion

        //    private string getId(byte b)
        //    {
        //        //00010001
        //        int fpid = (0xF0 & b) >> 4;
        //        int chid = (0x0F & b);
        //        return Convert.ToString(fpid) + Convert.ToString(chid);

        //    }
        //    private int channel(byte b)
        //    {
        //        //00010001

        //        return (0x0F & b);

        //    }

        //    private byte[] GetCommandResponse(byte[] buffer, int command)
        //    {
        //        int index = -1;
        //        for (int i = 0; i < buffer.Length; i++)
        //        {
        //            if (buffer[i] != command)
        //                continue;
        //            index = i;
        //            break;
        //        }
        //        if (index < 0)
        //            return new byte[] { };

        //        return buffer.Skip(index).ToArray();
        //    }

        //    private bool evalTotals(byte[] totals)
        //    {
        //        if (totals.validateCRC())
        //        {
        //            //System.IO.File.AppendAllText("Teosis.log", nz.ParentFuelPoint.Address.ToString() + " :    " + BitConverter.ToString(totals) + "\r\n");

        //            totals = GetCommandResponse(totals, 135);

        //        //    if (System.Convert.ToString(nz.NozzleIndex)[0] == getId(totals[2])[1]
        //        //        && System.Convert.ToString(nz.ParentFuelPoint.Address + 4)[0] == getId(totals[2])[0])
        //        //    {
        //        //        //1  2  3  4  5  6  7  8  9  10 11  12 13 14 15 16 17 18 19 20 21 22 23
        //        //        //50 31 87 0F 11 00 00 00 51 00 00  00 00 00 60 00 00 00 01 8C EB 03 FA 

        //        //        //nz.TotalVolume = totals.skip(3).takeToDecimal(4);
        //        //        //nz.TotalPrice = totals.skip(7).takeToDecimal(6);
        //        //        //nz.SetExtendedProperty("GotTotals", true);
        //        //        int init = 0;
        //        //        //foreach (Common.Nozzle nz1 in nz.ParentFuelPoint.Nozzles)
        //        //        //{
        //        //        //    if ((bool)nz1.GetExtendedProperty("GotTotals", false))
        //        //        //        init++;
        //        //        //}
        //        //        //if (init == nz.ParentFuelPoint.NozzleCount)
        //        //        //    nz.ParentFuelPoint.Initialized = true;
        //        //        //else nz.ParentFuelPoint.Initialized = false;

        //        //        //if (this.TotalsRecieved != null)
        //        //        //    this.TotalsRecieved(this, new Common.TotalsEventArgs(
        //        //        //        nz.ParentFuelPoint,
        //        //        //        nz.Index,
        //        //        //        nz.TotalVolume,
        //        //        //        nz.TotalPrice));

        //        //        return true;
        //        //    }
        //        //    else return false;
        //        }
        //        return false;
        //    }

        //    //private bool evalStatusWhileWorking(byte[] status, Common.FuelPoint fp)
        //    //{
        //    //    if (status.Length == 14 && fp.Status == Common.Enumerators.FuelPointStatusEnum.Work)
        //    //    {
        //    //        //50 31 80 06 11 02 [00 00 00 ,06] CE 33 03 FA 
        //    //        decimal newAmountS = status.skip(6).takeToDecimal(4) / (decimal)Math.Pow(10, fp.AmountDecimalPlaces); ;
        //    //        decimal newVolumeS = decimal.Round(status.skip(6).takeToDecimal(4) * fp.ActiveNozzle.UnitPrice / (decimal)Math.Pow(10, fp.VolumeDecimalPlaces), 3, MidpointRounding.AwayFromZero);


        //    //        if (fp.DispensedVolume >= 0 && newVolumeS > 0)
        //    //            fp.DispensedVolume = newVolumeS;

        //    //        if (fp.DispensedAmount >= 0 && newAmountS > 0)
        //    //            fp.DispensedAmount = newAmountS;

        //    //        if (this.DataChanged != null)
        //    //        {
        //    //            Common.FuelPointValues values = new Common.FuelPointValues();
        //    //            values.CurrentSalePrice = fp.ActiveNozzle.UnitPrice;
        //    //            values.CurrentPriceTotal = fp.DispensedAmount;
        //    //            values.CurrentVolume = fp.DispensedVolume;

        //    //            this.DataChanged(this, new Common.FuelPointValuesArgs()
        //    //            {
        //    //                CurrentFuelPoint = fp,
        //    //                CurrentNozzleId = fp.ActiveNozzle.NozzleIndex,
        //    //                Values = values
        //    //            });

        //    //        }
        //    //        return true;

        //    //    }
        //    //    else return false;
        //    //}
        //    //private bool evalStatus(byte[] status, Common.FuelPoint fp)
        //    //{

        //    //    if (status.validateCRC() && Convert.ToString(fp.Address)[0] == this.getId(status[4])[0])
        //    //    {
        //    //        status = GetCommandResponse(status, 128);

        //    //        byte flag = status[3];
        //    //        try
        //    //        {
        //    //            Common.Enumerators.FuelPointStatusEnum oldStatus = fp.Status;

        //    //            switch (flag)
        //    //            {

        //    //                case (0x00):
        //    //                    fp.Status = Common.Enumerators.FuelPointStatusEnum.Idle;
        //    //                    break;
        //    //                case (0x01):
        //    //                    fp.Status = Common.Enumerators.FuelPointStatusEnum.Nozzle;
        //    //                    break;
        //    //                case (0x02):
        //    //                    fp.Status = Common.Enumerators.FuelPointStatusEnum.Work;
        //    //                    fp.QueryAuthorize = false;
        //    //                    break;
        //    //                case (0x03):
        //    //                    //Delivered Paused 
        //    //                    break;
        //    //                case (0x04):
        //    //                    fp.Status = Common.Enumerators.FuelPointStatusEnum.TransactionCompleted;
        //    //                    break;
        //    //                case (0x05):
        //    //                    fp.Status = Common.Enumerators.FuelPointStatusEnum.Error;
        //    //                    break;
        //    //                case (0x06):
        //    //                    fp.Status = Common.Enumerators.FuelPointStatusEnum.Ready;

        //    //                    break;
        //    //                case (0x07):
        //    //                    fp.Status = Common.Enumerators.FuelPointStatusEnum.Ready;

        //    //                    break;

        //    //            }
        //    //            //System.IO.File.AppendAllText(System.Environment.CurrentDirectory + "\\g.txt", "\n " + Convert.ToString(fp.Status));
        //    //            //System.IO.File.AppendAllText(System.Environment.CurrentDirectory + "\\g.txt", "\n " + Convert.ToString(fp.QueryAuthorize));
        //    //            fp.DispenserStatus = fp.Status;

        //    //            if (oldStatus != fp.Status && this.DispenserStatusChanged != null)
        //    //            {

        //    //                Common.FuelPointValues values = new Common.FuelPointValues();
        //    //                if (fp.Status != Common.Enumerators.FuelPointStatusEnum.Idle && fp.Status != Common.Enumerators.FuelPointStatusEnum.Offline)
        //    //                {


        //    //                    fp.ActiveNozzleIndex = this.channel(status[2]) - 1;
        //    //                    values.ActiveNozzle = this.channel(status[2]) - 1;
        //    //                }
        //    //                else
        //    //                {
        //    //                    fp.ActiveNozzleIndex = -1;
        //    //                    values.ActiveNozzle = -1;
        //    //                }

        //    //                values.Status = fp.Status;
        //    //                this.DispenserStatusChanged(this, new Common.FuelPointValuesArgs()
        //    //                {
        //    //                    CurrentFuelPoint = fp,
        //    //                    CurrentNozzleId = values.ActiveNozzle + 1,
        //    //                    Values = values
        //    //                });
        //    //            }

        //    //            if (status.Length >= 7 && fp.Status == Common.Enumerators.FuelPointStatusEnum.Work)
        //    //            {
        //    //                //50 31 80 06 11 02 [00 00 00 ,06] CE 33 03 FA 
        //    //                byte[] amountBCD = status.skip(4).take(4);
        //    //                decimal newAmountS = status.skip(4).takeToDecimal(4) / (decimal)Math.Pow(10, fp.VolumeDecimalPlaces);
        //    //                decimal up = fp.ActiveNozzle.UnitPrice;
        //    //                decimal newVolumeS = decimal.Round(newAmountS / up, fp.VolumeDecimalPlaces, MidpointRounding.AwayFromZero);

        //    //                //System.IO.File.AppendAllText("Teosis.log", string.Format("{0} : {1} : {2:N3} : {3:N3}",
        //    //                //    BitConverter.ToString(amountBCD), status.skip(4).takeToDecimal(4).ToString(), newAmountS, newVolumeS) + "\r\n");


        //    //                if (fp.DispensedVolume >= 0 && newVolumeS > 0)
        //    //                    fp.DispensedVolume = newVolumeS;

        //    //                if (fp.DispensedAmount >= 0 && newAmountS > 0)
        //    //                    fp.DispensedAmount = newAmountS;

        //    //                if (this.DataChanged != null)
        //    //                {
        //    //                    Common.FuelPointValues values = new Common.FuelPointValues();
        //    //                    values.CurrentSalePrice = fp.ActiveNozzle.UnitPrice;
        //    //                    values.CurrentPriceTotal = fp.DispensedAmount;
        //    //                    values.CurrentVolume = fp.DispensedVolume;

        //    //                    this.DataChanged(this, new Common.FuelPointValuesArgs()
        //    //                    {
        //    //                        CurrentFuelPoint = fp,
        //    //                        CurrentNozzleId = fp.ActiveNozzle.NozzleIndex,
        //    //                        Values = values
        //    //                    });

        //    //                }
        //    //            }


        //    //            return true;
        //    //        }
        //    //        catch { return false; }

        //    //    }
        //    //    else
        //    //        return false;
        //    //}


        //    #region POll ACK NACK POLLACK

        //    private byte[] POLL(int tx)
        //    {
        //        return new byte[] { ADR(), CTR(MasterControlByte.poll, tx), 0xFA };

        //    }

        //    private byte[] ACK(int tx)
        //    {
        //        return new byte[] { ADR(), CTR(MasterControlByte.ack, tx), 0xFA };

        //    }

        //    private byte[] NAK(int tx)
        //    {
        //        return new byte[] { ADR(), CTR(MasterControlByte.nak, tx), 0xFA };

        //    }

        //    private byte[] EOT(int tx)
        //    {
        //        return new byte[] { ADR(), CTR(MasterControlByte.eot, tx), 0xFA };

        //    }

        //    private byte[] AckPoll(int tx)
        //    {
        //        return new byte[] { ADR(), CTR(MasterControlByte.ackpoll, tx), 0xFA };

        //    }
        //    #endregion


        //    //private void Paid(Common.Nozzle nz)
        //    //{
        //    //    this.cmd = new byte[] { ADR(), CTR(MasterControlByte.data, 0), (byte)CmdByte.paidConfirm, 0x01 };
        //    //    List<byte> mycmd = new List<byte>();
        //    //    mycmd.AddRange(this.cmd);
        //    //    mycmd.Add(this.PNG(nz));


        //    //    this.cmd = mycmd.ToArray();
        //    //    this.cmd = this.cmd.CRC().FIN();

        //    //    byte[] response = this.ExecuteCommand(3);
        //    //}
        //    //private void Paid(Common.FuelPoint fp)
        //    //{

        //    //    //50 30 82 01 11 AE B8 03 FA
        //    //    this.cmd = new byte[] { ADR(), CTR(MasterControlByte.data, 0), (byte)CmdByte.paidConfirm, 0x01 };
        //    //    List<byte> mycmd = new List<byte>();
        //    //    mycmd.AddRange(this.cmd);
        //    //    mycmd.Add(this.PNG(fp.ActiveNozzle));


        //    //    this.cmd = mycmd.ToArray();
        //    //    this.cmd = this.cmd.CRC().FIN();

        //    //    byte[] response = this.ExecuteCommand(3);
        //    //}

        //    //private void EndOfFilling(Common.FuelPoint fp)
        //    //{

        //    //    //Request End of Filling
        //    //    //50 30 83 01 11 FF 78 03 FA
        //    //    //50 30 83 01 11 FF 78 03 FA

        //    //    this.cmd = new byte[] { ADR(), CTR(MasterControlByte.data, 0), 0x83, 0x01 };
        //    //    List<byte> mycmd = new List<byte>();
        //    //    mycmd.AddRange(this.cmd);
        //    //    mycmd.Add(this.PNG(fp.ActiveNozzle));





        //    //    this.cmd = mycmd.ToArray();
        //    //    this.cmd = this.cmd.CRC().FIN();
        //    //    //0  1  2  3  4  5  6  7  8  9  10 11 12 13 14 15 16 17 18 19 20 21 22 23 24
        //    //    //50-31-83-0D-11-00-00-00-00-00-00-00-00-00-00-41-11-EA-74-03-FA
        //    //    byte[] response = this.ExecuteCommand(3);
        //    //    if (AreEqual(response, this.ACK()))
        //    //    {
        //    //        this.cmd = this.POLL();
        //    //        response = this.ExecuteCommand(21);
        //    //        if (this.evalEndOfFilling(response, fp))
        //    //        {
        //    //            this.cmd = this.ACK();
        //    //            this.ExecuteCommand(0);

        //    //            this.Paid(fp);

        //    //        }
        //    //        else
        //    //        {
        //    //            this.cmd = this.NAK();
        //    //            byte[] temp = this.ExecuteCommand(50);
        //    //        }
        //    //    }
        //    //    else
        //    //    {
        //    //        this.cmd = this.NAK();
        //    //        byte[] temp = this.ExecuteCommand(50);
        //    //    }

        //    //    //this.Paid(fp);



        //    //}

        //    //private bool evalEndOfFilling(byte[] response, Common.FuelPoint fp)
        //    //{
        //    //    if (response.validateCRC() && Convert.ToString(fp.Address)[0] == this.getId(response[4])[0])
        //    //    {
        //    //        //                                  0,06Euro     0,56Litr
        //    //        //RX: 0779ms. 50 31 83 0D 11 00[ 00 00 00 60] [00 00 00 56] 00 01 14 0C 17 03 FA



        //    //        try
        //    //        {
        //    //            decimal newAmountS = response.skip(6).takeToDecimal(4) / (decimal)Math.Pow(10, fp.AmountDecimalPlaces + 1);
        //    //            decimal newVolumeS = response.skip(10).takeToDecimal(4) / (decimal)Math.Pow(10, fp.VolumeDecimalPlaces);


        //    //            if (fp.DispensedVolume >= 0 && newVolumeS > 0)
        //    //                fp.DispensedVolume = newVolumeS;

        //    //            if (fp.DispensedAmount >= 0 && newAmountS > 0)
        //    //                fp.DispensedAmount = newAmountS;


        //    //            if (this.DataChanged != null)
        //    //            {
        //    //                Common.FuelPointValues values = new Common.FuelPointValues();
        //    //                values.CurrentSalePrice = fp.ActiveNozzle.UnitPrice;
        //    //                values.CurrentPriceTotal = fp.DispensedAmount;
        //    //                values.CurrentVolume = fp.DispensedVolume;

        //    //                this.DataChanged(this, new Common.FuelPointValuesArgs()
        //    //                {
        //    //                    CurrentFuelPoint = fp,
        //    //                    CurrentNozzleId = fp.ActiveNozzle.NozzleIndex,
        //    //                    Values = values
        //    //                });

        //    //            }
        //    //            return true;

        //    //        }
        //    //        catch
        //    //        { return false; }
        //    //    }
        //    //    else return false;
        //    //}

        //    //private void ExecuteCommand(byte cmd)
        //    //{

        //    //}

        //    //private void Authorize(Common.FuelPoint fp)
        //    //{
        //    //    decimal max = 999999M;
        //    //    if ((fp.PresetVolume == max || fp.PresetVolume < 0) && (fp.PresetAmount == max || fp.PresetAmount < 0))
        //    //    {
        //    //        this.cmd = new byte[] { ADR(), CTR(MasterControlByte.data, 0), (byte)CmdByte.authorize, 0x07 };
        //    //        List<byte> mycmd = new List<byte>();
        //    //        mycmd.AddRange(this.cmd);
        //    //        mycmd.Add(this.PNG(fp.ActiveNozzle));
        //    //        mycmd.AddRange(new byte[] { 0x00, 0x00, 0x99, 0x99, 0x99, 0x00 });
        //    //        this.cmd = mycmd.ToArray();
        //    //        this.cmd = this.cmd.CRC().FIN();
        //    //    }

        //    //    else if (fp.PresetAmount != max && fp.PresetVolume == max)
        //    //    {
        //    //        this.cmd = new byte[] { ADR(), CTR(MasterControlByte.data, 0), (byte)CmdByte.authorize, 0x07 };
        //    //        List<byte> mycmd = new List<byte>();
        //    //        mycmd.AddRange(this.cmd);
        //    //        mycmd.Add(this.PNG(fp.ActiveNozzle));
        //    //        //string Amount = Convert.ToString((int)fp.PresetAmount * 100);


        //    //        /////
        //    //        ///// string priceString = Convert.ToString(nz.UntiPriceInt);
        //    //        //priceString = priceString.PadLeft(4, '0'); // Zero padding => product 4characters.
        //    //        //int p1 = Convert.ToInt16(priceString.Substring(0, 2));
        //    //        //int p2 = Convert.ToInt16(priceString.Substring(2, 2));
        //    //        //mycmd.Add((byte)ith(p1));
        //    //        //mycmd.Add((byte)ith(p2));
        //    //        /////


        //    //        mycmd.AddRange(new byte[] { 0x00, 0x00, 0x99, 0x99, 0x99, 0x00 });
        //    //        this.cmd = mycmd.ToArray();
        //    //        this.cmd = this.cmd.CRC().FIN();
        //    //    }

        //    //    else if (fp.PresetVolume != max && fp.PresetAmount == max)
        //    //    {
        //    //        this.cmd = new byte[] { ADR(), CTR(MasterControlByte.data, 0), (byte)CmdByte.authorize, 0x07 };
        //    //        List<byte> mycmd = new List<byte>();
        //    //        mycmd.AddRange(this.cmd);
        //    //        mycmd.Add(this.PNG(fp.ActiveNozzle));
        //    //        mycmd.AddRange(new byte[] { 0x00, 0x00, 0x99, 0x99, 0x99, 0x00 });
        //    //        this.cmd = mycmd.ToArray();
        //    //        this.cmd = this.cmd.CRC().FIN();
        //    //    }
        //    //    else return;


        //    //    byte[] response = this.ExecuteCommand(3);
        //    //    if (!this.AreEqual(response, this.ACK()))
        //    //    {
        //    //        this.cmd = this.NAK();
        //    //        this.ExecuteCommand(0);
        //    //        return;
        //    //    }
        //    //    fp.QueryAuthorize = false;
        //    //    return;
        //    //}

        //    //private void SetPrice(Common.Nozzle nz)
        //    //{
        //    //    // 50 30 8C 04 11 00 99 99 |74 F5 03 FA

        //    //    this.cmd = new byte[] { ADR(), CTR(MasterControlByte.data, this.blockSequence), 0x8C, 0x04 };
        //    //    List<byte> mycmd = new List<byte>();

        //    //    mycmd.AddRange(cmd);
        //    //    mycmd.Add(this.PNG(nz));
        //    //    mycmd.Add(0x00);

        //    //    string priceString = Convert.ToString(nz.UntiPriceInt);
        //    //    priceString = priceString.PadLeft(4, '0'); // Zero padding => product 4characters.
        //    //    int p1 = Convert.ToInt16(priceString.Substring(0, 2));
        //    //    int p2 = Convert.ToInt16(priceString.Substring(2, 2));
        //    //    mycmd.Add((byte)ith(p1));
        //    //    mycmd.Add((byte)ith(p2));
        //    //    this.cmd = mycmd.ToArray();
        //    //    this.cmd = cmd.CRC().FIN();

        //    //    byte[] response = this.ExecuteCommand(3);
        //    //    if (this.AreEqual(response, this.ACK()))
        //    //        nz.ParentFuelPoint.QuerySetPrice = false;
        //    //    else { this.cmd = NAK(); this.ExecuteCommand(0); }

        //    //}
        //    //private void GetTotals(Common.Nozzle nz)
        //    //{
        //    //    // 50 30 87 01 51 |BF 49 03 FA
        //    //    // 50 30 87 01 11 BE B9 03 FA 
        //    //    this.cmd = new byte[] { ADR(), CTR(MasterControlByte.data, 0), 0x87, 0x01 };
        //    //    List<byte> mycmd = new List<byte>();

        //    //    mycmd.AddRange(cmd);
        //    //    mycmd.Add(this.PNGTotals(nz));
        //    //    this.cmd = mycmd.ToArray();
        //    //    this.cmd = this.cmd.CRC().FIN();
        //    //    byte[] response = this.ExecuteCommand(3);
        //    //    if (AreEqual(response, this.ACK()))
        //    //    {
        //    //        this.cmd = this.POLL();
        //    //        response = this.ExecuteCommand(23);

        //    //        if (this.evalTotals(response, nz))
        //    //        {
        //    //            this.cmd = this.ACK();
        //    //            this.ExecuteCommand(0);
        //    //        }
        //    //        else
        //    //        {
        //    //            this.cmd = this.NAK();
        //    //            this.ExecuteCommand(0);
        //    //        }
        //    //    }
        //    //    else
        //    //    {
        //    //        this.cmd = this.NAK();
        //    //        this.ExecuteCommand(0);
        //    //    }
        //    //}
        //    private bool AreEqual(byte[] a1, byte[] a2)
        //    {
        //        return StructuralComparisons.StructuralEqualityComparer.Equals(a1, a2);
        //    }
        //    private int ith(int num)
        //    {
        //        return 16 * (num / 10) + (num % 10);
        //    }// hex = integerToHex(int)

        //    //private void GetStatus(Common.FuelPoint fp)
        //    //{
        //    //    try
        //    //    {
        //    //        this.cmd = new byte[] { ADR(), CTR(MasterControlByte.data, this.blockSequence), (byte)CmdByte.status, 0x01 };
        //    //        List<byte> mycmd = new List<byte>();
        //    //        mycmd.AddRange(cmd);
        //    //        mycmd.AddRange(this.PNG(fp));

        //    //        this.cmd = mycmd.ToArray().CRC().FIN();


        //    //        //Universal

        //    //        byte[] response = this.ExecuteCommand(3);
        //    //        if (this.AreEqual(response, this.ACK()))
        //    //        {
        //    //            //Send Poll to retrieve Status if ok ACK
        //    //            this.cmd = this.POLL();

        //    //            response = this.ExecuteCommand(14);

        //    //            if (this.evalStatus(response, fp))
        //    //            {
        //    //                this.cmd = this.ACK();
        //    //                this.ExecuteCommand(0);
        //    //            }
        //    //        }
        //    //        else
        //    //        {
        //    //            this.cmd = this.NAK();
        //    //            this.ExecuteCommand(0);
        //    //        }
        //    //    }
        //    //    catch { }
        //    //}

        //    private void GetDCRStatus(int dcr)
        //    {
        //        try
        //        {
        //            this.cmd = new byte[]
        //            {
        //                ADR(),
        //                CTR(MasterControlByte.data, this.blockSequence),
        //                (byte)CmdByte.dcrStatus,
        //                0x02,
        //                (byte)dcr,
        //                0x00
        //            };
        //            List<byte> mycmd = new List<byte>();
        //            mycmd.AddRange(cmd);

        //            this.cmd = mycmd.ToArray().CRC().FIN();


        //            //Universal

        //            byte[] response = this.ExecuteCommand(3);
        //            if (this.AreEqual(response, this.ACK(0)))
        //            {
        //                //Send Poll to retrieve Status if ok ACK
        //                this.cmd = this.POLL(0);

        //                response = this.ExecuteCommand(14);
        //            }
        //            else
        //            {
        //                this.cmd = this.NAK(0);
        //                this.ExecuteCommand(0);
        //            }
        //        }
        //        catch { }
        //    }

        //    private byte[] ExecuteCommand(int responseLength)
        //    {
        //        this.serialPort.Write(this.cmd, 0, this.cmd.Length);
        //        System.Threading.Thread.Sleep((int)((double)(cmd.Length + 3) * speed) + 10);

        //        int TimeOut = 120;//ms
        //        int waiting = 0;
        //        while (this.serialPort.BytesToRead < (int)responseLength && waiting < ((TimeOut / 20) * 10))
        //        {
        //            waiting += 10;
        //            System.Threading.Thread.Sleep(20);
        //        }
        //        byte[] response = new byte[this.serialPort.BytesToRead];
        //        this.serialPort.Read(response, 0, response.Length);

        //        if (response.Length > 0)
        //        {
        //            int i = 0;
        //        }
        //        return response;
        //    }

        //}
    }

    public static class Extensions
    {

        public static byte[] skip(this byte[] value, int number)
        {
            int i = 0;
            byte[] buffer = new byte[value.Length - number + 1];
            for (int k = number; k < value.Length; k++)
            {
                buffer[i] = value[k];
                i++;
            }
            return buffer;
        }

        public static byte[] take(this byte[] value, int number)
        {

            byte[] buffer = new byte[number];
            for (int k = 0; k < number; k++)
            {
                buffer[k] = value[k];
            }
            return buffer;
        }

        public static decimal takeToDecimal(this byte[] value, int number)
        {
            return decimal.Parse(BitConverter.ToString(value.take(number)).Replace("-", ""));
        }

    }

    public static class crc
    {
        public static byte[] CRC(this byte[] buffer)
        {
            #region tables
            byte[] auchCRCHi = {
                                    0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81,
                                    0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0,
                                    0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01,
                                    0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41,
                                    0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81,
                                    0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0,
                                    0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01,
                                    0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40,
                                    0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81,
                                    0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0,
                                    0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01,
                                    0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
                                    0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81,
                                    0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0,
                                    0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01,
                                    0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
                                    0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81,
                                    0x40
                         };
            byte[] auchCRCLo = {
                                    0x00, 0xC0, 0xC1, 0x01, 0xC3, 0x03, 0x02, 0xC2, 0xC6, 0x06, 0x07, 0xC7, 0x05, 0xC5, 0xC4,
                                    0x04, 0xCC, 0x0C, 0x0D, 0xCD, 0x0F, 0xCF, 0xCE, 0x0E, 0x0A, 0xCA, 0xCB, 0x0B, 0xC9, 0x09,
                                    0x08, 0xC8, 0xD8, 0x18, 0x19, 0xD9, 0x1B, 0xDB, 0xDA, 0x1A, 0x1E, 0xDE, 0xDF, 0x1F, 0xDD,
                                    0x1D, 0x1C, 0xDC, 0x14, 0xD4, 0xD5, 0x15, 0xD7, 0x17, 0x16, 0xD6, 0xD2, 0x12, 0x13, 0xD3,
                                    0x11, 0xD1, 0xD0, 0x10, 0xF0, 0x30, 0x31, 0xF1, 0x33, 0xF3, 0xF2, 0x32, 0x36, 0xF6, 0xF7,
                                    0x37, 0xF5, 0x35, 0x34, 0xF4, 0x3C, 0xFC, 0xFD, 0x3D, 0xFF, 0x3F, 0x3E, 0xFE, 0xFA, 0x3A,
                                    0x3B, 0xFB, 0x39, 0xF9, 0xF8, 0x38, 0x28, 0xE8, 0xE9, 0x29, 0xEB, 0x2B, 0x2A, 0xEA, 0xEE,
                                    0x2E, 0x2F, 0xEF, 0x2D, 0xED, 0xEC, 0x2C, 0xE4, 0x24, 0x25, 0xE5, 0x27, 0xE7, 0xE6, 0x26,
                                    0x22, 0xE2, 0xE3, 0x23, 0xE1, 0x21, 0x20, 0xE0, 0xA0, 0x60, 0x61, 0xA1, 0x63, 0xA3, 0xA2,
                                    0x62, 0x66, 0xA6, 0xA7, 0x67, 0xA5, 0x65, 0x64, 0xA4, 0x6C, 0xAC, 0xAD, 0x6D, 0xAF, 0x6F,
                                    0x6E, 0xAE, 0xAA, 0x6A, 0x6B, 0xAB, 0x69, 0xA9, 0xA8, 0x68, 0x78, 0xB8, 0xB9, 0x79, 0xBB,
                                    0x7B, 0x7A, 0xBA, 0xBE, 0x7E, 0x7F, 0xBF, 0x7D, 0xBD, 0xBC, 0x7C, 0xB4, 0x74, 0x75, 0xB5,
                                    0x77, 0xB7, 0xB6, 0x76, 0x72, 0xB2, 0xB3, 0x73, 0xB1, 0x71, 0x70, 0xB0, 0x50, 0x90, 0x91,
                                    0x51, 0x93, 0x53, 0x52, 0x92, 0x96, 0x56, 0x57, 0x97, 0x55, 0x95, 0x94, 0x54, 0x9C, 0x5C,
                                    0x5D, 0x9D, 0x5F, 0x9F, 0x9E, 0x5E, 0x5A, 0x9A, 0x9B, 0x5B, 0x99, 0x59, 0x58, 0x98, 0x88,
                                    0x48, 0x49, 0x89, 0x4B, 0x8B, 0x8A, 0x4A, 0x4E, 0x8E, 0x8F, 0x4F, 0x8D, 0x4D, 0x4C, 0x8C,
                                    0x44, 0x84, 0x85, 0x45, 0x87, 0x47, 0x46, 0x86, 0x82, 0x42, 0x43, 0x83, 0x41, 0x81, 0x80,
                                    0x40
                                    };
            #endregion
            byte init = 0x00;
            byte crch = init;
            byte crcl = init;
            int index = 0;

            crch = (byte)(init >> 8);
            for (int k = 0; k < buffer.Length; k++)
            {
                index = crcl ^ buffer[k];
                crcl = (byte)(crch ^ auchCRCHi[index]);
                crch = auchCRCLo[index];
            }
            index = crch;
            index = crcl;
            List<byte> result = new List<byte>();
            result.AddRange(buffer);

            result.Add(crcl);
            result.Add(crch);



            //result.Add(0xEA);
            //result.Add(0xFA);
            return result.ToArray();
            //return (index);
        }
        public static bool validateCRC(this byte[] buffer)
        {
            byte[] rawdata = buffer.Take(buffer.Length - 4).ToArray();
            if (System.BitConverter.ToString(buffer) == System.BitConverter.ToString(rawdata.CRC().FIN()))
                return true;
            else return false;
        }
        public static byte[] FIN(this byte[] buffer)
        {
            List<byte> response = new List<byte>();
            response.AddRange(buffer);
            response.Add(0x03);
            response.Add(0xFA);
            return response.ToArray();
        }
    }
}
