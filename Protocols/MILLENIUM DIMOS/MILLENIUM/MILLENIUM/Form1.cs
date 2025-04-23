using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace MILLENIUM
{
    public partial class Form1 : Form
    {
       

        private Thread workerThread = null;
        private bool stopProcess = false;
        private bool startNozzle = false;
        int bufSize;

        string status1, status2;
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            PumpController.PortName = "COM12";
            PumpController.BaudRate = 9600;
            PumpController.Parity = System.IO.Ports.Parity.None;
            PumpController.DataBits = 8;
            PumpController.ParityReplace = 0;
            PumpController.RtsEnable = true;
            PumpController.Open();
        }
        private void Simulator()
        {
            try
            {
                if (!stopProcess)
                {
                    while (!stopProcess)
                    {
                      
                        /*if (!startNozzle)
                        {
                            PumpController.Write(Protocols.Millenium.StartNozzle1, 0, Protocols.Millenium.StartNozzle1.Length);
                            PumpController.Write(Protocols.Millenium.StartNozzle2, 0, Protocols.Millenium.StartNozzle2.Length);
                            PumpController.Write(Protocols.Millenium.GetData, 0, Protocols.Millenium.GetData.Length);
                           
                            startNozzle = true;
                        }
                        else
                        {*/
                            Thread.Sleep(200);
                            PumpController.Write(Protocols.Millenium.StatusNZ1, 0, Protocols.Millenium.StatusNZ1.Length);
                            PumpController.Write(Protocols.Millenium.StatusNZ2, 0, Protocols.Millenium.StatusNZ2.Length);
                            Thread.Sleep(50);
                            PumpController.Write(Protocols.Millenium.GetData, 0, Protocols.Millenium.GetData.Length);
                       // }
                        


                    }
                }
                else
                {
                    this.workerThread.Abort();
                }
            }
            catch
            {

            }

        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.stopProcess = false;
            this.workerThread = new Thread(new ThreadStart(this.Simulator));
            this.workerThread.Start();
            button1.Enabled = false;
        }

        private void PumpController_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            try
            {
                Thread.Sleep(100);
                bufSize = PumpController.BytesToRead;
                Byte[] dataBuffer = new Byte[bufSize];
                PumpController.Read(dataBuffer, 0, bufSize);
                this.Invoke((MethodInvoker)delegate() { status1 = BitConverter.ToString(dataBuffer).Replace("-", string.Empty); });
                this.Invoke((MethodInvoker)delegate() { status2 = BitConverter.ToString(dataBuffer); });
                this.Invoke(new EventHandler(RecieveData));
                PumpController.DiscardOutBuffer();
            }
            catch (Exception Error_DataRecieved)
            {
                //MessageBox.Show(Error_DataRecieved.ToString());
            }
        }


        private void RecieveData(object sender, EventArgs e)
        {
            richTextBox1.Clear();
            richTextBox1.AppendText(status1 + "\n");
            richTextBox1.AppendText(bufSize.ToString());

            string requestPump = status1.Replace("-", string.Empty);
            string[] conter = status2.Split('-');
            if (conter.Length == 47 && conter[0] == "FD" && conter[1] == "02" && conter[22] == "FB")
            {
                if (conter[21] == "42")
                {
                    PumpController.Write(Protocols.Millenium.StartNozzle1, 0, Protocols.Millenium.StartNozzle1.Length);
                    PumpController.Write(Protocols.Millenium.GetData, 0, Protocols.Millenium.GetData.Length);
                }
                if (conter[21] == "43")
                {
                    StatusNZ1.Text = "IDLE";
                    StatusNZ1.BackColor = Color.Green;
                }
                if (conter[21] == "45")
                {
                    StatusNZ1.Text = "READY";
                    StatusNZ1.BackColor = Color.Red;
                   
                }
            }

            if (conter.Length == 47 && conter[23] == "FD" && conter[24] == "02" && conter[45] == "FB")
            {
                if (conter[44] == "43")
                {
                    PumpController.Write(Protocols.Millenium.StartNozzle2, 0, Protocols.Millenium.StartNozzle2.Length);
                    PumpController.Write(Protocols.Millenium.GetData, 0, Protocols.Millenium.GetData.Length);
                }
                if (conter[44] == "44")
                {
                    StatusNZ2.Text = "IDLE";
                    StatusNZ2.BackColor = Color.Green;
                }
                if (conter[44] == "46")
                {
                    StatusNZ2.Text = "READY";
                    StatusNZ2.BackColor = Color.Red;

                }
            }

            
            
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.workerThread.Abort();
            button1.Enabled = true;
            startNozzle = false;
            StatusNZ1.Text = "OFFLINE";
            StatusNZ1.BackColor = Color.Black;
            
            StatusNZ2.Text = "OFFLINE";
            StatusNZ2.BackColor = Color.Black;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            PumpController.Write(Protocols.Millenium.AuthoriseNZ1, 0, Protocols.Millenium.AuthoriseNZ1.Length);
            PumpController.Write(Protocols.Millenium.GetData, 0, Protocols.Millenium.GetData.Length);
        }
    }
   
}
