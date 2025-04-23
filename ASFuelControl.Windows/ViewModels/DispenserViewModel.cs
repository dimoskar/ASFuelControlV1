using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASFuelControl.Data;

namespace ASFuelControl.Windows.ViewModels
{
    public partial class DispenserViewModel
    {
        private List<NozzleViewModel> nozzles = new List<NozzleViewModel>();

        public string Description { set; get; }

        public NozzleViewModel[] Nozzles
        {
            get { return this.nozzles.ToArray(); }
        }

        public DispenserViewModel()
        {
        }

        public override void Load(DatabaseModel db, Dispenser entity)
        {
            base.Load(db, entity);
            this.Description = string.Format("Αντλία :{0} ({1})", entity.DispenserNumber, entity.OfficialPumpNumber);
        }

        protected override void LoadArrayProperty(DatabaseModel db, Dispenser entity, string propName)
        {
            if (propName == "Nozzles")
            {
                this.nozzles.Clear();
                foreach (var line in entity.Nozzles)
                {
                    NozzleViewModel nzvm = new NozzleViewModel();
                    nzvm.Load(db, line.NozzleId);
                    nzvm.PropertyChanged += Nzvm_PropertyChanged;
                    this.nozzles.Add(nzvm);
                }
            }
        }

        protected override void SaveArrayProperty(DatabaseModel db, Dispenser entity, string propName)
        {
            if (propName == "Nozzles")
            {
                foreach (var nz in this.nozzles)
                {
                    if (this.EntityState == EntityStateEnum.Deleted)
                        nz.EntityState = EntityStateEnum.Deleted;
                    nz.Save(db, nz.NozzleId);
                }
            }
        }

        private void Nzvm_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            
        }
    }

    public partial class NozzleViewModel
    {
        public string Description { set; get; }

        public decimal UnitPrice { set; get; }

        public override void Load(DatabaseModel db, Nozzle entity)
        {
            base.Load(db, entity);
            if(entity.FuelType != null)
                this.UnitPrice = entity.FuelType.CurrentPrice;
            this.Description = entity.Description;
        }

    }

    public partial class DispenserSettingsViewModel : DispenserViewModel
    {
        private string serialNumber = "";
        private string deviceSeal = "";
        private int totalDecimalPlaces = 2;
        private int saleTimeOut = 2;

        public string DeviceSeal
        {
            set
            {
                if (this.deviceSeal == value)
                    return;
                this.deviceSeal = value;
                this.OnPropertyChanged("DeviceSeal");
            }
            get { return this.deviceSeal; }
        }

        public string SerialNumber
        {
            set
            {
                if (this.serialNumber == value)
                    return;
                this.serialNumber = value;
                this.OnPropertyChanged("SerialNumber");
            }
            get { return this.serialNumber; }
        }

        public int TotalDecimalPlaces
        {
            set
            {
                if (this.totalDecimalPlaces == value)
                    return;
                this.totalDecimalPlaces = value;
                this.OnPropertyChanged("TotalDecimalPlaces");
            }
            get { return this.totalDecimalPlaces; }
        }

        public int SaleTimeOut
        {
            set
            {
                if (this.saleTimeOut == value)
                    return;
                this.saleTimeOut = value;
                this.OnPropertyChanged("SaleTimeOut");
            }
            get { return this.saleTimeOut; }
        }

        public DispenserSettingsViewModel()
        {
            
        }

        public override void Load(DatabaseModel db, Dispenser entity)
        {
            base.Load(db, entity);
            this.Description = string.Format("Αντλία :{0} ({1})", entity.DispenserNumber, entity.OfficialPumpNumber);
            this.SerialNumber = entity.DeviceSerialNumber;
            this.TotalDecimalPlaces = entity.TotalVolumeDecimalPlaces;
            this.SaleTimeOut = entity.SaleTimeOut;
            
        }

        private void Nzvm_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {

        }
    }
}
