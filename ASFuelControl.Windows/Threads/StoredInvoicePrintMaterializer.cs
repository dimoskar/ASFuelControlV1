using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using ASFuelControl.Invoicing;
using ASFuelControl.Invoicing.Persistence;
using Newtonsoft.Json;
using QRCoder;
using Telerik.Reporting;
using Telerik.Reporting.Drawing;

namespace ASFuelControl.Windows.Threads
{
    internal sealed class StoredInvoicePrintMaterializer : IFiscalPrintMaterializer
    {
        internal const string StoredFormat = "invoice-report-data/v1";
        internal const string DataFormat = "invoice-print-datasource/v1";
        private readonly SqlFiscalDatabase database;

        internal StoredInvoicePrintMaterializer(SqlFiscalDatabase database)
        {
            this.database = database ?? throw new ArgumentNullException(nameof(database));
        }

        public FiscalPrintMaterialization Materialize(string destination,
            string storedFormat, byte[] storedPayload)
        {
            bool explicitStoredFormat = string.Equals(storedFormat, StoredFormat,
                StringComparison.OrdinalIgnoreCase);
            bool referencePayload = IsStoredReference(storedPayload);
            if (!explicitStoredFormat && !referencePayload)
            {
                return new FiscalPrintMaterialization
                {
                    Format = storedFormat,
                    Document = storedPayload
                };
            }

            string referenceJson = Encoding.UTF8.GetString(storedPayload);
            StoredInvoicePrintReference reference =
                JsonConvert.DeserializeObject<StoredInvoicePrintReference>(referenceJson);
            if (reference == null || reference.Version != 1)
            {
                throw new InvalidOperationException("Invalid stored invoice print reference.");
            }

            var store = new SqlInvoicePrintArchiveStore(database);
            InvoicePrintDataRecord printData = store.LoadPrintData(
                reference.InvoicePrintDataId);
            if (printData == null || printData.ReportDefinitionId !=
                reference.ReportDefinitionId)
            {
                throw new InvalidOperationException(
                    "The stored invoice print data is unavailable or inconsistent.");
            }
            if (!string.Equals(printData.DataFormat, DataFormat,
                StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "The stored invoice print data format is unsupported.");
            }
            string dataSha256 = ComputeSha256(printData.DataPayload);
            if (!string.Equals(dataSha256, printData.DataSha256,
                StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "The stored invoice datasource checksum is invalid.");
            }
            InvoiceReportDefinitionRecord definition = store.LoadDefinition(
                reference.ReportDefinitionId);
            if (definition == null || !string.Equals(definition.DefinitionFormat,
                InvoiceReportDefinitionRegistry.DefinitionFormat,
                StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "The stored invoice report definition is unavailable or unsupported.");
            }
            string definitionSha256 = ComputeSha256(definition.DefinitionPayload);
            if (!string.Equals(definitionSha256, definition.DefinitionSha256,
                StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "The stored invoice report definition checksum is invalid.");
            }

            string dataJson = Encoding.UTF8.GetString(printData.DataPayload);
            StoredInvoicePrintData data =
                JsonConvert.DeserializeObject<StoredInvoicePrintData>(dataJson);
            if (data == null || data.Version != 1 || data.InvoiceId !=
                printData.InvoiceId || !string.Equals(data.ReportKey,
                    definition.ReportKey, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "The stored invoice datasource is unavailable or inconsistent.");
            }
            SharedPrinterProfile profile =
                JsonConvert.DeserializeObject<SharedPrinterProfile>(destination);
            using (Report report = InvoiceReportDefinitionRegistry.Deserialize(
                definition.DefinitionPayload))
            {
                StoredInvoiceReportBinder.Bind(report, data);
                string footer = data.ReportKey ==
                    InvoiceReportDefinitionRegistry.ThermalKey
                    ? null
                    : data.FiscalFooter;
                byte[] document = SharedInvoiceLaserReportRenderer.RenderPages(
                    report, profile, footer);
                return new FiscalPrintMaterialization
                {
                    Format = SharedInvoicePrinting.Format,
                    Document = document
                };
            }
        }

