using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Concurrent;

namespace ASFuelControl.Box69
{
    public class Box69Protocol
    {
        public event EventHandler DataChanged;
        public event EventHandler<TotalsEventArgs> TotalsRecieved;

        enum CurrentThreadStatusEnum
        {
            Idle,
            WaitingResponse
        }

        private bool errorOccured = false;
        private OpenCommand curCommand;
        private List<FuelPoint> fuelPoints = new List<FuelPoint>();
        
        private CurrentThreadStatusEnum currentThreadStatus = CurrentThreadStatusEnum.Idle;
        private List<OpenCommand> commands = new List<OpenCommand>();

        private System.Threading.Thread th;
        private System.IO.Ports.SerialPort serialPort = new System.IO.Ports.SerialPort();

        public string CommunicationPort
        {
            set;
            get;
        }

        public FuelPoint[] FuelPoints
        {
            get { return this.fuelPoints.ToArray(); }
        }

        public bool IsConnected 
        {
            get 
            {
                if (this.serialPort == null)
                    return false;
                return this.serialPort.IsOpen; 
            }
            
        }

        public Box69Protocol()
        {

        }

        public void AddFuelPoint(FuelPoint fp)
        {
            this.fuelPoints.Add(fp);
        }

        public void Connect()
        {
            this.serialPort.PortName = this.CommunicationPort;
            this.serialPort.BaudRate = 9600;

            //this.serialPort.DataReceived -= new System.IO.Ports.SerialDataReceivedEventHandler(serialPort_DataReceived);
            //this.serialPort.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(serialPort_DataReceived);
            this.serialPort.DtrEnable = true;
            //this.serialPort.Handshake = System.IO.Ports.Handshake.RequestToSend;
            this.serialPort.Open();
            th = new System.Threading.Thread(new System.Threading.ThreadStart(this.ThreadRun));
            th.Start();
        }

        public void DisConnect()
        {
            this.serialPort.Close();
        }

        private void ThreadRun()
        {
            while (true)
            {
                if (!(this.serialPort.IsOpen || errorOccured))
                    break;
                try
                {
                    foreach (FuelPoint fp in this.fuelPoints)
                    {
                        if (!fp.Initialized)
                        {
                            fp.DispenserStatus = this.InitializeFuelPoint(fp);
                            fp.Status = fp.DispenserStatus;
                            if (fp.Status != Common.Enumerators.FuelPointStatusEnum.Offline)
                            {
                                fp.Initialized = true;
                                System.Threading.Thread.Sleep(200);
                                bool queryTotals = false;
                                foreach (Nozzle nz in fp.Nozzles)
                                {
                                    nz.QueryTotals = !this.GetTotals(nz);
                                    System.Threading.Thread.Sleep(50);
                                    queryTotals = queryTotals | nz.QueryTotals;
                                }
                                fp.QueryTotals = queryTotals;

                            }
                            continue;
                        }
                        if (fp.QuerySetPrice)
                        {
                            fp.QuerySetPrice = !this.SendPrices(fp);
                            continue;
                        }
                        if (fp.QueryTotals)
                        {
                            if (fp.ActiveNozzle != null)
                                fp.QueryTotals = !this.GetTotals(fp.ActiveNozzle);
                            else
                            {
                                bool queryTotals = false;
                                foreach (Nozzle nz in fp.Nozzles)
                                {
                                    if (nz.QueryTotals)
                                    {
                                        nz.QueryTotals = !this.GetTotals(nz);
                                        System.Threading.Thread.Sleep(50);
                                        queryTotals = queryTotals | nz.QueryTotals;
                                    }
                                }
                                fp.QueryTotals = queryTotals;
                            }
                            if (!fp.QueryTotals)
                            {
                                fp.Initialized = true;
                            }
                        }
                        else if (fp.QueryAuthorize)
                        {
                            fp.QueryAuthorize = !this.AuthorizeFuelPoint(fp);
                        }
                        else if (fp.Status == Common.Enumerators.FuelPointStatusEnum.Work)
                        {
                            this.GetFuelingPointData(fp.ActiveNozzle);
                        }
                        else
                        {
                            fp.DispenserStatus = this.GetDispenserStatus(fp);
                            if (fp.DispenserStatus != Common.Enumerators.FuelPointStatusEnum.Idle && fp.DispenserStatus != Common.Enumerators.FuelPointStatusEnum.Offline)
                            {
                                if(this.GetActiveNozzle(fp))
                                    fp.Status = fp.DispenserStatus;
                            }
                            else
                                fp.Status = fp.DispenserStatus;
                        }
                        System.Threading.Thread.Sleep(50);
                    }
                    System.Threading.Thread.Sleep(250);
                }
                catch (Exception ex)
                {
                    errorOccured = true;
                    System.Threading.Thread.Sleep(100);
                    System.Diagnostics.Trace.WriteLine(ex.Message);
                    if (!this.serialPort.IsOpen)
                        return;
                }
            }
        }

