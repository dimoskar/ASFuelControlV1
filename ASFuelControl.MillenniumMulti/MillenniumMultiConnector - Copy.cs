using ASFuelControl.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASFuelControl.MillenniumMulti
{
    public class Controller : Common.FuelPumpControllerBase
    {
        public Controller()
        {
            this.ControllerType = Common.Enumerators.ControllerTypeEnum.MillenniumMulti;
            this.Controller = new MillenniumMultiConnector();
        }

    }
    public class MillenniumMultiConnector : Common.IFuelProtocol
    {
        private Dictionary<int, int> NozzleIndexForHumans = new Dictionary<int, int>();

        private decimal max = 999999;
        private bool superdh = true;
        private byte[] cmd;
        private List<byte> buffer = new List<byte>();
        public int offset = 0; //product1 Super, Product2 Diese_heat
        public event EventHandler<Common.TotalsEventArgs> TotalsRecieved;
        public event EventHandler<Common.FuelPointValuesArgs> DataChanged;
        public event EventHandler<Common.FuelPointValuesArgs> DispenserStatusChanged;
        public event EventHandler DispenserOffline;
        private System.Threading.Thread th;
        private List<Common.FuelPoint> fuelPoints = new List<Common.FuelPoint>();
        private System.IO.Ports.SerialPort serialPort = new System.IO.Ports.SerialPort();
        //public Common.DebugValues foo = new Common.DebugValues();

        public DebugValues DebugStatusDialog(FuelPoint fp) { throw new NotImplementedException(); }
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
            NozzleIndexForHumans.Add(1, 1);
            NozzleIndexForHumans.Add(2, 2);
            NozzleIndexForHumans.Add(4, 3);
            NozzleIndexForHumans.Add(8, 4);
            try
            {
                this.serialPort.PortName = this.CommunicationPort;

                this.serialPort.Open();

                this.th = new System.Threading.Thread(new System.Threading.ThreadStart(this.ThreadRun));
                th.Start();
            }
            catch (Exception ex)
            {
                if (System.IO.File.Exists(LogPath()))
                    System.IO.File.AppendAllText(LogPath(), ex.Message + "\r\n");
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

        //public  Common.DebugValues DebugStatusDialog(Common.FuelPoint fp)
        //{
        //    foo = null;
        //    GetStatus(fp);
        //    foo.Status = fp.Status;
        //    return foo;
        //}


        private void ThreadRun()
        {
            try
            {
                foreach (Common.FuelPoint fp in this.fuelPoints)
                {
                    foreach (Nozzle nz in fp.Nozzles)
                    {
                        nz.QueryTotals = true;
                    }
                }
            }
            catch (Exception ex)
            {
                if (System.IO.File.Exists(LogPath()))
                    System.IO.File.AppendAllText(LogPath(), ex.Message + "\r\n");

            }
            while (this.IsConnected)
            {
                try
                {
                    foreach (Common.FuelPoint fp in this.fuelPoints)
                    {

                        try
                        {
                            //this.Open(fp);

                            //if (System.IO.File.Exists(LogPath()))
                            //    System.IO.File.AppendAllText(LogPath(), string.Format("Address : {0}, Channel : {1}", fp.Address, fp.Channel) + "\r\n");


                            int nozzleForTotals = fp.Nozzles.Where(n => n.QueryTotals).Count();
                            if (nozzleForTotals > 0)
                            {
                                foreach (Common.Nozzle nz in fp.Nozzles)
                                {
                                    if (nz.QueryTotals)
                                    {
                                        this.GetLastSalesId(nz);
                                    }
                                }
                                continue;
                            }
                            this.GetStatus(fp);


                            if (fp.QueryAuthorize)
                            {
                                //System.Threading.Thread.Sleep(15);
                                this.SetPrice(fp.ActiveNozzle);
                                this.PostWork(fp);

                                if (fp.PresetVolume == max && fp.PresetAmount == max)
                                    this.Authorize(fp.ActiveNozzle);
                                else if (fp.PresetAmount != max && fp.PresetVolume == max)
                                    this.AuthorizeAmount(fp.ActiveNozzle);
                                else if (fp.PresetVolume != max && fp.PresetAmount == max)
                                    this.AuthorizeVolume(fp.ActiveNozzle);

                                //this.Authorize(fp.ActiveNozzle);
                                continue;
                            }

                            if (fp.Status == Common.Enumerators.FuelPointStatusEnum.Work)
                            {
                                this.GetDisplay(fp);
                                continue;
                            }

                            if (fp.Status == Common.Enumerators.FuelPointStatusEnum.Close)
                            {
                                this.Open(fp);
                                continue;
                            }

                            if (fp.Status == Common.Enumerators.FuelPointStatusEnum.Ready)
                            {
                                this.PostAuth(fp);
                                fp.QueryAuthorize = false;
                                fp.Status = Common.Enumerators.FuelPointStatusEnum.Work;
                                fp.DispenserStatus = fp.Status;
                                continue;
                            }

                        }
                        finally
                        {
                            System.Threading.Thread.Sleep(110);
                        }


                    }
                }
                catch (Exception ex)
                {
                    System.Threading.Thread.Sleep(200);
                    if (System.IO.File.Exists(LogPath()))
                        System.IO.File.AppendAllText(LogPath(), ex.Message + "\r\n");
                }
            }
        }



        public void GetDisplay(Common.FuelPoint fp)
        {
            CreateCmd(fp.Address, fp.Channel, 1, CommandType.RequestDisplayData);
            this.executeCommand(this.cmd, fp, 28);
        }
        public void GetTotals(Common.Nozzle nz)
        {
            CreateCmd(nz.ParentFuelPoint.Address, nz.ParentFuelPoint.Channel, nz.NozzleIndex, CommandType.RequestTotals);
            this.executeCommand(this.cmd, nz.ParentFuelPoint, 200);
        }
        public void PostAuth(Common.FuelPoint fp)
        {
            CreateCmd(fp.Address, fp.Channel, 1, CommandType.InitializeA);
            this.executeCommand(this.cmd, fp, 20);
        }
        public void PostWork(Common.FuelPoint fp)
        {
            CreateCmd(fp.Address, fp.Channel, 1, CommandType.PostWork);
            this.executeCommand(this.cmd, fp, 20);
        }
        public void GetStatus(Common.FuelPoint fp)
        {
            CreateCmd(fp.Address, fp.Channel, 1, CommandType.RequestStatus);
            this.executeCommand(this.cmd, fp, 10);
        }
        public void SetPrice(Common.Nozzle nz)
        {
            CreateCmd(nz.ParentFuelPoint.Address, nz.ParentFuelPoint.Channel, nz.NozzleIndex, CommandType.SendPrices, (int)(nz.UnitPrice * 1000));
            this.executeCommand(this.cmd, nz.ParentFuelPoint, 40);
        }
        public void AuthorizeVolume(Common.Nozzle nz)
        {
            ////
            ////Volume * unitprice = Amount;
            ////fp.PresetVolume* fp.act
            int amount = (int)(nz.UnitPrice * nz.ParentFuelPoint.PresetVolume * 100);
            CreateCmd(nz.ParentFuelPoint.Address, nz.ParentFuelPoint.Channel, nz.NozzleIndex, CommandType.Preset, (int)nz.ParentFuelPoint.PresetAmount * 100);
            this.executeCommand(this.cmd, nz.ParentFuelPoint, 100);
        }
        public void AuthorizeAmount(Common.Nozzle nz)
        {
            ////Mexri 2 dekadika
            CreateCmd(nz.ParentFuelPoint.Address, nz.ParentFuelPoint.Channel, nz.NozzleIndex, CommandType.Preset, (int)nz.ParentFuelPoint.PresetAmount * 100);
            this.executeCommand(this.cmd, nz.ParentFuelPoint, 100);
        }
        public void Authorize(Common.Nozzle nz)
        {
            CreateCmd(nz.ParentFuelPoint.Address, nz.ParentFuelPoint.Channel, nz.NozzleIndex, CommandType.Authorize);
            this.executeCommand(this.cmd, nz.ParentFuelPoint, 25);
        }
        public void Open(Common.FuelPoint fp)
        {
            CreateCmd(fp.Address, fp.Channel, 1, CommandType.OpenNozzle);
            this.executeCommand(this.cmd, fp, 10);
        }
        public void Close(Common.FuelPoint fp)
        {
            CreateCmd(fp.Address, fp.Channel, 1, CommandType.CloseNozzle);
            this.executeCommand(this.cmd, fp, 10);
        }
        public void GetLastSalesId(Common.Nozzle nz)
        {
            CreateCmd(nz.ParentFuelPoint.Address, nz.ParentFuelPoint.Channel, nz.NozzleIndex, CommandType.GetLastSalesId);
            this.executeCommand(this.cmd, nz.ParentFuelPoint, 40);
        }
        public void LastTransactionInfo(Common.Nozzle nz)
        {
            int id = (int)nz.GetExtendedProperty("LastSalesId", 10);
            if (id != 0)
            {
                try
                {
                    CreateCmd(nz.ParentFuelPoint.Address, nz.ParentFuelPoint.Channel, nz.NozzleIndex, CommandType.InfoForId, id);
                    this.executeCommand(this.cmd, nz.ParentFuelPoint, 40);
                    System.Threading.Thread.Sleep(70);
                }
                catch
                {
                }
            }
        }



        private void CallEventDataChanged(Common.FuelPoint fp)
        {
            if (this.DataChanged != null)
            {
                Common.FuelPointValues values = new Common.FuelPointValues();
                values.CurrentSalePrice = fp.Nozzles[0].UnitPrice;
                values.CurrentPriceTotal = fp.DispensedAmount;
                values.CurrentVolume = fp.DispensedVolume;


                this.DataChanged(this, new Common.FuelPointValuesArgs()
                {
                    CurrentFuelPoint = fp,
                    CurrentNozzleId = fp.ActiveNozzle != null ? fp.ActiveNozzle.Index : 0,
                    Values = values
                });
            }
        }
        private void evaluateBuffer(List<byte> buffer)
        {
            Response type;
            try
            {

                foreach (byte[] response in buffer.ToArray().ValidResponses())
                {
                    if (System.IO.File.Exists(LogPath()))
                        System.IO.File.AppendAllText(LogPath(), "RX : " + BitConverter.ToString(response) + "\r\n");
                    Common.Nozzle tnz = new Nozzle();
                    Common.FuelPoint tfp = this.fuelPoints.Where(x => (x.Address == (int)response[3]) && x.Channel == (int)(response[10] - 32)).First();
                    type = (Response)response.Length;

                    #region responsetypeSW

                    switch (type)
                    {

                        #region SelfStatus
                        case Response.SelfStatus:
                            Common.Enumerators.FuelPointStatusEnum oldStatus = tfp.Status;
                            int nzIndexA = (int)response[18]; //Minimum Value 1
                            tfp.Status = evalStatus((int)response[15]);
                            tfp.DispenserStatus = tfp.Status;
                            tnz = tfp.Nozzles.Where(x => (x.Index == nzIndexA)).FirstOrDefault();
                            if (tfp.Status == Common.Enumerators.FuelPointStatusEnum.Work || tfp.Status == Common.Enumerators.FuelPointStatusEnum.Idle) tfp.QueryAuthorize = false;

                            if (tfp.Status != oldStatus && this.DispenserStatusChanged != null)
                            {
                                Common.FuelPointValues values = new Common.FuelPointValues();
                                if (tfp.Status != Common.Enumerators.FuelPointStatusEnum.Close && tfp.Status != Common.Enumerators.FuelPointStatusEnum.Idle && tfp.Status != Common.Enumerators.FuelPointStatusEnum.Offline)
                                {
                                    tfp.ActiveNozzleIndex = tnz.Index - 1;
                                    values.ActiveNozzle = tnz.Index - 1;
                                }
                                else
                                {
                                    tfp.ActiveNozzleIndex = -1;
                                    values.ActiveNozzle = -1;
                                }

                                values.Status = tfp.Status;

                                if (tfp.Status == Common.Enumerators.FuelPointStatusEnum.Close)
                                {
                                    values.Status = Common.Enumerators.FuelPointStatusEnum.Idle;
                                }

                                this.DispenserStatusChanged(this, new Common.FuelPointValuesArgs()
                                {
                                    CurrentFuelPoint = tfp,
                                    CurrentNozzleId = nzIndexA != 0 ? tnz.Index : 0,
                                    Values = values
                                });

                            }
                            break;

                        #endregion

                        #region TransactionComplete
                        case Response.SelfTransactionComplete:
                            decimal newAmountS = response.skip(30).takeToDecimal(3) / 100;
                            decimal newVolumeS = response.skip(40).takeToDecimal(3) / 100;

                            if (tfp.DispensedVolume >= 0 && newVolumeS > 0)
                                tfp.DispensedVolume = newVolumeS;

                            if (tfp.DispensedAmount >= 0 && newAmountS > 0)
                                tfp.DispensedAmount = newAmountS;

                            CallEventDataChanged(tfp);
                            break;
                        #endregion

                        #region Display
                        case Response.Display:
                            decimal newVolume = response.skip(21).takeToDecimal(3) / 100;
                            decimal newAmount = response.skip(14).takeToDecimal(3) / 100;

                            int nzIndexD = tfp.ActiveNozzle.Index + 1;//(int)response[16];
                            tnz = tfp.Nozzles.Where(x => (x.NozzleIndex == nzIndexD)).FirstOrDefault();

                            if (tfp.DispensedVolume >= 0 && newVolume > 0)
                                tfp.DispensedVolume = newVolume;
                            if (tfp.DispensedAmount >= 0 && newAmount > 0)
                                tfp.DispensedAmount = newAmount;

                            int flag = (int)response.skip(13).takeToDecimal(1);
                            if (flag >= 6)
                            {
                                tnz.SetExtendedProperty("LastSalesId", (int)response.skip(33).takeToDecimal(2));
                            }
                            CallEventDataChanged(tfp);
                            break;
                        #endregion

                        #region LastSaleID
                        case Response.LastSalesId:
                            int nzIndexL = (int)response[11] - 16;
                            nzIndexL = NozzleIndexForHumans.FirstOrDefault(x => x.Value == nzIndexL).Key;
                            tnz = tfp.Nozzles.Where(x => (x.NozzleIndex == nzIndexL)).FirstOrDefault();
                            lock (tnz)
                            {
                                tnz.TotalVolume = response.skip(15).takeToDecimal(6);
                                tnz.TotalPrice = response.skip(24).takeToDecimal(6);
                                tnz.SetExtendedProperty("LastSalesId", (int)response.skip(37).takeToDecimal(2));
                                tnz.SetExtendedProperty("GotTotals", true);
                            }
                            Common.Enumerators.FuelPointStatusEnum tempstatus = tfp.Status;
                            int init = 0;
                            foreach (Common.Nozzle nz in tfp.Nozzles)
                            {
                                if ((bool)nz.GetExtendedProperty("GotTotals", false))
                                    init++;
                            }
                            tnz.QueryTotals = false;
                            //if (tnz.GetExtendedProperty("LastTotalVolume") != null && (decimal)tnz.GetExtendedProperty("LastTotalVolume") == tnz.TotalVolume)
                            //{
                            //    if (init == tfp.NozzleCount)
                            //        tfp.Initialized = true;
                            //    else tfp.Initialized = false;
                            //    tfp.Status = tempstatus;
                            //}
                            //else
                            //{
                            //tnz.SetExtendedProperty("LastTotalVolume", tnz.TotalVolume);

                            if (init == tfp.NozzleCount)
                                tfp.Initialized = true;
                            else tfp.Initialized = false;

                            if (this.TotalsRecieved != null)
                                this.TotalsRecieved(this, new Common.TotalsEventArgs(
                                    tfp,
                                    tnz.Index,
                                    tnz.TotalVolume,
                                    tnz.TotalPrice));
                            tfp.Status = tempstatus;
                            //}
                            break;
                        #endregion

                        #region Totals
                        case Response.Totals:
                            int nzIndexT = (int)response[11];
                            tnz = tfp.Nozzles.Where(x => (x.NozzleIndex == nzIndexT)).FirstOrDefault();
                            tnz.TotalVolume = response.skip(14).takeToDecimal(6);
                            tnz.TotalPrice = response.skip(23).takeToDecimal(6);
                            Common.Enumerators.FuelPointStatusEnum tempstatus1 = tfp.Status;
                            tnz.QueryTotals = false;
                            //if (tnz.GetExtendedProperty("LastTotalVolume") == null || (decimal)tnz.GetExtendedProperty("LastTotalVolume") > tnz.TotalVolume)
                            //{
                            if (this.TotalsRecieved != null)
                                this.TotalsRecieved(this, new Common.TotalsEventArgs(tfp, tnz.NozzleIndex, tnz.TotalVolume, tnz.TotalPrice));
                            //}
                            tfp.Status = tempstatus1;
                            break;
                        #endregion

                        #region InfoforID
                        case Response.InfoForId:
                            tfp.DispensedAmount = response.skip(21).takeToDecimal(3) / 100;
                            tfp.DispensedVolume = response.skip(28).takeToDecimal(3) / 100;
                            // tfp.Nozzles[0].UnitPrice = response.skip(35).takeToDecimal(2) / 1000;
                            break;
                        #endregion

                        #region SummaryFor ID
                        case Response.SummaryForId:
                            tfp.DispensedAmount = response.skip(29).takeToDecimal(3) / 100;
                            tfp.DispensedVolume = response.skip(36).takeToDecimal(3) / 100;
                            CallEventDataChanged(tfp);
                            break;
                        #endregion

                        #region Status
                        case Response.Status:
                        case Response.Status2:
                            tfp.SetExtendedProperty("statusFails", 0);
                            oldStatus = tfp.Status;

                            tfp.Status = evalStatus((int)response[13]);
                            tfp.DispenserStatus = tfp.Status;
                            //foo.Status = tfp.Status;
                            int nzIndexS = (int)response[16];
                            if (nzIndexS != 0)
                                tnz = tfp.Nozzles.Where(x => (x.NozzleIndex == nzIndexS)).FirstOrDefault();

                            if (tfp.Status == Common.Enumerators.FuelPointStatusEnum.Work || tfp.Status == Common.Enumerators.FuelPointStatusEnum.Idle)
                                tfp.QueryAuthorize = false;
                            if (tfp.Status != oldStatus && this.DispenserStatusChanged != null)
                            {
                                Common.FuelPointValues values = new Common.FuelPointValues();
                                if (tfp.Status != Common.Enumerators.FuelPointStatusEnum.Close && tfp.Status != Common.Enumerators.FuelPointStatusEnum.Idle && tfp.Status != Common.Enumerators.FuelPointStatusEnum.Offline)
                                {
                                    tfp.ActiveNozzleIndex = tnz.Index - 1;
                                    values.ActiveNozzle = tnz.Index - 1;
                                }
                                else
                                {
                                    tfp.ActiveNozzleIndex = -1;
                                    values.ActiveNozzle = -1;
                                }

                                values.Status = tfp.Status;

                                if (tfp.Status == Common.Enumerators.FuelPointStatusEnum.Close)
                                {
                                    values.Status = Common.Enumerators.FuelPointStatusEnum.Idle;
                                }

                                this.DispenserStatusChanged(this, new Common.FuelPointValuesArgs()
                                {
                                    CurrentFuelPoint = tfp,
                                    CurrentNozzleId = nzIndexS != 0 ? tnz.Index : 0,
                                    Values = values
                                });

                            }


                            break;
                        #endregion

                        #region SetPrice
                        case Response.SetPrice:
                            tfp.QuerySetPrice = false;
                            //if(response[14] == 1) tfp.SetExtendedProperty("setPrice1", 1);
                            //else if(response[14] == 2) tfp.SetExtendedProperty("setPrice2", 1);

                            //if((int)tfp.GetExtendedProperty("setPrice1", 0) == 1 && (int)tfp.GetExtendedProperty("setPrice2", 0) == 1)
                            //{
                            //    tfp.QuerySetPrice = false;
                            //}
                            break;
                        #endregion

                    }
                    #endregion

                }


                buffer.Clear();
            }

            catch
            {
                buffer.Clear();
            }
        }
        private Common.Enumerators.FuelPointStatusEnum evalStatus(int flag)
        {

            switch (flag)
            {
                case (2):
                    return Common.Enumerators.FuelPointStatusEnum.Close;
                case (3):
                    return Common.Enumerators.FuelPointStatusEnum.Idle;
                case (4):
                    return Common.Enumerators.FuelPointStatusEnum.Nozzle;
                case (6):
                    return Common.Enumerators.FuelPointStatusEnum.Ready;
                case (8):
                    return Common.Enumerators.FuelPointStatusEnum.Work;
                case (9):
                    return Common.Enumerators.FuelPointStatusEnum.Work;
                default:
                    return Common.Enumerators.FuelPointStatusEnum.Offline;
            }
        }
        private void executeCommand(byte[] cmd, Common.FuelPoint fp, int Timeout)
        {

            byte[] fetchbuffer = CreateCmd(fp.Address);

            this.serialPort.Write(cmd, 0, cmd.Length);
            if (System.IO.File.Exists(LogPath()))
                System.IO.File.AppendAllText(LogPath(), "TX : " + BitConverter.ToString(cmd) + "\r\n");
            this.serialPort.Write(fetchbuffer, 0, fetchbuffer.Length);
            System.Threading.Thread.Sleep(Timeout);
            System.Threading.Thread.Sleep(100);
            while (this.serialPort.BytesToRead > 0)
            {
                System.Threading.Thread.Sleep(45);
                byte[] temp = new byte[this.serialPort.BytesToRead];
                this.serialPort.Read(temp, 0, temp.Length);
                this.buffer.AddRange(temp);
            }

            if (buffer.Count == 0 && DateTime.Now.Subtract(fp.LastValidResponse).TotalSeconds > 5)
            {
                if (this.DispenserOffline != null)
                    this.DispenserOffline(fp, new EventArgs());
                return;
            }

            if (this.buffer.Count == 0)
                return;
            fp.LastValidResponse = DateTime.Now;
            if (System.IO.File.Exists(LogPath()))
                System.IO.File.AppendAllText(LogPath(), "RX : " + BitConverter.ToString(this.buffer.ToArray()) + "\r\n");
            evaluateBuffer(this.buffer);
        }
        private byte[] CreateCmd(int dispenserId)
        {
            byte[] Buffer = new byte[] { 0xFF, (byte)(dispenserId) };
            return Buffer;
        }
        private void CreateCmd(int dispenserId, int fuelPointChannel, int nzIndex, CommandType command)
        {
            byte[] Buffer = new byte[] { 0x00 };
            switch (command)
            {
                case CommandType.RequestStatus:
                    Buffer = new byte[] { 0xFD, (byte)(dispenserId), 0x00, 0x02, 0x01, 0x00, 0x00, 0x00, 0x04, 0x01, (byte)(fuelPointChannel + 32), 0x14, 0x15, 0xFC };
                    break;
                case CommandType.SendPrices1:
                    throw new Exception("wrong format, use createCmd(int dispenserId, int fuelPointChannel, CommandTypeEnum command, int price) to setPrices");
                case CommandType.SendPrices2:
                    throw new Exception("wrong format, use createCmd(int dispenserId, int fuelPointChannel, CommandTypeEnum command, int price) to setPrices");
                case CommandType.Authorize:
                    Buffer = new byte[] { 0xFD, (byte)(dispenserId), 0x00, 0x02, 0x01, 0x00, 0x40, 0x00, 0x07, 0x01, (byte)(fuelPointChannel + 32), 0x19, 0x01, (byte)nzIndex, 0x3E, 0x00, 0xFC };
                    break;
                case CommandType.CloseNozzle:
                    Buffer = new byte[] { 0xFD, (byte)(dispenserId), 0x00, 0x02, 0x01, 0x00, 0x40, 0x00, 0x04, 0x01, (byte)(fuelPointChannel + 32), 0x3D, 0x00, 0xFC };
                    break;
                case CommandType.OpenNozzle:
                    Buffer = new byte[] { 0xFD, (byte)(dispenserId), 0x00, 0x02, 0x01, 0x00, 0x40, 0x00, 0x04, 0x01, (byte)(fuelPointChannel + 32), 0x3C, 0x00, 0xFC };
                    break;
                case CommandType.RequestTotals:
                    Buffer = new byte[] { 0xFD, (byte)(dispenserId), 0x00, 0x02, 0x01, 0x00, 0x00, 0x00, 0x05, 0x02, (byte)(fuelPointChannel + 32), (byte)(16 + nzIndex), 0x14, 0x15, 0xFC };
                    break;
                case CommandType.RequestDisplayData:
                    Buffer = new byte[] { 0xFD, (byte)(dispenserId), 0x00, 0x02, 0x01, 0x00, 0x00, 0x00, 0x05, 0x01, (byte)(fuelPointChannel + 32), 0x22, 0x23, 0x1D, 0xFC };
                    break;
                case CommandType.GetLastSalesId:
                    Buffer = new byte[] { 0xFD, (byte)(dispenserId), 0x00, 0x02, 0x01, 0x00, 0x00, 0x00, 0x06, 0x02, (byte)(fuelPointChannel + 32), (byte)(16 + NozzleIndexForHumans[nzIndex]), 0x14, 0x15, 0x16, 0xFC };
                    break;
                case CommandType.PostWork:
                    Buffer = new byte[] { 0xFD, (byte)(dispenserId), 0x00, 0x02, 0x01, 0x00, 0x40, 0x00, 0x04, 0x01, (byte)(fuelPointChannel + 32), 0xD8, 0x00, 0xFC };
                    break;
            }

            if (System.IO.File.Exists(LogPath()))
                System.IO.File.AppendAllText(LogPath(), string.Format("DispenserID : {0}, Channel : {1}, NozzleIndex: {2}, Command: {3}", dispenserId, fuelPointChannel, nzIndex, BitConverter.ToString(Buffer)) + "\r\n");
            ReturnCmdWithCrc(Buffer);
        }
        private void CreateCmd(int dispenserId, int fuelPointChannel, int nzIndex, CommandType command, int price)
        {
            byte[] buffer = new byte[] { 0x3F };
            if (command == CommandType.SendPrices)
            {

                string sprice = price.ToString();

                for (int i = sprice.Length; i < 4; i++)
                {
                    sprice = "0" + sprice;
                }

                int p1 = Convert.ToInt32((sprice.Substring(0, 2)));
                int p2 = Convert.ToInt32((sprice.Substring(2, 2)));

                buffer = new byte[] { 0xFD, (byte)(dispenserId), 0x00, 0x02, 0x01, 0x00, 0x40, 0x00, 0x0D, 0x06, 0x61, 0x00, 0x00, 0x00, (byte)nzIndex, 0x11, 0x02, 0x04, 0x03, 0x00, (byte)ith(p1), (byte)ith(p2), 0xFC };
                ReturnCmdWithCrc(buffer);
            }
            else if (command == CommandType.SummaryForId)
            {
                string sprice = price.ToString();
                for (int i = sprice.Length; i < 4; i++)
                {
                    sprice = "0" + sprice;
                }
                int p1 = Convert.ToInt32((sprice.Substring(0, 2)));
                int p2 = Convert.ToInt32((sprice.Substring(2, 2)));
                //                    FD         01            00       02 01 00 40 00 07 04 [21] 21 03 00 1E 00 FC 30 30 42 32 FB
                //:                   FD         01              00     02    01    00    40    00    07    04         21                       21                      01              49         1E     00     FC
                buffer = new byte[] { 0xFD, (byte)(dispenserId), 0x00, 0x02, 0x01, 0x00, 0x40, 0x00, 0x07, 0x04, (byte)(fuelPointChannel + 32), (byte)(nzIndex + 32), (byte)ith(p1), (byte)ith(p2), 0x1E, 0x00, 0xFC };
                ReturnCmdWithCrc(buffer);

            }
            else if (command == CommandType.InfoForId)
            {
                string sprice = price.ToString();
                for (int i = sprice.Length; i < 4; i++)
                {
                    sprice = "0" + sprice;
                }
                int p1 = Convert.ToInt32((sprice.Substring(0, 2)));
                int p2 = Convert.ToInt32((sprice.Substring(2, 2)));
                buffer = new byte[] { 0xFD, (byte)(dispenserId), 0x00, 0x02, 0x01, 0x00, 0x02, 0x00, 0x0C, 0x04, (byte)(fuelPointChannel + 32), (byte)(fuelPointChannel + 32), (byte)ith(p1), (byte)ith(p2), 0x01, 0x05, 0x06, 0x07, 0x08, 0x0C, 0x15, 0xFC };
                ReturnCmdWithCrc(buffer);
            }
            else if (command == CommandType.Preset)
            {
                string sprice = price.ToString();
                for (int i = sprice.Length; i < 4; i++)
                {
                    sprice = "0" + sprice;
                }
                int p1 = Convert.ToInt32((sprice.Substring(0, 2)));
                int p2 = Convert.ToInt32((sprice.Substring(2, 2)));

                buffer = new byte[] { 0xFD, (byte)dispenserId, 0x00, 0x02, 0x01, 0x00, 0x40, 0x00, 0x0D, 0x01, (byte)(fuelPointChannel + 32), 0x1B, 0x04, 0x04, 0x00, (byte)ith(p1), (byte)ith(p2), 0x19, 0x01, 0x01, 0x3E, 0x00, 0xFC };
                ReturnCmdWithCrc(buffer);
            }
            else
            {
                throw new System.ArgumentException("you cannot useoverloeaded creatCMD() for CommandTypeEnum{0} ", "command");
            }
        }
        private int ith(int num)
        {
            return 16 * (num / 10) + (num % 10);
        }// hex = integerToHex(int)
        private void ReturnCmdWithCrc(byte[] buffer)
        {   //input buffer    FD xx ... xx FC
            //return          FD xx ... xx FC crc FB
            if (buffer.Last() != 0xFC || buffer[0] != 0xFD)
            {
                throw new Exception("this is not valid command");
            }
            else
            {
                int sumR = 0;
                int sumL = 0;
                int temp = 0;
                int crc0 = 0;
                int crc1 = 0;
                int crc2 = 0;

                // byte[] buffer = new byte[] { 0xFD, 0x07, 0x00, 0x02, 0x01, 0x00, 0x40, 0x00, 0x0D, 0x06, 0x61, 0x00, 0x00, 0x00, 0x05, 0x11, 0x02, 0x04, 0x03, 0x00, 0x44, 0x40, 0xFC };
                string command = BitConverter.ToString(buffer.Skip(1).Take(buffer.Length - 2).ToArray()); //drop FD and FC
                string[] commandParms = command.Split('-'); //commandParms = xx-xx-xx-xx and parms are hex form.

                for (int j = 0; j < 2; j++)
                {
                    for (int i = 0; i < commandParms.Length; i++)
                    {
                        temp = Int32.Parse(commandParms[i][j].ToString(), System.Globalization.NumberStyles.HexNumber); //hex to int
                        if (j == 0)
                        {
                            sumL += Convert.ToInt32(temp);
                        }
                        else
                        {
                            sumR += Convert.ToInt32(temp);
                        }
                    }
                }

                for (int i = 1; i < sumR + 1; i++)
                {
                    crc0++;
                    if (crc0 == 16)
                    {
                        crc1++; crc0 = 0;
                    }
                }

                for (int i = 1; i < sumL + 1; i++)
                {
                    crc1++;
                    if (crc1 == 16)
                    {
                        crc2++; crc1 = 0;
                    }
                }

                //convert these intergers to Bytes
                if (crc0 < 10) crc0 += 48; else crc0 += 55;
                if (crc1 < 10) crc1 += 48; else crc1 += 55;
                if (crc2 < 10) crc2 += 48; else crc2 += 55;

                List<byte> cmdWithCrc = new List<byte>();
                cmdWithCrc.AddRange(buffer);
                cmdWithCrc.Add(0x30);
                cmdWithCrc.Add((byte)crc2);
                cmdWithCrc.Add((byte)crc1);
                cmdWithCrc.Add((byte)crc0);
                cmdWithCrc.Add(0xFB);
                // return cmdWithCrc.ToArray();
                this.cmd = cmdWithCrc.ToArray();
                if (System.IO.File.Exists(LogPath()))
                    System.IO.File.AppendAllText(LogPath(), "TX : " + BitConverter.ToString(this.cmd) + "\r\n");
            }

        }
        private enum Response
        {
            Totals = 36,
            Status = 23,
            Status2 = 21,
            SelfStatus = 29,
            SetPrice = 25,
            Display = 35,
            LastSalesId = 45,
            SelfTransactionComplete = 47,
            InfoForId = 53,
            SummaryForId = 71,

        }
        private enum ResponseLength
        {
            ResponseStatus = 23,
            ResoibseSetPrice = 25,
            ResponseDisplayData = 35,
            ResponseGetLastSalesId = 45,
            ResponseInfoForId = 53,
            ResponseSummaryForId = 71,
        }
        private enum CommandType
        {

            SendPrices = 0,
            SendPrices1 = 1,
            SendPrices2 = 2,
            InfoForId = 3,
            SummaryForId = 4,
            Unlock,
            PostWork,
            InitializeA,
            PreAuthorize,
            GlobalInitialize,
            FetchBuffer,
            CloseNozzle,
            OpenNozzle,
            RequestDisplayData,
            RequestStatus,
            Authorize,
            SendMainDisplayData,
            RequestTotals,
            Preset,

            GetLastSalesId,

        }
        public string LogPath()
        {
            string LogPath = "Mil_" + this.CommunicationPort + ".log";
            return LogPath;
        }
    }

    #region Extensions
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
        public static List<byte[]> ValidResponses(this byte[] value)
        {
            List<byte[]> responses = new List<byte[]>();
            byte StartByte = 0xFD;
            byte FinByte = 0xFB;
            byte[] result = new byte[] { };
            int l = value.Length;

            foreach (int index in value.indexesOfByte(StartByte))
            {
                for (int k = index; k < l; k++)
                {
                    if (value[k] == FinByte)
                    {
                        result = value.ToArray().Skip(index).Take(k - index + 1).ToArray();
                        if (CheckCrc(result))
                            responses.Add(result);
                        break;
                    }
                }
                continue;
            }
            return responses;

        }
        private static bool CheckCrc(byte[] buffer)
        {
            if (buffer.Length < 6)
                return false;

            int sumR = 0;
            int sumL = 0;
            int temp = 0;
            int crc0 = 0;
            int crc1 = 0;
            int crc2 = 0;

            // byte[] buffer = new byte[] { 0xFD, 0x07, 0x00, 0x02, 0x01, 0x00, 0x40, 0x00, 0x0D, 0x06, 0x61, 0x00, 0x00, 0x00, 0x05, 0x11, 0x02, 0x04, 0x03, 0x00, 0x44, 0x40, 0xFC };

            byte[] pBuffer = buffer.Reverse().Skip(5).Reverse().ToArray();

            string command = BitConverter.ToString(pBuffer.Skip(1).Take(pBuffer.Length - 2).ToArray()); //drop FD and FC
            string[] commandParms = command.Split('-'); //commandParms = xx-xx-xx-xx and parms are hex form.

            for (int j = 0; j < 2; j++)
            {
                for (int i = 0; i < commandParms.Length; i++)
                {
                    temp = Int32.Parse(commandParms[i][j].ToString(), System.Globalization.NumberStyles.HexNumber); //hex to int
                    if (j == 0)
                    {
                        sumL += Convert.ToInt32(temp);
                    }
                    else
                    {
                        sumR += Convert.ToInt32(temp);
                    }
                }
            }

            for (int i = 1; i < sumR + 1; i++)
            {
                crc0++;
                if (crc0 == 16)
                {
                    crc1++; crc0 = 0;
                }
            }

            for (int i = 1; i < sumL + 1; i++)
            {
                crc1++;
                if (crc1 == 16)
                {
                    crc2++; crc1 = 0;
                }
            }

            //convert these intergers to Bytes
            if (crc0 < 10) crc0 += 48; else crc0 += 55;
            if (crc1 < 10) crc1 += 48; else crc1 += 55;
            if (crc2 < 10) crc2 += 48; else crc2 += 55;

            byte[] revBuffer = buffer.Reverse().Skip(1).Take(3).ToArray();

            if (crc0 == revBuffer[0] && crc1 == revBuffer[1] && crc2 == revBuffer[2])
                return true;

            return false;
        }
        public static List<int> indexesOfByte(this byte[] value, byte target)
        {
            List<int> indexes = new List<int>();
            for (int i = 0; i < value.Length; i++)
                if (value[i] == target)
                    indexes.Add(i);
            return indexes;
        }
    #endregion

    }

}