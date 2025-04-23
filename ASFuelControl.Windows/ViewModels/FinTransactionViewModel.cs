using ASFuelControl.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASFuelControl.Windows.ViewModels
{
    public partial class FinTransactionViewModel
    {
        public FinTransactionViewModel() : base()
        {

        }

        public FinTransactionViewModel(DatabaseModel db, FinTransaction entity) : base(db, entity)
        {

        }
    }
}
