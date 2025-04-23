using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ASFuelControl.Common;
using System.IO.Ports;
using System.Threading;
using ASFuelControl.Common.Enumerators;

namespace ASFuelControl.GVR
{
  
    public class GVRConnector : IFuelProtocol, IPumpDebug
    {
      private List<byte> buffer = new List<byte>();
      private List<FuelPoint> fuelPoints = new List<FuelPoint>();
      private SerialPort serialPort = new SerialPort();
      private byte[] cmd;
      private byte[] abuffer;
      private Thread th;

      private double speed { get; set; }

      public FuelPoint[] FuelPoints
      {
        get
        {
          return this.fuelPoints.ToArray();
        }
        set
        {
          this.fuelPoints = new List<FuelPoint>((IEnumerable<FuelPoint>) value);
        }
      }

      public bool IsConnected
      {
        get
        {
          return this.serialPort.IsOpen;
        }
      }

      public string CommunicationPort { get; set; }

      public event EventHandler<TotalsEventArgs> TotalsRecieved;
      public event EventHandler<Common.SaleEventArgs> SaleRecieved;
      public event EventHandler<FuelPointValuesArgs> DataChanged;

      public event EventHandler<FuelPointValuesArgs> DispenserStatusChanged;
      public event EventHandler DispenserOffline;

      public DebugValues DebugStatusDialog(FuelPoint fp)
      {
        throw new NotImplementedException();
      }

      public void Connect()
      {
        try
        {
          this.serialPort.PortName = this.CommunicationPort;
          this.serialPort.Open();
          this.speed = (double) (1 / (this.serialPort.BaudRate / 8000));
          this.th = new Thread(new ThreadStart(this.ThreadRun));
          this.th.Start();
        }
        catch
        {
        }
      }

      public void Disconnect()
      {
        if (!this.serialPort.IsOpen)
          return;
        this.serialPort.Close();
      }

      public void AddFuelPoint(FuelPoint fp)
      {
        this.fuelPoints.Add(fp);
      }

      public void ClearFuelPoints()
      {
        this.fuelPoints.Clear();
      }

      private void ThreadRun()
      {
        foreach (FuelPoint fuelPoint in this.fuelPoints)
        {
          foreach (Nozzle nozzle in fuelPoint.Nozzles)
            nozzle.QueryTotals = true;
          fuelPoint.SetExtendedProperty("isInitialized", (object) false);
        }
        while (this.IsConnected)
        {
          try
          {
            foreach (FuelPoint fp in this.fuelPoints)
            {
              try
              {
                  if (!(bool)fp.GetExtendedProperty("isInitialized", (object)false))
                      this.InitializeMe(fp);
                  else if (fp.QueryHalt)
                      this.Halt(fp);
                  else if ((bool)fp.GetExtendedProperty("iNeedResume", (object)false))
                  {
                      this.Resume(fp);
                      this.GetUnitPrice(fp);
                  }
                  else if (Enumerable.Count<Nozzle>(Enumerable.Where<Nozzle>((IEnumerable<Nozzle>)fp.Nozzles, (Func<Nozzle, bool>)(n => n.QueryTotals))) > 0)
                  {
                      foreach (Nozzle nz in fp.Nozzles)
                      {
                          if (nz.QueryTotals)
                              this.GetTotals(nz);
                      }
                  }
                  else
                  {
                      this.GetDisplay(fp);
                      if (fp.QueryAuthorize)
                          this.Authorize(fp.ActiveNozzle);
                      else if (fp.QuerySetPrice)
                      {
                          foreach (Nozzle nz in fp.Nozzles)
                              this.SetPrice(nz, nz.UntiPriceInt);
                          if (Enumerable.Count<Nozzle>(Enumerable.Where<Nozzle>((IEnumerable<Nozzle>)fp.Nozzles, (Func<Nozzle, bool>)(n => n.QuerySetPrice))) == 0)
                              fp.QuerySetPrice = false;
                      }
                  }
              }
              finally
              {
                Thread.Sleep(80);
              }
            }
          }
          catch
          {
            Thread.Sleep(100);
          }
        }
      }

