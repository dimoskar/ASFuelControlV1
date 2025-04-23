using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Telerik.WinControls.UI;

namespace ASFuelControl.Reports
{
    public partial class ReportViewerForm : RadForm
    {
        Data.DatabaseModel db;
        List<IDisposable> disposableObject = new List<IDisposable>();

        public string ReportType
        {
            set;
            get;
        }

        public string DBConnectionString
        {
            set;
            get;
        }

        public ReportViewerForm(string connString)
        {
            this.DBConnectionString = connString;
            InitializeComponent();
            this.db = new Data.DatabaseModel(connString);
            this.FormClosed += new FormClosedEventHandler(ReportViewerForm_FormClosed);
        }

        void ReportViewerForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            foreach (IDisposable disp in this.disposableObject)
                disp.Dispose();
            this.db.Dispose();
            this.reportViewer1.Dispose();
        }

        public void InvoicePerDayReport()
        {
            using (SelectFromToForm sftf = new SelectFromToForm())
            {
                TimeSpan tc = Data.Implementation.OptionHandler.Instance.GetTimeSpanOption("DayCloseHour", TimeSpan.ParseExact("01:00:00:00", "g", System.Globalization.CultureInfo.CurrentCulture));
                sftf.StartDate = DateTime.Today.AddDays(-1).Add(tc);
                sftf.EndDate = DateTime.Today.Add(tc);
                DialogResult res = sftf.ShowDialog(this);
                if (res != DialogResult.OK)
                    return;

                DateTime dtFrom = sftf.StartDate.Value;
                DateTime dtTo = sftf.EndDate.Value;

                try
                {
                    Reports.Invoices.InvoicePerDayReport report = new Reports.Invoices.InvoicePerDayReport();
                    Reports.Invoices.InvoicePeriodFuelSum subReport1 = new Reports.Invoices.InvoicePeriodFuelSum();
                    Reports.Invoices.InvoicePeriodInvoiceTypeSum subReport2 = new Reports.Invoices.InvoicePeriodInvoiceTypeSum();


                    List<Data.InvoiceLine> invoiceLines = db.InvoiceLines.Where(i => i.Invoice.TransactionDate.Date >= dtFrom && i.Invoice.TransactionDate.Date <= dtTo &&
                        i.Invoice.InvoiceType.TransactionType == 0 && (!i.Invoice.InvoiceType.IsInternal.HasValue || !i.Invoice.InvoiceType.IsInternal.Value) &&
                        (!i.Invoice.IsEuromat.HasValue || !i.Invoice.IsEuromat.Value)).ToList();

                    invoiceLines = invoiceLines.Where(i => !i.Invoice.IsReplaced).ToList();

                    Telerik.Reporting.ObjectDataSource ds1 = new Telerik.Reporting.ObjectDataSource();
                    ds1.DataSource = invoiceLines.OrderBy(i => i.Invoice.TransactionDate).ThenBy(i => i.Invoice.InvoiceType.Description);
                    ds1.DataMember = "";

                    report.DataSource = ds1;
                    subReport1.DataSource = ds1;
                    subReport2.DataSource = ds1;

                    ((Telerik.Reporting.SubReport)report.Items["reportFooter"].Items["subReport1"]).ReportSource = subReport1;
                    ((Telerik.Reporting.SubReport)report.Items["reportFooter"].Items["subReport2"]).ReportSource = subReport2;

                    report.ReportParameters[0].Value = dtFrom;
                    report.ReportParameters[1].Value = dtTo;

                    this.reportViewer1.ReportSource = report;
                    this.reportViewer1.RefreshReport();

                    disposableObject.Add(report);
                    disposableObject.Add(ds1);
                    disposableObject.Add(subReport1);
                    disposableObject.Add(subReport2);
                }
                catch
                {
                }
            }
        }

