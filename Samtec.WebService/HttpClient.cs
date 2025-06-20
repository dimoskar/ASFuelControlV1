using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Samtec.WebService
{
    public class HttpClient
    {
        public static Models.Response CallWS(string jsonRequest, string url)
        {
            var jsonSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore,
            };
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.Headers.Add("charset", "UTF-8");
            httpWebRequest.ContentType = "text/json";
            httpWebRequest.Accept = "application/json, text/javascript, */*";
            httpWebRequest.Method = "POST";
            httpWebRequest.Timeout = 130000; //130sec είναι το default Timeout που έχει οριστεί για την αναμονή απάντησης από τα EFTPOS

            //Send JSON Bytes
            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                streamWriter.Write(jsonRequest);
            }

            //Έναρξη λήψης απάντησης
            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                var result = streamReader.ReadToEnd();

                //Analize Reply
                var resp = JsonConvert.DeserializeObject<Models.Response>(result, jsonSettings);
                return resp;
            }
        }
        public static Models.Response CallWS(Models.Request request, string url)
        {
            var reqStr = Newtonsoft.Json.JsonConvert.SerializeObject(request);
            return CallWS(reqStr, url);
        }
    }
}
