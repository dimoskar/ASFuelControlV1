using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using AxFP_CLOCKLib;

namespace ASFuelControl.RFID
{
    public class Communication
    {
        #region InitializeFPCLOCK  
        private AxFP_CLOCK FPCLOCK = new AxFP_CLOCK();
        #endregion

        #region Events
        public event RfidHasEnrollNumber UpdateRfidData;
        #endregion

        #region Public GETSET Variables
        /// <summary>
        /// Ip Address for RFID Device
        /// </summary>
        public string IpTerminal
        {
            get;
            set;
        }
        /// <summary>
        /// Port Number for RFID Device
        /// </summary>
        public int PortTerminal
        {
            get;
            set;
        }
        /// <summary>
        /// Index Number RFID Device
        /// </summary>
        public int IndexDevice
        {
            get;
            set;
        }
        /// <summary>
        ///  RFID Device Password
        /// </summary>
        public int DevicePassword
        {
            get;
            set;
        }
        #endregion

        private Thread ThreadWork;

        private bool DeviceIsConnected
        {
            get;
            set;
        }

        public Communication()
        {
            FPCLOCK.CreateControl();
        }

        private int vTMN = 0, //MachineNumber
            EnrollNumber = 0, //EnrollNumber
            vSMN = 0,//MachineNumber
            vVM = 0, //VerifyMode
            vYY = 0, // Year
            vMon = 0, // Month
            vDD = 0, // Date
            vHH = 0, // Hour
            vMin = 0; // Minute
        
        public void Start(System.Windows.Forms.Form uiForm)
        {
            this.FPCLOCK.HandleCreated += FPCLOCK_HandleCreated;
            uiForm.Controls.Add(this.FPCLOCK);
            this.FPCLOCK.Visible = false;

            string ipAd = this.IpTerminal;
            DeviceIsConnected = FPCLOCK.SetIPAddress(ref ipAd, this.PortTerminal, this.DevicePassword);
            if (this.FPCLOCK.OpenCommPort(this.IndexDevice))
            {
                if (DeviceIsConnected)
                {
                    this.ThreadWork = new Thread(ThreadRun);
                    this.ThreadWork.Start();
                }
            }
        }

        private void FPCLOCK_HandleCreated(object sender, EventArgs e)
        {
            
        }

        public void Stop()
        {
            this.DeviceIsConnected = false;
            
        }

        private void EnableDevice(int mode)
        {
            
            this.FPCLOCK.Invoke(new Action(() => this.EnableDeviceInvoke(mode)));
        }

        private void EnableDeviceInvoke(int mode)
        {
            this.FPCLOCK.EnableDevice(this.IndexDevice, mode);
        }

        private bool ReadAllData()
        {
            return this.ReadAllDataInvoke();
        }

        private bool ReadAllDataInvoke()
        {
            return this.FPCLOCK.ReadAllGLogData(this.IndexDevice);
        }

        private void GetAllData()
        {
            this.FPCLOCK.Invoke(new Action(() => this.GetAllDataInvoke()));
        }

        private void GetAllDataInvoke()
        {
            int lastNumber = 0;
            DateTime dt = DateTime.MinValue;
            while (true)
            {
                int enrollNumber = 0;
                this.FPCLOCK.GetAllGLogData(this.IndexDevice, ref vTMN, ref enrollNumber, ref vSMN, ref vVM, ref vYY, ref vMon, ref vDD, ref vHH, ref vMin);

                if (enrollNumber != 0)
                {
                    lastNumber = enrollNumber;
                    dt = new DateTime(vYY, vMon, vDD, vHH, vMin, 0);
                }
                else
                    break;
            }
            //Event EnrollNumber
            if (lastNumber > 0)
            {
                RFIDEvent eventa = new RFIDEvent(lastNumber, dt);
                UpdateRfidData(this, eventa);
                Thread.Sleep(100);
                //Delete LastRecord
                this.FPCLOCK.EmptyGeneralLogData(this.IndexDevice);
            }
        }

        private void ThreadRun()
        {
            try
            {
                while (DeviceIsConnected)
                {

                    this.EnableDevice(0);
                    bool HasData = this.ReadAllData();

                    if (HasData)
                    {
                        this.GetAllData();
                    }

                    this.EnableDevice(1);

                    Thread.Sleep(500);
                }
                this.FPCLOCK.CloseCommPort();
            }
            catch(Exception ThreadError)
            {
                try
                {
                    this.FPCLOCK.CloseCommPort();
                }
                catch (Exception ex)
                {
                    Logger.Output("RFID_logger.txt", ex);
                    return;
                }
                Logger.Output("RFID_logger.txt",ThreadError);
            }
        }
    }
}
