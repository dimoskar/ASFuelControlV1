using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Exedron.Invoicing.Core;
using Newtonsoft.Json.Linq;

namespace Exedron.Invoicing.Adapters
{
    public sealed class SemanticInvoicePayloadComparer : IInvoicePayloadComparer
    {
        public InvoicePayloadComparison Compare(string previousPayload,
            string currentPayload, string payloadFormat)
        {
            if (string.IsNullOrWhiteSpace(previousPayload) ||
                string.IsNullOrWhiteSpace(currentPayload))
            {
                return new InvoicePayloadComparison
                {
                    Kind = InvoicePayloadComparisonKind.Unknown,
                    ChangedPaths = new string[0]
                };
            }

            bool xml = IsXml(payloadFormat, previousPayload,
                currentPayload);
            return xml
                ? CompareXml(previousPayload, currentPayload)
                : CompareJson(previousPayload, currentPayload);
        }

        public static bool AreEquivalentJson(string previousPayload,
            string currentPayload)
        {
            InvoicePayloadComparison comparison = CompareJson(
                previousPayload, currentPayload);
            return comparison.Kind ==
                InvoicePayloadComparisonKind.Equivalent;
        }

        public static bool AreEquivalentXml(string previousPayload,
            string currentPayload)
        {
            InvoicePayloadComparison comparison = CompareXml(
                previousPayload, currentPayload);
            return comparison.Kind ==
                InvoicePayloadComparisonKind.Equivalent;
        }

        private static InvoicePayloadComparison CompareJson(
            string previousPayload, string currentPayload)
        {
            JToken previous = JToken.Parse(previousPayload);
            JToken current = JToken.Parse(currentPayload);
            string previousIdentity = JsonIdentity(previous);
            string currentIdentity = JsonIdentity(current);
            var differences = new List<string>();
            CompareTokens(previous, current, "$", differences);
            bool identityChanged = IdentityChanged(previousIdentity,
                currentIdentity);
            if (identityChanged && differences.Count == 0)
            {
                differences.Add("$.identity");
            }
            InvoicePayloadComparisonKind kind = identityChanged
                ? InvoicePayloadComparisonKind.IdentityChanged
                : differences.Count == 0
                    ? InvoicePayloadComparisonKind.Equivalent
                    : InvoicePayloadComparisonKind.Changed;
            return new InvoicePayloadComparison
            {
                Kind = kind,
                ChangedPaths = differences.Distinct(
                    StringComparer.Ordinal).Take(50).ToArray()
            };
        }

        private static InvoicePayloadComparison CompareXml(
            string previousPayload, string currentPayload)
        {
            XDocument previousDocument = XDocument.Parse(previousPayload,
                LoadOptions.None);
            XDocument currentDocument = XDocument.Parse(currentPayload,
                LoadOptions.None);
            string previousIdentity = MyDataIdentity(previousDocument);
            string currentIdentity = MyDataIdentity(currentDocument);
            JToken previous = XmlToken(previousDocument.Root);
            JToken current = XmlToken(currentDocument.Root);
            var differences = new List<string>();
            CompareTokens(previous, current, "$", differences);
            bool identityChanged = IdentityChanged(previousIdentity,
                currentIdentity);
            if (identityChanged && differences.Count == 0)
            {
                differences.Add("$.identity");
            }
            InvoicePayloadComparisonKind kind = identityChanged
                ? InvoicePayloadComparisonKind.IdentityChanged
                : differences.Count == 0
                    ? InvoicePayloadComparisonKind.Equivalent
                    : InvoicePayloadComparisonKind.Changed;
            return new InvoicePayloadComparison
            {
                Kind = kind,
                ChangedPaths = differences.Distinct(
                    StringComparer.Ordinal).Take(50).ToArray()
            };
        }

