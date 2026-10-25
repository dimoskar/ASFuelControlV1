namespace ASFuelControl.Windows.ViewModels
{
    public sealed class SupplierInvoiceViewModel : ScenarioInvoiceViewModel
    {
        public SupplierInvoiceViewModel()
            : base(InvoiceEditorScenario.Supplier, InvoiceCounterpartySelection.Supplier,
                  InvoiceEditorLayout.SupplierPurchase)
        {
        }
    }
}
