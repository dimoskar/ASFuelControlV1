namespace ASFuelControl.Reports
{
    partial class ShiftReportDispenser
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
            Telerik.Reporting.Drawing.StyleRule styleRule1 = new Telerik.Reporting.Drawing.StyleRule();
            Telerik.Reporting.Drawing.StyleRule styleRule2 = new Telerik.Reporting.Drawing.StyleRule();
            Telerik.Reporting.Drawing.StyleRule styleRule3 = new Telerik.Reporting.Drawing.StyleRule();
            Telerik.Reporting.Drawing.StyleRule styleRule4 = new Telerik.Reporting.Drawing.StyleRule();
            this.labelsGroupFooterSection = new Telerik.Reporting.GroupFooterSection();
            this.textBox9 = new Telerik.Reporting.TextBox();
            this.textBox10 = new Telerik.Reporting.TextBox();
            this.textBox11 = new Telerik.Reporting.TextBox();
            this.textBox12 = new Telerik.Reporting.TextBox();
            this.textBox17 = new Telerik.Reporting.TextBox();
            this.labelsGroupHeaderSection = new Telerik.Reporting.GroupHeaderSection();
            this.textBox1 = new Telerik.Reporting.TextBox();
            this.textBox2 = new Telerik.Reporting.TextBox();
            this.totalPriceCaptionTextBox = new Telerik.Reporting.TextBox();
            this.volumeCaptionTextBox = new Telerik.Reporting.TextBox();
            this.volumeNormalizedCaptionTextBox = new Telerik.Reporting.TextBox();
            this.salesTransactionIdCaptionTextBox = new Telerik.Reporting.TextBox();
            this.nozzleDispenserOfficialPumpNumberGroupFooterSection = new Telerik.Reporting.GroupFooterSection();
            this.textBox13 = new Telerik.Reporting.TextBox();
            this.textBox14 = new Telerik.Reporting.TextBox();
            this.textBox15 = new Telerik.Reporting.TextBox();
            this.textBox16 = new Telerik.Reporting.TextBox();
            this.textBox18 = new Telerik.Reporting.TextBox();
            this.shape1 = new Telerik.Reporting.Shape();
            this.nozzleDispenserOfficialPumpNumberGroupHeaderSection = new Telerik.Reporting.GroupHeaderSection();
            this.textBox3 = new Telerik.Reporting.TextBox();
            this.nozzleOfficialNozzleNumberGroupFooterSection = new Telerik.Reporting.GroupFooterSection();
            this.nozzleOfficialNozzleNumberGroupHeaderSection = new Telerik.Reporting.GroupHeaderSection();
            this.textBox4 = new Telerik.Reporting.TextBox();
            this.totalPriceDataTextBox = new Telerik.Reporting.TextBox();
            this.volumeDataTextBox = new Telerik.Reporting.TextBox();
            this.volumeNormalizedDataTextBox = new Telerik.Reporting.TextBox();
            this.salesTransactionIdDataTextBox = new Telerik.Reporting.TextBox();
            this.shape2 = new Telerik.Reporting.Shape();
            this.Sales = new Telerik.Reporting.OpenAccessDataSource();
            this.pageFooter = new Telerik.Reporting.PageFooterSection();
            this.currentTimeTextBox = new Telerik.Reporting.TextBox();
            this.pageInfoTextBox = new Telerik.Reporting.TextBox();
            this.reportHeader = new Telerik.Reporting.ReportHeaderSection();
            this.textBox19 = new Telerik.Reporting.TextBox();
            this.reportFooter = new Telerik.Reporting.ReportFooterSection();
            this.detail = new Telerik.Reporting.DetailSection();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            // 
            // labelsGroupFooterSection
            // 
            this.labelsGroupFooterSection.Height = Telerik.Reporting.Drawing.Unit.Cm(0.4999997615814209D);
            this.labelsGroupFooterSection.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.textBox9,
            this.textBox10,
            this.textBox11,
            this.textBox12,
            this.textBox17});
            this.labelsGroupFooterSection.Name = "labelsGroupFooterSection";
            this.labelsGroupFooterSection.Style.Visible = true;
            // 
            // textBox9
            // 
            this.textBox9.CanGrow = true;
            this.textBox9.Format = "{0:N2}€";
            this.textBox9.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(12.780826568603516D), Telerik.Reporting.Drawing.Unit.Cm(0.00010012308484874666D));
            this.textBox9.Name = "textBox9";
            this.textBox9.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.5739583969116211D), Telerik.Reporting.Drawing.Unit.Cm(0.4505251944065094D));
            this.textBox9.Style.Font.Bold = true;
            this.textBox9.Style.Font.Name = "Verdana";
            this.textBox9.Style.Font.Size = Telerik.Reporting.Drawing.Unit.Point(10D);
            this.textBox9.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.textBox9.StyleName = "Data";
            this.textBox9.Value = "= Avg(Fields.TotalPrice)";
            // 
            // textBox10
            // 
            this.textBox10.CanGrow = true;
            this.textBox10.Format = "{0:N0}";
            this.textBox10.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(10.153950691223145D), Telerik.Reporting.Drawing.Unit.Cm(0.00010012308484874666D));
            this.textBox10.Name = "textBox10";
            this.textBox10.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.5739583969116211D), Telerik.Reporting.Drawing.Unit.Cm(0.4505251944065094D));
            this.textBox10.Style.Font.Bold = true;
            this.textBox10.Style.Font.Name = "Verdana";
            this.textBox10.Style.Font.Size = Telerik.Reporting.Drawing.Unit.Point(10D);
            this.textBox10.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.textBox10.StyleName = "Data";
            this.textBox10.Value = "=Count(Fields.SalesTransactionId)";
            // 
            // textBox11
            // 
            this.textBox11.CanGrow = true;
            this.textBox11.Format = "{0:N2}Lt";
            this.textBox11.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(7.52707576751709D), Telerik.Reporting.Drawing.Unit.Cm(0.00010012308484874666D));
            this.textBox11.Name = "textBox11";
            this.textBox11.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.5739583969116211D), Telerik.Reporting.Drawing.Unit.Cm(0.4505251944065094D));
            this.textBox11.Style.Font.Bold = true;
            this.textBox11.Style.Font.Name = "Verdana";
            this.textBox11.Style.Font.Size = Telerik.Reporting.Drawing.Unit.Point(10D);
            this.textBox11.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.textBox11.StyleName = "Data";
            this.textBox11.Value = "=Sum(Fields.Volume)";
            // 
            // textBox12
            // 
            this.textBox12.CanGrow = true;
            this.textBox12.Format = "{0:N2}€";
            this.textBox12.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(4.9002008438110352D), Telerik.Reporting.Drawing.Unit.Cm(0.00010012308484874666D));
            this.textBox12.Name = "textBox12";
            this.textBox12.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.5739583969116211D), Telerik.Reporting.Drawing.Unit.Cm(0.4505251944065094D));
            this.textBox12.Style.Font.Bold = true;
            this.textBox12.Style.Font.Name = "Verdana";
            this.textBox12.Style.Font.Size = Telerik.Reporting.Drawing.Unit.Point(10D);
            this.textBox12.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.textBox12.StyleName = "Data";
            this.textBox12.Value = "=Sum(Fields.TotalPrice)";
            // 
            // textBox17
            // 
            this.textBox17.CanGrow = true;
            this.textBox17.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0D), Telerik.Reporting.Drawing.Unit.Cm(0D));
            this.textBox17.Name = "textBox17";
            this.textBox17.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(3.9000000953674316D), Telerik.Reporting.Drawing.Unit.Cm(0.4505251944065094D));
            this.textBox17.Style.Font.Bold = true;
            this.textBox17.Style.Font.Name = "Verdana";
            this.textBox17.Style.Font.Size = Telerik.Reporting.Drawing.Unit.Point(10D);
            this.textBox17.Style.Padding.Left = Telerik.Reporting.Drawing.Unit.Cm(0.20000000298023224D);
            this.textBox17.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Left;
            this.textBox17.StyleName = "Data";
            this.textBox17.Value = "Γενικό Σύνολο";
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
            this.salesTransactionIdCaptionTextBox});
            this.labelsGroupHeaderSection.Name = "labelsGroupHeaderSection";
            this.labelsGroupHeaderSection.PrintOnEveryPage = true;
            // 
            // textBox1
            // 
            this.textBox1.CanGrow = true;
            this.textBox1.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(1.9470832347869873D), Telerik.Reporting.Drawing.Unit.Cm(0.99999988079071045D));
            this.textBox1.Style.Color = System.Drawing.Color.White;
            this.textBox1.Style.Font.Name = "Verdana";
            this.textBox1.Style.Padding.Left = Telerik.Reporting.Drawing.Unit.Cm(0.20000000298023224D);
            this.textBox1.Style.Padding.Right = Telerik.Reporting.Drawing.Unit.Cm(0.20000000298023224D);
            this.textBox1.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            this.textBox1.StyleName = "Caption";
            this.textBox1.Value = "Αρ. Αντλίας";
            // 
            // textBox2
            // 
            this.textBox2.CanGrow = true;
            this.textBox2.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(2.0423731803894043D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.8576269149780273D), Telerik.Reporting.Drawing.Unit.Cm(0.99999988079071045D));
            this.textBox2.Style.Color = System.Drawing.Color.White;
            this.textBox2.Style.Font.Name = "Verdana";
            this.textBox2.Style.Padding.Left = Telerik.Reporting.Drawing.Unit.Cm(0.20000000298023224D);
            this.textBox2.Style.Padding.Right = Telerik.Reporting.Drawing.Unit.Cm(0.20000000298023224D);
            this.textBox2.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            this.textBox2.StyleName = "Caption";
            this.textBox2.Value = "Ακροσωλήνιο";
            // 
            // totalPriceCaptionTextBox
            // 
            this.totalPriceCaptionTextBox.CanGrow = true;
            this.totalPriceCaptionTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(4.9002008438110352D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.totalPriceCaptionTextBox.Name = "totalPriceCaptionTextBox";
            this.totalPriceCaptionTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.5739583969116211D), Telerik.Reporting.Drawing.Unit.Cm(0.99999988079071045D));
            this.totalPriceCaptionTextBox.Style.Color = System.Drawing.Color.White;
            this.totalPriceCaptionTextBox.Style.Font.Name = "Verdana";
            this.totalPriceCaptionTextBox.Style.Padding.Left = Telerik.Reporting.Drawing.Unit.Cm(0.20000000298023224D);
            this.totalPriceCaptionTextBox.Style.Padding.Right = Telerik.Reporting.Drawing.Unit.Cm(0.20000000298023224D);
            this.totalPriceCaptionTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            this.totalPriceCaptionTextBox.StyleName = "Caption";
            this.totalPriceCaptionTextBox.Value = "Σύνολο";
            // 
            // volumeCaptionTextBox
            // 
            this.volumeCaptionTextBox.CanGrow = true;
            this.volumeCaptionTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(7.5165324211120605D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.volumeCaptionTextBox.Name = "volumeCaptionTextBox";
            this.volumeCaptionTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.5739583969116211D), Telerik.Reporting.Drawing.Unit.Cm(0.99999988079071045D));
            this.volumeCaptionTextBox.Style.Color = System.Drawing.Color.White;
            this.volumeCaptionTextBox.Style.Font.Name = "Verdana";
            this.volumeCaptionTextBox.Style.Padding.Left = Telerik.Reporting.Drawing.Unit.Cm(0.20000000298023224D);
            this.volumeCaptionTextBox.Style.Padding.Right = Telerik.Reporting.Drawing.Unit.Cm(0.20000000298023224D);
            this.volumeCaptionTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            this.volumeCaptionTextBox.StyleName = "Caption";
            this.volumeCaptionTextBox.Value = "Όγκος";
            // 
            // volumeNormalizedCaptionTextBox
            // 
            this.volumeNormalizedCaptionTextBox.CanGrow = true;
            this.volumeNormalizedCaptionTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(10.132863998413086D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.volumeNormalizedCaptionTextBox.Name = "volumeNormalizedCaptionTextBox";
            this.volumeNormalizedCaptionTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.5739583969116211D), Telerik.Reporting.Drawing.Unit.Cm(0.99999988079071045D));
            this.volumeNormalizedCaptionTextBox.Style.Color = System.Drawing.Color.White;
            this.volumeNormalizedCaptionTextBox.Style.Font.Name = "Verdana";
            this.volumeNormalizedCaptionTextBox.Style.Padding.Left = Telerik.Reporting.Drawing.Unit.Cm(0.20000000298023224D);
            this.volumeNormalizedCaptionTextBox.Style.Padding.Right = Telerik.Reporting.Drawing.Unit.Cm(0.20000000298023224D);
            this.volumeNormalizedCaptionTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            this.volumeNormalizedCaptionTextBox.StyleName = "Caption";
            this.volumeNormalizedCaptionTextBox.Value = "Κινήσεις";
            // 
            // salesTransactionIdCaptionTextBox
            // 
            this.salesTransactionIdCaptionTextBox.CanGrow = true;
            this.salesTransactionIdCaptionTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(12.74919605255127D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.salesTransactionIdCaptionTextBox.Name = "salesTransactionIdCaptionTextBox";
            this.salesTransactionIdCaptionTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.5739583969116211D), Telerik.Reporting.Drawing.Unit.Cm(0.99999988079071045D));
            this.salesTransactionIdCaptionTextBox.Style.Color = System.Drawing.Color.White;
            this.salesTransactionIdCaptionTextBox.Style.Font.Name = "Verdana";
            this.salesTransactionIdCaptionTextBox.Style.Padding.Left = Telerik.Reporting.Drawing.Unit.Cm(0.20000000298023224D);
            this.salesTransactionIdCaptionTextBox.Style.Padding.Right = Telerik.Reporting.Drawing.Unit.Cm(0.20000000298023224D);
            this.salesTransactionIdCaptionTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            this.salesTransactionIdCaptionTextBox.StyleName = "Caption";
            this.salesTransactionIdCaptionTextBox.Value = "€ / Κινήση";
            // 
            // nozzleDispenserOfficialPumpNumberGroupFooterSection
            // 
            this.nozzleDispenserOfficialPumpNumberGroupFooterSection.Height = Telerik.Reporting.Drawing.Unit.Cm(0.89145678281784058D);
            this.nozzleDispenserOfficialPumpNumberGroupFooterSection.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.textBox13,
            this.textBox14,
            this.textBox15,
            this.textBox16,
            this.textBox18,
            this.shape1});
            this.nozzleDispenserOfficialPumpNumberGroupFooterSection.Name = "nozzleDispenserOfficialPumpNumberGroupFooterSection";
            // 
            // textBox13
            // 
            this.textBox13.CanGrow = true;
            this.textBox13.Format = "{0:N2}€";
            this.textBox13.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(4.9002008438110352D), Telerik.Reporting.Drawing.Unit.Cm(0D));
            this.textBox13.Name = "textBox13";
            this.textBox13.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.5739583969116211D), Telerik.Reporting.Drawing.Unit.Cm(0.55062568187713623D));
            this.textBox13.Style.Font.Bold = true;
            this.textBox13.Style.Font.Name = "Verdana";
            this.textBox13.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.textBox13.StyleName = "Data";
            this.textBox13.Value = "=Sum(Fields.TotalPrice)";
            // 
            // textBox14
            // 
            this.textBox14.CanGrow = true;
            this.textBox14.Format = "{0:N2}Lt";
            this.textBox14.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(7.52707576751709D), Telerik.Reporting.Drawing.Unit.Cm(0D));
            this.textBox14.Name = "textBox14";
            this.textBox14.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.5739583969116211D), Telerik.Reporting.Drawing.Unit.Cm(0.55062568187713623D));
            this.textBox14.Style.Font.Bold = true;
            this.textBox14.Style.Font.Name = "Verdana";
            this.textBox14.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.textBox14.StyleName = "Data";
            this.textBox14.Value = "=Sum(Fields.Volume)";
            // 
            // textBox15
            // 
            this.textBox15.CanGrow = true;
            this.textBox15.Format = "{0:N0}";
            this.textBox15.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(10.153950691223145D), Telerik.Reporting.Drawing.Unit.Cm(0D));
            this.textBox15.Name = "textBox15";
            this.textBox15.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.5739583969116211D), Telerik.Reporting.Drawing.Unit.Cm(0.55062568187713623D));
            this.textBox15.Style.Font.Bold = true;
            this.textBox15.Style.Font.Name = "Verdana";
            this.textBox15.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.textBox15.StyleName = "Data";
            this.textBox15.Value = "=Count(Fields.SalesTransactionId)";
            // 
            // textBox16
            // 
            this.textBox16.CanGrow = true;
            this.textBox16.Format = "{0:N2}€";
            this.textBox16.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(12.780826568603516D), Telerik.Reporting.Drawing.Unit.Cm(0D));
            this.textBox16.Name = "textBox16";
            this.textBox16.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.5739583969116211D), Telerik.Reporting.Drawing.Unit.Cm(0.55062568187713623D));
            this.textBox16.Style.Font.Bold = true;
            this.textBox16.Style.Font.Name = "Verdana";
            this.textBox16.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.textBox16.StyleName = "Data";
            this.textBox16.Value = "= Avg(Fields.TotalPrice)";
            // 
            // textBox18
            // 
            this.textBox18.CanGrow = true;
            this.textBox18.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0D), Telerik.Reporting.Drawing.Unit.Cm(0.041580956429243088D));
            this.textBox18.Name = "textBox18";
            this.textBox18.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(3.9000000953674316D), Telerik.Reporting.Drawing.Unit.Cm(0.4505251944065094D));
            this.textBox18.Style.Font.Bold = true;
            this.textBox18.Style.Font.Name = "Verdana";
            this.textBox18.Style.Font.Size = Telerik.Reporting.Drawing.Unit.Point(9D);
            this.textBox18.Style.Padding.Left = Telerik.Reporting.Drawing.Unit.Cm(0.20000000298023224D);
            this.textBox18.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Left;
            this.textBox18.StyleName = "Data";
            this.textBox18.Value = "Σύνολο Αντλίας";
            // 
            // shape1
            // 
            this.shape1.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0D), Telerik.Reporting.Drawing.Unit.Cm(0.591457724571228D));
            this.shape1.Name = "shape1";
            this.shape1.ShapeType = new Telerik.Reporting.Drawing.Shapes.LineShape(Telerik.Reporting.Drawing.Shapes.LineDirection.EW);
            this.shape1.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(15.35478401184082D), Telerik.Reporting.Drawing.Unit.Cm(0.19999989867210388D));
            this.shape1.Style.LineStyle = Telerik.Reporting.Drawing.LineStyle.Dashed;
            // 
            // nozzleDispenserOfficialPumpNumberGroupHeaderSection
            // 
            this.nozzleDispenserOfficialPumpNumberGroupHeaderSection.Height = Telerik.Reporting.Drawing.Unit.Cm(0.58249956369400024D);
            this.nozzleDispenserOfficialPumpNumberGroupHeaderSection.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.textBox3});
            this.nozzleDispenserOfficialPumpNumberGroupHeaderSection.Name = "nozzleDispenserOfficialPumpNumberGroupHeaderSection";
            // 
            // textBox3
            // 
            this.textBox3.CanGrow = true;
            this.textBox3.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.textBox3.Name = "textBox3";
            this.textBox3.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(1.9470828771591187D), Telerik.Reporting.Drawing.Unit.Cm(0.5295829176902771D));
            this.textBox3.Style.Font.Name = "Verdana";
            this.textBox3.StyleName = "Data";
            this.textBox3.Value = "=Fields.Nozzle.Dispenser.OfficialPumpNumber";
            // 
            // nozzleOfficialNozzleNumberGroupFooterSection
            // 
            this.nozzleOfficialNozzleNumberGroupFooterSection.Height = Telerik.Reporting.Drawing.Unit.Cm(0.71437495946884155D);
            this.nozzleOfficialNozzleNumberGroupFooterSection.Name = "nozzleOfficialNozzleNumberGroupFooterSection";
            this.nozzleOfficialNozzleNumberGroupFooterSection.Style.Visible = false;
            // 
            // nozzleOfficialNozzleNumberGroupHeaderSection
            // 
            this.nozzleOfficialNozzleNumberGroupHeaderSection.Height = Telerik.Reporting.Drawing.Unit.Cm(0.81166714429855347D);
            this.nozzleOfficialNozzleNumberGroupHeaderSection.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.textBox4,
            this.totalPriceDataTextBox,
            this.volumeDataTextBox,
            this.volumeNormalizedDataTextBox,
            this.salesTransactionIdDataTextBox,
            this.shape2});
            this.nozzleOfficialNozzleNumberGroupHeaderSection.Name = "nozzleOfficialNozzleNumberGroupHeaderSection";
            // 
            // textBox4
            // 
            this.textBox4.CanGrow = true;
            this.textBox4.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(2.000199556350708D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.textBox4.Name = "textBox4";
            this.textBox4.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.8998007774353027D), Telerik.Reporting.Drawing.Unit.Cm(0.52375072240829468D));
            this.textBox4.Style.Font.Name = "Verdana";
            this.textBox4.StyleName = "Data";
            this.textBox4.Value = "=Fields.Nozzle.OfficialNozzleNumber + \"(\" + Fields.Nozzle.FuelType.Name + \")\"";
            // 
            // totalPriceDataTextBox
            // 
            this.totalPriceDataTextBox.CanGrow = true;
            this.totalPriceDataTextBox.Format = "{0:N2}€";
            this.totalPriceDataTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(4.9002008438110352D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.totalPriceDataTextBox.Name = "totalPriceDataTextBox";
            this.totalPriceDataTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.5739583969116211D), Telerik.Reporting.Drawing.Unit.Cm(0.52375072240829468D));
            this.totalPriceDataTextBox.Style.Font.Name = "Verdana";
            this.totalPriceDataTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.totalPriceDataTextBox.StyleName = "Data";
            this.totalPriceDataTextBox.Value = "=Sum(Fields.TotalPrice)";
            // 
            // volumeDataTextBox
            // 
            this.volumeDataTextBox.CanGrow = true;
            this.volumeDataTextBox.Format = "{0:N2}Lt";
            this.volumeDataTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(7.52707576751709D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.volumeDataTextBox.Name = "volumeDataTextBox";
            this.volumeDataTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.5739583969116211D), Telerik.Reporting.Drawing.Unit.Cm(0.52375072240829468D));
            this.volumeDataTextBox.Style.Font.Name = "Verdana";
            this.volumeDataTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.volumeDataTextBox.StyleName = "Data";
            this.volumeDataTextBox.Value = "=Sum(Fields.Volume)";
            // 
            // volumeNormalizedDataTextBox
            // 
            this.volumeNormalizedDataTextBox.CanGrow = true;
            this.volumeNormalizedDataTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(10.153950691223145D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.volumeNormalizedDataTextBox.Name = "volumeNormalizedDataTextBox";
            this.volumeNormalizedDataTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.5739583969116211D), Telerik.Reporting.Drawing.Unit.Cm(0.52375072240829468D));
            this.volumeNormalizedDataTextBox.Style.Font.Name = "Verdana";
            this.volumeNormalizedDataTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.volumeNormalizedDataTextBox.StyleName = "Data";
            this.volumeNormalizedDataTextBox.Value = "=Count(Fields.SalesTransactionId)";
            // 
            // salesTransactionIdDataTextBox
            // 
            this.salesTransactionIdDataTextBox.CanGrow = true;
            this.salesTransactionIdDataTextBox.Format = "{0:N2}€";
            this.salesTransactionIdDataTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(12.780826568603516D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.salesTransactionIdDataTextBox.Name = "salesTransactionIdDataTextBox";
            this.salesTransactionIdDataTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.5739583969116211D), Telerik.Reporting.Drawing.Unit.Cm(0.52375072240829468D));
            this.salesTransactionIdDataTextBox.Style.Font.Name = "Verdana";
            this.salesTransactionIdDataTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.salesTransactionIdDataTextBox.StyleName = "Data";
            this.salesTransactionIdDataTextBox.Value = "= Avg(Fields.TotalPrice)";
            // 
            // shape2
            // 
            this.shape2.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0.026458332315087318D), Telerik.Reporting.Drawing.Unit.Cm(0.611667275428772D));
            this.shape2.Name = "shape2";
            this.shape2.ShapeType = new Telerik.Reporting.Drawing.Shapes.LineShape(Telerik.Reporting.Drawing.Shapes.LineDirection.EW);
            this.shape2.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(15.328326225280762D), Telerik.Reporting.Drawing.Unit.Cm(0.19999989867210388D));
            this.shape2.Style.Color = System.Drawing.Color.Silver;
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
            this.pageFooter.Height = Telerik.Reporting.Drawing.Unit.Cm(3.0856254100799561D);
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
            this.pageInfoTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(7.4212422370910645D), Telerik.Reporting.Drawing.Unit.Cm(0.99999988079071045D));
            this.pageInfoTextBox.Style.Font.Name = "Verdana";
            this.pageInfoTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.pageInfoTextBox.StyleName = "PageInfo";
            this.pageInfoTextBox.Value = "=PageNumber";
            // 
            // reportHeader
            // 
            this.reportHeader.Height = Telerik.Reporting.Drawing.Unit.Cm(0.89999997615814209D);
            this.reportHeader.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.textBox19});
            this.reportHeader.Name = "reportHeader";
            // 
            // textBox19
            // 
            this.textBox19.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0D), Telerik.Reporting.Drawing.Unit.Cm(9.9921220680698752E-05D));
            this.textBox19.Name = "textBox19";
            this.textBox19.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(15.354784965515137D), Telerik.Reporting.Drawing.Unit.Cm(0.800000011920929D));
            this.textBox19.Style.Font.Name = "Verdana";
            this.textBox19.Style.Font.Size = Telerik.Reporting.Drawing.Unit.Point(14D);
            this.textBox19.StyleName = "Title";
            this.textBox19.Value = "Εκροές ανα Αντλία";
            // 
            // reportFooter
            // 
            this.reportFooter.Height = Telerik.Reporting.Drawing.Unit.Cm(0.71437495946884155D);
            this.reportFooter.Name = "reportFooter";
            // 
            // detail
            // 
            this.detail.Height = Telerik.Reporting.Drawing.Unit.Cm(1.1058331727981567D);
            this.detail.Name = "detail";
            this.detail.Style.Visible = false;
            // 
            // ShiftReportDispenser
            // 
            this.DataSource = this.Sales;
            this.Filters.Add(new Telerik.Reporting.Filter("=IsNull(Fields.ShiftId, \"\").ToString().ToLower()", Telerik.Reporting.FilterOperator.Equal, "=Parameters.ShiftId.Value.ToLower()"));
            this.Filters.Add(new Telerik.Reporting.Filter("= IsNull(Fields.IsErrorResolving, false)", Telerik.Reporting.FilterOperator.Equal, "false"));
            this.Filters.Add(new Telerik.Reporting.Filter("= ASFuelControl.Reports.ShiftReportFunctions.IsInternale(Fields.InvoiceLines)", Telerik.Reporting.FilterOperator.Equal, "false"));
            group1.GroupFooter = this.labelsGroupFooterSection;
            group1.GroupHeader = this.labelsGroupHeaderSection;
            group1.Name = "labelsGroup";
            group2.GroupFooter = this.nozzleDispenserOfficialPumpNumberGroupFooterSection;
            group2.GroupHeader = this.nozzleDispenserOfficialPumpNumberGroupHeaderSection;
            group2.Groupings.Add(new Telerik.Reporting.Grouping("=Fields.Nozzle.Dispenser.OfficialPumpNumber"));
            group2.Name = "nozzleDispenserOfficialPumpNumberGroup";
            group3.GroupFooter = this.nozzleOfficialNozzleNumberGroupFooterSection;
            group3.GroupHeader = this.nozzleOfficialNozzleNumberGroupHeaderSection;
            group3.Groupings.Add(new Telerik.Reporting.Grouping("=Fields.Nozzle.OfficialNozzleNumber"));
            group3.Name = "nozzleOfficialNozzleNumberGroup";
            this.Groups.AddRange(new Telerik.Reporting.Group[] {
            group1,
            group2,
            group3});
            this.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.labelsGroupHeaderSection,
            this.labelsGroupFooterSection,
            this.nozzleDispenserOfficialPumpNumberGroupHeaderSection,
            this.nozzleDispenserOfficialPumpNumberGroupFooterSection,
            this.nozzleOfficialNozzleNumberGroupHeaderSection,
            this.nozzleOfficialNozzleNumberGroupFooterSection,
            this.pageFooter,
            this.reportHeader,
            this.reportFooter,
            this.detail});
            this.Name = "ShiftReportDispenser";
            this.PageSettings.Margins = new Telerik.Reporting.Drawing.MarginsU(Telerik.Reporting.Drawing.Unit.Mm(25.399999618530273D), Telerik.Reporting.Drawing.Unit.Mm(25.399999618530273D), Telerik.Reporting.Drawing.Unit.Mm(25.399999618530273D), Telerik.Reporting.Drawing.Unit.Mm(25.399999618530273D));
            this.PageSettings.PaperKind = System.Drawing.Printing.PaperKind.A4;
            reportParameter1.Name = "ShiftId";
            reportParameter1.Value = "1377a9d5-2612-462c-bc4d-ea9e420f8d34";
            this.ReportParameters.Add(reportParameter1);
            this.Style.BackgroundColor = System.Drawing.Color.White;
            this.Style.Font.Name = "Arial";
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
            this.Width = Telerik.Reporting.Drawing.Unit.Cm(15.35478401184082D);
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();

        }
        #endregion

        private Telerik.Reporting.OpenAccessDataSource Sales;
        private Telerik.Reporting.GroupHeaderSection labelsGroupHeaderSection;
        private Telerik.Reporting.TextBox textBox1;
        private Telerik.Reporting.TextBox textBox2;
        private Telerik.Reporting.TextBox totalPriceCaptionTextBox;
        private Telerik.Reporting.TextBox volumeCaptionTextBox;
        private Telerik.Reporting.TextBox volumeNormalizedCaptionTextBox;
        private Telerik.Reporting.TextBox salesTransactionIdCaptionTextBox;
        private Telerik.Reporting.GroupFooterSection labelsGroupFooterSection;
        private Telerik.Reporting.GroupHeaderSection nozzleDispenserOfficialPumpNumberGroupHeaderSection;
        private Telerik.Reporting.TextBox textBox3;
        private Telerik.Reporting.GroupFooterSection nozzleDispenserOfficialPumpNumberGroupFooterSection;
        private Telerik.Reporting.GroupHeaderSection nozzleOfficialNozzleNumberGroupHeaderSection;
        private Telerik.Reporting.TextBox textBox4;
        private Telerik.Reporting.GroupFooterSection nozzleOfficialNozzleNumberGroupFooterSection;
        private Telerik.Reporting.PageFooterSection pageFooter;
        private Telerik.Reporting.TextBox currentTimeTextBox;
        private Telerik.Reporting.TextBox pageInfoTextBox;
        private Telerik.Reporting.ReportHeaderSection reportHeader;
        private Telerik.Reporting.ReportFooterSection reportFooter;
        private Telerik.Reporting.DetailSection detail;
        private Telerik.Reporting.TextBox totalPriceDataTextBox;
        private Telerik.Reporting.TextBox volumeDataTextBox;
        private Telerik.Reporting.TextBox volumeNormalizedDataTextBox;
        private Telerik.Reporting.TextBox salesTransactionIdDataTextBox;
        private Telerik.Reporting.TextBox textBox9;
        private Telerik.Reporting.TextBox textBox10;
        private Telerik.Reporting.TextBox textBox11;
        private Telerik.Reporting.TextBox textBox12;
        private Telerik.Reporting.TextBox textBox13;
        private Telerik.Reporting.TextBox textBox14;
        private Telerik.Reporting.TextBox textBox15;
        private Telerik.Reporting.TextBox textBox16;
        private Telerik.Reporting.TextBox textBox17;
        private Telerik.Reporting.TextBox textBox18;
        private Telerik.Reporting.TextBox textBox19;
        private Telerik.Reporting.Shape shape1;
        private Telerik.Reporting.Shape shape2;

    }
}