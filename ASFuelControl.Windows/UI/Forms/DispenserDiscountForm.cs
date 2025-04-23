using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Telerik.WinControls.UI;

namespace ASFuelControl.Windows.UI.Forms
{
    public partial class DispenserDiscountForm : RadForm
    {
        private Data.Dispenser currentDispenser;

        public Data.Dispenser CurrentDispenser
        {
            set 
            {
                this.currentDispenser = value;

                foreach (VirtualDevices.VirtualNozzle nz in this.CurrentVirtualDispenser.Nozzles)
                {
                    foreach (Data.Nozzle dnz in this.currentDispenser.Nozzles)
                    {
                        if (dnz.NozzleId != nz.NozzleId)
                            continue;
                        dnz.DiscountPercentage = nz.DiscountPercentage;
                    }
                }
                
                this.nozzleBindingSource.DataSource = this.currentDispenser.Nozzles.OrderBy(n=>n.OrderId);
                this.nozzleBindingSource.ResetBindings(false);
            }
            get { return this.currentDispenser; }
        }

        public VirtualDevices.VirtualDispenser CurrentVirtualDispenser
        {
            set;
            get;
        }

        public DispenserDiscountForm()
        {
            InitializeComponent();
        }

        private void radButton2_Click(object sender, EventArgs e)
        {
            foreach (Data.Nozzle nz in this.currentDispenser.Nozzles)
                nz.DiscountPercentage = 0;
            foreach (VirtualDevices.VirtualNozzle nz in this.CurrentVirtualDispenser.Nozzles)
            {
                foreach (Data.Nozzle dnz in this.currentDispenser.Nozzles)
                {
                    if (dnz.NozzleId != nz.NozzleId)
                        continue;
                    nz.DiscountPercentage = dnz.DiscountPercentage;
                }
            }
            this.Close();
        }

        private void radButton3_Click(object sender, EventArgs e)
        {
            string password = Data.Implementation.OptionHandler.Instance.GetOption("[DiscountEnablePassword]");
            if (password != null && this.discountText1.Text != password)
                return;
            this.nozzleRadGridView.Enabled = true;
            this.radButton1.Enabled = true;
        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            foreach (VirtualDevices.VirtualNozzle nz in this.CurrentVirtualDispenser.Nozzles)
            {
                foreach (Data.Nozzle dnz in this.currentDispenser.Nozzles)
                {
                    if (dnz.NozzleId != nz.NozzleId)
                        continue;
                    nz.DiscountPercentage = dnz.DiscountPercentage;
                }
            }
            this.Close();
        }
    }
}
