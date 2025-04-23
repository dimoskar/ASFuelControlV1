using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;

namespace MillenniumAPI
{
    public class dispenserCommands
    {
        private string sprice;
        //private int price=9999;
        public dispenserCommands() { }

        private int ith(int num) { return 16 * (num / 10) + (num % 10); }// hex = integerToHex(int)

        public byte[] createCmd(int dispenserId, int fuelPointChannel, CommandTypeEnum command)
        {   // inputs: dispenser id, fuelpoint channel, type of command.
            // return the desired command for that id and channel.
            // Setup of a dispenser with 2 fuelpoints. 
            // pump1: address1 = dispenser's address, channel = 1.
            // pump2: address2 = dispenser's address, channel = 2.

            byte[] buffer = new byte[] { 0x00 };
            switch (command)
            {
                case CommandTypeEnum.RequestStatus:
                    buffer = new byte[] { 0xFD, (byte)dispenserId, 0x00, 0x02, 0x01, 0x00, 0x40, 0x00, 0x04, 0x01, (byte)(fuelPointChannel + 32), 0x3C, 0x00, 0xFC };
                    break;
                case CommandTypeEnum.FetchBuffer:
                    buffer = new byte[] { 0xFF, (byte)dispenserId };
                    return buffer;
                case CommandTypeEnum.SendPrices:
                    throw new Exception("wrong format, use createCmd(int dispenserId, int fuelPointChannel, CommandTypeEnum command, int price) to setPrices");
                case CommandTypeEnum.Authorize:
                    buffer = new byte[] { 0xFD, (byte)dispenserId, 0x00, 0x02, 0x01, 0x00, 0x40, 0x00, 0x07, 0x01, (byte)(fuelPointChannel + 32), 0x19, 0x01, 0x01, 0x3E, 0x00, 0xFC };
                    break;
                case CommandTypeEnum.CloseNozzle:
                    buffer = new byte[] { 0xFD, (byte)dispenserId, 0x00, 0x02, 0x01, 0x00, 0x40, 0x00, 0x04, 0x01, (byte)(fuelPointChannel + 32), 0x3D, 0x00, 0xFC };
                    break;
                case CommandTypeEnum.OpenNozzle:
                    buffer = new byte[] { 0xFD, (byte)dispenserId, 0x00, 0x02, 0x01, 0x00, 0x40, 0x00, 0x04, 0x01, (byte)(fuelPointChannel + 32), 0x3C, 0x00, 0xFC };
                    break;
                case CommandTypeEnum.RequestTotals:
                    buffer = new byte[] { 0xFD, (byte)dispenserId, 0x00, 0x02, 0x01, 0x00, 0x00, 0x00, 0x05, 0x02, (byte)(fuelPointChannel + 32), 0x11, 0x14, 0x15, 0xFC };
                    break;
                case CommandTypeEnum.RequestDisplayData:
                    //this command return Display while fueling
                    buffer = new byte[] { 0xFD, (byte)dispenserId, 0x00, 0x02, 0x01, 0x00, 0x00, 0x00, 0x05, 0x01, (byte)(fuelPointChannel + 32), 0x22, 0x23, 0x1D, 0xFC };
                    break;
                case CommandTypeEnum.GetLastSalesId:
                    buffer = new byte[] { 0xFD, (byte)dispenserId, 0x00, 0x02, 0x01, 0x00, 0x00, 0x00, 0x06, 0x02, 0x21, (byte)(fuelPointChannel + 32), 0x14, 0x15, 0x16, 0xFC };
                    break;
            }
            return returnCmdWithCrc(buffer);
        }