        public void ShiftReport()
        {
            using (SelectShift ssf = new SelectShift(this.DBConnectionString))
            {
                DialogResult res = ssf.ShowDialog(this);
                if (res == DialogResult.Cancel)
                    return;
                if (ssf.SelectedShift == Guid.Empty)
                    return;
                Guid invoiceTypeId = Data.Implementation.OptionHandler.Instance.GetGuidOption("LiterCheckInvoiceType", Guid.Empty);
                try
                {
                    Reports.ShiftReport report = new Reports.ShiftReport();
                    //report.DataSource = 



                    Telerik.Reporting.OpenAccessDataSource openAccessDataSource = new Telerik.Reporting.OpenAccessDataSource(db, "Shifts");

                    Reports.ShiftReportFuelType subReport1 = new Reports.ShiftReportFuelType();
                    Reports.ShiftReportDispenser subReport2 = new Reports.ShiftReportDispenser();
                    Reports.ShiftLiterCheck subReport3 = new Reports.ShiftLiterCheck();

                    var q1 = db.SalesTransactions.Where(s => s.ShiftId.HasValue && s.ShiftId == ssf.SelectedShift);
                    var q2 = db.InvoiceLines.Where(s => s.SalesTransaction.ShiftId.HasValue && s.SalesTransaction.ShiftId == ssf.SelectedShift && s.Invoice.InvoiceTypeId == invoiceTypeId);

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
                    subReport1.ReportParameters[0].Value = ssf.SelectedShift.ToString();
                    subReport2.ReportParameters[0].Value = ssf.SelectedShift.ToString();
                    subReport3.ReportParameters[0].Value = ssf.SelectedShift.ToString();
                    subReport3.ReportParameters[1].Value = invoiceTypeId.ToString().ToLower();

                    report.DataSource = openAccessDataSource;

                    //((Telerik.Reporting.OpenAccessDataSource)report.DataSource).ConnectionString = Properties.Settings.Default.DBConnection;
                    report.ReportParameters[0].Value = ssf.SelectedShift.ToString();

                    this.reportViewer1.ReportSource = report;
                    this.reportViewer1.RefreshReport();

                    disposableObject.Add(report);
                    disposableObject.Add(ds1);
                    disposableObject.Add(ds2);
                    disposableObject.Add(subReport1);
                    disposableObject.Add(subReport2);
                    disposableObject.Add(subReport3);
                }
                catch
                {
                }
            }
        }

