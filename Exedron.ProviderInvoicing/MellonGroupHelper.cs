using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exedron.ProviderInvoicing
{
    public class MellonGroupHelper
    {
        public static string ApiKeyGenerator(string otp, string arbitransName, string arbitransKey, int nsp, bool isTest)
        {
            var mel = new ArbitransMyData.POS.MellonGroupPOS(arbitransName, arbitransKey, nsp, isTest);
            var myApi = mel.getApiKey(otp);
            if (!myApi.ContainsKey("Id"))
            {
                for (int i = 0, loopTo = myApi.Count - 1; i <= loopTo; i++)
                {
                    if (myApi.Keys.Any(a => a == "errorIn"))
                    {
                        string error = string.Join("\r\n", myApi.Select(a => a.Key + ": " + a.Value));
                        ASFuelControl.Common.Logger.Instance.Error("MellonGroupHelper.ApiKeyGenerator ERROR :: " + error);
                    }
                }
            }
            var apiKey = myApi["Id"]; // => Αποθηκευση
            return apiKey;
        }
        public static int GetMellonNsp(string posType)
        {
            var posTypeParams = posType.Split('.');
            if (posTypeParams.Length == 2)
            {
                if (posTypeParams[0] == "Mellon")
                {
                    switch (posTypeParams[1])
                    {
                        case "JCC":
                        case "AtticaBank":
                        case "Pancreta":
                            return 1;
                        case "Nexi":
                            return 2;
                        case "NBG":
                            return 3;
                        case "Worldline":
                            return 4;
                        default:
                            return 1;

                    }
                }
                return 1;
            }
            return 0;
        }
    }
}