        private void ThreadRun2()
        {
            while (true)
            {
                if (!(this.serialPort.IsOpen || errorOccured))
                    break;
                try
                {
                    foreach (FuelPoint fp in this.fuelPoints)
                    {
                        OpenCommand command = fp.CreateNextCommand();
                        if (command != null)
                            this.commands.Add(command);
                    }
                    //foreach (OpenCommand cmd in this.commands)
                    //{
                    //    System.Diagnostics.Trace.WriteLine("COMMAND :" + cmd.CommandType.ToString() + " --- " + cmd.Fpoint.AddressId.ToString() + " -- " + cmd.CommandString);
                    //}
                    DateTime dtStart = DateTime.Now;
                    List<OpenCommand> toRemove = new List<OpenCommand>();
                    foreach (OpenCommand cmd in this.commands)
                    {
                       
                        
                        //this.serialPort.DiscardInBuffer();
                        //this.serialPort.DiscardOutBuffer();

                        DateTime dt = DateTime.Now;
                        this.serialPort.Write(cmd.Command, 0, cmd.Command.Length);
                        if (Properties.Settings.Default.LogCommunication)
                        {
                            System.IO.File.AppendAllLines("Box69Log.txt", new List<string>() {cmd.CommandString});
                        }
                        while (DateTime.Now.Subtract(dt).TotalMilliseconds < cmd.MinimumTimeNeeded)
                        {
                            System.Threading.Thread.Sleep(10);
                        }
                        DateTime dtNow = DateTime.Now;
                        while (this.serialPort.BytesToRead < cmd.ResponseLength && DateTime.Now.Subtract(dtNow).TotalMilliseconds < 50)
                        {
                            System.Threading.Thread.Sleep(50);
                        }
                        if(this.serialPort.BytesToRead < cmd.ResponseLength)
                        {
                            //System.Diagnostics.Trace.WriteLine("NO DATA :" + this.serialPort.BytesToRead.ToString()  + " - " +  cmd.ResponseLength.ToString());
                            if (this.serialPort.BytesToRead == 0)
                            {
                                if (Properties.Settings.Default.LogCommunication)
                                {
                                    System.IO.File.AppendAllLines("Box69Log.txt", new List<string>() { "NO ANSWER" });
                                }

                                OpenCommand cmd2 = cmd.Fpoint.GetId();

                                this.serialPort.Write(cmd2.Command, 0, cmd2.Command.Length);
                                if (Properties.Settings.Default.LogCommunication)
                                {
                                    System.IO.File.AppendAllLines("Box69Log.txt", new List<string>() { cmd2.CommandString });
                                }
                                System.Threading.Thread.Sleep(100);
                                Byte[] dummy = new byte[this.serialPort.BytesToRead];
                                this.serialPort.Read(dummy, 0, this.serialPort.BytesToRead);
                                continue;
                            }
                            //OpenCommand deqComd = null;
                            toRemove.Add(cmd);
                            //this.commands.Remove(deqComd);
                            System.Threading.Thread.Sleep(100);
                            this.serialPort.DiscardInBuffer();
                            this.serialPort.DiscardOutBuffer();
                            continue;
                        }
                        if (cmd.Fpoint.NoAnswer)
                            cmd.Fpoint.NoAnswer = false;
                        Byte[] buffer = new byte[this.serialPort.BytesToRead];
                        //System.Diagnostics.Trace.WriteLine("Commad Write :" + this.commands.Count().ToString() + " --- " +  cmd.CommandType.ToString() + " --- " + cmd.Fpoint.AddressId.ToString() + " -- " + cmd.CommandString);
                        this.serialPort.Read(buffer, 0, this.serialPort.BytesToRead);
                        byte[] response = buffer.Take(cmd.ResponseLength).ToArray();
                        
                        if (!this.CheckBuffer(response))
                        {
                            System.Diagnostics.Trace.WriteLine(cmd.CommandType);
                            toRemove.Add(cmd);
                            System.Diagnostics.Trace.WriteLine("Error Data :");

                            if (Properties.Settings.Default.LogCommunication)
                            {
                                System.IO.File.AppendAllLines("Box69Log.txt", new List<string>() { BitConverter.ToString(response) });
                            }

                            continue;
                        }

                        response = this.GetBuffer(response);

                        if (Properties.Settings.Default.LogCommunication)
                        {
                            System.IO.File.AppendAllLines("Box69Log.txt", new List<string>() { BitConverter.ToString(response) });
                        }

                       //System.Diagnostics.Trace.WriteLine("Commad Read :"+ cmd.CommandType.ToString() + " --- " + cmd.Fpoint.AddressId.ToString() + " -- " + BitConverter.ToString(response));
                        switch (cmd.CommandType)
                        {
                            case CommandTypeEnum.RequestStatus:
                            case CommandTypeEnum.SendMainDisplayData:
                                Common.Enumerators.FuelPointStatusEnum newStatus = this.EvaluateStatus(response, cmd.Fpoint.DispenserStatus);
                                cmd.Fpoint.InitializeSent = true;

                                if (cmd.CommandType == CommandTypeEnum.RequestStatus && newStatus == Common.Enumerators.FuelPointStatusEnum.Nozzle && cmd.Fpoint.ActiveNozzle == null)
                                    cmd.Fpoint.QueryNozzle = true;
                                else
                                    cmd.Fpoint.DispenserStatus = newStatus;
                                break;
                            case CommandTypeEnum.Halt:
                                Common.Enumerators.FuelPointStatusEnum newStatus1 = this.EvaluateStatus(response, cmd.Fpoint.DispenserStatus);
                                cmd.Fpoint.DispenserStatus = newStatus1;
                                cmd.Fpoint.QueryStop = false;
                                break;
                            case CommandTypeEnum.Authorize:
                                Common.Enumerators.FuelPointStatusEnum newStatus2 = this.EvaluateStatus(response, cmd.Fpoint.DispenserStatus);
                                cmd.Fpoint.DispenserStatus = newStatus2;
                                cmd.Fpoint.QueryAuthorize = false;
                                break;
                            case CommandTypeEnum.RequestActiveNozzle:
                                cmd.Fpoint.ActiveNozzle = this.EvaluateActiveNozzle(cmd.Fpoint, response);
                                cmd.Fpoint.DispenserStatus = Common.Enumerators.FuelPointStatusEnum.Nozzle;
                                cmd.Fpoint.QueryNozzle = false;
                                break;
                            case CommandTypeEnum.SendPrices:
                                if (response[0] != 0xB0)
                                {
                                    //continue;
                                }
                                cmd.Fpoint.PriceSet = true;
                                cmd.Fpoint.QuerySetPrice = false;
                                cmd.Fpoint.PriceSetSent = true;
                                break;
                            case CommandTypeEnum.RequestTotals:
                                if (cmd.CurrentNozzle != null)
                                {
                                    bool ignore = false;
                                    if (cmd.Fpoint.ErrorCount > 2)
                                        ignore = true;
                                    bool ret = this.EvaluateTotals(cmd.CurrentNozzle, response, ignore);
                                    if (ret)
                                    {
                                        cmd.Fpoint.QueryTotals = false;
                                        cmd.Fpoint.ErrorCount = 0;
                                    }
                                    else
                                    {
                                        cmd.Fpoint.ErrorCount++;
                                    }
                                }
                                if (cmd.Fpoint.Status != cmd.Fpoint.DispenserStatus)
                                    cmd.Fpoint.Status = cmd.Fpoint.DispenserStatus;
                                break;
                            case CommandTypeEnum.RequestID:
                                //if (cmd.Fpoint.QueryTotals)
                                //{
                                //    Nozzle nz = cmd.Fpoint.LastNozzle;
                                //    //if(cmd.Fpoint.ActiveNozzle != null)
                                //    //    nz = cmd.Fpoint.ActiveNozzle;
                                //    //cmd.Fpoint.QueryTotals = false;
                                //    //nz.Totalizer = nz.Totalizer + (nz.SaleVolume * 100);
                                //    //if (cmd.Fpoint.Status != cmd.Fpoint.DispenserStatus)
                                //    //    cmd.Fpoint.Status = cmd.Fpoint.DispenserStatus;
                                //    //if (this.TotalsRecieved != null)
                                //    //    this.TotalsRecieved(cmd.Fpoint, new TotalsEventArgs(cmd.Fpoint.AddressId, (byte)nz.NozzleIndex, nz));
                                //}
                                break;
                            case CommandTypeEnum.RequestDisplayData:
                                if (cmd.CurrentNozzle != null)
                                    this.EvaluateDisplayData(cmd.CurrentNozzle, response);
                                break;
                        }
                        toRemove.Add(cmd);
                        //this.commands.TryDequeue(out this.curCommand);

                    }
                    //foreach (OpenCommand cmd in toRemove)
                    this.commands.Clear();
                    DateTime dtEnd = DateTime.Now;
                    int millisecs = (int)dtEnd.Subtract(dtStart).TotalMilliseconds;
                    if(millisecs < 250)
                        System.Threading.Thread.Sleep(250 - millisecs);
                }
                catch(Exception ex)
                {
                    errorOccured = true;
                    System.Threading.Thread.Sleep(100);
                    System.Diagnostics.Trace.WriteLine(ex.Message);
                    if (!this.serialPort.IsOpen)
                        return;
                }
            }
            
        }

