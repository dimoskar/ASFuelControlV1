using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ASFuelControl.Windows.UI.Forms
{
    internal sealed class FiscalPrintPreviewForm : Form
    {
        private readonly List<Bitmap> pageImages = new List<Bitmap>();

        internal FiscalPrintPreviewForm(byte[][] pages)
        {
            if (pages == null || pages.Length == 0)
            {
                throw new ArgumentException("Δεν υπάρχουν αποθηκευμένες σελίδες.",
                    "pages");
            }

            Text = "Προεπισκόπηση αποθηκευμένου εγγράφου";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(640,480);
            ClientSize = new Size(900,650);
            ShowIcon = false;
            ShowInTaskbar = false;

            Color viewerBackColor = Color.FromArgb(70,70,70);
            var viewer = new FlowLayoutPanel {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(12),
                BackColor = viewerBackColor
            };
            Controls.Add(viewer);

            for (int pageIndex = 0; pageIndex < pages.Length; pageIndex++)
            {
                Bitmap pageImage = LoadPage(pages[pageIndex]);
                pageImages.Add(pageImage);
                Control page = BuildPage(pageImage,pageIndex + 1,pages.Length);
                viewer.Controls.Add(page);
            }
        }

        private static Bitmap LoadPage(byte[] pageBytes)
        {
            if (pageBytes == null || pageBytes.Length == 0)
            {
                throw new InvalidDataException("Η αποθηκευμένη σελίδα είναι κενή.");
            }
            using (var stream = new MemoryStream(pageBytes,false))
            using (var sourceImage = Image.FromStream(stream))
            {
                return new Bitmap(sourceImage);
            }
        }

        private static Control BuildPage(Bitmap image, int pageNumber,
            int pageCount)
        {
            var pageHost = new TableLayoutPanel {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.White,
                Padding = new Padding(8),
                Margin = new Padding(0,0,12,16)
            };
            pageHost.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pageHost.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            pageHost.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var caption = new Label {
                AutoSize = true,
                Text = "Σελίδα " + pageNumber + " από " + pageCount,
                Font = new Font("Segoe UI",9F,FontStyle.Bold),
                Margin = new Padding(0,0,0,8)
            };
            var picture = new PictureBox {
                AutoSize = true,
                SizeMode = PictureBoxSizeMode.AutoSize,
                Image = image,
                Margin = new Padding(0)
            };
            pageHost.Controls.Add(caption,0,0);
            pageHost.Controls.Add(picture,0,1);
            return pageHost;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (Bitmap pageImage in pageImages)
                {
                    pageImage.Dispose();
                }
                pageImages.Clear();
            }
            base.Dispose(disposing);
        }
    }
}