        public byte[] createCmd(int dispenserId, int fuelPointChannel, CommandTypeEnum command, int price)
        {
            byte[] buffer = new byte[] { 0xFD, 0x00, 0xFC };
            if (command == CommandTypeEnum.SendPrices)
            {
                sprice = price.ToString();
                int p1 = Convert.ToInt32((sprice.Substring(0, 2)));
                int p2 = Convert.ToInt32((sprice.Substring(2, 2)));
                buffer = new byte[] { 0xFD, (byte)dispenserId, 0x00, 0x02, 0x01, 0x00, 0x40, 0x00, 0x0D, 0x06, 0x61, 0x00, 0x00, 0x00, (byte)(fuelPointChannel), 0x11, 0x02, 0x04, 0x03, 0x00, (byte)ith(p1), (byte)ith(p2), 0xFC };
                return returnCmdWithCrc(buffer);
            }
            else
            {
                throw new System.ArgumentException("you cannot useoverloeaded creatCMD() for CommandTypeEnum{0} ", "command");
            }
        } //overloaded

        private byte[] returnCmdWithCrc(byte[] buffer)
        {   //input buffer    FD xx ... xx FC
            //return          FD xx ... xx FC crc FB
            if (buffer.Last() != 0xFC || buffer[0] != 0xFD) { throw new Exception("this is not valid command"); }
            else
            {
                int sumR = 0;
                int sumL = 0;
                int temp = 0;
                int crc0 = 0;
                int crc1 = 0;
                int crc2 = 0;

                // byte[] buffer = new byte[] { 0xFD, 0x07, 0x00, 0x02, 0x01, 0x00, 0x40, 0x00, 0x0D, 0x06, 0x61, 0x00, 0x00, 0x00, 0x05, 0x11, 0x02, 0x04, 0x03, 0x00, 0x44, 0x40, 0xFC };
                string command = BitConverter.ToString(buffer.Skip(1).Take(buffer.Length - 2).ToArray()); //drop FD and FC
                string[] commandParms = command.Split('-'); //commandParms = xx-xx-xx-xx and parms are hex form.

                for (int j = 0; j < 2; j++)
                {
                    for (int i = 0; i < commandParms.Length; i++)
                    {
                        temp = Int32.Parse(commandParms[i][j].ToString(), System.Globalization.NumberStyles.HexNumber); //hex to int
                        if (j == 0)
                        {
                            sumL += Convert.ToInt32(temp);
                        }
                        else
                        {
                            sumR += Convert.ToInt32(temp);
                        }
                    }
                }

                for (int i = 1; i < sumR + 1; i++)
                {
                    crc0++;
                    if (crc0 == 16) { crc1++; crc0 = 0; }
                }

                for (int i = 1; i < sumL + 1; i++)
                {
                    crc1++;
                    if (crc1 == 16) { crc2++; crc1 = 0; }
                }

                //convert these intergers to Bytes
                if (crc0 < 10) crc0 += 48; else crc0 += 55;
                if (crc1 < 10) crc1 += 48; else crc1 += 55;
                if (crc2 < 10) crc2 += 48; else crc2 += 55;

                List<byte> cmdWithCrc = new List<byte>();
                cmdWithCrc.AddRange(buffer);
                cmdWithCrc.Add(0x30);
                cmdWithCrc.Add((byte)crc2);
                cmdWithCrc.Add((byte)crc1);
                cmdWithCrc.Add((byte)crc0);
                cmdWithCrc.Add(0xFB);
                return cmdWithCrc.ToArray();

            }

        }

