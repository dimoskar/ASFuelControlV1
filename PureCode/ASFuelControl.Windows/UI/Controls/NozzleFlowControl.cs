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
    public partial class NozzleFlowControl : UserControl
    {
        private Data.NozzleFlow nozzleFlow;

        public Data.NozzleFlow NozzleFlow
        {
            set
            {
                this.nozzleFlow = value;
                if(nozzleFlow.FlowState == 0)
                    this.panel1.BackgroundImage = Properties.Resources.ValveClose;
                else
                    this.panel1.BackgroundImage = Properties.Resources.ValveOpen;
                this.radLabel1.Text = "Δεξαμενή : " + this.nozzleFlow.Tank.Description;
            }
            get{return this.nozzleFlow;}
        }

        public NozzleFlowControl()
        {
            InitializeComponent();
        }

        private void panel1_Click(object sender, EventArgs e)
        {
            if(this.nozzleFlow.FlowState == 0)
            {
                this.nozzleFlow.FlowState = 1;
                this.panel1.BackgroundImage = Properties.Resources.ValveOpen;
            }
            else if(this.nozzleFlow.FlowState == 1)
            {
                this.nozzleFlow.FlowState = 0;
                this.panel1.BackgroundImage = Properties.Resources.ValveClose;
            }
        }
    }
}
