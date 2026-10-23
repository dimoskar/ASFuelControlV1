using System;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Exedron.Invoicing.Core;
using Exedron.Invoicing.Core.Models;
using Exedron.Invoicing.Adapters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static partial class Program
{
    private static void HeatingAllowance(string culture)
    {
        foreach (var code in new[] { "30", "31" })
        foreach (var type in new[] { "1.1", "1.4", "1.6", "5.1", "5.2", "11.1", "11.4" })
            Test(culture + " heating category " + type + " / fuel " + code, () =>
                CheckHeatingPayloads(HeatingModel(type, code), true));

        foreach (var supply in new[] { null, "", "  \t\r\n" })
            Test(culture + " heating category omitted for blank supply [" + supply + "]", () => {
                var model = HeatingModel("11.1", "30"); model.CounterPart.SupplyAccountNo = supply;
                CheckHeatingPayloads(model, false);
            });
        Test(culture + " heating category omitted without recipient", () => {
            var model = HeatingModel("11.1", "31"); model.CounterPart = null;
            CheckHeatingPayloads(model, false);
        });
        foreach (var code in new[] { "10", "20", "32" })
            Test(culture + " heating category omitted for other fuel " + code, () =>
                CheckHeatingPayloads(HeatingModel("1.1", code), false));
        foreach (var type in new[] { "1.2", "1.3", "2.1", "11.2" })
        foreach (var code in new[] { "30", "31" })
            Test(culture + " heating category omitted for other type " + type + " / " + code, () =>
                CheckHeatingPayloads(HeatingModel(type, code), false));
        Test(culture + " heating category omitted for valid nonfuel invoice", () => {
            var model = HeatingModel("1.1", null); model.InvoiceHeader.FuelInvoice = false;
            CheckHeatingPayloads(model, false);
        });
        foreach (var code in new[] { "30", "31" })
            Test(culture + " nonfuel flag excludes heating category without weakening validation / " + code, () => {
                var model = HeatingModel("1.1", code); model.InvoiceHeader.FuelInvoice = false;
                var before = JsonConvert.SerializeObject(model);
                Equal<int?>(null, MyDataHeaderFields.SpecialInvoiceCategory(model));
                // A fuel code on a nonfuel invoice is already invalid. Keep that
                // validation rather than sending an invalid invoice to test omission.
                foreach (var provider in new[] { InvoiceProviderCatalog.MyDataKey, InvoiceProviderCatalog.ITimologiaKey, InvoiceProviderCatalog.DirectIlydaKey })
                    Throws<ProviderInvoiceValidationException>(() => Factory.CreatePayloadMapper(provider).Serialize(model, Guid.NewGuid()));
                Equal(before, JsonConvert.SerializeObject(model));
            });
        Test(culture + " heating line need not be the first or only line", () => {
            var model = HeatingModel("1.1", "10");
            var heating = JsonConvert.DeserializeObject<InvoiceDetail>(JsonConvert.SerializeObject(model.InvoiceDetails[0]));
            heating.LineNumber = 2; heating.FuelCode = "31";
            model.InvoiceDetails = new[] { model.InvoiceDetails[0], heating };
            model.InvoiceSummary.TotalNetValue *= 2; model.InvoiceSummary.TotalVATAmount *= 2; model.InvoiceSummary.TotalGrossValue *= 2;
            CheckHeatingPayloads(model, true);
        });
        Test(culture + " heating classification does not leak into a subsequent serialization", () => {
            var model = HeatingModel("11.4", "30"); CheckHeatingPayloads(model, true);
            model.CounterPart.SupplyAccountNo = null; CheckHeatingPayloads(model, false);
        });
        Test(culture + " Ilyda normalizes category in custom embedded XML", () => {
            var model = HeatingModel("1.1", "30");
            var before = JsonConvert.SerializeObject(model);
            Func<InvoiceModel, string> oldSerializer = m => {
                var xml = XDocument.Parse(WorkflowInvoiceXml.Build(m));
                xml.Descendants(Ns + "specialInvoiceCategory").Remove();
                return xml.ToString(SaveOptions.DisableFormatting);
            };
            var json = JObject.Parse(IlydaInvoiceProvider.PrepareRequestJson(model, oldSerializer));
            CheckHeatingXml(EmbeddedXml(json), true);
            Equal(JTokenType.Integer, json["aadeData"]["aadeSpecialInvoiceCategory"].Type);
            Equal(before, JsonConvert.SerializeObject(model));
            model.CounterPart.SupplyAccountNo = null;
            Func<InvoiceModel, string> staleSerializer = m => {
                var xml = XDocument.Parse(WorkflowInvoiceXml.Build(m));
                xml.Descendants(Ns + "fuelInvoice").Single().AddAfterSelf(new XElement(Ns + "specialInvoiceCategory", 11));
                return xml.ToString(SaveOptions.DisableFormatting);
            };
            json = JObject.Parse(IlydaInvoiceProvider.PrepareRequestJson(model, staleSerializer));
            Check(json["aadeData"] == null, "Ineligible Ilyda payload retained aadeData.");
            CheckHeatingXml(EmbeddedXml(json), false);
        });
    }

    private static InvoiceModel HeatingModel(string type, string code)
    {
        var model = Builder.Build(Invoice());
        model.InvoiceHeader.InvoiceType = type;
        model.InvoiceHeader.FuelInvoice = true;
        model.InvoiceDetails[0].FuelCode = code;
        model.CounterPart.SupplyAccountNo = " 123456789 ";
        if (type == "5.1") model.InvoiceHeader.CorrelatedInvoices = new long[] { 123456 };
        return model;
    }

    private static void CheckHeatingPayloads(InvoiceModel model, bool expected)
    {
        var before = JsonConvert.SerializeObject(model);
        Equal<int?>(expected ? (int?)11 : null, MyDataHeaderFields.SpecialInvoiceCategory(model));
        var direct = Factory.CreatePayloadMapper(InvoiceProviderCatalog.MyDataKey).Serialize(model, Guid.NewGuid());
        CheckHeatingXml(direct, expected);
        Equal(before, JsonConvert.SerializeObject(model));

        var itimologia = JObject.Parse(Factory.CreatePayloadMapper(InvoiceProviderCatalog.ITimologiaKey).Serialize(model, Guid.NewGuid()));
        var category = itimologia["invoice_header"]["special_invoice_category"];
        if (expected) { Equal(JTokenType.String, category.Type); Equal("11", (string)category); }
        else Check(category == null, "iTimologia must omit the property, not send null or zero.");
        Equal(before, JsonConvert.SerializeObject(model));

        var ilyda = JObject.Parse(Factory.CreatePayloadMapper(InvoiceProviderCatalog.DirectIlydaKey).Serialize(model, Guid.NewGuid()));
        category = ilyda["aadeData"]?["aadeSpecialInvoiceCategory"];
        if (expected) { Equal(JTokenType.Integer, category.Type); Equal(11, (int)category); }
        else Check(category == null && ilyda["aadeData"] == null, "Ilyda must omit the category and unnecessary aadeData.");
        CheckHeatingXml(EmbeddedXml(ilyda), expected);
        Equal(before, JsonConvert.SerializeObject(model));
    }

    private static string EmbeddedXml(JObject ilyda)
    {
        var attachment = ilyda["additionalSupportDocs"].Single(d => (string)d["description"] == "##InvoicesDoc##");
        return Encoding.UTF8.GetString(Convert.FromBase64String((string)attachment["reference"]));
    }

    private static void CheckHeatingXml(string xml, bool expected)
    {
        MyDataSchema.Validate(xml);
        var header = XDocument.Parse(xml).Descendants(Ns + "invoiceHeader").Single();
        var categories = header.Elements(Ns + "specialInvoiceCategory").ToArray();
        Equal(expected ? 1 : 0, categories.Length);
        if (expected)
        {
            Equal("11", categories[0].Value);
            Equal(Ns + "fuelInvoice", ((XElement)categories[0].PreviousNode).Name);
        }
    }
}
