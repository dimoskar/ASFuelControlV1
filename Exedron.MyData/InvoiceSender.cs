using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Net.Http;
using System.Web;
using System.Threading.Tasks;

namespace Exedron.MyData
{
    public class InvoiceSender
    {
        private static readonly HttpClient Client = CreateClient();

        private static HttpClient CreateClient()
        {
            return new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        }

        public static Models.Responses.ResponseDoc MakeRequest(string url, string xmlData)
        {
            var pos1 = xmlData.IndexOf('<');
            if (pos1 >= 0)
                xmlData = xmlData.Substring(pos1);

            if (!Exedron.MyData.Settings.IsActive)
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
            var uri = url + "/SendInvoices";
            byte[] byteData = Encoding.UTF8.GetBytes(xmlData);

            using (var request = new HttpRequestMessage(HttpMethod.Post, uri))
            {
                request.Headers.Add("aade-user-id", Settings.Username);
                request.Headers.Add("Ocp-Apim-Subscription-Key", Settings.SubscriptionKey);
                request.Content = new ByteArrayContent(byteData);
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
                    catch(Exception ex)
                    {
                        var respDoc = new Models.Responses.ResponseDoc();
                        respDoc.response = new Models.Responses.ResponseDocResponse();
                        if (responseString.Contains("Unexpected technical error"))
                        {
                            respDoc.response.errors = new Models.Responses.ResponseDocResponseError[]
                            {
                                new Models.Responses.ResponseDocResponseError(){ code = 0, message = ex.Message },
                                new Models.Responses.ResponseDocResponseError(){ code = 2, message = responseString }
                            };
                        }
                        else
                        {
                            respDoc.response.errors = new Models.Responses.ResponseDocResponseError[]
                            {
                                new Models.Responses.ResponseDocResponseError(){ code = 0, message = ex.Message },
                                new Models.Responses.ResponseDocResponseError(){ code = 1, message = responseString }
                            };
                        }
                        respDoc.response.responseData = responseString;
                        return respDoc;
                    }
                }
            }
            return new Models.Responses.ResponseDoc();
        }

        public static Models.Responses.ResponseDoc MakeRequest(string url, string userName, string password, string xmlData, bool enabled = false)
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
            var uri = url + "/SendInvoices";
            byte[] byteData = Encoding.UTF8.GetBytes(xmlData);

            using (var request = new HttpRequestMessage(HttpMethod.Post, uri))
            {
                request.Headers.Add("aade-user-id", userName);
                request.Headers.Add("Ocp-Apim-Subscription-Key", password);
                request.Content = new ByteArrayContent(byteData);
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/xml");
                using (HttpResponseMessage response = Client.SendAsync(request).Result)
                {
                    var responseString = response.Content.ReadAsStringAsync().Result;
                    try 
                    { 
                        var respDoc = Models.Responses.ResponseDoc.Deserialize(responseString);
                        respDoc.response.responseData = responseString;
                        return respDoc;
                    }
                    catch(Exception ex)
                    {
                        var respDoc = new Models.Responses.ResponseDoc();
                        respDoc.response = new Models.Responses.ResponseDocResponse();
                        if (responseString.Contains("Unexpected technical error"))
                        {
                            respDoc.response.errors = new Models.Responses.ResponseDocResponseError[]
                            {
                                new Models.Responses.ResponseDocResponseError(){ code = 0, message = ex.Message },
                                new Models.Responses.ResponseDocResponseError(){ code = 2, message = responseString }
                            };
                        }
                        else
                        {
                            respDoc.response.errors = new Models.Responses.ResponseDocResponseError[]
                            {
                                new Models.Responses.ResponseDocResponseError(){ code = 0, message = ex.Message },
                                new Models.Responses.ResponseDocResponseError(){ code = 1, message = responseString }
                            };
                        }
                        respDoc.response.responseData = responseString;
                        return respDoc;
                    }
                }
            }
            return new Models.Responses.ResponseDoc();
        }
    }
}
