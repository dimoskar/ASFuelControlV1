using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ASFuelControl.Common.Enumerators;

namespace ASFuelControl.Common
{
    public class OpenCommand
    {
        public string CommandString
        {
            get { return BitConverter.ToString(this.Command); }
        }
        public CommandTypeEnum CommandType { private set; get; }
        public FuelPoint Fpoint { private set; get; }
        public Nozzle CurrentNozzle { private set; get; }
        public byte[] Command { private set; get; }
        public DateTime SendTime { set; get; }
        public DateTime TimeExeeded { set; get; }
        public int ResponseLength { set; get; }
        public int BaudRate { set; get; }
        public int MinimumTimeNeeded
        {
            get
            {
                double speed = 1 / (this.BaudRate / 8000);
                return (int)((double)(this.Command.Length + this.ResponseLength) * speed);
            }
        }

        public OpenCommand(byte[] cmd, CommandTypeEnum typ, FuelPoint fp)
        {
            this.CommandType = typ;
            this.Fpoint = fp;
            this.Command = cmd;
            this.SetResponseLength();
            this.BaudRate = 9600;
            this.SendTime = DateTime.MinValue;
        }

        public OpenCommand(byte[] cmd, CommandTypeEnum typ, Nozzle nozzle)
        {
            this.CommandType = typ;
            this.Fpoint = nozzle.ParentFuelPoint;
            this.CurrentNozzle = nozzle;
            this.Command = cmd;
            this.SetResponseLength();
            this.BaudRate = 9600;
            this.SendTime = DateTime.MinValue;
        }

        private void SetResponseLength()
        {
            switch (this.CommandType)
            {
                case CommandTypeEnum.RequestStatus:
                    this.ResponseLength = 2;
                    break;
                case CommandTypeEnum.RequestTotals:
                    this.ResponseLength = 22;
                    break;
                case CommandTypeEnum.SendPrices:
                    this.ResponseLength = 2;
                    break;
                case CommandTypeEnum.RequestDisplayData:
                    this.ResponseLength = 18;
                    break;
                case CommandTypeEnum.Authorize:
                    this.ResponseLength = 2;
                    break;
                case CommandTypeEnum.Halt:
                    this.ResponseLength = 2;
                    break;
                case CommandTypeEnum.RequestActiveNozzle:
                    this.ResponseLength = 2;
                    break;
                case CommandTypeEnum.SendMainDisplayData:
                    this.ResponseLength = 2;
                    break;

            }
        }
    }
}
