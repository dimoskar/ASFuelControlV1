using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ASFuelControl.Invoicing.Persistence;
using ASFuelControl.Windows.UI.Controls;
using Exedron.Invoicing.Core;
using Telerik.WinControls.UI;

namespace ASFuelControl.Windows.UI.Forms
{
    public partial class InvoiceFormEx
    {
        private FiscalDocumentPanel fiscalPanel;
        private MyDieselCouponPanel myDieselPanel;
        private bool myDieselVisible;
        private bool fiscalStatusVisible;
        private Panel invoicingStatusHost;
        private readonly Dictionary<Control,bool> editableControls = new Dictionary<Control,bool>();
        private bool lockedForIssuance;
        private bool myDieselDiscountLocked;
        private bool discountEditorEnabledBeforeMyDiesel;
        private Control fiscalPrimaryAction;
        private void InitializeFiscalStatus()
        {
            fiscalPanel = new FiscalDocumentPanel { Dock = DockStyle.Fill };
            fiscalStatusVisible = editorLayout != ViewModels.InvoiceEditorLayout.SupplierPurchase;
            if (!fiscalStatusVisible)
                Disposed += (s,e) => fiscalPanel.Dispose();
            myDieselVisible = fiscalStatusVisible && Data.Implementation.OptionHandler.Instance.MyDieselEnabled;
            if (fiscalStatusVisible)
                myDieselPanel = new MyDieselCouponPanel(PrepareInvoiceForMyDiesel,
                    InvoiceTraderVat, InvoiceMyDieselEligibilityProblem,
                    ApplyMyDieselDiscount, ClearMyDieselDiscount,
                    RefreshInvoiceValuesAfterMyDiesel);
            bool fixedEditor = editorLayout != ViewModels.InvoiceEditorLayout.Legacy;
            if (fiscalStatusVisible)
            {
                if (fixedEditor)
                {
                    invoicingStatusHost = fixedInvoicingStatusHost;
                }
                else
                {
                    invoicingStatusHost = new Panel
                    {
                        Dock = DockStyle.Bottom,
                        Height = 210
                    };
                }
                invoicingStatusHost.Controls.Add(fiscalPanel);
                if (myDieselVisible)
                    fiscalPanel.AddPage("myDiesel",myDieselPanel);
                if (fixedEditor && fixedFiscalPrimaryActionHost != null)
                {
                    fiscalPanel.AttachPrimaryAction(fixedFiscalPrimaryActionHost);
                    fiscalPrimaryAction = fiscalPanel.PrimaryAction;
                }
                if (!fixedEditor)
                {
                    ClientSize = new System.Drawing.Size(ClientSize.Width,
                        ClientSize.Height + invoicingStatusHost.Height);
                    Controls.Add(invoicingStatusHost);
                }
            }
            var signatureBinding = radLabel13 == null ? null : radLabel13.DataBindings["Text"];
            if (signatureBinding != null)
                signatureBinding.DataSourceUpdateMode = DataSourceUpdateMode.Never;
            fiscalPanel.StateChanged += (s,e) => {
                ApplyInvoiceSignature();
                LockIssuedInvoice();
            };
            if (myDieselVisible) myDieselPanel.StateChanged += (s,e) => {
                ApplyMyDieselDiscountLock();
                LockIssuedInvoice();
            };
            if (myDieselVisible)
                invoiceLinesBindingSource.ListChanged += (s,e) => myDieselPanel.RefreshEligibility();
            Shown += (s,e) =>
            {
                if (fiscalStatusVisible) fiscalPanel.LoadInvoice(ivm.InvoiceId);
                if (myDieselVisible) myDieselPanel.LoadInvoice(ivm.InvoiceId, ivm.EntityState == ViewModels.EntityStateEnum.Added);
            };
            paymentTypeDropDownList.SelectedIndexChanging += (s,e) => {
                if (!Visible || lockedForIssuance || e.Position < 0 || e.Position >= paymentTypeDropDownList.Items.Count) return;
                var value = paymentTypeDropDownList.Items[e.Position].Value;
                if (value == null || Convert.ToInt32(value) != 4) return;
                try
                {
                    if (!Threads.SharedInvoiceRuntime.Enabled || Threads.SharedInvoiceRuntime.SupportsIris) return;
                    e.Cancel = true;
                    Telerik.WinControls.RadMessageBox.Show("IRIS μέσω GBridge υποστηρίζεται μόνο με iTimologia και τρόπο POS Mellon.","Πληρωμή IRIS");
                }
                catch (Exception ex) { e.Cancel = true; Telerik.WinControls.RadMessageBox.Show(ex.Message,"Έλεγχος πληρωμής"); }
            };
        }

