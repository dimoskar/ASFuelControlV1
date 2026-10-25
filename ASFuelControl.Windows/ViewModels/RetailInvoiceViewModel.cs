namespace ASFuelControl.Windows.ViewModels
{
    public sealed class RetailInvoiceViewModel : ScenarioInvoiceViewModel
    {
        public RetailInvoiceViewModel()
            : base(InvoiceEditorScenario.Retail, InvoiceCounterpartySelection.Customer,
                  InvoiceEditorLayout.DetailedInvoice)
        {
        }
    }
}
