using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ASFuelControl.Common;
using ASFuelControl.Common.Enumerators;

namespace ASFuelControl.Gilbarco
{
    public class Connector : Common.IFuelProtocol
    {
        public event EventHandler<Common.FuelPointValuesArgs> DataChanged;
        public event EventHandler<Common.TotalsEventArgs> TotalsRecieved;
        public event EventHandler<Common.FuelPointValuesArgs> DispenserStatusChanged;

        private List<Common.FuelPoint> fuelPoints = new List<Common.FuelPoint>();
        private System.Threading.Thread th;
        private System.IO.Ports.SerialPort serialPort = new System.IO.Ports.SerialPort();
        private bool isConnected = false;

        public bool IsConnected
        {
            get { return this.isConnected; }
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
                this.isConnected = true;
                this.th = new System.Threading.Thread(new System.Threading.ThreadStart(this.ThreadRun));
                this.th.Start();
            }
            catch
            {
            }
        }

        public void Disconnect()
        {
            this.isConnected = false;
            this.serialPort.Close();
        }

        public Common.FuelPoint[] FuelPoints
        {
            get
            {
                return fuelPoints.ToArray();
            }
            set
            {
                this.fuelPoints = new List<Common.FuelPoint>(value);
            }
        }

        public void AddFuelPoint(Common.FuelPoint fp)
        {
            this.fuelPoints.Add(fp);
        }

        public void ClearFuelPoints()
        {
            this.fuelPoints.Clear();
        }

        #region Protocol

        private void EvaluateTotals(Nozzle nozzle, byte[] buffer)
        {
            byte[] volumeBuffer = buffer.Skip(5).Take(8).ToArray();
            volumeBuffer = volumeBuffer.Reverse().ToArray();
            string volumeBufferStr = BitConverter.ToString(volumeBuffer, 0, volumeBuffer.Length).Replace("-", "").Replace("E", "");
            byte[] priceBuffer = buffer.Skip(14).Take(8).ToArray();
            priceBuffer = priceBuffer.Reverse().ToArray();
            string priceBufferStr = BitConverter.ToString(priceBuffer, 0, priceBuffer.Length).Replace("-", "").Replace("E", "");

            nozzle.TotalVolume = decimal.Parse(volumeBufferStr)/10;
            nozzle.TotalPrice = decimal.Parse(priceBufferStr);
        }

        private void EvaluateDisplayData(Nozzle nozzle, byte[] buffer)
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

        public OpenCommand SetPrices(FuelPoint fp)  //douleuei mia xara alla prepei prwta na mpei se listen mode to nozzle.
        {
            byte[] Price = new byte[4];
            int crc = 0;
            int digit;

            if (fp.Nozzles[0].UntiPriceInt <= 0)
                return null;

            for (int j = 0; j < fp.Nozzles.Length; j++)
            {
                string str = fp.Nozzles[j].UntiPriceInt.ToString();
                byte[] nozzlePrice = new byte[4];
                for (int i = 0; i < str.Length; i++)
                {
                    digit = (fp.Nozzles[j].UntiPriceInt / (int)(Math.Pow(10, i))) % 10;
                    crc += digit;
                    nozzlePrice[i] = (byte)((digit + 224));
                }
                for(int i = str.Length; i < 4; i++)
                {
                    nozzlePrice[i] = (byte)((224));
                }
                Price = nozzlePrice;
            }
            crc = (256 - crc);
            byte[] cmd = new byte[] { 0xFF, 0xE5, 0xF4, 0xF6, 0xE0, 0xF7, Price[0], Price[1], Price[2], Price[3], 0xFB, (byte)crc, 0xF0 };
            OpenCommand oc = new OpenCommand(cmd, CommandTypeEnum.SendPrices, fp);
            oc.ResponseLength = this.SetResponseLength(CommandTypeEnum.SendPrices);
            return oc;
        }

        private OpenCommand GetFuelPointStatus(FuelPoint fp)
        {
            byte startByte = BitConverter.GetBytes(0 + fp.Address)[0];
            byte[] command = new byte[] { startByte };

            OpenCommand oc = new OpenCommand(command, CommandTypeEnum.RequestStatus, fp);
            oc.ResponseLength = this.SetResponseLength(CommandTypeEnum.RequestStatus);
            return oc;
        }

        private OpenCommand SetFuelPointStatusToListen(FuelPoint fp)
        {
            byte startByte = BitConverter.GetBytes(32 + fp.Address)[0];
            byte[] command = new byte[] { startByte };

            OpenCommand oc = new OpenCommand(command, CommandTypeEnum.RequestStatus, fp);
            oc.ResponseLength = this.SetResponseLength(CommandTypeEnum.RequestStatus);
            return oc;
        }

