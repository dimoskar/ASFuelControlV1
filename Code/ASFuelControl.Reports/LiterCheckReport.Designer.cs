namespace ASFuelControl.Reports
{
    partial class LiterCheckReport
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
            Telerik.Reporting.Group group3 = new Telerik.Reporting.Group();
            Telerik.Reporting.ReportParameter reportParameter1 = new Telerik.Reporting.ReportParameter();
            Telerik.Reporting.ReportParameter reportParameter2 = new Telerik.Reporting.ReportParameter();
            Telerik.Reporting.ReportParameter reportParameter3 = new Telerik.Reporting.ReportParameter();
            Telerik.Reporting.Drawing.StyleRule styleRule1 = new Telerik.Reporting.Drawing.StyleRule();
            Telerik.Reporting.Drawing.StyleRule styleRule2 = new Telerik.Reporting.Drawing.StyleRule();
            Telerik.Reporting.Drawing.StyleRule styleRule3 = new Telerik.Reporting.Drawing.StyleRule();
            Telerik.Reporting.Drawing.StyleRule styleRule4 = new Telerik.Reporting.Drawing.StyleRule();
            this.invoiceLineDS = new Telerik.Reporting.ObjectDataSource();
            this.labelsGroupHeaderSection = new Telerik.Reporting.GroupHeaderSection();
            this.labelsGroupFooterSection = new Telerik.Reporting.GroupFooterSection();
            this.textBox1 = new Telerik.Reporting.TextBox();
            this.textBox2 = new Telerik.Reporting.TextBox();
            this.totalPriceCaptionTextBox = new Telerik.Reporting.TextBox();
            this.volumeCaptionTextBox = new Telerik.Reporting.TextBox();
            this.volumeNormalizedCaptionTextBox = new Telerik.Reporting.TextBox();
            this.textBox4 = new Telerik.Reporting.TextBox();
            this.salesTransactionNozzleDispenserOfficialPumpNumberGroupHeaderSection = new Telerik.Reporting.GroupHeaderSection();
            this.salesTransactionNozzleDispenserOfficialPumpNumberGroupFooterSection = new Telerik.Reporting.GroupFooterSection();
            this.salesTransactionNozzleOfficialNozzleNumberGroupHeaderSection = new Telerik.Reporting.GroupHeaderSection();
            this.salesTransactionNozzleOfficialNozzleNumberGroupFooterSection = new Telerik.Reporting.GroupFooterSection();
            this.textBox5 = new Telerik.Reporting.TextBox();
            this.textBox6 = new Telerik.Reporting.TextBox();
            this.totalPriceDataTextBox = new Telerik.Reporting.TextBox();
            this.volumeDataTextBox = new Telerik.Reporting.TextBox();
            this.volumeNormalizedDataTextBox = new Telerik.Reporting.TextBox();
            this.textBox8 = new Telerik.Reporting.TextBox();
            this.pageFooter = new Telerik.Reporting.PageFooterSection();
            this.currentTimeTextBox = new Telerik.Reporting.TextBox();
            this.pageInfoTextBox = new Telerik.Reporting.TextBox();
            this.reportHeader = new Telerik.Reporting.ReportHeaderSection();
            this.titleTextBox = new Telerik.Reporting.TextBox();
            this.reportFooter = new Telerik.Reporting.ReportFooterSection();
            this.detail = new Telerik.Reporting.DetailSection();
            this.textBox10 = new Telerik.Reporting.TextBox();
            this.textBox11 = new Telerik.Reporting.TextBox();
            this.textBox12 = new Telerik.Reporting.TextBox();
            this.textBox16 = new Telerik.Reporting.TextBox();
            this.textBox17 = new Telerik.Reporting.TextBox();
            this.textBox18 = new Telerik.Reporting.TextBox();
            this.shape2 = new Telerik.Reporting.Shape();
            this.shape1 = new Telerik.Reporting.Shape();
            this.textBox9 = new Telerik.Reporting.TextBox();
            this.textBox13 = new Telerik.Reporting.TextBox();
            this.textBox14 = new Telerik.Reporting.TextBox();
            this.textBox15 = new Telerik.Reporting.TextBox();
            this.textBox3 = new Telerik.Reporting.TextBox();
            this.textBox7 = new Telerik.Reporting.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            // 
            // invoiceLineDS
            // 
            this.invoiceLineDS.DataSource = "ASFuelControl.Data.InvoiceLine, ASFuelControl.Data, Version=1.0.0.0, Culture=neut" +
    "ral, PublicKeyToken=null";
            this.invoiceLineDS.Name = "invoiceLineDS";
            // 
            // labelsGroupHeaderSection
            // 
            this.labelsGroupHeaderSection.Height = Telerik.Reporting.Drawing.Unit.Cm(1.1058331727981567D);
            this.labelsGroupHeaderSection.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.textBox1,
            this.textBox2,
            this.totalPriceCaptionTextBox,
            this.volumeCaptionTextBox,
            this.volumeNormalizedCaptionTextBox,
            this.textBox4,
            this.textBox14});
            this.labelsGroupHeaderSection.Name = "labelsGroupHeaderSection";
            this.labelsGroupHeaderSection.PrintOnEveryPage = true;
            // 
            // labelsGroupFooterSection
            // 
            this.labelsGroupFooterSection.Height = Telerik.Reporting.Drawing.Unit.Cm(0.71437495946884155D);
            this.labelsGroupFooterSection.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.textBox12,
            this.textBox11,
            this.textBox10,
            this.textBox13,
            this.textBox7});
            this.labelsGroupFooterSection.Name = "labelsGroupFooterSection";
            this.labelsGroupFooterSection.Style.Visible = true;
            // 
            // textBox1
            // 
            this.textBox1.CanGrow = true;
            this.textBox1.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.1986904144287109D), Telerik.Reporting.Drawing.Unit.Cm(0.99999988079071045D));
            this.textBox1.Style.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(121)))), ((int)(((byte)(167)))), ((int)(((byte)(227)))));
            this.textBox1.Style.Font.Name = "Verdana";
            this.textBox1.Style.Font.Size = Telerik.Reporting.Drawing.Unit.Point(9D);
            this.textBox1.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            this.textBox1.StyleName = "Caption";
            this.textBox1.Value = "Αντλία";
            // 
            // textBox2
            // 
            this.textBox2.CanGrow = true;
            this.textBox2.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(2.2939951419830322D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(1.5954763889312744D), Telerik.Reporting.Drawing.Unit.Cm(0.99999988079071045D));
            this.textBox2.Style.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(121)))), ((int)(((byte)(167)))), ((int)(((byte)(227)))));
            this.textBox2.Style.Font.Name = "Verdana";
            this.textBox2.Style.Font.Size = Telerik.Reporting.Drawing.Unit.Point(9D);
            this.textBox2.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            this.textBox2.StyleName = "Caption";
            this.textBox2.Value = "Ακρο-σωλήνιο";
            // 
            // totalPriceCaptionTextBox
            // 
            this.totalPriceCaptionTextBox.CanGrow = true;
            this.totalPriceCaptionTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(5.4480938911437988D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.totalPriceCaptionTextBox.Name = "totalPriceCaptionTextBox";
            this.totalPriceCaptionTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(3.1259524822235107D), Telerik.Reporting.Drawing.Unit.Cm(0.99999988079071045D));
            this.totalPriceCaptionTextBox.Style.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(121)))), ((int)(((byte)(167)))), ((int)(((byte)(227)))));
            this.totalPriceCaptionTextBox.Style.Font.Name = "Verdana";
            this.totalPriceCaptionTextBox.Style.Font.Size = Telerik.Reporting.Drawing.Unit.Point(9D);
            this.totalPriceCaptionTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            this.totalPriceCaptionTextBox.StyleName = "Caption";
            this.totalPriceCaptionTextBox.Value = "Σύνολο";
            // 
            // volumeCaptionTextBox
            // 
            this.volumeCaptionTextBox.CanGrow = true;
            this.volumeCaptionTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(8.59999942779541D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.volumeCaptionTextBox.Name = "volumeCaptionTextBox";
            this.volumeCaptionTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(3.2835655212402344D), Telerik.Reporting.Drawing.Unit.Cm(0.99999988079071045D));
            this.volumeCaptionTextBox.Style.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(121)))), ((int)(((byte)(167)))), ((int)(((byte)(227)))));
            this.volumeCaptionTextBox.Style.Font.Name = "Verdana";
            this.volumeCaptionTextBox.Style.Font.Size = Telerik.Reporting.Drawing.Unit.Point(9D);
            this.volumeCaptionTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            this.volumeCaptionTextBox.StyleName = "Caption";
            this.volumeCaptionTextBox.Value = "Όγκος";
            // 
            // volumeNormalizedCaptionTextBox
            // 
            this.volumeNormalizedCaptionTextBox.CanGrow = true;
            this.volumeNormalizedCaptionTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(11.92508602142334D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.volumeNormalizedCaptionTextBox.Name = "volumeNormalizedCaptionTextBox";
            this.volumeNormalizedCaptionTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(3.1411776542663574D), Telerik.Reporting.Drawing.Unit.Cm(0.99999988079071045D));
            this.volumeNormalizedCaptionTextBox.Style.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(121)))), ((int)(((byte)(167)))), ((int)(((byte)(227)))));
            this.volumeNormalizedCaptionTextBox.Style.Font.Name = "Verdana";
            this.volumeNormalizedCaptionTextBox.Style.Font.Size = Telerik.Reporting.Drawing.Unit.Point(9D);
            this.volumeNormalizedCaptionTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            this.volumeNormalizedCaptionTextBox.StyleName = "Caption";
            this.volumeNormalizedCaptionTextBox.Value = "Όγκος 15oC";
            // 
            // textBox4
            // 
            this.textBox4.CanGrow = true;
            this.textBox4.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(15.100000381469727D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.textBox4.Name = "textBox4";
            this.textBox4.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.90000057220459D), Telerik.Reporting.Drawing.Unit.Cm(0.99999988079071045D));
            this.textBox4.Style.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(121)))), ((int)(((byte)(167)))), ((int)(((byte)(227)))));
            this.textBox4.Style.Font.Name = "Verdana";
            this.textBox4.Style.Font.Size = Telerik.Reporting.Drawing.Unit.Point(9D);
            this.textBox4.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            this.textBox4.StyleName = "Caption";
            this.textBox4.Value = "Μετρητής";
            // 
            // salesTransactionNozzleDispenserOfficialPumpNumberGroupHeaderSection
            // 
            this.salesTransactionNozzleDispenserOfficialPumpNumberGroupHeaderSection.Height = Telerik.Reporting.Drawing.Unit.Cm(1.1058331727981567D);
            this.salesTransactionNozzleDispenserOfficialPumpNumberGroupHeaderSection.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.textBox5});
            this.salesTransactionNozzleDispenserOfficialPumpNumberGroupHeaderSection.Name = "salesTransactionNozzleDispenserOfficialPumpNumberGroupHeaderSection";
            // 
            // salesTransactionNozzleDispenserOfficialPumpNumberGroupFooterSection
            // 
            this.salesTransactionNozzleDispenserOfficialPumpNumberGroupFooterSection.Height = Telerik.Reporting.Drawing.Unit.Cm(0.97125041484832764D);
            this.salesTransactionNozzleDispenserOfficialPumpNumberGroupFooterSection.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.textBox16,
            this.textBox17,
            this.textBox18,
            this.shape1,
            this.textBox9,
            this.textBox3});
            this.salesTransactionNozzleDispenserOfficialPumpNumberGroupFooterSection.Name = "salesTransactionNozzleDispenserOfficialPumpNumberGroupFooterSection";
            // 
            // salesTransactionNozzleOfficialNozzleNumberGroupHeaderSection
            // 
            this.salesTransactionNozzleOfficialNozzleNumberGroupHeaderSection.Height = Telerik.Reporting.Drawing.Unit.Cm(0.88833361864089966D);
            this.salesTransactionNozzleOfficialNozzleNumberGroupHeaderSection.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.textBox6,
            this.totalPriceDataTextBox,
            this.volumeDataTextBox,
            this.volumeNormalizedDataTextBox,
            this.textBox8,
            this.shape2,
            this.textBox15});
            this.salesTransactionNozzleOfficialNozzleNumberGroupHeaderSection.Name = "salesTransactionNozzleOfficialNozzleNumberGroupHeaderSection";
            // 
            // salesTransactionNozzleOfficialNozzleNumberGroupFooterSection
            // 
            this.salesTransactionNozzleOfficialNozzleNumberGroupFooterSection.Height = Telerik.Reporting.Drawing.Unit.Cm(0.71437495946884155D);
            this.salesTransactionNozzleOfficialNozzleNumberGroupFooterSection.Name = "salesTransactionNozzleOfficialNozzleNumberGroupFooterSection";
            this.salesTransactionNozzleOfficialNozzleNumberGroupFooterSection.Style.Visible = false;
            // 
            // textBox5
            // 
            this.textBox5.CanGrow = true;
            this.textBox5.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.textBox5.Name = "textBox5";
            this.textBox5.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.1986904144287109D), Telerik.Reporting.Drawing.Unit.Cm(0.99999988079071045D));
            this.textBox5.Style.Font.Name = "Verdana";
            this.textBox5.StyleName = "Data";
            this.textBox5.Value = "=Fields.SalesTransaction.Nozzle.Dispenser.OfficialPumpNumber";
            // 
            // textBox6
            // 
            this.textBox6.CanGrow = true;
            this.textBox6.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(2.2939951419830322D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.textBox6.Name = "textBox6";
            this.textBox6.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(1.5954763889312744D), Telerik.Reporting.Drawing.Unit.Cm(0.58843386173248291D));
            this.textBox6.Style.Font.Name = "Verdana";
            this.textBox6.StyleName = "Data";
            this.textBox6.Value = "= Fields.SalesTransaction.Nozzle.OfficialNozzleNumber + \"(\" + Fields.FuelType.Cod" +
    "e + \")\"";
            // 
            // totalPriceDataTextBox
            // 
            this.totalPriceDataTextBox.CanGrow = true;
            this.totalPriceDataTextBox.Format = "{0:N2}€";
            this.totalPriceDataTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(5.4480938911437988D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.totalPriceDataTextBox.Name = "totalPriceDataTextBox";
            this.totalPriceDataTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(3.1259524822235107D), Telerik.Reporting.Drawing.Unit.Cm(0.58843386173248291D));
            this.totalPriceDataTextBox.Style.Font.Name = "Verdana";
            this.totalPriceDataTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.totalPriceDataTextBox.StyleName = "Data";
            this.totalPriceDataTextBox.Value = "=Sum(Fields.TotalPrice)";
            // 
            // volumeDataTextBox
            // 
            this.volumeDataTextBox.CanGrow = true;
            this.volumeDataTextBox.Format = "{0:N2}Lt";
            this.volumeDataTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(8.59999942779541D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.volumeDataTextBox.Name = "volumeDataTextBox";
            this.volumeDataTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(3.2835655212402344D), Telerik.Reporting.Drawing.Unit.Cm(0.58843386173248291D));
            this.volumeDataTextBox.Style.Font.Name = "Verdana";
            this.volumeDataTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.volumeDataTextBox.StyleName = "Data";
            this.volumeDataTextBox.Value = "=Sum(Fields.Volume)";
            // 
            // volumeNormalizedDataTextBox
            // 
            this.volumeNormalizedDataTextBox.CanGrow = true;
            this.volumeNormalizedDataTextBox.Format = "{0:N2}Lt";
            this.volumeNormalizedDataTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(11.92508602142334D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.volumeNormalizedDataTextBox.Name = "volumeNormalizedDataTextBox";
            this.volumeNormalizedDataTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(3.1411776542663574D), Telerik.Reporting.Drawing.Unit.Cm(0.58843386173248291D));
            this.volumeNormalizedDataTextBox.Style.Font.Name = "Verdana";
            this.volumeNormalizedDataTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.volumeNormalizedDataTextBox.StyleName = "Data";
            this.volumeNormalizedDataTextBox.Value = "=Sum(Fields.VolumeNormalized)";
            // 
            // textBox8
            // 
            this.textBox8.CanGrow = true;
            this.textBox8.Format = "{0:N2}";
            this.textBox8.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(15.100000381469727D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.textBox8.Name = "textBox8";
            this.textBox8.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.90000057220459D), Telerik.Reporting.Drawing.Unit.Cm(0.58843386173248291D));
            this.textBox8.Style.Font.Name = "Verdana";
            this.textBox8.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.textBox8.StyleName = "Data";
            this.textBox8.Value = "=Max(Fields.SalesTransaction.TotalizerEnd) / 100";
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
            this.pageInfoTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(11.772357940673828D), Telerik.Reporting.Drawing.Unit.Cm(0.00010012308484874666D));
            this.pageInfoTextBox.Name = "pageInfoTextBox";
            this.pageInfoTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(6.2275409698486328D), Telerik.Reporting.Drawing.Unit.Cm(0.99999988079071045D));
            this.pageInfoTextBox.Style.Font.Name = "Verdana";
            this.pageInfoTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.pageInfoTextBox.StyleName = "PageInfo";
            this.pageInfoTextBox.Value = "=PageNumber";
            // 
            // reportHeader
            // 
            this.reportHeader.Height = Telerik.Reporting.Drawing.Unit.Cm(1D);
            this.reportHeader.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.titleTextBox});
            this.reportHeader.Name = "reportHeader";
            // 
            // titleTextBox
            // 
            this.titleTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0.099999949336051941D), Telerik.Reporting.Drawing.Unit.Cm(0.099999949336051941D));
            this.titleTextBox.Name = "titleTextBox";
            this.titleTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(15.814167022705078D), Telerik.Reporting.Drawing.Unit.Cm(0.800000011920929D));
            this.titleTextBox.Style.Font.Name = "Verdana";
            this.titleTextBox.Style.Font.Size = Telerik.Reporting.Drawing.Unit.Point(14D);
            this.titleTextBox.StyleName = "Title";
            this.titleTextBox.Value = "Λιτρομετρήσεις";
            // 
            // reportFooter
            // 
            this.reportFooter.Height = Telerik.Reporting.Drawing.Unit.Cm(0.71437495946884155D);
            this.reportFooter.Name = "reportFooter";
            // 
            // detail
            // 
            this.detail.Height = Telerik.Reporting.Drawing.Unit.Cm(0.71437495946884155D);
            this.detail.Name = "detail";
            this.detail.Style.Visible = false;
            // 
            // textBox10
            // 
            this.textBox10.CanGrow = true;
            this.textBox10.Format = "{0:N2}Lt";
            this.textBox10.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(11.92508602142334D), Telerik.Reporting.Drawing.Unit.Cm(0.12584099173545837D));
            this.textBox10.Name = "textBox10";
            this.textBox10.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(3.1411776542663574D), Telerik.Reporting.Drawing.Unit.Cm(0.58843386173248291D));
            this.textBox10.Style.Font.Bold = true;
            this.textBox10.Style.Font.Name = "Verdana";
            this.textBox10.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.textBox10.StyleName = "Data";
            this.textBox10.Value = "=Sum(Fields.VolumeNormalized)";
            // 
            // textBox11
            // 
            this.textBox11.CanGrow = true;
            this.textBox11.Format = "{0:N2}Lt";
            this.textBox11.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(8.59999942779541D), Telerik.Reporting.Drawing.Unit.Cm(0.12584099173545837D));
            this.textBox11.Name = "textBox11";
            this.textBox11.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(3.2835655212402344D), Telerik.Reporting.Drawing.Unit.Cm(0.58843386173248291D));
            this.textBox11.Style.Font.Bold = true;
            this.textBox11.Style.Font.Name = "Verdana";
            this.textBox11.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.textBox11.StyleName = "Data";
            this.textBox11.Value = "=Sum(Fields.Volume)";
            // 
            // textBox12
            // 
            this.textBox12.CanGrow = true;
            this.textBox12.Format = "{0:N2}€";
            this.textBox12.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(5.4480938911437988D), Telerik.Reporting.Drawing.Unit.Cm(0.12584099173545837D));
            this.textBox12.Name = "textBox12";
            this.textBox12.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(3.1259524822235107D), Telerik.Reporting.Drawing.Unit.Cm(0.58843386173248291D));
            this.textBox12.Style.Font.Bold = true;
            this.textBox12.Style.Font.Name = "Verdana";
            this.textBox12.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.textBox12.StyleName = "Data";
            this.textBox12.Value = "=Sum(Fields.TotalPrice)";
            // 
            // textBox16
            // 
            this.textBox16.CanGrow = true;
            this.textBox16.Format = "{0:N2}Lt";
            this.textBox16.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(11.92508602142334D), Telerik.Reporting.Drawing.Unit.Cm(0.12584099173545837D));
            this.textBox16.Name = "textBox16";
            this.textBox16.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(3.1411776542663574D), Telerik.Reporting.Drawing.Unit.Cm(0.58843386173248291D));
            this.textBox16.Style.Font.Bold = true;
            this.textBox16.Style.Font.Name = "Verdana";
            this.textBox16.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.textBox16.StyleName = "Data";
            this.textBox16.Value = "=Sum(Fields.VolumeNormalized)";
            // 
            // textBox17
            // 
            this.textBox17.CanGrow = true;
            this.textBox17.Format = "{0:N2}Lt";
            this.textBox17.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(8.59999942779541D), Telerik.Reporting.Drawing.Unit.Cm(0.12584099173545837D));
            this.textBox17.Name = "textBox17";
            this.textBox17.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(3.2835655212402344D), Telerik.Reporting.Drawing.Unit.Cm(0.58843386173248291D));
            this.textBox17.Style.Font.Bold = true;
            this.textBox17.Style.Font.Name = "Verdana";
            this.textBox17.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.textBox17.StyleName = "Data";
            this.textBox17.Value = "=Sum(Fields.Volume)";
            // 
            // textBox18
            // 
            this.textBox18.CanGrow = true;
            this.textBox18.Format = "{0:N2}€";
            this.textBox18.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(5.4480938911437988D), Telerik.Reporting.Drawing.Unit.Cm(0.12584099173545837D));
            this.textBox18.Name = "textBox18";
            this.textBox18.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(3.1259524822235107D), Telerik.Reporting.Drawing.Unit.Cm(0.58843386173248291D));
            this.textBox18.Style.Font.Bold = true;
            this.textBox18.Style.Font.Name = "Verdana";
            this.textBox18.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.textBox18.StyleName = "Data";
            this.textBox18.Value = "=Sum(Fields.TotalPrice)";
            // 
            // shape2
            // 
            this.shape2.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0D), Telerik.Reporting.Drawing.Unit.Cm(0.68833369016647339D));
            this.shape2.Name = "shape2";
            this.shape2.ShapeType = new Telerik.Reporting.Drawing.Shapes.LineShape(Telerik.Reporting.Drawing.Shapes.LineDirection.EW);
            this.shape2.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(17.999898910522461D), Telerik.Reporting.Drawing.Unit.Cm(0.19999989867210388D));
            this.shape2.Style.Color = System.Drawing.Color.Silver;
            // 
            // shape1
            // 
            this.shape1.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0D), Telerik.Reporting.Drawing.Unit.Cm(0.77125054597854614D));
            this.shape1.Name = "shape1";
            this.shape1.ShapeType = new Telerik.Reporting.Drawing.Shapes.LineShape(Telerik.Reporting.Drawing.Shapes.LineDirection.EW);
            this.shape1.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(17.999898910522461D), Telerik.Reporting.Drawing.Unit.Cm(0.19999989867210388D));
            this.shape1.Style.LineStyle = Telerik.Reporting.Drawing.LineStyle.Dashed;
            // 
            // textBox9
            // 
            this.textBox9.CanGrow = true;
            this.textBox9.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D), Telerik.Reporting.Drawing.Unit.Cm(0.19479532539844513D));
            this.textBox9.Name = "textBox9";
            this.textBox9.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.8162508010864258D), Telerik.Reporting.Drawing.Unit.Cm(0.4505251944065094D));
            this.textBox9.Style.Font.Bold = true;
            this.textBox9.Style.Font.Name = "Verdana";
            this.textBox9.Style.Font.Size = Telerik.Reporting.Drawing.Unit.Point(8D);
            this.textBox9.Style.Padding.Left = Telerik.Reporting.Drawing.Unit.Cm(0.20000000298023224D);
            this.textBox9.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Left;
            this.textBox9.StyleName = "Data";
            this.textBox9.Value = "Σύνολο Αντλίας";
            // 
            // textBox13
            // 
            this.textBox13.CanGrow = true;
            this.textBox13.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D), Telerik.Reporting.Drawing.Unit.Cm(0.19479532539844513D));
            this.textBox13.Name = "textBox13";
            this.textBox13.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.8162508010864258D), Telerik.Reporting.Drawing.Unit.Cm(0.4505251944065094D));
            this.textBox13.Style.Font.Bold = true;
            this.textBox13.Style.Font.Name = "Verdana";
            this.textBox13.Style.Font.Size = Telerik.Reporting.Drawing.Unit.Point(8D);
            this.textBox13.Style.Padding.Left = Telerik.Reporting.Drawing.Unit.Cm(0.20000000298023224D);
            this.textBox13.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Left;
            this.textBox13.StyleName = "Data";
            this.textBox13.Value = "Γενικό Σύνολο";
            // 
            // textBox14
            // 
            this.textBox14.CanGrow = true;
            this.textBox14.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(3.9145572185516357D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.textBox14.Name = "textBox14";
            this.textBox14.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(1.4997998476028442D), Telerik.Reporting.Drawing.Unit.Cm(0.99999988079071045D));
            this.textBox14.Style.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(121)))), ((int)(((byte)(167)))), ((int)(((byte)(227)))));
            this.textBox14.Style.Font.Name = "Verdana";
            this.textBox14.Style.Font.Size = Telerik.Reporting.Drawing.Unit.Point(9D);
            this.textBox14.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            this.textBox14.StyleName = "Caption";
            this.textBox14.Value = "Κινήσεις";
            // 
            // textBox15
            // 
            this.textBox15.CanGrow = true;
            this.textBox15.Format = "{0:N0}";
            this.textBox15.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(3.9318592548370361D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.textBox15.Name = "textBox15";
            this.textBox15.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(1.4997998476028442D), Telerik.Reporting.Drawing.Unit.Cm(0.58843386173248291D));
            this.textBox15.Style.Font.Name = "Verdana";
            this.textBox15.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            this.textBox15.StyleName = "Data";
            this.textBox15.Value = "=Count(Fields.InvoiceLineId)";
            // 
            // textBox3
            // 
            this.textBox3.CanGrow = true;
            this.textBox3.Format = "{0:N0}";
            this.textBox3.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(3.9145567417144775D), Telerik.Reporting.Drawing.Unit.Cm(0.12584099173545837D));
            this.textBox3.Name = "textBox3";
            this.textBox3.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(1.4997998476028442D), Telerik.Reporting.Drawing.Unit.Cm(0.58843386173248291D));
            this.textBox3.Style.Font.Bold = true;
            this.textBox3.Style.Font.Name = "Verdana";
            this.textBox3.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            this.textBox3.StyleName = "Data";
            this.textBox3.Value = "=Count(Fields.InvoiceLineId)";
            // 
            // textBox7
            // 
            this.textBox7.CanGrow = true;
            this.textBox7.Format = "{0:N0}";
            this.textBox7.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(3.9145567417144775D), Telerik.Reporting.Drawing.Unit.Cm(0.12584099173545837D));
            this.textBox7.Name = "textBox7";
            this.textBox7.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(1.4997998476028442D), Telerik.Reporting.Drawing.Unit.Cm(0.58843386173248291D));
            this.textBox7.Style.Font.Bold = true;
            this.textBox7.Style.Font.Name = "Verdana";
            this.textBox7.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            this.textBox7.StyleName = "Data";
            this.textBox7.Value = "=Count(Fields.InvoiceLineId)";
            // 
            // LiterCheckReport
            // 
            this.DataSource = this.invoiceLineDS;
            this.Filters.Add(new Telerik.Reporting.Filter("=Fields.SalesTransaction.TransactionTimeStamp", Telerik.Reporting.FilterOperator.LessOrEqual, "=Parameters.DateTo.Value"));
            this.Filters.Add(new Telerik.Reporting.Filter("=Fields.SalesTransaction.TransactionTimeStamp", Telerik.Reporting.FilterOperator.GreaterOrEqual, "=Parameters.DateFrom.Value"));
            this.Filters.Add(new Telerik.Reporting.Filter("=Fields.Invoice.InvoiceTypeId.ToString().ToLower()", Telerik.Reporting.FilterOperator.Equal, "=Parameters.InvoiceTypeId.Value.ToLower()"));
            group1.GroupFooter = this.labelsGroupFooterSection;
            group1.GroupHeader = this.labelsGroupHeaderSection;
            group1.Name = "labelsGroup";
            group2.GroupFooter = this.salesTransactionNozzleDispenserOfficialPumpNumberGroupFooterSection;
            group2.GroupHeader = this.salesTransactionNozzleDispenserOfficialPumpNumberGroupHeaderSection;
            group2.Groupings.Add(new Telerik.Reporting.Grouping("=Fields.SalesTransaction.Nozzle.Dispenser.OfficialPumpNumber"));
            group2.Name = "salesTransactionNozzleDispenserOfficialPumpNumberGroup";
            group3.GroupFooter = this.salesTransactionNozzleOfficialNozzleNumberGroupFooterSection;
            group3.GroupHeader = this.salesTransactionNozzleOfficialNozzleNumberGroupHeaderSection;
            group3.Groupings.Add(new Telerik.Reporting.Grouping("=Fields.SalesTransaction.Nozzle.OfficialNozzleNumber"));
            group3.Name = "salesTransactionNozzleOfficialNozzleNumberGroup";
            this.Groups.AddRange(new Telerik.Reporting.Group[] {
            group1,
            group2,
            group3});
            this.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.labelsGroupHeaderSection,
            this.labelsGroupFooterSection,
            this.salesTransactionNozzleDispenserOfficialPumpNumberGroupHeaderSection,
            this.salesTransactionNozzleDispenserOfficialPumpNumberGroupFooterSection,
            this.salesTransactionNozzleOfficialNozzleNumberGroupHeaderSection,
            this.salesTransactionNozzleOfficialNozzleNumberGroupFooterSection,
            this.pageFooter,
            this.reportHeader,
            this.reportFooter,
            this.detail});
            this.Name = "LiterCheckReport";
            this.PageSettings.Landscape = false;
            this.PageSettings.Margins = new Telerik.Reporting.Drawing.MarginsU(Telerik.Reporting.Drawing.Unit.Mm(15D), Telerik.Reporting.Drawing.Unit.Mm(15D), Telerik.Reporting.Drawing.Unit.Mm(15D), Telerik.Reporting.Drawing.Unit.Mm(15D));
            this.PageSettings.PaperKind = System.Drawing.Printing.PaperKind.A4;
            reportParameter1.Name = "DateFrom";
            reportParameter1.Type = Telerik.Reporting.ReportParameterType.DateTime;
            reportParameter1.Value = "2014/01/01";
            reportParameter2.Name = "DateTo";
            reportParameter2.Type = Telerik.Reporting.ReportParameterType.DateTime;
            reportParameter2.Value = "2014/12/31";
            reportParameter3.Name = "InvoiceTypeId";
            this.ReportParameters.Add(reportParameter1);
            this.ReportParameters.Add(reportParameter2);
            this.ReportParameters.Add(reportParameter3);
            this.Style.BackgroundColor = System.Drawing.Color.White;
            this.Style.Font.Name = "Verdana";
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
            this.Width = Telerik.Reporting.Drawing.Unit.Cm(17.999998092651367D);
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();

        }
        #endregion

        private Telerik.Reporting.ObjectDataSource invoiceLineDS;
        private Telerik.Reporting.GroupHeaderSection labelsGroupHeaderSection;
        private Telerik.Reporting.TextBox textBox1;
        private Telerik.Reporting.TextBox textBox2;
        private Telerik.Reporting.TextBox totalPriceCaptionTextBox;
        private Telerik.Reporting.TextBox volumeCaptionTextBox;
        private Telerik.Reporting.TextBox volumeNormalizedCaptionTextBox;
        private Telerik.Reporting.TextBox textBox4;
        private Telerik.Reporting.GroupFooterSection labelsGroupFooterSection;
        private Telerik.Reporting.GroupHeaderSection salesTransactionNozzleDispenserOfficialPumpNumberGroupHeaderSection;
        private Telerik.Reporting.TextBox textBox5;
        private Telerik.Reporting.GroupFooterSection salesTransactionNozzleDispenserOfficialPumpNumberGroupFooterSection;
        private Telerik.Reporting.GroupHeaderSection salesTransactionNozzleOfficialNozzleNumberGroupHeaderSection;
        private Telerik.Reporting.TextBox textBox6;
        private Telerik.Reporting.TextBox totalPriceDataTextBox;
        private Telerik.Reporting.TextBox volumeDataTextBox;
        private Telerik.Reporting.TextBox volumeNormalizedDataTextBox;
        private Telerik.Reporting.TextBox textBox8;
        private Telerik.Reporting.GroupFooterSection salesTransactionNozzleOfficialNozzleNumberGroupFooterSection;
        private Telerik.Reporting.PageFooterSection pageFooter;
        private Telerik.Reporting.TextBox currentTimeTextBox;
        private Telerik.Reporting.TextBox pageInfoTextBox;
        private Telerik.Reporting.ReportHeaderSection reportHeader;
        private Telerik.Reporting.TextBox titleTextBox;
        private Telerik.Reporting.ReportFooterSection reportFooter;
        private Telerik.Reporting.DetailSection detail;
        private Telerik.Reporting.TextBox textBox12;
        private Telerik.Reporting.TextBox textBox11;
        private Telerik.Reporting.TextBox textBox10;
        private Telerik.Reporting.TextBox textBox16;
        private Telerik.Reporting.TextBox textBox17;
        private Telerik.Reporting.TextBox textBox18;
        private Telerik.Reporting.Shape shape2;
        private Telerik.Reporting.Shape shape1;
        private Telerik.Reporting.TextBox textBox9;
        private Telerik.Reporting.TextBox textBox14;
        private Telerik.Reporting.TextBox textBox13;
        private Telerik.Reporting.TextBox textBox15;
        private Telerik.Reporting.TextBox textBox7;
        private Telerik.Reporting.TextBox textBox3;

    }
}