using System;
using System.Threading;
using ASFuelControl.Data.Implementation;
using ASFuelControl.Logging;

namespace ASFuelControl.Windows.Threads
{
    /// <summary>
    /// Runs one workload with commit-driven wakeups, adaptive fallback waits,
    /// and an explicit lifecycle.
    /// </summary>
    internal sealed class ManagedPollingWorker
    {
        private readonly Func<bool> action;
        private readonly TimeSpan[] idleIntervals;
        private readonly object lifecycleSync = new object();
        private readonly TimeSpan minimumActionInterval;
        private readonly string name;
        private readonly ManualResetEvent stopSignal = new ManualResetEvent(false);
        private readonly WaitHandle[] waitHandles;
        private readonly OutboundWorkState workState;
        private Thread thread;

        internal ManagedPollingWorker(
            string name,
            TimeSpan[] idleIntervals,
            Func<bool> action,
            OutboundWorkState workState)
            : this(
                name,
                idleIntervals,
                action,
                workState,
                TimeSpan.Zero)
        {
        }

        internal ManagedPollingWorker(
            string name,
            TimeSpan[] idleIntervals,
            Func<bool> action,
            OutboundWorkState workState,
            TimeSpan minimumActionInterval)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("A worker name is required.", "name");
            }
            if (idleIntervals == null || idleIntervals.Length == 0)
            {
                throw new ArgumentException(
                    "At least one idle interval is required.",
                    "idleIntervals");
            }
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }
            if (minimumActionInterval < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException("minimumActionInterval");
            }
            TimeSpan previousInterval = TimeSpan.Zero;
            foreach (TimeSpan idleInterval in idleIntervals)
            {
                if (idleInterval <= TimeSpan.Zero || idleInterval < previousInterval)
                {
                    throw new ArgumentOutOfRangeException("idleIntervals");
                }
                previousInterval = idleInterval;
            }

            TimeSpan[] configuredIntervals = new TimeSpan[idleIntervals.Length];
            Array.Copy(idleIntervals, configuredIntervals, idleIntervals.Length);
            this.name = name;
            this.idleIntervals = configuredIntervals;
            this.action = action;
            this.workState = workState;
            this.minimumActionInterval = minimumActionInterval;

            if (this.workState == null)
            {
                this.waitHandles = new WaitHandle[] { this.stopSignal };
            }
            else
            {
                WaitHandle workSignal = this.workState.WakeSignal;
                this.waitHandles = new WaitHandle[] { this.stopSignal, workSignal };
            }
        }

        internal string Name
        {
            get
            {
                return this.name;
            }
        }

        internal bool IsAlive
        {
            get
            {
                lock (this.lifecycleSync)
                {
                    return this.thread != null && this.thread.IsAlive;
                }
            }
        }

        internal void Start()
        {
            lock (this.lifecycleSync)
            {
                if (this.thread != null && this.thread.IsAlive)
                {
                    return;
                }

                this.stopSignal.Reset();
                ThreadStart threadStart = this.Run;
                Thread workerThread = new Thread(threadStart);
                workerThread.IsBackground = true;
                workerThread.Name = this.name;
                this.thread = workerThread;
                workerThread.Start();
            }
        }

        internal void RequestStop()
        {
            this.stopSignal.Set();
        }

        internal bool WaitForStop(TimeSpan timeout)
        {
            Thread workerThread;
            lock (this.lifecycleSync)
            {
                workerThread = this.thread;
            }

            if (workerThread == null || workerThread == Thread.CurrentThread)
            {
                return true;
            }

            bool stopped = workerThread.Join(timeout);
            return stopped;
        }

        private void Run()
        {
            int idleIntervalIndex = 0;
            DateTime lastActionStartedUtc = DateTime.MinValue;
            try
            {
                while (true)
                {
                    bool stopRequested = this.stopSignal.WaitOne(0);
                    if (stopRequested)
                    {
                        break;
                    }

                    DateTime nowUtc = DateTime.UtcNow;
                    TimeSpan elapsedSinceLastAction =
                        nowUtc - lastActionStartedUtc;
                    TimeSpan minimumDelay =
                        this.minimumActionInterval - elapsedSinceLastAction;
                    if (minimumDelay > TimeSpan.Zero)
                    {
                        bool stoppedDuringMinimumDelay =
                            this.stopSignal.WaitOne(minimumDelay);
                        if (stoppedDuringMinimumDelay)
                        {
                            break;
                        }

                        if (this.workState != null)
                        {
                            WaitHandle workSignal = this.workState.WakeSignal;
                            workSignal.WaitOne(0);
                        }
                    }

                    DateTime actionStartedUtc = DateTime.UtcNow;
                    lastActionStartedUtc = actionStartedUtc;
                    bool workProcessed = false;
                    try
                    {
                        workProcessed = this.action();
                    }
                    catch (Exception ex)
                    {
                        string source = string.Format("{0}::Loop", this.name);
                        Logger.Instance.LogToFile(source, ex);
                    }

                    if (workProcessed)
                    {
                        idleIntervalIndex = 0;
                    }

                    TimeSpan idleInterval = this.idleIntervals[idleIntervalIndex];
                    if (!workProcessed &&
                        idleIntervalIndex < this.idleIntervals.Length - 1)
                    {
                        idleIntervalIndex++;
                    }

                    DateTime actionFinishedUtc = DateTime.UtcNow;
                    TimeSpan actionDuration =
                        actionFinishedUtc - actionStartedUtc;
                    TimeSpan waitTime = idleInterval - actionDuration;
                    if (waitTime < TimeSpan.Zero)
                    {
                        waitTime = TimeSpan.Zero;
                    }

                    int signalIndex = WaitHandle.WaitAny(this.waitHandles, waitTime);
                    if (signalIndex == 0)
                    {
                        break;
                    }
                    if (this.workState != null && signalIndex == 1)
                    {
                        idleIntervalIndex = 0;
                    }
                }
            }
            finally
            {
                lock (this.lifecycleSync)
                {
                    if (this.thread == Thread.CurrentThread)
                    {
                        this.thread = null;
                    }
                }

                string message = string.Format("{0}, Thread Stopped", this.name);
                Common.Logger.Instance.Debug(message);
            }
        }
    }
}
