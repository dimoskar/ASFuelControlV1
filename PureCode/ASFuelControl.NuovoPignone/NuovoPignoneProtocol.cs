//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace ASFuelControl.NuovoPignone
//{
//    public class NuovoPignoneProtocol: Common.IFuelProtocol
//    { 
//        public event EventHandler<Common.FuelPointValuesArgs> DataChanged;
//        public event EventHandler<Common.TotalsEventArgs> TotalsRecieved;
//        public event EventHandler<Common.FuelPointValuesArgs> DispenserStatusChanged;
//        private List<Common.FuelPoint> fuelPoints = new List<Common.FuelPoint>();
//        private System.IO.Ports.SerialPort serialPort = new System.IO.Ports.SerialPort();
//        private System.Threading.Thread th;
//        public Common.DebugValues foo = new Common.DebugValues();
//        public Common.FuelPoint[] FuelPoints
//        {
//            get
//            {
//                return this.fuelPoints.ToArray();
//            }
//            set
//            {
//                this.fuelPoints = new List<Common.FuelPoint>(value);
//            }
//        }
//        public bool IsConnected
//        {
//            get
//            {
//                return this.serialPort.IsOpen;
//            }
//        }
//        public string CommunicationPort
//        {
//            set;
//            get;
//        }
//        public void Connect()
//        {
//            try
//            {
//                this.serialPort.PortName = this.CommunicationPort;
//                this.serialPort.Parity = System.IO.Ports.Parity.Odd;
//                this.serialPort.BaudRate = 2400;
//                this.serialPort.Open();
//                this.th = new System.Threading.Thread(new System.Threading.ThreadStart(this.ThreadRun));
//                th.Start();
//            }
//            catch
//            {
//            }
//        }
//        public void Disconnect()
//        {
//            if(this.serialPort.IsOpen)
//                this.serialPort.Close();
//        }
//        public void AddFuelPoint(Common.FuelPoint fp)
//        {
//            this.fuelPoints.Add(fp);
//        }
//        public void ClearFuelPoints()
//        {
//            this.fuelPoints.Clear();
//        }
//        public Common.DebugValues DebugStatusDialog(Common.FuelPoint fp)
//        {
//            foo = null; 
//            fp=GetStatus(fp);
//            foo.Status = fp.Status;
//            return foo;
//        }
//        private void ThreadRun()
//        {
//            foreach(Common.FuelPoint fp in this.fuelPoints)
//            {
//                fp.Nozzles[0].QueryTotals = true;
//            }
//            while(this.IsConnected)
//            {
//                try
//                {
//                    foreach(Common.FuelPoint fp in this.fuelPoints)
//                    {
//                        try
//                        {
//                            //this.serialPort.DiscardInBuffer();
//                            //this.serialPort.DiscardOutBuffer();

//                            if(fp.QuerySetPrice)
//                            {
//                                foreach(Common.Nozzle nz in fp.Nozzles)
//                                {
//                                    if(this.SetPrice(nz, nz.UntiPriceInt))
//                                        nz.QuerySetPrice = false;
//                                    System.Threading.Thread.Sleep(50);
//                                }
//                                if(fp.Nozzles.Where(n => n.QuerySetPrice).Count() == 0)
//                                    fp.QuerySetPrice = false;
//                                continue;
//                            }
//                            int nozzleForTotals = fp.Nozzles.Where(n => n.QueryTotals).Count();
//                            if(nozzleForTotals > 0)
//                            {
//                                foreach(Common.Nozzle nz in fp.Nozzles)
//                                {
//                                    if(nz.QueryTotals)
//                                    {   //25510 38444      ===>  301
//                                        this.GetTotals(nz);
//                                        System.Threading.Thread.Sleep(10);
                                       
//                                            if((bool)(fp.GetExtendedProperty("iNeedDisplay", true)))
//                                            {
//                                                this.GetDisplay(nz);
//                                                fp.SetExtendedProperty("iNeedDisplay", false);
//                                            }
//                                            // fp.SetExtendedProperty("TotalsInitialized", true);
//                                            fp.Initialized = true;
//                                            if(this.TotalsRecieved != null)
//                                            {
//                                                this.TotalsRecieved(this, new Common.TotalsEventArgs(fp, nz.Index, nz.TotalVolume, nz.TotalPrice));
//                                            }
                                        
//                                    }
//                                }

//                                continue;
//                            }

//                            if(fp.QueryAuthorize)
//                            {
//                                if(this.AuthorizeFuelPoint(fp))
//                                    fp.QueryAuthorize = false;
//                                continue;
//                            }

//                            if(fp.Status == Common.Enumerators.FuelPointStatusEnum.Work)
//                            {
//                                fp.SetExtendedProperty("iNeedDisplay", true);
//                                this.GetDisplay(fp.ActiveNozzle);
//                            }
//                            Common.Enumerators.FuelPointStatusEnum oldStatus = fp.Status;
//                            this.GetStatus(fp);
//                             //this.GetDisplay(fp.Nozzles[0]);
//                            //if (fp.Status != Common.Enumerators.FuelPointStatusEnum.Idle && fp.Status != Common.Enumerators.FuelPointStatusEnum.Offline)
//                            //{
//                            //    fp.ActiveNozzle = fp.Nozzles[0];

//                            //}
//                            if(oldStatus != fp.Status && this.DispenserStatusChanged != null)
//                            {

