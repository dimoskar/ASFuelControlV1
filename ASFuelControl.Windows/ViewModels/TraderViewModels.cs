using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASFuelControl.Windows.ViewModels
{
    public partial class TraderViewModel
    {
        public TraderViewModel()
        {
        }

        public TraderViewModel(Data.DatabaseModel db, Data.Trader entity) : base(db, entity)
        {

        }

        public bool CanDelete
        {
            get
            {
                Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
                try
                {
                    if (db.Invoices.Where(i => i.TraderId.HasValue && i.TraderId == this.traderid).Count() > 0)
                        return false;
                    return true;
                }
                catch
                {
                    return false;
                }
                finally
                {
                    db.Dispose();
                }
            }
        }
    }
}
