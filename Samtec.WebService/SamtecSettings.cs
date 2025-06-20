using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Samtec.WebService
{
    public class SamtecSettings
    {
        public string SamtecUrl { get; set; }
        public string SamtecURLPort { get; set; }
        public string EftPosTID { set; get; }
        public static SamtecSettings ReadSettings()
        {
            if (!System.IO.File.Exists("SamtecSettings.json"))
                return null;
            var str = System.IO.File.ReadAllText("SamtecSettings.json");
            return Newtonsoft.Json.JsonConvert.DeserializeObject<SamtecSettings>(str);
        }
    }
}
