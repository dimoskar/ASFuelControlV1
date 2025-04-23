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
    public partial class PriceControl : UserControl
    {
        Data.FuelType currentFuelType = null;
        List<LcdUpDown> lcdControls = new List<LcdUpDown>(); 

        public Data.FuelType CurrentFuelType
        {
            set 
            { 
                this.currentFuelType = value;
                this.fuelTypeBindingSource.DataSource = this.currentFuelType;
                this.ApllyPrice();
                if (this.currentFuelType.Color.HasValue)
                    this.ApplyColor(Color.FromArgb(this.currentFuelType.Color.Value));
                else
                    this.ApplyColor(Color.Black);
            }
            get { return this.currentFuelType; }
        }

        public decimal Price
        {
            set;
            get;
        }

        public PriceControl()
        {
            InitializeComponent();
            this.int1.DigitChanged += new EventHandler<DigitChangedArgs>(price_DigitChanged);
            this.int2.DigitChanged += new EventHandler<DigitChangedArgs>(price_DigitChanged);
            this.decimal1.DigitChanged += new EventHandler<DigitChangedArgs>(price_DigitChanged);
            this.decimal2.DigitChanged += new EventHandler<DigitChangedArgs>(price_DigitChanged);
            this.decimal3.DigitChanged += new EventHandler<DigitChangedArgs>(price_DigitChanged);

            lcdControls.Add(int1);
            lcdControls.Add(int2);
            lcdControls.Add(decimal1);
            lcdControls.Add(decimal2);
            lcdControls.Add(decimal3);

            this.excludeBalanceLabelCbx.Enabled = Program.AdminConnected;
        }

        void price_DigitChanged(object sender, DigitChangedArgs e)
        {
            LcdUpDown lcd = sender as LcdUpDown;
            if(lcd == null)
                return;
            
            if (e.OldDigit == 0 && e.NewDigit == 9)
            {
                int index = this.lcdControls.IndexOf(lcd);
                if (index > 0)
                    this.lcdControls[index - 1].DecreaseDigit();
            }
            if (e.OldDigit == 9 && e.NewDigit == 0)
            {
                int index = this.lcdControls.IndexOf(lcd);
                if (index > 0)
                    this.lcdControls[index - 1].IncreaseDigit();
            }

            string strPrice = string.Format("{0}{1},{2}{3}{4}", this.int1.Digit, this.int2.Digit, this.decimal1.Digit, this.decimal2.Digit, this.decimal3.Digit);
            decimal dec = decimal.Parse(strPrice);
            this.currentFuelType.CurrentPrice = dec;
        }

        public string ReverseString(string s)
        {
            char[] arr = s.ToCharArray();
            Array.Reverse(arr);
            return new string(arr);
        }

        private void ApllyPrice()
        {
            if (this.currentFuelType == null)
                return;

            string s = this.currentFuelType.CurrentPrice.ToString("N3").Replace(",", ".");
            string decimalPart = s.Substring(s.IndexOf(".") + 1);

            int integerPart = (int)this.currentFuelType.CurrentPrice;

            if (decimalPart.Length > 0)
                this.decimal1.Digit = int.Parse(decimalPart[0].ToString());
            if (decimalPart.Length > 1)
                this.decimal2.Digit = int.Parse(decimalPart[1].ToString());
            if (decimalPart.Length > 2)
                this.decimal3.Digit = int.Parse(decimalPart[2].ToString());

            string strInt = ReverseString(integerPart.ToString());
            if (strInt.Length > 0)
                this.int2.Digit = int.Parse(strInt[0].ToString());
            if (strInt.Length > 1)
                this.int1.Digit = int.Parse(strInt[1].ToString());
        }

        private void ApplyColor(Color color)
        {
            this.CurrentFuelType.Color = color.ToArgb();
            this.nameLabel.BackColor = color;
            this.panel1.BackColor = color;
            this.int1.BaseColor = color;
            this.int2.BaseColor = color;
            this.decimal1.BaseColor = color;
            this.decimal2.BaseColor = color;
            this.decimal3.BaseColor = color;

            Color foreColor = GetReadableForeColor(color);// (PerceivedBrightness(color) > 130 ? Color.Black : Color.White);

            this.int1.ForeColor = foreColor;
            this.int2.ForeColor = foreColor;
            this.decimal1.ForeColor = foreColor;
            this.decimal2.ForeColor = foreColor;
            this.decimal3.ForeColor = foreColor;
            this.nameLabel.ForeColor = foreColor;

            this.lcdUpDown6.BaseColor = color;
            this.lcdUpDown6.ForeColor = foreColor;
        }

        private int PerceivedBrightness(Color c)
        {
            return (int)Math.Sqrt(
            c.R * c.R * .241 +
            c.G * c.G * .691 +
            c.B * c.B * .068);
        }

        private static Color GetReadableForeColor(Color c)
        {
            return (((c.R + c.B + c.G) / 3) > 128) ? Color.Black : Color.White;
        }

        private void nameLabel_Click(object sender, EventArgs e)
        {
            using (Telerik.WinControls.UI.RadColorDialogForm dlg = new Telerik.WinControls.UI.RadColorDialogForm())
            {
                if (this.currentFuelType == null)
                    return;
                if (currentFuelType.Color.HasValue)
                    dlg.SelectedColor = Color.FromArgb(CurrentFuelType.Color.Value);
                else
                    dlg.SelectedColor = Color.DarkGreen;
                dlg.ShowDialog();
                ApplyColor(dlg.SelectedColor);
            }
        }
    }
}
