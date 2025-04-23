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
{ 
    class Program
    {

        static void Main(string[] args)
        {
            DispenserCommands dispenser = new DispenserCommands();
            int Address = 1;
            int FuelPoint_id = 2;
            byte[] Buffer = new byte[] { };
            List<byte> megabuffer = new List<byte>();
              SerialPort serialPort = new SerialPort();
            
            
            
            serialPort.PortName="COM14";
              serialPort.Open();
              
            
            
            if (serialPort.IsOpen)
              {
                 


              //nea prosthikh 
              int VolumeDecimalPlaces = 2;
              int PriceDecimalPlaces = 2;


              List<MilleniumNozzle> Affected_nozzles = new List<MilleniumNozzle>();
              //dispenser.CreateCmd(Address, FuelPoint_id, CmdEnum.GetLastSalesId);        
              //Buffer = dispenser.CreateCmd(Address, FuelPoint_id, CmdEnum.RequestStatus);
              //serialPort.Write(Buffer, 0, Buffer.Length);

              
              Buffer = dispenser.CreateCmd(Address);
              serialPort.Write(Buffer, 0, Buffer.Length);
              Thread.Sleep(150);
              Byte[] combuffer = new byte[serialPort.BytesToRead];
              serialPort.Read(combuffer, 0, serialPort.BytesToRead);
              Affected_nozzles = dispenser.EvaluateBuffer(combuffer);

             
              foreach (MilleniumNozzle affected_nozzle in Affected_nozzles)
              {
                  //
                  Console.WriteLine("Dispenser ID: {0}, FuelPoint: {1}", affected_nozzle.DispenserID, affected_nozzle.FuelPointChannel);
                  Console.WriteLine("Last response: {0}", affected_nozzle.LastResponseType);
                  Console.WriteLine("STATE: {0}", affected_nozzle.Status);
                  
                  if (affected_nozzle.isNozzle && !affected_nozzle.isAuthorized)
                  {
                      Thread.Sleep(150);
                      Buffer = dispenser.CreateCmd(affected_nozzle.DispenserID,affected_nozzle.FuelPointChannel, CmdEnum.Authorize);
                      serialPort.Write(Buffer, 0, Buffer.Length);
                      Thread.Sleep(150);
                      Buffer = dispenser.CreateCmd(affected_nozzle.DispenserID, affected_nozzle.FuelPointChannel, CmdEnum.SendPrices, 1223);
                      serialPort.Write(Buffer, 0, Buffer.Length);
                  }

                  if (affected_nozzle.isWorking)
                  {
                      Thread.Sleep(150);
                      Buffer = dispenser.CreateCmd(affected_nozzle.DispenserID, affected_nozzle.FuelPointChannel, CmdEnum.RequestDisplayData);
                      serialPort.Write(Buffer, 0, Buffer.Length);
                      Thread.Sleep(150);
                      
                  }


                  if (affected_nozzle.isClosed)
                  {
                      Thread.Sleep(150);
                      Buffer = dispenser.CreateCmd(affected_nozzle.DispenserID, affected_nozzle.FuelPointChannel, CmdEnum.OpenNozzle);
                      serialPort.Write(Buffer, 0, Buffer.Length);
                  }
                  if (affected_nozzle.isCheckLCD)
                  {
                      Console.WriteLine("******************");
                      Console.WriteLine("Euro: {0}", affected_nozzle.getDisplayPrice(PriceDecimalPlaces));
                      Console.WriteLine("Litra: {0}", affected_nozzle.getDisplayLitres(VolumeDecimalPlaces));
                      Console.WriteLine("******************");
                  }
                  Console.WriteLine();
              }

              
              Buffer = dispenser.CreateCmd(Address, FuelPoint_id, CmdEnum.SendPrices, 1547); //price as integer
             
              //how to evaluate a buffer
              //example.
              //Buffer = new byte[] { 0xFD, 0x02, 0x01, 0x01, 0x00, 0x00, 0x80, 0x00, 0x20, 0x04, 0x22, 0x21, 0x12, 0x00, 0x64, 0x00
              //,0x01, 0x02, 0x12, 0x00, 0x15, 0x01, 0x02, 0x14, 0x02, 0x00, 0x00, 0x05, 0x05, 0x04, 0x00, 0x11, 0x45, 0x00, 0x06, 0x05
              //,0x04, 0x00, 0x09, 0x37, 0x00, 0xFC, 0x30, 0x32, 0x35, 0x37, 0xFB,0xFD,0x02,0x01,0x01,0x00,0x00,0x20,0x00,0x08,0x01,0x22
              //,0x14,0x01,0x02,0x15,0x01,0x00,0xFC,0x30,0x30,0x37,0x43,0xFB,0xFE,0xFD,0x02,0x01,0x01,0x00,0x00,0x20,0x00,0x08,0x01,0x22
              //,0x14,0x01,0x03,0x15,0x01,0x00,0xFC,0x30,0x30,0x37,0x44,0xFB,0xFE,0xFD,0x02,0x01,0x01,0x00,0x00,0x20,0x00,0x08,0x01,0x22
              //,0x14,0x01,0x04,0x15,0x01,0x00,0xFC,0x30,0x30,0x37,0x45,0xFB,0xFE,0xFD,0x02,0x01,0x01,0x00,0x00,0x20,0x00,0x08,0x01,0x22
              //,0x14,0x01,0x06,0x15,0x01,0x00,0xFC,0x30,0x30,0x38,0x30,0xFB,0xFE,0xFD,0x02,0x01,0x01,0x00,0x00,0x20,0x00,0x14,0x01,0x22
              //,0x22,0x05,0x04,0x00,0x11,0x10,0x00,0x23,0x05,0x04,0x00,0x09,0x08,0x00,0x1D,0x02,0x11,0x97,0xFC,0x30,0x31,0x41,0x42,0xFB};
              ////ston parapananw buffer exei 5 entoles kai tyxaio thorybo


              //example 2. read changelog.txt
              //edw o buffer exei 2 responses apo to idio nozzle
              // a closed
              // b idle
              // mono to pio prosfato epistrefetai.
              //Buffer = new byte[] { 0xFD, 0x02, 0x01, 0x01, 0x00, 0x00, 0x20, 0x00, 0x08, 0x01, 0x22, 0x14, 0x01, 0x02, 
                 // 0x15, 0x01, 0x00, 0xFC, 0x30, 0x30, 0x37, 0x43, 0xFB, 0xFE, 0xFD, 0x02, 0x01, 0x01, 0x00, 0x00, 
                  //0x20, 0x00, 0x08, 0x01, 0x22, 0x14, 0x01, 0x03, 0x15, 0x01, 0x00, 0xFC, 0x30, 0x30, 0x37, 0x44, 0xFB, 0xFE };
            
              
       
 
                




               
                //int Address = 7;
                //int Channel = 2;
                
                //buffer = dispenser.createCmd(Address, Channel, CommandTypeEnum.SendPrices,1547);
                //global::System.Windows.Forms.MessageBox.Show(BitConverter.ToString(buffer.ToArray()));
                
                ////serialPort.Write(buffer, 0, buffer.Length);
                // System.Threading.Thread.Sleep(200);
                
                // buffer = dispenser.createCmd(Address, Channel, CommandTypeEnum.Authorize);
                ////serialPort.Write(buffer, 0, buffer.Length);

                //global::System.Windows.Forms.MessageBox.Show(BitConverter.ToString(buffer.ToArray()));

              }
              else
              {
                  throw new Exception("COM BUSY");
              }



        }



    }
}