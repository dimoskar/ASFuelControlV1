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
    public partial class LcdUpDown : UserControl
    {
        public event EventHandler<DigitChangedArgs> DigitChanged;

        private Color baseColor = Color.Blue;
        private bool isSeparator = false;

        public bool IsSeparator 
        {
            set 
            { 
                this.isSeparator = value;
                if (isSeparator)
                {
                    this.lcdDisplay1.LabelText = ",";
                    this.panel2.BackgroundImage = null;
                    this.panel1.BackgroundImage = null;
                }
                else
                {
                    this.lcdDisplay1.LabelText = "0";
                    this.panel2.BackgroundImage = Properties.Resources.Up;
                    this.panel1.BackgroundImage = Properties.Resources.Down;
                }
            }
            get { return this.isSeparator; }
        }

        public int Digit 
        {
            set 
            {
                this.lcdDisplay1.LabelText = value.ToString();
            }
            get 
            {
                if (isSeparator)
                    return 0;
                return int.Parse(this.lcdDisplay1.LabelText); 
            }
        }

        public Color BaseColor
        {
            set
            {
                this.baseColor = value;
                this.lcdDisplay1.BaseColor = this.baseColor;
                this.panel1.BackColor = this.baseColor;
                this.panel2.BackColor = this.baseColor;
                this.Invalidate();
            }
            get { return this.baseColor; }
        }

        public LcdUpDown()
        {
            InitializeComponent();
            this.lcdDisplay1.LabelText = "0";
        }

        public void IncreaseDigit()
        {
            if (isSeparator)
                return;
            int oldValue = this.Digit;
            int currentDigit = this.Digit + 1;
            if (currentDigit > 9)
                this.Digit = 0;
            else
                this.Digit = currentDigit;
            if (this.DigitChanged != null)
                this.DigitChanged(this, new DigitChangedArgs(this.Digit, oldValue));
        }

        public void DecreaseDigit()
        {
            if (isSeparator)
                return;
            int oldValue = this.Digit;
            int currentDigit = this.Digit - 1;
            if (currentDigit < 0)
                this.Digit = 9;
            else
                this.Digit = currentDigit;
            if (this.DigitChanged != null)
                this.DigitChanged(this, new DigitChangedArgs(this.Digit, oldValue));
        }

        protected override void OnForeColorChanged(EventArgs e)
        {
            base.OnForeColorChanged(e);
            this.lcdDisplay1.ForeColor = this.ForeColor;
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            this.lcdDisplay1.Font = this.Font;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            this.panel1.Height = this.Height / 8;
            this.panel2.Height = this.Height / 8;
        }

        private void panel2_Click(object sender, EventArgs e)
        {
            this.IncreaseDigit();
        }

        private void panel1_Click(object sender, EventArgs e)
        {
            this.DecreaseDigit();
        }
    }

    public class DigitChangedArgs : EventArgs
    {
        public int NewDigit { set; get; }
        public int OldDigit { set; get; }

        public DigitChangedArgs(int newVal, int oldVal)
        {
            this.NewDigit = newVal;
            this.OldDigit = oldVal;
        }
    }
}
