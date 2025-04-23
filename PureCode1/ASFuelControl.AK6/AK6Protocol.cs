using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ASFuelControl.AK6
{  
    public class AK6Protocol : Common.IFuelProtocol
    {  //37,52,53
        public AK6Protocol()
        {
            SRegex.DispenserStatusChanged +=SRegex_DispenserStatusChanged;
        }

        void SRegex_DispenserStatusChanged(object sender, Common.FuelPointValuesArgs e)
        {
            if(this.DataChanged != null)
            {
                this.DataChanged(this, e);
            }
        }
        bool isMultiConnected = true;
        public event EventHandler<Common.TotalsEventArgs> TotalsRecieved;
        public event EventHandler<Common.FuelPointValuesArgs> DataChanged;
        public event EventHandler<Common.FuelPointValuesArgs> DispenserStatusChanged;

        private System.Threading.Thread th;
        private List<Common.FuelPoint> fuelPoints=new List<Common.FuelPoint>();
        private System.IO.Ports.SerialPort serialPort=new System.IO.Ports.SerialPort();

        public Common.FuelPoint[] FuelPoints
        {
            set
            {
                this.fuelPoints=new List<Common.FuelPoint>(value);
            }
            get
            {
                return this.fuelPoints.ToArray();
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
                this.serialPort.PortName=this.CommunicationPort;
                this.serialPort.Open();
                this.th=new System.Threading.Thread(new System.Threading.ThreadStart(this.ThreadRun));
                th.Start();
            }
            catch
            {
            }
        }

        public void Disconnect()
        {
            if(this.serialPort.IsOpen)
                this.serialPort.Close();
        }

        public void AddFuelPoint(Common.FuelPoint fp)
        {
            this.fuelPoints.Add(fp);
            //fp.PropertyChanged-=new System.ComponentModel.PropertyChangedEventHandler(fp_PropertyChanged);
            //fp.PropertyChanged+=new System.ComponentModel.PropertyChangedEventHandler(fp_PropertyChanged);
        }

        public void ClearFuelPoints()
        {
            this.fuelPoints.Clear();
        }

        private void ThreadRun()
        {
            foreach(Common.FuelPoint fp in this.fuelPoints)
            {
                //fp.Nozzles[0].TotalVolume = -1;
                fp.Nozzles[0].QueryTotals = true;
            }
            while(this.IsConnected)
            
            {
                try
                {
                    foreach(Common.FuelPoint fp in this.fuelPoints)
                    {
                        try
                        {
                            //fp.Nozzles[0].QueryTotals = true;
                            this.ReadAk(fp,responseLength.any);
                            this.serialPort.DiscardInBuffer();
                            this.serialPort.DiscardOutBuffer();

                            if(fp.Nozzles[0].QueryTotals)
                            {
                                this.GetTotals(fp.Nozzles[0]);
                                System.Threading.Thread.Sleep(250);
                                continue;

                            }

                            if(!(bool)fp.GetExtendedProperty("AkInitialize", false))
                            {
                                this.Initialize(fp);
                                this.ReadAk(fp, responseLength.status);
                                System.Threading.Thread.Sleep(250);
                                continue;
                            }

                           
                            if(fp.QuerySetPrice)
                            {
                                    this.SetPrice(fp);
                                    fp.Nozzles[0].QuerySetPrice = false;
                                    System.Threading.Thread.Sleep(50);
               
                                if(fp.Nozzles.Where(n => n.QuerySetPrice).Count() == 0)
                                    fp.QuerySetPrice = false;
                                System.Threading.Thread.Sleep(250);
                                continue;
                            }

                            if(fp.QueryAuthorize)
                            {
                                this.SetPrice(fp);
                                System.Threading.Thread.Sleep(50);
                                GetStatus(fp);
                                System.Threading.Thread.Sleep(50);
                                this.Authorize(fp);
                                System.Threading.Thread.Sleep(50);
                                this.SetPrice(fp);
                                System.Threading.Thread.Sleep(50);
                                GetStatus(fp);
                                System.Threading.Thread.Sleep(50);
                                this.Authorize(fp);
                                //auth-price-auth-price
                            }

                            //if(fp.Nozzles[0].QueryTotals)
                            //{
                            //    this.GetTotals(fp.Nozzles[0]);
                            //}
                                
                        }
                        finally
                        {

                        }
                        
                        System.Threading.Thread.Sleep(250);
                    }
                }
                catch
                {
                }
            }
        }

        private void Authorize(Common.FuelPoint fp)
        {
            List<byte> command = new List<byte>();
            byte[] sufix = new byte[] { 0x00, 0x08, 0x03, (byte)fp.Channel,0x00,0x00,0x00,0x00,0x00,0x00, (byte)(11 + fp.Channel) };
            if(isMultiConnected)
            {
                byte[] multiplexer = new byte[] { 0xAA, 0x55, 0x0A, (byte)fp.Address };
                command.AddRange(multiplexer);
            }
            command.AddRange(sufix);
            this.serialPort.Write(command.ToArray(), 0, command.ToArray().Length);
            //Console.WriteLine("auth: " + BitConverter.ToString(command.ToArray()));
            System.Threading.Thread.Sleep(20);
        }
        private void Initialize(Common.FuelPoint fp)
        {  //AA 55 04 01 A1 02 0F 01 12
            //AA 55 04 01 00 02 01 01 04
           
            List<byte> command = new List<byte>();
            byte[] sufix = new byte[] { 0x00, 0x02, 0x01, (byte)fp.Channel, (byte)(3 + fp.Channel) };
            
            if(isMultiConnected)
            {
                byte[] multiplexer = new byte[] { 0xAA, 0x55, 0x04, (byte)fp.Address };
                command.AddRange(multiplexer);
            }
            command.AddRange(sufix);
            this.serialPort.Write(command.ToArray(), 0, command.ToArray().Length);
            System.Threading.Thread.Sleep(20);
            
        }
        private void InitializeB(Common.FuelPoint fp)//unused
        {  //AA 55 04 01 A1 02 0F 01 12
            //AA 55 04 01 00 02 01 01 04

            List<byte> command = new List<byte>();
            byte[] sufix = new byte[] { 0x00, (byte)fp.Channel, 0x01, 0x01, (byte)(2 + fp.Channel) };
            if(isMultiConnected)
            {
                byte[] multiplexer = new byte[] { 0xAA, 0x55, 0x04, (byte)fp.Address };
                command.AddRange(multiplexer);
            }
            command.AddRange(sufix);
            this.serialPort.Write(command.ToArray(), 0, command.ToArray().Length);
            
        }
        private byte[] CMDSetPrice(Common.FuelPoint fp)
        {   //00 0C 08 02 15 99 15 99 15 99 15 99 15 99
            byte[] price = new byte[4];
            int unitprice = fp.Nozzles[0].UntiPriceInt;
            if(unitprice == 0) unitprice = 9999;
            string priceString = unitprice.ToString();
            
            for(int i = priceString.Length; i < 4; i++)
            {
                priceString = "0" + priceString;
            }
            int upLeft = int.Parse(priceString.Substring(0, 2));
            int upRight =int.Parse(priceString.Substring(2, 2));
            byte upLeftPart = (byte)upLeft;//upl
            byte upRightPart = (byte)upRight;//upr
            byte crc = (byte)(12+8+fp.Channel + (ith(upLeft) + ith(upRight)) * 5);
            byte[] prefix = new byte[] { 0x00, 0x0C, 0x08,(byte)(fp.Channel) };
             List<byte> command = new List<byte>();
             if(isMultiConnected)
             {
                 byte[] multiplexer = new byte[] { 0xAA, 0x55, 0x0E, (byte)fp.Address };
                 command.AddRange(multiplexer);
             }
            command.AddRange(prefix);
           
            for(int k = 0; k < 5; k++)
            {
                command.Add((byte)ith(int.Parse(priceString.Substring(0, 2))));
                command.Add((byte)ith(int.Parse(priceString.Substring(2, 2))));
            }
            command.Add(crc);
            //Console.WriteLine("comWrite: " + BitConverter.ToString(command.ToArray()));
            return command.ToArray();

        }
        private int ith(int num)
        {
            return 16 * (num / 10) + (num % 10);
        }// hex = integerToHex(int)

        public bool SetPrice(Common.FuelPoint fp)
        {
            this.serialPort.DiscardInBuffer();
            this.serialPort.DiscardOutBuffer();
            try
            {
                this.serialPort.DiscardInBuffer();
                byte[] cmd = CMDSetPrice(fp);
                this.serialPort.Write(cmd, 0, cmd.Length);
                //Console.WriteLine(BitConverter.ToString(cmd));
                return true;
            }
            catch(Exception e)
            {
                return false;
            }
        }

        public void GetStatus(Common.FuelPoint fp)
        {
            //this.serialPort.DiscardInBuffer();
            //this.serialPort.DiscardOutBuffer();
            byte[] buffer = CMDGetStatus(fp);
            //Console.WriteLine("ComWrite: " +BitConverter.ToString(buffer));
            this.serialPort.Write(buffer, 0, buffer.Length);
            //int waiting = 0;
            //while(this.serialPort.BytesToRead < (int)responseLength.status && waiting < 300)
            //{
            //    System.Threading.Thread.Sleep(15); //15*4 = 70 [ NP 68ms response time]
            //    waiting += 10;
            //}
            //BitConverter.ToString(buffer);
        
        }
 

        
        //private Common.Enumerators.FuelPointStatusEnum ReturnStatus(byte flag)
        //{
        //    if(flag == 0x03) return Common.Enumerators.FuelPointStatusEnum.Nozzle;
        //    else if(flag == 0x06) return Common.Enumerators.FuelPointStatusEnum.Ready;
        //    else if(flag == 0x08) return Common.Enumerators.FuelPointStatusEnum.Work;
        //    else return Common.Enumerators.FuelPointStatusEnum.Idle;
        //}
        //private Common.Enumerators.FuelPointStatusEnum ReturnStatus(int flag)
        //{
        //    if(flag == 3) return Common.Enumerators.FuelPointStatusEnum.Nozzle;
        //    else if(flag == 6) return Common.Enumerators.FuelPointStatusEnum.Ready;
        //    else if(flag == 8) return Common.Enumerators.FuelPointStatusEnum.Work;
        //    else return Common.Enumerators.FuelPointStatusEnum.Idle;
        //}
        //private void EvalGetTotals(Common.Nozzle nozzle, byte[] response)
        //{
        //    string data = BitConverter.ToString(response);
        //    nozzle.TotalVolume = decimal.Parse(data.Substring(9, 17).Replace("-", "")) / (decimal)Math.Pow(10, 2); ;

        //}
        private byte[] CMDGetStatus(Common.FuelPoint fp)
        {   
            List<byte> command = new List<byte>();
            byte[] sufix = new byte[] { 0x00, 0x02, 0x07, (byte)fp.Channel, (byte)(9 + fp.Channel) };
            if(isMultiConnected)
            {
                byte[] multiplexer = new byte[] { 0xAA, 0x55, 0x04, (byte)fp.Address };
                command.AddRange(multiplexer);
            }
            command.AddRange(sufix);
            return command.ToArray();
        }
        private byte[] CMDGetTotals(Common.Nozzle nozzle)
        {
            List<byte> command = new List<byte>();
            byte[] sufix = new byte[] { 0xA1, 0x02, 0x0F, (byte)nozzle.ParentFuelPoint.Channel, (byte)(17 + nozzle.ParentFuelPoint.Channel) };
            if(isMultiConnected)
            {
                byte[] multiplexer = new byte[] { 0xAA, 0x55, 0x04, (byte)nozzle.ParentFuelPoint.Address };
                command.AddRange(multiplexer);
            }
            command.AddRange(sufix);
            return command.ToArray();
        }

        private void ReadAk(Common.FuelPoint fp, responseLength l)
        {
            Common.Enumerators.FuelPointStatusEnum oldStatus = fp.Status;
            this.GetStatus(fp); 
            int waiting = 0;
            while(this.serialPort.BytesToRead <(int)l && waiting < 80)
            {
                System.Threading.Thread.Sleep(15); //15*4 = 70 [ NP 68ms response time]
                waiting += 10;
            }
            
            byte[] response = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(response, 0, response.Length);
            
            SRegex.EvaluateBuffer(response,fp);

            if(oldStatus != fp.Status && this.DispenserStatusChanged != null)
            {

                Common.FuelPointValues values = new Common.FuelPointValues();
                values.Status = fp.Status;
                if(fp.Status != Common.Enumerators.FuelPointStatusEnum.Idle && fp.Status != Common.Enumerators.FuelPointStatusEnum.Offline)
                {
                    fp.ActiveNozzleIndex = 0;
                    values.ActiveNozzle = 0;
                }
                else
                {
                    fp.ActiveNozzleIndex = -1;
                    values.ActiveNozzle = -1;
                }
                this.DispenserStatusChanged(this, new Common.FuelPointValuesArgs()
                {
                    CurrentFuelPoint = fp,
                    CurrentNozzleId = 1,
                    Values = values
                });

                //if(oldStatus == Common.Enumerators.FuelPointStatusEnum.Work && fp.Status == Common.Enumerators.FuelPointStatusEnum.Idle)
                //{
                //    fp.QueryTotals = true;
                //}

            }

            if(fp.Status == Common.Enumerators.FuelPointStatusEnum.Work)
            {
                Common.FuelPointValues values = new Common.FuelPointValues();
                values.CurrentSalePrice = fp.Nozzles[0].UnitPrice;
                values.CurrentPriceTotal = fp.DispensedAmount;
                values.CurrentVolume = fp.DispensedVolume;
                //Console.WriteLine("**************************************************************");
                //Console.WriteLine("DispensedVolume : {0}, DispensedAmount : {1}", fp.DispensedVolume, fp.DispensedAmount);
                //Console.WriteLine("**************************************************************");
                    this.DataChanged(this, new Common.FuelPointValuesArgs()
                    {
                        CurrentFuelPoint = fp,
                        CurrentNozzleId = 1,
                        Values = values
                    });
            }
        }

        
        public void GetTotals(Common.Nozzle nozzle)
        {
            //Console.WriteLine("TOTALS FOR Nozzle " + nozzle.ParentFuelPoint.Channel + "and Address " + nozzle.ParentFuelPoint.Address);
            this.serialPort.DiscardInBuffer();
            this.serialPort.DiscardOutBuffer();
            byte[] buffer = CMDGetTotals(nozzle);
            this.serialPort.Write(buffer, 0, buffer.Length);
            //Console.WriteLine(BitConverter.ToString(buffer));
            //int waiting = 0;
            //while(this.serialPort.BytesToRead <200 && waiting < 300)
            //{
            //    System.Threading.Thread.Sleep(15); //15*4 = 70 [ NP 68ms response time]
            //    waiting += 10;
            //}
            
            //byte[] response = new byte[this.serialPort.BytesToRead];
            this.ReadAk(nozzle.ParentFuelPoint,responseLength.any);
            
            if(!nozzle.QueryTotals)
            {
                if(nozzle.ParentFuelPoint.Initialized == false)
                {
                    nozzle.ParentFuelPoint.Initialized = true;
                }
                if(this.TotalsRecieved!=null)
                {
                    this.TotalsRecieved(this, new Common.TotalsEventArgs(nozzle.ParentFuelPoint,1,nozzle.TotalVolume, 0));
                   

                }
            }

        }


        private enum responseLength
        {
            status = 32,
            totals = 68,
            any=104
        }
        //9600bp 60"
        //104
    }

}