//                                Common.FuelPointValues values = new Common.FuelPointValues();
//                                if(fp.Status != Common.Enumerators.FuelPointStatusEnum.Idle && fp.Status != Common.Enumerators.FuelPointStatusEnum.Offline)
//                                {
//                                    fp.ActiveNozzleIndex = 0;
//                                    values.ActiveNozzle = 0;
//                                }
//                                else
//                                {
//                                    fp.ActiveNozzleIndex = -1;
//                                    values.ActiveNozzle = -1;
//                                }

//                                values.Status = fp.Status;
//                                this.DispenserStatusChanged(this, new Common.FuelPointValuesArgs()
//                                {
//                                    CurrentFuelPoint = fp,
//                                    CurrentNozzleId = values.ActiveNozzle + 1,
//                                    Values = values
//                                });

//                            }

//                        }
//                        finally
//                        {
//                            System.Threading.Thread.Sleep(120);
//                        }

//                    }
//                }
//                catch(Exception e)
//                {
//                    System.IO.Directory.CreateDirectory(System.Environment.CurrentDirectory + "\\logs");
//                    System.IO.File.AppendAllText(System.Environment.CurrentDirectory + "\\logs" + "\\NuovoPignoneError.txt", "\n" + e.ToString()); 
//                    System.Threading.Thread.Sleep(250);
//                }
//            }
//        }

//        #region SerialPort

//        public int MinimumTimeNeeded(int cmdl, int respl)
//        {
//            double speed = 1 / (this.serialPort.BaudRate / 8000);
//            return (int)((double)(cmdl + respl) * speed);
//        }

//        #endregion

//        #region Private methods

//        private int ith(int num)
//        {
//            return 16 * (num / 10) + (num % 10);
//        }// hex = integerToHex(int)
//        private int idGetStatus(int num)
//        {
//            return ((num - 1) * 8 + 9);
//        }
//        private int idGetDisplay(int num)
//        {
//            return ((num - 1) * 8 + 10);
//        }
//        private int idSetPrice(int num)
//        {
//            return ((num - 1) * 8 + 12);
//        }


//        private byte[] requestDisplayData(int id)
//        {
//            byte[] Buffer = new byte[] { 0x00, (byte)(idGetDisplay(id)), 0xB4 };
//            return Buffer;

//        }
//        private Common.Nozzle evalDisplay(Common.Nozzle nozzle, byte[] response)
//        {
//            //tx 00 0A B4 //NZ1
//            //rx 0A B4 00 FF 07 F8 29 D6 00 FF 04 FB 45 BA 00 FF (7.29e) (4.45ltr)
//            string[] parms = BitConverter.ToString(response).Split('-');
//            for(int i = 1; i < 8; i++)
//            {
//                if(StringToByteArray(parms[2 * i])[0] + StringToByteArray(parms[2 * i + 1])[0] != 255)
//                {
//                    throw new Exception("evalDisplay() Failed");
//                }
//            }

//            string upString = parms[2] + parms[4] + parms[6];
//            string volString = parms[8] + parms[10] + parms[12];

//            //string status = parms[14];
//            //if(status == "00")
//            //{
//            //    nozzle.ParentFuelPoint.Status = Common.Enumerators.FuelPointStatusEnum.Idle;
//            //}
//            //else if(status == "0A")
//            //{
//            //    nozzle.ParentFuelPoint.Status = Common.Enumerators.FuelPointStatusEnum.Work;
//            //}
//            //else if(status == "08")
//            //{
//            //    nozzle.ParentFuelPoint.Status = Common.Enumerators.FuelPointStatusEnum.Nozzle;
//            //}
//            //else nozzle.ParentFuelPoint.Status = Common.Enumerators.FuelPointStatusEnum.Idle;

//            //nozzle.ParentFuelPoint.DispenserStatus = nozzle.ParentFuelPoint.Status;

//            nozzle.ParentFuelPoint.DispensedAmount = decimal.Parse(upString) / (decimal)Math.Pow(10, nozzle.ParentFuelPoint.AmountDecimalPlaces);
//            nozzle.ParentFuelPoint.DispensedVolume = decimal.Parse(volString) / (decimal)Math.Pow(10, nozzle.ParentFuelPoint.VolumeDecimalPlaces);

//            //if(nozzle.ParentFuelPoint.Address == 2) nozzle.ParentFuelPoint.DispensedAmount = nozzle.ParentFuelPoint.DispensedAmount * 10;
//            //// Volume 5litr
//            ////unitprice 1,500
//            ////calculated = 7,5
//            ////orginal = 75
//            //if(nozzle.ParentFuelPoint.DispensedVolume != 0)
//            //{
//            //    decimal ratio = nozzle.ParentFuelPoint.DispensedAmount / decimal.Round(nozzle.ParentFuelPoint.DispensedVolume * nozzle.UnitPrice, nozzle.ParentFuelPoint.DecimalPlaces);
//            //    if(ratio >= 1)
//            //        nozzle.ParentFuelPoint.DispensedAmount = nozzle.ParentFuelPoint.DispensedAmount / decimal.Round(ratio, 0,MidpointRounding.AwayFromZero);
//            //    else
//            //        nozzle.ParentFuelPoint.DispensedAmount = nozzle.ParentFuelPoint.DispensedAmount * decimal.Round(1 / ratio, 0, MidpointRounding.AwayFromZero);
//            //}
//            if(this.DataChanged != null)
//            {
//                Common.FuelPointValues values = new Common.FuelPointValues();
//                values.CurrentSalePrice = nozzle.UnitPrice;
//                values.CurrentPriceTotal = nozzle.ParentFuelPoint.DispensedAmount;
//                values.CurrentVolume = nozzle.ParentFuelPoint.DispensedVolume;