        private byte[] ExecuteCommnad(OpenCommand cmd)
        {
            try
            {
                DateTime dt = DateTime.Now;
                this.serialPort.DiscardInBuffer();
                this.serialPort.DiscardOutBuffer();
                this.serialPort.Write(cmd.Command, 0, cmd.Command.Length);
                if (Properties.Settings.Default.LogCommunication)
                {
                    System.IO.File.AppendAllLines("Box69Log.txt", new List<string>() { cmd.CommandString });
                }
                while (DateTime.Now.Subtract(dt).TotalMilliseconds < cmd.MinimumTimeNeeded)
                {
                    System.Threading.Thread.Sleep(10);
                }
                DateTime dtNow = DateTime.Now;
                while (this.serialPort.BytesToRead < cmd.ResponseLength && DateTime.Now.Subtract(dtNow).TotalMilliseconds < cmd.MinimumTimeNeeded * 3)
                {
                    System.Threading.Thread.Sleep(50);
                }

                if (this.serialPort.BytesToRead < cmd.ResponseLength)
                {
                    return new byte[0];
                }

                Byte[] buffer = new byte[this.serialPort.BytesToRead];
                //System.Diagnostics.Trace.WriteLine("Commad Write :" + this.commands.Count().ToString() + " --- " +  cmd.CommandType.ToString() + " --- " + cmd.Fpoint.AddressId.ToString() + " -- " + cmd.CommandString);
                this.serialPort.Read(buffer, 0, this.serialPort.BytesToRead);
                byte[] response = buffer.Take(cmd.ResponseLength).ToArray();
                if (!this.CheckBuffer(response))
                    return new byte[0];
                response = this.GetBuffer(response);

                return response;
            }
            catch
            {
                return new byte[0];
            }
        }

