using System.Drawing;
using ASFuelControl.Windows.UI.Forms;
using Exedron.Invoicing.Adapters;
using QRCoder;

internal static partial class Program
{
    private static void MyDieselCameraTests()
    {
        Test("myDiesel camera decoder reads a QR image without retaining it", () =>
        {
            const string payload = "{\"code\":\"AYNY-1022-8625-7956-7356\",\"afm\":\"123456789\",\"validUntil\":1792666800,\"issuedLitres\":150}";
            using (var generator = new QRCodeGenerator())
            using (var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q))
            using (var code = new QRCode(data))
            using (Bitmap image = code.GetGraphic(8))
            {
                string decoded = MyDieselQrImageDecoder.Decode(image);
                Equal(payload, decoded);
                var coupon = MyDieselQrParser.Parse(decoded);
                Equal("AYNY-1022-8625-7956-7356", coupon.Identity.CouponCode);
                Equal("123456789", coupon.Identity.BeneficiaryVat);
            }
        });
        Test("myDiesel camera decoder ignores images without a QR", () =>
        {
            using (var image = new Bitmap(160, 120)) Equal<string>(null, MyDieselQrImageDecoder.Decode(image));
        });
    }
}
