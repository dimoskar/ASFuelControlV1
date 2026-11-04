using System.Collections.Generic;
using System.Drawing;
using ZXing;
using ZXing.Common;

namespace ASFuelControl.Windows.UI.Forms
{
    /// <summary>
    /// Decodes only QR symbols. Camera frames are owned by the caller and are
    /// never retained or written to disk.
    /// </summary>
    internal static class MyDieselQrImageDecoder
    {
        internal static string Decode(Bitmap frame)
        {
            if (frame == null) return null;
            var reader = new BarcodeReader
            {
                AutoRotate = true,
                Options = new DecodingOptions
                {
                    TryHarder = true,
                    TryInverted = true,
                    CharacterSet = "UTF-8",
                    PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.QR_CODE }
                }
            };
            var result = reader.Decode(frame);
            return result == null || result.BarcodeFormat != BarcodeFormat.QR_CODE || string.IsNullOrWhiteSpace(result.Text)
                ? null
                : result.Text.Trim();
        }
    }
}
