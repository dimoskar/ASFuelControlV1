using ASFuelControl.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASFuelControl.Windows.Threads
{
    public class TankCheckThread : IDisposable
    {
        private Data.DatabaseModel database = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
        private volatile bool stopThread = false;
        private DateTime lastTankCheck = DateTime.MinValue;
        private System.Threading.Thread th = null;
        private int tankCheckInterval = 300000;
        public bool IsAlive { get { return this.th != null && this.th.IsAlive; } }

        public TankCheckThread()
        {
            this.tankCheckInterval = Data.Implementation.OptionHandler.Instance.GetIntOption("TankCheckInterval", 60000);
        }

        public void StartThread()
        {
            if (this.th != null && this.th.IsAlive)
                return;
            this.stopThread = false;
            th = new System.Threading.Thread(new System.Threading.ThreadStart(this.ThreadRun));
            th.IsBackground = true;
            th.Start();
        }

        /// <summary>
        /// Stops the main thread of the agent
        /// </summary>
        public bool StopThread()
        {
            this.stopThread = true;
            System.Threading.Thread worker = this.th;
            if (worker == null || worker == System.Threading.Thread.CurrentThread)
                return true;
            bool stopped = worker.Join(TimeSpan.FromSeconds(10));
            if (!stopped)
                Logger.Instance.LogToFile("TankCheckThread::Stop", "Tank check worker did not stop within 10 seconds.");
            return stopped;
        }

        public void Dispose()
        {
            if (StopThread())
                this.database.Dispose();
        }

        public void ThreadRun()
        {
            int minute = tankCheckInterval;
            while (!stopThread)
            {
                System.Threading.Thread.Sleep(500);
                DateTime dt = DateTime.Now;
                if ((int)dt.TimeOfDay.TotalMinutes % minute != 0)
                    continue;
                if (this.lastTankCheck.Date == dt.Date && this.lastTankCheck.Hour == dt.Hour && this.lastTankCheck.Minute == dt.Minute)
                    continue;
                this.lastTankCheck = dt;
                this.CreateTankCheck();
                
            }
            this.th = null;
        }

        private void CreateTankCheck()
        {
            using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                bool validValues = true;
                foreach (Data.Tank tank in this.database.Tanks)
                {
                    try
                    {
                        if (tank.PhysicalState == 0)
                        {
                            continue;
                        }
                        Data.TankCheck tc = new Data.TankCheck();
                        tc.TankCheckId = Guid.NewGuid();
                        tc.TankId = tank.TankId;
                        tc.TankLevel = tank.FuelLevel;
                        tc.CheckDate = DateTime.Now;
                        tc.Temperature = tank.Temperatire;
                        db.Add(tc);
                        db.SaveChanges();
                    }
                    catch (Exception etc)
                    {
                        Logger.Instance.LogToFile("TankCheckThread:CreateTankCheck", etc);
                    }
                }
            }
        }
    }
}