        private static bool IsXml(string payloadFormat,
            string previousPayload, string currentPayload)
        {
            if (!string.IsNullOrWhiteSpace(payloadFormat) &&
                payloadFormat.IndexOf("xml",
                    StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            return previousPayload.TrimStart().StartsWith("<",
                       StringComparison.Ordinal) &&
                   currentPayload.TrimStart().StartsWith("<",
                       StringComparison.Ordinal);
        }

        private static void CompareTokens(JToken previous, JToken current,
            string path, IList<string> differences)
        {
            if (EquivalentEmpty(previous, current))
            {
                return;
            }
            if (previous == null || current == null)
            {
                differences.Add(path);
                return;
            }

            JObject previousObject = previous as JObject;
            JObject currentObject = current as JObject;
            if (previousObject != null && currentObject != null)
            {
                CompareObjects(previousObject, currentObject, path,
                    differences);
                return;
            }

            JArray previousArray = previous as JArray;
            JArray currentArray = current as JArray;
            if (previousArray != null && currentArray != null)
            {
                CompareArrays(previousArray, currentArray, path,
                    differences);
                return;
            }

            string previousValue = NormalizeScalar(previous);
            string currentValue = NormalizeScalar(current);
            if (!string.Equals(previousValue, currentValue,
                StringComparison.Ordinal))
            {
                if (!EquivalentEmbeddedXml(previousValue, currentValue))
                {
                    differences.Add(path);
                }
            }
        }

        private static void CompareObjects(JObject previous, JObject current,
            string path, IList<string> differences)
        {
            var previousProperties = previous.Properties().ToDictionary(
                property => property.Name, property => property.Value,
                StringComparer.OrdinalIgnoreCase);
            var currentProperties = current.Properties().ToDictionary(
                property => property.Name, property => property.Value,
                StringComparer.OrdinalIgnoreCase);
            string[] sharedNames = previousProperties.Keys.Intersect(
                currentProperties.Keys, StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            bool previousHasValues = previousProperties.Values.Any(value =>
                !IsEmpty(value));
            bool currentHasValues = currentProperties.Values.Any(value =>
                !IsEmpty(value));
            if (sharedNames.Length == 0 && previousHasValues &&
                currentHasValues)
            {
                differences.Add(path + ".common");
                return;
            }
            foreach (string name in sharedNames)
            {
                JToken previousValue = previousProperties[name];
                JToken currentValue = currentProperties[name];
                string childPath = path + "." + name;
                CompareTokens(previousValue, currentValue, childPath,
                    differences);
            }
        }

        private static void CompareArrays(JArray previous, JArray current,
            string path, IList<string> differences)
        {
            JToken[] previousValues = previous.Where(value =>
                !IsEmpty(value)).ToArray();
            JToken[] currentValues = current.Where(value =>
                !IsEmpty(value)).ToArray();
            if (previousValues.Length != currentValues.Length)
            {
                differences.Add(path + ".count");
                return;
            }

            var unmatched = new List<JToken>(currentValues);
            foreach (JToken previousValue in previousValues)
            {
                int matchIndex = -1;
                for (int index = 0; index < unmatched.Count; index++)
                {
                    var candidateDifferences = new List<string>();
                    CompareTokens(previousValue, unmatched[index], path,
                        candidateDifferences);
                    if (candidateDifferences.Count == 0)
                    {
                        matchIndex = index;
                        break;
                    }
                }

                if (matchIndex < 0)
                {
                    differences.Add(path);
                    return;
                }
                unmatched.RemoveAt(matchIndex);
            }
        }

        private static bool EquivalentEmpty(JToken previous, JToken current)
        {
            return IsEmpty(previous) && IsEmpty(current);
        }

        private static bool IsEmpty(JToken value)
        {
            if (value == null || value.Type == JTokenType.Null ||
                value.Type == JTokenType.Undefined)
            {
                return true;
            }
            JValue scalar = value as JValue;
            if (scalar != null && scalar.Type == JTokenType.String)
            {
                string text = Convert.ToString(scalar.Value,
                    CultureInfo.InvariantCulture);
                return string.IsNullOrWhiteSpace(text);
            }
            return false;
        }

        private static string NormalizeScalar(JToken token)
        {
            if (IsEmpty(token))
            {
                return string.Empty;
            }

            JValue value = token as JValue;
            object raw = value == null ? null : value.Value;
            if (raw is bool)
            {
                return (bool)raw ? "true" : "false";
            }
            if (raw is decimal || raw is double || raw is float ||
                raw is int || raw is long || raw is short || raw is byte)
            {
                decimal number = Convert.ToDecimal(raw,
                    CultureInfo.InvariantCulture);
                return number.ToString("G29", CultureInfo.InvariantCulture);
            }

            string text = Convert.ToString(raw,
                CultureInfo.InvariantCulture);
            text = (text ?? string.Empty).Trim();
            decimal parsedNumber;
            bool numberParsed = decimal.TryParse(text,
                NumberStyles.AllowLeadingSign |
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out parsedNumber);
            if (numberParsed)
            {
                return parsedNumber.ToString("G29",
                    CultureInfo.InvariantCulture);
            }
            bool boolean;
            if (bool.TryParse(text, out boolean))
            {
                return boolean ? "true" : "false";
            }
            DateTimeOffset dateTime = default(DateTimeOffset);
            bool dateTimeParsed = text.IndexOf('T') > 0 &&
                DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture,
                    DateTimeStyles.AllowWhiteSpaces, out dateTime);
            if (dateTimeParsed)
            {
                return dateTime.ToUniversalTime().ToString("O",
                    CultureInfo.InvariantCulture);
            }
            DateTime date = default(DateTime);
            bool dateParsed = text.Length == 10 &&
                DateTime.TryParseExact(text, "yyyy-MM-dd",
                    CultureInfo.InvariantCulture, DateTimeStyles.None,
                    out date);
            if (dateParsed)
            {
                return date.ToString("yyyy-MM-dd",
                    CultureInfo.InvariantCulture);
            }
            return text;
        }

        private static bool EquivalentEmbeddedXml(string previous,
            string current)
        {
            string previousXml;
            string currentXml;
            bool previousDecoded = TryDecodeXml(previous, out previousXml);
            bool currentDecoded = TryDecodeXml(current, out currentXml);
            if (!previousDecoded || !currentDecoded)
            {
                return false;
            }

            InvoicePayloadComparison comparison = CompareXml(previousXml,
                currentXml);
            return comparison.Kind ==
                InvoicePayloadComparisonKind.Equivalent;
        }

        private static bool TryDecodeXml(string value, out string xml)
        {
            xml = null;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }
            try
            {
                byte[] bytes = Convert.FromBase64String(value);
                string decoded = Encoding.UTF8.GetString(bytes).Trim();
                if (!decoded.StartsWith("<", StringComparison.Ordinal))
                {
                    return false;
                }
                XDocument document = XDocument.Parse(decoded,
                    LoadOptions.None);
                xml = document.ToString(SaveOptions.DisableFormatting);
                return true;
            }
            catch (Exception exception) when (exception is FormatException ||
                exception is System.Xml.XmlException)
            {
                return false;
            }
        }

        private static JToken XmlToken(XElement element)
        {
            if (element == null)
            {
                return JValue.CreateNull();
            }
            XElement[] children = element.Elements().ToArray();
            if (children.Length == 0)
            {
                return new JValue(element.Value);
            }

            var result = new JObject();
            foreach (IGrouping<string, XElement> group in children.GroupBy(
                child => child.Name.LocalName, StringComparer.Ordinal))
            {
                XElement[] elements = group.ToArray();
                if (elements.Length == 1)
                {
                    JToken childToken = XmlToken(elements[0]);
                    result[group.Key] = childToken;
                }
                else
                {
                    var values = new JArray();
                    foreach (XElement child in elements)
                    {
                        JToken childToken = XmlToken(child);
                        values.Add(childToken);
                    }
                    result[group.Key] = values;
                }
            }
            return result;
        }

        private static string JsonIdentity(JToken token)
        {
            JObject root = token as JObject;
            if (root == null)
            {
                return null;
            }
            foreach (string name in new[]
            {
                "ext_system_id", "externalSystemId", "invoiceId",
                "invoice_id", "invoiceNumber"
            })
            {
                JToken identityToken = root.GetValue(name,
                    StringComparison.OrdinalIgnoreCase);
                string identity = NormalizeScalar(identityToken);
                if (!string.IsNullOrWhiteSpace(identity))
                {
                    return name.ToLowerInvariant() + "=" + identity;
                }
            }
            return null;
        }

        private static string MyDataIdentity(XDocument document)
        {
            XElement invoice = document.Descendants().FirstOrDefault(
                element => element.Name.LocalName == "invoice");
            if (invoice == null)
            {
                return null;
            }
            XElement issuer = invoice.Elements().FirstOrDefault(
                element => element.Name.LocalName == "issuer");
            XElement header = invoice.Elements().FirstOrDefault(
                element => element.Name.LocalName == "invoiceHeader");
            if (issuer == null || header == null)
            {
                return null;
            }
            string[] values =
            {
                XmlValue(issuer, "vatNumber"),
                XmlValue(header, "issueDate"),
                XmlValue(issuer, "branch"),
                XmlValue(header, "invoiceType"),
                XmlValue(header, "series"),
                XmlValue(header, "aa"),
                XmlValue(header, "invoiceVariationType")
            };
            IEnumerable<string> identityValues = values.Select(value =>
                value ?? string.Empty);
            return string.Join("|", identityValues);
        }

        private static string XmlValue(XElement parent, string name)
        {
            XElement element = parent.Elements().FirstOrDefault(value =>
                value.Name.LocalName == name);
            if (element == null)
            {
                return null;
            }
            return element.Value.Trim();
        }

        private static bool IdentityChanged(string previous,
            string current)
        {
            return !string.IsNullOrWhiteSpace(previous) &&
                !string.IsNullOrWhiteSpace(current) &&
                !string.Equals(previous, current,
                    StringComparison.OrdinalIgnoreCase);
        }
    }
}