        private void ApplyInvoiceSignature()
        {
            if (radLabel13 == null) return;
            var value = string.IsNullOrWhiteSpace(ivm?.InvoiceSignature)
                ? fiscalPanel?.InvoiceSignature
                : ivm.InvoiceSignature;
            radLabel13.Text = value ?? string.Empty;
        }

        private void RefreshInvoiceValuesAfterMyDiesel()
        {
            bool invoicePersisted = InvoiceExists(ivm.InvoiceId);
            if (!invoicePersisted)
            {
                ResetEditorBindings();
                ApplyPriceBindings();
                SetView();
                ApplyMyDieselDiscountLock();
                return;
            }

            ExecuteWithInvoiceTypeBindingReadOnly(() => ivm.Load(ivm.InvoiceId));
            var loadedInvoiceTypeId = ivm.InvoiceTypeId;
            ResetEditorBindings();
            ApplyPriceBindings();
            SetView();
            ScheduleInvoiceTypeValidation(loadedInvoiceTypeId, false);
            ApplyMyDieselDiscountLock();
        }

        private void ApplyMyDieselDiscountLock()
        {
            if (!myDieselVisible || myDieselPanel == null) return;
            bool locked = myDieselPanel.DiscountLocked;
            if (radGridView1 != null && radGridView1.Columns["DiscountPercentage"] != null)
                radGridView1.Columns["DiscountPercentage"].ReadOnly = locked;
            if (radSpinEditor3 == null || locked == myDieselDiscountLocked) return;
            if (locked)
            {
                discountEditorEnabledBeforeMyDiesel = radSpinEditor3.Enabled;
                radSpinEditor3.Enabled = false;
            }
            else if (!lockedForIssuance)
                radSpinEditor3.Enabled = discountEditorEnabledBeforeMyDiesel;
            myDieselDiscountLocked = locked;
        }
        private void LockIssuedInvoice()
        {
            btnPrint.Enabled = invoiceTypeBindingValid && !fiscalPanel.Busy &&
                (!myDieselVisible || !myDieselPanel.Busy) && !fiscalPanel.ReadFailed;
            bool locked = fiscalPanel.Owned || fiscalPanel.ReadFailed || fiscalPanel.Busy ||
                (myDieselVisible && myDieselPanel.Busy);
            if (locked == lockedForIssuance) return;
            lockedForIssuance = locked;
            if (locked)
            {
                editableControls.Clear();
                // Keep the original Print button available while freezing all invoice
                // fields, including the other controls in its toolbar.
                foreach (Control control in Controls) LockInvoiceControl(control);
            }
            else foreach (var entry in editableControls) entry.Key.Enabled = entry.Value;
            if (!invoiceTypeBindingValid)
                DisableInvoiceActions();
        }
        private void LockInvoiceControl(Control control)
        {
            if (control == fiscalPanel || control == myDieselPanel ||
                control == invoicingStatusHost || control == btnPrint ||
                control == fiscalPrimaryAction) return;
            bool pageNavigation = control is RadPageView || control is RadPageViewPage;
            bool containsPrintButton = control.Contains(btnPrint);
            bool containsFiscalPanel = fiscalPanel != null && control.Contains(fiscalPanel);
            bool containsFiscalPrimaryAction = fiscalPrimaryAction != null &&
                control.Contains(fiscalPrimaryAction);
            if (pageNavigation || containsPrintButton || containsFiscalPanel ||
                containsFiscalPrimaryAction)
            {
                foreach (Control child in control.Controls) LockInvoiceControl(child);
                return;
            }
            editableControls.Add(control,control.Enabled);
            control.Enabled = false;
        }

