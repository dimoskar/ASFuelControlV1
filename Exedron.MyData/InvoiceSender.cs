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
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(30);

            // Request headers
            client.DefaultRequestHeaders.Add("aade-user-id", Settings.Username);
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", Settings.SubscriptionKey);

            var uri = url + "/SendInvoices";

            HttpResponseMessage response;

            // Request body
            byte[] byteData = Encoding.UTF8.GetBytes(xmlData);

            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/xml");
                response = client.PostAsync(uri, content).Result;
                if (response != null)
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
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(30);

            // Request headers
            client.DefaultRequestHeaders.Add("aade-user-id", userName);
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", password);

            var uri = url + "/SendInvoices";

            HttpResponseMessage response;

            // Request body
            byte[] byteData = Encoding.UTF8.GetBytes(xmlData);

            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/xml");
                response = client.PostAsync(uri, content).Result;
                if (response != null)
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
