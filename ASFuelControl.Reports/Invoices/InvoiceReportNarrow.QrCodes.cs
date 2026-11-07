using System;
using System.Collections.Generic;
using Telerik.Reporting;
using Telerik.Reporting.Drawing;

namespace ASFuelControl.Reports.Invoices
{
    public partial class InvoiceReportNarrow
    {
        private const double EmptyQrFooterHeightCm = 4.30D;
        private const double TwoQrFooterHeightCm = 8.05D;
        private const double ThreeQrFooterHeightCm = 11.567706108093262D;

        /// <summary>
        /// Compacts the available QR images into the report slots while keeping
        /// each image together with the caption that describes its source.
        /// </summary>
        public void CompactQrCodes(string fiscalDeviceUrl, string documentUrl)
        {
            object fiscalDeviceImage = pictureBox1.Value;
            object documentImage = pictureBox2.Value;
            object companyImage = pictureBox3.Value;
            var entries = new List<QrCodeEntry>();

            bool duplicateDocumentUrl = SameQrCodeValue(fiscalDeviceUrl,
                documentUrl);
            bool printTestQrCode = Data.Implementation.OptionHandler.Instance
                .GetBoolOption("PrintTestQRCode", false);
            bool includeFiscalDeviceQr = !string.IsNullOrWhiteSpace(fiscalDeviceUrl) ||
                printTestQrCode;
            if (includeFiscalDeviceQr &&
                fiscalDeviceImage != null && !duplicateDocumentUrl)
            {
                var fiscalEntry = new QrCodeEntry
                {
                    Value = fiscalDeviceImage,
                    Caption = "QR CODE \u03A6\u03BF\u03C1\u03BF\u03BB\u03BF\u03B3\u03B9\u03BA\u03AE\u03C2 \u03A3\u03AE\u03BC\u03B1\u03BD\u03C3\u03B7\u03C2"
                };
                entries.Add(fiscalEntry);
            }

            if (!string.IsNullOrWhiteSpace(documentUrl) && documentImage != null)
            {
                var documentEntry = new QrCodeEntry
                {
                    Value = documentImage,
                    Caption = "QR Code \u03A0\u03B1\u03C1\u03B1\u03C3\u03C4\u03B1\u03C4\u03B9\u03BA\u03BF\u03CD"
                };
                entries.Add(documentEntry);
            }

            if (companyImage != null)
            {
                var companyEntry = new QrCodeEntry
                {
                    Value = companyImage,
                    Caption = "QR CODE \u0395\u03C4\u03B1\u03B9\u03C1\u03B5\u03AF\u03B1\u03C2"
                };
                entries.Add(companyEntry);
            }

            QrCodeEntry firstEntry = entries.Count > 0 ? entries[0] : null;
            QrCodeEntry secondEntry = entries.Count > 1 ? entries[1] : null;
            QrCodeEntry thirdEntry = entries.Count > 2 ? entries[2] : null;
            SetQrCodeSlot(pictureBox1, textBox1, firstEntry);
            SetQrCodeSlot(pictureBox2, textBox22, secondEntry);
            SetQrCodeSlot(pictureBox3, textBox13, thirdEntry);
            ApplyQrCodeFooterLayout(entries.Count);
        }

        private static bool SameQrCodeValue(string left, string right)
        {
            if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
            {
                return false;
            }

            string normalizedLeft = left.Trim();
            string normalizedRight = right.Trim();
            return string.Equals(normalizedLeft, normalizedRight,
                StringComparison.OrdinalIgnoreCase);
        }

        private static void SetQrCodeSlot(PictureBox pictureBox, TextBox caption,
            QrCodeEntry entry)
        {
            bool visible = entry != null;
            pictureBox.Visible = visible;
            caption.Visible = visible;
            pictureBox.Value = visible ? entry.Value : null;
            caption.Value = visible ? entry.Caption : string.Empty;
            pictureBox.Sizing = ImageSizeMode.ScaleProportional;
        }

        private void ApplyQrCodeFooterLayout(int qrCodeCount)
        {
            double footerHeightCm;
            double invoiceIdTopCm;
            double bottomLineTopCm;
            if (qrCodeCount <= 0)
            {
                footerHeightCm = EmptyQrFooterHeightCm;
                invoiceIdTopCm = 11.067707061767578D;
                bottomLineTopCm = 4.07D;
            }
            else if (qrCodeCount <= 2)
            {
                footerHeightCm = TwoQrFooterHeightCm;
                invoiceIdTopCm = 7.55D;
                bottomLineTopCm = 7.75D;
            }
            else
            {
                footerHeightCm = ThreeQrFooterHeightCm;
                invoiceIdTopCm = 11.067707061767578D;
                bottomLineTopCm = 11.267507553100586D;
            }

            pageFooterSection1.Height = Unit.Cm(footerHeightCm);
            Unit invoiceIdTop = Unit.Cm(invoiceIdTopCm);
            textBox18.Location = new PointU(textBox18.Location.X, invoiceIdTop);
            Unit bottomLineTop = Unit.Cm(bottomLineTopCm);
            shape3.Location = new PointU(shape3.Location.X, bottomLineTop);
        }

        private sealed class QrCodeEntry
        {
            internal object Value { get; set; }
            internal string Caption { get; set; }
        }
    }
}
