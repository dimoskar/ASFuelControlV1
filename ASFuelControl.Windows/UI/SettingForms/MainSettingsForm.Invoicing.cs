using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ASFuelControl.Invoicing;
using ASFuelControl.Invoicing.Mapping;
using ASFuelControl.Invoicing.Persistence;
using ASFuelControl.Windows.Threads;
using ASFuelControl.Windows.UI.Controls;
using Exedron.Invoicing.Adapters;
using Exedron.Invoicing.Core;
using Newtonsoft.Json;
using Telerik.WinControls;
using Telerik.WinControls.UI;

namespace ASFuelControl.Windows.UI.SettingForms
{
    public partial class MainSettingsForm
    {
        private SharedInvoiceConfiguration invoicing;
        private long invoicingRevision;
        private RadCheckBox sharedEnabled, sharedPosEnabled;
        private RadDropDownList sharedEnvironment, sharedProvider, sharedPosMode;
        private RadDateTimePicker sharedCutover;
        private RadTextBox sharedMyDataUser, sharedMyDataKey, sharedMyDataUrl;
        private RadTextBox sharedIlydaAccount, sharedIlydaPassword, sharedITimologiaAccount, sharedITimologiaToken;
        private RadTextBox sharedGateway, sharedTerminal, sharedWholesale;
        private RadLabel providerEnvironment, providerCode, providerLicence;
        private RadCheckBox myDieselEnabled;
        private RadTextBox myDieselAadeUser, myDieselSecret;
        private RadDropDownList myDieselCompany, myDieselStation;
        private RadSpinEditor myDieselExciseRate, myDieselPoll;
        private RadLabel myDieselReferenceStatus;
        private RadDropDownList myDieselEnvironment;
        private InvoicingFields providerFields, myDieselFields;
        private DateTime? myDieselReferenceCheckedUtc;
        private string originalMyDataKey;
        private readonly Dictionary<string,string> pendingSecrets = new Dictionary<string,string>();

