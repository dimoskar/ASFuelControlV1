using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ASFuelControl.Logging;

namespace ASFuelControl.Windows.UI.Controls
{
    public partial class WaitingClock : UserControl
    {
        private delegate void UpdateUiDelegate(TimeSpan ts);

        public event EventHandler TimerCompleted;
        public event EventHandler TimerStarted;

        System.Threading.Timer timer = null;
        TimeSpan remainingTime = TimeSpan.FromSeconds(0);

        public WaitingClock()
        {
            InitializeComponent();
            timer = new System.Threading.Timer(new System.Threading.TimerCallback(this.TimerCallBack), null, System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
        }

        public void AddWaitingTime(TimeSpan ts)
        {
            if (this.remainingTime > ts)
                return;
            this.remainingTime = ts;
            this.timer.Change((int)1000, 1000);
            this.radProgressBar1.Maximum = (int)this.remainingTime.TotalSeconds;
            this.Invoke(new UpdateUiDelegate(this.UpdateUi), new object[] { this.remainingTime });
            if (this.TimerStarted != null)
                this.TimerStarted(this, new EventArgs());
        }

        private void UpdateUi(TimeSpan ts)
        {
            this.radTextBox1.Text = ts.ToString("c");
            this.radProgressBar1.Value1 = (int)ts.TotalSeconds;
        }

        private void TimerCallBack(object foo)
        {
            try
            {
                this.remainingTime = this.remainingTime.Subtract(TimeSpan.FromSeconds(1));
                this.Invoke(new UpdateUiDelegate(this.UpdateUi), new object[] { this.remainingTime });
                if (this.remainingTime.TotalSeconds < 1)
                {
                    this.timer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
                    if (this.TimerCompleted != null)
                        this.TimerCompleted(this, new EventArgs());
                }
            }
            catch(Exception ex)
            {
                Logger.Instance.LogToFile("WaitingClock::TimerCallBack", ex);
            }
        }
    }
}
