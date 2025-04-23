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
        public static Models.Transmitted.RequestedDoc MakeRequest(string url, string mark, string nextPartitionKey, string nextRowKey)
        {
            try { 
            if (!Exedron.MyData.Settings.IsActive)
                return new Models.Transmitted.RequestedDoc()
                {
                    invoicesDoc = new Models.Transmitted.RequestedDocInvoice[] { }
                };
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(30);

            // Request headers
            client.DefaultRequestHeaders.Add("aade-user-id", Settings.Username);
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", Settings.SubscriptionKey);

            var uri = url.EndsWith("/") ? url + "RequestTransmittedDocs?mark=" + mark : url + "/RequestTransmittedDocs?mark=" + mark;
            if(!string.IsNullOrEmpty(nextPartitionKey))
                uri = uri + "&nextPartitionKey" + nextPartitionKey;
            if (!string.IsNullOrEmpty(nextRowKey))
                uri = uri + "&nextRowKey" + nextRowKey;

            HttpResponseMessage response = client.GetAsync(uri).Result;
                if (response != null)
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
