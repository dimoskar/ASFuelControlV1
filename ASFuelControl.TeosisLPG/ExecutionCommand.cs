using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASFuelControl.Common;
using System.Collections;

namespace ASFuelControl.TeosisLPG
{
    public class ExecutionCommand
    {
        public static bool LogIO { set; get; }
        public static bool LogSalesIO { set; get; }

        private int executionIndex = 0;

        public const byte PRESET = 0x7F;
        public const byte GETPUMPSTATUS = 0x80;
        public const byte PAID = 0x82;
        public const byte FILLINGREPORT = 0x83;
        public const byte SUSPENDRESUME = 0x84;
        public const byte GETTOTALS = 0x87;
        public const byte GETPUMPTOTALS = 0x17;
        public const byte AUTHORIZE = 0x88;
        public const byte SETPRICE = 0x8C;
        public const byte ERRORREPORT = 0x93;
        public const byte GETDCRTATUS = 0x94;
        public const byte GETPRICE = 0x99;
        public const byte DCRRESET = 0x94;
        public int DCRAddress { private set; get; } = 0;
        public int TX { set; get; }
        public FuelPoint FuelPump { set; get; }
        public int FuelPumpAddress { set; get; }
        public int NozzleIndex { set; get; } = -1;
        public byte CommandByte { set; get; }
        public byte[] NextCommand { set; get; }
        public byte[] Request { set; get; }
        public byte[] Response { set; get; }
        public CommandStatus Status { set; get; }
        public int ResponseNozzle { set; get; } = 0;
        public int DispenserStatus { set; get; }
        public int PriceTotals { set; get; }
        public int PresetAmount { set; get; }
        public int PresetVolume { set; get; }
        public int VolumeTotals { set; get; }
        public int Display { set; get; }
        public DateTime CreationTimeStamp { private set; get; }
        public bool TotalsRecieved { set; get; }
        public bool ReportRecieved = false;
        public bool StatusChanged { set; get; }
        public bool PriceSet { set; get; }
        public bool DisplaySet { set; get; }
        public decimal UnitPriceInt { set; get; }
        public bool Resume { set; get; }
        public int SalesVolume { set; get; }
        public int SalesPrice { set; get; }
        public int SalesUnitPrice { set; get; }
        public Controller.TeosisConnector Controller { set; get; }
        public Nozzle GetCurrentNozzle(FuelPoint fp)
        {
            if (this.NozzleIndex < 0)
                return null;
            return fp.Nozzles[this.NozzleIndex];
        }

        public event EventHandler StatusSet;
        public event EventHandler TotalsResponse;
        public event EventHandler SaleResponse;

        public ExecutionCommand()
        {
            this.CreationTimeStamp = DateTime.Now;
        }

        public bool Execute(System.IO.Ports.SerialPort port)
        {
            try
            {
                switch(this.CommandByte)
                {
                    case FILLINGREPORT:
                        return this.ExecuteCommonCommand(port, 1000);
                    case GETTOTALS:
                        return this.ExecuteCommonCommand(port, 2000);
                    case GETPUMPTOTALS:
                    case ERRORREPORT:
                        return this.ExecuteCommonCommand(port, 5000);
                    case AUTHORIZE:
                    case SUSPENDRESUME: 
                        return this.ExecuteCommonCommand(port, 500);

                    case GETPUMPSTATUS:
                    case GETDCRTATUS:
                        return this.ExecuteCommonCommand(port, 200);
                    case PAID:
                        return this.ExecuteInfoCommand(port, 500);
                    case SETPRICE:
                        return this.ExecuteInfoCommand(port, 500);
                }
                return false;
            }
            catch
            {
                ExecuteReset(port);
                System.Threading.Thread.Sleep(250);
                return false;
            }
        }

