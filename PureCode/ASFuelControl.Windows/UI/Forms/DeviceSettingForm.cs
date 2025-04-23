using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Telerik.WinControls.UI;

namespace ASFuelControl.Windows.UI.Forms
{
    public partial class DeviceSettingForm : RadForm
    {
        Data.DatabaseModel database = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
        public DeviceSettingForm()
        {
            InitializeComponent();
            List<DeviceDescValue> list = new List<DeviceDescValue>();

            foreach (var it in Enum.GetValues(typeof(Common.Enumerators.DeviceTypeEnum)))
            {
                
                string resName = "DeviceType_" + it.ToString();
                string disp;
                string val = it.ToString();
                switch (val)
                {
                    case "Dispenser":
                    case "Tank":
                    case "Nozzle":
                    case "CommunicationController":
                        continue;
                }
                try
                {
                    disp = Properties.Resources.ResourceManager.GetString(resName);
                }
                catch
                {
                    disp = it.ToString();
                }
                DeviceDescValue item = new DeviceDescValue(disp, val);
                list.Add(item);
            }
            this.radDropDownList1.DataSource = list;
            this.radDropDownList1.DisplayMember = "DisplayDescription";
            this.radDropDownList1.ValueMember = "ValueDescription";
        }

        public void CreateDeviceSetting()
        {
            Data.DeviceSetting setting = new Data.DeviceSetting();
            setting.DeviceSettingId = Guid.NewGuid();
            this.database.Add(setting);
            setting.IsSerialNumber = true;
            setting.SettingKey = "Device_SerialNumber";
            
            this.deviceSettingBindingSource.DataSource = setting;
        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            if (this.database.HasChanges)
            {
                this.database.SaveChanges();
                this.DialogResult = System.Windows.Forms.DialogResult.OK;
            }
            else
                this.DialogResult = System.Windows.Forms.DialogResult.None;
        }

        private void radButton2_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        }
    }

    public class DeviceDescValue
    {
        public string DisplayDescription { private set; get; }
        public string ValueDescription { private set; get; }

        public DeviceDescValue(string disp, string val)
        {
            this.DisplayDescription = disp;
            this.ValueDescription = val;
        }
    }
}
