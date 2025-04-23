using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASFuelControl.Data;

namespace ASFuelControl.Windows.ViewModels
{
    public partial class TraderDetailsViewModel : TraderViewModel
    {
        private ObservableCollection<TraderVehicleViewModel> vehicles = new ObservableCollection<TraderVehicleViewModel>();
        private List<TraderInvoiceViewModel> invoices = new List<TraderInvoiceViewModel>();
        private List<TraderFinTransactionViewModel> finTransactions = new List<TraderFinTransactionViewModel>();
        private DateTime invoicesFrom = DateTime.Today.AddMonths(-3);
        private DateTime invoicesTo = DateTime.Today;
        private DateTime finTransFrom = DateTime.Today.AddMonths(-3);
        private DateTime finTransTo = DateTime.Today;

        public InvoiceTypeViewModel[] InvoiceTypes { set; get; }

        public Common.Enumerators.EnumItem[] PaymentTypes { set; get; }

        public bool UnLockedUI
        {
            get
            {
                if (Program.AdminConnected)
                    return true;
                if (this.InvoiceCount > 0)
                    return false;
                return true;
            }
        }

        public TraderVehicleViewModel[] Vehicles
        {
            get
            {
                return this.vehicles.Where(v=>v.EntityState != EntityStateEnum.Deleted).ToArray();
            }
        }

        public VehicleViewModel[] VehiclesDropDown
        {
            get
            {
                List<VehicleViewModel> veh = new List<VehicleViewModel>();
                veh.Add(new VehicleViewModel() { PlateNumber = "(Χωρίς Επιλογή)", VehicleId = Guid.Empty });
                veh.AddRange(this.vehicles.Where(v => v.EntityState != EntityStateEnum.Deleted).ToArray());
                return veh.ToArray();
            }
        }

        public TraderInvoiceViewModel[] Invoices
        {
            get
            {
                return this.invoices.Where(v => v.EntityState != EntityStateEnum.Deleted).ToArray();
            }
        }

        public TraderFinTransactionViewModel[] FinTransactions
        {
            get
            {
                return this.finTransactions.Where(v => v.EntityState != EntityStateEnum.Deleted).ToArray();
            }
        }

        public Guid SelectdInvoicesVehicleId { set; get; }

        public DateTime InvoicesFrom
        {
            set
            {
                this.invoicesFrom = value;
                this.OnPropertyChanged("InvoicesFrom", true);
            }
            get { return this.invoicesFrom; }
        }

        public DateTime InvoicesTo
        {
            set
            {
                this.invoicesTo = value;
                this.OnPropertyChanged("InvoicesTo", true);
            }
            get { return this.invoicesTo; }
        }

        public DateTime FinTransFrom
        {
            set
            {
                this.finTransFrom = value;
                this.OnPropertyChanged("FinTransFrom", true);
            }
            get { return this.finTransFrom; }
        }

        public DateTime FinTransTo
        {
            set
            {
                this.finTransTo = value;
                this.OnPropertyChanged("FinTransTo", true);
            }
            get { return this.finTransTo; }
        }

        public decimal PreviousCreditSum { set; get; }
        public decimal PreviousDebitSum { set; get; }

        public int InvoiceCount { set; get; }

        public Common.Enumerators.EnumItem[] TransactionTypes { set; get; }

        public string FilterInvoiceNumber { set; get; }

        public TraderDetailsViewModel()
        {
            List<Common.Enumerators.EnumItem> list = Common.Enumerators.EnumToList.EnumList<Common.Enumerators.FinTransactionTypeEnum>();
            this.TransactionTypes = list.ToArray();

            this.InvoiceTypes = CommonCache.Instance.InvoiceTypes.Where(i => i.DispenserTypeEx).ToArray();

            this.vehicles.CollectionChanged += Vehicles_CollectionChanged;

            List<Common.Enumerators.EnumItem> plist = Common.Enumerators.EnumToList.EnumList<Common.Enumerators.PaymentTypeEnum>();
            this.PaymentTypes = plist.ToArray();
        }

        public void CreateNew()
        {
            this.TraderId = Guid.NewGuid();
            this.EntityState = EntityStateEnum.Added;
        }

        public void AddVehicle()
        {
            TraderVehicleViewModel newVehicle = new TraderVehicleViewModel();
            newVehicle.TraderId = this.TraderId;
            newVehicle.EntityState = EntityStateEnum.Added;
            this.vehicles.Add(newVehicle);
            this.OnPropertyChanged("Vehicles");
            this.OnPropertyChanged("VehiclesDropDown");
        }

