namespace ASFuelControl.Reports
{
    partial class SaleReport
    {
        #region Component Designer generated code
        /// <summary>
        /// Required method for telerik Reporting designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            Telerik.Reporting.InstanceReportSource instanceReportSource1 = new Telerik.Reporting.InstanceReportSource();
            Telerik.Reporting.InstanceReportSource instanceReportSource2 = new Telerik.Reporting.InstanceReportSource();
            Telerik.Reporting.InstanceReportSource instanceReportSource3 = new Telerik.Reporting.InstanceReportSource();
            Telerik.Reporting.ReportParameter reportParameter1 = new Telerik.Reporting.ReportParameter();
            Telerik.Reporting.ReportParameter reportParameter2 = new Telerik.Reporting.ReportParameter();
            Telerik.Reporting.Drawing.StyleRule styleRule1 = new Telerik.Reporting.Drawing.StyleRule();
            this.saleReportFuelType1 = new ASFuelControl.Reports.SaleReportFuelType();
            this.saleReportDispenser1 = new ASFuelControl.Reports.SaleReportDispenser();
            this.literCheckReport1 = new ASFuelControl.Reports.LiterCheckReport();
            this.pageHeaderSection1 = new Telerik.Reporting.PageHeaderSection();
            this.textBox5 = new Telerik.Reporting.TextBox();
            this.textBox6 = new Telerik.Reporting.TextBox();
            this.textBox7 = new Telerik.Reporting.TextBox();
            this.textBox8 = new Telerik.Reporting.TextBox();
            this.titleTextBox = new Telerik.Reporting.TextBox();
            this.detail = new Telerik.Reporting.DetailSection();
            this.subReport1 = new Telerik.Reporting.SubReport();
            this.shape2 = new Telerik.Reporting.Shape();
            this.subReport2 = new Telerik.Reporting.SubReport();
            this.shape1 = new Telerik.Reporting.Shape();
            this.shape3 = new Telerik.Reporting.Shape();
            this.subReport3 = new Telerik.Reporting.SubReport();
            this.pageFooterSection1 = new Telerik.Reporting.PageFooterSection();
            this.textBox1 = new Telerik.Reporting.TextBox();
            this.pictureBox1 = new Telerik.Reporting.PictureBox();
            this.textBox4 = new Telerik.Reporting.TextBox();
            this.pageInfoTextBox = new Telerik.Reporting.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.saleReportFuelType1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.saleReportDispenser1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.literCheckReport1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            // 
            // saleReportFuelType1
            // 
            this.saleReportFuelType1.Name = "SaleReportDispenser";
            // 
            // saleReportDispenser1
            // 
            this.saleReportDispenser1.Name = "SaleReportFuelType";
            // 
            // literCheckReport1
            // 
            this.literCheckReport1.Name = "LiterCheckReport";
            // 
            // pageHeaderSection1
            // 
            this.pageHeaderSection1.Height = Telerik.Reporting.Drawing.Unit.Cm(2.3999998569488525D);
            this.pageHeaderSection1.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.textBox5,
            this.textBox6,
            this.textBox7,
            this.textBox8,
            this.titleTextBox});
            this.pageHeaderSection1.Name = "pageHeaderSection1";
            // 
            // textBox5
            // 
            this.textBox5.CanGrow = true;
            this.textBox5.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0.00010012308484874666D), Telerik.Reporting.Drawing.Unit.Cm(1.2000000476837158D));
            this.textBox5.Name = "textBox5";
            this.textBox5.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.5998997688293457D), Telerik.Reporting.Drawing.Unit.Cm(0.99999988079071045D));
            this.textBox5.Style.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(121)))), ((int)(((byte)(167)))), ((int)(((byte)(227)))));
            this.textBox5.Style.Color = System.Drawing.Color.White;
            this.textBox5.Style.Font.Name = "Verdana";
            this.textBox5.Style.Padding.Left = Telerik.Reporting.Drawing.Unit.Cm(0.20000000298023224D);
            this.textBox5.Style.Padding.Right = Telerik.Reporting.Drawing.Unit.Cm(0.20000000298023224D);
            this.textBox5.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Left;
            this.textBox5.Style.VerticalAlign = Telerik.Reporting.Drawing.VerticalAlign.Middle;
            this.textBox5.StyleName = "Caption";
            this.textBox5.Value = "Έναρξη";
            // 
            // textBox6
            // 
            this.textBox6.CanGrow = true;
            this.textBox6.Format = "{0:ddd dd/MM/yyyy HH:mm}";
            this.textBox6.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(2.6548960208892822D), Telerik.Reporting.Drawing.Unit.Cm(1.2000000476837158D));
            this.textBox6.Name = "textBox6";
            this.textBox6.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(5.2295875549316406D), Telerik.Reporting.Drawing.Unit.Cm(0.99999988079071045D));
            this.textBox6.Style.Font.Name = "Verdana";
            this.textBox6.Style.Padding.Left = Telerik.Reporting.Drawing.Unit.Cm(0.20000000298023224D);
            this.textBox6.Style.Padding.Right = Telerik.Reporting.Drawing.Unit.Cm(0.20000000298023224D);
            this.textBox6.Style.VerticalAlign = Telerik.Reporting.Drawing.VerticalAlign.Middle;
            this.textBox6.StyleName = "Data";
            this.textBox6.Value = "= Parameters.DateFrom.Value.ToString(\"ddd dd/MM/yyyy HH:mm\")";
            // 
            // textBox7
            // 
            this.textBox7.CanGrow = true;
            this.textBox7.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(7.8846831321716309D), Telerik.Reporting.Drawing.Unit.Cm(1.2000000476837158D));
            this.textBox7.Name = "textBox7";
            this.textBox7.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.5998997688293457D), Telerik.Reporting.Drawing.Unit.Cm(0.99999988079071045D));
            this.textBox7.Style.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(121)))), ((int)(((byte)(167)))), ((int)(((byte)(227)))));
            this.textBox7.Style.Color = System.Drawing.Color.White;
            this.textBox7.Style.Font.Name = "Verdana";
            this.textBox7.Style.Padding.Left = Telerik.Reporting.Drawing.Unit.Cm(0.20000000298023224D);
            this.textBox7.Style.Padding.Right = Telerik.Reporting.Drawing.Unit.Cm(0.20000000298023224D);
            this.textBox7.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Left;
            this.textBox7.Style.VerticalAlign = Telerik.Reporting.Drawing.VerticalAlign.Middle;
            this.textBox7.StyleName = "Caption";
            this.textBox7.Value = "Λήξη";
            // 
            // textBox8
            // 
            this.textBox8.CanGrow = true;
            this.textBox8.Format = "{0:ddd dd/MM/yyyy HH:mm}";
            this.textBox8.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(10.484783172607422D), Telerik.Reporting.Drawing.Unit.Cm(1.2000000476837158D));
            this.textBox8.Name = "textBox8";
            this.textBox8.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(5.2122921943664551D), Telerik.Reporting.Drawing.Unit.Cm(0.99999988079071045D));
            this.textBox8.Style.Font.Name = "Verdana";
            this.textBox8.Style.Padding.Left = Telerik.Reporting.Drawing.Unit.Cm(0.20000000298023224D);
            this.textBox8.Style.Padding.Right = Telerik.Reporting.Drawing.Unit.Cm(0.20000000298023224D);
            this.textBox8.Style.VerticalAlign = Telerik.Reporting.Drawing.VerticalAlign.Middle;
            this.textBox8.StyleName = "Data";
            this.textBox8.Value = "= Parameters.DateTo.Value.ToString(\"ddd dd/MM/yyyy HH:mm\")";
            // 
            // titleTextBox
            // 
            this.titleTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0.00010012308484874666D), Telerik.Reporting.Drawing.Unit.Cm(0D));
            this.titleTextBox.Name = "titleTextBox";
            this.titleTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(14.999899864196777D), Telerik.Reporting.Drawing.Unit.Cm(1.196774959564209D));
            this.titleTextBox.Style.Font.Name = "Verdana";
            this.titleTextBox.Style.Font.Size = Telerik.Reporting.Drawing.Unit.Point(18D);
            this.titleTextBox.StyleName = "Title";
            this.titleTextBox.Value = "Αναφορά Περιόδου";
            // 
            // detail
            // 
            this.detail.Height = Telerik.Reporting.Drawing.Unit.Cm(7.4998998641967773D);
            this.detail.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.subReport1,
            this.shape2,
            this.subReport2,
            this.shape1,
            this.shape3,
            this.subReport3});
            this.detail.Name = "detail";
            // 
            // subReport1
            // 
            this.subReport1.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0.00010012308484874666D), Telerik.Reporting.Drawing.Unit.Cm(0.80030155181884766D));
            this.subReport1.Name = "subReport1";
            instanceReportSource1.Parameters.Add(new Telerik.Reporting.Parameter("DateFrom", "=Parameters.DateFrom.Value"));
            instanceReportSource1.Parameters.Add(new Telerik.Reporting.Parameter("DateTo", "=Parameters.DateTo.Value"));
            instanceReportSource1.ReportDocument = this.saleReportFuelType1;
            this.subReport1.ReportSource = instanceReportSource1;
            this.subReport1.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(15.919699668884277D), Telerik.Reporting.Drawing.Unit.Cm(1.4998999834060669D));
            // 
            // shape2
            // 
            this.shape2.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0.00010012308484874666D), Telerik.Reporting.Drawing.Unit.Cm(2.3004016876220703D));
            this.shape2.Name = "shape2";
            this.shape2.ShapeType = new Telerik.Reporting.Drawing.Shapes.LineShape(Telerik.Reporting.Drawing.Shapes.LineDirection.EW);
            this.shape2.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(17.999797821044922D), Telerik.Reporting.Drawing.Unit.Cm(0.40010073781013489D));
            // 
            // subReport2
            // 
            this.subReport2.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0D), Telerik.Reporting.Drawing.Unit.Cm(3.1000001430511475D));
            this.subReport2.Name = "subReport2";
            instanceReportSource2.Parameters.Add(new Telerik.Reporting.Parameter("DateFrom", "=Parameters.DateFrom.Value"));
            instanceReportSource2.Parameters.Add(new Telerik.Reporting.Parameter("DateTo", "=Parameters.DateTo.Value"));
            instanceReportSource2.ReportDocument = this.saleReportDispenser1;
            this.subReport2.ReportSource = instanceReportSource2;
            this.subReport2.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(15.919699668884277D), Telerik.Reporting.Drawing.Unit.Cm(1.4998999834060669D));
            // 
            // shape1
            // 
            this.shape1.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0D), Telerik.Reporting.Drawing.Unit.Cm(0.00010012308484874666D));
            this.shape1.Name = "shape1";
            this.shape1.ShapeType = new Telerik.Reporting.Drawing.Shapes.LineShape(Telerik.Reporting.Drawing.Shapes.LineDirection.EW);
            this.shape1.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(17.999898910522461D), Telerik.Reporting.Drawing.Unit.Cm(0.40010073781013489D));
            // 
            // shape3
            // 
            this.shape3.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0D), Telerik.Reporting.Drawing.Unit.Cm(4.6001009941101074D));
            this.shape3.Name = "shape3";
            this.shape3.ShapeType = new Telerik.Reporting.Drawing.Shapes.LineShape(Telerik.Reporting.Drawing.Shapes.LineDirection.EW);
            this.shape3.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(17.999797821044922D), Telerik.Reporting.Drawing.Unit.Cm(0.40010073781013489D));
            // 
            // subReport3
            // 
            this.subReport3.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0D), Telerik.Reporting.Drawing.Unit.Cm(5.40000057220459D));
            this.subReport3.Name = "subReport3";
            instanceReportSource3.Parameters.Add(new Telerik.Reporting.Parameter("DateFrom", "=Parameters.DateFrom.Value"));
            instanceReportSource3.Parameters.Add(new Telerik.Reporting.Parameter("DateTo", "=Parameters.DateTo.Value"));
            instanceReportSource3.ReportDocument = this.literCheckReport1;
            this.subReport3.ReportSource = instanceReportSource3;
            this.subReport3.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(15.920000076293945D), Telerik.Reporting.Drawing.Unit.Cm(1.9998996257781982D));
            // 
            // pageFooterSection1
            // 
            this.pageFooterSection1.Height = Telerik.Reporting.Drawing.Unit.Cm(1.5000003576278687D);
            this.pageFooterSection1.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.textBox1,
            this.pictureBox1,
            this.textBox4,
            this.pageInfoTextBox});
            this.pageFooterSection1.Name = "pageFooterSection1";
            // 
            // textBox1
            // 
            this.textBox1.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(1.2965583801269531D), Telerik.Reporting.Drawing.Unit.Cm(0.44989258050918579D));
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(5D), Telerik.Reporting.Drawing.Unit.Cm(0.70812535285949707D));
            this.textBox1.Style.VerticalAlign = Telerik.Reporting.Drawing.VerticalAlign.Bottom;
            this.textBox1.Value = "= Now().ToString(\"dd/MM/yyyy HH:mm\")";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0.00010012308484874666D), Telerik.Reporting.Drawing.Unit.Cm(0.00010093052696902305D));
            this.pictureBox1.MimeType = "";
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(1.1998999118804932D), Telerik.Reporting.Drawing.Unit.Cm(1.1493750810623169D));
            this.pictureBox1.Sizing = Telerik.Reporting.Drawing.ImageSizeMode.Stretch;
            this.pictureBox1.Value = "";
            // 
            // textBox4
            // 
            this.textBox4.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(1.2965583801269531D), Telerik.Reporting.Drawing.Unit.Cm(0.00010093052696902305D));
            this.textBox4.Name = "textBox4";
            this.textBox4.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(4.199798583984375D), Telerik.Reporting.Drawing.Unit.Cm(0.34927469491958618D));
            this.textBox4.Style.Color = System.Drawing.Color.LightGray;
            this.textBox4.Style.Font.Bold = true;
            this.textBox4.Style.Font.Size = Telerik.Reporting.Drawing.Unit.Point(9D);
            this.textBox4.Value = "AS Fuel Control";
            // 
            // pageInfoTextBox
            // 
            this.pageInfoTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(7.1999998092651367D), Telerik.Reporting.Drawing.Unit.Cm(0.21176758408546448D));
            this.pageInfoTextBox.Name = "pageInfoTextBox";
            this.pageInfoTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(8.7200002670288086D), Telerik.Reporting.Drawing.Unit.Cm(0.9493747353553772D));
            this.pageInfoTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.pageInfoTextBox.Style.VerticalAlign = Telerik.Reporting.Drawing.VerticalAlign.Bottom;
            this.pageInfoTextBox.StyleName = "PageInfo";
            this.pageInfoTextBox.Value = "Σελίδα {PageNumber} από {PageCount}";
            // 
            // SaleReport
            // 
            this.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.pageHeaderSection1,
            this.detail,
            this.pageFooterSection1});
            this.Name = "SaleReport";
            this.PageSettings.Landscape = false;
            this.PageSettings.Margins = new Telerik.Reporting.Drawing.MarginsU(Telerik.Reporting.Drawing.Unit.Mm(15D), Telerik.Reporting.Drawing.Unit.Mm(15D), Telerik.Reporting.Drawing.Unit.Mm(15D), Telerik.Reporting.Drawing.Unit.Mm(15D));
            this.PageSettings.PaperKind = System.Drawing.Printing.PaperKind.A4;
            reportParameter1.Name = "DateFrom";
            reportParameter1.Type = Telerik.Reporting.ReportParameterType.DateTime;
            reportParameter1.Value = "2014/01/01";
            reportParameter2.Name = "DateTo";
            reportParameter2.Type = Telerik.Reporting.ReportParameterType.DateTime;
            reportParameter2.Value = "2014/07/01";
            this.ReportParameters.Add(reportParameter1);
            this.ReportParameters.Add(reportParameter2);
            this.Style.BackgroundColor = System.Drawing.Color.White;
            styleRule1.Selectors.AddRange(new Telerik.Reporting.Drawing.ISelector[] {
            new Telerik.Reporting.Drawing.TypeSelector(typeof(Telerik.Reporting.TextItemBase)),
            new Telerik.Reporting.Drawing.TypeSelector(typeof(Telerik.Reporting.HtmlTextBox))});
            styleRule1.Style.Padding.Left = Telerik.Reporting.Drawing.Unit.Point(2D);
            styleRule1.Style.Padding.Right = Telerik.Reporting.Drawing.Unit.Point(2D);
            this.StyleSheet.AddRange(new Telerik.Reporting.Drawing.StyleRule[] {
            styleRule1});
            this.Width = Telerik.Reporting.Drawing.Unit.Cm(18D);
            ((System.ComponentModel.ISupportInitialize)(this.saleReportFuelType1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.saleReportDispenser1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.literCheckReport1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();

        }
        #endregion

        private Telerik.Reporting.PageHeaderSection pageHeaderSection1;
        private Telerik.Reporting.DetailSection detail;
        private Telerik.Reporting.PageFooterSection pageFooterSection1;
        private Telerik.Reporting.TextBox textBox5;
        private Telerik.Reporting.TextBox textBox6;
        private Telerik.Reporting.TextBox textBox7;
        private Telerik.Reporting.TextBox textBox8;
        private Telerik.Reporting.TextBox titleTextBox;
        private Telerik.Reporting.SubReport subReport1;
        private SaleReportFuelType saleReportFuelType1;
        private Telerik.Reporting.Shape shape2;
        private Telerik.Reporting.SubReport subReport2;
        private SaleReportDispenser saleReportDispenser1;
        private Telerik.Reporting.Shape shape1;
        private Telerik.Reporting.TextBox textBox1;
        private Telerik.Reporting.PictureBox pictureBox1;
        private Telerik.Reporting.TextBox textBox4;
        private Telerik.Reporting.TextBox pageInfoTextBox;
        private Telerik.Reporting.Shape shape3;
        private Telerik.Reporting.SubReport subReport3;
        private LiterCheckReport literCheckReport1;
    }
}