using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Ports;

using System.Threading;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace MillenniumAPI
{ //Delegate


    class Program
    {

        static void Main(string[] args)
        {
             //SerialPort serialPort = new SerialPort();
             // serialPort.PortName="COM5";
             // serialPort.Open();
             // if (serialPort.IsOpen)
             // {
              dispenserCommands dispenser = new dispenserCommands();
              int Address = 7;
              int Channel = 2;
              byte[] buffer = new byte[] { };

              // how to build a cmd 
              buffer = dispenser.createCmd(Address, Channel, CommandTypeEnum.FetchBuffer);
              buffer = dispenser.createCmd(Address, Channel, CommandTypeEnum.Authorize);
              buffer = dispenser.createCmd(Address, Channel, CommandTypeEnum.SendPrices, 1547); //price as integer
             
              //how to evaluate a buffer
              //example.
              buffer = new byte[] { 0xFD, 0x02, 0x01, 0x01, 0x00, 0x00, 0x80, 0x00, 0x20, 0x04, 0x22, 0x21, 0x12, 0x00, 0x64, 0x00
              ,0x01, 0x02, 0x12, 0x00, 0x15, 0x01, 0x02, 0x14, 0x02, 0x00, 0x00, 0x05, 0x05, 0x04, 0x00, 0x11, 0x45, 0x00, 0x06, 0x05
              ,0x04, 0x00, 0x09, 0x37, 0x00, 0xFC, 0x30, 0x32, 0x35, 0x37, 0xFB,0xFD,0x02,0x01,0x01,0x00,0x00,0x20,0x00,0x08,0x01,0x22
              ,0x14,0x01,0x02,0x15,0x01,0x00,0xFC,0x30,0x30,0x37,0x43,0xFB,0xFE,0xFD,0x02,0x01,0x01,0x00,0x00,0x20,0x00,0x08,0x01,0x22
              ,0x14,0x01,0x03,0x15,0x01,0x00,0xFC,0x30,0x30,0x37,0x44,0xFB,0xFE,0xFD,0x02,0x01,0x01,0x00,0x00,0x20,0x00,0x08,0x01,0x22
              ,0x14,0x01,0x04,0x15,0x01,0x00,0xFC,0x30,0x30,0x37,0x45,0xFB,0xFE,0xFD,0x02,0x01,0x01,0x00,0x00,0x20,0x00,0x08,0x01,0x22
              ,0x14,0x01,0x06,0x15,0x01,0x00,0xFC,0x30,0x30,0x38,0x30,0xFB,0xFE,0xFD,0x02,0x01,0x01,0x00,0x00,0x20,0x00,0x14,0x01,0x22
              ,0x22,0x05,0x04,0x00,0x11,0x10,0x00,0x23,0x05,0x04,0x00,0x09,0x08,0x00,0x1D,0x02,0x11,0x97,0xFC,0x30,0x31,0x41,0x42,0xFB};
              //ston parapananw buffer exei 5 entoles kai tyxaio thorybo
             



              List<Nozzle> affected_nozzles = new List<Nozzle>();
              affected_nozzles = dispenser.evaluateBuffer(buffer);

             foreach(Nozzle  affected_nozzle in affected_nozzles)
             {  
                Console.WriteLine("Dispenser ID: {0}, FuelPoint: {1}", affected_nozzle.dispenserID, affected_nozzle.fuelPointChannel);
                Console.WriteLine("Last response: {0}", affected_nozzle.LastResponseType);
                Console.WriteLine("STATE: {0}",affected_nozzle.Status);
                
                if (affected_nozzle.checkLCD)
                {
                    Console.WriteLine("Litra: {0}",affected_nozzle.displayLitres);
                    Console.WriteLine("Euro: {0}", affected_nozzle.displayPrice);
                }
                Console.WriteLine(); 
                

             }
             Console.ReadLine();
 
                




               
                //int Address = 7;
                //int Channel = 2;
                
                //buffer = dispenser.createCmd(Address, Channel, CommandTypeEnum.SendPrices,1547);
                //global::System.Windows.Forms.MessageBox.Show(BitConverter.ToString(buffer.ToArray()));
                
                ////serialPort.Write(buffer, 0, buffer.Length);
                // System.Threading.Thread.Sleep(200);
                
                // buffer = dispenser.createCmd(Address, Channel, CommandTypeEnum.Authorize);
                ////serialPort.Write(buffer, 0, buffer.Length);

                //global::System.Windows.Forms.MessageBox.Show(BitConverter.ToString(buffer.ToArray()));
                
             //}
             //else
             //{
             //    throw new Exception("?"); 
             //}



        }



    }
}