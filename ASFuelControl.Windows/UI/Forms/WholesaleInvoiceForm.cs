namespace ASFuelControl.Windows.UI.Forms
{
    public sealed class WholesaleInvoiceForm : InvoiceFormEx
    {
        public WholesaleInvoiceForm() : base(new ViewModels.WholesaleInvoiceViewModel(),
            ViewModels.InvoiceEditorLayout.DetailedInvoice)
        {
            Text = "Τιμολόγιο Χονδρικής";
        }
    }
}
