using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ASFuelControl.Millenium.Enums;
using System.Globalization;
using ASFuelControl.Common.Enumerators;

namespace ASFuelControl.Millenium
{
    public class DispenserCommands
    {
        private string sprice;
        //private int price=9999;
        public DispenserCommands() { }

        private int ith(int num) 
        { 
            return 16 * (num / 10) + (num % 10); 
        }// hex = integerToHex(int)

        public byte[] CreateCmd(int dispenserId, CmdEnum command = CmdEnum.FetchBuffer)
        {
            byte[] Buffer = new byte[] { 0x00 };
            if (command == CmdEnum.FetchBuffer)
            {
                Buffer = new byte[] { 0xFF, (byte)(dispenserId) };
                return Buffer;
            }
            else
            {
                throw new Exception("CreateCmd(int dispenserId, CommandTypeEnum command) only for FetchBuffer");
            }
        }
        public byte[] CreateCmd(int dispenserId, int fuelPointChannel, CmdEnum command)
        {   // inputs: dispenser id, fuelpoint channel, type of command.
            // return the desired command for that id and channel.
            // Setup of a dispenser with 2 fuelpoints. 
            // pump1: address1 = dispenser's address, channel = 1.
            // pump2: address2 = dispenser's address, channel = 2.

            byte[] Buffer = new byte[] { 0x00 };
            switch (command)
            {
                case CmdEnum.RequestStatus:
                    //FD 01 00 02 01 00 00 00 04 01 22 14 15 FC 30 30 35 34 FB
                    Buffer = new byte[] { 0xFD, (byte)(dispenserId), 0x00, 0x02, 0x01, 0x00, 0x00, 0x00, 0x04, 0x01, (byte)(fuelPointChannel + 32), 0x14, 0x15, 0xFC };
                    break;
                case CmdEnum.FetchBuffer:
                    Buffer = new byte[] { 0xFF, (byte)(dispenserId) };
                    return Buffer;
                case CmdEnum.SendPrices:
                    throw new Exception("wrong format, use createCmd(int dispenserId, int fuelPointChannel, CommandTypeEnum command, int price) to setPrices");
                case CmdEnum.Authorize:
                    Buffer = new byte[] { 0xFD, (byte)(dispenserId), 0x00, 0x02, 0x01, 0x00, 0x40, 0x00, 0x07, 0x01, (byte)(fuelPointChannel + 32), 0x19, 0x01, 0x01, 0x3E, 0x00, 0xFC };
                    break;
                case CmdEnum.CloseNozzle:
                    Buffer = new byte[] { 0xFD, (byte)(dispenserId), 0x00, 0x02, 0x01, 0x00, 0x40, 0x00, 0x04, 0x01, (byte)(fuelPointChannel + 32), 0x3D, 0x00, 0xFC };
                    break;
                case CmdEnum.OpenNozzle:
                    Buffer = new byte[] { 0xFD, (byte)(dispenserId), 0x00, 0x02, 0x01, 0x00, 0x40, 0x00, 0x04, 0x01, (byte)(fuelPointChannel + 32), 0x3C, 0x00, 0xFC };
                    break;
                case CmdEnum.RequestTotals:
                    Buffer = new byte[] { 0xFD, (byte)(dispenserId), 0x00, 0x02, 0x01, 0x00, 0x00, 0x00, 0x05, 0x02, (byte)(fuelPointChannel + 32), 0x11, 0x14, 0x15, 0xFC };
                    break;
                case CmdEnum.RequestDisplayData:
                    Buffer = new byte[] { 0xFD, (byte)(dispenserId), 0x00, 0x02, 0x01, 0x00, 0x00, 0x00, 0x07, 0x01, (byte)(fuelPointChannel + 32), 0x14, 0x15, 0x22, 0x23, 0x1D, 0xFC };
                    break;
                case CmdEnum.GetLastSalesId:
                    Buffer = new byte[] { 0xFD, (byte)(dispenserId), 0x00, 0x02, 0x01, 0x00, 0x00, 0x00, 0x06, 0x02, (byte)(fuelPointChannel + 32), 0x21, 0x14, 0x15, 0x16, 0xFC };
                    break;
            }
            return ReturnCmdWithCrc(Buffer);
        }

