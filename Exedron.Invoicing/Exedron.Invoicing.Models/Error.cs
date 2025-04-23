namespace Exedron.Invoicing.Models
{
	public class Error
	{
		public string code { get; set; }

		public string defaultMessage { get; set; }

		public bool fatal { get; set; }
	}
}