        public void BalanceReport()
        {
            using (SelectBalanceForm ssf = new SelectBalanceForm(this.DBConnectionString))
            {
                DialogResult res = ssf.ShowDialog(this);
                if (res == DialogResult.Cancel)
                    return;
                if (ssf.SelectedBalance == Guid.Empty)
                    return;
                try
                {
                    Data.Balance balance = db.Balances.Where(b => b.BalanceId == ssf.SelectedBalance).FirstOrDefault();
                    Reports.BalanceReports.BalanceLoad bl = new Reports.BalanceReports.BalanceLoad();
                    bl.LoadBalance(ssf.SelectedBalance, balance.BalanceText);
                    bl.Model.Balance[0].Sign = balance.DocumentSign;
                    Reports.BalanceReport report = new Reports.BalanceReport();
                    Reports.BalanceReports.TankBalanceReport subReport1 = new Reports.BalanceReports.TankBalanceReport();
                    Reports.BalanceReports.PumpBalanceReport subReport2 = new Reports.BalanceReports.PumpBalanceReport();
                    Reports.BalanceReports.DivergenceBalanceReport subReport3 = new Reports.BalanceReports.DivergenceBalanceReport();
                    Reports.BalanceReports.TankFillingBalanceReport subReport4 = new Reports.BalanceReports.TankFillingBalanceReport();

                    Telerik.Reporting.ObjectDataSource ds1 = new Telerik.Reporting.ObjectDataSource();
                    ds1.DataSource = bl.Model;
                    ds1.DataMember = "TankData";

                    Telerik.Reporting.ObjectDataSource ds2 = new Telerik.Reporting.ObjectDataSource();
                    ds2.DataSource = bl.Model;
                    ds2.DataMember = "DispenserData";

                    Telerik.Reporting.ObjectDataSource ds3 = new Telerik.Reporting.ObjectDataSource();
                    ds3.DataSource = bl.Model;
                    ds3.DataMember = "FuelTypeData";

                    Telerik.Reporting.ObjectDataSource ds4 = new Telerik.Reporting.ObjectDataSource();
                    ds4.DataSource = bl.Model;
                    ds4.DataMember = "Balance";

                    Telerik.Reporting.ObjectDataSource ds5 = new Telerik.Reporting.ObjectDataSource();
                    ds5.DataSource = bl.Model;
                    ds5.DataMember = "TankFillingData";

                    subReport1.DataSource = ds1;
                    subReport2.DataSource = ds2;
                    subReport3.DataSource = ds3;
                    subReport4.DataSource = ds5;
                    report.DataSource = ds4;

                    Telerik.Reporting.InstanceReportSource subReportSource1 = new Telerik.Reporting.InstanceReportSource();
                    subReportSource1.ReportDocument = subReport1;

                    Telerik.Reporting.InstanceReportSource subReportSource2 = new Telerik.Reporting.InstanceReportSource();
                    subReportSource2.ReportDocument = subReport2;

                    Telerik.Reporting.InstanceReportSource subReportSource3 = new Telerik.Reporting.InstanceReportSource();
                    subReportSource3.ReportDocument = subReport3;

                    Telerik.Reporting.InstanceReportSource subReportSource4 = new Telerik.Reporting.InstanceReportSource();
                    subReportSource4.ReportDocument = subReport4;

                    //Telerik.Reporting.InstanceReportSource repSource = new Telerik.Reporting.InstanceReportSource();
                    //repSource.ReportDocument = report;

                    ((Telerik.Reporting.SubReport)report.Items["detail"].Items["subReport1"]).ReportSource = subReportSource1;
                    ((Telerik.Reporting.SubReport)report.Items["detail"].Items["subReport2"]).ReportSource = subReportSource2;
                    ((Telerik.Reporting.SubReport)report.Items["detail"].Items["subReport3"]).ReportSource = subReportSource3;
                    ((Telerik.Reporting.SubReport)report.Items["detail"].Items["subReport4"]).ReportSource = subReportSource4;

                    Telerik.Reporting.InstanceReportSource reportSource = new Telerik.Reporting.InstanceReportSource();
                    reportSource.ReportDocument = report;

                    this.reportViewer1.ReportSource = reportSource;
                    this.reportViewer1.RefreshReport();

                    disposableObject.Add(report);
                    disposableObject.Add(ds1);
                    disposableObject.Add(ds2);
                    disposableObject.Add(ds3);
                    disposableObject.Add(ds4);
                    disposableObject.Add(ds5);
                    disposableObject.Add(subReport1);
                    disposableObject.Add(subReport2);
                    disposableObject.Add(subReport3);
                    disposableObject.Add(subReport4);

                }
                catch
                {

                }
            }
        }

        public void InvoiceReport()
        {
            using (SelectFromToForm sftf = new SelectFromToForm())
            {
                TimeSpan tc = Data.Implementation.OptionHandler.Instance.GetTimeSpanOption("DayCloseHour", TimeSpan.ParseExact("01:00:00:00", "g", System.Globalization.CultureInfo.CurrentCulture));
                sftf.StartDate = DateTime.Today.AddDays(-1).Add(tc);
                sftf.EndDate = DateTime.Today.Add(tc);

                DialogResult res = sftf.ShowDialog(this);
                if (res != DialogResult.OK)
                    return;

                DateTime dtFrom = sftf.StartDate.Value;
                DateTime dtTo = sftf.EndDate.Value;

                try
                {

                    Reports.InvoiceReport report = new Reports.InvoiceReport();

                    List<Data.InvoiceGroupView> invoiceLines = db.InvoiceGroupViews.Where(i => i.TransactionDate.Value.Date >= dtFrom && i.TransactionDate.Value.Date <= dtTo
                       && i.TransactionType == 0 && (!i.IsInternal.HasValue || !i.IsInternal.Value)).ToList();

                    Telerik.Reporting.ObjectDataSource ds1 = new Telerik.Reporting.ObjectDataSource();
                    ds1.DataSource = invoiceLines;
                    ds1.DataMember = "";

                    report.DataSource = ds1;

                    //report.DataSource = new Telerik.Reporting.OpenAccessDataSource(db, "InvoiceGroupViews");

                    report.ReportParameters[0].Value = dtFrom;
                    report.ReportParameters[1].Value = dtTo;

                    this.reportViewer1.ReportSource = report;
                    this.reportViewer1.RefreshReport();

                    disposableObject.Add(report);
                    disposableObject.Add(ds1);
                }
                catch
                {
                }
            }
        }

