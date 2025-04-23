using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Telerik.WinControls.UI;

namespace ASFuelControl.Windows.UI.SelectionForms
{
    public partial class SelectInvoiceColorForm : RadForm
    {
        Dictionary<Color, int> colors = new Dictionary<Color, int>();

        public Image LogoImage { set; get; }

        public Color SelectedColor { private set; get; }

        public SelectInvoiceColorForm()
        {
            InitializeComponent();
            this.Load += new EventHandler(SelectInvoiceColorForm_Load);
        }

        void SelectInvoiceColorForm_Load(object sender, EventArgs e)
        {
            this.GetColors();
            List<Color> cols = this.colors.OrderByDescending(c => c.Value).Select(c => c.Key).Take(8).ToList();
            foreach (Color color in cols)
            {
                ListViewDataItem item = new ListViewDataItem();
                Image img = new Bitmap(100, 100);
                using (Graphics gfx = Graphics.FromImage(img))
                {
                    using (SolidBrush brush = new SolidBrush(color))
                    {
                        gfx.FillRectangle(brush, 0, 0, img.Width, img.Height);
                    }
                }
                item.Image = img;
                item.ImageAlignment = ContentAlignment.MiddleCenter;
                item.Tag = color;
                this.radListView1.Items.Add(item);
            }
        }

        private void GetColors()
        {
            if (this.LogoImage == null)
                return;
            for (int i = 0; i < LogoImage.Width; i++)
            {
                for (int x = 0; x < LogoImage.Height; x++)
                {
                    //Get the Pixel
                    Color bitmapColor = ((Bitmap)LogoImage).GetPixel(i, x);
                    if (this.colors.ContainsKey(bitmapColor))
                        this.colors[bitmapColor] = this.colors[bitmapColor] + 1;
                    else
                        colors.Add(bitmapColor, 1);
                }
            }
            List<Color> toRemove = new List<Color>();
            List<Color> cols = this.colors.OrderByDescending(c => c.Value).Where(c=>c.Value > 20).Select(c => c.Key).ToList();
            for (int i=0; i < cols.Count;i++)
            {
                Color c = cols[i];
                for (int j = i + 1; j < cols.Count; j++)
                {
                    Color cm = cols[j];
                    if (toRemove.Contains(cm))
                        continue;
                    double dbl_test_red = Math.Pow(Convert.ToDouble(((Color)c).R) - Convert.ToDouble(((Color)cm).R), 2.0);
                    double dbl_test_green = Math.Pow(Convert.ToDouble(((Color)c).G) - Convert.ToDouble(((Color)cm).G), 2.0);
                    double dbl_test_blue = Math.Pow(Convert.ToDouble(((Color)c).B) - Convert.ToDouble(((Color)cm).B), 2.0);

                    double temp = Math.Sqrt(dbl_test_blue + dbl_test_green + dbl_test_red);
                    if (temp < 50)
                        toRemove.Add(cm);
                }
            }
            foreach (Color cm in toRemove)
                this.colors.Remove(cm);
        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            if (this.radListView1.SelectedItem == null)
                this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            else if (this.radListView1.SelectedItem.Tag == null)
                this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            else
            {
                this.SelectedColor = (Color)this.radListView1.SelectedItem.Tag;
                this.DialogResult = System.Windows.Forms.DialogResult.OK;
            }
        }

        private void radButton2_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        }
    }
}