        public void CreateCommand(int dcrAddress)
        {
            this.DCRAddress = dcrAddress;
            switch (this.CommandByte)
            {
                case GETPUMPSTATUS:
                    {
                        var cmd = new List<byte> { ADR(), CTR(MasterControlByte.data, this.TX), this.CommandByte, 0x01 };
                        cmd.Add(this.PNG());
                        this.Request = cmd.ToArray().CRC().FIN();
                        this.NextCommand = this.Request;
                        break;
                    }
                case PAID:
                    {
                        var cmd = new List<byte> { ADR(), CTR(MasterControlByte.data, this.TX), this.CommandByte, 0x01 };
                        cmd.Add(this.PNG());
                        this.Request = cmd.ToArray().CRC().FIN();
                        this.NextCommand = this.Request;
                        break;
                    }
                case FILLINGREPORT:
                    {
                        var cmd = new List<byte> { ADR(), CTR(MasterControlByte.data, this.TX), this.CommandByte, 0x01 };
                        cmd.Add(this.PNG());
                        this.Request = cmd.ToArray().CRC().FIN();
                        this.NextCommand = this.Request;
                        break;
                    }
                case GETTOTALS:
                    {
                        var cmd = new List<byte> { ADR(), CTR(MasterControlByte.data, this.TX), this.CommandByte, 0x01 };
                        cmd.Add(this.PNG());
                        this.Request = cmd.ToArray().CRC().FIN();
                        this.NextCommand = this.Request;
                        break;
                    }
                case GETPUMPTOTALS:
                    {
                        var cmd = new List<byte> { ADR(), CTR(MasterControlByte.data, this.TX), GETTOTALS, 0x01 };
                        cmd.Add(this.PNGTotals());
                        this.Request = cmd.ToArray().CRC().FIN();
                        this.NextCommand = this.Request;
                        break;
                    }
                case SETPRICE:
                    {
                        byte size = (byte)(4 * this.FuelPump.Nozzles.Length);
                        var cmd = new List<byte> { ADR(), CTR(MasterControlByte.data, this.TX), this.CommandByte, size };
                        for (int i = 0; i < this.FuelPump.Nozzles.Length; i++)
                        {
                            var nz = this.FuelPump.Nozzles[i];
                            cmd.Add(this.PNG(i));
                            string priceString = Convert.ToString(nz.UntiPriceInt);
                            priceString = priceString.PadLeft(6, '0'); // Zero padding => product 4characters.
                            int p1 = Convert.ToInt16(priceString.Substring(0, 2));
                            int p2 = Convert.ToInt16(priceString.Substring(2, 2));
                            int p3 = Convert.ToInt16(priceString.Substring(4, 2));
                            cmd.Add((byte)ith(p1));
                            cmd.Add((byte)ith(p2));
                            cmd.Add((byte)ith(p3));
                        }
                        this.Request = cmd.ToArray().CRC().FIN();
                        this.NextCommand = this.Request;
                        break;
                    }
                case AUTHORIZE:
                    {
                        var cmd = new List<byte> { ADR(), CTR(MasterControlByte.data, this.TX), this.CommandByte, 0x07 };
                        cmd.Add(this.PNG());
                        string presetString = "999999";
                        if (this.PresetAmount <= 0 && this.PresetVolume <= 0)
                            cmd.Add(0x00);
                        else if (this.PresetVolume > 0)
                        {
                            presetString = Convert.ToString(this.PresetVolume);
                            cmd.Add(0x02);
                        }
                        else if (this.PresetAmount > 0)
                        {
                            presetString = Convert.ToString(this.PresetAmount);
                            cmd.Add(0x01);
                        }
                        else
                            cmd.Add(0x00);


                        presetString = presetString.PadLeft(8, '0'); // Zero padding => product 4characters.
                        int p1 = Convert.ToInt16(presetString.Substring(0, 2));
                        int p2 = Convert.ToInt16(presetString.Substring(2, 2));
                        int p3 = Convert.ToInt16(presetString.Substring(4, 2));
                        int p4 = Convert.ToInt16(presetString.Substring(6, 2));
                        cmd.Add((byte)ith(p1));
                        cmd.Add((byte)ith(p2));
                        cmd.Add((byte)ith(p3));
                        cmd.Add((byte)ith(p4));
                        cmd.Add(0x00);
                        this.Request = cmd.ToArray().CRC().FIN();
                        this.NextCommand = this.Request;
                        break;
                    }
                case SUSPENDRESUME:
                    {
                        var cmd = new List<byte> { ADR(), CTR(MasterControlByte.data, this.TX), this.CommandByte, 0x02 };
                        cmd.Add(this.PNG());
                        if (this.Resume)
                            cmd.Add(0x01);
                        else
                            cmd.Add(0x00);
                        this.Request = cmd.ToArray().CRC().FIN();
                        this.NextCommand = this.Request;
                        break;
                    }
                case GETDCRTATUS:
                    {
                        var cmd = new List<byte> { ADR(), CTR(MasterControlByte.data, this.TX), this.CommandByte, 0x02 };
                        cmd.Add((byte)this.DCRAddress);
                        cmd.Add(0x00);
                        this.Request = cmd.ToArray().CRC().FIN();
                        this.NextCommand = this.Request;
                        break;
                    }
                case ERRORREPORT:
                    {
                        var cmd = new List<byte> { ADR(), CTR(MasterControlByte.data, this.TX), this.CommandByte, 0x01 };
                        cmd.Add((byte)this.FuelPumpAddress);
                        this.Request = cmd.ToArray().CRC().FIN();
                        this.NextCommand = this.Request;
                        break;
                    }
            }
        }

