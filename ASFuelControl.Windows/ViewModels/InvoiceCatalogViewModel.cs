using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASFuelControl.Windows.ViewModels
{
    public class InvoiceCatalogViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<QueryInvoiceDataArgs> QueryInvoiceData;

        private List<Data.InvoiceCatalogView> invoices = new List<Data.InvoiceCatalogView>();
        private string filter = "";
        private bool selectCustomer = true;
        private DateTime invoicesFrom = DateTime.Today;
        private DateTime invoicesTo = DateTime.Today;

        public Guid FilterInvoiceTypeId { set; get; }

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

        public InvoiceTypeViewModel[] InvoiceTypes
        {
            get
            {
                List<InvoiceTypeViewModel> list = new List<InvoiceTypeViewModel>();
                InvoiceTypeViewModel empty = new InvoiceTypeViewModel() { Description = "(Χωρίς Επιλογή)", InvoiceTypeId = Guid.Empty };
                list.Add(empty);
                list.AddRange(CommonCache.Instance.InvoiceTypes);
                return list.ToArray();
            }
        }

        public DateTime InvoicesFrom
        {
            set
            {
                this.invoicesFrom = value;
                this.OnPropertyChanged("InvoicesFrom");
            }
            get { return this.invoicesFrom; }
        }

        public DateTime InvoicesTo
        {
            set
            {
                this.invoicesTo = value;
                this.OnPropertyChanged("InvoicesTo");
            }
            get { return this.invoicesTo; }
        }

        public Data.InvoiceCatalogView[] Invoices
        {
            get { return this.invoices.ToArray(); }
        }

        public bool IsAdmin
        {
            get
            {
                Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
                bool isAdmin = false;
                var user = db.ApplicationUsers.SingleOrDefault(a => a.ApplicationUserId == Data.DatabaseModel.UserLoggedIn);
                if (user != null && user.UserLevel == 0)
                    isAdmin = true;
                db.Dispose();
                return isAdmin;
            }
        }

        public Common.Enumerators.EnumItem[] PaymentTypes { set; get; }

        public InvoiceCatalogViewModel()
        {
            List<Common.Enumerators.EnumItem> list = Common.Enumerators.EnumToList.EnumList<Common.Enumerators.PaymentTypeEnum>();
            this.PaymentTypes = list.ToArray();
        }

        public void LoadData()
        {
            using (Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                
                var q = db.InvoiceCatalogViews.Where(i =>
                    i.TransactionDate.Date >= this.InvoicesFrom.Date &&
                    i.TransactionDate.Date <= this.InvoicesTo.Date);
                if (this.FilterInvoiceTypeId != Guid.Empty)
                    q = q.Where(i => i.InvoiceTypeId == this.FilterInvoiceTypeId);
                if (filter != null && filter != "")
                {
                    if (filter.Contains(","))
                    {
                        var numbers = filter.Split(',');
                        q = q.Where(f =>
                            numbers.Contains(f.Number.ToString())
                         );
                    }
                    else
                    {
                        q = q.Where(f =>
                                f.Number.ToString().Contains(filter) ||
                                (f.TraderName.Contains(filter)) ||
                                (f.PlateNumber.Contains(filter)) ||
                                (f.SupplyNumber.Contains(filter))
                             );
                    }
                }
                var qa = q.ToArray();
                foreach (var inv in qa)
                {
                    if (!inv.TraderId.HasValue)
                        continue;
                    var trader = db.Traders.SingleOrDefault(t => t.TraderId == inv.TraderId.Value);
                    if (trader == null)
                        continue;
                    inv.VATNumber = trader.TaxRegistrationNumber;
                }
                this.invoices.Clear();
                this.invoices.AddRange(qa.OrderByDescending(t => t.TransactionDate));
            }
            this.OnPropertyChanged("Invoices");
            this.OnPropertyChanged("IsAdmin");
        }

        public void RefreshIsAdmin()
        {
            this.OnPropertyChanged("IsAdmin");
        }

        //public Guid ReplaceInvoices(Guid[] invoiceIds, Guid invoiceType, Guid traderId, Guid vehicleId)
        //{
        //    Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
        //    try
        //    {
        //        var invType = db.InvoiceTypes.SingleOrDefault(i => i.InvoiceTypeId == invoiceType);
        //        if (invType == null)
        //            return Guid.Empty;

        //        Data.Invoice replacementInvoice = new Data.Invoice();
        //        replacementInvoice.InvoiceId = Guid.NewGuid();
        //        replacementInvoice.TraderId = traderId;
        //        if (vehicleId != Guid.Empty)
        //            replacementInvoice.VehicleId = vehicleId;
        //        replacementInvoice.InvoiceTypeId = invoiceType;
        //        replacementInvoice.TransactionDate = DateTime.Now;
        //        replacementInvoice.NettoAmount = 0;
        //        replacementInvoice.VatAmount = 0;
        //        replacementInvoice.DiscountAmount = 0;
        //        replacementInvoice.TotalAmount = 0;
        //        replacementInvoice.Notes = "Σε αντικατάσταση των:";
        //        foreach (var id in invoiceIds)
        //        {
        //            var invoice = db.Invoices.SingleOrDefault(i => i.InvoiceId == id);
        //            if (invoice == null)
        //                continue;
        //            if (invoice.ChildInvoiceRelations.Count > 0)
        //                continue;
        //            if (invoice.InvoiceLines.Count != 1)
        //                continue;
        //            Data.InvoiceRelation relation = new Data.InvoiceRelation();
        //            relation.InvoiceRelationId = Guid.NewGuid();
        //            relation.ParentInvoiceId = invoice.InvoiceId;
        //            relation.ChildInvoiceId = replacementInvoice.InvoiceId;
        //            relation.RelationType = (int)Common.Enumerators.InvoiceRelationTypeEnum.Replace;
        //            replacementInvoice.ChildInvoiceRelations.Add(relation);

        //            Data.InvoiceLine invLine = new Data.InvoiceLine();
        //            invLine.DiscountAmount = invoice.DiscountAmount.HasValue ? invoice.DiscountAmount.Value : 0;
        //            invLine.FuelDensity = invoice.InvoiceLines.First().FuelDensity;
        //            invLine.FuelTypeId = invoice.InvoiceLines.First().FuelTypeId;
        //            invLine.InvoiceId = replacementInvoice.InvoiceId;
        //            invLine.InvoiceLineId = Guid.NewGuid();
        //            invLine.InvoiceRelationId = relation.InvoiceRelationId;
        //            invLine.TankId = invoice.InvoiceLines.First().TankId;
        //            invLine.Temperature = invoice.InvoiceLines.First().Temperature;
        //            invLine.TotalPrice = invoice.InvoiceLines.First().TotalPrice;
        //            invLine.UnitPrice = invoice.InvoiceLines.First().UnitPrice;
        //            invLine.VatAmount = invoice.InvoiceLines.First().VatAmount;
        //            invLine.VatPercentage = invoice.InvoiceLines.First().VatPercentage;
        //            invLine.Volume = invoice.InvoiceLines.First().Volume;
        //            invLine.VolumeNormalized = invoice.InvoiceLines.First().VolumeNormalized;
        //            invLine.SaleTransactionId = invoice.InvoiceLines.First().SaleTransactionId;
        //            invLine.TankFillingId = invoice.InvoiceLines.First().TankFillingId;
        //            replacementInvoice.InvoiceLines.Add(invLine);
        //            replacementInvoice.NettoAmount = replacementInvoice.NettoAmount.Value + invLine.TotalPrice - invLine.VatAmount - invLine.DiscountAmount;
        //            replacementInvoice.VatAmount = replacementInvoice.VatAmount + invLine.VatAmount;
        //            replacementInvoice.DiscountAmount = replacementInvoice.DiscountAmount + invLine.DiscountAmount;
        //            replacementInvoice.TotalAmount = replacementInvoice.TotalAmount + invLine.TotalPrice;
        //            replacementInvoice.Notes = replacementInvoice.Notes + invoice.Description + ",";
        //        }
        //        replacementInvoice.Notes = replacementInvoice.Notes.Substring(0, replacementInvoice.Notes.Length - 1);
        //        replacementInvoice.ApplicationUserId = Data.DatabaseModel.CurrentUserId;
        //        replacementInvoice.Number = invType.LastNumber + 1;
        //        invType.LastNumber = replacementInvoice.Number;
        //        replacementInvoice.IsPrinted = true;
        //        db.Add(replacementInvoice);
        //        db.SaveChanges();
        //        return replacementInvoice.InvoiceId;
        //    }
        //    catch
        //    {
        //        return Guid.Empty;
        //    }
        //    finally
        //    {
        //        db.Dispose();
        //    }
        //}

        public Guid CancelInvoices(Guid[] invoiceIds, Guid invoiceType, Guid traderId, Guid vehicleId)
        {
            Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
            try
            { 
                var invType = db.InvoiceTypes.SingleOrDefault(i => i.InvoiceTypeId == invoiceType);
                if (invType == null)
                    return Guid.Empty;
                if(!invType.IsCancelation.HasValue || !invType.IsCancelation.Value)
                    return Guid.Empty;

                Data.Invoice replacementInvoice = new Data.Invoice();
                replacementInvoice.InvoiceId = Guid.NewGuid();
                if (traderId != Guid.Empty)
                    replacementInvoice.TraderId = traderId;
                if (vehicleId != Guid.Empty)
                    replacementInvoice.VehicleId = vehicleId;
                replacementInvoice.InvoiceTypeId = invoiceType;
                replacementInvoice.TransactionDate = DateTime.Now;
                replacementInvoice.NettoAmount = 0;
                replacementInvoice.VatAmount = 0;
                replacementInvoice.DiscountAmount = 0;
                replacementInvoice.TotalAmount = 0;
                replacementInvoice.Notes = "Ακύρωση των:";
                foreach (var id in invoiceIds)
                {
                    var invoice = db.Invoices.SingleOrDefault(i => i.InvoiceId == id);
                    if (invoice == null)
                        continue;
                    if (invoice.ParentInvoiceRelations.Count > 0)
                        continue;
                    if (invoice.InvoiceLines.Count != 1)
                        continue;
                    Data.InvoiceRelation relation = new Data.InvoiceRelation();
                    relation.InvoiceRelationId = Guid.NewGuid();
                    relation.ParentInvoiceId = invoice.InvoiceId;
                    relation.ChildInvoiceId = replacementInvoice.InvoiceId;
                    relation.RelationType = (int)Common.Enumerators.InvoiceRelationTypeEnum.Replace;
                    replacementInvoice.ChildInvoiceRelations.Add(relation);

                    Data.InvoiceLine invLine = new Data.InvoiceLine();
                    invLine.DiscountAmount = invoice.DiscountAmount;
                    invLine.FuelDensity = invoice.InvoiceLines.First().FuelDensity;
                    invLine.FuelTypeId = invoice.InvoiceLines.First().FuelTypeId;
                    invLine.InvoiceId = replacementInvoice.InvoiceId;
                    invLine.InvoiceLineId = Guid.NewGuid();
                    invLine.InvoiceRelationId = relation.InvoiceRelationId;
                    invLine.TankId = invoice.InvoiceLines.First().TankId;
                    invLine.Temperature = invoice.InvoiceLines.First().Temperature;
                    invLine.TotalPrice = invoice.InvoiceLines.First().TotalPrice;
                    invLine.UnitPrice = invoice.InvoiceLines.First().UnitPrice;
                    invLine.VatAmount = invoice.InvoiceLines.First().VatAmount;
                    invLine.VatPercentage = invoice.InvoiceLines.First().VatPercentage;
                    invLine.Volume = invoice.InvoiceLines.First().Volume;
                    invLine.VolumeNormalized = invoice.InvoiceLines.First().VolumeNormalized;
                    invLine.SaleTransactionId = invoice.InvoiceLines.First().SaleTransactionId;
                    invLine.TankFillingId = invoice.InvoiceLines.First().TankFillingId;
                    replacementInvoice.NettoAmount = replacementInvoice.NettoAmount.Value + invLine.TotalPrice - invLine.VatAmount - invLine.DiscountAmount;
                    replacementInvoice.VatAmount = replacementInvoice.VatAmount + invLine.VatAmount;
                    replacementInvoice.DiscountAmount = replacementInvoice.DiscountAmount + invLine.DiscountAmount;
                    replacementInvoice.TotalAmount = replacementInvoice.TotalAmount + invLine.TotalPrice;
                    replacementInvoice.Notes = replacementInvoice.Notes + invoice.Description + ",";
                    replacementInvoice.InvoiceLines.Add(invLine);
                }
                replacementInvoice.Notes = replacementInvoice.Notes.Substring(0, replacementInvoice.Notes.Length - 1);
                replacementInvoice.ApplicationUserId = Data.DatabaseModel.CurrentUserId;
                //replacementInvoice.Number = invType.LastNumber + 1;
                //invType.LastNumber = replacementInvoice.Number;
                //replacementInvoice.IsPrinted = true;
                db.Add(replacementInvoice);
                db.SaveChanges();
                return replacementInvoice.InvoiceId;
            }
            catch
            {
                return Guid.Empty;
            }
            finally
            {
                db.Dispose();
            }
        }

        public Guid TransformInvoices(Guid[] invoiceIds, Guid invoiceType, Guid traderId, Guid vehicleId)
        {
            using (Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                var invoice = db.Invoices.Where(i => i.InvoiceId == invoiceIds[0]).SingleOrDefault();
                if (invoice == null)
                    return Guid.Empty;

                var invType = db.InvoiceTypes.SingleOrDefault(i => i.InvoiceTypeId == invoiceType);
                if (invType == null)
                    return Guid.Empty;

                Guid fromType = db.Invoices.Where(i => invoiceIds.Contains(i.InvoiceId)).Select(i => i.InvoiceTypeId).FirstOrDefault();
                var transformationType = db.InvoiceTypeTransforms.Where(i => i.ParentInvoiceTypeId == fromType && i.ChildInvoiceTypeId == invoiceType).FirstOrDefault();
                if (transformationType == null)
                    return Guid.Empty;

                Data.Invoice newInvoice = new Data.Invoice();
                newInvoice.InvoiceId = Guid.NewGuid();
                newInvoice.TraderId = traderId;
                if (vehicleId != Guid.Empty)
                    newInvoice.VehicleId = vehicleId;
                newInvoice.InvoiceTypeId = invoiceType;
                newInvoice.TransactionDate = DateTime.Now;
                newInvoice.NettoAmount = 0;
                newInvoice.VatAmount = 0;
                newInvoice.DiscountAmount = 0;
                newInvoice.TotalAmount = 0;
                newInvoice.Notes = transformationType.NotesAddition;

                var lines = db.InvoiceLines.Where(i => i.InvoiceId == invoiceIds[0]);
                Dictionary<Guid, Guid> lineRelations = new Dictionary<Guid, Guid>();
                if (transformationType.TransformationMode == (int)Common.Enumerators.InvoiceTransformationTypeEnum.OneToMany)
                {
                    if (invoiceIds.Length != 1)
                        return Guid.Empty;
                    if (this.QueryInvoiceData != null)
                    {
                        List<VolumeData> volumeData = new List<VolumeData>();
                        foreach (var line in lines)
                        {
                            var children = line.InvoiceLineRelations_ParentLineId.Select(c => c.InvoiceLine_ChildRelationId).ToArray();
                            decimal sumVolume = children.Sum(c => c.Volume);
                            if (line.Volume - sumVolume <= 0)
                                continue;
                            VolumeData vd = new VolumeData();
                            vd.Description = line.FuelType.Name;
                            vd.InvoiceLineId = line.InvoiceLineId;
                            vd.TaxValue = line.VatAmount;
                            vd.VatPercentage = line.VatPercentage;
                            vd.UnitPrice = line.UnitPrice;
                            vd.MaxAllowedVolume = line.Volume - sumVolume;
                            vd.Volume = line.Volume > vd.MaxAllowedVolume ? vd.MaxAllowedVolume : line.Volume;
                            vd.DiscountAmount = line.DiscountAmount;
                            volumeData.Add(vd);
                        }
                        QueryInvoiceDataArgs args = new QueryInvoiceDataArgs();
                        args.Volumes = volumeData.ToArray();
                        args.TraderId = traderId;
                        args.VehicleId = vehicleId;
                        this.QueryInvoiceData(this, args);
                        if (args.Cancel)
                            return Guid.Empty;
                        newInvoice.TraderId = args.TraderId;
                        if (args.VehicleId != Guid.Empty)
                            newInvoice.VehicleId = args.VehicleId;

                        foreach (var vd in args.Volumes)
                        {
                            var line = invoice.InvoiceLines.Where(i => i.InvoiceLineId == vd.InvoiceLineId).SingleOrDefault();
                            if (line == null)
                                continue;
                            Data.InvoiceLine invLine = new Data.InvoiceLine();
                            invLine.DiscountAmount = line.DiscountAmount;
                            invLine.FuelDensity = line.FuelDensity;
                            invLine.FuelTypeId = line.FuelTypeId;
                            invLine.InvoiceId = newInvoice.InvoiceId;
                            invLine.InvoiceLineId = Guid.NewGuid();
                            invLine.TankId = line.TankId;
                            invLine.Temperature = line.Temperature;
                            invLine.TotalPrice = line.TotalPrice;
                            invLine.UnitPrice = vd.UnitPrice;
                            invLine.VatAmount = line.VatAmount;
                            invLine.VatPercentage = line.VatPercentage;
                            invLine.Volume = vd.Volume > vd.MaxAllowedVolume ? vd.MaxAllowedVolume : vd.Volume;
                            invLine.VolumeNormalized = vd.Volume;
                            invLine.SaleTransactionId = line.SaleTransactionId;
                            invLine.TankFillingId = line.TankFillingId;
                            newInvoice.InvoiceLines.Add(invLine);
                            lineRelations.Add(line.InvoiceLineId, invLine.InvoiceLineId);
                            db.Add(invLine);
                        }
                    }
                }
                else
                {
                    foreach (var line in lines)
                    {
                        Data.InvoiceLine invLine = new Data.InvoiceLine();
                        invLine.DiscountAmount = line.DiscountAmount;
                        invLine.FuelDensity = line.FuelDensity;
                        invLine.FuelTypeId = line.FuelTypeId;
                        invLine.InvoiceId = newInvoice.InvoiceId;
                        invLine.InvoiceLineId = Guid.NewGuid();
                        invLine.TankId = line.TankId;
                        invLine.Temperature = line.Temperature;
                        invLine.TotalPrice = line.TotalPrice;
                        invLine.UnitPrice = line.UnitPrice;
                        invLine.VatAmount = line.VatAmount;
                        invLine.VatPercentage = line.VatPercentage;
                        invLine.Volume = line.Volume;
                        invLine.VolumeNormalized = line.VolumeNormalized;
                        invLine.SaleTransactionId = line.SaleTransactionId;
                        invLine.TankFillingId = line.TankFillingId;
                        newInvoice.InvoiceLines.Add(invLine);
                        lineRelations.Add(line.InvoiceLineId, invLine.InvoiceLineId);
                        db.Add(invLine);
                    }
                }
                foreach (var id in invoiceIds)
                {
                    if (invoice == null)
                        continue;
                    Data.InvoiceRelation relation = new Data.InvoiceRelation();
                    relation.InvoiceRelationId = Guid.NewGuid();
                    relation.ParentInvoiceId = id;
                    relation.ChildInvoiceId = newInvoice.InvoiceId;
                    relation.RelationType = (int)Common.Enumerators.InvoiceRelationTypeEnum.Replace;
                    db.Add(relation);
                    foreach(Guid plid in lineRelations.Keys)
                    {
                        if (invoice.InvoiceLines.SingleOrDefault(l => l.InvoiceLineId == plid) == null)
                            continue;
                        Data.InvoiceLineRelation lineRelation = new Data.InvoiceLineRelation();
                        lineRelation.InvoiceLineRelationId = Guid.NewGuid();
                        lineRelation.ParentLineId = plid;
                        lineRelation.ChildRelationId = lineRelations[plid];
                        lineRelation.InvoiceRelationId = relation.InvoiceRelationId;
                        db.Add(lineRelation);
                    }
                }
                newInvoice.ApplicationUserId = Data.DatabaseModel.CurrentUserId;
                //newInvoice.Number = invType.LastNumber + 1;
                //invType.LastNumber = newInvoice.Number;
                newInvoice.IsPrinted = true;
                db.Add(newInvoice);
                db.SaveChanges();
                return newInvoice.InvoiceId;
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class QueryInvoiceDataArgs : EventArgs, INotifyPropertyChanged
    {
        private VolumeData[] volumes = new VolumeData[] { };

        public event PropertyChangedEventHandler PropertyChanged;
        private Guid traderId;
        private Guid vehicleId;
        private string traderName;
        private string plateNumber;

        public VolumeData[] Volumes
        {
            set
            {
                this.volumes = value;
                if (value == null)
                    return;
                foreach(var vd in this.volumes)
                {
                    vd.PropertyChanged += Vd_PropertyChanged;
                }
            }
            get { return this.volumes; }
        }

        private void Vd_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            this.OnPropertyChanged("Volumes");
        }

        public Guid TraderId
        {
            set
            {
                if (this.traderId == value)
                    return;
                this.traderId = value;
                using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
                {
                    var trader = db.Traders.SingleOrDefault(t => t.TraderId == traderId);
                    if (trader == null)
                    {
                        this.traderName = "";
                        this.vehicleId = Guid.Empty;
                    }
                    else
                    {
                        this.traderName = trader.Name;
                        this.vehicleId = Guid.Empty;
                    }
                    this.OnPropertyChanged("TraderName");
                    this.OnPropertyChanged("PlateNumber");
                }
                this.OnPropertyChanged("TraderId");
            }
            get
            {
                return this.traderId;
            }
        }

        public Guid VehicleId
        {
            set
            {
                if (this.vehicleId == value)
                    return;
                this.vehicleId = value;
                using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
                {
                    var vehicle = db.Vehicles.SingleOrDefault(t => t.VehicleId == vehicleId);
                    if (vehicle == null)
                    {
                        this.plateNumber = "";
                        this.TraderId = Guid.Empty;
                    }
                    else
                    {
                        this.plateNumber = vehicle.PlateNumber;
                        this.TraderId = vehicle.TraderId;
                    }
                    this.OnPropertyChanged("PlateNumber");
                }
                this.OnPropertyChanged("VehicleId");
            }
            get
            {
                return this.vehicleId;
            }
        }

        public string TraderName
        {
            set
            {
                if (this.traderName == value)
                    return;
                this.traderName = value;
                this.OnPropertyChanged("TraderName");
            }
            get
            {
                return this.traderName;
            }
        }
        public string PlateNumber
        {
            set
            {
                if (this.plateNumber == value)
                    return;
                this.plateNumber = value;
                this.OnPropertyChanged("PlateNumber");
            }
            get
            {
                return this.plateNumber;
            }
        }

        public bool Cancel { set; get; }

        private void OnPropertyChanged(string propName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
    }

    public class VolumeData : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string description;
        private decimal volume;
        private decimal unitPrice;
        private decimal discountAmount;
        private decimal taxValue;
        private decimal vatPercentage;
        private decimal maxAllowedVolume;


        public string Description
        {
            set
            {
                if (this.description == value)
                    return;
                this.description = value;
                this.OnPropertyChanged("Description");
            }
            get
            {
                return this.description;
            }
        }
        public decimal Volume
        {
            set
            {
                if (this.volume == value)
                    return;
                if(value > this.maxAllowedVolume)
                {
                    this.OnPropertyChanged("Volume");
                    return;
                }
                this.volume = value;
                this.OnPropertyChanged("Volume");
            }
            get
            {
                return this.volume;
            }
        }
        public decimal UnitPrice
        {
            set
            {
                if (this.unitPrice == value)
                    return;
                this.unitPrice = value;
                this.OnPropertyChanged("UnitPrice");
            }
            get
            {
                return this.unitPrice;
            }
        }
        public decimal DiscountAmount
        {
            set
            {
                if (this.discountAmount == value)
                    return;
                this.discountAmount = value;
                this.OnPropertyChanged("DiscountAmount");
            }
            get
            {
                return this.discountAmount;
            }
        }
        public decimal TaxValue
        {
            set
            {
                if (this.taxValue == value)
                    return;
                this.taxValue = value;
                this.OnPropertyChanged("TaxValue");
            }
            get
            {
                return this.taxValue;
            }
        }
        public decimal VatPercentage
        {
            set
            {
                if (this.vatPercentage == value)
                    return;
                this.vatPercentage = value;
                this.OnPropertyChanged("VatPercentage");
            }
            get
            {
                return this.vatPercentage;
            }
        }
        public decimal MaxAllowedVolume
        {
            set
            {
                if (this.maxAllowedVolume == value)
                    return;
                this.maxAllowedVolume = value;
                this.OnPropertyChanged("MaxAllowedVolume");
            }
            get
            {
                return this.maxAllowedVolume;
            }
        }

        public Guid InvoiceLineId { set; get; }

        private void OnPropertyChanged(string propName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
    }

    //public class InvoiceCatalogView
    //{
    //    public string Description
    //    {
    //        get
    //        {
    //            if(this.InvoiceTypeDesc == null)
    //                return this.Number.ToString() + " " + this.TransactionDate.ToString("dd/MM/yyyy HH:mm");
    //            else
    //                return this.InvoiceTypeDesc + " " + this.Number.ToString() + " " + this.TransactionDate.ToString("dd/MM/yyyy HH:mm");
    //        }
    //    }
    //    public string TraderName { set; get; }
    //    public string PlateNumber { set; get; }
    //    public int PaymentType { set; get; }
    //    public decimal NettoAmount { set; get; }
    //    public decimal DiscountAmount { set; get; }
    //    public decimal VatAmount { set; get; }
    //    public bool VatExemption { set; get; }
    //    public decimal TotalAmount { set; get; }
    //    public DateTime TransactionDate { set; get; }
    //    public string InvoiceTypeDesc { set; get; }
    //    public int InvoiceTypeSign { set; get; }
    //    public int Number { set; get; }
    //    public bool IsCancelation { set; get; }
    //    public bool IsReplaced
    //    {
    //        get { return this.ReplaceInvoicesCount > 0; }
    //    }
    //    public int ReplaceInvoicesCount { set; get; }

    //    public decimal VatValue { set; get; }
    //    public decimal FinancialTransactionSum { set; get; }

    //    public decimal VATAmountEx
    //    {
    //        get
    //        {
    //            return this.IsCancelation ? -this.VatAmount : this.VatAmount;
    //        }
    //    }

    //    public decimal TotalAmountEx
    //    {
    //        get
    //        {
    //            return this.IsCancelation ? -this.TotalAmount : this.TotalAmount;
    //        }
    //    }

    //    public decimal NettoAmountEx
    //    {
    //        get
    //        {
    //            return this.IsCancelation ? -this.NettoAmount : this.NettoAmount;
    //        }
    //    }

    //    public decimal PreDiscountTotal
    //    {
    //        get
    //        {
    //            decimal total = this.TotalAmount + this.DiscountAmount;
    //            return total;
    //        }
    //    }

    //    public decimal PreDiscountVAT
    //    {
    //        get
    //        {
    //            decimal vatMult = 1 + this.VatValue / 100;
    //            decimal vat = 0;

    //            if (this.VatExemption)
    //            {
    //                vat = 0;
    //            }
    //            else
    //            {
    //                vat = this.PreDiscountTotal - decimal.Round(this.PreDiscountTotal / vatMult, 2);
    //            }
    //            return vat;
    //        }
    //    }

    //    public decimal PreDiscountNetto
    //    {
    //        get
    //        {
    //            return this.NettoAmount - DiscountNettoAmount;
    //        }
    //    }

    //    public decimal DiscountNettoAmount
    //    {
    //        get
    //        {
    //            if (this.NettoAmount == 0)
    //                return 0;
    //            decimal vatRatio = this.PreDiscountTotal / this.NettoAmount;
    //            decimal disc = Math.Round(this.DiscountAmount / vatRatio, 2);
    //            return disc;
    //        }
    //    }

    //    public decimal CreditAmount{ set; get; }

    //    public decimal CashAmount { set; get; }

    //    //public decimal DiscountPercentage
    //    //{
    //    //    get
    //    //    {
    //    //        decimal percentage = 0;
    //    //        decimal sum = 0;
    //    //        foreach (InvoiceLine invLine in this.InvoiceLines)
    //    //        {
    //    //            percentage = percentage + invLine.DiscountPercentage * invLine.TotalPrice;
    //    //            sum = sum + invLine.TotalPrice;
    //    //        }
    //    //        if (sum == 0)
    //    //            return 0;
    //    //        return percentage / sum;
    //    //    }
    //    //}

    //    public decimal RestAmount
    //    {
    //        get
    //        {
    //            if (this.IsReplaced)
    //                return 0;
    //            return this.TotalAmount - this.FinancialTransactionSum;
    //        }
    //    }

    //    public InvoiceCatalogView()
    //    {
    //    }

    //    public InvoiceCatalogView(decimal vatValue)
    //    {
    //        this.VatValue = this.VatValue;
    //    }
    //}
}
