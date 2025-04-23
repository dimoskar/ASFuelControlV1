namespace ASFuelControl.Reports
{
    partial class NozzleTotalsReport
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
            this.nozzle = new Telerik.Reporting.ObjectDataSource();
            this.dispenserOfficialPumpNumberGroupHeaderSection = new Telerik.Reporting.GroupHeaderSection();
            this.dispenserOfficialPumpNumberGroupFooterSection = new Telerik.Reporting.GroupFooterSection();
            this.textBox1 = new Telerik.Reporting.TextBox();
            this.textBox2 = new Telerik.Reporting.TextBox();
            this.labelsGroupHeaderSection = new Telerik.Reporting.GroupHeaderSection();
            this.labelsGroupFooterSection = new Telerik.Reporting.GroupFooterSection();
            this.officialNozzleNumberCaptionTextBox = new Telerik.Reporting.TextBox();
            this.textBox3 = new Telerik.Reporting.TextBox();
            this.totalCounterCaptionTextBox = new Telerik.Reporting.TextBox();
            this.pageFooter = new Telerik.Reporting.PageFooterSection();
            this.reportHeader = new Telerik.Reporting.ReportHeaderSection();
            this.titleTextBox = new Telerik.Reporting.TextBox();
            this.reportFooter = new Telerik.Reporting.ReportFooterSection();
            this.detail = new Telerik.Reporting.DetailSection();
            this.officialNozzleNumberDataTextBox = new Telerik.Reporting.TextBox();
            this.textBox4 = new Telerik.Reporting.TextBox();
            this.totalCounterDataTextBox = new Telerik.Reporting.TextBox();
            this.pageInfoTextBox = new Telerik.Reporting.TextBox();
            this.textBox5 = new Telerik.Reporting.TextBox();
            this.pictureBox1 = new Telerik.Reporting.PictureBox();
            this.textBox6 = new Telerik.Reporting.TextBox();
            this.textBox7 = new Telerik.Reporting.TextBox();
            this.textBox8 = new Telerik.Reporting.TextBox();
            this.textBox9 = new Telerik.Reporting.TextBox();
            this.textBox10 = new Telerik.Reporting.TextBox();
            this.shape1 = new Telerik.Reporting.Shape();
            this.shape2 = new Telerik.Reporting.Shape();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            // 
            // nozzle
            // 
            this.nozzle.DataSource = "ASFuelControl.Data.Nozzle, ASFuelControl.Data, Version=1.0.0.0, Culture=neutral, " +
    "PublicKeyToken=null";
            this.nozzle.Name = "nozzle";
            // 
            // dispenserOfficialPumpNumberGroupHeaderSection
            // 
            this.dispenserOfficialPumpNumberGroupHeaderSection.Height = Telerik.Reporting.Drawing.Unit.Cm(1.1058331727981567D);
            this.dispenserOfficialPumpNumberGroupHeaderSection.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.textBox1,
            this.textBox2});
            this.dispenserOfficialPumpNumberGroupHeaderSection.Name = "dispenserOfficialPumpNumberGroupHeaderSection";
            // 
            // dispenserOfficialPumpNumberGroupFooterSection
            // 
            this.dispenserOfficialPumpNumberGroupFooterSection.Height = Telerik.Reporting.Drawing.Unit.Cm(1.1443753242492676D);
            this.dispenserOfficialPumpNumberGroupFooterSection.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.textBox9,
            this.textBox10,
            this.shape2});
            this.dispenserOfficialPumpNumberGroupFooterSection.Name = "dispenserOfficialPumpNumberGroupFooterSection";
            this.dispenserOfficialPumpNumberGroupFooterSection.Style.Visible = true;
            // 
            // textBox1
            // 
            this.textBox1.CanGrow = true;
            this.textBox1.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(3D), Telerik.Reporting.Drawing.Unit.Cm(0.99999988079071045D));
            this.textBox1.Style.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(121)))), ((int)(((byte)(167)))), ((int)(((byte)(227)))));
            this.textBox1.Style.Padding.Left = Telerik.Reporting.Drawing.Unit.Cm(0.20000000298023224D);
            this.textBox1.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Left;
            this.textBox1.StyleName = "Caption";
            this.textBox1.Value = "Αριθ. Αντλίας";
            // 
            // textBox2
            // 
            this.textBox2.CanGrow = true;
            this.textBox2.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(3.1058332920074463D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(12.602499961853027D), Telerik.Reporting.Drawing.Unit.Cm(0.99999988079071045D));
            this.textBox2.Style.Font.Bold = true;
            this.textBox2.Style.Font.Size = Telerik.Reporting.Drawing.Unit.Point(12D);
            this.textBox2.Style.Padding.Left = Telerik.Reporting.Drawing.Unit.Cm(0.30000001192092896D);
            this.textBox2.StyleName = "Data";
            this.textBox2.Value = "=Fields.Dispenser.OfficialPumpNumber";
            // 
            // labelsGroupHeaderSection
            // 
            this.labelsGroupHeaderSection.Height = Telerik.Reporting.Drawing.Unit.Cm(1.1058331727981567D);
            this.labelsGroupHeaderSection.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.officialNozzleNumberCaptionTextBox,
            this.textBox3,
            this.totalCounterCaptionTextBox});
            this.labelsGroupHeaderSection.Name = "labelsGroupHeaderSection";
            this.labelsGroupHeaderSection.PrintOnEveryPage = true;
            // 
            // labelsGroupFooterSection
            // 
            this.labelsGroupFooterSection.Height = Telerik.Reporting.Drawing.Unit.Cm(0.71437495946884155D);
            this.labelsGroupFooterSection.Name = "labelsGroupFooterSection";
            this.labelsGroupFooterSection.Style.Visible = false;
            // 
            // officialNozzleNumberCaptionTextBox
            // 
            this.officialNozzleNumberCaptionTextBox.CanGrow = true;
            this.officialNozzleNumberCaptionTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(1.0529166460037231D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.officialNozzleNumberCaptionTextBox.Name = "officialNozzleNumberCaptionTextBox";
            this.officialNozzleNumberCaptionTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(4.867499828338623D), Telerik.Reporting.Drawing.Unit.Cm(0.99999988079071045D));
            this.officialNozzleNumberCaptionTextBox.Style.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(121)))), ((int)(((byte)(167)))), ((int)(((byte)(227)))));
            this.officialNozzleNumberCaptionTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            this.officialNozzleNumberCaptionTextBox.StyleName = "Caption";
            this.officialNozzleNumberCaptionTextBox.Value = "Αριθ. Ακροσωλήνιου";
            // 
            // textBox3
            // 
            this.textBox3.CanGrow = true;
            this.textBox3.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(5.9733333587646484D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.textBox3.Name = "textBox3";
            this.textBox3.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(4.867499828338623D), Telerik.Reporting.Drawing.Unit.Cm(0.99999988079071045D));
            this.textBox3.Style.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(121)))), ((int)(((byte)(167)))), ((int)(((byte)(227)))));
            this.textBox3.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            this.textBox3.StyleName = "Caption";
            this.textBox3.Value = "Καύσιμο";
            // 
            // totalCounterCaptionTextBox
            // 
            this.totalCounterCaptionTextBox.CanGrow = true;
            this.totalCounterCaptionTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(10.893750190734863D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.totalCounterCaptionTextBox.Name = "totalCounterCaptionTextBox";
            this.totalCounterCaptionTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(4.867499828338623D), Telerik.Reporting.Drawing.Unit.Cm(0.99999988079071045D));
            this.totalCounterCaptionTextBox.Style.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(121)))), ((int)(((byte)(167)))), ((int)(((byte)(227)))));
            this.totalCounterCaptionTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            this.totalCounterCaptionTextBox.StyleName = "Caption";
            this.totalCounterCaptionTextBox.Value = "Συνολικός Όγκος";
            // 
            // pageFooter
            // 
            this.pageFooter.Height = Telerik.Reporting.Drawing.Unit.Cm(2.5000002384185791D);
            this.pageFooter.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.pageInfoTextBox,
            this.textBox5,
            this.pictureBox1,
            this.textBox6,
            this.shape1,
            this.textBox7,
            this.textBox8});
            this.pageFooter.Name = "pageFooter";
            // 
            // reportHeader
            // 
            this.reportHeader.Height = Telerik.Reporting.Drawing.Unit.Cm(1.2000000476837158D);
            this.reportHeader.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.titleTextBox});
            this.reportHeader.Name = "reportHeader";
            // 
            // titleTextBox
            // 
            this.titleTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0D), Telerik.Reporting.Drawing.Unit.Cm(0D));
            this.titleTextBox.Name = "titleTextBox";
            this.titleTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(15.814167022705078D), Telerik.Reporting.Drawing.Unit.Cm(0.89999997615814209D));
            this.titleTextBox.Style.Font.Underline = false;
            this.titleTextBox.Style.Padding.Left = Telerik.Reporting.Drawing.Unit.Cm(0.20000000298023224D);
            this.titleTextBox.Style.VerticalAlign = Telerik.Reporting.Drawing.VerticalAlign.Middle;
            this.titleTextBox.StyleName = "Title";
            this.titleTextBox.Value = "Μετρητές Αντλιών";
            // 
            // reportFooter
            // 
            this.reportFooter.Height = Telerik.Reporting.Drawing.Unit.Cm(1.3000005483627319D);
            this.reportFooter.Name = "reportFooter";
            this.reportFooter.Style.Visible = false;
            // 
            // detail
            // 
            this.detail.Height = Telerik.Reporting.Drawing.Unit.Cm(0.82958316802978516D);
            this.detail.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.officialNozzleNumberDataTextBox,
            this.textBox4,
            this.totalCounterDataTextBox});
            this.detail.Name = "detail";
            // 
            // officialNozzleNumberDataTextBox
            // 
            this.officialNozzleNumberDataTextBox.CanGrow = true;
            this.officialNozzleNumberDataTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(1.0529166460037231D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.officialNozzleNumberDataTextBox.Name = "officialNozzleNumberDataTextBox";
            this.officialNozzleNumberDataTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(4.867499828338623D), Telerik.Reporting.Drawing.Unit.Cm(0.67666614055633545D));
            this.officialNozzleNumberDataTextBox.StyleName = "Data";
            this.officialNozzleNumberDataTextBox.Value = "=Fields.OfficialNozzleNumber";
            // 
            // textBox4
            // 
            this.textBox4.CanGrow = true;
            this.textBox4.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(5.9733333587646484D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.textBox4.Name = "textBox4";
            this.textBox4.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(4.867499828338623D), Telerik.Reporting.Drawing.Unit.Cm(0.67666614055633545D));
            this.textBox4.StyleName = "Data";
            this.textBox4.Value = "=Fields.FuelType.Name";
            // 
            // totalCounterDataTextBox
            // 
            this.totalCounterDataTextBox.CanGrow = true;
            this.totalCounterDataTextBox.Format = "{0:N0}";
            this.totalCounterDataTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(10.893750190734863D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.totalCounterDataTextBox.Name = "totalCounterDataTextBox";
            this.totalCounterDataTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(4.867499828338623D), Telerik.Reporting.Drawing.Unit.Cm(0.67666614055633545D));
            this.totalCounterDataTextBox.Style.Font.Size = Telerik.Reporting.Drawing.Unit.Point(11D);
            this.totalCounterDataTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.totalCounterDataTextBox.StyleName = "Data";
            this.totalCounterDataTextBox.Value = "=Fields.TotalCounter";
            // 
            // pageInfoTextBox
            // 
            this.pageInfoTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(7.2495832443237305D), Telerik.Reporting.Drawing.Unit.Cm(1.2999997138977051D));
            this.pageInfoTextBox.Name = "pageInfoTextBox";
            this.pageInfoTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(8.4587497711181641D), Telerik.Reporting.Drawing.Unit.Cm(0.9493747353553772D));
            this.pageInfoTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.pageInfoTextBox.Style.VerticalAlign = Telerik.Reporting.Drawing.VerticalAlign.Bottom;
            this.pageInfoTextBox.StyleName = "PageInfo";
            this.pageInfoTextBox.Value = "Σελίδα {PageNumber} από {PageCount}";
            // 
            // textBox5
            // 
            this.textBox5.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(1.3493750095367432D), Telerik.Reporting.Drawing.Unit.Cm(1.0883331298828125D));
            this.textBox5.Name = "textBox5";
            this.textBox5.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(4.199798583984375D), Telerik.Reporting.Drawing.Unit.Cm(0.34927469491958618D));
            this.textBox5.Style.Color = System.Drawing.Color.LightGray;
            this.textBox5.Style.Font.Bold = true;
            this.textBox5.Style.Font.Size = Telerik.Reporting.Drawing.Unit.Point(9D);
            this.textBox5.Value = "AS Fuel Control";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D), Telerik.Reporting.Drawing.Unit.Cm(1.0883331298828125D));
            this.pictureBox1.MimeType = "";
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(1.1998999118804932D), Telerik.Reporting.Drawing.Unit.Cm(1.1493750810623169D));
            this.pictureBox1.Sizing = Telerik.Reporting.Drawing.ImageSizeMode.Stretch;
            this.pictureBox1.Value = "";
            // 
            // textBox6
            // 
            this.textBox6.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(1.3493750095367432D), Telerik.Reporting.Drawing.Unit.Cm(1.5381247997283936D));
            this.textBox6.Name = "textBox6";
            this.textBox6.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(5D), Telerik.Reporting.Drawing.Unit.Cm(0.70812535285949707D));
            this.textBox6.Style.VerticalAlign = Telerik.Reporting.Drawing.VerticalAlign.Bottom;
            this.textBox6.Value = "= Now().ToString(\"dd/MM/yyyy HH:mm\")";
            // 
            // textBox7
            // 
            this.textBox7.CanGrow = true;
            this.textBox7.Format = "{0:N0}";
            this.textBox7.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(10.787917137145996D), Telerik.Reporting.Drawing.Unit.Cm(0.30010038614273071D));
            this.textBox7.Name = "textBox7";
            this.textBox7.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(4.867499828338623D), Telerik.Reporting.Drawing.Unit.Cm(0.67666614055633545D));
            this.textBox7.Style.Font.Bold = true;
            this.textBox7.Style.Font.Size = Telerik.Reporting.Drawing.Unit.Point(11D);
            this.textBox7.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.textBox7.StyleName = "Data";
            this.textBox7.Value = "=Sum(Fields.TotalCounter)";
            // 
            // textBox8
            // 
            this.textBox8.CanGrow = true;
            this.textBox8.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0D), Telerik.Reporting.Drawing.Unit.Cm(0.30010038614273071D));
            this.textBox8.Name = "textBox8";
            this.textBox8.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(4.867499828338623D), Telerik.Reporting.Drawing.Unit.Cm(0.67666614055633545D));
            this.textBox8.Style.Font.Bold = true;
            this.textBox8.StyleName = "Data";
            this.textBox8.Value = "Σύνολα";
            // 
            // textBox9
            // 
            this.textBox9.CanGrow = true;
            this.textBox9.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D), Telerik.Reporting.Drawing.Unit.Cm(0.03760913759469986D));
            this.textBox9.Name = "textBox9";
            this.textBox9.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(4.867499828338623D), Telerik.Reporting.Drawing.Unit.Cm(0.67666614055633545D));
            this.textBox9.StyleName = "Data";
            this.textBox9.Value = "Σύνολα Αντλίας";
            // 
            // textBox10
            // 
            this.textBox10.CanGrow = true;
            this.textBox10.Format = "{0:N0}";
            this.textBox10.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(10.893750190734863D), Telerik.Reporting.Drawing.Unit.Cm(0.03760913759469986D));
            this.textBox10.Name = "textBox10";
            this.textBox10.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(4.867499828338623D), Telerik.Reporting.Drawing.Unit.Cm(0.67666614055633545D));
            this.textBox10.Style.Font.Bold = false;
            this.textBox10.Style.Font.Size = Telerik.Reporting.Drawing.Unit.Point(11D);
            this.textBox10.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.textBox10.StyleName = "Data";
            this.textBox10.Value = "=Sum(Fields.TotalCounter)";
            // 
            // shape1
            // 
            this.shape1.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0.00010012308484874666D), Telerik.Reporting.Drawing.Unit.Cm(0.00010012308484874666D));
            this.shape1.Name = "shape1";
            this.shape1.ShapeType = new Telerik.Reporting.Drawing.Shapes.LineShape(Telerik.Reporting.Drawing.Shapes.LineDirection.EW);
            this.shape1.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(15.708232879638672D), Telerik.Reporting.Drawing.Unit.Cm(0.27666592597961426D));
            this.shape1.Style.LineWidth = Telerik.Reporting.Drawing.Unit.Point(3D);
            // 
            // shape2
            // 
            this.shape2.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0D), Telerik.Reporting.Drawing.Unit.Cm(0.71447592973709106D));
            this.shape2.Name = "shape2";
            this.shape2.ShapeType = new Telerik.Reporting.Drawing.Shapes.LineShape(Telerik.Reporting.Drawing.Shapes.LineDirection.EW);
            this.shape2.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(15.708232879638672D), Telerik.Reporting.Drawing.Unit.Cm(0.13229167461395264D));
            this.shape2.Style.LineStyle = Telerik.Reporting.Drawing.LineStyle.Dotted;
            this.shape2.Style.LineWidth = Telerik.Reporting.Drawing.Unit.Point(1D);
            // 
            // NozzleTotalsReport
            // 
            this.DataSource = this.nozzle;
            group1.GroupFooter = this.dispenserOfficialPumpNumberGroupFooterSection;
            group1.GroupHeader = this.dispenserOfficialPumpNumberGroupHeaderSection;
            group1.Groupings.Add(new Telerik.Reporting.Grouping("=Fields.Dispenser.OfficialPumpNumber"));
            group1.Name = "dispenserOfficialPumpNumberGroup";
            group2.GroupFooter = this.labelsGroupFooterSection;
            group2.GroupHeader = this.labelsGroupHeaderSection;
            group2.Name = "labelsGroup";
            this.Groups.AddRange(new Telerik.Reporting.Group[] {
            group1,
            group2});
            this.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.dispenserOfficialPumpNumberGroupHeaderSection,
            this.dispenserOfficialPumpNumberGroupFooterSection,
            this.labelsGroupHeaderSection,
            this.labelsGroupFooterSection,
            this.pageFooter,
            this.reportHeader,
            this.reportFooter,
            this.detail});
            this.Name = "NozzleTotalsReport";
            this.PageSettings.Landscape = false;
            this.PageSettings.Margins = new Telerik.Reporting.Drawing.MarginsU(Telerik.Reporting.Drawing.Unit.Mm(25.399999618530273D), Telerik.Reporting.Drawing.Unit.Mm(15D), Telerik.Reporting.Drawing.Unit.Mm(25.399999618530273D), Telerik.Reporting.Drawing.Unit.Mm(15D));
            this.PageSettings.PaperKind = System.Drawing.Printing.PaperKind.A4;
            this.Sortings.Add(new Telerik.Reporting.Sorting("Fields.Dispenser.OfficialPumpNumber", Telerik.Reporting.SortDirection.Asc));
            this.Sortings.Add(new Telerik.Reporting.Sorting("=Fields.OfficialNozzleNumber", Telerik.Reporting.SortDirection.Asc));
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
            this.Width = Telerik.Reporting.Drawing.Unit.Cm(15.814167022705078D);
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();

        }
        #endregion

        private Telerik.Reporting.ObjectDataSource nozzle;
        private Telerik.Reporting.GroupHeaderSection dispenserOfficialPumpNumberGroupHeaderSection;
        private Telerik.Reporting.TextBox textBox1;
        private Telerik.Reporting.TextBox textBox2;
        private Telerik.Reporting.GroupFooterSection dispenserOfficialPumpNumberGroupFooterSection;
        private Telerik.Reporting.GroupHeaderSection labelsGroupHeaderSection;
        private Telerik.Reporting.TextBox officialNozzleNumberCaptionTextBox;
        private Telerik.Reporting.TextBox textBox3;
        private Telerik.Reporting.TextBox totalCounterCaptionTextBox;
        private Telerik.Reporting.GroupFooterSection labelsGroupFooterSection;
        private Telerik.Reporting.PageFooterSection pageFooter;
        private Telerik.Reporting.ReportHeaderSection reportHeader;
        private Telerik.Reporting.TextBox titleTextBox;
        private Telerik.Reporting.ReportFooterSection reportFooter;
        private Telerik.Reporting.DetailSection detail;
        private Telerik.Reporting.TextBox officialNozzleNumberDataTextBox;
        private Telerik.Reporting.TextBox textBox4;
        private Telerik.Reporting.TextBox totalCounterDataTextBox;
        private Telerik.Reporting.TextBox pageInfoTextBox;
        private Telerik.Reporting.TextBox textBox5;
        private Telerik.Reporting.PictureBox pictureBox1;
        private Telerik.Reporting.TextBox textBox6;
        private Telerik.Reporting.TextBox textBox9;
        private Telerik.Reporting.TextBox textBox10;
        private Telerik.Reporting.TextBox textBox7;
        private Telerik.Reporting.TextBox textBox8;
        private Telerik.Reporting.Shape shape2;
        private Telerik.Reporting.Shape shape1;

    }
}