namespace ASFuelControl.Reports
{
    partial class ShiftReport
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
            Telerik.Reporting.Drawing.StyleRule styleRule1 = new Telerik.Reporting.Drawing.StyleRule();
            this.shiftReportFuelType1 = new ASFuelControl.Reports.ShiftReportFuelType();
            this.shiftReportDispenser1 = new ASFuelControl.Reports.ShiftReportDispenser();
            this.shiftLiterCheck1 = new ASFuelControl.Reports.ShiftLiterCheck();
            this.pageHeaderSection1 = new Telerik.Reporting.PageHeaderSection();
            this.titleTextBox = new Telerik.Reporting.TextBox();
            this.textBox8 = new Telerik.Reporting.TextBox();
            this.textBox7 = new Telerik.Reporting.TextBox();
            this.textBox6 = new Telerik.Reporting.TextBox();
            this.textBox5 = new Telerik.Reporting.TextBox();
            this.textBox1 = new Telerik.Reporting.TextBox();
            this.detail = new Telerik.Reporting.DetailSection();
            this.subReport1 = new Telerik.Reporting.SubReport();
            this.subReport2 = new Telerik.Reporting.SubReport();
            this.shape1 = new Telerik.Reporting.Shape();
            this.shape2 = new Telerik.Reporting.Shape();
            this.shape3 = new Telerik.Reporting.Shape();
            this.subReport3 = new Telerik.Reporting.SubReport();
            this.pageFooterSection1 = new Telerik.Reporting.PageFooterSection();
            this.pageInfoTextBox = new Telerik.Reporting.TextBox();
            this.textBox4 = new Telerik.Reporting.TextBox();
            this.pictureBox1 = new Telerik.Reporting.PictureBox();
            this.textBox2 = new Telerik.Reporting.TextBox();
            this.Sales = new Telerik.Reporting.OpenAccessDataSource();
            ((System.ComponentModel.ISupportInitialize)(this.shiftReportFuelType1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.shiftReportDispenser1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.shiftLiterCheck1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            // 
            // shiftReportFuelType1
            // 
            this.shiftReportFuelType1.Name = "ShiftReport";
            // 
            // shiftReportDispenser1
            // 
            this.shiftReportDispenser1.Name = "ShiftReportDispenser";
            // 
            // shiftLiterCheck1
            // 
            this.shiftLiterCheck1.Name = "LiterCheckReport";
            // 
            // pageHeaderSection1
            // 
            this.pageHeaderSection1.Height = Telerik.Reporting.Drawing.Unit.Cm(3.4999997615814209D);
            this.pageHeaderSection1.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.titleTextBox,
            this.textBox8,
            this.textBox7,
            this.textBox6,
            this.textBox5,
            this.textBox1});
            this.pageHeaderSection1.Name = "pageHeaderSection1";
            // 
            // titleTextBox
            // 
            this.titleTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0D), Telerik.Reporting.Drawing.Unit.Cm(0.00010002215276472271D));
            this.titleTextBox.Name = "titleTextBox";
            this.titleTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(14.999899864196777D), Telerik.Reporting.Drawing.Unit.Cm(1.196774959564209D));
            this.titleTextBox.Style.Font.Name = "Verdana";
            this.titleTextBox.Style.Font.Size = Telerik.Reporting.Drawing.Unit.Point(18D);
            this.titleTextBox.StyleName = "Title";
            this.titleTextBox.Value = "Αναφορά Βάρδιας";
            // 
            // textBox8
            // 
            this.textBox8.CanGrow = true;
            this.textBox8.Format = "{0:ddd dd/MM/yyyy HH:mm}";
            this.textBox8.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(10.5D), Telerik.Reporting.Drawing.Unit.Cm(2.4998998641967773D));
            this.textBox8.Name = "textBox8";
            this.textBox8.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(4.9999990463256836D), Telerik.Reporting.Drawing.Unit.Cm(0.99999988079071045D));
            this.textBox8.Style.Font.Name = "Verdana";
            this.textBox8.Style.Padding.Left = Telerik.Reporting.Drawing.Unit.Cm(0.20000000298023224D);
            this.textBox8.Style.Padding.Right = Telerik.Reporting.Drawing.Unit.Cm(0.20000000298023224D);
            this.textBox8.Style.VerticalAlign = Telerik.Reporting.Drawing.VerticalAlign.Middle;
            this.textBox8.StyleName = "Data";
            this.textBox8.Value = "=Fields.ShiftEnd";
            // 
            // textBox7
            // 
            this.textBox7.CanGrow = true;
            this.textBox7.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(7.8999996185302734D), Telerik.Reporting.Drawing.Unit.Cm(2.4998998641967773D));
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
            // textBox6
            // 
            this.textBox6.CanGrow = true;
            this.textBox6.Format = "{0:ddd dd/MM/yyyy HH:mm}";
            this.textBox6.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(2.5999999046325684D), Telerik.Reporting.Drawing.Unit.Cm(2.4998998641967773D));
            this.textBox6.Name = "textBox6";
            this.textBox6.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(5.2295875549316406D), Telerik.Reporting.Drawing.Unit.Cm(0.99999988079071045D));
            this.textBox6.Style.Font.Name = "Verdana";
            this.textBox6.Style.Padding.Left = Telerik.Reporting.Drawing.Unit.Cm(0.20000000298023224D);
            this.textBox6.Style.Padding.Right = Telerik.Reporting.Drawing.Unit.Cm(0.20000000298023224D);
            this.textBox6.Style.VerticalAlign = Telerik.Reporting.Drawing.VerticalAlign.Middle;
            this.textBox6.StyleName = "Data";
            this.textBox6.Value = "= Fields.ShiftBegin";
            // 
            // textBox5
            // 
            this.textBox5.CanGrow = true;
            this.textBox5.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0.00010012308484874666D), Telerik.Reporting.Drawing.Unit.Cm(2.4998998641967773D));
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
            // textBox1
            // 
            this.textBox1.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0D), Telerik.Reporting.Drawing.Unit.Cm(1.1970748901367188D));
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(14.999899864196777D), Telerik.Reporting.Drawing.Unit.Cm(0.902925431728363D));
            this.textBox1.Style.Font.Name = "Verdana";
            this.textBox1.Style.Font.Size = Telerik.Reporting.Drawing.Unit.Point(12D);
            this.textBox1.Style.VerticalAlign = Telerik.Reporting.Drawing.VerticalAlign.Middle;
            this.textBox1.StyleName = "Title";
            this.textBox1.Value = "Χειριστής :  {Fields.ApplicationUser.UserName}";
            // 
            // detail
            // 
            this.detail.Height = Telerik.Reporting.Drawing.Unit.Cm(6D);
            this.detail.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.subReport1,
            this.subReport2,
            this.shape1,
            this.shape2,
            this.shape3,
            this.subReport3});
            this.detail.Name = "detail";
            // 
            // subReport1
            // 
            this.subReport1.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0.00010012308484874666D), Telerik.Reporting.Drawing.Unit.Cm(0.40000021457672119D));
            this.subReport1.Name = "subReport1";
            instanceReportSource1.Parameters.Add(new Telerik.Reporting.Parameter("ShiftId", "=Parameters.ShiftId.Value"));
            instanceReportSource1.ReportDocument = this.shiftReportFuelType1;
            this.subReport1.ReportSource = instanceReportSource1;
            this.subReport1.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(14.999799728393555D), Telerik.Reporting.Drawing.Unit.Cm(1.4000000953674316D));
            // 
            // subReport2
            // 
            this.subReport2.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0D), Telerik.Reporting.Drawing.Unit.Cm(2.2999999523162842D));
            this.subReport2.Name = "subReport2";
            instanceReportSource2.Parameters.Add(new Telerik.Reporting.Parameter("ShiftId", "=Parameters.ShiftId.Value"));
            instanceReportSource2.ReportDocument = this.shiftReportDispenser1;
            this.subReport2.ReportSource = instanceReportSource2;
            this.subReport2.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(14.999799728393555D), Telerik.Reporting.Drawing.Unit.Cm(1.5999995470046997D));
            // 
            // shape1
            // 
            this.shape1.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0.00010012308484874666D), Telerik.Reporting.Drawing.Unit.Cm(0.00010012308484874666D));
            this.shape1.Name = "shape1";
            this.shape1.ShapeType = new Telerik.Reporting.Drawing.Shapes.LineShape(Telerik.Reporting.Drawing.Shapes.LineDirection.EW);
            this.shape1.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(15.499899864196777D), Telerik.Reporting.Drawing.Unit.Cm(0.40010073781013489D));
            // 
            // shape2
            // 
            this.shape2.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0D), Telerik.Reporting.Drawing.Unit.Cm(1.8002005815505981D));
            this.shape2.Name = "shape2";
            this.shape2.ShapeType = new Telerik.Reporting.Drawing.Shapes.LineShape(Telerik.Reporting.Drawing.Shapes.LineDirection.EW);
            this.shape2.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(15.5D), Telerik.Reporting.Drawing.Unit.Cm(0.40010073781013489D));
            // 
            // shape3
            // 
            this.shape3.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0.00010012308484874666D), Telerik.Reporting.Drawing.Unit.Cm(3.9001991748809814D));
            this.shape3.Name = "shape3";
            this.shape3.ShapeType = new Telerik.Reporting.Drawing.Shapes.LineShape(Telerik.Reporting.Drawing.Shapes.LineDirection.EW);
            this.shape3.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(15.499899864196777D), Telerik.Reporting.Drawing.Unit.Cm(0.40010073781013489D));
            // 
            // subReport3
            // 
            this.subReport3.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0D), Telerik.Reporting.Drawing.Unit.Cm(4.30049991607666D));
            this.subReport3.Name = "subReport3";
            instanceReportSource3.ReportDocument = this.shiftLiterCheck1;
            this.subReport3.ReportSource = instanceReportSource3;
            this.subReport3.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(14.999899864196777D), Telerik.Reporting.Drawing.Unit.Cm(1.6494745016098023D));
            // 
            // pageFooterSection1
            // 
            this.pageFooterSection1.Height = Telerik.Reporting.Drawing.Unit.Cm(1.3000004291534424D);
            this.pageFooterSection1.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.pageInfoTextBox,
            this.textBox4,
            this.pictureBox1,
            this.textBox2});
            this.pageFooterSection1.Name = "pageFooterSection1";
            this.pageFooterSection1.Style.Visible = true;
            // 
            // pageInfoTextBox
            // 
            this.pageInfoTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(7.9111366271972656D), Telerik.Reporting.Drawing.Unit.Cm(0.20019933581352234D));
            this.pageInfoTextBox.Name = "pageInfoTextBox";
            this.pageInfoTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(7.5888633728027344D), Telerik.Reporting.Drawing.Unit.Cm(0.9493747353553772D));
            this.pageInfoTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.pageInfoTextBox.Style.VerticalAlign = Telerik.Reporting.Drawing.VerticalAlign.Bottom;
            this.pageInfoTextBox.StyleName = "PageInfo";
            this.pageInfoTextBox.Value = "Σελίδα {PageNumber} από {PageCount}";
            // 
            // textBox4
            // 
            this.textBox4.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(1.2965583801269531D), Telerik.Reporting.Drawing.Unit.Cm(0.00019943872757721692D));
            this.textBox4.Name = "textBox4";
            this.textBox4.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(4.199798583984375D), Telerik.Reporting.Drawing.Unit.Cm(0.34927469491958618D));
            this.textBox4.Style.Color = System.Drawing.Color.LightGray;
            this.textBox4.Style.Font.Bold = true;
            this.textBox4.Style.Font.Size = Telerik.Reporting.Drawing.Unit.Point(9D);
            this.textBox4.Value = "AS Fuel Control";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0.00010012308484874666D), Telerik.Reporting.Drawing.Unit.Cm(0.00019943872757721692D));
            this.pictureBox1.MimeType = "";
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(1.1998999118804932D), Telerik.Reporting.Drawing.Unit.Cm(1.1493750810623169D));
            this.pictureBox1.Sizing = Telerik.Reporting.Drawing.ImageSizeMode.Stretch;
            this.pictureBox1.Value = "";
            // 
            // textBox2
            // 
            this.textBox2.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(1.2965583801269531D), Telerik.Reporting.Drawing.Unit.Cm(0.44999110698699951D));
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(5D), Telerik.Reporting.Drawing.Unit.Cm(0.70812535285949707D));
            this.textBox2.Style.VerticalAlign = Telerik.Reporting.Drawing.VerticalAlign.Bottom;
            this.textBox2.Value = "= Now().ToString(\"dd/MM/yyyy HH:mm\")";
            // 
            // Sales
            // 
            this.Sales.ConnectionString = "ASFuelControl.Reports.Properties.Settings.ASFuelControlDB";
            this.Sales.Name = "Sales";
            this.Sales.ObjectContext = "ASFuelControl.Data.DatabaseModel, ASFuelControl.Data, Version=1.0.0.0, Culture=ne" +
    "utral, PublicKeyToken=null";
            this.Sales.ObjectContextMember = "Shifts";
            // 
            // ShiftReport
            // 
            this.DataSource = this.Sales;
            this.Filters.Add(new Telerik.Reporting.Filter("=Fields.ShiftId", Telerik.Reporting.FilterOperator.Equal, "=Parameters.ShiftId.Value"));
            this.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.pageHeaderSection1,
            this.detail,
            this.pageFooterSection1});
            this.Name = "ΣηιφτΡεπορτ";
            this.PageSettings.Margins = new Telerik.Reporting.Drawing.MarginsU(Telerik.Reporting.Drawing.Unit.Mm(25.399999618530273D), Telerik.Reporting.Drawing.Unit.Mm(25.399999618530273D), Telerik.Reporting.Drawing.Unit.Mm(25.399999618530273D), Telerik.Reporting.Drawing.Unit.Mm(25.399999618530273D));
            this.PageSettings.PaperKind = System.Drawing.Printing.PaperKind.A4;
            reportParameter1.Name = "ShiftId";
            reportParameter1.Value = "1377a9d5-2612-462c-bc4d-ea9e420f8d34";
            this.ReportParameters.Add(reportParameter1);
            this.Style.BackgroundColor = System.Drawing.Color.White;
            styleRule1.Selectors.AddRange(new Telerik.Reporting.Drawing.ISelector[] {
            new Telerik.Reporting.Drawing.TypeSelector(typeof(Telerik.Reporting.TextItemBase)),
            new Telerik.Reporting.Drawing.TypeSelector(typeof(Telerik.Reporting.HtmlTextBox))});
            styleRule1.Style.Padding.Left = Telerik.Reporting.Drawing.Unit.Point(2D);
            styleRule1.Style.Padding.Right = Telerik.Reporting.Drawing.Unit.Point(2D);
            this.StyleSheet.AddRange(new Telerik.Reporting.Drawing.StyleRule[] {
            styleRule1});
            this.Width = Telerik.Reporting.Drawing.Unit.Cm(15.5D);
            ((System.ComponentModel.ISupportInitialize)(this.shiftReportFuelType1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.shiftReportDispenser1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.shiftLiterCheck1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();

        }
        #endregion

        private Telerik.Reporting.PageHeaderSection pageHeaderSection1;
        private Telerik.Reporting.DetailSection detail;
        private Telerik.Reporting.PageFooterSection pageFooterSection1;
        private Telerik.Reporting.SubReport subReport1;
        private Telerik.Reporting.SubReport subReport2;
        private Telerik.Reporting.TextBox titleTextBox;
        private ShiftReportFuelType shiftReportFuelType1;
        private ShiftReportDispenser shiftReportDispenser1;
        private Telerik.Reporting.OpenAccessDataSource Sales;
        private Telerik.Reporting.TextBox textBox8;
        private Telerik.Reporting.TextBox textBox7;
        private Telerik.Reporting.TextBox textBox6;
        private Telerik.Reporting.TextBox textBox5;
        private Telerik.Reporting.Shape shape1;
        private Telerik.Reporting.Shape shape2;
        private Telerik.Reporting.TextBox textBox1;
        private Telerik.Reporting.TextBox pageInfoTextBox;
        private Telerik.Reporting.TextBox textBox4;
        private Telerik.Reporting.PictureBox pictureBox1;
        private Telerik.Reporting.TextBox textBox2;
        private Telerik.Reporting.Shape shape3;
        private Telerik.Reporting.SubReport subReport3;
        private ShiftLiterCheck shiftLiterCheck1;
    }
}