namespace ASFuelControl.Windows.UI.Forms
{
    public sealed class RetailReceiptForm : InvoiceFormEx
    {
        public RetailReceiptForm() : base(new ViewModels.RetailReceiptViewModel(),
            ViewModels.InvoiceEditorLayout.RetailReceipt)
        {
            Text = "Απόδειξη Λιανικής Καυσίμων";
        }
    }
}
