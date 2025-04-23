using ASFuelControl.Common.Enumerators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.Millenium
{
    public class MilleniumProtocol : Common.IFuelProtocol
    {
        public event EventHandler<Common.TotalsEventArgs> TotalsRecieved;
        public event EventHandler<Common.FuelPointValuesArgs> DataChanged;
        public event EventHandler<Common.FuelPointValuesArgs> DispenserStatusChanged;

        private DispenserCommands commandHelper = new DispenserCommands();
        private System.Threading.Thread th;
        private List<Common.FuelPoint> fuelPoints = new List<Common.FuelPoint>();
        private System.IO.Ports.SerialPort serialPort = new System.IO.Ports.SerialPort();

        public Common.FuelPoint[] FuelPoints
        {
            set { this.fuelPoints = new List<Common.FuelPoint>(value); }
            get { return this.fuelPoints.ToArray(); }
        }

        public bool IsConnected
        {
            get { return this.serialPort.IsOpen; }
        }

        public string CommunicationPort { set; get; }

        public void Connect()
        {
            try
            {
                this.serialPort.PortName = this.CommunicationPort;
                this.serialPort.Open();
                this.th = new System.Threading.Thread(new System.Threading.ThreadStart(this.ThreadRun));
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
            fp.PropertyChanged -= new System.ComponentModel.PropertyChangedEventHandler(fp_PropertyChanged);
            fp.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(fp_PropertyChanged);
        }

        public void ClearFuelPoints()
        {
            this.fuelPoints.Clear();
        }

        private void ThreadRun()
        {
            foreach (Common.FuelPoint fp in this.fuelPoints)
            {
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
                            this.serialPort.DiscardInBuffer();
                            this.serialPort.DiscardOutBuffer();
                            if (!fp.Initialized && fp.Status != Common.Enumerators.FuelPointStatusEnum.Close)
                            {
                                this.UnlockFuelPoint(fp);
                                fp.SetExtendedProperty("Status", FuelPointStatusEnum.Idle);
                                this.GetDisplayData(fp);
                                continue;
                            }
                            else if (fp.Status == Common.Enumerators.FuelPointStatusEnum.Close)
                            {
                                this.OpenFuelPoint(fp);
                                continue;
                            }
                            if (fp.QuerySetPrice)
                            {
                                this.SetPrices(fp);
                                System.Threading.Thread.Sleep(50);
                                continue;
                            }
                            int nozzleForTotals = fp.Nozzles.Where(n => n.QueryTotals).Count();
                            if(nozzleForTotals > 0)
                            {
                                this.UnlockFuelPoint(fp);
                                foreach (Common.Nozzle nz in fp.Nozzles)
                                {
                                    if (nz.QueryTotals)
                                    {
                                        this.GetTotals(nz);
                                        System.Threading.Thread.Sleep(50);
                                    }

                                }
                                this.UnlockFuelPoint(fp);
                                continue;
                            }
                                
                            if (fp.DispenserStatus == Common.Enumerators.FuelPointStatusEnum.Work)
                                fp.SetExtendedProperty("LastIdGot", false);
                            
                            if (fp.QueryAuthorize)
                            {
                                this.AuthorizeFuelPoint(fp);
                                continue;
                            }

                            bool lastIdGot = (bool)fp.GetExtendedProperty("LastIdGot", false);
                            if (!lastIdGot && fp.Status == Common.Enumerators.FuelPointStatusEnum.Idle)
                            {
                                this.UnlockFuelPoint(fp);
                            }

                            this.GetDisplayData(fp);
                        }
                        finally
                        {
                            byte[] cmd = this.commandHelper.CreateCmd(fp.Address);
                            this.ExecuteCommnad(cmd, fp);
                        }
                        System.Threading.Thread.Sleep(20);
                    }
                }
                catch
                {
                    System.Threading.Thread.Sleep(250);
                }
            }
        }

        private void ExecuteCommnad(byte[] command, Common.FuelPoint fp)
        {
            try
            {
                DateTime dt = DateTime.Now;
                this.serialPort.DiscardInBuffer();
                this.serialPort.DiscardOutBuffer();

                this.serialPort.Write(command, 0, command.Length);
                System.Threading.Thread.Sleep(20);


                List<byte> response = new List<byte>();

                System.Threading.Thread.Sleep(150);
                if (this.serialPort.BytesToRead == 0)
                    return;
                byte[] resp = new byte[this.serialPort.BytesToRead];
                this.serialPort.Read(resp, 0, resp.Length);
                response.AddRange(resp);
                if (response.Count <= 1)
                    return;
                List<MilleniumProtocol.OldFpStatus> OldFpStatus = new List<MilleniumProtocol.OldFpStatus>();

                //Dictionary<int, Millenium.Enums.FuelPointStatusEnum> oldStatuses = new Dictionary<int, Enums.FuelPointStatusEnum>();
                foreach (Common.FuelPoint fc in this.fuelPoints)
                {
                    MilleniumProtocol.OldFpStatus oldFpStatus = new MilleniumProtocol.OldFpStatus();
                    oldFpStatus.FPAddress = fc.Address;
                    oldFpStatus.FPchannel = fc.Channel;
                    oldFpStatus.status = (Common.Enumerators.FuelPointStatusEnum)fc.GetExtendedProperty("Status", Common.Enumerators.FuelPointStatusEnum.Idle);
                    OldFpStatus.Add(oldFpStatus);
                }

                //Millenium.Enums.FuelPointStatusEnum oldStatus = (Millenium.Enums.FuelPointStatusEnum)fp.GetExtendedProperty("Status", Millenium.Enums.FuelPointStatusEnum.Idle);
                List<MilleniumNozzle> nozzles = this.commandHelper.EvaluateBuffer(response.ToArray(), OldFpStatus);
                foreach (MilleniumNozzle nz in nozzles)
                {
                    

                    Common.FuelPoint fuelPoint = this.fuelPoints.Where(f => f.Address == nz.DispenserID && f.Channel == nz.FuelPointChannel).FirstOrDefault();
                    if (fuelPoint == null)
                        continue;
                    if (nz.isOpen && !fuelPoint.Initialized)
                        fuelPoint.Initialized = true;
                    

                    Common.Enumerators.FuelPointStatusEnum newStatus = Common.Enumerators.FuelPointStatusEnum.Idle;
                    if (nz.Status == FuelPointStatusEnum.Auth)
                        newStatus = Common.Enumerators.FuelPointStatusEnum.Work;
                    else
                        newStatus = (Common.Enumerators.FuelPointStatusEnum)((int)nz.Status);
                    fuelPoint.SetExtendedProperty("Status", nz.Status);
                    bool statusChanged = false;
                    if(fuelPoint.DispenserStatus != newStatus)
                        statusChanged = true;

                    if(fuelPoint.LastStatus != Common.Enumerators.FuelPointStatusEnum.Work && newStatus== Common.Enumerators.FuelPointStatusEnum.TransactionCompleted)
                    {
                        newStatus = Common.Enumerators.FuelPointStatusEnum.Idle;
                    }
                    //if(nz.FuelPointChannel == 2)
                    //{  
                    //    Console.WriteLine("--------------------------------------------  " +nz.isNozzle + "===" + nz.isIdle);
                    //}
                    
                    //if (newStatus == Common.Enumerators.FuelPointStatusEnum.TransactionCompleted )
                    //    newStatus = Common.Enumerators.FuelPointStatusEnum.Idle;

                    fuelPoint.DispenserStatus = newStatus;
                    fuelPoint.Status = newStatus;

                    if(newStatus == Common.Enumerators.FuelPointStatusEnum.TransactionCompleted)
                    {
                        fuelPoint.DispensedAmount = nz.getDisplayPrice(fuelPoint.DecimalPlaces);
                        fuelPoint.DispensedVolume = nz.getDisplayLitres(fuelPoint.DecimalPlaces);
                    //    if(this.DataChanged != null)
                    //    {
                    //        Common.FuelPointValues values = new Common.FuelPointValues();
                    //        values.CurrentSalePrice = fuelPoint.Nozzles[0].UnitPrice;
                    //        values.CurrentPriceTotal = nz.getDisplayPrice(fuelPoint.DecimalPlaces);
                    //        values.CurrentVolume = nz.getDisplayLitres(fuelPoint.DecimalPlaces);
                    //        Console.WriteLine("DispensedVolume : {0}, DispensedAmount : {1}", fuelPoint.DispensedVolume, fuelPoint.DispensedAmount);

                    //        this.DataChanged(this, new Common.FuelPointValuesArgs()
                    //        {
                    //            CurrentFuelPoint = fuelPoint,
                    //            CurrentNozzleId = 1,
                    //            Values = values
                    //        });
                    //    }
                    }
                    if (nz.LastSaleID > 0)
                    {
                        fuelPoint.SetExtendedProperty("LastSalesId", nz.LastSaleID);
                    }

                    if (fuelPoint.Status == FuelPointStatusEnum.Idle)
                        fuelPoint.ActiveNozzle = null;
                    else
                        fuelPoint.ActiveNozzle = fuelPoint.Nozzles[0];

                    if (statusChanged && this.DispenserStatusChanged != null)
                    {
                        Common.FuelPointValues values = new Common.FuelPointValues();
                        values.Status = fuelPoint.Status;
                        this.DispenserStatusChanged(this, new Common.FuelPointValuesArgs() { CurrentFuelPoint = fuelPoint, CurrentNozzleId = 1, Values = values });
                    }
                    if (nz.isWorking)
                    {
                        fuelPoint.DispensedAmount = nz.getDisplayPrice(fuelPoint.DecimalPlaces) ;
                        fuelPoint.DispensedVolume = nz.getDisplayLitres(fuelPoint.DecimalPlaces);
                        if (this.DataChanged != null)
                        {
                            Common.FuelPointValues values = new Common.FuelPointValues();
                            values.CurrentSalePrice = fuelPoint.Nozzles[0].UnitPrice;
                            values.CurrentPriceTotal = nz.getDisplayPrice(fuelPoint.DecimalPlaces);
                            values.CurrentVolume = nz.getDisplayLitres(fuelPoint.DecimalPlaces);
                            Console.WriteLine("DispensedVolume : {0}, DispensedAmount : {1}", fuelPoint.DispensedVolume, fuelPoint.DispensedAmount);

                            this.DataChanged(this, new Common.FuelPointValuesArgs() { CurrentFuelPoint = fuelPoint, CurrentNozzleId = 1, Values = values });
                        }
                    }
                    if (nz.LastResponseType == Enums.ResponseTypeEnum.Totals)
                    {
                        fuelPoint.QueryTotals = false;
                        fuelPoint.Nozzles[0].QueryTotals = false;
                        fuelPoint.SetExtendedProperty("TotalsInitialized", true);
                        if (fuelPoint.Nozzles[0].TotalVolume != nz.TotalLitres)
                        {
                            fuelPoint.Nozzles[0].TotalVolume = nz.TotalLitres;
                            if (this.TotalsRecieved != null)
                                this.TotalsRecieved(this, new Common.TotalsEventArgs(fuelPoint, 1, nz.TotalLitres, nz.TotalLitres));
                        }
                    }
                    if (nz.isPriceSet)
                    {
                        fuelPoint.QuerySetPrice = false;
                        fuelPoint.Nozzles[0].QuerySetPrice = false;
                    }
                    if (nz.isAuthorized)
                    {
                        fuelPoint.QueryAuthorize = false;
                    }
                }
            }
            catch
            {
                return;
            }
        }

        private void OpenFuelPoint(Common.FuelPoint fp)
        {
            try
            {
                byte[] cmd = commandHelper.CreateCmd(fp.Address, fp.Channel, Enums.CmdEnum.OpenNozzle);
                this.serialPort.Write(cmd, 0, cmd.Length);
                //this.ExecuteCommnad(cmd, fp);
            }
            catch
            {
                
            }
        }

        private void SetPrices(Common.FuelPoint fp)
        {
            try
            {
                byte[] cmd = commandHelper.CreateCmd(fp.Address, fp.Channel, Enums.CmdEnum.SendPrices, fp.Nozzles[0].UntiPriceInt);
                this.serialPort.Write(cmd, 0, cmd.Length);
                //this.ExecuteCommnad(cmd, fp);
            }
            catch
            {
            }
        }

        private void GetTotals(Common.Nozzle nz)
        {
            try
            {
                byte[] cmd = commandHelper.CreateCmd(nz.ParentFuelPoint.Address, nz.ParentFuelPoint.Channel, Enums.CmdEnum.RequestTotals);
                this.serialPort.Write(cmd, 0, cmd.Length);
                //this.ExecuteCommnad(cmd, nz.ParentFuelPoint);
            }
            catch
            {
            }
        }

        private void AuthorizeFuelPoint(Common.FuelPoint fp)
        {
            try
            {
                byte[] cmd = commandHelper.CreateCmd(fp.Address, fp.Channel, Enums.CmdEnum.Authorize);
                this.serialPort.Write(cmd, 0, cmd.Length);
                //this.ExecuteCommnad(cmd, fp);
            }
            catch
            {
            }
        }

        private void GetDisplayData(Common.FuelPoint fp)
        {
            try
            {
                byte[] cmd = commandHelper.CreateCmd(fp.Address, fp.Channel, Enums.CmdEnum.RequestDisplayData);
                this.serialPort.Write(cmd, 0, cmd.Length);
                //this.ExecuteCommnad(cmd, fp);
            }
            catch
            {
            }
        }

        private void GetLastId(Common.FuelPoint fp)
        {
            try
            {
                byte[] cmd = commandHelper.CreateCmd(fp.Address, fp.Channel, Enums.CmdEnum.GetLastSalesId);
                this.serialPort.Write(cmd, 0, cmd.Length);
                //this.ExecuteCommnad(cmd, fp);
            }
            catch
            {
            }
        }

        private void GetLastInfo(Common.FuelPoint fp)
        {
            try
            {
                byte[] cmd = commandHelper.CreateCmd(fp.Address, fp.Channel, Enums.CmdEnum.InforForId, (int)fp.GetExtendedProperty("LastSalesId", 0));
                this.serialPort.Write(cmd, 0, cmd.Length);
                //this.ExecuteCommnad(cmd, fp);
            }
            catch
            {
            }
        }

        private void GetDispenserStatus(Common.FuelPoint fp)
        {
            try
            {
                byte[] cmd = commandHelper.CreateCmd(fp.Address, fp.Channel, Enums.CmdEnum.RequestStatus);
                this.serialPort.Write(cmd, 0, cmd.Length);
                //this.ExecuteCommnad(cmd, fp);
            }
            catch
            {
            }
        }

        private void UnlockFuelPoint(Common.FuelPoint fp)
        {
            this.GetLastId(fp);
            byte[] cmd = this.commandHelper.CreateCmd(fp.Address);
            this.ExecuteCommnad(cmd, fp);
            System.Threading.Thread.Sleep(100);
            if ((int)fp.GetExtendedProperty("LastSalesId", 0) > 0)
            {
                this.GetLastInfo(fp);
                this.ExecuteCommnad(cmd, fp);
            }
            System.Threading.Thread.Sleep(50);
        }

        public struct OldFpStatus
        {
            public int FPAddress
            {
                get;
                set;
            }

            public int FPchannel
            {
                get;
                set;
            }

            public Common.Enumerators.FuelPointStatusEnum status
            {
                get;
                set;
            }
        }
        void fp_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {

        }
    }
}
