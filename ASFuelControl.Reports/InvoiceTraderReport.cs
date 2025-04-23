namespace ASFuelControl.Reports
{
    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.Windows.Forms;
    using Telerik.Reporting;
    using Telerik.Reporting.Drawing;

    /// <summary>
    /// Summary description for InvoiceTraderReport.
    /// </summary>
    public partial class InvoiceTraderReport : Telerik.Reporting.Report
    {
        public InvoiceTraderReport()
        {
            //
            // Required for telerik Reporting designer support
            //
            InitializeComponent();
            this.pictureBox1.Value = Properties.Resources.MainLogo;

            Data.CompanyData company = new Data.CompanyData();

            this.companyAddressDataTextBox.Value = company.CompanyAddress;
            this.companyCityDataTextBox.Value = company.CompanyCity;
            this.companyNameDataTextBox.Value = company.CompanyName;
            this.companyOccupationDataTextBox.Value = company.CompanyOccupation;
            this.companyPhoneDataTextBox.Value = company.CompanyPhone;
            this.companyPostalCodeDataTextBox.Value = company.CompanyPostalCode;
            this.companyTaxOfficeDataTextBox.Value = company.CompanyTaxOffice;
            this.companyTINDataTextBox.Value = company.CompanyTIN;
            this.companyEfkTextBox.Value = company.CompanyEfk;

            //
            // TODO: Add any constructor code after InitializeComponent call
            //
        }
    }
}