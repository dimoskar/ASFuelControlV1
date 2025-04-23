namespace ASFuelControl.Reports.Invoices
{
    using QRCoder;
    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.Windows.Forms;
    using Telerik.Reporting;
    using Telerik.Reporting.Drawing;
    using static QRCoder.PayloadGenerator;

    /// <summary>
    /// Summary description for InvoiceReportNarrow.
    /// </summary>
    public partial class InvoiceReportNarrow : Telerik.Reporting.Report
    {
        public InvoiceReportNarrow()
        {
            //
            // Required for telerik Reporting designer support
            //
            InitializeComponent();

            //
            // TODO: Add any constructor code after InitializeComponent call
            //
        }

        public void SetFont(string fontName, float fSize)
        {
            if (fSize < 4)
                return;
            float defaultFontSize = 9f;
            var change = fSize / defaultFontSize;
            foreach (var item in this.pageFooterSection1.Items)
            {
                item.Style.Font.Name = fontName;
                var prevSize = item.Style.Font.Size.Value;
                item.Style.Font.Size = new Unit(prevSize * change, item.Style.Font.Size.Type);
            }
            foreach (var item in this.pageHeaderSection1.Items)
            {
                item.Style.Font.Name = fontName;
                var prevSize = item.Style.Font.Size.Value;
                item.Style.Font.Size = new Unit(prevSize * change, item.Style.Font.Size.Type);
            }
            foreach (var item in this.detail.Items)
            {
                item.Style.Font.Name = fontName;
                var prevSize = item.Style.Font.Size.Value;
                item.Style.Font.Size = new Unit(prevSize * change, item.Style.Font.Size.Type);
            }
            foreach (var item in this.groupHeaderSection1.Items)
            {
                item.Style.Font.Name = fontName;
                var prevSize = item.Style.Font.Size.Value;
                item.Style.Font.Size = new Unit(prevSize * change, item.Style.Font.Size.Type);
            }
            foreach (var item in this.panel1.Items)
            {
                item.Style.Font.Name = fontName;
                var prevSize = item.Style.Font.Size.Value;
                item.Style.Font.Size = new Unit(prevSize * change, item.Style.Font.Size.Type);
            }
            foreach (var item in this.panel2.Items)
            {
                item.Style.Font.Name = fontName;
                var prevSize = item.Style.Font.Size.Value;
                item.Style.Font.Size = new Unit(prevSize * change, item.Style.Font.Size.Type);
            }
        }

        public void SetSupplyNumber(Data.Invoice inv, string myDataUrl)
        {
            this.textBoxPreDiscount.Value = inv.NettoAmount.Value.ToString("N2");
            this.textBoxDiscount.Value = inv.DiscountAmount.ToString("N2");
            this.textBox15.Value = inv.VatAmount.HasValue ? inv.VatAmount.Value.ToString("N2") : (0).ToString("N2");
            this.textBox6.Value = inv.TotalAmount.HasValue ? inv.TotalAmount.Value.ToString("N2") : (0).ToString("N2");
            if (inv.InvoiceLines != null && inv.InvoiceLines.Count > 0)
                this.textBoxVATPercentage.Value = inv.InvoiceLines[0].VatPercentage.ToString("N1");

            int pageHeight = Data.Implementation.OptionHandler.Instance.GetIntOption("ThermalPrintHeight", 150);

            if (inv.Trader != null)
            {
                this.textBoxTraderAfm.Value = "ÁÖÌ: " + inv.Trader.TaxRegistrationNumber;
                this.textBoxTraderDoy.Value = "ÄÏÕ: " + inv.Trader.TaxRegistrationOffice;
                this.textBoxTraderNameData.Value = "ÐåëÜôçò: " + inv.Trader.Name;
                this.textBoxVehicleNumberData.Value = "¼÷çìá: " + inv.VehiclePlateNumber;
                this.pageHeaderSection1.Height = new Unit(5.3, UnitType.Cm);
                this.PageSettings.PaperSize = new SizeU(new Unit(80, UnitType.Mm), new Unit(pageHeight, UnitType.Mm));
            }
            else
            {
                this.textBoxTraderAfm.Value = "";
                this.textBoxTraderDoy.Value = "";
                this.textBoxTraderNameData.Value = "";
                this.textBoxVehicleNumberData.Value = "";
                this.shape4.Visible = false;
                this.pageHeaderSection1.Height = new Unit(3.9, UnitType.Cm);
                this.PageSettings.PaperSize = new SizeU(new Unit(80, UnitType.Mm), new Unit(pageHeight - 10, UnitType.Mm));
            }

            string brandData = BrandData.SetBrandData(inv);
            if(brandData == null || brandData == "")
            {
                this.pictureBox3.Visible = false;
                this.textBox13.Visible = false;
            }
            else
            {
                QRCodeGenerator qrGenerator = new QRCodeGenerator();
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(brandData, QRCodeGenerator.ECCLevel.L);
                QRCode qrCode = new QRCode(qrCodeData);
                this.pictureBox3.Sizing = ImageSizeMode.ScaleProportional;
                var image = qrCode.GetGraphic(10);
                
                this.pictureBox3.Value = image;
            }

            if (inv.Trader != null && inv.Trader.VatExemption.HasValue && inv.Trader.VatExemption.Value)
            {
                decimal vat = Data.Implementation.OptionHandler.Instance.GetDecimalOption("VATValue", 24);
                foreach (var inl in inv.InvoiceLines)
                {
                    inl.UnitPrice = inl.UnitPrice / ((100 + vat) / 100);
                }
            }
            foreach (var line in inv.InvoiceLines)
            {
                if (line.FuelType == null)
                    continue;
                if (!line.FuelType.SupportsSupplyNumber.HasValue || !line.FuelType.SupportsSupplyNumber.Value)
                    continue;
                this.textBoxVehicleNumberData.Value = "Êùä. Çë. Ðëçñ. ÄÅÇ:" + inv.SupplyNumber;
                break;
            }

            var str = inv.QRCodeData;

            if ((str == null || str == "") && Data.Implementation.OptionHandler.Instance.GetBoolOption("PrintTestQRCode", false))
                str = "http://147.102.24.100/myweb/q1.php?SIG=CFY9900000100010720416CBF73F32A1E087A3D812F0ED212EBF7B7988F124.00";

            if (str == null || str == "")
            {
                int currentHeight = (int)this.PageSettings.PaperSize.Height.Value;
                this.PageSettings.PaperSize = new SizeU(new Unit(80, UnitType.Mm), new Unit(currentHeight - 20, UnitType.Mm));
                pageFooterSection1.Height = new Unit(4.30, UnitType.Cm);
                this.pictureBox1.Visible = false;
                this.textBox1.Visible = false;
                if (brandData == null || brandData == "")
                    this.shape3.Location = new PointU(new Unit(0.04, UnitType.Cm), new Unit(4.07, UnitType.Cm));

            }
            else
            {
                try
                {
                    string code = str;

                    Url generator = new Url(code);
                    string payload = generator.ToString();

                    QRCodeGenerator qrGenerator = new QRCodeGenerator();
                    QRCodeData qrCodeData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.L);
                    QRCode qrCode = new QRCode(qrCodeData);
                    this.pictureBox1.Sizing = ImageSizeMode.ScaleProportional;
                    var image = qrCode.GetGraphic(10);

                    this.pictureBox1.Value = image;
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
                        this.pictureBox2.Sizing = ImageSizeMode.ScaleProportional;
                        var image = qrCode.GetGraphic(20);
                        this.pictureBox2.Value = image;
                        this.pictureBox2.Sizing = ImageSizeMode.ScaleProportional;
                    }
                }
                catch
                { }
            }
        }
    }
}