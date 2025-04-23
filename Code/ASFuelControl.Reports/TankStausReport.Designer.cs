namespace ASFuelControl.Reports
{
    partial class TankStausReport
    {
        #region Component Designer generated code
        /// <summary>
        /// Required method for telerik Reporting designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            Telerik.Reporting.Group group1 = new Telerik.Reporting.Group();
            Telerik.Reporting.Drawing.StyleRule styleRule1 = new Telerik.Reporting.Drawing.StyleRule();
            Telerik.Reporting.Drawing.StyleRule styleRule2 = new Telerik.Reporting.Drawing.StyleRule();
            Telerik.Reporting.Drawing.StyleRule styleRule3 = new Telerik.Reporting.Drawing.StyleRule();
            Telerik.Reporting.Drawing.StyleRule styleRule4 = new Telerik.Reporting.Drawing.StyleRule();
            this.labelsGroupFooterSection = new Telerik.Reporting.GroupFooterSection();
            this.labelsGroupHeaderSection = new Telerik.Reporting.GroupHeaderSection();
            this.textBox1 = new Telerik.Reporting.TextBox();
            this.fuelLevelCaptionTextBox = new Telerik.Reporting.TextBox();
            this.waterLevelCaptionTextBox = new Telerik.Reporting.TextBox();
            this.temperatireCaptionTextBox = new Telerik.Reporting.TextBox();
            this.textBox3 = new Telerik.Reporting.TextBox();
            this.textBox5 = new Telerik.Reporting.TextBox();
            this.textBox6 = new Telerik.Reporting.TextBox();
            this.textBox9 = new Telerik.Reporting.TextBox();
            this.tankDS = new Telerik.Reporting.ObjectDataSource();
            this.pageFooter = new Telerik.Reporting.PageFooterSection();
            this.pageInfoTextBox = new Telerik.Reporting.TextBox();
            this.textBox20 = new Telerik.Reporting.TextBox();
            this.pictureBox1 = new Telerik.Reporting.PictureBox();
            this.textBox19 = new Telerik.Reporting.TextBox();
            this.reportHeader = new Telerik.Reporting.ReportHeaderSection();
            this.titleTextBox = new Telerik.Reporting.TextBox();
            this.reportFooter = new Telerik.Reporting.ReportFooterSection();
            this.detail = new Telerik.Reporting.DetailSection();
            this.textBox2 = new Telerik.Reporting.TextBox();
            this.fuelLevelDataTextBox = new Telerik.Reporting.TextBox();
            this.waterLevelDataTextBox = new Telerik.Reporting.TextBox();
            this.temperatireDataTextBox = new Telerik.Reporting.TextBox();
            this.textBox4 = new Telerik.Reporting.TextBox();
            this.textBox7 = new Telerik.Reporting.TextBox();
            this.textBox8 = new Telerik.Reporting.TextBox();
            this.textBox10 = new Telerik.Reporting.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            // 
            // labelsGroupFooterSection
            // 
            this.labelsGroupFooterSection.Height = Telerik.Reporting.Drawing.Unit.Cm(0.71437495946884155D);
            this.labelsGroupFooterSection.Name = "labelsGroupFooterSection";
            this.labelsGroupFooterSection.Style.Visible = false;
            // 
            // labelsGroupHeaderSection
            // 
            this.labelsGroupHeaderSection.Height = Telerik.Reporting.Drawing.Unit.Cm(1.1058331727981567D);
            this.labelsGroupHeaderSection.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.textBox1,
            this.fuelLevelCaptionTextBox,
            this.waterLevelCaptionTextBox,
            this.temperatireCaptionTextBox,
            this.textBox3,
            this.textBox5,
            this.textBox6,
            this.textBox9});
            this.labelsGroupHeaderSection.Name = "labelsGroupHeaderSection";
            this.labelsGroupHeaderSection.PrintOnEveryPage = true;
            // 
            // textBox1
            // 
            this.textBox1.CanGrow = true;
            this.textBox1.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(1.4659261703491211D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(4.5000004768371582D), Telerik.Reporting.Drawing.Unit.Cm(0.99999988079071045D));
            this.textBox1.Style.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(121)))), ((int)(((byte)(167)))), ((int)(((byte)(227)))));
            this.textBox1.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            this.textBox1.StyleName = "Caption";
            this.textBox1.Value = "Εϊδος Καύσιμου";
            // 
            // fuelLevelCaptionTextBox
            // 
            this.fuelLevelCaptionTextBox.CanGrow = true;
            this.fuelLevelCaptionTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(10.177247047424316D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.fuelLevelCaptionTextBox.Name = "fuelLevelCaptionTextBox";
            this.fuelLevelCaptionTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.0067710876464844D), Telerik.Reporting.Drawing.Unit.Cm(0.99999988079071045D));
            this.fuelLevelCaptionTextBox.Style.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(121)))), ((int)(((byte)(167)))), ((int)(((byte)(227)))));
            this.fuelLevelCaptionTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            this.fuelLevelCaptionTextBox.StyleName = "Caption";
            this.fuelLevelCaptionTextBox.Value = "Στάθμη";
            // 
            // waterLevelCaptionTextBox
            // 
            this.waterLevelCaptionTextBox.CanGrow = true;
            this.waterLevelCaptionTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(12.249943733215332D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.waterLevelCaptionTextBox.Name = "waterLevelCaptionTextBox";
            this.waterLevelCaptionTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(1.8664581775665283D), Telerik.Reporting.Drawing.Unit.Cm(0.99999988079071045D));
            this.waterLevelCaptionTextBox.Style.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(121)))), ((int)(((byte)(167)))), ((int)(((byte)(227)))));
            this.waterLevelCaptionTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            this.waterLevelCaptionTextBox.StyleName = "Caption";
            this.waterLevelCaptionTextBox.Value = "Στάθμη Νερού";
            // 
            // temperatireCaptionTextBox
            // 
            this.temperatireCaptionTextBox.CanGrow = true;
            this.temperatireCaptionTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(14.182328224182129D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.temperatireCaptionTextBox.Name = "temperatireCaptionTextBox";
            this.temperatireCaptionTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(1.6261450052261353D), Telerik.Reporting.Drawing.Unit.Cm(0.99999988079071045D));
            this.temperatireCaptionTextBox.Style.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(121)))), ((int)(((byte)(167)))), ((int)(((byte)(227)))));
            this.temperatireCaptionTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            this.temperatireCaptionTextBox.StyleName = "Caption";
            this.temperatireCaptionTextBox.Value = "Θερμο-κρασία";
            // 
            // textBox3
            // 
            this.textBox3.CanGrow = true;
            this.textBox3.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.textBox3.Name = "textBox3";
            this.textBox3.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(1.4000002145767212D), Telerik.Reporting.Drawing.Unit.Cm(0.99999988079071045D));
            this.textBox3.Style.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(121)))), ((int)(((byte)(167)))), ((int)(((byte)(227)))));
            this.textBox3.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            this.textBox3.StyleName = "Caption";
            this.textBox3.Value = "Δεξα-μενή";
            // 
            // textBox5
            // 
            this.textBox5.CanGrow = true;
            this.textBox5.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(6.0318527221679688D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.textBox5.Name = "textBox5";
            this.textBox5.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.0067710876464844D), Telerik.Reporting.Drawing.Unit.Cm(0.99999988079071045D));
            this.textBox5.Style.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(121)))), ((int)(((byte)(167)))), ((int)(((byte)(227)))));
            this.textBox5.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            this.textBox5.StyleName = "Caption";
            this.textBox5.Value = "Όγκος Καυσίμου";
            // 
            // textBox6
            // 
            this.textBox6.CanGrow = true;
            this.textBox6.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(8.1045494079589844D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.textBox6.Name = "textBox6";
            this.textBox6.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.0067710876464844D), Telerik.Reporting.Drawing.Unit.Cm(0.99999988079071045D));
            this.textBox6.Style.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(121)))), ((int)(((byte)(167)))), ((int)(((byte)(227)))));
            this.textBox6.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            this.textBox6.StyleName = "Caption";
            this.textBox6.Value = "Όγκος Νερού";
            // 
            // textBox9
            // 
            this.textBox9.CanGrow = true;
            this.textBox9.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(15.874399185180664D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.textBox9.Name = "textBox9";
            this.textBox9.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.1255009174346924D), Telerik.Reporting.Drawing.Unit.Cm(0.99999988079071045D));
            this.textBox9.Style.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(121)))), ((int)(((byte)(167)))), ((int)(((byte)(227)))));
            this.textBox9.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            this.textBox9.StyleName = "Caption";
            this.textBox9.Value = "Όγκος 15oC";
            // 
            // tankDS
            // 
            this.tankDS.DataSource = "ASFuelControl.Data.Tank, ASFuelControl.Data, Version=1.0.0.0, Culture=neutral, Pu" +
    "blicKeyToken=null";
            this.tankDS.Name = "tankDS";
            // 
            // pageFooter
            // 
            this.pageFooter.Height = Telerik.Reporting.Drawing.Unit.Cm(1.2712498903274536D);
            this.pageFooter.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.pageInfoTextBox,
            this.textBox20,
            this.pictureBox1,
            this.textBox19});
            this.pageFooter.Name = "pageFooter";
            // 
            // pageInfoTextBox
            // 
            this.pageInfoTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(10.100000381469727D), Telerik.Reporting.Drawing.Unit.Cm(0.071250088512897491D));
            this.pageInfoTextBox.Name = "pageInfoTextBox";
            this.pageInfoTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(7.8277082443237305D), Telerik.Reporting.Drawing.Unit.Cm(0.99999988079071045D));
            this.pageInfoTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.pageInfoTextBox.Style.VerticalAlign = Telerik.Reporting.Drawing.VerticalAlign.Bottom;
            this.pageInfoTextBox.StyleName = "PageInfo";
            this.pageInfoTextBox.Value = "Σελίδα {PageNumber} από {PageCount}";
            // 
            // textBox20
            // 
            this.textBox20.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(1.2000000476837158D), Telerik.Reporting.Drawing.Unit.Cm(0D));
            this.textBox20.Name = "textBox20";
            this.textBox20.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(4.199798583984375D), Telerik.Reporting.Drawing.Unit.Cm(0.34927469491958618D));
            this.textBox20.Style.Color = System.Drawing.Color.LightGray;
            this.textBox20.Style.Font.Bold = true;
            this.textBox20.Style.Font.Size = Telerik.Reporting.Drawing.Unit.Point(9D);
            this.textBox20.Value = "AS Fuel Control";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0D), Telerik.Reporting.Drawing.Unit.Cm(0D));
            this.pictureBox1.MimeType = "";
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(1.1998999118804932D), Telerik.Reporting.Drawing.Unit.Cm(1.1493750810623169D));
            this.pictureBox1.Sizing = Telerik.Reporting.Drawing.ImageSizeMode.Stretch;
            this.pictureBox1.Value = "";
            // 
            // textBox19
            // 
            this.textBox19.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(1.2000000476837158D), Telerik.Reporting.Drawing.Unit.Cm(0.37125033140182495D));
            this.textBox19.Name = "textBox19";
            this.textBox19.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(5D), Telerik.Reporting.Drawing.Unit.Cm(0.70812535285949707D));
            this.textBox19.Style.VerticalAlign = Telerik.Reporting.Drawing.VerticalAlign.Bottom;
            this.textBox19.Value = "= Now().ToString(\"dd/MM/yyyy HH:mm\")";
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
            this.titleTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0D), Telerik.Reporting.Drawing.Unit.Cm(0.052816543728113174D));
            this.titleTextBox.Name = "titleTextBox";
            this.titleTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(14.399999618530273D), Telerik.Reporting.Drawing.Unit.Cm(0.89999997615814209D));
            this.titleTextBox.StyleName = "Title";
            this.titleTextBox.Value = "Αποθέματα Δεξαμενών";
            // 
            // reportFooter
            // 
            this.reportFooter.Height = Telerik.Reporting.Drawing.Unit.Cm(0.71437495946884155D);
            this.reportFooter.Name = "reportFooter";
            // 
            // detail
            // 
            this.detail.Height = Telerik.Reporting.Drawing.Unit.Cm(0.59416669607162476D);
            this.detail.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.textBox2,
            this.fuelLevelDataTextBox,
            this.waterLevelDataTextBox,
            this.temperatireDataTextBox,
            this.textBox4,
            this.textBox7,
            this.textBox8,
            this.textBox10});
            this.detail.Name = "detail";
            // 
            // textBox2
            // 
            this.textBox2.CanGrow = true;
            this.textBox2.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(1.4659261703491211D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(4.5000004768371582D), Telerik.Reporting.Drawing.Unit.Cm(0.54125005006790161D));
            this.textBox2.StyleName = "Data";
            this.textBox2.Value = "=Fields.FuelType.Name";
            // 
            // fuelLevelDataTextBox
            // 
            this.fuelLevelDataTextBox.CanGrow = true;
            this.fuelLevelDataTextBox.Format = "{0:N2}";
            this.fuelLevelDataTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(10.177247047424316D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.fuelLevelDataTextBox.Name = "fuelLevelDataTextBox";
            this.fuelLevelDataTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.0067710876464844D), Telerik.Reporting.Drawing.Unit.Cm(0.54125005006790161D));
            this.fuelLevelDataTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.fuelLevelDataTextBox.StyleName = "Data";
            this.fuelLevelDataTextBox.Value = "=Fields.FuelLevel";
            // 
            // waterLevelDataTextBox
            // 
            this.waterLevelDataTextBox.CanGrow = true;
            this.waterLevelDataTextBox.Format = "{0:N2}";
            this.waterLevelDataTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(12.249943733215332D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.waterLevelDataTextBox.Name = "waterLevelDataTextBox";
            this.waterLevelDataTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(1.8664581775665283D), Telerik.Reporting.Drawing.Unit.Cm(0.54125005006790161D));
            this.waterLevelDataTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.waterLevelDataTextBox.StyleName = "Data";
            this.waterLevelDataTextBox.Value = "=Fields.WaterLevel";
            // 
            // temperatireDataTextBox
            // 
            this.temperatireDataTextBox.CanGrow = true;
            this.temperatireDataTextBox.Format = "{0:N1}";
            this.temperatireDataTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(14.182328224182129D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.temperatireDataTextBox.Name = "temperatireDataTextBox";
            this.temperatireDataTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(1.6261450052261353D), Telerik.Reporting.Drawing.Unit.Cm(0.54125005006790161D));
            this.temperatireDataTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.temperatireDataTextBox.StyleName = "Data";
            this.temperatireDataTextBox.Value = "=Fields.Temperatire";
            // 
            // textBox4
            // 
            this.textBox4.CanGrow = true;
            this.textBox4.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.textBox4.Name = "textBox4";
            this.textBox4.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(1.4000002145767212D), Telerik.Reporting.Drawing.Unit.Cm(0.54125005006790161D));
            this.textBox4.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            this.textBox4.StyleName = "Data";
            this.textBox4.Value = "=Fields.TankNumber";
            // 
            // textBox7
            // 
            this.textBox7.CanGrow = true;
            this.textBox7.Format = "{0:N2}";
            this.textBox7.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(6.0318527221679688D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.textBox7.Name = "textBox7";
            this.textBox7.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.0067710876464844D), Telerik.Reporting.Drawing.Unit.Cm(0.54125005006790161D));
            this.textBox7.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.textBox7.StyleName = "Data";
            this.textBox7.Value = "= ASFuelControl.Reports.TankReportFunctions.GetVolume(Fields.TankId, Fields.FuelL" +
    "evel)";
            // 
            // textBox8
            // 
            this.textBox8.CanGrow = true;
            this.textBox8.Format = "{0:N2}";
            this.textBox8.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(8.1045494079589844D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.textBox8.Name = "textBox8";
            this.textBox8.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.0067710876464844D), Telerik.Reporting.Drawing.Unit.Cm(0.54125005006790161D));
            this.textBox8.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.textBox8.StyleName = "Data";
            this.textBox8.Value = "= ASFuelControl.Reports.TankReportFunctions.GetVolume(Fields.TankId, Fields.Water" +
    "Level)";
            // 
            // textBox10
            // 
            this.textBox10.CanGrow = true;
            this.textBox10.Format = "{0:N1}";
            this.textBox10.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(15.874399185180664D), Telerik.Reporting.Drawing.Unit.Cm(0.052816543728113174D));
            this.textBox10.Name = "textBox10";
            this.textBox10.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.1255009174346924D), Telerik.Reporting.Drawing.Unit.Cm(0.54125005006790161D));
            this.textBox10.Style.Padding.Right = Telerik.Reporting.Drawing.Unit.Cm(0.10000000149011612D);
            this.textBox10.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.textBox10.StyleName = "Data";
            this.textBox10.Value = "= ASFuelControl.Reports.TankReportFunctions.GetVolumeNormalized(Fields.TankId, Fi" +
    "elds.FuelLevel)";
            // 
            // TankStausReport
            // 
            this.DataSource = this.tankDS;
            group1.GroupFooter = this.labelsGroupFooterSection;
            group1.GroupHeader = this.labelsGroupHeaderSection;
            group1.Name = "labelsGroup";
            this.Groups.AddRange(new Telerik.Reporting.Group[] {
            group1});
            this.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.labelsGroupHeaderSection,
            this.labelsGroupFooterSection,
            this.pageFooter,
            this.reportHeader,
            this.reportFooter,
            this.detail});
            this.Name = "TankStausReport";
            this.PageSettings.Landscape = false;
            this.PageSettings.Margins = new Telerik.Reporting.Drawing.MarginsU(Telerik.Reporting.Drawing.Unit.Mm(15D), Telerik.Reporting.Drawing.Unit.Mm(15D), Telerik.Reporting.Drawing.Unit.Mm(15D), Telerik.Reporting.Drawing.Unit.Mm(15D));
            this.PageSettings.PaperKind = System.Drawing.Printing.PaperKind.A4;
            this.Sortings.Add(new Telerik.Reporting.Sorting("=Fields.TankNumber", Telerik.Reporting.SortDirection.Asc));
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
            this.Width = Telerik.Reporting.Drawing.Unit.Cm(17.999898910522461D);
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();

        }
        #endregion

        private Telerik.Reporting.ObjectDataSource tankDS;
        private Telerik.Reporting.GroupHeaderSection labelsGroupHeaderSection;
        private Telerik.Reporting.TextBox textBox1;
        private Telerik.Reporting.TextBox fuelLevelCaptionTextBox;
        private Telerik.Reporting.TextBox waterLevelCaptionTextBox;
        private Telerik.Reporting.TextBox temperatireCaptionTextBox;
        private Telerik.Reporting.TextBox textBox3;
        private Telerik.Reporting.TextBox textBox5;
        private Telerik.Reporting.TextBox textBox6;
        private Telerik.Reporting.TextBox textBox9;
        private Telerik.Reporting.GroupFooterSection labelsGroupFooterSection;
        private Telerik.Reporting.PageFooterSection pageFooter;
        private Telerik.Reporting.ReportHeaderSection reportHeader;
        private Telerik.Reporting.TextBox titleTextBox;
        private Telerik.Reporting.ReportFooterSection reportFooter;
        private Telerik.Reporting.DetailSection detail;
        private Telerik.Reporting.TextBox textBox2;
        private Telerik.Reporting.TextBox fuelLevelDataTextBox;
        private Telerik.Reporting.TextBox waterLevelDataTextBox;
        private Telerik.Reporting.TextBox temperatireDataTextBox;
        private Telerik.Reporting.TextBox textBox4;
        private Telerik.Reporting.TextBox textBox7;
        private Telerik.Reporting.TextBox textBox8;
        private Telerik.Reporting.TextBox textBox10;
        private Telerik.Reporting.TextBox pageInfoTextBox;
        private Telerik.Reporting.TextBox textBox20;
        private Telerik.Reporting.PictureBox pictureBox1;
        private Telerik.Reporting.TextBox textBox19;

    }
}