using System;
using System.Linq;
using ASFuelControl.Invoicing;
using Exedron.Invoicing.Core;
using Exedron.Invoicing.Core.Models;
using Newtonsoft.Json;
using Telerik.Reporting;

namespace ASFuelControl.Windows.Threads
{
    /// <summary>
    /// Freezes the established full Telerik reports for balances and tank
    /// movements. These pages are retained by FiscalPrintService for safe retry.
    /// </summary>
    internal static class SharedDocumentReportRenderer
    {
        internal static byte[] Render(InvoiceWorkflowSnapshot snapshot, InvoiceResponse response)
        {
            var profile = JsonConvert.DeserializeObject<SharedPrinterProfile>(snapshot.PrintDestination);
            if (profile == null || profile.Thermal)
                throw new InvalidOperationException("Τα ισοζύγια και οι παραλαβές απαιτούν πλήρη αναφορά A4.");

            switch (snapshot.Signing.Document.Kind)
            {
                case FiscalDocumentKind.Balance:
                    return RenderBalance(snapshot,response,profile);
                case FiscalDocumentKind.TankFilling:
                    return RenderTankFilling(snapshot,response,profile);
                default:
                    throw new NotSupportedException("Δεν υπάρχει πλήρης αναφορά για αυτό το έγγραφο.");
            }
        }

        private static byte[] RenderBalance(InvoiceWorkflowSnapshot snapshot, InvoiceResponse response, SharedPrinterProfile profile)
        {
            using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                var balance = db.Balances.Single(item => item.BalanceId == snapshot.InvoiceId);
                var load = new Reports.BalanceReports.BalanceLoad();
                load.LoadBalance(balance.BalanceId,balance.BalanceText);
                load.Model.Balance[0].Sign = response == null ? balance.DocumentSign : response.InvoiceSignature;

                using (var report = new Reports.BalanceReport())
                using (var tanks = new Reports.BalanceReports.TankBalanceReport())
                using (var pumps = new Reports.BalanceReports.PumpBalanceReport())
                using (var divergence = new Reports.BalanceReports.DivergenceBalanceReport())
                using (var fillings = new Reports.BalanceReports.TankFillingBalanceReport())
                using (var tankData = Source(load.Model,"TankData"))
                using (var pumpData = Source(load.Model,"DispenserData"))
                using (var fuelData = Source(load.Model,"FuelTypeData"))
                using (var balanceData = Source(load.Model,"Balance"))
                using (var fillingData = Source(load.Model,"TankFillingData"))
                {
                    tanks.DataSource = tankData;
                    pumps.DataSource = pumpData;
                    divergence.DataSource = fuelData;
                    fillings.DataSource = fillingData;
                    report.DataSource = balanceData;
                    report.Name = "BalanceReport";
                    report.DocumentName = "BalanceReport";
                    SetSubReport(report,"subReport1",tanks);
                    SetSubReport(report,"subReport2",pumps);
                    SetSubReport(report,"subReport3",divergence);
                    SetSubReport(report,"subReport4",fillings);
                    return SharedInvoiceLaserReportRenderer.RenderPages(report,profile,null);
                }
            }
        }

        private static byte[] RenderTankFilling(InvoiceWorkflowSnapshot snapshot, InvoiceResponse response, SharedPrinterProfile profile)
        {
            using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
            using (var report = new Reports.Invoices.TankFillingReport())
            using (var reportData = new OpenAccessDataSource(db,"InvoiceLines"))
            {
                var filling = db.TankFillings.Single(item => item.TankFillingId == snapshot.InvoiceId);
                var line = filling.InvoiceLines.FirstOrDefault();
                if (line == null)
                    throw new InvalidOperationException("Η παραλαβή / εξαγωγή δεν έχει γραμμές για εκτύπωση.");

                report.DataSource = reportData;
                report.ReportParameters[0].Value = filling.TankFillingId.ToString();
                string description = line.Invoice.InvoiceType.InternalDeliveryDescription;
                report.ReportParameters[1].Value = !string.IsNullOrWhiteSpace(description)
                    ? description
                    : filling.LevelStart > filling.LevelEnd ? "Εξαγωγή Καυσίμου" : "Παραλαβή Καυσίμου";
                report.SetSignature(response == null ? filling.SignSignature : response.InvoiceSignature);
                return SharedInvoiceLaserReportRenderer.RenderPages(report,profile,null);
            }
        }

        private static ObjectDataSource Source(object value, string member)
        {
            return new ObjectDataSource { DataSource = value, DataMember = member };
        }

        private static void SetSubReport(Reports.BalanceReport report, string name, Telerik.Reporting.Report document)
        {
            ((SubReport)report.Items["detail"].Items[name]).ReportSource =
                new InstanceReportSource { ReportDocument = document };
        }
    }
}
