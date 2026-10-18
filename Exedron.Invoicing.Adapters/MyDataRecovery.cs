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
        private InvoiceResponse Reconcile(MyDataSubmission saved, Guid id)
        {
            var pages = new List<string>();
            InvoiceResponse result;
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
                    if (reply.HttpStatus < 200 || reply.HttpStatus >= 300) throw new InvalidOperationException("myDATA lookup HTTP " + reply.HttpStatus);
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
                if (!long.TryParse(Value(match,"mark"), out mark) || mark <= 0 || string.IsNullOrWhiteSpace(Value(match,"uid")) ||
                    cancelled.Contains(Value(match,"mark")) || !string.IsNullOrWhiteSpace(Value(match,"cancelledByMark")) && Value(match,"cancelledByMark") != "0")
                    throw new InvalidOperationException("Lookup lacks an active MARK and UID.");
                if (!XNode.DeepEquals(Comparable(original), Comparable(match)))
                    throw new InvalidOperationException("The matching myDATA identity has different invoice contents. Manual reconciliation is required.");
                var evidence = new XElement("ResponseDoc", new XElement("response", new XElement("statusCode","Success"),
                    new XElement("invoiceMark",Value(match,"mark")), new XElement("invoiceUid",Value(match,"uid")), new XElement("qrUrl",Value(match,"qrCodeUrl") ?? "")));
                saved.ResponseXml = evidence.ToString(SaveOptions.DisableFormatting);
                saved.State = "Accepted"; saved.CommunicationError = null;
                result = ParseInvoiceResponse(saved.ResponseXml);
            }
            catch (Exception ex)
            {
                saved.State = "Unknown";
                saved.CommunicationError = ex.GetType().FullName;
                result = Error("myDATA reconciliation remains unconfirmed: " + ex.Message + " No new invoice was sent.");
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
            var clone = new XElement(invoice);
            foreach (var name in new[] { "uid", "mark", "cancelledByMark", "authenticationCode", "qrCodeUrl" }) clone.Elements(InvoiceNs + name).Remove();
            // XML namespace prefixes and decimal formatting are not business values.
            foreach (var attribute in clone.DescendantsAndSelf().Attributes().Where(a => a.IsNamespaceDeclaration).ToArray()) attribute.Remove();
            var decimalFields = new HashSet<string>(new[] { "netValue", "vatAmount", "totalNetValue", "totalVatAmount", "totalGrossValue", "totalWithheldAmount", "totalFeesAmount", "totalStampDutyAmount", "totalOtherTaxesAmount", "totalDeductionsAmount", "quantity", "amount" });
            foreach (var leaf in clone.Descendants().Where(e => !e.HasElements && decimalFields.Contains(e.Name.LocalName)))
            {
                decimal number;
                if (decimal.TryParse(leaf.Value, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out number))
                    leaf.Value = number.ToString("G29",CultureInfo.InvariantCulture);
            }
            return clone;
        }
        private static XDocument ReadXml(string xml)
        {
            using (var reader = XmlReader.Create(new StringReader(xml),new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, IgnoreWhitespace = true, IgnoreComments = true, MaxCharactersInDocument = 32 * 1024 * 1024 }))
                return XDocument.Load(reader);
        }
    }
}
