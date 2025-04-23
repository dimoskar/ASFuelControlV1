using System.IO;
using Newtonsoft.Json;

namespace Samtec.WebService
{
	public class SamtecSettings
	{
		public string SamtecUrl { get; set; }

		public string SamtecURLPort { get; set; }

		public string EftPosTID { get; set; }

		public static SamtecSettings ReadSettings()
		{
			if (!File.Exists("SamtecSettings.json"))
			{
				return null;
			}
			string str = File.ReadAllText("SamtecSettings.json");
			return JsonConvert.DeserializeObject<SamtecSettings>(str);
		}
	}
}
