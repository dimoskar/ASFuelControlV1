using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.EMR3
{
    public class Protocol :Common.IFuelProtocol
    {
        public Common.DebugValues DebugStatusDialog(Common.FuelPoint fp) { throw new NotImplementedException(); }
        private System.IO.Ports.SerialPort serialPort = new System.IO.Ports.SerialPort();
        private System.Threading.Thread th;
        private List<Common.FuelPoint> fuelPoints = new List<Common.FuelPoint>();

        public event EventHandler<Common.FuelPointValuesArgs> DataChanged;
        public event EventHandler<Common.TotalsEventArgs> TotalsRecieved;
        public event EventHandler<Common.FuelPointValuesArgs> DispenserStatusChanged;

        public bool IsConnected
        {
            get { return this.serialPort.IsOpen; }
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
                th = new System.Threading.Thread(new System.Threading.ThreadStart(this.ThreadRun));
                th.Start();
            }
            catch { }
        }

        public void Disconnect()
        {
            if (this.IsConnected)
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

        private byte[] CreateCommand(string body, byte address, byte f1)
        {
            string bodyOfMessage = body;

            byte delimeterFlag = 0x7E;       //Ignore me [head]
            byte destinationAddress = address;  //ignore me [OBC]
            byte sourceAddress = 0xFF;       //meter 1   [address]

            byte[] buffer = new byte[] { };

            byte bodyCommand = System.Text.Encoding.ASCII.GetBytes(bodyOfMessage)[0];

            int sum = destinationAddress + sourceAddress + bodyCommand + f1;
            byte crc = BitConverter.GetBytes(-sum)[0];

            buffer = new byte[] { delimeterFlag, destinationAddress, sourceAddress, bodyCommand, f1, crc, delimeterFlag };

            return buffer;
        }

        private byte[] CreateCommand(string body, byte address, string f1)
        {
            string bodyOfMessage = body;
            string field = f1;
            byte delimeterFlag = 0x7E;       //Ignore me [head]
            byte destinationAddress = address;  //ignore me [OBC]
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

        private byte[] CreatePriceCommand(string body, byte addres, byte f1, decimal price)
        {
            return new byte[0];
        }

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

        private decimal Hex32toDecimal(byte[] buffer)
        {
            string Hex32Input = BitConverter.ToString(buffer).Replace("-", "");
            double doubleout = 0;
            decimal decimalout = 0;
            //string doublestr = "";
            UInt64 bigendian;
            bool success = UInt64.TryParse(Hex32Input,
                System.Globalization.NumberStyles.HexNumber, null, out bigendian);
            if (success)
            {
                double fractionDivide = Math.Pow(2, 23);

                int sign = (bigendian & 0x80000000) == 0 ? 1 : -1;
                Int64 exponent = ((Int64)(bigendian & 0x7F800000) >> 23) - (Int64)127;
                UInt64 fraction = (bigendian & 0x007FFFFF);
                if (fraction == 0)
                    doubleout = sign * Math.Pow(2, exponent);
                else
                    doubleout = sign * (1 + (fraction / fractionDivide)) * Math.Pow(2, exponent);
                decimalout = Convert.ToDecimal(doubleout);
            }
            return decimalout;
        }

        private decimal Hex64toDecimal(byte[] buffer)
        {
            try
            {
                string Hex32Input = BitConverter.ToString(buffer).Replace("-", "");
                double doubleout = 0;
                decimal decimalout = 0;
                //string doublestr = "";
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

        private EMRStateEnum EvalEmrState(byte s, EMRStateEnum curState)
        {
            if ((int)s == 0) { return EMRStateEnum.PRE_DELIVERY_STATE; }
            else if ((int)s == 1) { return EMRStateEnum.KEY_TIMEOUT_STATE; }
            else if ((int)s == 2) { return EMRStateEnum.DELIVERY_STATE; }
            else if ((int)s == 3) { return EMRStateEnum.FINISH_STATE; }
            else if ((int)s == 4) { return EMRStateEnum.POPUP_STATE; } //POPUP STATE ?
            else if ((int)s == 5) { return EMRStateEnum.DISPLAY_TEST_STATE; }
            else return curState;
        }

        private DeliveryStatusEnum EvalDeliveryStatus(byte[] buffer, DeliveryStatusEnum curStatus)
        {
            UInt64 bigendian;
            string bufferHex = BitConverter.ToString(buffer).Replace("-", "");
            bool success = UInt64.TryParse(bufferHex,
            System.Globalization.NumberStyles.HexNumber, null, out bigendian);
            if (success)
            {
                DeliveryStatusEnum status = (DeliveryStatusEnum)bigendian;
                return status;
            }
            return curStatus;
        }

        private EMRStateEnum GetCurrentState(Common.FuelPoint fp)
        {
            byte[] buffer = this.CreateCommand("T", (byte)fp.Address, 1);
            this.serialPort.DiscardInBuffer();
            this.serialPort.DiscardOutBuffer();
            this.serialPort.Write(buffer, 0, buffer.Length);
            DateTime dt = DateTime.Now;
            while (serialPort.BytesToRead < 8)
            {
                if (DateTime.Now.Subtract(dt).TotalMilliseconds > 200)
                    break;
            }
            EMRStateEnum curState = (EMRStateEnum)fp.GetExtendedProperty("EMRState", EMRStateEnum.DELIVERY_STATE);
            if (serialPort.BytesToRead < 8)
                return curState;
            byte[] response = new byte[serialPort.BytesToRead];
            serialPort.Read(response, 0, response.Length);
            return this.EvalEmrState(response[5], curState);
        }

        private bool GetDeliveryData(Common.FuelPoint fp)
        {
            byte[] buffer = this.CreateCommand("G", (byte)fp.Address, "g");
            this.serialPort.DiscardInBuffer();
            this.serialPort.DiscardOutBuffer();
            this.serialPort.Write(buffer, 0, buffer.Length);
            DateTime dt = DateTime.Now;
            while (serialPort.BytesToRead < 15)
            {
                if (DateTime.Now.Subtract(dt).TotalMilliseconds > 200)
                    break;
            }
            if (serialPort.BytesToRead < 15)
                return false;
            byte[] response = new byte[serialPort.BytesToRead];
            serialPort.Read(response, 0, response.Length);
            decimal volume = this.Hex64toDecimal(response.Skip(5).Take(8).Reverse().ToArray());
            fp.DispensedVolume = volume;
            return true;
        }

        private DeliveryStatusEnum GetDeliveryStatus(Common.FuelPoint fp)
        {
            byte[] buffer = this.CreateCommand("T", (byte)fp.Address, 3);
            this.serialPort.DiscardInBuffer();
            this.serialPort.DiscardOutBuffer();
            this.serialPort.Write(buffer, 0, buffer.Length);
            DateTime dt = DateTime.Now;
            while (serialPort.BytesToRead < 8)
            {
                if (DateTime.Now.Subtract(dt).TotalMilliseconds > 200)
                    break;
            }
            DeliveryStatusEnum curState = (DeliveryStatusEnum)fp.GetExtendedProperty("DeliveryStatus", DeliveryStatusEnum.ATCError);
            if (serialPort.BytesToRead < 8)
                return curState;
            byte[] response = new byte[serialPort.BytesToRead];
            serialPort.Read(response, 0, response.Length);
            return this.EvalDeliveryStatus(new byte[] { response[6], response[5] }, curState);
        }

        private bool GetTotalsData(Common.FuelPoint fp)
        {
            try
            {
                byte[] buffer = this.CreateCommand("G", (byte)fp.Address, "j");
                this.serialPort.DiscardInBuffer();
                this.serialPort.DiscardOutBuffer();
                this.serialPort.Write(buffer, 0, buffer.Length);
                DateTime dt = DateTime.Now;
                System.Threading.Thread.Sleep(100);
                while (serialPort.BytesToRead < 15)
                {
                    if (DateTime.Now.Subtract(dt).TotalMilliseconds > 200)
                        break;
                }
                if (serialPort.BytesToRead < 15)
                    return false;
                byte[] response = new byte[serialPort.BytesToRead];
                serialPort.Read(response, 0, response.Length);
                decimal volume = this.Hex64toDecimal(response.Skip(5).Take(8).Reverse().ToArray());
                if (volume <= 0)
                    return false;
                if (fp.Nozzles[0].TotalVolume > 0 && volume <= 0)
                    return false;
                fp.Nozzles[0].TotalVolume = volume * 100;
                Console.WriteLine("TOTAL VOLUME :" + fp.Nozzles[0].TotalVolume.ToString("N0"));
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private bool HaltDispenser(Common.FuelPoint fp)
        {
            try
            {
                byte[] buffer = this.CreateCommand("O", (byte)fp.Address, "3");
                this.serialPort.DiscardInBuffer();
                this.serialPort.DiscardOutBuffer();
                this.serialPort.Write(buffer, 0, buffer.Length);
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
                
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private bool SetPrice(Common.FuelPoint fp)
        {
            byte[] buffer = this.CreatePriceCommand("O", (byte)fp.Address, 8, fp.Nozzles[0].UnitPrice);
            this.serialPort.DiscardInBuffer();
            this.serialPort.DiscardOutBuffer();
            this.serialPort.Write(buffer, 0, buffer.Length);
            DateTime dt = DateTime.Now;
            while (serialPort.BytesToRead < 7)
            {
                if (DateTime.Now.Subtract(dt).TotalMilliseconds > 200)
                    break;
            }
           
            if (serialPort.BytesToRead < 7)
                return false;
            byte[] response = new byte[serialPort.BytesToRead];
            serialPort.Read(response, 0, response.Length);
            if (response[3] != (byte)('A') && response[3] != 0)
                return false;
            return true;
        }

        private bool AuthorizeDelivery(Common.FuelPoint fp)
        {
            byte[] buffer = this.CreateAuthorizeCommand((byte)fp.Address);
            this.serialPort.DiscardInBuffer();
            this.serialPort.DiscardOutBuffer();
            this.serialPort.Write(buffer, 0, buffer.Length);
            DateTime dt = DateTime.Now;
            while (serialPort.BytesToRead < 7)
            {
                if (DateTime.Now.Subtract(dt).TotalMilliseconds > 200)
                    break;
            }

            if (serialPort.BytesToRead < 7)
                return false;
            byte[] response = new byte[serialPort.BytesToRead];
            serialPort.Read(response, 0, response.Length);
            if (response[3] != (byte)('A') && response[3] != 0)
                return false;
            return true;
        }

        private bool EndDelivery(Common.FuelPoint fp)
        {
            try
            {
                byte[] buffer = this.CreateCommand("O",(byte)fp.Address, 3);
                this.serialPort.DiscardInBuffer();
                this.serialPort.DiscardOutBuffer();
                this.serialPort.Write(buffer, 0, buffer.Length);
                DateTime dt = DateTime.Now;
                while (serialPort.BytesToRead < 7)
                {
                    if (DateTime.Now.Subtract(dt).TotalMilliseconds > 200)
                        break;
                }

                if (serialPort.BytesToRead < 7)
                    return false;
                byte[] response = new byte[serialPort.BytesToRead];
                serialPort.Read(response, 0, response.Length);
                if (response[3] != (byte)('A') && response[3] != 0)
                    return false;
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private bool StartDelivery(Common.FuelPoint fp)
        {
            try
            {
                byte[] buffer = this.CreateCommand("O", (byte)fp.Address, 1);
                this.serialPort.DiscardInBuffer();
                this.serialPort.DiscardOutBuffer();
                this.serialPort.Write(buffer, 0, buffer.Length);
                DateTime dt = DateTime.Now;
                while (serialPort.BytesToRead < 7)
                {
                    if (DateTime.Now.Subtract(dt).TotalMilliseconds > 200)
                        break;
                }

                if (serialPort.BytesToRead < 7)
                    return false;
                byte[] response = new byte[serialPort.BytesToRead];
                serialPort.Read(response, 0, response.Length);
                if (response[3] != (byte)('A') && response[3] != 0)
                    return false;
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private void ThreadRun()
        {
            try
            {
                foreach (Common.FuelPoint fp in this.fuelPoints)
                {
                    if (this.GetTotalsData(fp))
                    {
                        fp.QueryTotals = false;
                        fp.Nozzles[0].QueryTotals = false;
                        fp.Initialized = true;
                        if (this.TotalsRecieved != null)
                        {
                            this.TotalsRecieved(this, new Common.TotalsEventArgs(fp, 1, fp.Nozzles[0].TotalVolume, 0));
                        }
                    }
                    if (this.SetPrice(fp))
                    {
                        fp.QuerySetPrice = false;
                    }
                }
                while (this.IsConnected)
                {
                    try
                    {

                        foreach (Common.FuelPoint fp in this.fuelPoints)
                        {
                            EMRStateEnum currentState = (EMRStateEnum)fp.GetExtendedProperty("EMRState", EMRStateEnum.PRE_DELIVERY_STATE);
                            DeliveryStatusEnum devState = this.GetDeliveryStatus(fp);
                            EMRStateEnum newState = currentState;

                            if (fp.QueryHalt)
                            {
                                if (this.EndDelivery(fp))
                                {
                                    fp.Halted = true;
                                    fp.QueryHalt = false;
                                    System.Threading.Thread.Sleep(200);
                                    continue;
                                }
                            }
                            if (fp.QueryResume)
                            {
                                fp.Halted = false;
                                fp.QueryHalt = false;
                                fp.QueryResume = false;
                                System.Threading.Thread.Sleep(200);
                                continue;
                            }
                            if (fp.QueryTotals || fp.Nozzles[0].QueryTotals)//|| fp.Nozzles[0].QueryTotals && 
                                //(fp.Status == Common.Enumerators.FuelPointStatusEnum.Idle || fp.Status == Common.Enumerators.FuelPointStatusEnum.Offline || fp.Status == Common.Enumerators.FuelPointStatusEnum.TransactionCompleted))
                            {
                                if (this.GetTotalsData(fp))
                                {
                                    fp.QueryTotals = false;
                                    fp.Nozzles[0].QueryTotals = false;
                                    if (!fp.Initialized)
                                        fp.Initialized = true;
                                    if (this.TotalsRecieved != null)
                                    {
                                        this.TotalsRecieved(this, new Common.TotalsEventArgs(fp, 1, fp.Nozzles[0].TotalVolume, 0));
                                    }
                                }
                                else
                                {
                                    System.Threading.Thread.Sleep(50);
                                    continue;
                                }
                            }

                            if (fp.QueryStart)
                            {
                                if (this.StartDelivery(fp))
                                {
                                    fp.QueryStart = false;
                                }
                                continue;
                            }

                            //if (fp.DispenserStatus == Common.Enumerators.FuelPointStatusEnum.Idle && fp.ActiveNozzle != null)
                            //{
                            //    fp.QueryTotals = true;
                            //}

                            this.EvaluateFuelPointStatus(fp, newState, devState);

                            if(fp.DispenserStatus == Common.Enumerators.FuelPointStatusEnum.Idle)
                            {
                                if (fp.QuerySetPrice)
                                {
                                    if (this.SetPrice(fp))
                                        fp.QuerySetPrice = false;
                                    fp.QuerySetPrice = false;
                                }
                                //if (fp.ActiveNozzle != null)
                                //{
                                //    fp.QueryTotals = true;
                                //}
                            }
                            else if (fp.DispenserStatus == Common.Enumerators.FuelPointStatusEnum.Nozzle)
                            {
                                //if (fp.LastStatus != Common.Enumerators.FuelPointStatusEnum.Nozzle)
                                //{
                                //    this.GetTotalsData(fp);
                                //}
                                //if (fp.QueryTotals || fp.Nozzles[0].QueryTotals)
                                //{
                                //    if (this.GetTotalsData(fp))
                                //    {
                                //        fp.Nozzles[0].QueryTotals = false;
                                //        fp.QueryTotals = false;
                                //        if (this.TotalsRecieved != null)
                                //        {
                                //            this.TotalsRecieved(this, new Common.TotalsEventArgs(fp, 1, fp.Nozzles[0].TotalVolume, 0));
                                //        }
                                //    }
                                //}
                                //else
                                //{
                                    newState = this.GetCurrentState(fp);
                                    if (newState != currentState)
                                    {
                                        fp.SetExtendedProperty("EMRState", newState);
                                    }
                                //}
                            }
                            else if (fp.DispenserStatus == Common.Enumerators.FuelPointStatusEnum.Work)
                            {
                                if (this.GetDeliveryData(fp))
                                {
                                    if (this.DataChanged != null)
                                    {
                                        this.DataChanged(this, new Common.FuelPointValuesArgs() { CurrentFuelPoint = fp, CurrentNozzleId = 1, Values = new Common.FuelPointValues() });
                                    }
                                }
                            }
                            //else if (fp.DispenserStatus == Common.Enumerators.FuelPointStatusEnum.TransactionCompleted)
                            //{
                            //    fp.QueryTotals = true;
                            //    //if (fp.QueryTotals || fp.Nozzles[0].QueryTotals)
                            //    //{
                            //    //    if (this.GetTotalsData(fp))
                            //    //    {
                            //    //        fp.Nozzles[0].QueryTotals = false;
                            //    //        fp.QueryTotals = false;
                            //    //        if (this.TotalsRecieved != null)
                            //    //        {
                            //    //            this.TotalsRecieved(this, new Common.TotalsEventArgs(fp, 1, fp.Nozzles[0].TotalVolume, 0));
                            //    //        }
                            //    //    }
                            //    //}
                            //}

                            if (newState != currentState)
                            {
                                fp.GetExtendedProperty("EMRState", newState);
                                Console.WriteLine("EMR3 State : {0}", newState);
                            }
                            if (devState != DeliveryStatusEnum.None)
                                Console.WriteLine("Delivery State : {0}", devState);

                            System.Threading.Thread.Sleep(50);
                        }
                        System.Threading.Thread.Sleep(200);
                    }
                    catch
                    {
                        System.Threading.Thread.Sleep(200);
                    }
                }
            }
            catch
            {
            }
        }

        private void EvaluateFuelPointStatus(Common.FuelPoint fp, EMRStateEnum state, DeliveryStatusEnum devStatus)
        {
            //bool isFlow = devStatus.HasFlag(DeliveryStatusEnum.DeliveryIsActive) || devStatus.HasFlag(DeliveryStatusEnum.FlowIsActive);

            if (devStatus.HasFlag(DeliveryStatusEnum.DeliveryCompleted) &&
                (fp.Status == Common.Enumerators.FuelPointStatusEnum.Work || fp.Status == Common.Enumerators.FuelPointStatusEnum.Nozzle))
            {
                fp.ActiveNozzleIndex = 0;
                fp.DispenserStatus = Common.Enumerators.FuelPointStatusEnum.Idle;
                fp.Status = Common.Enumerators.FuelPointStatusEnum.Idle;
            }
            else if (devStatus.HasFlag(DeliveryStatusEnum.DeliveryIsActive) && !devStatus.HasFlag(DeliveryStatusEnum.FlowIsActive) && fp.DispenserStatus == Common.Enumerators.FuelPointStatusEnum.Idle)
            {
                fp.ActiveNozzleIndex = 0;
                fp.DispenserStatus = Common.Enumerators.FuelPointStatusEnum.Nozzle;
                fp.Status = Common.Enumerators.FuelPointStatusEnum.Nozzle;
            }
            else if (devStatus.HasFlag(DeliveryStatusEnum.DeliveryIsActive) && fp.DispenserStatus == Common.Enumerators.FuelPointStatusEnum.Idle)
            {
                fp.ActiveNozzleIndex = 0;
                fp.DispenserStatus = Common.Enumerators.FuelPointStatusEnum.Nozzle;
                fp.Status = Common.Enumerators.FuelPointStatusEnum.Nozzle;
                System.Threading.Thread.Sleep(1000);
            }
            else if (devStatus.HasFlag(DeliveryStatusEnum.FlowIsActive) && fp.DispenserStatus == Common.Enumerators.FuelPointStatusEnum.Nozzle)
            {
                fp.ActiveNozzleIndex = 0;
                fp.DispenserStatus = Common.Enumerators.FuelPointStatusEnum.Work;
                fp.Status = Common.Enumerators.FuelPointStatusEnum.Work;
            }
            else if (devStatus.HasFlag(DeliveryStatusEnum.FlowIsActive))
            {
                fp.ActiveNozzleIndex = 0;
                fp.DispenserStatus = Common.Enumerators.FuelPointStatusEnum.Work;
                fp.Status = Common.Enumerators.FuelPointStatusEnum.Work;
            }
            else if (devStatus.HasFlag(DeliveryStatusEnum.DeliveryIsActive))
            {
                fp.ActiveNozzleIndex = 0;
                fp.DispenserStatus = Common.Enumerators.FuelPointStatusEnum.Work;
                fp.Status = Common.Enumerators.FuelPointStatusEnum.Work;
            }
            else if (devStatus.HasFlag(DeliveryStatusEnum.FlowIsActive))
            {
                fp.ActiveNozzleIndex = 0;
                fp.DispenserStatus = Common.Enumerators.FuelPointStatusEnum.Work;
                fp.Status = Common.Enumerators.FuelPointStatusEnum.Work;
            }
            else
            {
                if (fp.DispenserStatus == Common.Enumerators.FuelPointStatusEnum.TransactionCompleted && (fp.QueryTotals || fp.Nozzles[0].QueryTotals))
                    fp.ActiveNozzleIndex = 0;
                else
                {
                    fp.ActiveNozzleIndex = -1;
                    fp.DispenserStatus = Common.Enumerators.FuelPointStatusEnum.Idle;
                    fp.Status = Common.Enumerators.FuelPointStatusEnum.Idle;
                }
            }
            //if (state == EMRStateEnum.PRE_DELIVERY_STATE )
            //{
            //    fp.ActiveNozzleIndex = 0;
            //    fp.DispenserStatus = fp.Status;
            //    fp.Status = Common.Enumerators.FuelPointStatusEnum.Nozzle;
            //}
            //else if (state == EMRStateEnum.POPUP_STATE || state == EMRStateEnum.KEY_TIMEOUT_STATE)
            //{
            //    fp.ActiveNozzleIndex = -1;
            //    fp.DispenserStatus = Common.Enumerators.FuelPointStatusEnum.Idle;
            //    fp.Status = Common.Enumerators.FuelPointStatusEnum.Idle;
            //}
            //else if (state == EMRStateEnum.DELIVERY_STATE)
            //{
            //    fp.ActiveNozzleIndex = 0;
            //    fp.QueryTotals = true;

            //    if (devStatus.HasFlag(DeliveryStatusEnum.WaitingForAuthorization))
            //    {
            //        fp.DispenserStatus = Common.Enumerators.FuelPointStatusEnum.Nozzle;
            //        fp.Status = Common.Enumerators.FuelPointStatusEnum.Nozzle;
            //    }
            //    else
            //    {
            //        fp.DispenserStatus = Common.Enumerators.FuelPointStatusEnum.Work;
            //        fp.Status = Common.Enumerators.FuelPointStatusEnum.Work;
            //    }

            //}
            //else if ((state == EMRStateEnum.FINISH_STATE || devStatus.HasFlag(DeliveryStatusEnum.DeliveryCompleted)) && fp.QueryTotals == false)
            //{
            //    fp.ActiveNozzleIndex = 0;
            //    fp.DispenserStatus = Common.Enumerators.FuelPointStatusEnum.TransactionCompleted;
            //    fp.Status = Common.Enumerators.FuelPointStatusEnum.TransactionCompleted;
            //}
        }

        void fp_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Common.FuelPoint fp = sender as Common.FuelPoint;
            if (fp == null)
                return;
            if (e.PropertyName == "Status")
            {
                if (this.DispenserStatusChanged != null)
                {
                    this.DispenserStatusChanged(this, new Common.FuelPointValuesArgs() { CurrentFuelPoint = fp, CurrentNozzleId=1, Values = new Common.FuelPointValues() });
                }
            }
        }
    }

    public enum EMRStateEnum
    {
        PRE_DELIVERY_STATE = 0,
        KEY_TIMEOUT_STATE = 1,
        DELIVERY_STATE,
        FINISH_STATE,
        POPUP_STATE,
        DISPLAY_TEST_STATE
    }

    public enum DeliveryStatusEnum
    {
        None = 0,
        ATCError = 1,
        PulserError = 2,
        PresetError = 4,
        PresetStop = 8,
        NoFlowStop = 16,
        PauseDeliveryRequest = 32,
        DeliveryEndRequest = 64,
        WaitingForAuthorization = 128,
        FeliveryTicketIsPending = 256,
        FlowIsActive = 512,
        DeliveryIsActive = 1024,
        NetPresetIsActive = 2048,
        GrossPresetIsActive = 4096,
        ATCisActive = 8192,
        DeliveryCompleted = 16384,
        DeliveryError = 36768,
    }

}
