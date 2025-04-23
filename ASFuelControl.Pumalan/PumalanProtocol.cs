
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;

namespace ASFuelControl.Pumalan
{
    public class PumalanProtocol : Common.IFuelProtocol
    {
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

        public int countfp = 0;
        public void Connect()
        {
            try
            {
                this.serialPort.PortName = this.CommunicationPort;
                this.serialPort = new SerialPort(this.CommunicationPort, 4800, Parity.Odd, 7, StopBits.One);
                this.serialPort.Open();
                countfp = this.fuelPoints.Count;
                int counter = 0;
                foreach (Common.FuelPoint fp in this.fuelPoints)
                {

                    bitarray[counter, 0] = Convert.ToString(fp.Channel);
                    bitarray[counter, 1] = Convert.ToString("30");
                    bitarray[counter, 2] = Convert.ToString("0");
                    counter += 1;      
                    data_send(fp.Address,fp.Channel);                  
                }
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
            fp = GetStatus(fp,fp.Channel);
            foo.Status = fp.Status;
            return foo;
        }
        private void ThreadRun()
        {

            try
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
                                if (fp.Status == Common.Enumerators.FuelPointStatusEnum.Nozzle)
                                {
                                    fp.Status = Common.Enumerators.FuelPointStatusEnum.Work;
                                }
                                if (fp.QuerySetPrice)
                                {
                                    foreach (Common.Nozzle nz in fp.Nozzles)
                                    {
                                        if (this.SetPrice(nz, nz.UntiPriceInt,fp.Channel))
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
                                            bool totalOK = this.GetTotals(nz, fp.Channel);
                                            if (!totalOK)
                                                totalOK = this.GetTotals(nz, fp.Channel);
                                            System.Threading.Thread.Sleep(10);

                                            if ((bool)(fp.GetExtendedProperty("iNeedDisplay", true)))
                                            {
                                                int countdisplay = 0;
                                                bool okdis = false;
                                                do
                                                {
                                                    okdis = this.GetDisplay(nz, nz.ParentFuelPoint.Channel);
                                                    countdisplay += 1;
                                                }
                                                while (!okdis || countdisplay > 10);



                                                fp.SetExtendedProperty("iNeedDisplay", false);
                                            }
                                            if (totalOK)
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

                                if (fp.QueryAuthorize)
                                {
                                    if (this.AuthorizeFuelPoint(fp, fp.Channel))
                                        fp.QueryAuthorize = false;
                                  continue;
                                }
                           
                                if (fp.Status == Common.Enumerators.FuelPointStatusEnum.Work)
                                {
                                    fp.SetExtendedProperty("iNeedDisplay", true);

                                    int countdisplay = 0;
                                    bool okdis=false;
                                    do
                                    {
                                        okdis = this.GetDisplay(fp.ActiveNozzle, fp.Channel);
                                        countdisplay += 1;
                                    }
                                    while (!okdis || countdisplay > 10);
                                }
                            


                                Common.Enumerators.FuelPointStatusEnum oldStatus = fp.Status;

                                foreach (Common.Nozzle nz in fp.Nozzles)
                                {
                                    send_data2(nz.UntiPriceInt, fp, nz.ParentFuelPoint.Address, fp.Channel);
                     
                                }
                                Thread.Sleep(100);
                                if (oldStatus == Common.Enumerators.FuelPointStatusEnum.Work && fp.Status == Common.Enumerators.FuelPointStatusEnum.Idle)
                                {
                                    fp.Status = Common.Enumerators.FuelPointStatusEnum.TransactionCompleted;
                                }

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
                                System.Threading.Thread.Sleep(200);
                              
                               
                            }

                        }
                    }
                    catch (Exception e)
                    {
                        System.IO.Directory.CreateDirectory(System.Environment.CurrentDirectory + "\\logs");
                        System.IO.File.AppendAllText(System.Environment.CurrentDirectory + "\\logs" + "\\PumalanError.txt", "\n" + e.ToString());
                        System.Threading.Thread.Sleep(250);
                    }
                }
            }
            catch
            {
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
        }
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

        private byte[] requestDisplayData(int id,int channel)
        { 
            string value3 = Convert.ToString(channel + 30);
            int pchannel = Convert.ToInt32(value3) + 18;
            byte bchannel = Convert.ToByte(pchannel);
            string value = Convert.ToString(getbit(channel));
            int pt0 = Convert.ToInt32(value) + 18;
            byte bt0 = Convert.ToByte(pt0);
            int result = 0x30 ^ bchannel ^ (byte)id ^ bt0 ^ 0x43 ^ 0x03;
            int intresult = result;
            byte byteresult = Convert.ToByte(intresult);
            byte[] Buffer = new byte[] { 0x04, 0x30, bchannel, (byte)id, bt0, 0x43, 0x03, byteresult };
            return Buffer;

        }

