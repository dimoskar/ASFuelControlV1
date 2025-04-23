using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Exedron.MyData
{
    public class InvoiceReciever
    {
        //https://mydata-dev.azure-api.net/RequestDocs?[mark]&[nextPartitionKey]&[nextRowKey]

        public static async Task<Models.Responses.RequestedDoc> RequestRecievedDocs(string url, long mark)
        {
            if (!Exedron.MyData.Settings.IsActive)
                return new Models.Responses.RequestedDoc();

            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Request headers
            client.DefaultRequestHeaders.Add("aade-user-id", Settings.Username);
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", Settings.SubscriptionKey);

            // Request parameters

            queryString["mark"] = mark.ToString();
            var uri = url + "/RequestDocs?" + queryString;

            var response = await client.GetAsync(uri);
            if (response != null)
            {
                var responseString = response.Content.ReadAsStringAsync().Result;
                return Models.Responses.RequestedDoc.Deserialize(responseString);
            }
            return null;
        }

        public static async Task<Models.Responses.RequestedDoc> RecieveSubmittedDocs(string url, long mark, string issuer)
        {
            if (!Exedron.MyData.Settings.IsActive)
            {
                new Models.Responses.RequestedDoc();
            }
            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Request headers
            client.DefaultRequestHeaders.Add("aade-user-id", Settings.Username);
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", Settings.SubscriptionKey);

            // Request parameters

            //queryString["issuervat"] = issuer;
            queryString["mark"] = mark.ToString();
            var uri = url + "/RequestTransmittedDocs?" + queryString;

            var response = await client.GetAsync(uri);
            if (response != null)
            {
                var responseString = response.Content.ReadAsStringAsync().Result;
                return Models.Responses.RequestedDoc.Deserialize(responseString);
            }
            return null;
        }
    }
}
