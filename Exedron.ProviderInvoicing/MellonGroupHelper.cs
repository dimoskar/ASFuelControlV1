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
            var apiKey = myApi["Id"]; // => Αποθηκευση
            return apiKey;
        }
    }
}
