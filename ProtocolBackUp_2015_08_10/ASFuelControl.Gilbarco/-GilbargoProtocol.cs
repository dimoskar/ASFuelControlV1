using ASFuelControl.Common;
using ASFuelControl.Common.Enumerators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.Gilbargo
{
    public class GilbargoProtocol : Common.IFuelProtocol
    {
        public event EventHandler<Common.FuelPointValuesArgs> DataChanged;
        public event EventHandler<Common.TotalsEventArgs> TotalsRecieved;
        public event EventHandler<Common.FuelPointValuesArgs> DispenserStatusChanged;

        private List<Common.FuelPoint> fuelPoints = new List<Common.FuelPoint>();
        private System.IO.Ports.SerialPort serialPort = new System.IO.Ports.SerialPort();
        private System.Threading.Thread th;

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
            get { return this.serialPort.IsOpen; }
        }

        public string CommunicationPort { set; get; }

        public void Connect()
        {
            try
            {
                this.serialPort.PortName = this.CommunicationPort;
                //this.serialPort.DtrEnable = true;
                this.serialPort.BaudRate = 5787;
                this.serialPort.Parity = System.IO.Ports.Parity.Even;
                this.serialPort.StopBits = System.IO.Ports.StopBits.One;
                this.serialPort.DataBits = 8;
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
                            Common.Enumerators.FuelPointStatusEnum oldStatus = fp.Status;
                            if (fp.QuerySetPrice)
                            {
                                foreach (Common.Nozzle nz in fp.Nozzles)
                                {
                                    if (this.SetPrice(nz)) 
                                        nz.QuerySetPrice = false;
                                    System.Threading.Thread.Sleep(50);
                                }
                                if (fp.Nozzles.Where(n => n.QuerySetPrice).Count() == 0)
                                    fp.QuerySetPrice = false;
                                continue;
                            }
                            
                            int nozzleForTotals = fp.Nozzles.Where(n => n.QueryTotals).Count();
                            if(nozzleForTotals > 0)
                            {
                                foreach(Common.Nozzle nz in fp.Nozzles)
                                {
                                    if(nz.QueryTotals)
                                    {
                                        nz.QueryTotals = !this.GetTotals(nz);
                                        System.Threading.Thread.Sleep(50);

                                        if(!nz.QueryTotals)
                                        {
                                            if(fp.Nozzles[0].TotalVolume != nz.TotalVolume)
                                            {
                                                fp.Nozzles[0].TotalVolume = nz.TotalVolume;
                                                if(this.TotalsRecieved != null)
                                                    this.TotalsRecieved(this, new Common.TotalsEventArgs(fp, 1, nz.TotalVolume, nz.TotalPrice));
                                            }
                                    }

                                    }
                                }
                                continue;
                            }

                            if (fp.QueryAuthorize)
                            {
                                if (this.AuthorizeFuelPoint(fp))
                                    fp.QueryAuthorize = false;
                                continue;
                            }

                            
                            this.GetStatus(fp);

                            if(fp.Status != Common.Enumerators.FuelPointStatusEnum.Idle && fp.Status != Common.Enumerators.FuelPointStatusEnum.Offline)
                            {
                                fp.ActiveNozzle = fp.Nozzles[0];
                            }

                            if (fp.Status == Common.Enumerators.FuelPointStatusEnum.Work)
                            {  //Check this one.
                                //this.GetDisplay(fp.ActiveNozzle);
                                this.GetDisplay(fp.ActiveNozzle);
                                if(this.DataChanged != null)
                                {
                                    Common.FuelPointValues values = new Common.FuelPointValues();
                                    values.Status = fp.Status;
                                    this.DataChanged(this, new Common.FuelPointValuesArgs()
                                    {
                                        CurrentFuelPoint = fp,
                                        CurrentNozzleId = fp.ActiveNozzleIndex + 1,
                                        Values = values
                                    });
                                }
                                //this.GetDisplay(fp.ActiveNozzle);
                               // this.GetStatus(fp.ActiveNozzle.ParentFuelPoint);
                            }

                            
                            if (oldStatus != fp.Status && this.DispenserStatusChanged != null)
                            {
                                System.Threading.Thread.Sleep(50);
                                Common.FuelPointValues values = new Common.FuelPointValues();
                                values.ActiveNozzle = fp.ActiveNozzleIndex;
                                values.Status = fp.Status;
                                this.DispenserStatusChanged(this, new Common.FuelPointValuesArgs() { CurrentFuelPoint = fp, CurrentNozzleId = values.ActiveNozzle + 1, Values = values });
                            }
                            if(oldStatus == FuelPointStatusEnum.TransactionCompleted ||
                                oldStatus == FuelPointStatusEnum.Work && fp.Status== FuelPointStatusEnum.Idle)
                            {
                                fp.QueryTotals = true;

                            }
                        }
                        finally
                        {

                        }
                        System.Threading.Thread.Sleep(20);
                    }
                }
                catch
                {
                }
            }
        }

        #region commands

        private byte[] CMDGetStatus(Common.FuelPoint fp)
        {
            byte startByte = BitConverter.GetBytes(0 + fp.Address)[0];
            byte[] cmd = new byte[] { startByte };
            return cmd;
        }
        private Common.FuelPoint EvalGetStatus(Common.FuelPoint fp, byte[] response)
        {
            if (response.Length < 2)
                return fp;
            byte b1 = response[1];
            if (b1 >= 0x61 && b1 <= 0x6F)
                fp.Status = Common.Enumerators.FuelPointStatusEnum.Idle; //0x6x
            else if (b1 >= 0xA1 && b1 <= 0xBF)
                fp.Status = Common.Enumerators.FuelPointStatusEnum.Idle;//0xAx--0xBF
            else if (b1 >= 0x71 && b1 <= 0x7F)
            {
                fp.Status = Common.Enumerators.FuelPointStatusEnum.Nozzle;//0x7x
                fp.QueryAuthorize = true;
            }
            else if (b1 >= 0x81 && b1 <= 0x8F)
                fp.Status = Common.Enumerators.FuelPointStatusEnum.Ready;//0x8x
            else if (b1 >= 0x91 && b1 <= 0x9F)
                fp.Status = Common.Enumerators.FuelPointStatusEnum.Work;//0x9
            else if (b1 >= 0xC1 && b1 <= 0xCF)
                fp.Status = Common.Enumerators.FuelPointStatusEnum.TransactionStopped;
            //else if (b1 >= 0x01 && b1 <= 0x0F)
            //    return Common.Enumerators.FuelPointStatusEnum.TransactionStopped;
            else if (b1 >= 0xD1 && b1 <= 0xDF)
                fp.Status = Common.Enumerators.FuelPointStatusEnum.Listen; // Den yparxei to sygkekrimeno Enum
            Console.WriteLine(fp.Status);
            return fp;
        }
        public Common.FuelPoint GetStatus(Common.FuelPoint fp)
        {
            this.serialPort.DiscardInBuffer();
            byte[] buffer = CMDGetStatus(fp);
            this.serialPort.Write(buffer, 0, buffer.Length);
            int waiting = 0;
            while (this.serialPort.BytesToRead < (int)responseLength.status && waiting < 300)
            {
                System.Threading.Thread.Sleep(15); //15*4 = 70 [ NP 68ms response time]
                waiting += 10;
            }
            byte[] response = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(response, 0, response.Length);
            return EvalGetStatus(fp, response);
        }

        private bool SetFPtoListen(Common.Nozzle nozzle)
        {
            //GetStatus(nozzle.ParentFuelPoint);
            byte[] cmd = new byte[] { BitConverter.GetBytes(32 + nozzle.ParentFuelPoint.Address)[0] };
            this.serialPort.Write(cmd, 0, 1);
           int waiting = 0;
             
            while (this.serialPort.BytesToRead < (int)responseLength.listen && waiting <300)
            {
                System.Threading.Thread.Sleep(15); //15*4 = 70 [ NP 68ms response time]
                waiting += 10;
            }
            byte[] response = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(response, 0, response.Length);
            Console.WriteLine(BitConverter.ToString(response));
            if (response[1] >= 0xD0 && response[1] <= 0xDF) return true;
            else return false;
        }
        private byte[] CMDSetPrice(Common.Nozzle nozzle)
        {
            int unitprice = nozzle.UntiPriceInt;

            //Set To listen. try 5 times,  meanwhile check fp.status )
            bool isListen = false;
            int repeat = 0;
            while (isListen == false || repeat == 5)
            {
                repeat++;
                isListen = SetFPtoListen(nozzle);
            }
            if (isListen == false)
            {
                Console.WriteLine("SetPrice at: " + nozzle.ParentFuelPoint.Address + "[address] failed");
                throw new Exception("SetPrice failed");
            }

            byte[] price = new byte[4];
            int crc = 0;
            int digit;
            string priceString = unitprice.ToString();
            //ZeroPadding 1 ->0001 
            for (int i = priceString.Length; i < 4; i++)
            {
                priceString = "0" + priceString;
            }

            //Get digits sum
            for (int i = 0; i < priceString.Length; i++)
            {
                digit = (unitprice / (int)(Math.Pow(10, i))) % 10;
                crc += digit;
                price[i] = (byte)((digit + 224)); //Ex Ex Ex Ex
            }
            crc = (256 - crc);
            byte[] cmd = new byte[] { 0xFF, 0xE5, 0xF4, 0xF6, 0xE0, 0xF7, price[0], price[1], price[2], price[3], 0xFB, (byte)crc, 0xF0 };
            return cmd;

        }
        public bool SetPrice(Common.Nozzle nozzle)
        {
            try
            {
                this.serialPort.DiscardInBuffer();
                byte[] cmd = CMDSetPrice(nozzle);
                this.serialPort.Write(cmd, 0, cmd.Length);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("no listeners: {0}", e.Source);
                return false;
            }
        }

        private byte[] CMDGetDisplay(Common.Nozzle nozzle)
        {
            if (nozzle == null)
                return null;
            byte startByte = BitConverter.GetBytes(64 + nozzle.ParentFuelPoint.Address)[0];
            byte[] cmd = new byte[] { startByte };
            return cmd;
        }
        private void EvalGetDisplay(Common.Nozzle nozzle, byte[] buffer)
        {
            if (buffer.Length < 34)
                return;
            try
            {
                byte[] unitPrice = buffer.Skip(13).Take(4).ToArray();
                unitPrice = unitPrice.Reverse().ToArray();
                byte[] priceBuffer = buffer.Skip(25).Take(6).ToArray();
                priceBuffer = priceBuffer.Reverse().ToArray();
                byte[] volumeBuffer = buffer.Skip(18).Take(6).ToArray();
                volumeBuffer = volumeBuffer.Reverse().ToArray();
                string upString = BitConverter.ToString(unitPrice).Replace("-", "").Replace("E", "");// unitPrice[0].ToString() + unitPrice[1].ToString();
                string volString = BitConverter.ToString(volumeBuffer).Replace("-", "").Replace("E", ""); ;// volumeBuffer[0].ToString() + volumeBuffer[1].ToString() + volumeBuffer[2].ToString();
                string priceString = BitConverter.ToString(priceBuffer).Replace("-", "").Replace("E", ""); ;// priceBuffer[0].ToString() + priceBuffer[1].ToString() + priceBuffer[2].ToString();
                nozzle.UnitPrice = decimal.Parse(upString) / (decimal)Math.Pow(10, nozzle.ParentFuelPoint.UnitPriceDecimalPlaces);
                nozzle.UntiPriceInt = int.Parse(upString);
                nozzle.ParentFuelPoint.DispensedVolume = decimal.Parse(volString) / 1000;
                nozzle.ParentFuelPoint.DispensedAmount = decimal.Parse(priceString) / (decimal)Math.Pow(10, nozzle.ParentFuelPoint.DecimalPlaces);
                Console.WriteLine("Dispensed Volume : {0}, Amount : {1}", nozzle.ParentFuelPoint.DispensedVolume, nozzle.ParentFuelPoint.DispensedAmount);
                //nozzle.ParentFuelPoint.DispensedAmount = nozzle.LastTotalPrice;
                //nozzle.ParentFuelPoint.DispensedVolume = nozzle.LastTotalVolume;
            }
            catch
            {
            }
        }
        public void GetDisplay(Common.Nozzle nozzle)
        {
            this.serialPort.DiscardInBuffer();
            byte[] cmd = CMDGetDisplay(nozzle);
            this.serialPort.Write(cmd, 0, cmd.Length);
            int waiting = 0;
            while (this.serialPort.BytesToRead < (int)responseLength.display && waiting<300)
            {
                System.Threading.Thread.Sleep(15); //15*4 = 70 [ NP 68ms response time]
                waiting+=10;
            }

            byte[] response = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(response, 0, response.Length);
            EvalGetDisplay(nozzle, response);
        }

        private byte[] CMDAuthorizeFuelPoint(Common.FuelPoint fp)
        {
            byte[] cmd = new byte[] { BitConverter.GetBytes(16 + fp.Address)[0] };
            return cmd;
        }
        public bool AuthorizeFuelPoint(Common.FuelPoint fp)
        {
            byte[] cmd = CMDAuthorizeFuelPoint(fp);
            this.serialPort.Write(cmd, 0, cmd.Length);

            return true;
        }

        private byte[] CMDGetTotals(Common.Nozzle nozzle)
        {
            byte cmd = BitConverter.GetBytes(80 + nozzle.ParentFuelPoint.Address)[0];
            byte[] command = new byte[] { cmd };
            return command;
        }
        private bool EvalGetTotals(Common.Nozzle nozzle, byte[] response)
        {
            byte[] volumeBuffer = response.Skip(5).Take(8).ToArray();
            volumeBuffer = volumeBuffer.Reverse().ToArray();
            string volumeBufferStr = BitConverter.ToString(volumeBuffer, 0, volumeBuffer.Length).Replace("-", "").Replace("E", "");
            byte[] priceBuffer = response.Skip(14).Take(8).ToArray();
            priceBuffer = priceBuffer.Reverse().ToArray();
            string priceBufferStr = BitConverter.ToString(priceBuffer, 0, priceBuffer.Length).Replace("-", "").Replace("E", "");
            nozzle.TotalVolume = decimal.Parse(volumeBufferStr);
            nozzle.TotalPrice = decimal.Parse(priceBufferStr);
            return true;
        }
        public bool GetTotals(Common.Nozzle nozzle)
        {
            this.serialPort.DiscardInBuffer();
            byte[] cmd = CMDGetTotals(nozzle);
            this.serialPort.Write(cmd, 0, cmd.Length);
            int waiting = 0;
            while (this.serialPort.BytesToRead < (int)responseLength.totals &&  waiting < 300)
            {
                System.Threading.Thread.Sleep(15); //15*4 = 70 [ NP 68ms response time]
                waiting += 10;
            }
            byte[] response = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(response, 0, response.Length);
            Console.WriteLine(BitConverter.ToString(response));
            if(response.Length < 2)
                return false;
            else 
            return EvalGetTotals(nozzle, response);
        }

        private enum responseLength
        {
            status = 2,
            totals = 95,
            setprice = 2,
            display = 34,
            auth = 2,
            halt = 3,
            listen = 2
        }
        
        #endregion

    }
   
}
