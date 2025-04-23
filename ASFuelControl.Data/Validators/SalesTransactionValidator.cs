using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Telerik.OpenAccess;
using ASFuelControl.Data.Implementation;

namespace ASFuelControl.Data.Validators
{
    public class SalesTransactionValidator : IDataValidator<Data.SalesTransaction>
    {
        #region IDataValidator Members

        public DatabaseModel Database { set; get; }

        public Type EntityType
        {
            get { return typeof(Data.TankFilling); }
        }

        public Implementation.ErrorInfo BeforeInsert(AddEventArgs args)
        {
            return new Implementation.ErrorInfo();
        }

        public Implementation.ErrorInfo AfterInsert(AddEventArgs args)
        {
            Implementation.ErrorInfo error = this.AssignPrice(args.PersistentObject as SalesTransaction);
            return error;
        }

        public Implementation.ErrorInfo BeforeUpdate(ChangeEventArgs args)
        {

            return new Implementation.ErrorInfo();
        }

        public Implementation.ErrorInfo AfterUpdate(ChangeEventArgs args)
        {
            SalesTransaction sale = args.PersistentObject as SalesTransaction;
            sale.CRC = sale.CalculateCRC32();
            if (sale.UsagePeriod != null && sale.UsagePeriod.IsLocked)
            {
                ErrorInfo error = new ErrorInfo();
                error.AddErrorInfo(Properties.Resources.UsagePeriodLocked, ErrorInfoStateEnum.Error);
                return error;
            }
            else
            {
                Implementation.ErrorInfo error = this.AssignPrice(args.PersistentObject as SalesTransaction);
                return error;
            }
        }

        public Implementation.ErrorInfo BeforeDelete(RemoveEventArgs args)
        {
            return new Implementation.ErrorInfo();
        }

        public Implementation.ErrorInfo AfterDelete(RemoveEventArgs args)
        {
            return new Implementation.ErrorInfo();
        }

        public void InitializeEntity(Data.SalesTransaction entity)
        {
        }

        #endregion

        #region private methods

        private Implementation.ErrorInfo AssignPrice(SalesTransaction sale)
        {
            Implementation.ErrorInfo error = new Implementation.ErrorInfo();
            try
            {
                DateTime dt = sale.InvoiceLines.FirstOrDefault().Invoice.TransactionDate;
                if (sale.Nozzle == null)
                    return error;
                
                UsagePeriod usagePeriod = this.Database.UsagePeriods.Where(up => up.PeriodStart <= dt && (!up.PeriodEnd.HasValue || up.PeriodEnd.Value >= dt)).FirstOrDefault();
                if (usagePeriod == null)
                {
                    string messageText = string.Format(Properties.Resources.UsagePeriod_PerioNotFound, dt);
                    error.AddErrorInfo(messageText, Implementation.ErrorInfoStateEnum.Error);
                }
                else
                {
                    sale.UsagePeriodId = usagePeriod.UsagePeriodId;
                }
                return error;
            }
            catch (Exception ex)
            {
                error.AddErrorInfo(ex.Message + "::" + ex.StackTrace, Implementation.ErrorInfoStateEnum.Error);
                return error;

            }
            finally
            {
                
            }
        }

        #endregion
    }
}
