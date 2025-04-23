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
        private bool DispenserLocked { set; get; }
        BindingList<VirtualDevices.VirtualTank> Tanks;
        List<UI.Controls.DispenserControl> dispenserControls = new List<Controls.DispenserControl>();

        private int currentPage = 0;
        private int maxPages = 0;

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

        public DispenserGrid()
        {
            InitializeComponent();
            previousSize = new Size(405, 472);

            ExpressionFormattingObject obj = new ExpressionFormattingObject("MyCondition", "column4 = true", false);
            obj.CellBackColor = Color.Red;
            obj.CellForeColor = Color.Black;
            obj.ApplyToRow = true;
            obj.ApplyOnSelectedRows = true;
            this.radGridView1.Columns["column3"].ConditionalFormattingObjectList.Add(obj);

           

            this.radButton1.DataBindings.Add("Enabled", this, "PreviousPageEnabled");
            this.radButton2.DataBindings.Add("Enabled", this, "NextPageEnabled");
        }

        private float previousScale = 1;
        public void RearangeControls2(bool defaultSize)
        {
            int cellWidth = (tableLayoutPanel1.Width - 20) / 4;// tableLayoutPanel1.GetColumnWidths().Min(a => a);
            int cellHeight = (tableLayoutPanel1.Height - 20) / 2;// tableLayoutPanel1.GetRowHeights().Min(a => a);

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
            foreach (UI.Controls.DispenserControl c in this.dispenserControls)
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
            previousScale = scaleFactor;
        }

        private void tableLayoutPanel1_Resize(object sender, EventArgs e)
        {
            if (this.Width == 0 || this.Height == 0)
                return;
            this.RearangeControls2(false);
        }

        public void LoadGrid(VirtualDevices.VirtualDispenser[] dispensers)
        {
            this.tableLayoutPanel1.Controls.Clear();
            this.dispenserControls.Clear();
            int col = 0;
            int row = 0;
            this.maxPages = (int)Math.Ceiling((decimal)dispensers.Length / (decimal)8);
            this.currentPage = 1;
            this.radLabel2.Text = string.Format("Σελίδα {0} από {1}", this.currentPage, this.maxPages);
            
            foreach (VirtualDevices.VirtualDispenser dispenser in dispensers)
            {
                UI.Controls.DispenserControl dispC = new UI.Controls.DispenserControl();
                dispC.Size = previousSize;
                dispC.Dispenser = dispenser;
                this.tableLayoutPanel1.Controls.Add(dispC);
                this.tableLayoutPanel1.SetColumn(dispC, col);
                this.tableLayoutPanel1.SetRow(dispC, row);
                //dispC.Dock = DockStyle.Fill;
                dispenser.DeviceLocked = dispenser.IsLocked(dispenser.DispenserId);
                dispC.RefreshValues();

                col++;
                if(col > 3)
                {
                    col = 0;
                    row++;
                    if (row > 1)
                        row = 0;
                }
                this.dispenserControls.Add(dispC);
                
            }
            radButton2.DataBindings[0].ReadValue();
            //this.flowLayoutPanel1.Controls.Clear();
            //foreach (VirtualDevices.VirtualDispenser dispenser in dispensers)
            //{
            //    UI.Controls.DispenserControl dispC = new UI.Controls.DispenserControl();
            //    dispC.Dispenser = dispenser;
            //    this.flowLayoutPanel1.Controls.Add(dispC);
            //    dispenser.DeviceLocked = dispenser.IsLocked(dispenser.DispenserId);
            //    dispC.RefreshValues();
                
            //}
        }

        public void LoadTanks(VirtualDevices.VirtualTank[] tanks)
        {
            this.Tanks = new BindingList<VirtualDevices.VirtualTank>(tanks);
            this.radGridView1.DataSource = this.Tanks;
            foreach (VirtualDevices.VirtualTank vt in this.Tanks)
            {
                vt.PropertyChanged += new PropertyChangedEventHandler(vt_PropertyChanged);
            }
           
        }

        void vt_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Status")
            {
                foreach (GridViewRowInfo row in this.radGridView1.Rows)
                {
                    row.InvalidateRow();
                }
            }
        }

        private void radGridView1_ViewCellFormatting(object sender, CellFormattingEventArgs e)
        {
            
        }

        private void radGridView1_CellFormatting(object sender, CellFormattingEventArgs e)
        {
            //VirtualDevices.VirtualTank vt = e.Row.DataBoundItem as VirtualDevices.VirtualTank;
            //if (vt == null)
            //    return;
            //if (e.Column.Name == "FuelType")
            //{
            //    e.CellElement.BackColor = Color.FromArgb(vt.BaseColor);
            //    //e.CellElement.ForeColor = Color.White;
            //}
            //else if (e.Column.Name == "TankNumber")
            //{
            //    if (vt.TankStatus == Common.Enumerators.TankStatusEnum.Offline)
            //    {
            //        e.CellElement.BackColor = Color.Black;
            //        e.CellElement.ForeColor = Color.White;
            //    }
            //    else if (vt.TankStatus == Common.Enumerators.TankStatusEnum.Idle)
            //    {
            //        e.CellElement.BackColor = Color.FromArgb(150, 150, 255);//.Blue;
            //        e.CellElement.ForeColor = Color.White;
            //    }
            //    else if (vt.TankStatus == Common.Enumerators.TankStatusEnum.Selling)
            //    {
            //        e.CellElement.BackColor = Color.Green;
            //        //e.CellElement.ForeColor = Color.White;
            //    }
            //    else if (vt.TankStatus == Common.Enumerators.TankStatusEnum.Filling || vt.TankStatus == Common.Enumerators.TankStatusEnum.FuelExtraction)
            //    {
            //        e.CellElement.BackColor = Color.Green;
            //        e.CellElement.ForeColor = Color.White;
            //    }
            //    else if (vt.TankStatus == Common.Enumerators.TankStatusEnum.Error || vt.TankStatus == Common.Enumerators.TankStatusEnum.LowLevel ||
            //        vt.TankStatus == Common.Enumerators.TankStatusEnum.LevelDecrease || vt.TankStatus == Common.Enumerators.TankStatusEnum.LevelIncrease || vt.TankStatus == Common.Enumerators.TankStatusEnum.HighWaterLevel)
            //    {
            //        e.CellElement.BackColor = Color.Red;
            //        e.CellElement.ForeColor = Color.White;
            //    }
            //    else
            //    {
            //        e.CellElement.BackColor = Color.Orange;
            //        e.CellElement.ForeColor = Color.Black;
            //    }
            //}
        }

        private void panel3_Click(object sender, EventArgs e)
        {
            this.DispenserLocked = !this.DispenserLocked;
            if (this.DispenserLocked)
            {
                this.panel3.BackgroundImage = Properties.Resources.Locked;
            }
            else
            {
                this.panel3.BackgroundImage = Properties.Resources.UnLocked;
            }
            foreach (Control c in this.dispenserControls)
            {
                UI.Controls.DispenserControl dispenser = c as UI.Controls.DispenserControl;
                
                dispenser.Dispenser.DeviceLocked = this.DispenserLocked;
                dispenser.RefreshValues();
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {
            if (this.Parent == null)
                return;
            this.Parent.Controls.Remove(this);
            this.Dispose();
        }

        private void radButton1_Click(object sender, EventArgs e)
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

        private void radButton2_Click(object sender, EventArgs e)
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
