using System.IO;
using System.Net;
using Newtonsoft.Json;
using Samtec.WebService.Models;

namespace Samtec.WebService
{
	public class HttpClient
	{
		public static Response CallWS(string jsonRequest, string url)
		{
			JsonSerializerSettings jsonSettings = new JsonSerializerSettings
			{
				Formatting = Formatting.Indented,
				ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
				NullValueHandling = NullValueHandling.Ignore,
				MissingMemberHandling = MissingMemberHandling.Ignore
			};
			HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
			httpWebRequest.Headers.Add("charset", "UTF-8");
			httpWebRequest.ContentType = "text/json";
			httpWebRequest.Accept = "application/json, text/javascript, */*";
			httpWebRequest.Method = "POST";
			httpWebRequest.Timeout = 130000;
			using (StreamWriter streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
			{
				streamWriter.Write(jsonRequest);
			}
			HttpWebResponse httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
			using (StreamReader streamReader = new StreamReader(httpResponse.GetResponseStream()))
			{
				string result = streamReader.ReadToEnd();
				return JsonConvert.DeserializeObject<Response>(result, jsonSettings);
			}
		}

		public static Response CallWS(Request request, string url)
		{
			string reqStr = JsonConvert.SerializeObject(request);
			return CallWS(reqStr, url);
		}
	}
}