//                this.DataChanged(this, new Common.FuelPointValuesArgs()
//                {
//                    CurrentFuelPoint = nozzle.ParentFuelPoint,
//                    CurrentNozzleId = 1,
//                    Values = values
//                });
//            }

//            ////Get Unitprice////////////////////////////////////////////////////////////
//            //byte[] upBuffer = requestUnitPrice(nozzle.NozzleId);
//            //this.serialPort.Write(upBuffer, 0, upBuffer.Length);

//            //while (this.serialPort.BytesToRead < 6)
//            //{
//            //    System.Threading.Thread.Sleep(15); //15*4 = 70 [ NP 68ms response time]
//            //}
//            //byte[] upResponse = new byte[this.serialPort.BytesToRead];
//            //this.serialPort.Read(upResponse, 0, this.serialPort.BytesToRead);
//            //////////////////////////////////////////////////////////////////////////////

//            //nozzle.SaleUnitPrice = evalUnitPrice(nozzle, upResponse).SaleUnitPrice;
//            return nozzle;

//        }

//        private byte[] requestUnitPrice(int id)
//        {
//            byte[] Buffer = new byte[] { 0x00, (byte)(idGetDisplay(id)), 0xC4 };
//            return Buffer;
//        }
//        private Common.Nozzle evalUnitPrice(Common.Nozzle nozzle, byte[] response)
//        {
//            //Get UnitPrice
//            //tx 00 12 c3 //get nz2 ltr price
//            //rx 12 C3 55 AA 49 B6 //(5.549 eURO) 
//            string respString = BitConverter.ToString(response);
//            string price1 = respString.Substring(6, 2);
//            string price2 = respString.Substring(12, 2);
//            nozzle.UntiPriceInt = int.Parse(price1 + price2);
//            nozzle.UnitPrice = (decimal)nozzle.UntiPriceInt / (decimal)Math.Pow(10, nozzle.ParentFuelPoint.UnitPriceDecimalPlaces);
//            return nozzle;
//        }

//        private byte[] requestTotals(int id)
//        {
//            byte[] Buffer = new byte[] { 0x00, (byte)(idGetDisplay(id)), 0x87 };
//            return Buffer;
//        }
//        private Common.Nozzle evalTotals(Common.Nozzle nozzle, byte[] response)
//        {   //nz1
//            //TX 00 0A 87
//            //   0 1  2     4      6    8     10    12   
//            //RX 0A 87 00 FF 00 FF 06 F9 97 68 03 FC 65 9A (optimal)

//            //rx
//            //RX 00 0A 87 00 FF 02 F4 04 FB 95 6A 69 96 21 DE
//            string[] parms = BitConverter.ToString(response).Split('-');
//            for(int i = 1; i < 7; i++)
//            {
//                if(StringToByteArray(parms[2 * i])[0] + StringToByteArray(parms[2 * i + 1])[0] != 255)
//                {
//                    throw new Exception("evalTotals() Failed");
//                }
//            }
//            string volume = parms[2] + parms[4] + parms[6] + parms[8] + parms[10] + parms[12];
//            nozzle.TotalVolume = decimal.Parse(volume);
//            return nozzle;
//        }


//        private byte[] setPrices(int unitPrice, int id)
//        {
//            // 1234 => 1.234E
//            // 100  => 0.1  E
//            // 1    =>0.001 E
//            if(unitPrice >= 9999)
//            {
//                throw new ArgumentException("max value 9999", "unitPrice");
//            }
//            else
//            {
//                string priceString = Convert.ToString(unitPrice);
//                priceString = priceString.PadLeft(4, '0'); // Zero padding => product 4characters.
//                int p1 = Convert.ToInt16(priceString.Substring(0, 2));
//                int p2 = Convert.ToInt16(priceString.Substring(2, 2));
//                byte[] Buffer = new byte[] { 0x00, (byte)(idSetPrice(id)), 0x69, (byte)ith(p1), (byte)ith(p2) };
//                return Buffer;
//            }
//        }
//        private byte[] confirmSetPrices(byte[] response, int unitPrice, int id)
//        {
//            byte[] Buffer = new byte[] { };
//            Buffer = setPrices(unitPrice, id);
//            if(response.SequenceEqual(Buffer.Skip(1).Take(4).ToArray())) //response == Buffer
//            {
//                Buffer = new byte[] { 0x00, (byte)(idSetPrice(id) + 1), (byte)(254 - idSetPrice(id)), Buffer[3], Buffer[4] };
//                return Buffer;
//            }
//            else
//            {
//                throw new ArgumentException(@"SetPrice() Failed ", "byte[] Response");
//            }
//        }

//        private byte[] authoriseFuelPoint(int id)
//        {
//            byte[] Buffer = new byte[] { 0x00, (byte)(idSetPrice(id)), 0xE1 };
//            return Buffer;
//        }
//        private byte[] confirmAuthoriseFuelPoint(byte[] response, int id)
//        {
//            byte[] Buffer = new byte[] { (byte)(idSetPrice(id)), 0xE1 };
//            if(response.SequenceEqual(Buffer))
//            {
//                Buffer = new byte[] { 0x00, (byte)(idSetPrice(id) + 1), (byte)(254 - idSetPrice(id)) };
//                return Buffer;
//            }
//            else
//            {
//                throw new ArgumentException(@"AuthoriseFuelPoint() Failed ", "byte[] Response");
//            }

