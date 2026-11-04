namespace ASFuelControl.Windows.ViewModels
{
    /// <summary>
    /// VAT-exclusive counterpart of RetailReceiptViewModel. The shared compact
    /// layout is appropriate for a single fuel line, while the wholesale
    /// scenario keeps prices and invoice-type filtering VAT-exclusive.
    /// </summary>
    public sealed class CompactWholesaleInvoiceViewModel : ScenarioInvoiceViewModel
    {
        public CompactWholesaleInvoiceViewModel()
            : base(InvoiceEditorScenario.Wholesale,
                  InvoiceCounterpartySelection.Customer,
                  InvoiceEditorLayout.RetailReceipt)
        {
        }
    }
}
