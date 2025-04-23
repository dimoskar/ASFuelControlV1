namespace BalanceExporter.Reports
{
    partial class DivergenceBalanceReport
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
            Telerik.Reporting.Drawing.StyleRule styleRule1 = new Telerik.Reporting.Drawing.StyleRule();
            Telerik.Reporting.Drawing.StyleRule styleRule2 = new Telerik.Reporting.Drawing.StyleRule();
            Telerik.Reporting.Drawing.StyleRule styleRule3 = new Telerik.Reporting.Drawing.StyleRule();
            Telerik.Reporting.Drawing.StyleRule styleRule4 = new Telerik.Reporting.Drawing.StyleRule();
            this.divergenceData = new Telerik.Reporting.ObjectDataSource();
            this.labelsGroupHeaderSection = new Telerik.Reporting.GroupHeaderSection();
            this.labelsGroupFooterSection = new Telerik.Reporting.GroupFooterSection();
            this.diffPercentafeNormelizedCaptionTextBox = new Telerik.Reporting.TextBox();
            this.diffPercentageCaptionTextBox = new Telerik.Reporting.TextBox();
            this.diffVolumeCaptionTextBox = new Telerik.Reporting.TextBox();
            this.diffVolumeNormalizedCaptionTextBox = new Telerik.Reporting.TextBox();
            this.fuelTypeIdGroupHeaderSection = new Telerik.Reporting.GroupHeaderSection();
            this.fuelTypeIdGroupFooterSection = new Telerik.Reporting.GroupFooterSection();
            this.fuelTypeIdDataTextBox = new Telerik.Reporting.TextBox();
            this.pageHeader = new Telerik.Reporting.PageHeaderSection();
            this.reportNameTextBox = new Telerik.Reporting.TextBox();
            this.pageFooter = new Telerik.Reporting.PageFooterSection();
            this.reportHeader = new Telerik.Reporting.ReportHeaderSection();
            this.titleTextBox = new Telerik.Reporting.TextBox();
            this.reportFooter = new Telerik.Reporting.ReportFooterSection();
            this.detail = new Telerik.Reporting.DetailSection();
            this.diffPercentafeNormelizedDataTextBox = new Telerik.Reporting.TextBox();
            this.diffPercentageDataTextBox = new Telerik.Reporting.TextBox();
            this.diffVolumeDataTextBox = new Telerik.Reporting.TextBox();
            this.diffVolumeNormalizedDataTextBox = new Telerik.Reporting.TextBox();
            this.textBox1 = new Telerik.Reporting.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            // 
            // divergenceData
            // 
            this.divergenceData.DataMember = "FuelTypeData";
            this.divergenceData.DataSource = "ASFuelControl.Reports.BalanceReports.BalanceDS, ASFuelControl.Reports, Version=1." +
    "0.0.0, Culture=neutral, PublicKeyToken=null";
            this.divergenceData.Name = "divergenceData";
            // 
            // labelsGroupHeaderSection
            // 
            this.labelsGroupHeaderSection.Height = Telerik.Reporting.Drawing.Unit.Cm(0.70000046491622925D);
            this.labelsGroupHeaderSection.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.textBox1,
            this.diffVolumeCaptionTextBox,
            this.diffVolumeNormalizedCaptionTextBox,
            this.diffPercentafeNormelizedCaptionTextBox,
            this.diffPercentageCaptionTextBox});
            this.labelsGroupHeaderSection.Name = "labelsGroupHeaderSection";
            this.labelsGroupHeaderSection.PrintOnEveryPage = true;
            this.labelsGroupHeaderSection.Style.Visible = true;
            // 
            // labelsGroupFooterSection
            // 
            this.labelsGroupFooterSection.Height = Telerik.Reporting.Drawing.Unit.Cm(1.0001000165939331D);
            this.labelsGroupFooterSection.Name = "labelsGroupFooterSection";
            this.labelsGroupFooterSection.Style.Visible = false;
            // 
            // diffPercentafeNormelizedCaptionTextBox
            // 
            this.diffPercentafeNormelizedCaptionTextBox.CanGrow = true;
            this.diffPercentafeNormelizedCaptionTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(18.5D), Telerik.Reporting.Drawing.Unit.Cm(0.00010012308484874666D));
            this.diffPercentafeNormelizedCaptionTextBox.Name = "diffPercentafeNormelizedCaptionTextBox";
            this.diffPercentafeNormelizedCaptionTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(3.9993343353271484D), Telerik.Reporting.Drawing.Unit.Cm(0.55301570892333984D));
            this.diffPercentafeNormelizedCaptionTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            this.diffPercentafeNormelizedCaptionTextBox.StyleName = "Caption";
            this.diffPercentafeNormelizedCaptionTextBox.Value = "% Διαφορά 15 οC";
            // 
            // diffPercentageCaptionTextBox
            // 
            this.diffPercentageCaptionTextBox.CanGrow = true;
            this.diffPercentageCaptionTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(22.606889724731445D), Telerik.Reporting.Drawing.Unit.Cm(0.00010012308484874666D));
            this.diffPercentageCaptionTextBox.Name = "diffPercentageCaptionTextBox";
            this.diffPercentageCaptionTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(3.9993343353271484D), Telerik.Reporting.Drawing.Unit.Cm(0.55301570892333984D));
            this.diffPercentageCaptionTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            this.diffPercentageCaptionTextBox.StyleName = "Caption";
            this.diffPercentageCaptionTextBox.Value = "% Διαφορά";
            // 
            // diffVolumeCaptionTextBox
            // 
            this.diffVolumeCaptionTextBox.CanGrow = true;
            this.diffVolumeCaptionTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(10.286222457885742D), Telerik.Reporting.Drawing.Unit.Cm(0.00010052680590888485D));
            this.diffVolumeCaptionTextBox.Name = "diffVolumeCaptionTextBox";
            this.diffVolumeCaptionTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(3.9993343353271484D), Telerik.Reporting.Drawing.Unit.Cm(0.55301570892333984D));
            this.diffVolumeCaptionTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            this.diffVolumeCaptionTextBox.StyleName = "Caption";
            this.diffVolumeCaptionTextBox.Value = "Διαφορά Όγκου";
            // 
            // diffVolumeNormalizedCaptionTextBox
            // 
            this.diffVolumeNormalizedCaptionTextBox.CanGrow = true;
            this.diffVolumeNormalizedCaptionTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(14.386222839355469D), Telerik.Reporting.Drawing.Unit.Cm(0.00010052680590888485D));
            this.diffVolumeNormalizedCaptionTextBox.Name = "diffVolumeNormalizedCaptionTextBox";
            this.diffVolumeNormalizedCaptionTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(3.9993343353271484D), Telerik.Reporting.Drawing.Unit.Cm(0.55301570892333984D));
            this.diffVolumeNormalizedCaptionTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            this.diffVolumeNormalizedCaptionTextBox.StyleName = "Caption";
            this.diffVolumeNormalizedCaptionTextBox.Value = "Διαφορά Όγκου 15οC";
            // 
            // fuelTypeIdGroupHeaderSection
            // 
            this.fuelTypeIdGroupHeaderSection.Height = Telerik.Reporting.Drawing.Unit.Cm(0.75875008106231689D);
            this.fuelTypeIdGroupHeaderSection.Name = "fuelTypeIdGroupHeaderSection";
            this.fuelTypeIdGroupHeaderSection.Style.Visible = false;
            // 
            // fuelTypeIdGroupFooterSection
            // 
            this.fuelTypeIdGroupFooterSection.Height = Telerik.Reporting.Drawing.Unit.Cm(0.71437495946884155D);
            this.fuelTypeIdGroupFooterSection.Name = "fuelTypeIdGroupFooterSection";
            this.fuelTypeIdGroupFooterSection.Style.Visible = false;
            // 
            // fuelTypeIdDataTextBox
            // 
            this.fuelTypeIdDataTextBox.CanGrow = true;
            this.fuelTypeIdDataTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0.1006891056895256D), Telerik.Reporting.Drawing.Unit.Cm(0.041249904781579971D));
            this.fuelTypeIdDataTextBox.Name = "fuelTypeIdDataTextBox";
            this.fuelTypeIdDataTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(7.0993103981018066D), Telerik.Reporting.Drawing.Unit.Cm(0.4942685067653656D));
            this.fuelTypeIdDataTextBox.Style.BorderStyle.Default = Telerik.Reporting.Drawing.BorderType.Solid;
            this.fuelTypeIdDataTextBox.StyleName = "Data";
            this.fuelTypeIdDataTextBox.Value = "= Fields.FuelTypeName + \" (\" + Fields.FuelTypeCode + \")\"";
            // 
            // pageHeader
            // 
            this.pageHeader.Height = Telerik.Reporting.Drawing.Unit.Cm(1.1058331727981567D);
            this.pageHeader.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.reportNameTextBox});
            this.pageHeader.Name = "pageHeader";
            // 
            // reportNameTextBox
            // 
            this.reportNameTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.reportNameTextBox.Name = "reportNameTextBox";
            this.reportNameTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(15.708333015441895D), Telerik.Reporting.Drawing.Unit.Cm(0.99999988079071045D));
            this.reportNameTextBox.StyleName = "PageInfo";
            this.reportNameTextBox.Value = "DivergenceBalanceReport";
            // 
            // pageFooter
            // 
            this.pageFooter.Height = Telerik.Reporting.Drawing.Unit.Cm(0.55104231834411621D);
            this.pageFooter.Name = "pageFooter";
            this.pageFooter.Style.Visible = false;
            // 
            // reportHeader
            // 
            this.reportHeader.Height = Telerik.Reporting.Drawing.Unit.Cm(1.09416663646698D);
            this.reportHeader.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.titleTextBox});
            this.reportHeader.Name = "reportHeader";
            // 
            // titleTextBox
            // 
            this.titleTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0D), Telerik.Reporting.Drawing.Unit.Cm(0D));
            this.titleTextBox.Name = "titleTextBox";
            this.titleTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(24.399900436401367D), Telerik.Reporting.Drawing.Unit.Cm(0.89416676759719849D));
            this.titleTextBox.StyleName = "Title";
            this.titleTextBox.Value = "Ισοζύγια ανά τύπο Καυσίμου";
            // 
            // reportFooter
            // 
            this.reportFooter.Height = Telerik.Reporting.Drawing.Unit.Cm(0.71437495946884155D);
            this.reportFooter.Name = "reportFooter";
            this.reportFooter.Style.Visible = false;
            // 
            // detail
            // 
            this.detail.Height = Telerik.Reporting.Drawing.Unit.Cm(0.64124959707260132D);
            this.detail.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.diffPercentafeNormelizedDataTextBox,
            this.diffVolumeDataTextBox,
            this.diffVolumeNormalizedDataTextBox,
            this.fuelTypeIdDataTextBox,
            this.diffPercentageDataTextBox});
            this.detail.Name = "detail";
            // 
            // diffPercentafeNormelizedDataTextBox
            // 
            this.diffPercentafeNormelizedDataTextBox.CanGrow = true;
            this.diffPercentafeNormelizedDataTextBox.Format = "{0:N2} %";
            this.diffPercentafeNormelizedDataTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(18.5D), Telerik.Reporting.Drawing.Unit.Cm(0.041249904781579971D));
            this.diffPercentafeNormelizedDataTextBox.Name = "diffPercentafeNormelizedDataTextBox";
            this.diffPercentafeNormelizedDataTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(3.9993343353271484D), Telerik.Reporting.Drawing.Unit.Cm(0.49426770210266113D));
            this.diffPercentafeNormelizedDataTextBox.Style.BorderStyle.Default = Telerik.Reporting.Drawing.BorderType.Solid;
            this.diffPercentafeNormelizedDataTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.diffPercentafeNormelizedDataTextBox.StyleName = "Data";
            this.diffPercentafeNormelizedDataTextBox.Value = "=Fields.DiffPercentafeNormelized";
            // 
            // diffPercentageDataTextBox
            // 
            this.diffPercentageDataTextBox.CanGrow = true;
            this.diffPercentageDataTextBox.Format = "{0:N2} %";
            this.diffPercentageDataTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(22.606889724731445D), Telerik.Reporting.Drawing.Unit.Cm(0.041250709444284439D));
            this.diffPercentageDataTextBox.Name = "diffPercentageDataTextBox";
            this.diffPercentageDataTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(3.9993343353271484D), Telerik.Reporting.Drawing.Unit.Cm(0.49426770210266113D));
            this.diffPercentageDataTextBox.Style.BorderStyle.Default = Telerik.Reporting.Drawing.BorderType.Solid;
            this.diffPercentageDataTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.diffPercentageDataTextBox.StyleName = "Data";
            this.diffPercentageDataTextBox.Value = "=Fields.DiffPercentage";
            // 
            // diffVolumeDataTextBox
            // 
            this.diffVolumeDataTextBox.CanGrow = true;
            this.diffVolumeDataTextBox.Format = "{0:N2}";
            this.diffVolumeDataTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(10.286222457885742D), Telerik.Reporting.Drawing.Unit.Cm(0.041249904781579971D));
            this.diffVolumeDataTextBox.Name = "diffVolumeDataTextBox";
            this.diffVolumeDataTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(3.9993343353271484D), Telerik.Reporting.Drawing.Unit.Cm(0.49426770210266113D));
            this.diffVolumeDataTextBox.Style.BorderStyle.Default = Telerik.Reporting.Drawing.BorderType.Solid;
            this.diffVolumeDataTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.diffVolumeDataTextBox.StyleName = "Data";
            this.diffVolumeDataTextBox.Value = "=Fields.DiffVolume";
            // 
            // diffVolumeNormalizedDataTextBox
            // 
            this.diffVolumeNormalizedDataTextBox.CanGrow = true;
            this.diffVolumeNormalizedDataTextBox.Format = "{0:N2}";
            this.diffVolumeNormalizedDataTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(14.386222839355469D), Telerik.Reporting.Drawing.Unit.Cm(0.041249904781579971D));
            this.diffVolumeNormalizedDataTextBox.Name = "diffVolumeNormalizedDataTextBox";
            this.diffVolumeNormalizedDataTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(3.9993343353271484D), Telerik.Reporting.Drawing.Unit.Cm(0.49426770210266113D));
            this.diffVolumeNormalizedDataTextBox.Style.BorderStyle.Default = Telerik.Reporting.Drawing.BorderType.Solid;
            this.diffVolumeNormalizedDataTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.diffVolumeNormalizedDataTextBox.StyleName = "Data";
            this.diffVolumeNormalizedDataTextBox.Value = "=Fields.DiffVolumeNormalized";
            // 
            // textBox1
            // 
            this.textBox1.CanGrow = true;
            this.textBox1.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0.1006891056895256D), Telerik.Reporting.Drawing.Unit.Cm(0.00010012308484874666D));
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(7.0993103981018066D), Telerik.Reporting.Drawing.Unit.Cm(0.55301570892333984D));
            this.textBox1.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            this.textBox1.StyleName = "Caption";
            this.textBox1.Value = "Τύπος Καυσίμου";
            // 
            // DivergenceBalanceReport
            // 
            this.DataSource = this.divergenceData;
            group1.GroupFooter = this.labelsGroupFooterSection;
            group1.GroupHeader = this.labelsGroupHeaderSection;
            group1.Name = "labelsGroup";
            group2.GroupFooter = this.fuelTypeIdGroupFooterSection;
            group2.GroupHeader = this.fuelTypeIdGroupHeaderSection;
            group2.Groupings.Add(new Telerik.Reporting.Grouping("=Fields.FuelTypeId"));
            group2.Name = "fuelTypeIdGroup";
            this.Groups.AddRange(new Telerik.Reporting.Group[] {
            group1,
            group2});
            this.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.labelsGroupHeaderSection,
            this.labelsGroupFooterSection,
            this.fuelTypeIdGroupHeaderSection,
            this.fuelTypeIdGroupFooterSection,
            this.pageHeader,
            this.pageFooter,
            this.reportHeader,
            this.reportFooter,
            this.detail});
            this.Name = "DivergenceBalanceReport";
            this.PageSettings.Landscape = true;
            this.PageSettings.Margins = new Telerik.Reporting.Drawing.MarginsU(Telerik.Reporting.Drawing.Unit.Mm(15D), Telerik.Reporting.Drawing.Unit.Mm(15D), Telerik.Reporting.Drawing.Unit.Mm(15D), Telerik.Reporting.Drawing.Unit.Mm(15D));
            this.PageSettings.PaperKind = System.Drawing.Printing.PaperKind.A4;
            this.Style.BackgroundColor = System.Drawing.Color.White;
            styleRule1.Selectors.AddRange(new Telerik.Reporting.Drawing.ISelector[] {
            new Telerik.Reporting.Drawing.StyleSelector("Title")});
            styleRule1.Style.Color = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(58)))), ((int)(((byte)(112)))));
            styleRule1.Style.Font.Name = "Tahoma";
            styleRule1.Style.Font.Size = Telerik.Reporting.Drawing.Unit.Point(18D);
            styleRule2.Selectors.AddRange(new Telerik.Reporting.Drawing.ISelector[] {
            new Telerik.Reporting.Drawing.StyleSelector("Caption")});
            styleRule2.Style.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(58)))), ((int)(((byte)(112)))));
            styleRule2.Style.Color = System.Drawing.Color.White;
            styleRule2.Style.Font.Name = "Tahoma";
            styleRule2.Style.Font.Size = Telerik.Reporting.Drawing.Unit.Point(10D);
            styleRule2.Style.VerticalAlign = Telerik.Reporting.Drawing.VerticalAlign.Middle;
            styleRule3.Selectors.AddRange(new Telerik.Reporting.Drawing.ISelector[] {
            new Telerik.Reporting.Drawing.StyleSelector("Data")});
            styleRule3.Style.Color = System.Drawing.Color.Black;
            styleRule3.Style.Font.Name = "Tahoma";
            styleRule3.Style.Font.Size = Telerik.Reporting.Drawing.Unit.Point(9D);
            styleRule3.Style.VerticalAlign = Telerik.Reporting.Drawing.VerticalAlign.Middle;
            styleRule4.Selectors.AddRange(new Telerik.Reporting.Drawing.ISelector[] {
            new Telerik.Reporting.Drawing.StyleSelector("PageInfo")});
            styleRule4.Style.Color = System.Drawing.Color.Black;
            styleRule4.Style.Font.Name = "Tahoma";
            styleRule4.Style.Font.Size = Telerik.Reporting.Drawing.Unit.Point(8D);
            styleRule4.Style.VerticalAlign = Telerik.Reporting.Drawing.VerticalAlign.Middle;
            this.StyleSheet.AddRange(new Telerik.Reporting.Drawing.StyleRule[] {
            styleRule1,
            styleRule2,
            styleRule3,
            styleRule4});
            this.Width = Telerik.Reporting.Drawing.Unit.Cm(26.69999885559082D);
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();

        }
        #endregion

        private Telerik.Reporting.ObjectDataSource divergenceData;
        private Telerik.Reporting.GroupHeaderSection labelsGroupHeaderSection;
        private Telerik.Reporting.TextBox diffPercentafeNormelizedCaptionTextBox;
        private Telerik.Reporting.TextBox diffPercentageCaptionTextBox;
        private Telerik.Reporting.TextBox diffVolumeCaptionTextBox;
        private Telerik.Reporting.TextBox diffVolumeNormalizedCaptionTextBox;
        private Telerik.Reporting.GroupFooterSection labelsGroupFooterSection;
        private Telerik.Reporting.GroupHeaderSection fuelTypeIdGroupHeaderSection;
        private Telerik.Reporting.TextBox fuelTypeIdDataTextBox;
        private Telerik.Reporting.GroupFooterSection fuelTypeIdGroupFooterSection;
        private Telerik.Reporting.PageHeaderSection pageHeader;
        private Telerik.Reporting.TextBox reportNameTextBox;
        private Telerik.Reporting.PageFooterSection pageFooter;
        private Telerik.Reporting.ReportHeaderSection reportHeader;
        private Telerik.Reporting.TextBox titleTextBox;
        private Telerik.Reporting.ReportFooterSection reportFooter;
        private Telerik.Reporting.DetailSection detail;
        private Telerik.Reporting.TextBox diffPercentafeNormelizedDataTextBox;
        private Telerik.Reporting.TextBox diffPercentageDataTextBox;
        private Telerik.Reporting.TextBox diffVolumeDataTextBox;
        private Telerik.Reporting.TextBox diffVolumeNormalizedDataTextBox;
        private Telerik.Reporting.TextBox textBox1;

    }
}