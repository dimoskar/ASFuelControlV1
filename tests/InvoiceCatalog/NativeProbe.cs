using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ASFuelControl.Data;
using ASFuelControl.Windows.UI.Catalogs;
using ASFuelControl.Windows.ViewModels;
using Telerik.WinControls.UI;
using Telerik.WinControls.Data;

internal static class NativeProbe
{
    private static int result = 1;
    [STAThread]
    private static int Main(string[] args)
    {
        try { return RunMain(args); }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }

    private static int RunMain(string[] args)
    {
        if (args.Length != 1) return 2;
        var connection = new SqlConnectionStringBuilder(args[0]);
        if (!connection.DataSource.StartsWith("(localdb)\\ASFC_Catalog_") || !connection.InitialCatalog.StartsWith("Catalog_")) return 2;
        // Override only this test process's in-memory settings. Never save them or run Program.Main.
        var assembly = typeof(InvoiceCatalog).Assembly;
        var settingsType = assembly.GetType("ASFuelControl.Windows.Properties.Settings");
        var settings = (ApplicationSettingsBase)settingsType.GetProperty("Default").GetValue(null, null);
        var unused = settings["DBConnection"];
        settings.PropertyValues["DBConnection"].PropertyValue = args[0];
        if ((string)settings["DBConnection"] != args[0]) throw new Exception("Test connection was not installed.");
        var cache = (CommonCache)FormatterServices.GetUninitializedObject(typeof(CommonCache));
        cache.InvoiceTypes = Enumerable.Range(1, 3).Select(n => new InvoiceTypeDetailViewModel {
            InvoiceTypeId = Guid.Parse("00000000-0000-0000-0000-" + n.ToString("D12")), TransactionType = n == 1 ? 0 : n == 2 ? 1 : 2, Description = "Test " + n
        }).ToArray();
        typeof(CommonCache).GetField("instance", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, cache);
        DatabaseModel.UserLoggedIn = Guid.Parse("00000000-0000-0000-0000-000000000010");
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Control.CheckForIllegalCrossThreadCalls = true;
        using (var form = new Form { Width = 1100, Height = 650, ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000) })
        using (var catalog = new InvoiceCatalog { Dock = DockStyle.Fill })
        {
            form.Controls.Add(catalog);
            Application.ThreadException += delegate(object sender, ThreadExceptionEventArgs e) { Console.Error.WriteLine(e.Exception); result = 1; form.Close(); };
            form.Shown += async delegate
            {
                try { await Run(catalog, args[0]); result = 0; }
                catch (Exception ex) { Console.Error.WriteLine(ex); result = 1; }
                finally { form.Close(); }
            };
            Application.Run(form);
        }
        return result;
    }

    private static async Task Run(InvoiceCatalog catalog, string connection)
    {
        var vm = (InvoiceCatalogViewModel)Field(catalog, "vm");
        var grid = (RadGridView)Field(catalog, "invoiceRadGridView");
        vm.InvoicesFrom = vm.InvoicesTo = new DateTime(2026, 10, 18);
        using (var blocker = new SqlConnection(connection))
        {
            blocker.Open();
            using (var transaction = blocker.BeginTransaction())
            {
                LockInvoices(blocker, transaction);
                catalog.RefreshData();
                Check(!grid.Enabled, "grid and document actions disabled while loading");
                var posted = new TaskCompletionSource<bool>();
                catalog.BeginInvoke(new Action(() => posted.SetResult(!grid.Enabled)));
                Check(await Task.WhenAny(posted.Task, Task.Delay(5000)) == posted.Task && posted.Task.Result,
                    "WinForms message loop remains responsive during database load");
                vm.Filter = "12,13"; catalog.RefreshData();
                vm.Filter = "14,15"; catalog.RefreshData();
                transaction.Rollback();
            }
            await Until(() => grid.Enabled);
            Check(vm.Invoices.Length == 2 && vm.Invoices.All(i => i.Number == 14 || i.Number == 15), "native grid binds only the latest overlapping search");
            Check(grid.Rows.Count == 2 && object.ReferenceEquals(vm.Invoices, vm.Invoices), "all returned rows are bound to a stable array");
            decimal total = vm.GetSummary(4, "TotalAmount"), rest = vm.GetSummary(4, "RestAmount");
            Check(total == 19.18m && rest == -1.37m, "native view model retains legacy signed total rules");
            grid.FilterDescriptors.Add(new FilterDescriptor("TotalAmount", FilterOperator.IsEqualTo, 19.18m));
            await Task.Delay(100);
            Check(grid.ChildRows.Count == 1, "existing grid column filtering still works");
            Check(Convert.ToDecimal(grid.MasterView.SummaryRows[4].Cells["TotalAmount"].Value) == total &&
                  Convert.ToDecimal(grid.MasterView.SummaryRows[4].Cells["RestAmount"].Value) == rest,
                  "visible grid summaries remain full-result totals after column filtering");
            grid.FilterDescriptors.Clear();
            grid.Rows[0].Cells["Selected"].Value = true;
            Check(vm.Invoices.Any(i => i.Selected), "checkbox selection still updates the invoice used by catalog actions");
            using (var bitmap = new Bitmap(catalog.Width, catalog.Height))
            {
                catalog.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
                bitmap.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "catalog-loaded.png"));
            }
            var previous = vm.Invoices;
            using (var transaction = blocker.BeginTransaction())
            {
                LockInvoices(blocker, transaction);
                vm.Filter = ""; catalog.RefreshData();
                await Task.Delay(100);
                catalog.Dispose();
                transaction.Rollback();
            }
            var gate = (SemaphoreSlim)Field(Field(vm, "loader"), "gate");
            await Until(() => gate.CurrentCount == 1);
            Check(object.ReferenceEquals(previous, vm.Invoices), "closing a loading catalog does not bind results to disposed controls");
        }
    }

    private static void LockInvoices(SqlConnection c, SqlTransaction t)
    {
        using (var cmd = new SqlCommand("SELECT TOP (1) InvoiceId FROM dbo.Invoice WITH (TABLOCKX, HOLDLOCK)", c, t)) cmd.ExecuteScalar();
    }
    private static object Field(object value, string name) { return value.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(value); }
    private static void Check(bool ok, string name) { if (!ok) throw new Exception("FAIL native " + name); Console.WriteLine("PASS native " + name); }
    private static async Task Until(Func<bool> predicate)
    {
        var clock = Stopwatch.StartNew();
        while (!predicate()) { if (clock.Elapsed.TotalSeconds > 30) throw new TimeoutException("Native catalog did not finish loading."); await Task.Delay(20); }
    }
}
