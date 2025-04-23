using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ASFuelControl.Windows.UI.Controls
{
    public partial class TankDataControl : UserControl
    {
        private decimal percentage = 0;
        private decimal temperature = 0;

        public decimal Percentage 
        {
            set 
            { 
                this.percentage = value;
                this.Invalidate();
            }
            get { return this.percentage; }
        }

        public decimal Temperature
        {
            set
            {
                this.temperature = value;
                this.Invalidate();
            }
            get { return this.temperature; }
        }

        public TankDataControl()
        {
            InitializeComponent();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            string str1 = this.percentage.ToString("N2") + "%";
            string str2 = this.percentage.ToString("N2") + "oC";
            SizeF s1 = e.Graphics.MeasureString(str1, new Font(this.Font.FontFamily, 18));
            SizeF s2 = e.Graphics.MeasureString(str2, new Font(this.Font.FontFamily, 14));

            PointF p1 = new PointF(((float)this.Width - s1.Width) / 2, (this.Height / 2) - s1.Height - 2);
            PointF p2 = new PointF(((float)this.Width - s2.Width) / 2, (this.Height / 2) + 1);
            e.Graphics.DrawString(str1, new Font(this.Font.FontFamily, 18), Brushes.White, p1);
            e.Graphics.DrawString(str2, new Font(this.Font.FontFamily, 14), Brushes.White, p2);
        }
    }
}
