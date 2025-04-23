namespace BalanceExporter.Reports
{
    partial class TankFillingBalanceReport
    {
        #region Component Designer generated code
        /// <summary>
        /// Required method for telerik Reporting designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            Telerik.Reporting.Drawing.StyleRule styleRule1 = new Telerik.Reporting.Drawing.StyleRule();
            this.detail = new Telerik.Reporting.DetailSection();
            this.diffPercentageDataTextBox = new Telerik.Reporting.TextBox();
            this.fuelTypeIdDataTextBox = new Telerik.Reporting.TextBox();
            this.diffVolumeNormalizedDataTextBox = new Telerik.Reporting.TextBox();
            this.diffVolumeDataTextBox = new Telerik.Reporting.TextBox();
            this.diffPercentafeNormelizedDataTextBox = new Telerik.Reporting.TextBox();
            this.textBox4 = new Telerik.Reporting.TextBox();
            this.textBox5 = new Telerik.Reporting.TextBox();
            this.pageFooterSection1 = new Telerik.Reporting.PageFooterSection();
            this.fillingDS = new Telerik.Reporting.ObjectDataSource();
            this.titleTextBox = new Telerik.Reporting.TextBox();
            this.diffPercentageCaptionTextBox = new Telerik.Reporting.TextBox();
            this.diffPercentafeNormelizedCaptionTextBox = new Telerik.Reporting.TextBox();
            this.diffVolumeNormalizedCaptionTextBox = new Telerik.Reporting.TextBox();
            this.diffVolumeCaptionTextBox = new Telerik.Reporting.TextBox();
            this.textBox1 = new Telerik.Reporting.TextBox();
            this.textBox2 = new Telerik.Reporting.TextBox();
            this.textBox3 = new Telerik.Reporting.TextBox();
            this.reportHeaderSection1 = new Telerik.Reporting.ReportHeaderSection();
            this.shape5 = new Telerik.Reporting.Shape();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            // 
            // detail
            // 
            this.detail.Height = Telerik.Reporting.Drawing.Unit.Cm(0.7999998927116394D);
            this.detail.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.diffPercentageDataTextBox,
            this.fuelTypeIdDataTextBox,
            this.diffVolumeNormalizedDataTextBox,
            this.diffVolumeDataTextBox,
            this.diffPercentafeNormelizedDataTextBox,
            this.textBox4,
            this.textBox5});
            this.detail.Name = "detail";
            // 
            // diffPercentageDataTextBox
            // 
            this.diffPercentageDataTextBox.CanGrow = true;
            this.diffPercentageDataTextBox.Format = "{0:N2} Lt";
            this.diffPercentageDataTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(17.040323257446289D), Telerik.Reporting.Drawing.Unit.Cm(0.20000030100345612D));
            this.diffPercentageDataTextBox.Name = "diffPercentageDataTextBox";
            this.diffPercentageDataTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(3D), Telerik.Reporting.Drawing.Unit.Cm(0.49426770210266113D));
            this.diffPercentageDataTextBox.Style.BorderStyle.Default = Telerik.Reporting.Drawing.BorderType.Solid;
            this.diffPercentageDataTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.diffPercentageDataTextBox.StyleName = "Data";
            this.diffPercentageDataTextBox.Value = "= Fields.Invoiced15";
            // 
            // fuelTypeIdDataTextBox
            // 
            this.fuelTypeIdDataTextBox.CanGrow = true;
            this.fuelTypeIdDataTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0.10000000149011612D), Telerik.Reporting.Drawing.Unit.Cm(0.20000030100345612D));
            this.fuelTypeIdDataTextBox.Name = "fuelTypeIdDataTextBox";
            this.fuelTypeIdDataTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(7.0993103981018066D), Telerik.Reporting.Drawing.Unit.Cm(0.4942685067653656D));
            this.fuelTypeIdDataTextBox.Style.BorderStyle.Default = Telerik.Reporting.Drawing.BorderType.Solid;
            this.fuelTypeIdDataTextBox.StyleName = "Data";
            this.fuelTypeIdDataTextBox.Value = "= Fields.FuelTypeName + \" (\" + Fields.FuelTypeCode + \")\"";
            // 
            // diffVolumeNormalizedDataTextBox
            // 
            this.diffVolumeNormalizedDataTextBox.CanGrow = true;
            this.diffVolumeNormalizedDataTextBox.Format = "{0:N2} Lt";
            this.diffVolumeNormalizedDataTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(10.813441276550293D), Telerik.Reporting.Drawing.Unit.Cm(0.20000030100345612D));
            this.diffVolumeNormalizedDataTextBox.Name = "diffVolumeNormalizedDataTextBox";
            this.diffVolumeNormalizedDataTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(3D), Telerik.Reporting.Drawing.Unit.Cm(0.49426770210266113D));
            this.diffVolumeNormalizedDataTextBox.Style.BorderStyle.Default = Telerik.Reporting.Drawing.BorderType.Solid;
            this.diffVolumeNormalizedDataTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.diffVolumeNormalizedDataTextBox.StyleName = "Data";
            this.diffVolumeNormalizedDataTextBox.Value = "= Fields.Deliveries15";
            // 
            // diffVolumeDataTextBox
            // 
            this.diffVolumeDataTextBox.CanGrow = true;
            this.diffVolumeDataTextBox.Format = "{0:N2} Lt";
            this.diffVolumeDataTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(7.6999998092651367D), Telerik.Reporting.Drawing.Unit.Cm(0.20000030100345612D));
            this.diffVolumeDataTextBox.Name = "diffVolumeDataTextBox";
            this.diffVolumeDataTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(3D), Telerik.Reporting.Drawing.Unit.Cm(0.49426770210266113D));
            this.diffVolumeDataTextBox.Style.BorderStyle.Default = Telerik.Reporting.Drawing.BorderType.Solid;
            this.diffVolumeDataTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.diffVolumeDataTextBox.StyleName = "Data";
            this.diffVolumeDataTextBox.Value = "= Fields.Deliveries";
            // 
            // diffPercentafeNormelizedDataTextBox
            // 
            this.diffPercentafeNormelizedDataTextBox.CanGrow = true;
            this.diffPercentafeNormelizedDataTextBox.Format = "{0:N2} Lt";
            this.diffPercentafeNormelizedDataTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(13.926881790161133D), Telerik.Reporting.Drawing.Unit.Cm(0.20000030100345612D));
            this.diffPercentafeNormelizedDataTextBox.Name = "diffPercentafeNormelizedDataTextBox";
            this.diffPercentafeNormelizedDataTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(3D), Telerik.Reporting.Drawing.Unit.Cm(0.49426770210266113D));
            this.diffPercentafeNormelizedDataTextBox.Style.BorderStyle.Default = Telerik.Reporting.Drawing.BorderType.Solid;
            this.diffPercentafeNormelizedDataTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.diffPercentafeNormelizedDataTextBox.StyleName = "Data";
            this.diffPercentafeNormelizedDataTextBox.Value = "= Fields.Invoiced";
            // 
            // textBox4
            // 
            this.textBox4.CanGrow = true;
            this.textBox4.Format = "{0:N2} Lt";
            this.textBox4.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(20.153764724731445D), Telerik.Reporting.Drawing.Unit.Cm(0.20000030100345612D));
            this.textBox4.Name = "textBox4";
            this.textBox4.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(3D), Telerik.Reporting.Drawing.Unit.Cm(0.49426770210266113D));
            this.textBox4.Style.BorderStyle.Default = Telerik.Reporting.Drawing.BorderType.Solid;
            this.textBox4.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.textBox4.StyleName = "Data";
            this.textBox4.Value = "= Fields.Additional";
            // 
            // textBox5
            // 
            this.textBox5.CanGrow = true;
            this.textBox5.Format = "{0:N2} Lt";
            this.textBox5.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(23.2672061920166D), Telerik.Reporting.Drawing.Unit.Cm(0.20000030100345612D));
            this.textBox5.Name = "textBox5";
            this.textBox5.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(3D), Telerik.Reporting.Drawing.Unit.Cm(0.49426770210266113D));
            this.textBox5.Style.BorderStyle.Default = Telerik.Reporting.Drawing.BorderType.Solid;
            this.textBox5.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.textBox5.StyleName = "Data";
            this.textBox5.Value = "= Fields.Additional15";
            // 
            // pageFooterSection1
            // 
            this.pageFooterSection1.Height = Telerik.Reporting.Drawing.Unit.Cm(0.30019959807395935D);
            this.pageFooterSection1.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.shape5});
            this.pageFooterSection1.Name = "pageFooterSection1";
            this.pageFooterSection1.Style.Visible = true;
            // 
            // fillingDS
            // 
            this.fillingDS.DataSource = "ASFuelControl.Reports.BalanceReports.BalanceDS+TankFillingDataDataTable, ASFuelCo" +
    "ntrol.Reports, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";
            this.fillingDS.Name = "fillingDS";
            // 
            // titleTextBox
            // 
            this.titleTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0.00010012308484874666D), Telerik.Reporting.Drawing.Unit.Cm(0.00010012308484874666D));
            this.titleTextBox.Name = "titleTextBox";
            this.titleTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(24.399999618530273D), Telerik.Reporting.Drawing.Unit.Cm(0.88999998569488525D));
            this.titleTextBox.Style.Color = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(58)))), ((int)(((byte)(112)))));
            this.titleTextBox.Style.Font.Name = "Tahoma";
            this.titleTextBox.Style.Font.Size = Telerik.Reporting.Drawing.Unit.Point(18D);
            this.titleTextBox.StyleName = "Title";
            this.titleTextBox.Value = "Εισροές ανά τύπο Καυσίμου";
            // 
            // diffPercentageCaptionTextBox
            // 
            this.diffPercentageCaptionTextBox.CanGrow = true;
            this.diffPercentageCaptionTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(17.040323257446289D), Telerik.Reporting.Drawing.Unit.Cm(1.0000001192092896D));
            this.diffPercentageCaptionTextBox.Name = "diffPercentageCaptionTextBox";
            this.diffPercentageCaptionTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(3D), Telerik.Reporting.Drawing.Unit.Cm(0.8997003436088562D));
            this.diffPercentageCaptionTextBox.Style.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(58)))), ((int)(((byte)(112)))));
            this.diffPercentageCaptionTextBox.Style.Color = System.Drawing.Color.White;
            this.diffPercentageCaptionTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            this.diffPercentageCaptionTextBox.StyleName = "Caption";
            this.diffPercentageCaptionTextBox.Value = "Παραστατικα     15 οC";
            // 
            // diffPercentafeNormelizedCaptionTextBox
            // 
            this.diffPercentafeNormelizedCaptionTextBox.CanGrow = true;
            this.diffPercentafeNormelizedCaptionTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(13.926881790161133D), Telerik.Reporting.Drawing.Unit.Cm(1.0000001192092896D));
            this.diffPercentafeNormelizedCaptionTextBox.Name = "diffPercentafeNormelizedCaptionTextBox";
            this.diffPercentafeNormelizedCaptionTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(3D), Telerik.Reporting.Drawing.Unit.Cm(0.8997003436088562D));
            this.diffPercentafeNormelizedCaptionTextBox.Style.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(58)))), ((int)(((byte)(112)))));
            this.diffPercentafeNormelizedCaptionTextBox.Style.Color = System.Drawing.Color.White;
            this.diffPercentafeNormelizedCaptionTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            this.diffPercentafeNormelizedCaptionTextBox.StyleName = "Caption";
            this.diffPercentafeNormelizedCaptionTextBox.Value = "Παραστατικα";
            // 
            // diffVolumeNormalizedCaptionTextBox
            // 
            this.diffVolumeNormalizedCaptionTextBox.CanGrow = true;
            this.diffVolumeNormalizedCaptionTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(10.813441276550293D), Telerik.Reporting.Drawing.Unit.Cm(1.0000001192092896D));
            this.diffVolumeNormalizedCaptionTextBox.Name = "diffVolumeNormalizedCaptionTextBox";
            this.diffVolumeNormalizedCaptionTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(3D), Telerik.Reporting.Drawing.Unit.Cm(0.8997003436088562D));
            this.diffVolumeNormalizedCaptionTextBox.Style.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(58)))), ((int)(((byte)(112)))));
            this.diffVolumeNormalizedCaptionTextBox.Style.Color = System.Drawing.Color.White;
            this.diffVolumeNormalizedCaptionTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            this.diffVolumeNormalizedCaptionTextBox.StyleName = "Caption";
            this.diffVolumeNormalizedCaptionTextBox.Value = "Παραλαβές 15οC";
            // 
            // diffVolumeCaptionTextBox
            // 
            this.diffVolumeCaptionTextBox.CanGrow = true;
            this.diffVolumeCaptionTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(7.6999998092651367D), Telerik.Reporting.Drawing.Unit.Cm(1.0000001192092896D));
            this.diffVolumeCaptionTextBox.Name = "diffVolumeCaptionTextBox";
            this.diffVolumeCaptionTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(3D), Telerik.Reporting.Drawing.Unit.Cm(0.8997003436088562D));
            this.diffVolumeCaptionTextBox.Style.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(58)))), ((int)(((byte)(112)))));
            this.diffVolumeCaptionTextBox.Style.Color = System.Drawing.Color.White;
            this.diffVolumeCaptionTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            this.diffVolumeCaptionTextBox.StyleName = "Caption";
            this.diffVolumeCaptionTextBox.Value = "Παραλαβές";
            // 
            // textBox1
            // 
            this.textBox1.CanGrow = true;
            this.textBox1.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0.10000000149011612D), Telerik.Reporting.Drawing.Unit.Cm(1.0000001192092896D));
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(7.0993103981018066D), Telerik.Reporting.Drawing.Unit.Cm(0.8997003436088562D));
            this.textBox1.Style.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(58)))), ((int)(((byte)(112)))));
            this.textBox1.Style.Color = System.Drawing.Color.White;
            this.textBox1.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            this.textBox1.StyleName = "Caption";
            this.textBox1.Value = "Τύπος Καυσίμου";
            // 
            // textBox2
            // 
            this.textBox2.CanGrow = true;
            this.textBox2.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(20.153764724731445D), Telerik.Reporting.Drawing.Unit.Cm(1.0000001192092896D));
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(3D), Telerik.Reporting.Drawing.Unit.Cm(0.8997003436088562D));
            this.textBox2.Style.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(58)))), ((int)(((byte)(112)))));
            this.textBox2.Style.Color = System.Drawing.Color.White;
            this.textBox2.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            this.textBox2.StyleName = "Caption";
            this.textBox2.Value = "Άλλα";
            // 
            // textBox3
            // 
            this.textBox3.CanGrow = true;
            this.textBox3.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(23.2672061920166D), Telerik.Reporting.Drawing.Unit.Cm(1.0000001192092896D));
            this.textBox3.Name = "textBox3";
            this.textBox3.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(3D), Telerik.Reporting.Drawing.Unit.Cm(0.8997003436088562D));
            this.textBox3.Style.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(58)))), ((int)(((byte)(112)))));
            this.textBox3.Style.Color = System.Drawing.Color.White;
            this.textBox3.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            this.textBox3.StyleName = "Caption";
            this.textBox3.Value = "Άλλα 15 οC";
            // 
            // reportHeaderSection1
            // 
            this.reportHeaderSection1.Height = Telerik.Reporting.Drawing.Unit.Cm(1.8998003005981445D);
            this.reportHeaderSection1.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.titleTextBox,
            this.diffPercentageCaptionTextBox,
            this.diffPercentafeNormelizedCaptionTextBox,
            this.diffVolumeNormalizedCaptionTextBox,
            this.diffVolumeCaptionTextBox,
            this.textBox1,
            this.textBox2,
            this.textBox3});
            this.reportHeaderSection1.Name = "reportHeaderSection1";
            // 
            // shape5
            // 
            this.shape5.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0D), Telerik.Reporting.Drawing.Unit.Cm(0.00010012308484874666D));
            this.shape5.Name = "shape5";
            this.shape5.ShapeType = new Telerik.Reporting.Drawing.Shapes.LineShape(Telerik.Reporting.Drawing.Shapes.LineDirection.EW);
            this.shape5.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(26.646984100341797D), Telerik.Reporting.Drawing.Unit.Cm(0.24175122380256653D));
            this.shape5.Style.Color = System.Drawing.Color.Black;
            this.shape5.Style.LineWidth = Telerik.Reporting.Drawing.Unit.Point(4D);
            // 
            // TankFillingBalanceReport
            // 
            this.DataSource = this.fillingDS;
            this.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.detail,
            this.pageFooterSection1,
            this.reportHeaderSection1});
            this.Name = "TankFillingBalanceReport";
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
            this.Width = Telerik.Reporting.Drawing.Unit.Cm(26.646984100341797D);
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();

        }
        #endregion

        private Telerik.Reporting.DetailSection detail;
        private Telerik.Reporting.PageFooterSection pageFooterSection1;
        private Telerik.Reporting.ObjectDataSource fillingDS;
        private Telerik.Reporting.TextBox diffPercentageDataTextBox;
        private Telerik.Reporting.TextBox fuelTypeIdDataTextBox;
        private Telerik.Reporting.TextBox diffVolumeNormalizedDataTextBox;
        private Telerik.Reporting.TextBox diffVolumeDataTextBox;
        private Telerik.Reporting.TextBox diffPercentafeNormelizedDataTextBox;
        private Telerik.Reporting.TextBox textBox4;
        private Telerik.Reporting.TextBox textBox5;
        private Telerik.Reporting.TextBox titleTextBox;
        private Telerik.Reporting.TextBox diffPercentageCaptionTextBox;
        private Telerik.Reporting.TextBox diffPercentafeNormelizedCaptionTextBox;
        private Telerik.Reporting.TextBox diffVolumeNormalizedCaptionTextBox;
        private Telerik.Reporting.TextBox diffVolumeCaptionTextBox;
        private Telerik.Reporting.TextBox textBox1;
        private Telerik.Reporting.TextBox textBox2;
        private Telerik.Reporting.TextBox textBox3;
        private Telerik.Reporting.ReportHeaderSection reportHeaderSection1;
        private Telerik.Reporting.Shape shape5;
    }
}