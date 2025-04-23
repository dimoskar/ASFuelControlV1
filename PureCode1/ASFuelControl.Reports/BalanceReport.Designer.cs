namespace ASFuelControl.Reports
{
    partial class BalanceReport
    {
        #region Component Designer generated code
        /// <summary>
        /// Required method for telerik Reporting designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            Telerik.Reporting.Drawing.FormattingRule formattingRule1 = new Telerik.Reporting.Drawing.FormattingRule();
            Telerik.Reporting.InstanceReportSource instanceReportSource1 = new Telerik.Reporting.InstanceReportSource();
            Telerik.Reporting.InstanceReportSource instanceReportSource2 = new Telerik.Reporting.InstanceReportSource();
            Telerik.Reporting.InstanceReportSource instanceReportSource3 = new Telerik.Reporting.InstanceReportSource();
            Telerik.Reporting.InstanceReportSource instanceReportSource4 = new Telerik.Reporting.InstanceReportSource();
            Telerik.Reporting.Drawing.StyleRule styleRule1 = new Telerik.Reporting.Drawing.StyleRule();
            this.tankBalanceReport1 = new ASFuelControl.Reports.BalanceReports.TankBalanceReport();
            this.pumpBalanceReport1 = new ASFuelControl.Reports.BalanceReports.PumpBalanceReport();
            this.divergenceBalanceReport1 = new ASFuelControl.Reports.BalanceReports.DivergenceBalanceReport();
            this.tankFillingBalanceReport1 = new ASFuelControl.Reports.BalanceReports.TankFillingBalanceReport();
            this.pageHeaderSection1 = new Telerik.Reporting.PageHeaderSection();
            this.textBox1 = new Telerik.Reporting.TextBox();
            this.pictureBox1 = new Telerik.Reporting.PictureBox();
            this.textBox4 = new Telerik.Reporting.TextBox();
            this.detail = new Telerik.Reporting.DetailSection();
            this.subReport1 = new Telerik.Reporting.SubReport();
            this.subReport2 = new Telerik.Reporting.SubReport();
            this.subReport3 = new Telerik.Reporting.SubReport();
            this.subReport4 = new Telerik.Reporting.SubReport();
            this.balanceDataSource = new Telerik.Reporting.ObjectDataSource();
            this.pageFooterSection1 = new Telerik.Reporting.PageFooterSection();
            this.textBox2 = new Telerik.Reporting.TextBox();
            this.textBox7 = new Telerik.Reporting.TextBox();
            this.textBox3 = new Telerik.Reporting.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.tankBalanceReport1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pumpBalanceReport1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.divergenceBalanceReport1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tankFillingBalanceReport1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            // 
            // tankBalanceReport1
            // 
            this.tankBalanceReport1.Name = "TankBalanceReport";
            // 
            // pumpBalanceReport1
            // 
            this.pumpBalanceReport1.Name = "PumpBalanceReport";
            // 
            // divergenceBalanceReport1
            // 
            this.divergenceBalanceReport1.Name = "DivergenceBalanceReport";
            // 
            // tankFillingBalanceReport1
            // 
            this.tankFillingBalanceReport1.Name = "TankFillingBalanceReport";
            // 
            // pageHeaderSection1
            // 
            formattingRule1.Filters.Add(new Telerik.Reporting.Filter("= PageNumber > 1 AND PageNumber < PageCount", Telerik.Reporting.FilterOperator.Equal, "True"));
            formattingRule1.Style.Visible = false;
            this.pageHeaderSection1.ConditionalFormatting.AddRange(new Telerik.Reporting.Drawing.FormattingRule[] {
            formattingRule1});
            this.pageHeaderSection1.Height = Telerik.Reporting.Drawing.Unit.Cm(1.1999999284744263D);
            this.pageHeaderSection1.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.textBox1});
            this.pageHeaderSection1.Name = "pageHeaderSection1";
            this.pageHeaderSection1.PrintOnLastPage = false;
            // 
            // textBox1
            // 
            this.textBox1.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0.00010012308484874666D), Telerik.Reporting.Drawing.Unit.Cm(9.9921220680698752E-05D));
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(26.699800491333008D), Telerik.Reporting.Drawing.Unit.Cm(0.99999988079071045D));
            this.textBox1.Style.Font.Bold = true;
            this.textBox1.Style.Font.Size = Telerik.Reporting.Drawing.Unit.Point(20D);
            this.textBox1.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            this.textBox1.Value = "Éóïæýãéï {Fields.BeginDateTime.ToString(\"dd/MM/yyyy HH:mm\")} - {Fields.EndDateTim" +
    "e.ToString(\"dd/MM/yyyy HH:mm\")}";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0.00010012308484874666D), Telerik.Reporting.Drawing.Unit.Cm(0D));
            this.pictureBox1.MimeType = "";
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(1.1998999118804932D), Telerik.Reporting.Drawing.Unit.Cm(1.2000001668930054D));
            this.pictureBox1.Sizing = Telerik.Reporting.Drawing.ImageSizeMode.Stretch;
            this.pictureBox1.Value = "";
            // 
            // textBox4
            // 
            this.textBox4.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(1.2002004384994507D), Telerik.Reporting.Drawing.Unit.Cm(9.9921220680698752E-05D));
            this.textBox4.Name = "textBox4";
            this.textBox4.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(4.199798583984375D), Telerik.Reporting.Drawing.Unit.Cm(0.39989984035491943D));
            this.textBox4.Style.Color = System.Drawing.Color.LightGray;
            this.textBox4.Style.Font.Bold = true;
            this.textBox4.Style.Font.Size = Telerik.Reporting.Drawing.Unit.Point(9D);
            this.textBox4.Value = "AS Fuel Control";
            // 
            // detail
            // 
            this.detail.Height = Telerik.Reporting.Drawing.Unit.Cm(6.4999995231628418D);
            this.detail.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.subReport1,
            this.subReport2,
            this.subReport3,
            this.subReport4});
            this.detail.Name = "detail";
            // 
            // subReport1
            // 
            this.subReport1.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0.00010012308484874666D), Telerik.Reporting.Drawing.Unit.Cm(0.00010012308484874666D));
            this.subReport1.Name = "subReport1";
            instanceReportSource1.ReportDocument = this.tankBalanceReport1;
            this.subReport1.ReportSource = instanceReportSource1;
            this.subReport1.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(5D), Telerik.Reporting.Drawing.Unit.Cm(1.399899959564209D));
            // 
            // subReport2
            // 
            this.subReport2.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0.00010012308484874666D), Telerik.Reporting.Drawing.Unit.Cm(1.6000000238418579D));
            this.subReport2.Name = "subReport2";
            instanceReportSource2.ReportDocument = this.pumpBalanceReport1;
            this.subReport2.ReportSource = instanceReportSource2;
            this.subReport2.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(5D), Telerik.Reporting.Drawing.Unit.Cm(1.399899959564209D));
            // 
            // subReport3
            // 
            this.subReport3.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0.00010012308484874666D), Telerik.Reporting.Drawing.Unit.Cm(4.8000006675720215D));
            this.subReport3.Name = "subReport3";
            instanceReportSource3.ReportDocument = this.divergenceBalanceReport1;
            this.subReport3.ReportSource = instanceReportSource3;
            this.subReport3.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(5D), Telerik.Reporting.Drawing.Unit.Cm(1.399899959564209D));
            // 
            // subReport4
            // 
            this.subReport4.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0.00010012308484874666D), Telerik.Reporting.Drawing.Unit.Cm(3.2000002861022949D));
            this.subReport4.Name = "subReport4";
            instanceReportSource4.ReportDocument = this.tankFillingBalanceReport1;
            this.subReport4.ReportSource = instanceReportSource4;
            this.subReport4.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(5.0000004768371582D), Telerik.Reporting.Drawing.Unit.Cm(1.3998006582260132D));
            // 
            // balanceDataSource
            // 
            this.balanceDataSource.DataMember = "Balance";
            this.balanceDataSource.DataSource = "ASFuelControl.Reports.BalanceReports.BalanceDS, ASFuelControl.Reports, Version=1." +
    "0.0.0, Culture=neutral, PublicKeyToken=null";
            this.balanceDataSource.Name = "balanceDataSource";
            // 
            // pageFooterSection1
            // 
            this.pageFooterSection1.Height = Telerik.Reporting.Drawing.Unit.Cm(1.399999737739563D);
            this.pageFooterSection1.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.textBox2,
            this.textBox7,
            this.textBox3,
            this.pictureBox1,
            this.textBox4});
            this.pageFooterSection1.Name = "pageFooterSection1";
            // 
            // textBox2
            // 
            this.textBox2.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(1.2002004384994507D), Telerik.Reporting.Drawing.Unit.Cm(0.40020003914833069D));
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(5D), Telerik.Reporting.Drawing.Unit.Cm(0.79980015754699707D));
            this.textBox2.Style.VerticalAlign = Telerik.Reporting.Drawing.VerticalAlign.Middle;
            this.textBox2.Value = "= Now().ToString(\"dd/MM/yyyy HH:mm\")";
            // 
            // textBox7
            // 
            this.textBox7.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(18.999902725219727D), Telerik.Reporting.Drawing.Unit.Cm(0.40020003914833069D));
            this.textBox7.Name = "textBox7";
            this.textBox7.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(7.6999998092651367D), Telerik.Reporting.Drawing.Unit.Cm(0.79980015754699707D));
            this.textBox7.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.textBox7.Style.VerticalAlign = Telerik.Reporting.Drawing.VerticalAlign.Middle;
            this.textBox7.Value = "Óåëßäá {PageNumber} áðü {PageCount}";
            // 
            // textBox3
            // 
            this.textBox3.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(7.9999990463256836D), Telerik.Reporting.Drawing.Unit.Cm(0.40020003914833069D));
            this.textBox3.Name = "textBox3";
            this.textBox3.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(10.300000190734863D), Telerik.Reporting.Drawing.Unit.Cm(0.79980015754699707D));
            this.textBox3.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            this.textBox3.Style.VerticalAlign = Telerik.Reporting.Drawing.VerticalAlign.Middle;
            this.textBox3.Value = "Éóïæýãéï {Fields.BeginDateTime.ToString(\"dd/MM/yyyy HH:mm\")} - {Fields.EndDateTim" +
    "e.ToString(\"dd/MM/yyyy HH:mm\")}";
            // 
            // BalanceReport
            // 
            this.DataSource = this.balanceDataSource;
            this.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.pageHeaderSection1,
            this.detail,
            this.pageFooterSection1});
            this.Name = "BalanceReport";
            this.PageSettings.Landscape = true;
            this.PageSettings.Margins = new Telerik.Reporting.Drawing.MarginsU(Telerik.Reporting.Drawing.Unit.Mm(15D), Telerik.Reporting.Drawing.Unit.Mm(15D), Telerik.Reporting.Drawing.Unit.Mm(15D), Telerik.Reporting.Drawing.Unit.Mm(15D));
            this.PageSettings.PaperKind = System.Drawing.Printing.PaperKind.A4;
            this.Style.BackgroundColor = System.Drawing.Color.White;
            styleRule1.Selectors.AddRange(new Telerik.Reporting.Drawing.ISelector[] {
            new Telerik.Reporting.Drawing.TypeSelector(typeof(Telerik.Reporting.TextItemBase)),
            new Telerik.Reporting.Drawing.TypeSelector(typeof(Telerik.Reporting.HtmlTextBox))});
            styleRule1.Style.Padding.Left = Telerik.Reporting.Drawing.Unit.Point(2D);
            styleRule1.Style.Padding.Right = Telerik.Reporting.Drawing.Unit.Point(2D);
            this.StyleSheet.AddRange(new Telerik.Reporting.Drawing.StyleRule[] {
            styleRule1});
            this.Width = Telerik.Reporting.Drawing.Unit.Cm(26.700000762939453D);
            ((System.ComponentModel.ISupportInitialize)(this.tankBalanceReport1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pumpBalanceReport1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.divergenceBalanceReport1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tankFillingBalanceReport1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();

        }
        #endregion

        private Telerik.Reporting.PageHeaderSection pageHeaderSection1;
        private Telerik.Reporting.DetailSection detail;
        private Telerik.Reporting.ObjectDataSource balanceDataSource;
        private Telerik.Reporting.SubReport subReport1;
        private Telerik.Reporting.SubReport subReport2;
        private BalanceReports.PumpBalanceReport pumpBalanceReport1;
        private BalanceReports.TankBalanceReport tankBalanceReport1;
        private Telerik.Reporting.SubReport subReport3;
        private BalanceReports.DivergenceBalanceReport divergenceBalanceReport1;
        private Telerik.Reporting.TextBox textBox1;
        private Telerik.Reporting.PageFooterSection pageFooterSection1;
        private Telerik.Reporting.TextBox textBox2;
        private Telerik.Reporting.TextBox textBox7;
        private Telerik.Reporting.TextBox textBox3;
        private Telerik.Reporting.PictureBox pictureBox1;
        private Telerik.Reporting.TextBox textBox4;
        private Telerik.Reporting.SubReport subReport4;
        private BalanceReports.TankFillingBalanceReport tankFillingBalanceReport1;
    }
}