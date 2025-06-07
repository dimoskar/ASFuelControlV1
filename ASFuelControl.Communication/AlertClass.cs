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
                    break;
                case 2:
                    this.Alert = Enums.AlertIdEnum.TankAlert;
                    break;
                case 3:
                    this.Alert = Enums.AlertIdEnum.TankAlert;
                    break;
                case 4:
                    this.Alert = Enums.AlertIdEnum.FuelLoss;
                    break;
                case 5:
                    this.Alert = Enums.AlertIdEnum.TankAlert;
                    break;
                case 6:
                    this.Alert = Enums.AlertIdEnum.WrongDataFuelPoint;
                    break;
                case 7:
                    this.Alert = Enums.AlertIdEnum.CommunicationLossFuelPoint;
                    break;
                case 8:
                    this.Alert = Enums.AlertIdEnum.CommunicationLossTank;
                    break;
                case 9:
                    this.Alert = Enums.AlertIdEnum.BalanceDifference;
                    break;
                case 10:
                    this.Alert = Enums.AlertIdEnum.TankAlert;
                    break;
                case 11:
                    this.Alert = Enums.AlertIdEnum.CommunicationLossFuelPoint;
                    break;
                case 12:
                    this.Alert = Enums.AlertIdEnum.CommunicationLossTank;
                    break;
                case 13:
                    this.Alert = Enums.AlertIdEnum.TitrimetryDataChange;
                    break;
                case 14:
                    this.Alert = Enums.AlertIdEnum.ProgramTermination;
                    break;
                case 100:
                    this.Alert = Enums.AlertIdEnum.TankDensityError;
                    break;
            }
        }
    }
}
