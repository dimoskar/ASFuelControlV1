using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;


namespace ASFuelControl.TLS4B_Lan
{
    public class TcpCom
    {

        public delegate void DataReceivedEventHandler(String data);
        public event DataReceivedEventHandler OnDataRecievedEvent;


        public delegate void OnConnectEventHandler(bool status);
        public event OnConnectEventHandler OnConnectEvent;

        private IPAddress ipAddress;
        private int port;

        private Socket socket;
        private byte[] readerBuffer = new byte[256];

        public TcpCom(string _ip, int _port)
        {
            ipAddress = IPAddress.Parse(_ip);
            port = _port;
        }

        public void Connect()
        {
            try
            {
                if (socket != null && socket.Connected)
                {
                    socket.Shutdown(SocketShutdown.Both);
                    System.Threading.Thread.Sleep(10);
                    socket.Close();
                }

                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                IPEndPoint epServer = new IPEndPoint(ipAddress, port);

                socket.Blocking = false;
                AsyncCallback onconnect = new AsyncCallback(OnConnect);
                socket.BeginConnect(epServer, onconnect, socket);
            }
            catch (Exception ex)
            {
                OnConnectEvent(false);
            }
        }

        public bool IsConnected()
        {
            if (socket != null)
            {
                return socket.Connected;
            }
            return false;
        }

        private void OnConnect(IAsyncResult ar)
        {
            Socket _socket = (Socket)ar.AsyncState;

            try
            {
                if (_socket.Connected)
                {
                    SetupRecieveCallback(_socket);
                    OnConnectEvent(true);
                }
                else
                {
                    OnConnectEvent(false);
                }
            }
            catch (Exception ex)
            {

            }
        }

        private void SetupRecieveCallback(Socket _socket)
        {
            try
            {
                AsyncCallback recieveData = new AsyncCallback(OnRecievedData);
                _socket.BeginReceive(readerBuffer, 0, readerBuffer.Length, SocketFlags.None, recieveData, _socket);
            }
            catch (Exception ex)
            {
                Dispose();
            }
        }

        private void OnRecievedData(IAsyncResult ar)
        {
            Socket _socket = (Socket)ar.AsyncState;

            if (IsConnected())
            {
                try
                {
                    int nBytesRec = _socket.EndReceive(ar);
                    if (nBytesRec > 0)
                    {
                        string sRecieved = "";
                        for (int i = 0; i < nBytesRec; i++)
                        {
                            sRecieved += (char)readerBuffer[i];
                        }
                        OnDataRecievedEvent(sRecieved);
                        SetupRecieveCallback(_socket);
                    }
                    else
                    {
                        Dispose();
                    }
                }
                catch (Exception ex)
                {
                    Dispose();
                }
            }
        }

        public bool Write(String _data)
        {
            // Check Connection
            if (IsConnected())
            {
                try
                {
                    Byte[] byteDateLine = Encoding.ASCII.GetBytes(_data.ToCharArray());
                    socket.Send(byteDateLine, byteDateLine.Length, 0);
                    return true;
                }
                catch (Exception ex)
                {
                    Dispose();
                    return false;
                    //throw new Exception("Data Writing Operation Failed");
                }
            }
            else
            {
                return false;
            }
        }

        public bool Write(byte[] _data)
        {

            if (IsConnected())
            {
                try
                {
                    socket.Send(_data, _data.Length, 0);
                    return true;
                }
                catch (Exception ex)
                {
                    Dispose();
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public void Dispose()
        {
            if (socket != null && socket.Connected)
            {
                OnConnectEvent(false);
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
        }

        public void Disconnect()
        {
            if (socket != null && socket.Connected)
            {
                OnConnectEvent(false);
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
        }
    }

}
