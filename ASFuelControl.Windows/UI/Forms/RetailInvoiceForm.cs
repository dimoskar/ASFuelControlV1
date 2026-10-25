namespace ASFuelControl.Windows.UI.Forms
{
    public sealed class RetailInvoiceForm : InvoiceFormEx
    {
        public RetailInvoiceForm() : base(new ViewModels.RetailInvoiceViewModel(),
            ViewModels.InvoiceEditorLayout.DetailedInvoice)
        {
            Text = "Τιμολόγιο Λιανικής";
        }
    }
}
