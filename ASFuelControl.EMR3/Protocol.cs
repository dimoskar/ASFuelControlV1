using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ASFuelControl.Common;
using ASFuelControl.Common.Enumerators;
using System.Globalization;
using System.IO;

namespace ASFuelControl.EMR3
{
    public class Protocol : Common.IFuelProtocol
    {
        #region Basics
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
                this.serialPort.Parity = System.IO.Ports.Parity.None;
                this.serialPort.BaudRate = 9600;
                this.serialPort.Open();
                this.th = new System.Threading.Thread(new System.Threading.ThreadStart(this.WorkFlow));
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

        #region Workflow Dispensers

        private void WorkFlow()
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

                                fp.QueryHalt = !this.HaltDispenser(fp);
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

                                if (this.AuthorizeDelivery(fp))
                                    fp.QueryAuthorize = false;
                                continue;
                            }
                            #endregion

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
                        }
                        finally
                        {
                            System.Threading.Thread.Sleep(200);
                        }
                    }
                }
                catch (Exception e)
                {
                }
            }
        }


        #endregion

        #region GetStatus
        private byte[] CreateCommand(string body, int address, string f1)
        {

            string bodyOfMessage = body;
            string field = f1;
            byte delimeterFlag = 0x7E;       //Ignore me [head]
            byte destinationAddress = (byte)address;  //ignore me [OBC]
            byte sourceAddress = 0xFF;       //meter 1   [address]

            byte[] buffer = new byte[] { };

            byte bodyCommand = System.Text.Encoding.ASCII.GetBytes(bodyOfMessage)[0];
            byte f = 0x00;
            if (field != "") { f = System.Text.Encoding.ASCII.GetBytes(field)[0]; }
            int sum = destinationAddress + sourceAddress + bodyCommand + f;
            byte crc = BitConverter.GetBytes(-sum)[0];
            if (field == "")
            {
                buffer = new byte[] { delimeterFlag, destinationAddress, sourceAddress, bodyCommand, crc, delimeterFlag };
            }
            else
            {
                buffer = new byte[] { delimeterFlag, destinationAddress, sourceAddress, bodyCommand, f, crc, delimeterFlag };
            }
            return buffer;
        }
        private byte[] CreateCommand2(string body, int address, int f1)
        {

            string bodyOfMessage = body;
            byte delimeterFlag = 0x7E;       //Ignore me [head]
            byte destinationAddress = (byte)address;  //ignore me [OBC]
            byte sourceAddress = 0xFF;       //meter 1   [address]

            byte[] buffer = new byte[] { };

            byte bodyCommand = System.Text.Encoding.ASCII.GetBytes(bodyOfMessage)[0];


            int sum = destinationAddress + sourceAddress + bodyCommand + f1;
            byte crc = BitConverter.GetBytes(-sum)[0];

            buffer = new byte[] { delimeterFlag, destinationAddress, sourceAddress, bodyCommand, (byte)f1, crc, delimeterFlag };

            return buffer;
        }
        int extendedProperty;

        public void GetStatus(Common.FuelPoint fp)
        {
            int waiting = 0;
            byte[] buffer = this.CreateCommand2("T", fp.Address, 3);
            this.serialPort.DiscardInBuffer();
            this.serialPort.DiscardOutBuffer();
            this.serialPort.Write(buffer, 0, buffer.Length);
            if (System.IO.File.Exists("EMR3.log"))
            {
                System.IO.File.AppendAllText("EMR3.log", "EVALSTATUS\t" + DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + "\t TX: " + BitConverter.ToString(buffer) + "\r\t FPSTATUS: " + fp.Status.ToString() + "\r\n");
            }
            while (this.serialPort.BytesToRead < 10 && waiting < 300)
            {
                System.Threading.Thread.Sleep(20);
                waiting += 20;
            }
            byte[] response = new byte[serialPort.BytesToRead];
            serialPort.Read(response, 0, response.Length);
            this.evaluateStatus(fp, response);
        }
        private void evaluateStatus(Common.FuelPoint fp, byte[] response)
        {
            FuelPointValues fuelPointValue = new FuelPointValues();
            FuelPointStatusEnum oldStatus = fp.Status;
            FuelPointStatusEnum newStatus = fp.Status;
            int currentNozzle = (int)fp.GetExtendedProperty("CurrentNozzle", -1);


            if (response.Length == 0 && DateTime.Now.Subtract(fp.LastValidResponse).TotalSeconds > 5)
            {
                newStatus = Common.Enumerators.FuelPointStatusEnum.Offline;
            }

            if (response.Length == 0)
                return;
            fp.LastValidResponse = DateTime.Now;

            //Eval Display
            if (response.Length == 9 && fp.Status == FuelPointStatusEnum.Work)
            {
                this.evalDisplay(fp.ActiveNozzle);
            }

            if (response.Length == 9 && response[6] == 0x40 && fp.Status == FuelPointStatusEnum.Work)
            {

                newStatus = FuelPointStatusEnum.Idle;
                //if (currentNozzle >= 0)
                //    fp.Nozzles[currentNozzle].QueryTotals = true;
                //else
                //{
                //    foreach (Nozzle nz in fp.Nozzles)
                //        nz.QueryTotals = true;
                //}
            }
            if (response.Length == 9 && response[6] == 0x40 && fp.Status == FuelPointStatusEnum.Nozzle)
            {
                newStatus = FuelPointStatusEnum.Idle;
            }

            if (response.Length == 9 && response[6] == 0xC0 && fp.Status != FuelPointStatusEnum.Idle)
            {
                newStatus = FuelPointStatusEnum.Idle;
                //if (currentNozzle >= 0)
                //    fp.Nozzles[currentNozzle].QueryTotals = true;
            }
            if (response.Length == 9 && response[6] == 0xC0)
            {
                newStatus = FuelPointStatusEnum.Idle;
            }
            if (response.Length == 9 && response[6] == 0x40 && fp.Status == FuelPointStatusEnum.Offline)
            {
                newStatus = FuelPointStatusEnum.Idle;
                fp.SetExtendedProperty("CurrentNozzle", -1);
            }

            if (response.Length == 9 && response[6] == 0x04 && fp.Status == FuelPointStatusEnum.Idle)
            {
                this.extendedProperty = 0;
                newStatus = FuelPointStatusEnum.Nozzle;
                fp.SetExtendedProperty("CurrentNozzle", this.extendedProperty);
            }
            if (response.Length == 9 && response[6] == 0x86 && fp.Status == FuelPointStatusEnum.Nozzle)
            {
                newStatus = FuelPointStatusEnum.Work;
            }
            if (response.Length == 9 && response[6] == 0x06 && fp.Status == FuelPointStatusEnum.Nozzle)
            {
                newStatus = FuelPointStatusEnum.Work;
            }

            if (System.IO.File.Exists("EMR3.log"))
            {
                System.IO.File.AppendAllText("EMR3.log", "EVALSTATUS\t" + DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + "\t RX: " + BitConverter.ToString(response) + "\t FPSTATUS: " + fp.Status.ToString() + "\r\n");
            }

            fp.Status = newStatus;
            fp.DispenserStatus = fp.Status;
        }

        #endregion

        #region GetDisplay
        private Common.Nozzle evalDisplay(Common.Nozzle nozzle)
        {
            try
            {
                int waiting = 0;
                byte[] buffer = this.CreateCommand("G", nozzle.ParentFuelPoint.Address, "K");
                this.serialPort.DiscardInBuffer();
                this.serialPort.DiscardOutBuffer();
                this.serialPort.Write(buffer, 0, 7);
                if (System.IO.File.Exists("EMR3.log"))
                {
                    System.IO.File.AppendAllText("EMR3.log", "GET DISPLAY\t" + DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + "\t TX: " + BitConverter.ToString(buffer) + "\t FPSTATUS: " + nozzle.ParentFuelPoint.Status.ToString() + "\r\n");
                }
                while (this.serialPort.BytesToRead < 18 && waiting < 300)
                {
                    System.Threading.Thread.Sleep(20);
                    waiting += 20;
                }
                byte[] responsed = new byte[serialPort.BytesToRead];
                this.serialPort.Read(responsed, 0, responsed.Length);
                decimal volume = this.Hex64toDecimal(responsed.Skip(5).Take(8).Reverse().ToArray());

                if (System.IO.File.Exists("EMR3.log"))
                {
                    System.IO.File.AppendAllText("EMR3.log", "GET DISPLAY\t" + DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + "\t RX: " + volume.ToString("N2") + "\t FPSTATUS: " + nozzle.ParentFuelPoint.Status.ToString() + "\r\n");
                }

                if (volume < 0)
                {

                }
                else
                {
                    nozzle.ParentFuelPoint.DispensedAmount = (volume * nozzle.UnitPrice);
                    nozzle.ParentFuelPoint.DispensedVolume = volume;

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

            }
            catch (Exception)
            {

            }
            return nozzle;
        }

        #endregion

        #region GetTotals
        public bool GetTotals(Common.Nozzle nozzle)
        {
            try
            {
                int waiting = 0;
                byte[] buffer = this.CreateCommand("G", nozzle.ParentFuelPoint.Address, "l");
                this.serialPort.DiscardInBuffer();
                this.serialPort.DiscardOutBuffer();
                this.serialPort.Write(buffer, 0, buffer.Length);
                if (System.IO.File.Exists("EMR3.log"))
                {
                    System.IO.File.AppendAllText("EMR3.log", "GET TOTALS\t" + DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + "\t TX: " + BitConverter.ToString(buffer) + "\t FPSTATUS: " + nozzle.ParentFuelPoint.Status.ToString() + "\r\n");
                }
                while (this.serialPort.BytesToRead < 20 && waiting < 300)
                {
                    System.Threading.Thread.Sleep(50);
                    waiting += 20;
                }
                byte[] response = new byte[serialPort.BytesToRead];
                this.serialPort.Read(response, 0, this.serialPort.BytesToRead);
                return evalTotals(nozzle, response);
            }
            catch
            {
                return false;
            }
        }
        private bool evalTotals(Common.Nozzle nozzle, byte[] response)
        {
            string VolumeAscii = System.Text.Encoding.ASCII.GetString(response.Skip(5).Take(9).ToArray());
            VolumeAscii = VolumeAscii.Replace(",", null);
            decimal volume = decimal.Parse(VolumeAscii);
            nozzle.TotalVolume = volume * 10;

            if (System.IO.File.Exists("EMR3.log"))
            {
                System.IO.File.AppendAllText("EMR3.log", "GET TOTALS\t" + DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + "\t RX: " + VolumeAscii + "\t FPSTATUS: " + nozzle.ParentFuelPoint.Status.ToString() + "\r\n");
            }

            return true;
        }
        #endregion

        #region Authorise
        private byte[] CreateAuthorizeCommand(byte address)
        {
            string bodyOfMessage = "O";
            byte delimeterFlag = 0x7E;       //Ignore me [head]
            byte destinationAddress = address;  //ignore me [OBC]
            byte sourceAddress = 0xFF;       //meter 1   [address]
            byte[] buffer = new byte[] { };
            byte bodyCommand = System.Text.Encoding.ASCII.GetBytes(bodyOfMessage)[0];
            int sum = destinationAddress + sourceAddress + bodyCommand + (byte)6 + 1;
            byte crc = BitConverter.GetBytes(-sum)[0];
            buffer = new byte[] { delimeterFlag, destinationAddress, sourceAddress, bodyCommand, 6, 1, crc, delimeterFlag };
            return buffer;
        }
        private bool AuthorizeDelivery(Common.FuelPoint fp)
        {
            byte[] buffer = this.CreateAuthorizeCommand((byte)fp.Address);
            this.serialPort.DiscardInBuffer();
            this.serialPort.DiscardOutBuffer();
            this.serialPort.Write(buffer, 0, buffer.Length);
            DateTime dt = DateTime.Now;
            if (System.IO.File.Exists("EMR3.log"))
            {
                System.IO.File.AppendAllText("EMR3.log", "Authorise TX\t" + DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + "\t TX: " + BitConverter.ToString(buffer) + "\r\t FPSTATUS: " + fp.Status.ToString() + "\r\n");
            }
            while (serialPort.BytesToRead < 7)
            {
                if (DateTime.Now.Subtract(dt).TotalMilliseconds > 200)
                    break;
            }

            if (serialPort.BytesToRead < 7)
                return false;
            byte[] response = new byte[serialPort.BytesToRead];
            serialPort.Read(response, 0, response.Length);
            if (System.IO.File.Exists("EMR3.log"))
            {
                System.IO.File.AppendAllText("EMR3.log", "Authorise RX\t" + DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + "\t RX: " + BitConverter.ToString(response) + "\r\t FPSTATUS: " + fp.Status.ToString() + "\r\n");
            }
            if (response[3] != (byte)('A') && response[3] != 0)
                return false;
            return true;
        }
        #endregion

        #region SetPrice

        public bool SetPrice(Common.Nozzle nozzle, int unitPrice)
        {

            return true;

        }

        #endregion

        #region Halt

        private bool HaltDispenser(Common.FuelPoint fp)
        {
            try
            {
                byte[] buffer = this.CreateCommand2("O", (int)fp.Address, 3);
                this.serialPort.DiscardInBuffer();
                this.serialPort.DiscardOutBuffer();
                this.serialPort.Write(buffer, 0, buffer.Length);
                if (System.IO.File.Exists("EMR3.log"))
                {
                    System.IO.File.AppendAllText("EMR3.log", "Halt TX \t" + DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + "\t TX: " + BitConverter.ToString(buffer) + "\r\t FPSTATUS: " + fp.Status.ToString() + "\r\n");
                }
                DateTime dt = DateTime.Now;
                while (serialPort.BytesToRead < 1)
                {
                    if (DateTime.Now.Subtract(dt).TotalMilliseconds > 200)
                        break;
                }
                if (serialPort.BytesToRead < 1)
                    return false;
                byte[] response = new byte[serialPort.BytesToRead];
                serialPort.Read(response, 0, response.Length);
                if (System.IO.File.Exists("EMR3.log"))
                {
                    System.IO.File.AppendAllText("EMR3.log", "Halt RX\t" + DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + "\t RX: " + BitConverter.ToString(response) + "\r\t FPSTATUS: " + fp.Status.ToString() + "\r\n");
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        #endregion

        #region Tools

        private decimal Hex64toDecimal(byte[] buffer)
        {
            try
            {
                string Hex32Input = BitConverter.ToString(buffer).Replace("-", "");
                double doubleout = 0;
                decimal decimalout = 0;
                UInt64 bigendian;
                bool success = UInt64.TryParse(Hex32Input,
                    System.Globalization.NumberStyles.HexNumber, null, out bigendian);
                if (success)
                {
                    double fractionDivide = Math.Pow(2, 52);

                    int sign = (bigendian & 0x8000000000000000) == 0 ? 1 : -1;
                    Int64 exponent = ((Int64)(bigendian & 0x7FF0000000000000) >> 52) - (Int64)1023;
                    UInt64 fraction = (bigendian & 0x00000FFFFFFFFFFFFF);
                    if (fraction == 0)
                        doubleout = sign * Math.Pow(2, exponent);
                    else
                        doubleout = sign * (1 + (fraction / fractionDivide)) * Math.Pow(2, exponent);

                    Console.WriteLine(doubleout);

                    decimalout = Convert.ToDecimal(doubleout);

                }
                return decimalout;
            }
            catch (Exception ex)
            {
                return -1;
            }
        }

        public static void Logger(string FileNameToSave, string Error_Recieve, string VoidMethodName)
        {
            string fileName = "Logs/EMR3_" + FileNameToSave + "_LOG.txt";
            using (StreamWriter writer = new StreamWriter(fileName, true, Encoding.UTF8))
            {
                writer.Write("-->" + VoidMethodName + "  " + DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss.fff") + "   <--- \r\n\r\n" + Error_Recieve.ToString() + "\r\n\r\n");
            }
        }
        #endregion

    }
}
