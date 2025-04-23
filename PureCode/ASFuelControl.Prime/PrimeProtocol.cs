using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ASFuelControl.Common;

namespace ASFuelControl.Prime
{
    public class PrimeProtocol : Common.IFuelProtocol
    {
        const byte SOH = 0x01;
        const byte STX = 0x02;
        const byte ETX = 0x03;
        const byte EOT = 0x04;
        const byte ENQ = 0x05;
        const byte ACK = 0x06;
        const byte NAK = 0x15;
        const byte EOB = 0x17;
        const byte FS  = 0x1C;

        public event EventHandler<Common.FuelPointValuesArgs> DataChanged;
        public event EventHandler<Common.TotalsEventArgs> TotalsRecieved;
        public event EventHandler<Common.FuelPointValuesArgs> DispenserStatusChanged;

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
                this.serialPort.Parity = System.IO.Ports.Parity.Even;
                this.serialPort.BaudRate = 9600;
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

        public Common.DebugValues DebugStatusDialog(Common.FuelPoint fp)
        {
            foo = null;
            //fp = GetStatus(fp);
            foo.Status = fp.Status;
            return foo;
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
                            if (fp.QueryHalt)
                            {
                                this.Halt(fp);
                                continue;
                            }
                            if (fp.QueryResume)
                            {
                                //resume
                            }

                            if (fp.QuerySetPrice)
                            {
                                foreach (Common.Nozzle nz in fp.Nozzles)
                                {
                                    this.SetPrice(nz);
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
                                        this.GetTotals(nz);
                                        System.Threading.Thread.Sleep(10);
                                        fp.Initialized = true;
                                    }
                                }

                                continue;
                            }

                            if (fp.QueryAuthorize)
                            {
                                this.Authorize(fp);
                                continue;
                            }
                           
