using Exedron.Invoicing.Core;
using System;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using Exedron.Invoicing.Core.Models;

namespace Exedron.Invoicing.Adapters
{
    // ERP-channel XML for the shared validated sales/credit/delivery model.
    public static class WorkflowInvoiceXml
    {
        private static readonly XNamespace Inv = "http://www.aade.gr/myDATA/invoice/v1.0";
        private static readonly XNamespace Income = "https://www.aade.gr/myDATA/incomeClassificaton/v1.0";
        public static string Build(InvoiceModel model)
        {
            // Provider adapters retain their existing party mapping and validation.
            return Build(model, false);
        }
        public static string BuildForDirectMyData(InvoiceModel model)
        {
            return Build(model, true);
        }
        private static string Build(InvoiceModel model, bool directMyData)
        {
            InvoiceValidation.Validate(model);
            var h = model.InvoiceHeader;
            var specialCategory = MyDataHeaderFields.SpecialInvoiceCategory(model);
            var isDeliveryNote = MyDataHeaderFields.IsDeliveryNote(model);
            var movePurpose = MyDataHeaderFields.EffectiveMovePurpose(model);
            var deliveryHeader = MyDataHeaderFields.EffectiveDeliveryHeader(model);
            var movementOnly = h.InvoiceType == "9.1" || h.InvoiceType == "9.2" || h.InvoiceType == "9.3" || h.InvoiceType == "10.1";
            var delivery = movementOnly || isDeliveryNote;
            var deliveryParties = directMyData && delivery;
            var supplyAccount = MyDataPartyFields.SupplyAccountNumber(model);
            if (deliveryParties)
            {
                ValidateDeliveryParty(model.Issuer, "issuer");
                ValidateDeliveryParty(model.CounterPart, "recipient");
            }
            var includeQuantity = MyDataLineFields.IncludeQuantityAndUnit(h.InvoiceType);
            var header = E("invoiceHeader", E("series", h.Series), E("aa", h.AA),
                E("issueDate", h.IssueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                E("invoiceType", h.InvoiceType),
                h.VATPaymentSuspension ? E("vatPaymentSuspension", true) : null,
                E("currency", h.Currency));
            if (h.CorrelatedInvoices != null) foreach (var mark in h.CorrelatedInvoices) header.Add(E("correlatedInvoices", mark));
            if (h.InvoiceType != "10.1" && h.DispatchDateTime != default(DateTime))
            {
                header.Add(E("dispatchDate", h.DispatchDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
                header.Add(E("dispatchTime", h.DispatchDateTime.ToString("HH:mm:ss", CultureInfo.InvariantCulture)));
            }
            if (h.InvoiceType != "10.1" && !string.IsNullOrWhiteSpace(h.VehicleNumber)) header.Add(E("vehicleNumber", h.VehicleNumber));
            if (delivery && h.InvoiceType != "10.1") header.Add(E("movePurpose", (int)movePurpose));
            if (h.FuelInvoice) header.Add(E("fuelInvoice", true));
            if (specialCategory.HasValue) header.Add(E("specialInvoiceCategory", specialCategory.Value));
            if (h.InvoiceType != "10.1" && deliveryHeader != null)
            {
                var d = deliveryHeader;
                header.Add(E("otherDeliveryNoteHeader", Address("loadingAddress", d.LoadingAddress),
                    Address("deliveryAddress", d.DeliveryAddress), E("startShippingBranch", d.StartShippingBranch), E("completeShippingBranch", d.CompleteShippingBranch)));
            }
            if (isDeliveryNote && !movementOnly) header.Add(E("isDeliveryNote", true));
            var invoice = E("invoice", Party("issuer", model.Issuer, deliveryParties));
            if (model.CounterPart != null && (!h.InvoiceType.StartsWith("11.", StringComparison.Ordinal) || deliveryParties || supplyAccount != null))
                invoice.Add(Party("counterpart", model.CounterPart, deliveryParties, supplyAccount));
            invoice.Add(header);
            if (model.PaymentMethods != null && model.PaymentMethods.Length > 0)
                invoice.Add(E("paymentMethods", model.PaymentMethods.Select(p => E("paymentMethodDetails", E("type", (int)p.Type), E("amount", Money(p.Amount))))));
            foreach (var line in model.InvoiceDetails)
            {
                var row = E("invoiceDetails", E("lineNumber", line.LineNumber));
                if (delivery)
                {
                    if (!string.IsNullOrWhiteSpace(line.ItemCode)) row.Add(E("itemCode", line.ItemCode));
                    if (!string.IsNullOrWhiteSpace(line.ItemDescription)) row.Add(E("itemDescr", line.ItemDescription));
                }
                if (!string.IsNullOrEmpty(line.FuelCode)) row.Add(E("fuelCode", line.FuelCode));
                if (includeQuantity)
                {
                    row.Add(E("quantity", line.Quantity));
                    if ((int)line.MeasurementUnit > 0) row.Add(E("measurementUnit", (int)line.MeasurementUnit));
                }
                row.Add(E("netValue", Money(line.NetValue)), E("vatCategory", (int)line.VATCategory), E("vatAmount", Money(line.VATAmount)));
                if (line.VATExemptionCategory != null) row.Add(E("vatExemptionCategory", Convert.ToInt32(line.VATExemptionCategory, CultureInfo.InvariantCulture)));
                if (line.IncomeClassification != null) row.Add(Classification(line.IncomeClassification.ClassificationType, line.IncomeClassification.ClassificationCategory, line.IncomeClassification.Amount));
                var quantity15 = MovementQuantities.Get(line);
                if (includeQuantity && quantity15.HasValue) row.Add(E("quantity15", quantity15.Value));
                invoice.Add(row);
            }
            var summary = E("invoiceSummary", E("totalNetValue", Money(model.InvoiceSummary.TotalNetValue)),
                E("totalVatAmount", Money(model.InvoiceSummary.TotalVATAmount)), E("totalWithheldAmount", "0.00"),
                E("totalFeesAmount", "0.00"), E("totalStampDutyAmount", "0.00"), E("totalOtherTaxesAmount", "0.00"),
                E("totalDeductionsAmount", "0.00"), E("totalGrossValue", Money(model.InvoiceSummary.TotalGrossValue)));
            foreach (var group in model.InvoiceDetails.Select(l => l.IncomeClassification).Where(c => c != null).GroupBy(c => new { c.ClassificationType, c.ClassificationCategory }))
                summary.Add(Classification(group.Key.ClassificationType, group.Key.ClassificationCategory, group.Sum(c => c.Amount)));
            invoice.Add(summary);
            var xml = E("InvoicesDoc", new XAttribute(XNamespace.Xmlns + "icls", Income), invoice).ToString(SaveOptions.DisableFormatting);
            MyDataSchema.Validate(xml);
            return xml;
        }
        private static XElement E(string name, params object[] content) { return new XElement(Inv + name, content); }
        private static string Money(decimal value) { return value.ToString("0.00", CultureInfo.InvariantCulture); }
        private static XElement Classification(string type, string category, decimal amount)
        {
            return E("incomeClassification", string.IsNullOrWhiteSpace(type) ? null : new XElement(Income + "classificationType", type),
                new XElement(Income + "classificationCategory", category), new XElement(Income + "amount", Money(amount)));
        }
        private static void ValidateDeliveryParty(Trader party, string label)
        {
            if (party == null || string.IsNullOrWhiteSpace(party.VATNumber) || string.IsNullOrWhiteSpace(party.Name) ||
                party.Address == null || string.IsNullOrWhiteSpace(party.Address.PostalCode) || string.IsNullOrWhiteSpace(party.Address.City))
                throw new ProviderInvoiceValidationException("The delivery invoice requires " + label + " details (VAT number, name and address). Check the company/customer settings before sending to myDATA.");
        }
        private static XElement Party(string name, Trader party, bool delivery, string supplyAccount = null)
        {
            var result = E(name, E("vatNumber", party.VATNumber), E("country", party.Country), E("branch", party.Branch));
            if (delivery || party.Country != "GR")
            {
                if (!string.IsNullOrWhiteSpace(party.Name)) result.Add(E("name", party.Name));
                if (party.Address != null) result.Add(Address("address", party.Address));
            }
            if (supplyAccount != null) result.Add(E("supplyAccountNo", supplyAccount));
            return result;
        }
        private static XElement Address(string name, Exedron.Invoicing.Core.Models.Address address)
        {
            return E(name, string.IsNullOrWhiteSpace(address.Street) ? null : E("street", address.Street),
                string.IsNullOrWhiteSpace(address.Number) ? null : E("number", address.Number), E("postalCode", address.PostalCode), E("city", address.City));
        }
    }
}
