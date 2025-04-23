using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Telerik.OpenAccess;
using ASFuelControl.Data.Implementation;
using System.Collections;
using System.ComponentModel;
using System.Runtime.Serialization;
using ASFuelControl.Logging;

namespace ASFuelControl.Data
{
    partial class DatabaseModel :INotifyPropertyChanged
    {
        private bool databaseChanged = false;

        private List<string> tablesToLog = new List<string>();

        private static Guid currentUserId;

        public static Guid CurrentUserId 
        {
            set { currentUserId = value; }
            get 
            { 
                return currentUserId; 
            } 
        }

        private static Guid currentShiftId;

        public static Guid CurrentShiftId
        {
            set { currentShiftId = value; }
            get
            {
                return currentShiftId;
            }
        }

        public static Guid UserLoggedIn { set; get; }

        public bool DatabaseChanged
        {
            set 
            {
                if (this.databaseChanged == value)
                    return;
                this.databaseChanged = value;
                if(this.PropertyChanged!= null)
                    this.PropertyChanged(this, new PropertyChangedEventArgs("DatabaseChanged"));
            }
            get { return this.databaseChanged; }
        }

        public event EventHandler<ValidationErrorEventArgs> ValidationError;

        private Dictionary<object, ErrorInfo> errorInfos = new Dictionary<object, ErrorInfo>();
        private Validators.ValidatorContainer validatorContainer = new Validators.ValidatorContainer();

        #region events

        public event EventHandler<SendDataEventArgs> DataForSendReady;
        public event EventHandler<DatabaseChangeArgs> BeforeInsert;
        public event EventHandler<DatabaseChangeArgs> AfterInsert;
        public event EventHandler<DatabaseChangeArgs> BeforeUpdate;
        public event EventHandler<DatabaseChangeArgs> AfterUpdate;
        public event EventHandler<DatabaseChangeArgs> BeforeDelete;
        public event EventHandler<DatabaseChangeArgs> AfterDelete;

        void Events_ObjectConstructed(object sender, ObjectConstructedEventArgs e)
        {
            IList list = this.validatorContainer.GetValidators(e.PersistentObject.GetType());
            ErrorInfo globalError = new ErrorInfo();
            if (list != null)
            {
                foreach (object validator in list)
                {
                    this.validatorContainer.InitializeObject((IDataErrorInfo)e.PersistentObject, validator);
                }
            }
        }

        void Events_Removed(object sender, RemoveEventArgs e)
        {
            IList list = this.validatorContainer.GetValidators(e.PersistentObject.GetType());
            ErrorInfo globalError = new ErrorInfo();
            if (list != null)
            {
                foreach (object validator in list)
                {
                    ErrorInfo error = this.validatorContainer.ExecuteAfterDelete(e, validator);
                    if (error == null)
                        continue;
                    globalError.MergeErrorInfo(error);
                }
                if (globalError.Entries.Length > 0)
                {
                    if (this.ValidationError != null)
                    {
                        ValidationErrorEventArgs args = new ValidationErrorEventArgs(globalError, e);
                        this.ValidationError(this, args);
                        e.Cancel = args.Cancel;
                    }
                }
            }
            this.DatabaseChanged = true;
        }

        void Events_Changed(object sender, ChangeEventArgs e)
        {
            IList list = this.validatorContainer.GetValidators(e.PersistentObject.GetType());
            ErrorInfo globalError = new ErrorInfo();
            if (list != null)
            {
                foreach (object validator in list)
                {
                    ErrorInfo error = this.validatorContainer.ExecuteAfterUpdate(e, validator);
                    if (error == null)
                        continue;
                    globalError.MergeErrorInfo(error);
                }
                if (globalError.Entries.Length > 0)
                {
                    if (this.ValidationError != null)
                    {
                        ValidationErrorEventArgs args = new ValidationErrorEventArgs(globalError, e);
                        this.ValidationError(this, args);

                        if (args.Cancel)
                        {
                            validatorContainer.ResetChanges(e);
                        }
                    }
                }
                else
                {
                    if (this.ValidationError != null)
                    {
                        ValidationErrorEventArgs args = new ValidationErrorEventArgs(globalError, e);
                        this.ValidationError(this, args);
                    }
                }
            }
            this.DatabaseChanged = true;
            this.LogChange(e);
            if (e.PersistentObject.GetType() == typeof(Data.Invoice))
            {
                Data.Invoice invoice = e.PersistentObject as Data.Invoice;
                if ((e.PropertyName == "TotalAmount" || e.PropertyName == "InvoiceTypeId" || e.PropertyName == "TransactionDate" || e.PropertyName == "PaymentType") 
                    && invoice.InvoiceTypeId != Guid.Empty && invoice.InvoiceType.HasFinancialTransactions.HasValue && invoice.InvoiceType.HasFinancialTransactions.Value)
                {
                    invoice.CreateFinancialTransactions();
                }
            }
        }

        void Events_Changing(object sender, ChangeEventArgs e)
        {
            IList list = this.validatorContainer.GetValidators(e.PersistentObject.GetType());
            ErrorInfo globalError = new ErrorInfo();
            if (list != null)
            {
                foreach (object validator in list)
                {
                    ErrorInfo error = this.validatorContainer.ExecuteBeforeUpdate(e, validator);
                    if (error == null)
                        continue;
                    globalError.MergeErrorInfo(error);
                }
                if (globalError.Entries.Length > 0)
                {
                    if (this.ValidationError != null)
                    {
                        ValidationErrorEventArgs args = new ValidationErrorEventArgs(globalError, e);
                        this.ValidationError(this, args);

                        if (args.Cancel)
                        {
                            validatorContainer.ResetChanges(e);
                            e.Cancel = true;
                        }
                    }
                }
            }
        }

        void Events_Removing(object sender, RemoveEventArgs e)
        {
            IList list = this.validatorContainer.GetValidators(e.PersistentObject.GetType());
            ErrorInfo globalError = new ErrorInfo();
            if (list != null)
            {
                foreach (object validator in list)
                {
                    ErrorInfo error = this.validatorContainer.ExecuteBeforeDelete(e, validator);
                    if (error == null)
                        continue;
                    globalError.MergeErrorInfo(error);
                }
                if (globalError.Entries.Length > 0)
                {
                    if (this.ValidationError != null)
                    {
                        ValidationErrorEventArgs args = new ValidationErrorEventArgs(globalError, e);
                        this.ValidationError(this, args);
                        e.Cancel = args.Cancel;
                    }
                }
            }
        }

        void Events_Adding(object sender, AddEventArgs e)
        {
            IList list = this.validatorContainer.GetValidators(e.PersistentObject.GetType());
            ErrorInfo globalError = new ErrorInfo();
            if (list != null)
            {
                foreach (object validator in list)
                {
                    ErrorInfo error = this.validatorContainer.ExecuteBeforeInsert(e, validator);
                    if (error == null)
                        continue;
                    globalError.MergeErrorInfo(error);
                }
                if (globalError.Entries.Length > 0)
                {
                    if (this.ValidationError != null)
                    {
                        ValidationErrorEventArgs args = new ValidationErrorEventArgs(globalError, e);
                        this.ValidationError(this, args);
                        e.Cancel = args.Cancel;
                    }
                }
            }
        }

        void Events_Added(object sender, AddEventArgs e)
        {
            ObjectKey key = ObjectKey.Create(e.PersistentObject);
            this.SetKey(e.PersistentObject, key);

            IList list = this.validatorContainer.GetValidators(e.PersistentObject.GetType());
            ErrorInfo globalError = new ErrorInfo();
            if (list != null)
            {
                foreach (object validator in list)
                {
                    ErrorInfo error = this.validatorContainer.ExecuteAfterInsert(e, validator);
                    if (error == null)
                        continue;
                    globalError.MergeErrorInfo(error);
                }
                if (globalError.Entries.Length > 0)
                {
                    if (this.ValidationError != null)
                    {
                        ValidationErrorEventArgs args = new ValidationErrorEventArgs(globalError, e);
                        this.ValidationError(this, args);
                        e.Cancel = args.Cancel;

                    }
                }
            }
            this.DatabaseChanged = true;
        }

        private void LogChange(ChangeEventArgs e)
        {
            string tabName = e.PersistentObject.GetType().Name.ToString();
            if (!this.tablesToLog.Contains(tabName))
                return;
            DatabaseModel db = DatabaseModel.GetContext(e.PersistentObject) as DatabaseModel;
            if(db == null)
                return;
            
            try
            {
                Telerik.OpenAccess.Metadata.Relational.MetaTable table = db.Metadata.Tables.Where(t => t.Name == tabName).FirstOrDefault();
                Telerik.OpenAccess.Metadata.Relational.MetaColumn pkColumn = table.Columns.Where(c => c.IsPrimaryKey).FirstOrDefault();
                string pkName = pkColumn.Name;
                System.Reflection.PropertyInfo pInfo = e.PersistentObject.GetType().GetProperty(pkName);
                if (pInfo == null)
                    return;

                if (tabName == "Tank")
                {
                    List<string> excludeColumns = new List<string>() { "WaterLevel", "FuelLevel", "FuelLevelBase", "Temperatire" };
                    if (excludeColumns.Contains(e.PropertyName))
                        return;
                }

                string primaryKeyValue = pInfo.GetValue(e.PersistentObject, null).ToString();

                ChangeLog log = new ChangeLog();
                log.ChangeLogId = Guid.NewGuid();
                log.PrimaryKey = primaryKeyValue;
                log.TableName = tabName;
                log.ColumnName = e.PropertyName;
                if (e.OldValue == null)
                    log.OldValue = "";
                else
                    log.OldValue = e.OldValue.ToString();
                if (e.NewValue == null)
                    log.NewValue = "";
                else
                    log.NewValue = e.NewValue.ToString();
                log.DateTimeStamp = DateTime.Now;
                ApplicationUser user = db.ApplicationUsers.Where(u => u.ApplicationUserId == DatabaseModel.UserLoggedIn).FirstOrDefault();
                if (user == null)
                    log.ApplicationUserName = "";
                else
                    log.ApplicationUserName = user.UserName;

                if (tabName == "Option")
                {
                    Option option = e.PersistentObject as Option;
                    log.AdditionalDescription = option.OptionKey;
                    log.OldValue = OptionHandler.Instance.DecryptValue(e.OldValue.ToString());
                    log.NewValue = OptionHandler.Instance.DecryptValue(e.NewValue.ToString());
                }
                else if (tabName == "DispenserSetting")
                {
                    DispenserSetting setting = e.PersistentObject as DispenserSetting;
                    log.AdditionalDescription = setting.SettingKey;
                }

                
                if (tabName == "Dispenser" || tabName == "Nozzle" || tabName == "Tank" || tabName == "CommunicationController")
                {
                    DeviceSetting.ChangeDescription(e.PersistentObject);
                }

                //else if (tabName == "TankSetting")
                //{
                //    TankSetting setting = e.PersistentObject as TankSetting;
                //    log.AdditionalDescription = setting.SettingKey;
                //}
                db.Add(log);
            }
            catch
            {
            }
        }

        #endregion

        #region public methods

        public bool isOnSave = false;

        public void SerializeItem(object entity, string fileName, IFormatter formatter)
        {
            System.IO.FileStream s = new System.IO.FileStream(fileName, System.IO.FileMode.Create);
            formatter.Serialize(s, entity);
            s.Close();
        }

        public string SerializeEntity(object entity)
        {
            System.Xml.Serialization.XmlSerializer xmlSerializer = new System.Xml.Serialization.XmlSerializer(entity.GetType());
            System.IO.StringWriter textWriter = new System.IO.StringWriter();

            xmlSerializer.Serialize(textWriter, entity);
            return textWriter.ToString();
        }

        public object DeserializeItem(string fileName, IFormatter formatter)
        {
            System.IO.FileStream s = new System.IO.FileStream(fileName, System.IO.FileMode.Open);
            object entity  = formatter.Deserialize(s);
            return entity;
        }

        public override void SaveChanges(Telerik.OpenAccess.ConcurrencyConflictsProcessingMode failureMode)
        {
            //IObjectScope scope = this.GetScope();
            
            if (isOnSave)
                return;
            try
            {
                isOnSave = true;

                //if (!scope.Transaction.IsActive)
                //    scope.Transaction.Begin();

                Telerik.OpenAccess.ContextChanges changes = this.GetChanges();
                IList<object> inserts = changes.GetInserts<object>();
                IList<object> updates = changes.GetUpdates<object>();
                IList<object> deletes = changes.GetDeletes<object>();
                List<string> fileNames = new List<string>();
                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                if (inserts != null && inserts.Count > 0)
                {

                    foreach (object entity in inserts)
                    {
                        string directoryName = "Inserts\\" + entity.GetType().Name;
                        if (!System.IO.Directory.Exists(directoryName))
                            System.IO.Directory.CreateDirectory(directoryName);
                        string fileName = directoryName + "\\" + Guid.NewGuid().ToString() + ".insert";
                        fileNames.Add(fileName);
                        this.SerializeItem(entity, fileName, formatter);
                    }
                    if (BeforeInsert != null)
                    {
                        DatabaseChangeArgs args = new DatabaseChangeArgs { Changes = inserts };
                        this.BeforeInsert(this, args);
                        if (args.Cancel)
                            return;
                    }
                }
                if (updates != null && updates.Count > 0)
                {
                    foreach (object entity in updates)
                    {
                        string directoryName = "Updates\\" + entity.GetType().Name;
                        if (!System.IO.Directory.Exists(directoryName))
                            System.IO.Directory.CreateDirectory(directoryName);
                        string fileName = directoryName + "\\" + Guid.NewGuid().ToString() + ".update";
                        fileNames.Add(fileName);
                        this.SerializeItem(entity, fileName, formatter);
                    }
                    if (this.BeforeUpdate != null)
                    {
                        DatabaseChangeArgs args = new DatabaseChangeArgs { Changes = updates };
                        this.BeforeUpdate(this, args);
                        if (args.Cancel)
                            return;
                    }
                }
                if (deletes != null && deletes.Count > 0)
                {
                    //foreach (object entity in deletes)
                    //{
                    //    string directoryName = "Deletes\\" + entity.GetType().Name;
                    //    if (!System.IO.Directory.Exists(directoryName))
                    //        System.IO.Directory.CreateDirectory(directoryName);
                    //    string fileName = directoryName + "\\" + Guid.NewGuid().ToString() + ".delete";
                    //    fileNames.Add(fileName);
                    //    this.SerializeItem(entity, fileName, formatter);
                    //}
                    if (this.BeforeDelete != null)
                    {
                        DatabaseChangeArgs args = new DatabaseChangeArgs { Changes = deletes };
                        this.BeforeDelete(this, args);
                        if (args.Cancel)
                            return;
                    }
                }

                try
                {
                    base.SaveChanges(failureMode);
                    foreach (string fileName in fileNames)
                    {
                        System.IO.File.Delete(fileName);
                    }
                }
                catch(Exception ex1)
                {
                    System.Threading.Thread.Sleep(1000);
                    //this.ClearChanges();
                    //foreach(string fileName in fileNames)
                    //{
                    //    if(fileName.EndsWith(".update"))
                    //    {
                    //        object entity = this.DeserializeItem(fileName, formatter);
                    //        this.Add(entity);
                    //    }
                    //}
                    //this.ClearChanges();
                    //foreach(string fileName in fileNames)
                    //{
                    //    if(fileName.EndsWith(".insert"))
                    //    {
                    //        object entity = this.DeserializeItem(fileName, formatter);
                    //        this.Add(entity);
                    //    }
                    //    else if(fileName.EndsWith(".delete"))
                    //    {
                    //        object entity = this.DeserializeItem(fileName, formatter);
                    //        this.Add(entity);
                    //        this.Delete(entity);
                    //    }
                    //}

                    //this.SaveChanges();
                    //foreach(string fileName in fileNames)
                    //{
                    //    System.IO.File.Delete(fileName);
                    //}
                }

                if (inserts != null && inserts.Count > 0)
                {
                    if (this.AfterInsert != null)
                        this.AfterInsert(this, new DatabaseChangeArgs { Changes = inserts });
                    foreach (object entity in inserts)
                    {
                        if (entity.GetType() == typeof(TankFilling))
                        {
                            if (this.DataForSendReady != null)
                                this.DataForSendReady(this, new SendDataEventArgs(entity));
                        }
                        else if (entity.GetType() == typeof(SalesTransaction))
                        {
                            if (this.DataForSendReady != null)
                                this.DataForSendReady(this, new SendDataEventArgs(entity));
                        }
                        else if (entity.GetType() == typeof(FuelTypePrice))
                        {
                            if (this.DataForSendReady != null)
                                this.DataForSendReady(this, new SendDataEventArgs(entity));
                        }
                    }
                }
                if (updates != null && updates.Count > 0)
                {
                    if (this.AfterUpdate != null)
                        this.AfterUpdate(this, new DatabaseChangeArgs { Changes = updates });
                }
                if (deletes != null && deletes.Count > 0)
                {
                    if (this.AfterDelete != null)
                        this.AfterDelete(this, new DatabaseChangeArgs { Changes = deletes });
                }
                this.DatabaseChanged = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.Message);
                Logging.Logger.Instance.LogToFile("Database Save", ex);
            }
            finally
            {
                //if(!scope.Transaction.IsManaged)
                //    scope.Transaction.Commit();
                isOnSave = false;
            }
        }

        protected override void Init(string connectionString, Telerik.OpenAccess.BackendConfiguration backendConfiguration, Telerik.OpenAccess.Metadata.MetadataContainer metadataContainer)
        {
            this.tablesToLog.Add("ApplicationUser");
            this.tablesToLog.Add("Balance");
            this.tablesToLog.Add("CommunicationController");
            this.tablesToLog.Add("Dispenser");
            this.tablesToLog.Add("DispenserSetting");
            this.tablesToLog.Add("FuelType");
            this.tablesToLog.Add("Invoice");
            this.tablesToLog.Add("InvoiceLine");
            this.tablesToLog.Add("InvoiceType");
            this.tablesToLog.Add("Nozzle");
            this.tablesToLog.Add("NozzleFlow");
            this.tablesToLog.Add("Option");
            this.tablesToLog.Add("SendLog");
            this.tablesToLog.Add("SystemEvent");
            this.tablesToLog.Add("SystemEventData");
            this.tablesToLog.Add("Tank");
            //this.tablesToLog.Add("TankSetting");
            this.tablesToLog.Add("Titrimetry");
            this.tablesToLog.Add("TitrimetryLevel");

            base.Init(connectionString, backendConfiguration, metadataContainer);
            IObjectScope scope = base.GetScope();
            base.Events.Added += new AddEventHandler(Events_Added);
            base.Events.Adding += new AddEventHandler(Events_Adding);
            base.Events.Removing += new RemoveEventHandler(Events_Removing);
            base.Events.Changing += new ChangeEventHandler(Events_Changing);
            base.Events.Changed += new ChangeEventHandler(Events_Changed);
            base.Events.Removed += new RemoveEventHandler(Events_Removed);
            base.Events.ObjectConstructed += new ObjectConstructedEventHandler(Events_ObjectConstructed);
            this.ContextOptions.RefreshObjectsAfterSaveChanges = true;
            this.ContextOptions.MaintainOriginalValues = true;

            Validators.InvoiceLineValidator invLineValidator = new Validators.InvoiceLineValidator();
            invLineValidator.Database = this;
            this.validatorContainer.AddValidator<Data.InvoiceLine>(invLineValidator);

            Validators.InvoiceValidator invoiceValidator = new Validators.InvoiceValidator();
            invoiceValidator.Database = this;
            this.validatorContainer.AddValidator<Data.Invoice>(invoiceValidator);

            if (currentUserId == Guid.Empty)
            {
                currentUserId = this.ApplicationUsers.Where(u => u.UserLevel == 0).FirstOrDefault().ApplicationUserId;
            }

            //this.CheckUsagePeriod();
        }

