using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ASFuelControl.Reports
{
    public partial class ReportViewer : UserControl
    {
        public ReportViewer()
        {
            InitializeComponent();
            this.shiftReport1.ReportParameters[0].Value = "f85ebc40-fa71-42a4-bf6e-14d90bebf3db";
        }
    }
}