        private Common.Nozzle evalDisplay(Common.Nozzle nozzle, byte[] response)
        {
            if (System.IO.File.Exists("PUMALAN.txt"))
            {
                System.IO.File.AppendAllText("PUMALAN.txt", "DISPLAY Buffer\t" + DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + BitConverter.ToString(response) + "\r\n");
            }
            if (response.Length > 15)
            {
                byte[] response2 = { response[14], response[13], response[12], response[11], response[10], response[9] };
                string timi = System.Text.Encoding.ASCII.GetString(response2).Trim();
                Convert.ToString(Convert.ToDouble(timi));
                double price = Convert.ToDouble(timi) / Math.Pow(10, nozzle.ParentFuelPoint.AmountDecimalPlaces);

                byte[] response22 = { response[8], response[7], response[6], response[5], response[4] };
                string timi2 = System.Text.Encoding.ASCII.GetString(response22).Trim();
                Convert.ToString(Convert.ToDouble(timi2));
                double priceVol = (double)Math.Round((decimal.Parse(timi2) / (decimal)Math.Pow(10, nozzle.ParentFuelPoint.AmountDecimalPlaces)) * nozzle.UnitPrice, 2, MidpointRounding.AwayFromZero);
                if (System.IO.File.Exists("PUMALAN_Log.txt"))
                {
                    System.IO.File.AppendAllText("PUMALAN_Log.txt", string.Format("Price : {0:N3}, Price Volume : {1:N3}\r\n", price, priceVol));
                }
                if (Math.Abs(price - priceVol) < 0.02)
                    if(priceVol > price)
                        nozzle.ParentFuelPoint.DispensedAmount = (decimal)priceVol;
                    else
                        nozzle.ParentFuelPoint.DispensedAmount = (decimal)price;
                else
                {
                    nozzle.ParentFuelPoint.DispensedAmount = (decimal)priceVol;
                }
                //nozzle.ParentFuelPoint.DispensedAmount = Math.Round((decimal.Parse(timi2) / (decimal)Math.Pow(10, nozzle.ParentFuelPoint.AmountDecimalPlaces)) * nozzle.UnitPrice, 2, MidpointRounding.AwayFromZero);
                nozzle.ParentFuelPoint.DispensedVolume = decimal.Parse(timi2) / (decimal)Math.Pow(10, nozzle.ParentFuelPoint.VolumeDecimalPlaces);
                if (System.IO.File.Exists("PUMALAN.txt"))
                {
                    System.IO.File.AppendAllText("PUMALAN.txt", "DISPLAY\t" + DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + "\t TX: Amount: " + timi + ">>" + nozzle.ParentFuelPoint.DispensedAmount.ToString("N3") + " | Volume: " + timi2 + ">>" + nozzle.ParentFuelPoint.DispensedVolume.ToString("N3") + " | Volume: " + nozzle.UnitPrice.ToString("N3") + "\r\n");
                }
            }
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
            return nozzle;
        }


