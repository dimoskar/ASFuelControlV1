using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TestTatsuno
{
    public partial class Form1 : Form
    {
        ASFuelControl.Tatsuno.TatsunoPdeClient client;
        SerialPort port;

        public Form1()
        {
            InitializeComponent();
            ASFuelControl.Common.Logger.LevelFrom = NLog.LogLevel.Trace;
            ASFuelControl.Common.Logger.LevelTo = NLog.LogLevel.Fatal;
            string path = System.Environment.CurrentDirectory;
            System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(path);
            var logDir = dir.CreateSubdirectory("Logging");
            ASFuelControl.Common.Logger.InitializeLogger(logDir.FullName);
            this.comboBox1.Items.Clear();
            var comPorts = System.IO.Ports.SerialPort.GetPortNames();
            foreach (string cp in comPorts)
                this.comboBox1.Items.Add(cp);
            if (comPorts.Length > 0)
            {
                this.comboBox1.Text = comPorts.First();
            }
            // Optional: ensure textBox1 is ready for logs
            textBox1.WordWrap = false;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            port = new SerialPort(this.comboBox1.Text);
            client = new ASFuelControl.Tatsuno.TatsunoPdeClient(port, new ASFuelControl.Tatsuno.PdeOptions());
            client.Open();
            this.checkBox2.Checked = client.IsOpen();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.checkBox1.Checked = client.Initialize(this.GetAddress());
            //client.Authorize(GetAddress(), 0, 0, 0, 0, 1);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            var status = client.RequestStatus(this.GetAddress());
            if (status != null)
            {
                string st = string.Format("K:{0}, M:{1}, N:{2}, S:{3}", status.Keyboard, status.Mode, status.Nozzles, status.State);
                this.textBox1.Text = st;
            }
            else
                this.textBox1.Text = "Error";
        }

        private byte GetAddress()
        {
            return (byte)(32 + this.numericUpDown1.Value);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            this.checkBox3.Checked = false;
            this.checkBox3.Checked = client.SendResetCommand(this.GetAddress());
        }
    }
}
