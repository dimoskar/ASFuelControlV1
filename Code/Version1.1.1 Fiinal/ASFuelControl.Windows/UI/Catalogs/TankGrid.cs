using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ASFuelControl.Windows.UI.Catalogs
{
    public partial class TankGrid : UserControl
    {
        private delegate void ShowHide(bool visible);

        Size previousSize = new Size(0, 0);
        int deliveryTime = 0;
        int literCheckTime = 0;
        int timeToWait = 0;
        List<UI.Controls.TankControl> tankControls = new List<Controls.TankControl>();
        Timer t = new Timer();

        public TankGrid()
        {
            InitializeComponent();
            previousSize = new Size(405, 460);
            deliveryTime = Data.Implementation.OptionHandler.Instance.GetIntOption("DeliveryWaitingTime", 900) * 1000;
            literCheckTime = Data.Implementation.OptionHandler.Instance.GetIntOption("LiterCheckWaitingTime", 15) * 1000;
            this.waitingClock1.TimerCompleted += new EventHandler(waitingClock1_TimerCompleted);
        }

        void waitingClock1_TimerCompleted(object sender, EventArgs e)
        {
            this.Invoke(new ShowHide(this.ShowHideClock), new object[] { false });
        }

        public void AddTimer(TimeSpan ts)
        {
            this.waitingClock1.AddWaitingTime(ts);
            this.Invoke(new ShowHide(this.ShowHideClock), new object[] { true });
            
        }

        public void RearangeControls(bool defaultSize)
        {
            if (this.ParentForm != null && this.ParentForm.WindowState == FormWindowState.Minimized)
                return;
            if (defaultSize)
                previousSize = new Size(405, 493);
            float ratioA = 1;

            int height = (int)this.flowLayoutPanel1.Height - 20;
            int width = (int)this.flowLayoutPanel1.Width - 20;
            decimal ratio = (decimal)405 / (decimal)493;
            int count = this.flowLayoutPanel1.Controls.Count;
            if (count == 0)
                return;

            Size maxSize = new Size(0, 0);
            for (int r = 1; r <= count; r++)
            {

                for (int c = 1; c <= count; c++)
                {
                    if (r * c < count)
                        continue;
                    if (r * c - r > count)
                        continue;
                    if (r * c - c > count)
                        continue;

                    int rheight = height / r;
                    int cwidth = width / c;
                    int ratioWidth = cwidth;
                    int ratioHeight = (int)((decimal)cwidth / ratio);
                    if (ratioHeight > rheight)
                    {
                        ratioWidth = (int)((decimal)rheight * ratio);
                    }
                    if (maxSize.Width < ratioWidth)
                        maxSize = new Size(ratioWidth, ratioHeight);
                }
            }

            float prevScaleFactor = (float)previousSize.Width / (float)405;
            float scaleFactor = (float)maxSize.Width / (float)previousSize.Width;
            previousSize = maxSize;

            foreach (Control c in this.flowLayoutPanel1.Controls)
            {
                UI.Controls.TankControl tank = c as UI.Controls.TankControl;
                if (tank != null)
                    tank.ScaleControl(scaleFactor);
            }
        }

        private void ShowHideClock(bool visibe)
        {
            if (visibe)
                this.waitingClock1.Show();
            else
                this.waitingClock1.Hide();
        }

        private void flowLayoutPanel1_Resize(object sender, EventArgs e)
        {
            this.RearangeControls(false);
        }

        public void LoadGrid(VirtualDevices.VirtualTank[] tanks)
        {
            tankControls.Clear();
            this.flowLayoutPanel1.Controls.Clear();
            foreach (VirtualDevices.VirtualTank tank in tanks)
            {
                UI.Controls.TankControl tankC = new UI.Controls.TankControl();
                tankC.Tank = tank;
                tank.PropertyChanged += new PropertyChangedEventHandler(tank_PropertyChanged);
                tankC.BaseColor = Color.FromArgb(tank.BaseColor);
                if (tankC.BaseColor.A == 0)
                {
                    tankC.BaseColor = Color.FromArgb(255, tankC.BaseColor.R, tankC.BaseColor.G, tankC.BaseColor.B); 
                }
                this.flowLayoutPanel1.Controls.Add(tankC);
                tankControls.Add(tankC);
                tankC.RefreshValues();
            }
        }

        void tank_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "TankStatus")
            {
                VirtualDevices.VirtualTank tank = sender as VirtualDevices.VirtualTank;
                if (tank == null)
                    return;
                var q1 = tankControls.Where(t => t.Tank.WaitingShouldEnd != DateTime.MinValue);
                if (q1 == null || q1.Count() == 0)
                    return;
                DateTime dMax = q1.Select(t => t.Tank).Max(t => t.WaitingShouldEnd);
                TimeSpan ts = dMax.Subtract(DateTime.Now);
            }
        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            using (Forms.InvoiceForm invForm = new Forms.InvoiceForm())
            {
                invForm.AddNewInvoice();
                invForm.ShowDialog();
            }
        }
    }
}