        private byte[] requestUnitPrice(int id,int channel)
        {
            string value3 = Convert.ToString(channel + 30);
            int pchannel = Convert.ToInt32(value3) + 18;
            byte bchannel = Convert.ToByte(pchannel);

            string value = Convert.ToString(getbit(channel));
            int pt0 = Convert.ToInt32(value) + 18;
            byte bt0 = Convert.ToByte(pt0);

            int result = 0x30^ bchannel^ (byte)(idGetDisplay(id))^ bt0^ 0x53^ 0x30^ 0x03;
            int intresult = result;
            byte byteresult = Convert.ToByte(intresult);

            byte[] Buffer = new byte[] { 0x04, 0x30, bchannel, (byte)(idGetDisplay(id)), bt0, 0x53, 0x30, 0x03, byteresult };
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
        
        public string [,] bitarray = new string[3,3];

        
        private byte[] requestTotals(int id,int channel)
        {
                string value3 = Convert.ToString(channel + 30);
            int pchannel = Convert.ToInt32(value3) + 18;
            byte bchannel = Convert.ToByte(pchannel);

            string value = Convert.ToString(getbit(channel));
            int pt0 = Convert.ToInt32(value) + 18;
            byte bt0 = Convert.ToByte(pt0);

            int result = 0x30 ^ bchannel ^ (byte)id ^ bt0 ^ 0x56 ^ 0x30 ^ 0X03;
            int intresult = result;
            byte byteresult = Convert.ToByte(intresult);

            //     byte[] bytes4 = { 0x04, 0x30, 0x34, 0x02, bt0, 0x56, 0x30, 0X03, byteresult };
            byte[] Buffer = { 0x04, 0x30, bchannel, (byte)id, bt0, 0x56, 0x30, 0X03, byteresult };


            return Buffer;
        }
        private bool evalTotals(Common.Nozzle nozzle, byte[] response)
        {
           try
           {

               byte[] buf2 = new byte[14];
               Array.Copy(response, 4, buf2, 0, 14);

                string parms = System.Text.Encoding.ASCII.GetString(buf2).Trim();
                string total =parms;
                nozzle.TotalVolume = decimal.Parse(total)/1000;
                return true;
            }
         catch
           {
               return false;
         
           }
        }


        private byte[] setPrices(int unitPrice, int id,int channel)
        {

            if (unitPrice >= 9999)
            {
                throw new ArgumentException("max value 9999", "unitPrice");
            }
            else
            {
                string unitPriceS = "0" + Convert.ToString(unitPrice);

                if (unitPrice < 1000)
                    unitPriceS = "0" + Convert.ToString(unitPrice);
                else
                    unitPriceS = Convert.ToString(unitPrice);


                string value3 = Convert.ToString(channel + 30);
                int pchannel = Convert.ToInt32(value3) + 18;
                byte bchannel = Convert.ToByte(pchannel);

             
                string price = Convert.ToString(unitPriceS[0]);
                string price2 = Convert.ToString(unitPriceS[1]);
                string price3 = Convert.ToString(unitPriceS[2]);
                string price4 = Convert.ToString(unitPriceS[3]);


                int p0 = Convert.ToInt32(price) + 48;
                int p1 = Convert.ToInt32(price2) + 48;
                int p2 = Convert.ToInt32(price3) + 48;
                int p3 = Convert.ToInt32(price4) + 48;


                byte b0 = Convert.ToByte(p0);
                byte b1 = Convert.ToByte(p1);
                byte b2 = Convert.ToByte(p2);
                byte b3 = Convert.ToByte(p3);

                string value = Convert.ToString(getbit(channel));
                int pt0 = Convert.ToInt32(value) + 18;
                byte bt0 = Convert.ToByte(pt0);


                int result = 0x30 ^ bchannel ^ (byte)id ^ bt0 ^ 0x55 ^ 0x03 ^ b0 ^ b1 ^ b2 ^ b3;
                int intresult = result;
                byte byteresult = Convert.ToByte(intresult);
                byte[] Buffer = { 0x04, 0x30, bchannel, (byte)id, bt0, 0x55, b0, b1, b2, b3, 0x03, byteresult };
                return Buffer;
            }
        }
        private byte[] confirmSetPrices(byte[] response, int unitPrice, int id,int channel)
        {

          

            string pricev = "0" + Convert.ToString(unitPrice);
            if (unitPrice < 1000)
                pricev = "0" + Convert.ToString(unitPrice);
            else
                pricev = Convert.ToString(unitPrice);


            string value3 = Convert.ToString(channel + 30);
            int pchannel = Convert.ToInt32(value3) + 18;
            byte bchannel = Convert.ToByte(pchannel);

         
            string price = Convert.ToString(pricev[0]);
            string price2 = Convert.ToString(pricev[1]);
            string price3 = Convert.ToString(pricev[2]);
            string price4 = Convert.ToString(pricev[3]);

            int p0 = Convert.ToInt32(price) + 48;
            int p1 = Convert.ToInt32(price2) + 48;
            int p2 = Convert.ToInt32(price3) + 48;
            int p3 = Convert.ToInt32(price4) + 48;

            byte bp0 = Convert.ToByte(p0);
            byte bp1 = Convert.ToByte(p1);
            byte bp2 = Convert.ToByte(p2);
            byte bp3 = Convert.ToByte(p3);

            string value = Convert.ToString(getbit(channel));
            int pt0 = Convert.ToInt32(value) + 18;
            byte bt0 = Convert.ToByte(pt0);

            int result = 0x30 ^ bchannel ^ (byte)id ^ bt0 ^ 0x45 ^ 0x30 ^ 0x30 ^ 0x39 ^ 0x39 ^ 0x39 ^ bp0 ^ bp1 ^ bp2 ^ bp3 ^ 0x30 ^ 0x30 ^ 0x30 ^ 0x30 ^ 0x30 ^ 0x30 ^ 0x03;
            int intresult = result;
            byte byteresult = Convert.ToByte(intresult);

            //04 30 34 02 36 45 30 30 39 39 39 31 37 38 39 30 30 30 30 30 30 03 48
            byte[] bytes3 = { 0x04, 0x30, bchannel, (byte)id, bt0, 0x45, 0x30, 0x30, 0x39, 0x39, 0x39, bp0, bp1, bp2, bp3, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x03, byteresult };

            byte[] Buffer = new byte[] { };
            Buffer = setPrices(unitPrice, id,channel);
            return Buffer;

        }

        private byte[] authoriseFuelPoint(int id,int channel)
        {
            string value3 = Convert.ToString(channel + 30);
            int pchannel = Convert.ToInt32(value3) + 18;
            byte bchannel = Convert.ToByte(pchannel);
       
            
            int result = 0x30 ^ bchannel ^ (byte)id  ^ 0x30^ 0x53^ 0x30^ 0X03;
            int intresult = result;
            byte byteresult = Convert.ToByte(intresult);

            byte[] Buffer = { 0x04, 0x30, bchannel, (byte)id, 0x30, 0x53, 0x30, 0X03,byteresult};
            return Buffer;
        }
        private byte[] confirmAuthoriseFuelPoint(byte[] response, int id,int channel)
        {
          
                string value3 = Convert.ToString(channel + 30);
            int pchannel = Convert.ToInt32(value3) + 18;
            byte bchannel = Convert.ToByte(pchannel);
            byte[] Buffer = { 0x04, 0x30, bchannel, (byte)id, 0x30, 0x53, 0x30, 0X03, 0x56 };

            return Buffer;


        }

        private byte[] getFuelPointStatus(int id,int channel)
        {
            string value3 = Convert.ToString(channel + 30);
            int pchannel = Convert.ToInt32(value3) + 18;
            byte bchannel = Convert.ToByte(pchannel);

            string value = Convert.ToString(getbit(channel));
            int pt0 = Convert.ToInt32(value) + 18;
            byte bt0 = Convert.ToByte(pt0);

            int result = 0x30 ^ bchannel ^ (byte)id ^ bt0 ^ 0x53 ^ 0x30 ^ 0x03;
            //04 30 34 02 30 53 30 03 56
              //  int result = 0x30^ 0x34^ 0x02^ bt0^ 0x53^ 0x30^ 0x3;
            int intresult = result;
            byte byteresult = Convert.ToByte(intresult);

     //       byte[] Buffer = new byte[] { 0x04, 0x30, 0x34, 0x02, bt0, 0x53, 0x30, 0x03, byteresult };
            //04 30 34 02 30 53 30 03 56
            byte[] Buffer = new byte[] { 0x04, 0x30, bchannel, (byte)id, bt0, 0x53, 0x30, 0x03, byteresult };

            return Buffer;
        }
        private Common.FuelPoint evaluateStatus(Common.FuelPoint fp, byte[] response)
        {
            if (response.Length == 0 && DateTime.Now.Subtract(fp.LastValidResponse).TotalSeconds > 5)
            {
                if (this.DispenserOffline != null)
                    this.DispenserOffline(fp, new EventArgs());
                return fp;
            }
            if (response.Length == 0)
                return fp;
            fp.LastValidResponse = DateTime.Now;
            if (response.Length !=15 )
            {

                return fp;
            }
            else
            {

                Thread.Sleep(50);
                byte b1 = response[6];
                if ((byte)b1 == 0X50)
                {
                    fp.Status = Common.Enumerators.FuelPointStatusEnum.Idle;
                }
                else if (b1 == 0x70)
                {
                    fp.Status = Common.Enumerators.FuelPointStatusEnum.TransactionCompleted;
                }
                else if ((byte)b1 == 0x53)
                {
                    if (fp.Status != Common.Enumerators.FuelPointStatusEnum.Work)              
                    fp.Status = Common.Enumerators.FuelPointStatusEnum.Nozzle;
               
                }           
                else fp.Status = Common.Enumerators.FuelPointStatusEnum.Idle;
             
              fp.DispenserStatus = fp.Status;




                return fp;
            }
        }
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Public methods


        public bool GetTotals(Common.Nozzle nozzle,int channel)
        {
            try
            {
                byte[] bytest = { 0x06, 0x30, 0x34, 0x02, 0x30, 0x53, 0x30, 0X03, 0x56 };
                byte[] upBuffer = requestTotals(nozzle.ParentFuelPoint.Address, channel);

                this.serialPort.Write(bytest, 0, bytest.Length);
                this.serialPort.Write(upBuffer, 0, upBuffer.Length);


                int waiting = 0;

                System.Threading.Thread.Sleep(25);

                while (this.serialPort.BytesToRead < 25 && waiting < 200)
                {
                    System.Threading.Thread.Sleep(10);
                    waiting = waiting + 10;
                }



                byte[] response;
                response = new byte[this.serialPort.BytesToRead];
                this.serialPort.Read(response, 0, this.serialPort.BytesToRead);
                if (response.Length != 25)
                    return false;


                if (Convert.ToString(response[2]) == "84")
                {
                    bool totalGot = evalTotals(nozzle, response);

                    return totalGot;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }   
        }

        public bool GetDisplay(Common.Nozzle nozzle,int channel)
        {
            try
            {

                byte[] bytest = { 0x06, 0x30, 0x34, 0x02, 0x30, 0x53, 0x30, 0X03, 0x56 };
                string value3 = Convert.ToString(channel + 30);
                int pchannel = Convert.ToInt32(value3) + 18;
                byte bchannel = Convert.ToByte(pchannel);

                byte[] upBuffer = requestDisplayData(nozzle.ParentFuelPoint.Address, channel);
                this.serialPort.Write(bytest, 0, bytest.Length);
                Thread.Sleep(50);
                byte[] clear = new byte[this.serialPort.BytesToRead];
                this.serialPort.Read(clear, 0, this.serialPort.BytesToRead);
                this.serialPort.Write(upBuffer, 0, upBuffer.Length);

                int waiting = 0;

                while (this.serialPort.BytesToRead != 18 && waiting < 200)
                {
                    System.Threading.Thread.Sleep(10);
                    waiting = waiting + 10;
                }
                if (this.serialPort.BytesToRead == 18)
                {
                    byte[] response;
                    response = new byte[this.serialPort.BytesToRead];
                    this.serialPort.Read(response, 0, this.serialPort.BytesToRead);
                    evalDisplay(nozzle, response);
                    return true;
                }
                else
                    return false;
            }
            catch
            {
                return false;
            }
        
        }
        public bool SetPrice(Common.Nozzle nozzle, int unitPrice,int channel)
        {
            try
            {
                this.serialPort.DiscardInBuffer();
                byte[] buffer = setPrices(unitPrice, nozzle.ParentFuelPoint.Address,channel);
                this.serialPort.Write(buffer, 0, buffer.Length);
                int waiting = 0;


                while (this.serialPort.BytesToRead < (int)responseLength.setprice && waiting < 140)
                {
                    System.Threading.Thread.Sleep(20);
                    waiting += 20;
                }
                byte[] response = new byte[this.serialPort.BytesToRead];
                this.serialPort.Read(response, 0, this.serialPort.BytesToRead);
                buffer = confirmSetPrices(response, unitPrice, nozzle.ParentFuelPoint.Address,channel);
                this.serialPort.Write(buffer, 0, buffer.Length);



            }
            catch
            {
                return false;
            }
            return true;
        }
        public Common.Nozzle GetUnitPrice(Common.Nozzle nozzle,int channel)
        {
            byte[] upBuffer = requestUnitPrice(nozzle.Index,channel);
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
        public Common.FuelPoint GetStatus(Common.FuelPoint fp,int channel)
        {

           this.serialPort.DiscardInBuffer();
           this.serialPort.DiscardOutBuffer();

            byte[] buffer = getFuelPointStatus(fp.Address,channel);   
            this.serialPort.Write(buffer, 0, buffer.Length);
            int waiting = 0;
          

            while (this.serialPort.BytesToRead < 15 && waiting < 200)
            {        
                System.Threading.Thread.Sleep(10);
                waiting = waiting + 10;
            }
            byte[] response;
            response = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(response, 0, this.serialPort.BytesToRead);

            return evaluateStatus(fp, response);
        }
        public bool AuthorizeFuelPoint(Common.FuelPoint fp,int channel)
        {
            try
            {
          
                this.serialPort.DiscardInBuffer();
                byte[] buffer = authoriseFuelPoint(fp.Address,channel);
                this.serialPort.Write(buffer, 0, buffer.Length);
                int waiting = 0;

                System.Threading.Thread.Sleep(25);

                while (this.serialPort.BytesToRead < 15 && waiting < 200)
                {
                    System.Threading.Thread.Sleep(10);
                    waiting = waiting + 10;
                }

                byte[] response;
                response = new byte[this.serialPort.BytesToRead];
                this.serialPort.Read(response, 0, this.serialPort.BytesToRead);
                if (response.Length != 15)
                    return false;
                else
                {
              
                    buffer = confirmAuthoriseFuelPoint(response, fp.Address,channel);
                }
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

        private void data_send(int id,int channel)
        {

            string value = Convert.ToString(channel+30);
            int p0 = Convert.ToInt32(value) + 18;
            byte b0 = Convert.ToByte(p0);

            byte[] bytest = { 0x06, 0x30, 0x34, 0x02, 0x30, 0x53, 0x30, 0X03, 0x56 };
            if (getpos(channel) == "0")
            {


                int result4 =  0x30^ b0^ (byte)id^ 0x30^ 0x53^ 0x30^ 0X03;
                int intresult4 = result4;
                byte byteresult4 = Convert.ToByte(intresult4);


                //04 30 34 02 30 53 30 03 56
                byte[] bytes1 = { 0x04, 0x30, b0, (byte)id, 0x30, 0x53, 0x30, 0X03, byteresult4 };
                serialPort.Write(bytes1, 0, bytes1.Length);
                Thread.Sleep(200);
                serialPort.Write(bytest, 0, bytest.Length);
                Thread.Sleep(200);
                calculate_bits(channel, Convert.ToInt32(getpos(channel)) + 1, Convert.ToInt32(getbit(channel)) + 1);
              
            }
            if (getpos(channel)=="1")
            {

                int result4 = 0x30^ b0^ (byte)id^ 0x31^ 0x50^ 0X03;
                int intresult4 = result4;
                byte byteresult4 = Convert.ToByte(intresult4);

                //04 30 34 02 31 50 03 64
                byte[] bytes2 = { 0x04, 0x30, b0, (byte)id, 0x31, 0x50, 0X03, byteresult4 };
                serialPort.Write(bytes2, 0, bytes2.Length);
                Thread.Sleep(200);
                serialPort.Write(bytest, 0, bytest.Length);
                Thread.Sleep(200);
                calculate_bits(channel, Convert.ToInt32(getpos(channel)) + 1, Convert.ToInt32(getbit(channel)) + 1);
           
            }
            if (getpos(channel) == "2")
            {

                int result4 =0x30^ b0^ (byte)id^ 0x32^ 0x56^ 0X30^ 0x03;
                int intresult4 = result4;
                byte byteresult4 = Convert.ToByte(intresult4);
                //  04  30 34 02 32  56  30  03  51
                byte[] bytes3 = { 0x04, 0x30, b0, (byte)id, 0x32, 0x56, 0X30, 0x03, byteresult4 };
                serialPort.Write(bytes3, 0, bytes3.Length);
                Thread.Sleep(200);
                serialPort.Write(bytest, 0, bytest.Length);
                Thread.Sleep(200);
                calculate_bits(channel, Convert.ToInt32(getpos(channel)) + 1, Convert.ToInt32(getbit(channel)) + 1);
           
            }
            if (getpos(channel) == "3")
            {
                int result4 =  0x30^ b0^ (byte)id^ 0x33^ 0x56^ 0x30^ 0X03;
                int intresult4 = result4;
                byte byteresult4 = Convert.ToByte(intresult4);

                //04 30 34 02 33 56 30 03 50
                byte[] bytes4 = { 0x04, 0x30, b0, (byte)id, 0x33, 0x56, 0x30, 0X03, byteresult4 };
                serialPort.Write(bytes4, 0, bytes4.Length);
                Thread.Sleep(200);
                serialPort.Write(bytest, 0, bytest.Length);
                Thread.Sleep(200);
                calculate_bits(channel, Convert.ToInt32(getpos(channel)) + 1, Convert.ToInt32(getbit(channel)) + 1);
           
            }
            if (getpos(channel) == "4")
            {

                int result4 = 0x30^ b0^ (byte)id^ 0x34^ 0x53^ 0x30^ 0X03;
                int intresult4 = result4;
                byte byteresult4 = Convert.ToByte(intresult4);

                //04 30 34 02 34 53 30 03 52
                byte[] bytes5 = { 0x04, 0x30, b0, (byte)id, 0x34, 0x53, 0x30, 0X03, byteresult4 };
                serialPort.Write(bytes5, 0, bytes5.Length);
                Thread.Sleep(200);
                serialPort.Write(bytest, 0, bytest.Length);
                Thread.Sleep(200);
                calculate_bits(channel, Convert.ToInt32(getpos(channel)) + 1, Convert.ToInt32(getbit(channel)) + 1);
           
            }

            if (getpos(channel) == "5")
            {
                int result4 =  0x30^ b0^ (byte)id^ 0x35^ 0x50^ 0X03;
                int intresult4 = result4;
                byte byteresult4 = Convert.ToByte(intresult4);

                //04 30 34 02 35 50 03 60
                byte[] bytes6 = { 0x04, 0x30, b0, (byte)id, 0x35, 0x50, 0X03, byteresult4 };
                serialPort.Write(bytes6, 0, bytes6.Length);
                Thread.Sleep(200);
                serialPort.Write(bytest, 0, bytest.Length);
                Thread.Sleep(200);
                calculate_bits(channel, Convert.ToInt32(getpos(channel)) + 1, Convert.ToInt32(getbit(channel)) + 1);
           
            }
        }

        #region ever sending

        private void send_data2(int unitprice, Common.FuelPoint fp,int id,int channel)    
        {

            string value3 = Convert.ToString(channel + 30);
            int pchannel = Convert.ToInt32(value3) + 18;
            byte bchannel = Convert.ToByte(pchannel);

            byte[] bytest = { 0x06, 0x30, 0x34, (byte)id, 0x30, 0x53, 0x30, 0X03, 0x56 };
            if (getpos(channel) == "6")
            {
                //this.serialPort.DiscardInBuffer();
                //this.serialPort.DiscardOutBuffer();
                string value = Convert.ToString(getbit(channel));
                int p0 = Convert.ToInt32(value) + 18;
                byte b0 = Convert.ToByte(p0);

                if (unitprice < 1000)
                    calculate_price("0" + Convert.ToString(unitprice), b0, id, channel);
                else
                    calculate_price(Convert.ToString(unitprice), b0, id, channel);

                calculate_bits(channel, Convert.ToInt32(getpos(channel)) + 1, Convert.ToInt32(getbit(channel)) + 1);

                Thread.Sleep(100);
            }

            if (getpos(channel) == "7")
            {

                byte[] response;
                response = new byte[this.serialPort.BytesToRead];
                this.serialPort.Read(response, 0, this.serialPort.BytesToRead);
                Common.Enumerators.FuelPointStatusEnum oldStatus = fp.Status;
                GetStatus(fp,channel);

     //           Common.Enumerators.FuelPointStatusEnum oldStatus = fp.Status;         
     //           string value4 = Convert.ToString(getbit(channel));
     //           int p04 = Convert.ToInt32(value4) + 18;
     //           byte b04 = Convert.ToByte(p04);
     //           int result4 = 0x30 ^ bchannel ^ (byte)id ^ b04 ^ 0x53 ^ 0x30 ^ 0x03;
     //           int intresult4 = result4;
     //           byte byteresult4 = Convert.ToByte(intresult4);
     //           byte[] bytes = { 0x04, 0x30, bchannel, (byte)id, b04, 0x53, 0x30, 0x03, byteresult4 };
     //           serialPort.Write(bytes, 0, bytes.Length);
     //         int waiting = 0;
     //             Thread.Sleep(100);

     //             while (this.serialPort.BytesToRead < 15 && waiting < 200)
     //           {
     //               System.Threading.Thread.Sleep(10);
     //               waiting = waiting + 10;
     //           }
     //           byte[] response;
     //           response = new byte[this.serialPort.BytesToRead];
     //           this.serialPort.Read(response, 0, this.serialPort.BytesToRead);
     //             Thread.Sleep(100);  
             
             
     serialPort.Write(bytest, 0, bytest.Length);
      
                calculate_bits(channel, Convert.ToInt32(getpos(channel)) + 1, Convert.ToInt32(getbit(channel)) + 1);
            }
            if (getpos(channel) == "8")
            {

                //this.serialPort.DiscardInBuffer();
                //this.serialPort.DiscardOutBuffer();
                //  04 30 34 02 38  50  03  6d
                string value66 = Convert.ToString(getbit(channel));
                int p06 = Convert.ToInt32(value66) + 18;
                byte b06 = Convert.ToByte(p06);
                int result6 = 0x30 ^ bchannel ^ (byte)id ^ b06 ^ 0x50 ^ 0x03;
                int intresult6 = result6;
                byte byteresult6 = Convert.ToByte(intresult6);
                byte[] bytes6 = { 0x04, 0x30, bchannel, (byte)id, b06, 0x50, 0x03, byteresult6 };
                serialPort.Write(bytes6, 0, bytes6.Length);
               Thread.Sleep(100); 
                byte[] response;
                response = new byte[this.serialPort.BytesToRead];
                this.serialPort.Read(response, 0, this.serialPort.BytesToRead);
                serialPort.Write(bytest, 0, bytest.Length);
            
                calculate_bits(channel, Convert.ToInt32(getpos(channel)) + 1, Convert.ToInt32(getbit(channel)) + 1);    
            }

        #endregion
        }
        #region calc price
        private void calculate_price(string price_value, byte b0,int id,int channel)
        {

         

            string value3 = Convert.ToString(channel + 30);
            int pchannel = Convert.ToInt32(value3) + 18;
            byte bchannel = Convert.ToByte(pchannel);

            byte[] bytest = { 0x06, 0x30, 0x34, (byte)id, 0x30, 0x53, 0x30, 0X03, 0x56 };
            string pricev = Convert.ToString(price_value);
            string price = Convert.ToString(pricev[0]);
            string price2 = Convert.ToString(pricev[1]);
            string price3 = Convert.ToString(pricev[2]);
            string price4 = Convert.ToString(pricev[3]);

            int p0 = Convert.ToInt32(price) + 48;
            int p1 = Convert.ToInt32(price2) + 48;
            int p2 = Convert.ToInt32(price3) + 48;
            int p3 = Convert.ToInt32(price4) + 48;

            byte bp0 = Convert.ToByte(p0);
            byte bp1 = Convert.ToByte(p1);
            byte bp2 = Convert.ToByte(p2);
            byte bp3 = Convert.ToByte(p3);

            int result = 0x30 ^ bchannel ^ (byte)id ^ b0 ^ 0x45 ^ 0x30 ^ 0x30 ^ 0x39 ^ 0x39 ^ 0x39 ^ bp0 ^ bp1 ^ bp2 ^ bp3 ^ 0x30 ^ 0x30 ^ 0x30 ^ 0x30 ^ 0x30 ^ 0x30 ^ 0x03;
            int intresult = result;
            byte byteresult = Convert.ToByte(intresult);

            //04 30 34 02 36 45 30 30 39 39 39 31 37 38 39 30 30 30 30 30 30 03 48
            byte[] bytes3 = { 0x04, 0x30, bchannel, (byte)id, b0, 0x45, 0x30, 0x30, 0x39, 0x39, 0x39, bp0, bp1, bp2, bp3, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x03, byteresult };
            serialPort.Write(bytes3, 0, bytes3.Length);
            Thread.Sleep(100);
            byte[] response;
            response = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(response, 0, this.serialPort.BytesToRead);

            serialPort.Write(bytest, 0, bytest.Length);
          
        }

        private void calculate_bits(int intchannel,int intposition,int intbit)
        {
           
                if (intposition ==9)
                    intposition = 6;
                if (intbit >39)
                    intbit = 31;

                string bitcell = Convert.ToString(intbit);
                string positioncell = Convert.ToString(intposition);
                string channelcell = Convert.ToString(intchannel);


                for (int i = 0; i < countfp; i++)
                {
                    if (bitarray[i, 0] == channelcell)
                    {
                        bitarray[i, 1] = bitcell;
                        bitarray[i, 2] = positioncell;
                        break;
                    }
                }
           
          
       }


        private string getbit(int intchannel)
        {
            string channelcell = Convert.ToString(intchannel);
            string returnbit = "";
          
            for (int i = 0; i < countfp; i++)
            {
                if (bitarray[i, 0] == channelcell)
                {
                    returnbit = bitarray[i, 1];
                    break;                
            }
            }
           
            return returnbit;
        }

        private string getpos(int intchannel)
        {
            string channelcell = Convert.ToString(intchannel);
            string returnpos = "";

            for (int i = 0; i < countfp; i++)
                {
                    if (bitarray[i, 0] == channelcell)
                    {
                        returnpos = bitarray[i, 2];
                        break;
                    }
                }
         
            return returnpos;
        }
        #endregion
    }
}