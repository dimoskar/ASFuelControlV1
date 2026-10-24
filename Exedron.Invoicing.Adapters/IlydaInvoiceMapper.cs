using Exedron.Invoicing.Core;
using Exedron.Invoicing.Core.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Exedron.Invoicing.Adapters
{
    internal static class IlydaInvoiceMapper
    {
        internal static JObject Build(InvoiceModel invoice, Func<InvoiceModel, string> xmlFactory)
        {
            InvoiceValidation.Validate(invoice);
            if (invoice?.InvoiceHeader == null || invoice.Issuer == null || invoice.InvoiceSummary == null ||
                invoice.InvoiceDetails == null || invoice.InvoiceDetails.Length == 0 || xmlFactory == null)
                throw new InvalidOperationException("Ilyda: Λείπουν τα απαιτούμενα στοιχεία παραστατικού.");
            var h = invoice.InvoiceHeader;
            var type = h.InvoiceType;
            if ((invoice.TaxesTotals ?? new TaxesTotal[0]).Length != 0 || h.SelfPricing || h.VATPaymentSuspension || (h.ExchangeRate != 0 && h.ExchangeRate != 1))
                throw new InvalidOperationException("Ilyda: Η τρέχουσα διασύνδεση δεν υποστηρίζει πρόσθετους φόρους, αυτοτιμολόγηση, αναστολή ΦΠΑ ή μετατροπή νομίσματος.");
            if (!new[] { "1.1", "1.2", "1.3", "1.4", "1.6", "2.1", "2.2", "2.3", "5.1", "5.2", "11.1", "11.2", "11.4", "9.1", "9.2", "9.3", "10.1" }.Contains(type))
                throw new InvalidOperationException("Ilyda: Μη υποστηριζόμενος τύπος παραστατικού: " + type);
            if (h.IssueDate == default(DateTime) || string.IsNullOrWhiteSpace(h.AA) || string.IsNullOrWhiteSpace(h.Series))
                throw new InvalidOperationException("Ilyda: Απαιτούνται ημερομηνία, σειρά και αριθμός.");
            if (type == "9.1" && (h.CorrelatedInvoices == null || h.CorrelatedInvoices.Length == 0 || h.CorrelatedInvoices.Any(mark => mark <= 0)))
                throw new InvalidOperationException("Ilyda: Το συσχετιζόμενο δελτίο αποστολής 9.1 απαιτεί έγκυρο MARK αρχικού παραστατικού.");
            var xml = xmlFactory(invoice);
            var document = XDocument.Parse(xml);
            XNamespace ns = "http://www.aade.gr/myDATA/invoice/v1.0";
            var xinvoice = document.Root?.Element(ns + "invoice");
            if (xinvoice?.Element(ns + "invoiceHeader")?.Element(ns + "invoiceType")?.Value != type)
                throw new InvalidOperationException("Ilyda: Το ενσωματωμένο myDATA XML δεν συμφωνεί με το παραστατικό.");
            var specialCategory = MyDataHeaderFields.SpecialInvoiceCategory(invoice);
            var isDeliveryNote = MyDataHeaderFields.IsDeliveryNote(invoice);
            var movePurpose = MyDataHeaderFields.EffectiveMovePurpose(invoice);
            var effectiveDeliveryHeader = MyDataHeaderFields.EffectiveDeliveryHeader(invoice);
            var xmlHeader = xinvoice.Element(ns + "invoiceHeader");
            // Also support host-provided XML serializers. Keep JSON and embedded
            // XML consistent, without changing the source model or duplicating the element.
            xmlHeader.Elements(ns + "specialInvoiceCategory").Remove();
            if (specialCategory.HasValue)
            {
                var fuel = xmlHeader.Element(ns + "fuelInvoice");
                if (fuel == null || (fuel.Value != "true" && fuel.Value != "1"))
                    throw new InvalidOperationException("Ilyda: Το ενσωματωμένο myDATA XML δεν περιέχει fuelInvoice=true.");
                fuel.AddAfterSelf(new XElement(ns + "specialInvoiceCategory", specialCategory.Value));
            }
            if (specialCategory == 11)
            {
                var purpose = xmlHeader.Element(ns + "movePurpose");
                if (purpose == null)
                {
                    purpose = new XElement(ns + "movePurpose", (int)movePurpose);
                    var fuel = xmlHeader.Element(ns + "fuelInvoice");
                    if (fuel != null) fuel.AddBeforeSelf(purpose);
                    else xmlHeader.Element(ns + "specialInvoiceCategory").AddBeforeSelf(purpose);
                }
                else purpose.Value = ((int)movePurpose).ToString(CultureInfo.InvariantCulture);
                var normalizedDeliveryHeader = DeliveryHeader(ns, effectiveDeliveryHeader);
                var existingDeliveryHeader = xmlHeader.Element(ns + "otherDeliveryNoteHeader");
                if (existingDeliveryHeader != null) existingDeliveryHeader.ReplaceWith(normalizedDeliveryHeader);
                else xmlHeader.Element(ns + "specialInvoiceCategory").AddAfterSelf(normalizedDeliveryHeader);
                xmlHeader.Elements(ns + "isDeliveryNote").Remove();
                var deliveryFlag = new XElement(ns + "isDeliveryNote", isDeliveryNote);
                var deliveryHeader = xmlHeader.Element(ns + "otherDeliveryNoteHeader");
                if (deliveryHeader != null) deliveryHeader.AddAfterSelf(deliveryFlag);
                else xmlHeader.Element(ns + "specialInvoiceCategory").AddAfterSelf(deliveryFlag);
            }
            xml = document.ToString(SaveOptions.DisableFormatting);
            if (type == "10.1")
            {
                var purpose = ReceivingNotePurposes.Get(h);
                if (!purpose.HasValue)
                    throw new InvalidOperationException("Ilyda: Λείπει η αιτία έκδοσης του δελτίου ποσοτικής παραλαβής.");
                var header = xinvoice.Element(ns + "invoiceHeader");
                header.Element(ns + "receivingNotePurpose")?.Remove();
                // Current shared XML ends before the v2.0.2 receiving-purpose fields.
                header.Add(new XElement(ns + "receivingNotePurpose", purpose.Value));
                xml = document.ToString(SaveOptions.DisableFormatting);
            }
            bool retailDelivery = type == "11.1" && h.DeliveryNote;
            var supplyAccount = MyDataPartyFields.SupplyAccountNumber(invoice);
            if (retailDelivery)
            {
                var buyer = invoice.CounterPart;
                if (buyer == null || string.IsNullOrWhiteSpace(buyer.VATNumber) || string.IsNullOrWhiteSpace(buyer.Country) ||
                    string.IsNullOrWhiteSpace(buyer.Name) || buyer.Address == null || string.IsNullOrWhiteSpace(buyer.Address.City) ||
                    string.IsNullOrWhiteSpace(buyer.Address.PostalCode))
                    throw new InvalidOperationException("Ilyda: Η απόδειξη λιανικής με διακίνηση απαιτεί στοιχεία παραλήπτη (ΑΦΜ, επωνυμία, χώρα και διεύθυνση).");
                // Complete the retail delivery recipient with Ilyda's required identity/address.
                var counterpart = new XElement(ns + "counterpart",
                    new XElement(ns + "vatNumber", buyer.VATNumber), new XElement(ns + "country", buyer.Country),
                    new XElement(ns + "branch", buyer.Branch), new XElement(ns + "name", buyer.Name),
                    new XElement(ns + "address",
                        string.IsNullOrWhiteSpace(buyer.Address.Street) ? null : new XElement(ns + "street", buyer.Address.Street),
                        string.IsNullOrWhiteSpace(buyer.Address.Number) ? null : new XElement(ns + "number", buyer.Address.Number),
                        new XElement(ns + "postalCode", buyer.Address.PostalCode), new XElement(ns + "city", buyer.Address.City)));
                if (supplyAccount != null) counterpart.Add(new XElement(ns + "supplyAccountNo", supplyAccount));
                xinvoice.Element(ns + "counterpart")?.Remove();
                xinvoice.Element(ns + "invoiceHeader").AddBeforeSelf(counterpart);
                xml = document.ToString(SaveOptions.DisableFormatting);
            }
            MyDataSchema.Validate(xml);
            if ((invoice.PaymentMethods ?? new PaymentMethod[0]).Any(p => (int)p.Type == 7))
            {
                // Ilyda EG-5 carries the complete payment breakdown. Duplicating it in
                // the attached myDATA XML causes X-0213, including after POS recovery.
                xinvoice.Element(ns + "paymentMethods")?.Remove();
                xml = document.ToString(SaveOptions.DisableFormatting);
            }
            var lines = new JArray();
            foreach (var line in invoice.InvoiceDetails)
            {
                if (line.Quantity <= 0 || string.IsNullOrWhiteSpace(line.ItemDescription) || line.NetValue < 0 || line.VATAmount < 0)
                    throw new InvalidOperationException("Ilyda: Κάθε γραμμή απαιτεί περιγραφή, θετική ποσότητα και μη αρνητικές αξίες.");
                var rate = VatRate((int)line.VATCategory);
                var category = VatCategory(line, h.InvoiceType);
                var unit = Unit((int)line.MeasurementUnit);
                lines.Add(new JObject {
                    ["lineNumber"] = line.LineNumber, ["invoicedQuantity"] = line.Quantity, ["invoicedQuantityUnits"] = unit,
                    ["netAmount"] = line.NetValue, ["isAadeSynopsis"] = false,
                    ["itemInfo"] = new JObject { ["itemInfoName"] = line.ItemDescription, ["itemInfoDescription"] = line.ItemDescription, ["sellerIdentifier"] = line.ItemCode },
                    ["priceDetails"] = new JObject { ["itemPriceBaseQuantity"] = 1, ["itemPriceBaseQuantityUnitsCode"] = unit, ["itemNetPrice"] = line.NetValue / line.Quantity },
                    ["lineVatInfo"] = new JObject { ["vatRate"] = rate, ["vatAmount"] = line.VATAmount, ["vatCategoryCode"] = category,
                        ["aadeVatData"] = new JObject { ["aadeVatCategory"] = (int)line.VATCategory } }
                });
            }
            var totals = invoice.InvoiceSummary;
            if (invoice.InvoiceDetails.Sum(l => l.NetValue) != totals.TotalNetValue || invoice.InvoiceDetails.Sum(l => l.VATAmount) != totals.TotalVATAmount ||
                totals.TotalNetValue + totals.TotalVATAmount != totals.TotalGrossValue)
                throw new InvalidOperationException("Ilyda: Τα σύνολα δεν συμφωνούν με τις γραμμές.");
            var vat = new JArray();
            foreach (var group in invoice.InvoiceDetails.GroupBy(l => new { Category = (int)l.VATCategory, l.VATExemptionCategory, NativeCategory = VatCategory(l,h.InvoiceType) }))
            {
                var rate = VatRate(group.Key.Category);
                var breakdown = new JObject { ["categoryCode"] = group.Key.NativeCategory,
                    ["categoryRate"] = rate, ["categoryTaxAmount"] = group.Sum(l => l.VATAmount), ["categoryTaxableAmount"] = group.Sum(l => l.NetValue),
                    ["aadeVatData"] = new JObject { ["aadeVatCategory"] = group.Key.Category } };
                if (group.Key.VATExemptionCategory != null)
                {
                    var exemption = Convert.ToInt32(group.Key.VATExemptionCategory, CultureInfo.InvariantCulture);
                    ((JObject)breakdown["aadeVatData"])["aadeVatExemptionCategory"] = exemption;
                    breakdown["exemptionReasonText"] = "Απαλλαγή ΦΠΑ myDATA " + exemption;
                }
                vat.Add(breakdown);
            }
            var payload = new JObject {
                ["invoiceNumber"] = string.Join("|", invoice.Issuer.VATNumber, h.IssueDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                    invoice.Issuer.Branch.ToString(CultureInfo.InvariantCulture), type, h.Series, h.AA),
                ["seriesNumber"] = h.Series, ["serialNumber"] = h.AA, ["invoiceIssueDate"] = FormatIssueDate(h.IssueDate),
                ["invoiceTypeCode"] = type == "5.1" || type == "5.2" || type == "11.4" ? "381" : "380",
                ["invoiceCurrencyCode"] = h.Currency, ["b2g"] = false, ["selfPricing"] = h.SelfPricing, ["vatPaidByBuyer"] = h.VATPaymentSuspension,
                ["paymentTerms"] = invoice.PaymentTerms,
                ["seller"] = Party(invoice.Issuer, true), ["invoiceLines"] = lines, ["vatBreakdowns"] = vat,
                ["paymentMethods"] = new JArray((invoice.PaymentMethods ?? new PaymentMethod[0]).Select(p => new JObject { ["type"] = (int)p.Type, ["amount"] = p.Amount })),
                ["docTotal"] = new JObject { ["invoiceLinesNetAmountSum"] = totals.TotalNetValue, ["invoiceTotalWithoutVat"] = totals.TotalNetValue,
                    ["invoiceTotalVatAmount"] = totals.TotalVATAmount, ["invoiceTotalAmountWithVat"] = totals.TotalGrossValue,
                    ["amountDueForPayment"] = totals.TotalGrossValue, ["paidAmount"] = 0, ["roundingAmount"] = 0,
                    ["documentLevelChargesSum"] = 0, ["documentLevelAllowancesSum"] = 0 },
                ["additionalSupportDocs"] = new JArray(new JObject { ["description"] = "##InvoicesDoc##", ["reference"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(xml)) })
            };
            if (invoice.CounterPart != null && (!type.StartsWith("11.", StringComparison.Ordinal) || retailDelivery || supplyAccount != null)) payload["buyer"] = Party(invoice.CounterPart, false);
            if (specialCategory.HasValue)
                payload["aadeData"] = new JObject { ["aadeSpecialInvoiceCategory"] = specialCategory.Value };
            if (effectiveDeliveryHeader?.DeliveryAddress != null && type != "10.1")
                payload["delivery"] = new JObject { ["deliveryAddress"] = Address(effectiveDeliveryHeader.DeliveryAddress, "delivery", "GR") };
            if (h.CorrelatedInvoices != null && h.CorrelatedInvoices.Length > 0)
                payload["precedingInvoices"] = new JArray(h.CorrelatedInvoices.Select(mark => new JObject { ["precedingInvoiceReference"] = mark.ToString(CultureInfo.InvariantCulture) }));
            return payload;
        }
        private static XElement DeliveryHeader(XNamespace ns, OtherDeliveryNoteHeader source)
        {
            return new XElement(ns + "otherDeliveryNoteHeader",
                XmlAddress(ns, "loadingAddress", source.LoadingAddress),
                XmlAddress(ns, "deliveryAddress", source.DeliveryAddress),
                new XElement(ns + "startShippingBranch", source.StartShippingBranch),
                new XElement(ns + "completeShippingBranch", source.CompleteShippingBranch));
        }
        private static XElement XmlAddress(XNamespace ns, string name, Exedron.Invoicing.Core.Models.Address source)
        {
            return new XElement(ns + name,
                string.IsNullOrWhiteSpace(source.Street) ? null : new XElement(ns + "street", source.Street),
                string.IsNullOrWhiteSpace(source.Number) ? null : new XElement(ns + "number", source.Number),
                new XElement(ns + "postalCode", source.PostalCode),
                new XElement(ns + "city", source.City));
        }
        internal static string FormatIssueDate(DateTime issueDate)
        {
            // InvoiceModel contains the Greek fiscal wall-clock time from the database.
            // Ilyda documents a timestamp with an explicit offset (e.g. ...000+0200).
            // Use the offset at issuance, including daylight saving, not the PC's zone.
            var local = DateTime.SpecifyKind(issueDate, DateTimeKind.Unspecified);
            var zone = TimeZoneInfo.FindSystemTimeZoneById("GTB Standard Time");
            var dated = new DateTimeOffset(local, zone.GetUtcOffset(local));
            // Keep the existing whole-second precision, including when retrying a saved request.
            return dated.ToString("yyyy-MM-ddTHH:mm:ss'.000'", CultureInfo.InvariantCulture) +
                dated.ToString("zzz", CultureInfo.InvariantCulture).Replace(":", "");
        }

        private static string VatCategory(InvoiceDetail line, string invoiceType)
        {
            if (VatRate((int)line.VATCategory) > 0) return "S";
            if ((int)line.VATCategory == 8)
                return new[] { "9.1","9.2","9.3","10.1" }.Contains(invoiceType) ? "Z" : "O";
            var exemption = line.VATExemptionCategory == null ? 0 : Convert.ToInt32(line.VATExemptionCategory, CultureInfo.InvariantCulture);
            if (exemption == 14) return "K"; // Article 28, intra-community supply.
            if (exemption == 8) return "G";  // Article 24, export outside the EU.
            if (exemption == 16) return "AE"; // Article 39a, reverse charge.
            if (exemption == 1 || exemption == 2 || exemption == 3 || exemption == 4) return "O"; // Outside VAT scope.
            return "E";
        }

        private static JObject Party(Trader source, bool seller)
        {
            string prefix = seller ? "seller" : "buyer";
            return new JObject { [prefix + "Name"] = source.Name, [prefix + "VatIdentifier"] = source.VATNumber,
                [seller ? "branch" : "buyerBranch"] = source.Branch,
                [prefix + "PostalAddress"] = Address(source.Address, prefix, source.Country) };
        }
        private static JObject Address(Exedron.Invoicing.Core.Models.Address address, string prefix, string country)
        {
            return new JObject { [prefix + "CountryCode"] = country,
                [prefix + "AddressLine1"] = address?.Street,
                [prefix + "AddressLine2"] = address?.Number,
                [prefix + "City"] = address?.City, [prefix + "PostCode"] = address?.PostalCode };
        }
        internal static decimal VatRate(int category)
        {
            switch (category) { case 1: return 24; case 2: return 13; case 3: return 6; case 4: return 17; case 5: return 9; case 6: return 4; case 7: case 8: return 0; default: throw new InvalidOperationException("Ilyda: Μη υποστηριζόμενη κατηγορία ΦΠΑ."); }
        }
        private static string Unit(int unit)
        {
            switch (unit) { case 0: return "C62"; case 1: return "H87"; case 2: return "KGM"; case 3: return "LTR"; case 4: return "MTR"; case 5: return "MTK"; case 6: return "MTQ"; default: throw new InvalidOperationException("Ilyda: Μη υποστηριζόμενη μονάδα μέτρησης."); }
        }
    }
}