        public void AddFinTransaction()
        {
            TraderFinTransactionViewModel newTrans = new TraderFinTransactionViewModel();
            newTrans.TraderId = this.TraderId;
            newTrans.FinTransactionId = Guid.NewGuid();
            newTrans.TransactionDate = DateTime.Now;
            newTrans.ApplicationUserId = Data.DatabaseModel.CurrentUserId;
            newTrans.EntityState = EntityStateEnum.Added;
            newTrans.TransactionType = (int)Common.Enumerators.FinTransactionTypeEnum.Debit;
            this.finTransactions.Add(newTrans);
            this.OnPropertyChanged("FinTransactions");
            newTrans.PropertyChanged += Fvm_PropertyChanged;
        }

        public void DeleteVehicle(Guid vehicleId)
        {
            var vh = this.vehicles.SingleOrDefault(v => v.VehicleId == vehicleId);
            if (vh == null)
                return;
            vh.EntityState = EntityStateEnum.Deleted;
            this.OnPropertyChanged("Vehicles");
            this.OnPropertyChanged("VehiclesDropDown");
        }

        public void LoadInvoices()
        {
            Data.DatabaseModel db = new DatabaseModel(Properties.Settings.Default.DBConnection);
            try
            {
                this.LoadInvoices(db);
            }
            catch
            {

            }
            finally
            {
                db.Dispose();
            }
        }

        public void LoadInvoices(Data.DatabaseModel db)
        {
            try
            {
                this.invoices.Clear();
                var invoices = db.Invoices.Where(i => i.TraderId == this.TraderId &&
                    i.TransactionDate.Date >= this.invoicesFrom.Date &&
                    i.TransactionDate.Date <= this.invoicesTo.Date);
                if (this.SelectdInvoicesVehicleId != Guid.Empty)
                    invoices = invoices.Where(i => i.VehicleId == this.SelectdInvoicesVehicleId);
                if (this.FilterInvoiceNumber != null && this.FilterInvoiceNumber != "")
                    invoices = invoices.Where(i => i.Number.ToString().Contains(this.FilterInvoiceNumber));
                var invArray = invoices.OrderBy(i=>i.TransactionDate).Select(i=> new TraderInvoiceViewModel(db, i)).ToArray();
                this.invoices.AddRange(invArray);
            }
            catch
            {

            }
            finally
            {
                
            }
        }

        public void LoadFinTransactions()
        {
            Data.DatabaseModel db = new DatabaseModel(Properties.Settings.Default.DBConnection);
            try
            {
                this.LoadFinTransactions(db);
            }
            catch
            {

            }
            finally
            {
                db.Dispose();
            }
        }

        public void LoadFinTransactions(Data.DatabaseModel db)
        {
            try
            {
                this.finTransactions.Clear();
                var transactions = db.FinTransactions.Where(i => i.TraderId == this.TraderId &&
                    i.TransactionDate.Date >= this.finTransFrom.Date &&
                    i.TransactionDate.Date <= this.finTransTo.Date);
                var finArray = transactions.OrderBy(i => i.TransactionDate).Select(i => new TraderFinTransactionViewModel(db, i)).ToArray();
                this.finTransactions.AddRange(finArray);
                foreach(var fvm in finArray)
                {
                    fvm.PropertyChanged += Fvm_PropertyChanged;
                    fvm.HasChanges = false;
                }

                this.PreviousCreditSum = db.FinTransactions.Where(i => i.TraderId == this.TraderId && i.TransactionDate.Date < this.finTransFrom.Date).Sum(f => f.CreditAmount);
                this.PreviousDebitSum = db.FinTransactions.Where(i => i.TraderId == this.TraderId && i.TransactionDate.Date < this.finTransFrom.Date).Sum(f => f.DebitAmount);
            }
            catch(Exception ex)
            {

            }
            finally
            {

            }
        }