      private void GetTotals(Nozzle nz)
      {
        this.cmd = new byte[6]
        {
          (byte) 170,
          (byte) 85,
          (byte) 3,
          (byte) nz.ParentFuelPoint.Address,
          (byte) 161,
          (byte) 1
        };
        this.cmd = CRC.crc(this.cmd);
        this.executeCommand(nz.ParentFuelPoint, GVRConnector.ResponseL.totals, CommandType.totals);
      }

      private void PresetVolume(Nozzle nz)
      {
        this.cmd = new byte[9]
        {
          (byte) 170,
          (byte) 85,
          (byte) 6,
          (byte) nz.ParentFuelPoint.Address,
          (byte) 177,
          (byte) 1,
          (byte) 0,
          (byte) 32,
          (byte) 0
        };
        this.cmd = CRC.crc(this.cmd);
        this.executeCommand(nz.ParentFuelPoint, GVRConnector.ResponseL.confirm, GVRConnector.CommandType.presetVolume);
      }

      private void PresetAmount(Nozzle nz)
      {
        this.cmd = new byte[9]
        {
          (byte) 170,
          (byte) 85,
          (byte) 6,
          (byte) nz.ParentFuelPoint.Address,
          (byte) 177,
          (byte) 2,
          (byte) 0,
          (byte) 32,
          (byte) 0
        };
        this.cmd = CRC.crc(this.cmd);
        this.executeCommand(nz.ParentFuelPoint, GVRConnector.ResponseL.confirm, GVRConnector.CommandType.presetAmount);
      }

      private void InitializeMe(FuelPoint fp)
      {
        this.cmd = new byte[6]
        {
          (byte) 170,
          (byte) 85,
          (byte) 3,
          (byte) fp.Address,
          (byte) 176,
          (byte) 1
        };
        this.cmd = CRC.crc(this.cmd);
        this.executeCommand(fp, GVRConnector.ResponseL.confirm, GVRConnector.CommandType.initialized);
      }

      private void GetUnitPrice(FuelPoint fp)
      {
        this.cmd = new byte[6]
        {
          (byte) 170,
          (byte) 85,
          (byte) 3,
          (byte) fp.Address,
          (byte) 192,
          (byte) 1
        };
        this.cmd = CRC.crc(this.cmd);
        this.executeCommand(fp, GVRConnector.ResponseL.UnitPrice, GVRConnector.CommandType.GetUnitPrice);
      }

      private void Halt(FuelPoint fp)
      {
        this.cmd = new byte[5]
        {
          (byte) 170,
          (byte) 85,
          (byte) 2,
          (byte) fp.Address,
          (byte) 181
        };
        this.cmd = CRC.crc(this.cmd);
        this.executeCommand(fp, GVRConnector.ResponseL.confirm, GVRConnector.CommandType.halt);
      }

      private void Resume(FuelPoint fp)
      {
        this.cmd = new byte[5]
        {
          (byte) 170,
          (byte) 85,
          (byte) 2,
          (byte) fp.Address,
          (byte) 179
        };
        this.cmd = CRC.crc(this.cmd);
        this.executeCommand(fp, GVRConnector.ResponseL.confirm, GVRConnector.CommandType.resume);
      }

      private void Authorize(Nozzle nozzle)
      {
        this.cmd = new byte[6]
        {
          (byte) 170,
          (byte) 85,
          (byte) 3,
          (byte) nozzle.ParentFuelPoint.Address,
          (byte) 178,
          (byte) 1
        };
        this.cmd = CRC.crc(this.cmd);
        this.executeCommand(nozzle.ParentFuelPoint, GVRConnector.ResponseL.confirm, GVRConnector.CommandType.authorize);
      }