        private Common.Enumerators.FuelPointStatusEnum GetDispenserStatus(FuelPoint fp)
        {
            OpenCommand cmd = fp.GetFuelPointStatus();
            byte[] response = this.ExecuteCommnad(cmd);
            if (response.Length == 0)
                return fp.Status;
           
            return this.EvaluateStatus(response, fp.Status);
        }

        private bool GetFuelingPointData(Nozzle nz)
        {
            OpenCommand cmd = nz.ParentFuelPoint.GetFuelingPointData(nz);
            byte[] response = this.ExecuteCommnad(cmd);
            if (response.Length == 0)
                return false;
            return this.EvaluateDisplayData(nz, response);
        }

        private bool GetTotals(Nozzle nz)
        {
            bool totalsRecieved = false;
            int i = 0;
            while (!totalsRecieved && i < 5)
            {
                OpenCommand cmd = nz.ParentFuelPoint.GetNozzleTotals(nz);
                byte[] response = this.ExecuteCommnad(cmd);
                if (response.Length == 0)
                    totalsRecieved = false;
                else
                    totalsRecieved = this.EvaluateTotals(nz, response, false);
                i++;
                System.Threading.Thread.Sleep(50);
            }
            return totalsRecieved;
        }

        private Common.Enumerators.FuelPointStatusEnum InitializeFuelPoint(FuelPoint fp)
        {
            OpenCommand cmd = fp.InitializeFuelPoint();
            byte[] response = this.ExecuteCommnad(cmd);
            if (response.Length == 0)
                return fp.Status;
            
            return this.EvaluateStatus(response, fp.Status);
        }

