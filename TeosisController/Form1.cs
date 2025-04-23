using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TeosisController
{
    public partial class Form1 : Form
    {
        TeosisController.TeosisConnector controller = new TeosisConnector();
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            controller.ExecuteGetStatus(1);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            controller.CommunicationPort = "COM7";
            if (controller.IsConnected)
                return;
            controller.Connect();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (!controller.IsConnected)
                return;
            controller.Disconnect();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            controller.ExecuteGetTotals(1, 0);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            controller.ExecuteSetPrice(1, 0, this.numericUpDown1.Value);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            controller.ExecuteAuthorize(1, 0);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            controller.ExecuteDCRStatus();
        }
    }
}
