
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASFuelControl.Common;
using System.Collections;

namespace ASFuelControl.Teosis
{
    public class Controller : Common.FuelPumpControllerBase
    {
        public Controller()
        {
            //string logBool = ConfigurationManager.AppSettings["LogController"];
            this.ControllerType = Common.Enumerators.ControllerTypeEnum.Teosis;
            this.Controller = new TeosisConnector();

        }
        public class TeosisConnector : IFuelProtocol
        {
            #region ControlCharacters
            //private static byte POLL = 0x20;
            private static byte IAP = 0x40;
            // private static byte ACK = 0xC0;
            private static byte ACKPOLL = 0xE0;
            private static byte EOT = 0x70;
            private byte blockSequence = 0x00;

            #endregion

            public event EventHandler<Common.TotalsEventArgs> TotalsRecieved;
            public event EventHandler<Common.SaleEventArgs> SaleRecieved;
            public event EventHandler<Common.FuelPointValuesArgs> DataChanged;
            public event EventHandler<Common.FuelPointValuesArgs> DispenserStatusChanged;
            public event EventHandler DispenserOffline;
            private List<Common.FuelPoint> fuelPoints = new List<Common.FuelPoint>();
            private System.IO.Ports.SerialPort serialPort = new System.IO.Ports.SerialPort();
            private System.Threading.Thread th;
            private Dictionary<FuelPoint, List<ExecutionCommand>> executionCommands = new Dictionary<FuelPoint, List<ExecutionCommand>>();
            private byte[] readBuffer = new byte[] { };