        private bool SendPrices(FuelPoint fp)
        {
            OpenCommand cmd = fp.SetPrices();
            byte[] response = this.ExecuteCommnad(cmd);
            if (response.Length == 0)
                return false;
            if(response[0] != 0xB0)
                return false;
            return true;
        }

        private bool AuthorizeFuelPoint(FuelPoint fp)
        {
            Nozzle nz = fp.ActiveNozzle;
            decimal price = (decimal)nz.UnitPrice / (decimal)Math.Pow(10, fp.PriceDecimalPlaces);
            OpenCommand cmd = fp.AuthoriseFuelPoint(price, (decimal)9999.99, (decimal)9999.99);
            byte[] response = this.ExecuteCommnad(cmd);
            if (response.Length == 0)
                return false;
            //fp.ActiveNozzle = this.EvaluateActiveNozzle(cmd.Fpoint, response);
            return true;
        }

        private bool GetActiveNozzle(FuelPoint fp)
        {
            OpenCommand cmd = fp.GetActiveNozzle();
            byte[] response = this.ExecuteCommnad(cmd);
            if (response.Length == 0)
                return false;
            fp.ActiveNozzle = this.EvaluateActiveNozzle(fp, response);
            if(fp.ActiveNozzle != null)
                return true;
            return false;
        }

        private void StopFuelPoint(FuelPoint fp)
        {
            fp.QueryStop = true;
        }

        private void GetNozzleTotals(Nozzle nozzle)
        {
            nozzle.ParentFuelPoint.QueryTotals = true;
        }

        private bool CheckBuffer(byte[] buffer)
        {
            if (buffer.Length % 2 != 0)
                return false;
            for (int i = 0; i < buffer.Length / 2; i++)
            {
                int j = (2 * i);
                if (buffer[j] + buffer[j + 1] != 255)
                    return false;
            }
            return true;
        }

        private byte[] GetBuffer(byte[] buffer)
        {
            if (buffer.Length % 2 != 0)
                return new byte[]{};
            List<byte> newBuffer = new List<byte>();
            for (int i = 0; i < buffer.Length / 2; i++)
            {
                int j = (2 * i);
                newBuffer.Add(buffer[j]);
            }
            return newBuffer.ToArray();
        }

        private int ToNumeral(BitArray binary, int length)
        {
            int numeral = 0;
            for (int i = 0; i < length; i++)
            {
                if (binary[i])
                {
                    numeral = numeral | (((int)1) << (length - 1 - i));
                }
            }
            return numeral;
        }

        private Common.Enumerators.FuelPointStatusEnum EvaluateStatus(byte[] buffer, Common.Enumerators.FuelPointStatusEnum defStatus)
        {
            byte b1 = buffer[0];
            if(b1 == 0x2F)
                return Common.Enumerators.FuelPointStatusEnum.Offline;
            else if (b1 == 0x20)
                return Common.Enumerators.FuelPointStatusEnum.Idle;
            else if (b1 == 0xA0)
                return Common.Enumerators.FuelPointStatusEnum.Nozzle;
            else if (b1 == 0x90)
                return Common.Enumerators.FuelPointStatusEnum.Ready;
            else if (b1 == 0xD0)
                return Common.Enumerators.FuelPointStatusEnum.Work;
            else if (b1 == 0xF0)
                return Common.Enumerators.FuelPointStatusEnum.Work;
            else if(b1 == 0x91)
                return Common.Enumerators.FuelPointStatusEnum.TransactionStopped;
            else if (b1 == 0x92)
                return Common.Enumerators.FuelPointStatusEnum.TransactionStopped;
            else if (b1 == 0x99)
                return Common.Enumerators.FuelPointStatusEnum.TransactionStopped;

            return defStatus;
        }

        private Nozzle EvaluateActiveNozzle(FuelPoint fp, byte[] buffer)
        {
            BitArray answer = new BitArray(new byte[] { buffer[0] });
            BitArray nozzleNrArray = new BitArray(7);
            nozzleNrArray.Set(0, answer.Get(6));
            nozzleNrArray.Set(1, answer.Get(5));
            nozzleNrArray.Set(2, answer.Get(4));
            nozzleNrArray.Set(3, answer.Get(3));
            nozzleNrArray.Set(4, answer.Get(2));
            nozzleNrArray.Set(5, answer.Get(1));
            nozzleNrArray.Set(6, answer.Get(0));
            int nozzleIndex = this.ToNumeral(nozzleNrArray, 7);
            nozzleIndex = nozzleIndex - 1;
            if (nozzleIndex < 0)
                return null;
            return fp.Nozzles[nozzleIndex];
        }

