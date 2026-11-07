using System;
using System.Collections.Generic;
using System.IO;
using ASFuelControl.Invoicing.Persistence;
using Telerik.Reporting;
using Telerik.Reporting.XmlSerialization;

namespace ASFuelControl.Windows.Threads
{
    internal static class InvoiceReportDefinitionRegistry
    {
        internal const string DefinitionFormat = "telerik-report-xml/v1";
        internal const string InternalLaserKey = "invoice-laser-internal/v1";
        internal const string StandardLaserKey = "invoice-laser-standard/v1";
        internal const string ThermalKey = "invoice-thermal/v1";

        internal static IReadOnlyDictionary<string, InvoiceReportDefinitionRecord>
            EnsureCurrent(SqlFiscalDatabase database)
        {
            if (database == null)
            {
                throw new ArgumentNullException(nameof(database));
            }

            var store = new SqlInvoicePrintArchiveStore(database);
            var definitions = new Dictionary<string, InvoiceReportDefinitionRecord>();
            using (var internalReport = new Reports.Invoices.InvoiceReport())
            {
                byte[] definition = Serialize(internalReport);
                InvoiceReportDefinitionRecord record = store.EnsureDefinition(
                    InternalLaserKey, DefinitionFormat, definition);
                definitions.Add(InternalLaserKey, record);
            }
            using (var standardReport = new Reports.Invoices.IncoiceReportShort())
            {
                RemoveRuntimeCompanyData(standardReport);
                byte[] definition = Serialize(standardReport);
                InvoiceReportDefinitionRecord record = store.EnsureDefinition(
                    StandardLaserKey, DefinitionFormat, definition);
                definitions.Add(StandardLaserKey, record);
            }
            using (var thermalReport = new Reports.Invoices.InvoiceReportNarrow())
            {
                thermalReport.ApplyThermalPrintSafety();
                byte[] definition = Serialize(thermalReport);
                InvoiceReportDefinitionRecord record = store.EnsureDefinition(
                    ThermalKey, DefinitionFormat, definition);
                definitions.Add(ThermalKey, record);
            }
            return definitions;
        }

        private static void RemoveRuntimeCompanyData(Report report)
        {
            foreach (string name in new[] { "companyAddressDataTextBox",
                "companyCityDataTextBox", "companyNameDataTextBox",
                "companyOccupationDataTextBox", "companyPhoneDataTextBox",
                "companyTINDataTextBox", "companyEfkTextBox",
                "txtBankAccount", "txtBankAccount2" })
            {
                ReportItemBase[] items = report.Items.Find(name, true);
                foreach (TextBox textBox in items)
                {
                    textBox.Value = string.Empty;
                }
            }
            ReportItemBase[] logos = report.Items.Find("pictureBox1", true);
            foreach (PictureBox logo in logos)
            {
                logo.Value = null;
            }
            report.PageSettings.Watermarks.Clear();
        }

        internal static Report Deserialize(byte[] definition)
        {
            if (definition == null || definition.Length == 0)
            {
                throw new ArgumentException("A report definition is required.",
                    nameof(definition));
            }

            var serializer = new ReportXmlSerializer();
            using (var input = new MemoryStream(definition, false))
            {
                object restored = serializer.Deserialize(input);
                var report = restored as Report;
                if (report == null)
                {
                    throw new InvalidOperationException(
                        "The stored report definition is not a Telerik report.");
                }
                return report;
            }
        }

        private static byte[] Serialize(Report report)
        {
            var serializer = new ReportXmlSerializer();
            using (var output = new MemoryStream())
            {
                serializer.Serialize(output, report);
                return output.ToArray();
            }
        }
    }
}