//        }

//        private byte[] getFuelPointStatus(int id)
//        {
//            byte[] Buffer = new byte[] { 0x00, (byte)(idGetStatus(id)) };
//            return Buffer;
//        }
//        private Common.FuelPoint evaluateStatus(Common.FuelPoint fp, byte[] response)
//        {
//            if (response.Length == 0 && DateTime.Now.Subtract(fp.LastValidResponse).TotalSeconds > 5)
//            {
//                fp.Status = Common.Enumerators.FuelPointStatusEnum.Offline;
//                return fp;
//            }
//            if (response.Length == 0)
//                return fp;
//            fp.LastValidResponse = DateTime.Now;
//            if(response.Length != 3 || (response[1] + response[2]) != 0xFF)
//            {
//                return fp;
//            }
//            else
//            {
//                byte b1 = response[2];
//                if(b1 == 0xFF)
//                {
//                    fp.Status = Common.Enumerators.FuelPointStatusEnum.Idle;
//                }
//                else if(b1 == 0xF7)
//                {
//                    fp.Status = Common.Enumerators.FuelPointStatusEnum.Nozzle;
//                }
//                else if(b1 == 0xF5)
//                {
//                    fp.Status = Common.Enumerators.FuelPointStatusEnum.Work;
//                }
//                else if(b1 == 0xBF)
//                {
//                    fp.Status = Common.Enumerators.FuelPointStatusEnum.Idle;
//                }
//                else fp.Status = Common.Enumerators.FuelPointStatusEnum.Idle;
//                fp.DispenserStatus = fp.Status;
//                return fp;
//            }
//        }
//        #endregion
//        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//        #region Public methods

//        public Common.Nozzle GetTotals(Common.Nozzle nozzle)
//        {
//            byte[] upBuffer = requestTotals(nozzle.ParentFuelPoint.Address);
//            this.serialPort.Write(upBuffer, 0, upBuffer.Length);
//            int waiting = 0;
//            while(this.serialPort.BytesToRead < (int)responseLength.totals && waiting < 300)
//            {
//                System.Threading.Thread.Sleep(15); //15*4 = 70 [ NP 68ms response time]
//                waiting += 20;
//            }
//            byte[] response = new byte[this.serialPort.BytesToRead];
//            this.serialPort.Read(response, 0, this.serialPort.BytesToRead);
//            nozzle = evalTotals(nozzle, response);
//            //nozzle.QueryTotals = false;
//            return nozzle;
//        }
//        public Common.Nozzle GetDisplay(Common.Nozzle nozzle)
//        {
//            byte[] upBuffer = requestDisplayData(nozzle.ParentFuelPoint.Address);
//            this.serialPort.Write(upBuffer, 0, upBuffer.Length);
//            int waiting = 0;
//            while(this.serialPort.BytesToRead < (int)responseLength.display && waiting < 300)
//            {
//                System.Threading.Thread.Sleep(15); //15*4 = 70 [ NP 68ms response time]
//                waiting += 20;
//            }
//            byte[] response = new byte[this.serialPort.BytesToRead];
//            this.serialPort.Read(response, 0, this.serialPort.BytesToRead);
//            return evalDisplay(nozzle, response);

//        }
//        public bool SetPrice(Common.Nozzle nozzle, int unitPrice)
//        {
//            try
//            {
//                this.serialPort.DiscardInBuffer();
//                byte[] buffer = setPrices(unitPrice, nozzle.ParentFuelPoint.Address);
//                this.serialPort.Write(buffer, 0, buffer.Length);
//                int waiting = 0;
//                while(this.serialPort.BytesToRead < (int)responseLength.setprice && waiting < 300)
//                {
//                    System.Threading.Thread.Sleep(20);
//                    waiting += 20;
//                }
//                byte[] response = new byte[this.serialPort.BytesToRead];
//                this.serialPort.Read(response, 0, this.serialPort.BytesToRead);
//                buffer = confirmSetPrices(response, unitPrice, nozzle.ParentFuelPoint.Address);
//                this.serialPort.Write(buffer, 0, buffer.Length);
//            }
//            catch
//            {
//                return false;
//            }
//            return true;
//        }
//        public Common.Nozzle GetUnitPrice(Common.Nozzle nozzle)
//        {
//            byte[] upBuffer = requestUnitPrice(nozzle.Index);
//            this.serialPort.Write(upBuffer, 0, upBuffer.Length);
//            int waiting = 0;
//            while(this.serialPort.BytesToRead < (int)responseLength.unitprice && waiting < 300)
//            {
//                System.Threading.Thread.Sleep(15); //15*4 = 70 [ NP 68ms response time]
//                waiting += 20;
//            }
//            byte[] upResponse = new byte[this.serialPort.BytesToRead];
//            this.serialPort.Read(upResponse, 0, this.serialPort.BytesToRead);
//            return evalUnitPrice(nozzle, upResponse);
//        }
//        public Common.FuelPoint GetStatus(Common.FuelPoint fp)
//        {
//            byte[] buffer = getFuelPointStatus(fp.Address);
//            this.serialPort.Write(buffer, 0, buffer.Length);
//            int waiting = 0;
//            while(this.serialPort.BytesToRead < (int)responseLength.status && waiting < 300)
//            {
//                System.Threading.Thread.Sleep(15); //15*4 = 70 [ NP 68ms response time]
//                waiting += 20;
//            }
//            byte[] response = new byte[this.serialPort.BytesToRead];
//            this.serialPort.Read(response, 0, this.serialPort.BytesToRead);
//            return evaluateStatus(fp, response);
//        }
//        public bool AuthorizeFuelPoint(Common.FuelPoint fp)
//        {
//            try
//            {
//                this.serialPort.DiscardInBuffer();
//                byte[] buffer = authoriseFuelPoint(fp.Address);
//                this.serialPort.Write(buffer, 0, buffer.Length);
//                int waiting = 0;
//                while(this.serialPort.BytesToRead < (int)responseLength.auth && waiting < 300)
//                {
//                    System.Threading.Thread.Sleep(20);
//                    waiting += 20;
//                }
//                byte[] response = new byte[this.serialPort.BytesToRead];
//                this.serialPort.Read(response, 0, this.serialPort.BytesToRead);
//                buffer = confirmAuthoriseFuelPoint(response, fp.Address);
//                this.serialPort.Write(buffer, 0, buffer.Length);
//            }
//            catch
//            {
//                return false;
//            }
//            return true;

