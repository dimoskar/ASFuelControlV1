using System;
using System.Linq;
using System.Windows.Forms;

namespace ASFuelControl.Windows.UI.Forms
{
    public static class InvoiceEditorFormFactory
    {
        public static InvoiceFormEx SelectForNewInvoice(IWin32Window owner)
        {
            using (var selection = new SelectionForms.InvoiceTypeSelection())
            {
                selection.FilterAdminView = true;
                selection.HideCancelationTypes = true;
                if (selection.ShowDialog(owner) != DialogResult.OK)
                    return null;

                var invoiceType = selection.SelectedEntity as ViewModels.InvoiceTypeViewModel;
                if (invoiceType == null)
                    return null;

                var form = CreateForInvoiceType(invoiceType.InvoiceTypeId);
                form.CreateNew(invoiceType.InvoiceTypeId);
                return form;
            }
        }

        public static InvoiceFormEx CreateForInvoice(Guid invoiceId)
        {
            using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                var invoiceTypeId = db.Invoices
                    .Where(invoice => invoice.InvoiceId == invoiceId)
                    .Select(invoice => invoice.InvoiceTypeId)
                    .SingleOrDefault();
                return CreateForInvoiceType(db, invoiceTypeId);
            }
        }

        public static InvoiceFormEx OpenInvoice(Guid invoiceId)
        {
            var form = CreateForInvoice(invoiceId);
            try
            {
                form.LoadInvoice(invoiceId);
                return form;
            }
            catch
            {
                form.Dispose();
                throw;
            }
        }

        public static InvoiceFormEx CreateForInvoiceType(Guid invoiceTypeId)
        {
            using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                return CreateForInvoiceType(db, invoiceTypeId);
            }
        }

        private static InvoiceFormEx CreateForInvoiceType(Data.DatabaseModel db,
            Guid invoiceTypeId)
        {
            var type = db.InvoiceTypes.SingleOrDefault(
                invoiceType => invoiceType.InvoiceTypeId == invoiceTypeId);
            return type == null ? Create(ViewModels.InvoiceEditorScenario.General) :
                CreateForType(type.TransactionType, type.IsInternal == true,
                    type.RetailInvoice == true, type.Printable,
                    type.DispenserType == true, type.IsLaserPrint);
        }

        private static InvoiceFormEx CreateForType(int transactionType, bool isInternal,
            bool usesRetailPrices, bool printable, bool dispenserType, bool? isLaserPrint)
        {
            var scenario = ViewModels.InvoiceEditorScenarioResolver.Resolve(
                transactionType, isInternal, usesRetailPrices);
            bool compact = printable && (dispenserType || isLaserPrint != true);
            if (scenario == ViewModels.InvoiceEditorScenario.Retail && compact)
                return new RetailReceiptForm();
            if (scenario == ViewModels.InvoiceEditorScenario.Wholesale && compact)
                return new CompactWholesaleInvoiceForm();
            return Create(scenario);
        }

        public static InvoiceFormEx Create(ViewModels.InvoiceEditorScenario scenario)
        {
            switch (scenario)
            {
                case ViewModels.InvoiceEditorScenario.Retail:
                    return new RetailInvoiceForm();
                case ViewModels.InvoiceEditorScenario.Wholesale:
                    return new WholesaleInvoiceForm();
                case ViewModels.InvoiceEditorScenario.Supplier:
                    return new SupplierInvoiceForm();
                default:
                    return new InvoiceFormEx();
            }
        }
    }
}
