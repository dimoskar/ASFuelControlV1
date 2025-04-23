namespace ASFuelControl.Reports
{
    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.Windows.Forms;
    using Telerik.Reporting;
    using Telerik.Reporting.Drawing;

    /// <summary>
    /// Summary description for ShiftReportDispenser.
    /// </summary>
    public partial class ShiftReportDispenser : Telerik.Reporting.Report
    {
        public ShiftReportDispenser()
        {
            //
            // Required for telerik Reporting designer support
            //
            InitializeComponent();

            //
            // TODO: Add any constructor code after InitializeComponent call
            //
        }
    }

    public static class ShiftReportFunctions
    {
        //private static Data.DatabaseModel database = new Data.DatabaseModel(Data.Implementation.OptionHandler.ConnectionString);

        [Telerik.Reporting.Expressions.Function(Category = "Shift Report Functions", Description = "Returns if the row is from an internal sale")]
        public static bool IsInternale(System.Collections.Generic.IList<Data.InvoiceLine> invoiceLInes)
        {
            if (invoiceLInes.Count == 0)
                return true;

            Data.InvoiceLine invLine = invoiceLInes[0];
            return invLine.Invoice.InvoiceType.IsInternal.HasValue ? invLine.Invoice.InvoiceType.IsInternal.Value : false;
        }
    }
}