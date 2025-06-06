﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.IO;
using System.Diagnostics;
using ASFuelControl.Common;
using ASFuelControl.Common.Enumerators;

namespace ASFuelControl.AK6_Dual
{
    public class AK6_DualProtocol : IFuelProtocol
    {
        #region Basics

        public event EventHandler<FuelPointValuesArgs> DataChanged;
        public event EventHandler<TotalsEventArgs> TotalsRecieved;
        public event EventHandler<FuelPointValuesArgs> DispenserStatusChanged;

        private List<FuelPoint> fuelPoints = new List<FuelPoint>();

        private System.IO.Ports.SerialPort serialPort = new System.IO.Ports.SerialPort();
        private System.Threading.Thread th;

        public DebugValues foo = new DebugValues();
        public FuelPoint[] FuelPoints
        {
            get
            {
                return this.fuelPoints.ToArray();
            }
            set
            {
                this.fuelPoints = new List<FuelPoint>(value);
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
                this.serialPort.DtrEnable = true;
                this.serialPort.BaudRate = 9600;
                this.serialPort.Open();
                this.th = new System.Threading.Thread(new System.Threading.ThreadStart(this.WorkFlowDispenser));
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
        public void AddFuelPoint(FuelPoint fp)
        {
            this.fuelPoints.Add(fp);
        }
        public void ClearFuelPoints()
        {
            this.fuelPoints.Clear();
        }
        public DebugValues DebugStatusDialog(FuelPoint fp)
        {
            foo = null;
            GetStatus(fp);
            foo.Status = fp.Status;
            return foo;
        }
        #endregion

        #region WorkFlowDispenser
        private void WorkFlowDispenser()
        {
            foreach (Common.FuelPoint fp in this.fuelPoints)
            {
                foreach (Nozzle nz in fp.Nozzles)
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
                            #region Halt
                            if (fp.QueryHalt)
                            {
                                if (this.Halt(fp))
                                    continue;
                            }
                            #endregion

                            #region Totals
                            int nozzleForTotals = fp.Nozzles.Where(n => n.QueryTotals).Count();
                            if (nozzleForTotals > 0)
                            {
                                foreach (Common.Nozzle nz in fp.Nozzles)
                                {
                                    if (nz.QueryTotals)
                                    {
                                        if (this.GetTotals(nz))
                                        {
                                            fp.Initialized = true;
                                            if (this.TotalsRecieved != null)
                                            {
                                                this.TotalsRecieved(this, new Common.TotalsEventArgs(fp, nz.Index, nz.TotalVolume, nz.TotalPrice));
                                            }
                                        }
                                    }
                                }
                                continue;
                            }
                            #endregion

                            #region SetPrice
                            if (fp.QuerySetPrice)
                            {
                                System.Threading.Thread.Sleep(10);
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
                            #endregion

                            #region Authorise
                            if (fp.QueryAuthorize)
                            {
                                if (this.AuthorizeFuelPoint(fp))
                                    fp.QueryAuthorize = false;
                                continue;

                                //SetPrice(fp.ActiveNozzle,fp.ActiveNozzle.UntiPriceInt);
                                //System.Threading.Thread.Sleep(20);
                                //GetStatus(fp);
                                //InitializeDispenser(fp);
                                //System.Threading.Thread.Sleep(20);
                                //AuthorizeFuelPoint(fp);
                            }
                            #endregion

                            #region Status
                            Common.Enumerators.FuelPointStatusEnum oldStatus = fp.Status;

                            this.GetStatus(fp);


                            if (oldStatus != fp.Status && this.DispenserStatusChanged != null)
                            {
                                Common.FuelPointValues values = new Common.FuelPointValues();
                                int currentNozzle = (int)fp.GetExtendedProperty("CurrentNozzle", -1);
                                if (fp.Status != Common.Enumerators.FuelPointStatusEnum.Idle && fp.Status != Common.Enumerators.FuelPointStatusEnum.Offline)
                                {
                                    fp.ActiveNozzleIndex = (int)fp.GetExtendedProperty("CurrentNozzle");
                                    values.ActiveNozzle = currentNozzle;
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
                            if (fp.Status == Common.Enumerators.FuelPointStatusEnum.Work)
                            {
                                fp.SetExtendedProperty("iNeedDisplay", true);
                            }
                            #endregion
                        }
                        finally
                        {
                            System.Threading.Thread.Sleep(40);
                        }
                    }
                }
                catch (Exception e)
                {
                }
            }
        }
        #endregion

        #region Initialize
        private byte[] InitializeCommand(int FuelPointID, int Nozzle)
        {
            byte[] Send = new byte[] { 0xAA, 0x55, 0x04, (byte)FuelPointID, 0x00, 0x02, 0x01, (byte)Nozzle, (byte)(3 + Nozzle) };
            return Send;
        }
        public void InitializeDispenser(FuelPoint fp)
        {
            byte[] numArray = InitializeCommand(fp.Address, fp.Channel);
            this.serialPort.Write(numArray, 0, (int)numArray.Length);
            int num = 0;
            while (this.serialPort.BytesToRead <= 32 && num < 300)
            {
                System.Threading.Thread.Sleep(10);
                num += 20;
            }
            byte[] numArray1 = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(numArray1, 0, this.serialPort.BytesToRead);
        }
        #endregion

        #region HaltPump
        private byte[] HaltCMD(FuelPoint fp)
        {
            int FuelPointID = fp.Address;
            int NozzleID = fp.Channel;
            byte[] Send = new byte[] { 0xAA, 0x55, 0x04, (byte)FuelPointID, 0x00, 0x02, 0x04, (byte)NozzleID, (byte)(6 + NozzleID) };
            return Send;
        }
        public bool Halt(FuelPoint fp)
        {
            byte[] numArray = HaltCMD(fp);
            this.serialPort.Write(numArray, 0, 9);
            int num = 0;
            while (this.serialPort.BytesToRead <= 32 && num < 300)
            {
                System.Threading.Thread.Sleep(40);
                num += 25;
            }
            byte[] numArray1 = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(numArray1, 0, this.serialPort.BytesToRead);
            int StatusBuf = 3 + (16 * (fp.Channel - 1));
            if (numArray1[StatusBuf] == 0x05)
            {
                return false;
            }
            else if (numArray1[StatusBuf] == 0x03)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion

        #region GetStatus
        int extendedProperty;
        private static byte[] StatusCommand(int FuelPointID)
        {
            //AA 55 04 01 00 02 07 01 0A
            //9 Bytes
            byte[] send = new byte[] { 0xAA, 0x55, 0x04, (byte)FuelPointID, 0x00, 0x02, 0x07, 0x01, 0x0A };
            return send;
        }
        private void GetStatus(Common.FuelPoint fp)
        {
            byte[] numArray = StatusCommand(fp.Address);
            this.serialPort.Write(numArray, 0, 9);
            int num = 0;
            while (this.serialPort.BytesToRead <= 32 && num < 300)
            {
                System.Threading.Thread.Sleep(40);
                num += 25;
            }
            byte[] numArray1 = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(numArray1, 0, this.serialPort.BytesToRead);
            this.evaluateStatus(fp, numArray1);
        }
        private void evaluateStatus(Common.FuelPoint fp, byte[] response)
        {
            try
            {
                FuelPointValues fuelPointValue = new FuelPointValues();
                FuelPointStatusEnum oldStatus = fp.Status;
                FuelPointStatusEnum newStatus = fp.Status;
                int currentNozzle = (int)fp.GetExtendedProperty("CurrentNozzle", -1);
                if (response.Length == 0 && DateTime.Now.Subtract(fp.LastValidResponse).TotalSeconds > 10)
                {
                    fp.Status = Common.Enumerators.FuelPointStatusEnum.Offline;
                }
                if (response.Length == 0)
                    return;
                fp.LastValidResponse = DateTime.Now;
                int StatusBuf = 3 + (16 * (fp.Channel - 1));
                if (response[StatusBuf] == 0x05)
                {
                    newStatus = FuelPointStatusEnum.Idle;
                    Halt(fp);
                }
                if (response[StatusBuf] == 0x02)
                {
                    InitializeDispenser(fp);
                }
                if (response[StatusBuf] == 0x03 && fp.Status == FuelPointStatusEnum.Work)
                {
                    evalDisplay(fp.ActiveNozzle, response);
                    newStatus = FuelPointStatusEnum.Idle;
                    if (currentNozzle >= 0)
                        fp.Nozzles[currentNozzle].QueryTotals = true;
                }
                if (response[StatusBuf] == 0x03 && fp.Status == FuelPointStatusEnum.Offline)
                {
                    newStatus = FuelPointStatusEnum.Idle;
                }
                if (response[StatusBuf] == 0x03 && fp.Status == FuelPointStatusEnum.Nozzle)
                {
                    newStatus = FuelPointStatusEnum.Idle;
                }
                if (response[StatusBuf] == 0x04 && fp.Status == FuelPointStatusEnum.Idle)
                {
                    this.extendedProperty = 0;
                    newStatus = FuelPointStatusEnum.Nozzle;
                    fp.SetExtendedProperty("CurrentNozzle", this.extendedProperty);
                }
                if (response[StatusBuf] == 0x06 && fp.Status == FuelPointStatusEnum.Nozzle)
                {
                    newStatus = FuelPointStatusEnum.Work;
                }
                if (response[StatusBuf] == 0x08)
                {
                    newStatus = FuelPointStatusEnum.Work;
                    evalDisplay(fp.ActiveNozzle, response);
                }
                fp.Status = newStatus;
                fp.DispenserStatus = fp.Status;
            }
            catch (Exception ex)
            {
                Logger(ex.ToString(), "EvaluateStatus");
            }
        }
        #endregion

        #region GetDisplay

        private Common.Nozzle evalDisplay(Common.Nozzle nozzle, byte[] response)
        {
            try
            {
                //Channel For MultiDispenser
                int ChannelBuf = (16 * (nozzle.ParentFuelPoint.Channel - 1));
                string[] parms = BitConverter.ToString(response).Split('-');
                string upString = parms[10 + ChannelBuf] + parms[11 + ChannelBuf] + parms[12 + ChannelBuf];
                string volString = parms[7 + ChannelBuf] + parms[8 + ChannelBuf] + parms[9 + ChannelBuf];
                nozzle.ParentFuelPoint.DispensedAmount = decimal.Parse(upString) / (decimal)Math.Pow(10, nozzle.ParentFuelPoint.AmountDecimalPlaces);
                nozzle.ParentFuelPoint.DispensedVolume = decimal.Parse(volString) / (decimal)Math.Pow(10, nozzle.ParentFuelPoint.VolumeDecimalPlaces);
                if (this.DataChanged != null)
                {
                    Common.FuelPointValues values = new Common.FuelPointValues();
                    values.CurrentSalePrice = nozzle.UnitPrice;
                    values.CurrentPriceTotal = nozzle.ParentFuelPoint.DispensedAmount;
                    values.CurrentVolume = nozzle.ParentFuelPoint.DispensedVolume;

                    this.DataChanged(this, new Common.FuelPointValuesArgs()
                    {
                        CurrentFuelPoint = nozzle.ParentFuelPoint,
                        CurrentNozzleId = nozzle.Index,
                        Values = values
                    });
                }

            }
            catch (Exception)
            {

            }
            return nozzle;
        }

        #endregion

        #region Authorise

        private byte[] AuthoriseCMD(Common.FuelPoint fp)
        {
            //AA 55 10 01 00 08 03 02 00 00 00 00 00 00 0D
            //15 Bytes
            int FPAddress = fp.Address;
            int NozzleID = fp.Channel;
            byte[] send = new byte[] { 0xAA, 0x55, 0x10, (byte)FPAddress, 0x00, 0X08, 0x03, (byte)NozzleID, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, (byte)(11 + NozzleID) };
            return send;
        }
        private bool AuthorizeFuelPoint(Common.FuelPoint fp)
        {
            int waiting = 0;
            byte[] buffer = AuthoriseCMD(fp);
            this.serialPort.Write(buffer, 0, 15);
            while (this.serialPort.BytesToRead <= 32 && waiting < 300)
            {
                System.Threading.Thread.Sleep(25);
                waiting += 20;
            }
            byte[] response = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(response, 0, this.serialPort.BytesToRead);
            int ChannelBuf = 3 + (16 * (fp.Channel - 1));
            if (response[ChannelBuf] == 0x06)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion

        #region Totals

        private static byte[] TotalsCMD(int FuelPointID, int Channel)
        {
            //AA 55 04 01 A1 02 0F 01 18
            byte[] send = new byte[] { 0xAA, 0x55, 0x04, (byte)FuelPointID, 0xA1, 0x02, 0x0F, (byte)Channel, (byte)(17 + Channel) };
            return send;
        }
        private bool GetTotals(Common.Nozzle nozzle)
        {
            int waiting = 0;
            byte[] cmd = TotalsCMD(nozzle.ParentFuelPoint.Address, nozzle.ParentFuelPoint.Channel);
            this.serialPort.Write(cmd, 0, cmd.Length);
            while (this.serialPort.BytesToRead <= 34 && waiting < 300)
            {
                System.Threading.Thread.Sleep(60);
                waiting += 20;
            }
            byte[] response = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(response, 0, this.serialPort.BytesToRead);
            return evalTotals(nozzle, response);
        }
        private bool evalTotals(Common.Nozzle nozzle, byte[] response)
        {
            if (response[0] == 0x22 && response.Length == 34)
            {
                string[] Vtotal = BitConverter.ToString(response).Split('-');
                string volume = Vtotal[3] + Vtotal[4] + Vtotal[5] + Vtotal[6] + Vtotal[7] + Vtotal[8];
                nozzle.TotalVolume = decimal.Parse(volume);
                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion

        #region SetPrice
        
        private byte[] SetPriceCMD(int fp, int NozzleA, int UnitPrice)
        {
            //AA 55 14 01 0 0C 08 01 12 24 00 00 00 00 00 00 00 00 0C
            if (UnitPrice > 9999)
            {
                throw new ArgumentException("max value 9999", "unitPrice");
            }
            List<byte> Command = new List<byte>();
            string str = UnitPrice.ToString();
            if (str.Length == 3) { str = "0" + str; }
            if (str.Length == 2) { str = "00" + str; }
            if (str.Length == 1) { str = "000" + str; }
            byte[] price = StringToByteArray(str);
            byte[] Multi = new byte[] { 0xAA, 0x55, 0x14, (byte)fp, 0x00 };
            byte[] Buffer = new byte[] { 0x0C, 0x08, (byte)NozzleA, price[0], price[1], 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            Command.AddRange(Multi);
            Command.AddRange(Buffer);
            byte CRC = Checksum(Buffer);
            Command.Add(CRC);
            return Command.ToArray();

        }

        private bool SetPrice(Nozzle noz, int unitPrice)
        {
            int waiting = 0;
            byte[] buf = this.SetPriceCMD(noz.ParentFuelPoint.Address, noz.ParentFuelPoint.Channel, noz.UntiPriceInt);
            this.serialPort.Write(buf, 0, (int)buf.Length);
            while (this.serialPort.BytesToRead <= 32 && waiting < 300)
            {
                System.Threading.Thread.Sleep(25);
                waiting += 25;
            }
            byte[] response = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(response, 0, this.serialPort.BytesToRead);
            if ((int)response.Length == 32)
                return true;
            else
                return false;
        }

        #endregion

        #region Tools

        public static byte Checksum(byte[] data)
        {
            byte sum = 0;
            unchecked
            {
                foreach (byte b in data)
                {
                    sum += b;
                }
            }
            return sum;
        }

        public byte[] StringToByteArray(string hex)
        {
            return (
                from x in Enumerable.Range(0, hex.Length)
                where x % 2 == 0
                select Convert.ToByte(hex.Substring(x, 2), 16)).ToArray<byte>();
        }

        public static void Logger(string ExceptionData, string VoidMethodName)
        {
            string fileName = "Logs/AK6_" + DateTime.Now.ToString("dd-MM-yyyy").ToString() + "_LOG.txt";
            using (StreamWriter writer = new StreamWriter(fileName, true, Encoding.UTF8))
            {
                writer.Write(":" + VoidMethodName + "\r\n" + DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss.fff") + " --> " + ExceptionData.ToString() + "\r\n\r\n");
            }
        }

        #endregion
    }
}