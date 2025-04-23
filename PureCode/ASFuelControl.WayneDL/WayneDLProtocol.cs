using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ASFuelControl.Common;

namespace ASFuelControl.WayneDL
{
    public class WayneDLProtocol : Common.IFuelProtocol
    {
        #region public events

        public event EventHandler<Common.FuelPointValuesArgs> DataChanged;
        public event EventHandler<Common.TotalsEventArgs> TotalsRecieved;
        public event EventHandler<Common.FuelPointValuesArgs> DispenserStatusChanged;

        #endregion

        #region private variables

        private List<Common.FuelPoint> fuelPoints = new List<Common.FuelPoint>();
        private System.IO.Ports.SerialPort serialPort = new System.IO.Ports.SerialPort();
        private System.Threading.Thread th;

        #endregion

        #region public properties

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
                return this.serialPort.IsOpen;
            }
        }
        public string CommunicationPort
        {
            set;
            get;
        }

        #endregion

        #region public methods

        public void Connect()
        {
            try
            {
                this.serialPort.PortName = this.CommunicationPort;
                this.serialPort.Parity = System.IO.Ports.Parity.Odd;
                this.serialPort.StopBits = System.IO.Ports.StopBits.One;
                this.serialPort.BaudRate = 9600;
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
        }
        public void ClearFuelPoints()
        {
            this.fuelPoints.Clear();
        }
        public Common.DebugValues DebugStatusDialog(Common.FuelPoint fp)
        {
            foo = null;
            GetStatus(fp);
            foo.Status = fp.Status;
            return foo;
        }

        #endregion

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
                            if (fp.Status == Common.Enumerators.FuelPointStatusEnum.Auth)
                            {
                                this.Initialize(fp);
                                continue;
                            }
                            if (fp.QueryHalt)
                            {
                                if (this.Lock(fp))
                                {
                                    fp.QueryHalt = false;
                                    fp.Halted = true;
                                    continue;
                                }
                            }
                            if (fp.QueryResume)
                            {
                                if (fp.Status == Common.Enumerators.FuelPointStatusEnum.Close)
                                {
                                    if (this.UnLock(fp))
                                    {
                                        fp.QueryResume = false;
                                        fp.Halted = false;
                                    }
                                }
                            }

                            if (fp.QuerySetPrice)
                            {
                                foreach (Common.Nozzle nz in fp.Nozzles)
                                {
                                    if (this.SetPrice(nz, nz.UntiPriceInt))
                                        nz.QuerySetPrice = false;
                                    System.Threading.Thread.Sleep(50);
                                }
                                if (fp.Nozzles.Where(n => n.QuerySetPrice).Count() == 0)
                                    fp.QuerySetPrice = false;
                                continue;
                            }
                            int nozzleForTotals = fp.Nozzles.Where(n => n.QueryTotals).Count();
                            if (nozzleForTotals > 0)
                            {
                                foreach (Common.Nozzle nz in fp.Nozzles)
                                {
                                    if (nz.QueryTotals)
                                    {
                                        if (!this.GetTotals(nz))
                                            continue;
                                        System.Threading.Thread.Sleep(10);
                                        fp.Initialized = true;
                                        if (this.TotalsRecieved != null)
                                        {
                                            this.TotalsRecieved(this, new Common.TotalsEventArgs(fp, nz.Index, nz.TotalVolume, nz.TotalPrice));
                                        }
                                    }
                                }
                                continue;
                            }

                            if (fp.QueryAuthorize)
                            {
                                //if(SetPreset(fp.ActiveNozzle))
                                //{
                                if (this.AuthorizeFuelPoint(fp))
                                    fp.QueryAuthorize = false;
                                continue;
                                // }

                            }

                            if (fp.Status == Common.Enumerators.FuelPointStatusEnum.TransactionCompleted)
                            {
                                //this.Initialize(fp);
                                //this.Lock(fp);
                                //this.UnLock(fp);
                            }
                            Common.Enumerators.FuelPointStatusEnum oldStatus = fp.Status;
                            
                            this.GetStatus(fp);
                        }
                        finally
                        {
                            System.Threading.Thread.Sleep(150);
                        }

                    }
                }
                catch (Exception e)
                {
                    //System.IO.Directory.CreateDirectory(System.Environment.CurrentDirectory + "\\logs");
                    //System.IO.File.AppendAllText(System.Environment.CurrentDirectory + "\\logs" + "\\NuovoPignoneError.txt", "\n" + e.ToString()); 
                    System.Threading.Thread.Sleep(250);
                }
            }
        }

        #region Fuel Point Commands

        private byte[] ExecuteCommand(int responseLength, byte[] cmd)
        {
            this.serialPort.Write(cmd, 0, cmd.Length);
            int waiting = 0;
            while (this.serialPort.BytesToRead < responseLength + cmd.Length && waiting < 300)
            {
                System.Threading.Thread.Sleep(15); //15*4 = 70 [ NP 68ms response time]
                waiting += 20;
            }
            byte[] upResponse = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(upResponse, 0, this.serialPort.BytesToRead);
            if (upResponse.Length != responseLength + cmd.Length)
                return new byte[0];
            string str1 = BitConverter.ToString(cmd);
            string str2 = BitConverter.ToString(upResponse);
            if (str2.Contains(str1))
                return upResponse.ToList().Skip(cmd.Length).ToArray();
            return new byte[0];
        }

        private bool Initialize(FuelPoint fp)
        {
            byte NozzleIdAddress = BitConverter.GetBytes(8 * fp.Address)[0];
            int Checksum = (255 - (8 * fp.Address));
            byte[] send = new byte[] { 0x00, 0x00, NozzleIdAddress, (byte)Checksum, 0x87, 0x78, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0xFF };

            send = this.ExecuteCommand(13, send);
            if (send.Length < 12)
            {
                return false;
            }
            byte b1 = send[6];
            byte b2 = send[10];
            if(b1 == 0xC0 && b2 == 0x91)
                return false;
            return true;
        }

        private void GetStatus(FuelPoint fp)
        {

            byte NozzleIdAddress = BitConverter.GetBytes(1 + (8 * fp.Address))[0];
            int Checksum = (255 - (1 + (8 * fp.Address)));
            byte[] send = new byte[] { 0x00, 0x00, NozzleIdAddress, (byte)Checksum, 0xFF };

            send = this.ExecuteCommand(13, send);

            this.EvaluateStatus(fp, send);

            
        }

        private bool Lock(FuelPoint fp)
        {
            byte NozzleIdAddress = BitConverter.GetBytes(8 * fp.Address)[0];
            int Checksum = (255 - (8 * fp.Address));
            byte[] send = new byte[] { 0x00, 0x00, NozzleIdAddress, (byte)Checksum, 0x20, 0xDF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0xFF };

            send = this.ExecuteCommand(13, send);

            this.GetStatus(fp);

            if (fp.Status == Common.Enumerators.FuelPointStatusEnum.Close)
                return true;
            return false;
        }

        private bool UnLock(FuelPoint fp)
        {
            byte NozzleIdAddress = BitConverter.GetBytes(8 * fp.Address)[0];
            int Checksum = (255 - (8 * fp.Address));
            byte[] send = new byte[] { 0x00, 0x00, NozzleIdAddress, (byte)Checksum, 0x10, 0xEF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0xFF };

            send = this.ExecuteCommand(13, send);

            this.GetStatus(fp);

            if (fp.Status != Common.Enumerators.FuelPointStatusEnum.Close)
                return true;
            return false;
        }

        private bool GetTotals(Nozzle nz)
        {
            try
            {
                byte[] buffer1 = this.GetTotalsVolume1(nz);
                byte[] buffer2 = this.GetTotalsVolume2(nz);

                if (buffer1.Length < 11 || buffer2.Length < 11)
                    return false;
                byte b1 = buffer1[10];
                byte b2 = buffer1[8];
                byte b3 = buffer2[10];
                byte b4 = buffer2[8];
                byte[] buffer = new byte[] { b1, b2, b3, b4 };
                string val = BitConverter.ToString(buffer).Replace("-", "");
                //nz.QueryTotals = false;
                nz.TotalVolume = decimal.Parse(val);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private byte[] GetTotalsVolume1(Nozzle nz)
        {
            byte NozzleIdAddress = BitConverter.GetBytes(7 + (8 * nz.ParentFuelPoint.Address))[0];
            int Checksum = (255 - (7 + (8 * nz.ParentFuelPoint.Address)));
            byte[] send = new byte[] { 0x00, 0x00, NozzleIdAddress, (byte)Checksum, 0x02, 0xFD, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0xFF };

            send = this.ExecuteCommand(13, send);

            return send;
        }

        private byte[] GetTotalsVolume2(Nozzle nz)
        {
            byte NozzleIdAddress = BitConverter.GetBytes(7 + (8 * nz.ParentFuelPoint.Address))[0];
            int Checksum = (255 - (7 + (8 * nz.ParentFuelPoint.Address)));
            byte[] send = new byte[] { 0x00, 0x00, NozzleIdAddress, (byte)Checksum, 0x04, 0xFB, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0xFF };

            send = this.ExecuteCommand(13, send);

            return send;
        }

        private byte[] ClearDisplay(FuelPoint fp)
        {
            byte NozzleIdAddress = BitConverter.GetBytes(8 * fp.Address)[0];
            int Checksum = (255 - (8 * fp.Address));
            byte[] send = new byte[] { 0x00, 0x00, NozzleIdAddress, (byte)Checksum, 0x88, 0x77, 0x14, 0xEB, 0x00, 0xFF, 0x00, 0xFF, 0xFF };

            send = this.ExecuteCommand(13, send);

            return send;

        }

        private byte[] ResetStatus(FuelPoint fp)
        {
            byte NozzleIdAddress = BitConverter.GetBytes(8 * fp.Address)[0];
            int Checksum = (255 - (8 * fp.Address));
            byte[] send = new byte[] { 0x00, 0x00, NozzleIdAddress, (byte)Checksum, 0x00, 0xFF, 0x02, 0xFD, 0x00, 0xFF, 0x00, 0xFF, 0xFF };

            send = this.ExecuteCommand(13, send);

            return send;
        }


        private byte[] GetPrice(FuelPoint fp)
        {
            byte NozzleIdAddress = BitConverter.GetBytes(7 + (8 * fp.Address))[0];
            int Checksum = (255 - (7 + (8 * fp.Address)));
            byte[] send = new byte[] { 0x00, 0x00, NozzleIdAddress, (byte)Checksum, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0xFF };

            send = this.ExecuteCommand(13, send);

            return send;
        }

        private bool SetPrice(Nozzle nz, int price)
        {
            string priceValue = price.ToString("X4");
            //string price1 = priceValue.Substring(2);
            //string price2 = priceValue.Substring(0, 2);

            string val1 = priceValue.Substring(0,2);
            string val2 = priceValue.Substring(2, 2);

            byte b1 = (byte)int.Parse(val1, System.Globalization.NumberStyles.HexNumber);
            byte b2 = (byte)int.Parse(val2, System.Globalization.NumberStyles.HexNumber);

            byte nozzleIdAddress = BitConverter.GetBytes(7 + (8 * nz.ParentFuelPoint.Address))[0];
            byte[] send = new byte[] { 0x00, 0x00, nozzleIdAddress, (byte)(255 - (7 + (8 * nz.ParentFuelPoint.Address))), 0x01, 0xFE, 0x00, 0xFF, b2, (byte)(255 - b2), b1, (byte)(255 - b1), 0xFF };

            byte[] resp = this.ExecuteCommand(13, send);
            if(BitConverter.ToString(resp) == BitConverter.ToString(send))
                return true;
            return false;
        }

        private bool AuthorizeFuelPoint(FuelPoint fp)
        {
            try
            {
                byte nozzleIdAddress = BitConverter.GetBytes(8 * fp.Address)[0];
                int checksum = (255 - (8 * fp.Address));
                byte[] send = new byte[] { 0x00, 0x00, nozzleIdAddress, (byte)checksum, 0x8F, 0x70, 0x20, 0xDF, 0x00, 0xFF, 0x00, 0xFF, 0xFF };

                send = this.ExecuteCommand(0, send);

                return true;
            }
            catch
            {
                return false;
            }
        }


        #endregion

        #region Command Evaluation Methods

        private void EvaluateStatus(FuelPoint fp, byte[] response)
        {
            //07 -> IDLE
            //17 -> LOCKED
            //00 -> NOZZLE
            //88 -> WORK
            //8F -> TRANSACTION COMPLETED
            //0F -> Authorised Nozzle Closed
            //F0 -> *Authorised Nozzle -> Need Initialize Command to return Idle State
            try
            {
                if (response.Length == 0 && DateTime.Now.Subtract(fp.LastValidResponse).TotalSeconds > 5)
                {
                    fp.Status = Common.Enumerators.FuelPointStatusEnum.Offline;
                    if (this.DispenserStatusChanged != null)
                    {
                        Common.FuelPointValues values = new Common.FuelPointValues();
                        values.Status = Common.Enumerators.FuelPointStatusEnum.Offline;
                        this.DispenserStatusChanged(this, new Common.FuelPointValuesArgs()
                        {
                            CurrentFuelPoint = fp,
                            CurrentNozzleId = 0,
                            Values = values
                        });
                    }
                    return;
                }
                if (response.Length == 0)
                    return;
                fp.LastValidResponse = DateTime.Now;

                Common.Enumerators.FuelPointStatusEnum status = fp.Status;
                if (response[10] == (byte)0x07)
                {
                    status = Common.Enumerators.FuelPointStatusEnum.Idle;
                    fp.Nozzles[0].SetExtendedProperty("TransCompletedLast", null);
                }
                else if (response[10] == (byte)0x17)
                {
                    status = Common.Enumerators.FuelPointStatusEnum.Close;
                    if (fp.Status != Common.Enumerators.FuelPointStatusEnum.Work)
                    {
                        fp.Halted = true;
                        fp.QueryResume = true;
                    }

                }
                else if (response[10] == (byte)0x00)
                    status = Common.Enumerators.FuelPointStatusEnum.Nozzle;
                else if (response[10] == (byte)0x88)
                    status = Common.Enumerators.FuelPointStatusEnum.Work;
                else if (response[10] == (byte)0x8F)
                {
                    status = Common.Enumerators.FuelPointStatusEnum.TransactionCompleted;

                    DateTime? dt = (DateTime?)fp.Nozzles[0].GetExtendedProperty("TransCompletedLast");
                    if (dt.HasValue && DateTime.Now.Subtract(dt.Value).TotalSeconds > 10)
                    {
                        byte[] respBuffer = this.ClearDisplay(fp);
                        System.Threading.Thread.Sleep(150);
                        while (respBuffer == null || respBuffer.Length == 0)
                        {
                            respBuffer = this.ClearDisplay(fp);
                            System.Threading.Thread.Sleep(150);
                        }
                        respBuffer = this.ResetStatus(fp);

                    }
                    else if(!dt.HasValue)
                        fp.Nozzles[0].SetExtendedProperty("TransCompletedLast", DateTime.Now);

                }
                else if (response[10] == (byte)0xF0 || response[10] == (byte)0x0F)
                {
                    fp.Status = Common.Enumerators.FuelPointStatusEnum.Auth;

                }
                if (status != fp.Status && status != Common.Enumerators.FuelPointStatusEnum.Auth )
                {
                    fp.Status = status;
                    fp.DispenserStatus = status;
                    FuelPointValues vals = new FuelPointValues();
                    vals.Status = status;
                    if (status == Common.Enumerators.FuelPointStatusEnum.Idle || status == Common.Enumerators.FuelPointStatusEnum.Offline)
                    {
                        vals.ActiveNozzle = -1;
                        fp.ActiveNozzle = null;
                    }
                    else
                    {
                        fp.ActiveNozzle = fp.Nozzles[0];
                        vals.ActiveNozzle = 0;
                    }

                    if(this.DispenserStatusChanged != null)
                        this.DispenserStatusChanged(this, new FuelPointValuesArgs() { CurrentFuelPoint = fp, CurrentNozzleId = vals.ActiveNozzle + 1, Values = vals });
                }
            }
            catch
            {
                
            }
        }

        #endregion
    }
}
