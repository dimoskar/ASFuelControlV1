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
        private int currentPage = 0;
        private int maxPages = 0;

        Size previousSize = new Size(0, 0);
        int deliveryTime = 0;
        int literCheckTime = 0;
        int timeToWait = 0;
        List<UI.Controls.TankControl> tankControls = new List<Controls.TankControl>();
        Timer t = new Timer();

        public bool PreviousPageEnabled
        {
            get
            {
                if (this.currentPage <= 1)
                    return false;
                return true;
            }
        }

        public bool NextPageEnabled
        {
            get
            {
                if (this.currentPage >= this.maxPages)
                    return false;
                return true;
            }
        }

        public TankGrid()
        {
            InitializeComponent();

            this.radButton4.DataBindings.Add("Enabled", this, "PreviousPageEnabled");
            this.radButton3.DataBindings.Add("Enabled", this, "NextPageEnabled");

            previousSize = new Size(405, 493);
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

        public void RearangeControls2(bool defaultSize)
        {
            int cellWidth = (tableLayoutPanel1.Width - 20) / 4;
            int cellHeight = (tableLayoutPanel1.Height - 20) / 2;

            if (cellWidth <= 0 || cellHeight <= 0)
                return;

            float ratioWidth = (float)cellWidth / (float)previousSize.Width;
            float ratioHeight = (float)cellHeight / (float)previousSize.Height;
            float scaleFactor = 1;
            if (ratioHeight > ratioWidth)
                scaleFactor = ratioWidth;
            else
                scaleFactor = ratioHeight;
            //scaleFactor = scaleFactor / previousScale;

            int i = 0;
            this.tableLayoutPanel1.SuspendLayout();
            foreach (UI.Controls.TankControl c in this.tankControls)
            {

                int minControl = (this.currentPage - 1) * 8;
                int maxControl = minControl + 8 - 1;
                if (i >= minControl && i <= maxControl)
                    c.Show();
                else
                    c.Hide();
                float prevScale = (float)c.Width / (float)this.previousSize.Width;
                float sFactor = scaleFactor / prevScale;
                c.ScaleControl(sFactor);
                i++;
            }
            this.tableLayoutPanel1.ResumeLayout();
        }

        private void ShowHideClock(bool visibe)
        {
            if (visibe)
                this.waitingClock1.Show();
            else
                this.waitingClock1.Hide();
        }

        private void tableLayoutPanel1_Resize(object sender, EventArgs e)
        {
            this.RearangeControls2(false);
        }

        public void LoadGrid(VirtualDevices.VirtualTank[] tanks)
        {
            this.tableLayoutPanel1.Controls.Clear();
            this.tankControls.Clear();
            int col = 0;
            int row = 0;
            this.maxPages = (int)Math.Ceiling((decimal)tanks.Length / (decimal)8);
            this.currentPage = 1;
            this.radLabel2.Text = string.Format("Σελίδα {0} από {1}", this.currentPage, this.maxPages);

            foreach (VirtualDevices.VirtualTank tank in tanks)
            {
                UI.Controls.TankControl tankC = new UI.Controls.TankControl();
                tankC.Size = previousSize;
                tankC.Tank = tank;
                this.tableLayoutPanel1.Controls.Add(tankC);
                this.tableLayoutPanel1.SetColumn(tankC, col);
                this.tableLayoutPanel1.SetRow(tankC, row);
                tank.PropertyChanged += new PropertyChangedEventHandler(tank_PropertyChanged);
                //dispC.Dock = DockStyle.Fill;

                tankC.BaseColor = Color.FromArgb(tank.BaseColor);
                if (tankC.BaseColor.A == 0)
                {
                    tankC.BaseColor = Color.FromArgb(255, tankC.BaseColor.R, tankC.BaseColor.G, tankC.BaseColor.B);
                }

                tankC.RefreshValues();

                col++;
                if (col > 3)
                {
                    col = 0;
                    row++;
                    if (row > 1)
                        row = 0;
                }
                this.tankControls.Add(tankC);

            }
            this.RearangeControls2(false);
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
            using (Forms.InvoiceFormEx invForm = new Forms.InvoiceFormEx())
            {
                invForm.CreateNew();
                invForm.ShowDialog();
            }
        }

        private void radButton2_Click(object sender, EventArgs e)
        {
            using (Forms.DeliveryForm dForm = new Forms.DeliveryForm())
            {
                dForm.Tanks = this.tankControls.Select(t => t.Tank).ToArray();
                dForm.ShowDialog();
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {
            if (this.Parent == null)
                return;
            this.Parent.Controls.Remove(this);
            this.Dispose();
        }

        private void radButton4_Click(object sender, EventArgs e)
        {
            if (this.currentPage > 1)
            {
                currentPage--;
                this.radButton1.DataBindings[0].ReadValue();
                this.radButton2.DataBindings[0].ReadValue();
                this.RearangeControls2(false);
                this.radLabel2.Text = string.Format("Σελίδα {0} από {1}", this.currentPage, this.maxPages);

            }
        }

        private void radButton3_Click(object sender, EventArgs e)
        {
            if (this.currentPage < this.maxPages)
            {
                currentPage++;
                this.radButton1.DataBindings[0].ReadValue();
                this.radButton2.DataBindings[0].ReadValue();
                this.RearangeControls2(false);
                this.radLabel2.Text = string.Format("Σελίδα {0} από {1}", this.currentPage, this.maxPages);
            }
        }

        
    }
}
