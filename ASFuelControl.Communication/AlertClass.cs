using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.Communication
{
    [Serializable]
    public class AlertClass
    {
        public Enums.AlertIdEnum Alert { set; get; }
        public DateTime AlertTime { set; get; }
        public string DeviceId { set; get; }
        public string Description { set; get; }
        public string AlertCode { set; get; }

        public void SetAlertType(int alertType)
        {
            switch(alertType)
            {
                case 1:
                    this.Alert = Enums.AlertIdEnum.TankAlert;
                    this.AlertCode = "D09";
                    break;
                case 2:
                    this.Alert = Enums.AlertIdEnum.TankAlert;
                    this.AlertCode = "D12";
                    break;
                case 3:
                    this.Alert = Enums.AlertIdEnum.TankAlert;
                    this.AlertCode = "D04";
                    break;
                case 4:
                    this.Alert = Enums.AlertIdEnum.FuelLoss;
                    this.AlertCode = "D16";
                    break;
                case 5:
                    this.Alert = Enums.AlertIdEnum.TankAlert;
                    this.AlertCode = "D05";
                    break;
                case 6:
                    this.Alert = Enums.AlertIdEnum.WrongDataFuelPoint;
                    this.AlertCode = "D02";
                    break;
                case 7:
                    this.Alert = Enums.AlertIdEnum.CommunicationLossFuelPoint;
                    this.AlertCode = "H01";
                    break;
                case 8:
                    this.Alert = Enums.AlertIdEnum.CommunicationLossTank;
                    this.AlertCode = "H02";
                    break;
                case 9:
                    this.Alert = Enums.AlertIdEnum.BalanceDifference;
                    this.AlertCode = "D07";
                    break;
                case 10:
                    this.Alert = Enums.AlertIdEnum.TankAlert;
                    break;
                case 11:
                    this.Alert = Enums.AlertIdEnum.CommunicationLossFuelPoint;
                    this.AlertCode = "H01";
                    break;
                case 12:
                    this.Alert = Enums.AlertIdEnum.CommunicationLossTank;
                    this.AlertCode = "H02";
                    break;
                case 13:
                    this.Alert = Enums.AlertIdEnum.TitrimetryDataChange;
                    this.AlertCode = "D06";
                    break;
                case 14:
                    this.Alert = Enums.AlertIdEnum.ProgramTermination;
                    this.AlertCode = "S01";
                    break;
                case 15:
                    this.Alert = Enums.AlertIdEnum.FuelLoss;
                    this.AlertCode = "D01";
                    break;
                case 16:
                    this.Alert = Enums.AlertIdEnum.WrongDataTank;
                    this.AlertCode = "D11";
                    break;
                case 100:
                    this.Alert = Enums.AlertIdEnum.TankDensityError;
                    break;
            }
        }
    }
}
