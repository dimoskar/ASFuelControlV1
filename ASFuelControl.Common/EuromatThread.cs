using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;

namespace ASFuelControl.Common
{
    public class EuromatThread
    {
        private static EuromatThread instance = null;

        public static EuromatThread Instance
        {
            get
            {
                if (instance == null)
                    instance = new EuromatThread();
                return instance;
            }
        }

        private System.Threading.Thread thread = null;
        private bool stopThread = false;

        ConcurrentQueue<FuelPointControllerCalss> fuelPoints = new ConcurrentQueue<FuelPointControllerCalss>();

        public void ClearFuelPumps()
        {
            this.fuelPoints = new ConcurrentQueue<FuelPointControllerCalss>();
        }

        public void InitilizePump(Common.IController controller, int channel, int address, int euromatNumber)
        {
            if (!controller.EuromatEnabled)
                return;
            
            this.fuelPoints.Enqueue(new FuelPointControllerCalss()
            {
                Controller = controller,
                Channel = channel,
                Address = address,
                EuromatServer = controller.EuromatIp,
                EuromatPort = controller.EuromatPort,
                EuromatNumber = euromatNumber
            });
        }

        public void FuelPointNozzle(int channel, int address, int nozzle)
        {
            FuelPointControllerCalss fpc = this.fuelPoints.Where(f => f.Channel == channel && f.Address == address).FirstOrDefault();
            if (fpc == null)
                return;
            byte[] ret = SocketHandler.SendNzCommand(fpc.EuromatServer, fpc.EuromatPort, fpc.EuromatNumber, nozzle);
            if (this.EvaluateNzResponse(ret, fpc))
            {
                fpc.Controller.AuthorizeDispenser(channel, address);
            }
        }

        public bool TransactionCompleted(int channel, int address, int nozzle, decimal price, decimal totalAmount, decimal totalVol, int invNumber)
        {
            FuelPointControllerCalss fpc = this.fuelPoints.Where(f => f.Channel == channel && f.Address == address).FirstOrDefault();
            if (fpc == null)
                return false;

            byte[] retNz = SocketHandler.SendNzCommand(fpc.EuromatServer, fpc.EuromatPort, fpc.EuromatNumber, 0);
            if (!EvaluateAckResponse(retNz, fpc))
                return false;

            byte[] ret = SocketHandler.SendFtCommand(fpc.EuromatServer, fpc.EuromatPort, fpc, nozzle, price, totalAmount, totalVol);
            string nrData = EvaluateFtResponse(ret, fpc, invNumber);
            
            SocketHandler.SendNRCommand(fpc.EuromatServer, fpc.EuromatPort, fpc, nrData);
            if (nrData.Contains("*********"))
                return false;
            return true;
        }

        private bool EvaluateAckResponse(Byte[] response, FuelPointControllerCalss fpc)
        {
            string ret = Encoding.ASCII.GetString(response, 0, response.Length);
            if (ret.Contains("AK"))
                return true;
            return false;
        }

        private bool EvaluateNzResponse(Byte[] response, FuelPointControllerCalss fpc)
        {
            string ret = Encoding.ASCII.GetString(response, 0, response.Length);
            if (ret.Contains("CT"))
            {
                int ctIndex = ret.IndexOf("CT") + 2;

                fpc.RateType = (RateTypeEnum)int.Parse(ret.Substring(ctIndex)[6].ToString());
                fpc.RateTypeText = ret[6].ToString();
                fpc.TransType = (TransTypeEnum)int.Parse(ret.Substring(ctIndex)[5].ToString());
                
                string[] vals = ret.Substring(ctIndex + 7).Split('|');
                fpc.CurrentTransactionId = vals[vals.Length - 1];
                if (fpc.RateType == RateTypeEnum.Both)
                {
                    for (int i = 0; i < vals.Length % 4; i++)
                    {
                        fpc.AddNozzleData
                        (
                            decimal.Parse(vals[i * 4].Replace(".", System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator)),
                            decimal.Parse(vals[i * 4 + 1].Replace(".", System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator)),
                            decimal.Parse(vals[i * 4 + 2].Replace(".", System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator)),
                            decimal.Parse(vals[i * 4 + 3].Replace(".", System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator))
                        );
                    }
                }
                return true;
            }
            else if(ret.Contains("DT"))
            {
                return false;
            }
            return false;
        }

