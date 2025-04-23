using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace ASFuelControl.Windows.Threads
{
    public class EuromatTransmitter
    {

        //Fuel Total Amount	    Number, (2 decimals)
        //Fuel Quantity 	    Number, (2 decimals)
        //Vehicle Plate	        String 
        //Transaction Time 	    HH:MM
        //Transaction Date	    DD/MM/YY
        //Fuel Type Description	String (size as in TS file spec)
        //Fuel Type Code	    String (size as in TS file spec)
        //Vehicle Unit ID	    String (size as in TS file spec)
        //Odometer Value	    Integer as string, 8 digits, ‘0’ left padded
        //Fuel Rate	Number,     (3 decimals)
        //Receipt Number	    String (DocSeries & No combined)

        public static EutromatStateEnum SendTransactionEndCommand(string euromatIp, int euromatPort, Common.Sales.SaleData sale, int invoiceNumber)
        {
            if (sale.EuromatParameters.Length < 160)
            {
                System.IO.File.AppendAllText("Euromat.dat", "ERROR DATA : " + sale.EuromatParameters);
                return EutromatStateEnum.Error;
            }
            System.IO.File.AppendAllText("Euromat.dat", sale.EuromatParameters);

            string price = new string(sale.EuromatParameters.Take(9).ToArray()).Replace(" ", "");
            string volume = new string(sale.EuromatParameters.Skip(11).Take(9).ToArray()).Replace(" ", "");

            string plates = new string(sale.EuromatParameters.Skip(23).Take(10).ToArray()).Trim();
            
            string nz = new string(sale.EuromatParameters.Skip(34).Take(1).ToArray()).Replace(" ", "");
            string dp = new string(sale.EuromatParameters.Skip(38).Take(2).ToArray()).Replace(" ", "");
            string trnr = new string(sale.EuromatParameters.Skip(45).Take(6).ToArray()).Replace(" ", "");
            string time = new string(sale.EuromatParameters.Skip(52).Take(5).ToArray()).Replace(" ", "");
            string dt = new string(sale.EuromatParameters.Skip(58).Take(8).ToArray()).Replace(" ", "");
            string ftd = new string(sale.EuromatParameters.Skip(67).Take(10).ToArray()).Replace(" ", "");
            string ftt = new string(sale.EuromatParameters.Skip(78).Take(4).ToArray()).Replace(" ", "");
            string id = new string(sale.EuromatParameters.Skip(100).Take(19).ToArray()).Replace(" ", "");
            string odometer = new string(sale.EuromatParameters.Skip(135).Take(8).ToArray()).Replace(" ", "");
            string up = new string(sale.EuromatParameters.Skip(153).Take(7).ToArray()).Replace(" ", "");
            if (odometer == "")
                odometer = "0";

            if (plates.Contains("*"))
                return EutromatStateEnum.MasterUsed;



            using (TcpClient server = new TcpClient())
            {

                try
                {
                    try
                    {
                        server.ReceiveTimeout = 15000;
                        server.Connect(euromatIp, euromatPort + 2);

                    }
                    catch (Exception ex)
                    {
                        System.IO.File.AppendAllText("Euromat.log", "SendTransactionEndCommand :: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff") + " - Server Connect\r\n");
                        System.IO.File.AppendAllText("Euromat.log", ex.Message);
                        return EutromatStateEnum.Error;
                    }



                    string data = string.Format("{0:N2}|{1:N2}|{2}|{3}|{4}|{5}|{6}|{7}|{8}|{9:N3}|{10}|{11}\r\n", price, volume, plates, time, dt, ftd, ftt, id, odometer, up, invoiceNumber, dp);

                    string str = "NR:";
                    str = str + data;
                    System.IO.File.AppendAllText("Euromat.dat", str);

                    server.Client.ReceiveTimeout = 1000;
                    Byte[] buffer = System.Text.UTF8Encoding.ASCII.GetBytes(str);
                    using (NetworkStream nwStream = server.GetStream())
                    {
                        nwStream.Write(buffer, 0, buffer.Length);
                        nwStream.ReadTimeout = 15000;
                        //server.Client.Send(buffer);

                        try
                        {
                            byte[] bufferRecieve = new byte[server.Client.ReceiveBufferSize];

                            //---read incoming stream---
                            int bytesRead = nwStream.Read(bufferRecieve, 0, server.Client.ReceiveBufferSize);

                            string ack = Encoding.ASCII.GetString(bufferRecieve, 0, bytesRead);
                            System.IO.File.AppendAllText("Euromat.log", "RECIEVE : " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff") + "    " + ack + "\r\n");
                        }
                        catch (Exception ex)
                        {
                            System.IO.File.AppendAllText("Euromat.log", "SendTransactionEndCommand :: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff") + " - Read Response\r\n");
                            System.IO.File.AppendAllText("Euromat.log", ex.Message);
                        }
                    }
                }
                catch (Exception ex1)
                {
                    System.IO.File.AppendAllText("Euromat.log", "SendTransactionEndCommand :: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff") + " - General Error\r\n");
                    System.IO.File.AppendAllText("Euromat.log", ex1.Message);
                    return EutromatStateEnum.Error;
                }
                finally
                {
                    try
                    {
                        server.Close();
                    }
                    catch
                    {
                    }
                }
            }
            return EutromatStateEnum.OK;
        }

        public enum EutromatStateEnum
        {
            OK,
            Error,
            MasterUsed
        }
    }
}