                            this.GetStatus(fp);
                        }
                        finally
                        {
                            System.Threading.Thread.Sleep(120);
                        }

                    }
                }
                catch (Exception e)
                {
                    System.Threading.Thread.Sleep(250);
                }
            }
        }

        private bool ExecuteCommand(int responseLength, byte[] command, bool awaitAck)
        {
            this.serialPort.Write(command, 0, command.Length);
            int waiting = 0;
            int respLen = awaitAck ? responseLength + 1 : responseLength;
            while (this.serialPort.ReadBufferSize < respLen || waiting < 100)
                waiting = waiting + 10;
            byte[] response = new byte[this.serialPort.ReadBufferSize];
            this.serialPort.Read(response, 0, response.Length);
            if (response.Length >= respLen)
            {
                return this.EvaluateResponse(response);
            }
            return false;
        }

        private byte[] GetNumberAsByte(long number, int length)
        {
            List<byte> buffer = new List<byte>();
            string str = number.ToString();
            for (int i = str.Length; i < length; i++)
                buffer.Add(30);
            for (int i = 0; i < str.Length; i++)
                buffer.Add((byte)(30 + int.Parse(str[i].ToString())));
            
            return buffer.ToArray();
        }

        private void GetStatus(FuelPoint fp)
        {
            byte[] addressBuffer = this.GetNumberAsByte(fp.Address, 2);
            List<byte> buffer = new List<byte>();
            buffer.AddRange(ASCIIEncoding.ASCII.GetBytes("ENQ"));
            buffer.AddRange(addressBuffer);
            this.ExecuteCommand(8, buffer.ToArray(), false);
        }

        private void GetTotals(Nozzle nz)
        {
            byte[] command = this.BuildCommand(nz.ParentFuelPoint, "TQ", new byte[] { });
            this.ExecuteCommand(17, command, true);
        }

        private void SetPrice(Nozzle nz)
        {
            int price = nz.UntiPriceInt;
            byte[] priceBuffer = this.GetNumberAsByte(price, 4);
            byte[] command = this.BuildCommand(nz.ParentFuelPoint, "PC", priceBuffer.ToArray());
            this.ExecuteCommand(17, command, false);
        }

        private void Authorize(FuelPoint fp)
        {
            if (fp.PresetAmount < 999999)
            {
                int amount = (int)(fp.PresetAmount * (decimal)Math.Pow(10, fp.AmountDecimalPlaces));
                byte[] cmd = BuildCommand(fp, "STA", this.GetNumberAsByte(amount, 7));
                int i = 0;
                while(!this.ExecuteCommand(0, cmd, true) && i < 200)
                {
                    System.Threading.Thread.Sleep(20);
                    i = i + 1;
                    continue;
                }
                if (i == 200)
                    return;
            }

            byte[] command = this.BuildCommand(fp, "AP", new byte[] { });
            if (this.ExecuteCommand(0, command, true))
                fp.QueryAuthorize = false;
        }

        private void Halt(FuelPoint fp)
        {
            byte[] command = this.BuildCommand(fp, "SC", new byte[] { });
            if (this.ExecuteCommand(0, command, true))
                fp.QueryHalt = false;
        }

        private byte[] BuildCommand(FuelPoint fp, string command, byte[] data)
        {
            byte[] addressBuffer = this.GetNumberAsByte(fp.Address, 2);
            byte[] commandBuffer = ASCIIEncoding.ASCII.GetBytes(command);
            List<byte> buffer = new List<byte>();
            buffer.Add(SOH);
            buffer.AddRange(addressBuffer);
            buffer.Add(STX);
            buffer.AddRange(commandBuffer);
            buffer.AddRange(data);
            buffer.Add(ETX);

            byte lrc = this.CalculateLRC(buffer.Skip(1).ToArray());
            buffer.Add(lrc);
            return buffer.ToArray();
        }

        private void GetDisplayData(FuelPoint fp, byte[] dataBuffer)
        {
            if (dataBuffer.Length != 14)
                return;
            string volumeData = ASCIIEncoding.ASCII.GetString(dataBuffer.Take(7).ToArray());
            string priceData = ASCIIEncoding.ASCII.GetString(dataBuffer.Skip(7).Take(7).ToArray());
            fp.DispensedAmount = decimal.Parse(priceData) / (decimal)Math.Pow(10, fp.AmountDecimalPlaces);
            fp.DispensedVolume = decimal.Parse(volumeData) / (decimal)Math.Pow(10, fp.VolumeDecimalPlaces);
            if (this.DataChanged != null)
            {
                this.DataChanged(this, new FuelPointValuesArgs() { CurrentFuelPoint = fp, CurrentNozzleId = 0, Values = 
                    new FuelPointValues() { Status = Common.Enumerators.FuelPointStatusEnum.Work, ActiveNozzle = 0, CurrentVolume = fp.DispensedVolume, CurrentPriceTotal = fp.DispensedAmount, CurrentSalePrice = fp.Nozzles[0].UnitPrice } });
            }
        }

        private bool GetTrancactionEndsData(FuelPoint fp, byte[] dataBuffer)
        {
            try
            {
                if (dataBuffer.Length != 18)
                    return false;
                string unitPriceData = ASCIIEncoding.ASCII.GetString(dataBuffer.Take(4).ToArray());
                string volumeData = ASCIIEncoding.ASCII.GetString(dataBuffer.Skip(4).Take(7).ToArray());
                string priceData = ASCIIEncoding.ASCII.GetString(dataBuffer.Skip(11).Take(7).ToArray());
                fp.DispensedAmount = decimal.Parse(priceData) / (decimal)Math.Pow(10, fp.AmountDecimalPlaces);
                fp.DispensedVolume = decimal.Parse(volumeData) / (decimal)Math.Pow(10, fp.VolumeDecimalPlaces);
                decimal unitPrice = decimal.Parse(unitPriceData) / (decimal)Math.Pow(10, fp.UnitPriceDecimalPlaces);

                if (this.DataChanged != null)
                {
                    this.DataChanged(this, new FuelPointValuesArgs()
                    {
                        CurrentFuelPoint = fp,
                        CurrentNozzleId = 0,
                        Values =
                            new FuelPointValues() { Status = Common.Enumerators.FuelPointStatusEnum.Work, ActiveNozzle = 0, CurrentVolume = fp.DispensedVolume, CurrentPriceTotal = fp.DispensedAmount, CurrentSalePrice = unitPrice }
                    });
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool GetTotalsData(FuelPoint fp, byte[] dataBuffer)
        {
            try
            {
                if (dataBuffer.Length != 9)
                    return false;
                string totalVolume = ASCIIEncoding.ASCII.GetString(dataBuffer.Take(9).ToArray());
                fp.Nozzles[0].TotalVolume = decimal.Parse(totalVolume) / (decimal)Math.Pow(10, fp.TotalDecimalPlaces);
                fp.Nozzles[0].TotalPrice = fp.Nozzles[0].TotalPrice + ((fp.DispensedVolume * fp.Nozzles[0].UnitPrice) * (decimal)Math.Pow(10, fp.TotalDecimalPlaces));
                return true;
            }
            catch
            {
                return false;
            }
        }

        private int GetPriceData(byte[] dataBuffer)
        {
            try
            {
                if (dataBuffer.Length != 14)
                    return -1;

                string priceData = ASCIIEncoding.ASCII.GetString(dataBuffer.Take(4).ToArray());
                int price = int.Parse(priceData);
                return price;
            }
            catch
            {
                return -1;
            }
        }

        private bool EvaluateResponse(byte[] buffer)
        {
            if (buffer.Length == 1)
            {
                if (buffer[0] == ACK)
                    return true;
                return false;
            }
            int currentIndex = 0;
            while(true)
            {
                int start = buffer.ToList().IndexOf(SOH, currentIndex);
                if (start < 0 )
                    return false;
                int textStart = buffer.ToList().IndexOf(STX, start);
                if (textStart < 0)
                    return false;
                int textEnd = buffer.ToList().IndexOf(ETX, textStart);
                if (textEnd < 0)
                    return false;
                byte[] dataBuffer = buffer.Skip(start).Take(textEnd - start + 1).ToArray();
                byte lrc = buffer.Skip(start + dataBuffer.Length).Take(1).FirstOrDefault();
                if(lrc != CalculateLRC(dataBuffer.Skip(1).ToArray()))
                    return false;
                
                int fpAddress = int.Parse(ASCIIEncoding.ASCII.GetString(dataBuffer.Skip(1).Take(2).ToArray()));
                FuelPoint fp = this.FuelPoints.Where(f=>f.Address == fpAddress).FirstOrDefault();
                if(fp == null)
                    continue;

                Common.Enumerators.FuelPointStatusEnum oldStatus = fp.Status;
                string command = ASCIIEncoding.ASCII.GetString(dataBuffer.Skip(textStart + 1).Take(2).ToArray());
                switch(command)
                {
                    case "AQ":
                        fp.Status = Common.Enumerators.FuelPointStatusEnum.Nozzle;
                        fp.ActiveNozzle = fp.Nozzles[0];
                        fp.ActiveNozzleIndex = 0;
                        this.serialPort.Write(new byte[]{ ACK }, 0, 1);
                        
                        break;
                    case "PP":
                        fp.Status = Common.Enumerators.FuelPointStatusEnum.Work;
                        fp.ActiveNozzle = fp.Nozzles[0];
                        fp.ActiveNozzleIndex = 0;
                        this.GetDisplayData(fp, dataBuffer.Skip(textStart + 3).Take(14).ToArray());
                        break;
                    case "LK":
                        fp.Status = Common.Enumerators.FuelPointStatusEnum.TransactionCompleted;
                        fp.ActiveNozzle = fp.Nozzles[0];
                        fp.ActiveNozzleIndex = 0;
                        fp.Nozzles[0].QueryTotals = true;
                        this.serialPort.Write(new byte[]{ ACK }, 0, 1);
                        break;
                    case "TR":
                        bool displayOk = this.GetTrancactionEndsData(fp, dataBuffer.Skip(textStart + 3).Take(18).ToArray());
                        if(displayOk)
                            this.serialPort.Write(new byte[]{ ACK }, 0, 1);
                        else
                            this.serialPort.Write(new byte[]{ NAK }, 0, 1);
                        break;
                    case "PC":
                        if (this.GetPriceData(dataBuffer.Skip(textStart + 3).Take(4).ToArray()) == fp.Nozzles[0].UntiPriceInt)
                            fp.Nozzles[0].QuerySetPrice = false;
                        break;
                    case "CT":
                        bool totalsOk = this.GetTotalsData(fp, dataBuffer.Skip(textStart + 3).Take(9).ToArray());
                        if(totalsOk)
                        {
                            if(this.TotalsRecieved != null)
                                this.TotalsRecieved(this, new TotalsEventArgs(fp, 0, fp.Nozzles[0].TotalVolume, fp.Nozzles[0].TotalPrice));
                        }
                        fp.Status = Common.Enumerators.FuelPointStatusEnum.Idle;
                        break;
                }
                if(oldStatus != fp.Status)
                {
                    if(this.DispenserStatusChanged != null)
                        this.DispenserStatusChanged(this, new FuelPointValuesArgs(){ CurrentFuelPoint = fp, CurrentNozzleId = 0, Values = new FuelPointValues(){ ActiveNozzle = 0, Status = Common.Enumerators.FuelPointStatusEnum.Nozzle}});
                }
                currentIndex = textEnd;
                return true;
            }
        }

        private byte CalculateLRC(byte[] bytes)
        {
            int LRC = 0;
            for (int i = 0; i < bytes.Length; i++)
            {
                LRC -= bytes[i];
            }
            return (byte)LRC;

            //byte LRC = 0;
            //for (int i = 0; i < bytes.Length; i++)
            //{
            //    LRC ^= bytes[i];
            //}
            //return LRC;
        }
    }
}
