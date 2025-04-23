using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace ASFuelControl.Windows.UI.Controls
{
    public partial class TankLinearGauge : UserControl
    {
        float percentageValue = 0;
        GaugeOrientationEnum orientation;

        public GaugeOrientationEnum Orientation
        {
            set 
            { 
                this.orientation = value;
                this.Invalidate();
            }
            get { return this.orientation; }
        }

        public Color BaseColor { set; get; }
        public Color ForeColor
        {
            set
            {
                this.label1.ForeColor = value;
                base.ForeColor = value;
            }
            get
            {
                return this.label1.ForeColor;
            }
        }

        public float PercentageValue
        {
            set
            {
                if (this.percentageValue == value)
                    return;
                this.percentageValue = value;
                this.label1.Text = "";// string.Format("{0:N2}%", this.percentageValue);
            }
            get { return this.percentageValue; }
        }
        public decimal CurrentLevel { set; get; }

        public TankLinearGauge()
        {
            InitializeComponent();
        }

        private void DrawTankGaugeVertical(Color baseColor, Graphics gr)
        {
            Color cLight = ControlPaint.Light(baseColor);
            Color cDark = ControlPaint.Dark(baseColor);
            Color cLightLight = ControlPaint.LightLight(baseColor);
            Color cDarkDark = ControlPaint.DarkDark(baseColor);

            gr.SmoothingMode = SmoothingMode.AntiAlias;
            gr.CompositingQuality = CompositingQuality.HighQuality;

            float h = this.Height;
            float w = this.Width;
            
            float width = w * 0.5f;
            float height = h * 0.85f;
            float diff = h * 0.10f;
            float padding = w * 0.1f;

            float value = this.percentageValue * height / 100;

            Point pTR = new Point((int)(w - width)/2, (int)padding);
            Point pTRS = new Point((int)(w - width) / 2 + 5, (int)padding + 5);

            Point pTRVal = new Point((int)((w - width - diff) / 2), (int)((height - value) + padding - diff / 2));
            Point pTRValS = new Point((int)((w - width - diff) / 2) + 5, (int)((height - value) + padding - diff / 2) + 5);
            
            if (width == 0)
                return;

            LinearGradientBrush brush = new LinearGradientBrush(new Point(0, 0), new Point(this.Width, this.Height), cDarkDark, cLight);
            LinearGradientBrush brush2 = new LinearGradientBrush(new Point(0, 0), new Point(this.Width, this.Height), cLightLight, Color.White);
            LinearGradientBrush shadowBrush = new LinearGradientBrush(new Point(0, 0), new Point(2 * this.Width, 2 * this.Height), Color.FromArgb(50, 0, 0, 0), Color.FromArgb(150, 0, 0, 0));

            gr.FillRectangle(shadowBrush, new Rectangle(pTRS, new System.Drawing.Size((int)width, (int)height)));
            gr.FillRectangle(brush, new Rectangle(pTR, new System.Drawing.Size((int)width, (int)height)));

            gr.FillRectangle(shadowBrush, new Rectangle(pTRValS, new System.Drawing.Size((int)width, (int)(value))));
            gr.FillRectangle(brush2, new Rectangle(pTRVal, new System.Drawing.Size((int)width, (int)(value))));

            float fontSize = 12;
            SizeF size = new SizeF(100, 100);
            while (size.Width > this.Width * 0.9f)
            {
                fontSize = fontSize - 0.1f;
                size = gr.MeasureString("0000", new Font(this.Font.FontFamily, fontSize));
            }

            size = gr.MeasureString(this.CurrentLevel.ToString("N0"), new Font(this.Font.FontFamily, fontSize));
            Point pText = new Point((int)((this.Width - size.Width) / 2), (int)(height + 1.5f * padding));

            gr.DrawString(this.CurrentLevel.ToString("N0"), new Font(this.Font.FontFamily, fontSize), Brushes.White, new Rectangle(pText, new Size(this.Width, (int)(h * 0.15f))));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if(this.orientation == GaugeOrientationEnum.Vertical)
                this.DrawTankGaugeVertical(this.BaseColor, e.Graphics);

        }
    }

    public enum GaugeOrientationEnum
    {
        Horizontal,
        Vertical
    }
}
