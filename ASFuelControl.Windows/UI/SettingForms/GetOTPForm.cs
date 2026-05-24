using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ASFuelControl.Windows.UI.SettingForms
{
    public partial class GetOTPForm : Form
    {
        public string OTP { get; private set; }

        public GetOTPForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.OTP = this.textBox1.Text;
            if (string.IsNullOrEmpty(this.OTP))
                MessageBox.Show(this, "Δεν Καταχωρήσατε το OTP", "Σφαλμα...");
            else
                this.DialogResult = DialogResult.OK;
        }
    }
}
