using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ASFuelControl.Reports
{
    public class ReportHelper
    {
        public static byte[] GetShiftReportData(Data.Shift shift)
        {
            Data.DatabaseModel database = Data.DatabaseModel.GetContext(shift) as Data.DatabaseModel;
            Data.Implementation.ShiftSalesTotals totals = Data.Implementation.ShiftSalesTotalsCalculator.Calculate(database, shift);
            byte[] reportData = GetShiftReportData(shift, totals);
            return reportData;
        }

        public static byte[] GetShiftReportData(Data.Shift shift, Data.Implementation.ShiftSalesTotals totals)
        {
            string repClass = "ASFuelControl.Reports.ShiftReport";
            Type t1 = typeof(Reports.ShiftReport);
            Telerik.Reporting.Report report = t1.Assembly.CreateInstance(repClass) as Telerik.Reporting.Report;
            Data.DatabaseModel db = Data.DatabaseModel.GetContext(shift) as Data.DatabaseModel;
            Guid invoiceTypeId = Data.Implementation.OptionHandler.Instance.GetGuidOption("LiterCheckInvoiceType", Guid.Empty);
           
            Telerik.Reporting.OpenAccessDataSource openAccessDataSource = new Telerik.Reporting.OpenAccessDataSource(db, "Shifts");

            Reports.ShiftReportFuelType subReport1 = new Reports.ShiftReportFuelType();
            Reports.ShiftReportDispenser subReport2 = new Reports.ShiftReportDispenser();
            Reports.ShiftLiterCheck subReport3 = new Reports.ShiftLiterCheck();

            var q1 = db.SalesTransactions.Where(s => s.ShiftId.HasValue && s.ShiftId == shift.ShiftId);
            var q2 = db.InvoiceLines.Where(s => s.SalesTransaction.ShiftId.HasValue && s.SalesTransaction.ShiftId == shift.ShiftId && s.Invoice.InvoiceTypeId == invoiceTypeId);

            Telerik.Reporting.ObjectDataSource ds1 = new Telerik.Reporting.ObjectDataSource();
            ds1.DataSource = q1;
            ds1.DataMember = "";

            Telerik.Reporting.ObjectDataSource ds2 = new Telerik.Reporting.ObjectDataSource();
            ds2.DataSource = q2;
            ds2.DataMember = "";

            subReport1.DataSource = ds1;
            subReport2.DataSource = ds1;
            subReport3.DataSource = ds2;

            ((Telerik.Reporting.SubReport)report.Items["detail"].Items["subReport1"]).ReportSource = subReport1;
            ((Telerik.Reporting.SubReport)report.Items["detail"].Items["subReport2"]).ReportSource = subReport2;
            ((Telerik.Reporting.SubReport)report.Items["detail"].Items["subReport3"]).ReportSource = subReport3;
            subReport1.ReportParameters[0].Value = shift.ShiftId.ToString();
            subReport2.ReportParameters[0].Value = shift.ShiftId.ToString();
            subReport3.ReportParameters[0].Value = shift.ShiftId.ToString();
            subReport3.ReportParameters[1].Value = invoiceTypeId.ToString().ToLower();

            AddPaymentTotals(report, totals);

            report.DataSource = openAccessDataSource;
            report.ReportParameters[0].Value = shift.ShiftId.ToString();


            Telerik.Reporting.Processing.ReportProcessor reportProcessor = new Telerik.Reporting.Processing.ReportProcessor();
            Telerik.Reporting.InstanceReportSource instanceReportSource = new Telerik.Reporting.InstanceReportSource();
            instanceReportSource.ReportDocument = report;
            Telerik.Reporting.Processing.RenderingResult result = reportProcessor.RenderReport("PDF", instanceReportSource, null);
            byte[] res = result.DocumentBytes;
            report.Dispose();

            return res;
        }

        private static void AddPaymentTotals(Telerik.Reporting.Report report, Data.Implementation.ShiftSalesTotals totals)
        {
            Telerik.Reporting.DetailSection detail = report.Items["detail"] as Telerik.Reporting.DetailSection;
            if (detail == null)
            {
                return;
            }

            StringBuilder textBuilder = new StringBuilder();
            textBuilder.AppendLine("Σύνολα ανά τρόπο πληρωμής");
            foreach (Data.Implementation.ShiftPaymentTotal paymentTotal in totals.PaymentTotals)
            {
                string paymentLine = string.Format("{0}: {1:N2} €", paymentTotal.Description, paymentTotal.Amount);
                textBuilder.AppendLine(paymentLine);
            }

            string grandTotalLine = string.Format("Σύνολο: {0:N2} €", totals.GrandTotal);
            textBuilder.Append(grandTotalLine);

            Telerik.Reporting.TextBox paymentTotalsTextBox = new Telerik.Reporting.TextBox();
            Telerik.Reporting.Drawing.Unit left = Telerik.Reporting.Drawing.Unit.Cm(0D);
            Telerik.Reporting.Drawing.Unit top = Telerik.Reporting.Drawing.Unit.Cm(6.2D);
            paymentTotalsTextBox.Location = new Telerik.Reporting.Drawing.PointU(left, top);
            paymentTotalsTextBox.Name = "paymentTotalsTextBox";
            Telerik.Reporting.Drawing.Unit width = Telerik.Reporting.Drawing.Unit.Cm(15D);
            Telerik.Reporting.Drawing.Unit height = Telerik.Reporting.Drawing.Unit.Cm(3.8D);
            paymentTotalsTextBox.Size = new Telerik.Reporting.Drawing.SizeU(width, height);
            paymentTotalsTextBox.Style.Font.Name = "Verdana";
            paymentTotalsTextBox.Style.Font.Size = Telerik.Reporting.Drawing.Unit.Point(9D);
            paymentTotalsTextBox.Style.Font.Bold = true;
            paymentTotalsTextBox.Value = textBuilder.ToString();

            detail.Height = Telerik.Reporting.Drawing.Unit.Cm(10.2D);
            detail.Items.Add(paymentTotalsTextBox);
        }
    }
}
