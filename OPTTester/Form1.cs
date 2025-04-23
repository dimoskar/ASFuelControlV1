using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OPTTester
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ASFuelControl.Unixfor.Controller ctrl = new ASFuelControl.Unixfor.Controller(32004);

            //ASFuelControl.VirtualDevices.VirtualNozzle nz1 = new ASFuelControl.VirtualDevices.VirtualNozzle();
            //nz1.FuelCode = "20";
            //nz1.FuelTypeDescription = "Diesel";
            //nz1.NozzleIndex = 1;
            //nz1.NozzleNumber = 1;

            //ASFuelControl.VirtualDevices.VirtualNozzle nz2 = new ASFuelControl.VirtualDevices.VirtualNozzle();
            //nz2.FuelCode = "21";
            //nz2.FuelTypeDescription = "Unleaded 95";
            //nz2.NozzleIndex = 2;
            //nz2.NozzleNumber = 2;

            //ASFuelControl.VirtualDevices.VirtualNozzle nz3 = new ASFuelControl.VirtualDevices.VirtualNozzle();
            //nz3.FuelCode = "20";
            //nz3.FuelTypeDescription = "Unleaded 100";
            //nz3.NozzleIndex = 1;
            //nz3.NozzleNumber = 1;

            //ASFuelControl.VirtualDevices.VirtualNozzle nz4 = new ASFuelControl.VirtualDevices.VirtualNozzle();
            //nz4.FuelCode = "21";
            //nz4.FuelTypeDescription = "Unleaded 95";
            //nz4.NozzleIndex = 2;
            //nz4.NozzleNumber = 2;

            //ASFuelControl.VirtualDevices.VirtualDispenser disp1 = new ASFuelControl.VirtualDevices.VirtualDispenser();
            //disp1.AddressId = 1;
            //disp1.ChannelId = 1;
            //disp1.DecimalPlaces = 3;
            //disp1.DispenserId = Guid.NewGuid();
            //disp1.DispenserNumber = 1;
            //disp1.OfficialNumber = 1;
            //disp1.Nozzles = new ASFuelControl.VirtualDevices.VirtualNozzle[] { nz1, nz2 };

            //ASFuelControl.VirtualDevices.VirtualDispenser disp2 = new ASFuelControl.VirtualDevices.VirtualDispenser();
            //disp2.AddressId = 2;
            //disp2.ChannelId = 1;
            //disp2.DecimalPlaces = 3;
            //disp2.DispenserId = Guid.NewGuid();
            //disp2.DispenserNumber = 2;
            //disp2.OfficialNumber = 2;
            //disp2.Nozzles = new ASFuelControl.VirtualDevices.VirtualNozzle[] { nz3, nz4 };

            //ctrl.FuelPumps = new ASFuelControl.VirtualDevices.VirtualDispenser[] { disp1, disp2 };
            ctrl.Start();
        }
    }
}