        private bool PrepareInvoiceForMyDiesel()
        {
            if (ivm.InvoiceId == Guid.Empty) return false;
            // The voucher dialog is opened while a Telerik grid editor may still
            // own the displayed fuel/volume values. Commit that editor before
            // reading the view model; otherwise the legacy sale creator receives
            // the previous defaults (for example 0 litres and another fuel).
            CommitEditorValues();

            string draftProblem = InvoiceMyDieselEligibilityProblem();
            if (draftProblem != null)
            {
                Telerik.WinControls.RadMessageBox.Show(draftProblem,"myDiesel",
                    MessageBoxButtons.OK,Telerik.WinControls.RadMessageIcon.Exclamation);
                return false;
            }

            Guid? selectedTraderId = ivm.TraderId;
            if (!selectedTraderId.HasValue || selectedTraderId.Value == Guid.Empty)
            {
                Telerik.WinControls.RadMessageBox.Show("Επιλέξτε πελάτη πριν χρησιμοποιήσετε κουπόνι myDiesel.",
                    "myDiesel",MessageBoxButtons.OK,Telerik.WinControls.RadMessageIcon.Exclamation);
                return false;
            }
            return true;
        }

        private void ApplyMyDieselDiscount(MyDieselCouponCaseRecord coupon)
        {
            if (coupon == null || !coupon.IssuedLitres.HasValue ||
                coupon.IssuedLitres.Value <= 0)
            {
                throw new InvalidOperationException(
                    "Δεν επιστράφηκαν έγκυρα λίτρα κουπονιού.");
            }

            bool invoicePersisted = InvoiceExists(ivm.InvoiceId);
            if (invoicePersisted)
            {
                Threads.PendingMyDieselSelectionStore.ApplyExciseDiscount(
                    ivm.InvoiceId, coupon.IssuedLitres.Value,
                    coupon.ExciseDiscountPerLitre);
                return;
            }

            decimal remainingLitres = Math.Min(coupon.IssuedLitres.Value,
                ivm.InvoiceLines.Sum(line => Math.Abs(line.Volume)));
            foreach (ViewModels.InvoiceLineViewModel line in ivm.InvoiceLines)
            {
                decimal originalTotal = Math.Abs(line.TotalPrice);
                if (originalTotal <= 0)
                {
                    decimal retailUnitPrice = line.UnitPriceRetail != 0
                        ? Math.Abs(line.UnitPriceRetail)
                        : Math.Abs(line.UnitPrice);
                    originalTotal = Math.Round(Math.Abs(line.Volume) *
                        retailUnitPrice, 2);
                }
                decimal applicableLitres = Math.Min(Math.Abs(line.Volume),
                    remainingLitres);
                decimal retailDiscount = applicableLitres <= 0
                    ? 0
                    : MyDieselTaxRules.CalculateVatInclusiveExciseDiscount(
                        applicableLitres, applicableLitres,
                        coupon.ExciseDiscountPerLitre, line.VatPercentage);
                if (retailDiscount > originalTotal)
                {
                    retailDiscount = originalTotal;
                }
                decimal wholesaleDiscount =
                    MyDieselTaxRules.CalculateNetDiscountFromGross(
                        retailDiscount, line.VatPercentage);
                decimal discountPercentage = originalTotal == 0
                    ? 0
                    : Math.Round(100 * retailDiscount / originalTotal, 4);
                decimal discountedTotal = Math.Round(originalTotal -
                    retailDiscount, 2);
                decimal vatMultiplier = 1 + line.VatPercentage / 100;
                decimal vatAmount = Math.Round(discountedTotal -
                    discountedTotal / vatMultiplier, 2);

                line.DiscountAmountRetail = retailDiscount;
                line.DiscountAmountWhole = wholesaleDiscount;
                line.DiscountAmount = retailDiscount;
                line.DiscountPercentage = discountPercentage;
                line.TotalPrice = discountedTotal;
                line.VatAmount = vatAmount;
                if (line.SalesTransactionView != null)
                {
                    line.SalesTransactionView.TotalPrice = discountedTotal;
                    line.SalesTransactionView.DiscountPercentage =
                        discountPercentage;
                }
                remainingLitres -= applicableLitres;
            }
            ivm.RecalculateSums();
        }

