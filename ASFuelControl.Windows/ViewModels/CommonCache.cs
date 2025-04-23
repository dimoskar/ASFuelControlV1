using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASFuelControl.Windows.ViewModels
{
    public class CommonCache
    {
        private static CommonCache instance;

        private InvoiceTypeDetailViewModel[] invoiceTypes = new InvoiceTypeDetailViewModel[] { };

        public static CommonCache Instance
        {
            get
            {
                if (instance == null)
                    instance = new CommonCache();
                return instance;
            }
        }

        public FuelTypeViewModel[] FuelTypes { set; get; }
        public InvoiceTypeDetailViewModel[] InvoiceTypes
        {
            set { this.invoiceTypes = value; }
            get
            {
                //Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
                //bool isAdmin = false;
                //var user = db.ApplicationUsers.SingleOrDefault(a => a.ApplicationUserId == Data.DatabaseModel.UserLoggedIn);
                //if (user != null && user.UserLevel == 0)
                //    isAdmin = true;
                //db.Dispose();
                //if (!isAdmin)
                //    return this.invoiceTypes.Where(i => !i.AdminView.HasValue || !i.AdminView.Value).ToArray();
                return this.invoiceTypes;
            }
        }
        public TankViewModel[] Tanks { set; get; }
        public DispenserViewModel[] Dispensers { set; get; }

        public CommonCache()
        {
            Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);

            this.LoadFuelTypes(db);
            this.LoadInvoiceTypes(db);
            this.LoadTanks(db);
            this.LoadDispensers(db);
            db.Dispose();
        }

        public void Refresh(string typeName)
        {
            Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
            switch (typeName)
            {
                case "FuelTypes":
                    this.LoadFuelTypes(db);
                    break;
                case "InvoiceTypes":
                    this.LoadInvoiceTypes(db);
                    break;
                case "Tanks":
                    this.LoadTanks(db);
                    break;
                case "Dispensers":
                    this.LoadDispensers(db);
                    break;
            }
            db.Dispose();
        }

        private void LoadFuelTypes(Data.DatabaseModel db)
        {
            var q = db.FuelTypes.OrderBy(f => f.Name).ToArray();
            List<FuelTypeViewModel> list = new List<FuelTypeViewModel>();
            foreach(var item in q)
            {
                FuelTypeViewModel vm = new FuelTypeViewModel();
                vm.Load(db, item.FuelTypeId);
                list.Add(vm);
            }
            this.FuelTypes = list.ToArray();
        }

        private void LoadInvoiceTypes(Data.DatabaseModel db)
        {
            var q = db.InvoiceTypes.OrderBy(f => f.Description).ToArray();
            List<InvoiceTypeDetailViewModel> list = new List<InvoiceTypeDetailViewModel>();
            foreach (var item in q)
            {
                InvoiceTypeDetailViewModel vm = new InvoiceTypeDetailViewModel();
                vm.Load(db, item.InvoiceTypeId);
                list.Add(vm);
            }
            this.InvoiceTypes = list.OrderBy(d => d.Description).ToArray();
        }

        private void LoadTanks(Data.DatabaseModel db)
        {
            var q = db.Tanks.OrderBy(f => f.TankNumber).ToArray();
            List<TankViewModel> list = new List<TankViewModel>();
            foreach (var item in q)
            {
                TankViewModel vm = new TankViewModel();
                vm.Load(db, item.TankId);
                list.Add(vm);
            }
            this.Tanks = list.OrderBy(d => d.TankNumber).ToArray();
        }

        private void LoadDispensers(Data.DatabaseModel db)
        {
            var q = db.Dispensers.OrderBy(f => f.DispenserNumber).ToArray();
            List<DispenserViewModel> list = new List<DispenserViewModel>();
            foreach (var item in q)
            {
                DispenserViewModel vm = new DispenserViewModel();
                vm.Load(db, item.DispenserId);
                list.Add(vm);
            }
            this.Dispensers = list.OrderBy(d=>d.DispenserNumber).ToArray();
        }
    }
}
