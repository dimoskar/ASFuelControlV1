namespace ASFuelControl.Reports.Invoices
{
    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.Windows.Forms;
    using Telerik.Reporting;
    using Telerik.Reporting.Drawing;

    /// <summary>
    /// Summary description for InvoiceReport.
    /// </summary>
    public partial class InvoiceReport : Telerik.Reporting.Report
    {
        public InvoiceReport()
        {
            InitializeComponent();

            //if (System.IO.File.Exists("InvoiceLogo.png"))
            //{
            //    this.pictureBox1.Value = Image.FromFile("InvoiceLogo.png");
            //    //this.pictureBox1.Size = new SizeU(new Unit(10.93, UnitType.Cm), new Unit(2.5, UnitType.Cm));
            //    this.pictureBox1.Location = this.panel1.Location;
            //    this.panel1.Visible =false;
            //    this.pictureBox1.Visible = true;
            //}
            //this.pageHeader.Height = this.pageHeader.Height - new Unit(2, UnitType.Cm);
        }
    }
}