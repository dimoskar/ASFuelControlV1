using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
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
                myDieselPanel = new MyDieselCouponPanel(EnsureInvoiceSavedForMyDiesel,InvoiceTraderVat,
                    InvoiceMyDieselEligibilityProblem,RefreshInvoiceValuesAfterMyDiesel);
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

        private bool EnsureInvoiceSavedForMyDiesel()
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
            try
            {
                if (ivm.HasChanges && !ivm.Save(ivm.InvoiceId))
                {
                    Telerik.WinControls.RadMessageBox.Show(string.Join("\r\n",ivm.Errors.Select(error => error.ErrorMessage)),
                        "Σφάλμα Αποθήκευσης",MessageBoxButtons.OK,Telerik.WinControls.RadMessageIcon.Exclamation);
                    return false;
                }

                // The scanner's "Έλεγχος και δέσμευση" button reaches this
                // method directly; the operator does not press the invoice Save
                // button first. Preserve the selected customer across legacy
                // pump-sale save paths that create and reload the invoice model.
                using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
                {
                    var invoice = db.Invoices.SingleOrDefault(value => value.InvoiceId == ivm.InvoiceId);
                    if (invoice == null)
                        throw new InvalidOperationException("Δεν δημιουργήθηκε το παραστατικό για τη δέσμευση myDiesel.");
                    if (invoice.TraderId != selectedTraderId)
                    {
                        invoice.TraderId = selectedTraderId;
                        db.SaveChanges();
                    }
                }
                fiscalPanel.LoadInvoice(ivm.InvoiceId);
                return true;
            }
            catch (Exception ex)
            {
                Telerik.WinControls.RadMessageBox.Show(ex.Message,"Σφάλμα Αποθήκευσης myDiesel",
                    MessageBoxButtons.OK,Telerik.WinControls.RadMessageIcon.Exclamation);
                return false;
            }
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
            if (!ivm.HasChanges && ivm.InvoiceId != Guid.Empty)
                return Threads.SharedMyDieselRuntime.InvoiceEligibilityProblem(ivm.InvoiceId);

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
    }
}