        public byte[] CreateCmd(int dispenserId, int fuelPointChannel, CmdEnum command, int price)
        {
            byte[] buffer = new byte[] { 0xFD, 0x00, 0xFC };
            if (command == CmdEnum.SendPrices)
            {
              
                sprice = price.ToString();
                for(int i = sprice.Length; i < 4; i++)
                {
                    sprice = "0" + sprice;
                }
                int p1 = Convert.ToInt32((sprice.Substring(0, 2)));
                int p2 = Convert.ToInt32((sprice.Substring(2, 2)));
                buffer = new byte[] { 0xFD, (byte)(dispenserId), 0x00, 0x02, 0x01, 0x00, 0x40, 0x00, 0x0D, 0x06, 0x61, 0x00, 0x00, 0x00, (byte)(fuelPointChannel), 0x11, 0x02, 0x04, 0x03, 0x00, (byte)ith(p1), (byte)ith(p2), 0xFC };
                return ReturnCmdWithCrc(buffer);
            }
            if (command == CmdEnum.InforForId)
            {   //FD 01 00 02 01 00 02 00 0C 04 [21] 21 [01 00] 01 05 06 07 08 0C 15 FC 30 30 39 35 FB 
                sprice = price.ToString();
                for (int i = sprice.Length; i < 4; i++ )
                {
                    sprice = "0" + sprice;
                }
                
                int p1 = Convert.ToInt32((sprice.Substring(0, 2)));
                int p2 = Convert.ToInt32((sprice.Substring(2, 2)));
                /////////////////////FD 01 00                           02    01    00    40    00    07    04         21                    21      01              49         1E    00           FC 30 30 46 39 FB FF 01 
                buffer = new byte[] { 0xFD, (byte)(dispenserId), 0x00, 0x02, 0x01, 0x00, 0x40, 0x00, 0x07, 0x04, (byte)(fuelPointChannel + 32), 0x21, (byte)ith(p1), (byte)ith(p2), 0x1E, 0x00, 0xFC };
                return ReturnCmdWithCrc(buffer);
            }
            else
            {
                throw new System.ArgumentException("you cannot useoverloeaded creatCMD() for CommandTypeEnum{0} ", "command");
            }
        } //overloaded

        private byte[] ReturnCmdWithCrc(byte[] buffer)
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

        private string CalculateCrc(string buffer)
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