        public void SetNewStatus()
        {
            if (this.Status == CommandStatus.Initialized)
                this.Status = CommandStatus.Sent;
            else if (this.Status == CommandStatus.Sent && this.CommandByte != SETPRICE)
                this.Status = CommandStatus.Poll;
        }

        //public void EvaluateResponse(byte[] response)
        //{
        //    if (this.Status == CommandStatus.Sent)
        //    {
        //        if (AreEqual(response, ACK(this.TX)))
        //        {
        //            if (this.CommandByte != SETPRICE)
        //                this.SendPoll();
        //            else
        //                this.Status = CommandStatus.Executed;
        //        }
        //    }
        //    else if (this.Status == CommandStatus.Poll)
        //    {
        //        if (response[0] != this.Request[0])
        //            return;

        //        if (AreEqual(response, EOT(this.TX)) || (this.TX > 0 && AreEqual(response, EOT(this.TX - 1))))
        //        {
        //            executionIndex++;
        //            if (executionIndex > 50)
        //            {
        //                this.SendNack();
        //                this.Status = CommandStatus.Executed;
        //            }
        //            //
        //            //this.Status = CommandStatus.Executed;
        //            return;
        //        }

        //        if (this.CommandByte == GETPUMPSTATUS || this.CommandByte == AUTHORIZE || this.CommandByte == SUSPENDRESUME || this.CommandByte == PAID)
        //            EvaluateStatus(response);
        //        else if (this.CommandByte == SETPRICE)
        //            PriceSet = true;

        //        var cb = response[2];
        //        if (cb == GETTOTALS)
        //            EvaluateTotals(response);
        //        else if (cb == GETDCRTATUS)
        //            EvaluateDCRStatus(response);
        //        else if (cb == FILLINGREPORT)
        //        {
        //            if (System.IO.File.Exists("Teosis_sales.log"))
        //                System.IO.File.AppendAllLines("Teosis_sales.log", new string[] { BitConverter.ToString(response) });
        //            EvaluateReport(response);
        //        }
        //        this.SendAck();
        //        this.Status = CommandStatus.Executed;
        //    }

        //    if (AreEqual(response, ACK(this.TX)))
        //    {
        //        this.TX++;
        //    }
        //}

        public void SendPoll()
        {
            this.NextCommand = this.POLL(this.TX);
        }

        public void SendAck()
        {
            this.NextCommand = this.ACK(this.TX);
        }

        public void SendNack()
        {
            this.NextCommand = this.NAK(this.TX);
        }

        private bool ExecuteCommonCommand(System.IO.Ports.SerialPort port, int totalWait)
        {
            try
            {
                byte[] response;
                this.WriteRead(port, true, 50, out response);
                if (!AreEqual(response, this.ACK(this.TX)))
                    return false;
                DateTime dtNow = DateTime.Now;
                while (true)
                {
                    if (DateTime.Now.Subtract(dtNow).TotalMilliseconds > totalWait)
                        return false;

                    this.NextCommand = this.POLL(this.TX);
                    System.Threading.Thread.Sleep(10);
                    this.WriteRead(port, true, 25, out response);
                    if (response.Length == 0)
                    {
                        continue;
                    }

                    var responses = SplitResponses(response);
                    bool shouldContinue = false;
                    foreach(var resp in responses)
                    {
                        if (AreEqual(resp, this.EOT(this.TX)))
                        {
                            shouldContinue = true;
                            continue;
                        }
                        if (EvaluateResponse(resp))
                        {
                            this.NextCommand = this.ACK(this.TX);
                            System.Threading.Thread.Sleep(10);
                            this.WriteRead(port, false, 25, out response);
                            return true;
                        }
                        else
                        {
                            this.NextCommand = this.ACK(this.TX);
                            System.Threading.Thread.Sleep(10);
                            this.WriteRead(port, false, 25, out response);
                            continue;
                        }
                    }
                    if(!shouldContinue)
                        return false;
                    System.Threading.Thread.Sleep(50);
                }
            }
            finally
            {
                if (System.IO.File.Exists("TeosisIO.log"))
                {
                    string str = "-------------------------------------------------------------";
                    System.IO.File.AppendAllLines("TeosisIO.log", new string[] { str });
                }
            }
        }

