using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Configuration;

namespace ASFuelControl.AK6
{

    public static class SRegex
    {
        public static void EvaluateBuffer(byte[] buffer, ASFuelControl.Common.FuelPoint fp)
        {
            ASFuelControl.Common.Enumerators.FuelPointStatusEnum curStatus = fp.Status;
            string bufferString = BitConverter.ToString(buffer.ToArray(), 0);
            MatchCollection possibleResponses = Regex.Matches(bufferString, "(10-09-04\\S{39})|(10-09-03\\S{39})|(10-09-02\\S{39})|(10-09-01\\S{39})|(22-0A\\S{96})");
            List<string> responses = new List<string>();
            List<string> totals = new List<string>();

            for(int i = 0; i < possibleResponses.Count; i++)
            {
                try
                {
                    if(possibleResponses[i].Value.Length == 47)
                    {
                        responses.Add(possibleResponses[i].Value.EvalCrc(fp));
                    }
                    else
                    {

                        totals.Add(possibleResponses[i].Value.EvalCrc(fp));
                    }
                }
                catch
                {
                    continue;
                }
            }

            //Analyze Responses (status)
            foreach(string r in responses)
            {
                int id = Convert.ToInt16(r.Substring(5, 1));
                int statusFlag = Convert.ToInt16(r.Substring(7, 1));
                
                if(id == fp.Channel)
                {
                    fp.Status = ReturnStatus(statusFlag,fp);
                    fp.DispenserStatus = fp.Status;//return status and set Flags
                    if(fp.Status == ASFuelControl.Common.Enumerators.FuelPointStatusEnum.Work)
                    {
                        EvalDisplay(fp, r);
                    }
                }
                //Console.WriteLine("FpStatus : " + fp.Status);
            }

            //Analyze Responses (totals)
            foreach(string t in totals)
            {
                int id = Convert.ToInt16(t.Substring(5, 1));
                if(id == fp.Channel)
                {
                    fp.Nozzles[0].TotalVolume = decimal.Parse(t.Substring(6, 12));
                    fp.Nozzles[0].QueryTotals = false;
                }
               // System.IO.File.AppendAllText(System.Environment.CurrentDirectory + "\\ak6.txt", string.Format(
               //"\nTime: {0}, Fp: {1}|{2}, Totals: {3} " , System.DateTime.Now,fp.Address,fp.Channel,decimal.Parse(t.Substring(6, 12)) / (decimal)Math.Pow(10, 2)));
               
               
            }


        }

        private static ASFuelControl.Common.Enumerators.FuelPointStatusEnum ReturnStatus(int flag, ASFuelControl.Common.FuelPoint fp)
        {
            if(flag == 2)
            {
                fp.SetExtendedProperty("AkInitialize", false);
               
                return ASFuelControl.Common.Enumerators.FuelPointStatusEnum.Idle;
            }
            else if(flag == 3)
            {

                fp.SetExtendedProperty("AkInitialize", true);
                return ASFuelControl.Common.Enumerators.FuelPointStatusEnum.Idle;
            }
            else if(flag == 4)
            {
                return ASFuelControl.Common.Enumerators.FuelPointStatusEnum.Nozzle;
            }
            else if(flag == 5)
            {
                //Egine Ready alla ekleise to nozzle xwris sale.
                //Einai Authorized alla idle;
                fp.QueryAuthorize = false; 
                return ASFuelControl.Common.Enumerators.FuelPointStatusEnum.Idle;
            }
            else if(flag == 6)
            {
                fp.QueryAuthorize = false;
                return Common.Enumerators.FuelPointStatusEnum.Ready;
            }
            else if(flag == 8)
            {
                fp.QueryAuthorize = false;
                return ASFuelControl.Common.Enumerators.FuelPointStatusEnum.Work;
            }
            else
            {
                return ASFuelControl.Common.Enumerators.FuelPointStatusEnum.Offline;
            }
            
               
        }
        private static void EvalDisplay(Common.FuelPoint fp, string response)
        {
            //10 09 01 08 01 00 00 00 03 74 00 05 00 13 39 EB
            //
            fp.DispensedVolume = decimal.Parse(response.Substring(10,10) )/ (decimal)Math.Pow(10, fp.AmountDecimalPlaces);
            fp.DispensedAmount = decimal.Parse(response.Substring(20,6)) / (decimal)Math.Pow(10, fp.AmountDecimalPlaces);
        }

        public static string EvalCrc(this string value,Common.FuelPoint fp)
        {
            int crc = 0;
            var parms = value.Split('-');
            for(int k = 0; k < parms.Length - 1; k++)
            {
                crc += Convert.ToInt32(Int32.Parse(parms[k], System.Globalization.NumberStyles.HexNumber));
            }

            {
                System.IO.Directory.CreateDirectory(System.Environment.CurrentDirectory + "\\logs" + "\\LogController");
                System.IO.File.AppendAllText(System.Environment.CurrentDirectory + "\\logs" + "\\LogController\\" + ".txt",
                  string.Format("\n{0} \t fp.Address:{3} \tfp.channel:{4} \tResponse:{1} \tCalculatedCrc:{2}", System.DateTime.Now, value, crc.ToString(), fp.Address, fp.Channel));
            }

            if((byte)crc == Byte.Parse(parms[parms.Length - 1], System.Globalization.NumberStyles.HexNumber))
                return value.Replace("-", "");
            else
            {
                throw new ArgumentException("value", "value");
            }
        }
    

    }
 
     
        

}
