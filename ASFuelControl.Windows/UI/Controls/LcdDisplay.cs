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
    public partial class LcdDisplay : UserControl
    {
        private Color baseColor = Color.Blue;
        
        public string LabelText
        {
            set 
            { 
                this.label1.Text = value;
            }
            get { return this.label1.Text; }
        }

        public Color BaseColor 
        {
            set 
            { 
                this.baseColor = value;
                this.Invalidate();
            }
            get { return this.baseColor; } 
        }

        public LcdDisplay()
        {
            //SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            InitializeComponent();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Point center1 = new Point(0, this.Height / 2);
            Point center2 = new Point(0, 0);
            System.Drawing.Drawing2D.LinearGradientBrush brush = new System.Drawing.Drawing2D.LinearGradientBrush(center1, center2, Color.FromArgb(128, 180, 180, 180), Color.Transparent);
            System.Drawing.SolidBrush solidBrush = new System.Drawing.SolidBrush(this.BaseColor);

            e.Graphics.FillRectangle(solidBrush, new Rectangle(0, 0, this.Width, this.Height));
            e.Graphics.FillRectangle(brush, new Rectangle(0,0,this.Width, this.Height/2));
        }

        protected override void OnForeColorChanged(EventArgs e)
        {
            base.OnForeColorChanged(e);
            this.label1.ForeColor = this.ForeColor;
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            this.label1.Font = this.Font;
        }
    }
}
