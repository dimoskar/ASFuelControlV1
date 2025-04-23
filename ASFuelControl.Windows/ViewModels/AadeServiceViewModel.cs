using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace ASFuelControl.Windows.ViewModels
{
    public class AadeServiceViewModel
    {
        public string UserName
        {
            set
            {
                ASFuelControl.Data.Implementation.OptionHandler.Instance.SetOption("AadeServiceUserName", value);
            }
            get { return ASFuelControl.Data.Implementation.OptionHandler.Instance.GetOption("AadeServiceUserName", ""); }
        }
        public string Password
        {
            set
            {
                ASFuelControl.Data.Implementation.OptionHandler.Instance.SetOption("AadeServicePassword", value);
            }
            get { return ASFuelControl.Data.Implementation.OptionHandler.Instance.GetOption("AadeServicePassword", ""); }
        }

        public AadeTraderResponse GetTraderFromTaxNumber(string taxNumber)
        {
            AadeTraderResponse traderResponse = new AadeTraderResponse();
            try
            {
                var handler = new System.Net.Http.HttpClientHandler
                {
                    AllowAutoRedirect = false
                };

                ServicePointManager.SecurityProtocol =
                    SecurityProtocolType.Ssl3 |
                    SecurityProtocolType.Tls |
                    SecurityProtocolType.Tls11 |
                    SecurityProtocolType.Tls12;

                XNamespace ns = "http://schemas.xmlsoap.org/soap/envelope/";
                XNamespace ns2 = "http://rgwspublic2/RgWsPublic2Service";
                XNamespace ns3 = "http://rgwspublic2/RgWsPublic2";
                XNamespace env = "http://www.w3.org/2003/05/soap-envelope";
                XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";
                XNamespace xsd = "http://www.w3.org/2001/XMLSchema";
                XNamespace ns1 = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";

                XDocument soapRequest = new XDocument(
                    new XDeclaration("1.0", "UTF-8", "no"),
                    new XElement(env + "Envelope",
                        new XAttribute(XNamespace.Xmlns + "env", env),
                        new XAttribute(XNamespace.Xmlns + "ns1", ns1),
                        new XAttribute(XNamespace.Xmlns + "ns2", ns2),
                        new XAttribute(XNamespace.Xmlns + "ns3", ns3),
                        new XElement(env + "Header",
                            new XElement(ns1 + "Security",
                                new XElement(ns1 + "UsernameToken",
                                    new XElement(ns1 + "Username", this.UserName),
                                    new XElement(ns1 + "Password", this.Password)))),
                        new XElement(env + "Body",
                            new XElement(ns2 + "rgWsPublic2AfmMethod",
                                new XElement(ns2 + "INPUT_REC",
                                    new XElement(ns3 + "afm_called_by", ""),
                                    new XElement(ns3 + "afm_called_for", taxNumber),
                                    new XElement(ns3 + "as_on_date", DateTime.Now.ToString("yyyy-MM-dd"))))
                            )
                        )
                    );


                StringContent content = null;
                using (HttpClient cl = new HttpClient(handler))
                {
                    var request = new HttpRequestMessage()
                    {
                        RequestUri = new Uri("https://www1.gsis.gr:443/wsaade/RgWsPublic2/RgWsPublic2"),
                        Method = HttpMethod.Post
                    };
                    request.Content = new StringContent(soapRequest.ToString(), Encoding.UTF8, "application/soap+xml");
                    request.Headers.Clear();
                    cl.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/soap+xml"));
                    request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/soap+xml");
                    request.Headers.Add("SOAPAction", "http://rgwspublic2/RgWsPublic2Service/rgWsPublic2AfmMethod");
                    cl.Timeout = TimeSpan.FromSeconds(120);
                    string responseString = "";
                    try
                    {
                        HttpResponseMessage response = cl.SendAsync(request).Result;
                        if (response != null)
                        {
                            responseString = response.Content.ReadAsStringAsync().Result;

                            var soapResponse = XDocument.Parse(responseString);

                            var xmlResp = soapResponse.Descendants(ns2 + "result").FirstOrDefault().Descendants().FirstOrDefault().ToString();

                            XmlSerializer xmlDeserializer = new XmlSerializer(typeof(rg_ws_public2_result_rtType), ns3.NamespaceName);

                            using (TextReader reader = new StringReader(xmlResp))
                            {
                                var result = xmlDeserializer.Deserialize(reader) as rg_ws_public2_result_rtType;
                                if (result.error_rec.error_descr != null && result.error_rec.error_descr != "")
                                {
                                    traderResponse.ErrorMessage = result.error_rec.error_descr;
                                    return traderResponse;
                                }
                                var trader = new TraderDetailsViewModel();
                                
                                trader.Name = result.basic_rec.onomasia;
                                trader.TraderId = Guid.NewGuid();
                                trader.TaxRegistrationNumber = result.basic_rec.afm;
                                trader.TaxRegistrationOffice = result.basic_rec.doy_descr;
                                trader.Address =
                                    result.basic_rec.postal_address + " " +
                                    result.basic_rec.postal_address_no;
                                trader.City = result.basic_rec.postal_area_description;
                                trader.ZipCode = result.basic_rec.postal_zip_code;
                                if (result.firm_act_tab != null)
                                {
                                    var mainOccupation = result.firm_act_tab.FirstOrDefault(f => f.firm_act_kind == "1");
                                    trader.Occupation = mainOccupation != null ? mainOccupation.firm_act_descr : "";
                                }
                                traderResponse.Status = result.basic_rec.deactivation_flag_descr;
                                traderResponse.Trader = trader;
                                traderResponse.ErrorMessage = result.error_rec != null ? result.error_rec.error_descr : "";
                                return traderResponse;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        traderResponse.ErrorMessage = ex.Message;
                        return traderResponse;
                    }
                }
                return traderResponse;
            }
            catch (Exception ex)
            {
                traderResponse.ErrorMessage = ex.Message;
                return traderResponse;
            }
        }
    }
    public class AadeTraderResponse
    {
        public TraderDetailsViewModel Trader { set; get; }
        public string ErrorMessage { set; get; }
        public string Status { set; get; }
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.8.3752.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://rgwspublic2/RgWsPublic2")]
    public class rg_ws_public2_result_rtType
    {
        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, Order = 0)]
        public System.Nullable<decimal> call_seq_id { set; get; }
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool call_seq_idSpecified { set; get; }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public ErrorRecord error_rec { set; get; }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 2)]
        public AfmCalled afm_called_by_rec { set; get; }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 3)]
        public BasicRec basic_rec { set; get; }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute(Order = 4)]
        [System.Xml.Serialization.XmlArrayItemAttribute("item", IsNullable = false)]
        public FirmActTab[] firm_act_tab { set; get; }

        [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.8.3752.0")]
        [System.SerializableAttribute()]
        [System.Diagnostics.DebuggerStepThroughAttribute()]
        [System.ComponentModel.DesignerCategoryAttribute("code")]
        [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://rgwspublic2/RgWsPublic2")]
        public class ErrorRecord
        {
            [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, Order = 0)]
            public string error_code { set; get; }

            /// <remarks/>
            [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, Order = 1)]
            public string error_descr { set; get; }
        }

        [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.8.3752.0")]
        [System.SerializableAttribute()]
        [System.Diagnostics.DebuggerStepThroughAttribute()]
        [System.ComponentModel.DesignerCategoryAttribute("code")]
        [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://rgwspublic2/RgWsPublic2")]
        public class FirmActTab
        {
            [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, Order = 0)]
            public System.Nullable<decimal> firm_act_code { set; get; }

            /// <remarks/>
            [System.Xml.Serialization.XmlIgnoreAttribute()]
            public bool firm_act_codeSpecified { set; get; }

            /// <remarks/>
            [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, Order = 1)]
            public string firm_act_descr { set; get; }

            /// <remarks/>
            [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, Order = 2)]
            public string firm_act_kind { set; get; }

            /// <remarks/>
            [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, Order = 3)]
            public string firm_act_kind_descr { set; get; }
        }

        [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.8.3752.0")]
        [System.SerializableAttribute()]
        [System.Diagnostics.DebuggerStepThroughAttribute()]
        [System.ComponentModel.DesignerCategoryAttribute("code")]
        [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://rgwspublic2/RgWsPublic2")]
        public class BasicRec
        {
            [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, Order = 0)]
            public string afm { set; get; }
            /// <remarks/>
            [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, Order = 1)]
            public string doy { set; get; }

            /// <remarks/>
            [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, Order = 2)]
            public string doy_descr { set; get; }

            /// <remarks/>
            [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, Order = 3)]
            public string i_ni_flag_descr { set; get; }

            /// <remarks/>
            [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, Order = 4)]
            public string deactivation_flag { set; get; }

            /// <remarks/>
            [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, Order = 5)]
            public string deactivation_flag_descr { set; get; }

            /// <remarks/>
            [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, Order = 6)]
            public string firm_flag_descr { set; get; }

            /// <remarks/>
            [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, Order = 7)]
            public string onomasia { set; get; }

            /// <remarks/>
            [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, Order = 8)]
            public string commer_title { set; get; }

            /// <remarks/>
            [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, Order = 9)]
            public string legal_status_descr { set; get; }

            /// <remarks/>
            [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, Order = 10)]
            public string postal_address { set; get; }

            /// <remarks/>
            [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, Order = 11)]
            public string postal_address_no { set; get; }

            /// <remarks/>
            [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, Order = 12)]
            public string postal_zip_code { set; get; }

            /// <remarks/>
            [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, Order = 13)]
            public string postal_area_description { set; get; }

            /// <remarks/>
            [System.Xml.Serialization.XmlElementAttribute(DataType = "date", IsNullable = true, Order = 14)]
            public System.Nullable<System.DateTime> regist_date { set; get; }

            /// <remarks/>
            [System.Xml.Serialization.XmlIgnoreAttribute()]
            public bool regist_dateSpecified { set; get; }

            /// <remarks/>
            [System.Xml.Serialization.XmlElementAttribute(DataType = "date", IsNullable = true, Order = 15)]
            public System.Nullable<System.DateTime> stop_date { set; get; }

            /// <remarks/>
            [System.Xml.Serialization.XmlIgnoreAttribute()]
            public bool stop_dateSpecified { set; get; }

            /// <remarks/>
            [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, Order = 16)]
            public string normal_vat_system_flag { set; get; }
        }

        [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.8.3752.0")]
        [System.SerializableAttribute()]
        [System.Diagnostics.DebuggerStepThroughAttribute()]
        [System.ComponentModel.DesignerCategoryAttribute("code")]
        [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://rgwspublic2/RgWsPublic2")]
        public partial class AfmCalled
        {
            /// <remarks/>
            [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, Order = 0)]
            public string token_username { set; get; }

            /// <remarks/>
            [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, Order = 1)]
            public string token_afm { set; get; }

            /// <remarks/>
            [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, Order = 2)]
            public string token_afm_fullname { set; get; }

            /// <remarks/>
            [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, Order = 3)]
            public string afm_called_by { set; get; }

            /// <remarks/>
            [System.Xml.Serialization.XmlElementAttribute(IsNullable = true, Order = 4)]
            public string afm_called_by_fullname { set; get; }

            /// <remarks/>
            [System.Xml.Serialization.XmlElementAttribute(DataType = "date", IsNullable = true, Order = 5)]
            public System.Nullable<System.DateTime> as_on_date { set; get; }

            /// <remarks/>
            [System.Xml.Serialization.XmlIgnoreAttribute()]
            public bool as_on_dateSpecified { set; get; }
        }
    }
}
