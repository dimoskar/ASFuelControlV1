using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASFuelControl.Common;

namespace ASFuelControl.Tatsuno
{
    public class TasunoController : Common.IFuelProtocol
    {
        private TatsunoPdeClient client;

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
                return this.client.IsOpen();
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
                if (client != null)
                    client.Dispose();
                //SerialPortTransport transport = new SerialPortTransport(this.CommunicationPort);
                this.serialPort = new SerialPort(this.CommunicationPort, 9600, Parity.Even, 7, StopBits.Two);
                this.serialPort.Handshake = Handshake.RequestToSend;
                client = new TatsunoPdeClient(this.serialPort, new PdeOptions() { });

                this.serialPort.Open();
                this.th = new System.Threading.Thread(new System.Threading.ThreadStart(this.ThreadRun));
                th.Start();
            }
            catch(Exception ex)
            {
                Common.Logger.Instance.Error("Connection Failed" + ex.Message);
            }
        }
        public void Disconnect()
        {
            if (this.serialPort.IsOpen)
                this.serialPort.Close();
            if(th != null && th.IsAlive)
                th.Abort();
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
            var status = client.RequestStatus((byte)(32 + fp.Address));
            if(status == null)
                return null;
            if(status.State < 4)
            {
                if (status.State == 2 || status.State == 3)
                    foo.Status = Common.Enumerators.FuelPointStatusEnum.Work;
                else if (status.State == 1)
                    foo.Status = Common.Enumerators.FuelPointStatusEnum.Nozzle;
                else
                    foo.Status = Common.Enumerators.FuelPointStatusEnum.Idle;
            }
            else
                foo.Status = Common.Enumerators.FuelPointStatusEnum.Offline;
            return foo;
        }

        private void ThreadRun()
        {
            var errorFps = new List<FuelPoint>();
            foreach(var fp in this.FuelPoints)
            {
                try
                {
                    var initResult = client.Initialize((byte)(32 + fp.Address));
                    if (!initResult)
                    {
                        Logger.Instance.Error(string.Format("Dispenser (Address {1}, Channel {2}) has failed to initiliaze. Communication Port: {0}", this.CommunicationPort, fp.Address, fp.Channel));
                        errorFps.Add(fp);
                    }
                    else
                    {
                        client.SendClearDisplay((byte)(fp.Address + 32));
                        fp.Initialized = true;
                        fp.QuerySetPrice = true;
                    }
                }
                catch(Exception ex)
                {
                    Logger.Instance.Error(string.Format("Dispenser Init Exception (Address {1}, Channel {2}) has failed to initiliaze. Communication Port: {0}", this.CommunicationPort, fp.Address, fp.Channel));
                    Logger.Instance.Error(ex.Message);
                    Logger.Instance.Error(ex.StackTrace);
                }
            }
            foreach (Common.FuelPoint fp in this.fuelPoints)
            {
                if (errorFps.Contains(fp))
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
                        foreach (var nz in fp.Nozzles)
                        {
                            if (!nz.QueryTotals)
                            {
                                System.Threading.Thread.Sleep(50);
                                continue;
                            }
                            var totalResult = client.RequestTotalizers((byte)(32 + fp.Address), nz.Index);
                            if (totalResult == null)
                            {
                                Logger.Instance.Debug(string.Format("Totals Result is null"));
                                System.Threading.Thread.Sleep(50);
                                continue;
                            }
                            if (totalResult.NozzleIndex != nz.Index)
                            {
                                Logger.Instance.Debug(string.Format("Totals Recieved Index mismatch {0}, {1}", nz.Index, totalResult.NozzleIndex));

                                System.Threading.Thread.Sleep(50);
                                continue;
                            }
                            nz.TotalVolume = totalResult.Volume;
                            nz.TotalPrice = 0;
                            nz.QueryTotals = false;
                            if (this.TotalsRecieved != null)
                            {
                                this.TotalsRecieved(this, new Common.TotalsEventArgs(fp, nz.Index, nz.TotalVolume, nz.TotalPrice));
                            }
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
                        System.Threading.Thread.Sleep(50);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error(string.Format("Dispenser Init Exception (Address {1}, Channel {2}) has failed to initiliaze. Communication Port: {0}", this.CommunicationPort, fp.Address, fp.Channel));
                    Logger.Instance.Error(ex.Message);
                    Logger.Instance.Error(ex.StackTrace);
                }                
            }
            while (this.IsConnected)
            {
                foreach (var fp in this.fuelPoints)
                {
                    System.Threading.Thread.Sleep(50);
                    try
                    {
                        if (errorFps.Contains(fp))
                            continue;

                        if (fp.QueryHalt)
                        {
                            fp.QueryHalt = !this.Halt(fp);
                            continue;
                        }

                        var statusResult = client.RequestStatus((byte)(32 + fp.Address));
                        System.Threading.Thread.Sleep(50);
                        bool hasError = false;
                        if (statusResult == null)
                        {
                            Logger.Instance.Error(string.Format("Dispenser (Address {1}, Channel {2}) has failed to send status. Communication Port: {0}", this.CommunicationPort, fp.Address, fp.Channel));
                            hasError = true;
                        }
                        else if (statusResult.State >= 4)
                        {
                            if (statusResult.Error != null)
                            {
                                Logger.Instance.Error(string.Format("Dispenser (Address {1}, Channel {2}) has failed to send status. Error Code : {3}. Communication Port: {0}",
                                    this.CommunicationPort, fp.Address, fp.Channel, statusResult.Error.Code));
                            }
                            else
                                Logger.Instance.Error(string.Format("Dispenser (Address {1}, Channel {2}) is out of service. Communication Port: {0}", this.CommunicationPort, fp.Address, fp.Channel));
                            hasError = true;
                        }
                        System.Threading.Thread.Sleep(10);
                        SetStatus(fp, statusResult);
                        
                        if (!hasError)
                        {
                            if (statusResult.State == 1) // FuelPoint is idle
                            {
                                if (fp.QueryAuthorize)
                                {
                                    if (AuthorizeFuelPoint(fp, statusResult))
                                    {
                                        fp.QueryAuthorize = false;
                                        System.Threading.Thread.Sleep(50);
                                    }
                                    continue;
                                }
                                if (statusResult.Nozzles == 0)
                                {
                                    if (fp.QuerySetPrice)
                                    {
                                        foreach (var nz in fp.Nozzles)
                                        {
                                            client.SetUnitPrice((byte)(32 + fp.Address), 1, 1000);
                                            System.Threading.Thread.Sleep(50);
                                            client.SetUnitPrice((byte)(32 + fp.Address), 2, 2000);
                                            System.Threading.Thread.Sleep(50);
                                            client.SetUnitPrice((byte)(32 + fp.Address), 3, 3000);
                                            System.Threading.Thread.Sleep(50);
                                            client.SetUnitPrice((byte)(32 + fp.Address), 4, 4000);
                                            System.Threading.Thread.Sleep(50);
                                            client.SetUnitPrice((byte)(32 + fp.Address), 5, 5000);
                                            System.Threading.Thread.Sleep(50);
                                            client.SetUnitPrice((byte)(32 + fp.Address), 6, 6000);
                                            System.Threading.Thread.Sleep(50);
                                            client.SetUnitPrice((byte)(32 + fp.Address), 7, 7000);
                                            System.Threading.Thread.Sleep(50);
                                            client.SetUnitPrice((byte)(32 + fp.Address), 8, 8000);
                                        }
                                        if (fp.Nozzles.Where(n => n.QuerySetPrice).Count() == 0)
                                            fp.QuerySetPrice = false;
                                    }
                                    int nozzleForTotals = fp.Nozzles.Where(n => n.QueryTotals).Count();
                                    if (nozzleForTotals > 0)
                                    {
                                        foreach (Common.Nozzle nz in fp.Nozzles)
                                        {
                                            if (nz.QueryTotals)
                                            {
                                                if (this.GetTotals(nz))
                                                {
                                                    if (this.TotalsRecieved != null)
                                                    {
                                                        this.TotalsRecieved(this, new Common.TotalsEventArgs(fp, nz.Index, nz.TotalVolume, nz.TotalPrice));
                                                    }
                                                    nz.QueryTotals = false;
                                                }
                                                System.Threading.Thread.Sleep(50);
                                            }
                                        }
                                        continue;
                                    }
                                    
                                }
                            }

                            else if(statusResult.State > 1)
                            {
                                var display = client.RequestDisplay((byte)(32 + fp.Address));
                                fp.DispensedAmount = display.Amount / (decimal)System.Math.Pow(10, fp.AmountDecimalPlaces);
                                fp.DispensedVolume = display.Volume / (decimal)System.Math.Pow(10, fp.VolumeDecimalPlaces);
                            }
                        }
                        //SetStatus(fp, statusResult);
                    }
                    catch(Exception ex)
                    {
                        Common.Logger.Instance.Error(ex.Message);
                        Common.Logger.Instance.Error(ex.StackTrace);
                        System.Threading.Thread.Sleep(50);
                    }
                }
            }
        }

        #region protocol

        private bool AuthorizeFuelPoint(FuelPoint f, PdeStatus status)
        {
            if (f.ActiveNozzle == null)
                return true;
            var authResult = client.Authorize((byte)(32 + f.Address), 1, 0, 0, 0, f.ActiveNozzleIndex + 1);
            Logger.Instance.Debug(string.Format("Dispenser (Address {1}, Channel {2}) Authorize Result: {3}. Communication Port: {0}", this.CommunicationPort, f.Address, f.Channel, authResult));
            return authResult;
        }
        private bool Halt(FuelPoint fp)
        {
            return true;
        }

        private bool GetTotals(Nozzle nz)
        {
            var registers = client.RequestTotalizers((byte)(32 + nz.ParentFuelPoint.Address), nz.Index);
            Logger.Instance.Debug(string.Format("Dispenser (Address: {1}, Channel: {2}, Nozzle: {4}) Volume: {3}. Communication Port: {0}",
                this.CommunicationPort, nz.ParentFuelPoint.Address, nz.ParentFuelPoint.Channel, registers.Volume, nz.Index));
            if (nz.Index != registers.NozzleIndex)
                return false;
            nz.TotalPrice = 0;// (decimal)registers / (decimal)System.Math.Pow(10, nz.ParentFuelPoint.AmountDecimalPlaces);
            nz.TotalVolume = (decimal)registers.Volume; // / (decimal)System.Math.Pow(10, nz.ParentFuelPoint.VolumeDecimalPlaces);
            return true;
        }

        private void SetStatus(FuelPoint fp, PdeStatus status)
        {

            Common.Enumerators.FuelPointStatusEnum newStatus = Common.Enumerators.FuelPointStatusEnum.Offline;  
            if(status == null || status.State >= 4)
            {
                int cm = int.Parse(fp.GetExtendedProperty("StatusMismatch", 0).ToString());
                if (cm < 5)
                {
                    fp.SetExtendedProperty("StatusMismatch", cm + 1);
                    newStatus = fp.Status;

                }
                else
                    newStatus = Common.Enumerators.FuelPointStatusEnum.Offline;
            }
            else
            {
                fp.SetExtendedProperty("StatusMismatch", 0);
                if (status.State == 1)
                {
                    if (status.Nozzles > 0)
                    {
                        fp.ActiveNozzleIndex = status.Nozzles - 1;
                        if (fp.Status == Common.Enumerators.FuelPointStatusEnum.Idle)
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
            if (this.DispenserStatusChanged != null)
            {
                Logger.Instance.Debug(string.Format("Dispenser Status Changed (Address: {1}, Channel: {2}) Old Status: {3}, New Status: {4}, Communication Port: {0}",
                    this.CommunicationPort, fp.Address, fp.Channel, oldStatus, newStatus));
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
                values.Status = fp.Status;
                this.DispenserStatusChanged(this, new Common.FuelPointValuesArgs()
                {
                    CurrentFuelPoint = fp,
                    CurrentNozzleId = values.ActiveNozzle + 1,
                    Values = values
                });
            }

        }

        #endregion
    }
}