        private OpenCommand AuthoriseFuelPoint(FuelPoint fp, decimal presetMoney, decimal presetVolume)
        {
            byte[] command = new byte[] { BitConverter.GetBytes(16 + fp.Address)[0] };

            OpenCommand oc = new OpenCommand(command, CommandTypeEnum.Authorize, fp);
            oc.ResponseLength = this.SetResponseLength(CommandTypeEnum.Authorize);
            return oc;
        }

        private OpenCommand GetNozzleTotals(Nozzle nozzle)
        {   //>
            byte cmd = BitConverter.GetBytes(80 + nozzle.ParentFuelPoint.Address)[0];
            byte[] command = new byte[] { cmd };

            OpenCommand oc = new OpenCommand(command, CommandTypeEnum.RequestTotals, nozzle);
            oc.ResponseLength = this.SetResponseLength(CommandTypeEnum.RequestTotals);
            return oc;
        }

        private OpenCommand GetFuelingPointData(Nozzle nozzle)
        {   //>
            if (nozzle == null)
                return null;
            byte startByte = BitConverter.GetBytes(64 + nozzle.ParentFuelPoint.Address)[0];
            byte[] command = new byte[] { startByte };

            OpenCommand oc = new OpenCommand(command, CommandTypeEnum.RequestDisplayData, nozzle);
            oc.ResponseLength = this.SetResponseLength(CommandTypeEnum.RequestDisplayData);
            return oc;

        }

        private OpenCommand StopFuelPoint(FuelPoint fp)
        {   //>
            byte startByte = BitConverter.GetBytes(48 + fp.Address)[0];
            byte[] command = new byte[] { startByte };

            OpenCommand oc = new OpenCommand(command, CommandTypeEnum.Halt, fp);
            oc.ResponseLength = this.SetResponseLength(CommandTypeEnum.Halt);
            return oc;
        }

        private Common.Enumerators.FuelPointStatusEnum EvaluateStatus(byte[] buffer, Common.Enumerators.FuelPointStatusEnum defStatus)
        {
            if (buffer.Length < 2)
                return defStatus;
            byte b1 = buffer[1];
            if (b1 >= 0x61 && b1 <= 0x6F)
                return Common.Enumerators.FuelPointStatusEnum.Idle; //0x6x
            else if (b1 >= 0xA1 && b1 <= 0xBF)
                return Common.Enumerators.FuelPointStatusEnum.Idle;//0xAx--0xBF
            else if (b1 >= 0x71 && b1 <= 0x7F)
                return Common.Enumerators.FuelPointStatusEnum.Nozzle;//0x7x
            else if (b1 >= 0x81 && b1 <= 0x8F)
                return Common.Enumerators.FuelPointStatusEnum.Ready;//0x8x
            else if (b1 >= 0x91 && b1 <= 0x9F)
                return Common.Enumerators.FuelPointStatusEnum.Work;//0x9
            else if (b1 >= 0xC1 && b1 <= 0xCF)
                return Common.Enumerators.FuelPointStatusEnum.TransactionStopped;
            //else if (b1 >= 0x01 && b1 <= 0x0F)
            //    return Common.Enumerators.FuelPointStatusEnum.TransactionStopped;
            else if (b1 >= 0xD1 && b1 <= 0xDF)
                return Common.Enumerators.FuelPointStatusEnum.Listen; // Den yparxei to sygkekrimeno Enum

            return defStatus;
        }

        private int SetResponseLength(CommandTypeEnum cmdType)
        {
            switch (cmdType)
            {
                case CommandTypeEnum.RequestStatus:
                    return 2;
                case CommandTypeEnum.RequestTotals:
                    return 95;
                case CommandTypeEnum.SendPrices:
                    return 2;
                case CommandTypeEnum.RequestDisplayData:
                    return 34;
                case CommandTypeEnum.Authorize:
                    return 2;
                case CommandTypeEnum.Halt:
                    return 3;
                case CommandTypeEnum.RequestActiveNozzle:
                    return 2;
                case CommandTypeEnum.SendMainDisplayData:
                    return 2;
            }
            return 0;
        }		

        #endregion

