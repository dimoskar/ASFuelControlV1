using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Telerik.WinControls.UI;

namespace ASFuelControl.Windows.UI.Catalogs
{
    public partial class DispenserGrid : UserControl
    {
        Size previousSize = new Size(0, 0);

        BindingList<VirtualDevices.VirtualTank> Tanks;

        public DispenserGrid()
        {
            InitializeComponent();
            previousSize = new Size(405, 460);

            ExpressionFormattingObject obj = new ExpressionFormattingObject("MyCondition", "column4 = true", false);
            obj.CellBackColor = Color.Red;
            obj.CellForeColor = Color.White;
            this.radGridView1.Columns["column3"].ConditionalFormattingObjectList.Add(obj);
        }

        public void RearangeControls(bool defaultSize)
        {
            //405; 460
            if(defaultSize)
                previousSize = new Size(405, 460);
            if (this.ParentForm != null && this.ParentForm.WindowState == FormWindowState.Minimized)
                return;
            float ratioA = 1;

            int height = (int)this.flowLayoutPanel1.Height - 20;
            int width = (int)this.flowLayoutPanel1.Width - 20;
            decimal ratio = (decimal)405 / (decimal)460;
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
                UI.Controls.DispenserControl dispenser = c as UI.Controls.DispenserControl;
                if (dispenser != null)
                    dispenser.ScaleControl(scaleFactor);
            }
        }

        private void flowLayoutPanel1_Resize(object sender, EventArgs e)
        {
            this.RearangeControls(false);
           
        }

        public void LoadGrid(VirtualDevices.VirtualDispenser[] dispensers)
        {
            this.flowLayoutPanel1.Controls.Clear();
            foreach (VirtualDevices.VirtualDispenser dispenser in dispensers)
            {
                UI.Controls.DispenserControl dispC = new UI.Controls.DispenserControl();
                dispC.Dispenser = dispenser;
                this.flowLayoutPanel1.Controls.Add(dispC);
                dispC.RefreshValues();
            }
        }

        public void LoadTanks(VirtualDevices.VirtualTank[] tanks)
        {
            this.Tanks = new BindingList<VirtualDevices.VirtualTank>(tanks);
            this.radGridView1.DataSource = this.Tanks;

           
        }

        private void radGridView1_ViewCellFormatting(object sender, CellFormattingEventArgs e)
        {
            VirtualDevices.VirtualTank vt = e.Row.DataBoundItem as VirtualDevices.VirtualTank;
            if (vt == null)
                return;
            if (e.Column.Name != "FuelType")
                return;
            e.CellElement.BackColor = Color.FromArgb(vt.BaseColor);
            e.CellElement.ForeColor = Color.White;
        }
    }
}