            public Common.FuelPoint[] FuelPoints
            {
                set
                {
                    this.fuelPoints = new List<Common.FuelPoint>(value);
                }
                get
                {
                    return this.fuelPoints.ToArray();
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
                    this.serialPort.BaudRate = 9600;
                    this.serialPort.PortName = this.CommunicationPort;
                    this.serialPort.Parity = System.IO.Ports.Parity.Odd;
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
            public void AddFuelPoint(Common.FuelPoint fp)
            {
                this.fuelPoints.Add(fp);

            }
            public void ClearFuelPoints()
            {
                this.fuelPoints.Clear();
            }

            public void ExecuteGetStatus(FuelPoint fp)
            {
                var cmd = this.CreateCommand(fp, ExecutionCommand.GETPUMPSTATUS);
                if (cmd == null)
                    return;
                cmd.CreateCommand(0);
            }

            public void ExecutePaid(FuelPoint fp)
            {
                var cmd = this.CreateCommand(fp, ExecutionCommand.PAID);
                if (cmd == null)
                    return;
                cmd.CreateCommand(0);
            }

            public void ExecuteFillingReport(FuelPoint fp, int nozzleIdx)
            {
                var cmd = this.CreateCommand(fp, ExecutionCommand.FILLINGREPORT);
                if (cmd == null)
                    return;
                cmd.NozzleIndex = nozzleIdx;
                cmd.CreateCommand(0);
            }

            public void ExecuteGetTotals(FuelPoint fp, int nozzleIdx)
            {
                var cmd = this.CreateCommand(fp, ExecutionCommand.GETTOTALS);
                if (cmd == null)
                    return;
                cmd.NozzleIndex = nozzleIdx;
                cmd.CreateCommand(0);
            }

            public void ExecuteSetPrice(FuelPoint fp, int nozzleIdx, int price)
            {
                var cmd = this.CreateCommand(fp, ExecutionCommand.SETPRICE);
                if (cmd == null)
                    return;
                cmd.UnitPriceInt = price;
                cmd.NozzleIndex = nozzleIdx;
                cmd.CreateCommand(0);
            }

            public void ExecuteAuthorize(FuelPoint fp)
            {
                var cmd = this.CreateCommand(fp, ExecutionCommand.AUTHORIZE);
                if (cmd == null)
                    return;
                cmd.NozzleIndex = fp.ActiveNozzle.NozzleIndex - 1;
                cmd.PresetAmount = -1;
                cmd.PresetVolume = -1;
                cmd.CreateCommand(0);
            }

            public void ExecuteAuthorizeVolume(FuelPoint fp, int nozzleIdx, int volume)
            {
                var cmd = this.CreateCommand(fp, ExecutionCommand.AUTHORIZE);
                if (cmd == null)
                    return;
                cmd.PresetAmount = -1;
                cmd.PresetVolume = volume;
                cmd.CreateCommand(0);
            }

            public void ExecuteAuthorizeAmount(FuelPoint fp, int nozzleIdx, int amount)
            {
                var cmd = this.CreateCommand(fp, ExecutionCommand.AUTHORIZE);
                if (cmd == null)
                    return;
                cmd.PresetAmount = amount;
                cmd.PresetVolume = -1;
                cmd.CreateCommand(0);
            }

            public void ExecuteDCRStatus()
            {
                var cmd = this.CreateCommand(null, ExecutionCommand.GETDCRTATUS);
                if (cmd == null)
                    return;
                cmd.CreateCommand(0);
            }

            private ExecutionCommand CreateCommand(FuelPoint fp, byte cmdByte)
            {
                if (fp == null)
                    fp = this.fuelPoints.First();
                if (!executionCommands.ContainsKey(fp))
                    executionCommands.Add(fp, new List<ExecutionCommand>());
                var command = executionCommands[fp].FirstOrDefault(e => e.CommandByte == cmdByte && e.FuelPump == fp);
                if (command == null)
                {
                    command = new ExecutionCommand();
                    command.Controller = this;
                    command.TX = 0;
                    command.FuelPump = fp;
                    if(fp != null)
                        command.FuelPumpAddress = fp.Address;
                    command.CommandByte = cmdByte;
                    this.executionCommands[fp].Add(command);
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

                this.ExecuteDCRStatus();
                foreach (Common.FuelPoint fp in this.fuelPoints)
                {
                    fp.SetExtendedProperty("ExecutePaid", true);
                    fp.Nozzles[0].QueryTotals = true;
                    fp.SetExtendedProperty("statusFails", (int)0);

                }
                System.Threading.Thread.Sleep(10000);
                while (this.IsConnected)
                {
                    try
                    {
                        foreach (FuelPoint fp in this.fuelPoints)
                        {
                            if (!executionCommands.ContainsKey(fp))
                                executionCommands.Add(fp, new List<ExecutionCommand>());
                            int c = this.executionCommands[fp].Count(c1 => c1.Status != CommandStatus.Executed);

                            if ((bool)fp.GetExtendedProperty("ExecutePaid", false) && DateTime.Now.Subtract(lastStatus).TotalMilliseconds > 250 && c == 0)
                            {
                                fp.SetExtendedProperty("ExecutePaid", false);
                                ExecutePaid(fp);
                                lastStatus = DateTime.Now;
                            }

                            if (fp.Initialized && DateTime.Now.Subtract(lastStatus).TotalMilliseconds > 250 && c == 0)
                            {
                                if (this.executionCommands[fp].Count == 0)
                                {
                                    ExecuteGetStatus(fp);
                                    lastStatus = DateTime.Now;
                                }
                            }

                            int nozzleForTotals = fp.Nozzles.Where(n => n.QueryTotals).Count();
                            c = this.executionCommands[fp].Count(c1 => c1.Status != CommandStatus.Executed);
                            if (nozzleForTotals > 0)
                            {

                                foreach (Common.Nozzle nz in fp.Nozzles)
                                {
                                    c = this.executionCommands[fp].Count(c1 => c1.Status != CommandStatus.Executed);
                                    if (nz.QueryTotals && c == 0)
                                    {
                                        this.ExecuteGetTotals(fp, nz.NozzleIndex - 1);
                                    }
                                }
                            }
                            c = this.executionCommands[fp].Count(c1 => c1.Status != CommandStatus.Executed);
                            if (fp.QueryAuthorize && c == 0)
                            {

                                //this.SetPrice(fp);
                                //System.Threading.Thread.Sleep(15);

                                this.ExecuteAuthorize(fp);
                            }
                            c = this.executionCommands[fp].Count(c1 => c1.Status != CommandStatus.Executed);
                            if (fp.QuerySetPrice && c == 0)
                            {
                                //foreach (Common.Nozzle nz in fp.Nozzles)
                                //{
                                //    this.ExecuteSetPrice(fp, nz.NozzleIndex - 1, nz.UntiPriceInt);
                                //}
                                fp.QuerySetPrice = false;
                            }

                            var currentCommand = this.executionCommands[fp].OrderBy(c1 => c1.CreationTimeStamp).
                                FirstOrDefault(c1 => c1.Status == CommandStatus.Initialized);
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
                                        if (currentCommand.Status != CommandStatus.Poll)
                                            currentCommand.NextCommand = new byte[] { };

                                    }
                                    ReadData();

                                    if (!executionCommands.ContainsKey(currentCommand.FuelPump))
                                        executionCommands.Add(currentCommand.FuelPump, new List<ExecutionCommand>());

                                    if (currentCommand.TotalsRecieved)
                                    {
                                        decimal totalVolume = (decimal)currentCommand.VolumeTotals / (decimal)Math.Pow(10, fp.TotalDecimalPlaces);
                                        decimal totalAmount = (decimal)currentCommand.PriceTotals / (decimal)Math.Pow(10, fp.TotalDecimalPlaces);

                                        var sale = currentCommand.FuelPump.GetExtendedProperty("LastSale", null) as SaleEventArgs;
                                        if (sale != null)
                                        {
                                            sale.TotalVolume = totalVolume;
                                            sale.TotalPrice = totalAmount;
                                            this.SaleRecieved(this, sale);
                                            currentCommand.FuelPump.SetExtendedProperty("LastSale", null);
                                        }
                                        else
                                        {
                                            if (this.TotalsRecieved != null)
                                                this.TotalsRecieved(this, new TotalsEventArgs(
                                                    currentCommand.FuelPump, currentCommand.NozzleIndex + 1, totalVolume, totalAmount)
                                                );
                                        }
                                        currentCommand.TotalsRecieved = false;
                                        //Console.WriteLine("Totals : {0}", currentCommand.VolumeTotals);
                                    }
                                    if (currentCommand.StatusChanged)
                                    {
                                        if (currentCommand.DispenserStatus == 8)
                                        {
                                            if (this.executionCommands[currentCommand.FuelPump].FirstOrDefault(c1 => c1.CommandByte == ExecutionCommand.FILLINGREPORT) == null)
                                                this.ExecuteFillingReport(currentCommand.FuelPump, currentCommand.NozzleIndex);
                                        }
                                        else if (currentCommand.DispenserStatus == 4)
                                        {
                                            if (this.executionCommands[currentCommand.FuelPump].FirstOrDefault(c1 => c1.CommandByte == ExecutionCommand.PAID) == null)
                                                this.ExecutePaid(currentCommand.FuelPump);
                                        }
                                        this.EvaluateStatus(currentCommand);
                                        currentCommand.StatusChanged = false;
                                    }
                                    if (currentCommand.ReportRecieved)
                                    {
                                        var saleInitiated = (bool)currentCommand.FuelPump.GetExtendedProperty("SaleInitiated", false);
                                        if (saleInitiated)
                                            break;
                                        fp.NeedsInvoice = true;
                                        currentCommand.FuelPump.SetExtendedProperty("SaleInitiated", true);
                                        var sale = new SaleEventArgs
                                            (
                                                currentCommand.FuelPump,
                                                currentCommand.NozzleIndex + 1,
                                                currentCommand.VolumeTotals,
                                                currentCommand.PriceTotals,
                                                currentCommand.FuelPump.DispensedAmount,
                                                currentCommand.FuelPump.DispensedVolume);
                                        currentCommand.FuelPump.SetExtendedProperty("LastSale", sale);
                                        
                                        //Console.WriteLine("Sales Amount : {0}", currentCommand.SalesPrice);
                                        //Console.WriteLine("Sales Volume : {0}", currentCommand.SalesVolume);
                                        //Console.WriteLine("Sales Unti Price : {0}", currentCommand.SalesUnitPrice);
                                        currentCommand.StatusChanged = false;
                                    }
                                    if (currentCommand.DisplaySet)
                                    {
                                        Console.WriteLine("Display : {0}", currentCommand.Display);
                                        currentCommand.DisplaySet = false;
                                    }
                                    if(currentCommand.PriceSet)
                                    {
                                        if (currentCommand.NozzleIndex >= 0)
                                            currentCommand.FuelPump.Nozzles[currentCommand.NozzleIndex].QuerySetPrice = false;
                                        currentCommand.PriceSet = false;
                                    }
                                    if (status == CommandStatus.Executed || DateTime.Now.Subtract(currentCommand.CreationTimeStamp).TotalSeconds > 2)
                                        break;
                                }
                                this.executionCommands[currentCommand.FuelPump].Remove(currentCommand);
                            }
                            System.Threading.Thread.Sleep(10);
                        }
                    }
                    catch(Exception ex)
                    {

                    }
                }
            }

