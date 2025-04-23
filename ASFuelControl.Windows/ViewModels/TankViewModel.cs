using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASFuelControl.Data;

namespace ASFuelControl.Windows.ViewModels
{
    public partial class TankViewModel
    {
        public string FuelTypeDesc { set; get; }

        public string Description
        {
            get { return "Δεξαμενή " + this.TankNumber.ToString() + FuelTypeDesc; }
        }

        public override void Load(DatabaseModel db, Data.Tank entity)
        {
            base.Load(db, entity);
            this.FuelTypeDesc = entity.FuelType.Name;
        }

        public override void Load(DatabaseModel db, Guid id)
        {
            base.Load(db, id);
            var ft = db.FuelTypes.SingleOrDefault(f => f.FuelTypeId == this.FuelTypeId);
            if(ft != null)
                this.FuelTypeDesc = ft.Name;
        }
    }
}
