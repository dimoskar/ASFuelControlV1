
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASFuelControl.Common;
using System.Collections;

namespace ASFuelControl.TeosisLPG
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

            public List<InternalPump> InternalPumps = new List<InternalPump>();

            public event EventHandler<Common.TotalsEventArgs> TotalsRecieved;
            public event EventHandler<Common.SaleEventArgs> SaleRecieved;
            public event EventHandler<Common.FuelPointValuesArgs> DataChanged;
            public event EventHandler<Common.FuelPointValuesArgs> DispenserStatusChanged;
            public event EventHandler DispenserOffline;
            private List<Common.FuelPoint> fuelPoints = new List<Common.FuelPoint>();
            private System.IO.Ports.SerialPort serialPort = new System.IO.Ports.SerialPort();
            private System.Threading.Thread th;
            private ExecutionCommand currentCommand = null;
            //private Dictionary<FuelPoint, List<ExecutionCommand>> executionCommands = new Dictionary<FuelPoint, List<ExecutionCommand>>();
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
                    this.InternalPumps.Clear();
                    foreach (var fp in this.fuelPoints)
                    {
                        InternalPump pump = new InternalPump();
                        pump.InitializePump(fp);
                        this.InternalPumps.Add(pump);
                    }

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

                InternalPump pump = new InternalPump();
                pump.InitializePump(fp);
                this.InternalPumps.Add(pump);
            }
            public void ClearFuelPoints()
            {
                this.fuelPoints.Clear();
                this.InternalPumps.Clear();
            }

            public bool ExecuteGetStatus(FuelPoint fp)
            {
                var cmd = this.CreateCommand(fp, ExecutionCommand.GETPUMPSTATUS);
                if (cmd == null)
                    return false;
                cmd.CreateCommand(0);
                return cmd.Execute(this.serialPort);
            }

            public bool ExecuteErrorReport(FuelPoint fp)
            {
                var cmd = this.CreateCommand(fp, ExecutionCommand.ERRORREPORT);
                if (cmd == null)
                    return false;
                cmd.CreateCommand(0);
                return cmd.Execute(this.serialPort);
            }

            public bool ExecuteReset()
            {
                var cmd = this.CreateCommand(null, ExecutionCommand.DCRRESET);
                if (cmd == null)
                    return false;
                cmd.CreateCommand(0);
                return cmd.Execute(this.serialPort);
            }

            public bool ExecutePaid(FuelPoint fp)
            {
                var cmd = this.CreateCommand(fp, ExecutionCommand.PAID);
                if (cmd == null)
                    return false;
                cmd.CreateCommand(0);
                return cmd.Execute(this.serialPort);
            }

            public bool ExecuteFillingReport(FuelPoint fp, int nozzleIdx)
            {
                var cmd = this.CreateCommand(fp, ExecutionCommand.FILLINGREPORT);
                if (cmd == null)
                    return false;
                cmd.NozzleIndex = nozzleIdx;
                cmd.CreateCommand(0);
                return cmd.Execute(this.serialPort);
            }

            public bool ExecuteGetTotals(FuelPoint fp, int nozzleIdx)
            {
                var cmd = this.CreateCommand(fp, ExecutionCommand.GETTOTALS);
                if (cmd == null)
                    return false;
                cmd.NozzleIndex = nozzleIdx;
                cmd.CreateCommand(0);
                return cmd.Execute(this.serialPort);
            }

            public bool ExecuteSetPrice(FuelPoint fp)
            {
                var cmd = this.CreateCommand(fp, ExecutionCommand.SETPRICE);
                if (cmd == null)
                    return false;
                cmd.CreateCommand(0);
                return cmd.Execute(this.serialPort);
            }

            public bool ExecuteAuthorize(FuelPoint fp)
            {
                var cmd = this.CreateCommand(fp, ExecutionCommand.AUTHORIZE);
                if (cmd == null)
                    return false;
                cmd.NozzleIndex = fp.ActiveNozzle.NozzleIndex - 1;
                cmd.PresetAmount = -1;
                cmd.PresetVolume = -1;
                cmd.CreateCommand(0);
                return cmd.Execute(this.serialPort);
            }

            public bool ExecuteHalt(FuelPoint fp, bool resume)
            {
                if (fp.ActiveNozzle == null)
                    return true;

                var cmd = this.CreateCommand(fp, ExecutionCommand.SUSPENDRESUME);
                if (cmd == null)
                    return false;
                cmd.Resume = resume;
                cmd.NozzleIndex = fp.ActiveNozzle.NozzleIndex - 1;
                cmd.CreateCommand(0);
                return cmd.Execute(this.serialPort);
            }

            public bool ExecuteResume(FuelPoint fp, int nozzleIndex)
            {
                if (fp.ActiveNozzle == null)
                    return true;

                var cmd = this.CreateCommand(fp, ExecutionCommand.SUSPENDRESUME);
                if (cmd == null)
                    return false;
                cmd.Resume = true;
                cmd.NozzleIndex = nozzleIndex;
                cmd.CreateCommand(0);
                return cmd.Execute(this.serialPort);
            }

            public bool ExecuteAuthorizeVolume(FuelPoint fp, int nozzleIdx, int volume)
            {
                var cmd = this.CreateCommand(fp, ExecutionCommand.AUTHORIZE);
                if (cmd == null)
                    return false;
                cmd.PresetAmount = -1;
                cmd.PresetVolume = volume;
                cmd.CreateCommand(0);
                return cmd.Execute(this.serialPort);
            }

            public bool ExecuteAuthorizeAmount(FuelPoint fp, int nozzleIdx, int amount)
            {
                var cmd = this.CreateCommand(fp, ExecutionCommand.AUTHORIZE);
                if (cmd == null)
                    return false;
                cmd.PresetAmount = amount;
                cmd.PresetVolume = -1;
                cmd.CreateCommand(0);
                return cmd.Execute(this.serialPort);
            }

            public bool ExecuteDCRStatus()
            {
                var cmd = this.CreateCommand(null, ExecutionCommand.GETDCRTATUS);
                if (cmd == null)
                    return false;
                cmd.CreateCommand(0);
                return cmd.Execute(this.serialPort);
            }

            private ExecutionCommand CreateCommand(FuelPoint fp, byte cmdByte)
            {
                if (fp == null)
                    fp = this.fuelPoints.First();


                currentCommand = new ExecutionCommand();
                currentCommand.Controller = this;
                currentCommand.TX = 0;
                currentCommand.FuelPump = fp;
                if (fp != null)
                    currentCommand.FuelPumpAddress = fp.Address;
                currentCommand.CommandByte = cmdByte;
                currentCommand.SaleResponse += CurrentCommand_SaleResponse;
                currentCommand.TotalsResponse += CurrentCommand_TotalsResponse;
                currentCommand.StatusSet += CurrentCommand_StatusSet;
                return currentCommand;
            }

            private void GetInternalTotals(Nozzle nz)
            {
                var ifp = this.InternalPumps.FirstOrDefault(p => p.Address == nz.ParentFuelPoint.Address);
                if (ifp != null)
                {
                    var inz = ifp.Nozzles.FirstOrDefault(n => n.NozzleIndex == nz.NozzleIndex);

                    decimal totalVolume = (decimal)inz.TotalVolume / (decimal)Math.Pow(10, nz.ParentFuelPoint.TotalDecimalPlaces);
                    decimal totalAmount = (decimal)inz.TotalPrice / (decimal)Math.Pow(10, nz.ParentFuelPoint.TotalDecimalPlaces);

                    var sale = nz.ParentFuelPoint.GetExtendedProperty("LastSale", null) as SaleEventArgs;
                    if (sale != null)
                    {
                        sale.TotalVolume = inz.TotalVolume;
                        sale.TotalPrice = inz.TotalPrice;
                        this.SaleRecieved(this, sale);
                        nz.ParentFuelPoint.SetExtendedProperty("LastSale", null);
                        nz.QueryTotals = false;
                    }
                    else
                    {
                        if ((int)nz.ParentFuelPoint.GetExtendedProperty("reportStatus", -1) == -1)
                        {
                            if (this.TotalsRecieved != null)
                                this.TotalsRecieved(this, new TotalsEventArgs(
                                    nz.ParentFuelPoint, nz.NozzleIndex, inz.TotalVolume, inz.TotalPrice)
                                );
                            nz.QueryTotals = false;
                        }
                    }
                    
                }
            }

            private void InitializePumpTotals()
            {
                foreach (FuelPoint fp in this.FuelPoints)
                {
                    fp.Initialized = false;
                    var ifp = this.InternalPumps.FirstOrDefault(p => p.Address == fp.Address);
                    foreach (Nozzle nz in fp.Nozzles)
                    {
                        nz.QueryTotals = true;
                        var inz = ifp.Nozzles.FirstOrDefault(n => n.NozzleIndex == nz.NozzleIndex);
                        inz.SalesVolume = 0;
                        inz.SalesPrice = 0;
                    }
                    fp.SetExtendedProperty("reportStatus", -1);
                }
                while (true)
                {
                    foreach(FuelPoint fp in this.FuelPoints)
                    {
                        foreach(Nozzle nz in fp.Nozzles)
                        {
                            if (!nz.QueryTotals)
                                continue;
                            if (!ExecuteGetTotals(fp, nz.NozzleIndex - 1))
                            {
                                System.Threading.Thread.Sleep(1000);
                                ExecutePaid(fp);
                            }
                        }
                    }
                    if (this.fuelPoints.SelectMany(f => f.Nozzles).Count(n => n.QueryTotals) == 0)
                        break;
                }
            }

            //private void HandleFillingReportExecution(FuelPoint fp, int nzIndex)
            //{
            //    if ((int)fp.GetExtendedProperty("reportAwaiting", -1) == -1)
            //        return;

            //    var saleInitiated = (bool)fp.GetExtendedProperty("SaleInitiated", false);
            //    if (saleInitiated)
            //        return;

            //    var red = fp.GetExtendedProperty("ExecuteRreport", null) as ReportExecutionDetails;
            //    if (red != null && !red.ShouldExecute)
            //        return;
            //    if (red != null && DateTime.Now.Subtract(red.FirstOccurance).TotalMinutes >= 1)
            //    {
            //        fp.SetExtendedProperty("ExecuteRreport", null);
            //    }
            //    if (nzIndex < 0 && red != null)
            //        nzIndex = red.NozzleIndex;

            //    if (nzIndex < 0)
            //        return;

            //    var executed = !this.ExecuteFillingReport(fp, nzIndex);
            //    if (!executed)
            //    {
            //        fp.SetExtendedProperty("ExecuteRreport",
            //            new ReportExecutionDetails() { ShouldExecute = true, NozzleIndex = nzIndex, FirstOccurance = DateTime.Now });
            //    }
            //    else
            //    {
            //        fp.SetExtendedProperty("ExecuteRreport", null);
            //    }
            //}

            private void CurrentCommand_StatusSet(object sender, EventArgs e)
            {
                if (currentCommand.DispenserStatus == 8)
                {
                    //if ((int)currentCommand.FuelPump.GetExtendedProperty("reportStatus", -1) == -1)
                    //{
                    //    currentCommand.FuelPump.SetExtendedProperty("reportStatus", 0);
                    //    if(this.ExecuteFillingReport(currentCommand.FuelPump, currentCommand.NozzleIndex))
                    //        currentCommand.FuelPump.SetExtendedProperty("reportStatus", 1);
                    //    //HandleFillingReportExecution(currentCommand.FuelPump, currentCommand.NozzleIndex);
                    //}
                    ////while (!this.ExecuteFillingReport(currentCommand.FuelPump, currentCommand.NozzleIndex))
                    ////{
                    ////    System.Threading.Thread.Sleep(100);
                    ////}
                }
                else if (currentCommand.DispenserStatus == 4)
                {
                    if ((int)currentCommand.FuelPump.GetExtendedProperty("reportStatus", -1) <= 0)
                    {
                        currentCommand.FuelPump.SetExtendedProperty("reportStatus", 0);
                        if (this.ExecuteFillingReport(currentCommand.FuelPump, currentCommand.NozzleIndex))
                            currentCommand.FuelPump.SetExtendedProperty("reportStatus", 1);
                    }
                    currentCommand.FuelPump.SetExtendedProperty("ExecutePaid", true);
                }
                //else if(currentCommand.DispenserStatus == 0 && currentCommand.FuelPump.Status == Common.Enumerators.FuelPointStatusEnum.Work)
                //{

                //}

                this.EvaluateStatus(currentCommand);
                currentCommand.StatusChanged = false;
            }

            private void CurrentCommand_TotalsResponse(object sender, EventArgs e)
            {
                decimal totalVolume = (decimal)currentCommand.VolumeTotals / (decimal)Math.Pow(10, currentCommand.FuelPump.TotalDecimalPlaces);
                decimal totalAmount = (decimal)currentCommand.PriceTotals / (decimal)Math.Pow(10, currentCommand.FuelPump.TotalDecimalPlaces);

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
                            currentCommand.FuelPump, currentCommand.NozzleIndex + 1, currentCommand.VolumeTotals, currentCommand.PriceTotals)
                        );
                }
            }

            private void CurrentCommand_SaleResponse(object sender, EventArgs e)
            {
                var saleInitiated = (bool)currentCommand.FuelPump.GetExtendedProperty("SaleInitiated", false);
                if (saleInitiated)
                    return;
                
                if (currentCommand.NozzleIndex >= 0)
                {
                    var ifp = this.InternalPumps.First(p => p.Address == currentCommand.FuelPump.Address);
                    var inz = ifp.Nozzles.First(n => n.NozzleIndex == currentCommand.NozzleIndex + 1);
                    var nz = currentCommand.FuelPump.Nozzles[currentCommand.NozzleIndex];
                    nz.TotalVolume = inz.TotalVolume;
                    nz.TotalPrice = inz.TotalPrice;

                    currentCommand.FuelPump.NeedsInvoice = true;
                    currentCommand.FuelPump.SetExtendedProperty("SaleInitiated", true);
                    var sale = new SaleEventArgs
                        (
                            currentCommand.FuelPump,
                            currentCommand.NozzleIndex + 1,
                            inz.TotalVolume,
                            inz.TotalPrice,
                            currentCommand.FuelPump.DispensedAmount,
                            currentCommand.FuelPump.DispensedVolume);
                    currentCommand.FuelPump.SetExtendedProperty("LastSale", sale);
                }
                
            }

            private void ThreadRun()
            {
                if(System.IO.File.Exists("TeosisIO.log"))
                    ExecutionCommand.LogIO = true;
                ExecutionCommand.LogSalesIO = false;
                DateTime lastStatus = DateTime.MinValue;
                //this.ExecuteReset();
                while(this.ExecuteDCRStatus() == false)
                {
                    System.Threading.Thread.Sleep(50);
                }
                foreach (Common.FuelPoint fp in this.fuelPoints)
                {
                    while (!this.ExecutePaid(fp))
                    {
                        System.Threading.Thread.Sleep(50);
                    }
                    foreach (var nz in fp.Nozzles)
                        nz.QueryTotals = true;
                    this.ExecuteSetPrice(fp);
                }

                InitializePumpTotals();

                //System.Threading.Thread.Sleep(10000);
                while (this.IsConnected)
                {
                    try
                    {
                        foreach (FuelPoint fp in this.fuelPoints)
                        {   
                            int reportStatus = (int)fp.GetExtendedProperty("reportStatus", -1);
                            if (reportStatus == 0)
                            {
                                if (this.ExecuteFillingReport(fp, -1))
                                    fp.SetExtendedProperty("reportStatus", 1);
                                else
                                    continue;
                            }

                            bool execPaid = (bool)fp.GetExtendedProperty("ExecutePaid", false);
                            if (execPaid)
                            {
                                if (this.ExecutePaid(fp))
                                {
                                    fp.SetExtendedProperty("FillingReportStatusSet", false);
                                    fp.SetExtendedProperty("ExecutePaid", false);
                                }
                                else
                                    continue;
                            }

                            if (fp.QueryHalt)
                            {
                                if (ExecuteHalt(fp, false))
                                {
                                    fp.QueryHalt = false;
                                    fp.Halted = true;
                                }
                            }

                            //if(fp.Halted && fp.Status == Common.Enumerators.FuelPointStatusEnum.Idle)
                            //{
                            //    bool hasFailed = false;
                            //    for(int i=0; i < fp.Nozzles.Length; i++)
                            //    {
                            //        if (!ExecuteResume(fp, i))
                            //        {
                            //            hasFailed = true;
                            //        }
                            //    }
                            //    fp.Halted = hasFailed;
                            //}
                            bool getStatus = true;
                            foreach (Common.Nozzle nz in fp.Nozzles)
                            {
                                if (nz.QueryTotals)
                                {
                                    GetInternalTotals(nz);
                                    //this.ExecuteGetTotals(fp, nz.NozzleIndex - 1);
                                    if(!nz.QueryTotals)
                                        getStatus = false;
                                }
                            }

                            if (fp.QuerySetPrice)
                            {
                                if(this.ExecuteSetPrice(fp))
                                {
                                    fp.QuerySetPrice = false;
                                    getStatus = false;
                                }
                                continue;
                            }

                            if (fp.QueryAuthorize)
                            {
                                this.ExecuteAuthorize(fp);
                                System.Threading.Thread.Sleep(100);
                                if (fp.Status != Common.Enumerators.FuelPointStatusEnum.Work)
                                {
                                    if (ExecuteGetStatus(fp))
                                    {
                                        if (fp.Status == Common.Enumerators.FuelPointStatusEnum.Error)
                                        {
                                            ExecuteErrorReport(fp);
                                        }
                                        if (fp.Status == Common.Enumerators.FuelPointStatusEnum.Work)
                                            fp.QueryAuthorize = false;
                                    }
                                }
                                getStatus = false;
                            }
                            
                            if (getStatus)
                            {
                                ExecuteGetStatus(fp);
                                if(fp.Status == Common.Enumerators.FuelPointStatusEnum.Error)
                                {
                                    ExecuteErrorReport(fp);
                                }
                            }
                            System.Threading.Thread.Sleep(100);
                        }
                    }
                    catch(Exception ex)
                    {

                    }
                }
            }

            private void ResetFuelPumps()
            {
                foreach (Common.FuelPoint fp in this.fuelPoints)
                {
                    while (!this.ExecutePaid(fp))
                    {
                        System.Threading.Thread.Sleep(50);
                    }
                    foreach (var nz in fp.Nozzles)
                        nz.QueryTotals = true;
                    this.ExecuteSetPrice(fp);
                    fp.SetExtendedProperty("reportStatus", -1);
                }
                this.InitializePumpTotals();
            }

            private void EvaluateStatus(ExecutionCommand currentCommand)
            {
                FuelPoint fp = currentCommand.FuelPump;
                var oldStatus = fp.Status;
                switch (currentCommand.DispenserStatus)
                {
                    case 0:
                        if (fp.LastStatus != Common.Enumerators.FuelPointStatusEnum.Offline && fp.Status == Common.Enumerators.FuelPointStatusEnum.Offline)
                        {
                            ResetFuelPumps();
                        }
                        else if (fp.LastStatus != Common.Enumerators.FuelPointStatusEnum.Error && fp.Status == Common.Enumerators.FuelPointStatusEnum.Error)
                        {
                            ResetFuelPumps();
                        }
                        fp.Status = Common.Enumerators.FuelPointStatusEnum.Idle;
                        fp.DispenserStatus = fp.Status;
                        currentCommand.FuelPump.SetExtendedProperty("SaleInitiated", false);
                        
                        if (fp.Halted)
                        {
                            
                        }
                        break;
                    case 1:
                        if(fp.Status != Common.Enumerators.FuelPointStatusEnum.Nozzle)
                            fp.QuerySetPrice = true;
                        fp.Status = Common.Enumerators.FuelPointStatusEnum.Nozzle;
                        fp.DispenserStatus = fp.Status;
                        if (fp.Status != Common.Enumerators.FuelPointStatusEnum.Work)
                            currentCommand.FuelPump.SetExtendedProperty("reportStatus", -1);
                        break;
                    case 2:
                        fp.Status = Common.Enumerators.FuelPointStatusEnum.Work;
                        fp.DispenserStatus = fp.Status;
                        fp.QueryAuthorize = false;
                        break;
                    case 3:
                        //if (fp.Halted && (bool)fp.GetExtendedProperty("CloseAfterHalted", false))
                        //{
                        //    fp.Halted = false;
                        //    fp.QueryResume = true;
                        //}
                        break;
                    case 5:
                        fp.Status = Common.Enumerators.FuelPointStatusEnum.Error;
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

            //private void ReadData()
            //{
            //    if (this.serialPort.BytesToRead == 0)
            //        return;
            //    try
            //    {
            //        byte[] buffer = new byte[this.serialPort.BytesToRead];

            //        this.serialPort.Read(buffer, 0, this.serialPort.BytesToRead);
            //        List<byte> list = new List<byte>(this.readBuffer);
            //        list.AddRange(buffer);
            //        this.readBuffer = list.ToArray();
            //        var responses = this.SplitResponses();
            //        foreach (var resp in responses)
            //        {
            //            //Console.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff") + " - RX : " + BitConverter.ToString(resp));
            //            EvaluateResponse(resp);
            //        }
            //    }
            //    catch (Exception ex)
            //    {

            //    }
            //}

            //private void EvaluateResponse(byte[] response)
            //{
            //    try
            //    {
            //        if (response.Length < 3)
            //            return;

            //        int address = (int)response[0] - 80;
            //        byte masterControlByte = (byte)((int)response[1] >> 4);
            //        var tx = (byte)(response[1] & 0x0F);
            //        foreach (var fp in this.executionCommands.Keys)
            //        {
            //            var cmd = this.executionCommands[fp].FirstOrDefault(c => c.DCRAddress == address);
            //            if (cmd != null)
            //                cmd.EvaluateResponse(response);
            //        }
            //    }
            //    catch (Exception ex)
            //    {

            //    }
            //}

            //private byte[][] SplitResponses()
            //{
            //    int lastIndex = 0;
            //    List<byte[]> responses = new List<byte[]>();
            //    for (int i = 0; i < this.readBuffer.Length; i++)
            //    {
            //        if (readBuffer[i] == 0xFA)
            //        {
            //            var response = readBuffer.Skip(lastIndex).Take(i + 1 - lastIndex).ToArray();
            //            responses.Add(response);
            //            lastIndex = i + 1;
            //        }
            //    }
            //    readBuffer = readBuffer.Skip(lastIndex).ToArray();
            //    return responses.ToArray();
            //}

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

    public class ReportExecutionDetails
    {
        public bool ShouldExecute { set; get; }
        public int NozzleIndex;
        public DateTime FirstOccurance { set; get; }
    }

    public class InternalPump
    {
        public int Address { set; get; }
        public InternalNozzle[] Nozzles { private set; get; }

        public void InitializePump(Common.FuelPoint pump)
        {
            this.Address = pump.Address;
            List<InternalNozzle> nzList = new List<InternalNozzle>();
            foreach (var nz in pump.Nozzles)
            {
                InternalNozzle n = new InternalNozzle();
                n.NozzleIndex = nz.NozzleIndex;
                nzList.Add(n);
            }
            this.Nozzles = nzList.ToArray();
        }
    }

    public class InternalNozzle
    {
        public int NozzleIndex { set; get; }
        public decimal TotalVolume
        {
            get
            {
                return this.InitialVolume + decimal.Round(this.SalesVolume / 10, 2);
            }
        }
        public decimal TotalPrice
        {
            get
            {
                return this.InitialPrice + this.SalesPrice;
            }
        }

        public decimal SalesVolume { set; get; }
        public decimal SalesPrice { set; get; }

        public decimal InitialVolume { set; get; }
        public decimal InitialPrice { set; get; }
    }
}