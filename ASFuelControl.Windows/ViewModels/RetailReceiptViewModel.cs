namespace ASFuelControl.Windows.ViewModels
{
    public sealed class RetailReceiptViewModel : ScenarioInvoiceViewModel
    {
        public RetailReceiptViewModel()
            : base(InvoiceEditorScenario.Retail, InvoiceCounterpartySelection.Customer,
                  InvoiceEditorLayout.RetailReceipt)
        {
        }
    }
}
