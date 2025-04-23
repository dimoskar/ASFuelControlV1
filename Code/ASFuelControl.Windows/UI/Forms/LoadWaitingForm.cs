using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Telerik.WinControls.UI;
using System.Threading;

namespace ASFuelControl.Windows.UI.Forms
{
    public partial class LoadWaitingForm : RadForm
    {
        private static Thread _waitingThread; 
        private static LoadWaitingForm _waitingDialog; 
 
        private LoadWaitingForm() 
        { 
            InitializeComponent();
            //this.Load += new EventHandler(LoadWaitingForm_Load);
        }

        void LoadWaitingForm_Load(object sender, EventArgs e)
        {
            this.radWaitingBar1.StartWaiting(); 
        } 
 
        public static void ShowForm() 
        { 
            //Show the form in a new thread 
            ThreadStart threadStart = new ThreadStart(LaunchDialog); 
            _waitingThread = new Thread(threadStart); 
            _waitingThread.IsBackground = true; 
            _waitingThread.Start(); 
        } 
 
        private static void LaunchDialog() 
        { 
            _waitingDialog = new LoadWaitingForm();
            _waitingDialog.radWaitingBar1.StartWaiting(); 
            //Create new message pump 
            int x = (System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Width - _waitingDialog.Width) / 2;
            int y = (System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Height - _waitingDialog.Height - 350) / 2;
            _waitingDialog.Location = new Point(x, y);
            Application.Run(_waitingDialog);
        } 
 
        private static void CloseDialogDown() 
        { 
            Application.ExitThread(); 
        } 
 
        public static void CloseDialog() 
        { 
            //Need to get the thread that launched the form, so 
            //we need to use invoke. 
            if (_waitingDialog != null) 
            { 
                try 
                { 
                    MethodInvoker mi = new MethodInvoker(CloseDialogDown); 
                    _waitingDialog.Invoke(mi); 
                } 
                catch { } 
            } 
        } 
    }
}
