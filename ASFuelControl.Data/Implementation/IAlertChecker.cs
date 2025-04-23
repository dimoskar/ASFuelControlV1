using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASFuelControl.Data.Implementation
{
    public interface IAlertChecker
    {
        void CreateBalanceAlert(DatabaseModel database, string fuelType, decimal diff, string message, Common.Enumerators.AlarmEnum alertType, DateTime dt, decimal totalSales);
        void ResolveAlert(Guid evenId, string message);
    }
}