        private void ClearMyDieselDiscount()
        {
            bool invoicePersisted = InvoiceExists(ivm.InvoiceId);
            if (invoicePersisted)
            {
                Threads.PendingMyDieselSelectionStore.ClearExciseDiscount(
                    ivm.InvoiceId);
                return;
            }

            foreach (ViewModels.InvoiceLineViewModel line in ivm.InvoiceLines)
            {
                decimal restoredTotal = Math.Round(line.TotalPrice +
                    Math.Abs(line.DiscountAmountRetail), 2);
                decimal retailUnitPrice = line.UnitPriceRetail != 0
                    ? Math.Abs(line.UnitPriceRetail)
                    : Math.Abs(line.UnitPrice);
                decimal expectedTotal = Math.Round(Math.Abs(line.Volume) *
                    retailUnitPrice, 2);
                if (expectedTotal > 0)
                {
                    restoredTotal = expectedTotal;
                }

                line.DiscountAmountRetail = 0;
                line.DiscountAmountWhole = 0;
                line.DiscountAmount = 0;
                line.DiscountPercentage = 0;
                line.TotalPrice = restoredTotal;
                decimal vatMultiplier = 1 + line.VatPercentage / 100;
                line.VatAmount = Math.Round(restoredTotal -
                    restoredTotal / vatMultiplier, 2);
                if (line.SalesTransactionView != null)
                {
                    line.SalesTransactionView.TotalPrice = restoredTotal;
                    line.SalesTransactionView.DiscountPercentage = 0;
                }
            }
            ivm.RecalculateSums();
        }

        private string InvoiceTraderVat()
        {
            using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                Guid? traderId = ivm.TraderId;
                // While the editor has pending changes, the view model is the user's
                // authoritative selection. After Save, use the persisted invoice instead:
                // disabling/refreshing bound controls can transiently clear TraderId on the
                // view model even though the invoice was saved with its trader intact.
                if (!ivm.HasChanges && ivm.InvoiceId != Guid.Empty)
                {
                    var invoice = db.Invoices.SingleOrDefault(value => value.InvoiceId == ivm.InvoiceId);
                    if (invoice != null) traderId = invoice.TraderId;
                }
                if (!traderId.HasValue) return null;
                var trader = db.Traders.SingleOrDefault(value => value.TraderId == traderId.Value);
                return trader == null ? null : trader.TaxRegistrationNumber;
            }
        }

        private string InvoiceMyDieselEligibilityProblem()
        {
            // Once saved, the database row is authoritative. This also avoids a
            // transiently cleared bound TraderId while controls are refreshed.
            bool invoicePersisted = ivm.InvoiceId != Guid.Empty && InvoiceExists(ivm.InvoiceId);
            if (!ivm.HasChanges && invoicePersisted)
                return Threads.SharedMyDieselRuntime.InvoiceEligibilityProblem(ivm.InvoiceId);

            string routeProblem = Threads.SharedMyDieselRuntime.InvoiceTypeEligibilityProblem(ivm.InvoiceTypeId);
            if (routeProblem != null)
            {
                return routeProblem;
            }

            if (!ivm.TraderId.HasValue)
                return "Επιλέξτε πελάτη πριν χρησιμοποιήσετε κουπόνι myDiesel.";

            var lines = ivm.InvoiceLines;
            if (lines.Length == 0 || lines.Any(value => value.Volume <= 0))
                return "Το myDiesel απαιτεί θετική ποσότητα λίτρων.";

            var availableFuels = ivm.FuelTypes ?? new ViewModels.FuelTypeViewModel[0];
            var fuelCodes = lines.Select(line => availableFuels
                .Where(fuel => fuel.FuelTypeId == line.FuelTypeId)
                .Select(fuel => (int?)fuel.EnumeratorValue)
                .FirstOrDefault()).ToArray();
            if (fuelCodes.Any(value => !value.HasValue || !MyDieselFuelRules.IsEligible(value.Value)))
                return "Το επιλεγμένο καύσιμο δεν είναι επιλέξιμο για myDiesel.";
            if (fuelCodes.Select(value => value.Value).Distinct().Count() != 1)
                return "Το παραστατικό πρέπει να περιέχει ένα μόνο επιλέξιμο καύσιμο myDiesel.";
            return null;
        }

        private static bool InvoiceExists(Guid invoiceId)
        {
            using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                bool invoiceExists = db.Invoices.Any(value => value.InvoiceId == invoiceId);
                return invoiceExists;
            }
        }
    }
}
