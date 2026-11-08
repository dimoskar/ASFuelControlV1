using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Xml;
using System.Xml.Linq;
using Exedron.Invoicing.Core;
using Exedron.Invoicing.Core.Models;

namespace Exedron.Invoicing.Adapters
{
    public sealed partial class MyDataClient
    {
        private static readonly XNamespace InvoiceNs = "http://www.aade.gr/myDATA/invoice/v1.0";
        private static readonly HashSet<string> ComparableLineFields =
            new HashSet<string>(new[]
            {
                "lineNumber", "itemCode", "fuelCode", "quantity",
                "measurementUnit", "netValue", "vatCategory", "vatAmount",
                "vatExemptionCategory", "incomeClassification",
                "expensesClassification"
            }, StringComparer.Ordinal);
        private InvoiceResponse Reconcile(MyDataSubmission saved, Guid id)
        {
            var pages = new List<string>();
            InvoiceResponse result;
            bool reconciliationRequired = saved.State == "ReconciliationRequired";
            bool reconciliationNotFound = false;
            try
            {
                var original = ReadXml(saved.RequestXml).Root.Element(InvoiceNs + "invoice");
                var date = DateTime.ParseExact(Value(original.Element(InvoiceNs + "invoiceHeader"), "issueDate"), "yyyy-MM-dd", CultureInfo.InvariantCulture);
                // Query the original issue date, then check issuer, identity AND the
                // frozen contents. Not finding an invoice never proves rejection.
                string path = "RequestTransmittedDocs?mark=0&dateFrom=" + Uri.EscapeDataString(date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)) +
                    "&dateTo=" + Uri.EscapeDataString(date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture));
                string continuation = "";
                var seen = new HashSet<string>();
                var matches = new List<XElement>();
                var cancelled = new HashSet<string>();
                for (int page = 0; ; page++)
                {
                    if (page >= 100 || !seen.Add(continuation)) throw new InvalidOperationException("Incomplete or repeating myDATA pagination.");
                    var reply = Send(HttpMethod.Get, path + continuation, null, id);
                    pages.Add(reply.Xml);
                    if (reply.HttpStatus < 200 || reply.HttpStatus >= 300)
                    {
                        reconciliationNotFound = reconciliationRequired &&
                            reply.HttpStatus == 404;
                        if (reconciliationNotFound)
                        {
                            saved.HttpStatus = reply.HttpStatus;
                        }
                        throw new InvalidOperationException("myDATA lookup HTTP " +
                            reply.HttpStatus);
                    }
                    var root = ReadXml(reply.Xml).Root;
                    if (root?.Name != InvoiceNs + "RequestedDoc") throw new InvalidOperationException("Unexpected myDATA lookup document.");
                    foreach (var row in root.Element(InvoiceNs + "invoicesDoc")?.Elements(InvoiceNs + "invoice") ?? Enumerable.Empty<XElement>())
                        if (SameIdentity(original, row)) matches.Add(row);
                    foreach (var row in root.Element(InvoiceNs + "cancelledInvoicesDoc")?.Elements(InvoiceNs + "cancelledInvoice") ?? Enumerable.Empty<XElement>())
                        cancelled.Add(Value(row, "invoiceMark"));
                    var token = root.Element(InvoiceNs + "continuationToken");
                    if (token == null) break;
                    var partition = Value(token, "nextPartitionKey"); var key = Value(token, "nextRowKey");
                    if (string.IsNullOrWhiteSpace(partition) || string.IsNullOrWhiteSpace(key)) throw new InvalidOperationException("Incomplete myDATA continuation token.");
                    continuation = "&nextPartitionKey=" + Uri.EscapeDataString(partition) + "&nextRowKey=" + Uri.EscapeDataString(key);
                }
                if (matches.Count != 1) throw new InvalidOperationException("Expected exactly one matching myDATA invoice; found " + matches.Count + ".");
                var match = matches[0]; long mark;
                var recoveredMark = Value(match,"mark");
                var recoveredUid = Value(match,"uid");
                if (!long.TryParse(recoveredMark, out mark) || mark <= 0 || string.IsNullOrWhiteSpace(recoveredUid) ||
                    cancelled.Contains(recoveredMark) || !string.IsNullOrWhiteSpace(Value(match,"cancelledByMark")) && Value(match,"cancelledByMark") != "0")
                    throw new InvalidOperationException("Lookup lacks an active MARK and UID.");
                if ((!string.IsNullOrWhiteSpace(saved.ReconciliationMark) && saved.ReconciliationMark != recoveredMark) ||
                    (!string.IsNullOrWhiteSpace(saved.ReconciliationUid) && !string.Equals(saved.ReconciliationUid, recoveredUid, StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException("The recovered MARK/UID does not match the AADE error 228 evidence.");
                var originalProjection = Comparable(original);
                var recoveredProjection = Comparable(match);
                if (!XNode.DeepEquals(originalProjection, recoveredProjection))
                    throw new InvalidOperationException("The matching myDATA identity has different invoice contents. Manual reconciliation is required.");
                var recoveredQrCode = Value(match,"qrCodeUrl") ?? "";
                var recoveredAuthenticationCode = Value(match,"authenticationCode") ?? "";
                var evidence = new XElement("ResponseDoc", new XElement("response", new XElement("statusCode","Success"),
                    new XElement("invoiceMark",recoveredMark), new XElement("invoiceUid",recoveredUid),
                    new XElement("qrUrl",recoveredQrCode), new XElement("authenticationCode",recoveredAuthenticationCode)));
                saved.ResponseXml = evidence.ToString(SaveOptions.DisableFormatting);
                saved.State = "Accepted"; saved.CommunicationError = null;
                result = MapResponse(saved.ResponseXml);
            }
            catch (Exception ex)
            {
                saved.State = reconciliationNotFound ? "Rejected" :
                    reconciliationRequired ? "ReconciliationRequired" : "Unknown";
                saved.CommunicationError = ex.GetType().FullName;
                string diagnostic = reconciliationNotFound
                    ? "myDATA recovery returned HTTP 404. The original route has no " +
                      "invoice to recover; a new submission through another configured " +
                      "route is now permitted."
                    : "myDATA reconciliation remains unconfirmed: " + ex.Message +
                      " No new invoice was sent.";
                result = Error(diagnostic);
            }
            saved.RecoveryResponsesXml = pages.ToArray();
            saved.CompletedUtc = DateTimeOffset.UtcNow;
            options.Store.Save(saved);
            return result;
        }
        private static bool SameIdentity(XElement original, XElement candidate)
        {
            var a = original.Element(InvoiceNs + "issuer"); var b = candidate.Element(InvoiceNs + "issuer");
            if (a == null || b == null || Value(a,"vatNumber") != Value(b,"vatNumber") || Value(a,"branch") != Value(b,"branch")) return false;
            a = original.Element(InvoiceNs + "invoiceHeader"); b = candidate.Element(InvoiceNs + "invoiceHeader");
            return a != null && b != null && new[] { "series", "aa", "issueDate", "invoiceType" }.All(field => Value(a,field) == Value(b,field));
        }
        private static XElement Comparable(XElement invoice)
        {
            var comparable = new XElement("invoice");

            var issuer = invoice.Element(InvoiceNs + "issuer");
            var comparableIssuer = CanonicalSection("issuer", issuer);
            comparable.Add(comparableIssuer);

            var counterpart = invoice.Element(InvoiceNs + "counterpart");
            var comparableCounterpart = CanonicalSection("counterpart", counterpart);
            comparable.Add(comparableCounterpart);

            var header = invoice.Element(InvoiceNs + "invoiceHeader");
            var comparableHeader = ComparableHeader(header);
            comparable.Add(comparableHeader);

            var paymentMethods = invoice.Element(InvoiceNs + "paymentMethods");
            var comparablePayments = ComparablePayments(paymentMethods);
            comparable.Add(comparablePayments);

            var lines = invoice.Elements(InvoiceNs + "invoiceDetails").ToArray();
            var comparableLines = lines.Select(ComparableLine)
                .OrderBy(LineNumber)
                .ThenBy(element => element.ToString(SaveOptions.DisableFormatting),
                    StringComparer.Ordinal)
                .ToArray();
            comparable.Add(comparableLines);

            var summary = invoice.Element(InvoiceNs + "invoiceSummary");
            var comparableSummary = ComparableSummary(summary);
            comparable.Add(comparableSummary);

            return comparable;
        }
        private static XElement ComparableHeader(XElement header)
        {
            if (header == null)
            {
                return new XElement("invoiceHeader");
            }

            var fields = header.Elements()
                .Where(element => element.Name.LocalName != "dispatchTime")
                .Where(element => element.Name.LocalName != "vatPaymentSuspension" ||
                    !string.Equals(element.Value.Trim(), "false",
                        StringComparison.OrdinalIgnoreCase))
                .ToArray();
            return CanonicalContainer("invoiceHeader", fields);
        }
        private static XElement ComparablePayments(XElement paymentMethods)
        {
            if (paymentMethods == null)
            {
                return new XElement("paymentMethods");
            }

            var comparableDetails = new List<XElement>();
            foreach (var detail in paymentMethods.Elements(InvoiceNs +
                "paymentMethodDetails"))
            {
                var detailFields = detail.Elements()
                    .Where(element => element.Name.LocalName == "type" ||
                        element.Name.LocalName == "amount")
                    .ToArray();
                var comparableDetail = CanonicalContainer(
                    "paymentMethodDetails", detailFields);
                comparableDetails.Add(comparableDetail);
            }

            var details = comparableDetails
                .OrderBy(element => element.ToString(SaveOptions.DisableFormatting),
                    StringComparer.Ordinal)
                .ToArray();
            return CanonicalContainer("paymentMethods", details);
        }
        private static XElement ComparableLine(XElement line)
        {
            var fields = line.Elements()
                .Where(element => ComparableLineFields.Contains(element.Name.LocalName) ||
                    IsAmountField(element.Name.LocalName))
                .ToArray();
            return CanonicalContainer("invoiceDetails", fields);
        }
        private static XElement ComparableSummary(XElement summary)
        {
            if (summary == null)
            {
                return new XElement("invoiceSummary");
            }

            var fields = summary.Elements()
                .Where(element => IsAmountField(element.Name.LocalName) ||
                    element.Name.LocalName.EndsWith("Classification",
                        StringComparison.Ordinal))
                .ToArray();
            return CanonicalContainer("invoiceSummary", fields);
        }
        private static XElement CanonicalSection(string name, XElement section)
        {
            if (section == null)
            {
                return new XElement(name);
            }

            var fields = section.Elements().ToArray();
            return CanonicalContainer(name, fields);
        }
        private static XElement CanonicalContainer(string name,
            IEnumerable<XElement> fields)
        {
            var canonicalFields = fields.Select(CanonicalElement)
                .OrderBy(element => element.Name.LocalName, StringComparer.Ordinal)
                .ThenBy(element => element.ToString(SaveOptions.DisableFormatting),
                    StringComparer.Ordinal)
                .ToArray();
            return new XElement(name, canonicalFields);
        }
        private static XElement CanonicalElement(XElement source)
        {
            var children = source.Elements().ToArray();
            if (children.Length == 0)
            {
                string normalizedValue = NormalizeValue(source.Name.LocalName,
                    source.Value);
                return new XElement(source.Name.LocalName, normalizedValue);
            }

            return CanonicalContainer(source.Name.LocalName, children);
        }
        private static string NormalizeValue(string fieldName, string value)
        {
            string trimmedValue = (value ?? string.Empty).Trim();
            if (IsDecimalField(fieldName))
            {
                decimal number;
                bool parsed = decimal.TryParse(trimmedValue,
                    NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture, out number);
                if (parsed)
                {
                    return number.ToString("G29", CultureInfo.InvariantCulture);
                }
            }

            bool boolean;
            if (bool.TryParse(trimmedValue, out boolean))
            {
                return boolean ? "true" : "false";
            }

            return trimmedValue;
        }
        private static bool IsAmountField(string fieldName)
        {
            return fieldName.EndsWith("Amount", StringComparison.Ordinal) ||
                fieldName == "netValue" || fieldName == "totalNetValue" ||
                fieldName == "totalGrossValue";
        }
        private static bool IsDecimalField(string fieldName)
        {
            return IsAmountField(fieldName) ||
                fieldName.EndsWith("Quantity", StringComparison.Ordinal) ||
                fieldName == "quantity" || fieldName == "exchangeRate";
        }
        private static int LineNumber(XElement line)
        {
            var numberElement = line.Element("lineNumber");
            int number;
            bool parsed = int.TryParse(numberElement?.Value,
                NumberStyles.Integer, CultureInfo.InvariantCulture, out number);
            return parsed ? number : int.MaxValue;
        }
        private static XDocument ReadXml(string xml)
        {
            using (var reader = XmlReader.Create(new StringReader(xml),new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, IgnoreWhitespace = true, IgnoreComments = true, MaxCharactersInDocument = 32 * 1024 * 1024 }))
                return XDocument.Load(reader);
        }
    }
}
