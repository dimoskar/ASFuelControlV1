using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ASFuelControl.Windows.UI.Catalogs
{
    public partial class ReportGrid : UserControl
    {
        public ReportGrid()
        {
            InitializeComponent();
            int weekDay = (int)DateTime.Today.DayOfWeek;
            if(weekDay == 0)
                weekDay = 7;
            //this.radDateTimePicker1.Value = DateTime.Today.AddDays(-weekDay).AddDays(1);
            //this.radDateTimePicker2.Value = DateTime.Today;

            //this.radDateTimePicker3.Value = DateTime.Today.AddDays(-DateTime.Today.Day).AddDays(1);
            //this.radDateTimePicker4.Value = DateTime.Today;

            
        }

        private void radMenuItem1_Click(object sender, EventArgs e)
        {
            UI.SelectionForms.SelectShift ssf = new SelectionForms.SelectShift();
            DialogResult res = ssf.ShowDialog();
            if (res == DialogResult.Cancel)
                return;
            if (ssf.SelectedShift == Guid.Empty)
                return;
            Guid invoiceTypeId = Data.Implementation.OptionHandler.Instance.GetGuidOption("LiterCheckInvoiceType", Guid.Empty);
            try
            {
                Reports.ShiftReport report = new Reports.ShiftReport();
                //report.DataSource = 

                Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);

                Telerik.Reporting.OpenAccessDataSource openAccessDataSource = new Telerik.Reporting.OpenAccessDataSource(db, "Shifts");

                Reports.ShiftReportFuelType subReport1 = new Reports.ShiftReportFuelType();
                Reports.ShiftReportDispenser subReport2 = new Reports.ShiftReportDispenser();
                Reports.ShiftLiterCheck subReport3 = new Reports.ShiftLiterCheck();

                subReport1.DataSource = new Telerik.Reporting.OpenAccessDataSource(db, "SalesTransactions");
                subReport2.DataSource = new Telerik.Reporting.OpenAccessDataSource(db, "SalesTransactions");
                subReport3.DataSource = new Telerik.Reporting.OpenAccessDataSource(db, "InvoiceLines");

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

                //db.Dispose();
            }
            catch
            {
            }
        }

        private void radMenuItem2_Click(object sender, EventArgs e)
        {

            SelectionForms.SelectFromToForm sftf = new SelectionForms.SelectFromToForm();
            TimeSpan tc = Data.Implementation.OptionHandler.Instance.GetTimeSpanOption("DayCloseHour", TimeSpan.ParseExact("01:00:00:00", "g", System.Globalization.CultureInfo.CurrentCulture));
            sftf.StartDate = DateTime.Today.AddDays(-1).Add(tc);
            sftf.EndDate = DateTime.Today.Add(tc);
            DialogResult res = sftf.ShowDialog();
            if (res != DialogResult.OK)
                return;

            DateTime dtFrom = sftf.StartDate.Value;
            DateTime dtTo = sftf.EndDate.Value;

            Guid invoiceTypeId = Data.Implementation.OptionHandler.Instance.GetGuidOption("LiterCheckInvoiceType", Guid.Empty); //Guid.Parse("394612c6-073c-4820-aa60-828aaecefcb7");

            try
            {
                Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);

                Reports.SaleReport report = new Reports.SaleReport();
                Reports.SaleReportFuelType subReport1 = new Reports.SaleReportFuelType();
                Reports.SaleReportDispenser subReport2 = new Reports.SaleReportDispenser();
                Reports.LiterCheckReport subReport3 = new Reports.LiterCheckReport();

                subReport1.DataSource = new Telerik.Reporting.OpenAccessDataSource(db, "SalesTransactions");
                subReport2.DataSource = new Telerik.Reporting.OpenAccessDataSource(db, "SalesTransactions");
                subReport3.DataSource = new Telerik.Reporting.OpenAccessDataSource(db, "InvoiceLines");

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
            }
            catch
            {
            }
        }

        private void radMenuItem3_Click(object sender, EventArgs e)
        {
            UI.SelectionForms.SelectBalanceForm ssf = new SelectionForms.SelectBalanceForm();
            DialogResult res = ssf.ShowDialog();
            if (res == DialogResult.Cancel)
                return;
            if (ssf.SelectedBalance == Guid.Empty)
                return;
            try
            {
                Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
                Data.Balance balance = db.Balances.Where(b => b.BalanceId == ssf.SelectedBalance).FirstOrDefault();
                Reports.BalanceReports.BalanceLoad bl = new Reports.BalanceReports.BalanceLoad();
                bl.LoadBalance(ssf.SelectedBalance, balance.BalanceText);

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

            }
            catch
            {

            }
        }

        private void radMenuItem4_Click(object sender, EventArgs e)
        {
            SelectionForms.SelectFromToForm sftf = new SelectionForms.SelectFromToForm();
            TimeSpan tc = Data.Implementation.OptionHandler.Instance.GetTimeSpanOption("DayCloseHour", TimeSpan.ParseExact("01:00:00:00", "g", System.Globalization.CultureInfo.CurrentCulture));
            sftf.StartDate = DateTime.Today.AddDays(-1).Add(tc);
            sftf.EndDate = DateTime.Today.Add(tc);

            DialogResult res = sftf.ShowDialog();
            if (res != DialogResult.OK)
                return;

            DateTime dtFrom = sftf.StartDate.Value;
            DateTime dtTo = sftf.EndDate.Value;

            try
            {
                Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);

                Reports.InvoiceReport report = new Reports.InvoiceReport();
                report.DataSource = new Telerik.Reporting.OpenAccessDataSource(db, "InvoiceGroupViews");

                report.ReportParameters[0].Value = dtFrom;
                report.ReportParameters[1].Value = dtTo;

                this.reportViewer1.ReportSource = report;
                this.reportViewer1.RefreshReport();
            }
            catch
            {
            }
        }

        private void radMenuItem5_Click(object sender, EventArgs e)
        {   
            SelectionForms.SelectFromToForm sftf = new SelectionForms.SelectFromToForm();
            TimeSpan tc = Data.Implementation.OptionHandler.Instance.GetTimeSpanOption("DayCloseHour", TimeSpan.ParseExact("01:00:00:00", "g", System.Globalization.CultureInfo.CurrentCulture));
            sftf.StartDate = DateTime.Today.AddDays(-1).Add(tc);
            sftf.EndDate = DateTime.Today.Add(tc);

            DialogResult res = sftf.ShowDialog();
            if (res != DialogResult.OK)
                return;

            DateTime dtFrom = sftf.StartDate.Value;
            DateTime dtTo = sftf.EndDate.Value;

            Guid invoiceTypeId = Data.Implementation.OptionHandler.Instance.GetGuidOption("LiterCheckInvoiceType", Guid.Empty); //Guid.Parse("394612c6-073c-4820-aa60-828aaecefcb7");

            try
            {
                Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);

                Reports.TransactionReport report = new Reports.TransactionReport();
                report.DataSource = new Telerik.Reporting.OpenAccessDataSource(db, "InvoiceLines");

                report.ReportParameters[0].Value = dtFrom;
                report.ReportParameters[1].Value = dtTo;

                this.reportViewer1.ReportSource = report;
                this.reportViewer1.RefreshReport();
            }
            catch
            {
            }
        }

        private void radMenuItem6_Click(object sender, EventArgs e)
        {
            SelectionForms.SelectFromToForm sftf = new SelectionForms.SelectFromToForm();
            TimeSpan tc = Data.Implementation.OptionHandler.Instance.GetTimeSpanOption("DayCloseHour", TimeSpan.ParseExact("01:00:00:00", "g", System.Globalization.CultureInfo.CurrentCulture));
            sftf.StartDate = DateTime.Today.AddDays(-1).Add(tc);
            sftf.EndDate = DateTime.Today.Add(tc);

            DialogResult res = sftf.ShowDialog();
            if (res != DialogResult.OK)
                return;

            DateTime dtFrom = sftf.StartDate.Value;
            DateTime dtTo = sftf.EndDate.Value;

            //Guid invoiceTypeId = Data.Implementation.OptionHandler.Instance.GetGuidOption("LiterCheckInvoiceType", Guid.Empty); //Guid.Parse("394612c6-073c-4820-aa60-828aaecefcb7");

            try
            {
                Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);

                Reports.DeliveryReport report = new Reports.DeliveryReport();
                report.DataSource = new Telerik.Reporting.OpenAccessDataSource(db, "InvoiceLines");

                report.ReportParameters[0].Value = dtFrom;
                report.ReportParameters[1].Value = dtTo;
                
                this.reportViewer1.ReportSource = report;
                this.reportViewer1.RefreshReport();
            }
            catch
            {
            }
        }

        private void radMenuItem7_Click(object sender, EventArgs e)
        {
            try
            {
                Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);

                Reports.TankStausReport report = new Reports.TankStausReport();
                report.DataSource = new Telerik.Reporting.OpenAccessDataSource(db, "Tanks");

                this.reportViewer1.ReportSource = report;
                this.reportViewer1.RefreshReport();
            }
            catch
            {
            }
        }
    }
}
