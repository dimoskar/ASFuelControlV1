namespace ASFuelControl.Reports
{
    partial class ReportHeader
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
            this.companyData = new Telerik.Reporting.ObjectDataSource();
            this.labelsGroupHeaderSection = new Telerik.Reporting.GroupHeaderSection();
            this.labelsGroupFooterSection = new Telerik.Reporting.GroupFooterSection();
            this.reportHeaderSection1 = new Telerik.Reporting.ReportHeaderSection();
            this.companyAddressCaptionTextBox = new Telerik.Reporting.TextBox();
            this.companyAddressDataTextBox = new Telerik.Reporting.TextBox();
            this.companyCityCaptionTextBox = new Telerik.Reporting.TextBox();
            this.companyCityDataTextBox = new Telerik.Reporting.TextBox();
            this.companyNameCaptionTextBox = new Telerik.Reporting.TextBox();
            this.companyNameDataTextBox = new Telerik.Reporting.TextBox();
            this.companyOccupationCaptionTextBox = new Telerik.Reporting.TextBox();
            this.companyOccupationDataTextBox = new Telerik.Reporting.TextBox();
            this.companyPhoneCaptionTextBox = new Telerik.Reporting.TextBox();
            this.companyPhoneDataTextBox = new Telerik.Reporting.TextBox();
            this.companyPostalCodeCaptionTextBox = new Telerik.Reporting.TextBox();
            this.companyPostalCodeDataTextBox = new Telerik.Reporting.TextBox();
            this.companyTaxOfficeCaptionTextBox = new Telerik.Reporting.TextBox();
            this.companyTaxOfficeDataTextBox = new Telerik.Reporting.TextBox();
            this.companyTINCaptionTextBox = new Telerik.Reporting.TextBox();
            this.companyTINDataTextBox = new Telerik.Reporting.TextBox();
            this.reportFooter = new Telerik.Reporting.ReportFooterSection();
            this.detail = new Telerik.Reporting.DetailSection();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            // 
            // companyData
            // 
            this.companyData.DataSource = "ASFuelControl.Data.CompanyData, ASFuelControl.Data, Version=1.0.0.0, Culture=neut" +
    "ral, PublicKeyToken=null";
            this.companyData.Name = "companyData";
            // 
            // labelsGroupHeaderSection
            // 
            this.labelsGroupHeaderSection.Height = Telerik.Reporting.Drawing.Unit.Cm(0.71437495946884155D);
            this.labelsGroupHeaderSection.Name = "labelsGroupHeaderSection";
            this.labelsGroupHeaderSection.PrintOnEveryPage = true;
            this.labelsGroupHeaderSection.Style.Visible = false;
            // 
            // labelsGroupFooterSection
            // 
            this.labelsGroupFooterSection.Height = Telerik.Reporting.Drawing.Unit.Cm(0.71437495946884155D);
            this.labelsGroupFooterSection.Name = "labelsGroupFooterSection";
            this.labelsGroupFooterSection.Style.Visible = false;
            // 
            // reportHeaderSection1
            // 
            this.reportHeaderSection1.Height = Telerik.Reporting.Drawing.Unit.Cm(1.7999999523162842D);
            this.reportHeaderSection1.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.companyAddressCaptionTextBox,
            this.companyAddressDataTextBox,
            this.companyCityCaptionTextBox,
            this.companyCityDataTextBox,
            this.companyNameCaptionTextBox,
            this.companyNameDataTextBox,
            this.companyOccupationCaptionTextBox,
            this.companyOccupationDataTextBox,
            this.companyPhoneCaptionTextBox,
            this.companyPhoneDataTextBox,
            this.companyPostalCodeCaptionTextBox,
            this.companyPostalCodeDataTextBox,
            this.companyTaxOfficeCaptionTextBox,
            this.companyTaxOfficeDataTextBox,
            this.companyTINCaptionTextBox,
            this.companyTINDataTextBox});
            this.reportHeaderSection1.Name = "reportHeaderSection1";
            // 
            // companyAddressCaptionTextBox
            // 
            this.companyAddressCaptionTextBox.CanGrow = true;
            this.companyAddressCaptionTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D), Telerik.Reporting.Drawing.Unit.Cm(0.40477240085601807D));
            this.companyAddressCaptionTextBox.Name = "companyAddressCaptionTextBox";
            this.companyAddressCaptionTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.5470831394195557D), Telerik.Reporting.Drawing.Unit.Cm(0.39416682720184326D));
            this.companyAddressCaptionTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Left;
            this.companyAddressCaptionTextBox.StyleName = "Caption";
            this.companyAddressCaptionTextBox.Value = "Διεύθυνση";
            // 
            // companyAddressDataTextBox
            // 
            this.companyAddressDataTextBox.CanGrow = true;
            this.companyAddressDataTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(2.6002006530761719D), Telerik.Reporting.Drawing.Unit.Cm(0.40000000596046448D));
            this.companyAddressDataTextBox.Name = "companyAddressDataTextBox";
            this.companyAddressDataTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(4.3804254531860352D), Telerik.Reporting.Drawing.Unit.Cm(0.39406669139862061D));
            this.companyAddressDataTextBox.StyleName = "Data";
            this.companyAddressDataTextBox.Value = "=Fields.CompanyAddress";
            // 
            // companyCityCaptionTextBox
            // 
            this.companyCityCaptionTextBox.CanGrow = true;
            this.companyCityCaptionTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D), Telerik.Reporting.Drawing.Unit.Cm(0.79913926124572754D));
            this.companyCityCaptionTextBox.Name = "companyCityCaptionTextBox";
            this.companyCityCaptionTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.5470831394195557D), Telerik.Reporting.Drawing.Unit.Cm(0.39416682720184326D));
            this.companyCityCaptionTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Left;
            this.companyCityCaptionTextBox.StyleName = "Caption";
            this.companyCityCaptionTextBox.Value = "Πόλη";
            // 
            // companyCityDataTextBox
            // 
            this.companyCityDataTextBox.CanGrow = true;
            this.companyCityDataTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(2.6002006530761719D), Telerik.Reporting.Drawing.Unit.Cm(0.800000011920929D));
            this.companyCityDataTextBox.Name = "companyCityDataTextBox";
            this.companyCityDataTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(4.3804254531860352D), Telerik.Reporting.Drawing.Unit.Cm(0.39406669139862061D));
            this.companyCityDataTextBox.StyleName = "Data";
            this.companyCityDataTextBox.Value = "=Fields.CompanyCity";
            // 
            // companyNameCaptionTextBox
            // 
            this.companyNameCaptionTextBox.CanGrow = true;
            this.companyNameCaptionTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D), Telerik.Reporting.Drawing.Unit.Cm(0D));
            this.companyNameCaptionTextBox.Name = "companyNameCaptionTextBox";
            this.companyNameCaptionTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.5470831394195557D), Telerik.Reporting.Drawing.Unit.Cm(0.39416682720184326D));
            this.companyNameCaptionTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Left;
            this.companyNameCaptionTextBox.StyleName = "Caption";
            this.companyNameCaptionTextBox.Value = "Επωνυμία";
            // 
            // companyNameDataTextBox
            // 
            this.companyNameDataTextBox.CanGrow = true;
            this.companyNameDataTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(2.6002001762390137D), Telerik.Reporting.Drawing.Unit.Cm(0D));
            this.companyNameDataTextBox.Name = "companyNameDataTextBox";
            this.companyNameDataTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(4.3804254531860352D), Telerik.Reporting.Drawing.Unit.Cm(0.39406669139862061D));
            this.companyNameDataTextBox.StyleName = "Data";
            this.companyNameDataTextBox.Value = "=Fields.CompanyName";
            // 
            // companyOccupationCaptionTextBox
            // 
            this.companyOccupationCaptionTextBox.CanGrow = true;
            this.companyOccupationCaptionTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(7.9863567352294922D), Telerik.Reporting.Drawing.Unit.Cm(1.1936072111129761D));
            this.companyOccupationCaptionTextBox.Name = "companyOccupationCaptionTextBox";
            this.companyOccupationCaptionTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.5470836162567139D), Telerik.Reporting.Drawing.Unit.Cm(0.39416682720184326D));
            this.companyOccupationCaptionTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Left;
            this.companyOccupationCaptionTextBox.StyleName = "Caption";
            this.companyOccupationCaptionTextBox.Value = "Επάγγελμα";
            // 
            // companyOccupationDataTextBox
            // 
            this.companyOccupationDataTextBox.CanGrow = true;
            this.companyOccupationDataTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(10.533639907836914D), Telerik.Reporting.Drawing.Unit.Cm(1.1936072111129761D));
            this.companyOccupationDataTextBox.Name = "companyOccupationDataTextBox";
            this.companyOccupationDataTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(4.3804254531860352D), Telerik.Reporting.Drawing.Unit.Cm(0.39406669139862061D));
            this.companyOccupationDataTextBox.StyleName = "Data";
            this.companyOccupationDataTextBox.Value = "=Fields.CompanyOccupation";
            // 
            // companyPhoneCaptionTextBox
            // 
            this.companyPhoneCaptionTextBox.CanGrow = true;
            this.companyPhoneCaptionTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(7.9863567352294922D), Telerik.Reporting.Drawing.Unit.Cm(0.010505959391593933D));
            this.companyPhoneCaptionTextBox.Name = "companyPhoneCaptionTextBox";
            this.companyPhoneCaptionTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.5470836162567139D), Telerik.Reporting.Drawing.Unit.Cm(0.39416682720184326D));
            this.companyPhoneCaptionTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Left;
            this.companyPhoneCaptionTextBox.StyleName = "Caption";
            this.companyPhoneCaptionTextBox.Value = "Τηλέφωνο";
            // 
            // companyPhoneDataTextBox
            // 
            this.companyPhoneDataTextBox.CanGrow = true;
            this.companyPhoneDataTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(10.533640861511231D), Telerik.Reporting.Drawing.Unit.Cm(0.010505959391593933D));
            this.companyPhoneDataTextBox.Name = "companyPhoneDataTextBox";
            this.companyPhoneDataTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(4.3804254531860352D), Telerik.Reporting.Drawing.Unit.Cm(0.39406669139862061D));
            this.companyPhoneDataTextBox.StyleName = "Data";
            this.companyPhoneDataTextBox.Value = "=Fields.CompanyPhone";
            // 
            // companyPostalCodeCaptionTextBox
            // 
            this.companyPostalCodeCaptionTextBox.CanGrow = true;
            this.companyPostalCodeCaptionTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0.052916664630174637D), Telerik.Reporting.Drawing.Unit.Cm(1.1935063600540161D));
            this.companyPostalCodeCaptionTextBox.Name = "companyPostalCodeCaptionTextBox";
            this.companyPostalCodeCaptionTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.5470836162567139D), Telerik.Reporting.Drawing.Unit.Cm(0.39416682720184326D));
            this.companyPostalCodeCaptionTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Left;
            this.companyPostalCodeCaptionTextBox.StyleName = "Caption";
            this.companyPostalCodeCaptionTextBox.Value = "Τ.Κ.";
            // 
            // companyPostalCodeDataTextBox
            // 
            this.companyPostalCodeDataTextBox.CanGrow = true;
            this.companyPostalCodeDataTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(2.6002006530761719D), Telerik.Reporting.Drawing.Unit.Cm(1.2000000476837158D));
            this.companyPostalCodeDataTextBox.Name = "companyPostalCodeDataTextBox";
            this.companyPostalCodeDataTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(4.3804254531860352D), Telerik.Reporting.Drawing.Unit.Cm(0.39406669139862061D));
            this.companyPostalCodeDataTextBox.StyleName = "Data";
            this.companyPostalCodeDataTextBox.Value = "=Fields.CompanyPostalCode";
            // 
            // companyTaxOfficeCaptionTextBox
            // 
            this.companyTaxOfficeCaptionTextBox.CanGrow = true;
            this.companyTaxOfficeCaptionTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(7.9863567352294922D), Telerik.Reporting.Drawing.Unit.Cm(0.40487301349639893D));
            this.companyTaxOfficeCaptionTextBox.Name = "companyTaxOfficeCaptionTextBox";
            this.companyTaxOfficeCaptionTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.5470836162567139D), Telerik.Reporting.Drawing.Unit.Cm(0.39416682720184326D));
            this.companyTaxOfficeCaptionTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Left;
            this.companyTaxOfficeCaptionTextBox.StyleName = "Caption";
            this.companyTaxOfficeCaptionTextBox.Value = "Δ.Ο.Υ.";
            // 
            // companyTaxOfficeDataTextBox
            // 
            this.companyTaxOfficeDataTextBox.CanGrow = true;
            this.companyTaxOfficeDataTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(10.533640861511231D), Telerik.Reporting.Drawing.Unit.Cm(0.40487313270568848D));
            this.companyTaxOfficeDataTextBox.Name = "companyTaxOfficeDataTextBox";
            this.companyTaxOfficeDataTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(4.3804254531860352D), Telerik.Reporting.Drawing.Unit.Cm(0.39406669139862061D));
            this.companyTaxOfficeDataTextBox.StyleName = "Data";
            this.companyTaxOfficeDataTextBox.Value = "=Fields.CompanyTaxOffice";
            // 
            // companyTINCaptionTextBox
            // 
            this.companyTINCaptionTextBox.CanGrow = true;
            this.companyTINCaptionTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(7.9863567352294922D), Telerik.Reporting.Drawing.Unit.Cm(0.7992401123046875D));
            this.companyTINCaptionTextBox.Name = "companyTINCaptionTextBox";
            this.companyTINCaptionTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(2.5470836162567139D), Telerik.Reporting.Drawing.Unit.Cm(0.39416682720184326D));
            this.companyTINCaptionTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Left;
            this.companyTINCaptionTextBox.StyleName = "Caption";
            this.companyTINCaptionTextBox.Value = "Α.Φ.Μ.";
            // 
            // companyTINDataTextBox
            // 
            this.companyTINDataTextBox.CanGrow = true;
            this.companyTINDataTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(10.533639907836914D), Telerik.Reporting.Drawing.Unit.Cm(0.79924017190933228D));
            this.companyTINDataTextBox.Name = "companyTINDataTextBox";
            this.companyTINDataTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(4.3804254531860352D), Telerik.Reporting.Drawing.Unit.Cm(0.39406669139862061D));
            this.companyTINDataTextBox.StyleName = "Data";
            this.companyTINDataTextBox.Value = "=Fields.CompanyTIN";
            // 
            // reportFooter
            // 
            this.reportFooter.Height = Telerik.Reporting.Drawing.Unit.Cm(0.71437495946884155D);
            this.reportFooter.Name = "reportFooter";
            this.reportFooter.Style.Visible = false;
            // 
            // detail
            // 
            this.detail.Height = Telerik.Reporting.Drawing.Unit.Cm(0.71437495946884155D);
            this.detail.Name = "detail";
            this.detail.Style.Visible = false;
            // 
            // ReportHeader
            // 
            this.DataSource = this.companyData;
            group1.GroupFooter = this.labelsGroupFooterSection;
            group1.GroupHeader = this.labelsGroupHeaderSection;
            group1.Name = "labelsGroup";
            this.Groups.AddRange(new Telerik.Reporting.Group[] {
            group1});
            this.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.labelsGroupHeaderSection,
            this.labelsGroupFooterSection,
            this.reportHeaderSection1,
            this.reportFooter,
            this.detail});
            this.Name = "ReportHeader";
            this.PageSettings.Margins = new Telerik.Reporting.Drawing.MarginsU(Telerik.Reporting.Drawing.Unit.Mm(25.399999618530273D), Telerik.Reporting.Drawing.Unit.Mm(25.399999618530273D), Telerik.Reporting.Drawing.Unit.Mm(25.399999618530273D), Telerik.Reporting.Drawing.Unit.Mm(25.399999618530273D));
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
            this.Width = Telerik.Reporting.Drawing.Unit.Cm(15.814167022705078D);
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();

        }
        #endregion

        private Telerik.Reporting.ObjectDataSource companyData;
        private Telerik.Reporting.GroupHeaderSection labelsGroupHeaderSection;
        private Telerik.Reporting.GroupFooterSection labelsGroupFooterSection;
        private Telerik.Reporting.ReportHeaderSection reportHeaderSection1;
        private Telerik.Reporting.TextBox companyAddressCaptionTextBox;
        private Telerik.Reporting.TextBox companyAddressDataTextBox;
        private Telerik.Reporting.TextBox companyCityCaptionTextBox;
        private Telerik.Reporting.TextBox companyCityDataTextBox;
        private Telerik.Reporting.TextBox companyNameCaptionTextBox;
        private Telerik.Reporting.TextBox companyNameDataTextBox;
        private Telerik.Reporting.TextBox companyOccupationCaptionTextBox;
        private Telerik.Reporting.TextBox companyOccupationDataTextBox;
        private Telerik.Reporting.TextBox companyPhoneCaptionTextBox;
        private Telerik.Reporting.TextBox companyPhoneDataTextBox;
        private Telerik.Reporting.TextBox companyPostalCodeCaptionTextBox;
        private Telerik.Reporting.TextBox companyPostalCodeDataTextBox;
        private Telerik.Reporting.TextBox companyTaxOfficeCaptionTextBox;
        private Telerik.Reporting.TextBox companyTaxOfficeDataTextBox;
        private Telerik.Reporting.TextBox companyTINCaptionTextBox;
        private Telerik.Reporting.TextBox companyTINDataTextBox;
        private Telerik.Reporting.ReportFooterSection reportFooter;
        private Telerik.Reporting.DetailSection detail;

    }
}