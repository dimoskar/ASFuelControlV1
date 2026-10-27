using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ASFuelControl.Windows.UI.Controls;

namespace ASFuelControl.Windows.UI.Forms
{
    public partial class InvoiceFormEx
    {
        private FiscalDocumentPanel fiscalPanel;
        private MyDieselCouponPanel myDieselPanel;
        private bool myDieselVisible;
        private Panel invoicingStatusHost;
        private readonly Dictionary<Control,bool> editableControls = new Dictionary<Control,bool>();
        private bool lockedForIssuance;
        private void InitializeFiscalStatus()
        {
            fiscalPanel = new FiscalDocumentPanel { Dock = DockStyle.Fill };
            myDieselPanel = new MyDieselCouponPanel(EnsureInvoiceSavedForMyDiesel,InvoiceTraderVat);
            myDieselVisible = Data.Implementation.OptionHandler.Instance.MyDieselEnabled;
            bool fixedEditor = editorLayout != ViewModels.InvoiceEditorLayout.Legacy;
            invoicingStatusHost = new Panel { Dock = DockStyle.Bottom,Height = fixedEditor ? 260 : (myDieselVisible ? 342 : 210) };
            invoicingStatusHost.Controls.Add(fiscalPanel);
            if (myDieselVisible && fixedEditor)
                fiscalPanel.AddPage("myDiesel",myDieselPanel);
            else if (myDieselVisible)
            {
                myDieselPanel.Dock = DockStyle.Top;
                invoicingStatusHost.Controls.Add(myDieselPanel);
            }
            ClientSize = new System.Drawing.Size(ClientSize.Width,ClientSize.Height + invoicingStatusHost.Height);
            Controls.Add(invoicingStatusHost);
            fiscalPanel.StateChanged += (s,e) => LockIssuedInvoice();
            if (myDieselVisible) myDieselPanel.StateChanged += (s,e) => LockIssuedInvoice();
            Shown += (s,e) =>
            {
                fiscalPanel.LoadInvoice(ivm.InvoiceId);
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
        private void LockIssuedInvoice()
        {
            btnPrint.Enabled = !fiscalPanel.Busy && (!myDieselVisible || !myDieselPanel.Busy) && !fiscalPanel.ReadFailed;
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
        }
        private void LockInvoiceControl(Control control)
        {
            if (control == fiscalPanel || control == myDieselPanel || control == invoicingStatusHost || control == btnPrint) return;
            if (control.Contains(btnPrint))
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
            if (!ivm.HasChanges) return true;
            if (ivm.Save(ivm.InvoiceId))
            {
                fiscalPanel.LoadInvoice(ivm.InvoiceId);
                return true;
            }
            Telerik.WinControls.RadMessageBox.Show(string.Join("\r\n",ivm.Errors.Select(error => error.ErrorMessage)),
                "Σφάλμα Αποθήκευσης",MessageBoxButtons.OK,Telerik.WinControls.RadMessageIcon.Exclamation);
            return false;
        }

        private string InvoiceTraderVat()
        {
            if (!ivm.TraderId.HasValue) return null;
            using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                var trader = db.Traders.SingleOrDefault(value => value.TraderId == ivm.TraderId.Value);
                return trader == null ? null : trader.TaxRegistrationNumber;
            }
        }
    }
}