        private static bool IsStoredReference(byte[] payload)
        {
            if (payload == null)
            {
                return false;
            }
            for (int index = 0; index < payload.Length; index++)
            {
                char value = (char)payload[index];
                if (!char.IsWhiteSpace(value))
                {
                    return value == '{';
                }
            }
            return false;
        }

        private static string ComputeSha256(byte[] payload)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(payload);
                return BitConverter.ToString(hash).Replace("-", string.Empty)
                    .ToLowerInvariant();
            }
        }
    }

    internal static class StoredInvoiceReportBinder
    {
        private const string TestQr =
            "http://147.102.24.100/myweb/q1.php?SIG=CFY9900000100010720416CBF73F32A1E087A3D812F0ED212EBF7B7988F124.00";

        internal static void Bind(Report report, StoredInvoicePrintData data)
        {
            if (string.Equals(data.ReportKey,
                InvoiceReportDefinitionRegistry.ThermalKey,
                StringComparison.Ordinal))
            {
                BindThermal(report, data);
                return;
            }
            if (string.Equals(data.ReportKey,
                InvoiceReportDefinitionRegistry.InternalLaserKey,
                StringComparison.Ordinal))
            {
                BindInternal(report, data);
                return;
            }
            if (!string.Equals(data.ReportKey,
                InvoiceReportDefinitionRegistry.StandardLaserKey,
                StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Unknown stored invoice report key.");
            }
            BindStandard(report, data);
        }

        private static void BindInternal(Report report, StoredInvoicePrintData data)
        {
            Data.Invoice invoice = data.RestoreInvoice();
            report.DataSource = invoice.InvoiceLines.ToArray();
            string invoiceId = invoice.InvoiceId.ToString();
            string normalizedInvoiceId = invoiceId.ToLowerInvariant();
            SetParameter(report, "InvoiceId", normalizedInvoiceId);
            SetParameter(report, "CompanyName", data.Company.Name);
            SetParameter(report, "CompanyAddress", data.Company.Address);
            string companyCity = Join(data.Company.PostalCode, data.Company.City);
            SetParameter(report, "CompanyCity", companyCity);
            SetParameter(report, "CompanyVATNumber", data.Company.VatNumber);
            SetParameter(report, "CompanyVATOffice", data.Company.TaxOffice);
            SetParameter(report, "CompanyOccupation", data.Company.Occupation);
            SetParameter(report, "CompanyReferenceNumber",
                data.Company.ReferenceNumber);
            SetParameter(report, "ShowAdditionalData", true);
            SetParameter(report, "SignLine", string.Empty);
            SetParameter(report, "CompanyEfk", data.Company.Efk);
            BindSupplyNumber(report, invoice, "textBox20", "textBox21");
            string fiscalQrValue = EffectiveFiscalQr(data);
            Image fiscalQr = CreateQr(fiscalQrValue, QRCodeGenerator.ECCLevel.L);
            Image documentQr = CreateQr(data.DocumentQr, QRCodeGenerator.ECCLevel.L);
            Image brandQr = CreateQr(data.BrandQr, QRCodeGenerator.ECCLevel.L);
            SetPicture(report, "pictureBox1", fiscalQr);
            SetPicture(report, "pictureBox2", documentQr);
            SetPicture(report, "pictureBox3", brandQr);
        }

        private static void BindStandard(Report report, StoredInvoicePrintData data)
        {
            Data.Invoice invoice = data.RestoreInvoice();
            report.DataSource = invoice.InvoiceLines.ToArray();
            decimal vat = invoice.InvoiceLines.Count == 0 ? 0m :
                invoice.InvoiceLines[0].VatPercentage;
            if (vat == 0m && invoice.VatAmount.GetValueOrDefault() > 0m)
            {
                vat = data.DefaultVat;
            }
            SetParameter(report, "ReplaceParameter", invoice.Notes ?? string.Empty);
            SetParameter(report, "VATValue", vat);
            SetText(report, "companyAddressDataTextBox", data.Company.Address);
            string companyCity = Join(data.Company.PostalCode, data.Company.City);
            SetText(report, "companyCityDataTextBox", companyCity);
            SetText(report, "companyNameDataTextBox", data.Company.Name);
            SetText(report, "companyOccupationDataTextBox",
                data.Company.Occupation);
            SetText(report, "companyPhoneDataTextBox", "Τηλέφωνο: " +
                (data.Company.Phone ?? string.Empty));
            SetText(report, "companyTINDataTextBox", "ΑΦΜ: " +
                (data.Company.VatNumber ?? string.Empty) + " ΔΟΥ: " +
                (data.Company.TaxOffice ?? string.Empty));
            SetText(report, "companyEfkTextBox", "ΑΜΔΙΚΑ / Αρ. Μητρώου: " +
                (data.Company.Efk ?? string.Empty));
            string firstBank = BankText(data.Company.Bank, data.Company.Iban);
            string secondBank = BankText(data.Company.Bank2, data.Company.Iban2);
            SetText(report, "txtBankAccount", firstBank);
            SetText(report, "txtBankAccount2", secondBank);
            Image logo = DecodeImage(data.Company.LogoBase64);
            string fiscalQrValue = EffectiveFiscalQr(data);
            Image fiscalQr = CreateQr(fiscalQrValue, QRCodeGenerator.ECCLevel.L);
            Image documentQr = CreateQr(data.DocumentQr, QRCodeGenerator.ECCLevel.L);
            Image brandQr = CreateQr(data.BrandQr, QRCodeGenerator.ECCLevel.L);
            SetPicture(report, "pictureBox1", logo);
            SetPicture(report, "pictureBox2", fiscalQr);
            SetPicture(report, "pictureBox3", documentQr);
            SetPicture(report, "pictureBox4", brandQr);
            bool hasDelivery = !string.IsNullOrWhiteSpace(invoice.DeliveryAddress);
            SetVisible(report, "textBox61", hasDelivery);
            SetVisible(report, "textBox62", hasDelivery);
            BindSupplyNumber(report, invoice, "textBox57", "textBox58");
            BindDebt(report, invoice);
        }

        private static void BindThermal(Report report, StoredInvoicePrintData data)
        {
            Data.Invoice invoice = data.RestoreInvoice();
            report.DataSource = data.RestoreThermalLines();
            string invoiceId = invoice.InvoiceId.ToString();
            string normalizedInvoiceId = invoiceId.ToLowerInvariant();
            SetParameter(report, "InvoiceId", normalizedInvoiceId);
            SetParameter(report, "PrintBarcode", data.PrintBarcode);
            SetParameter(report, "CompanyName", data.Company.Name);
            SetParameter(report, "Occupation", data.Company.Occupation);
            string companyAddress = Join(data.Company.Address, data.Company.City);
            SetParameter(report, "Address", companyAddress);
            SetParameter(report, "City", data.Company.City);
            SetParameter(report, "VATNumber", data.Company.VatNumber);
            SetParameter(report, "TaxOffice", data.Company.TaxOffice);
            SetParameter(report, "Phone", data.Company.Phone);
            SetParameter(report, "Fax", data.Company.Fax);
            SetParameter(report, "MainAddress", data.Company.MainAddress);
            SetParameter(report, "efk", data.Company.Efk ?? string.Empty);
            ApplyFont(report, data.ThermalFontName, data.ThermalFontSize);
            string transactionDate = invoice.TransactionDate.ToString(
                "dd/MM/yyyy HH:mm");
            decimal nettoAmount = invoice.NettoAmount.GetValueOrDefault();
            decimal vatAmount = invoice.VatAmount.GetValueOrDefault();
            decimal totalAmount = invoice.TotalAmount.GetValueOrDefault();
            string nettoText = nettoAmount.ToString("N2");
            string discountText = invoice.DiscountAmount.ToString("N2");
            string vatText = vatAmount.ToString("N2");
            string totalText = totalAmount.ToString("N2");
            SetText(report, "transactionDateDataTextBox", "Ημερ/νία: " +
                transactionDate);
            SetText(report, "textBoxPreDiscount", nettoText);
            SetText(report, "textBoxDiscount", discountText);
            SetText(report, "textBox15", vatText);
            SetText(report, "textBox6", totalText);
            decimal vat = invoice.InvoiceLines.Count == 0 ? data.DefaultVat :
                invoice.InvoiceLines[0].VatPercentage;
            string vatPercentage = vat.ToString("N1");
            SetText(report, "textBoxVATPercentage", vatPercentage);
            BindSupplyNumber(report, invoice, null, "textBoxVehicleNumberData");
            BindThermalQrCodes(report, data);
            string evidence = "Χώρος Σήμανσης:";
            if (!string.IsNullOrWhiteSpace(invoice.InvoiceSignature))
            {
                evidence += " " + invoice.InvoiceSignature.Trim();
            }
            if (!string.IsNullOrWhiteSpace(data.FiscalFooter))
            {
                evidence += "\r\n" + data.FiscalFooter.Trim();
            }
            SetText(report, "textBox11", evidence);
            FlowThermalFooter(report);
        }

        private static void BindSupplyNumber(Report report, Data.Invoice invoice,
            string labelName, string valueName)
        {
            bool supported = invoice.InvoiceLines.Any(line => line.FuelType != null &&
                line.FuelType.SupportsSupplyNumber == true);
            if (supported)
            {
                if (!string.IsNullOrWhiteSpace(labelName))
                {
                    SetText(report, labelName, "ΑΡ. ΠΑΡ. ΗΛ. ΕΝ.");
                    SetVisible(report, labelName, true);
                }
                SetText(report, valueName, invoice.SupplyNumber);
                SetVisible(report, valueName, true);
                return;
            }
            if (string.Equals(valueName, "textBoxVehicleNumberData",
                StringComparison.Ordinal))
            {
                return;
            }
            if (invoice.VehicleOdometer.GetValueOrDefault() == 0)
            {
                SetVisible(report, labelName, false);
                SetVisible(report, valueName, false);
            }
        }

        private static void BindDebt(Report report, Data.Invoice invoice)
        {
            bool visible = invoice.Trader != null &&
                invoice.Trader.PrintDebtOnInvoice == true;
            foreach (string name in new[] { "textBox52", "textBox53",
                "textBox54", "textBox55" })
            {
                SetVisible(report, name, visible);
            }
            if (!visible)
            {
                return;
            }
            decimal previous = invoice.LastRestAmount.GetValueOrDefault();
            string previousText = previous.ToString("C");
            SetText(report, "textBox54", previousText);
            SetText(report, "textBox55", previousText);
        }

        private static void BindThermalQrCodes(Report report,
            StoredInvoicePrintData data)
        {
            var entries = new List<Tuple<Image, string>>();
            Image fiscal = CreateQr(EffectiveFiscalQr(data),
                QRCodeGenerator.ECCLevel.L);
            if (fiscal != null)
            {
                Tuple<Image, string> entry = Tuple.Create(fiscal,
                    "QR CODE Φορολογικής Σήμανσης");
                entries.Add(entry);
            }
            Image document = CreateQr(data.DocumentQr,
                QRCodeGenerator.ECCLevel.L);
            if (document != null)
            {
                Tuple<Image, string> entry = Tuple.Create(document,
                    "QR Code Παραστατικού");
                entries.Add(entry);
            }
            Image brand = CreateQr(data.BrandQr, QRCodeGenerator.ECCLevel.L);
            if (brand != null)
            {
                Tuple<Image, string> entry = Tuple.Create(brand,
                    "QR CODE Εταιρείας");
                entries.Add(entry);
            }
            string[] pictures = { "pictureBox1", "pictureBox2", "pictureBox3" };
            string[] captions = { "textBox1", "textBox22", "textBox13" };
            for (int index = 0; index < pictures.Length; index++)
            {
                bool visible = index < entries.Count;
                SetPicture(report, pictures[index], visible ? entries[index].Item1 : null);
                SetText(report, captions[index], visible ? entries[index].Item2 : string.Empty);
                SetVisible(report, captions[index], visible);
            }
            ReportSection footer = Find<ReportSection>(report, "pageFooterSection1");
            if (footer != null)
            {
                footer.Height = Unit.Cm(entries.Count == 0 ? 4.30D :
                    entries.Count <= 2 ? 8.05D : 11.567706108093262D);
            }
        }

        private static void FlowThermalFooter(Report report)
        {
            var footer = Find<PageFooterSection>(report, "pageFooterSection1");
            if (footer == null || footer.Items.Count == 0)
            {
                return;
            }
            var items = footer.Items.Cast<ReportItemBase>().ToArray();
            var flowing = new ReportFooterSection
            {
                Name = "storedThermalReportFooter",
                Height = footer.Height
            };
            footer.Items.Clear();
            flowing.Items.AddRange(items);
            report.Items.Add(flowing);
            footer.Height = Unit.Mm(0D);
            footer.Style.Visible = false;
        }

        private static void ApplyFont(Report report, string fontName,
            float fontSize)
        {
            string effectiveName = string.IsNullOrWhiteSpace(fontName)
                ? "Tahoma"
                : fontName;
            float effectiveSize = fontSize < 4f ? 9f : fontSize;
            ReportItemBase[] items = report.Items.Find(typeof(ReportItem), true);
            foreach (ReportItem item in items.OfType<ReportItem>())
            {
                item.Style.Font.Name = effectiveName;
                if (item.Style.Font.Size.Value > 0D)
                {
                    double scaled = item.Style.Font.Size.Value * effectiveSize / 9D;
                    item.Style.Font.Size = Unit.Point(scaled);
                }
            }
        }

        private static string EffectiveFiscalQr(StoredInvoicePrintData data)
        {
            if (!string.IsNullOrWhiteSpace(data.FiscalDeviceQr))
            {
                return data.FiscalDeviceQr;
            }
            return data.PrintTestQr ? TestQr : null;
        }

        private static string BankText(string bank, string iban)
        {
            return string.IsNullOrWhiteSpace(bank) || string.IsNullOrWhiteSpace(iban)
                ? string.Empty
                : bank + " (IBAN) : " + iban;
        }

        private static string Join(string left, string right)
        {
            string[] values = new[] { left, right }
                .Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
            return string.Join(" ", values);
        }

        private static void SetParameter(Report report, string name, object value)
        {
            ReportParameter parameter = report.ReportParameters[name];
            if (parameter != null)
            {
                parameter.Value = value;
            }
        }

        private static void SetText(Report report, string name, object value)
        {
            TextBox textBox = Find<TextBox>(report, name);
            if (textBox != null)
            {
                string text = value == null ? string.Empty :
                    Convert.ToString(value, CultureInfo.CurrentCulture);
                textBox.Value = text;
            }
        }

        private static void SetVisible(Report report, string name, bool visible)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }
            ReportItem item = Find<ReportItem>(report, name);
            if (item != null)
            {
                item.Visible = visible;
            }
        }

        private static void SetPicture(Report report, string name, Image image)
        {
            PictureBox picture = Find<PictureBox>(report, name);
            if (picture != null)
            {
                picture.Value = image;
                picture.Visible = image != null;
                picture.Sizing = ImageSizeMode.ScaleProportional;
            }
        }

        private static T Find<T>(Report report, string name) where T : class
        {
            ReportItemBase[] matches = report.Items.Find(name, true);
            return matches.OfType<T>().FirstOrDefault();
        }

        private static Image DecodeImage(string base64)
        {
            if (string.IsNullOrWhiteSpace(base64))
            {
                return null;
            }
            try
            {
                byte[] bytes = Convert.FromBase64String(base64);
                using (var stream = new MemoryStream(bytes, false))
                using (Image source = Image.FromStream(stream))
                {
                    return new Bitmap(source);
                }
            }
            catch
            {
                return null;
            }
        }

        private static Image CreateQr(string value,
            QRCodeGenerator.ECCLevel level)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }
            var generator = new QRCodeGenerator();
            string normalizedValue = value.Trim();
            QRCodeData data = generator.CreateQrCode(normalizedValue, level);
            var qrCode = new QRCode(data);
            return qrCode.GetGraphic(10);
        }
    }
}
