namespace ASFuelControl.Reports
{
    partial class InvoiceReport
    {
        #region Component Designer generated code
        /// <summary>
        /// Required method for telerik Reporting designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            Telerik.Reporting.Group group1 = new Telerik.Reporting.Group();
            Telerik.Reporting.ReportParameter reportParameter1 = new Telerik.Reporting.ReportParameter();
            Telerik.Reporting.ReportParameter reportParameter2 = new Telerik.Reporting.ReportParameter();
            Telerik.Reporting.Drawing.StyleRule styleRule1 = new Telerik.Reporting.Drawing.StyleRule();
            Telerik.Reporting.Drawing.StyleRule styleRule2 = new Telerik.Reporting.Drawing.StyleRule();
            Telerik.Reporting.Drawing.StyleRule styleRule3 = new Telerik.Reporting.Drawing.StyleRule();
            Telerik.Reporting.Drawing.StyleRule styleRule4 = new Telerik.Reporting.Drawing.StyleRule();
            this.InvoiceGroupViewDS = new Telerik.Reporting.ObjectDataSource();
            this.labelsGroupHeaderSection = new Telerik.Reporting.GroupHeaderSection();
            this.labelsGroupFooterSection = new Telerik.Reporting.GroupFooterSection();
            this.dateStringCaptionTextBox = new Telerik.Reporting.TextBox();
            this.descriptionCaptionTextBox = new Telerik.Reporting.TextBox();
            this.minNumberCaptionTextBox = new Telerik.Reporting.TextBox();
            this.maxNumberCaptionTextBox = new Telerik.Reporting.TextBox();
            this.nettoAmountCaptionTextBox = new Telerik.Reporting.TextBox();
            this.vatAmountCaptionTextBox = new Telerik.Reporting.TextBox();
            this.totalAmountCaptionTextBox = new Telerik.Reporting.TextBox();
            this.pageFooter = new Telerik.Reporting.PageFooterSection();
            this.pageInfoTextBox = new Telerik.Reporting.TextBox();
            this.reportHeader = new Telerik.Reporting.ReportHeaderSection();
            this.titleTextBox = new Telerik.Reporting.TextBox();
            this.reportFooter = new Telerik.Reporting.ReportFooterSection();
            this.detail = new Telerik.Reporting.DetailSection();
            this.dateStringDataTextBox = new Telerik.Reporting.TextBox();
            this.descriptionDataTextBox = new Telerik.Reporting.TextBox();
            this.minNumberDataTextBox = new Telerik.Reporting.TextBox();
            this.maxNumberDataTextBox = new Telerik.Reporting.TextBox();
            this.nettoAmountDataTextBox = new Telerik.Reporting.TextBox();
            this.vatAmountDataTextBox = new Telerik.Reporting.TextBox();
            this.totalAmountDataTextBox = new Telerik.Reporting.TextBox();
            this.textBox1 = new Telerik.Reporting.TextBox();
            this.textBox2 = new Telerik.Reporting.TextBox();
            this.textBox3 = new Telerik.Reporting.TextBox();
            this.shape1 = new Telerik.Reporting.Shape();
            this.textBox4 = new Telerik.Reporting.TextBox();
            this.pictureBox1 = new Telerik.Reporting.PictureBox();
            this.textBox5 = new Telerik.Reporting.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            // 
            // InvoiceGroupViewDS
            // 
            this.InvoiceGroupViewDS.DataSource = "ASFuelControl.Data.InvoiceGroupView, ASFuelControl.Data, Version=1.0.0.0, Culture" +
    "=neutral, PublicKeyToken=null";
            this.InvoiceGroupViewDS.Name = "InvoiceGroupViewDS";
            // 
            // labelsGroupHeaderSection
            // 
            this.labelsGroupHeaderSection.Height = Telerik.Reporting.Drawing.Unit.Cm(1.1058331727981567D);
            this.labelsGroupHeaderSection.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.dateStringCaptionTextBox,
            this.descriptionCaptionTextBox,
            this.minNumberCaptionTextBox,
            this.maxNumberCaptionTextBox,
            this.nettoAmountCaptionTextBox,
            this.vatAmountCaptionTextBox,
            this.totalAmountCaptionTextBox});
            this.labelsGroupHeaderSection.Name = "labelsGroupHeaderSection";
            this.labelsGroupHeaderSection.PrintOnEveryPage = true;
            // 
            // labelsGroupFooterSection
            // 
            this.labelsGroupFooterSection.Height = Telerik.Reporting.Drawing.Unit.Cm(0.71437495946884155D);
            this.labelsGroupFooterSection.Name = "labelsGroupFooterSection";
            this.labelsGroupFooterSection.Style.Visible = false;
            // 
            // dateStringCaptionTextBox
            // 
            this.dateStringCaptionTextBox.CanGrow = true;
            this.dateStringCaptionTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.dateStringCaptionTextBox.Name = "dateStringCaptionTextBox";
            this.dateStringCaptionTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.1986904144287109D), Telerik.Reporting.Drawing.Unit.Cm(0.99999988079071045D));
            this.dateStringCaptionTextBox.Style.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(121)))), ((int)(((byte)(167)))), ((int)(((byte)(227)))));
            this.dateStringCaptionTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            this.dateStringCaptionTextBox.StyleName = "Caption";
            this.dateStringCaptionTextBox.Value = "Ημερομηνία";
            // 
            // descriptionCaptionTextBox
            // 
            this.descriptionCaptionTextBox.CanGrow = true;
            this.descriptionCaptionTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(2.3045237064361572D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.descriptionCaptionTextBox.Name = "descriptionCaptionTextBox";
            this.descriptionCaptionTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(4.4100570678710938D), Telerik.Reporting.Drawing.Unit.Cm(0.99999988079071045D));
            this.descriptionCaptionTextBox.Style.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(121)))), ((int)(((byte)(167)))), ((int)(((byte)(227)))));
            this.descriptionCaptionTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            this.descriptionCaptionTextBox.StyleName = "Caption";
            this.descriptionCaptionTextBox.Value = "Τύπος Παραστατικού";
            // 
            // minNumberCaptionTextBox
            // 
            this.minNumberCaptionTextBox.CanGrow = true;
            this.minNumberCaptionTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(6.7147812843322754D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.minNumberCaptionTextBox.Name = "minNumberCaptionTextBox";
            this.minNumberCaptionTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.1986904144287109D), Telerik.Reporting.Drawing.Unit.Cm(0.99999988079071045D));
            this.minNumberCaptionTextBox.Style.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(121)))), ((int)(((byte)(167)))), ((int)(((byte)(227)))));
            this.minNumberCaptionTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            this.minNumberCaptionTextBox.StyleName = "Caption";
            this.minNumberCaptionTextBox.Value = "Από";
            // 
            // maxNumberCaptionTextBox
            // 
            this.maxNumberCaptionTextBox.CanGrow = true;
            this.maxNumberCaptionTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(8.9663887023925781D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.maxNumberCaptionTextBox.Name = "maxNumberCaptionTextBox";
            this.maxNumberCaptionTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.1986904144287109D), Telerik.Reporting.Drawing.Unit.Cm(0.99999988079071045D));
            this.maxNumberCaptionTextBox.Style.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(121)))), ((int)(((byte)(167)))), ((int)(((byte)(227)))));
            this.maxNumberCaptionTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            this.maxNumberCaptionTextBox.StyleName = "Caption";
            this.maxNumberCaptionTextBox.Value = "Έως";
            // 
            // nettoAmountCaptionTextBox
            // 
            this.nettoAmountCaptionTextBox.CanGrow = true;
            this.nettoAmountCaptionTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(11.217995643615723D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.nettoAmountCaptionTextBox.Name = "nettoAmountCaptionTextBox";
            this.nettoAmountCaptionTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.1986904144287109D), Telerik.Reporting.Drawing.Unit.Cm(0.99999988079071045D));
            this.nettoAmountCaptionTextBox.Style.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(121)))), ((int)(((byte)(167)))), ((int)(((byte)(227)))));
            this.nettoAmountCaptionTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            this.nettoAmountCaptionTextBox.StyleName = "Caption";
            this.nettoAmountCaptionTextBox.Value = "Καθαρό Ποσό";
            // 
            // vatAmountCaptionTextBox
            // 
            this.vatAmountCaptionTextBox.CanGrow = true;
            this.vatAmountCaptionTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(13.469602584838867D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.vatAmountCaptionTextBox.Name = "vatAmountCaptionTextBox";
            this.vatAmountCaptionTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.1986904144287109D), Telerik.Reporting.Drawing.Unit.Cm(0.99999988079071045D));
            this.vatAmountCaptionTextBox.Style.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(121)))), ((int)(((byte)(167)))), ((int)(((byte)(227)))));
            this.vatAmountCaptionTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            this.vatAmountCaptionTextBox.StyleName = "Caption";
            this.vatAmountCaptionTextBox.Value = "ΦΠΑ";
            // 
            // totalAmountCaptionTextBox
            // 
            this.totalAmountCaptionTextBox.CanGrow = true;
            this.totalAmountCaptionTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(15.721210479736328D), Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D));
            this.totalAmountCaptionTextBox.Name = "totalAmountCaptionTextBox";
            this.totalAmountCaptionTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.1986904144287109D), Telerik.Reporting.Drawing.Unit.Cm(0.99999988079071045D));
            this.totalAmountCaptionTextBox.Style.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(121)))), ((int)(((byte)(167)))), ((int)(((byte)(227)))));
            this.totalAmountCaptionTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            this.totalAmountCaptionTextBox.StyleName = "Caption";
            this.totalAmountCaptionTextBox.Value = "Σύνολο";
            // 
            // pageFooter
            // 
            this.pageFooter.Height = Telerik.Reporting.Drawing.Unit.Cm(1.4712498188018799D);
            this.pageFooter.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.pageInfoTextBox,
            this.textBox4,
            this.pictureBox1,
            this.textBox5});
            this.pageFooter.Name = "pageFooter";
            // 
            // pageInfoTextBox
            // 
            this.pageInfoTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(7.9335417747497559D), Telerik.Reporting.Drawing.Unit.Cm(0.33000043034553528D));
            this.pageInfoTextBox.Name = "pageInfoTextBox";
            this.pageInfoTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(9.9810600280761719D), Telerik.Reporting.Drawing.Unit.Cm(0.99999988079071045D));
            this.pageInfoTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.pageInfoTextBox.Style.VerticalAlign = Telerik.Reporting.Drawing.VerticalAlign.Bottom;
            this.pageInfoTextBox.StyleName = "PageInfo";
            this.pageInfoTextBox.Value = "Σελίδα {PageNumber} από {PageCount}";
            // 
            // reportHeader
            // 
            this.reportHeader.Height = Telerik.Reporting.Drawing.Unit.Cm(1.6999999284744263D);
            this.reportHeader.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.titleTextBox});
            this.reportHeader.Name = "reportHeader";
            // 
            // titleTextBox
            // 
            this.titleTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0D), Telerik.Reporting.Drawing.Unit.Cm(0D));
            this.titleTextBox.Name = "titleTextBox";
            this.titleTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(17.919900894165039D), Telerik.Reporting.Drawing.Unit.Cm(1.6000000238418579D));
            this.titleTextBox.StyleName = "Title";
            this.titleTextBox.Value = "Παραστατικά Περιόδου : {Parameters.DateFrom.Value.ToString(\"dd/MM/yyyy\")} - {Para" +
    "meters.DateTo.Value.ToString(\"dd/MM/yyyy\")}";
            // 
            // reportFooter
            // 
            this.reportFooter.Height = Telerik.Reporting.Drawing.Unit.Cm(0.71437495946884155D);
            this.reportFooter.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.textBox1,
            this.textBox2,
            this.textBox3});
            this.reportFooter.Name = "reportFooter";
            // 
            // detail
            // 
            this.detail.Height = Telerik.Reporting.Drawing.Unit.Cm(0.83541691303253174D);
            this.detail.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.dateStringDataTextBox,
            this.descriptionDataTextBox,
            this.minNumberDataTextBox,
            this.maxNumberDataTextBox,
            this.nettoAmountDataTextBox,
            this.vatAmountDataTextBox,
            this.totalAmountDataTextBox,
            this.shape1});
            this.detail.Name = "detail";
            // 
            // dateStringDataTextBox
            // 
            this.dateStringDataTextBox.CanGrow = true;
            this.dateStringDataTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D), Telerik.Reporting.Drawing.Unit.Cm(0.00010012308484874666D));
            this.dateStringDataTextBox.Name = "dateStringDataTextBox";
            this.dateStringDataTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.1986904144287109D), Telerik.Reporting.Drawing.Unit.Cm(0.48250040411949158D));
            this.dateStringDataTextBox.StyleName = "Data";
            this.dateStringDataTextBox.Value = "=Fields.DateString";
            // 
            // descriptionDataTextBox
            // 
            this.descriptionDataTextBox.CanGrow = true;
            this.descriptionDataTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(2.3045237064361572D), Telerik.Reporting.Drawing.Unit.Cm(0.00010012308484874666D));
            this.descriptionDataTextBox.Name = "descriptionDataTextBox";
            this.descriptionDataTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(4.4100565910339355D), Telerik.Reporting.Drawing.Unit.Cm(0.48250040411949158D));
            this.descriptionDataTextBox.StyleName = "Data";
            this.descriptionDataTextBox.Value = "=Fields.Description";
            // 
            // minNumberDataTextBox
            // 
            this.minNumberDataTextBox.CanGrow = true;
            this.minNumberDataTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(6.7147812843322754D), Telerik.Reporting.Drawing.Unit.Cm(0.00010012308484874666D));
            this.minNumberDataTextBox.Name = "minNumberDataTextBox";
            this.minNumberDataTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.1986904144287109D), Telerik.Reporting.Drawing.Unit.Cm(0.48250040411949158D));
            this.minNumberDataTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            this.minNumberDataTextBox.StyleName = "Data";
            this.minNumberDataTextBox.Value = "=Fields.MinNumber";
            // 
            // maxNumberDataTextBox
            // 
            this.maxNumberDataTextBox.CanGrow = true;
            this.maxNumberDataTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(8.9663887023925781D), Telerik.Reporting.Drawing.Unit.Cm(0.00010012308484874666D));
            this.maxNumberDataTextBox.Name = "maxNumberDataTextBox";
            this.maxNumberDataTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.1986904144287109D), Telerik.Reporting.Drawing.Unit.Cm(0.48250040411949158D));
            this.maxNumberDataTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            this.maxNumberDataTextBox.StyleName = "Data";
            this.maxNumberDataTextBox.Value = "=Fields.MaxNumber";
            // 
            // nettoAmountDataTextBox
            // 
            this.nettoAmountDataTextBox.CanGrow = true;
            this.nettoAmountDataTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(11.217995643615723D), Telerik.Reporting.Drawing.Unit.Cm(0.00010012308484874666D));
            this.nettoAmountDataTextBox.Name = "nettoAmountDataTextBox";
            this.nettoAmountDataTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.1986904144287109D), Telerik.Reporting.Drawing.Unit.Cm(0.48250040411949158D));
            this.nettoAmountDataTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.nettoAmountDataTextBox.StyleName = "Data";
            this.nettoAmountDataTextBox.Value = "=Fields.NettoAmount";
            // 
            // vatAmountDataTextBox
            // 
            this.vatAmountDataTextBox.CanGrow = true;
            this.vatAmountDataTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(13.469602584838867D), Telerik.Reporting.Drawing.Unit.Cm(0.00010012308484874666D));
            this.vatAmountDataTextBox.Name = "vatAmountDataTextBox";
            this.vatAmountDataTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.1986904144287109D), Telerik.Reporting.Drawing.Unit.Cm(0.48250040411949158D));
            this.vatAmountDataTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.vatAmountDataTextBox.StyleName = "Data";
            this.vatAmountDataTextBox.Value = "=Fields.VatAmount";
            // 
            // totalAmountDataTextBox
            // 
            this.totalAmountDataTextBox.CanGrow = true;
            this.totalAmountDataTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(15.721210479736328D), Telerik.Reporting.Drawing.Unit.Cm(0.00010012308484874666D));
            this.totalAmountDataTextBox.Name = "totalAmountDataTextBox";
            this.totalAmountDataTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.1986904144287109D), Telerik.Reporting.Drawing.Unit.Cm(0.48250040411949158D));
            this.totalAmountDataTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.totalAmountDataTextBox.StyleName = "Data";
            this.totalAmountDataTextBox.Value = "=Fields.TotalAmount";
            // 
            // textBox1
            // 
            this.textBox1.CanGrow = true;
            this.textBox1.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(15.715911865234375D), Telerik.Reporting.Drawing.Unit.Cm(0.00010012308484874666D));
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.1986904144287109D), Telerik.Reporting.Drawing.Unit.Cm(0.48250040411949158D));
            this.textBox1.Style.Font.Bold = true;
            this.textBox1.Style.Font.Size = Telerik.Reporting.Drawing.Unit.Point(10D);
            this.textBox1.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.textBox1.StyleName = "Data";
            this.textBox1.Value = "=Sum(Fields.TotalAmount)";
            // 
            // textBox2
            // 
            this.textBox2.CanGrow = true;
            this.textBox2.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(13.466954231262207D), Telerik.Reporting.Drawing.Unit.Cm(0.00010012308484874666D));
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.1986904144287109D), Telerik.Reporting.Drawing.Unit.Cm(0.48250040411949158D));
            this.textBox2.Style.Font.Bold = true;
            this.textBox2.Style.Font.Size = Telerik.Reporting.Drawing.Unit.Point(10D);
            this.textBox2.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.textBox2.StyleName = "Data";
            this.textBox2.Value = "=Sum(Fields.VatAmount)";
            // 
            // textBox3
            // 
            this.textBox3.CanGrow = true;
            this.textBox3.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(11.217995643615723D), Telerik.Reporting.Drawing.Unit.Cm(0.00010012308484874666D));
            this.textBox3.Name = "textBox3";
            this.textBox3.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.1986904144287109D), Telerik.Reporting.Drawing.Unit.Cm(0.48250040411949158D));
            this.textBox3.Style.Font.Bold = true;
            this.textBox3.Style.Font.Size = Telerik.Reporting.Drawing.Unit.Point(10D);
            this.textBox3.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            this.textBox3.StyleName = "Data";
            this.textBox3.Value = "=Sum(Fields.NettoAmount)";
            // 
            // shape1
            // 
            this.shape1.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0D), Telerik.Reporting.Drawing.Unit.Cm(0.63541704416275024D));
            this.shape1.Name = "shape1";
            this.shape1.ShapeType = new Telerik.Reporting.Drawing.Shapes.LineShape(Telerik.Reporting.Drawing.Shapes.LineDirection.EW);
            this.shape1.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(17.914602279663086D), Telerik.Reporting.Drawing.Unit.Cm(0.19999989867210388D));
            // 
            // textBox4
            // 
            this.textBox4.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(1.2999999523162842D), Telerik.Reporting.Drawing.Unit.Cm(0.12999972701072693D));
            this.textBox4.Name = "textBox4";
            this.textBox4.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(4.199798583984375D), Telerik.Reporting.Drawing.Unit.Cm(0.39989984035491943D));
            this.textBox4.Style.Color = System.Drawing.Color.LightGray;
            this.textBox4.Style.Font.Bold = true;
            this.textBox4.Style.Font.Size = Telerik.Reporting.Drawing.Unit.Point(9D);
            this.textBox4.Value = "AS Fuel Control";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0.0093750329688191414D), Telerik.Reporting.Drawing.Unit.Cm(0.12999972701072693D));
            this.pictureBox1.MimeType = "";
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(1.1998999118804932D), Telerik.Reporting.Drawing.Unit.Cm(1.2000001668930054D));
            this.pictureBox1.Sizing = Telerik.Reporting.Drawing.ImageSizeMode.Stretch;
            this.pictureBox1.Value = "";
            // 
            // textBox5
            // 
            this.textBox5.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(1.2999999523162842D), Telerik.Reporting.Drawing.Unit.Cm(0.57124984264373779D));
            this.textBox5.Name = "textBox5";
            this.textBox5.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(5D), Telerik.Reporting.Drawing.Unit.Cm(0.75875049829483032D));
            this.textBox5.Style.VerticalAlign = Telerik.Reporting.Drawing.VerticalAlign.Bottom;
            this.textBox5.Value = "= Now().ToString(\"dd/MM/yyyy HH:mm\")";
            // 
            // InvoiceReport
            // 
            this.DataSource = this.InvoiceGroupViewDS;
            this.Filters.Add(new Telerik.Reporting.Filter("=Fields.TransactionDate", Telerik.Reporting.FilterOperator.GreaterOrEqual, "=Parameters.DateFrom.Value"));
            this.Filters.Add(new Telerik.Reporting.Filter("=Fields.TransactionDate", Telerik.Reporting.FilterOperator.LessOrEqual, "=Parameters.DateTo.Value"));
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
            this.Name = "InvoiceReport";
            this.PageSettings.Margins = new Telerik.Reporting.Drawing.MarginsU(Telerik.Reporting.Drawing.Unit.Cm(1.5399999618530273D), Telerik.Reporting.Drawing.Unit.Cm(1.5399999618530273D), Telerik.Reporting.Drawing.Unit.Cm(1.5399999618530273D), Telerik.Reporting.Drawing.Unit.Cm(1.5399999618530273D));
            this.PageSettings.PaperKind = System.Drawing.Printing.PaperKind.A4;
            reportParameter1.Name = "DateFrom";
            reportParameter1.Text = "Από";
            reportParameter1.Type = Telerik.Reporting.ReportParameterType.DateTime;
            reportParameter1.Value = "=2014/01/01";
            reportParameter2.Name = "DateTo";
            reportParameter2.Text = "Εώς";
            reportParameter2.Type = Telerik.Reporting.ReportParameterType.DateTime;
            reportParameter2.Value = "2015/01/01";
            this.ReportParameters.Add(reportParameter1);
            this.ReportParameters.Add(reportParameter2);
            this.Sortings.Add(new Telerik.Reporting.Sorting("=Fields.TransactionDate", Telerik.Reporting.SortDirection.Asc));
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
            this.Width = Telerik.Reporting.Drawing.Unit.Cm(17.920000076293945D);
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();

        }
        #endregion

        private Telerik.Reporting.ObjectDataSource InvoiceGroupViewDS;
        private Telerik.Reporting.GroupHeaderSection labelsGroupHeaderSection;
        private Telerik.Reporting.TextBox dateStringCaptionTextBox;
        private Telerik.Reporting.TextBox descriptionCaptionTextBox;
        private Telerik.Reporting.TextBox minNumberCaptionTextBox;
        private Telerik.Reporting.TextBox maxNumberCaptionTextBox;
        private Telerik.Reporting.TextBox nettoAmountCaptionTextBox;
        private Telerik.Reporting.TextBox vatAmountCaptionTextBox;
        private Telerik.Reporting.TextBox totalAmountCaptionTextBox;
        private Telerik.Reporting.GroupFooterSection labelsGroupFooterSection;
        private Telerik.Reporting.PageFooterSection pageFooter;
        private Telerik.Reporting.TextBox pageInfoTextBox;
        private Telerik.Reporting.ReportHeaderSection reportHeader;
        private Telerik.Reporting.TextBox titleTextBox;
        private Telerik.Reporting.ReportFooterSection reportFooter;
        private Telerik.Reporting.DetailSection detail;
        private Telerik.Reporting.TextBox dateStringDataTextBox;
        private Telerik.Reporting.TextBox descriptionDataTextBox;
        private Telerik.Reporting.TextBox minNumberDataTextBox;
        private Telerik.Reporting.TextBox maxNumberDataTextBox;
        private Telerik.Reporting.TextBox nettoAmountDataTextBox;
        private Telerik.Reporting.TextBox vatAmountDataTextBox;
        private Telerik.Reporting.TextBox totalAmountDataTextBox;
        private Telerik.Reporting.TextBox textBox1;
        private Telerik.Reporting.TextBox textBox2;
        private Telerik.Reporting.TextBox textBox3;
        private Telerik.Reporting.Shape shape1;
        private Telerik.Reporting.TextBox textBox4;
        private Telerik.Reporting.PictureBox pictureBox1;
        private Telerik.Reporting.TextBox textBox5;

    }
}