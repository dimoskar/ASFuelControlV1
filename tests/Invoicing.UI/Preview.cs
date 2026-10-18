using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Windows.Forms;
using ASFuelControl.Invoicing;
using ASFuelControl.Invoicing.Persistence;
using ASFuelControl.Windows.Threads;
using Exedron.Invoicing.Core;
using Exedron.Invoicing.Adapters.Samtec;
using Newtonsoft.Json;
using Telerik.WinControls;
using Telerik.WinControls.UI;

internal static class Preview
{
    internal static readonly Guid TypeId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    static Assembly resources;
    internal static ResourceManager Resources(string name) => new ResourceManager(name,resources);
    internal static SharedInvoiceConfiguration Configuration()
    {
        var route = new ProviderRoute(InvoiceProviderCatalog.DirectIlydaKey,true,"000000000",0,"demo-profile","demo-account");
        var scope = new FiscalSigningScope("Samtec","demo-device",FiscalEnvironment.Test,"000000000",0);
        return new SharedInvoiceConfiguration { Version = 2,TestMode = true,FirstInvoiceDate = new DateTime(2026,10,16),InvoiceProvider = route,
            Pos = PaymentRoute.ForMode(route,"http://gateway.invalid:1234","DEMO-TID","VIVA"),
            FiscalDevice = scope,Devices = new[] { new SamtecDeviceProfile(scope,new Uri("http://device.invalid:1234")) },
            DefaultPrinter = new SharedPrinterProfile { PrinterName = "DEMO Thermal" },
            Credentials = new[] { new SharedCredentialProfile { Route = route,ApiKeyOption = "demo-key-reference",PasswordOption = "demo-password-reference" } },
            Mapping = new ASFuelControl.Invoicing.Mapping.MappingContext { WholesaleCodes = new HashSet<int> { 100 } } };
    }
    [STAThread] static int Main(string[] args)
    {
        resources = Assembly.LoadFrom(args[0]);
        Application.EnableVisualStyles(); new Telerik.WinControls.Themes.TelerikMetroBlueTheme(); ThemeResolutionService.ApplicationThemeName = "TelerikMetroBlue";
        using(var form = new ASFuelControl.Windows.UI.SettingForms.MainSettingsForm())
        {
            foreach(var tab in new[] { "radPageViewPage4","radPageViewPage5","radPageViewPage3" })
            {
                Find<RadPageView>(form,"radPageView1").SelectedPage = Find<RadPageViewPage>(form,tab);
                Capture(form,"settings-"+tab);
            }
            var build = form.GetType().GetMethod("BuildSharedInvoicing",BindingFlags.Instance|BindingFlags.NonPublic);
            var config = (SharedInvoiceConfiguration)build.Invoke(form,null);
            if(config.InvoiceProvider.ProviderKey != InvoiceProviderCatalog.DirectIlydaKey || config.Credentials.Last().ApiKeyOption != null ||
                config.Credentials.Last().PasswordOption != "demo-password-reference" || config.Credentials.First().ApiKeyOption != "demo-key-reference")
                throw new Exception("Ilyda password profile or historical API-key retention failed");
            var activeCredentials = config.Resolve(config.InvoiceProvider,key => key);
            if(activeCredentials.ApiKey != null || activeCredentials.Password != "demo-password-reference") throw new Exception("Old API key overrides password authentication");
            var originalConfig = Field<SharedInvoiceConfiguration>(form,"invoicing");
            originalConfig.Credentials[0].PasswordOption = null;
            try { build.Invoke(form,null); throw new Exception("API-key-only profile accepted without a password"); }
            catch(TargetInvocationException ex) { if(!(ex.InnerException is InvalidOperationException) || !ex.InnerException.Message.Contains("κωδικό πρόσβασης Ilyda")) throw; }
            originalConfig.Credentials[0].PasswordOption = "demo-password-reference";
            if(config.InvoiceProvider.SoftwareLicence != InvoiceProviderCatalog.DirectIlydaSoftwareLicence || Field<RadLabel>(form,"providerCode").Text != "008")
                throw new Exception("Ilyda catalogue identity was not applied");
            var ilydaAccount = Field<RadTextBox>(form,"sharedIlydaAccount");
            var ilydaPassword = Field<RadTextBox>(form,"sharedIlydaPassword");
            var timAccount = Field<RadTextBox>(form,"sharedITimologiaAccount");
            var timToken = Field<RadTextBox>(form,"sharedITimologiaToken");
            if(!RowShown(ilydaAccount) || !RowShown(ilydaPassword) || RowShown(timAccount) || RowShown(timToken))
                throw new Exception("Ilyda screen mixes provider fields");
            ilydaPassword.Text = "synthetic-ilyda-password";
            var posMode = Field<RadDropDownList>(form,"sharedPosMode");
            if(posMode.Items.Count != 9 || config.Pos.PosMode != "VIVA" || config.Pos.IlydaProtocol != "VIVA") throw new Exception("Ilyda POS mode binding failed");
            Field<RadDropDownList>(form,"sharedProvider").SelectedIndex = 1;
            if(RowShown(ilydaAccount) || RowShown(ilydaPassword) || !RowShown(timAccount) || !RowShown(timToken))
                throw new Exception("iTimologia screen mixes provider fields");
            if(timAccount.Text != "" || timToken.Text != "") throw new Exception("Ilyda credentials crossed into iTimologia");
            timAccount.Text = "tim-subscription"; timToken.Text = "synthetic-token";
            config = (SharedInvoiceConfiguration)build.Invoke(form,null);
            if(config.InvoiceProvider.ProviderKey != InvoiceProviderCatalog.ITimologiaKey || config.Credentials.Last().MasterTokenOption == null) throw new Exception("iTimologia selection failed");
            if(config.InvoiceProvider.Account != "tim-subscription" || config.Credentials.Last().ApiKeyOption != null || config.Credentials.Last().PasswordOption != null)
                throw new Exception("iTimologia used Ilyda credential fields");
            var secrets = Field<Dictionary<string,string>>(form,"pendingSecrets");
            if(secrets.Count != 1 || secrets[config.Credentials.Last().MasterTokenOption] != "synthetic-token")
                throw new Exception("Hidden provider credentials were persisted");
            if(config.InvoiceProvider.SoftwareLicence != InvoiceProviderCatalog.ITimologiaSoftwareLicence || Field<RadLabel>(form,"providerCode").Text != "034")
                throw new Exception("iTimologia catalogue identity was not applied");
            Find<RadPageView>(form,"radPageView1").SelectedPage = Find<RadPageViewPage>(form,"radPageViewPage5");
            Capture(form,"settings-provider-itimologia");
            if(posMode.Items.Count != 6 || config.Pos.PosMode != "VIVA" || config.Pos.ITimologiaSignatureFormat != 1) throw new Exception("iTimologia POS mode mapping failed");
            posMode.SelectedIndex = posMode.Items.IndexOf(posMode.Items.Single(item => (string)item.Value == "MELLON"));
            config = (SharedInvoiceConfiguration)build.Invoke(form,null);
            if(config.Pos.ITimologiaSignatureFormat != 0 || config.Pos.PosMode != "MELLON") throw new Exception("Mellon signature mapping failed");
            Field<RadDropDownList>(form,"sharedProvider").SelectedIndex = 0;
            config = (SharedInvoiceConfiguration)build.Invoke(form,null);
            if(config.InvoiceProvider.Account != "demo-account" || config.Credentials.Last().ApiKeyOption != null || secrets.Count != 1 ||
                secrets[config.Credentials.Last().PasswordOption] != "synthetic-ilyda-password" || config.Credentials.Last().MasterTokenOption != null)
                throw new Exception("Switching providers lost or mixed their own credentials");
            posMode.SelectedIndex = posMode.Items.IndexOf(posMode.Items.Single(item => (string)item.Value == "WORLDLINE"));
            Field<RadDropDownList>(form,"sharedProvider").SelectedIndex = 1;
            if(posMode.SelectedIndex != -1) throw new Exception("Unsupported POS mode silently replaced");
            try { build.Invoke(form,null); throw new Exception("Missing POS mode accepted"); }
            catch(TargetInvocationException ex) { if(!(ex.InnerException is ArgumentException)) throw; }
            if(ilydaPassword.PasswordChar == '\0' || timToken.PasswordChar == '\0') throw new Exception("Unmasked credential input");
            if(timAccount.Text != "tim-subscription" || timToken.Text != "synthetic-token") throw new Exception("Switching erased iTimologia fields");
        }
        using(var form = new ASFuelControl.Windows.UI.SettingForms.PrinterSettings())
        {
            var tabs = form.Controls.OfType<RadPageView>().Single();
            tabs.SelectedPage = tabs.Pages[0]; Capture(form,"signing");
            tabs.SelectedPage = tabs.Pages[1]; Capture(form,"printing");
        }
        using(var form = new ASFuelControl.Windows.UI.Forms.InvoiceTypesForm())
        {
            Capture(form,"invoice-types");
            var choice = Field<RadDropDownList>(form,"transmission");
            var check = Field<RadCheckBox>(form,"fiscalSigning");
            if(choice.Items.Count != 3 || choice.SelectedIndex != 2 || !check.IsChecked) throw new Exception("Type policy binding failed");
            choice.SelectedIndex = 0; check.IsChecked = false;
            var policies = Field<Dictionary<Guid,InvoiceTypePolicy>>(form,"policies");
            if(!policies[TypeId].LocalOnly) throw new Exception("Print-only UI selection failed");
            check.IsChecked = true; choice.SelectedIndex = (int)InvoiceTransmissionMode.Provider;
            if(check.Enabled || check.IsChecked || policies[TypeId].SignWithFiscalDevice) throw new Exception("Provider route allows fiscal device signing");
            Capture(form,"invoice-types-provider");
            choice.SelectedIndex = (int)InvoiceTransmissionMode.MyData;
            if(!check.Enabled) throw new Exception("Direct myDATA device choice is unavailable");
        }
        using(var form = new ASFuelControl.Windows.UI.Forms.InvoiceFormEx())
        {
            Capture(form,"invoice-status");
            var print = Find<RadButton>(form,"btnPrint");
            var save = Find<RadButton>(form,"btnSave");
            var panel = Field<ASFuelControl.Windows.UI.Controls.FiscalDocumentPanel>(form,"fiscalPanel");
            if (!print.Enabled || save.Enabled || Find<RadButton>(form,"btnSelectTrader").Enabled)
                throw new Exception("Issued invoice must keep Print available and editing locked");
            foreach (bool owned in new[] { false,true })
            {
                SharedInvoiceRuntime.Status = new SharedInvoiceStatus { Snapshot = owned ? new InvoiceWorkflowSnapshot {
                    Provider = new ProviderRoute("myDATA",true,"000000000",0,"test","test"),PrintDestination = "test" } : null };
                var id = Guid.NewGuid(); panel.LoadInvoice(id);
                if (!print.Enabled || save.Enabled == owned) throw new Exception("Incorrect manual issuance controls");
                using(var entered = new System.Threading.ManualResetEventSlim())
                using(var release = new System.Threading.ManualResetEventSlim())
                {
                    SharedInvoiceRuntime.OnProcess = () => { entered.Set(); if (!release.Wait(5000)) throw new Exception("UI test timed out"); };
                    int before = SharedInvoiceRuntime.ProcessCalls;
                    var pending = panel.PrintInvoice();
                    try
                    {
                        if (!entered.Wait(5000) || !panel.Busy || print.Enabled || save.Enabled)
                            throw new Exception("Manual processing must disable duplicate clicks and editing");
                        if (!panel.PrintInvoice().IsCompleted || SharedInvoiceRuntime.ProcessCalls != before + 1)
                            throw new Exception("Duplicate manual action dispatched");
                        if (SharedInvoiceRuntime.LastInvoice != id || SharedInvoiceRuntime.LastReason != null || !SharedInvoiceRuntime.LastPrint)
                            throw new Exception("Manual action did not use normal PrintAgent processing");
                    }
                    finally { release.Set(); }
                    var wait = System.Diagnostics.Stopwatch.StartNew();
                    while (!pending.IsCompleted && wait.ElapsedMilliseconds < 5000) { Application.DoEvents(); System.Threading.Thread.Sleep(10); }
                    if (!pending.IsCompleted) throw new Exception("UI continuation did not finish");
                    pending.GetAwaiter().GetResult();
                    if (panel.Busy || !print.Enabled || save.Enabled == owned) throw new Exception("Manual controls were not restored");
                }
            }
            SharedInvoiceRuntime.FailRead = true; panel.LoadInvoice(Guid.NewGuid());
            if (print.Enabled || save.Enabled) throw new Exception("Status read failure must disable print and editing");
            SharedInvoiceRuntime.FailRead = false; SharedInvoiceRuntime.OnProcess = null;
            SharedInvoiceRuntime.Status = new SharedInvoiceStatus();
        }
        Console.WriteLine("PASS manual print UI: original button available, issued fields locked, new and unfinished invoices use Process, duplicate clicks blocked, read failures locked.");
        using(var form = new ASFuelControl.Windows.UI.Forms.InvoiceFormEx())
        {
            var config = Configuration(); config.Enabled = true;
            var type = new ASFuelControl.Data.InvoiceType { InvoiceTypeId = TypeId,Printable = true };
            var invoice = new ASFuelControl.Data.Invoice { InvoiceId = Guid.NewGuid(),InvoiceTypeId = TypeId,InvoiceType = type,
                Number = 0,TransactionDate = config.FirstInvoiceDate };
            invoice.InvoiceLines.Add(new ASFuelControl.Data.InvoiceLine { InvoiceLineId = Guid.NewGuid() });
            var policy = new InvoiceTypePolicy { InvoiceTypeId = TypeId,Transmission = InvoiceTransmissionMode.Provider };
            config.Policies = new Dictionary<Guid,InvoiceTypePolicy> { { TypeId,policy } };
            var panel = Field<ASFuelControl.Windows.UI.Controls.FiscalDocumentPanel>(form,"fiscalPanel");
            var summary = Field<RadTextBox>(panel,"summary");
            var originalCalls = SharedInvoiceRuntime.ProcessCalls;
            SharedInvoiceRuntime.Status = new SharedInvoiceStatus { Snapshot = null,PendingSummary = config.PendingInvoiceSummary(invoice,false) };
            Capture(form,"invoice-pending-provider");
            if (panel.Owned || summary.Text.Contains("Παλαιό") || !summary.Text.Contains("Ilyda") || !summary.Text.Contains("Δεν ξεκίνησε"))
                throw new Exception("New invoice did not show pending provider route");
            type.IsCancelation = true; type.OfficialEnumerator = 215;
            SharedInvoiceRuntime.Status.PendingSummary = config.PendingInvoiceSummary(invoice,false);
            panel.LoadInvoice(invoice.InvoiceId); Capture(form,"invoice-pending-cancellation");
            if (!summary.Text.Contains("ακυρωτικός")) throw new Exception("Cancellation blocker is hidden");
            policy.Transmission = InvoiceTransmissionMode.MyData;
            config.DirectMyData = new ProviderRoute("myDATA",true,"000000000",0,"test","test");
            SharedInvoiceRuntime.Status.PendingSummary = config.PendingInvoiceSummary(invoice,false);
            panel.LoadInvoice(invoice.InvoiceId);
            if (!summary.Text.Contains("Απευθείας myDATA") || summary.Text.Contains("Ilyda")) throw new Exception("Pending myDATA route is wrong");
            SharedInvoiceRuntime.Status.Snapshot = new InvoiceWorkflowSnapshot { Provider = config.InvoiceProvider,PrintDestination = "test" };
            panel.LoadInvoice(invoice.InvoiceId);
            if (!summary.Text.Contains("Αποθηκευμένη διαβίβαση") || !summary.Text.Contains("Ilyda") || summary.Text.Contains("ακυρωτικός"))
                throw new Exception("Current configuration replaced the saved route");
            if (SharedInvoiceRuntime.ProcessCalls != originalCalls || invoice.Number != 0) throw new Exception("Status preview issued or numbered an invoice");
            type.IsCancelation = false; type.OfficialEnumerator = 100; invoice.IsPrinted = true; invoice.Number = 42;
            invoice.TransactionDate = config.FirstInvoiceDate.AddDays(-1);
            SharedInvoiceRuntime.Status = new SharedInvoiceStatus { Snapshot = null,PendingSummary = config.PendingInvoiceSummary(invoice,false),
                CanSubmitPrinted = config.CanSubmitPrintedInvoice(invoice,false) };
            panel.LoadInvoice(invoice.InvoiceId); Capture(form,"invoice-printed-pending");
            var submit = Field<RadButton>(panel,"resume");
            if (!submit.Enabled || submit.Text != "Διαβίβαση" || !summary.Text.Contains("χωρίς νέα εκτύπωση"))
                throw new Exception("Printed-only invoice has no explicit send-only action");
            SharedInvoiceRuntime.Status.CanSubmitPrinted = false; panel.LoadInvoice(invoice.InvoiceId);
            if (submit.Enabled) throw new Exception("Ineligible invoice exposes first submission");
            SharedInvoiceRuntime.Status = new SharedInvoiceStatus { AlreadyPrinted = true,
                Snapshot = new InvoiceWorkflowSnapshot { Provider = new ProviderRoute("iTimologia",true,"000000000",0,"profile-for-evidence-only","3"),PrintDestination = "test" } };
            panel.LoadInvoice(invoice.InvoiceId);
            if (!submit.Enabled || !summary.Text.Contains("δεν έχει καταγραφεί αποστολή") || !summary.Text.Contains("Ήδη εκτυπωμένο"))
                throw new Exception("Prepared send-only status hides prior printing or the absence of submission");
            var failedResult = new InvoiceWorkflowResult();
            typeof(InvoiceWorkflowResult).GetProperty("FiscalState").SetValue(failedResult,"NotSubmitted",null);
            typeof(InvoiceWorkflowResult).GetProperty("Response").SetValue(failedResult,new Exedron.Invoicing.Core.Models.InvoiceResponse {
                Errors = new[] { "iTimologia request failed (HTTP 401): Synthetic subscription denied" } },null);
            panel.RecordResult(invoice.InvoiceId,failedResult); panel.LoadInvoice(invoice.InvoiceId);
            if (!summary.Text.Contains("Δεν εστάλη") || !summary.Text.Contains("HTTP 401") || !summary.Text.Contains("Synthetic subscription denied") ||
                summary.Text.Contains("profile-for-evidence-only") || !Field<RadTextBox>(panel,"evidence").Text.Contains("profile-for-evidence-only"))
                throw new Exception("Submission errors are lost on refresh or credential profile is exposed in summary");
            if (Field<RadButton>(panel,"print").Enabled || !Field<RadTextBox>(panel,"printing").Text.Contains("δεν έγινε νέα εκτύπωση"))
                throw new Exception("Failed send-only status falsely reports or allows new printing");
            Capture(form,"invoice-printed-send-error");
            SharedInvoiceRuntime.Status.ConfiguredProvider = new ProviderRoute("iTimologia",true,"000000000",0,"configured-profile","1");
            panel.LoadInvoice(invoice.InvoiceId);
            if (!summary.Text.Contains("Λογαριασμός / συνδρομή: 1 (ρυθμίσεις)") || summary.Text.Contains("συνδρομή: 3") ||
                SharedInvoiceRuntime.Status.Snapshot.Provider.Account != "3" || !submit.Enabled)
                throw new Exception("Pending submission did not preview configured connection without changing saved invoice");
            Capture(form,"invoice-configured-subscription");
            SharedInvoiceRuntime.Status.ConnectionProblem = "Synthetic scope mismatch";
            panel.LoadInvoice(invoice.InvoiceId);
            if (submit.Enabled || !summary.Text.Contains("Synthetic scope mismatch")) throw new Exception("Connection mismatch not shown");
            SharedInvoiceRuntime.Status.ConnectionProblem = null;
            var otherInvoiceId = Guid.NewGuid(); panel.LoadInvoice(otherInvoiceId);
            panel.RecordResult(invoice.InvoiceId,failedResult); panel.LoadInvoice(otherInvoiceId);
            if (summary.Text.Contains("Synthetic subscription denied")) throw new Exception("Error carried over to another invoice");
            if (SharedInvoiceRuntime.ProcessCalls != originalCalls) throw new Exception("Viewing submission error dispatched a transport");
            var oldPayment = PaymentEvidence("Unknown",null,null);
            var approvedPayment = PaymentEvidence("Approved",oldPayment.AttemptId,null);
            SharedInvoiceRuntime.Status = new SharedInvoiceStatus {
                Snapshot = new InvoiceWorkflowSnapshot { Provider = config.InvoiceProvider,Payment = new PaymentRoute(config.InvoiceProvider,"http://gateway.invalid:7902/","TID-1"),PrintDestination = "test" },
                Attempts = new[] { oldPayment,approvedPayment } };
            panel.LoadInvoice(invoice.InvoiceId);
            if (!submit.Enabled || !Field<RadTextBox>(panel,"payment").Text.Contains("F4 → F2 → 0E"))
                throw new Exception("Superseded unknown payment blocks continuation or hides the operator retry");
            var paymentPages = Field<RadPageView>(panel,"pages"); paymentPages.SelectedPage = paymentPages.Pages[2];
            Capture(form,"invoice-pos-operator-retry");
            var singleF4 = PaymentEvidence("Unknown",null,null,"GBridge.F4.v1");
            SharedInvoiceRuntime.Status.Attempts = new[] { singleF4,PaymentEvidence("Approved",singleF4.AttemptId,null) };
            panel.LoadInvoice(invoice.InvoiceId);
            if (!submit.Enabled || !Field<RadTextBox>(panel,"payment").Text.Contains("μετά από F4.") || Field<RadTextBox>(panel,"payment").Text.Contains("F4 → F2"))
                throw new Exception("Single F4 retry shows the wrong recovery policy");
            Capture(form,"invoice-pos-single-f4-retry");
            var retryButton = Field<RadButton>(panel,"retryPayment");
            foreach(var retryPolicy in new[] { "GBridge.F4.v1", "GBridge.F4-F2-0E.v1" })
            {
                var awaiting = PaymentEvidence("Unknown",null,null,retryPolicy);
                SharedInvoiceRuntime.Status.Attempts = new[] { awaiting };
                SharedInvoiceRuntime.Status.RetryPaymentAttemptId = awaiting.AttemptId;
                panel.LoadInvoice(invoice.InvoiceId);
                form.Show(); Application.DoEvents();
                if (!retryButton.Enabled || !retryButton.Visible || retryButton.Text != "Επανάληψη πληρωμής" || submit.Enabled ||
                    !Field<RadButton>(panel,"recover").Enabled || !summary.Text.Contains("Αναμονή επανάληψης"))
                    throw new Exception("F4 does not offer a separate operator payment retry");
                Capture(form,retryPolicy == "GBridge.F4.v1" ? "invoice-pos-awaiting-f4-retry" : "invoice-pos-awaiting-sequence-retry");
                form.Show(); Application.DoEvents();
                using(var entered = new System.Threading.ManualResetEventSlim())
                using(var release = new System.Threading.ManualResetEventSlim())
                {
                    SharedInvoiceRuntime.OnRetry = () => { entered.Set(); if (!release.Wait(5000)) throw new Exception("Retry UI test timed out"); };
                    int before = SharedInvoiceRuntime.RetryCalls;
                    retryButton.PerformClick();
                    try {
                        if (!entered.Wait(5000) || !panel.Busy || retryButton.Enabled ||
                            SharedInvoiceRuntime.LastRetryAttempt != awaiting.AttemptId || SharedInvoiceRuntime.LastInvoice != invoice.InvoiceId)
                            throw new Exception("Retry did not dispatch immediately without a confirmation dialog: entered=" + entered.IsSet + ", busy=" + panel.Busy + ", enabled=" + retryButton.Enabled + ", calls=" + SharedInvoiceRuntime.RetryCalls);
                        retryButton.PerformClick();
                        if (SharedInvoiceRuntime.RetryCalls != before + 1) throw new Exception("Duplicate retry click dispatched");
                    } finally { release.Set(); }
                    var wait = System.Diagnostics.Stopwatch.StartNew();
                    while(panel.Busy && wait.ElapsedMilliseconds < 5000) { Application.DoEvents(); System.Threading.Thread.Sleep(10); }
                    if(panel.Busy) throw new Exception("Retry UI continuation did not finish");
                    SharedInvoiceRuntime.OnRetry = null;
                }
            }
            SharedInvoiceRuntime.Status.RetryPaymentAttemptId = null;
            SharedInvoiceRuntime.Status.Attempts = new[] { PaymentEvidence("Unknown",null,"Ελλιπής απάντηση POS. Λείπουν πεδία: PaymentType. Επιλέξτε Ανάκτηση κατάστασης.") };
            panel.LoadInvoice(invoice.InvoiceId);
            if (submit.Enabled || retryButton.Enabled || !Field<RadButton>(panel,"recover").Enabled || !Field<RadTextBox>(panel,"payment").Text.Contains("PaymentType"))
                throw new Exception("Incomplete payment does not offer lookup-only recovery with missing-field diagnostics");
            Capture(form,"invoice-pos-incomplete-response");
            SharedInvoiceRuntime.Status.Attempts = new[] { PaymentEvidence("Declined",null,"NSP credentials rejected") };
            panel.LoadInvoice(invoice.InvoiceId);
            if (!submit.Enabled || !Field<RadTextBox>(panel,"payment").Text.Contains("NSP credentials rejected"))
                throw new Exception("Declined payment hides retry or actionable diagnostics");
            Capture(form,"invoice-pos-declined");
            SharedInvoiceRuntime.Status.Attempts = new[] { oldPayment }; panel.LoadInvoice(invoice.InvoiceId);
            if (submit.Enabled) throw new Exception("Unresolved unknown payment enables ordinary retry");
            SharedInvoiceRuntime.Status = new SharedInvoiceStatus();
        }
        Console.WriteLine("PASS pending invoice UI: configured provider/myDATA, cancellation blocker, saved route precedence and no issuance during inspection.");
        Console.WriteLine("PASS native UI: twenty renders, explicit F4 and sequence retry without confirmation, duplicate click blocked, incomplete response lookup-only recovery, configured subscription before first attempt, send-only errors survive refresh, prior printing is distinguished, isolated provider fields and credentials, catalogue IDs/licences, provider-only signing and POS mode selection. No application DB or external transport used.");
        return 0;
    }
    static T Field<T>(object value,string name) => (T)value.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(value);
    static FiscalAttemptEvidence PaymentEvidence(string state,Guid? operatorRetryOf,string error,string policy = null)
    {
        var type = typeof(SqlFiscalDatabase).GetNestedType("JournalEntry",BindingFlags.NonPublic);
        var entry = Activator.CreateInstance(type,true);
        foreach(var pair in new Dictionary<string,object> { {"Id",Guid.NewGuid()},{"Family","Payment"},{"Operation","Payment"},{"State",state},
            {"Payload",new byte[] {1}},{"Snapshot",JsonConvert.SerializeObject(new { Attempt = new { OperatorRetryOfAttemptId = operatorRetryOf },ErrorMessage = error,OperatorRetry = new { Policy = policy } })} })
            type.GetField(pair.Key,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(entry,pair.Value);
        return (FiscalAttemptEvidence)Activator.CreateInstance(typeof(FiscalAttemptEvidence),BindingFlags.Instance|BindingFlags.NonPublic,null,new[] {entry},null);
    }
    static bool RowShown(Control control) { var table = (TableLayoutPanel)control.Parent; return table.RowStyles[table.GetRow(control)].Height > 0; }
    static T Find<T>(Control parent,string name) where T : Control => parent.Controls.Find(name,true).OfType<T>().First();
    static void Capture(Form form,string name)
    {
        form.ShowInTaskbar = false; form.StartPosition = FormStartPosition.Manual; form.Location = new Point(-3000,-3000);
        form.Show(); form.PerformLayout(); Application.DoEvents();
        using(var bitmap = new Bitmap(form.ClientSize.Width,form.ClientSize.Height))
        {
            using(var graphics = Graphics.FromImage(bitmap)) graphics.Clear(form.BackColor);
            foreach(Control child in form.Controls.Cast<Control>().Reverse())
                if(child.Visible && child.Width > 0 && child.Height > 0) child.DrawToBitmap(bitmap,child.Bounds);
            bitmap.Save(name+".png",System.Drawing.Imaging.ImageFormat.Png);
        }
        form.Hide();
    }
}

// Only layout constructors and synthetic collaborators. None of the original form
// constructors, Program startup, OptionHandler, PrintAgent or transport calls run.
namespace ASFuelControl.Windows.UI.SettingForms
{
    public partial class MainSettingsForm : RadForm
    {
        public MainSettingsForm() { InitializeComponent(); tinCompanyTextBox.Text = "000000000"; InitializeSharedInvoicing(); }
    }
    public partial class PrinterSettings : RadForm
    {
        private Data.Implementation.OptionHandler optionHandler;
        public PrinterSettings() { InitializeComponent(); InitializeSharedPrinter(); }
    }
}
namespace ASFuelControl.Windows.UI.Forms
{
    public partial class InvoiceTypesForm : RadForm
    {
        ViewModels.InvoiceTypeCatalogViewModel vm = new ViewModels.InvoiceTypeCatalogViewModel();
        public InvoiceTypesForm()
        {
            InitializeComponent(); InitializeInvoicingPolicy();
            invoiceTypeViewModelBindingSource.DataSource = new[] { new ViewModels.InvoiceTypeDetailViewModel { InvoiceTypeId = Preview.TypeId } };
        }
    }
    public partial class InvoiceFormEx : RadForm
    {
        ViewModels.InvoiceDetailsViewModel ivm = new ViewModels.InvoiceDetailsViewModel();
        public InvoiceFormEx() { InitializeComponent(); InitializeFiscalStatus(); }
    }
}
namespace ASFuelControl.Windows.ViewModels
{
    public sealed class InvoiceTypeDetailViewModel { public Guid InvoiceTypeId { get; set; } }
    public sealed class InvoiceTypeCatalogViewModel { public bool HasChanges; public void SaveChanges() { throw new InvalidOperationException("No DB in preview"); } }
    public sealed class InvoiceDetailsViewModel { public Guid InvoiceId = Guid.NewGuid(); }
}
namespace ASFuelControl.Windows
{
    internal static class Program { internal static bool AdminConnected => false; }
}
namespace ASFuelControl.Windows.Threads
{
    internal sealed class SharedPrinterProfile
    {
        public string PrinterName { get; set; }
        public int WidthHundredths { get; set; } = 315;
        public int HeightHundredths { get; set; } = 1100;
        public short Copies { get; set; } = 1;
        public float FontSize { get; set; } = 8;
    }
    internal static class SharedInvoicePrinting { internal static void CheckProfile(SharedPrinterProfile value) { throw new InvalidOperationException("No transport in preview"); } }
    internal static class SharedInvoicingSettings
    {
        internal static SharedInvoiceConfiguration Load(out long revision) { revision = 1; return Preview.Configuration(); }
        internal static string Preflight(SharedInvoiceConfiguration value) { throw new InvalidOperationException("No DB in preview"); }
        internal static void ValidateActivation(SharedInvoiceConfiguration value) { throw new InvalidOperationException("No DB in preview"); }
        internal static long Save(SharedInvoiceConfiguration value,long revision) { throw new InvalidOperationException("No DB in preview"); }
        internal static FakePolicyStore Store() => new FakePolicyStore();
    }
    internal sealed class FakePolicyStore
    {
        internal Dictionary<Guid,InvoiceTypePolicy> LoadPolicies() => new Dictionary<Guid,InvoiceTypePolicy> { { Preview.TypeId,new InvoiceTypePolicy { InvoiceTypeId = Preview.TypeId,Transmission = InvoiceTransmissionMode.MyData,SignWithFiscalDevice = true,Revision = 1 } } };
        internal void SavePolicies(IEnumerable<InvoiceTypePolicy> values) { throw new InvalidOperationException("No DB in preview"); }
    }
    internal sealed class SharedInvoiceStatus
    {
        internal string PendingSummary;
        internal bool CanSubmitPrinted;
        internal bool AlreadyPrinted;
        internal ProviderRoute ConfiguredProvider;
        internal string ConnectionProblem;
        internal Guid? RetryPaymentAttemptId;
        internal InvoiceWorkflowSnapshot Snapshot = new InvoiceWorkflowSnapshot { LocalOnly = true,PrintDestination = "DEMO Thermal" };
        internal FiscalAttemptEvidence[] Attempts = new FiscalAttemptEvidence[0], Revisions = new FiscalAttemptEvidence[0];
        internal string State(string family) => Attempts.LastOrDefault(a => a.Family == family)?.State ?? "Δεν ξεκίνησε";
    }
    internal static class SharedInvoiceRuntime
    {
        internal static SharedInvoiceStatus Status = new SharedInvoiceStatus();
        internal static bool FailRead, LastPrint;
        internal static int ProcessCalls;
        internal static Guid LastInvoice;
        internal static string LastReason;
        internal static Action OnProcess;
        internal static Action OnRetry;
        internal static int RetryCalls;
        internal static Guid LastRetryAttempt;
        internal static bool Enabled => false;
        internal static bool SupportsIris => false;
        internal static SharedInvoiceStatus ReadStatus(Guid id) { if (FailRead) throw new InvalidOperationException("Synthetic read failure"); return Status; }
        internal static string RecoverStatus(Guid id) { throw new InvalidOperationException("No transport in preview"); }
        internal static ProviderPaymentResult RetryPayment(Guid id,Guid expectedAttemptId)
        {
            System.Threading.Interlocked.Increment(ref RetryCalls); LastInvoice = id; LastRetryAttempt = expectedAttemptId; OnRetry?.Invoke();
            var result = new ProviderPaymentResult();
            typeof(ProviderPaymentResult).GetProperty("Payment").SetValue(result,new ProviderPaymentAttempt { Id = expectedAttemptId,State = "Unknown" },null);
            return result;
        }
        internal static InvoiceWorkflowResult ResumeIssuance(Guid id) { throw new InvalidOperationException("No transport in preview"); }
        internal static InvoiceWorkflowResult SubmitPrintedInvoice(Guid id) { throw new InvalidOperationException("No transport in preview"); }
        internal static InvoiceWorkflowResult Process(Guid id,bool print,string reason)
        {
            System.Threading.Interlocked.Increment(ref ProcessCalls);
            LastInvoice = id; LastPrint = print; LastReason = reason; OnProcess?.Invoke();
            return null; // Dispatch spy only: no DB or external transport in the native UI check.
        }
        internal static void ReconcilePrint(Guid id,Guid job,bool confirmed,string reason) { throw new InvalidOperationException("No DB in preview"); }
    }
}