      private void SetPrice(Nozzle nz, int unitPrice)
      {
        string str = unitPrice.ToString();
        for (int length = str.Length; length < 4; ++length)
          str = "0" + str;
        int num1 = Convert.ToInt32(str.Substring(0, 2));
        int num2 = Convert.ToInt32(str.Substring(2, 2));
        this.cmd = new byte[8]
        {
          (byte) 170,
          (byte) 85,
          (byte) 5,
          (byte) nz.ParentFuelPoint.Address,
          (byte) 193,
          (byte) 1,
          (byte) this.ith(num1),
          (byte) this.ith(num2)
        };
        this.cmd = CRC.crc(this.cmd);
        this.executeCommand(nz.ParentFuelPoint, GVRConnector.ResponseL.display, GVRConnector.CommandType.SetPrice);
      }

      private void GetDisplay(FuelPoint fp)
      {
        this.cmd = new byte[5]
        {
          (byte) 170,
          (byte) 85,
          (byte) 2,
          (byte) fp.Address,
          (byte) 160
        };
        this.cmd = CRC.crc(this.cmd);
        this.executeCommand(fp, GVRConnector.ResponseL.display, GVRConnector.CommandType.display);
      }

      private void executeCommand(FuelPoint fp, GVRConnector.ResponseL l, GVRConnector.CommandType t)
      {
        byte[] buffer1 = new byte[this.serialPort.BytesToRead];
        this.serialPort.Read(buffer1, 0, buffer1.Length);
        int num1 = 150;
        this.buffer = new List<byte>();
        this.serialPort.Write(this.cmd, 0, this.cmd.Length);
        Thread.Sleep((int) ((double) (this.cmd.Length + l) * this.speed + 20.0));
        int num2 = 0;
        Console.WriteLine(BitConverter.ToString(this.cmd));
        while ((GVRConnector.ResponseL) this.serialPort.BytesToRead < l && num2 < num1 / 20 * 10)
        {
          num2 += 10;
          Thread.Sleep(20);
        }
        byte[] buffer2 = new byte[this.serialPort.BytesToRead];
        this.serialPort.Read(buffer2, 0, buffer2.Length);
        this.buffer.AddRange((IEnumerable<byte>) buffer2);
        Console.WriteLine(BitConverter.ToString(this.buffer.ToArray()));
        if (this.buffer.ToArray().Length <= 0)
        {
            if (DateTime.Now.Subtract(fp.LastValidResponse).TotalSeconds > 5)
            {
                if (this.DispenserOffline != null)
                    this.DispenserOffline(fp, new EventArgs());
            }
            return;
        }
        fp.LastValidResponse = DateTime.Now;
        try
        {
          this.evaluateBuffer(this.buffer, fp, t);
        }
        catch (Exception ex)
        {
        }
      }

