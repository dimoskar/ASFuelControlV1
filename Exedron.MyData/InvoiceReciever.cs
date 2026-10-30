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
        private static readonly HttpClient Client = new HttpClient();

        //https://mydata-dev.azure-api.net/RequestDocs?[mark]&[nextPartitionKey]&[nextRowKey]

        public static async Task<Models.Responses.RequestedDoc> RequestRecievedDocs(string url, long mark)
        {
            if (!Exedron.MyData.Settings.IsActive)
                return new Models.Responses.RequestedDoc();

            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Request parameters

            queryString["mark"] = mark.ToString();
            var uri = url + "/RequestDocs?" + queryString;

            using (var request = new HttpRequestMessage(HttpMethod.Get, uri))
            {
                request.Headers.Add("aade-user-id", Settings.Username);
                request.Headers.Add("Ocp-Apim-Subscription-Key", Settings.SubscriptionKey);
                using (HttpResponseMessage response = await Client.SendAsync(request))
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    return Models.Responses.RequestedDoc.Deserialize(responseString);
                }
            }
        }

        public static async Task<Models.Responses.RequestedDoc> RecieveSubmittedDocs(string url, long mark, string issuer)
        {
            if (!Exedron.MyData.Settings.IsActive)
                return new Models.Responses.RequestedDoc();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Request parameters

            //queryString["issuervat"] = issuer;
            queryString["mark"] = mark.ToString();
            var uri = url + "/RequestTransmittedDocs?" + queryString;

            using (var request = new HttpRequestMessage(HttpMethod.Get, uri))
            {
                request.Headers.Add("aade-user-id", Settings.Username);
                request.Headers.Add("Ocp-Apim-Subscription-Key", Settings.SubscriptionKey);
                using (HttpResponseMessage response = await Client.SendAsync(request))
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    return Models.Responses.RequestedDoc.Deserialize(responseString);
                }
            }
        }
    }
}
