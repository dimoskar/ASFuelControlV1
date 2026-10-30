using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Exedron.MyData
{
    public class RequestTransmitted
    {
        private static readonly HttpClient Client = CreateClient();

        private static HttpClient CreateClient()
        {
            return new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        }

        public static Models.Transmitted.RequestedDoc MakeRequest(string url, string mark, string nextPartitionKey, string nextRowKey)
        {
            try { 
            if (!Exedron.MyData.Settings.IsActive)
                return new Models.Transmitted.RequestedDoc()
                {
                    invoicesDoc = new Models.Transmitted.RequestedDocInvoice[] { }
                };
            var uri = url.EndsWith("/") ? url + "RequestTransmittedDocs?mark=" + mark : url + "/RequestTransmittedDocs?mark=" + mark;
            if(!string.IsNullOrEmpty(nextPartitionKey))
                uri = uri + "&nextPartitionKey" + nextPartitionKey;
            if (!string.IsNullOrEmpty(nextRowKey))
                uri = uri + "&nextRowKey" + nextRowKey;

            using (var request = new HttpRequestMessage(HttpMethod.Get, uri))
            {
                request.Headers.Add("aade-user-id", Settings.Username);
                request.Headers.Add("Ocp-Apim-Subscription-Key", Settings.SubscriptionKey);
                using (HttpResponseMessage response = Client.SendAsync(request).Result)
                {
                    var responseString = response.Content.ReadAsStringAsync().Result;
                    var pos = responseString.IndexOf('<');
                    if (pos >= 0)
                        responseString = responseString.Substring(pos);
                    try
                    {
                        var respDoc = Models.Transmitted.RequestedDoc.Deserialize(responseString);
                        if (respDoc.invoicesDoc == null || respDoc.invoicesDoc.Length == 0)
                        {

                        }
                        return respDoc;
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            var error = Models.Transmitted.ErrorResponse.Deserialize(responseString);
                            var reqDoc = new Models.Transmitted.RequestedDoc();
                            reqDoc.ErrorCode = error.statusCode.ToString();
                            reqDoc.ErrorMessage = error.message;
                            reqDoc.invoicesDoc = new Models.Transmitted.RequestedDocInvoice[] { };
                            return reqDoc;
                        }
                        catch
                        {
                            return new Models.Transmitted.RequestedDoc()
                            {
                                invoicesDoc = new Models.Transmitted.RequestedDocInvoice[] { }
                            };
                        }

                    }
                }
            }
            }

            catch (Exception ex)
            {
                return new Models.Transmitted.RequestedDoc()
                {
                    ErrorCode = "1000"
                };
            }

            return new Models.Transmitted.RequestedDoc();
        }
    }
}
