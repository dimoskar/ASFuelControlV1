using ASFuelControl.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ASFuelControl.Tatsuno
{
    public class TasunoController : Common.IFuelProtocol
    {
        private TatsunoPdeClient client;

#pragma warning disable CS0067
        public event EventHandler<Common.FuelPointValuesArgs> DataChanged;
        public event EventHandler<Common.SaleEventArgs> SaleRecieved;
        public event EventHandler DispenserOffline;
#pragma warning restore CS0067

        public event EventHandler<Common.TotalsEventArgs> TotalsRecieved;
        public event EventHandler<Common.FuelPointValuesArgs> DispenserStatusChanged;

        private List<Common.FuelPoint> fuelPoints = new List<Common.FuelPoint>();
        private List<Common.FuelPoint> errorFps = new List<Common.FuelPoint>();

        private System.IO.Ports.SerialPort serialPort = new System.IO.Ports.SerialPort();
        private System.Threading.Thread th;
        private bool stopping = false;
        private object disposeLock = new object();

        // New primitives for wait-notify pattern
        private ManualResetEventSlim workEvent;
        private CancellationTokenSource cts;

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
                return this.client != null && this.client.IsOpen();
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
                // Ensure any previous resources are cleaned up before creating new ones
                lock (disposeLock)
                {
                    StopCleanup();
                }

                this.stopping = false;

                // create synchronization primitives
                this.workEvent = new ManualResetEventSlim(false);
                this.cts = new CancellationTokenSource();

                this.serialPort = new SerialPort(this.CommunicationPort, 9600, Parity.Even, 7, StopBits.Two)
                {
                    Handshake = Handshake.RequestToSend
                };

                client = new TatsunoPdeClient(this.serialPort, new PdeOptions() { });

                this.serialPort.Open();

                this.th = new System.Threading.Thread(new System.Threading.ThreadStart(this.ThreadRun));
                this.th.IsBackground = true;
                th.Start();
            }
            catch (Exception ex)
            {
                Common.Logger.Instance.Error("Connection Failed" + ex.Message);
                // Ensure no resources are left open on failure
                try
                {
                    lock (disposeLock)
                    {
                        StopCleanup();
                    }
                }
                catch { }
            }
        }

        public void Disconnect()
        {
            try
            {
                // Signal the thread to stop and clean up resources
                this.stopping = true;

                // Cancel token and wake thread so it can exit quickly
                try
                {
                    this.cts?.Cancel();
                }
                catch { }

                try
                {
                    this.workEvent?.Set();
                }
                catch { }

                lock (disposeLock)
                {
                    StopCleanup();
                }

                if (th != null && th.IsAlive)
                {
                    // Give the thread some time to exit cleanly
                    if (!th.Join(2000))
                    {
                        try
                        {
                            th.Abort();
                        }
                        catch (Exception ex)
                        {
                            Logger.Instance.Error("Thread abort failed: " + ex.Message);
                        }
                    }
                    th = null;
                }
            }
            catch (Exception ex)
            {
                Common.Logger.Instance.Error("Disconnect error: " + ex.Message);
            }
        }
        private void StopCleanup()
        {
            lock (disposeLock)
            {
                try
                {
                    if (this.serialPort != null)
                    {
                        try
                        {
                            if (this.serialPort.IsOpen)
                            {
                                try { this.serialPort.Close(); }
                                catch (Exception ex) { Logger.Instance.Error("Error closing serial port: " + ex.Message); }
                            }
                        }
                        finally
                        {
                            try { this.serialPort.Dispose(); }
                            catch (Exception ex) { Logger.Instance.Error("Error disposing serial port: " + ex.Message); }
                            finally { this.serialPort = null; }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error("Unexpected error during serial port cleanup: " + ex.Message);
                }

                try
                {
                    if (this.client != null)
                    {
                        try { this.client.Dispose(); }
                        catch (Exception ex) { Logger.Instance.Error("Error disposing TatsunoPdeClient: " + ex.Message); }
                        finally { this.client = null; }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error("Unexpected error during client cleanup: " + ex.Message);
                }

                // Dispose synchronization primitives
                try
                {
                    if (this.cts != null)
                    {
                        try { this.cts.Dispose(); }
                        catch (Exception ex) { Logger.Instance.Error("Error disposing cts: " + ex.Message); }
                        finally { this.cts = null; }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error("Unexpected error disposing cts: " + ex.Message);
                }

                try
                {
                    if (this.workEvent != null)
                    {
                        try { this.workEvent.Set(); this.workEvent.Dispose(); }
                        catch (Exception ex) { Logger.Instance.Error("Error disposing workEvent: " + ex.Message); }
                        finally { this.workEvent = null; }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error("Unexpected error disposing workEvent: " + ex.Message);
                }
            }
        }
        public void AddFuelPoint(Common.FuelPoint fp)
        {
            this.fuelPoints.Add(fp);
            // Wake worker so it can process the new fuel point immediately
            try { this.workEvent?.Set(); } catch { }
        }
        public void ClearFuelPoints()
        {
            this.fuelPoints.Clear();
            try { this.workEvent?.Set(); } catch { }
        }
        public Common.DebugValues DebugStatusDialog(Common.FuelPoint fp)
        {
            // Debugging helper - left as-is
            return new DebugValues();
        }
        private void HandleAck(AckResult ack)
        {
            if (!ack.Success)
            {
                // NAK received — no response expected
                return;
            }
            client.ReadAndPoolResponse((byte)ack.EchoedAddress);
            var response = client.GetNextResponse(200);
            if (ack.AddressMatched)
            {
                if(response == null || response.Item2 == null)
                {
                    return;
                }
                HandleResponse(response);
            }
            else
            {
                // ACK mismatched — response likely belongs to another request
            }
        }
        private void InitFuelPoint(Common.FuelPoint fp)
        {
            try
            {
                var ack = client.SendRequest((byte)(32 + fp.Address), 'P', "");
                HandleAck(ack);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(string.Format("Dispenser Init Exception (Address {1}, Channel {2}) has failed to initiliaze. Communication Port: {0}", this.CommunicationPort, fp.Address, fp.Channel));
                Logger.Instance.Error(ex.Message);
                Logger.Instance.Error(ex.StackTrace);
            }
        }
        private void LogCommunication() 
        {
            var messages = client.GetLogLines();
            foreach (var msg in messages)
            {
                Logger.Instance.Debug(msg);
            }
        }
        private void ThreadRun()
        {
            var token = this.cts?.Token ?? CancellationToken.None;

            // Use the controller-level errorFps list (don't shadow it)
            // Initialize fuel points which are not initialized and not in error list
            while (!this.stopping && !token.IsCancellationRequested)
            {
                var fuelPointToInit = this.fuelPoints.Where(fp => fp.Initialized == false && !this.errorFps.Contains(fp)).ToList();
                if (!fuelPointToInit.Any())
                    break;
                foreach (var fp in fuelPointToInit)
                {
                    if (this.stopping || token.IsCancellationRequested) break;
                    InitFuelPoint(fp);
                }

                // If still fuel points pending initialization, wait efficiently before retrying
                var remaining = this.fuelPoints.Where(fp => fp.Initialized == false && !this.errorFps.Contains(fp)).Any();
                if (remaining && !this.stopping && !token.IsCancellationRequested)
                {
                    // Wait to be signalled or timeout
                    try { this.workEvent?.Wait(500, token); } catch (OperationCanceledException) { break; }
                }
                LogCommunication();
            }

            foreach (Common.FuelPoint fp in this.fuelPoints)
            {
                if (this.errorFps.Contains(fp))
                    continue;
                foreach (Nozzle nz in fp.Nozzles)
                {
                    ASFuelControl.Common.Logger.Instance.Trace(string.Format("Nozzle Data. FuelPoint Address: {3} Index:{0}, NozzleIndex: {1}, NozzleSocket: {2}", nz.Index, nz.NozzleIndex, nz.NozzleSocket, fp.Address));
                    nz.QuerySetPrice = true;
                }
                try
                {
                    foreach (var nz in fp.Nozzles)
                    {
                        nz.QueryTotals = true;
                    }
                    while (true)
                    {
                        if (this.stopping || token.IsCancellationRequested) break;

                        foreach (var nz in fp.Nozzles)
                        {
                            if (this.stopping || token.IsCancellationRequested) break;

                            if (!nz.QueryTotals)
                            {
                                // Efficient wait instead of Thread.Sleep(50)
                                try { this.workEvent?.Wait(50, token); } catch (OperationCanceledException) { break; }
                                continue;
                            }
                            var payload = nz.Index.ToString("D2");
                            var ack = client.SendRequest((byte)(32 + fp.Address), 'X', payload);
                            HandleAck(ack);
                        }

                        var notRecCount = fp.Nozzles.Count(n => n.QueryTotals);
                        if (notRecCount == 0)
                        {
                            Logger.Instance.Debug("Totals Recieved");
                            break;
                        }
                        else
                        {
                            Logger.Instance.Debug("Totals not Recieved for " + notRecCount + " of " + fp.Nozzles.Length);
                        }
                        try { this.workEvent?.Wait(200, token); } catch (OperationCanceledException) { break; }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error(string.Format("Dispenser Init Exception (Address {1}, Channel {2}) has failed to initiliaze. Communication Port: {0}", this.CommunicationPort, fp.Address, fp.Channel));
                    Logger.Instance.Error(ex.Message);
                    Logger.Instance.Error(ex.StackTrace);
                }
                finally
                {
                    LogCommunication();
                }
            }

            // Main polling loop - uses wait-notify instead of busy spin
            while (this.IsConnected && !this.stopping && !(this.cts?.IsCancellationRequested ?? false))
            {
                foreach (var fp in this.fuelPoints)
                {
                    // Instead of Thread.Sleep(50) use event wait with timeout to yield CPU and support wakeups.
                    try { this.workEvent?.Wait(50, token); } catch (OperationCanceledException) { break; }

                    try
                    {
                        if (this.errorFps.Contains(fp))
                            continue;

                        if (fp.QueryHalt)
                        {
                            fp.QueryHalt = !this.Halt(fp);
                            continue;
                        }

                        var ack = client.SendRequest((byte)(32 + fp.Address), 'S', "");
                        HandleAck(ack);
                    }
                    catch(Exception ex)
                    {
                        Common.Logger.Instance.Error(ex.Message);
                        Common.Logger.Instance.Error(ex.StackTrace);
                        // Instead of Thread.Sleep(50)
                        try { this.workEvent?.Wait(50, token); } catch (OperationCanceledException) { break; }
                    }
                }
                LogCommunication();
                var errorFuelpoint = this.errorFps.ToArray();
                foreach (var fp in errorFuelpoint)
                {
                    if (this.stopping || token.IsCancellationRequested) break;
                    InitFuelPoint(fp);
                }
                LogCommunication();
            }
        }

        private void HandleResponse(Tuple<byte, object> response)
        {
            var address = response.Item1;
            object payload = response.Item2;
            var fpRsp = this.fuelPoints.Where(f => f.Address == (address - 32)).FirstOrDefault();

            if (payload is PdeInitReq)
            {
                var code = ((PdeInitReq)payload).Code;
                var authCode = TatsunoPdeClient.AuthCompute(code, address);
                var ack = client.SendRequest(address, 'I', authCode);
                if (ack.Success)
                {
                    HandleAck(ack);
                }
            }
            else if (payload is PdeError)
            {
                if (fpRsp != null && fpRsp.Initialized)
                {
                    fpRsp.Initialized = false;
                    if (!this.errorFps.Contains(fpRsp))
                        this.errorFps.Add(fpRsp);
                }
                else
                {
                    var err = (PdeError)payload;
                    if (err.Code == 99)
                    {
                        if (fpRsp != null)
                        {
                            fpRsp.Initialized = true;
                            fpRsp.QuerySetPrice = true;
                        }
                    }
                    else if (err.Code == 63)
                    {
                        if (fpRsp != null && !this.errorFps.Contains(fpRsp))
                            this.errorFps.Add(fpRsp);
                    }
                    else if (err.Code == 00)
                    {
                        return;
                    }
                    else
                    {
                        if (fpRsp != null && !this.errorFps.Contains(fpRsp))
                            this.errorFps.Add(fpRsp);
                    }
                }
            }
            else if (payload is PdeRegisters)
            {
                var totalResult = (PdeRegisters)payload;
                if (fpRsp != null)
                {
                    if (totalResult == null)
                    {
                        Logger.Instance.Debug(string.Format("Totals Result is null"));
                        try { this.workEvent?.Wait(50); } catch { }
                        return;
                    }
                    var nz = fpRsp.Nozzles.Where(n => n.Index == totalResult.NozzleIndex).FirstOrDefault();
                    if (nz != null)
                    {
                        nz.TotalVolume = totalResult.Volume;
                        nz.TotalPrice = 0;
                        nz.QueryTotals = false;

                        // Thread-safe event invocation: copy to local variable before invoking
                        var totalsHandler = this.TotalsRecieved;
                        if (totalsHandler != null)
                        {
                            totalsHandler(this, new Common.TotalsEventArgs(fpRsp, nz.Index, nz.TotalVolume, nz.TotalPrice));
                        }
                    }
                }
            }
            else if (payload is PdeStatus)
            {
                var statusResult = (PdeStatus)payload;
                if (fpRsp != null)
                {
                    HandleStatus(fpRsp, statusResult);
                }
            }
            else if(payload is PdeDisplay)
            {
                var display = (PdeDisplay)payload;
                if (fpRsp != null)
                {
                    fpRsp.DispensedAmount = display.Amount / (decimal)System.Math.Pow(10, fpRsp.AmountDecimalPlaces);
                    fpRsp.DispensedVolume = display.Volume / (decimal)System.Math.Pow(10, fpRsp.VolumeDecimalPlaces);
                    var handler = this.DataChanged;
                    if (handler != null && fpRsp.ActiveNozzle != null)
                    {
                        handler(this, new Common.FuelPointValuesArgs()
                        {
                            CurrentFuelPoint = fpRsp,
                            CurrentNozzleId = fpRsp.ActiveNozzle.Index,
                            Values = new Common.FuelPointValues()
                            {
                                CurrentSalePrice = fpRsp.ActiveNozzle.UnitPrice,
                                CurrentPriceTotal = fpRsp.DispensedAmount,
                                CurrentVolume = fpRsp.DispensedVolume,
                            }
                        });
                    }
                }
            }
        }

        private void HandleStatus(FuelPoint fp, PdeStatus statusResult)
        {
            try { this.workEvent?.Wait(50); } catch { }

            // If status is null or indicates an out-of-service/error, mark the fuel point in the shared error list
            if (statusResult == null)
            {
                Logger.Instance.Error(string.Format("Dispenser (Address {1}, Channel {2}) has failed to send status. Communication Port: {0}", this.CommunicationPort, fp.Address, fp.Channel));
                if (!this.errorFps.Contains(fp))
                    this.errorFps.Add(fp);
            }
            else if (statusResult.State > 4)
            {
                if (statusResult.Error != null)
                {
                    Logger.Instance.Error(string.Format("Dispenser (Address {1}, Channel {2}) has failed to send status. Error Code : {3}. Communication Port: {0}",
                        this.CommunicationPort, fp.Address, fp.Channel, statusResult.Error.Code));
                }
                else
                    Logger.Instance.Error(string.Format("Dispenser (Address {1}, Channel {2}) is out of service. Communication Port: {0}", this.CommunicationPort, fp.Address, fp.Channel));
                if (!this.errorFps.Contains(fp))
                    this.errorFps.Add(fp);
            }

            try { this.workEvent?.Wait(10); } catch { }
            SetStatus(fp, statusResult);
            if (statusResult != null && statusResult.State == 1) // FuelPoint is idle
            {
                if (statusResult.Nozzles == 0)
                {
                    if (fp.QuerySetPrice)
                    {
                        fp.QuerySetPrice = false;
                    }
                    int nozzleForTotals = fp.Nozzles.Where(n => n.QueryTotals).Count();
                    if (nozzleForTotals > 0)
                    {
                        var nozzlesForTotals = fp.Nozzles.Where(n => n.QueryTotals);
                        while (nozzlesForTotals.Count() > 0)
                        {
                            foreach (var nz in nozzlesForTotals)
                            {
                                GetTotals(nz);
                                try { this.workEvent?.Wait(50); } catch { }
                            }
                            nozzlesForTotals = fp.Nozzles.Where(n => n.QueryTotals);
                        }
                    }
                }
            }
            else if (statusResult != null && statusResult.State == 2)
            {
                if (fp.QueryAuthorize)
                {
                    AuthorizeFuelPoint(fp, statusResult);
                    try { this.workEvent?.Wait(50); } catch { }
                }
            }
            else if (statusResult != null && statusResult.State == 3)
            {
                var ack = client.SendRequest((byte)(32 + fp.Address), 'D', "");
                HandleAck(ack);
            }
        }

        #region protocol

        private bool EndOfFueling(FuelPoint f)
        {
            var ack = client.SendRequest((byte)(32 + f.Address), 'C', "2");
            HandleAck(ack);
            return true;
        }

        private void AuthorizeFuelPoint(FuelPoint f, PdeStatus status)
        {
            if (f.ActiveNozzle == null)
                return;
            var product = f.ActiveNozzle.NozzleSocket;

            string payload = (1).ToString() +
                 (999999).ToString("D6") +
                 (0).ToString() +
                 f.ActiveNozzle.UntiPriceInt.ToString("D4") +
                 product.ToString();

            var ack = client.SendRequest((byte)(32 + f.Address), 'A', payload);
            HandleAck(ack);
        }
        private bool Halt(FuelPoint fp)
        {
            return true;
        }

        private void GetTotals(Nozzle nz)
        {
            var payload = nz.Index.ToString("D2");
            var ack = client.SendRequest((byte)(32 + nz.ParentFuelPoint.Address), 'X', payload);
            HandleAck(ack);
        }

        private void SetStatus(FuelPoint fp, PdeStatus status)
        {

            Common.Enumerators.FuelPointStatusEnum newStatus = Common.Enumerators.FuelPointStatusEnum.Offline;
            if (status == null || status.State > 4)
            {
                Logger.Instance.Debug($"Status is null or invalid State= {status?.State}, Nozzle= {status?.Nozzles} ");
                int cm = int.Parse(fp.GetExtendedProperty("StatusMismatch", 0).ToString());
                if (cm < 5)
                {
                    fp.SetExtendedProperty("StatusMismatch", cm + 1);
                    newStatus = fp.Status;

                }
                else
                    newStatus = Common.Enumerators.FuelPointStatusEnum.Offline;
            }
            else if (status.State == 4)
            {
                if (EndOfFueling(fp))
                    newStatus = Common.Enumerators.FuelPointStatusEnum.Idle;
            }
            else
            {
                fp.SetExtendedProperty("StatusMismatch", 0);
                if (status.State == 1)
                {
                    newStatus = Common.Enumerators.FuelPointStatusEnum.Idle;
                }
                else if (status.State == 2)
                {
                    if (status.Nozzles > 0)
                    {
                        fp.ActiveNozzleIndex = status.Nozzles - 1;
                        if (fp.Status == Common.Enumerators.FuelPointStatusEnum.Idle || fp.Status == Common.Enumerators.FuelPointStatusEnum.Offline)
                            newStatus = Common.Enumerators.FuelPointStatusEnum.Nozzle;
                    }
                    else
                        newStatus = Common.Enumerators.FuelPointStatusEnum.Idle;
                }
                else
                {
                    fp.ActiveNozzleIndex = status.Nozzles - 1;
                    newStatus = Common.Enumerators.FuelPointStatusEnum.Work;
                }
            }
            var oldStatus = fp.Status;
            if(newStatus == Common.Enumerators.FuelPointStatusEnum.Offline)
            {
                Logger.Instance.Debug($"OFFLINE: State= {status?.State}, Nozzle= {status?.Nozzles} ");
                return;
            }
            fp.Status = newStatus;
            fp.DispenserStatus = fp.Status;
            if (status != null)
            {
                Logger.Instance.Debug(string.Format("Dispenser (Address: {1}, Channel: {2}) Status: {3}, Keyboard: {4}, Mode: {5}, Nozzle: {6}, State: {7} Communication Port: {0}",
                    this.CommunicationPort, fp.Address, fp.Channel, newStatus, status.Keyboard, status.Mode, status.Nozzles, status.State));
            }
            else
            {
                Logger.Instance.Debug(string.Format("Dispenser (Address: {1}, Channel: {2}) Status: {3}, Communication Port: {0}",
                    this.CommunicationPort, fp.Address, fp.Channel, newStatus));
            }
            Logger.Instance.Debug($"Nozzle: {fp.ActiveNozzleIndex} - From: {oldStatus} -> To: {newStatus}");
            if (oldStatus != newStatus)
            {
                // Thread-safe event invocation: copy to local variable before invoking
                var handler = this.DispenserStatusChanged;
                if (handler != null)
                {
                    Common.FuelPointValues values = new Common.FuelPointValues();
                    if (status != null)
                    {
                        if (fp.Status != Common.Enumerators.FuelPointStatusEnum.Idle && fp.Status != Common.Enumerators.FuelPointStatusEnum.Offline)
                        {
                            fp.ActiveNozzleIndex = status.Nozzles - 1;
                            values.ActiveNozzle = status.Nozzles - 1;
                        }
                        else
                        {
                            fp.ActiveNozzleIndex = -1;
                            values.ActiveNozzle = -1;
                        }
                    }
                    else
                    {
                        fp.ActiveNozzleIndex = -1;
                        values.ActiveNozzle = -1;
                    }
                    values.Status = newStatus;//fp.Status;
                    handler(this, new Common.FuelPointValuesArgs()
                    {
                        CurrentFuelPoint = fp,
                        CurrentNozzleId = values.ActiveNozzle + 1,
                        Values = values
                    });
                }
            }
        }

        #endregion
    }
}
