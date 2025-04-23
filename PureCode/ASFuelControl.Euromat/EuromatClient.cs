using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace ASFuelControl.Euromat
{
    public class EuromatClient
    {
        public string ServerIp { set; get; }
        public int ServrerPort { set; get; }

        public Common.IController Controller { set; get; }

        public void PumpNotification(int pumpNumber, int nozzlIndex)
        {

            string str = "nz";
            str = str + string.Format("{0:00}", pumpNumber);
            if (nozzlIndex < 0)
                str = str + "0";
            else
                str = str + (nozzlIndex + 1).ToString();

            TcpClient server = new TcpClient();
            try
            {
                server.Connect(this.ServerIp, this.ServrerPort);
            }
            catch
            {
                return;
            }
            Byte[] buffer = System.Text.UTF8Encoding.UTF8.GetBytes(str);
            server.Client.Send(buffer);
            if(nozzlIndex < 0)
                return;
            byte[] recBuffer = new byte[100];
            server.Client.Receive(recBuffer);

        }
    }
}
