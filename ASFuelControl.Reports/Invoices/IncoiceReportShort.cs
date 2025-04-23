namespace ASFuelControl.Reports.Invoices
{
    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.Windows.Forms;
    using Telerik.Reporting;
    using Telerik.Reporting.Drawing;
    using System.Drawing.Imaging;
    using QRCoder;
    using static QRCoder.PayloadGenerator;

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
            this.companyEfkTextBox.Value = "ΕΦΚ / Αρ. Άδειας: " + company.CompanyEfk;

            if(company.CompanyBank != null && company.CompanyBank != "" && company.CompanyIBAN != null && company.CompanyIBAN != "")
            {
                txtBankAccount.Value = company.CompanyBank + " (IBAN) : " + company.CompanyIBAN;
            }

            if (company.CompanyBank2 != null && company.CompanyBank2 != "" && company.CompanyIBAN2 != null && company.CompanyIBAN2 != "")
            {
                txtBankAccount2.Value = company.CompanyBank2 + " (IBAN) : " + company.CompanyIBAN2;
            }

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

        public void SetSupplyNumber(Data.Invoice inv, string myDataUrl)
        {
            var str = inv.QRCodeData;

            try
            {
                string brandData = BrandData.SetBrandData(inv);
                if (brandData == null || brandData == "")
                {
                    this.pictureBox4.Visible = false;
                    //this.textBox13.Visible = false;
                }
                else
                {
                    QRCodeGenerator qrGenerator = new QRCodeGenerator();
                    QRCodeData qrCodeData = qrGenerator.CreateQrCode(brandData, QRCodeGenerator.ECCLevel.L);
                    QRCode qrCode = new QRCode(qrCodeData);
                    this.pictureBox4.Sizing = ImageSizeMode.ScaleProportional;
                    var image = qrCode.GetGraphic(10);

                    this.pictureBox4.Value = image;
                }
            }
            catch(Exception ex)
            {

            }

            if ((str == null || str == "") && Data.Implementation.OptionHandler.Instance.GetBoolOption("PrintTestQRCode", false))
                str = "http://147.102.24.100/myweb/q1.php?SIG=CFY9900000100010720416CBF73F32A1E087A3D812F0ED212EBF7B7988F124.00";

            if (str != null && str != "")
            {
                try
                {
                    string code = str;
                    if (code.StartsWith("http"))
                    {
                        Url generator = new Url(code);
                        string payload = generator.ToString();

                        QRCodeGenerator qrGenerator = new QRCodeGenerator();
                        QRCodeData qrCodeData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.L);
                        QRCode qrCode = new QRCode(qrCodeData);
                        this.pictureBox1.Sizing = ImageSizeMode.ScaleProportional;
                        this.pictureBox2.Value = qrCode.GetGraphic(20);
                    }
                }
                catch
                { }
            }
            if (!string.IsNullOrEmpty(myDataUrl))
            {
                try
                {
                    string code = myDataUrl;
                    if (code.StartsWith("http"))
                    {
                        Url generator = new Url(code);
                        string payload = generator.ToString();

                        QRCodeGenerator qrGenerator = new QRCodeGenerator();
                        QRCodeData qrCodeData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.L);
                        QRCode qrCode = new QRCode(qrCodeData);
                        this.pictureBox3.Sizing = ImageSizeMode.ScaleProportional;
                        var image = qrCode.GetGraphic(20);
                        this.pictureBox3.Value = image;
                        this.pictureBox3.Sizing = ImageSizeMode.ScaleProportional;
                    }
                }
                catch
                { }
            }

            if (!string.IsNullOrEmpty(inv.DeliveryAddress))
            {
                textBox61.Visible = true;
                textBox62.Visible = true;
            }
            foreach (var line in inv.InvoiceLines)
            {
                if (line.FuelType == null)
                    continue;
                if (!line.FuelType.SupportsSupplyNumber.HasValue || !line.FuelType.SupportsSupplyNumber.Value)
                    continue;
                textBox57.Visible = true;
                textBox58.Visible = true;
                textBox61.Visible = true;
                textBox62.Visible = true;
                textBox58.Value = inv.SupplyNumber;
                textBox57.Value = "Κωδ. Ηλ. Πληρ. ΔΕΗ";
                textBox57.Style.Font.Size = Unit.Point(7);
                return;
            }
            if(inv.VehicleOdometer == 0)
            {
                textBox57.Visible = false;
                textBox58.Visible = false;
            }
        }

        public void SetRestAmounts(Data.Invoice inv)
        {
            if(inv.Trader == null || !inv.Trader.PrintDebtOnInvoice.HasValue || !inv.Trader.PrintDebtOnInvoice.Value)
            {
                this.textBox52.Visible = false;
                this.textBox53.Visible = false;
                this.textBox54.Visible = false;
                this.textBox55.Visible = false;
                return;
            }
            if(inv.LastRestAmount.HasValue)
                this.textBox54.Value = inv.LastRestAmount.Value.ToString("C", System.Globalization.CultureInfo.CurrentCulture);
            else
                this.textBox54.Value = (0).ToString("C", System.Globalization.CultureInfo.CurrentCulture);
            this.textBox55.Value = inv.NewRestAmount.ToString("C", System.Globalization.CultureInfo.CurrentCulture);
        }
    }

    public static class PaymentFunctions
    {
        private static Data.DatabaseModel database = new Data.DatabaseModel(Data.Implementation.OptionHandler.ConnectionString);

        [Telerik.Reporting.Expressions.Function(Category = "Payment Functions", Description = "Returns Payment Type Text")]
        public static string GetPaymentTypeText(Data.Invoice invoice)
        {
            if (invoice == null)
                return "";
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
                case 3:
                    return "Κάρτα Στόλου";
                default:
                    return "";
            }
            
        }

        [Telerik.Reporting.Expressions.Function(Category = "Payment Functions", Description = "Returns Invoice NettoAmount")]
        public static decimal GetNettoAmount(Data.Invoice invoice)
        {
            if (invoice == null)
                return 0;

            //decimal net = 0;
            //decimal disc = 0;
            //foreach (var line in invoice.InvoiceLines)
            //{
            //    net = net + (line.UnitPrice * line.Volume) / ((100 + line.VatPercentage) / 100);
            //    disc = disc + line.DiscountNettoAmount;
            //}
            //net = decimal.Round(net - disc, 2);
            return invoice.NettoAmount.HasValue ? invoice.NettoAmount.Value : 0;
        }

        [Telerik.Reporting.Expressions.Function(Category = "Payment Functions", Description = "Returns Invoice VatAmount")]
        public static decimal GetVatAmount(Data.Invoice invoice)
        {
            if (invoice == null)
                return 0;
            //decimal vat = 0;
            //foreach (var line in invoice.InvoiceLines)
            //{
            //    vat = vat + line.VatAmount;
            //}
            //vat = decimal.Round(vat, 2);
            if (invoice.VatAmount.HasValue && invoice.VatAmount.Value == 0)
                return 0;
            return invoice.VatAmount.Value;
        }

        [Telerik.Reporting.Expressions.Function(Category = "Payment Functions", Description = "Returns Invoice Discount Amount")]
        public static decimal GetDiscountAmount(Data.Invoice invoice)
        {
            if (invoice == null)
                return 0;

            //decimal disc = 0;
            //foreach (var line in invoice.InvoiceLines)
            //{
            //    disc = disc + line.DiscountAmount;
            //}
            //disc = decimal.Round(disc, 2);
            //return disc;
            return invoice.DiscountAmountWhole;
        }

        [Telerik.Reporting.Expressions.Function(Category = "Payment Functions", Description = "Returns Invoice Total Amount")]
        public static decimal GetTotalAmount(Data.Invoice invoice)
        {
            if (invoice == null)
                return 0;

            //decimal tot = 0;
            //foreach (var line in invoice.InvoiceLines)
            //{
            //    tot = tot + line.TotalPrice;
            //}
            //tot = decimal.Round(tot, 2);
            //return tot;
            return invoice.TotalAmount.HasValue ? invoice.TotalAmount.Value : 0;
        }

        [Telerik.Reporting.Expressions.Function(Category = "Payment Functions", Description = "Returns PreDiscount Total Amount")]
        public static decimal GetPreDiscountNettoAmount(Data.Invoice invoice)
        {
            if (invoice == null)
                return 0;

            //decimal net = 0;
            //foreach (var line in invoice.InvoiceLines)
            //{
            //    net = net + (line.UnitPrice * line.Volume) / ((100 + line.VatPercentage) / 100);
            //}
            //net = decimal.Round(net, 2);
            //return net;
            return invoice.NettoAmount.HasValue ? invoice.NettoAmount.Value : 0;
        }

        [Telerik.Reporting.Expressions.Function(Category = "Payment Functions", Description = "Returns Invoice After Discount NettoAmount")]
        public static decimal GetNettoAfterDiscountAmount(Data.Invoice invoice)
        {
            if (invoice == null)
                return 0;

            //decimal net = 0;
            //decimal disc = 0;
            //foreach (var line in invoice.InvoiceLines)
            //{
            //    net = net + (line.UnitPrice * line.Volume) / ((100 + line.VatPercentage) / 100);
            //    disc = disc + line.DiscountNettoAmount;
            //}
            //net = decimal.Round(net - disc, 2);
            return invoice.NettoAfterDiscount;
            //return invoice.NettoAfterDiscount;
        }
    }
}