      private void evaluateBuffer(List<byte> response, FuelPoint fp, GVRConnector.CommandType t)
      {
        try
        {
          byte[] numArray = response.ToArray();
          switch ((GVRConnector.ResponseL) numArray.Length)
          {
            case GVRConnector.ResponseL.confirm:
              if ((int) numArray[3] != fp.Address)
                break;
              switch (t)
              {
                case GVRConnector.CommandType.initialized:
                  fp.SetExtendedProperty("isInitialized", (object) true);
                  return;
                case GVRConnector.CommandType.display:
                  return;
                case GVRConnector.CommandType.totals:
                  return;
                case GVRConnector.CommandType.authorize:
                  fp.QueryAuthorize = false;
                  return;
                case GVRConnector.CommandType.SetPrice:
                  fp.QuerySetPrice = false;
                  return;
                case GVRConnector.CommandType.halt:
                  fp.QueryHalt = false;
                  return;
                case GVRConnector.CommandType.resume:
                  fp.SetExtendedProperty("iNeedResume", (object) false);
                  return;
                default:
                  return;
              }
            case GVRConnector.ResponseL.UnitPrice:
              if ((int) numArray[3] != fp.Address)
                break;
              BitConverter.ToString(Extensions.take(Extensions.skip(numArray, 5), 3)).Replace("-", "");
              break;
            case GVRConnector.ResponseL.display:
              if ((int) numArray[3] != fp.Address)
                break;
              FuelPointStatusEnum status = fp.Status;
              fp.Status = this.evalStatus(numArray[6]);
              fp.DispenserStatus = fp.Status;
              if (fp.Status == FuelPointStatusEnum.TransactionCompleted)
                fp.SetExtendedProperty("iNeedResume", (object) true);
              else
                fp.SetExtendedProperty("iNeedResume", (object) false);
              if (fp.Status != status && this.DispenserStatusChanged != null)
              {
                FuelPointValues fuelPointValues = new FuelPointValues();
                if (fp.Status != FuelPointStatusEnum.Idle && fp.Status != FuelPointStatusEnum.Offline)
                {
                  fp.ActiveNozzleIndex = 0;
                  fuelPointValues.ActiveNozzle = 0;
                }
                else
                {
                  fp.ActiveNozzleIndex = -1;
                  fuelPointValues.ActiveNozzle = -1;
                }
                fuelPointValues.Status = fp.Status;
                this.DispenserStatusChanged((object) this, new FuelPointValuesArgs()
                {
                  CurrentFuelPoint = fp,
                  CurrentNozzleId = fuelPointValues.ActiveNozzle + 1,
                  Values = fuelPointValues
                });
              }
              if (fp.Status != FuelPointStatusEnum.Work && fp.Status != FuelPointStatusEnum.TransactionCompleted)
                break;
              fp.DispensedAmount = Extensions.takeToDecimal(Extensions.skip(numArray, 11), 3) / (Decimal) Math.Pow(10.0, (double) fp.AmountDecimalPlaces);
              fp.DispensedVolume = Extensions.takeToDecimal(Extensions.skip(numArray, 8), 3) / (Decimal) Math.Pow(10.0, (double) fp.VolumeDecimalPlaces);
              if (this.DataChanged != null)
                this.DataChanged((object) this, new FuelPointValuesArgs()
                {
                  CurrentFuelPoint = fp,
                  CurrentNozzleId = 1,
                  Values = new FuelPointValues()
                  {
                    CurrentSalePrice = fp.Nozzles[0].UnitPrice,
                    CurrentPriceTotal = fp.DispensedAmount,
                    CurrentVolume = fp.DispensedVolume
                  }
                });
              break;
            case GVRConnector.ResponseL.totals:
              if ((int) numArray[3] != fp.Address)
                break;
              fp.Nozzles[0].TotalVolume = Extensions.takeToDecimal(Extensions.skip(numArray, 6), 5);
              fp.Nozzles[0].TotalPrice = Extensions.takeToDecimal(Extensions.skip(numArray, 11), 5);
              fp.Initialized = true;
              if (this.TotalsRecieved == null)
                break;
              this.TotalsRecieved((object) this, new TotalsEventArgs(fp, fp.Nozzles[0].Index, fp.Nozzles[0].TotalVolume, fp.Nozzles[0].TotalPrice));
              //fp.Nozzles[0].QueryTotals = false;
              break;
          }
        }
        catch
        {
        }
      }

      private FuelPointStatusEnum evalStatus(byte p)
      {
        try
        {
          switch (p)
          {
            case (byte) 0:
              return FuelPointStatusEnum.Idle;
            case (byte) 1:
              return FuelPointStatusEnum.Work;
            case (byte) 2:
              return FuelPointStatusEnum.Nozzle;
            case (byte) 4:
              return FuelPointStatusEnum.TransactionCompleted;
            default:
              return FuelPointStatusEnum.Offline;
          }
        }
        catch (Exception ex)
        {
          return FuelPointStatusEnum.Offline;
        }
      }

      private int ith(int num)
      {
        return 16 * (num / 10) + num % 10;
      }

      private enum CommandType
      {
        initialized,
        display,
        totals,
        authorize,
        SetPrice,
        halt,
        resume,
        presetVolume,
        presetAmount,
        GetUnitPrice,
      }

      private enum ResponseL
      {
        confirm = 7,
        UnitPrice = 9,
        display = 16,
        totals = 23,
      }
    }
  
}