        private bool EvaluateTotals(Nozzle nozzle, byte[] buffer, bool ignoreError)
        {
            byte[] volumeBuffer = buffer.Take(5).ToArray();
            volumeBuffer = volumeBuffer.Reverse().ToArray();
            byte[] priceBuffer = buffer.Skip(5).Take(5).ToArray();
            priceBuffer = priceBuffer.Reverse().ToArray();

            byte[] status = buffer.Skip(10).Take(1).ToArray();
            string volString = BitConverter.ToString(volumeBuffer).Replace("-", "");// volumeBuffer[0].ToString() + volumeBuffer[1].ToString() + volumeBuffer[2].ToString() + volumeBuffer[3].ToString() + volumeBuffer[4].ToString();
            string priceString = BitConverter.ToString(priceBuffer).Replace("-", ""); //priceBuffer[0].ToString() + priceBuffer[1].ToString() + priceBuffer[2].ToString() + priceBuffer[3].ToString() + priceBuffer[4].ToString();
            //nozzle.ParentFuelPoint.DispenserStatus = this.EvaluateStatus(status, nozzle.ParentFuelPoint.DispenserStatus);
            decimal newTotal = decimal.Parse(volString);

            Console.WriteLine("TOTALS Bytes : {2}, String : {0},  Value : {1}", volString, newTotal, BitConverter.ToString(volumeBuffer));


            //if (nozzle.Totalizer > 0 && (nozzle.Totalizer + (nozzle.SaleVolume * 100) != newTotal) && !ignoreError)
            //    return false;
            nozzle.Totalizer = newTotal;

            if(this.TotalsRecieved!= null)
                this.TotalsRecieved(nozzle.ParentFuelPoint, new TotalsEventArgs(nozzle.ParentFuelPoint.AddressId, (byte)nozzle.NozzleIndex, nozzle));
            return true;
        }

        private bool EvaluateDisplayData(Nozzle nozzle, byte[] buffer)
        {
            try
            {
                byte[] unitPrice = buffer.Take(2).ToArray();
                unitPrice = unitPrice.Reverse().ToArray();
                byte[] priceBuffer = buffer.Skip(2).Take(3).ToArray();
                priceBuffer = priceBuffer.Reverse().ToArray();
                byte[] volumeBuffer = buffer.Skip(5).Take(3).ToArray();
                volumeBuffer = volumeBuffer.Reverse().ToArray();


                byte[] status = buffer.Skip(8).Take(1).ToArray();
                string upString = BitConverter.ToString(unitPrice).Replace("-", "");// unitPrice[0].ToString() + unitPrice[1].ToString();
                string volString = BitConverter.ToString(volumeBuffer).Replace("-", "");// volumeBuffer[0].ToString() + volumeBuffer[1].ToString() + volumeBuffer[2].ToString();
                string priceString = BitConverter.ToString(priceBuffer).Replace("-", "");// priceBuffer[0].ToString() + priceBuffer[1].ToString() + priceBuffer[2].ToString();

                nozzle.ParentFuelPoint.DispenserStatus = this.EvaluateStatus(status, nozzle.ParentFuelPoint.DispenserStatus);
                nozzle.SaleUnitPrice = decimal.Parse(upString) / (decimal)Math.Pow(10, nozzle.PriceDecimalPlaces);
                nozzle.SaleVolume = decimal.Parse(volString) / (decimal)Math.Pow(10, nozzle.VolumeDecimalPlaces);
                nozzle.SalePrice = decimal.Parse(priceString) / 100;

                nozzle.ParentFuelPoint.LastSalePrice = nozzle.SalePrice;
                nozzle.ParentFuelPoint.LastSaleUnitPrice = nozzle.SaleUnitPrice;
                nozzle.ParentFuelPoint.LastSaleVolume = nozzle.SaleVolume;

                if (this.DataChanged != null)
                    this.DataChanged(nozzle.ParentFuelPoint, new EventArgs());
                return true;
            }
            catch
            {
                return false;
            }
        }

        private List<byte> responseBuffer = new List<byte>();