        private void ThreadRun()
        {
            foreach (FuelPoint fp in this.fuelPoints)
            {
                foreach (Nozzle nz in fp.Nozzles)
                {
                    nz.QueryTotals = true;
                    nz.TotalVolume = -1;
                }
                fp.QuerySetPrice = true;
                fp.Initialized = false;
                fp.SetExtendedProperty("SetToListen", false);
            }
            while (this.isConnected)
            {
                System.Threading.Thread.Sleep(200);
                try
                {
                    foreach (FuelPoint fp in this.fuelPoints)
                    {
                        OpenCommand cmd = null;
                        lock (fp)
                        {
                            if (fp.QuerySetPrice && fp.DispenserStatus != FuelPointStatusEnum.Listen)
                            {
                                if (this.fuelPoints.Where(f => f.DispenserStatus == FuelPointStatusEnum.Listen || ((bool)f.GetExtendedProperty("SetToListen") && f != fp)).Count() == 0 )
                                {
                                    cmd = this.SetFuelPointStatusToListen(fp);
                                    fp.SetExtendedProperty("SetToListen", true);
                                }
                                else
                                    continue;
                            }
                            else
                            {
                                switch (fp.DispenserStatus)
                                {
                                    case FuelPointStatusEnum.Offline:
                                    case FuelPointStatusEnum.Idle:
                                        if (fp.Nozzles[0] != null && fp.Nozzles[0].QueryTotals && !fp.QuerySetPrice)
                                            cmd = this.GetNozzleTotals(fp.Nozzles[0]);
                                        else
                                            cmd = this.GetFuelPointStatus(fp);
                                        break;
                                    case FuelPointStatusEnum.Listen:
                                        if (fp.QuerySetPrice)
                                        {
                                            
                                            cmd = this.SetPrices(fp);
                                            if(cmd != null)
                                                fp.QuerySetPrice = false;
                                        }
                                        else
                                            cmd = GetFuelPointStatus(fp);
                                        break;
                                    case FuelPointStatusEnum.Nozzle:
                                        if (fp.ActiveNozzle.QueryTotals)
                                            cmd = this.GetNozzleTotals(fp.ActiveNozzle);
                                        else
                                        {
                                            if (fp.QueryAuthorize)
                                            {
                                                cmd = this.AuthoriseFuelPoint(fp, fp.PresetAmount, fp.PresetVolume);
                                                fp.QueryAuthorize = false;
                                            }
                                            else
                                                cmd = this.GetFuelPointStatus(fp);
                                        }
                                        break;
                                    case FuelPointStatusEnum.Ready:
                                        cmd = this.GetFuelPointStatus(fp);
                                        break;
                                    case FuelPointStatusEnum.Work:
                                        if (fp.ActiveNozzle != null)
                                        {
                                            if (fp.ActiveNozzle.QueryTotals)
                                                cmd = this.GetNozzleTotals(fp.ActiveNozzle);
                                            else
                                                cmd = this.GetFuelPointStatus(fp);
                                        }
                                        break;
                                    case FuelPointStatusEnum.TransactionCompleted:
                                    case FuelPointStatusEnum.TransactionStopped:
                                        if (fp.ActiveNozzle.QueryTotals)
                                            cmd = this.GetNozzleTotals(fp.ActiveNozzle);
                                        else
                                            cmd = this.GetFuelingPointData(fp.ActiveNozzle);
                                        break;
                                    case FuelPointStatusEnum.Error:
                                        cmd = this.GetFuelPointStatus(fp);
                                        break;
                                }
                            }
                            if (cmd == null)
                                continue;
                            this.serialPort.DiscardInBuffer();
                            this.serialPort.DiscardOutBuffer();
                            this.serialPort.Write(cmd.Command, 0, cmd.Command.Length);
                            System.Threading.Thread.Sleep(cmd.MinimumTimeNeeded);
                            int waiting = 0;
                            while (this.serialPort.BytesToRead < cmd.ResponseLength && waiting < 300)
                            {
                                System.Threading.Thread.Sleep(10);
                                waiting = waiting + 10;
                            }
                            if (serialPort.BytesToRead == 0)
                            {
                                fp.DispenserStatus = FuelPointStatusEnum.Offline;
                                continue;
                            }
                            byte[] buffer = new byte[serialPort.BytesToRead];
                            serialPort.Read(buffer, 0, buffer.Length);
                            if (cmd.CommandType == CommandTypeEnum.RequestStatus)
                            {
                                Common.Enumerators.FuelPointStatusEnum newStatus = this.EvaluateStatus(buffer, fp.DispenserStatus);
                                if (newStatus == FuelPointStatusEnum.Idle && fp.DispenserStatus == FuelPointStatusEnum.Work && !fp.ActiveNozzle.QueryTotals)
                                {
                                    newStatus = FuelPointStatusEnum.TransactionCompleted;
                                    //fp.Nozzles[0].QueryTotals = true;
                                }
                                if (!fp.Initialized && fp.DispenserStatus != FuelPointStatusEnum.Listen)
                                {
                                    if (newStatus == FuelPointStatusEnum.Listen)
                                    {
                                        fp.DispenserStatus = newStatus;
                                        fp.SetExtendedProperty("SetToListen", false);
                                    }
                                    continue;
                                }
                                //if (cmd.Fpoint.Nozzles[0].QueryTotals)
                                //    continue;
                                if (newStatus == FuelPointStatusEnum.Ready || newStatus == FuelPointStatusEnum.Work && fp.QueryAuthorize)
                                    fp.QueryAuthorize = false;

                                if(newStatus == FuelPointStatusEnum.Idle || newStatus == FuelPointStatusEnum.Offline)
                                    fp.ActiveNozzle = null;
                                else
                                    fp.ActiveNozzle = fp.Nozzles[0];
                                
                                if (fp.DispenserStatus != newStatus || fp.LastStatus != newStatus)
                                {
                                    fp.DispenserStatus = newStatus;
                                    if(fp.DispenserStatus != FuelPointStatusEnum.Listen)
                                        fp.Status = fp.DispenserStatus;
                                    if (this.DispenserStatusChanged != null)
                                    {

                                        int nozzle = 1;
                                        if (newStatus == FuelPointStatusEnum.Idle || newStatus == FuelPointStatusEnum.Offline || newStatus == FuelPointStatusEnum.Listen)
                                            nozzle = 0;
                                        this.DispenserStatusChanged(this, new FuelPointValuesArgs() { CurrentFuelPoint = fp, CurrentNozzleId = nozzle, Values = new FuelPointValues() });
                                    }
                                }
                                if (newStatus == FuelPointStatusEnum.Work)
                                {
                                    Common.FuelPointValues values = new FuelPointValues();
                                    values.ActiveNozzle = 0;
                                    values.CurrentSalePrice = fp.Nozzles[0].UnitPrice;
                                    values.CurrentVolume = 0;
                                    values.CurrentPriceTotal = 0;
                                    this.DataChanged(this, new FuelPointValuesArgs() { CurrentFuelPoint = fp, CurrentNozzleId = 1, Values = values });
                                }
                                Console.WriteLine("Address {2}, Status : {0}, Dispenser Status : {1}", fp.Status, fp.DispenserStatus, fp.Address);
                            }
                            else if (cmd.CommandType == CommandTypeEnum.SendPrices)
                            {
                                fp.QuerySetPrice = false;
                                Common.Enumerators.FuelPointStatusEnum newStatus = this.EvaluateStatus(buffer, fp.DispenserStatus);
                            }
                            else if (cmd.CommandType == CommandTypeEnum.RequestDisplayData)
                            {
                                this.EvaluateDisplayData(fp.Nozzles[0], buffer);
                                if (this.DataChanged != null)
                                {
                                    Common.FuelPointValues values = new FuelPointValues();
                                    values.ActiveNozzle = 0;
                                    values.CurrentSalePrice = fp.Nozzles[0].UnitPrice;
                                    values.CurrentVolume = fp.DispensedVolume;
                                    values.CurrentPriceTotal = fp.DispensedAmount;
                                    this.DataChanged(this, new FuelPointValuesArgs() { CurrentFuelPoint = fp, CurrentNozzleId = 1, Values = values});
                                    if (fp.DispenserStatus == FuelPointStatusEnum.TransactionCompleted)
                                    {
                                        fp.DispenserStatus = FuelPointStatusEnum.Idle;
                                        if (this.DispenserStatusChanged != null)
                                        {
                                            this.DispenserStatusChanged(this, new FuelPointValuesArgs() { CurrentFuelPoint = fp, CurrentNozzleId = 0, Values = new FuelPointValues() });
                                        }
                                    }
                                }
                                fp.Nozzles[0].QueryTotals = true;
                                fp.QueryTotals = true;
                            }
                            else if (cmd.CommandType == CommandTypeEnum.RequestTotals)
                            {
                                this.EvaluateTotals(fp.Nozzles[0], buffer);
                                
                                if (this.TotalsRecieved != null)
                                {
                                    this.TotalsRecieved(this, new TotalsEventArgs(fp, 1, fp.Nozzles[0].TotalPrice, fp.Nozzles[0].TotalVolume));
                                }
                                if (fp.Nozzles[0].UnitPrice == 0)
                                    continue;
                                bool initialized = true;
                                foreach (Nozzle nz in fp.Nozzles)
                                {
                                    if (nz.TotalVolume >= 0)
                                        continue;
                                    initialized = false;
                                    break;
                                }
                                fp.Initialized = initialized;
                                Console.WriteLine("Initialized {0}", fp.Address);
                                if (fp.Initialized)
                                {
                                    fp.Nozzles[0].QueryTotals = false;
                                    fp.QueryTotals = false;
                                }
                            }
                        }
                    }
                }
                catch
                {
                    System.Threading.Thread.Sleep(200);
                }
            }
        }
    }
}
