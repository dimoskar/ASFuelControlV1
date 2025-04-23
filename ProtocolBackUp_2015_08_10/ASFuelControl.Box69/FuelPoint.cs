using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace ASFuelControl.Box69
{
    public class FuelPoint
    {
        public event EventHandler<StatusChangedEventArgs> StatusChanged;
        public event EventHandler DataChanged;
        public event EventHandler<TotalsEventArgs> TotalsRecieved;

        List<Nozzle> nozzles = new List<Nozzle>();
        Common.Enumerators.FuelPointStatusEnum status = Common.Enumerators.FuelPointStatusEnum.Offline;
        Common.Enumerators.FuelPointStatusEnum dispenserStatus = Common.Enumerators.FuelPointStatusEnum.Offline;
        private byte[] identity = null;
        private DateTime lastCommand = DateTime.Now.AddMilliseconds(-1000);
        private DateTime lastOffline = DateTime.Now.AddMilliseconds(-15000);
        private bool noAnswer = false;

        public byte[] Identity
        {
            set
            {
                this.identity = value;
                this.Channel = BitConverter.ToInt32(this.identity, 0);
            }
            get { return this.identity; }
        }

        public int Channel
        {
            set;
            get;
        }

        public Nozzle[] Nozzles
        {
            get { return this.nozzles.ToArray(); }
        }
        public bool QueryAuthorize { set; get; }
        public bool QueryTotals { set; get; }
        public int ErrorCount { set; get; }
        public bool QueryStop { set; get; }
        public bool QuerySetPrice { set; get; }
        public bool QueryNozzle { set; get; }
        public Nozzle ActiveNozzle { set; get; }
        public Nozzle LastNozzle { set; get; }
        public bool Initialized { set; get; }
        public bool NoAnswer
        {
            set 
            {
                if (value == noAnswer)
                {
                    this.lastOffline = DateTime.Now;
                }
                this.noAnswer = value; 
            }
            get { return noAnswer; }
        }
        public bool InitializeSent { set; get; }
        public bool PriceSet { set; get; }
        public bool PriceSetSent { set; get; }
        public int AddressId { set; get; }
        public Common.Enumerators.FuelPointStatusEnum PreviousStatus { set; get; }
        public Common.Enumerators.FuelPointStatusEnum Status
        {
            set
            {
                if (status == value)
                    return;
                //if (value == Common.Enumerators.FuelPointStatusEnum.Offline)
                //{
                //    if (DateTime.Now.Subtract(this.lastOffline).TotalSeconds < 10)
                //        return;
                //    else
                //        this.lastOffline = DateTime.Now;
                //}
                this.PreviousStatus = this.status;
                this.status = value;
                if (this.status == Common.Enumerators.FuelPointStatusEnum.Offline)
                    this.Initialized = false;
                else
                    this.Initialized = true;
               

               //if(this.StatusChanged!= null)
               //    this.StatusChanged(this, new StatusChangedEventArgs(this.status, this.PreviousStatus));
            }
            get { return status; }
        }

        public Common.Enumerators.FuelPointStatusEnum DispenserStatus
        {
            set
            {
                if (dispenserStatus == value)
                    return;
                //if (value == Common.Enumerators.FuelPointStatusEnum.Offline)
                //{
                //    if (DateTime.Now.Subtract(this.lastOffline).TotalSeconds < 10)
                //        return;
                //    else
                //        this.lastOffline = DateTime.Now;
                //}

                this.dispenserStatus = value;
                if (this.dispenserStatus == Common.Enumerators.FuelPointStatusEnum.Idle && (this.status == Common.Enumerators.FuelPointStatusEnum.Work ||
                    this.status == Common.Enumerators.FuelPointStatusEnum.TransactionCompleted || this.status == Common.Enumerators.FuelPointStatusEnum.TransactionStopped ||
                    this.status == Common.Enumerators.FuelPointStatusEnum.Ready))
                {
                    this.QueryTotals = true;
                }
                else if (this.dispenserStatus == Common.Enumerators.FuelPointStatusEnum.Nozzle && this.status == Common.Enumerators.FuelPointStatusEnum.Idle)
                {
                    this.QueryTotals = true;
                }
                else
                    this.Status = this.dispenserStatus;
            }
            get { return dispenserStatus; }
        }

        //public void AddNozzle(Nozzle nozzle)
        //{
        //    this.nozzles.Add(nozzle);
        //    nozzle.ParentFuelPoint = this;
        //}

        public decimal LastSaleVolume { set; get; }
        public decimal LastSalePrice { set; get; }
        public decimal LastSaleUnitPrice { set; get; }

        public int PriceDecimalPlaces { set; get; }
        public int VolumeDecimalPlaces { set; get; }

        public FuelPoint()
        {
            for (int i = 0; i < 6; i++)
            {
                Nozzle nozzle = new Nozzle();
                nozzle.NozzleIndex = i + 1;
                nozzle.ParentFuelPoint = this;
                this.nozzles.Add(nozzle);
                nozzle.QueryTotals = true;
            }
            this.QueryTotals = true;
        }

        public FuelPoint(int nozzleCount)
        {
            for (int i = 0; i < nozzleCount; i++)
            {
                Nozzle nozzle = new Nozzle();
                nozzle.NozzleIndex = i + 1;
                nozzle.ParentFuelPoint = this;
                this.nozzles.Add(nozzle);
                nozzle.QueryTotals = true;
            }
            this.QueryTotals = true;
        }

        public OpenCommand CreateNextCommand()
        {
            DateTime dtNow = DateTime.Now;
            OpenCommand nextCommand = null;
            try
            {
                if (this.noAnswer && DateTime.Now.Subtract(this.lastOffline).TotalSeconds < 10)
                    nextCommand = null;
                else if(this.noAnswer)
                    nextCommand = this.GetFuelPointStatus();
                else if (this.QueryStop)
                    nextCommand = this.StopFuelPoint();
                else if (this.QuerySetPrice && this.Nozzles.Where(n=>n.UnitPrice > 0).Count() > 0)
                    nextCommand = this.SetPrices();
                else if (this.QueryTotals)
                {
                    if (this.ActiveNozzle != null)
                        nextCommand = this.GetNozzleTotals(this.ActiveNozzle);
                    else if (this.LastNozzle != null)
                        nextCommand = this.GetNozzleTotals(this.LastNozzle);
                    else
                        this.QueryTotals = false;
                    //if (this.status != Common.Enumerators.FuelPointStatusEnum.Idle && this.ActiveNozzle != null)
                        
                    //else
                    //{
                    //    nextCommand = this.GetId();
                    //    //nextCommand = this.GetNozzleTotals(this.LastNozzle);
                    //}
                }
                else
                {
                    if (this.DispenserStatus == Common.Enumerators.FuelPointStatusEnum.Offline)
                    {
                        this.InitializeSent = false;
                        this.PriceSetSent = false;
                    }
                    if (!this.InitializeSent || !this.PriceSetSent)
                    {
                        if (dtNow.Subtract(this.lastCommand).TotalMilliseconds < 500)
                            nextCommand = null;
                        if (!this.InitializeSent)
                            nextCommand = this.InitializeFuelPoint();
                        else if (!this.PriceSetSent)
                            nextCommand = this.SetPrices();
                        else
                            nextCommand = this.GetFuelPointStatus();
                    }
                    else
                    {
                        
                        if (this.DispenserStatus == Common.Enumerators.FuelPointStatusEnum.Idle)
                        {
                            if (this.ActiveNozzle != null)
                            {
                                this.LastNozzle = this.ActiveNozzle;
                                this.ActiveNozzle = null;
                            }
                            if (dtNow.Subtract(this.lastCommand).TotalMilliseconds < 1000)
                                nextCommand = null;
                            if (this.QueryNozzle)
                                nextCommand = this.GetActiveNozzle();
                            else
                                nextCommand = this.GetFuelPointStatus();
                        }
                        else if (this.status == Common.Enumerators.FuelPointStatusEnum.Nozzle)
                        {
                            if (dtNow.Subtract(this.lastCommand).TotalMilliseconds < 50)
                                nextCommand = null;

                            //if (this.ActiveNozzle == null)
                            //    nextCommand = this.GetActiveNozzle();
                            if (this.QueryAuthorize)
                            {
                                decimal price = (decimal)this.ActiveNozzle.UnitPrice / (decimal)Math.Pow(10, this.PriceDecimalPlaces);
                                nextCommand = this.AuthoriseFuelPoint(price, (decimal)9999.99, 999999);
                            }
                            else if (this.QueryTotals)
                                nextCommand = this.GetNozzleTotals(this.ActiveNozzle);
                            else
                                nextCommand = this.GetFuelPointStatus();
                        }
                        else if (this.status == Common.Enumerators.FuelPointStatusEnum.Work || this.status == Common.Enumerators.FuelPointStatusEnum.Ready)
                        {
                            if (dtNow.Subtract(this.lastCommand).TotalMilliseconds < 50)
                                nextCommand = null;
                            nextCommand = this.GetFuelingPointData(this.ActiveNozzle);
                        }
                        else
                            nextCommand = this.GetFuelPointStatus();
                    }
                }
                return nextCommand;
            }
            catch
            {
                nextCommand = null;
                return nextCommand;
            }
            finally
            {
                if (nextCommand != null)
                    this.lastCommand = dtNow;
                
            }
        }

        private byte TotalNozzleDefinition(int nozzleIndex)
        {
            BitArray bitArr = new BitArray(8);
            byte nzi = (byte)nozzleIndex;
            BitArray baNzi = new BitArray(new Byte[] { nzi });
            bitArr.Set(0, false);
            bitArr.Set(1, baNzi.Get(0));
            bitArr.Set(2, baNzi.Get(1));
            bitArr.Set(3, baNzi.Get(2));
            bitArr.Set(4, false);
            bitArr.Set(5, false);
            bitArr.Set(6, true);
            bitArr.Set(7, false);

            byte[] bytes = new byte[1];
            bitArr.CopyTo(bytes, 0);
            return bytes[0];
        }

        public byte[] StringToByteArray(string hex)
        {
            if (hex.Length % 2 != 0)
                hex = "0" + hex;
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        private byte[] GetDecimalBytes(decimal value, int decimalPlaces, int byteCount)
        {
            long valueInt = (long)((double)value * Math.Pow(10, decimalPlaces));
            List<Byte> bufferList = new List<byte>();
            int maxPow = 2 * (byteCount - 1);
            double restValue = (double)valueInt;
            string str = "";
            for (int i = 0; i < byteCount; i++)
            {
                int pow = maxPow - (2 * i);
                double byteVal = (int)(restValue / System.Math.Pow(10, pow));
                int bv = (int)byteVal;
                str = str + bv.ToString();
                //bufferList.Add(b);
                restValue = restValue - (byteVal * System.Math.Pow(10, pow));
            }
            return StringToByteArray(str);
        }

        private byte[] NormaliseBuffer(byte[] buffer)
        {
            List<byte> newBuffer = new List<byte>();
            foreach (Byte b in buffer)
            {
                newBuffer.Add(b);
                newBuffer.Add((byte)(255 - b));
            }
            return newBuffer.ToArray();
        }

        public OpenCommand GetFuelPointStatus()
        {
            byte startByte = BitConverter.GetBytes(240 + this.AddressId - 1)[0];
            byte startByteNeg = (byte)(255 - startByte);
            byte[] command = new byte[] { startByte, startByteNeg, 0xA2, 0x5D };

            return new OpenCommand(command, CommandTypeEnum.RequestStatus, this);
        }

        public OpenCommand InitializeFuelPoint()
        {
            byte startByte = BitConverter.GetBytes(240 + this.AddressId - 1)[0];
            List<Byte> commandBuffer = new List<byte>();
            this.InitializeSent = true;
            byte cmd = 0xA6;
            commandBuffer.Add(startByte);
            commandBuffer.Add(cmd);

            for (int i = 0; i < this.Nozzles.Length; i++)
            {
                byte[] price1 = this.GetDecimalBytes(this.Nozzles[i].UnitPrice, this.PriceDecimalPlaces, 2);
                byte[] price2 = this.GetDecimalBytes(this.Nozzles[i].UnitPrice, this.PriceDecimalPlaces, 2);
                commandBuffer.AddRange(price1);
                commandBuffer.AddRange(price2);
            }
            for (int i = this.Nozzles.Length; i < 4; i++)
            {
                byte[] price1 = this.GetDecimalBytes(0, this.PriceDecimalPlaces, 2);
                byte[] price2 = this.GetDecimalBytes(0, this.PriceDecimalPlaces, 2);
                commandBuffer.AddRange(price1);
                commandBuffer.AddRange(price2);
            }
            //commandBuffer.Add(0x00);
            //commandBuffer.Add(0x00);
            //commandBuffer.Add(0x00);
            //commandBuffer.Add(0x00);
            //commandBuffer.Add(0x00);
            //commandBuffer.Add(0x00);

            byte[] command = this.NormaliseBuffer(commandBuffer.ToArray());

            Console.WriteLine("InitializeFuelPoint Code : " + BitConverter.ToString(command));
            return new OpenCommand(command, CommandTypeEnum.SendMainDisplayData, this);
        }
        private int ith(int num)
        {
            return 16 * (num / 10) + (num % 10);
        }// hex = integerToHex(int)
        public OpenCommand SetPrices()
        {

            byte startByte = BitConverter.GetBytes(192 + this.AddressId - 1)[0];
            List<Byte> commandBuffer = new List<byte>();
            this.PriceSetSent = true;
            byte cmd = 0xA3;
            commandBuffer.Add(startByte);
            commandBuffer.Add(cmd);
            for (int i = 0; i < this.Nozzles.Length; i++)
            {
               // decimal price = (decimal)this.Nozzles[i].UnitPrice / (decimal)Math.Pow(10, this.PriceDecimalPlaces);
                //if (price == 0)
                  //  return null;

                string sprice = this.Nozzles[i].UnitPrice.ToString();

                for(int k = sprice.Length; k < 4; i++)
                {
                    sprice = "0" + sprice;
                }
                int p1 = Convert.ToInt32((sprice.Substring(0, 2)));
                int p2 = Convert.ToInt32((sprice.Substring(2, 2)));

                byte price1 = (byte)ith(p1);
                byte price2 = (byte)ith(p2);
                commandBuffer.Add(price1);
                commandBuffer.Add(price2);
            }

            for (int i = this.Nozzles.Length; i < 8; i++)
            {
                byte[] price1 = this.GetDecimalBytes(0, this.PriceDecimalPlaces, 2);
                byte[] price2 = this.GetDecimalBytes(0, this.PriceDecimalPlaces, 2);
                commandBuffer.AddRange(price1.Take(2));
                commandBuffer.AddRange(price2.Take(2));
            }
            for (int i = 0; i < 8; i++)
            {
                commandBuffer.Add(0x00);
            }

            byte[] command = this.NormaliseBuffer(commandBuffer.ToArray());
            

            return new OpenCommand(command, CommandTypeEnum.SendPrices, this);
        }

        public OpenCommand GetId()
        {
            byte startByte = BitConverter.GetBytes(240 + this.AddressId - 1)[0];
            List<Byte> commandBuffer = new List<byte>();
            this.InitializeSent = true;
            byte cmd = 0xA0;
            commandBuffer.Add(startByte);
            commandBuffer.Add(cmd);
            byte[] command = this.NormaliseBuffer(commandBuffer.ToArray());

            Console.WriteLine("Get ID : " + BitConverter.ToString(command));
            return new OpenCommand(command, CommandTypeEnum.RequestID, this);
        }

        public OpenCommand AuthoriseFuelPoint(decimal price, decimal presetMoney, decimal presetVolume)
        {
            byte startByte = BitConverter.GetBytes(240 + this.AddressId - 1)[0];
            List<Byte> commandBuffer = new List<byte>();

            byte slowFlowOffsetStart = 0x20;

            commandBuffer.Add(startByte);
            commandBuffer.Add(0xA5);
            commandBuffer.Add(slowFlowOffsetStart);

            //price = price * (decimal)Math.Pow(10, this.PriceDecimalPlaces);

            byte[] upBuffer = this.GetDecimalBytes(price, this.PriceDecimalPlaces, 2);
            byte[] prBuffer = this.GetDecimalBytes((decimal)999999, this.VolumeDecimalPlaces, 3);
            byte[] volBuffer = this.GetDecimalBytes((decimal)999999, this.VolumeDecimalPlaces, 3);

            commandBuffer.AddRange(upBuffer.Take(2).Reverse());
            commandBuffer.AddRange(prBuffer.Reverse());
            commandBuffer.AddRange(volBuffer.Reverse());

            byte[] command = this.NormaliseBuffer(commandBuffer.ToArray());
            Console.WriteLine("AuthoriseFuelPoint Code : " + BitConverter.ToString(command));
            return new OpenCommand(command, CommandTypeEnum.Authorize, this);
        }

        public OpenCommand GetActiveNozzle()
        {
            byte startByte = BitConverter.GetBytes(192 + this.AddressId - 1)[0];
            List<Byte> commandBuffer = new List<byte>();

            byte cmd = 0xA1;

            commandBuffer.Add(startByte);
            commandBuffer.Add(cmd);

            byte[] command = this.NormaliseBuffer(commandBuffer.ToArray());
            return new OpenCommand(command, CommandTypeEnum.RequestActiveNozzle, this);
        }

        public OpenCommand GetNozzleTotals(Nozzle nozzle)
        {
            byte startByte = BitConverter.GetBytes(240 + this.AddressId - 1)[0];
            List<Byte> commandBuffer = new List<byte>();

            byte cmd = 0xA9;

            commandBuffer.Add(startByte);
            commandBuffer.Add(cmd);
            commandBuffer.Add(this.TotalNozzleDefinition(nozzle.NozzleIndex - 1));

            byte[] command = this.NormaliseBuffer(commandBuffer.ToArray());
            return new OpenCommand(command, CommandTypeEnum.RequestTotals, nozzle);
        }

        public OpenCommand GetFuelingPointData(Nozzle nozzle)
        {
            if (nozzle == null)
                return null;
            byte startByte = BitConverter.GetBytes(240 + nozzle.ParentFuelPoint.AddressId - 1)[0];
            byte startByteNeg = (byte)(255 - startByte);
            byte[] command = new byte[] { startByte, startByteNeg, 0xA1, 0x5E };
            return new OpenCommand(command, CommandTypeEnum.RequestDisplayData, nozzle);

        }

        public OpenCommand StopFuelPoint()
        {
            byte startByte = BitConverter.GetBytes(240 + this.AddressId - 1)[0];
            byte startByteNeg = (byte)(255 - startByte);
            byte[] command = new byte[] { startByte, startByteNeg, 0xA3, 0x5C };
            return new OpenCommand(command, CommandTypeEnum.Halt, this);
        }
    }

    public class StatusChangedEventArgs : EventArgs
    {
        public StatusChangedEventArgs(FuelPoint fp, Common.Enumerators.FuelPointStatusEnum currentStatus, Common.Enumerators.FuelPointStatusEnum previousStatus)
        {
            this.CurrentStatus = currentStatus;
            this.PreviousStatus = previousStatus;
            this.tfp = fp;
            
        }
        public FuelPoint tfp
        {
            private set;
            get;
        }
        public Common.Enumerators.FuelPointStatusEnum CurrentStatus { private set; get; }
        public Common.Enumerators.FuelPointStatusEnum PreviousStatus { private set; get; }
    }

    public class TotalsEventArgs : EventArgs
    {
        public TotalsEventArgs(int address, byte nozzleId, Nozzle nozzle)
        {
            this.Address = address;
            this.Nozzle = nozzle;
            this.NozzleID = nozzleId;
        }

        public int Address { private set; get; }
        public Nozzle Nozzle { private set; get; }
        public byte NozzleID { private set; get; }
    }

    public class OpenCommand
    {
        public string CommandString
        {
            get { return BitConverter.ToString(this.Command); }
        }
        public CommandTypeEnum CommandType { private set; get; }
        public FuelPoint Fpoint { private set; get; }
        public Nozzle CurrentNozzle { private set; get; }
        public byte[] Command { private set; get; }
        public DateTime SendTime { set; get; }
        public DateTime TimeExeeded { set; get; }
        public int ResponseLength { private set; get; }
        public int BaudRate { set; get; }
        public int MinimumTimeNeeded
        {
            get
            {
                double speed = 1 / (this.BaudRate / 8000);
                return (int)((double)(this.Command.Length + this.ResponseLength) * speed);
            }
        }

        public OpenCommand(byte[] cmd, CommandTypeEnum typ, FuelPoint fp)
        {
            this.CommandType = typ;
            this.Fpoint = fp;
            this.Command = cmd;
            this.SetResponseLength();
            this.BaudRate = 9600;
            this.SendTime = DateTime.MinValue;
        }

        public OpenCommand(byte[] cmd, CommandTypeEnum typ, Nozzle nozzle)
        {
            this.CommandType = typ;
            this.Fpoint = nozzle.ParentFuelPoint;
            this.CurrentNozzle = nozzle;
            this.Command = cmd;
            this.SetResponseLength();
            this.BaudRate = 9600;
            this.SendTime = DateTime.MinValue;
        }

        private void SetResponseLength()
        {
            switch (this.CommandType)
            {
                case CommandTypeEnum.RequestStatus:
                    this.ResponseLength = 2;
                    break;
                case CommandTypeEnum.RequestTotals:
                    this.ResponseLength = 22;
                    break;
                case CommandTypeEnum.SendPrices:
                    this.ResponseLength = 2;
                    break;
                case CommandTypeEnum.RequestDisplayData:
                    this.ResponseLength = 18;
                    break;
                case CommandTypeEnum.Authorize:
                    this.ResponseLength = 2;
                    break;
                case CommandTypeEnum.Halt:
                    this.ResponseLength = 2;
                    break;
                case CommandTypeEnum.RequestActiveNozzle:
                    this.ResponseLength = 2;
                    break;
                case CommandTypeEnum.SendMainDisplayData:
                    this.ResponseLength = 2;
                    break;

            }
        }
    }
}

