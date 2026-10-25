using System;
using System.Linq;

namespace ASFuelControl.Windows.ViewModels
{
    public enum InvoiceEditorScenario
    {
        General = 0,
        Retail = 1,
        Wholesale = 2,
        Supplier = 3
    }

    public enum InvoiceEditorLayout
    {
        Legacy = 0,
        RetailReceipt = 1,
        DetailedInvoice = 2,
        SupplierPurchase = 3
    }

    public enum InvoiceCounterpartySelection
    {
        Any = 0,
        Customer = 1,
        Supplier = 2
    }

    public static class InvoiceEditorScenarioResolver
    {
        public static InvoiceEditorScenario Resolve(InvoiceTypeViewModel invoiceType)
        {
            if (invoiceType == null)
                return InvoiceEditorScenario.General;

            return Resolve(invoiceType.TransactionType, invoiceType.IsInternal == true,
                invoiceType.RetailInvoice == true);
        }

        public static InvoiceEditorScenario Resolve(int transactionType, bool isInternal,
            bool usesRetailPrices)
        {
            if (isInternal)
                return InvoiceEditorScenario.General;
            if (transactionType == (int)Common.Enumerators.TransactionTypeEnum.Delivery)
                return InvoiceEditorScenario.Supplier;
            if (transactionType != (int)Common.Enumerators.TransactionTypeEnum.Extraction)
                return InvoiceEditorScenario.General;
            return usesRetailPrices
                ? InvoiceEditorScenario.Retail
                : InvoiceEditorScenario.Wholesale;
        }

        public static bool Matches(InvoiceTypeViewModel invoiceType, InvoiceEditorScenario scenario)
        {
            return scenario == InvoiceEditorScenario.General || Resolve(invoiceType) == scenario;
        }
    }

    public static class InvoiceEditorLayoutResolver
    {
        public static bool IsCompact(InvoiceTypeViewModel invoiceType)
        {
            if (invoiceType == null)
                return false;

            // This is the static equivalent of the old ReportPrint/SimplePrint
            // decision.  It is evaluated before a form is opened and never
            // changes the controls of an already open editor.
            return invoiceType.Printable &&
                (invoiceType.DispenserType == true || invoiceType.IsLaserPrint != true);
        }

        public static bool Matches(InvoiceTypeViewModel invoiceType, InvoiceEditorLayout layout)
        {
            if (layout == InvoiceEditorLayout.Legacy || layout == InvoiceEditorLayout.SupplierPurchase)
                return true;
            return layout == InvoiceEditorLayout.RetailReceipt
                ? IsCompact(invoiceType)
                : !IsCompact(invoiceType);
        }
    }

    public abstract class ScenarioInvoiceViewModel : InvoiceDetailsViewModel
    {
        protected ScenarioInvoiceViewModel(InvoiceEditorScenario scenario,
            InvoiceCounterpartySelection counterpartySelection,
            InvoiceEditorLayout editorLayout)
        {
            EditorScenario = scenario;
            CounterpartySelection = counterpartySelection;
            EditorLayout = editorLayout;
            InvoiceTypes = InvoiceTypes
                .Where(invoiceType => InvoiceEditorScenarioResolver.Matches(invoiceType, scenario) &&
                    InvoiceEditorLayoutResolver.Matches(invoiceType, editorLayout))
                .ToArray();
        }

        public override InvoiceEditorScenario EditorScenario { get; }
        public override InvoiceCounterpartySelection CounterpartySelection { get; }
        public InvoiceEditorLayout EditorLayout { get; private set; }
    }

}
