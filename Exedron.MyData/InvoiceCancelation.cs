using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Exedron.MyData
{
    public class InvoiceCancelation
    {
        private static readonly HttpClient Client = CreateClient();

        private static HttpClient CreateClient()
        {
            return new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        }

        public static Models.Responses.ResponseDoc MakeRequest(string url, long mark)
        {
            if (!Exedron.MyData.Settings.IsActive)
                return new Models.Responses.ResponseDoc();
            var queryString = HttpUtility.ParseQueryString(string.Empty);
            queryString["mark"] = mark.ToString();
            var uri = url + "/CancelInvoice?" + queryString;

            using (var request = new HttpRequestMessage(HttpMethod.Post, uri))
            {
                request.Headers.Add("aade-user-id", Settings.Username);
                request.Headers.Add("Ocp-Apim-Subscription-Key", Settings.SubscriptionKey);
                request.Content = new ByteArrayContent(new byte[] { });
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/xml");
                using (HttpResponseMessage response = Client.SendAsync(request).Result)
                {
                    var responseString = response.Content.ReadAsStringAsync().Result;
                    var pos = responseString.IndexOf('<');
                    if (pos >= 0)
                        responseString = responseString.Substring(pos);
                    try
                    {
                        var respDoc = Models.Responses.ResponseDoc.Deserialize(responseString);
                        respDoc.response.responseData = responseString;
                        return respDoc;
                    }
                    catch (Exception ex)
                    {
                        var respDoc = new Models.Responses.ResponseDoc();
                        respDoc.response = new Models.Responses.ResponseDocResponse();
                        respDoc.response.errors = new Models.Responses.ResponseDocResponseError[]
                        {
                            new Models.Responses.ResponseDocResponseError(){ code = 0, message = ex.Message }
                        };
                        respDoc.response.responseData = responseString;
                        return respDoc;
                    }
                    
                }
            }
            return new Models.Responses.ResponseDoc();
        }

        public static Models.Responses.ResponseDoc MakeRequest(string url, long mark, string userName, string password, bool enabled = false)
        {
            if (!enabled && !Exedron.MyData.Settings.IsActive)
                return new Models.Responses.ResponseDoc()
                {
                    response = new Models.Responses.ResponseDocResponse()
                    {
                        statusCode = "0",
                        errors = new Models.Responses.ResponseDocResponseError[]
                        {
                            new Models.Responses.ResponseDocResponseError(){ code = 0, message = "My Data is not activated on this installation." }
                        }
                    }
                };

            var queryString = HttpUtility.ParseQueryString(string.Empty);
            queryString["mark"] = mark.ToString();
            var uri = url + "/CancelInvoice?" + queryString;

            using (var request = new HttpRequestMessage(HttpMethod.Post, uri))
            {
                request.Headers.Add("aade-user-id", userName);
                request.Headers.Add("Ocp-Apim-Subscription-Key", password);
                request.Content = new ByteArrayContent(new byte[] { });
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/xml");
                using (HttpResponseMessage response = Client.SendAsync(request).Result)
                {
                    var responseString = response.Content.ReadAsStringAsync().Result;
                    var pos = responseString.IndexOf('<');
                    if (pos >= 0)
                        responseString = responseString.Substring(pos);
                    try
                    {
                        var respDoc = Models.Responses.ResponseDoc.Deserialize(responseString);
                        respDoc.response.responseData = responseString;
                        return respDoc;
                    }
                    catch (Exception ex)
                    {
                        var respDoc = new Models.Responses.ResponseDoc();
                        respDoc.response = new Models.Responses.ResponseDocResponse();
                        respDoc.response.errors = new Models.Responses.ResponseDocResponseError[]
                        {
                            new Models.Responses.ResponseDocResponseError(){ code = 0, message = ex.Message }
                        };
                        respDoc.response.responseData = responseString;
                        return respDoc;
                    }

                }
            }
            return new Models.Responses.ResponseDoc();
        }
    }
}
