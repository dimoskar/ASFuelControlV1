namespace ASFuelControl.Reports
{
    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.Windows.Forms;
    using Telerik.Reporting;
    using Telerik.Reporting.Drawing;

    /// <summary>
    /// Summary description for AlertReport.
    /// </summary>
    public partial class AlertReport : Telerik.Reporting.Report
    {
        public AlertReport()
        {
            //
            // Required for telerik Reporting designer support
            //
            InitializeComponent();
            this.pictureBox1.Value = Properties.Resources.MainLogo;
            //
            // TODO: Add any constructor code after InitializeComponent call
            //
        }
    }
}