        private void InitializeSharedInvoicing()
        {
            invoicing = SharedInvoicingSettings.Load(out invoicingRevision);
            var legacy = new RadPageViewPage { Text = "Παλαιές συνδέσεις" };
            var legacyTabs = new RadPageView { Dock = DockStyle.Fill };
            legacy.Controls.Add(legacyTabs);
            var legacyMyData = new RadPageViewPage { Text = "myDATA (παλαιό)" };
            var legacyProvider = new RadPageViewPage { Text = "Πάροχος (παλαιό)" };
            var legacyPos = new RadPageViewPage { Text = "POS (παλαιό)" };
            legacyTabs.Pages.Add(legacyMyData); legacyTabs.Pages.Add(legacyProvider); legacyTabs.Pages.Add(legacyPos);
            legacyMyData.Controls.Add(radGroupBox2); radGroupBox2.Dock = DockStyle.Fill;
            legacyProvider.Controls.Add(radGroupBox4); radGroupBox4.Dock = DockStyle.Fill;
            foreach (var control in new Control[] { groupBox1,groupBox2,posTypeCombo,radLabel55,posTerminalIdTxt,posTerminalId }) legacyPos.Controls.Add(control);

            // Upgrade starts disabled until the per-type review and cutover check succeed.
            var common = new InvoicingFields("Κοινές ρυθμίσεις τιμολόγησης / myDATA");
            common.Group.Dock = DockStyle.None; common.Group.SetBounds(8,8,radPageViewPage4.Width-16,326);
            common.Group.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            radPageViewPage4.Controls.Add(common.Group);
            radGroupBox3.SetBounds(8,340,radPageViewPage4.Width-16,112);
            sharedEnabled = common.Row("Κοινή τιμολόγηση",new RadCheckBox { Text = "Ενεργή μετά την επανεκκίνηση", IsChecked = invoicing.Version >= 2 && invoicing.Enabled });
            sharedEnvironment = common.Row("Περιβάλλον",InvoicingFields.Choice("Δοκιμαστικό","Παραγωγή"));
            sharedEnvironment.SelectedIndex = invoicing.TestMode ? 0 : 1;
            sharedCutover = common.Row("Έναρξη νέας ροής",new RadDateTimePicker { Value = invoicing.FirstInvoiceDate, Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy HH:mm" });
            sharedMyDataUser = common.Row("Χρήστης myDATA",InvoicingFields.Text(invoicing.DirectMyData?.Account));
            originalMyDataKey = SavedMyDataSubscriptionKey();
            sharedMyDataKey = common.Row(!string.IsNullOrWhiteSpace(originalMyDataKey) ? "Subscription key (αποθηκευμένο)" : "Subscription key",InvoicingFields.Text(originalMyDataKey,true));
            sharedMyDataUrl = common.Row("Endpoint myDATA",InvoicingFields.Text(invoicing.DirectMyData?.MyDataBaseUrl ?? MyDataOptions.TestUrl));
            common.Row("Υποκατάστημα",branchSpinEditor);
            sharedWholesale = common.Row("Κωδικοί χονδρικής",InvoicingFields.Text(string.Join(",",invoicing.Mapping?.WholesaleCodes ?? new HashSet<int>())));
            common.Note("Μετά την πρώτη ενεργοποίηση, η απενεργοποίηση παύει τις νέες εκδόσεις.\r\nΔεν επαναφέρει την παλαιά σύνδεση. Το αποθηκευμένο κλειδί φορτώνεται κρυμμένο και μπορεί να αντικατασταθεί.");

            var provider = providerFields = new InvoicingFields("Κοινός πάροχος τιμολόγησης");
            radPageViewPage5.Text = "Πάροχος Τιμολόγησης"; radPageViewPage5.Controls.Add(provider.Group);
            sharedProvider = provider.Row("Πάροχος",InvoicingFields.Choice("Ilyda","iTimologia"));
            sharedProvider.SelectedIndex = (invoicing.SelectedProviderKey ?? invoicing.InvoiceProvider?.ProviderKey) == InvoiceProviderCatalog.ITimologiaKey ? 1 : 0;
            providerEnvironment = provider.Row("Κοινό περιβάλλον",new RadLabel());
            sharedIlydaAccount = provider.Row("Όνομα χρήστη Ilyda",InvoicingFields.Text(SavedProviderRoute(InvoiceProviderCatalog.DirectIlydaKey)?.Account));
            sharedIlydaPassword = provider.Row("Κωδικός πρόσβασης Ilyda",InvoicingFields.Text("",true));
            sharedITimologiaAccount = provider.Row("Συνδρομή iTimologia (ID)",InvoicingFields.Text(SavedProviderRoute(InvoiceProviderCatalog.ITimologiaKey)?.Account));
            sharedITimologiaToken = provider.Row("Master token",InvoicingFields.Text("",true));
            providerCode = provider.Row("Κωδικός παρόχου ΑΑΔΕ",new RadLabel());
            providerLicence = provider.Row("Άδεια ΥΠΑΗΕΣ",new RadLabel());
            provider.Note("Χρησιμοποιείται για τύπους με διαβίβαση σε πάροχο.\r\nΚενός κωδικός / token διατηρεί το αποθηκευμένο προφίλ.");
            var check = provider.Row("",new RadButton { Text = "Έλεγχος μετάβασης…" });
            check.Click += (s,e) => {
                try { RadMessageBox.Show(SharedInvoicingSettings.Preflight(BuildSharedInvoicing()),"Έλεγχος ρυθμίσεων"); }
                catch (Exception ex) { RadMessageBox.Show(ex.Message,"Έλεγχος ρυθμίσεων"); }
            };
            provider.Note("Λογαριασμός / κλειδιά: ισχύουν πριν από την πρώτη προσπάθεια.\r\nΟι υπόλοιπες αλλαγές απαιτούν επανεκκίνηση.");

            var pos = new InvoicingFields("Κοινές ρυθμίσεις POS");
            pos.Group.Dock = DockStyle.None; pos.Group.SetBounds(8,190,radPageViewPage3.Width-16,270);
            pos.Group.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            radPageViewPage3.Controls.Add(pos.Group);
            sharedPosEnabled = pos.Row("Πληρωμές POS",new RadCheckBox { Text = "Μέσω GBridge", IsChecked = invoicing.Pos != null });
            sharedPosMode = pos.Row("Τρόπος POS",InvoicingFields.Choice());
            sharedGateway = pos.Row("Endpoint GBridge",InvoicingFields.Text(invoicing.Pos?.Endpoint));
            sharedTerminal = pos.Row("Τερματικό (TID)",InvoicingFields.Text(invoicing.Pos?.TerminalId));
            pos.Note("Ο τρόπος POS καθορίζει αυτόματα την υπογραφή του παρόχου.\r\nΕπιλέξτε τρόπο πριν ενεργοποιήσετε τις πληρωμές POS.");
            pos.Note("Ilyda: κάρτα · iTimologia: κάρτα.\r\nIRIS: μόνο iTimologia με τρόπο POS Mellon.");
            RefreshPosModes(invoicing.Pos?.PosMode ?? (invoicing.Pos?.Provider.ProviderKey == InvoiceProviderCatalog.DirectIlydaKey ? invoicing.Pos.IlydaProtocol : null));
            sharedPosEnabled.ToggleStateChanged += (s,e) => RefreshPosEnabled();
            sharedEnvironment.SelectedIndexChanged += (s,e) => {
                providerEnvironment.Text = sharedEnvironment.Text;
                if (sharedMyDataUrl.Text.TrimEnd('/') == MyDataOptions.TestUrl.TrimEnd('/') || sharedMyDataUrl.Text.TrimEnd('/') == MyDataOptions.ProductionUrl.TrimEnd('/'))
                    sharedMyDataUrl.Text = sharedEnvironment.SelectedIndex == 0 ? MyDataOptions.TestUrl : MyDataOptions.ProductionUrl;
            };
            providerEnvironment.Text = sharedEnvironment.Text;
            sharedProvider.SelectedIndexChanged += (s,e) => {
                RefreshProviderFields();
                RefreshPosModes(sharedPosMode.SelectedItem?.Value as string);
            };
            RefreshProviderFields();

            var dieselPage = new RadPageViewPage { Text = "My Diesel" };
            radPageView1.Pages.Add(dieselPage);
            radPageView1.Pages.Add(legacy);
            var diesel = myDieselFields = new InvoicingFields("ΑΑΔΕ myDiesel"); dieselPage.Controls.Add(diesel.Group);
            var dieselProfile = invoicing.MyDiesel;
            myDieselEnabled = diesel.Row("My Diesel",new RadCheckBox { Text = "Νέες δεσμεύσεις ενεργές μετά την επανεκκίνηση",
                IsChecked = Data.Implementation.OptionHandler.Instance.MyDieselEnabled });
            myDieselEnvironment = diesel.Row("Περιβάλλον",InvoicingFields.Choice("Δοκιμαστικό","Παραγωγή"));
            myDieselEnvironment.SelectedIndex = sharedEnvironment.SelectedIndex;
            myDieselExciseRate = diesel.Row("Έκπτωση ΕΦΚ",new RadSpinEditor { Minimum = 0.0001m, Maximum = MyDieselTaxRules.MaximumExciseDiscountPerLitre,
                DecimalPlaces = 4, Value = dieselProfile?.ExciseDiscountPerLitre > 0 ? dieselProfile.ExciseDiscountPerLitre : MyDieselTaxRules.DefaultExciseDiscountPerLitre });
            diesel.Row("",new RadLabel { Text = "€ ανά λίτρο — η τιμή αποθηκεύεται με κάθε νέο κουπόνι" });
            myDieselCompany = diesel.Row("ΑΦΜ εταιρείας",InvoicingFields.Choice());
            LoadMyDieselCompanyChoices(dieselProfile?.CompanyVat ?? tinCompanyTextBox.Text.Trim());
            myDieselAadeUser = diesel.Row("Χρήστης ΑΑΔΕ",InvoicingFields.Text(CurrentMyDieselCredential()?.AadeUserId));
            myDieselSecret = diesel.Row("Κλειδί πρόσβασης",InvoicingFields.Text("",true));
            myDieselStation = diesel.Row("Καταχωρημένο πρατήριο",InvoicingFields.Choice());
            if (dieselProfile != null && !string.IsNullOrWhiteSpace(dieselProfile.StationName))
                myDieselStation.Items.Add(new RadListDataItem(dieselProfile.BranchNum + " — " + dieselProfile.StationName + " (" + dieselProfile.StationAmdika + ")") {
                    Value = new MyDieselGasStation { BranchNum = dieselProfile.BranchNum, Name = dieselProfile.StationName, Amdika = dieselProfile.StationAmdika }
                });
            if (myDieselStation.Items.Count != 0) myDieselStation.SelectedIndex = 0;
            myDieselPoll = diesel.Row("Έλεγχος κατάστασης",new RadSpinEditor { Minimum = 5, Maximum = 1440, Value = dieselProfile?.PollMinutes ?? 30 });
            diesel.Row("",new RadLabel { Text = "λεπτά (μόνο για ήδη εξαργυρωμένα κουπόνια)" });
            myDieselReferenceCheckedUtc = dieselProfile?.LastReferenceCheckUtc;
            myDieselReferenceStatus = diesel.Row("Στοιχεία αναφοράς",new RadLabel { Text = ReferenceCheckText() });
            var dieselCheck = diesel.Row("",new RadButton { Text = "Έλεγχος σύνδεσης και πρατηρίων…" });
            dieselCheck.Click += (s,e) => CheckMyDieselConnection();
            diesel.Note("UAT: χρησιμοποιείται το κοινό κλειδί ERP της επίσημης έκδοσης 0.1 και ένας από τους δοκιμαστικούς ΑΦΜ.\r\n" +
                "Δικαιούχοι δοκιμών: " + string.Join(" / ",MyDieselUatCatalog.BeneficiaryVats) + ". Εκδίδετε κουπόνια 5–10 λίτρων.");
            diesel.Note("Παραγωγή: χρησιμοποιούνται ο χρήστης και το αποθηκευμένο subscription key του ενεργού προφίλ myDATA.");
            diesel.Note("Το query κουπονιού μεταβάλλει κατάσταση και δεν εκτελείται από αυτή τη φόρμα.\r\n" +
                "Παραγωγή: ελεγμένο OpenAPI " + SharedMyDieselProductionContract.ContractVersion + " της ΑΑΔΕ (29/10/2026).");
            myDieselEnabled.ToggleStateChanged += (s,e) => RefreshMyDieselFields();
            myDieselEnvironment.SelectedIndexChanged += (s,e) => {
                if (myDieselEnvironment.SelectedIndex >= 0 && sharedEnvironment.SelectedIndex != myDieselEnvironment.SelectedIndex)
                    sharedEnvironment.SelectedIndex = myDieselEnvironment.SelectedIndex;
            };
            sharedEnvironment.SelectedIndexChanged += (s,e) => {
                myDieselReferenceCheckedUtc = null;
                LoadMyDieselCompanyChoices(invoicing.MyDiesel?.Environment == (sharedEnvironment.SelectedIndex == 0 ? FiscalEnvironment.Test : FiscalEnvironment.Production)
                    ? invoicing.MyDiesel.CompanyVat : tinCompanyTextBox.Text.Trim());
                myDieselStation.Items.Clear();
                RefreshMyDieselFields();
            };
            tinCompanyTextBox.TextChanged += (s,e) => {
                if (sharedEnvironment.SelectedIndex != 1) return;
                myDieselReferenceCheckedUtc = null; LoadMyDieselCompanyChoices(tinCompanyTextBox.Text.Trim()); myDieselStation.Items.Clear(); RefreshMyDieselFields();
            };
            myDieselCompany.SelectedIndexChanged += (s,e) => {
                if (!myDieselCompany.ContainsFocus) return;
                myDieselReferenceCheckedUtc = null; myDieselStation.Items.Clear(); myDieselReferenceStatus.Text = ReferenceCheckText();
            };
            myDieselAadeUser.TextChanged += (s,e) => myDieselReferenceCheckedUtc = null;
            myDieselStation.SelectedIndexChanged += (s,e) => { if (Focused) myDieselReferenceCheckedUtc = null; myDieselReferenceStatus.Text = ReferenceCheckText(); };
            RefreshMyDieselFields();
        }

        private SharedMyDieselCredentialProfile CurrentMyDieselCredential()
        {
            return (invoicing.MyDieselCredentials ?? new SharedMyDieselCredentialProfile[0])
                .SingleOrDefault(p => p.Id == invoicing.MyDiesel?.CredentialProfileId);
        }

        private void RefreshMyDieselFields()
        {
            if (myDieselFields == null) return;
            bool production = sharedEnvironment.SelectedIndex == 1;
            if (myDieselEnvironment.SelectedIndex != sharedEnvironment.SelectedIndex)
                myDieselEnvironment.SelectedIndex = sharedEnvironment.SelectedIndex;
            myDieselFields.SetRowVisible(myDieselAadeUser,false);
            myDieselFields.SetRowVisible(myDieselSecret,false);
            myDieselCompany.Enabled = myDieselEnabled.IsChecked && !production;
            myDieselStation.Enabled = myDieselExciseRate.Enabled = myDieselPoll.Enabled = myDieselEnabled.IsChecked;
            myDieselReferenceStatus.Text = ReferenceCheckText();
        }

        private void LoadMyDieselCompanyChoices(string preferredVat)
        {
            if (myDieselCompany == null) return;
            bool production = sharedEnvironment.SelectedIndex == 1;
            myDieselCompany.Items.Clear();
            if (production)
            {
                string vat = tinCompanyTextBox.Text.Trim();
                myDieselCompany.Items.Add(new RadListDataItem(vat) { Value = vat });
            }
            else
            {
                var vats = MyDieselUatCatalog.CompanyVats;
                for (int i = 0; i < vats.Length; i++)
                    myDieselCompany.Items.Add(new RadListDataItem(vats[i] + " — Δοκιμαστής " + (i / 3 + 1).ToString("00") +
                        " / Επιχείρηση " + (i % 3 + 1)) { Value = vats[i] });
            }
            int selected = -1;
            for (int i = 0; i < myDieselCompany.Items.Count; i++)
                if (string.Equals(myDieselCompany.Items[i].Value as string,preferredVat,StringComparison.Ordinal)) { selected = i; break; }
            myDieselCompany.SelectedIndex = selected >= 0 ? selected : myDieselCompany.Items.Count == 0 ? -1 : 0;
        }

        private string SelectedMyDieselCompanyVat()
        {
            return (myDieselCompany.SelectedItem?.Value as string ?? myDieselCompany.Text).Trim();
        }

        private string ReferenceCheckText()
        {
            return myDieselReferenceCheckedUtc.HasValue ? "Επιτυχής έλεγχος: " + myDieselReferenceCheckedUtc.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm") : "Δεν έχει ολοκληρωθεί";
        }

        private SharedCredentialProfile CredentialFor(SharedInvoiceConfiguration configuration, ProviderRoute route)
        {
            if (configuration == null || route == null) return null;
            string serializedRoute = JsonConvert.SerializeObject(route);
            return (configuration.Credentials ?? new SharedCredentialProfile[0])
                .LastOrDefault(profile => profile.Route != null && JsonConvert.SerializeObject(profile.Route) == serializedRoute);
        }

        private string SavedMyDataSubscriptionKey()
        {
            var profile = CredentialFor(invoicing,invoicing?.DirectMyData);
            return string.IsNullOrWhiteSpace(profile?.SubscriptionKeyOption) ? "" :
                Data.Implementation.OptionHandler.Instance.GetOption(profile.SubscriptionKeyOption,"");
        }

        private string ChangedMyDataSubscriptionKey()
        {
            string current = sharedMyDataKey.Text.Trim();
            var environment = sharedEnvironment.SelectedIndex == 0 ? FiscalEnvironment.Test : FiscalEnvironment.Production;
            return string.Equals(current,originalMyDataKey,StringComparison.Ordinal) && SameSavedMyDataContext(environment) ? null : current;
        }

        private bool SameSavedMyDataContext(FiscalEnvironment environment)
        {
            return invoicing.DirectMyData != null &&
                invoicing.DirectMyData.ProviderKey == InvoiceProviderCatalog.MyDataKey &&
                invoicing.DirectMyData.TestMode == (environment == FiscalEnvironment.Test) &&
                string.Equals(invoicing.DirectMyData.Account,sharedMyDataUser.Text.Trim(),StringComparison.Ordinal) &&
                string.Equals(invoicing.DirectMyData.IssuerVat,tinCompanyTextBox.Text.Trim(),StringComparison.Ordinal) &&
                invoicing.DirectMyData.IssuerBranch == (int)branchSpinEditor.Value;
        }

        private string CurrentMyDataSubscriptionKey(FiscalEnvironment environment)
        {
            string entered = sharedMyDataKey.Text.Trim();
            if (!string.IsNullOrWhiteSpace(entered)) return entered;

            var profile = SameSavedMyDataContext(environment) ? CredentialFor(invoicing,invoicing.DirectMyData) : null;
            string option = profile?.SubscriptionKeyOption;
            string saved = string.IsNullOrWhiteSpace(option) ? null : Data.Implementation.OptionHandler.Instance.GetOption(option,"");
            if (string.IsNullOrWhiteSpace(saved))
                throw new InvalidOperationException("Συμπληρώστε το subscription key myDATA για το επιλεγμένο περιβάλλον και αποθηκεύστε τις ρυθμίσεις.");
            return saved;
        }

        private void CheckMyDieselConnection()
        {
            try
            {
                var environment = sharedEnvironment.SelectedIndex == 0 ? FiscalEnvironment.Test : FiscalEnvironment.Production;
                string company = MyDieselCouponIdentity.NormalizeVat(SelectedMyDieselCompanyVat(),"Company VAT");
                string secret = environment == FiscalEnvironment.Test ? MyDieselUatCatalog.PrimaryErpApiKey : CurrentMyDataSubscriptionKey(environment);
                string user = environment == FiscalEnvironment.Production ? sharedMyDataUser.Text.Trim() : null;
                if (environment == FiscalEnvironment.Production && string.IsNullOrWhiteSpace(user))
                    throw new InvalidOperationException("Συμπληρώστε τον χρήστη myDATA για το περιβάλλον παραγωγής.");
                var temporaryScope = new MyDieselScope(environment,company,0,"settings-check");
                var database = new SqlFiscalDatabase(Properties.Settings.Default.DBConnection);
                using (var client = new MyDieselClient(new MyDieselClientOptions { Scope = temporaryScope,
                    AadeUserId = user, ProductionSubscriptionKey = environment == FiscalEnvironment.Production ? secret : null,
                    UatApiKey = environment == FiscalEnvironment.Test ? secret : null, Logger = new SqlEvidenceLogger(database) }))
                {
                    var companyResult = client.GetCompany(company); var fuels = client.GetFuelTypes();
                    string context = (environment == FiscalEnvironment.Production ? "Παραγωγή" : "Δοκιμαστικό") + ", ΑΦΜ " + company;
                    if (!companyResult.Succeeded) throw new InvalidOperationException("Ο έλεγχος εταιρείας απέτυχε (" + context + "): " + (companyResult.Error?.Code ?? companyResult.CommunicationError));
                    if (companyResult.Data == null || !companyResult.Data.Active || companyResult.Data.GasStations == null || companyResult.Data.GasStations.Length == 0)
                        throw new InvalidOperationException("Δεν βρέθηκε ενεργό καταχωρημένο πρατήριο myDiesel (" + context + "). Ελέγξτε ότι η επιχείρηση έχει εγγραφεί στο myDiesel της ΑΑΔΕ.");
                    if (!fuels.Succeeded || fuels.Data == null || !fuels.Data.Any(f => MyDieselFuelRules.IsEligible(f.Code)))
                        throw new InvalidOperationException("Δεν επιστράφηκαν οι αναμενόμενοι κωδικοί καυσίμου myDiesel.");
                    int previous = (myDieselStation.SelectedItem?.Value as MyDieselGasStation)?.BranchNum ?? invoicing.MyDiesel?.BranchNum ?? -1;
                    myDieselStation.Items.Clear();
                    foreach (var station in companyResult.Data.GasStations)
                        myDieselStation.Items.Add(new RadListDataItem(station.BranchNum + " — " + station.Name + " (" + station.Amdika + ")") { Value = station });
                    myDieselStation.SelectedIndex = Math.Max(0,companyResult.Data.GasStations.ToList().FindIndex(s => s.BranchNum == previous));
                    myDieselReferenceCheckedUtc = DateTime.UtcNow; myDieselReferenceStatus.Text = ReferenceCheckText();
                    RadMessageBox.Show("Η εταιρεία, τα πρατήρια και οι κωδικοί καυσίμου ελέγχθηκαν επιτυχώς. Δεν έγινε query ή δέσμευση κουπονιού.","myDiesel");
                }
            }
            catch (Exception ex) { myDieselReferenceCheckedUtc = null; myDieselReferenceStatus.Text = ReferenceCheckText(); RadMessageBox.Show(ex.Message,"myDiesel",MessageBoxButtons.OK,RadMessageIcon.Error); }
        }

        private ProviderRoute SavedProviderRoute(string key)
        {
            if (invoicing.InvoiceProvider?.ProviderKey == key) return invoicing.InvoiceProvider;
            return (invoicing.Credentials ?? new SharedCredentialProfile[0]).Select(p => p.Route).LastOrDefault(r =>
                r != null && r.ProviderKey == key && r.TestMode == invoicing.TestMode &&
                r.IssuerVat == tinCompanyTextBox.Text.Trim() && r.IssuerBranch == (int)branchSpinEditor.Value);
        }
        private void RefreshProviderFields()
        {
            bool ilyda = sharedProvider.SelectedIndex == 0;
            foreach (var control in new[] { sharedIlydaAccount,sharedIlydaPassword }) providerFields.SetRowVisible(control,ilyda);
            foreach (var control in new[] { sharedITimologiaAccount,sharedITimologiaToken }) providerFields.SetRowVisible(control,!ilyda);
            var definition = InvoiceProviderCatalog.Get(ilyda ? InvoiceProviderCatalog.DirectIlydaKey : InvoiceProviderCatalog.ITimologiaKey);
            providerCode.Text = definition.AadeProviderCode;
            providerLicence.Text = definition.SoftwareLicence;
        }

        private void RefreshPosModes(string selectedMode)
        {
            if (selectedMode == "DEFAULT") selectedMode = "EDPS";
            sharedPosMode.Items.Clear();
            var providerKey = sharedProvider.SelectedIndex == 0 ? InvoiceProviderCatalog.DirectIlydaKey : InvoiceProviderCatalog.ITimologiaKey;
            foreach (var mode in PosModeCatalog.ForProvider(providerKey))
                sharedPosMode.Items.Add(new RadListDataItem(mode.DisplayName) { Value = mode.Key });
            sharedPosMode.SelectedIndex = -1;
            for (int i = 0; i < sharedPosMode.Items.Count; i++)
                if ((string)sharedPosMode.Items[i].Value == selectedMode) sharedPosMode.SelectedIndex = i;
            RefreshPosEnabled();
        }
        private void RefreshPosEnabled()
        {
            sharedPosMode.Enabled = sharedGateway.Enabled = sharedTerminal.Enabled = sharedPosEnabled.IsChecked;
        }

        private SharedInvoiceConfiguration BuildSharedInvoicing()
        {
            pendingSecrets.Clear();
            var value = JsonConvert.DeserializeObject<SharedInvoiceConfiguration>(JsonConvert.SerializeObject(invoicing));
            if (invoicing.Version == 1)
            {
                // Freeze imported secret references before legacy controls can change
                // their Option values. Historical routes themselves remain identical.
                foreach (var profile in value.Credentials ?? new SharedCredentialProfile[0])
                {
                    profile.SubscriptionKeyOption = PreserveImportedSecret(profile.SubscriptionKeyOption);
                    profile.MasterTokenOption = PreserveImportedSecret(profile.MasterTokenOption);
                    profile.ApiKeyOption = PreserveImportedSecret(profile.ApiKeyOption);
                    profile.PasswordOption = PreserveImportedSecret(profile.PasswordOption);
                }
            }
            value.Version = 3; value.Enabled = sharedEnabled.IsChecked; value.TestMode = sharedEnvironment.SelectedIndex == 0;
            value.InvoiceTypes = new Dictionary<Guid,SharedInvoiceRoute>();
            value.FirstInvoiceDate = sharedCutover.Value;
            if (value.Mapping == null) value.Mapping = new MappingContext { Countries = new[] { new CountryInput { CountryCode = "GR", DisplayMember = "Ελλάδα", IsEu = true } } };
            value.Mapping.WholesaleCodes = new HashSet<int>(sharedWholesale.Text.Split(new[] { ',', ';', ' ' },StringSplitOptions.RemoveEmptyEntries).Select(int.Parse));
            value.DirectMyData = BuildCredential(value,InvoiceProviderCatalog.MyDataKey,sharedMyDataUser.Text,ChangedMyDataSubscriptionKey(),null,sharedMyDataUrl.Text,invoicing.DirectMyData);
            var providerKey = sharedProvider.SelectedIndex == 0 ? InvoiceProviderCatalog.DirectIlydaKey : InvoiceProviderCatalog.ITimologiaKey;
            value.SelectedProviderKey = providerKey;
            value.InvoiceProvider = providerKey == InvoiceProviderCatalog.DirectIlydaKey
                ? BuildCredential(value,providerKey,sharedIlydaAccount.Text,null,sharedIlydaPassword.Text,null,SavedProviderRoute(providerKey))
                : BuildCredential(value,providerKey,sharedITimologiaAccount.Text,sharedITimologiaToken.Text,null,null,SavedProviderRoute(providerKey));
            value.Pos = sharedPosEnabled.IsChecked ? PaymentRoute.ForMode(value.InvoiceProvider,sharedGateway.Text,sharedTerminal.Text,sharedPosMode.SelectedItem?.Value as string) : null;
            if (value.FiscalDevice != null && ((value.FiscalDevice.Environment == FiscalEnvironment.Test) != value.TestMode || value.FiscalDevice.IssuerVat != tinCompanyTextBox.Text.Trim() || value.FiscalDevice.IssuerBranch != (int)branchSpinEditor.Value))
            {
                // Keep the old profile for recovery, but require re-selection for new issuance.
                value.FiscalDevice = null;
            }
            value.MyDiesel = BuildMyDiesel(value);
            return value;
        }

        private SharedMyDieselProfile BuildMyDiesel(SharedInvoiceConfiguration value)
        {
            var old = CurrentMyDieselCredential();
            var environment = value.TestMode ? FiscalEnvironment.Test : FiscalEnvironment.Production;
            string company = SelectedMyDieselCompanyVat();
            var myDataCredential = environment == FiscalEnvironment.Production ? CredentialFor(value,value.DirectMyData) : null;
            bool same = old != null && old.Environment == environment && old.CompanyVat == company &&
                (environment == FiscalEnvironment.Test || myDataCredential != null &&
                    old.AadeUserId == value.DirectMyData.Account &&
                    old.ProductionSubscriptionKeyOption == myDataCredential.SubscriptionKeyOption);
            var station = myDieselStation.SelectedItem?.Value as MyDieselGasStation;
            if (!myDieselEnabled.IsChecked && old == null)
                return new SharedMyDieselProfile { Enabled = false, Environment = environment, CompanyVat = company,
                    BranchNum = station?.BranchNum ?? -1, StationName = station?.Name, StationAmdika = station?.Amdika,
                    ExciseDiscountPerLitre = MyDieselTaxRules.NormalizeExciseDiscountPerLitre(myDieselExciseRate.Value),
                    LastReferenceCheckUtc = myDieselReferenceCheckedUtc, PollMinutes = (int)myDieselPoll.Value };
            SharedMyDieselCredentialProfile credential = same ? old : null;
            if (credential == null)
            {
                string id = Guid.NewGuid().ToString("N");
                if (environment == FiscalEnvironment.Production && (value.DirectMyData == null || myDataCredential == null ||
                    string.IsNullOrWhiteSpace(myDataCredential.SubscriptionKeyOption)))
                    throw new InvalidOperationException("Το myDiesel παραγωγής απαιτεί αποθηκευμένο προφίλ χρήστη και subscription key myDATA.");
                credential = new SharedMyDieselCredentialProfile { Id = id, Environment = environment, CompanyVat = company,
                    AadeUserId = environment == FiscalEnvironment.Production ? value.DirectMyData.Account : null,
                    ProductionSubscriptionKeyOption = environment == FiscalEnvironment.Production ? myDataCredential.SubscriptionKeyOption : null };
                var credentials = (value.MyDieselCredentials ?? new SharedMyDieselCredentialProfile[0]).ToList(); credentials.Add(credential); value.MyDieselCredentials = credentials.ToArray();
            }
            return new SharedMyDieselProfile { Enabled = myDieselEnabled.IsChecked, Environment = environment, CompanyVat = company,
                BranchNum = station?.BranchNum ?? -1, StationName = station?.Name, StationAmdika = station?.Amdika,
                ExciseDiscountPerLitre = MyDieselTaxRules.NormalizeExciseDiscountPerLitre(myDieselExciseRate.Value),
                CredentialProfileId = credential.Id, ContractVersion = environment == FiscalEnvironment.Test ? MyDieselUatCatalog.ContractVersion : SharedMyDieselProductionContract.ContractVersion,
                OpenApiSha256 = environment == FiscalEnvironment.Test ? null : SharedMyDieselProductionContract.OpenApiSha256, LastReferenceCheckUtc = myDieselReferenceCheckedUtc,
                PollMinutes = (int)myDieselPoll.Value };
        }
        private string PreserveImportedSecret(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return key;
            string secret = Data.Implementation.OptionHandler.Instance.GetOption(key,"");
            if (string.IsNullOrWhiteSpace(secret)) return key;
            string frozen = "Fiscal_" + Guid.NewGuid().ToString("N") + "_Imported";
            pendingSecrets.Add(frozen,secret);
            return frozen;
        }
        private ProviderRoute BuildCredential(SharedInvoiceConfiguration value,string key,string account,string secret,string password,string endpoint,ProviderRoute previous)
        {
            if (string.IsNullOrWhiteSpace(account))
            {
                if (!string.IsNullOrWhiteSpace(secret) || !string.IsNullOrWhiteSpace(password)) throw new InvalidOperationException("Ορίστε λογαριασμό για τα νέα διαπιστευτήρια.");
                return null;
            }
            var profiles = (value.Credentials ?? new SharedCredentialProfile[0]).ToList();
            bool sameAccount = previous != null && previous.ProviderKey == key && previous.Account == account.Trim() && previous.TestMode == value.TestMode && previous.IssuerVat == tinCompanyTextBox.Text.Trim() && previous.IssuerBranch == (int)branchSpinEditor.Value;
            var old = sameAccount ? profiles.SingleOrDefault(p => JsonConvert.SerializeObject(p.Route) == JsonConvert.SerializeObject(previous)) : null;
            if (string.IsNullOrWhiteSpace(secret) && string.IsNullOrWhiteSpace(password) && old == null)
                throw new InvalidOperationException("Ο νέος λογαριασμός / περιβάλλον απαιτεί διαπιστευτήρια: " + key);
            string version = Guid.NewGuid().ToString("N");
            string licence = InvoiceProviderCatalog.Get(key).SoftwareLicence;
            var route = new ProviderRoute(key,value.TestMode,tinCompanyTextBox.Text,(int)branchSpinEditor.Value,version,account,endpoint,licence);
            // New settings use only the selected provider's credentials. Retain old
            // API-key profiles in the history, never on a new Ilyda password profile.
            var profile = new SharedCredentialProfile { Route = route,
                SubscriptionKeyOption = key == InvoiceProviderCatalog.MyDataKey ? old?.SubscriptionKeyOption : null,
                MasterTokenOption = key == InvoiceProviderCatalog.ITimologiaKey ? old?.MasterTokenOption : null,
                PasswordOption = key == InvoiceProviderCatalog.DirectIlydaKey ? old?.PasswordOption : null };
            if (!string.IsNullOrWhiteSpace(secret))
            {
                string option = "Fiscal_" + version + "_Key"; pendingSecrets.Add(option,secret);
                if (key == InvoiceProviderCatalog.MyDataKey) profile.SubscriptionKeyOption = option;
                else if (key == InvoiceProviderCatalog.ITimologiaKey) profile.MasterTokenOption = option;
            }
            if (key == InvoiceProviderCatalog.DirectIlydaKey && !string.IsNullOrWhiteSpace(password))
            {
                profile.PasswordOption = "Fiscal_" + version + "_Password"; pendingSecrets.Add(profile.PasswordOption,password);
            }
            if (key == InvoiceProviderCatalog.DirectIlydaKey && string.IsNullOrWhiteSpace(profile.PasswordOption))
                throw new InvalidOperationException("Συμπληρώστε τον κωδικό πρόσβασης Ilyda.");
            if (key == InvoiceProviderCatalog.MyDataKey && profile.SubscriptionKeyOption == null || key == InvoiceProviderCatalog.ITimologiaKey && profile.MasterTokenOption == null)
                throw new InvalidOperationException("Ελλιπή διαπιστευτήρια: " + key);
            profiles.Add(profile); value.Credentials = profiles.ToArray();
            return route;
        }
        private bool SaveSharedInvoicing()
        {
            try
            {
                var value = BuildSharedInvoicing();
                bool transitionPaused;
                if (!PrepareFiscalEnvironmentTransition(value,out transitionPaused)) return false;
                SharedInvoicingSettings.ValidateActivation(value);
                // Unique keys are never overwritten. An interrupted save can leave an
                // unused secret, but cannot change any current or historical profile.
                foreach (var entry in pendingSecrets) Data.Implementation.OptionHandler.Instance.SetOption(entry.Key,entry.Value);
                invoicingRevision = SharedInvoicingSettings.Save(value,invoicingRevision);
                Data.Implementation.OptionHandler.Instance.SetMyDieselEnabled(myDieselEnabled.IsChecked);
                invoicing = value;
                if (!string.IsNullOrWhiteSpace(sharedMyDataKey.Text)) originalMyDataKey = sharedMyDataKey.Text.Trim();
                string message = transitionPaused
                    ? "Το περιβάλλον αποθηκεύτηκε με την κοινή τιμολόγηση και το myDiesel προσωρινά σε παύση. Ρυθμίστε τον Samtec για το νέο περιβάλλον από τις Επιλογές Φορολογικού Μηχανισμού και μετά ενεργοποιήστε ξανά την κοινή τιμολόγηση."
                    : "Οι ρυθμίσεις τιμολόγησης αποθηκεύτηκαν. Ο λογαριασμός και τα κλειδιά ισχύουν πριν από την πρώτη προσπάθεια έκδοσης. Οι υπόλοιπες αλλαγές απαιτούν επανεκκίνηση της εφαρμογής.";
                RadMessageBox.Show(message,"Τιμολόγηση");
                return true;
            }
            catch (Exception ex) { RadMessageBox.Show(ex.Message,"Ρυθμίσεις τιμολόγησης",MessageBoxButtons.OK,RadMessageIcon.Error); return false; }
        }

        private bool PrepareFiscalEnvironmentTransition(SharedInvoiceConfiguration value, out bool paused)
        {
            paused = false;
            if (!value.Enabled || invoicing.TestMode == value.TestMode || value.FiscalDevice != null) return true;
            int signingTypes = SharedInvoicingSettings.Store().LoadPolicies().Values.Count(policy => policy.SignWithFiscalDevice);
            if (signingTypes == 0) return true;
            string environment = value.TestMode ? "Δοκιμαστικό" : "Παραγωγή";
            var answer = RadMessageBox.Show(
                "Το περιβάλλον αλλάζει σε " + environment + ", αλλά " + signingTypes +
                " τύποι παραστατικών απαιτούν Samtec και δεν υπάρχει προφίλ για το νέο περιβάλλον.\r\n\r\n" +
                "Να αποθηκευτεί η αλλαγή με την κοινή τιμολόγηση και το myDiesel προσωρινά σε παύση; " +
                "Στη συνέχεια ρυθμίστε τον Samtec και ενεργοποιήστε ξανά την κοινή τιμολόγηση.",
                "Αλλαγή περιβάλλοντος",MessageBoxButtons.YesNo,RadMessageIcon.Question);
            if (answer != DialogResult.Yes) return false;
            value.Enabled = false;
            sharedEnabled.IsChecked = false;
            if (value.MyDiesel != null) value.MyDiesel.Enabled = false;
            myDieselEnabled.IsChecked = false;
            paused = true;
            return true;
        }
    }
}
