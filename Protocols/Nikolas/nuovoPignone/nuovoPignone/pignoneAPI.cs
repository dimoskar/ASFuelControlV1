using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace nuovoPignone
{
    class pignoneAPI
    {
        //Nozzle nozzle = new Nozzle();

        #region SerialPort

        private System.IO.Ports.SerialPort serialPort = new System.IO.Ports.SerialPort();

        public string CommunicationPort
        {
            set;
            get;
        }
        public void Connect()
        {
            this.serialPort.PortName = this.CommunicationPort;
            this.serialPort.Parity = System.IO.Ports.Parity.Odd;
            this.serialPort.BaudRate = 2400;
            this.serialPort.Open();
        }

        public int MinimumTimeNeeded(int cmdl, int respl)
        {
            double speed = 1 / (this.serialPort.BaudRate / 8000);
            return (int)((double)(cmdl + respl) * speed);
        }

        #endregion

        #region Private methods

        private int ith(int num) { return 16 * (num / 10) + (num % 10); }// hex = integerToHex(int)
        private int idGetStatus(int num)  { return ((num - 1) * 8 + 9 ); }
        private int idGetDisplay(int num) { return ((num - 1) * 8 + 10); }
        private int idSetPrice(int num)   { return ((num - 1) * 8 + 12); }


        private byte[] requestDisplayData(int id)
        {
            byte[] Buffer = new byte[] { 0x00, (byte)(idSetPrice(id)), 0xB4 };
            return Buffer;

        }
        private Nozzle evalDisplay(Nozzle nozzle, byte[] response)
        {
            //tx 00 0A B4 //NZ1
            //rx 0A B4 00 FF 07 F8 29 D6 00 FF 04 FB 45 BA 00 FF (7.29e) (4.45ltr)
            string[] parms = BitConverter.ToString(response).Split('-');
            for (int i = 2; i < 15; i++)
            {
                if (StringToByteArray(parms[i])[0] + StringToByteArray(parms[i + 1])[0] != 255) { throw new Exception("evalDisplay() Failed"); }
            }

            string upString = parms[2] + parms[4] + parms[6];
            string volString = parms[8] + parms[10] + parms[12];

            string status = parms[14];
            if (status == "00") { nozzle.ParentFuelPoint.Status = Common.Enumerators.FuelPointStatusEnum.Idle; }
            else if (status == "0A") { nozzle.ParentFuelPoint.Status = Common.Enumerators.FuelPointStatusEnum.Work; }
            else if (status == "08") { nozzle.ParentFuelPoint.Status = Common.Enumerators.FuelPointStatusEnum.TransactionCompleted; }
            else throw new Exception("unknonw status flag evalDisplay() failed");
            nozzle.SalePrice = decimal.Parse(upString) / (decimal)Math.Pow(10, nozzle.PriceDecimalPlaces);
            nozzle.SaleVolume = decimal.Parse(volString) / (decimal)Math.Pow(10, nozzle.VolumeDecimalPlaces);

            //Get Unitprice////////////////////////////////////////////////////////////
            byte[] upBuffer = requestUnitPrice(nozzle.NozzleId);
            this.serialPort.Write(upBuffer, 0, upBuffer.Length);

            while (this.serialPort.BytesToRead < 6)
            {
                System.Threading.Thread.Sleep(15); //15*4 = 70 [ NP 68ms response time]
            }
            byte[] upResponse = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(upResponse, 0, this.serialPort.BytesToRead);
            ////////////////////////////////////////////////////////////////////////////

            nozzle.SaleUnitPrice = evalUnitPrice(nozzle, upResponse).SaleUnitPrice;
            return nozzle;

        }

        private byte[] requestUnitPrice(int id)
        {
            byte[] Buffer = new byte[] { 0x00, (byte)(idGetDisplay(id)), 0xC4 };
            return Buffer;
        }
        private Nozzle evalUnitPrice(Nozzle nozzle, byte[] response)
        {
            //Get UnitPrice
            //tx 00 12 c3 //get nz2 ltr price
            //rx 12 C3 55 AA 49 B6 //(5.549 eURO) 
            string respString = BitConverter.ToString(response);
            string price1 = respString.Substring(6, 2);
            string price2 = respString.Substring(12, 2);
            nozzle.SaleUnitPrice = decimal.Parse(price1 + price2) / 1000;
            return nozzle;
        }

        private byte[] requestTotals(int id)
        {
            byte[] Buffer = new byte[] { 0x00, (byte)(idGetDisplay(id)), 0x87 };
            return Buffer;
        }
        private Nozzle evalTotals(Nozzle nozzle, byte[] response)
        {   //nz1
            //TX 00 0A 87
            //   0 1  2     4      6    8     10    12   
            //RX 0A 87 00 FF 00 FF 06 F9 97 68 03 FC 65 9A 
            string[] parms = BitConverter.ToString(response).Split('-');
            for (int i = 2; i < 13; i++)
            {
                if (StringToByteArray(parms[i])[0] + StringToByteArray(parms[i + 1])[0] != 255) { throw new Exception("evalTotals() Failed"); }
            }
            string volume = parms[2] + parms[4] + parms[6] + parms[8] + parms[10] + parms[12];
            nozzle.Totalizer = decimal.Parse(volume);
            return nozzle;
        }


        private byte[] setPrices(int unitPrice, int id)
        {
            // 1234 => 1.234E
            // 100  => 0.1  E
            // 1    =>0.001 E
            if (unitPrice >= 9999) { throw new ArgumentException("max value 9999", "unitPrice"); }
            else
            {
                string priceString = Convert.ToString(unitPrice);
                priceString = priceString.PadLeft(4, '0'); // Zero padding => product 4characters.
                int p1 = Convert.ToInt16(priceString.Substring(0, 2));
                int p2 = Convert.ToInt16(priceString.Substring(2, 2));
                byte[] Buffer = new byte[] { 0x00, (byte)(idSetPrice(id)), 0x69, (byte)ith(p1), (byte)ith(p2) };
                return Buffer;
            }
        }
        private byte[] confirmSetPrices(byte[] response, int unitPrice, int id)
        {
            byte[] Buffer = new byte[] { };
            Buffer = setPrices(unitPrice, id);
            if (response.SequenceEqual(Buffer.Skip(1).Take(4).ToArray())) //response == Buffer
            {
                Buffer = new byte[] { 0x00, (byte)(idSetPrice(id) + 1), (byte)(254 - idSetPrice(id)), Buffer[3], Buffer[4] };
                return Buffer;
            }
            else { throw new ArgumentException(@"SetPrice() Failed ", "byte[] Response"); }
        }

        private byte[] authoriseFuelPoint(int id)
        {
            byte[] Buffer = new byte[] { 0x00, (byte)(idSetPrice(id)), 0xE1 };
            return Buffer;
        }
        private byte[] confirmAuthoriseFuelPoint(byte[] response, int id)
        {
            byte[] Buffer = new byte[] { (byte)(idSetPrice(id)), 0xE1 };
            if (response.SequenceEqual(Buffer))
            {
                Buffer = new byte[] { 0x00, (byte)(idSetPrice(id) + 1), (byte)(254 - idSetPrice(id)) };
                return Buffer;
            }
            else { throw new ArgumentException(@"AuthoriseFuelPoint() Failed ", "byte[] Response"); }

        }

        private byte[] getFuelPointStatus(int id)
        {
            byte[] Buffer = new byte[] { 0x00, (byte)(idGetStatus(id)) };
            return Buffer;
        }
        private Nozzle evaluateStatus(Nozzle nozzle, byte[] response)
        {
            if (response.Length != 3 || (response[1] + response[2]) != 0xFF)
            {
                return nozzle;
            }
            else
            {
                byte b1 = response[3];
                if (b1 == 0xFF) { nozzle.ParentFuelPoint.Status = Common.Enumerators.FuelPointStatusEnum.Idle; }
                else if (b1 == 0xF7) { nozzle.ParentFuelPoint.Status = Common.Enumerators.FuelPointStatusEnum.Nozzle; }
                else if (b1 == 0xF5) { nozzle.ParentFuelPoint.Status = Common.Enumerators.FuelPointStatusEnum.Work; }
                else if (b1 == 0xBF) { nozzle.ParentFuelPoint.Status = Common.Enumerators.FuelPointStatusEnum.TransactionCompleted; }
                return nozzle;
            }
        }
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Public methods

        public Nozzle GetTotals(Nozzle nozzle)
        {
            byte[] upBuffer = requestTotals(nozzle.ParentFuelPoint.AddressId);
            this.serialPort.Write(upBuffer, 0, upBuffer.Length);
            while (this.serialPort.BytesToRead < (int)responseLength.totals)
            {
                System.Threading.Thread.Sleep(15); //15*4 = 70 [ NP 68ms response time]
            } 
            byte[] response = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(response, 0, this.serialPort.BytesToRead);
            return evalTotals(nozzle, response);
        }

        public Nozzle GetDisplay(Nozzle nozzle) 
        {  
            byte[] upBuffer = requestDisplayData(nozzle.ParentFuelPoint.AddressId;
            this.serialPort.Write(upBuffer, 0, upBuffer.Length);
            while (this.serialPort.BytesToRead < (int)responseLength.display)
            {
                System.Threading.Thread.Sleep(15); //15*4 = 70 [ NP 68ms response time]
            }
            byte[] response = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(response, 0, this.serialPort.BytesToRead);
            return evalDisplay(nozzle,response);
    
        }
        public bool SetPrice(Nozzle nozzle, int unitPrice)
        {
            try
            {
                this.serialPort.DiscardInBuffer();
                byte[] buffer = setPrices(unitPrice, nozzle.ParentFuelPoint.AddressId);
                this.serialPort.Write(buffer, 0, buffer.Length);
                while (this.serialPort.BytesToRead < (int)responseLength.setprice)
                {
                    System.Threading.Thread.Sleep(20);
                }
                byte[] response = new byte[this.serialPort.BytesToRead];
                this.serialPort.Read(response, 0, this.serialPort.BytesToRead);
                buffer = confirmSetPrices(response, unitPrice, nozzle.ParentFuelPoint.AddressId);
                this.serialPort.Write(buffer, 0, buffer.Length);
            }
            catch { return false; }
            return true;
        }
        public Nozzle GetUnitPrice(Nozzle nozzle)
        {
            byte[] upBuffer = requestUnitPrice(nozzle.NozzleId);
            this.serialPort.Write(upBuffer, 0, upBuffer.Length);

            while (this.serialPort.BytesToRead < (int)responseLength.unutprice)
            {
                System.Threading.Thread.Sleep(15); //15*4 = 70 [ NP 68ms response time]
            }
            byte[] upResponse = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(upResponse, 0, this.serialPort.BytesToRead);
            return evalUnitPrice(nozzle, upResponse);
        }
        public Nozzle GetStatus(Nozzle nozzle)
        {
            this.serialPort.DiscardInBuffer();
            byte[] buffer = getFuelPointStatus(nozzle.ParentFuelPoint.AddressId);
            this.serialPort.Write(buffer, 0, buffer.Length);
            while (this.serialPort.BytesToRead < (int)responseLength.status)
            {
                System.Threading.Thread.Sleep(15); //15*4 = 70 [ NP 68ms response time]
            }
            byte[] response = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(response, 0, this.serialPort.BytesToRead);
            return evaluateStatus(nozzle, response);
        }
        public bool AuthorizeFuelPoint(Nozzle nozzle)
        {
            try
            {
                this.serialPort.DiscardInBuffer();
                byte[] buffer = authoriseFuelPoint(nozzle.ParentFuelPoint.AddressId);
                this.serialPort.Write(buffer, 0, buffer.Length);
                while (this.serialPort.BytesToRead < (int)responseLength.status)
                {
                    System.Threading.Thread.Sleep(20);
                }
                byte[] response = new byte[this.serialPort.BytesToRead];
                this.serialPort.Read(response, 0, this.serialPort.BytesToRead);
                buffer = confirmAuthoriseFuelPoint(response, nozzle.ParentFuelPoint.AddressId);
                this.serialPort.Write(buffer, 0, buffer.Length);
            }
            catch { return false; }
            return true;

        }

        public byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        #endregion

        private enum responseLength 
        { 
            totals = 14,
            display = 16,
            auth = 2,
            status = 3,
            setprice=4,
            unitprice=6



        }


    }


}


