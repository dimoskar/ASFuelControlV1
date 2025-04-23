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
    /// Summary description for InvoiceReport.
    /// </summary>
    public partial class InvoiceReport : Telerik.Reporting.Report
    {
        public InvoiceReport()
        {
            InitializeComponent();

            //if (System.IO.File.Exists("InvoiceLogo.png"))
            //{
            //    this.pictureBox1.Value = Image.FromFile("InvoiceLogo.png");
            //    //this.pictureBox1.Size = new SizeU(new Unit(10.93, UnitType.Cm), new Unit(2.5, UnitType.Cm));
            //    this.pictureBox1.Location = this.panel1.Location;
            //    this.panel1.Visible =false;
            //    this.pictureBox1.Visible = true;
            //}
            //this.pageHeader.Height = this.pageHeader.Height - new Unit(2, UnitType.Cm);
        }

        public void SetSupplyNumber(Data.Invoice inv, string myDataUrl)
        {
            foreach(var line in inv.InvoiceLines)
            {
                if (line.FuelType == null)
                    continue;
                if (!line.FuelType.SupportsSupplyNumber.HasValue || !line.FuelType.SupportsSupplyNumber.Value)
                    continue;
                textBox21.Value = inv.SupplyNumber;
                //textBox20.Value = "Αριθμός Παροχής";
                textBox20.Value = "Κωδ. Ηλ. Πληρ. ΔΕΗ";
                textBox20.Style.Font.Size = Unit.Point(8);
                break;
            }

            try
            {
                string brandData = BrandData.SetBrandData(inv);
                if (brandData == null || brandData == "")
                {
                    this.pictureBox3.Visible = false;
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
            }
            catch
            {

            }
            var str = inv.QRCodeData;

            if ((str == null || str == "") && Data.Implementation.OptionHandler.Instance.GetBoolOption("PrintTestQRCode", false))
                str = str + "," + "http://147.102.24.100/myweb/q1.php?SIG=CFY9900000100010720416CBF73F32A1E087A3D812F0ED212EBF7B7988F124.00";

            if (!(str == null || str == ""))
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
                        var image = qrCode.GetGraphic(20);
                        this.pictureBox1.Value = image;
                        this.pictureBox1.Sizing = ImageSizeMode.ScaleProportional;
                    }
                }
                catch
                { }
            }
            if(!string.IsNullOrEmpty(myDataUrl))
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