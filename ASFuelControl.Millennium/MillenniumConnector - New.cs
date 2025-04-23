using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASFuelControl.Millennium
{
    public class MillenniumConnector: Common.IFuelProtocol
    {
        private decimal max = 999999;
        private bool superdh = true;
        private byte[] cmd;
        private List<byte> buffer = new List<byte>();
        public int offset = 0; //product1 Super, Product2 Diese_heat
        public event EventHandler<Common.TotalsEventArgs> TotalsRecieved;
        public event EventHandler<Common.FuelPointValuesArgs> DataChanged;
        public event EventHandler<Common.FuelPointValuesArgs> DispenserStatusChanged;
        public event EventHandler DispenserOffline;
        public Common.DebugValues foo = new Common.DebugValues();
        private System.Threading.Thread th;
        private List<Common.FuelPoint> fuelPoints = new List<Common.FuelPoint>();
        private System.IO.Ports.SerialPort serialPort = new System.IO.Ports.SerialPort();
        public Common.DebugValues DebugStatusDialog(Common.FuelPoint fp)
        {
            foo = null;
            GetStatus(fp);
            return foo;
        }
        public Common.FuelPoint[] FuelPoints
        {
            set
            { this.fuelPoints = new List<Common.FuelPoint>(value); }
            get
            { return this.fuelPoints.ToArray(); }
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

                this.serialPort.Open();

                this.th = new System.Threading.Thread(new System.Threading.ThreadStart(this.ThreadRun));
                th.Start();
            }
            catch(Exception ex)
            {
            }
        }
        public void Disconnect()
        {
            if(this.serialPort.IsOpen)
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



        private void ThreadRun()
        {
            foreach(Common.FuelPoint fp in this.fuelPoints)
            {
                fp.Nozzles[0].QueryTotals = true;
                fp.SetExtendedProperty("statusFails", (int)0);
            }
            while(this.IsConnected)
            {
                try
                {
                    foreach(Common.FuelPoint fp in this.fuelPoints)
                    {
                        try
                        {
         

                            this.GetStatus(fp);

                            int nozzleForTotals = fp.Nozzles.Where(n => n.QueryTotals).Count();
                            if(nozzleForTotals > 0)
                            {

                                foreach(Common.Nozzle nz in fp.Nozzles)
                                {
                                    if(nz.QueryTotals)
                                    {
                                        //if((bool)(fp.GetExtendedProperty("iNeedDisplay", true)))
                                        //{
                                        //    this.LastTransactionInfo(nz.ParentFuelPoint);
                                        //    fp.SetExtendedProperty("iNeedDisplay", false);
                                        //}
                                        this.GetLastSalesId(fp);
                                        this.PostWork(fp);
                                    }
                                }

                                continue;
                            }

                            if(fp.QueryAuthorize)
                            {

                                this.SetPrice(fp);
                                System.Threading.Thread.Sleep(15);
                                this.PostWork(fp);
                                if (fp.PresetVolume == max && fp.PresetAmount == max)
                                    this.Authorize(fp);
                                else if (fp.PresetAmount != max && fp.PresetVolume == max)
                                    this.AuthorizeAmount(fp);
                                else if (fp.PresetVolume != max && fp.PresetAmount == max)
                                    this.AuthorizeVolume(fp);
                                continue;
                            }


                            if(fp.Status == Common.Enumerators.FuelPointStatusEnum.Work)
                            {
                                this.GetDisplay(fp);
                                continue;
                            }

                            if(fp.Status == Common.Enumerators.FuelPointStatusEnum.Close)
                            {
                                this.Open(fp);
                                continue;
                            }

                            if(fp.Status == Common.Enumerators.FuelPointStatusEnum.Ready)
                            {
                                //this.PostAuth(fp);
                                fp.QueryAuthorize = false;
                                fp.Status = Common.Enumerators.FuelPointStatusEnum.Work;
                                fp.DispenserStatus = fp.Status;
                                continue;
                            }

                            if(fp.QuerySetPrice)
                            {
                                this.SetPrice(fp);
                                continue;
                            }
                           
                        }
                        finally
                        {
                            System.Threading.Thread.Sleep(80);
                        }
                    }
                }
                catch
                {
                    System.Threading.Thread.Sleep(200);
                }
            }
        }

        public void GetDisplay(Common.FuelPoint fp)
        {
            Common.Nozzle nz = fp.Nozzles[0];
            CreateCmd(fp.Address, fp.Channel, nz.NozzleSocket, CommandType.RequestDisplayData);
            this.executeCommand(this.cmd, fp, true);
        }
        public void GetTotals(Common.FuelPoint fp)
        {
            Common.Nozzle nz = fp.Nozzles[0];
            CreateCmd(fp.Address, fp.Channel, nz.NozzleSocket, CommandType.RequestTotals);
            this.executeCommand(this.cmd, fp, true);
        }
        public void PostAuth(Common.FuelPoint fp)
        {
            Common.Nozzle nz = fp.Nozzles[0];
            CreateCmd(fp.Address, fp.Channel, nz.NozzleSocket, CommandType.InitializeA);
            this.executeCommand(this.cmd, fp, false);
        }
        public void PostWork(Common.FuelPoint fp)
        {
            Common.Nozzle nz = fp.Nozzles[0];
            CreateCmd(fp.Address, fp.Channel, nz.NozzleSocket, CommandType.PostWork);
            this.executeCommand(this.cmd, fp, false);
        }
        public void GetStatus(Common.FuelPoint fp)
        {
            Common.Nozzle nz = fp.Nozzles[0];
            CreateCmd(fp.Address, nz.NozzleSocket, fp.Channel, CommandType.RequestStatus);
            foo.TX = this.cmd;
            this.executeCommand(this.cmd, fp, true);
            
        }
        public void SetPrice(Common.FuelPoint fp)
        {
            Common.Nozzle nz = fp.Nozzles[0];
            //if((int)fp.GetExtendedProperty("setPrice1", 0) == 0)
            //{
            //if(fp.Nozzles[0].UnitPrice == 0) return;
            CreateCmd(fp.Address, fp.Channel, nz.NozzleSocket, CommandType.SendPrices, (int)(fp.Nozzles[0].UnitPrice * 1000));
            this.executeCommand(this.cmd, fp, false);

            //CreateCmd(fp.Address, fp.Channel, CommandType.SendPrices, (int)(fp.Nozzles[0].UnitPrice * 1000),2);
            //this.executeCommand(this.cmd, fp);


            fp.QuerySetPrice = false;

        }

        public void AuthorizeVolume(Common.FuelPoint fp)
        {
            //
            //Volume * unitprice = Amount;
            //fp.PresetVolume* fp.act
            Common.Nozzle nz = fp.Nozzles[0];
            int amount = (int)(fp.ActiveNozzle.UnitPrice * fp.PresetVolume * 100);
            CreateCmd(fp.Address, fp.Channel, nz.NozzleSocket, CommandType.Preset, amount);
            this.executeCommand(this.cmd, fp, false);
        }
        public void AuthorizeAmount(Common.FuelPoint fp)
        {
            //Mexri 2 dekadika
            Common.Nozzle nz = fp.Nozzles[0];
            CreateCmd(fp.Address, fp.Channel, nz.NozzleSocket, CommandType.Preset, (int)fp.PresetAmount * 100);
            this.executeCommand(this.cmd, fp, false);
        }
        public void Authorize(Common.FuelPoint fp)
        {
            Common.Nozzle nz = fp.Nozzles[0];
            CreateCmd(fp.Address, fp.Channel, nz.NozzleSocket, CommandType.Authorize);
            this.executeCommand(this.cmd, fp, false);
        }

        public void Open(Common.FuelPoint fp)
        {
            Common.Nozzle nz = fp.Nozzles[0];
            CreateCmd(fp.Address, fp.Channel, nz.NozzleSocket, CommandType.OpenNozzle);
            this.executeCommand(this.cmd, fp, false);
        }
        public void Close(Common.FuelPoint fp)
        {
            Common.Nozzle nz = fp.Nozzles[0];
            CreateCmd(fp.Address, fp.Channel, nz.NozzleSocket, CommandType.CloseNozzle);
            this.executeCommand(this.cmd, fp, false);
        }
        public void GetLastSalesId(Common.FuelPoint fp)
        {   //GETTOTALS
            Common.Nozzle nz = fp.Nozzles[0];
            CreateCmd(fp.Address, fp.Channel, nz.NozzleSocket, CommandType.GetLastSalesId);
            this.executeCommand(this.cmd, fp, true);

        }
        public void LastTransactionInfo(Common.FuelPoint fp)
        {
            int id = (int)fp.GetExtendedProperty("LastSalesId", 0);
            if(id != 0)
            {
                try
                {
                    Common.Nozzle nz = fp.Nozzles[0];
                    CreateCmd(fp.Address, fp.Channel, nz.NozzleSocket, CommandType.InfoForId, id);
                    
                    this.executeCommand(this.cmd, fp, true);

                     System.Threading.Thread.Sleep(50);
                }
                catch
                {
                }
                try
                {
                    //CreateCmd(fp.Address, fp.Channel, CommandType.SummaryForId, id);
                   // this.executeCommand(this.cmd, fp, true);
                }
                catch
                {
                }
            }
        }


        private void CallEventDataChanged(Common.FuelPoint fp)
        {
            if(this.DataChanged != null)
            {
                Common.FuelPointValues values = new Common.FuelPointValues();
                values.CurrentSalePrice = fp.Nozzles[0].UnitPrice;
                values.CurrentPriceTotal = fp.DispensedAmount;
                values.CurrentVolume = fp.DispensedVolume;


                this.DataChanged(this, new Common.FuelPointValuesArgs()
                {
                    CurrentFuelPoint = fp,
                    CurrentNozzleId = 1,
                    Values = values
                });
            }
        }
        private void evaluateBuffer(List<byte> buffer)
        {
            Response type;
            try
            {

                foreach(byte[] response in buffer.ToArray().ValidResponses())
                {
                    Common.FuelPoint tfp = this.fuelPoints.Where(x => (x.Address == (int)response[3]) && x.Channel == (int)(response[10] - 32)).First();
                    type = (Response)response.Length;
                    //todo recalc CRC
                    #region responsetypeSW

                    switch(type)
                    {
                        case Response.SelfStatus:
                            //0  1  2  3  4  5  6  7  8  9   10  11 12 13 14  15  16 17 18   
                            //FD 02 01 01 00 00 80 00 0E 01 [21] 64 00 14 01 [02] 15 01 [00] 16 02 02 01 FC 30 31 36 30 FB

                            Common.Enumerators.FuelPointStatusEnum oldStatus = tfp.Status;
                            tfp.Status = evalStatus((int)response[15]);
                            tfp.DispenserStatus = tfp.Status;
                            if(tfp.Status == Common.Enumerators.FuelPointStatusEnum.Ready ||tfp.Status == Common.Enumerators.FuelPointStatusEnum.Work || tfp.Status == Common.Enumerators.FuelPointStatusEnum.Idle) tfp.QueryAuthorize = false;

                            if(tfp.Status != oldStatus && this.DispenserStatusChanged != null)
                            {
                                Common.FuelPointValues values = new Common.FuelPointValues();
                                if(tfp.Status != Common.Enumerators.FuelPointStatusEnum.Idle && tfp.Status != Common.Enumerators.FuelPointStatusEnum.Offline)
                                {
                                    tfp.ActiveNozzleIndex = 0;
                                    values.ActiveNozzle = 0;
                                }
                                else
                                {
                                    tfp.ActiveNozzleIndex = -1;
                                    values.ActiveNozzle = -1;
                                }

                                values.Status = tfp.Status;

                                if(tfp.Status == Common.Enumerators.FuelPointStatusEnum.Close)
                                {
                                    tfp.QueryAuthorize = false;
                                    values.Status = Common.Enumerators.FuelPointStatusEnum.Idle;
                                }

                                this.DispenserStatusChanged(this, new Common.FuelPointValuesArgs()
                                {
                                    CurrentFuelPoint = tfp,
                                    CurrentNozzleId = 1,
                                    Values = values
                                });

                            }
                            break;
                        case Response.SelfTransactionComplete:
                            decimal newAmountS = response.skip(30).takeToDecimal(3) / 100;
                            decimal newVolumeS = response.skip(40).takeToDecimal(3) / 100;

                            ///
                            if(tfp.DispensedVolume >= 0 && newVolumeS > 0)
                                tfp.DispensedVolume = newVolumeS;

                            if(tfp.DispensedAmount >= 0 && newAmountS > 0)
                                tfp.DispensedAmount = newAmountS;

                            CallEventDataChanged(tfp);
                            break;
                        case Response.Display:
                            decimal newVolume = response.skip(21).takeToDecimal(3) / 100;
                            decimal newAmount = response.skip(14).takeToDecimal(3) / 100;

                            ///
                            if(tfp.DispensedVolume >= 0 && newVolume > 0)
                                tfp.DispensedVolume = newVolume;

                            if(tfp.DispensedAmount >= 0 && newAmount > 0)
                                tfp.DispensedAmount = newAmount;

                            ////

                            int flag = (int)response.skip(13).takeToDecimal(1);
                            if(flag >= 6)
                            {
                                tfp.SetExtendedProperty("LastSalesId", (int)response.skip(33).takeToDecimal(2));
                            }
                            CallEventDataChanged(tfp);
                            break;
                        case Response.LastSalesId:
                            lock(tfp)
                            {
                                lock(tfp.Nozzles[0])
                                {
                                    tfp.Nozzles[0].TotalVolume = response.skip(15).takeToDecimal(6);
                                    tfp.Nozzles[0].TotalPrice = response.skip(24).takeToDecimal(6);
                                    tfp.SetExtendedProperty("LastSalesId", (int)response.skip(37).takeToDecimal(2));
                                }
                            }
                            Common.Enumerators.FuelPointStatusEnum tempstatus = tfp.Status;
                            tfp.Initialized = true;
                            if (this.TotalsRecieved != null)
                                this.TotalsRecieved(this, new Common.TotalsEventArgs(tfp, 1, tfp.Nozzles[0].TotalVolume, tfp.Nozzles[0].TotalPrice));
                            tfp.Status = tempstatus;
                            break;
                        case Response.Totals:
                            tfp.Nozzles[0].TotalVolume = response.skip(14).takeToDecimal(6);
                            tfp.Nozzles[0].TotalPrice = response.skip(23).takeToDecimal(6);
                            Common.Enumerators.FuelPointStatusEnum tempstatus1 = tfp.Status;

                            //if (tfp.GetExtendedProperty("LastTotalVolume") == null || (decimal)tfp.GetExtendedProperty("LastTotalVolume") > tfp.Nozzles[0].TotalVolume)
                            //{
                            //    tfp.SetExtendedProperty("LastTotalVolume", tfp.Nozzles[0].TotalVolume);
                            if (this.TotalsRecieved != null)
                                this.TotalsRecieved(this, new Common.TotalsEventArgs(tfp, 1, tfp.Nozzles[0].TotalVolume, tfp.Nozzles[0].TotalPrice));
                                
                            //}
                            tfp.Status = tempstatus1;
                            break;

                        case Response.InfoForId:
                            tfp.DispensedAmount = response.skip(21).takeToDecimal(3) / 100;
                            tfp.DispensedVolume = response.skip(28).takeToDecimal(3) / 100;
                            tfp.Nozzles[0].UnitPrice = response.skip(35).takeToDecimal(2) / 1000;
                            break;
                        case Response.SummaryForId:
                            tfp.DispensedAmount = response.skip(29).takeToDecimal(3) / 100;
                            tfp.DispensedVolume = response.skip(36).takeToDecimal(3) / 100;
                            CallEventDataChanged(tfp);
                            break;
                        case Response.Status:
                            tfp.SetExtendedProperty("statusFails", 0);
                            oldStatus = tfp.Status;
                            tfp.Status = evalStatus((int)response[13]);
                            tfp.DispenserStatus = tfp.Status;

                            foo.Status = tfp.Status;

                            if(tfp.Status == Common.Enumerators.FuelPointStatusEnum.Ready ||tfp.Status == Common.Enumerators.FuelPointStatusEnum.Work || tfp.Status == Common.Enumerators.FuelPointStatusEnum.Idle) tfp.QueryAuthorize = false;
                            if(tfp.Status != oldStatus && this.DispenserStatusChanged != null)
                            {
                                Common.FuelPointValues values = new Common.FuelPointValues();
                                if(tfp.Status != Common.Enumerators.FuelPointStatusEnum.Idle && tfp.Status != Common.Enumerators.FuelPointStatusEnum.Offline)
                                {
                                    tfp.ActiveNozzleIndex = 0;
                                    values.ActiveNozzle = 0;
                                }
                                else
                                {
                                    tfp.ActiveNozzleIndex = -1;
                                    values.ActiveNozzle = -1;
                                }

                                values.Status = tfp.Status;

                                if(tfp.Status == Common.Enumerators.FuelPointStatusEnum.Close)
                                {
                                    values.Status = Common.Enumerators.FuelPointStatusEnum.Idle;
                                }

                                this.DispenserStatusChanged(this, new Common.FuelPointValuesArgs()
                                {
                                    CurrentFuelPoint = tfp,
                                    CurrentNozzleId = 1,
                                    Values = values
                                });

                            }


                            break;
                        case Response.SetPrice:
                            tfp.QuerySetPrice = false;
                            //if(response[14] == 1) tfp.SetExtendedProperty("setPrice1", 1);
                            //else if(response[14] == 2) tfp.SetExtendedProperty("setPrice2", 1);

                            //if((int)tfp.GetExtendedProperty("setPrice1", 0) == 1 && (int)tfp.GetExtendedProperty("setPrice2", 0) == 1)
                            //{
                            //    tfp.QuerySetPrice = false;
                            //}
                            break;



                    }
                    #endregion

                }


                buffer.Clear();
            }

            catch
            {
                buffer.Clear();

                ////int fails = (int)fp.GetExtendedProperty("statusFails", 0);
                ////fails++;
                ////if(fails > 50)
                ////{
                ////    fp.Status = Common.Enumerators.FuelPointStatusEnum.Offline;
                ////    Common.FuelPointValues values = new Common.FuelPointValues();
                ////    values.Status = fp.Status;
                ////    this.DispenserStatusChanged(this, new Common.FuelPointValuesArgs()
                ////    {
                ////        CurrentFuelPoint = fp,
                ////        CurrentNozzleId = 1,
                ////        Values = values,
                ////    });
                ////}
                ////else
                ////{
                ////    fp.SetExtendedProperty("statusFails", fails);
                ////}

            }
        }
        private Common.Enumerators.FuelPointStatusEnum evalStatus(int flag)
        {
            switch(flag)
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
                case (9)://Reached Preset Amount
                    return Common.Enumerators.FuelPointStatusEnum.Work;
                default:
                    return Common.Enumerators.FuelPointStatusEnum.Offline;
            }
        }
        private void executeCommand(byte[] cmd, Common.FuelPoint fp, bool read)
        {

            byte[] fetchbuffer = CreateCmd(fp.Address);

            this.serialPort.Write(cmd, 0, cmd.Length);
            this.serialPort.Write(fetchbuffer, 0, fetchbuffer.Length);
            System.Threading.Thread.Sleep(70);
            var sw = Stopwatch.StartNew();
            while(this.serialPort.BytesToRead > 0)
            {

                System.Threading.Thread.Sleep(30);
                byte[] temp = new byte[this.serialPort.BytesToRead];
                this.serialPort.Read(temp, 0, temp.Length);

                this.buffer.AddRange(temp);
            }
            sw.Stop();
            Console.WriteLine("\n\n\n\n\n\nTime elapsed: {0} milliseconds", sw.ElapsedMilliseconds);
            foo.RX = this.buffer.ToArray();

            if (buffer.Count == 0 && DateTime.Now.Subtract(fp.LastValidResponse).TotalSeconds > 5)
            {
                if (this.DispenserOffline != null)
                    this.DispenserOffline(fp, new EventArgs());
                return;
            }

            //if (this.buffer.Count == 0 && DateTime.Now.Subtract(fp.LastValidResponse).TotalSeconds > 5)
            //{
            //    fp.Status = Common.Enumerators.FuelPointStatusEnum.Offline;
            //    if (this.DispenserStatusChanged != null)
            //    {
            //        Common.FuelPointValues values = new Common.FuelPointValues();
            //        values.Status = Common.Enumerators.FuelPointStatusEnum.Offline;
            //        this.DispenserStatusChanged(this, new Common.FuelPointValuesArgs()
            //        {
            //            CurrentFuelPoint = fp,
            //            CurrentNozzleId = 0,
            //            Values = values
            //        });
            //    }
            //}
            if (this.buffer.Count == 0)
                return;
            fp.LastValidResponse = DateTime.Now;

            evaluateBuffer(this.buffer);
           

        }
        private byte[] CreateCmd(int dispenserId)
        {
            byte[] Buffer = new byte[] { 0xFF, (byte)(dispenserId) };
            return Buffer;
        }

        private void CreateCmd(int dispenserId, int fuelPointChannel, int index, CommandType command)
        {
            int nzIndex = (int)Math.Pow(2, (index - 1));
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
                    Buffer = new byte[] { 0xFD, (byte)(dispenserId), 0x00, 0x02, 0x01, 0x00, 0x00, 0x00, 0x06, 0x02, (byte)(fuelPointChannel + 32), (byte)(16 + index), 0x14, 0x15, 0x16, 0xFC };
                    break;
                case CommandType.PostWork:
                    Buffer = new byte[] { 0xFD, (byte)(dispenserId), 0x00, 0x02, 0x01, 0x00, 0x40, 0x00, 0x04, 0x01, (byte)(fuelPointChannel + 32), 0xD8, 0x00, 0xFC };
                    break;
            }

            //if (System.IO.File.Exists(LogPath()))
            //    System.IO.File.AppendAllText(LogPath(), string.Format("DispenserID : {0}, Channel : {1}, NozzleIndex: {2}, Command: {3}", dispenserId, fuelPointChannel, nzIndex, BitConverter.ToString(Buffer)) + "\r\n");
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

        //private void CreateCmd(int dispenserId, int fuelPointChannel, CommandType command)
        //{   // inputs: dispenser id, fuelpoint channel, type of command.
        //    // return the desired command for that id and channel.
        //    // Setup of a dispenser with 2 fuelpoints. 
        //    // pump1: address1 = dispenser's address, channel = 1.
        //    // pump2: address2 = dispenser's address, channel = 2.

        //    byte[] Buffer = new byte[] { 0x00 };
        //    switch(command)
        //    {
        //        case CommandType.RequestStatus:
        //            Buffer = new byte[] { 0xFD, (byte)(dispenserId), 0x00, 0x02, 0x01, 0x00, 0x00, 0x00, 0x04, 0x01, (byte)(fuelPointChannel + 32), 0x14, 0x15, 0xFC };
        //            break;
        //        case CommandType.SendPrices1:
        //            throw new Exception("wrong format, use createCmd(int dispenserId, int fuelPointChannel, CommandTypeEnum command, int price) to setPrices");
        //        case CommandType.SendPrices2:
        //            throw new Exception("wrong format, use createCmd(int dispenserId, int fuelPointChannel, CommandTypeEnum command, int price) to setPrices");
        //        case CommandType.Authorize:
        //            Buffer = new byte[] { 0xFD, (byte)(dispenserId), 0x00, 0x02, 0x01, 0x00, 0x40, 0x00, 0x07, 0x01, (byte)(fuelPointChannel + 32), 0x19, 0x01, 0x01, 0x3E, 0x00, 0xFC };
        //            break;
        //        case CommandType.CloseNozzle:
        //            Buffer = new byte[] { 0xFD, (byte)(dispenserId), 0x00, 0x02, 0x01, 0x00, 0x40, 0x00, 0x04, 0x01, (byte)(fuelPointChannel + 32), 0x3D, 0x00, 0xFC };
        //            break;
        //        case CommandType.OpenNozzle:
        //            Buffer = new byte[] { 0xFD, (byte)(dispenserId), 0x00, 0x02, 0x01, 0x00, 0x40, 0x00, 0x04, 0x01, (byte)(fuelPointChannel + 32), 0x3C, 0x00, 0xFC };
        //            break;
        //        case CommandType.RequestTotals:
        //            Buffer = new byte[] { 0xFD, (byte)(dispenserId), 0x00, 0x02, 0x01, 0x00, 0x00, 0x00, 0x05, 0x02, (byte)(fuelPointChannel + 32), 0x11, 0x14, 0x15, 0xFC };
        //            break;
        //        case CommandType.RequestDisplayData://FD 01           00     02    01    00    00    00   05     01   21   22     23     1D   FC
        //            ///////////////////     FD      01                 00    02    01     00    00    00    05   01     21                             22    23    1D   FC 30 30 38 44 FB 
        //            Buffer = new byte[] { 0xFD, (byte)(dispenserId), 0x00, 0x02, 0x01, 0x00, 0x00, 0x00, 0x05, 0x01, (byte)(fuelPointChannel + 32), 0x22, 0x23, 0x1D, 0xFC };
        //            break;
        //        case CommandType.InitializeA: //FD     01             00    02    01    00    00    00   03     01     81                            04    FC 
        //            Buffer = new byte[] { 0xFD, (byte)(dispenserId), 0x00, 0x02, 0x01, 0x00, 0x00, 0x00, 0x03, 0x01, (byte)(fuelPointChannel + 128), 0x04, 0xFC };
        //            break;
        //        case CommandType.GlobalInitialize: //FD 01            00     02     01   00    01    00     08   01     01  02    05      04   28   29    2A   FC 
        //            ////////////          FD           01            00     02    01    00    00   00     05     02    21   11   01    07     FC    30   30    34   35  FB
        //            Buffer = new byte[] { 0xFD, (byte)(dispenserId), 0x00, 0x02, 0x01, 0x00, 0x01, 0x00, 0x08, 0x01, 0x01, 0x02, 0x05, 0x04, 0x28, 0x29, 0x2A, 0xFC };
        //            break;
        //        case CommandType.Unlock:
        //            ////////////          FD           01            00     02    01    00    00   00     05     02    21                           11     01    07     FC    30   30    34   35  FB
        //            Buffer = new byte[] { 0xFD, (byte)(dispenserId), 0x00, 0x02, 0x01, 0x00, 0x00, 0x00, 0x05, 0x02, (byte)(fuelPointChannel + 32), 0x11, 0x01, 0x07, 0xFC };
        //            break;
        //        case CommandType.GetLastSalesId:
        //            Buffer = new byte[] { 0xFD, (byte)(dispenserId), 0x00, 0x02, 0x01, 0x00, 0x00, 0x00, 0x06, 0x02, (byte)(fuelPointChannel + 32), 0x21, 0x14, 0x15, 0x16, 0xFC };
        //            break;

        //        case CommandType.PreAuthorize:  //FD       01         00     02    01    00     00   00   06     01      21                          1D     24   25   26    FC 
        //            Buffer = new byte[] { 0xFD, (byte)(dispenserId), 0x00, 0x02, 0x01, 0x00, 0x00, 0x00, 0x06, 0x01, (byte)(fuelPointChannel + 32), 0x1D, 0x24, 0x25, 0x26, 0xFC };
        //            break;
        //        case CommandType.PostWork:
        //            //                    FD      01                  00      02    01    00    40    00   04    01        21                          D8   00   FC
        //            Buffer = new byte[] { 0xFD, (byte)(dispenserId), 0x00, 0x02, 0x01, 0x00, 0x40, 0x00, 0x04, 0x01, (byte)(fuelPointChannel + 32), 0xD8, 0x00, 0xFC };
        //            break;
        //    }
        //    ReturnCmdWithCrc(Buffer);
        //}
        //private void CreateCmd(int dispenserId, int fuelPointChannel, CommandType command, int price, int product = 1)
        //{
        //    byte[] buffer = new byte[] { 0x3F };
        //    if(command == CommandType.SendPrices)
        //    {

        //        string sprice = price.ToString();
        //        for(int i = sprice.Length; i < 4; i++)
        //        {
        //            sprice = "0" + sprice;
        //        }
        //        int p1 = Convert.ToInt32((sprice.Substring(0, 2)));
        //        int p2 = Convert.ToInt32((sprice.Substring(2, 2)));



        //        if(superdh && fuelPointChannel == 2)
        //        {
        //            buffer = new byte[] { 0xFD, (byte)(dispenserId), 0x00, 0x02, 0x01, 0x00, 0x40, 0x00, 0x0D, 0x06, 0x61, 0x00, 0x00, 0x00, (byte)5, 0x11, 0x02, 0x04, 0x03, 0x00, (byte)ith(p1), (byte)ith(p2), 0xFC };

        //        }
        //        else
        //        {
        //            buffer = new byte[] { 0xFD, (byte)(dispenserId), 0x00, 0x02, 0x01, 0x00, 0x40, 0x00, 0x0D, 0x06, 0x61, 0x00, 0x00, 0x00, (byte)1, 0x11, 0x02, 0x04, 0x03, 0x00, (byte)ith(p1), (byte)ith(p2), 0xFC };

        //        }

        //        //buffer = new byte[] { 0xFD, (byte)(dispenserId), 0x00, 0x02, 0x01, 0x00, 0x40, 0x00, 0x0D, 0x06, 0x61, 0x00, 0x00, 0x00, (byte)product, (byte)(fuelPointChannel+16), 0x02, 0x04, 0x03, 0x00, (byte)ith(p1), (byte)ith(p2), 0xFC };
        //        ReturnCmdWithCrc(buffer);
        //    }
        //    else if(command == CommandType.SummaryForId)
        //    {
        //        string sprice = price.ToString();
        //        for(int i = sprice.Length; i < 4; i++)
        //        {
        //            sprice = "0" + sprice;
        //        }
        //        int p1 = Convert.ToInt32((sprice.Substring(0, 2)));
        //        int p2 = Convert.ToInt32((sprice.Substring(2, 2)));
        //        //                    FD         01            00       02 01 00 40 00 07 04 [21] 21 03 00 1E 00 FC 30 30 42 32 FB
        //        //:                   FD         01              00     02    01    00    40    00    07    04         21                       21      01              49         1E     00     FC
        //        buffer = new byte[] { 0xFD, (byte)(dispenserId), 0x00, 0x02, 0x01, 0x00, 0x40, 0x00, 0x07, 0x04, (byte)(fuelPointChannel + 32), 0x21, (byte)ith(p1), (byte)ith(p2), 0x1E, 0x00, 0xFC };
        //        ReturnCmdWithCrc(buffer);

        //    }
        //    else if(command == CommandType.InfoForId)
        //    {
        //        string sprice = price.ToString();
        //        for(int i = sprice.Length; i < 4; i++)
        //        {
        //            sprice = "0" + sprice;
        //        }
        //        int p1 = Convert.ToInt32((sprice.Substring(0, 2)));
        //        int p2 = Convert.ToInt32((sprice.Substring(2, 2)));
        //        // 
        //        //////////////////                    FD  01                      00    02    01    00   02    00    0C 04 [21] 21 03 00 01 05 06 07 08 0C 15 FC 30 30 39 37 FB
        //        //                    FD     01                  00     02     01   00    02    00     0C   04      [21]                        21      03             00           01    05     06   07     08    0C    15   FC 
        //        buffer = new byte[] { 0xFD, (byte)(dispenserId), 0x00, 0x02, 0x01, 0x00, 0x02, 0x00, 0x0C, 0x04, (byte)(fuelPointChannel + 32), 0x21, (byte)ith(p1), (byte)ith(p2), 0x01, 0x05, 0x06, 0x07, 0x08, 0x0C, 0x15, 0xFC };
        //        ReturnCmdWithCrc(buffer);
        //    }
        //    else if (command == CommandType.Preset)
        //    {
        //        string sprice = price.ToString();
        //        for (int i = sprice.Length; i < 4; i++)
        //        {
        //            sprice = "0" + sprice;
        //        }
        //        int p1 = Convert.ToInt32((sprice.Substring(0, 2)));
        //        int p2 = Convert.ToInt32((sprice.Substring(2, 2)));

        //        buffer = new byte[] { 0xFD, (byte)dispenserId, 0x00, 0x02, 0x01, 0x00, 0x40, 0x00, 0x0D, 0x01, (byte)(fuelPointChannel + 32), 0x1B, 0x04, 0x04, 0x00, (byte)ith(p1), (byte)ith(p2), 0x19, 0x01, 0x01, 0x3E, 0x00, 0xFC };
        //        ReturnCmdWithCrc(buffer);
        //    }
        //    else
        //    {
        //        throw new System.ArgumentException("you cannot useoverloeaded creatCMD() for CommandTypeEnum{0} ", "command");
        //    }
        //}
        private int ith(int num)
        {
            return 16 * (num / 10) + (num % 10);
        }// hex = integerToHex(int)
        private void ReturnCmdWithCrc(byte[] buffer)
        {   //input buffer    FD xx ... xx FC
            //return          FD xx ... xx FC crc FB
            if(buffer.Last() != 0xFC || buffer[0] != 0xFD)
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

                for(int j = 0; j < 2; j++)
                {
                    for(int i = 0; i < commandParms.Length; i++)
                    {
                        temp = Int32.Parse(commandParms[i][j].ToString(), System.Globalization.NumberStyles.HexNumber); //hex to int
                        if(j == 0)
                        {
                            sumL += Convert.ToInt32(temp);
                        }
                        else
                        {
                            sumR += Convert.ToInt32(temp);
                        }
                    }
                }

                for(int i = 1; i < sumR + 1; i++)
                {
                    crc0++;
                    if(crc0 == 16)
                    {
                        crc1++; crc0 = 0;
                    }
                }

                for(int i = 1; i < sumL + 1; i++)
                {
                    crc1++;
                    if(crc1 == 16)
                    {
                        crc2++; crc1 = 0;
                    }
                }

                //convert these intergers to Bytes
                if(crc0 < 10) crc0 += 48; else crc0 += 55;
                if(crc1 < 10) crc1 += 48; else crc1 += 55;
                if(crc2 < 10) crc2 += 48; else crc2 += 55;

                List<byte> cmdWithCrc = new List<byte>();
                cmdWithCrc.AddRange(buffer);
                cmdWithCrc.Add(0x30);
                cmdWithCrc.Add((byte)crc2);
                cmdWithCrc.Add((byte)crc1);
                cmdWithCrc.Add((byte)crc0);
                cmdWithCrc.Add(0xFB);
                // return cmdWithCrc.ToArray();
                this.cmd = cmdWithCrc.ToArray();

            }

        }
        private enum Response
        {
            Totals = 36,
            Status = 23,
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
            //RequestID,
            RequestDisplayData,
            RequestStatus,
            //Halt,
            Authorize,
            SendMainDisplayData,
            RequestTotals,
            Preset,
            //RequestActiveNozzle,
            //AcknowledgeDeactivatedNozzle,

            GetLastSalesId,

        }

    }

    #region Extensions
    public static class Extensions
    {

        public static byte[] skip(this byte[] value, int number)
        {
            int i = 0;
            byte[] buffer = new byte[value.Length - number + 1];
            for(int k = number; k < value.Length; k++)
            {
                buffer[i] = value[k];
                i++;
            }
            return buffer;
        }

        public static byte[] take(this byte[] value, int number)
        {

            byte[] buffer = new byte[number];
            for(int k = 0; k < number; k++)
            {
                buffer[k] = value[k];
            }
            return buffer;
        }

        public static decimal takeToDecimal(this byte[] value, int number)
        {
            return decimal.Parse(BitConverter.ToString(value.take(number)).Replace("-", ""));
        }
        //public static List<byte[]> ValidResponses(this byte[] value, int length)
        //{
        //    List<byte[]> responses = new List<byte[]>();
        //    byte StartByte = 0xFD;
        //    byte FinByte = 0xFB;
        //    byte[] result = new byte[] { };
        //    int l = value.Length;

        //    if(length > l)
        //    {
        //        throw new ArgumentException("missmatch", "length");
        //    }

        //    foreach(int index in value.indexesOfByte(StartByte))
        //    {
        //        if(length + index > l) continue;
        //        if(value[index + length - 1] == FinByte)
        //        {
        //            result = value.ToArray().Skip(index).Take(index + length).ToArray();
        //            responses.Add(result);
        //        }
        //    }
        //    Console.WriteLine(BitConverter.ToString(responses[0]));
        //    return responses;

        //}
        public static List<byte[]> ValidResponses(this byte[] value)
        {
            List<byte[]> responses = new List<byte[]>();
            byte StartByte = 0xFD;
            byte FinByte = 0xFB;
            byte[] result = new byte[] { };
            int l = value.Length;

            foreach(int index in value.indexesOfByte(StartByte))
            {
                for(int k = index; k < l; k++)
                {
                    if(value[k] == FinByte)
                    {
                        result = value.ToArray().Skip(index).Take(k - index + 1).ToArray();
                        responses.Add(result);
                        break;
                    }
                }
                continue;
            }
            //Console.WriteLine(BitConverter.ToString(responses[0]));
            return responses;

        }
        public static List<int> indexesOfByte(this byte[] value, byte target)
        {
            List<int> indexes = new List<int>();
            for(int i = 0; i < value.Length; i++)
                if(value[i] == target)
                    indexes.Add(i);
            return indexes;
        }
    #endregion

    }

}