        private string EvaluateFtResponse(Byte[] response, FuelPointControllerCalss fpc, int invNumber)
        {
            string resp = Encoding.ASCII.GetString(response, 0, response.Length);
            string price = new string(resp.Take(9).ToArray()).Replace(" ", "");
            string volume = new string(resp.Skip(11).Take(9).ToArray()).Replace(" ", "");

            string plates = new string(resp.Skip(23).Take(10).ToArray()).Trim();

            string nz = new string(resp.Skip(34).Take(1).ToArray()).Replace(" ", "");
            string dp = new string(resp.Skip(38).Take(2).ToArray()).Replace(" ", "");
            string trnr = new string(resp.Skip(45).Take(6).ToArray()).Replace(" ", "");
            string time = new string(resp.Skip(52).Take(5).ToArray()).Replace(" ", "");
            string dt = new string(resp.Skip(58).Take(8).ToArray()).Replace(" ", "");
            string ftd = new string(resp.Skip(67).Take(10).ToArray()).Replace(" ", "");
            string ftt = new string(resp.Skip(78).Take(4).ToArray()).Replace(" ", "");
            string id = new string(resp.Skip(100).Take(19).ToArray()).Replace(" ", "");
            string odometer = new string(resp.Skip(135).Take(8).ToArray()).Replace(" ", "");
            string up = new string(resp.Skip(153).Take(7).ToArray()).Replace(" ", "");
            if (odometer == "")
                odometer = "0";
            return string.Format("{0:N2}|{1:N2}|{2}|{3}|{4}|{5}|{6}|{7}|{8}|{9:N3}|{10}|{11}\r\n", price, volume, plates, time, dt, ftd, ftt, id, odometer, up, invNumber, dp);
        }
    }

    class SocketHandler
    {
        public static string GetNzCommand(int fuelPump, int nozzle)
        {
            return string.Format("NZ{0:00}{1}/r/n", fuelPump, nozzle);

        }

        public static string GetAckCommand(int fuelPump)
        {
            return string.Format("AK{0:00}0/r/n", fuelPump);

        }

        public static string GetFtCommand(FuelPointControllerCalss fc, int nz, decimal fulePrice, decimal totalPrice, decimal totalVolume)
        {
            return string.Format("ft{0:00}0{1}{2}{3:N3}|{4:N2}|{5:N2}/r/n", fc.EuromatNumber, nz, (int)fc.RateType, fulePrice, totalPrice, totalVolume).Replace(",", ".");
        }

        public static string GetNrCommand(FuelPointControllerCalss fc, string data)
        {
            return string.Format("NR:{0}", data);
        }

        private static Socket ConnectSocket(string server, int port)
        {
            Socket s = null;
            IPHostEntry hostEntry = null;

            // Get host related information.
            hostEntry = Dns.GetHostEntry(server);

            // Loop through the AddressList to obtain the supported AddressFamily. This is to avoid
            // an exception that occurs when the host IP Address is not compatible with the address family
            // (typical in the IPv6 case).
            foreach (IPAddress address in hostEntry.AddressList)
            {
                IPEndPoint ipe = new IPEndPoint(address, port);
                Socket tempSocket =
                    new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                tempSocket.Connect(ipe);

                if (tempSocket.Connected)
                {
                    s = tempSocket;
                    break;
                }
                else
                {
                    continue;
                }
            }
            return s;
        }

        // This method requests the home page content for the specified server.
        public static Byte[] SendNzCommand(string server, int port, int fp, int nozzle)
        {
            string data = GetNzCommand(fp, nozzle);
            Byte[] bytesSent = Encoding.ASCII.GetBytes(data);
            Byte[] bytesReceived = new Byte[256];

            // Create a socket connection with the specified server and port.
            Socket s = ConnectSocket(server, port);

            if (s == null)
                return (Encoding.ASCII.GetBytes("Connection failed"));

            // Send request to the server.
            s.Send(bytesSent, bytesSent.Length, 0);

            // Receive the server home page content.
            int bytes = 0;
            string page = "";
            List<byte> recieveBuffer = new List<byte>();
            // The following will block until te page is transmitted.
            bool dataOK = false;
            DateTime dtStart = DateTime.Now;
            do
            {
                bytes = s.Receive(bytesReceived, bytesReceived.Length, 0);
                recieveBuffer.AddRange(bytesReceived);
                page = page + Encoding.ASCII.GetString(bytesReceived, 0, bytes);
                if (page.ToUpper().Contains("AK") || page.ToUpper().Contains("CT") || DateTime.Now.Subtract(dtStart).TotalSeconds > 20)
                    dataOK = true;
                
                System.Threading.Thread.Sleep(200);
            }
            while (s.Available > 0 || !dataOK);
            if (System.IO.File.Exists("Euromat.log"))
                System.IO.File.WriteAllText("Euromat.log", string.Format("{1:dd/MM/yyyy HH:mm:ss.fff} {0}\r\n", page, DateTime.Now));
            bytesSent = Encoding.ASCII.GetBytes(GetAckCommand(fp));
            s.Send(bytesSent, bytesSent.Length, 0);
            System.Threading.Thread.Sleep(200);
            return recieveBuffer.ToArray();
        }