        public override bool Save(DatabaseModel db, Guid id)
        {
            this.Errors = new ValidationError[] { };
            if (this.TaxRegistrationNumber != null && this.TaxRegistrationNumber != "")
            {
                if (this.TaxRegistrationNumber.Length < 9 || this.TaxRegistrationNumber.Length > 17)
                {
                    if (this.TaxRegistrationNumber.Where(a => char.IsLetter(a)).Count() != 0)
                    {
                        string message = string.Format("To ΑΦΜ {0} πρέπει\r\nνα έχει μήκος από 9 εώς 17 χαρακτήρες.", this.TaxRegistrationNumber);
                        this.AddError("Σφάλμα καταχώρησης ΑΦΜ", message);
                    }   
                }
                else if(char.IsLetter(this.TaxRegistrationNumber.First()))
                {
                    if (this.TaxRegistrationNumber.Skip(2).Where(a => char.IsLetter(a)).Count() != 0)
                    {
                        string message = string.Format("Λάθος μορφή στο ΑΦΜ του πελάτη.", this.TaxRegistrationNumber);
                        this.AddError("Σφάλμα καταχώρησης ΑΦΜ", message);
                    }
                }
                else if(this.TaxRegistrationNumber.Where(a => char.IsLetter(a)).Count() != 0)
                {
                    if(!this.ValidateAFM(this.TaxRegistrationNumber))
                    {
                        string message = string.Format("Λάθος μορφή στο ΑΦΜ του πελάτη.", this.TaxRegistrationNumber);
                        this.AddError("Σφάλμα καταχώρησης ΑΦΜ", message);
                    }
                }
            }
            foreach(var vehicle in this.Vehicles)
            {
                if (vehicle.PlateNumber != null && vehicle.PlateNumber.Length > 0)
                {
                    if (vehicle.PlateNumber.Length < 6 || vehicle.PlateNumber.Length > 15)
                    {
                        if (vehicle.PlateNumber.Where(a => char.IsDigit(a)).Count() == 0)
                            continue;
                        string message = string.Format("Ο αριθ. κυκλοφορίας του Οχήματος {0} πρέπει\r\nνα έχει μήκος από 6 εώς 15 χαρακτήρες.", vehicle.PlateNumber);
                        this.AddError("Σφάλμα καταχώρησης Οχήματος", message);
                    }
                }
            }
            this.InvoiceCount = db.Invoices.Count(i => i.TraderId == id);
            this.OnPropertyChanged("LockedUI");
            if (this.Errors.Length > 0)
                return false;
            bool saved = base.Save(db, id);
            if (saved)
                this.LoadFinTransactions();
            return saved;
        }

        public override void Load(DatabaseModel db, Guid id)
        {
            base.Load(db, id);
            this.InvoiceCount = db.Invoices.Count(i => i.TraderId == id);
            LoadInvoices(db);
            LoadFinTransactions(db);
        }

        protected override void LoadArrayProperty(Data.DatabaseModel db, Trader entity, string propName)
        {
            if (propName == "Vehicles")
            {
                this.vehicles.Clear();
                var q = entity.Vehicles.ToArray();
                List<TraderVehicleViewModel> list = new List<TraderVehicleViewModel>();
                foreach (var veh in q)
                {
                    TraderVehicleViewModel vvm = new TraderVehicleViewModel();
                    vvm.Load(db, veh.VehicleId);
                    vvm.PropertyChanged += Vvm_PropertyChanged;
                    list.Add(vvm);
                    vvm.HasChanges = false;
                }
                this.vehicles = new ObservableCollection<TraderVehicleViewModel>(list);
                this.vehicles.CollectionChanged += Vehicles_CollectionChanged;
            }
        }

        protected override void SaveArrayProperty(DatabaseModel db, Trader entity, string propName)
        {
            if (propName == "Vehicles")
            {
                foreach (var veh in this.vehicles)
                {
                    if (veh.HasChanges)
                        veh.Save(db, veh.VehicleId);
                }
            }
            else if (propName == "FinTransactions")
            {
                foreach (var veh in this.FinTransactions)
                {
                    if(veh.HasChanges)
                        veh.Save(db, veh.FinTransactionId);
                }
            }
        }

        private void Vvm_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            this.HasChanges = true;
            this.OnPropertyChanged("Vehicles");
        }

