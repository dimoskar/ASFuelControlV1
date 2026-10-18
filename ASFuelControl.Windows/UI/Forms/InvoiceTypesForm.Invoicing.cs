using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Exedron.Invoicing.Core;
using Telerik.WinControls.UI;

namespace ASFuelControl.Windows.UI.Forms
{
    public partial class InvoiceTypesForm
    {
        private Dictionary<Guid,InvoiceTypePolicy> policies;
        private readonly HashSet<Guid> changedPolicies = new HashSet<Guid>();
        private RadDropDownList transmission;
        private RadCheckBox fiscalSigning;
        private bool loadingPolicy;

        private void InitializeInvoicingPolicy()
        {
            policies = Threads.SharedInvoicingSettings.Store().LoadPolicies();
            radCheckBox14.DataBindings.Clear();
            tableLayoutPanel1.Controls.Remove(radCheckBox14);
            radCheckBox14.Dispose();
            radLabel25.Text = "Τρόπος διαβίβασης";
            transmission = new RadDropDownList { Dock = DockStyle.Fill, DropDownStyle = Telerik.WinControls.RadDropDownStyle.DropDownList };
            transmission.Items.Add("Χωρίς διαβίβαση");
            transmission.Items.Add("Πάροχος τιμολόγησης");
            transmission.Items.Add("Απευθείας myDATA");
            tableLayoutPanel1.Controls.Add(transmission,1,13);
            tableLayoutPanel1.SetColumnSpan(transmission,3);
            fiscalSigning = new RadCheckBox { Text = "Σήμανση από φορολογικό μηχανισμό", Dock = DockStyle.Fill };
            tableLayoutPanel1.Controls.Add(fiscalSigning,0,14);
            tableLayoutPanel1.SetColumnSpan(fiscalSigning,4);
            invoiceTypeViewModelBindingSource.CurrentChanged += (s,e) => ShowPolicy();
            transmission.SelectedIndexChanged += (s,e) => ChangePolicy();
            fiscalSigning.ToggleStateChanged += (s,e) => ChangePolicy();
            ShowPolicy();
        }
        private void ShowPolicy()
        {
            loadingPolicy = true;
            try
            {
                var type = invoiceTypeViewModelBindingSource.Current as ViewModels.InvoiceTypeDetailViewModel;
                transmission.Enabled = fiscalSigning.Enabled = type != null;
                InvoiceTypePolicy policy;
                bool reviewed = type != null && policies.TryGetValue(type.InvoiceTypeId,out policy);
                policy = type == null ? null : policies.ContainsKey(type.InvoiceTypeId) ? policies[type.InvoiceTypeId] : null;
                // Missing policy remains visibly unselected; legacy SendToMyData is ambiguous.
                transmission.SelectedIndex = policy == null ? -1 : (int)policy.Transmission;
                fiscalSigning.IsChecked = policy?.SignWithFiscalDevice ?? false;
                fiscalSigning.Enabled = type != null && transmission.SelectedIndex != (int)InvoiceTransmissionMode.Provider;
                fiscalSigning.Text = transmission.SelectedIndex == (int)InvoiceTransmissionMode.Provider ? "Η σήμανση παρέχεται από τον πάροχο" : "Σήμανση από φορολογικό μηχανισμό";
                radLabel25.Text = reviewed ? "Τρόπος διαβίβασης" : "Διαβίβαση (επιλέξτε)";
            }
            finally { loadingPolicy = false; }
        }
        private void ChangePolicy()
        {
            if (loadingPolicy || transmission.SelectedIndex < 0) return;
            var type = invoiceTypeViewModelBindingSource.Current as ViewModels.InvoiceTypeDetailViewModel;
            if (type == null) return;
            InvoiceTypePolicy policy;
            if (!policies.TryGetValue(type.InvoiceTypeId,out policy)) policies[type.InvoiceTypeId] = policy = new InvoiceTypePolicy { InvoiceTypeId = type.InvoiceTypeId };
            policy.Transmission = (InvoiceTransmissionMode)transmission.SelectedIndex;
            bool providerSigning = policy.Transmission == InvoiceTransmissionMode.Provider;
            loadingPolicy = true;
            try
            {
                if (providerSigning) fiscalSigning.IsChecked = false;
                fiscalSigning.Enabled = !providerSigning;
                fiscalSigning.Text = providerSigning ? "Η σήμανση παρέχεται από τον πάροχο" : "Σήμανση από φορολογικό μηχανισμό";
            }
            finally { loadingPolicy = false; }
            policy.SignWithFiscalDevice = fiscalSigning.IsChecked;
            changedPolicies.Add(type.InvoiceTypeId);
            vm.HasChanges = true;
            radLabel25.Text = "Τρόπος διαβίβασης";
        }
        private void SaveInvoicingPolicies()
        {
            foreach (var id in changedPolicies) policies[id].Validate();
            long revision;
            var config = Threads.SharedInvoicingSettings.Load(out revision);
            if (config.Enabled && config.Version == 2)
            {
                config.Policies = policies;
                foreach (var id in changedPolicies) config.Route(id,null);
            }
            vm.SaveChanges();
            Threads.SharedInvoicingSettings.Store().SavePolicies(changedPolicies.Select(id => policies[id]));
            changedPolicies.Clear();
            vm.HasChanges = false;
            Telerik.WinControls.RadMessageBox.Show("Αποθηκεύτηκε. Οι αλλαγές της κοινής τιμολόγησης ισχύουν μετά από επανεκκίνηση της εφαρμογής.","Τύποι παραστατικών");
        }
    }
}