        public void TransactionReport()
        {
            using (SelectFromToForm sftf = new SelectFromToForm())
            {
                TimeSpan tc = Data.Implementation.OptionHandler.Instance.GetTimeSpanOption("DayCloseHour", TimeSpan.ParseExact("01:00:00:00", "g", System.Globalization.CultureInfo.CurrentCulture));
                sftf.StartDate = DateTime.Today.AddDays(-1).Add(tc);
                sftf.EndDate = DateTime.Today.Add(tc);

                DialogResult res = sftf.ShowDialog(this);
                if (res != DialogResult.OK)
                    return;

                DateTime dtFrom = sftf.StartDate.Value;
                DateTime dtTo = sftf.EndDate.Value;

                Guid invoiceTypeId = Data.Implementation.OptionHandler.Instance.GetGuidOption("LiterCheckInvoiceType", Guid.Empty); //Guid.Parse("394612c6-073c-4820-aa60-828aaecefcb7");


                try
                {
                    Reports.TransactionReport report = new Reports.TransactionReport();

                    List<Data.InvoiceLine> invoiceLines = db.InvoiceLines.Where(i => i.SalesTransaction.TransactionTimeStamp <= dtTo && i.SalesTransaction.TransactionTimeStamp >= dtFrom &&
                        i.Invoice.InvoiceType.TransactionSign.HasValue && i.Invoice.InvoiceType.TransactionSign.Value != 0 &&
                        (!i.Invoice.IsEuromat.HasValue || !i.Invoice.IsEuromat.Value)).ToList();

                    invoiceLines = invoiceLines.Where(i => !i.Invoice.IsReplaced).OrderBy(i => i.Invoice.TransactionDate).ThenBy(i => i.Invoice.InvoiceType.Description).ToList();

                    Telerik.Reporting.ObjectDataSource ds1 = new Telerik.Reporting.ObjectDataSource();
                    ds1.DataSource = invoiceLines;
                    ds1.DataMember = "";

                    report.DataSource = ds1;

                    report.ReportParameters[0].Value = dtFrom;
                    report.ReportParameters[1].Value = dtTo;

                    this.reportViewer1.ReportSource = report;
                    this.reportViewer1.RefreshReport();

                    disposableObject.Add(report);
                    disposableObject.Add(ds1);
                }

                catch
                {
                }
            }
        }

        public void DeliveryReport()
        {
            using (SelectFromToForm sftf = new SelectFromToForm())
            {
                TimeSpan tc = Data.Implementation.OptionHandler.Instance.GetTimeSpanOption("DayCloseHour", TimeSpan.ParseExact("01:00:00:00", "g", System.Globalization.CultureInfo.CurrentCulture));
                sftf.StartDate = DateTime.Today.AddDays(-1).Add(tc);
                sftf.EndDate = DateTime.Today.Add(tc);

                DialogResult res = sftf.ShowDialog(this);
                if (res != DialogResult.OK)
                    return;

                DateTime dtFrom = sftf.StartDate.Value;
                DateTime dtTo = sftf.EndDate.Value;

                //Guid invoiceTypeId = Data.Implementation.OptionHandler.Instance.GetGuidOption("LiterCheckInvoiceType", Guid.Empty); //Guid.Parse("394612c6-073c-4820-aa60-828aaecefcb7");

                try
                {

                    Reports.DeliveryReport report = new Reports.DeliveryReport();

                    var q = db.InvoiceLines.Where(i => i.TankFilling.TransactionTime <= dtTo && i.TankFilling.TransactionTime >= dtFrom);

                    Telerik.Reporting.ObjectDataSource ds1 = new Telerik.Reporting.ObjectDataSource();
                    ds1.DataSource = q;
                    ds1.DataMember = "";

                    report.DataSource = ds1;

                    report.ReportParameters[0].Value = dtFrom;
                    report.ReportParameters[1].Value = dtTo;

                    this.reportViewer1.ReportSource = report;
                    this.reportViewer1.RefreshReport();

                    this.disposableObject.Add(report);
                    this.disposableObject.Add(ds1);
                }
                catch
                {
                }
            }
        }