        private bool ExecuteInfoCommand(System.IO.Ports.SerialPort port, int totalWaitTime)
        {
            try
            {
                byte[] response;
                this.WriteRead(port, true, 125, out response);
                if (!AreEqual(response, this.ACK(this.TX)))
                    return false;
                DateTime dtNow = DateTime.Now;
                while (true)
                {
                    this.NextCommand = this.POLL(this.TX);
                    this.WriteRead(port, true, 125, out response);
                    if (response.Length == 0)
                    {
                        if (DateTime.Now.Subtract(dtNow).TotalMilliseconds > totalWaitTime)
                            return false;
                        continue;
                    }
                    if (AreEqual(response, this.EOT(this.TX)))
                        return true;
                    return false;
                }
            }
            finally
            {
                if (System.IO.File.Exists("TeosisIO.log"))
                {
                    string str = "-------------------------------------------------------------";
                    System.IO.File.AppendAllLines("TeosisIO.log", new string[] { str });
                }
            }
        }

        private bool EvaluateResponse(byte[] response)
        {
            bool success = false;
            if (response.Length < 3)
                return false;
            if (response[0] != this.Request[0])
                success = false;

            var cb = response[2];
            if(!((this.CommandByte == GETPUMPTOTALS && cb == GETTOTALS) || cb == this.CommandByte))
            {
                return false;
            }
            if (cb == GETPUMPSTATUS || cb == AUTHORIZE || cb == SUSPENDRESUME || cb == PAID || cb == SUSPENDRESUME)
                success = EvaluateStatus(response);
            else if (cb == GETTOTALS)
                success = EvaluateTotals(response);
            else if (cb == GETDCRTATUS)
                success = EvaluateDCRStatus(response);
            else if (cb == FILLINGREPORT)
            {
                if (System.IO.File.Exists("Teosis_sales.log"))
                    System.IO.File.AppendAllLines("Teosis_sales.log", new string[] { BitConverter.ToString(response) });
                success = EvaluateReport(response);
            }
            else if(cb == ERRORREPORT)
            {
                success = evaluateErrorReport(response);
            }
            return success;
        }

        private bool WriteRead(System.IO.Ports.SerialPort port, out byte[] response)
        {
            return this.WriteRead(port, true, out response);
        }

        private bool WriteRead(System.IO.Ports.SerialPort port, bool waitResponse, out byte[] response)
        {
            return this.WriteRead(port, true, 50, out response);
        }

        private bool WriteRead(System.IO.Ports.SerialPort port, bool waitResponse, int responseWaitTime, out byte[] response)
        {
            response = new byte[] { };
            try
            {
                if (System.IO.File.Exists("TeosisIO.log"))
                {
                    string str = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff") + " RX : " + BitConverter.ToString(this.NextCommand);
                    System.IO.File.AppendAllLines("TeosisIO.log", new string[] { str });
                }
                if(System.IO.File.Exists("TeosisSalesIO.log") && this.CommandByte == FILLINGREPORT)
                {
                    string str = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff") + " RX : " + BitConverter.ToString(this.NextCommand);
                    System.IO.File.AppendAllLines("TeosisIO.log", new string[] { str });
                }
                port.Write(this.NextCommand, 0, this.NextCommand.Length);
                if (!waitResponse)
                    return true;

                System.Threading.Thread.Sleep(responseWaitTime);
                DateTime dt = new DateTime();
                List<byte> buffer = new List<byte>();
                while (true)
                {
                    while (port.BytesToRead == 0)
                    {
                        if (DateTime.Now.Subtract(dt).TotalMilliseconds > 200)
                        {
                            response = new byte[] { };
                            return false;
                        }
                        System.Threading.Thread.Sleep(10);
                    }
                    response = new byte[port.BytesToRead];
                    port.Read(response, 0, port.BytesToRead);
                    buffer.AddRange(response);
                    if (buffer.Last() == 0xfa)
                    {
                        response = buffer.ToArray();
                        return true;
                    }
                    System.Threading.Thread.Sleep(10);
                }
            }
            catch
            {
                return false;
            }
            finally
            {
                if (System.IO.File.Exists("TeosisIO.log"))
                {
                    
                    string str = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff") + " TX : " + BitConverter.ToString(response);
                    System.IO.File.AppendAllLines("TeosisIO.log", new string[] { str });
                }
                if (System.IO.File.Exists("TeosisSalesIO.log") && this.CommandByte == FILLINGREPORT)
                {
                    string str = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff") + " TX : " + BitConverter.ToString(response);
                    System.IO.File.AppendAllLines("TeosisIO.log", new string[] { str });
                }
            }
        }