        public static Byte[] SendFtCommand(string server, int port, FuelPointControllerCalss fc, int nozzle, decimal fulePrice, decimal totalPrice, decimal totalVolume)
        {
            string data = GetFtCommand(fc, nozzle, fulePrice, totalPrice, totalVolume);
            Byte[] bytesSent = Encoding.ASCII.GetBytes(data);
            Byte[] bytesReceived = new Byte[256];

            // Create a socket connection with the specified server and port.
            Socket s = ConnectSocket(server, port);

            if (s == null)
                return (Encoding.ASCII.GetBytes("Connection failed"));

            // Send request to the server.
            s.Send(bytesSent, bytesSent.Length, 0);

            // Receive the server home page content.
            int bytes = 0;
            string page = "";
            List<byte> recieveBuffer = new List<byte>();
            // The following will block until te page is transmitted.
            bool dataOK = false;
            DateTime dtStart = DateTime.Now;
            do
            {
                bytes = s.Receive(bytesReceived, bytesReceived.Length, 0);
                recieveBuffer.AddRange(bytesReceived);
                page = page + Encoding.ASCII.GetString(bytesReceived, 0, bytes);
                if (page.Contains("**********") && page.Contains(new string(fc.CurrentTransactionId.Skip(fc.CurrentTransactionId.Length - 6).ToArray())))
                    dataOK = true;
                else if(page.Contains(fc.CurrentTransactionId))
                    dataOK = true;
                if (DateTime.Now.Subtract(dtStart).TotalSeconds > 20)
                    dataOK = true;

                System.Threading.Thread.Sleep(200);
            }
            while (s.Available > 0 || !dataOK);
            if (System.IO.File.Exists("Euromat.log"))
                System.IO.File.WriteAllText("Euromat.log", string.Format("{1:dd/MM/yyyy HH:mm:ss.fff} {0}\r\n", page, DateTime.Now));
            return recieveBuffer.ToArray();
        }

        public static Byte[] SendNRCommand(string server, int port, FuelPointControllerCalss fc, string nrdata)
        {
            string data = GetNrCommand(fc, nrdata);
            Byte[] bytesSent = Encoding.ASCII.GetBytes(data);
            Byte[] bytesReceived = new Byte[256];

            // Create a socket connection with the specified server and port.
            Socket s = ConnectSocket(server, port);

            if (s == null)
                return (Encoding.ASCII.GetBytes("Connection failed"));

            // Send request to the server.
            s.Send(bytesSent, bytesSent.Length, 0);

            // Receive the server home page content.
            int bytes = 0;
            string page = "";
            List<byte> recieveBuffer = new List<byte>();
            // The following will block until te page is transmitted.
            bool dataOK = false;
            DateTime dtStart = DateTime.Now;
            do
            {
                bytes = s.Receive(bytesReceived, bytesReceived.Length, 0);
                recieveBuffer.AddRange(bytesReceived);
                page = page + Encoding.ASCII.GetString(bytesReceived, 0, bytes);
                if (page.ToUpper().Contains("AKNR"))
                    dataOK = true;
                if (DateTime.Now.Subtract(dtStart).TotalSeconds > 20)
                    dataOK = true;

                System.Threading.Thread.Sleep(200);
            }
            while (s.Available > 0 || !dataOK);
            if (System.IO.File.Exists("Euromat.log"))
                System.IO.File.WriteAllText("Euromat.log", string.Format("{1:dd/MM/yyyy HH:mm:ss.fff} {0}\r\n", page, DateTime.Now));
            return recieveBuffer.ToArray();
        }
    }

    class FuelPointControllerCalss
    {
        private List<NozzleClass> nozzleClasses = new List<NozzleClass>();

        public Common.IController Controller { set; get; }
        public int Channel { set; get; }
        public int Address { set; get; }
        public string EuromatServer { set; get; }
        public int EuromatPort { set; get; }
        public int EuromatNumber { set; get; }
        public EuromatStateEnum EuromatState { set; get; }
        public string CurrentTransactionId { set; get; }
        public RateTypeEnum RateType { set; get; }
        public TransTypeEnum TransType { set; get; }
        public string RateTypeText { set; get; }
        public void ClearNozzleData()
        {
            this.nozzleClasses.Clear();
        }
        public void AddNozzleData(decimal vc, decimal ac, decimal vcr, decimal acr)
        {
            this.nozzleClasses.Add(new NozzleClass()
            {
                VolumeCash = vc,
                AmountCash = ac,
                VolumeCredit = vcr,
                AmountCredit = acr
            });
        }    
    }

    class NozzleClass
    {
        public decimal VolumeCash { set; get; }
        public decimal AmountCash { set; get; }
        public decimal VolumeCredit { set; get; }
        public decimal AmountCredit { set; get; }
    }

    enum EuromatStateEnum
    {
        Idle,
        AckAwaiting,
        CTAwaiting,
        FTAwaiting,
    }
}
