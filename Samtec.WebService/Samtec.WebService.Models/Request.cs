using Newtonsoft.Json;

namespace Samtec.WebService.Models
{
	public class Request
	{
		public string JobType { get; set; }

		public int InputType { get; set; }

		public Doc Doc { get; set; }

		public string AtxtContent { get; set; }

		public string JobParams { get; set; }

		public override string ToString()
		{
			return JsonConvert.SerializeObject(this);
		}
	}
}