        private void Fvm_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "HasChanges")
                return;
            this.HasChanges = true;
            this.OnPropertyChanged("FinTransactions");
        }

        private void Vehicles_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add || e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove ||
                e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Replace)
            {
                this.HasChanges = true;
            }
        }

        private bool ValidateAFM(string afm)
        {
            if (afm == null || afm == "")
                return false;
            if (afm == "000000000")
                return true;
            if (!char.IsNumber(afm.First()) && !char.IsNumber(afm.Skip(1).First()))
                return true;
            int _numAFM = 0;
            if (afm.Length != 9 || !int.TryParse(afm, out _numAFM))
                return false;
            else
            {
                double sum = 0;
                int iter = afm.Length - 1;
                afm.ToCharArray().Take(iter).ToList().ForEach(c =>
                {
                    sum += double.Parse(c.ToString()) * Math.Pow(2, iter);
                    iter--;
                });
                if (sum % 11 == double.Parse(afm.Substring(8)) || double.Parse(afm.Substring(8)) == 0)
                    return true;
                else
                    return false;
            }
        }
    }    

    public partial class TraderVehicleViewModel : VehicleViewModel
    {
        public int InvoiceCount { private set; get; }

        public bool CanDelete
        {
            get { return this.InvoiceCount == 0; }
        }

        public string ButtonText
        {
            get
            {
                if (this.CardId == null || this.CardId == "")
                    return "Διάθεση Κάρτας";
                return "Διαγραφή Κάρτας";
            }
        }

        public override void Load(DatabaseModel db, Guid id)
        {
            base.Load(db, id);
            var vehicle = db.Vehicles.SingleOrDefault(v => v.VehicleId == id);
            if (vehicle == null)
                return;
            this.InvoiceCount = db.Invoices.Where(i => i.VehicleId.HasValue && i.VehicleId.Value == id).Count();
        }

        protected override void OnPropertyChanged(string propertyName)
        {
            if(propertyName == "CardId")
            {
                if (!(this.CardId == null || this.CardId == ""))
                {
                    using (Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
                    {
                        var c = db.Vehicles.Where(v => v.VehicleId != this.VehicleId && v.CardId == this.CardId).Count();
                        if (c > 0)
                        {
                            this.AddError("Σφάλμα Ανάθεσης Κάρτας", "Η κάρτα αυτή είναι ήδη ανατεθημένη σε όχημα!", true);
                            this.CardId = "";
                        }
                    }
                }
            }
            this.OnPropertyChanged(propertyName, false);
        }
    }

    public class TraderInvoiceViewModel : InvoiceViewModel
    {
        private string invoiceTypeName = "";
        private string plateNumber = "";

        public string InvoiceTypeName
        {
            get { return this.invoiceTypeName; }
            set { this.invoiceTypeName = value; }
        }

        public string PlateNumber
        {
            get { return this.plateNumber; }
            set { this.plateNumber = value; }
        }

        public TraderInvoiceViewModel(DatabaseModel db, Invoice entity) : base(db, entity)
        {
            if (entity.Vehicle != null)
            {
                this.plateNumber = entity.Vehicle.PlateNumber;
            }
        }

        public override void Load(DatabaseModel db, Guid id)
        {
            base.Load(db, id);
            var invType = db.InvoiceTypes.SingleOrDefault(i => i.InvoiceTypeId == this.InvoiceTypeId);
            if(invType != null)
            {
                this.invoiceTypeName = invType.Description;
            }
            var vehicle = db.Vehicles.SingleOrDefault(i => i.VehicleId == this.VehicleId);
            if (vehicle != null)
            {
                this.plateNumber = vehicle.PlateNumber;
            }
        }
    }

    public class TraderFinTransactionViewModel : FinTransactionViewModel
    {
        private string invoiceDescription = "";

        public bool CanDelete
        {
            get
            {
                if (this.InvoiceId.HasValue && this.InvoiceId.Value != Guid.Empty)
                    return false;
                return true;
            }
        }

        public new decimal Amount
        {
            set
            {
                if (base.Amount == value)
                    return;
                base.Amount = value;
                this.SetCorrectAmount();
            }
            get { return base.Amount; }
        }

        public decimal RestAmount
        {
            get { return this.CreditAmount - this.DebitAmount; }
        }

        public string InvoiceDescription
        {
            get { return this.invoiceDescription; }
            set { this.invoiceDescription = value; }
        }

        public override void Load(DatabaseModel db, Guid id)
        {
            base.Load(db, id);
            var inv = db.Invoices.SingleOrDefault(i => i.InvoiceId == this.InvoiceId);
            if (inv != null)
            {
                this.invoiceDescription = inv.Description;
            }
        }

        public TraderFinTransactionViewModel() : base()
        {

        }

        public TraderFinTransactionViewModel(DatabaseModel db, FinTransaction entity) : base(db, entity)
        {
            if (entity.Invoice != null)
            {
                this.invoiceDescription = entity.Invoice.Description;
            }
        }

        protected override void OnPropertyChanged(string propertyName)
        {
            base.OnPropertyChanged(propertyName);
            if(propertyName == "TransactionType")
            {
                this.SetCorrectAmount();
            }
        }

        private void SetCorrectAmount()
        {
            if (this.TransactionType == (int)Common.Enumerators.FinTransactionTypeEnum.Credit)
            {
                this.CreditAmount = this.Amount;
                this.DebitAmount = 0;
            }
            else if (this.TransactionType == (int)Common.Enumerators.FinTransactionTypeEnum.Debit)
            {
                this.DebitAmount = this.Amount;
                this.CreditAmount = 0;
            }
        }
    }
}