        private string calculateCrc(string buffer)
        {   //input buffer    FD xx ... xx FC
            //return          FD xx ... xx FC crc FB
            //Console.WriteLine(buffer);
            int sumR = 0;
            int sumL = 0;
            int temp = 0;
            int crc0 = 0;
            int crc1 = 0;
            int crc2 = 0;

            string[] commandParms = buffer.Split('-'); //commandParms = xx-xx-xx-xx and parms are hex form.

            for (int j = 0; j < 2; j++)
            {
                for (int i = 0; i < commandParms.Length; i++)
                {
                    temp = Int32.Parse(commandParms[i][j].ToString(), System.Globalization.NumberStyles.HexNumber); //hex to int
                    if (j == 0)
                    {
                        sumL += Convert.ToInt32(temp);
                    }
                    else
                    {
                        sumR += Convert.ToInt32(temp);
                        //Console.Write("{0} => {1}\n", commandParms[i][j].ToString(), temp);

                    }

                }
            }

            //Console.WriteLine(sumR);
            for (int i = 1; i < sumR + 1; i++)
            {
                crc0++;
                if (crc0 == 16) { crc1++; crc0 = 0; }

            }

            for (int i = 1; i < sumL + 1; i++)
            {
                crc1++;
                if (crc1 == 16) { crc2++; crc1 = 0; }
            }

            //convert these intergers to hex
            if (crc0 < 10) { crc0 += 30; } else { crc0 += 31; }
            if (crc1 < 10) { crc1 += 30; } else { crc1 += 31; }
            if (crc2 < 10) { crc2 += 30; } else { crc2 += 31; }

            return ("30" + "-" + crc2.ToString() + "-" + crc1.ToString() + "-" + crc0.ToString());
        }

