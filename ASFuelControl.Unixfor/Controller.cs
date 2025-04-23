using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASFuelControl.Unixfor
{
    public class Controller
    {
        SimpleTCP.SimpleTcpServer server = new SimpleTCP.SimpleTcpServer();
        int serverPort = 0;
        static string unixforVersion = "2.0";

        public VirtualDevices.VirtualDispenser[] FuelPumps { set; get; }

        List<System.Net.Sockets.TcpClient> inProcess = new List<System.Net.Sockets.TcpClient>();
        Dictionary<System.Net.Sockets.TcpClient, string> clientMessages = new Dictionary<System.Net.Sockets.TcpClient, string>();

        public Controller(int port)
        {
            server.Delimiter = (byte)']';
            server.ClientConnected += Server_ClientConnected;
            server.ClientDisconnected += Server_ClientDisconnected;
            server.DataReceived += Server_DataReceived;
            serverPort = port;
        }

        public void Start()
        {
            server.Start(this.serverPort);
        }

        public void Stop()
        {
            server.Stop();
        }

        public string ExecuteCommand(string cmd)
        {
            string resp = "";
            var parms = cmd.Replace("[", "").Replace("]", "").Split('|');
            switch(parms[3])
            {
                case "LOGIN":
                    resp = Login(parms);break;
                case "KEEPALIVE":
                    resp = KeepAlive(parms); break;
                case "STATIONCONF":
                    resp = StationConf(parms);break;
                case "PRODINFO":
                    resp = ProdInfo(parms);break;
                case "PUMPINFO":
                    resp = PumpInfo(parms);break;
                case "FUELAUTH":
                    resp = FuelAuth(parms);break;
                case "FUELSTAT":
                    resp = FuelStat(parms);break;
                case "FUELCANCEL":
                    resp = FuelCancel(parms);break;
                case "FUELTERMINATE":
                    resp = FuelTerminate(parms);break;
                case "FUELDETAILS":
                    resp = FuelDetails(parms);break;
                case "FUELFINALIZE":
                    resp = FuelFinalize(parms); break;
                case "PUMPSTATUS":
                    resp = PumpStatus(parms); break;
                case "RECEIPTCONF":
                    resp = ReceiptConf(parms); break;
                case "PAYMENTCONF":
                    resp = PaymentConf(parms); break;
                case "STORERECEIPT":
                    resp = StoreReceipt(parms); break;
            }
            resp = "[" + resp + "]";
            return resp;
        }

        public string Login(string[] parms)
        {
            string resp = string.Format("{0}|{1}|{2}|{3}|{4}", parms[1], parms[2], parms[3], unixforVersion, "0");
            int len = resp.Length + resp.Length.ToString().Length;
            return len.ToString() + "|" + resp;
        }

        public string KeepAlive(string[] parms)
        {
            return string.Format("{0}|{1}|{2}|{3}", parms[0], parms[1], parms[2], parms[3]);
        }

        public string StationConf(string[] parms)
        {
            return string.Format("{0}|{1}|{2}|{3}", parms[0], parms[1], parms[2], parms[3]);
        }

        public string ProdInfo(string[] parms)
        {
            return string.Format("{0}|{1}|{2}|{3}", parms[0], parms[1], parms[2], parms[3]);
        }

        public string PumpInfo(string[] parms)
        {
            return string.Format("{0}|{1}|{2}|{3}", parms[0], parms[1], parms[2], parms[3]);
        }

        public string FuelAuth(string[] parms)
        {
            return string.Format("{0}|{1}|{2}|{3}", parms[0], parms[1], parms[2], parms[3]);
        }

        public string FuelStat(string[] parms)
        {
            return string.Format("{0}|{1}|{2}|{3}", parms[0], parms[1], parms[2], parms[3]);
        }

        public string FuelCancel(string[] parms)
        {
            return string.Format("{0}|{1}|{2}|{3}", parms[0], parms[1], parms[2], parms[3]);
        }

        public string FuelTerminate(string[] parms)
        {
            return string.Format("{0}|{1}|{2}|{3}", parms[0], parms[1], parms[2], parms[3]);
        }

        public string FuelDetails(string[] parms)
        {
            return string.Format("{0}|{1}|{2}|{3}", parms[0], parms[1], parms[2], parms[3]);
        }

        public string FuelFinalize(string[] parms)
        {
            return string.Format("{0}|{1}|{2}|{3}", parms[0], parms[1], parms[2], parms[3]);
        }

        public string PumpStatus(string[] parms)
        {
            int pumpIndex = int.Parse(parms[4]);
            var pump = this.FuelPumps.FirstOrDefault(f => f.OfficialNumber == pumpIndex);
            if(pump == null)
            {
                return string.Format("{0}|{1}|{2}|{3}", parms[0], parms[1], parms[2], parms[3]);
            }
            else
            {
                return string.Format("{0}|{1}|{2}|{3}", parms[0], parms[1], parms[2], parms[3]);
            }
            
        }

        public string ReceiptConf(string[] parms)
        {
            return string.Format("{0}|{1}|{2}|{3}", parms[0], parms[1], parms[2], parms[3]);
        }

        public string PaymentConf(string[] parms)
        {
            return string.Format("{0}|{1}|{2}|{3}", parms[0], parms[1], parms[2], parms[3]);
        }

        public string StoreReceipt(string[] parms)
        {
            return string.Format("{0}|{1}|{2}|{3}", parms[0], parms[1], parms[2], parms[3]);
        }

        private void Server_ClientConnected(object sender, System.Net.Sockets.TcpClient e)
        {
            if (!this.clientMessages.ContainsKey(e))
                clientMessages.Add(e, "");
        }

        private void Server_ClientDisconnected(object sender, System.Net.Sockets.TcpClient e)
        {
            if (this.clientMessages.ContainsKey(e))
                clientMessages.Remove(e);
        }

        private void Server_DataReceived(object sender, SimpleTCP.Message e)
        {
            while (inProcess.Contains(e.TcpClient))
            {
                System.Threading.Thread.Sleep(50);
            }
            inProcess.Add(e.TcpClient);
            try
            {
                if (!this.clientMessages.ContainsKey(e.TcpClient))
                {
                    return;
                }
                string msg = e.MessageString;
                if (!msg.Contains("]"))
                {
                    this.clientMessages[e.TcpClient] = this.clientMessages[e.TcpClient] + msg;
                    return;
                }
                msg = this.clientMessages[e.TcpClient] + msg;
                string rem = "";
                if (!msg.EndsWith("]"))
                {
                    int pos = msg.LastIndexOf(']');
                    rem = msg.Substring(pos);
                    rem = rem.Replace("]", "");
                }
                string[] commands = msg.Split(']');
                foreach (string cmd in commands)
                {
                    if (!cmd.StartsWith("["))
                        continue;
                    string respCmd = this.ExecuteCommand(cmd);
                    byte[] buffer = System.Text.ASCIIEncoding.ASCII.GetBytes(respCmd);
                    e.TcpClient.Client.Send(buffer);
                }
                this.clientMessages[e.TcpClient] = rem;
            }
            catch(Exception ex)
            {

            }
            finally
            {
                inProcess.Remove(e.TcpClient);
            }
        }
    }
}
