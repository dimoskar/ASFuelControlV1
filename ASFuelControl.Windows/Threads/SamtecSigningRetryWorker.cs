using System;
using System.Linq;
using System.Threading;
using ASFuelControl.Invoicing;
using ASFuelControl.Invoicing.Persistence;

namespace ASFuelControl.Windows.Threads
{
    // Durable retry queue for Samtec invoice signing. Each document is eligible
    // five minutes after its last inconclusive dispatch. A retryable failure opens
    // a short queue-wide circuit so a backlog cannot hammer an unavailable device.
    internal sealed class SamtecSigningRetryWorker : IDisposable
    {
        internal static readonly TimeSpan DocumentRetryDelay = TimeSpan.FromMinutes(5);
        internal static readonly TimeSpan QueueFailurePause = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan BacklogWarningInterval = TimeSpan.FromMinutes(30);
        private const int BatchSize = 25;
        private const int LargeBacklogSize = 100;

        private readonly ManualResetEvent stop = new ManualResetEvent(false);
        private Thread thread;
        private DateTime queuePausedUntilUtc = DateTime.MinValue;
        private DateTime lastBacklogWarningUtc = DateTime.MinValue;

        internal void Start()
        {
            if (thread != null) return;
            thread = new Thread(Loop) { IsBackground = true,Name = "Samtec signing retry queue" };
            thread.Start();
        }

        internal void Stop()
        {
            stop.Set();
            if (thread != null && thread.IsAlive) thread.Join(TimeSpan.FromSeconds(10));
            if (thread != null && !thread.IsAlive) thread = null;
        }

        private void Loop()
        {
            while (!stop.WaitOne(PollInterval))
            {
                try { RunCycle(DateTime.UtcNow); }
                catch (Exception ex) { Logging.Logger.Instance.LogToFile("Samtec signing retry worker",ex); }
            }
        }

        internal void RunCycle(DateTime nowUtc)
        {
            if (nowUtc.Kind != DateTimeKind.Utc)
                throw new ArgumentException("Retry queue time must be UTC.",nameof(nowUtc));
            if (nowUtc < queuePausedUntilUtc) return;

            var database = new SqlFiscalDatabase(Properties.Settings.Default.DBConnection,15000);
            var store = new SqlFiscalSigningStore(database);
            var dueBefore = nowUtc - DocumentRetryDelay;
            int count = store.CountDeferredSamtecInvoiceRetries(dueBefore);
            if (count >= LargeBacklogSize && nowUtc - lastBacklogWarningUtc >= BacklogWarningInterval)
            {
                lastBacklogWarningUtc = nowUtc;
                Logging.Logger.Instance.LogToFile("Samtec signing retry backlog",
                    new InvalidOperationException("The deferred Samtec signing queue contains " +
                        count + " due invoices. Processing remains oldest-first in bounded batches."));
            }

            foreach (var invoiceId in store.LoadDeferredSamtecInvoiceRetries(dueBefore,BatchSize))
            {
                if (stop.WaitOne(0)) return;
                try
                {
                    // Complete signing/submission but leave physical printing to the
                    // normal UI/PrintAgent path after the durable outcome is known.
                    var result = SharedInvoiceRuntime.Process(invoiceId,false);
                    if (IsRetryableSigningFailure(result))
                    {
                        queuePausedUntilUtc = DateTime.UtcNow + QueueFailurePause;
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Logging.Logger.Instance.LogToFile("Samtec signing retry " + invoiceId,ex);
                }
            }
        }

        private static bool IsRetryableSigningFailure(InvoiceWorkflowResult result)
        {
            return result?.Response?.Errors?.Any(error =>
                (error ?? "").StartsWith("Automatic signing retries were exhausted",
                    StringComparison.Ordinal)) == true;
        }

        public void Dispose()
        {
            Stop();
            // HttpClient can still be completing its bounded request when the host
            // shuts down. Do not dispose the stop handle underneath that thread.
            if (thread == null) stop.Dispose();
        }
    }
}