            private void EvaluateStatus(ExecutionCommand currentCommand)
            {
                FuelPoint fp = currentCommand.FuelPump;
                var oldStatus = fp.Status;
                switch (currentCommand.DispenserStatus)
                {
                    case 0:
                        fp.Status = Common.Enumerators.FuelPointStatusEnum.Idle;
                        fp.DispenserStatus = fp.Status;
                        currentCommand.FuelPump.SetExtendedProperty("SaleInitiated", false);
                        break;
                    case 1:
                        fp.Status = Common.Enumerators.FuelPointStatusEnum.Nozzle;
                        fp.DispenserStatus = fp.Status;
                        break;
                    case 2:
                        fp.Status = Common.Enumerators.FuelPointStatusEnum.Work;
                        fp.DispenserStatus = fp.Status;
                        fp.QueryAuthorize = false;
                        break;
                    case 6:
                        fp.Status = Common.Enumerators.FuelPointStatusEnum.Ready;
                        fp.DispenserStatus = fp.Status;
                        break;
                }
                if (oldStatus != fp.Status && this.DispenserStatusChanged != null)
                {
                    Common.FuelPointValues values = new Common.FuelPointValues();
                    //if (fp.Status != Common.Enumerators.FuelPointStatusEnum.Idle && fp.Status != Common.Enumerators.FuelPointStatusEnum.Offline)
                    //{
                    //    //fp.ActiveNozzleIndex = currentCommand.ResponseNozzle - 1;
                    //    values.ActiveNozzle = fp.ActiveNozzleIndex;
                    //}
                    //else
                    //{
                    //    fp.ActiveNozzleIndex = -1;
                    //    values.ActiveNozzle = -1;
                    //}

                    values.Status = fp.Status;
                    this.DispenserStatusChanged(this, new FuelPointValuesArgs()
                    {
                        CurrentFuelPoint = currentCommand.FuelPump,
                        CurrentNozzleId = currentCommand.NozzleIndex,
                        Values = values
                    });
                }
                if (fp.Status == Common.Enumerators.FuelPointStatusEnum.Work)
                {
                    fp.DispensedAmount = (decimal)currentCommand.Display / (decimal)Math.Pow(10, fp.AmountDecimalPlaces);
                    decimal up = fp.ActiveNozzle.UnitPrice;
                    fp.DispensedVolume = decimal.Round(fp.DispensedAmount / up, fp.VolumeDecimalPlaces, MidpointRounding.AwayFromZero);
                    if (this.DataChanged != null)
                    {
                        Common.FuelPointValues values = new Common.FuelPointValues();
                        values.CurrentSalePrice = fp.ActiveNozzle.UnitPrice;
                        values.CurrentPriceTotal = fp.DispensedAmount;
                        values.CurrentVolume = fp.DispensedVolume;
                        this.DataChanged(this, new Common.FuelPointValuesArgs()
                        {
                            CurrentFuelPoint = fp,
                            CurrentNozzleId = fp.ActiveNozzle.NozzleIndex,
                            Values = values
                        });

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
                catch (Exception ex)
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
                    foreach (var fp in this.executionCommands.Keys)
                    {
                        var cmd = this.executionCommands[fp].FirstOrDefault(c => c.DCRAddress == address);
                        if (cmd != null)
                            cmd.EvaluateResponse(response);
                    }
                }
                catch (Exception ex)
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

            public DebugValues DebugStatusDialog(FuelPoint fp)
            {
                return null;
            }
        }
    }

    public enum MasterControlByte
    {
        poll = 2,
        data = 3,
        iap = 4,
        nak = 5,
        eot = 7,
        ack = 12,
        ackpoll = 14,
    }

    public enum CommandStatus
    {
        Initialized = 0,
        Sent,
        Poll,
        Executed
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