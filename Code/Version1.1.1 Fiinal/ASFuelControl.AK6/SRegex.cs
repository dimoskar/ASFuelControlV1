using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ASFuelControl.AK6
{

    public static class SRegex
    {
        public static event EventHandler<Common.FuelPointValuesArgs> DispenserStatusChanged;
        public static void EvaluateBuffer(byte[] buffer, ASFuelControl.Common.FuelPoint fp)
        {
            ASFuelControl.Common.Enumerators.FuelPointStatusEnum curStatus = fp.Status;
            //Todo Calcuate CRC
            string bufferString = BitConverter.ToString(buffer.ToArray(), 0);
            MatchCollection possibleResponses = Regex.Matches(bufferString, "(10-09-02\\S{39})|(10-09-01\\S{39})|(10-09-03\\S{39})|(10-09-04\\S{39})|(22-0A\\S{96})");
            List<string> responses = new List<string>();
            List<string> totals = new List<string>();
            for(int i = 0; i < possibleResponses.Count; i++)
            {
                string currentResponse = possibleResponses[i].Value;
                if(currentResponse.Length == 47) responses.Add(currentResponse.Substring(0, 47).Replace("-", ""));
                else totals.Add(currentResponse.Substring(0, 101).Replace("-", ""));
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
                Console.WriteLine("Fp "+fp.Channel +" Totals : " + decimal.Parse(t.Substring(6, 12)) / (decimal)Math.Pow(10, 2));

            }

            //Console.WriteLine("Ad. " + fp.Address + "Ch. " + fp.Channel + "status. " + fp.Status);
            //if(fp.Status == ASFuelControl.Common.Enumerators.FuelPointStatusEnum.Nozzle)
            //{
                
            //    fp.QuerySetPrice = true;
            //    fp.QueryAuthorize = true;
            //}

         

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
                //Console.WriteLine("******** " + flag + " ********");
                //fp.QueryAuthorize = true;
                return ASFuelControl.Common.Enumerators.FuelPointStatusEnum.Nozzle;
            }
            else if(flag == 6)
            {

                Common.FuelPointValues values = new Common.FuelPointValues();
                fp.Status = Common.Enumerators.FuelPointStatusEnum.Nozzle;
                fp.DispenserStatus = fp.Status;
                fp.ActiveNozzleIndex = 0;
                values.Status = fp.Status;
                values.ActiveNozzle = 0;
                DispenserStatusChanged(null, new Common.FuelPointValuesArgs()
                {
                    CurrentFuelPoint = fp,
                    CurrentNozzleId = 1,
                    Values = values
                });
                System.Threading.Thread.Sleep(200);

                fp.QueryAuthorize = false;
                return ASFuelControl.Common.Enumerators.FuelPointStatusEnum.Work;
            }
            else if(flag == 8)
            {
                return ASFuelControl.Common.Enumerators.FuelPointStatusEnum.Work;
               
            }
            else
            {
                return ASFuelControl.Common.Enumerators.FuelPointStatusEnum.Idle;
            }
            
               
        }
        private static void EvalDisplay(Common.FuelPoint fp, string response)
        {
            //100901080100000003740005001339EB
            fp.DispensedVolume = decimal.Parse(response.Substring(10,10) )/ (decimal)Math.Pow(10, fp.DecimalPlaces);
            fp.DispensedAmount = decimal.Parse(response.Substring(20,6)) / (decimal)Math.Pow(10, fp.DecimalPlaces);
        }

    }
}
