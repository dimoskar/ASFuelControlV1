using System;
using System.Threading;
using ASFuelControl.Invoicing.Persistence;

namespace ASFuelControl.Windows.Threads
{
    internal sealed class MyDieselPollingWorker : IDisposable
    {
        private readonly ManualResetEvent stop = new ManualResetEvent(false);
        private Thread thread;

        internal void Start()
        {
            if (thread != null) return;
            thread = new Thread(Loop) { IsBackground = true,Name = "myDiesel verification polling" };
            thread.Start();
        }

        internal void Stop()
        {
            stop.Set();
            if (thread != null && thread.IsAlive) thread.Join(TimeSpan.FromSeconds(10));
            thread = null;
        }

        private void Loop()
        {
            while (!stop.WaitOne(TimeSpan.FromSeconds(30)))
            {
                try
                {
                    var configuration = SharedInvoicingSettings.LoadRuntime();
                    var profile = configuration.MyDiesel;
                    if (profile == null || profile.PollMinutes < 1) continue;
                    var database = new SqlFiscalDatabase(Properties.Settings.Default.DBConnection);
                    var store = new SqlMyDieselStore(database);
                    var service = SharedMyDieselRuntime.CreateService(configuration);
                    foreach (var caseId in store.LoadPollCandidates(DateTime.UtcNow.AddMinutes(-profile.PollMinutes),25))
                    {
                        if (stop.WaitOne(0)) return;
                        try { service.RefreshStatus(caseId); }
                        catch (Exception ex) { Logging.Logger.Instance.LogToFile("myDiesel verification polling",ex); }
                    }
                }
                catch (Exception ex) { Logging.Logger.Instance.LogToFile("myDiesel polling worker",ex); }
            }
        }

        public void Dispose() { Stop(); stop.Dispose(); }
    }
}