        public List<MilleniumNozzle> EvaluateBuffer(byte[] buffer, List<MilleniumProtocol.OldFpStatus> oldStatuses)
        {
            string price = "";
            string litres = "";
            string totalLitres = "";
            string rawResponseWithoutCrc = "";
            string originalCrc = "";
            string calculatedCrc = "";
            string currentResponse = "";
            string bufferString = BitConverter.ToString(buffer.ToArray(), 0);
            //string bufferString = String.Concat(Array.ConvertAll(buffer, x => x.ToString("X2"))); 
            MatchCollection possibleResponses = Regex.Matches(bufferString, @"(?<=FD+\-)(.*?)(?=\-+FB)"); //parse all commands 0xFD---,0xFB,0xFB 

            // Copy groups to a string array
            List<string> responses = new List<string>();

            //string[] commands = new string[possibleCmd.Count];

            for (int i = 0; i < possibleResponses.Count; i++)
            {
                currentResponse = possibleResponses[i].Groups[1].Value;
                originalCrc = currentResponse.Substring(currentResponse.Length - 11);//keep the last 4 bytes [crc]
                //Console.WriteLine(currentResponse.Substring(0, currentResponse.Length - 15));
                rawResponseWithoutCrc = currentResponse.Substring(0, currentResponse.Length - 15);
                calculatedCrc = CalculateCrc(rawResponseWithoutCrc); //discard oroginal crc, evaluate crc from scratch

                if (string.Compare(calculatedCrc, originalCrc, true) == 0) // if original crc == calculated 
                {
                    responses.Add(rawResponseWithoutCrc); //include that command in valid commands list
                    //       currentRespone: 02-01-01-00-00-20-00-08-01-22-14-01-06-15-01-01-FC-30-30-38-31
                    //rawResponseWithoutCrc: 02-01-01-00-00-20-00-08-01-22-14-01-06-15-01-01
                    //          originalCrc: 30-30-38-31
                    //        calculatedCrc: 30-30-38-31
                }
            }
            //nozzle status 
            //xx-01-01-00-00-20-00-08-01-xx-14-01-06-15-01-00
            List<string> LatestResponses = new List<string>(responses);
            List<MilleniumNozzle> nozzles = new List<MilleniumNozzle>();


            ////
            //// read previous response in responses. if current Nozzle was in Responses => discard(previous response)
            ////
            //foreach (string response in responses) //anlyze these commands
            //{   

            //    int currentIndex = responses.IndexOf(response);
            //    for (int i = 0; i < currentIndex; i++)
            //    {
            //        if (responses[i].Substring(0, 2) == responses[currentIndex].Substring(0, 2) &&
            //            responses[i].Substring(27, 2) == responses[currentIndex].Substring(27,2)) 
            //        {

            //            LatestResponses.RemoveAt(i);

            //        }

            //    }
            //}


            foreach (string response in LatestResponses) //anlyze these commands
            {   //todo 
                int DispenserID = int.Parse(response.Substring(6, 2), NumberStyles.AllowHexSpecifier);
                int FuelPointChannel = Convert.ToInt32(response.Substring(27, 2)) - 20;

                if (response.Length == (int)ResponseLengthEnum.SendPriceConfirm) //h monh diaforetikh domh
                { FuelPointChannel = Convert.ToInt32(response.Substring(39, 2)); }

                MilleniumNozzle affected_nozzle = new MilleniumNozzle(); //brand new
                
                affected_nozzle.DispenserID = DispenserID;
                affected_nozzle.FuelPointChannel = FuelPointChannel;
                //FuelPointStatusEnum fuelPointStatusEnum = oldStatuses.Find((MilleniumProtocol.OldFPStatus x) => (x.FPAddress != num ? false : x.FPchannel == num1)).status;
                try
                {

                    affected_nozzle.Status = oldStatuses.Where(x => (x.FPAddress == affected_nozzle.DispenserID && x.FPchannel == affected_nozzle.FuelPointChannel)).First().status;
                }
                catch
                {
                }
						
                //check current affect_nozzle if allready exists.
                for (int j = 0; j < nozzles.Count; j++)
                {
                    if (nozzles[j].DispenserID == DispenserID &&
                        nozzles[j].FuelPointChannel == FuelPointChannel)
                    {
                        affected_nozzle = nozzles[j]; //recover 
                        nozzles.RemoveAt(j);        //and delete
                    }
                }
                //affected_nozzle.DispenserID = int.Parse(response.Substring(6, 2), NumberStyles.AllowHexSpecifier);
                //affected_nozzle.FuelPointChannel = Convert.ToInt32(response.Substring(27, 2)) - 20;




                switch (response.Length)
                {
                    case ((int)ResponseLengthEnum.State):
                        affected_nozzle.LastResponseType = ResponseTypeEnum.nzStateChanged;
                        string state = response.Substring(34, 1);
                        if (state == "D")
                        {
                            affected_nozzle.isClosed = true;
                            affected_nozzle.Status = FuelPointStatusEnum.Close;
                        }
                        else //if (state == "C")
                        {
                            affected_nozzle.isOpen = true;
                            affected_nozzle.Status = FuelPointStatusEnum.Idle;
                        }
                        //else continue;
                        break;

                    case ((int)ResponseLengthEnum.AuthConfirm):
                        affected_nozzle.isStatusChanged = true;
                        affected_nozzle.isAuthorized = true;
                        affected_nozzle.Status = FuelPointStatusEnum.Auth;
                        affected_nozzle.LastResponseType = ResponseTypeEnum.nzSelfResponse;
                        break;

                    case ((int)ResponseLengthEnum.SendPriceConfirm):
                        //affected_nozzle.Status = FuelPointStatusEnum.InitializeSale;
                        affected_nozzle.FuelPointChannel = Convert.ToInt32(response.Substring(39, 2));
                        affected_nozzle.isPriceSet = true;
                        break;

                    //case ((int)ResponseLengthEnum.Status):
                    //    affected_nozzle.LastResponseType = ResponseTypeEnum.nzStatus;
                    //    int status = Convert.ToInt32(response.Substring(21, 2));
                    //    int ch1 = affected_nozzle.FuelPointChannel;
                    //    int add1 = affected_nozzle.DispenserID;
                    //    switch (status)
                    //    {
                    //        case (2):
                    //            affected_nozzle.isClosed = true;
                    //            affected_nozzle.isStatusChanged = true;
                    //            affected_nozzle.Status = FuelPointStatusEnum.Close;
                    //            break;
                    //        case (3):
                    //            affected_nozzle.isIdle = true;
                    //            affected_nozzle.isOpen = true;
                    //            affected_nozzle.isStatusChanged = true;
                    //            affected_nozzle.Status = FuelPointStatusEnum.Idle;
                    //            break;
                    //        case (4):
                    //            affected_nozzle.isNozzle = true;
                    //            affected_nozzle.isStatusChanged = true;
                    //            affected_nozzle.Status = FuelPointStatusEnum.Nozzle;
                    //            break;
                    //        case (6):
                    //            affected_nozzle.isAuthorized = true;
                    //            affected_nozzle.isStatusChanged = true;
                    //            affected_nozzle.Status = FuelPointStatusEnum.Auth;
                    //            break;
                    //        case (8):
                    //            //affected_nozzle.isWorking = true;
                    //            //affected_nozzle.isNozzle = true;
                    //            //affected_nozzle.isAuthorized = true;
                    //            //affected_nozzle.isStatusChanged = true;
                    //            //affected_nozzle.Status = FuelPointStatusEnum.Work;
                    //            // Console.WriteLine("++++++++++++++++  " + response);
                    //            break;
                    //        case (9):
                    //            affected_nozzle.isAuthorized = true;
                    //            affected_nozzle.isStatusChanged = true;
                    //            affected_nozzle.Status = FuelPointStatusEnum.TransactionStopped;
                    //            affected_nozzle.isError = true;
                    //            break;
                    //    }
                    //    //if (Convert.ToInt32(response.Substring(46, 1)) == 1)
                    //    //{
                    //    //    affected_nozzle.isError = true;
                    //    //    affected_nozzle.Status = FuelPointStatusEnum.Error;
                    //    //}
                    

                    case ((int)ResponseLengthEnum.DisplayWhileFueling):
                        affected_nozzle.LastResponseType = ResponseTypeEnum.DisplayWhileFueling;
                        this.GetStatus(affected_nozzle, response, 36);
                        this.GetDisplays(affected_nozzle, response,78,57);
                        //affected_nozzle.Status = Common.Enumerators.FuelPointStatusEnum.Work;
                        affected_nozzle.isOpen = true;
                        affected_nozzle.isNozzle = true;
                        break;

                    case ((int)ResponseLengthEnum.TransactionSummary):
                        affected_nozzle.isStatusChanged = true;
                        affected_nozzle.Status = FuelPointStatusEnum.TransactionCompleted;
                        affected_nozzle.LastResponseType = ResponseTypeEnum.TransactionSummary;
                        price = response.Substring(87, 8).Replace("-", "");//001110

                        //price = price.Substring(0, 4) + "," + price.Substring(4, 2);
                        affected_nozzle.DisplayPrice = Decimal.Parse(price);
                        litres = response.Substring(108, 8).Replace("-", "");
                        //litres = litres.Substring(0, 4) + "," + litres.Substring(4, 2);
                        affected_nozzle.DisplayLitres = Decimal.Parse(litres);
                        affected_nozzle.isCheckLCD = true;
                        break;

                    case ((int)ResponseLengthEnum.Totals):

                        //affected_nozzle.Status = prevStatuses[affected_nozzle.FuelPointChannel];
                        try
                        {

                            affected_nozzle.Status = oldStatuses.Where(x => (x.FPAddress == affected_nozzle.DispenserID && x.FPchannel == affected_nozzle.FuelPointChannel)).First().status;
                        }
                        catch
                        {
                        }
                        affected_nozzle.LastResponseType = ResponseTypeEnum.Totals;
                        totalLitres = response.Substring(42, 17).Replace("-", "");
                        //totalLitres = totalLitres.Substring(0, 13) + "," + totalLitres.Substring(13, 2);
                        affected_nozzle.TotalLitres = Decimal.Parse(totalLitres);
                        break;

                    case ((int)ResponseLengthEnum.SelfResponseStatus):
                        affected_nozzle.LastResponseType = ResponseTypeEnum.nzSelfResponse;
                        int SelfResponseStatus = Convert.ToInt32(response.Substring(42, 2));
                        switch (SelfResponseStatus)
                        {
                            case (2):
                                affected_nozzle.isClosed = true;
                                affected_nozzle.isStatusChanged = true;
                                affected_nozzle.Status = FuelPointStatusEnum.Close;
                                break;
                            case (3):
                                affected_nozzle.isIdle = true;
                                affected_nozzle.isStatusChanged = true;
                                affected_nozzle.Status = FuelPointStatusEnum.Idle;
                                break;
                            case (4):
                                affected_nozzle.isNozzle = true;
                                affected_nozzle.isStatusChanged = true;
                                affected_nozzle.Status = FuelPointStatusEnum.Nozzle;
                                break;
                            case (6):
                                affected_nozzle.isAuthorized = true;
                                affected_nozzle.isStatusChanged = true;
                                affected_nozzle.Status = FuelPointStatusEnum.Work;
                                affected_nozzle.isNozzle = true;
                                break;
                            case (8):

                                affected_nozzle.isAuthorized = true;
                                affected_nozzle.isStatusChanged = true;
                                affected_nozzle.Status = FuelPointStatusEnum.Work;
                                affected_nozzle.isWorking = true;
                                affected_nozzle.isNozzle = true;
                               
                                break;
                            case (9):
                                affected_nozzle.isStatusChanged = true;
                                affected_nozzle.Status = FuelPointStatusEnum.Error;
                                affected_nozzle.isFreezed = true;
                                affected_nozzle.isNozzle = true;
                                break;
                        }
                        break;

                    case ((int)ResponseLengthEnum.LastSalesInfo):

                        affected_nozzle.LastResponseType = ResponseTypeEnum.LastSalesSummary;
                        price = response.Substring(60, 8).Replace("-", "");//001110

                        //price = price.Substring(0, 4) + "," + price.Substring(4, 2);
                        affected_nozzle.DisplayPrice = Decimal.Parse(price);

                        litres = response.Substring(81, 8).Replace("-", "");
                        //litres = litres.Substring(0, 4) + "," + litres.Substring(4, 2);
                        affected_nozzle.DisplayLitres = Decimal.Parse(litres);


                        string EuroPerLitre = response.Substring(102, 6).Replace("-", "");
                        affected_nozzle.DisplayEuroPerLitre = Decimal.Parse(EuroPerLitre);

                        affected_nozzle.isCheckLCD = true;
                        break;

                    case ((int)ResponseLengthEnum.LastSalesID):
                        affected_nozzle.LastResponseType = ResponseTypeEnum.LastSalesId;
                        string LastSaleId = response.Substring(96, 17).Replace("-", "");
                        affected_nozzle.LastSaleID = int.Parse(LastSaleId);
                        break;

                    //case ((int)ResponseLengthEnum.ConfirmOC):




                }


                nozzles.Add(affected_nozzle);

            }
            return nozzles;
        }
        private void GetStatus(MilleniumNozzle affected_nozzle, string response, int digit)
        {
            int status = Convert.ToInt32(response.Substring(digit, 2));
            switch (status)
            {
                case (2):
                    affected_nozzle.isClosed = true;
                    affected_nozzle.isStatusChanged = true;
                    affected_nozzle.Status = FuelPointStatusEnum.Close;
                    break;
                case (3):
                    affected_nozzle.isIdle = true;
                    affected_nozzle.isOpen = true;
                    affected_nozzle.isStatusChanged = true;
                    affected_nozzle.Status = FuelPointStatusEnum.Idle;
                    break;
                case (4):
                    affected_nozzle.isNozzle = true;
                    affected_nozzle.isStatusChanged = true;
                    affected_nozzle.Status = FuelPointStatusEnum.Nozzle;
                    break;
                case (6):
                    affected_nozzle.isAuthorized = true;
                    affected_nozzle.isStatusChanged = true;
                    affected_nozzle.Status = FuelPointStatusEnum.Auth;
                    break;
                case (8):
                    affected_nozzle.isWorking = true;
                    affected_nozzle.isNozzle = true;
                    affected_nozzle.isAuthorized = true;
                    affected_nozzle.isStatusChanged = true;
                    affected_nozzle.Status = FuelPointStatusEnum.Work;
                    break;
                case (9):
                    affected_nozzle.isAuthorized = true;
                    affected_nozzle.isStatusChanged = true;
                    affected_nozzle.Status = FuelPointStatusEnum.TransactionStopped;
                    affected_nozzle.isError = true;
                    break;
            }
        }
        private void GetDisplays(MilleniumNozzle affected_nozzle, string response, int digitVol, int digitPrice) 
        {
            string price = response.Substring(digitPrice, 8).Replace("-", "");
           
            string litres = response.Substring(digitVol, 8).Replace("-", "");
            affected_nozzle.DisplayPrice = Decimal.Parse(price);
            affected_nozzle.DisplayLitres = Decimal.Parse(litres);
            //affected_nozzle.isCheckLCD = true;
 
        }
            
    }
    
