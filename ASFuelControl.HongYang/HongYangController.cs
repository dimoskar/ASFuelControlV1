using ASFuelControl.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO.Pipes;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASFuelControl.HongYang
{
    public class HongYangController : Common.IFuelProtocol
    {
        private DispenserClient client;
        private SerialTransport transport;
        public event EventHandler<Common.FuelPointValuesArgs> DataChanged;
        public event EventHandler<Common.TotalsEventArgs> TotalsRecieved;
        public event EventHandler<Common.SaleEventArgs> SaleRecieved;
        public event EventHandler<Common.FuelPointValuesArgs> DispenserStatusChanged;
        public event EventHandler DispenserOffline;

        private List<Common.FuelPoint> fuelPoints = new List<Common.FuelPoint>();

        private System.IO.Ports.SerialPort serialPort = new System.IO.Ports.SerialPort();
        private System.Threading.Thread th;

        public Common.DebugValues foo = new Common.DebugValues();
        public DispenserClient Client 
        { 
            get { return this.client; } 
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
                return this.client.IsConnected();
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
                transport = new SerialTransport(this.CommunicationPort);
                client = new DispenserClient(transport);
                transport.Open();

                this.th = new System.Threading.Thread(new System.Threading.ThreadStart(this.ThreadRun));
                th.Start();
            }
            catch (Exception ex)
            {
                Common.Logger.Instance.Error("Connection Failed" + ex.Message);
            }
        }
        public void Disconnect()
        {
            try
            {
                transport.Close();
            }
            catch (Exception ex)
            {
                Common.Logger.Instance.Error("Disconnection Failed" + ex.Message);
            }
            if (th != null && th.IsAlive)
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
            foo = new DebugValues();
            //string errMsg = "";
            //if (client.IsDispenserOnline((byte)(fp.Address), out errMsg))
            //    foo.Status = Common.Enumerators.FuelPointStatusEnum.Offline;
            //else {
            //    Enums.PumpStateFlags status = Enums.PumpStateFlags.AuthorizationMode;
            //    string diagnostic = "";
            //    var success = client.GetStatus((byte)(fp.Address), out status, out diagnostic);
            //    if (!success)
            //        return null;
            //    if (status.HasFlag(Enums.PumpStateFlags.Fuelling))
            //        foo.Status = Common.Enumerators.FuelPointStatusEnum.Work;
            //    else if (status.HasFlag(Enums.PumpStateFlags.NozzleWaiting))
            //        foo.Status = Common.Enumerators.FuelPointStatusEnum.Nozzle;
            //    else
            //        foo.Status = Common.Enumerators.FuelPointStatusEnum.Idle;
            //}
            return foo;
        }

        private void ThreadRun()
        {
            Logger.Instance.Info("HongYang Protocol Thread Started. Communication Port: " + this.CommunicationPort);
            Logger.Instance.Info($"Initializing Dispensers {this.fuelPoints.Count}...");
            var errorFps = new List<FuelPoint>();
            foreach (var fp in this.FuelPoints)
            {
                try
                {
                    string diagnostic = "";
                    var initSuccess = client.GetStatus((byte)fp.Address) != Common.Enumerators.FuelPointStatusEnum.Offline;
                    if (initSuccess)
                    {
                        diagnostic = "";
                        var clearDisplay = client.ClearDisplay((byte)fp.Address);
                        if (clearDisplay == Common.Enumerators.FuelPointStatusEnum.Idle)
                        {
                            fp.Initialized = true;
                            fp.QuerySetPrice = true;
                        }
                        else
                        {
                            Logger.Instance.Error(string.Format("Dispenser Clear Display Failed (Address {1}, Channel {2}). Communication Port: {0}. Diagnostic: {3}", this.CommunicationPort, fp.Address, fp.Channel, diagnostic));
                            errorFps.Add(fp);
                        }
                    }
                    else
                    {
                        Logger.Instance.Error(string.Format("Dispenser Init Failed (Address {1}, Channel {2}). Communication Port: {0}. Diagnostic: {3}", this.CommunicationPort, fp.Address, fp.Channel, diagnostic));
                        errorFps.Add(fp);
                    }
                }
                catch (Exception ex)
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
                            int volume = 0;
                            int amount = 0;
                            string diagnostic = "";
                            var status = client.GetTotals((byte)fp.Address, ref volume, ref amount);
                            if (status != Common.Enumerators.FuelPointStatusEnum.Offline)
                            {
                                nz.TotalVolume = volume;
                                nz.TotalPrice = amount;
                                nz.QueryTotals = false;
                                if (this.TotalsRecieved != null)
                                {
                                    this.TotalsRecieved(this, new Common.TotalsEventArgs(fp, nz.Index, nz.TotalVolume, nz.TotalPrice));
                                }
                            }
                            else
                            {
                                Logger.Instance.Error(string.Format("Dispenser Get Totals Failed (Address {1}, Channel {2}). Communication Port: {0}. Diagnostic: {3}", this.CommunicationPort, fp.Address, fp.Channel, diagnostic));
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
                        Enums.PumpStateFlags statusResult;
                        string diagnostic = "";
                        var status = client.GetStatus((byte)fp.Address);
                        SetStatus(fp, status);
                        if ((status != Common.Enumerators.FuelPointStatusEnum.Offline))
                        {
                            System.Threading.Thread.Sleep(50);
                            bool hasError = false;

                            System.Threading.Thread.Sleep(10);

                            if (!hasError)
                            {
                                if (status == Common.Enumerators.FuelPointStatusEnum.Work)
                                {
                                    int volume = 0;
                                    int amount = 0;
                                    diagnostic = "";
                                    var dispStatus = client.GetDisplay((byte)fp.Address, ref amount, ref volume);
                                    if (dispStatus != Common.Enumerators.FuelPointStatusEnum.Offline)
                                    {
                                        fp.DispensedAmount = amount / (decimal)System.Math.Pow(10, fp.AmountDecimalPlaces);
                                        fp.DispensedVolume = volume / (decimal)System.Math.Pow(10, fp.VolumeDecimalPlaces);
                                    }
                                }
                                else
                                {
                                    diagnostic = "";
                                    if (status == Common.Enumerators.FuelPointStatusEnum.Nozzle)
                                    {
                                        if (fp.QueryAuthorize)
                                        {
                                            if(client.AuthorizeDispenser((byte)fp.Address) == Common.Enumerators.FuelPointStatusEnum.Work)
                                            {
                                                fp.QueryAuthorize = false;
                                                System.Threading.Thread.Sleep(50);
                                            }
                                            else
                                            {
                                                Logger.Instance.Error(string.Format("Dispenser Authorize Failed (Address {1}, Channel {2}). Communication Port: {0}. Diagnostic: {3}", this.CommunicationPort, fp.Address, fp.Channel, diagnostic));
                                            }
                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        if (fp.QueryAuthorize)
                                        {
                                            fp.QueryAuthorize = false;
                                            continue;
                                        }
                                    }
                                }
                                
                                if (status == Common.Enumerators.FuelPointStatusEnum.Idle)
                                {
                                    if (fp.QuerySetPrice)
                                    {
                                        foreach (var nz in fp.Nozzles)
                                        {
                                            diagnostic = "";
                                            if(client.ChangePrice((byte)fp.Address, nz.UntiPriceInt) != Common.Enumerators.FuelPointStatusEnum.Offline)
                                            {
                                                fp.QuerySetPrice = false;
                                                System.Threading.Thread.Sleep(50);
                                            }
                                            else
                                            {
                                                Logger.Instance.Error(string.Format("Dispenser Set Price Failed (Address {1}, Channel {2}). Communication Port: {0}. Diagnostic: {3}", this.CommunicationPort, fp.Address, fp.Channel, diagnostic));
                                            }
                                        }   
                                    }
                                    int nozzleForTotals = fp.Nozzles.Where(n => n.QueryTotals).Count();
                                    if (nozzleForTotals > 0)
                                    {
                                        //var isOnSale = fp.GetExtendedProperty("IsOnSale");
                                        //if (isOnSale != null && (bool)isOnSale)
                                        //{
                                        //    System.Threading.Thread.Sleep(5000);
                                        //    fp.SetExtendedProperty("IsOnSale", false);
                                        //}
                                        foreach (Common.Nozzle nz in fp.Nozzles)
                                        {
                                            if (nz.QueryTotals)
                                            {
                                                var isOnSale = (bool)fp.GetExtendedProperty("Work", false);
                                                if (isOnSale && nz.GetTotalsIndex() == 0)
                                                {
                                                    System.Threading.Thread.Sleep(2000);
                                                }
                                                if (this.GetTotals(nz))
                                                {
                                                    //if (this.TotalsRecieved != null)
                                                    //{
                                                    //    this.TotalsRecieved(this, new Common.TotalsEventArgs(fp, nz.Index, nz.TotalVolume, nz.TotalPrice));
                                                    //}
                                                    nz.QueryTotals = false;
                                                }
                                                System.Threading.Thread.Sleep(50);
                                            }
                                        }
                                        continue;
                                    }

                                }
                                if(status == Common.Enumerators.FuelPointStatusEnum.Work)
                                {
                                    //fp.SetExtendedProperty("IsOnSale", true);
                                }
                            }
                        }
                        else
                        {
                            Logger.Instance.Error(string.Format("Dispenser Get Status Failed (Address {1}, Channel {2}). Communication Port: {0}. Diagnostic: {3}", this.CommunicationPort, fp.Address, fp.Channel, diagnostic));
                        }

                    }
                    catch (Exception ex)
                    {
                        Common.Logger.Instance.Error(ex.Message);
                        Common.Logger.Instance.Error(ex.StackTrace);
                        System.Threading.Thread.Sleep(50);
                    }
                }
            }
        }

        #region protocol

        private bool AuthorizeFuelPoint(FuelPoint f)
        {
            if (f.ActiveNozzle == null)
            {
                f.ActiveNozzleIndex = 0;
            }
            string diagnostic = "";
            if(client.AuthorizeDispenser((byte)f.Address) == Common.Enumerators.FuelPointStatusEnum.Offline)
            {
                Logger.Instance.Error(string.Format("Dispenser Authorize Failed (Address {1}, Channel {2}). Communication Port: {0}. Diagnostic: {3}", this.CommunicationPort, f.Address, f.Channel, diagnostic));
                return false;
            }
            return true;
        }
        private bool Halt(FuelPoint fp)
        {
            if (fp.ActiveNozzle == null)
                return true;
            string diagnostic = "";
            if (client.Stop((byte)fp.Address) != Common.Enumerators.FuelPointStatusEnum.Work)
            {
                return true;
            }
            Logger.Instance.Error(string.Format("Dispenser Halt Failed (Address {1}, Channel {2}). Communication Port: {0}. Diagnostic: {3}", this.CommunicationPort, fp.Address, fp.Channel, diagnostic));
            return false;
        }

        private bool GetTotals(Nozzle nz)
        {
            int volume = 0;
            int amount = 0;
            string diagnostic = "";
            if (client.GetTotals((byte)nz.ParentFuelPoint.Address, ref volume, ref amount) != Common.Enumerators.FuelPointStatusEnum.Offline)
            {
                nz.TotalPrice = amount;
                nz.TotalVolume = volume;
                if (this.TotalsRecieved != null)
                    this.TotalsRecieved(this, new TotalsEventArgs(nz.ParentFuelPoint, nz.NozzleIndex, volume, amount));
                return true;
            }
            else
            {
                Logger.Instance.Error(string.Format("Dispenser Get Totals Failed (Address {1}, Channel {2}). Communication Port: {0}. Diagnostic: {3}", this.CommunicationPort, nz.ParentFuelPoint.Address, nz.ParentFuelPoint.Channel, diagnostic));
                return false;
            }
        }

        private void SetStatus(FuelPoint fp, Common.Enumerators.FuelPointStatusEnum newStatus)
        {
            var oldStatus = fp.Status;
            fp.Status = newStatus;
            if (newStatus == Common.Enumerators.FuelPointStatusEnum.Idle)
                fp.ActiveNozzleIndex = -1;
            else
                fp.ActiveNozzleIndex = 0;

            fp.DispenserStatus = fp.Status;
            if (this.DispenserStatusChanged != null)
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
                if (fp.Status != oldStatus)
                {
                    Logger.Instance.Debug(string.Format("Dispenser Status Changed (Address: {1}, Channel: {2}) Old Status: {3}, New Status: {4}, Communication Port: {0}",
                    this.CommunicationPort, fp.Address, fp.Channel, oldStatus, newStatus));
                    this.DispenserStatusChanged(this, new Common.FuelPointValuesArgs()
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
