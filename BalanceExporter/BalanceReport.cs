namespace BalanceExporter
{
    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.Windows.Forms;
    using Telerik.Reporting;
    using Telerik.Reporting.Drawing;

    /// <summary>
    /// Summary description for BalanceReport.
    /// </summary>
    public partial class BalanceReport : Telerik.Reporting.Report
    {
        public BalanceReport()
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