    public class MilleniumNozzle
    {

        private decimal totalLitres;
        private decimal displayPrice;
        private decimal displayLitres;
        private decimal displayEuroPerLitre;

        //Addressing
        public int FuelPointChannel { get; set; }
        public int DispenserID { get; set; }
        public int LastSaleID { get; set; }


        public bool isStatusChanged { get; set; }
        public bool isCheckLCD { get; set; } //if(1) get displayPrice and displayLitres else ignore;
        //Bool
        public bool isPriceSet { get; set; }
        public bool isIdle { get; set; }
        public bool isOpen { get; set; }
        public bool isClosed { get; set; }
        public bool isError { get; set; }
        public bool isNozzle { get; set; }
        public bool isWorking { get; set; }
        public bool isAuthorized { get; set; }
        public bool isFreezed { get; set; }

        //Data
        public ResponseTypeEnum LastResponseType { get; set; }
        public FuelPointStatusEnum Status 
        { 
            get; 
            set; 
        }

        internal decimal DisplayPrice { 
            set { displayPrice = value; }
            
        }
        internal decimal DisplayLitres 
        { 
            set { displayLitres = value; }
        }
        internal decimal DisplayEuroPerLitre
        {
            set
            {
                displayEuroPerLitre = value;
            }
        }
        public decimal TotalLitres 
        { 
            set { totalLitres = value; }
            get { return totalLitres; }
        }

        public decimal getTotalLitres(int value) { return (totalLitres / (decimal)Math.Pow(10, value)); }
        public decimal getDisplayPrice(int value) { return (displayPrice / (decimal)Math.Pow(10, value)); }
        public decimal getDisplayLitres(int value) { return (displayLitres / (decimal)Math.Pow(10, value)); }
        public decimal getDisplayEuroPerLitre(int value) { return (displayEuroPerLitre / (decimal)Math.Pow(10, value)); }

        //extra


    }
}
