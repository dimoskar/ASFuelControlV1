using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASFuelControl.Windows.ViewModels
{
    public class TraderCatalogViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private List<TraderViewModel> traders = new List<TraderViewModel>();
        private string filter = "";
        private bool selectCustomer = true;

        public string Filter
        {
            set
            {
                if (this.filter == value)
                    return;
                this.filter = value;
                this.OnPropertyChanged("Filter");
            }
            get { return this.filter; }
        }

        public bool SelectCustomer
        {
            set
            {
                if (this.selectCustomer == value)
                    return;
                this.selectCustomer = value;
                this.OnPropertyChanged("SelectCustomer");
            }
            get { return this.selectCustomer; }
        }

        public bool SelectSuplier
        {
            set
            {
                if (!this.selectCustomer == value)
                    return;
                this.selectCustomer = !value;
                this.OnPropertyChanged("SelectCustomer");
            }
            get { return !this.selectCustomer; }
        }

        public TraderViewModel[] Traders
        {
            get { return this.traders.ToArray(); }
        }

        public TraderCatalogViewModel()
        {
        }

        public void LoadData()
        {
            Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);

            var q = db.Traders.Where(t=>
                (this.selectCustomer && t.IsCustomer) ||
                (!this.selectCustomer && t.IsSupplier));
            if (filter != null && filter != "")
            {
                q = q.Where(t =>
                    t.Name.Contains(filter) ||
                    (t.Address != null && t.Address.Contains(filter)) ||
                    (t.City != null && t.City.Contains(filter)) ||
                    (t.TaxRegistrationNumber != null && t.TaxRegistrationNumber.Contains(filter)) ||
                    (t.TaxRegistrationOffice != null && t.TaxRegistrationOffice.Contains(filter)) ||
                    (t.Phone1 != null && t.Phone1.Contains(filter)) ||
                    (t.Phone2 != null && t.Phone2.Contains(filter)) ||
                    (t.Email != null && t.Email.Contains(filter)) ||
                    (t.Occupation != null && t.Occupation.Contains(filter)));
            }
            var qa = q.OrderBy(t=>t.Name).Select(t=> new TraderViewModel(db, t)).ToArray();
            this.traders.Clear();
            this.traders.AddRange(qa);
            db.Dispose();
            this.OnPropertyChanged("Traders");
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