        private byte[][] SplitResponses(byte[] buffer)
        { 
            int lastIndex = 0;
            List<byte[]> responses = new List<byte[]>();
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i] == 0xFA)
                {
                    if (i < buffer.Length - 1 && buffer[i + 1] != ADR())
                        continue;
                    var response = buffer.Skip(lastIndex).Take(i + 1 - lastIndex).ToArray();
                    responses.Add(response);
                    lastIndex = i + 1;
                }
            }
            return responses.ToArray();
        }

        private bool ExecuteReset(System.IO.Ports.SerialPort port)
        {
            port.Write(this.NextCommand, 0, this.NextCommand.Length);
            System.Threading.Thread.Sleep(30);

            while (port.BytesToRead == 0)
            {
                System.Threading.Thread.Sleep(10);
            }
            byte[] resetResponse = new byte[port.BytesToRead];
            port.Read(resetResponse, 0, port.BytesToRead);
            return AreEqual(resetResponse, ACK(this.TX));
        }

        private byte ADR()
        {
            return (byte)(80 + this.DCRAddress);
        }

        private byte CTR(MasterControlByte type, int tx)
        {
            return (byte)((int)type << 4 | (byte)tx);

        }

        private byte PNG()
        {
            byte msb, lsb;
            msb = (byte)this.FuelPumpAddress;
            lsb = (byte)(this.NozzleIndex + 1);
            msb = (byte)(msb << 4);
            return (byte)(lsb | msb);
        }

        private byte PNG(int nzIndex)
        {
            byte msb, lsb;
            msb = (byte)this.FuelPumpAddress;
            lsb = (byte)(nzIndex + 1);
            msb = (byte)(msb << 4);
            return (byte)(lsb | msb);
        }

        private byte PNGTotals()
        {
            byte msb, lsb;
            msb = (byte)(this.FuelPumpAddress + 4);
            lsb = (byte)(this.NozzleIndex + 1);
            msb = (byte)(msb << 4);
            return (byte)(lsb | msb);
        }

        private byte[] POLL(int tx)
        {
            return new byte[] { ADR(), CTR(MasterControlByte.poll, tx), 0xFA };

        }

        private byte[] ACK(int tx)
        {
            return new byte[] { ADR(), CTR(MasterControlByte.ack, tx), 0xFA };

        }

        private byte[] NAK(int tx)
        {
            return new byte[] { ADR(), CTR(MasterControlByte.nak, tx), 0xFA };

        }

        private byte[] EOT(int tx)
        {
            return new byte[] { ADR(), CTR(MasterControlByte.eot, tx), 0xFA };

        }

        private static bool AreEqual(byte[] a1, byte[] a2)
        {
            if (a2.Length > a1.Length)
                return false;
            if (a1.Length == a2.Length)
                return StructuralComparisons.StructuralEqualityComparer.Equals(a1, a2);

            for (int i=0; i < a1.Length - a2.Length; i++)
            {
                var a = a1.Skip(i).Take(a2.Length).ToArray();
                if (StructuralComparisons.StructuralEqualityComparer.Equals(a, a2))
                    return true;
            }
            return false;
        }

        private static int ith(int num)
        {
            return 16 * (num / 10) + (num % 10);
        }// hex = integerToHex(int)

        private bool evaluateErrorReport(byte[] buffer)
        {
            if (!buffer.validateCRC())
            {
                if (System.IO.File.Exists("TeosisIO.log"))
                {
                    string str = "CRC ERROR";
                    System.IO.File.AppendAllLines("TeosisIO.log", new string[] { str });
                }
                return false;
            }
            if (buffer.Length <= 6)
                return false;
            int error = buffer[6];
            return true;
        }
        private bool EvaluateStatus(byte[] status)
        {
            if (status.validateCRC())
            {
                int nozzle = status[4] & 0x0F;
                int address = status[4] >> 4;

                byte flag = status[5];
                byte[] acceptFlags = new byte[] { 4, 8 };
                var fp = this.Controller.FuelPoints.FirstOrDefault(f => f.Address == address);
                if (fp != this.FuelPump && !acceptFlags.Contains(flag))
                {

                    if (System.IO.File.Exists("TeosisIO.log"))
                    {
                        {
                            string str = string.Format("ADDRESS MISMATCH Should: {0}  Got: {1}", this.FuelPumpAddress, address);
                            System.IO.File.AppendAllLines("TeosisIO.log", new string[] { str });
                        }
                    }
                    return false;
                }
                else if(fp != this.FuelPump && acceptFlags.Contains(flag))
                {
                    if (System.IO.File.Exists("TeosisIO.log"))
                    {
                        {
                            string str = string.Format("ADDRESS MISMATCH Should: {0}  Got: {1}", this.FuelPumpAddress, address);
                            System.IO.File.AppendAllLines("TeosisIO.log", new string[] { str });
                        }
                    }
                }
                this.FuelPump = fp;
                

                DispenserStatus = flag;
                this.StatusChanged = true;
                var nz = nozzle <= 0 ? null : fp.Nozzles[nozzle - 1];
                fp.ActiveNozzle = nz;
                fp.ActiveNozzleIndex = nozzle - 1;

                if (flag == 0 || flag == 5)
                {
                    this.ResponseNozzle = 0;
                    if (this.StatusSet != null)
                        this.StatusSet(this, new EventArgs());
                    return true;
                }
                if (flag == 2)
                {
                    this.Display = (int)status.skip(6).takeToDecimal(4);

                    var amount = (decimal)this.Display / (decimal)Math.Pow(10, fp.AmountDecimalPlaces);
                    decimal up = fp.Nozzles[nozzle - 1].UnitPrice;
                    var volume = decimal.Round(amount / up, fp.VolumeDecimalPlaces);
                    fp.DispensedAmount = amount;
                    fp.DispensedVolume = volume;

                    this.DisplaySet = true;
                }
                this.ResponseNozzle = nozzle;
                if (this.StatusSet != null)
                    this.StatusSet(this, new EventArgs());
                return true;
            }
            else
            {
                if (System.IO.File.Exists("TeosisIO.log"))
                {
                    string str = "CRC ERROR";
                    System.IO.File.AppendAllLines("TeosisIO.log", new string[] { str });
                }
                return false;
            }
        }

        private bool EvaluateTotals(byte[] buffer)
        {
            //50-31-87-0F-51-00-00-99-64-00-00-00-01-68-62-00-00-00-00-5D-A2-03-FA
            try
            {
                if (!buffer.validateCRC())
                {
                    if (System.IO.File.Exists("TeosisIO.log"))
                    {
                        string str = "CRC ERROR";
                        System.IO.File.AppendAllLines("TeosisIO.log", new string[] { str });
                    }
                    return false;
                }
                int nozzle = buffer[4] & 0x0F;
                int address = buffer[4] >> 4;
                if(address > 4)
                    address = address - 4;

                //if (address != this.FuelPumpAddress)
                //{
                //    if(LogIO)
                //    {
                //        {
                //            string str = string.Format("ADDRESS MISMATCH Should: {0}  Got: {1}", this.FuelPumpAddress, address);
                //            System.IO.File.AppendAllLines("TeosisIO.log", new string[] { str });
                //        }
                //    }
                //    return false;
                //}
                var fp = this.Controller.FuelPoints.FirstOrDefault(f => f.Address == address);
                if (fp == null)
                    return false;
                this.FuelPump = fp;

                var ifp = this.Controller.InternalPumps.First(f => f.Address == address);
                var inz = nozzle <= 0 ? null : ifp.Nozzles.First(n => n.NozzleIndex == nozzle);

                var nz = nozzle <= 0 ? null : fp.Nozzles[nozzle - 1];
                if (nz == null)
                    return false;
                this.NozzleIndex = nozzle - 1;

                this.VolumeTotals = (int)buffer.skip(5).takeToDecimal(4);
                this.PriceTotals = (int)buffer.skip(9).takeToDecimal(6);
                if (this.VolumeTotals == 0)
                    this.VolumeTotals = 1;

                inz.InitialVolume = this.VolumeTotals;
                inz.InitialPrice = this.PriceTotals;
                inz.SalesVolume = 0;
                inz.SalesPrice = 0;

                nz.TotalVolume = inz.TotalVolume;
                nz.TotalPrice = inz.TotalPrice;
                nz.QueryTotals = false;
                if (fp.Nozzles.Where(n => n.QueryTotals).Count() == 0)
                    fp.Initialized = true;
                //if (address == this.FuelPumpAddress)
                //    nz.QueryTotals = false;
                //TotalsRecieved = true;
                //if (this.TotalsResponse != null)
                //    this.TotalsResponse(this, new EventArgs());
            }
            catch (Exception ex)
            {
                TotalsRecieved = false;
                return false;
            }
            return true;
        }

        private bool EvaluateReport(byte[] buffer)
        {
            //50-31-83-0F-51-00-00-99-64-00-00-00-01-68-62-00-00-00-00-5D-A2-03-FA
            try
            {
                if (!buffer.validateCRC())
                {
                    if (System.IO.File.Exists("TeosisIO.log"))
                    {
                        string str = "CRC ERROR";
                        System.IO.File.AppendAllLines("TeosisIO.log", new string[] { str });
                    }
                    return false;
                }

                int nozzle = buffer[4] & 0x0F;
                int address = buffer[4] >> 4;

                var fp = this.Controller.FuelPoints.FirstOrDefault(f => f.Address == address);
                if (fp != this.FuelPump)
                {
                    if (System.IO.File.Exists("TeosisIO.log"))
                    {
                        {
                            string str = string.Format("ADDRESS MISMATCH Should: {0}  Got: {1}", this.FuelPumpAddress, address);
                            System.IO.File.AppendAllLines("TeosisIO.log", new string[] { str });
                        }
                    }
                    return false;
                }
                
                var nz = nozzle <= 0 ? null : fp.Nozzles[nozzle - 1];

                this.NozzleIndex = nozzle - 1;

                this.SalesPrice = (int)buffer.skip(6).takeToDecimal(4);
                this.SalesVolume = (int)buffer.skip(10).takeToDecimal(4);
                this.SalesUnitPrice = (int)buffer.skip(14).takeToDecimal(3);

                var ifp = this.Controller.InternalPumps.First(f => f.Address == address);
                var inz = nozzle <= 0 ? null : ifp.Nozzles.First(n => n.NozzleIndex == nozzle);
                inz.SalesVolume = inz.SalesVolume + this.SalesVolume;
                inz.SalesPrice = inz.SalesPrice + this.SalesPrice;
                this.VolumeTotals = (int)inz.TotalVolume;
                this.PriceTotals = (int)inz.TotalPrice;

                var amount = (decimal)this.SalesPrice / (decimal)Math.Pow(10, fp.AmountDecimalPlaces);
                var volume = (decimal)this.SalesVolume / (decimal)Math.Pow(10, fp.VolumeDecimalPlaces);
                fp.DispensedAmount = amount;
                fp.DispensedVolume = volume;

                ReportRecieved = true;
                if (this.SaleResponse != null)
                    this.SaleResponse(this, new EventArgs());
            }
            catch
            {
                ReportRecieved = false;
                return false;
            }
            return true;
        }

        private bool EvaluateDCRStatus(byte[] buffer)
        {
            if (!buffer.validateCRC())
            {
                if (System.IO.File.Exists("TeosisIO.log"))
                {
                    string str = "CRC ERROR";
                    System.IO.File.AppendAllLines("TeosisIO.log", new string[] { str });
                }
                return false;
            }

            if (buffer.Length >= 16)
                return true;
            return false;
        }
    }
}
