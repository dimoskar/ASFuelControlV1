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
    public partial class TankGauge : UserControl
    {
        float percentageValue = 0;
        private decimal temperature = 0;

        public Color StateColor { set; get; }
        public Color BaseColor { set; get; }
        public Color ForeColor 
        {
            set 
            { 
                base.ForeColor = value;
            }
            get 
            {
                return base.ForeColor;
            }
        }

        public float PercentageValue 
        {
            set
            {
                if (this.percentageValue == value)
                    return;
                this.percentageValue = value;
                this.Invalidate();
                //this.label1.Text = string.Format("{0:N2}%", this.percentageValue);
            }
            get { return this.percentageValue; }
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

        public TankGauge()
        {
            InitializeComponent();
        }

        private void DrawTankGauge(Color baseColor, Graphics gr)
        {
            Color cLight = ControlPaint.Light(baseColor);
            Color cDark = ControlPaint.Dark(baseColor);
            Color cLightLight = ControlPaint.LightLight(baseColor);
            Color cDarkDark = ControlPaint.DarkDark(baseColor);
            Color cSateDark = ControlPaint.Dark(this.StateColor);


            gr.SmoothingMode = SmoothingMode.AntiAlias;
            gr.CompositingQuality = CompositingQuality.HighQuality;

            float h = this.Height;
            float width = h * 0.85f;
            float widthI = h * 0.58f;
            float width2 = h * 0.9f;
            float width3 = h * 0.67f;
            float dist = h * 0.25f;
            if (width == 0)
                return;

            float penWidth = h * 0.25f / 2;

            LinearGradientBrush brushState = new LinearGradientBrush(new Point((int)(Width - width3), (int)(Width - width3)), new Point(3 * (int)(width3), 3 * (int)(width3)), cSateDark, this.StateColor);

            LinearGradientBrush brush = new LinearGradientBrush(new Point(0, 0), new Point(2 * this.Width, 2 * this.Height), cLight, cDarkDark);
            LinearGradientBrush brush2 = new LinearGradientBrush(new Point(0, 0), new Point(2 * this.Width, 2 * this.Height), cLightLight, Color.White);
            
            Pen p = new Pen(brush, penWidth);
            //gr.DrawEllipse(p, (this.Width - width) / 2, (this.Width - width) / 2, width, width);
                
            double angle = (360 * PercentageValue / 100) - 360;
            if (angle > 360)
                angle = 360;            

            GraphicsPath backCirclePath = new GraphicsPath();
            backCirclePath.AddEllipse((this.Width - width), (this.Width - width), 2 * width, 2 * width);

            GraphicsPath backInnerCirclePath = new GraphicsPath();
            backInnerCirclePath.AddEllipse((this.Width - widthI), (this.Width - widthI), 2 * widthI, 2 * widthI);

            GraphicsPath outterCirclePath = new GraphicsPath();
            outterCirclePath.AddEllipse((this.Width - width2), (this.Width - width2), 2 * width2, 2 * width2);

            GraphicsPath innerCirclePath = new GraphicsPath();
            innerCirclePath.AddEllipse((this.Width - width3), (this.Width - width3), 2 * width3, 2 * width3);

            GraphicsPath path = new GraphicsPath();//points, new byte[] { (byte)PathPointType.Start, (byte)PathPointType.Line, (byte)PathPointType.Line, (byte)PathPointType.Line });
            path.AddPie(0, 0, 2 * this.Width, 2 * this.Width, - (float)90, (float)angle);

            Image img = new Bitmap(2 * this.Width, 2 * this.Height);
            Graphics gg = Graphics.FromImage(img);

            Region backRegion = new System.Drawing.Region(backCirclePath);
            backRegion.Exclude(backInnerCirclePath);


            Region pathInnerRegion = new System.Drawing.Region(innerCirclePath);

            Region pathRegion = new System.Drawing.Region(outterCirclePath);
            pathRegion.Exclude(innerCirclePath);
            pathRegion.Exclude(path);

            Region regBackShadow = backRegion.Clone();
            regBackShadow.Translate(10, 10);

            Region regShadow = pathRegion.Clone();
            regShadow.Translate(10, 10);
            LinearGradientBrush shadowBrush = new LinearGradientBrush(new Point(0, 0), new Point(2 * this.Width, 2 * this.Height), Color.FromArgb(50, 0, 0, 0), Color.FromArgb(150, 0, 0, 0));

            gg.SmoothingMode = SmoothingMode.AntiAlias;
            gg.CompositingQuality = CompositingQuality.HighQuality;
            gg.InterpolationMode = InterpolationMode.HighQualityBicubic;

            gg.FillRegion(brushState, pathInnerRegion);

            gg.FillRegion(shadowBrush, regBackShadow);
            gg.FillRegion(brush, backRegion);
            
            gg.FillRegion(shadowBrush, regShadow);
            gg.FillRegion(brush2, pathRegion);

            gr.InterpolationMode = InterpolationMode.HighQualityBicubic;
            gr.DrawImage(img, 0, 0, this.Width, this.Height);

            gg.Dispose();
            innerCirclePath.Dispose();
            outterCirclePath.Dispose();
            backCirclePath.Dispose();
            
            pathRegion.Dispose();
            backRegion.Dispose();

            path.Dispose();
            brush.Dispose();
            brush2.Dispose();
            shadowBrush.Dispose();
            regShadow.Dispose();
            
            img.Dispose();


            string str1 = this.percentageValue.ToString("N2") + "%";
            string str2 = this.temperature.ToString("N2") + "oC";

            float size1 = this.Width / 13;
            float size2 = this.Width / 17;

            SizeF s1 = gr.MeasureString(str1, new Font(this.Font.FontFamily, size1));
            SizeF s2 = gr.MeasureString(str2, new Font(this.Font.FontFamily, size2));

            PointF p1 = new PointF(((float)this.Width - s1.Width) / 2, (this.Height / 2) - s1.Height - 2);
            PointF p2 = new PointF(((float)this.Width - s2.Width) / 2, (this.Height / 2) + 1);
            gr.DrawString(str1, new Font(this.Font.FontFamily, size1), Brushes.White, p1);
            gr.DrawString(str2, new Font(this.Font.FontFamily, size2), Brushes.White, p2);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
           
            this.DrawTankGauge(this.BaseColor, e.Graphics);

        }
    }
}
