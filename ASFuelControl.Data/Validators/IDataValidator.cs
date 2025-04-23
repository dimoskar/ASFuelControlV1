using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Telerik.OpenAccess;

namespace ASFuelControl.Data.Validators
{
    public interface IDataValidator<T> where T : IDataErrorInfo
    {
        DatabaseModel Database { set; get; }
        Type EntityType { get; }

        Data.Implementation.ErrorInfo BeforeInsert(AddEventArgs args);
        Data.Implementation.ErrorInfo AfterInsert(AddEventArgs args);
        Data.Implementation.ErrorInfo BeforeUpdate(ChangeEventArgs args);
        Data.Implementation.ErrorInfo AfterUpdate(ChangeEventArgs args);
        Data.Implementation.ErrorInfo BeforeDelete(RemoveEventArgs args);
        Data.Implementation.ErrorInfo AfterDelete(RemoveEventArgs args);
        void InitializeEntity(T entity);
    }
}
