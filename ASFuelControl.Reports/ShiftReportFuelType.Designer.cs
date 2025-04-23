namespace ASFuelControl.Reports
{
    partial class ShiftReportFuelType
    {
        #region Component Designer generated code
        /// <summary>
        /// Required method for telerik Reporting designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            Telerik.Reporting.Group group1 = new Telerik.Reporting.Group();
            Telerik.Reporting.Group group2 = new Telerik.Reporting.Group();
            Telerik.Reporting.ReportParameter reportParameter1 = new Telerik.Reporting.ReportParameter();
            Telerik.Reporting.Drawing.StyleRule styleRule1 = new Telerik.Reporting.Drawing.StyleRule();
            Telerik.Reporting.Drawing.StyleRule styleRule2 = new Telerik.Reporting.Drawing.StyleRule();
            Telerik.Reporting.Drawing.StyleRule styleRule3 = new Telerik.Reporting.Drawing.StyleRule();
            Telerik.Reporting.Drawing.StyleRule styleRule4 = new Telerik.Reporting.Drawing.StyleRule();
            this.labelsGroupFooterSection = new Telerik.Reporting.GroupFooterSection();
            this.labelsGroupHeaderSection = new Telerik.Reporting.GroupHeaderSection();
            this.textBox1 = new Telerik.Reporting.TextBox();
            this.totalPriceCaptionTextBox = new Telerik.Reporting.TextBox();
            this.volumeCaptionTextBox = new Telerik.Reporting.TextBox();
            this.volumeNormalizedCaptionTextBox = new Telerik.Reporting.TextBox();
            this.textBox11 = new Telerik.Reporting.TextBox();
            this.nozzleFuelTypeNameGroupFooterSection = new Telerik.Reporting.GroupFooterSection();
            this.nozzleFuelTypeNameGroupHeaderSection = new Telerik.Reporting.GroupHeaderSection();
            this.volumeNormalizedDataTextBox = new Telerik.Reporting.TextBox();
            this.totalPriceDataTextBox = new Telerik.Reporting.TextBox();
            this.volumeDataTextBox = new Telerik.Reporting.TextBox();
            this.textBox2 = new Telerik.Reporting.TextBox();
            this.textBox12 = new Telerik.Reporting.TextBox();
            this.shape1 = new Telerik.Reporting.Shape();
            this.Sales = new Telerik.Reporting.OpenAccessDataSource();
            this.pageFooter = new Telerik.Reporting.PageFooterSection();
            this.currentTimeTextBox = new Telerik.Reporting.TextBox();
            this.pageInfoTextBox = new Telerik.Reporting.TextBox();
            this.reportHeader = new Telerik.Reporting.ReportHeaderSection();
            this.textBox10 = new Telerik.Reporting.TextBox();
            this.reportFooter = new Telerik.Reporting.ReportFooterSection();
            this.textBox9 = new Telerik.Reporting.TextBox();
            this.textBox8 = new Telerik.Reporting.TextBox();
            this.textBox7 = new Telerik.Reporting.TextBox();
            this.textBox13 = new Telerik.Reporting.TextBox();
            this.textBox17 = new Telerik.Reporting.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            // 
            // labelsGroupFooterSection
            // 
            this.labelsGroupFooterSection.Height = Telerik.Reporting.Drawing.Unit.Cm(1.1000014543533325D);
            this.labelsGroupFooterSection.Name = "labelsGroupFooterSection";
            this.labelsGroupFooterSection.Style.Visible = false;
            // 
            // labelsGroupHeaderSection
            // 
            this.labelsGroupHeaderSection.Height = Telerik.Reporting.Drawing.Unit.Cm(1.0499999523162842D);
            this.labelsGroupHeaderSection.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.textBox1,
            this.totalPriceCaptionTextBox,
            this.volumeCaptionTextBox,
            this.volumeNormalizedCaptionTextBox,
            this.textBox11});
            this.labelsGroupHeaderSection.Name = "labelsGroupHeaderSection";
            this.labelsGroupHeaderSection.PrintOnEveryPage = true;
            // 
            // textBox1
            // 
            this.textBox1.CanGrow = true;
            this.textBox1.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(4.9470834732055664D), Telerik.Reporting.Drawing.Unit.Cm(0.95291662216186523D));
            this.textBox1.Style.Color = System.Drawing.Color.White;
            this.textBox1.Style.Font.Name = "Verdana";
            this.textBox1.Style.Padding.Left = Telerik.Reporting.Drawing.Unit.Cm(0.20000000298023224D);
            this.textBox1.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            this.textBox1.StyleName = "Caption";
            this.textBox1.Value = "Καύσιμο";
            // 
            // totalPriceCaptionTextBox
            // 
            this.totalPriceCaptionTextBox.CanGrow = true;
            this.totalPriceCaptionTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(5.0002002716064453D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.totalPriceCaptionTextBox.Name = "totalPriceCaptionTextBox";
            this.totalPriceCaptionTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.5699999332427979D), Telerik.Reporting.Drawing.Unit.Cm(0.95291662216186523D));
            this.totalPriceCaptionTextBox.Style.Color = System.Drawing.Color.White;
            this.totalPriceCaptionTextBox.Style.Font.Name = "Verdana";
            this.totalPriceCaptionTextBox.Style.Padding.Left = Telerik.Reporting.Drawing.Unit.Cm(0.20000000298023224D);
            this.totalPriceCaptionTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            this.totalPriceCaptionTextBox.StyleName = "Caption";
            this.totalPriceCaptionTextBox.Value = "Σύνολο";
            // 
            // volumeCaptionTextBox
            // 
            this.volumeCaptionTextBox.CanGrow = true;
            this.volumeCaptionTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(7.6101999282836914D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.volumeCaptionTextBox.Name = "volumeCaptionTextBox";
            this.volumeCaptionTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.5699999332427979D), Telerik.Reporting.Drawing.Unit.Cm(0.95291662216186523D));
            this.volumeCaptionTextBox.Style.Color = System.Drawing.Color.White;
            this.volumeCaptionTextBox.Style.Font.Name = "Verdana";
            this.volumeCaptionTextBox.Style.Padding.Left = Telerik.Reporting.Drawing.Unit.Cm(0.20000000298023224D);
            this.volumeCaptionTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            this.volumeCaptionTextBox.StyleName = "Caption";
            this.volumeCaptionTextBox.Value = "Όγκος";
            // 
            // volumeNormalizedCaptionTextBox
            // 
            this.volumeNormalizedCaptionTextBox.CanGrow = true;
            this.volumeNormalizedCaptionTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(10.230199813842773D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.volumeNormalizedCaptionTextBox.Name = "volumeNormalizedCaptionTextBox";
            this.volumeNormalizedCaptionTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.5699999332427979D), Telerik.Reporting.Drawing.Unit.Cm(0.95291662216186523D));
            this.volumeNormalizedCaptionTextBox.Style.Color = System.Drawing.Color.White;
            this.volumeNormalizedCaptionTextBox.Style.Font.Name = "Verdana";
            this.volumeNormalizedCaptionTextBox.Style.Padding.Left = Telerik.Reporting.Drawing.Unit.Cm(0.20000000298023224D);
            this.volumeNormalizedCaptionTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            this.volumeNormalizedCaptionTextBox.StyleName = "Caption";
            this.volumeNormalizedCaptionTextBox.Value = "Κινήσεις";
            // 
            // textBox11
            // 
            this.textBox11.CanGrow = true;
            this.textBox11.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(12.850198745727539D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.textBox11.Name = "textBox11";
            this.textBox11.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.5699999332427979D), Telerik.Reporting.Drawing.Unit.Cm(0.95291662216186523D));
            this.textBox11.Style.Color = System.Drawing.Color.White;
            this.textBox11.Style.Font.Name = "Verdana";
            this.textBox11.Style.Padding.Left = Telerik.Reporting.Drawing.Unit.Cm(0.20000000298023224D);
            this.textBox11.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            this.textBox11.StyleName = "Caption";
            this.textBox11.Value = "€ / Κινήση";
            // 
            // nozzleFuelTypeNameGroupFooterSection
            // 
            this.nozzleFuelTypeNameGroupFooterSection.Height = Telerik.Reporting.Drawing.Unit.Cm(1.1174991130828857D);
            this.nozzleFuelTypeNameGroupFooterSection.Name = "nozzleFuelTypeNameGroupFooterSection";
            this.nozzleFuelTypeNameGroupFooterSection.Style.Visible = false;
            // 
            // nozzleFuelTypeNameGroupHeaderSection
            // 
            this.nozzleFuelTypeNameGroupHeaderSection.Height = Telerik.Reporting.Drawing.Unit.Cm(0.950000524520874D);
            this.nozzleFuelTypeNameGroupHeaderSection.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.volumeNormalizedDataTextBox,
            this.totalPriceDataTextBox,
            this.volumeDataTextBox,
            this.textBox2,
            this.textBox12,
            this.shape1});
            this.nozzleFuelTypeNameGroupHeaderSection.Name = "nozzleFuelTypeNameGroupHeaderSection";
            // 
            // volumeNormalizedDataTextBox
            // 
            this.volumeNormalizedDataTextBox.CanGrow = true;
            this.volumeNormalizedDataTextBox.Format = "{0:N0}";
            this.volumeNormalizedDataTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(10.229999542236328D), Telerik.Reporting.Drawing.Unit.Cm(0.12021593004465103D));
            this.volumeNormalizedDataTextBox.Name = "volumeNormalizedDataTextBox";
            this.volumeNormalizedDataTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.5699253082275391D), Telerik.Reporting.Drawing.Unit.Cm(0.629584014415741D));
            this.volumeNormalizedDataTextBox.Style.Font.Name = "Verdana";
            this.volumeNormalizedDataTextBox.Style.Padding.Right = Telerik.Reporting.Drawing.Unit.Cm(0.20000000298023224D);
            this.volumeNormalizedDataTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.volumeNormalizedDataTextBox.StyleName = "Data";
            this.volumeNormalizedDataTextBox.Value = "=Count(Fields.SalesTransactionId)";
            // 
            // totalPriceDataTextBox
            // 
            this.totalPriceDataTextBox.CanGrow = true;
            this.totalPriceDataTextBox.Format = "{0:N2}€";
            this.totalPriceDataTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(5D), Telerik.Reporting.Drawing.Unit.Cm(0.12021593004465103D));
            this.totalPriceDataTextBox.Name = "totalPriceDataTextBox";
            this.totalPriceDataTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.5699753761291504D), Telerik.Reporting.Drawing.Unit.Cm(0.629584014415741D));
            this.totalPriceDataTextBox.Style.Font.Name = "Verdana";
            this.totalPriceDataTextBox.Style.Padding.Right = Telerik.Reporting.Drawing.Unit.Cm(0.20000000298023224D);
            this.totalPriceDataTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.totalPriceDataTextBox.StyleName = "Data";
            this.totalPriceDataTextBox.Value = "=Sum(Fields.TotalPrice)";
            // 
            // volumeDataTextBox
            // 
            this.volumeDataTextBox.CanGrow = true;
            this.volumeDataTextBox.Format = "{0:N2}Lt";
            this.volumeDataTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(7.6099996566772461D), Telerik.Reporting.Drawing.Unit.Cm(0.12021593004465103D));
            this.volumeDataTextBox.Name = "volumeDataTextBox";
            this.volumeDataTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.5699999332427979D), Telerik.Reporting.Drawing.Unit.Cm(0.629584014415741D));
            this.volumeDataTextBox.Style.Font.Name = "Verdana";
            this.volumeDataTextBox.Style.Padding.Right = Telerik.Reporting.Drawing.Unit.Cm(0.20000000298023224D);
            this.volumeDataTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.volumeDataTextBox.StyleName = "Data";
            this.volumeDataTextBox.Value = "=Sum(Fields.Volume)";
            // 
            // textBox2
            // 
            this.textBox2.CanGrow = true;
            this.textBox2.Format = "{0:N2}";
            this.textBox2.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0D), Telerik.Reporting.Drawing.Unit.Cm(0.12021593004465103D));
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(4.9470834732055664D), Telerik.Reporting.Drawing.Unit.Cm(0.629584014415741D));
            this.textBox2.Style.Font.Name = "Verdana";
            this.textBox2.Style.Padding.Left = Telerik.Reporting.Drawing.Unit.Cm(0.20000000298023224D);
            this.textBox2.Style.Padding.Right = Telerik.Reporting.Drawing.Unit.Cm(0D);
            this.textBox2.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Left;
            this.textBox2.StyleName = "Data";
            this.textBox2.Value = "=Fields.Nozzle.FuelType.Name";
            // 
            // textBox12
            // 
            this.textBox12.CanGrow = true;
            this.textBox12.Format = "{0:N2}€";
            this.textBox12.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(12.849998474121094D), Telerik.Reporting.Drawing.Unit.Cm(0.12021593004465103D));
            this.textBox12.Name = "textBox12";
            this.textBox12.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.5698995590209961D), Telerik.Reporting.Drawing.Unit.Cm(0.629584014415741D));
            this.textBox12.Style.Font.Name = "Verdana";
            this.textBox12.Style.Padding.Right = Telerik.Reporting.Drawing.Unit.Cm(0.20000000298023224D);
            this.textBox12.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.textBox12.StyleName = "Data";
            this.textBox12.Value = "= Avg(Fields.TotalPrice)";
            // 
            // shape1
            // 
            this.shape1.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D), Telerik.Reporting.Drawing.Unit.Cm(0.74999982118606567D));
            this.shape1.Name = "shape1";
            this.shape1.ShapeType = new Telerik.Reporting.Drawing.Shapes.LineShape(Telerik.Reporting.Drawing.Shapes.LineDirection.EW);
            this.shape1.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(15.368334770202637D), Telerik.Reporting.Drawing.Unit.Cm(0.19999989867210388D));
            this.shape1.Style.Color = System.Drawing.Color.Silver;
            // 
            // Sales
            // 
            this.Sales.ConnectionString = "ASFuelControl.Reports.Properties.Settings.ASFuelControlDB";
            this.Sales.Name = "Sales";
            this.Sales.ObjectContext = "ASFuelControl.Data.DatabaseModel, ASFuelControl.Data, Version=1.0.0.0, Culture=ne" +
    "utral, PublicKeyToken=null";
            this.Sales.ObjectContextMember = "SalesTransactions";
            // 
            // pageFooter
            // 
            this.pageFooter.Height = Telerik.Reporting.Drawing.Unit.Cm(1.1058331727981567D);
            this.pageFooter.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.currentTimeTextBox,
            this.pageInfoTextBox});
            this.pageFooter.Name = "pageFooter";
            // 
            // currentTimeTextBox
            // 
            this.currentTimeTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.currentTimeTextBox.Name = "currentTimeTextBox";
            this.currentTimeTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(7.8277082443237305D), Telerik.Reporting.Drawing.Unit.Cm(0.99999988079071045D));
            this.currentTimeTextBox.Style.Font.Name = "Verdana";
            this.currentTimeTextBox.StyleName = "PageInfo";
            this.currentTimeTextBox.Value = "=NOW()";
            // 
            // pageInfoTextBox
            // 
            this.pageInfoTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(7.9335417747497559D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.pageInfoTextBox.Name = "pageInfoTextBox";
            this.pageInfoTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(7.4877095222473145D), Telerik.Reporting.Drawing.Unit.Cm(0.99999988079071045D));
            this.pageInfoTextBox.Style.Font.Name = "Verdana";
            this.pageInfoTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.pageInfoTextBox.StyleName = "PageInfo";
            this.pageInfoTextBox.Value = "=PageNumber";
            // 
            // reportHeader
            // 
            this.reportHeader.Height = Telerik.Reporting.Drawing.Unit.Cm(0.89999991655349731D);
            this.reportHeader.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.textBox10});
            this.reportHeader.Name = "reportHeader";
            // 
            // textBox10
            // 
            this.textBox10.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0.052816543728113174D), Telerik.Reporting.Drawing.Unit.Cm(9.9921220680698752E-05D));
            this.textBox10.Name = "textBox10";
            this.textBox10.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(15.368434906005859D), Telerik.Reporting.Drawing.Unit.Cm(0.79990005493164062D));
            this.textBox10.Style.Font.Name = "Verdana";
            this.textBox10.Style.Font.Size = Telerik.Reporting.Drawing.Unit.Point(14D);
            this.textBox10.StyleName = "Title";
            this.textBox10.Value = "Εκροές ανά τύπο καυσίμου";
            // 
            // reportFooter
            // 
            this.reportFooter.Height = Telerik.Reporting.Drawing.Unit.Cm(0.59416592121124268D);
            this.reportFooter.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.textBox9,
            this.textBox8,
            this.textBox7,
            this.textBox13,
            this.textBox17});
            this.reportFooter.Name = "reportFooter";
            // 
            // textBox9
            // 
            this.textBox9.Format = "{0:N0}";
            this.textBox9.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(10.229999542236328D), Telerik.Reporting.Drawing.Unit.Cm(0.00010012308484874666D));
            this.textBox9.Name = "textBox9";
            this.textBox9.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.5699255466461182D), Telerik.Reporting.Drawing.Unit.Cm(0.47656816244125366D));
            this.textBox9.Style.Font.Bold = true;
            this.textBox9.Style.Font.Name = "Verdana";
            this.textBox9.Style.Padding.Right = Telerik.Reporting.Drawing.Unit.Cm(0.20000000298023224D);
            this.textBox9.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.textBox9.Value = "=Count(Fields.SalesTransactionId)";
            // 
            // textBox8
            // 
            this.textBox8.Format = "{0:N2}Lt";
            this.textBox8.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(7.6099996566772461D), Telerik.Reporting.Drawing.Unit.Cm(0.00010012308484874666D));
            this.textBox8.Name = "textBox8";
            this.textBox8.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.5865581035614014D), Telerik.Reporting.Drawing.Unit.Cm(0.47656816244125366D));
            this.textBox8.Style.Font.Bold = true;
            this.textBox8.Style.Font.Name = "Verdana";
            this.textBox8.Style.Padding.Right = Telerik.Reporting.Drawing.Unit.Cm(0.20000000298023224D);
            this.textBox8.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.textBox8.Value = "=Sum(Fields.Volume)";
            // 
            // textBox7
            // 
            this.textBox7.Format = "{0:N2}€";
            this.textBox7.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(5D), Telerik.Reporting.Drawing.Unit.Cm(0.00020024616969749332D));
            this.textBox7.Name = "textBox7";
            this.textBox7.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.5699758529663086D), Telerik.Reporting.Drawing.Unit.Cm(0.476468026638031D));
            this.textBox7.Style.Font.Bold = true;
            this.textBox7.Style.Font.Name = "Verdana";
            this.textBox7.Style.Padding.Right = Telerik.Reporting.Drawing.Unit.Cm(0.20000000298023224D);
            this.textBox7.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.textBox7.Value = "=Sum(Fields.TotalPrice)";
            // 
            // textBox13
            // 
            this.textBox13.Format = "{0:N2}€";
            this.textBox13.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(12.849998474121094D), Telerik.Reporting.Drawing.Unit.Cm(0.00010012308484874666D));
            this.textBox13.Name = "textBox13";
            this.textBox13.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.5712521076202393D), Telerik.Reporting.Drawing.Unit.Cm(0.47656816244125366D));
            this.textBox13.Style.Font.Bold = true;
            this.textBox13.Style.Font.Name = "Verdana";
            this.textBox13.Style.Padding.Right = Telerik.Reporting.Drawing.Unit.Cm(0.20000000298023224D);
            this.textBox13.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.textBox13.Value = "=Avg(Fields.TotalPrice)";
            // 
            // textBox17
            // 
            this.textBox17.CanGrow = true;
            this.textBox17.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D), Telerik.Reporting.Drawing.Unit.Cm(0.026142621412873268D));
            this.textBox17.Name = "textBox17";
            this.textBox17.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(3.8470833301544189D), Telerik.Reporting.Drawing.Unit.Cm(0.4505251944065094D));
            this.textBox17.Style.Font.Bold = true;
            this.textBox17.Style.Font.Name = "Verdana";
            this.textBox17.Style.Font.Size = Telerik.Reporting.Drawing.Unit.Point(10D);
            this.textBox17.Style.Padding.Left = Telerik.Reporting.Drawing.Unit.Cm(0.20000000298023224D);
            this.textBox17.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Left;
            this.textBox17.StyleName = "Data";
            this.textBox17.Value = "Γενικό Σύνολο";
            // 
            // ShiftReportFuelType
            // 
            this.DataSource = this.Sales;
            this.Filters.Add(new Telerik.Reporting.Filter("=IsNull(Fields.ShiftId, \"\").ToString().ToUpper()", Telerik.Reporting.FilterOperator.Equal, "=Parameters.ShiftId.Value.ToUpper()"));
            this.Filters.Add(new Telerik.Reporting.Filter("= IsNull(Fields.IsErrorResolving, false)", Telerik.Reporting.FilterOperator.Equal, "false"));
            group1.GroupFooter = this.labelsGroupFooterSection;
            group1.GroupHeader = this.labelsGroupHeaderSection;
            group1.Name = "labelsGroup";
            group2.GroupFooter = this.nozzleFuelTypeNameGroupFooterSection;
            group2.GroupHeader = this.nozzleFuelTypeNameGroupHeaderSection;
            group2.Groupings.Add(new Telerik.Reporting.Grouping("=Fields.Nozzle.FuelType.Name"));
            group2.Name = "nozzleFuelTypeNameGroup";
            this.Groups.AddRange(new Telerik.Reporting.Group[] {
            group1,
            group2});
            this.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.labelsGroupHeaderSection,
            this.labelsGroupFooterSection,
            this.nozzleFuelTypeNameGroupHeaderSection,
            this.nozzleFuelTypeNameGroupFooterSection,
            this.pageFooter,
            this.reportHeader,
            this.reportFooter});
            this.Name = "ShiftReport";
            this.PageSettings.Margins = new Telerik.Reporting.Drawing.MarginsU(Telerik.Reporting.Drawing.Unit.Mm(25.399999618530273D), Telerik.Reporting.Drawing.Unit.Mm(25.399999618530273D), Telerik.Reporting.Drawing.Unit.Mm(25.399999618530273D), Telerik.Reporting.Drawing.Unit.Mm(25.399999618530273D));
            this.PageSettings.PaperKind = System.Drawing.Printing.PaperKind.A4;
            reportParameter1.Name = "ShiftId";
            reportParameter1.Value = "1377a9d5-2612-462c-bc4d-ea9e420f8d34";
            this.ReportParameters.Add(reportParameter1);
            this.Style.BackgroundColor = System.Drawing.Color.White;
            styleRule1.Selectors.AddRange(new Telerik.Reporting.Drawing.ISelector[] {
            new Telerik.Reporting.Drawing.StyleSelector("Title")});
            styleRule1.Style.Color = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(34)))), ((int)(((byte)(77)))));
            styleRule1.Style.Font.Name = "Calibri";
            styleRule1.Style.Font.Size = Telerik.Reporting.Drawing.Unit.Point(18D);
            styleRule2.Selectors.AddRange(new Telerik.Reporting.Drawing.ISelector[] {
            new Telerik.Reporting.Drawing.StyleSelector("Caption")});
            styleRule2.Style.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(121)))), ((int)(((byte)(167)))), ((int)(((byte)(227)))));
            styleRule2.Style.Color = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(34)))), ((int)(((byte)(77)))));
            styleRule2.Style.Font.Name = "Calibri";
            styleRule2.Style.Font.Size = Telerik.Reporting.Drawing.Unit.Point(10D);
            styleRule2.Style.VerticalAlign = Telerik.Reporting.Drawing.VerticalAlign.Middle;
            styleRule3.Selectors.AddRange(new Telerik.Reporting.Drawing.ISelector[] {
            new Telerik.Reporting.Drawing.StyleSelector("Data")});
            styleRule3.Style.Color = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(34)))), ((int)(((byte)(77)))));
            styleRule3.Style.Font.Name = "Calibri";
            styleRule3.Style.Font.Size = Telerik.Reporting.Drawing.Unit.Point(9D);
            styleRule3.Style.VerticalAlign = Telerik.Reporting.Drawing.VerticalAlign.Middle;
            styleRule4.Selectors.AddRange(new Telerik.Reporting.Drawing.ISelector[] {
            new Telerik.Reporting.Drawing.StyleSelector("PageInfo")});
            styleRule4.Style.Color = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(34)))), ((int)(((byte)(77)))));
            styleRule4.Style.Font.Name = "Calibri";
            styleRule4.Style.Font.Size = Telerik.Reporting.Drawing.Unit.Point(8D);
            styleRule4.Style.VerticalAlign = Telerik.Reporting.Drawing.VerticalAlign.Middle;
            this.StyleSheet.AddRange(new Telerik.Reporting.Drawing.StyleRule[] {
            styleRule1,
            styleRule2,
            styleRule3,
            styleRule4});
            this.Width = Telerik.Reporting.Drawing.Unit.Cm(15.42125129699707D);
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();

        }
        #endregion

        private Telerik.Reporting.OpenAccessDataSource Sales;
        private Telerik.Reporting.GroupHeaderSection nozzleFuelTypeNameGroupHeaderSection;
        private Telerik.Reporting.TextBox textBox2;
        private Telerik.Reporting.TextBox totalPriceDataTextBox;
        private Telerik.Reporting.TextBox volumeDataTextBox;
        private Telerik.Reporting.TextBox volumeNormalizedDataTextBox;
        private Telerik.Reporting.GroupFooterSection nozzleFuelTypeNameGroupFooterSection;
        private Telerik.Reporting.PageFooterSection pageFooter;
        private Telerik.Reporting.TextBox currentTimeTextBox;
        private Telerik.Reporting.TextBox pageInfoTextBox;
        private Telerik.Reporting.ReportHeaderSection reportHeader;
        private Telerik.Reporting.ReportFooterSection reportFooter;
        private Telerik.Reporting.TextBox textBox7;
        private Telerik.Reporting.TextBox textBox8;
        private Telerik.Reporting.TextBox textBox9;
        private Telerik.Reporting.GroupFooterSection labelsGroupFooterSection;
        private Telerik.Reporting.TextBox volumeNormalizedCaptionTextBox;
        private Telerik.Reporting.TextBox volumeCaptionTextBox;
        private Telerik.Reporting.TextBox totalPriceCaptionTextBox;
        private Telerik.Reporting.TextBox textBox1;
        private Telerik.Reporting.GroupHeaderSection labelsGroupHeaderSection;
        private Telerik.Reporting.TextBox textBox10;
        private Telerik.Reporting.TextBox textBox12;
        private Telerik.Reporting.TextBox textBox11;
        private Telerik.Reporting.TextBox textBox13;
        private Telerik.Reporting.TextBox textBox17;
        private Telerik.Reporting.Shape shape1;

    }
}