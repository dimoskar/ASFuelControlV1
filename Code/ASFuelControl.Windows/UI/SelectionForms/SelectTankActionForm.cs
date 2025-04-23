using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Telerik.WinControls.UI;

namespace ASFuelControl.Windows.UI.SelectionForms
{
    public partial class SelectTankActionForm : RadForm
    {
        public bool Fill { set; get; }

        public SelectTankActionForm()
        {
            InitializeComponent();
        }

        private void Fill_Clicked(object sender, EventArgs e)
        {
            this.Fill = true;
            this.DialogResult = DialogResult.OK;
        }

        private void Drain_Clicked(object sender, EventArgs e)
        {
            this.Fill = false;
            this.DialogResult = DialogResult.OK;
        }
    }
}