        public void TankStatusReport()
        {
            try
            {

                Reports.TankStausReport report = new Reports.TankStausReport();

                var q = db.Tanks;

                Telerik.Reporting.ObjectDataSource ds1 = new Telerik.Reporting.ObjectDataSource();
                ds1.DataSource = q;
                ds1.DataMember = "";

                report.DataSource = ds1;

                this.reportViewer1.ReportSource = report;
                this.reportViewer1.RefreshReport();

                this.disposableObject.Add(report);
                this.disposableObject.Add(ds1);
            }
            catch
            {
            }
        }

        public void NozzleTotalsReport()
        {
            try
            {

                Reports.NozzleTotalsReport report = new Reports.NozzleTotalsReport();

                var q = db.Nozzles;

                Telerik.Reporting.ObjectDataSource ds1 = new Telerik.Reporting.ObjectDataSource();
                ds1.DataSource = q;
                ds1.DataMember = "";

                report.DataSource = ds1;
                
                this.reportViewer1.ReportSource = report;
                this.reportViewer1.RefreshReport();

                this.disposableObject.Add(report);
                this.disposableObject.Add(ds1);
            }
            catch
            {
            }
        }

        public void SalesReport()
        {
            using (SelectFromToForm sftf = new SelectFromToForm())
            {
                TimeSpan tc = Data.Implementation.OptionHandler.Instance.GetTimeSpanOption("DayCloseHour", TimeSpan.ParseExact("01:00:00:00", "g", System.Globalization.CultureInfo.CurrentCulture));
                sftf.StartDate = DateTime.Today.AddDays(-1).Add(tc);
                sftf.EndDate = DateTime.Today.Add(tc);
                DialogResult res = sftf.ShowDialog(this);
                if (res != DialogResult.OK)
                    return;

                DateTime dtFrom = sftf.StartDate.Value;
                DateTime dtTo = sftf.EndDate.Value;

                Guid invoiceTypeId = Data.Implementation.OptionHandler.Instance.GetGuidOption("LiterCheckInvoiceType", Guid.Empty); //Guid.Parse("394612c6-073c-4820-aa60-828aaecefcb7");

                try
                {

                    Reports.SaleReport report = new Reports.SaleReport();
                    Reports.SaleReportFuelType subReport1 = new Reports.SaleReportFuelType();
                    Reports.SaleReportDispenser subReport2 = new Reports.SaleReportDispenser();
                    Reports.LiterCheckReport subReport3 = new Reports.LiterCheckReport();

                    report.DataSource = new Telerik.Reporting.OpenAccessDataSource(new Data.CompanyData(), "");

                    var q1 = db.SalesTransactions.Where(s => s.TransactionTimeStamp >= dtFrom && s.TransactionTimeStamp <= dtTo);
                    var q2 = db.InvoiceLines.Where(i => i.SalesTransaction.TransactionTimeStamp >= dtFrom && i.SalesTransaction.TransactionTimeStamp <= dtTo && i.Invoice.InvoiceTypeId == invoiceTypeId);

                    Telerik.Reporting.ObjectDataSource ds1 = new Telerik.Reporting.ObjectDataSource();
                    ds1.DataSource = q1;
                    ds1.DataMember = "";

                    Telerik.Reporting.ObjectDataSource ds2 = new Telerik.Reporting.ObjectDataSource();
                    ds2.DataSource = q2.OrderBy(i => i.Invoice.TransactionDate).ThenBy(i => i.Invoice.InvoiceType.Description);
                    ds2.DataMember = "";

                    subReport1.DataSource = ds1;
                    subReport2.DataSource = ds1;
                    subReport3.DataSource = ds2;

                    ((Telerik.Reporting.SubReport)report.Items["detail"].Items["subReport1"]).ReportSource = subReport1;
                    ((Telerik.Reporting.SubReport)report.Items["detail"].Items["subReport2"]).ReportSource = subReport2;
                    ((Telerik.Reporting.SubReport)report.Items["detail"].Items["subReport3"]).ReportSource = subReport3;
                    subReport1.ReportParameters[0].Value = dtFrom;
                    subReport2.ReportParameters[0].Value = dtFrom;
                    subReport3.ReportParameters[0].Value = dtFrom;
                    subReport1.ReportParameters[1].Value = dtTo;
                    subReport2.ReportParameters[1].Value = dtTo;
                    subReport3.ReportParameters[1].Value = dtTo;
                    subReport3.ReportParameters[2].Value = invoiceTypeId.ToString().ToLower();

                    report.ReportParameters[0].Value = dtFrom;
                    report.ReportParameters[1].Value = dtTo;

                    this.reportViewer1.ReportSource = report;
                    this.reportViewer1.RefreshReport();

                    disposableObject.Add(report);
                    disposableObject.Add(ds1);
                    disposableObject.Add(ds2);
                    disposableObject.Add(subReport1);
                    disposableObject.Add(subReport2);
                    disposableObject.Add(subReport3);
                }
                catch
                {
                }
            }
        }