        public List<Nozzle> evaluateBuffer(byte[] buffer)
        {
            string price = "";
            string litres = "";
            string totalLitres = "";
            string rawResponseWithoutCrc = "";
            string originalCrc = "";
            string calculatedCrc = "";
            string currentResponse = "";
            string bufferString = BitConverter.ToString(buffer.ToArray(),0);
            //string bufferString = String.Concat(Array.ConvertAll(buffer, x => x.ToString("X2"))); 
            MatchCollection possibleResponses = Regex.Matches(bufferString, @"(?<=FD+\-)(.*?)(?=\-+FB)"); //parse all commands 0xFD---,0xFB,0xFB 
            // Copy groups to a string array
            List<string> responses = new List<string>();
            //string[] commands = new string[possibleCmd.Count];
            for (int i = 0; i < possibleResponses.Count; i++)
            { //String.Compare(s1, s2, true)
                currentResponse = possibleResponses[i].Groups[1].Value;
                originalCrc = currentResponse.Substring(currentResponse.Length - 11);//keep the last 4 bytes [crc]
                //Console.WriteLine(currentResponse.Substring(0, currentResponse.Length - 15));
                rawResponseWithoutCrc = currentResponse.Substring(0, currentResponse.Length - 15);
                calculatedCrc = calculateCrc(rawResponseWithoutCrc); //discard oroginal crc, evaluate crc from scratch

                if (string.Compare(calculatedCrc, originalCrc, true) == 0) // if origina crc == calculated 
                {
                    responses.Add(rawResponseWithoutCrc); //inclued that command in valid commands list
                    //       currentRespone: 02-01-01-00-00-20-00-08-01-22-14-01-06-15-01-01-FC-30-30-38-31
                    //rawResponseWithoutCrc: 02-01-01-00-00-20-00-08-01-22-14-01-06-15-01-01
                    //          originalCrc: 30-30-38-31
                    //        calculatedCrc: 30-30-38-31
                }
            }
            //nozzle status 
            //xx-01-01-00-00-20-00-08-01-xx-14-01-06-15-01-00
            List<Nozzle> nozzles = new List<Nozzle>();
            foreach (string response in responses) //anlyze these commands
            {
                Nozzle affected_nozzle = new Nozzle();
                affected_nozzle.dispenserID = int.Parse(response.Substring(0, 2), NumberStyles.AllowHexSpecifier);
                affected_nozzle.fuelPointChannel = Convert.ToInt32(response.Substring(27, 2)) - 20;
                switch (response.Length)
                {
                    case ((int)ResponseLengthEnum.State):
                        affected_nozzle.LastResponseType = ResponseTypeEnum.nzStateChanged;
                        string state = response.Substring(34, 1);
                        if (state == "D")
                        {
                            affected_nozzle.closed = true;
                            affected_nozzle.Status = FuelPointStatusEnum.Close;
                        }
                        else if (state == "C")
                        {
                            affected_nozzle.open = true;
                            affected_nozzle.Status = FuelPointStatusEnum.Idle;
                        }
                        else continue;
                        break;
                  
                    case ((int)ResponseLengthEnum.Status):
                        affected_nozzle.LastResponseType = ResponseTypeEnum.nzStatus;
                        int status = Convert.ToInt32(response.Substring(36, 2));
                        switch (status)
                        {
                            case (2):
                                affected_nozzle.closed = true;
                                affected_nozzle.Status = FuelPointStatusEnum.Close;
                                break;
                            case (3):
                                affected_nozzle.idle = true;
                                affected_nozzle.Status = FuelPointStatusEnum.Idle;
                                break;
                            case (4):
                                affected_nozzle.nozzle = true;
                                affected_nozzle.Status = FuelPointStatusEnum.Nozzle;
                                break;
                            case (6):
                                affected_nozzle.authorized = true;
                                affected_nozzle.Status = FuelPointStatusEnum.Work;
                                break;
                        }
                        if (Convert.ToInt32(response.Substring(46, 1)) == 1)
                        {
                            affected_nozzle.error = true;
                            affected_nozzle.Status = FuelPointStatusEnum.Error;
                        }
                        break;
                    
                    case ((int)ResponseLengthEnum.DisplayWhileFueling):
                        affected_nozzle.LastResponseType = ResponseTypeEnum.DisplayWhileFueling;
                        affected_nozzle.Status = FuelPointStatusEnum.Work;
                        price = response.Substring(39, 8).Replace("-", "");//001110
                        price = price.Substring(0, 4) + "," + price.Substring(4, 2);
                        affected_nozzle.displayPrice = Decimal.Parse(price);
                        litres = response.Substring(60, 8).Replace("-", "");
                        litres = litres.Substring(0, 4) + "," + litres.Substring(4, 2);
                        affected_nozzle.displayLitres = Decimal.Parse(litres);
                        affected_nozzle.checkLCD = true;
                        break;
                    
                    case ((int)ResponseLengthEnum.TransactionSummary):
                        affected_nozzle.LastResponseType = ResponseTypeEnum.TransactionSummary;
                        affected_nozzle.Status = FuelPointStatusEnum.SalingCompleted;
                        price = response.Substring(87, 8).Replace("-", "");//001110
                        price = price.Substring(0, 4) + "," + price.Substring(4, 2);
                        affected_nozzle.displayPrice = Decimal.Parse(price);
                        litres = response.Substring(108, 8).Replace("-", "");
                        litres = litres.Substring(0, 4) + "," + litres.Substring(4, 2);
                        affected_nozzle.displayLitres = Decimal.Parse(litres);
                        affected_nozzle.checkLCD = true;
                        break;
                    
                    case ((int)ResponseLengthEnum.Totals):
                        affected_nozzle.Status = FuelPointStatusEnum.Idle;
                        affected_nozzle.LastResponseType = ResponseTypeEnum.Totals;
                        totalLitres = response.Substring(42, 20).Replace("-", "");//001110
                        totalLitres = totalLitres.Substring(0, 13) + "," + totalLitres.Substring(13, 2);
                        affected_nozzle.TotalLitres = Decimal.Parse(totalLitres);
                        break;



                }


                nozzles.Add(affected_nozzle);
                
            }
            return nozzles;
        }
    }



    public class Nozzle
    {   //Addressing
        public int fuelPointChannel { get; set; }
        public int dispenserID { get; set; }

        //Bool
        public bool checkLCD { get; set; } //if(1) get displayPrice and displayLitres else ignore;
        public bool idle { get; set; }
        public bool open { get; set; }
        public bool closed { get; set; }
        public bool error { get; set; }
        public bool nozzle { get; set; }
        public bool working { get; set; }
        public bool authorized { get; set; }

        //Data
        public ResponseTypeEnum LastResponseType { get; set; }
        public FuelPointStatusEnum Status { get; set; }
        public decimal displayPrice { get; set; }
        public decimal displayLitres { get; set; }
        public decimal TotalLitres { get; set; }

        //extra
        public int lastSaleId { get; set; }
        public decimal lastSalePrice { get; set; }

    }
}

