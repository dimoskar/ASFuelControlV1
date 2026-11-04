namespace ASFuelControl.Windows.UI.Forms
{
    /// <summary>
    /// Compact editor for VAT-exclusive sales documents such as the special
    /// cancellation document. It uses the fuel line layout instead of the
    /// legacy InvoiceFormEx panel, which had no fuel field at all.
    /// </summary>
    public sealed class CompactWholesaleInvoiceForm : InvoiceFormEx
    {
        public CompactWholesaleInvoiceForm()
            : base(new ViewModels.CompactWholesaleInvoiceViewModel(),
                  ViewModels.InvoiceEditorLayout.RetailReceipt)
        {
            Text = "Παραστατικό Καυσίμων";
        }
    }
}
