using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exedron.ProviderInvoicing
{
    public class MellonGroupHelper
    {
        public static string ApiKeyGenerator(Func<string> otp, string arbitransName, string arbitransKey, int nsp, bool isTest)
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
            var otpValue = otp.Invoke();
            if (string.IsNullOrEmpty(otpValue))
                return "";
            var mel = new ArbitransMyData.POS.MellonGroupPOS(arbitransName, arbitransKey, nsp, isTest);
            var myApi = mel.getApiKey(otpValue);
            if (myApi == null)
            {
                ASFuelControl.Common.Logger.Instance.Error("getApiKey returned null");
                return "";
            }
            else
            {
                ASFuelControl.Common.Logger.Instance.Debug("getApiKey => " + string.Join("\r\n", myApi.Select(a => string.Format("Key: {0}, Value: {1}", a.Key, a.Value))));
            }
            var apiKey = myApi["Id"]; // => Αποθηκευση
            return apiKey;
        }
    }
}
