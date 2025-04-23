using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeosisController
{
    public class ExecutionCommand
    {
        public const byte PRESET = 0x7F;
        public const byte GETPUMPSTATUS = 0x80;
        public const byte PAID = 0x82;
        public const byte FILLINGREPORT = 0x83;
        public const byte SUSPENDRESUME = 0x84;
        public const byte GETTOTALS = 0x87;
        public const byte AUTHORIZE = 0x88;
        public const byte SETPRICE = 0x8C;
        public const byte GETDCRTATUS = 0x94;
        public const byte GETPRICE = 0x99;
        public int DCRAddress { private set; get; } = 0;
        public int TX { set; get; }
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
        public bool DisplaySet { set; get; }
        public decimal UnitPriceInt { set; get; }
        public bool Resume { set; get; }
        public int SalesVolume { set; get; }
        public int SalesPrice { set; get; }
        public int SalesUnitPrice { set; get; }

        public ExecutionCommand()
        {
            this.CreationTimeStamp = DateTime.Now;
        }

        public void CreateCommand(int dcrAddress)
        {
            this.DCRAddress = dcrAddress;
            switch (this.CommandByte)
            {
                case GETPUMPSTATUS:
                    {
                        var cmd = new List<byte> { ADR(), CTR(MasterControlByte.data), this.CommandByte, 0x01 };
                        cmd.Add(this.PNG());
                        this.Request = cmd.ToArray().CRC().FIN();
                        this.NextCommand = this.Request;
                        break;
                    }
                case PAID:
                    {
                        var cmd = new List<byte> { ADR(), CTR(MasterControlByte.data), this.CommandByte, 0x01 };
                        cmd.Add(this.PNG());
                        this.Request = cmd.ToArray().CRC().FIN();
                        this.NextCommand = this.Request;
                        break;
                    }
                case FILLINGREPORT:
                    {
                        var cmd = new List<byte> { ADR(), CTR(MasterControlByte.data), this.CommandByte, 0x01 };
                        cmd.Add(this.PNG());
                        this.Request = cmd.ToArray().CRC().FIN();
                        this.NextCommand = this.Request;
                        break;
                    }
                case GETTOTALS:
                    {
                        var cmd = new List<byte> { ADR(), CTR(MasterControlByte.data), this.CommandByte, 0x01 };
                        cmd.Add(this.PNGTotals());
                        this.Request = cmd.ToArray().CRC().FIN();
                        this.NextCommand = this.Request;
                        break;
                    }
                case SETPRICE:
                    {
                        var cmd = new List<byte> { ADR(), CTR(MasterControlByte.data), this.CommandByte, 0x04 };
                        cmd.Add(this.PNG());
                        string priceString = Convert.ToString(this.UnitPriceInt);
                        priceString = priceString.PadLeft(6, '0'); // Zero padding => product 4characters.
                        int p1 = Convert.ToInt16(priceString.Substring(0, 2));
                        int p2 = Convert.ToInt16(priceString.Substring(2, 2));
                        int p3 = Convert.ToInt16(priceString.Substring(4, 2));
                        cmd.Add((byte)ith(p1));
                        cmd.Add((byte)ith(p2));
                        cmd.Add((byte)ith(p3));

                        this.Request = cmd.ToArray().CRC().FIN();
                        this.NextCommand = this.Request;
                        break;
                    }
                case AUTHORIZE:
                    {
                        var cmd = new List<byte> { ADR(), CTR(MasterControlByte.data), this.CommandByte, 0x07 };
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
                        var cmd = new List<byte> { ADR(), CTR(MasterControlByte.data), this.CommandByte, 0x01 };
                        cmd.Add(this.PNG());
                        if(this.Resume)
                            cmd.Add(0x01);
                        else
                            cmd.Add(0x00);
                        this.Request = cmd.ToArray().CRC().FIN();
                        this.NextCommand = this.Request;
                        break;
                    }
                case GETDCRTATUS:
                    {
                        var cmd = new List<byte> { ADR(), CTR(MasterControlByte.data), this.CommandByte, 0x02 };
                        cmd.Add((byte)this.DCRAddress);
                        cmd.Add(0x00);
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

        public void ExecuteStatusCommand()
        {
            if (this.Status == CommandStatus.Initialized)
            {
                this.NextCommand = Request;
            }
        }

        public void EvaluateResponse(byte[] response)
        {
            if (this.Status == CommandStatus.Sent)
            {
                if (AreEqual(response, ACK(this.TX)))
                {
                    if(this.CommandByte != SETPRICE)
                        this.SendPoll();
                    else
                        this.Status = CommandStatus.Executed;
                }
            }
            else if (this.Status == CommandStatus.Poll)
            {
                if (response[0] != this.Request[0])
                    return;
                if (this.CommandByte == GETPUMPSTATUS || this.CommandByte == AUTHORIZE || this.CommandByte == SUSPENDRESUME || this.CommandByte == PAID)
                    EvaluateStatus(response);
                else if (this.CommandByte == GETTOTALS)
                    EvaluateTotals(response);
                else if (this.CommandByte == GETDCRTATUS)
                    EvaluateDCRStatus(response);
                else if (this.CommandByte == FILLINGREPORT)
                    EvaluateReport(response);
                this.SendAck();
                this.Status = CommandStatus.Executed;
            }

            if (AreEqual(response, ACK(this.TX)))
            {
                this.TX++;
            }
        }

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

        private byte ADR()
        {
            return (byte)(80 + this.DCRAddress);
        }

        private byte CTR(MasterControlByte type)
        {
            return (byte)((int)type << 4 | (byte)this.TX);

        }

        private byte PNG()
        {
            byte msb, lsb;
            msb = (byte)this.FuelPumpAddress;
            lsb = (byte)(this.NozzleIndex + 1);
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
            return new byte[] { ADR(), CTR(MasterControlByte.poll), 0xFA };

        }

        private byte[] ACK(int tx)
        {
            return new byte[] { ADR(), CTR(MasterControlByte.ack), 0xFA };

        }

        private byte[] NAK(int tx)
        {
            return new byte[] { ADR(), CTR(MasterControlByte.nak), 0xFA };

        }

        private byte[] EOT(int tx)
        {
            return new byte[] { ADR(), CTR(MasterControlByte.eot), 0xFA };

        }

        private bool AreEqual(byte[] a1, byte[] a2)
        {
            return StructuralComparisons.StructuralEqualityComparer.Equals(a1, a2);
        }

        private bool EvaluateStatus(byte[] status)
        {
            if (status.validateCRC())
            {
                int nozzle = status[4] & 0x0F;
                byte flag = status[5];
                
                DispenserStatus = flag;
                this.StatusChanged = true;
                
                if (flag == 0 || flag == 5)
                {
                    this.ResponseNozzle = 0;
                    return true;
                }
                if(flag == 2)
                {
                    this.Display = (int)status.skip(6).takeToDecimal(4);
                    this.DisplaySet = true;
                }
                this.ResponseNozzle = nozzle;
                return true;
            }
            else
                return false;
        }

        private bool EvaluateTotals(byte[] buffer)
        {
            //50-31-87-0F-51-00-00-99-64-00-00-00-01-68-62-00-00-00-00-5D-A2-03-FA
            try
            {
                int nozzle = buffer[4] & 0x0F;
                this.NozzleIndex = nozzle - 1;

                this.VolumeTotals = (int)buffer.skip(5).takeToDecimal(4);
                this.PriceTotals = (int)buffer.skip(9).takeToDecimal(6);
                TotalsRecieved = true;
            }
            catch
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
                int nozzle = buffer[4] & 0x0F;
                this.NozzleIndex = nozzle - 1;

                this.SalesPrice = (int)buffer.skip(6).takeToDecimal(4);
                this.SalesVolume = (int)buffer.skip(10).takeToDecimal(4);
                this.SalesUnitPrice = (int)buffer.skip(14).takeToDecimal(3);
                ReportRecieved = true;
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
            if (buffer.Length >= 16)
                return true;
            return false;
        }

        private int ith(int num)
        {
            return 16 * (num / 10) + (num % 10);
        }// hex = integerToHex(int)
    }

    public enum CommandStatus
    {
        Initialized = 0,
        Sent,
        Poll,
        Executed
    }

    public enum MasterControlByte
    {
        poll = 2,
        data = 3,
        iap = 4,
        nak = 5,
        eot = 7,
        ack = 12,
        ackpoll = 14,
    }
}