        public UsagePeriod GetUsagePeriod()
        {
            UsagePeriod period = this.UsagePeriods.Where(up => up.PeriodStart <= DateTime.Now && (!up.PeriodEnd.HasValue || up.PeriodEnd.Value >= DateTime.Now)).FirstOrDefault();
            if (period == null)
            {
                List<NameValue> values = new List<NameValue>();
                values.Add(new NameValue("PeriodStart", DateTime.Now));
                values.Add(new NameValue("IsLocked", false));
                period = this.CreateEntity<UsagePeriod>(values);
                //this.SaveChanges();
            }
            return period;
        }

        public UsagePeriod GetUsagePeriod(DateTime dt)
        {
            UsagePeriod period = this.UsagePeriods.Where(up => up.PeriodStart <= dt && (!up.PeriodEnd.HasValue || up.PeriodEnd.Value >= dt)).FirstOrDefault();
            if (period == null)
            {
                List<NameValue> values = new List<NameValue>();
                values.Add(new NameValue("PeriodStart", DateTime.Now));
                values.Add(new NameValue("IsLocked", false));
                period = this.CreateEntity<UsagePeriod>(values);
                this.SaveChanges();
            }
            return period;
        }

        public UsagePeriod GetCreateUsagePeriod(DateTime dt)
        {
            UsagePeriod period = this.UsagePeriods.Where(up => up.PeriodStart <= DateTime.Now && (!up.PeriodEnd.HasValue || up.PeriodEnd.Value >= DateTime.Now)).FirstOrDefault();
            if (period == null)
            {
                List<NameValue> values = new List<NameValue>();
                values.Add(new NameValue("PeriodStart", dt));
                values.Add(new NameValue("IsLocked", false));
                period = this.CreateEntity<UsagePeriod>(values);
                //this.SaveChanges();
            }
            return period;
        }

        public void RefreshData()
        {
            this.Refresh(RefreshMode.PreserveChanges, this.Tanks);
            this.Refresh(RefreshMode.PreserveChanges, this.Dispensers);
            this.Refresh(RefreshMode.PreserveChanges, this.Nozzles);
            this.Refresh(RefreshMode.PreserveChanges, this.SalesTransactions);
            this.Refresh(RefreshMode.PreserveChanges, this.TankFillings);
            this.Refresh(RefreshMode.PreserveChanges, this.TankSales);
            this.Refresh(RefreshMode.PreserveChanges, this.Invoices);
            this.Refresh(RefreshMode.PreserveChanges, this.InvoiceLines);
        }

        #endregion

        #region Private Methods

