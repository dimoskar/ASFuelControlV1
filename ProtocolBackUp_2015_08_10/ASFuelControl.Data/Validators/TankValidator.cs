using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Telerik.OpenAccess;

namespace ASFuelControl.Data.Validators
{
    public class TankValidator : IDataValidator<Data.Tank>
    {
        #region IDataValidator Members

        public DatabaseModel Database{set; get;}

        public Type EntityType
        {
            get { return typeof(Data.Tank); }
        }

        public Implementation.ErrorInfo BeforeInsert(AddEventArgs args)
        {
            return new Implementation.ErrorInfo();
        }

        public Implementation.ErrorInfo AfterInsert(AddEventArgs args)
        {
            return new Implementation.ErrorInfo();
        }

        public Implementation.ErrorInfo BeforeUpdate(ChangeEventArgs args)
        {
            
            return new Implementation.ErrorInfo();
        }

        public Implementation.ErrorInfo AfterUpdate(ChangeEventArgs args)
        {
            if (args.PropertyName == "OffsetVolume" && (decimal)args.NewValue > 50)
            {
                Implementation.ErrorInfo error = new Implementation.ErrorInfo();
                error.AddErrorInfo(args.PropertyName, "To Big", Implementation.ErrorInfoStateEnum.Error);
                return error;
            }
            return new Implementation.ErrorInfo();
        }

        public Implementation.ErrorInfo BeforeDelete(RemoveEventArgs args)
        {
            Implementation.ErrorInfo error = new Implementation.ErrorInfo();
            error.AddErrorInfo("Not Allowed Big", Implementation.ErrorInfoStateEnum.Error);
            return error;
            //return new Implementation.ErrorInfo();
        }

        public Implementation.ErrorInfo AfterDelete(RemoveEventArgs args)
        {
            return new Implementation.ErrorInfo();
        }

        public void InitializeEntity(Tank entity)
        {
            
        }

        #endregion

    }
}
