using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Ports;
using ASFuelControl.Common;
using ASFuelControl.Common.Enumerators;
using System.IO;

namespace ASFuelControl.DartMithra
{
    public class Controller : Common.FuelPumpControllerBase
    {
        
        public Controller()
        {
            this.ControllerType = ControllerTypeEnum.DartMithra;
            this.Controller = new DartMithraProtocol();
        }

    }
    public class DartMithraProtocol : Common.IFuelProtocol
    {
        #region Basics
        public event EventHandler<Common.FuelPointValuesArgs> DataChanged;
        public event EventHandler<Common.TotalsEventArgs> TotalsRecieved;
        public event EventHandler<Common.FuelPointValuesArgs> DispenserStatusChanged;
        public event EventHandler<SaleEventArgs> SaleRecieved;
        public event EventHandler DispenserOffline;

        private List<Common.FuelPoint> fuelPoints = new List<Common.FuelPoint>();

        private SerialPort serialPort = new SerialPort();
        private Thread th;
        private const int DeliveryCloseStatusTimeoutMilliseconds = 2000;
        private const int FinalDeliveryReadTimeoutMilliseconds = 3000;
        private const int MaxCrcCommandAttempts = 3;
        private const int ReceiveQuietMilliseconds = 15;
        private const int InitializationStatusRepublishIntervalMilliseconds = 600;
        private const int InitializationStatusRepublishCount = 3;
        private const int LiveDisplayIntervalMilliseconds = 300;
        private readonly List<byte> receiveBuffer = new List<byte>();
        private readonly object flowLogSync = new object();

        private const string PumpAuthorizedProperty = "Mithra.PumpAuthorized";
        private const string AuthorizationPendingProperty = "Mithra.AuthorizationPending";
        private const string PreSaleTotalizerProperty = "Mithra.PreSaleTotalizer";
        private const string PreSaleTotalizerReadyProperty = "Mithra.PreSaleTotalizerReady";
        private const string SaleStartedProperty = "Mithra.SaleStarted";
        private const string SaleNozzleIndexProperty = "Mithra.SaleNozzleIndex";
        private const string FinalizationPendingProperty = "Mithra.FinalizationPending";
        private const string FinalizationStartProperty = "Mithra.FinalizationStart";
        private const string FinalizationNextAttemptProperty = "Mithra.FinalizationNextAttempt";
        private const string FinalizationPreviousVolumeProperty = "Mithra.FinalizationPreviousVolume";
        private const string FinalizationPreviousAmountProperty = "Mithra.FinalizationPreviousAmount";
        private const string FinalizationStableResponsesProperty = "Mithra.FinalizationStableResponses";
        private const string LastHaltReasonProperty = "Mithra.LastHaltReason";
        private const string InitializationPendingProperty = "Mithra.InitializationPending";
        private const string InitialTotalReadProperty = "Mithra.InitialTotalRead";
        private const string TotalRequestReasonProperty = "Mithra.TotalRequestReason";
        private const string InitializationStatusRepublishRemainingProperty = "Mithra.InitializationStatusRepublishRemaining";
        private const string InitializationStatusRepublishAtProperty = "Mithra.InitializationStatusRepublishAt";
        private const string LastLiveDisplayRequestProperty = "Mithra.LastLiveDisplayRequest";