        private void SetKey(object entity, ObjectKey key)
        {
            foreach (ObjectKeyMember keyMember in key.ObjectKeyValues)
            {
                entity.GetType().GetProperty(keyMember.Key).SetValue(entity, keyMember.Value, null);
            }
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        public void RemoveLevelAlarm(SystemEvent sysEvent)
        {
            if (sysEvent.Tank == null)
                return;
            sysEvent.Tank.CorrectTankData();
        }

        public void RemoveCounterAlarm(SystemEvent sysEvent)
        {
            if (sysEvent.Nozzle == null)
                return;
            //sysEvent.Nozzle.TotalCounter = 0;

            Data.SystemEventDatum sdata = sysEvent.SystemEventData.Where(sd => sd.PropertyName == "TotalVolumeCounter").FirstOrDefault();
            if (sdata != null)
            {
                decimal tc = decimal.Parse(sdata.Value);
                sysEvent.Nozzle.CorrectTotalizer(tc);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    partial class ApplicationUser
    {
        public string PasswordEnc
        {
            get 
            {
                return AESEncryption.Decrypt(this.PasswordEncrypted, "Exedron@#");
            }
            set 
            {
                this.PasswordEncrypted = AESEncryption.Encrypt(value, "Exedron@#");
                this.Password = " ";
            }
        }
    }

    partial class Balance
    {
        private static decimal GetStartLevel(Data.DatabaseModel database, DateTime dt, Guid tankId)
        {
            Data.TankLevelEndView startLevel = database.TankLevelEndViews.Where(t => t.TansDate <= dt && t.TankId == tankId).OrderBy(t => t.TansDate).LastOrDefault();
            if (startLevel != null)
                return startLevel.Level.Value;
            return 0;
            //Data.TankLevelEndView endLevel = database.TankLevelEndViews.Where(t => t.TansDate <= dt && t.TankId == tankId).OrderBy(t => t.TansDate).LastOrDefault();
            //if (endLevel == null)
            //    return 0;
            //return endLevel.Level.Value;
        }

        private static decimal GetEndLevel(Data.DatabaseModel database, DateTime dt, Guid tankId)
        {
            Data.TankLevelEndView endLevel = database.TankLevelEndViews.Where(t => t.TansDate <= dt && t.TankId == tankId).OrderBy(t => t.TansDate).LastOrDefault();
            if (endLevel == null)
            {
                Data.TankLevelStartView startLevel = database.TankLevelStartViews.Where(t => t.TansDate <= dt && t.TankId == tankId).OrderBy(t => t.TansDate).LastOrDefault();
                if (startLevel != null)
                    return startLevel.Level.Value;
                return 0;
            }
            return endLevel.Level.Value;
        }

        public static Balance CreateBalance(Data.DatabaseModel database, DateTime dt1, DateTime dt2)
        {
            Guid literCheckType = Data.Implementation.OptionHandler.Instance.GetGuidOption("LiterCheckInvoiceType", Guid.Empty);
            Guid deliveryType = Data.Implementation.OptionHandler.Instance.GetGuidOption("DeliveryCheckInvoiceType", Guid.Empty);

            database.RefreshData();
            Communication.BalanceClass balance = new Communication.BalanceClass();
            balance.Date = dt1;
            balance.TimeStart = dt1;
            balance.TimeEnd = dt2;
            //period.PeriodEnd = dt2;
            balance.Reservoirs = new ASFuelControl.Communication.ReservoirsClass();
            balance.Reservoirs.Reservoirs = new List<ASFuelControl.Communication.ReservoirClass>();

            balance.PumpsPerFuel = new ASFuelControl.Communication.FuelTypeBalances();
            balance.PumpsPerFuel.FuelTypes = new List<ASFuelControl.Communication.FuelTypeClass>();

            balance.Movements = new ASFuelControl.Communication.FuelMovementsClass();
            balance.Movements.FuelMovements = new List<ASFuelControl.Communication.FuelMovementClass>();

            balance.Divergences = new ASFuelControl.Communication.FuelTypeDivsClass();
            balance.Divergences.Divergences = new List<ASFuelControl.Communication.FuelTypeDivClass>();

            Dictionary<Data.Tank, decimal> fillingsDelivery = new Dictionary<Data.Tank, decimal>();
            Dictionary<Data.Tank, decimal> fillingsDelivery15 = new Dictionary<Data.Tank, decimal>();
            Dictionary<Data.Tank, decimal> fillingRest = new Dictionary<Data.Tank, decimal>();
            Dictionary<Data.Tank, decimal> fillingRest15 = new Dictionary<Data.Tank, decimal>();
            Dictionary<Data.Tank, decimal> invoicedDeliveries = new Dictionary<Data.Tank, decimal>();
            Dictionary<Data.Tank, decimal> invoicedDeliveries15 = new Dictionary<Data.Tank, decimal>();

            foreach (Data.Tank tank in database.Tanks.OrderBy(t => t.TankNumber))
            {
                ASFuelControl.Communication.ReservoirClass reservoir = new ASFuelControl.Communication.ReservoirClass();

                decimal strartLevel = GetStartLevel(database, dt1, tank.TankId);
                decimal endLevel = GetEndLevel(database, dt2, tank.TankId);

                reservoir.Capacity = tank.TotalVolume;
                reservoir.FuelType = (Communication.Enums.FuelTypeEnum)tank.FuelType.EnumeratorValue;
                reservoir.LevelStart = strartLevel;                            //F_2232_MM
                reservoir.LevelEnd = endLevel;                                  //F_2234_MM
                reservoir.TankId = tank.TankNumber;
                reservoir.TankSerialNumber = tank.TankSerialNumber;
                reservoir.TemperatureStart = tank.GetTempmeratureAtTime(dt1);                 //F_2232_TEMP
                reservoir.TemperatureEnd = tank.GetTempmeratureAtTime(dt2);                     //F_2234_TEMP
                reservoir.VolumeStart = tank.GetTankVolume(reservoir.LevelStart);                           //F_2232_VOL
                reservoir.VolumeEnd = tank.GetTankVolume(reservoir.LevelEnd);                               //F_2234_VOL
                reservoir.VolumeStartNormalized = tank.FuelType.NormalizeVolume(reservoir.VolumeStart, reservoir.TemperatureStart, tank.GetDensityAtTime(dt1));   //F_2233
                reservoir.VolumeEndNormalized = tank.FuelType.NormalizeVolume(reservoir.VolumeEnd, reservoir.TemperatureEnd, tank.GetDensityAtTime(dt2));           //F_2235

                decimal fillingsVol = database.TankFillingInvoiceViews.Where(tfi => tfi.TankId == tank.TankId && tfi.TransactionTimeEnd <= balance.TimeEnd && tfi.TransactionTimeEnd >= balance.TimeStart && tfi.InvoiceTypeId == deliveryType).Sum(tfi => tfi.VolumeReal);
                decimal fillingsVol15 = database.TankFillingInvoiceViews.Where(tfi => tfi.TankId == tank.TankId && tfi.TransactionTimeEnd <= balance.TimeEnd && tfi.TransactionTimeEnd >= balance.TimeStart && tfi.InvoiceTypeId == deliveryType).Sum(tfi => tfi.VolumeRealNormalized);
                decimal fillingsRestVol = database.TankFillingInvoiceViews.Where(tfi => tfi.TankId == tank.TankId && tfi.TransactionTimeEnd <= balance.TimeEnd && tfi.TransactionTimeEnd >= balance.TimeStart && tfi.InvoiceTypeId != deliveryType && (tfi.InvoiceTypeId != literCheckType || tfi.TransactionType == 1)).Sum(tfi => tfi.VolumeReal);
                decimal fillingsRestVol15 = database.TankFillingInvoiceViews.Where(tfi => tfi.TankId == tank.TankId && tfi.TransactionTimeEnd <= balance.TimeEnd && tfi.TransactionTimeEnd >= balance.TimeStart && tfi.InvoiceTypeId != deliveryType && (tfi.InvoiceTypeId != literCheckType || tfi.TransactionType == 1)).Sum(tfi => tfi.VolumeRealNormalized);
                decimal invoicedVol = database.TankFillingInvoiceViews.Where(tfi => tfi.TankId == tank.TankId && tfi.TransactionTimeEnd <= balance.TimeEnd && tfi.TransactionTimeEnd >= balance.TimeStart && tfi.InvoiceTypeId == deliveryType).Sum(tfi => tfi.InvoiceVolume);
                decimal invoicedVol15 = database.TankFillingInvoiceViews.Where(tfi => tfi.TankId == tank.TankId && tfi.TransactionTimeEnd <= balance.TimeEnd && tfi.TransactionTimeEnd >= balance.TimeStart && tfi.InvoiceTypeId == deliveryType).Sum(tfi => tfi.InvoiceVolumeNormalized);

                fillingsDelivery.Add(tank, fillingsVol);
                fillingsDelivery15.Add(tank, fillingsVol15);
                fillingRest.Add(tank, fillingsRestVol);
                fillingRest15.Add(tank, fillingsRestVol15);
                invoicedDeliveries.Add(tank, invoicedVol);
                invoicedDeliveries15.Add(tank, invoicedVol15);

                balance.Reservoirs.Reservoirs.Add(reservoir);
            }

            if (balance.Reservoirs.Reservoirs.Count == 0)
                return null;

            List<Data.FuelType> fuelTypes = database.Tanks.Select(t => t.FuelType).Distinct().ToList();
            foreach (Data.FuelType ft in fuelTypes)
            {
                List<Data.Nozzle> nozzles = database.Tanks.Where(t => t.FuelTypeId == ft.FuelTypeId).SelectMany(t => t.NozzleFlows).Select(nf => nf.Nozzle).Distinct().ToList();
                if (nozzles.Count == 0)
                    continue;
                ASFuelControl.Communication.FuelTypeClass ftc = new ASFuelControl.Communication.FuelTypeClass();
                ftc.FuelPumps = new List<ASFuelControl.Communication.FuelTypePumpClass>();
                ftc.FuelType = (Communication.Enums.FuelTypeEnum)ft.EnumeratorValue;
                var qst = ft.Nozzles.SelectMany(n => n.SalesTransactions).Where(st => st.TransactionTimeStamp <= balance.TimeEnd && st.TransactionTimeStamp >= balance.TimeStart);
                foreach (Data.Nozzle n in nozzles)
                {
                    var qs = n.SalesTransactions.Where(s => s.TransactionTimeStamp <= balance.TimeEnd && s.TransactionTimeStamp >= balance.TimeStart && s.InvoiceLines.Count > 0 && s.TankSales.Count > 0);
                    if (qs.Count() == 0)
                        continue;

                    if (qs.SelectMany(s => s.TankSales).Count() == 0)
                        continue;

                    ASFuelControl.Communication.FuelTypePumpClass ftpc = new ASFuelControl.Communication.FuelTypePumpClass();
                    ftpc.FuelPumpId = n.Dispenser.OfficialPumpNumber.ToString();
                    ftpc.FuelPumpSerialNumber = n.Dispenser.PumpSerialNumber;
                    ftpc.FuelType = (Communication.Enums.FuelTypeEnum)ft.EnumeratorValue;
                    ftpc.TotalizerStart = n.GetTotalizerStartAtTime(balance.TimeStart);             //F_2242
                    ftpc.TotalizerEnd = n.GetTotalizerEndAtTime(balance.TimeEnd);                   //F_2243

                    ftpc.TotalLiterCheck = n.GetLitercheckSum(balance.TimeStart, balance.TimeEnd);  //F_2244D
                    ftpc.TotalLiterCheckNormalized = n.GetLitercheckSum(balance.TimeStart, balance.TimeEnd);  //F_2244D
                    ftpc.TotalSales = qs.Sum(s => s.Volume) - ftpc.TotalLiterCheck;                 //F_2244A
                    ftpc.TotalSalesNormalized = qs.Sum(s => s.VolumeNormalized) - ftpc.TotalLiterCheckNormalized;
                    //ftpc.TotalOut = qs.Sum(s => s.Volume) + ftpc.TotalLiterCheck;                 //F_2244B
                    //ftpc.TotalOutNormalized = qs.Sum(s => s.VolumeNormalized);                    //F_2244C

                    ftpc.TotalizerDifference = ftpc.TotalizerEnd - ftpc.TotalizerStart;             //F_2245A
                    ftpc.NozzleId = n.OfficialNozzleNumber;

                    var qd = n.Dispenser.Nozzles.Where(nn => nn.FuelTypeId == n.FuelTypeId);
                    var qsd = qd.SelectMany(nn => nn.SalesTransactions).Where(s => s.TransactionTimeStamp <= balance.TimeEnd && s.TransactionTimeStamp >= balance.TimeStart);

                    //ftpc.SumTotalOut = qs.Sum(s => s.Volume);// qsd.Sum(s => s.Volume);                                                      //F_2244E
                    //ftpc.SumTotalOutNormalized = qs.Sum(s => s.VolumeNormalized); // qsd.Sum(s => s.VolumeNormalized);                                  //F_2244F
                    //ftpc.SumTotalOutTotalizer =  ftpc.TotalizerEnd - ftpc.TotalizerStart;                            //F_2245A   qs.Sum(s => s.TotalizerEnd - s.TotalizerStart);
                    //ftpc.SumTotalOutTotalizerNormalized = qsd.Sum(s => s.TotalizerEnd - s.TotalizerStart);          //F_2245B

                    // ********* ftpc.SumTotalOutTotalizerNormalized *************//
                    var qt = n.SalesTransactions.SelectMany(s => s.TankSales).Select(ts => ts.Tank).Distinct();
                    Data.Tank tank = qt.First();
                    ftpc.TankSerialNumber = tank.TankSerialNumber;
                    ftc.FuelPumps.Add(ftpc);
                }
                var qsdiff = ft.Nozzles.SelectMany(n => n.SalesTransactions).Where(s => s.TransactionTimeStamp <= balance.TimeEnd && s.TransactionTimeStamp >= balance.TimeStart);
                ftc.SumTotalizerDifference = ftc.FuelPumps.Sum(f => f.TotalizerEnd - f.TotalizerStart);                                             //F_2245B
                ftc.SumTotalizerDifferenceNormalized = ft.NormalizeVolume(ftc.SumTotalizerDifference, database.GetAvgTemperature(qsdiff), database.GetAvgDensity(qsdiff, ft));//F_2245C
                ftc.TotalPumpsNumber = ftc.FuelPumps.Count;
                foreach (ASFuelControl.Communication.FuelTypePumpClass ftpc in ftc.FuelPumps)
                {
                    Data.FuelType fuelType = fuelTypes.Where(f => f.EnumeratorValue == (int)ftpc.FuelType).FirstOrDefault();
                    if (fuelType == null)
                        continue;
                    ftpc.TotalOut = ftc.FuelPumps.Where(fp => fp.FuelType == ftc.FuelType).Sum(fp => fp.TotalSales + fp.TotalLiterCheck);                                   //F_2244B 
                    ftpc.TotalOutNormalized = ftc.FuelPumps.Where(fp => fp.FuelType == ftc.FuelType).Sum(fp => fp.TotalSalesNormalized + fp.TotalLiterCheckNormalized);     //F_2244C
                    ftpc.SumTotalOut = ftc.FuelPumps.Where(fp => fp.FuelType == ftc.FuelType).Sum(fp => fp.TotalLiterCheck);                                                //F_2244E 
                    ftpc.SumTotalOutNormalized = ftc.FuelPumps.Where(fp => fp.FuelType == ftc.FuelType).Sum(fp => fp.TotalLiterCheckNormalized);                            //F_2244F
                }
                balance.PumpsPerFuel.FuelTypes.Add(ftc);
            }
            foreach (Data.FuelType ft in fuelTypes)
            {
                ASFuelControl.Communication.FuelMovementClass mov = new ASFuelControl.Communication.FuelMovementClass();
                mov.FuelType = (Communication.Enums.FuelTypeEnum)ft.EnumeratorValue;

                //var qFill = database.TankFillings.Where(tf => tf.TransactionTimeEnd >= balance.TimeStart && tf.TransactionTime <= balance.TimeEnd && tf.Tank.FuelTypeId == ft.FuelTypeId).ToList();
                //var qInvFillings = database.InvoiceLines.Where(il => il.FuelTypeId == ft.FuelTypeId && il.Invoice.InvoiceTypeId == deliveryType && il.TankFilling.TransactionTime <= balance.TimeEnd && il.TankFilling.TransactionTime >= balance.TimeStart && !il.TankFillingId.HasValue);
                //var qInvOther = qFill.SelectMany(tf => tf.InvoiceLines).Where(il => il.FuelTypeId == ft.FuelTypeId && il.Invoice.InvoiceTypeId == literCheckType || (il.Invoice.InvoiceTypeId != deliveryType && il.Invoice.InvoiceType.TransactionType == 1) && il.TankFilling.TransactionTime <= balance.TimeEnd && il.TankFilling.TransactionTime >= balance.TimeStart);

                mov.SumIn = fillingsDelivery.Where(t => t.Key.FuelTypeId == ft.FuelTypeId).Sum(t => t.Value);                           //F_2236A1
                mov.SumAdditionalIn = fillingRest.Where(t => t.Key.FuelTypeId == ft.FuelTypeId).Sum(t => t.Value);                      //F_2236A2
                mov.SumInNormalized = fillingsDelivery15.Where(t => t.Key.FuelTypeId == ft.FuelTypeId).Sum(t => t.Value);               //F_2236B1
                mov.SumAdditionalInNormalized = fillingRest15.Where(t => t.Key.FuelTypeId == ft.FuelTypeId).Sum(t => t.Value);            //F_2236B2
                mov.SumInInvoiced = invoicedDeliveries.Where(t => t.Key.FuelTypeId == ft.FuelTypeId).Sum(t => t.Value);                 //F_2237
                mov.SumInInvoicedNormalized = invoicedDeliveries15.Where(t => t.Key.FuelTypeId == ft.FuelTypeId).Sum(t => t.Value);     //F_2238
                mov.Diff = mov.SumIn - mov.SumInInvoiced;                                                                               //F_2239A
                mov.DiffNormalized = mov.SumInNormalized - mov.SumInInvoicedNormalized;                                                 //F_2239B

                mov.DaylyMove = balance.Reservoirs.Reservoirs.Where(t => t.FuelType == mov.FuelType).Sum(t => t.VolumeStart - t.VolumeEnd) + mov.SumIn + mov.SumAdditionalIn; //F_22310 
                mov.DaylyMoveNormalized = balance.Reservoirs.Reservoirs.Where(t => t.FuelType == mov.FuelType).Sum(t => t.VolumeStartNormalized - t.VolumeEndNormalized) + mov.SumInNormalized + mov.SumAdditionalInNormalized;

                balance.Movements.FuelMovements.Add(mov);

                ASFuelControl.Communication.FuelTypeDivClass div = new ASFuelControl.Communication.FuelTypeDivClass();
                div.FuelType = (Communication.Enums.FuelTypeEnum)ft.EnumeratorValue;
                div.Divergence = mov.DaylyMove - balance.PumpsPerFuel.FuelTypes.Where(f => f.FuelType == mov.FuelType).SelectMany(f => f.FuelPumps).Sum(fp => fp.TotalSales);
                div.DivergenceNormalized = mov.DaylyMoveNormalized - balance.PumpsPerFuel.FuelTypes.Where(f => f.FuelType == mov.FuelType).SelectMany(f => f.FuelPumps).Sum(fp => fp.TotalSalesNormalized);
                if (mov.DaylyMove != 0)
                    div.Percentage = 100 * (div.Divergence / mov.DaylyMove);
                else
                    div.Percentage = 0;

                if (mov.DaylyMoveNormalized != 0)
                    div.PercentageNormalized = 100 * (div.DivergenceNormalized / mov.DaylyMoveNormalized);
                else
                    div.PercentageNormalized = 0;

                balance.Divergences.Divergences.Add(div);
            }
            System.Xml.Serialization.XmlSerializer ser = new System.Xml.Serialization.XmlSerializer(typeof(Communication.BalanceClass));
            System.IO.StringWriter textWriter = new System.IO.StringWriter();
            ser.Serialize(textWriter, balance);
            string data = textWriter.ToString();
            textWriter.Close();
            textWriter.Dispose();
            Data.Balance bal = new Data.Balance();// this.database.CreateEntity<Data.Balance>();
            bal.BalanceId = Guid.NewGuid();
            database.Add(bal);
            bal.BalanceText = data;
            bal.StartDate = balance.TimeStart;
            bal.EndDate = balance.TimeEnd;
            bal.ApplicationUserId = Data.DatabaseModel.CurrentUserId;
            Data.TankFilling lastFil = database.TankFillings.Where(f => f.TransactionTime <= balance.TimeEnd).OrderBy(f => f.TransactionTimeEnd).LastOrDefault();
            if (lastFil != null)
                bal.LastFilling = lastFil.TankFillingId;
            return bal;
        }
    }

    partial class CommunicationController
    {
        public IList<Dispenser> OrderedDispensers
        {
            get { return this.Dispensers.ToList().OrderBy(d => d.Description).ToList(); }
        }

        public IList<Tank> OrderedTanks
        {
            get { return this.Tanks.ToList().OrderBy(d => d.TankNumber).ToList(); }
        }

        public string EuromatIpAddress
        {
            get 
            {
                if (this.EuromatIp == null || this.EuromatIp == "")
                    return "127.0.0.1";
                else
                {
                    string ip = this.EuromatIp.Replace(",", ".").Replace(" ", "");
                    return ip;
                }
            }
        }

        public string DeviceSerialNumber
        {
            get
            {
                return DeviceSetting.GetSetting("DeviceSerialNumber", this);
            }
            set
            {
                DeviceSetting.SetSetting("DeviceSerialNumber", value, this, "Σειραικός Αριθμός Κατασκευαστή", true);
            }
        }
    }

    partial class Dispenser
    {
        private ASFuelControl.Common.FuelPoint fp = null;

        public string Description 
        { 
            get 
            { 
                return "Αντλία : " + this.OfficialPumpNumber.ToString(); 
            } 
        }

        public string DescriptionExt
        {
            get
            {
                return "Αντλία : " + this.OfficialPumpNumber.ToString() + " (" + this.DispenserNumber.ToString() + ")";
            }
        }

        public string DeviceSerialNumber
        {
            get
            {
                return DeviceSetting.GetSetting("DeviceSerialNumber", this);
                //DispenserSetting setting = this.DispenserSettings.Where(ts => ts.SettingKey == "DeviceSerialNumber").FirstOrDefault();
                //if (setting == null)
                //    return "";
                //return setting.SettingValue;
            }
            set
            {
                DeviceSetting.SetSetting("DeviceSerialNumber", value, this, "Σειραικός Αριθμός Κατασκευαστή", true);
                //DispenserSetting setting = this.DispenserSettings.Where(ts => ts.SettingKey == "DeviceSerialNumber").FirstOrDefault();
                //if (setting == null)
                //{
                //    DatabaseModel db = DatabaseModel.GetContext(this) as DatabaseModel;

                //    setting = new DispenserSetting();
                //    setting.DispenserSettingId = Guid.NewGuid();
                //    db.Add(setting);
                //    setting.DispenserId = this.DispenserId;
                //    setting.SettingKey = "DeviceSerialNumber";
                //    this.DispenserSettings.Add(setting);
                //}
                //setting.SettingValue = value;
            }
        }

        public string DeviceSeal
        {
            get
            {
                return DeviceSetting.GetSetting("DeviceSeal", this);
            }
            set
            {
                DeviceSetting.SetSetting("DeviceSeal", value, this, "Σφραγγίδα Ηλεκτρονικών", false);
            }
        }

        public string DeviceType
        {
            get
            {
                return DeviceSetting.GetSetting("DeviceType", this);
            }
            set
            {
                DeviceSetting.SetSetting("DeviceType", value, this, "Τύπος Αντλίας", false);
            }
        }

        public IList<Nozzle> OrderedNozzles
        {
            get 
            {
                return this.Nozzles.OrderBy(n => n.OfficialNozzleNumber).ToList();
            }
        }

        public ASFuelControl.Common.FuelPoint CommonFuelPoint
        {
            get
            {
                if (fp == null)
                {
                    fp = new Common.FuelPoint();
                    fp.Address = this.PhysicalAddress;
                    fp.Channel = this.Channel;
                    fp.DispencerProtocol = this.DispenserType.DispenserProtocol.EnumeratorValue;
                    fp.NozzleCount = this.Nozzles.Count;
                    fp.ActiveNozzleIndex = -1;
                }
                return fp;
            }
        }

        public Invoice CreateSale(Common.Sales.SaleData currentSale)
        {
            try
            {
                decimal vatValue = Implementation.OptionHandler.Instance.GetDecimalOption("VATValue", (decimal)23);
                //if (nSale.TotalVolumeStart == nSale.TotalVolumeEnd)
                //    return null;
                //int nozzleId = nSale.NozzleId - 1;
                Nozzle lastNozzle = this.Nozzles.Where(n => n.NozzleId == currentSale.NozzleId).FirstOrDefault();

                DatabaseModel db = DatabaseModel.GetContext(this) as DatabaseModel;
                SalesTransaction sale = new SalesTransaction();
                sale.SalesTransactionId = Guid.NewGuid();


                sale.ApplicationUserId = DatabaseModel.CurrentUserId;
                if (currentSale.ErrorResolving)
                    sale.IsErrorResolving = true;
                sale.NozzleId = lastNozzle.NozzleId;
                sale.Nozzle = lastNozzle;
                sale.TransactionTimeStamp = DateTime.Now;
                sale.DiscountPercentage = lastNozzle.DiscountPercentage;
                lastNozzle.DiscountPercentage = 0;
                sale.TotalizerStart = currentSale.TotalizerStart;// lastSaleValues.TotalVolumes[lastNozzle.OrderId];// nSale.TotalVolumeStart;
                sale.TotalizerEnd = currentSale.TotalizerEnd;//nSale.TotalVolumeEnd;
                //if (currentSale.TotalizerEnd - currentSale.TotalizerStart != currentSale.TotalVolume)
                //{
                //    currentSale.TotalVolume = currentSale.TotalizerEnd - currentSale.TotalizerStart;
                //    sale.TotalPrice = decimal.Round(currentSale.TotalVolume * currentSale.UnitPrice, 2);
                //}
                
                sale.Volume = currentSale.TotalVolume;//nSale.TotalVolume;
                if (Data.DatabaseModel.CurrentShiftId != Guid.Empty)
                    sale.ShiftId = Data.DatabaseModel.CurrentShiftId;
                sale.UnitPrice = currentSale.UnitPrice;

                db.Add(sale);

                //nSale.TotalPrice;
                decimal totalVol = 0;
                decimal densityAvg = 0;
                List<decimal> densities = new List<decimal>();

                foreach (Common.Sales.TankSaleData tankData in currentSale.TankData)
                {
                    Data.Tank tank = db.Tanks.Where(t => t.TankId == tankData.TankId).FirstOrDefault();
                    TankSale tSale = db.CreateEntity<TankSale>();
                    decimal temperature = tankData.StartTemperature;// nSale.SaleTankStartValues[nf.TankId].CurrentTemperatur;

                    tSale.TankId = tankData.TankId;
                    sale.TankSales.Add(tSale);
                    tSale.SalesTransactionId = sale.SalesTransactionId;
                    tSale.SalesTransaction = sale;
                    tSale.StartTemperature = tankData.StartTemperature;//nSale.SaleTankStartValues[nf.TankId].CurrentTemperatur;
                    tSale.EndTemperature = tankData.EndTemperature;//nSale.SaleTankEndValues[nf.TankId].CurrentTemperatur;

                    tSale.StartLevel = tankData.StartLevel;// nSale.SaleTankStartValues[nf.TankId].FuelHeight;
                    tSale.EndLevel = tankData.EndLevel;// nSale.SaleTankEndValues[nf.TankId].FuelHeight;

                    tSale.StartVolume = tank.GetTankVolume(tSale.StartLevel);
                    tSale.EndVolume = tank.GetTankVolume(tSale.EndLevel.Value);

                    tSale.StartVolumeNormalized = lastNozzle.FuelType.NormalizeVolume(tSale.StartVolume, tSale.StartTemperature.Value, tank.CurrentDensity);
                    tSale.EndVolumeNormalized = lastNozzle.FuelType.NormalizeVolume(tSale.EndVolume.Value, tSale.EndTemperature, tank.CurrentDensity);
                    tSale.FuelDensity = tank.CurrentDensity;
                    decimal diff = tSale.EndVolumeNormalized.Value - tSale.StartVolumeNormalized;
                    densityAvg = tank.CurrentDensity * diff;
                    totalVol = totalVol + diff;
                    densities.Add(tank.CurrentDensity);
                }

                decimal startTempAvg = 0;
                decimal endTempAvg = 0;
                if (totalVol != 0)
                {
                    foreach (TankSale tSale in sale.TankSales)
                    {
                        decimal vol = tSale.EndVolumeNormalized.Value - tSale.StartVolumeNormalized;
                        startTempAvg = startTempAvg + tSale.StartTemperature.Value * (vol / totalVol);
                        endTempAvg = endTempAvg + tSale.EndTemperature * (vol / totalVol);
                    }
                }
                else
                {
                    if (sale.TankSales.Count > 0)
                    {
                        foreach (TankSale tSale in sale.TankSales)
                        {
                            startTempAvg = startTempAvg + tSale.StartTemperature.Value;
                            endTempAvg = endTempAvg + tSale.EndTemperature;
                        }
                        startTempAvg = startTempAvg / sale.TankSales.Count;
                        endTempAvg = endTempAvg / sale.TankSales.Count;
                    }

                }
                sale.TemperatureStart = startTempAvg;
                sale.TemperatureEnd = endTempAvg;
                try
                {
                    if (totalVol == 0)
                    {
                        if (densities.Count > 0)
                        {
                            densityAvg = densities.Sum() / densities.Count;
                            sale.VolumeNormalized = lastNozzle.FuelType.NormalizeVolume(sale.Volume, sale.TemperatureEnd, densityAvg);
                        }
                        else
                            sale.VolumeNormalized = lastNozzle.FuelType.NormalizeVolume(sale.Volume, sale.TemperatureEnd, lastNozzle.FuelType.BaseDensity);

                    }
                    else
                        sale.VolumeNormalized = lastNozzle.FuelType.NormalizeVolume(sale.Volume, sale.TemperatureEnd, densityAvg / totalVol);
                }
                catch
                {
                    sale.VolumeNormalized = lastNozzle.FuelType.NormalizeVolume(sale.Volume, sale.TemperatureEnd, lastNozzle.FuelType.BaseDensity);
                }

                if ((sale.Volume > sale.VolumeNormalized && (sale.VolumeNormalized == 0 || sale.Volume / sale.VolumeNormalized > 2)) || 
                    (sale.VolumeNormalized > sale.Volume && (sale.Volume == 0 || sale.VolumeNormalized / sale.Volume > 2)))
                {
                    try
                    {
                        Guid[] tankIds = currentSale.TankData.Select(t => t.TankId).ToArray();
                        decimal dencityAvg = db.Tanks.Where(t => tankIds.Contains(t.TankId)).ToList().Select(t => t.CurrentDensity).Average();
                        sale.VolumeNormalized = lastNozzle.FuelType.NormalizeVolume(sale.Volume, sale.TemperatureEnd, dencityAvg);
                    }
                    catch
                    {
                        sale.VolumeNormalized = lastNozzle.FuelType.NormalizeVolume(sale.Volume, sale.TemperatureEnd, lastNozzle.FuelType.BaseDensity);
                    }
                }

                //sale.NozzleId = lastNozzle.NozzleId;// this.Nozzles.Where(n => n.OrderId == values.ActiveNozzle).FirstOrDefault().NozzleId;
                ////this.LastNozzleUsed = null;
                //sale.SalesPrice = lastSaleValues.CurrentSalePrice;
                UsagePeriod up = db.GetUsagePeriod();
                sale.UsagePeriodId = up.UsagePeriodId;
                sale.UsagePeriod = up;

                sale.TotalPrice = currentSale.TotalPrice;
                sale.CRC = sale.CalculateCRC32();


                InvoicePrint ip = sale.Nozzle.Dispenser.InvoicePrints.FirstOrDefault();
                if (ip == null)
                    return null;

                Invoice invoice = new Invoice();
                invoice.ApplicationUserId = DatabaseModel.CurrentUserId;
                invoice.InvoiceId = Guid.NewGuid();
                db.Add(invoice);
                invoice.TransactionDate = DateTime.Now;
                if (currentSale.LiterCheck)// lastNozzle.NozzleState == (int)Common.Enumerators.NozzleStateEnum.LiterCheck)
                {
                    Guid invoiceType = Implementation.OptionHandler.Instance.GetGuidOption("LiterCheckInvoiceType", Guid.Empty);
                    InvoiceType invType = db.InvoiceTypes.Where(it => it.InvoiceTypeId == invoiceType).FirstOrDefault();
                    invoice.InvoiceTypeId = invoiceType;
                    invoice.InvoiceType = invType;
                    invoice.Number = invType.LastNumber + 1;
                    invType.LastNumber = invoice.Number;
                    lastNozzle.NozzleState = (int)Common.Enumerators.NozzleStateEnum.Normal;
                    Console.WriteLine("LITER CHECK CREATED");
                }
                else //else if (lastNozzle.NozzleState == (int)Common.Enumerators.NozzleStateEnum.Normal)
                {
                    Guid invoiceType = currentSale.InvoiceTypeId;
                    if(invoiceType == Guid.Empty)
                        invoiceType = ip.DefaultInvoiceType;
                    InvoiceType invType = db.InvoiceTypes.Where(it => it.InvoiceTypeId == invoiceType).FirstOrDefault();

                    invoice.InvoiceTypeId = invoiceType;
                    invoice.InvoiceType = invType;
                    invoice.Number = invType.LastNumber + 1;
                    invType.LastNumber = invoice.Number;
                    if (currentSale.VehicleId != Guid.Empty)
                    {
                        invoice.VehicleId = currentSale.VehicleId;
                        invoice.Vehicle = db.Vehicles.Where(v => v.VehicleId == currentSale.VehicleId).FirstOrDefault();
                        invoice.TraderId = invoice.Vehicle.TraderId;
                        invoice.Trader = invoice.Vehicle.Trader;
                        invoice.VehiclePlateNumber = invoice.Vehicle.PlateNumber;
                        //if (invoice.Trader.InvoiceTypeId.HasValue && invoice.Trader.InvoiceTypeId.Value != invoice.InvoiceTypeId)
                        //{
                        //    invoice.InvoiceTypeId = invoice.Trader.InvoiceTypeId.Value;
                        //    invType = db.InvoiceTypes.Where(it => it.InvoiceTypeId == invoice.InvoiceTypeId).FirstOrDefault();
                        //    invoice.Number = invType.LastNumber + 1;
                        //}
                    }
                }
                invoice.InvoiceFormId = invoice.InvoiceType.InvoiceFormId;
                if (lastNozzle.Dispenser.InvoicePrints.Count > 0)
                    invoice.Printer = lastNozzle.Dispenser.InvoicePrints.First().Printer;
                if (invoice.Printer == null || invoice.Printer == "" || (invoice.InvoiceType != null && invoice.InvoiceType.IsLaserPrint.HasValue && invoice.InvoiceType.IsLaserPrint.Value))
                    invoice.Printer = invoice.InvoiceType.Printer;

                decimal vatMult = 1 + vatValue / 100;
                if (invoice.Trader != null && invoice.Trader.VatExemption.HasValue && invoice.Trader.VatExemption.Value)
                {
                    decimal vatAmount = currentSale.TotalPrice - decimal.Round(currentSale.TotalPrice / vatMult, 2);
                    invoice.TotalAmount = currentSale.TotalPrice - vatAmount;
                    invoice.VatAmount = 0;
                    invoice.NettoAmount = invoice.TotalAmount;
                    vatValue = 0;
                }
                else
                {
                    invoice.TotalAmount = currentSale.TotalPrice;
                    invoice.VatAmount = currentSale.TotalPrice - decimal.Round(invoice.TotalAmount.Value / vatMult, 2);
                    invoice.NettoAmount = currentSale.TotalPrice - invoice.VatAmount;
                }
                invoice.DiscountAmount = 0;


                InvoiceLine invLine = new InvoiceLine();
                invLine.InvoiceLineId = Guid.NewGuid();
                db.Add(invLine);
                invLine.InvoiceId = invoice.InvoiceId;
                invLine.Invoice = invoice;
                invoice.InvoiceLines.Add(invLine);
                invLine.SaleTransactionId = sale.SalesTransactionId;
                invLine.SalesTransaction = sale;
                invLine.TotalPrice = invoice.TotalAmount.Value;// currentSale.TotalPrice;
                invLine.FuelTypeId = lastNozzle.FuelTypeId;
                invLine.FuelType = lastNozzle.FuelType;
                invLine.UnitPrice = sale.UnitPrice;
                invLine.Volume = sale.Volume;
                invLine.VolumeNormalized = sale.VolumeNormalized;
                invLine.VatAmount = invoice.VatAmount.Value;// currentSale.TotalPrice - decimal.Round(invLine.TotalPrice / vatMult, 2);
                invLine.VatPercentage = vatValue;
                invLine.DiscountAmount = 0;
                sale.InvoiceLines.Add(invLine);
                if (currentSale.LiterCheck)
                {
                    ASFuelControl.Common.Sales.TankSaleData td = currentSale.TankData.OrderByDescending(t => t.EndLevel - t.StartLevel).FirstOrDefault();
                    if (td != null)
                        invLine.TankId = td.TankId;
                }
                invoice.InvoiceLines.Add(invLine);

                if (sale.DiscountPercentage.HasValue && sale.DiscountPercentage.Value > 0)
                {
                    decimal discount = sale.TotalPrice * sale.DiscountPercentage.Value / 100;
                    sale.TotalPrice = sale.TotalPrice - discount;
                    invoice.DiscountAmount = discount;
                    invLine.DiscountAmount = discount;
                    invoice.TotalAmount = invoice.TotalAmount - discount;
                    invLine.TotalPrice = invLine.TotalPrice - discount;
                    invoice.VatAmount = invoice.TotalAmount.Value - decimal.Round(invoice.TotalAmount.Value / vatMult, 2);
                    invLine.VatAmount = invoice.VatAmount.Value;
                }

                return invoice;
            }
            catch(Exception ex)
            {
                Logging.Logger.Instance.LogToFile("Create Sale", ex);
                return null;
            }
        }

        public Guid CreateSalesTransaction(Common.Sales.SaleData currentSale)
        {
            try
            {
                decimal vatValue = Implementation.OptionHandler.Instance.GetDecimalOption("VATValue", (decimal)23);
                Nozzle lastNozzle = this.Nozzles.Where(n => n.NozzleId == currentSale.NozzleId).FirstOrDefault();

                DatabaseModel db = DatabaseModel.GetContext(this) as DatabaseModel;
                SalesTransaction sale = new SalesTransaction();
                sale.SalesTransactionId = Guid.NewGuid();


                sale.ApplicationUserId = DatabaseModel.CurrentUserId;
                if (currentSale.ErrorResolving)
                    sale.IsErrorResolving = true;
                sale.NozzleId = lastNozzle.NozzleId;
                sale.Nozzle = lastNozzle;
                sale.TransactionTimeStamp = DateTime.Now;
                sale.TotalizerStart = currentSale.TotalizerStart;// lastSaleValues.TotalVolumes[lastNozzle.OrderId];// nSale.TotalVolumeStart;
                sale.TotalizerEnd = currentSale.TotalizerEnd;//nSale.TotalVolumeEnd;
                sale.InvalidSale = true;
                sale.DiscountPercentage = lastNozzle.DiscountPercentage;
                lastNozzle.DiscountPercentage = 0;
                sale.Volume = currentSale.TotalVolume;//nSale.TotalVolume;
                if (Data.DatabaseModel.CurrentShiftId != Guid.Empty)
                    sale.ShiftId = Data.DatabaseModel.CurrentShiftId;
                sale.UnitPrice = currentSale.UnitPrice;

                db.Add(sale);

                decimal totalVol = 0;
                decimal densityAvg = 0;
                List<decimal> densities = new List<decimal>();

                foreach (Common.Sales.TankSaleData tankData in currentSale.TankData)
                {
                    Data.Tank tank = db.Tanks.Where(t => t.TankId == tankData.TankId).FirstOrDefault();
                    TankSale tSale = db.CreateEntity<TankSale>();
                    decimal temperature = tankData.StartTemperature;// nSale.SaleTankStartValues[nf.TankId].CurrentTemperatur;

                    tSale.TankId = tankData.TankId;
                    sale.TankSales.Add(tSale);
                    tSale.SalesTransactionId = sale.SalesTransactionId;
                    tSale.SalesTransaction = sale;
                    tSale.StartTemperature = tankData.StartTemperature;//nSale.SaleTankStartValues[nf.TankId].CurrentTemperatur;
                    tSale.EndTemperature = tankData.EndTemperature;//nSale.SaleTankEndValues[nf.TankId].CurrentTemperatur;

                    tSale.StartLevel = tankData.StartLevel;// nSale.SaleTankStartValues[nf.TankId].FuelHeight;
                    tSale.EndLevel = tankData.EndLevel;// nSale.SaleTankEndValues[nf.TankId].FuelHeight;

                    tSale.StartVolume = tank.GetTankVolume(tSale.StartLevel);
                    tSale.EndVolume = tank.GetTankVolume(tSale.EndLevel.Value);

                    tSale.StartVolumeNormalized = lastNozzle.FuelType.NormalizeVolume(tSale.StartVolume, tSale.StartTemperature.Value, tank.CurrentDensity);
                    tSale.EndVolumeNormalized = lastNozzle.FuelType.NormalizeVolume(tSale.EndVolume.Value, tSale.EndTemperature, tank.CurrentDensity);
                    decimal diff = tSale.EndVolumeNormalized.Value - tSale.StartVolumeNormalized;
                    densityAvg = tank.CurrentDensity * diff;
                    totalVol = totalVol + diff;
                    densities.Add(tank.CurrentDensity);
                }

                decimal startTempAvg = 0;
                decimal endTempAvg = 0;
                if (totalVol != 0)
                {
                    foreach (TankSale tSale in sale.TankSales)
                    {
                        decimal vol = tSale.EndVolumeNormalized.Value - tSale.StartVolumeNormalized;
                        startTempAvg = startTempAvg + tSale.StartTemperature.Value * (vol / totalVol);
                        endTempAvg = endTempAvg + tSale.EndTemperature * (vol / totalVol);
                    }
                }
                else
                {
                    if (sale.TankSales.Count > 0)
                    {
                        foreach (TankSale tSale in sale.TankSales)
                        {
                            startTempAvg = startTempAvg + tSale.StartTemperature.Value;
                            endTempAvg = endTempAvg + tSale.EndTemperature;
                        }
                        startTempAvg = startTempAvg / sale.TankSales.Count;
                        endTempAvg = endTempAvg / sale.TankSales.Count;
                    }

                }
                sale.TemperatureStart = startTempAvg;
                sale.TemperatureEnd = endTempAvg;

                //if (totalVol == 0)
                //{
                //    densityAvg = densities.Sum() / densities.Count;
                //    sale.VolumeNormalized = lastNozzle.FuelType.NormalizeVolume(sale.Volume, sale.TemperatureEnd, densityAvg);
                //}
                //else
                //    sale.VolumeNormalized = lastNozzle.FuelType.NormalizeVolume(sale.Volume, sale.TemperatureEnd, densityAvg / totalVol);

                try
                {
                    if (totalVol == 0)
                    {
                        if (densities.Count > 0)
                        {
                            densityAvg = densities.Sum() / densities.Count;
                            sale.VolumeNormalized = lastNozzle.FuelType.NormalizeVolume(sale.Volume, sale.TemperatureEnd, densityAvg);
                        }
                        else
                            sale.VolumeNormalized = lastNozzle.FuelType.NormalizeVolume(sale.Volume, sale.TemperatureEnd, lastNozzle.FuelType.BaseDensity);

                    }
                    else
                        sale.VolumeNormalized = lastNozzle.FuelType.NormalizeVolume(sale.Volume, sale.TemperatureEnd, densityAvg / totalVol);
                }
                catch
                {
                    sale.VolumeNormalized = lastNozzle.FuelType.NormalizeVolume(sale.Volume, sale.TemperatureEnd, lastNozzle.FuelType.BaseDensity);
                }
                UsagePeriod up = db.GetUsagePeriod();
                sale.UsagePeriodId = up.UsagePeriodId;
                sale.UsagePeriod = up;

                sale.TotalPrice = currentSale.TotalPrice;
                sale.CRC = sale.CalculateCRC32();
                return sale.SalesTransactionId;
            }
            catch (Exception ex)
            {
                Logging.Logger.Instance.LogToFile("Create SalesTransaction", ex);
                return Guid.Empty;
            }
        }

        public Invoice CreateInvoice(SalesTransaction sale, bool isLiterCheck)
        {
            InvoicePrint ip = sale.Nozzle.Dispenser.InvoicePrints.FirstOrDefault();
            if (ip == null)
                return null;
            DatabaseModel db = DatabaseModel.GetContext(sale) as DatabaseModel;
            
            decimal vatValue = Implementation.OptionHandler.Instance.GetDecimalOption("VATValue", (decimal)23);

            Nozzle lastNozzle = sale.Nozzle;

            Invoice invoice = new Invoice();
            invoice.ApplicationUserId = DatabaseModel.CurrentUserId;
            invoice.InvoiceId = Guid.NewGuid();
            db.Add(invoice);
            invoice.TransactionDate = DateTime.Now;
            if (isLiterCheck)// lastNozzle.NozzleState == (int)Common.Enumerators.NozzleStateEnum.LiterCheck)
            {
                Guid invoiceType = Implementation.OptionHandler.Instance.GetGuidOption("LiterCheckInvoiceType", Guid.Empty);
                InvoiceType invType = db.InvoiceTypes.Where(it => it.InvoiceTypeId == invoiceType).FirstOrDefault();
                invoice.InvoiceTypeId = invoiceType;
                invoice.InvoiceType = invType;
                invoice.Number = invType.LastNumber + 1;
                invType.LastNumber = invoice.Number;
                lastNozzle.NozzleState = (int)Common.Enumerators.NozzleStateEnum.Normal;
                Console.WriteLine("LITER CHECK CREATED");
            }
            else //else if (lastNozzle.NozzleState == (int)Common.Enumerators.NozzleStateEnum.Normal)
            {
                Guid invoiceType = lastNozzle.Dispenser.InvoicePrints.First().DefaultInvoiceType;
                
                if (invoiceType == Guid.Empty)
                    invoiceType = ip.DefaultInvoiceType;
                InvoiceType invType = db.InvoiceTypes.Where(it => it.InvoiceTypeId == invoiceType).FirstOrDefault();

                invoice.InvoiceTypeId = invoiceType;
                invoice.InvoiceType = invType;
                invoice.Number = invType.LastNumber + 1;
                invType.LastNumber = invoice.Number;
            }
            invoice.InvoiceFormId = invoice.InvoiceType.InvoiceFormId;
            if (lastNozzle.Dispenser.InvoicePrints.Count > 0)
                invoice.Printer = lastNozzle.Dispenser.InvoicePrints.First().Printer;
            if (invoice.Printer == null || invoice.Printer == "")
                invoice.Printer = invoice.InvoiceType.Printer;

            decimal vatMult = 1 + vatValue / 100;
            invoice.TotalAmount = sale.TotalPrice;
            invoice.VatAmount = sale.TotalPrice - decimal.Round(invoice.TotalAmount.Value / vatMult, 2);
            invoice.NettoAmount = sale.TotalPrice - invoice.VatAmount;
            //invoice.TotalAmount = 


            InvoiceLine invLine = new InvoiceLine();
            invLine.InvoiceLineId = Guid.NewGuid();
            db.Add(invLine);
            invLine.InvoiceId = invoice.InvoiceId;
            invLine.Invoice = invoice;
            invoice.InvoiceLines.Add(invLine);
            invLine.SaleTransactionId = sale.SalesTransactionId;
            invLine.SalesTransaction = sale;
            invLine.TotalPrice = sale.TotalPrice;
            invLine.FuelTypeId = lastNozzle.FuelTypeId;
            invLine.FuelType = lastNozzle.FuelType;
            invLine.UnitPrice = sale.UnitPrice;
            invLine.Volume = sale.Volume;
            invLine.VolumeNormalized = sale.VolumeNormalized;
            invLine.VatAmount = sale.TotalPrice - decimal.Round(invLine.TotalPrice / vatMult, 2);
            invLine.VatPercentage = vatValue;
            sale.InvoiceLines.Add(invLine);
            if (isLiterCheck)
            {
                invLine.TankId = sale.TankSales.Select(t => t.TankId).First();
            }
            invoice.InvoiceLines.Add(invLine);

            return invoice;
        }

        public int EuromatDispenserNumber 
        {
            set 
            {
                DispenserSetting dispSet = this.DispenserSettings.Where(s => s.SettingKey == "EuromatDispenserNumber").FirstOrDefault();
                if (dispSet == null)
                {
                    DatabaseModel db = DatabaseModel.GetContext(this) as DatabaseModel;
                    dispSet = new DispenserSetting();
                    dispSet.DispenserSettingId = Guid.NewGuid();
                    db.Add(dispSet);
                    dispSet.DispenserId = this.DispenserId;
                    dispSet.SettingKey = "EuromatDispenserNumber";
                    this.DispenserSettings.Add(dispSet);
                    dispSet.Dispenser = this;
                }
                dispSet.SettingValue = value.ToString();
            }
            get 
            { 
                DispenserSetting dispSet = this.DispenserSettings.Where(s => s.SettingKey == "EuromatDispenserNumber").FirstOrDefault();
                if (dispSet == null)
                    return 0;
                return int.Parse(dispSet.SettingValue);
            }
        }

        public bool EuromatEnabledController
        {
            get 
            {
                if (this.CommunicationController == null)
                    return false;
                if (this.CommunicationController.EuromatEnabled.HasValue)
                    return this.CommunicationController.EuromatEnabled.Value;
                return false;
            }
        }

        public bool EuromatEnabled
        {
            set
            {
                DispenserSetting dispSet = this.DispenserSettings.Where(s => s.SettingKey == "EuromatEnabled").FirstOrDefault();
                if (dispSet == null)
                {
                    DatabaseModel db = DatabaseModel.GetContext(this) as DatabaseModel;
                    dispSet = new DispenserSetting();
                    dispSet.DispenserSettingId = Guid.NewGuid();
                    db.Add(dispSet);
                    dispSet.DispenserId = this.DispenserId;
                    dispSet.SettingKey = "EuromatEnabled";
                    this.DispenserSettings.Add(dispSet);
                    dispSet.Dispenser = this;
                }
                dispSet.SettingValue = value.ToString();
            }
            get
            {
                DispenserSetting dispSet = this.DispenserSettings.Where(s => s.SettingKey == "EuromatEnabled").FirstOrDefault();
                if (dispSet == null)
                    return false;
                return bool.Parse(dispSet.SettingValue);
            }
        }

        public int TotalVolumeDecimalPlaces
        {
            get
            {
                DispenserSetting setting = this.DispenserSettings.Where(ts => ts.SettingKey == "TotalColumeDecimalPlaces").FirstOrDefault();
                if (setting == null)
                    return 2;
                return int.Parse(setting.SettingValue);
            }
            set
            {
                DispenserSetting setting = this.DispenserSettings.Where(ts => ts.SettingKey == "TotalColumeDecimalPlaces").FirstOrDefault();
                if (setting == null)
                {
                    DatabaseModel db = DatabaseModel.GetContext(this) as DatabaseModel;

                    setting = new DispenserSetting();
                    setting.DispenserSettingId = Guid.NewGuid();
                    db.Add(setting);
                    setting.DispenserId = this.DispenserId;
                    setting.SettingKey = "TotalColumeDecimalPlaces";
                }
                setting.SettingValue = value.ToString();
            }
        }

        public bool Removed
        {
            get
            {
                DispenserSetting setting = this.DispenserSettings.Where(ts => ts.SettingKey == "Removed").FirstOrDefault();
                if (setting == null)
                    return false;
                return bool.Parse(setting.SettingValue);
            }
            set
            {
                DispenserSetting setting = this.DispenserSettings.Where(ts => ts.SettingKey == "Removed").FirstOrDefault();
                if (setting == null)
                {
                    DatabaseModel db = DatabaseModel.GetContext(this) as DatabaseModel;

                    setting = new DispenserSetting();
                    setting.DispenserSettingId = Guid.NewGuid();
                    db.Add(setting);
                    setting.DispenserId = this.DispenserId;
                    setting.SettingKey = "Removed";
                }
                setting.SettingValue = value.ToString();
            }
        }
    }

    public partial class DeviceSetting
    {
        public static void ChangeDescription(object obj)
        {
            DatabaseModel db = DatabaseModel.GetContext(obj) as DatabaseModel;
            var q = db.DeviceSettings.Where(ts => ts.DeviceType == obj.GetType().Name);
            foreach (DeviceSetting setting in q)
            {
                if(obj.GetType() == typeof(Nozzle))
                    setting.Description = string.Format(GetText(obj, setting.SettingKey), ((Nozzle)obj).SerialNumber);
                else if (obj.GetType() == typeof(Dispenser))
                    setting.Description = string.Format(GetText(obj, setting.SettingKey), ((Dispenser)obj).PumpSerialNumber);
                else if (obj.GetType() == typeof(Tank))
                    setting.Description = string.Format(GetText(obj, setting.SettingKey), ((Tank)obj).TankSerialNumber);
                else if (obj.GetType() == typeof(CommunicationController))
                    setting.Description = string.Format(GetText(obj, setting.SettingKey), ((CommunicationController)obj).Name);
            }
        }

        private static string GetText(object obj, string key)
        {
            //DeviceSetting_CommunicationController_DeviceSerialNumber
            try
            {
                string resKey = "DeviceSetting_" + obj.GetType().Name + "_" + key;
                return Properties.Resources.ResourceManager.GetString(resKey);
            }
            catch
            {
                return key;
            }
        }

        public static List<DeviceSetting> settings = new List<DeviceSetting>();

        public static void SetSetting(string key, object value, Dispenser disp, string desc, bool serialNumber)
        {
            DatabaseModel db = DatabaseModel.GetContext(disp) as DatabaseModel;
            DeviceSetting setting = db.DeviceSettings.Where(ts => ts.SettingKey == key && ts.DeviceId == disp.DispenserId && ts.DeviceType == "Dispenser").FirstOrDefault();
            if (setting == null)
            {
                setting = new DeviceSetting();
                setting.DeviceSettingId = Guid.NewGuid();
                db.Add(setting);
                setting.DeviceId = disp.DispenserId;
                setting.SettingKey = key;
                setting.Description = string.Format(GetText(disp, key), disp.PumpSerialNumber);// desc + " - " + disp.PumpSerialNumber;
                setting.DeviceType = "Dispenser";
                settings.Add(setting);
                setting.IsSerialNumber = serialNumber;
            }
            setting.SettingValue = value.ToString();
        }

        public static string GetSetting(string key, Dispenser disp)
        {
            DatabaseModel db = DatabaseModel.GetContext(disp) as DatabaseModel;
            
            DeviceSetting setting = db.DeviceSettings.Where(ts => ts.SettingKey == key && ts.DeviceId == disp.DispenserId && ts.DeviceType == "Dispenser").FirstOrDefault();
            if (setting != null)
                return setting.SettingValue;
            setting = settings.Where(s => s.DeviceId == disp.DispenserId && s.SettingKey == key).FirstOrDefault();
            if (setting != null)
                return setting.SettingValue;
            return null;
        }

        public static void SetSetting(string key, object value, Tank tank, string desc, bool serialNumber)
        {
            DatabaseModel db = DatabaseModel.GetContext(tank) as DatabaseModel;
            DeviceSetting setting = db.DeviceSettings.Where(ts => ts.SettingKey == key && ts.DeviceId == tank.TankId && ts.DeviceType == "Tank").FirstOrDefault();
            if (setting == null)
            {
                setting = new DeviceSetting();
                setting.DeviceSettingId = Guid.NewGuid();
                db.Add(setting);
                setting.DeviceId = tank.TankId;
                setting.SettingKey = key;
                setting.Description = string.Format(GetText(tank, key), tank.TankSerialNumber);// desc + " - " + disp.PumpSerialNumber;
                setting.DeviceType = "Tank";
                setting.IsSerialNumber = serialNumber;
                settings.Add(setting);
            }
            setting.SettingValue = value.ToString();
        }

        public static string GetSetting(string key, Tank tank)
        {
            DatabaseModel db = DatabaseModel.GetContext(tank) as DatabaseModel;
            DeviceSetting setting = db.DeviceSettings.Where(ts => ts.SettingKey == key && ts.DeviceId == tank.TankId && ts.DeviceType == "Tank").FirstOrDefault();
            if (setting != null)
                return setting.SettingValue;
            setting = settings.Where(s => s.DeviceId == tank.TankId && s.SettingKey == key).FirstOrDefault();
            if (setting != null)
                return setting.SettingValue;
            return null;
        }

        public static void SetSetting(string key, object value, Nozzle nz, string desc, bool serialNumber)
        {
            DatabaseModel db = DatabaseModel.GetContext(nz) as DatabaseModel;
            DeviceSetting setting = db.DeviceSettings.Where(ts => ts.SettingKey == key && ts.DeviceId == nz.NozzleId && ts.DeviceType == "Nozzle").FirstOrDefault();
            if (setting == null)
            {
                setting = new DeviceSetting();
                setting.DeviceSettingId = Guid.NewGuid();
                db.Add(setting);
                setting.DeviceId = nz.NozzleId;
                setting.SettingKey = key;
                setting.Description = string.Format(GetText(nz, key), nz.SerialNumber);// desc + " - " + disp.PumpSerialNumber;
                setting.DeviceType = "Nozzle";
                setting.IsSerialNumber = serialNumber;
                settings.Add(setting);
            }
            setting.SettingValue = value.ToString();
        }

        public static string GetSetting(string key, Nozzle nz)
        {
            DatabaseModel db = DatabaseModel.GetContext(nz) as DatabaseModel;
            DeviceSetting setting = db.DeviceSettings.Where(ts => ts.SettingKey == key && ts.DeviceId == nz.NozzleId && ts.DeviceType == "Nozzle").FirstOrDefault();
            if (setting != null)
                return setting.SettingValue;
            setting = settings.Where(s => s.DeviceId == nz.NozzleId && s.SettingKey == key).FirstOrDefault();
            if (setting != null)
                return setting.SettingValue;
            return null;
        }

        public static void SetSetting(string key, object value, CommunicationController controller, string desc, bool serialNumber)
        {
            DatabaseModel db = DatabaseModel.GetContext(controller) as DatabaseModel;
            DeviceSetting setting = db.DeviceSettings.Where(ts => ts.SettingKey == key && ts.DeviceId == controller.CommunicationControllerId && ts.DeviceType == "CommunicationController").FirstOrDefault();
            if (setting == null)
            {
                setting = new DeviceSetting();
                setting.DeviceSettingId = Guid.NewGuid();
                db.Add(setting);
                setting.DeviceId = controller.CommunicationControllerId;
                setting.SettingKey = key;
                setting.Description = string.Format(GetText(controller, key), controller.Name);// desc + " - " + disp.PumpSerialNumber;
                setting.DeviceType = "CommunicationController";
                setting.IsSerialNumber = serialNumber;
                settings.Add(setting);
            }
            setting.SettingValue = value.ToString();
        }

        public static string GetSetting(string key, CommunicationController controller)
        {
            DatabaseModel db = DatabaseModel.GetContext(controller) as DatabaseModel;

            DeviceSetting setting = db.DeviceSettings.Where(ts => ts.SettingKey == key && ts.DeviceId == controller.CommunicationControllerId && ts.DeviceType == "CommunicationController").FirstOrDefault();
            if (setting != null)
                return setting.SettingValue;
            setting = settings.Where(s => s.DeviceId == controller.CommunicationControllerId && s.SettingKey == key).FirstOrDefault();
            if (setting != null)
                return setting.SettingValue;
            return null;
        }

        public string DeviceTypeDescription
        {
            get 
            {
                try
                {
                    return Properties.Resources.ResourceManager.GetString("DeviceType_" + this.DeviceType);
                }
                catch
                {
                    return this.DeviceType;
                }
            }
        }
    }

    public partial class FinancialTransaction
    {
        public decimal RealAmount
        {
            get 
            {
                return this.Amount * this.TransactionTYpe;
            }
        }

        public static FinancialTransaction CreateTransaction(Invoice invoice, int transactionType)
        {
            DatabaseModel db = DatabaseModel.GetContext(invoice) as DatabaseModel;
            FinancialTransaction ft = new FinancialTransaction();
            ft.FinancialTransactionId = Guid.NewGuid();
            ft.TransactionDate = invoice.TransactionDate;
            ft.TransactionTYpe = transactionType;
            ft.InvoiceId = invoice.InvoiceId;
            ft.Invoice = invoice;
            invoice.FinancialTransactions.Add(ft);
            if (invoice.InvoiceType.IsCancelation.HasValue && invoice.InvoiceType.IsCancelation.Value)
                ft.Amount = invoice.TotalAmount.HasValue ? -invoice.TotalAmount.Value : 0;
            else
                ft.Amount = invoice.TotalAmount.HasValue ? invoice.TotalAmount.Value : 0;

            return ft;
        }
    }

    public partial class FuelType
    {
        public decimal GetCurrentPrice()
        {
            FuelTypePrice price = this.FuelTypePrices.Where(fp => fp.ChangeDate <= DateTime.Now).OrderBy(fp => fp.ChangeDate).LastOrDefault();
            if (price == null)
                return 0;
            return price.Price;
        }

        public decimal CurrentPrice
        {
            get
            {
                FuelTypePrice ftp = this.FuelTypePrices.OrderBy(fp => fp.ChangeDate).LastOrDefault();
                if (ftp == null)
                    return 0;
                return ftp.Price;
            }
            set
            {
                DatabaseModel database = DatabaseModel.GetContext(this) as DatabaseModel;

                FuelTypePrice ftp = this.FuelTypePrices.OrderBy(fp => fp.ChangeDate).LastOrDefault();
                if (ftp != null)
                {
                    if ((database.GetState(ftp) & ObjectState.MaskNew) == ObjectState.MaskNew || (database.GetState(ftp) & ObjectState.New) == ObjectState.New)
                    {
                        ftp.Price = value;
                        return;
                    }
                }

                ftp = database.CreateEntity<FuelTypePrice>();
                ftp.FuelTypeId = this.FuelTypeId;
                ftp.FuelType = this;
                ftp.ChangeDate = DateTime.Now;
                ftp.Price = value;
                this.FuelTypePrices.Add(ftp);
            }
        }

        public decimal NormalizeVolume(decimal volStart, decimal temp, decimal density)
        {
            if (this.ThermalCoeficient == 0 || this.BaseDensity == 0)
                return volStart;

            decimal dt = 15 - temp;
            decimal coefficient = (density == 0 ? this.BaseDensity : density) * (this.ThermalCoeficient / this.BaseDensity);
            decimal dv = volStart * coefficient * dt;
            return volStart + dv;
        }
    }

    public partial class Invoice
    {
        public decimal PreDiscountTotal
        {
            get
            {
                decimal total = (this.TotalAmount.HasValue ? this.TotalAmount.Value : 0) + (this.DiscountAmount.HasValue ? this.DiscountAmount.Value : 0);
                return total;
            }
        }

        public decimal PreDiscountVAT
        {
            get
            {
                decimal vatValue = Implementation.OptionHandler.Instance.GetDecimalOption("VATValue", (decimal)23);
                decimal vatMult = 1 + vatValue / 100;
                decimal vat = 0;
               
                if (this.Trader != null && this.Trader.VatExemption.HasValue && this.Trader.VatExemption.Value)
                {
                    vat =  0;
                }
                else
                {
                    vat = this.PreDiscountTotal - decimal.Round(this.PreDiscountTotal / vatMult, 2);
                }
                return vat;
            }
        }

        public decimal RestAmount
        {
            get
            {
                if (this.FinancialTransactions.Count == 0)
                    return 0;
                if (this.IsReplaced)
                    return 0;
                return this.FinancialTransactions.Sum(f => f.Amount * f.TransactionTYpe);
            }
        }

        public bool IsCanceled
        {
            get
            {
                if (this.ChildInvoiceRelations.Where(i => i.RelationType == (int)Common.Enumerators.InvoiceRelationTypeEnum.Cancel).Count() > 0)
                    return true;
                return false;
            }
        }

        public bool IsReplaced
        {
            get
            {
                if (this.ChildInvoiceRelations.Where(i => i.RelationType == (int)Common.Enumerators.InvoiceRelationTypeEnum.Replace).Count() > 0)
                    return true;
                return false;
            }
        }

        public bool IsCanceling
        {
            get
            {
                if (this.ParentInvoiceRelations.Where(i => i.RelationType == (int)Common.Enumerators.InvoiceRelationTypeEnum.Cancel).Count() > 0)
                    return true;
                return false;
            }
        }

        public bool IsReplacing
        {
            get
            {
                if (this.ParentInvoiceRelations.Where(i => i.RelationType == (int)Common.Enumerators.InvoiceRelationTypeEnum.Replace).Count() > 0)
                    return true;
                return false;
            }
        }

        public string CanceledInvoiceString
        {
            get
            {
                if (!this.IsCanceled)
                    return "";
                return "Ακυρώθηκε με το : " + this.ChildInvoiceRelations.Where(i => i.RelationType == (int)Common.Enumerators.InvoiceRelationTypeEnum.Cancel).First().ChildInvoice.Description;
            }
           
        }

        public string ReplacedInvoiceString
        {
            get
            {
                if (!this.IsReplaced)
                    return "";
                return "Αντικαταστάθηκε με το : " + this.ChildInvoiceRelations.Where(i => i.RelationType == (int)Common.Enumerators.InvoiceRelationTypeEnum.Replace).First().ChildInvoice.Description;
            }

        }

        public string CancelingInvoiceString
        {
            get
            {
                if (!this.IsCanceling)
                    return "";
                return "Ακυρωση του το : " + this.ParentInvoiceRelations.Where(i => i.RelationType == (int)Common.Enumerators.InvoiceRelationTypeEnum.Cancel).First().ParentInvoice.Description;
            }

        }

        public string ReplacingInvoiceString
        {
            get
            {
                if (!this.IsReplacing)
                    return "";
                return "Αντικατάσταση του : " + this.ParentInvoiceRelations.Where(i => i.RelationType == (int)Common.Enumerators.InvoiceRelationTypeEnum.Replace).First().ParentInvoice.Description;
            }

        }

        public System.Drawing.Image InvoiceStatusImage
        {
            get
            {
                if (this.IsCanceled)
                    return Properties.Resources.InvoiceCanceled;
                if (this.IsReplaced)
                    return Properties.Resources.InvoiceReplaced;
                return Properties.Resources.InvoiceValid;
            }
        }

        public decimal CreditAmount 
        {
            get
            {
                if (this.FinancialTransactions.Count == 0)
                    return 0;
                return this.FinancialTransactions.Where(ft=>ft.TransactionTYpe == 1).Sum(f => f.Amount);
            }
        }

        public decimal CashAmount
        {
            get
            {
                if (this.FinancialTransactions.Count == 0)
                    return 0;
                if (this.IsReplaced)
                    return 0;
                return this.FinancialTransactions.Where(ft => ft.TransactionTYpe == -1).Sum(f => f.Amount);
            }
        }

        public decimal VATAmountEx
        {
            get 
            {
                if (this.InvoiceType == null || !this.VatAmount.HasValue)
                    return 0;
                return (this.InvoiceType.IsCancelation.HasValue && this.InvoiceType.IsCancelation.Value) ? -this.VatAmount.Value : this.VatAmount.Value; 
            }
        }

        public string Description
        {
            get 
            { 
                string desc = this.InvoiceType.Abbreviation;
                desc = desc + " " + this.Number.ToString();
                desc = desc + "/" + this.TransactionDate.ToString("dd.MM.yyyy HH:mm");
                return desc;
            }
        }

        public string TraderDescription
        {
            get 
            {
                if (this.Trader == null)
                    return "";
                return this.Trader.Name + "(" + this.Vehicle.PlateNumber + ")";
            }
        }

        public string UserName
        {
            get 
            {
                if (this.ApplicationUser == null)
                    return "";
                return this.ApplicationUser.UserName;
            }
        }

        public string InvoiceTypeName
        {
            get 
            {
                if (this.InvoiceType == null)
                    return "";
                return this.InvoiceType.Description;
            }
        }

        public Guid[] UpdateTankSales(List<KeyValuePair<Guid, Common.TankValues>> tankValues)
        {
            var qts = this.InvoiceLines.SelectMany(il => il.SalesTransaction.TankSales).Distinct();
            List<Guid> list = new List<Guid>();
            foreach (TankSale tSale in qts)
            {
                Common.TankValues tvs = tankValues.Where(tv => tv.Key == tSale.TankId).First().Value;
                if (tvs == null)
                    continue;
                if (tvs.LastMeasureTime < tSale.SalesTransaction.TransactionTimeStamp)
                    return new List<Guid>().ToArray();

                tSale.EndTemperature = tvs.CurrentTemperatur;
                tSale.EndLevel = tvs.FuelHeight;// nSale.SaleTankEndValues[nf.TankId].FuelHeight;
                tSale.EndVolume = tSale.Tank.GetTankVolume(tSale.EndLevel.Value);
                tSale.EndVolumeNormalized = tSale.Tank.FuelType.NormalizeVolume(tSale.EndVolume.Value, tSale.EndTemperature, tSale.Tank.CurrentDensity);
                list.Add(tSale.TankId);
            }
            return list.ToArray();
        }

        public InvoiceLine[] OpenInvoiceLines
        {
            get
            {
                return this.InvoiceLines.Where(il => !il.TankFillingId.HasValue).ToArray();
            }
        }

        public void CreateFinancialTransactions()
        {
            if (this.InvoiceTypeId == Guid.Empty)
                return;
            if (this.InvoiceType == null)
                return;
            DatabaseModel db = DatabaseModel.GetContext(this) as DatabaseModel;
            if (db == null)
                return;
            if (!this.InvoiceType.HasFinancialTransactions.HasValue || !this.InvoiceType.HasFinancialTransactions.Value)
            {
                if(this.FinancialTransactions.Count > 0)
                    db.Delete(this.FinancialTransactions);
                return;
            }
            FinancialTransaction creditTransaction = this.FinancialTransactions.Where(f => f.TransactionTYpe == 1).FirstOrDefault();
            FinancialTransaction debitTransaction = this.FinancialTransactions.Where(f => f.TransactionTYpe == -1).FirstOrDefault();
            if (this.PaymentType.HasValue && this.PaymentType.Value == (int)Common.Enumerators.PaymentTypeEnum.Cash)
            {
                if(creditTransaction == null)
                    FinancialTransaction.CreateTransaction(this, 1);
                else
                {
                    creditTransaction.Amount = this.TotalAmount.Value;
                    creditTransaction.TransactionDate = this.TransactionDate;
                }
                if (debitTransaction == null)
                    FinancialTransaction.CreateTransaction(this, -1);
                else
                {
                    debitTransaction.Amount = this.TotalAmount.Value;
                    debitTransaction.TransactionDate = this.TransactionDate;
                }
            }
            else if (this.PaymentType.HasValue && this.PaymentType.Value == (int)Common.Enumerators.PaymentTypeEnum.Credit)
            {
                if (debitTransaction != null)
                    db.Delete(debitTransaction);
                if (creditTransaction == null)
                    FinancialTransaction.CreateTransaction(this, 1);
                else
                {
                    creditTransaction.Amount = this.TotalAmount.Value;
                    creditTransaction.TransactionDate = this.TransactionDate;
                }
            }
            else if (this.PaymentType.HasValue && this.PaymentType.Value == (int)Common.Enumerators.PaymentTypeEnum.CreditCard)
            {
                if (creditTransaction == null)
                    FinancialTransaction.CreateTransaction(this, 1);
                else
                {
                    creditTransaction.Amount = this.TotalAmount.Value;
                    creditTransaction.TransactionDate = this.TransactionDate;
                }
                if (debitTransaction == null)
                    FinancialTransaction.CreateTransaction(this, -1);
                else
                {
                    debitTransaction.Amount = this.TotalAmount.Value;
                    debitTransaction.TransactionDate = this.TransactionDate;
                }
            }
        }

        public void CreatePaymentTransaction()
        {
            if (this.RestAmount <= 0)
                return;
            FinancialTransaction.CreateTransaction(this, -1);
        }
    }

    public partial class InvoiceLine
    {
        public decimal PreDiscountTotal
        {
            get
            {
                decimal total = this.TotalPrice + this.DiscountAmount;
                return total;
            }
        }

        public decimal PreDiscountVAT
        {
            get
            {
                decimal vatPercentage = this.VatPercentage;
                decimal vatMult = 1 + vatPercentage / 100;
                decimal vat = 0;
                vat = this.PreDiscountTotal - decimal.Round(this.PreDiscountTotal / vatMult, 2);
                
                return vat;
            }
        }
    }

    public partial class InvoicePrintView
    {
        public string TraderDescription
        {
            get 
            {
                if (!this.HasTrader)
                    return "ΠΕΛΑΤΗΣ ΛΙΑΝΙΚΗΣ";
                return this.TraderName;
            }
        }

        public bool HasTrader
        {
            get { return !(this.TraderTin == null || this.TraderTin == ""); }
        }
    }

    public partial class Nozzle
    {
        public string Description
        {
            get
            {
                string desc = "";
                if (this.FuelType != null)
                    desc = this.FuelType.Name;
                desc = this.OfficialNozzleNumber.ToString() + " - " + desc;
                return desc;
            }
        }

        public string VolumeCounterSeal
        {
            get
            {
                return DeviceSetting.GetSetting("VolumeCounterSeal", this);
            }
            set
            {
                DeviceSetting.SetSetting("VolumeCounterSeal", value, this, "Σφραγγίδα Ογκομετρητή", false);
            }
        }

        public decimal LastValidTotalizer
        {
            get 
            {
                SalesTransaction sale = this.SalesTransactions.OrderBy(s => s.TransactionTimeStamp).LastOrDefault();
                if (sale == null)
                    return this.TotalCounter;
                return sale.TotalizerEnd;
            }
        }

        public void CorrectTotalizer(decimal newCounter)
        {
            DatabaseModel db = DatabaseModel.GetContext(this) as DatabaseModel;
            SalesTransaction sale = db.CreateEntity<SalesTransaction>();

            sale.NozzleId = this.NozzleId;
            sale.Nozzle = this;
            sale.TransactionTimeStamp = DateTime.Now;
            sale.TotalizerStart = this.LastValidTotalizer;// lastSaleValues.TotalVolumes[lastNozzle.OrderId];// nSale.TotalVolumeStart;
            sale.TotalizerEnd = newCounter;//nSale.TotalVolumeEnd;
            sale.Volume = (sale.TotalizerEnd - sale.TotalizerStart) / 100;//nSale.TotalVolume;
            sale.TotalPrice = 0;
            sale.TransactionTimeStamp = DateTime.Now;
            sale.UnitPrice = 0;
            UsagePeriod up = db.GetUsagePeriod();
            sale.UsagePeriodId = up.UsagePeriodId;
            sale.UsagePeriod = up;
            sale.TemperatureStart = 15;
            sale.TemperatureEnd = 15;
            sale.VolumeNormalized = sale.Volume;
            sale.CRC = sale.CalculateCRC32();

            this.TotalCounter = newCounter;

            List<Tank> tanks = this.NozzleFlows.Where(t=>t.FlowState ==1).Select(t=>t.Tank).Distinct().ToList();

            foreach (Tank t in tanks)
            {
                TankSale tSale = db.CreateEntity<TankSale>();
                tSale.SalesTransactionId = sale.SalesTransactionId;
                tSale.SalesTransaction = sale;
                sale.TankSales.Add(tSale);
                tSale.EndLevel = t.FuelLevel;
                tSale.EndTemperature = t.Temperatire;
                tSale.EndVolume = t.GetTankVolume(t.FuelLevel);
                tSale.EndVolumeNormalized = t.CurrentNettoFuelNomalizedVolume;
                tSale.FuelDensity = t.CurrentDensity;
                tSale.StartLevel = t.FuelLevel;
                tSale.StartTemperature = t.Temperatire;
                tSale.StartVolume = t.GetTankVolume(t.FuelLevel);
                tSale.StartVolumeNormalized = t.CurrentNettoFuelNomalizedVolume;
                tSale.TankId = t.TankId;
                tSale.Tank = t;
                t.TankSales.Add(tSale);
                
            }

            this.SalesTransactions.Add(sale);
        }

        public decimal DiscountPercentage { set; get; }

        public decimal DiscountUnitPrice
        {
            get 
            {
                return Math.Round(this.FuelType.CurrentPrice - (this.FuelType.CurrentPrice * this.DiscountPercentage / 100), 3);
            }
        }
    }

    public partial class OilCompany
    {
        public System.Drawing.Image Image
        {
            get
            {
                try
                {
                    System.Drawing.Image image = System.Drawing.Image.FromStream(new System.IO.MemoryStream(this.Logo));
                    return image;
                }
                catch
                {
                    return null;
                }
            }
        }
    }

    public partial class OutdoorPaymentTerminal
    {

    }

    public partial class OutdoorPaymentTerminalNozzle
    {
        public string Description
        {
            get 
            {
                if (!this.NozzleId.HasValue)
                    return "(Χωρίς Επιλογή)";
                return string.Format("Αντλία {0} / {1} - {2}", this.Nozzle.Dispenser.OfficialPumpNumber, this.Nozzle.OfficialNozzleNumber, this.Nozzle.FuelType.Name);
            }
        }

    }

    public partial class OutdoorPaymentTerminalSchedule
    {
        public bool HasNozzle
        {
            get
            {
                return this.OutdoorPaymentTerminalNozzleId.HasValue;
            }
        }

        public string NozzleDescription
        {
            get 
            {
                if (this.OutdoorPaymentTerminalNozzleId.HasValue)
                    return this.OutdoorPaymentTerminalNozzle.Description;
                return "";
            }
        }

        public Guid SelectedNozzleId
        {
            set
            {
                if (value == Guid.Empty)
                    this.OutdoorPaymentTerminalNozzleId = null;
                else
                {
                    this.OutdoorPaymentTerminalNozzleId = value;
                }
            }
            get
            {
                if (this.OutdoorPaymentTerminalNozzleId.HasValue)
                    return this.OutdoorPaymentTerminalNozzleId.Value;
                return Guid.Empty;
            }
        }
    }

    public partial class OutdoorPaymentTerminalTimeSchedule
    {
        public DateTime TimeTo
        {
            set 
            {
                if (value.TimeOfDay.TotalSeconds > this.TimeFrom.TimeOfDay.TotalSeconds)
                {
                    this.Duration = (int)value.TimeOfDay.Subtract(this.TimeFrom.TimeOfDay).TotalMinutes;
                    //this.TimeFrom = this.TimeFrom.Date.Add(value.TimeOfDay.Subtract(new TimeSpan(0, this.Duration, 0)));
                }
                else
                {
                    TimeSpan ts = value.TimeOfDay.Add(new TimeSpan(24, 0, 0));
                    this.Duration = (int)ts.Subtract(this.TimeFrom.TimeOfDay).TotalMinutes;
                    //this.TimeFrom = this.TimeFrom.Date.Add(ts.Subtract(new TimeSpan(0, this.Duration, 0)));
                }
            }
            get 
            {
                return this.TimeFrom.Add(new TimeSpan(0, this.Duration, 0));
            }
        }

        public DateTime DurationTimeSpan
        {
            set
            {
                this.Duration = (int)value.TimeOfDay.TotalMinutes;
            }
            get 
            {
                return DateTime.Today.Add(new TimeSpan(0, this.Duration, 0));
            }
        }

        public string DurationString
        {
            set 
            {
                if (value == null || value == "")
                    return;
                string[] vals = value.Split(':');
                int d = int.Parse(vals[0]);
                int h = int.Parse(vals[1]);
                int m = int.Parse(vals[2]);
                this.Duration = d * 24 * 60 + h * 60 + m;
            }
            get 
            {
                try
                {
                    TimeSpan ts = new TimeSpan(0, this.Duration, 0);
                    string d = ts.Days.ToString();
                    string h = ts.Hours.ToString();
                    string m = ts.Minutes.ToString();
                    if (d.Length == 1)
                        d = "0" + d;
                    if (h.Length == 1)
                        h = "0" + h;
                    if (m.Length == 1)
                        m = "0" + m;
                    return d + ":" + h + ":" + m;
                }
                catch (Exception ex)
                {
                    return "00:00:00";
                }
            }
        }
    }

    public partial class SystemEvent
    {
        public string Description
        {
            get
            {
                string desc = "";
                if (this.Tank != null)
                    desc = "Δεξαμενή : " + this.Tank.TankNumber.ToString();
                if (this.Nozzle != null)
                    desc = "Αντλία " + this.Nozzle.Dispenser.DispenserNumber.ToString() + " - Ακροσωλήνιο : " + (this.Nozzle.OrderId + 1).ToString();

                string desc2 = Common.Enumerators.EnumToList.GetDescription<Common.Enumerators.AlertTypeEnum>((Common.Enumerators.AlertTypeEnum)this.EventType);
                if (desc == "")
                    desc = desc2;
                else
                    desc = desc + " " + desc2;

                if (desc == "")
                    return this.Message;
                else
                    return desc + " " + this.Message;
            }
        }

        public string DeviceDescription
        {
            get 
            {
                if (this.Nozzle != null)
                    return "Αντλία " + this.Nozzle.Dispenser.DispenserNumber.ToString() + " Ακροσωλήνιο : " + this.Nozzle.OfficialNozzleNumber.ToString();
                else if(this.Tank != null)
                    return "Δεξαμενή : " + this.Tank.TankNumber.ToString();
                else if (this.Dispenser != null)
                    return "Αντλία : " + this.Dispenser.OfficialPumpNumber.ToString();
                return "";
            }
        }
    }

    public partial class SystemEventDatum
    {
        public string Description
        {
            get
            {
                return Properties.Resources.ResourceManager.GetString("AlertData_" + this.PropertyName);
            }
        }
    }

    partial class Tank
    {
        public bool Initialized { private set; get; }

        public bool IsRealTank
        {
            get 
            {
                return this.IsVirtual.HasValue ? !this.IsVirtual.Value : true;
            }
        }

        public Common.Tank CommonTank
        {
            get
            {
                Common.Tank tank = new Common.Tank();
                tank.Address = this.Address;
                tank.Channel = this.Channel;
                if (this.AtgProbeType != null && this.AtgProbeType.AtgProbeProtocol != null)
                    tank.AtgProbeProtocol = this.AtgProbeType.AtgProbeProtocol.EnumeratorValue;
                return tank;
            }
        }

        public decimal FuelLevel
        {
            get 
            {
                decimal fl = this.FuelLevelBase + this.OffsetVolume;
                if (fl < 0)
                    return 0;
                return fl;
            }
            set
            {
                decimal fl = value - this.OffsetVolume;
                if (fl < 0)
                    fl = 0;
                this.FuelLevelBase = fl;
            }
        }

        public decimal WaterLevel
        {
            get
            {
                decimal wl = this.WaterLevelBase + this.OffestWater;
                if (wl < 0)
                    wl = 0;
                return wl;
            }
            set
            {
                decimal wl = value - this.OffestWater;
                if (wl < 0)
                    wl = 0;
                this.WaterLevelBase = wl;
            }
        }

        public decimal CurrentDensity
        {
            get
            {
                TankPrice tankPrice = this.TankPrices.OrderBy(tp => tp.ChangeDate).LastOrDefault();
                if (tankPrice == null)
                    return this.FuelType.BaseDensity;
                return tankPrice.FuelDensity;
            }
        }

        public decimal PreviousDensity
        {
            get
            {
                TankPrice tankPrice = this.TankPrices.OrderByDescending(tp => tp.ChangeDate).Skip(1).FirstOrDefault();
                if (tankPrice == null)
                    return this.FuelType.BaseDensity;
                return tankPrice.FuelDensity;
            }
        }

        public decimal LastValidLevel 
        {
            get { return this.GetLastValidLevel(); }
        }

        public decimal LastValidLevelTF
        {
            get { return this.GetLastValidLevelTF(); }
        }

        public decimal CurrentNettoFuelNomalizedVolume
        {
            get
            {
                this.FuelLevel = this.GetLastValidLevel();
                
                return this.FuelType.NormalizeVolume(this.GetTankVolume(this.FuelLevel), this.Temperatire, this.CurrentDensity);
            }
        }

        public decimal LastNettoFuelNormalizedVolume
        {
            get
            {
                this.FuelLevel = this.GetLastValidLevel();
                
                return this.FuelType.NormalizeVolume(this.GetTankVolume(this.LastValidLevel), this.GetLastValidTemperatur(), this.CurrentDensity);
            }
        }

        public string CurrentStateName
        {
            get 
            {
                return ((Common.Enumerators.TankStatusEnum)this.PhysicalState).ToString();
            }
        }

        public decimal GetTankVolume(decimal height)
        {
            if (height < this.GaugeMinimumHeight + this.OffsetVolume)
                height = 0;
            //if (height < this.MinFuelHeight - 10)//LEVELCORRECTION
            //    height = 0;
            Titrimetry titrimetry = this.Titrimetries.OrderBy(tm => tm.TitrationDate).LastOrDefault();
            if (titrimetry == null)
                return 0;
            if (titrimetry.TitrimetryLevels.Count == 0)
                return 0;
            TitrimetryLevel previousLevel = titrimetry.TitrimetryLevels.OrderBy(tml => tml.Height).Where(tml => tml.Height.Value <= height).LastOrDefault();
            TitrimetryLevel nextLevel = titrimetry.TitrimetryLevels.OrderBy(tml => tml.Height).Where(tml => tml.Height.Value > height).FirstOrDefault();
            if (nextLevel != null)
            {
                decimal prev = previousLevel ==  null || !previousLevel.Volume.HasValue ? 0 : previousLevel.Volume.Value;
                decimal volDiff = nextLevel.Volume.Value - prev;
                decimal heightStep = (nextLevel.Height.Value) - (previousLevel == null || !previousLevel.Height.HasValue ? 0 : previousLevel.Height.Value); //previousLevel.Height.Value;
                decimal dHeight = height - (previousLevel == null || !previousLevel.Height.HasValue ? 0 : previousLevel.Height.Value);// prev;
                decimal dVolume = 0;
                if(heightStep != 0)
                    dVolume = volDiff * dHeight / heightStep;
                return prev + dVolume;
            }
            else
            {
                return previousLevel.Volume.Value;
            }
        }

        public decimal GetTankVolumeNormalized(decimal height)
        {
            return this.FuelType.NormalizeVolume(this.GetTankVolume(height), this.Temperatire, this.CurrentDensity);
        }

        public TankSaleView GetLastSale()
        {
            DatabaseModel db = DatabaseModel.GetContext(this) as DatabaseModel;

            try
            {
                var q = db.ExecuteQuery<Data.TankSaleView>("select tOP 1 * from dbo.TankSaleView where tankId = '" + this.TankId.ToString() + "' ORDER BY TransactionTimeStamp desc", new System.Data.Common.DbParameter[] { });
                int viewsCount = q.Count;
                if (viewsCount == 0)
                    return null;
                return q.LastOrDefault();
                
                //int viewsCount = db.TankSaleViews.Where(t => t.TankId == this.TankId).Count();
                //if (viewsCount == 0)
                //    return null;
                //TankSaleView lastSale = db.TankSaleViews.Where(t => t.TankId == this.TankId).OrderByDescending(t => t.TransactionTimeStamp).FirstOrDefault();
                //return lastSale;
            }
            catch(Exception ex)
            {
                Logger.Instance.LogToFile(" TankSaleView GetLastSale()", ex);
                db.Refresh(RefreshMode.OverwriteChangesFromStore, db.TankSaleViews.Where(t => t.TankId == this.TankId));
                try
                {
                    TankSaleView lastSale = db.TankSaleViews.Where(t => t.TankId == this.TankId).OrderByDescending(t => t.TransactionTimeStamp).FirstOrDefault();
                    return lastSale;
                }
                catch(Exception exc)
                {
                    Logger.Instance.LogToFile( "TankSaleView GetLastSale()", exc);
                    return null;
                }
            }
        }
        public decimal[] GetLastValidValues()
        {
            DatabaseModel db = DatabaseModel.GetContext(this) as DatabaseModel;

            DateTime lastFillingDate = DateTime.MinValue;
            DateTime lastSaleDate = DateTime.MinValue;
            TankFilling lastFilling = this.TankFillings.OrderByDescending(t => t.TransactionTimeEnd).FirstOrDefault();
            
            //System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
            TankSaleView lastSale = this.GetLastSale();// db.TankSaleViews.Where(t => t.TankId == this.TankId).OrderByDescending(t => t.TransactionTimeStamp).FirstOrDefault();
            //sw.Stop();
            //TimeSpan elapsedTime = sw.Elapsed;
            //System.IO.File.AppendAllText(System.Environment.CurrentDirectory + "\\ThreadController.txt", string.Format("\n{1}  this.GetLastSale(); {0}", elapsedTime, System.DateTime.Now));
            
            if(lastSale != null)
                lastSaleDate = lastSale.TransactionTimeStamp;

            if(lastFilling != null)
                lastFillingDate = lastFilling.TransactionTimeEnd;
            decimal[] validValues = new decimal[3];

            //////ValidLevel = 0
            if(lastFillingDate < lastSaleDate)
                validValues[0] = lastSale.EndLevel.Value;
            else if(lastFillingDate > lastSaleDate)
                validValues[0] = lastFilling.LevelEnd;
            else
                validValues[0] = 0;

            ////ValidWater =1
            if(lastFillingDate < lastSaleDate)
                validValues[1] = lastSale.EndTemperature;
            else if(lastFillingDate > lastSaleDate)
                validValues[1] = lastFilling.TankTemperatureEnd;
            else
                validValues[1] = this.WaterLevel;

            ////ValidTemp =2
            if(lastFillingDate < lastSaleDate)
                validValues[2] = lastSale.EndTemperature;
            else if(lastFillingDate > lastSaleDate)
                validValues[2] = lastFilling.TankTemperatureEnd;
            else
                validValues[2] = this.Temperatire;

            return validValues;
        }
        public decimal GetLastValidLevel()
        {
            DatabaseModel db = DatabaseModel.GetContext(this) as DatabaseModel;

            DateTime lastFillingDate = DateTime.MinValue;
            DateTime lastSaleDate = DateTime.MinValue;
            TankFilling lastFilling = this.TankFillings.OrderByDescending(t => t.TransactionTimeEnd).FirstOrDefault();
            TankSaleView lastSale = this.GetLastSale();// db.TankSaleViews.Where(t => t.TankId == this.TankId).OrderByDescending(t => t.TransactionTimeStamp).FirstOrDefault();

            if (lastSale != null)
                lastSaleDate = lastSale.TransactionTimeStamp;

            if (lastFilling != null)
                lastFillingDate = lastFilling.TransactionTimeEnd;

            if (lastFillingDate < lastSaleDate)
                return lastSale.EndLevel.Value;
            else if (lastFillingDate > lastSaleDate)
                return lastFilling.LevelEnd;
            else
                return 0;
        }

        public decimal GetLastValidLevelTF()
        {
            DatabaseModel db = DatabaseModel.GetContext(this) as DatabaseModel;

            DateTime lastFillingDate = DateTime.MinValue;
            DateTime lastSaleDate = DateTime.MinValue;
            TankFilling lastFilling = this.TankFillings.Where(t=>t.InvoiceLines.Count > 0 || t.LevelStart > t.LevelEnd).OrderByDescending(t => t.TransactionTimeEnd).FirstOrDefault();
            TankSaleView lastSale = this.GetLastSale();// db.TankSaleViews.Where(t => t.TankId == this.TankId).OrderByDescending(t => t.TransactionTimeStamp).FirstOrDefault();

            if (lastSale != null)
                lastSaleDate = lastSale.TransactionTimeStamp;

            if (lastFilling != null)
                lastFillingDate = lastFilling.TransactionTimeEnd;

            if (lastFillingDate < lastSaleDate)
                return lastSale.EndLevel.Value;
            else if (lastFillingDate > lastSaleDate)
                return lastFilling.LevelEnd;
            else
                return 0;
        }

        public decimal GetLastValidWaterLevel()
        {
            DatabaseModel db = DatabaseModel.GetContext(this) as DatabaseModel;
            DateTime lastFillingDate = DateTime.MinValue;
            DateTime lastSaleDate = DateTime.MinValue;
            TankFilling lastFilling = this.TankFillings.OrderByDescending(t => t.TransactionTimeEnd).FirstOrDefault();
            TankSaleView lastSale = this.GetLastSale();// db.TankSaleViews.Where(t => t.TankId == this.TankId).OrderByDescending(t => t.TransactionTimeStamp).FirstOrDefault();

            if (lastSale != null)
                lastSaleDate = lastSale.TransactionTimeStamp;

            if (lastFilling != null)
                lastFillingDate = lastFilling.TransactionTimeEnd;

            if (lastFillingDate < lastSaleDate)
                return lastSale.EndTemperature;
            else if (lastFillingDate > lastSaleDate)
                return lastFilling.TankTemperatureEnd;
            else
                return this.WaterLevel;
        }

        public decimal GetLastValidTemperatur()
        {
            DatabaseModel db = DatabaseModel.GetContext(this) as DatabaseModel;
            DateTime lastFillingDate = DateTime.MinValue;
            DateTime lastSaleDate = DateTime.MinValue;
            TankFilling lastFilling = this.TankFillings.OrderByDescending(t => t.TransactionTimeEnd).FirstOrDefault();
            TankSaleView lastSale = this.GetLastSale();// db.TankSaleViews.Where(t => t.TankId == this.TankId).OrderByDescending(t => t.TransactionTimeStamp).FirstOrDefault();

            if (lastSale != null)
                lastSaleDate = lastSale.TransactionTimeStamp;

            if (lastFilling != null)
                lastFillingDate = lastFilling.TransactionTimeEnd;

            if (lastFillingDate < lastSaleDate)
                return lastSale.EndTemperature;
            else if (lastFillingDate > lastSaleDate)
                return lastFilling.TankTemperatureEnd;
            else
                return this.Temperatire;
        }

        public decimal StatisticalVolumeError
        {
            get
            {
                try
                {
                    Titrimetry titrimetry = this.Titrimetries.OrderBy(t => t.TitrationDate).LastOrDefault();
                    
                    if (titrimetry == null || titrimetry.TitrimetryLevels.ToList().Count == 0)
                        return 0;

                    TitrimetryLevel l1 = titrimetry.TitrimetryLevels.OrderBy(t => t.Height).Where(t => t.Height <= this.FuelLevel).LastOrDefault();
                    TitrimetryLevel l2 = titrimetry.TitrimetryLevels.OrderBy(t => t.Height).Where(t => t.Height >= this.FuelLevel).FirstOrDefault();
                    if (l1 == null || l2 == null)
                        return 0;

                    if (l2.Equals(l1))
                        l2 = titrimetry.TitrimetryLevels.OrderBy(t => t.Height).Where(t => t.Height >= this.FuelLevel).Skip(1).FirstOrDefault();

                    //this.statisticalHeightError = 2 * (l2.Height.Value - l1.Height.Value);
                    return 2 * (l2.Volume.Value - l1.Volume.Value);
                }
                catch
                {
                    return 0;
                }
            }
        }

        public decimal StatisticalHeightError
        {
            get
            {
                try
                {
                    Titrimetry titrimetry = this.Titrimetries.OrderBy(t => t.TitrationDate).LastOrDefault();
                    
                    if (titrimetry == null || titrimetry.TitrimetryLevels.ToList().Count == 0)
                        return 0;

                    TitrimetryLevel l1 = titrimetry.TitrimetryLevels.OrderBy(t => t.Height).Where(t => t.Height <= this.FuelLevel).LastOrDefault();
                    TitrimetryLevel l2 = titrimetry.TitrimetryLevels.OrderBy(t => t.Height).Where(t => t.Height >= this.FuelLevel).FirstOrDefault();
                    if (l1 == null || l2 == null)
                        return 0;

                    if (l2.Equals(l1))
                        l2 = titrimetry.TitrimetryLevels.OrderBy(t => t.Height).Where(t => t.Height >= this.FuelLevel).Skip(1).FirstOrDefault();

                    //this.statisticalHeightError = 2 * (l2.Height.Value - l1.Height.Value);
                    return 2 * (l2.Height.Value - l1.Height.Value);
                }
                catch
                {
                    return 0;
                }
            }
        }

        public string descExt;
        public string DescriptionExt
        {
            set 
            {
                this.descExt = value;
            }
            get 
            {
                if (this.TankId == Guid.Empty)
                    return descExt;
                else
                    return this.Description;
            }
        }

        public string Description
        {
            get 
            {
                string desc = "";
                desc = this.TankNumber.ToString();
                if (this.FuelType != null)
                    desc = desc + " - " + this.FuelType.Name;
                return desc;
            }
        }

        public void InitializeTank()
        {
            if (this.Initialized)
                return;
            
            if (this.FuelLevel != this.GetLastValidLevel())
            {
                DatabaseModel db = DatabaseModel.GetContext(this) as DatabaseModel;
                TankFilling tf = db.CreateEntity<TankFilling>();

                UsagePeriod up = db.GetUsagePeriod();
                tf.UsagePeriodId = up.UsagePeriodId;
                up.TankFillings.Add(tf);

                tf.FuelDensity = this.FuelType.BaseDensity;
                tf.LevelStart = 0;
                tf.LevelEnd = this.FuelLevel;
                tf.TankId = this.TankId;
                tf.TankTemperatureStart = 0;
                tf.TankTemperatureEnd = this.Temperatire;
                tf.TransactionTime = DateTime.Now;
                tf.TransactionTimeEnd = DateTime.Now;
                tf.Volume = this.GetTankVolume(tf.LevelEnd);
                tf.VolumeNormalized = this.FuelType.NormalizeVolume(tf.Volume, tf.TankTemperatureEnd, tf.FuelDensity);
                tf.VolumeReal = tf.Volume;
                tf.VolumeRealNormalized = tf.VolumeNormalized;
                tf.Tank = this;

                TankPrice lastTankPrice = this.TankPrices.OrderBy(tp => tp.ChangeDate).LastOrDefault();
                if (lastTankPrice == null)
                {
                    lastTankPrice = db.CreateEntity<TankPrice>();
                    lastTankPrice.ChangeDate = DateTime.Now;
                    lastTankPrice.Price = this.FuelType.CurrentPrice;
                    lastTankPrice.FuelDensity = FuelType.BaseDensity;
                    lastTankPrice.TankId = this.TankId;
                    this.TankPrices.Add(lastTankPrice);
                }
                tf.TankPriceId = lastTankPrice.TankPriceId;
                tf.TankPrice = lastTankPrice;
                tf.CRC = this.CalculateCRC32();
                this.TankFillings.Add(tf);
                this.Initialized = true;
            }
            this.Initialized = true;
        }

        public void CorrectTankData()
        {
            
            DatabaseModel db = DatabaseModel.GetContext(this) as DatabaseModel;
            TankFilling tf = db.CreateEntity<TankFilling>();

            UsagePeriod up = db.GetUsagePeriod();
            tf.UsagePeriodId = up.UsagePeriodId;
            up.TankFillings.Add(tf);

            tf.FuelDensity = this.FuelType.BaseDensity;
            tf.LevelStart = this.GetLastValidLevel();
            tf.LevelEnd = this.FuelLevel;
            tf.TankId = this.TankId;
            tf.TankTemperatureStart = this.GetLastValidTemperatur();
            tf.TankTemperatureEnd = this.Temperatire;
            tf.TransactionTime = DateTime.Now;
            tf.TransactionTimeEnd = DateTime.Now;

            decimal volumeStart = this.GetTankVolume(tf.LevelStart);
            decimal volumeStartNormalized = this.FuelType.NormalizeVolume(volumeStart, tf.TankTemperatureStart, this.CurrentDensity);
            decimal volumeEnd = this.GetTankVolume(tf.LevelEnd);
            decimal volumeEndNormalized = this.FuelType.NormalizeVolume(volumeEnd, tf.TankTemperatureEnd, tf.FuelDensity);
            decimal volDiff = volumeEndNormalized - volumeStartNormalized;

            tf.Volume = 0;
            tf.VolumeNormalized = 0;
            tf.VolumeReal = volumeEnd - volumeStart;
            tf.VolumeRealNormalized = volDiff;
            tf.Tank = this;

            TankPrice lastTankPrice = this.TankPrices.OrderBy(tp => tp.ChangeDate).LastOrDefault();
            if (lastTankPrice == null)
            {
                lastTankPrice = db.CreateEntity<TankPrice>();
                lastTankPrice.ChangeDate = DateTime.Now;
                lastTankPrice.Price = this.FuelType.CurrentPrice;
                lastTankPrice.FuelDensity = FuelType.BaseDensity;
                lastTankPrice.TankId = this.TankId;
                this.TankPrices.Add(lastTankPrice);
            }
            tf.TankPriceId = lastTankPrice.TankPriceId;
            tf.TankPrice = lastTankPrice;
            tf.CRC = this.CalculateCRC32();
            this.TankFillings.Add(tf);
        }

        public decimal GetLastPrice()
        {
            TankPrice tankPrice = this.TankPrices.OrderByDescending(tp => tp.ChangeDate).FirstOrDefault();
            if (tankPrice == null)
                return 0;
            decimal vatValue = Implementation.OptionHandler.Instance.GetDecimalOption("VATValue", (decimal)23);
            decimal vatMultiplier = 1 + vatValue / 100;
            return tankPrice.Price * vatMultiplier;
        }

        public Guid CreateFillingInvoice(Common.Sales.TankFillingData data)
        {
            DatabaseModel db = DatabaseModel.GetContext(this) as DatabaseModel;

            Tank tank = db.Tanks.Where(t=>t.TankId == data.TankId).FirstOrDefault();
            if(tank == null)
                return Guid.Empty;

            FuelType fuelType = db.FuelTypes.Where(f=>f.FuelTypeId == data.FuelTypeId).FirstOrDefault();
            if(fuelType == null)
                return Guid.Empty;

            Invoice invoice = new Invoice();
            invoice.InvoiceId = Guid.NewGuid();
            db.Add(invoice);

            InvoiceType invType = db.InvoiceTypes.Where(it => it.InvoiceTypeId == data.InvoiceTypeId).FirstOrDefault();
            invoice.InvoiceTypeId = invType.InvoiceTypeId;
            invoice.InvoiceType = invType;
            invoice.Number = invType.LastNumber + 1;
            invType.LastNumber = invoice.Number;
            invoice.TransactionDate = DateTime.Now;
            invoice.Printer = invType.Printer;
            invoice.ApplicationUserId = DatabaseModel.CurrentUserId;
            invoice.IsPrinted = false;
            invoice.NettoAmount = 0;
            invoice.TotalAmount = 0;
            if (data.VehicelId != Guid.Empty)
            {
                invoice.VehicleId = data.VehicelId;
                invoice.Vehicle = db.Vehicles.Where(v => v.VehicleId == data.VehicelId).FirstOrDefault();
                invoice.TraderId = invoice.Vehicle.TraderId;
                invoice.Trader = invoice.Vehicle.Trader;
            }
            decimal levelStart = this.LastValidLevel;
            decimal leveEnd = data.Values.FuelHeight;

            if (levelStart < this.GaugeMinimumHeight + this.OffsetVolume)
                levelStart = 0;
            if (leveEnd < this.GaugeMinimumHeight + this.OffsetVolume)
                leveEnd = 0;
            //if (levelStart < tank.MinFuelHeight - 10)//LEVELCORRECTION
            //    levelStart = 0;
            //if (leveEnd < tank.MinFuelHeight - 10) //LEVELCORRECTION
            //    leveEnd = 0;

            decimal volume = Math.Abs(this.GetTankVolume(levelStart) - this.GetTankVolume(leveEnd));
            decimal volume15 = Math.Abs(this.GetTankVolumeNormalized(levelStart) - this.GetTankVolumeNormalized(leveEnd));

            InvoiceLine invoiceLine = new InvoiceLine();
            invoiceLine.InvoiceId = invoice.InvoiceId;
            db.Add(invoiceLine);
            invoiceLine.Invoice = invoice;
            invoiceLine.FuelTypeId = data.FuelTypeId;
            invoiceLine.FuelType = tank.FuelType;
            invoice.InvoiceLines.Add(invoiceLine);
            invoiceLine.Volume = volume;//tank.GetTankVolume(data.Values.FuelHeight);
            invoiceLine.VolumeNormalized = volume15;// tank.GetTankVolumeNormalized(data.Values.FuelHeight);
            invoiceLine.FuelTypeId = data.FuelTypeId;
            invoiceLine.DiscountAmount = 0;
            invoiceLine.FuelDensity = tank.GetDensityAtTime(DateTime.Now);
            invoiceLine.TankId = tank.TankId;
            invoiceLine.Temperature = tank.GetLastValidTemperatur();
            invoiceLine.TotalPrice = 0;
            invoiceLine.UnitPrice = 0;
            invoiceLine.VatAmount = 0;
            invoiceLine.VatPercentage = 0;
            
            invoiceLine.Tank = tank;
            invoiceLine.FuelType = fuelType;
            tank.InvoiceLines.Add(invoiceLine);
            fuelType.InvoiceLines.Add(invoiceLine);
            return invoiceLine.InvoiceLineId;
        }

        public TankFilling CreateTankFilling(Guid invoiceLineId, Common.TankValues endParameters, DateTime dtStart)
        {
            DatabaseModel db = DatabaseModel.GetContext(this) as DatabaseModel;
            TankFilling tf = db.CreateEntity<TankFilling>();
            InvoiceLine il = db.InvoiceLines.Where(i => i.InvoiceLineId == invoiceLineId).FirstOrDefault();
            if (il != null)
            {
                tf.FuelDensity = il.FuelDensity;
                il.TankFillingId = tf.TankFillingId;
                il.TankFilling = tf;
                tf.InvoiceLines.Add(il);
                tf.Volume = il.Volume;
                tf.VolumeNormalized = il.VolumeNormalized;
            }
            tf.TankId = this.TankId;
            tf.TankTemperatureEnd = endParameters.CurrentTemperatur;
            tf.TankTemperatureStart = this.GetLastValidTemperatur();
            tf.TransactionTime = dtStart;
            tf.TransactionTimeEnd = DateTime.Now;
            UsagePeriod up = db.GetUsagePeriod();
            tf.UsagePeriodId = up.UsagePeriodId;
            up.TankFillings.Add(tf);
            //tf.UsagePeriod = up;
            // this.GetTankVolume(endParameters.FuelHeight - startParameters.FuelHeight);
            // db.NormalizeVolume(tf.Volume, endParameters.CurrentTemperatur);
            tf.LevelStart = this.LastValidLevelTF;
            tf.LevelEnd = endParameters.FuelHeight + this.OffsetVolume;

            if (tf.LevelStart < this.GaugeMinimumHeight + this.OffsetVolume)
                tf.LevelStart = 0;
            if (tf.LevelEnd < this.GaugeMinimumHeight + this.OffsetVolume)
                tf.LevelEnd = 0;

            //if (tf.LevelStart < this.MinFuelHeight - 10)//LEVELCORRECTION
            //    tf.LevelStart = 0;
            //if (tf.LevelEnd < this.MinFuelHeight - 10)//LEVELCORRECTION
            //    tf.LevelEnd = 0;

            TankPrice lastTankPrice = this.TankPrices.OrderBy(tp => tp.ChangeDate).LastOrDefault();
            if (lastTankPrice == null)
            {
                lastTankPrice = db.CreateEntity<TankPrice>();
                lastTankPrice.ChangeDate = DateTime.Now;
                if (il != null)
                {
                    lastTankPrice.Price = il.UnitPrice;
                }
                lastTankPrice.FuelDensity = FuelType.BaseDensity;
                lastTankPrice.TankId = this.TankId;
                this.TankPrices.Add(lastTankPrice);
            }
            TankPrice newTankPrice = db.CreateEntity<TankPrice>();

            decimal volumeStart = this.GetTankVolume(tf.LevelStart);
            decimal volumeStartNormalized = this.FuelType.NormalizeVolume(volumeStart, tf.TankTemperatureStart, lastTankPrice.FuelDensity);
            decimal volumeEnd = this.GetTankVolume(tf.LevelEnd);
            decimal volumeEndNormalized = this.FuelType.NormalizeVolume(volumeEnd, tf.TankTemperatureEnd, tf.FuelDensity);
            decimal volDiff = volumeEndNormalized - volumeStartNormalized;

            tf.VolumeReal = Math.Abs(volumeEnd - volumeStart);
            tf.VolumeRealNormalized = Math.Abs(volDiff);

            newTankPrice.ChangeDate = DateTime.Now;

            if (volumeEndNormalized == 0)
            {
                newTankPrice.Price = 0;
                newTankPrice.FuelDensity = 0;
            }
            else
            {
                if (il != null)
                {
                    newTankPrice.Price = ((volumeStartNormalized * lastTankPrice.Price) + (volDiff * il.UnitPrice)) / volumeEndNormalized;
                }
                newTankPrice.FuelDensity = ((volumeStartNormalized * lastTankPrice.FuelDensity) + (volDiff * tf.FuelDensity)) / volumeEndNormalized;
            }
            newTankPrice.TankId = this.TankId;
            this.TankPrices.Add(newTankPrice);
            tf.TankPriceId = newTankPrice.TankPriceId;
            tf.TankPrice = newTankPrice;
            tf.CRC = this.CalculateCRC32();
            return tf;
        }
    }

    public partial class DatabaseModel
    {
        public decimal GetAvgTemperature(IEnumerable<SalesTransaction> sales)
        {
            decimal temp = 0;
            decimal vol = 0;
            foreach (SalesTransaction sale in sales)
            {
                decimal sTemp = (sale.TemperatureEnd + sale.TemperatureEnd) / 2;
                sTemp = sTemp * sale.VolumeNormalized;
                temp = temp + sTemp;
                vol = vol + sale.VolumeNormalized;
            }
            if (vol == 0)
                return 0;
            return temp / vol;
        }

        public decimal GetAvgDensity(IEnumerable<SalesTransaction> sales, FuelType ft)
        {
            decimal dens = 0;
            decimal vol = 0;
            foreach (SalesTransaction sale in sales)
            {
                dens = dens + sale.GetAvgDensity();
                vol = vol + sale.VolumeNormalized;
            }
            if (vol > 0)
                return dens / vol;
            return ft.BaseDensity;
        }
    }

    public partial class InvoiceLine
    {
        public string FuelTypeName 
        {
            get 
            {
                if (this.FuelType != null)
                    return this.FuelType.Name;
                else if (this.SalesTransaction != null && this.SalesTransaction.Nozzle != null && this.SalesTransaction.Nozzle.FuelType != null)
                    return this.SalesTransaction.Nozzle.FuelType.Name;
                else
                    return "";
            }
        }

        public string InvoiceTypeName
        {
            get
            {
                if (this.Invoice != null && this.Invoice.InvoiceType != null)
                    return this.Invoice.InvoiceType.Description;
                return "";
            }
        }

        public DateTime InvoiceDate
        {
            get 
            { 
                if(this.Invoice != null)
                    return this.Invoice.TransactionDate.Date;
                return new DateTime(1900, 1, 1);
            }
        }
    }

    public partial class Tank
    {
        //public string Description
        //{
        //    get { return "Δεξαμενή : " + this.TankNumber.ToString(); }
        //}

        public decimal GetLevelAtTime(DateTime dt)
        {
            
            TankFilling firstFilling = this.TankFillings.Where(tf => tf.TransactionTimeEnd <= dt).OrderByDescending(tf => tf.TransactionTimeEnd).FirstOrDefault();
            TankSale firstTransaction = this.TankSales.Where(ts => ts.SalesTransaction.TransactionTimeStamp <= dt).OrderByDescending(ts => ts.SalesTransaction.TransactionTimeStamp).FirstOrDefault();
            SalesTransaction firstSale = null;
            if (firstTransaction != null)
                firstSale = firstTransaction.SalesTransaction;

            if (firstFilling == null && firstSale == null)
                return 0;
            if (firstSale == null)
                return firstFilling.LevelStart;
            if (firstFilling == null)
                return firstTransaction.StartLevel;
            if (firstSale.TransactionTimeStamp > firstFilling.TransactionTimeEnd)
                return firstTransaction.StartLevel;
            return firstFilling.LevelStart;
        }

        public decimal GetTempmeratureAtTime(DateTime dt)
        {
            TankFilling lastFilling = this.TankFillings.Where(tf => tf.TransactionTimeEnd <= dt).OrderByDescending(tf => tf.TransactionTimeEnd).FirstOrDefault();
            TankSale lastTransaction = this.TankSales.Where(ts => ts.SalesTransaction.TransactionTimeStamp <= dt).OrderByDescending(ts => ts.SalesTransaction.TransactionTimeStamp).FirstOrDefault();
            SalesTransaction lastSale = null;
            if (lastTransaction != null)
                lastSale = lastTransaction.SalesTransaction;

            if (lastFilling == null && lastSale == null)
                return 0;
            if (lastSale == null)
                return lastFilling.TankTemperatureStart;
            if (lastFilling == null)
                return lastTransaction.StartTemperature.Value;
            if (lastSale.TransactionTimeStamp > lastFilling.TransactionTimeEnd)
                return lastTransaction.StartTemperature.Value;
            return lastFilling.TankTemperatureStart;
        }

        public decimal GetDensityAtTime(DateTime dt)
        {
            TankPrice tankPrice = this.TankPrices.Where(tp => tp.ChangeDate <= dt).OrderByDescending(tp => tp.ChangeDate).FirstOrDefault();
            if (tankPrice == null)
                return FuelType.BaseDensity;
            return tankPrice.FuelDensity;
        }

        public decimal LastSalesDifference(int salesCount)
        {
            if (!this.IsVirtual.HasValue || this.IsVirtual.Value)
                return 0;
            //DatabaseModel db = DatabaseModel.GetContext(this) as DatabaseModel;
            //db.SalesTransactions.Where(s=>s.TankSales.Select(t=>t.Tank).Contains(this)).OrderByDescending(s=>s.TransactionTimeStamp).SelectMany(s=>s.TankSales).Take(salesCount)
            TankSale[] tankSales = this.TankSales.OrderByDescending(t => t.SalesTransaction.TransactionTimeStamp).Take(salesCount).ToArray();
            SalesTransaction[] sales = this.TankSales.OrderByDescending(t => t.SalesTransaction.TransactionTimeStamp).Select(t=>t.SalesTransaction).Take(salesCount).ToArray();
            if (tankSales.Length == 0)
                return 0;
            decimal tankVolume = tankSales.Sum(t => t.StartVolume - t.EndVolume.Value);
            decimal vol = this.GetTankVolume(this.FuelLevel);
            decimal vol1 = this.GetTankVolume(this.FuelLevel - (decimal)(this.AlarmThreshold.HasValue ? this.AlarmThreshold.Value : 4) / 2);
            decimal volStart = this.TankSales.Last().EndVolume.Value;
            decimal volthreshold = Math.Abs(vol1 - vol);
            decimal saleVolume = sales.Sum(s => s.Volume);
            if (saleVolume < volthreshold)
                return 0;

            decimal vol2 = this.GetTankVolume(this.GetLastValidLevel());
            decimal vol3 = volStart - vol2;
            
            if (Math.Abs(saleVolume - tankVolume) < Math.Abs(saleVolume - vol3))
                return Math.Abs(saleVolume - tankVolume);
            else
                return Math.Abs(saleVolume - vol3);
        }

        public decimal TankAlertMargin
        {
            get
            {
                TankSetting setting = this.TankSettings.Where(ts => ts.SettingKey == "TankAlertMargin").FirstOrDefault();
                if (setting == null)
                    return 15;
                return decimal.Parse(setting.SettingValue);
            }
            set
            {
                TankSetting setting = this.TankSettings.Where(ts => ts.SettingKey == "TankAlertMargin").FirstOrDefault();
                if (setting == null)
                {
                    DatabaseModel db = DatabaseModel.GetContext(this) as DatabaseModel;

                    setting = new TankSetting();
                    setting.TankSettingId = Guid.NewGuid();
                    db.Add(setting);
                    setting.TankId = this.TankId;
                    setting.SettingKey = "TankAlertMargin";
                    this.TankSettings.Add(setting);
                }
                setting.SettingValue = value.ToString();
            }
        }

        public Guid FillingInvoiceId
        {
            get 
            {
                TankSetting setting = this.TankSettings.Where(ts => ts.SettingKey == "FillingInvoiceId").FirstOrDefault();
                if (setting == null)
                    return Guid.Empty;
                return Guid.Parse(setting.SettingValue);
            }
            set 
            { 
                TankSetting setting = this.TankSettings.Where(ts => ts.SettingKey == "FillingInvoiceId").FirstOrDefault();
                if (setting == null)
                {
                    DatabaseModel db = DatabaseModel.GetContext(this) as DatabaseModel;

                    setting = new TankSetting();
                    setting.TankSettingId = Guid.NewGuid();
                    db.Add(setting);
                    setting.TankId = this.TankId;
                    setting.SettingKey = "FillingInvoiceId";
                    this.TankSettings.Add(setting);
                }
                setting.SettingValue = value.ToString();
            }
        }

        public Guid InvoiceTypeId
        {
            get
            {
                TankSetting setting = this.TankSettings.Where(ts => ts.SettingKey == "InvoiceTypeId").FirstOrDefault();
                if (setting == null)
                    return Guid.Empty;
                return Guid.Parse(setting.SettingValue);
            }
            set
            {
                TankSetting setting = this.TankSettings.Where(ts => ts.SettingKey == "InvoiceTypeId").FirstOrDefault();
                if (setting == null)
                {
                    DatabaseModel db = DatabaseModel.GetContext(this) as DatabaseModel;

                    setting = new TankSetting();
                    setting.TankSettingId = Guid.NewGuid();
                    db.Add(setting);
                    setting.TankId = this.TankId;
                    setting.SettingKey = "InvoiceTypeId";
                    this.TankSettings.Add(setting);
                }
                setting.SettingValue = value.ToString();
            }
        }

        public Guid FillingFuelTypeId
        {
            get
            {
                TankSetting setting = this.TankSettings.Where(ts => ts.SettingKey == "FillingFuelTypeId").FirstOrDefault();
                if (setting == null)
                    return Guid.Empty;
                return Guid.Parse(setting.SettingValue);
            }
            set
            {
                TankSetting setting = this.TankSettings.Where(ts => ts.SettingKey == "FillingFuelTypeId").FirstOrDefault();
                if (setting == null)
                {
                    DatabaseModel db = DatabaseModel.GetContext(this) as DatabaseModel;

                    setting = new TankSetting();
                    setting.TankSettingId = Guid.NewGuid();
                    db.Add(setting);
                    setting.TankId = this.TankId;
                    setting.SettingKey = "FillingFuelTypeId";
                    this.TankSettings.Add(setting);
                }
                setting.SettingValue = value.ToString();
            }
        }

        public Guid VehicleId
        {
            get
            {
                TankSetting setting = this.TankSettings.Where(ts => ts.SettingKey == "VehicleId").FirstOrDefault();
                if (setting == null)
                    return Guid.Empty;
                return Guid.Parse(setting.SettingValue);
            }
            set
            {
                TankSetting setting = this.TankSettings.Where(ts => ts.SettingKey == "VehicleId").FirstOrDefault();
                if (setting == null)
                {
                    DatabaseModel db = DatabaseModel.GetContext(this) as DatabaseModel;

                    setting = new TankSetting();
                    setting.TankSettingId = Guid.NewGuid();
                    db.Add(setting);
                    setting.TankId = this.TankId;
                    setting.SettingKey = "VehicleId";
                    this.TankSettings.Add(setting);
                }
                setting.SettingValue = value.ToString();
            }
        }

        public bool IsLiterCheck
        {
            get
            {
                TankSetting setting = this.TankSettings.Where(ts => ts.SettingKey == "IsLiterCheck").FirstOrDefault();
                if (setting == null)
                    return false;
                return bool.Parse(setting.SettingValue);
            }
            set
            {
                TankSetting setting = this.TankSettings.Where(ts => ts.SettingKey == "IsLiterCheck").FirstOrDefault();
                if (setting == null)
                {
                    DatabaseModel db = DatabaseModel.GetContext(this) as DatabaseModel;

                    setting = new TankSetting();
                    setting.TankSettingId = Guid.NewGuid();
                    db.Add(setting);
                    setting.TankId = this.TankId;
                    setting.SettingKey = "IsLiterCheck";
                    this.TankSettings.Add(setting);
                }
                setting.SettingValue = value.ToString();
            }
        }

        public bool Removed
        {
            get
            {
                TankSetting setting = this.TankSettings.Where(ts => ts.SettingKey == "Removed").FirstOrDefault();
                if (setting == null)
                    return false;
                return bool.Parse(setting.SettingValue);
            }
            set
            {
                TankSetting setting = this.TankSettings.Where(ts => ts.SettingKey == "Removed").FirstOrDefault();
                if (setting == null)
                {
                    DatabaseModel db = DatabaseModel.GetContext(this) as DatabaseModel;

                    setting = new TankSetting();
                    setting.TankSettingId = Guid.NewGuid();
                    db.Add(setting);
                    setting.TankId = this.TankId;
                    setting.SettingKey = "Removed";
                    this.TankSettings.Add(setting);
                }
                setting.SettingValue = value.ToString();
            }
        }

        public bool InitializeFilling
        {
            get
            {
                TankSetting setting = this.TankSettings.Where(ts => ts.SettingKey == "InitializeFilling").FirstOrDefault();
                if (setting == null)
                    return false;
                return bool.Parse(setting.SettingValue);
            }
            set
            {
                TankSetting setting = this.TankSettings.Where(ts => ts.SettingKey == "InitializeFilling").FirstOrDefault();
                if (setting == null)
                {
                    DatabaseModel db = DatabaseModel.GetContext(this) as DatabaseModel;

                    setting = new TankSetting();
                    setting.TankSettingId = Guid.NewGuid();
                    db.Add(setting);
                    setting.TankId = this.TankId;
                    setting.SettingKey = "InitializeFilling";
                    this.TankSettings.Add(setting);
                }
                setting.SettingValue = value.ToString();
            }
        }

        public Common.Enumerators.TankStatusEnum PreviousState
        {
            get
            {
                TankSetting setting = this.TankSettings.Where(ts => ts.SettingKey == "PreviousState").FirstOrDefault();
                if (setting == null)
                    return Common.Enumerators.TankStatusEnum.Offline;
                return (Common.Enumerators.TankStatusEnum)Enum.Parse(typeof(Common.Enumerators.TankStatusEnum), setting.SettingValue);
            }
            set
            {
                TankSetting setting = this.TankSettings.Where(ts => ts.SettingKey == "PreviousState").FirstOrDefault();
                if (setting == null)
                {
                    DatabaseModel db = DatabaseModel.GetContext(this) as DatabaseModel;

                    setting = new TankSetting();
                    setting.TankSettingId = Guid.NewGuid();
                    db.Add(setting);
                    setting.TankId = this.TankId;
                    setting.SettingKey = "PreviousState";
                    this.TankSettings.Add(setting);
                }
                setting.SettingValue = value.ToString();
            }
        }

        public DateTime WaitingStarted
        {
            get
            {
                TankSetting setting = this.TankSettings.Where(ts => ts.SettingKey == "WaitingStarted").FirstOrDefault();
                if (setting == null)
                    return new DateTime(1900, 1, 1);
                return DateTime.Parse(setting.SettingValue);
            }
            set
            {
                TankSetting setting = this.TankSettings.Where(ts => ts.SettingKey == "WaitingStarted").FirstOrDefault();
                if (setting == null)
                {
                    DatabaseModel db = DatabaseModel.GetContext(this) as DatabaseModel;

                    setting = new TankSetting();
                    setting.TankSettingId = Guid.NewGuid();
                    db.Add(setting);
                    setting.TankId = this.TankId;
                    setting.SettingKey = "WaitingStarted";
                    this.TankSettings.Add(setting);
                }
                setting.SettingValue = value.ToString();
            }
        }

        public DateTime WaitingShouldEnd
        {
            get
            {
                TankSetting setting = this.TankSettings.Where(ts => ts.SettingKey == "WaitingShouldEnd").FirstOrDefault();
                if (setting == null)
                    return new DateTime(1900, 1, 1);
                return DateTime.Parse(setting.SettingValue);
            }
            set
            {
                TankSetting setting = this.TankSettings.Where(ts => ts.SettingKey == "WaitingShouldEnd").FirstOrDefault();
                if (setting == null)
                {
                    DatabaseModel db = DatabaseModel.GetContext(this) as DatabaseModel;

                    setting = new TankSetting();
                    setting.TankSettingId = Guid.NewGuid();
                    db.Add(setting);
                    setting.TankId = this.TankId;
                    setting.SettingKey = "WaitingShouldEnd";
                    this.TankSettings.Add(setting);
                }
                setting.SettingValue = value.ToString();
            }
        }

        public DateTime DeliveryStrarted
        {
            get
            {
                TankSetting setting = this.TankSettings.Where(ts => ts.SettingKey == "DeliveryStrarted").FirstOrDefault();
                if (setting == null)
                    return new DateTime(1900, 1, 1);
                return DateTime.Parse(setting.SettingValue);
            }
            set
            {
                TankSetting setting = this.TankSettings.Where(ts => ts.SettingKey == "DeliveryStrarted").FirstOrDefault();
                if (setting == null)
                {
                    DatabaseModel db = DatabaseModel.GetContext(this) as DatabaseModel;

                    setting = new TankSetting();
                    setting.TankSettingId = Guid.NewGuid();
                    db.Add(setting);
                    setting.TankId = this.TankId;
                    setting.SettingKey = "DeliveryStrarted";
                    this.TankSettings.Add(setting);
                }
                setting.SettingValue = value.ToString();
            }
        }

        public string DeviceSeal
        {
            get
            {
                return DeviceSetting.GetSetting("DeviceSeal", this);
            }
            set
            {
                DeviceSetting.SetSetting("DeviceSeal", value, this, "Σφραγγίδα Δεξαμενής", false);
            }
        }

        public decimal GaugeMinimumHeight
        {
            get
            {
                TankSetting setting = this.TankSettings.Where(ts => ts.SettingKey == "GaugeMinimumHeight").FirstOrDefault();
                if (setting == null)
                    return 130;
                return decimal.Parse(setting.SettingValue);
            }
            set
            {
                TankSetting setting = this.TankSettings.Where(ts => ts.SettingKey == "GaugeMinimumHeight").FirstOrDefault();
                if (setting == null)
                {
                    DatabaseModel db = DatabaseModel.GetContext(this) as DatabaseModel;

                    setting = new TankSetting();
                    setting.TankSettingId = Guid.NewGuid();
                    db.Add(setting);
                    setting.TankId = this.TankId;
                    setting.SettingKey = "GaugeMinimumHeight";
                    this.TankSettings.Add(setting);
                }
                setting.SettingValue = value.ToString();
            }
        } 
    }

    public partial class Nozzle
    {
        public decimal GetTotalizerStartAtTime(DateTime dt)
        {
            SalesTransaction sale = this.SalesTransactions.Where(s => s.TransactionTimeStamp >= dt).OrderBy(s => s.TransactionTimeStamp).FirstOrDefault();
            if (sale == null)
                return this.TotalCounter / 100;
            return sale.TotalizerStart / 100;
        }

        public decimal GetTotalizerEndAtTime(DateTime dt)
        {
            SalesTransaction sale = this.SalesTransactions.Where(s => s.TransactionTimeStamp >= dt).OrderBy(s => s.TransactionTimeStamp).FirstOrDefault();
            if (sale == null)
                return this.TotalCounter / 100;
            return sale.TotalizerEnd / 100;
        }

        public decimal GetLitercheckSum(DateTime ds, DateTime de)
        {
            Guid literCheckType = OptionHandler.Instance.GetGuidOption("LiterCheckInvoiceType", Guid.Empty);
            DatabaseModel db = DatabaseModel.GetContext(this) as DatabaseModel;
            var q = db.InvoiceLines.Where(il => il.SalesTransaction.NozzleId == this.NozzleId && il.Invoice.TransactionDate <= de && il.Invoice.TransactionDate >= ds && il.Invoice.InvoiceTypeId == literCheckType);
            return q.Sum(s => s.Volume);

        }

        public decimal GetLitercheckNomalizedSum(DateTime ds, DateTime de)
        {
            Guid literCheckType = OptionHandler.Instance.GetGuidOption("LiterCheckInvoiceType", Guid.Empty);
            DatabaseModel db = DatabaseModel.GetContext(this) as DatabaseModel;
            var q = db.InvoiceLines.Where(il => il.SalesTransaction.NozzleId == this.NozzleId && il.Invoice.TransactionDate <= de && il.Invoice.TransactionDate >= ds && il.Invoice.InvoiceTypeId == literCheckType);
            return q.Sum(s => s.VolumeNormalized);

        }

        public decimal GetAvgTemperature(IEnumerable<SalesTransaction> sales)
        {
            decimal temp = 0;
            decimal vol = 0;
            foreach (SalesTransaction sale in sales)
            {
                decimal sTemp = (sale.TemperatureEnd + sale.TemperatureEnd) / 2;
                sTemp = sTemp * sale.VolumeNormalized;
                temp = temp + sTemp;
                vol = vol + sale.VolumeNormalized;
            }
            if (vol == 0)
                return 0;
            return temp / vol;
        }

        public decimal GetAvgDensity(IEnumerable<SalesTransaction> sales)
        {
            decimal dens = 0;
            decimal vol = 0;
            foreach (SalesTransaction sale in sales)
            {
                dens = dens + sale.GetAvgDensity();
                vol = vol + sale.VolumeNormalized;
            }
            if (vol > 0)
                return dens / vol;
            return this.FuelType.BaseDensity;
        }
    }

    public partial class SalesTransaction
    {
        public decimal GetAvgDensity()
        {
            decimal density = 0;
            decimal volume = 0;
            foreach (TankSale ts in this.TankSales)
            {
                density = density + (ts.FuelDensity * this.Volume);
                volume = volume + ts.StartVolumeNormalized - ts.EndVolumeNormalized.Value;
            }
            if (volume > 0)
                return density / volume;
            return this.Nozzle.FuelType.BaseDensity;
        }

        public InvoiceLine MainInvoiceLine
        {
            get
            {
                if (this.InvoiceLines.Count == 0)
                    return null;
                return this.InvoiceLines[0];
            }
        }

        public decimal TotalizerDiff
        {
            get 
            {
                return this.TotalizerEnd - this.TotalizerStart;
            }
        }
    }

    public partial class InvoiceGroupView
    {
        public List<FuelTypeSale> FuelTypeSales
        {
            get
            {
                List<FuelTypeSale> sales = new List<FuelTypeSale>();
                DatabaseModel db = DatabaseModel.GetContext(this) as DatabaseModel;
                DateTime dt = this.TransactionDate.Value.Date;
                var q = db.SalesTransactions.Where(s => s.TransactionTimeStamp.Date == dt && s.InvoiceLines.Count > 0).ToList();

                var qg = q.GroupBy(s => new { s.Nozzle.FuelTypeId, s.MainInvoiceLine.Invoice.InvoiceTypeId });
                foreach (var g in qg)
                {
                    FuelTypeSale sale = new FuelTypeSale();
                    sale.FuelTypeId = g.Key.FuelTypeId;
                    sale.InvoiceTypeId = g.Key.InvoiceTypeId;
                    sale.InvoiceTypeName = db.InvoiceTypes.Where(i => i.InvoiceTypeId == g.Key.InvoiceTypeId).First().Description;
                    sale.FuelTypeName = db.FuelTypes.Where(f => f.FuelTypeId == g.Key.FuelTypeId).First().Name;
                    sale.TotalVolume = g.Sum(s => s.Volume);
                    sale.TotalInvoiceCount = g.Count();
                    sale.TotalSum = g.Sum(s => s.MainInvoiceLine.TotalPrice);
                    sale.VATAmount = g.Sum(s => s.MainInvoiceLine.VatAmount);
                    sale.NettoPrice = sale.TotalSum - sale.VATAmount;
                    sales.Add(sale);
                }

                return sales;
            }
        }
    }

    public class FuelTypeSale
    {
        public Guid InvoiceTypeId { set; get; }
        public Guid FuelTypeId { set; get; }
        public string FuelTypeName { set; get; }
        public string InvoiceTypeName { set; get; }
        public int TotalInvoiceCount { set; get; }
        public decimal TotalVolume { set; get; }
        public decimal NettoPrice { set; get; }
        public decimal VATAmount { set; get; }
        public decimal TotalSum { set; get; }
    }

    public partial class SaleDataView
    {
        public decimal TotalizerDiff
        {
            get 
            { 
                return this.TotalizerEnd - this.TotalizerStart; 
            }
        }

        public decimal TankVolumeDiff
        {
            get
            {
                return this.StartVolume - (this.EndVolume.HasValue ? this.EndVolume.Value : 0);
            }
        }

        public decimal TankLevelDiff
        {
            get
            {
                return this.StartLevel - (this.EndLevel.HasValue ? this.EndLevel.Value : 0);
            }
        }
    }

    public class CompanyData
    {
        public string CompanyAddress
        {
            get { return Implementation.OptionHandler.Instance.GetOption("CompanyAddress"); }
        }

        public string CompanyCity
        {
            get { return Implementation.OptionHandler.Instance.GetOption("CompanyCity"); }
        }

        public string CompanyFax
        {
            get { return Implementation.OptionHandler.Instance.GetOption("CompanyFax"); }
        }

        public string CompanyMainAddress
        {
            get { return Implementation.OptionHandler.Instance.GetOption("CompanyMainAddress"); }
        }

        public string CompanyName
        {
            get { return Implementation.OptionHandler.Instance.GetOption("CompanyName"); }
        }

        public string CompanyOccupation
        {
            get { return Implementation.OptionHandler.Instance.GetOption("CompanyOccupation"); }
        }

        public string CompanyPhone
        {
            get { return Implementation.OptionHandler.Instance.GetOption("CompanyPhone"); }
        }

        public string CompanyPostalCode
        {
            get { return Implementation.OptionHandler.Instance.GetOption("CompanyPostalCode"); }
        }

        public string CompanyTaxOffice
        {
            get { return Implementation.OptionHandler.Instance.GetOption("CompanyTaxOffice"); }
        }

        public string CompanyTIN
        {
            get { return Implementation.OptionHandler.Instance.GetOption("CompanyTIN"); }
        }

        public System.Drawing.Image CompanyLogo
        {
            get 
            {
                try
                {
                    string logoData = Implementation.OptionHandler.Instance.GetOption("CompanyLogo");
                    if (logoData == "" || logoData == "")
                        return null;
                    return StringToImage(logoData);
                }
                catch
                {
                    return null;
                }
            }
        }

        public System.Drawing.Color InvoiceColor
        {
            get
            {
                try
                {
                    string logoColor = Implementation.OptionHandler.Instance.GetOption("CompanyInvoiceColor");
                    if (logoColor == "" || logoColor == "")
                        return System.Drawing.Color.Empty;
                    return System.Drawing.Color.FromArgb(int.Parse(logoColor));
                }
                catch
                {
                    return System.Drawing.Color.Empty;
                }
            }
        }


        private System.Drawing.Image StringToImage(string imageString)
        {
            try
            {
                if (imageString == null)
                    throw new ArgumentNullException("imageString");

                byte[] array = Convert.FromBase64String(imageString);
                System.Drawing.Image image = System.Drawing.Image.FromStream(new System.IO.MemoryStream(array));
                return image;
            }
            catch
            {
                return null;
            }
        }
    }

    #region event args definitions

    public class DatabaseChangeArgs : System.EventArgs
    {
        public IList<object> Changes { set; get; }
        public bool Cancel { set; get; }
    }

    public class SendDataEventArgs : EventArgs
    {
        public object NewEntry { set; get; }

        public SendDataEventArgs(object entity)
        {
            this.NewEntry = entity;
        }
    }

    #endregion
}

