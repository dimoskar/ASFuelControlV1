namespace ASFuelControl.Reports.Invoices
{
    public partial class InvoiceReportNarrow
    {
        /// <summary>
        /// POS80 printers commonly expose less than the nominal 80 mm roll.
        /// Preserve every field's width while moving report items 2 mm away
        /// from the physical right edge, where final digits were being clipped.
        /// </summary>
        public void ApplyThermalPrintSafety()
        {
            ShiftItemsLeft(this.pageHeaderSection1.Items);
            ShiftItemsLeft(this.groupHeaderSection1.Items);
            ShiftItemsLeft(this.detail.Items);
            ShiftItemsLeft(this.pageFooterSection1.Items);
        }

        private static void ShiftItemsLeft(System.Collections.IEnumerable items)
        {
            const double safetyCm = 0.2;
            foreach (object entry in items)
            {
                var item = entry as Telerik.Reporting.ReportItem;
                if (item == null)
                    continue;
                if (item.Location.X.Value >= safetyCm)
                    item.Location = new Telerik.Reporting.Drawing.PointU(
                        new Telerik.Reporting.Drawing.Unit(item.Location.X.Value - safetyCm,item.Location.X.Type),
                        item.Location.Y);

                var panel = item as Telerik.Reporting.Panel;
                if (panel != null)
                    ShiftItemsLeft(panel.Items);
            }
        }
    }
}