        public void InvoiceTraderReport(int transType)
        {
            using (SelectFromToForm sftf = new SelectFromToForm())
            {
                TimeSpan tc = Data.Implementation.OptionHandler.Instance.GetTimeSpanOption("DayCloseHour", TimeSpan.ParseExact("01:00:00:00", "g", System.Globalization.CultureInfo.CurrentCulture));
                sftf.StartDate = DateTime.Today.AddDays(-1).Add(tc);
                sftf.EndDate = DateTime.Today.Add(tc);

                DialogResult res = sftf.ShowDialog(this);
                if (res != DialogResult.OK)
                    return;

                DateTime dtFrom = sftf.StartDate.Value;
                DateTime dtTo = sftf.EndDate.Value;

                try
                {

                    Reports.InvoiceTraderReport report = new Reports.InvoiceTraderReport();

                    var q = db.InvoiceLines.Where(i =>
                            i.Invoice.TransactionDate <= dtTo &&
                            i.Invoice.TransactionDate >= dtFrom &&
                            i.Invoice.InvoiceType.TransactionType == transType &&
                            (
                                !i.Invoice.InvoiceType.IsInternal.HasValue ||
                                !i.Invoice.InvoiceType.IsInternal.Value
                            ) &&
                            i.Invoice.TraderId.HasValue
                        ).OrderBy(i => i.Invoice.TransactionDate).ThenBy(i => i.Invoice.InvoiceType.Description);

                    Telerik.Reporting.ObjectDataSource ds1 = new Telerik.Reporting.ObjectDataSource();
                    ds1.DataSource = q;
                    ds1.DataMember = "";

                    report.DataSource = ds1;

                    report.ReportParameters[0].Value = dtFrom;
                    report.ReportParameters[1].Value = dtTo;
                    if (transType == 1)
                        report.ReportParameters[2].Value = "Παραστατικά Αγορών";
                    else if (transType == 0)
                        report.ReportParameters[2].Value = "Παραστατικά Χονδρικής";

                    report.ReportParameters[3].Value = transType;

                    this.reportViewer1.ReportSource = report;
                    this.reportViewer1.RefreshReport();

                    this.disposableObject.Add(report);
                    this.disposableObject.Add(ds1);

                }
                catch
                {
                }
            }
        }
    }
}
