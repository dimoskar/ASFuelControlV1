namespace ASFuelControl.Reports.Invoices
{
    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.Windows.Forms;
    using Telerik.Reporting;
    using Telerik.Reporting.Drawing;
    using System.Drawing.Imaging;

    /// <summary>
    /// Summary description for IncoiceReportShort.
    /// </summary>
    public partial class IncoiceReportShort : Telerik.Reporting.Report
    {
        public IncoiceReportShort()
        {
            //
            // Required for telerik Reporting designer support
            //
            InitializeComponent();

            Data.CompanyData company = new Data.CompanyData();

            try
            {
                this.pictureBox1.Value = company.CompanyLogo;// Bitmap.FromFile("StationLogo.png");
                
                PictureWatermark wm = new PictureWatermark();
                int newWidth = (company.CompanyLogo.Width * 150) / 100;
                int newHeight = (company.CompanyLogo.Height * 150) / 100;
                int heightOffset = 200;
                Bitmap img = new Bitmap(newWidth, heightOffset + newHeight);
                using (Graphics gr = Graphics.FromImage(img))
                {
                    gr.DrawImage(company.CompanyLogo, new Rectangle(0, heightOffset, newWidth, newHeight));
                }

                wm.Image = img;//new Bitmap(company.CompanyLogo, (company.CompanyLogo.Width * 150) / 100, (company.CompanyLogo.Height * 150) / 100);
                wm.Position = WatermarkPosition.Behind;
                wm.Opacity = 0.1;
                wm.Sizing = WatermarkSizeMode.Normal;
                this.PageSettings.Watermarks.Add(wm);
                //this.Style.BackgroundImage.Repeat = BackgroundRepeat.Repeat;


                //this.Style.BackgroundImage.ImageData = new Bitmap(company.CompanyLogo, company.CompanyLogo.Width * 2, company.CompanyLogo.Height * 2);
                //this.Style.BackgroundImage.Repeat = BackgroundRepeat.NoRepeat;
                
            }
            catch
            {

            }

            this.companyAddressDataTextBox.Value = company.CompanyAddress;
            this.companyCityDataTextBox.Value = company.CompanyPostalCode + " " + company.CompanyCity;
            this.companyNameDataTextBox.Value = company.CompanyName;
            this.companyOccupationDataTextBox.Value = company.CompanyOccupation;
            this.companyPhoneDataTextBox.Value = "Τηλέφωνο: " + company.CompanyPhone;
            this.companyTINDataTextBox.Value = "ΑΦΜ: " + company.CompanyTIN + " ΔΟΥ: " + company.CompanyTaxOffice;

            Color invoiceColor = company.InvoiceColor;
            if (invoiceColor != Color.Empty)
            {
                ReportItemBase[] allTextBoxes = this.Items.Find(typeof(Telerik.Reporting.TextBox), true);
                foreach (Telerik.Reporting.TextBox textBox in allTextBoxes)
                {
                    if (textBox.Style.BackgroundColor == Color.FromArgb(255, 121, 167, 227))
                        textBox.Style.BackgroundColor = invoiceColor;
                    if(textBox.Style.BorderColor.Default == Color.FromArgb(255, 121, 167, 227))
                        textBox.Style.BorderColor.Default = invoiceColor;
                }
            }

            string pageSetting = Data.Implementation.OptionHandler.Instance.GetOptionOrAdd("InvoicePageSetting", "A4");
            if (pageSetting == "A4")
            {
                this.PageSettings.PaperKind = System.Drawing.Printing.PaperKind.A4;
                this.PageSettings.Landscape = false;
                
            }
            
            //
            // TODO: Add any constructor code after InitializeComponent call
            //
        }
    }

    public static class PaymentFunctions
    {
        private static Data.DatabaseModel database = new Data.DatabaseModel(Data.Implementation.OptionHandler.ConnectionString);

        [Telerik.Reporting.Expressions.Function(Category = "Payment Functions", Description = "Returns Payment Type Text")]
        public static string GetPaymentTypeText(Data.Invoice invoice)
        {
            if (!invoice.PaymentType.HasValue)
                return "";
            switch (invoice.PaymentType.Value)
            {
                case 0:
                    return "Επί Πιστώσει";
                case 1:
                    return "Μετρητά";
                case 2:
                    return "Πιστ. Κάρτα";
                default:
                    return "";
            }
            
        }
    }
}