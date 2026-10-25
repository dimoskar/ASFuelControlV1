namespace ASFuelControl.Windows.UI.Forms
{
    public sealed class SupplierInvoiceForm : InvoiceFormEx
    {
        public SupplierInvoiceForm() : base(new ViewModels.SupplierInvoiceViewModel(),
            ViewModels.InvoiceEditorLayout.SupplierPurchase)
        {
            Text = "Παραστατικό Αγοράς Προμηθευτή";
        }
    }
}