        //void serialPort_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        //{
        //    try
        //    {
        //        Byte[] buffer = new byte[this.serialPort.BytesToRead];
        //        this.serialPort.Read(buffer, 0, this.serialPort.BytesToRead);
        //        if (this.currentCommand == null || this.currentThreadStatus != CurrentThreadStatusEnum.WaitingResponse)
        //            return;

        //        responseBuffer.AddRange(buffer);

        //        OpenCommand cmd = this.currentCommand;
        //        cmd.TimeExeeded = DateTime.Now.AddMilliseconds(50);
        //        if (this.responseBuffer.Count < cmd.ResponseLength)
        //            return;

        //        byte[] response = this.responseBuffer.Take(cmd.ResponseLength).ToArray();
        //        this.responseBuffer.Clear();
        //        if (!this.CheckBuffer(response))
        //        {
        //            //this.serialPort.Write(this.currentCommand.Command, 0, this.currentCommand.Command.Length);
        //            Console.WriteLine(this.currentCommand.CommandType);
        //            return;
        //        }

        //        response = this.GetBuffer(response);

        //        switch (this.currentCommand.CommandType)
        //        {
        //            case CommandTypeEnum.RequestStatus:
        //                Common.Enumerators.FuelPointStatusEnum status1 = this.EvaluateStatus(response);
        //                if (this.currentCommand.Fpoint.Status == Common.Enumerators.FuelPointStatusEnum.Idle && status1 == Common.Enumerators.FuelPointStatusEnum.Nozzle)
        //                {
        //                    if (this.curCommand.Fpoint.ActiveNozzle == null)
        //                    {
        //                        this.GetActiveNozzle(this.currentCommand.Fpoint);
        //                    }
        //                    else
        //                        this.currentCommand.Fpoint.Status = status1;

        //                }
        //                else if (this.currentCommand.Fpoint.Status != Common.Enumerators.FuelPointStatusEnum.Idle && status1 == Common.Enumerators.FuelPointStatusEnum.Idle)
        //                {
        //                    this.GetFuelingPointData(this.currentCommand.Fpoint.ActiveNozzle);
        //                    this.currentCommand.Fpoint.Status = status1;
        //                    this.currentCommand.Fpoint.ActiveNozzle = null;
        //                }
        //                else
        //                    this.currentCommand.Fpoint.Status = status1;

                        

        //                break;
        //            case CommandTypeEnum.Halt:
        //            case CommandTypeEnum.Authorize:
        //            case CommandTypeEnum.SendMainDisplayData:
        //                Common.Enumerators.FuelPointStatusEnum newStatus = this.EvaluateStatus(response);
        //                this.currentCommand.Fpoint.Status = newStatus;
        //                break;
        //            case CommandTypeEnum.RequestActiveNozzle:
        //                this.currentCommand.Fpoint.ActiveNozzle = this.EvaluateActiveNozzle(this.currentCommand.Fpoint, response);
        //                break;
        //            case CommandTypeEnum.SendPrices:
        //                if (response[0] != 0xB0)
        //                {
        //                    this.serialPort.Write(this.currentCommand.Command, 0, this.currentCommand.Command.Length);
        //                    Console.WriteLine(this.currentCommand.CommandType);
        //                    return;
        //                }
        //                this.currentCommand.Fpoint.PriceSet = true;
        //                break;
        //            case CommandTypeEnum.RequestTotals:
        //                if (this.currentCommand.CurrentNozzle != null)
        //                    this.EvaluateTotals(this.currentCommand.CurrentNozzle, response);
        //                break;
        //            case CommandTypeEnum.RequestDisplayData:
        //                if (this.currentCommand.CurrentNozzle != null)
        //                    this.EvaluateDisplayData(this.currentCommand.CurrentNozzle, response);
        //                break;
        //        }
        //        this.commands.TryDequeue(out this.curCommand);
        //        this.currentCommand = null;
        //        this.currentThreadStatus = CurrentThreadStatusEnum.Idle;
        //    }
        //    catch
        //    {
        //        errorOccured = true;
        //    }
        //}
    }

    public enum CommandTypeEnum
    {
        RequestID,
        RequestDisplayData,
        RequestStatus,
        Halt,
        Authorize,
        SendMainDisplayData,
        RequestTotals,
        RequestActiveNozzle,
        AcknowledgeDeactivatedNozzle,
        SendPrices,
        Initialize
    }

    
}