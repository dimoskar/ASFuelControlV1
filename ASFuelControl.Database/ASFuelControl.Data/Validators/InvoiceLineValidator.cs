using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.Data.Validators
{
    public class InvoiceLineValidator : IDataValidator<Data.InvoiceLine>
    {
        private bool suspendAfterUpdateEvent = false;

        public DatabaseModel Database { set; get; }

        public Type EntityType
        {
            get { return typeof(Data.InvoiceLine); }
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
            if (suspendAfterUpdateEvent)
                return new Implementation.ErrorInfo();

            decimal vatValue = Implementation.OptionHandler.Instance.GetDecimalOption("VATValue", 23);

            InvoiceLine invL = args.PersistentObject as InvoiceLine;

            //if (invL.Invoice.InvoiceType.TransactionType == 1 && args.PropertyName != "InvoiceId" && invL.Invoice != null)
            //    Implementation.OptionHandler.Instance.CheckFillinInvoices = true;

            if ((args.PropertyName == "UnitPrice" || args.PropertyName == "VolumeNormalized" || args.PropertyName == "DiscountAmount") && invL.Invoice.InvoiceType != null && invL.Invoice.InvoiceType.TransactionType == 1)
            {

                suspendAfterUpdateEvent = true;
                InvoiceLine invLine = args.PersistentObject as InvoiceLine;
                invLine.TotalPrice = invLine.UnitPrice * invLine.VolumeNormalized - invLine.DiscountAmount;
                invLine.VatPercentage = vatValue;
                invLine.VatAmount = invLine.TotalPrice * invLine.VatPercentage / 100;
                if (invLine.Invoice != null)
                {
                    decimal netto = 0;
                    decimal total = 0;
                    decimal vat = 0;
                    foreach (InvoiceLine il in invLine.Invoice.InvoiceLines)
                    {
                        netto = netto + il.TotalPrice;
                        total = netto + il.VatAmount;
                        vat = vat + il.VatAmount;
                    }
                    invLine.Invoice.NettoAmount = decimal.Round(netto, 2);
                    invLine.Invoice.VatAmount = decimal.Round(vat, 2);
                    invLine.Invoice.TotalAmount = decimal.Round(invLine.Invoice.NettoAmount.Value + invLine.Invoice.VatAmount.Value, 2);
                }

                suspendAfterUpdateEvent = false;
            }
            if (args.PropertyName == "VolumeNormalized" || args.PropertyName == "Volume" ||  args.PropertyName == "FuelDensity" || args.PropertyName == "Temperature")
            {
                InvoiceLine invLine = args.PersistentObject as InvoiceLine;
                if (invLine.Volume == 0 || invLine.VolumeNormalized == 0 || invLine.FuelType == null || invLine.FuelDensity == 0)
                    return new Implementation.ErrorInfo();
                decimal vol = invLine.FuelType.NormalizeVolume(invLine.Volume, invLine.Temperature, invLine.FuelDensity);
                Implementation.ErrorInfo info = new Implementation.ErrorInfo();
                if (vol < invLine.VolumeNormalized && vol / invLine.VolumeNormalized < (decimal)0.99)
                {
                    info.AddErrorInfo(string.Format(Properties.Resources.InvoiceLine_RecieveNormalizationError, vol), Implementation.ErrorInfoStateEnum.Info);
                }
                else if (vol > invLine.VolumeNormalized && invLine.VolumeNormalized / vol < (decimal)0.99)
                {
                    info.AddErrorInfo(string.Format(Properties.Resources.InvoiceLine_RecieveNormalizationError, vol), Implementation.ErrorInfoStateEnum.Info);
                }
                return info;
            }
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

        public void InitializeEntity(InvoiceLine entity)
        {

        }
    }
}
