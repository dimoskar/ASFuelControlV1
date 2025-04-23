using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Telerik.OpenAccess;
using ASFuelControl.Data.Implementation;

namespace ASFuelControl.Data.Validators
{
    public class TankFillingValidator : IDataValidator<Data.TankFilling>
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
            TankFilling filling = args.PersistentObject as TankFilling;
            Implementation.ErrorInfo error = this.RecalculateTankPurchasePrice(filling);
            return error;
        }

        public Implementation.ErrorInfo BeforeUpdate(ChangeEventArgs args)
        {

            return new Implementation.ErrorInfo();
        }

        public Implementation.ErrorInfo AfterUpdate(ChangeEventArgs args)
        {
            TankFilling filling = args.PersistentObject as TankFilling;
            filling.CRC = filling.CalculateCRC32();
            if (filling.UsagePeriod != null && filling.UsagePeriod.IsLocked)
            {
                ErrorInfo error = new ErrorInfo();
                error.AddErrorInfo(Properties.Resources.UsagePeriodLocked, ErrorInfoStateEnum.Error);
                return error;
            }
            return new Implementation.ErrorInfo();
        }

        public Implementation.ErrorInfo BeforeDelete(RemoveEventArgs args)
        {
            return new Implementation.ErrorInfo();
        }

        public Implementation.ErrorInfo AfterDelete(RemoveEventArgs args)
        {
            return new Implementation.ErrorInfo();
        }

        public void InitializeEntity(Data.TankFilling entity)
        {
        }

        #endregion

        #region private methods

        private Implementation.ErrorInfo RecalculateTankPurchasePrice(TankFilling filling)
        {
            Implementation.ErrorInfo error = new Implementation.ErrorInfo();
            try
            {
                //InvoiceLine invoiceLIne = this.Database.InvoiceLines.Where(inl => inl.TankFillingId.HasValue && inl.TankFillingId.Value == filling.TankFillingId).FirstOrDefault();

                //decimal currentVolume = filling.Tank.CurrentVolume;
                //UsagePeriod usagePeriod = this.Database.GetUsagePeriod(filling.TransactionTime);
                //if (usagePeriod == null)
                //{
                //    string messageText = string.Format(Properties.Resources.UsagePeriod_PerioNotFound, filling.TransactionTime);
                //    error.AddErrorInfo(messageText, Implementation.ErrorInfoStateEnum.Error);
                //}
                //else
                //{
                //    TankUsagePeriod tup = this.Database.GetCurrentTankUsagePeriod(filling.Tank, filling.TransactionTime);

                //    if (tup == null)
                //    {
                //        error.AddErrorInfo(Properties.Resources.TankFillingValidator_TankUsagePriodNotFound, Implementation.ErrorInfoStateEnum.Error);
                //    }
                //    else
                //    {
                //        if (filling.TankId != Guid.Empty)
                //        {
                //            filling.UsagePeriodId = usagePeriod.UsagePeriodId;

                //            TankPrice currentPriceRecord = filling.Tank.TankPrices.Where(tp=>tp.ChangeDate < filling.TransactionTime).OrderByDescending(tp => tp.ChangeDate).Take(1).FirstOrDefault();
                //            decimal currentPrice = 0;
                //            decimal currentDensity = 0;
                //            if (currentPriceRecord != null)
                //            {
                //                currentPrice = currentPriceRecord.Price;
                //                currentDensity = currentPriceRecord.FuelDensity;
                //            }
                //            decimal previousValue = currentVolume * currentPrice;
                //            decimal newValue = (filling.VolumeNormalized * invoiceLIne.UnitPrice) + previousValue;
                //            decimal newPrice = newValue / (currentVolume + filling.VolumeNormalized);

                //            decimal previousDensity = currentVolume * currentDensity;
                //            decimal newDensity1 = (filling.VolumeNormalized * invoiceLIne.FuelDensity) + previousDensity;
                //            decimal newDensity = newDensity1 / (currentVolume + filling.VolumeNormalized);

                //            if (filling.TankPrice != null)
                //            {
                //                filling.TankPrice.Price = newPrice;
                //                filling.TankPrice.FuelDensity = newDensity;
                //            }
                //            else
                //            {
                //                TankPrice tankPrice = this.Database.CreateEntity<TankPrice>();

                //                if (tankPrice == null)
                //                {
                //                    error.AddErrorInfo(Properties.Resources.TankFillingValidator_TankPriceCreationFailed, Implementation.ErrorInfoStateEnum.Error);
                //                }
                //                else
                //                {
                //                    filling.TankPriceId = tankPrice.TankPriceId;
                //                    filling.TankPrice = tankPrice;
                //                    tankPrice.TankFillings.Add(filling);

                //                    tankPrice.Price = newPrice;
                //                    tankPrice.FuelDensity = newDensity;
                //                    tankPrice.ChangeDate = filling.TransactionTime;
                //                    tankPrice.TankId = filling.TankId;
                //                }
                //            }
                //        }
                //    }
                //}
                return error;
            }
            catch(Exception ex)
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