        public DebugValues foo = new Common.DebugValues();
        public FuelPoint[] FuelPoints
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
                this.serialPort.Parity = Parity.Odd;
                this.serialPort.BaudRate = 9600;
                LoggerFlow(string.Format("CONNECT opening port={0}, baud={1}, parity={2}",
                    this.serialPort.PortName, this.serialPort.BaudRate, this.serialPort.Parity));
                this.serialPort.Open();
                LoggerFlow("CONNECT port opened; starting workflow");
                this.th = new Thread(this.WorkFlow);
                th.Start();
            }
            catch (Exception ex)
            {
                LoggerException("Connect Error " + ex.Message);
            }
        }
        public void Disconnect()
        {
            LoggerFlow("DISCONNECT requested");
            if (this.serialPort.IsOpen)
                this.serialPort.Close();
            LoggerFlow("DISCONNECT port closed");
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
            //GetStatus(fp);
            foo.Status = fp.Status;
            return foo;
        }
        #endregion

        #region Workflow Dispensers

        private void WorkFlow()
        {
            try
            {
                foreach (FuelPoint fp in this.fuelPoints)
                {
                    BeginInitialization(fp, "startup");
                    fp.QuerySetPrice = true;
                    fp.SetExtendedProperty("LastValidResponse", DateTime.Now.AddSeconds(-10));
                    // Force an immediate status request. Using DateTime.Now as the
                    // getter default prevents the two-second interval from ever elapsing.
                    fp.SetExtendedProperty("dtGetStatus_" + fp.Address, DateTime.MinValue);
                    fp.SetExtendedProperty(PumpAuthorizedProperty, false);
                    fp.SetExtendedProperty(AuthorizationPendingProperty, false);
                    fp.SetExtendedProperty(PreSaleTotalizerReadyProperty, false);
                    fp.SetExtendedProperty(SaleStartedProperty, false);
                    fp.SetExtendedProperty(SaleNozzleIndexProperty, -1);
                    fp.SetExtendedProperty(FinalizationPendingProperty, false);
                    fp.SetExtendedProperty(LastLiveDisplayRequestProperty, DateTime.MinValue);
                    LoggerFlow(string.Format(
                        "WORKFLOW initialized pump={0}, nozzles={1}, status={2}",
                        fp.Address, fp.Nozzles.Count(), fp.Status));
                }

                while (this.IsConnected)
                {
                    try
                    {
                        foreach (FuelPoint fp in this.fuelPoints.OrderBy(x => x.Address))
                        {
                            DateTime lastValidResponse = (DateTime)fp.GetExtendedProperty(
                                "LastValidResponse", DateTime.Now.AddSeconds(-10));
                            if ((DateTime.Now - lastValidResponse).TotalSeconds > 8 &&
                                fp.Status != FuelPointStatusEnum.Offline)
                            {
                                LoggerFlow(string.Format(
                                    "OFFLINE pump={0}, previousStatus={1}, silenceMs={2:0}",
                                    fp.Address, fp.Status,
                                    (DateTime.Now - lastValidResponse).TotalMilliseconds));
                                fp.Status = FuelPointStatusEnum.Offline;
                                fp.DispenserStatus = FuelPointStatusEnum.Offline;
                                BeginInitialization(fp, "communication-timeout");
                                fp.SetExtendedProperty(PumpAuthorizedProperty, false);
                                fp.SetExtendedProperty(AuthorizationPendingProperty, false);
                                PublishStatusData(fp, 0, -1);
                                if (this.DispenserOffline != null)
                                    this.DispenserOffline(fp, EventArgs.Empty);
                            }

                            // Only a validated response can recover an offline pump.
                            Poll(fp);
                            if (fp.Status == FuelPointStatusEnum.Offline)
                                continue;
                            RepublishInitializedStatusIfDue(fp);

                            if (fp.QueryHalt || fp.QueryStop)
                            {
                                LoggerFlow(string.Format(
                                    "WORKFLOW pump={0}, action={1}", fp.Address,
                                    fp.QueryHalt ? "Halt" : "Stop"));
                                StopPump(fp);
                                continue;
                            }

                            if (fp.QueryResume)
                            {
                                LoggerFlow(string.Format(
                                    "WORKFLOW pump={0}, action=Resume", fp.Address));
                                ResumePump(fp);
                            }

                            if (fp.QueryAuthorize)
                            {
                                Nozzle activeNozzle = GetCurrentNozzle(fp);
                                if (activeNozzle == null)
                                {
                                    LoggerFlow(string.Format(
                                        "AUTHORIZE deferred pump={0}: no active nozzle", fp.Address));
                                    continue;
                                }

                                bool preSaleReady = (bool)fp.GetExtendedProperty(
                                    PreSaleTotalizerReadyProperty, false);
                                if (activeNozzle != null && !preSaleReady)
                                {
                                    LoggerFlow(string.Format(
                                        "AUTHORIZE deferred pump={0}, nozzle={1}: reading pre-sale totalizer",
                                        fp.Address, activeNozzle.Index));
                                    QueueTotals(activeNozzle, "pre-sale");
                                    GetTotals(activeNozzle);
                                    continue;
                                }

                                if (fp.QuerySetPrice)
                                    SetPrice(fp);
                                Authorize(fp);
                                continue;
                            }

                            // One totalizer request per pass prevents a multi-nozzle pump from
                            // monopolizing the serial line and delaying status polls for others.
                            Nozzle totalsNozzle = fp.Nozzles.OrderBy(x => x.Index)
                                .FirstOrDefault(x => x.QueryTotals);
                            if (totalsNozzle != null)
                                GetTotals(totalsNozzle);

                            if (fp.QuerySetPrice && fp.Status != FuelPointStatusEnum.Offline)
                                SetPrice(fp);

                            Thread.Sleep(25);
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggerException("Workflow Error " + ex.Message);
                    }
                    finally
                    {
                        Thread.Sleep(25);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerException("Workflow stopped: " + ex.Message);
            }
        }

        private void Authorize(FuelPoint fp)
        {
            try
            {
                LoggerFlow(string.Format(
                    "AUTHORIZE pump={0}, nozzle={1}, preSaleTotal={2}",
                    fp.Address, fp.ActiveNozzleIndex + 1,
                    fp.GetExtendedProperty(PreSaleTotalizerProperty, "not-read")));
                // Authorize changes pump state. Do not resend it just because its reply
                // was lost; subsequent status polls reconcile the real pump state.
                byte[] response = ExecuteAndReadResponse(
                    Commands.Authorize(fp.Address), fp, 75, false);
                fp.QueryAuthorize = false;
                fp.SetExtendedProperty(AuthorizationPendingProperty, response.Length > 0);
                LoggerFlow(string.Format(
                    "AUTHORIZE pump={0}, responseBytes={1}, pending={2}",
                    fp.Address, response.Length, response.Length > 0));
            }
            catch (Exception ex)
            {
                fp.QueryAuthorize = false;
                LoggerException("Authorize Error " + ex.Message);
            }
        }

        private void StopPump(FuelPoint fp)
        {
            try
            {
                string haltReason = fp.QueryHalt ? "Halt" : "Stop";
                byte[] response = ExecuteAndReadResponse(
                    Commands.Stop(fp.Address), fp, 75, false);
                fp.QueryHalt = false;
                fp.QueryStop = false;
                fp.QuerySetPrice = false;
                fp.Halted = true;
                fp.SetExtendedProperty(LastHaltReasonProperty, haltReason);
                fp.SetExtendedProperty(PumpAuthorizedProperty, false);
                fp.SetExtendedProperty(AuthorizationPendingProperty, false);
                LoggerFlow(string.Format(
                    "{0} pump={1}, responseBytes={2}, halted={3}",
                    haltReason.ToUpperInvariant(), fp.Address, response.Length, fp.Halted));
            }
            catch (Exception ex)
            {
                fp.QueryHalt = false;
                fp.QueryStop = false;
                LoggerException("Stop Error " + ex.Message);
            }
        }

        private void ResumePump(FuelPoint fp)
        {
            try
            {
                fp.QueryResume = false;
                Nozzle activeNozzle = GetCurrentNozzle(fp);
                if (activeNozzle == null)
                {
                    fp.Halted = false;
                    return;
                }

                byte[] response = ExecuteAndReadResponse(
                    Commands.Authorize(fp.Address), fp, 75, false);
                fp.SetExtendedProperty(AuthorizationPendingProperty, response.Length > 0);
                if (response.Length > 0)
                    fp.Halted = false;
                LoggerFlow(string.Format(
                    "RESUME pump={0}, nozzle={1}, responseBytes={2}, halted={3}",
                    fp.Address, activeNozzle.Index, response.Length, fp.Halted));
            }
            catch (Exception ex)
            {
                fp.QueryResume = false;
                LoggerException("Resume Error " + ex.Message);
            }
        }

        private Nozzle GetCurrentNozzle(FuelPoint fp)
        {
            if (fp.ActiveNozzleIndex >= 0 && fp.ActiveNozzleIndex < fp.Nozzles.Count())
                return fp.Nozzles.ElementAt(fp.ActiveNozzleIndex);

            int saleNozzleIndex = (int)fp.GetExtendedProperty(SaleNozzleIndexProperty, -1);
            if (saleNozzleIndex >= 0 && saleNozzleIndex < fp.Nozzles.Count())
                return fp.Nozzles.ElementAt(saleNozzleIndex);

            return null;
        }

        private void BeginInitialization(FuelPoint fp, string reason)
        {
            fp.Initialized = false;
            fp.SetExtendedProperty(InitializationPendingProperty, true);
            fp.SetExtendedProperty(InitializationStatusRepublishRemainingProperty, 0);
            foreach (Nozzle nz in fp.Nozzles)
            {
                nz.SetExtendedProperty(InitialTotalReadProperty, false);
                QueueTotals(nz, "initialization-" + reason);
            }

            if (!fp.Nozzles.Any())
            {
                fp.SetExtendedProperty(InitializationPendingProperty, false);
                fp.Initialized = true;
            }

            LoggerFlow(string.Format(
                "INITIALIZATION started pump={0}, reason={1}, nozzles={2}",
                fp.Address, reason, fp.Nozzles.Count()));
        }

        private void QueueTotals(Nozzle nz, string reason)
        {
            bool alreadyQueued = nz.QueryTotals;
            nz.QueryTotals = true;
            nz.SetExtendedProperty(TotalRequestReasonProperty, reason);
            LoggerFlow(string.Format(
                "TOTALIZER {0} pump={1}, nozzle={2}, reason={3}",
                alreadyQueued ? "request coalesced" : "queued",
                nz.ParentFuelPoint.Address, nz.Index, reason));
        }

        private bool CompleteInitializationRead(Nozzle nz)
        {
            FuelPoint fp = nz.ParentFuelPoint;
            bool initializationPending = (bool)fp.GetExtendedProperty(
                InitializationPendingProperty, false);
            bool wasUnread = !(bool)nz.GetExtendedProperty(
                InitialTotalReadProperty, false);
            if (!initializationPending || !wasUnread)
                return false;

            nz.SetExtendedProperty(InitialTotalReadProperty, true);
            int initializedNozzles = fp.Nozzles.Count(x =>
                (bool)x.GetExtendedProperty(InitialTotalReadProperty, false));
            int nozzleCount = fp.Nozzles.Count();
            LoggerFlow(string.Format(
                "INITIALIZATION progress pump={0}, initializedNozzles={1}/{2}",
                fp.Address, initializedNozzles, nozzleCount));

            if (initializedNozzles != nozzleCount)
                return true;

            fp.SetExtendedProperty(InitializationPendingProperty, false);
            fp.Initialized = true;

            // The shared controller treats Idle + LastStatus Offline as a new
            // recovery and queues every totalizer. The initialization reads above
            // already performed that work, so consume the recovery transition once.
            if (fp.Status == FuelPointStatusEnum.Idle &&
                fp.LastStatus == FuelPointStatusEnum.Offline)
                fp.LastStatus = fp.Status;

            LoggerFlow(string.Format(
                "INITIALIZATION completed pump={0}, status={1}; publishing initialized status",
                fp.Address, fp.Status));
            PublishDispenserStatus(fp, "initialization-complete");
            fp.SetExtendedProperty(InitializationStatusRepublishRemainingProperty,
                InitializationStatusRepublishCount);
            fp.SetExtendedProperty(InitializationStatusRepublishAtProperty,
                DateTime.Now.AddMilliseconds(InitializationStatusRepublishIntervalMilliseconds));
            return true;
        }

        private void RepublishInitializedStatusIfDue(FuelPoint fp)
        {
            if (!fp.Initialized)
                return;

            int remaining = (int)fp.GetExtendedProperty(
                InitializationStatusRepublishRemainingProperty, 0);
            if (remaining <= 0)
                return;

            DateTime publishAt = (DateTime)fp.GetExtendedProperty(
                InitializationStatusRepublishAtProperty, DateTime.MaxValue);
            if (DateTime.Now < publishAt)
                return;

            PublishDispenserStatus(fp, "initialization-confirmation");
            fp.SetExtendedProperty(InitializationStatusRepublishRemainingProperty,
                remaining - 1);
            fp.SetExtendedProperty(InitializationStatusRepublishAtProperty,
                DateTime.Now.AddMilliseconds(InitializationStatusRepublishIntervalMilliseconds));
        }

        private void SetPrice(FuelPoint fp)
        {
            try
            {
                byte[] data = Commands.SetPrice(fp);//, decimal.Parse((nz.UntiPriceInt).ToString().PadLeft(4,'0')));
                byte[] numArray = ExecuteAndReadResponse(data, fp, 50);

                if (numArray.Length < 3 || fp.Status == FuelPointStatusEnum.Offline)
                    return;

                fp.QuerySetPrice = false;

            }
            catch (Exception ex)
            {
                LoggerException(ex.ToString());
            }
        }

        private void ExecuteSlave(byte[] buffer, FuelPoint fp)
        {
            try
            {
                if (buffer == null || buffer.Length == 0)
                    return;
                LoggerFlow(string.Format(
                    "TX pump={0}, command={1}, bytes={2}",
                    fp == null ? -1 : fp.Address, DescribeCommand(buffer),
                    BitConverter.ToString(buffer)));
                if (this.serialPort.IsOpen)
                {
                    this.serialPort.Write(buffer, 0, buffer.Length);                    
                }
                else
                {
                    LoggerFlow("TX skipped: serial port is closed");
                }
                Logger(buffer);

            }
            catch (Exception ex)
            {
                LoggerException(ex.ToString());
            }
        }

        private byte[] ExecuteAndReadResponse(byte[] command, FuelPoint fp, int waitMilliseconds,
            bool allowRetry = true, bool waitForDataFrame = false)
        {
            if (command == null || fp == null)
                return new byte[0];

            byte[] response = new byte[0];
            int attempts = allowRetry ? MaxCrcCommandAttempts : 1;

            for (int attempt = 1; attempt <= attempts; attempt++)
            {
                DateTime attemptStarted = DateTime.Now;
                LoggerFlow(string.Format(
                    "COMMAND pump={0}, name={1}, attempt={2}/{3}, waitMs={4}, waitForData={5}",
                    fp.Address, DescribeCommand(command), attempt, attempts,
                    waitMilliseconds, waitForDataFrame));
                ExecuteSlave(command, fp);
                bool invalidCrc;
                response = ReadSerialResponse(waitMilliseconds, fp.Address, waitForDataFrame, out invalidCrc);

                bool validDataReceived = ContainsCompleteDataFrame(response, fp.Address);
                if (response.Length > 0 && (!invalidCrc || validDataReceived || !allowRetry))
                {
                    LoggerFlow(string.Format(
                        "COMMAND result pump={0}, name={1}, attempt={2}, elapsedMs={3:0}, responseBytes={4}, dataFrame={5}, invalidCrcSeen={6}",
                        fp.Address, DescribeCommand(command), attempt,
                        (DateTime.Now - attemptStarted).TotalMilliseconds,
                        response.Length, validDataReceived, invalidCrc));
                    return response;
                }

                string reason = invalidCrc ? "invalid response CRC" : "no complete response";
                LoggerFlow(string.Format(
                    "COMMAND retry pump={0}, name={1}, attempt={2}, elapsedMs={3:0}, reason={4}, bufferedBytesDiscarded={5}",
                    fp.Address, DescribeCommand(command), attempt,
                    (DateTime.Now - attemptStarted).TotalMilliseconds, reason,
                    this.receiveBuffer.Count));
                LoggerException(string.Format("Command {0}, attempt {1}/{2}: {3}",
                    BitConverter.ToString(command), attempt, attempts, reason));
                this.receiveBuffer.Clear();
            }

            LoggerException(string.Format(
                "Command failed after {0} attempt(s). Command: {1}", attempts,
                BitConverter.ToString(command)));
            LoggerFlow(string.Format(
                "COMMAND failed pump={0}, name={1}, attempts={2}",
                fp.Address, DescribeCommand(command), attempts));
            return new byte[0];
        }

        private string DescribeCommand(byte[] command)
        {
            if (command == null || command.Length < 2)
                return "Unknown";

            if (command.Length == 3)
            {
                if (command[1] == 0x20)
                    return "Poll";
                if (command[1] == 0xC0)
                    return "ACK";
                return string.Format("Control-0x{0:X2}", command[1]);
            }

            if (command.Length >= 5)
            {
                if (command[2] == 0x65)
                    return "RequestVolumeTotalizer";
                if (command[2] == 0x02)
                    return "AllowedNozzle";
                if (command[2] == 0x05)
                    return "SetPrice";
                if (command[2] == 0x01)
                {
                    switch (command[4])
                    {
                        case 0x00: return "GetStatus";
                        case 0x04: return "GetDisplay";
                        case 0x05: return "Reset";
                        case 0x06: return "Authorize";
                        case 0x08: return "Stop";
                    }
                }
            }

            return "DataCommand";
        }

        private bool HasValidDataFrameCrc(byte[] frame, out string validationError)
        {
            validationError = null;
            if (frame == null || frame.Length < 6 || frame[frame.Length - 2] != 0x03 ||
                frame[frame.Length - 1] != 0xFA)
            {
                validationError = "Incomplete data frame";
                return false;
            }

            int crcOffset = frame.Length - 4;
            byte[] crcData = frame.Take(crcOffset).ToArray();
            ushort calculatedCrc = Commands.CRC.ComputeChecksum(crcData);
            ushort receivedCrc = (ushort)(frame[crcOffset] | (frame[crcOffset + 1] << 8));
            if (calculatedCrc == receivedCrc)
                return true;

            validationError = string.Format("received {0:X4}, calculated {1:X4}",
                receivedCrc, calculatedCrc);
            return false;
        }

        public enum PendingCommand_Dart
        {
            None,
            GetStatus,
            GetTotals,
            Authorise,
            Reset,
            Stop,
            Resume,
            AllowedNozzle
        }

        public  void Logger(byte[] buf)
        {
            string fileName = "dart_"+ this.serialPort.PortName +".txt";

            if (File.Exists(fileName))
            {
                using (StreamWriter writer = new StreamWriter(fileName, true, Encoding.UTF8))
                {
                    writer.Write(DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss.fff") + " \t" + BitConverter.ToString(buf) + "  " + "\r\n" /*+ Error_Recieve.ToString() + "\r\n\r\n"*/);
                }
            }
        }

        public void LoggerFlow(string text)
        {
            try
            {
                string portName = string.IsNullOrEmpty(this.serialPort.PortName)
                    ? this.CommunicationPort
                    : this.serialPort.PortName;
                string fileName = "dartflow_" + portName + ".txt";
                if (!File.Exists(fileName))
                    return;

                lock (this.flowLogSync)
                {
                    using (StreamWriter writer = new StreamWriter(fileName, true, Encoding.UTF8))
                    {
                        writer.WriteLine(string.Format(
                            "{0} [T{1}] {2}",
                            DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss.fff"),
                            Thread.CurrentThread.ManagedThreadId, text));
                    }
                }
            }
            catch
            {
            }
        }

        public void LoggerException(string txt)
        {
            try
            {
                string fileName = "dart_errors_" + this.serialPort.PortName + ".txt";
                using (StreamWriter writer = new StreamWriter(fileName, true, Encoding.UTF8))
                {
                    writer.Write(DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss.fff") + " \t" + txt + "  " + "\r\n" /*+ Error_Recieve.ToString() + "\r\n\r\n"*/);
                }
            }
            catch
            {
            }
        }
        public void LoggerExtra(string txt)
        {
            string fileName = "dartextra_" + this.serialPort.PortName + ".txt";
            if (File.Exists(fileName))
            {
                using (StreamWriter writer = new StreamWriter(fileName, true, Encoding.UTF8))
                {
                    writer.Write(DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss.fff") + " \t" + txt + "  " + "\r\n" /*+ Error_Recieve.ToString() + "\r\n\r\n"*/);
                }
            }
        }
        #endregion

        #region Methods

        private void Poll(FuelPoint fp)
        {
            try
            {
                DateTime dtGetStatus = (DateTime)fp.GetExtendedProperty(
                    "dtGetStatus_" + fp.Address, DateTime.MinValue);
                
                
                if ((DateTime.Now - dtGetStatus).TotalSeconds > 2)
                {
                    fp.SetExtendedProperty("dtGetStatus_" + fp.Address, DateTime.Now);
                    byte[] statusResponse = ExecuteAndReadResponse(
                        Commands.GetStatus(fp.Address), fp, 75);
                    if (statusResponse.Length > 0)
                    {
                        fp.SetExtendedProperty("LastValidResponse", DateTime.Now);
                        AnalyzeDart(statusResponse, fp);
                    }
                }

                RequestLiveDisplayIfDue(fp);

                byte[] numArray = ExecuteAndReadResponse(
                    Commands.Poll(fp.Address), fp, 100, true, true);
                if (numArray.Length > 0)
                {
                    fp.SetExtendedProperty("LastValidResponse", DateTime.Now);
                    AnalyzeDart(numArray, fp);
                }

                TryFinalizePendingDelivery(fp);
            }
            catch (Exception ex)
            {
                LoggerException("Poll Error " + ex.Message);
                //Base.Logger.ProtocolErrror(ex.ToString(), "Poll");
            }
        }

        private void RequestLiveDisplayIfDue(FuelPoint fp)
        {
            if (fp.Status != FuelPointStatusEnum.Work)
                return;

            DateTime lastRequest = (DateTime)fp.GetExtendedProperty(
                LastLiveDisplayRequestProperty, DateTime.MinValue);
            if ((DateTime.Now - lastRequest).TotalMilliseconds < LiveDisplayIntervalMilliseconds)
                return;

            fp.SetExtendedProperty(LastLiveDisplayRequestProperty, DateTime.Now);
            LoggerFlow(string.Format(
                "LIVE DISPLAY request pump={0}, nozzle={1}, volume={2}, amount={3}",
                fp.Address, fp.ActiveNozzleIndex + 1,
                fp.DispensedVolume, fp.DispensedAmount));

            byte[] displayResponse = ExecuteAndReadResponse(
                Commands.GetDisplay(fp.Address), fp, 60);
            if (displayResponse.Length == 0)
                return;

            fp.SetExtendedProperty("LastValidResponse", DateTime.Now);
            if (ContainsCompleteDataFrame(displayResponse, fp.Address))
                AnalyzeDart(displayResponse, fp);
        }

        private void GetTotals(Nozzle nz)
        {
            try
            {
                string requestReason = (string)nz.GetExtendedProperty(
                    TotalRequestReasonProperty, "external/common");
                LoggerFlow(string.Format(
                    "TOTALIZER request pump={0}, nozzle={1}, reason={2}, pumpStatus={3}, saleStarted={4}, preSaleReady={5}",
                    nz.ParentFuelPoint.Address, nz.Index, requestReason,
                    nz.ParentFuelPoint.Status,
                    nz.ParentFuelPoint.GetExtendedProperty(SaleStartedProperty, false),
                    nz.ParentFuelPoint.GetExtendedProperty(PreSaleTotalizerReadyProperty, false)));
                byte[] requestResponse = ExecuteAndReadResponse(
                    Commands.RequestVolumeTotalizer(nz.ParentFuelPoint.Address, nz.Index),
                    nz.ParentFuelPoint, 75);

                decimal VolumeTotalizer;
                byte[] numArray = requestResponse;
                if (!TryExtractVolumeTotalizer(
                    numArray, nz.ParentFuelPoint, nz.Index, out VolumeTotalizer))
                {
                    byte[] pollResponse = ExecuteAndReadResponse(
                        Commands.Poll(nz.ParentFuelPoint.Address), nz.ParentFuelPoint,
                        200, true, true);
                    numArray = requestResponse.Concat(pollResponse).ToArray();
                }

                if (!TryExtractVolumeTotalizer(
                    numArray, nz.ParentFuelPoint, nz.Index, out VolumeTotalizer))
                {
                    LoggerFlow(string.Format(
                        "TOTALIZER invalid/missing pump={0}, nozzle={1}, response={2}",
                        nz.ParentFuelPoint.Address, nz.Index,
                        numArray.Length == 0 ? "<empty>" : BitConverter.ToString(numArray)));
                    LoggerException(string.Format(
                        "Volume totalizer response did not contain a valid 0x65 record for nozzle {0}. Response: {1}",
                        nz.Index, BitConverter.ToString(numArray)));
                    return;
                }

                FuelPoint fp = nz.ParentFuelPoint;
                int saleNozzleIndex = (int)fp.GetExtendedProperty(SaleNozzleIndexProperty, -1);
                bool saleStarted = (bool)fp.GetExtendedProperty(SaleStartedProperty, false);
                bool capturePreSale = !saleStarted && fp.ActiveNozzleIndex == nz.Index - 1 &&
                    (fp.Status == FuelPointStatusEnum.Nozzle || fp.Status == FuelPointStatusEnum.Ready) &&
                    !(bool)fp.GetExtendedProperty(PreSaleTotalizerReadyProperty, false);

                if (capturePreSale)
                {
                    fp.SetExtendedProperty(PreSaleTotalizerProperty, VolumeTotalizer);
                    fp.SetExtendedProperty(PreSaleTotalizerReadyProperty, true);
                    fp.SetExtendedProperty(SaleNozzleIndexProperty, nz.Index - 1);
                    LoggerFlow(string.Format(
                        "TOTALIZER pre-sale captured pump={0}, nozzle={1}, value={2}",
                        fp.Address, nz.Index, VolumeTotalizer));
                }

                var prevTotals = nz.TotalVolume;
                nz.TotalVolume = VolumeTotalizer;

                // Preserve the totalizer captured at nozzle lift as the start of this sale.
                // This prevents a stale earlier total from being paired with the post-sale total.
                if (saleStarted && saleNozzleIndex == nz.Index - 1 &&
                    (bool)fp.GetExtendedProperty(PreSaleTotalizerReadyProperty, false))
                {
                    nz.LastTotalVolume = (decimal)fp.GetExtendedProperty(
                        PreSaleTotalizerProperty, nz.LastTotalVolume);
                }

                LoggerFlow(string.Format(
                    "TOTALIZER accepted pump={0}, nozzle={1}, value={2}, previous={3}, lastForSale={4}, phase={5}",
                    fp.Address, nz.Index, VolumeTotalizer, prevTotals,
                    nz.LastTotalVolume, capturePreSale ? "pre-sale" :
                    (saleStarted ? "post-sale" : "regular")));

                ExecuteAndReadResponse(Commands.ACK(fp.Address), fp, 25, false);

                nz.QueryTotals = false;
                nz.SetExtendedProperty(TotalRequestReasonProperty, "completed");
                bool initializationRead = CompleteInitializationRead(nz);

                // The nozzle-lift total is an internal sale-start snapshot, not a
                // completed-sale total. Publishing it would trigger the common totals
                // majority workflow and delay authorization with unnecessary reads.
                if (capturePreSale)
                    return;

                // Initial totals make GetNozzleTotalizer available to the application;
                // they are not completed-sale totals. Publishing them through
                // TotalsRecieved invokes the common three-sample sale validation and
                // needlessly queues the same nozzle again.
                if (initializationRead && !saleStarted)
                {
                    LoggerFlow(string.Format(
                        "TOTALIZER initialization-only pump={0}, nozzle={1}; transaction event suppressed",
                        fp.Address, nz.Index));
                    return;
                }

                if (prevTotals == nz.TotalVolume)
                {
                    LoggerExtra("TOTALS ARE THE SAME");
                    LoggerExtra(string.Format("DISPENCED AMOUNT:{1}, DISPENSED VOLUME: {0}", nz.ParentFuelPoint.DispensedVolume, nz.ParentFuelPoint.DispensedAmount));
                    //return;
                }

                if (this.TotalsRecieved != null)
                    this.TotalsRecieved(this, new Common.TotalsEventArgs(
                        fp, nz.Index, nz.TotalVolume, nz.TotalPrice));

                if (saleStarted && saleNozzleIndex == nz.Index - 1 && !nz.QueryTotals)
                {
                    fp.SetExtendedProperty(SaleStartedProperty, false);
                    fp.SetExtendedProperty(PreSaleTotalizerReadyProperty, false);
                    fp.SetExtendedProperty(SaleNozzleIndexProperty, -1);
                    LoggerFlow(string.Format(
                        "TOTALIZER post-sale completed pump={0}, nozzle={1}, start={2}, end={3}",
                        fp.Address, nz.Index, nz.LastTotalVolume, nz.TotalVolume));
                }
                

            }
            catch (Exception ex)
            {
                LoggerException("Get Totals Error " + ex.Message);
            }
        }

        private bool TryExtractVolumeTotalizer(byte[] response, FuelPoint fp, int nozzleIndex,
            out decimal volumeTotalizer)
        {
            volumeTotalizer = 0M;
            if (response == null || response.Length == 0)
                return false;

            foreach (byte[] frame in GetCompleteDataFrames(response, fp.Address))
            {
                int transactionEnd = frame.Length - 4;
                int transactionOffset = 2;
                while (transactionOffset + 1 < transactionEnd)
                {
                    int transactionLength = frame[transactionOffset + 1];
                    int dataOffset = transactionOffset + 2;
                    if (dataOffset + transactionLength > transactionEnd)
                        break;

                    // Total-counter record: nozzle byte followed by a five-byte packed-BCD volume total.
                    if (frame[transactionOffset] == 0x65 && transactionLength >= 6 &&
                        frame[dataOffset] == (byte)nozzleIndex)
                    {
                        return TryParsePackedBcd(frame, dataOffset + 1, 5, out volumeTotalizer);
                    }

                    transactionOffset = dataOffset + transactionLength;
                }
            }

            return false;
        }

        private void TryFinalizePendingDelivery(FuelPoint fp)
        {
            if (!(bool)fp.GetExtendedProperty(FinalizationPendingProperty, false))
                return;

            DateTime now = DateTime.Now;
            DateTime nextAttempt = (DateTime)fp.GetExtendedProperty(
                FinalizationNextAttemptProperty, now);
            if (now < nextAttempt)
                return;

            DateTime started = (DateTime)fp.GetExtendedProperty(FinalizationStartProperty, now);
            decimal previousVolume = (decimal)fp.GetExtendedProperty(
                FinalizationPreviousVolumeProperty, -1M);
            decimal previousAmount = (decimal)fp.GetExtendedProperty(
                FinalizationPreviousAmountProperty, -1M);
            int stableResponses = (int)fp.GetExtendedProperty(
                FinalizationStableResponsesProperty, 0);
            bool finalValuesReceived = previousVolume >= 0M && previousAmount >= 0M;
            LoggerFlow(string.Format(
                "FINALIZE read pump={0}, elapsedMs={1:0}, stableResponses={2}, previousVolume={3}, previousAmount={4}",
                fp.Address, (now - started).TotalMilliseconds, stableResponses,
                previousVolume, previousAmount));

            byte[] displayResponse = ExecuteAndReadResponse(
                Commands.GetDisplay(fp.Address), fp, 60);
            CaptureFinalDeliveryResponse(fp, displayResponse, ref finalValuesReceived,
                ref previousVolume, ref previousAmount, ref stableResponses);

            byte[] pollResponse = ExecuteAndReadResponse(
                Commands.Poll(fp.Address), fp, 100, true, true);
            CaptureFinalDeliveryResponse(fp, pollResponse, ref finalValuesReceived,
                ref previousVolume, ref previousAmount, ref stableResponses);

            fp.SetExtendedProperty(FinalizationPreviousVolumeProperty, previousVolume);
            fp.SetExtendedProperty(FinalizationPreviousAmountProperty, previousAmount);
            fp.SetExtendedProperty(FinalizationStableResponsesProperty, stableResponses);
            fp.SetExtendedProperty(FinalizationNextAttemptProperty, DateTime.Now.AddMilliseconds(100));
            LoggerFlow(string.Format(
                "FINALIZE result pump={0}, volume={1}, amount={2}, stableResponses={3}, valuesReceived={4}",
                fp.Address, previousVolume, previousAmount, stableResponses,
                finalValuesReceived));

            if (stableResponses >= 2 ||
                (DateTime.Now - started).TotalMilliseconds >= FinalDeliveryReadTimeoutMilliseconds)
            {
                if (!finalValuesReceived)
                {
                    LoggerExtra(string.Format(
                        "FINAL DC2 TIMEOUT. USING BEST OBSERVED AMOUNT:{0}, VOLUME:{1}",
                        fp.DispensedAmount, fp.DispensedVolume));
                }
                CompleteDelivery(fp);
            }
        }

        private void BeginDeliveryFinalization(FuelPoint fp, bool immediate)
        {
            if ((bool)fp.GetExtendedProperty(FinalizationPendingProperty, false))
            {
                if (immediate)
                {
                    fp.SetExtendedProperty(FinalizationNextAttemptProperty, DateTime.Now);
                    LoggerFlow(string.Format(
                        "FINALIZE expedited pump={0}", fp.Address));
                }
                return;
            }

            DateTime now = DateTime.Now;
            fp.SetExtendedProperty(FinalizationPendingProperty, true);
            fp.SetExtendedProperty(FinalizationStartProperty, now);
            fp.SetExtendedProperty(FinalizationNextAttemptProperty,
                immediate ? now : now.AddMilliseconds(DeliveryCloseStatusTimeoutMilliseconds));
            fp.SetExtendedProperty(FinalizationPreviousVolumeProperty, -1M);
            fp.SetExtendedProperty(FinalizationPreviousAmountProperty, -1M);
            fp.SetExtendedProperty(FinalizationStableResponsesProperty, 0);
            LoggerFlow(string.Format(
                "FINALIZE started pump={0}, immediate={1}, status={2}, nozzle={3}",
                fp.Address, immediate, fp.Status, fp.ActiveNozzleIndex + 1));
        }

        private void CompleteDelivery(FuelPoint fp)
        {
            fp.SetExtendedProperty(FinalizationPendingProperty, false);
            fp.SetExtendedProperty(PumpAuthorizedProperty, false);
            fp.SetExtendedProperty(AuthorizationPendingProperty, false);

            int saleNozzleIndex = (int)fp.GetExtendedProperty(
                SaleNozzleIndexProperty, fp.ActiveNozzleIndex);
            if (saleNozzleIndex >= 0 && saleNozzleIndex < fp.Nozzles.Count())
                QueueTotals(fp.Nozzles.ElementAt(saleNozzleIndex), "post-sale");

            fp.Status = FuelPointStatusEnum.Idle;
            fp.DispenserStatus = FuelPointStatusEnum.Idle;
            fp.ActiveNozzleIndex = -1;
            LoggerFlow(string.Format(
                "FINALIZE completed pump={0}, nozzle={1}, volume={2}, amount={3}; post-sale totalizer queued={4}",
                fp.Address, saleNozzleIndex + 1, fp.DispensedVolume,
                fp.DispensedAmount,
                saleNozzleIndex >= 0 && saleNozzleIndex < fp.Nozzles.Count()));
            PublishStatusData(fp, 0, -1);
            PublishDispenserStatus(fp, "delivery-complete");
        }

        private byte[] ReadSerialResponse(int waitMilliseconds, int fuelPointAddress,
            bool waitForDataFrame, out bool invalidCrc)
        {
            invalidCrc = false;
            byte expectedAddress = (byte)(fuelPointAddress + 79);
            List<byte> response = new List<byte>();
            DateTime deadline = DateTime.Now.AddMilliseconds(waitMilliseconds);
            DateTime readStarted = DateTime.Now;
            DateTime lastByteReceived = DateTime.MinValue;
            bool dataFrameReceived = false;
            bool noDataControlReceived = false;
            LoggerFlow(string.Format(
                "RX wait pump={0}, expectedAddress=0x{1:X2}, timeoutMs={2}, waitForData={3}, bufferedBytes={4}",
                fuelPointAddress, expectedAddress, waitMilliseconds,
                waitForDataFrame, this.receiveBuffer.Count));

            while (DateTime.Now < deadline)
            {
                int bytesToRead = this.serialPort.BytesToRead;
                if (bytesToRead > 0)
                {
                    byte[] readBuffer = new byte[bytesToRead];
                    int bytesRead = this.serialPort.Read(readBuffer, 0, bytesToRead);
                    if (bytesRead > 0)
                    {
                        this.receiveBuffer.AddRange(readBuffer.Take(bytesRead));
                        lastByteReceived = DateTime.Now;
                        LoggerFlow(string.Format(
                            "RX chunk pump={0}, bytes={1}, data={2}, bufferedBytes={3}",
                            fuelPointAddress, bytesRead,
                            BitConverter.ToString(readBuffer, 0, bytesRead),
                            this.receiveBuffer.Count));
                    }
                }

                byte[] frame;
                while (TryExtractBufferedFrame(out frame))
                {
                    if (frame[0] != expectedAddress)
                    {
                        LoggerFlow(string.Format(
                            "RX discarded pump={0}, reason=unexpected-address, expected=0x{1:X2}, actual=0x{2:X2}, frame={3}",
                            fuelPointAddress, expectedAddress, frame[0],
                            BitConverter.ToString(frame)));
                        LoggerException(string.Format(
                            "Discarded response for unexpected address {0:X2}; expected {1:X2}",
                            frame[0], expectedAddress));
                        continue;
                    }

                    if (frame.Length > 3)
                    {
                        string validationError;
                        if (!HasValidDataFrameCrc(frame, out validationError))
                        {
                            invalidCrc = true;
                            LoggerFlow(string.Format(
                                "RX data pump={0}, crc=INVALID ({1}), frame={2}",
                                fuelPointAddress, validationError,
                                BitConverter.ToString(frame)));
                            LoggerException("Invalid response CRC: " + validationError +
                                ". Frame: " + BitConverter.ToString(frame));
                            continue;
                        }

                        dataFrameReceived = true;
                        LoggerFlow(string.Format(
                            "RX data pump={0}, crc=OK, frame={1}",
                            fuelPointAddress, BitConverter.ToString(frame)));
                    }
                    else
                    {
                        if (frame[1] == 0x70)
                            noDataControlReceived = true;
                        LoggerFlow(string.Format(
                            "RX control pump={0}, frame={1}",
                            fuelPointAddress, BitConverter.ToString(frame)));
                    }

                    response.AddRange(frame);
                }

                bool quiet = lastByteReceived != DateTime.MinValue &&
                    (DateTime.Now - lastByteReceived).TotalMilliseconds >= ReceiveQuietMilliseconds;
                if (quiet && (dataFrameReceived || noDataControlReceived ||
                    (!waitForDataFrame && response.Count > 0)))
                    break;

                Thread.Sleep(5);
            }

            // Parse bytes that arrived at the deadline before returning.
            byte[] finalFrame;
            while (TryExtractBufferedFrame(out finalFrame))
            {
                if (finalFrame[0] != expectedAddress)
                {
                    LoggerFlow(string.Format(
                        "RX discarded-at-deadline pump={0}, reason=unexpected-address, frame={1}",
                        fuelPointAddress, BitConverter.ToString(finalFrame)));
                    continue;
                }

                if (finalFrame.Length > 3)
                {
                    string validationError;
                    if (!HasValidDataFrameCrc(finalFrame, out validationError))
                    {
                        invalidCrc = true;
                        LoggerFlow(string.Format(
                            "RX data-at-deadline pump={0}, crc=INVALID ({1}), frame={2}",
                            fuelPointAddress, validationError,
                            BitConverter.ToString(finalFrame)));
                        LoggerException("Invalid response CRC: " + validationError +
                            ". Frame: " + BitConverter.ToString(finalFrame));
                        continue;
                    }
                    LoggerFlow(string.Format(
                        "RX data-at-deadline pump={0}, crc=OK, frame={1}",
                        fuelPointAddress, BitConverter.ToString(finalFrame)));
                    dataFrameReceived = true;
                }
                else
                {
                    if (finalFrame[1] == 0x70)
                        noDataControlReceived = true;
                    LoggerFlow(string.Format(
                        "RX control-at-deadline pump={0}, frame={1}",
                        fuelPointAddress, BitConverter.ToString(finalFrame)));
                }

                response.AddRange(finalFrame);
            }

            byte[] result = response.ToArray();
            LoggerFlow(string.Format(
                "RX complete pump={0}, elapsedMs={1:0}, responseBytes={2}, dataFrame={3}, noDataControl={4}, invalidCrcSeen={5}, remainingBufferedBytes={6}{7}",
                fuelPointAddress, (DateTime.Now - readStarted).TotalMilliseconds,
                result.Length, dataFrameReceived, noDataControlReceived,
                invalidCrc, this.receiveBuffer.Count,
                result.Length == 0 ? string.Empty : ", response=" + BitConverter.ToString(result)));
            if (result.Length > 0)
                Logger(result);
            return result;
        }

        private bool TryExtractBufferedFrame(out byte[] frame)
        {
            frame = null;

            int addressOffset = this.receiveBuffer.FindIndex(b => b >= 0x50 && b <= 0x5F);
            if (addressOffset < 0)
            {
                if (this.receiveBuffer.Count > 0)
                {
                    LoggerFlow(string.Format(
                        "RX noise discarded bytes={0}, data={1}",
                        this.receiveBuffer.Count,
                        BitConverter.ToString(this.receiveBuffer.ToArray())));
                }
                this.receiveBuffer.Clear();
                return false;
            }

            if (addressOffset > 0)
            {
                LoggerFlow(string.Format(
                    "RX leading noise discarded bytes={0}, data={1}",
                    addressOffset,
                    BitConverter.ToString(this.receiveBuffer.Take(addressOffset).ToArray())));
                this.receiveBuffer.RemoveRange(0, addressOffset);
            }

            if (this.receiveBuffer.Count < 3)
                return false;

            if (this.receiveBuffer[2] == 0xFA)
            {
                frame = this.receiveBuffer.Take(3).ToArray();
                this.receiveBuffer.RemoveRange(0, 3);
                return true;
            }

            int frameEnd = -1;
            for (int i = 2; i + 1 < this.receiveBuffer.Count; i++)
            {
                if (this.receiveBuffer[i] == 0x03 && this.receiveBuffer[i + 1] == 0xFA)
                {
                    frameEnd = i + 1;
                    break;
                }
            }

            if (frameEnd < 0)
                return false;

            frame = this.receiveBuffer.Take(frameEnd + 1).ToArray();
            this.receiveBuffer.RemoveRange(0, frameEnd + 1);
            return true;
        }

        private void CaptureFinalDeliveryResponse(FuelPoint fp, byte[] response,
            ref bool finalValuesReceived, ref decimal previousVolume, ref decimal previousAmount,
            ref int stableResponses)
        {
            decimal volume;
            decimal amount;
            if (TryExtractFilledValues(response, fp, out volume, out amount))
            {
                ApplyFilledValues(fp, volume, amount);
                finalValuesReceived = true;

                if (volume == previousVolume && amount == previousAmount)
                    stableResponses++;
                else
                    stableResponses = 1;

                previousVolume = volume;
                previousAmount = amount;
            }

            if (ContainsCompleteDataFrame(response, fp.Address))
                ExecuteAndReadResponse(Commands.ACK(fp.Address), fp, 25, false);
        }

        private bool TryExtractFilledValues(byte[] response, FuelPoint fp, out decimal volume, out decimal amount)
        {
            volume = 0M;
            amount = 0M;
            bool found = false;
            foreach (byte[] frame in GetCompleteDataFrames(response, fp.Address))
            {
                int transactionEnd = frame.Length - 4;
                int transactionOffset = 2;
                while (transactionOffset + 1 < transactionEnd)
                {
                    int transactionLength = frame[transactionOffset + 1];
                    int dataOffset = transactionOffset + 2;
                    if (dataOffset + transactionLength > transactionEnd)
                        break;

                    if (frame[transactionOffset] == 0x02 && transactionLength == 8)
                    {
                        decimal rawVolume;
                        decimal rawAmount;
                        if (TryParsePackedBcd(frame, dataOffset, 4, out rawVolume) &&
                            TryParsePackedBcd(frame, dataOffset + 4, 4, out rawAmount))
                        {
                            decimal parsedVolume = rawVolume / (decimal)Math.Pow(10, fp.VolumeDecimalPlaces);
                            decimal parsedAmount = rawAmount / (decimal)Math.Pow(10, fp.AmountDecimalPlaces);
                            volume = found ? Math.Max(volume, parsedVolume) : parsedVolume;
                            amount = found ? Math.Max(amount, parsedAmount) : parsedAmount;
                            found = true;
                        }
                    }

                    transactionOffset = dataOffset + transactionLength;
                }
            }

            return found;
        }

        private bool ContainsCompleteDataFrame(byte[] response, int fuelPointAddress)
        {
            return GetCompleteDataFrames(response, fuelPointAddress).Count > 0;
        }

        private List<byte[]> GetCompleteDataFrames(byte[] response, int fuelPointAddress)
        {
            List<byte[]> frames = new List<byte[]>();
            if (response == null || response.Length < 3)
                return frames;

            byte address = (byte)(fuelPointAddress + 79);
            int cursor = 0;
            while (cursor < response.Length)
            {
                int frameStart = Array.IndexOf(response, address, cursor);
                if (frameStart < 0 || frameStart + 2 >= response.Length)
                    break;

                if (response[frameStart + 2] == 0xFA)
                {
                    cursor = frameStart + 3;
                    continue;
                }

                int frameEnd = FindFrameEnd(response, frameStart + 2);
                if (frameEnd < 0)
                    break;

                frames.Add(response.Skip(frameStart).Take(frameEnd + 2 - frameStart).ToArray());
                cursor = frameEnd + 2;
            }

            return frames;
        }

        private int FindFrameEnd(byte[] response, int startIndex)
        {
            for (int i = startIndex; i + 1 < response.Length; i++)
            {
                if (response[i] == 0x03 && response[i + 1] == 0xFA)
                    return i;
            }

            return -1;
        }

        private bool TryParsePackedBcd(byte[] data, int offset, int count, out decimal value)
        {
            value = 0M;
            if (offset < 0 || count < 1 || offset + count > data.Length)
                return false;

            for (int i = offset; i < offset + count; i++)
            {
                int high = (data[i] >> 4) & 0x0F;
                int low = data[i] & 0x0F;
                if (high > 9 || low > 9)
                    return false;

                value = (value * 100M) + (high * 10) + low;
            }

            return true;
        }

        private void ApplyFilledValues(FuelPoint fp, byte[] clearData)
        {
            decimal rawVolume;
            decimal rawAmount;
            if (clearData.Length < 8 ||
                !TryParsePackedBcd(clearData, 0, 4, out rawVolume) ||
                !TryParsePackedBcd(clearData, 4, 4, out rawAmount))
                return;

            ApplyFilledValues(fp,
                rawVolume / (decimal)Math.Pow(10, fp.VolumeDecimalPlaces),
                rawAmount / (decimal)Math.Pow(10, fp.AmountDecimalPlaces));
        }

        private void ApplyFilledValues(FuelPoint fp, decimal volume, decimal amount)
        {
            decimal previousVolume = fp.DispensedVolume;
            decimal previousAmount = fp.DispensedAmount;
            fp.DispensedVolume = Math.Max(fp.DispensedVolume, volume);
            fp.DispensedAmount = Math.Max(fp.DispensedAmount, amount);
            LoggerFlow(string.Format(
                "DELIVERY pump={0}, nozzle={1}, receivedVolume={2}, receivedAmount={3}, storedVolume={4}->{5}, storedAmount={6}->{7}",
                fp.Address, fp.ActiveNozzleIndex + 1, volume, amount,
                previousVolume, fp.DispensedVolume,
                previousAmount, fp.DispensedAmount));

            if (fp.DispensedAmount <= 0 || this.DataChanged == null)
                return;

            int nozzleIndex = fp.ActiveNozzleIndex;
            if (nozzleIndex < 0 || nozzleIndex >= fp.Nozzles.Count())
                nozzleIndex = (int)fp.GetExtendedProperty(SaleNozzleIndexProperty, -1);
            if (nozzleIndex < 0 || nozzleIndex >= fp.Nozzles.Count())
            {
                LoggerFlow(string.Format(
                    "LIVE DISPLAY ignored pump={0}: no active sale nozzle", fp.Address));
                return;
            }

            this.DataChanged(this, new Common.FuelPointValuesArgs()
            {
                CurrentFuelPoint = fp,
                CurrentNozzleId = nozzleIndex + 1,
                Values = new Common.FuelPointValues()
                {
                    CurrentSalePrice = fp.Nozzles[nozzleIndex].UnitPrice,
                    CurrentPriceTotal = fp.DispensedAmount,
                    CurrentVolume = fp.DispensedVolume,
                    ActiveNozzle = nozzleIndex
                }
            });
        }

        private void AnalyzeDart(byte[] response, FuelPoint fp)
        {
            try
            {
                FuelPointStatusEnum previousStatus = fp.Status;
                int previousNozzle = fp.ActiveNozzleIndex;
                List<byte[]> dataFrames = GetCompleteDataFrames(response, fp.Address);
                foreach (byte[] frame in dataFrames)
                {
                    LoggerFlow(string.Format(
                        "ANALYZE pump={0}, frameBytes={1}, frame={2}",
                        fp.Address, frame.Length, BitConverter.ToString(frame)));
                    int transactionEnd = frame.Length - 4;
                    int transactionOffset = 2;
                    while (transactionOffset + 1 < transactionEnd)
                    {
                        byte dataType = frame[transactionOffset];
                        int dataLength = frame[transactionOffset + 1];
                        int dataOffset = transactionOffset + 2;
                        if (dataOffset + dataLength > transactionEnd)
                        {
                            LoggerException("Malformed Dart transaction: " + BitConverter.ToString(frame));
                            break;
                        }

                        byte[] clearData = frame.Skip(dataOffset).Take(dataLength).ToArray();
                        LoggerFlow(string.Format(
                            "ANALYZE transaction pump={0}, type=0x{1:X2}, length={2}, data={3}",
                            fp.Address, dataType, dataLength,
                            clearData.Length == 0 ? "<empty>" : BitConverter.ToString(clearData)));
                        switch (dataType)
                        {
                            case 0x01:
                                HandleStatusTransaction(fp, clearData);
                                break;
                            case 0x02:
                                if (clearData.Length >= 8 && fp.Status != FuelPointStatusEnum.Offline)
                                    ApplyFilledValues(fp, clearData);
                                break;
                            case 0x03:
                                HandleNozzleStatusTransaction(fp, clearData);
                                break;
                        }

                        transactionOffset = dataOffset + dataLength;
                    }

                    byte[] ackResponse = ExecuteAndReadResponse(
                        Commands.ACK(fp.Address), fp, 25, false);
                    if (ContainsCompleteDataFrame(ackResponse, fp.Address))
                        AnalyzeDart(ackResponse, fp);
                }

                fp.DispenserStatus = fp.Status;
                if (previousStatus != fp.Status || previousNozzle != fp.ActiveNozzleIndex)
                    PublishDispenserStatus(fp, "state-change");
            }
            catch (Exception ex)
            {
                LoggerException("Analyze Error " + ex.Message);
            }
        }

        private void HandleStatusTransaction(FuelPoint fp, byte[] clearData)
        {
            if (clearData == null || clearData.Length != 1)
                return;

            FuelPointStatusEnum previousStatus = fp.Status;
            bool previousAuthorized = (bool)fp.GetExtendedProperty(
                PumpAuthorizedProperty, false);
            switch (clearData[0])
            {
                case 0x00:
                case 0x01:
                    fp.SetExtendedProperty(PumpAuthorizedProperty, false);
                    fp.SetExtendedProperty(AuthorizationPendingProperty, false);
                    if (fp.Status == FuelPointStatusEnum.Work ||
                        fp.Status == FuelPointStatusEnum.TransactionCompleted ||
                        ((bool)fp.GetExtendedProperty(SaleStartedProperty, false) &&
                         fp.ActiveNozzleIndex >= 0))
                    {
                        BeginDeliveryFinalization(fp, false);
                    }
                    else
                    {
                        fp.Status = FuelPointStatusEnum.Idle;
                        fp.DispenserStatus = FuelPointStatusEnum.Idle;
                        fp.ActiveNozzleIndex = -1;
                    }
                    break;

                case 0x02:
                    fp.SetExtendedProperty(PumpAuthorizedProperty, true);
                    fp.SetExtendedProperty(AuthorizationPendingProperty, false);
                    fp.Halted = false;
                    if (fp.Status != FuelPointStatusEnum.Nozzle && fp.Status != FuelPointStatusEnum.Work)
                    {
                        fp.Status = FuelPointStatusEnum.Ready;
                        fp.DispenserStatus = FuelPointStatusEnum.Ready;
                    }
                    break;

                case 0x04:
                    bool saleAlreadyStarted = (bool)fp.GetExtendedProperty(
                        SaleStartedProperty, false);
                    if (!saleAlreadyStarted)
                    {
                        fp.DispensedAmount = 0M;
                        fp.DispensedVolume = 0M;
                    }
                    fp.SetExtendedProperty(SaleStartedProperty, true);
                    if (!saleAlreadyStarted)
                        fp.SetExtendedProperty(LastLiveDisplayRequestProperty, DateTime.MinValue);
                    fp.SetExtendedProperty(FinalizationPendingProperty, false);
                    fp.SetExtendedProperty(PumpAuthorizedProperty, true);
                    fp.SetExtendedProperty(AuthorizationPendingProperty, false);
                    fp.Halted = false;
                    if (fp.ActiveNozzleIndex >= 0)
                        fp.SetExtendedProperty(SaleNozzleIndexProperty, fp.ActiveNozzleIndex);
                    fp.Status = FuelPointStatusEnum.Work;
                    fp.DispenserStatus = FuelPointStatusEnum.Work;
                    PublishStatusData(fp, fp.ActiveNozzleIndex + 1, fp.ActiveNozzleIndex);
                    break;

                case 0x05:
                case 0x06:
                    fp.SetExtendedProperty(PumpAuthorizedProperty, false);
                    fp.SetExtendedProperty(AuthorizationPendingProperty, false);
                    if (fp.Status == FuelPointStatusEnum.Work ||
                        fp.Status == FuelPointStatusEnum.TransactionCompleted ||
                        ((bool)fp.GetExtendedProperty(SaleStartedProperty, false) &&
                         fp.ActiveNozzleIndex >= 0))
                    {
                        fp.Status = FuelPointStatusEnum.TransactionCompleted;
                        fp.DispenserStatus = FuelPointStatusEnum.TransactionCompleted;
                        BeginDeliveryFinalization(fp, true);
                    }
                    break;

                case 0x07:
                    fp.SetExtendedProperty(PumpAuthorizedProperty, false);
                    fp.SetExtendedProperty(AuthorizationPendingProperty, false);
                    if ((bool)fp.GetExtendedProperty(SaleStartedProperty, false) &&
                        fp.ActiveNozzleIndex >= 0)
                        BeginDeliveryFinalization(fp, true);
                    else
                    {
                        fp.Status = FuelPointStatusEnum.Close;
                        fp.DispenserStatus = FuelPointStatusEnum.Close;
                        fp.ActiveNozzleIndex = -1;
                    }
                    break;
            }

            LoggerFlow(string.Format(
                "STATE DC1 pump={0}, code=0x{1:X2}, status={2}->{3}, authorized={4}->{5}, halted={6}, activeNozzle={7}, finalizationPending={8}",
                fp.Address, clearData[0], previousStatus, fp.Status,
                previousAuthorized,
                fp.GetExtendedProperty(PumpAuthorizedProperty, false),
                fp.Halted, fp.ActiveNozzleIndex + 1,
                fp.GetExtendedProperty(FinalizationPendingProperty, false)));
        }

        private void HandleNozzleStatusTransaction(FuelPoint fp, byte[] clearData)
        {
            if (clearData == null || clearData.Length < 4)
                return;

            FuelPointStatusEnum previousStatus = fp.Status;
            int previousNozzle = fp.ActiveNozzleIndex;
            byte nozzleStatus = clearData[3];
            if (nozzleStatus <= 0x0F)
            {
                if (fp.Status == FuelPointStatusEnum.Work ||
                    fp.Status == FuelPointStatusEnum.TransactionCompleted)
                {
                    BeginDeliveryFinalization(fp, false);
                }
                else
                {
                    fp.Status = FuelPointStatusEnum.Idle;
                    fp.DispenserStatus = FuelPointStatusEnum.Idle;
                    fp.ActiveNozzleIndex = -1;
                    PublishStatusData(fp, 0, -1);
                }
                LoggerFlow(string.Format(
                    "STATE DC3 pump={0}, nozzleCode=0x{1:X2}, status={2}->{3}, activeNozzle={4}->{5}, action=nozzle-down",
                    fp.Address, nozzleStatus, previousStatus, fp.Status,
                    previousNozzle + 1, fp.ActiveNozzleIndex + 1));
                return;
            }

            if (nozzleStatus < 0x11 || nozzleStatus > 0x18)
                return;

            int selectedNozzleIndex = nozzleStatus - 0x11;
            if (selectedNozzleIndex < 0 || selectedNozzleIndex >= fp.Nozzles.Count())
                return;

            bool newLift = fp.ActiveNozzleIndex != selectedNozzleIndex ||
                fp.Status == FuelPointStatusEnum.Idle ||
                fp.Status == FuelPointStatusEnum.Offline ||
                fp.Status == FuelPointStatusEnum.Close;

            fp.ActiveNozzleIndex = selectedNozzleIndex;
            fp.SetExtendedProperty(SaleNozzleIndexProperty, selectedNozzleIndex);
            if (newLift)
            {
                fp.SetExtendedProperty(PreSaleTotalizerReadyProperty, false);
                fp.SetExtendedProperty(SaleStartedProperty, false);
                QueueTotals(fp.Nozzles.ElementAt(selectedNozzleIndex), "nozzle-lift");
                ExecuteAndReadResponse(
                    Commands.AllowedNozzle(fp.Address, selectedNozzleIndex + 1), fp, 50, false);
            }

            if (fp.Status != FuelPointStatusEnum.Work)
            {
                fp.Status = FuelPointStatusEnum.Nozzle;
                fp.DispenserStatus = FuelPointStatusEnum.Nozzle;
            }
            LoggerFlow(string.Format(
                "STATE DC3 pump={0}, nozzleCode=0x{1:X2}, status={2}->{3}, activeNozzle={4}->{5}, newLift={6}",
                fp.Address, nozzleStatus, previousStatus, fp.Status,
                previousNozzle + 1, fp.ActiveNozzleIndex + 1, newLift));
            PublishStatusData(fp, selectedNozzleIndex + 1, selectedNozzleIndex);
        }

        private void PublishStatusData(FuelPoint fp, int currentNozzleId, int activeNozzle)
        {
            if (this.DataChanged == null)
                return;

            this.DataChanged(this, new FuelPointValuesArgs()
            {
                CurrentFuelPoint = fp,
                CurrentNozzleId = currentNozzleId,
                Values = new FuelPointValues()
                {
                    Status = fp.Status,
                    ActiveNozzle = activeNozzle
                }
            });
        }

        private void PublishDispenserStatus(FuelPoint fp, string reason)
        {
            if (this.DispenserStatusChanged == null)
                return;

            LoggerFlow(string.Format(
                "STATUS publish pump={0}, status={1}, nozzle={2}, initialized={3}, reason={4}",
                fp.Address, fp.Status, fp.ActiveNozzleIndex + 1,
                fp.Initialized, reason));
            this.DispenserStatusChanged(this, new FuelPointValuesArgs()
            {
                CurrentFuelPoint = fp,
                CurrentNozzleId = fp.ActiveNozzleIndex + 1,
                Values = new FuelPointValues()
                {
                    Status = fp.Status,
                    ActiveNozzle = fp.ActiveNozzleIndex
                }
            });
        }

        /*
           if (this.TotalsRecieved != null)
           {
            this.TotalsRecieved(this, new Common.TotalsEventArgs(fp, nz.Index, nz.TotalVolume, nz.TotalPrice));
            }
         */

        #endregion
    }
}
