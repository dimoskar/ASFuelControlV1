using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.Data.Validators
{
    public class InvoiceValidator : IDataValidator<Data.Invoice>
    {
        public DatabaseModel database;

        public DatabaseModel Database 
        {
            set 
            { 
                this.database = value;
                this.database.AfterInsert += new EventHandler<DatabaseChangeArgs>(database_AfterInsert);
                this.database.AfterUpdate += new EventHandler<DatabaseChangeArgs>(database_AfterUpdate);
                this.database.AfterDelete += new EventHandler<DatabaseChangeArgs>(database_AfterDelete);
            }
            get { return this.database; } 
        }

        public Type EntityType
        {
            get { return typeof(Data.Invoice); }
        }

        public Implementation.ErrorInfo BeforeInsert(Telerik.OpenAccess.AddEventArgs args)
        {
            return new Implementation.ErrorInfo();
        }

        public Implementation.ErrorInfo AfterInsert(Telerik.OpenAccess.AddEventArgs args)
        {
            return new Implementation.ErrorInfo();
        }

        public Implementation.ErrorInfo BeforeUpdate(Telerik.OpenAccess.ChangeEventArgs args)
        {
            return new Implementation.ErrorInfo();
        }

        public Implementation.ErrorInfo AfterUpdate(Telerik.OpenAccess.ChangeEventArgs args)
        {
            return new Implementation.ErrorInfo();
        }

        public Implementation.ErrorInfo BeforeDelete(Telerik.OpenAccess.RemoveEventArgs args)
        {
            return new Implementation.ErrorInfo();
        }

        public Implementation.ErrorInfo AfterDelete(Telerik.OpenAccess.RemoveEventArgs args)
        {
            return new Implementation.ErrorInfo();
        }

        void database_AfterDelete(object sender, DatabaseChangeArgs e)
        {

        }

        void database_AfterUpdate(object sender, DatabaseChangeArgs args)
        {
            //foreach(object entity in args.Changes)
            //{
            //    if(entity.GetType() != typeof(Invoice))
            //        continue;
            //    Invoice inv = entity as Invoice;

            //    if (inv == null)
            //        return;
            //    if (inv.InvoiceTypeId == Guid.Empty)
            //        return;
            //    if (inv.InvoiceType.TransactionType == 1)
            //    {
            //        Implementation.OptionHandler.Instance.CheckFillingStart = true;
            //        Implementation.OptionHandler.Instance.CheckFillingEnd = true;
            //        Implementation.OptionHandler.Instance.CheckFillingCancel = true;
            //    }
            //}
            
        }

        void database_AfterInsert(object sender, DatabaseChangeArgs args)
        {
            //foreach (object entity in args.Changes)
            //{
            //    if (entity.GetType() != typeof(Invoice))
            //        continue;
            //    Invoice inv = entity as Invoice;

            //    if (inv == null)
            //        return;
            //    if (inv.InvoiceTypeId == Guid.Empty)
            //        return;
            //    if (inv.InvoiceType.TransactionType == 1)
            //    {
            //    }
            //}
        }

        public void InitializeEntity(Invoice entity)
        {
        }
    }
}
