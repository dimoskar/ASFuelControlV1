namespace ASFuelControl.Windows.ViewModels
{
    public sealed class WholesaleInvoiceViewModel : ScenarioInvoiceViewModel
    {
        public WholesaleInvoiceViewModel()
            : base(InvoiceEditorScenario.Wholesale, InvoiceCounterpartySelection.Customer,
                  InvoiceEditorLayout.DetailedInvoice)
        {
        }
    }
}