//        }

//        public byte[] StringToByteArray(string hex)
//        {
//            return Enumerable.Range(0, hex.Length)
//                             .Where(x => x % 2 == 0)
//                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
//                             .ToArray();
//        }

//        #endregion

//        private enum responseLength
//        {
//            totals = 14,
//            display = 16,
//            auth = 2,
//            status = 3,
//            setprice = 4,
//            unitprice = 6
//        }

//        private struct IPumpDebugArgs
//        {
//            public Common.Enumerators.FuelPointStatusEnum status;
//            public decimal totalizer;
//            public decimal volume;
//            public decimal amount;
//            public List<byte[]> comBuffer;
//        }
//    }
//}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.NuovoPignone
{
    public class NuovoPignoneProtocol : Common.IFuelProtocol
    {
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
                this.serialPort.Parity = System.IO.Ports.Parity.Odd;
                this.serialPort.BaudRate = 2400;
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
            fp = GetStatus(fp);
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
                                //Halt pump 
                                //continue;
                            }
                            if (fp.QueryResume)
                            {
                                //resume
                            }

                            if (fp.QuerySetPrice)
                            {
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
                            int nozzleForTotals = fp.Nozzles.Where(n => n.QueryTotals).Count();
                            if (nozzleForTotals > 0)
                            {
                                foreach (Common.Nozzle nz in fp.Nozzles)
                                {
                                    if (nz.QueryTotals)
                                    {
                                        if (this.GetTotals(nz))
                                        {
                                            System.Threading.Thread.Sleep(10);

                                            //if ((bool)(fp.GetExtendedProperty("iNeedDisplay", true)))
                                            //{
                                            //    if (this.GetDisplay(nz))
                                            //        fp.SetExtendedProperty("iNeedDisplay", false);
                                            //    else
                                            //    {
                                            //        if (nz.LastTotalVolume != nz.TotalVolume)
                                            //        {
                                            //            System.Threading.Thread.Sleep(100);
                                            //            this.GetDisplay(nz);
                                            //            fp.SetExtendedProperty("iNeedDisplay", false);
                                            //        }
                                            //    }
                                            //}

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

                            if (fp.QueryAuthorize)
                            {
                                //if(SetPreset(fp.ActiveNozzle))
                                //{
                                if (this.AuthorizeFuelPoint(fp))
                                    fp.QueryAuthorize = false;
                                continue;
                                // }

                            }

                            if (fp.Status == Common.Enumerators.FuelPointStatusEnum.Work)
                            {
                                fp.SetExtendedProperty("iNeedDisplay", true);
                                this.GetDisplay(fp.ActiveNozzle);
                            }
                            Common.Enumerators.FuelPointStatusEnum oldStatus = fp.Status;
                            //System.IO.File.AppendAllText(System.Environment.CurrentDirectory + "\\logs" + "\\NuovoPignoneSTATUS.txt", "\n" + Convert.ToString( fp.Status));
                            this.GetStatus(fp);
                            //this.GetDisplay(fp.Nozzles[0]);
                            //if (fp.Status != Common.Enumerators.FuelPointStatusEnum.Idle && fp.Status != Common.Enumerators.FuelPointStatusEnum.Offline)
                            //{
                            //    fp.ActiveNozzle = fp.Nozzles[0];

                            //}
                            if (oldStatus != fp.Status && this.DispenserStatusChanged != null)
                            {

                                Common.FuelPointValues values = new Common.FuelPointValues();
                                if (fp.Status != Common.Enumerators.FuelPointStatusEnum.Idle && fp.Status != Common.Enumerators.FuelPointStatusEnum.Offline)
                                {
                                    fp.ActiveNozzleIndex = 0;
                                    values.ActiveNozzle = 0;
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

                        }
                        finally
                        {
                            System.Threading.Thread.Sleep(120);
                        }

                    }
                }
                catch (Exception e)
                {
                    //System.IO.Directory.CreateDirectory(System.Environment.CurrentDirectory + "\\logs");
                    //System.IO.File.AppendAllText(System.Environment.CurrentDirectory + "\\logs" + "\\NuovoPignoneError.txt", "\n" + e.ToString()); 
                    System.Threading.Thread.Sleep(250);
                }
            }
        }

        #region SerialPort

        public int MinimumTimeNeeded(int cmdl, int respl)
        {
            double speed = 1 / (this.serialPort.BaudRate / 8000);
            return (int)((double)(cmdl + respl) * speed);
        }

        #endregion

        #region Private methods

        private int ith(int num)
        {
            return 16 * (num / 10) + (num % 10);
        }// hex = integerToHex(int)
        private int idGetStatus(int num)
        {
            return ((num - 1) * 8 + 9);
        }
        private int idGetDisplay(int num)
        {
            return ((num - 1) * 8 + 10);
        }
        private int idSetPrice(int num)
        {
            return ((num - 1) * 8 + 12);
        }
        private int idSetPreset(int num)
        {
            return idSetPrice(num);
        }
        private byte[] setPresetValue(int id, int euro)
        {

            if (euro > 999999) throw new ArgumentException("euro over 9999.99", "euro");

            string eurostring = Convert.ToString(euro);
            for (int i = eurostring.Length; i < 6; i++)
                eurostring += "0";
            int p1 = Convert.ToInt32((eurostring.Substring(0, 2)));
            int p2 = Convert.ToInt32((eurostring.Substring(2, 2)));
            int p3 = Convert.ToInt32((eurostring.Substring(4, 2)));
            byte[] Buffer = new byte[] { 0x00, (byte)(idSetPreset(id)), 0x78, (byte)ith(p1), (byte)ith(p2), (byte)ith(p3) };
            return Buffer;
        }
        private byte[] requestDisplayData(int id)
        {
            byte[] Buffer = new byte[] { 0x00, (byte)(idGetDisplay(id)), 0xB4 };
            return Buffer;

        }
        private bool evalDisplay(Common.Nozzle nozzle, byte[] response)
        {
            try
            {
                if ((byte)idGetDisplay(nozzle.ParentFuelPoint.Address) != response[0])
                    throw new Exception("evalDisplay() Failed");

                string[] parms = BitConverter.ToString(response).Split('-');
                for (int i = 1; i < 8; i++)
                {
                    if (StringToByteArray(parms[2 * i])[0] + StringToByteArray(parms[2 * i + 1])[0] != 255)
                    {
                        throw new Exception("evalDisplay() Failed");
                    }
                }

                string upString = parms[2] + parms[4] + parms[6];
                string volString = parms[8] + parms[10] + parms[12];
                decimal volDecimal = decimal.Parse(volString) / (decimal)Math.Pow(10, nozzle.ParentFuelPoint.VolumeDecimalPlaces);
                decimal priceDecimal = decimal.Parse(upString) / (decimal)Math.Pow(10, nozzle.ParentFuelPoint.AmountDecimalPlaces);
                //if (nozzle.QueryTotals && volDecimal == 0 && nozzle.LastTotalVolume != nozzle.TotalVolume)
                //    return false;

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
                        CurrentNozzleId = 1,
                        Values = values
                    });
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
        private byte[] requestUnitPrice(int id)
        {
            byte[] Buffer = new byte[] { 0x00, (byte)(idGetDisplay(id)), 0xC4 };
            return Buffer;
        }
        private Common.Nozzle evalUnitPrice(Common.Nozzle nozzle, byte[] response)
        {
            string respString = BitConverter.ToString(response);
            string price1 = respString.Substring(6, 2);
            string price2 = respString.Substring(12, 2);
            nozzle.UntiPriceInt = int.Parse(price1 + price2);
            nozzle.UnitPrice = (decimal)nozzle.UntiPriceInt / (decimal)Math.Pow(10, nozzle.ParentFuelPoint.UnitPriceDecimalPlaces);
            return nozzle;
        }
        private byte[] requestTotals(int id)
        {
            byte[] Buffer = new byte[] { 0x00, (byte)(idGetDisplay(id)), 0x87 };
            return Buffer;
        }
        private bool evalTotals(Common.Nozzle nozzle, byte[] response)
        {
            try
            {
                if ((byte)idGetDisplay(nozzle.ParentFuelPoint.Address) != response[0])
                    throw new Exception("evalTotals Failed WrongPump Failed");

                string[] parms = BitConverter.ToString(response).Split('-');
                for (int i = 1; i < 7; i++)
                {
                    if (StringToByteArray(parms[2 * i])[0] + StringToByteArray(parms[2 * i + 1])[0] != 255)
                    {
                        throw new Exception("evalTotals() Failed");
                    }
                }
                string volume = parms[2] + parms[4] + parms[6] + parms[8] + parms[10] + parms[12];
                nozzle.TotalVolume = decimal.Parse(volume);
                
                return true;
            }
            catch
            {
                return false;
            }
        }
        private byte[] setPrices(int unitPrice, int id)
        {
            // 1234 => 1.234E
            // 100  => 0.1  E
            // 1    =>0.001 E
            if (unitPrice >= 9999)
            {
                throw new ArgumentException("max value 9999", "unitPrice");
            }
            else
            {
                string priceString = Convert.ToString(unitPrice);
                priceString = priceString.PadLeft(4, '0'); // Zero padding => product 4characters.
                int p1 = Convert.ToInt16(priceString.Substring(0, 2));
                int p2 = Convert.ToInt16(priceString.Substring(2, 2));
                byte[] buffer = new byte[] { 0x00, (byte)(idSetPrice(id)), 0x69, (byte)ith(p1), (byte)ith(p2) };
                return buffer;
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
            else
            {
                throw new ArgumentException(@"SetPrice() Failed ", "byte[] Response");
            }
        }
        private byte[] authoriseFuelPoint(int id)
        {
            byte[] Buffer = new byte[] { 0x00, (byte)(idSetPrice(id)), 0xE1 };
            return Buffer;
        }
        private byte[] confirmAuthoriseFuelPoint(byte[] response, int id)
        {

            byte[] Buffer = new byte[] { 0x00, (byte)(idSetPrice(id) + 1), (byte)(254 - idSetPrice(id)) };
            return Buffer;




        }
        private byte[] getFuelPointStatus(int id)
        {
            byte[] Buffer = new byte[] { 0x00, (byte)(idGetStatus(id)) };
            return Buffer;
        }
        private Common.FuelPoint evaluateStatus2(Common.FuelPoint fp, byte[] response)
        {

            try
            {
                if (response.Length == 0 && DateTime.Now.Subtract(fp.LastValidResponse).TotalSeconds > 5)
                {
                    fp.Status = Common.Enumerators.FuelPointStatusEnum.Offline;
                    if (this.DispenserStatusChanged != null)
                    {
                        Common.FuelPointValues values = new Common.FuelPointValues();
                        values.Status = Common.Enumerators.FuelPointStatusEnum.Offline;
                        this.DispenserStatusChanged(this, new Common.FuelPointValuesArgs()
                        {
                            CurrentFuelPoint = fp,
                            CurrentNozzleId = 0,
                            Values = values
                        });
                    }
                    return fp;
                }
                if (response.Length == 0)
                    return fp;
                fp.LastValidResponse = DateTime.Now;

                byte[] temp = getFuelPointStatus(fp.Address);
                string sresponse = System.BitConverter.ToString(response);
                string id = sresponse.Substring(0, 2);
                string sstatus = sresponse.Substring(3, 2);
                if (response.Length != 3 || (response[1] + response[2]) != 0xFF)
                {
                    // System.IO.File.AppendAllText(System.Environment.CurrentDirectory + "\\logs" + "\\NuovoPignoneLOG.txt", "\nFailedEVAL");
                    return fp;
                }
                else
                {

                    if (sstatus == "80")
                    {
                        // System.IO.File.AppendAllText(System.Environment.CurrentDirectory + "\\logs" + "\\NuovoPignoneLOG.txt", "\nidle");
                        fp.Status = Common.Enumerators.FuelPointStatusEnum.Idle;
                    }
                    else if (sstatus == "88")
                    {
                        //  System.IO.File.AppendAllText(System.Environment.CurrentDirectory + "\\logs" + "\\NuovoPignoneLOG.txt", "\nnozzle");
                        fp.Status = Common.Enumerators.FuelPointStatusEnum.Nozzle;
                    }
                    else if (sstatus == "8A")
                    {
                        //  System.IO.File.AppendAllText(System.Environment.CurrentDirectory + "\\logs" + "\\NuovoPignoneLOG.txt", "\nwork");
                        fp.Status = Common.Enumerators.FuelPointStatusEnum.Work;
                    }
                    else
                    {
                        //fp.Status = Common.Enumerators.FuelPointStatusEnum.Idle;
                        //System.IO.File.AppendAllText(System.Environment.CurrentDirectory + "\\logs" + "\\NuovoPignoneLOG.txt", "\nELSE");
                    }
                    fp.DispenserStatus = fp.Status;
                    return fp;
                }
            }
            catch (Exception ex)
            {
                //System.IO.File.AppendAllText(System.Environment.CurrentDirectory + "\\logs" + "\\NuovoPignoneError.txt", "\n" + ex.ToString());
                return fp;
            }
        }
        private Common.FuelPoint evaluateStatus(Common.FuelPoint fp, byte[] response)
        {

            if (response.Length == 0 && DateTime.Now.Subtract(fp.LastValidResponse).TotalSeconds > 5)
            {
                fp.Status = Common.Enumerators.FuelPointStatusEnum.Offline;
                if (this.DispenserStatusChanged != null)
                {
                    Common.FuelPointValues values = new Common.FuelPointValues();
                    values.Status = Common.Enumerators.FuelPointStatusEnum.Offline;
                    this.DispenserStatusChanged(this, new Common.FuelPointValuesArgs()
                    {
                        CurrentFuelPoint = fp,
                        CurrentNozzleId = 0,
                        Values = values
                    });
                }
                return fp;
            }
            if (response.Length == 0)
                return fp;
            fp.LastValidResponse = DateTime.Now;

            if ((byte)idGetStatus(fp.Address) != response[0]) return fp; //wrong pump
            if (response.Length != 3 || (response[1] + response[2]) != 0xFF)
            {
                return fp;
            }
            else
            {
                byte b1 = response[2];
                if (b1 == 0xFF)
                {
                    fp.Status = Common.Enumerators.FuelPointStatusEnum.Idle;
                }
                else if (b1 == 0xF7)
                {
                    fp.Status = Common.Enumerators.FuelPointStatusEnum.Nozzle;
                }
                else if (b1 == 0xF5)
                {
                    fp.Status = Common.Enumerators.FuelPointStatusEnum.Work;
                }
                else if (b1 == 0xBF)
                {
                    fp.Status = Common.Enumerators.FuelPointStatusEnum.Idle;
                }
                else fp.Status = Common.Enumerators.FuelPointStatusEnum.Idle;
                fp.DispenserStatus = fp.Status;
                return fp;
            }
        }
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Public methods

        public bool GetTotals(Common.Nozzle nozzle)
        {
            try
            {
                byte[] upBuffer = requestTotals(nozzle.ParentFuelPoint.Address);
                this.serialPort.Write(upBuffer, 0, upBuffer.Length);
                int waiting = 0;
                while (this.serialPort.BytesToRead < (int)responseLength.totals && waiting < 300)
                {
                    System.Threading.Thread.Sleep(15); //15*4 = 70 [ NP 68ms response time]
                    waiting += 20;
                }
                byte[] response = new byte[this.serialPort.BytesToRead];
                this.serialPort.Read(response, 0, this.serialPort.BytesToRead);
                if (response.Length == 0)
                    return false;

                bool totalsOk = evalTotals(nozzle, response);
                if (totalsOk)
                {
                    if ((bool)nozzle.ParentFuelPoint.GetExtendedProperty("iNeedDisplay", true))
                    {
                        bool displayOk = this.GetDisplay(nozzle);

                        int countError = (int)nozzle.GetExtendedProperty("DisplayErrorCount", 0);
                        if (!displayOk && countError < 5)
                        {
                            nozzle.SetExtendedProperty("DisplayErrorCount", countError + 1);
                            return false;
                        }
                        nozzle.SetExtendedProperty("DisplayErrorCount", 0);
                        nozzle.ParentFuelPoint.SetExtendedProperty("iNeedDisplay", !totalsOk);
                        return totalsOk;
                    }
                }
                return totalsOk;
            }
            catch
            {
                return false;
            }
        }
        public bool GetDisplay(Common.Nozzle nozzle)
        {
            try
            {
                byte[] upBuffer = requestDisplayData(nozzle.ParentFuelPoint.Address);
                this.serialPort.Write(upBuffer, 0, upBuffer.Length);
                int waiting = 0;
                while (this.serialPort.BytesToRead < (int)responseLength.display && waiting < 300)
                {
                    System.Threading.Thread.Sleep(15); //15*4 = 70 [ NP 68ms response time]
                    waiting += 20;
                }
                if (this.serialPort.BytesToRead == 0)
                    return false;
                byte[] response = new byte[this.serialPort.BytesToRead];

                this.serialPort.Read(response, 0, this.serialPort.BytesToRead);
                return evalDisplay(nozzle, response);
            }
            catch
            {
                return false;
            }
        }
        public bool SetPrice(Common.Nozzle nozzle, int unitPrice)
        {
            try
            {
                this.serialPort.DiscardInBuffer();
                byte[] buffer = setPrices(unitPrice, nozzle.ParentFuelPoint.Address);
                this.serialPort.Write(buffer, 0, buffer.Length);
                int waiting = 0;
                while (this.serialPort.BytesToRead < (int)responseLength.setprice && waiting < 300)
                {
                    System.Threading.Thread.Sleep(20);
                    waiting += 20;
                }
                byte[] response = new byte[this.serialPort.BytesToRead];
                this.serialPort.Read(response, 0, this.serialPort.BytesToRead);
                buffer = confirmSetPrices(response, unitPrice, nozzle.ParentFuelPoint.Address);
                this.serialPort.Write(buffer, 0, buffer.Length);
            }
            catch
            {
                return false;
            }
            return true;
        }
        public Common.Nozzle GetUnitPrice(Common.Nozzle nozzle)
        {
            byte[] upBuffer = requestUnitPrice(nozzle.Index);
            this.serialPort.Write(upBuffer, 0, upBuffer.Length);
            int waiting = 0;
            while (this.serialPort.BytesToRead < (int)responseLength.unitprice && waiting < 300)
            {
                System.Threading.Thread.Sleep(15); //15*4 = 70 [ NP 68ms response time]
                waiting += 20;
            }
            byte[] upResponse = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(upResponse, 0, this.serialPort.BytesToRead);
            return evalUnitPrice(nozzle, upResponse);
        }
        public Common.FuelPoint GetStatus(Common.FuelPoint fp)
        {
            byte[] buffer = getFuelPointStatus(fp.Address);
            this.serialPort.Write(buffer, 0, buffer.Length);
            int waiting = 0;
            //System.IO.File.AppendAllText(System.Environment.CurrentDirectory + "\\logs" + "\\NuovoPignoneLOG.txt", "\n tx:" + BitConverter.ToString(buffer));
            while (this.serialPort.BytesToRead < (int)responseLength.status && waiting < 300)
            {
                System.Threading.Thread.Sleep(15); //15*4 = 70 [ NP 68ms response time]
                waiting += 20;
            }
            byte[] response = new byte[this.serialPort.BytesToRead];

            this.serialPort.Read(response, 0, this.serialPort.BytesToRead);
            if (fp.Channel == 100)
            {
                return evaluateStatus2(fp, response);
            }
            else
                return evaluateStatus(fp, response);


        }
        public bool AuthorizeFuelPoint(Common.FuelPoint fp)
        {
            try
            {

                byte[] buffer = authoriseFuelPoint(fp.Address);
                this.serialPort.Write(buffer, 0, buffer.Length);
                int waiting = 0;
                while (this.serialPort.BytesToRead < (int)responseLength.auth && waiting < 300)
                {
                    System.Threading.Thread.Sleep(20);
                    waiting += 20;
                }
                byte[] response = new byte[this.serialPort.BytesToRead];
                this.serialPort.Read(response, 0, this.serialPort.BytesToRead);
                buffer = confirmAuthoriseFuelPoint(response, fp.Address);
                this.serialPort.Write(buffer, 0, buffer.Length);
            }
            catch
            {
                return false;
            }
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
            setprice = 4,
            unitprice = 6
        }

        private struct IPumpDebugArgs
        {
            public Common.Enumerators.FuelPointStatusEnum status;
            public decimal totalizer;
            public decimal volume;
            public decimal amount;
            public List<byte[]> comBuffer;
        }
    }
}
